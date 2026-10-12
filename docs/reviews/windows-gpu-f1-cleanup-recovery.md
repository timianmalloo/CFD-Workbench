---
id: review-windows-gpu-f1-cleanup-recovery
title: "Astra independent review of F1 cleanup-only recovery"
type: proof-pack
status: complete
owner: "@pc-f1-r217-cleanup-recovery-astra"
phase: preflight
tags: [windows, wsl, recovery, independent-review, veto]
links:
  - { to: note-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: proof-win-gpu-f1-r217-packet, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  VETO candidate 02ec2cc3: an expired deadline still launches a child, exact target
  GUID survival can pass the absence check, and a final-check error loses the
  initiating deletion error. Historical evidence is byte-identical and bound.
---

# Independent verdict

**VETO** source commit `02ec2cc3aaccc2da2b8ce63fe5f4790ce26839d3`.
The supplied tests pass, but independently planted boundary cases violate the
proposed deadline, exact-identity absence and error-retention contracts. This
review grants no authority. No WSL command, recovery entry point, deletion,
unregister, download, package install, source acquisition, build or CFD workload
was executed. The original execution worktree was read only.

The reviewer is structurally separate from the author. Security, SRE and Test
Architect adversarial lenses cover authority, destructive boundaries, timing,
ownership and evidence truth. Rulings 217 and 220 were read; neither supplies new
cleanup-recovery authority. The proposal correctly calls for a new Mac ruling.

## Blocking findings

All three findings below are **Verified, high confidence**, using the exact
candidate Python functions with synthetic inputs or a mocked process boundary.
The reviewed source was not repaired.

| ID | Severity | Exact source and observed failure | Required acceptance condition |
| --- | --- | --- | --- |
| CR-1 | High | `recovery/recover.py:178` calls `subprocess.Popen` before `self.remaining(cap)` at line 181. With `start = monotonic() - 601`, the real `command()` called the mocked launcher once with `['wsl.exe', '--unregister', TARGET]`, then raised the exhausted-deadline error. A timeout is therefore checked after a destructive request can begin. `start_guard()` lines 292-294 has the same ordering. | Reject an expired deadline before every launch; retain one absolute deadline through launch, waits, teardown and evidence binding. A planted expired-deadline test must observe zero launches, including lifecycle and guard paths. |
| CR-2 | High | `recovery/recover.py:121` checks absence by target name, list name and the original path only. Passing a row with `key=TARGET_KEY`, a different name and a different path, alongside the unchanged protected row, returns normally. The original target registry identity survives while the function declares absence. | Reject any surviving target GUID, target name or target path across all registrations. Retain the exact protected identity comparison. Add renamed/moved-identity fixtures and assert refusal. |
| CR-3 | Medium | `cleanup_sequence()` line 136 replaces an initiating deletion exception if `final-owned-check` also raises. `run()` line 428 retains only `str(error)`. A real sequence probe raising `initiating-delete-failure` and then `final-check-failure` exposes only the latter to the receipt path. Python exception context exists, but is never serialized. | Record both errors independently while keeping final stop/reap unconditional and unregister blocked. A dual-failure fixture must assert both errors in the eventual receipt. |

Reproduction results from the independent offline probe:

```text
EXPIRED-DEADLINE-PROBE launch_count=1 error=600-second recovery deadline exhausted; stop for Mac
DUAL-FAILURE-PROBE receipt_error=final-check-failure initiating_error_retained=False
ABSENCE-GUID-PROBE renamed exact target GUID accepted as absent
```

The probe imported `recover.py` without invoking `main()` or `Recovery.run()`.
For CR-1 it constructed a `Recovery` instance without initialization, supplied a
temporary output directory, set its clock 601 seconds in the past, and replaced
`Popen` with an already-exited mock child. For CR-2 it called
`validate_absence([protected, renamed_target], [PROTECTED, 'renamed-target'],
False, protected)`. For CR-3 it called the actual `cleanup_sequence` with an
action that raised distinct exceptions on deletion and final ownership checking.
These probes measure Python control flow; they do not claim WSL fidelity.

## Evidence preservation and binding

**Verified, high confidence:** the original `Save-Checkpoint` contains 75 payload
files plus its original manifest. Every payload byte, size and SHA-256 equals
the committed `run-1/checkpoint` copy. The manifest itself is byte-identical.
The original `run-1/live` contains exactly the 93 payload files listed in the
new retained inventory; every byte and size equals `run-1/failed-live`.

| Inventory | SHA-256 | Real packet Git verification |
| --- | --- | --- |
| Original freeze manifest | `76ab6e220a21c10c694a7eecd9d00679d1a0a42b3444598dae2682ac627f0bea` | PASS: 76 blobs including manifest |
| Retained live inventory | `56470fa05eaeca14327ce1e63a4f76f3127b2dcbbe3a94d093684037197f8e93` | PASS: 94 blobs including manifest |

Both checks used `packet/manifest.py verify --commit
02ec2cc3aaccc2da2b8ce63fe5f4790ce26839d3`, not just working-tree hashes.
The old receipt still reports `stage-simulate exited 5`, the checkpoint
`Substring` error, unproved checkpoint and withheld cleanup. No F1 closure,
capacity or acceptance result was inferred from these captures.

## Remaining contract assessment

**Verified by source inspection; high confidence unless qualified:**

- Authority is absent by default. No accepted recovery authorization is supplied.
  The exact authorization dictionary requires a new ruling above 220, the
  reviewed source commit, recovery session, epoch, identities, freeze hash,
  three allowed actions and a 600-second bound. `precheck()` binds the committed
  authorization bytes and hash in the numbered ruling, checks source ancestry,
  executable source bytes, clean status and the existing live-Mac-epoch helper.
  This is an executor contract check, not independent authentication of the human
  who authored a future ruling; Mac must supply that governance decision.
- `cleanup_sequence()` verifies both committed manifests and original evidence
  before identity or destructive actions. The original 75-file manifest has a
  fixed hash. Retained-live entries are compared against the original source.
- Fresh identity admission checks the exact distro names, GUIDs, version 2,
  BasePaths, list/registry agreement and nonlinked host ancestry. The accepted
  source names target `{e958c4d5-ae78-4b50-a634-7102f334d3aa}` and protected
  `{b7566a82-9cee-47ce-a246-f83632353cea}`. Public protected paths use
  `%USERPROFILE%`; fresh live identity was intentionally not queried here.
- Original stage and guard ownership checks call the packet's exact argv and
  PID/PGID/SID implementation. A new full protected snapshot must equal the
  retained terminal hash. The fresh guard checks session, expected hash,
  complete event/state agreement, continuation and freshness. Guard checks
  surround deletion and the exact terminate/unregister commands; success also
  requires a later guard row and an equal full protected snapshot.
- Deletion dispatch is fixed to `delete-debs` for the original run ID. The
  cache admission refuses linked ancestry, unexpected entries, device changes
  and intersecting mountpoints. Recovery has no simulate, download, install,
  source, build or CFD dispatch, and no recursive host-path deletion fallback.
- Final stop/check is called from deletion's `finally`; its failure prevents
  unregister. The implementation begins this action with a fresh identity check,
  so identity failure can still prevent the stop helper itself from running.
  That outcome must remain unknown, not be represented as successful teardown.
- Final registry/list/path checks and protected invariance exist, but CR-2
  prevents accepting the exact-target absence claim. Separate recovery receipt,
  freeze, Git commit and committed-blob verification exist. Historical receipt
  bytes are preserved. The `recovery_success` field is written before final
  freeze/commit/verification: consumers must require `RECOVERY-BINDING-PASS` and
  its verified commit, not treat that earlier field alone as final acceptance.

**Flagged; not closed by this review:** cache admission and `find ... -delete`
are separate guest operations. There is no lock, pinned directory handle or
fresh deletion-time ancestry check. Concurrent replacement of an ancestor or
mount can invalidate admission. Likewise registry/name/path checks and WSL
lifecycle calls are not atomic. These are explicit residual risks in the
proposal; this review has not established the exclusive machine ownership
needed to accept them. A recovery deletion also has no prelaunch `.intent.json`
like the original wrapper: its missing-entry/late-start boundary is not covered
by the 16 tests and needs an explicit unknown-state admission test before any
future live acceptance. No claim of current cache/process/WSL state is made.

## Independent verification

All commands below ran offline on the review branch. Supplied tests exercise
mocked process boundaries and real temporary files; they are not live WSL proof.

| Check | Observed result |
| --- | --- |
| `tests/test_f1_cleanup_recovery.py` | 16/16 PASS; measured 0.417 seconds |
| `packet/packet_selftest.py` | PASS, including ownership, exact argv, authority contracts and transaction fixtures; 0.526 seconds |
| Windows PowerShell `packet/SelfTest.ps1` | PASS; 0.307 seconds |
| `packet/authority_scan.py` | Planted negatives PASS; packet scan PASS |
| `packet/manifest.py self-test` | PASS |
| `packet/completed_guard.py self-test` | PASS |
| Original capture byte comparison and both committed manifests | PASS, counts and hashes above |
| Independent CR-1/CR-2/CR-3 counterexamples | Reproduced all three defects |
| Offline-safe `run-verify-gates.py` bundle | 10/10 PASS; application-core, application-adapters and Windows-store gates explicitly excluded |
| `packet/transaction.py self-test` | PASS |
| Candidate `tools/check-docs.py` | PASS; 131 existing freshness suggestions |
| `tools/check-proof-pii.py` | PASS; zero raw Windows user paths or machine SIDs, one existing allowlist, 365 binary files skipped |

The authority scanner covers `packet/`; recovery's narrower dispatch was
inspected separately. The safe gates passing do not clear the three findings.
Docs, graph and proof-PII closing results are recorded in the audit entry.

## Review execution graph and residual risk

Goal: independently accept or veto the exact candidate. Done when a decisive
review, measured offline evidence, derived index and audit are committed and
pushed. Not in scope: implementation repair, any live action or new authority.
Tier T2; one reviewer, no further agents. Surface list: original captures →
manifests/Git blobs → ruling/authorization → identity and ownership → guard →
cache/lifecycle effects → absence/invariance → receipt and separate commit.
Product domain model and UI are unchanged.

The graph was ground → inspect/bind → independent refusal probes and offline
gates → verdict → derive/audit → commit/push. Data and decision edges retain
every review floor. Independent read-only checks were batched with width at
most three; no agent fan-out or implementation repair loop occurred. Every
check has a finite input set and a pass/fail oracle. Planned and actual six
nodes agree; the review identified defects rather than repairing the candidate.
No speedup, token cost or whole-run timing claim is made. Audit instrumentation
measures from its recorded marker, which was created after initial grounding.

Residual risk is **unaccepted**: real WSL child startup/group teardown, stale or
reused process identities, UTF-16 listing, mutable registration/path and cache
boundaries, protected runtime invariance, privacy of future raw subprocess
errors, and incomplete stopping/binding at the deadline remain unobserved.
The three reproduced defects are known failures, not residual uncertainty.
The next decision belongs to the Mac/operator. No automatic repair or live
retry follows this VETO.
