namespace CfdWorkbench.Analysis;

/// <summary>
/// Trefftz-plane lift and induced drag from the strip circulations and the trailing-wake downwash (design §5.2).
/// w_T = Σ G_k / (2π) [1/(y − y_b,k) − 1/(y − y_a,k)], evaluated at strip mid-span.
/// L_T = ρ V Σ Γ Δy. D_i,T = ½ ρ Σ Γ (−w_T) Δy. CDi is this drag; e = CL² / (π AR CDi).
/// </summary>
public static class Trefftz
{
    public static double Downwash(double y, ReadOnlySpan<double> gamma, ReadOnlySpan<double> yInboard, ReadOnlySpan<double> yOutboard)
    {
        double w = 0;
        for (int k = 0; k < gamma.Length; k++)
        {
            double ya = yInboard[k], yb = yOutboard[k];
            w += gamma[k] / (2 * Math.PI) * (1 / (y - yb) - 1 / (y - ya));
        }
        return w;
    }

    public static double Lift(ReadOnlySpan<double> gamma, ReadOnlySpan<double> dy, double rho, double speed)
    {
        double lift = 0;
        for (int i = 0; i < gamma.Length; i++) lift += gamma[i] * dy[i];
        return rho * speed * lift;
    }

    public static double InducedDrag(ReadOnlySpan<double> gamma, ReadOnlySpan<double> downwash, ReadOnlySpan<double> dy, double rho)
    {
        double drag = 0;
        for (int i = 0; i < gamma.Length; i++) drag += gamma[i] * -downwash[i] * dy[i];
        return 0.5 * rho * drag;
    }

    public static double Oswald(double liftCoefficient, double aspectRatio, double inducedDragCoefficient) =>
        liftCoefficient * liftCoefficient / (Math.PI * aspectRatio * inducedDragCoefficient);

    /// <summary>Wind axes from body-axis strip forces. Drag is along V∞, lift is the upward perpendicular in the x–z plane.</summary>
    public static (double Lift, double Drag) WindAxes(IReadOnlyList<StripForce> forces, double alphaDeg)
    {
        double a = VortexLattice.ToRadians(alphaDeg);
        double liftX = -Math.Sin(a), liftZ = Math.Cos(a), dragX = Math.Cos(a), dragZ = Math.Sin(a);
        double lift = 0, drag = 0;
        foreach (StripForce force in forces)
        {
            lift += force.Fx * liftX + force.Fz * liftZ;
            drag += force.Fx * dragX + force.Fz * dragZ;
        }
        return (lift, drag);
    }
}
