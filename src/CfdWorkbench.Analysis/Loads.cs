using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Wing loads derived from the strip forces (design §5.2). Nothing here is stored on the run.</summary>
public static class Loads
{
    public const string TotalDragReason =
        "Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray";

    public const string AttachmentReason = "Unavailable — no attachment point named (DR-ANA-5)";

    public readonly record struct Vec(double X, double Y, double Z)
    {
        public static Vec operator +(Vec left, Vec right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        public static Vec Cross(Vec left, Vec right) => new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);
    }

    /// <summary>Dynamic pressure and the force pair of ANA-03. q = ½ ρ V², L = q S CL, D = q S CD.</summary>
    public static (double Q, double Lift, double Drag, double Ratio) Dynamic(double rho, double speed, double area, double liftCoefficient, double dragCoefficient)
    {
        double q = 0.5 * rho * speed * speed;
        double lift = q * area * liftCoefficient;
        double drag = q * area * dragCoefficient;
        return (q, lift, drag, liftCoefficient / dragCoefficient);
    }

    /// <summary>Moment about P. M_P = M_O + (O − P) × F.</summary>
    public static Vec About(Vec force, Vec momentAtOrigin, Vec point)
    {
        var originMinusPoint = new Vec(-point.X, -point.Y, -point.Z);
        return momentAtOrigin + Vec.Cross(originMinusPoint, force);
    }

    public static StripValue TotalDrag(IPolarSource polar)
    {
        if (polar.UnavailableReason is null)
            throw new ContractError("ANA-INPUT-OP", "A3a has no installed polar, so Total drag cannot name a profile value.");
        return new(null, TotalDragReason);
    }

    /// <summary>Fraction |D_near − D_Trefftz| / |D_Trefftz|. The near field is the wind-axis drag of the strip forces.</summary>
    public static double InducedDragGap(LatticeSolution solution, double alphaDeg, double rho)
    {
        (double _, double drag) = Trefftz.WindAxes(solution.Forces, alphaDeg);
        double trefftz = TrefftzDrag(solution, rho);
        return Math.Abs(drag - trefftz) / Math.Abs(trefftz);
    }

    public static double TrefftzLift(LatticeSolution solution, double rho, double speed) =>
        Trefftz.Lift(Column(solution, strip => strip.Gamma), Column(solution, strip => strip.Dy), rho, speed);

    public static double TrefftzDrag(LatticeSolution solution, double rho) =>
        Trefftz.InducedDrag(Column(solution, strip => strip.Gamma), Column(solution, strip => strip.Downwash), Column(solution, strip => strip.Dy), rho);

    /// <summary>Half-span root bending from the trailing wake, ρ V ∫₀^(b/2) Γ y dy.</summary>
    public static double RootBending(LatticeSolution solution, double rho, double speed)
    {
        double moment = 0;
        foreach (LatticeStrip strip in solution.Strips)
        {
            if (strip.Y < 0) continue;
            moment += strip.Gamma * strip.Y * strip.Dy;
        }
        return rho * speed * moment;
    }

    private static double[] Column(LatticeSolution solution, Func<LatticeStrip, double> select)
    {
        var values = new double[solution.Strips.Count];
        for (int i = 0; i < values.Length; i++) values[i] = select(solution.Strips[i]);
        return values;
    }
}
