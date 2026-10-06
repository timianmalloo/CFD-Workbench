using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Analysis.NeuralFoil;

namespace CfdWorkbench.Analysis;

/// <summary>One drawn point of a chart series.</summary>
public readonly record struct ChartPoint(double X, double Y);

/// <summary>A line. <paramref name="Marker"/> is one of none, dot, square, triangle, diamond; shape and dash tell series apart as well as colour.</summary>
public sealed record ChartSeries(string Name, IReadOnlyList<ChartPoint> Points, bool Dashed, string Marker, int Colour);

/// <summary>A vertical line at X (Y null), or a dot at (X, Y).</summary>
public sealed record ChartMarker(string Name, double X, double? Y);

/// <summary>One plot with its own axes. The band lies between two series of equal X.</summary>
public sealed record ChartPlot(string Title, string XTitle, string YTitle, bool InvertY, IReadOnlyList<ChartSeries> Series,
    IReadOnlyList<ChartMarker> Markers, (string Low, string High)? Band = null);

/// <summary>A chart of the Section tab: its plots, its legend line and the fixed label that travels with it.</summary>
public sealed record ChartModel(string Id, string Title, IReadOnlyList<ChartPlot> Plots, string Legend, string Label,
    string? Unavailable = null);

/// <summary>The Section tab content for one shown station (DXM-9): tables and the four charts of one chart selector (DXM-4).</summary>
public sealed record SectionView(double Eta, bool IsGoverning, string StationName, IReadOnlyList<ResultGroup> Groups,
    IReadOnlyList<ChartModel> Charts, int UnderreadSolves);

/// <summary>
/// The A3b and A3c Section tab for one station, as pure data. The shown station is the selected strip, else the governing
/// cavitation station (DR-DXM-9). Its panel under-read comes from the run when the station was solved at 400 panels, else from
/// one on-demand <c>SectionTier.UnderreadAt</c> through <paramref name="underread"/>; that value feeds only the provisional row
/// and never the cavitation screen (Ruling 110 (3)).
/// </summary>
public static class SectionDisplay
{
    public const double SelectionTolerance = 0.02;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public delegate double UnderreadSeam(byte[] source, double eta, double alphaEffDeg, double reynolds);

    public static SectionView Build(AnalysisRun run, SectionTierResult tier, byte[]? source, double? selectedEta,
        string? revision = null, UnderreadSeam? underread = null, IReadOnlyList<ResultRow>? strip = null,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(tier);
        SectionStationResult governing = tier.Stations.MinBy(s => Math.Abs(s.Eta - tier.GoverningEta))!;
        SectionStationResult? picked = selectedEta is { } eta
            ? tier.Stations.Where(s => Math.Abs(s.Eta - Math.Abs(eta)) <= SelectionTolerance).MinBy(s => Math.Abs(s.Eta - Math.Abs(eta)))
            : null;
        SectionStationResult station = picked ?? governing;
        bool isGoverning = picked is null;
        string name = Labels.StationName(station.Eta, isGoverning);
        var groups = new List<ResultGroup> { new("Station", [R("Station", name)]) };

        // ---- estimator tier (A3b) ----
        PanelResult panel = station.Estimate.Panel;
        (double cpX, string side) = CpMinLocation(panel);
        bool depthSet = run.Op.HRef.HasValue;
        var estimator = new List<ResultRow>
        {
            R("Tier", Labels.EstimatorChip, note: depthSet ? Labels.EstimatorLabelDeep : Labels.EstimatorLabelNoDepth),
            R(Labels.ClPanel, N(station.Estimate.Cl, "0.###")),
            R(Labels.CmQuarter, N(station.Estimate.CmQuarter, "0.###")),
            R(Labels.AlphaL0Panel, N(station.Estimate.AlphaL0Deg, "0.###"), "°"),
            R(Labels.CdBoundLabel, N(station.Estimate.CdTurbulentBound, "0.#####"), note: Labels.CdBoundNote),
            R(Labels.CpMinLabel, N(-panel.CpMin, "0.###"), note: Labels.CpMinWhere(cpX, side, panel.StationCount))
        };
        if (station.EstimatorAvailabilityCode is { } code)
            estimator = [R("Tier", Labels.EstimatorChip, note: Labels.UnavailableBecause(code)), R(Labels.CpMinLabel, Labels.UnavailableBecause(code))];
        groups.Add(new("Estimator", estimator));

        // ---- cavitation screen ----
        CavitationResult cav = tier.Cavitation;
        var screen = new List<ResultRow>();
        screen.Add(R("Cavitation screen", cav.State switch
        {
            CavitationState.Clear => Labels.CavClear,
            CavitationState.InsideMargin => Labels.CavInside,
            CavitationState.PossibleAboveCritical => Labels.CavPossible,
            _ => Labels.UnavailableBecause(cav.Reason ?? "")
        }));
        string none = cav.Reason is { } why ? Labels.UnavailableBecause(why) : "";
        screen.Add(R(Labels.SigmaLabel, cav.Sigma is { } sigma ? N(sigma, "0.00") : none));
        screen.Add(R(Labels.CpMinLabel, cav.CpMin is { } cp && cp < 0 ? N(-cp, "0.00") : N(cav.CpMin is { } c ? -c : double.NaN, "0.00")));
        screen.Add(R(Labels.VcritLabel, cav.CriticalSpeed is { } vc ? N(vc, "0.##") : none, cav.CriticalSpeed is null ? null : "m/s"));
        screen.Add(R("Margin", N(100 * cav.MarginFraction, "0.#"), "%", Labels.MarginLabel));
        if (cav.GoverningDepth is { } depth && tier.Stations.Count > 0)
            screen.Add(R("Governing station", Labels.StationCavitationLine(tier.GoverningEta, depth, tier.Stations.Count)));
        screen.Add(R("Screen", cav.ScreenText));
        groups.Add(new("Cavitation", screen));

        // ---- panel under-read of the shown station (Rulings 110, 113) ----
        double? measured = station.PanelUnderread;
        int solves = 0;
        if (measured is null && source is not null && underread is not null)
        {
            measured = underread(source, station.Eta, station.AlphaEffDeg, station.Reynolds);
            solves = 1;
        }
        var gap = new List<ResultRow>();
        if (measured is { } fraction)
        {
            gap.Add(R(Labels.UnderreadMeasured, N(100 * fraction, "0.00"), "%"));
            if (fraction > 0.10) gap.Add(R("Provisional", Labels.Provisional));
        }
        else gap.Add(R("Cp_min under-read", Labels.UnderreadNotMeasured));
        groups.Add(new("Under-read", gap));
        // The tier samples every span eta (126 on the example); the table lists the stations that carry a measurement or are on screen.
        groups.Add(new("Stations", tier.Stations.Where(s => s.PanelUnderread is not null || s.Eta == station.Eta || s.Eta == governing.Eta)
            .OrderBy(s => s.Eta).Select(s => R("η " + N(s.Eta, "0.###"), N(-s.Estimate.Panel.CpMin, "0.###"),
            note: s.PanelUnderread is { } u ? Labels.UnderreadMeasured + " " + N(100 * u, "0.00") + " %" +
                (u > 0.10 ? " · " + Labels.Provisional : "") : Labels.UnderreadNotMeasured)).ToArray()));

        // ---- polar tier (A3c) ----
        NeuralFoilSection? section = null;
        if (run.Settings.Polar is not null && source is not null)
            try { section = RunPolarResolver.SectionsAt(source, [station.Eta], cancellation)[station.Eta]; }
            catch (ContractError) { section = null; }
        ResultRow[]? beside = strip?.Where(row => row.Label == "Envelope (this strip)").ToArray();
        if (section is not null)
        {
            var src = new NeuralFoilPolarSource(_ => section);
            var polar = new List<ResultRow>
            {
                R("Tier", Labels.PolarChip, note: Labels.SurrogateAccuracy + " · " + Labels.PolarSurrogate),
                R("Method", NeuralFoilPolarSource.Method.Id)
            };
            NeuralFoilEvaluation? first = null;
            foreach (int ncrit in new[] { 4, 2 })
            {
                NeuralFoilEvaluation ev = src.Evaluate(section, station.AlphaEffDeg, station.Reynolds, ncrit, cancellation);
                first ??= ev;
                polar.Add(R("Ncrit " + ncrit, ev.Prediction is { } p
                    ? "cl " + N(p.Cl, "0.###") + " · cd " + N(p.Cd, "0.#####") + " · cm " + N(p.Cm, "0.###") +
                        " · x_tr " + N(p.XtrUpper, "0.###") + "/" + N(p.XtrLower, "0.###")
                    : NotComputable(ev, station)));
            }
            polar.Add(R("Confidence", first!.Prediction is { } fp ? Labels.Confidence(fp.AnalysisConfidence) : Labels.ConfidenceNotRecorded));
            if (first.LowConfidence) polar.Add(R("Low confidence", Labels.LowConfidence(first.Prediction?.AnalysisConfidence)));
            polar.Add(R("Section fit", Labels.CstResidual(first.CstResidualMax, first.CstResidualRms)));
            polar.Add(R(Labels.BracketLabel, first.Computable ? Bracket(first, station, section) : "Unavailable"));
            if (beside is { Length: > 0 }) polar.AddRange(beside);
            polar.Add(R("Polar Re range", Re(station.Reynolds)));
            polar.Add(R("Tripped", Labels.PolarNotComputed));
            if (tier.PolarConsistency?.Strips.FirstOrDefault(s => Math.Abs(s.Eta - station.Eta) < 1e-9) is { ClDelta: { } delta })
                polar.Add(R(Labels.DeltaVsLattice, N(delta, "0.###"), note: Labels.DeltaVsLatticeNote));
            groups.Add(new("Polar", polar));
        }

        var charts = Charts(run, tier, station, section, source, revision, cancellation);
        return new(station.Eta, isGoverning, name, groups, charts, solves);
    }

    private static string Re(double re) =>
        re is >= NeuralFoilPolarSource.ReynoldsMin and <= NeuralFoilPolarSource.ReynoldsMax
            ? Labels.ReInside(re, NeuralFoilPolarSource.ReynoldsMin, NeuralFoilPolarSource.ReynoldsMax)
            : Labels.ReOutside(re, NeuralFoilPolarSource.ReynoldsMin, NeuralFoilPolarSource.ReynoldsMax);

    /// <summary>The bracket row (DXM-2, DXM-8): the default inside text, or one flag per axis, computed and flagged.</summary>
    public static string Bracket(NeuralFoilEvaluation ev, SectionStationResult station, NeuralFoilSection section)
    {
        if (!ev.OutsideValidatedBracket) return Labels.BracketInside;
        var parts = new List<string>();
        foreach (string reason in ev.OutsideBracketReasons)
            parts.Add(reason.StartsWith("alpha", StringComparison.Ordinal) ? Labels.BracketOutsideAlpha(station.AlphaEffDeg)
                : reason.StartsWith("Re ", StringComparison.Ordinal) ? Labels.BracketOutsideRe(station.Reynolds)
                : reason.StartsWith("Ncrit", StringComparison.Ordinal) ? Labels.BracketOutsideNcrit(3)
                : Labels.BracketOutsideFamily(Labels.UnvalidatedFamily));
        return string.Join(" ", parts);
    }

    /// <summary>A non-computable point reads the approved Unavailable text with its own number (DXM-2, row 37).</summary>
    private static string NotComputable(NeuralFoilEvaluation ev, SectionStationResult station) => ev.Reason switch
    {
        { } r when r.StartsWith("alpha", StringComparison.Ordinal) => Labels.TrainingAlpha(station.AlphaEffDeg),
        { } r when r.StartsWith("Re", StringComparison.Ordinal) => Labels.TrainingRe(station.Reynolds),
        { } r when r.StartsWith("Ncrit", StringComparison.Ordinal) => Labels.TrainingNcrit(station.AlphaEffDeg),
        _ => Labels.CstOverLimit(ev.CstResidualMax)
    };

    private static (double X, string Side) CpMinLocation(PanelResult panel)
    {
        // Upper runs TE to LE and Lower LE to TE; CpMin omits the three panels nearest the TE on each side.
        double best = double.PositiveInfinity, x = 0;
        string side = "upper";
        int skip = PanelMethod.CpMinTrailingEdgePanelsPerSide;
        for (int i = skip; i < panel.Upper.Count; i++)
            if (panel.Upper[i].Cp < best) { best = panel.Upper[i].Cp; x = panel.Upper[i].X; side = "upper"; }
        for (int i = 0; i < panel.Lower.Count - skip; i++)
            if (panel.Lower[i].Cp < best) { best = panel.Lower[i].Cp; x = panel.Lower[i].X; side = "lower"; }
        return (x, side);
    }

    private static IReadOnlyList<ChartModel> Charts(AnalysisRun run, SectionTierResult tier, SectionStationResult station,
        NeuralFoilSection? section, byte[]? source, string? revision, CancellationToken cancellation)
    {
        PanelResult panel = station.Estimate.Panel;
        (double cpX, _) = CpMinLocation(panel);
        double lo = Math.Min(panel.Upper.Concat(panel.Lower).Min(p => p.Cp), 0), hi = Math.Max(panel.Upper.Concat(panel.Lower).Max(p => p.Cp), 0);
        var cpPlot = new ChartPlot("Cp", "x/c", "Cp (suction up)", true,
            [new("upper", panel.Upper.Select(p => new ChartPoint(p.X, p.Cp)).OrderBy(p => p.X).ToArray(), false, "none", 0),
             new("lower", panel.Lower.Select(p => new ChartPoint(p.X, p.Cp)).OrderBy(p => p.X).ToArray(), true, "none", 0)],
            [new("Cp_min", cpX, panel.CpMin)]);
        string cpLegend = Labels.CpLegend + " · " + N(lo, "0.##") + " to +" + N(hi, "0.##");
        string label = run.Op.HRef.HasValue ? Labels.EstimatorLabelDeep : Labels.EstimatorLabelNoDepth;
        var charts = new List<ChartModel> { new("cp", "Cp", [cpPlot], cpLegend, label) };
        // The Section view (DXM-4): the profile with Cp drawn as a comb off each surface, Cp = 0 pinned at the contour.
        const double comb = 0.12;
        ChartPoint[] Offset(IReadOnlyList<PanelCp> side, double outward) =>
            side.OrderBy(p => p.X).Select(p => new ChartPoint(p.X, p.Z + outward * -p.Cp * comb)).ToArray();
        var profile = new ChartPlot("Section", "x/c", "z/c", false,
            [new("upper", panel.Upper.OrderBy(p => p.X).Select(p => new ChartPoint(p.X, p.Z)).ToArray(), false, "none", 0),
             new("lower", panel.Lower.OrderBy(p => p.X).Select(p => new ChartPoint(p.X, p.Z)).ToArray(), true, "none", 0),
             new("Cp upper", Offset(panel.Upper, 1), false, "none", 1),
             new("Cp lower", Offset(panel.Lower, -1), true, "none", 1)],
            [new("Cp_min", cpX, panel.Upper.Concat(panel.Lower).OrderBy(p => Math.Abs(p.X - cpX)).First().Z)]);
        charts.Add(new("profile", "Section", [profile], cpLegend, label));

        if (section is null || source is null)
        {
            string reason = run.Settings.Polar is null ? Labels.NoPolar : Labels.UnavailableBecause("ANA-POLAR-REVISION-MISSING");
            charts.Add(new("polar", "Polar", [], "", Labels.PolarSurrogate, reason));
            charts.Add(new("transition", "Transition", [], "", Labels.PolarSurrogate, reason));
        }
        else
        {
            var src = new NeuralFoilPolarSource(_ => section);
            var rows = new Dictionary<int, List<(double A, NeuralFoilPrediction P)>> { [2] = [], [4] = [] };
            // One CST fit serves the whole sweep. DXM-2: points are computed inside the training range and flagged by the bracket row
            // (validated or not); the sweep is the bracket's α range, so nothing is extrapolated beyond it, and a section the fit
            // cannot represent draws nothing.
            CstFitResult fit = CstFit.Fit(section);
            bool validated = fit.MaxResidual <= NeuralFoilPolarSource.MaxCstResidual &&
                station.Reynolds is >= NeuralFoilPolarSource.TrainingReynoldsMin and <= NeuralFoilPolarSource.TrainingReynoldsMax;
            NeuralFoilNetwork network = NeuralFoilNetwork.FromEmbedded();
            if (validated)
                for (double a = NeuralFoilPolarSource.AlphaMinDeg; a <= NeuralFoilPolarSource.AlphaMaxDeg + 1e-9; a += 1)
                    foreach (int ncrit in new[] { 2, 4 })
                        rows[ncrit].Add((a, network.Predict(fit.Parameters, a, station.Reynolds, ncrit)));
            ChartSeries S(string n, int ncrit, Func<NeuralFoilPrediction, double> y, string marker, int colour) =>
                new(n, rows[ncrit].Select(r => new ChartPoint(r.A, y(r.P))).ToArray(), ncrit == 2, marker, colour);
            var alphaMark = new ChartMarker("α_eff", station.AlphaEffDeg, null);
            var cl = new ChartPlot("cl", "α (°)", "cl", false, [S("cl Ncrit 4", 4, p => p.Cl, "dot", 0), S("cl Ncrit 2", 2, p => p.Cl, "square", 1)], [alphaMark]);
            var cd = new ChartPlot("cd", "α (°)", "cd", false, [S("cd Ncrit 4", 4, p => p.Cd, "dot", 0), S("cd Ncrit 2", 2, p => p.Cd, "square", 1)],
                [alphaMark], ("cd Ncrit 4", "cd Ncrit 2"));
            charts.Add(new("polar", "Polar", [cl, cd], Labels.SurrogateAccuracy + " · " + Labels.PolarSurrogate +
                " · inside −6° to 6° only; nothing is extrapolated", Labels.PolarSurrogate));
            var tr = new ChartPlot("x_tr/c", "α (°)", "x_tr/c", false,
                [S("upper Ncrit 4", 4, p => p.XtrUpper, "dot", 0), S("upper Ncrit 2", 2, p => p.XtrUpper, "square", 1),
                 S("lower Ncrit 4", 4, p => p.XtrLower, "triangle", 2), S("lower Ncrit 2", 2, p => p.XtrLower, "diamond", 3)], [alphaMark]);
            charts.Add(new("transition", "Transition", [tr], Labels.TransitionLegend(revision ?? run.Inputs.AcceptedId, station.Reynolds) +
                " · " + Labels.OverlayMenu, Labels.PolarSurrogate));
        }

        charts.Add(Bucket(run, tier, station, source, cancellation));
        return charts;
    }

    private static ChartModel Bucket(AnalysisRun run, SectionTierResult tier, SectionStationResult station, byte[]? source,
        CancellationToken cancellation)
    {
        string label = Labels.EstimatorLabelNoDepth;
        if (source is null || station.Depth is not { } depth || depth <= 0)
            return new("bucket", "Bucket", [], "", label, Labels.UnavailableBecause(
                station.Depth is null ? Cavitation.DepthNotSet : Cavitation.SurfacePiercing));
        SectionSample sample = PanelMethod.SampleSection(source, station.Eta, PanelMethod.DefaultPanelCount, cancellation);
        PanelMethod.Prepared prepared = PanelMethod.Prepare(sample, cancellation);
        double numerator = run.Op.PAtm + run.Water.Rho * OperatingPoints.Gravity * depth - run.Water.Pv;
        var sigmaRequired = new List<ChartPoint>();
        var vCrit = new List<ChartPoint>();
        for (double a = 0; a <= 8.0001; a += 1)
        {
            PanelResult r = prepared.Solve(a, cancellation);
            if (-r.CpMin <= 0 || !(numerator > 0)) continue;
            sigmaRequired.Add(new ChartPoint(r.Cl, -r.CpMin));
            vCrit.Add(new ChartPoint(r.Cl, Math.Sqrt(2 * numerator / (run.Water.Rho * -r.CpMin))));
        }
        double sigma = numerator / (0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed);
        var s1 = new ChartPlot("σ required", "Cl", "σ required (−Cp_min)", false, [new("σ required", sigmaRequired, false, "dot", 0)],
            [new("operating σ", station.Estimate.Cl, sigma)]);
        var s2 = new ChartPlot("V_crit", "Cl", "V_crit (m/s)", false, [new("V_crit", vCrit, false, "dot", 0)],
            [new("operating speed", station.Estimate.Cl, run.Op.Speed)]);
        return new("bucket", "Bucket", [s1, s2], Labels.BucketLegend + " · " + tier.Cavitation.ScreenText, label);
    }

    private static ResultRow R(string label, string value, string? unit = null, string? note = null) => new(label, value, unit, note);
    private static string N(double value, string format) => value.ToString(format, Inv);
}
