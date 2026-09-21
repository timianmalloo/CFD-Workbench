---
id: review-ui-workbench-v3
title: UI review — workbench mockup v3 (thick-client shell)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, ux, accessibility, desktop, mockup, v3]
links:
  - { to: spec-cfd-workbench-v1, rel: documents }
  - { to: mockup-workbench-v3, rel: relates-to }
  - { to: design-language, rel: relates-to }
  - { to: workbench-direction, rel: relates-to }
  - { to: thick-client-shell, rel: relates-to }
  - { to: review-ui-workbench-v2, rel: supersedes }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-21
summary: >-
  Elevate-mode review of the thick-client shell rebuild. The v2 page was measured first (1,450–6,500 px tall, a
  wrapping area strip, a clipping toolbar); the v3 shell was built to a shell contract proven by its oracle at five
  window presets × six areas. The independent UX & Accessibility lens returned BLOCK on its first read (a clipped
  overflow menu, a 0-px bottom panel at the reflow preset, focus dropped on re-render, composite roles without
  keyboards, one-way dock collapse) and the Native Desktop lens PASS-WITH-CONDITIONS (sashes, maximize, real
  document tabs, the macOS title bar, platform key labels); both sets were built and are observed by the oracle.
  The veto cleared on the third read; the review artifact passes.
review-suggested:
  - { by: mockup-workbench-v3, on: 2026-09-20, reason: "Mockup v3 (thick-client shell) supersedes v2 as the review artifact; shell contract proven by tools/check-mockup-v3.mjs; UI-23 and the activity rail in spec 1.1a." }
---

# UI review — workbench mockup v3 (thick-client shell)

*Produced by `/ui-design` (mode: **elevate**). Governed by `ui-design-craft.md` DX22–DX25 over the floors in `ui-interaction-design.md` (U1–U20) and the native-client trigger UI-T4. Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surface(s) reviewed:** `docs/mockups/workbench-v3.html` — the shell (title bar / menu strip · one-row measured toolbar · parameter row · activity rail · Navigator dock · document tabs over the viewport or document · tabbed bottom panel · Properties dock with the prompt entry · status bar · sashes · drawers at 640 × 400) and the seven vignettes arranged in it.
**Reviewed against:** spec v1.1/1.1a `docs/specs/cfd-workbench-v1.md` (B1 verb table, B7 wireframe, C1 archetypes, UI-18, UI-23, UX-17–22) · `DESIGN.md` §5 and §12.0b · the direction brief's v3 section · `docs/lessons/defect-classes.md` (UI-B, UI-C, UI-F, UI-H, UI-I, UI-H2).
**Reviewers:** UX & Accessibility (lead, a11y hard veto; one independent read plus a clearance read) · Native Desktop Developer (advisory, platform idiom) · the author (measurements and fixes only; cleared nothing).
**Date:** 2026-09-21 · **Mode:** elevate

## 1. Verdict

> **PASS (accessibility veto cleared)** — the UX & Accessibility lens returned **BLOCK** on its first read, **PASS-WITH-CONDITIONS** (two line-local conditions) on the clearance read, and **PASS** on the third read after both conditions landed and were observed by the oracle; the Native Desktop lens returned **PASS-WITH-CONDITIONS** (advisory) and its four Majors were built. Verdict is on the HTML review artifact; native accessibility, DPI and signing stay unproved (UI-T4 proof pack at handoff).

> **Highest-leverage change:** the shell contract became an oracle *before* the content was re-homed — "the window never scrolls; the toolbar is one 44 px row that overflows into `More ▾` by measurement; every dock scrolls inside itself" — so every later layout decision (sashes, maximize, drawers, gutters) was measured against the same 30 cells rather than eyeballed, and the user's two complaints (page scroll, a toolbar that wraps) cannot come back without the build going red.

| | First pass (UX & Accessibility) | First pass (Native Desktop) | Clearance read |
|---|---|---|---|
| Blockers (sev 4, or any a11y ≥ 3) | 2 (+ 7 a11y Majors counted as Blockers under the obligation) | 0 | 0 |
| Majors (sev 3) | 9 | 4 | 2 (conditions; closed and observed before the third read) |
| Minors (sev 2) | 8 | 14 | 4 (closed) |
| Nits (sev 1) | 1 | 2 | 1 (closed) + 1 on the third read (closed) |

**Accessibility veto:** **PASS** — cleared by the lens on the third read: keyboard end to end (shell regions, document forms and the modal observed), semantic roles with their keyboard patterns, token-pair and computed-colour contrast, and 1.4.10 / 2.4.3 / 2.4.7 / 2.4.11 / 4.1.2 observed at every preset. One nit closed after the read (static `menuitem` attributes deleted from the markup).

## 2. Measurements (DX23 — measure before you diagnose)

| Metric | v2 (baseline, measured before the rebuild) | v3 | Note |
|---|---|---|---|
| Window height at 1024 · 1280 · 1440 · 1600 px | 1,450–6,500 px (page scrolls at every width) | 700 · 800 · 900 · 1000 px (the window never scrolls) | 30 shell cells green in `docs/proof/workbench-v3-browser-check.json` |
| Primary navigation | area strip wrapping to 2–3 rows inside a 64 px title bar | 68 px activity rail, six tabs + Export + Checks + Settings, readiness in the accessible name | oracle group 3 |
| Toolbar | scrolled / clipped | one 44 px row; groups hidden in `More ▾`: 0 at ≥ 1280 px, ≤ 1 at 1024 × 700, ≤ 3 at 640 × 400 | oracle group 1; a moved control is visible, hit-testable and works |
| Panels that scroll internally | 0 | every dock, pane and document body | oracle group 1 |
| Interactive controls per area (1280 px) | Setup 43 · CAD 78 · Analysis 46 · Experiment 19 · Run 45 · Results 45 | Setup 38 · CAD 57 · Analysis 49 · Experiment 28 · Run 57 · Results 55 | the curve-editor palette left the toolbar; queue and environment moved into the docks |
| Simultaneous regions on the primary screen (CAD) | strip · project · canvas · inspector + prompt · editor · status | rail · Navigator · viewport · Curve editor pane · Properties + prompt · status | one focal point: the viewport; the parameter row is one line |
| Network calls on first load / page errors | 0 / 0 | 0 / 0 | asserted |
| Smallest rendered SVG text | ≥ 12 CSS px at five viewports | ≥ 12 CSS px at five presets × six areas (viewBoxes follow their containers) | oracle group 2 |
| Worst required contrast pairing | 3.43:1 (`--control` on `--surface`, UI) | 3.43:1 (same); worst text pair 5.34:1 (caption on soft) | plus an oracle scan that no viewport-only colour token colours dock or pane text |
| Arbitrary values / craft gate | one recorded deviation (em-dash count) | one recorded deviation (em-dash count); the first run's all-caps, border+shadow and cramped-padding findings fixed | `docs/proof/ui-craft-findings-v3.json` |
| `design-lint.py --strict` | pass | pass (shell tokens added) | |
| Artifact size | 237 KB | 272 KB (under the 300 KB direction budget) | single file |
| Browser oracle | 13 groups · 84 measurements | 15 groups · 78 measurements · 30 shell cells | `tools/check-mockup-v3.mjs` |

## 3. Findings

*Structure before surface (DX24). Severity 0–4 → Blocker(4) / Major(3) / Minor(2) / Nit(1); an accessibility finding at ≥ 3 is a Blocker under the obligation. Lens: A = UX & Accessibility, D = Native Desktop. Confidence is the lens's own label; "observed" means the oracle now measures it.*

| # | Lens | Location | Dimension | Sev | Evidence (first pass) | Fix applied | Disposition |
|---|---|---|---|---|---|---|---|
| 1 | A | `.toolbar` / `#tb-more .menu` | Accessibility (2.4.11), task completion | 4 | the overflow menu's containing block sat inside the toolbar's `overflow:hidden`; the moved verbs were unreachable at 1024 and 640 | `.toolbar{overflow:visible;z-index}` with the app column `minmax(0,1fr)`; oracle opens the menu by real click, asserts the moved button visible and hit at its centre, activates → Experiment through it | fixed, observed |
| 2 | A | `[data-window="zoom"] .editor-area` | Accessibility (1.4.10, 4.1.2) | 4 | the bottom panel was 32 px (tab strip only) at the reflow preset; every bottom tab's content was lost | `--bottom:160px` at zoom, `.bottom{grid-row:3}` (a hidden sash row had swallowed it); starts collapsed, expands to 160 px; oracle: visible pane ≥ 120 px at every preset × area | fixed, observed |
| 3 | A | every renderer that replaces its container | Accessibility (2.4.3), class UI-C | 3 | rail, toolbar, bottom-tab, editor, navigator and results re-renders dropped focus to `<body>` | `focusKey/restoreFocus/withFocus` over ten renderers; oracle activates a rail tab, a toolbar button and a bottom tab by keyboard and asserts the same logical control keeps focus | fixed, observed |
| 4 | A | rail · doc tabs · bottom tabs · toolbar · listboxes · menubar | Accessibility (4.1.2, 2.1.1) | 3 | composite roles without their keyboard patterns; a `tab` without a `tablist`; every option tabbable; a menubar with no menus | WAI-ARIA tabs (roving tabindex, arrows, Home/End) on three tablists; listbox pattern on stations, samples, cases; toolbar arrow traversal; `#doctabs` is a tablist with a working tab; menubar → `group`; panels `aria-labelledby` | fixed, observed (rail, bottom tabs) |
| 5 | A | `#left-collapse` / `#right-collapse` | Accessibility (2.4.11), idiom | 3 | the only expand control lived inside the 0-px collapsed dock | 24-px gutter with `#left-expand`/`#right-expand` outside the dock; focus moves to it and back | fixed, observed |
| 6 | A | drawers at 640 × 400 | Accessibility (2.4.3) | 3 | focus targeted a `display:none` button; Escape always returned to the left opener | first *visible* focusable; opener remembered and refocused | fixed, observed |
| 7 | A | `#layer-list` tier caption | Accessibility (1.4.3) | 3 | inline `--viewport-mute` on `--surface` = 1.74:1, outside the audited pairs | inline colour removed; oracle scans dock/pane text for viewport tokens | fixed, observed |
| 8 | A | `#shape-canvas` keydown · document keydown | Copy truth, 2.1.1 | 3 | ⌘Z zoomed *and* undid; ⌘Z/⌘K stolen from text fields | modifier chords ignored by the viewport; editable targets keep native undo; oracle asserts zoom unchanged under ⌘Z | fixed, observed |
| 9 | A | Results `compact` mode | State completeness, class UI-I | 3 | labels dropped with a comment claiming the caption and Properties carried them — they did not | caption carries L/D and the separation criterion; Properties has Lift/Drag and Separation rows always | fixed |
| 10 | A | `.banners` | State completeness (error, loading) | 3 | absolute banners covered the HUD and the first 100 px of documents | banners in flow (`grid-template-rows:auto minmax(0,1fr)`); Results banners join the shared row | fixed |
| 11 | A + D | `#doctabs` | IA (documents vs areas) | 3 | one tab per area renamed "the document" on every rail switch | one persistent tab per open design (dirty dot, ⌘S), Settings opens beside it; the rail selects the perspective; oracle asserts the tab name survives Run and Settings returns to the last area | fixed, observed |
| 12 | D | `#bp-curve` | Craft / idiom | 3 | the curve editor never fit its pane at any preset | pane fills its height; panel maximize (⌘⇧M); stacked layout below 720 px scrolls | fixed, observed |
| 13 | D | docks and bottom panel | Idiom (VS Code sash, Fusion panel edge) | 3 | no resizing anywhere | `role=separator` sashes: pointer drag, arrow keys, double-click reset, `aria-valuenow`; oracle resizes by keyboard and pointer and resets | fixed, observed |
| 14 | D | curve editor pointer path | Idiom | 3 | "no `pointerdown` on `#editor-svg`" | **finding wrong**: the anchors drag on `mousedown` (part of the v1 core); pointer orbit/wheel zoom in the viewport remain out of scope until the spec declares the pointer contract | recorded, not acted on |
| 15 | D | `.menubar` under macOS | HIG | 2 | in-window menu strip; document name at the right of the strip | macOS: the row is the title bar with the document proxy title and Saved/Edited; Windows: the strip | fixed, observed |
| 16 | D | key labels | HIG | 2 | "Ctrl+⇧A", "CtrlZ" on Windows | one `kbd()` formatter: ⇧⌘A vs Ctrl+Shift+A; oracle asserts both | fixed, observed |
| 17 | D | shortcuts | Idiom | 2 | no Ctrl+Y, no pane cycling, no panel/dock toggles, Ctrl+Alt+arrow collides with Intel hotkeys | Ctrl+Y (Windows), F6/⇧F6 pane cycle, ⌘J · ⌘B · ⌘⌥B · ⌘⇧M · ⌘, · ⌘S; fine orbit on `[` `]` | fixed |
| 18 | D | status bar | Craft | 2 | 11 segments incl. a 90-char shortcut string, clipped silently | 8 segments; segments ellipsize; status, parameter and document-tab rows join the audit and the oracle | fixed, observed |
| 19 | D | toolbar buttons | Idiom | 2 | no tooltips | `title` from the verb table plus the shortcut | fixed |
| 20 | A | tabs in 32-px rows | Accessibility (2.4.7) | 2 | focus ring clipped to slivers | `outline-offset:-3px` | fixed |
| 21 | A | `overflow` state | State completeness | 2 | one finding *describing* twelve | twelve fixture findings | fixed |
| 22 | A | ghost label | Copy truth (UI-I) | 2 | "ghost: r3" is a scaled current outline | "illustrative offset standing in for r3" | fixed |
| 23 | A | rail accessible names | Copy (U11) | 2 | "gated (SPIKE-03/04)" read aloud | **kept**: it is the spec's C2 readiness string; a plain-language alias is a spec change | recorded |
| 24 | A | state banners | Copy | 2 | area-blind loading/empty copy | not changed | recorded, next step |
| 25 | D | Run bottom tabs | IA | 2 | Queue duplicated between the navigator list and the Queue tab | **kept**: the list is compact, the tab carries reasons and retry | recorded |
| 26 | D | context menus | Idiom | 2 | none on stations, layers, tabs | not changed | recorded, next step |
| 27 | D | 640 × 400 label; DPI | Realism | 2 | "200 % scale" is a browser-zoom case, not native DPI | relabelled "(WCAG reflow)"; DESIGN.md §5 names the three unproved DPI cases | fixed |
| 28 | D | rail width | Doc drift | 1 | 56 px in the brief, 68 px built | brief reconciled | fixed |
| 29 | A (clearance read) | document-form renderers | Accessibility (2.4.3), class UI-C | 3 | the focus sweep stopped at the shell: Setup, Experiment, prompt entry, Checks, Brief, DAT, catalog and the section editor still dropped focus; ten `data-*` keys missing from `focusKey` | nine more renderers wrapped; twenty keys; dialog-aware restore; exclusion moves focus to the next finding or the Checks tab; oracle activates one control per document form | fixed, observed |
| 30 | A (clearance read) | viewport accessible name, iso caption | Copy truth (4.1.2) | 3 | "Control 5°" claimed after the ⌘Z guard removed it; `[` `]` undocumented | strings say "[ and ] 5°"; oracle asserts | fixed, observed |
| 31 | A (clearance read) | `.menubar .m` | Accessibility (4.1.2) | 2 | `menuitem` without a menu | role removed; oracle asserts zero | fixed, observed |
| 32 | A (clearance read) | sash focus indicator | Accessibility (1.4.11) | 2 | 2.7:1 at opacity .6 | opacity 1 (6.6:1) | fixed |
| 33 | A (clearance read) | named views at 640 × 400 | Accessibility (1.4.10) | 2 | Plan/Front hidden with the buttons | `#view-select` in the tab row at the reflow preset | fixed |
| 34 | A (clearance read) | dirty marker | Accessibility (1.1.1) | 2 | "black circle" read aloud | `sr-only` "edited, unsaved changes" | fixed |
| 35 | A (clearance read) | proof payload | Evidence hygiene | 1 | `hiddenByWindow` null (key mismatch) | `preset` key; values recorded | fixed |

## 4. Rubric scorecard (DX22)

| Dimension | v2 | v3 | Note |
|---|---|---|---|
| 1 Archetype fit | 3 | 5 | `Layout:ViewportWorkbench` is now built, not only declared |
| 2 Information architecture | 4 | 5 | documents vs perspectives settled; the queue and environment have homes |
| 3 Flow integrity | 4 | 4 | unchanged contracts, all re-proved |
| 4 State completeness | 4 | 4 | compact results, banners in flow, twelve-finding overflow; area-specific state copy still open |
| 5 Hierarchy | 3 | 4 | one focal viewport; palettes live with their panes |
| 6 Density | 3 | 4 | sashes and maximize let the user set it |
| 7 Copy | 4 | 4 | fixed strings verbatim; identifiers in accessible names recorded |
| 8 Motion | 4 | 4 | reduced motion observed |
| 9 Platform idiom | 2 | 4 | rail · docks · sashes · title bar · key labels; context menus and pointer orbit open |
| 10 Accessibility | 3 | 5 | lens verdict PASS; the oracle observes the shell and the document forms |
| 11 Craft floor | 4 | 5 | gate at its recorded floor |
| 12 Honesty | 5 | 5 | every number Illustrative; the oracle observes rather than assumes |

## 5. Ranked plan

1. **Done in this run:** the shell contract with its oracle; the accessibility blockers and majors; the desktop-idiom majors (sashes, maximize, document tabs, title bar, key labels).
2. **Next (spec first):** declare the pointer contract for the viewport (LMB constrained drag, MMB pan, Alt/RMB orbit, wheel zoom, Workbench vs Rhino preset) in Part B, then prove orbit and zoom by pointer in the mockup.
3. **Next (mockup):** context menus on stations, layers and document tabs; area-specific loading/empty copy; a plain-language readiness alias for the rail if the spec adopts one.
4. **At handoff (UI-T4):** the native proof pack — window chrome, per-monitor DPI, VoiceOver/NVDA over the chosen toolkit, signing and notarization — none of which an HTML mockup can evidence.

## 6. Evidence

`docs/proof/workbench-v3-browser-check.json` (15 groups · 78 measurements · 30 shell cells) · `docs/proof/ui-craft-findings-v3.json` · `DESIGN.md` §12.0b · `docs/notes/thick-client-shell.md` · `docs/lessons/defect-classes.md` (UI-H2).
