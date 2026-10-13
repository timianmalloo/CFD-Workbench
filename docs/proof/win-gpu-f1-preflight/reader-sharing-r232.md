---
id: proof-f1-ruling-229-reader-sharing-r232
title: "Ruling 229 F1 live evidence reader-sharing repair"
type: proof-pack
status: draft
owner: "@pc-f1-r232-reader-repair"
phase: "preflight"
tags: [windows, offline, reader-sharing]
links:
  - { to: proof-win-gpu-f1-r217-packet, rel: refines }
  - { to: defect-classes, rel: documents }
review-by: 2026-10-20
review-suggested: []
summary: >-
  Offline red-first repair of the F1 preflight live evidence readers with explicit ReadWrite|Delete sharing; no live action.
---

# Ruling 229 F1 live evidence reader-sharing repair


**Goal:** repair the Ruling 229 F1 proof-reader sharing defect without running the wrapper or changing product code. **Done when:** the offline red-first test, reader-sharing control, documentation gate, and relevant safe checks pass; only the two F1 grandfathering entries are removed; the source and proof hashes are recorded. **Not in scope:** WSL, live lifecycle or cleanup, registry actions, product changes, or edits to historical attempt receipts. **Tier:** T1. **Fan-out cap:** zero. **Repair cap:** two cycles; stop after a second failed repair cycle.

## Evidence

**Verified source observation:** the old Read-GuardState used Get-Content -Raw on $Live/completed-guard.state.json; Assert-Guard used [IO.File]::ReadAllText on the append-only $Live/completed-guard-events.jsonl. tools/check-reader-sharing.py grandfathered both F1 lines by exact line hash. Ruling 229 recorded the append-reader collision with its live writer. The relevant surfaces were these two reads, the PowerShell packet self-test, the central reader-sharing control, and the existing READER-SHARE-DELETE class entry.

**Verified red before repair:** powershell -NoProfile -File docs/proof/win-gpu-f1-preflight/packet/SelfTest.ps1 exited 1 with “Ruling 229 live evidence FileStream opener missing” after the regression was added to the offline self-test and before the source change. It exercises only temporary files and parses actual functions from Invoke-Preflight.ps1; no wrapper top-level code runs.

**Repair:** Open-LiveEvidenceStream creates a read-only FileStream with FileShare.ReadWrite -bor FileShare.Delete. Read-LiveText uses a UTF-8 StreamReader with BOM detection and disposes both objects. Read-GuardState and Assert-Guard use this shared reader; their JSON parsing and complete-event selection remain in place. The test checks BOM-encoded state, ignores an incomplete trailing event, opens and appends through a concurrent writer while the reader is held, and deletes the evidence file while that reader remains open. The source-token assertion checks the fully qualified enum token rather than a partial spelling.

**Verified green:** the focused Windows PowerShell 5.1 SelfTest.ps1 passed in 0.546 s on the first source-repair run after correcting the test's enum-token assertion. tools/check-reader-sharing.py --self-test passed (9 C# + 3 PowerShell offenders caught; 8 C# + 6 PowerShell clean forms passed). The normal repository scan passed with zero unshared readers; exactly seven grandfathered lines remain, all in immutable B1/B2 proof scripts. No F1 grandfathering remains.

## Byte-exact boundary

The existing run-1, failed-live, checkpoint, and integration source-hash receipts are historical evidence and are not rewritten. They remain bound to their recorded bytes. The repaired packet source and self-test are staged with canonical Git blobs verified from the index: Invoke-Preflight.ps1 blob ab47d52e450099c2f6a82feb8a6cf7027b44525c, 33,057 bytes, SHA-256 3bc3be1b64e8d5e2aec5c18b6817494ece30a82c7e12852b9f375414048534c4; SelfTest.ps1 blob 8605616014058b45a1fa4afb7c553e3122bd742c, 14,043 bytes, SHA-256 ee263c1ff382293ace76be40f3ae89c8ee30be46e54fe5e3dba72b002b48f953. The final commit check must independently compare each value with its committed Git blob. No capture log, checkpoint, recovery output, source hash receipt, or live evidence file is regenerated.

## Full-ring disposition

Two `tools/run-tests.sh` attempts were made, with no source, test, or harness changes between them. Both used Git Bash MINGW64 and user-local .NET SDK 10.0.203. Attempt 1 used the standard Desktop concurrency and exited 1: Desktop reported 18 unexpected findings alongside 673 PASS, 120-second child timeouts, drain failures, and incomplete partitions. Core and CLI failures listed in the Windows expected-failure manifest were classified as expected; Analysis and Python passed. The first run alone did not establish whether those Desktop findings were deterministic or load-related.

Attempt 2 was the one authorized bounded-concurrency rerun: `DOTNET_PROCESSOR_COUNT=8` (five Desktop slots), `CFD_TEST_BUDGET_SECONDS=60`, and `CFD_TEST_CHILD_TIMEOUT_SECONDS=120`. It exited 0 and printed `all test harnesses passed`. Desktop completed with 776 PASS and 33 manifest EXPECTED-FAIL, with no unexpected findings, timeout, or incomplete partition. Core parts had 346/210/205 PASS and 9/7/11 manifest EXPECTED-FAIL; Analysis had 44/69/146 PASS and no failures; CLI had 18 PASS and 2 manifest EXPECTED-FAIL; Python had 50 PASS. The functional disposition is **GREEN** for this ring attempt. The changed processor bound coincided with the resolved Desktop failures, but two runs do not prove that concurrency was the cause.

The cost verdict is separate from functional status. Attempt 1 reported 0 cost-check failures and 16 COST-MISS advisories, 170 s wall against 60 s, and load 11.67 → 17.19; its exit 1 was the functional Desktop RED. Attempt 2 reported 0 cost-check failures and 17 COST-MISS advisories, 191 s wall (190933 ms, net 188819 ms) against 60 s, and load 11.20 → 13.45. It printed `COST-MISS TEST-BUDGET 191 s` and still exited 0. Recommendation: accept attempt 2 as functional evidence, retain the separate ring-budget miss for cost review, and do not run a third ring attempt under this two-attempt limit.

All ring output files were preserved in user-local temporary captures before the next command could overwrite `.tmp-tests`: attempt 1 has 80 files and attempt 2 has 116 files. These private captures are not committed. `src/` and `tests/` are unchanged.

## Verification record

| Check | Result |
|---|---|
| Red-first PowerShell regression before source repair | **RED**, exit 1: missing FileStream opener |
| Focused PowerShell packet self-test after repair | **PASS**, 0.546 s |
| py -3 tools/check-reader-sharing.py --self-test | **PASS**, planted offenders caught and clean forms accepted |
| py -3 tools/check-reader-sharing.py | **PASS**, zero unshared readers; seven frozen B1/B2 lines grandfathered |
| py -3 tools/check-docs.py | **PASS**, exit 0; 134 pre-existing review-suggested advisories, no graph defects/orphans/index drift |
| Full test ring attempt 1 | **RED**, exit 1: Desktop 673 PASS and 18 UNEXPECTED; child timeouts at 120 s (124), drain failures (125), and incomplete partitions. No failure was suppressed or retried. |
| Full test ring attempt 1 cost | 170 s wall against 60 s; load 11.67 → 17.19; 0 cost failures and 16 COST-MISS advisories. |
| Full test ring attempt 2 | **PASS**, exit 0: all harnesses passed; Desktop 776 PASS, EXPECTED-FAIL 33; no unexpected findings, timeouts, or incomplete partitions. Other suite results are recorded above. |
| Full test ring attempt 2 cost | 191 s wall (190933 ms, net 188819 ms) against 60 s; load 11.20 → 13.45; 0 cost failures and 17 COST-MISS advisories, including `COST-MISS TEST-BUDGET`. |
| Full test ring disposition | Functional evidence green on bounded attempt 2; retain budget miss as a separate cost finding. Two attempts reached the cap; no third run. |
| Invoke-Preflight.ps1 committed blob | ab47d52e450099c2f6a82feb8a6cf7027b44525c; 33,057 bytes; SHA-256 3bc3be1b64e8d5e2aec5c18b6817494ece30a82c7e12852b9f375414048534c4 |
| SelfTest.ps1 committed blob | 8605616014058b45a1fa4afb7c553e3122bd742c; 14,043 bytes; SHA-256 ee263c1ff382293ace76be40f3ae89c8ee30be46e54fe5e3dba72b002b48f953 |

**Limit:** offline sharing tests establish behavior on the Windows host and the file handles used by the fixture. They do not authorize or claim a live preflight, cleanup, registry operation, or acceptance of historical evidence.
