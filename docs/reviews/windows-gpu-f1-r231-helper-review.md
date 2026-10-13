---
id: review-windows-gpu-f1-r231-helper
title: "Independent R231 helper candidate review"
type: doc
status: blocked
owner: "@pc-f1-r231-helper-review"
phase: preflight
tags: [windows, cleanup, offline, independent-review]
links:
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  REJECT: the import repair and sanitized failure propagation pass, but missing
  or non-directory evidence still reports owned absence. The disclosed Desktop
  ring remains red. No source repair or live action occurred.
---

# Verdict

**REJECT** candidate `6089217fe55ae7b10624bd892b44e7ca0ad717b5`, whose exact
parent is `cecafe4e93367de66cbf161381404f2e707f0cfb`. This is an independent
review of an offline candidate, not a ruling or live authorization. The import
repair is verified. The asserted unknown-state fail-closed contract is not met.
The reported full Desktop ring is also red; no join/readiness clearance follows.

Grounding includes the prior independent failure review
`b8fea6a7b30fadf5ee9829af79217f072f5feb86`, candidate helper-repair proof,
AGENTS, Codex grounding guide, Windows proof runbook, testing and CI standards.
The review uses adversarial Test Architect and safety/ownership lenses. It
changes only review, plan, audit and derived documentation artifacts.

# Hard finding: unavailable evidence becomes absence

**VERIFIED:** the committed independent probe
`docs/reviews/r231-helper-review-probe.py` calls the real helper, dynamic loader,
owner reader and process matcher through the author's synthetic process seam.
Both stage and guard checks return `OWNED-ABSENT` for a nonexistent evidence
root. A regular file used as the evidence root also returns `OWNED-ABSENT`.
These are observations of synthetic offline cases, not observations of R230.

The helper does not validate the evidence directory before `read_owner_records`.
That reader uses child-path existence checks; missing children become an empty
owner mapping. The unresolved-intent check likewise finds no children. With no
matching synthetic process, the helper emits success. An empty existing fixture
does not distinguish confirmed absence of records from unavailable evidence.
This behavior predates this repair; the review does not attribute its creation
to the new module registration. It nevertheless violates the requested full
helper unknown-state contract and blocks acceptance of that claim.

**INFERRED consequence from the matcher:** owner records preserve group identity
when the original leader exits. Losing those records can hide surviving group
members whose argv no longer identifies the original launcher. The Windows
manifest precheck verifies host bytes; it does not prove the later Linux path
is available to this helper. No missing live mount or surviving process is
asserted. **FLAGGED:** actual current process, registration and filesystem state
remain unmeasured; the original live failure cause remains inferred.

Required bounded correction: reject a missing/non-directory/unreadable evidence
root with the stable unknown-state refusal before process scanning, and test
that real boundary for stage and guard. Preserve residual and reused-owner
refusals. Prove that unknown evidence cannot become a successful absence result;
do not loosen ownership matching or mutate historical evidence. This is a
finding and acceptance condition, not permission for this reviewer to repair.

# Verified green behavior

- The real postponed-annotation dataclass loads with its module registered in
  `sys.modules` before execution. The focused fixture clears that registry key
  first and forbids process launches during import.
- Full helper fixtures exercise real loader/owner reader/process parser/matcher
  with synthetic ps and proc boundaries: empty stage/guard and foreign argv
  succeed; owned stage/guard, reused owner, unavailable processes and unresolved
  intent refuse. The independent malformed-owner case also refuses unknown.
- Helper CLI unknown-process failures for both kinds print only
  `F1-OWNED-STATE-UNKNOWN`, exit 5, and omit the injected private error text.
- Production `Cleanup.action -> owned_check -> run_command` propagates only
  the three allowlisted failure codes, or the generic check-failed code for
  arbitrary/extra text. `execute` binds `failed-stop-no-retry`; `receipt` retains
  the code. Tests verify stop before guard/lifecycle and metadata-only stream
  capture. Binding is mocked in that fixture; no Git receipt publication or
  live child is executed by it. The changed receipt field is directly tested.
- No action order, target/protected identity, authority admission, deadline,
  stable absence, or lifecycle implementation was altered by this candidate.

The loader sweep disposition in the candidate is consistent with the prior
review: recovery and index-retained generic loaders target modules without this
dataclass shape. They remain latent generic-loader risks, not repaired or newly
runtime-verified by the candidate. This review does not require a broad loader
refactor. The registry entry left by a failed import cannot bypass this helper's
next explicit load; the helper recreates and executes the module each time.

# Exact hashes and conservation

SHA-256 of independently read candidate bytes:

| Path beneath cleanup directory unless noted | SHA-256 |
|---|---|
| cleanup.py | `443d3278a1b98bdcd2b3dc29c8861dcc0e6e6c9b0d1d680f6ea2365653f161ef` |
| cleanup_owned_check.py | `8e6cf1b1711a728c7ba7ddedecc6fb2bbac8f220f472918b2a269c0f1910e72a` |
| tests/test_f1_cleanup_owned_check.py | `007294ced4042c710cc595f2f1bd9efc396704a6836534180ee16a92f41de082` |
| payload.json (unchanged historical authority) | `3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89` |
| cleanup-authorization.json (unchanged historical authority) | `1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d` |

**VERIFIED:** Git diff from parent to candidate is empty for canonical auth,
activation sample, payload, cleanup run-1, attempt-1/final, historical run-1 and
recovery-1, and rulings. The R230 manifest verifies 69 files against evidence
commit `901bc8d79767a3e4b69564749ec3319c8d750f52`. Historical manifest SHA-256
remains `ed5417016dd8469d0db3de56ffc36d71d04d05accb0c4c97859e1675e6b64114`.
Existing authority binds old source bytes and the consumed attempt, not this
repair. The retained run output also prevents reuse through existing precheck.

# Independent offline commands and results

All commands ran in the assigned review worktree. Helper process data were
synthetic; subprocess launch and signal boundaries were patched. No wsl.exe,
registry/live inspection, terminate, unregister, cleanup or activation ran.

| Command | Observed result |
|---|---|
| `py -3 -m unittest discover -s tests -p test_f1_cleanup_owned_check.py -v` | 10/10 PASS, 0.074 s reported |
| `py -3 -m unittest discover -s tests -p test_*.py` | 91/91 PASS, 8.903 s reported |
| `py -3 docs/reviews/r231-helper-review-probe.py` | Three fail-open cases reproduced; malformed-owner and two sanitized CLI negatives pass; exit 0 means reproducer matched the defect, not acceptance |
| `py -3 docs/proof/win-gpu-f1-preflight/packet/manifest.py verify --repo . --frozen docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/run-1/final --commit 901bc8d79767a3e4b69564749ec3319c8d750f52` | GIT-BINDING-PASS, 69 files |
| `py -3 docs/ai-forward-pack/scripts/run-verify-gates.py --skip verify-application-core.py verify-application-adapters.py verify-windows-store.py` | 10/10 PASS; three explicit repository safe-bundle skips |

The safe gates include privacy and ruling-citation checks (230 cited, 230
defined). They do not replace the full ring. Final documentation/graph/whitespace
results are recorded in the review audit entry.

**REPORTED, not independently rerun:** the committed author proof records full
ring exit 1: 640 Desktop passes and 16 timeout/child-exit/partition findings,
including six child deadlines and four incomplete partitions; total wall 191 s.
Python 91/91 and documentation passed there. This review independently confirms
the Python result. It does not guess that unchanged C# sources make the Desktop
failures unrelated or environmental. The red result stands and requires measured
disposition plus required green join/readiness checks before promotion. Repeating
the broad ring is unnecessary to establish the verified helper blocker and is
outside this bounded failure repair review.

# Requirements before a future live attempt

Resolve the helper finding under separately authorized repair, independently
review exact new source/test bytes and required checks, and dispose of the red
ring. The owner must acknowledge the consumed R230 failed-stop receipt and issue
a new leader-assigned ruling; this review grants no live authority. Any new
candidate needs fresh attempt/output identity, exact source/helper/test/payload
and authority hashes, original R228 checkpoint and R230 failure binding,
unchanged reviewed target/protected identities, same-commit ruling authority and
activation sample, finite >=200-second activation and immediate samples, live
epoch, PC exclusivity, 600-second scope, independent activation rehash and fresh
identity/ownership admission. Retain one attempt and stop/new-ruling semantics.
No preflight, delete-debs, install/source/build/case/benchmark or manual host-path
deletion follows from this review. Any separate live diagnosis needs separate
explicit authority. Preserve the frozen evidence; never delete run output to
simulate a fresh authorization.
