#!/bin/bash
# Ruling 197 G1: read-only linkage inspection of the installed v2512 libraries.
set -euo pipefail
export LC_ALL=C
export OMP_NUM_THREADS=1

readonly self_sha256=$(sha256sum "$0" | awk '{print $1}')
printf 'capture_start_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
printf 'script_sha256=%s\n' "$self_sha256"
printf 'effective_nice=%s\n' "$(ps -o ni= -p $$ | tr -d ' ')"
printf 'effective_affinity=%s\n' "$(taskset -pc $$ | sed 's/^[^:]*: //')"

set +eu
source /usr/lib/openfoam/openfoam2512/etc/bashrc
activation_exit=$?
set -eu
if ((activation_exit != 0)); then
    printf 'activation_exit=%s\n' "$activation_exit"
    exit "$activation_exit"
fi

printf 'wm_project_version=%s\n' "${WM_PROJECT_VERSION:-Not recorded}"
printf 'wm_options=%s\n' "${WM_OPTIONS:-Not recorded}"
printf 'foam_libbin=%s\n' "${FOAM_LIBBIN:-Not recorded}"
dpkg-query -W -f='package=${binary:Package} version=${Version}\n' openfoam2512 openfoam2512-common 2>/dev/null || true

for library_name in libfusedFiniteVolume.so libOpenFOAM.so; do
    library_path="${FOAM_LIBBIN}/${library_name}"
    printf '\n[library name=%s]\n' "$library_name"
    if [[ ! -f "$library_path" ]]; then
        printf 'state=Not recorded (file absent at activated FOAM_LIBBIN)\n'
        continue
    fi
    printf 'path=%s\n' "$library_path"
    printf 'bytes=%s\n' "$(stat -c '%s' "$library_path")"
    printf 'sha256=%s\n' "$(sha256sum "$library_path" | awk '{print $1}')"
    ldd "$library_path"
done

printf '\ncapture_end_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
