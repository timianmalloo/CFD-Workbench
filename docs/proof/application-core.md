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
  Placed preview, sessions, native persistence and telemetry remain incomplete or unimplemented.
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
No acceptance/session or file-store boundary can be invoked.
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

## Open obligations before handback

This is an isolated checkpoint, not a partial join. Remaining work includes full language boundary/fuzz and
phase-order breadth, complete independent identity corpus, adversarial continuous rational
geometry and bounded placed-preview error, session transaction/certificate ownership, immutable
native history and replay, growth/recovery admission, actual native primitive/fault/race proof, normal-path
256-event telemetry and redaction, architecture checks, cross-platform evidence and independent veto review.
The native persistence project is currently only a project-reference boundary, with no file operations.
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
