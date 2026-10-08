#!/bin/bash
# Linux manual adapter: identical M1 bundle, publisher setup, source tree/lint/preflight controls.
set -o pipefail
source /usr/lib/openfoam/openfoam2512/etc/bashrc
set -eu
run="$1"; log="$2"; app="$3"; shift 3
repo=/mnt/c/Projects/CFD-Workbench-win-w4-validation
bundle="$repo/cases/tools/foam-bundle/controlDict"
case "$app" in checkMesh|decomposePar|simpleFoam|reconstructPar) ;; *) exit 2 ;; esac
test "$(sha256sum "$bundle" | cut -d' ' -f1)" = f3debe8b5541fb400b0719976f591781a2faa21f96ea7ae0dccca97e4a6ef854
python3 "$repo/cases/tools/launcher-record.py" verify "$run"
python3 "$repo/cases/tools/foam-dict-lint.py" "$run"
if test "$app" != checkMesh; then python3 "$repo/cases/tools/launcher-record.py" preflight-check "$run"; fi
test ! -e "$repo/runs/.security-stop"
export HOME="/root/CFDWorkbench/homes/$(basename "$run")"
mkdir -p "$HOME/.OpenFOAM/2512"
cp "$bundle" "$HOME/.OpenFOAM/2512/controlDict"
export FOAM_CONTROLDICT="$(cat "$bundle")"
export OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
printf 'start=%s app=%s load=%s\n' "$(date -u +%FT%TZ)" "$app" "$(cat /proc/loadavg)" >> "$run/run-ledger.txt"
argv=("$FOAM_APPBIN/$app" -case "$run" "$@")
if test "$app" = simpleFoam; then
  argv=(mpirun --allow-run-as-root -np 6 "$FOAM_APPBIN/simpleFoam" -parallel -case "$run")
fi
set +e
/usr/bin/time -v -o "$run/time.$log" /usr/bin/nice -n 10 "${argv[@]}" > "$run/log.$log" 2>&1
status=$?
set -e
printf 'end=%s app=%s exit=%s load=%s\n' "$(date -u +%FT%TZ)" "$app" "$status" "$(cat /proc/loadavg)" >> "$run/run-ledger.txt"
if grep -q 'allowSystemOperations : Allowing' "$run/log.$log"; then
  mkdir -p "$repo/runs"
  printf 'Allowing in %s\n' "$run" > "$repo/runs/.security-stop"
  exit 99
fi
grep -q 'allowSystemOperations : Disallowing' "$run/log.$log" || exit 94
python3 "$repo/cases/tools/launcher-record.py" update "$run"
if test "$app" = checkMesh && test "$status" = 0; then python3 "$repo/cases/tools/launcher-record.py" preflight-set "$run"; fi
exit "$status"
