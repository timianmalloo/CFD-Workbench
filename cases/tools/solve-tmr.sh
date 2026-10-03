#!/bin/bash
# Mesh check, solve and summarise one generated SPIKE-04 TMR case.
# Usage: solve-tmr.sh <run-dir> <n-procs ≤ 6>
set -euo pipefail
run="$1"; np="$2"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
"$of" "$run" checkMesh checkMesh
"$of" "$run" decomposePar decomposePar -force
set +e
"$of" "$run" simpleFoam mpirun -np "$np" simpleFoam -parallel
solve=$?
set -e
"$of" "$run" reconstructPar reconstructPar -latestTime
python3 "$here/tmr-convergence.py" "$run" > "$run/convergence.txt"
cat "$run/convergence.txt"
exit $solve
