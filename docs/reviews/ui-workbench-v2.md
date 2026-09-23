---
id: review-ui-workbench-v2
title: UI review — workbench mockup v2 (seven areas)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, ux, accessibility, mockup, v2]
links:
  - { to: spec-cfd-workbench-v1, rel: documents }
  - { to: mockup-workbench-v2, rel: relates-to }
  - { to: design-language, rel: relates-to }
  - { to: workbench-direction, rel: relates-to }
  - { to: review-ui-workbench-v1, rel: supersedes }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-21
summary: >-
  Elevate-mode review of the seven-area mockup against specification v1.1. The independent UX & Accessibility
  lens returned BLOCK on the first pass (layer names presentational under role=img, a bare character-key
  shortcut, a false inequality on the candidate card, and a Major list across state completeness, copy truth
  and the marine CAD idiom) and PASS-WITH-CONDITIONS with the veto cleared after the fixes were applied and re-measured. Highest-leverage
  change: the outer SVGs of the plan view and the Results viewport became role=group, which exposed every
  authored layer name to assistive technology with one attribute in two places.
review-suggested:
  - { by: mockup-workbench-v2, on: 2026-09-20, reason: "Mockup v2 (seven areas) cleared by the UX & Accessibility lens 2026-09-21; supersedes v1 as the review artifact." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# UI review — workbench mockup v2 (seven areas)

*Produced by `/ui-design` (mode: **elevate**). Governed by `ui-design-craft.md` DX22–DX25 over the floors in `ui-interaction-design.md` (U1–U20). Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surface(s) reviewed:** `docs/mockups/workbench-v2.html` — the area strip; 1 Setup (language box, parameter form with soft targets, seeded preview, goal state, feasibility); 2 CAD (Outline · Twist · Dihedral · Thickness curves, stations add/remove, section dialog, catalog, lines-plan); 3 Analysis (layer set on the shared canvas, the CAD ⇄ Analysis toggle, 2D/3D/compare, charts); 4 Experiment (sweep grid and case preview, optimize form); 5 Run (environment, allow-listed steps, queue, console); 6 Results (layer list, viewport, timeline, small multiples, difference flood, candidates); 7 Export; the prompt entry in every area; states default · first launch · loading · empty · error · partial · overflow · success; themes light · dark · high-contrast; viewports 1024 · 1280 · 1440 · 1600 · 640.
**Reviewed against:** spec v1.1 `docs/specs/cfd-workbench-v1.md` (A2 areas, A5.9–A5.12, B1 IA and verb table, F6–F8, UX-17–22, C1 per-area archetypes, C2 incl. the run/experiment/area-chip rows, UI-18–22) · `DESIGN.md` (§4 new component rows, COPY-73…97, §12.0a).
**Reviewers:** UX & Accessibility (lead, a11y hard veto; one independent run plus a clearance read) · Marine CAD UX expert (second lens of the same run) · the author (measurements and fixes only; cleared nothing).
**Date:** 2026-09-21 · **Mode:** elevate

## 1. Verdict

> **PASS-WITH-CONDITIONS (accessibility veto cleared)** — the UX & Accessibility lens cleared the hard veto on its second read from the recorded oracle evidence; the three Verified residuals it left (an exponential-format inconsistency, dead code, the batlow low end on graphite) are closed in the artifact, and the two Flagged marine-CAD residuals (the Outline TE rail plots chord; stations are markers) stay recorded.
> **Highest-leverage change:** `role="group"` on the plan SVG and the Results viewport SVG (leaf groups keep `role="img"`), so the lift vectors, the resultant, the depth band and the separation layer the author had already named are read by assistive technology (DX25). One attribute in two places cleared an accessibility Blocker.

| | First pass | Clearance read |
|---|---|---|
| Blockers (sev 4, or any a11y ≥ 3) | 4 (+ a11y Majors counted as Blockers under the obligation) | 0 |
| Majors (sev 3) | 17 (incl. 2 marine-CAD) | 0 |
| Minors (sev 2) | 22 | 3 (closed after the read) + 2 recorded |
| Nits (sev 1) | 4 | 1 (closed) |

**Accessibility veto:** **PASS** — clears when: *every new control is keyboard-operable with visible focus; every authored layer name is exposed (no interactive or named descendant under a `role="img"` parent); no single-character shortcut is active outside a focused component (SC 2.1.4); options live in listboxes; every text/surface, focus/surface and data-strip pair is ≥ 3:1 (text ≥ 4.5:1) in three themes; reduced motion stops the dash animation and removes autoplay, seeded from the OS preference; error slots are described; the C2 strings render verbatim in their components; no rendered value is false to its fixture.* Cleared by **the UX & Accessibility lens (independent agent run), not the author**.

## 2. Measurements (DX23 — measure before you diagnose)

| Metric | Value | Note |
|---|---|---|
| Interactive controls per area (1280 px) | Setup 43 · CAD 78 · Analysis 46 · Experiment 19 · Run 45 · Results 45 · Export 21 | browser oracle |
| Simultaneous regions on the primary screen (CAD) | area strip · project · canvas · inspector + prompt entry · editor · status strip | one focal point: the canvas |
| Network calls on first load / page errors | 0 / 0 | asserted |
| Distinct type sizes | 7 tokens; SVG text 13–15 units measured after viewBox scale; HTML captions at the 12 px token | oracle: smallest rendered text ≥ 12 CSS px at five viewports × seven areas |
| Distinct colours | token references only in the design region; harness uses the dark-theme tokens | detector: 0 off-token colours |
| Modes/views doing the same job | 0 | plan / front / iso / lines-plan are views; the toggle is navigation |
| Arbitrary values (non-token) | 0 flagged by `ui-craft-gate.py`; inline widths moved to `--w-num-*` tokens after the gate | one standing finding: `em-dash-overuse` (the spec's fixed strings; recorded deviation) |
| `design-lint.py --strict` | pass | COPY-73…97 added |
| Worst required contrast pairing | 3.43:1 (`--control` on `--surface`, UI) | text pairs worst 5.63:1; focus ring on the viewport 9.35:1; batlow strips now carry a 1 px ink stroke and a contrast-theme ramp |
| Artifact size | 239 KB (under the 300 KB direction budget) | single file |
| Browser oracle | 13 oracle groups · 84 measurements green | `docs/proof/workbench-v2-browser-check.json` |

## 3. Findings

*Structure before surface (DX24). Severity 0–4 → Blocker(4) / Major(3) / Minor(2) / Nit(1); an accessibility finding at ≥ 3 is a Blocker under the obligation. Disposition after the author's fix and the oracle's re-measurement.*

| # | Location | Dimension | Sev | Evidence (first pass) | Fix applied | Disposition |
|---|---|---|---|---|---|---|
| 1 | plan SVG, Results viewport SVG | Accessibility (1.1.1, 4.1.2) | 4 | outer `role="img"` made every named layer group presentational | outer SVGs `role="group"`; oracle reads the aria snapshot for the lift vectors, the depth band and the separation layer | fixed |
| 2 | global keydown | Accessibility (2.1.4) | 4 | `⇧A` toggled areas with no off switch | `⌘⇧A` / `Ctrl+Shift+A`; oracle presses `Shift+A` alone (no change) and the chord (toggles) | fixed |
| 3 | candidate card | Copy truth | 4 | "A_cav 0.041 ≤ 0.02" rendered as fact; bound differed from A5.9 | comparison from data with a satisfied/violated chip; A5.9 bound 5×10⁻⁴, k = 10, "Inferred" label; Pareto axis rescaled | fixed |
| 4 | `renderInspector` | IA (UX-17) | 3 | `'analyze'` typo left the CAD lock editor in Analysis | `'analysis'`; oracle asserts the run-manifest inspector and no lock controls | fixed |
| 5 | layer state | IA | 3 | Analysis and Results shared one layer object | `M.ana.layers` vs `M.res.layers`; oracle toggles one, asserts the other | fixed |
| 6 | Analysis with a preview open | State completeness (ANA-22) | 3 | no "Preview hidden" banner | banner rendered and asserted | fixed |
| 7 | Run console | State completeness (C2 run states) | 3 | strings deviated; three states never rendered | `STATE_STRING` map keyed to the C2 row; oracle asserts Readiness check, Meshing, Mesh gate passed, Solving, Cancelled, Failed, Unsupported | fixed |
| 8 | optimize form | State completeness / copy truth (XS-02) | 3 | thickness could be unfrozen silently | freeze checkbox disabled with the fixed string as its description | fixed |
| 9 | difference flood | Copy truth (RES-03/04) | 3 | decorative sine labelled ΔCp; wrong twin; no mesh check | ΔCp computed from the two samples; "Unavailable — mesh differs" on differing meshes; own table twin | fixed |
| 10 | main flood | Copy truth (A5.11) | 3 | range rescaled per sample during replay | fixed across the series with an unlock control; caption says which | fixed |
| 11 | Unsupported case | Copy truth (A5.10) | 3 | "SU2: no VOF" on an OpenFOAM backend | reason from the shown capability record (transient requested; steady only) | fixed |
| 12 | station add/remove | Copy truth (IO rule) | 3 | literal deviations "0" and "0.004 mm" | "Not recorded (fixture…)"; the artifact states stations are markers | fixed (residual: stations do not enter the loft here) |
| 13 | charts and floods | UI-17 | 3 | flood, multiples and difference lacked twins | `TWIN.flood/multiples/diff`; oracle opens all three | fixed |
| 14 | section dialog | Marine CAD idiom | 3 | Esc closed with the draft pending; Return did nothing | `cancel` discards the draft; Return applies; oracle asserts | fixed |
| 15 | station list | Marine CAD idiom (UX-20) | 3 | added stations were static rows | every station a `data-eta` option; Remove and Edit for any authored non-root/tip; oracle adds, selects, removes | fixed |
| 16 | queue/sample lists, error slots, Play, reduced motion, batlow strips, listbox roles, 640 px overlay, fixture buttons, dead code, meters, V ≤ 0, estimate, conflict string, Cancel gating, Docker text, handlers | Minors | 2 | as listed by the lens | all applied (see the gate message) | fixed |
| 17 | Outline TE rail, stations as loft inputs, section monotone count | Marine CAD idiom | 2/1 | label honest; markers stated; count absent | recorded | residual |

## 4. Scorecard by dimension

| Dimension | First pass | After fixes | Worst remaining |
|---|---|---|---|
| Archetype fit | 4 | 4 | Export inline rather than the declared modal (recorded C1 deviation) |
| IA | 3 | 5 | stations are markers in the artifact |
| State completeness | 2 | 4 | loading skeleton for the Results layer list is coarse |
| Accessibility | 2 | 4 | native AT behaviour of SVG groups unproven |
| Craft | 3 | 4 | uniform bordered boxes remain the idiom |
| Copy truth | 2 | 4 | every number is a labelled fixture |
| Marine CAD idiom | 3 | 4 | TE rail plots chord; no section monotone count |

## 5. Generic-tells self-check (DX3)

| Tell | Present? | Justification |
|---|---|---|
| Default violet/indigo gradient or lone saturated blue | no | one teal accent for selection |
| Everything in same-radius, same-shadow cards | no | square panel joins; shadows only on overlays |
| Three equal stat tiles | five quantity tiles (Analysis); three candidate cards (Results) | UX-08 and the candidate provenance card; each carries tier and basis |
| Uniform spacing | no | 8/12/16/24 rhythm; detector clean |
| One or two type sizes | no | seven tokens |
| Lorem / placeholder numbers | no | fixtures labelled Illustrative; "Not recorded" where unmeasured |
| Emoji as iconography | no | — |
| Happy-path-only screens | no | eight states; failed, unsupported, cancelled and reduction-failed cases rendered |
| Symmetry everywhere | no | canvas-dominant asymmetric grid; console and viewport trade the inspector's lower half |
| Motion on everything, or none | none by design; one labelled dash affordance | stopped under reduced motion, seeded from the OS preference |

## 6. The Simplifier's delete-list

```
delete:  fixture controls inside the product region → harness header
delete:  dead task branches (sections, brief) and the v1 tab CSS
shrink:  three "Cancel" affordances → one, enabled only on a running case
yagni:   Play in the artifact (no autoplay by design) → disabled with its reason
net: -7 elements.
```

## 7. Ranked plan

**Must fix before ship** — none open after the clearance read.

**Should fix next**
1. Fit the loft through authored stations in the artifact (or keep the marker statement) — owner: `/design-slice` for the station model.
2. Plot the Outline TE rail as a position curve with chord as the typed value — owner: Marine CAD UX.
3. Native proof rows for SVG `role="group"` exposure in VoiceOver/NVDA and the ⌘⇧A chord on both OSes — owner: Native Desktop developer.

**Worth doing**
- Section-editor monotone-piece count; a finer loading skeleton for the Results layer list; Export as the declared modal or a recorded C1 deviation.

> **Do this one first:** the station fit — because it is the one place the artifact still teaches something the product will not do.

## 8. Residual risk & what this review did not cover

Everything here is proof about an HTML review artifact. Not covered: native screen-reader exposure of SVG groups; platform key conflicts for the chord; real browser zoom; pointer drag on section handles; the loft rule, blend and correspondence (not exercised); the scientific truth of any number (every result is a labelled fixture; the run is stepped by the review harness and no solver exists). The deterministic craft gate does not see per-sample rescaling, false inequalities or inline widths — three gaps the lens found by reading; recorded as UI-I in the defect-class register.
