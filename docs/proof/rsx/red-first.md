---
id: proof-rsx-red-first
title: RSX red-first - proof PowerShell reader sharing and text-unset capture folders
type: proof-pack
status: draft
owner: "@trk-rsx"
tags: [rsx, reader-share, capture-manifest]
links:
  - { to: proof-application-core, rel: relates-to }
review-by: 2026-11-12
summary: >-
  The reader-sharing scan now covers proof PowerShell (report mode until 2026-10-20) and a fast-ring check keeps every capture folder text-unset.
---
# RSX red-first (Ruling 229)

Base main 0d37bced. Both controls were run on the unchanged tree first.

## Item 1: reader-sharing scan covers proof PowerShell
Rule chosen: a default-share read (`[IO.File]::ReadAll*/OpenRead/OpenText`, `[IO.File]::Open` without FileShare,
`Get-Content`) is flagged when its line, or the line assigning a variable it uses, names a `live` directory or a
`*.jsonl` file. Allow-marker `# reader-share: reviewed <ruling>`. Report mode until 2026-10-20, then failing.
Why not markers only: a marker on the PC scripts edits frozen proof bytes. Why not fail now: main's ring must not
go red while the PC repairs the scripts.

- Old check on main: `python3 tools/check-reader-sharing.py` exit 0, `READER-SHARE ok: 0 readers without delete sharing`
  (`old-reader-share.txt`). It missed the F1 defect.
- New check, default (report mode): exit 0, 9 `READER-SHARE-PS REPORT` lines (`new-reader-share.txt`).
- New check, `--enforce-ps` (what it does from 2026-10-20): exit 1 (`enforced-reader-share.txt`), including
  `docs/proof/win-gpu-f1-preflight/packet/Invoke-Preflight.ps1:133: $raw = [IO.File]::ReadAllText($events)`.
  The ruling cites line 136; on main the same statement is at line 133 (the attempt branch has a different file).
- Sweep (9 hits, 5 scripts; not fixed here, the PC repairs the F1 ones):
  - win-gpu-b1/capture.ps1:75 ReadAllText($eventsPath), :92 Get-Content $samplesPath
  - win-gpu-b1/wait-observer.ps1:24 ReadAllText($eventsPath), :41 Get-Content $statePath
  - win-gpu-b2/capture-completed-install.ps1:83 ReadAllText($eventsPath)
  - win-gpu-b2/capture-install.ps1:75 ReadAllText($eventsPath), :131 Get-Content $samplesPath
  - win-gpu-f1-preflight/packet/Invoke-Preflight.ps1:122 Get-Content $path (completed-guard.state.json, the os.replace'd state file), :133 ReadAllText($events)
- Self-test: 9 C# offenders and 3 PowerShell offenders caught (variable-resolved ReadAllText, Get-Content on a Live path,
  `[System.IO.File]::Open` default share); 8 C# and 6 PowerShell clean forms pass (FileStream with
  `ReadWrite,Delete`, `File.Open` with FileShare, marker on the same line, marker on the previous line, non-live
  path, comment).

## Item 2: every capture folder keeps exact bytes
`tools/check-capture-text-attr.py`: each tracked file listed in a `capture-manifest.json` must have `git check-attr text`
unset (`-text` or `binary`); `text` with an explicit `eol=` counts as declared (authored sources in win-gpu-b1/b2/g1/qualification).

- Before the `.gitattributes` change (`old-capture-attr.txt`): exit 1, 11 of 17 manifests have listed files at `text: auto`:
  r181-store-verifier-rerun (5 of 14), ring-windows (40 of 497), win-gpu-b1 (31), win-gpu-b2 (81), win-gpu-g1 (8),
  win-gpu-qualification (3), win-r151-l3 (2), win-store-implementation (1), wri-probe (19), wri-r182-runner (16),
  wri-r184-result (12). Already unset: copy447-reachability, win-gpu-f1-preflight run-1/checkpoint, run-1/failed-live, recovery-1/final, wri-r179.
  The `auto` counts for b1/b2/g1/qualification come from the folder's own `.gitattributes` and `.json/.md/.py/.sh/.ps1`, which declare `eol=`; they pass as declared.
- After adding 11 `docs/proof/<dir>/** -text` lines to the root `.gitattributes`: exit 0. `git diff --stat` shows only
  `.gitattributes`: no blob changed. `check-capture-manifests.py`: 17 manifests match their committed blobs.
- Self-test (throw-away git repo): folder without `-text` fails and names `cap/** -text`; with `-text` passes; with
  `binary` passes; with `text eol=lf` passes; a second listed file left at `auto` fails.
- Cost: about 0.3 s wall for the run and for the self-test; wired in `tools/check-docs.py` (fast ring).
