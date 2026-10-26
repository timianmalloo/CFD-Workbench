---
id: proof-craft-gate-scanned-types
title: "Craft gate scanned types: what Impeccable 0.1.16 reads, and the Save dialog error state"
type: proof-pack
summary: "The detector walks 13 web file types and skips node_modules, dist, build, __pycache__ and dot-folders; Avalonia XAML matches no rule, so ui-craft-gate.py now refuses it. The Save-section name error is drawn per DR-CELL-3, for name refusals only, and both new checks were seen red first and their mutants fail."
status: active
owner: "@pc-laptop-leader"
phase: wave4
tags: [ui-craft-gate, impeccable, avalonia, accessibility, proof]
links:
  - { to: rulings, rel: depends-on }
---

# Craft gate scanned types, and the Save dialog error state

Two wave 3 follow-ups, closed on 2026-10-26 in branch `w4-copy-and-followups`.

## 1. ui-craft-gate.py refuses what its detector cannot read

**Question.** Wave 3 SC1 recorded a clean craft-gate run on `.axaml` as vacuous. Which inputs does the detector
actually read?

**Method.** `probe.sh <impeccable executable>`. It plants the same CSS anti-patterns (gradient text, bounce easing,
Inter) under every candidate extension and in candidate folders, then counts the findings. It ran on this ARM64 laptop
with the x64 engine `0.1.16+engine` under emulation, because no windows-arm64 build exists.

**Result (Verified, observed):**

| Probe | Result |
|---|---|
| Tree walk, extensions reported | `.astro .css .htm .html .js .jsx .less .sass .scss .svelte .ts .tsx .vue` |
| Not walked | `.xhtml .mjs .axaml .xaml .cshtml .razor .md .txt` |
| A named `.axaml` in XAML syntax (magenta, 9 px, Comic Sans) | 0 findings |
| A named file of any type holding CSS text | read as text (3 findings), so naming a file does not make its language supported |
| Folders the walk skips | `node_modules`, `dist`, `build`, `__pycache__`, any dot-folder; names are case-sensitive (`Node_Modules` is walked) |
| Folders walked | `vendor`, `out`, `bin`, `obj`, `public` |
| `.gitignore` / `.impeccableignore` naming a folder | not honoured (the folder is scanned) |

**Change.** `SCANNED` and `WALK_SKIPS` in `docs/ai-forward-pack/scripts/ui-craft-gate.py`. Before the detector runs,
the gate exits 2 for a file outside `SCANNED`, or for a tree with no such file outside the skipped folders. A mixed tree
runs, and the gate names the XAML and Razor files it skipped. `tests/test_ui_craft_gate.py` (6 tests, using a stand-in
detector that records whether it ran) was observed red against the previous gate, then green. A live run refuses
`src/CfdWorkbench.Desktop` and `SaveSectionDialog.axaml` with exit 2, and scans `docs/mockups/a3d-compare.html` (exit 0).

**Limits.** The sets are measured for engine 0.1.16 only: re-run `probe.sh` when the engine changes. One probe run
reported `Node_Modules` as skipped because of a probe bug. On a case-insensitive drive, two parent folders that differ
only in case are one folder. The probe now numbers its parent folders.

## 2. The Save-section name error is drawn as well as stated

**Change.** `SaveSectionDialog` draws DR-CELL-3 for a name refusal (`LIB-NAME-*`): focused, the field keeps its
`PrimaryBrush` box inside a 1 px `DangerBrush` ring; unfocused, the ring only. The ring is always there, transparent,
so the error moves nothing. The next save attempt clears the class, the ItemStatus and the help text together. A
store or library failure (`LIB-IO`, `LIB-CLAIM-HELD`, `DOC-*`) shows its text in the assertive region and leaves the
field unmarked, because it is not the name's fault.

**Evidence (Verified, observed).** `SaveDialog_Error_RenderedThenClearedOnRetry` was red on the missing class, then green.
Each of these mutants fails it:

| Mutant | Failure |
|---|---|
| no unfocused rule | `Unfocused error is not the danger box only: ring=#ffa92e37, box=#ffc9d3cf` |
| every error marks the name (the previous behaviour) | `LIB-IO marked the name invalid` |
| ring without thickness | `The error ring is not 1 px: 0,0,0,0` |

`bash tools/run-tests.sh` passed on the branch. The final run is recorded in the join commit.

**Residual (not assessed).** The check resolves brushes in the light theme only. It reads resolved properties, not pixels.
Dark and high-contrast rendering of the ring were not sampled. Fluent's focused border thickness was not measured
against DR-CELL-3's 1 px. That gap predates this change and applies to every TextBox.
