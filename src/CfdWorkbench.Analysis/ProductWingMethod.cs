using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// The product wing method (design §6.1). It places nothing itself: the service samples <see cref="Settings"/> stations.
/// Those stations are the starboard half (η 0..1). This method mirrors them to port before the lattice, because the
/// lattice spans the Y range it is given. Forces use the water density through the lattice density overload; Γ and α_i
/// stay kinematic. A lattice failure becomes a contract error the service records as a Failed run with no diagnostics.
/// </summary>
public sealed class ProductWingMethod : IWingMethod
{
    private readonly LatticePlant plant;
    private readonly IPolarSource polar;

    public ProductWingMethod()
        : this(null, LatticePlant.None, UnavailablePolar.Instance)
    {
    }

    public ProductWingMethod(RunSettings settings)
        : this(settings, LatticePlant.None, UnavailablePolar.Instance)
    {
    }

    internal ProductWingMethod(RunSettings? settings, LatticePlant plant, IPolarSource? polar = null)
    {
        Settings = global::CfdWorkbench.Analysis.Settings.WithStations(settings ?? global::CfdWorkbench.Analysis.Settings.Default);
        this.plant = plant;
        this.polar = polar ?? UnavailablePolar.Instance;
    }

    public RunMethod Method => MethodRecord.VlmStrip.Method;

    public RunSettings Settings { get; }

    public double ReconciliationTolerance => global::CfdWorkbench.Analysis.Settings.ReconciliationTolerance;

    public RunReference Reference(byte[] source) => ReferenceQuantities.From(source);

    public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
    {
        try
        {
            return VortexLattice.Solve(Mirror(sections), Settings, op, water.Rho, plant, cancellation);
        }
        catch (LatticeFailedException failure)
        {
            throw new ContractError(failure.Code, failure.Message);
        }
    }

    public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
        StripCoupler.Couple(sections, solution, op, water, polar, cancellation);

    /// <summary>Port is the starboard section with Y and η negated. The root (Y = 0) is kept once. Z is unchanged.</summary>
    internal static SectionSample[] Mirror(IReadOnlyList<SectionSample> starboard)
    {
        var wing = new List<SectionSample>(starboard.Count * 2);
        foreach (SectionSample section in starboard)
        {
            if (Math.Abs(SectionY(section)) > 1e-9) wing.Add(Flip(section));
            wing.Add(section);
        }
        return wing.ToArray();
    }

    private static SectionSample Flip(SectionSample section)
    {
        var frame = section.Frame with { Eta = -section.Frame.Eta, SpanMeters = -section.Frame.SpanMeters };
        var camber = new Point3[section.PlacedCamber.Count];
        for (int i = 0; i < camber.Length; i++)
        {
            Point3 point = section.PlacedCamber[i];
            camber[i] = new Point3(point.X, -point.Y, point.Z);
        }
        return section with { Frame = frame, PlacedCamber = camber };
    }

    private static double SectionY(SectionSample section) =>
        section.PlacedCamber.Count > 0 ? section.PlacedCamber[0].Y : section.Frame.SpanMeters;
}
