---
id: proof-wri-r184-blocked-result
title: "Ruling 184 Windows execution: two blocked cycles"
type: proof-pack
status: blocked
owner: "@win-wri-r182-runner"
phase: implementation
tags: [windows, runner, ruling-184, blocked, proof]
links:
  - { to: review-pr-24, rel: relates-to }
  - { to: proof-wri-r182-runner-ready, rel: depends-on }
  - { to: proof-sdg, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Both authorized execution cycles stopped at the initial preflight. Cycle 1 passed
  callback text as the script path; the reviewed repair fixed that binding. Cycle 2
  rejected an absent Settings frame. No scale mutation, build, product check, restore,
  or fresh scale readback ran. The two-cycle cap fired and readiness remains closed.
---

# Ruling 184 blocked Windows execution receipt

**Verified blocked.** Both cycles failed before any scale mutation. None of the 14
checks was assessed at either scale. Current display scale and a fresh 1.5/1.5
reading were not observed in this execution. Windows PASS and readiness remain closed.
The unchanged Ruling 181 store verifier is still queued; it was not executed.

## Goal, budget and provenance

Goal: execute the unchanged 14-check contract at 150% then 200%, restore 150%, and
observe fresh in-process RenderScaling/PrimaryScaling 1.5/1.5. Done when both scale
results, numeric exits, source equality and restoration proof are retained.
Not in scope: store verifier or unrelated checks. Tier T1; fan-out 0.

The coordinator authorized at most two execution cycles and 20 minutes, starting
at **2026-10-09 19:26:46Z**, with deadline **19:46:46Z**. Cycle 1 failed, one reviewed
repair was committed, and final cycle 2 failed. The stop was reported at 19:34:40Z.
The **two-cycle cap fired** even though wall-clock allowance remained. No further
implementation edit, retry, scale action or verifier followed. A new execution needs new authority.

Cycle 1 HEAD: `36ccfa49bc8c57f48697b991ce6373a8f6806907`.
Cycle 2 HEAD: `1477951f6cff53f7f9f6eee111425738dcc24b96`.
The repair's separate Astra PASS was reported by the coordinator before cycle 2.

Each command used an absolute Python executable obtained read-only with
`py -3 -c 'import sys; print(sys.executable)'`. Its user-home path was not published.
The exact command shape was:

```powershell
pwsh -NoProfile -File tools/windows-scale-run.ps1 -Action Run -PythonPath <absolute-py-3-sys.executable> -OutputDirectory docs/proof/wri-r184-result
pwsh -NoProfile -File tools/windows-scale-run.ps1 -Action Run -PythonPath <absolute-py-3-sys.executable> -OutputDirectory docs/proof/wri-r184-result-cycle2
```

Each output directory was confirmed absent before its command. Root numeric exits
were tool-observed: cycle 1 exec chunk `3241cf`, exit 1, 8.407 s; cycle 2 exec chunk
`2f1fbe`, exit 1, 9.733 s. Root stdout/stderr existed only in those tool responses;
no filesystem raw root capture existed. This receipt does not reconstruct one.

The live `run.json` files are the driver's published derivatives. They retain
child stdout/stderr, numeric exits, cleanup disposition and original raw-stream
SHA-256 bindings. Temporary raw child files were not retained. Each child's PHN
status was PASS with an empty substitution list. Empty `publication.json` means
no ledger substitutions, not a successful product run.

## Observed child records

| Observation | Cycle 1 | Cycle 2 |
|---|---|---|
| Child | preflight-initial | preflight-initial |
| Started UTC | 19:27:23.6159017Z | 19:34:24.0160224Z |
| Ended UTC | 19:27:26.5251534Z | 19:34:28.3286109Z |
| Numeric exit | 64 | 1 |
| Child ceiling | 60000 ms | 60000 ms |
| Child wall | 1119 ms | 2765 ms |
| Shared-clock entry/final | 1326/4232 ms | 1433/5741 ms |
| Driver receipt elapsed | 4789 ms | 6305 ms |
| Timeout | false | false |
| Cleanup | none-active-verified | none-active-verified |
| Residual status | job-active-zero-verified | job-active-zero-verified |
| SourceUnchanged | true | true |
| RestoreExit / ReadbackExit | null / null | null / null |

Both records used Normal mode. Build/check/readback children would have used
BuildVerifier mode with a 120000-ms end-to-end child ceiling, giving the root at
most half that allowance. None launched. The declared driver ceiling is 1200000 ms,
with an execution deadline of 1020000 ms reserving 180000 ms for restoration and
publication. Those are allowances, not measured execution capacity.

**Cycle 1:** `-File` received the text of the Preflight callback instead of the
preflight script path. PowerShell's case-insensitive dynamic scope resolved the
outer `$preflight` path name to the sequence's `$Preflight` callback parameter.
The child printed usage and rejected the argument as a script filename. No UIA
preflight actually executed, and no build or selection followed.

**Cycle 2:** the repaired `-File` argument named
`tools/windows-settings-preflight.ps1`. The actual read-only UIA preflight rejected:

> WRI-PREFLIGHT: exact Settings frame absent; prepare Settings manually

No selection/build/product child followed. Initial preflight runs before the
mutation/restore block. Because neither attempt mutated scale, restoration and
fresh readback were not entered. Their exits are **not recorded**, represented by
null; they are not zero or PASS. No Settings preparation was attempted after cap.

## Repair proof and implementation binding

The sole live-track repair renamed the path variable to `$preflightPath` at its
declaration and both path call sites. No other production behavior changed.
`tools/test-windows-scale-run.ps1` now invokes the actual driver callback with
external children/source/toolchain probes stubbed, stops before build, and checks
the actual preflight `-File` argument against the script path.

The boundary test was red before the rename (exit 1, 3.147 s) and green afterward
(exit 0, 3.020 s). The targeted policy check was exit 0, 1.837 s capture wall,
with `WRI-POLICY PASS controls=a,b,c,d,e,f,R181`. Evidence is retained in
`../wri-r184-driver/callback-repair-red/` and `callback-repair-green/`. The red
artifact is named `restore-green` by the reused collector's gate selector;
its recorded numeric exit 1 and boundary error establish its actual red outcome.
These stubs did not change scale or execute product checks.

| Implementation | Cycle 1 SHA-256 | Cycle 2 SHA-256 |
|---|---|---|
| tools/windows-scale-run.ps1 | fae58d4c71ecb110467f90842774c20c814cdec7c15dbb081469b2c846b8e834 | d153d413ea618e8d4cc5346236f0d7c9ec57e38f04312ae0dfaa5d8044a63ff4 |
| tools/test-windows-scale-run.ps1 | 39f89e36e429adeb8472554006098f4fb6dc03942ab9cf3b3b3d9502691dcf04 | 4742af74ad76a5547005948424cd0ccf3a905793a4fd1d8b60706b95d37f6163 |
| tools/windows-runner.ps1 | cc201aca4cd2e9e434bec1b9630487a2184622d2ece08617c373d037d528848d | cc201aca4cd2e9e434bec1b9630487a2184622d2ece08617c373d037d528848d |
| tools/windows-settings-preflight.ps1 | ee80b2676ab74c1b58da6918a1ad4e0ab0223a78c99b8775fccd2eab10f2c17b | ee80b2676ab74c1b58da6918a1ad4e0ab0223a78c99b8775fccd2eab10f2c17b |
| tools/check-windows-runner.py | 7bdae949718480bb9984ef30e75632d0d17cf04ba0161d478e35d2220af580ed | 7bdae949718480bb9984ef30e75632d0d17cf04ba0161d478e35d2220af580ed |
| tools/test-windows-runner.ps1 | 4907aa2c94276b27115230080020e764c3f2a375079847b9d4520247bc7b1f3d | 4907aa2c94276b27115230080020e764c3f2a375079847b9d4520247bc7b1f3d |

The earlier owner-approved preparation qualification remains separate evidence:
`../wri-r184-driver/r185/qualification.json`, overall exit 0, 43.794 s, SDK
10.0.203 and Python 3.13.14, numeric build-server shutdown 0. It did not assess
product behavior or establish the live Settings state. No full preparation
qualification was rerun after the live path repair; only the targeted tests above.

## Fourteen-check disposition

| Item | Exact check | Route | 150% | 200% |
|---|---|---|---|---|
| 1 | KeyBindings_MenuGesture_NotBound | shell-window | NOT ASSESSED | NOT ASSESSED |
| 2 | ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView | views | NOT ASSESSED | NOT ASSESSED |
| 3 | ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack | views | NOT ASSESSED | NOT ASSESSED |
| 4 | Elevation_SideSelectedStation_RenderedFullWeight | views | NOT ASSESSED | NOT ASSESSED |
| 5 | View3d_SelectedStation_RenderedWidthAndChip | views | NOT ASSESSED | NOT ASSESSED |
| 6 | PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs | properties-cells | NOT ASSESSED | NOT ASSESSED |
| 7 | PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused | properties-cells | NOT ASSESSED | NOT ASSESSED |
| 8 | PropertiesPane_Density_EveryTargetAtLeast24 | properties-cells | NOT ASSESSED | NOT ASSESSED |
| 9 | PropertiesPane_Density_EveryInputDeclaresMinHeightOf24 | properties-cells | NOT ASSESSED | NOT ASSESSED |
| 10 | ModelArea_Views_SeparatedByGutterAndFramed | readiness | NOT ASSESSED | NOT ASSESSED |
| 11 | Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870 | readiness | NOT ASSESSED | NOT ASSESSED |
| 12 | View3d_ChipBorderSampler_FindsStationColourInTheLoggedWindowsBlock | views | NOT ASSESSED | NOT ASSESSED |
| 13 | Elevation_ChipBorderSampler_HoldsHalfOfASplitLine | views | NOT ASSESSED | NOT ASSESSED |
| 14 | View3d_CubeFocusRing_GapOnCurrentFaceThreeToOne | views | NOT ASSESSED | NOT ASSESSED |

The item-10/11 route would select exactly those two names together through
CFD_TEST_ONLY and --readiness. No route launched; no SCALE_CONTEXT, ITEM6 or P3
product observation exists in these cycles. `ContractFailed=false` only means
no contract child set that flag; it is not a PASS.

## Source and capture proof

Each ledger retains a 411-row baseline over src/, tests/, tools/, global.json and
CFDWorkbench.slnx. SHA-256 of the exact UTF-8 baseline string:

- Cycle 1: `6543ff65b9456fb6cf36afb194286d979e264f14bbba96b99c8d7927b8e54ed8`.
- Cycle 2: `3a04530e9c9078a45c6c1de2870b283d0852709ffca83b052dfffaf2ffdee9a7`.

The fingerprints differ across attempts because the reviewed tools repair changed
the execution head. Within each attempt the final comparison completed and the
driver published `SourceUnchanged=true`; a separate final fingerprint string was
not published. Post-cycle `git diff --exit-code HEAD -- src tests tools global.json
CFDWorkbench.slnx` returned 0. The same comparison between the two execution heads
excluding tools returned 0. Independent 315-file src/tests/global/slnx before/final
fingerprint is `1edf8197872ec541eb81cec168ddd1ce24c2728463728f8869e31457aa26ce2e`
in the preparation captures; packaging rechecks it in `source-state.json`.

Live ledger hashes:

- Cycle 1 run.json: `29794889cda07730b216468ad9d46659d811efc65b46d37912aa27d7b4bf5876`.
- Cycle 2 run.json: `6caa99f1786a1737e775f234e4180c3cba9dca714ea8d0753e9e24e14c4e1754`.
- Both publication.json files are empty bytes: SHA-256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.

The capture manifest binds this folder; the closing manifest also binds cycle 2,
repair evidence, prior preparation qualification and immutable snapshots of the
implementation at both execution heads. They are generated from staged bytes with the repository's sealing script
and checked against committed blobs after local commit. No product or store
verifier was executed during packaging. Readiness remains closed.

## Packaging validation

This is a docs-only follow-up to the two committed implementation heads. Closing
commands are `py -3 tools/check-proof-pii.py`, with CFD_PII_HOSTNAMES set from local
COMPUTERNAME only in the capture process; `py -3 docs/ai-forward-pack/scripts/docs-graph.py
validate`; `py -3 tools/check-docs.py`; `git diff --check`; and after commit,
`py -3 tools/check-capture-manifests.py`. No tools/run-tests.sh or readiness/product
ring is run for this packaging-only delta.

PII stdout/stderr and numeric exit are retained in `pii.*`. The initial check-docs
capture `docs.*` is retained red: exit 1, 107.570 s. Its manifest failure is
classified below. Graph validation was tool-observed exit 0 with zero defects and 131
existing review suggestions; no filesystem raw graph capture existed. The audit
entry is `al-01M4H300C80Q6Q7BJB81XD7NAG`. Gate results attest the packaging checks,
not the unexecuted scale contract.

## Immutable source handoff

The initial packaging check-docs failed because the historical R182 closing
manifest still named four live tools paths. It declared the approved R182 bytes,
while HEAD contained their later R184 versions. This was a committed branch
source-evolution mismatch, not a new receipt or audit defect. The failed capture
is preserved. No assertion about origin/main drift is made.

The coordinator reported Mac authorization and Astra's required handoff: preserve
the approved source as immutable snapshots and rebind only the four paths. Each
snapshot below is the exact Git blob from source commit
`e9714ce525681ade39aac19e0d86e80ab240907a`. No snapshot was executed.

| Original path | Source Git blob | New path |
|---|---|---|
| tools/windows-runner.ps1 | e7f1ae9f4c5c21514aa66eb4d443dcf8d405b7f3 | docs/proof/wri-r182-runner/source-snapshots/e9714ce5/windows-runner.ps1 |
| tools/windows-settings-preflight.ps1 | 0d4942e7e9fcaa18204ca81b5f80b838ba378d03 | docs/proof/wri-r182-runner/source-snapshots/e9714ce5/windows-settings-preflight.ps1 |
| tools/test-windows-runner.ps1 | 90f45d80ea1537d9468767f5ec675ea4a711ca40 | docs/proof/wri-r182-runner/source-snapshots/e9714ce5/test-windows-runner.ps1 |
| tools/check-windows-runner.py | 1cc0ab16f5d90c6f57870f8de5db73d42f4df47f | docs/proof/wri-r182-runner/source-snapshots/e9714ce5/check-windows-runner.py |

The R182 closing manifest retains all 39 entries, each original byte count and
SHA-256, and every other field. Only these four path values change. Historical
R182 receipts, captures, and current tools/checker remain unchanged.
`source-bindings.json` records the original paths, full source commits, blob IDs,
snapshot paths, byte counts and SHA-256 for these four blobs and eight R184 blobs:
six at cycle-2 HEAD `1477951f6cff53f7f9f6eee111425738dcc24b96`, plus the two changed
driver/test blobs at cycle-1 HEAD `36ccfa49bc8c57f48697b991ce6373a8f6806907`.
The R184 closing manifest binds these immutable snapshots, with no live tools
entry. Narrow `-text` attributes preserve their Git bytes.

Packaging is committed before HEAD-based manifest/check-docs validation. The
post-handoff docs gate capture uses `docs-repair1.*`; that name denotes a
packaging gate retry, not another implementation repair or live execution cycle.
Manifests are resealed after appended gate evidence. Readiness and the execution
budget remain closed throughout this handoff.
