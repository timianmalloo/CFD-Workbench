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
  Windows, a lock taken in the create's own openat on macOS, compiled and SKIP), claim last, checked twice, never silently
  and never announcing a recovery. Read-only Open admits a fully local file in a OneDrive folder by Ruling 260's predicate
  on Open's walk only, refuses a cloud-only file before any data handle exists, reads through a second handle opened from
  the held attribute handle with no name and no recall, and makes the document read-only for its location (in-place Save
  refused with the OneDrive refusal, Save As at the local default folder, a window notice). Cycle 1 folds the Security,
  Data & Persistence and UX conditions.
---

# Windows store recovery: Clear unfinished save and read-only Open from OneDrive

**Governing ruling:** Ruling 259 (R-OD: Q1 a, Q2 a) as amended by Ruling 260 (the leaf on Open), read with Rulings
135, 136, 146 (2), 160, 253 (W2-Q3 c), 254 and 257. **Cycle 1** folds the Adversary verdict (ACCEPT WITH CONDITIONS, no
veto): Security S1 to S5, Data & Persistence D1 to D4, UX U1 to U5 (section 9 maps each to its section). Facts cited as
*the reviewer's measurement* were measured by that review's read-only probes on the developer laptop.
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
whose name equals `.cfd-writer.claim` **compared case-insensitively** (ordinal ignore-case: the kernel's collision rule,
since `OpenRelative` passes `OBJ_CASE_INSENSITIVE` and the walk refuses case-sensitive folders, so a case variant blocks the
claim create exactly as the exact name does; D2), or matches `\A\.cfd-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(\.bak)?\.tmp\z`
(the lowercase D form `Guid.ToString("D")` produces, Verified in `TempName`; a case-variant temp blocks nothing and is
not a candidate). A case-variant claim is selected so that it is held, tested and, being foreign, refused (it fails P3
unless this store made it), never skipped. A candidate is a **provenance-matched
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
FILE_READ_ATTRIBUTES | SYNCHRONIZE`, **share mode `FILE_SHARE_READ`** (0x1, winnt.h), `FILE_OPEN_REPARSE_POINT`,
`OBJ_DONT_REPARSE`. A live writer's handle has write and DELETE access and shares read only, so the open fails with
`STATUS_SHARING_VIOLATION` (Win32 32) on two counts: Clear asks for DELETE, which the writer does not share, and Clear
does not share write, which the writer holds. **Live writer → refused, COPY-449, nothing deleted.** The kernel enforces it
for writers in any process, any build since W2, and other windows of this process alike. A crashed writer's handles were
closed by the kernel at process exit, so the open succeeds. Once Clear holds a candidate, no new handle with write or
DELETE access can open it, and a new overwrite save cannot create the claim (the name exists: its NT create fails,
DOC-CONFLICT, Verified `CodeOf` maps 80/183 to DOC-CONFLICT). **Race window: none for held candidates.** The one window is
between the directory enumeration and the exclusive opens, where a create-only save may add a new temp; the second
enumeration (I-4) catches it.

**Share mode: `FILE_SHARE_READ`, not 0 (the D&P advisory, taken).** Share 0 would also conflict with a reader that holds
a read handle on a temp (the search indexer, antivirus), and Clear would then show COPY-449, which names "another CFD
Workbench window" and would be false. Sharing read keeps both refusals that matter (DELETE not shared by any writer; write
not shared by Clear) and admits only readers, which cannot publish or delete. Residual: a reader that does not itself share
delete still yields COPY-449; Try again re-runs once it closes (Inferred, not measured; no indexer test is planned).

`assume:` `NtCreateFile` with DELETE access and share 0 on a file another handle holds with write and DELETE access and
`FILE_SHARE_READ` fails with `STATUS_SHARING_VIOLATION`. This is the documented sharing rule; confirmed on this laptop by
`WindowsStore_Clear_LiveWriterClaimHeld_RefusedInUse`. Breaks if false: a live claim could be deleted; the test is red-first
and blocks the build.

**macOS (compiled, SKIP).** POSIX has no share modes, so the rule needs the writer's cooperation, added to the shared claim
code: `OwnedEntry.Create` takes the lock **in the same `openat` that creates the entry** (D1): `openat(parent, name,
O_CREAT | O_EXCL | O_NOFOLLOW | O_CLOEXEC | O_EXLOCK | O_NONBLOCK | <the existing access flags>, 0600)`, so no instant
exists in which the entry is visible and unlocked. The lock is held until the entry's cleanup (the kernel drops it when
the process dies). A separate `flock` after the create is **not** used: the gap between them would let a Clear win the
lock and unlink a live claim. Clear opens each candidate with `openat(parent, name, O_RDONLY | O_NOFOLLOW | O_CLOEXEC |
O_EXLOCK | O_NONBLOCK)`; `EAGAIN` / `EWOULDBLOCK` → live writer → COPY-449, nothing deleted. Constants (Ruling 136 asks for
header name and value): `O_EXLOCK` 0x00000020 and `O_NONBLOCK` 0x00000004, `<sys/fcntl.h>`.
`assume:` those two values are Darwin's (recalled, not read from the SDK here: Windows laptop, no macOS SDK). Confirmed by:
the build reads `sys/fcntl.h` of the pinned macOS SDK on the Mac and cites the line in the constant's comment. Breaks if
false: the open fails with `EINVAL` or takes no lock; `MacStore_Save_ClaimAndTempLockedFromCreate` is red on the Mac.
**Race window:** deletion is by
name (`unlinkat`), so between the second identity check (`fstatat` dev+ino equal to the held fd) and `unlinkat` a foreign
rename could swap the name; this is the residual the macOS `OwnedEntry.Cleanup` already carries (Verified, `IsOwned` then
`UnlinkAt`). **Mixed builds (D4):** a writer from a build before this change takes no lock, so a Clear on macOS could
remove the claim of a live older-build save in the same folder. DR-DS-4 (a): the proof names this as an **unassessed
residual** under Ruling 259's "macOS path not assessed" statement, in those words; no time rule is added.

### 2.4 Clear: the sequence, what is checked twice, what remains on failure

| Step | Action | On failure |
|---|---|---|
| C0 | Expose placeholders; `Parent.Open(projectPath)` (the Save walk, unchanged); `RequireLocalFolder` | refuse with the walk's code (DOC-UNSUPPORTED-PERSISTENCE); nothing held |
| C1 | **Check 1.** Enumerate the folder through the retained parent handle (`NtQueryDirectoryFile`); select candidates by name (the claim case-insensitively, 2.2) | DOC-IO |
| C2 | Open every candidate exclusively (2.3). A candidate gone since C1 (not found) is dropped (the writer finished) | sharing violation → **InUse**; **any other open error** (access denied, delete pending, any status but not-found and the sharing violation) → **Failed** with the code `CodeOf` gives it (D3); in both cases release all; nothing deleted |
| C3 | Provenance P1 to P5 on every held handle | any fails → **Failed DOC-CONFLICT**; release all; nothing deleted |
| C4 | **Check 2.** `parent.Verify()` (every ancestor id unchanged); `SameEntry(parent, name, held)` for each; enumerate again: the candidate set must equal the held set | a new candidate → **InUse**; a mismatch → **Failed DOC-CONFLICT**; release all; nothing deleted |
| C5 | Dispose the temps: POSIX delete disposition on each held handle, close, confirm the name is absent | **Failed DOC-IO**; the claim is still present, so the folder is still blocked and Clear is offered again |
| C6 | Dispose the claim last, the same way; confirm absent | **Failed DOC-IO**; temps are gone, claim present: still blocked, Clear offered again |
| C7 | Look the claim name up once more through the retained parent (`OpenRelative`, `OBJ_CASE_INSENSITIVE`): return **Cleared** only when that lookup finds no entry (also when C1 found nothing: the leftovers were already gone); otherwise **Failed DOC-CONFLICT** (D2). Emit `store.clear` (code, candidates, removed, ms) | an entry still answers the claim name → Failed DOC-CONFLICT; the folder stays blocked and Clear is offered again |

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
`FILE_ATTRIBUTE_OFFLINE` (0x1000). It is **fully local** otherwise. PINNED and UNPINNED are admitted when no recall or
offline bit is set: the bytes are on the disk now (Ruling 260: the bits, not the tag, decide).

**Corrected fact (the reviewer's measurement, cycle 1).** On the developer laptop, with placeholders exposed: the OneDrive
root directory carries attributes 0x431 and reparse tag `0x9000701A`; **fully local user files carry tag `0x9000601A`**
with attributes 0x400 or 0x421 (0x180420 when pinned); only desktop.ini-type hidden system files carry no tag; cloud-only
files carry 0x401400 with tag `0x9000401A`. So every real fully local project file in OneDrive is a cloud-family
placeholder, and a "no tag only" leaf rule would refuse all of them. Ruling 260's predicate (2.6) admits them because
`0x9000601A & 0xFFFF0FFF == 0x9000001A` and none of the three bits is set; it refuses the cloud-only file because
0x401400 carries RECALL_ON_DATA_ACCESS and OFFLINE. The design's earlier Inferred line ("a fully local OneDrive file is a
placeholder with a reparse tag") is now measured, and Ruling 260's own text ("whether a hydrated user file keeps the tag
is not measured") is answered by this measurement for this laptop; the build's proof records the real leaf's tag and
attributes again (S3).

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

**The admission rule for the leaf on Open is Ruling 260's predicate, and only it.** Verbatim:

> The exact predicate the build implements: on the Open path, for a file whose folder is classified as at or under a
> OneDrive root (the OneDrive, OneDriveConsumer or OneDriveCommercial variables), with placeholders exposed, read through
> the attribute-only handle opened with FILE_OPEN_REPARSE_POINT and no data access. Admit the file if and only if all of
> the following hold: it is not a directory; it has one link; it is not delete-pending; its attributes carry none of
> RECALL_ON_DATA_ACCESS (0x400000), RECALL_ON_OPEN (0x40000) or OFFLINE (0x1000); and either it carries no reparse point,
> or its reparse tag masked with 0xFFFF0FFF equals 0x9000001A (the cloud-files family). If any of the three recall or
> offline bits is set, refuse with DOC-CLOUD-ONLY before any data handle exists and before any byte is read. Refuse any
> other reparse tag, and any shape check that cannot run, with DOC-UNSUPPORTED-PERSISTENCE. The same attribute test is
> repeated on the data handle before the read and on both handles after the last byte, and a recall bit seen at either
> point refuses with DOC-CLOUD-ONLY, never a partial document. What does not change: a file in a local folder keeps
> today's leaf rule (no reparse point at all); a cloud folder outside the OneDrive roots stays refused; Save,
> PublishUnderClaim, the in-place save and Clear keep the walk and the leaf rule of today, with no exception.

One clause is applied differently, on the leader's instruction folding Security condition S4: a recall bit seen **after
the data handle exists** (at O4 or O6) refuses with **DOC-CONFLICT**, not DOC-CLOUD-ONLY, because DOC-CLOUD-ONLY's
sentence (DS-COPY-3) says the file "wasn't downloaded", which is no longer known once a data handle was opened. The repeat
tests themselves are kept exactly; only the code differs. This departs from the ruling's text and is **pending Owner
notice (S4)** in section 10.

| Step | Action | Refusal |
|---|---|---|
| O0 | Expose placeholders for the whole synchronous `Read` (as today) | — |
| O1 | `Parent.Open(path, ParentMode.OpenRead)`. **The placeholder exception (ancestors):** it applies only when **both** hold (S2): the environment-named OneDrive root has **at least one path component below the drive root** (`C:\Users\<u>\OneDrive` qualifies; `C:\` or `D:\` does not), and the **root directory itself, reached by the handle walk, carries a cloud-family tag** (`tag & 0xFFFF0FFF == 0x9000001A`; the reviewer measured `0x9000701A` on the real root). When both hold, `RequireDirectory` also admits, for the root and each ancestor under it, a directory whose reparse tag is in the cloud family and that carries no other reparse. Otherwise **no exception applies** and the walk is today's (a tagged OneDrive folder is then refused). Every other tag stays refused: junction / mount point `0xA0000003`, symlink `0xA000000C`, the legacy `0x80000021`, any unknown tag. Ancestors above the root are unchanged. `ParentMode.Save` (the default, used by `Save`, `PublishUnderClaim` and Clear) is the walk of today, byte for byte. | DOC-UNSUPPORTED-PERSISTENCE |
| O2 | `ClassifyForOpen(folder)`: Local → origin Local; at or under a OneDrive root that met O1's two conditions → origin OneDriveReadOnly; any other cloud folder or a check that cannot run → refuse | DOC-UNSUPPORTED-PERSISTENCE |
| O3 | **Attribute handle A**: open the file relative to the parent with `FILE_READ_ATTRIBUTES | SYNCHRONIZE` only (no read-data access), share read and delete, `FILE_OPEN_REPARSE_POINT`, `FILE_OPEN_NO_RECALL`. Read attributes and tag on A. **Leaf admission (S3):** origin Local → today's rule (no reparse point at all). Origin OneDriveReadOnly → Ruling 260's predicate above: a leaf that carries a reparse point is admitted **only** with a cloud-family tag, **only** for origin OneDriveReadOnly, **only** with RECALL_ON_DATA_ACCESS, RECALL_ON_OPEN and OFFLINE all clear. A set bit → refuse; **no data handle exists and no byte was read.** This is the **only** point that refuses with DOC-CLOUD-ONLY (S4, DR-DS-6) | **DOC-CLOUD-ONLY** (a recall or offline bit, O3 only); any other tag or a shape check that cannot run → DOC-UNSUPPORTED-PERSISTENCE |
| O4 | **Data handle D** from A (S1): a native `NtCreateFile` with `RootDirectory = A` and an **empty name** (a `UNICODE_STRING` of length 0), so no name lookup happens. Access `GENERIC_READ` (0x80000000, winnt.h) `| SYNCHRONIZE` (0x00100000, winnt.h); share `FILE_SHARE_READ | FILE_SHARE_DELETE` (0x1 \| 0x4, winnt.h); disposition `FILE_OPEN` (0x1, wdm.h); options `FILE_NON_DIRECTORY_FILE` (0x40) `| FILE_SYNCHRONOUS_IO_NONALERT` (0x20) `| FILE_OPEN_REPARSE_POINT` (0x00200000) `| FILE_OPEN_NO_RECALL` (0x00400000), all wdm.h; object attributes `OBJ_CASE_INSENSITIVE` (0x40) `| OBJ_DONT_REPARSE` (0x1000), ntdef.h. It reuses `WindowsNative.OpenRelative` (Verified: already NtCreateFile relative to a retained handle with `CaseInsensitive | DontReparse`), given the empty name and one extra option bit. **Not `ReOpenFile`:** the reviewer measured `ReOpenFile` with `FILE_FLAG_OPEN_NO_RECALL` (0x00100000, winbase.h) failing with `ERROR_INVALID_PARAMETER` (87) on a local NTFS file and on a OneDrive file, for every variant of A's open flags, so as first designed every Open would have failed; the empty-name `NtCreateFile` above succeeded on a local file and on a fully local OneDrive file, gave the same file id as A, read the same bytes as a normal open and left the attributes unchanged (the reviewer's measurement). Then check `Identity(D) == Identity(A)`; read attributes on D: still fully local; record `ChangeTime` and length. A is held until O6 completes | DOC-CONFLICT on identity **and on a recall bit** (S4) |
| O5 | `ReadAll(D)` (bounded by MaxBytes, as today) | as today |
| O6 | **After the last byte**: attributes on A and D still fully local; identity, length and `ChangeTime` unchanged; `parent.Verify()`; `SameEntry` | a recall bit, a read error or any change → **DOC-CONFLICT** (S4), never DOC-CLOUD-ONLY, never DOC-IO from this check; **never a partial document** (the image is returned only after O6) |
| O7 | Return `ReadResult(image, sha, origin)`; emit `store.read` with the code and `store.read-origin` (Local or OneDriveReadOnly, no path) | — |

Local-folder Opens follow O3 to O6 too (the attribute handle and the empty-name data handle replace `OpenRegular` on the
read path); for them the leaf reparse rule is today's (no reparse at all), so their behaviour is unchanged. Because O4 now
runs on every Open, the S1 test below is red on the `ReOpenFile` path for a plain local file as well.

`assume:` opening with `FILE_READ_ATTRIBUTES` only and `FILE_OPEN_REPARSE_POINT` does not make the cloud filter fetch
data (Inferred: no data access is requested; the reviewer only enumerated cloud-only files and opened none). Confirmed by:
the real online-only recording (allocation size stays 0, RECALL_ON_DATA_ACCESS still set after the refusal). Breaks if
false: a refused file is downloaded; the recording is red and the build stops.

`assume:` an open read handle prevents OneDrive from dehydrating the file during the read (Inferred: unmeasured;
`ERROR_CLOUD_FILE_IN_USE` exists). Not load-bearing: `FILE_OPEN_NO_RECALL` on D keeps a mid-read dehydration from turning
the read into a download, and O6's check on A and D is the refusal. Confirmed by: not testable without the real provider;
the synthetic `…RecallBitAfterDataHandle_RefusedConflict` test drives O6. Breaks if false: O6 refuses with DOC-CONFLICT;
the user never gets a partial document.

### 2.7 Read-only Open: what the document can and cannot do

| Behaviour | Rule |
|---|---|
| Edit | allowed; dirty state as for any document |
| Save (in place, Ctrl+S, the toolbar, the unsaved-changes prompt's Save) | refused before any store call with the **OneDrive save refusal**: the S2 dialog (COPY-440, Ruling 146 (1) body). "Choose another folder…" opens the picker; Cancel keeps the document dirty. The store's `Save` would refuse too (`RequireLocalFolder`, unchanged): defence in depth |
| Save As | the picker opens at the local default folder `%USERPROFILE%\CFD Workbench` (already so, `PickSavePathAsync`); a OneDrive pick is refused by S2 as today. A successful Save As to a local folder makes the origin Local, clears the notice and the title suffix, and sets `NativePath` to the new file |
| Recent list | the path is recorded as for any Open; reopening it re-runs every check, so a file made cloud-only since is refused at O3 with DOC-CLOUD-ONLY and the alert's "Remove from recent" applies |
| How it is shown | the window title gains DS-COPY-1. DS-COPY-2 is a **separate persistent notice row** directly above the status strip (U3): info kind, its own element, shown from the Open until the origin becomes Local, announced once on open (`role="status"`, polite; its text never changes, so it is not re-announced). The status strip's message slot is untouched by it, so every other report (Saving…, the saved status, an analysis or export message) uses the slot and can never replace or hide DS-COPY-2. **No modal on open.** |
| Writes on Open | none: no claim, no temp, no backup, no attribute change (I-9) |

- **I-9** A read-only Open writes nothing to the OneDrive folder.
- **I-10** A cloud-only file is refused before a data handle exists; no byte of its data is requested.
- **I-11** No `ReadResult` is returned unless the post-read check (O6) passes.
- **I-12** The placeholder exception is reachable only through `ParentMode.OpenRead`, which only `Read` passes; the save,
  publish-under-claim and Clear walks are unchanged.
- **I-13** A document with origin OneDriveReadOnly never calls `store.SaveAsync` with its `NativePath`.
- **I-14** DOC-CLOUD-ONLY is produced only at O3, before D exists; after D exists every refusal is DOC-CONFLICT (S4).
- **I-15** D is opened from A with an empty name; no name lookup occurs between the attribute check and the read (S1).

## 3. E7 surface list

| Surface | Clear unfinished save (node A) | Read-only Open (node B) |
|---|---|---|
| Store (Windows) | `ClearUnfinishedSave(projectPath)` per 2.4; `WindowsNative.OpenExclusiveForDelete` (share read), directory enumeration, the case-insensitive claim lookup | `ParentMode.OpenRead` with the S2 root test; attribute handle A; the empty-name `OpenRelative` data handle D with `FILE_OPEN_NO_RECALL`; Ruling 260's leaf predicate; `ReadResult.Origin` |
| Store (macOS) | `O_EXLOCK \| O_NONBLOCK` in `OwnedEntry.Create`'s create `openat`; `ClearUnfinishedSave` on `ParentPath`; compiled, SKIP | none (Classify returns Local off Windows; Verified) |
| Store API | `IProjectStore.ClearUnfinishedSaveAsync(path) → ClearResult(Outcome, Code, Removed)`; `ClearOutcome { Cleared, InUse, Failed }` | `ReadResult.Origin : DocumentOrigin { Local, OneDriveReadOnly }`; new code DOC-CLOUD-ONLY |
| Session / controller | `WorkbenchController.ClearUnfinishedSaveAsync(destination)` passes through; the document stays dirty | `WorkbenchController.Origin`, set at Open, reset to Local after a local Save As; exposed to the window |
| Desktop: S3 | gains "Clear unfinished save…" (COPY-444); step 3 and the stays-blocked sentence per Ruling 259 | — |
| Desktop: S4 | the confirm swaps in place (COPY-446, 447, 448, 445); focus kept inside the dialog while busy (U1); InUse shows COPY-449 with Try again (COPY-450); Failed shows COPY-455; Escape or Cancel back to S3 focuses "Clear unfinished save…" (U2); Cleared closes and retries the blocked save **once** through `SaveToAsync` (U4, DR-DS-3) | — |
| Desktop: Save | unchanged | for OneDriveReadOnly, the S2 refusal directly (skips the store); Save As unchanged |
| Desktop: Open | — | `OpenOutcome.ClassifyCode`: DOC-CLOUD-ONLY → new kind CloudOnly (DR-DS-6); `StartView.Sentence` CloudOnly → DS-COPY-3; the title suffix DS-COPY-1; the notice row DS-COPY-2 (U3) |
| CLI | none (no `clear` command: YAGNI) | `inspect <file> --json` of a fully local OneDrive file succeeds through the same store read; a cloud-only file exits **3** with DOC-CLOUD-ONLY: `Program.ExitForCode` maps DOC-CLOUD-ONLY with DOC-UNSUPPORTED-PERSISTENCE to 3 (DR-DS-6; Verified today's switch would give the default 2); no new JSON field |
| Copy | COPY-444, 446 to 450, 455; S3 step 3 and DS-COPY-5 | DS-COPY-1 to 4 (DS-COPY-3, 4: seam to WF) |
| Telemetry | `store.clear` (code, candidates, removed, ms) | `store.read` code DOC-CLOUD-ONLY; `store.read-origin` |
| Proof | the Clear runs, the crash children, the macOS SKIP lines; the statement that the macOS path, including a live writer from an older build that takes no lock, is **not assessed** (D4) | the real online-only recording; the real fully local OneDrive leaf's tag and attributes recorded (S3; Ruling 260's run once the operator makes a file available offline, and until then the statement that the hydrated case is not measured on the build); the junction refusal; the read-only save writes nothing |

## 4. States (all in the mockup)

| # | State | Surface | What the user sees |
|---|---|---|---|
| A1 | Clear offered | S3 dialog | COPY-441 title, the Ruling 146 (2) lead, steps 1-3 (step 3 per Ruling 259), DS-COPY-5, buttons Save As… (default), Clear unfinished save…, Cancel |
| A2 | S4 confirm | the same dialog, content swapped | COPY-446, COPY-447, buttons Clear unfinished save and Cancel; first focus and Enter on Cancel; Escape and Cancel return to A1 **with focus on "Clear unfinished save…"**, the control that opened S4 (U2), not Save As |
| A3 | Clearing | S4 | both buttons disabled, `aria-busy`; no text (sub-second, Inferred; no spinner below 1 s). **Focus stays inside the dialog** (U1): it moves to the dialog's heading (focusable, `tabindex="-1"`), never to a disabled button and never out of the dialog; Tab cycles nowhere while busy; Escape is ignored while busy |
| A4 | Clear refused: in use | S4 | COPY-449 in the inline message (role alert); Try again (COPY-450) and Cancel; nothing removed; **focus moves to Cancel** when A4 renders (U1) |
| A5 | Clear failed | S4 | COPY-455 with the code (DOC-CONFLICT for a foreign entry or a claim still present; the code `CodeOf` gives for any other open error; DOC-IO for a dispose failure); Try again and Cancel; **focus moves to Cancel** (U1) |
| A6 | Cleared | the window | the dialog closes; the blocked save is retried **exactly once**, automatically, through `SaveToAsync` with the **same destination and the same expected-disk hash** (U4; Ruling 160's S4 outcome a, which the leader confirmed against the approved mockup): status COPY-452 "Saving…", then the ordinary save status. If that one retry is blocked again, its outcome is reported through the ordinary save status and **S3 is not reopened automatically**; S3 opens again only when the user saves again. **No sentence says anything was cleared or recovered.** |
| B1 | Read-only Open | the window | the project opens; title suffix DS-COPY-1; the persistent notice row DS-COPY-2 (info) above the status strip (U3); no modal |
| B2 | Cloud-only refused | Start view alert | "“<file>” didn't open." + DS-COPY-3 + "Code: DOC-CLOUD-ONLY"; Open another / Remove from recent as for any failed open. Only for the O3 refusal (U5, S4); a refusal after the data handle exists is DOC-CONFLICT and never shows DS-COPY-3 |
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
| DS-COPY-2 | This project was opened from OneDrive, so it can't be saved there. Use Save As to save it in a local folder. | the persistent notice row above the status strip, info kind, while the origin is OneDriveReadOnly (U3) |
| DS-COPY-3 | This file is in OneDrive but isn't on this PC yet. The file hasn't been changed and wasn't downloaded. In OneDrive, choose "Always keep on this device" for it, or copy it to a local folder, then open it again. | B2, **only** the O3 refusal (DOC-CLOUD-ONLY, no data handle ever opened, so "wasn't downloaded" is true; U5, S4). Proposed row: the operator approves the wording later (seam: WF). The OneDrive label "Always keep on this device" is **unconfirmed** (Inferred to be the current Explorer label; to be confirmed on the developer laptop before approval) |
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
| File dehydrated or changed mid-read | O6 refuses DOC-CONFLICT; no partial document | `…RecalledOrChangedDuringRead_RefusedNoPartial`, `…RecallBitAfterDataHandle_RefusedConflict` |
| The data handle opened by `ReOpenFile` with NO_RECALL | error 87 on every file (the reviewer's measurement); replaced by the empty-name `NtCreateFile` | `…DataHandleFromHeldHandle_NoRecall_LocalAndOneDrive` |
| OneDrive variable names a drive root, or an untagged root | no exception; today's walk refuses any tagged ancestor | `…UnqualifiedOneDriveRoot_NoException` |
| A Clear candidate's open fails (delete pending) or its delete fails (read-only, C5) | Failed with its code, nothing deleted | `…DeletePendingCandidate_C2OpenError_FailedNothingRemoved`, `…ReadOnlyCandidate_FailsAtC5Delete_DocIo_NothingRemoved` |
| A case-variant `.cfd-writer.claim` | selected, held, refused by P3 (or removed when provenance-matched); Cleared only if no claim name answers afterwards | `…CaseVariantClaim_NotReportedCleared` |
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
| **D2** `WindowsStore_Clear_CaseVariantClaim_NotReportedCleared` (a leftover named `.CFD-Writer.Claim`, made once with the private descriptor and once by `File.Create`) | Windows-run | no API | exact-name (ordinal) selection → "Cleared, 0 removed" while the folder stays blocked; C7 returning Cleared without the final lookup |
| **D3** `WindowsStore_Clear_DeletePendingCandidate_C2OpenError_FailedNothingRemoved` (a candidate delete-pending through a held handle with delete disposition set; a deny-DELETE ACE does not fail the open where the folder grants FILE_DELETE_CHILD, as in an ordinary user folder, so it is not a C2 case; a read-only candidate opens and fails at C5, `…ReadOnlyCandidate_FailsAtC5Delete_DocIo_NothingRemoved`) | Windows-run | no API | every non-sharing error mapped to InUse, or to dropped-as-gone, so the rest is deleted |
| **D-advisory** `WindowsStore_Clear_ReaderOnTemp_NotInUse` (a plain `FileShare.ReadWrite \| FileShare.Delete` read handle on a leftover temp; Clear succeeds) | Windows-run | no API | share mode 0 on the exclusive open → false COPY-449 |
| **U1** `SaveBlockedDialog_Clearing_FocusInsideDialog_ThenCancelOnRefusal` (while busy: the focused element is the dialog's heading, inside the dialog, not a disabled button; after InUse and after Failed: focus is Cancel) | Windows-run (Desktop) | no Clear | focus left on the disabled Cancel, or on the window behind |
| **U2** `SaveBlockedDialog_EscapeOrCancelFromConfirm_FocusReturnsToClear` (Escape and Cancel from A2, A4 and A5) | Windows-run (Desktop) | no Clear | focus returned to Save As (the S3 default) |
| **U4** `MainWindow_Cleared_RetriesSaveExactlyOnce_SameDestinationAndHash` (a `StoreHooks` counter on `Save`; the leftover is re-created by a hook before the retry, so the retry is blocked; asserts one store `Save` call after Clear, the same destination and expected-disk hash as the blocked save, S3 not open, the ordinary save status shown; then a user Save opens S3) | Windows-run (Desktop) | no Clear | a retry loop (two or more calls), a retry with a recomputed hash, or S3 reopened automatically |
| `MacStore_Clear_RemovesCrashLeftovers_ClaimLast` | macOS-compiled, SKIP | — | — |
| `MacStore_Clear_LiveWriterLockHeld_RefusedInUse` | macOS-compiled, SKIP | — | — |
| `MacStore_Clear_ForeignOwnerOrMode_Kept` | macOS-compiled, SKIP | — | — |
| **D1** `MacStore_Save_ClaimAndTempLockedFromCreate` (an `O_EXLOCK \| O_NONBLOCK` open by a second fd at the `ClaimCreated` and `TempCreated` stages fails with `EAGAIN`) | macOS-compiled, SKIP | — | a separate `flock` after the create (the mutant on the Mac: a hook between create and lock wins the lock) |

### Node B: read-only Open

| Test | Platform | Red today | Mutant it kills |
|---|---|---|---|
| `WindowsStore_Open_OneDriveFullyLocal_ReadOnlyOrigin_NothingWritten` (a fixture folder named as the OneDrive root by the environment; the folder listing with sizes and file ids before and after) | Windows-run | DOC-UNSUPPORTED-PERSISTENCE (Ruling 254) | a claim or temp taken on read |
| `WindowsStore_Open_CloudOnlyAttribute_RefusedBeforeAnyRead` (three cases: RECALL_ON_DATA_ACCESS, RECALL_ON_OPEN, OFFLINE through a `StoreHooks` attribute override; **no-read proof:** the new stage `DataHandleOpened` is never visited and a `ReadAll` call counter is 0) | Windows-run | refused with the wrong code, and no stage exists | read first, then check |
| `WindowsStore_Open_RealOnlineOnlyFile_RefusedLeftCloudOnly` (`CFD_ONLINE_ONLY_FILE` names a real online-only file on this laptop; before and after: RECALL_ON_DATA_ACCESS set and allocation size 0, read by directory query; prints `SKIP … no-online-only-file` when unset; **recorded once in the proof**) | Windows-run, recorded | DOC-UNSUPPORTED-PERSISTENCE | any data open before the check |
| `WindowsStore_Open_JunctionUnderOneDrive_Refused` (a real junction inside the fixture OneDrive root) | Windows-run | passes today; guards the exception | the exception admits any reparse tag |
| `WindowsStore_Open_ReparseTagsUnderOneDrive_OnlyCloudFamilyAdmitted` (synthetic tags `0xA0000003`, `0xA000000C`, `0x80000021`, an unknown tag refused; `0x9000101A` admitted; a cloud tag above the root refused) | Windows-run | refused (all) | mask or root bound loosened |
| **R260** `WindowsStore_Open_Ruling260LeafPredicate_Rows` (synthetic leaf through the `StoreHooks` attribute override, origin OneDriveReadOnly: no tag and local → admitted; tag `0x9000601A` (the measured fully local tag) and local → admitted; RECALL_ON_DATA_ACCESS alone, RECALL_ON_OPEN alone, OFFLINE alone → DOC-CLOUD-ONLY with `DataHandleOpened` never visited; tag `0xA0000003`, `0xA000000C`, an unknown tag → DOC-UNSUPPORTED-PERSISTENCE; the same `0x9000601A` leaf in a Local folder → DOC-UNSUPPORTED-PERSISTENCE) | Windows-run | DOC-UNSUPPORTED-PERSISTENCE on every OneDrive row | a "no tag only" leaf rule (refuses the `0x9000601A` row); the tag test before the bit test (a cloud-only `0x9000401A` leaf admitted) |
| **S1** `WindowsStore_Open_DataHandleFromHeldHandle_NoRecall_LocalAndOneDrive` (a plain local file and a fixture OneDrive file: D opens, `Identity(D) == Identity(A)`, the bytes equal the file; a `StoreHooks` rename of the name to another file between O3 and O4 does not change what is read, so no name lookup happened) | Windows-run | stage absent; today's read opens by name | the `ReOpenFile(A, …, FILE_FLAG_OPEN_NO_RECALL)` path: it fails with error 87 and every Open fails (the reviewer's measurement); a re-open by name (the rename case reads the other file) |
| **S2** `WindowsStore_Open_UnqualifiedOneDriveRoot_NoException` (the root qualification on the drive-root forms `C:\`, `D:\` and `\\?\C:\` → no exception, checked on the function that qualifies the root, so no system drive mapping is made; a second case names the fixture root while the `StoreHooks` attribute override gives the root directory no tag and a subfolder `0x9000601A` → refused) | Windows-run | refused (no exception exists) | the root bound by the environment string only |
| **S3** `WindowsStore_Open_LeafCloudTagOnlyForOneDriveOrigin` (folded into the R260 row's Local-folder case and a cloud-tagged leaf under a non-OneDrive cloud folder → DOC-UNSUPPORTED-PERSISTENCE) and the **proof** line recording the real fully local leaf's tag and attributes | Windows-run, recorded | refused | the leaf exception applied without the OneDrive origin |
| **S4** `WindowsStore_Open_RecallBitAfterDataHandle_RefusedConflict` (a hook at `DataHandleOpened` sets RECALL_ON_DATA_ACCESS on D's attributes → DOC-CONFLICT; a hook at `Reading` → DOC-CONFLICT; neither returns an image; the code is never DOC-CLOUD-ONLY or DOC-IO) | Windows-run | stage absent | the post-handle recall mapped to DOC-CLOUD-ONLY (Ruling 260's literal sentence) |
| `WindowsStore_Open_RecalledOrChangedDuringRead_RefusedNoPartial` (a hook at `Reading` sets a recall bit, then a second case changes length / `ChangeTime`; both DOC-CONFLICT) | Windows-run | stage absent | the post-read check dropped |
| `WindowsStore_SaveWalks_PlaceholderExceptionNotApplied` (Save, PublishUnderClaim and Clear under the fixture root with synthetic cloud tags: refused, nothing written) | Windows-run | passes today; guards I-12 | `ParentMode.OpenRead` passed by a save walk |
| `ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing` (open, edit, Save → S2; the folder listing unchanged; stays dirty) | Windows-run (Desktop) | open refused | the store's Save called with `NativePath` |
| `ControllerShell_OneDriveOpen_SaveAsAtLocalDefault_OriginBecomesLocal` | Windows-run (Desktop) | open refused | origin not reset; picker start not the default folder |
| `ControllerShell_OneDriveOpen_TitleAndNotice_NoModal` | Windows-run (Desktop) | open refused | a modal on open |
| **U3** `ControllerShell_OneDriveOpen_NoticeStaysAcrossStatusReports` (after the open, three ordinary reports through the status slot (Saving…, a save refusal, an analysis message): the notice row still shows DS-COPY-2 each time and is a separate element from the slot; after a local Save As the row is gone) | Windows-run (Desktop) | open refused | DS-COPY-2 written to the shared message slot (the next report replaces it) |
| `ControllerShell_CloudOnlyRefusal_SentenceAndCodeLine` (no "damaged") | Windows-run (Desktop) | the code is unknown → NotRecognised "damaged" | DOC-CLOUD-ONLY mapped to NotRecognised |
| **DR-DS-6** `OpenOutcome_ClassifyCode_CloudOnly_And_StartViewSentence` (`ClassifyCode("DOC-CLOUD-ONLY")` → CloudOnly; `StartView.Sentence(CloudOnly)` → DS-COPY-3; DOC-CONFLICT never yields DS-COPY-3 (U5)) | Windows-run (Desktop) | NotRecognised | the code unmapped, or DS-COPY-3 reused for another kind |
| **DR-DS-6** `Cli_ExitForCode_CloudOnly_Is3` (`ExitForCode("DOC-CLOUD-ONLY") == 3`, equal to DOC-UNSUPPORTED-PERSISTENCE) | CLI | the default 2 (Verified, `Program.ExitForCode`) | the switch arm omitted |
| `Cli_Inspect_OneDriveFullyLocal_SucceedsAndJunctionRefused` | Windows-run (CLI) | DOC-UNSUPPORTED-PERSISTENCE | — |

## 8. Owned paths for the build, by node, and seams

Both nodes edit `WindowsProjectStore.cs`, `WindowsNative.cs`, `ProjectStore.cs` and `MainWindow.axaml.cs`, so they run
**in sequence: node A first** (smaller; its review is the Data & Persistence veto), node B on A's result.

**Node A (Clear):** `src/CfdWorkbench.Persistence/UnfinishedSave.cs`, `WindowsProjectStore.cs`, `WindowsNative.cs`,
`ProjectStore.cs` (API, `ClearResult`, macOS Clear, `O_EXLOCK | O_NONBLOCK` in `OwnedEntry.Create`'s create `openat`, `StoreStage` and `StoreHooks`
additions), `src/CfdWorkbench.Desktop/WorkbenchController.cs` (the pass-through only), `MainWindow.axaml.cs`
(`SaveBlockedDialog`, `SaveToAsync`), `tests/CfdWorkbench.Core.Tests/WindowsProjectStoreTests.cs`,
`tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` (the macOS SKIP checks), `tests/CfdWorkbench.Desktop.Tests/SaveBlockedDialogTests.cs`
(new), the Desktop harness registration, DESIGN.md section 7 rows COPY-444 to 455 status and DS-COPY-5, a proof folder
`docs/proof/win-store-recovery/`.

**Node B (read-only Open):** `src/CfdWorkbench.Persistence/WindowsProjectStore.cs`, `WindowsNative.cs`
(attribute-only open; `OpenRelative` given an empty name and the `FILE_OPEN_NO_RECALL` option for the data handle; no
`ReOpenFile`), `CloudFolder.cs` (`ClassifyForOpen`, the S2 root qualification), `ProjectStore.cs` (`ReadResult.Origin`,
`DocumentOrigin`, the `DataHandleOpened` stage, `StoreHooks` attribute override), `src/CfdWorkbench.Desktop/WorkbenchController.cs`
(`Origin`), `MainWindow.axaml.cs` and `MainWindow.axaml` (title, the notice row, the S2 short-cut), `OpenOutcome.cs`
(CloudOnly), `Panes/StartView.axaml.cs` (the sentence lookup), **`src/CfdWorkbench.Cli/Program.cs` (`ExitForCode`:
DOC-CLOUD-ONLY → 3; DR-DS-6)**, `src/CfdWorkbench.Analysis/Labels.cs` (DS-COPY-1, 2 and the provisional DS-COPY-3),
`tests/CfdWorkbench.Core.Tests/WindowsProjectStoreTests.cs`, `tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs`,
**`tests/CfdWorkbench.Cli.Tests/CliTests.cs`**, the proof folder.

**Moved in cycle 1:** `ReOpenFile` leaves node B (replaced by the `OpenRelative` extension, S1); `src/CfdWorkbench.Cli/Program.cs`
and `tests/CfdWorkbench.Cli.Tests/CliTests.cs` join node B (DR-DS-6); `MainWindow.axaml` joins node B for the notice row
(U3). Node A's list gains no file (D2, D3, the share-read choice, U1, U2 and U4 land in files it already owns). The order
is unchanged: node A first, node B on A's result.

**Seams.** The W2-Q2 message track **WF runs after this build and owns the open-refusal message rows**: it gives
DS-COPY-3 its final COPY id and wording and edits COPY-590 to DS-COPY-4. Node B lands DS-COPY-3 as a provisional row so
the refusal never shows the NotRecognised "damaged" sentence in between. Findings for WF (not in this scope): Open maps
DOC-CONFLICT to NotRecognised, whose sentence says "damaged" (Verified `OpenOutcome.ClassifyCode`); and the export folder
defaults to the folder of `NativePath` (Verified `WorkbenchController`), which for a read-only document is the OneDrive
folder: exports are not project saves and are out of this ruling's scope.

## 9. Review packet

### What changed in cycle 1

The Adversary verdict (Security, Data & Persistence, UX & Accessibility) returned ACCEPT WITH CONDITIONS, no veto. Each
condition is folded at the section named.

| Condition | Change | Section |
|---|---|---|
| Corrected fact 1 | `ReOpenFile` with `FILE_FLAG_OPEN_NO_RECALL` fails with error 87 (the reviewer's measurement) | 2.6 O4 |
| Corrected fact 2 | fully local user files carry `0x9000601A`; the root `0x9000701A`; cloud-only `0x9000401A` with 0x401400 | 2.5 |
| R260 | Ruling 260's predicate verbatim; the only leaf admission rule on Open; DR-DS-1 removed as settled | 2.6, 10 |
| S1 | data handle D: empty-name `NtCreateFile` relative to A with `FILE_OPEN_REPARSE_POINT`, `FILE_OPEN_NO_RECALL`, `OBJ_DONT_REPARSE`; constants cited (Ruling 136) | 2.6 O4, I-15, 7 (S1), 8 |
| S2 | exception only when the root is below the drive root and itself cloud-tagged | 2.6 O1, O2, 7 (S2) |
| S3 | a tagged leaf only cloud family, only OneDrive origin, all three bits clear; the proof records the real leaf tag | 2.5, 2.6 O3, 7 (S3) |
| S4 | DOC-CLOUD-ONLY and "wasn't downloaded" only at O3; after D exists → DOC-CONFLICT | 2.6 O4, O6, I-14, 4 B2, 5, 7 (S4), 10 |
| S5 | the "same held handle" reading, pending Owner notice | 10 |
| D1 | macOS lock in the create `openat` (`O_EXLOCK \| O_NONBLOCK`), no separate `flock` | 2.3, 3, 7 (D1), 8 |
| D2 | claim selected case-insensitively; Cleared only when the claim name is absent afterwards | 2.2, 2.4 C1, C7, 7 (D2) |
| D3 | any open error but the sharing violation (and not-found) → Failed with its code, nothing deleted | 2.4 C2, 7 (D3) |
| D4 | older macOS builds named as an unassessed residual; share-read chosen over share 0 | 2.3, 7 (D-advisory), 10 |
| U1 | focus inside the dialog while busy; Cancel on A4 and A5 | 4 A3 to A5, 7 (U1), mockup |
| U2 | Escape or Cancel back to S3 focuses "Clear unfinished save…" | 4 A2, 7 (U2), mockup |
| U3 | DS-COPY-2 is a separate persistent notice row; the status slot cannot hide it | 2.7, 4 B1, 5, 7 (U3), mockup |
| U4 | one automatic retry through `SaveToAsync`, same destination and hash; a second block reopens S3 only by user action | 4 A6, 7 (U4), 10 |
| U5 | DS-COPY-3 only for O3; wording a proposed row; "Always keep on this device" unconfirmed | 4 B2, 5, mockup note |
| DR-DS-6 | DOC-CLOUD-ONLY mapped in `OpenOutcome`, `StartView` and `ExitForCode` (3); paths and tests added to node B | 3, 7 (DR-DS-6), 8 |

The Q-lists below are the cycle-0 review questions, kept as asked.

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

## 10. Decisions: status after cycle 1

| DR | Status | Settled by | What it means for the build |
|---|---|---|---|
| DR-DS-1 (leaf placeholder) | **settled**: (a); removed from the open list | Ruling 260 | the leaf on Open is admitted by Ruling 260's predicate only (2.6) |
| DR-DS-2 (one handle or two) | **settled**: (a), with the S1 mechanism | the leader | attribute handle A, then D by an empty-name `NtCreateFile` relative to A (2.6 O4); see the S5 notice below |
| DR-DS-3 (after Clear) | **settled**: (a), under U4 | the leader | one automatic retry through `SaveToAsync`, same destination and hash; no automatic S3 reopen (4 A6) |
| DR-DS-4 (older macOS writers) | **settled**: (a), under D1 and D4 | the leader | the lock is in the create `openat`; the proof names older builds as an unassessed residual (2.3) |
| DR-DS-5 (other cloud providers) | **(b), carried by track WF** | the leader routes it to WF | "or a cloud-synced folder" in DS-COPY-4; WF owns the row and the operator approves the words; not built here |
| DR-DS-6 (the cloud-only code) | **settled**: (a) | the leader | DOC-CLOUD-ONLY, produced at O3 only, mapped in `OpenOutcome.ClassifyCode`, `StartView.Sentence` and `Program.ExitForCode` (3) |

**Pending Owner notices (the leader sends them; the build does not wait on them unless the Owner answers otherwise).**

- **S5, pending Owner notice (S5).** Ruling 259 condition 2 says "the read then uses that same held handle". The design
  reads from D, a second handle opened from the held attribute handle A with an empty name: no name lookup, A held until O6
  completes, and `Identity(D) == Identity(A)` checked. The Security lens asks the Owner to record that this meets "the same
  held handle". `ReOpenFile`, the literal "reopen the same handle", cannot carry `FILE_FLAG_OPEN_NO_RECALL` here (error 87,
  the reviewer's measurement).
- **S4, pending Owner notice (S4).** Ruling 260 says "a recall bit seen at either point [on the data handle before the read,
  or on both handles after the last byte] refuses with DOC-CLOUD-ONLY". Under S4 and DR-DS-6 as the leader settled them,
  the design refuses those two points with **DOC-CONFLICT**, because DOC-CLOUD-ONLY's sentence says the file "wasn't
  downloaded", which is not known once a data handle exists. The tests the ruling asks for are all kept; only the code at
  O4 and O6 differs. The Owner is asked to record this reading, or to rule that the literal text stands (then O4 and O6
  map a recall bit to DOC-CLOUD-ONLY with a sentence that does not claim no download, and the S4 test's mutant flips).

## 11. Telemetry

Questions an operator will ask, each with its source: how often Clear is refused or fails, and why (`store.clear` code);
how much it removed (`candidates`, `removed`); how long it took (`ms`); how often Open meets a cloud-only file
(`store.read` DOC-CLOUD-ONLY); how often a read-only OneDrive document is opened (`store.read-origin`). Every event
degrades to "not recorded"; none carries a path.

**Known reuse (follow-up, no schema change now).** `store.clear` puts the candidate count in `InputBytes` and the removed count
in `OutputBytes` (counts, not bytes; for this action only), and `RecordPersistence` logs it under the `document.save`
operation, so a reader that counts saves by `Operation` also counts Clears; `Action` stays `store.clear`. The follow-up gives
Clear its own `Operation` value and its own count fields (the `PointsBefore`/`PointsAfter` pattern).
