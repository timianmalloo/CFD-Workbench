#!/bin/bash
# Copy the small, committed receipts of one run (runs/ is git-ignored) into docs/proof/<spike>/receipts/<run-name>/.
# Usage: collect-receipts.sh <run-dir> <dest-dir>
set -euo pipefail
run="$1"; dest="$2/$(basename "$1")"
mkdir -p "$dest"
for f in run-ledger.txt layer-coverage.txt log.checkMesh log.checkMesh-plain log.topoSet-det log.surfaceCheck \
         log.blockMesh log.decomposePar convergence.txt; do
  [ -f "$run/$f" ] && cp "$run/$f" "$dest/"
done
if [ -f "$run/log.snappyHexMesh" ]; then
  grep -n -E "nProcs|Initial mesh|Layer mesh|Mesh with layers|target   mesh|^wing |Finished meshing|FOAM (FATAL|Warning)" \
    "$run/log.snappyHexMesh" > "$dest/snappyHexMesh.excerpt.txt" || true
fi
for solver in simpleFoam; do
  if [ -f "$run/log.$solver" ]; then
    { head -40 "$run/log.$solver"; echo "..."; tail -60 "$run/log.$solver"; } > "$dest/$solver.head-tail.txt"
  fi
done
if [ -d "$run/postProcessing" ]; then
  find "$run/postProcessing" -name "*.dat" | while read -r dat; do
    name=$(echo "${dat#"$run"/postProcessing/}" | tr '/' '_')
    { head -20 "$dat"; echo "..."; tail -20 "$dat"; } > "$dest/$name"
  done
fi
( cd "$run" && find . -maxdepth 1 -type d | sort > "$OLDPWD/$dest/top-level-dirs.txt" )
echo "receipts -> $dest"
