---
id: proof-stc-red-first
title: STC red-first receipts (Ruling 147)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [stc, red-first, ruling-147]
links:
  - { to: proof-sma-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  The Stations table caption (COPY-411): the checks that failed to build on the old code, the runs that pass after the change, and the captures.
---

# STC red-first receipts

Ruling 147: the Stations table gets a caption above it, "{shown} of {judged} stations shown — solved, governing and selected". {judged} is the count the wing cavitation line (COPY-304) prints, now one property, `SectionTierResult.JudgedCount`.

## Red

The three behaviours were written as checks before the code. On the old code the Analysis test project did not build: `dotnet build -c Release tests/CfdWorkbench.Analysis.Tests` gave 7 errors, among them `'SectionView' does not contain a definition for 'StationsCaption'`, `'Labels' does not contain a definition for 'StationsShown'`, `'SectionTierResult' does not contain a definition for 'JudgedCount'`. The Desktop check (`section-stations-caption` control and table help text) cannot pass on the old view, which has no such control (`Single` would throw). A compile failure is the red here; no behaviour was characterised on old code.

## Green

| Check | Mode | Result |
|---|---|---|
| `StationsCaption_TextCountsTheTableRows_AndMatchesTheLineCount_Ruling147` (a) | Analysis `--readiness` | PASS, 695 ms |
| `StationsCaption_JudgedCount_IsOneValue_MovesWithTipNotJudgedCount_Ruling147` (b) | Analysis `--readiness` | PASS, 152 ms |
| `SectionMainArea_StationsCaption_AboveTable_DescribesTable_Ruling147` (c) | Desktop `--readiness` | PASS, 2213 ms |

Command: `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet <test dll> --readiness`.

Check (b) plants `TipNotJudgedCount + 1` on the tier with a `with` expression; both the line and the caption move. The existing table-structure check now finds the header row by type, not by child index 1, because the caption sits between the heading and the header row.

## Captures

`01-section-stations-caption-light.png` and `01-section-stations-caption-dark.png` (1500 x 870, the Example foil, V 5.14 m/s, alpha 3, 4 spans per half so 6 stations): caption "4 of 6 stations shown — solved, governing and selected" above the column headers, in the note style. Command: `STC_CAPTURE_DIR=<dir> CFD_TEST_ONLY=SectionMainArea_StationsCaption_Captures tools/run-suite.sh dotnet <Desktop test dll> --readiness`. The dark capture re-themes only part of the harness window (the left panel stays light); the caption and table are on the dark side and legible.

## Craft gate

`python3 docs/ai-forward-pack/scripts/ui-craft-gate.py src/CfdWorkbench.Desktop/Analysis/SectionTabView.cs`: no findings (a floor, not a verdict).
