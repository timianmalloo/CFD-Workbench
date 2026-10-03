"""Print where the faces of an OpenFOAM faceSet sit (centre x, y ranges and a coarse histogram)."""
import re
import sys

import numpy as np

run, setname = sys.argv[1], sys.argv[2]
poly = f"{run}/constant/polyMesh"


def body(path):
    t = open(path).read()
    t = t[t.index("}", t.index("FoamFile")) + 1:]
    return t


pts = np.array([list(map(float, m)) for m in re.findall(r"\(([-\d.eE+]+) ([-\d.eE+]+) ([-\d.eE+]+)\)", body(f"{poly}/points"))])
faces = [list(map(int, m.split())) for m in re.findall(r"4\(([\d ]+)\)", body(f"{poly}/faces"))]
s = body(f"{poly}/sets/{setname}")
ids = list(map(int, re.search(r"\d+\s*\(([\d\s]*)\)", s).group(1).split()))
c = np.array([pts[faces[i]].mean(axis=0) for i in ids])
r = np.hypot(c[:, 0] - 1, c[:, 1])
print(f"{len(ids)} faces; x [{c[:,0].min():.4g}, {c[:,0].max():.4g}] y [{c[:,1].min():.4g}, {c[:,1].max():.4g}]")
for lo, hi in ((0, 0.01), (0.01, 0.1), (0.1, 1), (1, 10), (10, 100), (100, 1e9)):
    print(f"  distance from TE in [{lo}, {hi}): {int(((r >= lo) & (r < hi)).sum())}")
