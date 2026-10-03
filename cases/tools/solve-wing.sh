#!/bin/bash
# Short simpleFoam run on a meshed SPIKE-03 wing (after mesh-wing.sh and the layer-coverage read).
# Usage: solve-wing.sh <run-dir> <n-procs ≤ 6>
set -euo pipefail
run="$1"; np="$2"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
[ -d "$run/0" ] && { echo "refusing: $run/0 exists" >&2; exit 2; }
cp -R "$run/0.orig" "$run/0"
"$of" "$run" decomposePar-solve decomposePar -force
"$of" "$run" simpleFoam mpirun -np "$np" simpleFoam -parallel
"$of" "$run" reconstructPar reconstructPar -latestTime
