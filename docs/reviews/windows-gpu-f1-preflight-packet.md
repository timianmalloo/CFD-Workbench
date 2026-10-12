---
id: review-windows-gpu-f1-preflight-packet
title: "Astra packet review - Ruling 217 dependency preflight"
type: proof-pack
status: complete
owner: "@pc-f1-r217-astra-packet"
phase: preflight
tags: [windows, wsl, gpu, ruling-217, owner-review]
links:
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: plan-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: coordination-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-12"
summary: >-
  VETO on live execution: guard failure can bypass cleanup, owned-process and deadline controls are incomplete, UTC freshness conversion fails, and trust/input/size reconciliation needs repair. Offline self-tests pass but do not cover these failures.
---

# Independent packet verdict

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
