---
id: coordination-windows-runtime-route
title: Windows x64 M1 runtime route and ownership packet
type: plan
status: in-progress
owner: "@cfd-coordinator-20260923"
phase: application-foundation
tags: [coordination, application, windows, runtime]
links:
  - { to: note-m1-scope-decision, rel: depends-on }
  - { to: coordination-application-build, rel: depends-on }
  - { to: coordination-contract-c-native, rel: relates-to }
  - { to: architecture-application, rel: relates-to }
  - { to: spec-cfd-workbench-v1, rel: relates-to }
review-by: "2026-10-25"
summary: >-
  Route real Windows x64 M1 execution through an unattended hosted runner and a
  separately qualified interactive desktop, with distinct store, adapter and
  independent proof ownership. A hosted Windows job ran, but its native
  qualification failed; interactive and product gates remain open.
review-suggested:
  - { by: coordination-application-build, on: 2026-09-24, reason: "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims" }
---

# Windows x64 M1 runtime route

**Goal ·** Fulfil the [user-approved Windows-in-M1 decision](../notes/m1-scope-decision.md)
without relabelling a cross-published binary as a Windows run. **Done when ·**
Windows x64 executable, native persistence and UI workflows pass their named
oracles on Windows, including independent accessibility and file review.
**Not in this packet ·** source changes, CI dispatch, provisioning a desktop,
M1.1 section editing, and a change to the visible timing or security gates.
**Tier ·** T2 platform seam. **Fan-out cap ·** one Windows implementation author
beside root and Coordinator after the active C repair yields; an Owner ruling
must freeze paths and budget first.

## Checked starting point

The accepted source on 2026-09-25 supports macOS arm64 persistence. The
`ProjectStore.Supported` guard at `src/CfdWorkbench.Persistence/ProjectStore.cs:173`
refuses other platforms with `DOC-UNSUPPORTED-PERSISTENCE`; its native calls
bind a macOS helper and `libSystem.B.dylib`. The persistence project invokes
`xcrun clang -dynamiclib -arch arm64` in
`src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj:14`.
`tools/verify-application-core.py:150` exits on Windows before a child launch;
the isolated C `tools/verify-application-adapters.py:591` does likewise.
The isolated C package script has a Windows copy branch, and its macOS gate
cross-publishes `win-x64`, but neither is Windows-host execution.

The [scope inventory](../notes/m1-scope-decision.md#current-evidence-and-concrete-windows-routes)
found an Actions-enabled public repository, no registered repository runner,
and only the Ubuntu `docs-health` workflow. It found no configured local Windows
VM or interactive route within its stated search boundary. [GitHub's hosted
runner reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)
lists x64 Windows runner images, including `windows-2022` and `windows-2025`.
This establishes a concrete *candidate* for execution, not a scheduled job or
qualified UIA/desktop capability. W0 must measure whether the hosted session
can support those interactive checks before a separate host is assumed. The inventory did not inspect
credentials or every possible external host. Windows x64 remains **Not assessed**.

## Dependency graph and ownership

| Node | Entry condition and owner | Exit evidence |
|---|---|---|
| W0 · route/contract spike | Coordinator records this packet; Owner rules exact scope and independent Data/Security/Test gates. Read Windows handle, reparse, sharing, durability and process-lifecycle APIs before selecting a native implementation. | Typed Windows store/process contract and failure oracle. No guessed `DllImport` constants or path-only fallback. |
| W1 · unattended host | After W0, tooling author owns a new exact Windows Actions workflow and Windows-aware verifier process observer/cleanup. Pin one x64 runner image and toolchain; retain source, SDK, binary, fixture, child/descendant and failure receipts. | Actual hosted Windows x64 job starts, runs and cleans its own processes. A checkout or cross-publish alone is insufficient. |
| W2 · product runtime | After W0, a distinct persistence author owns native Windows file semantics; adapter author takes CLI/Desktop platform seams only after the current C work and B contract are frozen. Implement red-first file, fault, conflict, symlink/reparse, identity, recovery and native UI cases. | Packaged CLI and GUI execute on W1, save/reopen exact source and accepted/history/recovery identities, retain original files, and pass negative store/security tests. Unsupported platform or missing helper fails clearly. |
| W3 · interactive desktop | Independently qualify an actual Windows x64 graphical session. None was verified in the bounded current inventory. W0 must first inspect the hosted runner session/automation capability rather than assume it cannot provide any UIA or graphical evidence. Root/Test/UX review real native Open/Save, keyboard, UIA/Narrator, focus, recovery, contrast and failure states. | Source-bound app/host/session receipts, independent UIA/Narrator and rendered workflow evidence; failures and unsupported states retained. Reference-device display timing remains a separate gate. |
| W4 · integrated gate | Depends on W1–W3, native C acceptance, and the separate visible-presentation track. Coordinator runs the supported conductor only after root and Owner gates. | Integrated two-platform M1 evidence. Any missing W3 or visible endpoint keeps M1 open. |

W1 and the interactive-route qualification can progress independently after W0.
W2 depends on measured native semantics and need not wait for a desktop before
red-first store work. W3 cannot be marked green by W1. The loop variant is the
set of unresolved W0–W3 exit oracles; a failed or unavailable host remains a
named blocker, not a reason to remove the gate.

For any future workflow launch, pin the candidate workflow bytes and run a
semantic GitHub Actions check with `actionlint` before push. The R43 original
failed that check on three unavailable `runner.temp` job-level contexts;
R44's exact step-level correction passed. YAML syntax, source review and a
green `check-docs.py` do not exercise context availability. A remote run
remains necessary to observe actual hosted behavior. This route rule is the
recurrence control for [CI-ACTIONS-CONTEXT](../lessons/defect-classes.md), not
permission for another W1 push: Ruling 44 exhausted its two pushes.

**Candidate exact-path ownership for the Owner to freeze, not an assignment:**

| Boundary | Candidate paths | Exclusions and review |
|---|---|---|
| Persistence/security | `src/CfdWorkbench.Persistence/ProjectStore.cs`, `src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj`, a proposed Windows native-helper path after API spike, `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs`, `tools/verify-application-core.py`, `docs/proof/application-core.md` | Preserve macOS measured branch and accepted file format; independent Data/Security/Test veto. Do not edit the four active R39 C paths. |
| Adapter/package | `src/CfdWorkbench.Desktop/`, `src/CfdWorkbench.Cli/`, `tests/CfdWorkbench.Desktop.Tests/`, `tools/verify-application-adapters.py`, `tools/package-application.py`, `docs/proof/application-adapters.md` | Freeze individual files after C R39 handback and B API contract; never acquire a directory lease over another author. Independent native UX/Test review. |
| Windows CI | Ruling 40 freezes `.github/workflows/application-windows-qualification.yml` and `tools/qualify-windows-runtime.py` for W0; later production CI paths require a new ruling. | No GUI or UIA claim before the hosted session is measured. CI alone cannot establish the specified reference-device display timing. |

The complete file list is deliberately **candidate**: W0 must identify the
minimal Win32/PInvoke seam and actual fixture consumers before an implementation
lease. A new native file name or workflow is not authoritative until then.
**Ruling 40** admits one bounded W0 packet: requested-Astra isolated author,
≤70 calls/45 minutes, exactly `docs/design/windows-runtime.md`,
`docs/proof/windows-runtime.md`, `tools/spikes/WindowsRuntime/WindowsRuntime.csproj`,
`tools/spikes/WindowsRuntime/Program.cs`, `tools/qualify-windows-runtime.py`, and
`.github/workflows/application-windows-qualification.yml`. This is local
contract/executable/workflow preparation; no branch push, CI dispatch,
production store/C edit, or Windows-runtime pass. Root independently reviews
Data, Security and Test before a concrete W1 dispatch decision. No routine
human foreground or credential request follows from the present lack of a host.

## Required failure and success controls

- A Windows build without an executed `.exe` and actual native file operations
  fails the W1/W2 gate. The current macOS `DOC-UNSUPPORTED-PERSISTENCE` path is a
  truthful pre-implementation result, not a Windows pass.
- For every child process, record PID/start/executable and exact descendant
  cleanup; unsupported observation exits **Not assessed** before a misleading
  green result. Retain stdout/stderr and source/binary hashes in the receipt.
- Exercise create, overwrite, reopen, conflict, cancellation, injected write/
  replace failure, permission denial, symlink/reparse and wrong-directory
  negatives against the chosen Windows file model. No post-create ACL patch
  may mask an unverified creation boundary.
- Compare packaged Windows CLI and GUI source, surface, revision and recovery
  identity with the accepted macOS fixture set. Windows UIA/Narrator and native
  file-picker behavior require W3; a toolkit tree alone is not that proof.
- W0 receipt validation asserts each named case's exact before/after target,
  owned temporary/claim, and unrelated foreign bytes plus publication and
  durability state. The self-test mutates conflict preservation,
  cancel-before absence, write-fault cleanup and cleanup-refusal foreign
  retention individually; each wrong result must fail even if other rows are
  Not assessed. Hash authored inputs both before and after build/run and
  reject source drift. These are executable controls for [EVID-WIN-STATE](../lessons/defect-classes.md),
  not substitutes for a Windows-native run.
- Before a tooling author hands off a changed qualifier or spike, retain actual
  passing `verify-portable-text-io.py` and `verify-subprocess-utf8.py` receipts
  alongside its local controls. `check-docs.py` does not exercise those gates.
  Test a printing CLI's `--help` in a bounded child with its real exit behavior;
  an intercepted `sys.exit` can continue into qualification work.
- Keep the [visible-timing obligation](../notes/m1-scope-decision.md#what-honest-screen-timing-would-require)
  independent. A hosted runner or compositor-batch number cannot establish the
  specified reference laptop's final displayed frame.

**Current disposition:** W0 preparation is joined after root's independent
[Data/Security/Test review](../reviews/windows-runtime.md). The original local
source-bound packet had 35 wrong-result controls and macOS `Not assessed`
refusals; Ruling 42 corrected five portable-text findings in the qualifier and
the integrated 11/11 gate passed. The old native receipt does not bind the
corrected qualifier. Rulings 43–44 sent the disposable W1 branch twice. The
first run failed workflow validation before a job existed; the corrected
second run [actually executed on Windows x64](../proof/windows-runtime.md#w1-hosted-execution-2026-09-25)
and retained 26 native rows, with 21 Pass, four Fail and one Not assessed.
Its validator also rejected the native private-DACL row's compact SID alias.
The job ended failure; UIA was not reached. Ruling 44 permits no further push
or rerun. The hosted route is established, while Windows qualification, W2
product runtime and W3 interactive proof remain open. Root's source-bound R39 High Contrast native
regression passed on the repaired isolated C package; Windows, displayed
timing, broader assistive technology and M1 acceptance remain open. No C or M1
production join follows from this document.
