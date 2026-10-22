#!/usr/bin/env bash
# Reproduces the W-2 B2 evidence on the machine that runs it: build, then run the Windows store checks of the Core harness
# and print the measured lines (environment, Ruling 145 cases, the crash-injection inventory, the layouts).
#
#   bash docs/proof/win-store-implementation/2026-10-22/reproduce.sh [<output file>]
#
# The whole suite is `bash tools/run-tests.sh` (TESTS-GREEN, 0 skipped for Core and Cli). Nothing here writes into the tree:
# the harness temp directory is under .tmp-tests, which is not tracked.
set -uo pipefail
root="$(cd "$(dirname "$0")/../../../.." && pwd -P)"
cd "$root"
out="${1:-/dev/stdout}"
dotnet build tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj -c Release -nologo -v q || exit 1
mkdir -p .tmp-tests/tmp
export TMPDIR="$root/.tmp-tests/tmp" TMP="$root/.tmp-tests/tmp" TEMP="$root/.tmp-tests/tmp"
CFD_TEST_ONLY=WindowsNative_,WindowsStore_ tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.exe --harness > "$out"
status=$?
echo "harness exit $status"
exit $status
