---
id: mockup-local-sweeps
title: "Local-tier sweeps & performance curves — interactive review mockup (Experiment setup, local Run, sweep Results)"
type: doc
status: proposed
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, sweeps, area-4, analysis, local-tier, performance-curves, hard-states, operator-show]
links:
  - { to: design-local-sweeps, rel: documents }
  - { to: spec-amendment-local-sweeps, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: design-language, rel: depends-on }
  - { to: mockup-area3-analysis, rel: relates-to }
review-by: "2026-12-19"
summary: >-
  Self-contained, dependency-free interactive HTML mockup (docs/mockups/local-sweeps.html) covering all seven hard
  states of Area 4 Local-Tier Sweeps & Performance Curves over local in-process VLM + strip and Estimator tiers:
  Empty, Invalid input blocked, Preview ready (12-case 3 speeds x 4 alphas with derived Re/q/h/c/Fr_h/sigma and zero
  Run button in Experiment setup), Running / Cancelled (live local progress and non-destructive Cancel), Completed with
  1 failed case (11 Completed + 1 Failed with explicit non-bridged chart gaps, Ncrit 2-4 bands, Cm datum, table twin,
  linked selection, and Retry sample), Depth unset (free surface not modelled, never deep water), and Historical sweep
  after geometry edit. Includes review harness (persona, viewport 1440x900 / 1024x700, state, light/dark/high-contrast
  theme, reduced motion) and live WCAG contrast audit.
---

# Local-tier sweeps & performance curves — review mockup

Open [`local-sweeps.html`](local-sweeps.html) directly over `file://`. It is 100 % self-contained with zero network
requests, external fonts, or third-party scripts. The design is [`docs/design/local-sweeps.md`](../design/local-sweeps.md)
and the companion spec note is [`docs/specs/amendments/spec-local-sweeps.md`](../specs/amendments/spec-local-sweeps.md).

**What the operator reviews here:** workflow separation (`XS-03`), preview validation (`XS-01`), local run/cancel/retry
ergonomics (`RUN-05`/`RUN-06` local), performance-curve readability with explicit gaps (`ANA-08`, `ANA-16`, `RES-04`),
honest depth-unset and Historical states (`ANA-07`, `ANA-19`), and proposed copy (`§5.3` of the design). Nothing is
built until this mockup and `DR-SWP-1..6` are approved.

## Review harness controls

- **State (7):** `Empty` · `Invalid input blocked` · `Preview ready (12 cases)` · `Running / Cancelled` · `Completed (11/12, 1 failed)` · `Depth unset` · `Historical (r4 → r5)`.
- **Persona (4):** `designer` · `keyboard-only` (adds persistent keyboard shortcut hints and visible tab-order badges) · `screen-reader` (exposes an inline live ARIA announcement log and accessible-name readout for every focused/selected control and chart gap) · `reviewer` (shows design-rule annotations tracing each region to `XS-01`, `XS-03`, `ANA-08`, `ANA-16`, `RES-04`, `ANA-19`, `LAB-01`).
- **Viewport (2):** `1440 × 900` · `1024 × 700` (fixed-frame shell; internal pane scroll only, zero page overflow inside the shell frame).
- **Theme (3):** `light` · `dark` · `contrast` (WCAG high contrast), using `DESIGN.md` tokens verbatim.
- **Reduced motion:** toggles instant transitions and disables any progress pulse; stepping remains explicit.
- **In-page WCAG contrast audit:** computes live luminance contrast ratios from rendered CSS custom properties for every
  text and control/focus token pair in the active theme, plus a live `LAB-01` forbidden-word scan over the rendered shell.

## Rendered hard states

| State | Sub-view | Key verified behaviours in the mockup |
|---|---|---|
| **Empty** | Experiment setup | Empty guidance (`SWP-COPY-1`), `New sweep` action, `Queue` disabled, **zero Run button** (`XS-03`), status strip `Sweep: none`. |
| **Invalid input blocked** | Experiment setup | Duplicate speed (`14, 14`), negative speed (`−5`), and out-of-ITTC temperature (`55 °C`, `COPY-52`) highlighted with `aria-invalid`, per-field inline errors, preview blocked, `Queue` disabled. |
| **Preview ready (12 cases)** | Experiment setup | 3 speeds `[10, 14, 18] kn` × 4 alphas `[0, 2, 4, 6]°` = 12 unique cases; live-computed `q`, `Re_ref`, `h/c`, `Fr_h`, `σ` for Example foil (`b = 0.900 m`, `c_ref = 0.120 m`, `S_ref = 0.108 m²`, salt 15 °C); interactive tier switch (`VLM + strip` vs `Estimator`) updating storage estimate (`≈ 1.53 MB` vs `≈ 25.5 kB`) and 32 MB headroom; wall time reads `Not recorded`; **no Run button**, only `Queue`. |
| **Running / Cancelled** | Run (`local · in-process`) | Shows `Running 7 of 12` with per-case rows (`Completed` 1–6, `Running` 7, `Pending` 8–12) and live interactive **Cancel** button (in both console and status strip) that transitions to `Cancelled (6 of 12 completed)` preserving cases 1–6 and keeping `Resume remaining` available. |
| **Completed (11/12, 1 failed)** | Results (local sweep) | Interactive SVG performance curves (`CL`, `CD (Wing only, Ncrit 2–4)`, `Wing-only L/D (Ncrit 2–4)`, `Cm (frame origin, c_ref)`) toggleable between **vs α (°)** and **vs Speed (kn)**; interior case `14 kn · α 4°` (case #7) failed (`ANA-SOLVE-RESIDUAL`) so BOTH `vs α` and `vs Speed` views prove an **explicit interior gap with zero line connecting across the gap**; distinct dash/marker + direct label per series; one-click **Show table** twin; clicking any chart point, small-multiples card, or table row synchronizes selection (`ANA-08`) and offers **Retry sample** on the failed case. |
| **Depth unset** | Results (local sweep) | `h_ref` unset: `h/c`, `Fr_h`, `σ`, and `V_crit` read `Unavailable — depth not set` (`COPY-45`); fixed VLM basis reads `attached flow; no stall; no ventilation; free surface not modelled` (`COPY-224`) — zero occurrences of `"deep water"`; cavitation ceiling band omitted with explicit reason. |
| **Historical (r4 → r5)** | Results (local sweep) | Amber Historical banner `Historical — surface changed (r4 → r5, a twist point moved)`, Historical tier chip on every case and chart header, per-case freshness derived by run key, plus `New version on r5` and `Archive sweep` controls. |

## Fixture basis (all values computed in page, labelled Illustrative)

All hydrodynamic and derived condition values in `local-sweeps.html` are computed live in JavaScript from the Example
foil dimensions (`b = 0.900 m`, `c_ref = 0.120 m`, `S_ref = 0.108 m²`, `AR = 7.50`, `−2°` tip washout) and ITTC
7.5-02-01-03 Rev 03 standard seawater at 15 °C (`ρ = 1026.02 kg/m³`, `ν = 1.1892 × 10⁻⁶ m²/s`, `p_v = 1670.9 Pa`,
`p_atm = 101325 Pa`, `g = 9.80665 m/s²`, `1 kn = 0.514444444 m/s`). The mockup's embedded algebraic polar/lifting-surface
fixture is **Illustrative — not computed for this design**; production numbers come from `CfdWorkbench.Analysis`
(`VortexLattice`, `StripCoupler`, `NeuralFoil`, `WingEstimator`) during build nodes `SWP-2`–`SWP-4`.

## Not proved by this HTML artifact

Native Avalonia window chrome, OS accessibility trees (VoiceOver / Narrator / UIA), per-monitor DPI scaling, real
`.cfdw.json` persistence, and real `VortexLattice` / `NeuralFoil` solver convergence remain build-time verification
obligations (`SWP-1`–`SWP-5`).
