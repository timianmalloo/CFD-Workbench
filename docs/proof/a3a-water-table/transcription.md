---
id: proof-a3a-water-table
title: "ITTC water table transcription and second check"
type: proof-pack
status: active
owner: "@track-stp"
phase: implementation
tags: [a3a, stp, water, ittc]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: proof-a3a-stp-red-first, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  Integer fresh-water and standard-seawater rows from ITTC 7.5-02-01-03 Rev 03, hashed at load.
  The second check matched Table 1 and Table 3 against the 0.1 °C appendix. The 0 °C row is a one-step extension.
---

# ITTC water table

Source: ITTC recommended procedure 7.5-02-01-03 Rev 03 (2024), standard pressure 0.101325 MPa. The product table is the integer temperatures 0 through 50 °C at two salinity columns, fresh water (0 g/kg) and standard seawater (35.16504 g/kg). Temperature and salinity between those nodes are linear. A query outside the band is refused and is never clamped. Vapour pressure is stored in pascals. The resource is `src/CfdWorkbench.Analysis/IttcWater.csv`. Its BLAKE3 is `fca86591cf575f50c0c17ca6de8dac6372d0c2baaa40f7711d184c2b6e154480`.

The integer rows from 0.1 °C through 50 °C were read from the procedure's 0.1 °C appendices (fresh pages 18–32, seawater pages 33–47). The published tables start at 0.1 °C. The product band includes 0 °C, so the 0 °C row is the one-step linear extension of the 0.1 °C and 0.2 °C rows. It is not a clamp: fresh 0 °C density is 999.8433 kg/m³ and the 0.1 °C density is 999.8498 kg/m³.

## Rows used by the checks

| Water | T °C | ρ kg/m³ | ν m²/s | p_v Pa |
|---|---:|---:|---:|---:|
| Fresh | 15 | 999.1026 | 1.1386e-6 | 1705.8 |
| Standard seawater | 15 | 1026.0210 | 1.1892e-6 | 1670.9 |

ν_sea / ν_fresh = 1.1892e-6 / 1.1386e-6, so Re falls by 4.255 %, inside 4.25 ± 0.05 %. ρ_sea / ρ_fresh = 1.026942, which is not the rounded 1.0269.

## Second check

Signed by track STP, 2026-10-04. Every integer row of Table 1 (fresh water, 10–40 °C, 31 rows) and Table 3 (standard seawater, 1–30 °C, 30 rows) was compared, token for token, with the same temperature in the 0.1 °C appendix. Density, kinematic viscosity and vapour pressure all matched. No row was filled from a formula or from the knowledge-base rounding.
