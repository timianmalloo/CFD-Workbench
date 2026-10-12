---
id: review-windows-gpu-f1-live-defect-repair
title: "Astra review of F1 live-defect packet repair"
type: proof-pack
status: complete
owner: "@pc-f1-r217-live-defect-astra"
phase: preflight
tags: [windows, wsl, gpu, ruling-217, ruling-220, owner-review]
links:
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: review-windows-gpu-f1-preflight-packet, rel: relates-to }
  - { to: review-windows-gpu-f1-environment-contract-r220, rel: relates-to }
  - { to: coordination-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-12"
summary: >-
  ACCEPT immutable repair 20e77e34 for the two reproduced URI/scoping failures.
  Independent offline checks prove the repaired matching and checkpoint integration.
  Recovery and a future retry remain separate decisions; the old contract cannot bind these bytes.
---

# Independent verdict

**ACCEPT** repair commit `20e77e34259ce68637f5374f3a08acd948fab3af` for the
two reported live defects. Both old failures were independently reproduced against
parent `6445bb6e1c6b9f27969724c711cc2ff322f01867`; the repaired paths pass the
complete offline suites and additional adversarial fixtures. No source-repair
hard veto remains. Confidence: **Verified** for the observed host-only behavior;
no repaired live execution or cleanup result is claimed.

This is sufficient repair of these two failures for a future retry candidate.
It is **not sufficient authority or environment binding to start that retry**.
No recovery, deletion, unregister, retry, installation, source acquisition, build,
or case execution was performed or authorized by this review. Ruling 217
(`docs/notes/rulings.md:1699`) and the serial plan's one-attempt/no-retry boundary
(`docs/coordination/windows-gpu-f1-preflight-r217.md:51`) remain material limits.
Ruling 220 (`docs/notes/rulings.md:1717`) is not reinterpreted as another live attempt.

Goal: independently decide whether the exact repair fixes both observed failures
without changing previously cleared safety controls. Done when: reproduced reds,
reviewed integration, offline gates, this verdict, audit, commit and push are
complete. Tier T1; fan-out zero. Only this review and derived audit/index files are
authored. Graph: source/diff grounding → old-failure reproduction → repaired
integration/negative tests → verdict → docs/gates → audit/commit/push. Independent
read-only checks overlap; mutations remain serial. The surfaces are URI text →
signed-index basename/size identity → lock fields, and fixed checkpoint path →
dynamic callback scope → manifest freeze → scoped Git commit → manifest binding →
cleanup permission. UI, product code and live proof are outside scope. The closing
audit records measured total duration; no modeled speedup is claimed.

## Checklist and evidence

All packet paths below are under `docs/proof/win-gpu-f1-preflight/packet/`.

| Gate | Observed evidence | Verdict |
| --- | --- | --- |
| A: reproduce the original failure | Executed the old `transaction.py` module from the parent Git blob with the captured cpp-13 spelling, `%7e` URL and 1042 bytes. `match_uri` raised the exact `found 0` error. | Verified PASS |
| A: decode only the basename, once | `transaction.py:131-139` splits the raw URL path at its last slash, validates percent syntax, then calls strict `unquote` once. It does not use `unquote_plus`. Encoded slash/backslash, controls, invalid UTF-8, empty/dot names fail closed. | Verified PASS |
| A: preserve authority and identity | `transaction.py:142-147` retains exact size and exactly-one-match; the result returns the original cache key and raw URL. `:157-166` keeps signed-index filename/hash/size and raw URL/cache identity as distinct lock fields. | Verified PASS |
| A: boundary fixtures | `packet_selftest.py:68-93` covers the cpp-13 row, both cases of `%7e/%2b/%3a`, literal plus, one-pass decoding, wrong size, duplicates and malformed/unsafe input. Independent checks also reject two differently encoded URLs with the same decoded name and equal size; a size of `2**63` matches exactly and `2**63-1` does not. Additional incomplete escapes, overlong UTF-8, encoded DEL/newline and dot segments reject. | Verified PASS |
| B: reproduce dynamic scope, including Substring | Ran the new actual-body fixture with old `Control.ps1` and old wrapper source in an external temporary directory. Kept the real repository-length root and removed only the fixture's early path assertion so the original downstream call executed. The real `Invoke-GitCommit` threw `Substring: startIndex cannot be larger than length of string`; finalization reported checkpoint unproved and withheld deletion/unregister. | Verified PASS |
| B: fixed canonical path and callback names | Wrapper `:27-30` derives `CheckpointPath` from resolved repository/run paths; `:258-260` passes it to both freeze and Git binding; `:429-430` checks that same path before effects. `Control.ps1:31-37` uses six distinct `*Action` parameters; legacy named aliases remain compatible with wrapper `:539-545`. No finalizer variable now shadows `CheckpointPath`. | Verified PASS |
| B: actual integration, not callback count alone | `SelfTest.ps1:40-97` loads the actual path assignment, `Save-Checkpoint` and `Invoke-GitCommit` AST bodies and calls them through the real finalizer. It proves canonical freeze and verify directories, exact Git add relative path, commit ordering and retained SHA. Real wrapper `:237-248` performs the unchanged Substring conversion and post-commit manifest verification. Both PowerShell 5.1 and 7 pass. | Verified PASS |
| B: failure and cleanup permission | Independent external fixtures inject child failures at freeze, Git add, Git commit, SHA read and manifest verify in the actual-body chain. All five cases on both PowerShell versions report checkpoint unproved plus Mac handoff, never enter destructive cleanup, and preserve owned stop/guard stop/bind. Existing `SelfTest.ps1:141-170` covers checkpoint/snapshot/guard failures. | Verified PASS |
| Preserve previously cleared safety | Diff against accepted `d5bb68fe7db5e511ac46708e14564bb6a738db37` changes only six packet files: the three repaired source files, two suites and README. `Control.ps1:46-67` retains the unconditional fresh final reap; ownership, identity, authority, deadline helpers, contract, manifest, guard and shell stages are otherwise unchanged. Full planted-failure suites still pass. | Verified PASS |
| Class sweep and prevention | Repeated URL-reader and scriptblock-parameter searches across `docs/proof`, `tools`, `tests`. B2 already uses `unquote`; F1 was the undecoded matcher. Actual F1 callback/path collision is removed. `docs/lessons/defect-classes.md:2783-2789` records both classes and executable controls; no wider repair claim is inferred from the search. | Verified PASS |
| Claims and historic bindings | README `:21-102` labels this offline repair, reports the old failures, and keeps recovery/live proof separate. Prior packet/environment reviews and contract-preparation have explicit `review-suggested` reasons. The invocation contract's bytes are unchanged; its `accepted_packet_commit` at `run-1/invocation-contract.json:8` still names the earlier packet. | Verified PASS |

The first external old-scope fixture used a temporary-directory-derived repository
root and failed earlier on the scoped Git path assertion. Restoring the actual
repository-length root reproduced the exact Substring failure above. This fixture
correction changed neither repository source nor evidence. A mistaken audit CLI
help subcommand was corrected before the closing append; no audit content was
written by that rejected command.

## Verification record

Observed on the review checkout at the exact target commit:

- `py -3 -B .../packet_selftest.py`: PASS, 0.552 s reported by the suite.
- `powershell.exe -NoProfile -File .../SelfTest.ps1`: PASS, 0.429 s.
- `pwsh.exe -NoProfile -File .../SelfTest.ps1`: PASS, 0.315 s.
- Authority scanner including planted negatives; transaction, manifest and
  completed-guard self-tests: PASS. These tests use synthetic/offline data.
- Independent old-source failures, URI boundary fixtures, and ten actual-body
  failure runs described above: expected reds and repaired PASS results observed.
- PowerShell AST parsing of wrapper, Control and SelfTest is part of SelfTest.
  Git Bash `-n` passes for identity, owned-control, preflight and verify-rootfs.
  `tools/check-wsl-inline.py`: PASS, no inline WSL shell command.
- `run-verify-gates.py --skip verify-application-core.py
  verify-application-adapters.py verify-windows-store.py --budget 120`: all ten
  selected offline-safe gates PASS. Those three build/store gates remain outside
  scope; no full ring was run.
- `tools/check-docs.py`, graph validation and `git diff --check`: PASS. Graph
  validation reports zero defects, orphans or index drift and 134 advisory review
  flags. All 17 capture manifests match committed blobs. Expected negative-test
  diagnostics and existing Windows-store staleness are not new packet failures.
- All 15 packet files are byte-identical to `20e77e34`. The prior contract remains
  byte-identical to its parent-commit blob, SHA-256
  `f109d4f253f436d8b178e40ea5af497316faf36b0c4398c8a598abc52748336c`.

The fixtures exercise real PowerShell function bodies but replace child launchers
and file writers. They prove argument flow and exception behavior, not a real
Git commit from the live wrapper or WSL lifecycle success. `Invoke-GitCommit`
remains an internal helper with fixed caller paths; this review does not claim
it validates arbitrary untrusted folder input.

## Remaining decision and evidence boundary

**Flagged, not cleared by this ACCEPT:** the failed attempt's preserved evidence,
target state and recovery remain the incident owner's/Mac's separate decision.
Bind the existing checkpoint bytes before any evidence deletion or unregister;
this review did not inspect or mutate that live target. Do not rerun work merely
to repair evidence binding.

A future retry requires an explicit attempt/authority decision, preservation of
the first attempt, an immutable reviewed source and run-identity arrangement,
and a separately committed/reviewed environment contract binding these accepted
bytes, all inputs and signer sets, execution branch/session and then-live Mac
epoch. The present wrapper still fixes `run-1` and rejects existing output
(`Invoke-Preflight.ps1:28,426-430`); this review does not propose bypassing that
guard or overwriting attempt 1. Any change for another attempt must be reviewed.
Recheck the permitted target/protected identity, resource ceilings and guard
contract at that later boundary. Until then, recovery/retry remains closed;
install/build/case authority remains closed independently. No F1 speedup or
whole-solver qualification follows from this source review.
