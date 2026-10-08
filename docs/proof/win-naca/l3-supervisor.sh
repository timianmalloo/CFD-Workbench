#!/bin/bash
# No wall cap. Solver and A4 monitor persist independently of the calling Windows terminal.
set -euo pipefail
: "${CFDW_START_NOTE_REF:?Missing coordinator start-note gate}"
run="$1"
repo=/mnt/c/Projects/CFD-Workbench-win-w4-validation
here="$repo/docs/proof/win-naca"
printf 'start-note=%s supervisor_pid=%s utc=%s\n' "$CFDW_START_NOTE_REF" "$$" "$(date -u +%FT%TZ)" >> "$run/run-ledger.txt"
python3 "$repo/cases/tools/launcher-record.py" init "$run"
for app in checkMesh decomposePar; do
  args=(); if test "$app" = decomposePar; then args=(-force); fi
  env -i PATH=/usr/bin:/bin HOME=/root USER=root LOGNAME=root LANG=C bash --noprofile --norc "$here/of-command-l3.sh" "$run" "$app" "$app" "${args[@]}"
done
python3 "$here/a4-monitor-linux.py" watch "$run" simpleFoam "$repo/cases/win-spike04r3-g2-l3.yaml" > "$run/a4-monitor.log" 2>&1 &
monitor=$!
set +e
env -i PATH=/usr/bin:/bin HOME=/root USER=root LOGNAME=root LANG=C bash --noprofile --norc "$here/of-command-l3.sh" "$run" simpleFoam simpleFoam
status=$?
wait "$monitor"; monitor_status=$?
set -e
printf 'solver_exit=%s monitor_exit=%s utc=%s\n' "$status" "$monitor_status" "$(date -u +%FT%TZ)" >> "$run/run-ledger.txt"
if test "$status" != 0; then exit "$status"; fi
env -i PATH=/usr/bin:/bin HOME=/root USER=root LOGNAME=root LANG=C bash --noprofile --norc "$here/of-command-l3.sh" "$run" reconstructPar reconstructPar -latestTime
python3 "$here/a4-monitor-linux.py" report "$run" simpleFoam "$repo/cases/win-spike04r3-g2-l3.yaml" > "$run/convergence.txt"
cat "$run/convergence.txt"
