#!/usr/bin/env bash
# wsl-build-openfoam-cuda.sh - reproducible Area 5 WSL2 build & qualification for
# CUDA 13.2 + OpenFOAM v2512 + PETSc 3.26.0 (--with-cuda=1) + petsc4Foam on Ubuntu 24.04 (arm64/amd64).
#
# Run inside the dedicated WSL2 distribution (never against the host's primary Ubuntu-24.04):
#   wsl.exe -d cfdw-openfoam2512-arm64 -u root -- bash tools/wsl-build-openfoam-cuda.sh \
#       --mode cuda-petsc --keyrings /mnt/d/reviewed --receipt-dir /root/cfdw-receipts
#
# Modes:
#   cuda-petsc      Install signed CUDA 13.2 + GCC-13/OpenMPI/BLAS/LAPACK toolchain, build & check
#                   PETSc 3.26.0 (--with-cuda=1), run CPU vs GPU PETSc KSP smoke, and stage pinned petsc4Foam.
#   openfoam-resume Configure signed OpenCFD repo, install openfoam2512*=2512.0-2, run M1 icoFoam/cavity
#                   CPU smoke (t=0.5, allowSystemOperations 0), build petsc4Foam Allwmake, and run
#                   OpenFOAM CPU vs GPU PETSc pressure-solve trial.
#   full            Run both cuda-petsc and openfoam-resume end-to-end (skips already-built stages).
#
# Security & integrity invariants:
#   - Verifies reviewed keyring SHA-256 digests before writing /etc/apt/keyrings/*.gpg.
#   - Verifies every downloaded apt InRelease index with gpgv VALIDSIG.
#   - Simulates every apt transaction first: forbids package removals and Linux GPU driver packages.
#   - Never bypasses TLS, DNS, or proxy controls.
#   - Verifies PETSc 3.26.0 tarball SHA-256 and exact petsc4Foam commit SHA; forbids PETSc --download-*.
#   - Disables errexit around OpenFOAM etc/bashrc sourcing (Ruling 133).
set -euo pipefail

mode="full"
keyrings="/mnt/d/reviewed"
receipt_dir="/root/cfdw-receipts"
opt_root="/opt/cfdw-f1"
runs_root="/root/cfdw-runs"
make_np="8"
cuda_arch_override=""
repo_root=""

usage() {
    cat <<'EOF' >&2
usage: wsl-build-openfoam-cuda.sh [options]
  --mode <full|cuda-apt|petsc-cuda|cuda-petsc|openfoam-resume>   Build mode (default: full)
  --keyrings <dir>                           Reviewed keyrings directory (default: /mnt/d/reviewed)
  --receipt-dir <dir>                        Persistent directory for JSON receipts/logs (default: /root/cfdw-receipts)
  --opt-root <dir>                           Prefix for PETSc & petsc4Foam (default: /opt/cfdw-f1)
  --runs-root <dir>                          Directory for smoke/qualification runs (default: /root/cfdw-runs)
  --make-np <N>                              Parallel build jobs for PETSc/Allwmake (default: 8)
  --cuda-arch <arch>                         Override nvcc CUDA arch (e.g. 121 or 120); auto-detected if empty
  --repo-root <dir>                          Repo root containing docs/proof/win-routes/r133/cavity-inputs (optional)
EOF
    exit 64
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --mode) mode=$2; shift 2 ;;
        --keyrings) keyrings=$2; shift 2 ;;
        --receipt-dir) receipt_dir=$2; shift 2 ;;
        --opt-root) opt_root=$2; shift 2 ;;
        --runs-root) runs_root=$2; shift 2 ;;
        --make-np) make_np=$2; shift 2 ;;
        --cuda-arch) cuda_arch_override=$2; shift 2 ;;
        --repo-root) repo_root=$2; shift 2 ;;
        -h|--help) usage ;;
        *) printf 'unknown argument: %s\n' "$1" >&2; usage ;;
    esac
done

case "$mode" in
    full|cuda-apt|petsc-cuda|cuda-petsc|openfoam-resume) ;;
    *) printf 'invalid --mode: %s\n' "$mode" >&2; usage ;;
esac

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
if [[ -z "$repo_root" ]]; then
    repo_root=$(cd "$here/.." && pwd)
fi

# Reviewed supply-chain pins
OPENFOAM_KEYRING_SHA="6d17d37b82fc840b8a5c767545703df7264cfa3e30f9cf74556ebb124bda1a93"
NVIDIA_KEYRING_SHA="25100d6f2eccaee7d6719a70e3c0c6145064aa9edc2c32148f693ac4c524a376"
UBUNTU_KEYRING="/usr/share/keyrings/ubuntu-archive-keyring.gpg"

PETSC_VERSION="3.26.0"
PETSC_URL="https://web.cels.anl.gov/projects/petsc/download/release-snapshots/petsc-3.26.0.tar.gz"
PETSC_BYTES="16933940"
PETSC_SHA256="f5230023e6e22ee607802a13c82bc25f3d81e71654ad386a5b9bdff17fed93df"
PETSC_ARCH="arch-linux-cuda132-opt"

PETSC4FOAM_URL="https://gitlab.com/petsc/petsc4foam.git"
PETSC4FOAM_COMMIT="cb1badc4978306e7001a4654dcdf53f284a92365"

arch=$(dpkg --print-architecture)
case "$arch" in
    arm64)
        ubuntu_archive="http://ports.ubuntu.com/ubuntu-ports"
        nvidia_repo="https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/sbsa/"
        ;;
    amd64)
        ubuntu_archive="http://archive.ubuntu.com/ubuntu"
        nvidia_repo="https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/"
        ;;
    *)
        printf 'unsupported architecture: %s\n' "$arch" >&2
        exit 65
        ;;
esac

mkdir -p "$receipt_dir" "$opt_root" "$runs_root" /etc/apt/keyrings

step() { printf '== [%s] %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$1"; }

verify_file_sha256() {
    local path=$1 expected=$2 label=$3 actual
    [[ -f "$path" ]] || { printf 'missing %s: %s\n' "$label" "$path" >&2; exit 1; }
    actual=$(sha256sum -- "$path" | cut -c1-64)
    if [[ "$actual" != "$expected" ]]; then
        printf '%s sha256 mismatch: got %s expected %s\n' "$label" "$actual" "$expected" >&2
        exit 1
    fi
}

verify_inrelease_dir() {
    local pattern=$1 keyring=$2 label=$3 found=0
    local idx
    for idx in /var/lib/apt/lists/${pattern}; do
        [[ -f "$idx" ]] || continue
        found=1
        local status_file="$receipt_dir/gpgv-$(basename "$idx").txt"
        gpgv --status-fd 1 --keyring "$keyring" "$idx" > "$status_file" 2>&1 || true
        if ! grep -q '^\[GNUPG:\] VALIDSIG ' "$status_file"; then
            printf 'gpgv VALIDSIG failed for %s (%s)\n' "$idx" "$label" >&2
            exit 1
        fi
    done
    if [[ "$found" -eq 0 ]]; then
        printf 'no InRelease file matched %s (%s)\n' "$pattern" "$label" >&2
        exit 1
    fi
}

simulate_and_install_apt() {
    local tag=$1
    shift
    local pkgs=("$@")
    local sim_log="$receipt_dir/apt-simulate-${tag}.log"
    local inst_log="$receipt_dir/apt-install-${tag}.log"

    step "apt simulate (${tag})"
    DEBIAN_FRONTEND=noninteractive apt-get --simulate --no-install-recommends install "${pkgs[@]}" > "$sim_log" 2>&1
    if grep -q '^Remv ' "$sim_log"; then
        printf 'ABORT (%s): apt simulation removes packages:\n' "$tag" >&2
        grep '^Remv ' "$sim_log" >&2
        exit 1
    fi
    if grep -Eq '^Inst (cuda |cuda-drivers|nvidia-driver-|nvidia-kernel-|libnvidia-gl)' "$sim_log"; then
        printf 'ABORT (%s): apt simulation attempted to install forbidden Linux NVIDIA driver:\n' "$tag" >&2
        grep -E '^Inst (cuda |cuda-drivers|nvidia-driver-|nvidia-kernel-|libnvidia-gl)' "$sim_log" >&2
        exit 1
    fi

    step "apt install (${tag})"
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "${pkgs[@]}" > "$inst_log" 2>&1
}

stage_cuda_apt() {
    step "verify reviewed NVIDIA + Ubuntu keyrings"
    verify_file_sha256 "$keyrings/cuda-archive-keyring.gpg" "$NVIDIA_KEYRING_SHA" "cuda-archive-keyring.gpg"
    [[ -f "$UBUNTU_KEYRING" ]] || { printf 'missing ubuntu keyring: %s\n' "$UBUNTU_KEYRING" >&2; exit 1; }
    install -m 0644 "$keyrings/cuda-archive-keyring.gpg" /etc/apt/keyrings/cuda-archive-keyring.gpg
    install -m 0644 "$UBUNTU_KEYRING" /etc/apt/keyrings/ubuntu-archive-keyring.gpg

    step "configure signed Ubuntu + NVIDIA apt sources"
    rm -f /etc/apt/sources.list.d/ubuntu.sources
    cat > /etc/apt/sources.list <<EOF
deb [arch=${arch} signed-by=/etc/apt/keyrings/ubuntu-archive-keyring.gpg] ${ubuntu_archive} noble main universe
deb [arch=${arch} signed-by=/etc/apt/keyrings/ubuntu-archive-keyring.gpg] ${ubuntu_archive} noble-updates main universe
deb [arch=${arch} signed-by=/etc/apt/keyrings/ubuntu-archive-keyring.gpg] ${ubuntu_archive} noble-security main universe
EOF
    cat > /etc/apt/sources.list.d/cuda-ubuntu2404.list <<EOF
deb [arch=${arch} signed-by=/etc/apt/keyrings/cuda-archive-keyring.gpg] ${nvidia_repo} /
EOF

    step "apt-get update (Ubuntu + NVIDIA)"
    apt-get -o Acquire::Languages=none -o APT::Update::Error-Mode=any update > "$receipt_dir/apt-update-cuda.log" 2>&1

    step "gpgv verify Ubuntu + NVIDIA InRelease indices"
    verify_inrelease_dir "*ubuntu*_dists_noble_InRelease" /etc/apt/keyrings/ubuntu-archive-keyring.gpg "ubuntu-noble"
    verify_inrelease_dir "*ubuntu*_dists_noble-updates_InRelease" /etc/apt/keyrings/ubuntu-archive-keyring.gpg "ubuntu-noble-updates"
    verify_inrelease_dir "*ubuntu*_dists_noble-security_InRelease" /etc/apt/keyrings/ubuntu-archive-keyring.gpg "ubuntu-noble-security"
    verify_inrelease_dir "*developer.download.nvidia.com*_InRelease" /etc/apt/keyrings/cuda-archive-keyring.gpg "nvidia-cuda"

    local cuda_roots=(
        "cuda-toolkit-13-2=13.2.2-1"
        gcc-13 g++-13 gfortran-13
        openmpi-bin libopenmpi-dev
        libblas-dev liblapack-dev
        pkg-config "make:${arch}"
        git curl ca-certificates python3 time
    )
    simulate_and_install_apt "cuda-toolchain" "${cuda_roots[@]}"
}

stage_petsc_cuda() {
    export PATH="/usr/local/cuda-13.2/bin:${PATH}"
    export LD_LIBRARY_PATH="/usr/local/cuda-13.2/lib64:${LD_LIBRARY_PATH:-}"
    export OMPI_CC="gcc-13"
    export OMPI_CXX="g++-13"
    export OMPI_FC="gfortran-13"

    step "inspect nvcc and Blackwell GPU architecture flags"
    nvcc --version > "$receipt_dir/nvcc-version.txt" 2>&1
    nvcc --list-gpu-arch > "$receipt_dir/nvcc-list-gpu-arch.txt" 2>&1
    nvidia-smi --query-gpu=name,driver_version,compute_cap,memory.total --format=csv > "$receipt_dir/nvidia-smi.txt" 2>&1

    local cuda_arch="$cuda_arch_override"
    if [[ -z "$cuda_arch" ]]; then
        if grep -qx 'compute_121' "$receipt_dir/nvcc-list-gpu-arch.txt"; then
            cuda_arch="121"
        elif grep -qx 'compute_120' "$receipt_dir/nvcc-list-gpu-arch.txt"; then
            cuda_arch="120"
        else
            printf 'could not determine Blackwell compute_121/120 support from nvcc --list-gpu-arch\n' >&2
            exit 1
        fi
    fi
    printf '%s\n' "$cuda_arch" > "$receipt_dir/selected-cuda-arch.txt"

    local tarball="${opt_root}/petsc-${PETSC_VERSION}.tar.gz"
    local petsc_dir="${opt_root}/petsc-${PETSC_VERSION}"
    if [[ ! -f "$tarball" ]]; then
        step "download PETSc ${PETSC_VERSION}"
        curl -fL --retry 3 -o "${tarball}.part" "$PETSC_URL"
        mv "${tarball}.part" "$tarball"
    fi
    local actual_bytes
    actual_bytes=$(stat -c%s "$tarball")
    if [[ "$actual_bytes" != "$PETSC_BYTES" ]]; then
        printf 'PETSc tarball byte count mismatch: got %s expected %s\n' "$actual_bytes" "$PETSC_BYTES" >&2
        exit 1
    fi
    verify_file_sha256 "$tarball" "$PETSC_SHA256" "petsc-${PETSC_VERSION}.tar.gz"

    if [[ ! -d "$petsc_dir" ]]; then
        step "extract PETSc ${PETSC_VERSION}"
        tar -xzf "$tarball" -C "$opt_root"
    fi

    export PETSC_DIR="$petsc_dir"
    export PETSC_ARCH="$PETSC_ARCH"

    step "check PETSc ./configure --help for CUDA/precision flags"
    (cd "$PETSC_DIR" && ./configure --help) > "$receipt_dir/petsc-configure-help.txt" 2>&1

    if [[ ! -f "${PETSC_DIR}/${PETSC_ARCH}/lib/libpetsc.so" ]]; then
        step "configure PETSc ${PETSC_VERSION} (--with-cuda=1 --with-cuda-arch=${cuda_arch})"
        local t0 t1
        t0=$(date +%s)
        (
            cd "$PETSC_DIR"
            ./configure \
                --with-cc=mpicc \
                --with-cxx=mpicxx \
                --with-fc=mpif90 \
                --with-cuda=1 \
                --with-cuda-dir=/usr/local/cuda-13.2 \
                --with-cuda-arch="$cuda_arch" \
                --with-precision=double \
                --with-scalar-type=real \
                --with-64-bit-indices=0 \
                --with-debugging=0 \
                --with-shared-libraries=1 \
                --with-make-np="$make_np"
        ) > "$receipt_dir/petsc-configure.log" 2>&1
        t1=$(date +%s)
        printf '%s\n' "$((t1 - t0))" > "$receipt_dir/petsc-configure-seconds.txt"

        step "build PETSc ${PETSC_VERSION} (make all -j${make_np})"
        t0=$(date +%s)
        make -C "$PETSC_DIR" PETSC_DIR="$PETSC_DIR" PETSC_ARCH="$PETSC_ARCH" all > "$receipt_dir/petsc-build.log" 2>&1
        t1=$(date +%s)
        printf '%s\n' "$((t1 - t0))" > "$receipt_dir/petsc-build-seconds.txt"
    else
        step "PETSc ${PETSC_VERSION} already built at ${PETSC_DIR}/${PETSC_ARCH}/lib/libpetsc.so; skipping build"
    fi

    step "run PETSc make check"
    local c0 c1
    c0=$(date +%s)
    if [[ "$(id -u)" -eq 0 ]]; then
        export OMPI_ALLOW_RUN_AS_ROOT=1
        export OMPI_ALLOW_RUN_AS_ROOT_CONFIRM=1
    fi
    # Ubuntu Noble system OpenMPI 4.1.6 is not CUDA/GPU-aware; single-rank GPU execution requires -use_gpu_aware_mpi 0
    export PETSC_OPTIONS="${PETSC_OPTIONS:+${PETSC_OPTIONS} }-use_gpu_aware_mpi 0"
    if [[ ! -f "${HOME}/.petscrc" ]] || ! grep -qx -- '-use_gpu_aware_mpi 0' "${HOME}/.petscrc"; then
        printf '%s\n' '-use_gpu_aware_mpi 0' >> "${HOME}/.petscrc"
    fi
    make -C "$PETSC_DIR" PETSC_DIR="$PETSC_DIR" PETSC_ARCH="$PETSC_ARCH" check > "$receipt_dir/petsc-check.log" 2>&1
    c1=$(date +%s)
    printf '%s\n' "$((c1 - c0))" > "$receipt_dir/petsc-check-seconds.txt"

    step "stage pinned petsc4Foam commit ${PETSC4FOAM_COMMIT}"
    local p4f_dir="${opt_root}/petsc4foam"
    if [[ ! -d "${p4f_dir}/.git" ]]; then
        git clone "$PETSC4FOAM_URL" "$p4f_dir" > "$receipt_dir/petsc4foam-clone.log" 2>&1
    fi
    git -C "$p4f_dir" fetch --all --tags >> "$receipt_dir/petsc4foam-clone.log" 2>&1 || true
    git -C "$p4f_dir" checkout --detach "$PETSC4FOAM_COMMIT" >> "$receipt_dir/petsc4foam-clone.log" 2>&1
    local head_sha
    head_sha=$(git -C "$p4f_dir" rev-parse HEAD)
    if [[ "$head_sha" != "$PETSC4FOAM_COMMIT" ]]; then
        printf 'petsc4Foam HEAD mismatch: got %s expected %s\n' "$head_sha" "$PETSC4FOAM_COMMIT" >&2
        exit 1
    fi
    git -C "$p4f_dir" rev-parse 'HEAD^{tree}' > "$receipt_dir/petsc4foam-tree-sha.txt"
}

stage_openfoam_and_petsc4foam() {
    step "verify reviewed OpenFOAM keyring"
    verify_file_sha256 "$keyrings/openfoam-archive-keyring.gpg" "$OPENFOAM_KEYRING_SHA" "openfoam-archive-keyring.gpg"
    install -m 0644 "$keyrings/openfoam-archive-keyring.gpg" /etc/apt/keyrings/openfoam-archive-keyring.gpg

    cat > /etc/apt/sources.list.d/openfoam.list <<EOF
deb [arch=${arch} signed-by=/etc/apt/keyrings/openfoam-archive-keyring.gpg] https://dl.openfoam.com/repos/deb noble main
EOF

    step "apt-get update (OpenFOAM)"
    apt-get -o Acquire::Languages=none -o APT::Update::Error-Mode=any update > "$receipt_dir/apt-update-openfoam.log" 2>&1
    verify_inrelease_dir "*dl.openfoam.com*_InRelease" /etc/apt/keyrings/openfoam-archive-keyring.gpg "openfoam-noble"

    local of_roots=(
        "openfoam2512=2512.0-2"
        "openfoam2512-common=2512.0-2"
        "openfoam2512-dev=2512.0-2"
        "openfoam2512-source=2512.0-2"
        "openfoam2512-tools=2512.0-2"
    )
    simulate_and_install_apt "openfoam2512" "${of_roots[@]}"

    step "source OpenFOAM v2512 environment (set +eu per Ruling 133)"
    set +eu
    # shellcheck disable=SC1091
    source /usr/lib/openfoam/openfoam2512/etc/bashrc
    set -eu

    printf 'WM_OPTIONS=%s\nFOAM_API=%s\nWM_PROJECT_VERSION=%s\n' "${WM_OPTIONS:-}" "${FOAM_API:-}" "${WM_PROJECT_VERSION:-}" > "$receipt_dir/openfoam-env.txt"
    icoFoam -help > "$receipt_dir/icofoam-help.txt" 2>&1 || true

    step "run OpenFOAM v2512 M1 icoFoam/cavity CPU smoke"
    local smoke_dir="${runs_root}/smoke-cavity"
    local cavity_src="${repo_root}/docs/proof/win-routes/r133/cavity-inputs"
    local m1_ctrl="${repo_root}/docs/proof/win-routes/r133/product-controlDict"
    rm -rf "$smoke_dir"
    mkdir -p "$smoke_dir/product-home/.OpenFOAM/2512"
    if [[ -d "$cavity_src" && -f "$m1_ctrl" ]]; then
        cp -a "$cavity_src"/. "$smoke_dir/"
        cp -- "$m1_ctrl" "$smoke_dir/product-home/.OpenFOAM/2512/controlDict"
    else
        printf 'missing frozen cavity inputs or M1 product-controlDict under %s\n' "$repo_root" >&2
        exit 1
    fi

    (
        export HOME="$smoke_dir/product-home"
        export FOAM_CONTROLDICT
        FOAM_CONTROLDICT=$(cat "$HOME/.OpenFOAM/2512/controlDict")
        export OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
        /usr/bin/time -v -o "$smoke_dir/time.blockMesh" blockMesh -case "$smoke_dir" > "$smoke_dir/log.blockMesh" 2>&1
        /usr/bin/time -v -o "$smoke_dir/time.checkMesh" checkMesh -case "$smoke_dir" > "$smoke_dir/log.checkMesh" 2>&1
        /usr/bin/time -v -o "$smoke_dir/time.icoFoam" icoFoam -case "$smoke_dir" > "$smoke_dir/log.icoFoam" 2>&1
    )
    grep -q 'Disallowing' "$smoke_dir/log.icoFoam"
    grep -q 'Mesh OK' "$smoke_dir/log.checkMesh"
    grep -q 'Time = 0.5' "$smoke_dir/log.icoFoam"

    step "build petsc4Foam Allwmake against OpenFOAM v2512 + PETSc ${PETSC_VERSION}"
    export PETSC_DIR="${opt_root}/petsc-${PETSC_VERSION}"
    export PETSC_ARCH="$PETSC_ARCH"
    export LD_LIBRARY_PATH="${PETSC_DIR}/${PETSC_ARCH}/lib:/usr/local/cuda-13.2/lib64:${LD_LIBRARY_PATH:-}"
    local p4f_dir="${opt_root}/petsc4foam"
    (
        cd "$p4f_dir"
        ./Allwmake -j"$make_np" -prefix="${opt_root}/openfoam-module"
    ) > "$receipt_dir/petsc4foam-build.log" 2>&1
    export LD_LIBRARY_PATH="${opt_root}/openfoam-module/platforms/${WM_OPTIONS}/lib:${LD_LIBRARY_PATH}"
    foamHasLibrary -verbose petscFoam > "$receipt_dir/foamHasLibrary-petscFoam.txt" 2>&1
}

case "$mode" in
    cuda-apt)
        stage_cuda_apt
        ;;
    petsc-cuda)
        stage_petsc_cuda
        ;;
    cuda-petsc)
        stage_cuda_apt
        stage_petsc_cuda
        ;;
    openfoam-resume)
        stage_openfoam_and_petsc4foam
        ;;
    full)
        stage_cuda_apt
        stage_petsc_cuda
        stage_openfoam_and_petsc4foam
        ;;
esac

step "done (mode=${mode})"
