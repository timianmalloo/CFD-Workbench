---
id: review-windows-gpu-f1-r233-composite
title: "Independent acceptance of the exact F1 composite repair"
type: doc
status: accepted
owner: "@pc-f1-r233-repairs-review"
tags: [windows, offline, review, conservation]
links:
  - { to: proof-f1-composite-helper-reader-r233, rel: depends-on }
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  Independent ACCEPT of b74af1e for offline integration only. Helper controls,
  reader sharing, protected history and the single retained functional ring
  are verified; budget misses remain recorded and no live authority is granted.
---

# Verdict and exact scope

**ACCEPT**, independently, for candidate
`b74af1e488e7d7bd3fd7c7901d2940da10fa4c07`. Its sole parent is accepted helper
`37b96e4749a13d2fee3a7d73444d66a6a39a6403`. The four-file reader delta comes from
`70d022bf4369c8cb58888f88e6efba00070f35db`; that commit is provenance, not a
replacement for the helper ancestry. This final composite cycle required no
source repair and leaves no code veto open. The author did not clear this verdict.

The reviewed surfaces are helper loading and unknown evidence, cleanup action
and receipt diagnostics, admission and exact identities, final absence, packet
evidence readers, tests/static controls, immutable history, and retained ring
evidence. This review grants no live authority, cleanup retry, activation, or
main readiness. Existing authorization hashes do not authorize the new packet
blobs. A future live operation still requires the designated leader's exact
ruling and all applicable identity, lease, one-attempt and stop requirements.

# Verified conservation and production controls

The author's replay verifier and the independent
[evidence verifier](r233-composite-evidence.py) both pass. The latter verifies
Git modes and blob identities for all **5,379** protected parent paths, the
unaltered raw prefix of **1,087** audit entries, and all **2,897** parent defect
register lines in order. Exactly three new proof files and the declared four
reader paths plus four metadata paths account for the candidate delta.

Every helper/cleanup/authority payload and frozen R230 evidence blob remains
identical to the accepted helper parent. The wrapper retains its newer
authorization, RunId, GUID, protected identity, and final-absence controls.
Exactly the two F1 reader grandfather entries are removed; seven unrelated
B1/B2 exceptions remain. Both tool blobs equal the accepted reader origin.
The replay patch SHA-256 is
`5b33ae81e5cd3337ab713536b37357b56879db9e90e325b9b1a3a338b1945c4e`.

| Composite blob | Git blob | SHA-256 |
| --- | --- | --- |
| Invoke-Preflight.ps1 | `d53b5d85c92e976b483c2e7e57a895ca4777afc1` | `fec35b23017fb96a6496de331c0dcf7a9a4d54dfb8e230ed7fade7c1d0df7029` |
| SelfTest.ps1 | `d4caaf28cafa4f9d1a6840169d1e1bce74904f92` | `f97bcfac7fdb292759a5d8bf9f8a45cd85ee14dd41ad9e21ae048ed27fcf4f1a` |

The explicit read-only FileStream shares ReadWrite and Delete. Nested disposal
covers reader construction and read exceptions. UTF-8 default/BOM detection,
four tested encodings, and complete-event validation are preserved. An open
writer, rename/delete, and path replacement are exercised against the actual
production functions. The former reader is observed failing the writer-open
control, so the fixture distinguishes the defect from its repair.

# Independent execution

These are offline focused checks, not a repeated full ring. The copied probes
preserve the accepted earlier probe bytes and their original output labels.

| Command or control | Observed result |
| --- | --- |
| `verify_composite_r233.py --head b74af1e488e7d7bd3fd7c7901d2940da10fa4c07` | PASS; replay and conservation, 0.767 s |
| `py -3 docs/reviews/r233-composite-evidence.py` | PASS; stronger byte/order conservation and original ring binding |
| `py -3 -m unittest discover -s tests -p test_f1_cleanup_owned_check.py -v` | 13/13 PASS, 0.162 s |
| `py -3 -m unittest discover -s tests -p test_*.py` | 94/94 PASS, 9.773 s |
| Packet `SelfTest.ps1`, Windows PowerShell 5.1 and PowerShell 7 | PASS, 0.423 / 0.336 s |
| `tests/f1_attempt_controls.ps1`, `tests/f1_attempt_absence.ps1`, both runtimes | PASS; production authority/identity negatives; zero WSL calls |
| `r233-reader-probe.ps1`, both runtimes | PASS; old-reader red, four encodings, writer/rename/delete, four invalid events, constructor/read disposal; zero live calls |
| `r233-helper-probe.py` | PASS; missing/file/unreadable roots × stage/guard refuse with exit 5, empty stdout, exact sanitized UNKNOWN, zero process calls |
| `tools/check-reader-sharing.py --self-test` and scan | PASS; 9+3 offenders / 8+6 clean fixtures; seven retained exceptions |
| Safe bundle | 10/10 PASS; configured application-core, application-adapters and Windows-store skips retained |
| `py -3 tools/check-docs.py` | PASS, exit 0; 17 capture manifests match committed blobs; graph zero defects, 134 existing review suggestions |
| Staged whitespace and machine-path gate | PASS |

The helper union also retains loader, process identity, diagnostic sanitization,
and Cleanup.action/receipt propagation fixtures. Zero-live-call statements refer
to the instrumented offline probes and the commands run in this review; no WSL,
registry inspection, lifecycle operation, cleanup attempt or ring rerun occurred.

# Single retained ring: functional success and measured cost

The [sanitized receipt](r233-composite-evidence.json) independently hashes the
existing local capture files. It binds the original execution record at line
720 of telemetry basename
`rollout-2026-10-12T20-35-42-01a13354-8f61-7582-9b9a-13b253936fd9.jsonl`:
`$.payload.item.exit_code` is **0**, status is completed, and the command argv
contains the ring entry point. This is the original execution, not a later
command that printed its output. Raw-line SHA-256 is
`8bc1291fa1f3d7ab34b9d7bc26c45e7b52b86fb4b1d794d343479a78d24e6822`;
output UTF-8 SHA-256 is
`ddca8d45e4c1b7be39913154b6492b07c5d82f82e45f40695421c0211c429425`.
Only metadata, hashes and allowlisted verdict lines are published.

Verified retained outcomes: Core parts **346/210/205** passing checks with
**9/7/11** expected failures; Desktop **776** passes, **33** manifest-expected
failures and **0** unexpected failures; Analysis parts **44/69/146** passes;
CLI **18** passes and **2** expected failures; Python **94/94** passes.
All **18** unique Desktop children completed, including **15** partitions in
seven complete families; the longest child was **90.3 s**, below the configured
120 s child timeout. The original top-level output says all harnesses passed.

Wall time is **210,689 ms**, build **16,169 ms**, net **194,520 ms**. The original
receipt records **14 COST-MISS**, zero cost failures, load **6.92 → 13.97**, and
the **211 s** summary exceeding the **60 s** ring budget. Those cost misses
remain advisory evidence, not green budget compliance. Earlier ring failures
remain historical facts; this execution establishes neither their cause nor a
general performance cure.

# Limits and disposition

**VERIFIED:** the exact composite preserves the accepted safety surface and
passes the independent controls above; retained ring evidence is bound.
**FLAGGED:** the ring budget and advisory costs remain missed. **INFERRED:** no
claim is made about the cause of earlier timing failures or any live target
state. Local capture availability is needed to replay the receipt verifier.
This acceptance is limited to the immutable candidate and offline integration.
