---
id: mockup-setup-seeding
title: "Area 1 — Setup & Goal State interactive HTML review mockup"
type: doc
status: proposed
owner: "@timianmalloo"
phase: ui-design — review mockup (no code build until operator approves)
tags: [mockup, setup, goal-state, seeding, soft-targets, class-rules, presets, hard-states, wave5]
links:
  - { to: design-setup-seeding, rel: documents }
  - { to: spec-amendment-setup-seeding, rel: relates-to }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: design-language, rel: depends-on }
review-by: 2026-12-19
summary: >-
  Self-contained, dependency-free, theme-aware interactive HTML mockup (docs/mockups/setup-seeding.html) for Area 1
  Setup & Goal State. Exercises all six hard states (clean start with Stated/Inferred/Defaulted badges, Flagged
  acknowledgement gate for Wakefoil -> Surf, SET-01 conflicting soft targets with live 2D deterministic solver and
  deviation badges, GWA Race class-rule DRC violations with Jump and Exclude, Re-seed on an already-edited r4
  document preserving history and marking prior runs Historical, and AI-02 language entry slot in its no-key state)
  across Light/Dark themes, 1440x900 / 1280x800 / 1024x700 viewports, four review personas, and Reduced Motion.
---

# Area 1 — Setup & Goal State mockup

Open [`setup-seeding.html`](setup-seeding.html) directly over `file://` (no build step, zero external requests). A query string selects any state directly, e.g. `setup-seeding.html?state=conflict-targets`.

**What the operator reviews here:** layout, workflow, hard states, copy wording, and the five Decision Requests (`DR-SET-1..5`) in [`docs/design/setup-seeding.md`](../design/setup-seeding.md). No C# source or `DESIGN.md` rows are modified until approved.

## Review harness controls

| Control | Options | What it checks |
|---|---|---|
| **State** | `clean-race` · `flagged-gate` · `conflict-targets` · `class-violation` · `reseed-edited` · `no-key-language` | All six required Flow F6 / F3 hard states |
| **Persona** | `Engineer / Maker` · `Keyboard-only` · `Screen-reader` · `Reviewer` | Focus visibility, accessible roles/names, audit visibility |
| **Viewport** | `1440 × 900` · `1280 × 800` · `1024 × 700` | Fixed-window desktop shell with internal scroll per pane (UI-23) |
| **Theme** | `Light` · `Dark` | `DESIGN.md` light and dark semantic token sets; graphite `#17272c` viewport retained |
| **Reduced motion** | `Standard` · `Reduced motion` | Removes all CSS transitions (`data-motion="reduced"` and `prefers-reduced-motion`) |

## Six hard states rendered interactively

1. **`clean-race` — Clean start · Discipline preset:** Loads Wingfoil race (`1000 cm²`, `AR 12`, `b_max 1.10 m`, `c_max 120 mm`, `taper 0.24`), per-field `Stated` / `Inferred` / `Defaulted` badges, GEN fallback notice (`Eppler E817 Pending admission → NACA 4412`), live planform SVG with 4-CV control polygon, Wing estimates (`≈`), and the **Goal state & feasibility** tab with exact `GOAL-02` 7-point arithmetic (including one-click `P1 h_ref = 0.3 m → σ = 7.56`).
2. **`flagged-gate` — Flagged acknowledgement required:** Loads `Wakefoil → Surf preset` with the `Flagged` boat-wake disclosure and `COPY-39`. **Seed design** stays `aria-disabled="true"` until the reviewer checks **Acknowledge flagged assumptions**.
3. **`conflict-targets` — Conflicting soft targets (`SET-01`):** Defaults to the tight-span conflict (`target AR 12 · seeded 11.6 · −3 %`) with `COPY-73`, plus a one-click **Load SET-01 triple (`w_b=1`)** button proving `(1000 cm², 0.8 m, 100 mm, w = (1,0,0,0))` seeds `b = 0.800 m` exact + conflict notice (never a hard error). Every target and weight is live-editable.
4. **`class-violation` — Class-rule DRC violation (`CAT-04`):** GWA Race Box (`v2022-02-01`) with `S = 650 cm² < 700 cm²` and `mast = 120 cm > 115 cm`. Shows both located DRC findings with rule id, version, date, **Jump to field**, and **Exclude finding…** (requiring a non-empty reason). Geometry is never mutated.
5. **`reseed-edited` — Re-seed on an already-edited document (`SET-03`, `GEO-04`):** Document at `r4` (`Direct parametric`), existing VLM run `k-8a2f1c Current`. Shows ghosted `r4` outline behind the `r5` candidate, explains that `r1–r4` remain in history and earlier runs become `Historical`, and relabels the primary action **Re-seed as r5**.
6. **`no-key-language` — Language entry slot with no API key (`AI-01`, `SET-02`):** Highlights `"Describe a starting design"` (`COPY-79`) showing `COPY-53` (*"No API key configured — every design and analysis tool works without it · Configure key"*) with **Configure key** action while keeping the entire parameter form and presets 100 % active.

## Automated checks (measured 2026-11-19)

| Check | Result |
|---|---|
| **LAB-01 forbidden-word scan** (`setup-seeding.html` + proposed `COPY-SET-*` rows in `setup-seeding.md`) | **PASS — 0 hits** across all seven forbidden terms (`cavitation-free`, `ventilation-safe`, `validated`, `optimized`, `best`, `recommended`, `certified`) |
| **Token & external-request static audit** (`DESIGN.md` frontmatter hexes vs `setup-seeding.html`) | **PASS — 34 hex tokens used, 0 off-token hexes, 0 external URLs, 88,418 bytes** (budget `<= 250,000` bytes) |
| **WCAG 2.2 AA contrast audit** (16 Light, Dark, and Graphite Viewport token pairs) | **PASS — 16 / 16 pairs meet floor** (text `>= 4.5:1`, controls/viewport strokes `>= 3.0:1`; worst text pair `5.85:1`, worst control pair `3.43:1`) |
| **JavaScript syntax & in-page deterministic self-test** (`node` syntax parse + `GOAL-02` 10 kn `0.773 / 7.71 / 7.56` + `SET-01` triple `b = 0.8000 m`) | **PASS** (47,458-char script parses cleanly with 0 errors; both math oracles verified) |
| **UI craft gate (`ui-craft-gate.py`)** | **Not recorded on this Windows ARM64 host** (`impeccable` 0.1.27 ships no `impeccable-windows-arm64.exe` binary/checksum; fail-closed exit 1). Run on macOS ARM64 or Windows x64 before build sign-off. |
| **Docs & PII gates (`docs-graph.py derive` + `tools/check-docs.py`)** | **PASS — exit 0** (`624` artifacts, `0` problems, `0` PII hits) |
