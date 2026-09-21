---
id: review-ui-workbench-v1
title: UI review — workbench mockup v1
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, ux, accessibility, mockup, v1]
links:
  - { to: spec-cfd-workbench-v1, rel: documents }
  - { to: mockup-workbench-v1, rel: relates-to }
  - { to: design-language, rel: relates-to }
  - { to: workbench-direction, rel: relates-to }
  - { to: review-ui-workbench, rel: supersedes }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-20
summary: >-
  Elevate-mode review of the v1 interactive mockup against specification v1. The independent UX & Accessibility
  lens returned BLOCK on the first pass (focus loss on nudge, handles under role=img, page-wide live region,
  sub-12 px chart text, NaN in the error state), PASS-WITH-CONDITIONS on the second, and PASS (veto cleared) after the
  conditions were applied and re-measured. Highest-leverage change: re-query the SVG handle after every rerender
  so keyboard editing survives — one line per editor that unblocked the keyboard-only persona entirely.
review-suggested:
  - { by: mockup-workbench-v1, on: 2026-09-20, reason: "Mockup v1 built against spec v1 and cleared by the UX & Accessibility lens 2026-09-20; supersedes the 2026-09-19 prototype as the review artifact." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table." }
---

# UI review — workbench mockup v1

*Produced by `/ui-design` (mode: **elevate**). Governed by `ui-design-craft.md` DX22–DX25 over the floors in `ui-interaction-design.md` (U1–U20). Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surface(s) reviewed:** `docs/mockups/workbench-v1.html` — Brief, Shape (single and lines-plan), Sections (catalog, section editor, DAT dialog), Analyze (section · wing · compare), Checks drawer, Export dialog, Assistant, Settings; states default · first launch · loading · empty · error · partial · overflow · success; themes light · dark · high-contrast; viewports 1024 · 1280 · 1440 · 1600 · 640 (200 % zoom of 1280); personas designer · keyboard-only · screen-reader · reviewer; capabilities no key · key/unevaluated · key/evaluated.
**Reviewed against:** spec v1 Part B/C `docs/specs/cfd-workbench-v1.md` (C2 state · string · component table; UX-01…16; UI-01…17) · `DESIGN.md` (COPY-28…72, batlow/vik policy, `target-dense`) · archetype `ParametricWorkbench { … Nav:TaskTabs+CommandPalette; Input:PrecisionPointer+SpatialGestures+KeyboardFirst; … }` with `SciVizPipeline` inside Analyze.
**Reviewers:** UX & Accessibility (lead, a11y hard veto; two independent runs plus a clearance read) · Marine CAD UX expert (second lens of the first run) · the author (measurements and fixes only; cleared nothing).
**Date:** 2026-09-20 · **Mode:** elevate

## 1. Verdict

> **PASS** — the UX & Accessibility lens cleared the veto on its third read after the four conditions of the second pass were applied and re-measured; the two nits it raised at clearance (a hidden-element artefact in the oracle proof; the loading banner outside the Shape block) are fixed.
> **Highest-leverage change:** re-query the handle element after `innerHTML` replacement (`focusPoint(i)` in the channel editor, the `[data-sec]` re-query in the section editor) — one line per editor that turned the keyboard-only persona from "loses focus on the first arrow press" into "edits end to end" (DX25).

| | First pass | Second pass (after fixes) | Clearance read |
|---|---|---|---|
| Blockers (sev 4, or any a11y ≥ 3) | 5 | 0 | 0 |
| Majors (sev 3) | 12 (incl. 4 marine-CAD) | 4 (conditions A, B, C, E) | 0 |
| Minors (sev 2) | 15 | 3 | 0 |
| Nits (sev 1) | 3 | 5 | 3 (recorded residuals) |

**Accessibility veto:** **PASS** — clears when: *keyboard operation survives every rerender in both editors; every handle is exposed as a named, ranged, oriented slider outside any `role="img"`; there is exactly one live region; every text/surface and focus/surface pair, including the focus ring on the viewport, is ≥ 3:1 (text ≥ 4.5:1) in all three themes; no rendered text falls below 12 CSS px at any harness viewport; no NaN, Infinity or placeholder string renders in any state × destination; the C2 states render with their fixed strings.* Cleared by **the UX & Accessibility lens (independent agent run), not the author**.

## 2. Measurements (DX23 — measure before you diagnose)

| Metric | Value | Note |
|---|---|---|
| Interactive controls on the primary screen (Shape) | 44 | Brief 33 · Sections 75 · Analyze 30 · Settings 24 (browser oracle, 1280 px) |
| Simultaneous sections / cards | Shape: canvas · project tree · inspector · editor · status strip (5 regions, 1 focal) | the canvas is the one focal point; the four boxes of strings in Analyze were merged into one list at the second pass |
| Network calls on first load | 0 | asserted by the oracle (`externalRequests: []`) |
| Competing focal points | 1 | the viewport (DX18) |
| Distinct type sizes | 7 tokens (32/24/18/14/13/12 + 13 mono) | SVG text sized 13–15 units and measured after viewBox scale |
| Distinct colours in use | 60 `var(--…)` token references; 0 literal colours in the design region | harness chrome uses the dark-theme tokens so the detector reads one palette |
| Modes/views doing the same job | 0 | plan / front / iso / lines-plan are views of one selection, not modes |
| Arbitrary values (non-token) in the component code | 0 flagged by `ui-craft-gate.py` | the one standing finding is `em-dash-overuse` — the spec's fixed strings (recorded deviation, CD16) |
| `design-lint.py --strict` | pass (0 warnings) | after batlow/vik/`target-dense`/COPY-28…72 were added |
| Worst required text/surface contrast pairing | 3.43:1 (`--control` on `--surface`, a UI boundary; needs 3.0) | text pairs: worst 5.63:1 (`--mute` on `--canvas`); focus ring on the viewport 8.60:1 |
| Smallest rendered text | 12.0 CSS px (HTML captions) at every viewport; 20.2 px chart text at 1024 | oracle 18: font-size × applied viewBox scale, per visible destination |
| Artifact size / external requests / page errors | 154 KB · 0 · 0 | under the 300 KB direction budget |
| Browser oracle | 29 oracles · 38 measurements green | `docs/proof/workbench-v1-browser-check.json` |
| GEO-13 property | gaps 1.027e-5 → 5.176e-6 → 2.599e-6 → 1.302e-6 → 6.518e-7 over weights 1→16 | strictly decreasing, never zero — observed, not claimed |
| GOAL-02 arithmetic | 7/7 (V, CL, σ) triples match the pinned inputs; σ 7.56 at h_ref 0.3 m | observed |

## 3. Findings

*Ordered structure before surface (DX24). Severity 0–4 → Blocker(4) / Major(3) / Minor(2) / Nit(1); an accessibility finding at ≥ 3 is a Blocker under the obligation. "First" = found in the first independent run; "Second" = found in the second; disposition after the author's fix and the oracle's re-measurement.*

| # | Location | Dimension | Sev | Evidence (observed / measured) | Fix applied | Confidence · disposition |
|---|---|---|---|---|---|---|
| 1 | channel and section editors, arrow keys | Accessibility (U16) | 4 | First: `nudge(i, dir); el.focus()` after `innerHTML` replacement detached `el`; focus fell to body on every press (SC 2.1.1, 2.4.3) | re-query after render (`focusPoint(i)`; `[data-sec]` re-query) | Verified · fixed; oracle 16 asserts `activeElement.dataset.point` after ArrowUp/PageUp/Home |
| 2 | `#editor-svg`, section SVG | Accessibility | 4 | First: handles were interactive descendants of `role="img"`; no aria-valuemin/max/valuetext/orientation | SVGs `role="group"`; sliders carry static name, min, max, valuenow, valuetext with unit, `aria-orientation="vertical"`; PageUp/Down, Home/End, Shift ×10 | Verified · fixed; oracle 17 aria snapshot ≥ 6 sliders |
| 3 | `<section class="centre" aria-live="polite">` | Accessibility | 3→B | First: every nudge re-rendered inside a live region (SC 4.1.3) | attribute removed; `#status-msg role="status"` is persistent markup | Verified · fixed |
| 4 | Cl–α α-cursor | Accessibility (1.4.11) | 3→B | First: `--station` on light surface ≈ 1.6:1 | cursor on `--primary` (6.1:1); pair audited | Verified · fixed |
| 5 | Analyze charts, viewport captions | Accessibility / craft (HighLegibility) | 3→B | First: 5.6–9 px annotations at 1280/1600 (viewBox scaling). Second (B): oracle measured charts only and a bbox against an 11.5 px floor | charts `auto-fit minmax(400px)` with 13-unit text; captions moved to HTML at the 12 px token; oracle computes font-size × applied scale per visible destination and asserts ≥ 12 | Verified · fixed; evidence in the oracle's proof |
| 6 | Analyze, error state and V = 0 | State completeness / UI-12 | 3 | First: `polar(α, NaN)` → "NaN – NaN", "NaN N". Second (C): band cells "NaN×10-Infinity" and "Infinity" at V = 0 | `waterOut` and `q === 0` route every result and band cell to Unavailable/Undefined with cause; `fmtSci` guarded; window-wide NaN/Infinity scan over 8 states × 5 destinations × 3 scopes and at V = 0 | Verified · fixed |
| 7 | C2 rows | State completeness (U9) | 3 | First: COPY-30, -32, -39 (+Acknowledge), -58, -69 and the Catalog original / Modified from header absent; placeholder leaks. Second (E): loading state only on Brief | all six rendered in their named components; Analyze `loadingView()` and Shape "Rebuilding preview…" | Verified · fixed; oracles 20 and 23 |
| 8 | infeasible lock set | UX-12 / copy truth | 3→2 | First: unreachable, Release list hard-coded. Second (D): message named DOF as the cause | inspector fixture lock; computed removable list with one-action Release; message names the conflict and quotes the solver's reason | Verified · fixed |
| 9 | iso caption, status strip, Fit | Copy truth (UI-03) | 3 | First: orbit/zoom/Fit claimed, no handler | stepped orbit 15°/90°/5°, Z/⇧Z, F on the focused viewport; Rhino caption says pointer mapping is not in this artifact | Verified · fixed; oracle 22 |
| 10 | "Screen reader view" blocks | Copy truth | 3 | First: described a tree the artifact lacks | retitled "Target native reading (not delivered by this artifact)"; station ticks carry names | Verified · fixed |
| 11 | nudge labels, expression echo, section editor precision, comb direction | Marine CAD idiom | 3 | First: "mm" on twist and t/c; static echo; no typed y/c; comb teeth vertical | per-unit ladders; live echo of the model value; typed y/c with steps and error; comb on the normal | Verified · fixed |
| 12 | focus ring on the viewport | Accessibility (1.4.11) | 3→B | Second (A): `--focus` #006c67 on `--viewport` #17272c = 2.45:1 for the newly focusable section handles | `.canvas [role="slider"]:focus` etc. outline `--station` (8.60:1); pair audited | Verified · fixed; oracle 23 |
| 13 | ARIA structure | Accessibility | 2 | orphan labels, dangling `for`, `radiogroup` owning toggle buttons, dead tabstops, `aria-selected` on `<tr>` | spans, `aria-labelledby`, `role="group"`, static lists, class-based selection; listbox roving tabindex and tab arrow keys **not** done | Verified · partially fixed (residual recorded) |
| 14 | invalid numeric entry | Error prevention (3.3.1) | 2 | Second (G): silent revert on `#op-v/#op-h/#op-a/#s-te`; `#s-margin` no handler | `.err` slot with cause; previous value kept | Verified · fixed |
| 15 | 640 px viewport | Accessibility (1.4.10) | 2 | Second: "no overflow" at 640 vacuous (window clips) | recorded: the frame stacks panes; the data table scrolls in its own container (permitted); native 200 % proof stays Flagged | Inferred · recorded, not proven |
| 16 | `prompt()` exclusion, Home/End range, "Acknowledge to continue" gates nothing | Nits | 1 | as named | `simplify:` marker; others recorded | Verified · recorded |

## 4. Scorecard by dimension

| # | Dimension | First pass | After fixes | Worst remaining |
|---|---|---|---|---|
| 11 | **Archetype fit** | 4 | 4 | KeyboardFirst now real for orbit/zoom/fit; native key conflicts untested |
| — | IA | 4 | 4 | station not pickable from the viewport itself |
| 12 | **State completeness** | 3 | 4 | loading state is a fixture, not a live run |
| 14 | **Accessibility (WCAG 2.2 AA)** | 2 | 4 | listbox roving tabindex; native tree unproven |
| 17 | **Craft** | 3 | 4 | uniform bordered boxes remain the idiom |
| 16 | **Content & copy** | 3 | 4 | "Acknowledge to continue" gates nothing in the artifact |
| — | Marine CAD idiom | 3 | 4 | pointer drag on section handles absent |
| 13 | **Token discipline** | 5 | 5 | one recorded deviation (em-dashes in fixed strings) |
| 15 | **Performance & stability** | 4 | 4 | 154 KB, 0 requests, 0 errors; no layout shift on state change |
| 18 | **AI-surface honesty** | 4 | 4 | no-key, unevaluated, rejected-field, withheld-numeral, cap and no-action states |

## 5. Generic-tells self-check (DX3)

| Tell | Present? | If present: the deliberate justification |
|---|---|---|
| Default violet/indigo gradient or lone saturated blue | no | one teal accent reserved for selection |
| Everything in same-radius, same-shadow cards | no | square panel joins; 4 px controls; 8 px dialogs only; shadows only on overlays (no border + shadow) |
| Three equal stat tiles | five quantity tiles in Analyze | required by UX-08 (five quantities visible together); each carries tier and basis, not a KPI |
| Uniform spacing (no grouping rhythm) | no | 8/12/16/24 rhythm; detector's spacing rule clean after the second pass |
| One or two type sizes, weight doing all the work | no | seven token sizes |
| Lorem / placeholder names / placeholder numbers | no | placeholders removed at the second pass; numbers computed from pinned inputs or labelled fixtures |
| Emoji as iconography | no | — |
| Happy-path-only screens | no | eight states, three capabilities, error/partial/empty/loading rendered |
| Symmetry everywhere with no earned asymmetry | no | canvas-dominant asymmetric grid |
| Motion on everything, or none at all | none by design | Motion:Micro is hover/focus only; reduced motion proven |

## 6. The Simplifier's delete-list

```
delete:  five navigation presets → two (Workbench, Rhino); the rest carry a simplify: trigger (spec A8.1)
delete:  the section-editor demo buttons inside the design → one harness button
shrink:  four bordered string boxes → one bordered list
yagni:   objective definition and robustness rule on the Goal state (reserved until a reader exists — spec A3.1)
native:  none (prompt() stands in for the exclusion form; simplify: marker)
net: -9 elements.
```

## 7. Ranked plan

**Must fix before ship (Blockers)** — none open after the clearance read.

**Should fix next (Majors, ranked by user impact × effort)**
1. Listbox roving tabindex and tab arrow keys (#13) — one handler each — owner: `/design-slice` of the workbench shell.
2. Native proof pack rows for SVG `<g role="slider">` exposure in VoiceOver/NVDA and the Option/Alt-arrow key conflicts (#12, residual) — owner: Native Desktop developer.
3. A real 200 % zoom measurement (browser zoom, not a 640 px frame) (#15) — owner: Test Architect.

**Worth doing (Minors / Nits)**
- Section handle pointer drag; station picking from the viewport; Home/End to the announced range; "Acknowledge to continue" that gates generation; an inline exclusion form.
- Raise to the spec owner: UI-11 asks for the control polygon in Smooth, but the polygon joins the eight solver control points the user cannot grab while the six influence controls are the weighted targets (A4.2) — a False-CAD-Model risk to decide in the spec, not the mockup.

> **Do this one first:** the roving tabindex on the station listbox — because it is the last keyboard idiom gap on the primary screen and costs one handler.

## 8. Residual risk & what this review did not cover

Everything here is proof about an HTML review artifact. Not covered: native VoiceOver/Narrator behaviour of SVG sliders and the focus ring on SVG; platform key conflicts (Windows Ctrl+Alt+arrow, macOS Option+arrow in text contexts); real browser zoom at 200 %; pointer drag on section handles; loft options, zebra, right-click menus; scientific truth of any number (every result is Illustrative). The oracle's 0/0/0 is token pairs, boxes and text sizes; the accessibility pass is the lens's, given on the evidence named above. Defect classes recorded from this review: UI-F (unscoped attribute selector), UI-G (review chrome measured as product surface) in `docs/lessons/defect-classes.md`.
