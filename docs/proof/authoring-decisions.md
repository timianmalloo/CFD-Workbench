---
id: proof-authoring-decisions
title: V7 authoring decisions proof and review boundary
type: proof-pack
status: in-review
owner: "@timianmalloo"
tags: [proof, foildsl, ux, authoring]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: spec-foildsl, rel: documents}
  - {to: mockup-workbench-v7, rel: documents}
  - {to: plan-authoring-decisions, rel: relates-to}
  - {to: review-authoring-v7-independent, rel: depends-on}
  - {to: review-authoring-v7-gaps, rel: relates-to}
review-by: 2026-12-22
summary: Observed source, profile-bank, dimension, comparison, history and rendered browser proof for v7, with explicit prototype and production limits.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Result and scope

The five authorized authoring additions are incorporated in product revision 1.5 and the v7 interactive
mockup. The language remains the proposed, unapproved FoilDSL 4.0 contract; no new grammar productions are
needed for these transactions. Profile banks and assignments serialize existing productions. Alternatives,
baselines and decision facts remain project metadata. The native project and shape-only file distinction,
full-thickness equation, normalized Rule A blend and independent-rail/LE-pivot distinction are explicit.

**Verified:** the executable browser assertions below and the inspected rendered artifacts. **Inferred:**
the new entry and scope workflow improves unaided discoverability; UX-26's formative study has not run.
**Flagged:** exact geometry, native persistence/accessibility and scientific evidence are separate product
obligations. Passing the prototype suite is not full product acceptance.

## Executed evidence

| Check | Observed result | Evidence |
|---|---|---|
| Source authoring | 13 groups; 15 layout/theme cells; no browser errors or external requests | [FoilDSL v7 report](foildsl-v7-browser-check.json) |
| Independent LE/TE | 6 groups including unequal bases and real pointer, numeric, keyboard, source/history edits; no errors | [Rail report](independent-edges-v7.json) |
| Inherited full CAD/workbench | 16 groups; 77 accessibility/layout measurements; 30 shell cells; no errors or external requests | [CAD report](workbench-v7-browser-check.json) |
| New authoring transactions | 25 checks; 33 entry/accepted-section/active-draft cells; zero errors/network; shared/fork reach, held/source thickness, invalid recovery, owned draft, source/bank history, held rails/span mapping, baseline/Keep/Discard/retained evidence and hostile text | [Authoring report](authoring-v7-browser-check.json) |
| Design tokens | `design-lint.py DESIGN.md --strict`: clean, zero warnings | Executed during final join |
| Craft detector | Zero Blocker/Major findings; 14 Minor advisories retained for the compact desktop grammar and fixed copy | [Craft report](ui-craft-findings-v7.json), independently interpreted in review |
| Product/language HTML | Source hash/block/criterion/flow parity; desktop/narrow rendered checks; no external requests | [Product report](spec-html-check-cfd-workbench-v1.json), [language report](spec-html-check-foildsl.json) |
| Repository docs | `python3 tools/check-docs.py`: passed; 79 artifacts, zero defects, 56 intentional review suggestions | Final graph/render join passed; closing audit is followed by the same repository check |

Run browser tools with Node and an available Playwright module directory. `check-foildsl.mjs` and
`check-independent-edges.mjs` accept `MOCKUP_NAME=workbench-v7`; the two v7 tools target v7 directly.
`python3 tools/build-mockup-v7.py` regenerates the self-contained HTML. The CAD oracle's historical v6
file stays unchanged; `derive-v7-oracle.py` evolves only the explicitly changed inspection/section and
control-budget assertions, and repairs an invalid direct-mutation fixture. No unrelated checks were removed.

## Integrity trace

Accepted source → named profile curves with their own knots → station assignments → equal-normalized-x
camber/thickness blend → effective span t/c → placed sections/3D body → source round-trip, revision and
history. The new test changes profile selection without changing any geometry samples. Shared/fork changes
reach the appropriate blend intervals. Keep current t/c holds the complete channel record. Source thickness
proposes constrained targets on that same channel, shows residual/support bounds, and refuses locked cases.

A visual/source/dimension draft owns its target and base. Inspection cannot retarget it. Apply/Cancel and
undo/redo cover profiles, assignments, channels and accepted source. Comparison uses accepted full snapshots;
Discard archives source/history/run pins before restoring the immutable baseline. Scientific rows explicitly
say no comparable evaluated run. Untrusted names and rationale are inserted as text, not executable markup.

Section source curves are inverted at each requested chord x. The prior coarse resampling artifact near a
stress profile's leading edge was corrected during review. Maximum-thickness normalization still uses 201
samples; it is not a certified extremum. Dimension target residual tolerance is 1e-8 in the channel's unit;
support disclosure is a conservative full-span bound. The sampled area comparison uses 200 midpoint spans.

## Review, repairs and remaining work

The [independent review](../reviews/authoring-v7-independent.md) holds the geometry/data and UX/accessibility
veto. It records source inspection versus observed interaction/visual evidence. The author does not clear
its own veto. Review corrections reached profile-name/policy truth, real blend readers, draft focus,
native button keyboard behavior, section reflow target sizes, actual residuals and retained evidence.
Failure classes and executable controls are recorded in the always-loaded defect register.

The [further gap review](../reviews/authoring-v7-gaps.md) ranks three remaining decisions: external file
changes, bounded representation of an exact promoted slice, and comparison correspondence across spans.
They are captured for user review, not automatically implemented. Existing limitations remain explicit:
page-session alternative storage, sampled geometry, unsupported multi-profile promotion/removal,
standalone-section/assets/stable-ID/full constraint conformance, native saves and native accessibility.

Review the [runnable v7](../mockups/workbench-v7.html), [specification](../specs/cfd-workbench-v1.html),
[normative language](../specs/foildsl.html), and the [short walkthrough](../mockups/workbench-v7.md).
No production stack/backend, deployment, push or merge is part of this handoff.
