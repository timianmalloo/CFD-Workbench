using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The wing estimator's outputs at one operating point: CL, CDi and e (e is 1 by construction).</summary>
public sealed record EstimatorOutputs(double Cl, double Cdi, double E);

/// <summary>
/// The wing estimator tier <c>cfdw.estimator.wing</c> 1.0.0 (design a3d-compare.md §2.6; Ruling 250 Q1), labelled
/// <see cref="Labels.EstimatorNotValidated"/>. Closed form: the Helmbold lift slope with Diederich's sweep extension at
/// the mid-chord sweep (DATCOM 4.1.3.2; Raymer eq. 12.6 with beta = 1 and eta = 1), CL = CL_alpha (alpha - alpha_L0), and
/// CDi = CL^2 / (pi A) with elliptic loading assumed, so e = 1 by construction. Its behaviour on a swept hydrofoil is
/// Inferred: no swept fixture has been compared against it. A stored run keeps only its inputs; the outputs are derived
/// on read by <see cref="Derive"/> (DM7).
/// </summary>
public static class WingEstimator
{
    public const string TierId = "estimator";
    public const string Envelope = "estimator-envelope/1";

    /// <summary>The stored method identity. Order 0: a closed form has no observed convergence order.</summary>
    public static RunMethod Method { get; } = new("cfdw.estimator.wing", "1.0.0", 0);

    /// <summary>CL_alpha = 2 pi A / (2 + sqrt(A^2 (1 + tan^2 sweep) + 4)) per radian, sweep the mid-chord sweep in degrees.</summary>
    public static double LiftSlopePerRad(double aspectRatio, double midChordSweepDeg) =>
        LiftSlopeFromTan(aspectRatio, Math.Tan(VortexLattice.ToRadians(midChordSweepDeg)));

    private static double LiftSlopeFromTan(double aspectRatio, double tanSweep) =>
        2 * Math.PI * aspectRatio / (2 + Math.Sqrt(aspectRatio * aspectRatio * (1 + tanSweep * tanSweep) + 4));

    /// <summary><paramref name="alphaMinusAlphaL0Deg"/> is the wing angle above the chord-weighted mean zero-lift angle.</summary>
    public static EstimatorOutputs Outputs(double aspectRatio, double midChordSweepDeg, double alphaMinusAlphaL0Deg) =>
        FromTan(aspectRatio, Math.Tan(VortexLattice.ToRadians(midChordSweepDeg)), alphaMinusAlphaL0Deg);

    private static EstimatorOutputs FromTan(double aspectRatio, double tanSweep, double alphaMinusAlphaL0Deg)
    {
        double cl = LiftSlopeFromTan(aspectRatio, tanSweep) * VortexLattice.ToRadians(alphaMinusAlphaL0Deg);
        return new EstimatorOutputs(cl, cl * cl / (Math.PI * aspectRatio), 1.0);
    }

    /// <summary>
    /// The outputs of a stored estimator run, derived from its inputs and the accepted source it read. Throws a
    /// <see cref="ContractError"/> (<c>ANA-INPUT-ESTIMATOR</c>) when the run is not a Completed estimator run or the
    /// planform has no finite aspect ratio; a missing input is never a zero standing in for it.
    /// </summary>
    public static EstimatorOutputs Derive(AnalysisRun run, byte[] source, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(source);
        if (run.Tier != TierId || run.Outcome is not RunOutcome.Completed || run.Settings.SectionEtas is not { Count: > 0 } etas)
            throw new ContractError("ANA-INPUT-ESTIMATOR", "not a Completed estimator run with section stations");
        double aspect = WingEstimates.From(source, "accepted", 0).AspectRatio;
        if (!double.IsFinite(aspect) || aspect <= 0)
            throw new ContractError("ANA-INPUT-ESTIMATOR", "the accepted planform has no finite aspect ratio");
        double tanSweep = MidChordTan(source, cancellation);
        double alphaL0 = MeanZeroLiftDeg(source, etas, cancellation);
        return FromTan(aspect, tanSweep, run.Op.AlphaDeg - alphaL0);
    }

    // tan of the mid-chord sweep: the mid-chord line from the root station to the tip station, dx_mid / dy. Read from the
    // placed sections, the one placement authority (ADR-0010), so no second planform definition exists here.
    internal static double MidChordTan(byte[] source, CancellationToken cancellation)
    {
        var sections = Placement.Sections(source, [0, 1], [0, 1], cancellation);
        var root = sections.MinBy(section => section.Frame.Eta)!.Frame;
        var tip = sections.MaxBy(section => section.Frame.Eta)!.Frame;
        double dy = Math.Abs(tip.SpanMeters) - Math.Abs(root.SpanMeters);
        double dx = 0.5 * (tip.LeadingMeters + tip.TrailingMeters) - 0.5 * (root.LeadingMeters + root.TrailingMeters);
        if (!(dy > 0)) throw new ContractError("ANA-INPUT-ESTIMATOR", "the accepted planform has no span");
        return dx / dy;
    }

    // The chord-weighted mean of the section estimator's zero-lift angles at the run's stations (the call MethodRecord
    // makes: Re does not enter the zero-lift angle).
    internal static double MeanZeroLiftDeg(byte[] source, IReadOnlyList<double> etas, CancellationToken cancellation)
    {
        double weighted = 0, chords = 0;
        foreach (double eta in etas)
        {
            double chord = WingEstimates.ChordMeters(source, eta);
            weighted += chord * SectionEstimator.Estimate(source, eta, 0, 1e6, PanelMethod.DefaultPanelCount, cancellation).AlphaL0Deg;
            chords += chord;
        }
        return weighted / chords;
    }

    /// <summary>The settings of an estimator row: no lattice, the stations the run reads, the estimator envelope.</summary>
    internal static RunSettings Settings(RunSettings stations) => new(0, 0, "none", "none", 0, "none", 0, Envelope, null, [],
        stations.SurfaceState, stations.TeFloorMm, stations.SectionEtas, stations.SectionXs);
}
