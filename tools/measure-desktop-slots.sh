#!/usr/bin/env bash
# Calibrate the Desktop harness's child-slot count for THIS host (track DTJ). For each slot count N it runs the whole
# Desktop harness REPS times (default 3) with CFD_DESKTOP_SLOTS=N and records wall time, TIMEOUT count, DRAIN count,
# the 1-minute load and the harness exit code. It changes no cap, budget or timeout and edits no baseline: it prints the
# N with the lowest median wall and zero TIMEOUTs, and you commit limit.desktop_slots yourself (docs/plans/test-cost.md §9.11).
#
#   tools/measure-desktop-slots.sh                 N = 2 4 6 8, 3 reps      (Git Bash on Windows, bash on macOS)
#   tools/measure-desktop-slots.sh 8 10            N = 8 and 10
#   REPS=1 CFD_MEASURE_OUT=/some/smoke.csv tools/measure-desktop-slots.sh 8 10      (a smoke run; not a calibration)
#
# Output: docs/proof/ring-<host>/desktop-slots.csv (host key from tools/check-test-costs.py --resolve-host, so CFD_RING_HOST
# applies), header slots,rep,wall_ms,timeouts,drains,load1,exit. Run it on a quiet machine: other work distorts the walls,
# and the load column shows whether it was. The harness takes a ring-lock slot (tools/run-suite.sh) like any single-suite run.
# Exit 0 when a best N was found, 1 when every N timed out or failed, 2 on a bad argument.
set -uo pipefail
root="$(cd "$(dirname "$0")/.." && pwd -P)"
# shellcheck source=tools/py-resolve.sh
. "$root/tools/py-resolve.sh"
cd "$root" || exit 2

reps="${REPS:-3}"
case "$reps" in ''|*[!0-9]*|0) echo "REPS=$reps is refused: use a positive whole number" >&2; exit 2 ;; esac
slot_list=("$@")
if [ "${#slot_list[@]}" -eq 0 ]; then slot_list=(2 4 6 8); fi
for n in "${slot_list[@]}"; do
  case "$n" in ''|*[!0-9]*|0) echo "slot count '$n' is refused: use a positive whole number" >&2; exit 2 ;; esac
done

now_ms() { py -c 'import time; print(time.time_ns() // 1000000)'; }
load() {
  if [ -r /proc/loadavg ]; then cut -d' ' -f1 /proc/loadavg
  elif sysctl -n vm.loadavg >/dev/null 2>&1; then sysctl -n vm.loadavg | tr -d '{}' | awk '{print $1}'
  else echo "not-recorded"; fi
}

host=$(py "$root/tools/check-test-costs.py" --resolve-host "$(hostname | cut -d. -f1)") || exit 2
out="${CFD_MEASURE_OUT:-$root/docs/proof/ring-$host/desktop-slots.csv}"
mkdir -p "$(dirname "$out")"
scratch=$(mktemp -d "${TMPDIR:-/tmp}/desktop-slots.XXXXXX")
trap 'rm -rf "$scratch"' EXIT
project="tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj"
unset CFD_TEST_ONLY CFD_DESKTOP_SLOTS_BASELINE

echo "host $host; slot counts ${slot_list[*]}; $reps rep(s) each; output $out"
build_status=0
dotnet build "$project" -c Release -nologo -v q || build_status=$?
if [ "$build_status" -ne 0 ]; then echo "build failed ($build_status)" >&2; exit 1; fi

echo "slots,rep,wall_ms,timeouts,drains,load1,exit" > "$out"
for n in "${slot_list[@]}"; do
  rep=1
  while [ "$rep" -le "$reps" ]; do
    log="$scratch/slots$n-rep$rep.log"
    load_at=$(load)
    start_ms=$(now_ms)
    status=0
    CFD_DESKTOP_SLOTS="$n" "$root/tools/run-suite.sh" dotnet run -c Release --no-build --project "$project" > "$log" 2>&1 || status=$?
    wall_ms=$(( $(now_ms) - start_ms ))
    timeouts=$(grep -c '^FAIL TIMEOUT' "$log" || true)
    drains=$(grep -c '^FAIL DRAIN' "$log" || true)
    echo "$n,$rep,$wall_ms,$timeouts,$drains,$load_at,$status" >> "$out"
    echo "slots $n rep $rep: $wall_ms ms, $timeouts TIMEOUT, $drains DRAIN, load $load_at, exit $status"
    rep=$((rep + 1))
  done
done

# The best N: every rep exited 0 with zero TIMEOUTs, then the lowest median wall. Printed, never written to a baseline.
py "$root/tools/measure-desktop-slots-best.py" "$out"
