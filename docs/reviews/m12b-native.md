---
id: review-m12b-native
title: Native review — M1.2b CAD point editing on the Plan view
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation — U3 (M1.2b)
tags: [native-ui, m1.2b, plan-view, points, operator-run, review]
links:
  - {to: design-m12b-points, rel: documents}
  - {to: proof-m12b-native, rel: refines}
  - {to: review-app-shell-native, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-30
summary: >-
  Operator-run native session on build c43711a, 1 October 2026. The operator walked the twelve §0.1 demo steps; ten
  pass and two pass with a defect. Five defects (D-1 to D-5), five design findings (F-1 to F-5), six observations and
  one open question. D-4 (MAC and Mean chord go blank after the first edit) and F-1 (Properties is not a property grid)
  are the two that matter most. M1.2b stays open until D-1 to D-4 are fixed under test.
review-suggested: []
---

# Native review — M1.2b CAD point editing on the Plan view

Operator session, 1 October 2026, on `feature/ui-cad-direction` at `c43711a` (the U3-attach join; the M1.2b code is
`a44f951`, readiness green). Design: [`docs/design/m12b-points.md`](../design/m12b-points.md) §0.1 (the demo script)
and §11.3 (interaction map). The automated attach record is
[`docs/proof/m12b-native/index.md`](../proof/m12b-native/index.md): all twelve attaches were blocked
(`cgWindowNotFound`), so this record is the operator's own run, with the Coordinator taking screen captures.

**Disposition: M1.2b is OPEN.** Every demo step was performed by the operator in the packaged app. Ten steps pass.
Steps 6 and 11 pass in part. D-4 also leaves step 4's "MAC changes during the drag" unproven. M1.2b closes when D-1 to
D-4 are fixed, each with a test that fails first, and steps 4, 6 and 11 are re-run.

## 1. Build and launch

| Item | Value | Confidence |
|---|---|---|
| Source | `c43711a`, clean tree `CFD-Workbench-feature-ui-cad-direction` | Verified |
| Build | `dotnet publish src/CfdWorkbench.Desktop -c Release -r osx-arm64 --self-contained false` (exit 0) | Verified |
| Package | `tools/package-application.py --platform macos` → unsigned, framework-dependent `CFD Workbench.app`, bundle `com.cfdworkbench.desktop` (exit 0) | Verified |
| Launch guard | `docs/coordination/review-launch-guard.py` empty `blocked`, `matched`, `retained`, `unknown` before both launches | Verified |
| Launch | `CFDW_REVIEW_MODE=1`, persona `designer`, state `empty`, theme `system`. Relaunched once after the operator quit by mistake at step 8 | Verified |
| Evidence | Operator reports (paraphrased below), three Coordinator captures cropped to the app window in [`operator/`](../proof/m12b-native/operator/) | Verified |

## 2. Demo steps (§0.1)

| Step | What was checked | Result | Evidence |
|---|---|---|---|
| 1 · New foil / Plan | Plan tab fills the model area; planform, rails, ten points per starboard rail, stations, scale bar; Properties ends with the Wing block | **Pass** | Operator; [start.png](../proof/m12b-native/operator/start.png), [plan-select.png](../proof/m12b-native/operator/plan-select.png) |
| 2 · Hover / probe | Probe reads η, span, % half-span, LE, TE, chord; point tooltip names edge, index, type, span, aft | **Pass** (O-1, O-2) | Operator; plan-select.png |
| 3 · Select point | Glyph ringed; Properties shows title, Type, Span and Aft in mm | **Pass** | Operator; plan-select.png |
| 4 · Drag / Undo | Drag follows the pointer; one undo step; ⌘Z restores; typed Span/Aft entry works | **Pass**, live MAC unproven (D-4) | Operator |
| 5 · Nudge | ↓ / ⌘↓ / ⇧↓ steps | **Pass** | Operator. Held-key single undo not separately confirmed |
| 6 · Edge crossing | Point snaps back on release | **Partial** — D-2 | Operator |
| 7 · Point type / tangents | Control ⇄ Anchor in Properties; Smooth, Symmetric, Corner; handle co-motion; undo per change | **Pass** (F-4, F-5, O-6, D-5) | Operator; [anchor-handle.png](../proof/m12b-native/operator/anchor-handle.png) |
| 8 · Save / reopen | Save, quit, open: the file is the same | **Pass** | Operator; saved file read back (§4) |
| 9 · Typed chords | Root 152.09, root 190 (warning), tip `15 cm` and `#root_chord * 0.1`; status copy matches | **Pass** | Operator |
| 10 · Keyboard only | Tab order, nudge, Return/Escape, no trap, ⌥-arrows, ⌘= / ⌘−, ⌘0 | **Pass** | Operator. VoiceOver trace not run |
| 11 · Pointer navigation | Two-finger scroll pans, pinch and wheel zoom, ⌥-arrows pan | **Partial** — D-1, D-3 | Operator. Middle-drag not separately confirmed |
| 12 · Curvature comb | Comb on the selected rail, live during a drag, break at a Corner | **Pass** | Operator |

## 3. Findings

### 3.1 Defects (the build does not do what the design says)

| ID | Finding | Design reference | Evidence | Severity |
|---|---|---|---|---|
| **D-4** | MAC and Mean chord show `≈ —` after the first accepted edit and never come back: not after undo, not on a fresh New foil once a chord is typed, not on reopen of an edited file. Before any edit they read ≈ 99.60 and ≈ 107.11 | §0.1 step 4 ("MAC, Area and Aspect ratio change during the drag"); §11.4 Wing block | Operator confirmed twice; plan-select.png (numbers before edits) vs anchor-handle.png (`≈ —`) | **High**: MAC is the quantity the slice's demo reads |
| D-1 | Shift-drag on empty canvas does not pan | §0.1 step 11; §11.3 Pan row | Operator | Medium |
| D-2 | The red edge-crossing marker appears after the snap-back instead of during the drag | §0.1 step 6 | Operator ("the red marker is after the snap back"). Whether it also lingers is unconfirmed | Medium |
| D-3 | Control-click on a point opens no context menu; it switches Properties between the Wing and the point | §0.1 step 11; §11.3 Point type row | Operator. Source: no canvas `ContextMenu` in `src/CfdWorkbench.Desktop` (only dock tabs, `ShellHost.cs`:702, and Browser rows, `BrowserPane.axaml.cs`:159) | Medium |
| D-5 | Anchor → Control → Anchor is not a round trip. `MakeAnchor` inserts a fresh knot to multiplicity 3 (+3 vertices); `MakeControl` (`AuthoringSession.cs`:1119) removes two vertices and two knot copies without refitting. Each cycle leaves one extra vertex (10 → 13 → 11 → 14) and moves the curve | §1 A4.15, CAD-15; ADR-0005 | **Inferred** from source. The operator saw 14 points but may also have made more than one anchor, so the observation does not prove it. Needs a reproducing test | Medium |

### 3.2 Design findings (the build matches the design; the design is wrong or short)

| ID | Finding | Operator's words / evidence | Next |
|---|---|---|---|
| **F-1** | The Properties pane reads as a column of text, not a property sheet. Labels sit above values, derived rows have no units, and sections have no grid | "doesn't look like a properties sheet but just a bunch of text: take inspiration from coding tools and editor tools and CAD tools property sheets, specifically property sheets in VS Code and Adobe Premiere" | `/ui-design` pass for a property grid before M1.2b2 reuses the pane |
| F-2 | A plain drag on empty canvas should pan | "I should be able to drag" | Design change to §11.3: plain drag on empty canvas pans; click still clears the selection |
| F-3 | The default view places the wing under the Tracing probe box | "the default view is under the status box in the top right of the view (I like that box)" | Fit must treat the probe as an obscuring rectangle (as §11.3 already does for focus) |
| F-4 | Tangent kind is hard to find: three unlabelled buttons (`PropertiesPane.axaml`:55–59), shown only when the anchor itself is selected (`PropertiesPane.axaml.cs`:310). Selecting a handle hides its anchor's kind | "I don't see where to switch the tangent to Symmetric or to Corner" | Label the group; show the parent anchor's kind when a handle is selected. Lands with F-1 |
| F-5 | Make Anchor adds three visible points, so it reads as "add a point" | "I thought that was adding a new point" | Insert point is OI-2. Until then, the type change must say in the status line that it adds handles, not points |

### 3.3 Observations

| ID | Observation | Evidence |
|---|---|---|
| O-1 | The probe box covers the leading-edge rail and its points on the starboard half | plan-select.png |
| O-2 | The point tooltip is clipped at the canvas right edge | plan-select.png |
| O-3 | The tip station chip is clipped at the right edge; the tip points overlap | plan-select.png |
| O-4 | Root and Tip chord fields have no `mm` unit; Span does | plan-select.png |
| O-5 | A fresh foil's status line reads "No point change." | plan-select.png |
| O-6 | With a handle selected, the header and helper text describe the anchor ("point 7 of 14"), while the type row says "Anchor handle" | anchor-handle.png |
| O-7 | Start screen banner "Open Example or a .foil / .cfdw.json file." above "Start a foil"; two blank square buttons at the top right of the left pane | start.png |

### 3.4 Open question for the operator

**Q-1. Handles on every point?** The operator asked whether control points should have handles too. The design
(A4.15, CAD-15, ADR-0005) follows Rhino and Alias: a control point is an off-curve vertex with no handles, and only an
on-curve anchor has handles. Handles on every point is a different curve model (Bézier path, as in Illustrator).
**Ruled (Ruling 58, 2026-10-01): keep anchors only.** The curve model does not change; F-5 and OI-2 carry the
"adds a point" confusion.

## 4. Saved file read back (step 8)

`~/Documents/foil.cfdw.json`, 560 KB, format `cfdw-project-1`, holds 38 accepted source revisions (the full history).
The current revision starts `foildsl "4.1"`. Earlier revisions carry `tangents` rows (`smooth`, `symmetric`). The last
two carry none, which fits the operator undoing to all-control points before saving (a Corner writes no row by design).
Verified by decoding the file; the reopened Foil source tab was not captured.

## 5. Not covered by this session

- VoiceOver trace and the point peers' accessible names (N-B10's screen-reader half).
- Held-key nudge as one undo step; middle-drag pan.
- Theme variants other than `system`; reduced motion.
- Any automated attach: the supported attach is still blocked (`cgWindowNotFound`, [proof record](../proof/m12b-native/index.md)).

## 6. Next actions

1. Root-cause D-4 (`/investigate`), then one bounded fix track for D-1 to D-4 and F-2 to F-4, each with a test that
   fails first. Write a reproducing test for D-5 before deciding on its fix.
2. `/ui-design` on the property grid (F-1), referencing VS Code settings and Premiere Pro Effect Controls, before
   M1.2b2 starts.
3. Operator decisions: done. Ruling 58 keeps anchors-only handles (Q-1) and accepts ADR-0010, which opens M1.2b2.
4. Re-run steps 4, 6 and 11 on the fix build, and close M1.2b.
