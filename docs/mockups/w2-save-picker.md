---
id: mockup-w2-save-picker
title: W-2 save picker — OneDrive refusal and unfinished-save recovery
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, w2, save-picker, onedrive, crash-recovery, windows]
links:
  - {to: design-language, rel: depends-on}
  - {to: rulings, rel: implements}
  - {to: mockup-m12d-catalog, rel: refines}
review-by: 2026-12-31
summary: >-
  Three surfaces around the native Windows save dialog, shown before any build: the first Save of a new project (what
  CFD Workbench controls in the native dialog), the refusal when the user picks a OneDrive folder, and the block left by
  a crashed save with its Clear unfinished save step. Each of S2 and S3 has two variants, a modal dialog (approved) and the alert
  band (rejected). Ruling 146 copy is verbatim; the new rows COPY-440 to COPY-457 are approved by Ruling 160, which also picked variant A for S2 and S3. This page is now the approved reference; variant B stays in the harness, labelled rejected.
review-suggested: []
---

# W-2 save picker — the mockup

Open [`w2-save-picker.html`](w2-save-picker.html) over `file://`. The header switches the state (S1 to S5), its detail,
the variant (A modal dialog, approved and the default; B alert band, rejected by Ruling 160), the theme, the viewport (1280 × 800, 1500 × 870) and reduced motion. The box
"Another window holds this project" chooses the outcome of Clear unfinished save. Captures are in
[`w2-save-picker/`](w2-save-picker/). Oracle: `node tools/check-mockup-svp.mjs <node_modules dir> [<shot dir>]`.

**Display only.** The Windows store stays fail-closed (Ruling 145 (4)); this UI ships when W-2 B2 lands. Nothing under
`src/` or `tests/` changed.

## Direction (in words)

The save flow is a **moment of refusal**, not a feature. The user is trying to keep their work; every sentence must say
what happened, that the saved file is safe, and the one next step. Calm, plain cause, no raw code alone (Rulings 155,
158). The window already has two homes for a message: the **status strip** (a result) and the **alert band** (an
assertive notice with actions). A third home, the **modal dialog**, exists for a decision (`SaveSectionDialog`,
`CatalogDialog`). The two variants differ in which home carries the refusal. The native Windows picker is never
restyled or imitated in the product: the mockup draws it as a dashed stand-in.

## Archetype Signature

`NativeFileWorkbench { Type:OLTP; Arch:SPA; Layout:MasterDetail; Density:Compact; Nav:Breadcrumb+Sidebar+CommandPalette; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:Utilitarian; Depth:Flat; Sync:LocalFirst; Persistence:Cloud→LocalDevice; Feedback:Confirmed+Instant; Motion:None; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA+HighLegibility; }`

Adjusted from the catalog row of the same name. Persistence is **LocalDevice** (the whole point of Ruling 135) and Motion is
**None** (modal dialogs hard-cut, DESIGN.md section 6). Facets are not re-derived per screen: this is a modal flow inside the
existing ViewportWorkbench shell (`ParametricWorkbench`, DESIGN.md), so the shell's facets stand. Fit to the task:
**a modal refusal is serial** (decide, then continue), which matches a dialog; a band suits a *result* the user may
ignore, which a blocked save is not.

## Triggered standards

UI-T4 (native client) fires: Avalonia, Windows. UI-T1 (expert/quantitative), UI-T2 (generated assets), UI-T3 (fronts a
model) do not fire: no quantities, no imagery, no model. The unconditional floor applies (complete states, tokens only,
WCAG 2.2 AA, keyboard).

## States

| State | What it shows | Variants |
|---|---|---|
| S1 | The native dialog stand-in; the three things the app controls (title, start folder `%USERPROFILE%\CFD Workbench`, suggested name `foil.cfdw.json`). | none |
| S2 | Detection after the pick and before any write. Nothing is written into OneDrive. | **A dialog (approved)**; B band + picker reopens (rejected) |
| S3 | A crash-left unfinished save blocks Save. The document stays dirty. | **A dialog (approved)**; B band (rejected) |
| S4 | The confirm step of Clear unfinished save, swapped in place into the S3 dialog (one dialog, no stack); outcome a (cleared, ordinary Save proceeds, no recovery announcement); outcome b (refused, another window holds it). | none approved; B kept for reference |
| S5 | Floor: picker open and app waiting; Save in progress; picker failed to open; OneDrive check failed (follows the S2 variant); Clear failed; default folder missing (the folder is created silently at the first Save, then the picker opens there). | check-failed follows the variant |

**Flows.** S2: Pick, then Check (before any write), then Refuse. S3-A: Save As… reopens the picker; Clear unfinished
save… swaps the dialog in place to the S4 confirm; Cancel closes and returns focus to Save. S4: Cancel and Escape swap back to the S3 content with focus on Clear unfinished save…;
Clear unfinished save runs the no-live-writer check, then clears and the ordinary Save runs, or refuses.

**Keyboard** (each has a handler and an oracle assertion): focus lands on the default button of a dialog; Escape closes it
(Cancel) and returns focus to what opened it; Enter activates the default button; Tab and Shift+Tab stay inside. The
native dialog keeps Windows's keys. In S3-B and S2-B the band is an assertive live region and takes no focus; in S2-B
focus moves to the reopened native picker.

**Mac.** macOS uses the native macOS save panel and has no OneDrive rule.

## Copy

Ruling 146 (1), (2) and (3) are used verbatim; the oracle reads them from `docs/notes/rulings.md` and compares every
rendered sentence. The crash text is shown as a lead paragraph plus an ordered list of its three steps; the oracle
reassembles it to the ruling's exact string. Proposed rows are in the house voice of Rulings 146, 155 and 158 (result
first, "Your saved file …" reassurance, one next step, `(<code>)` at the end). The page shows the existing code `DOC-IO` in
place of `<code>`; the real code names are a build decision.

| ID | Text | Status | Where |
|---|---|---|---|
| COPY-440 | Can't save to OneDrive | approved — Ruling 160 | S2-A dialog title |
| COPY-441 | Save is blocked | approved — Ruling 160 | S3-A dialog title |
| COPY-442 | Choose another folder… | approved — Ruling 160 | S2-A primary button; S5 OneDrive-check dialog |
| COPY-443 | Save As… | approved — Ruling 160 | S3 primary button (dialog and band) |
| COPY-444 | Clear unfinished save… | approved — Ruling 160 | S3 action that opens the confirm step |
| COPY-445 | Cancel | approved — Ruling 160 | every dialog and the S3 band; reuse the existing Cancel label if the Labels lookup has one |
| COPY-446 | Clear the unfinished save? | approved — Ruling 160 | S4 confirm title |
| COPY-447 | This removes the leftover files from the save that didn't finish. Your saved file and your current work are not touched. It works only if no other CFD Workbench window is using this project. | approved — Ruling 160 | S4 confirm body |
| COPY-448 | Clear unfinished save | approved — Ruling 160 | S4 confirm action (the Ruling 146 action name, no ellipsis because it acts) |
| COPY-449 | Can't clear it yet: another CFD Workbench window is still using this project. Close that window, then try again. Your saved file and your current work are unchanged. | approved — Ruling 160 | S4 outcome b, inside the confirm dialog |
| COPY-450 | Try again | approved — Ruling 160 | S4 outcome b and Clear failed; reuse the band's existing label |
| COPY-451 | Choose where to save in the Windows dialog. | approved — Ruling 160 | S5 status strip while the picker is open |
| COPY-452 | Saving… | approved — Ruling 160 | S4 outcome a and S5 status strip during a Save |
| COPY-453 | Couldn't open the Windows save dialog (<code>). Nothing was saved. Your changes are kept and still marked unsaved. Try again. | approved — Ruling 160 | S5 status strip, warning kind |
| COPY-454 | Couldn't check whether this folder is synced by OneDrive (<code>). Nothing was saved. Choose another folder. | approved — Ruling 160 | S5 OneDrive check failed (dialog or band) |
| COPY-455 | Couldn't clear the unfinished save (<code>). Your saved file and your current work are unchanged. | approved — Ruling 160 | S5 Clear failed, inside the confirm dialog |
| COPY-456 | Dismiss | approved — Ruling 160 | S2-B and check-failed band; the band's existing Dismiss |
| COPY-457 | Can't check this folder | approved — Ruling 160 | S5 OneDrive-check dialog title |

S4's sentences are COPY-446, 447, 448, 449 (and 450, 455). No row announces a recovery: after Clear, the user sees only
the ordinary Save status (COPY-452), per Ruling 146.

## Decisions (Ruling 160) and build notes

The operator viewed the screenshots and decided everything as Ruling 160 (2026-10-08).

1. **Folder creation (S1): decided.** `%USERPROFILE%\CFD Workbench` is created at the first Save, just before the picker
   opens, and only when missing. The creation is silent (no copy). S5 "Default folder missing" shows it.
2. **S2: decided, variant A** (modal dialog, COPY-440, Ruling 146 (1) text). B is rejected and kept in the harness only.
3. **S3: decided, variant A** (modal dialog, COPY-441, Ruling 146 (2) text). B is rejected and kept in the harness only.
4. **S4: decided.** Clear unfinished save… swaps the S3 dialog's content in place to the confirm (no stacked modal);
   first focus and Enter go to Cancel; Escape and Cancel return to the S3 content.
5. **OneDrive check failure: decided, refuse** (fail closed, Ruling 135), COPY-457 title, COPY-454 body.
6. **Build note, open (waits on the PC):** what "the leftover files" are (COPY-447). The phrase stays until the PC states
   what an unfinished save leaves on disk.
7. **Build note, open (decided at build):** the real code names for the three `(<code>)` rows. The page shows `DOC-IO`.

All rows COPY-440 to COPY-457 are approved as drafted. Reuse the existing Cancel, Try again and Dismiss labels (COPY-445,
450, 456) and an existing Saving status line if one exists (COPY-452). The build ships only when W-2 B2 lands (Ruling 145
(4)). The band-button token finding (`control-line` on `surface-soft`, 2.98:1) is a separate token fix.

## Captures

In `w2-save-picker/` (light unless named): `S1-first-save`, `S2-A-dialog`, `S2-B-band`, `S3-A-dialog`, `S3-B-band`,
`S4-confirm`, `S4a-cleared`, `S4b-refused`, `S2-A-dialog-dark`, `S3-A-dialog-dark`; the S5 floor in `S5-waiting`,
`S5-picker-failed`, `S5-folder-missing`. Each was opened and read after the final oracle run.

## Measurement

- **Oracle** (`tools/check-mockup-svp.mjs`): 9 of 9 groups pass. It checks every rendered sentence against Ruling 146 (read
  from `docs/notes/rulings.md`) and against the rows of the table above (read from this file), 81 sentences across 24 state ×
  detail × variant combinations; no placeholder or NaN in 48 renders; focus in, Escape, Enter and the Tab trap; event order
  (the OneDrive check follows the pick and precedes any write; nothing clears until the confirm click; no recovery wording
  after Clear); one live region; 72 size checks over both viewports (targets at least 24 px, text at least 12 px, dialogs
  inside the shell); 2016 contrast pairs in both themes; no animation in either motion mode; no network request. A
  deliberate copy mutation made it fail, then passed again when reverted.
- **Craft gate** (`ui-craft-gate.py --markdown`): first run 6 Minor (repeating-stripes, a side-tab on the warning strip, 4
  cramped-padding). The stripes and the side-tab were removed. The remaining 4 Minor `cramped-padding` flags are the shell's
  own strips (window, title, plan area, status strip), whose text children carry their own padding; they match the
  existing shell idiom and are accepted. A clean run is a floor, not a verdict: it cannot see the findings below.
- **Not done:** an independent accessibility lens (the author does not clear the a11y veto), a screen-reader pass (operator
  ranks it below function and look; deferred), and anything on a real Windows machine or the real native dialog.

## Critique

Written before Ruling 160; the S2-B, S3-B and S4-stack findings are resolved by it (A chosen, S4 swapped in place). Structure before surface. Severity: Major = change before build; Minor = fix in the build; Note = awareness.

| Location | Dimension | Severity | Evidence | Fix | Confidence |
|---|---|---|---|---|---|
| S2-B | IA, state | Major | The reopened native picker sits over the band (`S2-B-band`); the Ruling 146 sentence is cut off mid-line while the user chooses. | Choose A for S2, or in B keep the band text in the status strip too. | High |
| S3-B | IA, a11y | Major | The band becomes a three-line block and announces about 60 words assertively while Save stays live (`S3-B-band`). | Choose A for S3. | Medium-high |
| S4 stack | IA | Major | S4 opens a second modal over the S3 dialog (`S4-confirm`): two headings, two Cancel buttons, Escape means different things in each. | One dialog that swaps its content to the confirm step and back, so there is one Cancel. Not built here, so the operator can see the stack first. | Medium |
| COPY-447 | Copy truth | Major | "the leftover files" is a generic claim; what the unfinished save leaves is a PC fact, not read here. | The PC names the artefact; reword to say it exactly, or soften to "the unfinished save". | Medium (Inferred) |
| S4 buttons | Hierarchy | Minor | Both buttons have equal weight; Cancel carries the focus ring (`S4-confirm`) and Clear unfinished save is not marked as the action. | Keep Cancel as default; give Clear unfinished save the accent outline. | Medium |
| Band buttons | Token | Minor | `control-line` on `surface-soft` measures 2.98:1 (oracle, first run), under the 3:1 boundary floor. The mockup uses `ink-mute` for band buttons. | Fix in DESIGN.md (a token on the wrong surface, class UI-K), or keep `ink-mute` on the band. | High (Verified) |
| S5 / S1 notes | Harness | Minor | The keyboard list shows in states with no product dialog. | Show it only when a dialog is open. Harness only. | High |
| COPY-452 | Copy | Minor | `Saving…` has no source row in DESIGN.md section 7; it may duplicate an existing status sentence. | Check the Labels lookup before adopting. | Medium |
| S2 detection failure | Flow | Note | COPY-454 refuses when the OneDrive check fails (fail closed). That is Open question 5, not a ruled behaviour. | Confirm in the ruling. | Medium |
| Whole page | State completeness | Note | Empty state of S2 and S3 is "no surface" (nothing to refuse, nothing unfinished); loading is the S5 waiting and saving states; errors are S5 plus S4b. | none | High |

### Ranked plan

1. **Choose A for S2 and S3 (highest leverage).** It removes two of the four hard surfaces from the build (both bands), keeps
   the reason readable, and gives S4 a dialog to return to.
2. **Answer Open question 1** (folder creation). It decides what the Windows start-folder code must do before the picker opens.
3. **Have the PC name what the unfinished save leaves** (COPY-447), then lock the S4 sentences.
4. **Replace the S4 stack** with one dialog that swaps its content.
5. **Approve the proposed rows** (COPY-440 to COPY-457) as one batch, reusing existing Cancel, Try again and Dismiss labels.
6. Move the band-button border fix into DESIGN.md.
