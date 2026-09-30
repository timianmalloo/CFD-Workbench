---
id: review-app-shell-native
title: Native review — CAD-first app shell, M1.2a rows
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: ui-design — U1a (M1.2a)
tags: [native-ui, accessibility, app-shell, m1.2a, copy, operator-run]
links:
  - {to: design-app-shell, rel: documents}
  - {to: design-language, rel: depends-on}
  - {to: mockup-workbench-v10, rel: relates-to}
  - {to: review-ui-application-native, rel: refines}
  - {to: investigation-review-window-attach, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-30
summary: >-
  U1a for M1.2a. COPY-125 to COPY-139 recorded; the build has fourteen copy and behaviour findings (C-1 to C-14; C-10, dead open-failed buttons, is a Blocker). No native
  row was attached: the supported CUA adapter is absent from this harness, and review mode turns the Dock shell off,
  so the supported attach cannot bind the M1.2a shell. Every native row is operator-run, not done. The accessibility veto is held. M1.2a stays open.
---

# Native review — CAD-first app shell (M1.2a rows)

Track U1a of [the app-shell build plan](../coordination/app-shell-build.md), 30 September 2026, worktree
`u1a-native-review` at `025afae` (the merged M1.2a Dock shell). Design: [`docs/design/app-shell.md`](../design/app-shell.md)
§11. Mockup: [`docs/mockups/workbench-v10.html`](../mockups/workbench-v10.html). Template:
[`ui-application-native.md`](ui-application-native.md).

**Disposition: M1.2a is OPEN.** No native row is attached. Every row below is **operator-run, not done**. A launch
is not review readiness (defect class CO-UI-READY), so this track did not launch the app. M1.2a closes only when the
operator runs the rows below and each carries an attach receipt that `tools/check-review-attach.mjs` accepts.

## 1. Attach attempt (the one bounded attempt)

| Check | Observed | Confidence |
|---|---|---|
| Supported adapter | `docs/coordination/review-attach.mjs` runs only in the `cua_repl` runtime (`mcp__cua_repl`, per `docs/investigations/review-window-attach.md`:120). A tool search for `cua_repl` in this Claude Code harness returned no tool. | Verified |
| Attach attempts made | 0. With no adapter, `attachReview` cannot be called. No receipt exists, so `check-review-attach.mjs` was not run on one. | Verified |
| App launched | No. Without an adapter, a launch proves only liveness (CO-UI-READY). `pgrep -fl CfdWorkbench` exited 1 (no process) before and after this track. | Verified |
| Package | No packaged `.app` exists in the worktree. The architecture §10.6 path is `dotnet publish` then `tools/package-application.py`. | Verified |

### 1.1 Blocker the operator will also hit — review mode turns the shell off

This is a static reading of the contract against the source. It is not an investigation of an attach failure.

- `App.axaml.cs`:32 builds `new MainWindow(shellMode: NativeReviewOptions.Current is null, …)`. With
  `CFDW_REVIEW_MODE=1`, `shellMode` is false. `MainWindow.axaml.cs`:68 then returns before it creates `ShellHost`,
  the Dock, F6 binding and the NativeMenu. **Verified (source read).**
- The ` · REVIEW …` title suffix is set only in review mode (`MainWindow.axaml.cs`:94). `review-attach.mjs` refuses a
  title without `REVIEW`. So the supported attach can bind only the pre-shell window, never the M1.2a shell.
  **Verified (source read).**
- `review-attach.mjs` also needs `ID: FoilViewport` and `ID: DocumentTabs` in the AX state. In the shell,
  `DocumentTabs` is hidden until a foil opens (`ModelArea.axaml`:28, `ModelArea.axaml.cs`:19). **Inferred:** a hidden
  Avalonia control is absent from the macOS AX tree, so Start, Opening and Open-failed would fail
  `REVIEW_SURFACE_NOT_READY` even with the shell on. Confirm by one attach on the Start screen.

**Follow-on F-ATTACH (code, before the operator session):** let review mode keep the Dock shell and its REVIEW title,
and give the Start states a readiness predicate the attach contract accepts. Owner: the D3a lane (src/) plus the
CO-UI-READY control owner (`review-attach.mjs`, `tools/test-review-attach.mjs`). Not edited here (src/ out of scope).

## 2. Copy parity — COPY record vs the built strings

`DESIGN.md` §7 gains COPY-125 to COPY-139, verbatim from design §11. COPY-103, COPY-104, COPY-105, COPY-108,
COPY-118, COPY-106 and COPY-122 already existed. Each built string was read from source at `025afae`.

| COPY | Where built | Built string | Result |
|---|---|---|---|
| COPY-08 | `Panes/StartView.axaml`:38 | Start with a foil, then make it yours. | match |
| COPY-103 | `StartView.axaml.cs`:69, :123 | title + second line | match (split across two lines) |
| COPY-104 | `StartView.axaml.cs`:51 | Opening {fileName}… | match |
| COPY-105 | `ShellHost.cs`:289–290 → `CancelOpening()` | *(nothing)* | **C-1 missing:** Cancel hides the panel and moves focus; no status message |
| COPY-125 | `StartView.axaml.cs`:78, :96; buttons `StartView.axaml`:14–17 | match, incl. Locate…, Open another file…, Remove from Recent | copy match; behaviour fails (C-10, C-11) |
| COPY-126 | `StartView.axaml.cs`:102 | match | match |
| COPY-127 | `StartView.axaml.cs`:106 | match | match |
| COPY-128 | `StartView.axaml.cs`:111 | match | match |
| COPY-129 | `StartView.axaml.cs`:115 | It is larger than CFD Workbench can open. The file hasn't been changed. | **C-2 mismatch:** `(<limit>)` is absent |
| COPY-130 | `StartView.axaml.cs`:119 | match | match |
| COPY-125–130 in the alert band (foil open) | `ShellHost.cs`:281 | Failed to open {fileName}: {failed.Failure} | **C-3 mismatch:** design §11 puts the same strings in the alert band; the build interpolates the `OpenFailure` record (`ShellHost.cs`:217 constructs one with a code and the path), so it shows the type and its fields, probably including the path (Inferred) |
| — | `StartView.axaml.cs`:88 | An error occurred while opening the file. | **C-4 uncatalogued** fallback string |
| — | `ShellHost.cs`:296, :302 | Explicit candidate IDs available for insertion. · Opening refused ({code}): foil opened as read-only. | **C-5 uncatalogued** (ID-candidate and refused alert-band strings; §11 names the states but gives no string) |
| COPY-136, COPY-137 | `Panes/PropertiesPane.axaml`:20–21, `PropertiesView.cs`:51–53 | match | match |
| COPY-136 (Browser) | `Panes/BrowserPane.axaml`:20–21 | No foil open + Open or start a foil to browse its stations. | **C-6 uncatalogued** caption; §11 says a single rendering |
| COPY-138 / COPY-139 | `BrowserPane.axaml`:13, `PropertiesPane.axaml`:13, `RailEditorPane.axaml.cs`:127 | <Pane> couldn't be shown. · Try again | partial: COPY-139 matches; **C-7 missing:** COPY-138 (foil open) is never chosen |
| COPY-118 / COPY-106 | `Panes/PropertiesPane.axaml.cs`:158 | Invalid span. Enter a positive number. | **C-8 mismatch** with both rows |
| COPY-122 | `PropertiesView.cs`:94–96 | Set these in the workspace. | match |
| COPY-108 | `PropertiesView.cs`:138 | Tip closes — edit the tip station | match |
| COPY-125–130 buttons | `StartView.axaml`:14–17; handlers searched in all of `src/CfdWorkbench.Desktop` | Locate…, Open another file…, Try again, Remove from Recent | **C-10 Blocker, dead controls:** none of the four has a Click handler (only New foil, `ShellHost.cs`:195, and Dismiss, `StartView.axaml.cs`:17, do). Focus lands on a button that does nothing |
| COPY-125 from Recent | `ShellHost.cs`:285 | Remove from Recent never shown | **C-11:** `ShowAlert` is never passed `fromRecent`, so the Recent variant cannot appear |
| COPY-125 (missing example) | `StartView.axaml.cs`:76–83 | New foil instead of Locate… | **C-12:** the missing-example alert has no COPY row of its own |
| COPY-127 | `StartView.axaml`:15–16 | Open another file… before Try again | **C-13:** button order differs from the COPY row |
| — | `StartView.axaml`:19, `ModelArea.axaml`:18 | Dismiss | **C-14 uncatalogued** button label |

Not in the M1.2a build (no row checked): Span not assessed, Layout fallback / newer / not kept (×2), Recent not
cleared, Floats clamped, Maximize, the Dock empty strings, COPY-116, COPY-120, COPY-131–135. Span not assessed and
Recent not cleared are M1.2a states (typed Span, Recent), so their absence is **C-9**.

**Fix list for the Sonnet AXAML/C# edit after this track:** C-1 to C-9 and C-12 to C-14. C-4, C-5, C-6, C-12 and
C-14 first need a copy decision: add a COPY row, or remove the string. **C-10 and C-11 are behaviour, not copy:** they
need wired handlers and tests (a D3a-lane coding follow-on), and C-10 blocks M1.2a on its own. C-10 and C-11 were found
by the UX & Accessibility lens (§5) and confirmed by the author with a source search.

## 3. Native rows (M1.2a)

Every row is **operator-run, not done**. The "Compare with" column names the v10 mockup state. Source-read AX
notes are labelled; none is native evidence.

| # | Row | Attach proof | Compare with (v10) | Source-read AX note (not native evidence) | Status |
|---|---|---|---|---|---|
| N1 | Start: three cards and Recent | none | Screen "No foil open (first run)", `#start` | Cards carry names New foil, New from example, Open… (`StartView.axaml`:44, :53, :62). Recent list items show the full path (`StartView.axaml.cs`:44); §11 wants the file name with the path as description | operator-run, not done |
| N2 | Opening with Cancel | none | Screen "Opening a file", `#opening` (`role="status"`) | No `AutomationProperties.LiveSetting` anywhere in src/, so Opening… is not announced (SC 4.1.3); Cancel's name is "Cancel opening" | operator-run, not done |
| N3 | Open-failed | none | Screen "File didn't open", `#open-error` (`role="alert"`) | Focus moves to the first action (`StartView.axaml.cs`:130–133). No alert role or live setting on `AlertPanel` | operator-run, not done |
| N4 | Planform workspace and left side bar | none | Screen "Design workspace", `#dock-left`, `#views` | — | operator-run, not done |
| N5 | Properties with the Wing block last | none | `#dock-left` Properties, Wing block | Wing is skipped in the generic loop and built after it (`PropertiesPane.axaml.cs`:52, :90) | operator-run, not done |
| N6 | Browser, rail-editor pane, Section sample tab | none | Browser and model tabs in the design workspace | — | operator-run, not done |
| N7 | File / Edit / Window in the macOS menu bar | none | `#appbar` command set | Built by `NativeMenuBuilder.BuildMenu` in shell mode only (`MainWindow.axaml.cs`:74) | operator-run, not done |
| N8 | ⌘Z in the Span field edits the text | none | Wing Span field | Edit verbs route through `EditVerbRouter` (`Shell/EditVerbRouter.cs`) | operator-run, not done |
| N9 | F6 region cycling | none | region ring order | `ShellHost.BindF6` (`MainWindow.axaml.cs`:71) | operator-run, not done |
| N10 | New foil opens the near-elliptic default | none | Start card "New foil" → workspace | `ShellHost.cs`:229 `Controller.NewFoilAsync` | operator-run, not done |

The M1.2 entry-gate rendered/AX proof (architecture §10.6, Owner Ruling 25) is part of N4 and is also not done.

## 4. Operator steps

Run once, in one session, after follow-on F-ATTACH merges. Batch every foreground request into this one session.

1. In a clean tree at the merged commit: `dotnet publish src/CfdWorkbench.Desktop -c Release -r osx-arm64
   --self-contained false -o <build>`, then `python3 tools/package-application.py --build-dir <build> --output-dir
   <empty-dir> --platform macos`. Record the commit and the `.app` path.
2. Copy the `.app` to a unique path with a unique bundle ID, as in
   `docs/investigations/review-attach-evidence/build2-launch.json`. Run
   `python3 docs/coordination/review-launch-guard.py` first; it must report no unaccounted window.
3. Launch with `CFDW_REVIEW_MODE=1`, `CFDW_REVIEW_PERSONA=keyboard`, `CFDW_REVIEW_STATE=empty` (N1–N3, N10) or
   `example` (N4–N9), `CFDW_REVIEW_THEME=system`. Write the launch receipt with the keys of `build2-launch.json`
   (`pid`, `pidIdentity`, `reviewApp`, `reviewBundleId`, `exitCodeAfterFiveSeconds`, …).
4. In the `cua_repl` runtime: reset the session, then call `attachReview(cua, {appPath, bundleId, expectedTitle,
   launchReceiptSha256, sessionReset: true})` once. Save the receipt JSON.
5. `node tools/check-review-attach.mjs <receipt.json> <launch.json>` must exit 0. On `NATIVE_REVIEW_BLOCKED`, stop:
   record the error in the row and do not relaunch in a loop.
6. For each row N1–N10: save the AX tree and the screenshot beside the receipt, compare with the v10 state named
   in §3, and write the finding. For N7, open each menu. For N8, type in Span, press ⌘Z, and confirm the text reverts
   while the foil does not. For N9, press F6 repeatedly and record the ring order. Turn VoiceOver on for N2 and N3 and
   record what it speaks.
7. Trigger each open failure for N3 (after C-10 and C-11 are fixed): a Recent entry whose file was moved (missing,
   Recent variant), a copy with `chmod 000` (permission), a text file renamed `.foil` (not recognised), a file over
   the size limit (too large). For N2, open a file slow enough to press Cancel, then record where focus returns from
   the Open… card and from a Recent item. Repeat one failure with a foil already open, for the alert band.
8. Keyboard-only pass over N1–N10: a visible focus indicator on every stop (SC 2.4.7) that is never covered
   (SC 2.4.11). In the Span field, enter an invalid value, press Tab and Shift+Tab, and record whether focus can
   leave without Escape (SC 2.1.2) and whether VoiceOver speaks the error (SC 3.3.1, 4.1.3).
9. Measure contrast in light, dark and high-contrast: DangerBrush text on SurfaceSoftBrush (SC 1.4.3), and the focus
   ring against its neighbours (SC 1.4.11). Measure the smallest button and list-item target (SC 2.5.8).
10. VoiceOver on: the Opening status, the open-failed panel, the alert band, the Span error, the Recent list names
    (file name spoken, path as description) and Cancel's focus return. Record the spoken text.
11. Quit the app and confirm with `pgrep -fl CfdWorkbench` (exit 1).
12. Replace "operator-run, not done" in §3 with the evidence paths. M1.2a closes only when all ten rows are attached
    and the §5 veto is cleared by the UX & Accessibility lens on that evidence.

## 5. Adversarial pass — UX & Accessibility (Adversary Mode)

Lens: `ux-accessibility`, a separate agent, 30 September 2026, reading this review, `DESIGN.md` COPY-125–139, design
§11 and the M1.2a source. One repair cycle of two was used (this document only; no src/ edit).

**Verdict: BLOCK — the WCAG 2.2 AA veto is HELD.** No native evidence exists, so keyboard, names and roles, contrast
and announcements are unproven; C-10 blocks on its own. The author does not clear it. It clears only when the lens
re-reviews the operator's attached rows.

The lens confirmed the disposition is honest (every native row operator-run, M1.2a open), the COPY rows are verbatim
(§11 lines 715–732), and C-1, C-2, C-4, C-5, C-6 and C-8 are correct. Its findings (source-read, not native evidence):

| # | Severity | Finding | Location | Follow-on |
|---|---|---|---|---|
| A1 | Blocker | Four open-failed buttons have no handler (C-10) | `StartView.axaml`:14–17 | wire and test |
| A2 | Major | Remove from Recent can never appear (C-11) | `ShellHost.cs`:285 | pass `fromRecent` |
| A3 | Major | Cancel from Open… or Recent returns focus to nothing (SC 2.4.3) | `ShellHost.cs`:240, `StartView.axaml.cs`:59 | pass the originating control |
| A4 | Major | No live region anywhere; the alert band does not move focus to its first action (SC 4.1.3; §11 "first action on show") | `ModelArea.axaml.cs`:22–30; no `LiveSetting` in src/ | status live region; focus on show |
| A5 | Major | Span error is not tied to the field or announced; Tab and Shift+Tab are swallowed on an invalid value, Escape is the only way out and nothing says so (SC 3.3.1, 4.1.3, 2.1.2); a raw `{code}: {message}` can reach the user | `PropertiesPane.axaml`:39–46, `PropertiesPane.axaml.cs`:135–142, :173 | associate, announce, say how to leave |
| A6 | Minor | Copy gaps C-12, C-13, C-14 | §2 | copy decision |
| A7 | Minor | Recent items are named by the full path (SC 1.3.1) | `StartView.axaml.cs`:44 | file name + path description |

The author confirmed A1 and A2 with a source search (no Click handler for the four buttons; no `fromRecent` argument).
A3–A5 and A7 are the lens's source reading, not re-observed by the author. Residual risk (lens): how Avalonia's macOS
AX tree exposes hidden controls and live regions is unknown until VoiceOver runs.

**Code follow-on list (not edited here):** F-ATTACH (§1.1); A1–A5 and A7 (D3a lane, with tests); copy fixes C-1 to
C-9 and C-12 to C-14 (Sonnet AXAML/C# edit).

