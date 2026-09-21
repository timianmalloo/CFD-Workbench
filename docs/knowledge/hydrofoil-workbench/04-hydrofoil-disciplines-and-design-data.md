---
id: kb-hw-hydrofoil-disciplines-and-design-data
title: "Hydrofoil disciplines and design data for water-sports foils"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, disciplines, wingfoil, windfoil, kitefoil, downwind, pump-foil, e-foil, parawing, product-specs, class-rules, operating-points, cavitation, ventilation, free-surface]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes, per water-sports discipline, the rider/craft system, speed envelope, design-CL and
  Reynolds bands, real product geometry bands (30+ front wings from official and retailer spec pages),
  the racing class rules that bound geometry, and the operating physics (free surface, ventilation,
  cavitation, pumping) with cited numbers. Main design implication: discipline presets and operating
  points must carry explicit, labelled speed × load × depth × water inputs, and the analysis tier must
  bound its output against the product table and the cavitation/free-surface envelopes here.
---

# Hydrofoil disciplines and design data for water-sports foils

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. Per discipline: rider/craft system, speeds, envelope, design-CL and Re bands, and what riders optimize for.
2. Front-wing, stabilizer, mast and fuselage geometry bands grounded in actual product specs.
3. Racing class rules that constrain geometry (iQFOiL, Formula Kite, GWA wingfoil, Moth, PWA, parawing).
4. Operating physics that shape design (free surface, ventilation, cavitation, pumping, stall/take-off, water properties).
5. Real-world validation and measurement data usable to sanity-bound the estimator.
6. The user's goal-state (90 kg rider, race wingfoil, salt water, 10–20 kn wind) as derived operating points.
7. Product-line trends 2023–2026 and their implications for a design tool.

Labels: **Verified** = the cited primary/official source was opened this session; **Inferred** = arithmetic
or reasoning on Verified inputs, or an official number reached only through a retailer/search snippet;
**Flagged** = recall, single tertiary source, or a source that could not be opened. Retailer and forum
sources are marked *tertiary* in the source table. All arithmetic uses ITTC 7.5-02-01-03 Rev03 water
properties [S1] and 1 kn = 0.514444 m/s.

## Headline findings

1. Manufacturers' aspect-ratio conventions are inconsistent: Armstrong, Code, Lift, Unifoil, North and Sabfoil (1160, 1400) match span²/area within ±1%, but Axis ART Pro values run ~2% above span²/area and the Sabfoil Leviathan Pro 1060 retailer figure runs ~3% below. A tool must derive AR from its own projected geometry and show the convention. — *(Inferred from Verified product numbers, [S2][S3][S6][S7][S8][S9][S10][S11])*
2. The whole consumer market sits in a narrow AR band: 7.1–7.6 (Armstrong MA, "stability at speed plus fast roll rate"), 8–9.5 (Unifoil Progression, Code S, F-One SK8/Seven Seas), 9.6–11 (Armstrong HA, Axis ART v2, North Sonar HA), 11–13.1 (Axis ART Pro, Lift HA-X, Sabfoil Leviathan). Areas span 480–1880 cm², spans 690–1380 mm. — *(Verified/Inferred, [S2]–[S11])*
3. GWA wingfoil race is a box rule, not open: production ≥50 pieces, front wing ≥700 cm², rear wing ≥150 cm², mast ≤115 cm, fuselage ≤100 cm, foil ≥3 kg, board ≥3 kg, max 3 foil systems and 5 wings per season. — *(Verified, [S12])*
4. iQFOiL is one-design: 900 cm² front wing (Tom Speer profile, "thin and relatively symmetrical"), 255 tail at −2° default (−2° to +1° by shim), 115 Plus fuselage, 95 cm mast; juniors 800 cm² + aluminium mast. — *(Verified, [S13])*
5. Formula Kite registers production hydrofoils with measurement tolerances: weight ±4%, general ±0.4 mm, height/length ±2 mm, chord −1 mm, front/rear-wing span ±2 mm; 10 pieces per item are 3D-scanned. Dimensional maxima live in the class rules PDF that could not be opened. — *(Verified tolerances, Flagged maxima, [S14][S15])*
6. Free-surface loss is large in the racing regime: a tank test of a kitefoil/windfoil wing (AR ≈ 6.8, Re 7.3×10⁴–2.9×10⁵, Fr_h 0.4–6.7) shows ≈17% lift-coefficient loss between Fr_h = 2 and 5 at h/c = 4, effects concentrated at h/c < 4, and deep-water asymptote only at h/c > 5. Race foils at 20 kn and 0.3–0.5 m depth sit at Fr_h ≈ 4.6–6 and h/c ≈ 3–6, i.e. inside the affected region. — *(Verified paper, Inferred placement, [S16])*
7. Ventilation on Olympic kitefoil masts correlates with the *rate* of angle-of-attack change and vertical acceleration, not with their static values; a hysteresis model fits; surface treatment is secondary. Steady tank tests on surface-piercing foils (Fr 0.5–2.5) identify nose, tail and base ventilation triggers with a bistable region wider than earlier stability maps. — *(Verified abstracts, [S17][S18])*
8. Cavitation ceiling: with seawater at 15 °C, 0.5 m depth, σ = 0.86 at 30 kn, 0.63 at 35 kn, 0.48 at 40 kn, 0.38 at 45 kn. A section whose −Cp_min at the operating CL is 0.4–0.65 therefore cavitates between ≈35 and 45 kn, which is the practical ceiling seen in kite/wind racing (Paris 2024 Formula Kite "up to 70 km/h" ≈ 38 kn; wingfoil 1 s record 37.89 kn). — *(Inferred arithmetic on Verified [S1]; speeds secondary/tertiary [S19][S20])*
9. Seawater vs fresh water is not a density multiplier alone: at 15 °C ρ_sea/ρ_fresh = 1026.02/999.10 = 1.0269 (+2.7% force at fixed CL) but ν_sea/ν_fresh = 1.1892/1.1386 = 1.044, so Re is 4.3% lower in the sea at the same V·c; a 10 → 30 °C swing changes Re by ×1.87 (sea) at fixed V·c. — *(Verified [S1], Inferred arithmetic)*
10. Design-CL bands implied by manufacturer-declared envelopes: Sabfoil states 7–9 kn take-off and 16–18 kn top for a 1549 cm² wing (80 kg rider) and 10–12 kn / 25–27 kn for a 947 cm² wing; with a ≈93 kg system these imply CL ≈ 0.6–0.7 at take-off and ≈ 0.10–0.12 at top speed. Race foils run CL ≈ 0.1–0.25 in flight and ≈ 0.6–0.8 at take-off. — *(Verified specs [S9][S10], Inferred CL, Flagged system mass)*
11. Reynolds bands (seawater 15 °C): surf 4×10⁵–1.2×10⁶; pump 4×10⁵–7×10⁵; downwind 4×10⁵–8×10⁵; wingfoil race 5×10⁵–1.2×10⁶; iQFOiL/slalom windfoil 7×10⁵–1.6×10⁶; kitefoil race 7×10⁵–1.4×10⁶; e-foil 8×10⁵–2×10⁶. All are in the laminar-bubble-sensitive regime where Eppler ranking flips (E874 at 6×10⁵ vs E904 at 10⁶ per the existing catalog note). — *(Inferred arithmetic; chord bands from [S2]–[S11]; speeds Flagged where noted)*
12. Pump foiling operates at a low Strouhal number: a 1–2 Hz cadence with ≈0.2–0.4 m heave at 4–6 m/s gives St ≈ 0.05–0.15, far below the 0.25–0.40 propulsive optimum of the flapping-foil literature; glide (L/D) dominates, which is why pump foils trend to AR 9–11 (Armstrong APF 8.6–10.7). — *(Inferred; cadence/heave Flagged, St optimum secondary [S21], APF specs [S3])*
13. The 90 × 165 mm four-bolt plate is the de-facto mast–board interface (F-One offers 160 × 90 and 165 × 90); Deep Tuttle remains the windfoil/iQFOiL standard. Wing–fuselage connections are proprietary (Sabfoil T8/MH172, Axis, Armstrong). — *(Flagged: tertiary [S22]; iQFOiL Deep Tuttle Verified [S13])*
14. Two official research foils exist as validation anchors for a consumer-scale hydrofoil: the CEHINAV kitefoil/windfoil model (0.059 m², 0.634 m span, 0.0735 m mean chord, h/c 0.5–9.5, α −5° to +10°) [S16] and the DELFT surface-piercing models (semi-ogive and modified NACA 0010-34, AR 1.0/1.5) [S18]. No public GPS/IMU dataset of a consumer wingfoil race foil was found. — *(Verified, gap Flagged)*
15. Parawing downwind racing gained World Cup status in 2026 with no published equipment box rule found; PWA foil slalom from 1 Jan 2026 limits equipment to 1 mast, 2 front wings, 2 back wings, 2 fuselages and 3 sails ≤ 8.0 m². — *(Flagged: secondary/tertiary [S23][S24])*

## State of the art

### The rider/craft system per discipline

The system a design tool must model is rider + board + foil (+ hand-held wing, kite, parawing or motor/battery), moving at a speed set by the drive (wind, wave, muscle or motor) and supported by front-wing lift minus stabilizer down-force. The table below records what could be tied to a source; everything else is Flagged. Speeds are in knots with m/s in brackets.

| Discipline | Rider mass (kg) | Added mass (kg) | Drive power / envelope | Take-off · cruise · top (kn) | Optimizes for | Label |
|---|---|---|---|---|---|---|
| Wingfoil freeride | 55–110 (size charts) | board 4–8 [Flagged], foil 3.5–5 [Inferred from 2.19 kg front wing alone, S9], wing 1.8–2.6 (Duotone Unit 3.5–6.5 m², S25) | wind 10–25 kn for a 5.0 m² wing, rider >70 kg [S25] | 8–10 · 12–18 · 20–25 [Flagged] | balance of early lift, glide, carve | Inferred/Flagged |
| Wingfoil race (GWA box) | 60–95 [Flagged] | board ≥3 kg, foil ≥3 kg (rule minima, S12); wing 2.2–2.6 | wind: races held 5–35 kn for iQFOiL-style fleets [S26, secondary]; GWA wind limits Flagged | 9–12 · 15–25 · 30–37.9 (1 s record 37.89 kn [S20, tertiary]) | top speed, upwind VMG, ventilation resistance, stability at speed | Inferred/Flagged |
| Windfoil iQFOiL | Olympic athletes, ~70–90 M / ~55–70 W [Flagged] | board + rig + foil; foil one-design [S13] | wind 5–35 kn (package design range) [S26] | 10–12 · 18–28 · 30–35 [Flagged] | course racing speed and pumping efficiency in light wind | Verified equipment, Flagged speeds |
| Windfoil slalom/Formula (PWA) | 75–100 [Flagged] | sails ≤8.0 m² (2026 foil slalom) [S24, tertiary] | wind 7–35 kn [Flagged] | 12 · 25–30 · 35+ [Flagged] | top speed, cavitation margin, jibe stability | Flagged |
| Kitefoil race (Formula Kite) | ~65–85 M, ~50–65 W [Flagged] | kites ≤21 m² men / ≤19 m² women from 10 Aug 2024 [S15, secondary]; foil systems registered [S14] | wind 6–35 kn [Flagged] | 8–10 · 25–32 · ≈38 ("up to 70 km/h" Paris 2024 [S19]) | top speed, ventilation resistance (main athlete-reported limit [S17]) | Verified rule items, Flagged speeds |
| Surf foil (prone) | 55–100 | board 3–5, foil 3.5–5 [Flagged] | wave-driven; no published wave-energy band found | 6–8 · 10–15 · 18–22 [Flagged] | early lift, turning (roll rate, low second moment of area [S2]), ventilation recovery on breach [S3], pump to reconnect | Flagged |
| Pump foil / dock start | 50–95 | board 2–4 [Flagged], foil 3.5–5 | muscle: cadence ≈1–2 Hz [Flagged, S27] | 6–8 · 8–12 · 14 [Flagged]; Sabfoil Leviathan 1400 states 7–9 kn take-off, 16–18 kn top for 80 kg [S9] | glide (L/D), low take-off speed, pumping efficiency | Verified where cited |
| Downwind (SUP/prone) | 60–100 | SUP board 6–9 [Flagged], paddle | ocean swell/bump; Sabfoil Leviathan Pro 1060: 10–12 kn take-off, 25–27 kn top for 80 kg [S10, tertiary] | 7–10 · 12–18 · 25–27 | glide, early lift at low speed, high AR/long span | Inferred |
| Parawing foiling | as downwind | parawing 0.3–0.8 [Flagged] | wind 12–30 kn [Flagged]; World Cup status 2026 [S23] | as downwind; upwind legs exist [S23] | downwind glide plus upwind ability | Flagged |
| E-foil | 40–120 | board 20–21 without battery, battery 8.75–13 [S28, tertiary] | motor 4–6 kW (Waydoo EVO) [S28, tertiary]; Fliteboard cells "up to 45 min / 1.5 h / 2.5 h" [S29] | 6–8 · 12–18 · 21.6–24 (40–45 km/h [S28]) | stability, low take-off speed at high system mass, battery range | Verified Fliteboard ride times only |

Practitioner claims (tertiary) that a wingfoil racer's *foil* is chosen by wind band — ≈1000–1300 cm² at 8–12 kn, ≈700–900 cm² at 15–25 kn — are consistent with the GWA 700 cm² floor [S12] and the Axis ART Pro / Lift HA-X size ladders [S2][S6], but were not confirmed by a race-results dataset (Flagged; the IWSA equipment articles returned 404/403 this session [S30]).

### Front-wing geometry bands from product specs

The table records official spec pages (primary, Verified) and retailer spec sheets (tertiary, Inferred). AR_calc = span²/area computed here; AR_stated is the manufacturer's number. Accessed 2026-09-20.

| Brand · model | Area cm² | Span mm | AR_stated | AR_calc | Discipline (maker's words) | Source · type |
|---|---|---|---|---|---|---|
| Axis ART v2 1099 | 1220 | 1099 | ~10 | 9.90 | winging; prone/DW/SUP/wind/kite | S2 official |
| Axis ART v2 999 | 1038 | 999 | ~10 | 9.61 | as above | S2 official |
| Axis ART v2 939 | 900 | 939 | ~10 | 9.80 | as above | S2 official |
| Axis ART v2 879 | 790 | 879 | ~10 | 9.78 | as above | S2 official |
| Axis ART v2 819 | 647 | 819 | ~10 | 10.37 | as above | S2 official |
| Axis ART Pro 1201 | 1318 | 1201 | 11.14 | 10.94 | speed/glide, 12+ AR marketing | S31 tertiary |
| Axis ART Pro 1121 | 1088 | 1121 | 11.78 | 11.55 | | S31 tertiary |
| Axis ART Pro 1051 | 932 | 1051 | 12.05 | 11.85 | | S31 tertiary |
| Axis ART Pro 1001 | 845 | 1001 | 12.06 | 11.86 | root chord 105 mm listed → mean 84 mm | S31 tertiary |
| Axis ART Pro 901 | 686 | 901 | 12.04 | 11.83 | chord 93 mm | S31 tertiary |
| Axis ART Pro 801 | 582 | 801 | 11.17 | 11.02 | chord 86 mm | S31 tertiary |
| Axis ART Pro 751 | 518 | 751 | 11.10 | 10.89 | | S31 tertiary |
| Armstrong MA625 | 625 | 690 | 7.6 | 7.62 | mid-aspect, "stability at speed + fast roll rate" | S3 official |
| Armstrong MA800 | 800 | 765 | 7.2 | 7.31 | | S3 official |
| Armstrong MA1000 | 1000 | 850 | 7.2 | 7.23 | | S3 official |
| Armstrong MA1225 | 1225 | 935 | 7.1 | 7.14 | | S3 official |
| Armstrong MA1475 | 1475 | 1050 | 7.5 | 7.47 | | S3 official |
| Armstrong MA1750 | 1750 | 1152 | 7.5 | 7.58 | | S3 official |
| Armstrong HA480 | 480 | 726 | 11 | 10.98 | high aspect | S32 tertiary |
| Armstrong HA680 | 680 | 818 | 9.80 | 9.84 | | S32 tertiary |
| Armstrong HA980 | 980 | 974 | 9.68 | 9.68 | | S32 tertiary |
| Armstrong HA1180 | 1180 | 1066 | 9.63 | 9.63 | | S32 tertiary |
| Armstrong APF1350 | 1350 | 1202 | 10.7 | 10.70 | pump foil | S32 tertiary |
| Armstrong APF1675 | 1675 | 1202 | 8.6 | 8.63 | pump foil | S32 tertiary |
| Armstrong APF1880 | 1880 | 1302 | 9.2 | 9.02 | pump foil | S32 tertiary |
| Lift 90 HA | 580 | 810 | 11 | 11.31 | HA pump/wave | S6 tertiary |
| Lift 110 HA-X | 710 | 965 | 13.1 | 13.11 | HA-X specialty | S6 tertiary |
| Lift 150 HA-X | 970 | 990 | 10.1 | 10.10 | | S6 tertiary |
| Lift 170 HA | 1100 | 940 | — | 8.03 | | S6 tertiary |
| Code 720S | 720 | 830 | 9.5 | 9.57 | "loose and surfy" | S7 tertiary |
| Code 850S | 850 | 900 | 9.5 | 9.53 | all-round | S7 tertiary |
| F-One Seven Seas 1200 | 1200 | 950 | 7.6 (range 7.5–8.0) | 7.52 | wing freeride | S8 tertiary |
| F-One SK8 (550–1050) | 550–1050 | — | 8.0 | — | surf/wing | S8 tertiary |
| Unifoil Progression 100 | 645 | 824 | 10.5 | 10.53 | | S11 tertiary |
| Unifoil Progression 170 | 1097 | 953 | 8.3 | 8.28 | easy lift, stable pitch, long glide | S11 tertiary |
| Unifoil Progression 200 | 1290 | 1037 | 8.3 | 8.34 | | S11 tertiary |
| North Sonar HA1050 | 1050 | 1050 | 10.5 | 10.50 | "upper speed spectrum, short chord, thin profile" | S11 tertiary |
| Sabfoil Leviathan Blackbird 1400 | 1549 | 1380 | 12.3 | 12.29 | DW/pump/SUP; root chord 173 mm, max t 18 mm (t/c_root 10.4%), 2190 g | S9 official |
| Sabfoil Leviathan Pro 1060 | 947 | 1060 | 11.53 | 11.87 | fast DW, pump race; max t 14 mm, volume 757 cm³ | S10 tertiary |
| Sabfoil Leviathan Pro 1160 | 1083 | 1160 | 12.42 | 12.42 | | S10 tertiary |
| Mikes Lab (wing/DW) | 950 | 1010 | — | 10.74 | split fuselage, DW/parawing/light-wind wing | S33 tertiary |
| Starboard iQFOiL 900 | 900 | — (not published) | — | — | one-design race; Speer profile | S13 official |
| Starboard iQFOiL 800 (junior) | 800 | — | — | — | | S13 official |

Geometry bands not on spec pages — taper, sweep, anhedral, twist, section family, tip shape — were not published by any brand opened this session. What can be stated: Axis lists a root chord (105 mm for ART Pro 1001) against a mean chord of 84 mm, implying taper (mean/root ≈ 0.8) consistent with a ≈0.4–0.5 tip/root ratio on a rounded-tip planform (Inferred); Axis ART v2 cites a "tapered outline and reduced second moment of area for roll" [S2] (Verified wording); Armstrong MA cites "rondure" for ventilation recovery on breach [S3] (Verified wording). Anhedral of 5–12°, washout 0–3° and downturned tips are practitioner claims (Flagged). The existing catalog note that hydrofoil sections average ≈9.3% t/c (E817–E908) matches Sabfoil's 10.4% root t/c on a "race-derived thin profile" [S9]; the "ultra-thin" marketing of 2024–2026 therefore means ≈9–11% at the root, not <8% (Inferred).

**Rear stabilizer.** Rule floor 150 cm² (GWA) [S12]; iQFOiL 255 cm² with −2° default and a −2° to +1° shim range [S13]. Typical wing/surf stabilizers are 130–300 cm², span 350–500 mm, AR 4–7, with anhedral and 0–2° shims (Flagged; no spec page opened gave span/AR). Decalage (front-wing incidence minus stabilizer incidence) is the pitch-stability and trim driver; the iQFOiL shim range is the only rule-published decalage adjustment found.

**Mast.** GWA cap 115 cm [S12]; iQFOiL 95 cm carbon UHM, Deep Tuttle [S13]; consumer masts 60–100 cm (Flagged practitioner; surf-magazine advice 85–95 cm for sporty riding/racing [S34, tertiary]). Mast chord ≈110–140 mm, t/c ≈11–15% with symmetric sections (E836–E838, NACA 00xx class in the existing catalog) (Flagged). Stiffness is marketed (UHM/HM carbon) but no brand publishes EI or tip deflection under load (gap).

**Fuselage.** GWA cap 100 cm [S12]; iQFOiL "115 Plus" (length not published on the page opened) [S13]; consumer 55–85 cm (Flagged). A longer fuselage increases stabilizer moment arm (pitch damping, lower shim), raises yaw/roll inertia, and moves the mast–wing junction relative to the pressure centre; no primary measurement of the effect on a consumer foil was found.

### Racing class rules

| Class | Governing text | Geometry constraints | Openness | Label |
|---|---|---|---|---|
| GWA Wingfoil World Tour, Race | Rulebook via equipment page [S12] | production ≥50 pcs; front wing ≥700 cm²; rear ≥150 cm²; mast ≤115 cm (≤3); fuselage ≤100 cm; foil ≥3 kg; board ≥3 kg, no extra strap plugs; wings symmetric, inflatable LE, any size, ≤5/season; ≤3 foil systems, ≤2 boards/season; box rule since 1 Feb 2022 | designer free above the minima; size and count caps push toward one "all-wind" foil of ≈700–900 cm² | Verified |
| iQFOiL (Olympic windfoil) | Starboard iQFOiL pages [S13]; class rules 2025 not opened | one-design: 900 front (800 junior), 255 tail −2°…+1°, 115 Plus fuselage, 95 cm mast, Deep Tuttle | nothing to design; useful as a fixed benchmark | Verified equipment |
| Formula Kite (Olympic) | IKA builder info [S14]; news [S15] | registered production series (≥100 pcs/model/size per news item); tolerances weight ±4%, general ±0.4 mm, height/length ±2 mm, chord −1 mm, span ±2 mm; 10 pieces 3D-scanned; 500 EUR/item + 5000 EUR/manufacturer; kites ≤21 m² M / ≤19 m² W | maxima (span, length, mast) are in the class rules PDF — not opened | Verified tolerances; Flagged maxima |
| International Moth | Class rules 2017 [S35] (Dec 2024 edition exists, not opened) | 6.2 overall beam ≤2250 mm; 6.3.3 any foil except rudder foils must protrude from the hull below the static waterplane; 8.1 mast spars ≤6250 mm; 11.2 no multihull; 12.1 sail pumping limited | foils otherwise unrestricted (development class); whether foil span counts in "overall beam" is not stated in the extracted text | Verified quotes; Flagged span interpretation |
| PWA windfoil slalom 2026 | forum report [S24] | 1 mast, 2 front, 2 back wings, 2 fuselages; 3 sails ≤8.0 m² | count limits only | Flagged (tertiary) |
| Parawing downwind | SUPboarder report [S23] | World Cup status 2026 (Leucate); no box rule found | open | Flagged |
| IWSA Formula Wing / Open class | article [S30] returned 404 | — | — | Flagged (not opened) |

"Open-class" (Moth, downwind, parawing, freeride) designers are free in area, span, AR, section and configuration; box-rule classes (GWA, Formula Kite) fix minima/maxima and force production runs, which is why race foils cluster at the rule floor (≈700 cm² wing race) and why a design tool needs rule presets that validate a recipe against these numbers.

### Operating physics that shape design

**Free surface.** The CEHINAV tank campaign [S16] (Martínez-Barberá, Calderon-Sanchez, Moulian, Souto-Iglesias, J. Marine Sci. Appl. 25(1), 2026) towed a 0.059 m² kitefoil/windfoil wing (span 0.634 m, mean chord 0.0735 m; AR 6.8 by the b²/S convention used throughout this base — the paper itself defines AR := b_eff/c with b_eff = S/c, i.e. S/c² = 10.9, which is why area file 07 quotes 10.9; both numbers describe the same wing and the convention must travel with any imported reference figure) at Re 7.3×10⁴–2.9×10⁵ and Fr_h = V/√(g·h) 0.4–6.7, h/c 0.5–9.5, α −5° to +10°. Findings (Verified): coefficients asymptote to deep-water values only for h/c > 5; effects concentrate at h/c < 4; at h/c = 4, raising Fr_h from 2 to 5 removes ≈17% of the lift coefficient; the authors state Fr_h ≈ 5 is the competition regime. Two-dimensional RANS-VOF work by Pernod et al. (J. Sailing Technology 2023) [S36] reports free-surface deformation for h/c < 2 with lift rising slightly to h/c ≈ 1 then falling sharply, and drag up to ≈3× the deep value near h/c ≈ 0.5 (Flagged: taken from search abstracts; article returned 403). The classical treatments — Wadlin & Christopher (1958) finite-depth lift for rectangular surfaces including dihedral to 30°, and Hough & Moran (1969) Froude-number effects — are cited via secondary summaries only (Flagged). Consequence: the high-Froude limit behaves like an image vortex of opposite sign (lift-slope reduction), the low-Froude limit like a rigid wall (lift-slope increase), so *depth and speed must both be operating-point inputs* and "deep water" must be an explicit assumption with h/c ≥ 5 as its validity floor.

**Ventilation.** Augier et al. (MARINE 2025) [S17] tested Olympic-series kitefoil masts on a boat-mounted beam at several speeds and surface treatments; ventilation inception correlates with the *dynamics* of angle of attack and vertical acceleration rather than their static values, surface preparation is secondary, and a hysteresis model represents the behaviour (Verified abstract). Aguiar Ferreira et al. (J. Fluid Mech. 1028, A25, 2026) [S18] towed surface-piercing semi-ogive and modified NACA 0010-34 models (AR 1.0, 1.5) at Fr 0.5–2.5: nose ventilation dominates at Fr < 1.0–1.25 (inception α rising with Fr), tail ventilation at higher Fr (inception α falling), base ventilation only on the blunt profile; the bistable/stable boundary extends to significantly higher α than previous maps (Verified abstract). Bartesaghi & Provinciali (2022) "Kite foil mast ventilation study" exists but was not opened (Flagged). Design reading (Inferred): a steady estimator cannot predict ventilation onset; it can only show the σ- and Fr-based envelope and flag surface-piercing/near-surface states, and the tool must never label a result "ventilation-safe".

**Cavitation.** σ = (p_atm + ρ g h − p_v)/(½ ρ V²); inception when −Cp_min ≥ σ (Verified definition; standard). With p_atm = 101.325 kPa, h = 0.5 m, seawater 15 °C (ρ = 1026.02 kg/m³, p_v = 1.671 kPa [S1]): p_∞ − p_v = 104.69 kPa. See the σ table below. Tom Speer's H105 page (tspeer.com) could not be opened (TLS failure) — the claims that H105 has a flat rooftop, higher incipient-cavitation speed than E817 at 10–20 kN/m² loading and lower take-off speed come from a forum thread quoting it (Flagged, tertiary [S37]). The existing repo note "E818 42.1 kn vs NACA 4412 31.4 kn" (IHS source) is consistent with −Cp_min ≈ 0.42 vs ≈0.76 at 0.5 m depth in this arithmetic (Inferred).

**Pumping.** The flapping-foil literature places peak propulsive efficiency at St = f·A/U ≈ 0.25–0.40 with large heave/chord and 15–25° maximum effective α at ≈90° pitch–heave phase [S21, secondary]; quasi-steady analysis loses accuracy above ≈2–3 Hz [S38, secondary]. Human pump cadence (≈1–2 Hz, 60–120 strokes/min) and heave amplitude (≈0.2–0.4 m) are practitioner numbers only [S27] (Flagged); no field IMU dataset was found. At f = 1.2 Hz, A = 0.3 m, U = 5 m/s, St = 0.072 (Inferred) — a low-St, glide-dominated regime in which lift-to-drag ratio at CL ≈ 0.4–0.7 and Re 4–7×10⁵ governs, consistent with the market's high-AR pump foils [S3].

**Stall and take-off.** Take-off occurs at the highest CL the section family sustains without laminar separation at Re 3–6×10⁵ — ≈0.6–0.8 from the Sabfoil envelopes above (Inferred) and up to ≈0.78 for surf presets per the existing repo note. Thin race sections (≈9–10% t/c, low camber) have lower CL_max and need either a larger wing, pumping, or more wind to take off; this is the light-wind/top-speed conflict every racer manages by carrying 2–3 registered front wings [S12].

**Roll/yaw stability, anhedral, junction.** Anhedral adds dihedral-effect of negative sign and lowers the roll second moment, giving the "loose" feel brands advertise [S2]; no primary quantification for consumer foils was found (Flagged). The mast–wing junction sits at the root where loading is highest; the existing gap register lists junction drag as unquantified and this session found no consumer-foil junction measurement (gap stands).

**Water properties.** See the ITTC table and Re table below (Verified [S1]).

### Real-world validation and measurement

| Dataset / study | What it measured | Usable for | Label · source |
|---|---|---|---|
| CEHINAV kitefoil/windfoil wing tank test (2026) | CL, CD vs α, h/c, Fr_h; Re 0.7–2.9×10⁵ | free-surface correction validation; low-Re polars of a real race-type planform | Verified [S16] |
| Delft surface-piercing foils (JFM 2026) | ventilation inception maps vs Fr, α, AR | envelope flags for mast/near-surface states | Verified [S18] |
| Augier et al. Olympic kitefoil mast (MARINE 2025) | ventilation inception vs input dynamics | justification for "steady result ≠ ventilation-safe" | Verified [S17] |
| Pernod et al. 2D RANS-VOF (JST 2023) | 2D free-surface lift/drag vs h/c | 2D correction sanity check | Flagged [S36] |
| ResearchGate "Tank testing of a windfoil hydrofoil with free surface effects" (2025) | tank data on a windfoil | potential second anchor | Flagged (403) [S39] |
| Manufacturer take-off/top-speed statements | operating envelope per wing size and rider mass | bounding CL bands | Verified [S9], tertiary [S10] |
| GPS speed records / GPS Formula racing metrics | best 200/400/1000 m speeds, tack/jibe VMG | upper speed bound | Tertiary [S20][S40] |
| Vaaka cadence sensor | paddle stroke cadence (paddling, not foiling) | none found for pumping | Flagged [S41] |
| Foilmount | mounting adapters, not sensors | none | Flagged [S22] |

No brand publishes force/moment data, and no open GPS/IMU dataset for a consumer race foil was found; the estimator's sanity bounds must therefore come from (a) the product table (area/span/AR envelope), (b) manufacturer envelopes (CL band), (c) the σ and Fr_h tables, and (d) the CEHINAV low-Re polars once obtained.

### Goal-state derivation: 90 kg rider, race wingfoil, salt water, 10–20 kn wind

Assumptions, each labelled:

| Input | Value | Label · basis |
|---|---|---|
| Rider | 90 kg | given |
| Race board | 5.5 kg | Flagged (GWA minimum 3 kg [S12]; typical carbon race boards 4–7 kg, practitioner) |
| Foil complete | 4.5 kg | Inferred (front wing alone 2.19 kg [S9]; GWA minimum 3 kg [S12]) |
| Hand wing | 2.4 kg | Inferred (Duotone Unit 5.0 m² 2.18–2.34 kg [S25, retailer-quoted spec]) |
| Wetsuit, harness, helmet, leashes | 3 kg | Flagged |
| System mass | ≈105 kg → W = 1030 N | Inferred |
| Front-wing lift | 1.00–1.10 W (stabilizer down-force minus wing aero vertical lift) → use 1050 N | Flagged (no measured trim data) |
| Water | seawater 35 g/kg, 15 °C: ρ 1026.02 kg/m³, ν 1.1892×10⁻⁶ m²/s, p_v 1.671 kPa | Verified [S1] |
| Foil depth | 0.3–0.5 m (mast 85–95 cm, board clearance) | Flagged |
| Board speeds, 10 kn wind | take-off 9–11; upwind 11–14; downwind 14–18 kn | Flagged (no race dataset opened; consistent with iQFOiL 5–35 kn design range [S26] and record 37.9 kn [S20]) |
| Board speeds, 20 kn wind | upwind 18–22; downwind 24–30 kn | Flagged |
| Foil choice by wind | 10 kn: ≈1000 cm², AR ≈12, span ≈1.10 m; 20 kn: ≈750 cm², AR ≈12, span ≈0.95 m | Inferred from the product ladder [S2][S6][S31] and the GWA 700 cm² floor [S12] |

Operating points computed with q = ½ρV², CL = L/(q S), Re = V c̄/ν, c̄ = S/b:

| Wind | Wing | V kn (m/s) | q Pa | CL | Re | σ at 0.5 m | Note |
|---|---|---|---|---|---|---|---|
| 10 kn | 1000 cm², b 1.10 m, c̄ 0.091 m | 10 (5.14) | 13,577 | 0.773 | 3.9×10⁵ | 7.7 | take-off; at/above thin-section CL_max → pump or bigger wing |
| 10 kn | same | 14 (7.20) | 26,611 | 0.395 | 5.5×10⁵ | 3.9 | upwind cruise |
| 10 kn | same | 18 (9.26) | 43,989 | 0.239 | 7.1×10⁵ | 2.4 | downwind |
| 20 kn | 750 cm², b 0.95 m, c̄ 0.079 m | 15 (7.72) | 30,548 | 0.458 | 5.1×10⁵ | 3.4 | post-take-off |
| 20 kn | same | 22 (11.32) | 65,712 | 0.213 | 7.5×10⁵ | 1.6 | upwind |
| 20 kn | same | 28 (14.40) | 106,443 | 0.132 | 9.6×10⁵ | 0.98 | downwind |
| 20 kn | same | 32 (16.46) | 139,028 | 0.101 | 1.1×10⁶ | 0.75 | top end; needs −Cp_min < 0.75 |

*Correction 2026-09-20 (spec v1 gate, re-executed with 1 kn = 0.514444 m/s, ρ 1026.02, W = 1050 N, h_ref 0.5 m): q recomputed for every row; the 10 kn CL is 0.773, not 0.774 (the earlier row rounded q to 13,570). σ at 0.3 m reads 7.56 / 3.86 / 2.33 / 3.36 / 1.56 / 0.96 / 0.74.*

Wing loading 10.5 kPa (1000 cm²) and 14.0 kPa (750 cm²) — inside the 10–20 kN/m² range attributed to Speer's kite/wind foil design discussion [S37, tertiary]. Cavitation ceiling: at 0.5 m depth a section holding −Cp_min ≤ 0.63 at CL ≈ 0.1 reaches 35 kn; ≤ 0.48 reaches 40 kn (Inferred from [S1] arithmetic). Plausible race geometry for this brief: 700–1050 cm², AR 11–13, span 0.93–1.15 m, root t/c 9–10.5%, mean chord 75–95 mm, Eppler E817/E818-class or a Speer-type flat-rooftop section ranked at Re 5×10⁵–1.1×10⁶ (Inferred). Competing race-oriented production foils with published numbers: Axis ART Pro 751–1051 (518–932 cm², AR 11.1–12.1) [S31]; Lift 110 HA-X (710 cm², AR 13.1) [S6]; Armstrong HA480–HA680 (480–680 cm², AR 9.8–11) [S32]; North Sonar HA1050 (AR 10.5) [S11]; Sabfoil Leviathan Pro 1060/1160 (947/1083 cm², AR 11.5–12.4, downwind-marketed) [S10]; Mikes Lab 950 (AR 10.7) [S33]; the iQFOiL 900 as the fixed windfoil benchmark [S13]. Dedicated wing-race models (F-One, North "R", Sabfoil race line) were not opened and are Flagged.

### Product-line trends 2023–2026

- **Downwind-derived ultra-high-AR foils** (AR 11–13, spans 1.2–1.4 m, areas 950–1550 cm²) crossed from downwind into wing and pump use (Sabfoil Leviathan/Blackbird [S9][S10], Lift HA-X [S6], Axis ART Pro [S31], Naish "Super High Aspect" blog [S42, secondary]). Industry guides call downwind and wing the two sales drivers of 2025 [S43, secondary].
- **Thin "race-derived" sections** are now the default across freeride lines (Sabfoil Blackbird: 10.4% root t/c, Verified arithmetic on [S9]); the trade is higher take-off speed and a narrower CL band, which brands offset with "active-lift" fuselages and bigger areas.
- **Mast stiffness** is marketed via UHM/HM carbon (iQFOiL "stiffer UHM carbon mast" [S13]; F-One SK8 HM [S8]); no brand publishes stiffness numbers (gap).
- **Modular interfaces**: 90 × 165 mm plate de facto; F-One 160 × 90/165 × 90; Deep Tuttle for windfoil [S22, tertiary; S13]; proprietary wing–fuselage joints (Sabfoil T8/MH172 [S9][S10]).
- **Parawing** (2024–2026) reuses downwind foils and adds upwind legs to racing [S23]; **e-foil** systems converge on 4–6 kW motors and 2–2.3 kWh packs [S28, tertiary].
- Implication (Inferred): a design tool's presets must span AR 7–13 and area 480–1900 cm², expose depth and speed as first-class operating inputs, and treat "thin" as a t/c channel value (≈0.09–0.11 root) rather than a marketing tag.

## Comparables

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| Manufacturer spec pages (Axis, Armstrong, Sabfoil) [S2][S3][S9] | area/span/AR/(thickness) per SKU, prose envelope | published planform numbers, sometimes take-off/top speed per rider mass | authoritative dimensions; consistent ladders | no section, taper, twist, anhedral; AR convention unstated | proprietary web pages, cite only | Verified |
| Retailer spec sheets [S6][S7][S8][S10][S11][S31][S32] | copied maker numbers | same | broad coverage | copy errors (Leviathan 1060 AR mismatch) | proprietary | Inferred/tertiary |
| GWA / IKA / iQFOiL / Moth rules [S12][S13][S14][S35] | equipment box or one-design | hard minima/maxima | unambiguous constraints | maxima for Formula Kite not accessible here | rule documents, cite | Verified |
| CEHINAV tank test [S16] | forces vs h/c, Fr_h, α on a race-type wing | towing tank | quantifies free-surface loss at competition Fr | Re one order below full scale | journal article (no data licence stated) | Verified |
| Delft surface-piercing ventilation [S18] | inception maps | towing tank | trigger taxonomy, revised stability map | simple sections, AR ≤1.5 | JFM, arXiv preprint (arXiv non-exclusive licence) | Verified |
| Augier et al. kitefoil mast [S17] | inception vs input dynamics | on-water rig | real Olympic hardware | abstract only, no numbers | conference abstract | Verified abstract |
| Flapping-foil St literature [S21][S38] | propulsive efficiency vs St | CFD/experiment | optimum St band | not human pumping | journals / arXiv | Inferred |
| Pump-foil simulator (foilphysics) [S27] | angle telemetry toy model | web app | cadence vocabulary | unvalidated | unknown | Flagged |
| Speer H105 write-up [S37] | low-Re, flat-rooftop hydrofoil section | analysis + coordinates | design rationale for kite/wing foils | site unreachable; rights on coordinates unknown | unknown (rights gate stands in spec) | Flagged |

## Reference information

- **ITTC 7.5-02-01-03 Rev03 (2024)** — fresh and standard-seawater density, viscosity, kinematic viscosity and vapour pressure with sensitivity coefficients; requires every operating point to pin temperature, salinity (35 g/kg standard) and pressure basis. Values used here are Verified [S1].
- **GWA Wingfoil World Tour race equipment rules** — minima/maxima above; require a "race preset" validator (area ≥700, rear ≥150, mast ≤115, fuselage ≤100 cm) [S12].
- **iQFOiL equipment definition** — fixed 900/255/115/95 configuration; requires a benchmark preset whose geometry is Flagged until span/section are obtained [S13].
- **IKA Formula Kite builder information** — tolerances and registration; requires a tolerance display (±2 mm span, −1 mm chord) when a design targets registration [S14].
- **International Moth class rules 2017 (World Sailing)** — 6.2 beam ≤2250 mm, 6.3.3 foil protrusion; requires only a note that Moth is a development class [S35].
- **Martínez-Barberá et al. 2026, J. Marine Sci. Appl. 25(1)** — free-surface test data; requires depth as an operating-point input and h/c ≥ 5 as the "deep" validity floor [S16].
- **Aguiar Ferreira et al. 2026, J. Fluid Mech. 1028 A25** and **Augier et al. MARINE 2025** — ventilation; require the estimator to never claim ventilation safety [S17][S18].
- **Hough & Moran 1969; Wadlin & Christopher 1958** — seminal free-surface lift corrections; not opened (Flagged) — the cheapest next probe is to fetch the NACA/NASA TR from NTRS.

## Data, constants, formulae and invariants

**Water properties (ITTC 7.5-02-01-03 Rev03, Verified [S1]).** Fresh water at standard atmospheric pressure; seawater at absolute salinity 35 g/kg.

| T °C | ρ fresh kg/m³ | ν fresh m²/s | ρ sea kg/m³ | ν sea m²/s | p_v fresh kPa | p_v sea kPa |
|---|---|---|---|---|---|---|
| 5 | 999.97 | 1.5182×10⁻⁶ | 1027.72 | 1.5762×10⁻⁶ | 0.8726 | 0.8547 |
| 10 | 999.70 | 1.3063×10⁻⁶ | 1027.00 | 1.3604×10⁻⁶ | 1.2282 | 1.2030 |
| 15 | 999.10 | 1.1386×10⁻⁶ | 1026.02 | 1.1892×10⁻⁶ | 1.7058 | 1.6709 |
| 20 | 998.21 | 1.0034×10⁻⁶ | 1024.81 | 1.0508×10⁻⁶ | 2.3393 | 2.2914 |
| 25 | 997.05 | 8.9266×10⁻⁷ | 1023.39 | 9.3713×10⁻⁷ | 3.1699 | 3.1050 |
| 30 | 995.65 | 8.0071×10⁻⁷ | 1021.77 | 8.4253×10⁻⁷ | 4.2470 | 4.1600 |

(The vapour-pressure column was identified by the table's header order — density, dynamic viscosity, kinematic viscosity, vapour pressure — and its 15 °C fresh value 1.7058 kPa matches the standard steam-table value; Verified with that caveat.)

**Reynolds number.** Re = V·c/ν, with V in m/s, c the local (or mean, S/b) chord in m, ν from the table. Worked once: V = 20 kn = 10.289 m/s, c = 0.12 m, seawater 15 °C → Re = 10.289 × 0.12 / 1.1892×10⁻⁶ = 1.038×10⁶. Temperature/salinity sensitivity at fixed V·c = 0.926 m²/s (20 kn, 0.09 m):

| T °C | Re fresh | Re sea |
|---|---|---|
| 5 | 6.10×10⁵ | 5.88×10⁵ |
| 10 | 7.09×10⁵ | 6.81×10⁵ |
| 15 | 8.13×10⁵ | 7.79×10⁵ |
| 20 | 9.23×10⁵ | 8.81×10⁵ |
| 25 | 1.04×10⁶ | 9.88×10⁵ |
| 30 | 1.16×10⁶ | 1.10×10⁶ |

Invariant: at equal V and c, Re_sea < Re_fresh at every temperature in the table (ν_sea/ν_fresh = 1.038–1.052). Invariant: force at fixed CL scales by ρ_sea/ρ_fresh = 1.0269 (15 °C), 1.0266 (20 °C) — but CL itself is Re-dependent, so the two effects must be applied through the polar, not as a multiplier (this confirms the reconciliation register's treatment).

**Discipline Re bands (seawater 15 °C; chord and speed inputs stated).**

| Discipline | c̄ m (from table) | V kn | Re | Label |
|---|---|---|---|---|
| Surf | 0.17–0.20 (1500–2000 cm², AR 5–7) | 7–15 | 5.2×10⁵–1.3×10⁶ | Inferred; chords from AR/area bands (spec [S3] MA1750: c̄ 0.152) |
| Pump | 0.12–0.15 (APF, Leviathan) | 8–12 | 4.2×10⁵–8.0×10⁵ | Inferred [S3][S9] |
| Downwind | 0.085–0.115 (Leviathan Pro/Blackbird) | 10–20 | 3.7×10⁵–1.0×10⁶ | Inferred [S9][S10] |
| Wingfoil freeride | 0.10–0.14 (MA, SK8, Progression) | 10–22 | 4.3×10⁵–1.3×10⁶ | Inferred [S3][S8][S11] |
| Wingfoil race | 0.075–0.095 (ART Pro, HA-X) | 12–32 | 3.9×10⁵–1.3×10⁶ | Inferred [S6][S31]; speeds Flagged |
| Windfoil iQFOiL/slalom | 0.085–0.10 (900 cm² one-design; span Flagged) | 15–35 | 5.5×10⁵–1.5×10⁶ | Inferred/Flagged [S13] |
| Kitefoil race | 0.07–0.095 | 18–38 | 5.5×10⁵–1.6×10⁶ | Inferred [S33]; speeds [S19] |
| E-foil | 0.15–0.25 | 10–24 | 6.5×10⁵–2.6×10⁶ | Flagged (no e-foil wing spec opened) |

**Lift and lift coefficient.** L = ½ ρ V² S CL; CL = L/(q S). Validity: steady, attached flow, S the projected planform area of the wing whose lift is meant, deep water (h/c ≥ 5) unless a free-surface correction is applied. Wing loading W/S in kPa is the cavitation-relevant quantity: for a given depth, the cavitation-limited top speed scales as V_cav = √(2 (p_∞ − p_v) / (ρ · (−Cp_min))) and −Cp_min rises with CL, so higher loading pushes the ceiling down.

**Cavitation number, seawater 15 °C, h = 0.5 m, p_∞ − p_v = 104.69 kPa (Verified inputs, Inferred table).**

| V kn | V m/s | q Pa | σ |
|---|---|---|---|
| 20 | 10.29 | 54,320 | 1.93 |
| 25 | 12.86 | 84,850 | 1.23 |
| 30 | 15.43 | 122,200 | 0.86 |
| 35 | 18.01 | 166,400 | 0.63 |
| 40 | 20.58 | 217,200 | 0.48 |
| 45 | 23.15 | 274,900 | 0.38 |
| 50 | 25.72 | 339,400 | 0.31 |

Invariant: σ falls as V⁻²; each extra metre of depth adds ρ g ≈ 10.07 kPa to p_∞ (sea) — ≈+9.6% σ at 0.5 → 1.5 m, which is why depth belongs in the operating point. Inception speed for −Cp_min = 0.35 / 0.5 / 0.7: 46.9 / 39.3 / 33.2 kn (Inferred).

**Free-surface Froude number.** Fr_h = V/√(g h). At 20 kn: h = 0.5 m → 4.65; h = 0.3 m → 6.0. Validity floor for "deep": h/c ≥ 5 and use the CEHINAV correction trend below that [S16].

**Strouhal number for pumping.** St = f·A/U with A the peak-to-peak heave (m), f cadence (Hz), U forward speed (m/s); propulsive optimum ≈0.25–0.40 in the flapping-foil literature [S21]; human pumping ≈0.05–0.15 (Inferred, inputs Flagged). Quasi-steady strip theory is acceptable below ≈2–3 Hz at these amplitudes [S38, secondary].

**Aspect ratio.** AR = b²/S with b the full projected span and S the projected planform area — the repo's stated convention. The product table shows makers deviating by −3% to +2%; the tool must display "AR (projected, b²/S)" and may show a maker's stated value only as an annotation.

**Invariants and edge cases.** V ≤ 0 → no operating point. h ≤ 0 → surface-piercing state: estimator unavailable, ventilation flag only. σ ≤ −Cp_min → cavitation flag, result qualified. CL_required > CL_max(Re) → "no supported take-off at this speed". Re < 2×10⁵ → outside every catalog polar cited here; Flagged output. Any preset value in this file carrying Flagged is displayed as a source-based assumption, never as a constant.

## Design implications for CFD-Workbench

1. **Discipline presets (CAT-03, A4 recipe defaults) must expand from four to at least eight** — surf, pump/dock-start, downwind/SUP, parawing, wingfoil freeride, wingfoil race (GWA box), windfoil (iQFOiL benchmark + slalom), kitefoil race, e-foil — each carrying a recipe (area, AR, span, taper, anhedral, washout, root t/c), a context (rider mass, system mass model, water, depth, speed band) and a label per field (Verified/Inferred/Flagged) as recorded here. The existing four-preset bands (surf 1500–2000 cm² AR 4–6; freeride 1200–1500 AR 6–8; SUP/DW 900–1200 AR 9–12; windsurf/race 700–900 AR 10–14) are broadly consistent with the product table but the market's surf/freeride AR floor is now ≈7 (Armstrong MA) and downwind spans reach 1.38 m at 1549 cm² (Sabfoil), so the bands should be re-centred on the table in this file.
2. **Operating point value object (A3) must add depth h and a system-load model**: speed, water conditions, incidence, depth, and explicit load (N) are already listed; add derived read-outs Fr_h, h/c, σ (with p_v from the pinned water record), and label "deep-water assumption (h/c ≥ 5)" when depth is unspecified.
3. **Goal-state inputs for the optimizer** (future): rider mass, board/foil/wing masses (each defaulted from this file with labels), water, wind band → target speed band (Flagged until a race dataset exists), depth band, class rule preset. Objective candidates per discipline: min drag at cruise CL with take-off CL_max constraint (race), max L/D at low speed (pump/downwind), min roll inertia at fixed area (surf).
4. **Validity envelopes the analysis tier must enforce**: Re 2×10⁵–2×10⁶ for the catalog polars; h/c ≥ 5 for uncorrected results; σ > −Cp_min for cavitation-free claims; never a ventilation claim (ANA-04 already forbids 3D stall/ventilation claims — keep it).
5. **Sanity-bound checks to show** (ANA tier, GEO-01 style): area, span and AR against the product envelope (480–1900 cm², 690–1380 mm, AR 7–13.1), wing loading against 8–20 kPa, take-off CL against 0.6–0.8, cruise CL against 0.1–0.45, and root t/c against 0.09–0.12 — with the message "outside every catalogued product" rather than "wrong".
6. **Class-rule presets (new GAP-13 closure)**: GWA race validator (front ≥700 cm², rear ≥150 cm², mast ≤115 cm, fuselage ≤100 cm, foil ≥3 kg), iQFOiL fixed benchmark, Formula Kite tolerance display; Moth and parawing as "open". Store rule version and date with each preset.
7. **Assembly grammar (future surfaces)**: stabilizer as a second lifting surface with incidence/shim and decalage read-out; mast as a `z`-axis strut with length 60–115 cm and symmetric section; fuselage as a length parameter driving stabilizer arm. The iQFOiL shim range (−2° to +1°) is the first concrete decalage requirement.
8. **AR display**: derive b²/S from projected geometry, never store; show the maker-convention gap when a product is imported as a comparison (the ART Pro +2% and Leviathan 1060 −3% cases).
9. **Water handling (ANA-15)**: keep temperature-dependent ν and p_v in the operating point; the Re table above shows a 10 → 30 °C swing moves Re by ×1.6 in seawater — enough to change Eppler ranking.
10. **Borrow / avoid**: borrow the manufacturer practice of stating take-off/top speed per rider mass as an *envelope annotation* on a design; avoid copying any maker's AR or thickness number without its convention; avoid the "ventilation-resistant" and "cavitation-free" marketing vocabulary in tool output.
11. **Product benchmarks (GAP-12 partial closure)**: this file's table is the first real-product envelope; it should become a machine-readable fixture with per-row source URL and access date so the sanity checks in (5) are data-driven and re-verifiable.

## Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Race wingfoil board speeds at 10 and 20 kn wind (upwind/downwind VMG) — no dataset opened | fetch IWSA/GWA event GPS analyses or a GPS Formula export; or one instrumented session (GPS + wind) |
| iQFOiL 900 front-wing span, section t/c, tail span | fetch the 2025 iQFOiL class rules PDF (equipment appendix) |
| Formula Kite dimensional maxima (span, total length, mast) | fetch the current IKA Formula Kite class rules PDF from kiteclasses.org |
| Pump cadence and heave amplitude, measured | one IMU session (phone accelerometer at 100 Hz) on a dock start; compute f and A |
| Speer H105 design Re/CL, cavitation comparison, coordinate rights | retry tspeer.com with certificate override or the IHS mirror; record rights before vendoring |
| Hough & Moran / Wadlin & Christopher formulae | fetch the NASA/NACA TRs from NTRS and transcribe the lift-slope correction |
| Whether Moth "overall beam" includes foils | read rule 6.2 definitions in the Dec 2024 edition |
| E-foil wing geometry and motor thrust | fetch Fliteboard/Waydoo wing spec pages (areas/spans) |
| Stabilizer spans/AR/anhedral per brand | fetch three stabilizer spec pages (Axis, Armstrong, Sabfoil) |
| Mast EI / stiffness numbers | none published; would need a bench deflection test |

Failure modes in this domain: **silent** — using deep-water polars at h/c < 4 (up to ≈17% lift error at competition Fr [S16]); applying a density multiplier for salt water without re-evaluating Re; treating a maker's AR as b²/S. **Expensive** — sizing a race wing at CL_cruise without checking take-off CL_max at Re 4×10⁵, producing a foil that never lifts in 10 kn. **Irreversible** — declaring a design "cavitation-free" or "ventilation-safe" from a steady estimator; a foil failing or ventilating at 30+ kn injures the rider (GAP-14 safety framing stands).

## Disconfirming views sought

- **"AR conventions are consistent; the differences are rounding."** Fared badly: Armstrong MA rows match b²/S to two decimals across six sizes while Axis ART Pro rows deviate by +1.6% to +2.0% in the same direction across nine sizes — systematic, not rounding. The Leviathan 1060 −3% is a single retailer row and may be a copy error (kept Flagged).
- **"The 35–45 kn ceiling is folklore; modern thin sections go faster."** Partly holds: the arithmetic shows the ceiling is set by −Cp_min at the operating CL and depth, not by a fixed speed; a −Cp_min ≈ 0.3 section at 1 m depth reaches ≈50 kn. But the observed records (37.9 kn wing, ≈38 kn Formula Kite, 39.1 kn kite 1 NM) sit inside the band, so the band stands as the *practical* limit at consumer depths and loadings.
- **"Free-surface effects are negligible for foils 0.8 m deep."** Holds only for h/c ≥ 5–10; at c̄ = 0.09 m and h = 0.5 m, h/c = 5.6 — marginal; the CEHINAV data [S16] put the affected region at h/c < 4–5 with Fr_h ≈ 5, which is exactly the racing regime, so depth must remain an input.
- **"Pumping is a propulsive flapping-foil problem; use St ≈ 0.3 design rules."** Fared badly on the Inferred St ≈ 0.05–0.15 of human cadence — pump-foil design is glide-limited, not thrust-limited; the flapping literature informs the unsteady tier, not the preset.
- **"The GWA box rule makes race-foil design moot."** Fared badly: the rule fixes minima only; area, AR, span, section and configuration remain free above 700 cm², and the production-run requirement (≥50) favours tools that produce manufacturable, documented geometry.
- **"A race board in 10 kn of wind does not reach 10 kn boat speed; take-off is pump-assisted and the whole speed axis of the goal state is wrong."** Sought because every board speed in the goal-state table is Flagged (no GPS dataset opened). Fared: partly holds — the iQFOiL package is designed for 5–35 kn wind [S26] and light-wind wingfoil racing is pump-assisted (practitioner reports, tertiary), so the 10 kn-wind take-off row is the least trustworthy row and pumping is not modelled by any steady tier in this base; the 20 kn-wind rows are bounded above by the 37.9 kn record [S20] and below by the manufacturer envelopes [S9][S10], which is the only corroboration available. Disposition: the speed axis stays **Flagged** and every bound derived from it (take-off CL 0.6–0.8, cruise CL 0.1–0.45, wing loading 8–20 kPa) is Inferred-on-Flagged until an instrumented session or a race GPS export replaces it; the index tags them so.
- **"The 17 % free-surface loss is a laminar-bubble / Reynolds artefact of a 10⁵-Re model with rear wing and mast present, not Froude physics."** Sought because the test Re (0.7–2.9×10⁵) is an order below full scale and the model carried a stabilizer and strut [S16]. Fared: not refutable from this base — the paper reports the loss as a Froude effect at fixed h/c and the deep-water asymptote at h/c > 5 is Re-independent in its own data, but the Re confound is real for a 63-210 section near its laminar-bubble regime. Disposition: the 17 % is carried as measured at model scale with the confound named; a full-scale confirmation is the cheapest next probe.
- **"Seawater just adds 2.7% lift."** Fared badly: Re drops 4.3% at the same speed and chord (15 °C); the effect on CL/CD through laminar-bubble behaviour at Re 4–8×10⁵ can exceed the density gain in either direction — must go through the polar.

**Contradicts existing repo knowledge.** Nothing in the repo was overturned. Two re-centrings are recorded: (1) the proposal's preset bands (`proposal-sequence.md` §3.1a: surf AR 4–6, freeride AR 6–8) sit below the 2025–2026 market, where the lowest production AR opened is 7.1 (Armstrong MA) and freeride lines run 8–9.5 (Code S, Unifoil Progression, F-One SK8) — the bands are not wrong, they are dated, and the presets should be re-centred on the product table above; (2) the Bench claim "seawater yields about 2.7% more force at identical geometry/speed/angle" is confirmed numerically (ρ ratio 1.0269 at 15 °C) *and* the reconciliation register's objection is strengthened: ν_sea/ν_fresh = 1.044 at 15 °C, so Re is 4.3% lower, which at Re 4–8×10⁵ can move CL/CD through the polar by more than the density gain. The Speer H105 rights gate in the spec remains open: tspeer.com was unreachable this session (TLS), so no licence statement was read.

## Glossary terms

- **Aspect ratio (AR)** — b²/S with b the full projected span and S the projected planform area; makers' stated values may use other conventions. *(Verified convention, Inferred deviations, [S2][S3][S31])*
- **Cavitation number (σ)** — (p_∞ − p_v)/(½ρV²) at foil depth; inception when −Cp_min ≥ σ. *(Verified definition; values [S1])*
- **Decalage** — incidence difference between front wing and stabilizer; iQFOiL exposes it as a −2° to +1° shim. *(Verified, [S13])*
- **Froude number based on submergence (Fr_h)** — V/√(g h); governs free-surface lift loss; ≈5 in kite/wind racing. *(Verified, [S16])*
- **Box rule** — class rule fixing equipment minima/maxima and production status rather than a one-design. *(Verified, [S12])*
- **One-design** — every competitor uses identical registered equipment (iQFOiL). *(Verified, [S13])*
- **Ventilation** — air ingress along a low-pressure region connected to the atmosphere (surface-piercing strut or breaching tip), replacing water on part of the foil and collapsing lift; onset is dynamic and hysteretic. *(Verified, [S17][S18])*
- **Strouhal number (St)** — f·A/U for an oscillating foil; propulsive optimum 0.25–0.40; human pumping ≈0.05–0.15. *(Inferred, [S21])*
- **Wing loading** — weight (or lift) per projected area, kPa; race wing foils ≈10–14 kPa in this file's derivation. *(Inferred)*
- **Deep-water assumption** — h/c ≥ 5, where free-surface effects are within the test's asymptote. *(Verified, [S16])*
- **Shim** — a wedge between stabilizer and fuselage changing stabilizer incidence in ≈0.5–1° steps. *(Verified range for iQFOiL, [S13])*
- **Take-off CL** — the lift coefficient required at the speed where the board leaves the water; 0.6–0.8 in this file's derivations. *(Inferred, [S9][S10])*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | ITTC Recommended Procedures 7.5-02-01-03 Rev03 (2024), Fresh Water and Seawater Properties (PDF opened, text extracted) | standard | https://www.ittc.info/media/11764/75-02-01-03.pdf | 2026-09-20 | ρ, μ, ν, p_v tables; Re and σ arithmetic |
| S2 | AXIS Foils — ART v2 999 product page (range table) | primary (official) | https://axisfoils.com/products/artv2-999 | 2026-09-20 | ART v2 area/span/AR; outline and roll-inertia wording |
| S3 | Armstrong Foils — MA Mid Aspect Front Foil product page | primary (official) | https://armstrongfoils.com/products/ma-front-foil | 2026-09-20 | MA sizes; AR rationale; ventilation-recovery wording |
| S4 | AXIS Foils — ART v2 collection and front-wings collection (search result only) | primary (official, not opened) | https://axisfoils.com/collections/artv2 | 2026-09-20 | range existence |
| S5 | REAL Watersports — Armstrong HA front wing page (search snippet) | tertiary | https://www.realwatersports.com/products/armstrong-high-aspect-v2-front-wing | 2026-09-20 | HA range existence |
| S6 | Lift Foils HA/HA-X specs via retailers (foil-tec, amosshapes, kiteboardingcloseouts) | tertiary | https://foil-tec.com/en-us/products/copy-of-lift-high-aspect-wing-90-front ; https://amosshapes.com/en-us/products/lift-foils-ha110-x-high-aspect-front-wing | 2026-09-20 | Lift 90 HA, 110 HA-X, 150 HA-X, 170 HA numbers |
| S7 | Code Foils 720S / 850S specs (Kite Barn) | tertiary | https://www.kitebarn.co.uk/product/code-foils-850s-front-wing/ | 2026-09-20 | Code S numbers |
| S8 | F-One Seven Seas / SK8 specs (MACkite, REAL Watersports) | tertiary | https://www.mackiteboarding.com/f-one-seven-seas-carbon-front-wing/ ; https://www.realwatersports.com/products/f-one-sk8-hm-carbon-front-wing | 2026-09-20 | Seven Seas 1200, SK8 AR/size range, HM mast marketing |
| S9 | Sabfoil — Leviathan Blackbird 1400 (WL1400-BB) product page | primary (official) | https://sabfoil.com/en/products/WL1400-BB | 2026-09-20 | span, area, AR, root chord, max thickness, weight, take-off/top speed |
| S10 | Sabfoil Leviathan Pro 1060/1160 specs (houstonkiteboarding, pacificnorthsports) | tertiary | https://houstonkiteboarding.com/products/sabfoil-leviathan-pro-front-wing-only | 2026-09-20 | Leviathan Pro numbers, thickness, envelope |
| S11 | Unifoil Progression specs (foilshop, uni-foil, Foiling Magazine tests); North Sonar HA1050 (northactionsports/retail) | tertiary | https://foilshop.com/products/unifoil-progression-front-wing ; https://northactionsports.com/products/sonar-ha1150-front-wing | 2026-09-20 | Progression 100/170/200; Sonar HA1050 |
| S12 | GWA Wingfoil World Tour — Wingfoil Race Class Equipment | primary (rules) | https://www.wingfoilworldtour.com/wingfoil-race-class/equipment/ | 2026-09-20 | box-rule minima/maxima, counts, dates |
| S13 | Starboard Foils — 2025 iQFOiL introduction (equipment definition) | primary (official) | https://starboardfoils.com/pages/2025-iqfoil-intro | 2026-09-20 | 900/800 wings, 255 tail and shim range, 115 Plus fuselage, 95 cm mast, Deep Tuttle, class usage |
| S14 | International Kiteboarding Association — Information for Builders: Kites and Foils | primary (rules) | https://www.kiteclasses.org/equipment/information-for-builders/kites-and-foils | 2026-09-20 | Formula Kite tolerances, scanning, fees |
| S15 | IKA news — Formula Kite class rules update / registered equipment LA 2028 (search snippets) | secondary | https://kiteclasses.org/component/content/article/452-formula-kite-class-rules-update-and-end-of-new-equipment-registration-process?catid=60&Itemid=322 | 2026-09-20 | ≥100-piece production series; kite size caps |
| S16 | Martínez-Barberá, Calderon-Sanchez, Moulian, Souto-Iglesias — Fluid Forces on a Hydrofoil for Kitefoil and Windfoil Boards: dependence on submergence Froude number, J. Marine Sci. Appl. 25(1), 2026 (HTML mirror; Springer page redirected to login) | primary (peer-reviewed) | https://html.rhhz.net/jmsa/html/20260101.htm ; https://link.springer.com/article/10.1007/s11804-025-00708-2 | 2026-09-20 | tank setup, Re/Fr_h ranges, 17% CL loss, h/c thresholds |
| S17 | Augier, Hochhausen, Iachkine, Fermigier, Clanet — Experimental study of ventilation inception on Olympic series kitefoil, MARINE 2025 abstract | primary (conference abstract) | https://marine2025.cimne.com/event/contribution/1f6f3e16-d263-11ef-94cb-000c29ddfc0c | 2026-09-20 | ventilation inception dynamics, hysteresis |
| S18 | Aguiar Ferreira, Navas Rodríguez, Jacobi, Fiscaletti, Greidanus, Westerweel — On the ventilation of surface-piercing hydrofoils under steady-state conditions, J. Fluid Mech. 1028 A25 (2026); arXiv 2503.18015 | primary (peer-reviewed) | https://arxiv.org/abs/2503.18015 | 2026-09-20 | ventilation triggers, Fr range, stability map |
| S19 | TheKiteMag / thekitespot — Formula Kite at Paris 2024 ("up to 70 km/h") | secondary | https://www.thekitemag.com/feature/olympic-games/ | 2026-09-20 | kitefoil race top speed |
| S20 | SROKA Company blog — wingfoil speed record 37.89 kn | tertiary | https://srokacompany.com/en/blog/how-to-go-faster-in-wingfoil/ | 2026-09-20 | wingfoil top-speed bound |
| S21 | Strouhal number impact on propulsion efficiency in fully-active oscillating foils, Ocean Engineering (2024) (abstract via search) | secondary | https://www.sciencedirect.com/science/article/abs/pii/S0029801824000234 | 2026-09-20 | St optimum 0.25–0.40 |
| S22 | MACkite — Hydrofoil boards, bolt patterns and compatibility; Foilmount adapter page | tertiary | https://www.mackiteboarding.com/news/-hydrofoil-boards-bolt-patterns-and-compatibility/ ; https://foilmount.com/ | 2026-09-20 | 90×165 plate, Tuttle depth, F-One 160/165×90 |
| S23 | SUPboarder — Parawing downwind racing gets World Cup status in 2026 | secondary | https://supboardermag.com/2026/04/02/parawing-downwind-racing-gets-world-cup-status-in-2026/ | 2026-09-20 | parawing racing status, course legs |
| S24 | Seabreeze forum — PWA 2026 (foil slalom equipment limits) | tertiary | https://www.seabreeze.com.au/forums/Windsurfing/General/PWA-2026?page=1 | 2026-09-20 | PWA 2026 equipment counts |
| S25 | Duotone Unit 2025/2026 product pages (weights per size, via search snippets) | secondary (official, not opened) | https://www.duotonesports.com/en/us/products/duotone-wing-unit-2025-42250-3519 | 2026-09-20 | hand-wing mass, wind range for 5.0 m² |
| S26 | Surfertoday — iQFOiL: the new Olympic windsurfing equipment (5–35 kn design range) | secondary | https://www.surfertoday.com/windsurfing/ifoil-the-new-olympic-windsurfing-equipment | 2026-09-20 | racing wind range |
| S27 | foilphysics Pump Foil Simulator; MACkite dock-start guide | tertiary | https://lsegessemann.github.io/foilphysics/ ; https://www.mackiteboarding.com/news/how-to-dockstart-a-foil-a-stepbystep-pump-foiling-guide/ | 2026-09-20 | cadence vocabulary (no measured values) |
| S28 | Waydoo Flyer EVO / Fliteboard specs via retailers (MACkite, electro-riding-shop) | tertiary | https://www.mackiteboarding.com/waydoo-flyer-one-evo-efoil-performance-kit/ ; https://www.electro-riding-shop.com/en/efoils/61-fliteboard-pro.html | 2026-09-20 | e-foil motor kW, battery Wh, masses, top speed |
| S29 | Fliteboard — Product details / features | primary (official) | https://fliteboard.com/product-details | 2026-09-20 | battery ride times only |
| S30 | IWSA WingFoil Racing — gear-direction and class-rules articles (404/403 this session) | not opened | https://wingfoilracing.com/ | 2026-09-20 | Flagged gap |
| S31 | Axis ART Pro specs (The SUP Company UK / houstonkiteboarding) | tertiary | https://www.thesupco.com/foil-c3/axis-art-pro-front-wing-p5997 | 2026-09-20 | ART Pro sizes, chords, stated AR |
| S32 | Armstrong HA / APF specs (Big Winds, REAL Watersports, Wet N Dry) | tertiary | https://bigwinds.com/products/armstrong-ma-front-foil-wings/ ; https://wetndryboardsports.com/armstrong-apf-front-wing-range.html | 2026-09-20 | HA and APF numbers |
| S33 | Mikes Lab wing specs (Bay Area Kiteboarding forum; mikeslab.com) | tertiary | https://www.bayareakiteboarding.com/forum/viewtopic.php?t=15334 ; https://www.mikeslab.com/bullet-3/ | 2026-09-20 | 950 cm², 101 cm span |
| S34 | SURF Magazin — Wingfoiling with speed: technique and tuning | tertiary | https://www.surf-magazin.de/en/wingsurfing/how-to/tips-and-tricks/tips-and-tricks-wingfoiling-with-speed-tips-on-riding-technique-and-tuning/ | 2026-09-20 | mast length practice |
| S35 | International Moth Class Rules 2017 (World Sailing PDF, text extracted) | primary (rules) | https://www.sailing.org/tools/documents/MTHCR010517-%5B22785%5D.pdf | 2026-09-20 | rules 6.2, 6.3.3, 8.1, 11.2, 12.1 |
| S36 | Pernod, Sacher, Wackers, Augier, Bot — free-surface effects on 2D hydrofoils by RANS-VOF / BEM analysis, J. Sailing Technology 8 (2023) (abstract via search; article 403) | secondary | https://doi.org/10.5957/jst/2023.8.10.183 | 2026-09-20 | 2D h/c trends |
| S37 | Boat Design Net threads — Eppler 387 vs H105; foil loading limit to avoid cavitation (Speer quoted) | tertiary | https://www.boatdesign.net/threads/foil-loading-limit-to-avoid-cavitation.67382/page-2 ; https://www.boatdesign.net/threads/eppler-387-vs-h105-for-kite-hydrofoil.59737/ | 2026-09-20 | H105 claims, 10–20 kN/m² loading |
| S38 | Scaling and performance of simultaneously heaving and pitching foils (arXiv 1801.07625) | secondary | https://arxiv.org/pdf/1801.07625 | 2026-09-20 | quasi-steady validity ≈2–3 Hz |
| S39 | Tank Testing of a Windfoil Hydrofoil with Free Surface Effects (ResearchGate, 2025; 403) | not opened | https://www.researchgate.net/publication/389790484_Tank_Testing_of_a_Windfoil_Hydrofoil_with_Free_Surface_Effects | 2026-09-20 | Flagged second anchor |
| S40 | GPS Formula — discipline page | tertiary | https://www.gpsformula.com/discipline | 2026-09-20 | GPS racing metrics |
| S41 | Vaaka cadence sensor (paddling.com, paddler.nz) | tertiary | https://paddling.com/gear/manufacturers/vaaka | 2026-09-20 | no foiling data |
| S42 | Naish blog — Super High Aspect: the future of foiling? | secondary | https://www.naish.com/blogs/blog/super-high-aspect-the-future-of-foiling | 2026-09-20 | trend statement |
| S43 | Boardsport SOURCE — Foil S/S 2025 retail buyer's guide | secondary | https://www.boardsportsource.com/retail-buyers-guide/foil-s-s-2025-retail-buyers-guide/ | 2026-09-20 | market drivers 2025 |
