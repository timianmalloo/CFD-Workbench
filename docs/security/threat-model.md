---
id: threat-model
title: Application security boundary review
type: threat-model
status: proposed
owner: "@cfd-owner-20260923"
phase: architecture
tags: [security, application, files]
links:
  - {to: architecture-application, rel: documents}
  - {to: design-application-foundation, rel: documents}
  - {to: design-application-contracts, rel: documents}
  - {to: design-app-shell, rel: documents}
  - {to: design-m12b-points, rel: documents}
  - {to: design-m12b2-3d-elevations, rel: documents}
  - {to: design-m12c-section-editor, rel: documents}
review-by: 2027-03-23
summary: >-
  Rolls up the offline application's file, command, rendering and telemetry threat analysis.
  Mitigations are proposed and tested only to the extent recorded in the architecture spike proof;
  filesystem race handling and distribution trust remain independent release gates.
review-suggested:
  - { by: design-application-contracts, on: 2026-09-23, reason: "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams." }
  - { by: architecture-application, on: 2026-09-23, reason: "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open." }
  - { by: design-application-foundation, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
---

# Application security review

Owner Ruling 5 assigns this authored register to the architecture author; independent approval remains
with root/Owner. The first milestone has no network listener, account, remote model or credential flow.

The B0 contract author refreshes this register under its explicit follow-on lease. Rulings 9/10 approve
bounded numeric/history/receipt policy. The new [contract proof](../proof/application-contracts.md) separates
executed session/identity/refusal checks from fixture geometry and modeled platform persistence. No native
overwrite capability or telemetry-minimization implementation is accepted through these tables.

## Trust boundaries

| Boundary | Untrusted input | Trusted action |
|---|---|---|
| File ingestion | Source/native bytes and metadata | Bounded parser, reference/hash checks and certificate admission |
| UI/CLI command | Text, draft generation, selected target | Owned command and explicit Apply |
| Filesystem | Paths, links, existing files, concurrent writer | Scoped save with exclusive claim and expected hash |
| Display/logging | Names, comments, errors, path strings | Literal native text and minimized metadata events |

## Threat register (generated)

| source | Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|---|
| [design-app-shell](../design/app-shell.md) | Layout file (user-writable) | T: crafted JSON crashes or hangs launch | mitigate | store-bounded read, version peek, 64 KiB cap, depth 8, closed DTO, source-generated STJ, per-workspace fallback | `LayoutCodec_RandomBytes_NeverThrows`; mutated fixtures → presets |
| [design-app-shell](../design/app-shell.md) | Layout file | E: type-name deserialization → code execution | mitigate | no polymorphism, no `$type`, no Dock serializer | fixture with `$type` → `LAYOUT-SCHEMA` |
| [design-app-shell](../design/app-shell.md) | Layout file | T: a float placed off-screen or over the model to hide UI | mitigate | clamp; option (a) | off-screen and oversized floats → clamped |
| [design-app-shell](../design/app-shell.md) | Layout file | I: disclosure of the user's work | mitigate | ids, sizes and screen bounds only (no display names); mode 0600; directory 0700 | codec refuses strings outside the closed sets; modes asserted |
| [design-app-shell](../design/app-shell.md) | Preference paths | T: symlink redirects a write | mitigate | `ProjectStore` refuses symlinks and checks the parent identity | symlinked directory → session-only |
| [design-app-shell](../design/app-shell.md) | Layout / recent files | D: huge file slows launch | mitigate | store cap and 64 KiB cap before parse | 65 KiB file → presets, never written if newer |
| [design-app-shell](../design/app-shell.md) | Layout / recent files | S, R | accept | single-user OS-principal files (P11); preferences need no audit; residual: another process of the same user can rewrite them, as any user file | — |
| [design-app-shell](../design/app-shell.md) | Recent file | T: an entry points to a hostile file | mitigate | opened only on explicit choice; full path in the accessible description; FoilDSL/native limits (SRC-09) apply | relative / over-long / wrong-extension entries dropped |
| [design-app-shell](../design/app-shell.md) | Crash output | I: exception text with paths to stderr / crash reports | mitigate | handlers write type + code only | `Unhandled_Exception_StderrHasNoMarkerPath` |
| [design-app-shell](../design/app-shell.md) | Dock dependency | T: a changed transitive package | mitigate | exact pins + `packages.lock.json` in locked mode; licence register (A8.5) | restore with a modified lock entry fails |
| [design-app-shell](../design/app-shell.md) | Command routing | E: a document verb runs while typing | mitigate | Edit-verb router; text-owned chords; single keys inert in fields | ⌘Z / ⌥← in fields never change the document or a float |
| [design-application-contracts](../design/application-contracts.md) | Source/native input | S/T: forged hashes/evaluator/receipt; R: unauthenticated author claims | Mitigate recomputation and exact binding/replay; operation IDs are not personal attribution; tampered receipt/hash cases |
| [design-application-contracts](../design/application-contracts.md) | Source/native input | D: exponent allocation, oversized history/escaped IDs | Mitigate preallocation scans and atomic size/line preflight; hostile-exponent and growth boundary cases |
| [design-application-contracts](../design/application-contracts.md) | Consumer → session | S/T/E: certificate/target substitution or mutated returned payload | Mitigate opaque authority, session-owned draft and defensive copies; wrong-binding/retarget/mutation cases |
| [design-application-contracts](../design/application-contracts.md) | Session → filesystem | T/E/D: ancestor/reparse race, colliding creator/temp/claim | Mitigate capability-gated native adapters and owned cleanup; model and local primitive checks only, native gate remains |
| [design-application-contracts](../design/application-contracts.md) | Display/telemetry | I/R: source/path/name disclosure or missing outcome | Mitigate literal display and bounded metadata events; production marker-scan and outcome tests required |
| [design-application-foundation](../design/application-foundation.md) | File → parser | S/T: forged source/hash or evaluator | Recompute exact hashes, strict version/grammar; tampered source never accepted |
| [design-application-foundation](../design/application-foundation.md) | File → parser | D: huge nested/number/string inputs | Enforce byte/token/count/time limits before expensive math; limits±1/fuzz |
| [design-application-foundation](../design/application-foundation.md) | Source text → UI | E/I: script or format injection | Native text values only, no eval/HTML/link execution; hostile strings remain literal |
| [design-application-foundation](../design/application-foundation.md) | Save → filesystem | T/E: symlink/path escape, external replacement | No-follow path/parent validation and scoped handle policy; link ancestor/race tests |
| [design-application-foundation](../design/application-foundation.md) | Command → Apply | S/T/R: stale/forged/duplicate write | Opaque certificate binding, operation IDs and append-only facts; mismatch fixtures |
| [design-application-foundation](../design/application-foundation.md) | Local metadata logs | I/R: source/name leakage or missing outcome | Local metadata-only events, no source/path/name; capture and scan event corpus |
| [design-m12b-points](../design/m12b-points.md) | FoilDSL file (user-writable) → parser | T: crafted `tangents` block (unknown kind, row on a missing id, very many rows) | mitigate | closed kind set; id must be an interior Anchor; existing token and count limits (`FoilSource.cs`:942, :954, :1165) | `Parse_TangentRowOnControlPoint_DslLock`, `Parse_Foil41RoundTrip_RandomRowsStable` |
| [design-m12b-points](../design/m12b-points.md) | FoilDSL file → parser | D: 16-point rails make certification slow | mitigate | 1 s proof budget; Not assessed, never a hang | `Assess_SixteenPointThreeAnchors_WorkCountBounded` |
| [design-m12b-points](../design/m12b-points.md) | Native envelope (user-writable) → reopen | T: forged `curve`, `rule` or `rail` value | mitigate | closed per-rail sets in `EditReference`; `DOC-REFERENCE` | `Reopen_CurveOnGestureReceipt_DocReference`, `Reopen_ForgedRuleValue_DocReference` |
| [design-m12b-points](../design/m12b-points.md) | Wing and Properties text fields → expression parser | D/T: huge or recursive expression | mitigate | 256 chars, depth 16, closed names | `LengthExpression_RandomText_NeverThrowsUnexpected` |
| [design-m12b-points](../design/m12b-points.md) | Telemetry ring | I: point positions or ids leak | mitigate | fields limited to counts, durations, codes and µm figures | `Telemetry_PointEdits_NoIdsOrPositions` |
| [design-m12b-points](../design/m12b-points.md) | History | R: an edit without attribution | accept | single local user; receipts name the edit kind, point and rule; the product has no identity | — |
| [design-m12b-points](../design/m12b-points.md) | — | S, E | not applicable | no authentication, privilege levels or network | — |
| [design-m12b2-3d-elevations](../design/m12b2-3d-elevations.md) | FoilDSL file → parser → display projection | D: a file whose mesh is expensive (16-point channels, many profiles) | mitigate | fixed mesh size (41 × 101 plus ≤ 32 authored stations); off-thread single-flight; cancellable; work count bounded | `Placement_Surface_EvaluatorCallsBounded` |
| [design-m12b2-3d-elevations](../design/m12b2-3d-elevations.md) | FoilDSL file → display | T: a file that certifies nothing but is drawn as if it did | mitigate | not-certified foils are dimmed "· not checked" and read-only | `ModelArea_NotCertifiedFoil_ViewsDimmedWithCaption` |
| [design-m12b2-3d-elevations](../design/m12b2-3d-elevations.md) | Native envelope → reopen | T: forged `rail`/`curve` values | mitigate | closed sets in `EditReference` | `Reopen_UnknownCurveOnReceipt_DocReference` |
| [design-m12b2-3d-elevations](../design/m12b2-3d-elevations.md) | Telemetry ring | I: point ids or positions leak | mitigate | curve family, counts, durations only | `Telemetry_ChannelEdits_CurveFamilyNoIdsOrPositions` |
| [design-m12b2-3d-elevations](../design/m12b2-3d-elevations.md) | History | R: an edit without attribution | accept | single local user; receipts name curve and point (M1.2b) | — |
| [design-m12b2-3d-elevations](../design/m12b2-3d-elevations.md) | — | S, E | not applicable | no authentication, privilege levels or network | — |
| [design-m12c-section-editor](../design/m12c-section-editor.md) | FoilDSL file → parser → certifier | T: hand-edited profile `tangents` rows that do not hold, incl. a cusp | mitigate | the profile row branch (Euclidean, with ordering conditions) | `Assess_ProfileVerticalRowHandlesSameSide_InvalidDslLock` |
| [design-m12c-section-editor](../design/m12c-section-editor.md) | FoilDSL file → certifier | D: a section crafted to exhaust the per-surface overlay | mitigate | atom budget, 32,768-bit ceiling, 1 s budget → Not assessed, never Certified | `SectionAssess_OverlayBudgetExhausted_NotAssessedNeverCertified` |
| [design-m12c-section-editor](../design/m12c-section-editor.md) | Native envelope → reopen | T: a forged `"section"` receipt or recovery | mitigate | closed `rail` set; `EditReference` / `RecoveryReference` "section" arms | `Receipt_ForgedSectionRailUnknownProfile_DocReference` |
| [design-m12c-section-editor](../design/m12c-section-editor.md) | Telemetry ring | I: profile names, point ids or positions leak | mitigate | step kind, counts, durations, codes only | `SectionTelemetry_Events_NoNamesIdsOrPositions` |
| [design-m12c-section-editor](../design/m12c-section-editor.md) | History | R: an edit without attribution | accept | single local user; the receipt names the profile | — |
| [design-m12c-section-editor](../design/m12c-section-editor.md) | — | S, E | not applicable | no authentication, privilege levels or network | — |

<!-- rolled up from 6 artifact(s) by docs-graph.py rollup on 2026-10-03 -->


## Accepted-risk register

No author-approved risk acceptance is asserted. Proposed residuals awaiting Owner disposition:

Owner accepts the cooperative-writer and OS-user ACL posture (Ruling 8). Same-user hostile processes,
uncooperative overwrite and native ancestor/reparse races remain outside any proved guarantee. Missing safe
platform capability disables publication; new-file publication requires atomic no-replace (Ruling 9).

| Residual | Proposed handling | Release gate |
|---|---|---|
| Arbitrary writer after final hash check | Declare cooperative write policy; do not promise compare-and-swap | OS-specific conflict policy and race/fault suite |
| Ancestor swap or symlink race | Handle-relative no-follow implementation, chosen directory identity | macOS and Windows real filesystem negative tests |
| Native package authenticity/distribution | Pinned packages, transitive notices/SBOM, signed application | Signing/notarization and Windows installer proof |
| Edit history has no user attribution (M1.2b receipts name the edit kind, point and blend rule only) | Proposed accept: single local user, no identity in the product (`docs/design/m12b-points.md` STRIDE-lite, R row) | None; revisit if accounts or shared projects arrive |
| Channel edits (M1.2b2: dihedral, twist, thickness) inherit the same unattributed history; an M1.2b build refuses a project holding them (`DOC-REFERENCE`, file unchanged, D-5b) | Proposed accept: single local user; forward refusal is loud and non-destructive | Owner disposition with the M1.2b row |

## Cross-cutting controls and gaps

OS-user filesystem permissions are a named platform boundary. They do not establish app-level encryption
or protect from the same user's malicious processes. Source/hash mismatches, invalid schemas and exhausted
geometry proof fail closed. No content reaches a shell/eval/network importer. Metadata events omit user
source/path/name text. The [proof](../proof/application-spikes.md) records exercised primitives; native
production I/O and complete parser fuzzing remain Not assessed. Generated tables are refreshed with:

`python3 docs/ai-forward-pack/scripts/docs-graph.py rollup --heading "Adversarial analysis (STRIDE-lite)" --type design --relative-to docs/security`
