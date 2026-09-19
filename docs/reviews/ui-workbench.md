---
id: review-ui-workbench
title: CFD-Workbench interface review and proof
type: proof-pack
status: in-review
owner: "@timianmalloo"
tags: [ui, accessibility, geometry, review]
links:
  - {to: mockup-workbench, rel: documents}
  - {to: design-language, rel: documents}
  - {to: spec-cfd-workbench, rel: documents}
  - {to: proof-native-ui-workbench, rel: depends-on}
  - {to: review-specification-gate, rel: depends-on}
review-by: 2026-12-19
summary: Independent review clears the HTML workbench for design iteration after correcting geometry authority, historical results, keyboard focus, narrow navigation and partial-field rendering. Four minor craft findings and unverified native, scientific and full accessibility obligations remain explicit.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract." }
  - { by: mockup-workbench, on: 2026-09-19, reason: "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof." }
---

# Interface review

This is the **revision 0.1 baseline review**. See [the weighted-editing and flow-results review](foil-editing-flow-results.md) for the current iteration. Counts below describe the original run; the linked generated proof JSON is refreshed by later iterations.

Date: 2026-09-19. Mode: create, followed by independent adversarial review. The UI author did not clear its own veto. `geometry_evidence` inspected rendered screenshots and source, independently exercised the original focus and historical-result failures, and reread the final two fixes with their executable assertions and persisted browser report. Its final verdict was **PASS for HTML design iteration**. The final closure was source/proof inspection, not another independent browser run.

This verdict concerns a reviewable design, not a production application or a WCAG conformance certificate. Full assistive-technology evaluation, native behavior and scientific correctness remain Flagged. The user requested mockups for iteration; these are not concealed implementation gaps.

## Measurement and scope

[Browser evidence](../proof/workbench-browser-check.json): 144 measurements and 14 passing behavioral oracles, Chrome 153.0.8010.48, 2.584 seconds for the recorded sweep. Zero reported exceptions, external requests, overflow, token-pair contrast failures or controls below 24 px in the measured states. The desktop default views contain 43/37/28/25/30 interactive controls for Shape/Sections/Analyze/Simulate/Results respectively; the narrow Shape fixture has 33. These are the report's initial fixtures, not invariant counts after editing.

The sweep includes five tasks, nine states and three themes, plus narrow layout, unavailable backend, reviewer/reduced-motion and comfortable-density cases. Every measured frame has nonzero width and height. Exact coverage and assertions are in [the portable verifier](../../tools/check-mockup.mjs). This is not an exhaustive Cartesian product of every harness setting and every possible model.

[Craft evidence](../proof/ui-craft-findings.json): the installed deterministic detector scanned both actual HTML files using Impeccable 3.5.0 and returned four Minor findings. No rules were suppressed. `design-lint.py DESIGN.md --strict` resolves all tokens with zero warnings. Neither check proves information architecture, copy truth or accessibility by itself.

[Specification HTML proof](../proof/spec-html-check.json): all 459 source text blocks present, five rendered flows, matching source hash, no page exceptions or external requests, no overflow at 1440/720 px, working navigation filter and empty-search recovery. The independently reviewed specification is the contract; the mockup's fixture labels describe its narrower behavior.

## Structure before surface

The chosen G1 Parametric Modeling Workbench matches serial editing of one bounded foil. G2 scientific inspection is subordinate within Results. One selected station links the canvas, curve, profile and inspector. Five scalar channels avoid separately editable trailing-edge geometry or competing thickness definitions. A recipe is a starting dependency; a reviewed manual edit detaches it while keeping explicit parameters. These are intentional product decisions, grounded in CFD-Bench and the final proposal.

### Findings and disposition

All rows are **Verified observations of the reviewed artifacts**; production implications are not inferred as proof. Severity uses 4 Blocker, 3 Major, 2 Minor, 1 Nit. Accessibility findings at Major are blocking.

| Location | Dimension | Severity when found | Evidence → fix inspected | Final |
|---|---|---|---|---|
| Shape metrics | Consistency, technical authority | 3 | Area used station trapezoids while the foil used a quintic curve → bounded Simpson integration calls the same chord evaluator on both halves; tangent edit changes area/AR | Closed |
| Sections camber | User control, geometry authority | 3 | Accepted camber could diverge from recipe and 3D → first-edit review, accepted profile→loft path, recipe detach and coherent Undo | Closed |
| Results viewport | Provenance, error prevention | 4 | Historical run label accompanied mutable current geometry → immutable run snapshot; current-design edits leave its SVG unchanged | Closed |
| Partial Results | State completeness, copy truth | 3 | Missing values were claimed masked without visible masks → dashed uncolored missing tiles and focused oracle | Closed |
| Channel/curve edits | Accessibility, control | 4 | Rerender discarded focus → stable control identity and focus restoration after exact edit | Closed; full assistive-technology audit still Flagged |
| Narrow shell | Navigation, accessibility | 4 | Collapsed project pane removed access to recipe/history → Project drawer with retained access | Closed |
| Task/selection semantics | Accessibility | 4 | Selection attribute without matching semantic role → valid current-item semantics | Closed in inspected path |
| Shape/Results framing | Craft, recognition | 3 | Clipped foil, ineffective fit and misleading scale → evaluated bounds fit, full model in frame, misleading scale removed | Closed |
| Plot axes | Technical readability | 3 | Open SVG axes filled a black triangle → explicit fill:none and rendered assertion | Closed |
| Simulate error | Recovery, content | 3 | Generic geometry error in run workflow → failed-stage explanation and retained setup | Closed |
| Spec search | Recovery | 2 | Unmatched search appeared blank → visible recovery text | Closed |
| Modal edge/shadow | Craft | 2 | Detector: 1 px border plus 24 px blur → simplify elevation in next visual pass | Open, nonblocking |
| Task strip lower edge | Spacing | 2 | Detector: children flush against bottom border → evaluate extra token-sized inset against compact density | Open, nonblocking |
| Workbench type | Hierarchy | 2 | Detector sees 12/13/16 px, ratio 1.3:1 → trial stronger workspace-heading contrast without expanding numeric controls | Open, nonblocking |
| Token preview type | Hierarchy | 2 | Detector sees 14/18/24 px, ratio 1.7:1 → assess catalog hierarchy in the next visual pass | Open, nonblocking |

### Rubric scorecard

Scores below are qualitative review findings, not fabricated numerical quality metrics.

| Dimension | Result and limit |
|---|---|
| 1 System status | Revision, dirty/detached, stale/partial and backend states visible |
| 2 Real-world match | Hydrofoil quantities, section identities, units and coordinate contract |
| 3 User control | Reviewed detach, cancel and whole-operation Undo; no real file operations |
| 4 Consistency | Shared selection and shape evaluator; historical result isolated |
| 5 Error prevention | Invalid draft retains valid shape; run is deliberate; real validation deferred |
| 6 Recognition | Selected station and relevant inspector remain visible |
| 7 Efficiency | Direct and exact inputs; focused keyboard paths checked |
| 8 Minimalism | One canvas focal point; supporting plots and optional assistant subordinate |
| 9 Recovery | Route-specific errors and retained data; native recovery unimplemented |
| 10 Help | In-context model explanation and fixture boundaries |
| 11 Archetype | G1/G2 fit documented from task, not visual fashion |
| 12 States | Harness and persisted rendered evidence cover named hard states |
| 13 Tokens | Strict design lint clean; detector reported no token findings |
| 14 Accessibility | Named blockers independently closed; full WCAG 2.2 AA still Flagged |
| 15 Performance/stability | No network/exception/zero-frame/overflow failures; per-edit latency and native scale unmeasured |
| 16 Copy | Actionable recovery, units, uncertainty and illustrative-data labels |
| 17 Craft | Four unsuppressed Minor findings; user visual iteration remains |
| 18 AI honesty | Deterministic fixture, reviewed proposal and no network/key submission; real provider integration deferred |

The generic-tells review finds no violet gradient, promotional stat-card grid, repeated raised cards, placeholder people/content, emoji controls or all-happy-state assumption. Pane spacing varies by relationship; the canvas is asymmetric and dominant. Type contrast and modal elevation remain the disclosed Minor findings. Motion is restricted to the written inventory. This is a design judgment, not a detector claim.

## Definition of done: 22-item evidence ledger

| # | Requirement | Evidence / status |
|---|---|---|
| 1 | Words-first direction | Direction brief predates screen authoring; persona assumptions explicitly Inferred |
| 2 | Task-shaped archetype | G1 serial editing / G2 parallel evidence inspection justified in direction |
| 3 | UX before UI | Spec Part B and independent A→B→C gate |
| 4 | Type/color/space personality | Direction's three explicit decisions |
| 5 | System before screens | DESIGN tokens, contrast pairs, states, motion and rendered catalog; strict lint clean |
| 6 | Self-contained hard-state mockup | Single-file HTML, zero external requests, source-controlled deliverable |
| 7 | Exercised harness | 144 persisted measurements; all harness dimensions represented, not every combination |
| 8 | Craft | Canvas focal point and compact hierarchy reviewed; four Minor refinements recorded |
| 9 | Motion and stability | Written inventory, reduced-motion harness case, static loading; full layout-shift/per-edit instrumentation remains unmeasured |
| 10 | Real copy | DESIGN COPY-01–18 and route-specific copy |
| 11 | Generic-tells check | Above; no silent suppression |
| 12 | Deterministic control | Two-file detector corpus and JSON findings; no token/a11y detector findings |
| 13 | Detector not a verdict | Separate independent structural, rendered and behavior review |
| 14 | Generated assets | N/A: native HTML/SVG, no generated bitmap or copied assets |
| 15 | Measurement before diagnosis | Browser counts and detector observations precede final critique; create mode |
| 16 | Structure-first rubric | This artifact and independent findings |
| 17 | WCAG 2.2 AA / independent veto | Named prototype blockers cleared independently; **full WCAG conformance not established** |
| 18 | Ranked plan | Below, one highest-leverage iteration |
| 19 | Trigger union | Direction maps UI-T1/T3/T4 and excludes generated assets UI-T2 |
| 20 | Technical UI | Numeric precision, cividis, provenance, uncertainty, exact/direct edit; scientific validity Flagged |
| 21 | AI UI | Named HAX/Shape-of-AI patterns in DESIGN; wrong/unsupported response fixtures; live AI unimplemented |
| 22 | Native UI | **Native proof unverified; Apple HIG body inspection incomplete**; framework unselected and explicit proof ledger linked |

The unmet portions of items 9, 17 and 22 are evidence obligations for production readiness. They do not turn this HTML iteration artifact into native or scientific proof. They must remain visible in subsequent acceptance reviews.

## Ranked next steps

**Must fix before production:** native keyboard/accessibility/platform proof; real geometry/analysis/file contracts; measured performance on both target OSes. These are product implementation work, not additions to this mockup task.

**Should fix next — highest leverage:** iterate one complete curve→station→profile edit with the user, especially tangent control, profile identity and the recipe-detach explanation. This tests the core ownership model before multiplying commands. Resolve the four Minor craft findings in that same visual pass.

**Worth doing:** compare compact and comfortable density with real long foil names and a 21-station design; refine evidence-reading affordances after the model-edit workflow settles.

Simplifier delete-list: no further production scope or decorative panel is added. The misleading viewport scale was removed during blocker closure. Current review deletion proposal: **net: -0**; avoid further code churn after the focused oracles pass.

| | |
|---|---|
| Completed | Full specification, design language, five interactive workspaces, first run, hard-state harness and independent prototype gate |
| Remaining | User iteration, four Minor craft refinements; full WCAG evidence, measured edit latency and native proof unverified |
| Best next action | Review the curve–station–profile handoff in the HTML mockup |
