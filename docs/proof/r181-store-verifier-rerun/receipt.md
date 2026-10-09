---
id: proof-r181-store-verifier-rerun
title: "Ruling 181 Windows verifier rerun"
type: proof-pack
status: blocked
owner: "@win-r181-store-verifier-rerun"
phase: implementation
tags: [windows, verifier, ruling-181, proof]
links:
  - { to: review-pr-22, rel: relates-to }
  - { to: proof-win-store-final-rerun-r175, rel: supersedes }
  - { to: proof-wri-r182-runner-ready, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Capture wrapper and immutable source snapshots are prepared for the single Ruling 181
  rerun. The verifier has not been executed; no Windows PASS is claimed.
---

# Ruling 181 Windows verifier rerun

**Status: pending the one authorized live run.** The proof-local wrapper, residual
sampler, and exact source snapshots are prepared. `tools/verify-windows-store.py`
has not been executed by this package. No Windows PASS or readiness claim is made.

## Goal and scope

Goal: replace the Ruling 175 missing-exit capture with one complete, PHN-safe
Ruling 181 run of the unchanged verifier.

Done when: the receipt binds the verifier and runner snapshots, pinned Python and
SDK identities, retained process handle, numeric verifier and shutdown exits,
shutdown-before-residual ordering, zero active job count, unchanged source, and a
post-shutdown sample of the evidenced SDK build-server processes.

Not in scope: verifier, runner, source, test, or display changes; readiness admission;
any second verifier invocation; or a claim about unrelated processes. The verifier's
internal 60-second contract is unchanged. Its capture wrapper uses one 180-second
Stopwatch envelope; the post-shutdown residual child has a 20-second sub-ceiling
inside that same envelope.

## Immutable source binding

The proof package carries byte snapshots under `source-snapshots/`. The expected
verifier SHA-256 is
`a79ac73c04cc8769d65868cde2f3c69154f27ebfcff40743bd20ca0b668afd52`. The expected
runner SHA-256 is recorded in `capture-r181.ps1` and checked against both its live
source and the packaged snapshot before any child starts. The verifier runs from
the repository's relative path because it derives its working root from `__file__`;
the source guard and hashes bind the bytes before and after execution.

## Capture record

The following measurements are **Not recorded** until the wrapper is run on the
Windows host:

| Measurement | Status |
| --- | --- |
| Tested HEAD and snapshot hashes | Not recorded |
| Python and SDK identity | Not recorded |
| Verifier numeric exit, timeout, wall time, retained handle | Not recorded |
| Build-server shutdown numeric exit and event order | Not recorded |
| Job active-process count and cleanup | Not recorded |
| Source fingerprint before/after | Not recorded |
| PHN-safe stdout/stderr derivatives | Not recorded |
| Post-shutdown MSBuild/VBCSCompiler sample | Not recorded |

`capture-r181.ps1` writes an append-only launch marker immediately before the sole
verifier launch and refuses any existing marker or output set. A post-launch failure
therefore records a blocked receipt and cannot be retried. The exact verifier command is
`<Assert-WriToolchain Python> tools/verify-windows-store.py`, with no arguments.
Only the runner's PHN-checked derivatives are retained. The residual sampler reads
process command lines in memory only to classify MSBuild and VBCSCompiler roles; it
publishes role, PID, parent PID, UTC creation time, count, sample UTC, elapsed time,
completion, and unreadable count. The prior receipt and PR #22 review identify the
MSBuild node and VBCSCompiler. No Razor server process name appears in the joined
verifier, runner, tests, or referenced proof. Other processes are **Not assessed**.

## Run procedure

After owner review, invoke the wrapper once from the repository root in PowerShell 7
on Windows x64, passing the absolute Python executable selected for this run:

```powershell
& docs/proof/r181-store-verifier-rerun/capture-r181.ps1 -PythonPath '<absolute Python executable>'
```

Do not rerun the verifier if this wrapper exits nonzero. Preserve any emitted evidence
and update this receipt with the observed state. The wrapper does not change display
settings and does not run the Windows Settings preflight.
