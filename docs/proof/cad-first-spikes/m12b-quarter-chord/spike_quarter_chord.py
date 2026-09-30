"""Spike (M1.2b design-slice, 2026-09-30): typed Root/Tip chord under the ruled rules.

Ruling 53: DR-2 = quarter-chord line held (rigid x-translation so LE(0) = 0 after a root edit);
DR-9 = root-flat blend s = f + (1-f) eta^2 (root) or 1 + (f-1) eta^2 (tip) on root-mirror-locked rails,
both numbers reported. Question: on the New foil the operator actually opens (FoilSource.NewDefault:
10 CVs per rail, tip-clustered knots power 3, near-elliptic chord sqrt(1 - 0.99 eta^2), straight
quarter-chord line, area 0.1 m^2, b = 1 m) and on the 7-CV Example, what residual does each rail's
ordinate-only refit leave against the root-flat target, and how far is the result from the
operator's linear rule? Both rails move under quarter-chord hold, so both are refitted.

Hard rows per rail (ADR-0006 decision 3): root_mirror P0 = P1; the typed end exact; the other end
exact (its target value). Residual is max |fit - target| on the distribution-curve oracle set
(201 uniform eta + every knot image), A4.5. Run: uv run --with numpy python3 spike_quarter_chord.py
"""
import numpy as np

P = 3


def basis(t, knots, p=P):
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


def greville(knots, n):
    return np.array([sum(knots[i + 1:i + P + 1]) / P for i in range(n)])


def param_at(eta, knots, ab):
    lo, hi = 0.0, 1.0
    for _ in range(70):
        mid = 0.5 * (lo + hi)
        if basis(mid, knots) @ ab < eta: lo = mid
        else: hi = mid
    return 0.5 * (lo + hi)


class Rail:
    def __init__(self, knots, ab, y):
        self.knots, self.ab, self.y = np.array(knots, float), np.array(ab, float), np.array(y, float)
        self.n = len(ab)

    def __call__(self, eta):
        return basis(param_at(eta, self.knots, self.ab), self.knots) @ self.y

    def samples(self):
        etas = list(np.linspace(0, 1, 201)) + [float(basis(k, self.knots) @ self.ab) for k in self.knots]
        return np.array(sorted(set(etas)))

    def fit(self, target):
        etas = self.samples()
        A = np.array([basis(param_at(e, self.knots, self.ab), self.knots) for e in etas])
        q = np.array([target(e) for e in etas])
        rows, rhs = [], []
        r = np.zeros(self.n); r[0], r[1] = 1, -1; rows.append(r); rhs.append(0.0)       # root_mirror
        r = np.zeros(self.n); r[0] = 1; rows.append(r); rhs.append(target(0.0))           # root end exact
        r = np.zeros(self.n); r[-1] = 1; rows.append(r); rhs.append(target(1.0))          # tip end exact
        C, d = np.array(rows), np.array(rhs)
        K = np.block([[2 * A.T @ A, C.T], [C, np.zeros((len(C), len(C)))]])
        y = np.linalg.solve(K, np.concatenate([2 * A.T @ q, d]))[:self.n]
        return y, float(np.max(np.abs(A @ y - q))), A, etas


def new_foil():
    n, power = 10, 3.0
    interior = n - P - 1
    knots = [0.0] * (P + 1) + [1 - (1 - i / (interior + 1)) ** power for i in range(1, interior + 1)] + [1.0] * (P + 1)
    knots = np.array(knots); ab = greville(knots, n)
    eta = np.linspace(0, 1, 401)
    chord = lambda e: np.sqrt(np.maximum(0, 1 - 0.99 * e * e))
    lead_t, trail_t = (1 - chord(eta)) / 4, (1 - chord(eta)) / 4 + chord(eta)
    A = np.array([basis(e, knots) for e in eta])        # Greville abscissae: eta(t) = t
    def pinned(target, pins):
        C = np.zeros((len(pins), n)); d = np.zeros(len(pins))
        for k, (i, v) in enumerate(pins): C[k, i] = 1; d[k] = v
        K = np.block([[2 * A.T @ A, C.T], [C, np.zeros((len(pins), len(pins)))]])
        return np.linalg.solve(K, np.concatenate([2 * A.T @ target, d]))[:n]
    c1 = float(chord(1.0))
    lead = pinned(lead_t, [(0, 0.0), (1, 0.0), (n - 1, (1 - c1) / 4)])
    trail = pinned(trail_t, [(0, 1.0), (1, 1.0), (n - 1, (1 - c1) / 4 + c1)])
    L, T = Rail(knots, ab, lead), Rail(knots, ab, trail)
    half = 0.5
    area = 2 * half * np.trapezoid([T(e) - L(e) for e in np.linspace(0, 1, 2001)], np.linspace(0, 1, 2001))
    s = 0.1 / area
    return Rail(knots, ab, lead * s), Rail(knots, ab, trail * s)


def example():
    knots = [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1]
    ab = [0, 0.1, 0.3, 0.5, 0.7, 0.9, 1.0]
    return Rail(knots, ab, np.zeros(7)), Rail(knots, ab, np.full(7, 0.120))


def case(name, L, T, f, end):
    c = lambda e: T(e) - L(e)
    flat = (lambda e: f + (1 - f) * e * e) if end == "root" else (lambda e: 1 + (f - 1) * e * e)
    lin = (lambda e: f + (1 - f) * e) if end == "root" else (lambda e: 1 + (f - 1) * e)
    le_t = lambda e, s=flat: L(e) + (1 - s(e)) * c(e) / 4
    te_t = lambda e, s=flat: L(e) + c(e) / 4 + 3 * s(e) * c(e) / 4
    yl, rl, Al, el = L.fit(le_t)
    yt, rt, At, et = T.fit(te_t)
    delta = le_t(0.0)                     # rigid x-translation so LE(0) = 0 (exact: partition of unity)
    op_l = np.max(np.abs(Al @ yl - np.array([L(e) + (1 - lin(e)) * c(e) / 4 for e in el])))
    op_t = np.max(np.abs(At @ yt - np.array([L(e) + c(e) / 4 + 3 * lin(e) * c(e) / 4 for e in et])))
    c0 = c(0.0) * (f if end == "root" else 1)
    print(f"{name:34s} {end:4s} f={f:<5} residual LE {rl*1e6:8.2f} um · TE {rt*1e6:8.2f} um"
          f" · from linear rule {max(op_l, op_t)*1e6:9.2f} um · translation {-delta*1e3:8.3f} mm · new root chord {c0*1e3:7.2f} mm")


if __name__ == "__main__":
    L, T = new_foil()
    print(f"New foil: root chord {(T(0)-L(0))*1e3:.3f} mm, tip chord {(T(1)-L(1))*1e3:.3f} mm, LE(0) {L(0)*1e3:.3f} mm")
    for f in (1.01, 1.05, 1.1, 1.2, 1.5, 0.9, 0.8):
        case("New foil (10 CV, near-elliptic)", L, T, f, "root")
    for f in (1.1, 0.8, 0.5):
        case("New foil (10 CV, near-elliptic)", L, T, f, "tip")
    L, T = example()
    for f in (1.01, 1.1, 1.2, 1.5):
        case("Example (7 CV, constant 120 mm)", L, T, f, "root")
    case("Example (7 CV, constant 120 mm)", L, T, 0.8, "tip")
