using System.Globalization;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// ANA-04 lattice rows (design §13.2, §18.8). The elliptic numbers are the recorded oracle in
/// docs/notes/area3-fixture-arithmetic.md: cosine span, four uniform chordwise panels, wake 20 spans.
/// The product default remains 64 × 4 cosine (DR-ANA-7). F-2, F-5 and F-15 read the solves F-6 builds.
/// </summary>
internal static class LatticeFixtureTests
{
    private const double Rho = 1000;
    private const double Speed = 1;
    private const double Q = 0.5 * Rho * Speed * Speed;
    private const double EllipticHalf = 1;
    private const double EllipticC0 = 1 / Math.PI;
    private const double EllipticS = 0.5;
    private const double EllipticAr = 8;
    private static readonly int[] Lattices = [32, 64, 128];
    private static Trio? shared;

    internal static void Run()
    {
        Check("F6_ObservedOrder", F6);
        Check("F2_EllipticAR8_RichardsonClInRecordedBand", F2);
        Check("F5_InducedDrag_TrefftzWithin1PercentOfNearField", F5);
        Check("F15_EllipticWing_InducedAngleUniform", F15);
        Check("F1_FlatPlate_RichardsonClAlphaTo2Pi", F1);
        Check("F3_SymmetricSection_ZeroLiftOddInAlpha", F3);
        Check("F4_MirroredWing_NoSideForceRollYaw", F4);
        Check("Vlm_PivotingSolve_ResidualAfterOneSolve", PivotingSolve);
        Check("F7_LinearWashout_TipAlphaEffBelowRoot", F7);
        Check("F18_Camber4_DefaultLatticeTipConverges", F18);
        Check("F19_Washin1_DefaultLatticeTipConverges", F19);
        Check("F20_EllipticStraightQuarterChord_SweepZero", F20);
        Check("F21_ParabolicCamber_ZeroLiftAngleThinAirfoil", F21);
        Check("F16_BertinSmithSwept_ClAlpha3p443", F16);
        Check("Vlm_ClosingTip_FiniteAndListed", ClosingTip);
        Check("Vlm_NonFinite_RecordsFailedNotZero", NonFinite);
        Check("Vlm_AlphaBeyondEnvelope_ShowsEnvelopeFinding", Envelope);
    }

    private static void F6()
    {
        RunSettings defaults = Settings.Default;
        Equal(64, defaults.NSpanPerHalf, "span");
        Equal(4, defaults.NChord, "chord");
        Equal("cosine", defaults.SpanSpacing, "span spacing");
        Equal("cosine", defaults.ChordSpacing, "chord spacing");
        Equal(20, defaults.WakeSpans, "wake");
        Equal("+x", defaults.WakeDirection, "wake direction");
        Equal(1e-8, defaults.SingularityCutoff, "cutoff");
        Equal(0.01, Settings.ReconciliationTolerance, "reconciliation");
        Equal(2048, Settings.UnknownCap, "cap");
        Equal("cfdw.vlm-strip", MethodRecord.VlmStrip.Method.Id, "method");
        Equal("1.1.0", MethodRecord.VlmStrip.Method.Version, "version");
        Equal(1, MethodRecord.VlmStrip.Method.Order, "order");
        Equal(10, MethodRecord.VlmStrip.Envelope.AlphaEffFromZeroLiftMaxDeg, "envelope alpha");
        Equal(1.0, MethodRecord.VlmStrip.Envelope.ClLocalMax, "envelope cl");
        Equal(30, MethodRecord.VlmStrip.Envelope.QuarterChordSweepMaxDeg, "envelope sweep");
        try
        {
            VortexLattice.Solve(Rectangle(1, 1, 4), Settings.Default with { NSpanPerHalf = 600, NChord = 2 }, At(1), Rho, default);
            throw new InvalidOperationException("a 2400-unknown lattice was accepted");
        }
        catch (ArgumentOutOfRangeException) { }

        shared = Trio.Build(LatticePlant.None);
        Console.WriteLine("MEASURE F6 CL=" + string.Join("/", shared.Cl.Select(Num))
            + " e=" + string.Join("/", shared.E.Select(Num)) + " pCL=" + Num(shared.OrderCl)
            + " pE=" + Num(shared.OrderE));
        InRange(shared.OrderCl, 0.8, 1.2, "p(CL)");
        InRange(shared.OrderE, 0.8, 1.2, "p(e)");
        Trio mutant = Trio.Build(LatticePlant.WakePerPanel);
        if (Math.Abs(mutant.OrderCl - 1) <= 0.2 && Math.Abs(mutant.OrderE - 1) <= 0.2)
            throw new InvalidOperationException("F-6 wake-per-panel mutant stayed inside 1 ± 0.2: p(CL) "
                + Num(mutant.OrderCl) + " p(e) " + Num(mutant.OrderE));
    }

    private static void F2()
    {
        Trio wing = Shared();
        // BC-3: a miss stops the track. The band is not widened.
        if (wing.RichCl < 0.4156 || wing.RichCl > 0.4198)
            throw new InvalidOperationException("BC-3 F-2 Richardson CL " + Num(wing.RichCl)
                + " is outside [0.4156, 0.4198]. CL " + Num(wing.Cl[0]) + " " + Num(wing.Cl[1]) + " " + Num(wing.Cl[2])
                + " p " + Num(wing.OrderCl) + ". Stopping for an operator ruling.");
        InRange(wing.RichE, 0.995, 1.005, "Richardson e");
        Trio mutant = Trio.Build(LatticePlant.ControlAtMidPanel);
        if (mutant.RichCl >= 0.4156 && mutant.RichCl <= 0.4198)
            throw new InvalidOperationException("F-2 mid-panel mutant stayed inside the band: " + Num(mutant.RichCl));
    }

    // F-5 (review 2026-10-04): the near field is checked for convergence, not for a band hit at one lattice. On the
    // default chord law (cosine, design DR-ANA-7) the gap to Trefftz must shrink 32 → 64 → 128 and sit inside the
    // reconciliation tolerance at 128 or after Richardson. One midpoint evaluation per bound segment (design §5.2).
    private static void F5()
    {
        var wings = new LatticeSolution[3];
        Parallel.Invoke(
            () => wings[0] = Elliptic(Lattices[0], LatticePlant.None, "cosine"),
            () => wings[1] = Elliptic(Lattices[1], LatticePlant.None, "cosine"),
            () => wings[2] = Elliptic(Lattices[2], LatticePlant.None, "cosine"));
        double[] ratio = wings.Select(NearOverTrefftz).ToArray();
        string seen = Num(ratio[0]) + " " + Num(ratio[1]) + " " + Num(ratio[2]);
        if (!(Math.Abs(ratio[0] - 1) > Math.Abs(ratio[1] - 1) && Math.Abs(ratio[1] - 1) > Math.Abs(ratio[2] - 1)))
            throw new InvalidOperationException("near-field/Trefftz gap does not shrink 32 → 64 → 128: " + seen);
        double rich = Richardson(ratio[1], ratio[2], Order(ratio[0], ratio[1], ratio[2]));
        if (Math.Abs(ratio[2] - 1) > Settings.ReconciliationTolerance && Math.Abs(rich - 1) > Settings.ReconciliationTolerance)
            throw new InvalidOperationException("near-field/Trefftz " + seen + ", Richardson " + Num(rich) + ": outside 1 %");
        double legless = NearOverTrefftz(Elliptic(Lattices[0], LatticePlant.NearFieldTrailingOmitted, "cosine"));
        if (Math.Abs(legless - 1) <= Settings.ReconciliationTolerance)
            throw new InvalidOperationException("near field without the trailing legs stayed within 1 % of Trefftz: " + Num(legless));
    }

    private static double NearOverTrefftz(LatticeSolution wing) => Trefftz.WindAxes(wing.Forces, 5).Drag / Drag(wing);

    // F-15 (review 2026-10-04): pointwise α_i / (CL/(π AR)) at η 0, 0.5, 0.8, 0.9. The lattice is a lifting surface, so
    // α_i is not uniform to 1 % (lifting-line theory); it converges at order 1 to a measured profile. The reference is
    // the independent lattice in docs/notes/area3-fixture-arithmetic.md (F-15 block), w_T at strip y-midpoints.
    private static readonly double[] Stations = [0, 0.5, 0.8, 0.9];
    private static readonly double[] Reference32 = [1.01556, 1.00082, 0.93735, 0.83744];
    private static readonly double[] ReferenceRichardson = [1.02743, 1.01678, 0.96735, 0.88407];

    private static void F15()
    {
        Trio trio = Shared();
        double[][] profile = trio.Solved.Select((wing, i) => Profile(wing, trio.Cl[i])).ToArray();
        for (int k = 0; k < Stations.Length; k++)
        {
            double p = Order(profile[0][k], profile[1][k], profile[2][k]);
            double rich = Richardson(profile[1][k], profile[2][k], p);
            string at = "η " + Num(Stations[k]) + ": " + Num(profile[0][k]) + " " + Num(profile[1][k]) + " " + Num(profile[2][k]);
            InRange(p, 0.8, 1.2, "α_i order at " + at + ", p");
            if (Math.Abs(rich / ReferenceRichardson[k] - 1) > 0.005)
                throw new InvalidOperationException("Richardson α_i ratio " + Num(rich) + " vs reference " + Num(ReferenceRichardson[k]) + " at " + at);
        }
        Near(profile[0], "32 per half");
        LatticeSolution wing64 = trio.Solved[1];
        double largest = wing64.Strips.Max(strip => Math.Abs(strip.InducedAngleDeg));
        for (int s = 0; s < wing64.Strips.Count; s++)
        {
            double mirror = wing64.Strips[wing64.Strips.Count - 1 - s].InducedAngleDeg;
            if (Math.Abs(wing64.Strips[s].InducedAngleDeg - mirror) > 1e-10 * largest)
                throw new InvalidOperationException("α_i not even in y at strip " + s + ": " + Num(wing64.Strips[s].InducedAngleDeg) + " vs " + Num(mirror));
        }
        foreach (LatticePlant plant in new[] { LatticePlant.InducedFromControlPoint, LatticePlant.DownwashNeighbour })
        {
            LatticeSolution wrong = Elliptic(Lattices[0], plant);
            try { Near(Profile(wrong, Coefficient(wrong, EllipticS)), plant.ToString()); }
            catch (InvalidOperationException) { continue; }
            throw new InvalidOperationException(plant + " mutant stayed within 0.5 % of the 32-span reference profile");
        }
    }

    private static void Near(double[] profile, string what)
    {
        for (int k = 0; k < Stations.Length; k++)
            if (Math.Abs(profile[k] / Reference32[k] - 1) > 0.005)
                throw new InvalidOperationException(what + ": α_i ratio " + Num(profile[k]) + " vs reference " + Num(Reference32[k]) + " at η " + Num(Stations[k]));
    }

    // α_i / (CL/(π AR)) at each station, linear between strip centres.
    private static double[] Profile(LatticeSolution wing, double cl)
    {
        double ideal = cl / (Math.PI * EllipticAr) * (180 / Math.PI);
        var result = new double[Stations.Length];
        for (int k = 0; k < Stations.Length; k++)
        {
            LatticeStrip inboard = wing.Strips.Where(strip => strip.Eta <= Stations[k]).MaxBy(strip => strip.Eta)!;
            LatticeStrip outboard = wing.Strips.Where(strip => strip.Eta > Stations[k]).MinBy(strip => strip.Eta)!;
            double t = (Stations[k] - inboard.Eta) / (outboard.Eta - inboard.Eta);
            result[k] = (inboard.InducedAngleDeg + t * (outboard.InducedAngleDeg - inboard.InducedAngleDeg)) / ideal;
        }
        return result;
    }

    private static void F1()
    {
        double[] ar = [50, 100, 200];
        var cla = new double[3];
        for (int i = 0; i < 3; i++) cla[i] = FlatPlateSlope(ar[i], 40);
        double p = Order(cla[0], cla[1], cla[2]);
        double rich = Richardson(cla[1], cla[2], p);
        double twoPi = 2 * Math.PI;
        if (Math.Abs(rich - twoPi) / twoPi > 0.005)
            throw new InvalidOperationException("Richardson CLα " + Num(rich) + " vs 2π " + Num(twoPi)
                + " from " + Num(cla[0]) + " " + Num(cla[1]) + " " + Num(cla[2]) + " p " + Num(p));
        double mid = FlatPlateSlope(80, 40, LatticePlant.BoundAtMidChord);
        if (Math.Abs(mid - twoPi) / twoPi <= 0.005)
            throw new InvalidOperationException("mid-chord bound vortex still matched 2π: " + Num(mid));
    }

    private static void F3()
    {
        const double half = 4, chord = 1;
        double zero = LiftCoefficient(Rectangular(half, chord, 8, 1, 0, 0, null), 2 * half * chord);
        double up = LiftCoefficient(Rectangular(half, chord, 8, 1, 4, 0, null), 2 * half * chord);
        double down = LiftCoefficient(Rectangular(half, chord, 8, 1, -4, 0, null), 2 * half * chord);
        if (Math.Abs(zero) > 1e-6) throw new InvalidOperationException("Cl(0) " + Num(zero));
        if (Math.Abs(up + down) / Math.Abs(up) > 1e-12)
            throw new InvalidOperationException("oddness " + Num(up) + " " + Num(down));
        if (!(up > 0.3)) throw new InvalidOperationException("Cl(4°) " + Num(up));
        double camber = LiftCoefficient(Rectangular(half, chord, 8, 1, 0, 0, UpperSurface), 2 * half * chord);
        if (Math.Abs(camber) <= 1e-6)
            throw new InvalidOperationException("upper-surface camber mutant stayed at Cl(0) " + Num(camber));
    }

    private static void F4()
    {
        const double half = 3, chord = 1;
        LatticeSolution wing = Rectangular(half, chord, 8, 2, 5, 0, TentCamber);
        LatticeSolution oldWing = Rectangular(half, chord, 8, 2, 5, 0, TentCamber, LatticePlant.CamberSurfaceHorseshoe);
        double area = 2 * half * chord, ar = 2 * half / chord;
        double oldCl = Coefficient(oldWing, area), newCl = Coefficient(wing, area);
        Console.WriteLine("MEASURE F4 old/new CL=" + Num(oldCl) + "/" + Num(newCl)
            + " e=" + Num(Oswald(oldWing, oldCl, area, ar)) + "/" + Num(Oswald(wing, newCl, area, ar)));
        double lift = Trefftz.WindAxes(wing.Forces, 5).Lift;
        double qS = Q * 2 * half * chord;
        if (!(lift / qS > 0.1)) throw new InvalidOperationException("CL " + Num(lift / qS));
        AssertSymmetry(wing, lift, 2 * half);
        LatticeSolution flipped = Rectangular(half, chord, 8, 2, 5, 0, (y, f, c) => (y < 0 ? -1 : 1) * TentCamber(y, f, c));
        if (Symmetric(flipped, Trefftz.WindAxes(flipped.Forces, 5).Lift, 2 * half))
            throw new InvalidOperationException("wrong-sign mirror stayed symmetric");
    }

    // F-4 defect (review 2026-10-04): the factor swapped whole rows, stored multipliers included, while the forward
    // substitution applies each interchange in step order. That pairing is right only for a trailing-column swap.
    private static void PivotingSolve()
    {
        const int n = 12;
        var a = new double[n * n];
        var b = new double[n];
        // A fixed linear congruential sequence (glibc constants) in [-1, 1): full rank, platform independent.
        uint state = 2026;
        double Next() { state = state * 1103515245 + 12345; return (state >> 1) / (double)(1u << 30) - 1; }
        for (int i = 0; i < n * n; i++) a[i] = Next();
        for (int i = 0; i < n; i++) b[i] = Next();
        VortexLattice.DenseSolution solved = VortexLattice.SolveDense(a, b, n, LatticePlant.None, default);
        if (solved.Interchanges < 2) throw new InvalidOperationException("the matrix pivots " + solved.Interchanges + " times");
        if (!(solved.ResidualInf <= 1e-12))
            throw new InvalidOperationException("residual after one solve " + Num(solved.ResidualInf));
        // κ₁ = ‖A‖₁ ‖A⁻¹‖₁ exactly, column by column; the ones-vector estimate is a lower bound of it.
        double inverse = 0;
        for (int j = 0; j < n; j++)
        {
            var e = new double[n];
            e[j] = 1;
            inverse = Math.Max(inverse, VortexLattice.SolveDense(a, e, n, LatticePlant.None, default).X.Sum(Math.Abs));
        }
        double norm = 0;
        for (int j = 0; j < n; j++)
        {
            double column = 0;
            for (int i = 0; i < n; i++) column += Math.Abs(a[i * n + j]);
            norm = Math.Max(norm, column);
        }
        double exact = norm * inverse;
        if (!(solved.Kappa1 <= exact * (1 + 1e-9) && solved.Kappa1 >= 1))
            throw new InvalidOperationException("κ₁ estimate " + Num(solved.Kappa1) + " vs exact " + Num(exact));
        try
        {
            VortexLattice.SolveDense(a, b, n, LatticePlant.PivotWholeRow, default);
            throw new InvalidOperationException("whole-row swap solved without a residual failure");
        }
        catch (LatticeFailedException failure)
        {
            Equal("ANA-SOLVE-RESIDUAL", failure.Code, "whole-row swap");
        }
    }

    private static void F7()
    {
        const double half = 3, chord = 1, alpha = 4;
        LatticeSolution plain = Rectangular(half, chord, 12, 2, alpha, 0, null);
        LatticeSolution washed = Rectangular(half, chord, 12, 2, alpha, -3, null);
        LatticeSolution oldWashed = Rectangular(half, chord, 12, 2, alpha, -3, null, LatticePlant.CamberSurfaceHorseshoe);
        double area = 2 * half * chord, ar = 2 * half / chord;
        double oldCl = Coefficient(oldWashed, area), newCl = Coefficient(washed, area);
        Console.WriteLine("MEASURE F7 old/new CL=" + Num(oldCl) + "/" + Num(newCl)
            + " e=" + Num(Oswald(oldWashed, oldCl, area, ar)) + "/" + Num(Oswald(washed, newCl, area, ar)));
        double plainCl = Coefficient(plain, 2 * half * chord);
        double washedCl = Coefficient(washed, 2 * half * chord);
        LatticeStrip root = Nearest(washed, 0);
        LatticeStrip tip = Nearest(washed, 0.9 * half);
        if (!(Outermost(washed).TwistDeg < -2.9))
            throw new InvalidOperationException("outer twist " + Num(Outermost(washed).TwistDeg));
        double rootEff = alpha + root.TwistDeg - root.InducedAngleDeg;
        double tipEff = alpha + tip.TwistDeg - tip.InducedAngleDeg;
        if (!(tip.TwistDeg < -2.5))
            throw new InvalidOperationException("90% twist " + Num(tip.TwistDeg));
        if (!(tipEff < rootEff))
            throw new InvalidOperationException("tip α_eff " + Num(tipEff) + " not below root " + Num(rootEff));
        if (!(washedCl < plainCl))
            throw new InvalidOperationException("washed CL " + Num(washedCl) + " not below " + Num(plainCl));
        LatticeSolution flipped = Rectangular(half, chord, 12, 2, alpha, 3, null);
        LatticeStrip flippedTip = Nearest(flipped, 0.9 * half);
        LatticeStrip flippedRoot = Nearest(flipped, 0);
        double flippedTipEff = alpha + flippedTip.TwistDeg - flippedTip.InducedAngleDeg;
        double flippedRootEff = alpha + flippedRoot.TwistDeg - flippedRoot.InducedAngleDeg;
        if (!(flippedTipEff > flippedRootEff))
            throw new InvalidOperationException("flipped twist tip " + Num(flippedTipEff) + " root " + Num(flippedRootEff));
    }

    internal static void RunReadiness()
    {
        Check("Readiness_Camber4_N256Point", ReadinessCamber256);
        Check("Readiness_Washin1_N256Solves", ReadinessWashin256);
        Check("Readiness_EllipticQuarterChord_SweepZeroFine", ReadinessSweepFine);
    }

    private static void F18() => FastNonplanar("camber", ParabolicCamber, 0);

    private static void F19() => FastNonplanar("washin", null, 1);

    // Four cosine chord panels must read the tangent at each 3/4-panel control point.
    // AR 40 leaves a finite-span correction; 0.25° allows 5.5% of the 2D angle while
    // excluding the 1.20° secant-normal defect measured by the independent 2D probe.
    private static void F21()
    {
        const double half = 20, chord = 1, area = 40;
        var sections = Nodes(-half, half, 16, "cosine")
            .Select(y => Section(y, -chord / 4, chord, 0, 0, y / half, 2 * half, ParabolicCamber, 20)).ToList();
        RunSettings settings = Lattice(16, 4, "cosine");
        double cl0 = Coefficient(VortexLattice.Solve(sections, settings, At(0), Rho, default), area);
        double cl5 = Coefficient(VortexLattice.Solve(sections, settings, At(5), Rho, default), area);
        // The linear no-penetration system makes circulation A cos(alpha) + B sin(alpha).
        // Recover its zero from two solves without a small-angle extrapolation.
        double alpha5 = Math.PI * 5 / 180;
        double alphaL0 = -Math.Atan2(cl0 * Math.Sin(alpha5), cl5 - cl0 * Math.Cos(alpha5)) * 180 / Math.PI;
        Console.WriteLine("MEASURE F21 AR=40 nc=4 alphaL0=" + Num(alphaL0) + " CL0=" + Num(cl0) + " CL5=" + Num(cl5));
        const double thinAirfoil = -0.08 * 180 / Math.PI;
        if (!(Math.Abs(alphaL0 - thinAirfoil) <= 0.25))
            throw new InvalidOperationException("alpha_L0 " + Num(alphaL0) + " vs thin-airfoil " + Num(thinAirfoil) + " ±0.25°");
    }

    // Ruling 77 (2), fast half. n64 against n128: tip α_i within 0.1° and CL within 1% of n128.
    // κ₁ at n64 stays within 10× the flat plate. The camber-surface horseshoe mutant must leave that α_i band.
    // A returned solve has already passed SolveDense's 1e-10 backward-error gate. n256 is readiness.
    private static void FastNonplanar(string label, Func<double, double, double, double>? camber, double tipTwist)
    {
        const double area = 0.5;
        // The four lattices share no data. Wall time is the n=128 solve (F-5 and F-6 use the same split).
        var wings = new LatticeSolution[4];
        Parallel.Invoke(
            () => wings[0] = SolveNonplanar(label, 64, camber, tipTwist),
            () => wings[1] = SolveNonplanar(label, 128, camber, tipTwist),
            () => wings[2] = SolveNonplanar("flat", 64, null, 0),
            () => wings[3] = StudyRectangle(64, camber, tipTwist, LatticePlant.CamberSurfaceHorseshoe));
        LatticeSolution wing64 = wings[0], wing128 = wings[1], flat = wings[2], mutant = wings[3];
        double ai64 = Outermost(wing64).InducedAngleDeg;
        double ai128 = Outermost(wing128).InducedAngleDeg;
        if (!(Math.Abs(ai64 - ai128) <= 0.1))
            throw new InvalidOperationException(label + " tip α_i n64/n128 " + Num(ai64) + "/" + Num(ai128));
        double cl64 = Coefficient(wing64, area), cl128 = Coefficient(wing128, area);
        if (!(Math.Abs(cl64 - cl128) / Math.Abs(cl128) <= 0.01))
            throw new InvalidOperationException(label + " CL n64/n128 " + Num(cl64) + "/" + Num(cl128));
        if (!(wing64.Diagnostics.Kappa1 <= 10 * flat.Diagnostics.Kappa1))
            throw new InvalidOperationException(label + " κ₁ " + Num(wing64.Diagnostics.Kappa1)
                + " exceeds 10× flat " + Num(flat.Diagnostics.Kappa1));
        double mutantTip = Outermost(mutant).InducedAngleDeg;
        if (Math.Abs(mutantTip - ai128) <= 0.1)
            throw new InvalidOperationException(label + " camber-surface horseshoe mutant stayed within 0.1°: " + Num(mutantTip));
        Console.WriteLine("MUTANT " + label + " camber-surface n64 tipAi=" + Num(mutantTip) + " RED");
    }

    private static void ReadinessCamber256() => SolveNonplanar("camber", 256, ParabolicCamber, 0);

    private static void ReadinessWashin256()
    {
        try { SolveNonplanar("washin", 256, null, 1); }
        catch (LatticeFailedException failure) when (failure.Code == "ANA-SOLVE-SINGULAR")
        {
            throw new InvalidOperationException("washin n=256 raised ANA-SOLVE-SINGULAR: " + failure.Message);
        }
    }

    private static LatticeSolution SolveNonplanar(string label, int n,
        Func<double, double, double, double>? camber, double tipTwist)
    {
        LatticeSolution wing = StudyRectangle(n, camber, tipTwist);
        Console.WriteLine("MEASURE " + label + " n=" + n + " tipAi=" + Num(Outermost(wing).InducedAngleDeg)
            + " CL=" + Num(Coefficient(wing, 0.5)) + " kappa1=" + Num(wing.Diagnostics.Kappa1)
            + " residualInf=" + Num(wing.Diagnostics.ResidualInf));
        if (!double.IsFinite(wing.Diagnostics.ResidualInf))
            throw new InvalidOperationException(label + " n=" + n + " residual is not finite");
        return wing;
    }

    private static void F20() => StraightQuarterChord([16, 32, 64], plantMutant: true);

    private static void ReadinessSweepFine() => StraightQuarterChord([128, 256], plantMutant: false);

    // Ruling 77 (3). Fast owns n16/32/64 and the front-bound sweep mutant. Readiness owns n128/256.
    private static void StraightQuarterChord(int[] counts, bool plantMutant)
    {
        foreach (int n in counts)
        {
            LatticeSolution wing = Elliptic(n, LatticePlant.None, "cosine");
            double maximum = wing.Strips.Max(strip => Math.Abs(strip.SweepDeg));
            Console.WriteLine("MEASURE straight-quarter-chord n=" + n + " maxSweep=" + Num(maximum));
            if (!(maximum <= 1e-9))
                throw new InvalidOperationException("quarter-chord sweep at n=" + n + " is " + Num(maximum));
            if (!plantMutant) continue;
            LatticeSolution mutant = Elliptic(n, LatticePlant.FrontBoundSweep, "cosine");
            double wrong = mutant.Strips.Max(strip => Math.Abs(strip.SweepDeg));
            if (!(wrong > 1e-9))
                throw new InvalidOperationException("front-bound sweep mutant at n=" + n + " stayed at zero");
            Console.WriteLine("MUTANT front-bound sweep n=" + n + " max=" + Num(wrong) + " RED");
        }
    }

    private static double ParabolicCamber(double y, double f, double chord) => 0.16 * f * (1 - f) * chord;

    private static LatticeSolution StudyRectangle(int n, Func<double, double, double, double>? camber, double tipTwist,
        LatticePlant plant = LatticePlant.None)
    {
        const double half = 1, chord = 0.25;
        var sections = new List<SectionSample>();
        foreach (double y in Nodes(-half, half, n, "cosine"))
            sections.Add(Section(y, -chord / 4, chord, 0, tipTwist * Math.Abs(y) / half,
                y / half, 2 * half, camber, camber is null ? 4 : 20));
        return VortexLattice.Solve(sections, Lattice(n, 4, "cosine"), At(5), Rho, plant, default);
    }

    private static void F16()
    {
        double slope = SweptSlope(LatticePlant.None);
        double target = 3.443;
        if (Math.Abs(slope - target) / target > 0.005)
            throw new InvalidOperationException("CLα " + Num(slope) + " outside 0.5 % of 3.443");
        double ignored = SweptSlope(LatticePlant.BoundUnswept);
        if (Math.Abs(ignored - target) / target <= 0.005)
            throw new InvalidOperationException("unswept bound vortex stayed inside the band: " + Num(ignored));
    }

    private static void ClosingTip()
    {
        LatticeSolution wing = Elliptic(16, LatticePlant.None);
        if (!(wing.MinimumPanelArea > 1e-8))
            throw new InvalidOperationException("tip panel area " + Num(wing.MinimumPanelArea));
        foreach (double gamma in wing.Gamma)
            if (!double.IsFinite(gamma)) throw new InvalidOperationException("non-finite tip circulation");
        LatticeSolution listed = MicroTip();
        if (listed.Exclusions.Count == 0 || !listed.Exclusions[0].Contains("10 µm", StringComparison.Ordinal))
            throw new InvalidOperationException("exclusion not listed");
        foreach (double gamma in listed.Gamma)
            if (!double.IsFinite(gamma)) throw new InvalidOperationException("excluded wing did not stay finite");
        try
        {
            Elliptic(12, LatticePlant.NormalFromLeadingEdge);
            throw new InvalidOperationException("leading-edge normal at the closing tip stayed finite");
        }
        catch (LatticeFailedException failure)
        {
            Equal("ANA-SOLVE-SINGULAR", failure.Code, "tip mutant");
        }
    }

    private static void NonFinite()
    {
        var broken = Rectangle(1, 1, 4);
        SectionSample first = broken[0];
        broken[0] = new SectionSample(first.Frame, first.X, first.Camber, first.Thickness, first.CamberSlope,
            new[] { new Point3(double.NaN, first.PlacedCamber[0].Y, 0), first.PlacedCamber[1] });
        try
        {
            LatticeSolution zeros = VortexLattice.Solve(broken, Lattice(4, 1), At(3), Rho, default);
            if (zeros.Gamma.All(g => g == 0))
                throw new InvalidOperationException("non-finite circulation was replaced by zero");
            throw new InvalidOperationException("non-finite geometry returned a solution");
        }
        catch (LatticeFailedException failure)
        {
            Equal("ANA-NONFINITE", failure.Code, "non-finite");
        }
    }

    private static void Envelope()
    {
        LatticeSolution wing = Rectangular(3, 1, 8, 2, 1, 20, null);
        IReadOnlyList<StripVerdict> verdicts = MethodRecord.Verdicts(wing, 1, 0);
        bool split = false;
        for (int i = 0; i < wing.Strips.Count; i++)
        {
            LatticeStrip strip = wing.Strips[i];
            StripVerdict byGeo = MethodRecord.JudgeStrip(1, 0, strip.ClLocal, strip.SweepDeg);
            if (!verdicts[i].Inside && byGeo.Inside && verdicts[i].Exceeded.Contains("|α_eff − α_L0|"))
                split = true;
        }
        if (!split)
        {
            var sample = wing.Strips.Select((strip, i) => Num(strip.Y) + " eff " + Num(1 + strip.TwistDeg - strip.InducedAngleDeg)
                + " cl " + Num(strip.ClLocal) + " " + verdicts[i].Text);
            throw new InvalidOperationException("no strip is outside on α_eff and inside on α_geo: " + string.Join("; ", sample));
        }
        if (!MethodRecord.JudgeRun(verdicts).StartsWith("Outside the method envelope", StringComparison.Ordinal))
            throw new InvalidOperationException(MethodRecord.JudgeRun(verdicts));
    }

    private sealed class Trio
    {
        public required LatticeSolution[] Solved { get; init; }
        public required double[] Cl { get; init; }
        public required double[] E { get; init; }
        public double OrderCl { get; init; }
        public double OrderE { get; init; }
        public double RichCl { get; init; }
        public double RichE { get; init; }

        public static Trio Build(LatticePlant plant)
        {
            var solved = new LatticeSolution[3];
            Parallel.Invoke(
                () => solved[0] = Elliptic(Lattices[0], plant),
                () => solved[1] = Elliptic(Lattices[1], plant),
                () => solved[2] = Elliptic(Lattices[2], plant));
            var cl = new double[3];
            var e = new double[3];
            for (int i = 0; i < 3; i++)
            {
                cl[i] = Coefficient(solved[i], EllipticS);
                e[i] = Oswald(solved[i], cl[i], EllipticS, EllipticAr);
            }
            double pCl = Order(cl[0], cl[1], cl[2]);
            double pE = Order(e[0], e[1], e[2]);
            return new Trio
            {
                Solved = solved, Cl = cl, E = e, OrderCl = pCl, OrderE = pE,
                RichCl = Richardson(cl[1], cl[2], pCl), RichE = Richardson(e[1], e[2], pE)
            };
        }
    }

    private static Trio Shared() => shared ?? throw new InvalidOperationException("F-6 did not build the shared solves");

    private static LatticeSolution Elliptic(int nPerHalf, LatticePlant plant, string chordSpacing = "uniform")
    {
        double[] nodes = Nodes(-EllipticHalf, EllipticHalf, nPerHalf, "cosine");
        var sections = new List<SectionSample>(nodes.Length);
        foreach (double y in nodes)
        {
            double chord = EllipticC0 * Math.Sqrt(Math.Max(0, 1 - (y / EllipticHalf) * (y / EllipticHalf)));
            sections.Add(Section(y, -chord / 4, chord, 0, 0, y / EllipticHalf, 2 * EllipticHalf, null));
        }
        return VortexLattice.Solve(sections, Lattice(nPerHalf, 4, chordSpacing), At(5), Rho, plant, default);
    }

    private static double FlatPlateSlope(double aspect, int nPerHalf, LatticePlant plant = LatticePlant.None)
    {
        double half = aspect / 2;
        LatticeSolution solved = Rectangular(half, 1, nPerHalf, 1, 1, 0, null, plant);
        double cl = Coefficient(solved, aspect);
        return cl / (Math.PI / 180);
    }

    private static double SweptSlope(LatticePlant plant)
    {
        const double chord = 1, half = 2.5;
        double[] nodes = Nodes(-half, half, 4, "uniform");
        var sections = new List<SectionSample>(nodes.Length);
        foreach (double y in nodes)
        {
            double xLe = Math.Abs(y) - 0.25 * chord;
            sections.Add(Section(y, xLe, chord, 0, 0, y / half, 2 * half, null));
        }
        LatticeSolution solved = VortexLattice.Solve(sections, Lattice(4, 1, "uniform", "uniform"), At(4), Rho, plant, default);
        return Coefficient(solved, 2 * half * chord) / (4 * Math.PI / 180);
    }

    private static LatticeSolution MicroTip()
    {
        double[] nodes = Nodes(-1, 1, 8, "cosine");
        var sections = new List<SectionSample>(nodes.Length);
        double yCut = nodes[^2];
        foreach (double y in nodes)
        {
            double chord = Math.Abs(y) >= yCut - 1e-12 ? 1e-6 : 0.2;
            sections.Add(Section(y, 0, chord, 0, 0, y, 2, null));
        }
        return VortexLattice.Solve(sections, Lattice(8, 2), At(3), Rho, default);
    }

    private static LatticeSolution Rectangular(double half, double chord, int nPerHalf, int nChord, double alpha, double tipTwist,
        Func<double, double, double, double>? camber, LatticePlant plant = LatticePlant.None)
    {
        double[] nodes = Nodes(-half, half, nPerHalf, "cosine");
        var sections = new List<SectionSample>(nodes.Length);
        foreach (double y in nodes)
        {
            double twist = tipTwist * Math.Abs(y) / half;
            sections.Add(Section(y, 0, chord, 0, twist, y / half, 2 * half, camber));
        }
        return VortexLattice.Solve(sections, Lattice(nPerHalf, nChord), At(alpha), Rho, plant, default);
    }

    private static List<SectionSample> Rectangle(double half, double chord, int nPerHalf) =>
        Nodes(-half, half, nPerHalf, "cosine")
            .Select(y => Section(y, 0, chord, 0, 0, y / half, 2 * half, null)).ToList();

    private static SectionSample Section(double y, double xLe, double chord, double z, double twistDeg, double eta, double span,
        Func<double, double, double, double>? camber, int camberIntervals = 4)
    {
        double[] fractions = camber is null ? [0, 1] : Enumerable.Range(0, camberIntervals + 1)
            .Select(i => i / (double)camberIntervals).ToArray();
        double rad = twistDeg * (Math.PI / 180);
        double cosine = Math.Cos(rad), sine = Math.Sin(rad);
        double pivot = xLe + 0.25 * chord;
        var placed = new Point3[fractions.Length];
        var xs = new double[fractions.Length];
        var cambers = new double[fractions.Length];
        var zeros = new double[fractions.Length];
        var slopes = new double[fractions.Length];
        for (int i = 0; i < fractions.Length; i++)
        {
            double f = fractions[i];
            double local = camber?.Invoke(y, f, chord) ?? 0;
            double dx = xLe + f * chord - pivot;
            placed[i] = new Point3(pivot + cosine * dx + sine * local, y, z - sine * dx + cosine * local);
            xs[i] = f;
            cambers[i] = chord == 0 ? 0 : local / chord;
            if (camber is not null && chord > 0)
            {
                double lo = Math.Max(0, f - 1e-6), hi = Math.Min(1, f + 1e-6);
                slopes[i] = (camber(y, hi, chord) - camber(y, lo, chord)) / ((hi - lo) * chord);
            }
        }
        var frame = new StationFrame(eta, span, xLe, xLe + chord, z, twistDeg, 0);
        return new SectionSample(frame, xs, cambers, zeros, slopes, placed);
    }

    private static double UpperSurface(double y, double f, double chord)
    {
        double t = 0.2969 * Math.Sqrt(f) - 0.1260 * f - 0.3516 * f * f + 0.2843 * f * f * f - 0.1015 * f * f * f * f;
        return 0.6 * t * chord;
    }

    private static double TentCamber(double y, double f, double chord) => 0.04 * chord * (1 - Math.Abs(2 * f - 1));

    private static double[] Nodes(double yMin, double yMax, int nPerHalf, string spacing)
    {
        double[] edges = VortexLattice.SpanStations(yMin, yMax, nPerHalf, spacing);
        var nodes = new double[edges.Length * 2 - 1];
        int k = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            nodes[k++] = edges[i];
            if (i + 1 < edges.Length) nodes[k++] = 0.5 * (edges[i] + edges[i + 1]);
        }
        return nodes;
    }

    private static RunSettings Lattice(int nPerHalf, int nChord, string chordSpacing = "uniform", string spanSpacing = "cosine") =>
        Settings.Default with { NSpanPerHalf = nPerHalf, NChord = nChord, ChordSpacing = chordSpacing, SpanSpacing = spanSpacing };

    private static OperatingPoint At(double alphaDeg) => new(Speed, 101325, null, "root LE", alphaDeg, null);

    private static double Coefficient(LatticeSolution solved, double area) =>
        Trefftz.Lift(Gammas(solved), Dys(solved), Rho, Speed) / (Q * area);

    private static double LiftCoefficient(LatticeSolution solved, double area) => Coefficient(solved, area);

    private static double Drag(LatticeSolution solved) =>
        Trefftz.InducedDrag(Gammas(solved), solved.DownwashTrefftz.ToArray(), Dys(solved), Rho);

    private static double Oswald(LatticeSolution solved, double cl, double area, double aspect)
    {
        double cdi = Drag(solved) / (Q * area);
        return Trefftz.Oswald(cl, aspect, cdi);
    }

    private static double[] Gammas(LatticeSolution solved) => solved.Gamma.ToArray();

    private static double[] Dys(LatticeSolution solved) => solved.Strips.Select(strip => strip.Dy).ToArray();

    private static void AssertSymmetry(LatticeSolution solved, double lift, double span)
    {
        if (!Symmetric(solved, lift, span))
        {
            double fy = 0, mx = 0, mz = 0;
            foreach (StripForce force in solved.Forces) { fy += force.Fy; mx += force.Mx; mz += force.Mz; }
            var rows = new List<string>();
            foreach (StripForce force in solved.Forces)
                rows.Add(Num(force.Y) + " G " + Num(force.Gamma) + " F " + Num(force.Fx) + "," + Num(force.Fy) + "," + Num(force.Fz));
            throw new InvalidOperationException("Fy/L " + Num(fy / lift) + " Mx " + Num(mx / (lift * span)) + " Mz " + Num(mz / (lift * span))
                + " | " + string.Join(" ; ", rows));
        }
    }

    private static bool Symmetric(LatticeSolution solved, double lift, double span)
    {
        double fy = 0, mx = 0, mz = 0;
        foreach (StripForce force in solved.Forces) { fy += force.Fy; mx += force.Mx; mz += force.Mz; }
        double scale = Math.Abs(lift);
        return Math.Abs(fy) <= 1e-12 * scale && Math.Abs(mx) <= 1e-12 * scale * span && Math.Abs(mz) <= 1e-12 * scale * span;
    }

    private static LatticeStrip Nearest(LatticeSolution solved, double y)
    {
        LatticeStrip best = solved.Strips[0];
        foreach (LatticeStrip strip in solved.Strips)
            if (Math.Abs(strip.Y - y) < Math.Abs(best.Y - y)) best = strip;
        return best;
    }

    private static LatticeStrip Outermost(LatticeSolution solved)
    {
        LatticeStrip best = solved.Strips[0];
        foreach (LatticeStrip strip in solved.Strips)
            if (Math.Abs(strip.Y) > Math.Abs(best.Y)) best = strip;
        return best;
    }

    private static double Order(double coarse, double mid, double fine) =>
        Math.Log((mid - coarse) / (fine - mid)) / Math.Log(2);

    private static double Richardson(double mid, double fine, double p) => fine + (fine - mid) / (Math.Pow(2, p) - 1);

    private static void InRange(double value, double lo, double hi, string what)
    {
        if (value < lo || value > hi)
            throw new InvalidOperationException(what + " " + Num(value) + " outside [" + lo + ", " + hi + "]");
    }

    private static string Num(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
