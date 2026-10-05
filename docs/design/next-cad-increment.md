---
id: design-next-cad-increment
title: "Proposal: the next CAD increment — planform limits felt during the gesture (drag holds at the minimum tip chord; root-widen refusal says what to do)"
type: design
status: proposed
owner: "@timianmalloo"
phase: design — operator sees the mockup before any build (memory rule); track E3, round oct05
tags: [desktop, core, cad, planform, tip-chord, gesture, clamp, copy, ruling-93, ruling-94, proposal, operator-show]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: depends-on }
  - { to: design-m12b-points, rel: refines }
  - { to: design-planform-point-verbs, rel: relates-to }
  - { to: design-language, rel: depends-on }
  - { to: mockup-cad-limits-in-gesture, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Inventory of the CAD-* and GEO-* rows against main, a ranked list of candidate CAD increments for one foil designer, and
  one pick: make the Ruling 93 minimum tip chord a limit the drag holds at during the gesture (instead of a refusal at
  release), with a visible limit marker and readout, the same hold under keyboard nudge, and a root-chord refusal that says
  "widen the tip first". Core change is one clamp in the point-gesture frame; no new row flips to built, so the proposal
  also names the next two candidates that do (group move and typed value for several points, then comb scale and the
  monotone count). Mockup shows today's behaviour, three drag variants side by side, and the hard states.
---

# Proposal: planform limits felt during the gesture

- **Track / tier:** E3 of round oct05, T1, docs and mockup only. No `src/` or `tests/` change. Code read at `33a3f31`.
- **Mockup:** [`docs/mockups/cad-limits-in-gesture.html`](../mockups/cad-limits-in-gesture.html). The operator chooses a drag
  variant and approves copy there before any build track (memory rule: the operator sees a mockup before a UI build).
- **Lens applied:** the `marine-cad-ux-expert` standard (`.claude/agents/marine-cad-ux-expert.md`), read and applied by the
  author, not convened. That is a self-review: it does not clear the Soft veto (the author never clears its own veto).
  A real Adversary pass is a condition in §9.

## 1. The problem

Ruling 93 (today) made a finite tip chord of at least max(5 mm, 2 % of the root chord) a design rule. It is enforced in one
place: `AuthoringSession.Commit` through `RequireTipChord` (`src/CfdWorkbench.Core/AuthoringSession.cs:500-512`). That is
the right place for the rule. It is the wrong place for the *feel* of the rule:

1. **A drag is free, then refused at release.** `UpdatePointGesture` clamps spacing, handle bounds and channel domains
   frame by frame (`AuthoringSession.cs:602-673`, `ClampGrowing` at `:683`) but has no tip clamp. While the designer drags the
   trailing-edge tip vertex toward the leading edge, the Wing block shows a tip chord of 3 mm, 2 mm, 1 mm
   (`WorkbenchController.FlushGestureFrame` reads the draft bytes, `WorkbenchController.cs:1668`). On release,
   `Apply` throws `DSL-TIP-CHORD-MIN` and the work snaps back (`TipChordTests.cs:22`,
   "TipChord_Drag_BelowMinimum_RefusedNothingAccepted"). The designer did a legal-looking gesture and lost it.
2. **A root edit gets a tip message.** When a root-chord edit pushes the tip under 2 % of the new root, the refusal is built
   with the *new root* as its argument (`RefusalReason(newRoot)`, `:507`) and reads "Tip chord can't go below 8 mm (the
   larger of 5 mm and 2 % of the root chord)." The designer edited the root. The message names the tip, and says nothing
   about what to do (`TipChordTests.cs` "TipChord_RootChordRaise_ThatPushesTipUnder_Refused" asserts the code only).

Both are fresh: they exist because Ruling 93 landed in this round. The existing live advisory for edge crossing
(`GestureCrossing`, `WorkbenchController.cs:1671`) shows the product already treats "this gesture is going somewhere illegal"
as something to show *during* the drag.

## 2. Inventory: CAD-* and GEO-* rows on main

Status is **Built** (code and a named test read), **Partly** (some clauses), **Not built** (no code found by search), or
**Superseded** (the spec says so). "Search" means a grep of `src/` and `tests/` for the verb; it proves absence of the
name, not of the behaviour, and is labelled so.

| Row | Status | Evidence (file:line or test) |
|---|---|---|
| CAD-01 five master curves | Built | `ElevationTests.cs:119` Elevation_FrontThicknessLane_CaptionedSharedSpanAxis; `Channels` in Core |
| CAD-02 add or remove a station, promote a slice | **Not built** | search: no Add station, Promote or slice verb in `src/`; `CommandTable.cs` has `point.add/remove/rebuild` (curve points, a different verb) |
| CAD-03 productive keyboard and precision | Partly | nudge `ControllerViewTests.cs:268`; expressions `PropertiesViewTests.cs:84`, `ShellWindowTests.cs:2555`; comb scale and station readouts open (OI-5) |
| CAD-04 edit curves in their elevation | Built (single point) | `ControllerShellTests.cs:624` Controller_DragPoint_OneUndoStepUndoExact; multi-point is OI-3 |
| CAD-05, CAD-07, CAD-08 | Superseded | by CAD-20, CAD-21, UX-31 (spec :2867-2870) |
| CAD-06 one camera, presets | Built | `ViewCameraTests.cs:15`; `View3dTests.cs:362` |
| CAD-09 find section editing | Partly | Edit section built (`section.edit`); inspection slice and Promote not built (search) |
| CAD-10 edit the intended scope | Built, one clause unread | Edit shared / Make independent / Keep current thickness in `SectionEditorView.axaml`, `AuthoringSession.cs`; adjacent-blend-interval preview text not read |
| CAD-11 inspect during a draft | Built, unverified here | `ControllerSectionTests.cs` SectionMode_* family (`:347` is Finish); not opened clause by clause |
| CAD-12 compare and decide | **Not built** | search: no Alternative, Keep or Discard |
| CAD-13 state dimension intent (Hold LE or TE) | **Not built** | search: no Hold LE/TE; spans use the quarter-chord rule (CAD-16 1.7) |
| CAD-14 start, open, fail safely | Built | `ShellWindowTests.cs:223`, `:933` |
| CAD-15 point type | Built | `ShellWindowTests.cs:2443` Properties_TypeToAnchor_OneUndoStepCurvePassesThrough |
| CAD-16 type span and chords | Built; root-refusal copy gap | `ControllerShellTests.cs:121`; `TipChordTests.cs` root-raise test; gap in §1 item 2 |
| CAD-17 Wing estimates, live in the drag | Built | `WingEstimates.cs`; `PointVerbTests.cs:313`; live in `FlushGestureFrame` |
| CAD-18 replace from catalog | Built | `CatalogDialogTests.cs`; `SectionReplaceTests.cs` |
| CAD-19 save to My sections | Built | `ControllerSectionTests.cs:634` |
| CAD-20 section edit is a mode | Built | `ControllerSectionTests.cs:347` |
| CAD-21 verbs reachable | Partly | `CommandTable.cs:76-78` point verbs, `section.smooth`, `view.comb`, `view.fit`; Add station, Measure, Fair and Fit points on rails absent (search) |
| GEO-01 generate from drivers | **Not built** | search: no driver or recipe code |
| GEO-02 derived dimensions | Built | `WingEstimates.cs`; CAD-17 tests |
| GEO-03 edit by curve or station | Partly | curve edit built (CAD-04); station-by-η-plot not read |
| GEO-04 drivers and "Direct parametric" | **Not built** | search: no recipe state (follows GEO-01) |
| GEO-05 vertices and levers precisely | Partly | three-step nudge and typed units built (`ControllerViewTests.cs:268`); several vertices at once not built (OI-3); Insert at 16 cap per spec 1.7 |
| GEO-06 add a station without altering shape | **Not built** | same as CAD-02 |
| GEO-07 mix profiles | Partly | Use source thickness and Keep current thickness present (`SectionEditorView.axaml`); A4.6 acceptance not read |
| GEO-08 reshape a predefined section | Built | `SectionReplaceTests.cs`; `ProvenanceTests.cs` |
| GEO-09 units and orientation | Built | `UnitFamily` in `PropertiesView.cs`; `PropertiesView_Formatter_PrecisionFollowsQuantity` |
| GEO-10 inspect the actual loft | Built | `View3dTests.cs:362`; `ViewCameraTests.cs:15` |
| GEO-11 root is continuous | Built | `PropertiesView.cs:150` RootMirrorHandle; `PointGestureTests.cs`; break marker and DRC warning not read |
| GEO-12 ghost and local dimensions | **Not built** (ghost) | search: no Ghost in `src/` |
| GEO-13 a vertex acts locally | Built, unverified here | locality handled in `Geometry.cs`; not opened |
| GEO-14 review and reverse a construction | Built | `RebuildPopover_CancelAndEscape_NoRowBytesAndFreshnessUnchanged`; `RebuildPopover.axaml.cs` |
| GEO-15 fair within a tolerance | Partly | section Fair built (`SectionEdits.cs:352`, monotone count); rails and channels have none (OI-2) |
| Spec node M (F11), "a typed value sets every point" | **Not built** | `ShellWindowTests.cs:2625` Properties_MultiplePoints_MixedReadOnly; `PropertiesView.cs:713` Mixed rows; AM-1.7-15 |
| Comb scale, density, radius readout, monotone count on rails (A4.9, OI-5) | **Not built** | comb is fixed gain (`PlanCanvas.cs:742`); section auto-scale only (`SectionEditorTests.cs:203`) |

Rows closed by this proposal: **none flips from Not built to Built.** It finishes a Partly-built clause family: CAD-04
("lock refusals" for gestures), CAD-16 (the refusal copy), CAD-17 ("the estimates change during the drag" now stay
legal), GEO-05 (echo the resolved value: the held value is echoed). The spec needs one amendment (§8). That is the honest
answer to "which unbuilt rows does it close"; the ranked list (§3) shows what to build if rows matter more than feel.

## 3. Candidates ranked for one foil designer

The user is the single foil designer in `docs/knowledge` and the memory note: one person, one laptop, shapes a hydrofoil
wing and reads what it does. Value is 1-5 for that person's most frequent edit loop; size is S (about a day) / M / L.

| # | Candidate | Rows | Value | Size | Why |
|---|---|---|---|---|---|
| 1 | **Planform limits held during the drag; root refusal copy** | CAD-04, CAD-16, CAD-17, GEO-05 (clauses) | 4 | **S** | Every planform drag ends at the tip. The refusal today loses the gesture. One clamp, one marker, one string. |
| 2 | Group move and typed value for several points | node M (F11), CAD-04, GEO-05 | 3 | S-M | `UpdatePointGesture` already takes a `moved` dictionary of indices (`:602-673`), so the drag half is cheap; the typed half needs a Properties edit on Mixed rows. Closes a spec clause marked "stands". |
| 3 | Comb scale and density, radius readout, monotone-piece count on rails | A4.9 (OI-5), GEO-05 | 3 | S | Diagnostics a Rhino or Shape3d user expects; fairness of the outline is the designer's craft. |
| 4 | Insert anchor and Fair on rails and channels | GEO-15, GEO-05 (OI-2) | 3 | M | Section editor has them; rails do not. |
| 5 | Add station and promote an inspection slice | CAD-02, CAD-09, GEO-06 | 4 | **L** | The lines-plan paradigm. Needs a new Core command under rule A, a slice UI and a loft check. Not the smallest. |
| 6 | Hold LE / Hold TE chord intent | CAD-13 | 3 | M | Depends on the span-policy mapping that CAD-16 1.7 changed. |
| 7 | Ghost of a prior revision, local dimensions | GEO-12 | 2 | M | Useful with alternatives; premature without CAD-12. |
| 8 | Alternatives, Keep and Discard | CAD-12 | 2 | L | Needs Area-3 evidence to compare. |

**Pick: #1.** It has the best value per size and it removes a defect-shaped experience that Ruling 93 created today.
**Next, if the operator prefers rows closed over feel: #2**, then #3. #5 is the biggest user value and the largest build;
it deserves its own design pass, not a slot in this round.

## 4. The increment (what the designer sees)

**Rule being shown** (unchanged, Core): tip chord ≥ max(5 mm, 2 % of root chord) (`TipChord.MinimumMeters`,
`TipChord.cs:28`). Files already under the minimum open and may be edited no further below it (`TipChord.Admits`, `TipChord.cs:46`).

1. **Drag the tip vertex of either planform rail toward the other.** The vertex follows the pointer until the tip chord
   reaches the limit, then **holds**. The pointer keeps moving; the vertex does not. A **limit marker** (a short dashed
   line across the tip at the minimum chord, labelled "Minimum tip chord 5 mm") appears when the vertex is within 5 mm of
   the limit or at it. The Wing block shows the held tip chord with the word "minimum" beside it. The status strip, in
   plain words: "Tip chord is at its minimum, 5 mm." No code is shown.
2. **Release** commits the held value as one undo step. No refusal. Escape cancels with no undo step, as today.
3. **Drag the root vertex outward.** The root chord can grow until the tip is exactly 2 % of it, then holds. The marker
   is at the root: "Root chord is at its maximum, 250 mm, for a 5 mm tip. Widen the tip first." The limit only bites above
   a 250 mm root, because below that the absolute 5 mm governs and the tip is untouched.
4. **Keyboard.** Arrow nudges of a tip vertex stop at the limit; extra presses do not bank steps past it (the nudge run
   already steps from the draft's position, `WorkbenchController.cs:1627`). The strip is a live region and says "Tip
   chord is at its minimum, 5 mm." once, not per key.
5. **Typed entry still refuses, and never rewrites what you typed.** Typing 3 in Tip chord keeps the field, marks it
   invalid and says "Tip chord can't go below 5 mm (the larger of 5 mm and 2 % of the root chord)." (Ruling 94, unchanged),
   and now ends with the allowed range: "Enter 5 mm or more." Typing 400 in Root chord with a 6 mm tip says "Root chord can't
   go above 300 mm while the tip chord is 6 mm (the tip must stay at least 2 % of the root). Widen the tip first." Geometry
   and undo depth are unchanged (CAD-16 clause).

Marine CAD lens (self-applied, Flagged where a convention is recalled and not cited): dragging against a design rule
that is a constraint is held at the constraint in the sketch-constraint family (Fusion, SolidWorks and SolveSpace sketch
drags stop at a solver limit; Flagged, recalled, not read from a manual in `docs/knowledge`, which has no row on this).
Rhino and Alias have no tip-chord rule, so they offer no precedent for either behaviour. Shape3d: not checked. What a
precision-CAD user does expect: a typed value is never silently changed, and a refusal names what is limiting and what
would lift it. Both are met.

## 5. Behaviour contract and where it lives

- **Core:** `UpdatePointGesture` gets one more clamp step, in the same place and style as `ClampGrowing`, for the end vertex
  of a planform rail, using `TipChord.Admits` as the oracle so the clamp and the release check cannot disagree. The
  `GestureFrame` already carries `Clamped`; add a reason (`TipMin`, `RootMax`) so the Desktop can say which limit.
  One definition stays in `TipChord` (E-class: no second definition).
- **Desktop:** `FlushGestureFrame` reads the reason and sets a limit state (like `GestureCrossing`); `PlanCanvas` draws the
  marker; `PropertiesView` appends "minimum" or "maximum" to the held Wing-block row; the status strip reads the strings
  in §7.
- **Legacy file** (tip under the minimum when opened): `Admits` allows an edit that does not take the tip further below it,
  so the limit during a drag is `min(minimum, tip at press)`. The marker says "This tip is already under the minimum.
  It can't go lower." (`TipChordTests.cs` "TipChord_OldFileBelowMinimum_OpensAndEditsUpAreAdmitted").
- **Instrumentation (IO):** the `gesture.end` record already carries `frames` and a clamp counter (`gestureClamped`,
  `WorkbenchController.cs:1666`, `:1884`). Add the clamp reason as a field. Operator questions: how often does a drag hit a tip
  limit; does any drag still end in `DSL-TIP-CHORD-MIN` (the answer should be zero after this increment, and a non-zero
  count is a defect signal).

assume: only the end vertices of the two rails set root and tip chord (a clamped B-spline interpolates its ends; interior
vertices and handles do not change `WingEstimates.Chord(·, 0)` or `(·, 1)`). Confirm: a Core test drags every interior
vertex and handle to its extremes and asserts both chords unchanged. If false: the clamp misses a path and release still
refuses, so the check on release stays as the backstop and a telemetry count above zero shows it.

## 6. Hard states

| State | What the designer sees | Why |
|---|---|---|
| Refusal (typed) | Field invalid, message with the limit and "Widen the tip first" where the root is the cause; nothing changes; undo depth unchanged | CAD-16; Ruling 94 copy; typed numbers are never rewritten |
| Hold (drag) | Vertex stops, marker and "minimum" word, strip line; pointer is free | this proposal |
| Release at the hold | One undo step; Undo restores exactly | CAD-04 |
| Escape in the hold | Cancel, no undo step | CAD-03 |
| Empty (no foil open) | No marker, no Wing block, Start card unchanged | CAD-14, CAD-17 |
| Tip closes (point tip) | Tip chord row is text "Tip closes — edit the tip station" and has no limit | existing `Properties_TipCloses_TipChordIsText` (`ControllerShellTests.cs:379`); Ruling 93 puts tip points out of scope |
| Mixed (several points selected) | Rows read "Mixed", read-only; no marker; drag of a group is not part of this increment | OI-3 unchanged |
| Legacy file under the minimum | Limit is where the tip was at press; message says it is already under | `TipChord.Admits` |
| Analysis area | Edits are inert as built (ANA-EDIT-INERT); no marker | TGL, `RefuseAnalysisEdit` guards, `WorkbenchController.cs:1423` |
| Keyboard | Nudge holds, no banked steps, one announcement; focus stays on the point; the marker text is in the accessible name of the point | GEO-05; a11y floors kept, proof deferred per the a11y-priority memory |
| Reduced motion | The marker appears without animation; nothing else moves | GEO-10 clause |
| Dark theme and 1024 × 700 | Marker and text use tokens; the mockup checks contrast ≥ 4.5:1 | design-language |

## 7. Copy (needs the operator's words; Ruling 94 covers only the first)

| Id | Where | Text |
|---|---|---|
| COPY-A | typed Tip chord or a refused tip gesture | Tip chord can't go below `<min>` (the larger of 5 mm and 2 % of the root chord). *(Ruling 94, kept; append "Enter `<min>` or more." for typed entry)* |
| COPY-B | drag hold, tip | Tip chord is at its minimum, `<min>`. |
| COPY-C | drag hold, root | Root chord is at its maximum, `<max>`, for a `<tip>` tip. Widen the tip first. |
| COPY-D | typed Root chord refused | Root chord can't go above `<max>` while the tip chord is `<tip>` (the tip must stay at least 2 % of the root). Widen the tip first. |
| COPY-E | legacy file | This tip is already under the minimum. It can't go lower. |
| COPY-F | Wing block | `<value>` mm · minimum |

## 8. What this does not do

- No change to the rule, the minimum, or `Commit`'s check (it stays as the backstop).
- No group drag, no typed value for several points (OI-3), no comb scale (OI-5), no station verbs (CAD-02).
- No clamp of any other planform quantity (edge crossing keeps its advisory; a clamp for it is a separate decision).
- No new file field, no schema or FoilDSL change, no analysis change (ANA-TIP-BELOW-FLOOR keeps its text).
- Spec: one amendment line under CAD-04 and the tip-chord paragraph at spec :809: "A drag holds at the tip-chord limit and
  a root-chord limit; the typed entry refuses." This is a spec change, so the operator owns it (memory rule).
- No manufacturing claim: the minimum is a design rule, not a buildability rule (Ruling 93, spec :809).

## 9. Test plan sketch (rings stated; sized to the work)

| Test | Ring and cost | Names what it protects |
|---|---|---|
| Core: drag the tip vertex of each rail below the limit; every frame is admitted by `TipChord.Admits`; `Apply` never throws `DSL-TIP-CHORD-MIN` (red first: today's `TipChord_Drag_BelowMinimum_RefusedNothingAccepted` flips) | fast ring, ms each | clamp and release cannot disagree |
| Core: root vertex drag past 50 × tip holds at the maximum; root at or under 250 mm never clamps | fast ring | the 2 % branch and the 5 mm branch |
| Core: legacy file (tip 2 mm) holds at 2 mm, moves up freely | fast ring | `Admits` equivalence |
| Core: interior vertices and handles never change either chord (confirms the `assume:`) | fast ring | no unclamped path |
| Core: root-edit refusal reason names root and max (COPY-D), tip-edit names tip (COPY-A) | fast ring | the copy defect |
| Desktop: drag past the limit; status shows COPY-B; Wing row shows "minimum"; release adds exactly one undo step; Escape adds none | Desktop ring, one drag check (cost stated at join; Desktop slot count unchanged) | the felt behaviour |
| Desktop: nudge ten presses past the limit; one announcement; one undo row | Desktop ring | no banked steps; live-region noise |
| Mutants: remove the clamp; make the clamp use a second definition of the minimum; drop the reason | per the repo's mutant practice | tests that pass but prove nothing |
| Telemetry: `gesture.end` carries the clamp reason; no `DSL-TIP-CHORD-MIN` after a gesture in the sample run | fast ring | instrumentation floor |

Verification that this note does not claim: nothing here was run against the app. The mockup shows intended states
computed from the real minimum rule, not captured from the product.

## 10. Decision requests for the operator

| Id | Question | Options | Recommendation |
|---|---|---|---|
| DR-LIM-1 | During a tip drag, what should happen at the limit? | **A** hold at the limit (vertex stops, pointer free); **B** drag freely with a warning, release lands at the limit; **C** keep today (free, refuse at release) | **A** |
| DR-LIM-2 | Should the root-chord drag also hold (at 50 × tip)? | A yes (symmetric); B no, refuse at release with COPY-C | **A** |
| DR-LIM-3 | Copy: approve COPY-B to COPY-F (A is Ruling 94, kept)? | approve / amend | approve |
| DR-LIM-4 | Spec amendment under CAD-04 and the tip-chord paragraph (§8) | approve / amend | approve |
| DR-LIM-5 | Build this now (S), or take candidate #2 (rows closed) first? | #1 now / #2 now / #1 then #2 | **#1 then #2** |
| DR-LIM-6 | Should a real marine-cad-ux-expert Adversary pass run before the build starts? | yes / no | yes (the self-applied lens is not a clear) |

## 11. Risks

- **Pointer-point offset.** After a hold, the pointer and the vertex are apart. Release must not jump the vertex to the
  pointer. Mitigation: the commit reads the draft, not the pointer (as `EndGestureAsync` does at `:1721-1759`); a test
  asserts it.
- **Hidden second definition.** A clamp written in Desktop would duplicate the rule. Mitigation: Core only, `Admits` as the
  oracle, a mutant that inlines the formula.
- **Convention claim is Flagged.** The sketch-constraint precedent is recalled. Mitigation: the operator chooses; DR-LIM-1
  offers B and C.
- **Several rails move together.** If a later group drag (OI-3) includes an end vertex, the clamp must hold the group. Out of
  scope here; recorded so #2 does not forget it.
