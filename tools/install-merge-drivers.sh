#!/usr/bin/env bash
# Register the repo-local git merge drivers that .gitattributes names and the pack's `coord-core.py install` does not
# (driver config lives in .git/config, which is not committed). Run once per clone, from the checkout whose tools/ you want
# git to call (worktrees share the clone's config, so the primary checkout is the usual place):
#   tools/install-merge-drivers.sh
# defect-register: tools/merge-defect-register.py for docs/lessons/defect-classes.md (JOIN-LOG-CONFLICT). A clone without
# this registration falls back to git's normal text merge, so nothing breaks; the register just conflicts as before.
# Bash 3.2-safe.
set -eu
root="$(cd "$(dirname "$0")/.." && pwd)"
python="$(command -v python3 || command -v python)"
git -C "$root" config merge.defect-register.name "defect-classes register: resolve whole-entry adds and strict extensions"
git -C "$root" config merge.defect-register.driver "\"$python\" \"$root/tools/merge-defect-register.py\" %O %A %B %P"
git -C "$root" config --get merge.defect-register.driver
