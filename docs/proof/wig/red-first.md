---
id: proof-wig-red-first
title: "WSL-INLINE-ARGV gate: red first, then green"
type: doc
status: observed
owner: "@trk-wig"
tags: [windows, wsl, gate, defect-class]
links:
  - { to: proof-win-cfmesh-probe, rel: refines }
review-by: "2026-11-09"
summary: >-
  tools/check-wsl-inline.py failed its three positive shapes on an empty gate, then passed after the pattern landed; main has 0 hits.
---

# WSL-INLINE-ARGV gate: red first, then green

Class: `WSL-INLINE-ARGV` in `docs/lessons/defect-classes.md`. Control: `tools/check-wsl-inline.py`, run by
`tools/check-docs.py` (fast ring).

## Red (empty gate)

`findings()` returned `[]`. `python3 tools/check-wsl-inline.py --self-test` exited **1** on the first positive
(PowerShell inline):

```
AssertionError: ('a.ps1', "wsl.exe --distribution d --exec /bin/bash -lc 'cartesianMesh -help'\n", 0, 1)
```

## Green (pattern in place)

`python3 tools/check-wsl-inline.py --self-test` exited **0**: `check-wsl-inline self-test OK`.

Positives (each must fire once): PowerShell `--exec /bin/bash -lc '...'`; PowerShell `-- bash -c "..."`; Python argv list
`['wsl.exe','bash','-lc',cmd]`; `WSL + ['bash','-lc',cmd]` through a variable; a Python list wrapped over three lines;
cmd `-e bash -c`; shell `-- sh -c`.

Negatives (each must stay silent): script-file calls (`-- bash /mnt/c/.../x.sh arg`, `bash -l x.sh`); the
`wsl + ['env','-i','bash','--noprofile','--norc',script]` shape in `docs/proof/win-naca/run-l6.py`; Git Bash
`& $bash -c 'cat /proc/loadavg'` (no WSL); a `#` comment; a cmd `REM` line; a Python docstring that states the rule; a
`/mnt/c/...` path with a `# wsl` comment; `/usr/lib/wsl/lib/...` followed by a plain `bash -c` on the same line.

## Sweep of main (3a139e3e base)

`python3 tools/check-wsl-inline.py`: exit 0, **0 hits** in tracked `.ps1`/`.py`/`.sh`/`.cmd` (none under `tools/`, none
under pinned `docs/proof/**`). The PR #11 inline `bash -lc` strings survive only in `.md` receipts
(`docs/proof/win-cfmesh/probe.md`, `receipt.md`), which the gate does not scan. The allowlist is empty and shrink-only.

Known limit: `bash` and its `-c` flag must share a line.
