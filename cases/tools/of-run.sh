#!/bin/bash
# Run one OpenFOAM command line inside the OpenFOAM-v2512.app environment, niced, after the host load settles.
# Usage: of-run.sh <case-dir> <log-name> <command...>
# Resource rule (Ruling 60): nice -n 10; wait while the 1-minute load average is above 10; one job at a time.
set -euo pipefail
case_dir="$1"; log_name="$2"; shift 2
max_load=10
waited=0
while :; do
  load=$(sysctl -n vm.loadavg | awk '{print $2}')
  if awk -v l="$load" -v m="$max_load" 'BEGIN{exit !(l<=m)}'; then break; fi
  echo "of-run: load $load > $max_load; waiting (${waited}s)" >&2
  sleep 30; waited=$((waited+30))
  if [ "$waited" -ge 1800 ]; then echo "of-run: load did not settle in 30 min" >&2; exit 75; fi
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
