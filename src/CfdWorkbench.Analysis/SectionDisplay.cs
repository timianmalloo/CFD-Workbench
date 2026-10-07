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
    IReadOnlyList<ChartModel> Charts, int UnderreadSolves, IReadOnlyList<StationTableRow>? StationTable = null,
    SectionProfile? Profile = null);

/// <summary>
/// The Section view's profile (approved mockup, DX state 5): the closed outline as panel points in order (upper TE to LE, then
/// lower LE to TE), each carrying the Cp of the existing panel solve; the Cp_min marker at the panel that sets Cp_min; the data
/// range (<paramref name="CpLow"/>, <paramref name="CpHigh"/>) the legend names; and the caption plates. Never re-solved.
/// </summary>
public sealed record SectionProfile(IReadOnlyList<PanelCp> Outline, PanelCp CpMinPanel, string Side, double CpLow, double CpHigh,
    string Caption, string Tier, string? Cavitation, SectionForces? Forces = null);

/// <summary>Where the Lift and Drag arrows start: the lattice centre of pressure, or the quarter chord with the couple drawn (Ruling 128 (3)).</summary>
public enum ForceAnchor { CentreOfPressure, QuarterChord }

/// <summary>
/// The lattice strip's force record for the shown station (Rulings 127, 128, 130), per span, SI; the display unit is applied by
/// <see cref="Labels"/>. Lift is perpendicular and drag parallel to V∞, which lies at <see cref="AlphaGeoDeg"/> to the chord line.
/// <see cref="LiftScale"/> is N/m for one chord of arrow, fixed per run; arrow lengths in chords are the <c>*Chords</c> members
/// (drag already carries <see cref="DragMultiple"/>). <see cref="XcpOverC"/> is the lattice centre of pressure whether or not it is
/// used; <see cref="Anchor"/> and <see cref="XcpText"/> say what is shown.
/// </summary>
public sealed record SectionForces(double Eta, double ChordMeters, double AlphaGeoDeg, double AlphaEffDeg, double AlphaIDeg, double ClLattice,
    double LiftPerSpan, double CouplePerSpan, double? XcpOverC, ForceAnchor Anchor, string XcpText,
    double? ProfileLow, double? ProfileHigh, string? ProfileFlags, string? ProfileUnavailable, double InducedPerSpan,
    double LiftScale, int DragMultiple, Units Units)
{
    /// <summary>Centre of the Ncrit 2-4 band.</summary>
    public double? ProfileMid => ProfileLow is { } low && ProfileHigh is { } high ? 0.5 * (low + high) : null;
    public double TotalPerSpan => (ProfileMid ?? 0) + InducedPerSpan;
    public bool LowConfidence => StripFlags.Has(ProfileFlags, StripFlags.LowConfidence);
    /// <summary>The anchor on the chord, as a fraction of the chord from the leading edge.</summary>
    public double AnchorX => Anchor == ForceAnchor.CentreOfPressure ? XcpOverC!.Value : 0.25;
    /// <summary>Unit vectors in section axes (x aft along the chord, z up): V∞ and drag at α_geo, lift perpendicular and leaning forward.</summary>
    public (double X, double Z) FreeStream => (Math.Cos(VortexLattice.ToRadians(AlphaGeoDeg)), Math.Sin(VortexLattice.ToRadians(AlphaGeoDeg)));
    public (double X, double Z) LiftDirection => (-FreeStream.Z, FreeStream.X);
    public double LiftChords => LiftPerSpan / LiftScale;
    public double? ProfileLowChords => ProfileLow is { } v ? v * DragMultiple / LiftScale : null;
    public double? ProfileHighChords => ProfileHigh is { } v ? v * DragMultiple / LiftScale : null;
    public double ProfileChords => (ProfileMid ?? 0) * DragMultiple / LiftScale;
    public double InducedChords => InducedPerSpan * DragMultiple / LiftScale;
}

/// <summary>
/// One row of the Section document's compact Stations table (Ruling 125, mockup B and C): the same stations as the "Stations"
/// group (solved, governing and shown), as columns. <paramref name="NotMeasured"/> carries COPY-384 for a station not solved at 400 panels.
/// </summary>
public sealed record StationTableRow(double Eta, string Station, string AlphaEff, string CpMin, string Cavitation, bool IsShown, string? NotMeasured);

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
        CancellationToken cancellation = default, Units units = Units.Metric)
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
        SectionForces? forces = ForcesAt(run, station.Eta, source, units, cancellation);
        groups.Add(ForceGroup(forces, station, depthSet, units));

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
        screen.Add(R(Labels.VcritLabel, cav.CriticalSpeed is { } vc ? N(Labels.Speed(vc, units), "0.##") : none, cav.CriticalSpeed is null ? null : Labels.SpeedUnit(units)));
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
            if (tier.PolarConsistency?.Strips.FirstOrDefault(s => Math.Abs(s.Eta - station.Eta) < 1e-9) is { ClDelta: { } delta } consistency)
                polar.Add(R(Labels.DeltaVsLattice, N(delta, "0.###"), note: consistency.SectionUnvalidated
                    ? Labels.DeltaVsLatticeNote + " · " + Labels.BracketOutsideFamily : Labels.DeltaVsLatticeNote));
            groups.Add(new("Polar", polar));
        }

        var charts = Charts(run, tier, station, section, source, revision, cancellation, units);
        var table = tier.Stations.Where(s => s.PanelUnderread is not null || s.Eta == station.Eta || s.Eta == governing.Eta)
            .OrderBy(s => s.Eta).Select(s => new StationTableRow(s.Eta, "η " + N(s.Eta, "0.###"), N(s.AlphaEffDeg, "0.00"),
                N(-s.Estimate.Panel.CpMin, "0.###"), CavitationWord(s.Cavitation.State), s.Eta == station.Eta,
                s.PanelUnderread is null ? Labels.UnderreadNotMeasured : null)).ToArray();
        return new(station.Eta, isGoverning, name, groups, charts, solves, table, Profile(station, units, forces));
    }

    // The leading word of the approved cavitation sentences (COPY-301 to COPY-303): "Clear", "Inside the margin", "Possible".
    private static string CavitationWord(CavitationState state)
    {
        string sentence = state switch
        {
            CavitationState.Clear => Labels.CavClear,
            CavitationState.InsideMargin => Labels.CavInside,
            CavitationState.PossibleAboveCritical => Labels.CavPossible,
            _ => "Unavailable"
        };
        int dash = sentence.IndexOf(" — ", StringComparison.Ordinal);
        return dash < 0 ? sentence : sentence[..dash];
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
                : Labels.BracketOutsideFamily);
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
        (PanelCp at, string side) = CpMinPanel(panel);
        return (at.X, side);
    }

    private static (PanelCp At, string Side) CpMinPanel(PanelResult panel)
    {
        // Upper runs TE to LE and Lower LE to TE; CpMin omits the three panels nearest the TE on each side.
        PanelCp best = default;
        double lowest = double.PositiveInfinity;
        string side = "upper";
        int skip = PanelMethod.CpMinTrailingEdgePanelsPerSide;
        for (int i = skip; i < panel.Upper.Count; i++)
            if (panel.Upper[i].Cp < lowest) { lowest = panel.Upper[i].Cp; best = panel.Upper[i]; side = "upper"; }
        for (int i = 0; i < panel.Lower.Count - skip; i++)
            if (panel.Lower[i].Cp < lowest) { lowest = panel.Lower[i].Cp; best = panel.Lower[i]; side = "lower"; }
        return (best, side);
    }

    private static SectionProfile Profile(SectionStationResult station, Units units, SectionForces? forces)
    {
        PanelResult panel = station.Estimate.Panel;
        PanelCp[] outline = panel.Upper.Concat(panel.Lower).ToArray();
        (PanelCp at, string side) = CpMinPanel(panel);
        CavitationResult cav = station.Cavitation;
        string? line = cav is { Sigma: { } sigma, CpMin: { } cpMin, CriticalSpeed: { } vcrit } &&
            cav.State is CavitationState.Clear or CavitationState.InsideMargin or CavitationState.PossibleAboveCritical
            ? Labels.ProfileCavitation(sigma, cpMin, cav.State switch
            {
                CavitationState.Clear => "clear of",
                CavitationState.InsideMargin => "inside",
                _ => "at or past"
            }, 100 * cav.MarginFraction, vcrit, units)
            : null;
        return new(outline, at, side, outline.Min(p => p.Cp), outline.Max(p => p.Cp), Labels.SectionCaption(station.Eta),
            Labels.EstimatorChip + " · inviscid; no boundary layer", line, forces);
    }

    private static IReadOnlyList<ChartModel> Charts(AnalysisRun run, SectionTierResult tier, SectionStationResult station,
        NeuralFoilSection? section, byte[]? source, string? revision, CancellationToken cancellation, Units units)
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
            bool computable = fit.MaxResidual <= NeuralFoilPolarSource.MaxCstResidual &&
                station.Reynolds is >= NeuralFoilPolarSource.TrainingReynoldsMin and <= NeuralFoilPolarSource.TrainingReynoldsMax;
            NeuralFoilNetwork network = NeuralFoilNetwork.FromEmbedded();
            if (computable)
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

        charts.Add(Bucket(run, tier, station, source, cancellation, units));
        return charts;
    }

    private static ChartModel Bucket(AnalysisRun run, SectionTierResult tier, SectionStationResult station, byte[]? source,
        CancellationToken cancellation, Units units)
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
            vCrit.Add(new ChartPoint(r.Cl, Labels.Speed(Math.Sqrt(2 * numerator / (run.Water.Rho * -r.CpMin)), units)));
        }
        double sigma = numerator / (0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed);
        var s1 = new ChartPlot("σ required", "Cl", "σ required (−Cp_min)", false, [new("σ required", sigmaRequired, false, "dot", 0)],
            [new("operating σ", station.Estimate.Cl, sigma)]);
        var s2 = new ChartPlot("V_crit", "Cl", "V_crit (" + Labels.SpeedUnit(units) + ")", false, [new("V_crit", vCrit, false, "dot", 0)],
            [new("operating speed", station.Estimate.Cl, Labels.Speed(run.Op.Speed, units))]);
        return new("bucket", "Bucket", [s1, s2], Labels.BucketLegend + " · " + tier.Cavitation.ScreenText, label);
    }

    private static ResultRow R(string label, string value, string? unit = null, string? note = null) => new(label, value, unit, note);
    private static string N(double value, string format) => value.ToString(format, Inv);

    /// <summary>
    /// The strip force record of the shown station, derived on read from the stored strip and the source the run was solved on (DM7:
    /// nothing is stored). Null when the source is not held, a span edge is missing, or the strip has no width; the table then says why.
    /// </summary>
    private static SectionForces? ForcesAt(AnalysisRun run, double eta, byte[]? source, Units units, CancellationToken cancellation)
    {
        if (source is null || run.Outcome is not RunOutcome.Completed || run.Strips.Count == 0
            || run.Settings.SectionEtas is not { Count: > 0 } etas || run.Settings.SectionXs is not { Count: > 0 } xs) return null;
        StripLoad strip = run.Strips.MinBy(s => Math.Abs(Math.Abs(s.Eta) - eta))!;
        if (strip.YLow is not { } low || strip.YHigh is not { } high) return null;
        try
        {
            var wing = ProductWingMethod.Mirror(Placement.Sections(source, etas, xs, cancellation));
            (double x, double z) = VortexLattice.StripLeadingEdges(wing, [(low, high)])[0];
            return SectionForceModel.Compute(run, strip, x, z, units);
        }
        catch (ContractError) { return null; }
    }

    private const string NoForcesReason = "the section source is not held by this session";

    // The table of the strip's values, one model named on every row (Ruling 128 (2)); "Not modelled" last, as on Properties.
    private static ResultGroup ForceGroup(SectionForces? f, SectionStationResult station, bool depthSet, Units units)
    {
        if (f is null)
            return new(Labels.StripTableHeading, [R("Result", Labels.UnavailableBecause(NoForcesReason))]);
        string fu = Labels.ForcePerSpanUnit(units), mu = Labels.MomentPerSpanUnit(units);
        string profile = f.ProfileLow is { } low && f.ProfileHigh is { } high
            ? Labels.DragBand(low, high, units) : Labels.UnavailableBecause(f.ProfileUnavailable ?? Labels.NoPolar);
        bool flagged = f.ProfileFlags is not null;
        // COPY-328 always, then COPY-364 and COPY-316 verbatim when the polar flags them (mockup state C)
        string profileNote = string.Join(" · ", new[] { f.ProfileLow is null ? null : Labels.ProfileDragNote,
            StripFlags.Has(f.ProfileFlags, StripFlags.SectionUnvalidated) ? Labels.BracketOutsideFamily : null,
            f.LowConfidence ? Labels.LowConfidence(null) : null }.Where(part => part is not null));
        string total = f.ProfileLow is null ? Labels.UnavailableBecause(f.ProfileUnavailable ?? Labels.NoPolar) : Labels.Sig3(Labels.ForcePerSpan(f.TotalPerSpan, units));
        return new(Labels.StripTableHeading,
        [
            R(Labels.ClPanelRow, N(station.Estimate.Cl, "0.000")), R(Labels.CmPanelRow, N(station.Estimate.CmQuarter, "0.000")),
            R(Labels.ClLatticeRow, N(f.ClLattice, "0.000")),
            R(Labels.AlphaGeoRow, N(f.AlphaGeoDeg, "0.00"), "°"), R(Labels.AlphaEffRow, N(f.AlphaEffDeg, "0.00"), "°"),
            R(Labels.AlphaIRow, N(f.AlphaIDeg, "0.00"), "°"),
            R(Labels.XcpRow, f.Anchor == ForceAnchor.CentreOfPressure ? N(f.XcpOverC!.Value, "0.00") : f.XcpText,
                note: f.Anchor == ForceAnchor.QuarterChord ? Labels.CouplePlaceNote : null),
            R(Labels.LiftRow, Labels.Sig3(Labels.ForcePerSpan(f.LiftPerSpan, units)), fu),
            R(Labels.CoupleRow, Labels.Sig3(Labels.MomentPerSpan(f.CouplePerSpan, units)), mu),
            R(Labels.ProfileDragRow, flagged && f.ProfileLow is not null ? profile + " " + fu + " " + Labels.FlaggedSuffix : profile,
                f.ProfileLow is null || flagged ? null : fu, profileNote.Length > 0 ? profileNote : null),
            R(Labels.InducedDragRow, Labels.Sig3(Labels.ForcePerSpan(f.InducedPerSpan, units)), fu),
            R(Labels.TotalDragRow, total, f.ProfileLow is null ? null : fu),
            R("Not modelled", Labels.NotModelled(depthSet))
        ]);
    }
}

/// <summary>
/// The lattice strip's Lift and Drag on free-stream axes (Rulings 127, 128, 130). One model throughout: L′ is the strip lift of
/// <c>AnalysisProjection.StripDetails</c>, x_cp comes from the strip's own moment and normal force once the moment is moved from the
/// frame origin to the strip leading edge (docs/proof/sfv/model.md), the induced share is the lifting-line d′ = ½ρΓ(−w_T) whose strips
/// sum to the wing's induced drag, and the profile drag is the polar band. Pure; no lattice or polar numerics are touched.
/// </summary>
public static class SectionForceModel
{
    /// <summary>|Cl_local (lattice)| below this puts the arrows at c/4 and x_cp Undefined (Ruling 130).</summary>
    public const double AnchorClMin = 0.05;
    /// <summary>The largest strip lift draws this many chords long; the scale rounds up in the 1-2-5 series.</summary>
    public const double LiftArrowChords = 0.3;
    /// <summary>Rule for the drag multiple: the largest of 10, 5, 2, 1 at which no strip's total drag arrow is longer than this many chords.</summary>
    public const double DragArrowMaxChords = 0.15;
    private static readonly int[] Multiples = [10, 5, 2, 1];

    public static double RoundUp125(double value)
    {
        double exponent = Math.Pow(10, Math.Floor(Math.Log10(value))), m = value / exponent;
        return exponent * (m <= 1 + 1e-9 ? 1 : m <= 2 + 1e-9 ? 2 : m <= 5 + 1e-9 ? 5 : 10);
    }

    private static bool Parts(AnalysisRun run, StripLoad strip, out double lift, out double induced, out double? low, out double? high)
    {
        double width = Loads.StripWidth(run, strip);
        lift = induced = 0;
        low = high = null;
        if (!(width > 0)) return false;
        double a = VortexLattice.ToRadians(run.Op.AlphaDeg), q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
        lift = (-strip.Fx * Math.Sin(a) + strip.Fz * Math.Cos(a)) / width;
        induced = 0.5 * run.Water.Rho * strip.Gamma * -strip.DownwashTrefftz;
        if (strip.CdNcrit2.Value is { } cd2 && strip.CdNcrit4.Value is { } cd4)
        {
            low = q * strip.Chord * Math.Min(cd2, cd4);
            high = q * strip.Chord * Math.Max(cd2, cd4);
        }
        return true;
    }

    /// <summary>
    /// The lift scale (N/m for one chord of arrow) and the drag multiple, fixed per run. The scale is the largest strip |L′| over
    /// <see cref="LiftArrowChords"/>, rounded up in the 1-2-5 series in the unit shown, so the label's number is the rounded one.
    /// </summary>
    public static (double LiftScale, int DragMultiple) RunScale(AnalysisRun run, Units units)
    {
        double maxLift = 0;
        var totals = new List<double>();
        foreach (StripLoad strip in run.Strips)
        {
            if (!Parts(run, strip, out double lift, out double induced, out double? low, out double? high)) continue;
            maxLift = Math.Max(maxLift, Math.Abs(lift));
            totals.Add(Math.Abs(((low + high) / 2 ?? 0) + induced));
        }
        double shown = Labels.ForcePerSpan(maxLift, units);
        double scale = shown > 0 ? RoundUp125(shown / LiftArrowChords) / Labels.ForcePerSpan(1, units) : 1;
        int multiple = Multiples.Cast<int?>().FirstOrDefault(m => totals.All(t => t * m / scale <= DragArrowMaxChords)) ?? 1;
        return (scale, multiple);
    }

    /// <summary>
    /// The force record of <paramref name="strip"/>. (<paramref name="leadingX"/>, <paramref name="leadingZ"/>) is its leading edge in the
    /// frame the strip moments are about (the frame origin). Null when the strip has no width.
    /// </summary>
    public static SectionForces? Compute(AnalysisRun run, StripLoad strip, double leadingX, double leadingZ, Units units)
    {
        if (!Parts(run, strip, out double lift, out double induced, out double? low, out double? high)) return null;
        double width = Loads.StripWidth(run, strip), chord = strip.Chord;
        // M_LE = M_origin - r_LE x F (y component), per span; the lattice puts every bound segment on the strip's plane, so this is
        // -(x_cp - x_LE) Fz exactly, and the centre of pressure is the Fz-weighted chordwise position.
        double momentLe = (strip.My - (leadingZ * strip.Fx - leadingX * strip.Fz)) / width, normal = strip.Fz / width;
        double couple = momentLe + 0.25 * chord * normal;
        double? xcp = run.Settings.NChord >= 2 && normal != 0 && double.IsFinite(momentLe / normal) ? -momentLe / (normal * chord) : null;
        bool nearZero = Math.Abs(strip.ClLocal) < AnchorClMin;
        bool onCp = xcp is { } x && x >= 0 && x <= 1 && !nearZero;
        string text = onCp ? Labels.Number(xcp!.Value, "0.00") : nearZero ? Labels.XcpNearZeroLift : xcp is null ? "Undefined" : Labels.XcpOffSection;
        string? unavailable = low is null ? (strip.CdNcrit2.Value is null ? strip.CdNcrit2.UnavailableReason : strip.CdNcrit4.UnavailableReason) : null;
        string? flags = StripFlags.Join(strip.CdNcrit2.FlagCode, strip.CdNcrit4.FlagCode);
        (double scale, int multiple) = RunScale(run, units);
        return new SectionForces(strip.Eta, chord, strip.AlphaEff + strip.AlphaI, strip.AlphaEff, strip.AlphaI, strip.ClLocal, lift, couple, xcp,
            onCp ? ForceAnchor.CentreOfPressure : ForceAnchor.QuarterChord, text, low, high, flags, unavailable, induced, scale, multiple, units);
    }
}
