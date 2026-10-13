---
id: review-windows-gpu-f1-r231-helper2
title: "Final-cycle independent R231 helper review"
type: doc
status: blocked
owner: "@pc-f1-r231-helper-review2"
phase: preflight
tags: [windows, cleanup, offline, independent-review]
links:
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  HOLD for the unresolved Desktop ring. The bounded offline helper repair is
  technically accepted: six evidence-root CLI refusals precede all process
  calls, existing fixtures pass and historical authority/evidence is unchanged.
---

# Verdict and exact scope

**HOLD** candidate `37b96e4749a13d2fee3a7d73444d66a6a39a6403`, exact parent
`6089217fe55ae7b10624bd892b44e7ca0ad717b5`, solely for the unresolved broad
Desktop ring recorded in cycle 1. **The bounded offline helper repair is
technically ACCEPTED.** The prior unknown-evidence hard finding in independent
review `5be072631cec930bacc26c91f33d451f0c509631` is cleared. No additional
source blocker was found in this final-cycle scope. No promotion, join readiness
or live authority is granted.

This is repair cycle 2, the cap. The reviewer performed zero source repairs and
starts no third cycle. The ring hold is reported to the operator for disposition,
not silently reclassified or retried. R230 remains a consumed failed-stop attempt.

# Observed production-boundary evidence

**VERIFIED:** the entire production delta from cycle 1 is a comment and
`list(evidence_root.iterdir())` inside the existing unknown-state exception
boundary, before owner reading or process inspection. Eager enumeration requires
an inspectable directory; missing/file/permission failures become the stable
`F1-OWNED-STATE-UNKNOWN` error. Import registration still occurs before execution
of the postponed-annotation dataclass. The receipt wrapper is byte-identical to
cycle 1. No identity, lifecycle, authority, deadline or action-order change exists.

The new permanent controls cover missing, file and unreadable evidence roots,
each in stage and guard modes. Real temporary missing/file cases are measured;
permission denial is injected because ACL/privilege behavior is platform-specific.
Each asserts the unknown-state exception, empty stdout and no subprocess call.
The independent probe `docs/reviews/r231-helper-review2-probe.py` additionally
executes the real CLI for all six combinations. Each exits 5, emits exactly
`F1-OWNED-STATE-UNKNOWN\n` to stderr, emits zero stdout bytes and makes zero
process calls. Private injected exception text is absent. `subprocess.run`,
`Popen` and signal boundaries are guarded. No wsl.exe or live process query runs.

**VERIFIED:** all previous fixtures remain: fresh real module/dataclass load;
process-parser fidelity; valid empty stage/guard evidence; foreign argv; owned
stage/guard residuals; reused owner; unavailable process data; unresolved intent;
sanitized import CLI error; and the actual
`Cleanup.action -> owned_check -> run_command -> failure -> receipt` diagnostic
flow. Arbitrary/extra stderr falls back to the generic code. The wrapper fixture
binds failed-stop-no-retry through a mock and stops before subsequent lifecycle
actions; it does not publish a real receipt or execute a WSL child.

The unchanged direct `owned_control.control` caller still lacks required-root
admission. The author explicitly records that sibling risk under coordinator
ownership. It is outside this cleanup helper invocation and is not asserted
repaired or newly runtime-verified. No broad packet/history change is requested
as part of this final-cycle review. Admission at one instant is not a guarantee
against later external filesystem mutation; no atomic snapshot claim is made.

# Exact hashes and immutable authority

Independently read SHA-256 values:

| File | SHA-256 |
|---|---|
| cleanup.py | `443d3278a1b98bdcd2b3dc29c8861dcc0e6e6c9b0d1d680f6ea2365653f161ef` |
| cleanup_owned_check.py | `e9265d5f8dbec3115d71434ae870d43284780e166f885b5182e041067828efef` |
| tests/test_f1_cleanup_owned_check.py | `e0d746a43af41d42b716a34cd685d8e171e2c184e0184dff76c44207269fa9c7` |
| historical payload.json | `3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89` |
| historical cleanup-authorization.json | `1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d` |

**VERIFIED:** Git diff from `cecafe4e93367de66cbf161381404f2e707f0cfb` to the
candidate is empty for canonical authorization, activation sample, payload,
cleanup run-1, attempt-1/final, historical run-1, recovery-1 and rulings. The R230
manifest verifies 69 files against evidence commit
`901bc8d79767a3e4b69564749ec3319c8d750f52`. Existing authority binds old bytes
and the consumed attempt; it does not authorize repaired bytes. Existing output
also prevents reuse through the unchanged precheck.

# Independent checks and remaining red ring

| Command | Observed result |
|---|---|
| `py -3 -m unittest discover -s tests -p test_*.py` | 94/94 PASS, 9.497 s reported; includes all 13 helper tests |
| `py -3 docs/reviews/r231-helper-review2-probe.py` | Six CLI refusals PASS; zero process calls; exact hashes above |
| `py -3 docs/proof/win-gpu-f1-preflight/packet/manifest.py verify --repo . --frozen docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/run-1/final --commit 901bc8d79767a3e4b69564749ec3319c8d750f52` | GIT-BINDING-PASS, 69 files |
| `git diff --exit-code cecafe4e HEAD --` followed by the historical paths above, before review edits | PASS, empty diff |

Safe-bundle, final documentation, privacy, graph and whitespace results are
recorded in the closing audit. The safe bundle explicitly skips application-core,
application-adapters and windows-store; it cannot establish full readiness.

**REPORTED historical ring result, not rerun here:** cycle-1 proof records
Desktop 640 PASS and 16 unexpected timeout/child-exit/partition findings, six
children exceeding deadlines and four incomplete partitions; ring exit 1 and
191 seconds wall. That failure remains red. Python green and unchanged C# source
do not establish its cause or irrelevance. The operator must obtain measured
disposition and satisfy required join/readiness gates before promotion. This
review performs the requested focused offline checks and does not retry the
broad ring. Final verdict remains HOLD even though the bounded code finding is
resolved.

# Future action requirements and confidence limits

**FLAGGED:** current target/protected registrations, paths and owned processes
are not measured. The original R230 live failure's import cause remains
**INFERRED**, not upgraded by these offline fixtures. Historical receipt and
manifest remain the record. No WSL, registry/live inspection, cleanup, terminate,
unregister, activation, mail or GitHub message occurred during review.

After the ring hold is disposed of, any live attempt still needs a new
leader-assigned ruling, fresh attempt/output identity and exact reviewed
source/helper/tests/payload/authority bindings, original R228 checkpoint and R230
failure binding, the unchanged reviewed target/protected identities, same-commit
ruling authorization and activation sample, finite >=200-second activation and
immediate samples, live epoch, PC exclusivity, 600-second scope, independent
activation rehash, and fresh identity/ownership admission. Keep one-attempt
stop/new-ruling semantics and all prohibitions: no preflight, delete-debs,
install/source/build/case/benchmark or manual host-path deletion. Never erase
historical output to reuse authority. Any live diagnostic requires separate
explicit authorization. This artifact provides none.
