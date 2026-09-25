---
id: proof-visible-presentation
title: Visible presentation feasibility proof packet
type: proof-pack
status: in-review
owner: "@cfd-visible-endpoint-20260925"
phase: application-foundation
tags: [performance, presentation, evidence]
links:
  - {to: design-visible-presentation, rel: documents}
  - {to: kb-visible-presentation, rel: depends-on}
review-by: 2026-12-24
summary: Source-bound builds, 50 synthetic receipt controls, four killed wrong-result mutants and 32 noncapture clock pairs establish limited preparation evidence. Native capture, visibility, lifecycle, rendered-state correlation and every M1 latency budget remain Not assessed.
review-suggested:
  - { by: design-visible-presentation, on: 2026-09-24, reason: "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims" }
---

# R41 bounded feasibility result

Superseded 2026-09-25: on-screen timing is no longer an M1 gate (see
docs/notes/m1-scope-decision.md).

**Disposition: partial preparation, independent review pending. No visible
latency PASS.** All capture-related behavior is Prepared/Compiled, never
native-executed. The target was not launched. There was no window enumeration,
permission query/prompt/reset/grant, screenshot, native UI input, product edit,
renderer override or foreground request.

Session `cfd-visible-endpoint-20260925`; assigned worktree
`/Users/mallalieut/projects/CFD-Workbench-feature-visible-presentation-spike`;
branch `feature/visible-presentation-spike`; base `f1a4e2c8a17c`.
Requested model `gpt-6-astra`, effective model identity **Not recorded**.
The audit marker began `2026-09-25T05:11:37Z`; closing audit contains measured
duration. Tokens/spend are **Not recorded**, not estimated. Six explicitly
authorized authored paths were claimed before edits; no seventh path was
invented for R41's arithmetic mismatch. Audit/index use official scripts.

## Claim, oracle and evidence

| Claim | Observed evidence / falsifier | Status and limit |
|---|---|---|
| Pinned target source compiles | .NET 10.0.203, Avalonia.Desktop 11.3.14; build zero warnings/errors | Verified build, not rendered target |
| Three model regions share intended generation/content | `--contracts` returned inspector/status/canvas value 120, generation 3, Preview; late cancelled generation refused | synthetic-executed; no pixels observed |
| Receipt state/identity/interval controls reject known false evidence | 50/50 final self-tests; full cases/raw fixtures retained | synthetic-executed; no native bridge |
| False lower bounds from regression refused | transient-final-regression and late-Preview cases return `VP-REGRESSION` | synthetic-executed; native unsampled persistence unresolved |
| Guard/region/regression/uncertainty mistakes are discriminated | Four deliberate wrong-result mutants each exit 1 | Verified targeted mutation, not full mutation score |
| Raw clock sampling can run without native UI | 32 paired Stopwatch/mach reads, exact frequency/timebase retained | noncapture-executed; drift across a trial Not assessed |
| SCK API signatures compile | Swift 6.4 / arm64 macOS compiler; no build diagnostics | Compiled only; no helper entry executed |
| Target-only content preserves physical visibility proof | No such claim: native mode always refused | Not assessed; window pixels cannot prove unoccluded screen |
| Permission and lifecycle refusals work on macOS | Source reviewable, no runtime permission/capture/stop test | native-unexecuted |
| M1 workload/reference-host timing | No product or reference-host trials | Not assessed |

The initial receipt contract test was seen red: `NotImplementedError: R41
red-first contract`, process exit 1, before the implementation existed. Negative
cases now call the real parser/algorithm. The suite checks meaningful refusal
codes, an independently calculated `[6,34]` ns interval, translation invariance,
outward uncertainty and exact budget equality/overlap/failure edges. Target-state
checks execute the real C# state object but do not claim UI composition proof.

## Frozen finite controls

Each row below is **synthetic-executed** unless explicitly native-unexecuted.
The committed qualifier is the exact fixture source and executable oracle.

| Cases | Expected result |
|---|---|
| wrong process/window/package/launch at envelope and individual frame (8) | `VP-IDENTITY` |
| stale operation/source/draft/generation/state/content (6) | `VP-NO-FINAL` |
| one region old; marker true with wrong canvas (2) | `VP-NO-FINAL` |
| idle/skipped/blank/suspended/incomplete/started frame (6) | `VP-FRAME` |
| zero presentation; duplicate; reorder; sequence gap; callback before event (5) | `VP-ORDER` |
| missing frame; missing predecessor (2) | `VP-LOSS`, `VP-PREDECESSOR` |
| unknown conversion/drift; inconsistent calibration (3) | `VP-CLOCK` |
| negative delta (1) | `VP-DELTA` |
| timeout; cancelled (2) | `VP-OUTCOME` |
| hidden; unproved occlusion (2) | `VP-VISIBILITY` |
| native receipt (1) | `VP-NATIVE-UNQUALIFIED` |
| unknown persistence; transient final/regression; late Preview (3) | `VP-PERSISTENCE`, `VP-REGRESSION`, `VP-REGRESSION` |
| malformed input; NaN budget (2) | `VP-SCHEMA` |
| valid, clock translation, three budget edges, paired conversion (6) | exact independent result |
| standalone disjoint paired clock intervals (1) | ValueError; CLI `VP-CLOCK`, exit 3 |
| Real blank capture, permission denial/revocation, process/window races, timeout/cancel/stop acknowledgement | native-unexecuted; no runtime claim |

Total 50 executed synthetic controls. Failed/time-out/unobserved trials cannot
be dropped from any future denominator; the design defines conservative bounds
and still requires a preregistered native sampling protocol.

## One corrective pass and defect controls

The first four mutation probes produced identity=0, regions=1, regression=1,
uncertainty=1. The identity mutant removed only the envelope check. The fixture
aliased envelope identity into frames, so changing the envelope also changed
every frame and the separate frame check concealed the missing guard.

**Class -> sweep -> derive -> prevent:** shared mutable negative fixtures can
mask a missing validation boundary. Swept envelope/frame identities and region
copies. Each frame now owns an independent copied identity; four additional
frame-only mismatch fixtures test the second boundary. The corrected suite is
49/49, and all four same deliberate mutants now exit 1. The identity mutant's
actual wrong results show synthetic acceptance for wrong envelope identity,
which the test oracle rejects. This is the one evidence-directed local
corrective pass. Coordinator owns shared-register capture; no out-of-scope
`defect-classes.md` edit was made by this author.

Root review added two evidence-directed corrections before freeze. The printing
CLI lacked the PLAT-A UTF-8 console guard: `portable-red.log` records one finding;
after adding the guard and explicit UTF-8 reads, `portable-green.log` is clean.
`verify-subprocess-utf8.py` is clean. The standalone clock-report consumer also
returned exit 0 with a null offset intersection for disjoint brackets, unlike
the synthetic assessment path. Root supplied an independent two-pair counterexample;
`disjoint-clock-red.json` retains that wrong success. The consumer now raises
ValueError on an empty intersection; the CLI emits `VP-CLOCK` and exits 3.
The permanent fiftieth fixture rejects this failure shape. Final four mutants
still each exit 1. These final corrections are disclosed rather than claiming
the initially green synthetic suite covered every consumer.

NG-local corrections: R41 counted six explicit paths as seven (Coordinator
resolved to the list); a guessed `coord.py` name was absent (used documented
`coord-core.py`); two guessed pinned GPU-interoperability source filenames
returned 404 (no claim rests on them). `check-docs.py --help` ran the check
instead of help; its successful check is a preliminary result, not final
verification. No unauthorized native action followed any failed lookup.
The documentation gate also rejected an unregistered `tests` relation; the
proof now uses the existing registered `documents` relation. The final official
derive/docs gate is the control for that schema mistake.

The implement skill's full product exit conditions are intentionally **unmet**:
rendered/cross-surface proof, native failure/lifecycle execution, UI craft and
accessibility qualification, native instrumentation and independent hard-veto
clearance. This is a research spike under R41, not implementation acceptance.
Testing Strategy D0/D1/D2/D6 apply to the deterministic receipt layer; D4/D7
native fidelity and D3 full composition remain explicitly unverified. The
isolated project has no product-project references and adds no product layer.

## Source and binary binding

SHA-256, measured after final code correction:

| Path | SHA-256 |
|---|---|
| `tools/qualify-visible-presentation.py` | `b76b22b2bf8bf2bf322a1779f963fdb8fc85f44701fd3cb7c8983fa752b732e1` |
| `tools/spikes/VisiblePresentation/Program.cs` | `425621729b42d0555b04a35a77ad8a13f70423af4cf36626a9a800a8b0161447` |
| `tools/spikes/VisiblePresentation/Capture.swift` | `33a0cf4d6d26e5cbca80584be3179a3a4b02959a382ff0bc481f88ce5d54f2f5` |
| `tools/spikes/VisiblePresentation/VisiblePresentation.csproj` | `1987e708e26f760cb3f81c5c9a88ac61ea765a8df39d213e04dea29e4022099c` |
| `global.json` (unchanged) | `6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df` |
| scratch `capture` binary | `6ab60e9b048f4c6793a4f4452bfe09c124326c5cdb276e158cb29e79cd777d46` |
| scratch `build/bin/VisiblePresentation/release/VisiblePresentation.dll` | `26168fa8d4dc7d5872b26d39c35b3d4dbc48b6748123501112e9c4ed1afb39b4` |
| scratch target apphost `VisiblePresentation` | `9c4df93ff809a4a350015f3003debd66d0b8f6340bfdc428216f7342da9d6fa9` |
| scratch target `VisiblePresentation.deps.json` | `500c196996229496575e75bd7019bfb3b3c0de6d6d9d066e4b64b2af6c2dbfeb` |
| scratch target `VisiblePresentation.runtimeconfig.json` | `9950d4583cfc9a106857c0d5b8a75b53e8774852b57b80bc3823a74d44a4d87f` |

Target project uses the pinned framework only. Builds/caches live under
`spikes/visible-presentation/`; no global cache setting or trust change was made.
The observed Swift compiler is `Apple Swift version 6.4
(swiftlang-6.4.0.34.1 clang-2100.3.34.1)`, target
`arm64-apple-macosx27.0.0`. Availability checks do not prove an older macOS build
or Windows execution. SDK warnings from .NET workload verification were retained;
the build did not perform a workload update.

## Raw receipt retention

Local raw receipts remain at the assigned worktree's ignored
`spikes/visible-presentation/`. They are not a committed durable artifact store;
root must retain them before any later scratch cleanup. No scratch was deleted.
The proof below pins their bytes rather than promoting raw logs to correctness.

| Receipt | SHA-256 |
|---|---|
| `synthetic.json` (50 cases, raw fixtures and source manifest) | `4ee48ade0991bf0d11d86cc042949e96e5caad7564e77a1f1c13dc473ba62334` |
| `state.json` | `c5bede10f100e78c61c9c2b5614b058875a90ad1b25bea79d8fa80e8e825a1e6` |
| `clock.json` (all 32 raw pairs) | `6662f3f11677a867d13b76c0a67aa6d4857ef9a411910a45421fba070c597f3a` |
| `clock-analysis.json` (also retains raw pairs) | `88ae1d39245c8112d84dd6fa9893017409276c3fb6e1636bd4abe9a31e38022a` |
| `red.stderr` | `efe47921e1ddc616742e784a1d75ec377254f3ff3e0118a1e1beeced2993a8b9` |
| `mutation-summary.json` | `dd2f8819bcd7a7ebd21796ba521293099e9c94e70317803ab1cbc165fd4e0398` |
| `build.log` | `2db07e942827cebfbcc331fdf0416d667805d12a2bcdf93624192805ed4fd3b4` |
| `swift-build.log` (empty, successful compiler exit) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `disjoint-clock-red.json` | `2ac02db2aa5552203618b91f00e3eb8505e86f81e4457292f3a6bd8f36f49d07` |
| `disjoint-clock-green.json` | `83bb45cc24a98f568146f5fc60443053e08d42690b325fd6dac710419d5d908e` |

Each `*-mutant.json` preserves the wrong-result receipts; mutant `.py` files
remain in scratch only. Final mutation summary is
`[{"exit":1,"mutant":"identity"},{"exit":1,"mutant":"regions"},
{"exit":1,"mutant":"regression"},{"exit":1,"mutant":"uncertainty"}]`.

Observed noncapture clock data: Stopwatch frequency `1000000000` ticks/s,
mach timebase `125/3` ns/tick, 32 pairs, conservative combined quantization
`128/3` ns, paired offset intersection `[-128/3,42]` ns. These are local adjacent
sample results; no claim of universal clock equality or trial-duration drift.
First raw pair `(540619720665041,12974873296005,540619720666916)`; last
`(540619720691833,12974873296604,540619720691833)` in
`(Stopwatch before, mach, Stopwatch after)` order.

## Reproduction and bounded handoff

Safe noncapture commands, from the assigned tree:

```sh
python3 tools/qualify-visible-presentation.py --self-test
dotnet spikes/visible-presentation/build/bin/VisiblePresentation/release/VisiblePresentation.dll --contracts
dotnet spikes/visible-presentation/build/bin/VisiblePresentation/release/VisiblePresentation.dll --clock
python3 tools/qualify-visible-presentation.py --clock-receipt spikes/visible-presentation/clock.json
```

Build commands used task-local `DOTNET_CLI_HOME`, `NUGET_PACKAGES`,
`--artifacts-path`, disabled build servers, telemetry and certificate generation.
The Swift compile used `-module-cache-path spikes/visible-presentation/swift-cache`
and output `spikes/visible-presentation/capture`. Compiling never executes the
helper. The design records the proposed launch command, but no concrete native
request exists and no capture authorization is requested in this author turn.

Remaining blockers: whole-package/runtime identity beyond apphost hash;
supported renderer presentation endpoint/actual backend; actual client-area crop
mapping and region decoding; cross-process drift; frame loss and state persistence;
physical unoccluded visibility; permission and stream startup/stop race behavior;
external OS-input/cold-start boundaries; observer overhead; product workload and
reference hosts. Native proof must distinguish WindowServer events from photons.

Root independently reviews Data/Security/Test and decides whether another
bounded preparation task is needed before any specific user capture request.
The author cannot clear those vetoes. The current packet is insufficient to
run native timing acceptance, and the six-path preparation scope is preserved.

Final local documentation check: **passed**, zero defects; 80 pre-existing
review-suggested entries remain. Portable text and subprocess UTF-8 checks:
**clean**. Planned nodes A-E completed as preparation; independent node F remains
root-owned. One local fixture correction and two root-directed consumer/portability
corrections were made; no performance trial or expanded scope followed them.

## R47 frozen preparation packet

**Disposition: preparation only, independent root review pending. Native timing
remains M1 / Not assessed.** Exactly five authored paths; no product, renderer,
project/dependency, Windows implementation or full section-editor change.
Session `cfd-visible-identity-20260925`, branch
`feature/visible-presentation-identity`, base `b93647f6a746`; audit start
`2026-09-25T05:58:14Z`. Requested profile: prior qualified Astra author;
effective model identity **not exposed/verified in this worker**, not asserted.

| Claim | Observed control / oracle | Confidence and boundary |
|---|---|---|
| Published private asset inventory is complete for the concrete RID deps | 31 sealed published files; 27 unique deps-resolved runtime/native assets | Verified disk/package inventory; not loaded-memory attestation |
| Changed/missing/extra/escaping package refuses | Six package controls: positive plus managed byte change, native removal, extra file, symlink, required-file removal | Verified noncapture; expected-map review remains root-owned |
| Exact target-only structural request rejects invalid inputs | 14 pure parser cases; each retained input/output and exit | Verified parser only; fixture PID/window/crops are synthetic, never a launch request |
| PID/start/executable/bundle snapshots fail closed | Eight pure identity cases, including nil observation and second/microsecond changes | Verified pure comparator; native reads/rechecks compiled, not executed |
| Self-process identity can be observed without UI | C# process receipt has identical PID/start ticks/executable and stable=true | Verified self-process only; not cross-API precision or target UI identity |
| Original receipt protections preserved | 50/50 synthetic rows; four deliberate wrong-result mutants each exit 1 with named failing rows | Verified deterministic assessor; no native metric |
| Local clock operands retained | 32 raw before/mach/after triples with frequency/timebase | Verified adjacent sampling; no cross-process trial drift claim |
| Pure entry does not initialize native UI | Swift pure parser/comparator branches return before observer; C# branches return before AppBuilder | Source-inspected and pure entries executed; no runtime instrumentation of forbidden calls |
| Portable text/subprocess gates remain active | Both raw gate outputs are clean; process exits 0 | Verified scanner receipts; not broader platform qualification |
| Native frame identity/geometry/permission/cleanup behavior | Compiled source only; no native entry invoked | **Unverified**; root retains veto |

The retained arm64 Mach-O load commands disambiguate install names from imports.
`/usr/local/lib/libAvalonia.Native.OSX.dylib` is LC_ID_DYLIB. The three private
dylibs and apphost import only `/System/Library` and `/usr/lib` dependencies.
This evidence shows no missing static private dependency in this concrete
published package. The installed .NET 10.0.7 runtime and OS libraries are the
declared trust boundary, not missing package files. Root must independently
accept the closure; dynamic loader configuration belongs in a later exact
launch context. Do not turn the absence of loaded-memory attestation into a
requirement to attest all system memory.

### Corrections and limits

The bounded implementation/check cycle exposed several defects. These are
disclosed separately rather than claiming every initially green result was
final. The empty portable deps target was initially treated as ambiguous; the
actual RID-specific target is now selected while nonempty competitors refuse.
A case-alias fixture cannot create two names on this case-insensitive volume;
it was replaced by a real missing-minimum fixture. Alias rejection exists in
the reader but has no executed dual-file fixture on this host. Swift rejected
conditional CFDictionary casts; typed dictionary bridging now compiles.

The C# first self-process check used anonymous-object reference equality:
identical printed PID/start/executable operands produced stable=false. It now
uses value equality. The first portable gate found one write_text without
explicit LF; the normal gate forced that correction. A canonical-root guard
then exposed macOS's symlinked temporary-directory prefix: the copied test root
is now resolved before use, while the production reader still rejects symlinked
roots. The failing package receipt is retained separately below. Earlier
compiler/process/portability errors are **transcribed observations from tool
output**, not byte-identical retained raw logs; only the capsule contents are
claimed exact. No correction triggered capture, permission, UI or Windows work.

Class -> sweep -> derive -> prevent, for coordinator register follow-up:
reference equality is not identity-value equality (self-process stable oracle);
host path aliases invalidate fixture assumptions (canonical positive plus path
refusal controls); cross-platform writes require explicit LF (existing portable
gate); concrete dependency schemas must be inspected, not inferred (selected
RID plus ambiguity/missing-asset guards). No shared register was edited here.

The original plan described one implementation plus one bounded correction
phase. Corrections were applied incrementally while collecting compile and
contract evidence, including a final fixture canonicalization after the
retention runner exposed it. This was not a single atomic corrective edit;
root should treat that as a procedural limitation, not a hidden extra pass.
No further source correction is included after this freeze.

### Exact next evidence, without implied authorization

| Remaining item | Smallest next evidence step | Why it remains |
|---|---|---|
| Private closure independent review | Root compare the 27 deps resolutions, 31 file hashes and arm64 load commands against the frozen package | Author cannot clear Data/Security/Test veto |
| PID/start precision and repeated native lookup | Review kernel/API precision contract and later authorize a bounded self/owned-process identity experiment | Header units are microseconds; actual resolution/collision behavior and AppKit consistency unmeasured |
| Concrete request geometry and crops | Later root-owned target measurement and exact request review | No UI target was launched; no observed PID/window/bounds/scale/crop exists |
| Permission/target races and stop lifecycle | Later specifically authorized finite refusal/lifecycle experiment | Pure comparator tests cannot execute native failure paths |
| Visibility/loss/drift/native endpoint | Independently measured native endpoint qualification | Callback sequence, desktop-independent pixels and adjacent clock pairs are insufficient |

Source-to-render, native instrumentation, UI/craft/accessibility and native
failure-path DoD remain unmet; their execution is excluded by R47. The existing
50 controls cover stale operation/generation/content, regression and late
Preview. The new parser checks dimensions/scale/crop bounds but does not claim
that a real moved window was tested. No exact user capture question is ready.

### R47 durable raw replay capsule

This is zlib-compressed base64 of the exact UTF-8 replay.json bytes, not a redacted reconstruction. Each process preserves argv, exit, measured duration and exact stdout/stderr bytes; fixtures, named results, 32 clock operands, package inventory and source bindings are inside. No native capture data exists. Decode with base64.b64decode then zlib.decompress and verify SHA-256 `711b114d58ca1c23ee67292402d5a0517aa82dcb1030d388ff0744e6f312d30d`. The capsule is durable even if scratch disappears.

```text
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
```

| Source or binary | SHA-256 |
|---|---|
| `tools/spikes/VisiblePresentation/Capture.swift` | `d0755efed8688fd72d3b4408ace4b51401a41498368c1629eae5673c2e6d845b` |
| `tools/spikes/VisiblePresentation/Program.cs` | `7a6351256d7ca5051ef6675a493c711f5af0f4736e4785eb3995e4a5f85871e1` |
| `tools/qualify-visible-presentation.py` | `e8b78af79c4d68b7361c27e91a28dd0af366846c7e5c3d53aeb28f1b2fe47b86` |
| `tools/spikes/VisiblePresentation/VisiblePresentation.csproj` | `1987e708e26f760cb3f81c5c9a88ac61ea765a8df39d213e04dea29e4022099c` |
| `global.json` | `6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df` |
| `capture binary` | `212e92e593f27dbf71075866a45bec9a898988ce624335d693a8a1db3ff2d1d7` |
| `VisiblePresentation.dll` | `d5af4ce0a28f1efe1d4c93fb0970d4552e844b2e27e6920b5682b67f0dc20c8f` |

### Exact static native resolution and failing fixture receipt

Same zlib/base64 decoding as above. SHA-256 `dee76d7754ddfa32d4e5f87dd07188b9fab489b86c08bb586a2a883bc62f9497`. Includes the exact otool argv/exit/output, binary hashes, parsed import operands and the original failing canonical-temporary-root package receipt. LC_ID_DYLIB is excluded from imported-library assertions; every observed arm64 import is inside the declared OS boundary.

```text
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
```
