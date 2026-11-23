---
id: design-export-step-3dm
title: "Design: Export Slice B (Area 7) — STEP AP203 & Rhino 3DM"
type: design
status: active
owner: "@timianmalloo"
phase: "design — Export Slice B (Wave 8A W8-EXP)"
tags: [export, step, 3dm, cad, rhino3dm, nurbs, area-7, exp-02, exp-04, exp-05, copy-481]
links:
  - { to: adr-0013-cad-export-kernel, rel: implements }
  - { to: design-export, rel: refines }
  - { to: proof-spike-cad-01, rel: implements }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: depends-on }
review-by: "2027-05-23"
summary: >-
  Detailed design of Export Slice B (Area 7): pure-C# Option B ISO 10303-21 STEP AP203 export behind
  ICadStepExporter and pure in-memory MIT Rhino3dm 8.35.0 .3dm export over Placement.Surface (ADR-0010),
  incorporating both KRN-SPK Phase 1 refinements (channel knot mapping t_i -> eta_i = x_channel(t_i) and
  OPEN_SHELL boundary vs interior edge separation), CLI parity, and honest Desktop ExportDialog gating
  via CadExportReleaseGate preserving COPY-481 until external SPIKE-CAD-02 receipts exist.
---

# Design: Export Slice B — STEP AP203 & Rhino 3DM (`W8-EXP`)

This design refines [Export Slice A](export.md) (`design-export`) and implements [ADR-0013](../adr/0013-cad-export-kernel.md) (`adr-0013-cad-export-kernel`) following the empirical `SPIKE-CAD-01` bake-off ([`proof-spike-cad-01`](../proof/spike-cad-01/README.md)) and operator Rulings 270 and 273.

## 1. End-to-End Surface List (E7)

| Layer | Surface | Responsibility |
|---|---|---|
| Geometry authority | `src/CfdWorkbench.Core/Placement.cs` | Single FoilDSL §6 Rule A placement authority (ADR-0010); read-only. |
| Core extractor | `src/CfdWorkbench.Core/CadExportExtractor.cs` | Pure zero-I/O cubic (`3×3`) tensor-product NURBS skinner; maps channel knots `t_i -> eta_i = x_channel(t_i)`; adaptive spanwise refinement (`R <= 4`, `N <= 257`, target `4 µm`, floor `≤ 10 µm`); computes always-on TE thickness (Ruling 195) and A-vs-B closest-point deviation against `Placement.Surface`. |
| Core STEP seam & writer | `src/CfdWorkbench.Core/ICadStepExporter.cs`, `StepAp203Export.cs` | Option B pure-C# ISO 10303-21 AP203 writer (`Whole`/`Half` × `ClosedShell` `MANIFOLD_SOLID_BREP` / `Untrimmed` `OPEN_SHELL`), mm unscaled, fixed zero-PII header (`1980-01-01T00:00:00`, blank author/org/path, `DatImport.Slug`, Ruling 270 `DR-KRN-6`). |
| Core STEP verifier | `src/CfdWorkbench.Core/StepVerifier.cs` | Independent Part 21 syntax, AP203 schema, mm units, B-spline knot where-rules, zero-PII, and topological edge verifier separating boundary edges (degree 1) vs interior edges (degree 2, `+1/-1`) on `OPEN_SHELL`. |
| Core 3DM writer | `src/CfdWorkbench.Core/Rhino3dmExport.cs` | Pure in-memory MIT `Rhino3dm 8.35.0` exporter (`File3dm.ToByteArray` / `File3dm.FromByteArray`, `STORE-SUBSET` compliant), mm unscaled, 3 ubiquitous-language layers (`Wing Skin`, `Master Curves`, `Authored Stations`), zero-PII metadata, live round-trip read-back. |
| Analysis gate & session | `src/CfdWorkbench.Analysis/Export/CadExportReleaseGate.cs`, `ExportSession.cs`, `ExportCopy.cs` | Reads `docs/proof/cad-export-measure/cad-open-measure.json` (`FileShare.Read \| FileShare.Delete`), validates red-first + green receipts for Autodesk Fusion, FreeCAD CAM (Path), Vectric VCarve/Aspire (STEP) and Rhino 8 (3DM); drives `ExportSession` options, summary rows, always-on TE row, zero-PII `ExportTelemetry`, and `WriteAtomicAsync`. |
| CLI | `src/CfdWorkbench.Cli/ExportVerb.cs`, `Program.cs` | Parity for `export --format step\|3dm` (`--step-variant closed\|untrimmed`, `--scope whole\|half`) sharing `ExportSession` and hardened atomic writes (Ruling 198). |
| Desktop UI | `src/CfdWorkbench.Desktop/ExportDialog.axaml`, `ExportDialog.axaml.cs` | Keeps STEP/3DM disabled behind verbatim `COPY-481` when `CadExportReleaseGate` is gated; unlocks `Wing (STEP)` and `Wing (3DM)` with scope/variant options, always-on TE row, and measured deviation readout when verified receipts exist. |
| `SPIKE-CAD-02` harness | `tools/spikes/cad-export-measure/{generate-fixture.py,verify-open-measure.py}`, `docs/proof/cad-export-measure/README.md` | Cross-platform Python fixture generator (5 benchmark wings + planted twist-omission defect) and receipt validator with `--self-test`. |

## 2. Core Contracts & `KRN-SPK` Phase 1 Refinements

### 2.1 Channel Knot Mapping (`t_i -> eta_i = x_channel(t_i)`)
In `SPIKE-CAD-01`, spanwise stations were seeded by regex-extracting raw knot values `t_i` from FoilDSL text. When a master channel's abscissa control points are non-uniform, `x_channel(t_i) != t_i`. `CadExportExtractor.ExtractChannelKnotEtas` inspects `parsed.Definition.Curves` directly inside `CfdWorkbench.Core` and evaluates `eta_i = sum_j N_j(t_i) * Points[j][0]` via `SplineBasis.Values` across all 5 channels (`leading`, `trailing`, `dihedral`, `twist`, `thickness`), seeding every physical knot image alongside `{0, 1}`, authored stations, and a 17-station uniform floor.

### 2.2 Pure-C# STEP AP203 & `StepVerifier` Edge Separation
- `StepAp203Export` implements `ICadStepExporter` (`EngineId = "option-b-pure-csharp-ap203"`), leaving Option A (process-isolated OCCT worker) replaceable behind the same interface if external CAM testing ever finds a topology gap.
- `StepVerifier.Verify` enforces exact degree-2 opposite-sense pairing (`+1/-1`, `Euler = 2`, `BoundaryEdgeCount = 0`, `NonManifoldEdgeCount = 0`) on `CLOSED_SHELL`.
- On `OPEN_SHELL` (`StepVariant.Untrimmed`), `StepVerifier.Verify` explicitly separates **boundary edges** (`Pos + Neg == 1`) from **interior edges** (`Pos == 1 && Neg == 1`) and rejects any edge with `Pos + Neg > 2` or same-sense pairing (`+2/-0` or `+0/-2`) as `NonManifoldEdgeCount > 0`.

### 2.3 Pure In-Memory MIT `Rhino3dm 8.35.0` (`STORE-SUBSET`)
`Rhino3dm 8.35.0` (MIT, notice in `THIRD-PARTY-NOTICES.md`) supports `File3dm.ToByteArray(File3dmWriteOptions)` and `File3dm.FromByteArray(byte[])`, avoiding all filesystem I/O in `CfdWorkbench.Core` and keeping `tools/store_subset.py` (`STORE-SUBSET`) statically clean. `openNURBS` stores `n + p - 1` knots (omitting the redundant first and last Piegl & Tiller clamped knots), which `Rhino3dmExport` slices cleanly (`src.Knots[i + 1]`). Every build verifies round-trip read-back (`UnitSystem.Millimeters`, 3 layers: `Wing Skin`, `Master Curves`, `Authored Stations`, `IsValid == true`, zero PII).

## 3. Release Gate (`CadExportReleaseGate` & `COPY-481`)

Per ADR-0013 §Decision 4 and Ruling 273 (`DR-KRN-2`), `CadExportReleaseGate` reads `docs/proof/cad-export-measure/cad-open-measure.json` (when present) and requires:
- **STEP unlock:** verified red-first planted-defect failure AND green pass for all 3 targets (`autodesk-fusion`, `freecad-cam`, `vectric-vcarve`) with `unitsMmUnscaled == true`, `chordErrorUm <= 10`, `spanErrorUm <= 10`, `thicknessErrorUm <= 10`, `trailingEdgeErrorUm <= 5`, `singleWatertightSolid == true`, `repairWarnings == 0`, `volumeErrorPercent <= 0.1`, `zeroPiiVerified == true`.
- **3DM unlock:** verified red-first + green pass for `rhino-8` under the same dimensional, zero-warning, and zero-PII bounds.
- **Default state on this host:** Because none of the 4 external CAD/CAM applications are installed on this host, `cad-open-measure.json` is intentionally not present; `ExportDialog` keeps 3 format rows (`Section (.dat)`, `Wing (STL)`, `Wing (3MF)`) and disables `StepButton` with verbatim `COPY-481`.

## 4. Hard States

| Hard state | Behaviour |
|---|---|
| **Gated (default production)** | `StepButton` visible and disabled; `StepWhy` shows verbatim `COPY-481`; `FormatList` keeps 3 rows (`.dat`, STL, 3MF). |
| **Geometry not accepted (`H2`)** | `BlockedBand` shown; `Export...` disabled; `export.validate` (`EXPORT-GEOMETRY-NOT-ACCEPTED`) logged. |
| **Preparing CAD surface (`H10`)** | Async build off UI thread; skeleton rows + `Preparing the CAD surface…` (`COPY-753`); stale option changes discard obsolete builds. |
| **TE below floor (Ruling 194/195)** | Always-on TE row + `Manufacturing: not assessed`; advisory band shown with station/gap jump; never blocks export. |
| **Self-verification refusal** | `EXPORT-STEP-INVALID` or `EXPORT-3DM-INVALID` sets `ClosureBand` (`COPY-756`), logs `export.validate`, refuses save panel, writes nothing. |
| **Symlink / directory / forced-extension conflict (Ruling 198)** | Refused before write with unified stable codes (`EXPORT-TARGET-LINK`, `EXPORT-TARGET-DIRECTORY`, `EXPORT-WOULD-REPLACE`) on CLI and Dialog alike. |
| **Cancelled** | Temp `.cfd-<guid>.tmp` deleted; earlier file untouched; `COPY-511`. |
