---
id: proof-win-gpu-f1-r225-attempt1-admission
title: "R225 attempt 1: offline admission candidate"
type: proof-pack
status: proposed
owner: "@pc-f1-r225-attempt1"
phase: preflight
tags: [windows, wsl, preflight, admission, offline-proof]
links:
  - { to: proof-win-gpu-f1-r217-packet, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: plan-windows-gpu-f1-guard-consistency-repair, rel: relates-to }
review-by: "2026-10-19"
summary: >-
  Fresh attempt/run/session/branch at epoch 7, exact packet and external helper
  bytes, seven rehashed inputs, proposed live-path authorization and precomputed
  accepted projection. Normal admission refuses until a hash-bound main ruling.
---

# Preparation candidate; no live authority

Goal: prepare attempt 1 under Rulings 225–226 offline. Done when exact bindings
and refusal controls pass and the candidate is pushed. Not in scope: WSL, live
effects, installation, build or CFD. Tier T1; no author delegation. Repair cap 2.
Astra holds independent review; the author does not grant or clear authority.

**Verified final source anchor:** `f1114270e38e04bf8cca47030a2a4125eafb750d`.
Earlier source candidates `a3086d4c` and `50a05988`, and payload candidate
`705e8ea0`, are **superseded**, retained as history, and must not be activated.
Production fix `1fc9519f` is retained but superseded for final binding by the
annotation-only source anchor above; its production logic is identical.
Source controls were committed before payloads to avoid self-hash circularity.
All 16 packet files and external `recovery/check_cache.py` are bound to those
Git blobs. The invocation keeps schema `cfdw-f1-r217-invocation/1` and adds exact
attempt/run/root values. Legacy run-1/epoch-6 admission is not reused.

| Binding | Exact value |
| --- | --- |
| Attempt / run | `1` / `r225-attempt1-epoch7-20261012` |
| Session / branch | `pc-f1-r225-attempt1` / `win/gpu-f1-r225-attempt1` |
| Leader | `mac`, PC-local epoch `7`; live renewal required for execution |
| Evidence root | `docs/proof/win-gpu-f1-preflight/attempt-1` |
| Contract SHA-256 | `63b38a67d661dc2164295a5af192bf107378b0d935c01b4529d3293c4468bf72` |
| Proposed authorization SHA-256 | `8fb79e406575febc807f69dd5a33a5131505ef38f489c4672b69013ad3694760` |
| Accepted projection SHA-256 | `d7d371ba768da659cf561fdd9dda9360c91ecf41d249cf40fbeb6dea8b01ed71` |

The live-path `attempt-authorization.json` remains **proposed**. The separate
`authorization.accepted-projection.json` is a review candidate, never an invocation
path. Its only differences are `status: accepted` and `ruling: 227`; `requested_ruling:
227` is supported by Ruling 226's explicit next-ruling statement. Future ruling
numbers are not derived. Projection bytes grant no permission.

The request to the Owner is for R227 to name the projection SHA above and admit
byte-identical promotion to the live path after source/payload acceptance. Its
accepted section must bind the live authorization path, projection SHA, source
anchor, contract path/SHA, run, session, branch and `epoch 7`, with the acceptance
marker `ACCEPT ONE PREFLIGHT ATTEMPT`. The verifier requires those bindings inside
that exact section of `origin/main:docs/notes/rulings.md`; tokens elsewhere cannot
satisfy it. No such ruling was authored here. After it exists, copy only the
projection bytes to the live path, commit, and require Astra to rehash that exact
activation before live use. An accepted projection passed as an invocation path
still refuses. Unknown/proposed status, absent ruling, altered scope/hash or
uncommitted live bytes refuse before WSL.

# Fresh target and fixed limits

The same target name/path (`cfdw-petsc-cuda132-f1` at its dedicated C: path) is
reused only as a **newly created throwaway distro**. The preserved R224 receipt
proves historical absence and unchanged protected snapshot. It does not replace
fresh admission: before creation the wrapper re-proves target name/registration/
path absence, including the old GUID under any renamed/moved identity. It rejects
aliases of protected GUID/BasePath and requires exact protected name/GUID/path/
version. Creation records a new observed GUID, distinct from old/protected GUIDs.
Deletion, containment terminate, terminate and unregister recheck that bound GUID;
unbound creation failures withhold destructive actions and report unknown state.
Fresh-attempt cleanup refuses residual host-path deletion.

R217 caps remain: target CPU 0–1, protected observer CPU 0, nice 10, 3600-second
absolute clock, work ceiling 2700 and checkpoint ceiling 3000 seconds, VHD maximum
8589934592 bytes, initial host floor 19684091961 bytes, reserve 10737418240 bytes.
Allowed stages are exactly probe/repositories/simulate/download/inspect/delete-debs;
creation, protected signature/snapshot/guard, owned stop/check, target rollback,
absence proof and evidence freeze/commit are named in the authorization. One
failure stops with an immutable classified receipt; no next attempt without its
own approval. Policy IDs `dpkg-install` and `apt-install` forbid `dpkg -i` and
`apt-get install`; source fetch, build, case run, benchmark and protected mutations
are forbidden. No scanner exception or executable command disguise was added.

# Offline evidence and scope

**Verified:** all seven local input files were rehashed and matched prior accepted
bytes. Rootfs/sums/signature/keyrings retain their existing cache paths; epoch-6
directory naming is a cache location, not authority. Package-lock and license-map
paths point into this new worktree. Exact signer allowlists are retained. Actual
`contract.py prepare` passed packet/source/helper/input checks and emitted
`PREPARATION-CONTRACT-PASS inputs=7 live_authority=False`.

Red-first: the fresh branch/run fixture failed the actual original
`check_contract_fields` with `execution branch absent`, exit 1. It passes after
the exact attempt adaptation. Focused controls reject wrong run/root/attempt/
epoch/branch/session/source/helper hash, signer/input changes and authority scope.
Ruling-section tests refuse correct tokens outside the requested section and a
wrong digest inside it. Actual projection controls prove only authority fields
differ and prohibit the projection as a live invocation path.

The PowerShell sentinel executes the real entrypoint's pre-WSL admission AST span;
only its host-child boundary is inert. It invokes the production authorization
checker, asserts its exact rejection reason and argument forwarding, and a WSL
sentinel fails safely if invalid authority reaches an effect. Both PowerShell
runtimes passed missing/proposed/altered authority with **zero WSL launches**.
Pure production identity controls test renamed old GUIDs, changed names/paths,
protected aliases and changed post-create GUIDs. Recursive run-1 byte conservation
against base `17d7427b` passed; recovery-1 and R224 bytes are also unchanged.

The initial sentinel used the production policy function at an inert child seam;
it alone did not establish `contract.verify` wiring. Final-cycle tests now call
the real `contract.verify` → `verify_authorization`, with only Git/process seams
inert, and assert exact authorization path, contract hash, RunId and exclusivity
forwarding. Missing/proposed/altered authority refuses with zero WSL calls;
changed working-tree and source bytes in the external-helper map both refuse.

The production `Assert-FinalAbsence` AST regression was red at `50a05988`: a
created GUID surviving renamed and moved was accepted. The final source reuses
all-row fresh identity checks and rejects the observed created GUID under any
alias, plus exact protected GUID/name/path/version and unique name. Both PS
runtimes pass survival and protected-identity mutation negatives. The missed
sibling and executable prevention extend `R217-RECOVERY-AUTHORITY-A`.

Ring: offline-admission, focused Python suite measured 6.222 s for eleven controls
including recursive Git-blob conservation; PS sentinel is separate. Existing
packet Python suite measured 0.613 s; final legacy PS5.1/PS7 suites 0.341/0.270 s.
Final full results and audit/graph/privacy checks are recorded in the closing audit.

Execution graph: ground → production red → narrow controls/test negatives → source
commit/push → hash payload/projection → independent offline gates → candidate
commit/push → Astra review. Eight finite nodes; no author fan-out. Data edges are
real (payloads consume source commit); source/publication splits avoid circularity.
Failed acceptance clauses decrease to zero; cap 2 is a stop/report breaker, not
the success criterion. Cycle 1 fixed declarative command labels, legacy fixture
state and EOF whitespace; gates stayed intact. No timing model or token spend is
invented. Default instrumentation comes from existing suite/audit measurements.
Cycle 2 closed final-absence aliasing and real admission wiring. At the original
cap, the machine-path gate refused two synthetic fixture expansions. The
coordinator reported the cap and invoked the operator's explicit attempt-3
permission, limited to standard `machine-path-ok` reasons on those two test lines.
Source `f1114270` changes only those comments; no production/logic change or gate
exception was added. Machine-path and full safe gates then passed (10/10).
No further production change followed `1fc9519f`.

Final closing checks: attempt suite 11/11 (7.447 s), recovery suite 50/50
(2.201 s), packet fixture suite (0.564 s), legacy and attempt PowerShell suites
on both runtimes, authority scanner and manifest/transaction/completed-guard
controls passed. Docs checks passed with 134 retained freshness suggestions.
Historical run-1/recovery-1/R224 diff against base `17d7427b` is empty. Final
privacy, graph and audit closing results are retained in the audit entry.

**Flagged:** no WSL or lifecycle behavior is measured by this preparation. Live
leader continuity and PC exclusivity require the coordinator's renewal procedure
and exact run attestation at execution; tests do not prove exclusive machine
ownership. Registry/lifecycle reads retain a bounded TOCTOU window. Created GUID,
capacity, package closure and final protected snapshot are not yet measured for
this attempt. Reviewer acceptance is readiness only. No live action ran.
