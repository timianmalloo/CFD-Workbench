---
id: mockup-workbench-v7
title: CFD-Workbench v7 — section scope and design alternatives
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, foildsl, cad, profiles, alternatives]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: decision-design-iteration, rel: depends-on}
  - {to: design-authoring-decisions, rel: refines}
  - {to: design-language, rel: depends-on}
  - {to: mockup-workbench-v6, rel: supersedes}
  - {to: proof-authoring-decisions, rel: relates-to}
  - {to: review-authoring-v7-independent, rel: relates-to}
  - {to: review-authoring-v7-gaps, rel: relates-to}
review-by: 2026-12-22
summary: >-
  The spatial workbench adds persistent section editing, shared and independent profiles, draft-safe inspection,
  explicit chord/span intent, and page-session alternatives with baseline comparison and decision rationale.
  Geometry remains sampled; scientific, native persistence and full language conformance are not proven.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Workbench v7: inspect impact before deciding

[Open interactive mockup](workbench-v7.html) · [Product specification 1.5](../specs/cfd-workbench-v1.html)
· [Normative language](../specs/foildsl.html) · [Design direction](../design/authoring-decisions.md)
· [Decision contract](../notes/design-iteration.md) · [Previous v6](workbench-v6.md)
· [Proof](../proof/authoring-decisions.md) · [Independent review](../reviews/authoring-v7-independent.md)
· [Remaining decisions](../reviews/authoring-v7-gaps.md)

Open the HTML directly in a browser. It is self-contained and needs no server, network, key or solver.
The current source emitter/parser pins evaluator `cfdw-cv/2`. An unavailable `/1` draft receives a version
diagnostic and retains the accepted source. This compatibility update does not turn sampled prototype
geometry or its illustrative identity into production certification. Historical v6/native-spike outputs
remain `/1` evidence, not current contract implementations.
The seven-area desktop workbench, four spatial viewports, Properties and review harness remain the context.
FoilDSL and visual editing read the same accepted definition. The persistent section card exposes the
selected station's shape and the action that edits it.

## Review walkthrough

1. Choose **Alternatives…**, name a trial, then **Pin baseline + create**. Close the dialog. The baseline
   is an accepted snapshot; the named trial is the editable alternative for this page session.
2. Select an interior station and choose **Edit section** on its persistent thumbnail card. Compare the
   scope choices **Edit shared profile** and **Make independent**. Read the assignment and interval summary.
3. Leave **Keep current t/c** selected, choose **Make independent**, then **Start section draft**. Move
   an upper/lower control or use its keyboard edit. Inspect another station and orbit the 3D view: the draft
   banner keeps the original target. A competing edit is refused. Return to that target and **Apply section**.
4. Open **FoilDSL** and inspect the new profile and assignment. Undo/Redo restores source, profile bank and
   geometry together. Repeat with **Edit shared profile** to inspect its broader reach. **Use source thickness**
   instead proposes an explicit constrained change to the existing t/c curve, with sampled preview/diagnostics.
5. Open **Edit intent…**. Preview a chord with **Hold LE**, cancel, then preview with **Hold TE**. Try both
   station-position policies when changing span. An interior station beyond the shortened tip blocks Apply.
6. Reopen **Alternatives…** and choose **Compare accepted**. Read the fixed-alignment planform and selected
   section overlays, sampled area/AR, and unavailable comparable scientific evidence. Enter a rationale,
   then **Keep alternative** or **Discard and return to baseline**. Discard retains the session archive and
   rationale, and restores the baseline source/history/run pins. **Read retained alternatives and baseline**
   opens read-only accepted source with retained history count and run pin. Keep leaves the named alternative active.
7. Use the inherited Source error fixtures, Preview/Validate/Apply/Cancel, **Save .foil** and **Open .foil**.
   A shape file does not contain alternatives, decisions or project history. Change theme, viewport,
   persona and reduced motion through the review harness.

## Demonstrated subset and honest limits

The v7 source model extends v6 to at most 16 inline profiles and explicit station assignments. It preserves
separate section-side knots and independent LE/TE curves. Master channels use the prototype's degree-3,
uniform-knot subset. Unsupported assets, stable CV IDs, fine-grained locks/assertions, standalone-section
import and other full-language constructs fail closed. Multi-profile blending uses sampled equal-normalized-x
camber and thickness shapes; it is not a certified implementation of the continuous language contract.

Shared editing and making a station independent change real source profile records and assignment references.
Keep current t/c preserves the channel. Use source thickness computes a constrained minimum-norm channel
adjustment; the preview is subject to prototype sampled validation and can fail. Derived quantities, profile
crossing tests and surface rendering remain sampled. Exact blended-slice promotion/removal in a multi-profile
document is refused because a lossless conversion is not proven; the existing single-profile path remains.

Alternatives, snapshots and rationale survive within this page session only. Discard retains archived state;
the read-only archive viewer exposes its source, history count and illustrative run pin without changing
the active definition. Keep retains the named active alternative. This is not a native archive writer. The
prototype offers one active trial with its pinned baseline; arbitrary project branching and repinning remain product obligations.
Refresh loses this session state. Exported `.foil` retains accepted geometry, not those project records.
Production semantic hashing, immutable persistence, recovery and cross-platform atomic saves remain unverified.
Visual source emission preserves comments while normalizing layout; it is not production lossless patching.

Geometry comparisons use accepted definitions and a labelled sampling basis. Cruise drag and takeoff lift
have no comparable evaluated run in this mockup. Scientific values elsewhere remain illustrative fixtures.
No interaction here proves hydrodynamic improvement, manufacturing safety or CFD conformance. Native
VoiceOver/Narrator, the five-person formative task and full evaluator conformance remain separate gates.

## Build and evidence

`python3 tools/build-mockup-v7.py` regenerates the standalone artifact from preserved v6 plus
`tools/mockup-v7.html`, `tools/mockup-v7.css` and `tools/mockup-v7-authoring.js`. This is prototype assembly,
not selection of the application stack. Repository checks remain `python3 tools/check-docs.py`.

`tools/check-authoring-v7.mjs` is the browser proof for section scope, one owned draft, dimension intent,
source agreement, baseline comparison and decision handling. Its run writes
`docs/proof/authoring-v7-browser-check.json`. The final [proof](../proof/authoring-decisions.md) records
25 authoring checks and 33 layout/theme cells, alongside source/rail/CAD regressions. The
[independent review](../reviews/authoring-v7-independent.md) records the scoped geometry/data/UX/test veto
passes, repairs and retained risks. Product and language HTML parity use `tools/check-spec-html.mjs`.
The [further gap review](../reviews/authoring-v7-gaps.md) leaves three policy decisions for user review:
external file changes, exact blended-slice representability, and comparison correspondence across spans.
Those findings do not extend implementation scope. This is bounded prototype acceptance, not full product acceptance.
