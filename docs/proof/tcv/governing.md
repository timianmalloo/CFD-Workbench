---
id: proof-tcv-governing
title: TCV governing station on the example wing, before and after Ruling 142, and the wing-verdict surface list
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [tcv, cavitation, tip-strip, ruling-142]
links:
  - { to: proof-tcv-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  Observed on the example wing: the governing station is eta 0.9757 before and after; three tip stations are now left out of the
  verdict. The list of every place that reads the tier's wing verdict, with file:line and whether it follows Ruling 142.
---

# TCV: governing station before and after, and where the wing verdict surfaces

Ruling 142: a tip-provisional strip never decides the wing cavitation verdict.

## Item 5. The example wing, observed

Check `TipCavitation_ExampleWing_GoverningStationIsJudged_BeforeAfterObserved` (readiness ring, `example-wing-run.txt` is its output).
Default product wing (`Settings.Default`, 129 stations), salt water, `Fixture.Op(3)`. "Before" is the same run's strips with the tip flag
cleared, which is what the tier saw before this track. "After" is `SectionTier.Derive` on the real run.

| | governing eta | state | sigma | -Cp_min | tip stations left out |
|---|---|---|---|---|---|
| Before | 0.9757 | Clear | 7.7090 | 1.0796 | 0 |
| After | 0.9757 | Clear | 7.7090 | 1.0796 | 3 (eta 0.9997, 0.9998, 1.0000) |

The three tip stations read sigma/(-Cp_min) = 7.810 at 200 panels; the governing station reads 7.140. So on the example at this operating
point **the tip stations did not govern and the verdict number is unchanged**. This settles the reviewer's Inferred residual: the defect was
latent here, not live. The wing line now reads "...; 3 Not judged — tip strip". The rule matters at an operating point where the tip strip's
alpha_eff is the largest on the wing; the planted tests (`red-first.md`) pin that case.

## Capture

`tip-section-app.png`: the real `ShellHost` (1500 x 1100), example wing at NSpanPerHalf 4, depth 0.6 m, tip station eta 1 selected (opened and read).
It shows: the station table row "eta 1" with "Not judged — tip strip" in both the -Cp_min and Cavitation columns; the profile plate "Not judged — tip strip"
(its cavitation line); the estimator group "-Cp_min: Not judged — tip strip"; the wing-level Cavitation group unchanged
("Clear — ...; 3 Not judged — tip strip", sigma 7.80, -Cp_min 0.83, "of 6 stations" = judged only, governing eta 0.191).
**Retaken after the Ruling 142 (3) extension** (the first capture still showed "Cp_min -0.46 · x/c 0.091 · upper", the ring, the chart's Cp_min point and the legend
"-0.46 to +0.99", whose low end is Cp_min). The current capture was opened: the Cp_min ring and plate are gone, the legend reads "Cp · vik pinned at 0", the aria text reads
"...; Cp_min Not judged — tip strip" (`capture-run.txt`), and the Cp chart has no Cp_min point. Remaining numbers on the picture are the Cp distribution (colour bar ends
-0.99 / 0.99, set by the pressure side here) and the wing-level group, which stays the wing result.
**All-tip wing line, observed** (`TipCavitation_Exclusion...` readiness check prints it): Section view wing line `[Not judged — tip strip]`, Analysis panel Cavitation row
`[Not judged — tip strip]`, band V_crit `[Not judged — tip strip]`. `UnavailableBecause` does not wrap it: the reason maps whole through `ReasonTexts`. Each is asserted equal to `Labels.TipNotJudged`.

## Item 4. Every reader of the wing verdict, and whether it follows the rule

| # | Reader (file:line) | What it shows | Follows Ruling 142 |
|---|---|---|---|
| 1 | `SectionTier.cs:125-159` (`Evaluate`: 200-panel pass, 400-panel candidates, near-tie width) | the governing station | yes, judged stations only; all-tip falls back to all so the Section view has a station |
| 2 | `SectionTier.cs:164-176` (wing screen via `Cavitation.SelectWing`) | the wing `CavitationResult` | yes, `SelectWing` receives judged screens only; all-tip replaces the screen with reason `ANA-TIP-PROVISIONAL` |
| 3 | `Cavitation.cs` `ScreenWing` (CavitationStation list) | a wing reduction used by tests only; no production caller (grep of `src`) | not changed: it has no strip, so it cannot know a tip flag. No production path reaches it |
| 4 | `SectionTier.cs:54-80` (`Derive` polar read at `GoverningEta`) | polar and consistency at the governing station | yes, follows the judged governing station |
| 5 | `SectionDisplay.cs:123-132` ("Cavitation screen" row) | the wing line | yes, suffix `; N Not judged — tip strip`, or the whole text when all-tip |
| 6 | `SectionDisplay.cs:139` ("Governing station" row, COPY-304) | "of N stations" | yes, judged stations only |
| 7 | `AnalysisProjection.cs:213-218` (Analysis panel "Cavitation" row) | the wing line | yes, same suffix; all-tip reads Not judged via the reason text |
| 8 | `AnalysisProjection.cs:102-109` (conditions band "V_crit") | wing V_crit | yes, note carries the suffix; all-tip reads Not judged |
| 9 | `AnalysisProjection.cs:208` (Analysis panel "Cp_min" row) | the governing station's Cp_min | yes (the governing station is judged; all-tip reads Not judged) |
| 10 | `Desktop/Analysis/SectionTabView.cs:46-48` (bottom-panel Section summary) | shown station's cl and -Cp_min from the estimator group | yes, it reads the display group, so a tip station shows Not judged |
| 11 | `Desktop/Analysis/SectionTabView.cs:81` (render key) | `GoverningEta` | yes, no value shown |
| 12 | `Desktop/Analysis/SectionTabView.cs:143-170` (Stations table) | table rows | yes, reads `StationTableRow` |
| 13 | `Desktop/Analysis/SectionProfileView.cs:30,112-114` (Cp_min marker plate and aria text from `profile.CpMinPanel`) | a -Cp_min number at the shown station | yes, after the extension: `SectionProfile.TipNotJudged` drops the ring, the plate and the legend range, and the aria text reads Not judged |
| 14 | `SectionDisplay.cs` `Charts` (Cp chart "Cp_min" marker, legend range) | a Cp_min point at the shown station | yes, after the extension: no marker, legend without range |
| 15 | `AnalysisService.cs:169-171` (trace only) | counts | not a verdict surface |
| 16 | Properties, Browser, status strip | none read `SectionTier` or `Cavitation` (grep of `src/CfdWorkbench.Desktop`, `Core`) | n/a |

Red-first for rows 13 and 14: `red-run-2-analysis.txt` (profile flag false, chart marker present) and `red-run-2-desktop.txt` (plates "Cp_min ring | Cp_min −0.50 · x/c 0.500 · upper" drawn on a
tip profile); both pass after the fix (`green-readiness.txt`, readiness ring: `TipCavitation_TipStationDisplay...` and Desktop `SectionProfile_TipStation_NoCpMinRingPlateOrAriaNumber_Ruling142`).
The ring itself is drawn directly (not a plate), so it is pinned by the capture and the code guard, not by a check.
