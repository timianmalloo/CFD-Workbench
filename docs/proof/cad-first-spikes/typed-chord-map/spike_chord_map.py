"""Spike: typed root/tip chord (A4.15) mapped onto the trailing rail of record.

Question: does c'(eta) = s(eta) c(eta), s linear (operator decision), fit on the
trailing rail's OWN knots and CV abscissae (ordinates only, IDs kept) within the
model/join tolerance (10 um), with and without the default root-mirror lock?
Leading edge held (DR-2 default). Degree 3, 7 CVs, knots as the Example.
Residual is measured on the distribution-curve oracle set (201 uniform eta + knots).
"""
import numpy as np

KNOTS = np.array([0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1], float)
P = 3


def basis(t, knots=KNOTS, p=P):
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


def param_at(eta, ab):
    lo, hi = 0.0, 1.0
    for _ in range(80):
        mid = 0.5 * (lo + hi)
        if basis(mid) @ ab < eta: lo = mid
        else: hi = mid
    return 0.5 * (lo + hi)


def rail(ab, ords):
    return lambda eta: basis(param_at(eta, ab)) @ ords


def sample_set(ab):
    etas = list(np.linspace(0, 1, 201))
    etas += [float(basis(k) @ ab) for k in KNOTS]
    return np.array(sorted(set(etas)))


def fit_trailing(ab_t, target, root_mirror, pin_tip=True):
    etas = sample_set(ab_t)
    A = np.array([basis(param_at(e, ab_t)) for e in etas])
    q = np.array([target(e) for e in etas])
    rows, rhs = [], []
    if root_mirror:
        r = np.zeros(7); r[0], r[1] = 1, -1; rows.append(r); rhs.append(0.0)
    r = np.zeros(7); r[0] = 1; rows.append(r); rhs.append(target(0.0))      # root chord hit exactly
    if pin_tip:
        r = np.zeros(7); r[-1] = 1; rows.append(r); rhs.append(target(1.0))  # tip chord unchanged
    C = np.array(rows); d = np.array(rhs)
    K = np.block([[2 * A.T @ A, C.T], [C, np.zeros((len(C), len(C)))]])
    sol = np.linalg.solve(K, np.concatenate([2 * A.T @ q, d]))
    y = sol[:7]
    return y, float(np.max(np.abs(A @ y - q)))


GREV = np.array([sum(KNOTS[i + 1:i + P + 1]) / P for i in range(7)])  # eta(t) = t
EX = np.array([0, 0.1, 0.3, 0.5, 0.7, 0.9, 1.0])                      # Example abscissae


def case(name, ab_l, y_l, ab_t, y_t, f, end, root_mirror):
    L, T = rail(ab_l, y_l), rail(ab_t, y_t)
    c = lambda e: T(e) - L(e)
    s = (lambda e: f + (1 - f) * e) if end == "root" else (lambda e: 1 + (f - 1) * e)
    target = lambda e: L(e) + s(e) * c(e)
    _, res = fit_trailing(ab_t, target, root_mirror, pin_tip=True)
    print(f"{name:58s} f={f:<4} end={end:4s} lock={'on ' if root_mirror else 'off'} max residual = {res*1e6:10.2f} um")


if __name__ == "__main__":
    # A: Example rails (constant 120 mm chord, flat), root-mirror default ON.
    case("A Example, constant chord 120 mm", EX, np.zeros(7), EX, np.full(7, 0.120), 1.5, "root", True)
    case("A Example, constant chord 120 mm", EX, np.zeros(7), EX, np.full(7, 0.120), 1.5, "root", False)
    case("A Example, constant chord 120 mm (tip x0.8)", EX, np.zeros(7), EX, np.full(7, 0.120), 0.8, "tip", True)
    # B: CAD-17 linear taper 200 -> 50 mm, straight LE, locks off (the fixture has a root kink).
    yl = np.zeros(7); yt = 0.20 - 0.15 * GREV
    case("B CAD-17 linear taper 200->50 mm", GREV, yl, GREV, yt, 1.2, "root", False)
    case("B CAD-17 linear taper 200->50 mm", GREV, yl, GREV, yt, 0.8, "tip", False)
    # C: root-mirrored taper: TE flat at root, 200 -> 80 mm, LE swept 0 -> 40 mm flat at root.
    yl = np.array([0, 0, 0.004, 0.012, 0.024, 0.034, 0.040]); yt = np.array([0.2, 0.2, 0.197, 0.185, 0.160, 0.130, 0.120])
    for lock in (True, False):
        case("C mirrored taper 200->80 mm, swept LE", GREV, yl, GREV, yt, 1.2, "root", lock)
    case("C mirrored taper 200->80 mm, swept LE", GREV, yl, GREV, yt, 0.8, "tip", True)
    case("C mirrored taper, small change", GREV, yl, GREV, yt, 1.01, "root", True)
