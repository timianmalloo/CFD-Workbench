#!/bin/bash
# Pre-flight, solve under the A4 monitor, and summarise one generated SPIKE-04 TMR case (round 2).
# Usage: solve-tmr.sh <run-dir> <n-procs ≤ 6> <case.yaml>
# Order is fixed by the launcher: lint -> checkMesh (pre-flight "Disallowing" banner) -> solver.
set -euo pipefail
run="$1"; np="$2"; yaml="$3"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
solver=$(sed -n 's/^  application: \([A-Za-z]*\)$/\1/p' "$yaml")
abs_run="$(cd "$run" && pwd)"
"$of" "$run" checkMesh checkMesh
"$of" "$run" decomposePar decomposePar -force
"$of" "$run" "$solver" mpirun -np "$np" "$solver" -parallel -case "$abs_run" &
solve_pid=$!
uv run --quiet --with pyyaml python3 "$here/a4-monitor.py" watch "$run" "$solver" "$yaml" > "$run/a4-monitor.log" 2>&1 &
mon_pid=$!
set +e
wait "$solve_pid"
solve=$?
wait "$mon_pid"
set -e
"$of" "$run" reconstructPar reconstructPar -latestTime
uv run --quiet --with pyyaml python3 "$here/a4-monitor.py" report "$run" "$solver" "$yaml" > "$run/convergence.txt"
cat "$run/convergence.txt"
exit $solve
