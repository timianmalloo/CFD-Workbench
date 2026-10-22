#!/usr/bin/env bash
# run-tests.sh - build once, run every test project with dotnet test, report. Nothing else.
#
#   bash tools/run-tests.sh [Release|Debug] [--no-desktop]
#
# The four C# test projects are xunit v3 assemblies around console harnesses (tests/HarnessTests.cs): `dotnet test` runs each
# harness once and reports every PASS/FAIL/SKIP line as one test, plus the harness's exit code. The projects run in parallel
# after one build, each into its own log under .tmp-tests/. A project that passes without one succeeded test fails: an exit
# code is not a result. Python suites under tests/test_*.py run afterwards when any exist. Exit 0 when everything passed,
# 1 otherwise.
#
# TMPDIR is set to a directory under the repository because the store tests refuse symlinked paths and macOS /tmp is one.
set -uo pipefail
root="$(cd "$(dirname "$0")/.." && pwd -P)"
cd "$root"
configuration=Release
no_desktop=0
for arg in "$@"; do
  case "$arg" in
    Release|Debug) configuration=$arg ;;
    --no-desktop) no_desktop=1 ;;   # the Desktop harness samples pixels, focus and window geometry: it needs a real display
    *) echo "usage: run-tests.sh [Release|Debug] [--no-desktop]"; exit 64 ;;
  esac
done
. "$root/tools/py-resolve.sh"   # py: python3, else py -3, else python
scratch="$root/.tmp-tests"
rm -rf "$scratch"
mkdir -p "$scratch/tmp"
export TMPDIR="$scratch/tmp" TMP="$scratch/tmp" TEMP="$scratch/tmp"   # .NET on Windows reads TMP/TEMP, not TMPDIR
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_TieredPGO=0
# Clear any inherited test-subset or probe selectors so a prior shell cannot narrow or redirect a harness.
unset CFD_TEST_ONLY CFD_SF2_TIMING CFD_NATIVE_CAPABILITY_PROBE CFD_DESKTOP_SLOTS_BASELINE 2>/dev/null || true
for v in $(compgen -e | grep '^CFD_OWNER_STRIPPING_'); do unset "$v"; done

started=$SECONDS
if ! dotnet build CFDWorkbench.slnx -c "$configuration" -nologo -v q; then
  echo "TESTS-RED build failed"
  exit 1
fi
echo "build $((SECONDS - started)) s ($configuration)"

projects=(Core Analysis Desktop Cli)
if [ "$no_desktop" -eq 1 ]; then projects=(Core Analysis Cli); echo "== CfdWorkbench.Desktop.Tests: not run (--no-desktop: needs a display)"; fi
pids=()
for project in "${projects[@]}"; do
  (
    dotnet test "tests/CfdWorkbench.$project.Tests/CfdWorkbench.$project.Tests.csproj" -c "$configuration" --no-build \
      > "$scratch/$project.log" 2>&1
  ) &
  pids+=("$!")
done

# The run summary dotnet test prints: "  total: N", "  failed: N", "  succeeded: N", "  skipped: N" on their own lines here, or
# on one "Test summary: total: N, failed: N, …" line; the key may follow a colour code, so the codes and CRs are dropped first.
count() {
  tr -d '\r' < "$1" | sed "s/$(printf '\033')\[[0-9;]*[A-Za-z]//g" |
    sed -n "s/^\(.*[^A-Za-z]\)\{0,1\}$2: *\([0-9][0-9]*\).*/\2/p" | tail -1
}
failed=0
for index in "${!projects[@]}"; do
  project="${projects[$index]}"
  status=0
  wait "${pids[$index]}" || status=$?
  log="$scratch/$project.log"
  succeeded=$(count "$log" succeeded); skipped=$(count "$log" skipped); failures=$(count "$log" failed)
  echo "== CfdWorkbench.$project.Tests: ${succeeded:-0} passed, ${skipped:-0} skipped, ${failures:-0} failed, exit $status"
  if [ "$status" -ne 0 ]; then
    grep -E '^(failed|error) ' "$log" || true
    tail -30 "$log"
    echo "FAILED: CfdWorkbench.$project.Tests (exit $status)"
    failed=1
  elif [ "${succeeded:-0}" -eq 0 ]; then
    tail -40 "$log"   # what dotnet test printed instead of a summary with succeeded tests
    echo "FAILED: CfdWorkbench.$project.Tests exited 0 but reported no succeeded test"
    failed=1
  fi
done

if ls "$root"/tests/test_*.py >/dev/null 2>&1; then
  py_status=0
  py -m unittest discover -s tests -p 'test_*.py' > "$scratch/python-unittest.log" 2>&1 || py_status=$?
  ran=$(sed -n 's/^Ran \([0-9]*\) tests\{0,1\} in .*/\1/p' "$scratch/python-unittest.log" | tail -1)
  echo "== Python unittest: ${ran:-0} ran, exit $py_status"
  if [ "$py_status" -ne 0 ] || [ "${ran:-0}" -eq 0 ]; then
    tail -30 "$scratch/python-unittest.log"
    echo "FAILED: Python unittest"
    failed=1
  fi
fi

if [ "$failed" -eq 0 ]; then
  echo "TESTS-GREEN $((SECONDS - started)) s"
else
  echo "TESTS-RED $((SECONDS - started)) s"
fi
exit "$failed"
