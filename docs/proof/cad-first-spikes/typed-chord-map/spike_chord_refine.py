"""Spike: does exact refinement of the moved rail (more CVs, up to the 10-CV ceiling of
ADR-0001) bring the typed-chord fit under 10 um with the root-mirror lock ON?
The original rail is first knot-inserted exactly (least squares on a finer basis is exact
for a curve already in its span), then the ordinates are fitted to the target."""
import numpy as np


def basis(t, knots, p=3):
    n = len(knots) - p - 1
    if t >= knots[-1]:
        out = np.zeros(n); out[-1] = 1.0; return out
    N = np.array([1.0 if knots[i] <= t < knots[i + 1] else 0.0 for i in range(len(knots) - 1)])
    for k in range(1, p + 1):
        M = np.zeros(len(knots) - k - 1)
        for i in range(len(M)):
            a = 0.0
            if knots[i + k] > knots[i]:
                a += (t - knots[i]) / (knots[i + k] - knots[i]) * N[i]
            if knots[i + k + 1] > knots[i + 1]:
                a += (knots[i + k + 1] - t) / (knots[i + k + 1] - knots[i + 1]) * N[i + 1]
            M[i] = a
        N = M
    return N[:n]


def uniform_knots(n):
    # Nested refinement: the 7-CV knots plus inserted knots near the root first, so the
    # original rail lies exactly in every refined space (knot insertion is exact).
    base = [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1]
    extra = [0.125, 0.0625, 0.375][: n - 7]
    return np.array(sorted(base + extra), float)


def greville(knots):
    n = len(knots) - 4
    return np.array([sum(knots[i + 1:i + 4]) / 3 for i in range(n)])  # eta(t) = t


def fit(n, target, lock):
    k = uniform_knots(n)
    etas = np.array(sorted(set(list(np.linspace(0, 1, 201)) + list(k))))
    A = np.array([basis(e, k) for e in etas])  # eta(t)=t with Greville abscissae
    q = np.array([target(e) for e in etas])
    rows, rhs = [], []
    if lock:
        r = np.zeros(n); r[0], r[1] = 1, -1; rows.append(r); rhs.append(0.0)
    for idx, e in ((0, 0.0), (n - 1, 1.0)):
        r = np.zeros(n); r[idx] = 1; rows.append(r); rhs.append(target(e))
    C = np.array(rows); d = np.array(rhs)
    K = np.block([[2 * A.T @ A, C.T], [C, np.zeros((len(C), len(C)))]])
    y = np.linalg.solve(K, np.concatenate([2 * A.T @ q, d]))[:n]
    return float(np.max(np.abs(A @ y - q)))


if __name__ == "__main__":
    k7 = uniform_knots(7)
    yl = np.array([0, 0, 0.004, 0.012, 0.024, 0.034, 0.040]); yt = np.array([0.2, 0.2, 0.197, 0.185, 0.160, 0.130, 0.120])
    L = lambda e: basis(e, k7) @ yl
    T = lambda e: basis(e, k7) @ yt
    for label, s in (("linear  s=f+(1-f)eta  ", lambda e, f: f + (1 - f) * e),
                     ("rootflat s=f+(1-f)eta^2", lambda e, f: f + (1 - f) * e * e)):
        for f in (1.01, 1.2, 1.5):
            target = lambda e, f=f, s=s: L(e) + s(e, f) * (T(e) - L(e))
            res = [fit(n, target, True) for n in (7, 8, 9, 10)]
            print(f"C mirrored taper, {label} f={f:<4} lock on  residual um @7/8/9/10 CVs: " + " / ".join(f"{r*1e6:8.2f}" for r in res))
