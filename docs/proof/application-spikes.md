---
id: proof-application-spikes
title: Application architecture contract and native spike evidence
type: proof-pack
status: in-review
owner: "@cfd-owner-20260923"
phase: architecture
tags: [proof, native, identity, geometry, persistence]
links:
  - {to: architecture-application, rel: documents}
  - {to: adr-application-stack, rel: documents}
  - {to: design-application-foundation, rel: documents}
  - {to: proof-native-ui-workbench, rel: refines}
review-by: 2026-12-23
summary: >-
  Records actual pinned SDK/package builds, native macOS accessibility and picker observations,
  exact identity vectors, conservative rational geometry bounds and filesystem fault injection.
  Separates the bounded architecture spike from unfinished production and Windows evidence.
---

# Architecture spike evidence

Author: `cfd-arch-codex-20260923`. Base `a25c175`. Started 2026-09-23 13:41:25 UTC.
Compiled contract `al-01M377KMYKFR9ZD023M2VXBNEP` originally owns seven paths; Owner Ruling 5 adds exactly
`docs/security/threat-model.md` and `docs/security/privacy-review.md`, for nine current authored paths.
No production code. Built-in author model identifier is unexposed; no Astra/Grok alias is claimed. Isolation
is procedural and observed-only, not an enforced filesystem sandbox. Coordinator owns cancellation receipts.
Independent root and Owner review are separate from these author observations.

## Versions and reproducibility

Observed SDK10.0.203/MSBuild18.3.3, runtime10.0.7, macOS27.0, osx-arm64. Python3.14.4.
Native project pins Avalonia.Desktop11.3.14, Avalonia.Themes.Fluent11.3.14 (MIT), Blake3.NET2.2.1
(BSD-2-Clause). Python oracle pins blake3 1.0.8 and rfc8785 0.1.4; these are spike-only dependencies.
NuGet nuspecs were read for package license declarations; this is not a full transitive distribution audit.

Reproduce from repository root; all generated files stay under disposable `.architecture-scratch`:

```sh
python3 -m venv .architecture-scratch/venv
.architecture-scratch/venv/bin/pip install blake3==1.0.8 rfc8785==0.1.4
DOTNET_CLI_HOME=$PWD/.architecture-scratch/dotnet NUGET_PACKAGES=$PWD/.architecture-scratch/nuget dotnet build tools/spikes/ApplicationNativeUi/ApplicationNativeUi.csproj -p:BaseIntermediateOutputPath=$PWD/.architecture-scratch/obj/ -p:OutputPath=$PWD/.architecture-scratch/bin/
.architecture-scratch/bin/ApplicationNativeUi --contracts
.architecture-scratch/venv/bin/python tools/spikes/application-contract-vectors.py .architecture-scratch .architecture-scratch/bin/ApplicationNativeUi
DOTNET_CLI_HOME=$PWD/.architecture-scratch/dotnet NUGET_PACKAGES=$PWD/.architecture-scratch/nuget dotnet publish tools/spikes/ApplicationNativeUi/ApplicationNativeUi.csproj -r osx-arm64 --self-contained true -p:BaseIntermediateOutputPath=$PWD/.architecture-scratch/obj/ -p:PublishDir=$PWD/.architecture-scratch/CFDSpike.app/Contents/MacOS/
```

The unsigned `.app` requires a disposable `Contents/Info.plist`: CFBundleExecutable=`ApplicationNativeUi`,
CFBundleIdentifier=`org.cfdworkbench.architecturespike`, CFBundleName=`CFD Architecture Spike`,
CFBundlePackageType=`APPL`, CFBundleShortVersionString=`0.0.1`, NSHighResolutionCapable=true.
The project can also run through its apphost; no installer, certificate or permission bypass is involved.
Windows uses the analogous `win-x64` publish and Windows executable path; live proof is not inferred.

## Native observed receipt

Build and macOS self-contained publish completed with zero errors/warnings. Windows `win-x64`
self-contained cross-publish also completed; this is build evidence, not Windows runtime or UIA evidence.
Author launched the generated
bundle through CUA, not a screenshot simulation. The observed tree contained:

```text
standard window CFD-Workbench architecture spike
  text field Leading edge x in metres, Value: 0.014049
  button Open foil file
  unknown Foil viewport. Illustrative open-tip section. No simulation.
  text Architecture spike. Analysis Unavailable.
```

Open displayed a native sheet `Open FoilDSL`, `open-panel`, Cancel and Open roles. Cancel returned the
window, preserved `0.014049` and produced `Open cancelled. Accepted source unchanged.` The screenshot
showed the cyan section outline inside the black viewport. This is a renderer/picker spike, not the
production geometry evaluator. It does not parse the selected file.

Root separately reported observed native input `0.020`, Tab focus to Open, Return opening the native
NSOpenPanel, Escape cancellation, preserved `0.020`, and focus return to Open. Earlier both sessions saw
intermittent `cgWindowNotFound` while the process remained alive; the subsequent direct inspection resolved
that observation gap. Do not turn it into a claim of native lifecycle robustness. The viewport's `unknown`
AX role is a **known product gap**; custom station/CV peers and VoiceOver/Narrator are not proven.

## Identity and numeric evidence

Exact rational unit conversion gives the same binary64 bits for `14.049 mm`, `1.4049 cm`, `0.014049 m`:
`3f8cc5b8dc55000d`, canonical `0.014049`. Its BLAKE3 is
`d8a5e1c62632686bcf41a6d6b866a1386e72d1eab173e0595b23aac5cf3eba15`.
Subnormal `5e-324` is bits `0000000000000001`; `1e-7` is `3e7ad7f29abcaf48`.

Canonical vector (UTF-8): `{"a":[1e-7,1e+21,0.014049],"text":"é","z":0}`.
SHA-256: `4eb74eced4ee97a967685ea401c6c43a15a455f8baba431ff5bb8f3e09039149`.
BLAKE3: `07d666df91be830426b5e13fdfee074e94ab59dac744427a4af04c2f1b46403c`.
Python and native C# package returned identical digests for these exact bytes. The official BLAKE3 empty
vector also matched. Source CRLF versus LF produced different SHA-256. No JCS claim is made for default
.NET JSON: the native roundtrip probe emits `1E-07`, `1E+21`, `-0`, unlike canonical JSON. A complete C#
exact-decimal converter/JCS implementation and GUI/CLI end-to-end identity remain downstream gates.

## Geometric proof scope

`application-contract-vectors.py` uses exact rational de Boor evaluation and per-span Bernstein recovery;
convex-hull positivity is continuous on the span, not sampled geometry validation. It checks derivative
support on nonempty spans, repeated knots, a flat curve rejection, repeated-root conservative refusal,
independent rail bounds, profile interior separation, bounded thickness maximum enclosure, and normalized
scale enclosure. Exhausted subdivision budget returns no certificate. The entire foil admission algorithm
is not implemented: parser/structural checks and 3D sufficient-proof assembly must bind these primitives.
Negative Bernstein coefficients mean **inconclusive**, not automatically an invalid mathematical curve.
The final oracle run passed **30 checks in 0.041069958 seconds**. The near-boundary chord case runs the
actual interval certificate primitive, admitting a strict 1e-12 m margin and refusing a touching bound.
The known degree-elevated quadratic encloses its exact maximum 1/4. The normalization error assertion
bounds thickness-only uncertainty below 1 nm for chord at most 2 m: in `C ± tc*T/2`, fixed camber cancels.
It does not bound inversion, trigonometric or final coordinate rounding error. This timing is not a product
performance budget. Native validation binding separately accepted the matching fixture and rejected all
**nine** mismatches, including forged/empty definition hash, changed base and different draft ID.

## Persistence proof scope

Real local-file fault injection exercises before-write, partial-write, before-replace and after-replace;
the target is old or complete new as appropriate. External change before the final precondition check is
retained and returns DOC-CONFLICT. Reopen preserves exact accepted CRLF bytes while offering a separate
incomplete recovery draft. Target and ancestor symlinks are rejected. New-file creation, colliding writer
claim, and pre-existing temporary-file sentinel cases all passed. Review found cleanup deleting a file
whose exclusive creation failed; cleanup now requires successful temporary-file ownership. Its regression
asserts unrelated temporary bytes and original target survive, and the operation's claim is released.
The regression was disconfirmed against a disposable copy with only that ownership guard removed:
it exited 1 with `FileNotFoundError` reading the other-owned sentinel. The final guarded source's
30-case run passes the sentinel and writer-claim tests. This is a cleanup-ownership failure class;
Coordinator owns adding its register entry at the joined graph boundary.
The recovery envelope in this low-level byte-store fixture is illustrative, **not native-v1 schema/parser
conformance**. These are disposable real files, not an in-memory
store fake. They do not establish .NET/Windows replace, arbitrary ancestor/race safety or power-loss durability.
Cooperative lock semantics and the uncooperative-writer TOCTOU gap are explicit Owner decisions.

## Sources read before or alongside execution

- [Avalonia11.3.14 IStorageProvider source](https://raw.githubusercontent.com/AvaloniaUI/Avalonia/11.3.14/src/Avalonia.Base/Platform/Storage/IStorageProvider.cs): async picker contract and capabilities; native invocation executed.
- [Avalonia11 file-picker options](https://v11.docs.avaloniaui.net/docs/concepts/services/storage-provider/file-picker-options/): picker options used in the compiled spike.
- [Avalonia accessibility](https://docs.avaloniaui.net/docs/app-development/accessibility): platform peer concept; source version drift is why native AX was measured, not inferred.
- [Blake3.NET2.2.1 package](https://www.nuget.org/packages/Blake3/2.2.1): pinned API/package/license; native hash executed.
- [BLAKE3 official vectors](https://raw.githubusercontent.com/BLAKE3-team/BLAKE3/master/test_vectors/test_vectors.json): empty vector used as independent digest oracle.
- [RFC8785](https://www.rfc-editor.org/rfc/rfc8785): canonical object/string/number contract. Python implementation is an oracle; its selected vectors do not establish all C# conformance.

## Open gates and outcome

Architecture author does not clear Owner/root vetoes. Windows live UIA/Narrator, signing/notarization,
complete source parser/patcher, C# canonicalization, immutable session implementation, native persistence
and complete admitted-shape validation are **Not assessed as product implementations**. A passing spike
only establishes the exercised package/API or proof primitive. No production application/backend is delivered.
Owner request: `req-01M3791C842XJCWYGECW0Q6ZVT`, conditional architecture acceptance with a serial
session/schema contract-completion gate; no full G3 freeze or production dispatch is claimed.

## Exact authored scope and source fingerprints

Worktree: `/Users/mallalieut/projects/CFD-Workbench-feature-application-architecture-codex`;
branch `feature/application-architecture-codex`; grounded HEAD `a25c175`.
Model: **Not recorded** (Owner Ruling 7 permits this only for architecture track A).
Scratch caches/build products under `.architecture-scratch/` and spike `bin/` are disposable and excluded.
Coordinator/root provide host cancellation and independent final-diff receipts; built-in confinement is
not claimed. This author has not launched another author or changed shared primary state.

The nine authored paths are the four linked architecture/ADR/design/proof documents, two security
registers `docs/security/{threat-model,privacy-review}.md`, and these three source files:

| Source | SHA-256 |
|---|---|
| `tools/spikes/ApplicationNativeUi/ApplicationNativeUi.csproj` | `bc4f360f18a131cdf5354862fbc62183756a7bd950477f9fffacd1ed00fbb6b6` |
| `tools/spikes/ApplicationNativeUi/Program.cs` | `665f2fd7e9838d7651a671b049c3c887015b2f7f8bdac8a8b108ba972e0373a3` |
| `tools/spikes/application-contract-vectors.py` | `3ad2f0a01c2c4838f6ad6a713dbf037a97a1ef77ce05a25ccae692f5fed38562` |

Derived graph and append-only audit outputs are mechanical additions, not new authored ownership.
`python3 tools/check-docs.py` passed: 89 artifacts, zero defects, zero index drift; 56 existing
review-suggested flags remain warnings. The descendant commit is reported with handback.
The architecture timer starts at 13:41:25 UTC. The design timer was only marked at 14:07:29 UTC after
its drafting had begun; its recorded duration covers finalization only, not the whole design effort.

## Final oracle case receipt

| Case | Observed |
|---|---|
| `Decimal_ScaledExact_FirstThreeBitsEqual` | PASS |
| `Jcs_ExponentNegativeZeroUnicode_ExactBytes` | PASS |
| `Blake3_Empty_OfficialVector` | PASS |
| `Source_Trivia_SeparateIdentity` | PASS |
| `CSharpPython_SameCanonicalBytes_SameDigests` | PASS |
| `Derivative_EveryOpenKnotSpan_StrictlyIncreasing` | PASS |
| `Derivative_RepeatedInteriorKnot_AllNonemptySpans` | PASS |
| `Derivative_FlatCurve_NotAssessed` | PASS |
| `Derivative_BoundaryRepeatedRoot_PositiveOpenSupport` | PASS |
| `Derivative_InteriorRepeatedRoot_ConservativeNotAssessed` | PASS |
| `Chord_IndependentBases_PositiveEveryOpenSpan` | PASS |
| `Chord_NearBound_AdmitsPositiveRefusesTouching` | PASS |
| `Chord_TouchOrNegativeCoefficient_FailClosed` | PASS |
| `Profile_ClosedEndpoints_StrictInteriorThickness` | PASS |
| `Thickness_Maximum_EnclosedNormalization` | PASS |
| `Thickness_BudgetExhausted_NotAssessed` | PASS |
| `Thickness_UnsupportedCrossing_NotAccepted` | PASS |
| `Maximum_KnownQuadraticDegreeElevated_EnclosesQuarter` | PASS |
| `Normalization_EnclosurePropagated_PhysicalErrorBelowOneNanometre` | PASS |
| `Atomic_NewFile_ExpectedAbsentCreatesCompleteEnvelope` | PASS |
| `Atomic_TemporaryCollision_PreservesUnownedFileTargetAndReleasesClaim` | PASS |
| `Atomic_ClaimCollision_PreservesOtherClaimAndTarget` | PASS |
| `Atomic_before-write_OldOrCompleteNew` | PASS |
| `Atomic_partial-write_OldOrCompleteNew` | PASS |
| `Atomic_before-replace_OldOrCompleteNew` | PASS |
| `Atomic_after-replace_OldOrCompleteNew` | PASS |
| `Atomic_ExternalChange_ConflictPreservesB` | PASS |
| `Reopen_RecoverySeparate_AcceptedSourceExact` | PASS |
| `Atomic_Symlink_Refused` | PASS |
| `Atomic_AncestorSymlink_Refused` | PASS |
