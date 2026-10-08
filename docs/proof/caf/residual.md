---
id: proof-caf-residual
title: "Track CAF - residual of naca4-closed/2 against /1 (P4)"
type: doc
status: done
owner: "@trk-caf"
phase: build
tags: [catalog, determinism, ruling-156, residual]
links:
  - {to: review-cat-geometry, rel: implements}
review-by: 2027-04-01
summary: >-
  Measured change of the three shipped NACA entries between generator naca4-closed/1 and /2: points changed, max ordinate change,
  and the 4412 frame deltas. All sit far under the 1e-6 identity tolerance, so no catalog id changes.
---

# Residual of naca4-closed/2 against /1 (Ruling 156 P4)

Measured on macOS arm64, .NET 10, 2026-10-08. Method: the committed `/1` `.dat` files (git `9e4154f5`) against the `/2` files written by the
generator in this branch (161 points each; the point lines of the Selig file, header excluded). Script:
`/Users/mallalieut/projects/cfd-workbench-continuation/scratch/caf/residual.py` (scratch, not committed). Label: **Verified** (observed here).

| entry | points changed | max abs change, x | max abs change, y | max abs change, chord |
|---|---|---|---|---|
| naca-0009 | 60 of 161 | 2.220e-16 | 3.123e-17 | 2.220e-16 |
| naca-0012 | 60 of 161 | 2.220e-16 | 4.163e-17 | 2.220e-16 |
| naca-4412 | 160 of 161 | 8.767e-12 | 1.503e-11 | 1.503e-11 |

The 4412 frame row in `catalog.tsv`:

| quantity | /1 | /2 | change |
|---|---|---|---|
| LE shift (chord) | -0.00030024789938986609 | -0.00030024789938986609 | 0 |
| rotation (degrees) | -0.17398455663928378 | -0.17398455570619686 | 9.331e-10 |
| scale | 1.0003048597813819 | 1.0003048597813324 | -4.952e-14 |

Reading:

- Largest change anywhere is 1.503e-11 chord (4412 y), against the 1e-6 identity tolerance stated in `docs/reviews/cat-geometry.md` finding 2:
  a factor of about 6.7e4 below it. The tolerance value is taken from that review; I did not re-derive it from code. The catalog ids do not change.
- The numbers match the geometry review's figures (60 of 161 and 2.2e-16 for 0009/0012; 160 of 161, 1.50e-11, 9.3e-10 degrees for 4412; scale -5e-14).
- 0009 and 0012 change only at the spacing (`CosPi` against `Math.Cos(Math.PI*i/80)`); sin and atan are exactly 0 on their path.
- All three `.dat` files, the three SHA-256 values and the 4412 rotation and scale doubles are re-recorded once, under Ruling 156. The LE shift string is unchanged.
- Cross-OS determinism of the `/2` bytes is **Inferred until the Windows ring** (P5).
