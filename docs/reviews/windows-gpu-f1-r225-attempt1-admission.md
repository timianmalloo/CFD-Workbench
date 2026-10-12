---
id: review-windows-gpu-f1-r225-attempt1-admission
title: "Independent R225 attempt-1 admission review"
type: doc
status: accepted
owner: "@pc-f1-r225-attempt1-review"
phase: preflight
tags: [windows, preflight, authorization, independent-review]
links:
  - { to: proof-win-gpu-f1-r225-attempt1-admission, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: depends-on }
  - { to: review-windows-gpu-f1-guard-consistency, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-19"
summary: >-
  Independent ACCEPT of the exact R225 attempt-1 source and proposed payload.
  Production GUID-survival refusal and pre-WSL authority bindings are proved
  offline. R227 and independent activation review remain mandatory.
---

# Verdict and exact candidate

**ACCEPT — offline source/artifact readiness only.** The reviewed candidate is
`deeb6a65fc29e5a122d87a4b4a4dc72166c00ca5`, with source anchor
`f1114270e38e04bf8cca47030a2a4125eafb750d`. The artifact commit changes no
source/test logic. The live-path authorization is still proposed. No WSL,
registry read or live lifecycle operation was performed by this review.

| Binding independently rehashed | SHA-256 |
| --- | --- |
| Invocation contract | `63b38a67d661dc2164295a5af192bf107378b0d935c01b4529d3293c4468bf72` |
| Proposed live-path authorization | `8fb79e406575febc807f69dd5a33a5131505ef38f489c4672b69013ad3694760` |
| Accepted projection outside live path | `d7d371ba768da659cf561fdd9dda9360c91ecf41d249cf40fbeb6dea8b01ed71` |

The independent `r225-attempt1-payload-probe.py` verified the exact source,
sixteen-file packet set, every working/source packet hash, the external cache
helper, and all seven actual local input files and resolved paths. The two B2
inputs also match this review checkout. Both authorization representations match
the production schema/scope checker; their only differing fields are status and
ruling. The final candidate's admission suite passed 11/11 in 6.229 seconds.
The separate projection cannot be supplied as the live authorization path.

Mac/Fable must commit R227 naming the exact accepted projection bytes and
bindings. A subsequent activation must promote those bytes to the live path and
receive independent re-hash review. Neither this ACCEPT nor the projection
grants live authority. Lease continuity and PC exclusivity remain coordinator
obligations, with live epoch admission enforced by the verifier.

# Goal and authority

Goal: independently accept or veto attempt-1 admission. Done when the exact
candidate's normal entrypoint, stale/altered binding refusals, rollback controls
and evidence preservation are verified and an independent verdict is committed.
Not in scope: WSL, live effects, granting authority, installing packages,
fetching solver source, builds, CFD or benchmarks. Tier T1; one reviewer, no
delegated fan-out; at most two author repair cycles.

**Verified authority input:** Rulings 225 and 226 were read from
`17d7427b88a4c00e17350bc197061c86b6c53249:docs/notes/rulings.md` before reviewing
the candidate. R225 permits up to three more attempts within unchanged R217
limits; each needs its own hash-bound Fable ruling. A failure stops and returns
its receipt. R226 permits the PC to renew the local Mac leadership designation;
it authorizes no live action. R227 must name the new live PC-local epoch.

The review graph is authority/source inspection → candidate and independent
negative controls → evidence/gates → exact verdict, audit, derive and publication.
The surface list is ruling/authorization → invocation JSON → Python verifier →
PowerShell invocation parameters and output routing → packet stages/external
cache helper → guard/capacity/cleanup finalization → receipt and manifest.

# Minimum admission invariants fixed before candidate review

| Boundary | Required invariant and refusal oracle |
| --- | --- |
| Authority | A separately committed accepted Fable authorization binds attempt 1, the exact reviewed source, invocation-contract hash, unique run/session/execution branch and epoch 7. Current scope is explicitly attempt 1/R227, never arithmetic allocation of later ruling numbers. R225/R226, draft/proposed/unknown status, or an absent/mismatched R227 cannot launch WSL. |
| Source | Clean execution worktree; accepted source is an ancestor of execution HEAD; contract and authorization bytes equal their committed blobs. Exact packet file set and SHA-256 map equal the accepted source blobs and working files. Additions, omissions or altered bytes refuse. |
| External executable dependency | `recovery/check_cache.py`, called by `preflight.sh` outside the packet directory, is bound to its exact reviewed path and source hash. An altered helper refuses before WSL. |
| Invocation identity | The runtime RunId, session, branch and epoch match the contract and authorization. A reviewed session cannot substitute another RunId. Expired epoch 6, a dead/missing lease, another leader or a changed epoch refuses. Lease renewal is a coordinator runtime obligation, not inferred from a static file. |
| Fresh attempt and history | A fresh output root holds attempt-1 live/checkpoint/final evidence. Historical `run-1`, `recovery-1` and previous receipts remain byte-identical. Existing attempt output is a refusal, not an overwrite or automatic rerun. |
| Target identity | Exact reviewed throwaway name/path is distinct from the protected distro. Name, registration, target path and historical target GUID must be absent before creation. Protected GUID/name/path/version must match the reviewed identity. Post-create binds a new exact GUID distinct from historical/protected GUIDs; this binding is enforced before containment, deletion, terminate and unregister. A recorded-but-unused GUID is a hard veto. Renamed/moved or same-name replacement registrations refuse. No old target GUID is invented for a not-yet-created distro. |
| Inputs | All seven inputs bind exact resolved paths and SHA-256: rootfs, Ubuntu sums, Ubuntu signature, OpenFOAM keyring, NVIDIA keyring, B2 lock and B2 licences. The normal verifier hashes actual bytes. Missing, altered or redirected inputs refuse. |
| Signers | The four reviewed allowlists cover Ubuntu rootfs, Ubuntu archive, OpenFOAM and NVIDIA. They are exact reviewed sets; no substituted or widened signer set is admitted. The authenticated rootfs and signed package indices remain the source of archive hashes. |
| Capacity and time | R217 remains at most CPU0-1/nice10, guard CPU0/nice10, 3600 seconds for the whole step including finalization, 8 GiB VHD, and initial host free-space floor 19,684,091,961 bytes. Existing work/checkpoint/cleanup reserves and measured capacity checks stay enforced. |
| Protected workload | Bind the completed-L3 snapshot before the target workload, continuously guard it, and prove the matching post-cleanup snapshot. Protected distro identity remains distinct and unchanged. Source inspection is not live proof. |
| Allowed work | Fresh throwaway creation; authenticated index capture; zero-removal simulation; download only the resolved signed-index-verified `.deb` files; licence and Installed-Size inspection; owned-process stop/reap, file deletion and target lifecycle cleanup with evidence. |
| Forbidden work | No `dpkg -i`, `apt-get install`, PETSc/petsc4Foam source fetch, build, case/CFD run or benchmark. The entrypoint's actual dispatched stages must stay within the allowlist. |
| Failure and rollback | Any failure stops new work and retains the initiating error. Owned work is independently stopped/reaped. Destructive cleanup requires the committed checkpoint and exact target proof. Unproven absence, guard invariance or evidence binding cannot become success. Failure returns its receipt and consumes the attempt; another attempt needs another ruling. |

The pre-WSL refusal oracle must intercept process launch so even a regression
cannot launch real WSL during this offline review. Proposed, missing and altered
authority must record zero attempted `wsl.exe` launches. Table-driven controls
cover run id, output root, attempt, epoch, branch, session, source and external
helper hash. Historical evidence conservation is recursive, not a spot check.

# Adversarial findings and their closure

**Verified blocker on source `50a059886163a53aebb882be3474da1a079f8dd9`:**
the production `Assert-FinalAbsence` function accepts a surviving created GUID
when its name and path change. The independent offline probe
`r225-attempt1-absence-probe.ps1` extracts that production function and supplies
inert registration/list/path boundaries. Its observed result was
`REVIEW-FAIL: renamed/moved created target GUID survived but production Assert-FinalAbsence accepted`.
It launched no WSL. Final absence must enumerate all registrations, reject the
created GUID under any name/path, and prove the protected GUID/name/path/version.
The coordinator accepted the veto and opened repair cycle 2, the final cycle.

The candidate's eight Python tests passed independently in 6.729 seconds and
the PowerShell 5.1 admission controls passed with zero WSL launches. These green
tests did not detect the GUID-survival defect. The sentinel's Python child also
substitutes `attempt.check_authorization` for production `contract.verify`;
production verifier wiring and external-helper enforcement need direct proof.

**Verified repair on `1fc9519fe1c6b2ddc9c3a7adf1a2bff0f552527f`:** the same
independent probe now refuses the GUID survivor. The nine-line final-absence
change enumerates every registration, calls the shared protected-identity and
historical-target checks, and rejects any surviving created GUID. Both PowerShell
runtimes pass the production-function alias and protected-identity mutations.
All eleven admission tests pass (6.708 seconds), including the real production
verifier's authorization forwarding and altered working/source helper checks.

**Verified remaining blocker:** the safe bundle passes nine of ten enabled
gates. `verify-no-machine-paths.py` fails on `tests/f1_attempt_absence.ps1:16`
and `tests/f1_attempt_controls.ps1:10`, which contain protected-path fixtures
without its documented fixture annotation. Repair cycle 2 is consumed. This
cap event was reported to the coordinator; no source/test repair or gate
exception was made by this review. The complete 50-test recovery suite (2.037 seconds), packet fixture
suite (0.505 seconds), manifest/transaction/guard helpers and authority scanner
passed independently. Legacy packet PowerShell suites passed in 0.345 seconds
(5.1) and 0.253 seconds (7).

**Verified narrow override and gate closure:** the coordinator reported the cap
event to the operator, then invoked the operator's previously explicit authority
for third/fourth/fifth attempts solely for the two documented fixture annotations.
The exact diff from `1fc9519f` to `f1114270e38e04bf8cca47030a2a4125eafb750d`
contains only two trailing `machine-path-ok` comments with fixture reasons.
There is no production or test-logic change. The reviewer independently reran
the safe bundle: all ten enabled gates passed; application-core,
application-adapters and Windows-store build gates were explicitly skipped.
Privacy passed with zero prohibited user paths or machine SIDs and one existing
allowlist entry. The prior executable test results apply by unchanged logic.
The independent payload probe also refused the still-superseded source binding
before the regenerated payload arrived: `payload source anchor is stale or altered`.

Historical `run-1`, `recovery-1` and rulings have an empty diff from the R225 base
`17d7427b`. Against `11f56c79`, no existing historical evidence file was modified,
removed or renamed; the only additions are the four already accepted R224
read-only verification files. The original failed recovery remains failed.

The regenerated artifact candidate passed the independent payload probe and
admission suite as recorded above. The class register now extends the existing
`R217-RECOVERY-AUTHORITY-A` with the missed packet sibling, all-registration
sweep, old-source red/new-source green control and real verifier wiring tests.
Mac/Fable R227 remains the designated source of attempt-1 live authorization.

The coordinator's staged delivery separates source controls, proposed
authorization/contract artifacts, independent readiness review, R227, and a
narrow activation commit. Proposed-state refusal is part of this candidate
review. An ACCEPT at this stage means source/artifact readiness only. The exact
activation bytes require a separate independent re-hash after R227, before any
live invocation.

R227 must bind the resulting accepted authorization bytes, or explicitly admit
the reviewed proposed-to-accepted/ruling-only projection and its precomputed
digest. A hash of the proposed JSON does not also identify changed accepted
JSON. The activation diff must be restricted to explicitly ruled fields, with
source, helper, inputs, signer sets, invocation identity and caps unchanged.
Runtime leader admission remains necessary; a review-time epoch observation
does not prove the lease will still be live at execution.

The review probes are offline-admission controls: the absence probe uses inert
boundaries and was observed red on `50a05988`, then green on `1fc9519f`; the
payload probe was observed red on the superseded source binding and green on
the final payload (1.335 seconds for all seven inputs). Broader tests were not
repeated after comment-only changes; their executable logic is unchanged.
No application build, live performance, package-closure or host/guest lifecycle
claim follows from these results. Registry-to-lifecycle TOCTOU remains a runtime
residual; this review proves refusal behavior under the exercised observations.

Final review-artifact validation: `tools/check-docs.py` passed, including all
17 committed capture manifests and readable audit entries. Graph validation
reported zero defects and retained 134 pre-existing review suggestions. Proof
privacy passed after staging the independent probes and review. Whitespace was
clean. This review changes only documentation, audit/derived views and the two
offline review probes; application test-ring/readiness checks remain join
obligations. No author source, test or historical evidence byte was edited by
the reviewer.
