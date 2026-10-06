import math, sys, gmsh
chord, half, t_te = 0.12, 0.025, 0.0005


def yt(x):
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4) + 0.5 * (t_te / chord) * x


def run(r, npts, mode):
    gmsh.initialize(["-nopopup"])
    gmsh.option.setNumber("General.Terminal", 0)
    occ = gmsh.model.occ
    xs = [0.5 * (1 - math.cos(math.pi * i / npts)) for i in range(npts + 1)]
    lower = [occ.addPoint(chord * x, -chord * yt(x), 0) for x in reversed(xs[1:])]
    le = occ.addPoint(0, 0, 0)
    upper = [occ.addPoint(chord * x, chord * yt(x), 0) for x in xs[1:]]
    pts = lower + [le] + upper
    spl = occ.addSpline(pts)
    cte = occ.addPoint(chord, 0, 0)
    cir = occ.addCircleArc(pts[-1], cte, pts[0])
    face = occ.addPlaneSurface([occ.addCurveLoop([spl, cir])])
    ext = occ.extrude([(2, face)], 0, 0, half)
    occ.synchronize()
    vol = [e[1] for e in ext if e[0] == 3][0]
    cap = ext[0][1]
    cc = [abs(c[1]) for c in gmsh.model.getBoundary([(2, cap)], combined=False, oriented=False)]
    if mode == "bspline":
        pass
    try:
        out = occ.fillet([vol], cc, [r], removeVolume=False)
        print(r, npts, "OK", out[:3])
    except Exception as e:
        print(r, npts, "FAIL", str(e)[:60])
    gmsh.finalize()


for r in (4e-6, 8e-6, 1e-5, 2e-5, 3e-5, 1e-4, 1.5e-4, 2e-4, 2.4e-4):
    run(r, 120, "")
