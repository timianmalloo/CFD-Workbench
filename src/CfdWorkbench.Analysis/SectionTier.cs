using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Analysis.NeuralFoil;

namespace CfdWorkbench.Analysis;

/// <summary>One station's section estimate, evaluated at the wing strip's effective angle.</summary>
public sealed record SectionStationResult(double Eta, double AlphaEffDeg, double Reynolds, double? Depth,
    SectionEstimate Estimate, CavitationResult Cavitation)
{
    public double LiftPerSpan { get; init; }
    /// <summary>
    /// Two-grid (200 vs 400 panels, p assumed 1) suction under-read of this station. Null means <b>not measured</b>:
    /// the station was not solved at 400 (Ruling 110 (4)). Never blank, never OK; its display text is operator copy.
    /// </summary>
    public double? PanelUnderread { get; init; }
    public bool PanelUnderreadMeasured => PanelUnderread.HasValue;
    /// <summary>
    /// Ruling 142: the strip this station reads α_eff from is the tip-provisional strip (<see cref="StripLoad.TipProvisionalReason"/>).
    /// Such a station never decides the wing cavitation verdict. Distinct from <see cref="Provisional"/>, the panel under-read state.
    /// </summary>
    public bool TipNotJudged { get; init; }
    /// <summary>The DR-DXM-7 provisional state: measured and above 10 %. A not-measured station is not provisional.</summary>
    public bool Provisional => PanelUnderread > 0.10;
    public string? EstimatorAvailabilityCode => Depth is <= 0 ? global::CfdWorkbench.Analysis.Cavitation.SurfacePiercing : null;
}

/// <summary>Derived wing section tier; no part of this record is stored in AnalysisRun or PolarSample.</summary>
public sealed record SectionTierResult(IReadOnlyList<SectionStationResult> Stations, CavitationResult Cavitation,
    double GoverningEta, double PanelUnderreadFraction)
{
    /// <summary>Two-grid, p assumed 1: the governing station's 200-vs-400 suction under-read above 10 %.</summary>
    public bool GoverningProvisional => PanelUnderreadFraction > 0.10;
    /// <summary>How many stations were solved at 400 panels (the governing station and its near-tie candidates, at most 4).</summary>
    public int PanelCandidateCount { get; init; }
    /// <summary>Ruling 142: stations left out of the wing cavitation verdict because they read the tip-provisional strip.</summary>
    public int TipNotJudgedCount { get; init; }
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
            StripLoad? nearest = strips.Count == 0 ? null : strips.MinBy(s => Math.Abs(Math.Abs(s.Eta) - section.Frame.Eta));
            double alphaEff = nearest?.AlphaEff ?? op.AlphaDeg;
            bool tipNotJudged = nearest is { Provisional: true, ProvisionalReason: StripLoad.TipProvisionalReason };
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
                LiftPerSpan = 0.5 * water.Rho * op.Speed * op.Speed * chord * estimate.Cl,
                TipNotJudged = tipNotJudged
            };
        }
        // Ruling 142: only judged stations decide the verdict. With every station tip-provisional there is no verdict, but the
        // Section view still needs a station to show, so the selection below runs over all of them and the wing screen is replaced.
        bool noneJudged = stations.All(station => station.TipNotJudged);
        bool Judged(int index) => noneJudged || !stations[index].TipNotJudged;
        for (int i = 0; i < stations.Length; i++)
        {
            SectionStationResult candidate = stations[i];
            if (!Judged(i)) continue;
            if (candidate.Cavitation.Sigma is { } sigma && candidate.Estimate.Panel.CpMin < 0)
            {
                double ratio = sigma / -candidate.Estimate.Panel.CpMin;
                if (ratio < bestRatio) { bestRatio = ratio; governing = i; }
            }
        }
        // With depth absent no sigma ratio exists. Keep a measurable Cp governing station; cavitation remains Unavailable.
        if (governing < 0)
            governing = Enumerable.Range(0, stations.Length).Where(Judged)
                .MinBy(i => stations[i].Estimate.Panel.CpMin);
        // Ruling 103/110. The 200-pass winner is re-solved at 400; its measured under-read u sets the near-tie width.
        // Candidates inside the Ruling 114 width are re-solved at 400, at most MaxPanelCandidates in all.
        // The governing station is then re-selected among the 400-solved stations by 400 ratio.
        // A 400 value is never compared with a 200 value (Ruling 110 (2)).
        var solved = new List<int>(MaxPanelCandidates);
        int winner200 = governing;
        Refine(winner200);
        solved.Add(winner200);
        if (!double.IsPositiveInfinity(bestRatio))
        {
            // Ruling 114: width = max(2u, 25 %). Slots: the governing station, the two lowest 200 ratios inside the
            // width, and the thinnest station inside it (a thin station under-reads most, so it swaps most often).
            double width = Math.Max(2 * stations[winner200].PanelUnderread!.Value, MinNearTieWidth);
            int[] inside = Enumerable.Range(0, stations.Length)
                .Where(i => i != winner200 && Judged(i) && Ratio(stations[i]) is double r && r <= bestRatio * (1 + width))
                .OrderBy(i => Ratio(stations[i])).ToArray();
            int[] others = inside.Take(MaxPanelCandidates - 2)
                .Concat(inside.Skip(MaxPanelCandidates - 2).OrderBy(i => sections[i].Frame.ThicknessRatio).Take(1)).ToArray();
            foreach (int i in others) { Refine(i); solved.Add(i); }
            double bestFine = double.PositiveInfinity;
            foreach (int i in solved)
                if (Ratio(stations[i]) is double r && r < bestFine) { bestFine = r; governing = i; }
        }
        SectionStationResult selected = stations[governing];
        CavitationResult[] screens = stations.Select(station => station.Cavitation).ToArray();
        CavitationResult[] judgedScreens = Enumerable.Range(0, stations.Length).Where(Judged).Select(i => screens[i]).ToArray();
        // An Unavailable station (for example surface-piercing) is reported whichever grid it was solved on.
        CavitationResult wingScreen = judgedScreens.Any(screen => screen.State == CavitationState.Unavailable)
            ? Cavitation.SelectWing(judgedScreens)
            : Cavitation.SelectWing(solved.Select(i => screens[i]).ToArray());
        // Ruling 142 (2): with no judged station the wing line reads Not judged - tip strip, with no sigma and no V_crit.
        if (noneJudged)
            wingScreen = wingScreen with { State = CavitationState.Unavailable, Sigma = null, CriticalSpeed = null, CpMin = null,
                GoverningStation = null, GoverningDepth = null, Reason = StripLoad.TipProvisionalReason };
        return new(stations, wingScreen, selected.Eta, selected.PanelUnderread!.Value)
        {
            PanelCandidateCount = solved.Count,
            TipNotJudgedCount = noneJudged ? 0 : stations.Count(station => station.TipNotJudged)
        };

        static double? Ratio(SectionStationResult station) =>
            station.Cavitation.Sigma is { } sigma && station.Estimate.Panel.CpMin < 0 ? sigma / -station.Estimate.Panel.CpMin : null;

        void Refine(int index)
        {
            SectionStationResult coarse = stations[index];
            SectionSample fineSection = PanelMethod.SampleSection(source, coarse.Eta, PanelMethod.GoverningPanelCount, cancellation);
            SectionEstimate fine = SectionEstimator.Estimate(fineSection, coarse.AlphaEffDeg, coarse.Reynolds, cancellation);
            double fineChord = fineSection.Frame.TrailingMeters - fineSection.Frame.LeadingMeters;
            stations[index] = coarse with
            {
                Estimate = fine,
                Cavitation = Cavitation.Screen(fine.Panel.CpMin, fine.Panel.StationCount, coarse.Depth,
                    op.Speed, water.Rho, op.PAtm, water.Pv, coarse.Cavitation.GoverningStation ?? ""),
                LiftPerSpan = 0.5 * water.Rho * op.Speed * op.Speed * fineChord * fine.Cl,
                PanelUnderread = Underread(coarse.Estimate.Panel.CpMin, fine.Panel.CpMin)
            };
        }
    }

    /// <summary>Most stations re-solved at 400 panels in one run: the governing station plus its near-tie candidates (Ruling 110).</summary>
    public const int MaxPanelCandidates = 4;

    /// <summary>Floor of the near-tie width (Ruling 114): a station within 25 % of the best 200 ratio is a candidate.</summary>
    public const double MinNearTieWidth = 0.25;

    /// <summary>
    /// The measured two-grid (200 vs 400 panels, p assumed 1) suction under-read of one station at its own α_eff and Re.
    /// It feeds only the provisional state; it never changes a screen (Ruling 110 (3)). Caller: the Section tab, for the shown station.
    /// </summary>
    public static double UnderreadAt(byte[] source, double eta, double alphaEffDeg, double reynolds,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        SectionEstimate coarse = SectionEstimator.Estimate(source, eta, alphaEffDeg, reynolds, PanelMethod.DefaultPanelCount, cancellation);
        SectionEstimate fine = SectionEstimator.Estimate(source, eta, alphaEffDeg, reynolds, PanelMethod.GoverningPanelCount, cancellation);
        return Underread(coarse.Panel.CpMin, fine.Panel.CpMin);
    }

    private static double Underread(double coarseCp, double fineCp)
    {
        double coarseSuction = Math.Max(0, -coarseCp);
        double fineSuction = Math.Max(0, -fineCp);
        return fineSuction > 0 ? (fineSuction - coarseSuction) / fineSuction : 0;
    }

}
