#!/bin/bash
# Run one OpenFOAM command line inside the OpenFOAM-v2512.app environment, niced, after the host load settles.
# Usage: of-run.sh <case-dir> <log-name> <command...>
# Resource rules (Ruling 60 + Coordinator 2026-10-03): nice -n 10; wait while the 1-minute load average is above 10;
# wait while the Coordinator's join lock exists (a join or readiness run is in progress); one job at a time.
set -euo pipefail
case_dir="$1"; log_name="$2"; shift 2
max_load=10
join_lock="${CFDW_JOIN_LOCK:-/private/tmp/claude-501/-Users-mallalieut-projects-CFD-Workbench/f19a2b12-f8df-4dcc-bc84-7353cfbcda0f/scratchpad/join.lock}"
waited=0
while :; do
  load=$(sysctl -n vm.loadavg | awk '{print $2}')
  if [ ! -e "$join_lock" ] && awk -v l="$load" -v m="$max_load" 'BEGIN{exit !(l<=m)}'; then break; fi
  if [ -e "$join_lock" ]; then reason="join lock present"; else reason="load $load > $max_load"; fi
  echo "of-run: $reason; waiting (${waited}s)" >&2
  echo "of-run: wait $(date -u +%Y-%m-%dT%H:%M:%SZ) $reason" >> "$case_dir/run-ledger.txt"
  sleep 30; waited=$((waited+30))
  if [ "$waited" -ge 3600 ]; then echo "of-run: not clear after 60 min" >&2; exit 75; fi
done
echo "of-run: start $(date -u +%Y-%m-%dT%H:%M:%SZ) load1=$load cmd=$*" >> "$case_dir/run-ledger.txt"
start=$(date +%s)
set +e
( cd "$case_dir" && nice -n 10 /Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam -c "$*" ) > "$case_dir/log.$log_name" 2>&1
status=$?
set -e
end=$(date +%s)
load_end=$(sysctl -n vm.loadavg | awk '{print $2}')
echo "of-run: end status=$status wall_s=$((end-start)) load1=$load_end cmd=$*" >> "$case_dir/run-ledger.txt"
echo "status=$status wall_s=$((end-start)) log=$case_dir/log.$log_name"
exit $status
