---
id: proof-windows-runtime
title: Windows W0 preparation and failed W1 hosted qualification
type: proof-pack
status: in-review
owner: "@cfd-windows-w0-20260925"
phase: application-foundation
tags: [windows, proof, qualification, native]
links:
  - { to: design-windows-runtime, rel: documents }
  - { to: coordination-windows-runtime-route, rel: relates-to }
review-by: 2026-10-25
summary: >-
  Records local W0 preparation and an actual but failed W1 Windows x64 native
  qualification. Independent review rejected production acceptance; UIA,
  directory durability and product runtime remain open.
review-suggested:
  - { by: coordination-windows-runtime-route, on: 2026-09-24, reason: "R43-R44 hosted route executed with failed native cases and a DACL receipt refusal; review route dependencies." }
---

# W0 proof (historical preparation checkpoint)

**Prepared:** a candidate contract, standalone net10.0 native experiment, Python
receipt/ownership driver and manual-only workflow. **Compiled:** local macOS arm64
with pinned SDK 10.0.203. **Executed:** local receipt negatives and real non-Windows
refusal. **At this W0 checkpoint, Not assessed:** every Windows native API
scenario, Windows job cleanup, hosted graphical/UIA session, Narrator and
displayed timing. Later hosted evidence and root's independent failure review
are recorded below; this author clears no veto.

Goal/done-when/T2/fan-out and the fixed six-path boundary are in the linked design.
The session began at `2026-09-25T04:08:19Z`, base
`ed0d070ff87dc1f0485e1ce0d3341869f54c4d62`, branch
`feature/windows-runtime-qualification`, isolated checkout
`/Users/mallalieut/projects/CFD-Workbench-feature-windows-runtime-qualification`.
`coord doctor` observed effective merge drivers, leader epoch 17 and no overlapping
leases before editing. Each authored file was then claimed separately.

## Evidence ledger

| Claim | Oracle and observed result | Red observed / confidence / limit |
|---|---|---|
| Parser must exist before a usable receipt contract | First `--self-test` exited 1 at `NotImplementedError: red-first receipt contract` | Verified RED in tool output; no native implication |
| Missing/duplicate cases and altered source/binary cannot qualify | Self-test rejects each altered synthetic row set | Executed wrong-result negatives; Verified only for the exercised parser behavior |
| Unsupported cannot become Pass | Change status of unsupported synthetic row → rejection | Executed mutation; native unsupported rows remain separate |
| Wrong bytes/publication/durability cannot qualify | Alter independent expected fixture hash, publication bit, or durability flag → rejection | Executed mutation; no filesystem atomicity claim |
| Live/unseen tree cannot qualify | Quiescent=false or fewer than three identified processes → rejection | Executed parser negatives; actual Windows job API remains Not assessed |
| Standalone project compiles against repo SDK | First build succeeded, zero warnings/errors; build stdout retained | Verified local compile; ABI execution unverified |
| Non-Windows refuses native qualification | Real apphost on macOS 27 arm64 emitted all 26 cases Not assessed, exit 3 | Verified real composition-root refusal; no Windows pass |
| Local child processes ended | Build and native process-group probes recorded quiescent=true | Verified local group absence; not Windows descendant identity evidence |
| Existing outputs cannot be reused | Driver uses exclusive root creation, no recursive removal | Prepared negative control; final execution receipt recorded below |

## Raw local receipts

The initial compile/refusal run is retained at `/tmp/cfd-w0-local-20260925` and the
next executable-set-binding run at `/tmp/cfd-w0-local-final-20260925`. They are
historical build iterations, **not the final source hash**. Each contains
`source.json`, SDK/build/native stdout.txt, stderr.txt, process.json, synthetic
fixture-a.bin/fixture-b.bin, compiled artifacts and summary.json. Do not pair an
old binary with newer source. The final bound run is recorded after the last edit.

Initial RED output (retained verbatim from execution):

```text
NotImplementedError: red-first receipt contract
exit: 1
```

Second build raw stdout:

```text
  Determining projects to restore...
  Restored /Users/mallalieut/projects/CFD-Workbench-feature-windows-runtime-qualification/tools/spikes/WindowsRuntime/WindowsRuntime.csproj (in 23 ms).
  WindowsRuntime -> /tmp/cfd-w0-local-final-20260925/artifacts/bin/WindowsRuntime/release/WindowsRuntime.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.30
```

Second run process evidence: build PID 69870, exit 0, timeout false,
quiescent true, elapsed 1.4258694580057636 seconds; native PID 69872, exit 3,
timeout false, quiescent true, elapsed 0.3445587500464171 seconds. These are
observed runtime measurements, not a performance budget or Windows timing.

## Test directives, floors and residual risk

Testing Strategy triggers: T1/D1 receipt decision mutations; T2/D2 bounded
case-set/schema and path rejection; T3/D3 standalone dependency boundary; T4/D4
real filesystem/native execution; T7/D6 structured JSONL/retained fixture contract.
D0 uses deterministic fixture values; generated run directory names only isolate
effects. T4 native behavior cannot execute on this host and is explicitly
**unmet**, not substituted with mocks. No product UI or generated-image surface
was authored, so UI craft tests are not applicable to this packet.

The candidate still needs actual Windows ABI, DACL effective access, hostile
namespace/race, junction, conflict/concurrent writer, cancellation/failure and
job-tree execution. File-content flush cannot satisfy directory durability.
The existing `IProjectStore` remains unchanged and unsupported on Windows.
Product `/2` identity/history/recovery round trips remain W2 work; binary fixtures
here are not a substitute. No PASS follows from documentation, a compile, a
nonempty receipt or a successful parser self-test.

Data/Security/Test admission is **pending independent root review**. No shared
security register was edited; new threat reasoning stays in the authorized design
for the designated register owner. Findings and unresolved decisions are named
there. The packet has no permission to resolve them by expanding production scope.

## Final source-bound local run

Final executable source/workflow was frozen for this run at
`/tmp/cfd-w0-final-proof-20260925`. The driver recorded the same five source hashes
before build and after all execution. SDK `--version` and `--info` are retained.
macOS 27 arm64 is the observed host; filesystem qualification is Not assessed on
this non-Windows branch. Every one of the 26 native rows has
`status="Not assessed"`, `code="W0-UNSUPPORTED-HOST"`, publication false and
durability false. Native stdout is retained unchanged in `native/stdout.txt`.

| Bound item | SHA-256 |
|---|---|
| Source manifest digest | `b6115c703aa1cf69d86da5b43ce9c7ce9bbce240844d6f8883c5e6cfa1747200` |
| `global.json` | `6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df` |
| `WindowsRuntime.csproj` | `495225c72823bbf7a53a54028d1180a89a10805e69db034cee0a318cdf474c8f` |
| `Program.cs` | `2b6d42ddd2d7b65b0085bdce9dfad0c978d68f9c6107f55f1a0c2d40f3f2ed54` |
| `qualify-windows-runtime.py` | `d3c46e7b831bc45d91ff08153b05680947bf02bda92efdd32186b9b11f93a83a` |
| Prepared workflow | `ccc7bd7b5d325da981a7b6ba2a30ac212a4d89f7f81012f6af84555bbd9c26f3` |
| Executable-set digest | `0ee79b74b9f38e7535cfda6d63167bfcfcdf34076530c82d98467976d4c8a13f` |
| macOS apphost | `74b7afee17054765efd90410d680944d6c034da40488fd8c41e810df78cb854e` |
| Managed DLL | `059e31a6dbbf632b927c71b759862e2647448e6b1192299a9a437611061cae61` |
| deps.json | `acae75476537b481a7d610843882a1e9897317c224691146e252de770f6a646e` |
| runtimeconfig.json | `9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f` |
| Complete `summary.json` receipt | `73197a1deb2dd0735ec91d4367c1a77d2d64354bcb9e4dbbbe59b5fd14963ae1` |
| `controls.stdout.txt` | `9af21fb8a957428a9243663cc82d8d3a9dc083c6ea4e3ce3bf5e9eb824a8361e` |
| `controls.stderr.txt` (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Existing-root refusal stderr | `951c0ebb3df3dac8c8a60cc9c3d7289c17124e1ee706fd7d0ed2d2580b7ff1e8` |

Exact executed commands and observed results:

```text
python3 tools/qualify-windows-runtime.py --output /tmp/cfd-w0-final-proof-20260925
  Build: exit 0, 0 warnings, 0 errors; native/driver: exit 3, Not assessed.
  Build PID 73152; quiescent true; timeout false; 1.4989450420252979 seconds.
  Native PID 73174; quiescent true; timeout false; 0.36368895904161036 seconds.
python3 tools/qualify-windows-runtime.py --self-test
  exit 0; 35 wrong-result/ownership/source-drift controls rejected;
  self_test Pass, native_qualification Not assessed.
python3 tools/qualify-windows-runtime.py --output /tmp/cfd-w0-final-proof-20260925
  exit 1; existing root refused before build or native launch.
```

The last command is an intentional negative. Raw stderr:

```json
{"code": "W0-FAILED", "error": "[Errno 17] File exists: '/tmp/cfd-w0-final-proof-20260925'"}
```

**Observed reviewer correction:** root supplied
`/private/tmp/cfd-w0-validator-review.py`. Before repair it printed
`conflict WRONG-ROW-ACCEPTED overall_qualified= False` and
`cleanup-refusal WRONG-ROW-ACCEPTED overall_qualified= False`. The overall false
value hid an invalid individual row because other cases remained Not assessed.
After repair the same script printed:

```text
conflict rejected W0-WRONG-BYTES
cleanup-refusal rejected W0-WRONG-cleanup-refusal-foreignAfter
```

Class → sweep → derive → prevent: a partial receipt schema checked envelope and
status without all case-specific state. The sweep reached conflict preservation,
cancel-before absence, partial-write cleanup, foreign cleanup preservation, DACL,
aliases, link rejection, sharing and flush fields. The validator now checks those
fields; self-test supplies independently written positive examples for the four
reported cases and mutates each field, so a permanently rejecting parser is not
a passing control. A copied real C# source file is hashed, changed during the
self-test operation and rejected by the same post-run source guard. The
designated Coordinator was sent this class/control for the shared defect register;
the author did not write outside the six-path lease.

Repository check: `python3 tools/check-docs.py` first detected only the two new
documents missing from the derived index while its shared lease was held by the
Coordinator. After the lease was released, both derived paths were claimed and
the official `docs-graph.py derive` produced 112 entries. The prescribed check
then exited 0: no metadata defects, no orphans, no index drift, 80 pre-existing
review suggestions, audit log readable, `Documentation checks passed`.
`git diff --check` also exited 0. No native acceptance is inferred from this gate.

Plan versus actual: six graph nodes, serial one-author execution, one reviewer-
directed correction round. Local source changes caused new bound builds; only the
last manifest is final evidence. Completion time is measured by the closing audit
entry; actual tokens/model identity are Not recorded. The native floor remains
unmet by design, and independent review is the next gate rather than a self-issued
PASS. Simplifier disposition: no new package, production adapter, compatibility
shim or second native abstraction was added; source is a disposable measurement
artifact. Further native scope is deferred to the named decisions.

## Completed / remaining / next

Completed: six-path preparation and the local executable evidence recorded above.
Remaining: independent review and every native
Windows/runtime/UI/timing gate. Next: root reviews the exact commit and raw
receipts; Owner then decides a concrete W1 run route. No push/dispatch occurred.

## W1 hosted execution, 2026-09-25

This later checkpoint supersedes the W0-only status above for **whether a
Windows process ran**, but does not turn the candidate into a Windows runtime
pass. Owner Rulings 43–44 authorized exactly two pushes to the disposable
`feature/windows-w1-qualification-20260925` branch. The first run
[`36097138344`](https://github.com/timianmalloo/CFD-Workbench/actions/runs/36097138344)
failed workflow validation before a job existed: `runner.temp` was used in
job-level `env`, where that context is unavailable. The retained original
workflow failed official `actionlint` v1.7.12 with three context errors. The
one-YAML correction moved those variables to the consuming steps; the same
binary passed the corrected workflow. The corrected commit was
`7f34c13c3568ec31779866e061eb0a7d52dbfb36`, with five-source composite
`78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d`.

The second and final authorized run
[`36097839626`](https://github.com/timianmalloo/CFD-Workbench/actions/runs/36097839626)
started a `windows-2022` x64 job at `2026-09-25T05:16:06Z` and completed
failure at `05:16:46Z`. Checkout, SDK setup and 35 receipt wrong-result
controls succeeded. The native step failed, and the artifact upload succeeded.
The Windows build process exited 0; its receipt records the `dotnet.exe`,
`csc.exe` and `conhost.exe` identities with `quiescent=true`. The actual
`WindowsRuntime.exe` process was observed at PID 6116, exited 3 in 0.171 s,
and its process receipt records `quiescent=true`. These are process observations,
not proof that every native case passed.

The retained native stdout has 26 unique case rows: **21 Pass, four Fail, one
Not assessed**. The failures are `overwrite` and `cancel-after` (Win32 5,
publication false, expected publication true), `dacl-replacement` (`Access is
denied.`), and `ancestor-substitution` (`W0-ANCESTOR-MOVED`). The last row
does not emit the attempted move result or Win32 error, so its code alone
cannot establish that the ancestor actually moved. `directory-durability`
remains explicitly Not assessed. `dacl-create` passed its native private-DACL
check and emitted `O:LAD:P(A;;FA;;;LA)`, but the Python validator accepts only
numeric SID text and raised `W0-WRONG-DACL`. That is a separate receipt-oracle
representation mismatch: the native `PrivateDacl` check parses the descriptor
and compares its ACE SID with the current Windows identity, while the validator
rejects the emitted alias. Validation stopped before a completed `summary.json`,
post-run source rehash, full binary-file manifest or UIA capability result.
No binary bytes were uploaded for an independent Windows-byte recount.

Raw API/job/log receipts and downloaded artifact are retained under
`/tmp/cfd-w1-run-36097839626/`. Their SHA-256 values include `run.json`
`01ebb16d456d850faa3152fb50d379f5422b9a649c3b5175797b896d74300ef6`,
`jobs.json` `af26a39fa466d1439016a2119f18f4ca4cdf0453638c692aeac9915fc06244ea`,
`run.log` `7f680a8a6535194d08a21957f294d611a5eccd83f3baae69b6b5ec0ac7af20b7`,
artifact `native/stdout.txt`
`436f50f0163bbd408a539fa7cf1e75e429b2d92d321eb9a682e2a66e0ff26b54`,
and `native/process.json`
`2bb0932d17b8d0d3190b37025e36cd31bc12f28962f29770d5cc7cadefa5d030`.
The `source.json` in that artifact reports Windows Server 2022 image
`20260920.314.1`, SDK 10.0.203 source binding, and `native_qualification: Not
assessed`; its SHA-256 is
`4b1924738fd745bdb667f3473df60134233581ea5c4604ba56833856baa5aab7`.
The run is a measured Windows execution with failed qualification. Ruling 44
permits no third push or rerun. Product Windows persistence, UIA/Narrator,
actual displayed timing and M1 acceptance remain open.

## R45 frozen local diagnostic packet (2026-09-25)

**Prepared, not admitted.** This author implements only Ruling 45. Base
`a5b1d4c30c8bc40a3e47f432eebaa1953c30832c` incorporates current governance and
the unchanged failed-run W1 inputs. Requested model: gpt-6-astra; effective model
identity: Not recorded. Start marker: `2026-09-25T05:41:51Z`. Exact authored
surfaces are Program.cs, qualifier, workflow retention entries, linked design and
this proof. Root keeps Security/Data/Test vetoes. No push, dispatch, production
file, timing file or section-editor change occurred.

The frozen graph was executed serially through local proof: control/design
freeze → observed missing-consumer RED → semantic writer/consumer and failure
finalizer → local compile/refusal and expanded controls → one evidence-directed
correction packet → final local proof. The correction distinguishes non-Windows
missing fixtures from a Windows containment failure, binds to a query-only actual
process token, adds ancestor identity snapshots and numeric/raw contradiction
controls, and makes the forced-failure test call the real validator/finalizer.
The first local snapshot `/tmp/cfd-r45-local-20260925` remains historical and is
not the final source binding below. A progress message estimated elapsed time
incorrectly; the clock observation at 05:49:11 established 440 seconds since the
start marker. Only measured timestamps are evidence.

| Claim | Oracle / observed result | Confidence and limit |
|---|---|---|
| Strict semantic DACL consumer exists | Initial self-test raised `NameError: validate_dacl` before implementation. Final numeric/compact positives accepted; wrong owner/token/trustee/ACE count/type/qualifier/flags/inheritance/mask/protection, malformed/missing fields and contradictory numeric SDDL rejected. | Verified synthetic contract only. Actual compact alias resolution and native admission await Windows. |
| Failure retains final evidence | Real `consume_rows([])` records W0-CASE-SET; finalizer returns 1 and writes source/fixture hashes plus explicit unrun probes. Changed source and binary generate named failures. | Verified temporary-filesystem control. Disk loss/unwritable output can still prevent receipt persistence and remains a fatal failure. |
| Unsafe probe continuation is refused | Live job, cleanup error and failed containment rejected; safe synthetic prerequisite accepted. Real fixture inventory accepts private file and rejects an external hardlink. | Verified local guard controls. No hostile same-user race or Windows job proof. |
| All original scenarios preserved | Final real macOS apphost emits all26 original case names, each Not assessed, publication/durability false; exit3. | Verified actual non-Windows refusal; no Windows pass. |
| Native source compiles | SDK10.0.203 build0 warnings/errors. | Verified compile; marshaling and filesystem semantics not executed locally. |
| Runtime binding retained | Final source-before/after and four binary hashes equal; all4 local process groups observed quiescent. | Verified local receipts only. Windows process identities remain unmeasured. |
| Artifact scope stays bounded | Workflow diff adds only rows.json, binary-manifest.json and four named spike binaries. Pinned actionlint1.7.12, portable-text and subprocess-UTF8 gates exit0. | Verified diff/gates. No remote run. |

Native rename diagnostics preserve exact BOOL, immediate error (null on success),
source/destination bytes and identities, parent snapshots, held access/share and
lifetime before refusal assertions. Overwrite's independent held-target and
released-target arms leave the original scenario intact. These experiments are
unexecuted on Windows; no cause, successful release strategy, race safety or
ancestor movement is inferred. Snapshot errors themselves are retained.

The current process SID comes from `OpenProcessToken(Process.SafeHandle,
TOKEN_QUERY=8)` and `WindowsIdentity(IntPtr)` with both resources disposed.
Descriptor evidence is taken from the actual byte descriptor, including numeric
owner/trustee and complete ACE facts, before first payload. The native predicate
and independent Python consumer check the same explicit policy. Raw SDDL syntax
is bounded, numeric spellings must agree with numeric evidence, and compact aliases
are never an authorization shortcut. The consumer cannot independently resolve a
machine-relative compact alias on macOS; Windows-emitted descriptor evidence and
root's binary/source review are required. No foreign ACL policy is established.

Finalization retains named errors, raw rows, source/binary/fixture snapshots,
per-stage process receipts and unrun-probe reasons. After native quiescence,
bounded fixture inventory records identities/hash/base64 bytes and checks that
symlink targets and hardlink multiplicities remain within the disposable tree.
Unsupported reparse objects and escaped references refuse continuation. This is
a post-job observation only, not race-resistant production containment. Raw fixture
directories are deliberately not uploaded by a broad recursive glob. Only the
bounded inventory in summary and the exact four binaries augment retained evidence.

**Shared-register handoff:** textual spelling mistaken for identity → semantic
positive/negative controls; missing owner binding → wrong-owner/token controls;
post-assert diagnostic loss → pre-assert rename/ancestor snapshots; consumer
exception erases evidence → real forced validation failure plus finalizer control.
Coordinator owns serialized defect-register, graph/V16 and audit closure; these
shared files are outside the five authored paths. No author clears its own veto.

Definition-of-done limits: independent review remains pending; Windows native ABI,
replacement/ancestor experiment, actual alias resolution, lifecycle and UIA remain
Not assessed. UI/DDD/product migration controls are N/A to this disposable command
packet. No Stryker/property-generator claim is made: field mutation controls cover
the finite receipt policy; real Windows integration is the outstanding D4 gate.
Spend/tokens are Not recorded. Directory durability and production admission remain
open. Full section-editor work remains stopped.

### Retained final local raw evidence

Full raw output/compiled bytes remain under `/tmp/cfd-r45-final-local-20260925`;
the self-test stream is `/tmp/cfd-r45-final-self-test.txt`, RED stream
`/tmp/cfd-r45-red.txt`. The following durable structured excerpt is copied from
the actual final summary, not reconstructed from expected test values.

```json
{
  "source": "d6fbf7287419ebe54427245f9e0c79e595ade94c259ff6ab3bb1a3deb52dccb2",
  "sources": {
    "global.json": "6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df",
    "tools/spikes/WindowsRuntime/WindowsRuntime.csproj": "495225c72823bbf7a53a54028d1180a89a10805e69db034cee0a318cdf474c8f",
    "tools/spikes/WindowsRuntime/Program.cs": "cd3e8f04c3b2dfdc4a2b3b24cf95074b3963bd30a8fa52a4f394303bdce96ccc",
    "tools/qualify-windows-runtime.py": "7691ceac5b575091b5b4bccafa4d02336117b6d34d0a9033c25ccbd4dff1168e",
    ".github/workflows/application-windows-qualification.yml": "03f23cb26ee23b9bf4b79fcd6929fc886c3c857573b28f8ef84c9878e412a832"
  },
  "binary": "a12ade49f763ed3d0bff6a67454f76b725f6d865c6ed103f52551290223058cf",
  "binary_files": {
    "WindowsRuntime": "74b7afee17054765efd90410d680944d6c034da40488fd8c41e810df78cb854e",
    "WindowsRuntime.dll": "de5b2127619005f469ba929cf74acfc07e6571ac4fe79a96d4ecba8563a4a147",
    "WindowsRuntime.deps.json": "acae75476537b481a7d610843882a1e9897317c224691146e252de770f6a646e",
    "WindowsRuntime.runtimeconfig.json": "9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f"
  },
  "sdk": "10.0.203",
  "os": "macOS-27.0-arm64-arm-64bit-Mach-O",
  "arch": "arm64",
  "native_exit": 3,
  "native_qualification": "Not assessed",
  "containment": {
    "status": "Not assessed",
    "reason": "non-Windows host"
  },
  "errors": [],
  "probes": {
    "tree-timeout": {
      "status": "Not assessed",
      "reason": "non-Windows host"
    },
    "observer-fault": {
      "status": "Not assessed",
      "reason": "non-Windows host"
    },
    "uia-capability": {
      "status": "Not assessed",
      "reason": "non-Windows host"
    }
  },
  "fixtures": {
    "fixture-a.bin": {
      "sha256": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "expected": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c"
    },
    "fixture-b.bin": {
      "sha256": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07",
      "expected": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07"
    }
  },
  "sources_equal_after": true,
  "binary_files_equal_after": true,
  "processes": {
    "build": {
      "pid": 92774,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 1.4141454170458019
    },
    "native": {
      "pid": 92776,
      "exit": 3,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.2107511250069365
    },
    "sdk": {
      "pid": 92772,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.07888808299321681
    },
    "sdk-info": {
      "pid": 92773,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.07431791699491441
    }
  },
  "cases": [
    {
      "case": "create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "create-collision",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "read",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "overwrite",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "conflict",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "immutable-input",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cancel-before",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cancel-after",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "write-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "replace-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "sharing",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-inheritance",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-replacement",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "denial",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "case-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "unicode-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ads-device-unc",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "hard-link",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "leaf-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ancestor-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ancestor-substitution",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "owned-cleanup",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cleanup-refusal",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "file-flush",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "directory-durability",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    }
  ]
}
```

Raw emitted control stream (72 controls plus final status):

```jsonl
{"control": "semantic-dacl-equivalent-positive", "result": "Pass"}
{"control": "semantic-dacl-equivalent-positive", "result": "Pass"}
{"control": "dacl-owner", "result": "rejected"}
{"control": "dacl-current-user", "result": "rejected"}
{"control": "dacl-protection", "result": "rejected"}
{"control": "dacl-present", "result": "rejected"}
{"control": "dacl-count", "result": "rejected"}
{"control": "dacl-raw-malformed", "result": "rejected"}
{"control": "dacl-raw-numeric-mismatch", "result": "rejected"}
{"control": "dacl-trusteeSid", "result": "rejected"}
{"control": "dacl-qualifier", "result": "rejected"}
{"control": "dacl-type", "result": "rejected"}
{"control": "dacl-inherited", "result": "rejected"}
{"control": "dacl-flags", "result": "rejected"}
{"control": "dacl-mask", "result": "rejected"}
{"control": "dacl-extra-ace", "result": "rejected"}
{"control": "dacl-missing-ace-type", "result": "rejected"}
{"control": "dacl-missing-ace-qualifier", "result": "rejected"}
{"control": "dacl-missing-ace-flags", "result": "rejected"}
{"control": "dacl-missing-ace-inherited", "result": "rejected"}
{"control": "dacl-missing-ace-mask", "result": "rejected"}
{"control": "dacl-missing-ace-trusteeSid", "result": "rejected"}
{"control": "dacl-missing-rawSddl", "result": "rejected"}
{"control": "dacl-missing-ownerSid", "result": "rejected"}
{"control": "dacl-missing-tokenUserSid", "result": "rejected"}
{"control": "dacl-missing-daclPresent", "result": "rejected"}
{"control": "dacl-missing-daclProtected", "result": "rejected"}
{"control": "dacl-missing-aceCount", "result": "rejected"}
{"control": "dacl-missing-aces", "result": "rejected"}
{"control": "unsafe-continuation-live", "result": "rejected"}
{"control": "unsafe-continuation-cleanup", "result": "rejected"}
{"control": "unsafe-continuation-containment", "result": "rejected"}
{"control": "validator-failure-finalization", "result": "Pass"}
{"control": "finalizer-source-drift", "result": "rejected"}
{"control": "finalizer-binary-drift", "result": "rejected"}
{"control": "fixture-external-hardlink", "result": "rejected"}
{"control": "missing", "result": "rejected"}
{"control": "duplicate", "result": "rejected"}
{"control": "source", "result": "rejected"}
{"control": "binary", "result": "rejected"}
{"control": "unsupported-pass", "result": "rejected"}
{"control": "durability", "result": "rejected"}
{"control": "wrong-after", "result": "rejected"}
{"control": "wrong-publication", "result": "rejected"}
{"control": "live-or-unobserved-tree", "result": "rejected"}
{"control": "live-or-unobserved-tree", "result": "rejected"}
{"control": "conflict-wrong-after", "result": "rejected"}
{"control": "conflict-wrong-saveCode", "result": "rejected"}
{"control": "conflict-wrong-candidateDurability", "result": "rejected"}
{"control": "conflict-wrong-targetExists", "result": "rejected"}
{"control": "conflict-wrong-tempExists", "result": "rejected"}
{"control": "conflict-wrong-claimExists", "result": "rejected"}
{"control": "conflict-wrong-cancellationRequested", "result": "rejected"}
{"control": "cancel-before-wrong-saveCode", "result": "rejected"}
{"control": "cancel-before-wrong-candidateDurability", "result": "rejected"}
{"control": "cancel-before-wrong-targetExists", "result": "rejected"}
{"control": "cancel-before-wrong-tempExists", "result": "rejected"}
{"control": "cancel-before-wrong-claimExists", "result": "rejected"}
{"control": "cancel-before-wrong-cancellationRequested", "result": "rejected"}
{"control": "write-fault-wrong-saveCode", "result": "rejected"}
{"control": "write-fault-wrong-candidateDurability", "result": "rejected"}
{"control": "write-fault-wrong-targetExists", "result": "rejected"}
{"control": "write-fault-wrong-tempExists", "result": "rejected"}
{"control": "write-fault-wrong-claimExists", "result": "rejected"}
{"control": "write-fault-wrong-writtenBeforeFault", "result": "rejected"}
{"control": "write-fault-wrong-cancellationRequested", "result": "rejected"}
{"control": "cleanup-refusal-wrong-foreignAfter", "result": "rejected"}
{"control": "cleanup-refusal-wrong-cleanupRefused", "result": "rejected"}
{"control": "cleanup-refusal-wrong-targetExists", "result": "rejected"}
{"control": "cleanup-refusal-wrong-ownedOriginalExists", "result": "rejected"}
{"control": "source-drift-during-operation", "result": "rejected"}
{"control": "help-exits-without-qualification", "result": "Pass"}
{"self_test": "Pass", "native_qualification": "Not assessed"}
```

Build stdout:

```text
  Determining projects to restore...
  Restored /Users/mallalieut/projects/CFD-Workbench-feature-windows-w1-diagnostics/tools/spikes/WindowsRuntime/WindowsRuntime.csproj (in 23 ms).
  WindowsRuntime -> /tmp/cfd-r45-final-local-20260925/artifacts/bin/WindowsRuntime/release/WindowsRuntime.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.31
```

### Durable original Windows failure evidence (R47 retention amendment)

These five raw row objects are copied from run36097839626 attempt1. They contain
only synthetic fixture hashes, disposable identity, platform metadata and errors;
no account name, token secret or user file path is present. Native Pass is a
self-report, not consumer acceptance. Missing ancestor operands remain missing.
The other21 original row objects remain in the downloaded raw receipt.

```json
[
  {
    "case": "overwrite",
    "status": "Fail",
    "code": "W0-ORACLE-FAILED",
    "source": "78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d",
    "binary": "cbb688e1ccf739eb47552d5d9c0b42a8113b1d3f0d67a1455602df6f673ad2b6",
    "publication": false,
    "durability": false,
    "elapsedMilliseconds": 1.7703,
    "evidence": {
      "os": "Microsoft Windows 10.0.20348",
      "architecture": "X64",
      "filesystem": "NTFS",
      "fixtureA": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "fixtureB": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07",
      "nativeError": 5,
      "saveCode": "DOC-SAVE-UNCERTAIN",
      "candidateDurability": false,
      "cancellationRequested": false,
      "expectedDiskHash": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "error": "W0-WRONG-SAVE-STATE"
    }
  },
  {
    "case": "cancel-after",
    "status": "Fail",
    "code": "W0-ORACLE-FAILED",
    "source": "78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d",
    "binary": "cbb688e1ccf739eb47552d5d9c0b42a8113b1d3f0d67a1455602df6f673ad2b6",
    "publication": false,
    "durability": false,
    "elapsedMilliseconds": 1.5924,
    "evidence": {
      "os": "Microsoft Windows 10.0.20348",
      "architecture": "X64",
      "filesystem": "NTFS",
      "fixtureA": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "fixtureB": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07",
      "nativeError": 5,
      "saveCode": "DOC-SAVE-UNCERTAIN",
      "candidateDurability": false,
      "cancellationRequested": false,
      "expectedDiskHash": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "error": "W0-WRONG-SAVE-STATE"
    }
  },
  {
    "case": "dacl-create",
    "status": "Pass",
    "code": "OK",
    "source": "78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d",
    "binary": "cbb688e1ccf739eb47552d5d9c0b42a8113b1d3f0d67a1455602df6f673ad2b6",
    "publication": false,
    "durability": false,
    "elapsedMilliseconds": 0.7023,
    "evidence": {
      "os": "Microsoft Windows 10.0.20348",
      "architecture": "X64",
      "filesystem": "NTFS",
      "fixtureA": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "fixtureB": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07",
      "before": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "identityBefore": "8c8e1a7a:00010000000020ff",
      "creationDacl": "O:LAD:P(A;;FA;;;LA)"
    }
  },
  {
    "case": "dacl-replacement",
    "status": "Fail",
    "code": "W0-ORACLE-FAILED",
    "source": "78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d",
    "binary": "cbb688e1ccf739eb47552d5d9c0b42a8113b1d3f0d67a1455602df6f673ad2b6",
    "publication": false,
    "durability": false,
    "elapsedMilliseconds": 1.2608,
    "evidence": {
      "os": "Microsoft Windows 10.0.20348",
      "architecture": "X64",
      "filesystem": "NTFS",
      "fixtureA": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "fixtureB": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07",
      "before": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "identityBefore": "8c8e1a7a:0001000000002105",
      "error": "Access is denied."
    }
  },
  {
    "case": "ancestor-substitution",
    "status": "Fail",
    "code": "W0-ORACLE-FAILED",
    "source": "78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d",
    "binary": "cbb688e1ccf739eb47552d5d9c0b42a8113b1d3f0d67a1455602df6f673ad2b6",
    "publication": false,
    "durability": false,
    "elapsedMilliseconds": 0.515,
    "evidence": {
      "os": "Microsoft Windows 10.0.20348",
      "architecture": "X64",
      "filesystem": "NTFS",
      "fixtureA": "6ef1575c271cc007a46a1aeb500268906bc8bc81633e4bd337c44751e43fa25c",
      "fixtureB": "948f1c0f3cf594212790d76d6b9a08b042b8067ff054644f8f08c031bf9d0f07",
      "error": "W0-ANCESTOR-MOVED"
    }
  }
]
```

## R48 bounded source/local-controls checkpoint

**Partial, pending root disposition.** Start marker `2026-09-25T06:18:42Z`, session
`cfd-windows-r48-author-20260925`, clean isolated base `752c869873fe`.
Requested model gpt-6-astra; effective identity Not recorded. Exactly four authored
paths: Program.cs, qualifier, design and this proof. Workflow bytes remain frozen.
No Windows run, push, production code, capture or section work occurred.

The RED stream below reproduces all eight contradictory owner/trustee SY/ZZ forms
against Administrator before the implementation. The consumer now resolves the
supported compact semantics using measured-context-shaped evidence, requires native
raw-SDDL reparsing, and compares every numeric descriptor/ACE fact. LA is bound to
an independently queried LSA system account domain, not a token suffix. Numeric,
LA/mixed and SY-under-System synthetic positives prevent blanket rejection.
Unsupported aliases and missing/contradictory context refuse. Native LSA and raw
conversion agreement remain unmeasured locally.

The same orchestration used around the actual original overwrite scenario runs
in `--diagnostic-controls`. Its injected actions deliberately do no Win32/file
work: they prove invocation, preservation and refusal control flow, not native
replacement or containment. Setup/operation/observation failures retain the next
arm and invoke the original once; original failure retains both arms; injected
unsafe containment and unclosed-handle status suppress further execution. Each
control emits source/binary binding and raw arm evidence. The driver independently
checks those rows and rejects five altered result packets.

**Unresolved safety predicate:** final fresh arm-directory pin failure can raise
`Unsupported`, which currently reaches the expected-error catch. The wrapper
revalidates the original directory, but that is not proof of the arm directory's
containment. The explicit identity-mismatch path is unsafe; the exception path is
not equivalently classified. Root must resolve this before publication. Local
green injected controls do not cover that native exception distinction. This is
reported rather than hidden by another correction beyond the authorized one.

| Claim | Evidence / oracle | Status and limit |
|---|---|---|
| Alias contradiction fixed in consumer | Eight previously accepted SY/ZZ fixtures now rejected; numeric/LA/mixed/SY-System positives pass; missing context and every resolved ACE-field mismatch reject. | Verified synthetic consumer behavior; actual LSA/Windows conversion not measured. |
| Prior controls retained | Final stream has102 controls, including prior72, then Pass. | Verified local execution; no Windows admission. |
| Diagnostic expected-error isolation | Six real apphost injected orchestration receipts plus five independent consumer mutations. | Verified control flow for injected actions; final native arm-pin exception remains unresolved. |
| Changed-source compilation/refusal | SDK10.0.203, build0 warnings/errors;26 original cases Not assessed, exit3. | Verified actual macOS composition root. |
| Binding and local lifecycle | Source-before/after and four binary hashes equal; five owned local process groups quiescent. | Verified local group observations only. |
| Portable text | Portable-text and subprocess-UTF8 gates exit0, read back clean messages. | Verified changed tool surface; workflow unchanged. |

One coherent patch followed by one local correction added raw-ACE type-sensitive
controls, nested-arm descriptor consumption, explicit unclosed-handle refusal,
five diagnostic consumer mutations and pre-acquisition identity placeholders.
The later native arm-pin distinction above is an outstanding finding, not an
unlogged second correction. No source changes followed the final run below.

Class handoff for Coordinator's serialized register: lexical alias shape mistaken
for identity → eight observed-RED contradictions plus explicit context/resolution;
diagnostic observer preempts subject → shared orchestration injection controls;
exception classification omits a containment-loss branch → unresolved root gate.
Coordinator owns official audit closure, docs-index/V16 and shared register. Author
does not self-clear any Security/Data/Test veto. The reference-device/timing,
Windows lifecycle, durability and full section-editor boundaries remain unchanged.

### Raw RED alias stream

```jsonl
{"code": "W0-FAILED", "error": "contradictory aliases accepted: SY-owner-numeric,SY-ace-numeric,SY-owner-alias,SY-ace-alias,ZZ-owner-numeric,ZZ-ace-numeric,ZZ-owner-alias,ZZ-ace-alias"}
{"control": "r48-SY-owner-numeric", "result": "WRONGLY ACCEPTED"}
{"control": "r48-SY-ace-numeric", "result": "WRONGLY ACCEPTED"}
{"control": "r48-SY-owner-alias", "result": "WRONGLY ACCEPTED"}
{"control": "r48-SY-ace-alias", "result": "WRONGLY ACCEPTED"}
{"control": "r48-ZZ-owner-numeric", "result": "WRONGLY ACCEPTED"}
{"control": "r48-ZZ-ace-numeric", "result": "WRONGLY ACCEPTED"}
{"control": "r48-ZZ-owner-alias", "result": "WRONGLY ACCEPTED"}
{"control": "r48-ZZ-ace-alias", "result": "WRONGLY ACCEPTED"}
```

### Final local structured receipt

Raw files and binary bytes remain at `/tmp/cfd-r48-final-local-20260925`; the following durable excerpt is copied from its actual summary.

```json
{
  "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
  "sources": {
    "global.json": "6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df",
    "tools/spikes/WindowsRuntime/WindowsRuntime.csproj": "495225c72823bbf7a53a54028d1180a89a10805e69db034cee0a318cdf474c8f",
    "tools/spikes/WindowsRuntime/Program.cs": "1cfff4004c5a9609aa0db6dff7eafd831e1867b44e987e8261c3b3895df8e334",
    "tools/qualify-windows-runtime.py": "d3c676732927cd0e043439698302bc85b2994848d30f19a106f304f9aa258b21",
    ".github/workflows/application-windows-qualification.yml": "03f23cb26ee23b9bf4b79fcd6929fc886c3c857573b28f8ef84c9878e412a832"
  },
  "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
  "binary_files": {
    "WindowsRuntime": "74b7afee17054765efd90410d680944d6c034da40488fd8c41e810df78cb854e",
    "WindowsRuntime.dll": "88da8696f57945f2f5d56aea4e97ed37b5a2c9337ac4f27202c6429d5e8c7ac0",
    "WindowsRuntime.deps.json": "acae75476537b481a7d610843882a1e9897317c224691146e252de770f6a646e",
    "WindowsRuntime.runtimeconfig.json": "9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f"
  },
  "sdk": "10.0.203",
  "os": "macOS-27.0-arm64-arm-64bit-Mach-O",
  "arch": "arm64",
  "native_exit": 3,
  "native_qualification": "Not assessed",
  "containment": {
    "status": "Not assessed",
    "reason": "non-Windows host"
  },
  "errors": [],
  "diagnostic_controls": [
    {
      "control": "setup",
      "scope": "synthetic orchestration",
      "result": "Pass",
      "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
      "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
      "originalCalls": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-disjoint-root/held-target",
            "targetPath": "synthetic-disjoint-root/held-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Fail",
            "exception": "injected setup",
            "exceptionType": "IOException"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-disjoint-root/released-target",
            "targetPath": "synthetic-disjoint-root/released-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          }
        ],
        "originalOverwriteInvoked": true
      }
    },
    {
      "control": "operation",
      "scope": "synthetic orchestration",
      "result": "Pass",
      "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
      "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
      "originalCalls": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-disjoint-root/held-target",
            "targetPath": "synthetic-disjoint-root/held-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Fail",
            "exception": "injected operation",
            "exceptionType": "IOException"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-disjoint-root/released-target",
            "targetPath": "synthetic-disjoint-root/released-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          }
        ],
        "originalOverwriteInvoked": true
      }
    },
    {
      "control": "observation",
      "scope": "synthetic orchestration",
      "result": "Pass",
      "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
      "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
      "originalCalls": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-disjoint-root/held-target",
            "targetPath": "synthetic-disjoint-root/held-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Fail",
            "exception": "injected observation",
            "exceptionType": "IOException"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-disjoint-root/released-target",
            "targetPath": "synthetic-disjoint-root/released-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          }
        ],
        "originalOverwriteInvoked": true
      }
    },
    {
      "control": "original",
      "scope": "synthetic orchestration",
      "result": "Pass",
      "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
      "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
      "originalCalls": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-disjoint-root/held-target",
            "targetPath": "synthetic-disjoint-root/held-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-disjoint-root/released-target",
            "targetPath": "synthetic-disjoint-root/released-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          }
        ],
        "originalOverwriteInvoked": true,
        "originalException": "injected original scenario"
      }
    },
    {
      "control": "unsafe",
      "scope": "synthetic orchestration",
      "result": "Pass",
      "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
      "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
      "originalCalls": 0,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-disjoint-root/held-target",
            "targetPath": "synthetic-disjoint-root/held-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "exception": "injected containment loss"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-disjoint-root/released-target",
            "targetPath": "synthetic-disjoint-root/released-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "originalException": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      }
    },
    {
      "control": "cleanup",
      "scope": "synthetic orchestration",
      "result": "Pass",
      "source": "e0982da30af707e001d17027ff51439d8b85d8d8e33761e6091a2164b5bafd22",
      "binary": "e55c5aef066c4f36af9c3d325225217a0c40521f0608cb32e92398fb9d7141b4",
      "originalCalls": 0,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-disjoint-root/held-target",
            "targetPath": "synthetic-disjoint-root/held-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [
              "setup",
              "operation",
              "observation"
            ],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "injected unclosed handle",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-disjoint-root/released-target",
            "targetPath": "synthetic-disjoint-root/released-target/target.bin",
            "stagedPath": "synthetic-disjoint-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "originalException": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      }
    }
  ],
  "diagnostic_control_mutations": [
    {
      "control": "suppressed-original",
      "result": "rejected",
      "error": "W0-ORIGINAL-SUPPRESSED"
    },
    {
      "control": "suppressed-second-arm",
      "result": "rejected",
      "error": "W0-DIAGNOSTIC-SECOND-ARM-SUPPRESSED"
    },
    {
      "control": "unsafe-continuation",
      "result": "rejected",
      "error": "W0-ORIGINAL-SUPPRESSED"
    },
    {
      "control": "cleanup-continuation",
      "result": "rejected",
      "error": "W0-ORIGINAL-SUPPRESSED"
    },
    {
      "control": "wrong-binary",
      "result": "rejected",
      "error": "W0-DIAGNOSTIC-CONTROL-BINDING"
    }
  ],
  "source_equal_after": true,
  "binary_equal_after": true,
  "processes": {
    "build": {
      "pid": 8509,
      "exit": 0,
      "quiescent": true,
      "timeout": false,
      "elapsed_seconds": 1.4838478750316426
    },
    "diagnostic-controls": {
      "pid": 8512,
      "exit": 0,
      "quiescent": true,
      "timeout": false,
      "elapsed_seconds": 0.3222116249380633
    },
    "native": {
      "pid": 8513,
      "exit": 3,
      "quiescent": true,
      "timeout": false,
      "elapsed_seconds": 0.047301416052505374
    },
    "sdk": {
      "pid": 8507,
      "exit": 0,
      "quiescent": true,
      "timeout": false,
      "elapsed_seconds": 0.08754554204642773
    },
    "sdk-info": {
      "pid": 8508,
      "exit": 0,
      "quiescent": true,
      "timeout": false,
      "elapsed_seconds": 0.08378879202064127
    }
  },
  "cases": [
    {
      "case": "create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "create-collision",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "read",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "overwrite",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "conflict",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "immutable-input",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cancel-before",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cancel-after",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "write-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "replace-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "sharing",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-inheritance",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-replacement",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "denial",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "case-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "unicode-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ads-device-unc",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "hard-link",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "leaf-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ancestor-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ancestor-substitution",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "owned-cleanup",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cleanup-refusal",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "file-flush",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "directory-durability",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    }
  ]
}
```

### Final emitted consumer controls

```jsonl
{"control": "r48-SY-owner-numeric", "result": "rejected"}
{"control": "r48-SY-ace-numeric", "result": "rejected"}
{"control": "r48-SY-owner-alias", "result": "rejected"}
{"control": "r48-SY-ace-alias", "result": "rejected"}
{"control": "r48-ZZ-owner-numeric", "result": "rejected"}
{"control": "r48-ZZ-ace-numeric", "result": "rejected"}
{"control": "r48-ZZ-owner-alias", "result": "rejected"}
{"control": "r48-ZZ-ace-alias", "result": "rejected"}
{"control": "r48-equivalent-S-1-5-21-1-2-3-500-S-1-5-21-1-2-3-500", "result": "Pass", "scope": "synthetic"}
{"control": "r48-equivalent-LA-LA", "result": "Pass", "scope": "synthetic"}
{"control": "r48-equivalent-S-1-5-21-1-2-3-500-LA", "result": "Pass", "scope": "synthetic"}
{"control": "r48-equivalent-LA-S-1-5-21-1-2-3-500", "result": "Pass", "scope": "synthetic"}
{"control": "r48-rawResolution", "result": "rejected"}
{"control": "r48-missing-context", "result": "rejected"}
{"control": "r48-missing-descriptor", "result": "rejected"}
{"control": "r48-missing-method", "result": "rejected"}
{"control": "r48-wrong-domain-context", "result": "rejected"}
{"control": "r48-raw-resolved-ownerSid", "result": "rejected"}
{"control": "r48-raw-resolved-daclPresent", "result": "rejected"}
{"control": "r48-raw-resolved-daclProtected", "result": "rejected"}
{"control": "r48-raw-resolved-aceCount", "result": "rejected"}
{"control": "r48-raw-resolved-aces", "result": "rejected"}
{"control": "r48-raw-resolved-ace-trusteeSid", "result": "rejected"}
{"control": "r48-raw-resolved-ace-type", "result": "rejected"}
{"control": "r48-raw-resolved-ace-qualifier", "result": "rejected"}
{"control": "r48-raw-resolved-ace-flags", "result": "rejected"}
{"control": "r48-raw-resolved-ace-inherited", "result": "rejected"}
{"control": "r48-raw-resolved-ace-mask", "result": "rejected"}
{"control": "r48-SY-equivalent-positive", "result": "Pass", "scope": "synthetic"}
{"control": "semantic-dacl-equivalent-positive", "result": "Pass"}
{"control": "semantic-dacl-equivalent-positive", "result": "Pass"}
{"control": "dacl-owner", "result": "rejected"}
{"control": "dacl-current-user", "result": "rejected"}
{"control": "dacl-protection", "result": "rejected"}
{"control": "dacl-present", "result": "rejected"}
{"control": "dacl-count", "result": "rejected"}
{"control": "dacl-raw-malformed", "result": "rejected"}
{"control": "dacl-raw-numeric-mismatch", "result": "rejected"}
{"control": "dacl-trusteeSid", "result": "rejected"}
{"control": "dacl-qualifier", "result": "rejected"}
{"control": "dacl-type", "result": "rejected"}
{"control": "dacl-inherited", "result": "rejected"}
{"control": "dacl-flags", "result": "rejected"}
{"control": "dacl-mask", "result": "rejected"}
{"control": "dacl-extra-ace", "result": "rejected"}
{"control": "dacl-missing-ace-type", "result": "rejected"}
{"control": "dacl-missing-ace-qualifier", "result": "rejected"}
{"control": "dacl-missing-ace-flags", "result": "rejected"}
{"control": "dacl-missing-ace-inherited", "result": "rejected"}
{"control": "dacl-missing-ace-mask", "result": "rejected"}
{"control": "dacl-missing-ace-trusteeSid", "result": "rejected"}
{"control": "dacl-missing-rawSddl", "result": "rejected"}
{"control": "dacl-missing-ownerSid", "result": "rejected"}
{"control": "dacl-missing-tokenUserSid", "result": "rejected"}
{"control": "dacl-missing-daclPresent", "result": "rejected"}
{"control": "dacl-missing-daclProtected", "result": "rejected"}
{"control": "dacl-missing-aceCount", "result": "rejected"}
{"control": "dacl-missing-aces", "result": "rejected"}
{"control": "dacl-missing-rawResolution", "result": "rejected"}
{"control": "unsafe-continuation-live", "result": "rejected"}
{"control": "unsafe-continuation-cleanup", "result": "rejected"}
{"control": "unsafe-continuation-containment", "result": "rejected"}
{"control": "validator-failure-finalization", "result": "Pass"}
{"control": "finalizer-source-drift", "result": "rejected"}
{"control": "finalizer-binary-drift", "result": "rejected"}
{"control": "fixture-external-hardlink", "result": "rejected"}
{"control": "missing", "result": "rejected"}
{"control": "duplicate", "result": "rejected"}
{"control": "source", "result": "rejected"}
{"control": "binary", "result": "rejected"}
{"control": "unsupported-pass", "result": "rejected"}
{"control": "durability", "result": "rejected"}
{"control": "wrong-after", "result": "rejected"}
{"control": "wrong-publication", "result": "rejected"}
{"control": "live-or-unobserved-tree", "result": "rejected"}
{"control": "live-or-unobserved-tree", "result": "rejected"}
{"control": "conflict-wrong-after", "result": "rejected"}
{"control": "conflict-wrong-saveCode", "result": "rejected"}
{"control": "conflict-wrong-candidateDurability", "result": "rejected"}
{"control": "conflict-wrong-targetExists", "result": "rejected"}
{"control": "conflict-wrong-tempExists", "result": "rejected"}
{"control": "conflict-wrong-claimExists", "result": "rejected"}
{"control": "conflict-wrong-cancellationRequested", "result": "rejected"}
{"control": "cancel-before-wrong-saveCode", "result": "rejected"}
{"control": "cancel-before-wrong-candidateDurability", "result": "rejected"}
{"control": "cancel-before-wrong-targetExists", "result": "rejected"}
{"control": "cancel-before-wrong-tempExists", "result": "rejected"}
{"control": "cancel-before-wrong-claimExists", "result": "rejected"}
{"control": "cancel-before-wrong-cancellationRequested", "result": "rejected"}
{"control": "write-fault-wrong-saveCode", "result": "rejected"}
{"control": "write-fault-wrong-candidateDurability", "result": "rejected"}
{"control": "write-fault-wrong-targetExists", "result": "rejected"}
{"control": "write-fault-wrong-tempExists", "result": "rejected"}
{"control": "write-fault-wrong-claimExists", "result": "rejected"}
{"control": "write-fault-wrong-writtenBeforeFault", "result": "rejected"}
{"control": "write-fault-wrong-cancellationRequested", "result": "rejected"}
{"control": "cleanup-refusal-wrong-foreignAfter", "result": "rejected"}
{"control": "cleanup-refusal-wrong-cleanupRefused", "result": "rejected"}
{"control": "cleanup-refusal-wrong-targetExists", "result": "rejected"}
{"control": "cleanup-refusal-wrong-ownedOriginalExists", "result": "rejected"}
{"control": "source-drift-during-operation", "result": "rejected"}
{"control": "help-exits-without-qualification", "result": "Pass"}
{"self_test": "Pass", "native_qualification": "Not assessed"}
```

Build stdout:

```text
  Determining projects to restore...
  Restored /Users/mallalieut/projects/CFD-Workbench-feature-windows-r48-alias-isolation/tools/spikes/WindowsRuntime/WindowsRuntime.csproj (in 23 ms).
  WindowsRuntime -> /tmp/cfd-r48-final-local-20260925/artifacts/bin/WindowsRuntime/release/WindowsRuntime.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.35
```

Portable-text and subprocess-UTF8 stdout:

```text
clean - every text write under pack/scripts, pack/adapters/hooks, tools is LF, every printing CLI guards its console
clean - every text-mode subprocess under pack/scripts, pack/adapters/hooks, tools states its encoding
```

## R51 final-arm guard and constructor ownership proof

**Implemented locally; independent root review pending.** Session
`cfd-windows-r51-author-20260925`, official implement marker
`2026-09-25T06:45:53Z`, isolated base `bf2ceb0`, exactly four authored paths.
Requested model gpt-6-astra; effective identity Not recorded. No Windows run,
remote push, workflow edit, production path, capture or section-editor work.
R48's frozen partial evidence is preserved above.

RED was measured through the actual orchestrator and extracted final-observation
call boundary: unsupported/access/observation faults for both arms caused original
invocation (six failures); identity inequality and cleanup uncertainty already
refused (four passes). The native final call was routed through that same boundary
before changing its behavior. The standalone RED build does not carry a final
source manifest; it is failure demonstration, not qualification. Its raw output is
`/tmp/cfd-r51-red-final-arm.jsonl`, with build output `/tmp/cfd-r51-red-build.txt`.

```json
[
  {
    "control": "held-unsupported",
    "result": "Fail",
    "originalCalls": 1,
    "operations": 2
  },
  {
    "control": "held-access",
    "result": "Fail",
    "originalCalls": 1,
    "operations": 2
  },
  {
    "control": "held-observation",
    "result": "Fail",
    "originalCalls": 1,
    "operations": 2
  },
  {
    "control": "held-identity",
    "result": "Pass",
    "originalCalls": 0,
    "operations": 1
  },
  {
    "control": "held-cleanup",
    "result": "Pass",
    "originalCalls": 0,
    "operations": 1
  },
  {
    "control": "released-unsupported",
    "result": "Fail",
    "originalCalls": 1,
    "operations": 2
  },
  {
    "control": "released-access",
    "result": "Fail",
    "originalCalls": 1,
    "operations": 2
  },
  {
    "control": "released-observation",
    "result": "Fail",
    "originalCalls": 1,
    "operations": 2
  },
  {
    "control": "released-identity",
    "result": "Pass",
    "originalCalls": 0,
    "operations": 2
  },
  {
    "control": "released-cleanup",
    "result": "Pass",
    "originalCalls": 0,
    "operations": 2
  }
]
```

The correction puts final arm validation in finally before cleanup and marks every
validation exception unsafe, retaining its exact type/message/native error. An
ordinary operation failure no longer bypasses final containment verification.
The orchestrator requires positive final verification and known cleanup, preserves
both arm records, sets originalOverwriteInvoked=false on uncertainty, and emits
later affected cases through the same Not assessed guard used by the real loop.
No alternate implementation of overwrite substitutes for the original Scenario.

| Claim | Executed oracle/result | Confidence/limit |
|---|---|---|
| Final observation uncertainty stops unsafe work | Ten held/released fault controls now Pass: Unsupported, Win325, IOException, identity inequality, cleanup uncertainty. Zero original calls; no later arm when first arm fails. | Verified shared guard/orchestrator behavior with injected observation actions. Native path observation not executed on macOS. |
| All original names survive containment loss | Each control calls the actual case-loop refusal helper for all26 names. Consumer checks26 exact names/status/code/bindings and false publication/durability. | Verified refusal path; earlier real cases would retain any actual outcomes already emitted. |
| Constructor cleanup was already correct | Actual PinnedPath ownership loop acquires1 and3 real managed file handles before injected inspection throws. All acquired handles close; foreign handle remains usable; exact exception reference preserved. | Verified local ownership cleanup; Windows directory-open ABI/flags remain unexecuted. No cleanup-policy repair was required. |
| Independent consumer catches false proof | Seven mutations reject original/later unsafe work, lost error, omitted cases, leaked acquired handle, closed foreign handle and replaced error. | Verified consumer negatives; no claim these mutants are native execution. |
| Prior controls preserved |102 self-test controls,6 prior apphost diagnostic controls and5 prior consumer mutations pass. | Verified local regression controls. |
| Changed-source composition root | SDK10.0.203 compile0 warnings/errors, original native26 cases Not assessed and exit3. | Verified macOS refusal only. |
| Binding/lifecycle/portability | Source and four binary files unchanged before/after; all7 local owned process groups quiescent; portable-text and subprocess-UTF8 clean. | Verified local receipts; Windows job/LSA/durability remain unqualified. |

Only the coherent RED→GREEN correction was needed; no evidence-directed corrective
pass was consumed. Frozen plan → RED → implementation → local proof completed;
root review is the next graph node. Shared audit/index/V16/defect register belong
to Coordinator. Class recurrence: an observer exception must not be treated as a
safe ordinary operation failure; controls now reach the shared final guard and
actual constructor ownership loop. Costs/tokens remain Not recorded.

### Durable final source-bound structured receipt

Full raw files and compiled bytes remain at `/tmp/cfd-r51-local-20260925`.
The following actual summary excerpt deduplicates only byte-equivalent parsed
26-row refusal arrays; each of the ten raw control rows contains that full array.
Constructor fixtures are one-byte synthetic files in the task-owned output tree.

```json
{
  "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
  "sources": {
    "global.json": "6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df",
    "tools/spikes/WindowsRuntime/WindowsRuntime.csproj": "495225c72823bbf7a53a54028d1180a89a10805e69db034cee0a318cdf474c8f",
    "tools/spikes/WindowsRuntime/Program.cs": "d046874d50b8587a460a6ad6bf19d1867152ed53c26c2d8ca7007a435c1cf964",
    "tools/qualify-windows-runtime.py": "3c8d5c3e6a1426a38d622466f5ebcb87f587cac09dec88ef633bceacfbf834cb",
    ".github/workflows/application-windows-qualification.yml": "ccc7bd7b5d325da981a7b6ba2a30ac212a4d89f7f81012f6af84555bbd9c26f3"
  },
  "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
  "binary_files": {
    "WindowsRuntime": "74b7afee17054765efd90410d680944d6c034da40488fd8c41e810df78cb854e",
    "WindowsRuntime.dll": "0d6a0d947daa5cc597e4f131365d66b6ef2444d10971d7fc425aecc9fae2bd95",
    "WindowsRuntime.deps.json": "acae75476537b481a7d610843882a1e9897317c224691146e252de770f6a646e",
    "WindowsRuntime.runtimeconfig.json": "9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f"
  },
  "sdk": "10.0.203",
  "os": "macOS-27.0-arm64-arm-64bit-Mach-O",
  "arch": "arm64",
  "native_exit": 3,
  "native_qualification": "Not assessed",
  "errors": [],
  "constructor_controls": [
    {
      "control": "constructor-after-1",
      "scope": "actual PinnedPath ownership path with injected file-handle acquisition/inspection",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "acquired": 1,
      "closed": [
        true
      ],
      "foreignOpen": true,
      "sameError": true,
      "error": "injected inspection after acquisition 1"
    },
    {
      "control": "constructor-after-3",
      "scope": "actual PinnedPath ownership path with injected file-handle acquisition/inspection",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "acquired": 3,
      "closed": [
        true,
        true,
        true
      ],
      "foreignOpen": true,
      "sameError": true,
      "error": "injected inspection after acquisition 3"
    }
  ],
  "r51_mutations": [
    {
      "control": "final-original-ran",
      "result": "rejected",
      "error": "W0-FINAL-ARM-UNSAFE-EXECUTION"
    },
    {
      "control": "final-later-arm-ran",
      "result": "rejected",
      "error": "W0-FINAL-ARM-UNSAFE-EXECUTION"
    },
    {
      "control": "final-error-lost",
      "result": "rejected",
      "error": "W0-FINAL-ARM-ERROR-LOST"
    },
    {
      "control": "final-case-missing",
      "result": "rejected",
      "error": "W0-FINAL-ARM-REFUSED-CASE-SET"
    },
    {
      "control": "constructor-leak",
      "result": "rejected",
      "error": "W0-CONSTRUCTOR-ACQUIRED-CLEANUP"
    },
    {
      "control": "constructor-foreign-closed",
      "result": "rejected",
      "error": "W0-CONSTRUCTOR-FOREIGN-OR-ERROR"
    },
    {
      "control": "constructor-error-replaced",
      "result": "rejected",
      "error": "W0-CONSTRUCTOR-FOREIGN-OR-ERROR"
    }
  ],
  "diagnostic_control_mutations": [
    {
      "control": "suppressed-original",
      "result": "rejected",
      "error": "W0-ORIGINAL-SUPPRESSED"
    },
    {
      "control": "suppressed-second-arm",
      "result": "rejected",
      "error": "W0-DIAGNOSTIC-SECOND-ARM-SUPPRESSED"
    },
    {
      "control": "unsafe-continuation",
      "result": "rejected",
      "error": "W0-ORIGINAL-SUPPRESSED"
    },
    {
      "control": "cleanup-continuation",
      "result": "rejected",
      "error": "W0-ORIGINAL-SUPPRESSED"
    },
    {
      "control": "wrong-binary",
      "result": "rejected",
      "error": "W0-DIAGNOSTIC-CONTROL-BINDING"
    }
  ],
  "source_equal_after": true,
  "binary_equal_after": true,
  "processes": {
    "build": {
      "pid": 16807,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 1.4718682089587674
    },
    "constructor-controls": {
      "pid": 16811,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.040293124970048666
    },
    "diagnostic-controls": {
      "pid": 16809,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.292220083065331
    },
    "final-arm-controls": {
      "pid": 16810,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.07659870793577284
    },
    "native": {
      "pid": 16812,
      "exit": 3,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.040313958073966205
    },
    "sdk": {
      "pid": 16805,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.07338366599287838
    },
    "sdk-info": {
      "pid": 16806,
      "exit": 0,
      "timeout": false,
      "quiescent": true,
      "elapsed_seconds": 0.07743183302227408
    }
  },
  "final_arm_controls": [
    {
      "control": "held-unsupported",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "Program+Unsupported",
              "message": "injected final pin unsupported",
              "nativeError": null
            },
            "exception": "final arm containment not established"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Not established",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "held-access",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "System.ComponentModel.Win32Exception",
              "message": "injected final pin access",
              "nativeError": 5
            },
            "exception": "final arm containment not established"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Not established",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "held-observation",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "System.IO.IOException",
              "message": "injected final observation",
              "nativeError": null
            },
            "exception": "final arm containment not established"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Not established",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "held-identity",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "Program+UnsafeDiagnostic",
              "message": "injected final identity inequality",
              "nativeError": null
            },
            "exception": "final arm containment not established"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Not established",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "held-cleanup",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 1,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "injected unclosed handle",
            "status": "Unsafe"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Not established",
            "cleanup": "no handles acquired",
            "status": "Not assessed",
            "reason": "prior arm lost containment or cleanup"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "released-unsupported",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 2,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "Program+Unsupported",
              "message": "injected final pin unsupported",
              "nativeError": null
            },
            "exception": "final arm containment not established"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "released-access",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 2,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "System.ComponentModel.Win32Exception",
              "message": "injected final pin access",
              "nativeError": 5
            },
            "exception": "final arm containment not established"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "released-observation",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 2,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "System.IO.IOException",
              "message": "injected final observation",
              "nativeError": null
            },
            "exception": "final arm containment not established"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "released-identity",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 2,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Unsafe",
            "cleanup": "all acquired handles disposed",
            "status": "Unsafe",
            "finalContainmentException": {
              "type": "Program+UnsafeDiagnostic",
              "message": "injected final identity inequality",
              "nativeError": null
            },
            "exception": "final arm containment not established"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    },
    {
      "control": "released-cleanup",
      "scope": "synthetic actions through actual final guard and orchestrator",
      "result": "Pass",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "originalCalls": 0,
      "operations": 2,
      "evidence": {
        "replacementDiagnostics": [
          {
            "arm": "held-target",
            "directory": "synthetic-final-arm-root/held-target",
            "targetPath": "synthetic-final-arm-root/held-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/held-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "all acquired handles disposed",
            "status": "Observed"
          },
          {
            "arm": "released-target",
            "directory": "synthetic-final-arm-root/released-target",
            "targetPath": "synthetic-final-arm-root/released-target/target.bin",
            "stagedPath": "synthetic-final-arm-root/released-target/staged.bin",
            "inputA": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "inputB": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "steps": [],
            "targetIdentity": "Not recorded",
            "stagedIdentity": "Not recorded",
            "nativeError": "Not recorded",
            "finalContainment": "Verified",
            "cleanup": "injected unclosed handle",
            "status": "Unsafe"
          }
        ],
        "originalOverwriteInvoked": false,
        "refusal": "DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"
      },
      "refusedRows": "same exact 26-row array as refusedRowsTemplate"
    }
  ],
  "refusedRowsTemplate": [
    {
      "case": "create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "create-collision",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "read",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "overwrite",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "conflict",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "immutable-input",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "cancel-before",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "cancel-after",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "write-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "replace-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "sharing",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "dacl-create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "dacl-inheritance",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "dacl-replacement",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "denial",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "case-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "unicode-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "ads-device-unc",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "hard-link",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "leaf-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "ancestor-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "ancestor-substitution",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "owned-cleanup",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "cleanup-refusal",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "file-flush",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    },
    {
      "case": "directory-durability",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS",
      "source": "4750c2a65365f818c4332ceba82e4d2b9819b852e1eceae07c79b46762b1be0e",
      "binary": "f4f5b86df28643f4f3e148bd02655544f2ec6cdccee060040a3db081394b7a01",
      "publication": false,
      "durability": false,
      "elapsedMilliseconds": 0,
      "evidence": {}
    }
  ],
  "original_native_cases": [
    {
      "case": "create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "create-collision",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "read",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "overwrite",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "conflict",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "immutable-input",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cancel-before",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cancel-after",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "write-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "replace-fault",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "sharing",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-create",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-inheritance",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "dacl-replacement",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "denial",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "case-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "unicode-alias",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ads-device-unc",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "hard-link",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "leaf-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ancestor-reparse",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "ancestor-substitution",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "owned-cleanup",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "cleanup-refusal",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "file-flush",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    },
    {
      "case": "directory-durability",
      "status": "Not assessed",
      "code": "W0-UNSUPPORTED-HOST",
      "publication": false,
      "durability": false
    }
  ]
}
```

Build stdout:

```text
  Determining projects to restore...
  Restored /Users/mallalieut/projects/CFD-Workbench-feature-windows-r51-final-arm/tools/spikes/WindowsRuntime/WindowsRuntime.csproj (in 23 ms).
  WindowsRuntime -> /tmp/cfd-r51-local-20260925/artifacts/bin/WindowsRuntime/release/WindowsRuntime.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.37
```

Portable-text/subprocess-UTF8 stdout:

```text
clean - every text write under pack/scripts, pack/adapters/hooks, tools is LF, every printing CLI guards its console
clean - every text-mode subprocess under pack/scripts, pack/adapters/hooks, tools states its encoding
```
