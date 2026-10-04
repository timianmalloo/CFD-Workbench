---
id: review-m12c-native
title: Native review — M1.2c section editor (UXR polish and the operator's native-look checklist)
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation — UXR (M1.2c)
tags: [native-ui, m1.2c, section-editor, review, operator-run, captures]
links:
  - {to: design-m12c-section-editor, rel: documents}
  - {to: review-m12b-native, rel: relates-to}
review-by: 2026-10-31
summary: >-
  Track UXR, 4 October 2026. The deviations EDT's and PNL's captures showed are fixed, except where §3 says why not.
  Screens 2, 2b, 2c and 3 were recaptured from the built app in light and dark mode (docs/proof/m12c-uxr). The
  marine-CAD re-review raised one soft veto: the comb pointed into the foil. It is fixed under a red-first test and
  the veto is cleared. Three findings go to the operator: handle Length is entered in mm, the comb is sparse, and the
  Points y colour. The packaged app is ready for the operator's native-look walk (§5). The native rows N-12C-1 to
  N-12C-11 are pending until the operator attaches their receipts.
review-suggested: []
---

# Native review — M1.2c section editor

Track UXR on `feature/m12c-uxr`, 4 October 2026, at the integration head with EDT and PNL merged. Authority:
[`docs/design/m12c-section-editor.md`](../design/m12c-section-editor.md) §0.1, §11 and §14 (UXR row). The approved
look is [`docs/mockups/m12c-section-editor.html`](../mockups/m12c-section-editor.html) (paired version, Rulings 60
and 61); its renders are in [`docs/proof/m12c-paired-mockup/`](../proof/m12c-paired-mockup/).

## 1. Captures

The captures come from the built app through `CFDW_EDT_CAPTURE=<dir>`, which is off by default and never part of the
gate. Each run uses the Precision workspace and the Example foil. The window **opens at 1280 × 800**, the app's launch
size.

The EDT captures resized a 1400 × 1000 window to 1280 × 800. That shrank both side bars (left 192 px, right 237 px),
which explains most of the PNL clipping they showed. Captures now open at the launch size: the left bar is 205 px and
the right bar is 258 px.

| Screen | Light | Dark | Mockup |
|---|---|---|---|
| 2 · point 4 made an anchor on both surfaces, Horizontal, Fit Selection | [edt-s2-light](../proof/m12c-uxr/edt-s2-light.png) | [edt-s2-dark](../proof/m12c-uxr/edt-s2-dark.png) | [light-s2](../proof/m12c-paired-mockup/light-s2.png) |
| 2b · a paired x move (point 10 → 60 %) | [edt-s2b-light](../proof/m12c-uxr/edt-s2b-light.png) | [edt-s2b-dark](../proof/m12c-uxr/edt-s2b-dark.png) | [light-s2x](../proof/m12c-paired-mockup/light-s2x.png) |
| 2c · Anchor → Control refused (refit over 10 µm) | [edt-s2c-light](../proof/m12c-uxr/edt-s2c-light.png) | [edt-s2c-dark](../proof/m12c-uxr/edt-s2c-dark.png) | [light-s2r](../proof/m12c-paired-mockup/light-s2r.png) |
| 3 · the surfaces cross; Finish blocked | [edt-s3-light](../proof/m12c-uxr/edt-s3-light.png) | [edt-s3-dark](../proof/m12c-uxr/edt-s3-dark.png) | [light-s3](../proof/m12c-paired-mockup/light-s3.png) |

## 2. What now matches the mockup (Verified in the captures)

- **Mode bar.**
  - The bar is 32 px with 24 px buttons and 0 8 px padding. The labels no longer clip.
  - The title reads "Editing **Root** section", with the station in the accent colour.
  - The scope chip is a hairline pill: "Shared with Tip · <u>Make unique to Root</u>". The link runs
    `section.make-unique`. When no other station shares the section, the chip reads "Only <station> uses this section".
  - A pressed toggle uses the soft fill with a hairline border; Fluent's blue is gone.
  - Cancel is outlined and Finish is the one primary button. Finish turns soft and muted when it is off.
- **Reason box.**
  - The box sits top-right under the mode bar, as the mockup's `.why` does. It has a 3 px danger rail and danger
    text. It no longer covers Fit or Fit Selection, or the refit marker's label.
  - It shows the strip's copy (COPY-187), not Core's text.
  - It goes away when the check passes (**Major 1**; see §4).
- **Plate line.**
  - The view plate ("Section · Root · 0.00 mm from root · display", with the station underline) sits at the left.
  - The probe at the right is now a viewport-soft plate with viewport-ink text, not a separate low-contrast box.
  - When a reason shows, the probe sits under it, as in the mockup's screen 3.
- **Chord axis.**
  - A viewport-grid line every 10 % (every 5 % past 60 % zoom), labelled "<n> %" just above the bottom.
  - Two fixes along the way: the 0 label no longer reads "-0 %", and the last label reads "100 %", not "100".
  - "Upper" and "Lower" label the trailing edge at full chord.
- **Comb plate.** "Comb · auto scale · <n> teeth clipped (×)" sits at the bottom left while Curvature is on.
- **Comb drawing.**
  - The teeth point outward (the soft veto in §3 is fixed).
  - Teeth are drawn in viewport-mute at half strength, as in the mockup.
  - A dashed station mark shows the break at each anchor.
- **Crossing marker.** A 4 px dashed danger-viewport line runs along both curves over the crossing, as in the
  mockup's `crossMark`. It replaces the two dashed circles.
- **Screen 3.** The capture now shows the full chord view (Fit). The strip still offers Show.
- **Station strip.**
  - Each 170 px thumbnail draws its station's section outline on the viewport, with "Root · 0.00 mm" over
    "c 120.00 mm · t/c 12.00 %".
  - The current station has a station-colour border and underline.
  - A station that shares the section being edited draws the draft shape.
- **Points pane.**
  - Type reads in full ("Control", "Handle", "Anchor"). A narrower pane trims a type name with an ellipsis.
  - x and y are no longer cut ("35.00", not "35.0("). The x and y boxes now stretch, as in Properties.
  - Kind keeps a 4 px gap after y ("6.50 Horizontal").
  - Rows use the mockup's 12 px left inset.
- **Properties.** A wide value that leaves its label less room than the label's longest word now stacks under the
  label. "Station t/c" sits over "From the Thickness curve ▾" and no longer wraps a letter per line.
- **Tabs.** At the launch size, "Rail controls" shows in full.

## 3. Marine-CAD re-review (Adversary, T1)

**First pass: SOFT VETO.** The comb teeth pointed into the foil, toward the centre of curvature. That breaks §11.2
("teeth point outward"); the mockup draws them with `dir = −sign κ`.

The cause is in `SectionCanvas`: it used `+Math.Sign(κ)`. The check `SectionEditor_Comb_TeethPointOutward` (UXR) was
red at "32 of 32 teeth inward on 20–60 % chord" and is green after the sign flip.

**Second pass: CLEAR.** The reviewer confirmed in the recaptures that the teeth now point outward, the Tip thumbnail
shows the draft, and "100 %" is whole.

Class sweep: the only other comb is the Plan view's. It takes its normal from Core's `Planform.Comb` and has no sign
flip in Desktop, so it does not share this mechanism.

Findings that remain for the operator (the reviewer agreed they do not block):

| # | Finding | Severity | Why it is not fixed here |
|---|---|---|---|
| R-1 | A handle's **Length is entered in mm**, with % c as an echo below. §11.4 makes it % c. A mm value depends on the station when the section is shared. | Major | This changes the field's unit and commit rule, which PNL owns. It needs its own red-first test and an operator ruling, and it is not in the UXR brief. |
| R-2 | The comb has 39 teeth per surface and no envelope line, so the break shows as one clipped tooth. The mockup samples about 400 teeth. | Minor | Comb density is a §11.2 contract question, and it costs drag frame time (the drag readiness row is not built; see §6). |
| R-3 | Points pane: y may not read in the accent colour like x. | Minor (Inferred by the reviewer; not checked) | It needs a look at the live pane. |

## 4. Deviations from the EDT and PNL captures

| Deviation (brief) | Status | Evidence |
|---|---|---|
| **Major 1:** a stale "Checking…" reason box stays after the check passes | **Fixed.** `Bind` now hides the state text it showed when that state ends, even within one generation. A gesture refusal written over it stays. | `SectionEditor_CheckFinished_CheckingReasonHidden` (UXR): red at "box 'Checking…', can finish True", then green |
| **Major 2:** the reason box sits bottom-centre over Fit and the refit label | **Fixed.** It sits top-right under the mode bar. The refit label also moves above its marker when it would hit the axis labels. | edt-s2c, edt-s3 |
| **Major 3:** the 2c box shows Core's raw text | **Fixed.** The box shows COPY-187 from the shared composer `ShellHost.RefitCopy`. The precision rule is new: the move prints to 0.0001 mm ("0.0104 mm, over the 0.010 mm limit"), because 10.37 µm at three places read as "0.010 mm over the 0.010 mm limit". | edt-s2c; DESIGN.md COPY-187 |
| **Minor 4:** mode-bar buttons clip vertically | **Fixed.** Buttons are 24 px with `ModeBarButtonPadding` (8,0). | edt-s2 |
| **Minor 5a:** Curvature uses Fluent's blue | **Fixed.** A pressed toggle uses the soft fill with a hairline border. | edt-s2 |
| **Minor 5b:** the scope chip reads "shared profile" | **Fixed.** It reads "Shared with Tip · Make unique to Root". | `SectionEditor_ModeBar_NamesStationAndScopeChip` (updated to the copy) |
| **Minor 5c:** the station name is not accented | **Fixed** (inline run in the primary colour). | edt-s2 |
| **Minor 5d:** the probe is a separate low-contrast box | **Fixed.** The probe is a plate on the plate line. | edt-s2 |
| **Minor 5e:** chord % axis labels are missing | **Fixed.** | edt-s2, edt-s3 |
| **Minor 5f:** the comb label is missing | **Fixed.** | edt-s2 |
| **Minor 5g:** strip thumbnails are text only | **Fixed.** Each thumbnail draws the section outline. | `SectionEditor_StripThumbnails_OnePerStationCurrentMarked` (now also asserts an outline) |
| **Minor 5h:** screen 3 is zoomed by Show | **Fixed in the capture.** Screen 3 is at Fit, as in the mockup; Show still frames the crossing when pressed. | edt-s3 |
| **PNL:** the Points Type column clips to "Ha" | **Fixed.** There were two causes: the capture's window size, and the Properties row inset applied to the Points rows. | edt-s2 |
| **PNL:** "Station t/c" wraps a letter per line | **Fixed.** A crowded wide row now stacks. | edt-s2b, edt-s3 |
| **PNL:** the "Rail contr…" tab is cut off | **Fixed at the launch size**: it was the capture's narrowed side bar. It would still cut off if the operator narrows the left bar below about 205 px (Inferred). | edt-s2 |
| (found by UXR) the canvas resolved theme brushes without the theme variant, so grid and axis fell back to the station teal | **Fixed.** `ResolveThemeBrush` passes `ActualThemeVariant`. | edt-s2 (the grid is now viewport-grid) |

**Still differs from the mockup:**

- 2b is at Fit; the mockup zooms to 40–85 %.
- The 2c reason box repeats the strip's warning; the mockup shows no box on 2c. The UXR brief asked for the copy in
  the box.
- The probe reads "Pointer · display" because no pointer is over the canvas in a capture.
- The side bars open at 205 px (left) and 258 px (right), not 260 px. The left bar is sized before the Precision
  preset adds the right one. This was not changed: the sizing code is shell code outside UXR (Inferred cause).
- R-1 to R-3 above.

## 5. Operator's native-look checklist (packaged app)

Package: `.tmp-tests/package-m12c-uxr/CFD Workbench.app` in the UXR worktree. It is unsigned and framework-dependent,
bundle `com.cfdworkbench.desktop`. Built by `dotnet publish src/CfdWorkbench.Desktop -c Release -r osx-arm64
--self-contained false`, then `tools/package-application.py --platform macos`. To open it the first time, Control-click
the app and choose Open, because it is unsigned. Then open the Example foil.

Each row is pending until the operator attaches a receipt (a screenshot or a note). VoiceOver proof is deferred, per the
operator's priority: accessibility proof comes after function and look. The floors stay in place.

| Row | Try this | Expect | Receipt |
|---|---|---|---|
| N-12C-1 | Select the Root station: click the Root chip in Plan, then the root section in Side | Properties ends with **Edit section…**; overlapping Side sections cycle on a second click | pending |
| N-12C-2 | Press Return, or double-click the section in Side | The mode bar reads "Editing **Root** section · Shared with Tip · Make unique to Root · Section ▾ · Curvature · Thickness ×2 · Cancel · **Finish section**". Curvature is pressed with a soft fill, not blue. The strip shows two outlined thumbnails. | pending |
| N-12C-3 | Click upper point 4 | Properties shows Control point, x 35.00 % c, y 6.50 % c and the Section group | pending |
| N-12C-4 | Type ▾ → Anchor point, then Kind → Horizontal | Both surfaces gain the anchor (paired); the strip shows COPY-185; the comb breaks at 35 % with a dashed mark, and its teeth point **outward** | pending |
| N-12C-5 | Drag lower point 11 up through the upper surface, then ⌘Z | A red dashed line runs along both curves; Finish turns off; the top-right box says "Upper and lower surfaces cross…"; Show frames it; ⌘Z clears the box | pending |
| N-12C-6 | ↑, ⌘↑, ⇧↑; Return then type `36`, and `43.2 mm`; press ] and [; ⌫; double-click on a curve | Steps of 0.1, 0.01 and 1 % c. **Look for a hitch on each key-up**: the step apply measured about 56 ms (§6). | pending |
| N-12C-7 | **Finish section**, then ⌘Z | The views return with Root selected; ⌘Z restores the old section exactly; Cancel leaves everything as it was | pending |
| N-12C-8 | Click **Make unique to Root** in the chip | The chip reads "Only Root uses this section"; the Tip thumbnail keeps the old shape | pending |
| N-12C-9 | Open Section ▾ | Insert point…, Insert anchor, Delete point, Smooth…, Import .dat…, Make unique to Root, Station t/c ▸ | pending |
| N-12C-10 | VoiceOver on a focused point and the crossing alert | The surface, index, type and x/y are spoken, and the alert speaks once | deferred (operator priority) |
| N-12C-11 | Comb on a tight nose, at Fit and at Fit Selection | Teeth point outward; clipped teeth are marked ×; the plate counts them | pending |

Also try these: switch to the dark appearance (System Settings) and repeat N-12C-2 and N-12C-5. Narrow the right side
bar and check that the Points Type column trims with "…" rather than cutting.

## 6. Readiness rows (gathered by UXR)

The rows were run by name with `CFD_TEST_ONLY`, not through `tools/run-readiness.py`. Load average was about 28.

| Row | value_ms | Status |
|---|---|---|
| `Readiness_ProfileViewRebuilt_Under5Ms` | (existing row; not re-run by UXR) | — |
| `Readiness_SectionStepApply_Under5Ms` | **55.7** (best of 5) | **Misses** the §6 `assume:` (under 5 ms). Per §6 this is measured, not a gate: the row prints `READINESS-MISS` and passes. The design's trigger now fires: move steps off the UI thread. |
| `Readiness_SectionAssessExample_Under50Ms` | 9.9 | Within budget |
| `Readiness_SectionDragFrameP95Under16Ms` (Desktop) | not built | Remaining. It needs a Desktop readiness entry, and the readiness switch sits in `WorkbenchTests.cs` next to the Spawn list another track owns. |
