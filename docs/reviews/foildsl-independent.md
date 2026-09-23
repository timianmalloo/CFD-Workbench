---
id: review-foildsl-independent
title: FoilDSL authoring — independent review
type: doc
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [foildsl, review, geometry, data, security, accessibility]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: mockup-workbench-v5, rel: relates-to}
  - {to: mockup-workbench-v6, rel: relates-to}
  - {to: spec-foildsl, rel: documents}
  - {to: plan-foildsl-authoring, rel: relates-to}
review-by: 2026-12-22
summary: Independent review of the FoilDSL contract and authoring experience. Separates observed baseline evidence, pre-build contract findings and final rendered-surface gates from unverified production obligations.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# FoilDSL authoring — independent review

Reviewer: a separate agent from both the language author and UI/product-spec author.
Tier: T2. Date: 2026-09-22. Findings are scoped to this specification and HTML review artifact;
native application, production kernel and scientific validity remain separate obligations.

## Authority and baseline

**Verified.** `spec-cfd-workbench-v1` is the build basis. Its body is revision 1.3, while its YAML
title and summary lag behind. `mockup-workbench-v5` implements that spec and supersedes v4.
The traversal through `control-vertex-workspace` and accepted `adr-0001-master-curve-degree`
establishes the authored CV record, degree-3 distribution curves and degree-5 section curves.
Git commits `f7ee6cf`, `a094103` and `034f0b8` establish the specification, degree decision
and latest pointer correction respectively. The reference material is a proposal to reconcile,
not an automatic supersession of those contracts.

**Verified rendered baseline.** [Baseline measurements](../proof/foildsl-baseline.json) capture
the v5 CAD surface at 1280 × 800. Required contrast failures, undersized targets and dense-target
failures are zero. The window, toolbar, document tabs, status and options rows do not overflow.
No FoilDSL/source entry exists. The screenshot was independently viewed: four viewports remain
the main working surface, the Properties dock holds precision and lock controls, and the palette
uses names alongside icons. Ellipsized viewport captions are existing behavior.

The baseline craft command `python3 docs/ai-forward-pack/scripts/ui-craft-gate.py
docs/mockups/workbench-v5.html --gate --a11y-obligation --json` reported Minor findings for
copy density, selected-tool stripe, compact viewport spacing and clipping heuristics; no
Blocker was reported. This detector result is a floor, not an accessibility verdict.

## Initial execution-plan gate

**PERSONA:** Test Architect and Simplifier, Adversary mode.
**Verdict:** PASS-WITH-CONDITIONS on the plan, not the finished artifacts.

The reference-grounding → canonical-contract → product/UX → mockup → proof → independent-gate
→ documentation/audit sequence preserves the necessary dependencies. Independent source inspection
is useful parallel work; a competing UI implementation is not. The following conditions were
sent before the UI build and accepted by the coordinator:

1. Settle canonical ownership and language invariants before the UX surface.
2. Cover parser/serializer round-trip and canonicalization idempotency laws, malformed and hostile
   input, unsupported versions and unknown fields. Explicitly bound syntax-only conformance claims.
3. Prove source → geometry → source consistency through actual writers, including section,
   station, example and catalog mutations, not just the master-curve Apply function.
4. Maintain one draft across text, visual and section authoring. Preserve the last valid geometry
   while a source draft is invalid; no edit may silently replace a competing draft.
5. Observe a missing source-control test fail before adding the feature. Add failure-causing
   mutations/negative fixtures to demonstrate new guards can reject violations.
6. Retain existing shell, camera, CV, section, freshness and precision-interaction coverage on
   the evolved artifact. New flow proof uses actual user clicks and keyboard actions.
7. Treat source and imported file text as untrusted: no evaluation, path fetching or HTML execution;
   add resource bounds and a STRIDE disposition for the new boundary.
8. Keep native accessibility, production serialization/migration and kernel tolerances explicitly
   unverified. A mockup gate does not establish them.

**Simplifier:** Lean plan; retain the existing dependency-free HTML workbench and its oracle.
No new runtime dependency is justified for the source-authoring mockup.

## Constitutional interpretation applied

BoK D1/D2/D3 require observed boundaries and independent review. Rules of the Road T2 requires
the security lens and a Proof Pack. LOA P1/P2 place parsing, identity and geometry on the
deterministic path; P3/P5/P9 make any future AI output a proposal through the same validation
boundary. P7 places accepted source/history outside the model. P8 requires repeated Apply not
to duplicate accepted edits and a stale-draft check. P10 requires attributable transitions.
P11 introduces no new principal surface in this local prototype. Existing AI-aside-CAD fits
archetype F without adding a model call to this task.

DM1–DM11 distinguish an accepted source fact from its rebuildable syntax/geometry projections,
an editing draft, an accepted edit event and a run's immutable input pin. Semantic identity,
source revision and history identity must be named separately: a comment edit need not stale
geometry; undo must not erase accepted history or reuse the identity of a different event.

## Contract and final artifact gates

The first actual review read `docs/specs/foildsl.md` and `docs/adr/0002-foildsl-authority.md`.
**Verdict: PASS-WITH-CONDITIONS for the conceptual source ownership and UX build; final language
and rendered-surface gates remain pending.** One source authority, explicit knot payloads,
normalized profile blending, LE pivot, fixed section planes, rejected nominal external profiles,
append-only history and no executable/imported-path semantics are specified coherently.

| ID | Lens / severity / confidence | Evidence and required resolution |
|---|---|---|
| R1 | Geometry / Major / Verified | §8 refers to a pinned degree-to-radian constant without specifying it. Pin the exact conversion or avoid conversion in identity. |
| R2 | Data / Major / Inferred | Canonical text export converts source units to mm and promises unchanged identity. Shortest-round-trip binary64 formatting alone does not establish unit-conversion round-trip invariance. Define normalization precisely and test equivalent cm/m/mm inputs plus formatting idempotency. |
| R3 | Data / Minor / Verified | `freeze` captures accepted coordinates but a standalone source has no prior accepted base. Define its first-acceptance and later visual-edit meaning. |
| R4 | Security / Major / Verified | The ADR promises bounded input, tokens, nesting and validation time, but the first language draft supplies no numerical acceptance limits. State and exercise an explicit bounded profile. |
| R5 | Geometry / Blocker on reuse / Verified | Executed v5 `secSeed()` produces 12 CVs per side while `CVSEC.U` remains 15 values from the initial nine-CV seed. Rendering uses that stale vector; the residual instead uses the discarded fit's `f.U`. A serialized section must carry the same complete knot vector its reader and residual use. |
| R6 | Geometry / Major on conversion / Verified | Executed v5 seed has near-zero rather than exact endpoint coordinates, root-lock ordinate differences about 10⁻⁷ m, and a second profile abscissa near −1.2×10⁻⁶. Those do not satisfy the proposed exact endpoints/nondecreasing abscissa contract. Any normalization must be explicit and have a measured deviation; do not relabel the old numbers conformant. |
| R7 | Data / Major / Verified | The canonical identity object initially names its contents in prose without fixing JSON keys/layout, and lacks the distinct standalone-section shape. Define literal canonical objects for both document kinds so independent implementations hash the same bytes. |

**Targeted second read:** R1–R4 are resolved at specification level. The updated language pins
`0.017453292519943295` radians/degree, retains exact decimal rationals through SI scaling,
emits canonical dimensional source in SI, puts freeze coordinates explicitly in syntax, and
sets 1 MiB/count/string limits with a one-second fail-closed interactive validation budget.
Production tests for those requirements remain release obligations. R5/R6 concern the evolving
prototype and await its implementation/proof. R7 is resolved by exact kind-tagged canonical
Foil and Section objects, literal keys, explicit default closure/tip values and profile ordering.
The **geometry/data/security conceptual contract gate is PASS for specification review**; no
production conformance or mockup-integrity verdict follows from that pass. Source identity's
algorithm was requested as a final minor clarification.

R5/R6 are findings at the newly touched serialization boundary, not a mandate to repair archived v5.
The evolved artifact must not inherit them. The geometry review does not claim a production kernel
conformance gate has passed.

## First evolved-surface review

**UX & Accessibility / UX Researcher / Marine CAD UX / Test Architect: BLOCK pending fixes.**
The separate reviewer exercised actual clicks and keyboard entry on v6 across five window presets
and three themes, inspected minimum/desktop/reflow screenshots, and read the source transaction seam.
Syntax failure retained an explicitly labelled Accepted geometry and disabled Apply; source typing
did not invoke CAD character tools; no page errors were observed. The following failures were sent
to the author while the build was still in progress:

| ID | Dimension / severity / confidence | Observed failure and smallest correction |
|---|---|---|
| R8 | Accessibility / Major / Verified | The failure-state select is 20 px high and normative-spec link 16 px; audit reports two small targets in all 15 width/theme cells. Apply the dense-control and link target floors. |
| R9 | Reflow / Major / Verified | At 640 × 400 the source editor has only about 20–30 visible pixels after chrome/actions/status. Collapse redundant Source chrome or actions so a usable editing region remains. |
| R10 | Provenance / Major / Verified | Opening Source alone changes the existing measured section residuals to null. `dsvg` calls `dload` twice, which clears residual metadata. Rendering must preserve the entire accepted record and its evidence. |
| R11 | Numerical legibility / Major / Verified | Source preview renders at a fixed 640 px then shrinks into an approximately 335 px box, reproducing UI-L's sub-12 px SVG text. Render at actual size, or remove inert navigation/caption text and expose a readable external caption. |
| R12 | Transaction integrity / Blocker / Verified | Actual GUI path: click chord CV 3 → type `-1 m` in Properties → Tab → Return on workspace. Revision becomes 5 with accepted chord −1; reparsing accepted source throws DSL-GEOMETRY. Every visual Apply must use the same validation gate before acceptance. |
| R13 | Source ownership / Major / Verified | Source pane retains active lock controls whose handlers only guard geometry drafts, while text may be dirty. Tip-value lock is also omitted from emitted source and imported from ambient state. Gate these controls and serialize authored locks, or explicitly classify nonauthored prototype state without a round-trip claim. |

This first review is a work-in-progress snapshot, not a verdict on later edits.

## Targeted verification after fixes

[Independent browser measurements](../proof/foildsl-independent-browser.json) record a rerun after
the fixes. **Verified:** all fifteen Source window/theme cells have zero target and contrast failures
and no application-window scroll. The 640 × 400 screenshot was independently inspected: eight source
lines are readable, with file/history actions moved into a named menu. Preview microtext is removed,
leaving readable context outside the inert image. Opening Source preserves the measured section
residuals, CVs, knots and geometry revision. The invalid GUI chord counterexample now leaves revision
4 and its prior chord unchanged, and the accepted source reparses without an error. Source typing
does not trigger CAD tools; malformed text disables Apply and labels the accepted geometry. No page
errors were recorded. **R8–R12 are resolved for these observed paths.**

**R5/R6 verified:** both section sides carry twelve CVs and eighteen knots, as required for degree
five. Their reported residuals exactly equal recomputation using the current CVs, per-side knots and
source parameters. Sampling 401 parameter positions measured the initial normalization change against
the pre-normalized fit as 8.208684513285464×10⁻⁷ chord upper and 8.900357212552994×10⁻⁷ chord lower.
These are sampled deviations, not certified maxima. The v6 hub names the initial conversion and
normalization; no lossless migration claim is made. **R13:** dirty-source guards now cover the lock
controls; the tip pin is explicitly excluded from `.foil` persistence as session-only prototype state.

## Cross-specification check and final verdict

The reviewer compared language §2/5/8/9 with product A4.13, B9 and C4a and the v6 hub. Their ownership,
native-project/source separation, shared transactions, profile reach, validation and run freshness
contracts agree. Prototype limitations are distinguished from normative product obligations.

**R14, Major, Verified, resolved:** product A4.13 and language §9 initially used DSL-01 onward for
different acceptance criteria. The product now uses **SRC-01–10**, while language criteria retain
DSL-01–12 and diagnostic codes remain unchanged. The reviewer read the corrected table and B9 heading.
The tip pin's session-only, unsaved status is now visible in Properties, not only in the hub.

**Final verdict: PASS for the specification and bounded HTML review artifact.** No unresolved
Blocker or Major remains in this review's scope. The reviewer read the regenerated proof contents,
not only command exit codes:

- [Source oracle](../proof/foildsl-browser-check.json): thirteen passing behavior checks, including
  64 generated serialization variants, invalid inputs, real download/reopen, source/geometry history,
  shared section-to-3D changes and the rejected negative-chord GUI counterexample; fifteen layout/theme
  cells; zero page errors and network requests.
- [Inherited CAD oracle](../proof/workbench-v6-browser-check.json): sixteen passing oracles,
  77 measurements and thirty shell cells; zero failed oracles, page errors or network requests.
  The intermediate residual failure was resolved by preserving section residual metadata in history
  snapshots/Undo; the inherited assertion was retained.
- [Craft measurement](../proof/ui-craft-findings-v6.json): fourteen Minor findings, no Major or
  Blocker. These remain advisory compact-CAD/copy/clipping heuristics and do not negate the observed
  interaction/layout checks. They do not establish an independent accessibility certification.

| Independent lens | Verdict / veto | Evidence and residual boundary |
|---|---|---|
| Test Architect | PASS; prototype veto cleared | Missing-source red observed, meaningful negative inputs and the actual GUI failure observed before repair, cross-surface assertions and populated proof. Full language/native/scientific conformance remains unverified. |
| Computational Geometry | PASS at specification/prototype scope | One authored authority, complete explicit knot payloads, measured normalized seed change and same-record residual. Interval certification, full evaluator identity and kernel export are release gates. |
| Data & Persistence | PASS at specification/prototype scope | Separate source/definition/run identity, immutable-history contract, real source file round-trip and synchronized undo. Native archive schema, crash recovery and production migration are not implemented. |
| Security & Identity | PASS at bounded local-artifact scope | Bounded inert source, rejection before acceptance, no fetching/execution and no observed network requests. Production import, resource exhaustion and native archive hardening remain implementation obligations. |
| UX Researcher / IA | PASS; mockup UX veto cleared | Source is reachable in one tab action, one draft has explicit recovery, accepted/preview and shared-profile reach are named. A usability study is not claimed. |
| UX & Accessibility / Marine CAD UX | PASS; mockup review veto cleared | Independent rendered inspection, keyboard/source isolation, fifteen theme/width cells, usable reflow, preserved CAD oracle and visible prototype limits. Native accessibility, assistive-technology certification and production trackpad behavior remain separate proof. |
| Simplifier | PASS | Existing dependency-free workbench retained; no new product runtime stack or model call. The restricted parser is explicitly a prototype subset. Lean within that scope. |

The product remains **in review** for the user. This independent gate authorizes no implementation,
merge, deployment or scientific/fabrication use. No production/native/scientific gate is cleared by
the browser evidence.

### Final narrow addendum

The final seed-writer sweep moved the already documented master endpoint/root normalization into
`loadExample`, so every example generator produces the same valid record shape as initial loading.
The reviewer read the generator and new `RecipeSeeds_EveryGenerator_EmitsValidSource` oracle:
the regenerated proof records both **light** and **strong** passing emit/parse semantic equality.
The source suite now has thirteen passing checks, with zero page errors or network requests.

The reviewer also inspected and independently executed the final two rejection guards: an empty
foil name reports `DSL-REFERENCE`, and a degree-five section with six repeated interior knots
reports `DSL-KNOTS`. These are narrow boundary checks, not a new conformance claim. The scoped
final PASS remains unchanged.
