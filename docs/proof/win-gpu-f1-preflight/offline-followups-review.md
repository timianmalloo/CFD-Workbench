---
id: proof-win-gpu-f1-offline-followups-review
title: "Independent acceptance of F1 offline controls"
type: proof-pack
status: accepted
owner: "@pc-coordinator"
phase: preflight
tags: [windows, offline, independent-review, importlib]
links:
  - { to: proof-win-gpu-f1-offline-followups, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  Independent ACCEPT of exact candidate 7affd893 for the two Ruling 231 offline
  controls, with behavioral mutation proof and immutable evidence conservation.
---

# Decision and exact binding

**ACCEPT** candidate `7affd8932eb304f282008d8e662d98ce77f6653a`, whose sole
parent is `d860ec33bcbaa9bf6e18fba024c3654641d642b0`. The independent reviewer
observed this exact HEAD and a clean author worktree before and after validation.
This accepts the two Ruling 231 offline controls; it grants no live authority.

Goal: durably bind the independent review to that candidate. Done when this
review, validation metadata, derived index and audit are committed and pushed.
Not in scope: source edits, WSL, a full functional ring or a new live ruling.
Tier T0; no delegated agents. Bounded graph: bind observed results -> write
review/metadata -> derive index/audit -> docs validation -> commit/push.

# Independently observed evidence

All results below are **Verified** by reviewer execution on native Windows.
Times are command-reported measurements; unmeasured checks have no invented cost.

| Command or check | Observed result | Seconds |
| --- | --- | ---: |
| `py -3 -B tools/check-importlib-registration.py --self-test` | 24 flow cases and 6 pin cases pass | 0.003 |
| `py -3 -B tools/check-importlib-registration.py` | 251 tracked Python files; zero failures; 10 historical pins | 1.515 |
| `py -3 -B -m unittest discover -s tests -p test_f1_cleanup*.py` | 64 tests pass | 2.120 |
| `py -3 -B -m unittest discover -s tests -p test_f1_r228_cleanup.py` | 20 tests pass, including stage/guard production-argv subprocesses | 0.238 |
| Production-argv test with `REL`, `FAILURE_REL` and `PACKET_REL` converted to `PurePosixPath` | Both child checks pass with the fixture's `PureWindowsPath` repository | 0.161 |
| Production-argv test against a temporary helper with only its registration line removed | Both stage and guard fail with exit 5 (`5 != 0`); expected red control | 0.179 |
| `py -3 -B docs/ai-forward-pack/scripts/verify-portable-text-io.py` | LF and printing-CLI console checks pass | not recorded |
| Pin-policy calls with changed line list and empty reason | `IMPORTLIB-PIN-CHANGED` and `IMPORTLIB-PIN-INVALID` respectively | not recorded |

The mutation check copied `cleanup_owned_check.py` outside the repository,
removed its sole `sys.modules[spec.name] = module` line, redirected the existing
test's `SCRIPT` to the temporary helper directory and ran the same test. Its two
failed subtests were asserted as the expected outcome. No tracked helper changed.

The author corrected three independently reproduced scanner false negatives:
definition defaults, registry rebinding before registration and conditional
factory-alias rebinding. Each now refuses and has a self-test. The argv fixture
also changed from host-dependent `Path` to `PureWindowsPath`. No hard veto remains.

# Conservation and limits

Git tree comparison against the exact parent found 3,798 existing proof blobs.
Only the approved `recovery/recover.py` and `recovery/index_retained.py` changed;
the remaining **3,796 blobs are identical**. All ten historical pin SHA-256 values
match parent blobs, and those blobs remain unchanged. The production helper,
R230 evidence, joined manual-unregister receipt and `docs/notes/rulings.md` are
unchanged. Active loader diffs add registration before execution; the retained
historical programs are not active runtime exemptions. check-docs invokes both
the scanner self-test and the repository scan.

Native macOS execution remains unassessed. The path-flavor test proves argv
construction on this host, not a macOS runtime run. The gate is conservative
static detection, not proof of arbitrary reflective/interprocedural Python.
New manifest entries remain visible policy changes requiring independent
historical classification review; the manifest digest is not frozen.

The author's full docs/safe-gate records were inspected, not independently
re-executed during candidate review. Publication runs docs validation on this
documentation-only descendant: `py -3 tools/check-docs.py` completed with exit 0,
`Documentation checks passed.`, and the existing 134 advisory freshness findings.
No source edit, WSL or full ring is authorized
by this review. The validation companion retains the author's measurements and
adds the independently measured acceptance record.
