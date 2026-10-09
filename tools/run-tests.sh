#!/usr/bin/env bash
# Build the solution once, then run every console test harness in parallel with a non-symlinked TMPDIR.
# The store tests refuse symlinked paths by design, and macOS /var and /tmp are symlinks.
# The harnesses share no files: each store test makes its own GUID directory under TMPDIR, and
# each harness writes its own log. Build, per-suite and wall seconds and PASS counts print on every run.
#
# Exit 0 green · 1 a suite failed, or a named suite printed no PASS line (an exit 0 is not a result)
# · 3 green but over the wall budget at an end load <= 24 (TEST-BUDGET; CFD_TEST_BUDGET_SECONDS, default 60 s = 2x the
# measured 30 s; the serial Debug runner this replaced took 67 s). Above 24, or load not recorded, it prints
# TEST-BUDGET-MISS <wall> s load <value> and exits 0 (Ruling 87, DR-RING-2, the same gate as C-2..C-4).
# Before the build it takes a slot from tools/ring-lock.sh: at most 2 rings at once on this machine, across every
# worktree (Ruling 87 condition 3); it prints RING-LOCK waited <s> s, or RING-LOCK-TIMEOUT after 15 min.
set -euo pipefail
# Clear any inherited Core-harness test-subset/probe selectors so an exported one from a prior
# debugging session cannot silently narrow this run and still report green (bash 3.2-safe: no
# associative arrays, no `${!prefix@}`).
unset CFD_TEST_ONLY CFD_NATIVE_CAPABILITY_PROBE
for v in $(compgen -e | grep '^CFD_OWNER_STRIPPING_'); do unset "$v"; done
# B2 (docs/plans/test-cost.md 9.5): dynamic PGO instruments every tier-0 method and re-JITs it, work a harness that lives
# 5-50 s never earns back. Off, the ring used 476-487 CPU-s against 553-568 (paired runs, same tree), wall 3-6 s shorter.
export DOTNET_TieredPGO=0
root="$(cd "$(dirname "$0")/.." && pwd -P)"
scratch="$root/.tmp-tests"
mkdir -p "$scratch"
export TMPDIR="$scratch/" TMP="$scratch" TEMP="$scratch"
cd "$root"
# Ruling 87 (3): cap concurrent rings at 2. The wait is before `started`, so it is never counted as ring wall time.
# shellcheck source=tools/py-resolve.sh
. "$root/tools/py-resolve.sh"   # py: a working python3, else py -3, else python (the Windows Store alias is not Python)
# shellcheck source=tools/ring-lock.sh
. "$root/tools/ring-lock.sh"
ring_lock_acquire "$$"
trap ring_lock_release EXIT
# Release: the shipped configuration. Measured 2026-09-27: Core suite 40 s Debug, 27 s Release.
# A certificate-precision display-sampling mutant (BUDGET-DISPLAY) is red in both configurations.
configuration="${CFD_TEST_CONFIGURATION:-Release}"
named=" Core Desktop Analysis "   # suites that print PASS <name>; an exit 0 with no PASS line fails
# Core (43 s alone, one core) runs as three interleaved parts (`--part=k/n`; B2: with two, part 2 held the heavier checks and
# ran 47 s against part 1's 33 s, and under a concurrent build it outlasted Desktop), so it is not the critical path
# (docs/reviews/test-ci-waste.md §12). Longest first. A part's log is <project>.part<k>of<n>.log.
# Analysis (A3a, design area3-analysis.md §18.2 PRE): its own job, concurrent with the others, never the critical path. B4:
# it runs as two parts too (`--part=k/n`, whole test classes, longest first onto the lighter part), so its wall does not grow
# with every track (ANALYSIS-HARNESS-GROWTH); C-2 limits each part to 5 s.
jobs=("Core 1/3" "Core 2/3" "Core 3/3" "Desktop" "Analysis 1/2" "Analysis 2/2" "Cli")
# A log left by an earlier layout (e.g. Core.log before the split) would feed old PASS lines to
# tools/check-named-tests.py, which reads every .tmp-tests/*.log.
rm -f "$scratch"/*.log "$scratch"/*.seconds "$scratch"/*.ms
# C-1 (docs/design/area3-analysis.md 13.4): a millisecond wall clock, one clock for every process. Bash 3.2 on macOS has
# no EPOCHREALTIME and SECONDS counts whole seconds (a 5 s limit read from it lets 5.9 s pass).
now_ms() { py -c 'import time; print(time.time_ns() // 1000000)'; }
# The 1-minute load average, so a TEST-BUDGET red can be told from contention (test-cost F-3); "not recorded"
# where neither source exists, never a guess.
load() {
  if [ -r /proc/loadavg ]; then cut -d' ' -f1 /proc/loadavg
  elif sysctl -n vm.loadavg >/dev/null 2>&1; then sysctl -n vm.loadavg | tr -d '{}' | awk '{print $1}'
  else echo "not-recorded"; fi
}
# The load source, passed to tools/check-test-costs.py (Ruling 139): /proc/loadavg under Git Bash or Cygwin is "proc-gitbash",
# an uncalibrated host (its cost rules and TEST-BUDGET are advisory until docs/proof/ring-<host>/baseline.csv exists).
load_source() {
  if [ -r /proc/loadavg ]; then
    case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) echo "proc-gitbash" ;; *) echo "proc" ;; esac
  elif sysctl -n vm.loadavg >/dev/null 2>&1; then echo "sysctl"
  else echo "none"; fi
}
ring_load_source=$(load_source)
# Rulings 152 (2) and 154 (3): on a Windows host only, a harness whose failures are all listed in
# tests/expected-failures.windows.json counts as passed, and a listed test that passes fails the ring (tools/check-expected-failures.py).
# On macOS nothing here runs and a listed test failing is a real failure.
ring_windows=0
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) ring_windows=1 ;; esac
machine_host=$(hostname | cut -d. -f1)
# Ruling 168 (3): CFD_RING_HOST (validated [a-z0-9-]{1,32}) replaces the hostname as the baseline key; a refusal stops the ring.
ring_host=$(py "$root/tools/check-test-costs.py" --resolve-host "$machine_host") || exit 2
# Ruling 176 (2): the wall budget is CFD_TEST_BUDGET_SECONDS, else the host baseline's limit.budget, else 60 s.
budget=$(py "$root/tools/check-test-costs.py" --resolve-budget "$ring_load_source" "$ring_host") || exit 2
load_start=$(load)
started=$SECONDS
started_ms=$(now_ms)
build_start_ms=$(now_ms)
dotnet build CFDWorkbench.slnx -c "$configuration" -nologo -v q
echo "$(( $(now_ms) - build_start_ms ))" > "$scratch/build.ms"   # C-3 reads net ring time, wall - build (Ruling 84)
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
    # C-2 reads the Analysis wall clock and the first 2-3 s of every other job is a JIT surge. The long jobs wait 2 s so the
    # 4 s Analysis harness is not queued behind it; the wait is before their own clock starts, so C-4 is not charged (B2).
    if [ "$project" != "Analysis" ] && [ "$project" != "Cli" ]; then sleep 2; fi
    suite_start=$SECONDS
    suite_start_ms=$(now_ms)
    status=0
    args=()
    if [ -n "$part" ]; then args=(-- "--part=$part"); fi
    dotnet run -c "$configuration" --no-build --project "tests/CfdWorkbench.$project.Tests/CfdWorkbench.$project.Tests.csproj" \
      ${args[@]+"${args[@]}"} > "$scratch/$name.log" 2>&1 || status=$?
    echo "$(( $(now_ms) - suite_start_ms ))" > "$scratch/$name.ms"
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
  expected_n=0
  if [ "$ring_windows" -eq 1 ]; then
    verdict=0
    classified=$(py "$root/tools/check-expected-failures.py" --log "$scratch/$name.log" --status "$status" --host windows) || verdict=$?
    if [ "$verdict" -eq 0 ]; then
      expected_n=$(printf '%s\n' "$classified" | sed -n 's/^EXPECTED-FAIL \([0-9]*\) (manifest)$/\1/p')
      status=0   # every failure is listed; the exit of this harness is explained
    else
      printf '%s\n' "$classified" | grep '^UNEXPECTED' || true
      if [ "$status" -eq 0 ]; then echo "FAILED: $label (a listed test passed: stale manifest entry)"; failed=1; fi
    fi
  fi
  passes=$(grep -c '^PASS ' "$scratch/$name.log" || true)
  echo "== $label $(cat "$scratch/$name.seconds") s ($(cat "$scratch/$name.ms") ms), $passes PASS"
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
    if [ "$expected_n" -gt 0 ]; then echo "EXPECTED-FAIL $expected_n (manifest)"; fi
  fi
done
# The parts run each check once only if the jobs hold parts 1..n of one n and every part enumerated the same registrations
# (Core: checks; Analysis: test classes): one PARTITION line per part, one count. Otherwise a check could drop out silently.
check_parts() {
  local proj="$1" expected="" job reported counts total complete
  for job in "${jobs[@]}"; do
    if [[ "$job" == "$proj "* ]]; then expected="$expected${job#* }"$'\n'; fi
  done
  reported=$(cat "$scratch"/"$proj".part*.log | grep '^PARTITION ' | cut -d' ' -f2 | sort || true)
  counts=$( (cat "$scratch"/"$proj".part*.log | grep '^PARTITION ' || true) | sed 's/.* of //' | sort -u | wc -l | tr -d ' ')
  total=$(printf '%s' "$expected" | grep -c . || true)
  complete=$(seq 1 "$total" | sed "s|\$|/$total|" | sort)
  if [ "$reported" != "$(printf '%s' "$expected" | sort)" ] || [ "$reported" != "$complete" ] || [ "$counts" -ne 1 ]; then
    echo "FAILED: $proj parts are incomplete or enumerated different checks:"
    grep -H '^PARTITION ' "$scratch"/"$proj".part*.log || true
    failed=1
  fi
}
check_parts Core
check_parts Analysis
wall=$((SECONDS - started))
echo "$(( $(now_ms) - started_ms ))" > "$scratch/wall.ms"
# CPU-seconds (user + sys) of every finished child: the work done, which load does not inflate the way it does wall.
# `times` must run in this shell (a pipe or $(...) would report a subshell), so it writes a file first.
times > "$scratch/times.txt"
cpu=$(tail -1 "$scratch/times.txt" | awk '{ total = 0; for (i = 1; i <= 2; i++) { split($i, t, "m"); total += t[1] * 60 + t[2] } printf "%.0f", total }')
# C-2..C-6: the cost rules, from the millisecond clocks and the Analysis COST lines (tools/check-test-costs.py).
cost_jobs=""
for name in "${names[@]}"; do cost_jobs="$cost_jobs${cost_jobs:+,}$name"; done
# The end load is read first: C-3 and C-4 fail only at a quiet end load (Ruling 84).
load_end=$(load)
if ! py "$root/tools/check-test-costs.py" --dir "$scratch" --jobs "$cost_jobs" --load "$load_end" --load-source "$ring_load_source" --host "$ring_host"; then failed=1; fi
echo "wall $wall s ($(cat "$scratch/wall.ms") ms, net $(( $(cat "$scratch/wall.ms") - $(cat "$scratch/build.ms") )) ms) (budget $budget s) cpu $cpu s load $load_start -> $load_end"
if [ "$failed" -ne 0 ]; then exit 1; fi
# TEST-BUDGET (Ruling 87): exit 3 only at an end load <= 24; above it a MISS line is printed and the run exits 0.
budget_status=0
py "$root/tools/check-test-costs.py" --budget "$wall" "$budget" "$load_end" --load-source "$ring_load_source" --host "$ring_host" || budget_status=$?
if [ "$budget_status" -ne 0 ]; then exit "$budget_status"; fi
echo "all test harnesses passed"
