import math

def kh(s, K=2.0, a=0.15, s0=0.5, w=0.06):
    return K * s**4 + a * math.exp(-((s - s0) / w) ** 2)

def dk(s, K=2.0, a=0.15, s0=0.5, w=0.06):
    return 4 * K * s**3 - 2 * a * (s - s0) / w**2 * math.exp(-((s - s0) / w) ** 2)

def roots(N=200000, **kw):
    out = []
    prev = dk(0, **kw)
    for i in range(1, N + 1):
        s = i / N
        cur = dk(s, **kw)
        if prev * cur < 0:
            lo, hi = (i - 1) / N, s
            for _ in range(60):
                m = (lo + hi) / 2
                if dk(lo, **kw) * dk(m, **kw) <= 0:
                    hi = m
                else:
                    lo = m
            out.append((lo + hi) / 2)
        prev = cur
    return out

def zig(seq, theta):
    dirn = 0
    ext = seq[0]
    n = 1
    for k in seq[1:]:
        if dirn == 0:
            if k - seq[0] > theta: dirn, ext = 1, k
            elif seq[0] - k > theta: dirn, ext = -1, k
            continue
        if dirn == 1:
            if k > ext: ext = k
            elif k < ext - theta: n += 1; dirn, ext = -1, k
        else:
            if k < ext: ext = k
            elif k > ext + theta: n += 1; dirn, ext = 1, k
    return n

for a in (0.15, 0.2, 0.25):
    r = roots(a=a)
    seq = [kh(0,a=a)] + [kh(x,a=a) for x in r] + [kh(1,a=a)]
    print('a', a, 'roots', [round(x, 4) for x in r], 'k at ext', [round(kh(x, a=a), 4) for x in r], 'ends', kh(0), kh(1))
    if len(r) >= 2:
        print(' peak-dip drop', round(kh(r[0], a=a) - kh(r[1], a=a), 5))
    for f in (0.5, 1, 2):
        print('  theta', f * 0.02, 'count', zig(seq, f * 0.02))
    print('  tau->0 count', zig(seq, 1e-9))
