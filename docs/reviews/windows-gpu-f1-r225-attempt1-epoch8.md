---
id: review-windows-gpu-f1-r225-attempt1-epoch8
title: "Independent epoch-8 attempt-1 readiness review"
type: doc
status: accepted
owner: "@pc-f1-r225-attempt1-review"
phase: preflight
tags: [windows, preflight, authorization, independent-review]
links:
  - { to: review-windows-gpu-f1-r225-attempt1-admission, rel: depends-on }
  - { to: proof-win-gpu-f1-r225-attempt1-admission, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  ACCEPT of the narrowly rebound epoch-8 source and proposed payload. Exact
  hashes, old-epoch refusal and proposed-state refusal are verified offline.
  R228 and independent activation review remain necessary before live use.
---

# Verdict and scope

**ACCEPT — offline readiness only.** Candidate
`df21decf4ed90fcedaecceec449c6edcd87e2179` binds source
`b7821e27d75a0a5ffed5ee3f9d36c3bf08e535a8`. The prior epoch-7 review remains
historical; this review evaluates the new epoch-8 candidate independently.

Goal: accept or veto the exact epoch-expiry repair. Done when source scope,
hashes and refusal behavior are independently verified and this review is
committed/pushed. Not in scope: WSL, live actions, broader production repairs or
granting authority. Tier T1; zero fan-out. Execution graph: exact source/ruling
diff → payload and focused negatives → documentation/privacy/safe gates →
audit and publication. No repair loop was needed in this continuation.

| Independently rehashed binding | SHA-256 |
| --- | --- |
| Invocation contract | `1141ee15dde086991290c508104addf9fe5c32c789dccbdbfdbfbaeb9e49f696` |
| Proposed live-path authorization | `a274d4d89eb789ffae57009b4197c08ea0dd632c95bfa576f7c446a30749e5b6` |
| Accepted projection outside live path | `39217fae100c358c1c98aba5e1827e23cb0e0824d2be9329f98c339a8275dff3` |

The invocation is attempt 1, run `r225-attempt1-epoch8-20261012`, session
`pc-f1-r225-attempt1`, execution branch `win/gpu-f1-r225-attempt1`, Mac epoch 8.
Requested ruling is fixed at 228. The live-path authorization is proposed with
no accepted ruling. The separate projection differs only in status and ruling.

# Verified scope and refusals

**Verified:** compared with source `f1114270`, production changes are confined
to `packet/Control.ps1` and `packet/attempt.py`: epoch 7→8, epoch-bearing RunId,
requested/accepted ruling 227→228, and matching prerequisite/ruling tokens.
Two admission test files change their fixtures consistently. The final payload
rebinds source, contract digest and the hashes of those two packet files.
There is no change to target/protected identities, external helper, seven input
hashes/paths, signer sets, R217 caps, stage/action allowlists, one-attempt policy,
GUID checks or cleanup implementation. Imported R227 and coordination history
are ancestor changes, distinct from the narrow source repair.

The surface sweep covered authorization/contract → Python binding/ruling/leader
reader → PowerShell routing → packet hash map → published proof. The reusable
independent payload probe verifies all sixteen packet files against source and
working bytes, the external helper and all seven actual local inputs. It passed
with the exact hashes above. Final focused admission tests passed **11/11 in
6.616 seconds**, including real verifier wiring and helper tampering. Admission
and final-absence controls passed on PowerShell 5.1 and 7 with zero WSL launches.

The additional independent `r225-attempt1-epoch8-probe.py` calls production
checks and proves refusal of epoch 7, epoch-7 RunId, a live epoch-7 leader, an
expired epoch-8 leader, proposed authorization and the projection supplied as
the live path. It also proves the committed R227 text cannot authorize the new
projection. The old ruling input is pinned to
`361af05a442b10307dff5748d27a643145bd84b5`, the fetched `origin/main` at review.
Ring: offline-admission; this bounded probe performs local reads and one Git
read, no WSL. The existing full packet/recovery proof remains applicable to its
unchanged code; this continuation reruns the changed admission surface.

**Verified history:** `git diff --exit-code deeb6a65 df21decf --
docs/proof/win-gpu-f1-preflight/run-1
docs/proof/win-gpu-f1-preflight/recovery-1` was empty. The attempt-1 directory
contains exactly its README, invocation contract and two authorization JSONs;
no live/checkpoint/final output exists. This review issued no WSL command and
read no live registry. The coordinator reports that R227 expired unused and no
live action occurred; repository absence alone is not host execution telemetry.

# Authority and residuals

R227 was read from the exact main commit above. It binds epoch 7 and the old
source/contract/projection, so it cannot authorize this epoch-8 candidate.
R226 authorizes local lease reclaim/renewal and grants no live action itself.
`coord-core.py leader who` independently reported `leader mac`, `epoch 8`,
`state live`, 887 seconds remaining during review. Continuous 90-second renewal
is coordinator-reported, not inferred from that one observation; lease freshness
must still pass at execution.

Mac/Fable must separately accept R228 with the exact new projection digest,
source, contract, run, session, branch and epoch. The activation must promote
only the accepted projection bytes to the live path and receive independent
rehash review before invocation. No live authority follows from this ACCEPT.
PC exclusivity, continued renewal and actual host/guest behavior remain runtime
obligations. Existing bounded registry/lifecycle TOCTOU residuals are unchanged.

Safe gates passed **10/10**, with application-core, application-adapters and
Windows-store builds explicitly skipped. Closing documentation, graph, privacy
and whitespace results are recorded in the accompanying audit entry.
