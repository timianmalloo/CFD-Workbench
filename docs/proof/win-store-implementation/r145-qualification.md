---
id: proof-windows-store-r145-qualification
title: Ruling 145 Windows native sharing and rename attribution qualification
type: doc
status: in-review
owner: "@windows-worker"
phase: implementation
tags: [windows, persistence, proof, blocked]
links:
  - { to: design-windows-native-store, rel: depends-on }
  - { to: proof-windows-store-implementation, rel: refines }
  - { to: receipt-windows-store-implementation, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  The approved sharing behavior is characterized on real NTFS, the missing stable
  sharing-conflict code is observed red then corrected, and independent Win32
  probes attribute error 87 to the tested rooted forms. Production admission and
  full qualification remain blocked; the required docs gate fails outside the lease.
---

# Ruling 145 qualification receipt

## Goal and disposition

Goal: produce the smallest reviewable Ruling 145 native qualification change.
Done when: the two distinct sharing cases, Win32-87 attribution, stable Win32-32
mapping, fail-closed admission, focused tests and gates have exact evidence.
Tier T1; fan-out cap 1; repair cap 2. No subagents or test ring ran.

**Disposition: BLOCKED for complete Ruling 145 qualification.** The compatible
sharing case was already green before the change. The incompatible sharing
invariants also held before the change; only its missing product-code assertion
was red. Under the coordinator's binding Owner direction, class 65 was not
regressed to manufacture red evidence. These are explicitly characterization
results, not two behavior repairs observed red-to-green. The Owner must disposition
the literal two-case red-first requirement. The repository docs gate also fails on
an unleased store-subset guard. No third repair is authorized or performed.

The source/test/design commit is
`391cda925cc5702a01ceb65faa75cb19990d437b`, on `win/store-b2-r145`, based at
`dfbef2fc869d7e0d6b07c1ebc87f778e7f48673f`. The final evidence-only commit is supplied
in the coordinator handback, avoiding a self-referential SHA. All final native
claims below were rerun against the exact source/test/design commit; subsequent
changes are proof files only.

The lease reaches only `WindowsNative.cs`, `WindowsProjectStoreTests.cs`, design
section 4, and this proof directory. `ProjectStore.cs`, reader share flags, UI,
default paths, OneDrive detection, shared tools/schema and Mac native code are
unchanged. Product admission remains false. There is no Windows adapter in this
change and no production save success claim.

## Contracts, surfaces and test selection

Ruling 145 requires replacement with `ShareRead | ShareDelete` to keep the held old
image while new opens see the new image. A `ShareRead`-only reader must refuse with
Win32 32 / NTSTATUS `0xc0000043`, preserve incumbent and temp, and expose a stable
product code. `NativeFailure.ProductCode` now maps 32 to `DOC-CONFLICT`; the future
adapter must preserve it. Other mappings are not newly qualified here.

Surface list: native rename completion/raw failure → `NativeFailure.ProductCode`
→ real native test consumer. Design section 4 records the code contract. The
product facade is the separate fail-closed consumer, verified through its real
`SaveAsync` entry point. Model, schema, persisted bytes, UI and compute readers
are unchanged; rendering and cross-surface product save proof remain outside this
qualification and are not asserted.

Testing Strategy union: D0 deterministic fixture cleanup and exact assertions;
D1 stable failure branch; D4 real filesystem/native handles. Existing D2 bounded
child-name, ABI layout, private descriptor, claim disposition and collision cases
are retained in the focused set. No I/O mock or new dependency is introduced.
The existing OS boundary, LibraryImport, SafeHandle and console harness are reused.

Failure analysis: compatible replacement must preserve the old reader and publish
the complete new object; incompatible sharing must preserve both names/objects;
wrong error mapping must fail the assertion; each attribution probe must use its
own fresh fixture and preserve bytes/identity across success or refusal. No
error-triggered API switch, retry, path mutation fallback or C fallback exists.
The production `Rename` entry point still selects only
`NtSetInformationFile(FileRenameInformationEx=65)`; Win32 classes 22 and 3 are
explicit qualification-only calls, never recovery from an NT error.

## Execution graph and bounded repairs

Serial graph: ground the ruled contracts → freeze two new sharing tests → observe
the actual initial result → cycle 1 mapping/probes → cycle 2 exact probe oracle →
freeze/commit → exact-HEAD focused verification → docs gate → evidence handback.
Edges are data/decision dependencies; no independent agent work is dispatched.
The variant is the number of unmet owned oracles: missing product code, then
unlocked attribution statuses. It reached zero in two cycles. The cap is a
circuit breaker: further defects or unleased gates are reported, not repaired.
Native behavior itself was not modified to force a red. Planned and actual width
are 1; actual repairs are 2; full-ring count is 0. Durations below are measured
whole-command wall time, not native-call latency or inferred cost savings.

| Run | Exact result | Exit | Measured seconds |
|---|---|---:|---:|
| Initial frozen sharing tests | Compatible CHARACTERIZATION GREEN; incompatible native invariants held, product-code assertion RED | 1 | 10.3391755 |
| Cycle 1 | Mapping added and probes measured; 11 focused PASS, 0 failures | 0 | 4.8316864 |
| Cycle 2 | Exact attribution oracles locked to observed 0/87; 11 focused PASS, 0 failures | 0 | 3.8445932 |
| Exact committed HEAD | 11 focused PASS, 0 failures; 687 registrations outside this subset | 0 | 0.701014 |
| Required docs gate | STORE-SUBSET failure before later checks | 1 | 0.2528742 |

Initial red tests were run before mapping/design correction. At that point the
product-code member did not exist; the test used reflection solely to compile
the absent-member assertion. The observed failure is preserved verbatim in
`r145-initial.stdout.txt`: expected `DOC-CONFLICT`, actual blank. After adding the
member, the assertion became a direct typed `refusal.ProductCode` read.

Cycle 1 adds the two isolated Win32 attribution entry points and product-code
mapping, with no change to class-65 selection or flags. Cycle 2 changes only the
probe tests to require the just-observed exact Win32 statuses and absent NT/IO
statuses. Source/tests are frozen after cycle 2; no third code/evidence repair ran.

## Exact commands and environment

Observed SDK: 10.0.203 from `%USERPROFILE%\.dotnet\dotnet.exe`; `global.json`
pins 10.0.203 with roll-forward disabled. Host: Windows 10.0.26300.0, X64, local
NTFS, from `WindowsNative_Environment_RealNtfsX64`. `DOTNET_PROCESSOR_COUNT=4`
and MSBuild `-m:4` bound the requested compute concurrency; no test processes ran
in parallel. The final native invocation reuses the Release build from cycle 2.

```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:DOTNET_PROCESSOR_COUNT = '4'
$env:CFD_TEST_ONLY = 'WindowsNative_Replace_ShareReadDeleteReaderKeepsOldImage,WindowsNative_Replace_ShareReadOnlyReaderRefused'
& "$env:USERPROFILE\.dotnet\dotnet.exe" run -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj -m:4
```

That initial command produced `r145-initial.stdout.txt` and `.stderr.txt`. Both
cycle commands use the following selector and command, producing the matching
`r145-cycle-1` and `r145-cycle-2` stdout/stderr pairs:

```powershell
$env:CFD_TEST_ONLY = 'WindowsStore_,WindowsNative_Environment_,WindowsNative_Layouts_,WindowsNative_RelativeCreate_,WindowsNative_Replace_ShareRead,WindowsNative_ClaimDispose_,WindowsNative_Rename_,WindowsNative_CreateOnly_,WindowsNative_RelativeName_'
& "$env:USERPROFILE\.dotnet\dotnet.exe" run -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj -m:4
```

At exact HEAD `391cda925cc5702a01ceb65faa75cb19990d437b`, with those same environment
variables and focused selector:

```powershell
git rev-parse HEAD
& "$env:USERPROFILE\.dotnet\dotnet.exe" run --no-build -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj -m:4
py -3 tools/check-docs.py
```

These commands produced `r145-head` and `r145-check-docs` stdout/stderr pairs.
For every command, stdout and stderr were redirected separately; `$LASTEXITCODE`
was captured immediately, outside a pipeline. Native stderr is empty. Docs stdout
contains the join-ring policy message; docs stderr contains the STORE-SUBSET failure.
The docs command exited 1 before subsequent gates. Test costs belong to Windows native
qualification/readiness; these cases are not promoted to a fast-ring budget here.

## Proof pack

| Claim and source | Observed evidence / oracle | Red observed | Confidence / limit |
|---|---|---|---|
| Compatible reader keeps old image; fresh reader gets new image; temp name removed (`WindowsProjectStoreTests.cs:58`) | `r145-head.stdout.txt`: NT/IO/Win32 zero; held bytes 0101, new bytes 020202; old/new IDs differ, held ID unchanged, temp absent; exact assertions reject any wrong identity/bytes/name | No: initial CHARACTERIZATION GREEN | Verified on this host; Owner red-first disposition required |
| Incompatible reader preserves incumbent/temp (`WindowsProjectStoreTests.cs:83`) | NTSTATUS c0000043, IO_STATUS 0, Win32 32; held/fresh incumbent 0101 and same ID; temp 020202 and same ID; both names present | Native invariants already held before correction | Verified on this host; not a new native behavior repair |
| Sharing failure maps to DOC-CONFLICT (`WindowsNative.cs:260`, design section 4) | Initial absent-member assertion fails; cycle 1/2 and exact-HEAD typed assertion pass after real native refusal | Yes: missing member yielded blank rather than required DOC-CONFLICT | Verified at owned NativeFailure boundary; future adapter not implemented |
| Win32 Ex22 NULL root / absolute DOS path succeeds (`WindowsProjectStoreTests.cs:179`) | Win32 0; source name gone, destination present; bytes 0405 and original source ID preserved | No: fresh execution spike and exact observed oracle | Verified attribution on measured host only |
| Win32 class3 rooted relative refuses | Win32 87, NT/IO not recorded; source present, destination absent; original bytes/ID preserved | No: fresh execution spike and exact observed oracle | Verified attribution on measured host only |
| Rooted Win32 Ex22 differs from rooted NT65 | Existing fresh-fixture comparison observes Ex22 error87 and NT65 success0 | Prior failed primitive receipts retained | Verified; identifies tested rooted Win32 forms, not an undocumented OS internal cause |
| Class65 is the only store rename primitive (`WindowsNative.cs:167`, `:209`) | Direct production entry-point source review: NtRelative dispatch; no failure catch/retry/switch | Historical Ex22 failures remain recorded | Verified source shape; independent reviewer owns final veto |
| Windows production admission remains false | Real `ProjectStore.SaveAsync` returns DOC-UNSUPPORTED-PERSISTENCE, publication/durability false, empty fixture | Earlier `red-store.stdout.txt` retained | Verified; does not qualify a Windows adapter |

The exact-HEAD IDs and bytes are in raw output beside this file. A held file ID is
an observed fixture identity, not a persisted project ID or lifetime uniqueness
promise. Win32 success probes mark NTSTATUS/IO_STATUS **not recorded**; they do
not manufacture zero NT statuses. The root-null probe succeeds with the same ABI
layout and source access, and both rooted Win32 forms fail 87. This supports the
bounded attribution to those forms on this build, not a universal Windows guarantee.
Microsoft source contracts:
[FILE_RENAME_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_rename_info),
[SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle).

## Preserved evidence, gate failures and handoff

The historical `WindowsNative_Replace_HeldReaderKeepsOldImage` body is byte-equivalent
after line-ending normalization to the assigned base; a direct extraction
comparison returned `HISTORICAL_PROBE_UNCHANGED=True`. Prior `red-native`, cycle-1
and cycle-2 receipts have no diff from the assigned base. This preserved failing
probe is deliberately outside the passing focused selector. A broad
`WindowsNative_` selection still includes it and is not claimed green.

Ruling 145's prior proof redaction was already present at the assigned base:
SIDs use `S-1-5-21-<machine>-<RID>` and home paths use `%USERPROFILE%`.
No further redaction was needed in these new native outputs. A recursive scan of
this proof directory for unredacted machine SIDs and Windows user home paths
found no matches. Historical raw evidence stays unchanged.

`py -3 tools/check-docs.py` fails at STORE-SUBSET because
`WindowsProjectStoreTests.cs` is outside the four allowed umask-sensitive store
test files. The source/test subset must be reconciled in shared tooling by the
Owner; no unleased tool edits occurred. This receipt also needs coordinator graph
derivation and inbound review propagation. Later docs gates, full ring, readiness
and independent Data/Security/Test/Fable admission gates are not claimed passed.

`git diff --check` before the initial commit reported one trailing space in the
verbatim initial raw failure line after `actual`. That check was not passed; raw
evidence is retained unchanged. The subsequent commit command proceeded, so this
is an explicit gate-handling correction for the coordinator's defect-class handoff:
always inspect a gate result before a dependent mutation, including sequential
shell commands. No code or evidence repair was attempted after the cap. The commit
hook reported AGENT_SESSION unset and advisory-only checking; no automated lease
enforcement claim is made. Owned paths were reviewed manually.

Coordinator owns audit close, graph derivation, shared tool seam and register
updates. The permitted ignored audit marker was started for session
`win-store-b2-r145`, skill `implement`, at `2026-10-08T15:29:01Z`; no authored audit,
index, ruling, register or xmsg output was written by this worker.

| Status | Result |
|---|---|
| Completed | Frozen owned qualification implementation; real initial mapping RED; 11 focused PASS at exact committed HEAD; class22/class3 attribution; historical probe and evidence preserved |
| Remaining | Literal two-sharing-case red-first disposition, STORE-SUBSET shared-tool repair, graph/audit close, full required test/security/admission matrix and independent vetoes |
| Best next action | Owner review and explicit disposition of the characterized sharing evidence and unleased docs-gate seam; keep production admission false |
