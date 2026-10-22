---
id: design-windows-store-recovery
title: "Windows store recovery design: Clear unfinished save and read-only Open from OneDrive"
type: design
status: in-review
owner: "@timianmalloo"
phase: windows-w-2-design
tags: [windows, persistence, design, security, onedrive, crash-recovery, clear-unfinished-save, read-only-open, ruling-259]
links:
  - {to: design-windows-native-store, rel: refines}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: mockup-w2-recovery, rel: relates-to}
  - {to: mockup-w2-save-picker, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-30
summary: >-
  The store design for Ruling 259. Clear unfinished save removes only the crash leftovers this store created (claim and
  .cfd- temps with the store's private descriptor), only when no live writer holds them (an exclusive DELETE open on
  Windows, an flock on the shared claim code on macOS, compiled and SKIP), claim last, checked twice, never silently and
  never announcing a recovery. Read-only Open admits a fully local file in a OneDrive folder through a narrow placeholder
  exception on Open's walk only, refuses a cloud-only file before any byte is read, and makes the document read-only for
  its location (in-place Save refused with the OneDrive refusal, Save As at the local default folder, a window notice).
---

# Windows store recovery: Clear unfinished save and read-only Open from OneDrive

**Governing ruling:** Ruling 259 (R-OD: Q1 a, Q2 a), read with Rulings 135, 146 (2), 160, 253 (W2-Q3 c), 254 and 257.
**Refines:** `docs/design/windows-native-store.md` sections 4 and 5. **Mockup:** `docs/mockups/w2-recovery.html`, note
`docs/mockups/w2-recovery.md`. **Tier:** T2. **Author:** the DS designer (Opus). The author clears no veto: the Security
lens reviews the placeholder exception and the handling of files not yet downloaded (section 9, Q-S); the Data &
Persistence lens reviews the no-live-writer rule and provenance (Q-D); UX & Accessibility reviews the states (Q-U).
Their vetoes hold until then (Ruling 259).

**Evidence labels.** *Verified* means the author opened the file at `main` 67639259 and read the line named.
*Inferred* means it was not observed; each Inferred claim that bears load carries an `assume:` line with what confirms it
and what breaks if it is false, and the confirming step is a named test in section 7.

**Spec amendment:** none. No row of `docs/specs/cfd-workbench-v1.md` names OneDrive or crash recovery (Verified by a
search of `docs/specs/` for "OneDrive": no match), so no spec amendment note is written.

## 1. Grounding: what the code does today

| Fact | Where (Verified) |
|---|---|
| The claim is `.cfd-writer.claim`, shared by the macOS and the Windows stores. | `ProjectStore.ClaimName()`, `ProjectStore.cs:269` |
| Temps are `.cfd-<guid D>.tmp`; the v1 backup temp is `.cfd-<guid D>.bak.tmp`. | `ProjectStore.TempName`, `BackupTempName`, `ProjectStore.cs:230-231` |
| Windows creates the claim and temps with `CreatePrivate`: access read, write and DELETE, **share read only**, NT create (fails if the name exists), the private owner-only protected DACL, read back before any byte. | `WindowsNative.CreatePrivate` `WindowsNative.cs:211-226`; `Owned.Create` and `HasPrivateDescriptor` in `WindowsProjectStore.cs` |
| A save holds its claim handle from creation until its own cleanup in `finally`. An overwrite takes the claim; a create-only save (Save As to a new name) takes **no** claim, only a temp. | `WindowsProjectStore.Save`: `if (!request.CreateOnly) { … claim = Owned.Create(…) }`, then `temp = Owned.Create(…)` |
| Owned cleanup removes an entry only through its retained handle, by POSIX delete disposition, after checking the name still denotes that handle. | `Owned.Cleanup`, `WindowsNative.DisposeEntry` (`DispositionDelete | DispositionPosix`) |
| macOS creates the claim and temps with `OwnedEntry.Create(parent.Fd, name, 0x180)` (mode 0600) and removes them with `IsOwned` then `unlinkat` by name. | `ProjectStore.cs:111, 120, 376-404` |
| Every ancestor in the Windows walk must be a plain directory: any reparse point is refused. | `Parent.RequireDirectory`, `WindowsProjectStore.cs` |
| The project file must carry no reparse attribute, one link, not delete-pending. | `OpenRegular` |
| `Read`, `Save` and `PublishUnderClaim` all call `RequireLocalFolder`, which refuses any folder `CloudFolder.Classify` does not call Local. | `WindowsProjectStore.cs` |
| `CloudFolder.Classify` exposes placeholders (`RtlSetThreadPlaceholderCompatibilityMode(2)`), then calls a folder Cloud when it is at or under `%OneDrive%`, `%OneDriveConsumer%` or `%OneDriveCommercial%`, or when it or an ancestor carries PINNED, UNPINNED, RECALL_ON_OPEN or RECALL_ON_DATA_ACCESS, the legacy OneDrive tag `0x80000021` or a tag in the cloud family `0x9000x01A`. | `CloudFolder.cs` |
| `UnfinishedSave.BlocksFolder` is true when the folder holds a file named `.cfd-writer.claim` (a name test only). | `UnfinishedSave.cs` |
| The in-place Save of a document whose `NativePath` folder classifies as Cloud already shows the S2 dialog (COPY-440, Ruling 146 (1) body) and "Choose another folder…" opens the picker. The picker starts at `%USERPROFILE%\CFD Workbench`. | `MainWindow.SaveWithPickerAsync`, `PickSavePathAsync`, `EnsureDefaultSaveFolder` |
| The "Save is blocked" dialog (S3) shows on a Windows save refused with DOC-CONFLICT while `BlocksFolder` is true; it has Save As and Cancel only. | `MainWindow.SaveToAsync`, `SaveBlockedDialog` |
| An Open refusal is classified by code; DOC-UNSUPPORTED-PERSISTENCE on Windows reads COPY-590 (Ruling 257); **DOC-CONFLICT falls through to NotRecognised**, whose sentence says the file "is damaged". | `OpenOutcome.ClassifyCode`; `StartView.Sentence` |
| The desktop app has one main window per process (no in-process window list). | search of `src/CfdWorkbench.Desktop` for `new MainWindow`, window lists: none |
| Crash injection runs a child harness process killed at a store stage. | `WindowsProjectStoreTests.CrashInjection` |

Measured inventory (W2 proof, quoted by the brief and DESIGN.md row COPY-447): a crash at Writing leaves the temp (2
bytes) and the claim (0 bytes); at BeforePublish the temp (6 bytes) and the claim; after Published only the claim. The
project file keeps its last published image.

## 2. Data model and invariants (settled first)

**Bounded context:** the project store (Persistence). No durable schema changes: nothing in this design is written into
the project document, the recent list or preferences. The new state is in-memory on the session and on disk only as the
absence of leftovers.

### 2.1 The objects

| Object | Kind | Identity | Lifetime |
|---|---|---|---|
| Project file | entity (the record) | its path and NTFS file id | durable; never touched by Clear or by a read-only Open |
| Claim `.cfd-writer.claim` | entity, a lock token | NTFS file id (Windows), dev+ino (macOS) | created by an overwrite save, removed by that save's cleanup; a leftover after a crash |
| Temp `.cfd-<guid>.tmp`, `.cfd-<guid>.bak.tmp` | entity, an unpublished image | file id | created per save; renamed onto the project (published) or removed; a leftover after a crash |
| **Leftover** | value: a claim or temp that no live handle holds | — | exists between a crash and a Clear |
| **Provenance** | value: the facts that say "this store created this entry" | — | read through the held handle, never from the name |
| **Document origin** | value object on the session: `Local` or `OneDriveReadOnly` | — | set at Open from the store's answer; changes to `Local` only after a successful Save As to a local folder; never persisted |
| **Cloud state** of a file | value: `FullyLocal` or `CloudOnly` | — | read at Open through the attribute handle, read again after the last byte |

### 2.2 Clear: what may be deleted (provenance)

The name pattern selects **candidates**; it is never provenance. A candidate is a directory entry of the project's folder
whose name is exactly `.cfd-writer.claim`, or matches `\A\.cfd-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(\.bak)?\.tmp\z`
(the lowercase D form `Guid.ToString("D")` produces, Verified in `TempName`). A candidate is a **provenance-matched
leftover** only when every test below holds, read through the handle Clear holds on it:

| # | Windows (handle opened relative to the retained parent) | macOS (fd opened with `openat`, `O_NOFOLLOW`) |
|---|---|---|
| P1 | regular file: no directory attribute, no reparse attribute | `S_ISREG` from `fstat` |
| P2 | one link, not delete-pending (`FileStandardInfo`) | `st_nlink == 1` |
| P3 | owner and DACL canonical-equal to `WindowsNative.PrivateDescriptor()` (the current user, protected, owner-only; the same `HasPrivateDescriptor` the save checks at creation) | `st_uid == geteuid()` and `(st_mode & 07777) == 0600` |
| P4 | claim: length 0 (the store never writes the claim). Temp: length at most `NativeProject.MaxBytes` | the same |
| P5 | the entry is on the project's folder, reached through the same retained, verified ancestor chain the save uses (`Parent.Open`, unchanged) | `ParentPath`, unchanged |

P3 is the provenance that carries weight: a file written by another tool, an unzip, a copy or a sync inherits the
folder's DACL and fails P3. **Rule: Clear is all-or-nothing.** If any candidate fails P1 to P5, Clear deletes nothing and
fails with COPY-455 and code DOC-CONFLICT; the entries stay for the user to inspect. A foreign `.cfd-writer.claim` is
therefore never removed. (A foreign `.cfd-` file in a project folder is unexplained; Clear does not choose among them.)

`assume:` a claim left by a crash of any build since W2 has the private descriptor. Confirmed by: the crash children in
`WindowsStore_Clear_RemovesCrashLeftovers_ClaimLast` (they run the shipped save code). Breaks if false: Clear fails with
COPY-455 on a genuine leftover and the user must delete by hand; it never deletes a wrong file.

### 2.3 Clear: the no-live-writer rule

**Definition.** A live writer is any open handle (Windows) or held lock (macOS) on a candidate. A save holds its claim and
its temp from creation to its own cleanup (Verified for Windows in `Save` and `PublishUnderClaim`), so "a live writer
holds the project" means "some candidate is held". A window that has the project open but is not saving holds nothing and
is not a writer; its next save runs every check of a normal save (the expected-disk hash, the claim create) and is safe.
Step 1 of the dialog ("Close any other CFD Workbench window using this project.") stays the user's instruction.

**Windows (run here).** Clear opens each candidate relative to the retained parent with `DELETE | READ_CONTROL |
FILE_READ_ATTRIBUTES | SYNCHRONIZE`, **share mode 0**, `FILE_OPEN_REPARSE_POINT`, `OBJ_DONT_REPARSE`. A live writer's
handle has write and DELETE access and does not share delete, so the open fails with `STATUS_SHARING_VIOLATION` (Win32
32): **live writer → refused, COPY-449, nothing deleted.** The kernel enforces it for writers in any process, any build
since W2, and other windows of this process alike. A crashed writer's handles were closed by the kernel at process exit,
so the open succeeds. Once Clear holds a candidate with share 0, no new handle with read-data, write or DELETE access can
open it, and a new overwrite save cannot create the claim (the name exists: its NT create fails, DOC-CONFLICT, Verified
`CodeOf` maps 80/183 to DOC-CONFLICT). **Race window: none for held candidates.** The one window is between the directory
enumeration and the exclusive opens, where a create-only save may add a new temp; the second enumeration (I-4) catches it.

`assume:` `NtCreateFile` with DELETE access and share 0 on a file another handle holds with write and DELETE access and
`FILE_SHARE_READ` fails with `STATUS_SHARING_VIOLATION`. This is the documented sharing rule; confirmed on this laptop by
`WindowsStore_Clear_LiveWriterClaimHeld_RefusedInUse`. Breaks if false: a live claim could be deleted; the test is red-first
and blocks the build.

**macOS (compiled, SKIP).** POSIX has no share modes, so the rule needs the writer's cooperation, added to the shared claim
code: `OwnedEntry.Create` takes `flock(fd, LOCK_EX | LOCK_NB)` on the claim and on each temp immediately after the
exclusive create and before the `ClaimCreated` / `TempCreated` stage, and holds it until its cleanup (the kernel drops it
when the process dies). Clear opens each candidate with `openat(parent, name, O_RDONLY | O_NOFOLLOW | O_CLOEXEC)` and
takes `flock(LOCK_EX | LOCK_NB)`; `EWOULDBLOCK` → live writer → COPY-449, nothing deleted. **Race window:** deletion is by
name (`unlinkat`), so between the second identity check (`fstatat` dev+ino equal to the held fd) and `unlinkat` a foreign
rename could swap the name; this is the residual the macOS `OwnedEntry.Cleanup` already carries (Verified, `IsOwned` then
`UnlinkAt`). **Mixed builds:** a writer from a build before this change takes no flock, so a Clear on macOS could remove
the claim of a live older-build save in the same folder. Recorded as DR-DS-4. The proof states the macOS path was not
assessed (Ruling 259).

### 2.4 Clear: the sequence, what is checked twice, what remains on failure

| Step | Action | On failure |
|---|---|---|
| C0 | Expose placeholders; `Parent.Open(projectPath)` (the Save walk, unchanged); `RequireLocalFolder` | refuse with the walk's code (DOC-UNSUPPORTED-PERSISTENCE); nothing held |
| C1 | **Check 1.** Enumerate the folder through the retained parent handle (`NtQueryDirectoryFile`); select candidates by name | DOC-IO |
| C2 | Open every candidate exclusively (2.3). A candidate gone since C1 is dropped (the writer finished) | sharing violation → **InUse**; release all; nothing deleted |
| C3 | Provenance P1 to P5 on every held handle | any fails → **Failed DOC-CONFLICT**; release all; nothing deleted |
| C4 | **Check 2.** `parent.Verify()` (every ancestor id unchanged); `SameEntry(parent, name, held)` for each; enumerate again: the candidate set must equal the held set | a new candidate → **InUse**; a mismatch → **Failed DOC-CONFLICT**; release all; nothing deleted |
| C5 | Dispose the temps: POSIX delete disposition on each held handle, close, confirm the name is absent | **Failed DOC-IO**; the claim is still present, so the folder is still blocked and Clear is offered again |
| C6 | Dispose the claim last, the same way; confirm absent | **Failed DOC-IO**; temps are gone, claim present: still blocked, Clear offered again |
| C7 | Emit `store.clear` (code, candidates, removed, ms); return **Cleared** (also when C1 found nothing: the leftovers were already gone) | — |

**Invariants.**

- **I-1** Clear deletes only provenance-matched leftovers it holds exclusively; the project file, `*.v1.bak`, any other
  file and any foreign `.cfd-` entry are never opened for delete.
- **I-2** A held (live) candidate is never deleted; one held candidate refuses the whole Clear.
- **I-3** Nothing is deleted before check 2 (C4) has passed for every candidate.
- **I-4** Check 2 repeats the parent identity, each name's identity and the enumeration; any difference refuses.
- **I-5** The claim is deleted last, so every partial state (a crash or failure in C5 or C6) still blocks the folder and
  still offers Clear; a retry converges.
- **I-6** A crash at any point before C5 leaves the folder exactly as it was: Clear's handles carry no persistent state, and
  the kernel closes them. A crash after a delete disposition is set completes that delete at handle close (POSIX
  semantics); I-5 keeps the remainder blocked.
- **I-7** Clear never runs without the S4 confirm click, never runs at startup or on a timer, and never writes a status or
  dialog sentence that says anything was recovered or restored (Rulings 146, 160).
- **I-8** Clear never touches the document in memory: it stays dirty, and its expected-disk hash is unchanged.

### 2.5 Read-only Open: what "fully local" means

A file is **cloud-only** when, read with placeholders exposed through the attribute handle (2.6), its attributes carry
any of `FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS` (0x400000), `FILE_ATTRIBUTE_RECALL_ON_OPEN` (0x40000) or
`FILE_ATTRIBUTE_OFFLINE` (0x1000). It is **fully local** otherwise. PINNED ("Always keep on this device") and UNPINNED are
admitted when no recall or offline bit is set: the bytes are on the disk now.

`assume:` a partly downloaded OneDrive file keeps RECALL_ON_DATA_ACCESS until it is fully hydrated, and a fully local
file carries none of the three bits (Inferred from the cloud-files attribute documentation). Confirmed by:
`WindowsStore_Open_RealOnlineOnlyFile_RefusedLeftCloudOnly` (recorded once, reads the real bits before and after) and the
recorded attributes of a real "Always keep on this device" file in the same proof. Breaks if false: a partly local file
could pass and its read could hydrate the rest; the post-read check (I-11) then refuses, and the recall would already
have started. The Security lens is asked to weigh this (Q-S2).

A **OneDrive folder** is a folder at or under the value of `%OneDrive%`, `%OneDriveConsumer%` or `%OneDriveCommercial%`
(the variables `CloudFolder` already reads). A cloud-tagged folder outside those roots (another sync provider) is not a
OneDrive folder and stays refused with DOC-UNSUPPORTED-PERSISTENCE (Ruling 259 admits OneDrive only).

### 2.6 Read-only Open: the walk, the attribute handle and the read

| Step | Action | Refusal |
|---|---|---|
| O0 | Expose placeholders for the whole synchronous `Read` (as today) | — |
| O1 | `Parent.Open(path, ParentMode.OpenRead)`. **The placeholder exception:** for an ancestor at or under a OneDrive root, `RequireDirectory` also admits a directory whose reparse tag is in the cloud family (`tag & 0xFFFF0FFF == 0x9000001A`) and that carries no other reparse. Every other tag stays refused: junction / mount point `0xA0000003`, symlink `0xA000000C`, the legacy `0x80000021`, any unknown tag. Ancestors above the OneDrive root are unchanged. `ParentMode.Save` (the default, used by `Save`, `PublishUnderClaim` and Clear) is the walk of today, byte for byte. | DOC-UNSUPPORTED-PERSISTENCE |
| O2 | `ClassifyForOpen(folder)`: Local → origin Local; at or under a OneDrive root → origin OneDriveReadOnly; any other cloud folder or a check that cannot run → refuse | DOC-UNSUPPORTED-PERSISTENCE |
| O3 | **Attribute handle A**: open the file relative to the parent with `FILE_READ_ATTRIBUTES | SYNCHRONIZE` only (no read-data access), share read and delete, `FILE_OPEN_REPARSE_POINT`, `FILE_OPEN_NO_RECALL`. Read attributes and tag on A. Cloud-only → refuse; **no data handle exists and no byte was read.** The leaf check: no directory; one link; not delete-pending; reparse tag none, or (origin OneDriveReadOnly only) the cloud family per DR-DS-1 | **DOC-CLOUD-ONLY** (new code, DR-DS-6); else DOC-UNSUPPORTED-PERSISTENCE |
| O4 | **Data handle D** from A: `ReOpenFile(A, FILE_GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, FILE_FLAG_OPEN_NO_RECALL | FILE_FLAG_OPEN_REPARSE_POINT)`. It reopens the same file object, so the file checked is the file read (DR-DS-2). Check `Identity(D) == Identity(A)`; read attributes on D: still fully local; record `ChangeTime` and length | DOC-CONFLICT on identity; DOC-CLOUD-ONLY on a recall bit |
| O5 | `ReadAll(D)` (bounded by MaxBytes, as today) | as today |
| O6 | **After the last byte**: attributes on A and D still fully local; identity, length and `ChangeTime` unchanged; `parent.Verify()`; `SameEntry` | a recall bit → DOC-CLOUD-ONLY; any other change → DOC-CONFLICT; **never a partial document** (the image is returned only after O6) |
| O7 | Return `ReadResult(image, sha, origin)`; emit `store.read` with the code and `store.read-origin` (Local or OneDriveReadOnly, no path) | — |

Local-folder Opens follow O3 to O6 too (the attribute handle replaces `OpenRegular` on the read path); for them the leaf
reparse rule is today's (no reparse at all), so their behaviour is unchanged.

`assume:` opening with `FILE_READ_ATTRIBUTES` only and `FILE_OPEN_REPARSE_POINT` does not make the cloud filter fetch
data (Inferred: no data access is requested). `FILE_FLAG_OPEN_NO_RECALL` is defence in depth whose effect on the cloud
filter is Inferred. Confirmed by: the real online-only recording (allocation size stays 0, RECALL_ON_DATA_ACCESS still
set after the refusal). Breaks if false: a refused file is downloaded; the recording is red and the build stops.

`assume:` a data handle that does not share write prevents OneDrive from dehydrating the file during the read (Inferred:
dehydration needs a write-capable open). Confirmed by: not testable without the real provider; the post-read check (O6)
refuses if it happened. Breaks if false: a mid-read dehydration makes the next read recall data; O6 still refuses the
Open, so the user never gets a partial document, but a download may have started. Raised to the Security lens (Q-S3).

### 2.7 Read-only Open: what the document can and cannot do

| Behaviour | Rule |
|---|---|
| Edit | allowed; dirty state as for any document |
| Save (in place, Ctrl+S, the toolbar, the unsaved-changes prompt's Save) | refused before any store call with the **OneDrive save refusal**: the S2 dialog (COPY-440, Ruling 146 (1) body). "Choose another folder…" opens the picker; Cancel keeps the document dirty. The store's `Save` would refuse too (`RequireLocalFolder`, unchanged): defence in depth |
| Save As | the picker opens at the local default folder `%USERPROFILE%\CFD Workbench` (already so, `PickSavePathAsync`); a OneDrive pick is refused by S2 as today. A successful Save As to a local folder makes the origin Local, clears the notice and the title suffix, and sets `NativePath` to the new file |
| Recent list | the path is recorded as for any Open; reopening it re-runs every check, so a file made cloud-only since is refused with DOC-CLOUD-ONLY and the alert's "Remove from recent" applies |
| How it is shown | the window title gains DS-COPY-1; the status strip shows DS-COPY-2 (info kind, polite live region) on open and keeps it while the origin is OneDriveReadOnly. **No modal on open.** |
| Writes on Open | none: no claim, no temp, no backup, no attribute change (I-9) |

- **I-9** A read-only Open writes nothing to the OneDrive folder.
- **I-10** A cloud-only file is refused before a data handle exists; no byte of its data is requested.
- **I-11** No `ReadResult` is returned unless the post-read check (O6) passes.
- **I-12** The placeholder exception is reachable only through `ParentMode.OpenRead`, which only `Read` passes; the save,
  publish-under-claim and Clear walks are unchanged.
- **I-13** A document with origin OneDriveReadOnly never calls `store.SaveAsync` with its `NativePath`.

## 3. E7 surface list

| Surface | Clear unfinished save (node A) | Read-only Open (node B) |
|---|---|---|
| Store (Windows) | `ClearUnfinishedSave(projectPath)` per 2.4; `WindowsNative.OpenExclusiveForDelete`, directory enumeration | `ParentMode.OpenRead`; attribute handle, `ReOpenFile`, the cloud-only check; `ReadResult.Origin` |
| Store (macOS) | flock in `OwnedEntry.Create`; `ClearUnfinishedSave` on `ParentPath`; compiled, SKIP | none (Classify returns Local off Windows; Verified) |
| Store API | `IProjectStore.ClearUnfinishedSaveAsync(path) → ClearResult(Outcome, Code, Removed)`; `ClearOutcome { Cleared, InUse, Failed }` | `ReadResult.Origin : DocumentOrigin { Local, OneDriveReadOnly }`; new code DOC-CLOUD-ONLY |
| Session / controller | `WorkbenchController.ClearUnfinishedSaveAsync(destination)` passes through; the document stays dirty | `WorkbenchController.Origin`, set at Open, reset to Local after a local Save As; exposed to the window |
| Desktop: S3 | gains "Clear unfinished save…" (COPY-444); step 3 and the stays-blocked sentence per Ruling 259 | — |
| Desktop: S4 | the confirm swaps in place (COPY-446, 447, 448, 445); InUse shows COPY-449 with Try again (COPY-450); Failed shows COPY-455; Cleared closes and retries the blocked save (DR-DS-3) | — |
| Desktop: Save | unchanged | for OneDriveReadOnly, the S2 refusal directly (skips the store); Save As unchanged |
| Desktop: Open | — | `OpenOutcome.ClassifyCode`: DOC-CLOUD-ONLY → new kind CloudOnly; `StartView.Sentence` → DS-COPY-3; the title suffix DS-COPY-1; the notice DS-COPY-2 |
| CLI | none (no `clear` command: YAGNI) | `inspect <file> --json` of a fully local OneDrive file succeeds through the same store read; a cloud-only file exits non-zero with DOC-CLOUD-ONLY on stderr; no new JSON field |
| Copy | COPY-444, 446 to 450, 455; S3 step 3 and DS-COPY-5 | DS-COPY-1 to 4 (DS-COPY-3, 4: seam to WF) |
| Telemetry | `store.clear` (code, candidates, removed, ms) | `store.read` code DOC-CLOUD-ONLY; `store.read-origin` |
| Proof | the Clear runs, the crash children, the macOS SKIP lines | the real online-only recording; the junction refusal; the read-only save writes nothing |

## 4. States (all in the mockup)

| # | State | Surface | What the user sees |
|---|---|---|---|
| A1 | Clear offered | S3 dialog | COPY-441 title, the Ruling 146 (2) lead, steps 1-3 (step 3 per Ruling 259), DS-COPY-5, buttons Save As… (default), Clear unfinished save…, Cancel |
| A2 | S4 confirm | the same dialog, content swapped | COPY-446, COPY-447, buttons Clear unfinished save and Cancel; first focus and Enter on Cancel; Escape and Cancel return to A1 |
| A3 | Clearing | S4 | both buttons disabled, `aria-busy`; no text (sub-second, Inferred; no spinner below 1 s) |
| A4 | Clear refused: in use | S4 | COPY-449 in the inline message (role alert); Try again (COPY-450) and Cancel; nothing removed |
| A5 | Clear failed | S4 | COPY-455 with the code (DOC-CONFLICT for a foreign entry; DOC-IO otherwise); Try again and Cancel |
| A6 | Cleared | the window | the dialog closes; the blocked save runs again: status COPY-452 "Saving…", then the ordinary saved status. **No sentence says anything was cleared or recovered.** |
| B1 | Read-only Open | the window | the project opens; title suffix DS-COPY-1; status DS-COPY-2 (info); no modal |
| B2 | Cloud-only refused | Start view alert | "“<file>” didn't open." + DS-COPY-3 + "Code: DOC-CLOUD-ONLY"; Open another / Remove from recent as for any failed open |
| B3 | Save on a read-only document | S2 dialog | COPY-440, Ruling 146 (1) body, Choose another folder…, Cancel; nothing written |
| B4 | Save As from it | the native picker | opens at `%USERPROFILE%\CFD Workbench`; after the save, the title suffix and notice are gone |
| B5 | Still refused: junction or other location | Start view alert | the Ruling 257 message without "OneDrive" (DS-COPY-4) |

## 5. Copy

Reserved and approved (Ruling 160; texts as in `docs/mockups/w2-save-picker.md` and DESIGN.md section 7):

| ID | Text | Where |
|---|---|---|
| COPY-441 | Save is blocked | S3 title (built) |
| COPY-443 | Save As… | S3 (built) |
| COPY-444 | Clear unfinished save… | S3 action that opens S4 |
| COPY-445 | Cancel | S3, S4 |
| COPY-446 | Clear the unfinished save? | S4 title |
| COPY-447 | This removes the files the unfinished save left in this folder (`.cfd-writer.claim` and any `.cfd-….tmp`). Your saved file and your current work are not touched. It works only if no other CFD Workbench window is using this project. | S4 body: the refined W2 text, HELD until now; shown by this build for the operator to confirm (DESIGN.md row 933) |
| COPY-448 | Clear unfinished save | S4 confirm |
| COPY-449 | Can't clear it yet: another CFD Workbench window is still using this project. Close that window, then try again. Your saved file and your current work are unchanged. | A4 |
| COPY-450 | Try again | A4, A5 |
| COPY-452 | Saving… | A6 |
| COPY-455 | Couldn't clear the unfinished save (<code>). Your saved file and your current work are unchanged. | A5 |
| COPY-440 | Can't save to OneDrive | B3 (built) |

Ruled by Ruling 259 (operator may revisit): S3 step 3 becomes "Use Save As to keep your current work in a different
folder, or choose Clear unfinished save, then Save."

Provisional, proposed for the operator (new copy is the operator's to approve):

| ID | Text | Where |
|---|---|---|
| DS-COPY-1 | (read-only: opened from OneDrive) | window title suffix after the project name |
| DS-COPY-2 | This project was opened from OneDrive, so it can't be saved there. Use Save As to save it in a local folder. | status strip, info kind, while the origin is OneDriveReadOnly |
| DS-COPY-3 | This file is in OneDrive but isn't on this PC yet. The file hasn't been changed and wasn't downloaded. In OneDrive, choose "Always keep on this device" for it, or copy it to a local folder, then open it again. | B2, Open refusal for DOC-CLOUD-ONLY (seam: WF) |
| DS-COPY-4 | CFD Workbench can't open a project from this location (a network or removable folder, or a linked folder). The file hasn't been changed. Copy the project to a local folder and open it from there. | B5: COPY-590 with "OneDrive" dropped (Ruling 259); seam: WF owns the row |
| DS-COPY-5 | This folder stays blocked until the unfinished save is cleared. | S3, replaces the W2 stays-blocked sentence (Ruling 259) |

## 6. Failure modes and adversarial analysis

| Mode | Disposition | Test |
|---|---|---|
| Clear while another process is mid-save | sharing violation → InUse, nothing deleted | `…LiveWriterClaimHeld_RefusedInUse`, `…LiveCreateOnlyTemp_RefusedInUse` |
| Clear on a foreign `.cfd-writer.claim` (copied, unzipped, another user) | P3 fails → Failed DOC-CONFLICT, kept | `…ForeignClaim_Kept_NothingRemoved` |
| A `.cfd-` name that is a hard link, symlink or junction | P1/P2 fail → kept | `…HardLinkOrReparseCandidate_Kept` |
| A writer starts between check 1 and the exclusive opens | its new temp shows at check 2 → InUse | `…NewEntryBetweenChecks_RefusedNothingRemoved` |
| The app crashes during Clear | I-5, I-6 | `…CrashDuringClear_ClaimLastAndRetryConverges` |
| An ancestor is swapped during Clear | `parent.Verify` → Failed DOC-CONFLICT | covered by the existing walk tests plus the check-2 test |
| Cloud-only file opened | refused before a data handle | `…CloudOnlyAttribute_RefusedBeforeAnyRead`, the real recording |
| File dehydrated or changed mid-read | O6 refuses; no partial document | `…RecalledOrChangedDuringRead_RefusedNoPartial` |
| Junction / symlink / mount point under OneDrive | refused (I-12, O1) | `…JunctionUnderOneDrive_Refused` |
| The exception leaks into the save walk | I-12; mutant test | `…SaveWalks_PlaceholderExceptionNotApplied` |

STRIDE-lite. **Tampering / elevation through the placeholder exception:** the exception admits one tag family, at or
under a OneDrive root the user's own environment names, on the Open path only, and Open writes nothing; a junction planted
in OneDrive stays refused. Residual: an attacker who controls the user's environment variables already controls the
user's process. **Denial of service by download:** the cloud-only check prevents a user-unrequested network fetch;
residuals are the two `assume:` lines in 2.6. **Repudiation / information:** telemetry carries codes and counts, never a
path or a file name (privacy; the existing events follow the same rule). **Tampering through Clear:** P3 plus all-or-
nothing means Clear can delete only files this user's store created with an owner-only DACL.

## 7. Red-first test plan

Each test names its failing input on today's code and a mutant it kills. Windows-run tests are in the Core harness
(`tests/CfdWorkbench.Core.Tests/WindowsProjectStoreTests.cs`), the Desktop harness, and the CLI harness; macOS-compiled
tests print `SKIP <name> macos-not-run-here` on Windows and are never reported PASS here.

### Node A: Clear unfinished save

| Test | Platform | Red today | Mutant it kills |
|---|---|---|---|
| `WindowsStore_Clear_RemovesCrashLeftovers_ClaimLast` | Windows-run | no Clear API (build red) | delete the claim but not the temp; delete without the provenance check (paired with the next) |
| `WindowsStore_Clear_ForeignClaim_Kept_NothingRemoved` (a `.cfd-writer.claim` made by `File.Create`, inherited DACL, plus a matched temp) | Windows-run | no API | provenance by name only → claim deleted |
| `WindowsStore_Clear_LiveWriterClaimHeld_RefusedInUse` (a save paused at `ClaimCreated` on another thread; then released: it completes OK) | Windows-run | no API | share mode `ShareAll` on the exclusive open → live claim deleted |
| `WindowsStore_Clear_LiveCreateOnlyTemp_RefusedInUse` (a create-only save paused at `TempCreated` next to a leftover claim) | Windows-run | no API | candidates limited to the claim → live temp's folder cleared under it |
| `WindowsStore_Clear_NewEntryBetweenChecks_RefusedNothingRemoved` (a hook at the new stage `ClearHeld` creates a private temp) | Windows-run | no API | check 2 without the second enumeration |
| `WindowsStore_Clear_CrashDuringClear_ClaimLastAndRetryConverges` (crash child killed at `ClearHeld` and at `ClearTempsDisposed`, the existing crash-child mechanism) | Windows-run | no API | claim disposed first → at `ClearTempsDisposed` the claim is gone while a temp remains |
| `WindowsStore_Clear_HardLinkOrReparseCandidate_Kept` | Windows-run | no API | P1/P2 dropped |
| `WindowsStore_Clear_UnderOneDrive_RefusedNothingRemoved` | Windows-run | no API | Clear walks with `ParentMode.OpenRead` |
| `WindowsStore_Clear_TelemetryEventCodeAndCounts` | Windows-run | no API | event omitted on refusal |
| `SaveBlockedDialog_ClearButton_SwapsToConfirm_FocusAndEnterOnCancel` | Windows-run (Desktop) | today's dialog has no Clear | default button on the confirm action |
| `SaveBlockedDialog_Step3AndStaysBlocked_Ruling259Text` | Windows-run (Desktop) | today's step 3 and sentence | text drift |
| `SaveBlockedDialog_ClearInUse_ShowsCopy449_TryAgain` | Windows-run (Desktop) | no Clear | InUse shown as success |
| `SaveBlockedDialog_ClearFailed_ShowsCopy455WithCode` | Windows-run (Desktop) | no Clear | code omitted |
| `MainWindow_ClearThenSave_NoRecoveryWording_DocumentSaved` (a real crash-left claim from the measured inventory; Clear; the retried save; every status and dialog string searched for "recover", "restor", "cleared") | Windows-run (Desktop) | no Clear | a success announcement |
| `MacStore_Clear_RemovesCrashLeftovers_ClaimLast` | macOS-compiled, SKIP | — | — |
| `MacStore_Clear_LiveWriterFlockHeld_RefusedInUse` | macOS-compiled, SKIP | — | — |
| `MacStore_Clear_ForeignOwnerOrMode_Kept` | macOS-compiled, SKIP | — | — |
| `MacStore_Save_ClaimAndTempFlockHeldUntilCleanup` | macOS-compiled, SKIP | — | — |

### Node B: read-only Open

| Test | Platform | Red today | Mutant it kills |
|---|---|---|---|
| `WindowsStore_Open_OneDriveFullyLocal_ReadOnlyOrigin_NothingWritten` (a fixture folder named as the OneDrive root by the environment; the folder listing with sizes and file ids before and after) | Windows-run | DOC-UNSUPPORTED-PERSISTENCE (Ruling 254) | a claim or temp taken on read |
| `WindowsStore_Open_CloudOnlyAttribute_RefusedBeforeAnyRead` (three cases: RECALL_ON_DATA_ACCESS, RECALL_ON_OPEN, OFFLINE through a `StoreHooks` attribute override; **no-read proof:** the new stage `DataHandleOpened` is never visited and a `ReadAll` call counter is 0) | Windows-run | refused with the wrong code, and no stage exists | read first, then check |
| `WindowsStore_Open_RealOnlineOnlyFile_RefusedLeftCloudOnly` (`CFD_ONLINE_ONLY_FILE` names a real online-only file on this laptop; before and after: RECALL_ON_DATA_ACCESS set and allocation size 0, read by directory query; prints `SKIP … no-online-only-file` when unset; **recorded once in the proof**) | Windows-run, recorded | DOC-UNSUPPORTED-PERSISTENCE | any data open before the check |
| `WindowsStore_Open_JunctionUnderOneDrive_Refused` (a real junction inside the fixture OneDrive root) | Windows-run | passes today; guards the exception | the exception admits any reparse tag |
| `WindowsStore_Open_ReparseTagsUnderOneDrive_OnlyCloudFamilyAdmitted` (synthetic tags `0xA0000003`, `0xA000000C`, `0x80000021`, an unknown tag refused; `0x9000101A` admitted; a cloud tag above the root refused) | Windows-run | refused (all) | mask or root bound loosened |
| `WindowsStore_Open_RecalledOrChangedDuringRead_RefusedNoPartial` (a hook at `Reading` sets a recall bit, then a second case changes length / `ChangeTime`) | Windows-run | stage absent | the post-read check dropped |
| `WindowsStore_SaveWalks_PlaceholderExceptionNotApplied` (Save, PublishUnderClaim and Clear under the fixture root with synthetic cloud tags: refused, nothing written) | Windows-run | passes today; guards I-12 | `ParentMode.OpenRead` passed by a save walk |
| `ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing` (open, edit, Save → S2; the folder listing unchanged; stays dirty) | Windows-run (Desktop) | open refused | the store's Save called with `NativePath` |
| `ControllerShell_OneDriveOpen_SaveAsAtLocalDefault_OriginBecomesLocal` | Windows-run (Desktop) | open refused | origin not reset; picker start not the default folder |
| `ControllerShell_OneDriveOpen_TitleAndNotice_NoModal` | Windows-run (Desktop) | open refused | a modal on open |
| `ControllerShell_CloudOnlyRefusal_SentenceAndCodeLine` (no "damaged") | Windows-run (Desktop) | the code is unknown → NotRecognised "damaged" | DOC-CLOUD-ONLY mapped to NotRecognised |
| `Cli_Inspect_OneDriveFullyLocal_SucceedsAndJunctionRefused` | Windows-run (CLI) | DOC-UNSUPPORTED-PERSISTENCE | — |

## 8. Owned paths for the build, by node, and seams

Both nodes edit `WindowsProjectStore.cs`, `WindowsNative.cs`, `ProjectStore.cs` and `MainWindow.axaml.cs`, so they run
**in sequence: node A first** (smaller; its review is the Data & Persistence veto), node B on A's result.

**Node A (Clear):** `src/CfdWorkbench.Persistence/UnfinishedSave.cs`, `WindowsProjectStore.cs`, `WindowsNative.cs`,
`ProjectStore.cs` (API, `ClearResult`, macOS Clear, flock in `OwnedEntry.Create`, `StoreStage` and `StoreHooks`
additions), `src/CfdWorkbench.Desktop/WorkbenchController.cs` (the pass-through only), `MainWindow.axaml.cs`
(`SaveBlockedDialog`, `SaveToAsync`), `tests/CfdWorkbench.Core.Tests/WindowsProjectStoreTests.cs`,
`tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` (the macOS SKIP checks), `tests/CfdWorkbench.Desktop.Tests/SaveBlockedDialogTests.cs`
(new), the Desktop harness registration, DESIGN.md section 7 rows COPY-444 to 455 status and DS-COPY-5, a proof folder
`docs/proof/win-store-recovery/`.

**Node B (read-only Open):** `src/CfdWorkbench.Persistence/WindowsProjectStore.cs`, `WindowsNative.cs`
(`ReOpenFile`, attribute-only open, `OPEN_NO_RECALL`), `CloudFolder.cs` (`ClassifyForOpen`, the root test), `ProjectStore.cs`
(`ReadResult.Origin`, `DocumentOrigin`, `StoreHooks` attribute override), `src/CfdWorkbench.Desktop/WorkbenchController.cs`
(`Origin`), `MainWindow.axaml.cs` (title, notice, the S2 short-cut), `OpenOutcome.cs` (CloudOnly), `Panes/StartView.axaml.cs`
(the sentence lookup), `src/CfdWorkbench.Analysis/Labels.cs` (DS-COPY-1, 2 and the provisional DS-COPY-3), the Core,
Desktop and CLI test files above, the proof folder.

**Seams.** The W2-Q2 message track **WF runs after this build and owns the open-refusal message rows**: it gives
DS-COPY-3 its final COPY id and wording and edits COPY-590 to DS-COPY-4. Node B lands DS-COPY-3 as a provisional row so
the refusal never shows the NotRecognised "damaged" sentence in between. Findings for WF (not in this scope): Open maps
DOC-CONFLICT to NotRecognised, whose sentence says "damaged" (Verified `OpenOutcome.ClassifyCode`); and the export folder
defaults to the folder of `NativePath` (Verified `WorkbenchController`), which for a read-only document is the OneDrive
folder: exports are not project saves and are out of this ruling's scope.

## 9. Review packet

**Security (Q-S; veto holds until reviewed).** Q-S1: is the placeholder exception narrow enough: cloud-family tags only,
at or under an environment-named OneDrive root, Open's walk only (I-12), and the leaf rule of DR-DS-1? Q-S2: is the
cloud-only test (three bits, read on an attribute-only handle with placeholders exposed) complete, and is the partly
downloaded case safe? Q-S3: the two `assume:` lines in 2.6 (no fetch on an attribute-only open; no dehydration under a
no-share-write data handle): accept with the recording, or require more? Q-S4: is `ReOpenFile` from the held attribute
handle the "same held handle" the ruling asks for (DR-DS-2)?

**Data & Persistence (Q-D; veto holds until reviewed).** Q-D1: is P3 (the private descriptor) sufficient provenance, and
is all-or-nothing the right response to a foreign entry? Q-D2: the Windows rule (share-0 DELETE open) and the macOS rule
(flock added to the shared claim code): does either leave a live claim deletable? Q-D3: the claim-last order and I-5/I-6:
is every partial state blocked and retryable? Q-D4: DR-DS-4 (older macOS writers without flock).

**UX & Accessibility (Q-U).** Q-U1: S3 with three buttons in the order Save As…, Clear unfinished save…, Cancel: is
Save As still the safe default? Q-U2: S4 first focus and Enter on Cancel, Escape returns to S3: keyboard and screen
reader order. Q-U3: A6 retries the save with no clear announcement (DR-DS-3): does the user understand what happened?
Q-U4: the read-only title suffix and persistent notice instead of a modal; the info kind's contrast in all three themes.

## 10. Open decisions (for the leader; not raised by the designer)

- **DR-DS-1 (Security): the leaf placeholder.** A fully local OneDrive file is itself a cloud placeholder with a reparse
  tag (Inferred), and `OpenRegular` refuses any reparse tag. (a) On the Open path only, for origin OneDriveReadOnly,
  admit a leaf whose only reparse is the cloud family and that passes the cloud-only test; measure the real leaf's tag
  first in the recording. (b) Admit only leaves with no reparse tag, which likely refuses every OneDrive file. **Recommend
  (a)**; the ruling's "ancestor walk only" read literally is (b).
- **DR-DS-2 (Security): one handle or two.** (a) An attribute-only handle checked first, the data read through
  `ReOpenFile` of that held handle (same file object). (b) One handle with read access, checked then read; it opens the
  data, which a RECALL_ON_OPEN file may fetch at open. **Recommend (a).**
- **DR-DS-3 (UX): after Clear.** (a) Close the dialog and retry the blocked save once (COPY-452, Ruling 160's S4a
  capture). (b) Close the dialog and wait for the user's Save. **Recommend (a)**; the Ruling 259 step 3 wording reads
  true for both.
- **DR-DS-4 (Data & Persistence): older macOS writers.** A macOS writer from a build without the flock holds no lock,
  so a macOS Clear could remove its live claim. (a) Accept and record it as not assessed with the macOS path (Ruling 259's
  pivot). (b) Also refuse a claim younger than a set age. **Recommend (a)**; (b) adds a time rule with no measured basis.
- **DR-DS-5 (copy, operator): other cloud providers.** A cloud-tagged folder outside OneDrive stays refused with the
  Ruling 257 message, which after DS-COPY-4 names no cloud. (a) Keep DS-COPY-4 as ruled. (b) Add "or a cloud-synced
  folder" to it. **Recommend (b)** for WF to carry; the operator approves the words.
- **DR-DS-6 (contract): the cloud-only code.** (a) A new stable code DOC-CLOUD-ONLY, so the row and the "Code:" line
  differ from the location refusal. (b) Reuse DOC-UNSUPPORTED-PERSISTENCE with a reason field. **Recommend (a)**: Ruling 259
  gives the cloud-only refusal its own row.

## 11. Telemetry

Questions an operator will ask, each with its source: how often Clear is refused or fails, and why (`store.clear` code);
how much it removed (`candidates`, `removed`); how long it took (`ms`); how often Open meets a cloud-only file
(`store.read` DOC-CLOUD-ONLY); how often a read-only OneDrive document is opened (`store.read-origin`). Every event
degrades to "not recorded"; none carries a path.
