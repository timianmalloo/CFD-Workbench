---
id: proof-gfx-red-first
title: GFX red-first - admission self-quote and PII UTF-16 blind spot
type: proof-pack
status: draft
owner: "@trk-gfx"
tags: [gfx, plat-a, pii-gate]
links:
  - { to: proof-asc-red-first, rel: relates-to }
review-by: 2026-11-08
summary: >-
  Admission check ignores a marker quoted as code; the PII guard decodes UTF-16 files; each shown failing first.
---

# GFX red-first proof

Track GFX, branch `fix/gfx-gate-fixes`. Two gate fixes, each red before green.

## Item 1: admission check fires on its own documentation (PLAT-A)

Red (new self-test cases against the old `claims()`), `docs/proof/gfx/adm-red.txt`:

```
self-test FAIL: backtick-quoted marker must pass while stale: 1 ...
self-test FAIL: fenced marker must pass while stale: 1 ...
self-test FAILED
```

Green after the fix, `docs/proof/gfx/adm-green.txt`: `self-test OK`. The cases: bare claim while stale fails (existing);
backtick-quoted passes; fenced passes; a bare marker after a code span still fails; stale with no claim prints STALE, exit 0.

Real check on the branch: `python3 tools/check-windows-admission.py` prints
`WINDOWS-STORE-EVIDENCE STALE since src/CfdWorkbench.Core: e6121133 -> 06bc8f79`, exit 0 (was exit 1). Cost 0.35 s wall.
Both documents already quoted the marker in backticks, so no document changed. The real check now runs in `check-docs.py`.

## Item 2: PII guard skips UTF-16 files (PII-GATE-SKIPS-UTF16)

Red (old `scan_tree` from HEAD on run-time offenders), `docs/proof/gfx/pii-red.txt`:

```
MISSED (old gate skips it) le_nobom.txt
MISSED (old gate skips it) le_bom.txt
MISSED (old gate skips it) be_nobom.txt
```

Green, `docs/proof/gfx/pii-green.txt`: `PROOF-PII self-test ok: 19 offenders caught, 23 clean passed`. The self-test builds
UTF-16LE no-BOM, UTF-16 BOM and UTF-16BE no-BOM offenders, a clean UTF-16LE file (must not be flagged or skipped) and a
binary file (must be skipped and named) at run time. UTF-16BE is handled, not out of scope.

Whole tree, `python3 tools/check-proof-pii.py`: 0 hits, 360 binary files skipped (paths with `--show-skipped`).
