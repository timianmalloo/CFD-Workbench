#!/usr/bin/env python3
"""Three-grid observed order, Richardson extrapolation and GCI (ASME V&V 20-2009 / Celik et al. 2008), constant r.

Run: python3 cases/tools/gci.py <r> <name> <S_fine> <S_medium> <S_coarse> [<U_I_fine> <U_I_medium> <U_I_coarse>]
Fs = 1.25 (three grids). Prints eps21, eps32, R = eps21/eps32, p, S_ext, e_a21, e_ext21, GCI_fine, and, when the
iterative half-bands are given, the A4 clause-5 admission U_I <= 0.01 |eps| of each neighbouring pair.
Monotonic convergence needs 0 < R < 1; otherwise no p and no GCI are reported (a finding, not a number).
"""
import math
import sys

r = float(sys.argv[1])
name = sys.argv[2]
s1, s2, s3 = (float(x) for x in sys.argv[3:6])
e21, e32 = s2 - s1, s3 - s2
R = e21 / e32 if e32 != 0 else float("inf")
print(f"{name}: S1(fine)={s1:.9f} S2={s2:.9f} S3(coarse)={s3:.9f} eps21={e21:.4e} eps32={e32:.4e} R={R:.4f}")
if len(sys.argv) > 6:
    u1, u2, u3 = (float(x) for x in sys.argv[6:9])
    a21 = max(u1, u2) <= 0.01 * abs(e21)
    a32 = max(u2, u3) <= 0.01 * abs(e32)
    print(f"{name}: A4 clause 5 admission pair21={'yes' if a21 else 'no'} (max U_I {max(u1, u2):.2e} vs {0.01 * abs(e21):.2e}); "
          f"pair32={'yes' if a32 else 'no'} (max U_I {max(u2, u3):.2e} vs {0.01 * abs(e32):.2e})")
if not 0 < R < 1:
    print(f"{name}: convergence is {'oscillatory' if R < 0 else 'divergent'} (R={R:.4f}); no observed order, no GCI")
    sys.exit(0)
p = math.log(abs(e32 / e21)) / math.log(r)
s_ext = (r ** p * s1 - s2) / (r ** p - 1)
ea = abs(e21 / s1)
eext = abs((s_ext - s1) / s_ext)
gci = 1.25 * ea / (r ** p - 1)
print(f"{name}: p={p:.3f} S_ext={s_ext:.9f} e_a21={ea:.3e} e_ext21={eext:.3e} GCI_fine={gci:.3e} (U_SN fine = {gci * abs(s1):.3e} absolute)")
