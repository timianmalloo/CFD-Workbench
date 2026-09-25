#!/usr/bin/env bash
# Build the solution and run every console test harness with a non-symlinked TMPDIR.
# The store tests refuse symlinked paths by design, and macOS /var and /tmp are symlinks.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd -P)"
scratch="$root/.tmp-tests"
mkdir -p "$scratch"
export TMPDIR="$scratch/" TMP="$scratch" TEMP="$scratch"
cd "$root"
dotnet build CFDWorkbench.slnx -nologo -v q
for project in Core Cli Desktop; do
  echo "== CfdWorkbench.$project.Tests"
  dotnet run --no-build --project "tests/CfdWorkbench.$project.Tests/CfdWorkbench.$project.Tests.csproj" > "$scratch/$project.log" 2>&1 \
    || { tail -20 "$scratch/$project.log"; echo "FAILED: CfdWorkbench.$project.Tests"; exit 1; }
  tail -1 "$scratch/$project.log"
done
echo "all test harnesses passed"
