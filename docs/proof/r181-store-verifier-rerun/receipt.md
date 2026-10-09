---
id: proof-r181-store-verifier-rerun
title: "Ruling 181 Windows verifier rerun"
type: proof-pack
status: complete
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
  The single Ruling 181 rerun observed verifier exit 0 through the committed runner,
  numeric build-server shutdown 0 before residual sampling, zero matching SDK build
  servers, PHN PASS, and unchanged protected source.
---

# Ruling 181 Windows verifier rerun

**Status: complete Windows qualification evidence, pending Mac leader admission.**
The single authorized verifier launch returned observed process exit **0**. The
committed runner retained the process handle, shut SDK build servers down with
numeric exit **0** before its residual query, observed its job active count at zero,
and returned PHN-checked derivatives. The protected source fingerprint stayed equal.
This receipt does not admit readiness or authorize another verifier run.

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

| Measurement | Observed result |
| --- | --- |
| Tested HEAD | `a5c4564402d9564b9a3b715cc0b178efee7565c1` |
| Launch count | **1**; the atomic marker now prevents another launch |
| Verifier / runner SHA-256 | `a79ac73c04cc8769d65868cde2f3c69154f27ebfcff40743bd20ca0b668afd52` / `cc201aca4cd2e9e434bec1b9630487a2184622d2ece08617c373d037d528848d` |
| Toolchain | SDK `10.0.203`; Python `3.13.14`; identity checks 499 ms |
| Verifier process | numeric exit **0**; timeout false; retained handle true; `BuildVerifier` mode |
| Lifecycle | `target-root-exit:0 > shutdown-start > shutdown-exit:0 > target-job-query > target-job-close` |
| Job cleanup | `none-active-verified`; job active zero true |
| Verifier timing | runner child 20,601 ms; verifier total 19.858595 s; shared wrapper envelope 25,740 ms |
| Verifier result | 12 required PASS names; the one Ruling 145 historical held-reader failure; `TEST_EXIT=1`; `EXPECTED-FAIL 1`; `UNEXPECTED 0`; `CLASSIFIER_EXIT=0`; qualification PASS |
| Build-server residual sample | after shutdown; exit 0; PHN PASS; complete in 367 ms; 0 matching processes; 0 unreadable candidates |
| PHN | PASS; one substitution binding; no raw home, SID, or hostname in committed derivatives |
| Protected source | before/after fingerprint `6cdde66c5494b6d222b52ceaf2ebb89ec6c8fbc46e42c1c56b18c89b10ac2be3`; unchanged true |

`capture-r181.ps1` creates an append-only launch marker atomically with exclusive
`FileMode.CreateNew`, flushes and closes it immediately before the sole verifier launch,
and refuses any existing marker or output set. A post-launch failure
therefore records a blocked receipt and cannot be retried. The exact verifier command is
`<Assert-WriToolchain Python> tools/verify-windows-store.py`, with no arguments.
Only the runner's PHN-checked derivatives are retained. The residual sampler reads
process command lines in memory only to classify MSBuild and VBCSCompiler roles; it
publishes role, PID, parent PID, UTC creation time, count, sample UTC, elapsed time,
completion, and unreadable count. The prior receipt and PR #22 review identify the
MSBuild node and VBCSCompiler. No Razor server process name appears in the joined
verifier, runner, tests, or referenced proof. Other processes are **Not assessed**.

Prelaunch owner review found one check/create race in the first marker draft. Repair
cycle **1/2** replaces the separate absence check as the authority with exclusive,
atomic `FileMode.CreateNew`. Its bounded stub self-test observes the first callback
once, rejects the second creation, leaves the marker bytes unchanged, and never
reaches the second callback. A losing concurrent invocation exits before writing any
shared capture path; the marker owner alone can publish the result. The verifier is
not referenced by the self-test path.

## Run procedure

The reviewed wrapper was invoked once from the repository root in PowerShell 7 on
Windows x64 with the absolute Python executable selected for the run:

```powershell
& docs/proof/r181-store-verifier-rerun/capture-r181.ps1 -PythonPath '<absolute Python executable>'
```

The wrapper exited **0** and emitted `R181 CAPTURE result=PASS ... launch_count=1`.
The display scale was not changed and the Windows Settings preflight was not run.
`verifier.stdout.txt` and `verifier.stderr.txt` are PHN-checked derivatives; their
metadata retains the raw stream SHA-256 values. `residuals.json` is the safe,
structured system snapshot. The original R175 blocked receipt remains unchanged.
