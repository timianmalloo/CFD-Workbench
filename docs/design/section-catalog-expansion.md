---
id: design-section-catalog-expansion
title: "Section catalog expansion: full public-domain NACA/DTMB generators, CFDW CST hydrofoil family, and 1-click user .dat import"
type: design
status: draft
owner: "@timianmalloo"
tags: [catalog, sections, naca, dtmb, cst, desktop, ux]
links:
  - { to: design-m12d-catalog, rel: refines }
  - { to: kb-hw-public-domain-and-parametric-catalog, rel: depends-on }
  - { to: decision-catalog-admission-classes, rel: refines }
review-by: 2027-05-27
summary: >-
  Expands the bundled section catalog from 3 to 77 active GEN entries (68 public-domain NACA 4/4M/5/16/6/6A/66(MOD)
  plus 9 project-owned Eppler-inspired degree-7 Bernstein CST hydrofoil sections), generalizes Catalog.Read across
  Gen/Vend/Link classes, adds an adaptive 16-point blossomed-sqrt B-spline fallback in SectionReplace so every
  admitted section fits within 10 µm, updates NeuralFoilPolarSource.MaxCstResidual to 0.00065 c for 6-series CST
  coverage, and adds 1-click "Import .dat to My sections…" on disabled VEND/LINK rows in CatalogDialog.
---

# Section catalog expansion

## 1. Goal & E7 surface list

Eliminate the half-empty state of the section catalog while strictly honoring `ADR-0008`, `COMMIT-02`, Ruling 156, and `LAB-01`:

1. **Core math & generators (`src/CfdWorkbench.Core/`):**
   - `PureMath.cs`: pure managed IEEE-754 `Ln`, `Exp`, `SinCos`, `PowInt` using `+ - * /` and bit ops only (zero C-runtime calls, Ruling 156).
   - `Naca6Tables.cs`: verbatim public-domain NASA TM 4741 / PDAS 201-point `EPS`/`PSI` conformal-mapping tables (`63, 64, 65, 66, 67, 63A, 64A, 65A`) and quartic `ScaleFactor` polynomials.
   - `CatalogGenerators.cs`: deterministic generators for `naca4-closed/2` (byte-identical on `naca-0009`, `naca-0012`, `naca-4412`), `naca4m-closed/1` (16-series & 4-digit-modified), `naca5-closed/1`, `naca6-tm4741/1` (6- & 6A-series with Report 903 `x > 0.95` linear TE closure and `C¹` `[0.85, 0.90]` `MeanLine6M` join), `naca66mod-dtmb1780/1` (DTMB 1780 parabolic tail + `a = 0.8`), and `cfdw-cst/1` (9 project-owned degree-7 Bernstein CST hydrofoil sections).
   - `Catalog.cs`: add `CatalogFamily.Hydrofoil`; generalize `Catalog.Read` by `AdmissionClass`:
     - `Gen` (active): SHA-256 check + dispatch by `generator` ID + exact byte equality + exact `LeShift`/`Scale` + `1e-12` relative `RotationDegrees`.
     - `Vend` (active): SHA-256 check + `DatImport.Parse` validation without invoking any generator (ready for future licensed vendored files); disabled `Vend` refuses any bundled coordinate bytes (`disabled-has-coordinates`).
     - `Link`: always requires `DisabledReason` and refuses any coordinate bytes (`link-has-coordinates`).
   - `CatalogData/`: 77 generated `.dat` resources + updated `catalog.tsv` (86 rows total: 77 active `GEN` + 8 pending `VEND` Epplers + 1 cite-only `LINK` Speer H105; zero E862–E864 fairings).
   - `SectionReplace.cs`: after `OwnSqrtBasis(8..16)` scans without achieving `≤ 10 µm`, try deterministic greedy knot-insertion blossomed-sqrt 16-point degree-5 bases (still FoilDSL 4.1 valid, `≤ 16` points, exact `x(u) = u²`, preserving `CAT-RESIDUAL` on `BumpSelig()` at `> 200 µm` while bringing all 77 catalog sections under `10 µm`).
   - `DisciplinePresets.cs`: update `GenFallbackProfileId` for wingfoil/surf/downwind/parawing/pump/windfoil/kitefoil presets to purpose-matched `cfdw-*` sections while preserving `PreferredProfileId` and `FallbackReason` honesty.
2. **Analysis (`src/CfdWorkbench.Analysis/`):**
   - `NeuralFoilPolarSource.cs`: raise `MaxCstResidual` from `0.00036` (measured only on NACA 0012/2412/4412 in SPIKE-ANA-1) to `0.00065` (`0.065%` chord), verified across all 77 catalog sections (max measured `6.05 × 10⁻⁴` c on `naca-63a412`, while `badResidual` negative control `0.03` c remains `46×` above threshold).
   - `Labels.cs`: add centralized `COPY-916..923` constants (`LAB-01` clean; `COPY-921` updates the displayed CST residual limit to `6.5 × 10⁻⁴ c` to match `NeuralFoilPolarSource.MaxCstResidual = 0.00065`).
3. **Desktop (`src/CfdWorkbench.Desktop/`):**
   - `CatalogDialog.axaml` & `CatalogDialog.axaml.cs`:
     - Display `"CFDW Hydrofoil"` (`CatalogFamily.Hydrofoil`) family group alongside `"NACA"`, `"Eppler"`, `"Speer"`, `"My sections"`.
     - Add `ImportDatButton` (`IsVisible="False"`, placed in the existing footer button row so dialog height adds 0 px and stays within the 700 px 200%-scaling ceiling) visible whenever a disabled `VEND` or `LINK` row is selected.
     - Clicking `ImportDatButton` invokes an injectable file-picker seam (`Func<Task<string?>>? pickDatFile`), calls `WorkbenchController.ImportDatToMySectionsAsync(path, suggestedName)`, refreshes the `CatalogSnapshot`, re-filters, and selects the newly created `My sections` row (which immediately previews on the canvas ready for 1-click `Replace`).
   - `WorkbenchController.cs`:
     - Update `OpenCatalog` family counts (`CatalogTelemetry`) cleanly without breaking existing event fields.
     - Add `ImportDatToMySectionsAsync(string path, string? suggestedName, CancellationToken cancellation = default)` reading the user's `.dat` via bounded `UserFile.OpenRead` (1 MiB cap), fitting via `SectionReplace.Preview`, saving to `SectionLibrary` under an auto-deduplicated name with `Provenance("dat:sha256:" + hash, false)` (strictly `RightsClass.YourFile`, honoring `DOC-UNSUPPORTED-PERSISTENCE`, `LIB-IO`, `DSL-IMPORT`, `CAT-RESIDUAL`), and returning the saved `LibraryEntry`.
   - `ShellHost.cs`: wire the host file picker into `CatalogDialog` (`pickOpenFile` seam for tests / `StorageProvider` for live desktop).
4. **Copy & tokens (`DESIGN.md`):**
   - Append proposed rows `COPY-916` through `COPY-923` (zero `LAB-01` banned words, zero raw hexes, zero new theme brushes in `Styles.axaml`, zero C# `FontSize` literals).

## 2. Copy table additions (`DESIGN.md` `COPY-916..923`, proposed)

| ID | Copy | Where |
|---|---|---|
| `COPY-916` | `CFDW Hydrofoil` | Catalog family group heading for project-owned `cfdw-cst/1` sections |
| `COPY-917` | `Import .dat to My sections…` | Footer action button on `CatalogDialog` when a disabled `VEND` or `LINK` row is selected |
| `COPY-918` | `Imported “<name>” to My sections. Select Replace to apply it.` | `CatalogDialog` detail line confirmation after user `.dat` import succeeds |
| `COPY-919` | `The .dat file couldn't be read. Nothing was saved.` | `CatalogDialog` detail line when user-selected `.dat` cannot be read (`IOException` / `UnauthorizedAccessException`) |
| `COPY-920` | `Import cancelled. Nothing was saved.` | `CatalogDialog` detail line when the user cancels the `.dat` file picker |
| `COPY-921` | `Unavailable — section fit residual <value> c exceeds the limit 6.5 × 10⁻⁴ c` | `Labels.CstOverLimit` & `ReasonTexts["ANA-POLAR-NONCOMPUTABLE"]` (supersedes `COPY-321` limit) |
| `COPY-922` | `The file isn't a supported Selig or Lednicer .dat section (max 1 MiB). Nothing was saved.` | `CatalogDialog` detail line on invalid or oversized user `.dat` (`DSL-IMPORT`) |
| `COPY-923` | `Couldn't save the imported section to My sections. Nothing was saved.` | `CatalogDialog` detail line fallback on library persistence refusal |

## 3. Verification & mutant plan

- **Core tests (`CatalogTests.cs`, `SectionReplaceTests.cs`, `SetupSeedingTests.cs`):**
  - `PureMath` accuracy vs IEEE reference (`≤ 5 × 10⁻¹⁵`) plus `Exp` overflow/underflow guards,
  - Every active `GEN` entry (all 77) regenerates bit-for-bit to its recorded SHA-256 and preserves exact `naca-0009`/`0012`/`4412` golden hashes,
  - NASA TM 4741 golden ordinate check (`64A410`, `16-012`, `23012`, `66(MOD)-412`, `cfdw-surf-118`, `cfdw-mast-125`),
  - `Catalog.Read` `Vend` admission acceptance + hash tamper refusal + disabled-with-bytes refusal + `Link`-with-bytes refusal + undefined enum & unknown generator refusal,
  - `DatImport.OwnAdaptiveSqrtBasis` bounded termination and monotonic knot guarantee,
  - All 77 active `GEN` entries fit via `SectionReplace.Preview` within `10 µm` (0 `CAT-RESIDUAL`) while `BumpSelig()` still refuses with `CAT-RESIDUAL`,
  - Fairings (`862/863/864`) never listed; 8 Epplers (`Vend`) + `speer-h105` (`Link`) stay `Coordinates == null`.
- **Analysis tests (`NeuralFoilTests.cs`, `DxSectionTests.cs`):**
  - All 77 active `GEN` catalog sections pass `RunPolarResolver` + `CstFit.MaxResidual ≤ 0.00065` and produce finite `NeuralFoil` polars + finite `PanelMethod` `CpMin`, and `DxSectionTests` verifies the `6.5 × 10⁻⁴ c` limit copy.
- **Desktop tests (`CatalogDialogTests.cs`, `ControllerSectionTests.cs`):**
  - `"CFDW Hydrofoil"` heading present and selectable; `ImportDatButton` visible ONLY on disabled `VEND`/`LINK` rows (hidden on active `GEN`, `My sections`, and `Damaged` rows);
  - Clicking `ImportDatButton` with a valid `.dat` imports to `My sections`, refreshes the list, selects the new `My sections` row, previews it, and enables `Replace`;
  - Cancel / unreadable / unsupported-library / invalid-`.dat` refusals keep `My sections` and current draft untouched and show honest copy;
  - `CatalogDialog` height remains `≤ 700 px` at 200% scaling (`UiP0_F07`).
