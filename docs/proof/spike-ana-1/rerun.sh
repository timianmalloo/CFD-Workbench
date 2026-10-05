#!/bin/sh
set -eu
root=/Users/mallalieut/projects/CFD-Workbench-spike-ana-1
proof="$root/docs/proof/spike-ana-1"
python=/Users/mallalieut/dev/sim/.venv/bin/python
if [ "$PWD" != "$root" ]; then
  echo "Run from $root" >&2
  exit 2
fi
export OPENBLAS_NUM_THREADS=1
export VECLIB_MAXIMUM_THREADS=1

"$python" "$proof/reference.py"
dotnet run -c Release --project "$proof/probe/probe.csproj" -- --selftest
dotnet run -c Release --no-build --project "$proof/probe/probe.csproj" -- --cases "$proof/cases.tsv" "$proof/csharp.tsv"
"$python" "$proof/compare_fidelity.py"

sh "$proof/xfoil/build.sh"
"$python" "$proof/run_xfoil.py"
"$python" "$proof/compare_accuracy.py"

dotnet run -c Release --no-build --project "$proof/probe/probe.csproj" -- --benchmark 300 > "$proof/timing.txt"
cat "$proof/timing.txt"
cat "$proof/cst-residual.tsv"
wc -c "$proof/probe/weights.bin"
python3 tools/check-docs.py
