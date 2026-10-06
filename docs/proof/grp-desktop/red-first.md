---
id: proof-grp-desktop-red-first
title: "Track GRP half 2 (Desktop): planted mutants, observed red then green"
type: proof-pack
status: active
owner: "@timianmalloo"
tags: [proof, group-move, desktop, mutants, round-oct06]
links:
  - { to: design-group-move-node-m, rel: implements }
review-by: 2027-04-01
summary: >-
  Three planted mutants of the group-move build, each turned red by a named Desktop check and green again once
  restored. Ring: --controller-shell (controller checks) and --properties-cells (window checks), one check each,
  CFD_TEST_ONLY. Captures against the approved mockup are beside this file.
---

# Red first: planted mutants (2026-10-06, commit fda2a791 and later)

Each run is `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet run -c Release --no-build --project
tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- <mode>`. The mutant was one edited line, built, run,
then restored with `git checkout` and rebuilt (the green runs are in the full ring, `PASS` lines below).

| Mutant | Edit | Check that went red | Observed |
|---|---|---|---|
| M1 collapse on press (old behaviour) | `BeginGesture`: `input != Typed && IsGroupMember(point)` became `false && ...` | `GroupDrag_PressOnMember_KeepsSelection` | `FAIL ... the press dropped the group` |
| M1 | same | `GroupDrag_Canvas_Trackpad_TwelveSmallMoves_GlyphUnderPointer_GroupRigid` (trackpad: twelve moves of 2.5 px, glyph under the pointer within 2 px, the other members on the same delta) | `FAIL ... the press dropped the group` |
| M1 | same | `GroupDrag_Canvas_ClickCollapses_ContextClickKeepsGroup_DoubleClickRestoresAndFocusesValueRow` | `FAIL ... the press collapsed the group` |
| M2 readout shows the request, not the applied move | `RecordGroupFrame`: `GestureApplied = (target.Span, target.Aft)` and the inspector delta from `target.Aft` | `GroupDrag_HoldsRigid_GrabNotTheBinder_AppliedNotRequested` | `FAIL ... applied -0.08000000000000002` (the 80 mm request, not the 115 mm applied) |
| M3 context click drops the group | `PlanCanvas.OpenPointMenu`: the `IsGroupMember` guard removed | `GroupDrag_Canvas_ClickCollapses_ContextClickKeepsGroup_DoubleClickRestoresAndFocusesValueRow` | `FAIL ... a context click dropped the group` |

M1 is the old product behaviour (a press replaces the selection), so the trackpad check is red on the old behaviour and green on
the build. Green: all of the above print `PASS` in the full ring (`tools/run-tests.sh`).

# Repair cycle 1 (marine-CAD BLOCK): red on the old src, green on the fix (2026-10-06)

Method: `git stash push -- src`, build, run the check (`--readiness`, `CFD_TEST_ONLY=<check>`), `git stash pop`, build, run again.

| Fix | Check | Red (old code) | Green |
|---|---|---|---|
| 1 Plan Δ from the press, span term in the inspector | `GroupDrag_Plan_ReadoutDelta_EqualsTheAppliedMove_SpanAndAft` | `FAIL ... the Plan readout is '... Δ from root 0.00 mm · Δ aft 0.00 mm', wanted ... 'Δ from root +59.87 mm · Δ aft +23.95 mm'` | `PASS` |
| 3 twist domain hold names the point | `GroupDrag_Elevation_TwistDomainHold_NamesThePointAndTheReason` | `FAIL ... no domain binder:  probe=Twist · point 3 of 7 ... · Δ twist +1.30° · Twist is limited to ±57.30° ...` | `PASS` |
| 2, 8 typed amounts carry unit and 2 decimals, G10 names "point n" | Core `ApplyGroupValue_TwistSpanMoveBy_NamesTheUnitAndThePointNumber_UseAmountIsAdmitted` (`--` default Core ring) | `FAIL ... Moving these points by 0.2 would pass point cv-4. The most they can move that way is 0.089.` | `PASS`; window check `Properties_MultiplePoints_TwistMoveBySpan_RefusalHasUnitAndPointNumber_UseEqualsTheAllowedAmount` `PASS` (Use offer equals the message's amount; the amount named is cut, not rounded, so it is admitted) |
| 7 stale refusal cleared when a gesture starts (stale-clear block disabled with `if (false && ...)`) | `Properties_MultiplePoints_TwistRangeRefusal_NamesPointAndRange_ClearsWhenADragStarts` | `FAIL ... the refused text stayed in the row during a drag: -200` | `PASS` (also asserts the range refusal reads COPY-400 through `GroupCopy` "G13"; Core `ApplyGroupValue_TwistSetToOutOfDomain_ReportsThePointAndTheRangeAsData` `PASS`) |
| 4 compact inline Set to / Move by switch in the value row, "move by" tag on From root, no Entry row | `Properties_MultiplePoints_EntrySwitch_InlineInTheValueRow_FromRootCarriesMoveByTag_KeyboardWorks` | Compile-red only on the old src: `GroupCopy.SetToNotOffered` and `MoveByTag` do not exist there (the old build also had the separate `Label_p_mode` row the check forbids). No behavioural red was observed. | `PASS` |
| 5, 6 applied value first, no one-frame lag | `GroupDrag_Elevation_TwistPointsMoveAsOneGroup_ReadoutShowsTheAppliedMove` | `FAIL ... the readout is 'Twist · point 3 of 7 ... · Applied +0.91°', wanted it to start 'Applied +1.01°'` (one frame behind) | `PASS` |

# Repair cycle 2 (marine-CAD CLEAR WITH CONDITIONS): red on the old src (`git stash push -- src`), green on the fix

| Finding | Check | Red (old code) | Green |
|---|---|---|---|
| 1 Plan readout outlives its gesture | `GroupDrag_Plan_ReadoutDelta_EqualsTheAppliedMove_SpanAndAft` (after release, after Escape, after undo: no Δ, chord is the model's) | `FAIL ... after release: the readout keeps a Δ: Δ from root +59.87 mm · Δ aft +23.95 mm · η 0.633 ...` | `PASS` |
| 2 Ruling 120 bounds with units | `Properties_MultiplePoints_TwistRangeRefusal_NamesPointAndRange_ClearsWhenADragStarts` | `FAIL ... refusal: Point 3 would leave its allowed range (−57.30 to 57.30 °).` | `PASS` ("(−57.30° to 57.30°)") |
| 3 domain hold strip is a warning | `GroupDrag_Elevation_TwistDomainHold_NamesThePointAndTheReason` | `FAIL ... the strip shows 'Moving 2 twist points.' as Info, not the hold's warning` | `PASS` |
| 4 U+2212 in refusal and Use | `Properties_MultiplePoints_RefusalKeepsText_UseNeverAutomatic` | `FAIL ... the refusal's amount has no U+2212: Moving these points by -200.00 mm ...` | `PASS` (also "Use −115.00 mm") |
| 5 no cut-off readouts | Plan readout wraps in rows of three; asserted by the Plan check's tail test and the captures | not separately red | captures opened |
