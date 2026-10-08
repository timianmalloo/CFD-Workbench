---
id: proof-cpy-cause-rows
title: "Track CPY, Ruling 148 - proposed plain-cause rows for DOC-/DSL- codes"
type: doc
status: done
owner: "@trk-cpy"
phase: design
tags: [copy, ruling-148, save, open, error-copy]
links:
  - {to: rulings, rel: depends-on}
review-by: 2027-04-01
summary: >-
  Measured inventory of every DOC-/DSL- code src/ can throw or return, with the surface and today's text, and
  proposed cause rows COPY-412 to COPY-430 for the operator to sign off (Ruling 148). Approved as Ruling 155 and wired in Labels.cs (track CWR).
---

# Ruling 148: cause rows (all rows "approved - Ruling 155"; wired by track CWR)

Branch `docs/cpy-cause-rows`, base main 08f9b5f1. Docs only. DESIGN.md, Labels.cs and OpenOutcome.cs are untouched.

Method. Code list: `git grep -o -E '"(DOC|DSL)-[A-Z0-9-]+"' -- src` (67 distinct codes, 32 files). Surfaces were read in
`Labels.cs:319-322`, `OpenOutcome.cs:66-103`, `StartView.axaml.cs:82-91`, `MainWindow.axaml.cs:125-193`,
`SaveSectionDialog.axaml.cs:66-82`, `WorkbenchController.cs` (2150-2180, 2410-2420, 2650-2690, 2738-2742, 2940-3020),
`ProjectStore.cs:76-170, 270-280`, `Program.cs:39-46`. "Internal guard" means the throw site is a `Guard.Require`/state check on
a call a person cannot make directly; I did not trace every site to prove it is unreachable from the UI, so the generic rows
(COPY-423, COPY-431) are the safety net for them.

## 1. Inventory

Text quoted is what the person sees today. "Approved" means a DESIGN.md row exists (COPY id given).

### 1a. Save and persistence (strip = status strip via `Labels.SaveRefusal` / `SaveFailureText`)

| Code | First throw sites | Surface | Text today | Status |
|---|---|---|---|---|
| DOC-IO | ProjectStore:85,151,167,276-278; PreferenceStore:249 | save strip; open alert (Unreadable) | strip: "Save failed: DOC-IO — the previous file is intact and your changes are kept. Retry or Save As." (COPY-31 with raw code). Open: COPY-127. `MainWindow.Guarded`: "DOC-IO: File operation failed. Accepted source retained." (stderr) | strip: draft COPY-412. Open: approved COPY-127 |
| DOC-DISK-FULL | ProjectStore:276 (errno 28), :318 | save strip | "Save failed: DOC-DISK-FULL — ..." | draft COPY-413 |
| DOC-CONFLICT | ProjectStore:79,111,126-132,149,194-207,309,360; errno 17/2 at :276; PreferenceStore:171-391 | save strip; section save (SectionLibrary:94); preference saves | "Save failed: DOC-CONFLICT — ..." | draft COPY-414 |
| DOC-CANCELLED | ProjectStore:82,158,256; PreferenceStore:401-435 | save strip | "Save failed: DOC-CANCELLED — ..." | draft COPY-415 |
| DOC-SAVE-PENDING | AuthoringSession:1913; WorkbenchController:2947,2990 | save strip (thrown, `SaveFailureText`) | "Save failed: DOC-SAVE-PENDING — ..." | draft COPY-416 |
| DOC-CLOSED | ProjectStore:57,66,186; AuthoringSession (40 sites) | save strip; otherwise internal | "Save failed: DOC-CLOSED — ..." | draft COPY-417 |
| DOC-SIZE | ProjectStore:15,298,305; AuthoringSession:1920,2189-2216 | save strip; open alert (TooLarge) | strip: "Save failed: DOC-SIZE — ...". Open: COPY-129 | strip: draft COPY-418. Open: approved COPY-129 |
| DOC-TYPE | WorkbenchController:2944,2553; MainWindow:179; Program:260 | save strip (file name); open alert | strip: "Save failed: DOC-TYPE — ...". Open: COPY-128 | strip: draft COPY-419. Open: approved COPY-128 |
| DSL-INVALID-NUMERIC | WorkbenchController:2825,2945,3224; LengthExpression:66 | save strip (draft number unfinished) | "Save failed: DSL-INVALID-NUMERIC — ..." | draft COPY-420 |
| DSL-DRAFT-OWNED | AuthoringSession (26 sites); WorkbenchController:2942 etc. | save strip; edit surfaces (see 1c) | strip: "Save failed: DSL-DRAFT-OWNED — ..."; commit: "Finish the current change first." (WorkbenchController:2387, no COPY id found) | strip: draft COPY-420; commit text: no COPY id, left as is |
| DOC-SAVE-UNCERTAIN | ProjectStore:144,158-218; WorkbenchController:2946,2969,2989; SectionLibrary:97 | save strip; Save section dialog; CLI exit 5 | strip: "DOC-SAVE-UNCERTAIN: Save was not acknowledged. The attempted path and image are retained for a durable retry." (WorkbenchController:2978, raw code first, no COPY id). Retry: "...: Uncertain save remains dirty; disk image could not be compared." (:3002), "...: Matching readback did not confirm durability; save remains dirty." (:3019). Section dialog: "CFD Workbench couldn't confirm whether this section was saved. Check My sections before trying again." (no COPY id found) | strip: draft COPY-421. Dialog sentence: plain and honest, no code; see note N3 |
| DOC-HASH, DOC-OPERATION, DOC-SAVE-CAPTURE, DOC-INTEGRITY, DOC-ID, DOC-EMPTY | ProjectStore:16,17,189; AuthoringSession:1922,2035,2183,2184,127; Freshness:25 | save strip when hit; otherwise internal | "Save failed: <code> — ..." | draft COPY-422 |
| DOC-UNSUPPORTED-PERSISTENCE | ProjectStore:85,105,270-380; Labels:322; PreferenceStore:316-535 | save strip (warning kind); Save section dialog; CLI exit 3 | "Saving isn't available on this system yet — your changes are kept in this session." (COPY-31W). Dialog: "This system can't save to My sections safely. Nothing was saved." (no COPY id found) | approved COPY-31W; do not repeat |
| LIB-* (LIB-IO, LIB-NAME-*, LIB-CLAIM-HELD, LIB-SECTION-INVALID) | SaveSectionDialog:73-78 | Save section dialog | plain sentences, no code | out of Ruling 148 scope (not DOC-/DSL-) |

### 1b. Open (StartView alert; text never shows the code)

| Code | Throw / map | Text today | Status |
|---|---|---|---|
| DOC-NOT-FOUND, FILE-NOT-FOUND | OpenOutcome:67,87 | COPY-125 | approved |
| DOC-ACCESS, EACCES | OpenOutcome:74,89 | COPY-126 | approved |
| DOC-IO | OpenOutcome:80,91 | COPY-127 | approved |
| DOC-SIZE, DSL-LIMIT | OpenOutcome:93 | COPY-129 | approved |
| DOC-VERSION, DSL-VERSION | OpenOutcome:95 | COPY-103 (COPY-30 for the status line) | approved |
| DOC-UNSUPPORTED-FIELD | OpenOutcome:97 | COPY-130 | approved |
| DOC-SCHEMA, DOC-TYPE, DOC-REFERENCE, every other DSL-*, any unknown code | OpenOutcome:99-102 (both branches return NotRecognised) | COPY-128 (the `_` arm of `StartView.FailureMessage` is the same sentence) | approved, already a generic cause for an unknown code |

Gap against Ruling 148: none of COPY-125..130/103/128 prints the code. See COPY-424.

### 1c. Editing, source and analysis refusals

| Code(s) | Throw sites (first) | Surface | Text today | Status |
|---|---|---|---|---|
| DSL-LOCK (55), DSL-CURVE (46), DSL-GROUP-HANDLE/-NEIGHBOUR/-RANGE/-SPAN-SET, DSL-PROFILE-ORDER, DSL-PROFILE-CROSS, DSL-EDGES-CROSS, DSL-UNIT, DSL-GEOMETRY, DSL-TOLERANCE, DSL-TIP-CHORD-MIN | AuthoringSession:1071,1119-1196,1349,1388-1439,1491,1790; ChannelEdits:31-141; ChordDimension:33-43; TipChord:13 | point and handle gestures, Properties pane, Section editor | Most carry specific approved text: ChannelEdits messages ("The root end can't be removed: the curve starts there."), COPY-123, COPY-400, PropertyCopy, `GroupCopy`. Fallback when no reason exists: "<code>: This change wasn't applied. Nothing changed." (WorkbenchController:2160,2176,2416; no COPY id) | specific texts approved; fallback draft COPY-425 |
| DSL-TARGET (27), DSL-PROFILE-TARGET (34), DSL-ID, DSL-CONFLICT (14), DSL-STALE, DSL-VALIDATION-BUSY, DSL-DRAFT-REUSED, DSL-CANCELLED, DOC-OPERATION-CONFLICT | AuthoringSession:297,575,881-894,386,427,656,942,574,991,220,505,1250; SectionEdits:565; WorkbenchController:1379 | same gesture surfaces; `DSL-CONFLICT`/`-VALIDATION-BUSY` are swallowed at WorkbenchController:1239 | fallback as above | draft COPY-426 |
| DSL-SYNTAX (18), DSL-LEX (14), DSL-LEGACY, DSL-REFERENCE, DSL-INVALID, DSL-UNSUPPORTED | FoilSource:170,206,1261-1269,1456-1525; SectionEdits:13,556; Program:85 | Foil source editor / open-for-edit refusal | "<code>: <reason> <recovery>" (WorkbenchController:2741; reason and recovery are per-diagnostic strings from FoilSource, e.g. "Input is not valid UTF-8." "Repair the source encoding."); otherwise "<code>: Refused. Original source retained read-only." (:2656,2683) | draft COPY-427 |
| DSL-VERSION | FoilSource:212-216 | same | as above | draft COPY-428 |
| DSL-LIMIT | FoilSource:165; Program:27,36 | same; CLI exit 4 | "Source exceeds 1 MiB." "Retain the original input and reduce its size." (diagnostic) with raw code prefix | draft COPY-429 |
| DSL-IMPORT (16) | DatImport:41-711 | .dat import report (line number is the detail); no Desktop copy site found | reason is a line number | draft COPY-430 |
| DSL-NOT-ASSESSED (18) | AnalysisService:95; AuthoringSession:425,1492,1677; WorkbenchController:1020,1631,2870,3218 | Properties pane, gestures, commit, analysis | "This change couldn't be checked, so it wasn't applied. Nothing changed." (:2172); "This foil couldn't be checked. Nothing changed." (:2389); Properties: "The new <label> couldn't be checked. <label> is unchanged. Try again or enter a different value." (COPY-145 pattern). Some status lines still lead with the raw code (:2843, :3238) | approved wording exists; do not repeat |
| DOC-REFERENCE (31), DOC-RUN-ID/-KEY/-STRIPS, DOC-POLAR-KEY, DOC-NO-RECOVERY, DOC-RECOVERY-BASE, DOC-SESSION-NOT-EMPTY, DSL-PATCH (45), DSL-RANGE, DOC-EXAMPLE-MISSING | AuthoringSession:2007,2308,1860,1874,1929; RunRecord:261-313; ChordDimension:60-226; FoilSource:188; Program:16 | internal guard; not traced to a UI path | none; if hit, `Guarded` prints "<code>: Action refused; accepted source retained." to stderr, or a strip/status shows the raw code | covered by generic COPY-423 and COPY-431 |
| Analysis failure (any ANA- reason code) | AnalysisProjection:37 | Analysis strip | "Analysis failed — <reason> (<code>). The previous result is kept as Historical." (COPY-208) | approved, already in the Ruling 148 shape |

## 2. Proposed rows

House style from DESIGN.md section 7: short, active, says what happened and, where true, that nothing was lost. Save-strip causes
slot into COPY-31: "Save failed: **<cause> (<code>)** — the previous file is intact and your changes are kept. Retry or Save As."
Why the COPY-31 tail is true for the save-strip codes: in `ProjectStore.SaveAsync` any failure after the publish step becomes
DOC-SAVE-UNCERTAIN (`code = published ? "DOC-SAVE-UNCERTAIN" : error.Code`, ProjectStore:159-167, 212-218), so every other code means
the target was not replaced. The unknown-code row (COPY-423) cannot lean on that and does not claim it.

All rows: **approved - Ruling 155** (operator).

### Save failures

| Proposed ID | Text | Codes | Surface | Why the wording is true for every code in the row |
|---|---|---|---|---|
| COPY-412 | Save failed: the disk couldn't be written (DOC-IO) — the previous file is intact and your changes are kept. Retry or Save As. | DOC-IO | save strip | The ruling's own example. Every DOC-IO site is a failed read, write, fsync or cleanup before publish (ProjectStore:85,151,276-278,320-323). |
| COPY-413 | Save failed: the disk is full (DOC-DISK-FULL) — the previous file is intact and your changes are kept. Free some space, then Retry or Save As. | DOC-DISK-FULL | save strip | Only source is errno 28 (ENOSPC) at ProjectStore:276 and the write hook at :318. |
| COPY-414 | Save failed: the file is in use by another program, or it changed since you opened it (DOC-CONFLICT) — the previous file is intact and your changes are kept. Close the other program and Retry, or Save As. | DOC-CONFLICT | save strip; section save; preference saves | Every site is a check that the target is still the one we read or hold exclusively (disk hash differs, entry replaced, claim not owned, errno 17/2). "In use" is Ruling 145 (3); "changed" covers the hash and entry-swap sites. |
| COPY-415 | Save failed: the save was cancelled (DOC-CANCELLED) — the previous file is intact and your changes are kept. Retry or Save As. | DOC-CANCELLED | save strip | Cancel after publish is reported as DOC-SAVE-UNCERTAIN (ProjectStore:158), so this code means nothing was published. |
| COPY-416 | Save failed: another save is still running (DOC-SAVE-PENDING) — the previous file is intact and your changes are kept. Wait for it to finish, then Retry. | DOC-SAVE-PENDING | save strip | Thrown by the `saving` interlock before any write (WorkbenchController:2947,2990; AuthoringSession:1913). |
| COPY-417 | Save failed: this project was closed (DOC-CLOSED) — the previous file is intact and your changes are kept. Open the project again. | DOC-CLOSED | save strip | Disposed store returns before any write (ProjectStore:57). Wording for the strip only; the same code elsewhere is internal. |
| COPY-418 | Save failed: the project is larger than CFD Workbench can save (DOC-SIZE) — the previous file is intact and your changes are kept. Save As won't help; remove content first. | DOC-SIZE | save strip | Raised before writing: image length over `NativeProject.MaxBytes` (ProjectStore:15). Limit value is not quoted because I did not read `MaxBytes`; the operator may want it, as COPY-129 does ("8 MB"). |
| COPY-419 | Save failed: the file name must end in .cfdw.json (DOC-TYPE) — the previous file is intact and your changes are kept. Choose another name. | DOC-TYPE | save strip | WorkbenchController:2944 is the only save-path throw; checked before any write. |
| COPY-420 | Save failed: a change is still in progress (DSL-DRAFT-OWNED, DSL-INVALID-NUMERIC) — the previous file is intact and your changes are kept. Finish or cancel the change, then Save. | DSL-DRAFT-OWNED, DSL-INVALID-NUMERIC | save strip | Both are checked at WorkbenchController:2942-2945 before any write: an edit gesture or draft is open, or a draft number is incomplete. Show only the code that fired in the parentheses. |
| COPY-421 | Save not confirmed: CFD Workbench couldn't tell whether the file was fully written (DOC-SAVE-UNCERTAIN). Your changes are kept and still marked unsaved. Retry to check the file, or Save As. | DOC-SAVE-UNCERTAIN | save strip (error kind) | Deliberately says neither "saved" nor "not saved": the code means the file was replaced (or may have been) but durability was not confirmed (ProjectStore:144,158-167). It does not claim the previous file is intact. "Still marked unsaved" is the controller state (:2969-2978, 3002, 3019). "Retry to check" matches the retry path that compares the disk image (:2989-3019). |
| COPY-422 | Save failed: CFD Workbench's own check of the project data failed (<code>) — the previous file is intact and your changes are kept. Retry; if it happens again, Save As and report it. | DOC-HASH, DOC-OPERATION, DOC-SAVE-CAPTURE, DOC-INTEGRITY, DOC-ID, DOC-EMPTY | save strip when hit | All are precondition checks on the image or session before any write (ProjectStore:16,17,189; AuthoringSession:127,1922,2035,2183). Not caused by anything the person did, so the cause says so. |
| COPY-423 | Save failed: something unexpected stopped the save (<code>). Your changes are kept in this session. Check the file before relying on it, then Retry or Save As. | any other code from `Labels.SaveRefusal` | save strip (generic) | Does not claim the previous file is intact: an unknown code with `PublicationKnown` true and durability confirmed would reach the `else` branch (WorkbenchController:2967-2979) without being classed uncertain. Always carries the code. |

### Open

| Proposed ID | Text | Codes | Surface | Why the wording is true |
|---|---|---|---|---|
| COPY-424 | Add one line under the approved open message of COPY-103, 125-130 and 128: "Code: <code>" (for example "Code: DOC-IO"). No other change to those rows. | every `OpenFailure` code, including unknown ones | StartView alert | Ruling 148 wants the code after the cause. Approved open rows are plain causes already and COPY-128 is the generic cause for an unknown code; they lack only the code. A separate line avoids editing six approved sentences and keeps their buttons and focus order. `OpenFailure.Code` already carries the string. |

### Editing and source (fallbacks, used only when the code has no specific approved text)

| Proposed ID | Text | Codes | Surface | Why the wording is true |
|---|---|---|---|---|
| COPY-425 | This change wasn't applied: the shape can't take it as entered (<code>). Nothing changed. Adjust it and try again. | DSL-LOCK, DSL-CURVE, DSL-GROUP-HANDLE, DSL-GROUP-NEIGHBOUR, DSL-GROUP-RANGE, DSL-GROUP-SPAN-SET, DSL-PROFILE-ORDER, DSL-PROFILE-CROSS, DSL-EDGES-CROSS, DSL-UNIT, DSL-GEOMETRY, DSL-TOLERANCE, DSL-TIP-CHORD-MIN | gesture, Properties, Section editor fallback | Each is a rule refusal of a requested edit: locked point, curve fold or too few points, group range, crossing edges, non-positive length, fair/rail result out of tolerance, tip chord floor. "Nothing changed" is the existing fallback's own claim (WorkbenchController:2160,2176). |
| COPY-426 | This change wasn't applied: what it pointed at has changed or is busy (<code>). Nothing changed. Try again. | DSL-TARGET, DSL-PROFILE-TARGET, DSL-ID, DSL-CONFLICT, DSL-STALE, DSL-VALIDATION-BUSY, DSL-DRAFT-REUSED, DSL-CANCELLED, DOC-OPERATION-CONFLICT | same | Each means the target point/profile no longer exists, another edit or validation owns the draft, the preview went stale, or the operation was superseded. I read the site names and a sample of throws, not every one of the 27 DSL-TARGET sites, so the operator or reviewer should confirm DSL-TARGET (note N2). |
| COPY-427 | This source can't be accepted: it isn't valid foil source (<code>). The original is kept, read-only, and nothing was changed. | DSL-SYNTAX, DSL-LEX, DSL-LEGACY, DSL-REFERENCE, DSL-INVALID, DSL-UNSUPPORTED | Foil source editor / refusal strip, shown with the diagnostic's own reason and recovery when present | The parse failed at a syntactic, lexical, legacy-keyword or reference diagnostic. WorkbenchController:2656,2683 retain the original read-only. DSL-UNSUPPORTED is handled as not-assessed by the session (AuthoringSession:969); the wording stays true but the operator may prefer to give it the DSL-NOT-ASSESSED text. |
| COPY-428 | This source can't be accepted: it was written for a version of the foil format this build doesn't read (DSL-VERSION). The original is kept, read-only, and nothing was changed. | DSL-VERSION | same | FoilSource:212-216 throws when the header is not "4.0"; the file may be newer or older, so the wording avoids "newer". |
| COPY-429 | This source can't be accepted: it is larger than CFD Workbench can read (DSL-LIMIT). The original is kept, read-only, and nothing was changed. | DSL-LIMIT | same | FoilSource:165 ("Source exceeds 1 MiB."). Add "(1 MiB)" if the operator wants the number, as COPY-129 does. |
| COPY-430 | This file can't be imported as a section (DSL-IMPORT) — <reason, with the line number>. Nothing was changed. | DSL-IMPORT | .dat import report | All 16 throw sites are shape or parse failures of the file text; the line number is the detail (DatImport:41-711). No Desktop copy site found, so this row is for the import surface when it is built. |
| COPY-431 | Something unexpected stopped this (<code>). Nothing changed. | any other DOC-/DSL- code on a non-save, non-open surface (`Guarded` stderr line, status lines) | generic fallback | Claim "Nothing changed" holds only for the refusal surfaces named in 1c, which retain the accepted source (MainWindow:132-137). Not for save, which uses COPY-423. |

## 3. Notes for the operator

- N1. COPY-424 is the only row that touches approved copy (it adds a line). If you prefer the code inline, COPY-103 and 125-130 each
  need an amended sentence; say so and I will draft those.
- N2. COPY-425 and COPY-426 are fallbacks. The specific approved sentences (ChannelEdits, GroupCopy, PropertyCopy, COPY-123,
  COPY-400) win when a reason exists. DSL-TARGET mixes "no longer there" and "wrong command" sites; the row takes the first reading.
- N3. Unlisted COPY ids (no id found in DESIGN.md): the Save-section dialog sentences (SaveSectionDialog:79-81), the commit
  sentences at WorkbenchController:2172 and 2389, and the "Saved and durability confirmed." status. They are plain already; the
  dialog's fallback `_` arm shows "(<code>)" in parentheses, which already matches Ruling 148.
- N4. The CLI prints codes and exits through `Program.ExitForCode` (Program.cs:39-46). I treated CLI output as out of scope for
  the plain-cause rule (a person at a terminal diagnoses by code). Confirm.
- N5. Counts in the inventory come from one grep run on main 08f9b5f1 and count quoted string literals in src/ only (no tests, no comments); a code's first
  sites are shown, not all of them.
