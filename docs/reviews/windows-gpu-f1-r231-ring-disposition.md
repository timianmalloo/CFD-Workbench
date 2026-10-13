---
id: review-windows-gpu-f1-r231-ring-disposition
title: "Independent R231 retained-ring disposition"
type: doc
status: accepted
owner: "@pc-f1-r231-ring-review"
phase: preflight
tags: [windows, ring, evidence, independent-review]
links:
  - { to: proof-win-gpu-f1-r231-desktop-ring-investigation, rel: depends-on }
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  ACCEPT corrected retained evidence and clear the prior functional-ring HOLD
  for the review-only helper candidate. Original exit 0 is independently bound;
  budget/cost advisories remain, and no live authority is granted.
---

# Disposition

**ACCEPT the evidence; clear the functional-ring HOLD** recorded in review
`f77f1a1cda49cab6365bd967e373ac8dd685c34d` for technically accepted helper
candidate `37b96e4749a13d2fee3a7d73444d66a6a39a6403`. The exact accepted
evidence tip is `73cf89c667386225fe646115a8191e6bedc067bf`, following
`2ae90b1717c208e8d42129d5e735b92fa1a04e52`. This is review-only clearance;
it grants no live action and is not blanket readiness or permission to merge.

The initial evidence had nine full-capture and six serial-capture manifest
mismatches after text normalization. Those findings remain historical. One
coordinator-authorized evidence repair binds the already-committed canonical
bytes, adds capture-local `-text` attributes, and recovers the original ring
receipt from existing telemetry. It does not rerun the ring or change any of the
91 captured file blobs. The helper's source repair cap is unchanged; this review
performs no source repair.

# Independently verified evidence

The read-only reproducer is `docs/reviews/r231-ring-evidence-probe.py`.
`py -3 docs/reviews/r231-ring-evidence-probe.py` passes in 6.728 seconds tool
wall. It checks every manifest entry against working bytes, exact Git blob OID,
byte count and SHA-256 at the accepted evidence tip and its named original
capture commit. Full capture **85/85** and serial capture **6/6** pass with zero
mismatches; the captured blobs are identical to 2ae90b17.

| Artifact | SHA-256 |
|---|---|
| Full capture manifest | `9f77c73534ea792b7b7cf51298bcdad39761e73db3332d7c831c94719577579c` |
| Serial capture manifest | `37d617872d6d6cd2d54cddbe0b864dc1e5b1c669dbc80a85bf0d7efd8a06fde1` |
| Original ring telemetry receipt | `f12be809b8318192dded940057f7461d134179e58b6e017367c10e85635c43d9` |

**VERIFIED original result:** the receipt's two raw JSONL line hashes and two
unmodified output-string hashes match the local retained telemetry source named
by the receipt. The wrapper and aggregate item both record exit **0**, the item
is completed, and all 18 allowlisted verdict lines match that original output.
The original output ends with `all test harnesses passed`. Only the host token
in one published verdict line is redacted; raw telemetry and that token were not
copied to the review. This establishes the original recorded result rather than
inferring an exit code from test counts.

**VERIFIED functional classification:** the production Windows classifier is
reapplied to the retained logs and unchanged expected-failures manifest.

| Suite | PASS lines | Manifest-expected classifications | Unexpected |
|---|---:|---:|---:|
| Core 1/3, 2/3, 3/3 | 346 / 210 / 205 | 9 / 7 / 11 | 0 |
| Analysis 1/3, 2/3, 3/3 | 44 / 69 / 146 | 0 / 0 / 0 | 0 |
| Desktop | 776 | 33 | 0 |
| CLI | 18 | 2 | 0 |
| Python unittest | 94 tests, OK | none | none reported |

Desktop's 33 classifications include expected child-exit lines; they are not
33 distinct test failures. Expected raw child failures remain expected failures,
not individual passes. No new expected-failure exemption or classifier change
was made. All **18** declared Desktop children have exactly one completion and
timing record; the maximum is **90.4 seconds**, below 120. The actual log has
**15 partition records across seven families**: the ten main partitions named
in the review request plus two properties-cells and three status-strip parts.
All declared parts are present exactly once with consistent registration counts.
All three Core and three Analysis partitions also complete.

# Separate cost result and causation limit

**VERIFIED:** retained timing files record **207,380 ms** total, **14,421 ms**
build and **192,959 ms** net. Original telemetry records load **11.70**, zero
cost failures, **16 COST-MISS** advisories, the separate 207-second budget miss,
and final exit 0. The 60-second budget was exceeded; it is not declared met.
An independent replay of the production cost policy with the reported
uncalibrated Git Bash policy reproduces zero failures, 16 misses and budget exit
0. This replay tests the stated policy, not a fresh host/load measurement.

**INFERRED:** original cycle-1 timeouts were load/contention dependent. The
retained bounded-concurrency run establishes completion at five Desktop slots;
it does not prove the cause of the earlier failures or reproduce the earlier
concurrency. The prior functional blocker is resolved by the complete passing
bounded run, not by promoting that causal inference to fact. Cost advisories
remain for the owner; future hard cost failures, timeouts, unexpected failures
or incomplete partitions remain blocking.

# Privacy, history and authority

**VERIFIED:** the proof privacy scan passes with zero user-profile paths or
machine SIDs (one existing allowlist); binary files are skipped by that text
scanner, not claimed text-scanned. The corrected receipt publishes allowlisted
verdicts and hashes, and omits the original host token. Source/helper/tests/tools,
R230 frozen evidence, canonical authority/payload/activation, prior R228 and
historical recovery evidence, and rulings are unchanged from helper37b96e47.
Final graph/privacy/whitespace results are recorded in the audit.

No ring, test harness, WSL, registry query, cleanup, terminate, unregister,
activation, GitHub message or mail ran during this review. Only retained files,
Git blobs, local telemetry hashes and deterministic classifier/cost functions
were read. Current target/protected state and actual owned processes remain
**FLAGGED / unmeasured**. The consumed R230 attempt is not reset. Any future
live action still requires the new exact owner ruling, fresh attempt/output
identity, source/payload/authorization bindings, preserved failure/checkpoint,
required leader samples and exclusivity, independent activation rehash and all
previous lifecycle prohibitions. This disposition supplies none of that authority.
