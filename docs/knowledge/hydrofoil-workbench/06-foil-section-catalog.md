---
id: kb-hw-foil-section-catalog
title: "Foil section catalog for hydrofoil wings, stabilizers, struts and fins"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, sections, catalog, provenance, licensing, cavitation, ncrit]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes which section families matter for water-sports hydrofoils (Eppler hydrofoil set, NACA 4/16/6-series,
  Speer H105, low-Re Selig/Drela/Wortmann sections, symmetric strut families), what each one's geometry actually is
  (measured from the coordinate files), where the authoritative coordinates and polars live and under what terms, and
  what the evidence says about water Ncrit, cavitation screening and section selection. Main design implication: the
  v1 catalog must separate "coordinates we may bundle" from "coordinates we generate analytically at build" from
  "link-only, pending permission" — the UIUC coordinate database carries no stated licence, the UIUC wind-tunnel data
  is GPL, Airfoil Tools forbids reproduction, and H105 has no published redistribution terms.
---

# Foil section catalog for hydrofoil wings, stabilizers, struts and fins

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. Which Eppler sections were designed for hydrofoils, what are they geometrically, and where are authoritative coordinates published under what terms?
2. Which NACA families matter (4-digit, 5-digit, 16-series, 6/6A-series, 7-series), how are they defined analytically, and where do exact and generated coordinates differ?
3. Which designed-for-hydrofoil or low-Re sections (H105, Moth/kitefoil sections, FX 63-137, S1223, SD7003, AG, Clark Y, Göttingen 796/797, base-vented) are relevant, with design Re/Cl, t/c, camber and terms?
4. Which symmetric families suit masts/struts/fins, what do brands actually build, and what is special about surface-piercing struts?
5. What are the selection criteria for hydrofoil sections (cavitation bucket, laminar bucket at water Re, Ncrit for water, LE radius, thickness, TE thickness, Cm, free-surface behaviour)?
6. Which data sources and tools supply polars and experimental data, with what accuracy claims and redistribution terms?
7. What should the v1 catalog admission list be, by role, source, terms status, available polars, and pending items?

Confidence labels: **Verified** = the cited primary source was opened this session, or the number was computed here from downloaded coordinate files (`spikes` equivalent: scratchpad script `geom2.py`, results quoted verbatim below). **Inferred** = reasoned from Verified sources. **Flagged** = single/tertiary/dated source, or recall not confirmed this session. Every recalled fact that could not be confirmed is Flagged, by rule.

## Headline findings

1. The UIUC Airfoil Coordinates Database (≈1,650 airfoils, Version 2.0) publishes the Eppler hydrofoil set (E817, E818, E836, E837, E838, E874, E904, E908, plus E862–E864 "strut" sections) and states **no licence, terms of use or redistribution permission** on the database page or its FAQ; the only rights statement is a site copyright notice "© 1994–2026 UIUC Applied Aerodynamics Group". Bundling those files is therefore an unestablished right, not a permissive one — *(Verified, [S1][S2])*.
2. The UIUC **wind-tunnel** data (Summary of Low-Speed Airfoil Data volumes) is explicitly released under the **GNU General Public License** with a "UIUC Low-Speed Airfoil Tests Manifesto": commercial use is permitted, but the product must "conspicuously state" the UIUC LSAT origin, charge nothing extra for the data, and let recipients copy and redistribute it. That is copyleft on data — COMMIT-02's "permissive only" rule needs an explicit ruling before any LSAT polar ships in the product — *(Verified, [S3])*.
3. Measured from the UIUC files, the five Eppler lifting hydrofoil sections are: E817 10.98 % t/c (x/c 0.33), camber 2.88 % peaking at x/c 0.69; E818 9.37 %, 2.79 % at 0.67; E874 7.90 %, 0.95 % at 0.33; E904 9.00 %, 1.34 % at 0.50; E908 9.00 %, 2.77 % at 0.66; mean t/c 9.25 %. All are sharp-TE (0.00 % gap) and all UIUC copies are **Lednicer**-format — *(Verified by execution, [S1])*.
4. The three "strut" Epplers in the repo list are symmetric hydrofoil sections at 12.64 % (E836), 16.10 % (E837) and 18.37 % (E838); the differently named E862/E863/E864 "strut airfoils" are 32–39 % thick fairing sections with 1.2–1.6 % TE gaps and are not mast candidates — *(Verified by execution, [S1])*.
5. The Eppler hydrofoil family's design record is the SNAME *Journal of Ship Research* trilogy: Eppler & Shen 1979 (Part 1, symmetric), Shen & Eppler 1981 (Part 2, non-symmetric, "minimum pressure bucket whose depth and width are adapted to practical applications"), Shen 1985 (Part 3, experimental verification). E817 has separately been tested for cavitation inception and scale effect at the St. Anthony Falls water tunnel (ASME JFE 2000, 2002), where it is described as "theoretically designed to have a wide cavitation-free bucket" — *(Verified for existence and abstracts, [S4][S5][S6][S7][S8])*.
6. Speer's H105 page (last updated 16 Jan 1999) states its intent — sailing hydrofoils at 20–30 kn and low Re, "avoid laminar separation and ventilation … while still having low velocities at small angles of attack to avoid cavitation at high speeds", convex velocity profiles instead of Eppler's rooftop, restrained aft loading, sacrificing "some of the upper range of the cavitation envelope for a significantly higher maximum lift" — but the page carries **no coordinates, no design Re/Cl numbers and no licence**; H105 is **not** in UIUC (HTTP 404) and Speer did not post coordinates in the "Hydrofoil exercise to validate CFD analysis" thread. H105 must stay *Pending admission* until written terms exist — *(Verified, [S9][S10][S1])*.
7. The "1 < Ncrit < 3 for water" rule the repo carries originates in Speer's 2004 forum posts, where he recommends "setting Ncrit to 3 or even lower" because of "critters and particulate matter" and, the next day, states "I don't have any test data to say what Ncrit should be". The one primary comparison with towing-tank measurements found (Day, Cocard, Troll, CSYS 2019, full-scale Moth T-foil) used XFOIL 6.99 free transition with **Ncrit = 4 (≈0.6 % turbulence)** and reports the lift–drag relation "reasonably accurate in most cases". The evidence supports a *band* (≈2–4) with no measured water N-factor, not a constant — *(Verified, [S11][S12])*.
8. NeuralFoil (Sharpe & Hansman, arXiv 2503.16323, 2025; MIT licence for its own code — but its package graph pulls AeroSandbox and thence CasADi, LGPL-3.0; see area file 09) claims an input space of Re 10²–10¹⁰ and 360° α, mean relative drag error 0.37 % on simple cases and 2.0 % on a test set with post-stall/transitional cases, 8–1,000× faster than XFOIL — all **relative to XFOIL**, not to experiment. AeroSandbox's `get_NACA_coordinates` raises `NotImplementedError("Only 4-digit NACA airfoils are currently supported!")`; 5-digit, 16- and 6-series must come from another generator — *(Verified, [S13][S14][S15])*.
9. NASA TM 4741 (Ladson, Brooks, Hill, Sproles, Dec 1996) is the reference generator for NACA 4-digit, 4-digit-modified, 5-digit, 16-series, 6- and 6A-series ordinates (Fortran 77, US Government work); for 6-series and all but the leading edge of 6A-series its ordinates agree with the published tables "generally within 5 × 10⁻⁵ chord". The 16-series is a 4-digit-modified thickness form (leading-edge index 4, maximum thickness at 0.5c). This is the build-time generation path for every NACA family the catalog needs — *(Verified, [S16][S17])*.
10. DTNSRDC report ADA032272 (Layne) measured NACA 16-309 (the PCH section) against a modified 64A309 in the High-Speed Tow and Rotating-Arm facilities, flaps to 17.5°, pitch to 12°, and found the 64A309 reaches a higher L/D — the primary source behind the repo's "6-series over 16-series" ranking. US Government work; the geometry is a foil-plus-strut system, so it is a **ranking** reference, not a 2D coefficient fixture — *(Verified abstract, [S18])*.
11. Published mast geometry: Armstrong states Performance-X / Performance Mk II chord 105.5–106.8 mm (earlier masts ≈110 mm, Mk I 114–118 mm) and a "12 mm" Performance-X profile; Mk II has "around 30 per cent more bending stiffness and 20 per cent more torsional stiffness" than Mk I. That is t/c ≈ 11–15 % at chord ≈ 106 mm; no section family is named. The Moth strut Day et al. digitised "matched very closely" a NACA 66012 scaled to 14.5 % thickness — *(Verified, [S19][S12])*.
12. Surface-piercing strut ventilation has fresh primary data: Aguiar Ferreira et al. (TU Delft, arXiv 2503.18015, CC BY 4.0, March 2025) tested a semi-ogive blunt-TE strut and a modified NACA 0010-34 at Fr 0.5–2.5, AR 1.0/1.5, identifying nose, tail and base ventilation regimes and showing the bistable/stable boundary "extends to significantly higher α than previously estimated" — *(Verified, [S20])*.
13. Airfoil Tools (1,638 airfoils, XFOIL-generated polars) carries "All Rights Reserved. No content or images on this web site should be reproduced without permission." It is link-only; Martin Hepperle's hydrofoil page is "educational, non-profit use" with recompilation prohibited — also link-only — *(Verified, [S21][S22])*.
14. No peer-reviewed head-to-head of E817 vs H105 vs NACA 63-412 at water-sports Re was found; the comparisons that exist are Speer's own text, forum posts and the repo's Phase-0 NeuralFoil run. The repo's "E818 best **measured** cavitation 42.1 kn" is a computed Cp_min-derived critical speed, not a measurement — *(Verified absence after four searches; see Contradictions)*.
15. Current section-design frontier for sailing foils is multipoint RANS shape optimization with an explicit Cp_min (cavitation) constraint across flap angle, Cl and speed (JST 2025, AC75), plus CFD–GA leading-edge optimisation for inception delay (2025) — the catalog should therefore carry design-point metadata (design Cl, design Re, intended σ range) as first-class fields rather than a name — *(Verified existence/abstracts, [S23][S24])*.

## State of the art

### Eppler hydrofoil sections (E817–E838, E874, E904, E908)

Eppler's hydrofoil work was done for DTNSRDC with Y. T. Shen using the Eppler design/analysis code. Part 1 (1979) produced "a series of symmetrical hydrofoil sections with improved hydrodynamic characteristics in terms of cavitation inception"; Part 2 (1981) applied the method to non-symmetrical sections "having a minimum pressure bucket whose depth and width are adapted to practical applications"; Part 3 (1985) reports experimental verification [S4][S5][S6]. The design philosophy is a **rooftop** pressure distribution: constant maximum velocity over the forward upper surface at the design angle, which gives the widest cavitation-free Cl range for a given −Cp_min, then a concave pressure recovery. Speer's critique (the H105 page) is that the recovery aft of the rooftop "is too short for low Reynolds numbers" and the flat rooftop "makes for a tendency toward leading edge stall" — that is, these are ship-scale (≈40 kn, Re ≳ 3 × 10⁶) sections [S9] *(Verified)*. The book *Airfoil Design and Data* (Springer 1990, ISBN 3-540-52505-X) exists and is cited by the E817 cavitation papers; whether it reproduces the hydrofoil chapter and the individual design Cl/Re for each of E817–E908 could not be opened this session, so per-section **design Cl and design Re remain Flagged** [S7][S25].

Geometry measured here from the UIUC coordinate files (linear interpolation on 1,001 chord stations; camber = mean line at its maximum |value|) *(Verified by execution)*:

| Section | UIUC file | Format | Points | t/c | x(t_max) | Camber | x(c_max) | TE gap | UIUC description |
|---|---|---|---|---|---|---|---|---|---|
| E817 | e817.dat | Lednicer | 70 | 10.98 % | 0.33 | 2.88 % | 0.69 | 0.00 % | "Eppler 817 hydrofoil airfoil" |
| E818 | e818.dat | Lednicer | 69 | 9.37 % | 0.33 | 2.79 % | 0.67 | 0.00 % | "Eppler 818 hydrofoil airfoil" |
| E874 | e874.dat | Lednicer | 122 | 7.90 % | 0.29 | 0.95 % | 0.33 | 0.00 % | "Eppler 874 hydrofoil airfoil" |
| E904 | e904.dat | Lednicer | 111 | 9.00 % | 0.45 | 1.34 % | 0.50 | 0.00 % | "Eppler 904 airfoil" (UIUC index: hydrofoil) |
| E908 | e908.dat | Lednicer | 111 | 9.00 % | 0.44 | 2.77 % | 0.66 | 0.00 % | "Eppler 908 airfoil" (UIUC index: hydrofoil) |
| E836 | e836.dat | Lednicer | 63 | 12.64 % | 0.43 | 0.00 % | — | 0.00 % | "Eppler E836 hydrofoil airfoil" |
| E837 | e837.dat | Lednicer | 63 | 16.10 % | 0.37 | 0.00 % | — | 0.00 % | "Eppler E837 hydrofoil airfoil" |
| E838 | e838.dat | Lednicer | 63 | 18.37 % | 0.37 | 0.00 % | — | 0.00 % | "Eppler E838 hydrofoil airfoil" |
| E862 | e862.dat | Lednicer | 63 | 32.37 % | 0.28 | 0.00 % | — | 1.20 % | "Eppler 862 strut airfoil" |
| E863 | e863.dat | Lednicer | 63 | 35.73 % | 0.28 | 0.00 % | — | 1.40 % | "Eppler 863 strut airfoil" |
| E864 | e864.dat | Lednicer | 63 | 38.82 % | 0.33 | 0.06 % | 0.11 | 1.60 % | "Eppler 864 strut airfoil" |

Reading of the table *(Inferred)*: E817/E818/E908 carry their camber far aft (x/c 0.66–0.69) — aft loading, which is how a rooftop section keeps the forward upper surface flat; E874 and E904 are the low-camber (≈1 %) members intended for higher speed / lower design Cl; E874 is the thinnest (7.9 %). The UIUC index also lists E850–E858 as propeller sections [S1]. The "E817–E838 family" phrasing in the task is only partly right: UIUC carries E817, E818, E836, E837, E838 in that numeric range; E819–E835 are not listed as hydrofoils there *(Verified by listing)*.

Experimental evidence on E817: Kjeldsen, Arndt & Effertz (ASME JFE 122(1), 2000) and Arndt et al. (JFE 124(1), 2002, scale effect) measured cavitation inception and development on a 2D E817 at St. Anthony Falls Laboratory; the ASME pages were not readable (HTTP 403) so inception σ values are **Flagged** [S7][S8]. Arndt's "Some remarks on hydrofoil cavitation" (J. Hydrodynamics 2012) is the review to cite for the physics of sheet/cloud cavitation on these sections [S26] *(Verified existence)*.

### NACA families: definitions, generators, and where "exact" and "generated" differ

**4-digit (0009/0010/0012 struts and fins, 4412 baseline).** Closed-form thickness polynomial and parabolic-arc mean line; the classical thickness polynomial gives a finite TE (0.21 % of chord for 0010 and 0.26 % for 4412 in the UIUC files measured here) unless the closed-TE coefficient (−0.1036 in place of −0.1015) is used. The repo's Phase-0 "silent 6 %-thick NACA 0012" defect [grounding] is exactly the Lednicer-read-as-Selig failure; the UIUC `naca0012` file name was not found this session (404 for `naca0012.dat`; `naca0010.dat` exists), so **generate 4-digit analytically, never vendor** *(Verified by execution and by TM 4741 [S16])*.

**5-digit.** Same thickness form with the 230-type mean line (design Cl 0.3, max camber forward); defined in TM X-3284/TM 4741. No hydrofoil use found; keep as generator capability only *(Inferred, [S16])*.

**16-series (16-006, 16-012, 16-021, 16-309).** Defined in TM X-3284 as a 4-digit-modified thickness distribution with leading-edge index 4 and maximum thickness at 0.5c; nomenclature 16-*d*ₗ*tt*: design Cl in tenths, thickness in percent (16-309 → Cl 0.3, 9 %). TN 1546 (Lindsey, Stevenson, Daley, 1948) is the wind-tunnel dataset for 24 sections at M 0.3–0.8 [S17]. Measured here from UIUC: 16-006 6.00 %, 16-012 12.00 %, 16-021 21.00 %, all with maximum thickness at x/c 0.50 and TE gaps 0.12/0.24/0.42 % (33 points each — coarse) *(Verified by execution)*. Hydrodynamic role: the classic propeller/ship-hydrofoil family (PCH used 16-309 [S18]); its mid-chord maximum thickness gives a flat rooftop and a deep cavitation bucket but poor pressure recovery at low Re — the reason Speer and the DTNSRDC test prefer a 6-series [S18] *(Verified ranking; mechanism Inferred)*. The mean line used with 16-series in the NACA generator is the a = 1.0 uniform-load line — **Flagged** (recall, not re-opened).

**6-series and 6A (63-, 64-, 65-, 66-; 64A410, 63-209, 63-412, 66-209, 66-012, 66-018).** Nomenclature 6*p*-*d*ₗ*tt*: second digit = chordwise position of minimum pressure in tenths for the basic symmetric thickness at zero lift; digit after the dash = design Cl in tenths; last two = t/c; an optional subscript gives the half-width of the low-drag bucket in Cl. The "A" modification replaces the cusped rear with essentially straight sides from about 0.8c to the TE, giving a thicker, more manufacturable trailing edge and slightly different bucket behaviour *(Inferred from TM 4741 [S16]; standard NACA nomenclature per Report 824 [S27] — the report's text was not re-opened, so the description is Inferred)*. Ordinates are **not** closed-form: the thickness forms were derived by conformal mapping and are tabulated; TM X-3069/TM 4741 reproduce them by fitted procedures, agreeing with the published tables "generally within 5 × 10⁻⁵ chord" for 6-series, with a known exception at the 6A leading edge [S16] *(Verified)*. Measured here (UIUC, 51–53 points, Selig format except `n63412.dat` which is Lednicer): 63-209 9.00 %/1.10 %; 63-412 12.00 %/2.20 %; 63A210 9.99 %/1.33 % (TE 0.04 %); 64-210 9.98 %/1.10 %; 64A410 9.99 %/2.66 % at x 0.40 (TE 0.04 %); 65-410 9.99 %/2.21 %; 65₂-415 14.99 %/2.20 %; 66-209 9.00 %/1.10 %; 66-210 10.00 %/1.10 % *(Verified by execution)*. Note the camber of the a = 1.0 mean line at design Cl 0.2 is ≈1.1 % and at 0.4 ≈2.2 % — consistent with the files, a useful admission check.

**7-series.** Tabulated in Report 824; designed for extended laminar flow on both surfaces; no hydrofoil use found and no generator in TM 4741 — exclude from v1 *(Inferred)*.

**Generators and their scope (all Verified this session unless noted):**
- NASA TM 4741 Fortran (US Government work; public domain in the US) — 4, 4-mod, 5, 16, 6, 6A [S16].
- AeroSandbox `Airfoil("naca4412")` → `get_NACA_coordinates`: **4-digit only**, raises otherwise; `get_UIUC_coordinates` reads a copy of the UIUC database bundled in the package (MIT-licensed repo; the bundling does not itself establish UIUC's terms) [S14].
- XFOIL `NACA` command: 4- and 5-digit only — **Flagged** (recall of the XFOIL manual; not re-opened).
- `airfoils` (airinnova, Apache-2.0): NACA 4-digit generator plus file I/O [S15] — scope beyond 4-digit **Flagged**.
- OpenVSP (NASA Open Source Agreement 1.3): includes 4-digit, 4-digit-mod, 5-digit, 16-series and 6-series cross-section types [S15][grounding]; NOSA is not MIT/BSD/Apache, so it is a process-invocation tool at most under COMMIT-02 *(Verified licence; feature list Verified via grounding note)*.
- Airfoil Tools NACA 4/5-digit generators — web only, no-reproduction terms [S21].

**Where exact and generated differ.** UIUC files for NACA sections are digitised/typed tables with 33–53 points and finite or zero TE gaps that vary by file; a TM 4741 generation at, say, 161 cosine-spaced points is the better-conditioned source for the same name and can be regenerated bit-for-bit at build. The two will differ at the 10⁻⁴–10⁻³ chord level (interpolation, LE resolution, TE closure), which is visible in Cp_min at high α *(Inferred from the point counts and TE gaps measured above)*.

### Designed-for-hydrofoil and low-Re sections

**Speer H105.** Intent verbatim on the page: low-Re sailing hydrofoils, 20–30 kn, "designed to avoid laminar separation and ventilation when operating at low speeds and moderate angles of attack, while still having low velocities at small angles of attack to avoid cavitation at high speeds"; "convex velocity profiles, which use the entire surface to control the position of the laminar separation bubble"; "the location of the maximum velocity also changes with angle of attack"; "restrained in its use of aft loading". The page compares H105 with the earlier H002/H005 and with E817; it gives **no** design Re, design Cl, t/c or camber numbers, and no coordinates or terms [S9] *(Verified)*. Community reports say H105 is used on the Axiom Moth and that Speer has shared coordinates by e-mail and in forum attachments — tertiary, **Flagged** [S28]. Because `h105.dat` is absent from UIUC (404) and no written redistribution statement was found, the catalog cannot bundle it; even a t/c value should be labelled Unknown until a coordinate file with a rights statement is in hand.

**Moth-class sections.** Forum-level history: NACA 63-412 on the original Ilett/Fastacraft foils "until the Bladerider", H105 on Axiom [S28] *(Flagged, tertiary)*. Primary: Beaver & Zseleczky (CSYS 2009) full-scale tow tests of a Moth [S29] *(Verified existence)*; Day, Cocard & Troll (CSYS 2019) tested a Bladerider main T-foil full-scale (mean-chord Re ≈ 1.2 × 10⁵ at 1.5 m/s and up), digitised the horizontal section (max thickness/camber ratios reported: 12.8 %, 3.1 %; strut ≈ NACA 66012 scaled to 14.5 %), ran XFOIL 6.99 at six Re between 10⁵ and 10⁶, 13 α and 17 flap angles with free transition and **Ncrit = 4** ("corresponding to a turbulence level of around 0.6 %"), and added the free surface by a biplane-image approach (about +3 % on drag area for a 12 % section at moderate chord Froude numbers). Their conclusion: the predicted lift–drag relation is "reasonably accurate in most cases", and "further investigation of turbulence levels and transition models are planned" [S12] *(Verified by execution — PDF text decoded)*.

**Kitefoil / windfoil.** A 2025 *Journal of Marine Science and Application* paper measures fluid forces on a kitefoil/windfoil hydrofoil as a function of the submergence-based Froude number [S30] *(Verified title only; body behind login — Flagged)*. Kiteforum/boatdesign threads discuss E387 vs H105 and "Reynolds 600K–1.5MM" kitefoil design — tertiary [S28].

**Selig S1223.** Designed for Re 2 × 10⁵ with concave pressure recovery and aft loading, using PROFOIL, the Eppler code and ISES; wind-tunnel Cl,max 2.2 (2.3 with vortex generators or a 1 % Gurney flap, separately) [S31] *(Verified abstract)*. Measured here: 12.14 % t/c at x 0.20, camber 8.67 % *(Verified by execution)*. Its Cp_min at working Cl is far below hydrofoil cavitation limits at 15 kn; it is a take-off/high-lift study fixture, not a foiling section *(Inferred)*.

**SD7003.** 8.51 % t/c, 1.46 % camber (file header "SD7003-085-88") *(Verified by execution)*. Its status as the standard laminar-separation-bubble benchmark near Re 6 × 10⁴ is **Flagged** (recall). Relevant as a regression fixture for transition/LSB behaviour of the 2D solver at the low end of the water Re band.

**Drela AG series.** `ag24.dat` header reads "AG24 Bubble Dancer DLG by Mark Drela"; AG24 8.41 %/2.22 %, AG35 8.72 %/4.37 % *(Verified by execution)*. Discus-launch-glider sections for Re ≈ 5 × 10⁴–2 × 10⁵ — **Flagged** design range (recall). Not hydrofoil sections; useful only as low-Re solver fixtures.

**Wortmann FX 63-137.** 13.71 % t/c, 5.97 % camber, 99 points *(Verified by execution)*; a high-lift low-Re sailplane/wind-turbine section. An *Aeronautical Journal* study of TE extensions on Göttingen 797 and FX 63-137 at Re 3 × 10⁵–10⁶ exists [S32] *(Verified title)*. Design Re/Cl **Flagged**.

**Clark Y.** 11.71 % t/c, 3.43 % camber, near-flat lower surface *(Verified by execution)*. Historic; the flat lower surface is a manufacturing convenience, not a cavitation feature *(Inferred)*.

**Göttingen 796/797.** UIUC files are coarse (43 and 29 points), 12.00 %/3.69 % and 16.00 %/5.02 %, TE gaps 0.40 %/0.80 % *(Verified by execution)*. The claim that these were "classic hydrofoil" sections (Supramar/Bell era) could **not** be confirmed in any source this session — **Flagged**; do not label them "hydrofoil" in the catalog without a citation.

**Eppler E387/E393.** E387 9.07 %/3.80 %; E393 11.53 %/4.00 % *(Verified by execution)*. E393 is the section used in a Moth performance program (tertiary [S28]); E387 is the low-Re wind-tunnel reference airfoil in the UIUC LSAT volumes — **Flagged** (recall).

**Liebeck high-lift, Tulin supercavitating, base-vented sections.** Named for completeness. Liebeck sections (Stratford-recovery high lift) were not verified this session — **Flagged**. Supercavitating (Tulin) and base-vented families are out of scope for a sub-cavitating design tool; the IHS compilation records U.S. Navy struts that were "one subcavitating, one base-ventilated, and one supercavitating", a 4 %-thick 16(35)04 rung, and an 18 % parabolic strut tested at 65 kt, and notes that "many strut 'cavitation' problems are really ventilation problems" [S33] *(Verified text; tertiary compilation of practitioner Q&A)*.

### Struts, masts and fins

**Families.** Symmetric candidates are (i) NACA 00xx (0009–0012, fins and thin struts), (ii) NACA 6-series symmetric (63-012, 64-012, 66-012/66-018 — the Moth strut in Day et al. "matched very closely" a 66012 scaled to 14.5 % [S12]), (iii) NACA 16-0xx (16-012, 16-021: mid-chord maximum thickness, deep bucket), (iv) Eppler symmetric hydrofoil sections E836/E837/E838 (12.6/16.1/18.4 %, designed for cavitation inception, Part 1 of the JSR trilogy [S4]). Trade-offs *(Inferred from the sources above)*: drag at zero lift favours thin (9–12 %) and aft maximum thickness; cavitation at yaw/sideslip favours a wide bucket (Eppler symmetric or 16/66-series) and a modest LE radius; ventilation on a surface-piercing mast is a separate mechanism driven by low pressure near the free surface at angle, not by vapour pressure [S20][S33]; bending/torsional stiffness scales with t³ and with chord, so brands trade drag against stiffness through thickness at fixed chord ≈ 106–118 mm (Armstrong: Mk II ≈ 30 % stiffer in bending than Mk I; Performance-X "12 mm") [S19] *(Verified chord/stiffness statements; the 12 mm figure is quoted from the page but other vendor summaries give 13.8–15.8 mm — treat the thickness as Flagged until read off a datasheet)*. Sabfoil ≈ 14 mm and a "10–12.5 % at ≈120 mm chord" rule of thumb are tertiary [S34] *(Flagged)*.

**Surface-piercing struts.** Aguiar Ferreira et al. 2025 (CC BY 4.0) is the current primary reference: quasi-steady towing-tank tests of a semi-ogive blunt-TE strut and a modified NACA 0010-34 at Fr 0.5–2.5, AR 1.0 and 1.5, sweeping α to ventilation onset. Three mechanisms: nose ventilation (Fr < 1.0–1.25, increasing α), tail ventilation (higher Fr, negative-α trends), and base ventilation (blunt section only, unstable cavity). The bistable/globally-stable boundary "extends to significantly higher α than previously estimated" and the paper publishes a revised stability map [S20] *(Verified)*. Speer's recommended validation case for surface-piercing foils is Kuhn & Scragg, "Analysis of Lift and Drag on a Surface Piercing Foil", 11th CSYS 1993 [S10] *(Verified citation)*.

**Surf/windsurf fins.** No primary literature on fin section templates (Futures/FCS, "flat-back", 50/50 vs 80/20 foils) was found in this session; only maker files (a NACA 0012 FCS fin print) — **Flagged, open question**. Treat fins as a future surface with the 4-digit symmetric generator as the only admitted default.

### Selection criteria for hydrofoil sections

1. **Cavitation bucket (σ_required = −Cp_min vs Cl).** Screening inception when −Cp_min ≥ σ, σ = (p_atm + ρ g h − p_v)/(½ ρ V²). Depth h, temperature (p_v) and salinity (ρ) enter through σ; the section enters through Cp_min(α, Re). Hepperle's page states the same dependence (vapour pressure, speed, immersion) and shows the usable Cl range shrinking with speed, with 15 and 20 kn examples on NACA 4405 vs 4410 [S22] *(Verified)*. Cp_min from a panel/IBL code under-reads sharp suction peaks at coarse resolution — the unsafe direction (the repo already records this; ANA-02).
2. **Laminar bucket at water Re (5 × 10⁵–2 × 10⁶).** For chord 0.06–0.15 m at 4–12 m/s and ν ≈ 1.05 × 10⁻⁶ m²/s (salt, ≈20 °C; ITTC value — **Flagged** recall, the repo's fluid table governs), Re ≈ 2.3 × 10⁵–1.7 × 10⁶; the Day et al. runs at 10⁵–10⁶ bracket this [S12]. Bucket width shrinks with Ncrit and Re; the bucket edges, not the bottom, are where E817's short recovery penalises low Re [S9] *(Inferred)*.
3. **Ncrit for water.** Evidence table — see Data section. Practical outcome: run every polar at two Ncrit values (e.g. 2 and 4) and show the spread; the spread is the transition uncertainty, which is currently unmeasurable *(Inferred from [S11][S12])*.
4. **Leading-edge radius vs debris tolerance.** Speer's remark that transition in water is promoted by "critters and particulate matter" [S11] is the only source found; no quantitative roughness-sensitivity study on hydrofoil sections was found — **Flagged**; the repo's roughness gap (GAP register) remains open.
5. **Thickness for structure.** No hydrodynamic optimum (repo Phase 0); Armstrong's mast data show the stiffness-vs-thickness trade being made at fixed chord [S19]. For wings, E874 (7.9 %) vs E817 (11 %) spans the practical range *(Verified geometry; trade Inferred)*.
6. **TE thickness for manufacture.** UIUC Eppler files close to 0.00 %; NACA 4/16-series carry 0.2–0.4 %; a 2026 study of thin NACA sections with moderately truncated trailing edges exists [S35] *(Verified title; content Flagged)*. The catalog should record the as-filed TE gap and any applied closure as a profile revision (A3).
7. **Pitching moment Cm.** Aft-loaded sections (E817/E818/E908 camber peak at x/c ≈ 0.67–0.69; S1223) carry large negative Cm₀, which increases the stabilizer down-load and thus the stabilizer area/drag for a given fuselage length *(Inferred from the measured camber positions; the Cm values themselves must come from the polar pipeline, not from this file)*.
8. **Free-surface proximity.** Day et al. added ≈3 % drag-area for a 12 % section at moderate chord Froude numbers using a biplane-image model and note wave-making effects at higher speeds require a free-surface CFD [S12]; the 2025 JMSA kitefoil paper frames the effect through the submergence Froude number [S30] *(Verified/Flagged as marked)*.

### Data sources and tools for polars

| Source | What it holds | Accuracy / method | Terms (as read 2026-09-20) | Confidence |
|---|---|---|---|---|
| UIUC coordinate database [S1] | ≈1,650 coordinate files, Selig or Lednicer | Provenance varies per file (typed tables, digitised) | No licence statement; site copyright notice only | Verified |
| UIUC LSAT volumes 1–4 (+ Williamson thesis, "vol. 6") [S3] | Low-speed wind-tunnel polars, Re ≈ 6 × 10⁴–5 × 10⁵ (range Flagged) | Measured | GPL + Manifesto; commercial use OK with conspicuous attribution, no data surcharge, redistribution rights preserved | Verified (terms) |
| Airfoil Tools [S21] | 1,638 airfoils, XFOIL polars (Ncrit 5 and 9, Re 5 × 10⁴–10⁶ — Flagged recall) | XFOIL-generated | "All Rights Reserved … should not be reproduced without permission" | Verified (terms) |
| NeuralFoil [S13] | Surrogate of XFOIL, Re 10²–10¹⁰, 360° α | 0.37 % / 2.0 % mean relative drag error vs XFOIL | MIT | Verified |
| XFOIL 6.99 [S36] | Panel + IBL, e^N transition | Reference for the surrogate; not experiment | GPL (process-invoke only under COMMIT-02) | Verified |
| NASA TM 4741 [S16] | NACA ordinate generator | ≤ 5 × 10⁻⁵ c vs tables (6-series) | US Government work | Verified |
| NACA Report 824 [S27] | 4/5/6/7-series tables and polars | Measured (LTPT) | Public domain, on NTRS | Verified availability |
| DTIC ADA032272 [S18] | 16-309 vs 64A309 foil+strut, flapped, tow + rotating arm | Measured | US Government work | Verified abstract |
| Shen & Dimotakis 1989 [S37] | NACA 66(MOD), GALCIT HSWT, 9–18 m/s, α 0–6°, 13 taps, cavitating/non-cavitating | Measured | Caltech authors' repository; reuse terms Flagged | Verified abstract |
| Delft Twist-11 (Foeth 2008) [S38] | NACA 0009 twisted foil, c 150 mm, span 300 mm; cavitation benchmark (VIRTUE WP4, SMP'11) | Measured | TU Delft thesis; data terms Flagged | Verified description |
| Aguiar Ferreira et al. 2025 [S20] | Surface-piercing strut ventilation maps | Measured | CC BY 4.0 | Verified |
| Day et al. 2019 [S12] | Full-scale Moth T-foil lift/drag vs speed, α, flap, immersion | Measured; XFOIL Ncrit 4 comparison | Open repository copy (Strathprints); reuse terms not checked — Flagged | Verified content |
| NASA TMR [grounding] | Turbulence-model verification cases (SST etc.), not hydrofoil polars | — | Public | Verified (grounding) |

**XFOIL/NeuralFoil vs experiment at water Re.** The only direct comparison found this session is Day et al.: XFOIL (Ncrit 4) inside a lifting-line + image model reproduces the measured lift–drag relation "reasonably accurate in most cases" but not the α/flap mapping [S12]. NeuralFoil's published errors are against XFOIL, so the chain "experiment ← XFOIL ← NeuralFoil" carries two unquantified links at water Re — the repo's "checked against experiment" verb (COMMIT-01) is still unmet at section level *(Inferred)*.

## Comparables

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| UIUC Airfoil Data Site [S1] | A coordinate archive, not a catalog | Flat file list, two formats, minimal metadata | Coverage; canonical names | No licence; no per-file provenance; mixed formats and point counts | Unstated | Verified |
| Airfoil Tools [S21] | Browse + XFOIL polars per airfoil | Precomputed XFOIL at fixed Ncrit/Re grid | Instant polars, comparisons | Air Ncrit, no cavitation, no-reproduction terms | Proprietary | Verified |
| AeroSandbox / NeuralFoil [S13][S14] | Programmatic airfoil object + surrogate polars | Python; bundled UIUC copy; 4-digit generator | Speed, differentiable, MIT | Not 6/16-series; accuracy vs XFOIL only | MIT | Verified |
| NASA TM 4741 generator [S16] | Reproducible NACA ordinates | Fortran 77 | Exactness, all NACA families | No non-NACA sections | US Gov | Verified |
| OpenVSP [S15] | CAD cross-section library | 4/5/16/6-series types + CST fit | Families built in | NOSA licence; heavy dependency | NOSA 1.3 | Verified |
| Speer H105 page [S9] | Design rationale essay | Prose + plots | Explains low-Re hydrofoil philosophy | No numbers, no coordinates, no terms | None stated | Verified |
| IHS "Hydrofoil, Rudder, and Strut Design Issues" [S33] | Practitioner Q&A compilation | Forum-style archive | Historic strut/ventilation lore, section list | Undated, unreviewed | IHS site | Verified text (tertiary) |
| Day et al. 2019 [S12] | Full-scale T-foil measurement + simplified prediction | Tank + XFOIL/LL | Real water Re data, Ncrit stated | One foil; free-surface model simplified | Conference paper | Verified |
| JST 2025 AC75 section optimisation [S23] | Cp_min-constrained multipoint RANS optimisation | Adjoint/RANS | Frontier method | AC75 speeds/Re; behind paywall | Journal | Verified abstract |

## Reference information

- **Eppler & Shen, JSR 23(3) 1979, 209–217; Shen & Eppler, JSR 25(3) 1981, 191–200; Shen, JSR 29(1) 1985, 39–50** [S4][S5][S6] — the design record of the hydrofoil family. Requires of us: cite them as the provenance of E817–E838; do not invent design Cl values; treat the sections as high-Re rooftop designs.
- **Eppler, *Airfoil Design and Data*, Springer 1990** [S25] — coordinate and polar plates for Eppler sections; the book is copyrighted, so plates are not a redistribution source.
- **NACA Report 824 (Abbott, von Doenhoff, Stivers, 1945)** [S27] — 4/5/6/7-series definitions, tables, LTPT polars. Public domain. Requires: nomenclature and the tabulated thickness forms as the "exact" reference for admission tests.
- **NASA TM 4741 (1996) and TM X-3284 (1975)** [S16][S17] — generators; the admission test for a generated NACA section is agreement with the tables within the report's own stated tolerance.
- **NACA TN 1546 (1948)** [S17] — 16-series aerodynamic data (air, M 0.3–0.8).
- **DTNSRDC ADA032272** [S18] — the only measured 16- vs 6-series hydrofoil ranking found; foil-plus-strut, flapped.
- **ASME JFE 122(1) 2000 and 124(1) 2002** [S7][S8] — E817 cavitation inception and scale effect.
- **Shen & Dimotakis, JFE 111(3) 1989** [S37]; **Foeth 2008 Delft Twist-11** [S38] — cavitation-tunnel fixtures for a future local-CFD validation tier.
- **Aguiar Ferreira et al. 2025, arXiv 2503.18015 (CC BY 4.0)** [S20] — ventilation stability map for surface-piercing struts.
- **Day, Cocard, Troll, CSYS 2019** [S12]; **Beaver & Zseleczky, CSYS 2009** [S29] — Moth full-scale data; Ncrit 4 precedent.
- **UIUC LSAT licence page** [S3] — the GPL/Manifesto conditions verbatim; **UIUC coordinate database** [S1] — no terms.
- **Sharpe & Hansman 2025 (NeuralFoil)** [S13] — accuracy envelope statements to quote in the UI's method/uncertainty status (ANA-01).
- **Selig & Guglielmo, J. Aircraft 34(1) 1997** [S31] — S1223 design record.

## Data, constants, formulae and invariants

**Cavitation screening.**
σ = (p_atm + ρ g h − p_v) / (½ ρ V²)  [dimensionless]; p in Pa, ρ in kg/m³, g = 9.80665 m/s², h = depth of the section below the free surface in m, V in m/s. Screening inception: cavitation is predicted when −Cp_min(α, Re) ≥ σ. Critical speed at a given (α, Re):
V_crit = √( 2 (p_atm + ρ g h − p_v) / (ρ · (−Cp_min)) )  [m/s]; 1 kn = 0.514444 m/s.
Validity: attached, steady, single-phase Cp from a 2D solver; nuclei content, roughness, unsteadiness and 3D tip/junction effects are not in it; the Delft/Caltech data show inception can depart from −Cp_min through laminar separation and nuclei effects [S37][S7] *(Verified formula structure via [S22]; the caveats Inferred)*.
Worked values (Verified by computation from the stated inputs; inputs Flagged where recalled): ρ = 1025 kg/m³, p_atm = 101,325 Pa, p_v ≈ 2,300 Pa (≈20 °C — use the admitted ITTC table, ANA-15), h = 0.8 m ⇒ p_atm + ρ g h − p_v = 107,069 Pa. Then Cp_min = −0.5 → V_crit = 20.4 m/s = 39.7 kn; −0.8 → 16.2 m/s = 31.4 kn; −1.0 → 14.5 m/s = 28.1 kn. The repo's "4412 31.4 kn / E818 42.1 kn" numbers correspond to Cp_min ≈ −0.80 and ≈ −0.445 under inputs of this kind — a consistency check, not a validation.

**Reynolds number in water.** Re = V c / ν; ν ≈ 1.05 × 10⁻⁶ m²/s (sea water ≈ 20 °C; **Flagged** recall, the pinned ITTC 7.5-02-01-03 table in the repo governs). c = 0.12 m at V = 8 m/s → Re ≈ 9.1 × 10⁵; c = 0.06 m at 5 m/s → 2.9 × 10⁵; c = 0.15 m at 12 m/s → 1.7 × 10⁶.

**Ncrit and turbulence level (Mack correlation as implemented in XFOIL):** Ncrit = −8.43 − 2.4 ln(Tu), Tu = turbulence intensity as a fraction. Tu = 0.6 % → Ncrit ≈ 3.85 (matches Day et al.'s "Ncrit 4 ≈ 0.6 %" [S12]); Ncrit 3 → Tu ≈ 0.85 %; Ncrit 2 → Tu ≈ 1.3 %; Ncrit 1 → Tu ≈ 2.0 %; Ncrit 9 → Tu ≈ 0.07 % *(formula Flagged as recall; the numerical mapping Verified by computation)*.

**Evidence table for water Ncrit** *(all Verified as quotations)*:

| Source | Value | Basis | Weight |
|---|---|---|---|
| Speer, boatdesign.net, 5 Feb 2004 [S11] | "3 or even lower" | Particulates/organisms promote transition; talk by W. Feifel (Boeing) | Practitioner opinion; author adds "I don't have any test data to say what Ncrit should be" |
| Forum hearsay, same thread [S11] | 1 "a bit too extreme" | Unnamed yacht-design company | Hearsay |
| Sailing Anarchy / repo carry-over [S28] | 1 < Ncrit < 3 | Restatement of Speer | Tertiary |
| Day, Cocard, Troll 2019 [S12] | 4 (≈0.6 % Tu) | Used against full-scale tank data; "reasonably accurate in most cases" | Primary comparison, one foil |

Invariant for the product: a polar without a stored Ncrit is not admissible (existing ANA-15); the Ncrit preset must expose a **range** and the sources above, not a constant.

**Geometry invariants for admission (ANA-09, A3):**
- Chord normalised to [0, 1] with LE at (0, 0); TE gap recorded as-filed; format (Selig/Lednicer) detected, never assumed — all eight Eppler hydrofoil files on UIUC are Lednicer, the NACA files are mostly Selig, and `n63412.dat` is Lednicer *(Verified by execution)*.
- For a generated NACA section: t/c from the coordinates must equal the designation's last two digits within 0.05 % chord (measured here: 63-209 → 9.00 %, 64A410 → 9.99 %, 16-012 → 12.00 %, 4412 → 12.02 %); camber for a 6-series a = 1.0 line at design Cl 0.2 ≈ 1.1 %, at 0.4 ≈ 2.2 % (measured: 1.10 %, 2.20 %) *(Verified by execution)*.
- SHA-256 of the exact bytes vendored or generated, plus the generator name/version/command (TM 4741 build, point count, closure option) — the hash of a generated file is meaningless without the command.

**Design Cl (ANA-16).** Known from designation only for NACA 16- and 6-series (digit after the dash ÷ 10) and 5-digit (first digit × 0.15). Eppler and H105 design Cl are **Unknown** until read from the JSR papers/book with a citation — the UI must show "Unknown", not a computed Cl at zero α *(Verified rule; values Flagged)*.

## Design implications for CFD-Workbench

1. **Three admission classes, not one (CAT-01, ANA-09, COMMIT-02).** (a) *Generated at build* — every NACA 4/5/16/6/6A section via a TM 4741-faithful generator, hashed with its command; (b) *Vendored with terms* — only files whose rights are written down (currently none of the UIUC files qualify; obtain written permission from the UIUC Applied Aerodynamics Group or download-at-first-use from the canonical URL with hash verification and a per-file source record); (c) *Link-only / pending* — H105, Airfoil Tools, Hepperle, LSAT polars until a licence ruling. The picker's "Pending admission" state already exists; extend it with the *reason* (no terms / GPL data / permission requested on date).
2. **Do not ship UIUC LSAT polars under COMMIT-02 without a written exception (CAT-03, COMMIT-02).** The data licence is GPL with pass-through obligations; a permissive-only policy either excludes it or records a scoped exception ("data, not linked code, attribution shown in About and in every export").
3. **Preset fallbacks must be generated sections (CAT-03).** A clean offline install with "no Python" can still generate NACA 63-412, 64A410, 66-209, 0012 and 16-012 deterministically in C#; the four discipline presets should therefore default to generated NACA sections with bundled precomputed polars, and name the Eppler/H105 preferences as pending until class (b) is resolved.
4. **Strut/mast is a separate role with its own families (CAT-01, future surface).** Admit E836/E837/E838 (12.6/16.1/18.4 %), NACA 66-012/66-015-class, 16-012, 0009–0012 under role = strut; never show E862–E864 (32–39 % fairings) as mast candidates; record chord and t/c in mm-capable units because brands publish masts as "106 mm chord, 12–16 mm thick".
5. **Profile revision aggregate (A3) carries as-filed vs normalised geometry.** Store the raw file bytes and hash, the detected format, the normalisation (translation/scale/rotation), TE closure applied (none/blend/thickness), and resampling; TE gap and point count are attributes of the revision, because they move Cp_min.
6. **Design-Cl vs operating-Cl display (ANA-16).** Populate design Cl from designation parsing for NACA 16/6/5-series only; leave Eppler/H105 as Unknown with a "source needed" affordance; show the operating Cl from the solve beside it and the bucket half-width where the subscript exists (e.g. 65₂-415 → ±0.2).
7. **Ncrit default (ANA-15).** Keep a named water preset but make it a *pair* (e.g. 2 and 4) run and displayed together, labelled "practitioner range; no measured water N-factor; Day 2019 used 4"; a single number invites false precision. The polar cache key already includes Ncrit — extend the comparison legend (ANA-14) to show both.
8. **Cavitation screening is Cp_min-based and must say so (ANA-02/10).** Use σ and V_crit as defined above with the pinned fluid table; label the E817-family results "rooftop sections: bucket edges sensitive to Re" and H105-class "trades top speed for Cl,max" per Speer — as *design intent* text with a citation, never as computed numbers.
9. **Ranking at Re is computed, not measured (ANA-14).** Every ranking view must carry "NeuralFoil/XFOIL-class, vs XFOIL accuracy only; not validated against water experiment" until a section-level experiment (E817 SAFL data or Day 2019) is admitted as a fixture.
10. **Regression fixtures.** Generated NACA 0012 (t/c must read 12.00 %), 64A410 (Phase-0 continuity), 4412 (cavitation-poor baseline), SD7003 (low-Re LSB behaviour), 66(MOD) Cp taps and Delft Twist-11 geometry as future CFD-tier fixtures — the latter two only after rights are checked.
11. **Fins are a future surface with one admitted default** (generated 4-digit symmetric) and an open research item on fin templates.
12. **Roll the free-surface caveat into section results** (ANA-02): a 2D polar is deep-water; near-surface lift loss and wave drag need a 3D/Froude-aware tier (Day et al.'s +≈3 % is a model, not a constant).

### Proposed v1 catalog admission list (question 7)

Terms status key: **GEN** = generated at build from a public-domain definition (hash of output + generator command); **VEND?** = coordinate file exists on UIUC but no written redistribution terms — vendor only after permission, else download-at-first-use with hash; **LINK** = link/cite only; **PEND** = pending admission (no coordinates or terms in hand). Polars column names what *could* be admitted, not what exists in the repo today.

| Section | Role | t/c · camber (measured) | Design Cl / Re | Source of coordinates | Terms status | Polars available / admissible | Status for v1 |
|---|---|---|---|---|---|---|---|
| E817 | lifting (high-speed, rooftop) | 10.98 % · 2.88 % @0.69 | Unknown (JSR 1981 / book) · ship-scale | UIUC `e817.dat` (Lednicer) | VEND? | Generated NeuralFoil/XFOIL at Ncrit 2/4; SAFL inception data (papers, not admitted) | Admit as geometry after terms; polars generated |
| E818 | lifting (freeride/race) | 9.37 % · 2.79 % @0.67 | Unknown | UIUC `e818.dat` | VEND? | Generated | Same as E817 |
| E874 | lifting (thin, glide) | 7.90 % · 0.95 % @0.33 | Unknown (low-camber member) | UIUC `e874.dat` | VEND? | Generated | Same |
| E904 | lifting (low camber) | 9.00 % · 1.34 % @0.50 | Unknown | UIUC `e904.dat` | VEND? | Generated | Same |
| E908 | lifting (aft-loaded) | 9.00 % · 2.77 % @0.66 | Unknown | UIUC `e908.dat` | VEND? | Generated | Same |
| E836 / E837 / E838 | strut / mast | 12.64 / 16.10 / 18.37 % · 0 | Symmetric hydrofoil (JSR 1979) | UIUC | VEND? | Generated (symmetric, Cp_min vs α) | Admit under role = strut after terms |
| E862 / E863 / E864 | — (fairing struts, 32–39 %) | 32.37 / 35.73 / 38.82 % | — | UIUC | — | — | **Exclude** (not mast sections) |
| NACA 0009 / 0010 / 0012 | strut, fin, regression fixture | 9 / 10 / 12 % · 0 | — | GEN (TM 4741 4-digit; closed TE option recorded) | GEN | Generated; Report 824 LTPT polars (air) as fixture | Admit (0012 must read 12.00 %) |
| NACA 4412 | regression fixture / cavitation-poor baseline | 12.02 % · 4.00 % @0.40 | design-camber section, Cl≈0.4 at α≈0 is *not* a design Cl | GEN | GEN | Generated; Report 824 polars (air) | Admit as fixture, labelled "baseline, not recommended" |
| NACA 64A410 | lifting (practitioner pick) | 9.99 % · 2.66 % @0.50 | design Cl 0.4 · air | GEN (6A form) vs UIUC 51-pt file (Phase-0 continuity) | GEN | Generated; Phase-0 NeuralFoil polar | Admit (generated; keep UIUC-file hash as continuity check) |
| NACA 63-209 / 63-412 | lifting (Moth lineage) | 9.00 % · 1.10 % / 12.00 % · 2.20 % | design Cl 0.2 / 0.4 | GEN | GEN | Generated | Admit |
| NACA 66-209 / 66-210 / 66-012 / 66-018 | lifting (race) / strut | 9–10 % · 1.10 % / symmetric | design Cl 0.2 / 0 | GEN | GEN | Generated | Admit; 66-012-class flagged as the Day 2019 Moth strut match |
| NACA 16-012 / 16-021 / 16-309 | strut / lifting (historic, propeller family) | 12 / 21 % · 0; 9 % · Cl 0.3 | design Cl 0 / 0 / 0.3 | GEN (4-digit-mod, LE index 4, x_t 0.5) | GEN | Generated; ADA032272 (foil+strut, ranking only) | Admit with "poor recovery at low Re" note |
| NACA 5-digit (e.g. 23012) | generator capability only | — | Cl 0.3 | GEN | GEN | — | Do not list in picker |
| NACA 7-series | — | — | — | Report 824 tables only | — | — | Exclude from v1 |
| Speer H105 | lifting (low-Re sailing/wing/kite) | Unknown (no file in hand) | 20–30 kn intent; numbers unpublished | Author (e-mail/forum attachments) | PEND | None admissible | **Pending admission** (permission + coordinates + rights note) |
| S1223 | fixture (high-lift, take-off study) | 12.14 % · 8.67 % | Re 2 × 10⁵, Cl,max 2.2 (wind tunnel) | UIUC `s1223.dat` | VEND? | LSAT (GPL) — not admissible without ruling | Fixture only; not a foiling section |
| SD7003 | fixture (LSB behaviour) | 8.51 % · 1.46 % | low-Re benchmark (Flagged) | UIUC `sd7003.dat` | VEND? | LSAT (GPL) | Fixture only |
| AG24 / AG35 (Drela) | fixture (low Re) | 8.41 % · 2.22 % / 8.72 % · 4.37 % | DLG, Re ≈ 10⁵ (Flagged) | UIUC | VEND? | — | Not listed in picker |
| FX 63-137 | lifting (high camber, low Re) | 13.71 % · 5.97 % | Flagged | UIUC | VEND? | Aero J. TE-extension data (paper) | Not admitted for v1 |
| Clark Y | historic | 11.71 % · 3.43 % | — | UIUC | VEND? | — | Not admitted |
| Göttingen 796 / 797 | historic (hydrofoil claim unverified) | 12 % · 3.69 % / 16 % · 5.02 % (29–43 points) | — | UIUC | VEND? | — | Exclude until provenance cited |
| E387 / E393 | low-Re reference / Moth VPP use | 9.07 % · 3.80 % / 11.53 % · 4.00 % | E387: LSAT reference (Flagged) | UIUC | VEND? | LSAT (GPL) | Not admitted for v1 |
| NACA 66(MOD) (Shen & Dimotakis) | CFD-tier validation fixture | per paper | α 0–6°, 9–18 m/s | Paper / Caltech record | LINK | Cp taps, forces, cavitation | Future (CFD tier) |
| Delft Twist-11 (NACA 0009, twisted) | CFD-tier validation fixture | 9 % · 0 | c 150 mm, span 300 mm | Foeth 2008 thesis | LINK | Cavitation benchmark | Future (CFD tier) |
| Surface-piercing strut set (semi-ogive, NACA 0010-34 mod) | mast ventilation fixture | 10 % | Fr 0.5–2.5 | arXiv 2503.18015 | CC BY 4.0 | Ventilation maps | Future (mast surface) |

Discipline presets, corrected to this list: **surf / take-off** → generated NACA 63-412 (H105 pending); **wing freeride** → generated 64A410 or E818-after-terms; **SUP / downwind** → generated 63-209 or E874-after-terms; **windsurf / race** → generated 66-209 (16-309 only with the recovery warning). Each preset names its fallback and the reason (terms pending) in the picker, per CAT-03 *(Inferred from the table)*.

### Contradicts existing repo knowledge

- **"E818 best *measured* cavitation (42.1 kn vs 4412's 31.4)"** (`proposal-sequence.md` §3 catalog table). No measurement exists; the numbers are Phase-0 NeuralFoil Cp_min converted to V_crit (they correspond to Cp_min ≈ −0.445 and ≈ −0.80 under the worked inputs above). Evidence: no experimental cavitation data for E818 was found in any source this session; the E817 SAFL papers [S7][S8] are the only measured Eppler-hydrofoil cavitation data located. The word must become "computed (NeuralFoil, Ncrit = ?, Re = ?)" with the run's provenance — *(Verified absence; Inferred correspondence)*.
- **"Ranking flips with Re: E874 leads at 6e5, E904 at 1e6"** (same table; ANA-14 rationale). Not contradicted, but unsupported outside the repo's own Phase-0 run; it is a NeuralFoil result at an unstated Ncrit. Keep as *Inferred (own computation)*, re-run at Ncrit 2 and 4 before it appears in UI copy.
- **"Ncrit for water: Speer: 1 < Ncrit < 3"** (`proposal-sequence.md` §4.5; ANA-15 preset 2.0). The primary post says "3 or even lower" and disclaims test data; the one tank-compared study used 4. The repo's framing ("source-based assumption") is right; the numeric band should be 2–4, not 1–3 — *(Verified, [S11][S12])*.
- **"Coordinates exist (tspeer.com / Speer's boatdesign posts), not UIUC"** (H105 row). tspeer.com's H105 page carries no coordinates, and the cited validation thread has none from Speer; the claim that coordinates are on his site is unsupported. Source remains the author directly — *(Verified, [S9][S10])*.
- **"11 UIUC files are already vendored with hashes"** (proposal). Hashes establish integrity, not rights: the UIUC database has no stated licence, so vendoring is class VEND? until permission — *(Verified, [S1][S2])*.
- **Strut family wording "Eppler E836, E837, E838; NACA 0009, 0010, 0012 — zero camber, 12–18 % t/c"** — confirmed by measurement (12.64/16.10/18.37 %); add the caution that UIUC's *named* "strut airfoils" E862–E864 are 32–39 % fairings and must not be confused with the mast set — *(Verified by execution)*.
- **"Göttingen 796/797 (classic hydrofoil)"** (this task's premise, not yet in the repo). Unverified; do not add it.

## Open questions and domain failure modes

**Open questions (cheapest next probe first):**
- *UIUC coordinate terms.* One e-mail to the UIUC Applied Aerodynamics Group webmaster asking for a written statement on redistribution of `coord/*.dat` in a commercial desktop product; until then class (c).
- *H105 coordinates and terms.* One e-mail to Tom Speer; the boatdesign thread and the H105 page show no coordinates. Also ask for design Re/Cl/t/c.
- *Eppler design Cl/Re per section.* Obtain JSR Part 2 (1981) and Part 3 (1985) or the 1990 book plates; one library request.
- *Water Ncrit.* No measured N-factor in water was found. Cheapest probe: run the catalog at Ncrit 2/3/4 against Day et al. 2019 lift–drag data and report which reproduces it best — a bounded spike, but only a one-foil calibration.
- *E817 SAFL inception σ values.* The ASME abstracts were HTTP 403; retrieve the two JFE papers for σ_i vs α to seed a section-level experimental fixture.
- *Göttingen 796/797 "hydrofoil" provenance.* Not confirmed; drop the label or find the citation.
- *Fin section templates.* No primary source found; survey Futures/FCS technical pages or drop fins from v1 text.
- *Mast thickness by brand.* Armstrong page gives chord and "12 mm" but summaries elsewhere say 13.8–15.8 mm; read a datasheet before quoting.
- *Airfoil Tools polar settings* (Ncrit 5/9 recall) and *XFOIL NACA-command scope* — one visit each to confirm; both are Flagged now.

**Domain failure modes:**
- *Silent format misread* (Lednicer as Selig) yields a plausible thin section — Phase 0 already hit this; every Eppler hydrofoil file is Lednicer, so a default-Selig parser fails on exactly the flagship set.
- *Coarse coordinate files* (16-series 33 points, Go 797 29 points) give under-resolved suction peaks → Cp_min under-read → cavitation margin over-stated (unsafe direction).
- *Air Ncrit (9)* applied to water shows a laminar bucket that does not exist at sea; *Ncrit 1* removes buckets that may exist — both are confident wrong numbers.
- *Design Cl guessed from the shape* (Cl at α = 0) presented as "design Cl" — misleads stabilizer sizing.
- *Licence drift*: bundling UIUC files or LSAT data because "everyone does" (AeroSandbox bundles the database) is not a right; a takedown after release is expensive.
- *Rooftop sections at low Re*: E817-class sections separate at the recovery at Re ≲ 5 × 10⁵ and moderate α — a take-off/pumping regime — yet rank well at high-speed Cp_min; single-point ranking hides this.

## Disconfirming views sought

- **"UIUC coordinates are free to use" (counter to finding 1).** The counter-argument is common practice: AeroSandbox, Airfoil Tools and countless repos redistribute the files. Sought and fared: practice is not a licence; the database page and FAQ contain no terms (both opened). The finding stands; the fix is a written statement or download-at-use.
- **"GPL on data is unenforceable / irrelevant" (finding 2).** Counter: copyright in measured data is thin in the US. Fared: the Manifesto's conditions are what the authors ask for regardless of enforceability; COMMIT-02 is a policy, not a legal analysis. Stands, as a policy ruling to make.
- **"Ncrit for water is 1–3, settled" (finding 7).** Counter: Speer is the most experienced practitioner and Feifel's Boeing experience backs him. Fared: Speer himself disclaims test data; the only primary tank comparison used 4. The finding (a band, not a constant) stands.
- **"H105 is the right default for wingfoil" (finding 6).** Counter: Moth use (Axiom) and Speer's rationale fit the 20–30 kn regime exactly. Fared: the rationale is Verified but no coordinates, terms or numbers are; the recommendation cannot enter the catalog on prose. Stands as pending.
- **"6-series beats 16-series" (finding 10).** Counter: 16-series has the deeper bucket at high speed and is the propeller standard. Fared: ADA032272 measures higher L/D for 64A309 in a flapped foil-plus-strut at PCH-like conditions; the 16-series advantage is at higher σ-limited speeds than water sports reach. Stands as a ranking under those conditions only.
- **"NeuralFoil is accurate to 0.4 %" (finding 8).** Counter: the paper's numbers are precise. Fared: they are relative to XFOIL, and XFOIL vs water experiment is itself only loosely tested (Day 2019). Stands with the two-link caveat.
- **"Göttingen 796/797 are classic hydrofoil sections" (task premise).** Sought a source; none found. The premise is now Flagged, not repeated.

## Glossary terms

- **Rooftop pressure distribution** — a design target with constant (minimum) pressure over the forward upper surface at the design angle, maximising the cavitation-free Cl range for a given −Cp_min; Eppler's hydrofoil philosophy. *(Verified, [S9][S5])*
- **Cavitation bucket** — the region in the (Cl, σ) plane where −Cp_min < σ; its width is the α/Cl tolerance and its depth the speed tolerance. *(Verified, [S5][S22])*
- **Cavitation number σ** — (p∞ − p_v)/(½ρV²) with p∞ including hydrostatic depth. *(Verified, [S22])*
- **Ncrit** — the e^N amplification exponent at which XFOIL/NeuralFoil declare transition; lower means earlier transition; related to turbulence intensity by Mack's correlation. *(Verified use, [S11][S12]; formula Flagged)*
- **Laminar (drag) bucket** — the Cl range over which a laminar-flow section keeps low drag; for 6-series, centred on the design Cl with half-width given by the subscript. *(Inferred, [S16][S27])*
- **"A" modification (6A-series)** — 6-series thickness form with straight rear surfaces from ≈0.8c to a thicker TE. *(Inferred, [S16])*
- **16-series** — NACA 4-digit-modified thickness form with LE index 4 and maximum thickness at 0.5c; designation 16-*d*ₗ*tt*. *(Verified, [S17])*
- **Selig / Lednicer format** — Selig: one loop from upper TE around the LE to lower TE; Lednicer: a point-count line then upper LE→TE, then lower LE→TE. *(Verified, [S1] and by execution)*
- **Ventilation** — atmospheric air drawn onto a low-pressure surface (nose, tail or base) of a surface-piercing body; distinct from vapour cavitation. *(Verified, [S20])*
- **Base-vented section** — a blunt-TE strut/foil section whose base is deliberately ventilated to atmosphere to stabilise the wake. *(Verified description, [S33][S20])*
- **Surface-piercing strut** — a strut crossing the free surface (mast); its ventilation stability depends on Fr, AR and α. *(Verified, [S20])*
- **Design Cl** — the lift coefficient the section's mean line was designed for (NACA: encoded in the designation); distinct from the solved operating Cl. *(Verified, [S16])*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | UIUC Airfoil Coordinates Database (Version 2.0) + coordinate files e817…sd7003 | primary | https://m-selig.ae.illinois.edu/ads/coord_database.html ; https://m-selig.ae.illinois.edu/ads/coord/ | 2026-09-20 | Terms absence, listing, downloaded files, measured geometry |
| S2 | UIUC Applied Aerodynamics Group FAQ | primary | https://m-selig.ae.illinois.edu/ads_faq.html | 2026-09-20 | No licence statement; copyright notice |
| S3 | UIUC Low-Speed Airfoil Tests — data licence page | primary | https://m-selig.ae.illinois.edu/pd.html | 2026-09-20 | GPL/Manifesto conditions |
| S4 | Eppler & Shen, "Wing Sections for Hydrofoils — Part 1: Symmetrical Profiles", JSR 23(3) 1979 | primary (listing) | https://onepetro.org/JSR/article/23/03/209/175531/ | 2026-09-20 | Symmetric hydrofoil family provenance |
| S5 | Shen & Eppler, "Wing Sections for Hydrofoils — Part 2: Nonsymmetrical Profiles", JSR 25(3) 1981 | primary (listing) | https://onepetro.org/journal-paper/SNAME-JSR-1981-25-3-191 | 2026-09-20 | Non-symmetric family; bucket adapted to application |
| S6 | Shen, "Wing Sections for Hydrofoils — Part 3: Experimental Verifications", JSR 29(1) 1985 | primary (listing) | https://onepetro.org/journal-paper/SNAME-JSR-1985-29-1-39 | 2026-09-20 | Experimental verification exists |
| S7 | "An Experimental Investigation of Cavitation Inception and Development on a Two-Dimensional Eppler Hydrofoil", ASME JFE 122(1) 2000 | primary (abstract via search; page 403) | https://asmedigitalcollection.asme.org/fluidsengineering/article-abstract/122/1/164/463672/ | 2026-09-20 | E817 cavitation tests |
| S8 | "Scale Effect of Cavitation Inception on a 2D Eppler Hydrofoil", ASME JFE 124(1) 2002 | primary (abstract) | https://asmedigitalcollection.asme.org/fluidsengineering/article-abstract/124/1/186/462766/ | 2026-09-20 | Scale effect on inception |
| S9 | T. Speer, "H105 — Low Reynolds Number Hydrofoils" (page dated 1999-01-16) | primary (author page) | http://www.tspeer.com/Hydrofoils/h105/h105.htm | 2026-09-20 | H105 intent; absence of coordinates/terms |
| S10 | boatdesign.net thread "Hydrofoil exercise to validate CFD analysis" (Speer posts, Oct 2014) | tertiary | https://www.boatdesign.net/threads/hydrofoil-exercise-to-validate-cfd-analysis.51552/ | 2026-09-20 | No H105 coordinates posted; Kuhn & Scragg 1993 citation |
| S11 | boatdesign.net thread "XFOIL for hydrofoils" (Speer posts, Feb 2004) | tertiary | https://www.boatdesign.net/threads/xfoil-for-hydrofoils.2909/ | 2026-09-20 | Origin of the water-Ncrit rule and its disclaimer |
| S12 | Day, Cocard, Troll, "Experimental measurement and simplified prediction of T-foil performance for monohull dinghies", 23rd CSYS, 2019 | primary | https://strathprints.strath.ac.uk/67655/1/Day_etal_CSYS2019_Experimental_measurement_and_simplified_prediction_of_T_foil.pdf | 2026-09-20 | Ncrit 4, Moth sections, free-surface model, accuracy statement |
| S13 | Sharpe & Hansman, "NeuralFoil: An Airfoil Aerodynamics Analysis Tool Using Physics-Informed Machine Learning", arXiv 2503.16323 (2025) | primary (abstract) | https://arxiv.org/abs/2503.16323 | 2026-09-20 | Accuracy and Re-range claims |
| S14 | AeroSandbox source `airfoil_families.py` and LICENSE.txt; NeuralFoil LICENSE.txt | primary (source) | https://github.com/peterdsharpe/AeroSandbox ; https://github.com/peterdsharpe/NeuralFoil | 2026-09-20 | 4-digit-only generator; bundled UIUC copy; MIT |
| S15 | `airinnova/airfoils` LICENSE.txt (Apache-2.0); OpenVSP LICENSE (NOSA) | primary (source) | https://github.com/airinnova/airfoils ; https://github.com/OpenVSP/OpenVSP | 2026-09-20 | Tool licences |
| S16 | Ladson, Brooks, Hill, Sproles, "Computer Program To Obtain Ordinates for NACA Airfoils", NASA TM 4741, 1996 | primary | https://ntrs.nasa.gov/citations/19970008124 | 2026-09-20 | Generator scope and tolerance |
| S17 | PDAS "References to NACA airfoil geometry" (16-series definition; TN 1546; TM X-3284) | secondary | https://www.pdas.com/naca456refs.html | 2026-09-20 | 16-series definition, report numbers |
| S18 | Layne, "Lift and Drag Characteristics of NACA 16-309 and NACA 64A309 Hydrofoils", DTNSRDC, DTIC ADA032272 | primary (abstract) | https://apps.dtic.mil/sti/citations/ADA032272 | 2026-09-20 | Measured 16- vs 6-series ranking |
| S19 | Armstrong Foils, "The Carbon Mast Range — FAQs" | primary (vendor) | https://armstrongfoils.com/blogs/resources/the-carbon-mast-range-faqs | 2026-09-20 | Published mast chord/thickness/stiffness |
| S20 | Aguiar Ferreira, Navas Rodríguez, Jacobi, Fiscaletti, Greidanus, Westerweel, "On the ventilation of surface-piercing hydrofoils under steady-state conditions", arXiv 2503.18015 (2025, CC BY 4.0) | primary | https://arxiv.org/abs/2503.18015 | 2026-09-20 | Strut ventilation regimes |
| S21 | Airfoil Tools site (footer terms; airfoil count) | primary (site) | http://airfoiltools.com/ | 2026-09-20 | No-reproduction terms |
| S22 | M. Hepperle, "Special considerations for hydrofoils" (2018-05-21) | secondary | https://www.mh-aerotools.de/airfoils/hydrofoils.htm | 2026-09-20 | Cp_crit dependence; usable Cl vs speed; page terms |
| S23 | "Design Optimization of America's Cup AC75 Hydrofoil Sections with Flaps", Journal of Sailing Technology 10(1), 2025 | primary (abstract) | https://onepetro.org/JST/article/10/01/50/681201/ | 2026-09-20 | Cp_min-constrained multipoint optimisation |
| S24 | "Optimization of hydrofoil leading-edge geometry for delaying incipient cavitation using a CFD–GA approach" (2025, PMC) | primary (abstract) | https://pmc.ncbi.nlm.nih.gov/articles/PMC13250433/ | 2026-09-20 | Frontier LE optimisation |
| S25 | Eppler, *Airfoil Design and Data*, Springer 1990, ISBN 3-540-52505-X (DNB record) | primary (catalogue record) | https://d-nb.info/901173800/04 | 2026-09-20 | Existence; TOC not readable |
| S26 | Arndt, "Some Remarks on Hydrofoil Cavitation", J. Hydrodynamics 2012 | primary (listing) | https://link.springer.com/article/10.1016/S1001-6058(11)60249-7 | 2026-09-20 | Review of hydrofoil cavitation physics |
| S27 | Abbott, von Doenhoff, Stivers, "Summary of Airfoil Data", NACA Report 824, 1945 | primary (availability) | https://ntrs.nasa.gov/ (Report 824) | 2026-09-20 | Public-domain tables/nomenclature |
| S28 | Sailing Anarchy "Hydrofoil sections"; boatdesign "Eppler 387 vs H105"; "Foil section? For foil board" | tertiary | https://forums.sailinganarchy.com/threads/hydrofoil-sections.82598/ ; https://www.boatdesign.net/threads/eppler-387-vs-h105-for-kite-hydrofoil.59737/ | 2026-09-20 | Moth section usage lore; Ncrit restatement |
| S29 | Beaver & Zseleczky, "Full Scale Measurements on a Hydrofoil International Moth", 19th CSYS 2009 | primary (listing) | https://onepetro.org/SNAMECSYS/proceedings/CSYS09/2-CSYS09/D021S002R006/461904 | 2026-09-20 | Moth full-scale data exists |
| S30 | "Fluid Forces on a Hydrofoil for Kitefoil and Windfoil Boards: … Froude Number Based on Submergence", J. Marine Sci. Appl. 2025 | primary (title; body not opened) | https://link.springer.com/article/10.1007/s11804-025-00708-2 | 2026-09-20 | Free-surface effect framing |
| S31 | Selig & Guglielmo, "High-Lift Low Reynolds Number Airfoil Design", J. Aircraft 34(1) 1997 | primary (abstract) | https://m-selig.ae.illinois.edu/pubs/GuglielmoSelig-1997-JofAC-S1223.pdf | 2026-09-20 | S1223 design Re/Cl,max |
| S32 | "Effect of trailing edge extensions on … Göttingen 797 and Wortmann FX 63-137 … Re 3×10⁵–1×10⁶", Aeronautical Journal | primary (title) | https://cambridge.org/core/journals/aeronautical-journal/ | 2026-09-20 | Low-Re data exist for Go 797 / FX 63-137 |
| S33 | IHS, "Hydrofoil, Rudder, and Strut Design Issues" (compiled Q&A, foils.org) | tertiary | https://foils.org/wp-content/uploads/2017/09/Hydrofoil-Rudder-and-Strut-Design-Issues.pdf | 2026-09-20 | Strut/ventilation lore; section list incl. H105 |
| S34 | Vendor/forum mast specs (Sabfoil ≈14 mm; "10–12.5 % at ≈120 mm") | tertiary | https://www.seabreeze.com.au/forums/ ; https://www.hydrofoiling.org/ | 2026-09-20 | Flagged mast thickness range |
| S35 | "Numerical Study on the Performance of Thin NACA Airfoils with Moderately Truncated Trailing Edges", Int. J. Aeronaut. Space Sci. 2026 | primary (title) | https://link.springer.com/article/10.1007/s42405-026-01201-y | 2026-09-20 | TE truncation study exists |
| S36 | XFOIL home page (MIT) — "released under the GNU General Public License" | primary | https://web.mit.edu/drela/Public/web/xfoil/ | 2026-09-20 | XFOIL licence |
| S37 | Shen & Dimotakis, "Viscous and Nuclei Effects on Hydrodynamic Loadings and Cavitation of a NACA 66(MOD) Foil Section", JFE 111(3) 1989 (Caltech record) | primary (abstract) | https://authors.library.caltech.edu/records/r0c75-1ez89 | 2026-09-20 | Cavitation-tunnel fixture |
| S38 | Delft Twist-11 hydrofoil (Foeth 2008) as described in Ocean Engineering / J. Hydrodynamics papers | secondary | https://www.sciencedirect.com/science/article/pii/S0301932212001735 | 2026-09-20 | Benchmark geometry |
