---
id: proof-win-gpu-f1-offline-followups
title: "F1 subprocess and dynamic import offline controls"
type: proof-pack
status: draft
owner: "@pc-coordinator"
phase: preflight
tags: [windows, offline, testing, importlib, recurrence-prevention]
links:
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: depends-on }
  - { to: proof-f1-composite-helper-reader-r233, rel: relates-to }
  - { to: defect-classes, rel: documents }
review-by: "2026-10-20"
summary: >-
  Prepared offline follow-ups replace the cleanup helper's source assertion with
  a real production-argv subprocess check and require registered dynamic modules
  across tracked Python. Historical proof loaders remain exact hash/line pins.
---

# Contract and bounded graph

**PREPARED-UNAUTHORIZED.** Goal: complete the two Mac-listed offline F1
prerequisites. Done when: behavioral subprocess proof, conservative dynamic
import gate, active sibling repair, focused tests, documentation/safe gates,
evidence conservation and a clean pushed review candidate are recorded.
Not in scope: WSL, registry/lifecycle/cleanup action, another live attempt,
authority/ruling edits, full functional ring, pack metadata/version changes,
or unrelated repairs. Tier T1; one author; fan-out cap 1; repair cap 2.
The parent owns fresh independent review and the join-ring decision.

The branch starts at `b27c4f4a` and fast-forwards to exact requested receipt
base `d860ec33bcbaa9bf6e18fba024c3654641d642b0` before committing. The seven-file
delta is receipt/audit/coordination/ruling history, with no code change. All
that history is preserved. This task grants no new live authority.

The execution graph is: grounding -> two red controls -> minimal loader fixes
and subprocess fixture -> focused union + scanner -> canonical metadata
derivation -> final docs/safe gates -> conservation -> commit/push -> review.
The two control designs are independent; both join before final proof. Real
dependencies, red-first, unknown-state refusal, audit and immutable evidence
are floors. No author-spawned agent or broad ring is needed. Actual graph adds
one bounded reviewer correction for definition headers, conditional factory
aliases, registry rebinding and portable production-argv derivation. No third
repair cycle is authorized. Planned cost: fast/docs gate emits measured cost;
focused tests and safe gates only. Actual costs and gate counts are emitted in
`offline-followups-validation.json`; elapsed session cost is in the audit log.

# Behavioral execution boundary

`tests/test_f1_r228_cleanup.py` removes `test_owned_checker_is_read_only`,
which only searched source text. Its replacement obtains each stage/guard argv
from the actual `Cleanup.owned_check` caller using a capture mock. It uses
`PureWindowsPath` for the synthetic Windows repository root on either host.
It preserves all caller argument names/order/run identity. At native child
launch it maps `/usr/bin/python3` to `sys.executable`, maps the checker to the
actual local production helper, and replaces only evidence-root and
owned-control filesystem seams with temporary paths.

The owned-control fixture executes the real packet definitions, including
postponed-annotation dataclasses and owner-record/group matching. It replaces
only process enumeration with an empty fixture and makes process/signal actions
raise. The production helper executes as an independent subprocess; both kinds
must return zero, exactly one `OWNED-ABSENT` stdout line, and empty stderr.
Line comparisons accept host line endings while refusing extra output.
This proves the offline absence protocol with synthetic empty ps, not actual
live process absence. No WSL or production cleanup command runs.

Red proof: an ignored temporary copy of the production helper, differing only
by removal of its `sys.modules[spec.name] = module` line, is launched through
the new test. Stage and guard each return 5; the test has two failing subtests
(`5 != 0`, one test, 0.160 s). Repository helper/evidence bytes remain unchanged.
The separate new negative control deliberately drops fixture registration,
asserting empty stdout and only `F1-OWNED-IMPORT-FAILED` on stderr, without a
process/signal call. Production stable diagnostic/receipt behavior is unchanged.
Green focused F1 union before final metadata: 95/95, 9.444 s. Final execution
results are in the validation companion. Mac execution remains unassessed here;
the portable Windows-path value avoids host-dependent argv construction.

# Class sweep and deterministic gate

Class: `IMPORTLIB-UNREGISTERED-MODULE`, also registered as
`DYNAMIC-IMPORT-MODULE-REGISTRATION`; sibling class:
`TEST-ASSERTS-SOURCE-NOT-BEHAVIOUR`. Derive: register every manually created
module in the real `sys.modules` namespace before execution or other use.
Prevent: `tools/check-importlib-registration.py` scans every tracked Python
file using AST flow, including active managed runtime code. `tools/check-docs.py`
runs both its self-test and real-tree scan. Ring: fast/docs, measured cost printed
on every normal run; first staged real scan 251 files, zero failures, 1.430 s.

Initial real-tree scan was red on the active/historical unregistered cohort.
The minimum fix adds 24 immediate registrations in 20 active files: ten pack
hooks/scripts, five repository tools, three test modules and two F1 recovery
loaders. `index_retained.py` also imports `sys`. No pack managed blocks,
metadata, versions, wrapper/helper authority or process behavior changes.

The scanner refuses unsupported creation shapes. It follows imported factory
and registry aliases, checks same-block order, key/module identity, conditional
registration and use/rebinding before registration. Factory identities survive
conditional rebinding as uncertain calls that refuse. Definition defaults,
decorators, bases, annotations and exception headers are inspected. Registry
and spec accesses/rebindings between creation and registration refuse.
Its 24 flow controls include independent-review counterexamples; the new
definition-default case was red before the gate correction. Six policy controls
prove exact pin acceptance, stale/hash-changed/new-unpinned refusal, and forbidden
tool/F1 active pins. Final self-test/real results are in the companion.

# Active versus immutable boundary

Active loaders in `tools/`, `tests/`, `docs/ai-forward-pack/`, and F1
`recovery/`, `packet/`, `attempt-1/cleanup/` are repaired, never pinned. Ten
historical proof programs remain unchanged; exact path, SHA-256, offending
line list and immutable-historical reason are in
`tools/importlib-historical-pins.json`:

- `docs/proof/ofn/spike-windows-shim.py`
- `docs/proof/rdh/plant-check.py`
- `docs/proof/win-gpu-f1-preflight/integration/verify_integration.py`
- `docs/proof/wri-r182-runner/capture-proof.py`
- `docs/proof/wri-r182-runner/capture-r183.py`
- `docs/proof/wri-r182-runner/source-snapshots/e9714ce5/check-windows-runner.py`
- `docs/proof/wri-r184-driver/capture-preparation.py`
- `docs/proof/wri-r184-driver/r185/capture-r185.py`
- `docs/proof/wri-r184-result/package-blocked.py`
- `docs/proof/wri-r184-result/source-snapshots/1477951f/check-windows-runner.py`

These are retained capture/plant/spike/reconstruction scripts and managed proof
source snapshots, not current runtime exceptions. Each was compared against the
exact base blob before pin creation. New unpinned findings, changed hash/line
findings, stale pins, paths outside `docs/proof`, and active F1 pin attempts fail.
A new manifest entry remains a visible review-required policy change with an
immutable-historical reason; the manifest digest itself is not frozen. This
classification is an independent-review boundary. The gate is conservative
static flow detection, not proof of arbitrary reflective/interprocedural Python.

# Final checks and residuals

Commands: `py -3 -m unittest discover -s tests -p 'test_f1*.py' -v`;
`py -3 tools/check-importlib-registration.py --self-test` and real scan;
`py -3 tools/check-docs.py`; safe `run-verify-gates.py` with only the existing
application-core/application-adapters/windows-store readiness skips. The
validation JSON records exact counts, status, elapsed time and conservation.
No full functional ring is run on this author branch, per parent instruction;
join/readiness and independent acceptance remain parent-owned. This document
does not change R230 evidence, the joined unregister receipt, or authorization.
