using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Analysis.NeuralFoil;

namespace CfdWorkbench.Analysis;

/// <summary>One station's section estimate, evaluated at the wing strip's effective angle.</summary>
public sealed record SectionStationResult(double Eta, double AlphaEffDeg, double Reynolds, double? Depth,
    SectionEstimate Estimate, CavitationResult Cavitation)
{
    public double LiftPerSpan { get; init; }
    public string? EstimatorAvailabilityCode => Depth is <= 0 ? global::CfdWorkbench.Analysis.Cavitation.SurfacePiercing : null;
}

/// <summary>Derived wing section tier; no part of this record is stored in AnalysisRun or PolarSample.</summary>
public sealed record SectionTierResult(IReadOnlyList<SectionStationResult> Stations, CavitationResult Cavitation,
    double GoverningEta, double PanelUnderreadFraction)
{
    public bool GoverningProvisional => PanelUnderreadFraction > 0.10;
    public PolarResult? PolarNcrit2 { get; init; }
    public PolarResult? PolarNcrit4 { get; init; }
    public string? PolarReason2 { get; init; }
    public string? PolarReason4 { get; init; }
    public PolarConsistencyResult? PolarConsistency { get; init; }
}

public static class SectionTier
{
    public static SectionTierResult Derive(AnalysisRun run, byte[] source, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (run.Outcome is not RunOutcome.Completed || run.Settings.SectionEtas is not { Count: > 0 } etas)
            throw new ContractError("ANA-SECTION-UNAVAILABLE", "The run has no section stations.");
        SourceParse parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.SurfaceHash != run.Inputs.SurfaceHash ||
            !parsed.Authored().Assignments.Select(a => a.ProfileIdentity).SequenceEqual(run.Inputs.ProfileHashes))
            throw new ContractError("ANA-SECTION-REVISION-MISMATCH", "The source does not match the run's revision.");
        SectionTierResult result = Evaluate(source, etas, run.Strips, run.Op, run.Water, cancellation);
        if (run.Settings.Polar is null) return result;
        if (run.Settings.Polar.Id != NeuralFoilPolarSource.Method.Id ||
            run.Settings.Polar.Version != NeuralFoilPolarSource.Method.Version)
            return result with { PolarReason2 = "ANA-POLAR-METHOD-MISMATCH", PolarReason4 = "ANA-POLAR-METHOD-MISMATCH" };
        NeuralFoilSection section = RunPolarResolver.SectionsAt(source, [result.GoverningEta], cancellation)[result.GoverningEta];
        var polar = new NeuralFoilPolarSource(hash => hash == section.ProfileHash ? section : null);
        SectionStationResult station = result.Stations.MinBy(item => Math.Abs(item.Eta - result.GoverningEta))!;
        (PolarResult? n2, string? r2) = Read(2);
        (PolarResult? n4, string? r4) = Read(4);
        return result with { PolarNcrit2 = n2, PolarNcrit4 = n4, PolarReason2 = r2, PolarReason4 = r4,
            PolarConsistency = TipPolarConsistency.Derive(run, source, cancellation) };

        (PolarResult?, string?) Read(int ncrit)
        {
            try
            {
                PolarResult? sampled = polar.Sample(section.ProfileHash, station.Reynolds, ncrit,
                    station.AlphaEffDeg, run.Water, cancellation);
                return (sampled, sampled?.AvailabilityCode);
            }
            catch (ContractError error) when (error.Code.StartsWith("ANA-POLAR-", StringComparison.Ordinal))
            {
                return (null, error.Code);
            }
        }
    }

    public static SectionTierResult Evaluate(byte[] source, IReadOnlyList<double> etas,
        IReadOnlyList<StripLoad> strips, OperatingPoint op, WaterRecord water, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(etas);
        ArgumentNullException.ThrowIfNull(strips);
        if (etas.Count == 0) throw new ContractError("ANA-SECTION-UNAVAILABLE", "The run has no section stations.");
        IReadOnlyList<SectionSample> sections = PanelMethod.SampleSections(source, etas, PanelMethod.DefaultPanelCount, cancellation);
        StationFrame datum = sections.MinBy(s => s.Frame.Eta)!.Frame;
        double wingAlpha = VortexLattice.ToRadians(op.AlphaDeg);
        var stations = new SectionStationResult[sections.Count];
        double bestRatio = double.PositiveInfinity;
        int governing = -1;
        for (int i = 0; i < sections.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            SectionSample section = sections[i];
            double alphaEff = strips.Count == 0 ? op.AlphaDeg : strips.MinBy(s => Math.Abs(Math.Abs(s.Eta) - section.Frame.Eta))!.AlphaEff;
            double chord = section.Frame.TrailingMeters - section.Frame.LeadingMeters;
            double reynolds = op.Speed * chord / water.Nu;
            SectionEstimate estimate = SectionEstimator.Estimate(section, alphaEff, reynolds, cancellation);
            double? depth = null;
            if (op.HRef is double h)
            {
                double rise = (section.Frame.ElevationMeters - datum.ElevationMeters) * Math.Cos(wingAlpha) -
                    (section.Frame.LeadingMeters - datum.LeadingMeters) * Math.Sin(wingAlpha);
                depth = h - rise;
            }
            string name = "η " + section.Frame.Eta.ToString("0.###", CultureInfo.InvariantCulture);
            CavitationResult screen = Cavitation.Screen(estimate.Panel.CpMin, estimate.Panel.StationCount, depth,
                op.Speed, water.Rho, op.PAtm, water.Pv, name);
            stations[i] = new(section.Frame.Eta, alphaEff, reynolds, depth, estimate, screen)
            {
                LiftPerSpan = 0.5 * water.Rho * op.Speed * op.Speed * chord * estimate.Cl
            };
            if (screen.Sigma is { } sigma && estimate.Panel.CpMin < 0)
            {
                double ratio = sigma / -estimate.Panel.CpMin;
                if (ratio < bestRatio) { bestRatio = ratio; governing = i; }
            }
        }
        // With depth absent no sigma ratio exists. Keep a measurable Cp governing station; cavitation remains Unavailable.
        if (governing < 0)
            governing = Array.FindIndex(stations, station => station.Estimate.Panel.CpMin == stations.Min(s => s.Estimate.Panel.CpMin));
        SectionStationResult selected = stations[governing];
        SectionSample fineSection = PanelMethod.SampleSection(source, selected.Eta, 400, cancellation);
        double fineCp = PanelMethod.Solve(fineSection, selected.AlphaEffDeg, cancellation).CpMin;
        double coarseSuction = Math.Max(0, -selected.Estimate.Panel.CpMin);
        double fineSuction = Math.Max(0, -fineCp);
        double underread = fineSuction > 0 ? (fineSuction - coarseSuction) / fineSuction : 0;
        CavitationResult wingScreen = Cavitation.SelectWing(stations.Select(station => station.Cavitation).ToArray());
        return new(stations, wingScreen, selected.Eta, underread);
    }

    /// <summary>Carry the Ruling 90 provisional code on the nearest judged strip; the tip's not-judged code has priority.</summary>
    public static IReadOnlyList<StripLoad> MarkGoverning(IReadOnlyList<StripLoad> strips, SectionTierResult result)
    {
        if (!result.GoverningProvisional || strips.Count == 0) return strips;
        int at = -1;
        double nearest = double.PositiveInfinity;
        for (int i = 0; i < strips.Count; i++)
        {
            double distance = Math.Abs(Math.Abs(strips[i].Eta) - result.GoverningEta);
            if (distance >= nearest || strips[i].ProvisionalReason == StripLoad.TipProvisionalReason) continue;
            nearest = distance;
            at = i;
        }
        if (at < 0) return strips;
        var marked = strips.ToArray();
        marked[at] = marked[at] with { Provisional = true, ProvisionalReason = StripLoad.PanelUnderreadReason };
        return marked;
    }
}
