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
  Includes placed enclosures and session/history candidates; the native adapter and full platform proof remain open.
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
Every verification node consumes a prior failing behavior or a named fault/guard-removal control.
The loop variant is the remaining failed contract assertions; a budget cap triggers a report and replan.

The required surface chain is native bytes → immutable rows → owned draft → certificate binding →
immutable projection → future GUI/CLI → geometry reader. The current increment reaches source bytes → static
parse projection → semantic identity → continuous subset proof → enclosed normalized section ordinate.
The third increment adds candidate session/history and preview boundaries. The file-store adapter remains absent.
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
