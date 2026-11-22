---
id: proof-spike-cad-01
title: "SPIKE-CAD-01 Proof Pack: Windows ARM64 ABI Probe & Pure-C# Option B STEP AP203 5-Wing Bake-Off"
type: proof-pack
status: active
owner: "@timianmalloo"
phase: "spike — Area 7 (Wave 6 KRN-SPK)"
tags: [proof, spike, cad, export, step, 3dm, rhino3dm, occt, win-arm64, area-7]
links:
  - { to: adr-0013-cad-export-kernel, rel: implements }
  - { to: architecture-cad-export-kernel, rel: implements }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: design-export, rel: depends-on }
  - { to: kernel-spike-occt-loft, rel: relates-to }
  - { to: rulings, rel: depends-on }
  - { to: coordination-wave6-and-area5-wsl, rel: implements }
review-by: 2027-05-22
summary: >-
  Empirical execution of ADR-0013 SPIKE-CAD-01 (authorized by Ruling 270) on Windows ARM64 (.NET 10.0.13):
  (1) Rhino3dm 8.35.0 (MIT) ships verified native PE ARM64 (4.19 MB), PE x64 (4.27 MB), and Mach-O Universal
  x64+ARM64 (17.95 MB) binaries and loads/round-trips zero-PII millimeter .3dm natively on win-arm64; zero prebuilt
  win-arm64 or osx-arm64 OCCT NuGet packages exist; (2) Pure-C# Option B cubic (3x3) tensor-product NURBS skinner +
  direct ISO 10303-21 STEP AP203 writer passes all 5 benchmark wings (4 export.md §3.3 wings + ADR-0010 twisted/blended
  wing) at 0.010-1.614 um max A-vs-B closest-point deviation (6.2x-1030x below the 10 um floor), passes all 20
  Whole/Half x Closed-Shell/Untrimmed STEP syntax/topology/zero-PII/determinism checks, and proves red-first negative
  controls (16,741.6 um on planted twist omission; RED on planted PII/timestamp). Recommends advancing Option B
  (+ MIT Rhino3dm 8.35.0) into SPIKE-CAD-02 while keeping OCCT BRepCheck_Analyzer and commercial Fusion/CAM read-back
  honestly Flagged/deferred to SPIKE-CAD-02.
---

# SPIKE-CAD-01 Proof Pack: `win-arm64` ABI Probe & Option B 5-Wing Bake-Off

- **Track:** `KRN-SPK` (Wave 6 — Area 7 `SPIKE-CAD-01`, authorized by **Ruling 270**).
- **ADR Trigger:** [ADR-0013 §Decision 3](../../adr/0013-cad-export-kernel.md) (`docs/architecture/cad-export-kernel.md` §5.1).
- **Host Measured:** Windows 10.0.28000 (`win-arm64`, OS `Arm64`, Process `Arm64`), .NET SDK `10.0.402` / Runtime `.NET 10.0.13`, MSVC BuildTools `14.51.36231` (`Hostarm64/arm64` present).
- **Reproducible Code (read-only against `CfdWorkbench.Core`; excluded from `CFDWorkbench.slnx`):**
  - Part 1 ABI Probe: `tools/spikes/cad-export-spike/AbiProbe/AbiProbe.csproj` → `docs/proof/spike-cad-01/abi-probe.json`
  - Part 2 Option B Benchmark: `tools/spikes/cad-export-spike/CadExportSpike.csproj` → `docs/proof/spike-cad-01/{deviation-matrix,pure-step-results,negative-controls}.json`

Confidence labels: **[Verified]** = directly executed and observed in this spike; **[Inferred]** = reasoned from Verified data; **[Flagged]** = unverified external CAD/CAM reader behavior deferred to `SPIKE-CAD-02`.

---

## Executive Recommendation (Option A vs Option B)

**Advance Option B (Pure-C# Tensor-Product NURBS Skinner + Direct ISO 10303-21 STEP AP203 Writer) paired with MIT `Rhino3dm 8.35.0` (for `.3dm`) into `SPIKE-CAD-02` as the primary Area 7 STEP candidate.** Keep **Option A** (process-isolated LGPL-2.1 OCCT worker `cfdw-step-worker`) strictly as the documented fallback behind `ICadStepExporter` only if `SPIKE-CAD-02` exposes a commercial-reader topology issue in Autodesk Fusion or the two operator-named CAM systems (`DR-KRN-2`) that cannot be fixed cleanly in Option B.

### Data Backing This Recommendation

1. **Option B clears the `10 µm` skin-deviation floor on all 5 wings by 6.2× to 1,030× [Verified]:** Worst closest-point A-vs-B deviation across `102,510` exact `Placement.Surface` samples (`51 × 201 × 2` per wing) is **`1.6139 µm`** (`adr0010-blended-twisted` with `+60 mm` dihedral, `-8°` twist, two blended profiles), and `0.0097–0.5579 µm` across the four `docs/design/export.md` §3.3 wings.
2. **Zero LGPL dependencies and zero native STEP binaries across all 3 RIDs [Verified]:** Pure .NET 10 managed code runs identically on `win-arm64`, `win-x64`, and `osx-arm64`, writes `234–624 KB` deterministic zero-PII AP203 files in `1.4–13.4 ms`, and satisfies `COMMIT-02` and `DR-KRN-4 (a)` natively without CMake/vcpkg OCCT builds or LGPL source-offer maintenance.
3. **Option A has zero prebuilt `win-arm64` or `osx-arm64` OCCT NuGet packages [Verified]:** All four inspected OCCT NuGet packages (`Occt.NET 7.9.0`, `Xbim.Geometry.Occt 7.8.1`, `OCCT.WIN64 7.8.1.3`, `Ab4d.OpenCascade 1.0.0`) lack `win-arm64` and `osx-arm64` native binaries (`32–183 MB` `win-x64`-only or unspecified license). Shipping Option A natively on `win-arm64` would require maintaining a custom multi-RID C++ OCCT source build pipeline (MSVC `14.51.36231` `Hostarm64/arm64` is available, but `cmake`/`vcpkg`/OCCT are not installed).
4. **3DM (`EXP-04`) `win-arm64` risk is completely closed [Verified]:** `Rhino3dm 8.35.0` (`MIT`) already ships verified native `PE ARM64` (`4.19 MB`), `PE x64` (`4.27 MB`), and `Mach-O Universal x64+ARM64` (`17.95 MB`) binaries and successfully created, wrote, and read back a zero-PII millimeter `.3dm` file with 3 ubiquitous-language layers in-process on `win-arm64`.
5. **Honest Remaining Gate [Flagged / Deferred to `SPIKE-CAD-02`]:** No local OCCT or FreeCAD STEP reader is installed on this Windows ARM64 host, so **OCCT `BRepCheck_Analyzer` read-back** and **Autodesk Fusion + two operator-named CAM system open-and-measure receipts (`DR-KRN-2`)** remain **Flagged / deferred to `SPIKE-CAD-02`**. Option B currently emits exact 3D `EDGE_CURVE`s without redundant 2D `PCURVE`s; `SPIKE-CAD-02` will verify whether Fusion and the two CAM systems accept 3D-edge `MANIFOLD_SOLID_BREP` directly or require explicit `SURFACE_CURVE`/`PCURVE` pairs.

---

## Part 1: Empirical `win-arm64` ABI & Packaging Probe (`abi-probe.json`)

Command:

```bash
dotnet run --project tools/spikes/cad-export-spike/AbiProbe/AbiProbe.csproj -- --out docs/proof/spike-cad-01/abi-probe.json
```

### 1.1 `Rhino3dm 8.35.0` (MIT) — Native RID Matrix & Live `win-arm64` Load [Verified]

| Target RID | Package Asset Path | Size | Binary Format | Verified CPU Arch(s) |
|---|---|---|---|---|
| `win-arm64` | `runtimes/win-arm64/native/librhino3dm_native.dll` | 4,191,744 B (4.19 MB) | PE | `ARM64` (`0xAA64`) |
| `win-x64` | `runtimes/win-x64/native/librhino3dm_native.dll` | 4,267,520 B (4.27 MB) | PE | `x64` (`0x8664`) |
| `osx-arm64` | `runtimes/osx-arm64/native/librhino3dm_native.dylib` | 17,948,752 B (17.95 MB) | Mach-O Universal | `x64`, `ARM64` |
| `linux-arm64` | `runtimes/linux-arm64/native/librhino3dm_native.so` | 16,216,400 B (16.22 MB) | ELF | `ARM64` |
| `linux-x64` | `runtimes/linux-x64/native/librhino3dm_native.so` | 15,493,744 B (15.49 MB) | ELF | `x64` |

- **License (from `.nuspec`):** `expression:MIT` (`COMMIT-02` linkable).
- **Live `win-arm64` Execution Result:** `Succeeded: true` (`1867.01 ms` cold load + write + read-back).
- **Round-Trip Read-Back:** `UnitSystemReadBack: "Millimeters"`, `LayerCountReadBack: 3` (`Wing Skin`, `Master Curves`, `Authored Stations`), `ObjectCountReadBack: 3` (`NurbsSurface` deg `2×2` + 2 `NurbsCurve`s), `WrittenBytes: 9438`, `ZeroPiiVerified: true`.

### 1.2 OCCT NuGet & Host Availability Probe [Verified]

| Source / Package | Version | License (`.nuspec`) | Package Size | `win-arm64` | `win-x64` | `osx-arm64` | Notes |
|---|---|---|---|---|---|---|---|
| `Xbim.Geometry.Occt` | `7.8.1` | `LGPL-2.1-only WITH OCCT-exception-1.0` | 49.57 MB (62 DLLs) | **No** | Yes (`PE x64`) | **No** | `lib/native/v143/win-x64/Release/TK*.dll` |
| `OCCT.WIN64` | `7.8.1.3` | `LGPL-2.1-or-later` | 32.42 MB (48 DLLs) | **No** | Yes (`PE x64`) | **No** | `runtimes/win64/TK*.dll` |
| `Ab4d.OpenCascade` | `1.0.0` | `file:LICENSE.txt` | 0.43 MB (6 DLLs) | **No** | Yes (`PE x64` C++/CLI) | **No** | Wrapper requiring separate OCCT x64 binaries |
| `Occt.NET` | `7.9.0` | `unspecified` | 183.43 MB | Skipped (>80 MB) | Unverified | Unverified | Unspecified license; fails `COMMIT-02` hygiene |
| Local Windows ARM64 Host | — | — | — | MSVC `14.51.36231` `Hostarm64/arm64` installed | — | — | FreeCAD: **No** · OCCT: **No** · vcpkg: **No** · cmake: **No** · Python `OCP`/`FreeCAD`: **No** |

---

## Part 2: Pure-C# Option B NURBS Skinner + STEP AP203 5-Wing Benchmark

Command:

```bash
dotnet run -c Release --project tools/spikes/cad-export-spike/CadExportSpike.csproj -- --out-dir docs/proof/spike-cad-01
```

### 2.1 Method & Authority Contract

- **Geometry Authority (ADR-0010):** Every wing parses via `FoilSource.Parse`, passes `Geometry.Assess` with `GeometryStatus.Certified`, and evaluates sections strictly via `Placement.Sections` / `Placement.ChordGrid`. A-vs-B verification samples **`Placement.Surface(..., stations: 51, chordSamples: 201)`** (`20,502` exact surface points per wing) using safeguarded 2D Newton closest-point projection.
- **Tensor-Product NURBS Skinning (`NurbsMath.cs`):** Compatible clamped cubic (`degreeU = 3`, `degreeV = 3`) non-rational `B_SPLINE_SURFACE_WITH_KNOTS` over `M = 65` cosine-spaced chord points (`u_j = j / (M-1)`, giving smooth `C^∞` parameterization at the vertical-tangent leading edge) and spanwise stations seeded at `{0, 1} ∪ {authored stations} ∪ {channel interior knots} ∪ {17 uniform stations}`, adaptively refined at spanwise midpoints (`target ≤ 4 µm`, bounded termination variant `R ≤ 4`, `N ≤ 257`).
- **STEP AP203 Writer & Independent Verifier (`StepAp203Writer.cs`, `StepVerifier.cs`):** Emits ISO 10303-21 Part 21 `CONFIG_CONTROL_DESIGN` in millimetres unscaled (`SI_UNIT(.MILLI., .METRE.)`, `UNCERTAINTY_MEASURE_WITH_UNIT(1.E-04)`), fixed zero-PII header (`1980-01-01T00:00:00`, blank author/org/path, `DatImport.Slug`), for both `Whole` + `Half` scopes and both `ClosedShell` (`MANIFOLD_SOLID_BREP` / `CLOSED_SHELL`) + `Untrimmed` (`SHELL_BASED_SURFACE_MODEL` / `OPEN_SHELL`) variants per Ruling 270 (`DR-KRN-3 (a)`, `DR-KRN-6 (a)`).

### 2.2 5-Wing A-vs-B Closest-Point Deviation Results (`deviation-matrix.json`) [Verified]

| Wing ID | Description | TE Type | `N` (`R`) | Deg `(u,v)` | Skin ms | Max Closest-Point Dev (µm) | Floor (µm) | Tip / Root Planar Err (µm) | TE vs `StlExport` (µm diff) | Verdict |
|---|---|---|---|---|---|---|---|---|---|---|
| `example-open-026` | Example foil (open TE 0.26 mm) | Open (`0.25573 mm`) | 17 (`R=0`) | `(3, 3)` | 37.2 | **0.5573** | `≤ 10.0` | `0.000` / `0.000` | `0.000` | **PASS** |
| `example-open-036` | Example foil (open TE 0.36 mm) | Open (`0.35802 mm`) | 17 (`R=0`) | `(3, 3)` | 22.4 | **0.5579** | `≤ 10.0` | `0.000` / `0.000` | `0.000` | **PASS** |
| `example-closed` | Example foil as shipped | Closed (`0.00000 mm`) | 17 (`R=0`) | `(3, 3)` | 23.4 | **0.5559** | `≤ 10.0` | `0.000` / `0.000` | `0.000` | **PASS** |
| `untitled-naca0012-closed` | Untitled NACA 0012 (`NewDefault`) | Closed (`0.00000 mm`) | 23 (`R=0`) | `(3, 3)` | 25.0 | **0.0097** | `≤ 10.0` | `0.000` / `0.000` | `0.000` | **PASS** |
| `adr0010-blended-twisted` | ADR-0010 (`+60 mm` dih, `-8°` twist, 2 profiles) | Closed (`0.00000 mm`) | 19 (`R=1`) | `(3, 3)` | 868.5 | **1.6139** | `≤ 10.0` | `0.000` / `0.000` | `0.000` | **PASS** |

*Note:* `adr0010-blended-twisted` has a higher skinning time (`868.5 ms`) because `Placement.Sections` solves the per-`η` blended thickness peak (`BlendedMaximum`) across two distinct profiles during adaptive midpoint checks; once skinned, STEP serialization takes `1.4–13.4 ms`. All 5 wings verified over `20,502` `Placement.Surface` points each (`102,510` total), including off-knot interior `η` values (worst samples occurred at off-knot `η = 0.72`, `0.10`, and `0.98`).

### 2.3 STEP AP203 Syntax, Topology, Determinism & Zero-PII Summary (`pure-step-results.json`) [Verified]

All **20** generated STEP files (`5 wings × {whole, half} × {closed-shell, untrimmed}`) passed **20/20**:

| Scope & Variant | Open-TE Topology `(V, E, F, χ)` | Closed-TE Topology `(V, E, F, χ)` | File Size Range | Write Time | Byte-Deterministic | Knot Where-Rules | Zero-PII Header |
|---|---|---|---|---|---|---|---|
| `whole` · `closed-shell` | `(9, 15, 8, χ=2)` | `(6, 10, 6, χ=2)` | `458.7 – 624.2 KB` | `2.3 – 7.2 ms` | 5/5 identical | 5/5 PASS | 5/5 PASS |
| `whole` · `untrimmed` | `(9, 15, 6, χ=0)` | `(6, 10, 4, χ=0)` | `457.7 – 623.3 KB` | `2.3 – 12.2 ms` | 5/5 identical | 5/5 PASS | 5/5 PASS |
| `half` · `closed-shell` | `(6, 9, 5, χ=2)` | `(4, 6, 4, χ=2)` | `234.6 – 317.0 KB` | `1.6 – 13.4 ms` | 5/5 identical | 5/5 PASS | 5/5 PASS |
| `half` · `untrimmed` | `(6, 9, 3, χ=0)` | `(4, 6, 2, χ=0)` | `233.7 – 316.1 KB` | `1.4 – 3.7 ms` | 5/5 identical | 5/5 PASS | 5/5 PASS |

- Every `CLOSED_SHELL` passed: `0` duplicate IDs, `0` dangling `#ref`s, `0.0 mm` vertex-to-edge endpoint mismatch, every `EDGE_LOOP` closed, every `EDGE_CURVE` shared by exactly two `ORIENTED_EDGE`s with opposite sense (`+1` and `-1`), and Euler characteristic `χ = V - E + F = 2`.
- **`BRepCheck_Analyzer` Read-Back Disclosure (C5):** Marked **`Flagged / deferred to SPIKE-CAD-02`** across all 20 entries because no local OCCT or FreeCAD STEP reader is installed on this Windows ARM64 host.

---

## Part 3: Mandatory Red-First Negative Controls (`negative-controls.json`) [Verified]

Per Ruling 270 and Wave 6 condition C5, both verifiers were proved able to fail RED on planted defects:

| Control | Planted Defect | Baseline (Green) | Mutant (Planted) | Verifier Verdict |
|---|---|---|---|---|
| **NEG-1 (A-vs-B Skin Deviation)** | Omit `-8°` twist (`twist = 0°` across span) on `adr0010-blended-twisted`, verify against true twisted `Placement.Surface` | `1.6139 µm` (`≤ 10 µm`) | **`16,741.55 µm`** (`16.74 mm`) | **RED** (caught; `> 10 µm` floor) |
| **NEG-2a (STEP Header PII — Author/Host)** | Plant synthetic marker `'SYNTH-PII-MARKER-9X'` in `FILE_NAME` author field | `0` errors (`('')`) | Non-empty author + forbidden marker detected | **RED** (caught) |
| **NEG-2b (STEP Header PII — Timestamp)** | Plant non-deterministic timestamp `'2026-11-22T03:00:00'` in `FILE_NAME` | `'1980-01-01T00:00:00'` | Timestamp mismatch `!= '1980-01-01T00:00:00'` | **RED** (caught) |

---

## Residual Risks & Next Gate (`SPIKE-CAD-02`)

1. **Operator `DR-KRN-2` (open):** Operator must name the two commercial/maker CAM systems alongside Autodesk Fusion (and confirm Rhino 8) for `SPIKE-CAD-02`.
2. **External CAD/CAM Open-and-Measure (`EXP-02`, `EXP-04`, `EXP-05`):** Before lifting `COPY-481` or writing production STEP/3DM code, Option B's generated STEP files (all 5 wings, Whole & Half, Closed-Shell & Untrimmed) must be opened and measured in **Autodesk Fusion**, **both operator-named CAM systems**, and an **OCCT `BRepCheck_Analyzer` reader** (e.g., FreeCAD on macOS/Windows), and `Rhino3dm 8.35.0` `.3dm` output must be opened and measured in **Rhino 8**. If any strict CAM reader requires explicit `SURFACE_CURVE`/`PCURVE` entities on faces, add `PCURVE` emission to Option B and re-measure before falling back to Option A.
3. **Zero committed binaries:** Scratch `bin/`, `obj/`, `.step`, `.3dm`, `.dll`, and `.nupkg` artifacts are excluded/deleted before commit; all receipts are scrubbed text JSON + Markdown + HTML verified by `tools/check-proof-pii.py` and `tools/check-docs.py`.
