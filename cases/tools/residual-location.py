"""Locate the largest cell residuals of a written OpenFOAM residual field (binary or ascii internalField)."""
import re
import struct
import sys

import numpy as np

run, time, field = sys.argv[1], sys.argv[2], sys.argv[3]


def internal(path):
    data = open(path, "rb").read()
    binary = re.search(rb"format\s+binary", data[:2000]) is not None
    m = re.search(rb"internalField\s+nonuniform\s+List<scalar>\s*(\d+)\s*\(", data)
    n = int(m.group(1))
    if binary:
        return np.array(struct.unpack("<%dd" % n, data[m.end():m.end() + 8 * n]))
    return np.array(data[m.end():data.index(b")", m.end())].split()[:n], dtype=float)


r = np.abs(internal(f"{run}/{time}/{field}"))
cx, cy = internal(f"{run}/{time}/Cx"), internal(f"{run}/{time}/Cy")
order = np.argsort(-r)
tot = r.sum()
print(f"{field}: cells={r.size} sum|r|={tot:.3e} max={r.max():.3e}")
print(f"share of sum|r| in top 10 cells: {r[order[:10]].sum() / tot:.3f}; top 100: {r[order[:100]].sum() / tot:.3f}")
rad = np.hypot(cx - 0.5, cy)
for lo, hi in ((0, 1), (1, 10), (10, 100), (100, 1e9)):
    sel = (rad >= lo) & (rad < hi)
    print(f"  r from mid-chord in [{lo}, {hi}): share {r[sel].sum() / tot:.3f}")
for k in order[:8]:
    print(f"  cell {k}: r={r[k]:.3e} at ({cx[k]:.5g}, {cy[k]:.5g})")
