---
id: proof-win-gpu-f1-attempt2-admission
title: "F1 attempt 2 offline admission source proof"
type: proof-pack
status: accepted
owner: "@pc-coordinator"
phase: preflight
tags: [windows, offline, f1, admission]
links:
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-attempt2-offline-request, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-offline-followups, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  Offline attempt-2 admission, exact process identities, epoch-bound renewal,
  bounded rollback, and committed evidence validation. No live authority.
---

# Outcome and authority boundary

**PREPARED-UNAUTHORIZED.** Ruling 234 authorizes this offline source update.
It does not authorize an execution, lease renewal, distro lifecycle operation,
or full ring. This branch executed none of those operations. Parent coordinates
the independent review and GitHub join. The original request JSON, its hash
snapshot, all attempt-1 evidence bytes, and the R233 receipt remain unchanged.

Starting source was `dc3e0752f04c3b8f680bc8738ef2e7fea4d5f0df`.
Upstream synchronization advanced the worktree to
`4e168c05f94ff2f717e69a78d63f7b08773f7561` before final focused checks.
The source branch is `win/gpu-f1-attempt2-admission`. This is an author worktree,
not the future execution worktree.

No executable attempt-2 invocation contract or accepted authorization has been
created. Mac still selects the live epoch, the next live ruling, final run date,
and execution worktree after source acceptance. The verifier reads a strictly
positive integer ruling from the accepted authorization and requires the exact
section and bound hashes on `origin/main`; it never infers the next number.
Epoch reuse and reclaim remain Mac decisions. Fresh activation and immediately
pre-first-WSL samples must each prove mac/live, the selected integer epoch, a
40-hex ref OID, and finite numeric remaining TTL of at least 200 seconds.

# Goal, model, surfaces and execution graph

Goal: implement the smallest R234-authorized offline source update for attempt 2.
Done when: focused offline proofs, docs gates, independent review, audit, and a
pushed candidate are complete. Out of scope: live execution, WSL, registry,
distro lifecycle, lease mutation, full ring, and author PR publication.
Tier T2; fan-out cap one independent review boundary; repair cap two cycles.

The authority aggregate is one attempt, identified by the committed contract,
source commit, run/session/worktree, and accepted ruling. It bounds the allowed
actions and references the Mac epoch by identity. The invocation and accepted
authorization are immutable values. A leader sample is one measured event;
renewal events are append-only facts belonging to one supervisor instance.
Creation timestamps are identity attributes, never replacement-PID history.
TTL and epoch are non-additive; elapsed seconds are per-run measures. Hashes
derive from bytes. Missing evidence becomes `Not recorded`, never a guessed
sample or a list of filenames implying verification.

Surface list: Python admission and exact main ruling; PowerShell host routing;
retired-GUID lifecycle/absence guards; actual WSL argument composition; protected
keepalive and own-launcher identity; renewal supervisor and external coordinator
hash; activation/pre-live samples; cleanup receipt; frozen captures and Git
attributes; B2 input paths; conservation and PR36 helper/reader regressions.
No product/UI surface changes.

Plan: ground R217 and R225-R234 plus accepted proof -> red fixtures -> source
update -> focused proof -> independent adversarial review -> one bounded repair
batch -> recheck -> documentation/audit -> commit/push. Independent test groups
run concurrently; shared edits and Git/index operations stay serial. The author
uses developer, Test Architect, SRE and Simplifier lenses; the parent owns the
independent safety veto. Actual: one review repair batch so far; upstream sync
is not a repair cycle. Tests exposed an omitted legacy fixture context and a
new dynamic loader missing module registration; both were corrected within the
same batch. Independent review accepted the final source/test snapshot after
the bounded repair; acceptance grants no live authority.

# Implemented contracts

- Attempt 1 retains its exact R228 authorization construction and historical
  epoch/session/branch/run behavior. Attempt 2 admits only its own branch,
  session, root and date-shaped run ID, with a contract-selected positive epoch.
  The new absolute normalized execution-worktree field binds both B2 paths.
  The existing executable wrapper still refuses whitespace in its Linux-mapped
  paths; the domain validator does not invent a broader ban on Windows paths.
- Both retired target GUIDs, `e958c4d5-ae78-4b50-a634-7102f334d3aa` and
  `e874e10f-c7b9-4a2b-a03b-87ad52fb9c41`, refuse attempt-2 binding,
  lifecycle, and alias-based absence proof. No historical target is reused.
- The supervisor shares full contract admission, including committed bytes,
  worktree/branch/session, source ancestry, exact packet set, external hashes,
  exact input paths/hashes, accepted authorization and live epoch. Only its
  exact run's runtime evidence subtree is exempt from the clean-worktree test
  after wrapper launch. The coordinator implementation is a new mandatory
  attempt-2 external hash; attempt 1 retains its original external set.
- Renewal checks the selected epoch and observed ref OID in one compare-and-swap
  attempt using the existing coordinator primitives. A successor ref refuses;
  there is no retry onto its epoch. The hash-checked coordinator module is
  registered before execution and cached for the supervisor lifetime.
- The supervisor binds its owner PID and exact Windows creation FILETIME ticks.
  It checks owner liveness each second and refuses PID reuse. Renewal cadence is
  60 seconds, maximum lifetime 3600 seconds. Owner loss records nonzero exit.
  The wrapper records a fresh marker and first complete event bound to the new
  supervisor PID, instance UUID, epoch, run, owner, and launch time. Preexisting
  control/evidence files refuse before launch; the event stream opens exclusively.
- Cheap supervisor/event checks remain in the 200-ms wait loop. Expensive
  protected-registration and CIM identity checks run at most every five seconds,
  and are forced before each WSL launch. Runtime TTL subtracts sample age and
  must still meet 200 seconds. Owned launcher exceptions bind exact PID,
  creation time and full command; names or command prefixes never suffice.
- Lease/supervisor loss blocks forward work and archive deletion. Exact owned
  stop/check and guard stop/check remain attempted. Target terminate/unregister
  requires the already Git/blob-bound checkpoint, intact contract/authorization/
  source hashes, bound fresh target GUID, protected identity and exclusivity.
  Identity drift refuses destructive rollback. Missing checkpoint requires Mac
  handoff. Protected after-snapshot cannot be proved after exclusivity loss.
- Nine final receipt inputs are mandatory: leader activation, immediately-pre-live,
  supervisor start, renewal events, stderr, exit, and three public keepalive samples
  at activation, immediately-pre-live and finalization. Their schema/run/epoch/process
  identities and success status are checked and their SHA-256 values recorded.
  Missing or invalid evidence records `Not recorded` and forces failure.
  Attempt-2 checkpoint/final bytes have the same Git attribute protection as
  attempt 1. The PR36 helper subprocess and shared FileStream reader remain used.

# Parent-observed keepalive evidence

This is **parent-observed read-only CIM evidence**, reported to the author at
`2026-10-13T19:56:27.8083144Z`; it is not an author-local measurement or live
activation proof. No WSL command was invoked for this observation.

| Field | Launcher | Child |
|---|---|---|
| ProcessId | 19296 | 3524 |
| ParentProcessId | 15528 (bound) | 19296 (bound) |
| Name | `wsl.exe` | `wsl.exe` |
| CreationDate, Local | `2026-10-08T10:02:23.1776050-07:00` | `2026-10-08T10:02:23.2168250-07:00` |
| Full CommandLine | `"C:\WINDOWS\System32\wsl.exe" -d cfdw-openfoam2512 -u root -- /usr/bin/sleep infinity` | Same exact bytes |

Both identities and the protected GUID
`{b7566a82-9cee-47ce-a246-f83632353cea}` are required. The earlier historical
R233 receipt is conserved. The new observation strengthens its child identity
without inventing missing historical fields. Missing/reparented/replaced child,
changed launcher, or another WSL actor refuses. The classifier recognizes WSL
launchers and WSL-invoking shell commands; host VM/services are infrastructure,
not authorized user sessions. Delayed unregistered child launchers conservatively
refuse. Mac must review the still-live exception again at activation.

# Offline verification

Ring: focused offline-fast plus existing docs gates. No full repository ring.
All live system boundaries in the new PowerShell fixtures are replaced by
sentinels. Python renewal/CAS/owner tests use mocks; no leader ref is written.

| Evidence | Observed result |
|---|---|
| Initial 8 Python attempt-2 tests | RED: 1 failure, 4 errors before implementation |
| Initial PowerShell controls / wrapper | RED: attempt routing / sample function absent |
| Initial 3 renewal tests | RED: supervisor module absent |
| Cycle-1 authority/CAS/owner/stale suite | RED: 6 failures, 3 errors across 17 tests |
| Exact measured keepalive representation | RED before full CIM identity binding |
| Receipt and rollback AST tests | RED: missing production validators |
| Focused Python union | PASS 115/115, 23.110 seconds, FREEZE9 |
| Legacy PowerShell packet SelfTest | PASS, 0.670 seconds after explicit legacy fixture context |
| Legacy admission and absence suites | PASS; zero WSL launches |
| Five new PowerShell suites | PASS: identity, wrapper, evidence, rollback, startup |
| Python packet planted failures | PASS, 0.583 seconds |
| Packet authority scan | PASS |
| Importlib registration gate | RED one new loader, then PASS 255 files, 2.189 seconds |
| Both PowerShell runtimes, eight suites each | PASS, 18.361 seconds; timestamp/keepalive extensions also pass both |
| Subsecond JSON representation regression | Parent-observed PS7 RED; normalized typed timestamps PASS both runtimes |
| Public keepalive receipt/capture | RED six-file receipt omitted identity proof; nine-input validator and actual public capture PASS both runtimes |

New tests are in existing `test_f1*.py` offline discovery and standalone
`tests/f1_attempt2_*.ps1`. They cover the actual shared verifier and AST-loaded
production wrapper functions. Measured Python union plus PowerShell bundle was
under 30 seconds before the final legacy fixture rerun. This is a local cost
measurement, not a cross-platform performance claim. Live throughput, WSL
execution, and successful activation are not recorded by this package.

# Independent review binding

Parent relayed independent **ACCEPT**, with no remaining hard veto, against base
`4e168c05f94ff2f717e69a78d63f7b08773f7561`. The exact bytes from
`git diff HEAD --binary -- .gitattributes docs/proof/win-gpu-f1-preflight/packet tests`
were 109632 bytes, SHA-256
`5482e3d1e91c4ff8df0bcd28e3e5ce3770016cd108ccad7efd3e7de1409340e3`.
The final rebind independently checked the added owner-liveness check immediately
before the CAS write, including zero writes when the owner disappears during
the preceding reads. Its renewal-only suite passed 11 tests in 0.204 seconds,
with FREEZE9. The author additionally observed the complete 115-test union.

The reviewer also checked the Python authority/renewal fixtures, both PowerShell
runtimes, legacy helper/reader conservation, packet self-tests, authority scan,
and dynamic-import gate. This records the offline review boundary; Mac still
reviews the final joined source and separately hash-bound live invocation.

Final repository documentation gate: `py -3 tools/check-docs.py` **PASS**.
Its capture-manifest check verified 17 manifests against committed blobs;
reader sharing had zero violations; import registration checked 255 files;
capture attributes and proof PII passed. Graph validation: 571 artifacts,
zero defects, 134 existing review suggestions. Portable text I/O passed.
Audit verification before closing entry: 1107 audit and 35 change entries,
zero unreadable lines. No full ring or readiness claim is made.
