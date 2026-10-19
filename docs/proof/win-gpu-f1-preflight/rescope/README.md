---
id: proof-win-gpu-f1-rescope-verification
title: "F1 re-scope (Ruling 237): node V verification receipts"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, arm64, f1, verification, proof]
links:
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  The code-stage proof pack of plan docs/plans/f1-rescope-r237.md node V: one receipt per run of
  tools/run-f1-verification.py, the whole offline union of the packet's checks on both PowerShell runtimes with the plan's
  suite-count floors. verification-v.json is the run the independent review (node R) reads; its `head` names the
  commit it measured and `worktree_clean` says nothing else was in the tree.
---

# F1 re-scope: node V verification receipts

**What a receipt is.** `tools/run-f1-verification.py` runs, in this order, the packet's authority scan, the Python and
PowerShell packet self-tests, the Python unit suites (`tests/test_f1_*.py`) and the PowerShell suites
(`tests/f1_*.ps1`, on pwsh and on Windows PowerShell 5.1 started from pwsh) concurrently (width 2), then the
host-bindings gate, the WSL-inline gate, the proof-PII gate and `check-docs`. Before anything runs it counts the suite
files against the plan's floors (Python at least 9, PowerShell at least 9) and refuses a smaller count: a deleted suite
is a lost control, never a faster ring. The receipt (`cfdw-f1-rescope-verification/1`) records `head`,
`worktree_clean`, the counts (files, tests ran, PowerShell runs), and every step's command, exit and seconds. Child
output stays in the scratch; no absolute path of the machine reaches the receipt (the first V run recorded the scratch
path and the proof-PII step refused it, which is the gate working).

**Reading `verification-v.json`.** `green` is true only when no floor refused and every step exited 0. `steps[].summary`
carries the last line of each step (`ran N tests`, `N runs (S suites x 2 runtimes)`, a gate's PASS line). The numbers
are the laptop's (Windows 11 ARM64, native pwsh 7.6.7 and Windows PowerShell 5.1); an x64 host produces its own
receipt the same way.

**What V found.** The run was red twice before it was green: the packet's authority scan refused a quoted `"make"`
literal that the C3 transaction self-test had added (that scan ran in no ring until V; it is in `check-docs` now,
register entry LISTED-CHECK-IN-NO-RING), and the proof-PII gate refused the first receipt's scratch path. Both are
fixed in the same join as this receipt.

```powershell
py -3 tools/run-f1-verification.py --receipt docs/proof/win-gpu-f1-preflight/rescope/verification-v.json
py -3 tools/run-f1-verification.py --self-test
```
