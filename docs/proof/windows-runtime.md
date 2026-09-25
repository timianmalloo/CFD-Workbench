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


## R52 hosted diagnostic run, 2026-09-25

**FAIL; no production or M1 acceptance.** Owner Ruling 52 authorized one exact
five-path ordinary child and one run after independent root review. The sole
parent is `7f34c13c3568ec31779866e061eb0a7d52dbfb36`; approved and pushed commit
is `d14a7beb609fcca9b011998907e712da702e101f`, tree
`dc7b74b1aacabfc36021d484392ff79c1612c71f`. All five blobs equal `b947069`;
all 739 other tree entries equal the remote parent. Root receipt SHA-256 is
`39d927046b1d334a164249635ec8c1e4933da9116419a23bec98ee417403ef2d`.
The immediately reread remote tip matched the sole parent, and one explicit
normal refspec push succeeded. No force, retry, rerun, main or tag operation.

[Run 36107339658](https://github.com/timianmalloo/CFD-Workbench/actions/runs/36107339658)
is event `push`, fixed feature branch, exact approved SHA, attempt 1. Job
107982790574 ran 07:22:59–07:23:39 UTC: 40 seconds by job timestamps, not billed
usage or application latency. SDK and wrong-result controls passed; native/UIA
step failed; retention succeeded. Host: Windows Server 2022 build 20348,
AMD64/X64, NTFS, runner image `20260913.307.1`, SDK 10.0.203.

The executable retained 26 unique rows: 21 self-reported Pass, four Fail
(overwrite, cancel-after, DACL replacement, ancestor substitution), and one
Not assessed (directory durability). These are native assertion counts, not
21 independently admitted cases. Held-target replacement returned false/error 5;
released-target returned true/no native error. Both arms retained Verified final
containment and disposed acquired handles; the original overwrite was invoked
and still failed `W0-WRONG-SAVE-STATE`. Cancel-after failed that same oracle;
DACL replacement reports access denied; ancestor substitution reports
`W0-ANCESTOR-MOVED`. Read discriminating raw observations before diagnosing cause.

The driver's later containment inventory raised PermissionError/errno 13 while
reading `native-fixtures/denial/target.bin`. Summary containment is Fail.
Timeout-tree, observer-fault and UIA capability are explicitly Not assessed
because the ownership/fixture prerequisite was refused. No hosted UIA capability
has been established; no retry is authorized. All seven retained process
receipts report quiescence, no timeout and no cleanup error; native exit is 3.
This process evidence does not establish fixture containment or stream cleanup.

The source composite remains
`78b61f2ae68196ecb9060382cd1e824d4194e04d287cb292f0b2b5f2252c8778`.
Coordinator compared actual Git source bytes, actual four downloaded binaries,
both before/after maps and immutable fixture bytes. Binary composite is
`b83e0cf870b08bce2f27400a6c276df0b303f40555d2af34209c5cf07708a6ba`.
This is evidence binding, not independent scientific/Data/Security acceptance;
root reviews the original retained bundle separately.

Artifact 10851791302 retained 31 files, including the four compiled files.
The downloaded archive SHA-256 independently matches the API digest:
`e3c843525004ca104ed9f6f1011a5dc36d1ac99f9a9bdb566601beca98623c56`.
Raw archive is `/private/tmp/cfd-r52-artifact.zip`; extracted bundle is
`/private/tmp/cfd-r52-download/w0-d14a7beb609fcca9b011998907e712da702e101f-1/`.
Metadata/jobs/full log are `/private/tmp/cfd-r52-{run-initial,jobs,artifacts}.json`
and `/private/tmp/cfd-r52-job.log`. Monetary cost and tokens: Not recorded.

### Durable R52 raw evidence capsule

The base64 below decodes to zlib-compressed UTF-8 JSON. Its `files` map contains
base64 copies of every noncompiled artifact file: raw summary/rows, source and
binary manifests, fixture bytes, all seven process receipts and stdout/stderr.
Its `sha256` map covers every artifact file, including the four compiled files
retained in the archive rather than embedded here. Decode without executing
content; compare each decoded byte stream to its map entry. This is raw evidence,
not synthetic replacement rows. Full compiled bytes remain in the downloaded
archive and the three-day remote artifact; preserve the archive when moving work.

Decoded capsule JSON SHA-256: `ff282e99c6783bc3d1afcd7a2e722ba6c1080bb499fb7fb8c73f9331979d49c1`.

```text
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
oZxV5wbmWG5jggiuvbUi3Za+J/i3n3M/8LluuxdEJ3O9BhEbNoRG0cchWnsBGcLzzO+SA0yDtYXzDucJ3vOFC/VL+nkORGwIwJxg
ogrX5OXgi9XBjhbaqqABUpet1PO7sQ8rYZHiXzjP28O7whlR6vKrOe7Os+RuIG8eJp7jPf/enrN4BvWaCFEqB7p4tjf0WmiXICD+
52X+JC3dQ9QV+6UYTJts0HC4x0ZLA2RMry2wFmCptaag8TNOdbTj93iUVW077iTO3cEmNfwx3sfdHXSMgoWLsAFnFthFB+IBz4Xn
a6zDBIAJ++4uDG/nWn74LnKnqeJejGuSONKaXaXG8GdAeLHuyjlbOc2/Aqy1APLQYl6nLOOG68B1FEbGs/ZjWNv1xozMKx1Ngp9H
utb8pvLyNw18/1NilPdd/vdn31RnLSo1vjNmtKHc4VVZaw7gMz3ZdX/IHVcZqj1RHnHTIJrXapn3nvHw39FQk/lxjVfUDpxxvtmW
tZ46Us3esAbXGx3fOyx2atF5hWcNP99TXEkdqjtxXJN76vL8+qVl5fnjICuZ91M0TdQ6rihrJq/wTXjHZl/TCq7Lv29Pvu/gd2bf
Lu9n/vqqTvObVua7wfeLZDGz3ozUH2ncD9lxRyoj/4D3g71v8jrP8arjwl5IfPKciVoomybaoWAgbeDTzIPktxKd4fAstmVnuA5t
cg8Tn6/jjiTILSbwjQxswGcXVuY6odwLGsjF03yqN/fxNScvcs16Ef/q75uLCdvZTMfixyGLwgZZpuC7soBg5dBGBvqxXeb7Mhbm
AnsJ5yjKatw9NzY+eLO/QX7MYlPwCdB+VtQmIfpDX2pbDQnLJG0c84wpVDGZ03K/oxcxlgNNd659JbtmmFdL01Ym2spb3b2oY7uo
sTMVdS8GrVyyO2gPa4btOvC7mmi7niiou4GyWA50sYHtpaKOI6iffUOBGLM93Js2tm2KDYPtgOensima2Ziu6Tg+/Ee6hQutrbVP
UyfD87WtRH4sLzWWzoolUvxwisPvvW7+tb5nDp8PIuNO9U6iP+WBc9/jXr+Q4+xQw8qIcPgc9pwVWX4p6l0WzE9NFHDmt1PDnl/J
n/uDNiw+3BsW/ckUTMdQXNdQnhmzzdmiLzkQlgbmy7CN2kBxWMOfOQk+4mUYBokf4JK9TVjN7zMHV2KEvQQTnXFzfwd3t156H5Pw
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
Uin9amI8ou/a6Vgr+D5maSPWpNx7Hhj2o+ntQTORUcf4LGiAesWmviDGEUB26+A/smfeHfcszOd9M8AvtVbmMa7Ln5KSndgL6+Ou
Qr3dxEbsesAWr4JMe4jiyCKsEtc7xLlnfOXCc4N55maM1k7K+RzW3Z4gs8pL7vcw/xHF/ScTSR6bq348W0ZmvyRsjvFPGLaLUVDR
+nHviBYNzmwnFQvl6oezdiqYrH3UvQVsAgnUTTm9XsYPj+95jb1IX/d+dun0PpGeOnMf2NPtbCxz4bqqvmFzyLwEcdvCM3zeFsEX
NXTMbctwDh2I/czlQOnCmZktMIcM/1jwbeF3ItY8sO4A9oMHGZhDrAdnWlHP3bf16Pvi9Afr5cwkr9UZHyzIj3OuRbbmK2yNAM/x
ftE3Dt8RYvu3aMIKXF8A3RyxLlzyybUjm0dPqaWue4GZ63yMn+czfpbeKn3tmI2KP7KL3uS33y/mL/DjSY/dVY8VfSaqe8SyQzmE
R+YQEi188zNMC9k9SOQ2lTq2Ic4C1ouR3mCR6SbBmvlP9noHFpTU5O5gTUA+w9pC9kykWgdZWOMoL5HUK13QZdjfAe+3z/0eAh70
XS2Xne7RbAWJuk8o090nkVW3Bms8ST74z+3FEvx88NG7dazJgK9sg5/NmgJeV9yKrAE+t8pI+nBv6IYPMfR2oAfw3q3kW1sEsxpe
J3U/kIc3kw3tLZ6fiEEu1WZZOFk9XL/tFM6cqTMoM52Jzq+mXhNZIu3c3GAuQyS/n4ZyWiZvfH3+Ryjhs99inzLXvcx2lvCpfu4+
lO+hfA/54CV98ChPdGZvsZU8zmM31CMrlPtNTubM5xcYos/rthz/8tPqbaWvfUsOoSDnfB9dV+DzU/3t9vpbwaS+S7Fp8JmM7JRk
RYyZh2/SWXHsdpMPf8q+XtZ/N6OaPLIS4jQEJpoW+LzEnryAia3gnsepG8FzBuBrQ5d2M93dJyctWHsOZXc7TbNqZd/92BsV937p
w+W5yXGZPUj6dN+wb3JWD1g8e9OViZM7nSyz8/F6x5pdEq5z6BcJ3++Yj21Rfpvy25TfprwQ5YX++LwQ+cLkC5MvTL7w7+oLF38v
1eMW9Mzqjf9GzP/z4+e2c4sFHwx03ET/e3lT78hK21gs72WpG8mv/kS/ukV+NfnVFfWrKR/9yHy0Nl1J4Muo1/daCxmfXth9YN2R
+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
GYbGeHP+7lAmJhZ8sjnEum5sScFnhouxi59ZNYLPLaAHA8ojnK7qAm/t3s0y1zssHvCpD12dVcjbfdiX7OyDgWeDXTDercfOaMAD
WzY0wV0idr4V53ch5UV1xH0eOmQgtBtRyhf2Vv1Gm9GIN24PZ487wDJ+ymPlspxv1G30y2eUV66o11sO//X88+fXC2IDfi7zebn8
/WNRbufTa48JzW2dHVuzmiOrObKaI6s5spojqzmymiOrOf5aNcfJbu7+Or1btznq/DyDY3U52kO/v4+VBXsoJnqWB1xeHDemWGo+
gw2KYDsjdhb7q/V6xRqFTXYWm53F/lJnsdOY6pasnmmAv1/zMFgPMtaDjPUgYz3IWA8y1oOM9SBjPcj+IT3I4h3+0pVw7paHEeW4
Zyd5bMtjdQmK64ps7if3cDjbXlyCxU72aDhqD0r4GB/owXCGvl+EtX6gx8LZ+vz52Gpfvw5z8j+Apa7Rz+bsHPy1sNM5/Wv+qpz7
NbBS+cwxFneyuPNXiTsvzHVrH80Jb3EnPePQZ7yiL8Urqr3peFfGK2K8oi/GK0p91t1wY8NTXil+Poctyc7z4G/o9ztxPuMdMd4R
4x0x3hHjHTHeEeMdMd7RP6bXwRYDdWsn6zifq4fn5yYuqdOc1LNjuYgSG/4RPTqde7iszvJxPTk/1/DpdZSD82pecY7zQ33Fr4FJ
zq9hX6uP+DkY5K+qWV+jb3jxmVs2g4HNYPiVZjBcVive14ldbFY7md8or0skTTx7hzu0Q2fLeHGOogwrfkVf3jPPq6ce0/G/1JdD
Vn9SbbQzrdfaE/hlLqqtcwYDrlOQM7gGx+zsOR/XyhHsccpmn1MrLNXJn1f/O9775u+SQyzrKVHsf07G818khwiccl1f8rhjuyFX
9oMFLFjW/2o3N9ulGJgDrnmF7hxgm4/WDDezI4Ykle/SmuHUAv6cl9UMH/Rb8md/Wq/2av1Zd1pv9WqD2t7ZlKyPSsa3EtqNJq8P
zaU57PNteoYkHoRmiFgCWKJdxeeq9ahdbXJWtYN4ohvp1e5UD0eCyTeXbVr/azSXZjiI9bBJ9FgXjcTN18DofLG4vsx6q7Qm7aoR
6Q0jblfduUlrQDG+j80lNGGu0/MiSVMw4i69bqJXjdio3hCjN5jqcT3Wkw5kLAqNoRHTGp5eHY+NuDXOv1+FmPHi2eTrG9t5qPet
6eneHq0I+zEHDpj2hhEwYpRgr9+dOOtvoU9EYGWX5OqOD1baP2XVRy3vt2NttX/kGXby3eW12LutPNDePND/BHo9vwtv3iiGM6qj
/frb2I2NJ+qTIa9kzVcbVk2u4AzHK+zKqsdWuubtQc3qlZ/1qF3x7Nam3nWb1S6nXLthLsykEus89hrxaopfk+ncqrq8VbUm7WFt
DhlFbEtl54Y3eAt734c/7XBWOMbX4xB2Z2HGpmRVK1QuDnqdfLLPnqzOgNG+frkY+Pw+hT+xP2GjyJ502TkTxvdh50zYORN2zoSd
M2HnTNg5k1/znMm+7P51WCtyHm4YN4txsxg3i3GzGDeLcbMYN4txs1hPqL+8J1Qt3xPqAF/nesVQbHdZXfnn1P9+rAZd8tmTMdgx
e1CSB/xAjHWGvl+EtX4ghjpbnz8fW+3rV8FcnY9jKVazLs1j555lkZiDOsEaSXn8I61w0lRe56g6+1hnms9ltePFO53fUjon4ApY
qXR+AIs7Wdz5q8SdncvmSBTME9vN29So32X8ccYfZ/xxxh9n/HHGH2f88V+MP76ZczmYUcx5ak7uek7tpfMuC9a7eJZu0gS+6iyx
7qJ+1izd/tmzdGEnny0+ejOH89F65uT6s83GeOzE3VfrnmKOLrVHUTp/keYbh94TnUnpxPWZVeBb3NXMsc2aNKJy/HyQf+nnZ+VW
++fNoQ1zs3KX1uSsWblJ/l76/COzcs3hmfN8czIAnL78wDzfXj3c14eHE/Kc7tPD2f0q1jP8Oj+9b8VDXrfobBxaU72j/M/JGXNM
o6/Bgdv8zf0x/7RZx9aJ2YH7+xsdnW3Yz/VCvP5sxf7Kpy2znvh01rceNuftwhh1swbH7NPu53T4KOCvaLbKGaaYA3bnic7cc+js
yuL53yHw/mtpHX1nHuIV573u72tpzLE7J/c6vXp+cMbkwZzNy+YLH8vh2Bf6UXPZPHP25M3CmJyaKZvNp7wAN+ds0z94LShGgK2X
TtXciuZykjNmoW/nyV55TUtxMZ0JCdxqTK7Wk/ZH9+ifOpv3ED/9nWfzHo2H87PZT9c2S/3CwUzu8rOggyXi7+VmbvfNj83t3Z3v
vYmDIAfNBu29tzeL+vzzU/Rv93IvLdgX2MXMHk6wFxTT7P4+dPjuKo7X7tj5qE84H3UrLoEb2fkodj5q4vHRm55keOBy/TbGkLsZ
zfmbDzrrpfrVeqn2EJMvWS9V1kv1S/VSTe3HXZoXrydsTjPjRjBOPuPkM04+4+QzTj7j5DNO/hfm5IfNg9getnHschfmv3Cdi2PN
x8Eb4hI6m5nFm18y3ryheQSi37N48x8fbz62IjvO/HCXcveyXO8H5rjXIVtkSnOT0M8Z/HTIZrl/qVnu0M+aaFT7bJY7m+W+tIbP
WY6jlsZPqS82NpypNU7azrPb8QeQ/3qy2s8n6Mn0DjLpxK/Mx3+1nDK1bczHMx//tXLKOzZjxvrpsX56rJ8e66fH+umxfnqsnx7r
p/dr9tPb/X6/J0WcO6twlZh2y++Nbnfz2Sd4vktj//xJnGK4VR/vvXhgw9f8tDz5f/bXd2Prdu1+xnvKznI1BmNrQoDdEI8+GhHL
T32p/NTiLuwIkDOWn2L5qd3+Ubs8g8KYp6g/jtsAfsueCxh64ZqIRx8oV/WhgpjThb/Q6bMIeqJLwFbiXa/uPjTScyq8dU82e+kS
xLF7dgMxLGxm9EjzRg7i350zoowXxXhRjBfFeFGMF8V4UYwXxXhR/xRe1BKY/tmlPIL6lk9QIr9fMjY0aYzxYFBslvY7QHyV5Pt2
sLNwf/lZuHsR69FfsLNw7Czc15oVVn+j+VF6/6x/S30JGcbPtaU9pPmI+msuZmS1TlbrZLVOVutktU5W62S1Tlbr/KfUOh9bkdWI
llRuYVP/e1x+W+OdHjyfU0u5nO86ptxJ4HxgM5J4QxoTRtw6f8hmTX/5Xipiu2ouWfzI4sdtvqf1p9ugXOsVb/oAR31Kr89z7UhZ
T6yfVtvM5b2EAbVbwKDZ7JG7IXkHfp0WzdbJ9fWbZr23jtfJjvrzCWKoV6u2jaNZL9yf1Qu3FR/0wi2YYbCRx+lu79KbgvlHG73Z
6VeqH/Y3XnFiTvQSPa8n4aU92+Li9T2zd92J3oFbjPDB/qm5dy7HqXs9+7IeekWfO+hDl/aYK8pZ7PSha+6e6y19L1o/obaArM/4
4bqVHdu17jmL+35vIcasYw1gL753n3AfYvGjEZ3f4+RyBvv3ac0cwSrnFDx6zw5sfcl7nWk3DnSE5PX4hhzq8cHfLPN/A706/Tdn
2IsT+tuox5f/jRVe/Df7duKAp7hd4yO9GHNcuL+37Od1+leyX8dm+ez2lhwA03T6UrU7MPo90pz1+tqw2+98oLdkWnef3g2NV2tY
f/MebthZwa92VrBqEtZ/jp0V/GJnBVe97UlIOZiw7XPWg45xLRnXknEtGdeScS0Z15JxLVkPui/cgy6p7WPmDM/VEFc0Bsdqf+uZ
bfWd+RYZJuhcXLPbYEfKzbIgL3u2lsWff3n82XyjuTLjlsWfLP78Wv3PD23HgvWsYTxOxuNkPE7G42Q8TsbjZDzOX5PHuS+7n36G
ZlMbpOfvYqxp2lthPWv7xPzjM2xCFnt+Tr+KD+aiLrYBKYaqpnnA27RuV14TOJp7Oprn5PyHSvQRPHNmrukDOt5aeOkMIut5fW70
wjrEZbmlK+l0oW7WuSM5dGNqNeZZHJo8651+R+7fjivAWA2Kt9rcovKf+7Ic/Gf3bMlhwnvszbuz4hEe9dN5TFN1eImHbHB9gfI6
pMdSPNXQyuoAD+U+iZ7RlTgLcfNJ7JCrQSwSc1AneC4pn7uXVjn+qbzGXJ39PP00j83a8eKdcqKO44kDvPajclhabyqvi5xZQ72a
bf1ILPvBmuoltvbM2PZkjfXHbO9lse5Ha64/xRZ/JPa9Zg32iG1excKX1G4Pr3PZrF8zl2+/Sl5xy73r53oTnuAP6sv9XCGdYW0O
SXT+mezt/Osvgitzs+s//5kGM/dITpH2pUAslZ0DOjI/fs3BvL9sjnuiF6/D+vzD7dXrTnsz2bPYM8WXwHBN+L0m4rTR/Nga9Pnx
ePXM5NjneogdLerTh6tekTR3MOyM7Eb9zWosohP85tPyM93RpZJc8Hnc2EM+aSnnNX2/8Rh2kD/1uRzvszHG+67PI1VCcyi9OUIH
zwQsc1sBLuqP0jNL95XEbQzC1BcMO2+HOLcQX2z3Zj3b/Thf/mfOuV/u2azFl5tz/09ci0foFmxjqltHedSrM0z9LZe7nO+9dyaj
nLO+rjemZ5iyM2DHYvYLudxJsyxOSuwG/G/PvLI9zXPh0/W7uUxvO5/fv+mYH+iYDy3q1yqb88Fn2NNj9Sv7lJ8M9/xkWYwNWaMc
NfNxwGX5l5uFMTllb4s4+mU8rENMQjGWV6vHwA7LX9ZmXNRPm2KUwbPFS5Wd8+NXqY2b/CKCryRuPasVX8hvKfrcii+z7SPU3Zfp
y/kwaT/ju6H1Ctz5xHpgf7Ue2IjX2DkMxoP5WjyYnM1gZzDYGQx2BoOdwWBnMNgZDHYGg53BYGcwvvIZjH7hPJtuvCAuP97FNhf3
zF73csOzSHfDfOxT3gtNevdu6RmLVDb39Genh3bkzu8Gg3afDDr9mnY/qA1qd/1uqz8dVAf4fnhPaoNBq9KZRvr9oGuxfmmf0i9t
YYSsXxrrl7biKH+HPAkr7BWrIzP1hRUaL4yshzpB3Dh2aP7wFlhwWYmzXtdkaiM2N2m/oMfpyHo0JCfWleZ3i/KDZtZ9Jca/iX1L
OR30GsAQywo9H8avsNmI9hhqQjbhC2FzaY4S+z4Ri/qQ/Gdrq1ur2usgoHngjMOx6lOV4U3Kq5mt48ENBsjnwBrAijtcHTrnoJ5k
2BmyydE8uvQOuU4e7isdrP3MBjZIdZI+I3yxVX0atbg0/l7VjHd6tR2Jw0trx/gbqn/2MO0pvn8W5TGtwa3x49oOPhiBeVj/mFj8
4t2ETbiLnzfvZIQjXufrE33Y5Ns9k9MbdC7YlKO6YyQj2EugCsiNyddEq2FNzV4Umb0bArkM9cSYwv+kdXAzNDn4dNg6b7rTW43G
O9h3/d0VKs82P0juyHpem3FPMR3tNVX4M9zdBVa0sxwPrt1f0h6eRtIU9dCYQDcEs9dP8D8Hvyda1dqiXa2M29VoDNwt6tVBZITR
1KS2pwG/06uM9aQmmnwX9tflzGS802eJ6pP26i61xG1EL8CQ75ucGmlRXhatE79T7GoJrbFzL4UreZlSnA+7D1/SxxqMSDvNd5k0
Bzmmum6FNehwTTRCF9inBfnAMydNrFNr2m4YY2Nlk3SaM4MPoHtQ8FxrDo5097CZdzcDDqO5e+iUNF/l73Ht8RT2LrFwf2CwJWxd
0saewpdB963YgN2DnxMNOiuO5l+H+rw9hA1ORqI1NGkuDr8fjE2ePvuIrt1k+zzNN4uPOPv7YHIneNDNWeykeUdt7H6v7JwbJMLa
B9w9bHDqOpf65g/J6wrLJFaIZ4bfhbxFdF/bw1ZsNODDhxb8tCnpYRTTZ4N3xLuNYLf6gk79NM3bDU3RaOgS1l+0ep2FHtdEWn+j
srfCMpu88QOf5TQf+ILaSVQkfwNxhScWes+YAjOM6drhOcfpukACzJ4Xr2RvqQPHwE6NKe6wgDEMrCX8vISf4z2A0eLm3Eymgh7T
/OSuvW1t/GV/u59TZ8X7tKp0X5rEwPV1vgabUYPMdyfAC5ClLvRxHOlDmjOnfXGxp0OsY3W0aKf5dcg6VtcK+5A3Cz7sBvuuSztz
PScFco7vB3N3Kf0X/vwty7fj/UJdAGZawI+Odaw51XOjh70JsUfVKX5P5w262I86ZLgSw5dyeF6Ko8bAYYJRbSYW3eMG1rGqQyd2
6qRF6/+4fp703Ch8i/tmw745WawT4nmgY/oCukOMRncKvZkDd03oWtGzrlS+sdZYh24ITEPgu7EOiN9i7FFYI6l8DTuS2cPzJR7W
bCrkbOnWryc5Wdn8vD8z7zPuTh4XXbvO24r875Vt3vKTeydv6/nbPPzdsJnlhfP1ihwX5Qq4iNZ/51te/afNxtzFHqvaW2vuxlqa
J1jlHzY+13wcwO7s57ko9vX4Q/5oSX/XB2NqE4oDNM6JB9zddGB292vScYsgFtuP+bfctRq05JY8IHZ7MSlPjx8X/+x7993hF7QG
sPKFFIMc6id+X2S3Qvj5pcM/7eHV5uwujTMQS9CafmHf29a9hTg17SG39w5392TswhYgngm97/B7WT/c/PtfUPOAP8A7WMC23aTg
XsCvBuIoWv/I1vSOcujTfKh+8F5u9bnt9YHHanWC+Jj7c5jlbw6u+3AjUy5+xk8bTDu8hveIXldroa1itS3GzdW503o+d8Dzzewj
MF9J/v4R+Ho4gOyX5ei2vRIe+CMcm1ib0Hy1t+Lr5GT2sH/yfBWPwFf0DzkAKd5uZe/Sg49IOtBHyjcYSRRrwscc8vaoLx4axGvU
J84qL1fMySSJ1Zjh55DVhkQxMtatoD/wo/FE7aiRYculPqT22FxYFGOm/RG6Ubva4UzovEXxJTCI3oAv6Y1ho+A/KV4bUjtxA19M
/XxnCTszhQ2VgJMRU8Pm7XLj9vK7B/nHx0qWV666uGbzsLeuAMy/Ov+H9UIs7UppPq1K88rAFr3pwd/AFtK8PGcOW68rHsBR2+Ly
3ZeH+nxbP8bP+nTG8vfoDWtLz/CENAdFz8i7vP5Gr1/QJzrn+7CuxIRdbffgg1N+DX1W+LawSzGdRHtXtDO8RDE7MAIwe4KYFZg0
9Q1D2F28K/wjZKNP9KQ7xZ6V95Ytl0UBOH1+tixiXxFvJMAz0jmy2Emed20kvl7lKnr6Mu0pwEtPjmBwK0xZ0Dt510/C9ySVSE99
/5Tm9/Du2EvgPZrfS58vxvoNawKNy/TeOAbujGkOCX4d+92d6NRX9mDDeuOxQX8X93O1ry2fJpfn3PbsqGu5vJBxL2Lf+hLiu83/
kPW9XqBZzgDvDRuT9Rdf5RBo/Pdj/q+wv3l9CZ+DdTC4tK95nMrpoU/CtWBvKf86oV8X4t1DWaZ+gO5bQusTZta3PT1LBBv56i5/
wPfsrdE21td/Kdvf7rnn234apyY3sL0FOnq27S/k4+/gBsrDbz07UZEMDCBnNDc6GKdz3Oql8eAJ3a0tEO9Nqd0DzgYORyxUBdpN
WmPEI7Bp4ynwJ3zUYGo1apQvNdVjHfEK5SdN8XMDv79BzFQJsX5Li2Lkvbr1pbpLc7p4vgNdBVYZW7z0Tjmz5jIn2/kaY1xfWoJ5
WFP7IN5a+ZIyTLlcY8ASfFngzym2xDsMvTdqY+2hnrM7v5BOQT/05QU6Bd/h8jSOYjp1JZ2iuabhfIb9yOKdx+6SxlL7cTHlUnmT
A17Q19mDCTcrfZdRyVptcu67nKNWeg8HOLSkHrXwhhHlk5Sd7dzh2Gr7vOHd67ybj1mMluLo3LoW9KxvZHVrI5wSvQDD0rNiqzOg
wNo6B0xK6ybAWnpC/6Z9cP6A2hN6brmbxo2XcvA+1z4exDufk8/Mxzv/KeRRbbmwQb4f3M1bZq92ePfpWbXOgY6t+Yp/O536QU5m
aV7n9Hvt5OlqRfzND15rlfM7aQ/0v489qI6Wxu0F9iCZLttVxKvMHvx8exDq3G68iXXGv+aBPaB5ix+zBQc5o/qwVoAxH2j+z0jz
lr8QphSMxDw33zRvV2mOnPK79B/ON21y2WdhwOaC1i+ssB4ZwHqUK2M0+qJR7U5o7dJA/NgemnOL8hkaTYnKIT6/oBx/vWqMDWBB
o9qKdXquKYlCs0frweYPY0AjaeZ9VtjnjPlBnQTy9TqzKf/0B2OrLEe3gI533ygXvzsY1wvyFs+UG/tL4bSkc4FdvoFcdES9B5u1
/Lhd3l/rs/KdIa1tWpCzPoGO0PNFnNUbcQZk0mr0iVHFv2El0ntT0QwjxCOUK1CP05l2fI2ndS6d5qGrZtIeGmOdH0T7+c4y27Pm
dT0IufNF87uwn+jV6W7uDna1VlTDon18AtgXYB1te5ZkfQb3XsN6d6kdQ2w95pqjZ+3b//717dUd+7H97d8EX45tXpK//fv/vtkv
s0lgu7PXP5zJ4x/DyaP3NH/tvj3OJrH/x4sf+farv/fj3z3/+fX38PXp8du/v9k8LygqTxRic67mKp6kBY6scRrPc56iqKIdaFzg
uKJg845k274i8kLg+5xti4Lm8d/+9bFHiCLc3FNV35EUzxdsQSCCLTqS63uiZ6s+8XmX2H4gabiXSlSPV8VAkeVAkl3ZFzlVcTX5
gzf3Fz5uTojgecSWfEf27UAVZIW3lYDjVY/wgud7nk0C3uNdKZBFThM81yMc72CRFN7XRI774M1fsn/dp8dgMlrvgssLnC0QxZZE
z9NkznEdLEwgiEog8T7e3pVlyeblQPEDz+YJNgkLpdmS7So+pyl4FjyB/bL8LbYfJ4H/OltfmQ84l+c4X1UCX8XG8aovS7LqqQre
2nE4Eqi8I/JBIGGnJbyYJ0O0eM8RXAgC79Mrv00i74/nlyfXf93IjST6aqBKiqg5quv7Pi/Kiq9omiPYDifatqNp2DAqT7zmYWN9
Gb+RHdv3IWwiL22u+zrz/JeX32eLGa7qC1hgUeQ1NXCJS0TNDpxAdFVNkwNH40VesX2R+KKM22qC6NqiJmkacRRV4h1Vyl316W22
uirAju+SwBElATf3Asf2uACS7biCj4UVeVn0FFHlXNkjGq9osiDwssoHGlZXdTQbV8V2vc5e3tzZ08tv+Hr28hS97i8Jh8UjTqDx
UgDp4QRot8qLGu9pHNYp0DQbK6ByniRAsEToHfSJJ45KeN+B9jllt/nZK1R2k+2CQeNFVQoggE6ANXM9h0A3iG1zHESC4wjxnSDQ
AsgV7smJiuNLrif7LjRKIQIVGm9ijx6fXmcTt3S9eJv4sgvb4nIQRBUGhohEEWxPhOhxioTbc1TocU3RVSTc2wlUR9EEXvE4jhdL
7vKzl6vkHtvVokKOXZQ5kS4PbKLsENFWBE5QAuw7ITBwksPZPuFwYR6PrzqC4sl4xwCKSMUrgPJGv9kvceliebbPK74ALYV08cRV
oaueJnmST9Va8/G8nqwIgaPiPqLgKwrsG75XZRf6T9Tim/zstSq+xXapPJ7nHQVCo6oCgbzg0h7UjHCiZHNElGQNt1OIqii2DQMi
O5Is85IEV+Pyqstlt1jM3l783+zfYfFwSdkPiKRILq8QCBKn2KJsw3E4kBcoscbJjqviPwKl9kXHEwQF76hIeA8hgKVzdy7prC6p
iWpAXC4QXLgfkScwCfCFsifDFnAqzBtel5OVIOAkLIYYqAGHhxOo7sOwcNQaP9qzybu/v4kq1MpzFFtTfV+CEtkenBtxbegQD/Ms
2qobQMckyIemwfEonAJBwYfhooksidsL/+yN2152u1nEU6hltAn8M2QWMm4HogMjJ8p4Qt+V4JaxhlgqweVkkQAf4BcazJ4Lrfbp
ZV/g+DYvj712ZYmnb4/nlOGZBInHzvDYMsUJ3CAgnCrb0BvPEV2Rt4OAd33XkV3iCQ7B5V696W+Tx+Bpf11dmFvIOacBVGkOtgAK
Z8OduY4GTdEcD/5HEgWsMQc7K3g2NTUcHliUZU7mcpf+2Su7e+Ht2vqSL8Ivaj48gURsESJJVNFTZaJCmQWPczyfvj38h6b4LoCR
BlngONdXiWQrdnbh/XVQYCVUR5J4AkFSINycC8+nER67BB/j2S7n8YHM+7BIkG8Cf4etwy0DKIprq6urfsIS5N9etGVNBFgQeEeQ
iQAQ4nsEwJLX4JZVnieAXIHM4X6y4AHjAAfaauCpnC05QEcU970+vb24/vrFA8+Dwmt4N9G3cUGFgkRblEQVuizbKvwV9j8QbAqb
HKBK1eFtToGUSA5AJdXY17c4BoBaXxH4CxIlaj6BGHEe4Aw8oe1BXjTslQJ5dwLFcRTBx9VEeEXVFvARyYNZc4ggf/vf//4fGxcH
Vw==
```


## R53 author freeze: blocked by new consumer discrimination holes

**Result: not ready for dispatch.** Requested `gpt-6-astra`; effective model and
cost: **Not recorded**. Ruling 53, isolated branch
`feature/windows-r53-discrimination`, clean base
`7471b79fbcdcd29833fb3b2cca7ac04c59f13766`. Official implement start marker:
`2026-09-25T07:37:09Z`, session `cfd-windows-r53-author-20260925`.
Proof captured at `2026-09-25T07:55:36.915750+00:00`.
Four authored paths only; Coordinator owns audit/index/V16/defect-register closure.
No remote operation, native Windows run, capture or production mutation occurred.

Goal/done-when: prepare executable, source-bound discrimination for R53 and hand
it to root. **Not fully met:** author adversarial negatives expose six consumer
holes below. Root retains the veto. T2, fan-out one, no descendants. One coherent
implementation and one evidence-directed correction; no automatic repair loop.

### Observed local evidence and limits

| Claim | Actual evidence / oracle | Confidence / residual |
|---|---|---|
| Version-bound ABI source inspected | Microsoft windows-sys 0.59.0 crate; archive SHA `1e38bc4d79ed67fd075bcc251a1c39b32a1776bbe92e5bef1f0bf1f8c531853b`; generated FileSystem SHA `632b876fac37a8c715aa90c99e974923d0c261b91ba6682bca845ae56ca5ec53`; WindowsProgramming SHA `dc38474a2d589e062b0de64b55e9c2ca8eb0d345718c28d28675b44f1257ce78` | Verified local bytes; no native invocation |
| Executable managed layout | Actual apphost emits class22/flags3, widths4/8/4/2, offsets0/8/16/20, size24, 34 name bytes and 58 buffer bytes; six receipt mutants rejected | Verified managed host only |
| Build and host refusal | SDK10.0.203, changed build zero warnings/errors; 26 original native rows all Not assessed, exit3 | Verified macOS arm64; all new Windows arms unmeasured |
| Prior controls retained | 6 orchestration, 10 final-arm, 2 constructor controls plus existing mutants pass through actual compiled apphost/consumer | Verified local seams; no Windows acceptance |
| Targeted controls | 141 JSONL self-test rows, 38 new R53 control rows, all pass; 20 new consumer mutants reject | Insufficient: six additional wrong results accepted |
| Actual driver ordering/finalization | Four controls call actual execute/finalize with injected runner; unsafe tree/observer cleanup prevents mutation; mutation fault retains prior probes; failed quiescent UIA records Not assessed and reaches mutation | Verified driver flow, explicitly synthetic process/UIA receipts |
| Process ownership | Eight actual local runner receipts have quiescent=true, no timeout/cleanup error; exact PID, argv and duration retained | Verified local process groups only |
| Source and binary drift | Five source hashes before=after; four executable-file hashes before=after | Verified local bytes |
| Portability | Portable-text gate clean; self-test invokes real help under cp1252 environment with UTF-8 subprocess IO | Verified local gate |

The initial RED required-consumer assertion failed before implementation:
`validate_discrimination`, `expected_denial`, `ordered_phases` were absent.
First changed build then failed CS0136 at the ancestor access-error variable.
Raw source hash `925173a7591681b89bee230635f971ab2c4bd16e1cd91b0bcf2813b4c51a7d70`
and failure-complete summary remain under `/private/tmp/cfd-r53-local-20260925`.
The single correction renamed that variable, strengthened final arm/expected
target checks, added metadata-before-read comparison and clarified probe flow.

Corrected source composite: `f18e47bad73db12ac06f64a2d3cb7f76f2d47ea811374190580d34c992d68141`.
Corrected four-file executable composite: `443a2b732fc933ca3219f7888bd9143bd1e0e1fbdb5a3712bae08f9945e7152a`.
Raw directory: `/private/tmp/cfd-r53-corrected-20260925`.
The four executable bytes remain in its `artifacts/bin/WindowsRuntime/release`.
All source/binary manifests, raw controls, process receipts and original rows are
retained in the durable capsule below; the capsule excludes binary payloads and
does not replace their explicit hash manifest.

```json
{
  "sources": {
    "global.json": "6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df",
    "tools/spikes/WindowsRuntime/WindowsRuntime.csproj": "495225c72823bbf7a53a54028d1180a89a10805e69db034cee0a318cdf474c8f",
    "tools/spikes/WindowsRuntime/Program.cs": "e71377518f92cc5796bde332c49ac39e4928ef33ef3de09b8f219eac3ef5c0e4",
    "tools/qualify-windows-runtime.py": "88fbaa73ee5e9b2419313e87395f60ce5bc9316bb2de77b4d0bf1ab46925d506",
    ".github/workflows/application-windows-qualification.yml": "03f23cb26ee23b9bf4b79fcd6929fc886c3c857573b28f8ef84c9878e412a832"
  },
  "binary_files": {
    "WindowsRuntime": "74b7afee17054765efd90410d680944d6c034da40488fd8c41e810df78cb854e",
    "WindowsRuntime.dll": "dbf66500ec01b64aa13a4732f2e3e9735cd62c3976f53219c89c196237777a4b",
    "WindowsRuntime.deps.json": "acae75476537b481a7d610843882a1e9897317c224691146e252de770f6a646e",
    "WindowsRuntime.runtimeconfig.json": "9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f"
  }
}
```

### Blocking wrong-result observations

`/private/tmp/cfd-r53-adversarial-controls.py` runs the actual new consumers.
The retained output below is a failing author gate, not a passing test report:

```jsonl
{"control": "missing-old-identities", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "old-identity-malformed", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "missing-parent-chain", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "ex-call-wrong-source", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "ex-call-wrong-buffer", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "denial-bool-integer-metadata", "result": "ACCEPTED-WRONG-RESULT"}
```

Class → sweep → derive → prevent: a consumer that checks relationships without
requiring every operand can accept jointly missing or contradictory observations.
The targeted sweep covers old/new identities, parent-chain presence, native-call
source and buffer operands, and exact metadata types. The executable harness
detects each surviving shape; it is retained here for root's corrective disposition.
The compile-shadowing class is caught by the changed-source build. Coordinator
must record these classes/control paths in the shared register; this author does
not edit that unleased surface. No acceptance-met or all-verification-green signal
is appropriate for this packet.

**Still Not assessed:** Ex rename success with old handle retained on Windows;
exact old-handle A/new-path B semantics and private DACL after that operation;
native no-replace/cancellation/foreign-cleanup arms; native list/read ancestor
denial versus zero-access/released controls; Win32 metadata reopening of the denied
fixture; actual Windows job/process cleanup and UIA capability. Hostile same-object
substitution and directory durability remain unqualified. This packet does not
reinterpret the R52 failed originals or admit a production Windows store.

### Durable sanitized raw receipt capsule

Codec: base64(zlib(UTF-8 JSON object mapping receipt names to exact text)).
Known task-local path prefixes are replaced by `<R53_TREE>`, `<R53_GOOD_OUT>`,
`<R53_FAILED_OUT>`, `<R53_EXECUTION_CONTROLS>` and `<R53_BINDING>`; no result values,
source/binary hashes, error codes or control outcomes are rewritten.
Decoded sanitized JSON SHA256: `0588a5b10b0f0da70e4fd3ddfaac9e2a69e6d911164a00584caa7ea9661f7cb7` (386992 bytes).
Original local raw JSON SHA256: `a8c107cc8f0e3ee08ce991fd84ad78bb286c5ebbbb742a5f9f11a50edfa65d78` (388675 bytes), retained at
`/private/tmp/cfd-r53-raw-receipts.json`. Selected binding excerpt SHA256:
`ee18baa371349e1c662ae4d2c8de27c40f10a04da5177c2a07a622453952741f`. The capsule includes executable ordering/adversarial
harnesses, all current raw local rows, both build receipts, manifests, process
receipts, SDK excerpt/provenance, self-tests and portability output.

```text
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
```

Completed: bounded preparation and truthful blocked proof. Remaining: six
consumer failures, root's independent review and any separately authorized next
disposition. Best next action: root review this frozen packet; no dispatch.


## R54 strict receipt/schema correction — author freeze

**Author result:** the six R53 false accepts now reject; root's independent HOLD
remains until frozen review. No Windows behavior or dispatch is admitted.
Ruling 54; isolated branch `feature/windows-r54-schema`; checked clean entry
`e7560d7b1253fef5d84a056d3bd1491dc61e75c2`, retaining the exact four R53 blobs.
Official implement marker: `cfd-windows-r54-author-20260925`,
`2026-09-25T08:05:37Z`; proof capture
`2026-09-25T08:16:57.959128+00:00`.
Requested model Astra; effective model, tokens and monetary cost **Not recorded**.
T2, one author, no children. One coherent correction; no additional corrective
pass used. Coordinator owns shared audit/index/V16/defect closure.

Goal: repair typed receipt validation and realistic fixtures only. Done when the
six REDs, schema sibling matrix and valid producer variants are exercised and
source-bound raw evidence is frozen for independent review. Production/native
algorithm, SDK/PInvoke/layout policy, access/share, workflow, remote execution,
capture and section work are outside scope. The change surfaces are the existing
native receipt writer → Python schema/consumer → synthetic fixtures → durable
proof. Program.cs adds exactly `evidence["directory"] = folder`; the directory
already exists as the arm operand. It changes no native operation.

### Producer/schema correspondence

| Writer | Required consumer binding |
|---|---|
| Discrimination arm and directory | Typed row/object, arm correspondence, directory basename, arm identity, nonempty typed parent chain, completed safety/cleanup |
| ExArm old/staged handles | Formatted old/staged/after identities; old retained, distinct staged; A/B hashes; target access/share/lifetime; boolean publication/cancellation; truthful save code |
| Snapshot | Present file: true exists, valid identity/hash, null error; directory variants: typed attributes/identity/null error or observed missing error2/3 and null metadata |
| RenameEx | source/destination derived from independent directory, staged identity/hash, old destination snapshot, class22/flags2or3, exact declared UTF16 byte count and buffer length24+count, result/error coherence |
| Conflict/pre-cancel | Native call absent, not an optional contradictory exCall |
| AncestorArm | paths, uint32 access/share/flags, boolean lifetime/movement, before and surviving identity/attributes, coherent missing-path snapshots and native error |
| Denial metadata | exact object/type/range for uint32 attributes/links/access/error and uint64 size; real booleans; valid identity, nonempty path and explicit absent content/hash |

Validation precedes nested reads, regex and relationships. Stable refusal codes
are `R54-OBJECT`, `R54-TEXT`, `R54-INTEGER`, `R54-IDENTITY`, `R54-BOOLEAN`,
`R54-RELATION`, `R54-UTF16` and `R54-DACL-SCHEMA` (existing DACL refusals remain).
No comparison of two absent values establishes identity. Declaration-length
checking is not a native-buffer byte attestation. Unicode controls exercise the
existing UTF16 builder/receipt contract; they do not widen native path admission.

### Observed controls

| Claim | Observed oracle | Confidence / limit |
|---|---|---|
| Six original RED→GREEN | Original harness SHA `d01933a5d55638d4dbf1011e46b3d763795db83a8bc6769bc29234946b6afcdc` reproduced six ACCEPTED-WRONG-RESULT rows on frozen R53; same six mutations against R54 all rejected | Verified local consumer; preserved RED and GREEN below |
| Required fields and sibling shapes | 3225 R54 matrix rows: 3219 rejected mutations, 6 accepted positive variants; 3366 total self-test JSONL rows | Verified finite synthetic schema controls, not native evidence |
| Variants retained | Nine realistic arms, error80/183 × error5/32 combinations, typed success/null/error snapshots, UTF16 non-ASCII byte-length control, valid denial | Verified synthetic producer-shaped receipts |
| Real entry/help/UTF8 | Actual `--self-test` exits0; prior alias/order controls and real help subprocess under cp1252 environment pass; portable-text gate clean | Verified local command paths |
| Actual driver/finalization flow | Four execute/finalize controls with injected runner preserve prior probes and refuse unsafe tree/observer continuation | Verified actual Python flow; process/UIA values explicitly synthetic |
| Changed receipt compiles | SDK10.0.203 build zero warnings/errors; actual compiled layout, 6 orchestration, 10 final-arm and 2 constructor controls validate | Verified macOS arm64 build/local seams only |
| Ownership and binding | Five owned runner processes quiescent; before/after source manifest and four executable hashes equal | Verified exact local receipts; no native Windows run |

Class → sweep → derive → prevent: missing operands and Python numeric coercion
were generalized to an explicit typed schema. The finite permanent `r54_self_test`
matrix removes every required field, supplies null and malformed nested objects,
injects bool/float/string/range errors, and breaks cross-field path/hash/identity/
length/result bindings. Realistic fixtures replace `old/new/dir`. These are the
controls Coordinator should bind in the shared defect register. Existing 26 case
definitions and native safety/security/durability predicates are unchanged.

```jsonl
{"control": "missing-old-identities", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "old-identity-malformed", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "missing-parent-chain", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "ex-call-wrong-source", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "ex-call-wrong-buffer", "result": "ACCEPTED-WRONG-RESULT"}
{"control": "denial-bool-integer-metadata", "result": "ACCEPTED-WRONG-RESULT"}
```

```jsonl
{"control": "missing-old-identities", "result": "rejected"}
{"control": "old-identity-malformed", "result": "rejected"}
{"control": "missing-parent-chain", "result": "rejected"}
{"control": "ex-call-wrong-source", "result": "rejected"}
{"control": "ex-call-wrong-buffer", "result": "rejected"}
{"control": "denial-bool-integer-metadata", "result": "rejected"}
```

Source composite `e532ad81298663ce35a5bdb47c0012d29287a9814a8ef17dd4c1a519e739581a`; executable composite
`c683d9c1036456d7cad91c5001b3334ca863e48e78f162195f658590f20c91e1`. Raw receipts and the four binary bytes remain in
`/private/tmp/cfd-r54-local`; no unchanged full native baseline was rerun.

```json
{
  "sources": {
    "global.json": "6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df",
    "tools/spikes/WindowsRuntime/WindowsRuntime.csproj": "495225c72823bbf7a53a54028d1180a89a10805e69db034cee0a318cdf474c8f",
    "tools/spikes/WindowsRuntime/Program.cs": "361e2bb66102c2cd9dd72853e6b23310a0aa2814586b52c08ee13a90530cba55",
    "tools/qualify-windows-runtime.py": "38e3d64dedb590993f553067cec42bcaee0893fe29fde831e80ff25a48941e52",
    ".github/workflows/application-windows-qualification.yml": "03f23cb26ee23b9bf4b79fcd6929fc886c3c857573b28f8ef84c9878e412a832"
  },
  "binary_files": {
    "WindowsRuntime.runtimeconfig.json": "9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f",
    "WindowsRuntime.deps.json": "acae75476537b481a7d610843882a1e9897317c224691146e252de770f6a646e",
    "WindowsRuntime": "74b7afee17054765efd90410d680944d6c034da40488fd8c41e810df78cb854e",
    "WindowsRuntime.dll": "55267de057f987f88bfe7c87562636e2e0871f2cc7e75d0475130729e15b6ca2"
  }
}
```

### Durable minimal raw evidence

The capsule maps raw receipt names to UTF8 text, including all RED/GREEN controls,
all finite matrix outcomes, realistic positive rows, actual execute harness,
layout/prior-control stdout, build and process receipts, source/binary manifest
and portable gate. Codec base64(zlib(JSON)); only task-local path prefixes are
sanitized to `<R54_TREE>`, `<R54_LOCAL>`, `<R54_EXECUTION>`.
Decoded sanitized SHA256 `b5465d6beeb55987b675389b0e8453ea74e6dc04b04b2e812321c60d31ac7358`, 936627 bytes.
Original local JSON SHA256 `d1f9042f34140e549cfd62a2cc0eb5d9aceb1ffc0775980344b0cab293bc3983`, 937281 bytes, retained at
`/private/tmp/cfd-r54-raw-receipts.json`. Binary payloads are retained locally;
the capsule carries their exact manifest, not fabricated runtime observations.

```text
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
```

Completed: bounded schema/receipt correction and local proof. Remaining:
independent root review; every R53 native Windows candidate, expected-denial
native metadata reopen, Windows lifecycle/UIA, hostile same-object containment,
directory durability and product admission remain **Not assessed/unqualified**.
Best next action: root independently replay the frozen packet; no dispatch is
authorized by this proof.
