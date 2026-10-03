#!/bin/bash
# Round-2 SPIKE-03: generate, mesh (snappyHexMesh) and gate one wing case, unattended. Optional y+ screen.
# Usage: mesh-gate-r2.sh <case.yaml> <n-procs ≤ 6> [screen]
# Every OpenFOAM process goes through cases/tools/of-run.sh (launcher record, lint, env allow-list, bundle, banner).
# checkMesh on the background mesh is the pre-flight before any other OpenFOAM app touches the case.
# Gate read-outs: checkMesh -allGeometry -allTopology; cellDeterminant cells < 0.001 and < 0.3 (DR-F2-6); layer
# coverage >= n on >= 95 % and >= 10 on 100 % per region (DR-F2-3); with `screen`, a short simpleFoam SA run and the
# area-weighted y+ on the wing (a screen, not the DR-F2-3 gate, which needs an A4-stationary field).
set -euo pipefail
yaml="$1"; np="$2"; screen="${3:-}"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
rec="$here/launcher-record.py"
name=$(basename "$yaml" .yaml)
grep -q '^run_dir: PENDING$' "$yaml" || { echo "refusing: $yaml already names a run" >&2; exit 2; }
run="runs/$(date -u +%Y%m%dT%H%M%SZ)-$name"
sed -i '' "s#^run_dir: PENDING\$#run_dir: $run#" "$yaml"
uv run --quiet --with pyyaml python3 "$here/make-wing-r2.py" "$yaml" "$run" > "$run.generator.txt"
mv "$run.generator.txt" "$run/generator.txt"
cat "$run/generator.txt"
python3 "$rec" init "$run"
"$of" "$run" blockMesh blockMesh
"$of" "$run" checkMesh-preflight checkMesh
"$of" "$run" surfaceCheck surfaceCheck constant/triSurface/wing.stl
"$of" "$run" surfaceFeatureExtract surfaceFeatureExtract
"$of" "$run" decomposePar decomposePar -force
"$of" "$run" snappyHexMesh mpirun -np "$np" snappyHexMesh -parallel -overwrite
"$of" "$run" writeCellCentres mpirun -np "$np" postProcess -parallel -func writeCellCentres -time 0
"$of" "$run" reconstructParMesh reconstructParMesh -constant
"$of" "$run" checkMesh checkMesh -constant -allGeometry -allTopology
read -r chord half n_layers ratio U nu <<< "$(uv run --quiet --with pyyaml python3 -c 'import sys,yaml; c=yaml.safe_load(open(sys.argv[1])); print(c["geometry"]["chord"], c["geometry"]["half_span"], c["mesh"]["layers"]["n"], c["mesh"]["layers"]["expansion_ratio"], c["physics"]["freestream"]["U_m_s"], c["physics"]["fluid"]["nu_m2_s"])' "$yaml")"
python3 "$here/layer-coverage.py" "$run" "$n_layers" "$ratio" "$U" "$nu" "$chord" 10 "$half" > "$run/layer-coverage.txt"
cat "$run/layer-coverage.txt"
"$of" "$run" checkMesh-det checkMesh -constant -allGeometry -writeFields '(cellDeterminant)'
"$of" "$run" topoSet-det topoSet -constant -dict system/topoSetDict.det
grep -E "detBelow|Read [0-9]+ cells|size" "$run/log.topoSet-det" | tail -6 || true
if [ "$screen" = screen ]; then
  # product pipeline step (not an OpenFOAM process): fresh fields for the screen, then re-pin the tree
  rm -rf "$run/0" "$run"/processor*
  cp -R "$run/0.orig" "$run/0"
  python3 "$rec" update "$run"
  "$of" "$run" decomposePar-solve decomposePar -force
  "$of" "$run" simpleFoam mpirun -np "$np" simpleFoam -parallel
  "$of" "$run" reconstructPar reconstructPar -latestTime
  python3 "$here/yplus-area.py" "$run" "$chord" "$half" > "$run/yplus-area.txt"
  cat "$run/yplus-area.txt"
fi
echo "run=$run"
