---
id: proof-windows-store-implementation
title: Windows managed native store first qualification checkpoint
type: doc
status: in-review
owner: "@windows-worker"
phase: implementation
tags: [windows, persistence, proof, w-2]
links:
  - { to: design-windows-native-store, rel: depends-on }
  - { to: review-pr-5, rel: depends-on }
  - { to: review-pr-6, rel: depends-on }
review-by: "2026-11-08"
summary: >-
  Native qualification slice only. Production admission is false. This checkpoint
  qualifies managed ABI declarations and selected real-NTFS primitives; it does
  not qualify the Windows ProjectStore adapter or the user save/recovery flow.
---

# Windows store qualification checkpoint

Goal: qualify the smallest managed native slice before admitting Windows persistence.
Done when: observed RED, native layout/operation evidence, bounded leased diff and
an explicit unmet-case inventory are reviewable. Tier T2; fan-out cap 1; repair cap
2. No subagents, full application ring, PR or push. Coordinator owns audit, defect
register and graph derivation; this artifact supplies their handoff content.

## Stage 0–2: contract and execution graph

Seat: implementation author, not independent veto reviewer. Rulings 135–137 replace
the C-helper proposal with LibraryImport/SafeHandle and approve only file flush plus
POSIX rename. No directory-fsync or power-loss equivalence is claimed.

Graph: ground leases and contracts → write functional RED → bind native layouts →
qualify private creation/rename/disposition on real NTFS → capture targeted evidence
→ checkpoint. Each operation is qualified before the adapter consumes it. A failed
contract is a decision request with raw native error; no C fallback. Build worker cap
4; this track runs no intentionally parallel harness.

Surface list: persistence native boundary and Windows test gate are changed. Core
domain/model, UTF-8 project envelope, accepted history, preferences `units` key,
service, projection/wire, client type, UI and compute readers are unchanged. Adapter
dispatch, public Save/Read and library publication are unmet. Shared UI handoffs are
listed below; production admission stays false.

DDD: Authoring/Native Project bounded context; the existing accepted document is
the aggregate, with complete immutable envelope and exact byte/disk-token invariants.
One durable image is exactly one complete serialized envelope. Accepted versions and
analysis facts retain existing append-only history; display preferences retain Type-1
semantics. Hash and current state derive from bytes/history. Native handles are
ephemeral value/ownership objects, not another durable model. Ruling 121's optional
`units` key remains the only units representation.

Test-trigger union: D0 always; T1/D1 (flags/error policy and mutation counterchecks),
T2/D2 (single-component input boundary), T3/D3 (platform dispatch and unchanged
Darwin source), T4/D4 (real NTFS), T7/D6 (future byte/backup/units contracts).
No boundary fake is introduced (T8/D7 not triggered). No HTTP or AI behavior is
introduced. D2 whole-path properties and D6 envelope cases remain unmet until adapter.

| Failure/threat | Disposition in this slice | Remaining qualification |
|---|---|---|
| Wrong ABI/layout, stale last error | Explicit native structs, size/offset oracle, immediate raw errors | Every added layout/API needs its own spike |
| Held readers / deletion blocked | POSIX handle rename/disposition; old handle content oracle | Real adapter overwrite and claim cleanup |
| Inherited/broad ACL, impersonated owner | Creation-time protected owner descriptor; actual handle descriptor oracle | AccessCheck, restricted-token denied opens, imported ACL refusal |
| Collided target / writer race | Native no-replace preserves complete incumbent | Cooperative claim and competing store creates |
| Invalid child path / escape | Relative simple-component boundary | Full selected-chain, drive binding and race containment |
| Reparse/cloud/case-sensitive/Unicode alias | Not admitted to production | All hostile fixtures and refusal guards |
| Partial write / flush / uncertain rename | Raw errors; explicit file-content flush | Store results, cancellation, hooks and stage telemetry |
| Crash-left claims / privacy leakage | No automatic reclaim; no product log fields added | Visible recovery, cleanup provenance, redacted operation events |

## Unmet admission conditions

WindowsProjectStore adapter, retained root/ancestor chain and revalidation, reparse
and in-place reparse mutation, foreign identity preservation, private overwrite ACL
policy and AccessCheck, NFC/NFD duplicate refusal, case-sensitive directory refusal,
full long-path admission, hardlinks/streams/compression/EFS policy, real create races,
exact token recheck, partial writes/disk full, cancellation, complete uncertainty
mapping and operation telemetry, exact v1 backup, optional units integration and
crash recovery are NOT ASSESSED by this slice.

Mac handoff: add the local default `%USERPROFILE%\CFD Workbench` through the shared
save picker; show proposed OneDrive copy "Save this project in a local folder outside
OneDrive. Choose another folder."; show crash-left claim steps: reopen/compare saved
document, preserve pending bytes, close other writers, explicitly review/remove a
claim only after establishing no live writer, then Save As/retry. Never silently
delete a claim or announce a successful recovery. Shared UI paths are outside lease;
Mac must select/approve exact copy IDs and paths. Native OneDrive guards are still unmet.

Data, Security, Test and Fable vetoes remain OPEN for implementation. No author claim
clears a veto. WFX2 ring rows 1–4 and same-build UI walk rows 5a–6 are pending; COST-MISS
under Ruling 139 is evidence, never PASS.

## Stage 3: frozen observed checkpoint

The inherited shell resolved `C:\Program Files\dotnet\dotnet.exe` and could not
load the pinned SDK. `global.json` remains `10.0.203`, `rollForward: disable`.
The already-installed user executable `C:\Users\malla\.dotnet\dotnet.exe`
reports 10.0.203; its SDK list includes 10.0.203. System x64 SDKs are 9.0.315 and
10.0.301; an x86 executable also exists but was not used. The bounded command sets
`DOTNET_ROOT=C:\Users\malla\.dotnet`, selects check prefixes with `CFD_TEST_ONLY`,
and runs the user executable:

```text
dotnet run -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj -m:4
```

Every run captured stdout and stderr separately, with the exit code immediately
recorded outside a pipeline. The worker did not run a full ring or intentionally
parallel harness. These measured outer tool waits include process startup/build;
they are not per-test latency or calibrated cost budgets.

| Evidence stem (files beside this document) | Selector | Exit | Observed result |
|---|---|---|---|
| `sdk-resolution` | WindowsStore_ | -2147450725 | SDK resolver failure; not a behavioral RED |
| `red-store` | WindowsStore_ | 1 | 1 selected; create expected OK, actual DOC-UNSUPPORTED-PERSISTENCE |
| `red-native` | WindowsNative_ | 1 | 7 selected; layout/environment/name guard PASS, four failures |
| `cycle-1` | WindowsNative_,WindowsStore_ | 1 | 8 selected; protected creation added; three failures |
| `cycle-2` | WindowsNative_,WindowsStore_ | 1 | 9 selected; eight PASS, held-reader failure remains |

Each stem has `.stdout.txt` and `.stderr.txt`. The resolver's stderr contains the
SDK mismatch. Behavioral-run stderr is empty. Raw outputs are preserved verbatim;
the ACL RED contains local principal SIDs because it compares actual descriptors.
No new production telemetry field contains a SID, path, hash or project bytes.

**Native observations, confidence Verified on the measured fixture only:** .NET
10.0.203, Windows x64 10.0.26300, real NTFS. Microsoft SDK headers 10.0.26100.0 were
read as declaration oracles; no C compiler/toolchain was invoked. Seven unmanaged
structures passed size and every declared field offset. Relative NtCreateFile
success and protected owner-only DACL were observed before the first content byte.
File-content flush and retained-handle identity were exercised. These are primitive
spikes, not the Windows store's complete security/durability proof.

| Claim/oracle | RED observed | Final observation | Admission limit |
|---|---|---|---|
| Exact private descriptor before bytes | Default inheritance yields broader inherited grants | Protected current-owner descriptor PASS | Effective AccessCheck and denied restricted-token opens unmet |
| POSIX claim cleanup with held observer | Plain delete leaves namespace entry; premature close-free POSIX assertion also fails | Set DELETE\|POSIX, close deleting handle; name absent while observer reads PASS | Adapter cleanup ownership/current-entry provenance unmet |
| Retained-parent relative handle rename | Win32 Ex wrapper returns 87 with rooted relative name | Fresh NT Ex65 returns NTSTATUS=0, IO_STATUS=0, Win32=0; identity/bytes match | Create-only native call only; full store race/chain checks unmet |
| Native no-replace preserves incumbent | Win32 wrapper error 87 fails collision oracle | NTSTATUS=0xc0000035, IO_STATUS=0, Win32=183; both complete objects preserved PASS | Concurrent store create race unmet |
| Held reader retains old image while new opens see new image | Win32 87 | NT Ex65 flags3 with ShareRead-only reader fails NTSTATUS=0xc0000043, IO_STATUS=0, Win32=32 | Required ShareRead\|ShareDelete replacement NOT ASSESSED |
| Bounded relative name blocks escape | Contract spike, no deleted-guard mutant run | Empty/dot/dotdot/slash/backslash/ADS/NUL/256 refused; 255 created PASS | Full-path properties and containment unmet |
| ABI layout agrees with SDK | Contract spike, no knowingly wrong ABI deployed | 7 sizes plus all field offsets PASS | Other required structs/APIs and minimum OS build unmet |

The original successful-create expectation remains an unmet feature requirement;
it was not repaired by changing the assertion. The final `WindowsStore_Unqualified_`
case is a separate fail-closed admission guard and must not be reported as Windows
Save GREEN. `ProjectStore.cs` remains unchanged and still refuses Windows.

Repair count **2/2**. The coordinator counted cycle 1 and expressly authorized cycle
2 after a source-backed diagnosis. Cycle 2 had a residual sharing failure, so the
cap fired and the executable track stopped. The Owner authorizes only this failure
checkpoint and the design erratum, then commit. No later native, build, test, docs
validation or full-ring run was made; docs validation is deliberately deferred by
the stop instruction. Independent reviewers may inspect this frozen code-bearing
prototype, but must not join/admit it as a working Windows store.

## Source contract and implementation choice

Constants were checked against installed Microsoft SDK `winnt.h`, `winbase.h`,
`winternl.h`, `ntdef.h`, `minwinbase.h`, `accctrl.h` and shared `sddl.h`. Each declaration
in `WindowsNative.cs` carries the Microsoft Learn contract for its API/layout/flag
group. These declarations do not claim a supported user path, effective security
policy or compatible-reader overwrite until their corresponding tests pass.

- [Source-generated P/Invoke](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation): LibraryImport with unsafe enabled; SafeFileHandle and scoped LocalFree memory ownership.
- [NtCreateFile](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntcreatefile) and [OBJECT_ATTRIBUTES](https://learn.microsoft.com/en-us/windows/win32/api/ntdef/ns-ntdef-_object_attributes): retained root, one bounded component, no-reparse flags, creation descriptor.
- [NT rename layout and flags](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_file_rename_information) and [NT information classes](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ne-wdm-_file_information_class): flags1/2 and Ex class65; x64 sizeof24, root8, bytecount16, WCHAR20, allocated size24 plus UTF-16 bytes.
- [NtSetInformationFile user-mode naming/signature](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-zwsetinformationfile): direct ntdll LibraryImport, separate NTSTATUS and IO_STATUS_BLOCK; raw mapped Win32 captured. Never a path-based or C fallback.
- [Win32 rename structure](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_rename_info) and [SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle): independent fresh-fixture Win32-vs-NT comparison, not switching API on an existing failed operation.
- [POSIX disposition](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_file_disposition_information_ex): namespace removal follows deleting-handle close; observer remains readable.
- [Private security descriptor syntax](https://learn.microsoft.com/en-us/windows/win32/secauthz/security-descriptor-string-format) and [GetSecurityInfo](https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-getsecurityinfo): actual handle owner/DACL oracle before writes.
- [FILE_ID_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info): live-handle volume plus 128-bit identity, not a persisted lifetime identifier.

## Corrections for coordinator class → sweep → derive → prevent

1. **Native wrapper contract differs from native service.** Signature/ABI agreement
   alone did not establish rooted Win32 rename behavior. Sweep: both replace and
   no-replace failed 87. Derive: qualify the user-mode NT service independently.
   Control: `WindowsNative_Rename_FreshWin32VersusNtFixtures` retains the exact
   contrast on independent fixtures. No API failure selects a fallback.
2. **Deletion completion depends on the deleting-handle lifecycle.** An assertion
   before closing that handle tested an unsupported timing. Sweep: plain-delete
   baseline and first POSIX run both left the name present. Derive: set disposition,
   close only the deleting handle, retain observer. Control: the final claim-disposal
   case encodes that ordering and checks observer bytes.
3. **Sharing capability must be explicit in every reader oracle.** The stronger
   ShareRead-only fixture cannot establish the approved ShareRead\|ShareDelete
   contract. Sweep: existing store cases and incoming adapter reader roles need
   independent review. Derive: two distinct obligations, compatible-reader overwrite
   and incompatible-reader refusal. Control pending fresh track; the ShareRead-only
   failure remains visible and is not weakened. Register/audit writes are coordinator-owned.

## Exact Mac/Fable handoff and remaining approval

> Request a fresh bounded Windows qualification track, with a new repair budget,
> to prove overwrite while an approved FILE_SHARE_READ | FILE_SHARE_DELETE reader
> retains the old image and a fresh open reads the complete new image; separately
> prove deterministic refusal and incumbent preservation with an incompatible
> ShareRead-only reader. Preserve the current failing ShareRead-only probe and raw
> receipts. Review the measured rooted SetFileInformationByHandle Ex=22 Win32-87
> versus NtSetInformationFile Ex=65 result and affirm the direct managed NT service
> as the selected handle-relative primitive, with no path/C/error-triggered fallback.
> Keep Windows ProjectStore fail-closed until the adapter, all native/security/test
> cases and independent Data/Security/Test/Fable gates are cleared. Separately hand
> off exact shared UI paths/copy IDs for the local default folder, OneDrive refusal
> and visible crash recovery. Do not broaden the PC lease.

No repair budget, production admission or independent veto is implicitly reset by
this commit. `WindowsProjectStore.cs`, `tools/verify-windows-store.py` and the second
shared-test gate remain untouched/unimplemented. Darwin case bodies/PInvokes, helper,
MSBuild target, core/tests/tools/cases/audit/index/xmsg and shared UI are unchanged
except the explicitly listed leased qualification files.
