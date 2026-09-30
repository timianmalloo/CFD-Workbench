---
id: proof-d3a-red-runs
title: D3a shell window red runs
type: proof-pack
status: in-review
owner: "@track-d3a"
phase: implementation
tags: [app-shell, desktop, d3a, red-first]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Recorded foreground red runs across D3a, D3b and THEME. All 60 ported
  inventory rows have destination tests and red evidence.
---

# D3a red runs — dispatch 2

## D3b continuation

All D3b commands run with `AGENT_SESSION=track-d3b AGENT_WI=D3b` in the assigned tree.

| Run | Pre-implementation state | Exit | Observed failure |
|---|---|---:|---|
| 26 | New foil tests added before the shell path existed | 1 | `ShellWindowTests.cs` CS1061: `ShellHost.OpenNewFoilAsync` absent; the test's first attempt also used an inaccessible generated `ModelArea` field and was corrected to use the named control. |
| 27 | Menu history test added before the controller exposed `CanUndo` / `CanRedo` | 1 | CS1061 for both controller properties. |
| 28 | Controller properties added, but native menu command still had unconditional `CanExecute` | 1 | `Menu_UndoEnabled_FollowsFocusAndHistory`: empty document enabled Undo or Redo. |
| 29 | Crash output test added before `StartupFailure.FailureCode` existed | 1 | CS0117 for the missing stable code. |
| 30 | Temporary `Console.Error.WriteLine(path)` in `ShellHost.OpenFileAsync` | 1 | `Telemetry_MarkerInjection_AbsentEverywhere`: marker appeared in captured stderr. Plant removed. |
| 31 | F6 and palette tests before shell APIs existed | 1 | CS0117/CS1061: `BindF6`, `PaletteCommand`, `OpenPalette`, and palette state absent. |
| 32 | Span Tab test before the focus advance | 1 | `Focus_SpanCommitTab_NextField`: focus stayed in Span. After adding the focus move, the test's `900` fixture was discovered to equal the example's existing full span; `1000` then passed. |
| 33 | Applied shell theme matrix before a Dock text correction | 1 | `ThemeMatrix_ShellControls_AppliedContrast`: dark model Dock tab text was black on `#101a1d` (ratio 1.19). Two `Styles.axaml` selector repairs left the rendered result unchanged. The 2-cycle repair cap fired; those ineffective selectors and the incomplete test were removed. |

After wiring the card, File menu, controller call, and Opening/Cancel outcome, the foreground `--shell-window` run exited 0 with both new Start names and `NativeMenu_MainWindow_BuiltFromTable` passing.
After binding the menu command predicates and raising `CanExecuteChanged`, the foreground `--shell-window` run exited 0 with `Menu_UndoEnabled_FollowsFocusAndHistory` passing.
With the handler changed to type and code only, the foreground `--shell-window` run exited 0 with both privacy names passing. The telemetry test initially observed session events, preference result metadata, and stderr.

The subsequent telemetry test also reads `ShellEvents.Read()`. The preference store has no separate telemetry ring in the inspected source; only its result metadata is checked. `KeyBindings_F6InFloat_Bound`, `Palette_Keyboard_FiltersAndRuns`, and `Focus_SpanCommitTab_NextField` passed in the foreground shell suite. `DockTabFocus_FreshBatch_ReadyAndTwoRing` passed on first execution, so it has no red receipt. The 52 remaining inventory rows were not marked done. `ThemeMatrix_ShellControls_AppliedContrast` and `Focus_MenuTab_ClosesMenuReturns` remain unwritten in the committed suite.

## D3b exit checks

Three foreground `tools/run-tests.sh` runs exited 0. Each printed 279 Core PASS and 79 Desktop PASS lines, with a non-empty `--shell-window` suite. Their sorted PASS sets had identical SHA-256 `e930608447f0526b746c8e5a99a40d7ee8083a4544f9c8ab0b5963d23a08d75b`. The D3a checker reported 38/40 PASS (exit 1); D1 12/12, D2 21/21, C1 14/14, and P1 23/23 all exited 0. `check-docs.py`, XAML token lint, locked restore, and the `CFDW_STARTUP_SMOKE=1 dotnet run --project src/CfdWorkbench.Desktop` launch each exited 0. The smoke log showed `main-window-assigned=True` and `window-opened`; it closed itself.

Every command below ran with `AGENT_SESSION=track-d3a AGENT_WI=D3a` in this worktree.
Each run used `dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -c Release -- --shell-window`.
The complete process exited after each run; no test window was left open.

| Run | Mutation or pre-implementation state | Exit | Observed failures |
|---|---|---:|---|
| 1 | Focused-target APIs absent | 1 | `Viewport_FocusVertex_RaisesFocusedTargetChanged`, `SectionCanvas_FocusVertex_RaisesFocusedTargetChanged` |
| 2 | Real `typeof(Dock.Model.Core.IDock)` field planted in `Program.cs`, outside `Shell/`; focus APIs still absent | 1 | `Architecture_DockConfinedToShell` named `src/CfdWorkbench.Desktop/Program.cs`; both focused-target names also failed |
| 3 | `ShellHost` constructor, before resource lookup fix | 1 | `ShellHost_PlanformLayout_ContainsModelAndSidePanes`: `InvalidCastException` from an unattached resource lookup |
| 4 | Start mutations: Opening hidden, Cancel left it visible, failure alert hidden, Dismiss left it visible | 1 | `Start_Opening_FocusOnCancel`, `Start_OpeningCancel_FocusReturnsToCard`, `Start_OpenMissing_AlertLocate`, `Start_OpenNewer_AlertOpenAnother`, `Start_OpenFailedDismissed_StartKept` |

The plant and Start mutations were removed. The run after focused-target implementation
printed `PASS` for both focused-target names. The run after the resource fix printed
`PASS ShellHost_PlanformLayout_ContainsModelAndSidePanes`. The Start names passed before
their deliberate mutation; a final post-revert run is recorded by the harness gate.

`NativeMenu_MainWindow_BuiltFromTable` passed on first execution with the selected menu
builder code. It has no red-first receipt in this dispatch and is not claimed as such.
The other D3a names and the reflection-bound inventory rows remain unproven here.

## Dispatch 3 red runs

Each run used `AGENT_SESSION=track-d3a AGENT_WI=D3a` and the foreground command
`dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -c Release -- --shell-window`.
The process ended after each run. Mutations listed here were removed after observation.

| Run | Test or mutation | Exit | Observed failure |
|---|---|---:|---|
| 5 | `MainWindow_ShellMode_ContainsDockHostAndNativeMenu` before its constructor existed | 1 | CS1739: no `shellMode` parameter |
| 6 | `NativeMenu_MainWindow_BuiltFromTable` while New foil was enabled | 1 | New foil enabled before controller seam |
| 7 | Browser `BindStations` changed to always replace | 1 | `Browser_AcceptedIdentity_KeepsOrReplacesRows`: selection refresh replaced row (inventory :1811) |
| 8 | Browser `BindStations` changed to never replace | 1 | `Browser_AcceptedIdentity_KeepsOrReplacesRows`: new identity retained row (inventory :1814) |
| 9 | Rail editor selection changed to enable numeric input for locked controls | 1 | `Controller_LockedRailControl_RefusesDraft`: locked control enabled draft input (inventory :1326) |
| 10 | Left Dock proportion changed from .25 to .9, after deferred content settled | 1 | `ModelArea_MinimumWindow_PlotWidthAtLeast250`: measured plot width 1 (inventory :1551) |
| 11 | `F6_RegionEntry_FocusesSelectedTabOrRow` before `ShellHost.MoveFocus` existed | 1 | CS1061: missing `MoveFocus` (inventory :1757a, :1806) |
| 12 | `PaneBind_Throws_ShowsErrorStateClearsOld` before pane catch paths retained the error state | 1 | `NullReferenceException` escaped Properties `Bind` |
| 13 | `EditVerbRouter` sent a focused TextBox's Undo to the document | 1 | `EditVerb_UndoInSpanField_EditsText`: field text was not undone |
| 14 | `HandleOpenOutcome(Opened)` focused the side bar toggle instead of the viewport | 1 | `Start_Opened_FocusModelArea`: model focus absent |
| 15 | Before tab focus and closed-pane lookup were added | 1 | `Focus_ClosePane_NextTab`, `Focus_WindowPanesShow_PaneTab` |
| 16 | Hide and final-close focus targets changed to the viewport; each close settled before the next | 1 | `Focus_HideDockHoldingFocus_ToToggle`, `Focus_CloseLastPane_DockToggle` |
| 17 | Before size targeted the owning dock and moved tabs regained focus | 1 | `Focus_SizeMenu_ReturnsToTab`, `Focus_MoveTo_StaysOnMovedTab` |
| 18 | Rerender focused the toggle, invalid Span hid its alert, and Browser Enter cleared the selection | 1 | `Focus_DockRerender_NeverWindowRoot`, `Focus_SpanInvalid_StaysInFieldWithAlert`, `Focus_BrowserEnter_StaysOnRowSelectsStation` |
| 19 | Document Undo focused the side bar toggle after restoring the source | 1 | `Focus_UndoFromCanvas_StaysOnCanvas` |
| 20 | `Start_Opened_FocusModelArea` opened after the Start view had rendered | 1 | `Call from invalid thread` while controller Changed bound panes off the UI thread; after marshaling, the same test failed because the viewport was not yet materialized. Bounded deferred focus made it pass. |
| 21 | Application About menu missing | 1 | `NativeMenu_Application_AboutOnly`. The first assertion expected exactly one item and revealed Avalonia's standard Services/Hide/Quit items; the final assertion permits those but requires one About and no workbench File/Edit/Window group. |
| 22 | `RefreshRecentMenu` populated no entries while the real preference file held one path | 1 | `Recent_StoredRows_StartAndFileMenu`: Start contained the stored row, File ▸ Open Recent contained only Clear Menu. Restoring the entries made both surfaces pass. |
| 23 | Removed the recent-add call from `HandleOpenOutcome(Opened)` | 1 | `Recent_OpenedOutcome_AppendsPath`: an actual `.foil` open completed but no P1 recent row appeared before the bounded 8 s deadline. Restoring the call passed. |
| 24 | Added a duplicate ⌘Z `KeyBinding` beside the native menu gesture | 1 | `KeyBindings_MenuGesture_NotBound`: duplicate binding detected. The plant was removed. |
| 25 | `Review_Persona_FocusesShellRegion` before `ShellHost.FocusForPersona` existed | 1 | CS1061: the four persona routes were absent. The shell now focuses Start Open, the viewport, a Browser row, or a rail control row. |

The first minimum-window attempt also exited 1 while Dock's deferred presenter had
not materialized. It was not counted as mutation evidence. The test now runs bounded
dispatcher jobs before measuring the rendered model area. A run with the .9 mutation
then failed on the actual 100 DIP model width; restoring .25 passed.

Dispatch 3 closed with three consecutive `tools/run-tests.sh` runs at exit 0.
Each had 274 Core and 67 Desktop PASS lines, with identical sorted PASS-set SHA-256
`65ac7c9429aca1bcb87684016a23a13f35b94c0f549379c023cb9bca6a885ab1`.
The D3a named check reached 29/40. The remaining 11 names and 52 inventory rows
are not claimed as complete by this proof. D1, D2, C1, and P1 named checks,
locked restore, token lint, documentation checks, and a foreground startup smoke
all exited 0. `pgrep -fl CfdWorkbench` found no remaining process.

## Green checks observed after removing mutations

`tools/run-tests.sh` exited 0 on three consecutive foreground runs. Each run
printed 274 Core and 44 Desktop PASS lines (the CLI harness has no named PASS
lines), and the sorted PASS set across `.tmp-tests/*.log` had 318 entries with
the same SHA-256 each time:
`d2c0e1c916033bdeb49223ff97fafb2bc672ec4635978d226268f4676cf2f3f3`.
The D3a checker found 10 of 40 required names; the remaining 30 are outstanding.

## THEME track (2026-09-30)

Every command ran with `AGENT_SESSION=track-theme AGENT_WI=THEME` in this worktree, as
`dotnet run --project tests/CfdWorkbench.Desktop.Tests -c Release -- --shell-window`.

**Cause (observed).** Dock 11.3.12.1 `Accents/Fluent.axaml` (package commit `f891bdb`) defines
`DockThemeForegroundBrush` (line 15) and the other theme brushes (lines 12-16, 20) once, outside
`ThemeDictionaries`, with `Color="{DynamicResource System*Color}"`. That color resolves against the
Application's variant. Lines 21-46 alias them by `StaticResource`, which freezes those instances. A window
with `RequestedThemeVariant` Dark (`MainWindow.axaml.cs:95`) under a Light Application therefore got
Light Fluent values: tab text `#FF000000` (`SystemBaseHighColor`) and the selected tab fill `#66000000`
(`SystemBaseMediumLowColor`). No app key mapped Dock's brushes, and `DockFluentTheme` is added after
`Styles.axaml` (`App.axaml.cs:16` → `ShellHost.cs:26`). The D3b selectors could not work either: they
targeted `TabItem`, and Dock tabs are `TabStripItem`s.

| Run | Mutation or pre-implementation state | Exit | Observed failure |
|---|---|---:|---|
| 34 | `ThemeMatrix_ShellControls_AppliedContrast` on `685bb1e` (test only) | 1 | 30 rows. Dark Dock tab text `Black` on `#101a1d` = 1.19 (doc and tool tabs); HC `Black` on `Black` = 1.00; Dock blue hover 2.77, active fill 3.96, selected tool text 2.80; selected tab fill `#66000000` (partial alpha) in all three themes. Inventory :113 and :1446 (matrix, no row failures). |
| 35 | Controller lookup renamed to `workbenchRenamed` | 1 | `Controller field unreadable` (inventory :1215) |
| 36 | Example open removed before the theme barrier | 1 | `Opened Example not bound before the theme barrier` (inventory :977) |
| 37 | Barrier wait cut to `TimeSpan.Zero` | 1 | `Theme barrier focus composition was not ready` (inventory :1018) |
| 38 | `SourceText.IsReadOnly` set false | 1 | `Foil source tab lost its read-only accepted text` (inventory :740) |
| 39 | Editable rail control `BeginEdit` before Span typing | 1 | `Span typing opened a draft` (inventory :721) |
| 40 | `PseudoClasses` lookup renamed | 1 | `Installed protected PseudoClasses unavailable` (inventory :1219) |
| 41 | Reflected property swapped for `Name` (not an `IPseudoClasses`) | 1 | `Installed IPseudoClasses unavailable` (inventory :1221) |
| 42 | Hover raised `PointerExited` instead of `PointerEntered` | 1 | `PointerEntered/Exited did not set :pointerover`, 15 rows (inventory :1228) |

Two plants were not evidence and were redone: a `span.SelectAll()` anchor that matched twice, and a
`control.Classes` cast that still yielded an `IPseudoClasses` (it passed). Runs 39 and 41 are the redone plants.
Each plant was reverted by the script before the next; the green run after the fix exited 0.

**Fix.** `App.axaml` maps the 22 Dock brush keys its templates read onto our color tokens in
`Application.Resources` `ThemeDictionaries` (Light, Dark, HighContrast). `Application.Resources` is searched
before `Application.Styles`, so it wins over `DockFluentTheme`. After the UX & Accessibility veto (selection
carried by a 1.00-1.15 fill difference), `Styles.axaml` sets the selected Dock tab fill to `PrimaryBrush` with
`OnPrimaryBrush` text; a Style outranks Dock's ControlTheme. Applied contrast after, minimum per theme: text
light 6.29, dark 5.29, HC 19.56; selected fill vs strip light 5.59, dark 9.35, HC 19.56; focus ring vs its
surface light 5.31, dark 7.79, HC 19.56; live Light→Dark switch 15.85 (text) and 10.73 (fill).

### THEME repair cycle 1 (Coordinator verification of `b0d5a28`)

**Flake.** One of three Coordinator `tools/run-tests.sh` runs on `b0d5a28` exited 1:
`FAIL ThemeMatrix_ShellControls_AppliedContrast InvalidOperationException: 1 theme rows failed: high-contrast/focus.span: Unknown group opacity on DeferredContentPresenter`.
The 2 s opacity poll in `RingRow` did not prevent it. It was removed.

**Cause (observed).** A diagnostic in the opacity refusal caught the state on run 8 of a `run-tests.sh` loop:
`DeferredContentPresenter#PART_ContentPresenter op=0.9642615023796038 prio=Animation animating=True transitions=1`,
templated by `DeferredContentControl`. Dock's `DeferredContentControl.cs` (package commit `f891bdb`) is the source.
Its `ApplyRevealAnimation` (lines 1104-1116) sets the presenter's opacity to 0.85 when presented content changes.
It then adds a `DoubleTransition` to 1 over `RevealDuration`, which is 90 ms by default (line 60). `ShowPane("properties")`
swaps the left dock's content, so the probe after it could land mid-fade. The run's load decided whether it did.

**Fix.** The matrix sets `DeferredContentPresentationSettings.RevealDuration` to zero for its duration and restores
it afterwards. With a zero duration, Dock sets opacity to 1 and adds no transition (line 1106). The probe measures
settled paint. `Backing` still refuses any group opacity it cannot resolve. No skip, retry or sleep was added.

**Focus ring on a selected fill (ux-accessibility should-fix).** Fluent's adorner has two rings. The observed geometry
corrects the r1 note above, which read an adorner-layer `TranslatePoint`. The outer ring (2 px, Primary) is the
target's own size and paints over its edge. The inner ring (1 px) is inset by 2 px and meets the fill. On a Primary
selected fill, the inner Ink tone measured 2.39 light, 1.48 dark and 1.07 HC. A tab focused and then selected kept
that adorner, because Avalonia builds the adorner at focus time.

| Run | Mutation or pre-implementation state | Exit | Observed failure |
|---|---|---:|---|
| 43 | Ring rows extended (`.vs-fill`: the best ring tone against the target's fill) before any style change | 1 | `focus.tab.vs-fill` and `focus.browser.selected.vs-fill`: `#1b2929` on `#006c67` = 2.39, `#edf4f2` on `#66ddc8` = 1.48, `White` on `Yellow` = 1.07 |
| 44 | Adorner override scoped to `:selected` only; new row focuses the Foil source tab, then selects it | 1 | `focus.tab.selected-while-focused.vs-fill` = 2.39 / 1.48 / 1.07: the adorner is not rebuilt on selection |

A global `SystemControlFocusVisualSecondaryBrush` change was tried first. The kept `WorkbenchTests.cs:1837` pin
(inner focus brush equals Ink) failed it, so the change was reverted. The fix sets a token-only `FocusAdorner`
on `DocumentTabStripItem`, `ToolTabStripItem` and `ListBoxItem`. It uses Fluent's own thickness and margin resources,
the Primary outer ring and an OnPrimary inner ring, in every state. The ring now separates from the fill:
6.29 light, 10.73 dark and 19.56 HC on a selected fill. The outer tone gives 6.11 / 9.35 / 19.56 on unselected
fills. The outer ring against the strip stays at 5.59 / 10.73 / 19.56.

**Proof.** Five consecutive foreground `tools/run-tests.sh` runs all exited 0. Each printed 279 Core and 80 Desktop PASS
lines, with the same sorted PASS-set SHA-256:
`b73bb5ab6e2b0814644d96d56556b3228102b7504ecb53a32c198dc7a49229f4`.

## D3b dispatch 2: remaining inventory and live shell seams

Commands ran with `AGENT_SESSION=track-d3b AGENT_WI=D3b` in the assigned tree. The
foreground red command was `dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -c Release -- --shell-window`.
Every run exited 1 with the named failure below. The mutation script restored the
original test source after each run; its measured result list was retained at
`/tmp/d3b_red_rows_results.json` for this dispatch. A post-revert shell-window run
exited 0, with no `FAIL` line.

The menu-focus test first failed `Properties tab menu or keyboard focus is missing`.
The live app-bar switch test first failed because the bar had no usable brush.
After the bar began resolving both brush keys on attach and `ActualThemeVariantChanged`,
the test oracle's own `host.FindResource` returned `UnsetValue`; that assertion was
corrected to check both *rendered bar brushes* changed on Light→Dark. The corrected
test passed. The two initial red failures are separate from the inventory mutations.

| Run | Inventory row(s) | Mutation | Observed `FAIL` |
|---:|---|---|---|
| 45 | :387 | reflected `workbench` → missing field | Dock: shell controller field unreadable |
| 46 | :389, :1141 | model Dock tab lookup → Properties tool | Dock: model tab did not render |
| 47 | :434 | compositor lookup on detached Border | Dock: tab lacks composition visual |
| 48 | :448 | `FoilViewport` lookup → missing name | Dock: `NullReferenceException` at viewport check |
| 49 | :453 | bound frame compared with another object | Dock: accepted frame not bound before barrier |
| 50 | :455 | retained compositor compared with another object | Dock: tab lost compositor after fixture open |
| 51 | :458 | stale/fresh identity check reversed | Dock: stale batch reused |
| 52 | :513 | reflected `closeApproved` → missing field | Dock: shell close approval field unreadable |
| 53 | :1146 | focus a detached Border instead of the Dock tab | Dock: tab refused keyboard focus |
| 54 | :1149 | adorner lookup on a detached Border | Dock: no adorner layer |
| 55 | :1156 | select borders with null brushes | Dock: lacks two focus rings |
| 56 | :1745 | native Undo key oracle Z → Y | Native menu: undo shortcut mismatched platform |
| 57 | :1751 | F6 next-region oracle 1 → 2 | F6: did not skip unavailable region |

Runs 45, 49 and 52 are separate reflection-bound red runs under Ruling 54 P3.
The locked-control reflection-bound row :1326 already had its own D3a run 9.
The seven other non-theme rows had D3a runs 7, 8, 10, 11 and 25; the inventory
records each row's receipt. The 39 THEME rows retain runs 34–42 and their own
reflection receipts. This completes the measured 60 ported rows.

**Final gates on the restored source.** Three consecutive foreground
`tools/run-tests.sh` runs exited `0, 0, 0`. Each copied log held 279 Core and
82 Desktop `PASS` lines, and each sorted combined PASS set had SHA-256
`5ca132cc62203367e34039fb1ad790a73c6b4ded11d086a00a61c6c7c4c741fe`.
The `--shell-window` suite was nonempty, including the menu Escape/Close focus and
live app-bar theme-switch tests. `check-named-tests.py` reported D3a 40/40,
D1 12/12, D2 21/21, C1 14/14 and P1 23/23, all exit 0. `check-docs.py`,
XAML token lint and locked restore each exited 0. The default shell launch with
`CFDW_STARTUP_SMOKE=1` exited 0 after `main-window-assigned=True` and
`window-opened`; its startup hook closed the window. `pgrep -fl CfdWorkbench`
returned no process. The harness cannot establish native Windows behavior or
full macOS accessibility acceptance from these foreground runs.
