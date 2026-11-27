---
id: mockup-ai-guided-design-notebook
title: "AI-Guided Hydrofoil Design Notebook — interactive HTML review mockup"
type: doc
status: proposed
owner: "@timianmalloo"
phase: ui-design — interactive HTML review mockup (explore & iterate before native Avalonia build)
tags: [mockup, ai, notebook, grill-me, foilreq, hydrofoil, cad-handoff, ux, ui]
links:
  - { to: spec-ai-guided-design-notebook, rel: documents }
  - { to: ui-review-ai-guided-design-notebook, rel: relates-to }
  - { to: design-language, rel: depends-on }
  - { to: design-byok-assistants, rel: relates-to }
  - { to: design-section-catalog-expansion, rel: relates-to }
review-by: "2027-05-27"
summary: >-
  Self-contained, dependency-free, theme-aware interactive HTML mockup (docs/mockups/ai-guided-design-notebook.html)
  for CFD-Workbench's AI-Guided Hydrofoil Design Notebook. Exercises all seven workflow and hard states (empty brief,
  live serial Matt Pocock grill-me Q&A with 950 cm -> 950 mm unit check, narrated local solver run, iterated v1 vs v2
  comparison with live v3 refinement, geometry refusal & AI-03 withheld response, offline no-key AI-01 state, and
  overflow extremes) across Light/Dark/High-Contrast themes, 1440/1280/1024 viewports, Reduced Motion, and real
  degree-7 Bernstein CST section, polar, orthographic elevation, and rotatable 3D wing renderings.
---

# AI-Guided Hydrofoil Design Notebook — Interactive Review Mockup

Open [`ai-guided-design-notebook.html`](ai-guided-design-notebook.html) directly over `file://` in any browser (zero build step, zero CDN/external requests). Deep-link any state or theme with query strings such as `?state=grill&theme=light`.

**What to explore and iterate on here:**
1. **Two-pane notebook ergonomics:** Left interactive conversation & pinned `FoilReq 1.0` schema vs. right synchronized engineering Artifacts Workbench (`2D Section & Cp`, `Polars & Curves`, `Plan & Elevations`, interactive rotatable `3D Wing`, and `Scorecard & FoilReq`).
2. **Interactive Matt Pocock `grill-me` Q&A (`State: 2. Grill-Me`):** Click through Q1 (`950 cm → 950 mm` unit sanity check) → Q2 (F4 Orca AR 12 vs 90 kg takeoff tension) → Q3 (24 kn steep-drop cavitation floor) → Q4 (cutback roll/anhedral feel), or type custom answers and watch the pinned `FoilReq 1.0` DSL update live.
3. **Interactive iteration & live `v3` synthesis (`State: 4. Iterated v2`):** Toggle `Compare v1 vs v2`, drag or arrow-orbit the 3D foil loft, inspect the `Table twin`, click a quick refinement chip (or type a custom refinement and press **Send** / `Ctrl+Enter`) to synthesize candidate `v3` live, inspect the **`Preview redacted payload (AI-04)`** modal, and click **`Open in CAD Mode →`** to review the non-destructive CAD handoff confirmation.
4. **Hard states & honesty floors:** Switch to `3. Local Simulation Running`, `5. Refused Geometry & Withheld AI` (`DSL-TIP-CHORD-MIN` + `AI-03`/`LAB-01`), `6. No API Key / Offline`, and `7. Overflow & Extremes`, and toggle the **AA PASS** mechanical audit pill in the review harness bar.

## Review harness controls

| Control | Options | Purpose |
|---|---|---|
| **State** | `empty` · `grill` · `simulating` · `iterated` · `refused` · `no-key` · `overflow` | Verifies every happy, alternate, loading, refusal, offline, and extreme state (`DX9`, `DX16`, `U9`) |
| **Viewport** | `Desktop 1440 × 820` · `Laptop 1280 × 760` · `Compact 1024 × 700` | Verifies fixed-frame desktop layout and internal pane scroll |
| **Theme** | `Dark` · `Light` · `High-Contrast` | 100% `DESIGN.md` tokens; graphite `#17272c` scientific canvas across Light & Dark |
| **Reduced motion** | Off / On | Collapses all CSS transitions to instant (`U10`, `DX19`) |
| **Audit pill** | Click to expand | Live WCAG 2.2 AA contrast ratios computed from rendered tokens + frame box guard (`DX11`, `DC-200`) |
