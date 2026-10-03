#!/bin/bash
# Copy the small, committed receipts of one run (runs/ is git-ignored) into docs/proof/<spike>/receipts/<run-name>/.
# Usage: collect-receipts.sh <run-dir> <dest-dir>
# Round 2: also the A4 monitor log and stop record, the generator output, the y+ screen, pre-flight/lint/record logs,
# the manifest and /usr/bin/time -l outputs (times.txt); the home path and user name are redacted in every copy.
set -euo pipefail
run="$1"; dest="$2/$(basename "$1")"
mkdir -p "$dest"
user="$(id -un)"
redact() { sed -e "s#$HOME#<HOME>#g" -e "s#/Users/$user#<HOME>#g" -e "s#^Host *:.*#Host   : <HOST>#"; }
for f in run-ledger.txt layer-coverage.txt log.checkMesh log.checkMesh-plain log.checkMesh-preflight log.checkMesh-det \
         log.checkMesh-v2 log.topoSet-det log.surfaceCheck log.blockMesh log.decomposePar log.decomposePar-solve \
         log.reconstructPar log.reconstructParMesh convergence.txt a4-monitor.log a4-stop.txt generator.txt \
         yplus-area.txt cfdw-manifest.json; do
  [ -f "$run/$f" ] && redact < "$run/$f" > "$dest/$f"
done
for f in "$run"/log.lint-* "$run"/log.record-*; do
  [ -f "$f" ] && redact < "$f" > "$dest/$(basename "$f")"
done
for t in "$run"/time.*; do
  [ -f "$t" ] && { echo "== $(basename "$t")"; cat "$t"; }
done > "$dest/times.txt" 2>/dev/null || true
[ -s "$dest/times.txt" ] || rm -f "$dest/times.txt"
if [ -f "$run/log.snappyHexMesh" ]; then
  grep -n -E "nProcs|Initial mesh|Layer mesh|Mesh with layers|target   mesh|^wing |Finished meshing|FOAM (FATAL|Warning)|allowSystemOperations" \
    "$run/log.snappyHexMesh" | redact > "$dest/snappyHexMesh.excerpt.txt" || true
fi
for solver in simpleFoam rhoSimpleFoam; do
  if [ -f "$run/log.$solver" ]; then
    { head -40 "$run/log.$solver"; echo "..."; tail -60 "$run/log.$solver"; } | redact > "$dest/$solver.head-tail.txt"
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
