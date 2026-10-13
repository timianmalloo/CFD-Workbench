#!/usr/bin/env bash
# Calibrate the Desktop harness's child-slot count for THIS host (track DTJ). For each slot count N it runs the whole
# Desktop harness REPS times (default 3) with CFD_DESKTOP_SLOTS=N and records wall time, TIMEOUT count, DRAIN count,
# the 1-minute load, the harness's raw exit code and a classified verdict (on Windows the harness always exits 1 because of
# the listed expected failures; the verdict is `pass` when only those occurred, as tools/run-tests.sh classifies them). It changes no cap, budget or timeout and edits no baseline: it prints the
# N with the lowest median wall and zero TIMEOUTs, and you commit limit.desktop_slots yourself (docs/plans/test-cost.md §9.11).
#
#   tools/measure-desktop-slots.sh                 N = 2 4 6 8, 3 reps      (Git Bash on Windows, bash on macOS)
#   tools/measure-desktop-slots.sh 8 10            N = 8 and 10
#   REPS=1 CFD_MEASURE_OUT=/some/smoke.csv tools/measure-desktop-slots.sh 8 10      (a smoke run; not a calibration)
#
# Output: docs/proof/ring-<host>/desktop-slots.csv (host key from tools/check-test-costs.py --resolve-host, so CFD_RING_HOST
# applies), header slots,rep,wall_ms,timeouts,drains,load1,exit,verdict.
# `tools/measure-desktop-slots.sh --self-test` checks the classification and the best-N pick without a build. Run it on a quiet machine: other work distorts the walls,
# and the load column shows whether it was. The harness takes a ring-lock slot (tools/run-suite.sh) like any single-suite run.
# Exit 0 when a best N was found, 1 when every N timed out or failed, 2 on a bad argument, 3 (AT-EDGE) when the best is the
# largest N tested: the wall was still falling, so measure larger N before committing a count.
set -uo pipefail
root="$(cd "$(dirname "$0")/.." && pwd -P)"
# shellcheck source=tools/py-resolve.sh
. "$root/tools/py-resolve.sh"
cd "$root" || exit 2

# Same host test as tools/run-tests.sh: only a Windows (Git Bash) host has an expected-failure manifest.
ring_windows=0
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) ring_windows=1 ;; esac

# classify_log LOG STATUS sets VERDICT (pass | fail | unexpected-pass | interrupted) and UNEXPECTED_NAMES (the unlisted FAIL names, without
# TIMEOUT, DRAIN and child-exit lines). On Windows it makes the very call tools/run-tests.sh makes (check-expected-failures.py
# --log --status --host windows): a harness whose only failures are listed is a pass. Elsewhere the verdict is the raw exit;
# the checker is still read with --host other, only to name the failures.
classify_log() {
  local log="$1" status="$2" host=other out rc=0
  if [ "$ring_windows" -eq 1 ]; then host=windows; fi
  out=$(py "$root/tools/check-expected-failures.py" --log "$log" --status "$status" --host "$host") || rc=$?
  UNEXPECTED_NAMES=$(printf '%s\n' "$out" | sed -n 's/^UNEXPECTED \([^: ]*\):.*/\1/p' | grep -v -e '^TIMEOUT$' -e '^DRAIN$' -e '^--' | tr '\n' ' ' | sed 's/ $//' || true)
  # The harness exits 0 or 1. A status of 126 or more is a killed or unlaunchable child, never a test result: on the PC an
  # interrupted child exited 127 and its partial log classified as `pass` (PR #40).
  if [ "$status" -ge 126 ]; then
    VERDICT=interrupted
  elif [ "$ring_windows" -eq 0 ]; then
    if [ "$status" -eq 0 ]; then VERDICT=pass; else VERDICT=fail; fi
  elif [ "$rc" -eq 0 ]; then
    VERDICT=pass
  elif printf '%s\n' "$out" | grep -q '^UNEXPECTED-PASS ' && ! printf '%s\n' "$out" | grep -q -e '^UNEXPECTED [^0-9]'; then
    VERDICT=unexpected-pass
  else
    VERDICT=fail
  fi
}

# --self-test: classify synthetic logs as a Windows host would (a listed expected failure; an unlisted failure; a TIMEOUT; a
# stale entry), then the Mac rule, then pick a best N from a synthetic CSV. No build, no Desktop run (cost: under 1 s).
if [ "${1:-}" = "--self-test" ]; then
  ring_windows=1
  st=$(mktemp -d "${TMPDIR:-/tmp}/slots-selftest.XXXXXX")
  trap 'rm -rf "$st"' EXIT
  bad=0
  expect() { # label want_verdict want_unexpected_names
    if [ "$2" != "$VERDICT" ]; then echo "SELF-TEST FAIL: $1: verdict '$VERDICT', want '$2'"; bad=1; fi
    if [ "$3" != "$UNEXPECTED_NAMES" ]; then echo "SELF-TEST FAIL: $1: unexpected '$UNEXPECTED_NAMES', want '$3'"; bad=1; fi
  }
  printf 'PASS A_Check\nFAIL Library_Save_PublishesHashNamedFile ContractError: DOC-UNSUPPORTED-PERSISTENCE\nRESULT failures=1\n' > "$st/listed.log"
  classify_log "$st/listed.log" 1; expect "one listed failure" pass ""
  printf 'PASS A_Check\nFAIL Library_Save_PublishesHashNamedFile ContractError: DOC-UNSUPPORTED-PERSISTENCE\nFAIL Unlisted_Check Boom\n' > "$st/unlisted.log"
  classify_log "$st/unlisted.log" 1; expect "one unlisted failure" fail "Unlisted_Check"
  printf 'PASS A_Check\nFAIL TIMEOUT Desktop_Child_X 120 s\n' > "$st/timeout.log"
  classify_log "$st/timeout.log" 1; expect "a TIMEOUT fails but names nothing else" fail ""
  printf 'PASS Library_Save_PublishesHashNamedFile\n' > "$st/stale.log"
  classify_log "$st/stale.log" 0; expect "a listed test that passes" unexpected-pass ""
  ring_windows=0
  classify_log "$st/listed.log" 1; expect "Mac: the verdict follows the raw exit (fail)" fail "Library_Save_PublishesHashNamedFile"
  classify_log "$st/stale.log" 0; expect "Mac: the verdict follows the raw exit (pass)" pass ""
  ring_windows=1
  classify_log "$st/listed.log" 127; expect "a killed child (exit 127) with a passing partial log" interrupted ""
  ring_windows=0
  printf 'slots,rep,wall_ms,timeouts,drains,load1,exit,verdict\n2,1,100000,0,0,1.0,1,pass\n4,1,90000,0,0,1.0,1,fail\n6,1,50000,1,0,1.0,1,pass\n' > "$st/best.csv"
  best=$(py "$root/tools/measure-desktop-slots-best.py" "$st/best.csv") || true
  case "$best" in *"BEST 2 "*) ;; *) echo "SELF-TEST FAIL: best N from the verdict column, got: $best"; bad=1 ;; esac
  # The PC's first calibration (PR #40) tested 2-8, was still falling at 8, and proposed 8 below its untested default of 12.
  printf 'slots,rep,wall_ms,timeouts,drains,load1,exit,verdict\n2,1,300000,0,0,1.0,1,pass\n4,1,200000,0,0,1.0,1,pass\n' > "$st/edge.csv"
  edge_rc=0; edge=$(py "$root/tools/measure-desktop-slots-best.py" "$st/edge.csv") || edge_rc=$?
  case "$edge_rc:$edge" in 3:*"AT-EDGE 4 "*) ;; *) echo "SELF-TEST FAIL: a best N at the largest tested N must exit 3 with AT-EDGE, got $edge_rc: $edge"; bad=1 ;; esac
  if [ "$bad" -ne 0 ]; then exit 1; fi
  echo "measure-desktop-slots self-test ok: 7 classifications, 2 best-N picks"
  exit 0
fi

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
# An interrupt ends the calibration, not just the current child: without a trap bash runs on to the next N (PR #40).
trap 'echo "interrupted; stopping (rows so far in $out)" >&2; exit 130' INT TERM
project="tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj"
unset CFD_TEST_ONLY CFD_DESKTOP_SLOTS_BASELINE

echo "host $host; slot counts ${slot_list[*]}; $reps rep(s) each; output $out"
build_status=0
dotnet build "$project" -c Release -nologo -v q || build_status=$?
if [ "$build_status" -ne 0 ]; then echo "build failed ($build_status)" >&2; exit 1; fi

echo "slots,rep,wall_ms,timeouts,drains,load1,exit,verdict" > "$out"
first_sample=1
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
    classify_log "$log" "$status"
    echo "$n,$rep,$wall_ms,$timeouts,$drains,$load_at,$status,$VERDICT" >> "$out"
    echo "slots $n rep $rep: $wall_ms ms, $timeouts TIMEOUT, $drains DRAIN, load $load_at, exit $status, verdict $VERDICT"
    if [ "$VERDICT" = "interrupted" ]; then
      echo "the harness child was killed or failed to launch (exit $status); stopping (rows so far in $out)" >&2
      exit 130
    fi
    # The first sample doubles as a sanity check: a failure that is not a TIMEOUT or a DRAIN means a broken tree, so stop before
    # it costs the other runs.
    if [ "$first_sample" -eq 1 ] && [ "$VERDICT" != "pass" ] && { [ "$VERDICT" = "unexpected-pass" ] || [ -n "$UNEXPECTED_NAMES" ]; }; then
      echo "first sample is broken, not slow; stopping (log kept in $out): verdict $VERDICT; unexpected: ${UNEXPECTED_NAMES:-a listed test passed (stale manifest entry)}" >&2
      exit 1
    fi
    first_sample=0
    rep=$((rep + 1))
  done
done

# The best N: every rep has verdict pass (expected failures classified as in the ring) with zero TIMEOUTs, then the lowest median wall. Printed, never written to a baseline.
py "$root/tools/measure-desktop-slots-best.py" "$out"
