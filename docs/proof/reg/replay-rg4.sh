#!/bin/bash
# Replay the ECR and V3D joins through the merge driver. Run from the RG4 worktree.
set -u
cd /Users/mallalieut/projects/CFD-Workbench-fix-rg4-append-union
F=docs/lessons/defect-classes.md
for M in f16ea452 6ca8caa6; do
  D=/Users/mallalieut/projects/cfd-workbench-continuation/scratch/rg4/replay-$M
  rm -rf "$D"; mkdir -p "$D"
  BASE=$(git merge-base "$M^1" "$M^2")
  echo "merge=$M ours=$(git rev-parse --short "$M^1") theirs=$(git rev-parse --short "$M^2") base=$(git rev-parse --short "$BASE")"
  git show "$BASE:$F" > "$D/base"
  git show "$M^1:$F" > "$D/ours"
  git show "$M^2:$F" > "$D/theirs"
  python3 tools/merge-defect-register.py "$D/base" "$D/ours" "$D/theirs"
  echo "driver exit=$?"
  git show "$M:$F" > "$D/committed"
  if cmp -s "$D/ours" "$D/committed"; then echo "IDENTICAL to committed $M:$F"; else echo "DIFFERS from committed"; diff "$D/ours" "$D/committed" | head -20; fi
  echo "markers: $(grep -c '<<<<<<<' "$D/ours")"
  wc -l "$D/ours"
done
