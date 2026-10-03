---
id: design-planform-point-verbs
title: "Design: planform outline point verbs — Add point, Remove point, Rebuild to N (floor 4 at degree 3)"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — Ruling 62 (operator 2026-10-03); build after M1.2c joins
tags: [desktop, core, cad, planform, rail, channel, point-verbs, insert-cv, delete-cv, rebuild, knot-insertion, knot-removal, foildsl, ruling-62, floor-4]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-foildsl, rel: implements }
  - { to: adr-0001-master-curve-degree, rel: refines }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-0009-cad-first-shell, rel: depends-on }
  - { to: design-m12b-points, rel: refines }
  - { to: design-m12b2-3d-elevations, rel: depends-on }
  - { to: design-m12c-section-editor, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: mockup-planform-point-verbs, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Detailed design of the three planform-outline verbs of Ruling 62. Add point (double-click on a curve) inserts a
  vertex by exact Boehm knot insertion. Remove point (⌫) drops one knot and refits only the two replacement vertices,
  refused at the floor of 4 and on named, handle and anchor points with the reason; the change is measured and
  reported. Rebuild to N (4–10) previews the refit with the measured largest change in mm and the curvature-break count,
  and applies as one undo step. Each verb is a one-shot point command (one accepted row, a new receipt kind), on all
  five channels, gated on FoilDSL 4.1 for the lowered floor (ADR-0001 Amendment 2). Fit points is deferred with reasons.
review-suggested:
  - { by: adr-0001-master-curve-degree, on: 2026-10-03, reason: "Amendment 2 (Ruling 62): channels hold 4-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); the verbs Add point, Remove point and Rebuild to N are designed in docs/design/planform-point-verbs.md; foildsl.md 5 item 3, A4.1/A4.2/GEO-05 floor text need amendment (F-4)." }
---

# Design: planform outline point verbs — Add point, Remove point, Rebuild to N

- **Context:** [Ruling 62](../notes/rulings.md) (operator, 2026-10-03): "There are too many points on the outlines for
  some of the foil shapes I would be building (where 3–4 points are sufficient) … we need a remove point option … as
  well as an add point option" — on the planform outline. The ruling: floor 4 at degree 3 ([ADR-0001](../adr/0001-master-curve-degree.md)
  Amendment 2, written with this design), verbs Add point, Remove point and Rebuild to N, design and mockup now, build
  after M1.2c joins. Spec [`cfd-workbench-v1.md`](../specs/cfd-workbench-v1.md) A4.1, A4.2, A4.3, A4.5, A4.6, CAD-15,
  CAD-21, GEO-05, GEO-14; [`foildsl.md`](../specs/foildsl.md) §5 item 3. ADR-0005 (point types are knot multiplicity),
  ADR-0007 (gesture commit, one undo row), ADR-0009 (one command table). The M1.2b and M1.2b2 designs (the point layer,
  the point-command path) and the M1.2c design (the section editor's Insert/Delete/Rebuild, whose gestures, menu
  placement and refusal style these verbs reuse).
- **Delivery phase:** a slice after M1.2c. Real: everything. No mock seam: every verb is a pure Core function over the
  source bytes, so Desktop tests drive it from fixtures.
- **Mockup:** [`docs/mockups/planform-point-verbs.html`](../mockups/planform-point-verbs.html) (six screens; captures in
  `docs/proof/planform-point-verbs/`). The operator approves it before any build track (memory rule; Ruling 62 (3)).
- **Author / date:** `/design-slice` sub-agent (session `planform-verbs`), 2026-10-03. Code read at `4fa5a3f`.

## 0. What the operator will see

### 0.1 The demo at the end of the slice (packaged `.app`, macOS, New foil)

1. **New foil.** Each rail has 10 points. Select trailing-edge point 6. The Edit menu holds **Add point…**,
   **Remove point ⌫** and **Rebuild trailing edge…** in one group above the point-type commands. Right-click on the point
   shows **Remove Point** and **Rebuild Trailing Edge…**; right-click on the outline away from a point shows
   **Add Point Here**.
2. **Rebuild trailing edge… → 4.** A popover opens below the drawing with **Points − 4 +** (4 to 10). The rebuilt curve
   is drawn dashed over the current one with its four points. A warning tick marks the largest change:
   **8.97 mm at 467.50 mm from root** (measured in the mockup on the New foil's elliptical tip; the build measures its
   own). The popover lists what is kept exactly (root end, its tangent square to the centre line, tip end), how far
   the tip direction turns (35.03° in the mockup) and "Curvature breaks 0 → 0". Return applies: one undo step. Escape or Cancel: nothing changed, no undo
   step.
3. **Add point.** Double-click the 4-point trailing edge at 250 mm from root. A fifth point appears there, selected and
   focused. The strip says "Added trailing edge point 3 of 5. Shape unchanged: largest change 0.000 mm. Points 2 and 4
   moved to keep it." From the keyboard, Edit ▸ Add point… asks "From root ___ mm", prefilled with the middle of the
   segment at the selected point.
4. **Remove point.** Select leading-edge point 6 of 10 and press ⌫. The strip: "Removed leading edge point 6. Now 9
   points. Largest change 0.26 mm at 480.00 mm from root." The next point toward the tip is selected. ⌘Z restores
   the curve exactly.
5. **The floor.** On the 4-point trailing edge, select point 3 and press ⌫. The strip warns: "A curve needs at least 4
   points. The trailing edge has 4, so point 3 stays." Nothing changes; the undo depth is the same. In both menus Remove
   point is disabled with that reason.
6. **Save, close, reopen.** The 4-point rail, its ids and the undo history are as saved. The file's header now reads
   `foildsl "4.1"`.

### 0.2 What the operator will NOT see in this slice, and why

| Not shown | Why | When |
|---|---|---|
| Fit points (a curve through typed or measured points) | Needs input the product does not have yet (a point list, parameterisation, end conditions) and a provenance field in the record (A4.1). The need named in Ruling 62 — fewer points — is met by Rebuild. §6.3 | Its own slice, when the operator asks to trace a planform from points |
| Rebuild of both edges in one step | DR-PV-5 recommends one curve per command | If the operator rules DR-PV-5 B |
| A CLI write command | The CLI has `inspect` only (m12b D-3 precedent). `inspect --json` already shows every channel's points | With a CLI editing slice |
| Insert anchor (keep shape) | The section editor has it (M1.2c §3.4); Ruling 62 names three verbs | A later request |
| Removing several selected points with one ⌫ | Each removal is measured and reported on its own ("Remove points one at a time, so each change is measured.") | An operator report |
| ~~A New foil with fewer rail points~~ | **Moved into scope by Ruling 64** (operator: "New foil should not ship with 10 points that is too complex — it should be 4 points on TE and LE"): see §14 "New foil default" | — |

## 1. Grounding — what this design must satisfy

**Spec (quoted).**

- A4.1: "channels — five curves, each: family B-spline, degree p stored per curve (default 3 for the master curves —
  seven control vertices, six to ten allowed …)". Amended by ADR-0001 Amendment 2 to four.
- A4.2: "**Insert CV** (knot insertion by Boehm's algorithm, shape-preserving to 10⁻¹² relative — the A4.5 exact path)
  and **Delete CV** (the shape change is measured on the distribution-curve oracle of A4.5 and reported; the floor is the
  record's six vertices, A4.1), always as a draft (Return applies, Escape cancels, one undo item)." "**Rebuild** is Fair
  with a chosen vertex count."
- A4.3: "Continuity is measured, never assumed."
- A4.5: "Every Fair, Rebuild, Fit points, Insert and Delete deviation … is measured on the same sample set: 201 samples
  uniform in η plus every knot of both curves … the reported deviation is the maximum over that set and the metric is
  named in the status line."
- A4.6: "an approximate path is accepted iff its measured deviation ≤ the model/join tolerance (10 µm) … a path above
  its acceptance disables Apply with the number." It lists representation conversions; DR-PV-3 asks whether a
  deliberate Rebuild is one.
- CAD-21: "every other CAD verb of the B1 table is reachable from a menu-bar menu and from the command palette, each with
  a keyboard route (where each verb sits is a `/ui-design` decision)." The B1 verbs include Insert CV · Delete CV ·
  Rebuild · Fit points (spec :1363).
- GEO-05: "Given Insert CV on the curve, then the shape is preserved to 10⁻¹² relative …; given Delete leaving fewer than
  p + 2 vertices, then it is blocked with the reason, and otherwise the shape change is reported." (F-4: p + 2 = 5
  conflicts with Ruling 62's floor 4 = p + 1.)
- GEO-14: "Given a previewed Fair, Rebuild, Fit points, Insert or Delete, when cancelled, then geometry, assignments,
  recipe state, freshness and undo history are unchanged. Given Apply, then one undo item captures it … Given Undo, Redo,
  save and reopen, then the identity oracle passes."
- `foildsl.md` §5 item 3: "Channels have p=3 and N in [6,10] in 4.0 and N in [6,16] in 4.1".

**Code as built (Verified by reading at `4fa5a3f`).**

- Parser count rule: `FoilSource.cs`:1279-1280 — `max = Profile ? 32 : 4.1 ? 16 : 10`; `Points.Length >= 6`. One rule
  per curve kind, not per name.
- Exact insertion: `FoilSource.InsertKnot` (`:751-781`, degree-general, refuses multiplicity ≥ p) and the channel copy
  inside `MakeAnchor` (`AuthoringSession.cs`:1123-1185). Tiller removal: `FoilSource.RemoveKnot` (`:783`). Least
  squares with pins: `FoilSource.FitOrdinates` (`:467-489`, λ = 0, `ConstrainedFit.Solve`). Fresh ids above the curve's
  maximum: `FoilSource.NextVertexIds` (`:690-700`). `ProfileFair` is degree 5 only (`ConstrainedFit.cs`:178, `:360`),
  so it is not reusable for channels.
- The point-command path: `AuthoringSession.ApplyPointCommand` (`:182-183`, core `:1021-1065`) — one-shot, idempotent by
  operation id, one accepted row with receipt `rail = "point-type" | "tangent-kind"` and `Curve`, outcome
  `PointOutcome(AcceptedId, MaxDeviationMeters, PointsBefore, PointsAfter)` (`:39`). `EvaluatePointCommand` prints and
  raises the header when `Tangents.Length > 0 || Points.Length > 10` (`:1116-1117`). `MakeControl` requires
  `Points.Length >= 8` (`:1190`). The replay guard names the two kinds (`:1032`). `EditReference` arms (`:1805-1822`).
- Roles: `Planform.Role` (`PointModel.cs`:147-155): 0 root end, 1 root handle, n−1 tip end, n−2 tip handle, anchors by
  `FoilSource.IsAnchor`, their neighbours handles, the rest Control. `Planform.Project` samples 8 points per non-empty
  span, starting half a step in (`PointModel.cs`:132-143). `EditableCurves` = the five channels (`:10`).
- Desktop: the controller's direct-command path `RunDirectCommandAsync` refuses while a gesture or draft is open and
  when the foil is not Certified (`WorkbenchController.cs`:967-978); its catch writes `"{code}: This change wasn't
  applied. Nothing changed."` and drops `ContractError.Reason` (`:995`; F-3). `ApplyPointCommandAsync` reports the
  generic "Point change applied. Max deviation …" (`:951`). Selection after a commit: `Reconcile` drops missing ids to
  `Selection.Foil` (`:528-566`). `PlanCanvas` double-click on a point requests its value field (`:324`); the point
  context menu is `OpenPointMenu` (`:378-402`; "Make Anchor Point", "Make Control Point", Tangent, Fit); key handling
  `OnKeyDown` (`:477`). `ElevationView` double-click on a point does the same (`:767`). `EditVerbRouter` sends Edit
  verbs to a focused `TextBox` first (`Shell/EditVerbRouter.cs`:19-33). Command rows `point.*` sit in the Edit menu
  (`Shell/CommandTable.cs`:111-115).
- The CLI has one verb, `inspect --json`, which prints every channel's points with role, kind, locks and freedom
  (`src/CfdWorkbench.Cli/Program.cs`:116-136).

**Defect classes that apply** (`docs/lessons/defect-classes.md`): EDIT-KIND-REOPEN (a new receipt kind must round-trip
reopen), REPLAY-CROSS-KIND (the replay guard must learn the new kinds), STATUS-CLOBBER (a verb's report must survive the
background sampling), UI-C / FOCUS-START (focus after a re-render and after the popover closes), UI-DEAD-CONTROL (every
new menu row has an action), UI-I / UI-M (every printed number is computed from its operands), BUDGET-DISPLAY (the
preview never spends the certificate's budget), TEST-RING / TEST-COST (each test states its ring and cost).

**Drift surfaced (not silently resolved):** Ruling 62 says "4 to 10"; ADR-0001 Amendment 1 allows 16 under 4.1. This
design reads "4 to 10" as Rebuild's range and keeps 16 for Add point (DR-PV-1). GEO-05's "p + 2" and A4.2's "six" need
the spec owner (F-4).

## 2. Responsibility

One responsibility: **change how many control vertices a channel has, with the shape change exact (Add) or measured
and reported (Remove, Rebuild), as one accepted revision.** It owns three Core point commands, one preview projection,
one deviation oracle and their Desktop entry points. It does not own point types (ADR-0005, M1.2b), moves (the gesture
path), sections (M1.2c), or Fit points (deferred).

## 3. Data model (settled first)

### 3.1 Bounded context and ubiquitous language

Context: **Authoring** (the source bytes are the only authority, ADR-0002). Terms:

| Term | Meaning |
|---|---|
| Channel / master curve | one of the five degree-3 distribution curves: leading, trailing (the **rails**, the planform outline), dihedral, twist, thickness |
| Point | a control vertex of a channel with a stable id (A4.1) |
| Floor · ceiling | the least and most points a channel may hold in the file: 4 and 16 under 4.1, 6 and 10 under 4.0. The verbs always use 4 and 16 (crossing 6 or 10 raises the header) |
| Add point | insert one knot by Boehm; the shape is unchanged; one point more |
| Remove point | remove one simple knot and refit the two points that replace three; one point fewer; the change is measured |
| Rebuild to N | refit the whole channel with N points on new knots; the change is measured; anchors do not survive |
| Largest change | the A4.5 oracle maximum of \|v_after(η) − v_before(η)\| over 201 uniform η plus every knot of both curves, with its η |

The UI says **point**, never "CV" or "vertex" (M1.2b vocabulary); the spec's Insert CV / Delete CV are these verbs (F-4).

### 3.2 Aggregates and invariants

| Aggregate (root) | Invariant it protects | Enforced by |
|---|---|---|
| **Surface revision** (the parsed source) | each channel: degree 3, clamped, n + 4 knots, floor ≤ n ≤ ceiling for its version, strictly increasing abscissae from 0 to 1, interior multiplicity ≤ 3, every tangent row names an interior anchor | the parser (`FoilSource.cs`:1277-1290), unchanged except the floor |
| **Authoring session** (history) | one verb = one accepted row whose receipt names its kind and target; replay of an operation id returns the same row or refuses | `ApplyPointCommandCore`, `Retry`, `EditReference` |

One aggregate per transaction: a verb touches one channel of one revision. No new aggregate.

### 3.3 Durable representation, grain, history

- **No new record field.** The count is the length of `points`; the knots are already stored. Point type stays derived
  from multiplicity (ADR-0005). Derive, don't store: the floor and ceiling are functions of the version, not fields.
- **Accepted row grain (unchanged):** one row is exactly one applied verb on one channel, identified by its operation id,
  recorded at Apply. A verb whose result equals its base adds no row (the same rule as M1.2c's no-op Finish). Rebuild
  to the same count on the same knots returns the base before any fit, so it adds no row and the bytes are untouched
  (a refit would not reproduce the ordinates bit for bit).
- **Receipt (expand-only):** three new values in the closed `rail` set, each with `Curve` set (the point-command shape):

  | `rail` | `Curve` | `VertexId` | Check on reopen (`EditReference`) |
  |---|---|---|---|
  | `point-add` | the channel | the new point's id | the child has the id on that channel; the parent does not |
  | `point-remove` | the channel | the removed point's id | the parent has it; the child does not |
  | `curve-rebuild` | the channel | the channel name (the target, as `dimension` puts its target name there) | `VertexId == Curve` |

  Writer: `ApplyPointCommandCore`. Compute reader: `EditReference` and the replay guard. Nothing else reads them; no
  field is dead weight (DM15). History rule: append-only (the existing chain); Undo moves the cursor.
- **Measures:** the largest change is reported, not stored (telemetry carries it, §10). Non-additive (a maximum).

### 3.4 Roles, refusals and what each verb may touch (derived, Core)

Roles come from `Planform.Role` as built. **Refusal order for Remove point** (first match wins; the floor comes first so
a 4-point curve always names the floor — screen 6):

| # | Condition | Code | Copy |
|---|---|---|---|
| 1 | n ≤ floor (4) | `DSL-CURVE` | COPY-192 |
| 2 | root end or tip end | `DSL-LOCK` | COPY-193 |
| 3 | root handle or tip handle | `DSL-LOCK` | COPY-194 (DR-PV-9) |
| 4 | an anchor, or an anchor's handle | `DSL-LOCK` | COPY-195 (names the anchor) |
| 5 | a lock row names the point's id | `DSL-LOCK` | the lock's existing copy |
| 6 | more than one point selected | (Desktop) | COPY-206 "Remove points one at a time, so each change is measured." |

So only **Control** points can be removed. A Control point i has 2 ≤ i ≤ n − 3, and its middle knot t₍ᵢ₊₂₎ is never part
of an anchor's triple (an anchor at a owns t₍ₐ₊₁₎…t₍ₐ₊₃₎, which would make i ∈ {a − 1, a, a + 1}, roles that are
refused). **Add point** has no role refusals; it refuses at the ceiling (COPY-196), on a too-close insertion (COPY-197)
and on the port half (COPY-198, Desktop). **Rebuild** refuses N outside 4–10 (COPY-205) and a result whose rails cross
(COPY-203). Every verb refuses while a gesture or draft is open and when the foil is not Certified (as built).

**Interior anchors and the floor.** A curve with k interior anchors holds at least 4 + 3k points (ADR-0001 Amendment 2).
At 4 points every point is named (root end, two handles, tip end), so Make anchor has no target. At 5 points Control →
Anchor adds two or three points and gives 7 or 8. A curve with an anchor has at least 7 points, so Anchor → Control
(minus 2) never goes below 5; `MakeControl`'s guard becomes n ≥ 7, which always holds.

**Locks.** Only `root_mirror` locks are certified (`Geometry.cs`:326); any other lock kind makes the document Unsupported,
and every verb is off then. `root_mirror` names a channel, not a point id, so Rebuild's new ids never orphan a lock row.

### 3.5 Ids and selection after each verb

- **Add:** every existing point keeps its id (Boehm replaces two points with three; the two outer ones keep their ids);
  the new point gets `cv-<max+1>` (`NextVertexIds` rule, never reissuing a number the curve has used).
- **Remove:** every other point keeps its id; the removed id is gone.
- **Rebuild:** when N equals the current count and the knots are unchanged, every id is kept (the identity). Otherwise
  the root end and tip end keep their ids (the same positions, exactly), and every other point gets a fresh id. The
  as-built `RebuildProfile` renumbers everything; the ends are kept here because they are named points.
- **Selection** has one owner, Core: `PointOutcome.SelectId`. Add returns the new point. Remove returns the nearest
  remaining point of that curve by η (ties toward the tip). Rebuild returns the selected id when it survives, otherwise
  null, and the Desktop clears the point selection (the curve was rebuilt, no point was picked; Rhino leaves the rebuilt
  curve selected). Focus stays in the view that issued the verb.
  Undo and Redo use `Reconcile` as built (a missing id falls back to the foil). **`simplify:`** ceiling: after Undo of a
  Remove the restored point is not reselected; upgrade trigger: an operator report.

### 3.6 FoilDSL and ADR-0001 Amendment 2 (summary; the text is in the ADR)

- 4.1: channels N in **[4, 16]**; 4.0 unchanged at [6, 10]. The floor applies to all five channels (the parser's rule is
  per kind).
- The verb that first takes a channel below 6 writes `"4.1"` through `EnsureHeader41` in the same patch (the one writer,
  never lowering). `EvaluatePointCommand`'s condition becomes `Points.Length > 10 || Points.Length < 6` (`:1116`); a
  4.1 document whose channels return to 6–10 stays 4.1.
- The verbs use two constants, `ChannelEdits.Floor = 4` and `ChannelEdits.Ceiling = 16`, whatever the version: crossing
  6 or 10 raises the header, so these are the real limits. No `CurveView.Floor` field.
- New conformance cases in `foildsl.md` (§5 item 3 text and the examples), in the same change as the parser.
- Old builds: pre-M1.2b → `DSL-VERSION`; M1.2b-era → `DSL-CURVE` for a sub-6 channel, `DOC-REFERENCE` for a new receipt
  kind. Observed by the characterization receipt (PVC, `docs/proof/planform-verbs-old-build/`), never asserted.
  DR-PV-2 offers "4.2".
- Fairness evidence for 4 and 5 vertices is named in the ADR and measured by track SPK.

### 3.7 Which curves get the verbs

**All five channels** (DR-PV-4 recommendation A). Why: the floor is per curve kind in the parser and ADR-0001 governs
all five; the views already edit all five through one point layer and one command path (`EditableCurves`, CH1); a
rails-only rule would add a per-name check in Core and a disabled-with-reason row in two elevations. Where they live:
Plan (leading, trailing), Front band (dihedral) and its t/c lane (thickness), the Side view's twist lane — double-click
on the curve and ⌫ on a point work the same way in each.

One consequence is named, not hidden: the **thickness** channel's "From this section" refit (`ThicknessFit`) solves for
its ordinates at fixed knots with every scoped station as a row. With fewer thickness points, fewer stations can each
hold their own t/c. That Finish already refuses with its reason where it happens; the verbs add nothing for it (R-3).

## 4. Persistence

- **Source:** no grammar change beyond the 4.1 floor. The canonical geometry format (`foildsl-geometry-4.0`) and
  evaluator id `cfdw-cv/2` are unchanged; the definition hash moves only because the geometry moves.
- **Envelope:** three new `rail` values (§3.3). Expand-only: an envelope without them reads as before. A build older
  than this change refuses an envelope that holds one, with `DOC-REFERENCE`, and leaves the file unchanged
  (`Reopen_OldBuildNewReceiptKind_RefusedFileUnchanged` (PVC), characterization).
- **Recovery:** none. The verbs are one-shot commands with no session draft, so a crash leaves either the base or the
  applied row. The Rebuild preview is Desktop state; a crash during it loses only the preview.
- **Migration:** none forced. A document stays 4.0 until a verb takes a channel below 6 or above 10.

## 5. Contracts

### 5.1 Exposed — Core

```csharp
// AuthoringSession.cs — PointCommand gains three records (expand; the base shape (Curve, VertexId) is kept)
public sealed record AddPoint(string Curve, double Eta) : PointCommand(Curve, "");
public sealed record RemovePoint(string Curve, string VertexId) : PointCommand(Curve, VertexId);
public sealed record RebuildCurve(string Curve, int Count) : PointCommand(Curve, Curve);

// PointOutcome gains two non-positional members with defaults (S-6 shape), so existing constructions compile unchanged
public sealed record PointOutcome(string AcceptedId, double MaxDeviationMeters, int PointsBefore, int PointsAfter)
{ public double AtEta { get; init; } public string? SelectId { get; init; } }

// measured through the session (no draft, no row); Run("rebuild.preview"). It runs EvaluatePointCommand without the
// commit, so preview and Apply share one code path. Called once when the popover opens: all counts 4..10 at once.
public IReadOnlyList<RebuildPreview> PreviewRebuilds(string curve);
public sealed record RebuildPreview(int Count, CurveView Candidate, double MaxChange, double AtEta,
    int BreaksBefore, int BreaksAfter, double TipTurnDegrees, string? Refusal);

// ChannelEdits.cs (new, internal) — the three algorithms of §6.4 and the oracle
internal static class ChannelEdits
{
    internal const int Floor = 4, Ceiling = 16;                                          // the verbs' limits (§3.6)
    internal static Curve Add(Curve curve, double eta, out int index);
    internal static Curve Remove(Curve curve, int index);
    internal static Curve Rebuild(Curve curve, int count);
    internal static (double Max, double AtEta) MaxChange(Curve before, Curve after);   // the A4.5 oracle
    internal static int CurvatureBreaks(Curve curve);                                   // A4.3, analytic one-sided κ
    internal static string? RemoveRefusal(CurveView view, string id);                    // §3.4 order; null when allowed
}

```

Errors: `DSL-CURVE` (floor, ceiling, count range, too close, fold), `DSL-LOCK` (role refusals), `DSL-GEOMETRY` (rails cross at certification), `DSL-NOT-ASSESSED`, `DSL-DRAFT-OWNED`,
`DOC-OPERATION-CONFLICT` (replay of another kind). Every refusal carries `ContractError.Reason` with the COPY string
(§11.4), so the Desktop shows the reason (F-3).

### 5.2 Exposed — Desktop

- `WorkbenchController.AddPointAsync(string curve, double eta)`, `RemovePointAsync(PointRef)`,
  `OpenRebuild(string curve)` (holds the controller `Busy`, so Undo and every edit are refused while it is open,
  `WorkbenchController.cs`:1547), `ApplyRebuildAsync(string curve, int count)`; all through `RunDirectCommandAsync`.
  The refusal path writes `ContractError.Reason` when present (F-3), and the commit report is the verb's COPY row.
- `CommandTable` rows (Edit menu and palette; one table, ADR-0009):

  | Command | Label | Shortcut | Enabled when |
  |---|---|---|---|
  | `point.add` | "Add point…" | — (double-click on a curve adds at the pointer) | one point selected; opens "From root ___ mm" (unit-aware, expressions accepted, prefilled with the middle of the knot span at that point; the position is marked on the curve while the field is open); Return adds, Escape cancels |
  | `point.remove` | "Remove point" | ⌫ on macOS; Delete and Backspace on Windows; view-scoped | one point selected and `RemoveRefusal` is null; otherwise disabled with the reason as its tooltip |
  | `curve.rebuild` | "Rebuild <curve>…" | — | a point of the curve selected, or the curve row in Browser / Points |

- `PlanCanvas` / `ElevationView`: double-click on a curve away from a point → `AddPointAsync` at the pointer's η
  (starboard half only; points take precedence within `HitRadius` 14 px, so double-click on a point keeps its value
  field). ⌫ with the canvas focused → `point.remove`. The point context menu gains Remove Point and Rebuild <Curve>…;
  a new outline context menu: Add Point Here, Rebuild <Curve>…, Fit.
- `RebuildPopover` (new control in the model area): Points stepper (4–10, typed or ±; the hint says the range is Ruling
  62's), readouts from the chosen `RebuildPreview`, Cancel and Rebuild (default). Modal to edits (the controller is
  `Busy` while open), not to navigation. The stepper only picks among the seven previews computed at open.

### 5.3 Consumed

| Contract | Source | Used for |
|---|---|---|
| `ChannelEvaluator.Parameter` / `.Value` | `Placement.cs`:110-125 | every η inversion (the one channel inversion) |
| `FoilSource.InsertKnot` / `MakeAnchor`'s Boehm loop | `FoilSource.cs`:751, `AuthoringSession.cs`:1150-1166 | Add (one shared internal function; the duplicate is folded) |
| `FoilSource.FitOrdinates` + `ConstrainedFit.Solve` | `FoilSource.cs`:467-489, `ConstrainedFit.cs`:12 | Remove and Rebuild ordinates (λ = 0, pins) |
| `FoilSource.NextVertexIds` | `:690` | fresh ids |
| `FoilSource.Print`, `EnsureHeader41`, `Parse` | `:187`, `:166`, `:126` | write and re-check every patch |
| `SplineBasis.Evaluate` (with derivatives) | `ConstrainedFit.cs`:582 | oracle and analytic curvature |
| `Controller.EdgeHullCrossing` | `WorkbenchController.cs`:751 | the preview's advisory crossing check (the certificate decides at Apply) |
| `RequireAdmission` / certificate | `AuthoringSession.cs` | Apply refuses an uncertified result |

No unfamiliar dependency: no spike is needed for a contract. The one empirical unknown, fairness at 4 and 5, is track
SPK (a measurement, ADR-0001 Amendment 2).

## 6. Patterns and structure

### 6.1 Named patterns

- **Command (GoF), one-shot with idempotent replay** — `PointCommand` records through `ApplyPointCommand`, as built.
  Reused, not new. The Simplifier's check: no draft object, because each verb is one step with no intermediate state
  the user edits.
- **Preview projection (pure function → view model)** — `PreviewRebuild` computes the candidate from the accepted bytes
  without touching history; Apply recomputes deterministically and compares the base. This is the M1.2b chord-preview
  shape (no draft, recompute at commit), not a second authority: the candidate bytes are never kept.
- **Guard clauses with a reason** — `RemoveRefusal` is one ordered table (§3.4), read by the command row (disabled
  reason), the key handler and Core (refusal). One definition, three readers.
- **Knot insertion / knot removal + constrained least squares** — Piegl & Tiller §5.2–5.4 and the KKT pin pattern
  already in `FitOrdinates`.

### 6.2 Solution-Selection Ladder

YAGNI: Fit points, multi-remove, both-edges Rebuild and a CLI write verb are out (§0.2). Reuse: Boehm, `FitOrdinates`,
`NextVertexIds`, `EnsureHeader41`, the point-command path, `RunDirectCommandAsync`, `OpenPointMenu`, `CommandTable`.
New code: `ChannelEdits` (three functions, the oracle, the break counter), three records, one popover. No new dependency.
`simplify:` markers: selection after Undo (§3.5); one-point Remove (§0.2); the 401-sample fit set (ceiling: a channel
whose features are narrower than 1/400 of the span is fitted coarsely; upgrade trigger: a Rebuild whose oracle maximum
lands between fit samples by more than 10 % of the reported change).

### 6.3 Fit points — decided: later, with reasons

1. It needs inputs the product does not collect: target points (typed, measured or from an image), a parameterisation
   and end conditions (A4.2).
2. A4.1 stores Fit points' **anchors as construction provenance**; FoilDSL has no field for it, so it is a grammar change
   with its own expand-migrate-contract — more than Ruling 62 asked for.
3. The operator's need is fewer points on a shape that already exists. Rebuild fits the current curve; Fit points fits
   external points. They share the solver, so adding Fit points later reuses `ChannelEdits`' least-squares core.
4. **Trigger:** the operator asks to trace a planform from measured points or a reference image (M1.2d's import work is
   the natural home). Recorded as OI-PV-1.

### 6.4 The three algorithms (Core, `ChannelEdits`)

**Add point (curve, η).**
1. t\* = the unique parameter with x(t\*) = η by `ChannelEvaluator.Parameter` (`Placement.cs`:125, "the one binary64
   channel inversion"). Every inversion in this design — Add's t\*, the fit targets v_old(η), each fit row's parameter
   (solved on the **candidate** curve, x_new(t) = η, never on the old one) and the oracle — calls `ChannelEvaluator`;
   no second inversion (class GEO-B).
2. Refuse `DSL-CURVE` "too close" (COPY-197) when t\* is within 10⁻⁹ of an existing knot, or when the new abscissa gap
   to a neighbour would be under the η grid 10⁻⁷ (Geometry F3 in M1.2b). The product never writes a multiplicity-2 knot.
3. Refuse at the ceiling (16; COPY-196). Past 10 the header becomes 4.1 in the same patch.
4. Insert t\* once by Boehm. The new id goes to index k − 1 (k the span index); the two recomputed neighbours keep
   theirs. **Bit-exact rows:** when the two ordinates a new point combines are bitwise equal, the new ordinate is that
   value, unchanged — (1 − α)·y₀ + α·y₀ is not bit-exact (the Geometry lens measured a 1-ulp miss in 4,207 of 100,000
   first-span inserts; New foil's trailing root fails at α = 0.013), and the certificate checks the root mirror by
   exact equality (`Geometry.cs`:329-330). `MakeAnchor`'s copy of the loop gets the same rule. Measured on the oracle: the mockup gives 7.1 × 10⁻¹⁴ mm on a 131 mm curve (Inferred for the build; the
   test asserts ≤ 10⁻¹² relative).
5. **Tangent rows:** insertion next to an anchor shortens that anchor's handle along its own line, so a `smooth` row
   stays true; a `symmetric` row would not, so it is rewritten to `smooth` in the same patch and reported (COPY-204).
6. Keyboard route (`point.add`): the field's default is the η of the midpoint of the knot span containing the selected
   point's Greville parameter (the span to its right when that parameter is an interior knot; the last span for the tip
   end, the first for the root end). A span midpoint is never a knot. The typed position then goes through steps 1–5.

**Remove point (curve, id)** — only a Control point at index i (§3.4).
1. Remove the knot t₍ᵢ₊₂₎ (simple; §3.4). Old points i − 1, i, i + 1 are replaced by new points i − 1 and i; every
   other point keeps its position and id exactly.
2. Abscissae of the two new points: the old curve's η at their Greville parameters on the reduced knots (on a curve
   whose x(t) = t this is the Greville abscissa itself). Refuse "would fold" (COPY-197b) unless strictly increasing
   with a gap ≥ 10⁻⁷.
3. Ordinates of the two new points: least squares to the old v(η) at the fit samples, every other point pinned. **Fit
   samples:** 401 uniform η plus 4 Gauss points in every non-empty span of the candidate (mapped to η), so a freed
   column is never empty; Schoenberg–Whitney is checked before the solve, and a failure refuses with `DSL-CURVE` and
   COPY-208 (the as-built `GEOMETRY-FIT-SINGULAR` never reaches the user). The rows
   that touch a freed point are pins too, because the freed point's η is fixed and its partners are outside the change:
   the root-mirror row (new point 1 free and P₁.y = P₀.y now, or a `root_mirror` lock) pins P₁.y = P₀.y; an anchor's
   `smooth` row pins the freed handle's ordinate on the line through the anchor and its other handle. So the existing
   `FitOrdinates` pins carry them; no new constraint kind (the root mirror copies P₀.y's bits). **End directions are
   kept:** when a freed point is the root or tip handle, it stays on the current end tangent line (one more pin), so
   Remove never turns an end direction — consistent with refusing to remove the handles (COPY-194). A `symmetric` row
   there becomes `smooth`, reported (COPY-204).
   *Precondition:* the removed knot is simple in every curve the product writes; the parser also accepts a
   multiplicity-2 knot (`FoilSource.cs`:1287), and there the same steps free more points than needed and still hold
   (Geometry lens, Verified).
4. Measure and report the largest change and its η, and curvature breaks before → after. No tolerance gate: Remove is a
   deliberate change (A4.2 Delete CV). Locality is checked on v(η) at the oracle samples outside
   [x(t₍ᵢ₋₁₎), x(t₍ᵢ₊₅₎)], where the change is exactly 0.0 (Geometry lens probe).
   Measured in the mockup: leading edge point 6 of 10 → 0.26 mm at 480 mm.
5. Below 6 points the header becomes 4.1 in the same patch.

Why not Tiller removal alone (as built for profiles): it moves points i − 1 and i + 1 with no knowledge of the
root-mirror or tangent rows, so removing point 3 of a 5-point rail would break the root square and the certificate
would refuse. Why not delete the point and refit the whole curve: it moves every point, against the user's local intent
(DR-PV-7).

**Rebuild (curve, N)**, N ∈ [4, 10].
0. If N equals the current count, the new knots are bitwise equal to the current ones **and** the refit's oracle change
   is within the A4.5 identity tolerance, return the base (no row, ids kept). Otherwise apply and report, with fresh
   ids. (A same-count refit is the identity only when every abscissa sits at its Greville point: the Geometry lens
   measured 0.023 mm after one point was nudged 0.03 in η, and 2 × 10⁻¹³ ordinate drift even on Greville points.)
1. **Knots follow the current spacing** (DR-PV-6 A): take the curve's distinct knot values as a distribution; interior
   knot j of the new curve is that distribution read at j / (N − 3) by linear interpolation. With N equal to the current
   count and no anchors this reproduces the current knots, so Rebuild to the same count is the identity. Measured in
   the mockup on the New foil trailing edge: 5 points 1.36 mm (even spacing: 5.61 mm); 10 points 0.000 mm (even: 1.75 mm).
2. Abscissae: the current curve's η at the new Greville parameters; first 0, last 1; refused with COPY-197b if a gap is
   under the η grid 10⁻⁷.
3. Ordinates: least squares to the current v(η) at the fit samples (as Remove: 401 uniform η plus 4 Gauss points per
   span, Schoenberg–Whitney checked, COPY-208) with pins: the root-end and tip-end ordinates exactly,
   and the root row P₁.y = P₀.y when the curve has it now or carries a `root_mirror` lock.
4. Anchors do not survive (the new knots are simple); every tangent row is removed in the same patch. The Desktop lists
   each Anchor-role point of the current curve as "becomes a control point" (COPY-202). This differs from the section
   editor's Rebuild, which keeps anchors as KKT rows (M1.2c §3.4): DR-PV-5 asks the operator, F-6 tells M1.2c's owner.
5. Report: largest change and η (oracle), curvature breaks before → after (A4.3, analytic one-sided curvature at each
   interior knot — the left and right span polynomials evaluated exactly at the knot, in the channel's physical unit
   (mm for span and aft), a break when the jump exceeds 10⁻⁶ · max|κ| over the curve; "before" is non-zero when the
   curve has anchors, and "after" is measured, never
   asserted from the simple knots — A4.3 is the reason this readout exists), the root and tip direction turns in
   degrees (the tip tangent is not kept; 35.03° on the New foil trailing edge at 4; the root turn is 0 when the root
   square is pinned), the planform area change (the Wing block's area moves 1000.0 → 998.2 cm² in the mockup), points
   before → after. The preview also runs the advisory rail-crossing check;
   a crossing disables Rebuild with COPY-203. Apply recomputes through the same `EvaluatePointCommand`, certifies and
   commits one row.
6. Measured in the mockup (New foil, 10 → 4): trailing edge 8.97 mm at 467.5 mm; leading edge 2.99 mm at 467.5 mm. The
   elliptical tip is what costs; the table for 4–10 is in the mockup's *Measured* section.

**The oracle (`MaxChange`).** 201 uniform η plus the η of every interior knot of both curves; v by `ChannelEvaluator`
on each curve; the maximum of the absolute difference and its η. It is a sampled lower bound (2.206 against a dense
2.2196 in the Geometry lens's probe), so the popover's help names it "measured at the A4.5 samples"; the design never
calls it the maximum. It measures vertical Δv at fixed η, which overstates the normal distance near a steep tip. The as-built `MaxPointDelta` samples 201 η only and no
knots (`AuthoringSession.cs`:1067-1090; F-5); the existing point-type commands switch to `MaxChange` in the same track,
so one definition serves every point command.

## 7. Error and concurrency model

- Every verb is synchronous Core work on the session lock, run off the UI thread by `RunDirectCommandAsync` (as built).
  The controller is `Busy` for its duration; a second verb, a gesture or a nudge is refused with "Finish the current
  change first." (as built).
- The seven Rebuild previews (N = 4..10) are computed once, on the pool, when the popover opens; the stepper picks
  one. No cancellation, no counter, no loading state. They read the accepted bytes, which cannot change while the
  popover is open (the controller is `Busy`; Undo and edits are refused). **`simplify:`** ceiling: seven solves at open;
  upgrade trigger: `Readiness_RebuildPreviews_Worst16PointCurveUnder100ms` fails — then compute on each change.
- Replay: the same operation id and payload returns the same row; another kind refuses `DOC-OPERATION-CONFLICT`
  (REPLAY-CROSS-KIND; the guard at `:1032` lists the five kinds).
- The status report is written through the status slot's version guard, so the accepted-slice sampling started by the
  commit cannot overwrite it (STATUS-CLOBBER).

## 8. Change-surface list (E7)

| Surface | Change | Track |
|---|---|---|
| Store (bytes) | 4.1 floor; header raised in the same patch | PVC |
| Domain / model | `ChannelEdits` (Floor, Ceiling, Add, Remove, Rebuild, `MaxChange`, `CurvatureBreaks`, `RemoveRefusal`); display samples include the ends (F-1); `MakeControl` guard | PVC |
| Service | `PointCommand` records, `ApplyPointCommandCore` arms, `PreviewRebuild`, receipt kinds, `EditReference`, replay guard, telemetry fields | PVC |
| Projection / wire | `PointOutcome.AtEta/SelectId`, `RebuildPreview`; CLI `inspect --json` unchanged (it already prints every channel's points) | PVC |
| Client type | controller verbs, refusal reason (F-3), selection rule | PVU |
| UI | double-click on a curve, ⌫, context menus, Edit rows, `RebuildPopover`, Properties curve group row, Points pane rows, strip copy | PVU |
| Compute reader | `EditReference` (reopen), replay guard, telemetry consumers (the event ring) | PVC |
| Spec text | `foildsl.md` §5 item 3 and examples; A4.1/A4.2/GEO-05 wording (spec owner, F-4) | PVC (foildsl.md); spec owner |
| Design language | COPY-190..205, the Rebuild popover component row | PVX |

## 9. Failure-mode analysis

| Failure mode | Cause | Disposition | Detect | Test |
|---|---|---|---|---|
| Add changes the shape | a wrong alpha or span index | **prevent**: one shared Boehm function | oracle | `AddPoint_Boehm_ShapeUnchangedWithin1e12Relative` (PVC) |
| Add writes a multiplicity-2 knot or near-coincident points | double-click at a knot | **prevent**: refuse within 10⁻⁹ / 10⁻⁷ | COPY-197 | `AddPoint_AtExistingKnot_RefusedTooClose` (PVC) |
| Add past the ceiling | 16 points | **prevent** | COPY-196 | `AddPoint_AtCeiling16_RefusedNamingCeiling` (PVC) |
| A 4.0 file with 11 or 3–5 points | the header not raised | **prevent**: one writer, same patch | parser | `AddPoint_PastTen_WritesHeader41InSamePatch` (PVC), `RemovePoint_BelowSix_WritesHeader41` (PVC) |
| A symmetric row made false by Add or Remove | handle shortened | **mitigate**: rewrite to smooth, report | COPY-204 | `AddPoint_NextToSymmetricAnchor_RowBecomesSmoothReported` (PVC), `AddPoint_NextToSmoothAnchor_RowStillHolds` (PVC) |
| Remove breaks the root square or a tangent row | refit ignores rows | **prevent**: hard rows in the fit | certificate | `RemovePoint_RootMirrorRowHolds` (PVC), `RemovePoint_NextToAnchorHandle_TangentRowHolds` (PVC) |
| Remove moves points outside its support | wrong pinning | **prevent**: pins | bitwise compare | `RemovePoint_Control_ChangeMeasuredAndOutsideSupportUnchanged` (PVC) |
| Remove at the floor, on a named, handle or anchor point | user intent | **prevent**: ordered refusals | COPY-192..195 | `RemovePoint_RefusalTable_OrderAndCopy` (PVC) |
| Remove folds the abscissae | an edited curve with uneven η | **prevent**: refuse | COPY-197b | `RemovePoint_WouldFold_RefusedNothingChanged` (PVC) |
| The header is lowered to 4.0 when a channel returns to 6–10 | the new two-sided condition | **prevent**: `EnsureHeader41` only raises | parser | `PointVerb_On41FileBackInto6To10_HeaderStays41` (PVC) |
| The preview changes state or spends the certificate | preview on the commit path | **prevent**: no commit, no admission | event ring | `PreviewRebuilds_NoRowNoCertificate_BytesHistoryFreshnessUnchanged` (PVC) |
| Fewer thickness points block "From this section" | ThicknessFit capacity | **accept**: the Finish refusal says why, where it happens (R-3) | the as-built refusal | — |
| Add breaks the root square by one ulp | (1 − α)·y₀ + α·y₀ in binary64 | **prevent**: equal inputs give the input bits | certificate (exact) | `AddPoint_FirstSpanRootMirror_RootSquareBitExact` (PVC) |
| The refit system is singular | clustered knots leave a column with no samples | **prevent**: per-span Gauss samples; Schoenberg–Whitney checked | COPY-208 | `RemovePoint_ClusteredKnots_SolvesWithSpanSamples` (PVC), `Rebuild_SchoenbergWhitneyFails_RefusedWithCopy` (PVC) |
| Remove turns an end direction | a freed end handle refitted freely | **prevent**: end handle pinned to its tangent line | — | `RemovePoint_NextToEndHandle_EndDirectionKept` (PVC) |
| Same-count Rebuild claimed as the identity when it is not | non-Greville abscissae | **prevent**: identity only within the A4.5 identity tolerance | report | `Rebuild_SameCountNonGrevilleAbscissae_AppliesAndReports` (PVC) |
| Rebuild's preview and apply differ | non-determinism | **prevent**: pure function, same base | bytes equal | `Rebuild_PreviewEqualsApply_SameBytes` (PVC) |
| Rebuild makes the rails cross | few points on a thin tip | **prevent**: advisory in preview, certificate at Apply | COPY-203 | `Rebuild_RailsWouldCross_RefusedNothingChanged` (PVC), `RebuildPopover_CrossingDisablesRebuildWithReason` (PVU) |
| Rebuild silently drops anchors | simple knots | **mitigate**: named in preview and report | COPY-202 | `Rebuild_WithAnchors_AnchorsDroppedRowsRemoved` (PVC) |
| Count typed outside 4–10 | user input | **prevent** | COPY-205 | `Rebuild_CountOutside4To10_DslCurve` (PVC), `RebuildPopover_TypedCountOutOfRange_FieldErrorNothingChanged` (PVU) |
| Reopen refuses a project with the new receipts | `EditReference` not taught | **prevent** (EDIT-KIND-REOPEN) | `DOC-REFERENCE` | `Reopen_PointAddRemoveRebuildReceipts_Check` (PVC) |
| A forged receipt | hand-edited envelope | **prevent** | `DOC-REFERENCE` | `Receipt_ForgedPointVerbReceipts_DocReference` (PVC) |
| Replay of an id from another kind crashes | REPLAY-CROSS-KIND | **prevent**: kind check | `DOC-OPERATION-CONFLICT` | `ApplyPointCommand_SameOperationDifferentVerb_DocOperationConflict` (PVC) |
| The refusal shows a code, not the reason | the catch drops `Reason` (F-3) | **prevent** | strip text | `Controller_RefusalCopy_UsesCoreReasonNotCode` (PVU) |
| ⌫ in a text field removes a point | key routed to the view | **prevent**: view-scoped key; `EditVerbRouter` | — | `Properties_BackspaceInTextField_EditsTextNotPoint` (PVU) |
| The verb's report is overwritten | background sampling (STATUS-CLOBBER) | **prevent**: slot version guard | strip | `StatusStrip_VerbReport_NotClobberedBySampling` (PVU) |
| Focus lost after the popover or a re-render | UI-C | **prevent**: focus returns to the view and the selected point | — | `RebuildPopover_FocusReturnsToPlanOnClose` (PVU), `PlanCanvas_DoubleClickOnOutline_AddsPointSelectsAndFocusesIt` (PVU) |
| A 4-point outline drawn as 8 segments missing the ends | sampling per span (F-1) | **prevent**: ends included, a per-curve minimum | — | `PlanformView_FourVertexCurve_SamplesIncludeEndsAndMinimumCount` (PVC) |
| A displayed number that is not the measurement | UI-I / UI-M | **prevent**: the popover reads `RebuildPreview` only | — | `RebuildPopover_StepperReadoutsFromCore` (PVU) |
| A new menu row with no action | UI-DEAD-CONTROL | **prevent** | the walk (with the Properties "Rebuild…" link and the Points pane rows) | `EditMenu_PointVerbs_EnabledStateAndReasonPerSelection` (PVU) |
| The preview is slow on the worst case | 16-point curves, every N change | **detect**: `rebuild.preview` duration | telemetry | `Readiness_RebuildPreviews_Worst16PointCurveUnder100ms` (PVX) |
| Old build opens a 4-point project | version skew | **accept** (single user; file unchanged) | characterization | `Reopen_OldBuildNewReceiptKind_RefusedFileUnchanged` (PVC) |

## 10. Telemetry (normal path, no flag)

Questions an operator will ask, each with its source:

| Question | Source |
|---|---|
| How often is each verb used, and refused, and why? | the existing `apply` event with `EditKind` = `point-add` / `point-remove` / `curve-rebuild`; `Outcome` = OK or the error code |
| How big are the changes people accept? | **new** `DeviationInCurveUnit` (double?) for every channel; its unit follows from `CurveFamily` (null = not recorded, never a converted guess). `DeviationMicrometres` is not written by these verbs |
| Which counts do people rebuild to? | **new** `PointsBefore`, `PointsAfter` (int?) on the `apply` event |
| How long do the previews take, and how often is Rebuild cancelled? | **new** event `rebuild.preview` (one per popover open: duration of the seven solves, `CurveFamily`); **new** event `rebuild.close` (one per popover: outcome `applied` or `cancelled`, `PointsAfter`) |
| Which curve family? | `CurveFamily` (as built: rail · dihedral · twist · thickness) |

No ids, names or positions in any event (`PointVerbEvents_NoIdsOrPositions` (PVC)). Each event carries the session's
trace context as built. Error codes are the existing stable ones (§5.1).

## 11. UI and interaction design

Archetype: unchanged (the CAD workspace, `ViewportWorkbench`). Tokens: existing only (DESIGN.md): `station` for the
preview curve, `warning-viewport` for the largest-change tick, `focus-ring-viewport`, `danger-viewport` for a crossing.

### 11.1 Key screens (the mockup draws each)

1. **Where the verbs live** — Edit menu group and the three context menus (point; point at the floor; outline).
2. **Rebuild preview** — popover below the drawing; dashed preview curve and points; tick and plate at the largest
   change; readouts.
3. **Rebuild applied** — 4 points; selection on the nearest point.
4. **Add point** — the double-click and the new selected point.
5. **Remove point** — ⌫ on leading-edge point 6; the report.
6. **Refusal at the floor** — warning strip, dashed warning ring, nothing changed.

### 11.2 Drawing

- The preview curve: 2 px dashed (7, 4) `station`; its points as the Point glyphs with a dashed stroke; its polygon
  dashed at 70 % `station`. The current curve stays solid `foil`. The preview always shows; with Curvature on, both
  combs are drawn: the preview's at full strength and the current curve's at reduced opacity beneath it.
- The largest-change tick: 2 px `warning-viewport` vertical line spanning both curves at η\*, with a plate "8.97 mm".
- Display sampling (F-1): every curve's samples include η = 0 and η = 1 and at least 64 points per curve, at least 8
  per non-empty span (Core, `Planform.Project`).

### 11.3 Keyboard and pointer map (every pointer verb has a keyboard path)

| Verb | Pointer | Keyboard |
|---|---|---|
| Add point | double-click on a curve away from a point (starboard half in Plan); right-click on the curve → Add Point Here | with a point selected: Edit ▸ Add point, or ⌘K "Add point" (adds in the knot span at that point) |
| Remove point | right-click the point → Remove Point | ⌫ (and Delete on Windows) with the view focused and one point selected; Edit ▸ Remove point; ⌘K |
| Rebuild | right-click → Rebuild <Curve>… | Edit ▸ Rebuild <curve>…; ⌘K "Rebuild"; in the popover: ↑/↓ or typing change N, Return applies, Escape cancels, Tab walks stepper → Cancel → Rebuild |
| Value of a point | double-click a point (as built) | Return (as built) |

⌫ acts only when the view has focus, never in a text field (`EditVerbRouter` precedence). Double-click precedence: a
point within the 14 px hit radius wins over the curve; the curve accepts a double-click within 6 px of its drawn
line; the dashed control polygon is never a target. On the port half, a double-click writes COPY-198 and adds nothing.

**Menu placement.** In the views the verbs sit in the Edit menu with the point-type rows, because the views have no
mode bar; the section editor keeps them in its mode bar's Section ▾ (M1.2c §11.3). The gestures and the refusal style
are the same in both: double-click adds, ⌫ removes, a disabled row says why. F-7 asks M1.2c's owner to use the
operator's words (Add point, Remove point) in Section ▾ too.

### 11.4 Properties, Points pane, selection and copy

- **Properties**, the selected curve's group summary reads "cubic · N points" (as built in M1.2b2); the group gains a
  read-only row **Points** "N" with the note "From 4 to 16. Rebuild… sets 4 to 10." and a link **Rebuild…** that opens the popover.
- **Points pane** (M1.2c PNL, views mode): rows are added and removed in place; the selection follows §3.5 both ways.
- **Selection:** §3.5. Focus stays in the issuing view; after the popover closes, focus returns to the view on the
  selected point.
- **Copy (proposed COPY-190 to COPY-205; PVX records them in DESIGN.md §7):**

| ID | String | Where |
|---|---|---|
| 190 | "Added <curve> point <m> of <N>. Shape unchanged: largest change <d> <unit>. Points <m−1> and <m+1> moved to keep it." | strip |
| 191 | "Removed <curve> point <n>. Now <N> points. Largest change <d> <unit> at <s> mm from root." | strip |
| 192 | "A curve needs at least 4 points. The <curve> has <N>, so point <n> stays." | strip (warning), disabled-row reason "A curve needs at least 4 points." |
| 193 | "The root end can't be removed: the curve starts there." / "The tip end can't be removed: the curve ends there." | strip (warning), disabled reason |
| 194 | "This handle sets the curve's direction at the root. Move it, or rebuild the curve with fewer points." (tip: "at the tip") | strip (warning), disabled reason |
| 195 | "Point <n> is an anchor. Make it a control point first, then remove it." / "Point <n> is a handle of the anchor at point <a>. Make that anchor a control point first." | strip (warning), disabled reason |
| 196 | "The <curve> has 16 points, the most a curve can have. Remove a point or rebuild with fewer." | strip (warning) |
| 197 | "Too close to point <n> to add one here. Double-click further along the curve." · 197b "Removing point <n> would fold the curve. Move its neighbours apart first." | strip (warning) |
| 198 | "Add points on the starboard half, where the points are." | strip |
| 199 | "Rebuild <curve>: <a> → <b> points. Largest change <d> <unit> at <s> mm from root." | strip while the popover is open |
| 200 | "Rebuilt the <curve> with <b> points. Largest change <d> <unit> at <s> mm from root. ⌘Z undoes it." | strip |
| 201 | "Rebuild cancelled. The <curve> is as it was." | strip |
| 202 | "Anchor at point <n> becomes a control point." (one per anchor) | popover readouts |
| 203 | "With <b> points the leading and trailing edges would cross at <s> mm from root. Choose more points." | popover, Rebuild disabled with this reason |
| 204 | "The anchor at point <a> is now Smooth: its handles are no longer the same length." | appended to 190 or 191 |
| 205 | "Enter a whole number from 4 to 10." | popover field error, `aria-invalid` |
| 206 | "Remove points one at a time, so each change is measured." | strip, with several points selected |
| 208 | "These points are too close together to refit. Move them apart, then try again." | strip (warning) or popover, Rebuild disabled with it |
| 207 | "Kept exactly: root end <a> mm aft, tangent square to the centre line; tip end <b> mm aft." · "Tip direction: changed by <θ>°" · "Area <a> → <b> cm²" | popover readouts |

Units: rails and dihedral in mm (0.01), twist in ° (0.01), thickness in % t/c (0.01) — one precision per quantity
(UI-O). The menu labels follow the as-built case per surface: Edit menu sentence case ("Add point"), context menus
title case ("Remove Point", as `OpenPointMenu` writes "Make Anchor Point"). OI-PV-3 asks UXR to settle one case.

### 11.5 Component states

| Component | default | hover / focus | disabled | loading | empty | error | success | overflow |
|---|---|---|---|---|---|---|---|---|
| Edit rows / context rows | enabled per selection | row highlight | with the reason (tooltip; context menu shows it under the row) | — | no foil: rows hidden as built | — | — | — |
| Rebuild popover | stepper, readouts, Cancel, Rebuild | focus ring on each control | Rebuild `aria-disabled` + COPY-203 | "Measuring…" once, while the seven previews compute at open | — | COPY-205 under the field | COPY-200 in the strip | readouts wrap; buttons never truncate |
| Preview curve | dashed curve + points + tick | — | — | — | — | crossing marker `danger-viewport` | — | the tick's plate flips side near the view edge |
| Point glyph on refusal | selected | — | — | — | — | dashed `warning-viewport` ring r 17 | — | — |

Motion: none; state changes are instant.

### 11.6 Accessibility and performance (cheap floors, per the operator's priority)

- The popover is a `role=dialog` named "Rebuild <curve>"; its readouts are a description list; the strip is the one
  polite region (DR-STATUS-1). The stepper is a numeric field with − and + buttons (24 px targets).
- Every pointer verb has the keyboard path of §11.3; ⌫ is not a character key (SC 2.1.4 does not apply) and is
  view-scoped anyway.
- Point peers keep their names; after Add, the new point's peer is focused and announced by its name (M1.2b rule).
- Performance: each preview is a 401 × N least-squares solve and a 220-sample oracle — milliseconds (Inferred); the
  seven at open are measured by the readiness test on the worst case, 16 points, against the 100 ms edit budget of
  GEO-05. It never runs the
  certificate (BUDGET-DISPLAY); certification runs once at Apply.

## 12. Test plan

### 12.1 Triggered directives

D0 hygiene (names `Method_State_Outcome`, one behaviour each). D1 pure functions (the three algorithms, the oracle,
the refusal table: example and property tests). D2 persistence (receipts round-trip reopen, old-build
characterization). D3 UI (headless controller and canvas, rendered strip text, focus). D5 numeric (exact-path
tolerance 10⁻¹² relative; measured deviations against a direct recomputation, never against a literal). D7 telemetry
(fields recorded, no ids). D4 (async) is covered by the as-built `RunDirectCommandAsync` tests plus the STATUS-CLOBBER
row; D6 (security) by the forged-receipt row.

### 12.2 Rings and cost

The fast ring is `tools/run-tests.sh` (60 s budget; measured 34–38 s wall at its last change). The Core names here are
pure functions on small fixtures, about 1–20 ms each: about 0.5 s for the 45 fast PVC names (Inferred; SUITE-TIME
measures it). The Desktop names are headless, about 50–150 ms each: about 2.5 s for the 23 PVU names (Inferred). That is
about 3 s of fast-ring growth; the track brief stops if SUITE-TIME grows by more than 5 s. Readiness holds the
old-build characterization (about 60 s, once) and the worst-case preview timing.

**Numeric oracles.** "Within 10⁻¹² relative" means |Δv| ≤ 10⁻¹² × max |v| over the curve's oracle samples. No test
compares a number with the mockup's: `Rebuild_NewFoilTenToFour_EndsAndRootSquareExactChangeReported` checks
`MaxChange` against a dense 10,001-sample brute force (it must not exceed it, and must be within 10⁻³ of it), and the
`MaxChange_IncludesEveryKnotOfBothCurves` fixture puts its maximum at a knot between the uniform samples, so dropping
the knots turns it red.

### 12.3 Fixtures

`Fixtures/planform-verbs/`: the New foil (10-point rails); the Example (7 points); a 5-point rail with the root square;
a 7-point rail with one smooth anchor and one with one symmetric anchor; a 16-point 4.1 rail; an 11-point 4.1 rail; a
rail with uneven η that folds on Remove; a thin-tip pair whose rails cross at 4 points. Synthetic, labelled synthetic.

### 12.4 Named tests (the ledger; the checker reads this section and §9)

Each name protects one behaviour. Names that protect nothing are not listed.

**PVC — Core, FoilDSL, receipts** (fast ring unless marked).
`Parse_Channel41FourVertices_Parses` (PVC) · `Parse_Channel41ThreeVertices_DslCurve` (PVC) ·
`Parse_Channel40FiveVertices_DslCurve` (PVC) · `Parse_TangentRowOnFourVertexChannel_DslLock` (PVC) ·
`MakeControl_SevenPointsOneAnchor_GivesFive` (PVC) ·
`AddPoint_Boehm_ShapeUnchangedWithin1e12Relative` (PVC) · `AddPoint_NewIdAboveMaxNeighboursKeepIds` (PVC) ·
`AddPoint_AtExistingKnot_RefusedTooClose` (PVC) · `AddPoint_AtCeiling16_RefusedNamingCeiling` (PVC) ·
`AddPoint_PastTen_WritesHeader41InSamePatch` (PVC) · `AddPoint_NextToSmoothAnchor_RowStillHolds` (PVC) ·
`AddPoint_NextToSymmetricAnchor_RowBecomesSmoothReported` (PVC) · `AddPoint_FirstSpanRootMirror_RootSquareBitExact` (PVC; α = 0.013 on the New foil trailing root) ·
`AddPoint_DefaultPosition_SpanMidpointNeverAKnot` (PVC; cases: a control point, the root end, the tip end — the last span —, an anchor) ·
`RemovePoint_RefusalTable_OrderAndCopy` (PVC; one row per §3.4 condition, floor first on a 4-point handle) ·
`RemovePoint_Control_ChangeMeasuredAndOutsideSupportUnchanged` (PVC) · `RemovePoint_RootMirrorRowHolds` (PVC) ·
`RemovePoint_NextToAnchorHandle_TangentRowHolds` (PVC) · `RemovePoint_WouldFold_RefusedNothingChanged` (PVC) ·
`RemovePoint_BelowSix_WritesHeader41` (PVC) · `RemovePoint_ClusteredKnots_SolvesWithSpanSamples` (PVC) ·
`RemovePoint_NextToEndHandle_EndDirectionKept` (PVC) ·
`PointVerb_On41FileBackInto6To10_HeaderStays41` (PVC; Rebuild a 16-point rail to 8, Remove from an 11-point rail) ·
`Rebuild_SameCountSameKnots_NoRowBytesUnchanged` (PVC) · `Rebuild_SameCountNonGrevilleAbscissae_AppliesAndReports` (PVC) ·
`Rebuild_SchoenbergWhitneyFails_RefusedWithCopy` (PVC) · `Rebuild_NewFoilTenToFour_EndsAndRootSquareExactChangeReported` (PVC) ·
`Rebuild_KnotsFollowCurrentSpacing` (PVC) · `Rebuild_CountOutside4To10_DslCurve` (PVC) ·
`Rebuild_WithAnchors_AnchorsDroppedRowsRemoved` (PVC) · `Rebuild_PreviewEqualsApply_SameBytes` (PVC) ·
`Rebuild_RailsWouldCross_RefusedNothingChanged` (PVC) · `Rebuild_TipTurn_MatchesDirectAngle` (PVC) ·
`PreviewRebuilds_NoRowNoCertificate_BytesHistoryFreshnessUnchanged` (PVC; counts admission runs in the existing event ring; no new counter) ·
`MaxChange_IncludesEveryKnotOfBothCurves` (PVC) · `CurvatureBreaks_SmoothAnchor_OneBreakSimpleKnotsNone` (PVC) ·
`PlanformView_FourVertexCurve_SamplesIncludeEndsAndMinimumCount` (PVC) ·
`Verbs_UndoRedo_EachVerbBytesRestoredAndReapplied` (PVC; cases include Rebuild with anchors and a 4.0 → 4.1 header raise) ·
`Reopen_AfterEachVerb_BytesIdsAndUndoDepthIdentical` (PVC) · `Reopen_PointAddRemoveRebuildReceipts_Check` (PVC) ·
`Receipt_ForgedPointVerbReceipts_DocReference` (PVC; add id already in the parent, remove id not in the parent, rebuild `VertexId` ≠ `Curve`) ·
`ApplyPointCommand_SameOperationDifferentVerb_DocOperationConflict` (PVC) ·
`SelectAfterVerb_SurvivorKeptElseNearestEtaTieTowardTip` (PVC; on `PointOutcome.SelectId`, including Rebuild's null) ·
`PointVerbEvents_NoIdsOrPositions` (PVC) · `PointVerbEvents_RecordKindOutcomeCountsDeviation` (PVC; includes `rebuild.preview`) ·
`Reopen_OldBuildNewReceiptKind_RefusedFileUnchanged` (PVC, readiness: a characterization receipt run once on the
main commit before PVC merges, named in the receipt; cases a — a 4-point 4.1 source, b — `point-add`, c — `point-remove`,
d — `curve-rebuild`; SHA-256 of the file before and after; prints a PASS line).

**PVU — Desktop** (fast ring, headless).
`PlanCanvas_DoubleClickOnOutline_AddsPointSelectsAndFocusesIt` (PVU) · `PlanCanvas_DoubleClickOnPoint_StillOpensValueField` (PVU) ·
`PlanCanvas_DoubleClickOnPolygonLeg_NoAdd` (PVU) · `PlanCanvas_DoubleClickOnPortHalf_StatusSaysStarboard` (PVU) ·
`PlanCanvas_BackspaceOrDelete_RemovesSelectedControlSelectsNearest` (PVU) ·
`PlanCanvas_BackspaceAtFloor_WarningCopyNothingChanged` (PVU) · `PlanCanvas_BackspaceWithSeveralSelected_OneAtATimeCopy` (PVU) ·
`CommandTable_PointRemove_GesturesPerOs` (PVU) · `Properties_BackspaceInTextField_EditsTextNotPoint` (PVU) ·
`ContextMenu_Outline_AddPointHere_AddsAtPressPoint` (PVU) · `EditMenu_AddPoint_TypedPositionAddsSelectsNew` (PVU) ·
`EditMenu_PointVerbs_EnabledStateAndReasonPerSelection` (PVU) · `RebuildPopover_StepperReadoutsFromCore` (PVU) ·
`RebuildPopover_CancelAndEscape_NoRowBytesAndFreshnessUnchanged` (PVU) ·
`RebuildPopover_ReturnApplies_OneUndoRowPointSelectionCleared` (PVU) · `RebuildPopover_FocusReturnsToPlanOnClose` (PVU) ·
`RebuildPopover_CrossingDisablesRebuildWithReason` (PVU) · `RebuildPopover_TypedCountOutOfRange_FieldErrorNothingChanged` (PVU) ·
`RebuildPopover_Close_OneEventWithOutcome` (PVU) · `Controller_RefusalCopy_UsesCoreReasonNotCode` (PVU) ·
`Strip_VerbReports_FormattedFromOutcomeWithUnit` (PVU; COPY-190/191/200 built from `PointOutcome`, a twist case in °) ·
`ElevationView_DoubleClickOnTwistCurve_AddsPoint` (PVU) · `StatusStrip_VerbReport_NotClobberedBySampling` (PVU).

**PVX — readiness.**
`Readiness_RebuildPreviews_Worst16PointCurveUnder100ms` (PVX, readiness, wall time on macOS for the seven previews).

## 13. Decisions, findings and open items

### 13.1 Decision requests — the DR-PV batch (one batch; the operator rules DR-PV-1 to DR-PV-5)

| ID | Question | Options | Evidence | Recommendation |
|---|---|---|---|---|
| **DR-PV-1** | Ruling 62 says "4 to 10". Does Add point stop at 10, or keep Amendment 1's 16 under 4.1? | A: 16 for Add (and Make anchor), 4–10 for Rebuild · B: 10 everywhere | New foil ships at 10 points (`FoilSource.cs`:403); under B, Add point never works on a New foil, and Make anchor already goes to 16 (`AuthoringSession.cs`:1142). Under A a 14-point curve cannot be rebuilt to 12 (the popover says the range is Ruling 62's) | **A** |
| **DR-PV-2** | Which FoilDSL version allows the floor of 4? | A: 4.1 (the existing gate) · B: a new 4.2 · C: any version | A: M1.2b-era builds say `DSL-CURVE`, older ones `DSL-VERSION`; the file is unchanged. B: every build since M1.2b says `DSL-VERSION`, at the cost of a third version and a second header writer. C: "4.0" would mean two things | **A** |
| **DR-PV-3** | Does Rebuild refuse above a tolerance? | A: no gate; the change, where it is and the tip turn are shown before Apply · B: a tolerance field, Apply off above it (A4.2's Fair contract) · C: the 10 µm A4.6 acceptance | Rhino's Rebuild shows the deviation without a gate; the operator asked for fewer points knowing the shape moves; C refuses 10 → 4 on New foil (8.97 mm) | **A**; the spec owner scopes A4.6 to conversions |
| **DR-PV-4** | Which curves get the verbs? | A: all five channels · B: the two rails only | one parser rule, one point layer, one command path (§3.7); B adds a per-name rule and two disabled reasons | **A** |
| **DR-PV-5** | What does rail Rebuild do with anchors? (Marine-CAD at the gate: the section editor's Rebuild keeps them) | A: drop them, listed in the popover before Apply · B: keep each anchor as a knot triple with its tangent row, so N ≥ 4 + 3k | A is what makes 4 points reachable; a 4-point curve cannot hold an anchor anyway. B matches the section editor but needs knot placement around the anchors and refuses small N | **A** now; B later if anchored rails are common |

**Decided in the design (the reasons are written; the operator may overturn any):**

| ID | Decision | Reason |
|---|---|---|
| DR-PV-6 | Rebuild's knots follow the current spacing (not even) | New foil trailing edge: 5 points 1.36 mm vs 5.61 mm; same count 0.000 vs 1.75 mm (mockup) |
| DR-PV-7 | Remove = drop one knot, refit the two replacement points with every other point and the touching rows pinned | Tiller removal alone breaks the root square and tangent rows; a full refit moves every point (§6.4) |
| DR-PV-9 | Remove point refuses the end handles | they set the end tangents, and under the root mirror the root square; Rebuild covers "fewer points" |

Not decisions for this slice (Ruling 62 named three verbs): Fit points (§6.3, OI-PV-1) and a New foil with fewer rail
points (§0.2). The "Both edges" Rebuild is OI-PV-2.

### 13.2 Findings (for their owners)

- **F-1** `Planform.Project` samples 8 points per span from half a step in (`PointModel.cs`:132-143): a 4-point curve
  draws as 8 segments that miss the root and tip by 31.25 mm. Fixed in PVC (§11.2).
- **F-2** ADR-0001's fixture `spikes/degree-adr/fixture.json` was never committed (no git history). SPK commits one.
- **F-3** The controller's direct-command catch drops `ContractError.Reason` (`WorkbenchController.cs`:995), so the
  as-built ceiling copy "Making this an anchor needs 3 more points…" (`AuthoringSession.cs`:1144) never reaches the strip.
  Fixed in PVU for every point command.
- **F-4** Spec owner: A4.1 "six to ten allowed", A4.2 "the floor is the record's six vertices", GEO-05 "fewer than
  p + 2" → floor 4 = p + 1 under 4.1; CAD-21 / B1 "Insert CV · Delete CV" are named "Add point · Remove point" in the
  UI (Ruling 62's words).
- **F-5** `MaxPointDelta` (`AuthoringSession.cs`:1067-1090) samples 201 η and no knots, short of A4.5. PVC replaces it
  with `MaxChange` for every point command.
- **F-6** (M1.2c owner) Section Rebuild keeps anchors as KKT rows and takes Tolerance and PreserveEnds (M1.2c §3.4,
  :583); rail Rebuild drops anchors and has neither (DR-PV-5). One word, two contracts, until the operator rules.
- **F-7** (M1.2c owner) Section ▾ says "Insert point… / Delete point" (M1.2c :126-128); the operator's words are "Add
  point / Remove point" (Ruling 62). One vocabulary across both editors.

### 13.3 Open items

- **OI-PV-1** Fit points (§6.3).
- **OI-PV-2** A "Both edges" (or several curves) Rebuild; Rhino's Rebuild takes several curves at once.
- **OI-PV-3** Menu case: Edit rows are sentence case, context rows title case (as built). UXR settles one.

## 14. Build tracks (exclusive file ownership)

All tracks start **after M1.2c joins** (Ruling 62). They touch these files that M1.2c owns until then:
`FoilSource.cs`, `PointModel.cs` (SPT); `AuthoringSession.cs` (SDR);
`Contracts.cs` (PRE); `WorkbenchController.cs`, `Selection.cs` (CTL); `CurvePointLayer.cs`, `ElevationView.cs` (EDT);
`PropertiesView.cs`, `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, `Shell/StatusStrip.axaml(.cs)`, `PointsView.cs`
(PNL); `DESIGN.md` (UXR). Briefs: foreground; the Return section; two repair cycles; `tools/run-tests.sh`, then
`tools/check-named-tests.py <track> --design docs/design/planform-point-verbs.md` (not for SPK, which has no names);
`AGENT_SESSION` exported.

| Track | Harness | Owns (exclusive) | Depends on | Box | Exit |
|---|---|---|---|---|---|
| **SPK** fairness and rebuild evidence | Claude (Computational Geometry judgement) | `docs/proof/planform-verbs-fairness/**` (committed script, fixture, output); ADR-0001 Amendment 2's evidence table | M1.2c joined | 30 min (no same-class prior; measured time recorded) | the committed script, re-run, reproduces its table: the five Example curves and the New foil rails at d3 · 4 and d3 · 5 — κ′ energy, pieces, anchor residual, Rebuild deviation, support, lever, comb sign changes |
| **PVC** Core, FoilDSL, receipts, CLI | Grok or Codex | `FoilSource.cs`, `ChannelEdits.cs` (new), `PointModel.cs`, `AuthoringSession.cs` (point-command region, `EditReference`, replay guard, `PreviewRebuild`, events), `docs/specs/foildsl.md` §5 + `docs/examples/foildsl/`, `PointVerbTests.cs` (new), `ReopenPointEditTests.cs` additions, `Fixtures/planform-verbs/`, `docs/proof/planform-verbs-old-build/` | M1.2c joined | 135 min (P1 45 × 3, Ruling 54) | PVC names PASS; planted mutant (an alpha off by one span turns `AddPoint_Boehm_ShapeUnchangedWithin1e12Relative` red) |
| **PVU** Desktop | Codex | `WorkbenchController.cs`, `PlanCanvas.cs`, `ElevationView.cs`, `CurvePointLayer.cs`, `RebuildPopover.axaml`(.cs) (new), `ModelArea.axaml`(.cs), `PropertiesView.cs`, `PointsView.cs`, `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, Desktop tests | PVC | 141 min (D3a 47 × 3) | PVU names PASS; the UI-DEAD-CONTROL walk green with the new rows; planted mutants: dropping `Reason` in the catch turns `Controller_RefusalCopy_UsesCoreReasonNotCode` red, removing the slot guard turns `StatusStrip_VerbReport_NotClobberedBySampling` red |
| **PVX** review and polish | Claude | `DESIGN.md` (COPY-190..205, the Rebuild popover component row), `docs/reviews/planform-verbs-native.md` (new), readiness rows | PVU | 60 min | marine-CAD re-review on the native build; the six screens captured from the packaged app and compared with the approved mockup (a review receipt, not a test); `Readiness_RebuildPreviews_Worst16PointCurveUnder100ms` PASS |

**New foil default (Ruling 64, owned by PVC).** `FoilSource.NewDefault` ships **4** control vertices on the
leading-edge and trailing-edge rails (today 10, `FoilSource.cs`:403); the other three channels keep their default
unless SPK's numbers give a reason (report it, do not change it silently). Consequences PVC must cover, each with a
named test written red first: New foil writes the FoilDSL **4.1** header (4.0's floor is 6); the New foil shape
changes — report the measured rail deviation from today's 10-point New foil (the mockup puts it at TE 8.97 mm,
LE 2.99 mm at 467.5 mm from root) and the tip direction change; every existing test or fixture that assumes a
10-point New foil rail (the demo in §0.1 included) is updated with its new number, never deleted; the M1.2c
ADR-0001 Amendment 1 note "New foil ships at 10, so under 6–10 it holds no interior anchor" is superseded (at 4 it
holds none either; Add point first). SPK measures the 4-point New foil rails (κ′ energy, comb) so the default is
evidence, not taste. PVX recaptures the §0.1 demo on the 4-point default.

**Order:** {SPK ∥ PVC} → PVU → PVX → join. Critical path PVC → PVU → PVX = 336 min of boxes; measured priors suggest
about 2.5 h of real time (Inferred). Width 2.

### 14.2 Seams

| Seam | Rule | Fallback |
|---|---|---|
| **S-PV-1** `AuthoringSession.cs` | PVC edits the point-command region, `EditReference`, the replay guard and `Run` events only | if M1.2c's S-3 contract step is still open, PVC rebases on it |
| **S-PV-2** `PointOutcome` / `CurveView` | new members are non-positional with defaults (M1.2c S-6 shape) | — |
| **S-PV-3** `PointsView.cs` | PVU edits row insert/remove and selection only | PNL's layout untouched |

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| Native envelope → reopen | T: a forged `point-remove` / `curve-rebuild` receipt | mitigate | closed `rail` set; the new `EditReference` arms | `Receipt_ForgedPointVerbReceipts_DocReference` (PVC) |
| FoilDSL file → parser | T: a hand-made 4.0 file with 4 points; D: a 3-point channel | mitigate | the version-gated floor; `DSL-CURVE` | `Parse_Channel40FiveVertices_DslCurve` (PVC), `Parse_Channel41ThreeVertices_DslCurve` (PVC) |
| Telemetry ring | I: point ids or positions leak | mitigate | kind, counts, deviation, durations, codes only | `PointVerbEvents_NoIdsOrPositions` (PVC) |
| History | R: an edit without attribution | accept | single local user; the receipt names the curve and the point | — |
| — | S, E | not applicable | no authentication, privilege levels or network | — |

## Privacy analysis (LINDDUN-lite)

This component touches no personal data. Geometry, counts, deviations, durations and error codes only; no path,
account or free text. The new events carry no ids or positions (`PointVerbEvents_NoIdsOrPositions` (PVC)).

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| point-verb events (local ring) | D: disclosure through logs | mitigate | no ids, names or positions | in-memory ring of 256; gone at exit |

## Conformance notes

- **ADR-0001:** Amendment 2 written with this design (floor 4 under 4.1; evidence for 4 and 5 named; nothing run).
- **ADR-0002:** one authority, the bytes. The preview candidate is never kept; Apply recomputes from the bytes.
- **ADR-0005:** types stay derived from knots; every verb keeps tangent rows true or rewrites them in the same patch and
  reports it (§6.4); Rebuild removes rows of anchors it drops, as ADR-0005 §5 requires.
- **ADR-0007:** each verb is one accepted row; the Rebuild preview is not a draft (no step list), so there is nothing to
  recover. A4.2's "always as a draft (Return applies, Escape cancels, one undo item)" holds at the UI: the popover is
  the draft surface, Return applies, Escape cancels, one undo item.
- **ADR-0009:** three `CommandTable` rows; menus and the palette read them.
- **Deviations:**

  | ID | Deviation | Why |
  |---|---|---|
  | D-1 | no CLI change | m12b D-3 precedent; `inspect --json` already prints every channel's points and roles |
  | D-2 | Rebuild has no tolerance gate (pending DR-PV-3) | A4.2 calls Rebuild "Fair with a chosen vertex count", and Fair has Apply off above its tolerance |
  | D-3 | Remove point refuses end handles | A4.2 allows any Delete CV above the floor; decision DR-PV-9 |
  | D-4 | Add point and Remove point apply at once, with no draft | A4.2 says Delete CV is "always as a draft"; Rhino and vector tools apply at once, the change is reported and ⌘Z restores it exactly (spec owner, with F-4) |

## Flagged risks and residual unknowns

- **R-1:** a 4-point rail cannot hold the New foil's elliptical tip (8.97 mm measured in the mockup). This is the
  operator's trade, made visible before Apply; SPK measures it on the record.
- **R-2:** at 4 and 5 points every point moves the whole curve (support is global). Users used to local edits may be
  surprised; the comb shows it. Measured by SPK, not asserted.
- **R-3:** fewer thickness points reduce how many stations can hold their own t/c (§3.7); named in the preview.
- **R-4:** M1.2c is still building (SPT, SDR, DSP in flight at `4fa5a3f`). Its file shapes can move; §14 rebases on
  the joined M1.2c.
- **Inferred until built:** every rendered and timing claim; the mockup's deviations are its own JavaScript evaluator,
  the same algorithms, not the build's numbers.

## Status & next action

| | |
|---|---|
| **Completed** | Detailed design of the three verbs (data model, receipts, contracts, algorithms, failure modes, telemetry, UI, test ledger, tracks); ADR-0001 Amendment 2; the mockup with six screens and captures; the DR-PV batch; the gate (four lenses, repair cycle 1) |
| **Remaining** | the operator's look at the mockup and rulings on DR-PV-1..5; the lenses not convened (Patterns Expert, Data & Persistence, UX & Accessibility); the spec owner's F-4 amendments; M1.2c's join; then SPK ∥ PVC → PVU → PVX |
| **Best next action** | show the operator `docs/mockups/planform-point-verbs.html` with the DR-PV batch |

## Gate record

`GATE design · 2026-10-03 · Computational Geometry (hard veto, narrow), Test Architect (hard veto), Marine-CAD UX (soft veto), Simplifier (soft veto) · each run as a separate Adversary-Mode agent (Opus 5.5) · criteria met: data model first (no new record field; three receipt kinds expand-only with writer and reader), E7, contracts with sources, failure modes with tests, STRIDE-lite, LINDDUN-lite, telemetry, UI with states and copy, the named-test ledger (checker extract: 0 errors) · verdict: PASS WITH CONDITIONS (all conditions written with named tests) · author did not self-clear · repair cycles: 1 of 1 (the Coordinator's box)`

| Lens | Initial verdict | Main findings | Repair cycle 1 | Re-review |
|---|---|---|---|---|
| Computational Geometry (hard veto, narrow) | Pass with conditions; no veto predicate | Boehm breaks the root square by one ulp in about 4 % of first-span inserts (the certificate checks exact equality). No Schoenberg–Whitney guard: clustered knots make the refit singular. "Same count = identity" false off Greville abscissae. A second η inversion beside `ChannelEvaluator` (GEO-B). End-direction change measured but not reported. Minors: the double-knot precondition, the curvature rule's units, the 5-point anchor count, the oracle is a sampled lower bound | Bit-exact rule for equal inputs (+ test, and `MakeAnchor`'s loop). Per-span Gauss samples, Schoenberg–Whitney check, COPY-208. Identity only within the A4.5 identity tolerance (+ non-Greville test). Every inversion through `ChannelEvaluator`, fit rows on the candidate. Remove pins end handles to their tangent line; Rebuild reports root and tip turns and the area. All minors written into §6.4 and the ADR | Not re-reviewed (box). Its conditions are written as stated, each with a named test |
| Test Architect (hard veto) | **BLOCK** (3 Blockers, 10 Majors) | Redo and save/reopen identity untested; "header never lowers" untested; "the preview never certifies or changes state" untestable as written. SPK unreachable by the checker; an unbuildable MakeControl test; a wrong derived cancel rate; Add's keyboard route and Windows Delete untested; selection and same-count cases; strip copy not asserted from operands; latest-wins race; PVU had no planted mutant | Every Blocker and Major has a named test or a cut (§9, §12.2–12.4, §14); SPK excluded with a reproduce-the-table exit; MakeControl at 7 → 5; one close event per popover; the latest-wins race removed (seven previews at open) | **Veto cleared** (Blockers traced to names; checker 0 errors). Conditions at implementation: red before green on each track, including the planted mutants; a Proof Pack at the join |
| Marine-CAD UX (soft veto) | Pass with conditions; no veto | "Rebuild" means two contracts across the editors (sections keep anchors). Vocabulary differs (Insert/Delete vs Add/Remove). Add point had no typed position. Minors: 4–10 vs 16 on one screen; selection jumping after Rebuild; Remove applies without a draft; tip direction not listed; comb comparison; multi-select copy; units; hit tolerance | DR-PV-5 (anchors in Rebuild) and F-6/F-7 for M1.2c's owner; §0.2 name corrected; menu placement explained (§11.3). "Add point…" opens a From-root field. Hint states Ruling 62's range; Rebuild clears the point selection; D-4; tip turn readout (35.03°); both combs; COPY-206; `<unit>` in COPY-190; 6 px curve hit tolerance, polygon not a target. The mockup was re-captured | Not re-reviewed (box); no veto was raised |
| Simplifier (soft veto) | **BLOCK** (soft) | `CurveView.Floor` by version would refuse Remove at 6 on a 4.0 New foil; latest-wins preview machinery for seven possible values. Minors: `RebuildPreview` members, preview on the commit path, same-count identity, two owners of selection, two deviation fields, rows are pins, thickness line, DR batch size, receipt check, refusal tests | Constants Floor 4 / Ceiling 16 (no field; CLI change cut). Seven previews at open. `AnchorsRemoved` and `BaseAcceptedId` cut (Undo is refused while Busy, `WorkbenchController.cs`:1547). Preview through `EvaluatePointCommand`. Core owns `SelectId`. One deviation field. Rows as pins. Thickness line cut. Operator DRs reduced to five. **Kept, with a defence:** the curvature-break readout (A4.3; non-zero before when anchors exist; the Geometry lens asked for it on Remove too) | **Soft veto cleared.** Two minors applied: the glossary names the verbs' limits; the preview test reads the existing event ring, no new counter |

**Not convened, with reason:** Patterns Expert (the brief named four lenses; the patterns are named in §6.1 and the
Ladder in §6.2), Data & Persistence (the receipt kinds and the 4.1 gate are expand-only and follow M1.2b/M1.2c
precedent, but a D&P lens has not attacked them; *pending*), UX & Accessibility (cheap floors per the operator's
priority; *pending* before PVU).
