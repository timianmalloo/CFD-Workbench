---
id: proof-cad-export-measure
title: "SPIKE-CAD-02 Operator Runbook: External CAD/CAM Open-and-Measure Release Gate"
type: proof-pack
status: active
owner: "@timianmalloo"
phase: "proof — Area 7 SPIKE-CAD-02 (Wave 8A W8-EXP)"
tags: [proof, spike, cad, export, step, 3dm, fusion, freecad, vectric, rhino, copy-481]
links:
  - { to: adr-0013-cad-export-kernel, rel: implements }
  - { to: design-export-step-3dm, rel: implements }
  - { to: proof-spike-cad-01, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2027-05-23"
summary: >-
  Operator runbook for executing ADR-0013 SPIKE-CAD-02 across Autodesk Fusion, FreeCAD CAM (Path),
  Vectric VCarve/Aspire, and Rhino 8 using generate-fixture.py and verify-open-measure.py. Explains why
  cad-open-measure.json is intentionally absent on the current build host (preserving COPY-481 honestly)
  and defines the exact schema required to unlock production STEP and 3DM export in ExportDialog.
---

# `SPIKE-CAD-02` Runbook: External CAD/CAM Open-and-Measure Receipt

**Current Host Status [Verified]:** Autodesk Fusion, FreeCAD CAM (Path), Vectric VCarve/Aspire, and Rhino 8 are **not installed** on this build host. Therefore `docs/proof/cad-export-measure/cad-open-measure.json` is **intentionally absent**, and production `ExportDialog` honestly keeps STEP and 3DM disabled behind `COPY-481` (`CadExportReleaseGate.LoadDefault()`).

## 1. Generate the Out-of-Git Fixture Set

Run on a workstation that has **Autodesk Fusion**, **FreeCAD CAM (Path)**, **Vectric VCarve/Aspire**, and/or **Rhino 8**:

```bash
python3 tools/spikes/cad-export-measure/generate-fixture.py --out-dir /tmp/cfdw-spike-cad-02
```

(Windows PowerShell: `py -3 tools/spikes/cad-export-measure/generate-fixture.py --out-dir $env:TEMP\cfdw-spike-cad-02`)

This writes:
- All **5 benchmark wings** (`example-open-026`, `example-open-036`, `example-closed`, `untitled-naca0012-closed`, `adr0010-blended-twisted`) × `{whole, half}` × `{closed, untrimmed}` `.step` and `{whole, half}` `.3dm`
- The **red-first planted defect** (`planted-defect-twist-omission/adr0010-defect-half-closed.step` and `.3dm`, omitting the `-8°` twist for a `~16.74 mm` tip-Z error)
- `expected-measurements.json` with expected chords, spans, trailing-edge thicknesses, and tolerances.

**Never commit binary `.step` or `.3dm` files into git.**

## 2. Red-First + Green Open-and-Measure Protocol (ADR-0013 §Decision 4, Ruling 273)

For each target application (`autodesk-fusion`, `freecad-cam`, `vectric-vcarve` for STEP; `rhino-8` for 3DM):

1. **Red-first planted defect:** Open `planted-defect-twist-omission/` and confirm the measured tip Z deviates from the twisted baseline by `> 10 µm` (`~16.74 mm` expected), proving the measurement workflow catches real geometry defects.
2. **Green 5-wing pass:** Open each generated wing file and verify:
   - Units imported as **millimetres, unscaled** (`unitsMmUnscaled: true`)
   - Span, root/mid/tip chord, and max thickness within **`≤ 10 µm`** of `expected-measurements.json`
   - Trailing-edge thickness within **`≤ 5 µm`**
   - Single watertight solid (`singleWatertightSolid: true` for STEP `closed`) with **`0` repair warnings**
   - Volume within **`≤ 0.1 %`** of reference
   - Zero header PII (`zeroPiiVerified: true`: timestamp `1980-01-01T00:00:00`, blank author/org/path).

## 3. Record and Validate `cad-open-measure.json`

Save the scrubbed JSON receipt (zero usernames, hostnames, or local file paths) at `docs/proof/cad-export-measure/cad-open-measure.json` with `schemaVersion: 1` and validate:

```bash
py -3 tools/spikes/cad-export-measure/verify-open-measure.py --self-test
py -3 tools/spikes/cad-export-measure/verify-open-measure.py --receipt docs/proof/cad-export-measure/cad-open-measure.json
py -3 tools/check-proof-pii.py
```

Once `verify-open-measure.py` exits `0` with all 3 STEP CAM targets and Rhino 8 green, `CadExportReleaseGate` automatically unlocks `Wing (STEP)` and `Wing (3DM)` in `ExportDialog`.
