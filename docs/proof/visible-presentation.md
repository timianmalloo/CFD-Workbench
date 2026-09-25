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
---

# R41 bounded feasibility result

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
