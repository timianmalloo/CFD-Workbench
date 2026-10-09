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
The already-installed user executable `%USERPROFILE%\.dotnet\dotnet.exe`
reports 10.0.203; its SDK list includes 10.0.203. System x64 SDKs are 9.0.315 and
10.0.301; an x86 executable also exists but was not used. The bounded command sets
`DOTNET_ROOT=%USERPROFILE%\.dotnet`, selects check prefixes with `CFD_TEST_ONLY`,
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
SDK mismatch. Behavioral-run stderr is empty. Native errors and assertion results
are preserved; the ACL RED retains the descriptor structure.
Ruling 145 proof redactions: machine SIDs use `S-1-5-21-<machine>-<RID>` and user paths use `%USERPROFILE%`.
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
this commit. At the time of this native qualification checkpoint,
`tools/verify-windows-store.py` and the second shared-test gate were
untouched/unimplemented. The bounded successor verifier and its evidence are
recorded below. Darwin case bodies/PInvokes, helper,
MSBuild target, core/tests/tools/cases/audit/index/xmsg and shared UI are unchanged
except the explicitly listed leased qualification files.

## Successor verifier evidence — 2026-10-09

`tools/verify-windows-store.py` is a bounded qualification runner, not production
admission. Its no-argument contract is exit 0 only for the exact Windows subset,
exit 1 for a Windows subject or verifier failure, and exit 4 with `NOT ASSESSED`
off Windows. Explicit `--self-test` verifies the exact registration/selection,
output parser failure cases, the off-Windows and Windows-failure exit distinction,
argument rejection, six-CPU affinity selection and bounded timeout calculation.
The external invocation below also demonstrates that a second argument is rejected
with exit 1. ProjectStore remains fail-closed.

The verifier parses every literal `Check` registration in
`WindowsProjectStoreTests.cs` and requires exactly the 13 names in the source
inventory. It rejects nonliteral registrations, duplicates, omissions, and any
unexpected registration, including an unscoped name that does not start with a
Windows prefix. The selector runs all 13 registrations. The 12 passing checks are:

1. `WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped`
2. `WindowsStore_Unqualified_ProductionAdmissionRemainsClosed`
3. `WindowsNative_Environment_RealNtfsX64`
4. `WindowsNative_Layouts_SdkX64`
5. `WindowsNative_RelativeCreate_PrivateAclBeforeAnyBytes`
6. `WindowsNative_Replace_ShareReadDeleteReaderKeepsOldImage`
7. `WindowsNative_Replace_ShareReadOnlyReaderRefused`
8. `WindowsNative_ClaimDispose_HeldObserverNamespaceRemoved`
9. `WindowsNative_Rename_FreshWin32VersusNtFixtures`
10. `WindowsNative_Rename_AttributionNullRootExAndRootedLegacy`
11. `WindowsNative_CreateOnly_CollisionPreservesBothObjects`
12. `WindowsNative_RelativeName_TraversalRefusedBeforeCreate`

`WindowsNative_Replace_HeldReaderKeepsOldImage` also runs. Ruling 152 (2) keeps
this unchanged historical probe in the qualification selector; the Windows
expected-failure classifier recognizes only its Ruling 145 `historical-probe`
failure. If it passes, the manifest checker reports `UNEXPECTED-PASS`; if any
other selected check fails, it reports `UNEXPECTED`. Either result makes this
verifier fail. The ShareRead|ShareDelete replacement and ShareRead-only refusal
remain separate checks with separate assertions.

### Correction to the first verifier commit

Commit `a9b1fed3` incorrectly omitted the historical probe from the selector.
The earlier reading treated Ruling 152's permitted ring classification as
permission to omit the test. That was wrong: the ruling keeps the probe running
and classifies its known failure. The 12-check logs `verify-windows-store*.txt`
from that commit are preserved as evidence of the defect, but their exit-0 result
is superseded and is not qualification evidence. A red self-test on this repair
track failed specifically because the selector omitted the Ruling 152 probe;
the new red capture is `verify-windows-store-r152-red.stdout.txt`.

The committed captures also exposed a byte-normalization defect: the retained
Windows worktree held 3,772 bytes for the initial stdout and 3,770 bytes for the
first stdout, while their committed blobs held 3,726 and 3,724 bytes. The new
`.gitattributes` rules disable text conversion for this proof directory's raw
stdout/stderr captures. `capture-integrity.py` plus `capture-manifest.json`
records every stdout/stderr capture in this proof directory with its byte count
and SHA-256 and compares worktree
bytes to the staged Git blob before delivery.

### Final verifier run

- Exact command: `py -3 tools\verify-windows-store.py`.
- Repository HEAD at run: `a9b1fed37b3cc39eb2f53c45fe031f691e964542`.
- Script SHA-256: `f257d9a2735f79fef1bd43cd695d326d5577cf9341c1ba4bb91e73fb0a5ad24e`.
- The committed script's final SHA-256 is `07912eb24e15064ba426ac97e1130b523ac1147f29c3e486a11c54a899508b6b`. It differs from the measured-run hash only by added self-test assertions for duplicate and missing registration inventory; the no-argument execution path was not changed after the captured run. The measured runtime above remains the authoritative timing and is not presented as a fresh measurement of the final file hash.
- SDK: pinned .NET `10.0.203`, resolved from the user-local SDK directory; the
  verifier set `DOTNET_ROOT` and prepended that directory to child `PATH`.
- OS: Windows 11 build `10.0.26300.0`, AMD64. Process affinity was restricted
  from prior mask `0xfffff` to `0x3f`, six logical CPUs, before child launch.
- Verifier UTC start/end: `2026-10-09T00:31:14.262892Z` /
  `2026-10-09T00:31:29.572262Z`; total measured duration: `15.309395` seconds.
- The Release test process used `-m:6`; its measured duration was `14.650950`
  seconds. The verifier's target is 15 seconds; this run exceeded that target by
  0.309395 seconds but remained below the 60-second hard ceiling. The run reports
  `TARGET_MET=false` and exits 0 only because the test result was exactly the
  Ruling 145 classified outcome.
- All 12 other names above emitted PASS. The historical probe emitted one FAIL
  with Win32 32. The harness reported `ran=13 skipped=694`,
  `RESULT failures=1`, and process exit 1. The separate classifier returned 0
  with exactly `EXPECTED-FAIL 1 (manifest)` and `UNEXPECTED 0`; verifier exit was
  0. Full stdout/stderr are `verify-windows-store-r152.stdout.txt` and
  `verify-windows-store-r152.stderr.txt`.
- The previous 13.820326-second run was the incorrect 12-case selector and is
  retained only as superseded evidence; it does not satisfy this qualification.
- `verify-windows-store-r152-selftest.stdout.txt` records passing checks for
  the 13-case selector/classifier contract, wrong or stale classifications,
  all-literal inventory checks, timeout cleanup and exit behavior. Its separate
  stderr is empty. The earlier parser red evidence also remains preserved.
- `py -3 tools\verify-windows-store.py --unexpected` was rejected with exit 1;
  the earlier captures remain `verify-windows-store-args.stdout.txt` and
  `verify-windows-store-args.stderr.txt`.
- `py -3 tools\check-expected-failures.py --self-test` passed all 10 synthetic
  classifier cases and loaded the 32-entry Windows manifest. The previous
  status-0 classification of the invalid 12-check capture remains in
  `manifest-check.stdout.txt` but is superseded. The corrected captured run was
  classified with `--status 1 --host windows`; it returned the single Ruling 145
  expected failure and `UNEXPECTED 0`, exit 0. Outputs are in
  `manifest-check-r152.stdout.txt` and `manifest-check-r152.stderr.txt`.
- `py -3 -m py_compile tools\verify-windows-store.py docs\proof\win-store-implementation\capture-integrity.py`
  passed (exit 0; `static-check.stdout.txt`). `py -3 tools\check-proof-pii.py`
  passed with 0 Windows home paths or machine SIDs (`pii-check.stdout.txt`).
  The staged `git diff --check` passed with the byte-exact stdout/stderr capture
  paths excluded; those paths are validated separately by the byte/hash check.
- `py -3 tools\check-docs.py` passed (exit 0; separate captures are
  `check-docs.stdout.txt` and `check-docs.stderr.txt`). It ran with affinity
  `0x3f` (six CPUs). Documentation freshness reported 131 review-suggested
  findings and 0 stale findings; the configured warning threshold did not fail
  the gate. The check completed with `Documentation checks passed.`
- `py -3 docs\proof\win-store-implementation\capture-integrity.py --record`
  records the byte count and SHA-256 of every stdout/stderr capture in this proof
  directory. After staging, the no-argument `capture-integrity.py` check compares
  every worktree file against its staged blob; the output is
  `capture-integrity-check.txt`.

Each invocation preserves stdout and stderr separately. Verifier output replaces
home paths with `%USERPROFILE%` and redacts machine SIDs before emission; no
account path or SID is committed. No product source/tests, readiness wiring or
full test ring was changed or run.

### Ruling 171 — bounded Windows pipe cleanup

The Ruling 171 regression probe runs on Windows with a real child process whose
stdout and stderr are anonymous pipes. The child starts a descendant that keeps
both inherited pipe handles open. The fixture injects `taskkill.exe` exit 5,
waits for the absolute work deadline created before target process launch to
expire, and observes timeout cleanup. The ceiling begins at `PROCESS_STARTED`,
before `main` dispatches the verifier. Cleanup has its own two-second absolute
bound inside the five-second hard-ceiling reserve.

- Red-first command: `py -3 tools\verify-windows-store.py --self-test`.
  The old synchronous `stream.close()` on the timeout thread blocked for
  `2.016905` seconds after the expired work deadline. The real-pipe regression
  failed; its taskkill injection was exit 5. The test then terminated its own
  fixture descendant so the red run left no test process behind. Captures:
  `r171-cleanup-red.stdout.txt` and `r171-cleanup-red.stderr.txt`.
- Green command: `py -3 tools\verify-windows-store.py --self-test`.
  Final cleanup returned in `1.058140` seconds after the expired work deadline, with
  injected taskkill exit 5. The verifier returned exit 125 as required and
  explicitly reported the residual state: root PID and exit, descendant
  termination unverified, and pipe close pending off the ceiling thread. The
  fixture then terminated its descendant and confirmed the deferred close
  worker finished. The real-pipe regression and all existing verifier contract
  self-tests passed. Captures: `r171-cleanup-green.stdout.txt` and
  `r171-cleanup-green.stderr.txt`.
- Final verifier script SHA-256 for the Ruling 171 cleanup pass: `7319b2f138b397056a1691496b340a852881d08f930df93c581ab83eb97d9112`.
- The production cleanup uses bounded waits under one absolute cleanup deadline.
  Stream close runs on a daemon cleanup worker so it cannot block the ceiling
  thread. A pending close or unverified descendant is reported as
  `TREE_CLEANUP_FAILED`; it is never counted as successful cleanup.
- No native qualification rerun or full test ring was run. The existing 13-test
  selector and Ruling 145 classification code were left unchanged.

### Ruling 171 cycle 2 — whole-invocation timing

The first R171 commit returned from `--self-test`, invalid-argument, and
NOT-ASSESSED paths before its Windows-only timing `finally` block. The repair
moves all dispatches behind one outer timing boundary. `PROCESS_STARTED` is
captured immediately after importing `time`, before the remaining module imports;
the wrapper always prints exactly one `TOTAL_WALL_SECONDS=<seconds>` line and
returns the intended 0, 1, or 4 result while under 60 seconds. It returns 1 when
elapsed time exceeds the hard ceiling, including when the dispatched path would
otherwise return 0 or 4. Exceptions and `SystemExit` are converted to an exit
code and still pass through the same finalizer.

- Exact command: `py -3 tools\verify-windows-store.py --self-test`.
- Final process total: `2.312432` seconds from module-start clock; exactly one
  `TOTAL_WALL_SECONDS=2.312432` line was emitted. The real-pipe taskkill-failure
  cleanup took `1.055106` seconds after its expired work deadline. The full
  self-test exited 0, including 0/1/4 early-result preservation, exception and
  `SystemExit` paths, synthetic over-60-second rejection, actual invalid-argument
  and off-Windows wrapper checks, and the R171 real-pipe regression. Captures:
  `r171-cycle2-selftest.stdout.txt` and `r171-cycle2-selftest.stderr.txt`.
- `py -3 -m py_compile tools\verify-windows-store.py` passed. The final
  `py -3 tools\check-docs.py` run passed under six-CPU affinity; proof PII scan
  found 0 Windows home paths or machine SIDs. Capture integrity and staged diff
  checks are recorded separately in the proof outputs and manifest.
- Final script SHA-256: `92151a8f72c602180c407240e6e10c28acdfff2dea8bb0acd988deda617205ee`.
- No no-argument native qualification run or full ring was performed.
