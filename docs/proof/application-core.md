---
id: proof-application-core
title: Production core implementation checkpoints
type: proof-pack
status: in-review
owner: "@cfd-application-core-20260923"
phase: implementation
tags: [application, core, parser, identity, proof]
links:
  - {to: design-application-contracts, rel: documents}
  - {to: coordination-contract-b-core, rel: depends-on}
  - {to: spec-foildsl, rel: depends-on}
review-by: 2026-10-23
summary: >-
  Records isolated, incomplete core increments with parser, identity and continuous subset evidence.
  Includes placed enclosures, session/history, authored projection and macOS store candidates; scientific and platform review remain open.
  This checkpoint is not a join candidate or M1 acceptance claim.
---

# Production implementation checkpoints

Goal: implement the UI-free source, certified geometry, owned authoring session and native persistence
contract frozen by Owner Ruling 13. Done when the entire required boundary set is implemented, exercised
red-before-green and independently reviewed. GUI, CLI, solver and export remain outside this track.
Tier T2; one serial author; no subagents. Requested model: gpt-6-astra. Effective model: Not recorded.

Assigned worktree: `/Users/mallalieut/projects/CFD-Workbench-feature-application-core`.
Branch: `feature/application-core`. Base: `598716f2931cab8ed94c0e618554c340134f25b0`.
Audit start: 2026-09-23T15:21:59Z. Exact tool-call/context usage is Not recorded by this harness.
The 90-call/55-minute boundary is a checkpoint, not a completeness criterion.

## Execution graph and current surface coverage

The compiled packet, design and architecture fix the graph: contract grounding and containment preflight
→ exact-number and identity red/green → whole-source grammar and static-phase red/green → continuous
geometry proof and preview → session/history/recovery → native primitive reproducers → native adapter
fault/race proof → full contract and architecture checks → independent review → handback.
Data dependencies make this one serial track. There is no parallel author or speculative adapter work.
Named behavioral REDs and controls first observed GREEN are distinguished in the receipts below.
The loop variant is the remaining failed contract assertions; a budget cap triggers a report and replan.

The required surface chain is native bytes → immutable rows → owned draft → certificate binding →
immutable projection → future GUI/CLI → geometry reader. The current increment reaches source bytes → static
parse projection → semantic identity → continuous subset proof → enclosed normalized section ordinate.
The third increment adds candidate session/history and preview boundaries; the fourth adds authored projection and a macOS store candidate.
No parsing success is a geometry certificate. No analysis or solver values are exposed.

Reuse decision: the reviewed B0 exact-decimal and canonical-number routines are moved into Core, with
new public-boundary guards and independent regression assertions. The B0 geometry allowlist is not reused.
The projects use .NET SDK 10.0.203, net10.0, nullable enabled, warnings as errors and Blake3 2.2.1.
The test executable uses ordinary deterministic assertions without adding a test-framework dependency.

## Containment incident and repair

The first gate used `tempfile.gettempdir()` with one shared directory name. On macOS that resolved to
`/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-application-core-20260923`, outside the approved
literal `/tmp` plan. .NET also printed a development-certificate installation banner. Work stopped.
The observed localhost PFX was dated 2026-05-05, before the session, so a newly installed certificate was
not established from that banner. No trust command or certificate cleanup ran. The old cache is preserved.

Ruling 14 permitted one leased-gate repair and one separately approved retry. The gate now allocates a
unique directory with an explicit `/tmp` parent, reports its `/private/tmp` canonical alias, puts all
six cache/temp variables and build outputs below it, and sets `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`
before launching .NET. It validates process observation before launch, records owned PID/start pairs,
checks quiescence, and uses a direct-child fallback with descendant status Not assessed if observation
fails. Windows fails closed before launch; its process-tree adapter and runtime remain Not assessed.
That guard is temporary and does not satisfy the eventual Windows verification requirement.

The approved retry at `/tmp/cfd-application-core-20260923-uj2qxazf` reported no certificate banner,
the expected compiler failure, and no remaining owned child. Coordinator independently inspected its
environment, raw build log, process receipt and process absence before authorizing implementation again.
Every later invocation has a distinct scratch directory with environment/build/test/process receipts.
No local runtime receipt proves a sandbox or a complete negative statement about all global effects.

## Executed checkpoint evidence

All paths below have the prefix `/tmp/cfd-application-core-20260923-`; on this macOS host they resolve
under `/private/tmp`. Runtime: osx-arm64, SDK 10.0.203. Gate command: `python3 tools/verify-application-core.py`.
Each run stores raw process output under `receipts/build.log` and, when launched, `receipts/tests.log`.

| Run suffix | Observed result | Scope |
|---|---|---|
| `ge_iseuu` | Build 0; tests 1; nine executed FAIL assertions | Unimplemented numeric/JCS/hash seams, orderly test failure |
| `8sm25hl7` | Build 0; tests 0; nine PASS | Initial exact-unit, tie, zero, subnormal, resource, JCS and BLAKE3 cases |
| `tco3vklj` | Build 0; tests 1; two FAIL | Unsupported public scale and trailing-newline token acceptance |
| `_w_8o2bx` | Build 0; tests 0; thirteen PASS | Both guards fixed; extreme integer scales refused before allocation |
| `e0mfdhzo` | Build 0; tests 1; fifteen parser FAIL | Grammar/source seams initially unimplemented |
| `ziez0_db` | Build 0; tests 1; five FAIL | Spoofed EOF, standalone asset grammar and actionable diagnostic controls |
| `z5pb_9xy` | Build 0; tests 0; thirty-six PASS | Those five regressions corrected |
| `ahej0qvi` | Build 0; tests 1; two FAIL | Late numeric overflow priority and two-assignment syntax requirement |
| `dp3u00hb` | Build 0; tests 0; thirty-nine PASS | Current checkpoint; zero compiler warnings/errors |

The first unhandled stub test in `t7nogco7` exited by exception (-6). The runner was corrected to catch
and report each failing test and return exit 1, rather than aborting the runtime. That earlier run is
not the nine-test red receipt. All listed final process receipts report `live: {}` and `quiescent: true`.

| Claim | Named control / oracle | Red evidence | Confidence and limit |
|---|---|---|---|
| Exact scaled decimal rounding | `Decimal_UnitEquivalent_ExactlyOneRounding`, expected bits `3f8cc5b8dc55000d`; halfway/subnormal/zero cases | `ge_iseuu` | Verified cases, not exhaustive numeric proof |
| Public scale cannot request arbitrary powers | `Decimal_UnsupportedScale_RefusesBeforeScaling`; only grammar scales 0,-2,-3,-4,-6 admitted | `tco3vklj` | Verified; extreme min/max tests added after guard to avoid intentionally huge allocation |
| Numeric API consumes a whole token | `Decimal_TrailingNewline_RefusesNonToken`; strict `\A`/`\z` | `tco3vklj` | Verified exact regression |
| Canonical number and hashing boundary | Negative zero, 1e21 spelling, official empty BLAKE3 vector | `ge_iseuu` | Verified cases; independent wide-number corpus still required |
| Grammar consumes the full source | Foil/section/assertion positives; syntax/version/unit/reference negatives; spoofed EOF controls | `e0mfdhzo`, `ziez0_db` | Verified corpus, not complete language conformance |
| Ordered diagnostics | Lexical-after-version, syntax-after-structure, late numeric-overflow-before-structure | `e0mfdhzo`, `ahej0qvi` | Verified combinations; combined numeric-overflow/syntax-failure ordering still needs work |
| Source and diagnostic ownership | Source mutation, immutable diagnostics, no public success constructor | Source-byte test red in `e0mfdhzo`; new diagnostic/constructor controls first observed green | Verified current behavior; no historical red claimed for diagnostic/constructor controls |
| Missing IDs form an explicit candidate | Materialization keeps semantic hash and is idempotent | First observed green | Verified fixture; explicit session acceptance is not implemented |
| Actionable curve/reference diagnostics | Curve entity plus degree/count/order requirement; missing profile named | `ziez0_db` | Verified named cases; remaining diagnostic detail is incomplete |

## Bounded independent parser review

Root reviewed the frozen `z5pb_9xy` DLL separately: 10/11 direct cases passed, with zero
crashes across 1,565 prefix truncations and 1,000 seeded byte inputs. This is bounded
robustness evidence, not whole-language conformance. Receipt:
`/tmp/cfd-parser-review.yVciSs/review-receipt.json`, SHA-256
`c09c41561d754da4a4a7e2c61e91a5973909ad57fda438b24af4e7376b74d822`.
The sole direct finding was the missing mandatory second assignment receiving a reference
rather than syntax diagnostic. The named regression failed in `ahej0qvi` and passed in
`dp3u00hb`. Root independently reran that named case against the frozen latter DLL,
observing syntactic `DSL-SYNTAX`, retained exact bytes and `IsParsed == false`.
Follow-up receipt: `/tmp/cfd-parser-review.yVciSs/followup/review-receipt.json`, SHA-256
`e53f4be04806d092b6748b5484779acdff11abea9e849099dda2be026c39817a`;
frozen DLL SHA-256 `d6a70401dc8120465b58e38ca0b3615597902202dbdc606e5156457b3eb203d5`.
The follow-up closes this named finding only; it does not rerun or extend the earlier corpus.

## Defect classes and controls carried forward

Coordinator owns the unleased defect register. Findings sent for consolidation: ambient temp defaults
escape an exact containment plan; SDK first-run effects need explicit disabling; process observation must
work before process acquisition and degrade honestly after failure; public secondary numeric parameters
must pass the same resource admission as tokens; line anchors are not whole-token anchors; parser EOF
must be a structural end condition rather than a forgeable spelling; shared grammar helpers must retain
their caller's narrower production; owned projections must not retain mutable caller collections.
Named tests/gate guards above implement the current controls. No control is inferred from a green exit alone.

## Second bounded increment

Coordinator authorized at most 60 additional tool calls or 35 minutes, no fan-out and a checkpoint before
session/native work. Audit start: 2026-09-23T15:59:58Z. First checkpoint commit:
`5f40af0888c529b057c6630ac3b1b130c47515ee`. Exact aggregate tool count remains Not recorded;
the author checkpoints conservatively rather than claiming a measured call count.

Owner Ruling 15, read at the coordination worktree `docs/notes/rulings.md:108`, resolves dependent numeric
phases. Malformed token/encoding errors precede syntax; a well-spelled decimal is bound to its known role
and actual unit before finite conversion. Unknown units/evaluator/roles never become implicit scale zero.
Blocking syntax may prevent binding, so no recovering parser or speculative overflow is introduced.
Named cases check exact diagnostic code, category, source span, retained bytes and no parsed adoption:
unknown unit/channel/evaluator, missing unit, malformed-plus-syntax, compensated `1e309 mm`, overflowing
`1e309 m`, and known overflow with bad degree/reference. `3bbh88ex` was RED; `83ry315v` is GREEN.
The missing-unit test initially expected the wrong span: `Word()` consumes `evaluator` as the prospective
unit, and the next required keyword fails at the quoted `"cfdw-cv"` token. Inspection corrected this exact
oracle; neither the span assertion nor production error reporting was weakened.

`PatchRail` owns only an explicit leading/trailing CV ordinate token. It emits the terminating decimal of
the exact binary64 rational after inverse SI scaling, reparses and compares requested SI bits. The caller's
source remains unchanged, the other rail suffix is byte-identical, and repeated patching is idempotent.
Executable RED `4d94zl4g` precedes GREEN `0jn8g1l8`. Missing IDs, non-rail targets and nonfinite values refuse.

The scientific candidate uses exact rational recovery of Bernstein span coefficients from p+1 rational
evaluations, derivative hull monotonicity, globally separated independent rail hulls, thickness hulls in
(0,1), common-basis profile separation and branch-and-bound maximum enclosure. Repeated zero-length knot
spans are excluded. Inconclusive sufficient bounds are Not assessed. Proven nonpositive chord is Invalid /
DSL-GEOMETRY; a violated root tangent lock is Invalid / DSL-LOCK; unsupported subset shapes remain distinct.
Result distinction RED `024eavzz` precedes GREEN `3bbh88ex` (that run still had parser RED failures).
The geometry-positive case was RED `vtkmd5he`, then GREEN `5pqhauab`.

Certificates retain source/Surface binding, algorithm version, domain intervals and rational hull/max
witnesses; doubles are outward presentation enclosures. Maximum convergence is relative, so its propagated
normalization error is at most 1e-12. `SectionAt` uses exact inverse-abscissa subdivision and interval
arithmetic through camber, effective thickness and normalization; no midpoint clears a proof obligation.
Normalized-section behavior was RED `oavl6s_y` then GREEN `hvrkd70l`. The independent dyadic polynomial
maximum is 25/128. At the symmetric peak, camber is exactly zero and normalized thickness is exactly one;
the fixture's constant binary64 0.12 thickness channel therefore gives exactly half that value, ±0.06,
independently of the implementation output. Tests enclose those values. Repeated interior
knots, subnormal positive chord, thickness immediately below one and unsupported independent profile bases
are named cases. These tests are bounded evidence, not a full scientific acceptance verdict.

The exact arithmetic ceiling is 32768 bits; maximum subdivision has a 4096-node/64-depth cap and inverse
evaluation a 128-depth cap. Time checks run at span, interpolation-row, elimination-row and subdivision
boundaries. The one-second limit is currently cooperative; no hard wall-time guarantee or adversarial
budget completion is claimed. Placed 3D preview/trigonometric error and final admission binding remain
unimplemented. `ProofScope` explicitly records that limitation; there is no session Apply path.

Normalized-section gate `hvrkd70l`: build exit 0, zero warnings/errors, test exit 0, 93 named PASS. This includes all
24 finite binary64/serialization pairs in [RFC 8785 Appendix B](https://www.rfc-editor.org/rfc/rfc8785.txt),
first observed GREEN in `juj8kses`; no historical RED is claimed for those independent published vectors.
Every receipt root is `/tmp/cfd-application-core-20260923-<suffix>`, with canonical `/private/tmp` alias.
Latest build PID 81652 and children 81655, 81657, 81675, 81689, 81702, plus test PID 81719 were recorded by
PID/start identity; retained process receipts show empty live sets.
No new containment incident or certificate/trust banner occurred. Old incident scratch and certificate
evidence remain preserved. The four later session/store implementation and test paths remain absent.

## Open obligations at the first checkpoint

This is an isolated checkpoint, not a partial join. Remaining work includes full language boundary/fuzz and
phase-order breadth, complete independent identity corpus, adversarial continuous rational
geometry and bounded placed-preview error, session transaction/certificate ownership, immutable
native history and replay, growth/recovery admission, actual native primitive/fault/race proof, normal-path
256-event telemetry and redaction, architecture checks, cross-platform evidence and independent veto review.
The native persistence project is currently only a project-reference boundary, with no adapter file operations.
No Windows runtime, native UI, whole-language conformance, M1 delivery or product acceptance is claimed.

## First checkpoint source fingerprints (commit 5f40af0)

| Path | SHA-256 |
|---|---|
| `global.json` | `6863ab1b69640d64946ef783866504ca8dfda9ea05d0c33d013610b64996c7df` |
| `CFDWorkbench.slnx` | `b712debe9c849de9d181a155ca4f8e7b236866a577a67b6e6756284509e1b743` |
| `src/CfdWorkbench.Core/CfdWorkbench.Core.csproj` | `f463ff0c27394c9762480baab31de7870ffe2c05797a956c1bb7a21a4e2cd829` |
| `src/CfdWorkbench.Core/Contracts.cs` | `ec856432d22cbe1536b03c7184bf77d1833fceea8d6ee4858dcc8f65f5d531c3` |
| `src/CfdWorkbench.Core/FoilSource.cs` | `9542fc623cf7d679cca63635ba6c0c83397cd91196e049399d69e8a5633904f9` |
| `src/CfdWorkbench.Core/Identity.cs` | `4deb9e1c6bf128ef76477bfc77abc8e73673245307c7e2ea0e9dc3ac945043c5` |
| `src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj` | `b88d9a982e2c8ce1bcf7bac7002b8906e5af04a6cbf28754af69202ec4443fc7` |
| `tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj` | `1bce4fd7f785517930ea9c3b5705cf2b6d01fc343f967f5dd33fb3206cccbbb0` |
| `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` | `cc08a357994a6c31f62732a316b0599647af5718dd7356f4201e16c8a892d24e` |
| `tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs` | `326513b7176408f2b90a5a8b99a09b0c3f7dd503afc0f7d29e45610eeb7a97cf` |
| `tools/verify-application-core.py` | `7b3581f7d25bb813ce06ece440b9b63ec1701ff5cda2fe72dbd898e5de171dbe` |

## Second checkpoint source fingerprints

The focused thickness boundary now edits only its channel and asserts exactly seven token replacements.
Final gate `ux3t5axm` again reports build/test exit 0, zero warnings and 93 PASS; all owned children are quiescent.
The final build PID is 82345; test/process identities are retained in that unique receipt root.

| Path | SHA-256 |
|---|---|
| `src/CfdWorkbench.Core/FoilSource.cs` | `8f1d653898d6b419e5ce71c845bd44043e73d6c804e596446be814531bc23c8b` |
| `src/CfdWorkbench.Core/Identity.cs` | `7abc1e0284d6b9043a5c938c5bf704888bde4d9d9c371a2198017940d70195b6` |
| `src/CfdWorkbench.Core/Geometry.cs` | `4e16f6f88701c7d9a718223ac3ce72e7704c81167d5464c61d8b8ec00b34b461` |
| `tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs` | `1f7745276f2f549e2d05e95f4ea7a612f8308e25be12e53447fa9ea89f36fea6` |
| `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` | `02e4ac2ca399687a7948a69d572670fe438fe663d78e88a26717c28df27926b7` |
| `tests/CfdWorkbench.Core.Tests/GeometryTests.cs` | `9254fb961bf57ddd1a683beafbc502cbeee3d899b24784b2fe67d03cd1b8a878` |

## Third bounded increment: placement, session and native primitive boundary

Fresh audit start: 2026-09-23T16:17:34Z. Read-only preflight observed clean
`feature/application-core` at `5b5b4895a81eaf072cf5165ba88e8c44ae7f6202`, assigned absolute cwd,
and allowed Geometry/AuthoringSession exact leases. Requested model remains gpt-6-astra; effective model
and exact aggregate call count are Not recorded. This remains a partial isolated checkpoint, not a join.

Placed coordinates use exact interval arithmetic and a rational Taylor enclosure on angles in [-1,1].
The evaluator first reads the degree-ordinate twist curve and then applies the pinned once-rounded
radians conversion, as FoilDSL section 6 requires. It does not evaluate a spline over pre-rounded radian CVs.
Point evaluation was RED `9cofy31x`, GREEN `69xr8wm5`. Whole-domain angle/error admission was RED
`gve0q7dq`, GREEN `lnj_sj8p`. Assess now derives and retains an exact rational uniform width bound across
eta/x in [0,1], upper/lower and both ports. It includes inverse-abscissa ordinate error, relative maximum
normalization, camber/thickness products, Taylor remainder, angle rounding, placement and outward conversion.
Profile inverse tolerance scales with the exact maximum lower bound. Degree times ordinate range bounds the
inverse ordinate hull width after 127 bisections. A 10 nm maximum coordinate interval width is a chosen
conservative implementation policy, explicitly marked simplify, not a normative language tolerance.
Queries return intervals; they do not authorize an otherwise unproved shape or certify a tessellated mesh.

No-crossing argument for the admitted subset: positive half-span makes Y=halfSpan*eta injective, so distinct
span stations cannot intersect. Positive chord and thickness preserve scale. The two section graphs are
strictly separated on their open chord domain and meet only their declared endpoints. The leading-edge
pivot rotation is rigid within each constant-Y plane, preserving that section simplicity. This argument
requires every admitted subset precondition; it does not extend to arbitrary blends or unsupported tips.

Final elapsed checks precede successful certificate/section/point returns. Callers may reduce the one-second
budget but cannot raise it. Explicit zero-budget and raised-budget controls were RED `r02r5m02`, GREEN
`4qhukw7e`. Polling remains cooperative, not a hard real-time scheduling guarantee. Arithmetic is bounded;
a scheduler pause or expensive individual bounded operation may exceed the requested wall time before
refusal. Universal timeout/power-loss/platform proof is not claimed.

Session tests were eight executed REDs in `qu0ljdyb`, then GREEN in `k_j6brrg`. Production session/native-v1
serialization/replay reuse the B0 contract implementation, with random IDs, exact production geometry proof,
private per-session assessment ownership, one in-flight validation and full immutable binding. Tests cover
candidate-ID acceptance, cancel preserving bytes/history, stale generations and assessments, outward mutation,
invalid/cancelled proofs, concurrent same-generation updates, durable operation retry after Undo/Reopen,
recovery offer/resume, late save acknowledgement, atomic history-growth refusal and hostile native schema.
Native parsing adopts only after schema/reference/hash/replay checks and fresh current geometry proof.

Root found a real stale-authority resurrection: a cancelled draft UUID could be reused with the same binding.
`7ms4h89s` records named REDs for that finding, the caller-raised native cap, empty recovery and uncaptured
save acknowledgement. `ttbkc0t_` closes those named tests: draft IDs retire on Begin and historical edit/recovery
IDs are reconstructed on Reopen; the public cap cannot exceed 8 MB; recovery alone may contain zero base64
chunks, while accepted sources still may not; empty recovery resumes as an invalid draft and cannot Apply.
SaveImage records bounded pending capture hashes and acknowledgement must match a prior captured image.
The store remains responsible for actual publication success. This does not authorize a caller to assert
OS publication, nor confuse normalized dirty identity with the exact disk conflict token.

All source/native input bytes are snapshotted before parsing so caller mutation cannot change bytes between
semantic parsing and authority capture. Source/Surface hashes are lazily cached over owned immutable input.
This hardening was first observed green; no historical behavioral RED is asserted for the concurrent-copy case.

The local session ring holds at most 256 events and is discarded at close. It records named language.parse,
identity.canonicalize, geometry.validate/preview, document.open/apply/cursor/recovery/save/reopen phases,
measured duration, known byte counts, generation/evaluator when established, local ephemeral trace ID and
observed retained-fact counts. Unknown fields are null, not estimated. Returned invalid/cancelled assessments
supply the event outcome instead of being labelled OK. No source, path, name, vertex, exception text or content
hash is emitted. Named phase RED `4jf0w4o6` precedes GREEN `f02ah0lr`; final tests also inject a private source
marker and check preview binding/correlation, cancellation outcome and ring redaction. There is no exporter
or disk telemetry archive. Broader adapter telemetry remains part of the unimplemented store boundary.

Native primitive prerequisite, before any ProjectStore.cs adapter: installed MacOSX.sdk headers grounded
openat/no-follow/directory/CLOEXEC flags, 144-byte stat64 fields and linkat/renameat/fsync signatures.
`NativePrimitive_MacHandleRelativeNoReplaceAndFlush` passed in `9s8nffho`: a second linkat creator received
EEXIST and preserved original bytes; no-follow open rejected a symlink; a held reader retained its original
inode after replacement; file and directory fsync returned success. This proves those observed local API
behaviors only. Complete ancestor traversal/replacement, owned temp/claim cleanup, cancellation, short writes,
disk full, post-publication uncertainty and cooperative overwrite races still need adapter tests. Windows
native runtime remains Not assessed. Primitive artifacts remain under each unique gate scratch/tmp directory.

Latest verification: `po5pbru9`, build/test exit 0, zero build warnings/errors, exactly 126 PASS counted from
retained test output. Build PID 89409 and observed children 89412/89414/89430/89445/89460, plus the test process,
have empty owned live sets in retained PID/start receipts. The new primitive fixture root is
`/private/tmp/cfd-application-core-20260923-po5pbru9/tmp/native-primitives-c7e5ad7fe6624d83810eac55b621a704`.
No production persistence adapter exists yet; that is the next dependency. Old containment evidence is preserved.

### Additional bounded independent evidence

The author read the receipts and independent numeric/placement oracle source; these are separate frozen-DLL
reviews, not universal conformance or clearance of later source.

| Frozen review | Observed bounded result | Receipt SHA-256 |
|---|---|---|
| `/tmp/cfd-identity-review.MnyjUB/review-receipt.json` | 1000 exact Python Fraction post-unit cases and 4996 finite raw-bit spellings against Node v22.22.2 JSON.stringify; zero failures | `5f6fbee621c20fd16cd490905c156bb4a89dd3a54d8662b15fe2727a58543671` |
| `/tmp/cfd-section-review.CL5eAD/review-receipt.json` | Eight cases; independent nonlinear x=t^5 at x=1/32 gives exact upper47/512, lower-17/512 | `86ac9be5d4aacabb1cc36564f69058b4e3b1ab73cb804990d92daf70553b56f0` |
| `/tmp/cfd-placement-review.s4ZNVb/review-receipt.json` | 36 placed points, 108 coordinates versus Decimal precision110/Taylor70 at once-rounded angles; zero failures | `bba1fe3306f0e7194f15aa7619cb6073027b0d981f6e0c73d2d37c9a1c161d1f` |
| `/tmp/cfd-session-review.nK51bi/followup/review-receipt.json` | Cancelled draft reuse now DSL-DRAFT-REUSED with acceptedChanged=false; 30 wrong-type mutations refused; empty recovery accepted | `192c6d5f3bb9098419eea9d3a2280fff84660be6f92c0e4b5918a9c896f27413` |

The session follow-up raw program retains an old scope string. Its receipt and frozen DLL hash bind the actual
ttbkc0t_ follow-up; no raw output was rewritten. Coordinator owns defect-register consolidation for authority
identifier reuse, public resource-bound bypass, dependent interpretation, mutable input capture and event
success classification. Their named controls above prevent the observed class from silently recurring.

### Third checkpoint fingerprints and remaining work

The final readback counted 126 PASS in `po5pbru9/receipts/tests.log`. Build/test process receipts record
exit 0 and empty live sets; direct `ps` readback for all seven owned PIDs returned no rows. Test PID was
89477. No product source changed after that run. The following hashes bind the tested increment.

| Path | SHA-256 |
|---|---|
| `src/CfdWorkbench.Core/FoilSource.cs` | `f30141f6ed10893e961a173df4911a476716feebb8043197139dabd88ca8eefd` |
| `src/CfdWorkbench.Core/Geometry.cs` | `26cd769c7034d6e1020f2397529299eda7ec000f43e1b9e633089a23c5521925` |
| `src/CfdWorkbench.Core/AuthoringSession.cs` | `b7c46a84522eae83bdf1938d7ab897628715871566af9185403fbb40e4eccfa9` |
| `tests/CfdWorkbench.Core.Tests/GeometryTests.cs` | `de554cfcd92ee268cb194079450d63003f63b590e34313d37d1980b6c4fb27cd` |
| `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` | `0df67b0ada7487104232c082b49529547eed055b89c68068c38a210a7d768c54` |
| `tests/CfdWorkbench.Core.Tests/AuthoringSessionTests.cs` | `d6140e4aab65b53eea60ad225f4538cb57db4e75f92c3ceee90b57ff05aa3b78` |
| `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` | `dcfebe26e3890087d6cd245be76dabe11da86e617f253a146eb052335336f595` |

Remaining: production ProjectStore, native ancestor/identity/claim/publication and fault/race tests,
cross-platform gate/runtime proof, architecture checks and independent whole-domain mathematical/Data
review. Session/native serialization and local telemetry now have the bounded executable evidence above;
they are not blanket contract acceptance. Coordinator requested this clean checkpoint yield the active seat
for review before another bounded serial continuation. No partial join or acceptance is requested.

## Fourth increment: native store and authored consumer boundary

Audit start 2026-09-23T16:57:22Z. Own preflight observed assigned cwd, clean branch and
`a8a635168f01cab38b87c91e7e7dd3675e4d9796`; ProjectStore, tests and Contracts exact leases allowed writes.
One serial author, requested gpt-6-astra, effective identity and aggregate tool-call count Not recorded.
The bounded graph was primitive prerequisite → immutable port RED → native handle implementation → fault/race
and lifecycle controls → authored public consumer RED → projection/diagnostics → checkpoint. Width stayed one;
no gate was removed to shorten the chain. Root/Owner review remains a separate node, not author clearance.

### Native adapter and limits of the evidence

`CfdWorkbench.Persistence.ProjectStore` now implements defensive SaveRequest/ReadResult and asynchronous
SaveAsync/ReadAsync. Null expected disk SHA means create-only; a non-null token is the exact original file
SHA, not normalized native history identity. The store moves bounded native I/O to a worker task. It does
not interpret or adopt native project contents; the session remains the parser and adoption authority.
The public save/reopen composition test captures an actual session image, publishes it, acknowledges exactly
that captured image, reads exact bytes/token and reopens another session with the same accepted revision.

On this measured macOS host, path resolution starts at an opened root and opens each directory component
relative to its retained predecessor with O_NOFOLLOW/O_DIRECTORY/O_CLOEXEC. Every chain link is rechecked
by device/inode/type before publication. Dot segments, relative paths, invalid UTF-8 strings, symlink ancestors,
symlink/nonregular targets and the case-insensitive reserved ASCII `.cfd-` target namespace are refused.
The native adapter rejects `/tmp` as a symlink ancestor; test fixtures use its canonical `/private/tmp` alias.
Directory/file handles remain owned through the operation. Read captures at most 8 MB through a held regular
file and checks metadata stability plus entry identity before exposing copied bytes.

New files use an exclusive same-directory owned temp, complete bounded writes, file fsync and atomic linkat
no-replace. Two actual concurrent creators are synchronized immediately before publication; exactly one wins,
the other gets DOC-CONFLICT, and the complete winner's bytes survive. Existing files use an exclusive fixed
directory-relative `.cfd-writer.claim`, exact held-file SHA checks and final entry/parent identity checks, then
renameat. Per Ruling19, all cooperating overwrites in the selected directory contend, including different
filenames. That conservative false contention avoids a guessed filesystem normalization policy. Case aliases
and equivalent parent spellings were measured; no Windows or universal filesystem claim follows.

Root's raw-filename claim finding was reproduced: `z8ih576w` records a second differently cased writer reaching
ClaimCreated while the first held its claim. The fixed directory claim closes that named test in `va2ff7_0`.
The final directory fsync now follows owned temp/claim cleanup; its ordering test was RED in `3bvkqqi6` and
GREEN in `va2ff7_0`. Cleanup verifies retained identity, preserves collided/replaced names and is idempotent
after its own successful unlink. It never removes a stale claim by name or age.

Cancellation before publication retains the old file. Cancellation after publication resolves the operation
without manufacturing a not-saved result. Failures after known publication return DOC-SAVE-UNCERTAIN with
publication-known true and durability false. A publication I/O error whose result is not established also
returns uncertainty, with no invented published hash; that named control was RED `x81f9kl0`, GREEN `0iub1e8v`.
Atomic no-replace collision remains a known DOC-CONFLICT. DurabilityConfirmed means the file fsync and final
directory fsync succeeded after owned cleanup; hardware power-loss durability has not been established.
Hash-check/rename is not OS compare-and-swap. Noncooperating or malicious same-user writers remain outside
the cooperative overwrite guarantee.

Primitive signatures/flags were grounded in installed MacOSX.sdk headers. Read/write/unlink were executed
in `y187xato` before their adapter code; prior linkat/renameat/fsync/stat probes remain retained. O_NONBLOCK
was initially authored before its separate FIFO probe, an ordering defect rather than retroactive preflight
proof. Its header value 4 and actual FIFO open/stat/refusal passed later in `x81f9kl0`. Another native-assumption
class was caught at review: Darwin ELOOP is 62, not the transcribed 40. Read-symlink refusal was RED `0c3x978a`
and GREEN `z7h0f5_c` after reading errno.h and correcting the map. Coordinator owns defect-register capture;
the named platform refusal and FIFO tests prevent silent recurrence of these measured boundary errors.

The suite exercises real parent-directory replacement, held readers across native rename, competing creators,
case aliases, separate-name contention, unowned temp/claim collisions, replaced-owned-temp preservation,
prepublication cancellation, before/after-publication failures, native read limits and nonregular files.
Partial writes are real writes deliberately fragmented by the test seam; disk-full and publication EIO are
injected boundary errors, not claims that a real volume was exhausted. The real storage-device/power-loss
boundary and Windows sharing/reparse/native primitives remain Not assessed. Missing/unknown platform
capabilities fail closed with DOC-UNSUPPORTED-PERSISTENCE; no path-only portable fallback exists.

### Actual I/O telemetry and lifecycle (Ruling19)

`ProjectStore(session)` writes measured store.read/save/publish/file-flush/final-directory-flush events into
the existing session ring via an internal friend-assembly hook. The app gets no second hidden ring. A standalone
store owns an otherwise empty private session/ring and disposes it. Each dispatched operation creates one
ephemeral trace ID before Task.Run; its native phase events share that ID. Fields carry actual elapsed time,
known byte counts and publication/durability facts. Generation/evaluator remain null at this opaque byte-store
boundary. No path, source, name, content hash, vertex identity or raw exception text is recorded.

Telemetry was behavioral RED `va2ff7_0` / `333ls0b1`, then GREEN `0yl6fbav`. Later named tests prove 256-event
retention, cancelled/conflicted outcomes, distinct operation traces, private-path redaction, injected-store
disposal retaining the caller session, owned-ring disposal clearing events and refusing new I/O, and in-flight
publication completing truthfully after disposal without retaining events. Telemetry failure cannot mask an
I/O result. Session capture/ack spans remain separate actions and are not presented as disk latency.

### Immutable authored projection (Ruling16)

SourceParse.Authored exposes source-bound parsed/IdCandidate/invalid facts. InspectAccepted binds them to the
captured accepted revision, Design/Surface identity and evaluator, alongside a fresh geometry assessment;
InspectDraft binds to base/draft/generation, including incomplete recovery. Availability does not certify
geometry. No projection can be passed as Apply authority. Old snapshots retain their old revision and bytes
after a later Apply. Missing-ID controls are explicitly unaccepted and not editable until existing acceptance.

The projection includes labels, ordered leading/trailing custom CV IDs, normalized authored abscissae,
authoritative binary64 SI ordinates and their exact rational representation, declared/default display units,
applicable lock kinds, authored lock values, typed assertions, normalized/physical assignments and profile
name/semantic identity. Profile identities are computed once per profile rather than once per assignment.
The SI rational describes the authoritative once-rounded binary64 value, not an invented exact pre-rounding
decimal. Nested lists are defensive read-only copies; DTO replacements cannot change session targets or history.
Derived section/point queries are separate from assignments and add no authored station.

The public consumer uses custom IDs/names/assignments, discovers the target only through public DTOs, queries
the bound certificate, begins the correct numeric rail draft and keeps ownership through unrelated inspection.
Its initial RED and missing-ID state RED are retained in `333ls0b1`; GREEN is `0yl6fbav`. Later controls prove
old accepted snapshots survive Apply, incomplete recovery diagnostics carry exact source hash/base/draft/
generation plus byte span/recovery text without a certificate, and every outward nested collection resists
mutation. Assertion projection is inspection-only; it cannot clear the unsupported geometry gate.

### Latest receipt, fingerprints and remaining work

Latest command: `python3 tools/verify-application-core.py`, assigned cwd. Logical scratch
`/tmp/cfd-application-core-20260923-z7h0f5_c`, canonical `/private/tmp/cfd-application-core-20260923-z7h0f5_c`.
Its environment.json records all six cache/temp variables under that unique root, artifacts under that root,
and DOTNET_GENERATE_ASPNET_CERTIFICATE=false. No certificate banner occurred. All scratch/native test fixtures
are retained for review; no old containment artifacts or global trust state were cleaned.

Build exit 0, zero warnings/errors, 4.439434374973644 seconds. Test exit 0, 159 PASS, 0.9770575420116074 seconds.
Build PID/start receipts include 99743/99746/99748/99765/99782/99796; test PID 99813. Both receipts end with
live={} and quiescent=true. Direct ps readback for all seven PIDs returned no rows, exit 1.
Compiler-only failures `jo4dic1p`, `jkeh055e`, `kasfnz9s` are retained separately and are not behavioral RED.
Controls without a specifically named earlier failure were first observed GREEN; no universal red-first claim.

| Tested path | SHA-256 |
|---|---|
| `src/CfdWorkbench.Persistence/ProjectStore.cs` | `613d31240b55dadba2a04c7e0017024542349e60b52d4473ebb8fd8c34254ee0` |
| `src/CfdWorkbench.Core/Contracts.cs` | `32dccc65bf06185e20faf8089e14471ca1a7b62981c36342d3b6a8a922b00eac` |
| `src/CfdWorkbench.Core/FoilSource.cs` | `36014a20a4843245464c82111c248c645e6bb69a353da3dcc28379589885b573` |
| `src/CfdWorkbench.Core/AuthoringSession.cs` | `ba3b5b1ecae5209e0da570af9764d6bad1b77eb633450e3c3246453b9b573b06` |
| `tests/CfdWorkbench.Core.Tests/AuthoringSessionTests.cs` | `072e37b0d44ec3a14480717f8fa06359caf878952ce8050909aa4ee5d513e17d` |
| `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` | `cd1e49282930cbf664ece364a8262ecb7ca204f0eebe65aa83e9bea871cf268d` |

Rulings17–19 were read from the Coordinator's canonical ruling file during this run. By explicit sequencing,
this checkpoint retains evaluator /1 and does not repair or clear the two verified scientific findings.
Next dependency is the reviewed /2 normative/examples/B0 companion commit, then a dedicated R17 identity/
compatibility sweep and R18 all-query deterministic arithmetic-feasibility proof. Root's scientific veto,
independent native-store/R16/Data review, Windows runtime/gate and full acceptance remain open. This is an
isolated progress checkpoint, never a partial B join, C release or complete implementation claim.

## Fifth increment: versioned identity and deterministic query admission

This run began at 2026-09-23T17:35:27Z on clean `2407b61`. Requested model gpt-6-astra;
effective identity and aggregate tool-call count Not recorded. The planned bound was 60 calls/35 minutes;
after context continuation the author explicitly limited remaining work to gate/proof/commit closure rather
than inventing a remaining-call measurement. No subagents or Windows executions occurred.

Owner Ruling 20's reviewed Rulings 17–19 companions were joined from Coordinator `1582d69` through the
supported conductor into isolated core `65ac0b9`. Generated index conflict used official derivation;
audit rows were retained. The first merge commit was refused by Root's review-document lease. An incorrectly
sequenced dependent conductor continuation also ran before that refusal was inspected. Both raw receipts
remain in task scratch. Work stopped; Root released the exact lease and Coordinator authorized separately
checked commit/continue operations. This is a sequencing defect, not an approved general lease bypass.
Control: never issue a dependent commit/conductor command before reading the prior exit and index state.
The conductor's generated audit success fields precede its final gate; they do not establish final success.
The retained `join-fbabb0os` run exited 8: 2/11 gates failed. The exact causes were the expected `/1`
production versus authoritative `/2` examples, and two missing LF text-write arguments/stdio encoding guard
in the leased production gate. Coordinator authorized those same-scope repairs. No `/1` fixture restoration
or gate skip occurred. Final integrated verification below closes both failures.

Ruling 17 implementation stores the original binary64 **degree** CV ordinates in `/2` semantic identity,
including profile evaluator version and session/native bindings. The evaluator still evaluates the degree
spline, multiplies by the pinned binary64 radians factor, and rounds exactly once. The collision inputs
1.791 and 1.7910000000000001 now have distinct semantic hashes. `/1` text refuses `DSL-VERSION` without
source replacement. Native unsupported evaluator bindings refuse `DOC-VERSION` before retained-source
parsing or adoption. Existing save/reopen/Undo/Redo/store controls now consume the authoritative `/2` example.

Root's authentic historical `/1` native file exposed a diagnostic-boundary defect: native source parsing
leaked `DSL-VERSION`; the first authored test had mutated only a binding and incorrectly expected
`DOC-REFERENCE`. The class is compatibility checks delegated past the container boundary. The sweep
separated source and native version checks, and the native loader now checks all design evaluators before
source parsing. Named controls assert unsupported `/1`, unknown evaluator/version, untouched input bytes,
and empty session after refusal. Root's authentic-file consumer remains separate evidence from those tests.

### All-query feasibility derivation

Admission now produces an immutable `QueryFeasibilityWitness` bound to the certificate's exact source and
surface. Its algorithm is `common-denominator-dyadic-128/taylor-grid-64/v1`. It records the actual span count,
degrees, common-denominator bit bounds, refined numerator/denominator bounds, maximum intermediate path,
32768-bit ceiling, operation bound, inverse depth 128 and angle grid 64. The certificate retains the exact
polynomial spans and exact maximum witnesses internally. Display sampling never supplies admission authority.

For each computed span let D be the LCM of the reduced denominators of all X/Y Bernstein coefficients and
M the largest absolute numerator when represented over D. Every subdivision coefficient is a convex
combination of its ancestors. At dyadic depth d its denominator divides D·2^(p·d), and the corresponding
numerator magnitude is at most M·2^(p·d). The implementation uses d=128, conservatively beyond the last
successful query depth 127. It also bounds the *uncancelled* addition and division-by-two operands of each
triangular split. Existing whole-domain derivative/hull proof establishes that every span meets the query
ordinate tolerance by depth 127. Endpoint returns and interval selection are covered by the same bounds.

Every finite binary64 query in [0,1] has an exact reduced numerator of at most 53 bits and a denominator
dividing 2^1074. The abstraction adds one-bit slack, covers construction, scans, endpoint comparisons and
cross-products against refined coefficients, and bounds tolerance comparisons. It propagates actual
expression bounds for camber, difference, maximum reciprocal, thickness, normalized section, rotation,
chord scaling, translation and mirrored span. For sizes (N,D), addition/subtraction use
(max(Na+Db,Nb+Da)+1,Da+Db); product uses (Na+Nb,Da+Db); division uses (Na+Db,Da+Nb).
Interval extrema additionally account for cross-products before comparison. Reduced outputs cannot exceed
these uncancelled bounds. Normalized section range and placed coordinate range are bounded before admission.

The once-rounded binary64 angle is enclosed outward on the dyadic grid 2^-64. This widens a query interval;
it never changes source identity, source CV values, the degree spline or the once-rounded evaluator angle.
Because the admitted angle hull lies in [-1,1], outward grid endpoints also lie there. Center and radius
denominators divide 2^65. Taylor term k has denominator dividing 2^(65k)·k!, k≤33. All sums and remainder
1/32! plus radius divide 2^(65·33)·33!, below 2270 bits; 2400-bit result bounds and 5000-bit intermediate
bounds include pre-reduction recurrence, sums and comparisons. Grid rounding shifts/divrem are bounded too.
`PlacementWidth` includes the added interval width 2/2^64; `Trigonometry` propagates its actual radius.
Tiny angles may share an enclosure containing zero, but are not silently evaluated as zero.

`DecimalSi.Round` exponent alignment and normal/subnormal numerator shifts, denominator shifts, divrem and
doubled remainder are included. Outward conversion comparisons include the full finite-binary64 rational
range (including denominators 2^1074). The witness records the largest such path, rather than estimating from
the successful samples. Arithmetic bounds above 32768 or the operation ceiling refuse before certificate
issuance with `GEOMETRY-QUERY-RESOURCE`; the expensive subnormal-knot fixture exercises this route.

The operation measure counts rational primitive arithmetic/comparisons and bounded conversion calls; it
does **not** count internal BigInteger limb instructions or GCD iterations, and is not a CPU-time estimate.
Per curve the upper bound is 8·spanCount + 128·(8p(p+1)+32(p+1)+64), covering two-coordinate triangular
splits, extrema, comparisons, interval selection and tolerances. A fixed 10000 reserve covers the remaining
straight-line query: fewer than 64 interval operators at a conservative 32 primitive units each, 16 Taylor
iterations at 16 units each, 32 factorial steps at two units each, and fewer than 32 conversions plus fixed
construction/normalization/sign/range checks. These conservative sub-bounds are below the reserve; the
32768-bit operand limit bounds the size of each underlying BigInteger/GCD input. The admitted ceiling is
one million primitive units. The dyadic fixture records 321472 units and a maximum 20502-bit bound at
`placed-X/round-shift-divrem`. This mathematical work bound is independent of OS scheduling.

Queries retain cooperative time and cancellation checks, with `GEOMETRY-BUDGET` and `GEOMETRY-CANCELLED`
distinct from deterministic proof failures. A deterministic refusal after certificate admission now reports
`GEOMETRY-CERTIFICATE-DEFECT`, not ordinary uncertainty. Tests cover both environmental outcomes and a
subsequent normal query. No hard one-second wall-time guarantee is claimed.

### Executable evidence and final frozen source

R17 initial RED `blk4_xki` and GREEN `bsbgn0ac` are retained. The initial R18 test incorrectly supplied four
CVs where the grammar requires at least six; its `DSL-PATCH` failure was fixture rejection, not tiny-angle
proof. Corrected seven-CV run `_vat6wvz` built successfully and failed for certified ±1e-300 query arithmetic
and absent feasibility witness; zero and minimum-subnormal cases passed. `iujqm0c6` first closed those
failures; `iy15m3m1` added environmental outcomes, all four side/port combinations at ten finite-binary64
domain boundary pairs, and pre-admission expensive-span refusal. Root's native diagnostic finding generated
named RED `u8xzpxk0` (wrong DOC-REFERENCE/DOC-INTEGRITY), followed by the final native guard GREEN below.
An intermediate native test used a nonempty session and correctly hit DOC-SESSION-NOT-EMPTY; it was corrected
to a fresh-session fixture before the meaningful RED. None of these fixture mistakes is claimed as proof.

Final supported `run-verify-gates.py` receipt is `/tmp/cfd-application-core-20260923-integrated-1b_1cn36`:
exit 0, all 11 gates passed, 6.687411791994236 seconds. Nested production run
`/tmp/cfd-application-core-20260923-9ty5gkbm` records build exit 0 with zero warnings/errors (4.383033499994781s),
172 PASS/test exit 0 (1.264692583004944s). Core DLL SHA-256
`86a9e74ba2c4054840df81fa5d7438ec50306d5fd21805674b43f0dcb5eaa261`.
The literal `/tmp` roots resolve through macOS `/private/tmp`; all six cache/temp variables and artifacts
are task-local, certificate generation=false before launch. No certificate banner occurred. Outer PID9030,
build PID9036 and descendants 9040/9043/9072/9103/9130, and test PID9159 have recorded start identities,
exit codes, live={} and quiescent=true. Scratch and earlier containment evidence remain preserved.
Documentation check: 103 artifacts, zero defects/index drift, 77 existing nonblocking review suggestions.

| Tested path | SHA-256 |
|---|---|
| `src/CfdWorkbench.Core/FoilSource.cs` | `c8daf453b21fbdc705e83cf6d64ec0f583ebd96a6275bc0314598f3463e38c1f` |
| `src/CfdWorkbench.Core/Geometry.cs` | `59aef82a565822c39166cd630091a506eb8e21c86183d5da1185d4a2211a8f9e` |
| `src/CfdWorkbench.Core/AuthoringSession.cs` | `2d95f0139cd807cc739815ca474ff8f9cd95930281eb086b17ec9693e6e8b405` |
| `tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs` | `560b97d5c63c4121ced586cf0d8d06728c5089e95dac6eda5333e603e3bfa469` |
| `tests/CfdWorkbench.Core.Tests/GeometryTests.cs` | `163964fb3c75f2a4b9367a73d1b4cd61b7c9b8961297de8f1ec1bddbd7c5158d` |
| `tests/CfdWorkbench.Core.Tests/AuthoringSessionTests.cs` | `79b437cdb52b96d35c6f20bb8190167ca058b3093f2731d58d84ea1fef8e1673` |
| `tools/verify-application-core.py` | `416ef8c6adacb2aed27e3150c76f7bf0c1be83d13adb2a81cf49e314cfd9180a` |

Root's bounded frozen scientific review found no mathematical blocker in the common-denominator,
intermediate/rounding, Taylor-grid and propagated-width argument. Its exact rational trig oracle enclosed
the collision cases and constant tiny-angle point. This is independent bounded evidence, not universal
sample coverage or a replacement for the derivation. Final independent frozen-DLL follow-up reports 204
assertions PASS, including authentic historical `/1` native DOC-VERSION with bytes/session preserved and
`/2` Example edit/save/reopen/Undo/Redo. The author read its retained results and verified hashes:
`/tmp/cfd-evaluator2-review.O3NdvG/receipts/tests.log` SHA-256
`7101da3d92f3ae608a3c662548cc83d58b58f9df81585f78af6c9d6fca727be0`, and
`exact-placement-final.json` SHA-256 `e1b2cf19a1ff4750aefcb944187a4e6b2fe794bea85850b3cd40982db8be99e5`.
The latter records exact rational X/Y/Z enclosure for the two distinct-angle collision inputs and constant
tiny angle; reviewer reports build/run exit 0 and three owned PID/start pairs absent. Review disposition
remains the independent reviewer's decision. Cross-platform execution/Windows store capability,
remaining full-contract review and acceptance remain open. The commit is an isolated progress checkpoint,
not a production B join, C dispatch, or complete implementation claim. Defect-class register integration
belongs to the Coordinator's ledger lease; the classes and executable controls above are handed back.
