---
id: design-group-move-node-m
title: "Proposal: group move and typed value for several points (node M, OI-3)"
type: design
status: proposed
owner: "@timianmalloo"
phase: design — operator sees the mockup before any build (memory rule); track E4, round oct05
tags: [desktop, core, cad, group-move, typed-value, node-m, oi-3, ruling-96, ruling-93, proposal, operator-show]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: depends-on }
  - { to: design-m12b-points, rel: refines }
  - { to: design-next-cad-increment, rel: refines }
  - { to: mockup-group-move-node-m, rel: relates-to }
  - { to: design-language, rel: depends-on }
review-by: 2027-04-01
amendments: Ruling 107 (decisions), Ruling 111 (findings 5, 6, 9, 10), marine-CAD Adversary findings 1-8 folded in by track GRP, round oct06
summary: >-
  Design for the CAD increment after the limit hold: select several points on one curve, drag them as one rigid gesture and
  one undo step, nudge them, and type one value for all of them (set-all, move-by, or both per row). A group that includes
  an end vertex holds as a whole at the Ruling 93 limit (Ruling 96). Closes spec node M's typed clause (AM-1.7-15), the
  multi-point half of CAD-04 and the several-vertices clause of GEO-05 (OI-3). Nine decision requests; the mockup shows
  the hold variants, the typed-value variants and the hard states.
---

# Proposal: group move and typed value for several points

- **Track / tier:** E4 of round oct05, T1, docs and mockup only. No `src/` or `tests/` change. Code read at `bc5471e6`.
- **Round oct06 amendment (track GRP):** the operator's decisions are Ruling 107 and Ruling 111. The marine-CAD Adversary
  findings 1-8 are folded in below, each marked **(finding N)**. Findings 9 and 10 are decided by Ruling 111 and folded in
  (§3.2, §3.6). Only finding 11 (small copy gaps) stays open, with the token wording in §5a (proposed, awaiting the operator).
  Scope is **all views** (Ruling 111 item 6): Plan and the elevation channels.
- **Mockup:** [`docs/mockups/group-move-node-m.html`](../mockups/group-move-node-m.html). The operator chooses variants and
  approves copy there before any build track.
- **Order:** this is candidate #2 of [`next-cad-increment.md`](next-cad-increment.md) and follows the limit increment
  (Ruling 96, DR-LIM-5). At the code read above the limit clamp is **not on main** (a search of `src/` for `TipMin`,
  `RootMax` and a clamp reason finds nothing). Every statement about the group hold below assumes that increment is built first.
- **Lens applied:** the `marine-cad-ux-expert` standard, read by the author and not convened. That is a self-review and does
  not clear its Soft veto. A real Adversary pass is a condition (DR-GM-9 note).

## 1. The problem

A designer can select several points today (Shift-click extends, Cmd-click on macOS and Ctrl-click elsewhere toggles:
`PlanCanvas.cs:363-374`, `SelectPoint` at `:210-227`) and then can do nothing with the selection:

1. **Properties is read-only for a selection of two or more points.** `SeveralRows` (`PropertiesView.cs:707-722`) writes
   `Mixed` into From root and the value row, a read-only Type, and the banner "Select one point to change it." The test
   `Properties_MultiplePoints_MixedReadOnly` (`ShellWindowTests.cs:2625`) asserts exactly that. It writes `Mixed` even when
   the values are equal, so the spec's "shared values, Mixed where they differ" is not met either.
2. **A press on a selected point throws the selection away.** A plain press calls `SelectPoint(reference, false, false)`,
   which replaces the selection with the pressed point, then starts a one-point gesture (`PlanCanvas.cs:366-373`).
3. **The gesture core is one-point.** `BeginPointGesture(draftId, curve, vertexId)` and
   `UpdatePointGesture(draftId, generation, span, ordinate)` (`AuthoringSession.cs:254-257`) take one grabbed vertex. Inside
   `UpdatePointGestureCore` a `moved` dictionary of indices is built from the grabbed point plus its fixed companions (an
   anchor's two handles, the root mirror point) at `:552-670`. The dictionary is **internal**; the public method does not
   accept several vertices. The spacing clamp already shifts the whole `moved` set as a unit (`shiftMin`, `shiftMax`,
   `:639-654`), so the clamp style generalises. `GestureFrame.MovedIds` already reports the moved ids (`:58`).

Spec text that stands unbuilt: node M of flow F11, "Properties: shared values, Mixed where they differ; a typed value sets
every point" (spec :2071), with the note that it "is not built in M1.2 ... The requirement stands" (spec :2125-2127,
AM-1.7-15). M1.2b OI-3 is the row that records it (`m12b-points.md:118`, `:1045`).

## 2. Rows and clauses this closes

| Row or clause | What closes | Evidence now | Evidence after |
|---|---|---|---|
| Spec F11 node M, typed clause (spec :2071, :2125; AM-1.7-15) | A typed value applies to every selected point | `Properties_MultiplePoints_MixedReadOnly` asserts read-only | the group typed tests in §9 |
| M1.2b OI-3 (`m12b-points.md:118`, `:1045`) | A multi-point draft exists | "No slice yet" | `BeginGroupGesture` and the group tests |
| CAD-04 (spec :1233; one draft, undo step) | A drag or nudge of several vertices is one gesture and one undo step; a locked member refuses and names its lock | single point only (`Controller_DragPoint_OneUndoStepUndoExact`, `ControllerShellTests.cs:624`) | group gesture tests |
| GEO-05 (spec :1183) | "Drag, nudge, or type a unit expression" works for a selection; the resolved value is echoed | one vertex | typed and nudge tests |
| CAD-03 (spec :1232) | A numeric change is echoed in inspector, strip and canvas within the edit budget | one vertex | group typed echo test |
| CAD-17 (spec :917) | The Wing estimates change during a group drag, before release | one vertex | the group drag test reads the Wing block |
| CAD-16 / Ruling 93 / Ruling 96 | A group with an end vertex cannot end in `DSL-TIP-CHORD-MIN` | no group | `GroupGesture_ReleaseNeverThrowsTipChordMin` |

The inventory of `next-cad-increment.md` marks node M "Not built"; this flips it to Built. It is the only row in the CAD
set that flips from a bounded S-M change.

## 3. Behaviour

### 3.1 Which points move together (DR-GM-1)

The draft is bound to one curve today (`draft.Rail`, `AuthoringSession.cs:529-540`). The first slice therefore moves a
selection that lies on **one curve**, in the Plan view and in the elevations (Ruling 111, item 6; finding 6). A selection on two curves (for example one leading-edge and one trailing-edge point)
does not drag; the strip says so (COPY-G7). The case that matters most to a foil designer, sweeping the tip by moving both
tip vertices, needs a two-rail draft and is DR-GM-1 option B. It is a larger Core change (a draft over two rails, and the
chord rule reads both). Recommendation: A for this slice, B as the next one.

### 3.2 Selecting and pressing (DR-GM-4)

- Shift-click extends, Cmd-click (macOS) or Ctrl-click toggles, as built. Space and Shift+Space select by keyboard (D-1).
- **A press on a point that is already in a multi-selection keeps the selection and drags all of it.** A click that ends
  without movement collapses the selection to that point **on release**. A press on an unselected point replaces the
  selection, as today. (Recommended. Option B keeps today's collapse and offers no group drag without a new modifier.)
- The grabbed point is the one under the pointer. Every other member moves by the same delta (a translation).
- **Root mate (finding 5, Ruling 111 item 5).** A selection that holds point 1 but not the root **seeds the root into the
  group**, like a handle riding with its anchor (`AuthoringSession.cs:600-601` is the root-mirror coupling). The root's span
  lock then holds the group's span delta at zero (DR-GM-5 A); the ordinate delta applies to the root and point 1 together.
  Tests: `{root, 1}` and `{1, 2}`.
- **Modifier matrix, in this order (finding 3):**
  1. Shift at press **extends** the selection and starts no drag (`PlanCanvas.cs:369-375`).
  2. Shift **during** a drag constrains the axis, about the **grabbed** point (`PlanCanvas.cs:425-433`).
  3. A click that ends within the existing 3 px collapses the selection to that point on release
     (`WorkbenchController.cs:1673-1676`).
  4. A context click (right-click, or Ctrl-click on macOS) on a member **keeps the group** (`PlanCanvas.cs:455`).
- **Double-click or Return on a member (finding 10, Ruling 111 item 10).** Focus goes to the group's value row in
  Properties and the group stays selected.

### 3.3 One gesture, one undo step

A press-drag-release of a group opens one draft, updates it per frame, and commits it as one undo step, as a single-point
drag does (CAD-04, DR-6). Escape cancels with no undo step and keeps the selection. The status strip says "Moving 3
trailing edge points." while dragging and "Moved 3 trailing edge points. Tip chord 5.00 mm." after release (COPY-G1, G2).
Undo restores every point exactly.

**What is shown while held (findings 1 and 2).** The strip and the inspector show the **applied** group delta and the
binding point, not the pointer minus the press origin (`PlanCanvas.cs:437-442` reads the pointer today). Only the tether
shows the requested delta. The tether always runs from the **grabbed** point to the pointer. A **warn outline** (not a ring)
goes on the binding member or neighbour. The axis lock draws no warn marker. The mockup frame D (grab point 9, tip 10
binds) shows it.

### 3.4 Rigid body for every limit (DR-GM-3)

The group translates as one body. Every limit is written as a bound on the **delta**, and the delta is clamped once:

| Limit | Source in Core | How it bounds the delta |
|---|---|---|
| Tip chord at least the Ruling 93 minimum (end vertex in the group) | `TipChord.Admits`, the same oracle as the single-point clamp | lower bound on the aft delta |
| Root chord at most 50 times the tip when above 250 mm (root vertex in the group) | same | upper bound on the aft delta |
| Minimum gap to a neighbour that is **not** selected | the `shiftMin` / `shiftMax` rule, `:639-654` | bounds on the span delta |
| Channel domain (thickness, twist) | `ClampGrowing`, `:683` today per point | today it clamps each point alone; for a group it must clamp the delta so the group does not deform |
| Axis freedom (a `ValueOnly` or `SpanOnly` member) | `PointFreedom`, `:569-571` | that axis delta is zero for the whole group (DR-GM-5 A) |

The result is the intersection of intervals, so any limit that binds holds the whole group, as Ruling 96 requires for the
tip. The warn outline on the binding point and the status line say which point and which limit. This is the Ruling 96 sentence
"a group including an end vertex must hold the whole group at the limit" generalised to every limit, so the group never
changes shape under the pointer. Option B (only the limited point holds) is drawn in the mockup to show what it does to the
shape: in the example the 3 mm gap between points 9 and 10 becomes 0 mm.

### 3.5 Nudge

Arrow keys with a multi-selection move every selected point by one step, from the focused point's position (the nudge run
already steps from the draft, not the pointer: `WorkbenchController.cs:1651`). The ladder is the channel's own: 0.01 / 0.1 / 1 mm on lengths, ° on twist, % on t/c
(`WorkbenchController.cs:1703`); 0.1 plain, 0.01 with Command on macOS or Ctrl on Windows, 1 with Shift (COPY-163, finding 7).
Up moves toward the leading edge on every screen, as in the mockup (finding 7). The run
ends on key-up as one undo step. A limit holds the run; the strip speaks on arrival and again only when the limit changes or
a nudge frees the hold (Ruling 96). Presses past the limit do not bank steps (mockup screen 4).

### 3.6 Typed value for several points (DR-GM-2)

The Properties pane for a selection of two or more points on one curve shows the **Point** group with these rows:

- **Type.** The common type, or `Mixed`. Read-only, as built.
- **From root** and the **value row** (Aft for the rails, or the curve's own label). A **shared value is shown as a value**;
  a differing one shows `Mixed` as the placeholder and a one-line range ("Range 30.0 to 36.0 mm.").
- Entry modes. The mockup draws three variants:
  - **A set-all only.** Typing gives every point that value. From root is read-only with a reason, because points cannot
    share a position along the span.
  - **B move-by only.** Typing gives an amount; every point moves by it.
  - **C both, per row (recommended).** A two-way switch **Set to | Move by** sits beside each value row. The value row
    defaults to Set to. From root offers only Move by.
- **Typed values act on anchors and plain points only (finding 4).** A handle rides with its anchor's delta and is never
  seeded twice (`AuthoringSession.cs:602-613`); a selection of an anchor and its handle moves the handle once.
- **After a commit (finding 9, Ruling 111 item 9):** the Move by field clears to 0 and the row re-reads its shared or Mixed
  value. The Set to / Move by mode **resets to the row default when the selection changes** and **stays while the selection
  is kept**.
- Why a switch and not a sign convention: the value rows take negative numbers (anhedral, twist), so "−2" cannot mean
  "move by". The mode is visible and the number is never guessed.
- Units and expressions work as for one point (mm, m, in, °, %, chord; `#root_chord * 0.5`). The resolved value is echoed:
  "Set aft of 3 points to 30.00 mm." or "Moved 3 points by +5.00 mm." (COPY-G11, G12).
- One typed entry is one undo step and no preview, as CAD-16 states for typed dimensions.
- Typing is **atomic**: if any point would violate a rule, nothing is applied.

### 3.7 Refusal states

Typed entry refuses and never rewrites the typed text (Ruling 96). The message names the cause, and a **Use** action offers
the nearest allowed value and never applies itself:

| Typed | Cause | Message | Action |
|---|---|---|---|
| Set to 3 with the tip in the group | tip chord under the minimum | COPY-A (Ruling 96): "Tip chord can't go below 5 mm (the larger of 5 mm and 2 % of the root chord). Enter 5 mm or more." | Use 5 mm |
| Move by −28 with the tip in the group | same, expressed as an amount | COPY-G8: "Moving these points by −28 mm would take the tip chord below 5 mm. The most they can move that way is 25 mm." | Use −25 mm |
| Set to on From root (variant A) | points would coincide | COPY-G9: "Points can't share a position along the span. Move them by an amount instead." | none |
| Move by on From root with the tip (span-locked) in the group | locked axis | existing lock refusal text naming the point (CAD-04 "lock refusals") | none |
| Move by that passes an unselected neighbour | spacing | COPY-G10: "Moving these points by +12 mm would pass point 7. The most they can move that way is 8 mm." | Use 8 mm |

## 4. Hard states

| State | What the designer sees | Why |
|---|---|---|
| Mixed types (an anchor and a control point) | Type `Mixed` read-only; value rows still edit, with the Set to / Move by switch (finding 8; mockup screen 5) | the value rows act on positions, not types; the type change stays one point at a time (CAD-15) |
| Point 1 selected without the root | the root joins the group; the span delta is zero (finding 5) | §3.2 root mate |
| Selection on two curves | rows read-only, drag does not start, strip COPY-G7 | the draft is bound to one curve (DR-GM-1 A) |
| A fully locked member | move refused before a draft opens, strip names the point (COPY-G3) | CAD-04 lock refusal |
| A span-locked or value-locked member (root, tip) | the group holds on that axis and moves on the other; one-line note (COPY-G4) | DR-GM-5 A; the root-mirror coupling still applies to the root end |
| A handle selected without its anchor | the group move does not start (COPY-G5) | a lone handle rotates around its anchor, which is not a translation; DR-GM-6 A |
| An anchor in the group | its two handles move with it, as for one anchor | existing rule `:582-586` |
| Held by a neighbour | whole group stops, strip names the neighbour (COPY-G6) | §3.4 |
| Escape | cancel, no undo step, selection kept | CAD-03 |
| Analysis mode | selecting works as built; drag and typed entry are inert; no marker | ANA-EDIT-INERT, `RefuseAnalysisEdit`, `WorkbenchController.cs:388` |
| Legacy file with the tip already under the minimum | limit is the chord at press; the group holds there going down and moves up freely; COPY-E | `TipChord.Admits` |
| One point selected | unchanged: editable fields, one point | no regression |
| Keyboard | nudge holds, no banked steps, one announcement; focus stays on the focused point; the other members are not announced one by one | GEO-05; a11y floors kept, proof deferred per the a11y-priority memory |
| Reduced motion, dark theme, 560 px | markers appear without animation; tokens only; the mockup checks contrast and overflow | design language |

## 5. Copy (needs the operator's words; COPY-A to F are Ruling 96's)

| Id | Where | Text |
|---|---|---|
| COPY-G1 | strip while dragging a group | Moving `<n>` `<curve>` points. |
| COPY-G2 | strip after release | Moved `<n>` `<curve>` points. Tip chord `<value>` mm. *(tip clause only when an end vertex moved)* |
| COPY-G3 | locked member | `<Point name>` is locked. Deselect it to move the others. |
| COPY-G4 | locked axis | The `<point>` can't move along the span, so the selection moves aft only. |
| COPY-G5 | handle without anchor | Handles move on their own, or with their anchor. Deselect the handle or select its anchor. |
| COPY-G6 | held by a neighbour | The selection is held by point `<n>`. Points can't close up on a neighbour. |
| COPY-G7 | selection on two curves | Select points on one curve to move them together. |
| COPY-G8 | typed move-by past the tip limit | Moving these points by `<typed>` would take the tip chord below `<min>`. The most they can move that way is `<amount>`. |
| COPY-G9 | set-all on From root | Points can't share a position along the span. Move them by an amount instead. |
| COPY-G10 | typed move-by past a neighbour | Moving these points by `<typed>` would pass point `<n>`. The most they can move that way is `<amount>`. |
| COPY-G11 | typed echo, set | Set `<row>` of `<n>` points to `<value>`. |
| COPY-G12 | typed echo, move | Moved `<n>` points by `<signed value>`. |

The mockup draws COPY-G1 to G12 as written. The strings "Mixed" and "Select one point to change it." stay for the cases
that remain read-only (two curves).

## 5a. Tokenised wording for all views (proposed — awaiting operator)

Ruling 111 item 6 makes group moves work in the Plan view and the elevations. COPY-G1, G2, G4 and G11 (COPY-281, 282, 284,
291) name "aft", "tip chord" or a millimetre unit, and the nudge ladder is in °, % or mm. **Proposed — awaiting operator**;
`DESIGN.md` is not edited here. The tokens are:

- `<curve>`: the curve noun, one of "leading edge", "trailing edge", "dihedral", "twist", "thickness" (the Points pane's name
  for the curve; COPY-G1 already carries it).
- `<axis>`: the value row's own label for the curve: "aft" (leading and trailing rails), "dihedral", "twist", "thickness".
- `<unit>`: the channel's unit: mm, ° or %.
- `<end-chord>`: "Tip chord" or "Root chord", shown only when a leading or trailing end vertex moved.

| Id | Where | Proposed text |
|---|---|---|
| COPY-G1 | strip while dragging | Moving `<n>` `<curve>` points. *(unchanged; `<curve>` now ranges over every channel)* |
| COPY-G2 | strip after release | Moved `<n>` `<curve>` points.` <end-chord> <value> mm.` *(clause only on the leading or trailing rail and only when an end vertex moved; none on an elevation channel)* |
| COPY-G4 | locked axis | The `<point>` can't move along the span, so the selection moves in `<axis>` only. *(on a channel with no span freedom: "The `<point>` can't change, so the selection holds.")* |
| COPY-G11 | typed echo, set | Set `<axis>` of `<n>` points to `<value> <unit>`. |
| COPY-G12 | typed echo, move | Moved `<n>` points by `<signed value> <unit>`. |
| COPY-G8, G10 | typed refusal | `<amount>` and `<typed>` carry `<unit>`; G8 is a Plan-only row (the tip chord exists on the rails only). |
| COPY-163 | nudge help | Add "Ctrl on Windows" beside Command on the group ladder's label (finding 7). |

**Finding 11 (small copy gaps)** travels with this table: the operator rules on it when ruling on the token wording.
`<end-chord>` for the root is a new string (the root tip's mirror is COPY-246's refusal, not a status line).

## 6. Where it lives

- **Core:** `BeginGroupGesture(draftId, curve, vertexIds)` and `UpdateGroupGesture(draftId, generation, grabbedId, span,
  ordinate)`, built from the same `UpdatePointGestureCore` body (the `moved` dictionary is seeded by every member and its
  companions instead of one; the delta is clamped once as in §3.4). The single-point methods become the one-member case, so
  there is one clamp path, not two. A new typed command applies a set-all or move-by to several vertices atomically through
  the same patch and admission path the typed single-point entry uses. Rule A holds: geometry rules stay in Core, and
  `TipChord.Admits` stays the only oracle for the tip. The Desktop-facing signatures (`BeginPointGesture`,
  `UpdatePointGesture`, `Selection`) are unchanged; the group is an overload pair beside them.
- **Desktop:** `BeginGesture` takes the selection; `SelectPoint` on a press of a member keeps it; `SeveralRows` computes
  shared values and builds editable rows with the per-row mode; nudge uses the selection; the strip strings are §5.
- **Instrumentation:** `gesture.end` gains `members` (count) and the clamp reason names the binding point; typed entry
  records `members` and the mode. Operator questions: how often is a group dragged, which limit binds, and is any group
  gesture still refused at release (target zero; non-zero is a defect signal).

Root seeding (finding 5, Ruling 111): the group's seed set gains the root whenever point 1 is a member and the curve has
the `root_mirror` lock, and the `{root, 1}` and `{1, 2}` cases are tests (§9).

assume: a group translation is expressible as a single delta on the existing `moved` set, because the current companion
rules (anchor with handles, root mirror) are themselves translations of a seeded set. Confirm: a Core test that drags a
one-member group and asserts byte equality with the single-point gesture across a sweep. If false: the group needs its own
update body and a second clamp path, which the Simplifier should object to.

assume: only the end vertices of a rail change root and tip chord (the `assume:` of the limit proposal). Confirm: the same
interior sweep test, now with group moves. If false: release still refuses and telemetry shows it.

## 6a. Open items

- **Domain hold:** closed. A group held by the twist or thickness domain names the binding point ("held by point n") and the
  existing clamp reason in the readout, the inspector and the strip (with the hold icon). The warn outline on the binding
  point is drawn for a domain hold by `ElevationView.DrawGroupHold` (the `GestureBinding` rectangle, the same as for a neighbour hold).
- **Finding 11 (small copy gaps):** open, with §5a. Findings 9 and 10 are decided (Ruling 111) and folded in at §3.2, §3.6.

## 7. What this does not do

- No group move across two curves (DR-GM-1 B is the next slice). Elevation channels are **in** this slice (Ruling 111
  item 6), one curve at a time.
- No marquee or select-all on a curve. Selecting is Shift/Cmd-click and Space, as built. A marquee is a next step to propose
  if the operator finds selecting three points tedious.
- No rotate, scale or align of a group; translation only.
- No change to the minimum tip chord rule, `Commit`'s check (stays the backstop), the file schema, or analysis.
- No multi-point type change (Anchor or Control) and no multi-point Delete or tangent change.
- No group handle move (DR-GM-6 A refuses).
- Spec amendments are DR-GM-8, and the spec is the operator's.

## 8. Risks

- **Selection collapse change.** DR-GM-4 A changes what a press on a selected point does. Mitigation: collapse on release
  for a click, so selecting one of several by clicking still works; tests in §9.
- **Rigid hold reads as "stuck".** The group stops while the pointer moves on. Mitigation: ring on the binding point, tether
  and strip, as Ruling 96.
- **Per-point channel clamps today.** The domain clamp is per point (`ClampGrowing`); leaving it per point in a group
  would deform the group silently. Mitigation: a named test and the delta-clamp rule in §3.4.
- **Two-curve demand.** The designer's strongest case, sweeping the tip, is B. Offering only A first may feel incomplete.
  The mockup and DR-GM-1 say so.
- **Convention claim is Flagged.** Rigid hold of a group at a constraint is recalled from sketch-constraint CAD, not cited
  from a manual in `docs/knowledge`. The operator chooses at DR-GM-3.
- **Nothing here was run against the app.** The mockup shows intended states computed from the real rule, not captured from
  the product.

## 9. Test plan sketch (rings stated; sized to the work)

| Test | Ring and cost | Names what it protects |
|---|---|---|
| Core: `GroupGesture_OneMember_EqualsSinglePointGesture` (sweep of deltas, byte equality) | fast, ms | one clamp path, no regression |
| Core: `GroupGesture_Translates_AllMembers_OneDraft` | fast | every member moves by one delta; gaps between members unchanged |
| Core: `GroupGesture_IncludesTipVertex_HoldsWholeGroupAtMinimum` (red first) | fast | Ruling 96 for a group; `TipChord.Admits` agrees on every frame |
| Core: `GroupGesture_IncludesRootEnd_HoldsWholeGroupAtRootMax`, `..._RootBelow250_NeverClamps` | fast | the 2 % branch and the 5 mm branch |
| Core: `GroupGesture_NeighbourSpacing_HoldsWholeGroupRigid` | fast | shift clamp for a group |
| Core: `GroupGesture_ChannelDomain_HoldsGroupNotMember` | fast | the per-point `ClampGrowing` defect |
| Core: `GroupGesture_FixedMember_RefusedNamingLock`, `..._ValueOnlyMember_HoldsSpanForGroup` | fast | CAD-04 lock refusal; DR-GM-5 |
| Core: `GroupGesture_AnchorBringsHandles`, `..._HandleWithoutAnchor_Refused` | fast | DR-GM-6 |
| Core: `GroupGesture_LegacyTipUnderMinimum_HoldsAtPressChord` | fast | `Admits` equivalence |
| Core: `GroupGesture_RootSeeded_WhenPointOneSelected` (`{root, 1}` and `{1, 2}`), `GroupGesture_HandleNeverSeededTwice` | fast | finding 5 (Ruling 111); finding 4 |
| Core: `GroupGesture_End_CarriesMembers` | fast | the `gesture.end` instrumentation floor |
| Core: `GroupGesture_ReleaseNeverThrowsTipChordMin` (sweep over groups including interior vertices) | fast | confirms both `assume:` lines |
| Core: `ApplyGroupValue_SetTo_AllEqual`, `..._MoveBy_AllShifted`, `..._AnyViolation_NothingApplied`, `..._MoveByPastTip_NamesMaxAmount` | fast | atomic typed entry and COPY-G8 |
| Desktop: `Properties_MultiplePoints_SharedValueShown_MixedWhereDiffer` (replaces `Properties_MultiplePoints_MixedReadOnly`; red first) | Desktop ring, one check | the spec's shared-or-Mixed rule |
| Desktop: `Properties_MultiplePoints_TypedSetTo_OneUndoStep`, `..._TypedMoveBy_OneUndoStep`, `..._RefusalKeepsText_UseNeverAutomatic` | Desktop ring | node M typed clause |
| Desktop: `GroupDrag_PressOnMember_KeepsSelection`, `GroupDrag_ClickWithoutDrag_CollapsesOnRelease` | Desktop ring | DR-GM-4 |
| Desktop: `GroupDrag_OneUndoStep_EscapeNone_AnalysisInert` | Desktop ring | CAD-04, ANA-EDIT-INERT |
| Desktop: `GroupNudge_TenPressesPastLimit_OneAnnouncement_OneUndoRow` | Desktop ring | no banked steps; live-region noise |
| Core: `ApplyGroupValue_Handles_RideWithAnchor_NeverSeededTwice`, `..._SetTo_PlainPointsAndAnchors` | fast | finding 4 |
| Mutants (Core, planted in `GroupGestureTests`): clamp per member instead of rigid; a handle seeded twice | with the suite | tests that pass but prove nothing |
| Mutants: clamp each member alone; drop one limit from the delta intersection; read a second tip formula; select-collapse on press | per the repo's mutant practice | tests that pass but prove nothing |
| Telemetry: `gesture.end` carries `members` and the binding reason | fast | instrumentation floor |

Costs are stated at the join (Test rings in `AGENTS.md`). The Core tests are ms-scale and join the 60 s budget of
`tools/run-tests.sh`; the Desktop checks add one drag, one nudge and three Properties checks to the Desktop ring, not to
the fast ring.

## 10. Decision requests for the operator

| Id | Question | Options | Recommendation |
|---|---|---|---|
| DR-GM-1 | Which points can move together in this slice? | **A** one curve at a time; **B** the two planform rails together (sweep the tip as one move); **C** any selection | **A** first, **B** as the next slice (needs a two-rail draft) |
| DR-GM-2 | Typed value for several points | **A** set-all only; **B** move-by only; **C** both, a switch per row, value rows default Set to, From root Move by only | **C** |
| DR-GM-3 | How does a group hold at a limit? | **A** the whole group as one rigid body for every limit; **B** only the limited point holds | **A** (Ruling 96 already says so for the tip; this extends it to neighbours and channel limits) |
| DR-GM-4 | Press on a point that is already selected | **A** drags the whole selection, a click collapses to that point on release; **B** keeps today (collapse on press) | **A** |
| DR-GM-5 | A selected point cannot move on one axis (root or tip span) | **A** the group holds on that axis and moves on the other; **B** refuse the whole move; **C** leave that point behind | **A** |
| DR-GM-6 | A handle selected without its anchor | **A** refuse the group move and say why; **B** leave the handle out; **C** move it anyway | **A** |
| DR-GM-7 | Copy: approve COPY-G1 to COPY-G12 | approve / amend | approve |
| DR-GM-8 | Spec: retire the AM-1.7-15 note under F11 node M and add one clause under CAD-04: "A drag, nudge or typed value of several points on one curve is one draft and one undo step; a limit holds the whole group; the typed entry refuses and never rewrites." | approve / amend | approve |
| DR-GM-9 | Order after the limit increment | **A** this next; **B** comb scale and monotone count (#3) first; **C** this, then #3. A real marine-cad-ux-expert Adversary pass before build: yes / no | **C**; Adversary pass **yes** |

If the operator is offline, items DR-GM-1, 3, 4, 5, 6 and 9 are non-spec and a Fable owner may rule them (memory: Fable
owner when offline). DR-GM-7 (copy) and DR-GM-8 (spec) wait for the operator.
