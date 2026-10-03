#!/bin/bash
# SPIKE-03 unattended meshing pipeline for one generated wing run directory. No human step between commands.
# Usage: mesh-wing.sh <run-dir> <n-procs ≤ 6>
set -euo pipefail
run="$1"; np="$2"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
"$of" "$run" surfaceCheck surfaceCheck constant/triSurface/wing.stl
"$of" "$run" surfaceFeatureExtract surfaceFeatureExtract
"$of" "$run" blockMesh blockMesh
"$of" "$run" decomposePar decomposePar -force
"$of" "$run" snappyHexMesh mpirun -np "$np" snappyHexMesh -parallel -overwrite
"$of" "$run" reconstructParMesh reconstructParMesh -constant
"$of" "$run" checkMesh checkMesh -constant -allGeometry -allTopology
