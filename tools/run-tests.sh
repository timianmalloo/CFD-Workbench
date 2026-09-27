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
started=$SECONDS
dotnet build CFDWorkbench.slnx -c "$configuration" -nologo -v q
echo "build $((SECONDS - started)) s ($configuration)"
projects=(Core Cli Desktop)
pids=()
for project in "${projects[@]}"; do
  (
    suite_start=$SECONDS
    status=0
    dotnet run -c "$configuration" --no-build --project "tests/CfdWorkbench.$project.Tests/CfdWorkbench.$project.Tests.csproj" \
      > "$scratch/$project.log" 2>&1 || status=$?
    echo "$((SECONDS - suite_start))" > "$scratch/$project.seconds"
    exit "$status"
  ) &
  pids+=("$!")
done
failed=0
for index in "${!projects[@]}"; do
  project="${projects[$index]}"
  status=0
  wait "${pids[$index]}" || status=$?
  passes=$(grep -c '^PASS ' "$scratch/$project.log" || true)
  echo "== CfdWorkbench.$project.Tests $(cat "$scratch/$project.seconds") s, $passes PASS"
  if [ "$status" -eq 0 ] && [ "$passes" -eq 0 ] && [[ "$named" == *" $project "* ]]; then
    echo "FAILED: CfdWorkbench.$project.Tests exited 0 but printed no PASS line"
    failed=1
  elif [ "$status" -ne 0 ]; then
    grep '^FAIL' "$scratch/$project.log" || true
    tail -20 "$scratch/$project.log"
    echo "FAILED: CfdWorkbench.$project.Tests (exit $status)"
    failed=1
  else
    tail -1 "$scratch/$project.log"
  fi
done
wall=$((SECONDS - started))
echo "wall $wall s (budget $budget s)"
if [ "$failed" -ne 0 ]; then exit 1; fi
if [ "$wall" -gt "$budget" ]; then
  echo "TEST-BUDGET: green, but $wall s is over the $budget s budget. Find the new cost before raising it (docs/reviews/test-ci-waste.md)."
  exit 3
fi
echo "all test harnesses passed"
