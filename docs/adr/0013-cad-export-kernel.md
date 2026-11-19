---
id: adr-0013-cad-export-kernel
title: "ADR-0013: Area 7 CAD export kernel — Hybrid T0 architecture (owned Core authority, process-isolated OCCT STEP worker, MIT rhino3dm/openNURBS 3DM) gated on a two-track SPIKE-CAD-01 bake-off with a pure-C# STEP writer"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "architecture — Area 7 (Wave 5 KRN)"
tags: [adr, export, cad, kernel, step, 3dm, occt, rhino3dm, commit-02, exp-02, exp-04, exp-05]
links:
  - { to: architecture-cad-export-kernel, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-application-stack, rel: refines }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: adr-0012-openfoam-backend-macos, rel: relates-to }
  - { to: design-export, rel: relates-to }
  - { to: kernel-spike-occt-loft, rel: depends-on }
  - { to: threat-model, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: 2027-05-18
summary: >-
  Resolves the unselected export geometry kernel gate left open in ADR-0003 for Area 7 STEP AP203 and Rhino 3DM export
  (EXP-02, EXP-04, EXP-05, COMMIT-02). Selects a COMMIT-02-compliant Hybrid baseline (owned C# Placement authority,
  process-isolated LGPL-2.1 OCCT worker for STEP AP203 B-Rep sewing, MIT rhino3dm/openNURBS for 3DM) subject to a
  mandatory SPIKE-CAD-01 bake-off against a pure-C# STEP AP203 writer across Windows ARM64, Windows x64, and macOS
  ARM64, and gates release on the SPIKE-CAD-02 Fusion + two-CAM + Rhino open-and-measure fixture.
---

# ADR-0013: Area 7 CAD export kernel (STEP AP203 & Rhino 3DM)

- **Status:** Proposed (2026-11-19). Becomes **Accepted** only after independent Adversary review (`w5-verifier`), operator rulings on `DR-KRN-1..6`, and `SPIKE-CAD-01` exit evidence.
- **Date:** 2026-11-19
- **Deciders:** Operator (`@timianmalloo`), with independent review from Enterprise Architect, Computational Geometry, and Security & Identity Architect lenses.
- **Context architecture:** [Area 7 CAD Export Kernel Architecture](../architecture/cad-export-kernel.md) (`architecture-cad-export-kernel`).
- **Refines:** [ADR-0003: native offline composition](0003-application-stack.md) (resolves the unselected export geometry kernel clause at line 49).

---

## Context

1. **Unselected gate in ADR-0003:** [ADR-0003](0003-application-stack.md) selected C#/.NET 10 (pinned to `10.0.402` via Ruling 239) and Avalonia `11.3.14` for the offline native workbench, while explicitly leaving the export geometry kernel unselected. Export Slice A (`.dat`, watertight binary STL, 3MF) subsequently shipped in-process over `Placement.Surface` with zero CAD kernel (`docs/design/export.md`, `StlExport.cs`, `ThreeMfExport.cs`, `ExportSession.cs`), leaving STEP AP203 (`EXP-02`, `EXP-05`) disabled behind `COPY-481` and Rhino 3DM (`EXP-04`) unbuilt.
2. **Licensing boundary (`COMMIT-02`):** `docs/specs/cfd-workbench-v1.md:211-212` requires: *"permissive licences (MIT, BSD, Apache) may be linked; GPL, LGPL, AGPL and NOSA components are process-invoked or excluded; the licence register (A8.5) is a release artefact."* A8.5 (`:1382-1383`) lists `OCCT (LGPL)` as **process-only or excluded** and `rhino3dm` (MIT) as linkable. Spec 1.3 A4.12 (`:655-656`) drifted by proposing OCCT via C# P/Invoke; this ADR resolves that drift in favor of strict `COMMIT-02` compliance.
3. **One geometry authority (ADR-0010) and non-polynomial Rule A:** `src/CfdWorkbench.Core/Placement.cs` is the single placement authority for FoilDSL §6. Because Rule A applies `cos/sin(twist(η))` rotation and per-η thickness renormalization, the exact wing surface is not a polynomial tensor-product B-spline in span (`02-parametric-curves-lofts-and-surfaces.md` §5). Every STEP or 3DM NURBS surface is therefore a **derived approximation** whose A-vs-B closest-point deviation against `Placement.Surface` must be measured (`≤ 10 µm` at local chord, A4.12) and reported honestly on every write (`EXP-02`, `LAB-01`).
4. **Updated geometry and platform baselines:**
   - **Finite planar tip (Rulings 92, 93, 95):** In v1, wing tips are open planar ends with finite chord `c_tip ≥ max(5 mm, 2% c_root)`. The zero-chord tip singularity that failed uniform OCCT skinning in the 2026-09-21 spike (`docs/notes/kernel-spike-occt-loft.md`) is out of scope for v1, though knot-aligned section placement remains mandatory.
   - **Windows ARM64 platform of record (Ruling 238):** The architecture must support **Windows ARM64**, **Windows x64**, and **macOS ARM64** offline with zero cloud dependencies. Native `win-arm64` availability for both `Rhino3dm` NuGet and prebuilt OCCT libraries is currently **[Flagged]** (unverified).
5. **LOA principles in play:** Pure **T0** deterministic execution (P1, P2, P3 — Export has no assistant action per A5.12), verification over plausibility via mandatory spikes and open-and-measure fixtures (P5), atomic idempotent file publication via `ExportSession.WriteAtomicAsync` (P8), typed wire boundaries (P9), zero-PII observability (P10), and least-privilege process isolation (P11).

---

## Decision

We will adopt the following **conditional, spike-gated Hybrid T0 architecture** for Area 7 STEP AP203 and Rhino 3DM export:

1. **Single geometry authority in `CfdWorkbench.Core`:** `Placement.cs` and FoilDSL §6 remain the sole geometry authority (ADR-0010). A new Core `CadExportExtractor` generates compatible section curves placed at **every rail knot position and every authored station** (refined spanwise up to a bounded cap until A-vs-B skin deviation passes `≤ 10 µm`), and computes the always-on trailing-edge row (`Ruling 195`) and A-vs-B skin deviation. No CAD kernel is ever linked into editing or `.cfdw.json` persistence.
2. **Baseline CAD export architecture (Option A — Hybrid, `COMMIT-02` strict):**
   - **STEP AP203 (`EXP-02`, `EXP-05`):** A small, bundled, hash-verified C++ worker executable (`cfdw-step-worker`) linking **Open CASCADE Technology (OCCT, LGPL-2.1 with OCCT exception)**, invoked **strictly as an isolated subprocess via argv only** (no shell, clean minimal environment, per-run confined scratch directory, bounded 30 s timeout and OS process-tree kill, zero access to user destination paths). It builds `BRepOffsetAPI_ThruSections` (pinned `maxDegree`, v-degree read back), closes tips/TE/root, sews via `BRepBuilderAPI_Sewing`, verifies manifold validity via `BRepCheck_Analyzer`, and emits millimeter unscaled STEP AP203 (`Fusion-ready STEP (closed shell)` `MANIFOLD_SOLID_BREP` and `untrimmed surfaces` `OPEN_SHELL`).
   - **Rhino 3DM (`EXP-04`):** Uses McNeel's **MIT `rhino3dm` / `openNURBS`** library (linked in-process if `win-arm64`/`win-x64`/`osx-arm64` native binaries verify cleanly, or built as a thin MIT native library/helper if NuGet lacks `win-arm64`) to emit the loft NURBS surfaces, the 5 master curves, and authored station profiles in millimetres under ubiquitous-language layers.
   - **Publication & metadata:** All exports publish through the existing `ExportSession.WriteAtomicAsync` (Ruling 198: refuse symlinks/folders, forced extension never overwrites unconfirmed files, exclusive `.cfd-<guid>.tmp`, atomic rename), emit zero-PII headers (`1980-01-01T00:00:00` fixed timestamp, blank author/org/path, sanitized `DatImport.Slug`, mirroring `ThreeMfExport.cs` and Ruling 196 A6), display the always-on TE row and A5.6 fixed safety string, and record zero-PII `ExportTelemetry` events.
3. **Mandatory two-track `SPIKE-CAD-01` bake-off before any build (Option B upgrade clause):**
   - Before building production OCCT packaging, `SPIKE-CAD-01` (`docs/architecture/cad-export-kernel.md` §5.1) will verify `win-arm64` / `win-x64` / `osx-arm64` ABI/packaging for `rhino3dm`/`openNURBS` and OCCT **and** benchmark **Option B (a pure-C# tensor-product B-spline skinner + direct ISO 10303-21 STEP AP203 writer)** on all 4 export wings from `docs/design/export.md` §3.3 plus the ADR-0010 twisted/blended wing.
   - **Objective promotion rule:** If Option B passes `SPIKE-CAD-01` (`≤ 10 µm` A-vs-B skin deviation on all 5 wings, valid topology in read-back) **and** passes `SPIKE-CAD-02` (Autodesk Fusion + two named CAM systems with zero repair warnings and all dimensions within threshold), **Option B replaces the OCCT worker for STEP**—eliminating all LGPL dependencies and native STEP binaries across every target RID behind the identical `ICadStepExporter` seam. Otherwise Option A stands.
4. **Mandatory `SPIKE-CAD-02` release gate (`EXP-02`, `EXP-04`, `EXP-05`):** STEP export remains disabled (`COPY-481`) until `docs/proof/cad-export-measure/cad-open-measure.json` records both a red-first planted-defect failure and green open-and-measure passes (units = mm unscaled, span, root/mid/tip chord `≤ 10 µm`, max thickness `≤ 10 µm`, TE thickness `≤ 5 µm`, single watertight solid with `0` repair warnings, volume within `0.1 %`, zero header PII) in **Autodesk Fusion** and **two operator-named CAM systems** (`DR-KRN-2`). 3DM release similarly requires the **Rhino 8** open-and-measure receipt.

---

## Alternatives Considered

- **Option B alone (Pure-C# STEP + Pure-C# 3DM without OCCT or `openNURBS`):**
  - *Not selected unconditionally* because hand-emitting watertight `MANIFOLD_SOLID_BREP` STEP topology (`EDGE_CURVE`, `PCURVE`, root-mirror seam, closed-TE degeneracy vs open-TE strip) has high, unverified risk of triggering stitching/sliver warnings in strict commercial CAM/Fusion readers **[Inferred / Flagged]**, and hand-rolling binary CRC32 `.3dm` chunk tables risks Rhino version incompatibility compared to McNeel's MIT `openNURBS`. Retained as a co-spiked STEP candidate in `SPIKE-CAD-01` behind `ICadStepExporter` so empirical proof—not guesswork—decides.
- **Option C (Unified C++ sidecar linking both OCCT and `openNURBS` for STEP and 3DM):**
  - *Rejected as default* because OCCT has no native `.3dm` writer and bundling MIT `openNURBS` inside an LGPL-2.1 worker unnecessarily couples MIT 3DM export to LGPL binary packaging and distribution. Retained only as a build-pipeline fallback if `Rhino3dm` NuGet lacks `win-arm64` AND Option A is chosen for STEP.
- **Option D (Commercial kernels: Parasolid, ACIS, Spatial):**
  - *Rejected outright.* Proprietary commercial licenses, royalties, and redistribution restrictions violate `COMMIT-02` and the project's offline open-distribution model.
- **Spec 1.3 A4.12 proposal (OCCT linked in-process via C# P/Invoke under the OCCT LGPL-2.1 exception):**
  - *Rejected.* Contradicts `COMMIT-02` and A8.5 (`OCCT (LGPL)` process-only/excluded), exposes the Avalonia desktop process to native C++ heap corruption/segfaults/OOM during B-Rep sewing, and creates a copyleft-linking precedent (`DR-KRN-5` tracks amending A4.12).

---

## Consequences

### Positive
- **Strict `COMMIT-02` compliance:** Zero LGPL/GPL/AGPL/NOSA libraries linked into any `CfdWorkbench.*` binary.
- **Single geometry authority preserved:** `Placement.cs` stays authoritative (ADR-0010); `.cfdw.json` requires zero schema changes (DM7).
- **Fault and security isolation:** Native C++ B-Rep sewing (if Option A is needed) runs in a clean-env, argv-only, timeout-bounded ephemeral subprocess with zero access to user destination folders or secrets.
- **Empirical de-risking before build:** `SPIKE-CAD-01` closes `win-arm64` packaging uncertainty and tests zero-LGPL Option B before committing to a multi-platform OCCT CMake build pipeline.
- **Zero-PII & `LAB-01` continuity:** Reuses `ExportSession`, `ExportVerb`, Ruling 196 always-on TE row, fixed safety string, Ruling 198 path/symlink policy, and deterministic zero-PII headers.

### Negative / Accepted Trade-offs
- **Upfront spike investment:** Phase 0 (`SPIKE-CAD-01` + `SPIKE-CAD-02`) requires executing native ABI probes and manual/scripted open-and-measure runs in Fusion, two named CAM systems, and Rhino 8 before STEP or 3DM can ship.
- **Native packaging overhead (if Option A wins):** Bundling a minimal OCCT worker adds estimated ~20–40 MB per target RID **[Inferred / Flagged]**, cross-RID code signing/notarization, and LGPL-2.1 notice/source-offer maintenance.
- **Derived approximation floor:** Because Rule A is non-polynomial in span, STEP/3DM exports carry a measured sampled deviation (bounded `≤ 10 µm` target) rather than claiming exact analytic identity.

### Follow-ups / New Risks
- Operator rulings required on **`DR-KRN-1` through `DR-KRN-6`** (`docs/architecture/cad-export-kernel.md` §9): bake-off approval, naming the two CAM systems, scope/variant confirmation, `win-arm64` native vs emulation fallback policy, A4.12 wording reconciliation, and deterministic header timestamp policy.
- Independent Adversary review required from **`w5-verifier`** (Enterprise Architect, Computational Geometry, Security & Identity Architect) before moving status from `proposed` to `accepted`.
- After `SPIKE-CAD-01` and `SPIKE-CAD-02` pass, run `/design-slice` (including Export dialog mockup update and `DESIGN.md` copy registration) before implementing Phases 1–3.

---

## Evidence

| Source / Artifact | What It Establishes | Confidence |
|---|---|---|
| `docs/notes/kernel-spike-occt-loft.md` (2026-09-21) | OCCT 7.8.1 `ThruSections` (macOS arm64 via FreeCAD 1.1.1) reproduces Rule A within `1.1 µm` (base) and `0.5 µm` (`+6°` twist) from `N = 16` exact sections, with STEP round-trip `0.1–0.2 µm`; establishes need for knot-aligned sections and v-degree read-back. | [Verified] (macOS arm64) |
| `docs/specs/cfd-workbench-v1.md` (A2, A4.11, A4.12, A6, A8.5) & Rulings 92, 93, 95, 193–198, 203–204, 238–239 | Fixes `COMMIT-02`, `EXP-02`/`EXP-04`/`EXP-05`, `LAB-01`, finite planar open tips (`c_tip ≥ max(5 mm, 2% c_root)`), always-on TE row, zero-PII metadata, Ruling 198 path/symlink policy, and Windows ARM64 platform of record. | [Verified] |
| `src/CfdWorkbench.Core/Placement.cs`, `StlExport.cs`, `ThreeMfExport.cs`, `src/CfdWorkbench.Analysis/Export/ExportSession.cs`, `ExportCopy.cs`, `src/CfdWorkbench.Cli/ExportVerb.cs`, `src/CfdWorkbench.Desktop/ExportDialog.axaml.cs` | Confirms single placement authority, 4 verified `Certified` export wings (`docs/design/export.md` §3.3), atomic write path, telemetry ring, CLI/GUI sharing, and disabled STEP row (`COPY-481`). | [Verified] |
| `docs/knowledge/hydrofoil-workbench/02-parametric-curves-lofts-and-surfaces.md` & `05-file-formats-and-grammars.md` | Confirms non-polynomial nature of Rule A loft, `rhino3dm` MIT license and lack of loft/closest-point math, STEP `B_SPLINE_SURFACE_WITH_KNOTS` where-rules, and OpenVSP trimmed vs untrimmed solid distinction. | [Verified] (licensed/entity facts) / [Inferred] (CAM sewing) |
| `win-arm64` native binaries for `Rhino3dm` NuGet and OCCT C++ worker; pure-C# `MANIFOLD_SOLID_BREP` acceptance in Fusion & 2 CAM systems | **Not yet executed.** Closed exclusively by `SPIKE-CAD-01` and `SPIKE-CAD-02`. | [Flagged] |
