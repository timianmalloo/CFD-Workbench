---
id: review-application-core
title: Independent review of the production application core
type: proof-pack
status: in-review
owner: "@cfd-application-20260923"
tags: [application, core, independent-review, geometry, persistence, identity]
links:
  - {to: coordination-contract-b-core, rel: documents}
  - {to: design-application-contracts, rel: depends-on}
  - {to: architecture-application, rel: depends-on}
  - {to: spec-foildsl, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: depends-on}
review-by: 2026-10-23
summary: >-
  Records independent observations against named frozen production-core checkpoints, including
  numeric and continuous-geometry oracles and a reproduced cancelled-assessment defect.
  Review remains open; these partial results do not authorize integration or application acceptance.
---

# Independent production core review

Root reviewer: `cfd-application-20260923`, 23 September 2026. The reviewer does not author production
Core or Persistence source. Data & Persistence, Computational Geometry, Test Architect, Security,
SRE and Simplifier are review lenses, not separately claimed model sessions. Technical Owner review
does not replace this independent gate.

**Disposition: OPEN — not a join clearance.** Production session and persistence work continues in
`feature/application-core`. The cancelled-assessment counterexample is repaired in the named followup;
the complete session, telemetry and persistence gate remains open. A positive result for one frozen binary does not certify later
source, the native UI, Windows runtime, a mesh, export or a simulation result.

## Scope and evidence method

The [frozen B packet](../coordination/contract-b-core.md) and
[session/native contract](../design/application-contracts.md) govern the review. The affected chain is
source bytes → parser and canonical identity → continuous geometry proof → session-owned validation
and draft → accepted/source/design/cursor facts → native envelope and store → future GUI/CLI consumers.
No second authored geometry authority is permitted at a join.

Root copies a named checkpoint's compiled assembly into a unique private directory under `/tmp`,
builds a separate consumer, and retains original source, output and PID/start-identity receipts.
All listed root build/test processes finished with an empty recorded live set. SDK 10.0.203 runs on
macOS arm64. Each scratch uses isolated .NET/NuGet/temp directories, disabled development-certificate
generation and build servers. These are observed process/path controls, not a filesystem sandbox claim.

Raw evidence directories below are retained local session artifacts. Their `review-receipt.json`
fingerprints bind the independent program, frozen DLL and output. The formulae, inputs and outcomes
below remain reviewable in this committed document if disposable files are later unavailable. Final
recurrence controls must also reside in the production tests/gates; temporary probes alone do not
satisfy that obligation.

## Observed checkpoint results

| Frozen worker checkpoint | Independent probe | Observed outcome and limit |
|---|---|---|
| `z5pb_9xy` | `/tmp/cfd-parser-review.yVciSs` | 10 of 11 direct cases passed; 1,565 source-prefix truncations and 1,000 seeded arbitrary-byte cases produced no unexpected crashes. Missing second assignment incorrectly reported `DSL-REFERENCE` instead of `DSL-SYNTAX`. No whole-language conformance claim. |
| `dp3u00hb` | Previous directory's `followup/` | Named second-assignment countercase now returned `DSL-SYNTAX`, Syntactic, with exact source retained and `IsParsed=false`. Build/test exit 0. This rerun did not repeat the whole corpus. |
| `5pqhauab` | `/tmp/cfd-geometry-review.TQVWXK` | Exact dyadic maximum, cambered same-thickness maximum, independent LE/TE bases and interior-knot contact cases matched their oracles. A fifth stress case had a reviewer-oracle error; see correction below. |
| `hvrkd70l` | `/tmp/cfd-section-review.CL5eAD` | Eight cases passed: nonlinear inverse abscissa with exact camber/normalization, closed endpoints, source/Surface binding, immutable witnesses, and four domain refusals. Build/test exit 0. |
| `hvrkd70l` | `/tmp/cfd-identity-review.MnyjUB` | 1,000 exact-rational decimal/unit cases and 4,996 finite binary64 serialization cases agreed with independent oracles. Build/test exit 0. No exhaustive, object-hash or Windows claim. |
| `lnj_sj8p` | `/tmp/cfd-placement-review.s4ZNVb` | 36 placed points / 108 coordinate enclosures passed an independent high-precision oracle. Both sides, both span halves and positive/zero/negative twist were covered. Build/test/oracle exit 0. |
| `k_j6brrg` | `/tmp/cfd-session-review.nK51bi` | Reusing a cancelled draft ID resurrected an old assessment and changed accepted history: build 0, tests 1. Thirty malformed top-level native field-type cases all returned stable `DOC-*` refusals. Empty recovery returned `DOC-SCHEMA`, initially recorded as an interpretation question rather than a failed oracle. |
| `ttbkc0t_` | Previous directory's `followup/` | The unchanged cancellation reproducer now returned `DSL-DRAFT-REUSED` without changing accepted history; all 30 malformed-type cases still passed. Empty recovery was admitted. Build/test exit 0. The raw program retains the old static checkpoint label; the receipt binds and explicitly identifies the actual repaired DLL. |
| `po5pbru9` / `a8a6351` | `/tmp/cfd-history-review.9Y6L0e` | A 120-command independent history model passed: 80 edits, 21 Undo, 19 Redo, 21 in-memory native reopens, 8 recovery resumes, 8 cancellation events, 81 accepted rows and 103 cursor facts. Eighty durable Apply retries preserved their original result without moving the cursor. No filesystem publication claim. |
| `po5pbru9` / `a8a6351` | `/tmp/cfd-twist-review.pRn02w` | Two Owner-proposed counterexamples independently confirmed: distinct evaluated twist curves share a Surface hash, and a certified constant `1e-300` degree twist cannot answer a placed-point query. Diagnostic build/run exited 0; the observed behavior is a blocking finding, not a passing conformance test. |

Receipt SHA-256 values, in the same order as the table:

```text
parser initial   c09c41561d754da4a4a7e2c61e91a5973909ad57fda438b24af4e7376b74d822
parser followup  e53f4be04806d092b6748b5484779acdff11abea9e849099dda2be026c39817a
geometry         a839886c1f9794931ec8eb496e6b1b3c5c096db7ac6c25b02c175b5a427462f8
section          86ac9be5d4aacabb1cc36564f69058b4e3b1ab73cb804990d92daf70553b56f0
identity         5f6fbee621c20fd16cd490905c156bb4a89dd3a54d8662b15fe2727a58543671
placement        bba1fe3306f0e7194f15aa7619cb6073027b0d981f6e0c73d2d37c9a1c161d1f
session initial  c90d195be66d01404fb9d088171b0ddef544bacaeb45ae312abc532a6ffa9f32
session followup 192c6d5f3bb9098419eea9d3a2280fff84660be6f92c0e4b5918a9c896f27413
history          30fefa63f45ac8a92bcb6fa1d53daabf84c85acf39e0de38ccc33f82997d5505
twist initial    edb54f6152ddef8f8f608576cd805c36eaf8863469926a2535aec4f16057c902
```

The section and numeric probes use Core DLL SHA-256
`7f507084a1f3d6fb0c1b81cfef9014d14c2b20a10fa776d24d89c8ce138e0ccf`.
The placed-point probe uses
`aaef6df42e6468b89a2f61d5656825db308bcd43bb5844b31a234861b8958371`.
Assembly identity is evidence binding, not the FoilDSL Surface identity.

## Independent oracle definitions

**Continuous maximum.** A single degree-five Bezier span has thickness ordinates
`[0, 1/16, 1/8, 1/8, 1/16, 0]`. Its derivative is
`(5/16)*(1-2t)*(1+2t-2t²)`, positive before `t=1/2` and negative afterwards.
Its exact global maximum is `25/256`. The certificate enclosed that value with equal endpoints.
Adding the same camber to both sides preserved this maximum. The independent-bases fixture used
different leading/trailing control counts; it was not a shared-rail-basis shortcut.

**Section inverse and normalization.** Profile abscissa coefficients `[0,0,0,0,0,1]` give `x(t)=t⁵`.
Upper ordinates `[0,1/16,3/32,3/32,1/16,0]` and lower ordinates
`[0,0,-1/32,-1/32,0,0]` give the same thickness polynomial above. At requested `x=1/32`,
the parameter is `t=1/2`, not `t=x`. Camber is `15/512`. With a constant full-thickness channel
`1/8`, normalized upper/lower ordinates are exactly `47/512` and `-17/512`. The probe returned
these exact enclosure endpoints. This checks inverse correspondence, camber preservation and full
versus half thickness independently of the implementation's own sampling.

**Placed coordinates.** Use that section, leading=0, trailing=`1/8 m`, dihedral=0,
half-span=`1/2 m`, and constant twist of -30, 0 or +30 degrees. Query eta 0, 1/4 and 1, both section
sides and both port/starboard halves. Form the angle using the contract's binary64
`0.017453292519943295` factor and its once-rounded product. Python Decimal precision 110 evaluates
70 sine/cosine Taylor terms from that exact binary64 angle; the comparison allowance is `1e-100`
for the decimal computation, far below the observed output widths. FoilDSL §6 then gives each point.
All 108 coordinates were enclosed, and every returned interval width was at most the certificate's
`4.4749916153017175e-14 m` uniform bound, itself below the 10 nm implementation policy. The numeric
oracles do not prove tessellation quality or replace the continuous admission argument.

**Numeric representation.** Python `random.Random(42019)` generated 1,000 decimal tokens with integer
significands in `[-10²⁴,10²⁴)`, exponent in `[-345,280)`, and scale chosen from 0,-2,-3,-4,-6.
`Fraction(token)*Fraction(10)**scale` is converted once to binary64 and compared by bits, with signed
zero normalized. Separately, a 64-bit LCG starts at 42019, uses multiplier 6364136223846793005 and
increment 1442695040888963407 modulo 2⁶⁴, and supplies 5,000 raw bit patterns. The 4,996 finite values
are serialized by Node v22.22.2 native `JSON.stringify` and compared byte-for-byte with production
`Jcs.Number`. This is a deterministic cross-runtime corpus, not a claim that random testing proves
all numbers. The author's separate published RFC 8785 vectors remain a different evidence set.

## Findings and open gates

| Finding | Evidence / consequence | Required disposition |
|---|---|---|
| Cancelled draft authority can reappear | Verified on frozen `k_j6brrg`: Begin UUID A → Validate generation 0 → Cancel A → Begin A with identical target/source → Apply old assessment succeeds. | Named veto cleared on frozen `ttbkc0t_`: root's unchanged reproducer refuses UUID reuse and preserves accepted state. Retired IDs are also reconstructed from durable edit/recovery records; broader session review remains open. |
| Native writer cap can be raised | Inferred from the initial public constructor accepting an arbitrary envelope cap while the reader has fixed 8 MB admission. | Repair inspected: constructor requires 0 < cap ≤ native maximum. Author retains RED/GREEN for this boundary; root has not separately executed that named case. |
| Empty recovery draft is refused | Verified `DOC-SCHEMA` for zero recovery chunks. Empty bytes are valid incomplete UTF-8; Coordinator and author are reconciling this within existing schema. | Permit empty recovery without accepting an empty source revision; Resume/Validate/Apply must retain accepted geometry and refuse invalid adoption. |
| Save acknowledgment is not tied to capture | Coordinator source finding: arbitrary byte-array acknowledgment can mark a session clean without the required captured-image contract. | Bind acknowledgment to captured successful publication; prove old acknowledgment after editing remains dirty and unrelated images are refused. Actual filesystem publication remains a separate store obligation. |
| Telemetry records incomplete semantics | Root source finding in repaired session candidate: generic operation events omit required phase/correlation/generation/evaluator facts, and returned Invalid/NotAssessed validation is logged OK when no exception is thrown. | Complete normal-path named events and derive validation status/code from its assessment. Prove privacy redaction, 256-event retention and discard on close; do not equate exception absence with valid geometry. |
| Placement admission and deadline limits | Source review finds the uniform inverse/normalization/trig interval propagation coherent; budget checks now precede returned results. | Record the continuous no-crossing argument, fixed-cap failure paths and distinction between uniform enclosure width and successful evaluation of every query. Cooperative polling is not a hard real-time scheduling guarantee. |
| Surface identity loses evaluated twist distinctions | Verified on frozen `a8a6351`: rounding each degree CV to radians for hashing is not equivalent to evaluating the degree spline then rounding its angle. The complete-source counterexample below has one hash and different point enclosures. | Independent identity/evaluator veto OPEN. Owner must resolve the normative §6/§8 conflict, compatibility consequences, production evaluator and conformance cases together. A small coordinate difference does not waive identity semantics. |
| Certified tiny-angle source cannot answer a point query | Verified on frozen `a8a6351`: every twist ordinate `1e-300` degrees is Certified, but `PointAt(certificate,0.203125,1,true)` throws `GEOMETRY-NOT-ASSESSED`. Source inspection identifies exact-rational Taylor arithmetic exceeding the bit budget. | Executability finding OPEN. A uniform error bound alone does not prove arithmetic feasibility. Require an explicit bounded numerical remedy or honest pre-admission refusal and regression evidence. |
| Public consumer lacks authored control projection | Verified public API inspection at `a8a6351`: source bytes and certificates do not expose arbitrary authored rail CV IDs/ordinates or section assignments. A UI would need a competing parser or guessed IDs. | Ruling 16 approves a defensive, identity-bound authored projection and accepted geometry/diagnostic seam in existing B paths, with a custom-ID consumer fixture. C remains held pending that proof. |
| Remaining end-to-end surfaces | Native OS primitive/fault/race handling, telemetry/privacy, complete replay/growth/recovery cases and final API consumer proof are unfinished. | Complete B, run required checks, independently review the final committed candidate, then consider C. No partial core join. |

For the admitted mathematical subset, the no-crossing argument must connect positive half-span
(`Y=half_span*eta`, so distinct eta have distinct Y), positive chord, separated normalized section sides,
and invertible in-plane rotation. A measured point grid cannot supply this argument. Port symmetry and
closed section boundaries must be addressed explicitly. This paragraph is a review obligation, not a
claim that every regularity or downstream meshing requirement has been proved.

### Exact twist identity counterexample

The frozen DLL is SHA-256 `d140bdf50d07a392840211aa4e4e2a31ca80458f6c47f3b63ed99ae5b6dc3251`.
Both documents use the exact section fixture described above, constant chord `1/8 m`, zero leading
and dihedral, and half-span `1/2 m`. Twist has degree 3, knots `[0,0,0,0,.5,.5,.5,1,1,1,1]`,
abscissae `[0,.125,.25,.5,.75,.875,1]`, and all ordinates zero except CV2. The two values of CV2
are decimal `1.791` and `1.7910000000000001`. Generated fixture text includes required leading zeros
and deterministic IDs. Both satisfy parsing and continuous subset admission.

Both Surface hashes are `605e426e29c1a9ac6d17e4620889a90ca9f9ab6842405c51be9840ca80af8604`.
Their source hashes differ. At eta `0.203125`, normalized x `1`, upper starboard, the Z enclosures are
`[-0.0014652248927230652,-0.001465224892723065]` and
`[-0.0014652248927230654,-0.0014652248927230652]`. The CV2 Bernstein weight is exactly `3/8`.
The rounded CV radian values coincide, while rounding after the weighted degree evaluation gives
`0.011722067588706916` versus `0.011722067588706917`. This is an ordering discrepancy between
canonicalization and evaluation, not a cryptographic collision. The returned Z intervals touch at
one endpoint; their difference alone does not prove distinct exact coordinates. A separate Python
`Fraction` oracle starts from each exact binary64 CV, multiplies by `3/8` and the exact binary64 degree
factor, then rounds once. It confirms the distinct angle bits. Both angles lie in `[0,1]`, where sine
is strictly increasing, and the closed profile endpoint gives `Z=-sin(angle)/8`, proving distinct
exact coordinates independently of the output intervals. Full original/materialized sources,
program and raw process receipts remain in the named probe directory.
Supplemental oracle SHA-256: program
`be71d0694f9c28da48c321a743dbd0fb95db88218acb1a2264d1412f6e0eb7c8`, output
`614930e2704084b7966b03d97bbe31b01c4a260257a3b14204d62b7a4b41b5e6`.

## Reviewer corrections and remaining proof limits

The initial near-zero-knot stress oracle incorrectly demanded Not assessed merely because a valid knot
was `5e-324`. Its failure was a **reviewer error, not a product failure**. The original result is retained.
The corrected stress-only followup accepted either a properly bound finite certificate or an honest
Not assessed result; it passed with certification in 236.6546 ms. One measured call establishes no
universal one-second deadline. Coordinator records the oracle correction class separately from product
defects. Root also stopped an optional Owner review when it would exceed the recorded three-active-seat
cap; the Coordinator added a preactivation seat-count control.

Current CUA inventory exposes no browser provider or running spike app. Earlier native capture failed
with `cgWindowNotFound`. Production rendered/keyboard/accessibility proof remains unassessed; historical
architecture-spike observations cannot satisfy it. Windows runtime and actual native store behavior
are also unassessed. Measured model token use and cost are **Not recorded**.

The root `implement` review marker starts at 15:56:16Z and measures the review checkpoint run only;
earlier parser-review preparation is not retroactively included. Its checkpoint audit records a
partial outcome. A final verdict will follow complete B evidence. This artifact deliberately remains open.
