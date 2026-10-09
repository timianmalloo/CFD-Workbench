#!/usr/bin/env bash
# Replay the PR #17 join: rebuild base/ours/theirs from the merge commit's parents, run the driver, show exit and markers.
set -u
cd "$(dirname "$0")/../../.."
F=docs/lessons/defect-classes.md
M=$(git log --format=%H --grep='^merge: PR #17' -n 1 main)
P1=$(git rev-parse "$M^1")
P2=$(git rev-parse "$M^2")
B=$(git merge-base "$P1" "$P2")
T=$(mktemp -d)
git show "$B:$F" > "$T/base"
git show "$P1:$F" > "$T/ours"
git show "$P2:$F" > "$T/theirs"
echo "merge=$M ours=$P1 theirs=$P2 base=$B"
python3 tools/merge-defect-register.py "$T/base" "$T/ours" "$T/theirs"
echo "driver exit: $?"
echo "marker lines: $(grep -c -E '^(<<<<<<<|=======|>>>>>>>)' "$T/ours")"
echo "theirs-only lines (vs base) missing from the driver output:"
comm -13 <(sort "$T/base") <(sort "$T/theirs") | grep -v '^$' | while IFS= read -r line; do
  grep -qxF -- "$line" "$T/ours" || echo "MISSING: $line"
done
echo "(end of missing list)"
echo "theirs' frontmatter links in output:"
grep -n -E '^  - \{ ?to: ' "$T/ours" | head -20
