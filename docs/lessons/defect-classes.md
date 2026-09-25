---
id: defect-classes
title: CFD-Workbench defect-class register
type: doc
status: accepted
owner: "@timianmalloo"
tags: [lessons, controls, geometry, specification]
links:
  - {to: spec-cfd-workbench, rel: relates-to}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: mockup-workbench-v1, rel: relates-to}
review-by: 2027-03-19
summary: Design-time failure classes and their mandatory checks, loaded at session grounding under AGENTS.md. Product-runtime controls remain explicitly pending until the corresponding implementation exists.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: mockup-workbench-v1, on: 2026-09-20, reason: "Mockup v1 built against spec v1 and cleared by the UX & Accessibility lens 2026-09-20; supersedes the 2026-09-19 prototype as the review artifact." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# Defect classes

This register is an always-loaded grounding control under AGENTS.md. Each row makes a future author check the class before changing its surface. Acceptance oracles below are specified controls, not claims of executed application tests.

| Class | Class → sweep → derive → prevent | Control and status |
|---|---|---|
| GEO-A · Competing shape authority | Catalog assignment, thickness interpolation and a t/c channel can each appear to own thickness. Swept A4, GEO-07, profile conversion and result invalidation. Derive effective coordinates from one t/c owner after normalized shape blending. | Standing review rule: blend profiles with different thickness-peak locations, renormalize before t/c; test retained override versus source thickness. A4/GEO-07 specify it; runtime fixture pending. |
| UX-A · Optional path becomes mandatory in flow | A flowchart linked export only from accepted AI output despite global export prose. Swept F1–F5 and IA for optional-dependency bottlenecks. Derive independent entry for global actions and a failure return path. | Standing flow review rule: trace no-key New→edit→save→export without AI nodes. F5 corrected; future keyboard E2E trace is required. |
| SPEC-A · Scope named but not falsifiable | Broad “analysis” requirements omitted transition/load/critical-speed outputs named in proposals. Swept all six source stages against story IDs. Derive explicit supported and unavailable paths. | Source-to-story coverage matrix plus ANA-10–14/AI-05; independent Product/Test review confirmed closure. |
| FRAME-A · Handedness asserted from intuition | A transient draft mistakenly called aft/starboard/up inconsistent. Swept coordinate, incidence and mirrored-half wording. Derive from forward/starboard/down: flipping x and z preserves determinant +1. | Always-loaded rule: express basis against a known physical reference and calculate determinant before changing signs; never “correct” a source from mental rotation alone. Final A4 uses the source's right-handed frame; numerical geometry tests remain a handoff requirement. |
| FIT-A · Approximate conversion treated as categorical error | Draft prohibited a fit from reporting zero, even if measured. Swept conversion, CST and source-identity claims. Derive error from observed comparison, not method label. | Always-loaded rule: conversion reports measured residual and tolerance; zero is permitted only with evidence, never assumed. A4 updated. |
| UI-B · Hidden state overridden by authored display rule | The specification contents filter set `hidden`, but the navigation `display:block` rule kept all 27 links visible. Swept prototype author guidance for the same selector shape. Derive proof from rendered visibility, not the attribute alone. | Explicit `[hidden]` rule; browser oracle observed 27 visible links before the fix and exactly 1 after the geometry query. Standing rule: harness/filter assertions inspect rendered visibility. |
| UI-C · Rerender discards focus or capabilities | Workspace replacement dropped keyboard focus to BODY; narrow CSS hid project navigation. Swept all task routes and review selectors. Derive semantic focus restoration and an explicit project drawer instead of hidden functionality. | Portable `tools/check-mockup.mjs` covers repeated keyboard edits, dialog return focus and narrow project access. **Recurred in mockup v3 (2026-09-21)** in every renderer that replaces its container — fixed as a class with one `withFocus` wrapper over nineteen renderers and a `focusKey` that names every `data-*` control identity; `tools/check-mockup-v3.mjs` activates one control per shell region *and* per document form by keyboard and asserts the same logical control keeps focus. Rule: a mockup wraps its renderers before it writes its first one. Independent reviewer observed the original failures and corrected behavior. |
| EVID-A · Historical presentation reads mutable current state | Results were labeled revision 12 while rendering current edited stations. Swept snapshot geometry, pressure masks and source labels. Derive the result view from its immutable fixture input; partial evidence must actually be masked. | Browser oracle edits the current shape and requires historical Results SVG unchanged; partial Results must contain masked paths. |
| GEO-B · Readout and preview use different evaluators | Quintic preview replaced linear geometry, but area still summed linear station segments. Swept area/AR and profile preview commit semantics. Derive readouts from the same curve evaluator, or label an unaccepted profile preview without mutating the design. | Tangent-edit regression requires linked dimensions to update; unaccepted section preview leaves design revision/geometry intact. Controls live in the portable mockup check. |
| DOC-C · Generated edition repeats canonical metadata | The HTML header hardcoded revision 0.1 while the updated Markdown declared 0.2. Swept renderer, visible body, source hash and browser parity. Derive the header from the canonical visible revision. | `tools/check-spec-html.mjs` now checks edition-label equality as well as full text/hash. Observed red: stale HTML returned `revisionMatches:false` and `hashMatches:false`; final regenerated evidence is recorded in the iteration proof. |
| TEST-A · Sensitivity oracle omits degeneracy and constraints | Draft GEO-13 required every weight increase to change the curve, including zero-influence or fully locked fixtures. Independent review disconfirmed the universal condition. Swept weighted-control, endpoint and lock acceptance wording. Derive expected sensitivity from a nondegenerate unlocked fixture; locks use a separate preservation/rejection oracle. | Always-loaded rule: geometry property tests name nondegeneracy, nonzero influence and applicable constraints before requiring a change. GEO-13 carries those preconditions; the mockup check exercises a specific off-curve control. Production evaluator tests remain required. |
| UI-E · Render failure leaves plausible prior evidence | A slice-preview local-name collision threw during a 2D switch, leaving the prior 3D view visible. Swept view selection, field selection and sample replay: selected controls alone are not proof of rendered state. Derive field/view presentation from selected identity and reject stale evidence after errors. | `tools/check-mockup.mjs` checks actual 2D SVG identity and fails on page exceptions, alongside sample/metric linkage. The author observed the view oracle fail on this transient defect before fixing its variable scope. |
| GEO-C · A draft follows mutable selection instead of its edit target | New curve drafts could be retargeted when station/channel selection changed. Swept section, outline and selection handlers. Keep target ownership explicit. V7 preserves a draft during read-only inspection and refuses competing writes; selection never transfers ownership. | Browser regression switches target with an active draft and requires unchanged draft/base/source and no cross-station/channel application. `tools/check-authoring-v7.mjs` covers another station and Source; the CAD oracle covers another curve. Production edit drafts bind base revision and target identity. |
| EVID-B · Missing-variable status overstates missing-result scope | The partial wall-shear state hid only a field but said coefficients and forces were absent while showing them. Swept missing case versus missing variable messages and metric rendering. Derive availability and copy at their actual scope. | Independent rendered review identified the mismatch; the missing-wall-field oracle requires field-specific copy with valid metrics retained, while failed-sample checks require all sample evidence cleared. |
| SPEC-B · One quantity, two definitions in one artifact | Specification v1 defined the identity tolerance as "1 µm AND 10⁻⁶ relative, both must fail" in A4.1 and "pass iff ≤ 1 µm AND ≤ 10⁻⁶" in A4.5 (opposite logic), and the run key with two member lists neither of which held the settings hash. Swept every term the spec defines more than once (identity tolerance, run key, Example, revision, supported, Unavailable). Derive: one section owns the definition; every other mention cites it. | Gate rule at `/specify` Stage 4: a Test Architect sweep greps each defined term for a second definition; the v1 gate observed the Blocker before the fix. A future `tools/check-spec-terms.py` lint is the pending mechanical control. |
| DATA-B · Derived number printed from rounded intermediates | KB 04's goal-state row printed CL 0.774 because q had been rounded to 13,570 Pa before the division; exact arithmetic gives 0.773, and the spec promised reproduction "to displayed precision". Swept the seven-point table and every figure the spec cites from it. Derive every displayed number from pinned inputs in one executed script, never by hand. | `scratchpad/goal_fixture.py` re-executed the table (pinned 1 kn = 0.514444 m/s); GOAL-02 now names its pinned inputs; the mockup's Brief computes the triples live and the browser oracle asserts them. Rule: a KB table of derived numbers carries its inputs and its generating script. |
| UI-F · Unscoped attribute selector binds a handler to the wrong element | The mockup's `[data-mode]` click binding matched both the mode buttons and the window element carrying `data-mode` state, so any click in the window reset the curve mode. Swept every `document.querySelectorAll('[data-…]')` in the artifact for an ancestor carrying the same attribute. Derive: state attributes live on the root/window, control attributes on buttons, and handler queries are scoped to their container. | `tools/check-mockup-v1.mjs` observed red (Smooth mode lost after clicking the weight field) before the fix; handler queries now scope to `#editor-side`; the window carries no attribute a control also uses. |
| UI-G · Review chrome measured as product surface | The craft detector flagged seven harness colours as off-token and a fixed drawer was hidden behind the sticky harness, so the review instrument produced findings and blocked a control. Swept the harness palette and every fixed-position overlay. Derive: review chrome uses the design language's dark tokens, and product overlays (Checks drawer, assistant) are positioned inside the window, never against the page. | Craft gate re-run clean but for one recorded deviation (the spec's em-dash strings); the browser oracle clicks the drawer close control while the harness is sticky. |
| SPEC-C · A derived state named without its function | Revision 1.1 said an Experiment's status was "derived from its runs" and listed six values; no rule decided the status of eleven Completed and one Failed case, so no test could assert it. Swept every "derived" in A3.1–A3.4. Derive: a derived attribute is written as a function of named facts and events, with the mixed-outcome input as its failing input. | Gate rule at `/specify` Stage 4: every "derived" carries `= f(…)`; the 1.1 gate observed the gap and the status function now sits in A3.1 with RUN-05's mixed-outcome Given. |
| DATA-C · A reference by hash to a mutable singleton | An Experiment referenced the Goal state by hash while the Goal state was a single editable value, so one constraint edit and a save left the reference with no referent. Swept every by-hash reference (Design revision ✓, Profile revision ✓, Goal state ✗, Backend environment ✗ — installation-local). Derive: anything referenced by hash is an append-only version; anything installation-local is referenced through a pinned value copied at the reference time. | Goal state and Setup brief are append-only entities; the Experiment carries a backend pin value; GOAL-03's reopen-and-resolve Given is the failing input; DM10 applies to references, not only to updates. |
| SEC-B · Allow-listed action with unbound parameters | The environment assistant allow-listed five actions and showed "the exact command", but nothing said where the digest, URL, distribution name or limit came from — a well-formed wrong digest would pass consent. Swept every proposal kind for parameters a model could supply to an executing step. Derive: a proposal carries an id and, at most, one bounded scalar; the tool binds every other parameter from published product data. | A5.10/A5.12/AI-11/A8.5: step id only; four negative tests (wrong-but-valid digest, out-of-range limit, metacharacters, `curl … | sh`); the tool never elevates. The v2 mockup renders a refused step. |
| UI-H · Duplicate object key silently overrides state | The v2 mockup's model literal declared `run:` twice (the freshness run object and the new run-console state); the later key won and the console crashed on `undefined.state`. Swept the model literal for duplicate keys and the render code for shared names. Derive: one object, one key; a new sub-state gets a new name. | The smoke run observed the crash before the oracle; the console state is `runs`; the assembler could lint duplicate keys in object literals (pending mechanical control). |
| UI-I · A rendered value false to its own fixture | The v2 mockup printed "A_cav 0.041 ≤ 0.02" as a fixed string, a sine wave labelled "difference flood", literal station deviations ("0", "0.004 mm") and a per-sample colour range under a "fixed range" caption — each a number or comparison the fixture did not produce. Swept every rendered inequality, every "computed" visual and every deviation readout. Derive: a comparison is rendered from the two operands; a visual named after a computation is computed from its inputs; an unmeasured quantity says "Not recorded". | The lens found all four by reading (the craft gate cannot); the v2 oracle now asserts the data-driven comparison strings, the computed difference twin, the "Not recorded" readouts and the series-fixed range caption. Rule for mockups: any string that states a relation carries its operands. |
| UI-H2 · Web-page habits in a client mockup | The v2 mockup rendered the client as a stacked page: 1,450–6,500 px tall at every window width, page scroll everywhere, no panel scrolling internally, the area strip wrapping to two or three rows inside a 64 px title bar, and toolbar controls that scrolled or clipped. The archetype signature said `Layout:ViewportWorkbench`; the build did not. Swept the layout facet against the signature and every region against "scrolls inside itself". Derive: for a thick client the window is the unit — a fixed frame, internal scroll per region, a toolbar that fits or overflows into a menu, never wraps or scrolls. | `tools/check-mockup-v3.mjs` fails on window scroll, toolbar > 44 px or overflowing, parameter row > one row, `More ▾` without a hidden group or any hidden group at ≥ 1280 px, or a dock that does not scroll internally, at five presets × six areas; the in-artifact audit prints the shell verdict on every render. Rule: a mockup for a client declares its window presets and proves the shell contract before its content. |
| UI-J · A drawing where the geometry is the interface | The v3 mockup drew every curve as a 60-point polyline, showed one projection per named view with keyboard-only rotation, edited the four channels only as η-plots and opened the section editor as a modal — a *viewer with a curve pane* where the user expected a CAD model. Swept every curve, every view and every editing surface. Derive: for a geometry tool the drawing *is* the interface — every curve a spline, one camera over one model with named presets and free orbit, control curves edited on the elevation that shapes them, and a section edited as a document in the editor group, never a modal. | `tools/check-mockup-v4.mjs` group 13 fails on a polyline in a geometry view, a named view that is not the camera, a channel without a handle in its elevation, an edit that does not open the shared draft, or a station editor that is a dialog; the register rule for mockups: declare the drawing model (splines · camera · elevations · documents) before the first view is built. |
| UI-K · A surface token used on the other surface | The v4 mockup drew the focus ring on the graphite viewport with the light surface's `--focus` (2.45:1) and the refusal ring with `--danger` (2.30:1); a token-pair audit that lists the intended pairs cannot see a token used off its surface, and an oracle that asserts *token identity* on the focused element proves nothing about contrast. Swept every state colour drawn inside `.viewport`, `.rail`, `.statusbar`. Derive: every state token has a viewport twin (`--focus-viewport`, `--danger-viewport`) and a rule that names the surface it belongs to; contrast is measured on the computed pair, never inferred from the token. | `tools/check-mockup-v4.mjs` focuses a viewport handle in all three themes and computes the ring's contrast against the computed viewport background (≥ 3:1, ≥ 3 px); the in-artifact audit lists both viewport pairs; DESIGN.md §2 names the twins and forbids the surface tokens on graphite. |
| UI-L · A drawing rendered for one box and shown in another | The v5 mockup drew each viewport's SVG with a `viewBox` sized at render time (with a 120 px floor) and CSS `width:100%`; whenever the box changed afterwards — a dock collapsing, the bottom panel, the window preset, the floor itself — the SVG scaled and its 12 px labels read 11.7 px and its 40 px vertex hit circles 17 px, under both floors, while every token and box assertion stayed green. Two sibling shapes in the same sweep: a closed `<details>` menu whose items still had a box (48 counted as small targets) and `innerText` of a hidden viewport that includes those items. Swept every SVG that takes its size from its container and every closed menu. Derive: a viewport renders at its true pixel size (no floor), re-renders synchronously from a `ResizeObserver` (a deferred frame can be throttled), closed menus are `display:none`, and the oracle measures text and target sizes *after* the walk, not on a fresh page. | `tools/check-mockup-v5.mjs` groups 1–2 measure the smallest SVG text and the target floors at five presets × six areas after the shell walk; group 13 asserts the forced single viewport at the reflow preset; the in-page audit ignores `details:not([open]) .menu`. |
| UI-M · A drawing rendered from a quantity it does not carry | The v5 station document printed "8 per side", "6 controls each" and "conversion residual 0.000" for a section whose fit had nine vertices and, once measured, an 841 µm residual: three surfaces, three constants, no operand. Swept every rendered count, residual and deviation in the workspace. Derive: a rendered number is computed from its operands at render time, or it reads "not recorded"; the same quantity on two surfaces comes from one function; and the conversion chooses its vertex count *by* the measured residual (centripetal parameters, knots by averaging) rather than asserting one. | `tools/check-mockup-v5.mjs` group 6 asserts the residual shown in the HUD, the strip and Properties equals the residual measured and meets the acceptance, and that Delete, Rebuild and Fit points report a deviation from the curves. |
| UI-N · An assertion the failure mode also satisfies | The v5 oracle's pointer-drag test asserted only the *direction* of the change ("dragging aft lengthens the chord"); the drag mapped the pointer through the SVG captured at press time, which the first re-render detached (its box read zero), so every drag slammed the vertex to its ordering limit — aft — and the test stayed green. The operator found it with a trackpad. Two siblings in the same sweep: hit circles that overlapped at the tip (a press grabbed whichever painted last) and a focus-preserving re-render whose restored focus re-selected the *previous* vertex through its focus handler. Swept every pointer test for direction-only assertions. Derive: a drag test asserts the **relation** (the glyph stays under the pointer to ≤ 2 px over a dozen small moves), a hit test **presses** every vertex at its own centre and reads the selection back, and any handler that calls `preventDefault` on a press focuses its target itself before the re-render. | `tools/check-mockup-v5.mjs` group 6: the twelve-step drag with the pointer-offset bound, the press-every-vertex selection sweep, focus on the dragged vertex on release; the mockup's `liveMap` resolves the svg and mapping per move and retargets a press to the nearest vertex centre. |

## Application architecture boundary sweep — 2026-09-23

**DATA-E recurrence · Validation detached from its candidate or derived identity.**
The first compiling port sketch allowed Apply to receive an unrelated validation result;
the first repair still allowed its public definition hash to be replaced independently.
Sweep: source bytes/hash, accepted base, draft ID, generation, evaluator, assessment and
certificate identity. Derive: one opaque certificate binds those inputs and its computed
definition hash; Apply compares all bindings under the transaction boundary. Prevent:
`tools/spikes/ApplicationNativeUi/Program.cs --contracts` exercises nine mismatch cases,
including a forged/empty definition hash and a moved accepted base. This is a binding
primitive, not proof of a production parser, session or geometry evaluator. Port the same
negative cases into the production core's required gate before its first acceptance.

**UI-N / TEST-A recurrence · A named oracle does not invoke its claimed behavior.**
An initial near-bound geometry check only compared a positive rational with zero, and a
normalization check only ordered interval endpoints. Sweep: every spike check's name,
focal function, failing input and claimed confidence. Derive: exercise the actual
separation function near its boundary and test a known maximum plus physical error
enclosure. Prevent: `Chord_NearBound_AdmitsPositiveRefusesTouching`,
`Maximum_KnownQuadraticDegreeElevated_EnclosesQuarter` and
`Normalization_EnclosurePropagated_PhysicalErrorBelowOneNanometre` in
`tools/spikes/application-contract-vectors.py`. These prove bounded primitives only;
they do not establish full-language conformance.

**IO-A · Cleanup deletes an artifact this invocation did not create.** Owner review
found that exclusive temporary creation could fail on an existing file, then `finally`
unconditionally removed that file. Sweep: target, temporary and sidecar-claim creation,
failure and cleanup paths. Derive: acquisition and ownership precede cleanup; a collision
does not transfer ownership. Prevent: the spike tracks `temp_created` and includes
`Atomic_TemporaryCollision_PreservesUnownedFileTargetAndReleasesClaim` and
`Atomic_ClaimCollision_PreservesOtherClaimAndTarget`. The guard is a local cooperative
writer control, not proof against an arbitrary hostile filesystem race.

**FFI-A · Matching apparent argument types hides a variadic native ABI mismatch.**
A real macOS native Save requested `0600` but published mode `0454` with correct
project bytes. The one-variable arm64 probe reproduced wrong creation modes when
`openat` was declared with four fixed arguments; the correct three-fixed-plus-
variadic declaration created `0600`. Sweep both `open` and `openat` in the
production store and the native primitive test helper, then independently read
temporary, overwrite-claim and final inode modes. Derive: a C compiler makes
the variadic call behind a fixed managed bridge, and the held descriptor must
show regular-file mode exactly `0600` **before any project byte write**; a
post-create chmod cannot establish safe creation. Prevent: candidate
`ee6d73ad` adds `Store_CreationPermissions_BeforeWriteAndAfterPublication`,
`Store_UnsafeCreatedMode_RefusedBeforeBytesWithOwnedCleanup` (extra `0777`
bits on create/overwrite), `Store_MissingOwnerPermissions_RefusedBeforeBytes`
(zero/one owner-bit loss on create/overwrite), and
`Store_OwnerStrippingUmask_FailsClosedWithoutRepair`. The core gate runs the
normal mode matrix under isolated child umasks `0000`, `0022` and `0077` in
build and published layouts; it separately exercises owner-stripping umask
`0600` as a fail-closed capability case. Root's
[investigation](../investigations/native-save-permissions.md)
retains the original file unchanged. The candidate is unjoined; independent
packaged native Save and Owner review still gate production acceptance.

**REVIEW-A · A reviewer substitutes a familiar equation for the normative transform.**
Root initially treated thickness normalization as scaling the whole section, then
incorrectly questioned a bound for cambered sections. Owner disconfirmed it; direct
FoilDSL §6 inspection shows fixed camber in `q=(x,C ± thickness*T/2)`, so C cancels
between normalization endpoints. Sweep: review claims about normalization, placement
and conversion error. Derive: trace exactly which terms change before asserting a
failure. Preventive always-loaded rule: before making a mathematical veto finding,
quote the governing equation and map the challenged computation to its terms. Read
the equation now; do not rely on a remembered model. The incorrect symmetry restriction
was withdrawn and is not a product requirement.

**REVIEW-B · A reviewer reads a call but not the supplied configuration.**
Coordinator saw `SerializeToUtf8Bytes` and raised a possible compact-one-line native
envelope conflict with the 4096-byte line cap, without opening the `Options` value
passed to that call. Root disconfirmed it: `Native.Options` sets `WriteIndented=true`.
Sweep: the encoder call, its options, actual encoded bytes and line-cap preflight.
Derive: a call-site name does not establish behavior when a passed configuration
changes it. Preventive always-loaded rule: follow every load-bearing argument to
its value and inspect the emitted artifact before raising a boundary finding. The
B0 contract gate must execute a positive encoded-envelope/line-cap case as well
as the long-scalar negative case; this mistaken compact-output finding is withdrawn.

**DATA-F · Outward DTO arrays alias session-owned state.** In the first B0
candidate, `Snapshot`, `CaptureRecovery` and `Envelope` returned nested mutable
arrays that a caller could alter without a command, generation or accepted
revision. Sweep: every public return and async save request carrying source,
draft, recovery or base64 chunks. Derive: the session retains authority and
returns defensive images at every boundary. Prevent: B0's named
`Draft_ExternalRetargetMutation_PreservesOwnedTarget`,
`RecoveryAndEnvelope_CallerMutation_PreservesOwnedBytes`,
`Snapshot_CallerMutation_DoesNotChangeSession` and
`SaveRequest_AsyncBoundary_CapturesDefensiveImage` checks. These are contract
fixture assertions; production core tests must repeat the class.

**DATA-F production recurrence:** the first `SourceParse` retained a caller's
mutable `IReadOnlyList<Diagnostic>` and exposed it while `IsParsed` read its
changing count; a public constructor could also manufacture apparent success.
Sweep: source bytes, diagnostics, session snapshots and every returned view
that could carry authority. Derive: parser construction stays internal, bytes
and diagnostic collections are copied into immutable outward images, and
success requires an internal parsed definition rather than an empty list
alone. Prevent: production `Source_OutwardMutation_PreservesAuthority`,
`Source_DiagnosticsMutation_Refused` and
`Source_PublicConstructor_CannotForgeSuccess` pass in the isolated core
checkpoint. These are API ownership controls, not geometry acceptance.

**DATA-G · Retry semantics are not reconstructable from durable facts.** The
first B0 candidate lost Open/Apply operation IDs or their target-sensitive
payload binding at Reopen, allowing duplicate or conflicting use of an ID.
Sweep: Open, rail Apply, Undo/Redo, no-op and branch replay, before and after
save/reopen. Derive: every persisted transaction retains exactly the facts
needed to compare its ID, action and immutable payload; a rail Apply carries
its edit receipt even when geometry is unchanged. Prevent: Ruling 10's schema
plus `Open_Retry_ExactlyOnce`, `Reopen_OpenRetry_DurableExactlyOnce`,
`Reopen_ApplyRetry_DurableExactlyOnce`, `Reopen_OperationKindReuse_Refused`,
`Reopen_ApplyTargetReuse_Refused`, `Reopen_ApplyDraftReuse_Refused` and
`NoOp_Reopen_VolatileIdExpired`. The final oracle passed; no preserved
historical pre-fix execution is claimed.

**DATA-E recurrence · Alternate admission entry skips its proof binding.**
The first B0 candidate guarded Apply but could Open or Reopen a source with an
unrelated or unavailable geometry authority after hash/schema checks. Sweep:
every way a source becomes accepted or active, including Open, Apply, Undo,
Redo and Reopen. Derive: byte integrity and semantic hash are necessary but do
not establish geometric admission; each entry verifies an opaque certificate
bound to the exact candidate/evaluator or preserves the prior state with Not
assessed. Prevent: `Open_UnrelatedCertificate_Refused`,
`Reopen_UnknownGeometry_Refused` and `Reopen_UnrelatedCertificate_Refused`
in the B0 contract fixture, with production analogues required.

**DATA-H · Dirty state compares a disk image with reserialized state.** A
valid native file may use different harmless JSON whitespace from the
writer's normalized envelope. Comparing raw disk SHA with a reserialized
current image falsely marks it dirty; acknowledging an older async save can
also falsely clear a newer revision. Sweep: raw file conflict token, normalized
session image, captured save request and post-save current state. Derive: keep
the exact on-disk hash for external-conflict detection, but compare normalized
session images for dirty state and acknowledge only the captured image.
Prevent: `Reopen_DifferentWhitespace_Clean`,
`Save_LateAcknowledgement_NewerRevisionRemainsDirty` and
`SaveRequest_AsyncBoundary_CapturesDefensiveImage`; a future native store must
exercise the same split with real files.

**EVID-C · A partial worker stream is mistaken for final execution evidence.**
Coordinator initially interpreted an early Grok stream prefix as containing no tool use;
the closed 117-row stream contained a completed read and successful `pwd`. Sweep: final
stream hash, event counts, tool IDs, terminal updates, exact commands and actual files.
Derive: inspect a closed complete receipt and distinguish narration, dispatched calls and
completed operations. Prevent: `tools/coord-stream-summary.py` hashes the complete stream,
correlates calls/updates and checks required successful commands. The complete receipt
passes its `pwd` requirement; the partial prefix and missing-build requirement fail.
No successful read/shell probe is promoted to write/build or containment qualification.

**COORD-REGEN · Shared ledger target mistaken for the invoking checkout.**
`coord regen` reads the shared primary registry and debt correctly, but its dispatcher
passed the primary path to the generator, changing primary `docs/audit/audit-data.js`
while leaving the linked author's derived file stale. Sweep: every coord command that
combines a shared registry/ledger root with checkout-local file effects. Derive: keep
`repo_root()` as the shared state root and pass `checkout_top(os.getcwd())` only to
the generator target; fail closed when the invoking checkout is unknown. Prevent:
`tools/verify-coord-regen-worktree.py` invokes the real CLI in a temporary primary and
linked Git worktree. Its reverted-dispatch RED case changes primary only; the fixed
GREEN case changes linked only and clears shared debt. The primary was restored and
both checkout statuses read back. No global `repo_root()` change is authorized by
this class.

**UI-N / TEST-A recurrence · A helper-level oracle bypasses the failing caller.**
The first regen regression called `cmd_regen(store, linked)` directly; that passed even
with the faulty dispatcher because the helper already honored its second argument.
Sweep: the public CLI entry point, dispatcher, helper and output tree for controls
whose stated failure arises at a boundary. Derive: inject or revert the actual faulty
call site and assert the wrong side effect before the fix, then invoke the same public
entry point after the fix. Prevent: the argument-free `verify-coord-regen-worktree.py`
is auto-discovered by `run-verify-gates.py`; it proves RED with only the dispatch line
reverted and GREEN in the current CLI. A direct-helper test alone cannot certify the
worktree routing behavior.

**TEST-A recurrence · A frozen consumer compiles against a different DLL than
the intended snapshot.** Root's first corrected viewport sampler project
resolved an older adapter assembly through MSBuild candidate-file probing
ahead of its stated `HintPath`; the missing `InteriorEta` compile error was a
review harness binding failure, not a product regression. The earlier
fixed-sampler RED still applied but was re-attributed to the actual frozen
controller DLL. Sweep: all isolated .NET consumers that copy app assemblies
or rely on `HintPath`. Derive: turn off default item discovery in the scratch
consumer and compare each runtime assembly SHA with a frozen manifest before
execution. Prevent: the always-read C contract requires that manifest for
independent binary review; root's corrected sampler retained the SHA-bound
assembly manifest and obtained the same-edit GREEN for all eight editable
targets. A passing consumer with only the top-level DLL fingerprint does not
close this class.

**PACK-I · Generated links are relative to the input root instead of their destination.**
Security/privacy rollups embedded under `docs/security/` contained `design/...` links,
which resolved below the wrong directory. Sweep: both rollup tables and the shared link
emitter. Derive: the scan root and output link base are distinct. Prevent:
`docs-graph.py rollup --relative-to docs/security` computes links for the destination;
the default docs-root behavior is retained. `tools/check-rollup-links.py`, wired into
`tools/check-docs.py`, was observed RED before the option existed and GREEN for default
and nested output directories, checking resolution to an actual source file.

**COORD-ENV · One-command environment assignment does not identify later mutations.**
Root scoped `AGENT_SESSION` to an audit command, then committed later in the same shell
without exporting it. The commit hook explicitly reported advisory/no identity; that
commit is not claimed as enforcing. Sweep: the eight committed paths were subsequently
checked with explicit identity and each returned structured `decision: allow`. Preventive
always-loaded rule: export the task identity in every mutating shell batch, or prefix
each Git mutation individually; inspect the hook result and never treat its advisory
exit zero as an ownership check. A post-commit check does not retroactively strengthen
the original commit-boundary evidence. B0's `cb73079e` commit repeated the
advisory/no-identity shape; Coordinator explicitly checked all twelve committed
paths under `cfd-contracts-author-20260923` afterward and observed twelve
`allow` decisions, again without a retrospective enforcement claim.
The R37 independent review checkpoint repeated the shape: its first commit
omitted `AGENT_SESSION`, so the hook was advisory. An attempted amend with the
identity set had no staged paths. Root then ran the supported ownership check
with explicit identity against all four committed paths and observed four
`allow` decisions. That readback bounds ownership but does not turn the first
commit into an enforced commit-boundary check. The same per-mutation identity
rule applies to review commits as to production commits.
The W1 independent review commit repeated this lapse: `AGENT_SESSION` was
omitted, so the hook was advisory. Root immediately checked all four committed
paths with the correct session while its claims were live; each returned
`allow`, then it released the claims. That is an ownership readback, not an
enforced original commit. A future review commit must prefix the actual
`git commit` process, not only the preceding audit or claim command.

**TOOL-PATCH · A replace operation is expressed as delete-plus-add in one patch.**
The first full parser patch asked `apply_patch` to delete and add the same
path in one transaction; the tool rejected it and changed no file. Sweep:
large rewrites within a leased file and any staged path after patch failure.
Derive: use one `Update File` operation (or bounded sequential updates), then
read back the intended path and status before compiling. Preventive
always-loaded procedure: on any patch error, check `git status --short` and
the file contents before retrying; never count a prepared patch as applied.
The parser's first build and tests were reported only after that readback.

**ENV-C · A declared build scratch is silently replaced by a host temp default.**
The first core gate used `tempfile.gettempdir()` at import, which resolved to
macOS `/var/folders/.../T` despite the approved task-specific `/tmp` plan.
Sweep: core gate and both joined .NET recount scripts; unrelated unique temp
directories without an exact-root promise are not mislabeled violations.
Derive: pass the scratch parent explicitly, make it unique per invocation,
resolve the macOS `/tmp`→`/private/tmp` alias, and assert the canonical parent
before launching a child. Prevent: `tools/verify-application-core.py` records
logical and canonical scratch, checks its parent, and records all six .NET
cache/temp paths and build outputs under that unique root on every normal run.
The Ruling 14 retry receipt
`/tmp/cfd-application-core-20260923-uj2qxazf/receipts/environment.json`
shows the corrected paths; its six observed owned PIDs ended quiescent. The
old task-named `/var/folders` scratch remains retained for review, not erased
as if it were unowned clutter.

**ENV-D · A skip-first-run switch does not disable every SDK first-run effect.**
The first .NET build printed a certificate-installation banner even with
`DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`; no new global certificate was proved,
and a matching PFX predates this session. Sweep: both joined .NET recount
scripts and the first-core verification gate. Derive: set Microsoft's
`DOTNET_GENERATE_ASPNET_CERTIFICATE=false` before invoking `dotnet`, retain
task-local CLI and NuGet caches, and inspect the actual command output rather
than inferring trust from a banner. Prevent: all three scripts now set that
variable; the Ruling 14 retry's environment receipt records it and its build
log contains no certificate/trust banner. No broad certificate clean or trust
change is a legitimate repair for this class.

**PROC-C · Agent interruption leaves an owned build child running.** The
built-in cancellation drill interrupted its active worker, but PID `70844`
survived until the Coordinator sent TERM to that exact observed PID and read
back absence. Sweep: every build/test runner and worktree handback. Derive:
track PID with start identity, stop dispatch on interruption, terminate only
verified owned children and read back quiescence; an interrupt API return is
not process proof. Prevent: the first-core gate writes process/environment
receipts and checks live descendants before success. Its post-launch observer
failure path reports descendant quiescence **Not assessed**, reaps the exact
`Popen` child, and stops the route. Windows fails closed before child launch
until a measured process-tree adapter exists. The [drill receipt](../coordination/application-cancel-drill.md)
retains the original survival/cleanup sequence.
The first Avalonia build is a sibling of this class: a package task launched
collector PID `22494` that outlived its failed compiler parent even with
`--disable-build-servers`. Sweep now includes dependency build tasks, not
only test runners. The pinned package documents process-local
`AVALONIA_TELEMETRY_OPTOUT=1`; the Ruling 24 measured retry recorded that
setting in the child environment and observed no collector in its sampled
process group, then verified the group empty. Prevent: the C gate keeps the
opt-out and exact PID/start/descendant sampling on every Avalonia build or
publish, and stops on an observed collector or override. Sampled absence is
not proof of zero prior network effect or a universal no-child guarantee.

**EVID-TZ · A local timestamp is given a UTC suffix.** A first read-only
`stat -t ...Z` printed the PFX's Pacific local clock while labeling it `Z`.
Sweep: evidence commands that attach `Z` to host-formatted timestamps.
Derive: set `TZ=UTC` on the command or retain an explicit numeric offset;
never add a UTC label after formatting. Preventive always-loaded command
pattern: `TZ=UTC stat -f '%Sm' -t '%Y-%m-%dT%H:%M:%SZ' <path>`. The corrected
readback was `2026-05-05T18:32:31Z`, before this task; the first mislabeled
output is not reused as UTC evidence.

**NUM-G · A secondary numeric parameter bypasses the token resource bound.**
The first production `DecimalSi.Parse(token, int decimalScale)` checked the
token's effective exponent, then added an arbitrary public `decimalScale`
before `BigInteger.Pow`; a small token with an extreme caller scale could
allocate without bound. Sweep: production `Identity.cs` and the B0 fixture's
same helper shape. Derive: grammar-supported length and area unit exponents
are a closed set `0, -2, -3, -4, -6`; reject any other scale **before** token
scanning or exponent construction. Prevent: production
`Decimal_UnsupportedScale_RefusesBeforeScaling` and extreme-int boundary tests
in `IdentityTests.cs`. The +1-scale case was observed failing before the guard
and passing after it; deliberately running `int.MaxValue` against the unsafe
version would defeat the resource control and is not claimed. The B0 source
remains a design fixture, not a production numeric API, and retains this
residual sibling pending any separately scoped spike maintenance.

**LEX-D · End-of-line anchor mistaken for full-token acceptance.** .NET regex
`$` can match before a final newline, so the first numeric helper could accept
`1\n` as one token. Sweep: production numeric lexer and B0 fixture token,
integer, hash and UUID regexes using terminal `$`; parser admission must
check complete spans rather than trust a prefix. Derive: use `\A...\z` or
check exact match length for a single token. Prevent: production
`Decimal_TrailingNewline_RefusesNonToken` was observed RED with `$` and GREEN with
`\z`; whole-source parser tests must retain the same byte-span boundary.
The B0 fixture's remaining `$` forms are recorded as design-only residuals,
not promoted to a production admission claim.

**LEX-E · A synthetic end marker is forgeable source text.** The first whole-
source parser used a token spelling `EOF` and `Expect("EOF")` without proving
the reader reached the actual final token index. A user identifier `EOF`
could end parsing early and hide trailing text. Sweep: foil, standalone
section and every alternate root production. Derive: success requires the
structural end sentinel **and** complete token consumption, never a word
equality alone. Prevent: production `Parse_FoilSpoofedEof_RejectsTail` and
`Parse_SectionSpoofedEof_RejectsTail` were observed RED before the index
check and GREEN afterward; the independent frozen-DLL consumer also checked
the spoofed-tail boundary.

**GRAMMAR-B · A shared helper admits its caller's forbidden supergrammar.**
`ReadProfile` accepted asset references when called by standalone section,
whose normative production permits profile body only. Sweep: shared profile,
lock, assertion and assignment readers at each root grammar entry. Derive:
pass the root production's allowed variant explicitly and reject a forbidden
variant in the syntactic phase, even if the shared helper can parse it for
another caller. Prevent: `Parse_StandaloneAsset_IsSyntaxError` was observed
RED then GREEN. `Parse_OneAssignment_ReportsSyntax` likewise makes a missing
required second assignment a syntax error; root independently reproduced
and then closed that named finding against frozen DLLs. The parser's full
language conformance remains a separate gate.

**DIAG-A · A generic parse failure erases actionable context.** The first
parser emitted null entity and generic reason/recovery for every failure.
Sweep: curve count/order, missing profile/asset, lock and assertion failures.
Derive: carry the failing curve or reference identity and the specific
required count/order/action into stable diagnostic fields while preserving
the original source span and phase. Prevent: production
`Diagnostic_CurveError_NamesCurveAndRequirement` and
`Diagnostic_MissingReference_NamesTarget` were observed RED before context
propagation and GREEN afterward. Uncovered diagnostic families remain
explicitly open; two named examples do not clear the full diagnostic contract.

**TEST-SPAN · A diagnostic oracle guesses the failure token.** An initial
missing-unit case expected the `evaluator` keyword. The parser consumed that
word as the prospective unit and correctly stopped at the following quoted
evaluator value. Sweep: mixed syntax/lexical cases where a missing token lets
the next token fill its slot. Derive the expected span by tracing the frozen
grammar and checking the exact source slice, rather than weakening the span
assertion after a mismatch. Prevent: `Ruling15_MissingUnit_PreventsOverflowBinding`
checks code, phase and the quoted token span; its first wrong-oracle receipt
`ac71hlaw` and corrected 63-case receipt `83ry315v` remain retained.

**CAP-SEAT · An optional review silently exceeds the agreed active-agent cap.**
During serial core work, root activated the Owner for an optional math review
while root, Coordinator and core author already occupied the three active
execution seats. Root stopped that review without a new writer or process.
Sweep: optional review activation, worker resume, and replacement after a
checkpoint. Derive: count actual active team members with `list_agents`
immediately before activation; schedule an optional reviewer only when a seat
is free, or explicitly replace a paused active seat. Prevent: the Coordinator
records the observed count and cap in the dispatch checkpoint, and refuses a
fourth active execution turn. The Owner review is deferred to a seat change;
its pending finding is not treated as already delivered or cleared.

**GRAPH-REG · Generic graph propagation crosses an exclusive register writer.**
`docs-graph.py flag --changed coordination-application-build` included the
inbound `rulings` artifact and added review-suggested frontmatter to
`docs/notes/rulings.md`, whose writer is `coord decide rule`. Sweep: V16 inbound
neighbors of coordination changes and other register-class targets. Derive:
metadata provenance does not override an exclusive register write path.
Prevent: the always-read coordination plan requires restoring only that
generated flag, deriving the index again, and checking the ruling-register
diff is empty before commit. The current flag was removed; no ruling prose
or numbered decision changed. A future graph-tool class-aware exclusion may
replace this local join control after separate review.

**ORACLE-X · A reviewer equates a physical station with its spline parameter.**
In the independent twist-collision calculation, I first used `eta=0.203125`
as the cubic span parameter and reported CV2 weight `9633/32768`. The authored
abscissa is nonuniform: its first span reaches that eta at local `u=1/2`, so
the correct weight is `3/8`. The initial angle bits were withdrawn before a
technical ruling; this was a reviewer-oracle error, not a product regression.
Sweep: section, twist, rail and placed-point oracles that evaluate a B-spline
at a physical x/eta. Derive: solve or prove `x(u)=requested position` before
using basis weights, then compare exact/interval outputs. Prevent: the
always-read B packet requires that inverse-abscissa check, and
root's retained `exact-oracle.py` plus nonlinear-x section consumer provide
the corrected named evidence. Matching a qualitative outcome from a wrong
parameter is not counted as a valid oracle.

**IDENT-CV · A derived-unit transform erases an authored distinction before identity.**
The `/1` twist hasher converted individual degree controls to rounded radians;
the adjacent binary64 degree values `1.791` and `1.7910000000000001`
collapsed to one Surface hash although degree-curve-then-convert evaluation
produced different placed coordinates. Sweep semantic hashes, cache keys and
certificate bindings that canonicalize a derived representation instead of
the inputs defining its evaluator. Derive identity from the exact defining
degree CVs under an explicit evaluator version; retain source SHA separately.
Prevent: FoilDSL DSL-19/20 and the B0 recount's four required Ruling 17 checks
exercise the collision and `/1` refusal. Production `/2` hash, native history
and no-adoption controls remain an open B gate; a green B0 fixture alone does
not close the class in the application.

**CERT-FEAS · A shape certificate omits deterministic query executability.**
The frozen `/1` evaluator certified a foil with constant `1e-300`-degree
twist, yet a subsequent `PointAt` exhausted its rational arithmetic cap and
returned Not assessed. Sweep certification preconditions against every public
finite-binary64 query path, inverse, trigonometric range reduction, rounding
and result conversion; a sampled grid cannot quantify the full domain.
Derive a whole-domain bound tied to actual operations and caps, or refuse
before certification. Prevent: Owner Ruling 18 is an always-read B admission
floor; a named tiny-angle counterexample plus an inspectable all-query bound
witness are required of the production B proof. This control is **open** until
the executable `/2` gate and independent science review pass.

**STORE-ALIAS · A lock key follows a path spelling rather than a directory entry.**
On the measured Mac volume, case-equivalent target names could acquire
different raw-spelling overwrite claims for the same file. Sweep case,
Unicode and parent-path aliases plus noncooperating-writer limits across
filesystem publication paths. Derive a single fixed reserved claim relative
to the held actual parent directory, with owned cleanup before final
directory durability and no CAS claim. Prevent: Ruling 19's fixed-claim
contract and production `Store_EquivalentCaseAlias_CannotAcquireSecondWriterClaim`
plus root's 23-assertion frozen store composition probe cover the observed
Mac recurrence. Windows and final integrated store acceptance remain open.

**CO-UI · A native adapter packet names UI outcomes but omits its review workflow.**
The first provisional C packet required keyboard, accessibility and token
proof, yet did not bind the author to `$implement` and the triggered
`$ui-design` review contract or its companion lenses. Sweep: native adapter
assignments that mention a prior mockup or design tokens as if those alone
review the running interface. Derive: a worker packet for a user-facing native
surface must name the implementation and UI review workflows, native harness
states, independent accessibility veto, and browser-only checks that are
inapplicable. Prevent: the always-read C launch packet carries those explicit
conditions; its independent pre-dispatch review checks them against the
actual compiled brief before any adapter lease is issued.
**CO-UI recurrence · A native window diagnostic leaves the supported UI
inspection surface.** After an Avalonia `Window.Opened` event and CUA
`cgWindowNotFound`, the author made one direct read-only CoreGraphics window
query (two onscreen same-bundle windows) and attempted a System Events count
(assistive access denied) before the CUA-only tool boundary was recalled. No
file/UI mutation or AX proof followed. Sweep: all native UI visibility,
accessibility, keyboard and screenshot observations; process state, stdout,
and application-internal lifecycle instrumentation remain distinct allowed
diagnostics. Derive: a window count or framework callback cannot certify the
user-visible or accessible surface, and a denied alternate inspection route
is not an invitation to retry. Prevent: the always-read C packet now names
`cua_repl` as the UI observation path, forbids direct external UI inspection
for worker proof, and reserves independent root CUA readback as the rendered
gate. The retained CoreGraphics result is diagnostic only, while CUA window
binding remains open.

**PLAT-A recurrence · Repository tools inherit host text defaults.** The integrated
pack gate found text writes without LF selection and printing CLIs without a UTF-8
console guard, including root's new rollup regression. Sweep: seven project scripts,
their text-read siblings, and the Coordinator-owned spike/recount scripts. Derive:
repository text is UTF-8/LF; console encoding must not depend on a Windows code page.
Prevent: explicit read/write encodings and LF writes, plus the pack's guarded stream
reconfiguration. `verify-portable-text-io.py --root .` reported ten findings before
root's fixes and passed as a standalone gate afterward. The Coordinator fixed
subprocess encoding and its newly joined scripts; the integrated gate then passed
10/10 checks on the joined branch. The initial root diagnostic batch continued after the failing command, so its
final shell exit is not claimed as the gate result; standalone checks preserve it.

**PLAT-A handoff recurrence · A new tool bypasses an applicable repository gate.**
The W0 author ran local documentation and receipt controls, but omitted the
existing portable-text gate. The first integrated conductor run then refused
`tools/qualify-windows-runtime.py`: four JSON writes had host-default newlines
and its printing entry point lacked the UTF-8 console guard. Root independently
reproduced all five findings. Ruling 42's one-file correction passed the
portable-text and subprocess-UTF8 gates, 35 existing refusal controls, a new
help-exit control, and the corrected integrated 11/11 gate. The frozen old
native receipt remains bound to the old source. Prevent: a tooling handoff
must attach actual `verify-portable-text-io.py` and
`verify-subprocess-utf8.py` exit/output receipts before its author declares
ready; `check-docs.py` is not their substitute. The W0 route packet carries
this pre-handoff rule.

**TEST-ENTRY · A test intercepts process exit and runs unintended work.** A
local legacy-console probe replaced `sys.exit` while invoking `--help` on the
W0 qualifier. Argparse's intended exit was suppressed, so `main` continued
into a task-local macOS build/refusal. That run exited with 26 Windows-native
cases Not assessed and did not dispatch or qualify Windows. The fixed control
executes the CLI in a bounded child, requires exit 0 and help output, and
rejects qualification output. Future entry-point tests must preserve normal
exit semantics or isolate the process; they must not monkey-patch `sys.exit`
then infer `--help` was harmless. The unintended run and correction are
retained in the R42 author audit.

**TEST-FIXTURE-ALIAS · A shared negative fixture hides a missing identity
guard.** In the R41 visible-presentation synthetic assessor, the first
envelope-identity mutant survived: the fixture reused one mutable identity
object for the envelope and frame records, so changing the envelope also
changed every frame. A separate frame guard then rejected the receipt, hiding
the removed envelope guard. The author changed each frame identity to an
independent deep copy, added four frame-only mismatch cases, and reran the
same four wrong-result mutants. Final synthetic controls were 50/50 and all
four mutants exited 1, including the formerly surviving envelope mutant.
Prevent: a validator with multiple independently required identity surfaces
must construct independent fixture values and mutate each surface alone.
The permanent R41 controls exercise envelope and frame mismatch separately;
future single-object fixture reuse must fail a boundary-specific mutant.
This is synthetic oracle evidence only; native capture and visible timing
remain Not assessed.

**API-ACCESS · Generated SDK documentation is mistaken for a callable client API.**
The installed Avalonia 11.3.14 XML documented `TopLevel.Renderer` and
`IRendererWithCompositor.Compositor`, but the exact Desktop compile rejected the
route (`CS0103`/`CS0122`): the documented types were not accessible to this
application. Sweep the proposed rendering chain through the installed public
signatures and an actual consumer compile before basing a timing contract on it.
Derive: XML presence proves documentation, not public accessibility. Prevent:
the always-loaded check-before-use rule requires an actual Desktop consumer
build against the adopted public route before relying on an SDK symbol. The
retained R29 internal-route compile RED and public
`ElementComposition.GetElementVisual(viewport)?.Compositor` compile GREEN show
that control firing once; those historical receipts are not a future gate by
themselves. No generated-XML claim alone clears the native render endpoint or
presentation gate.

**API-TYPE · A reflected SDK property is assigned an inferred runtime type.**
The R34 focus-placement test read public Avalonia composition properties by
reflection and demanded `System.Numerics.Vector2` for `Size`. The installed
Avalonia 11.3.14 getter returns `Avalonia.Vector`; the test rejected all four
tab-focus rows before checking placement. The same wrong-family assumption
affected `AnchorPoint`, `Offset`, `Scale` and `CenterPoint`. Sweep every public
property used by a reflected SDK probe for its exact installed getter type,
including reviewer assertions. Derive: successful compilation of a generic
reflection helper does not establish the runtime property contract. Prevent:
use direct typed public getters in the Desktop consumer; retain reflection
only for the two nonpublic adorner-link fields with exact type/version refusal.
The R35 compiler-negative deliberately assigning public `Size` to
`System.Numerics.Vector2` must fail with `CS0029`, while the restored typed
consumer must compile. The author and independent reviewer both missed the
type before R34; the compiler control applies to both paths.

**EVID-RENDER · A prior render marker can impersonate completion of new work.**
The first runtime spike compared the frame object by reference without resetting
a render epoch. A previous draw of the same accepted frame could satisfy that
comparison even if the requested Refresh had not drawn. Sweep every timing
endpoint for a fresh event serial, the exact target revision/frame, source-bound
operation and draft generation, and for a later state change before completion.
Derive: identity equality is necessary but not evidence that work happened
*after* the operation began. Prevent: `NativeRenderCorrelation.Fresh` requires a
strictly greater render serial plus matching revision/frame;
`NativeRenderCorrelation.SameState` separately checks draft generation. The
targeted negative checks reject an unchanged serial, a wrong revision, a prior
frame and a stale draft generation. The native-app control observed a new edit metric,
an injected delayed Preview, Cancel, and a superseded Preview; a separate
timeout control retained `not_assessed` after late publication. These are
batch-cycle diagnostics, not display-presentation proof. Final C source and
native UI review remain open.

**EVID-ENDPOINT · A framework callback is promoted beyond what it observes.**
Avalonia's compositor `Rendered` callback can complete after a render cycle
that returned early because the render interface was not ready, or after a
caught rendering failure. Sweep the start and stop of each cold, edit, Preview
and Cancel metric through interim status, target Render invocation, compositor
batch and actual display. Derive: a successful batch callback does not prove a
successful draw or pixels presented to the user; CUA call duration is not an
application metric either. Prevent: the R29 normal-path metric names its
endpoint `fresh_target_batch_cycle_not_presentation`, reports timeout and
supersession as `not_assessed`, and omits source/hash fields from emitted facts.
The targeted serialization check rejects source/hash keys after an early
candidate emitted a source-hash prefix. Root independently verified the
known-delay and timeout controls, while actual visible 5-second/100 ms/250 ms
acceptance stays Not assessed pending a supported presentation measurement.

## FoilDSL boundary sweep — 2026-09-22

**PACK-H · Additive hook refresh duplicates a logical callback.** Revision 92 changed the
managed Python launcher, so command-string union retained both old and new callbacks.
Sweep: every Claude lifecycle event and matcher, including read/prompt/session hooks.
Derive: callback identity is event + matcher + managed script, not launcher bytes.
Prevent: `tools/check-pack-hooks.py`, invoked by `tools/check-docs.py`, was observed RED
on four duplicate targets before removing the four obsolete entries. It refuses an
empty inspected set. Project-owned hooks and settings remain untouched. Pack-source
upstream repair is separate from this consumer-side control.

**GEO-R · An independently presented control has a coupled authority.** User correction: moving
LE carried TE because the record stored LE plus chord. Sweep: both pointer maps, keyboard/numeric
editors, cage, seeds, source grammar/identity/examples, preview/history, station/area/analysis readers.
Derive: author the two absolute rails independently; chord is TE−LE. Per-index compensation is invalid
when the rails' abscissae/knots differ. Prevent: `tools/check-independent-edges.mjs` (observed RED at
1.602926 mm unwanted TE motion) asserts opposite controls, 401 samples and rendered path unchanged
for unequal six/nine-CV bases, both-coordinate pointer moves, keyboard/source/numeric editing and
history. It also rejects positive-ordinate TE curves that cross LE. Native/full geometry certification
remains separate. Rename sweeps must distinguish authored channel identifiers from physical units
and brief targets: the unchanged source/CAD regressions caught a chord-target rename and sorted
test-identifier mismatch before handoff; the v5 oracle remains an archived-contract regression.

**DATA-D · A projection omits part of its source.** Class: a fitted spline is carried as points
while its reader uses a stale knot vector. Sweep: upper/lower fitting, section editor, source
serialization, skin, cage, residual and undo snapshots. Derive: every spline record carries degree,
points and its own knots; every reader consumes that record. Prevent: `tools/check-foildsl.mjs`
asserts knot cardinality, source/record round-trip and a profile edit reaching the 3D reader;
the preserved CAD oracle recomputes residuals from the same curve operands. Archived v5 is unchanged.

**DATA-E · One authoring route bypasses acceptance.** Class: visual Apply could accept negative
chord that the text validator rejects; re-seeding could emit near-endpoint floats outside the exact
language contract. Sweep: master/section Apply, source Apply, recipe seeds, station edits and locks.
Derive: shared pre-acceptance validation, one draft, and explicit normalization only at recipe conversion.
Prevent: the source oracle rejects negative visual Apply without changing revision, rejects competing
writers, round-trips both recipe generators, and checks malformed/version/reference/resource boundaries.

**EVID-A/UI-M recurrence:** read-only preview erased residual evidence, and undo restored geometry
without that evidence. Sweep: preview save/restore and both history directions. Derive: restore complete
accepted state, including evidence, or say not recorded. Prevent: the source-preview preservation
assertion and existing CAD residual oracle both run on v6. **UI-L/UI-H2 recurrence:** source preview
microtext and source chrome consumed the reflow editor; fifteen layout/theme cells now assert readable
editor height, target/contrast floors and no window scroll, with independent screenshot inspection.

**SPEC-A recurrence:** reused acceptance IDs made cross-specification evidence ambiguous. Product
criteria now use SRC and language criteria DSL; rendered-spec checks count actual source IDs and
blocks. **NG protocol correction:** an assumed pack graph node did not exist locally; graph inventory
rejected the dangling link before derivation. The control is the existing whole-graph validator,
not an invented local knowledge node. Measured normalizations and bounded prototype limitations
remain disclosed in the review hub; no full-language or scientific conformance claim follows.

**NG-LOCAL recurrence · A local path or subcommand is constructed before inventory.**
During the R17 companion handoff, three read-only attempts named absent
`verify-application-contracts.py`, `specify/reference/flow.md`, and
`coord leader show`; each failed before a write. The installed inventory instead
contains `recount-application-contracts.py`, the inline `specify` flow, and
`coord leader who`. Sweep unknown local files, skill references and custom CLI
subcommands in coordination packets. Derive: first inventory with `rg --files`
or read the actual skill/script dispatcher, then call the discovered path or
advertised subcommand. A guessed `--help` on a custom script is not presumed
read-only until its dispatch is inspected. Prevent: the always-read R17
companion packet requires this inventory gate before its recount/render/mockup
commands; dependent reads are sequential after inventory, not batched with it.

**CO-LEASE · A source-review lease is reclaimed before the downstream merge ends.**
The R17 companion was accepted and joined into coordination, but the root
reviewer reclaimed `docs/reviews/application-core.md` while that same committed
file still had to merge into the clean core worker tree. The core commit hook
refused the staged merge. Root released only that path, and the author resumed
without a bypass. Sweep multi-tree handoffs where a reviewed source branch has
more than one destination, including generated index and audit joins. Derive:
lease lifetime follows the last downstream integration fence, not the source
author's commit or the first join. Prevent: the always-read companion packet
requires the coordinator to name every destination, check `coord check` before
each destination commit, and notify the reviewer to reclaim only after the
final clean destination HEAD and conductor gate receipt. The refusal remains a
valid control observation, not a product failure.

**CO-LEASE recurrence · A pre-join inbound inventory misses a post-join neighbor.**
During the M1 scope V16 sweep, the Coordinator claimed the inbound file list
computed before joining the attachment investigation. The official `flag`
operation then wrote that newly joined investigation's frontmatter without an
advance exact-file claim. No conflicting lease existed; the Coordinator noticed
the changed path, claimed it, and re-enumerated the full inbound set before the
next mutation. Derive: a graph inventory is valid only for the current joined
tree, not for its pre-merge parent. Prevent: the always-read coordination plan
requires a fresh `docs-graph.py` inbound inventory and exact `coord check`/
claims **after every join** and immediately before a flag sweep. Stop before
the first mutation if any new neighbor is unclaimed; restore only the generated
register frontmatter delta when `rulings` is inbound.

**CO-EXIT · A dependent join step runs after its prerequisite fails.** The core
handoff initially batched `git commit --no-edit` and conductor continuation;
the commit was refused by the live review lease, but the conductor still ran
recounts and appended an audit entry before its own commit refusal. No hook was
bypassed; the author retained both raw attempts and checked exact child-process
quiescence. Sweep merge, audit, recount, release and launch command groups where
a later mutation depends on a prior exit and state. Derive: a successful probe
or recount does not imply the merge commit exists. Prevent: the companion packet
requires separate tool boundaries and explicit exit, `HEAD`, `MERGE_HEAD`,
staged-path and owned-child readback before continuation; an interrupted join
cannot be labelled complete from its partial gate output.

**CO-ARTIFACT · A task-local package cache is mistaken for task-local build outputs.**
The first C `dotnet run` calls bound NuGet, CLI home and TMPDIR to unique
scratch, but default MSBuild `bin/obj` still appeared in five projects under
the isolated source tree. Ruling 21's path-drift stop fired; the 150 files
were inventoried and preserved. Sweep every build, restore, publish and test
entry point, including transitive project references and implicit rebuilds.
Derive: environment cache variables do not relocate MSBuild output or
intermediate paths. Prevent: the always-read C packet requires the accepted
`--artifacts-path` per-project layout, all six local cache/temp roots and a
gate assertion that no new source-tree `bin/obj` or outside-root assets appear.
The one Ruling 23 corrected build produced four distinct project outputs under
fresh task scratch while preserving the first files; the full adapter gate
must make this recurrence control executable before C handback.

**NG-LOCAL recurrence:** this C worker first attempted an absent `coord.py`
path before using installed `coord-core.py`; a Coordinator read-only command
also used a shell glob for nonexistent `*log` filenames and failed before
inspection. Root had likewise guessed a nonexisting ADR path before `rg`
inventory. During this Ruling 24 handoff, Coordinator also guessed unsupported
`claim list` and `precommit --json` forms before reading the advertised CLI;
both failed before writes. The existing inventory-first control applies:
list exact paths or inspect script dispatch before constructing a command,
and read returned receipt paths rather than guessing suffixes. The C launch
brief now names `coord-core.py` and exact receipt paths; failed guesses remain
recorded so a future gate cannot call them verified.
At final C handoff, the author also used `for path in ...` in zsh: lowercase
`path` is tied to `PATH`, so the loop body could not find `python3`. No repo or
external write occurred, and the author reran with `artifact_file`. Sweep
task-local shell variable names used around executable lookup. The always-read
CT26/no-guessing control now names this tied-variable hazard: use a task-specific
name, then check the executable resolves before a mutating loop.
During visible-presentation source review, a web text rendering of pinned
Avalonia source omitted blank lines and made the comment/initializer appear
four lines earlier than the immutable file. The Reviewer sent a correction,
then `nl -ba` on the pinned downloaded bytes confirmed the author's original
line references. The source-content finding was valid, but the line-number
objection was wrong. For exact line citations, inspect the pinned raw bytes
with a physical line-number tool; a rendered web view's line positions are
not the file's line numbers. This is a reviewer NG-local correction, not an
author defect.

**CO-DECISION-VIS · A new ruling is assumed visible in an older isolated worktree.**
Ruling 23 was recorded after the C worktree fork, so that tree's local
`docs/notes/rulings.md` did not yet contain it. The Coordinator detected this
before the worker's corrected attempt and supplied the canonical Coordinator
tree path read-only. Sweep every post-fork ruling and contract amendment before
asking a paused worker to resume. Prevent: the resume packet explicitly names
the ruling's current absolute path and verified request ID, or joins the
reviewed decision before source work; a local stale copy cannot silently act
as current authority.

**CO-LEADER · A live review wait outlasts the conductor designation.**
While R36–R37 review continued, the Coordinator missed the prescribed renewal
interval; epoch 10 expired before a later `leader renew`. No join ran during
the lapse. Sweep long review and agent waits for elapsed designation time,
not just the next planned merge. Derive: a lease held at the start of a wait
does not remain authority at its end. Prevent: read `leader who` and renew
within its 100-second cadence while work continues. The conductor's epoch
fence rejects unread or lower epochs but does **not** establish a live
designation for an expired same-epoch invocation. The project precondition
is a supported `leader renew` followed immediately by `leader who --json`
showing the expected holder, live state and epoch before every conductor
mutation; preserve that readback. After the quiet period, the
supported `leader reclaim` established epoch 11 before shared work resumed.
An expired designation is never silently renewed or treated as a clean join.
Recurrence during the Part1/M1 decision join: epoch 14 expired while the
Coordinator waited for shared documentation leases. Renewal refused; the
supported quiet-period reclaim established epoch 15. No join or mutation ran
under the lapse. The Coordinator then released epoch 15 at the genuine wait
boundary. During the R42 independent-review join, epoch 17 again expired.
The same-epoch conductor invocation passed its narrow epoch fence and reached
merge step 1; this disproved the earlier claim that that fence rejects an
expired same-epoch join. No competing leader was observed. The Coordinator
stopped, used supported quiet-period reclaim to epoch 18, and reran the
affected join under a live readback. This is a missing project-level
live-leadership precondition, not a managed conductor code deviation. A
fail-closed project preflight is implemented under Ruling 46 in
`tools/run-owned-conductor.py`. Its embedded self-test first reproduces an
epoch-only expired join, then requires zero conductor invocations for expired,
released, absent, unknown or contradictory leader/context observations.
Positive live and post-conflict continuation fixtures preserve the child's
nonzero exit and bind the checked session/epoch. The coordination plan wires
future application joins through the wrapper after independent acceptance;
until that acceptance, the explicit renew/readback remains mandatory.
This closes an invocation-control omission, not a claimed managed-tool defect.
It remains an observed preflight, not an atomic leadership lock.

R46's first implementation run also exposed a local-contract error:
`WindowsJob` requires a process argument and constructs/assigns the job in its
constructor; it has no separate `assign` method. The source signature was
re-read, the caller corrected, and the same complete self-test passed. This
is the existing NG-LOCAL class: read the concrete installed signature before
using it. The runtime fixture prevents the zero-argument construction from
silently returning; Windows execution of the reused primitive remains unverified
in this macOS-only wrapper proof.

R45 author commit `fce9759` repeated COORD-ENV: missing `AGENT_SESSION` made
the commit hook advisory. Supported explicit checks subsequently returned
allow for its five owned paths. That bounds ownership after the fact; it does
not turn the original hook into enforcement. All wrapper launches bind the
environment identity, and all commit shells must still prefix Git mutations.

**TEST-CLI-RECEIPT · Helper-only input tests miss the actual refusal surface.**
R46's helper tests rejected nonfinite budgets, but the real CLI wrote a nonstandard
`NaN` intent and failed before its final refusal event. Root and independent
Owner reproduced exit 12 with no authority or conductor activity and an incomplete
receipt. Sweep: inspect both budget fields, positive/negative infinity, argparse
negative-value handling, both event serializers and receipt reuse. Derive: a
helper rejection is not input-to-evidence integrity. Prevent: R49 real-CLI fixtures
for all six inputs strictly decode every event, require a complete stable refusal,
prove zero leader/conductor effects and unchanged bytes on reuse. A direct strict
serializer injection and normalized duplicate-option test cover the seams the
repair changes. Invalid numbers are explicit diagnostic strings, never plausible
replacement budgets. These controls prevent malformed refusal evidence; they do
not grant live-leader or product acceptance.

**CI-ACTIONS-CONTEXT · A syntactically valid workflow uses a context where it
is unavailable.** Ruling 43's disposable Windows workflow placed three
`runner.temp` references in job-level `env`. Local checks and author,
reviewer and Owner pre-push review all missed the context-location rule.
GitHub rejected run `36097138344` before any job or artifact. The official
`actionlint` v1.7.12 task-local binary failed the frozen workflow with the
same three errors. Ruling 44 moved exactly those variables into their
consuming step environments; the same binary exited 0, and run
`36097839626` entered an actual Windows job. The prevention rule for future
workflow launches is an executable semantic `actionlint` check on exact
candidate bytes before push, in addition to YAML parsing and manual review.
This control catches context availability at the workflow key, while the
remote run remains the runtime oracle; a green lint cannot qualify native
Windows behavior.

**PACK-UIKB · A triggered skill links to a knowledge directory absent from the consuming checkout.**
The installed `ui-design` UI-T4 text references
`docs/knowledge/native-client-ui-design/`, but this project checkout does not
contain that directory; the installed native proof template and XAML token
linter do exist. Sweep triggered skill references before C dispatch, not after
UI authoring. Derive: an absent referenced path is a deployment-link gap, not
evidence that the native design standard is satisfied or absent. Prevent: the
always-read C packet requires path inventory at UI-T4 preflight, the installed
template/linter and named native proof rows, with the authoritative pack-source
knowledge read only where available. Pack deployment reconciliation remains a
separate exact-source change; no speculative local KB is created in C.

**UI-XAML · A typed XAML resource compiles but fails when the native window loads.**
The adapter gate built, tested and published the Desktop package, but the first
real app launch threw `InvalidCastException` at `MainWindow.axaml`'s section
`RowDefinition.Height`: its `SectionPanelHeight` resource was `x:Double`, while
the property requires `GridLength`. Sweep the Desktop XAML's static-resource
assignments to typed properties, including window dimensions and viewport
dimensions; the section row was the mismatched shape. Derive: source lint and
assembly compilation do not prove XAML resource conversion at window creation.
Prevent: `tools/verify-application-adapters.py` now runs the real Desktop
apphost's `native-xaml-startup-smoke` before publish and requires its
`NATIVE-STARTUP smoke-opened` marker plus exit zero. The resource is now a
`GridLength`. The original package launch is retained as RED at
`/private/tmp/cfd-c-final-ui-31t13mk0/launch-receipt.json`; the targeted
startup receipt is `/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-c-targeted-pronye03/receipts/targeted.json`.
This control proves loader startup only; rendered UI, AX and keyboard behavior
still require native inspection.

**UI-LIFETIME · A queued window callback reads a disposed document.**
In the same-process contrast fixture, closing the first native window disposed
its controller while a previously queued `Changed` callback still called
`MainWindow.Refresh`. The full stack reached `HasRecovery` and
`AuthoringSession.Snapshot`, which correctly refused `DOC-CLOSED`; the second
window remained open. Sweep every posted UI callback and asynchronous
continuation that can outlive its owning window, including a cancelled close.
Derive: unsubscription prevents future notifications but does not retract an
already queued callback. Prevent: the actual `Closed` path marks the window
closed before unsubscribing and disposing, and posted callbacks recheck that
state before reading the controller. The R37 two-window regression drains the
old callback, requires the next window to update, and verifies a dirty-close
Cancel leaves its draft live for another numeric change. Core `DOC-CLOSED`
continues to reject reads after disposal; the UI guard does not weaken it.

**UI-REVIEW-WINDOW · Review launches accumulate native windows after their proof ends.**
Serial R29/R37 review checkpoints left six exact receipt-bound old app processes
alive while the newest Dark window was under inspection; an earlier first app
had no exact ownership receipt. The user observed too many windows. Sweep every
native review launch and review-close boundary, including a CUA reattachment
that can auto-launch a previously closed bundle. Derive: a build's owned-child
quiescence does not cover separately launched GUI review instances, and a
matching bundle name alone does not authorize cleanup. Prevent: the
[review launch guard](../coordination/review-launch-guard.py) snapshots live
app PID/start/executable once, binds each to a retained exact launch receipt,
and refuses another launch unless zero apps remain or one current review is
explicitly allowlisted by receipt. Unknown or reused PID identities are
preserved and block, never killed. The launch helper invokes this guard before
copying or starting an app and records its hash and preflight state. The
simulated older-owned/allowed-current/reused-PID controls and real live-HC
refusal were observed; after each CUA close, inspect process inventory rather
than reattaching merely to verify exit, since reattachment can launch a new
window. Close only exact-owned windows through the UI after checking draft and
unsaved state; the first app was left to the user to close.

**UI-THEME-ORACLE · Token arithmetic hides runtime resource precedence and applied paint.**
The frozen Dark workbench rendered pale panels with near-white labels, and the
HighContrast window retained a light/teal palette. The verifier had combined
base and variant brush maps as if the variant won, while Avalonia resolves a
root `ResourceDictionary` key before its theme dictionary. Sweep resource
shadowing, actual template text and painted backing, focus indicators, selected
items and app-owned dialogs in every declared theme. Derive: palette values do
not prove the color pair that a control paints. Prevent: the adapter gate loads
the real XAML, rejects root keys shadowing theme keys, requires the complete
declared theme/control/state matrix, and fails closed on missing rows, unknown
opacity, backing or nonfinite contrast. The root-shadow mutation and native
Dark/HighContrast review are retained RED controls. The applied 72-row matrix
remains open until its actual receipt passes; resource-only GREEN is not a
contrast verdict.

**UI-INTERACTION-STATE-ORACLE · A resting-state contrast matrix misses a pointer state.**
The R37 72-row applied-control matrix passed its declared Light, Dark, High
Contrast and Default rows, yet root's actual High Contrast window twice hid
the selected FoilDSL tab label black-on-black during pointer hover; F6 removed
hover and restored the white label. Sweep the installed template's meaningful
enabled pointer-over, pressed, selected and focus combinations for both tab
headers, then the narrow custom-brush toolbar/modal button and navigator-list
siblings. Derive: a complete set of controls and themes is still incomplete if
the tested **state** set omits a template override. Prevent under Ruling 38:
freeze expected `(theme, control, state)` identities before execution; require
actual loaded-control foreground/painter/backdrop and 4.5:1 text or 3:1
applicable focus ratios for every declared row; fail on missing, duplicate,
wrong-state or nonfinite evidence; retain a negative with the state override
removed or the original failing source. A synthetic pseudo-class test is
styled-state evidence only; root's supported native pointer review is the
behavioral gate. This control is **pending** until the bounded author matrix
and rebuilt native review pass; no prior 72-row result clears it.

**TEST-CAPABILITY · A positional UI fixture selects a locked domain object.**
The R32 contrast fixture selected control-list index 1 and expected an editable
numeric draft. The Example's leading `cv-1` is locked by `root_mirror`, so the
product correctly refused `BeginEdit`; one fixture prerequisite concealed 70
downstream rows. Sweep test and review fixtures that equate list position with
domain editability or accepted identity. Derive: choose a target from the
authored `Editable` fact, then assert the UI item names that same rail/control
and the draft belongs to it. Prevent: the R33 matrix fixture selects by
authored editable identity, asserts the bound item and owned enabled field, and
retains a locked-target negative. Its complete-matrix receipt counts every
failed or unexecuted required row, so a prerequisite cannot manufacture a
partial PASS.

## Authoring decisions boundary sweep — 2026-09-22

**GEO-C / DATA-D recurrence:** introducing multiple profiles makes a selected-profile singleton unsafe as a
whole-wing geometry source. Sweep: profile bank, assignments, equal-x span blend, 2D inspection, 3D skin/cage,
source emit/parse, Apply/Cancel, undo/redo and retained alternatives. Derive: geometry reads assignments;
selection reads geometry. Prevent: `check-authoring-v7.mjs` requires exact geometry/source invariance while
selecting profiles, local blend reach for a fork, full-thickness reconstruction, unequal parameterization,
bank history, held t/c and explicit constrained source-thickness targets. Its missing-entry oracle was
observed RED on v6 before v7 authoring.

**UI-H2 / UI-M recurrence:** contextual controls reduced the available section canvas to a scaled 13.8 px
SVG at the reflow preset, shrinking its 24 CV targets to 2.1 px. Sweep: entry, section and dialog geometry at
the inherited viewport/theme cells. Derive: a precision editor scrolls internally at its minimum working
size rather than scaling targets below the floor. Prevent: the authoring oracle measures section targets,
contrast and shell overflow, including 640×400; the initial failure was observed before the CSS correction.
Identity/policy labels also retained old catalog text after a profile fork. The same oracle checks inspected
profile identity; all section/Properties/source projections now name the selected record and owned draft.

**UI-M recurrence:** focus restoration after picking a different curve reselected the old focused vertex.
Sweep: pointer picking, options selection and keyboard edits during an owned draft. Derive: transfer focus
before replacing the old focused subtree. Prevent: the v7 CAD oracle requires selecting elevation/twist
while retaining an unchanged TE draft and refuses a competing nudge. Native button/select Return must not
be intercepted as a global section Apply; contextual actions retain their own keyboard semantics.

**DATA-D recurrence:** a coarse equal-x table interpolated requested display points near a sharp leading
edge, creating an observed ~0.0068 chord error for a stress profile. Derive: evaluate the inverse-x curve at
each requested chord position and cache those evaluations; retain the declared sampled maximum-thickness
limit. The independent differing-parameterization probe is the control; it does not certify the continuous
kernel. Exact thickness extrema and geometry certification remain unverified production obligations.

**DATA-D / UI-M recurrence:** a profile-bank editor must derive its CV count, knot vector and valid selected
index from the inspected profile, not a previously visited profile. The source parser permits different
six/twelve-CV profile records. The authoring oracle visits both, requiring correct metadata, bounded
selection and unchanged accepted geometry/source. Draft-title and bottom-status projections are included
in active-draft reflow checks; an accepted-only screenshot misses those states.

**Test-fixture integrity:** a legacy lifecycle test directly appended station 0.7 after its earlier proposal
had already created that station. Sweep: direct station-list mutations in the CAD oracle. Derive: use the
public add operation only when absent; never corrupt the model to establish an unrelated lifecycle setup.
The full flow now runs without the observed DSL-STATION error, and source reconciliation validates emitted
records before replacing accepted text. Historical v6 remains available as its original contract oracle.

**Graph metadata recurrence:** an unregistered `follows` relation was rejected by the whole-repository docs
gate; it was corrected to the registered `relates-to`. The validator remains the preventive control.

## Coverage correction — weighted editing and linked flow evidence

The 2026-09-19 user iteration extends **SPEC-A**: the earlier mockup's profile edit was a camber fixture, its analysis condition handler only reported that conditions changed, and Results was a static pressure view. A visually populated control is not proof that the requested quantity has a compute reader. Sweep: section/outline editing, analysis inputs and metrics, simulation setup, Results field/plot/table selection, and unit conversion. Derive all views from accepted curve definitions or one selected immutable sample. Prevent: the extended `tools/check-mockup.mjs` must change weights/conditions/samples and assert the resulting geometry, load or field changes, then assert Cancel, Undo and historical isolation. A missing-control assertion was observed failing before this iteration's UI edits. Production curve and CFD validation remains a separate obligation.

**EVID-A recurrence:** after new sweeps could capture a later accepted revision, some banner/assistant/status strings still hardcoded revision 12. The geometry itself remained pinned. The sweep reached all revision labels; they now derive from the selected run snapshot. The regression must build a run after a geometry edit and compare its labels with its pinned identity. **HYG-A cleanup:** overwritten legacy condition/field handlers were removed instead of retaining a second dead implementation.

No product defects or scientific validation tests are claimed executed in this documentation/prototype task. Controls will fail the implementation gate if these fixtures or end-to-end paths are absent.

**2026-09-20 sweep (spec v1 and mockup v1).** The v1 gate found 1 Blocker and 20 Majors on revision 1.0; every one is a sentence-level control now in the spec (identity oracle per object kind, one run key, AI-06 thresholds, single home for station → profile, exclusion on the finding, ψ-aligned thickness, one e band, 41 enumerated non-happy flow edges, state · string · component table). The mockup's em-dash saturation finding is a standing, recorded deviation: the strings are the spec's fixed copy (one state, one string) and are not rewritten for cadence.

**2026-09-21 sweep (mockup v4, CAD editing views).** The operator's list of "little things" (icons, splines, free rotation, a 2D station editor, elevations for curve editing, explicit control curves) was one class — the drawing had been treated as an illustration rather than the interface — recorded as UI-J with the drawing-model rule as its control.

**2026-09-21 sweep (mockup v3, thick-client shell).** The operator's diagnosis (page scroll, a wrapping toolbar) was measured before the rebuild and became UI-H2; the layout-facet mismatch is the class, the v2 page the instance. Five layout defects found while building the shell (implicit grid rows and columns from auto-placement, a closed `<details>` menu counting toward scrollWidth, duplicate ids across navigator and toolbar) were caught by the shell oracle and the smoke run before any gate; they are recorded in `docs/notes/thick-client-shell.md` as constraints, not classes, because the oracle already fails their shape.

**2026-09-21 sweep (spec 1.1 and mockup v2).** The 1.1 delta gate found 3 Blockers and 28 Majors, all definitional; three are new classes above (SPEC-C, DATA-C, SEC-B). The mockup v2 build surfaced UI-H before any gate through the smoke run — the cheapest control fired first.
## CO-UI-READY · Launch success mistaken for review readiness

**Class and signature:** a process starts and reports `Window.Opened`, but the
actual inspection adapter cannot attach. Repeated operator foreground requests
then become an unbounded barrier for the whole execution graph. Five requests and
an approximately ten-hour wait were reported in the native workbench review.

**Why it survived:** build, process-liveness and applied-style tests did not call
the supported native AX/screenshot adapter. The process-accumulation guard correctly
prevents duplicate launches but never established attachment. A historical
`cgWindowNotFound` does not prove either missing target-bundle permission or an
off-Space window. A later reproduction affected both Workbench and System Settings;
supported CUA session reset restored the same original app PID. Internal stale
session state is inferred; the recovery boundary is observed.

**Class → sweep → derive → prevent:** the investigation swept native review
history, launcher, process guard and C contract. Confirmed siblings are the repeated
path/bundle attachment waits; process cleanup is a related but different guard.
Current CUA host permission reads and working captures rule out a blanket current
denial; temporary unique bundles also attach successfully, so their identity alone
is not a verified cause. No relevant `assume:`/`simplify:` marker in those launch
surfaces established an activation contract.

**Control:** start each review attachment with the supported CUA REPL reset and
first-call initialization, then `docs/coordination/review-attach.mjs` requires exact-title AX and a
nonempty supported screenshot, optionally uses the advertised Raise action, and
returns a bounded failure after at most three attempts/30 seconds. The executable
`tools/check-review-attach.mjs` gate was observed exiting 1 with
`NATIVE_REVIEW_BLOCKED` for the old successful-process-only receipt. The focused
test injects the historical adapter error and rejects wrong windows, incomplete
surfaces, missing capture, human-assisted, stale and timeout evidence. Injected
failure is protocol regression evidence, not a claim to reproduce the original
macOS trigger. Fresh-build observations and independent disposition belong in
[the investigation](../investigations/review-window-attach.md).

**Always-loaded rule:** batch necessary human requests once; block only dependent
nodes and schedule other ready work. Do not repeat a routine foreground request.
Do not widen permissions or substitute another UI API to clear the gate. Re-read
PID/start/executable before and after attach because CUA may relaunch an exited
app and does not expose a PID in its app handle.

**API-boundary recurrence (NG):** an unverified review persona caused an exited
launch; validating liveness before any CUA call prevents an automatic relaunch
from hiding it. The actual review parser admits designer/keyboard/screen-reader/
dense, not arbitrary prose roles. A separate helper error used realm-local
`instanceof Uint8Array` for a cross-realm screenshot Buffer. The actual byte view
was observed, a `node:vm` negative failed before repair, and the helper now checks
`ArrayBuffer.isView` plus the byte-array tag. Do not infer transport object identity
from a type declaration. Literal CUA function code is required; string code
generation is unavailable. The regression suite also exercises a changed window
after Raise and a timed-out uncancellable operation with no overlapping retry.

**CO-DECISION-WRITE · An Owner ruling mutates the designated leader tree during
an active conductor merge.** Ruling 40 was recorded through the supported
`coord decide rule next` command while the Coordinator's docs-only R39 review
merge had an unresolved generated-index conflict. The decision tool correctly
appended the authoritative ruling and audit in the designated leader checkout,
but the append appeared there as unstaged changes while the merge was open.
The independent Owner tree and its index stayed clean. This was a real
cross-session shared-checkout write window, even though no ruling bytes were
lost: the Coordinator inspected the exact append, regenerated the derived
views, staged the register and review together, and the conductor's conflict,
docs and ruling-citation gates passed.

**Class → sweep → derive → prevent:** a register-class path needs no ordinary
lease, so lease checks alone cannot serialize a decision append with a merge.
The same risk applies to future `coord decide rule next` calls and any official
writer targeting the leader tree's audit/register during conductor resolution.
The always-read [coordination plan](../coordination/application-build.md) now
requires a short shared-register write window: finish or pause the conductor
merge, tell the Owner when the designated leader tree is ready, let the ruling
append, inspect its unstaged diff and derived views, then resume the merge.
Do not transfer that write to the Owner's branch or overwrite the append to
make a generated conflict disappear. The check is the exact register/audit
diff plus `verify-ruling-citations.py` and the conductor gates; this incident
was recovered, not evidence that concurrent register writes are safe.

**EVID-WIN-STATE · A source-bound receipt validates the scenario name but not
its scenario-specific persisted state.** In the W0 Windows qualifier draft,
the common receipt validator checked fixture/host fields while its expected
state map omitted conflict preservation and foreign-file retention on cleanup
refusal. Root's disposable synthetic review and the author independently
fed a conflict row with a wrong after-hash and a cleanup-refusal row with
wrong `foreignAfter`; each case was accepted before the correction. The
overall W0 receipt was still nonpassing because unrelated native rows were
Not assessed, which could have hidden these local false accepts. Sweep the
same shape across cancel-before (target absent), write-fault owned cleanup
and every other named stateful case, not only the two reproduced rows.

**Class → sweep → derive → prevent:** a source/binary hash proves which
qualifier ran, not that each scenario's expected bytes and publication state
were asserted. The W0 author is adding case-specific pre/post/foreign byte
contracts and `--self-test` field mutations; a separate copied-source drift
negative must reject edits after the pre-build hash. The [always-read Windows
route](../coordination/windows-runtime-route.md) requires those controls
before W0 handback. This register records the confirmed draft failure class;
final repaired PASS remains subject to frozen author receipt and independent
root Data/Security/Test review. No Windows-native case is made PASS by a
synthetic validator test on macOS.

**EVID-TARGET-IDENTITY · A convenient object or host path is mistaken for the value being proved.** In R47's timing preparation, an envelope-identity negative shared the frame's mutable identity object. Removing the envelope guard left the frame guard to fail, so the deliberate mutant escaped the intended test. A separate self-process check compared anonymous C# objects by reference: equal PID/start/executable values printed identically but returned unstable. A copied fixture root used macOS's symlinked temporary-directory spelling and failed the production no-symlink rule for the wrong reason. The first dependency reader also treated an empty portable target as competing with the concrete RID-specific target. These are distinct instances of the same evidence-boundary error: a test surrogate did not preserve the real identity or representation grain.

**Class → sweep → derive → prevent:** the R47 suite now copies envelope and frame identities independently and includes frame-only mismatches; all four deliberate source mutants fail. The self-process oracle compares identity values, and its equal-value case must remain green. The temporary positive root is canonicalized while the actual package reader still refuses symlinked roots. The real deps reader selects the concrete RID and retains ambiguity/missing-asset negatives. Root independently replayed 78 pure controls and four mutants against frozen `657cd8c`; this proves these preparation controls, not native target identity. The [proof](../proof/visible-presentation.md) keeps the initial failures and corrected outcomes.

**EVID-CLOCK-CONSUMER · A derived receipt reports a plausible empty result as success.** The standalone R47 clock-report consumer returned exit zero and `offset_intersection_ns: null` for disjoint paired brackets even though the synthetic assessor rejected the same clock contradiction. Its permanent disjoint-pair fixture now requires `VP-CLOCK`/exit 3; the raw RED/GREEN receipts are retained in the proof capsule. Root's first independent recomputation omitted the documented clock quantization and failed its own assertion; the corrected rational check includes both quanta and records `[-128/3, 42]` ns for the retained 32 pairs. The control is the executable disjoint-intersection refusal plus independent exact-unit recomputation, not prose about clocks. Neither check measures cross-process drift or a displayed frame.

**COORD-SPIRAL · A verification or coordination loop around a self-made artifact with no repair-cycle cap.** Between 2026-09-25 04:09Z and 08:56Z a Codex-led session made 131 commits across branches; zero touched `src/` or `tests/`; roughly 75% of subjects were bookkeeping (join, record, ruling, review, hold, schedule, authorize). The mechanism: ruling → author packet → independent review finds the validator false-accepts → repair ruling → repeat (R41–R56). The evidence validator became the product; no cap bounded the repair cycles, so the loop ran unbounded instead of stopping and escalating.

**Class → sweep → derive → prevent:** the class is any coordination/verification loop that keeps repairing its own evidence artifact with no cycle cap and no product-side diff. Controls: `tools/check-spiral.py` (wired into `tools/check-docs.py`) fails a branch whose commits since `main` are `>= 12`, none touch `src/` or `tests/`, and `>= 60%` of subjects match the bookkeeping vocabulary; and the AGENTS.md project section now states the 2-cycle repair cap — at the cap, stop the track and report to the operator instead of issuing another ruling.
