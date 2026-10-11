#!/bin/bash
# Ruling 199 B1: read-only machine/toolchain inspection and immutable upstream metadata capture.
set -euo pipefail
export LC_ALL=C

readonly script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly cache_dir="${script_dir}/.source-cache"
readonly pid_file="${script_dir}/b1.pid"
readonly attempt_file="${script_dir}/b1.attempt"
readonly cancel_file="${script_dir}/probe-cancel.request"
readonly pid_tmp="${pid_file}.$$"
readonly attempt_tmp="${attempt_file}.$$"

if (($# != 2)) || [[ "$1" != "--attempt-id" ]] || [[ ! "$2" =~ ^[A-Za-z0-9._-]+$ ]]; then
    printf 'usage: %s --attempt-id ID\n' "$0" >&2
    exit 64
fi
readonly attempt_id=$2
cancel_requested() {
    [[ -f "$cancel_file" ]] && [[ "$(cat "$cancel_file")" == "$attempt_id" ]]
}
cleanup() {
    rm -f "$pid_file" "$attempt_file" "$pid_tmp" "$attempt_tmp"
}
trap cleanup EXIT
if cancel_requested; then
    printf 'probe_cancelled=before-ownership attempt=%s\n' "$attempt_id"
    exit 75
fi
printf '%s\n' "$attempt_id" > "$attempt_tmp"
mv -f "$attempt_tmp" "$attempt_file"
printf '%s\n' "$$" > "$pid_tmp"
mv -f "$pid_tmp" "$pid_file"
if cancel_requested; then
    printf 'probe_cancelled=after-ownership attempt=%s\n' "$attempt_id"
    exit 75
fi

mkdir -p "$cache_dir"

section() { printf '\n===== %s =====\n' "$1"; }
fetch() {
    local url=$1 target=$2
    if command -v curl >/dev/null 2>&1; then
        curl --fail --location --silent --show-error --output "$target" "$url"
    elif command -v wget >/dev/null 2>&1; then
        wget --quiet --output-document="$target" "$url"
    else
        printf 'no curl or wget available\n' >&2
        return 69
    fi
}

section "effective process"
printf 'pid=%s\n' "$$"
printf 'attempt_id=%s\n' "$attempt_id"
ps -o pid=,ppid=,pgid=,sid=,ni=,psr=,comm= -p "$$"
taskset -pc "$$"

section "distribution"
cat /etc/os-release
uname -m

section "driver CUDA ceiling"
if [[ -x /usr/lib/wsl/lib/nvidia-smi ]]; then
    /usr/lib/wsl/lib/nvidia-smi
    /usr/lib/wsl/lib/nvidia-smi --query-gpu=name,driver_version,compute_cap --format=csv,noheader
else
    printf 'nvidia-smi=Not recorded\n'
fi

section "disk"
df -h /
df -B1 --output=source,size,used,avail,pcent,target /

section "OpenFOAM development surface"
set +eu
source /usr/lib/openfoam/openfoam2512/etc/bashrc >/dev/null 2>&1
activation_exit=$?
set -eu
printf 'activation_exit=%s\n' "$activation_exit"
if ((activation_exit != 0)); then
    exit "$activation_exit"
fi
printf 'WM_PROJECT_DIR=%s\n' "${WM_PROJECT_DIR:-Not recorded}"
printf 'wmake='; command -v wmake || true
if command -v wmake >/dev/null 2>&1; then
    readlink -f "$(command -v wmake)"
fi
for path in \
    "${WM_PROJECT_DIR}/src/OpenFOAM/lnInclude/IOstreams.H" \
    "${WM_PROJECT_DIR}/src/finiteVolume/lnInclude/fvCFD.H" \
    "${WM_PROJECT_DIR}/modules/external-solver/README.md"; do
    if [[ -e "$path" ]]; then
        stat -c 'present %n bytes=%s' "$path"
    else
        printf 'absent %s\n' "$path"
    fi
done
dpkg-query -W -f='${binary:Package}\t${Version}\t${Architecture}\n' 'openfoam2512*' 2>/dev/null || true

section "configured apt metadata only"
apt-cache policy cuda-toolkit cuda-toolkit-13-2 cuda-toolkit-13-4 openfoam2512 openfoam2512-dev || true

section "official upstream metadata"
fetch "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/InRelease" "${cache_dir}/cuda-ubuntu2404-InRelease"
fetch "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/Packages.gz" "${cache_dir}/cuda-ubuntu2404-Packages.gz"
fetch "https://web.cels.anl.gov/projects/petsc/download/release-snapshots/petsc-3.26.0.tar.gz" "${cache_dir}/petsc-3.26.0.tar.gz"
fetch "https://petsc.org/release/install/install/" "${cache_dir}/petsc-install.html"
fetch "https://docs.nvidia.com/cuda/eula/index.html" "${cache_dir}/cuda-eula.html"
sha256sum \
    "${cache_dir}/cuda-ubuntu2404-InRelease" \
    "${cache_dir}/cuda-ubuntu2404-Packages.gz" \
    "${cache_dir}/petsc-3.26.0.tar.gz" \
    "${cache_dir}/petsc-install.html" \
    "${cache_dir}/cuda-eula.html"
stat -c '%n bytes=%s' \
    "${cache_dir}/cuda-ubuntu2404-InRelease" \
    "${cache_dir}/cuda-ubuntu2404-Packages.gz" \
    "${cache_dir}/petsc-3.26.0.tar.gz" \
    "${cache_dir}/petsc-install.html" \
    "${cache_dir}/cuda-eula.html"

section "PETSc CUDA contract excerpt"
grep -m 5 -o -E '.{0,120}--with-cuda.{0,180}' "${cache_dir}/petsc-install.html" || true

section "NVIDIA package closures"
/usr/bin/python3 "${script_dir}/resolve_packages.py" \
    --packages "${cache_dir}/cuda-ubuntu2404-Packages.gz" \
    --root cuda-toolkit-13-2 \
    --output "${script_dir}/cuda-13-2-inventory.json"
/usr/bin/python3 "${script_dir}/resolve_packages.py" \
    --packages "${cache_dir}/cuda-ubuntu2404-Packages.gz" \
    --root cuda-toolkit-13-4 \
    --output "${script_dir}/cuda-13-4-inventory.json"
