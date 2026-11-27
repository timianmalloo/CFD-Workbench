---
id: kb-hw-public-domain-and-parametric-catalog
title: "Public-domain and project-owned parametric hydrofoil section catalog"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, sections, catalog, public-domain, naca, dtmb, cst, wingfoil]
links:
  - { to: kb-hw-foil-section-catalog, rel: refines }
  - { to: decision-catalog-admission-classes, rel: relates-to }
  - { to: design-section-catalog-expansion, rel: relates-to }
review-by: 2027-05-27
summary: >-
  Documents every public-domain (unrestricted, US Government 17 U.S.C. § 105) hydrofoil-relevant section family
  (NACA 4-digit, 4-digit-modified/16-series, 5-digit, 6/6A-series via NASA TM 4741, DTMB Report 1780 NACA 66(MOD),
  and NASA NLF/LS(1) evaluation) plus nine project-owned Eppler-inspired degree-7 Bernstein CST hydrofoil sections
  (cfdw-cst/1) tuned and benchmarked in-process for wingfoil surf, downwind, race, stabilizer, and mast/strut roles.
---

# Public-domain and project-owned parametric hydrofoil section catalog

**Compiled:** 2026-11-27 · **Refines:** [06-foil-section-catalog.md](06-foil-section-catalog.md) · **Policy:** Strict `COMMIT-02` / `A4.10` (`GEN` public-domain or project-authored only; zero unlicensed third-party coordinate bytes bundled).

Confidence labels follow the project Rigor Protocol: **Verified** = directly observed in a primary US Government source opened this session or measured by executing `CfdWorkbench.Core` + `CfdWorkbench.Analysis` (`NeuralFoilNetwork` 0.3.2 xxxlarge + `PanelMethod` 200-panel solver + `SectionReplace` + `CstFit`); **Inferred** = reasoned from Verified sources; **Flagged** = unverified or single-source.

## 1. Public-domain & licensing audit of hydrofoil section families

| Family / source | Legal status | Primary source verified | Suitable hydrofoil roles | Catalog class |
|---|---|---|---|---|
| **NACA 4-digit** (`0006`–`0018`, `1408`–`4415`) | US Government public domain (17 U.S.C. § 105) | NACA Report 460 (1933); Report 824 (1945); NASA TM 4741 (1996) *(Verified)* | Symmetric fins/struts/stabs (`0006`–`0018`); baseline cambered surf/e-foil wings (`1408`–`4415`) | **GEN** (`naca4-closed/2`) |
| **NACA 16-series & 4-digit-modified** (`16-006`–`16-021`, `16-209/309/509/212`, `0010-34`) | US Government public domain | NACA TN 1546 (1948); NASA TM X-3284 (1975); NASA TM 4741 (1996); DTNSRDC ADA032272; TU Delft Aguiar Ferreira et al. 2025 (CC BY 4.0) *(Verified)* | High-speed cavitation-resistant struts (`16-009`–`16-021`); classic marine hydrofoil/propeller foils (`16-209/309/509`); surface-piercing ventilation benchmark (`0010-34`) | **GEN** (`naca4m-closed/1`) |
| **NACA 5-digit** (`23009`, `23012`, `23015`, `23112`) | US Government public domain | NACA Report 537 (1935); Report 824 (1945); NASA TM 4741 (1996) *(Verified)* | Low-`Cm0` / near-zero-trim-drag baseline & reflex wings (`Cm0 ≈ -0.007` to `+0.004`) | **GEN** (`naca5-closed/1`) |
| **NACA 6-series & 6A-series** (`63`, `63A`, `64`, `64A`, `65`, `65A`, `66`) | US Government public domain | NACA Report 824 (1945); NACA Report 903 (Loftin 1948); NASA TM 4741 / PDAS `naca456` (`epspsi.f90`, `nacax.f90`, public domain) *(Verified)* | Broad laminar-bucket hydrofoils (`63-209/412`, `63A210/412`); DTNSRDC hydrofoil benchmark (`64A309`, `64A410`); high-speed race/downwind (`64-209`, `65-210/410`); Moth-class struts & foils (`66-009`–`66-018`, `66-209/412`) | **GEN** (`naca6-tm4741/1`) |
| **DTMB Report 1780 NACA 66(MOD) + `a = 0.8`** (`66mod-012/015/209/210/412`) | US Navy public domain | DTMB Report 1780 (Brockett 1966); Shen & Dimotakis (JFE 1989) *(Verified formula & forebody)* | Marine propeller & hydrofoil cavitation standard: parabolic aft fairing from `x/c = 0.45` eliminates cusped TE while keeping flat `a = 0.8` minimum-pressure envelope | **GEN** (`naca66mod-dtmb1780/1`) |
| **NASA NLF(1)-0416 & LS(1)-0413** | US Government public domain (NTRS `19810015487` TP-1861; `19790016789` TM X-74018) | Opened NTRS fulltext *(Verified public domain; scanned OCR noisy)* | Thick (13–16%) general-aviation airfoils; omitted from v1 `GEN` bundle because NTRS text tables have scan OCR noise and their thick aft-loaded shapes cavitate early (`< 24 kn`), whereas our exact parametric CST family covers surf/downwind/race with clean math | Not bundled in v1 (user `.dat` import supported) |
| **Project-owned CFDW Hydrofoil CST** (`cfdw-surf-*`, `cfdw-dw-*`, `cfdw-race-*`, `cfdw-stab-085`, `cfdw-mast-*`) | Project-owned (MIT / repository source) | Authored & verified in-process via `NeuralFoilNetwork` + `PanelMethod` + `SectionReplace` *(Verified)* | Purpose-tuned wingfoil surf, downwind, race, stabilizer, and mast/strut sections inspired by Eppler's rooftop cavitation-bucket philosophy, with convex low-Re recovery | **GEN** (`cfdw-cst/1`) |
| **Eppler hydrofoil set** (`E817`, `E818`, `E874`, `E904`, `E908`, `E836`, `E837`, `E838`) | Unlicensed in UIUC (`© 1994–2026`, no grant); Eppler 1990 Springer book copyrighted | Verified in [06-foil-section-catalog.md](06-foil-section-catalog.md) | Reference ship/hydrofoil benchmarks — zero bundled coordinates; 1-click user `.dat` import to `My sections` supported in `CatalogDialog` | **VEND** (Pending admission) |
| **Speer H105** | No published coordinates or redistribution licence | Verified in [06-foil-section-catalog.md](06-foil-section-catalog.md) | Low-Re sailing hydrofoil reference — zero bundled coordinates; 1-click user `.dat` import to `My sections` supported | **LINK** (Cite only) |
| **Eppler E862–E864** | 32–39% thick strut fairings, 1.2–1.6% TE gap | Verified in [06-foil-section-catalog.md](06-foil-section-catalog.md) | Out of scope (fairings, never listed in catalog) | Excluded |

## 2. NASA TM 4741 & Ruling 156 deterministic verification

All `GEN` coordinate generators execute in pure managed IEEE-754 arithmetic (`+ - * / sqrt`, bit extraction, and pinned `double.CosPi(i/80.0)`), with **zero C-runtime transcendental calls** on any coordinate-emitting path (Ruling 156):

- **Pure managed kernels (`PureMath`):** verified against IEEE-754 reference across 10,000 points:
  - `PureMath.Ln(x)` max error `1.78 × 10⁻¹⁵`
  - `PureMath.Exp(x)` max relative error `2.18 × 10⁻¹⁶`
  - `PureMath.SinCos(x)` max error `1.11 × 10⁻¹⁶` (`Sin` and `Cos`)
- **NASA TM 4741 / PDAS golden cross-check (`naca456` official `.out` files):**
  - `NACA 64A410`: max thickness difference `6.19 × 10⁻⁷` c, max camber difference `4.88 × 10⁻⁷` c (vs NASA TM 4741's stated `5 × 10⁻⁵` c tolerance).
  - `NACA 63-006`, `64-010`, `65-010`, `66-021`: max thickness difference `4.66 × 10⁻⁷` to `7.18 × 10⁻⁷` c.
- **NACA 6A trailing-edge & `MeanLine6M` continuity fixes (verified against PDAS `64a410.out`):**
  1. NACA 6A conformal tables carry `psi(200) = 0.02243 > 0`, which produces a non-physical vertical bluff turn (`dy/dx ≈ -6.07`) in the last `0.002` chord; closing `x > 0.95` linearly to `(1, 0)` matches NACA Report 903's straight-sided aftbody and matches every published table ordinate `0.00..0.95` and `1.00` to `≤ 6.2 × 10⁻⁷` c.
  2. PDAS `nacax.f90` switches `MeanLine6M` discontinuously at `x = 0.86`, creating a `0.000228` c (`27 µm` at 120 mm) vertical step jump hidden between coarse table stations `0.85` and `0.90`. A `C¹` cubic Hermite join over `[0.85, 0.90]` removes the step jump while preserving exact agreement (`4.88 × 10⁻⁷` c) at every published station `0.00..0.85, 0.90, 0.95, 1.00`.
- **DTMB Report 1780 NACA 66(MOD):** uses the exact NASA TM 4741 NACA 66 forebody (`x ≤ 0.45`, scaled to `y_t(0.45) = 0.5 t/c`), Brockett's parabolic aft fairing closed to `(1, 0)` (`y_t(x) = 0.5 t/c · (1 - ((x - 0.45)/0.55)²)` for `x > 0.45`), and the NACA `a = 0.8` mean line.

## 3. Project-owned parametric hydrofoil family (`cfdw-cst/1`)

Inspired by Eppler's hydrofoil cavitation-bucket philosophy (Eppler & Shen, JSR 1979/1981) adapted for lower water-sports Reynolds numbers (`3 × 10⁵`–`2.5 × 10⁶`) with convex pressure recovery (avoiding abrupt laminar-separation-bubble stall), nine project-owned sections are defined directly in **degree-7 Bernstein CST** (`y(x) = √x (1 - x) Σ w_i C(7,i) x^i (1 - x)^(7-i)`, closed TE, LE at `(0, 0)`, chord `1.0`):

- **Exact CST fidelity:** machine-zero CST fit residual (`5.9 × 10⁻¹⁶` to `2.8 × 10⁻¹⁵` c) inside `NeuralFoilPolarSource`.
- **Compact FoilDSL B-spline fidelity:** every section fits on **8–9 control points** (`2.41–9.24 µm` at 120 mm chord, well below the `10 µm` acceptance gate).
- **Measured native-thickness hydrodynamic benchmarks** (`NeuralFoilNetwork` 0.3.2 xxxlarge at `Ncrit = 3`, `PanelMethod` 160–200 panels, 15 °C ITTC seawater at `h = 0.50 m`, `Δp = 104,684 Pa` — computed, not tank-measured *(Verified by execution)*):

| Catalog ID | Designation | Role | `t/c` (%) | `x(t_max)` | Camber (%) | `x(c_max)` | Fit (µm / pts) | `Re` band | `Cl,max` (`Re_lo`) | Peak `L/D` (`Re_hi`) | `Cd,min` | `Cm0` | `V_crit` (kn) | Min Conf |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `cfdw-surf-118` | CFDW Surf 118 | Wingfoil surf / prone wave | 11.80 | 0.26 | 3.13 | 0.44 | 6.00 / 9 | 3.5e5–7.0e5 | 1.43 | 92.0 | 0.0081 | -0.063 | 31.2 | 0.87 |
| `cfdw-surf-105` | CFDW Surf 105 | Wingfoil surf (mid-AR) / freeride | 10.50 | 0.26 | 2.80 | 0.42 | 6.25 / 9 | 4.0e5–8.5e5 | 1.43 | 88.8 | 0.0076 | -0.054 | 32.9 | 0.88 |
| `cfdw-dw-105` | CFDW Downwind 105 | Wingfoil downwind / SUP / pump | 10.50 | 0.27 | 2.30 | 0.46 | 6.95 / 9 | 4.5e5–1.0e6 | 1.40 | 83.5 | 0.0070 | -0.049 | 33.7 | 0.95 |
| `cfdw-dw-092` | CFDW Downwind 092 | Open-ocean downwind / parawing | 9.20 | 0.27 | 1.85 | 0.46 | 9.24 / 8 | 5.5e5–1.3e6 | 1.40 | 86.7 | 0.0065 | -0.042 | 35.8 | 0.96 |
| `cfdw-race-085` | CFDW Race 085 | Wingfoil race / windfoil | 8.50 | 0.31 | 1.68 | 0.48 | 4.58 / 8 | 8.0e5–2.0e6 | 1.38 | 93.1 | 0.0057 | -0.041 | 39.8 | 0.95 |
| `cfdw-race-075` | CFDW Race 075 | High-speed wingfoil / kitefoil race | 7.50 | 0.31 | 1.43 | 0.48 | 2.48 / 8 | 1.0e6–2.5e6 | 1.28 | 93.8 | 0.0054 | -0.036 | 42.4 | 0.93 |
| `cfdw-stab-085` | CFDW Stab 085 | Stabilizer (low-`Cm0` tail) | 8.50 | 0.29 | 0.71 | 0.40 | 2.41 / 8 | 3.0e5–9.0e5 | 1.13 | 71.5 | 0.0066 | -0.014 | 41.5 | 0.96 |
| `cfdw-mast-125` | CFDW Mast 125 | Race / downwind symmetric mast | 12.50 | 0.35 | 0.00 | — | 5.70 / 8 | 8.0e5–2.2e6 | 1.26 | 80.5 | 0.0055 | 0.000 | 38.7 | 0.96 |
| `cfdw-mast-150` | CFDW Mast 150 | Structural surf / wing mast | 15.00 | 0.35 | 0.00 | — | 6.46 / 8 | 8.0e5–2.2e6 | 1.24 | 79.7 | 0.0058 | 0.000 | 35.5 | 0.96 |

### Degree-7 Bernstein CST coefficients (`cfdw-cst/1`)

| Catalog ID | Upper weights `w_U[0..7]` | Lower weights `w_L[0..7]` |
|---|---|---|
| `cfdw-surf-118` | `[0.1953, 0.2239, 0.2446, 0.2286, 0.2120, 0.1853, 0.1580, 0.1220]` | `[-0.1253, -0.1139, -0.0846, -0.0486, -0.0220, -0.0053, 0.0020, 0.0180]` |
| `cfdw-surf-105` | `[0.1719, 0.2008, 0.2213, 0.2042, 0.1870, 0.1593, 0.1315, 0.1043]` | `[-0.1119, -0.1008, -0.0713, -0.0442, -0.0170, -0.0093, -0.0015, 0.0157]` |
| `cfdw-dw-105` | `[0.1536, 0.1820, 0.2020, 0.1942, 0.1864, 0.1591, 0.1313, 0.0945]` | `[-0.1136, -0.1120, -0.0920, -0.0642, -0.0364, -0.0191, -0.0113, 0.0055]` |
| `cfdw-dw-092` | `[0.1329, 0.1610, 0.1717, 0.1635, 0.1561, 0.1386, 0.1111, 0.0843]` | `[-0.1029, -0.1010, -0.0817, -0.0635, -0.0361, -0.0186, -0.0111, 0.0057]` |
| `cfdw-race-085` | `[0.1106, 0.1382, 0.1574, 0.1582, 0.1515, 0.1347, 0.1087, 0.0827]` | `[-0.0906, -0.0882, -0.0774, -0.0682, -0.0415, -0.0247, -0.0087, 0.0073]` |
| `cfdw-race-075` | `[0.0921, 0.1196, 0.1387, 0.1396, 0.1321, 0.1155, 0.0989, 0.0732]` | `[-0.0821, -0.0796, -0.0687, -0.0596, -0.0421, -0.0255, -0.0089, 0.0068]` |
| `cfdw-stab-085` | `[0.1154, 0.1342, 0.1348, 0.1260, 0.1172, 0.0995, 0.0812, 0.0636]` | `[-0.1054, -0.1042, -0.0948, -0.0860, -0.0772, -0.0595, -0.0512, -0.0336]` |
| `cfdw-mast-125` | `[0.1303, 0.1533, 0.1687, 0.1764, 0.1610, 0.1303, 0.0997, 0.0690]` | `[-0.1303, -0.1533, -0.1687, -0.1764, -0.1610, -0.1303, -0.0997, -0.0690]` |
| `cfdw-mast-150` | `[0.1593, 0.1821, 0.2049, 0.2125, 0.1897, 0.1518, 0.1138, 0.0759]` | `[-0.1593, -0.1821, -0.2049, -0.2125, -0.1897, -0.1518, -0.1138, -0.0759]` |

## 4. Complete public-domain `GEN` catalog inventory (77 active `GEN` + 9 gated `VEND`/`LINK` = 86 entries)

- **NACA 4-digit (`naca4-closed/2`, 15 entries):** `naca-0006`, `naca-0008`, `naca-0009` *(byte-identical preserved)*, `naca-0010`, `naca-0012` *(byte-identical preserved)*, `naca-0015`, `naca-0018`, `naca-1408`, `naca-1410`, `naca-2408`, `naca-2410`, `naca-2412`, `naca-4410`, `naca-4412` *(byte-identical preserved)*, `naca-4415`.
- **NACA 16-series & 4-digit-modified (`naca4m-closed/1`, 11 entries):** `naca-16-006`, `naca-16-009`, `naca-16-012` *(unlocked)*, `naca-16-015`, `naca-16-018`, `naca-16-021`, `naca-16-209`, `naca-16-309`, `naca-16-509`, `naca-16-212`, `naca-0010-34`.
- **NACA 5-digit (`naca5-closed/1`, 4 entries):** `naca-23009`, `naca-23012`, `naca-23015`, `naca-23112`.
- **NACA 6- & 6A-series (`naca6-tm4741/1`, 33 entries):**
  - 63/63A: `naca-63-009`, `naca-63-012`, `naca-63-209` *(unlocked)*, `naca-63-210`, `naca-63-212`, `naca-63-412` *(unlocked)*, `naca-63a010`, `naca-63a012`, `naca-63a210`, `naca-63a412`.
  - 64/64A: `naca-64-009`, `naca-64-012`, `naca-64-209`, `naca-64-210`, `naca-64-412`, `naca-64a010`, `naca-64a012`, `naca-64a210`, `naca-64a309`, `naca-64a410` *(unlocked)*.
  - 65/65A: `naca-65-009`, `naca-65-012`, `naca-65-210`, `naca-65-410`, `naca-65a010`, `naca-65a210`.
  - 66: `naca-66-009`, `naca-66-012` *(unlocked)*, `naca-66-015`, `naca-66-018` *(unlocked)*, `naca-66-209` *(unlocked)*, `naca-66-210`, `naca-66-412`.
- **DTMB 1780 NACA 66(MOD) (`naca66mod-dtmb1780/1`, 5 entries):** `naca-66mod-012`, `naca-66mod-015`, `naca-66mod-209`, `naca-66mod-210`, `naca-66mod-412`.
- **CFDW Hydrofoil parametric (`cfdw-cst/1`, 9 entries):** `cfdw-surf-118`, `cfdw-surf-105`, `cfdw-dw-105`, `cfdw-dw-092`, `cfdw-race-085`, `cfdw-race-075`, `cfdw-stab-085`, `cfdw-mast-125`, `cfdw-mast-150`.
- **Gated `VEND` / `LINK` (9 entries, zero bundled coordinates):** `e817`, `e818`, `e874`, `e904`, `e908`, `e836`, `e837`, `e838` (`Vend`, pending UIUC terms) and `speer-h105` (`Link`, cite only).

## 5. Caveats & honesty boundaries (`LAB-01`)

- All `L/D`, `Cl,max`, `Cd,min`, and `Cm0` figures above are computed by `NeuralFoilNetwork` 0.3.2 xxxlarge (surrogate trained on XFOIL, not water-tank measurements).
- All `V_crit` figures are inviscid 2D `-Cp_min` cavitation inception screenings at `h = 0.50 m` in 15 °C ITTC seawater (`PanelMethod`, no nuclei/roughness/3D-tip model).
- Section designations and UI copy never claim "optimized", "validated", "certified", "recommended", or "cavitation-free".
