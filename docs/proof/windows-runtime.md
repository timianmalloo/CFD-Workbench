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
