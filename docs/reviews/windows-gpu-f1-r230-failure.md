---
id: review-windows-gpu-f1-r230-failure
title: "Independent review of the R230 failed-stop receipt"
type: doc
status: blocked
owner: "@pc-f1-r230-failure-review"
phase: preflight
tags: [windows, cleanup, failure, independent-review]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  R230 failed-stop receipt and binding verify; no terminate/unregister occurred
  in the recorded sequence. An offline helper import defect is verified, but
  missing raw child diagnostics leave its live causation inferred and owned
  process state unknown. Another live action requires a new ruling.
---

# Verdict and immutable evidence

**ACCEPT the failure receipt as a bound failed-stop record; cleanup is not
proved and a retry is not authorized.** Binding commit
`cecafe4e93367de66cbf161381404f2e707f0cfb` follows evidence commit
`901bc8d79767a3e4b69564749ec3319c8d750f52`, whose parent is the reviewed execution
head `fd4f738ad01f6a4be89c92d844c5ef372c47ea07`. Both record
`failed-stop-no-retry`. No repair, WSL, registry read, lifecycle action, GitHub
message, mail, or live/helper rerun occurred during this review.

**VERIFIED:** `manifest.py verify` reports **GIT-BINDING-PASS, 69 files** against
901bc8d7. Binding names that exact evidence commit and manifest SHA-256
`ed5417016dd8469d0db3de56ffc36d71d04d05accb0c4c97859e1675e6b64114`.
The binding blob equals the file in cecafe4e. The evidence commit adds only its
frozen directory; the binding commit adds only binding.json. Prior R228 final
and historical run-1/recovery-1 evidence are unchanged from the execution head.

**VERIFIED:** the receipt binds source
`6e788efbb7c2bc6f708927b183515c9ced60226d`, authorization SHA-256
`1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d`, payload
`3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89`, original run
`r225-attempt1-epoch8-20261012` and exclusive id `pc-f1-r230-cleanup1`.
Its failure hash equals the wrapper text `owned-stage-check exited 1`;
that hash does not identify the child's underlying error.

# Recorded sequence and state limits

**VERIFIED:** all 31 commands before the final failed command exit 0. The last
four of 32 commands are fresh leader sample, two distro-list observations, and
the target-only `owned-stage-check`, which exits 1 after 3.415 seconds. The
receipt elapsed value is 10.526 seconds before evidence binding, not total
end-to-end binding duration. There is no owned-guard check, terminate,
unregister, stable-absence or protected-after proof in this run.

**VERIFIED:** activation sample shows live Mac epoch 8 with 836.385735 seconds;
the committed immediate sample shows the same live epoch with 848.680946
seconds. Both pass the production finite >=200 validator. The immediate sample
command precedes the first WSL list. There are exactly three recorded WSL calls:
two lists and the read-only target helper. No terminate/unregister argv exists.
Source sequence stops after that helper failure and binds the receipt.

**VERIFIED at identity-before only:** target `cfdw-petsc-cuda132-f1`, GUID
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`, version 2, path
`C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`; protected
`cfdw-openfoam2512`, GUID `{b7566a82-9cee-47ce-a246-f83632353cea}`, version 2,
path `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.
Both path-presence flags are true. Production identity validation passes the
committed rows and lists, including uniqueness checks. Source writes this
artifact only after two equal valid observations.

**INFERRED:** these registrations likely remain because the recorded action
never reached either lifecycle command. This assumes no outside actor; it is
not a new host observation. **FLAGGED:** current registration/path state, owned
process presence/absence and post-failure protected-content invariance are
unmeasured. This receipt does not prove target absence or protected L3 equality.

# Diagnosis with explicit confidence

**VERIFIED offline helper defect:** exact `cleanup_owned_check.py` uses
`module_from_spec` and `exec_module` without registering the module name in
`sys.modules`. The loaded `owned_control.py` defines a postponed-annotation
dataclass. Calling the production loader on the committed bytes raises
`AttributeError` at `dataclasses._is_type` while resolving the class module.
It fails before `read_owner_records`, `read_processes` or `find_groups` runs.
The probe guards all subprocess calls and observes zero launches.

**VERIFIED diagnostic control:** registering the same module in memory allows
the unmodified source to load. Its real evidence reader parses the committed
failure directory: one stage owner record, zero unresolved intents. This
excludes a reproduced host evidence-path/parser failure in this offline check;
it neither validates a future Linux mount nor proves process absence. The
in-memory change is an isolated diagnostic control, not a source repair.

**INFERRED live cause:** the module-import defect is consistent with the helper's
exit 1 and occurs before any process inspection on the tested Python runtime.
It is not promoted to verified live causation: the frozen child stderr is only
1888 bytes plus SHA-256
`277f37ae6b9ecd83de4d0cb9cd40463ab42a3aa6ea5ae3ebc4774e8fdebed0f4`; stdout is
empty. The untracked live directory was inspected read-only: 84 JSON files,
no raw-stream file, and the same stderr metadata. No sensitive raw content was
copied. A digest cannot establish an exception type or rule out other live
causes. **FLAGGED:** this evidence does not establish any actual owned process.

**VERIFIED review gap:** packet tests import `owned_control` normally; the
cleanup helper test checks strings for read-only scope. Neither exercises the
actual dynamic loader. The earlier independent prechecks stop before helper
execution, so their green result did not cover this boundary. The bounded
sibling sweep found unregistered dynamic loaders in `recovery/recover.py` and
`index_retained.py`, but their loaded packet modules do not declare dataclasses;
`owned_control.py` is the sole packet dataclass declaration found. No sibling
was silently repaired or newly asserted to work.

The executable diagnostic is `r230-failure-review-probe.py`; it verifies the
receipt and reproduces the loader failure/control without observing processes.
Measured tool wall time: 0.542 seconds, offline-fast.

# Requirements before another live action

1. The Mac owner must dispose of this exact failed receipt and acknowledge that
   the one R230 attempt is consumed. No retry under the same ruling.
2. Any repair must be authorized separately and reviewed offline. Cover the
   production dynamic loader and full helper path with injected process data,
   including empty, owned, foreign/reused-owner and unavailable-process cases.
   Preserve unknown-state refusal. Add a sanitized, stable diagnostic code so
   future receipts distinguish import failure from residual-process refusal
   without publishing private argv or raw stderr.
3. Preserve this frozen run and binding. A new exact candidate must name fresh
   attempt/output identity, reviewed source/helper/test/payload hashes, canonical
   authorization, unchanged target/protected identities, original R228 checkpoint
   and this R230 failure evidence. Existing run-1 outputs currently prevent reuse;
   removing them is not an authorized reset.
4. A new leader-assigned, hash-bound owner ruling must define the one permitted
   action, same-commit authority and activation sample, finite >=200-second
   activation and immediate samples, live epoch, PC exclusivity, 600-second
   scope, no preflight/delete-debs/install/source/build/case/benchmark/manual
   host deletion, failure receipt and stop. Independent activation rehash and
   fresh exact identity/ownership admission remain required.
5. If the owner needs new live diagnosis before deciding, it needs an explicit
   read-only authorization. This review grants neither that action nor cleanup.
