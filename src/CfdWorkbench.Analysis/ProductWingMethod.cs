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

    public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
    {
        IReadOnlyList<StripLoad> loads = StripCoupler.Couple(sections, solution, op, water, polar, cancellation);
        var withEdges = loads.Select((load, i) => load with
        {
            // Lattice YInboard/YOutboard are the lower-y and higher-y edges; stations increase in y.
            YLow = solution.Strips[i].YInboard,
            YHigh = solution.Strips[i].YOutboard
        }).ToArray();
        return MarkOutermostProvisional(withEdges);
    }

    /// <summary>
    /// Rulings 78, 88 and 91: the outermost strip of each half (greatest |y|, one port and one starboard) is not
    /// judged against the envelope. It is marked provisional, and no tolerance law replaces that. The scope is certified
    /// finite-chord tips with tip chord at least 2 % of the root chord (<c>AnalysisService.TipChordRatioFloor</c>); the
    /// service refuses every other planform before compute (<c>ANA-TIP-BELOW-FLOOR</c>, or <c>DSL-NOT-ASSESSED</c>). Every other strip is left unset, which the
    /// reader stores as false.
    /// </summary>
    // simplify: the whole tip strip is excluded from judgement, not only its unreliable angle. Ceiling: certified
    // finite-chord tips, r >= 0.02. Upgrade trigger: a new tip study or a lattice change.
    internal static IReadOnlyList<StripLoad> MarkOutermostProvisional(IReadOnlyList<StripLoad> loads)
    {
        int port = Outermost(loads, negative: true);
        int starboard = Outermost(loads, negative: false);
        if (port < 0 && starboard < 0) return loads;
        var marked = loads.ToArray();
        if (port >= 0) marked[port] = Flag(marked[port]);
        if (starboard >= 0) marked[starboard] = Flag(marked[starboard]);
        return marked;
    }

    private static int Outermost(IReadOnlyList<StripLoad> loads, bool negative)
    {
        int at = -1;
        double best = 0;
        for (int i = 0; i < loads.Count; i++)
        {
            double y = loads[i].Y;
            if (negative ? y >= 0 : y <= 0) continue;
            double abs = Math.Abs(y);
            if (at >= 0 && abs <= best) continue;
            at = i;
            best = abs;
        }
        return at;
    }

    private static StripLoad Flag(StripLoad strip) =>
        strip with { Provisional = true, ProvisionalReason = StripLoad.TipProvisionalReason };

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
