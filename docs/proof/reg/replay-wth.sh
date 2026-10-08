#!/usr/bin/env bash
# Replay the WTH join: rebuild base/ours/theirs from the merge commit's parents, run the driver, compare with the committed file.
set -u
cd "$(dirname "$0")/../../.."
F=docs/lessons/defect-classes.md
M=$(git log --format=%H --grep='^merge: WTH' -n 1 main)
P1=$(git rev-parse "$M^1")
P2=$(git rev-parse "$M^2")
B=$(git merge-base "$P1" "$P2")
T=$(mktemp -d)
git show "$B:$F" > "$T/base"
git show "$P1:$F" > "$T/ours"
git show "$P2:$F" > "$T/theirs"
git show "$M:$F" > "$T/leader"
echo "merge=$M ours=$P1 theirs=$P2 base=$B"
python3 tools/merge-defect-register.py "$T/base" "$T/ours" "$T/theirs"
echo "driver exit: $?"
if cmp -s "$T/ours" "$T/leader"; then
  echo "IDENTICAL to the leader's committed file"
else
  echo "DIFFERS from the leader's committed file:"
  diff "$T/ours" "$T/leader" | head -20
fi
