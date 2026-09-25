---
id: design-section-editor
title: M1.1 full section editor
type: design
status: in-review
owner: "@cfd-leader-fbfa35dc"
phase: design
tags: [section, profile, editor, m1.1, geometry, desktop]
links:
  - {to: spec-foildsl, rel: implements}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: architecture-application, rel: implements}
  - {to: design-application-contracts, rel: refines}
  - {to: note-m1-scope-decision, rel: depends-on}
review-by: 2026-12-25
summary: >-
  Design for the M1.1 section editor: profile control-vertex editing with shared or independent scope,
  certified multi-profile blending, and a UI-25 control-frame canvas in the desktop app. Delivered in two
  named increments (M1.1a edit core, M1.1b construction operations and thickness proposals); the editor is
  called "full" only once both land.
---

# M1.1 full section editor

## 1. Scope and increments

The scope is fixed by `docs/notes/m1-scope-decision.md` (L55–72): the product section-authoring workflow, not a plot.
It lands in two named increments. **The editor is labelled "full" only when both are merged.**

| Increment | Contents | Spec cases |
|---|---|---|
| **M1.1a Edit core** | Station card with thumbnail and **Edit section**; scope choice **Edit shared profile** / **Make independent at this station** with affected assignments and blend intervals; standalone 2D section canvas with upper/lower control frames (UI-25); pointer drag, keyboard nudge and numeric entry; fixed-vertex locks; **Keep current thickness** (default); certified multi-profile Rule A blend; Preview / Apply / Cancel; Undo / Redo; Save / Reopen; accessible vertex list while drafting | DSL-02, 03, 07, 15, 16; SRC-03, SRC-10; CAD-10; UX-01; UI-25; GEO-13 |
| **M1.1b Construction** | **Use source thickness** (explicit t/c channel proposal with residuals); Insert / Delete CV (GEO-05); Fair / Rebuild with achieved deviation (GEO-14, GEO-15); conversion residual and original-profile provenance display (GEO-08); value locks (A4.5/A4.6) | GEO-05, 07, 08, 14, 15 |

Not in scope: catalog ranking, analysis, export, alternatives/baseline workflow (separate product work per the note).

## 2. Current state (verified 2026-09-25 against `feature/takeover-integration`)

- The grammar already holds many profiles (`FoilSource.cs:340`). **`Geometry.Assess` refuses more than one** (`Geometry.cs:166-168`), and `SectionExact` evaluates one profile only (`Geometry.cs:124-135`).
- The only edit kind is the planform rail edit: `BeginRailEdit` accepts `leading|trailing` only (`AuthoringSession.cs:238`). There is no profile-CV edit and no profile patch function; `FoilSource.PatchRail` (`FoilSource.cs:168-201`) is the pattern to follow.
- Edits patch source bytes in place (no pretty-printer). Every mutating call takes an `operationId` and is memoized (`AuthoringSession.cs:197-202`).
- The desktop viewport draws 15 fixed sample points (`Viewport.cs:118-187`); no control-frame drawing exists anywhere.
- Tests are console harnesses registered with `Check(name, action)` (`tests/CfdWorkbench.Core.Tests/IdentityTests.cs:53-57`); Desktop tests run Avalonia headless. Run everything with `tools/run-tests.sh`.
- **Shared abscissa (pre-existing certification rule).** `Geometry.Assess` certifies a profile only when its upper and lower curves have the same degree, knots and CV x-coordinates, and the Rule A blend certifies adjacent profiles only on a shared abscissa basis. Decision (2026-09-25): a vertex's **y** is edited freely; an **x** edit moves the same-index vertex on the other side too (paired abscissa), and an x edit that breaks the basis shared with a neighbouring profile is refused with `DSL-GEOMETRY` and a message naming the neighbour. Certifying independent abscissae is moved to M1.1b.

## 3. Data model (settled first)

- **Aggregate: the foil document source.** The FoilDSL text is the only authority. Profiles, curves and assignments are projections of it; every edit is a source patch validated by `Geometry.Assess`. No second store of profile state exists anywhere, including the UI.
- **Entities:** *Profile*, identified by its unique name inside a document; its revision identity is the content hash already carried by `AuthoredAssignment.ProfileIdentity` (`Contracts.cs:23`).
- **Value objects:** *Curve* (degree 5, clamped knots, 6–32 CVs, per-curve vertex ids); *Assignment* (η, profile name); *SectionScope* (`Shared` | `Independent`).
- **Invariants (checked by Assess, tested):** profile sides run LE→TE; first CV is (0,0) on both sides; abscissae nondecreasing; upper strictly above lower on (0,1); `closure closed` ⇒ both TE ordinates 0; names unique and every profile referenced; assignments strictly increasing, first η=0, last η=1.
- **Grain of history:** one accepted revision = one source text (`AcceptedRow`, append-only, parent-linked). Undo/Redo move the cursor; nothing is rewritten.
- **Derive, don't store:** effective t/c comes only from the foil-wide `thickness` channel. Camber and unit-thickness shape are derived per profile. No station field stores t/c.
- **Make independent** inserts a copy of the shared profile named `<name>-i<k>` (smallest k ≥ 1 not in use), with fresh vertex ids, and repoints only the selected assignment. It is one transaction with the shape edit: Cancel removes both.

## 4. Contracts

### 4.1 Core (`src/CfdWorkbench.Core`)

```csharp
// Contracts.cs — pre-landed on the integration branch before any track starts (Owner amendment 2a)
public enum SectionScope { Shared, Independent }
public sealed record BlendInterval(double EtaStart, double EtaEnd, double RootDistanceStartMeters, double RootDistanceEndMeters);
public sealed record ScopeImpact(string Profile, SectionScope Scope, IReadOnlyList<int> AffectedAssignments, IReadOnlyList<BlendInterval> Intervals);
public sealed record ProfileVertex(string Side, string Id, double X, double Y, bool Fixed);   // Side: "upper" | "lower"
public sealed record ProfilePoint(double X, double Y);
public sealed record ProfileView(string Name, string Identity, IReadOnlyList<ProfileVertex> Upper, IReadOnlyList<ProfileVertex> Lower,
    IReadOnlyList<ProfilePoint> UpperCurve, IReadOnlyList<ProfilePoint> LowerCurve, string Closure);

// AuthoringSession.cs — new members (same guards, memoization and single-draft rule as the rail edit)
ScopeImpact DescribeScope(string profile, int assignmentIndex, SectionScope scope);
ProfileView ProfileAt(int assignmentIndex);                        // accepted state, or the open draft if it targets this assignment
SessionDraft BeginProfileEdit(string draftId, int assignmentIndex, SectionScope scope, string side, string vertexId);
SessionDraft UpdateProfileDraft(string draftId, long generation, double x, double y);   // normalized chord coordinates
// Validate / Preview / Apply / Cancel / Undo / Redo: unchanged, now accept profile drafts.

// FoilSource.cs — targeted source patches (same exact-decimal rules as PatchRail)
static byte[] PatchProfilePoint(byte[] source, string profile, string side, string vertexId, double x, double y);
static (byte[] Source, string NewProfile) MakeIndependent(byte[] source, string profile, int assignmentIndex);

// Geometry.cs
// Assess admits Profiles.Length >= 1. SectionExact/PointAt/SectionAt blend adjacent assignments by Rule A
// (foildsl.md §6, L246-255): w = (η-ηa)/(ηb-ηa); C = (1-w)Ca + w Cb; T0 = (1-w)Ta + w Tb; T = T0 / max_x T0;
// q = (x, C ± thickness(η)·T/2). The certificate must enclose max_x T0 (interval arithmetic, as for existing spans).
```

New error codes (added to the `foildsl.md` §7 table): `DSL-PROFILE-TARGET` (unknown profile, side or vertex), `DSL-PROFILE-ORDER` (abscissae would decrease), `DSL-PROFILE-CROSS` (upper not strictly above lower), and `DSL-LOCK` reused for fixed vertices (first CV on both sides; TE ordinates when closed).

### 4.2 Desktop (`src/CfdWorkbench.Desktop`)

- **`SectionCanvas.cs`** (new Avalonia `Control`): draws evaluated upper/lower curves as smooth polylines from `ProfileView.*Curve` samples and the **UI-25 control frame**: dashed control polygon, square vertices, diamond end vertices, 20 px hit circle, visible focus ring, fixed vertices drawn hollow. Input: pointer drag; arrow keys nudge 0.001 chord (Shift ×10); Tab cycles vertices; Enter opens numeric entry. Raises `VertexMoved(side, id, x, y)` and `VertexSelected(side, id)`. It holds no model state beyond the last `ProfileView` it was given. Exposes `ViewportSemantic`-style accessible text per vertex.
- **`WorkbenchController.cs`**: `DescribeScope`, `BeginSectionEdit(assignmentIndex, scope, side, vertexId)`, `UpdateSectionDraft(x, y)`; reuses `PreviewAsync` / `Apply` / `Cancel`. Fires `Changed` as today.
- **`MainWindow.axaml(.cs)`**: a station card in the Navigator (thumbnail = read-only `SectionCanvas`, profile name, station η and distance, effective t/c, **Edit section**). Edit section opens a new **Section** tab holding the scope chooser (radio: Edit shared / Make independent; lists affected assignments and blend intervals from `DescribeScope`), thickness intent (Keep current selected; Use source shown disabled with "M1.1b"), the editable `SectionCanvas`, a vertex list (accessible alternative to the canvas), numeric X/Y fields, and Preview / Apply / Cancel. The draft banner names the draft target while other stations are inspected (DSL-16).

## 5. Patterns (named, one adversarial pass)

- **Command + memento via source patch** (existing): each edit is a source patch, history is the accepted-row chain. *Simplifier:* no new undo mechanism is added. **Kept.**
- **Presentation adapter** (existing `WorkbenchController`): the UI never touches `AuthoringSession` directly. **Kept.**
- **Rejected:** a separate in-memory profile model in the UI (a second authority, violates DM derive-don't-store); a generic FoilDSL pretty-printer (large, unnecessary: targeted patches preserve formatting and exist already); a plugin system for edit kinds (speculative; two edit kinds do not justify it).

## 6. Failure modes

| Mode | Disposition | Test |
|---|---|---|
| Drag moves an abscissa past a neighbour | `DSL-PROFILE-ORDER`; Apply disabled; draft kept | Core |
| Upper crosses lower | `DSL-PROFILE-CROSS` from Assess; Apply disabled | Core |
| Fixed vertex edited | `DSL-LOCK`; canvas draws it hollow and refuses focus-edit | Core + Desktop |
| Stale generation | existing `DSL-CONFLICT` | Core |
| Competing edit while a draft is open | existing `DSL-DRAFT-OWNED`, names the draft | Core + Desktop |
| Blend certificate cannot enclose `max T0` | `GEOMETRY-CERTIFICATE-DEFECT`; never show an uncertified shape as accepted | Core |
| Make independent name collision | next free `-i<k>` | Core |

Trust boundary: none new (local files only). Telemetry: the existing session events gain `edit_kind = profile` and `scope`.

## 7. Test plan (red first per track)

- **Core:** patch round-trip is exact (DSL-05 style); editing an upper CV leaves the lower curve and every other profile byte-identical (DSL-13 analogue); local support: moving vertex *i* changes the curve only on its p+1 spans to 1e-12 (GEO-13); Make independent on the middle of three shared stations changes only that assignment and reports both neighbouring intervals (DSL-15); Cancel restores exact bytes; Apply → Undo → Redo restores source, profile bank and surface hash together (DSL-07); Rule A blend of two identical profiles equals the single-profile result; blend endpoints equal the station profiles; Save/Reopen keeps bytes (DSL-10).
- **Desktop (headless):** hit-test at 19 px hits and 21 px misses; arrow nudge moves 0.001 chord and Shift ×10; Tab order covers every editable vertex; fixed vertices refuse edits; controller flow Begin → Update → Preview → Apply leaves the accepted view updated; the draft target survives inspecting another station.
- **Command:** `tools/run-tests.sh` (build + all harnesses, real-path TMPDIR). `python3 tools/check-docs.py` for docs.

## 8. Build tracks (file ownership is exclusive)

| Track | Increment | Owns | Depends on | Harness / model |
|---|---|---|---|---|
| **T1 Core edit** | M1.1a | `FoilSource.cs` (profile patches), `AuthoringSession.cs`, `Contracts.cs` additions beyond the pre-landed records, `tests/CfdWorkbench.Core.Tests/SectionEditTests.cs` (+ one `Run()` line in `IdentityTests.cs`) | pre-landed `Contracts.cs` records | Grok `grok-4.7` |
| **T2 Blend** | M1.1a | `Geometry.cs`, `tests/CfdWorkbench.Core.Tests/BlendTests.cs` (+ one `Run()` line) | — | Grok `grok-4.7` (second session) |
| **T3 Canvas** | M1.1a | `src/CfdWorkbench.Desktop/SectionCanvas.cs`, `tests/CfdWorkbench.Desktop.Tests/SectionCanvasTests.cs` | pre-landed `Contracts.cs` records | Agy `gemini-3.8-flash-high` |
| **T4 Wiring** | M1.1a | `WorkbenchController.cs`, `MainWindow.axaml(.cs)`, `Styles.axaml`, Desktop controller tests | T1, T3 merged | Agy `gemini-3.8-flash-high` |
| **T5 Glue** | M1.1a | `docs/specs/foildsl.md` §7 error rows, fixtures under `tests/**/Fixtures`, proof note | T1 | Claude Code Sonnet |
| **T6 Construction** | M1.1b | split into B1–B6 in §9 | M1.1a | Grok / Agy |

T1, T2 and T3 run in parallel. Join rule: the Leader runs `tools/run-tests.sh` and `check-docs` on each branch and merges; one review pass; repair loops capped at 2. **T2 merges first** (Owner amendment 2b): T1's two-profile checks (`SectionEditTests.RunMultiProfile`, the Independent Apply/Undo/Redo path) cannot pass until multi-profile certification exists, so they are registered only after T1 is rebased onto merged T2.

As built (2026-09-25): T1 and T3 merged first because T1 left its two-profile checks unregistered, so its gate was green without T2; the amendment's purpose (no red gate) held. T2 then merged and the checks were registered (`532ec3d`). Follow-ups T1b (paired abscissa, `DSL-PROFILE-CROSS`) and R1 (profile-draft recovery, expand-only `RecoveryRow` fields) merged after.

## 9. M1.1b construction (design)

Every construction is a **draft** like a vertex move: Preview shows the result and its report, Apply records one undo item, Cancel leaves source, assignments and history unchanged (GEO-14). Every construction keeps the paired abscissa of §2: it changes both sides' x-coordinates identically or not at all.

| Track | What it adds | Contract | Owns | Harness / model |
|---|---|---|---|---|
| **B1 Insert / Delete CV** | Insert a knot on both sides at a chosen x (Boehm insertion, shape preserved to 1e-12 relative, GEO-05); Delete removes the paired vertex by knot removal and reports the maximum shape change; Delete is refused below p + 2 = 7 vertices with the reason | `SessionDraft BeginProfileInsert(draftId, assignmentIndex, scope, double x)`, `SessionDraft BeginProfileDelete(draftId, assignmentIndex, scope, int vertexIndex)`; report on `SessionAssessment` as `ConstructionReport(MaxDeviation, Tolerance?, VertexCount)` | `FoilSource.cs` knot patches, `AuthoringSession.cs` construction entry points, `tests/.../ConstructionTests.cs` | Grok `grok-4.7` |
| **B2 Fit solver + Fair / Rebuild** | One constrained weighted least-squares solver on fixed knots: minimise (NP − Q)ᵀW(NP − Q) + λ PᵀFP subject to A·P = b (KKT). Fair refits ordinates only (x fixed) with PreserveEnds ∈ {position, tangency, curvature}; reports achieved max deviation; Apply only when ≤ tolerance; monotone-piece count must not increase (GEO-15). Rebuild = Fair with a chosen vertex count and a new shared x distribution | `static FitResult ConstrainedFit.Solve(...)` in new `src/CfdWorkbench.Core/ConstrainedFit.cs`; `BeginProfileFair(draftId, assignmentIndex, scope, double tolerance, PreserveEnds ends)`, `BeginProfileRebuild(..., int vertexCount)` | `ConstrainedFit.cs`, `tests/.../FitTests.cs`; entry points appended to `AuthoringSession.cs` after B1 merges | Grok `grok-4.7` |
| **B3 Use source thickness** | With the edited profile's max(upper − lower) as t/c targets at the affected assignments, fit the foil-wide `thickness` channel (p = 3, 6–10 CVs) with `ConstrainedFit`, disclose the affected span and residuals, block Apply when infeasible or unassessed (A4.14). The policy is recorded on the edit receipt | `ThicknessIntent { KeepCurrent, UseSource }` parameter on `BeginProfileEdit` (default KeepCurrent, source-compatible) and `ThicknessProposal(Targets, Residuals, AffectedEta)` on the assessment | `AuthoringSession.cs` thickness path, `tests/.../ThicknessIntentTests.cs` | Grok `grok-4.7` (after B2) |
| **B4 DAT import (Fit points)** | Import a Selig/Lednicer `.dat` profile: Fit points by `ConstrainedFit` with the smallest vertex count 8–16 per side meeting the A4.6 acceptance (10 µm at local chord); store the original coordinates as provenance and the residual on the profile; the new profile is an explicit inline profile. This gives GEO-08 something real to show | `FoilSource.ImportDat(bytes, name) -> (profileBlock, residual, provenance)`; `AuthoringSession.BeginProfileImport(draftId, assignmentIndex, datBytes)` | new `src/CfdWorkbench.Core/DatImport.cs`, `tests/.../DatImportTests.cs` | Agy `gemini-3.8-flash-high` (after B2) |
| **B5 Desktop tools** | Section tab tool strip: Insert CV, Delete CV, Fair (tolerance + PreserveEnds), Rebuild (count), Import .dat; the report panel (achieved deviation, tolerance, vertex count, residual, provenance, t/c policy) shown before Apply; Use source thickness enabled | controller wrappers over B1–B4 | `WorkbenchController.cs`, `MainWindow.axaml(.cs)`, Desktop tests | Agy `gemini-3.8-flash-high` |
| **B6 Independent abscissae** | Certify Rule A blends between profiles with different x bases (enclose max_x T0 across mismatched Bernstein bases) so x edits on independent profiles stop being refused | internal to `Geometry.cs` | `Geometry.cs`, `BlendTests.cs` | Grok `grok-4.7`, last; Owner may defer it past M1.1 if the certificate is not tractable in two cycles |

Order: B1 and B2 in parallel (disjoint files; B2 appends its entry points after B1 merges). B3 and B4 after B2. B5 after B1–B4. B6 last. The editor is labelled **full** once B1–B5 are merged; B6 is the only item the Owner may move.
