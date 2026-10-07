---
id: proof-win-store-design-candidate
title: "B1 Windows native project-store design candidate"
type: proof-pack
status: in-review
owner: "@win-store-design-20261007"
phase: windows-w-2-design
tags: [windows, persistence, design, security, durability]
links:
  - { to: coordination-pc-kickoff, rel: implements }
  - { to: coordination-two-machine, rel: depends-on }
  - { to: coordination-windows-w0-w5-execution, rel: relates-to }
  - { to: design-windows-runtime, rel: refines }
  - { to: adr-application-project-contract, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: 2026-11-07
summary: >-
  Reviewable B1 blueprint for a separate Windows native store helper, handle-relative
  publication, identity and security controls. Native qualification, final-directory
  durability and independent Mac/Fable, Data, Security and Test approval remain open.
---

# B1 Windows native project-store design candidate

This is the complete proposed content for `docs/design/windows-native-store.md`.
It is retained in the PC-owned proof subtree pending affirmative Mac authorization
for that design path. It does not authorize B2 or alter an accepted contract.

**Goal:** make Windows Save/Open implementable without weakening native-project
integrity, macOS behavior or truthful save outcomes. **Done when for this B1 author
checkpoint:** source-cited design, exact prospective paths and falsifiable approval
request are committed; independent approval and production proof are separate.
**Not in scope:** implementation, new project format, UI redesign, general filesystem
support, privilege changes, push, PR, audit/register/derived-file writes. **Tier:** T1.
**Fan-out cap:** one author; independent reviewers are assigned by the coordinator.

Confidence labels: **Verified** means directly observed source or command state;
**Sourced** means a Microsoft contract was read, not executed; **Inferred** means a
proposed consequence awaiting execution; **Flagged** means a blocking gap. All native
behavior in this candidate is Sourced/Inferred, never Verified Windows qualification.

## 1. Authority, responsibility and current state

The store publishes and reads an immutable byte image in a selected directory. It
does not accept geometry, invent project history, serialize JSON or acknowledge a
session save. Those are existing Core/controller responsibilities.

Verified at `70c9ba534ebc858cad48ba260bb30e2901affbd2`:

- `ProjectStore.cs:8` snapshots `SaveRequest.Image`; its expected SHA-256 is either
  null for create-only or an exact disk token for cooperative overwrite.
- `ProjectStore.cs:26` defines confirmed durability as file fsync plus final-directory
  fsync after owned cleanup. `Supported()` admits macOS arm64 only. `cfd_store.c`
  supplies Apple's variadic open ABI; it is not a cross-platform native bridge.
- The macOS path uses retained parents, no-follow relative opens, a fixed directory
  claim, no-replace create publication, overwrite hash/identity checks, owned cleanup
  and final-directory flush. [ADR 0004](../../adr/0004-application-project-contract.md)
  explicitly accepts cooperative writers and distinguishes that from hostile CAS.
- `WorkbenchController.cs:2928` acknowledges only `OK`, known publication, confirmed
  durability and matching hash. Its uncertain-save resolution at `:2954` requires a
  second durable save; matching readback alone cannot acknowledge a save.
- `PreferenceStore.cs` advances its disk token on `OK`; `SectionLibrary.cs:86` calls
  `PublishUnderClaim`. These are part of the Windows routing surface, not just the
  `SaveAsync` entry point.

Dependency: the coordinator reports W-1 delivered in
[PR #3](https://github.com/timianmalloo/CFD-Workbench/pull/3), head `cb34dcee`, tested
evidence SHA `75335f8d`; leader disposition remains pending. This author did not
inspect or execute that receipt and makes no W-1 runtime claim. B2 waits for that
Mac disposition and the B1 gates below.

Graph grounding follows `coordination-pc-kickoff` → `coordination-two-machine` and
`coordination-windows-runtime-route` → `design-windows-runtime` →
`design-application-contracts`; ADR 0004 and ADR-0011 constrain byte publication and
backup semantics. The related STORE-ALIAS lesson requires one claim per held parent,
not one lock per target spelling.

## 2. Domain and durable representation, before native contracts

Bounded context: **Document Persistence**, an infrastructure boundary for the
**Authoring** context. Ubiquitous language: *image* is the complete immutable byte
snapshot; *disk token* is SHA-256 of exactly those bytes; *publication* changes the
selected directory entry; *durability confirmed* is the accepted flush contract;
*claim* serializes cooperating writers; *owned entry* is identified by a retained
handle, never by a convenient pathname alone.

The Authoring aggregate root is `AuthoringSession`: accepted history, cursor/recovery
facts and Analysis rows must obey the existing `NativeProject` invariants as one
document image. A save does not mutate that aggregate. The persistence operation
is a small transient consistency boundary rooted in one retained parent directory:
**publication exposes a complete captured image or preserves the old complete image,
and cleanup never removes a foreign object**. Other aggregates are referenced by
existing revision/run identities. A layout or display preference is a different
whole-document operation, not a member of the project transaction.

Logical representation and history rules conform to the accepted ADRs:

| Grain, declared before attributes | Identity and history rule | Measures / derivation |
|---|---|---|
| One source snapshot is one exact accepted source version | Existing source/design identity; immutable versions, not updates | Encoded size is non-additive across repeated snapshots; no second parsed-geometry store |
| One accepted fact is one accepted revision with its existing receipt | Existing accepted ID and parent; immutable append-only history | Current revision/redo state derives by replay; no Windows active pointer |
| One cursor fact is one recorded Undo/Redo transition | Existing cursor ID; append-only | Counts additive over distinct facts; state is non-additive |
| One Analysis run is one evaluation attempt at the ADR-0011 grain | Existing run ID/key; immutable runs, explicit retention tombstones | Scientific values are non-additive unless their existing domain definition says otherwise; keys/freshness derive in Core |
| One native image is one complete serialized envelope at a save attempt | `cfdw-project-1` or `-2` chosen by the existing writer | Byte count is non-additive across versions; disk hash derives from bytes |
| One display document is one installation user's current display choice | Existing `cfdw-display` version 1; intentional Type-1 preference state | Text size and units are non-additive; no historical unit rewrite of run facts |
| One local I/O event is one stage of one operation | Existing trace/operation fields; bounded session ring | Counts and byte volume additive over distinct operations; serial durations additive, concurrent wall time is not |

Physical representation stays the existing UTF-8 JSON envelope, one file per complete
image, plus reserved transient claim/temp entries. There is no database, sidecar
history, snowflake, Windows-specific persisted field or data backfill. Core writers
and `NativeProject.Read/Replay`, session reopen and Analysis readers remain the
producers/consumers; Windows transports their bytes unchanged. Read-size admission
uses `NativeProject.MaxBytes` (observed 8,000,000), not a duplicated native constant.

[Ruling 121](../../notes/rulings.md#ruling-121--units-remembered-by-a-shared-preference-key-added-on-the-mac-analysis-only-one-status-item)
and the Mac xmsg handoff of 2026-10-06 require using the existing optional `units`
key. `DisplayPreferences.Parse`, `LoadUnitsAsync` and `SaveUnitsAsync` already exist:
missing key means Metric; Imperial affects Analysis display only. A text-size save
must preserve the loaded/requested units. B2 must not create a second preference
file or change CAD millimetres. This is a shared platform-neutral contract.

ADR-0011's first `-1` → `-2` save keeps exact old bytes in `<name>.v1.bak` before
project publication. An existing backup is preserved. On Windows, missing approved
backup namespace durability blocks that transition before replacement; a file-content
flush alone does not satisfy the backup promise. No migration is introduced.

## 3. Simplest correct shape and frozen helper path

Selected candidate: reuse `IProjectStore`, `SaveRequest`, results, Core codecs,
telemetry and directory claim. Add one Windows implementation and one native C
bridge; route the existing public `ProjectStore` facade by platform. This is the
Adapter pattern plus handle ownership through `SafeHandle`. No new package, generic
filesystem framework, broker, background recovery scanner or parallel writer is needed.

**Frozen NEW native helper source:**
`src/CfdWorkbench.Persistence/native/cfd_store_windows.c`.
**Frozen managed implementation:**
`src/CfdWorkbench.Persistence/WindowsProjectStore.cs`.
**Native artifact:** `cfd_store_windows.dll`, application-directory load only.
`native/cfd_store.c` is excluded from B2, including conditional-compilation edits.

Ladder: the Windows need is real; reuse the existing aggregate/codec and facade;
`System.IO` alone lacks directory-relative creation and the required identity/security
contract; native APIs supply the necessary primitives. A small C bridge lets the
SDK compiler own NT/Win32 structure layouts instead of duplicating their ABI in C#.
Use SDK symbolic constants and compiler `sizeof`/`offsetof`; record the actual SDK
and compiler before implementation. No guessed numeric flags, managed Darwin errno
translation, copied `Stat` layout or unqualified WDK declaration is allowed.

`File.Move`/path-only `MoveFileExW` are rejected as namespace/security fallbacks.
`ReplaceFileW` preserves several original attributes/streams, but is path-based;
its documented intermediate failure outcomes and unsupported write-through option
do not supply the selected handle-relative/durability contract.
[Microsoft ReplaceFileW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew).

`simplify:` keep one fixed claim for all cooperating overwrites/library publication
in one directory; accept false contention. Upgrade only after measured contention
and an independently approved identity-based per-file lock design. Create-only
publication still relies on native no-replace, so competing creators cannot win by
omitting the cooperative claim.

## 4. Native API, handles, paths and sharing contract

The bridge exposes only owned handles, fixed-width identity/status records and
bounded byte operations. Managed code owns hashes, cancellation boundaries and
product result mapping. Native code owns SDK layouts, relative open/rename,
creation-time descriptors and exact raw errors. Neither serializes project data.
Capture NTSTATUS separately from Win32 last error before another API call; check
completion status, never treat a warning/pending result as completed I/O.

### Supported candidate subset

First qualification target: Windows x64 on a fixed local NTFS volume with persistent
ACL support. Arm64, ReFS, SMB/UNC, removable media, device paths supplied by the user,
cloud placeholders and case-sensitive directories remain unsupported until separate
qualification. Accept normal absolute drive-letter paths with well-formed UTF-16;
bound input at 32,768 UTF-16 code units and at 256 directory components. Reject NUL,
empty or dot components, drive-relative paths, `/`, ADS colon inside a component,
reserved device basenames/extensions, trailing dot/space and the `.cfd-` namespace
case-insensitively. Bound components by the handle-observed volume limit. Do not
normalize Unicode bytes/names or infer alias equivalence; qualify composed/decomposed
spellings and 8.3 aliases with actual handle identities. These bounds are proposal
admission rules, not a claim that every accepted spelling already works.

Bind a drive root once to an OS-returned volume GUID, open that root directory and
check the handle's NTFS/volume identity. Subsequent components are relative to held
directory handles; the DOS drive spelling is not used for mutation. Recheck the
drive-to-volume binding before publication and after read. A changed/unverifiable
binding is conflict; an undetected malicious drive-map swap is outside the accepted
OS-user/cooperative-writer threat posture, never a proven containment guarantee.
[Microsoft volume management](https://learn.microsoft.com/en-us/windows/desktop/FileIO/volume-management-functions),
[handle volume query](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumeinformationbyhandlew).

### Handle matrix

| Object / lifetime | Desired access | Sharing / disposition | Purpose and qualification condition |
|---|---|---|---|
| Root and each ancestor, retained until completion/cleanup | `FILE_TRAVERSE`, `FILE_READ_ATTRIBUTES`, `READ_CONTROL`, `SYNCHRONIZE` | Share read + write, never delete; open existing | Prevent compatible rename/delete of retained directories; this does not exclude in-place metadata/reparse mutation |
| Existing target for overwrite, held through prepublish checks | `GENERIC_READ`, `READ_CONTROL` | Share read + delete, never write; open existing | Exclude compatible new write opens; allow our rename replacement and old held readers; no arbitrary-writer CAS |
| Target for ordinary read | `GENERIC_READ`, `READ_CONTROL` | Share read + delete, never write; open existing | Stable held old image across a replacement; namespace mismatch yields conflict |
| Claim and new temp/backup temp | `GENERIC_READ`, `GENERIC_WRITE`, `DELETE`, `READ_CONTROL` | Share read, never write/delete; create new only | Own bytes, flush, rename/disposition through our own handle; collisions preserved |
| Short-lived identity observer of owned temp/published file | `FILE_READ_ATTRIBUTES`, `READ_CONTROL` | Share read + write + delete; open existing relative | No payload read through this observer; compatibility with retained write/delete access must be measured |
| Reopened final readback, after temp write handle closes | As ordinary read above | As ordinary read above | Exact bytes/hash and new identity independently checked |

Sharing is checked against existing accesses in both directions, not a universal
writer lock. `CreateFileW` documents metadata-access exceptions; share denial does
not establish immutable DACLs or block every filesystem filter. B2 must test already
open writers and writable mappings, not only opens created after the store handle.
[Microsoft CreateFileW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew).

Root acquisition uses `CreateFileW` with `OPEN_EXISTING`,
`FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT`, a non-inheritable handle
and no privilege enablement. Directory child opens use user-mode `NtCreateFile`
with one literal component in `UNICODE_STRING` and a retained
`OBJECT_ATTRIBUTES.RootDirectory`. Select `FILE_OPEN` or `FILE_CREATE`, synchronous
nonalert I/O with `SYNCHRONIZE`, and `FILE_OPEN_REPARSE_POINT`; do not combine a
guessed set of directory-only options. Query attributes after the open and require
a directory for ancestors, a disk regular file for leaves. No zero-access native
open, supersede, overwrite-in-place or delete-on-close is used.
[Microsoft NtCreateFile](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntcreatefile).

Every opened object is checked with `FileAttributeTagInfo`; **all** reparse tags are
rejected, including symlinks, junctions, mount points and cloud tags. A final-component
flag on one full path is insufficient. Retain every ancestor and re-open its single
name relative to the previous retained parent to compare identity before mutation,
after publication and after read. Any tag, missing entry or identity mismatch refuses.
[Microsoft attribute/tag record](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_attribute_tag_info).

**Flagged namespace qualification:** share-write is required by the candidate parent
publication path and permits more than child creation. An ancestor can acquire a
reparse point without changing file ID. Pre/post attribute checks alone have a race.
Qualification must establish that the retained directory used as `RootDirectory`
cannot redirect child resolution under in-place reparse mutation; otherwise refuse
production writes. The native `OBJ_DONT_REPARSE` contract is an additional candidate
guard, not assumed proven: its generic OBJECT_ATTRIBUTES documentation and the
narrower NtCreateFile attribute wording must be reconciled by the actual SDK/native
probe. Do not silently remove an unsupported guard to get green.
[Microsoft OBJECT_ATTRIBUTES](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wudfwdm/ns-wudfwdm-_object_attributes),
[reparse mutation API](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_set_reparse_point).

Handle identity is `(FILE_ID_INFO.VolumeSerialNumber, FILE_ID_INFO.FileId)` from
`GetFileInformationByHandleEx`. Compare the entire 128-bit ID, with handles alive;
refuse unavailable identity. Volume serial alone, spelling, timestamp and hash are
not object identity. Check file type, reparse status, link count and size separately.
Reject leaf hard-link count other than one; qualify later hard-link creation races
and preserve every foreign link. There is no durable file-ID cache: IDs can be reused
after handles close. Hash identifies image content; it does not substitute for ID.
[Microsoft FILE_ID_INFO](https://learn.microsoft.com/windows/win32/api/winbase/ns-winbase-file_id_info).
`FileStandardInfo` supplies size, directory/delete-pending state and link count;
`GetFileType` must also establish a disk handle. Refuse pending deletion or unavailable
metadata rather than borrowing a path-only check.
[Microsoft FILE_STANDARD_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_standard_info).

## 5. Publication, security, cleanup and durability

### Operation sequence and atomic replacement contract

1. Snapshot bytes already supplied by `SaveRequest`; validate size/path and capability
   before mutation. Acquire/verify parent chain. For overwrite acquire the fixed
   `.cfd-writer.claim` with create-new, inspect its descriptor, open target, hash the
   held disk bytes and compare the exact expected token.
2. For the first `-1` → `-2`, publish/flush the old-byte `.v1.bak` first using the same
   no-replace and approved durability contract. Preserve an existing backup.
3. Create `.cfd-<operationId>.tmp` relative to the held parent with the private descriptor
   present **at creation**. Check effective descriptor before the first payload byte.
   Write bounded chunks, account for short writes and reject zero progress. Flush the
   complete temp with `FlushFileBuffers` before publication.
4. Check cancellation, parent chain, owned claim/temp and target entry identity.
   Re-read target immediately before overwrite and require the same expected hash.
   Keep the expected target handle alive; do not call this sequence CAS.
5. Rename the **temp handle** with `SetFileInformationByHandle(FileRenameInfo)`;
   `FILE_RENAME_INFO.RootDirectory` is the held destination parent and `FileName`
   exactly one component. `ReplaceIfExists=FALSE` for create-only/backup/library;
   `TRUE` for cooperative overwrite. No copy, cross-volume fallback, destination
   unlink, absent-check/replace fallback or path-only retry is allowed.
6. On native success verify destination entry identity equals retained temp identity
   and parent chain remains selected. The renamed temp handle now denotes the
   published object; **do not disposition it during cleanup**. Close its write handle
   and independently reopen/read/hash the destination. Remove only owned transient
   claim/unused temp objects. Perform the approved final namespace flush after cleanup.
7. Return truthful publication/durability facts; cancellation after publication cannot
   return an unsaved cancellation. An uncertain save requires reopen/compare before
   any explicitly requested retry; never automatically overwrite on an ambiguous error.

The required atomic namespace behavior is old-or-new complete image at each new open,
with no missing target interval; an existing held reader may keep the old image.
Create-only must never replace a competing entry. Microsoft's rename contract gives
replace/no-replace and relative-root semantics; this candidate does **not** treat that
description as an independently observed all-fault atomicity guarantee. B2 must
qualify the concrete local NTFS operation with real concurrent readers/creators,
fault seams, held readers and identity evidence. Namespace atomicity, expected-token
CAS, crash consistency and durable publication are separate claims.
[Microsoft FILE_RENAME_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_rename_info),
[SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle).

Cooperating overwrites are serialized by parent identity plus fixed claim. An outside
writer can swap the destination after our last check because delete sharing is
necessary for replacement. A successful rename cannot prove that the expected entry
was still there at that instant. This accepted ADR 0004 limitation must remain visible
in the Security verdict; it is not widened to a hostile same-user CAS promise.

### Creation-time security and owned cleanup

Proposed private-file policy: protected DACL, owner equal to the effective token's
user SID, one current-user allow ACE with full file rights, no inherited, Everyone,
group or null-DACL grant. Use the effective token consistently; impersonation changes
during the operation refuse admission. Creating under a permissive parent must still
yield that descriptor. Inspect it through the created handle before payload writes;
validate real allowed/denied access with disposable native cases. No post-write ACL
repair, privilege enablement or global trust/registry change is authorized.
The native create descriptor is creation-only, as the
[NtCreateFile contract](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntcreatefile)
states. Windows ACLs are not a transliteration of POSIX 0600.

Read owner/DACL through `GetSecurityInfo(SE_FILE_OBJECT)` with retained `READ_CONTROL`
access; free its allocated descriptor with the documented allocator. Establish the
effective token from the thread impersonation token when present, otherwise the
process token. Descriptor shape alone is not complete effective-access proof:
qualification pairs it with `AccessCheck` using an impersonation token and correctly
mapped file rights, and with real native allowed/denied opens. Record token groups,
restrictions and integrity-policy limits locally in the synthetic proof; failure to
establish them remains Not assessed. No caller-supplied SID becomes the owner policy.
[Microsoft GetSecurityInfo](https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-getsecurityinfo),
[impersonation tokens](https://learn.microsoft.com/en-us/windows/win32/secauthz/impersonation-tokens),
[AccessCheck](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-accesscheck).

Overwrite admission requires the held existing object's owner/protected DACL to match
the same private-file policy. Otherwise return `DOC-UNSUPPORTED-PERSISTENCE`, preserve
it and offer the existing Save As path; do not silently broaden, preserve arbitrary
imported ACLs or normalize them. Read-only imported documents may be read if authorized
and otherwise safe; a save over them still requires the private overwrite policy.
Inherited EFS/compression and named-stream metadata require explicit qualification or
refusal, not accidental deletion under the ordinary-content contract.

Claim/temp cleanup requires creation provenance, retained handle identity, and the
current relative name still matching. Use handle disposition with `DELETE` access;
foreign name occupants are never deleted. A failed/mismatched check retains the
entry and reports conflict or cleanup failure. A successful publication transfers
the temp object's role to destination and disables its transient deletion path.
Deleting an owned object through a retained handle cannot delete an unrelated
replacement by name. No age-based stale-claim reclaim or directory recursion exists;
a crash-left claim blocks later overwrite until reviewed.
[Microsoft FILE_DISPOSITION_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_disposition_info).

### Durability decision: a hard product gate

`FlushFileBuffers(temp)` is the selected file-content flush. It requires write access.
No inspected Microsoft contract here establishes that a final-directory handle flush
after rename **and cleanup** supplies the existing final-directory-fsync promise.
An administrative volume flush is out of scope. Write-through data writes are not
proof of persistence of a later rename/delete; `REPLACEFILE_WRITE_THROUGH` is explicitly
unsupported. A successful native call or repeated readback never proves hardware
power-loss survival.
[Microsoft FlushFileBuffers](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-flushfilebuffers),
[ReplaceFileW flags](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew).

**D-B1-DUR, Flagged:** Mac/Fable and Data must select one of these explicit outcomes:

- **Recommended current boundary:** approve candidate qualification only. Keep Windows
  production write admission fail-closed (`DOC-UNSUPPORTED-PERSISTENCE`) until a
  documented, independently accepted namespace durability equivalent is demonstrated.
  B2 may implement/probe isolated primitives, but W-2 cannot be declared complete.
- Require a separate shared-contract design/ruling for a Windows publication-confirmed,
  durability-unconfirmed product outcome. This changes acknowledgement/retry/preference/
  backup behavior and needs Mac-owned UI/service/spec review. It is not approved here
  and cannot be implemented by returning weaker `OK` or changing this Boolean's meaning.

In an authorized qualification experiment that has already published, report
`DOC-SAVE-UNCERTAIN`, `PublicationKnown=true`, matching published hash,
`DurabilityConfirmed=false`. On failed/ambiguous publication report uncertain with
`PublicationKnown=false`, no published hash; false means not established, not proof
that nothing was written. Never erase evidence or auto-retry. This diagnostic behavior
does not make an always-uncertain production adapter usable: the verified controller
and preferences would remain dirty/unacknowledged. The backup gate is equally binding.

## 6. End-to-end surfaces and exact prospective B2 paths

The chain to verify is store/native → result/domain → public facade/service → typed
result/telemetry → desktop client/controller → existing Save/Open/uncertain UI → Core
reopen/Analysis reader. Project JSON is the existing wire representation. There is
no HTTP surface, new API DTO or changed geometry computation.

| Exact prospective path | Intended B2 change | PC allowance / handoff |
|---|---|---|
| `src/CfdWorkbench.Persistence/native/cfd_store_windows.c` | New Windows-only native primitive bridge; frozen helper source | Within Persistence Windows path; exact coordinator lease required |
| `src/CfdWorkbench.Persistence/WindowsProjectStore.cs` | New Windows adapter, status/security/identity and lifecycle | Within allowance; exact lease required |
| `src/CfdWorkbench.Persistence/ProjectStore.cs` | Windows branch at Save/Read/PublishUnderClaim facade; preserve all macOS bodies | Within allowance, shared-file intent explicitly reviewed by Mac before B2 |
| `src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj` | Windows-only helper build/copy; leave existing macOS target intact | Within allowance; exact lease plus compiler/SDK decision required |
| `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` | Retain Mac cases/expectations, add Windows path selection and native cases | Outside PC allowance: affirmative **Mac handoff required** |
| `tools/verify-application-core.py` | Declare Windows native proof coverage while preserving Darwin masks/helper faults | Outside PC allowance: affirmative **Mac handoff required** |
| `docs/proof/application-core.md` | Shared proof status/Windows admission after executable evidence | Outside PC allowance: affirmative **Mac handoff required** |
| `docs/proof/win-store-implementation/receipt.md` | Exact tested SHA, host/SDK/compiler and command evidence | Within allowance |
| `docs/proof/win-store-implementation/native-spike.md` | Native request shapes, raw errors, descriptors/identities and refusal cases | Within allowance |
| `docs/proof/win-store-implementation/red-green.md` | Observed RED then GREEN, immutable case outputs and negative controls | Within allowance |

`src/CfdWorkbench.Persistence/native/cfd_store.c` is **not** a prospective B2 path.
`PreferenceStore.cs` and `SectionLibrary.cs` are read-only consumers for the current
candidate: route their existing store calls, not parallel writes or new units logic.
If D-B1-DUR selects semantic changes, the above lease is invalid: Mac must first
design and authorize exact shared edits to `WorkbenchController.cs`, `PreferenceStore.cs`,
`SectionLibrary.cs`, their tests, accepted ADR/specs and result contract documentation.
Those edits are a separate handoff, not hidden inside this implementation plan.

Mac compatibility: keep Darwin helper exports, .dylib name, `Stat`, P/Invokes, errno
mapping, open/flush/backup algorithms and MSBuild condition unchanged. Windows handles
must never enter an `int` file-descriptor path. Windows build failures are stable
unsupported capability, with no `System.IO` fallback. Linux/other architectures remain
fail-closed. Mac leader independently runs readiness on the joined HEAD.

## 7. Failure-mode analysis

| Mode | Disposition / observable outcome | Required falsifier |
|---|---|---|
| Malformed/reserved/oversized path or image | Prevent native access; stable unsupported/size code | Boundary size, Unicode, ADS/device/UNC/path alias fixtures; no bytes changed |
| Unsupported host/FS/helper/ABI | Detect and refuse before mutation | Missing/wrong-architecture DLL, missing entry point, wrong ABI version/layout |
| Claim/temp collision or crash-left claim | Preserve foreign entry; conflict; no reclaim/retry | Exact foreign bytes/ID unchanged |
| Denied/foreign DACL or reparse/hard link | Detect and refuse; preserve content | Native denial, inherited grants, replacements and link/mutation cases |
| Short write, no progress, disk full, flush failure | Keep old file before publish; owned cleanup; disk-full/I/O | Real chunked write plus injected failure after a real prefix; original bytes equal |
| Ancestor/drive/target identity changes | Detect conflict; verify old and substituted trees | Controlled substitution/reparse hooks and independent external-fixture hashes |
| Noncooperating target rename after final check | Accepted cooperative-writer residual only; no CAS guarantee | Deterministic race fixture documents exact limitation |
| Native publication error/ambiguous state | Uncertain; reopen/compare; no fallback or automatic retry | Wrong-result negative must reject a falsely unsaved/successful receipt |
| Cancellation | Before publish preserves old; after publish preserves truthful known outcome | Hook-triggered cancellation on both sides of native rename |
| Postpublish identity/readback/cleanup/namespace-flush failure | Uncertain with only established publication facts; never acknowledge | Verify result fields, complete destination bytes and retained dirty session |
| Helper/handle lifetime/disposal race | SafeHandle ownership; in-flight completion truthful; closed requests refused | Missing events after owned session closes; all owned handles closed |
| Blocking native call/resource exhaustion | Bounded chunks/size/chain; no spin/retry; OS call latency recorded | Finite guard for test hang; no unsupported claim of cancellable sync Win32 I/O |

Synchronous native calls are not promised to be interruptible by CancellationToken.
Cancellation is observed between stages. No timeout may manufacture a not-published
result after a namespace operation. B2 records native call latency and handle balance.

## 8. Adversarial analysis (STRIDE-lite)

Trust boundaries: caller path/image → managed adapter; managed adapter → native ABI;
native handles → filesystem and effective token; helper artifact → loader; local
filesystem outcomes → session telemetry/acknowledgement.

| Threat | Disposition / minimum control | Misuse test |
|---|---|---|
| Spoofed path or object identity | Mitigate with component-relative opens, live volume/file ID and tag checks; never raw-spelling lock | Case/Unicode/8.3 aliases, target and ancestor replacement |
| Tampered bytes/target/claim | Mitigate immutable capture, disk token, private creation, owned handles and no-replace; accept same-user hostile CAS limit explicitly | Mutate caller buffers, stale token, competing creators, collided cleanup |
| Repudiated or ambiguous outcome | Mitigate stage-correlated outcome/publication/durability telemetry; raw native status in bounded proof | Stage faults and wrong-result receipt negatives |
| Project/path/SID disclosure | Mitigate private creation and existing redacted local telemetry; no path/image/hash/SID in production events | Private marker search in emitted JSON; permissive-parent descriptor inspection |
| Denial of service | Mitigate bounds and fail-closed collision; accepted stale-claim manual recovery | Oversized read, claim-held, unsupported filter/capability behavior |
| Elevation of privilege or helper substitution | Mitigate non-inheritable handles, no privilege changes, native helper load from assembly directory; OS ACL/user boundary retained | DLL missing/substituted/wrong ABI; no handle inheritance in child fixture |

Security cannot approve a claim that share denial freezes all metadata or that
reparse checks alone close the mutation race. Native qualification must answer the
retained-RootDirectory mutation question. Administrators, kernel compromise and
malicious processes with the same user rights are outside the accepted protection
boundary; qualification does not enlarge that boundary by implication.

## 9. Privacy analysis (LINDDUN-lite) and telemetry

Project contents and local paths may contain personal data; a Windows SID identifies
the effective user. They are processed only locally for this operation. Linkability/
identifiability/disclosure: omit paths, bytes, SID, descriptors and content hashes from
production telemetry. Non-repudiation/detectability: the existing session ring is local
and bounded, not an exported audit store. Unawareness: no new background scan/export.
Retention/non-compliance: retain only owned operation objects until outcome/cleanup;
crash-left entries require explicit reviewed recovery and are never uploaded. Native
proof fixtures are synthetic; proof may show disposable paths and SID evidence only
after reviewer-approved redaction. No new cloud service or telemetry exporter exists.

Reuse `AuthoringSession.RecordPersistence` and the existing bounded ring. Emit normal
path `store.read`, `store.file-flush`, `store.publish`, `store.flush`, `store.backup`,
`store.save` and `library.save` as applicable, including failure paths. Operator
questions: elapsed time → measured duration per stage; volume → input/output bytes;
failure path/rate → stable code and action; publication/durability → explicit booleans;
which operation → existing trace. Derive aggregates from these events, not a second
counter store. Missing measurement stays not recorded; telemetry failure cannot alter
known I/O outcomes. Preserve the current redaction and lifecycle tests.

The existing trace field is a session correlation ID, not evidence of a complete
OpenTelemetry span pipeline. No new exporter or shared Core telemetry schema is in
B2. OTel severity/span expansion would require a separate measured shared seam.
There is no HTTP boundary; RFC 9457 is N/A. UI layout/tokens are unchanged; the existing
uncertain/unsupported states are traced and must be captured, not redesigned here.

## 10. Test strategy, rings and admission oracles

Testing Strategy union: D0 deterministic hermetic hygiene; D1 result/error/cancellation
branch mutations; D2 bounded path/Unicode/size laws and exact byte round trips;
D3 platform/build dispatch; D4 real local NTFS native integration; D6 unchanged
project/preference golden images. D7 is triggered only if a boundary substitute is
added: it then needs a real-native fidelity pair; the candidate needs no I/O mocks.
AI/MCP/HTTP directives are N/A. The native API read is complete; execution spike is
**Not assessed** in B1, and mandatory before production dispatch/admission.

| Test group | Oracle / deliberate wrong result that must fail | Ring and cost |
|---|---|---|
| Existing portable store behavior | Exact captured/readback bytes+hash, collision/conflict, cancellation, faults, telemetry, lifecycle, backup and old-build behavior from ProjectStoreTests | Relevant Windows Core store subset first; full fast ring once per B2 join, total existing 60 s budget |
| Existing Darwin cases | Existing names, permissions/umask, native helper faults and durable outcomes unchanged | Mac readiness at leader join; existing cost policy, not rerun on Windows |
| Windows create/replace concurrency | Real separate handles/processes, complete old/new bytes; exactly one no-replace creator; old held reader remains old; no missing/partial target | Native integration in Windows qualification/readiness; cost not recorded, measure before adding to fast ring |
| Windows path/reparse/identity | Actual leaf symlink, junction, ancestor substitution and in-place reparse mutation; no writes into external fixture; parent/file IDs and tag record | Qualification/readiness; missing fixture rights = Not assessed/nonzero, never skip green |
| Windows sharing/alias | Already-held writer/mapping, target without delete sharing, parent rename denial, compatible publication; Unicode/8.3/case aliases share fixed claim | Qualification/readiness; measured cost required |
| Windows security | Descriptor before first write under permissive parent; foreign overwrite refused; effective allowed/denied opens; private replacement; no inheritance leak | Qualification/readiness; measured cost required |
| Cleanup/lifecycle faults | Foreign claimant/temp preserved; pre/post rename ownership state; published handle never disposed as temp; handle counts stable after repeated bounded operations | Relevant Windows subset; cost measured before promotion |
| Flush/durability/uncertainty | File flush succeeds/fails separately; final namespace contract proof; wrong `durable=true`, matching-readback-is-durable and false-not-saved negatives all fail | Qualification/readiness; no weaker fast-ring substitute |
| End-to-end readers | Real project create/open/overwrite/session dirty state, v1 backup/open, v2 Analysis run identity; display text-size/Metric/Imperial preserve optional units; library publication | Windows product acceptance after primitive and contract gates; Mac readiness checks compatibility |

Windows-specific expectations do not globally relax `OK`/durability or rename Mac
primitive tests. Keep POSIX-only fixtures Mac-only and add explicit Windows counterparts;
the current `NOT ASSESSED` early return cannot count as native proof. Use existing
console harness/`CFD_TEST_ONLY`, not `dotnet test`. Synchronize race seams with events,
never timing sleeps. Report each required case by name with raw outcome, exact tested
SHA, SDK/compiler/OS/FS and byte/identity evidence. No zero-PASS or missing required
case can be accepted. Power-loss survival remains outside all these tests.

B1 is docs-only and runs `py -3 tools/check-docs.py`; no source/test/build path changes
means no test ring. It introduces no executable gate and no new test cost. Graph
index derivation, audit/change entries and security/privacy rollups are coordinator
work under this exact-path lease; index drift from these new artifacts is reported,
not repaired by editing derived/shared files. No unrun test is recorded as passed.

## 11. Explicit Mac/Fable and independent approval gate

| Seat | Required decision / veto-clearing evidence | Current state |
|---|---|---|
| Mac leader | Affirmative handoff for final design path and every B2 path outside PC allowance; W-1 disposition | Pending |
| Fable Owner | Candidate scope, supported subset, helper path and D-B1-DUR ruling; prevent product success under weaker semantics | Pending |
| Independent Data & Persistence Architect | Preserve aggregate/bytes/history/units; valid backup+rollback and exact durability/result semantics; no violated data-integrity invariant | Pending, author cannot clear |
| Independent Security & Identity Architect | Creation-time effective rights, imported-ACL policy, retained-directory/reparse mutation and target-race boundary; no unqualified containment/CAS | Pending, author cannot clear |
| Independent Test Architect | Real NTFS spike and complete named oracle matrix, unchanged Mac checks and exact ring coverage; missing rights/cases fail closed | Pending, author cannot clear |

Data BLOCKS if Windows can acknowledge weaker durability, overwrite a foreign/partial
image, lose backup/history, or invent incompatible units/schema. Security BLOCKS if
the selected native request can follow an unqualified redirect, writes before private
creation is checked, deletes foreign objects, loads an arbitrary helper, or asserts
CAS/hostile namespace containment. Test BLOCKS if mocks/compiler success replace real
native execution, skipped required fixtures read green, or Mac assertions are weakened.
Any unresolved hard veto stops B2 admission. A design approval alone does not clear
the runtime qualifications; a native test PASS alone does not rule product semantics.

**Exact approval request for Mac/Fable:**

> Review this B1 candidate. Authorize writing `docs/design/windows-native-store.md`
> from it and freeze the separate helper `src/CfdWorkbench.Persistence/native/cfd_store_windows.c`
> with `WindowsProjectStore.cs`; exclude `native/cfd_store.c`. Rule D-B1-DUR: approve
> isolated Windows qualification with production writes fail-closed until equivalent
> final namespace durability is established, or commission the separate shared
> acknowledgement/durability contract design. Confirm W-1 PR #3 disposition. Approve
> or narrow the exact B2 paths in section 6, with affirmative Mac handoffs for
> `ProjectStoreTests.cs`, `tools/verify-application-core.py` and `docs/proof/application-core.md`.
> Obtain independent Data, Security and Test verdicts against section 11. B2 production
> admission and W-2 completion remain blocked until the selected durability contract,
> handle-relative/reparse/security qualification and every hard veto are resolved.

Open decisions are finite: final namespace durability/acknowledgement contract;
retained-directory reparse mutation and native relative-root ABI qualification;
exact compiler/SDK availability/build route; private imported-ACL/metadata policy
approval; prospective shared-path handoffs and W-1 Mac disposition. Expanding platform/
filesystem support or rescuing stale claims is a later design, not this repair loop.

| Status | Result |
|---|---|
| Completed | Source-cited B1 candidate, DDD/representation, native contract, exact helper/path list and approval package |
| Remaining | Final design-path handoff, coordinator audit/derivation, independent approvals and B2 native/product evidence |
| Best next action | Mac/Fable rule the concrete approval request; do not dispatch production B2 on unresolved durability/namespace gates |
