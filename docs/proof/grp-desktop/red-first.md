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
