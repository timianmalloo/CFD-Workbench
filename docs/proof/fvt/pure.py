"""Item 1: the old and new FourViews arrangement assertions as pure functions on the logged Windows numbers."""
import sys

def tolerance(scale, exact):  # DevicePixel.Tolerance
    return exact if scale % 1 == 0 else max(exact, 1 / scale)

def rounded(dip, scale):  # DevicePixel.Rounded: Math.Round(dip*scale, ToEven)/scale (Python round is half-to-even)
    return round(dip * scale) / scale

def near(expected, actual, tol):
    return abs(expected - actual) <= tol

def old(req, act, scale):
    return near(req, act, 0)

def new(req, act, scale):
    return near(req, act, tolerance(scale, 0))

# Logged Windows numbers: r177-150-views.stdout.txt:7 (width) and the brief's height arithmetic.
cases = [("width", 647, 647.3333333333334, 1.5), ("height", 487, 730 / 1.5, 1.5)]
bad = 0
for name, req, act, s in cases:
    o, n = old(req, act, s), new(req, act, s)
    print(f"{name}: requested {req}, actual {act:.4f}, scale {s}: old={'PASS' if o else 'FAIL'} new={'PASS' if n else 'FAIL'}"
          f" | Rounded({req},{s})={rounded(req, s):.4f} (== actual: {abs(rounded(req, s) - act) < 1e-9})")
    bad += (o) or (not n)
# Scales 1 and 2 on the Mac: exact stays exact (tolerance 0) so the decisions are unchanged.
for s in (1, 2):
    for req in (648, 647, 488, 487):
        assert tolerance(s, 0) == 0 and old(req, float(req), s) == new(req, float(req), s) == True
        assert old(req, req + 1 / s, s) == new(req, req + 1 / s, s) == False  # one device pixel off still fails
print("scales 1 and 2: tolerance 0, old == new on exact and off-by-one-device-pixel layouts")
# Boundary: the four/one decision depends on the laid-out grid, not on this assertion; at 150 % the 648 x 488 case is exact.
print("648 at 1.5:", 648 * 1.5, "488 at 1.5:", 488 * 1.5, "(whole device pixels, layout is exact)")
sys.exit(1 if bad else 0)
