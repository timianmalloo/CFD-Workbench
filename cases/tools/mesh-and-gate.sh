#!/bin/bash
# Generate, mesh and gate one SPIKE-03 wing case end to end (no solve). Prints the run directory last.
# Usage: mesh-and-gate.sh <case.yaml> <n-procs ≤ 6> <expansion-ratio>
set -euo pipefail
yaml="$1"; np="$2"; ratio="$3"
here="$(cd "$(dirname "$0")" && pwd)"
name=$(basename "$yaml" .yaml)
run="runs/$(date -u +%Y%m%dT%H%M%SZ)-$name"
uv run --quiet --with pyyaml python3 "$here/make-wing-case.py" "$yaml" "$run"
"$here/mesh-wing.sh" "$run" "$np"
"$here/of-run.sh" "$run" writeCellCentres mpirun -np "$np" postProcess -parallel -func writeCellCentres -time 0
python3 "$here/layer-coverage.py" "$run" 15 "$ratio" 5.0 1.0e-6 0.12 > "$run/layer-coverage.txt"
"$here/of-run.sh" "$run" checkMesh-plain checkMesh -constant
"$here/of-run.sh" "$run" checkMesh-det2 checkMesh -constant -allGeometry -writeFields "'(cellDeterminant)'"
printf 'FoamFile { version 2.0; format ascii; class dictionary; object topoSetDict; }\nactions ( { name detBelow03; type cellSet; action new; source fieldToCell; field cellDeterminant; min -1e30; max 0.3; } );\n' > "$run/system/topoSetDict.det"
"$here/of-run.sh" "$run" topoSet-det topoSet -constant -dict system/topoSetDict.det
echo "run=$run"
