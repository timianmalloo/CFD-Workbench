---
id: ui-review-ai-guided-design-notebook
title: "UI design direction & rubric review — AI-Guided Hydrofoil Design Notebook"
type: doc
status: draft
owner: "@timianmalloo"
phase: ui-design (create mode — direction brief, measurements, rubric critique, ranked plan)
tags: [ui-review, ui-design, ux, notebook, ai, hydrofoil, mockup]
links:
  - { to: spec-ai-guided-design-notebook, rel: documents }
  - { to: mockup-ai-guided-design-notebook, rel: relates-to }
  - { to: design-language, rel: depends-on }
review-by: "2027-02-27"
summary: >-
  Creative direction brief (DX5/DX6), triggered-standards union (UI-T1 + UI-T3 + UI-T4), verified quantitative
  measurements (DX23), structure-before-surface rubric critique (DX22/DX24), generic-tells audit (DX3), Simplifier
  delete-list, and ranked elevation plan for CFD-Workbench's AI-Guided Hydrofoil Design Notebook mockup.
---

# UI Design Direction & Rubric Review — AI-Guided Hydrofoil Design Notebook

*Produced by `/ui-design` (mode: **create**). Governed by `ui-design-craft.md` (DX1–DX25), `ui-interaction-design.md` (U1–U20), `technical-ui-design.md` (TQ1–TQ12), and `DESIGN.md`.*

**Surface:** AI-Guided Hydrofoil Design Notebook (`docs/mockups/ai-guided-design-notebook.html`)
**Reviewed against:** `docs/specs/ai-guided-design-notebook.md` (Parts A, B, C) · `DESIGN.md` · Archetype `GuidedDesignNotebook` (G3 + G1 companion)
**Date:** 2026-11-27 · **Mode:** Create (direction + high-fidelity interactive HTML mockup + Adversary-hardened critique)

---

## 1. Creative direction brief (DX5 / DX6 — words before pixels)

- **User & emotional state:** An experienced hydrofoil designer / rider-maker arriving with a clear mental riding feel and commercial foil references, wanting rapid physics-backed exploration without losing engineering rigor or fighting manual B-spline curves prematurely.
- **Job-to-be-done:** Decompose riding intent into structured `FoilReq 1.0` requirements, resolve physical tensions via serial Q&A, visually inspect local physics artifacts across iterations, and switch cleanly to CAD mode once a geometry-checked 3D candidate exists.
- **Archetype Signature:** `GuidedDesignNotebook { Type:Authoring; Arch:SPA; Layout:NotebookCells; Density:Compact; Nav:TopBar+ProgressPills+CommandPalette; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:Utilitarian; Depth:Flat; Sync:AsyncPipeline; Persistence:LocalDevice; Feedback:Generative+Choreographed+Confirmed; Motion:Micro; Pacing:UserDriven; Transition:HardCut; A11y:WCAG_2.2_AA+ReducedMotion; x-platform:"cross-platform"; x-framework:"avalonia"; x-companion-pane:"SynchronizedArtifactWorkbench"; }`.
- **Three defining adjectives and their opposites:**
  1. **Rigorous, not bureaucratic** — crisp requirement chips and 1-at-a-time grill-me cards instead of sprawling multi-page forms.
  2. **Conversational, not chatty** — compact notebook turns, local solver receipts, and Tier-0 cited numerals instead of chat walls.
  3. **Tactile, not theatrical** — real CST foil sections, polars, elevations, and rotatable 3D lofts on calm graphite instead of glowing marketing tiles.
- **Named references (adapt, never clone — DX4):**
  - **Google Antigravity & Claude Code / Artifacts:** Two-pane conversational thread + synchronized live artifacts pane with compact execution receipts and version switching (adapt structure; avoid opaque auto-mutations).
  - **Observable / Marimo (Catalog G3):** Explicit provenance and live reactive outputs below structured state (adapt lineage & staleness honesty; avoid raw code cells for geometric tuning).
  - **CFD-Workbench `DESIGN.md` (G1/G2):** Graphite scientific canvas (`#17272c`), single teal selection (`#006c67` / `#88d8c6`), `{colors.focus-ring-viewport}` (`#66ddc8`) on graphite, diverging `vik` colormap pinned at `Cp = 0`, tabular monospace readouts, zero card clutter.
- **Personality in three moves (DX6):**
  - **Type:** Platform system sans (`14 px` body, `14.5 px` session title, `12 px` compact chrome) paired with tabular monospace (`11–13.5 px`) so every physical quantity aligns cleanly.
  - **Color:** Calm neutral chrome (`canvas`/`surface`) framing one high-contrast graphite scientific canvas (`#17272c`) with a single earned teal accent (`#006c67` Light / `#88d8c6` Dark / `#ffee58` High-Contrast); amber/crimson reserved strictly for semantic warnings, unit checks, and refusals.
  - **Space:** Tight `6–8 px` spacing inside cells and metric bands, crisp `12 px` gutters between panes and turns, `1 px` hairline splits instead of nested drop-shadow cards.
- **Anti-goals:** No purple/indigo AI gradients, no glowing hero panels, no thick side-tab accent bars, no ungrounded marketing words (`LAB-01`), no hidden CAD overwrite, no vertical burial of engineering plots inside chat bubbles.
- **Triggered standards union:** **UI-T1** (expert quantitative `TQ1–TQ12`) + **UI-T3** (AI surface HAX + Shape-of-AI + wrong-answer/withheld states) + **UI-T4** (native Avalonia target — HTML mockup is direction evidence only, native proof pack required at build time). **UI-T2** does not fire (zero generated raster assets).

---

## 2. Measurements (DX23 — verified mechanically on `ai-guided-design-notebook.html`)

| Metric | Verified Measured Value | Target / Floor |
|---|---|---|
| Artifact size (uncompressed, self-contained HTML) | `141,137 B` (`137.8 KiB`, `3,113` lines — strictly `< 250,000 B` budget) | `< 250 KB` (`DESIGN.md` §8) |
| External network requests / CDNs | `0` | `0` (`DX8`) |
| `DESIGN.md` CSS custom properties declared & actively used | `39` declared, `39` actively used, `0` unused, `0` off-token hexes | `0` off-token (`U3`) |
| `LAB-01` forbidden product copy hits | `0` across all seven banned terms (`cavitation-free`, `ventilation-safe`, `validated`, `optimized`, `best`, `recommended`, `certified`) | `0` |
| Interactive controls mounted in primary iterated view | `21` (header `3`, pinned FoilReq `1`, composer/tuners `5`, artifact tabs/toggles `7`, 3D view controls `5`) | Compact workstation (`≤ 51` ceiling) |
| Minimum interactive target height | `32 px` (`DESIGN.md` `target-dense: 32px`, exceeds WCAG 2.2 SC 2.5.8 `24 px` floor) | `≥ 32 px` dense / `≥ 24 px` floor |
| Competing focal points | `1` (right-hand graphite engineering artifact canvas; left thread stays calm chrome) | `1` (`U6`/`DX18`) |
| Distinct theme modes verified in-artifact | `3` (Light, Dark, High-Contrast) | `3` |
| Hard states rendered interactively | `7` (`empty`, `grill`, `simulating`, `iterated`, `refused`, `no-key`, `overflow`) | All required (`DX9`, `U9`) |
| Worst text / UI contrast pairings across all 19 audited pairs (incl. graphite focus & control borders) | Light: `5.22:1` text (`--ink-mute`/`--selection`) · `3.13:1` UI (`--control-line`/`--canvas`, viewport focus `7.11:1`–`9.30:1`) · Dark: `5.22:1` text (`--ink-mute`/`--selection`) · `4.27:1` UI (`--control-line`/`--surface`) · High-Contrast: `17.62:1` text / `17.62:1` UI | `≥ 4.5:1` text / `≥ 3.0:1` UI (`WCAG 2.2 AA`) |
| `ui-craft-gate.py` (Impeccable) | Platform note: Windows ARM64 binary unavailable in Impeccable 0.1.27 (same recorded host status as `mockup-setup-seeding.md`); full static token, 19-pair WCAG AA contrast (incl. all 16 gated in-artifact pairs), `FoilReq 1.0` EBNF parse, ITTC physics, `LAB-01`, and `node --check` audits executed directly | Floor check |

---

## 3. Rubric critique (DX22 / DX24 — structure before surface)

| # | Location | Dimension | Sev | Evidence | Recommended fix | Confidence |
|---|---|---|---|---|---|---|
| 1 | `Scorecard & FoilReq` tab | 6 Direct manipulation + precision (`TQ6`) | 2 (Minor) | `FoilReq 1.0` DSL block is read-only `<pre>` with working Copy in mockup v1; power users will want direct inline edit + deterministic re-validate right inside the DSL tab. | Add editable `FoilReq` text editor with live `FoilReqValidator` feedback in `/design-slice` and mockup iteration 2. | **Verified** |
| 2 | Header (`1024 × 700` compact viewport) | 15 Performance & stability / Layout | 1 (Nit) | Secondary solver chip hides cleanly at `compact` (`1024 px`), leaving the consolidated `Model: Sonnet 4.6 (Eval ✓)` chip; long overflow titles truncate cleanly with ellipsis. | Add click-to-inspect model routing popover on the Model chip during `/implement`. | **Verified** |
| 3 | `Plan & Elevations` SVG plot | 17 Craft / Interactivity (`TQ6`) | 1 (Nit) | `2D Section & Cp` and `Polars & Curves` now carry live pointer hover tracing probes, and Starboard draws real CST section contours; `Plan & Elevations` does not yet trace spanwise `η` chord on hover. | Extend hover tracing to spanwise `η` stations in `/implement`. | **Verified** |
| 4 | Native client build (`UI-T4`) | 14 Native Desktop Proof (`DX9a`) | 3 (Major, build-gate) | HTML mockup proves flow, density, states, keyboard/focus traps, target sizing, and 19-pair token contrast (16 gated in-artifact + 3 static), but does not prove Avalonia DPI scaling, OS windowing, or `Styles.axaml` 30-brush conformance. | Produce native proof pack + Avalonia `NotebookWorkspaceView` tests during `/implement`. | **Verified** |

---

## 4. Scorecard by dimension

| # | Dimension | Verdict | Notes |
|---|---|---|---|
| 1 | Visibility of system status | Pass | Phase pills, local solver receipt, narrated progress, stale flag on re-opened grill, status strip |
| 2 | Match to the real world | Pass | Riding brief → `FoilReq` units → ITTC `kn`, `mm`, `cm²`, `AR = b²/S`, `σ`, `V_crit`, `h/c`, `Fr_h` |
| 3 | User control & freedom | Pass | Re-open any grill pill, Skip, Generate now, Cancel sim, Compare `v1`/`v2`, non-destructive CAD handoff |
| 4 | Consistency & standards | Pass | 100% `DESIGN.md` tokens, graphite viewport + `{colors.focus-ring-viewport}`, `vik` legend, `AI-01..06` & `LAB-01` |
| 5 | Error prevention | Pass | Deterministic `950 cm → 950 mm` sanity catch, `DSL-TIP-CHORD-MIN` block, disabled CAD button + inline reason |
| 6 | Recognition over recall | Pass | Pinned `FoilReq` chips, clickable grill-me options with impact previews, quick tuner chips |
| 7 | Flexibility & efficiency | Pass | `Ctrl/Cmd+Enter` send, tablist arrow keys, modal Escape + focus trap, keyboard 3D orbit |
| 8 | Aesthetic & minimalist design | Pass | Clean 1 px hairlines, zero card soup, single teal accent, single focal viewport, 0 unused tokens |
| 9 | Error recovery | Pass | Consented `Ask AI to repair` (≤ 2 cap), `Switch to v1`, `Configure key`, `Use manual Setup / CAD` |
| 10 | Help & documentation | Pass | Teaching empty state, `Needed for` per grill question, explicit `A5.1`/`A5.3`/`A5.4`/`ANA-05` honesty strips |
| 11 | **Archetype fit** | Pass | Serial left notebook + parallel spatial right artifact workbench matches reading-vs-entering split |
| 12 | **State completeness** | Pass | All 7 hard states rendered and interactive with zero stale cross-candidate mismatch |
| 13 | **Token discipline** | Pass | 39 declared tokens, 39 used, 0 unused, 0 off-token hexes across Light, Dark, High-Contrast |
| 14 | **Accessibility (Visual AA / Keyboard / Targets)** | Pass (visual/kb/targets); Native AT Flagged | All 19 audited contrast pairs pass in Light/Dark/HC (incl. graphite focus); `≥ 32 px` targets; tablist & modal keyboard complete; live OS screen-reader runs excluded per standing directive |
| 15 | **Performance & stability** | Pass | Self-contained `141,137 B` (`137.8 KiB`, strictly `< 250 KB` budget), 0 external calls, shape-matched skeletons (`CLS = 0`) |
| 16 | **Content & copy** | Pass | Verified ITTC physics data, explicit units & precision, 0 `LAB-01` banned words |
| 17 | **Craft** | Pass | Genuine scale contrast, tabular numerics, real degree-7 CST contours in Section, Side, and 3D views, live hover probes |
| 18 | **AI-surface honesty** | Pass | HAX + Shape-of-AI complete; `Flagged` reference traits; `AI-03` withheld & live dynamic `AI-04` payload preview |

---

## 5. Generic-tells self-check (DX3)

| Tell | Present? | Verification |
|---|---|---|
| Violet/indigo gradient or lone saturated blue | No | Strict `DESIGN.md` teal (`#006c67` / `#88d8c6`) + graphite `#17272c` |
| Nested same-radius shadowed cards | No | Flat `1 px` hairlines (`elevation: flat`), shadow reserved strictly for modals |
| Three equal stat tiles | No | Unified 6-cell hairline metric strip with primary span cell visually ranked |
| Uniform spacing | No | Tight within-cell (`4–6 px`), distinct between-turn (`12 px`) |
| Lorem / placeholder numbers | No | Real ITTC 15 °C seawater physics + degree-7 CST `cfdw-surf-118`/`105`/`dw-092` |
| Emoji as iconography | No | Clean typographic symbols (`✓`, `›`, `→`, `←`, `⚠`) & SVG plots only |
| Happy-path-only screen | No | Full 7-state harness including refusal, withheld response, no-key, and overflow |
| Thick coloured side-tab bars | No | 1 px full semantic borders per `mockup-harness.template.html` |

---

## 6. The Simplifier's delete-list

```
delete:  Removed unused --batlow-* and --dur-base tokens from the mockup stylesheet (0 unused tokens remain).
yagni:   Cut separate Fast-Model LLM endpoint in favor of Tier-0 deterministic C# for session titles, parameter diffs, and percentage deltas (0 tokens; preserves AI-04 single-endpoint rule).
yagni:   Keep OpenFOAM/SU2 execution out of the notebook composer (hand off to Area 4/5 after CAD handoff).
net: -1 redundant LLM endpoint, -6 unused CSS custom properties, 0 duplicated Run controls.
```

---

## 7. Ranked plan (DX25)

- **Must fix before native build (Blockers):**
  1. Complete `/design-slice` freezing C# `FoilReq 1.0` parser/DTOs, `CandidateIntentValidator`, `Envelope.notebook?` persistence, and `AssistantEvalSuite` v2.0 cases (`AI-06`) before writing `src/`.
- **Should fix next (Majors / High-Leverage):**
  1. **Highest-leverage change:** Make the `FoilReq 1.0` DSL tab directly editable with live deterministic `FoilReqValidator` feedback (unit checks + physics-tension recalculation) so expert users can combine conversational refinement with exact precision text editing (`TQ6`).
  2. Add a click-to-configure popover on the header Model chip for switching evaluated models on the configured single endpoint.
- **Worth doing (Minors / Nits):**
  - Extend SVG hover tracing probes to `Plan & Elevations` spanwise `η` stations and add a keyboard shortcut overlay (`?`).
