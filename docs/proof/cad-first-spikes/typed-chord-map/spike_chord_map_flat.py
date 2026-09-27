"""Spike variant: root-flat blend s = f + (1-f) eta^2 (root) or 1 + (f-1) eta^2 (tip), root-mirror lock ON.

Reports two numbers per case (geometry council, 2026-09-26), both on the distribution-curve
oracle set (201 uniform eta + knot images):
  fit      = max |fitted rail - root-flat target|  (residual against the substituted rule)
  operator = max |fitted rail - linear target|     (deviation from the operator's A4.15 / CAD-16 rule)"""
import numpy as np
from spike_chord_map import rail, fit_trailing, sample_set, basis, param_at, EX, GREV


def case(name, ab_l, y_l, ab_t, y_t, f, end):
    L, T = rail(ab_l, y_l), rail(ab_t, y_t)
    flat = (lambda e: f + (1 - f) * e * e) if end == "root" else (lambda e: 1 + (f - 1) * e * e)
    linear = (lambda e: f + (1 - f) * e) if end == "root" else (lambda e: 1 + (f - 1) * e)
    target = lambda e: L(e) + flat(e) * (T(e) - L(e))
    operator = lambda e: L(e) + linear(e) * (T(e) - L(e))
    y, res = fit_trailing(ab_t, target, True, pin_tip=True)
    etas = sample_set(ab_t)
    fitted = np.array([basis(param_at(e, ab_t)) @ y for e in etas])
    dev = float(np.max(np.abs(fitted - np.array([operator(e) for e in etas]))))
    print(f"{name:44s} f={f:<4} end={end:4s} lock=on  root-flat: fit {res*1e6:9.2f} um · from operator rule {dev*1e6:9.2f} um")


case("A Example, constant chord 120 mm", EX, np.zeros(7), EX, np.full(7, 0.120), 1.5, "root")
case("A Example, constant chord 120 mm", EX, np.zeros(7), EX, np.full(7, 0.120), 0.8, "tip")
yl = np.array([0, 0, 0.004, 0.012, 0.024, 0.034, 0.040]); yt = np.array([0.2, 0.2, 0.197, 0.185, 0.160, 0.130, 0.120])
case("C mirrored taper 200->80 mm, swept LE", GREV, yl, GREV, yt, 1.2, "root")
case("C mirrored taper 200->80 mm, swept LE", GREV, yl, GREV, yt, 0.8, "tip")
case("C mirrored taper, small change", GREV, yl, GREV, yt, 1.01, "root")
