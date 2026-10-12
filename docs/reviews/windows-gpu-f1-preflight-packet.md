---
id: review-windows-gpu-f1-preflight-packet
title: "Astra Ruling 220 packet review - dependency preflight"
type: proof-pack
status: complete
owner: "@pc-f1-r220-astra-review"
phase: preflight
tags: [windows, wsl, gpu, ruling-217, ruling-220, owner-review]
links:
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: plan-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: coordination-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-12"
summary: >-
  Ruling 220 ACCEPT for the exact repaired packet source: fresh final cleanup reap
  and exact process/argv ownership pass independent offline review. A separately
  reviewed, committed environment contract remains mandatory before live preflight.
---

# Ruling 220 independent verdict

**ACCEPT the exact repaired packet source.** No P1 or P3 hard veto remains at
author commit `d5bb68fe7db5e511ac46708e14564bb6a738db37`, joined unchanged at
`eac9d5f48dc41e19a429d10b8f5ce30ee9bfd5f6`. This is packet acceptance, not a
completed live preflight or an acceptance of unreviewed environment inputs.

Ruling 220 (`docs/notes/rulings.md:1717-1721`) explicitly reopened one repair and
fresh independent review after the prior terminal VETO. Ruling 217's scope and
bounds remain unchanged. The prior verdicts below are historical; this section
supersedes their P1/P3 disposition for these exact bytes. A later packet change
does not inherit this acceptance.

Goal: independently determine whether the two authorized repairs satisfy Ruling
220. Done when: an evidence-cited verdict is committed and pushed with audit and
derived index. Tier T1, fan-out zero. The review graph was serial source/authority
grounding → independent counterexamples and packet suites → verdict → repository
gates → audit/commit/push. Independent host-only checks overlapped; no agent was
spawned. The surface list was Linux process inspection → ownership selection and
signalling → deletion-stage finally → finalization callback → receipt status,
plus the unchanged authority/contract boundaries. Product/UI changes were out of
scope. Actual duration and check results are recorded in the closing audit.

**Confidence: high for source/control-flow and offline behavior.** The reviewer
ran no WSL, wrapper top-level, apt, network/package download, source acquisition,
installation, build or case. Git push publishes only this review and derived
documentation/audit changes. Runtime teardown, protected-state equality, real
transaction closure and live capacity remain unobserved.

## Ruling 220 checklist

Packet filenames below are relative to `docs/proof/win-gpu-f1-preflight/packet/`.

| Requirement | Independent disposition and evidence |
|---|---|
| P1: deletion launch cannot bypass a fresh stop | **Verified, cleared.** `Invoke-Preflight.ps1:340-341` wraps the actual `delete-debs` launch in `try/finally` calling `Stop-OwnedOrContain`. The AST-extracted real cleanup body, with inert boundary mocks and a post-launch timeout, records `delete-stage-launch,fresh-stop-reap`. The original exception is retained. |
| P1: final callback is a fresh stop, including failures | **Verified, cleared.** `Control.ps1:42-61` surrounds every finalization callback with an outer `finally`; it resets `owned_stopped=false` before the final stop and only sets it true after success. An error becomes `final-owned-stop`, never stale success. Independent fixtures covered checkpoint, cleanup, snapshot, guard-stop and bind failures as well as success. |
| P1: bounded stop/reap and no later workload launch | **Verified source.** The final callback is wired to `Stop-OwnedOrContain` at `Invoke-Preflight.ps1:539-545`. Its target settling, stop, residual check and uncertain-owner containment use bounded calls (`265-291`) under the shared absolute deadline (`142-184`, `Control.ps1:17-22`). After finalization returns, lines 546-571 write the receipt and run only host manifest/Git work; no target stage, guard or other owned Linux workload is launched. Host evidence children and the stop/check controls are not new preflight workloads. Exhausted budgets or failed inspection remain blocked errors, not termination proof. |
| P3: exact PID/PGID/SID and argv identity | **Verified, cleared.** Atomic records require positive PID=PGID=SID (`owned_control.py:67-93`). `ps` supplies numeric identity; `/proc/<pid>/cmdline` supplies strictly decoded NUL-delimited argv (`28-64`). The exact interpreter/script, stage and unique run-root/run-ID or session/event arguments must match (`96-151`). A present recorded leader with changed group/session/argv fails before signalling. |
| P3: runA never selects runAB | **Verified.** Stage and guard prefix-collision fixtures return no selected group; mocked stop sinks receive no signal. Exact positive identities are selected. Independent historical-source comparison reproduced the old `{123}` selection and the repaired empty set. |
| P3: pre-exec identity wrapper | **Verified.** `owned_control.py:104-116` recognizes only the exact identity script and exact owner-file token before checking the intended command. Both stage and guard fixtures pass; a foreign entry-file token fails. This preserves ownership during the atomic publication/exec boundary. |
| P3: leader gone, children remain | **Verified offline.** Saved group identity retains the child group when its leader has exited (`139-145`). The fixture drives the real stop function through a mocked signal sink and subsequent empty observation, proving group 101 is signalled and absence rechecked. It does not mistake a missing leader for a stopped group. |
| P3: vanished ps row versus unreadable process | **Verified.** Missing cmdline is omitted only after an independent `/proc/<pid>` stat also proves absence (`51-60`). Present PID or denied stat fails. Denied/unreadable, malformed or undecodable argv and failed/nonzero ps fail closed. NUL decoding preserves arguments containing spaces. An empty argv cannot authenticate a recorded leader; an additional reviewer fixture confirms rejection. Empty argv on an unrelated process is not itself ownership evidence. |
| P2/P4/P5/P6 remain cleared | **Preserved, not reopened.** Source comparison confines the repair to P1/P3 code, tests and README. Deadline and UTC fixtures, contract hash/signer/epoch negatives, Installed-Size reconciliation and capacity fixtures all pass. The environment-specific P5 review remains outstanding as before. |

## Red-first evidence and independent checks

The author records the initial red fixtures in `packet/README.md:35-43`.
Independently, this reviewer loaded only the historical pure `Control.ps1`
functions from committed base `2f643039d2df77a361a90d4578d64ccfc3fef6c9`: a
post-launch cleanup failure produced one stop. The repaired function produced
two stops, with the fresh stop last, for each failure point and the normal path.
The reviewer also loaded the historical Python module into memory with no CLI
execution, reproduced foreign `runAB` group selection for `runA`, then observed
the repaired empty set. No real group was signalled.

The complete current `packet_selftest.py` passed in **0.530 s**; `SelfTest.ps1`
passed in Windows PowerShell 5.1 (**0.226 s**) and PowerShell 7 (**0.221 s**).
The PowerShell suite parses the real wrapper AST and executes only its cleanup
function under mocks. Source inspection also traced the actual callback binding
and code following finalization; acceptance is not inferred from test names.

Passed: authority scanner and planted prohibited-command negatives; transaction,
manifest and completed-guard self-tests; AST parsing of all three PowerShell
files; installed Git Bash syntax checks for all four shell files; and
`tools/check-wsl-inline.py`. Documentation/graph and safe repository-gate outcomes
are recorded in the final audit/handoff. No application build/store gate is
claimed. Source and plan bytes remain unchanged by this review.

## Exact remaining environment-contract gate

Before any live preflight, independently review and commit
`docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json` in the isolated
EXECUTE worktree. It must bind:

1. These accepted packet bytes: an accepted ancestor commit, the complete packet
   file set and SHA-256 values matching both that commit's blobs and working bytes.
2. The exact clean execution branch and coordination session; the then-current
   live Mac leader epoch. A changed/expired epoch blocks the pending invocation;
   a stale hardcoded epoch is not approval.
3. Exact absolute local input paths and hashes for the existing B2 rootfs,
   Ubuntu sums/signature, OpenFOAM and NVIDIA keyrings, and B2 package lock/licence
   map. `contract.py:14-19,120-176` contains the fixed rootfs and input contract.
4. Independently authenticated allowed Ubuntu rootfs/archive, OpenFOAM and
   NVIDIA signer/keyring identities. Actual keyring fingerprints and every
   `VALIDSIG` must satisfy those reviewed sets (`contract.py:78-118`).
5. Exact target `cfdw-petsc-cuda132-f1`, protected `cfdw-openfoam2512`, and dedicated
   target path `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`, with the
   unchanged host-free, 8-GiB VHD, CPU/nice, completed-L3 guard, checkpoint and
   cleanup boundaries. Runtime prechecks must capture their actual state.

The wrapper must pass its existing mandatory contract verification before the
first WSL effect. The coordinator must also reconcile the carried plan's stopped
track wording with Ruling 220 and this acceptance before dispatch; this reviewer
does not own the plan or live decision. These are the remaining environment and
coordination gates, not unresolved P1/P3 source defects.

After those gates, Ruling 217 permits only its single bounded metadata/resolved-deb
preflight with manifest-bound checkpoint and cleanup receipt. Installation,
PETSc/petsc4Foam source acquisition, compilation, solver/case execution and
benchmarking remain outside this packet's authority. No speedup or whole-solver
offload claim is accepted.

# Historical Ruling 217 final verdict (superseded under Ruling 220 above)

**Second VETO — the live track is stopped. Do not execute the packet.** Reviewed
joined base `2f643039d2df77a361a90d4578d64ccfc3fef6c9`, branch
`win/gpu-f1-r217-astra-packet-r2`, session `pc-f1-r217-astra-packet-r2`.
The coordination plan at `docs/coordination/windows-gpu-f1-preflight-r217.md:51`
permits one repair and terminates before a live run on the second VETO. Mac/Fable
and the operator own any later decision; this verdict authorizes no further repair
or attempt. Ruling 217 (`docs/notes/rulings.md:1701`) remains the scope authority.

**Confidence: high.** The two remaining findings below are Verified source and
host-only counterexample observations. Their live consequences are Inferred, not
observed incidents. No WSL, apt, download, install, source acquisition, build,
solver or case command ran. Packet, plan and defect-register bytes were not edited.
The earlier report is retained below as historical evidence of the first verdict,
committed in `c8f4da4aa13c1969643b042ca3f1931db52707d9`; its old line references
and epoch statement do not describe the repaired packet.

## Final P1-P6 remediation table

All source paths in this table are beneath `docs/proof/win-gpu-f1-preflight/packet/`.

| Finding | Final disposition | Repaired source evidence |
|---|---|---|
| P1: failure-path cleanup | **VETO remains.** Checkpoint/snapshot failures now independently reach observer stop/bind. A failed deletion stage still bypasses a fresh owned-work stop. | `Control.ps1:41-52`; `Invoke-Preflight.ps1:327-343,539-545`; counterexample below. |
| P2: deadlines/reserve | **Original defect cleared.** Child budgets include startup and absolute work/checkpoint/cleanup ends of 2700/3000/3600 seconds. Distro list and registration settling are bounded. Checkpoint SHA is published only after timely blob verification. | `Control.ps1:17-22`; `Invoke-Preflight.ps1:63-67,142-186,250-264,294-316,433-452`. Delayed-startup and phase-boundary fixtures pass. This does not clear P1's omitted stop. |
| P3: exact ownership | **VETO remains.** Atomic records, intent, fail-closed ps inspection, leader-gone group termination and uncertain-creation reporting are present. Residual cmdline identity is still a substring. | `identity.sh:23-28`; `preflight.sh:37-42`; `owned_control.py:20-44,76-92,101-132`; `Invoke-Preflight.ps1:206-236,265-316`. Prefix counterexample below. |
| P4: UTC Z | **Cleared.** Typed dates normalize directly to UTC; actual strings require an offset; unspecified dates fail. | `Control.ps1:2-15`; `SelfTest.ps1:8-12`. Real ConvertFrom-Json Z-format fixture passes on this host. |
| P5: invocation/trust | **Code repair verified; environment-contract review still required.** Mandatory committed clean branch/session contract binds accepted source blobs and input hashes. Actual signers/keyrings match reviewed sets; live epoch must equal the contract. | `contract.py:26-75,78-177`; `Invoke-Preflight.ps1:420-453`; `verify-rootfs.sh:13-18`; `preflight.sh:72-77,111-129`. Planted signer/hash/epoch mismatches fail. Real input provenance is unobserved here. |
| P6: size/capacity | **Original defect cleared.** Advertised/control Installed-Size mismatch or missing control size fails with retained evidence. Absent index size is labelled control-only. Capacity incorporates measured guest and host values, separately from proposed future headroom. | `transaction.py:197-238,289-299,342-362`; full mocked verify fixtures in `packet_selftest.py`. Matching/control-only cases pass; 1-versus-99 and missing-control cases fail. Full F1 source/build/case capacity remains Flagged. |

## Remaining P1: deletion-stage failure bypasses fresh owned-work cleanup

**Verified:** the real `Control.ps1:41` stops owned work before checkpointing, then
calls cleanup at line 44. If cleanup throws it proceeds to snapshot, observer stop
and binding at lines 50-52 without another owned-work stop. The actual
`Invoke-Cleanup` starts the Linux `delete-debs` stage at `Invoke-Preflight.ps1:340`;
its `Stop-Owned` at line 341 is skipped if that stage fails. The child catch at
lines 174-184 requests cancellation and stops the Windows launcher but does not
itself prove the Linux group terminated.

**Verified host-only integration counterexample:** the reviewer parsed the wrapper
AST and loaded only its `Invoke-Cleanup` function with the real `Control.ps1`
finalizer. Registration/path checks were inert fixtures. A mock `Invoke-Stage`
recorded entry into deletion then threw a post-launch timeout. No wrapper top-level
code or process launch executed. The observed sequence was:

```text
stop-owned -> checkpoint -> delete-stage-started -> snapshot -> stop-guard -> bind-guard
```

The result reported `owned_stopped=true`, `checkpoint_proved=true`,
`cleanup_proved=false`, and the planted cleanup error. The stop flag therefore
describes only the pre-checkpoint work. **Inferred consequence:** deletion startup,
acknowledgement or timeout failure can leave a Linux group unproved after the
wrapper returns; independent observer shutdown does not terminate that group.

**Smallest repair if a new track is authorized:** cover every cleanup-stage Linux
launch with bounded unconditional group stop/verification and exact-target
containment when ownership is uncertain. Recompute `owned_stopped` after the last
possible workload launch. Preserve independent observer shutdown/binding. Add this
deletion-stage integration failure fixture; the current tests plant checkpoint,
snapshot and guard-stop failures but omit this path.

## Remaining P3: residual discovery accepts another attempt's prefix

**Verified:** `owned_control.py:81-87` uses textual substring membership for
run-root, run ID, and guard session/event identity. Selected groups subsequently
receive TERM/KILL at lines 118-119 and 127-128.

**Verified host-only counterexample:** the real `find_groups` received requested
root `/mnt/c/runA`, run ID `runA`, no saved owners, and this synthetic row:

```text
ProcessRow(123, 123, 123,
  '/bin/bash /packet/preflight.sh --stage download --run-root /mnt/c/runAB --run-id runAB')
```

It returned **`{123}`**. No signal was sent. Both values belong to another attempt,
but both contain the requested prefix. The guard branch uses the same unsafe
session-prefix and run-root-substring shape. **Inferred consequence:** cleanup can
signal a foreign attempt; for a guard this crosses the process boundary inside
the protected distro. A one-attempt policy is not exact identity proof.

**Smallest repair if a new track is authorized:** compare complete argument tokens
and exact script, run ID and run-root/event path using an unambiguous argv source.
Preserve fail-closed inspection and saved-group handling when leaders exit. Add
stage and guard prefix-collision negatives with a mocked signal sink proving no
foreign group receives a signal.

## Environment contract and remaining acceptance surfaces

**Verified code, unobserved environment:** `contract.py:114-118` rejects a live
epoch different from the contract. The fixture proves epoch 4 cannot satisfy a
reviewed epoch 5. The coordinator may renew/reclaim Mac leadership without changing
packet source; the later independently reviewed contract must bind the then-current
live epoch, exact branch/session, accepted source commit/hash set, local input
paths/hashes, B2 lock/licence bytes and authenticated Ubuntu/OpenFOAM/NVIDIA signer
sets. `docs/plans/windows-gpu-f1-preflight-r217.md:45` and the coordination plan at
line 28 now state that boundary. Caller assertion alone is insufficient. This seam
does not waive P1/P3 or authorize continuation after the second VETO.

**Verified source:** exact target/protected names, dedicated path, initial host
floor 19,684,091,961 bytes and 8-GiB maximum remain explicit
(`Invoke-Preflight.ps1:16-24,95-118`). Creation is target-only with `--from-file`,
`--vhd-size 8GB`, and `--no-launch` (523-528). Guest block/filesystem measurements
precede repository networking (`preflight.sh:50-64`). Helpers record effective
CPU/nice and PID/PGID/SID/cmdline. The five-second completed-L3 guard starts before
creation; missing/stale/changed state fails and stop/bind are independent
(`Invoke-Preflight.ps1:124-141,354-418,489-524`). P1/P3 still limit containment.

**Verified source:** normal freeze/commit/blob verification precedes deletion and
unregister (`Invoke-Preflight.ps1:250-264,327-343`). An unproved checkpoint explicitly
hands off to Mac and withholds destructive cleanup while still attempting owned
work and observer shutdown. `manifest.py:27-91` freezes complete event rows and
checks exact file sets and committed bytes. These observations are not a live
target-absence or protected-state-equality proof.

**Verified source:** exact B2 roots, five OpenFOAM roots and compiler/MPI/BLAS/LAPACK
prerequisites feed signed-index-backed zero-removal simulation, exact re-simulation
and resolved-only downloads (`transaction.py:18-23,56-74`; `preflight.sh:99-177`).
Archive identity/set/architecture/size/SHA-256, licence coverage and Installed-Size
checks remain explicit. Actual transaction closure and runtime capacity are
unobserved. No source acquisition, package installation, compilation, case run,
global WSL shutdown or resize stage was found by source review and the prohibited
command scanner. Apt install forms are simulation/URI-only. No speedup,
whole-solver offload or future F1 build authority is claimed.

## Final offline checks and handoff

Passed: `packet_selftest.py`; `SelfTest.ps1`; authority scanner and planted negatives;
transaction, manifest and completed-guard self-tests; PowerShell AST parsing of all
three `.ps1` files; syntax checks of all four `.sh` files using installed Git Bash
(no WSL); and `tools/check-wsl-inline.py`. The additional counterexamples above
explain why those passing checks are insufficient. Repository documentation,
graph and whitespace outcomes and measured duration are recorded in the closing
audit/handoff. No packet tests or code were modified.

**Terminal disposition:** P1 and P3 remain hard vetoes. The sole repair is exhausted;
the live track is stopped. Hand off only these findings and any new authority
decision to Mac/Fable/operator. F1 installation/build/source/case execution remains
closed.

# Historical first packet verdict (superseded by the final verdict above)

**VETO — do not run this packet.** Reviewed exact joined base
`5451931fbf5568f62781b22847b57bee86d38256`, with packet bytes unchanged.
[Ruling 217](../notes/rulings.md), line 1701, and the accepted
[plan](../plans/windows-gpu-f1-preflight-r217.md), lines 94–117, require the bounded cleanup and evidence contract
described below. One repair cycle is available; a second VETO stops before live execution. The coordinator's current
authority is Mac epoch **4**; the packet README and carried plan still say epoch 3 and must be reconciled before EXECUTE.

All source findings below are **Verified source observations**. Their described consequences are control-flow
implications, not claims that a live incident occurred. The date and Installed-Size counterexamples were observed in
safe host-only tests. No WSL, apt, package download, installation, build or case command was executed by this review.

## Hard findings and smallest repairs

All packet paths below are beneath `docs/proof/win-gpu-f1-preflight/packet/`.

### R217-P1 — a guard trip can defeat both target and observer cleanup

`Invoke-Preflight.ps1:226–228` runs checkpoint creation/commit with `GuardRequired=true`. A guard trip therefore
prevents a proved checkpoint. `Invoke-Cleanup` returns at lines 254–257 when that checkpoint is absent, before even
calling `Stop-Owned`. This is also the explicit behavior documented in README's checkpoint section. It contradicts
the accepted plan's cleanup obligation on every failure; a missing Git checkpoint must not leave Linux work running.

Separately, the final snapshot, comparison, guard stop request, wait and binding are in one `try` at lines 365–391.
A snapshot failure or changed protected hash throws before lines 382–390, so the guard is neither stopped/reaped nor
bound. Its Python sample limit is 732 samples plus sampling costs (`completed_guard.py:218–298`), not an independent
guarantee of termination inside the host's 3,600-second boundary.

**Repair:** stop owned workload promptly on every failure, independently of guard health and checkpoint success.
Checkpoint the failed guard's available evidence without requiring a continuing verdict. Put observer stop/reap and
event binding in their own unconditional bounded cleanup path, including a stop event or failed final snapshot.
Preserve the normal commit-before-deletion order. If a checkpoint cannot be committed, explicitly route the conflict
between retaining evidence and mandatory unregister to Mac rather than claiming the current packet satisfies both;
owned workload/observer termination must still occur. Plant guard-stop, snapshot-failure and checkpoint-failure tests.

### R217-P2 — the deadline and ten-minute cleanup reserve are not enforced on all waits

`Get-DistroNames` directly invokes `wsl.exe --list --quiet` without a timeout (`Invoke-Preflight.ps1:59–62`), including
final absence checking. `Save-Checkpoint` checks 3,000 seconds only before and after a sequence whose child budgets
are bounded by **3,600**, not 3,000 (219–230, 206–216, 142–144). Its freeze/add/commit/rev-parse/binding caps total
405 seconds; starting close to the checkpoint boundary can consume the promised cleanup reserve before the late
check detects failure. A `$CheckpointSha` assigned before the late check remains set, permitting cleanup to continue
with that reserve already spent. The stage budget is also calculated before process startup and starts its local
clock afterward (143–150), so startup time is outside that stage/cutoff budget.

**Repair:** use absolute host deadlines for work (2,700), checkpoint (3,000) and final cleanup (3,600), recalculating
remaining time after launch. Bound distro-list and every control child through the same mechanism. Ensure cancellation,
owner stop and guard shutdown can spend their reserved time even when an earlier stage exhausts its own budget.
Test delayed startup, a hung list command and a slow checkpoint near 3,000 seconds without launching WSL.

### R217-P3 — missing ownership and inspection failures can become false absence

`owned-control.sh:23–26` reports absence when `current-owner.pid` is missing; `scan_stage_groups` skips missing stage
files (11–14). A process that starts before either file exists therefore has no independent cmdline residual check.
The stage writes PID/owner files non-atomically (`preflight.sh:23,34–36`). Host failure stops only the Windows launcher
(`Invoke-Preflight.ps1:167–169`) and does not cancel/stop its Linux group before checkpointing. `cancel.request` is
checked once by the stage (42–43) but never written by the wrapper. Normal stage completion has no residual-group
check in `Invoke-Stage:191–205`, despite the README's per-stage claim.

The `ps | awk` conditions at `owned-control.sh:16,31,51,60` collapse inspection failure into the absence branch.
Furthermore, if a leader is gone but group members remain, lines 30–33 return an error without terminating those
members. The cleanup call then aborts before unregister. Killing the creation client likewise does not prove that
WSL's creation operation has settled; registration/path absence must not be asserted during an unresolved creation
race (`Invoke-Preflight.ps1:147–169,258–276,348–350`).

**Repair:** atomically publish attempt-bound ownership, add an independent exact target/run cmdline residual check on
every pre-ack failure and after every stage, distinguish `ps` failure from empty results, and terminate/verify the whole
proved owned group even if its original leader exits. Arm cancellation before startup can race it. Give timed-out or
partial distro creation a bounded settling/cleanup protocol; uncertain state is blocked evidence, never absence.
Plant missing/partial PID, failed `ps`, leader-gone/child-alive and delayed-creation tests.

### R217-P4 — UTC freshness conversion rejects the actual guard timestamp format

`completed_guard.py:200–201` emits UTC timestamps ending in `Z`. `Invoke-Preflight.ps1:123` converts the JSON value to
a string before parsing it as `DateTimeOffset`. On this host, `ConvertFrom-Json` returned a `System.DateTime` with
`Kind=Utc`; the string conversion lost the zone, parsing assigned `-07:00`, and a fresh value produced an age of
approximately **-25,199 seconds**, triggering line 124. This reproduces the prior JSON date-coercion defect class.

**Repair:** normalize typed `DateTime`/`DateTimeOffset` values directly to UTC, and only parse actual strings with
explicit offset handling. Add a self-test using the guard's exact `Z` format and the supported PowerShell versions.

### R217-P5 — the trusted invocation and signing identities are not closed

The OpenFOAM fingerprint is an arbitrary mandatory 40-hex argument (`Invoke-Preflight.ps1:6`); README explicitly says
its independent provenance remains unresolved and requires VETO if it cannot be established. Repository checks only
compare the **first** key fingerprint (`preflight.sh:65–70`); `gpgv` later accepts any key in those keyrings and only
tests that a `VALIDSIG` line exists (99–100). No equality check binds the actual signing fingerprint to the reviewed
identity. Ubuntu's archive keyring is inherited from the rootfs but its accepted signer identities are not captured
as an explicit reviewed set.

The wrapper records current packet/keyring hashes (305–316), but does not compare them or the referenced B2 lock and
licence inputs against an accepted execution contract. It has no live leader/epoch check. The README's epoch-3
instruction is stale under the epoch-4 dispatch. Recording current bytes establishes what ran, not that it was the
accepted packet or input set.

**Repair:** freeze an independently reviewed invocation contract with the source commit/hash set, B2 input hashes,
keyring hashes, authenticated OpenFOAM fingerprint, allowed Ubuntu/NVIDIA signer identities and current leader epoch.
Verify it before any WSL effect, either in this wrapper or a reviewed mandatory launcher. Match `VALIDSIG` to those
identities; reject additional unapproved signing keys. Reconcile epoch references through the coordinator-owned
plan seam. A caller-supplied value cannot by itself supply independent authentication.

### R217-P6 — Installed-Size disagreement is accepted as a complete result

`transaction.py:235–240` records the archive control value but never compares it with `installed_kib_metadata` from
the signed index. Lines 260–280 total the control value and pass whenever it is present. A host-only counterexample
supplied a lock value of **1 KiB** and an archive-control value of **99 KiB**, with unrelated archive operations mocked
to pass; the real `verify` function printed `ARCHIVES-PASS` and set `verified_against_signed_index=true`.

**Repair:** explicitly reconcile every present advertised/control Installed-Size value, fail with retained per-row
evidence on disagreement, and label a control-only value when the index omits it. Preserve archive payload as a
different measure. Add positive and mismatch/missing-field tests. Also distinguish the proposed future F1 10-GiB
headroom (`transaction.py:274–278`) from the current preflight's 8-GiB VHD; the latter's measured block/filesystem/free
values and host floors must be linked into the capacity evidence. Full F1 source/build/case capacity remains Flagged.

## Coverage and evidence limits

| Required surface | Observed disposition |
|---|---|
| Exact target/protected identity and dedicated path | Constants are explicit (`Invoke-Preflight.ps1:16–23`), collision checks precede creation, and registration path is checked before deletion. Partial-creation/owned cleanup remains blocked by P1/P3. |
| Rootfs and initial host threshold | Rootfs SHA-256 and 19,684,091,961-byte host floor are explicit; `verify-rootfs.sh` verifies the local sums signature and archive. Accepted input/trust binding still needs P5. |
| Target-only 8-GiB creation | `--install --from-file --name --location --vhd-size 8GB --no-launch` appears at 348–349; no resize or global shutdown is present. `probe` verifies block/filesystem maxima before the network stage (`preflight.sh:46–58`). Runtime capability is unobserved. |
| CPU/nice and completed-L3 observer | Linux launch arguments include taskset/nice/setsid, and identity checks record effective values. Guard projection and five-second sampling exist. P1–P4 prevent acceptance of their lifecycle guarantees. |
| Exact dependency roots and zero removals | `transaction.py:18–23,56–70` combines the 135 B2 selections, five OpenFOAM roots and compiler/MPI/BLAS prerequisites. Simulation/URI collection excludes recommends and exact re-simulation is compared (`preflight.sh:114–127`). Removal and Linux-driver fixtures pass. |
| Only resolved archive downloads and hash checks | `preflight.sh:129–141` downloads exact resolved arguments; `transaction.py:225–245` requires archive identity, size, architecture, SHA-256 and exact set. Authentication remains blocked by P5 and size reconciliation by P6. |
| Manifest and commit ordering | Freeze truncates guard events to complete rows; verification checks committed blob bytes (`manifest.py:27–89`). Normal checkpoint-before-deletion order exists. Failure paths and deadline/guard interactions fail P1/P2. |
| No package installation, PETSc/adapter source, build or case run | Static source review and the supplied prohibited-command scanner find no such live action. The apt install verbs present are simulation/URI-only. This is source evidence, not a live-run result. |

## Offline checks actually run

The supplied authority scanner (including planted negatives), transaction self-test, manifest self-test,
completed-guard self-test, `tools/check-wsl-inline.py`, and PowerShell AST parse all passed. Those tests do not exercise
the failure paths above. The UTC-`Z` freshness and mocked Installed-Size mismatch counterexamples were also run and
exposed P4/P6. One initial Python test command placed the launcher version flag after an interpreter flag and exited
before running; it was corrected to `py -3 -B` and the counterexample then executed successfully.

This review adds no tests or packet edits. The closing audit and handoff record documentation-gate results and measured
review duration. Repair should add the focused negative tests named above, not repeat a live attempt to discover them.
No speedup, solver offload, installation acceptance or build/benchmark authority is claimed.
