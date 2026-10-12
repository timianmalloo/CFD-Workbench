---
id: review-windows-gpu-f1-cleanup-recovery-cycle1
title: "Astra independent cleanup-recovery review: cycle 1"
type: proof-pack
status: complete
owner: "@pc-f1-r217-cleanup-recovery-astra-cycle1"
phase: preflight
tags: [windows, wsl, recovery, independent-review, repair-cycle-1]
links:
  - { to: review-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: proof-win-gpu-f1-cleanup-recovery-cycle1-author, rel: relates-to }
  - { to: note-windows-gpu-f1-cleanup-recovery, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  ACCEPT exact candidate be56d099 as an offline cleanup-source repair. All three
  historical counterexamples are red on both baselines and green on the candidate.
  No live recovery, race-free execution, integration or new authority is certified.
---

# Independent verdict

**ACCEPT** the scoped cleanup-source repair at
`be56d099e6190017d245b0ede4afbdee2b121ffc`, for independent offline source review.
There is no unresolved blocker in the demonstrated repair of CR-1, CR-2 and CR-3.
The historical VETO of `02ec2cc3` remains valid and unchanged. Acceptance of these
bytes does not authorize recovery, a preflight retry or live execution. A new
committed hash-bound Mac cleanup ruling remains required.

**Verified; high confidence:** this review independently reproduced the three
prior failures on both `02ec2cc3` and joined baseline `f5fa973a`, then reran them
against the exact candidate. The reviewer did not edit implementation or tests.
No WSL invocation, recovery entry point, guest deletion, distro lifecycle action,
download, install, source acquisition, build or CFD workload ran. Original failed
execution captures were read only.

## Repair evidence

| Finding | Historical observation, both baselines | Exact candidate observation |
| --- | --- | --- |
| CR-1: launch after deadline | `command()` invoked mocked `Popen` once with exact unregister argv at elapsed 601 seconds. | Zero launches. Both launch sites check before setup and immediately before `Popen`; guard and final-binding expiry fixtures also observe zero launches. |
| CR-2: surviving target GUID | A renamed/moved row with the original GUID passed `validate_absence()`. | Refused. All registry rows are checked for admitted GUID, target name or normalized target BasePath. Different-GUID/same-path and duplicate identity cases also refuse. |
| CR-3: finally overwrites cause | A deletion exception followed by a final-check exception exposed only the latter. | `RecoveryFailure` preserves deletion then final-check errors. Classified receipt tests retain ordered deletion, ownership and absence errors; independent guard-stop and capture-publication failures remain represented. |

The independent probe reported:

```text
02ec2cc3 CR-1/2/3 requirements: (False, False, False)
f5fa973a CR-1/2/3 requirements: (False, False, False)
be56d099 CR-1/2/3 requirements: (True, True, True)
```

The baseline modules were loaded from `git show <commit>:.../recover.py` into
isolated Python modules, without invoking `main()` or `Recovery.run()`. Process
launches were mocked. The deadline clock was fixed at 601 with start 0; the
registry and dual-failure action inputs were the same across all three revisions.
These are observed red/green control-flow results, not live WSL claims.

## Launch, identity and deletion boundaries

**Verified by source inspection and offline execution; high confidence:**

- `recover.py:218` admits budget before opening capture files and rechecks
  immediately before `Popen`. Its absolute end is the earlier of start+600 and
  the command's own start+cap. Post-child budget checks and bounded reaping
  retain deadline failure. `start_guard()` at line 452 has equivalent checks.
  An expired final binding at line 651 launches no child and writes an explicit
  incomplete receipt; it never prints the binding success signal.
- Every host child has an atomic intent published before launch. Host PID
  acknowledgement follows launch and explicitly leaves Linux state unknown.
  Host acknowledgements are written synchronously, not by atomic replacement;
  any write failure prevents success. Guest intent is atomically replaced before
  WSL launch. The existing `identity.sh` publishes its entry atomically before
  executing the helper. The host requires a fresh, complete PID=PGID=SID entry,
  recorded nice value and affinity. Missing, unchanged or partial acknowledgements
  refuse. The 35-test suite includes a real benign host Python child proving
  intent-before-launch and numeric PID acknowledgement.
- Exact target and protected names, GUIDs, version, BasePaths and nonlinked
  ancestry remain mandatory. The admitted target GUID is retained independently
  for final absence. `stable_identity()` at line 398 requires two agreeing
  registry/list and ancestry-identity observations immediately before deletion,
  terminate and unregister. Observed disagreement prevents the lifecycle call.
- The completed-L3 guard is still freshly bound to the retained terminal hash,
  with matching event/state, session, count, timestamp, continuation and freshness.
  It spans deletion, terminate and unregister. Post-unregister continuation and
  a full equal terminal snapshot remain required. Guard-stop/reap and publication
  outcomes are collected independently. A failed teardown or missing acknowledgement
  remains a failure and cannot certify unregister or a successful recovery.
- `check_cache.py:54` admits the exact original cache repeatedly, opens every
  directory component with `O_DIRECTORY|O_NOFOLLOW`, holds the cache descriptor,
  and compares pinned identity with the admitted path. Each unlink rechecks
  ancestry, mountinfo and remaining file identities. `unlink_verified()` at line
  46 compares the no-follow file stat, then uses `os.unlink(name, dir_fd=...)`.
  It does not follow a replacement ancestor into another directory. Residual
  entries or changed path/mount observations refuse success.

The independent reviewer additionally executed the **full `delete_cache()`
function** against synthetic syscall boundaries, beyond the suite's isolated
unlink test. A stable one-file case used no-follow on every directory open and
unlinked exactly `exact.deb` relative to descriptor 45. A changed mount snapshot
at the per-file check performed zero unlinks and raised. All filesystem deletion
and descriptor syscalls in this probe were mocked; no actual deletion occurred.

**Flagged limitation:** repeated observations do not close a concurrent registry
or file-entry race. A WSL registration can change between its last check and the
name-based lifecycle request. An entry can change between no-follow stat and
relative unlink; the pinned directory prevents ancestor redirection but is not
an atomic compare-and-unlink operation. The source does not enforce exclusive
machine ownership. Any future ruling must explicitly address that operational
boundary. This review does not certify race-free execution or hostile concurrent
administrator/root mutation. The author's follow-up correctly retains this risk.

## Scope and integration seam

**Verified; high confidence:** only the `delete-debs` case changes in
`packet/preflight.sh`. Every byte before that case and after it equals `02ec2cc3`.
The two independently recomputed boundary hashes are:

| Segment | SHA-256 |
| --- | --- |
| Prefix before deletion case | `d4aa0f307d4f1d7cf792386a0982a4be0d50d2ecf331d6a5b6ef7938cb7eb185` |
| Dispatch suffix after deletion case | `5f944b439dcdb44a87e81a051cfd0c670348f7e6e49a1bdf0623d25295beaefa` |

The only other packet edit adds the self-test scope oracle and a planted
non-deletion mutation. No simulation, repository, download, installation, source,
build or CFD authority was added to recovery. No recursive host deletion or
catch-path unregister fallback appears. The authority scanner covers the packet;
the recovery dispatch and cache helper were independently inspected as well.

**Integration is not accepted by this verdict.** The separately accepted packet
repair `65456e1c` is not part of this candidate. Comparing its packet tree with
this candidate shows differences in `Control.ps1`, `Invoke-Preflight.ps1`,
`README.md`, `SelfTest.ps1`, `packet_selftest.py`, `preflight.sh` and
`transaction.py`. A future authorized join must preserve the accepted packet
repair, apply the narrow deletion hunk, reconcile its scope oracle, and review
the resulting exact bytes. Replacing the packet wholesale would lose accepted
work. The original invocation contract and packet hash map remain historical;
they are not silently updated by this review.

## Evidence, error retention and privacy

**Verified; high confidence:** all 75 original checkpoint payloads and all 93
original live captures equal their candidate copies byte-for-byte, with exact
file sets. The original checkpoint manifest is unchanged. Real packet manifest
verification against `be56d099` passed for 76 and 94 blobs respectively.
`git diff f5fa973a..be56d099` is empty for both retained trees and the historical
VETO. The latter's SHA-256 is
`2be0ba9c514056cac232126c8cb4f13fa57f618623414f81ef8ec94efea0214b`.

The original failure remains `stage-simulate exited 5`, checkpoint unproved,
cleanup withheld. New raw stdout/stderr and exception strings stay in ignored
`recovery-1/private/`. Public capture records contain byte counts and SHA-256;
errors contain ordered phase, exception type and raw-message hash. The private
record retains diagnostic text. Protected home paths and hostname values are
projected through the redactor; guard stop reasons are classified before the
public event prefix is bound. The privacy gate found no raw user path or machine
SID in its scanned corpus; 365 binary files are explicitly outside that scan.

The separate v2 recovery receipt records `cleanup_proved`, ordered errors,
initiating error and binding state. It never sets `recovery_success` true. The
committed receipt remains explicitly pending because a commit cannot name its
own future verification result. Completion requires the actual manifest check,
`RECOVERY-BINDING-PASS commit=...`, and independent verification of that commit.
The local receipt records verified binding afterwards. Any binding failure
retains an incomplete local receipt and emits no success; no retry follows.

## Independent checks

| Check | Observed result |
| --- | --- |
| Recovery suite | 35/35 PASS; 0.978 seconds |
| Prior CR-1/2/3 counterexamples | All red on both historical baselines; all green on candidate |
| Full cache orchestration with mocked syscalls | Stable case PASS; changed mount refuses before unlink |
| Packet self-test, including ownership and scope mutation | PASS; 0.470 seconds |
| Windows PowerShell / PowerShell 7 packet suites | PASS; 0.260 / 0.204 seconds |
| Git Bash `-n` on `preflight.sh` | PASS; WindowsApps Bash/WSL was not invoked |
| Authority, transaction, manifest and completed-guard self-tests | PASS |
| Both real committed manifests and original-byte comparison | PASS |
| Offline-safe gate bundle | 10/10 PASS; application-core, application-adapters and Windows-store gates explicitly excluded |
| Proof privacy | PASS; zero raw user paths or SIDs, one existing allowlist, 365 binaries skipped |
| Candidate graph validation | Zero defects and drift; 131 existing advisory suggestions |

Final documentation and derived-index checks are recorded in the closing audit.
No green offline check is represented as a live WSL result.

## Execution graph and remaining risk

Goal: independently decide the exact repair candidate. Done when the follow-up,
audit and derived index are committed and pushed with measured checks. Not in
scope: repairs, live action or new authority. Tier T2; no agent fan-out. Surface
list: baseline counterexamples → source controls → launch/ownership/identity →
guard/cache/lifecycle → private/public errors → manifests and separate receipt.
Product model and UI are unchanged.

The six-node graph was ground → reproduce/inspect → independent probes and
offline gates → verdict → derive/audit → commit/push. Independent read-only
checks used bounded tool batches of at most three; all joins inspected every
output. No implementation repair loop occurred. All requested review floors
remained present. Duration comes from the per-run audit marker; no speedup or
token-cost estimate is asserted.

Remaining **unverified** behavior is actual WSL startup and group teardown,
live registry/list parsing and mutation, Linux directory-descriptor semantics,
current cache/process state, protected runtime invariance and deadline-incomplete
cleanup. Repeated identity observations and descriptor pinning narrow races;
they do not replace exclusive host access or an atomic lifecycle API. A deadline
can leave unknown live state and uncommitted evidence, explicitly failed.
Integration with `65456e1c` requires its own exact-byte review. Mac/operator
retains the decision on any future cleanup execution.
