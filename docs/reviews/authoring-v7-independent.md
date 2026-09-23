---
id: review-authoring-v7-independent
title: Authoring decisions v7 — independent review
type: doc
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [review, ux, geometry, testing, foildsl]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: spec-foildsl, rel: documents}
  - {to: mockup-workbench-v7, rel: relates-to}
  - {to: decision-design-iteration, rel: relates-to}
  - {to: review-authoring-v7-gaps, rel: relates-to}
review-by: 2026-12-22
summary: Independent adversarial review of the five authorized authoring improvements, with runtime geometry, transaction, keyboard and rendered-surface evidence. Native persistence, certified geometry and formative usability remain explicitly unverified.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
  - { by: mockup-workbench-v7, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
---

# Authoring decisions v7 — independent review

Reviewer: an agent separate from the product/language author and mockup author. Date: 2026-09-22.
Scope: the five approved authoring improvements and three specification contradictions. Further findings
are captured in the [bounded gap review](authoring-v7-gaps.md); they do not authorize more implementation.

## Contract review

**Verified.** Product A4.14, B10/F10, CAD-09–13 and UI-31–35 retain one accepted FoilDSL shape authority.
Profile sharing is an assignment relationship. Forking changes a profile reference at one station, while
its blend affects adjacent intervals. Keep current t/c preserves the existing channel; Use source thickness
authors explicit targets on that same channel and must refuse infeasible locks. Selection is inspection,
separate from the pinned draft owner. Alternatives, baseline references and decision facts belong to the
project, not a second shape grammar. Discard retains the alternative's accepted source and rationale.

**Verified.** Full upper-minus-lower t/c, independent inverse-x profile correspondence and project versus
shape opening now agree across the product, normative language and decision note. The misleading span
label “Keep proportions” was rejected during review: the chosen command keeps relative station positions,
not all dimensional proportions. The held rail and LE-pivot placement distinction remain explicit.

**Inferred, not established by automation.** Persistent entry and explicit edit scope should improve
findability and scope prediction. UX-26 supplies a falsifiable five-person task; it has not been run.

## Runtime and visual evidence

The independent oracle is [check-authoring-v7.mjs](../../tools/check-authoring-v7.mjs), with measured
results in [authoring-v7-browser-check.json](../proof/authoring-v7-browser-check.json): **25 checks and
33 layout/theme cells pass, with zero page errors or external requests**. Its initial red run against v6
observed no persistent section entry. The layout matrix includes 15 entry cells, nine accepted-section
cells and nine active-section-draft cells; every shell overflow flag is asserted. No native or scientific
conformance is inferred from a browser pass.

The oracle exercises real UI events for shared/fork editing, accepted-source coherence, full t/c,
assignment and bank Undo/Redo, source-thickness lock refusal, draft-safe inspection, chord held-edge intent,
span policies and infeasible shrink, baseline stability, Keep/Discard rationale, read-only archived source,
and hostile names remaining literal without network requests or script execution. It also checks Enter in
a dialog field, on Close and on section Cancel cannot apply an unrelated draft.

Two representation probes strengthen the geometry evidence:

- Different source abscissae (`x^1.6`) produce a mid-span blend checked at 99 cosine-distributed x values
  per side against a separate inverse-x calculation. The observed maximum normalized-chord error was
  `3.15e-11`. Both calculations use the declared 201-point maximum-thickness normalization; this does
  not certify a continuous maximum.
- Switching between profiles with 12 and 6 CVs updates the editor basis, handle count and remembered
  selection without changing accepted source or sampled placed geometry. This checks every profile is
  read through its own representation, not the first profile's global count.

The retained rendered views show the [entry](../proof/authoring-v7-entry.png),
[section draft](../proof/authoring-v7-section.png), [reflow](../proof/authoring-v7-section-zoom.png),
[dimension intent](../proof/authoring-v7-intent.png) and [alternatives](../proof/authoring-v7-alternatives.png).
The reviewer inspected these rendered surfaces, rather than judging the HTML alone. Reflow intentionally
scrolls the geometry locally to preserve handle size. It does not shrink the entire editor into tiny targets.

Preserved floors, inspected from their final reports:

- [Source regression](../proof/foildsl-v7-browser-check.json): 13 checks, 15 layout/theme cells,
  zero page errors or external requests.
- [Independent rails](../proof/independent-edges-v7.json): six checks, including opposite-record and
  sampled-geometry invariance with unequal rail bases.
- [CAD regression](../proof/workbench-v7-browser-check.json): 16 groups, 77 measurements and 30 shell
  cells, zero page errors. These inherited checks do not replace the new authoring oracle.

## Findings and resolutions

| Severity · confidence | Observed finding | Resolution and proof |
|---|---|---|
| Major · Verified | A 201-point linear lookup introduced about 0.00680c error near a sharp LE despite correct equal-x intent. | Evaluate each requested x by direct inverse curve evaluation; retain bounded caching. The full cosine probe passes. Only sampled normalization remains a declared approximation. |
| Major · Verified | Remembered section count/selection could belong to another profile. | Read the actual profile basis and clamp an out-of-range selection; 6/12-CV inspection oracle passes. |
| Major · Verified | Enter on native controls could apply an unrelated section draft. | Exclude native controls/dialogs from editor Apply capture; Enter-on-input/Close/Cancel regression preserves accepted source. |
| Major · Verified | Zoom shrank section handles below the 24 px floor; adding context also exposed a similar master-rail resize failure. | Preserve section and quadrant minimum geometry sizes with local scrolling. The final 33-cell oracle measures zero undersized targets and checks all shell flags. |
| Major · Verified | Long drafted section titles overflowed the narrow document-tab row. | Shrink and ellipsize the tab while retaining its accessible full label and close control; all shell flags are checked, including active drafts. |
| Minor · Verified | Scope controls were undersized; hardcoded profile/thickness labels contradicted actual policy and inspected target. | Measured controls and derived profile/policy labels replace the stale surfaces. |
| Minor · Verified | The left bottom status could say No draft while the section tab, owner banner and right status message showed a section draft. | The status producer now reads the shared draft owner. Direct refresh before and after Cancel passes; the refreshed retained screenshot shows Draft open. |
| Minor · Verified | Neighboring TE tangent and closure handles overlap at the default zoom. | Keyboard selection remains available; dense endpoint pointer disambiguation is a retained polish limitation, not claimed repaired. The differing-count oracle uses a separated interior handle. |

The review also required actual physical interval distances, held quantities, fit residual/tolerance and
conservative affected-span disclosure. These are present. Missing comparable analysis remains Unavailable;
the prototype does not turn illustrative scientific numbers into design evidence.

## Independent hard-veto disposition

```text
PERSONA: computational-geometry-expert   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Independent inverse-x section readers, full-thickness placement and held-edge intent agree across source, record and rendered geometry. evidence: equal-x stress probe, 6/12-CV reader probe, full t/c and held-rail oracles. fix: completed.
CLEARS-THE-VETO: yes — the bounded preview demonstrates the specified representation and refuses unsupported or infeasible edits.
RESIDUAL RISK: 201-point maximum normalization and sampled validity are not continuous certificates; exact blended-slice promotion remains unproved and refuses in this prototype.
```

```text
PERSONA: data-persistence-architect   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Profile bank, assignment references and source accept atomically; alternatives retain accepted snapshots and append decisions with rationale. evidence: source coherence, bank Undo/Redo, baseline/Discard and read-only archive oracles. fix: completed.
CLEARS-THE-VETO: yes — one authored shape authority and project-level decision history remain distinct; no native migration or durable storage implementation is claimed.
RESIDUAL RISK: Native save/recovery and external-file conflict handling require the recorded production gates and remaining policy decision.
```

```text
PERSONA: ux-researcher-ia   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Entry, edit scope, held quantities, draft ownership and baseline decision form a complete inspect/edit/compare loop. evidence: B10/F10, CAD-09–13 and the executed browser flows. fix: completed.
CLEARS-THE-VETO: yes — the UX specification is testable and the prototype demonstrates its bounded task; ease of discovery is not asserted as validated.
RESIDUAL RISK: UX-26's unaided five-person study remains unperformed. Geometry comparison correspondence after span changes is a recorded follow-up decision.
```

```text
PERSONA: ux-accessibility   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Native-control Enter no longer applies an unrelated draft; section and rail handles retain target sizes under reflow. evidence: keyboard Cancel/Close/input oracle, 33 cells, retained screenshots. fix: completed.
CLEARS-THE-VETO: yes — named keyboard controls, measured contrast/targets and complete changed states satisfy the scoped prototype review; retained minor polish findings are explicit.
RESIDUAL RISK: This is not a complete assistive-technology or native-platform certification. Dense TE pointer disambiguation and narrow-context readability remain polish risks.
```

```text
PERSONA: test-architect   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) The proof detects mismatched basis readers, sharp-LE interpolation, unrelated keyboard writes and active-draft reflow failures; inherited floors remain attached. evidence: red-before-green, 25 authoring checks, 33 cells, source/edge/CAD reports. fix: completed.
CLEARS-THE-VETO: yes — state assertions span source, model, history and rendering; each gate claim is bounded by its measured coverage.
RESIDUAL RISK: Browser samples and an exit code cannot establish continuous geometry, scientific correctness, native persistence or usability outcomes.
```

```text
PERSONA: the-simplifier   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Existing grammar records express profile forks and dimension changes; project snapshots express alternatives without a new shape language or runtime dependency. evidence: product/DSL/decision-note agreement and dependency-free v7 artifact. fix: completed.
CLEARS-THE-VETO: yes — additional machinery addresses authorized scope and disclosed limitations; no stack/backend or production implementation was introduced.
RESIDUAL RISK: Prototype wrappers are an illustrative implementation boundary, not an approved production architecture.
```

## Additional domain dispositions

These two lenses complete the requested persona coverage. They review the changed authoring and comparison
surfaces only; they do not certify the inherited scientific fixtures or production solver behavior.

```text
PERSONA: hydrofoil-hydrodynamicist   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Effective t/c is the full upper-minus-lower maximum; placed sides use camber ± thickness/2. Rule A interpolates shape at equal normalized chord x without asserting a hydrodynamic benefit. evidence: FoilDSL §6; full-thickness and independent inverse-x oracles. fix: completed.
  - [Minor] (Verified) New area/AR values are labelled sampled geometry, and comparison drag/lift remains Unavailable without comparable evaluated runs. evidence: retained intent/alternatives screenshots; knowledge area 07 presentation rule distinguishes geometry and method-bound physical evidence. fix: no new physical-performance claim was introduced.
CLEARS-THE-VETO: yes — changed surfaces make no unsupported physical-validity or safety claim; x aft/y starboard/z up, LE pivot and nose-up incidence agree with the recorded frame and preserved sign fixture.
RESIDUAL RISK: Shape changes alone cannot establish improved lift, drag, cavitation, ventilation, strength or handling. Production comparisons still need pinned method/envelope/water/depth and comparable evidence; the 201-point normalization is not a geometry certificate.
```

```text
PERSONA: marine-cad-ux-expert   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Shared versus independent profile edits name affected assignments and neighboring blend intervals; keep/source thickness, held chord edge and relative/absolute station placement state what remains fixed. evidence: CAD-09–13, authoring oracle and retained section/intent views. fix: completed.
  - [Minor] (Verified) Control vertices remain influence controls with typed/nudge paths, while section inspection does not retarget an open draft. Return/Cancel semantics exclude native dialog controls. evidence: retained section view, keyboard oracle, inherited CAD floor; knowledge areas 01/03 control-point and modal editing conventions. fix: completed.
CLEARS-THE-VETO: yes — changed modes, authority, locks and LE-pivot consequences are explicit; the bounded prototype preserves the recorded precision and platform contracts. Removing unsupported loft-mode names is justified by the single normative Rule A evaluator, rather than teaching fictional operations.
RESIDUAL RISK: Dense TE pointer disambiguation and narrow-context readability need further craft/usability work. The recorded unaided task and native trackpad/DPI behavior remain unverified; no additional implementation is authorized by this review.
```

## Verification limits

**Closing label-only check:** after the 25-check/33-cell run, the shared status producer received its final
reader correction. Two [targeted direct-refresh checks](../proof/authoring-v7-status-check.json) pass:
an active section renders Draft open, and Cancel restores No draft, with zero page errors. The same
assertions are retained in the main authoring oracle. All five screenshots were refreshed and the section
status visually re-inspected. This label correction does not change geometry or transaction semantics.

This is a dependency-free browser prototype. Native Windows/macOS behavior, durable archive/recovery,
continuous geometry validity and scientific results are not proved. The shape-preserving promotion of an
arbitrary blended slice remains a production feasibility question; multi-profile promotion currently
refuses rather than silently changing geometry. No solver, stack, deployment or merge decision is made.
