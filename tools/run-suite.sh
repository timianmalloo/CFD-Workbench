#!/usr/bin/env bash
# Inner-loop wrapper: a whole harness or mode (`--views`, a Core part, Analysis) takes one ring slot from
# tools/ring-lock.sh, so single-suite runs and rings together stay at CFD_RING_MAX (default 2) on this machine.
# Measured 2026-10-05: unlocked single-suite runs from several agents drove the 1-minute load to 100-200.
#
#   tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --views
#   CFD_TEST_ONLY=<prefix> tools/run-suite.sh dotnet <dll>      (one check, 0.6-2 s: no slot, no wait)
#
# The wait is bounded (CFD_SUITE_LOCK_WAIT_SECONDS, default 120 s, then the run proceeds and prints RING-LOCK-TIMEOUT), and
# the command's own exit status is returned. tools/run-tests.sh does not use this wrapper: it holds its own slot.
#   tools/run-suite.sh --self-test      (no suite is run; fake holders are `sleep` processes; ring: every join; cost: ~5 s)
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd -P)"
# shellcheck source=tools/ring-lock.sh
. "$root/tools/ring-lock.sh"

run_suite() {
  if [ -n "${CFD_TEST_ONLY:-}" ]; then "$@"; return $?; fi
  CFD_RING_LOCK_WAIT_SECONDS="${CFD_SUITE_LOCK_WAIT_SECONDS:-120}" ring_lock_acquire "$$"
  local status=0
  "$@" || status=$?
  ring_lock_release
  return "$status"
}

self_test() {
  local scratch fails=0 total=0 out holder1 holder2 status
  scratch=$(mktemp -d "${TMPDIR:-/tmp}/run-suite-test.XXXXXX")
  export CFD_RING_SLOTS_DIR="$scratch/slots" CFD_RING_MAX=2 CFD_RING_POLL_SECONDS=1
  mkdir -p "$CFD_RING_SLOTS_DIR"
  note() { total=$((total + 1)); if [ "$1" = ok ]; then echo "SELFTEST PASS $2"; else echo "SELFTEST FAIL $2: $3"; fails=$((fails + 1)); fi; }

  # 1. a whole-suite run takes a slot, returns the command's status, and frees the slot
  status=0; out=$(run_suite sh -c 'exit 7') || status=$?
  case "$out" in *"RING-LOCK waited 0 s"*) note ok "a suite run takes a slot";; *) note bad "a suite run takes a slot" "$out";; esac
  [ "$status" -eq 7 ] && note ok "the command's exit status is returned" || note bad "the command's exit status is returned" "$status"
  [ -z "$(ls "$CFD_RING_SLOTS_DIR")" ] && note ok "the slot is released after the run" || note bad "the slot is released after the run" "$(ls "$CFD_RING_SLOTS_DIR")"

  # 2. a narrowed run (CFD_TEST_ONLY) neither waits nor takes a slot, even with both slots held
  sleep 30 & holder1=$!
  sleep 30 & holder2=$!
  mkdir "$CFD_RING_SLOTS_DIR/slot-1" "$CFD_RING_SLOTS_DIR/slot-2"
  echo "$holder1" > "$CFD_RING_SLOTS_DIR/slot-1/pid"; echo "$holder2" > "$CFD_RING_SLOTS_DIR/slot-2/pid"
  out=$(CFD_TEST_ONLY=X run_suite echo ran)
  [ "$out" = "ran" ] && note ok "a CFD_TEST_ONLY run bypasses the lock" || note bad "a CFD_TEST_ONLY run bypasses the lock" "$out"

  # 3. with both slots held by live processes the wait is bounded, then the run proceeds with RING-LOCK-TIMEOUT
  out=$(CFD_SUITE_LOCK_WAIT_SECONDS=2 run_suite echo ran)
  case "$out" in *"RING-LOCK-TIMEOUT waited 2 s"*ran*) note ok "a held pair of slots times out, then the run proceeds";; *) note bad "a held pair of slots times out, then the run proceeds" "$out";; esac
  [ -d "$CFD_RING_SLOTS_DIR/slot-1" ] && [ -d "$CFD_RING_SLOTS_DIR/slot-2" ] && note ok "a timed-out run leaves the holders' slots alone" \
    || note bad "a timed-out run leaves the holders' slots alone" "slot removed"
  kill "$holder1" "$holder2" 2>/dev/null || true
  wait 2>/dev/null || true
  rm -rf "$scratch"
  echo "SELFTEST $((total - fails))/$total cases"
  [ "$fails" -eq 0 ]
}

if [ "${1:-}" = "--self-test" ]; then self_test; exit $?; fi
if [ "$#" -eq 0 ]; then echo "usage: tools/run-suite.sh <command...>   (or --self-test)" >&2; exit 2; fi
run_suite "$@"
