#!/bin/bash
# S6 tip BL route, snappyHexMesh variants W2a / W2b (Ruling 97): a copy of mesh-gate-r2.sh (generate, snappy in parallel,
# reconstruct) without the layer-coverage and y+ steps, ending in one checkMesh with sets and fields, the cell centres
# and the S6 locator (mesh-locate-snappy.py). The operator's machine-courtesy rule applies: of-run.sh waits while the
# 1-minute load is above CFDW_MAX_LOAD (30 here) or the join lock exists.
# Usage: mesh-gate-snappy.sh <case.yaml> <n-procs <= 6>
set -euo pipefail
export CFDW_MAX_LOAD=30
yaml="$1"; np="$2"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
rec="$here/launcher-record.py"
name=$(basename "$yaml" .yaml)
grep -q '^run_dir: PENDING$' "$yaml" || { echo "refusing: $yaml already names a run" >&2; exit 2; }
run="runs/$(date -u +%Y%m%dT%H%M%SZ)-$name"
sed -i '' "s#^run_dir: PENDING\$#run_dir: $run#" "$yaml"
uv run --quiet --with pyyaml python3 "$here/make-tip-bl-snappy.py" "$yaml" "$run" > "$run.generator.txt"
mv "$run.generator.txt" "$run/generator.txt"
cat "$run/generator.txt"
python3 "$rec" init "$run"
"$of" "$run" blockMesh blockMesh
"$of" "$run" checkMesh-preflight checkMesh
"$of" "$run" surfaceCheck surfaceCheck constant/triSurface/wing.stl
"$of" "$run" surfaceFeatureExtract surfaceFeatureExtract
"$of" "$run" snappyHexMesh snappyHexMesh -overwrite  # serial: the layer fields (0/nSurfaceLayers) then match the face order of the mesh
"$of" "$run" checkMesh checkMesh -constant -allGeometry -allTopology -writeSets vtk \
  -writeFields '(nonOrthoAngle cellDeterminant cellShapes faceWeight cellVolume)'
"$of" "$run" postProcess-C postProcess -constant -func writeCellCentres
uv run --quiet --with pyyaml --with numpy --with scipy python3 "$here/mesh-locate-snappy.py" "$run" "$yaml" > "$run/log.locate" 2>&1 \
  || { cat "$run/log.locate"; exit 1; }
cat "$run/log.locate"
echo "run=$run"
