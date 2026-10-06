---
id: design-dx-screen-states
title: "DX step 1: A3b and A3c screen states checked against the approved Area 3 mockup"
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [analysis, a3b, a3c, screen-states, mockup, copy, polar, cavitation, find-alpha, dx]
links:
  - { to: design-area3-analysis, rel: refines }
  - { to: mockup-area3-analysis, rel: relates-to }
  - { to: mockup-dx-section-polar-states, rel: documents }
  - { to: proof-spike-ana-1, rel: depends-on }
  - { to: proof-a3c-polar-source, rel: depends-on }
review-by: 2026-12-31
summary: >-
  Every visible A3b and A3c screen state (54), set against the operator-approved Area 3 mockup (rev 3, Ruling 63). 4 are
  covered by the approved mockup, 3 are covered in part, 47 are not covered; the 50 not or only partly covered are rendered
  in dx-section-polar-states.html for the operator's approval. Each state names its copy: an approved row, a spec
  string, or NEW with a proposed string (42 rows need operator copy). Nine decision requests are listed.
review-suggested: []
---

# DX step 1: A3b and A3c screen states against the approved mockup

**Result.** The approved mockup (`docs/mockups/area3-analysis.html`, rev 3) was built for A3a. For A3b and A3c it shows
only the *Unavailable* stubs: the Section tab is a label with no body, the Section (2D) group reads Unavailable, Total
drag names "profile" as missing, and the mockup's own choices list says Find operating α is "not on this page". Every
state that carries a value, a flag, a reason or a chart is **not covered**. The not-covered states are rendered in
[`docs/mockups/dx-section-polar-states.html`](../mockups/dx-section-polar-states.html) (guide:
[`.md`](../mockups/dx-section-polar-states.md)), in the approved page's shell, tokens and components.

Counts, over 54 states: **4 covered** (rows 9, 26, 40, 51), **3 covered in part** (rows 18, 30, 42: the stub or the
structure is drawn, the value or group is not), **47 not covered**. The 50 rows that are not or only partly covered
are rendered in the new mockup. Of those 50, 8 need no new copy (rows 1, 2, 4, 10, 12, 32, 43, 52) and 42 need operator copy.

Sources read: mockup rev 3 and notes; design `area3-analysis.md` §5.1, §5.4, §12, §12.3, §13.3, §16; Rulings 82, 86, 88
(D8, D14), 90, 92, 94; `spike-ana-1/verdict.md` "Conditions for A3c"; `a3c-polar-source/proof-pack.md`; `a3b/red-first.md`;
spec A5.3, A5.4, A5.6, ANA-01, 02, 05, 10, 21; `Cavitation.cs`, `Labels.cs`, `PanelMethod.cs`, `AnalysisProjection.cs`,
`AnalysisPanel.axaml(.cs)` (read only).

## Reading the table

- **Coverage.** *Covered* means the approved mockup renders that state; the element is cited. *NOT covered* means it
  does not; the screen in the new mockup is cited (D1 to D7).
- **Copy.** `COPY-nn` is an approved DESIGN.md row (Ruling 82 covers COPY-206 to 217 and 221 to 240; Rulings 92 and 94 cover
  220, 241, 242). *spec* is a fixed string in the spec with no COPY id (it still needs a DESIGN.md row). **NEW** means
  the string is not approved; the proposed text follows.

## A3b: Section tab, Cp, estimator, cavitation screen

| # | State | Coverage | Copy |
|---|---|---|---|
| 1 | Section tab body (any chart) | NOT covered: approved page draws the tab label only (`bottom()` tabs list); D1 | no copy |
| 2 | Cp plot present: suction up, upper solid / lower dashed, Cp_min marked, N stations, table twin | NOT covered; D1 | axis text only; the label row in 3 |
| 3 | Inviscid label on every Cp visual and the estimator fixed label | NOT covered; D1 | spec A5.6 "inviscid + turbulent-friction bound; deep water; steady" + 1.7 "inviscid; no boundary layer": NEW as a row, proposed `inviscid + turbulent-friction bound; deep water; steady · inviscid; no boundary layer` (depth unset: "free surface not modelled" replaces "deep water") |
| 4 | Estimator tier chip | NOT covered (approved page renders only the VLM chip); D1 | COPY-214 |
| 5 | Section view with Cp on the profile (vik pinned at 0, Cp_min marker) | NOT covered (approved views are Plan and 3D); D1 | legend "Cp · vik pinned at 0 · −a to +b": NEW |
| 6 | Section result values: cl, Cm c/4, α_L0, per span | NOT covered (approved row reads Unavailable only); D1 | row labels NEW: `cl (panel)`, `Cm c/4`, `α_L0 (panel)` |
| 7 | Estimator cd as the turbulent bound | NOT covered; D1, D7 | NEW: `Fully turbulent friction bound (ITTC-1957) at this Re; a bound, not a polar value` |
| 8 | −Cp_min with station count and TE panels excluded | NOT covered; D1 | NEW: `at x/c <x> on the <side> surface · 200 stations · three trailing-edge panels per side excluded` |
| 9 | Cp Unavailable: "no section Cp method (DR-ANA-2)" | Covered: screen 4, Section (2D) group, row Cp_min. Stale once A3b is wired: the string can no longer occur | COPY-212 (retire when A3b is wired) |
| 10 | Cp Unavailable after A3b (no accepted profile, panel solve failed) | NOT covered; no case is specified yet | NEW if a case is found; none proposed |
| 11 | Cavitation screen, value present: σ, −Cp_min, V_crit | NOT covered; D1, D4, D7 | row labels NEW |
| 12 | Fixed screen string with N | NOT covered; D1 | COPY-48 (already approved text); N = 200 |
| 13 | Margin 15 % with its label | NOT covered; D1 | spec A5.4 "practitioner assumption, not sourced": NEW row `15 % margin — practitioner assumption, not sourced` |
| 14 | Margin state: Clear | NOT covered; D1, D7 | NEW: `Clear — σ is above −Cp_min plus the margin` |
| 15 | Margin state: Inside the margin | NOT covered; D4, D7 | NEW: `Inside the margin — σ is above −Cp_min but within the margin` |
| 16 | Margin state: Possible above V_crit | NOT covered; D7 | NEW: `Possible — σ is at or below −Cp_min; speed is above V_crit` |
| 17 | Governing station and its depth (Ruling 86) | NOT covered; D1, D4 | NEW: `Governing station: η <η> · depth <h> m · smallest σ / (−Cp_min) of <n> stations` |
| 18 | Unavailable: depth not set | Covered in part: σ cell in the conditions band, screen 1 and 3b; V_crit row sits in a closed group, so not visible. The cavitation group itself is NOT covered | COPY-45 |
| 19 | Unavailable: vapour pressure missing | NOT covered; D7 | NEW (in code today, unapproved): `Unavailable — vapour pressure missing` |
| 20 | Unavailable: station surface piercing | NOT covered; D7 | NEW (in code): `Unavailable — local station is surface piercing` |
| 21 | Unavailable: water or local depth invalid | NOT covered; D7; defensive | NEW (in code): `Unavailable — water is invalid`, `Unavailable — local depth is invalid` |
| 22 | Undefined: −Cp_min ≤ 0 | NOT covered; D7 | COPY-69 with cause NEW: `Undefined — −Cp_min ≤ 0` |
| 23 | Undefined: static pressure not above vapour pressure | NOT covered; D7 | NEW: `Undefined — static pressure does not exceed vapour pressure` (code reads "pressure above vapour pressure ≤ 0") |
| 24 | Panel under-read measured per run (replaces the 1.61 % constant, Ruling 90) | NOT covered; D7 | NEW: `Cp_min under-read, 200 vs 400 panels` |
| 25 | Provisional when the under-read exceeds 10 % | NOT covered; D7 | NEW: `Provisional — Cp_min under-read at this station is above 10 % (200 vs 400 panels)` (DR-DXM-7) |
| 26 | Strip cd while no polar is installed | Covered: screen 4, Strip result row cd (profile) | COPY-210 |

## A3c: polar tier, flags, drag, Find α

| # | State | Coverage | Copy |
|---|---|---|---|
| 27 | Polar chart: cl and cd against α, Ncrit 4 and 2 band, α_eff marker | NOT covered; D2, D5 | COPY-44 on the band |
| 28 | Polar tier chip | NOT covered; D2 | COPY-215 |
| 29 | Surrogate label and COPY-66 | NOT covered; D2 | COPY-66 and NEW `surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only` (spike condition 3, proof pack proposal; DR-DXM-3) |
| 30 | Method id and version (in Properties, Provenance) | Covered in structure: Provenance "Method" row exists with "polar none". The value is NOT covered; D2 | NEW value `NeuralFoil-0.3.2/xxxlarge/94638c04` |
| 31 | analysis_confidence present | NOT covered; D2, D7 | NEW: `analysis_confidence 0.97 · advisory, not an error bar` |
| 32 | analysis_confidence not recorded | NOT covered; D7 | spec A5.3 `analysis_confidence: not recorded` |
| 33 | Low-confidence advisory (below 0.5, computed, never refused) | NOT covered; D7 | NEW: `Low confidence — analysis_confidence 0.31 is below 0.5. Computed and flagged, never refused; not an error bar.` |
| 34 | CST fit residual shown for the section | NOT covered; D2, D7 | NEW: `CST fit residual: max 1.09 × 10⁻⁴ c · RMS 2.93 × 10⁻⁵ c (shape residual, not an aerodynamic error)` |
| 35 | Inside the validated bracket | NOT covered; D2 | NEW: `Inside the validated bracket (α −6° to 6°, Re 2 × 10⁵ to 10⁶, Ncrit 2, 4, 9, NACA 0012 family)` |
| 36 | Outside the validated bracket, flagged with its axis (α, Re, Ncrit, section family) | NOT covered; D7 | NEW: `Outside the validated bracket — α 7.50° is beyond ±6°. Computed, not validated.` and one per axis |
| 37 | Non-computable: α, Re, Ncrit outside the training range; CST residual over the limit | NOT covered; D7 | NEW: `Unavailable — α 31.00° is outside the surrogate’s training range (−27.9° to 28.6°)`; `Unavailable — section fit residual 4.2 × 10⁻⁴ c exceeds the limit 3.6 × 10⁻⁴ c` |
| 38 | Strip Re inside the polar's Re range | NOT covered; D2, D7 | NEW (design §5.4 row, not in DESIGN.md): `Re_local <Re> inside <Re_min> to <Re_max>` |
| 39 | Strip Re outside the range, cd not extrapolated | NOT covered; D7 | NEW (design §12.6 row, not in DESIGN.md): `Re_local <Re> outside the polar’s Re range <Re_min> to <Re_max> — cd not extrapolated` |
| 40 | Strip Re range with no polar | Covered: screen 4, Strip verdict row "Polar Re range" (never "inside") | COPY-210 |
| 41 | Per-strip polar-versus-lattice consistency row | NOT covered; D2 | NEW: `Δ vs lattice Cl_local`, note `polar cl at α_eff against the lattice, a per-strip consistency check` |
| 42 | Strip envelope verdict unchanged, beside a separate polar-bracket row (D14 b) | Verdict covered: screen 4. The separate polar-bracket row is NOT covered; D2 | verdict: approved; row: `Polar bracket:` NEW (DR-DXM-2) |
| 43 | Tripped surface band | NOT covered; D2, D7 | spec table: `Unavailable — not computed` |
| 44 | Transition overlay x_tr/c per surface, both Ncrit, legend with revision, Re, Ncrit, surface state, range | NOT covered; D3 | legend line NEW; "Overlay a section ▾" NEW |
| 45 | Cavitation bucket: σ required and V_crit against Cl, operating line | NOT covered; D4 | NEW legend text; COPY-48 beside |
| 46 | Polar-sourced profile drag (band, both Ncrit) | NOT covered; D5, D7 | NEW: `Profile drag from the polar at α_eff, both Ncrit; band, not a prediction` |
| 47 | Estimator-sourced profile drag (the bound), kept apart from the polar value | NOT covered; D7 | NEW (see 7) |
| 48 | Profile drag with strips missing cd; the bound is not substituted | NOT covered; D7 | NEW: `Unavailable — cd missing at <n> strips; the estimator bound is not substituted` |
| 49 | Wing drag (induced + profile) | NOT covered; D5 | NEW: `Wing only: induced (VLM + strip) plus profile (polar). Not a total.` (DR-DXM-5) |
| 50 | Total drag with the profile part present | NOT covered; D5, D7 | NEW, derived from the approved string: `Unavailable — missing: junction, mast, wave, spray` |
| 51 | Total drag with no profile part | Covered: screens 3, 3b and 4 (Loads), Wing result row | approved mockup string `Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray` |
| 52 | Undefined: CD ≤ 0 | NOT covered (approved page has no total CD to divide by); D7 | COPY-230 |
| 53 | Find α dialog and a found α (bracket, iterations, termination, basis, polar limit, CL_max) | NOT covered: the approved page lists Find α under "Not on this page"; D6 | CL_max string is spec ANA-05 (`attached-flow polar limit, not measured stall; pumping not modelled`); the rest NEW: `α <a>° meets CL <t> within 1 %` and row labels |
| 54 | Find α, no root: five reasons (no sign change, polar not converged, confidence below floor, out of Re envelope, h/c below floor) | NOT covered; D7 | NEW: `Find α found no α — <reason>. Nothing was extrapolated.` with one reason sentence each |

## Findings from reading the code (read only)

- `AnalysisProjection.cs:119` still emits the Unavailable stub for the Section (2D) group, and `AnalysisPanel` builds the Section
  tab as a text table of that group. A3b computes (`Cavitation.cs`, `SectionEstimator.cs`) but nothing draws it.
- `Cavitation.cs` already carries five reason strings that are not approved rows (rows 19 to 23). They reach a user the first time
  A3b is wired.
- `PanelMethod.cs:23-27` still sets 400 panels and the 1.61 % constant; Ruling 90 says 200 panels and a per-run measured
  under-read. Out of scope here; recorded for the A3b wiring track.
- `IPolarSource.Sample` has no field for the bracket flag, low-confidence warning or CST residual (proof pack, tracked items).
  Rows 31 to 37 need that seam before they can render.

## Decision requests

DR-DXM-1 copy batch (every NEW row). DR-DXM-2 what "envelope" means in the UI after A3c (Ruling 88 D14 b): keep the
attached-flow verdict, add a separate polar-bracket row; open whether outside-bracket shows a flagged value or none.
DR-DXM-3 tier chip and surrogate label. DR-DXM-4 where the Section charts live. DR-DXM-5 meaning of Total drag with a profile
part. DR-DXM-6 Find α entry. DR-DXM-7 the provisional state. DR-DXM-8 the polar's Re range. DR-DXM-9 which station the
Section tab shows. Options and recommendations are on the mockup page and in its guide.
