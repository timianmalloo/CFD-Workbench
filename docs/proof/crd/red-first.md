---
id: proof-crd-red-first
title: "CRD red-first receipt"
type: proof-pack
status: active
owner: "@trk-crd"
phase: implementation
tags: [merge-driver, proof, register-class-mismatch]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Receipt for the merge-binding check: red on the rulings.md coord-register binding, green after rebinding, PHN join replay identical by hash.
---

# CRD red-first: merge-driver bindings match file formats

Check: `python3 tools/check-merge-bindings.py` (run by `tools/check-docs.py`). Ring: fast, every push. Cost: about 0.1 s.
Rule: a `merge=coord-register` path must parse as JSONL at HEAD; a `merge=defect-register` path must be markdown.

## Red (old binding, `.gitattributes:7`)

`red.txt`:

    MERGE-BINDING: docs/notes/rulings.md: bound to merge=coord-register but line 1 is not JSON
    exit 1

## Green (binding removed)

The binding came from `.agents/artifacts.yml` (repo-owned, below the managed-block end marker): `docs/notes/rulings.md: register`.
`coord install` (`_install_merge_driver`) appends one `.gitattributes` line per register pattern in that file.
I removed the line from both files, so a re-run of `coord install` does not restore it. The pack is untouched.

`green.txt`:

    merge bindings ok: every coord-register path parses as JSONL, every defect-register path is markdown
    exit 0

## Replay of the PHN join (`replay-phn.txt`)

Base, ours, theirs of `docs/notes/rulings.md` taken from merge `db58a1e2` and its merge-base. Plain `git merge-file`
exits 0, no conflict marker lines, and the result is byte-identical to the committed file at 2c656096
(sha256 `83f1b5c9...a2461` on both sides). The scrubbed literal is never printed.
