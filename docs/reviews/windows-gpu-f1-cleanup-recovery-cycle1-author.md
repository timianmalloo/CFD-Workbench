---
id: proof-win-gpu-f1-cleanup-recovery-cycle1-author
title: "F1 cleanup recovery cycle 1: author repair evidence"
type: proof-pack
status: proposed
owner: "@pc-f1-r217-cleanup-recovery-author"
phase: preflight
tags: [windows, recovery, offline, repair-cycle-1]
links:
  - { to: review-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: note-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: proof-win-gpu-f1-r217-packet, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  One authorized offline repair of CR-1/2/3 and the named ownership, TOCTOU,
  privacy and incomplete-binding gaps. Historical VETO stays intact; independent
  candidate review and a new Mac cleanup ruling remain required.
---

# Cycle 1 candidate — no author clearance or execution authority

**Disposition: proposed for fresh independent review.** Astra's VETO of
`02ec2cc3aaccc2da2b8ce63fe5f4790ce26839d3` remains unchanged in
[the original review](windows-gpu-f1-cleanup-recovery.md). This follow-up records
source repairs and offline observations. It does not change that verdict to ACCEPT.
Work began on joined HEAD `f5fa973a`. No WSL command, recovery entry point,
guest deletion, lifecycle change, acquisition, install, build or CFD work ran.
No retained checkpoint/live capture changed.

## Reproduced red, then observed green

The first regression run had **19 tests, exactly three failures** against the
joined candidate. These invoked the actual functions with deterministic clocks,
synthetic registrations and a mocked process boundary:

| Finding | Observed red | Source control and observed green |
| --- | --- | --- |
| CR-1 | Expired 600-second budget still called `Popen` once for exact unregister. | Budget admission runs before capture setup and immediately before both `Popen` sites. Lifecycle, guard and expiry-during-setup fixtures assert zero launches. Startup, waits, reap and binding share the original absolute deadline. |
| CR-2 | Renamed/moved row carrying the exact target GUID passed absence. | Admission captures that GUID. Absence scans every registration for the admitted GUID, exact target name or normalized target path; protected identity equality remains mandatory. Renamed/moved GUID and different-GUID/same-path fixtures refuse. Duplicate keys/path aliases refuse admission. |
| CR-3 | Deletion plus final-check failure exposed only the final-check error. | `RecoveryFailure` retains ordered independent exceptions. Deletion failure comes first, final ownership follows, and a read-only final absence diagnostic follows any failed cleanup. Unregister is skipped after either deletion or final-reap failure. The eventual receipt retains every classified failure and identifies the first as initiating. |

Additional red fixtures observed a guard launch after expiry, missing intent/ack,
unstable lifecycle admission, surviving target BasePath and the absence of the
pinned helper in the deletion branch. The packet scope and evidence-binding
controls were also introduced red before their methods existed. The guard-expiry
red exposed open capture handles; the repaired expiry path closes them before
returning. No raw red traceback containing local temporary paths is published.

**Verified green:** 35 recovery regressions, measured **0.304 seconds**, and the
real packet suite, measured **0.474 seconds** before closing gates. Tests are in
the **offline-fast ring**. They include the three reproduced defects, late expiry,
missing/stale/partial acknowledgements, exact native argv, duplicate/renamed/path
identity, dual/triple failure receipts, independent guard stop errors, changed
file refusal, exact directory-relative unlink, incomplete binding and public
classification. A real local Python child proves native PID acknowledgement and
intent-before-launch; it is a host-only contract test, not WSL fidelity proof.
The error-retention sweep also planted a capture-publication failure: it was red
when publication overwrote earlier guard errors, then green after each publication
and close failure became an independently retained outcome. Startup and stop paths
retain their original failures even if closing/classifying evidence also fails.

Closing observations: both PowerShell packet suites passed (Windows PowerShell
0.301 s; PowerShell 7 0.227 s); authority planted negatives and packet scan,
transaction, manifest and completed-guard self-tests passed; Git Bash syntax check
passed; `check-docs.py` passed; graph had zero defects and 131 preexisting advisory
suggestions; hostname-aware proof-PII had zero raw user paths/SIDs; all ten
offline-safe verify gates passed. Application-core, application-adapters and
Windows-store gates were excluded from this offline authoring track.

## Ownership, stable admission and deletion seam

Every host child has an atomic prelaunch record and a numeric host PID acknowledgement.
Host acknowledgement explicitly says Linux state is unknown. Each Linux helper also
has an atomic intent and requires a fresh PID=PGID=SID entry plus the recorded affinity
and nice value. An old unchanged entry is refused. Immutable intent/owner history is
retained alongside the canonical records that the packet's ownership scanner reads.
Missing or late guest acknowledgement remains unknown; no absence or unregister is
inferred from killing the Windows launcher.

Immediately before deletion, terminate and unregister, two complete fresh registry/list
and path-ancestry observations must agree. Path identities include device and inode;
links/junctions refuse admission. The admitted target GUID remains fixed. This is a
bounded repeat check, not an atomic Windows registry/WSL transaction.

Only `packet/preflight.sh`'s **delete-debs case** changes. It calls the recovery cache
helper with the exact original cache. All bytes before that case and after it match
the independently reviewed original source. `packet_selftest.py` enforces those two
SHA-256 anchors and plants a mutation to another stage; that mutation must fail.

The cache helper checks the exact path, ancestry, mountinfo and file identities twice,
opens every directory component with `O_DIRECTORY|O_NOFOLLOW`, pins the cache directory
handle, and rechecks ancestry/mount/file identity immediately before each unlink.
`os.unlink(name, dir_fd=pinned_fd)` cannot follow a replacement ancestor into another
directory. Changed entries and residual/new entries refuse success. No recursive path
removal or simulation/download/install/build/CFD dispatch was added. Directory-handle
syscalls are mocked on Windows; real Linux behavior remains unobserved in this track.

**Integration seam: accepted preflight repair `65456e1c` is not folded into this candidate.**
The newly changed `preflight.sh` and packet self-test require exact-byte inspection by
the cleanup Astra reviewer. At a later authorized integration, preserve that branch's
accepted non-deletion repair and this single deletion hunk; do not replace the full
packet file. The scope oracle's non-deletion anchor must be reconciled to the joined,
independently reviewed source. This author has no authority to reopen other stages or
to change the historical invocation contract/packet map.

## Public evidence and failed binding

Raw child stdout/stderr and raw exception text stay in ignored `recovery-1/private/`.
Public subprocess captures contain schema, byte count and SHA-256 instead of arbitrary
runtime text. Structured errors retain ordered phase, exception type and raw-message
hash; the local private record retains the original text. Guard stop reasons are
classified in a complete immutable public event prefix before the real packet binder
checks it. Original retained execution evidence is never redacted or rewritten.

The runtime receipt uses `cfdw-f1-cleanup-recovery/2`. It records `cleanup_proved`,
ordered `errors`, `initiating_error`, and `evidence_binding`. It never sets the earlier
`recovery_success` field true before committing/verification. The committed evidence
receipt explicitly remains pending; completion requires the real manifest verification
and the executor's `RECOVERY-BINDING-PASS` commit. The local receipt then records that
verified commit. Consumers must verify the referenced manifest/commit and the successful
completion signal, rather than treating a pre-binding receipt as acceptance.

Any freeze, Git or verification failure—including an exhausted deadline—updates the
local receipt to **incomplete, unverified, Mac decision required**, retains the failure
alongside earlier errors and emits no success. The expiry binding fixture invokes the
actual binding method with a deterministic expired clock, observes zero `Popen` calls,
and reads that explicit incomplete receipt. There is no retry or catch-path cleanup.

## Graph, controls and residual risk

Goal: repair only reproduced cleanup boundary defects and the reviewer-named hardening
surface. Done when red/green evidence and required offline gates are committed and
pushed for fresh review. Not in scope: live recovery, original capture mutation, broad
preflight repair, package/source/build/CFD work. Tier T1; fan-out zero; repair cycle 1 of 2.

Surface list: absolute deadline → atomic launch intent/host and guest ack → registry GUID
and path observations → guard → pinned cache deletion → final ownership/absence → ordered
classified receipt → public manifest and Git binding. Product model and UI are unchanged.
The optimize-graph plan is ground/reproduce → repair → focused/full offline proof →
follow-up/audit/derive → commit/push. Each edge consumes prior evidence; independent
read-only gates may overlap with width at most three. Source edits and mutations remain
sequential. Any repeat review failure is reported; cycle 2 is the final repair cap, not
an automatic retry. The audit marker measures this author run; token cost is not recorded.

Class → sweep → derive → prevent: deadline admission after launch, name-only identity
absence and finally-error overwrite are separate instances of boundary evidence loss.
Sweep: both launch sites, every lifecycle/deletion dispatch, all registry rows, final
owned/absence/guard paths, receipt assembly and evidence binding. Controls: zero-launch
expiry fixtures, exact GUID/path survival fixtures, ordered multi-error receipts, intent
acknowledgement fixtures, pinned unlink and packet non-deletion scope mutation.

Remaining **unverified** behavior: actual WSL startup/group teardown, registry/list
format and mutation races, Linux no-follow/dir-fd behavior, external root/mount mutation,
current target/cache state and protected runtime invariance. Repeated checks and pinned
handles narrow TOCTOU; they do not establish exclusive machine ownership or an atomic
WSL lifecycle API. Deadline exhaustion can still leave live work or uncommitted evidence;
the new controls retain that uncertainty and withhold success. A future recovery still
requires independently accepted exact source and a new committed hash-bound Mac ruling.
