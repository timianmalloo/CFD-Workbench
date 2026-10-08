#!/usr/bin/env bash
# Scratch-clone check of the wiring: two branches each append a register entry; merge with the driver registered (clean)
# and without it (git's normal merge conflicts). The shared repository's git config is never touched.
set -u
here="$(cd "$(dirname "$0")/../../.." && pwd)"
S="$(mktemp -d)"
git init -q "$S/r" && cd "$S/r"
git config user.email t@example.invalid && git config user.name t
mkdir -p docs/lessons tools
cp "$here/tools/merge-defect-register.py" "$here/tools/install-merge-drivers.sh" tools/
printf 'docs/lessons/defect-classes.md merge=defect-register\n' > .gitattributes
printf '# Register\n\n**A · T.** a.\n' > docs/lessons/defect-classes.md
git add -A && git commit -qm base
git checkout -qb one && printf '\n**B · T.** b.\n' >> docs/lessons/defect-classes.md && git commit -qam one
git checkout -q master 2>/dev/null || git checkout -q main
git checkout -qb two && printf '\n**C · T.** c.\n' >> docs/lessons/defect-classes.md && git commit -qam two

echo "== without the driver registered (fresh clone fallback)"
git merge --no-edit one >/dev/null 2>&1; echo "merge exit: $? (non-zero = normal conflict)"
grep -c '^<<<<<<<' docs/lessons/defect-classes.md
git merge --abort

echo "== with tools/install-merge-drivers.sh run in the scratch repo"
bash tools/install-merge-drivers.sh >/dev/null
git merge --no-edit one >/dev/null 2>&1; echo "merge exit: $?"
cat docs/lessons/defect-classes.md
