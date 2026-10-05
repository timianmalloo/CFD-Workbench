"""err(alpha_i(eta*)) against the fixed-eta reference, divided by the run's own tip gap g = |alpha_i,k1 - alpha_i,k2|."""
import math, sys
sys.argv = [sys.argv[0], sys.argv[1], "matrix.csv"]
exec(open(sys.argv[1] + "/law.py").read().split("print(\"eta* =")[0])
from collections import defaultdict
worst = defaultdict(float)
for fname in ("matrix.csv", "falsify2.csv"):
    rows.clear()
    with open(S + "/" + fname) as f:
        for line in f:
            if line.startswith(("FAIL", "wing")): continue
            r = line.rstrip("\n").split(",")
            rows[(r[0].lstrip("P"), float(r[2]), int(r[1]), float(r[3]))].append((int(r[4]), float(r[5]), float(r[7]), float(r[12])))
    for k in rows: rows[k].sort()
    for (w, a, tw) in sorted({(k[0], k[1], k[3]) for k in rows}):
        ref = sum(ai_at(rows[(w, a, n, tw)], ETA) for n in (128, 256)) / 2
        line = []
        for n in NS:
            st = rows[(w, a, n, tw)]
            g = abs(st[0][2] - st[1][2]); err = abs(ai_at(st, ETA) - ref)
            line.append("n%d g=%.3f err=%.3f r=%.2f" % (n, g, err, err / g if g else float("nan")))
            if n <= 64 and err > 0.05: worst[n] = max(worst[n], (err - 0.05) / g)
        print("%-4s a=%-3g " % (w, a) + " | ".join(line))
print("smallest c(n) with err <= c*g + 0.05 on every case:", {n: round(v, 3) for n, v in sorted(worst.items())})
