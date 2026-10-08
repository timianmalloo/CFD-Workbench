# TMH red-first receipt (WINDOWS-TEXT-MODE-HASH)

Gate: `python3 tools/check-text-mode-hash.py` (stdlib; fast ring; 0.25 s wall for the scan).

## Pre-fix tree (exit 1)

```
WINDOWS-TEXT-MODE-HASH: cases/tools/make-tip-bl-gmsh.py: ... line(s) 329, 427
WINDOWS-TEXT-MODE-HASH: cases/tools/make-tip-bl-snappy.py: ... line(s) 37, 364
WINDOWS-TEXT-MODE-HASH: cases/tools/make-tip-coupon-gmsh.py: ... line(s) 286, 384
WINDOWS-TEXT-MODE-HASH: cases/tools/make-tmr-case.py: ... line(s) 133, 135, 137, 138, 140, 151, 312
WINDOWS-TEXT-MODE-HASH: cases/tools/make-wing-case.py: ... line(s) 33
WINDOWS-TEXT-MODE-HASH: cases/tools/make-wing-gmsh.py: ... line(s) 209, 307
WINDOWS-TEXT-MODE-HASH: cases/tools/make-wing-r2.py: ... line(s) 37, 371
WINDOWS-TEXT-MODE-HASH: cases/tools/test-foam-dict-lint.py: ... line(s) 54, 56
```

(`cases/tools/launcher-record.py` is allowlisted with a reason; it also matches the shape.)

## Fixed tree (exit 0)

```
text-mode hash ok: 1 file(s) allowlisted with a reason, none unfixed
```

## Self-test

```
check-text-mode-hash self-test OK
```

It plants an offender (hashlib + `write_text` + text `open`: red), a clean file (`newline=`, `wb`: green), a file with no
hashing (green), and checks the allowlist and stale-entry rules.

## No hash drift (Mac)

`make-tmr-case.py` (HEAD version vs fixed) on `cases/spike04-tmr-l5-tvd.yaml` with the TMR family-II grid level 5, no meshing:
`diff -r` of the two run directories is empty; `cfdw-manifest.json` sha256 is `f7ee58ef...933fdf7` in both. No TMR manifest is
committed, so the comparison is old generator against new, not against a committed file. Other generators need gmsh or
snappyHexMesh and were not run.

## Not-writers (classified)

`check-neuralfoil-weights.py` (writes bytes), `check-notices.py`, `coord-stream-summary.py`,
`recount-application-contracts.py` (stdout only): no text-mode file write, no fix needed.
