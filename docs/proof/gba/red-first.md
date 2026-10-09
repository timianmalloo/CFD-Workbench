---
id: proof-gba-red-first
title: "GBA red-first receipt"
type: proof-pack
status: active
owner: "@trk-gba"
phase: implementation
tags: [gba, gate-before-add, red-first]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Sweep of git-listing gates and red/green runs showing the PII, WSL and notices gates now read new unstaged files.
---
# GBA red-first: tracked-file gates also read new, unstaged files

Class: GATE-BEFORE-ADD (`docs/lessons/defect-classes.md`). A gate that lists files with `git ls-files` never sees a new
file that is not yet `git add`ed, so a track's green run does not cover the file it is about to commit.

## Sweep

Every `tools/` script that lists files from git, and every gate `tools/check-docs.py` runs. Pack scripts under
`docs/ai-forward-pack/` were not edited; none reads the tree through `ls-files` in a way this track changed (audit only,
nothing found for `/updatepack`).

| Gate | Lists files with | Judges | New untracked file | Decision |
| --- | --- | --- | --- | --- |
| `check-proof-pii.py` | `git ls-files -z` (was) | working-tree text | invisible (was) | fixed, shared helper |
| `check-wsl-inline.py` | `git ls-files -z` (was) | working-tree text | invisible (was) | fixed, shared helper |
| `check-notices.py` | `git ls-files -z -- PREFIX` (was) | the XFOIL path allowlist over files that would be tracked | invisible (was) | fixed, shared helper |
| `check-merge-bindings.py` | `git ls-files` for names, `git show HEAD:path` for content | committed blobs (a merge driver acts on committed content) | invisible by design | left alone: HEAD |
| `check-capture-manifests.py` | `git ls-tree -r HEAD`, `git show` | manifests against committed blobs | invisible by design | left alone: HEAD |
| `check-foildsl-spec-sync.py` | `git show HEAD:`, `git ls-tree HEAD` | the committed branch state | invisible by design | left alone: HEAD |
| `check-spiral.py` | `git log`, `git diff-tree`, `git merge-base` | commits on the branch | not applicable (commit history) | left alone |

Shared helper: `tools/gate_files.py`, `worktree_files(root, *pathspec)` = `git ls-files` plus
`git ls-files --others --exclude-standard`, de-duplicated and sorted. Each gate keeps its own suffix, self-path and
pathspec filters.

## Red (before the fix) and green (after)

Scratch repo built at run time (`scratch/gba/redrun.py`): the gate copies are committed, then two untracked files are
added: `docs/untracked-pii.md` with a Windows home path with a one-letter account (assembled from parts at run time,
never written to the repo) and `tools/untracked-wsl.ps1` with an inline `bash -lc` through `wsl.exe`.

Red, gates from main (`9e969c5f`), run on the scratch repo:

```
check-proof-pii.py exit 1 names-untracked-file: False
check-wsl-inline.py exit 0 names-untracked-file: False
```

The PII exit 1 is the stale-allowlist report of the scratch repo, not the untracked file; the file is never named. The
WSL gate exits 0: blind.

Green, fixed gates, same scratch repo:

```
check-proof-pii.py exit 1 names-untracked-file: True
check-wsl-inline.py exit 1 names-untracked-file: True
```

`check-notices.py` red, the old listing in a scratch repo with an untracked file under the XFOIL prefix:
`old listing:[]`, `new listing:[docs/proof/spike-ana-1/xfoil/xfoil.f]`.

## Self-tests added (one per changed gate)

- `python3 tools/check-proof-pii.py --self-test`: builds a temp repo, writes an untracked offender, asserts `scan_tree` reports it.
- `python3 tools/check-wsl-inline.py --self-test`: temp repo with an untracked `new.ps1` carrying an inline WSL `bash -lc`; asserts `scan` reports it.
- `python3 tools/check-notices.py --self-test` (new flag): temp repo with an untracked file under the XFOIL prefix; asserts `worktree_files` lists it.

All three exit 0 on the fixed code (observed).

## Cost

`python3 tools/check-docs.py` on main before the change: 13.2 s wall (exit 0). After: see the Return (measured at the end).
