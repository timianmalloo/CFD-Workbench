---
id: proof-win-gpu-b1-launcher-precheck
title: "B1 launcher precheck result"
type: proof-pack
status: complete
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, wsl, ruling-201]
links:
  - { to: proof-win-gpu-b1-r199, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  Foreground was selected because launcher teardown removed its child; setsid --wait preserved its child.
---

# B1 launcher precheck result

The committed cap-free Ruling 201 precheck ran as `pc-b1-r201-precheck-20261010d`.

| Candidate | PID / PGID / SID | Tagged sleep before launcher stop | Tagged residual afterward | Result |
|---|---|---:|---:|---|
| foreground | `44185 / 44185 / 44185` | yes | no (`pgrep` exit 1) | selected |
| `setsid --wait` | `44197 / 44197 / 44197` | yes | yes (`pgrep` exit 0) | rejected; exact cleanup removed it |

`launcher-precheck.json` records the exact argv, output files, selection rule, and cleanup result. Precheck 1's bare
`setsid` evidence remains under `precheck-1-setsid-no-wait/`. The observed launcher behavior rejects bare `setsid`; the
precise fork and teardown cause remains **Inferred**.
