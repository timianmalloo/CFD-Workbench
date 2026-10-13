---
id: proof-win-gpu-f1-cleanup-helper-repair
title: "Prepared offline cleanup helper import repair"
type: doc
status: draft
owner: "@pc-coordinator"
phase: preflight
tags: [windows, cleanup, offline, red-first, helper]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  Prepared unauthorized source repair for the verified offline helper import
  defect, with real-loader ownership controls and sanitized future diagnostics.
  Frozen R230 evidence and authority are unchanged; no live action occurred.
---

# Prepared candidate and scope

**PREPARED-UNAUTHORIZED.** This is an offline source/test candidate, not a new
ruling, activation, or retry. The R230 attempt remains consumed. Frozen evidence,
`cleanup-authorization.json`, `leader-activation.json`, and `payload.json` retain
their committed bytes. Their existing hashes do not authorize the changed source.
Another live action needs a separately reviewed source/payload, new exact attempt
and output identity, canonical authorization, and owner ruling. This candidate
does not prepare or fabricate any of those records.

Goal: repair the production dynamic import and preserve ownership refusal while
making future failed receipts distinguish import, residual, and unknown state.
Done when: code/tests/control and exact offline validation are committed and
pushed for independent review. Not in scope: WSL invocation, process/registry
inspection, cleanup, lifecycle commands, or consuming a live attempt. Tier T0;
one author; repair cycle 1 of 2. The coordinator accepted the graph before edits.

Surface list: committed helper source -> dynamic loader -> real postponed
dataclass -> real owner reader/process parser/matcher -> helper CLI stderr ->
wrapper classified command log -> failure object -> future receipt. No domain
data model or UI surface changes. The public code is emitted on the normal failed
path; raw child streams remain bytes/hash metadata with `raw: Not published`.

# Evidence and red-first

Grounding: base `cecafe4e93367de66cbf161381404f2e707f0cfb` and independent Astra
failure review `b8fea6a7b30fadf5ee9829af79217f072f5feb86` on
`win/gpu-f1-r230-failure-review`. The retained receipt records failed-stop-no-retry
at owned-stage-check, exit 1, before terminate or unregister. Its child stderr is
hash-only. **Verified:** production loader defect offline. **Inferred:** the same
defect caused that live failure. **Flagged:** present registrations, paths, owned
processes, and protected content are not measured here.

`py -3 -m unittest discover -s tests -p test_f1_cleanup_owned_check.py -v` was red
before production edits: 10 tests, 16 errors, 0.060 seconds, exit 1. The real loader
failed at `dataclasses._is_type` with `AttributeError` because its class module
was absent from `sys.modules`. Receipt propagation tests failed with missing
`diagnostic_code`; the CLI import-error test exposed raw exception propagation.

The minimum import repair registers the module before `exec_module`, matching
the [Python direct-source import recipe](https://docs.python.org/3/library/importlib.html#importing-a-source-file-directly).
The helper emits only `F1-OWNED-IMPORT-FAILED`, `F1-OWNED-RESIDUAL`, or
`F1-OWNED-STATE-UNKNOWN`, exits 5 on those refusals, and keeps `OWNED-ABSENT` for
proven absence. The wrapper accepts only an exact allowlisted stderr value on a
failed owned-stage/guard command; anything else becomes `F1-OWNED-CHECK-FAILED`.
That code is stored in the command log and failed receipt. It never publishes
unknown child text or private process argv.

The permanent suite starts each loader test with no registry entry. It invokes
the real loader/dataclass, reader, process parser, and matcher with synthetic
`ps` rows and NUL-delimited proc argv. Empty stage/guard and foreign process cases
prove absence; owned stage/guard, reused-owner, unavailable-process, and unresolved
intent cases refuse. A parser contract fixture pairs the injected boundary with
the real `read_processes` injection seam. The wrapper tests exact allowed codes,
unrecognized text and extra private text, actual receipt fields, metadata-only
captures, and stop before the guard/lifecycle steps. Global process/signal guards
block unexpected launches and signaling in helper fixtures.

Testing Strategy union: D0 determinism; D1 behavioral/refusal controls; D4 real
temporary filesystem; D6 exact public protocol; D7 boundary-fidelity pairing.
Ring: offline-fast Python discovery in `tools/run-tests.sh`; initial focused green
10/10 in 0.138 seconds; final fixture strengthened through the actual
`Cleanup.action -> owned_check -> run_command -> receipt` path, 10/10 in 0.259
seconds. The mocked child is synthetic; no WSL invocation occurs. No dependency
was added. Independent acceptance remains
separate from the author's test results.

# Class sweep and disposition

Class: manually executing a module before registration hides its namespace from
definition-time readers. Sweep root: tracked repository Python under `docs/proof`,
`tools`, and `tests`, recursive; tokens `spec_from_file_location`,
`module_from_spec`, `exec_module`, `sys.modules`, `dataclasses`, `@dataclass`,
`NamedTuple`, and `namedtuple`. Managed pack/agent copies are excluded as sources;
the imported `coord-core.py` target was separately checked. Frozen snapshots are
read only, never allowlisted for mutation.

The target sweep found `owned_control.py` as the sole dataclass-bearing loaded
packet module. `recover.py:load_helper` currently loads manifest, completed_guard,
and contract; `index_retained.py` loads manifest. Those targets have no dataclass
declarations. Their unregistered generic loader shape remains a known latent
instance owned by `@pc-coordinator`; no current dataclass failure was claimed.

Other import callers: the two existing Python test files load cleanup, recovery,
packet_selftest, and check_cache; integration loads merge-defect-register and
recover; check-windows-runner, post-github-comment, and historical runner captures
load check-proof-pii; scan-github-text loads post-github-comment; store gate loads
check-windows-admission; regen test loads coord-core; OFN shim loads the application
vector spike; RDH plant loads check-docs; WRI wrappers load capture-proof,
capture-preparation, or seal-proof. These inspected targets do not declare the
dataclass/NamedTuple module-namespace shape. No other caller was changed or
claimed to have runtime verification by this scan. Adding namespace-dependent
definitions to them requires a real-loader regression. The permanent production
helper test prevents the verified recurrence now; a repository-wide loader lint
is a separate idea, not added scope.

# Validation ledger

Verified before artifact finalization: full Python union 91/91 in 8.606 seconds;
packet planted-failure self-test PASS, offline-fast, 0.663 seconds; owned paths
freeze query reports none frozen. The initial ring attempt failed before build
because system PATH resolved an SDK without the pinned 10.0.203. Repository
toolchain evidence identified the existing user-local SDK; `--version` then
reported exactly 10.0.203. No SDK or project pin changed.

**Verified broad ring is RED, exit 1.** With SDK 10.0.203, Release build passed
(27 seconds; three Avalonia warnings on unchanged sources). Core parts reported
346/210/205 PASS and 9/7/11 manifest EXPECTED-FAIL; Analysis parts 44/69/146 PASS;
CLI 18 PASS and two EXPECTED-FAIL. Desktop reported 640 PASS but 16 unexpected
timeout/child-exit/partition findings: six children exceeded their 120-second
deadline, and four partitions lacked completion. The failing children were
section-editor part 2, shell-window part 1, plan-canvas part 2, controller-shell,
analysis, and views part 2. The Python stage passed 91/91. Total wall 191 seconds
(190457 ms), net wall 164155 ms, against the 60-second budget; cost checker reported
zero cost failures and 16 COST-MISS at load 11.02. These failures are recorded,
not classified as green or repaired in this helper track. Their cause is not
established by the unchanged C# sources.

The initial documentation check failed only at graph validation because the new
proof had not yet been derived into the index. Derivation now includes it. The
published safe bundle (`run-verify-gates.py --skip verify-application-core.py
verify-application-adapters.py verify-windows-store.py`) passed 10/10, tool wall
5.533 seconds. Those three skipped gates remain explicit; this is the safe bundle,
not full readiness. Final documentation/privacy checks are recorded in the audit.

Conservation: the exact R230 manifest still verifies against evidence commit
`901bc8d79767a3e4b69564749ec3319c8d750f52`: GIT-BINDING-PASS, 69 files. `git diff
--exit-code cecafe4e` for the canonical authorization, activation, payload, R230
run-1, R228 final, historical run-1/recovery-1, and rulings exits 0 with no diff.

SHA-256 of the cycle-1 authored LF source bytes (candidate `6089217f`):

| File | SHA-256 |
|---|---|
| cleanup.py | 443d3278a1b98bdcd2b3dc29c8861dcc0e6e6c9b0d1d680f6ea2365653f161ef |
| cleanup_owned_check.py | 8e6cf1b1711a728c7ba7ddedecc6fb2bbac8f220f472918b2a269c0f1910e72a |
| tests/test_f1_cleanup_owned_check.py | 007294ced4042c710cc595f2f1bd9efc396704a6836534180ee16a92f41de082 |

Candidate commit/push hashes are reported to the coordinator after Git confirms
them. The source/test candidate is ready for review with a disclosed ring failure;
it is not cleared for a join, promotion, readiness, or live action.

Graph: grounding/sweep -> red behavioral proof -> repair -> offline checks ->
candidate commit/push. Real dependency edges only. Loop variant: remaining
verified defect failures, floor zero; exit when controls and required checks pass;
cap two repair cycles, with cap firing reported as a defect signal. Planned and
actual graph both have five nodes, serial width one. Span and token cost are not
modeled; duration is measured by the audit start/append pair. No repair rework
pass has occurred after the first green production result.

# Final repair cycle 2: required evidence directory

Astra rejected cycle-1 candidate `6089217fe55ae7b10624bd892b44e7ca0ad717b5`
at review commit `5be072631cec930bacc26c91f33d451f0c509631`. Exact review
artifact: `docs/reviews/windows-gpu-f1-r231-helper-review.md` on
`win/gpu-f1-r231-helper-review`, SHA-256
`430ccc12c21d2c031888cdba625b9a62258f8df10239edb6f3dc16d7f9a614ca`.
The written artifact was read first; its committed blob was then independently
hashed to the same value. The coordinator separately authorized cycle 2 offline.

**Verified defect:** a missing or file-shaped evidence root yielded an empty
owner mapping and `OWNED-ABSENT` with synthetic empty ps. This defect predates
module registration. It violated the helper's claimed unknown-state refusal.
The exact review also requires unreadable-root refusal. The first missing/file
red probe exposed four mode/case combinations; the final red suite covers six:
missing, file, and permission-denied root, each in stage and guard mode.
`test_f1_cleanup_owned_check.py` ran 13 tests, six failures, 0.063 seconds, exit 1
before production repair. The per-mode assertions remain inside each subtest so
an earlier failure cannot abort the second mode; this corrected the initial test
context before the final six-case red observation.

The production repair is one admission line, `list(evidence_root.iterdir())`,
inside the existing sanitized unknown-state boundary and before owner/process
reads. The inspected Python 3.13.14 implementation opens and enumerates
`os.scandir`; missing, non-directory, and permission-denied inputs refuse rather
than become absent optional records. Real temporary missing/file inputs pair
the injected permission-error fixture because ACL/chmod outcomes depend on host
privilege. Each invalid-root control requires stable `F1-OWNED-STATE-UNKNOWN`,
empty stdout, and zero process calls. Valid empty-directory, residual, reused
owner, import and receipt diagnostics remain covered by the existing controls.

Class sweep: all `read_owner_records` call sites under F1 proof Python and tests.
The cleanup helper and packet `owned_control.control` are the two production
callers. The packet's direct control has the same optional-child reader without
an explicit required-root admission; it is a recorded sibling risk owned by
`@pc-coordinator`, not runtime-verified or changed here. Packet self-tests call
the low-level reader with real temporary directories. This correction stays at
the reviewed cleanup boundary; it does not modify packet or historical bytes.

**Verified green:** focused helper 13/13 in 0.075 seconds; complete Python union
94/94 in 8.203 seconds. The R230 manifest again verifies GIT-BINDING-PASS, 69
files, and the frozen authority/evidence/rulings diff from cecafe4e remains empty.
The wrapper `cleanup.py` is unchanged from cycle 1. Final documentation and fast
gate results are recorded in this cycle's measured audit entry.

| Cycle-2 file | SHA-256 |
|---|---|
| cleanup_owned_check.py | e9265d5f8dbec3115d71434ae870d43284780e166f885b5182e041067828efef |
| tests/test_f1_cleanup_owned_check.py | e0d746a43af41d42b716a34cd685d8e171e2c184e0184dff76c44207269fa9c7 |

**PREPARED-UNAUTHORIZED.** This is the second and final repair cycle. The broad
Desktop ring is not rerun: no C# or build scope changed, and its earlier red
result remains undisposed. No cause or environment classification is inferred
from those unchanged sources. Independent acceptance, ring disposition, and any
new live ruling remain required. No live action occurred. A further code repair
finding stops this candidate track and returns to the operator; it cannot start
cycle 3. The five-node serial graph is retained, with one authorized rework pass
from independent rejection to the new red/green proof.
