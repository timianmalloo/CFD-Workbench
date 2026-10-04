#!/usr/bin/env bash
# Build the solution once, then run every console test harness in parallel with a non-symlinked TMPDIR.
# The store tests refuse symlinked paths by design, and macOS /var and /tmp are symlinks.
# The harnesses share no files: each store test makes its own GUID directory under TMPDIR, and
# each harness writes its own log. Build, per-suite and wall seconds and PASS counts print on every run.
#
# Exit 0 green · 1 a suite failed, or a named suite printed no PASS line (an exit 0 is not a result)
# · 3 green but over the wall budget (TEST-BUDGET; CFD_TEST_BUDGET_SECONDS, default 60 s = 2x the
# measured 30 s; the serial Debug runner this replaced took 67 s).
set -euo pipefail
# Clear any inherited Core-harness test-subset/probe selectors so an exported one from a prior
# debugging session cannot silently narrow this run and still report green (bash 3.2-safe: no
# associative arrays, no `${!prefix@}`).
unset CFD_TEST_ONLY CFD_NATIVE_CAPABILITY_PROBE
for v in $(compgen -e | grep '^CFD_OWNER_STRIPPING_'); do unset "$v"; done
root="$(cd "$(dirname "$0")/.." && pwd -P)"
scratch="$root/.tmp-tests"
mkdir -p "$scratch"
export TMPDIR="$scratch/" TMP="$scratch" TEMP="$scratch"
cd "$root"
# Release: the shipped configuration. Measured 2026-09-27: Core suite 40 s Debug, 27 s Release.
# A certificate-precision display-sampling mutant (BUDGET-DISPLAY) is red in both configurations.
configuration="${CFD_TEST_CONFIGURATION:-Release}"
budget="${CFD_TEST_BUDGET_SECONDS:-60}"
named=" Core Desktop "   # suites that print PASS <name>; add Desktop when its Check helper lands
# Core (43 s alone, one core) runs as two interleaved parts (`--part=k/n`), so it is no longer the critical path
# (docs/reviews/test-ci-waste.md §12). Longest first. A part's log is <project>.part<k>of<n>.log.
jobs=("Core 1/2" "Core 2/2" "Desktop" "Cli")
# A log left by an earlier layout (e.g. Core.log before the split) would feed old PASS lines to
# tools/check-named-tests.py, which reads every .tmp-tests/*.log.
rm -f "$scratch"/*.log "$scratch"/*.seconds
# The 1-minute load average, so a TEST-BUDGET red can be told from contention (test-cost F-3); "not recorded"
# where neither source exists, never a guess.
load() {
  if [ -r /proc/loadavg ]; then cut -d' ' -f1 /proc/loadavg
  elif sysctl -n vm.loadavg >/dev/null 2>&1; then sysctl -n vm.loadavg | tr -d '{}' | awk '{print $1}'
  else echo "not-recorded"; fi
}
load_start=$(load)
started=$SECONDS
dotnet build CFDWorkbench.slnx -c "$configuration" -nologo -v q
echo "build $((SECONDS - started)) s ($configuration)"
pids=()
names=()
for job in "${jobs[@]}"; do
  project="${job%% *}"
  part=""
  name="$project"
  if [ "$job" != "$project" ]; then part="${job#* }"; name="$project.part${part/\//of}"; fi
  names+=("$name")
  (
    suite_start=$SECONDS
    status=0
    args=()
    if [ -n "$part" ]; then args=(-- "--part=$part"); fi
    dotnet run -c "$configuration" --no-build --project "tests/CfdWorkbench.$project.Tests/CfdWorkbench.$project.Tests.csproj" \
      ${args[@]+"${args[@]}"} > "$scratch/$name.log" 2>&1 || status=$?
    echo "$((SECONDS - suite_start))" > "$scratch/$name.seconds"
    exit "$status"
  ) &
  pids+=("$!")
done
failed=0
for index in "${!jobs[@]}"; do
  project="${jobs[$index]%% *}"
  name="${names[$index]}"
  label="CfdWorkbench.$project.Tests"
  if [ "$name" != "$project" ]; then label="$label ${jobs[$index]#* }"; fi
  status=0
  wait "${pids[$index]}" || status=$?
  passes=$(grep -c '^PASS ' "$scratch/$name.log" || true)
  echo "== $label $(cat "$scratch/$name.seconds") s, $passes PASS"
  if [ "$status" -eq 0 ] && [ "$passes" -eq 0 ] && [[ "$named" == *" $project "* ]]; then
    echo "FAILED: $label exited 0 but printed no PASS line"
    failed=1
  elif [ "$status" -ne 0 ]; then
    grep '^FAIL' "$scratch/$name.log" || true
    tail -20 "$scratch/$name.log"
    echo "FAILED: $label (exit $status)"
    failed=1
  else
    tail -1 "$scratch/$name.log"
  fi
done
# The parts run each Core check once only if the jobs hold parts 1..n of one n and every part enumerated the
# same registrations: one PARTITION line per part, one count. Otherwise a check could drop out silently.
expected=""
for job in "${jobs[@]}"; do
  if [[ "$job" == "Core "* ]]; then expected="$expected${job#* }"$'\n'; fi
done
reported=$(cat "$scratch"/Core.part*.log | grep '^PARTITION ' | cut -d' ' -f2 | sort || true)
counts=$( (cat "$scratch"/Core.part*.log | grep '^PARTITION ' || true) | sed 's/.* of //' | sort -u | wc -l | tr -d ' ')
total=$(printf '%s' "$expected" | grep -c . || true)
complete=$(seq 1 "$total" | sed "s|\$|/$total|" | sort)
if [ "$reported" != "$(printf '%s' "$expected" | sort)" ] || [ "$reported" != "$complete" ] || [ "$counts" -ne 1 ]; then
  echo "FAILED: Core parts are incomplete or enumerated different checks:"
  grep -H '^PARTITION ' "$scratch"/Core.part*.log || true
  failed=1
fi
wall=$((SECONDS - started))
# CPU-seconds (user + sys) of every finished child: the work done, which load does not inflate the way it does wall.
# `times` must run in this shell (a pipe or $(...) would report a subshell), so it writes a file first.
times > "$scratch/times.txt"
cpu=$(tail -1 "$scratch/times.txt" | awk '{ total = 0; for (i = 1; i <= 2; i++) { split($i, t, "m"); total += t[1] * 60 + t[2] } printf "%.0f", total }')
echo "wall $wall s (budget $budget s) cpu $cpu s load $load_start -> $(load)"
if [ "$failed" -ne 0 ]; then exit 1; fi
if [ "$wall" -gt "$budget" ]; then
  echo "TEST-BUDGET: green, but $wall s is over the $budget s budget. Find the new cost before raising it (docs/reviews/test-ci-waste.md)."
  exit 3
fi
echo "all test harnesses passed"
