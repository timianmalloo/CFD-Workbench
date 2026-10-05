"""Ruling 75 law check: judge every strip outboard of eta* at alpha_i(eta*), eta* = (1+cos(pi/128))/2 (default-lattice tip
midpoint), alpha_i(eta*) linear in ln(1-eta) between the run's own strips. Prints raw vs law verdicts and the error
of alpha_i(eta*) against the fixed-eta reference (mean of n=128 and n=256 values)."""
import csv, math, sys
from collections import defaultdict
S, fname = sys.argv[1], sys.argv[2]
rows = defaultdict(list)
with open(S + "/" + fname) as f:
    for line in f:
        if line.startswith("FAIL") or line.startswith("wing"): continue
        r = line.rstrip("\n").split(",")
        rows[(r[0].lstrip("P"), float(r[2]), int(r[1]), float(r[3]))].append((int(r[4]), float(r[5]), float(r[7]), float(r[12])))
for k in rows: rows[k].sort()
ETA = 0.5 * (1 + math.cos(math.pi / 128))
NS = [16, 32, 64, 128, 256]
BOUND = 10.0

def ai_at(st, eta):
    x = math.log(1 - eta)
    if eta >= st[0][1]: a, b = st[0], st[1]
    else:
        a, b = next((st[i], st[i + 1]) for i in range(len(st) - 1) if st[i + 1][1] <= eta <= st[i][1])
    xa, xb = math.log(1 - a[1]), math.log(1 - b[1])
    return a[2] + (b[2] - a[2]) * (x - xa) / (xb - xa)

def verdict(v, u): return "in" if v + u <= BOUND else ("OUT" if v - u > BOUND else "at-bound")

print("eta* = 1 - %.4e" % (1 - ETA))
cases = sorted({(k[0], k[1], k[3]) for k in rows})
for (w, a, tw) in cases:
    ref_vals = [ai_at(rows[(w, a, n, tw)], ETA) for n in (128, 256) if (w, a, n, tw) in rows]
    ref = sum(ref_vals) / len(ref_vals)
    print("\n%s alpha=%g twist=%g  alpha_i(eta*) reference %.3f (n128/256 spread %.3f)" % (w, a, tw, ref, max(ref_vals) - min(ref_vals)))
    print("   n  tip-eta-gap  raw: n_out  tip_aeff  |  law: ai*     err    rel_err  max_aeff*(outboard)  verdict(U)")
    for n in NS:
        key = (w, a, n, tw)
        if key not in rows: print("%4d  failed" % n); continue
        st = rows[key]
        raw_out = sum(1 for s in st if s[3] > BOUND)
        ai = ai_at(st, ETA)
        err = ai - ref
        # alpha_eff* for strips outboard of eta*: own alpha_eff with alpha_i replaced by alpha_i(eta*)
        outboard = [s[3] + s[2] - ai for s in st if s[1] > ETA] or [st[0][3] + st[0][2] - ai]
        rel = abs(err) / abs(ref) if ref else float("nan")
        u = {16: 2.5, 32: 1.0, 64: 0.4}.get(n, 0.1) * abs(st[0][2] - st[1][2]) + 0.05
        print("%4d  %.2e  %5d  %8.3f  |  %7.3f  %+7.3f  %6.3f  %8.3f   U=%.2f %s" % (
            n, 1 - st[0][1], raw_out, st[0][3], ai, err, rel, max(outboard), u, verdict(max(outboard), u)))
