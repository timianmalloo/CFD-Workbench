---
id: design-application-foundation
title: Offline accepted-source workbench slice
type: design
status: proposed
owner: "@cfd-owner-20260923"
phase: design
tags: [application, source, geometry, persistence, native]
links:
  - {to: architecture-application, rel: implements}
  - {to: adr-application-stack, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: proof-application-spikes, rel: tested-by}
review-by: 2026-12-23
summary: >-
  Designs the first native GUI/CLI vertical slice around lossless accepted source, one owned rail draft,
  certified conservative geometry and append-only save/recovery. Defines compiling port vocabulary,
  failure/security/privacy tests and exact downstream ownership proposals without production implementation.
review-suggested:
  - { by: architecture-application, on: 2026-09-23, reason: "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open." }
  - { by: adr-application-stack, on: 2026-09-23, reason: "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates." }
  - { by: spec-foildsl, on: 2026-09-23, reason: "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax." }
---

# Offline accepted-source slice

**Responsibility:** let one person open, inspect, reversibly change and safely save a foil offline, with one
accepted geometry meaning shared by GUI and CLI. Architecture §2 defines the domain, grain and invariant.
The slice realizes M1 only. Existing source/spec/mockup artifacts establish behavior; they are not core code.

## Contracts and complete surface trace

`project source snapshot → lossless parser/typed definition → owned session command → immutable validation
and preview DTO → native/CLI adapter → accepted/draft labels and geometry → Analysis Unavailable`.
The source is the durable writer. Kernel readers derive channels/sections and measures. Store reads/writes
envelope bytes but cannot edit geometry. Viewport consumes only a derived projection keyed to source/base.
Every load-bearing persisted field has a single writer/read path:

| Field / grain | Writer | Reader | History |
|---|---|---|---|
| Exact source bytes + SHA-256 | Accepted source transaction | Parser/reopen/diagnostic mapping | Immutable snapshot |
| Revision ID, parent, definition hash | Apply after matching certificate | History/CLI/future run pin | Append-only |
| Cursor fact target | Undo/Redo/select | Session current revision | Append-only; branching never deletes facts |
| Recovery source, draft ID/base/target/generation | Recovery writer | Explicit reopen recovery offer | Replaceable recovery snapshot; never accepted |
| Format/project ID | New project | Store | Immutable within file version |
| UI preference/camera | Desktop preferences | Desktop only | Replaceable; excluded from identity |

The compiling public vocabulary is in the assigned [native spike](../../tools/spikes/ApplicationNativeUi/Program.cs):
SourceSnapshot, AcceptedRevision, OwnedDraft, DraftTarget (Leading/Trailing rail only), ValidationKey,
GeometryCertificate, GeometryView, Diagnostic, IAuthoringCore and IProjectStore. It is a **contract sketch**,
not a production namespace. GeometryCertificate has an assembly-internal constructor; adapters cannot
construct one. Its exact source/base/draft/generation/evaluator binding and definition hash must match at
Apply. A preview carries sampling/error provenance, never a certificate. The binding spike rejects stale,
different-source, missing-certificate, Not assessed and forged-identity candidates.

Before B/C parallelization, B must extract approved contracts into the owned Core assembly and provide a
compiling fake/session fixture implementing the same boundaries; the full session interface must include
Open, BeginRailEdit, UpdateDraft, Validate, Apply, Cancel, Undo, Redo, Snapshot and recovery. The current
sketch does not pretend to provide those complete session operations. Therefore **B→C remains serial until
the Owner's contract gate observes that executable fixture**, even if both path sets are known.

## Input/output shape and transaction algorithm

Open receives bounded bytes and explicit kind (.foil or native), never extension-based guessing alone.
Native schema v1 contains format, projectId, immutable source/design/accepted arrays, cursor facts and recovery.
Each source payload is base64 chunks in source order; concatenate decoded bytes, then verify stored SHA-256.
Unknown fields are schema failures or an explicitly read-only retained extension envelope; never dropped.
Source syntax/version/reference/geometry validation follows FoilDSL §7 phase order. A failed dependent phase
does not manufacture secondary diagnostics. Codes/ranges use UTF-8 byte offsets; UI computes line/column
without changing bytes. Invalid UTF-8 and lone surrogates are rejected.

### Exact proposed native envelope v1

This is a **proposed contract for the serial core design gate**, not an implemented serializer. JSON object
keys are case-sensitive; duplicate keys, comments, trailing commas and nonfinite numbers are invalid.
All listed keys are required except nullable values explicitly shown; unknown keys cause read-only
`DOC-UNSUPPORTED-FIELD`, preserving bytes and refusing overwrite. Unknown `format` gives
`DOC-VERSION`; no migration is guessed. Maximum8MB applies to encoded envelope bytes; each native line
maximum4096bytes excludes its CR/LF delimiter. Strict UTF-8, no BOM for native JSON. Base64 uses RFC4648
standard alphabet with required padding, no whitespace, chunks of at most3072characters; concatenate
decoded chunks, not encoded strings. Nonfinal chunks encode complete byte triples. Decoded source≤1MiB.

```json
{
  "format": "cfdw-project-1",
  "projectId": "uuid",
  "sources": [{"id": "64-lowercase-sha256", "utf8Base64Chunks": ["Zm9pbGRzbCAuLi4="]}],
  "designs": [{"id": "uuid", "parent": null, "surfaceHash": "64-lowercase-blake3", "evaluator": "cfdw-cv/1"}],
  "accepted": [{"id": "uuid", "parent": null, "sourceId": "64-lowercase-sha256", "designId": "uuid", "operationId": "uuid"}],
  "cursors": [{"sequence": 0, "target": "uuid", "reason": "open", "operationId": "uuid"}],
  "recovery": null
}
```

The string placeholders above describe type/format, not a valid geometry fixture. A non-null recovery has
exactly `draftId` UUID, `baseAcceptedId` UUID, `generation` nonnegative safe integer, `rail`="leading" or
"trailing", `vertexId` nonempty string≤4096Unicode scalars, and `utf8Base64Chunks` as above. Recovery bytes
may be syntactically incomplete but must be valid UTF-8; opaque undecodable file data is an import error,
not editable source. Arrays are ordered append histories; duplicate record IDs or transaction IDs outside
the linked accepted/cursor pair defined below, missing references,
parent references not earlier in their own array, nonconsecutive cursor sequence or absent target reject
the whole envelope with `DOC-REFERENCE`. At least one source/design/accepted/cursor exists. Source IDs are
recomputed; every accepted source reparses to its referenced design's surface hash/evaluator, otherwise
`DOC-INTEGRITY`. No persisted active pointer competes with the last cursor fact.

An accepted row is a **Source revision transaction**, not necessarily a new Design revision. Trivia-only
Apply appends accepted source history referencing the existing designId and Surface hash. A semantic change
appends a Design revision with the previous designId as parent and a Surface hash of exact canonical inputs.
Surface revision identity is the definition hash/evaluator pair; equivalent geometry with different defining
controls need not deduplicate. Undo/Redo only add cursor facts referring to accepted rows. Source hashes may
deduplicate byte-identical blobs; transactions remain separate. No timestamp changes identity or ordering.
Project alternatives/analysis records are outside this M1 envelope; opening a future envelope with those
fields is read-only unsupported, never destructive round-trip.

Operation-ID uniqueness is per logical transaction, not per row. An Apply's one accepted row and its one
cursor fact share an operationId and must refer to each other. An Open does the same for the first accepted
row. Undo/Redo have one cursor fact each and a fresh operationId. Retrying an identical transaction ID and
payload returns its existing result; reusing that ID with different source/base/target/action is
`DOC-OPERATION-CONFLICT`. A second accepted row or additional cursor fact with the same ID is malformed.

Deterministic cursor replay maintains a session redo stack derived only from facts: Undo moves to the
current accepted row's parent and pushes the previous target; Redo pops exactly that pushed target and
requires its parent equal to current. A new Apply or explicit branch selection clears the current redo
stack while retaining every historical node/fact. No arbitrary child is selected by array order or timestamp.
At reopen replay the complete cursor sequence, rejecting an Undo/Redo target that violates these rules.
Root Undo and empty-stack Redo are no-ops and append no cursor fact. These rules belong to the serial contract
completion gate and need executable fixtures before production code or B/C parallelization.

1. Begin numeric edit records accepted base, fresh draft ID and rail/CV ID. Another selection changes only
   inspection. Competing writes refuse with the current owner named.
2. A typed SI value patches the exact source token span. Untouched bytes are identical. Patch reparsing must
   reproduce the intended CV and preserve the other rail's complete record. If safe patching is impossible,
   show explicit source replacement diff; no silent reserialization.
3. Validation binds source SHA, base, draft ID, generation and evaluator. Any edit increments generation and
   cancels older validation; at one second return Not assessed if proof incomplete. Time is not proof.
4. Apply under the session lock checks current base and every certificate binding, appends one immutable
   revision, then a cursor fact. Duplicate operation ID returns the same commit, not another revision.
5. Cancel removes the draft only. Undo/Redo append cursor facts selecting previously accepted revisions;
   no parser refit, no mutable history. A new edit after Undo appends a branch with its actual predecessor.
6. Save captures one immutable envelope image, writes away from UI thread, checks original hash/claim, and
   atomically replaces only under the approved filesystem policy. Concurrent edits remain dirty afterward.
7. Reopen validates all source/hash/parent/cursor references and then the active geometry. An invalid recovery
   draft is offered alongside labelled accepted geometry; it never silently replaces accepted source.

CLI: `cfd-workbench inspect <path> --json` emits schemaVersion, acceptedSourceSha256, definitionHash,
evaluator, assessment, diagnostics and derived geometry with units/error bounds. Exit 0 = admitted; 2 =
invalid input; 3 = unsupported; 4 = Not assessed; 5 = I/O/conflict; 130 = cancellation. No partial success
with omitted fields. Numeric GUI controls use the same core conversion/evaluation, not formatted display text.
An implementation must compare exact identity and numerical bounds across both adapters and target OSes.

## Geometry admission and resource limits

Architecture §5 is the certificate algorithm contract. Exact rational inputs derive from canonical binary64
values, not from already-rounded display strings. Bounds are converted outward for presentation using
neighboring representable doubles. Each span needs monotone x support, chord positivity, thickness bounds,
profile separation and bounded maximum normalization. A proof object records the algorithm/version,
domain intervals, rational lower/upper witnesses and source binding. Invalid is reserved for an actual
counterexample or violated structural rule; insufficient sufficient bounds say Not assessed.

M1 one-profile restriction and supported lock/ID forms are feature admission, not changes to grammar.
Station root/tip/interior positions remain authored assignments; slices remain derived. Source preserves
unmodified unsupported input bytes in a read-only view. No duplicate chord/t/c authority is introduced.
Memory/time bounds: source1MiB, native8MB, language point/count caps, token/nesting bounds derived from grammar,
at most4096 subdivision nodes per scalar bound and one-second overall validation. The first exhausted cap
returns Not assessed and releases CPU. Display sampling is independently bounded and cannot clear that gate.

## Native and CLI UI

Reuse DESIGN.md's ParametricWorkbench tokens, fixed window and internal panel scrolling. M1 shows CAD,
selected station/section, two independent rails, numeric Properties, source, one draft status and file/history
commands. Other product areas show Unavailable with their missing capability; no scientific fixture values.
Windows Ctrl and macOS Cmd shortcuts use native command bindings. F6 cycles regions; keyboard selects a
station and editable CV with name, role, unit/value and constraints; Enter/Escape act only in their draft
context and never hijack a native button/dialog. Custom viewport accessibility exposes semantic station/CV
children; generic unknown role from the spike is unacceptable for product acceptance.

| Component | Required state/copy |
|---|---|
| Document | Empty: Open a foil or Example; loading: Reading local file; accepted: Accepted rN; dirty: Unsaved changes |
| Draft | Preview from rN; Apply/Cancel visible; selected other item says Inspection only — draft target remains … |
| Validation | Valid admitted subset; Invalid with code/range/repair; Not assessed — proof budget exhausted; Unsupported feature — source retained |
| Save | Saving immutable rN; Saved rN; Disk conflict — original changed; Save failed — accepted source retained; retry/Save As |
| Recovery | Recovery draft from rN available; Compare / Resume / Discard recovery; accepted source remains labelled |
| Analysis | Unavailable — no analysis method implemented |

All standard hover/focus/active/disabled states use existing tokens. No motion is needed; reduced motion
does not change meaning. At narrow/DPI presets use accessible region switching, never shrink CV hit targets.
Native platform sources are Apple AppKit/NSAccessibility and Microsoft UI Automation/desktop keyboard
guidance. A browser craft detector that matches zero C# files is N/A, not a pass. Native equivalent gates
measure computed token contrast, focus/target geometry, keyboard traversal and AX/UIA names/roles/values.
VoiceOver/Narrator traces and Windows/macOS DPI matrices remain required, beyond this minimal spike.

## Failure modes and test plan

| Choice / failure | Disposition | Required falsifier |
|---|---|---|
| Lossless source / invalid UTF-8, oversized or unknown token/version | Prevent partial acceptance; retain old source | Truncate at each token, malformed Unicode, limits±1, unknown field corpus |
| Background proof / stale or cancelled completion | Reject mismatched key; release resources | Interleave two generations, changed base, cancelled work and late reply |
| Exact bounds / precision explosion or ambiguous zero | Not assessed at fixed cap, never accept | Repeated knot/root, near-zero chord, crossing and long rational fixture |
| Apply / duplicate or concurrent action | Session lock + operation ID, certificate binding | Duplicate Apply, two writers and forged public definition hash |
| File write / short write, full disk, replace failure, interruption | Keep old or complete new; recover labelled temporary | Inject before/mid-write/before/after replace, read every output |
| External writer / file changed | Conflict; no silent overwrite; cooperative claim | Change before final check; lock collision; document late-race residual |
| Reopen / tampered hash, dangling/cyclic history | Reject envelope; preserve file | Mutation of accepted row, parent/cursor missing, cache removal/rebuild |
| Native UI / unknown AX role or lost focus | Block native acceptance | Inspect station/CV role/value with real AT; dialog Cancel/Enter negatives |

Testing Strategy union: D0 hygiene, D1 deterministic logic, D2 property/fuzz/boundary math and serialization,
D3 project dependency direction, D4 real filesystem integration, D5-provider public core contract tests,
D6 envelope/CLI golden payload compatibility, D7 future fake adapter fidelity. D5-consumer and A1–A6 do not
trigger: no service/MCP/model/prompt boundary. Red observations are fault injection and deliberate invalid
vectors, not assertion-free “smoke” claims. Normal-path telemetry tests assert event code, status, duration
and no source/path/name text. Architecture tests forbid UI/I/O dependencies in Core and network use in M1.

## Adversarial analysis (STRIDE-lite)

| Boundary | Threat | Disposition / negative test |
|---|---|---|
| File → parser | S/T: forged source/hash or evaluator | Recompute exact hashes, strict version/grammar; tampered source never accepted |
| File → parser | D: huge nested/number/string inputs | Enforce byte/token/count/time limits before expensive math; limits±1/fuzz |
| Source text → UI | E/I: script or format injection | Native text values only, no eval/HTML/link execution; hostile strings remain literal |
| Save → filesystem | T/E: symlink/path escape, external replacement | No-follow path/parent validation and scoped handle policy; link ancestor/race tests |
| Command → Apply | S/T/R: stale/forged/duplicate write | Opaque certificate binding, operation IDs and append-only facts; mismatch fixtures |
| Local metadata logs | I/R: source/name leakage or missing outcome | Local metadata-only events, no source/path/name; capture and scan event corpus |

## Privacy analysis (LINDDUN-lite)

| Flow / categories | Finding | Disposition | Verification |
|---|---|---|---|
| Source names/comments and file paths · L/I/D/D | Identifying content could enter logs or leave device | Mitigate: metadata-only local logs; no network exporter | Marker in path/source absent from logs; offline walk |
| Accepted history · N | Local facts might imply authenticated personal attribution | Mitigate: no authenticated-author claim; operation identity only | CLI/UI copy and schema omit fabricated actor |
| Recovery/history · U | User unaware incomplete source is retained | Mitigate: explicit recovery offer, retained-history disclosure | Reopen invalid draft labels accepted versus recovery |
| Local documents · N-compliance | Retention/access depends on local device policy | Transfer: OS-user filesystem ACL; explicit residual shared-device access | No egress or credential use; no application-encryption claim |

File paths, source comments/names and project rationale can identify people even though M1 has no account.
L/I/D/D: keep these data local and exclude them from logs; no exporter/network client. N: accepted history
is local document evidence, not a claim of authenticated authorship. U: label recovery/history retention and
shape-only export versus project Save. N-compliance: user controls local files; deletion/retention is not a
cloud rights workflow. Recovery cleanup requires explicit disposition and must not delete accepted history.
Tests put a unique personal marker into source/path and require it absent from captured telemetry.
Operating-system access control is a named transfer, not application encryption; shared-device exposure is
a recorded residual. No credential, rider record or third-party data flow is added by this slice.

## Proposed downstream ownership and gates

After Owner freezes contracts, B owns: `src/CfdWorkbench.Core/CfdWorkbench.Core.csproj`, `Contracts.cs`,
`FoilSource.cs`, `Geometry.cs`, `Identity.cs`, `AuthoringSession.cs`; `src/CfdWorkbench.Persistence/` project
and `ProjectStore.cs`; `tests/CfdWorkbench.Core.Tests/` project and parser/geometry/identity/history/store tests.
C owns `src/CfdWorkbench.Desktop/` project, `Program.cs`, `App.axaml`, `App.axaml.cs`, `MainWindow.axaml`,
`MainWindow.axaml.cs`, `Viewport.cs`; `src/CfdWorkbench.Cli/` project and `Program.cs`; native/CLI adapter tests.
These are **path proposals**; Coordinator must issue exact file leases, including shared solution/global.json
and package pins, before writing. No downstream author may change Core contracts unilaterally.

Shortest safe sequence: approve ADR → B first completes the exact session/schema design and compiling fixture
at a serial pre-code gate → B builds core+real store with negative tests
→ C binds native/CLI → integrated same-source save/edit/reopen test → root/Owner native review. Only a
compiled stable fixture removes B→C's decision edge. B is an internal technical track, not a delivered
increment or runnable CLI. Only the integrated M1 is the vertical delivery. G3 is not a full contract freeze
while that serial design gate is open; no downstream parallel implementation is authorized by this document.

## Definition-of-done disposition

Responsibility, data grain, history, surface trace, scoped contracts, patterns (Ports/Adapters, immutable
snapshots, owned transaction, memento history), failure/security/privacy, UI states, telemetry and test union
are specified. Standard library handles bytes/history/files; small rational certificate logic is bounded;
Avalonia supplies native UI and Blake3 supplies required hashing. No ORM/messenger framework dependency.
`simplify:` conservative certificate admission until a valid requested foil is rejected; extending admission
requires proof/oracles, never sampling fallback. Native performance/craft/AT, complete C# parser/JCS/store,
independent veto and security/privacy rollup join remain explicit gates. This author clears none of them.
