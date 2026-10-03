#!/bin/bash
# Stamp a run directory into one SPIKE-04 case file, generate the case from its TMR grid, solve and summarise.
# Usage: run-tmr-case.sh <case.yaml> <grid-dir>
set -euo pipefail
yaml="$1"; grids="$2"
here="$(cd "$(dirname "$0")" && pwd)"
name=$(basename "$yaml" .yaml)
grep -q '^run_dir: PENDING$' "$yaml" || { echo "refusing: $yaml already names a run" >&2; exit 2; }
run="runs/$(date -u +%Y%m%dT%H%M%SZ)-$name"
sed -i '' "s#^run_dir: PENDING\$#run_dir: $run#" "$yaml"
grid=$(sed -n 's/^    path: \(n0012family.*\)$/\1/p' "$yaml")
np=$(sed -n 's/.*n_subdomains: \([0-9]*\),.*/\1/p' "$yaml")
uv run --quiet --with pyyaml --with numpy python3 "$here/make-tmr-case.py" "$yaml" "$run" "$grids/$grid"
python3 "$here/launcher-record.py" init "$run"
"$here/solve-tmr.sh" "$run" "$np" "$yaml"
echo "run=$run"
