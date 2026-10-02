---
id: review-ui-status-bar
title: Status strip and warning toast (DR-STATUS-1) — message inventory and build brief
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, status-bar, toast, shell, properties, build-brief, announcements]
links:
  - {to: mockup-status-bar, rel: documents}
  - {to: property-grid-rulings, rel: implements}
  - {to: review-ui-property-grid-cells, rel: relates-to}
  - {to: design-app-shell, rel: relates-to}
  - {to: design-m12b-points, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-31
summary: >-
  The operator chose V2 (DR-STATUS-1): a status strip along the bottom of the shell plus a transient toast for commit
  warnings; field errors stay at their field; command reports leave the property sheet. This brief lists every message
  the app shows today, where it renders now and where it renders after V2, then gives the build: the strip in
  ShellHost row 1, the toast in ModelArea, one report sink that replaces the model area's top status line and the
  pane's row reports, the STATUS-CLOBBER rule at the strip, 17 named red-first checks, and the existing checks that
  move. Four small decisions remain for the operator.
review-suggested: []
---

# Status strip and warning toast — inventory and build brief

**Result.** V2 is promoted. DESIGN.md §4 has two new rows, *Status strip* and *Toast*, with the tokens
`spacing.toast-w` (420 px), `spacing.toast-inset` (12 px) and `motion.toast-hold` (8000 ms); §6 has a *Warning toast*
motion row. The strip reuses `spacing.shell-statusbar` (24 px). The pick page is `docs/mockups/status-bar.html`
(b7c03c5); the ruling is DR-STATUS-1 in `docs/notes/property-grid-rulings.md`. Nothing is built.

**Confidence.** The "today" column is **Verified** by reading the source at base 3b0545e (file:line given). The
"after V2" column is the design. Two statements are **Inferred** and marked so; the build proves them red-first.

## 1. Message inventory

"Status line" means the model area's top polite line today: `ModelArea.axaml` row 1, `StatusText` (LiveSetting
Polite) with `StatusTryAgainButton`. "Row" means a Properties row message (`Message_<key>`, rendered by
`PropertiesPane.ShowMessage`, `PropertiesPane.axaml.cs:532`).

### 1.1 Properties pane (`src/CfdWorkbench.Desktop/Panes/PropertiesPane.axaml.cs`)

| Message | Source | Today | After V2 |
|---|---|---|---|
| Type change report "Trailing edge point 7 is now an anchor point with 2 handles. The rail gained …" | :1442–1443 | Row `Message_p_type` (Report) **and** status line (Announced, Polite, PG-26) | **Strip** (info). Row message removed |
| Type change refused | :1446 | Row `Message_p_type` (Error) | **Stays at the field** (Error) |
| Tangent kind report "… is now Symmetric. …" | :1476–1477 | Row `Message_t_kind` (Report) and status line | **Strip** (info). Row message removed |
| Tangent kind refused | :1468 | Row `Message_t_kind` (Error) | **Stays at the field** |
| Nudge release "<Label> <value> <unit>." | :1310–1313 | Row (Report) and status line | **Strip** (info). Row message removed |
| Typed chord fit above the limit "Root chord 190.00 mm. Fit 22.53 µm (limit 10 µm) — above the limit …" | :1077 | Row `ChordWarningText` (Warning, `MessageName`, :1638) | **Strip** (warning) **+ toast**. The row keeps its warning **state** only: the 3 px warning rail and the warning icon; the icon's tooltip and the field's HelpText carry the report |
| Typed value echo / expression echo ("#root_chord × 0.1 = 15.21 mm (set once; doesn't follow Root chord)") | :1078, :1152 | Row (Echo) | **Strip** (info). Decision D-2 |
| Angle run stops (`PropertyCopy.AngleRunStops`) | :1263 | Row `Message_h_angle` (Warning) | **Strip** (warning). No toast: it is a gesture warning, not a commit warning |
| Pending drop "Type unchanged: Control point." / "Tangent kind unchanged: …" | :1412 | Status line only | **Strip** (info) |
| "Selected Trailing edge · point 7 of 14." | :1510 | Status line only | **Strip** (info) |
| Estimates availability (COPY-160) | :271–274 | Status line once, plus Wing notes | **Strip** once; the **Wing note stays** |
| Pending hint "Return applies; Esc keeps …" | :1359 | Row (`Message_p_type`, `Message_t_kind`) | **Stays at the field** (it teaches the staged enum) |
| Tip closes (`PropertyCopy.TipCloses`) | :1087 | Row `Message_w_tip` (Reason) + field refusal | **Stays at the field** |
| Field validation error ("Enter a number. Aft is unchanged.", COPY-118 / 106 / 158) | :477, :1190–1197 | Row (Error) + assertive announcement | **Stays at the field**, assertive. Not repeated in the strip |
| Pane alert banner (`model.Banner`, e.g. not checked) | :264–265 | Pane band | **Stays** as a band |
| Recovery panel "A recovered edit is open." + Apply / Discard | :266–269 | Pane band | **Stays** as a band |

### 1.2 Controller (`src/CfdWorkbench.Desktop/WorkbenchController.cs`, every `Status =` write)

All of these render today in the status line through `ShellHost.RefreshPanes` (`ShellHost.cs:459`). **After V2 all
go to the strip**; the kind is info unless named.

| Message | Source |
|---|---|
| "Span applied as one accepted source revision. Save to persist it." | :281 |
| "Checking the last change…" (busy placeholder) | :301 |
| "This {role} point is fixed by the foil definition." (also assertive on the Plan canvas, `PlanCanvas.cs:493`) | :316 — **error** |
| Gesture outcome: committed report · refused copy (edges cross, not assessed) · "Point change cancelled." | :539 — refused is **error**; the Plan marker stays |
| "No point change." / cancelled copy | :563 — decision D-3 |
| Chord report "{dimension} {mm} mm. Fit {µm} µm …" | :604–611 — **warning** when above the limit |
| Committed report · "{code}: This change wasn't applied. Nothing changed." | :635, :642 — the second is **error** |
| Recovery, "Refused. Original source retained read-only.", control IDs, "New foil." / "Opened.", diagnostics | :831, :858, :872, :885, :921, :941, :955, :966 — "Refused …" is **error** |
| Section draft messages, incl. "Accepted η {eta} slice; 15 measured display points in … ms" | :1021–1119, :1326–1350, :1443–1473 |
| Draft applied / cancelled · Undo / Redo · save and durability · recovery · "Opening…" | :1147, :1163, :1177, :1191, :1220–1285, :1301, :1310, :1419 |

### 1.3 Shell (`src/CfdWorkbench.Desktop/Shell/ShellHost.cs`)

| Message | Source | Today | After V2 |
|---|---|---|---|
| "Opening cancelled. Nothing changed." | `ShowStatus` | Status line | Strip |
| "The recent-files list wasn't cleared: {reason}. The list is unchanged." + **Try again** | `ShowStatus(…, offerTryAgain: true)`, :416 | Status line + button | Strip (**error**) + a 24 px **Try again** after the message |
| "Text size {n} %." · "Text size will apply this session only: …" | :570, :604 | Status line | Strip (the second is **warning**) |
| "Zoomed in." · "Zoomed out." · "Curvature comb on./off." · "Fit." | :686–698 | Status line | Strip |
| Alert band (open failures, recovery: Locate…, Try again, Remove from Recent, Resume / Discard) | `ModelArea.axaml:8–22`, assertive | Band at the top of the model area | **Stays** as a band |

## 2. Build brief

### 2.1 Surface list (E7)

Store: none (no persisted state; Text size already persists, DN-5). Model: a `StatusReport(string Text, ReportKind
Kind, bool Toast)` record and `ReportKind { Info, Warning, Error }`. Controller: `StatusKind` beside `Status`, and the
existing write counter exposed as `StatusVersion`. Pane: a `Reported` event replaces the polite branch of `Announced`.
Shell: the `StatusStrip` control and one sink. UI: the strip, the toast, the row-message removals. Readers: the 11
test sites in §2.6.

### 2.2 Where the strip lives

- **`ShellHost`** (a `Grid`, `ShellHost.cs:95`) changes `RowDefinitions("*")` to `RowDefinitions("*,Auto")`. Row 0
  keeps `DockHost` and the palette overlay. Row 1 holds a new **`StatusStrip`** (`Shell/StatusStrip.axaml`), full
  width, under every pane and dock. `MainWindow` is unchanged.
- **`StatusStrip`** parts: `StatusKindIcon` (12 px Path), **`StatusText`** (TextBlock, `LiveSetting="Polite"`,
  `TextTrimming=CharacterEllipsis`, `ToolTip.Tip` and automation name = full text), `StatusTryAgainButton` (24 px,
  hidden by default), then the read-only items `SelectionItem`, `UnitsItem`, `EstimateItem`, `TextSizeItem`. It keeps
  the names `StatusText` and `StatusTryAgainButton`, so tests move by host, not by name. Height
  `spacing.shell-statusbar`; 11 px `prop` type scaled by Text size; brushes `SurfaceBrush`, `LineBrush`,
  `WarningBrush`, `DangerBrush` (app resources, `Styles.axaml:24–77`, with dark and contrast variants).
- **`ModelArea.axaml` row 1** (the `StackPanel` holding `StatusText` and `StatusTryAgainButton`) is **deleted**, and
  `ModelArea.ShowStatus` with it. The Grid becomes `RowDefinitions="Auto,*"`; the alert band stays in row 0.
- **The toast** is a `Border x:Name="WarningToast"` in `ModelArea.ModelRoot` (`ModelArea.axaml:33`), last child so it
  draws on top: `HorizontalAlignment=Right`, `VerticalAlignment=Bottom`, `Margin=12`, `MaxWidth=420`, hidden by
  default. Parts `ToastIcon`, `ToastText` (wraps), `ToastDismissButton` (24 × 24, name "Dismiss"). It lives with the
  model document, so it sits at the bottom-right of the Plan and never covers the Properties dock or the strip.
  `simplify:` one toast slot owned by `ModelArea`; upgrade to a shell-level toast host only if a second document type
  needs warnings.

### 2.3 The announcement path

Today three writers share one TextBlock: the pane's polite `Announced` (`ShellHost.cs:118`), `RefreshPanes`
re-showing `Controller.Status` on **every** refresh (`ShellHost.cs:459`), and the shell's own `ShowStatus`
(`ShellHost.cs:416`). After V2 they all call **one sink**, `ShellHost.Report(StatusReport)`, which sets the strip and,
for `Toast: true`, the toast.

- **Pane.** `PropertiesPane` raises `Reported(StatusReport)` for every report in §1.1 marked *Strip*, and stops
  writing those to `messages[...]`. `Announced(…, Assertive)` for field errors stays as it is and does not reach the
  strip. The chord commit (`:1077`) sets the row's warning state and raises `Reported(new(report, Warning, Toast:
  true))`.
- **Controller.** `RefreshPanes` reports `Controller.Status` **only when `StatusVersion` changed** since the strip last
  showed it. *Inferred, not observed:* today a refresh after a pane report can re-show an older controller status over
  it, because `RefreshPanes` writes `Controller.Status` unconditionally after `Properties.Bind`. Check
  `StatusStrip_Refresh_DoesNotReshowOlderControllerStatus` proves or clears this red-first.
- **STATUS-CLOBBER holds at the strip.** The controller's counter rule (`WorkbenchController.cs:671`: a background
  report replaces only the placeholder it wrote) stays. The strip adds the same rule for its own slot: each report
  carries a sequence number, and a controller report whose `StatusVersion` predates the strip's last report is not
  shown. A background completion never replaces a newer report; an error still may.
- **One polite region.** The strip's `StatusText` is the only polite status region in the window (app-shell §11). The
  toast is not a live region: its text is already spoken by the strip.

### 2.4 The toast's behaviour

Opens for `Toast: true` only (today: the typed-chord fit above the limit). It takes no focus. Hold
`motion.toast-hold` (8000 ms) through a `DispatcherTimer` whose interval is a property, so checks can set it short.
The hold pauses on `PointerEntered` or focus inside and restarts on leave. It closes on ×, on Esc while focus is
inside (focus returns to the element that had it before), or when the next commit starts. A newer `Toast: true`
report replaces the text and restarts the hold; an info report does not close it. While open it is one more stop in
the F6 ring (`ShellHost.MoveFocus`, `:495`), after the model area. Native motion: none (hard cut), as the property
grid did (PG-16).

### 2.5 Named checks (red-first)

Write each first and see it fail on the current tree, then build. Runner and file named per check.

| # | Check | Runner (file) | Protects |
|---|---|---|---|
| 1 | `StatusStrip_SitsAtWindowBottom_FullWidth_24px` | `Check` (ShellWindowTests) | Strip bottom = client bottom; width = client width; height ≥ 24; below `DockHost` |
| 2 | `ModelArea_HasNoTopStatusLine` | `Check` (ShellWindowTests) | The old row-1 line is gone; no second status surface |
| 3 | `StatusStrip_IsTheOnlyPoliteStatusRegion` | `Check` (ShellWindowTests) | Exactly one polite `StatusText` in the window (no double speech) |
| 4 | `StatusStrip_TypeChange_ReportInStrip_NotInRow` | `Pane` (PropertiesViewTests) | The operator's case: report in the strip, `Message_p_type` hidden |
| 5 | `StatusStrip_KindChange_ReportInStrip_NotInRow` | `Pane` (PropertiesViewTests) | Same for Tangent kind |
| 6 | `StatusStrip_NudgeRelease_ValueInStrip_NotInRow` | `Nudge` (PropertiesViewTests) | Nudge value leaves the row |
| 7 | `StatusStrip_ChordAboveLimit_WarningInStrip_ToastOpens_RowKeepsRailOnly` | `Check` (ShellWindowTests) | Strip kind warning; toast visible with the same text; `ChordWarningText` hidden; the row keeps the warning class and the tooltip |
| 8 | `StatusStrip_FieldError_StaysAtField_StripUnchanged` | `Pane` (PropertiesViewTests) | "Enter a number. Aft is unchanged." at `Message_p_aft`, assertive; `StatusText` unchanged |
| 9 | `StatusStrip_GestureRefused_ErrorInStrip_NoToast` | `Check` (PlanCanvasTests) | Edges-cross refusal: strip kind error, toast hidden, Plan marker drawn |
| 10 | `StatusStrip_BackgroundCompletion_DoesNotReplaceNewerReport` | `Check` (ControllerShellTests, `--controller-shell`) | STATUS-CLOBBER at the strip: a pane report made during sampling survives the sampling completion |
| 11 | `StatusStrip_Refresh_DoesNotReshowOlderControllerStatus` | `Pane` (PropertiesViewTests) | A refresh after "Selected …" leaves it in place (the Inferred hazard in §2.3) |
| 12 | `Toast_Hold_ClosesAfterHold_PausedWhileHovered` | `Check` (ShellWindowTests) | The hold, with a short interval set by the check |
| 13 | `Toast_EscInside_ClosesAndReturnsFocus` | `Check` (ShellWindowTests) | Keyboard dismissal and focus return |
| 14 | `Toast_OpensWithoutTakingFocus` | `Check` (ShellWindowTests) | Focus stays in `RootChordInput` after the commit |
| 15 | `Toast_NewerWarning_ReplacesText_InfoDoesNotClose` | `Check` (ShellWindowTests) | One toast at a time |
| 16 | `StatusStrip_LongReport_EllipsizesWithFullTextInNameAndTooltip` | `Check` (ShellWindowTests) | Nothing lost when the strip trims |
| 17 | `StatusStrip_RecentNotCleared_TryAgainInStrip` | `Check` (ShellWindowTests) | The one action moves with its message |

### 2.6 Existing checks that move

**Helper and direct reads of the old line (change the host, not the name).** `PropertiesViewTests.cs:947`
`Status(host)` (used at PropertiesCellsTests :368, :758, :843 and PropertiesViewTests :244, :492, :792) and the
direct reads `host.ModelView.FindControl<TextBlock>("StatusText")` at PlanCanvasTests :631 and ShellWindowTests
:1622, :1816, :2469, :2499, :2812, :2832, :2933, :3105, :3113 read the strip instead.

**Assertions that flip from the row to the strip.**

- `TypeCombo_ArrowWhileClosed_DoesNotCommit` (PropertiesViewTests :607) reads the report on `Message_p_type`.
- `Properties_TypeToAnchor_OneUndoStepCurvePassesThrough` (ShellWindowTests :2470, PG-26) and
  `Properties_TangentSymmetric_OneUndoStep` (:2500, PG-33) assert status == row message. They become status == the
  report and the row message hidden.
- `WingRootChord_FitAboveLimit_WarningShownAndCommitted` (ShellWindowTests :2809–2811) asserts `ChordWarningText` is
  visible. It becomes strip warning + toast + row rail (check 7 can replace it).
- `FieldNudge_AngleRun_StopsAtDomainBound` (PropertiesViewTests :818, :825) reads `Message_h_angle`. It becomes the
  strip's warning.
- `WingTipChord_CentimetresAndReference_EchoedMm` (ShellWindowTests :2819–2840): the echo moves to the strip if D-2
  holds.

**Unchanged (they guard what stays at the field).** PropertiesViewTests :387 and :582 (pending lines), :657 and :672
(`PropertiesPane_Error_AnnouncedOncePerFailedCommit`); PropertiesCellsTests :249 (kind message hidden) and :723
(`Message_w_tip`, TipCloses); `PlanCanvas_LockedNudge_AssertiveLockCopy` keeps its canvas assertive check and reads the
strip.

**Strings.** No copy changes. The type, kind, nudge, chord, echo and angle-stop strings keep their text and move
surface.

### 2.7 Accessibility floor (lower priority, per the operator; cheap floors kept)

One polite region; field errors stay assertive at the field; the toast never takes focus; × is 24 × 24 with a name;
Esc closes from inside; the strip's trimmed text is in its name and tooltip; 11 px minimum (DR-DEN-3). The WCAG 2.2 AA
veto belongs to the UX & Accessibility lens and is not cleared here.

## 3. Decisions for the operator

| ID | Question | Default if not ruled |
|---|---|---|
| **D-1** | The page showed V2's strip saying "Ready · 1 warning shown above" while the toast is up. The design shows the **warning text in the strip too**, so one region announces it and the report survives the toast. Keep that? | Warning text in the strip |
| **D-2** | Unit and expression echoes ("… (set once; doesn't follow Root chord)", MC-3) move from under the field to the strip. Or keep the set-once echo at the field? | Strip |
| **D-3** | "No point change." (controller :563) — keep it in the strip, or drop it (the page proposed dropping)? | Keep; no copy change in this track |
| **D-4** | M1.2c planned a Messages pane for edit-report history in the bottom panel. DR-STATUS-1 forbids a docked scrolling list there. Does M1.2c keep a history at all, and where? | No history in M1.2c until ruled |

The 8000 ms hold and the 420 px width are design choices, not rulings.
