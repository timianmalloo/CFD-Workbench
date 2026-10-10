#!/bin/bash
set -u

run() {
    printf '\n===== %s =====\n' "$1"
    shift
    "$@"
    status=$?
    printf 'exit=%s\n' "$status"
}

printf 'capture_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
run kernel uname -srmo
run os-release cat /etc/os-release
run gpu-identity /usr/lib/wsl/lib/nvidia-smi --query-gpu=name,driver_version,memory.total,compute_cap --format=csv,noheader
run gpu-driver-libraries sh -c "ldconfig -p | grep -E 'libcuda|libcudadebugger' || true"

for command_name in nvcc hipcc rocminfo rocm-smi cmake gcc g++ clang++ pkg-config; do
    run "command-${command_name}" command -v "$command_name"
done

run cuda-root test -e /usr/local/cuda
run rocm-root test -e /opt/rocm
run selected-packages dpkg-query -W '-f=${binary:Package}\t${Version}\t${Status}\n' openfoam2512 openfoam2512-common libamdhip64-5
run accelerator-packages sh -c "dpkg-query -W '-f=\${binary:Package}\t\${Version}\n' | grep -Ei 'cuda|rocm|hip|petsc|amgx|umpire' || true"

run openfoam-files sh -c "dpkg-query -L openfoam2512 openfoam2512-common | grep -Ei 'cuda|rocm|amgx|petsc|sycl|offload|umpire|fused|/hip/' || true"
run openfoam-tree sh -c "find /usr/lib/openfoam/openfoam2512 -maxdepth 7 -type f \( -iname '*cuda*' -o -iname '*rocm*' -o -iname '*amgx*' -o -iname '*petsc*' -o -iname '*sycl*' -o -iname '*offload*' -o -iname '*umpire*' -o -iname '*fused*' \) -print"

if test -r /usr/lib/openfoam/openfoam2512/etc/bashrc; then
    set +u
    # shellcheck disable=SC1091
    source /usr/lib/openfoam/openfoam2512/etc/bashrc
    set -u
    printf '\n===== openfoam-activation =====\n'
    printf 'WM_PROJECT_VERSION=%s\nWM_OPTIONS=%s\nFOAM_APPBIN=%s\nFOAM_LIBBIN=%s\n' \
        "${WM_PROJECT_VERSION:-Not recorded}" "${WM_OPTIONS:-Not recorded}" \
        "${FOAM_APPBIN:-Not recorded}" "${FOAM_LIBBIN:-Not recorded}"
    printf 'exit=0\n'
    if test -x "${FOAM_APPBIN:-}/simpleFoam"; then
        run simplefoam-hash sha256sum "$FOAM_APPBIN/simpleFoam"
        run simplefoam-linkage ldd "$FOAM_APPBIN/simpleFoam"
    else
        printf '\n===== simplefoam-linkage =====\nnot found under activated FOAM_APPBIN\nexit=1\n'
    fi
else
    printf '\n===== openfoam-activation =====\n/etc/bashrc not readable\nexit=1\n'
fi

if test -r /usr/lib/openfoam/openfoam2512/etc/config.sh/umpire; then
    run umpire-config cat /usr/lib/openfoam/openfoam2512/etc/config.sh/umpire
else
    printf '\n===== umpire-config =====\nnot found\nexit=1\n'
fi

run candidate-linker-cache sh -c "ldconfig -p | grep -Ei 'libcuda|libamdhip|libpetsc|libamgx|libumpire' || true"
