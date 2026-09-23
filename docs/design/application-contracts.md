---
id: design-application-contracts
title: Native M1 session and project contracts
type: design
status: in-review
owner: "@cfd-owner-20260923"
phase: design
tags: [application, contracts, source, history, identity]
links:
  - {to: design-application-foundation, rel: refines}
  - {to: architecture-application, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: adr-application-project-contract, rel: depends-on}
  - {to: proof-application-contracts, rel: tested-by}
review-by: 2026-12-23
summary: >-
  Defines the complete serial M1 session, source patch, native-v1 history, identity and persistence seams.
  Executable contract fixtures establish bounded behavior without certifying geometry or claiming a native store.
  Owner and independent review retain the production gate.
review-suggested:
  - { by: architecture-application, on: 2026-09-23, reason: "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open." }
  - { by: adr-application-project-contract, on: 2026-09-23, reason: "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open." }
  - { by: spec-foildsl, on: 2026-09-23, reason: "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax." }
---

# Native M1 contract completion

**Status: in review.** Rulings 8–10 authorize serial design and contract fixtures only. This is not a
production parser, geometry kernel, native store, UI or delivered M1. The [proof](../proof/application-contracts.md)
names the executed behaviors and omissions. Root reviews the author independently; the author clears no veto.

## Goal, frame and execution graph

Goal: make the approved offline rail transaction implementable without downstream teams guessing shared
contracts. Done when exact typed seams, immutable history/replay, source/identity and failure fixtures are
reviewable and independently accepted. Not in scope: production B/C, wider source editing, solver/export,
geometry certification, Windows runtime or UI changes. Tier T2/T3; one serial author; no subagents.

Grounding follows `design-application-foundation → architecture-application → spec-foildsl` and
`architecture-application → proof-application-spikes`; root's `review-application-architecture` supplies
AR-01/02/05/09/10/12. FoilDSL §3 requires exact rational unit conversion; §5.10 requires IDs before acceptance;
§7 forbids acceptance on an unresolved proof; §8 separates exact source and semantic identity. Rulings 9/10
complete previously proposed format and resource policy, without changing those language obligations.

The frame compares an immutable snapshot envelope against a new database/event transport. The existing
ADR 0003 envelope wins: no new datastore, service bus or IPC is justified. The new edit receipt is necessary
because the earlier schema could not reconstruct the promised target-sensitive retry identity after reopen.
Stock: immutable source/design/accepted rows and cursor facts. Flow: session commands and captured save images.
Feedback: generations invalidate stale assessment. Delays: validation and file I/O. Unknowns: continuous
kernel proof and each OS handle/race contract, explicitly retained as later gates.

Execution graph: model/seam completion → fixture and independent oracle → schema/identity/history
disconfirmation → design/proof/security rollups → independent review → commit. Identity and parser checks share
one executable build; no separate agent fan-out. The one-second production proof budget returns Not assessed;
the author window is 50 minutes/70 tool calls, a checkpoint trigger rather than a success condition.

## Domain, grain and durable representation

Bounded context: Shape authoring. Aggregate root: one document session. Its invariant is that accepted
source, Design identity and Surface identity change together only through a matching accepted transaction.
Project history is the durable representation of that invariant. All references are immutable IDs, never
mutable geometry objects. [ADR 0004](../adr/0004-application-project-contract.md) records the format completion.

| Record / grain | Fields, writer and reader | History / additivity |
|---|---|---|
| Source: one distinct exact UTF-8 byte sequence | SHA-256 `id`, base64 chunks; session commit writes, parser/reopen reads | Immutable; byte count additive across distinct blobs, not revisions |
| Design: one semantic authoring revision | UUID `id`, prior Design `parent`, Surface hash and evaluator; commit writes, freshness/history reads | Immutable; Surface identity non-additive |
| Accepted: one accepted numeric rail or initial Open transaction | UUID, parent accepted ID, source/design references, operation ID, edit receipt; session writes, replay/retry reads | Append-only; transaction count additive |
| Cursor: one persisted selection-changing command | sequence, target, reason, operation ID; session writes, replay reads | Append-only; sequence is ordering, not a measure |
| Recovery: at most one offered incomplete draft | draft/base/generation/rail/vertex/source chunks; recovery capture writes, explicit resume reads | Replaceable Type-1, intentionally loses older recovery drafts only after complete replacement |
| Preview/view | source/base/generation/evaluator and derived geometry | Rebuildable cache; no durable editable AST; geometry measures never summed over revisions |

Project ID and format are written once at creation, read by the store and session loader. The last cursor
determines active accepted state; no competing active pointer exists. All geometry attributes are immutable
Type-2 meanings. Camera/display units are outside the envelope and identity. No timestamp controls replay.
No profile asset reader is included in this narrow fixture; valid asset/section/assertion/unsupported lock
syntax is refused as unsupported without silently interpreting it. Wider parser conformance remains required.

## Change surfaces and ownership

`native bytes → immutable rows → session-owned draft → assessment binding → immutable View/Diagnostic →
GUI and CLI adapters → labelled accepted/preview UI → geometry/analysis readers` is the required E7 surface list.
Source is the authority at every step. Store writes bytes only; it cannot edit geometry. GUI/CLI cannot create
certificates or replace draft targets. Analysis remains **Unavailable — no method implemented**.

The compiling types are in [Program.cs](../../tools/spikes/ApplicationContracts/Program.cs), namespace
`ApplicationContracts`, an executable fixture namespace, not a production assembly API. Records and interfaces
form the reviewed seam vocabulary; immutable ownership is the contract, not the mutable-array convenience
inside the fixture. Every outward source/chunk/draft/recovery array is copied. A consumer mutation cannot
change session state. The production Core must have no Avalonia, filesystem, network or process dependency.

Coordinator leases exact production paths after this gate. Core owns parser/identity/session/certificate
construction. Persistence owns OS handles, claims and exact disk token. Desktop/CLI own input/focus/rendering
only. Changing a shared record requires a typed seam request and Owner ruling before another writer proceeds.
The source-only metadata exceptions are the official docs index and audit outputs; this author owns no
architecture/specification/AGENTS/plan/product source path.

## Complete session port

`IAuthoringSession` exposes all rows below; `IProjectStore` consumes `SaveRequest` and emits `SaveResult`.
Core command mutation is serialized under one session lock. Validate captures immutable draft state under
the lock and evaluates outside it; Apply compares the complete binding under the lock. No await is held
inside the session write lock. One in-flight validation per session is the production scheduler rule.

| Port | Input and success | Failure / mutation rule |
|---|---|---|
| Open | bounded source bytes, UUID operation ID, explicit acceptance of ID-insertion diff | Missing IDs first return candidate bytes with no accepted state; actual acceptance requires exact authority binding. Original import bytes remain caller-owned and unmodified |
| BeginRailEdit | new draft UUID, `leading` or `trailing`, existing curve-local vertex ID | One owned draft; competing begin including same-ID retarget refuses. Inspection selection is not a write |
| UpdateDraft | draft UUID, exact expected generation, finite SI binary64 ordinate | Patch only owned token, verify reparse; generation increments once. Stale update, wrong target or unsafe patch keeps prior draft |
| Validate | exact draft UUID/generation, cancellation token | Returns immutable assessment with key; invalid, unsupported, cancelled and inconclusive cannot Apply |
| Apply | UUID operation ID and authority-produced assessment | Key and opaque certificate match exact source SHA/base/draft/generation/evaluator/Surface hash/rail/vertex. Preflight before any append; no partial commit |
| Cancel | exact draft UUID | Clears draft/recovery only; accepted bytes/history unchanged |
| Undo / Redo | fresh UUID | Refuse while draft owned; revalidate target geometry before adoption; append cursor only for a real move |
| Snapshot | no input | Defensive immutable view, accepted identity/bytes, draft/recovery labels and dirty state; no mutable authority escapes |
| CaptureRecovery | current draft | Exact base/target/generation and possibly incomplete UTF-8; size/line preflight before replacing prior recovery |
| ResumeRecovery | offered recovery | Only with no draft and matching current base; otherwise compare/discard or explicitly return to that base, never silently rebase |
| DiscardRecovery | explicit action with no active draft | Deletes recovery slot, never accepted history |
| SaveImage | no input | Capture one bounded serialized immutable image; no disk work in Core |
| AcknowledgeSaved | exactly the captured successfully published image | Compare current normalized image to captured image; an intervening edit remains dirty |
| Reopen | native bytes, empty session | Strict schema/reference/replay/hash checks, fresh exact current authority binding, then adopt atomically; recovery remains an offer |

The fixture calls positive certificates **Fixture admitted**, never “Valid foil”. Its `FixtureAuthority`
allowlists exact semantic hashes solely to test the boundary. Unknown geometry returns Not assessed. The
production authority must assemble the approved continuous conservative certificate; samples cannot replace it.
The certificate constructor is internal to the authority assembly; a public DTO hash is never sufficient.

Same operation ID and same persisted payload returns the original result with no new fact **and no cursor
movement**, even if the user subsequently moved elsewhere. Different action/base/source/draft/generation/rail/
vertex/Surface/evaluator under that ID gives `DOC-OPERATION-CONFLICT`. Initial Open identity is the explicitly
accepted materialized candidate SHA, not the pre-diff original. A receipt persists permanently with its facts.
Root Undo and empty Redo append nothing; their duplicate suppression is session-only and expires at Reopen.
Callers issue fresh IDs after Reopen and cannot infer durable exactly-once semantics for these no-ops.

Generic source editing is outside this numeric-rail interface. A rail command that preserves Surface meaning
still has a complete edit receipt and accepted source history; a future generic source edit needs its own
discriminated receipt, not a null receipt or a hidden bypass.

## Native-v1 exact schema and replay

No earlier format is released. Format string is `cfdw-project-1`; the seven required top-level keys are
`format`, `projectId`, `sources`, `designs`, `accepted`, `cursors`, `recovery`. The typed records enforce:

```text
SourceRow   = { id: SHA256, utf8Base64Chunks: string[] }
DesignRow   = { id: UUID, parent: UUID|null, surfaceHash: BLAKE3, evaluator: "cfdw-cv/1" }
AcceptedRow = { id: UUID, parent: UUID|null, sourceId: SHA256, designId: UUID,
                operationId: UUID, edit: EditReceipt|null }
EditReceipt = { draftId: UUID, generation: safeInteger, rail: "leading"|"trailing", vertexId: string }
CursorRow   = { sequence: safeInteger, target: UUID, reason: "open"|"apply"|"undo"|"redo", operationId: UUID }
RecoveryRow = { draftId: UUID, baseAcceptedId: UUID, generation: safeInteger,
                rail: "leading"|"trailing", vertexId: string, utf8Base64Chunks: string[] }
```

UUID spelling is lowercase `8-4-4-4-12` hexadecimal; no guessed UUID version restriction. Hashes are exactly
64 lowercase hex characters. Safe integers are [0, 9007199254740991], no fraction. Vertex IDs are nonempty,
at most 4096 Unicode scalars, case-sensitive and curve-local. Parent IDs refer strictly backward in their own
array; only the first row has null parent. Source/design/accepted IDs are unique within their record kind.
Every source/design is referenced by an accepted row. Every accepted source reparses with explicit IDs,
the pinned evaluator and the exact referenced Surface hash. SHA is recomputed from decoded bytes. No cache
is trusted. A receipt's rail/vertex resolves in both parent/base and resulting source. Null edit is legal only
on the initial Open. Source, Design and Surface identity are separate; a same-Surface Apply reuses its parent
Design ID. A changed Surface creates a new Design whose parent is the actual base Design, including branches.

Arrays are ordered facts, not a bag. Accepted rows are consumed in order by their linked Open/Apply cursor.
Exactly one Open cursor is first, paired with the root accepted operation ID. Every subsequent Apply cursor
pairs exactly one next accepted row with matching operation ID and parent equal to replay current. No second
accepted row or cursor reuses that operation ID. Undo target must be current's parent and pushes current on
redo. Redo must pop the exact most recently pushed target, whose parent equals current. Apply clears redo
but retains all history. Missing target, out-of-order parent, cycle, duplicate sequence/ID, fabricated Redo or
unconsumed accepted row rejects the entire envelope. No arbitrary child or timestamp chooses redo.

Strict JSON, UTF-8 without native BOM, duplicate keys/comments/trailing commas/nonfinite numbers rejected.
Every listed key is required, including nullable keys. Unknown format gives `DOC-VERSION`; unknown field
gives read-only `DOC-UNSUPPORTED-FIELD`, retaining original bytes outside the typed session and refusing save.
Unknown optional extension fields are not silently dropped: M1 has **no writable extension bag**.
JSON whitespace/key order is not canonical native identity. Dirty state compares normalized session images;
the store's conflict token remains SHA-256 of exact original disk bytes. These two tokens are not interchangeable.

Base64 uses standard alphabet/padding, no whitespace, canonical re-encoding equality; chunks ≤3072 characters,
all nonfinal chunks represent complete byte triples. Writer chunks at 2304 source bytes. Source BOM/CRLF/Unicode
and all untouched trivia survive decoded source hashing and save/reopen. Native line limit is 4096 encoded
UTF-8 bytes excluding CR/LF; pretty writer lines and base64 chunks satisfy it. A long escaped receipt ID can
violate it despite legal FoilDSL; writer preflight refuses **before** mutating history/recovery. This is a
native resource limitation, not a declaration that the source language is invalid.

## Source parser, patch and built-in Example

Executable parser scope is the complete ordered **inline foil subset** needed by the synthetic Example:
five independent channels, inline profiles, assignments, optional tip/closure/provenance/IDs and root-mirror
lock declarations. It tokenizes comments/JSON strings and preserves UTF-16 string indexes for lossless
splicing; these are not diagnostic byte offsets. Original UTF-8 bytes remain retained; diagnostic locations
use a separate UTF-8 byte-to-scalar mapper.
It checks degree/count/knot/order/reference constraints; parse success is not geometric assessment. Standalone
sections, assets, other lock kinds and assertions are explicit unsupported boundaries, not silently ignored.
Malformed supported syntax rejects; the fixture does not establish complete FoilDSL grammar conformance.
Production parsing must honor lexical → syntactic → version → structural → references → geometry/locks →
assertions phase order over the whole grammar. No secondary phase error may masquerade as the first cause.

For each curve without `ids`, the candidate inserts exactly `cv-0` through `cv-(N-1)` at that curve's closing
brace, in point order. A missing list means **all** IDs are absent, so these are collision-free within that
curve; existing explicit lists remain byte-identical and must already be unique. Curves may reuse the strings
because identity is curve-local. IDs are never regenerated after acceptance. Freeze/bounds with omitted IDs
remain forbidden; the narrow fixture refuses these unsupported lock productions before acceptance.

Open first computes this candidate and exposes its complete diff. Explicit acceptance then parses/certifies
that exact candidate; no IDs are inserted after acceptance. The built-in `Program.Example` is embedded source,
not a network/catalog dependency; it enters the same path. It has seven six-point curves, one manufactured
inline profile, root/tip assignments, zero twist/dihedral and independent leading/trailing rails. It makes no
hydrodynamic or manufacturability claim. Its positive session admission uses the fixture authority only.

Numeric patch: locate the accepted owned rail/CV ordinate token; derive its exact binary64 rational value,
invert the decimal unit scale exactly, emit the resulting terminating decimal, splice only that token, then
reparse and compare intended SI bits. No display rounding. Other rail's complete bytes remain unchanged.
No safe patch means `DSL-PATCH`, retained draft and explicit replacement diff in the production adapter.
Source export/formatting is a separate future command, never an implicit open/edit side effect.

## Exact numbers and Surface identity

Numeric token ceiling is 4096 UTF-8 bytes. Lexically scan sign/significand/exponent without BigInteger or
powers. All-zero significands return semantic +0 even for `-0e1000000000`. Strip leading zeros, trailing
significand zeros and decimal point; track its position. Strip exponent zero padding before bounded exponent
scan. A nonzero token ≤4096 bytes cannot compensate an exponent magnitude ≥10000, so reject such exponents
before allocating powers. Compute normalized decimal magnitude `digits.length-1 + exponent-fractionDigits`
(adjusting stripped trailing zeros); supported magnitude is [-400,400] **before unit scaling**.
Compensated spellings such as a 402-digit integer times 10^-401 and a tiny decimal times 10^401 remain one.
Outside this work domain: `DSL-LIMIT`, unsupported resource admission, source retained—not invalid grammar.

Within it, construct the bounded exact integer ratio; apply m=10^0, cm=10^-2, mm=10^-3 exactly; then round
once to binary64 nearest/ties-even using integer quotient/remainder. Normal and subnormal paths are distinct;
overflow/nonfinite is a normative lexical error, underflow rounds to semantic +0 and structural positivity
is checked afterward. `14.049 mm`, `1.4049 cm`, `0.014049 m` all produce bits `3f8cc5b8dc55000d`.
The exact halfway value above one rounds to even one; `5e-324` yields the minimum subnormal.

Semantic input is exactly FoilDSL §8: no names/comments/IDs/locks/provenance/assertions, all curves and ordered
profiles/assignments, evaluator/defaults/frame/symmetry, SI half span/ordinates; twist uses one rounded multiply
by pinned `0.017453292519943295`. Canonical object order is UTF-16 ordinal per RFC 8785; arrays retain order;
strings use minimal JSON escapes without Unicode normalization; invalid surrogate input refuses. Numbers
normalize negative zero and use ECMAScript shortest-roundtrip spelling with fixed range [1e-6,1e21).
The fixture normalizes .NET round-trip digits/exponents and cross-checks a deterministic raw-bit corpus against
an independent RFC 8785 library. This evidence is bounded, not a proof for all 2^64 bit patterns. BLAKE3 2.2.1
hashes canonical UTF-8 and matches Python 1.0.8 and the official empty vector. Source SHA-256 is independent.

## Growth, retention and recovery

Native encoded cap: 8,000,000 bytes; each decoded source/recovery: 1,048,576 bytes. Before Open/Apply/Undo/Redo
or recovery replacement, serialize the prospective exact image and check cap **and every reader line limit**.
The session fixture supports a smaller injected cap to prove boundary refusal cheaply; production cap is fixed.
Only after success may facts append. Refusal retains accepted state, complete prior recovery, draft/generation,
dirty state and last complete file. Save As of identical content does not solve overflow. No silent pruning,
history compaction, dropped branches, source rewriting or invented migration is permitted.

User copy: **Project history limit reached. Your draft is retained but cannot be committed or saved in this
format.** Recovery overflow: **Draft recovery is not saved; the previous complete recovery remains available.**
Any future archive/export/new-document operation needs its own explicit loss-of-history contract; none is added
here. Full history remains until the user deletes the project. Recovery remains until explicit resume/cancel/
discard or a successfully committed replacement. Recent-file paths are session-only in M1, no durable MRU list.
No independent source logs or automatic backup retention policy silently retains another copy.

## Persistence port and platform policy

`SaveRequest(Image, ExpectedDiskSha256, OperationId)` captures a defensive immutable snapshot at construction
and exposes only copies. `CreateOnly` is derived from a null expected disk hash, never independently set.
Expected null means target absent; non-null means exact original on-disk SHA. `SaveResult` separates publication
known from durability confirmed. I/O cancellation before publication returns cancelled with original intact;
after publication starts, resolve or return `DOC-SAVE-UNCERTAIN`, never falsely “not saved”. Reopen/compare before
retrying uncertain publication. The model oracle injects fault stages; it is not an OS implementation.

| Platform boundary | Required production adapter contract | Current proof / gate |
|---|---|---|
| macOS path resolution | Open a trusted selected directory; traverse each component handle-relative with no-follow, verify device/inode identities, reject symlink ancestors/target and nonregular target; retain parent handle through publication | Not assessed in native adapter; path-string `lstat` then open is insufficient |
| Windows path resolution | Open directory/file handles with reparse-point rejection for every component; verify volume/file identity and reject junction/symlink/reparse traversal; retain handles with documented sharing flags across publication | Not assessed; Windows cross-build does not test reparse or sharing behavior |
| New file, both OSes | Same-directory exclusive owned temp, complete write/flush, atomic **no-replace** publish against selected parent identity; competing creator must survive | Actual local hard-link no-replace primitive tested; full handle-relative adapter absent |
| Existing file, both OSes | Exclusive cooperative writer claim; verify expected exact hash and target/parent identity using held handles; atomic replace only under verified platform guarantees; release only owned claim/temp | Model fault/conflict cases only; native overwrite remains disabled until real race/fault suite |
| Missing safe primitive | `DOC-UNSUPPORTED-PERSISTENCE`; retain original/session state; no fallback to absent-check/rename/replace or path-only guard | Executable model case |
| Crash after publication | Report uncertainty; complete old or complete new image, never mixed; flush file and directory where supported and report actual durability | Model old/new check; no power-loss claim |

This specifies required capability semantics rather than guessing unspiked native API flags. macOS and Windows
implementation must first spike exact SDK/system signatures and show native fault/race evidence, including
ancestor replacement, reparse/junction traversal, open reader handles, temp/claim collision, disk-full/short
write, first-create collision, cancellation and after-publish uncertainty. Any inability to establish the
capability leaves that operation disabled. Same-user malicious processes and arbitrary noncooperating writers
are outside the cooperative guarantee; hash-check-then-replace is **not CAS**. Owner accepted the cooperative
and OS-user ACL posture, not a claim that uncooperative overwrite or race safety is solved.

## Diagnostics, telemetry and adapter obligations

`Diagnostic` carries stable code, phase, severity, UTF-8 byte start/length, one-based line/scalar column,
affected entity, plain reason and recovery action. Ranges are half-open, boundaries must align to UTF-8 scalars;
CRLF is one newline. BOM is retained and counted by the fixture location mapper; a display may hide its glyph
but cannot change byte offsets. Location oracle checks multibyte and non-BMP input. Parser exceptions in the
fixture carry code only; full phase/range aggregation is a production obligation, not falsely claimed here.

Codes: `DSL-LEX/SYNTAX/VERSION/UNIT/CURVE/REFERENCE/GEOMETRY/LOCK/ASSERT`, unsupported `DSL-UNSUPPORTED`,
resource `DSL-LIMIT`, ownership `DSL-DRAFT-OWNED`, stale binding `DSL-CONFLICT`, `DSL-PATCH`, cancelled
`DSL-CANCELLED`, inconclusive `DSL-NOT-ASSESSED`; native `DOC-SCHEMA/VERSION/UNSUPPORTED-FIELD/REFERENCE/
INTEGRITY/ID/SIZE/OPERATION-CONFLICT/RECOVERY-BASE`; persistence `DOC-CONFLICT/IO/WRITER-CLAIM/
UNSUPPORTED-PERSISTENCE/SAVE-UNCERTAIN`. Display literal text; no HTML/eval/auto-link execution.

Normal-path events/spans: `language.parse`, `identity.canonicalize`, `geometry.validate`, `document.open`,
`document.apply`, `document.cursor`, `document.recovery`, `document.save`, `document.reopen`. Record trace/operation
ID, action, status/code, input/output byte counts, generation, evaluator and measured duration. Histograms:
operation latency/bytes; counters: failures/conflicts/resource refusal/cancellation; gauge: retained source
and history count. No source/paths/names/vertex strings/raw exception text/hash correlation identifiers in logs.
Local-only session ring capped at 256 metadata events, discarded at close; no exporter or disk event archive.
Missing timing says Not recorded. Actual telemetry redaction remains a product test: inject identifying markers
through names/paths/recovery and scan every emitted sink. No HTTP boundary: RFC 9457 N/A, not omitted silently.

UI remains foundation/`DESIGN.md` ParametricWorkbench, no new screen or tokens. Accepted and preview labels
remain distinct; inspection cannot retarget draft. Empty/loading/error/unsupported/cancel/conflict/recovery
states use foundation copy plus limits above. Native AX/UIA peer/keyboard tests, macOS VoiceOver and Windows
Narrator are release gates; this console contract artifact is not rendered native workflow evidence.

## Failure modes and verification plan

| Failure from design choice | Disposition | Falsifier / required evidence |
|---|---|---|
| Invalid UTF-8, duplicate/unknown schema, damaged hash/reference | Prevent adoption; original retained | Strict parser/native negative corpus |
| Hostile decimal exponent or token length | Bound scan before powers; unsupported resource result | Huge ±exponent subprocess deadline, compensated/zero/boundary positives |
| Exact unit patch loses a bit or modifies other rail | Refuse patch; preserve draft | 14.049 equivalence, independent Fraction oracle, byte-identical other rail |
| Late validation, forged hash/target or borrowed mutable array | Refuse stale write; defensive ownership | Binding mismatches, target retarget and external array mutation |
| Reopen trusts hash as certificate | Fresh binding before adoption or Undo/Redo | Empty/unrelated fixture authority refusal |
| Retry after reopen loses operation identity | Immutable edit receipt + replay registry | Same result/no movement; changed draft/target/kind refuses |
| Branch redo chooses wrong child | Stack reconstructed only from facts | Undo/reopen/redo, apply clears redo but keeps branches |
| Growth/long JSON ID leaves unreadable image | Preflight exact prospective writer output | Small-cap and escaped 4096-scalar ID refusal, positive line/readback |
| Late save acknowledgement marks newer data clean | Compare captured normalized image, separate disk token | Interleaved capture/edit/ack and whitespace-reopen case |
| Missing native atomic/no-follow capability | Disable operation | Model unsupported result; real platform capability tests required |
| Temp/claim ownership, fault or racing creator | Only owned cleanup; old/complete-new, explicit uncertainty | Model fault stages; actual local no-replace creator; full native suite outstanding |

Testing union: D0 deterministic isolated cases; D1 meaningful assertions and explicit copy/guard-removal
mutants; D2 deterministic 2505-case bits/rational cross-runtime corpus plus boundary laws; D3 UI-free standalone
project with only Blake3 dependency (production architecture test required when assemblies exist); D4 actual
local no-replace primitive separately from models; D5-provider complete compiling session/store ports;
D6 strict typed schema/realistic synthetic roundtrip and tamper cases; D7 fixture authority explicitly pairs
with identity/binding assertions and has no geometry fidelity claim. D5-consumer and A1–A6 N/A: no service,
MCP/model/prompt boundary. Full parser fuzzing, all-number canonicalizer proof, native store races and production
geometry/UI are unverified product obligations, not waived by the spike. Independent review owns gate status.

## Adversarial analysis (STRIDE-lite)

| Boundary | Threat | Disposition / negative test |
|---|---|---|
| Source/native input | S/T: forged hashes/evaluator/receipt; R: unauthenticated author claims | Mitigate recomputation and exact binding/replay; operation IDs are not personal attribution; tampered receipt/hash cases |
| Source/native input | D: exponent allocation, oversized history/escaped IDs | Mitigate preallocation scans and atomic size/line preflight; hostile-exponent and growth boundary cases |
| Consumer → session | S/T/E: certificate/target substitution or mutated returned payload | Mitigate opaque authority, session-owned draft and defensive copies; wrong-binding/retarget/mutation cases |
| Session → filesystem | T/E/D: ancestor/reparse race, colliding creator/temp/claim | Mitigate capability-gated native adapters and owned cleanup; model and local primitive checks only, native gate remains |
| Display/telemetry | I/R: source/path/name disclosure or missing outcome | Mitigate literal display and bounded metadata events; production marker-scan and outcome tests required |

## Privacy analysis (LINDDUN-lite)

| Flow / categories | Finding | Disposition | Verification |
|---|---|---|---|
| Source, IDs, path · linkability/identifiability/disclosure | Retained personal text can leak through events | Mitigate no raw text/hash/path telemetry, local-only 256-event ring; no MRU persistence | Product sink-marker test remains required |
| History/operation receipts · non-repudiation/detectability | Local record may be mistaken for authenticated actor proof | Mitigate explicit operation-only identity; no author identity | Typed schema has no actor field |
| Full snapshots/recovery · unawareness | Old comments and failed edits remain | Mitigate explicit retained-history/recovery disclosure and discard; no silent pruning | Recovery offer/cancel fixtures; native UI proof outstanding |
| Local file retention · non-compliance | No server rights/deletion guarantee | Transfer OS-user ACL and user-controlled deletion; shared-device residual | No external data/account flows; native ACL proof remains |

## Confidence and design-slice exit record

Verified: the [proof](../proof/application-contracts.md) records observed build/assertion/oracle results only.
Inferred: this bounded contract removes interface guessing for the declared numeric-rail slice. Flagged:
full language/certificate/native runtime/telemetry/accessibility/distribution are not implemented or certified.
Patterns: Ports/Adapters to isolate filesystem/UI; immutable snapshots and Command/Memento history to preserve
source; one owned transaction for edits; idempotent action receipt for retry. Each earns its boundary; no ORM,
bus or extra service dependency. `simplify:` one session lock and bounded full-image preflight until measured
8 MB cost fails the UI budget; then redesign under an ADR rather than weakening atomic admission.

Design-slice checklist: responsibility/model/grain/history/surface/phase/contracts/patterns/ladder/failure/
STRIDE/LINDDUN/telemetry/testing are specified and trace to fixtures or explicit product gates. Existing UI
language/craft obligations are referenced, no new UI authored. Security/privacy rollups link this design.
Hard-veto resolution is **pending independent root/Owner**, not an author pass. No new migration is performed;
unshipped v1 schema is frozen only after that review. Future migrations preserve originals and require explicit
versioned readers, non-guessing conversion and tested rollback.

| | |
|---|---|
| Completed | Serial typed design and executable contract candidate; exact evidence in proof |
| Remaining | Independent acceptance; production parser/kernel/store/adapters and both native runtime matrices |
| Best next action | Root/Owner review this candidate and rule the serial contract gate before production dispatch |
