---
id: proof-win-gpu-f1-attempt2-offline-request
title: "F1 attempt 2 offline decision request"
type: proof-pack
status: proposed
owner: "@pc-coordinator"
phase: preflight
tags: [windows, offline, f1, decision-request]
links:
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-offline-followups, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  Request-only attempt 2 preparation binds current committed source and historical
  evidence, while leaving Mac authority and live identities explicitly unresolved.
---

# Decision requested from the Mac leader

**REQUEST-ONLY-NOT-EXECUTABLE.** Reserve the next Mac-assigned ruling number
and confirm the bounded attempt-2 admission changes below. This request does
not ask for immediate live authorization. Ruling 225 leaves attempts 2 and 3
available; Rulings 227/228 authorized the consumed attempt 1 only.

Request JSON: `offline-decision-request.json`, SHA-256
`d4c6b7bd8abe34630e0b24a20f8160d40644bf009aa1c12c5142f3ca00c7e035`.
Observed source: `ad1424b12b386c17d33178b8fa1e619289b70457` (`origin/main`
after synchronizing PR #37). This digest binds the review
request, **not** a proposed authorization or an executable invocation contract.
The JSON deliberately uses a different schema and carries null authority fields.

Recommended disposition: accept this bounded preparation scope, reserve the
ruling number, and require a separately reviewed source freeze and complete
hash-bound request before ruling on one live attempt. Alternatives: amend the
scope or hold. Fallback: no activation, no attempt consumed, no live action.
Only the Mac assigns the ruling; no next number is inferred from Ruling 233.

# Verified baseline and explicit gaps

The 27 hashes in the companion JSON were calculated from `git show` bytes at
the exact source commit, not worktree text. The historical attempt-1 invocation
contract still hashes to
`1141ee15dde086991290c508104addf9fe5c32c789dccbdbfdbfbaeb9e49f696`.
Two current packet blobs differ from that old contract: `Invoke-Preflight.ps1`
and `SelfTest.ps1`. The JSON lists both old and current hashes. The old contract
cannot authorize the current source or another attempt.

PR #35 joined at `d860ec33bcbaa9bf6e18fba024c3654641d642b0`.
Its committed manual-unregister receipt says `MANUAL-UNREGISTER-OK`, target
key/name removed and protected identity unchanged, with the exact Ruling 233
idle session present before and after. This is historical evidence, not a new
observation of current registrations, paths or processes.

PR #36 and PR #37's current main lesson closure record the Ruling 231 controls:
the production checker registers its loaded module; the subprocess fixture
executes production argv for stage and guard and expects `OWNED-ABSENT`; the
dynamic-import scanner and its self-tests run in the docs gate. The independent
acceptance binds candidate `7affd8932eb304f282008d8e662d98ce77f6653a`.
The Ruling 229 reader repair and capture-byte preservation are also present.
Focused verification for this preparation is recorded in `validation.json`.

The current source has these blocking admission contracts:

| Surface | Observed constraint | Required attempt-2 disposition |
|---|---|---|
| `packet/attempt.py` | Attempt 1, epoch 8, Ruling 228, historical session/branch/run/root only | Narrow, reviewed attempt-2 bindings; no wildcard admission |
| `packet/Control.ps1` | Same attempt-1 bindings and fixed run root | Match the Python admission and new invocation exactly |
| `Get-BoundTargetKey` | Refuses protected key and the original e958 target key | Also refuse the consumed attempt-1 e874 key |
| `contract.check_live_leader` | Name/state/epoch checked; no remaining-TTL floor | Enforce and record at least 200 seconds at activation and immediately before first WSL command |
| `.gitattributes` | Existing attempt-1 checkpoint/final captures protected | Bind attempt-2 capture directories before any capture |
| Contract inputs | B2 paths point to the old attempt-1 worktree | Bind both B2 files to the chosen new execution worktree |
| Renewal supervisor | Existing script defaults to epoch 8 | Bind exact selected epoch and whole-run event/stderr/exit evidence |

# Proposed identities and unresolved fields

Proposed run `f1-attempt2-20261013`, session `pc-f1-attempt2`, execution branch
`win/gpu-f1-attempt2`, and root `docs/proof/win-gpu-f1-preflight/attempt-2`
are fresh proposals. This preparation uses its own branch and session ending
in `-offline`. The execution worktree is not yet assigned.

Keep the bounded target name `cfdw-petsc-cuda132-f1` and dedicated path
`C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`. A fresh distro means
absence proved immediately before creation and a new GUID observed and bound
at creation. Neither historical GUID may be accepted:
`{e958c4d5-ae78-4b50-a634-7102f334d3aa}` or
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`. The new GUID is **unresolved** and
must never be invented offline. The protected name and key remain
`cfdw-openfoam2512`, `{b7566a82-9cee-47ce-a246-f83632353cea}`.

The PC-local Mac epoch is **unresolved**. Ruling 226 describes the PC-side
reclaim/renew mechanism on behalf of Mac; it does not make a guessed epoch
valid. This task did not inspect or mutate the leader ref. Ruling 225 required
a new epoch before attempt 1; it does not require a distinct epoch for attempt 2.
Mac decides reuse versus reclaim. The selected live value must be carried through
source admission, contract, authorization and fresh activation/pre-WSL samples
with at least 200 seconds remaining. Mac stays leader. This request binds no
epoch, and historical attempt-1 samples are not attempt-2 evidence.

Final source commit, invocation-contract SHA-256, accepted-projection SHA-256,
activation-review commit, current input-byte checks, current target absence,
protected identity and PC exclusivity remain explicitly unresolved. Ruling 233
exempts an exact idle protected session only from Ruling 232's cleanup clause;
Mac must decide any attempt-2 exception. It cannot be silently carried forward.

# Required sequence after the Mac decision

1. Make the narrow source/test/capture-policy update, with historical bytes
   preserved. Prove Python/PowerShell agreement, rejection of stale identities,
   both prior target GUIDs, missing/stale leader samples, and changed hashes.
   Bind any new test to the offline-fast ring and report measured cost.
2. Independently review and freeze the source. Assign the execution worktree,
   measure the selected PC-local Mac epoch, keep its lease renewed under the agreed
   mechanism, and verify all seven input bytes and exact paths.
3. Produce and hash the invocation contract, then the authorization projection.
   Submit exact source, contract, projection, run/session/branch/root, target
   rules and epoch to Mac. Do not present this baseline snapshot as that source.
4. Mac commits a numbered hash-bound ruling accepting **one** attempt on those
   exact bindings. Promote the accepted projection byte-identically; obtain
   independent activation rehash and both required fresh leader samples.
5. Only after all gates pass, the separately authorized executor may run.
   Any failure stops and returns its immutable receipt; attempt 3 still needs
   its own ruling. Never reuse the attempt-1 authorization or this request.

All Ruling 217 limits remain exact: simulation then resolved `.deb` download
for license/Installed-Size inspection only, signed-index hash verification,
two cores at nice 10, 3,600 seconds total, 8,589,934,592-byte VHD cap,
19,684,091,961-byte initial host free floor, completed-L3 guard before/after,
manifest-bound checkpoint before cleanup, target-only unregister and proven
absence. No install, PETSc/petsc4Foam source fetch, build, case or benchmark.

# Scope and proof boundary

Goal: a hash-bound, reviewable next-attempt decision request. Done when:
committed and pushed with measured offline checks and unresolved authority
fields. Not in scope: source repair, authorization activation, WSL, registry,
distro lifecycle, full ring or main merge. Tier T1; fan-out cap one; independent
review belongs to the parent. Surface list: current Git blobs and receipts ->
request JSON -> decision text -> Mac ruling -> later admission/source work.
There is no product data model or UI change. The bounded execution graph is in
`docs/plans/f1-attempt2-offline-request.md`.
