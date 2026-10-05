"""Tip-study analysis (Ruling 75): fixed-eta convergence, ln(1-eta) fit, candidate laws and their errors."""
import csv, math, sys
from collections import defaultdict
S = sys.argv[1]
rows = defaultdict(list)  # (wing, alpha, n) -> [(k, eta, ai, aeff, verdict)]
with open(S + "/matrix.csv") as f:
    for r in csv.DictReader(f):
        w = r["wing"].lstrip("P")
        rows[(w, float(r["alpha"]), int(r["n"]), float(r["twist"]))].append(
            (int(r["k_from_tip"]), float(r["eta"]), float(r["alpha_i_deg"]), float(r["alpha_eff"]), r["verdict"]))
for key in rows: rows[key].sort()
NS = [16, 32, 64, 128, 256]

def strip(key, k): return rows[key][k - 1]

def interp_ln(key, eta_ref):
    """alpha_i at eta_ref, linear in ln(1-eta) between the bracketing strips; extrapolate from k1,k2 when outboard of k1."""
    st = rows[key]
    x_ref = math.log(1 - eta_ref)
    if eta_ref >= st[0][1]:
        a, b = st[0], st[1]
    else:
        for i in range(len(st) - 1):
            if st[i + 1][1] <= eta_ref <= st[i][1]:
                a, b = st[i], st[i + 1]; break
    xa, xb = math.log(1 - a[1]), math.log(1 - b[1])
    return a[2] + (b[2] - a[2]) * (x_ref - xa) / (xb - xa)

mode = sys.argv[2]
if mode == "fixed-eta":
    # alpha_i at fixed eta (the n=32 and n=64 tip midpoints) read from every finer lattice by interpolation
    for w in ["ell", "rect", "tap"]:
        for a in [5.0]:
            for nref in [32, 64]:
                eta = strip((w, a, nref, 0.0), 1)[1]
                vals = [interp_ln((w, a, n, 0.0), eta) for n in NS if n >= nref]
                print(w, a, "eta_ref=1-%.3e" % (1 - eta), "alpha_i at eta_ref, n>=%d:" % nref, " ".join("%.3f" % v for v in vals))
if mode == "fit":
    # alpha_i = c0 + c1*ln(1-eta), per (wing, alpha), on every strip with 1-eta < 1e-3 from all lattices
    for w in ["ell", "rect", "tap"]:
        for a in [2.0, 5.0, 8.0]:
            pts = [(math.log(1 - e), ai) for n in NS for (k, e, ai, ae, v) in rows[(w, a, n, 0.0)] if 1 - e < 1e-3]
            nx = len(pts); mx = sum(p[0] for p in pts) / nx; my = sum(p[1] for p in pts) / nx
            sxx = sum((p[0] - mx) ** 2 for p in pts); sxy = sum((p[0] - mx) * (p[1] - my) for p in pts)
            c1 = sxy / sxx; c0 = my - c1 * mx
            res = [p[1] - (c0 + c1 * p[0]) for p in pts]
            print("%-4s a=%.0f pts=%2d c1=%+.4f deg/ln c0=%+.3f  max|res|=%.3f  rms=%.3f  c1/sin(a)=%+.3f" % (
                w, a, nx, c1, c0, max(abs(r) for r in res), math.sqrt(sum(r * r for r in res) / nx), c1 / math.sin(math.radians(a))))
    # local slope d alpha_i / d ln(1-eta) between successive tip strips, elliptic a=5, to see whether the log slope settles
    for n in NS:
        st = rows[("ell", 5.0, n, 0.0)]
        sl = [(st[i][2] - st[i + 1][2]) / (math.log(1 - st[i][1]) - math.log(1 - st[i + 1][1])) for i in range(3)]
        print("ell a5 n%-3d local slope k1-k2,k2-k3,k3-k4:" % n, " ".join("%.3f" % s for s in sl))
if mode in ("law", "falsify"):
    keys = sorted(k for k in rows if (mode == "law") == (k[3] == 0.0 and k[1] in (2.0, 5.0, 8.0)) or mode == "falsify")
    print("wing  alpha twist |  raw tip alpha_eff n16..256 (verdict)  |  law alpha_eff* n16..256  | err vs n64 tip, deg")
    for (w, a, n0, tw) in sorted({(k[0], k[1], 0, k[3]) for k in keys}):
        ref = (w, a, 64, tw)
        if ref not in rows: continue
        eta_ref = strip(ref, 1)[1]
        raw, law, err = [], [], []
        for n in NS:
            key = (w, a, n, tw)
            if key not in rows: continue
            k1 = strip(key, 1)
            raw.append("%.2f%s" % (k1[3], "o" if k1[3] > 10 else "i"))
            ai_star = interp_ln(key, eta_ref) if k1[1] > eta_ref or n < 64 else k1[2]
            if n == 64: ai_star = k1[2]
            aeff_star = k1[3] + k1[2] - ai_star
            law.append("%.2f%s" % (aeff_star, "o" if aeff_star > 10 else "i"))
            err.append("%.3f" % (ai_star - strip(ref, 1)[2]))
        print("%-4s %4.0f %5.1f | %s | %s | %s" % (w, a, tw, " ".join(raw), " ".join(law), " ".join(err)))
