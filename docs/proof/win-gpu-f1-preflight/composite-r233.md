---
id: proof-f1-composite-helper-reader-r233
title: "Prepared composite F1 helper and reader repair"
type: proof-pack
status: draft
owner: "@pc-coordinator"
phase: preflight
tags: [windows, offline, integration, reader-sharing, conservation]
links:
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: defect-classes, rel: documents }
review-by: "2026-10-20"
summary: >-
  Prepared unauthorized composite of the exact accepted cleanup-helper ancestry
  and the accepted reader-sharing delta; new packet blobs require fresh review.
  Protected parent source/tests/evidence are conserved; no live action occurred.
---

# Candidate contract and scope

**PREPARED-UNAUTHORIZED.** This is the second and final reader integration cycle,
authorized offline by the coordinator after the original exact-blob join was
found incompatible. It creates new packet blobs for fresh independent Astra
review. It is not a live ruling, activation, cleanup retry, or main promotion.

Goal: reapply only the accepted reader behavior/tests/control from
`70d022bf4369c8cb58888f88e6efba00070f35db` onto exact helper candidate
`37b96e4749a13d2fee3a7d73444d66a6a39a6403`. Done when: focused checks,
documentation/safe gates, required functional ring and exact conservation are
recorded, and the clean review candidate is committed/pushed. Not in scope:
WSL/live/lifecycle/registry action, authority fabrication, unrelated repairs,
semantic-conflict resolution, a third cycle, or silent acceptance of blob drift.
Tier T0; one author; no delegation. Graph: exact ancestry -> accepted delta
replay/red-first -> focused/conservation checks -> docs/functional ring -> audit,
commit/push and review handoff. Gates may run in parallel only once source edits
are complete. A failed focused union permits at most one correction; none has
been used. The ring is one bounded invocation, processor count 8, with no rerun
authorized by this composite contract.

# Why whole-branch merging was stopped

The original unused integration branch was based on
`64dbed61b68364af6f3bf12ac986809abac285ee`. Its merge base with the helper
candidate was `04354ecf0b1cc6ac6fffcab16dba4e5b34b34014`, so moving to the helper
candidate could not fast-forward. With the revised explicit base instruction,
the clean unused local branch was moved to exact `37b96e47`; `64dbed61` remains
in the retained main history. No work or unique local commit was discarded.

The helper ancestry's packet includes R225 authority, created-GUID, containment,
final-absence and no-unbound-host-delete controls that are absent from the reader
candidate's older main-based packet. Taking the reader's entire packet blob would
remove those controls; a blind merged blob would lack exact reader acceptance.
No whole source conflict was resolved. The coordinator instead authorized replay
of only four reader deltas, with fresh review of the resulting packet blobs.
No review commit was imported as a parent. Reader origin is provenance, not an
assertion that its whole branch is part of this ancestry.

# Exact replay and end-to-end surfaces

The four-file delta from `70d022bf^` to `70d022bf` was applied through Git's
context-checked patch tooling onto helper ancestry. The accepted `SelfTest.ps1`
addition was applied first: Windows PowerShell 5.1 was red, exit 1, with
`Ruling 229 live evidence FileStream opener missing`, before source changes.
The accepted wrapper/control delta then applied without conflict or correction.

Surfaces: `Open-LiveEvidenceStream` -> `Read-LiveText` -> actual Read-GuardState
and Assert-Guard readers -> AST-loaded offline packet self-test -> central reader
sharing scan -> docs gate. The opener uses FileAccess.Read with explicit
FileShare.ReadWrite | FileShare.Delete. The UTF-8 BOM-aware text reader disposes
reader and stream. State parsing and complete trailing-event selection stay
unchanged. Tests hold a reader while appending and deleting through another
handle, exercise BOM state and incomplete final JSONL data, and parse actual
production functions without executing wrapper top-level code.

Only the two F1 grandfather rows are removed. Seven frozen B1/B2 rows remain,
byte-identical to the accepted reader delta. Every cleanup helper, unknown-root
control, R225/R230 authority and ownership source/test file outside the four
replayed paths remains byte-identical to helper ancestry.

# Deterministic conservation

`verify_composite_r233.py` reconstructs the accepted four-file reader delta in a
temporary repository seeded with exact helper-base blobs. It compares expected
Git blobs with the staged candidate and actual worktree bytes. It separately
compares all other tracked parent blobs except the four permitted metadata views
(audit JSONL/JS, docs index, defect register), and conserves all parent audit ids
and entry values plus every parent register line, including multiplicity.
It performs Git/file operations only, never WSL or live reads. Initial replay
measured 1.516 seconds; final proof with register checks measured 0.656 seconds.
This on-demand offline-fast proof is not a new ring
gate. Failure oracle: any target blob other than exact delta replay, any changed
protected blob, or any lost/modified/duplicate parent audit record fails.

Recorded result in `composite-r233-conservation.json`: PASS, four exact target
blobs, **5,379 protected parent blobs**, **1,087 parent audit entries**, and all
**2,897 parent register lines**. An incidental register transcription was restored
from its exact parent line before this check; the line-conservation assertion now
prevents a changed unrelated paragraph from passing. Accepted
patch SHA-256 `5b33ae81e5cd3337ab713536b37357b56879db9e90e325b9b1a3a338b1945c4e`.
The JSON names the staged capture; `--head <candidate>` independently rechecks
the committed candidate without rewriting that capture.

| Composite path | Git blob | SHA-256 |
|---|---|---|
| packet/Invoke-Preflight.ps1 | d53b5d85c92e976b483c2e7e57a895ca4777afc1 | fec35b23017fb96a6496de331c0dcf7a9a4d54dfb8e230ed7fade7c1d0df7029 |
| packet/SelfTest.ps1 | d4caaf28cafa4f9d1a6840169d1e1bce74904f92 | f97bcfac7fdb292759a5d8bf9f8a45cd85ee14dd41ad9e21ae048ed27fcf4f1a |
| tools/check-docs.py | 74e329f98c811341b486561e66c4984ed9795082 | d0078df2e6ae5a4d0f7fb748c11542ba76d363debeec8c1ca82207bdc6954110 |
| tools/check-reader-sharing.py | 343ab7be89213cc9c79e91f322b0a8ac73431bb6 | 13b9a2847e0b9dc5ca5b9e66ee8f073887d1c052dc88e9ccf6c28eba5cb716bd |

Wrapper bytes: 36,278; self-test bytes: 14,176. Their helper-base blobs are
`ebe8f5995591d3a63abb8f4e91a71449404524b0` and
`dc08f5676afa69ccd90adb9966c0b417bb932ff1`. The two tools' composite blobs are
identical to reader origin. The cleanup wrapper/helper/test blobs remain the
accepted helper candidate's bytes. Historical authority and frozen R230 captures
are retained; their old hashes do not authorize this composite source.

# Verification and limits

Verified focused results: Windows PowerShell 5.1 SelfTest PASS, 0.470 seconds;
PowerShell 7 SelfTest PASS, 0.354 seconds; helper suite 13/13, 0.204 seconds;
Python union 94/94, 9.546 seconds; Python packet planted-failure self-test PASS,
0.681 seconds; reader static self-test PASS and normal scan zero unshared readers,
seven B1/B2 grandfather rows. These are offline observations. Live WSL/DrvFs
sharing behavior and original live failure causation are not promoted by them.

Repository `tools/join-ring.sh` is used for the functional ring; non-merge HEAD
fails safe to running `tools/run-tests.sh`, and tests/tools changes independently
trigger that policy. The invocation uses existing user-local SDK 10.0.203,
DOTNET_PROCESSOR_COUNT=8, 60-second budget and 120-second child deadlines. The
single ring completed **functionally GREEN**, exit 0, with `all test harnesses
passed`. No source, test, or harness changed during its invocation. Release build
passed in 16 seconds. Core parts: 346/210/205 PASS, with 9/7/11 manifest
EXPECTED-FAIL; Desktop: 776 PASS, 33 EXPECTED-FAIL; Analysis: 44/69/146 PASS,
zero failures; CLI: 18 PASS, two EXPECTED-FAIL; Python: 94 PASS. There were no
unexpected findings, timeouts, or incomplete partitions. No rerun was made.

**Separate cost finding:** wall 211 seconds (210689 ms; net 194520 ms), against
60 seconds; measured load 6.92 -> 13.97. Cost checker reported zero failures and
14 COST-MISS advisories, followed by `COST-MISS TEST-BUDGET 211 s`. Functional
exit 0 does not mean the performance budget passed. Processor bounding coincided
with green behavior; this one run does not establish that concurrency caused
the earlier failures. Cost disposition remains for the coordinator; no new
optimization or test exemption is introduced here.

Final documentation/safe-gate results are recorded in the closing measured audit.

Independent acceptance is pending. No live attempt may follow this prepared
candidate. A new owner-assigned ruling would need fresh reviewed source and
authority hashes, exact attempt/output identity, preserved failed R230 binding,
and all admission constraints. This document grants none of them.
