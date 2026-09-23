---
id: proof-application-contracts
title: Executable native M1 contract evidence
type: proof-pack
status: in-review
owner: "@cfd-contracts-author-20260923"
phase: design
tags: [application, contracts, proof, identity, persistence]
links:
  - {to: design-application-contracts, rel: documents}
  - {to: adr-application-project-contract, rel: depends-on}
  - {to: spec-foildsl, rel: depends-on}
review-by: 2026-10-23
summary: >-
  Records actual C# build/session assertions and independent decimal, RFC 8785, BLAKE3 and history oracles.
  Distinguishes fixture geometry authority and persistence models from live local filesystem primitives.
  Native Windows, full language/kernel/store and production telemetry remain unassessed.
review-suggested:
  - { by: design-application-contracts, on: 2026-09-23, reason: "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams." }
  - { by: adr-application-project-contract, on: 2026-09-23, reason: "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open." }
  - { by: spec-foildsl, on: 2026-09-23, reason: "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax." }
---

# Application contract proof

This is a **contract fixture**, not production implementation or scientific geometry certification.

## Current evaluator /2 companion amendment — 23 September 2026

Root applied Owner Rulings 17–19 to the normative companions and current fixtures. The earlier observations
below remain **historical `/1` evidence**, including their hashes and author context; they are not relabelled
as `/2` runs. Supplied v3 references, historical v6 and native architecture-spike outputs remain unchanged.
The production B tree is separately owned and was not changed by this companion run.

Current B0 fixture source fingerprints:

| Source | SHA-256 |
|---|---|
| `tools/spikes/ApplicationContracts/Program.cs` | `158297562c3a50db26cdd944c2c63290e9516b88666c043f25561f594d51effa` |
| `tools/spikes/ApplicationContracts/ApplicationContracts.csproj` | `4a67878b7b5d5a9512f836a4ccd38d6c55697f1b438db532013ac318ba2f2f62` |
| `tools/spikes/application-session-contract-vectors.py` | `595556833579d94beff37ec2d1df19bcd1aba25ce8f4769249b6cc367d427942` |

Retained scratch: `/tmp/cfd-r17-companions.qWdppH`. `red/` compiled successfully and exited -6 on
`Ruling17_TwistDegreeInputs_DoNotCollapseIdentity` against the old per-CV radians hasher. `green/` then
compiled with zero warnings/errors and passed **93 C# assertions** with degree-preserving `/2` identity.
The new source and native evaluator refusal assertions preserve an empty session rather than adopting old
bindings. The independent pinned Python oracle passed **42 checks and 2,505 vectors** on that exact DLL.
Owned .NET/oracle children were observed quiescent. The current Example Surface hash is
`3379643904752200d709113c1700a692bc8d9b2d3db001fbb6aa5249b66954f4`; native image remains 7,930 bytes.
This proves bounded fixture identity/session behavior, not R18's production all-query geometry proof.
The required `python3 tools/recount-application-contracts.py` also completed with exit 0 in
4.597695167 seconds under an owned parent; all eleven observed PID/start identities were absent at exit.
Its exact count is now 93 with all four Ruling 17 names required; the 42-Python/2,505-vector floors and
previous named assertions remain. Build servers/shared compilation are disabled during the recount.

The repository browser harness now defaults to the authoritative v7 and accepts `PROOF_DIR` for isolated
evidence output. On the amended mockup it passed **14 checks across 15 layout/theme cells**, with no
page errors or network requests. Its new real textarea Validate/Cancel case refuses unavailable `/1`,
disables Apply, and preserves accepted source/history. The /2 emitter pin and normal parse/emit, preview,
apply, cancel, Undo/Redo, file roundtrip and section-reader routes passed. Root inspected the rendered
final-source screenshot; no layout/token changes were made. This is browser prototype evidence, not a
native application or scientific-certification pass.
The unchanged rendered-spec checker ran against exact copies of the new Markdown/HTML in isolated
scratch: 195 content blocks, 20 requirement IDs and one flow agreed; source hash, revision, mockup link,
desktop/narrow overflow, navigation search and empty-search state passed, with no page errors/network.
Root inspected that rendered screenshot. `design-lint.py DESIGN.md` reported zero warnings; the craft
gate scanned the changed HTML and returned 14 Minor findings, identical to the recorded v7 baseline
when checkout location is excluded. No new craft finding or token/layout change was introduced.

Browser evidence SHA-256:

```text
mockup HTML     c311468182b2ff010c9174d196a8b343bdbaf6689fcb5cea7547d655ac8cb1ef
check script    63c9a7c247852308321ca05b9b595ad3c56dc42cd75ca09518140037a6083303
browser report  eb6f2bc0f1ca7d0d30509a8131cb5c819f9429e03d444ca029e8a807e58cf33f
rendered PNG    7785ced57d09bfd1d9d508418143d507ac869bf31a77e2d46adcb16acfcc2fc5
```

Independent Owner companion review remains pending. Production evaluator/store/API review, Windows runtime,
native rendered workflows, full geometry and M1 acceptance remain separate open gates. Cost: Not recorded.

## Historical original fixture run

Author session `cfd-contracts-author-20260923`; requested model `gpt-6-astra`, effective model **Not recorded**.
Ruling 9 permits that narrow B0 design exception. Worktree containment is observed-only, not a sandbox claim.
No subagent was spawned. Root is the independent reviewer; Owner rules the gate.

## Exact scope and reproducible commands

Assigned cwd: `/Users/mallalieut/projects/CFD-Workbench-feature-application-contracts`.
Branch: `feature/application-contracts`. Grounded HEAD: `73cabb89ed7fc77b484ca7786d1b835333299795`.
Timer marked at 2026-09-23 14:29:28 UTC, before substantive work. Final commit/clean-path evidence is supplied
at handoff. Authored scope is the design, this proof, ADR 0004, two security/privacy rollups, C# project/program
and Python oracle. Derived index and official audit/change render outputs are metadata exceptions.

At the 30-minute checkpoint the author estimated the 70-call window was near its cap; exact tool-call count
is **Not recorded**, because the host exposes no aggregate counter. Coordinator approved at most 20 additional
calls/20 minutes for finalization with unchanged scope. This is a budget-estimation gap and explicit replan,
not a claim that a cap proves completion. No extra agent, production scope or weaker gate was introduced.

Observed .NET SDK: 10.0.203; native runtime osx-arm64. C# pins `Blake3` 2.2.1 and `net10.0`, no UI dependency.
Python oracle pins `blake3==1.0.8`, `rfc8785==0.1.4`; standard-library Fraction supplies independent rational
conversion and struct supplies IEEE binary64 bit encoding. Scratch/cache/build products are not source.

```sh
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_HOME=$PWD/.contract-scratch/dotnet NUGET_PACKAGES=$PWD/.contract-scratch/nuget dotnet build tools/spikes/ApplicationContracts/ApplicationContracts.csproj -p:BaseIntermediateOutputPath=$PWD/.contract-scratch/obj/ -p:OutputPath=$PWD/.contract-scratch/bin/ --nologo
python3 -m venv .contract-scratch/venv
.contract-scratch/venv/bin/python -m pip install blake3==1.0.8 rfc8785==0.1.4
.contract-scratch/venv/bin/python tools/spikes/application-session-contract-vectors.py .contract-scratch/bin/ApplicationContracts.dll
```

Windows uses `py -3`/`python` and platform-specific environment/path syntax. These POSIX commands are not a
claim of Windows execution. Subprocesses explicitly use UTF-8, and Python output streams reconfigure safely.

## Observed evidence and limits

| Evidence | Observed result | What it does not establish |
|---|---|---|
| C# build | Zero warnings and errors | Production correctness, target runtime parity or native UI |
| Session/native parser fixture | Complete port transitions, refusal/preservation and retry checks listed below | A production core, complete grammar or certified kernel |
| Numeric/identity cross-check | Exact decimal unit bits, JCS UTF-16/string/number bytes, BLAKE3 digests, deterministic 2505-vector corpus | Exhaustive all-binary64 canonicalizer proof |
| Persistence state model | Both named platform contract models exercise five fault stages, competing creator and missing primitive | Actual Windows/macOS handle semantics or power-loss durability |
| Actual local filesystem | Same-directory hard-link no-replace preserves competing file and publishes complete new bytes | Ancestor/reparse safety, native adapter confinement or Windows runtime |
| Geometry authority | Allowlisted semantic hashes exercise positive binding; others Not assessed | No sampled or continuous geometry certificate is produced |

The Example canonical Surface hash is
`6da535f92a67ad5b8dca0a724cafbef2cb4f71a6101670c079f1c0973898baf6`.
The three unit-equivalent spellings yield `3f8cc5b8dc55000d`; source BOM, CRLF, comments and generated explicit
IDs remain separate exact bytes. The native branch-history image is 7930 bytes with every line ≤4096 bytes.
Its encoded length exceeds a single line, so readback also proves the pretty/chunk writer rather than a
compact-whole-envelope assumption.

The synthetic parser handles the declared inline-foil subset, not full FoilDSL. Unsupported syntax is refused
without acceptance. Diagnostics have a compiling DTO and exercised UTF-8 location mapper, but complete parser
phase/range aggregation is not implemented. Geometry allowance is deliberately a fixture, paired with binding
tests; it must never be reused as a production geometry authority. Store port capability semantics are frozen
by design; a native adapter may not overwrite until its actual OS race/fault suite passes.

## Independent-review corrections and controls

Root found outward recovery/source arrays could share mutable session storage, persisted operation IDs could
lose retry bindings on reopen, and Open/Reopen could accept a non-null unrelated certificate. The author fixed
those classes with defensive copies, complete durable edit receipts and exact authority binding checks.
Ruling 10 approves the receipt schema; base and resulting target membership are checked. Root additionally
required long escaped ID line closure, normalized dirty-state identity separate from disk formatting, and
public save/reopen ports. All have named fixtures below. No historical pre-fix source was retained: mutation
evidence below is explicitly **constructed guard/copy removal**, not a claim of historical execution.

The first expanded test itself changed its own outward draft buffer then incorrectly hashed that corrupted
local copy as an oracle. Its failure was `Validate_FixtureAuthority_BindingComplete`; the assertion now reads
the untouched session snapshot, separately proving external mutation has no effect. This was an oracle-fixture
error, not a discovered production geometry bug. Owner also found malformed root Apply replay, Unicode decimal
digit acceptance and EOF exhaustion in truncated length parsing. Explicit initial-Open replay, ASCII numeric
tokens and guarded token access now have stable-refusal/no-adoption regressions. Initial build used a relative NUGET_PACKAGES path and was
rejected; reproducible commands now use absolute task-local cache paths. Coordinator owns the unleased
defect-register consolidation for these classes; author does not edit that register outside its scope.

## Executed assertion receipt

<!-- RECEIPT:BEGIN -->
Observed osx-arm64: **89 C# checks, 42 Python checks, 2505 cross-runtime vectors**, all passed.
Measured oracle duration: 0.257421000 seconds; not a product performance budget.

| C# executable assertion | Result |
|---|---|
| `Decimal_Units_14_049_ExactEquivalent` | PASS |
| `Decimal_Tie_Even` | PASS |
| `Decimal_ZeroHugeExponent_Zero` | PASS |
| `Decimal_NegativeUnderflow_NormalizedZero` | PASS |
| `Decimal_CompensatedExponent_Equivalent` | PASS |
| `Decimal_TokenLimitBoundary_Admitted` | PASS |
| `Decimal_Underflow_Zero` | PASS |
| `Decimal_Subnormal_Minimum` | PASS |
| `Decimal_HugePositiveExponent_Bounded` | PASS |
| `Decimal_HugeNegativeExponent_Bounded` | PASS |
| `Decimal_Overflow_Refused` | PASS |
| `Decimal_TokenLimit_Refused` | PASS |
| `Decimal_UnicodeDigits_StableLexicalRefusal` | PASS |
| `Jcs_Utf16Ordering_ExponentAndEscapes` | PASS |
| `Blake3_OfficialEmptyVector` | PASS |
| `Open_MissingIds_MaterializationPreservesMeaning` | PASS |
| `Materialize_Repeated_Idempotent` | PASS |
| `Patch_IndependentRail_ExactUntouchedSuffix` | PASS |
| `Patch_ExactRoundTrip_SI` | PASS |
| `Source_Trivia_SHAChangesSurfaceStable` | PASS |
| `Diagnostic_Utf8ScalarCrLf_ExactLocation` | PASS |
| `Diagnostic_InteriorUtf8Byte_Refused` | PASS |
| `Parser_InvalidUtf8_Refused` | PASS |
| `Parser_TrailingUnknown_Refused` | PASS |
| `Parser_DuplicateField_Refused` | PASS |
| `Parser_HugeExponent_Refused` | PASS |
| `Open_TruncatedLength_StableRefusal` | PASS |
| `Open_TruncatedLength_NoAdoption` | PASS |
| `Open_UnrelatedCertificate_Refused` | PASS |
| `Open_IdDiff_RequiresAcceptance` | PASS |
| `Open_UnacceptedIdDiff_NoAcceptedState` | PASS |
| `Open_Retry_ExactlyOnce` | PASS |
| `Open_OperationChanged_Refused` | PASS |
| `Draft_RetargetBegin_Refused` | PASS |
| `Draft_ExternalRetargetMutation_PreservesOwnedTarget` | PASS |
| `Validate_FixtureAuthority_BindingComplete` | PASS |
| `Validate_Cancelled_NotAssessed` | PASS |
| `Apply_ForgedHash_Refused` | PASS |
| `Apply_StaleGeneration_Refused` | PASS |
| `Apply_Retry_ExactlyOnce` | PASS |
| `Apply_OperationPayloadChanged_Refused` | PASS |
| `Undo_RestoresSource` | PASS |
| `Redo_RestoresRevision` | PASS |
| `Reopen_ApplyRetry_DurableExactlyOnce` | PASS |
| `Reopen_OperationKindReuse_Refused` | PASS |
| `Reopen_ApplyTargetReuse_Refused` | PASS |
| `Reopen_RedoStack_Rebuilt` | PASS |
| `Apply_AfterUndo_RedoClearedHistoryRetained` | PASS |
| `Validate_UnknownGeometry_NotAssessed` | PASS |
| `RecoveryAndEnvelope_CallerMutation_PreservesOwnedBytes` | PASS |
| `Recovery_Offer_SeparateAccepted` | PASS |
| `Recovery_Resume_OriginalBase` | PASS |
| `Cancel_AcceptedUnchanged` | PASS |
| `Snapshot_CallerMutation_DoesNotChangeSession` | PASS |
| `Save_AcknowledgedImage_Clean` | PASS |
| `Native_EncodeDecode_ExactSource` | PASS |
| `SaveRequest_AsyncBoundary_CapturesDefensiveImage` | PASS |
| `SaveRequest_CreateMode_DerivedFromDiskToken` | PASS |
| `Reopen_DifferentWhitespace_Clean` | PASS |
| `Reopen_OpenRetry_DurableExactlyOnce` | PASS |
| `Reopen_PersistedUndoRetry_NoCursorMovement` | PASS |
| `Reopen_ApplyDraftReuse_Refused` | PASS |
| `NoOp_SameSession_ConflictingActionRefused` | PASS |
| `NoOp_Reopen_VolatileIdExpired` | PASS |
| `Reopen_UnknownGeometry_Refused` | PASS |
| `Reopen_UnrelatedCertificate_Refused` | PASS |
| `Recovery_IncompleteSource_OfferedWithoutAcceptance` | PASS |
| `Growth_ApplyOverflow_RefusedBeforeMutation` | PASS |
| `Growth_Refusal_PreservesDraftFactsAndDirty` | PASS |
| `Growth_RecoveryOverflow_RefusedBeforeReplacement` | PASS |
| `Growth_RecoveryRefusal_PreservesPriorImage` | PASS |
| `Save_LateAcknowledgement_NewerRevisionRemainsDirty` | PASS |
| `Native_LongId_InSourceChunks_Readable` | PASS |
| `Native_LongEscapedId_ApplyLineOverflow_PreflightRefused` | PASS |
| `Native_LongEscapedId_RecoveryLineOverflow_PreflightRefused` | PASS |
| `Native_LineOverflow_PreservesHistoryAndDraft` | PASS |
| `Native_WriterLines_WithinReaderLimit` | PASS |
| `Native_DuplicateKey_Refused` | PASS |
| `Native_UnknownField_ReadOnly` | PASS |
| `Native_MissingReference_Refused` | PASS |
| `Native_IllegalRedo_Refused` | PASS |
| `Native_FirstApplyInsteadOfOpen_Refused` | PASS |
| `Reopen_FirstApplyInsteadOfOpen_Refused` | PASS |
| `Reopen_MalformedRoot_NoAdoption` | PASS |
| `Native_SourceTamper_Refused` | PASS |
| `Native_SizeOverflow_Refused` | PASS |
| `Native_OperationCollision_Refused` | PASS |
| `Native_NullApplyReceipt_Refused` | PASS |
| `Native_ReceiptTargetMissing_Refused` | PASS |

| Independent Python assertion | Result |
|---|---|
| `CSharp_DeclaredChecks_AllExecuted` | PASS |
| `Decimal_IndependentFraction_1` | PASS |
| `Decimal_IndependentFraction_2` | PASS |
| `Decimal_IndependentFraction_3` | PASS |
| `Decimal_IndependentFraction_4` | PASS |
| `Decimal_IndependentFraction_5` | PASS |
| `Decimal_IndependentFraction_6` | PASS |
| `Decimal_IndependentFraction_7` | PASS |
| `Decimal_IndependentFraction_8` | PASS |
| `Decimal_IndependentFraction_9` | PASS |
| `Decimal_IndependentFraction_10` | PASS |
| `Decimal_IndependentFraction_11` | PASS |
| `Decimal_IndependentFraction_12` | PASS |
| `Jcs_IndependentLibrary_ExactBytes` | PASS |
| `Blake3_IndependentBinding_ExactDigest` | PASS |
| `Example_Jcs_ExactBytes` | PASS |
| `Example_Blake3_ExactDigest` | PASS |
| `Example_Source_BomCrLfRetained` | PASS |
| `Native_SourceDigest_e8319b7dccd3` | PASS |
| `Native_SourceDigest_5e4e13ea83af` | PASS |
| `Native_IndependentCursorReplay_BranchClearsRedo` | PASS |
| `Batch_CompleteResponseCount` | PASS |
| `Jcs_DeterministicBitCorpus_IndependentAgreement` | PASS |
| `Decimal_DeterministicRationalCorpus_IndependentAgreement` | PASS |
| `Resource_HostileExponent_PromptRefusal_1e1000000000` | PASS |
| `Resource_HostileExponent_PromptRefusal_1e-1000000000` | PASS |
| `macOS-contract_before-write_OldOrCompleteNew_Model` | PASS |
| `macOS-contract_partial-write_OldOrCompleteNew_Model` | PASS |
| `macOS-contract_before-flush_OldOrCompleteNew_Model` | PASS |
| `macOS-contract_before-publish_OldOrCompleteNew_Model` | PASS |
| `macOS-contract_after-publish_OldOrCompleteNew_Model` | PASS |
| `macOS-contract_CompetingCreator_NoOverwrite_Model` | PASS |
| `macOS-contract_MissingPrimitive_NoFallback_Model` | PASS |
| `Windows-contract_before-write_OldOrCompleteNew_Model` | PASS |
| `Windows-contract_partial-write_OldOrCompleteNew_Model` | PASS |
| `Windows-contract_before-flush_OldOrCompleteNew_Model` | PASS |
| `Windows-contract_before-publish_OldOrCompleteNew_Model` | PASS |
| `Windows-contract_after-publish_OldOrCompleteNew_Model` | PASS |
| `Windows-contract_CompetingCreator_NoOverwrite_Model` | PASS |
| `Windows-contract_MissingPrimitive_NoFallback_Model` | PASS |
| `LocalFilesystem_NoReplace_CreatorPreserved` | PASS |
| `LocalFilesystem_NoReplace_CompleteNewFile` | PASS |

| Constructed mutant | Observed failing regression | Process result |
|---|---|---|
| `root-apply` | `Native_FirstApplyInsteadOfOpen_Refused` | -6 (expected nonzero) |
| `unicode-digits` | `System.FormatException` | -6 (expected nonzero) |
| `borrowed-recovery` | `RecoveryAndEnvelope_CallerMutation_PreservesOwnedBytes` | -6 (expected nonzero) |
| `lost-durable-retry` | `DSL-CONFLICT` | -6 (expected nonzero) |
| `unbound-authority` | `Open_UnrelatedCertificate_Refused` | -6 (expected nonzero) |

The durable-retry mutant removes only reconstruction of Open/Apply registry entries; the identical persisted Apply retry then throws `DSL-CONFLICT` rather than returning its prior result. The other mutants remove the recovery defensive copy and the exact authority binding guard. All three unmutated regressions pass in the full suite above.
<!-- RECEIPT:END -->

## Source fingerprints

<!-- FINGERPRINTS:BEGIN -->
| Source | SHA-256 |
|---|---|
| `tools/spikes/ApplicationContracts/ApplicationContracts.csproj` | `4a67878b7b5d5a9512f836a4ccd38d6c55697f1b438db532013ac318ba2f2f62` |
| `tools/spikes/ApplicationContracts/Program.cs` | `95ec719b8e17fac4f7e33e32d4f8d7e910063fcec52f5cabf9b26937709e5b4f` |
| `tools/spikes/application-session-contract-vectors.py` | `595556833579d94beff37ec2d1df19bcd1aba25ce8f4769249b6cc367d427942` |
<!-- FINGERPRINTS:END -->

These fingerprints concern this contract source only. The earlier architecture proof predates a Coordinator
portable-I/O patch; this proof does not repeat its stale fingerprints or treat prior results as new execution.

## Sources and test selection

[RFC 8785](https://www.rfc-editor.org/rfc/rfc8785) defines canonical strings, UTF-16 property sorting and
ECMAScript numbers; [official BLAKE3 vectors](https://github.com/BLAKE3-team/BLAKE3/blob/master/test_vectors/test_vectors.json)
supply the empty digest used independently. The existing locally restored Blake3 2.2.1 package is reused,
not a newly selected dependency. FoilDSL §§3/5/7/8 and Rulings 8–10 are the product contract sources.

D0–D7 apply as enumerated in the design. D1 mutation evidence is the explicit mutants, not a claimed
mutation percentage. D2 uses deterministic seed 42019, plus adversarial boundary cases. D4 distinguishes
the real file primitive from the persistence state model. D7 geometry fake fidelity stops at the public
binding/identity boundary; there is no claim of geometric fidelity. AI/service directives are N/A.

## Gate and residuals

Author disposition: candidate ready for independent review after the listed checks. Root/Owner decide whether
the serial gate closes. No author self-clearance. The next product work still needs complete grammar and
diagnostics, certified conservative geometry/error bounds, actual OS store/telemetry, native UI/CLI workflow,
Windows live UIA/Narrator, macOS accessibility, signing/notarization and measured performance. No M1 delivery,
scientific values or full G3/production routing approval is claimed here.
