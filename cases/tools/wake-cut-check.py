"""Check wake-cut pairing and grid-line angles near the cut for a TMR PLOT3D 2-D C-grid."""
import gzip
import sys

import numpy as np

t = gzip.decompress(open(sys.argv[1], "rb").read()).split()
ni, nj = int(t[1]), int(t[2])
v = np.array(t[3:3 + 2 * ni * nj], dtype=float)
X, Y = v[:ni * nj].reshape(nj, ni).T, v[ni * nj:].reshape(nj, ni).T
ite = int(sys.argv[2])
cx = 0.25 * (X[:-1, 0] + X[1:, 0] + X[:-1, 1] + X[1:, 1])
cy = 0.25 * (Y[:-1, 0] + Y[1:, 0] + Y[:-1, 1] + Y[1:, 1])
worst = 0
for i in range(ite):
    m = ni - 2 - i
    d = np.array([cx[m] - cx[i], cy[m] - cy[i]])
    ang = np.degrees(np.arctan2(abs(d[0]), abs(d[1])))  # cut face normal is +-y
    worst = max(worst, ang)
print(f"wake-cut pair max non-orthogonality (centroid approx) = {worst:.2f} deg")
# angle between the j=0 -> j=1 grid line and the cut normal, lower side
angs = [np.degrees(np.arctan2(abs(X[i, 1] - X[i, 0]), abs(Y[i, 1] - Y[i, 0]))) for i in range(ite)]
print(f"j-line skew vs cut normal, lower wake: max {max(angs):.2f} deg, median {np.median(angs):.2f} deg")
h = [Y[i, 1] - Y[i, 0] for i in range(ite)]
w = [abs(X[i + 1, 0] - X[i, 0]) for i in range(ite)]
print(f"first-cell height at cut: min {min(map(abs, h)):.3e}; cell length along cut: max {max(w):.3e}")
