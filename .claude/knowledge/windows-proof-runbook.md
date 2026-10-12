---
id: windows-proof-runbook
title: "Windows and WSL proof runbook"
type: knowledge
status: active
owner: "@pc-coordinator"
phase: implementation
tags: [windows, wsl, powershell, proof, evidence]
links:
  - { to: proof-win-gpu-b1-r199, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: relates-to }
review-by: "2027-01-11"
summary: >-
  Mandatory launcher, process ownership, hashing, timing, and shell-boundary rules for Windows and WSL proof work.
load: glob
applyTo: "docs/proof/win-*/**,**/*.ps1"
---

# Windows and WSL proof runbook

PC sessions read this runbook at grounding. Any session that touches `docs/proof/win-*` reads it before the first
proof action.

- A Windows launcher exit is not Linux-workload state: WSL proof launchers select their foreground/session method with
  a committed teardown precheck, check the exact workload cmdline before launch and on every pre-acknowledgement exit,
  and record both checks.
- Hash-bound text proof is written with its repository LF form before hashing; a proof manifest is checked against
  committed blobs, not a CRLF-transformed worktree.
- A long-running gate and any repository or remote mutation run in separate shell calls; a harness yield is not process
  completion.
- After invoking a PowerShell script or cmdlet, use terminating errors and `$?`; `$LASTEXITCODE` is evidence only for
  a native executable that actually set it.
- A capture wrapper records its self-hash before its first child starts and reuses that recorded value in its receipt.
  A hash computed after a child returns is labeled post-probe and cannot establish pre-execution source identity.
- Post PR and issue comments only through `tools/post-github-comment.py`. Never paste a raw log; redact to
  `%USERPROFILE%` and `<host>` first.
- A bounded WSL proof workload starts in its own session/process group. Its stop path signals and verifies the whole
  owned group, runs on every wrapper error, and never treats stopping only the `wsl.exe` launcher or shell PID as
  Linux-workload termination.
- A timestamp read through `ConvertFrom-Json` is treated as a typed `DateTime`/`DateTimeOffset` before any string
  conversion. Casting the converted value to text first can drop its `Z`/offset and silently shift freshness checks by
  the host timezone.
- A gate that may outlive the tool yield runs in its own shell call. Never place a commit, push, merge, or other mutation
  after it in the same command, because the shell can continue after the harness returns a timeout/session boundary.
