using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Track DX (docs/proof/dx/test-plan.md): the A3b Section tab, the A3c polar displays, the drag row and Find alpha, as pure data.
/// Ring: fast (the Analysis harness), each check under 0.5 s warm. Every assertion reads a projected cell, never a Labels constant,
/// except where a check names a Labels table itself.
/// </summary>
internal static class DxSectionTests
{
    internal static void Run()
    {
        Check("SectionProjection_CpPlot_SeriesAndMarker", CpPlot);
        Check("SectionProjection_EstimatorLabel_DepthAware", EstimatorLabel);
        Check("SectionProjection_EstimatorChip_Copy214", EstimatorChip);
        Check("SectionProjection_Values_ClCmAlphaL0PerSpan", Values);
        Check("SectionView_CpOnProfile_DrawsAndPinsVik", ProfileView);
        Check("Cavitation_Screen_ValueStateAndFixedString", ScreenValues);
        Check("Cavitation_GoverningStation_NamedWithDepth", GoverningLine);
        Check("Cavitation_PanelUnderread_MeasuredNotConstant", UnderreadMeasured);
        Check("Cavitation_Provisional_AboveTenPercent", ProvisionalBoundary);
        Check("Section_UnderreadNotMeasured_ShowsRatifiedText", NotMeasured);
        Check("Section_Cp_NoMethodString_Retired", CpRetired);
        Check("Section_CpUnavailable_NoProfileAndSolveFailed_Row10", Row10);
        Check("Section_StripCdNoPolar_Copy210", StripCdNoPolar);
        Check("Polar_TierChipAndLabels_Dxm3", PolarChipAndLabels);
        Check("Provenance_PolarMethodId_Shown", PolarMethodId);
        Check("Polar_Confidence_PresentNotRecordedLow", Confidence);
        Check("Polar_TrippedSurface_NotComputed", Tripped);
        Check("Drag_ProfileSources_BandBoundKeptApart", ProfileSources);
        Check("Drag_StripsMissingCd_NamesCountNoSubstitute", MissingCd);
        Check("Drag_WingOnly_OneRow_Ruling109", WingOnly);
        Check("Drag_TotalNoProfile_StillNamesProfile", TotalNoProfile);
        Check("Drag_CdNonPositive_Undefined_Copy230", CdNonPositive);
        Check("FindAlpha_Dialog_ResultAndBasisRows", FindFound);
        Check("FindAlpha_NoRoot_FiveReasons", FindNoRoot);
        Check("Labels_Dx_EveryRowResolvesOnce", LabelsMatchDesign);
        Check("Labels_ReasonCodes_AllApprovedTextNoRawCode", ReasonCodesComplete);
    }

    /// <summary>Readiness ring: one run per registered code, about 1 s.</summary>
    internal static void RunReadiness()
    {
        // moved from the fast ring by C-5 (each measured 0.43-0.82 s under load; they build several views or sweep the polar)
        Check("Cavitation_UnavailableAndUndefined_ReasonRows", ReasonRows);
        Check("Section_ShownStation_SelectedElseGoverning_Dxm9", ShownStation);
        Check("Polar_Bracket_FlaggedPerAxis_Dxm2Dxm8", BracketAxes);
        Check("Polar_OutsideTrainingRange_UnavailableNoNumber_Dxm2", TrainingRange);
        Check("Cavitation_MarginStates_ClearInsidePossible", MarginStates);
        Check("Charts_TransitionAndBucket_Overlays", TransitionAndBucket);
        Check("Polar_Chart_BandAndAlphaEffMarker", PolarChart);
        Check("Polar_CstResidual_ShownAndLimit", CstResidual);
        Check("Strips_PolarRe_InsideOutsideNotExtrapolated", StripRe);
        Check("Projection_NoRawAnaCodeInAnyCell", NoRawCode);
        Check("Polar_NonNaca0012InsideTrainingRange_ComputedAndFlagged_Ruling117", NonNacaComputedAndFlagged);
    }

    // ---- fixture: the default foil at four stations, one tier, one run with the polar installed ----

    private sealed record Fx(byte[] Source, AnalysisRun Run, SectionTierResult Tier, SectionStationResult Gov, SectionStationResult Other);

    private static readonly Lazy<Fx> Fixed = new(() =>
    {
        byte[] source = FoilSource.NewDefault();
        OperatingPoint op = Fixture.Op(3);
        SectionTierResult tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], [], op, Fixture.Salt);
        AnalysisRun run = RunWith(null, op);
        SectionStationResult gov = tier.Stations.MinBy(s => Math.Abs(s.Eta - tier.GoverningEta))!;
        return new(source, run, tier, gov, tier.Stations.First(s => s != gov));
    });

    private static Fx F => Fixed.Value;

    private static AnalysisRun RunWith(Func<StripLoad, StripLoad>? change, OperatingPoint? op = null, bool polar = true)
    {
        AnalysisRun run = ProjectionTests.Data(change).Run;
        RunSettings settings = polar ? run.Settings with { Polar = new RunPolar(NeuralFoilPolarSource.Method.Id, NeuralFoilPolarSource.Method.Version, "xxxlarge") }
            : run.Settings;
        return ProjectionTests.Rehash(run with { Settings = settings, Op = op ?? run.Op, Water = op is null ? run.Water : Fixture.Salt });
    }

    private static SectionView Build(SectionTierResult? tier = null, double? selected = null, SectionDisplay.UnderreadSeam? seam = null,
        AnalysisRun? run = null, byte[]? source = null) =>
        SectionDisplay.Build(run ?? F.Run, tier ?? F.Tier, source ?? F.Source, selected, "r1", seam);

    private static ResultRow Cell(SectionView view, string group, string label) =>
        view.Groups.Single(g => g.Title == group).Rows.Single(r => r.Label == label);

    private static readonly Lazy<AnalysisRun> LiteRun = new(() => RunWith(null, F.Run.Op, polar: false));

    private static SectionView Lite(SectionTierResult? tier = null, double? selected = null, SectionDisplay.UnderreadSeam? seam = null, AnalysisRun? run = null) =>
        SectionDisplay.Build(run ?? LiteRun.Value, tier ?? F.Tier, F.Source, selected, "r1", seam);

    private static SectionTierResult WithCavitation(CavitationResult cav) => F.Tier with { Cavitation = cav };

    private static SectionTierResult WithStation(SectionStationResult from, SectionStationResult to) =>
        F.Tier with { Stations = F.Tier.Stations.Select(s => s == from ? to : s).ToArray() };

    private static string Text(SectionView v) => string.Join("\n", v.Groups.SelectMany(g => g.Rows).SelectMany(r => new[] { r.Label, r.Value, r.Unit, r.Note })
        .Concat(v.Charts.SelectMany(c => new[] { c.Title, c.Legend, c.Label, c.Unavailable }.Concat(c.Plots.SelectMany(p =>
            new[] { p.Title, p.XTitle, p.YTitle }.Concat(p.Series.Select(s => s.Name)).Concat(p.Markers.Select(m => m.Name))))))
        .Append(v.StationName).Where(s => !string.IsNullOrEmpty(s)));

    private static string Text(AnalysisViewModel v) => string.Join("\n", v.Groups.SelectMany(g => g.Rows).SelectMany(r => new[] { r.Label, r.Value, r.Unit, r.Note })
        .Concat(v.StripDetails.SelectMany(d => d.Rows).SelectMany(r => new[] { r.Label, r.Value, r.Unit, r.Note }))
        .Concat(v.Layers.SelectMany(l => new[] { l.Title, l.Legend, l.Note })).Append(v.Banner).Append(v.ErrorCard).Append(v.StatusText)
        .Where(s => !string.IsNullOrEmpty(s)));

    private static readonly Regex Code = new("(?<![A-Za-z-])ANA-[A-Z0-9-]+", RegexOptions.CultureInvariant);

    // ---- A3b: estimator, Cp, cavitation screen ----

    private static void CpPlot()
    {
        SectionView v = Build();
        ChartPlot plot = v.Charts.Single(c => c.Id == "cp").Plots.Single();
        Equal(2, plot.Series.Count, "upper and lower");
        Equal(false, plot.Series[0].Dashed, "upper solid");
        Equal(true, plot.Series[1].Dashed, "lower dashed");
        Equal(true, plot.InvertY, "suction up");
        Equal(F.Gov.Estimate.Panel.Upper.Count + F.Gov.Estimate.Panel.Lower.Count, F.Gov.Estimate.Panel.StationCount, "series hold every station");
        Equal(F.Gov.Estimate.Panel.CpMin, plot.Markers.Single(m => m.Name == "Cp_min").Y!.Value, "marker is the tier's Cp_min");
        ResultRow where = Cell(v, "Estimator", "−Cp_min");
        Equal(true, where.Note!.Contains("three trailing-edge panels per side excluded") && where.Note.Contains(F.Gov.Estimate.Panel.StationCount + " stations"), "N and TE exclusion named");
        Equal(true, Regex.IsMatch(where.Note, @"at x/c [0-9.]+ on the (upper|lower) surface"), "side and x/c named");
    }

    private static void EstimatorLabel()
    {
        Equal(true, Cell(Build(), "Estimator", "Tier").Note!.Contains("deep water"), "depth set reads deep water");
        AnalysisRun noDepth = RunWith(null, F.Run.Op with { HRef = null }, polar: false);
        SectionView v = Build(run: noDepth);
        Equal(true, Cell(v, "Estimator", "Tier").Note!.Contains("free surface not modelled"), "depth unset says so");
        Equal(false, Text(v).Contains("deep water"), "deep water appears nowhere");
    }

    private static void EstimatorChip() => Equal("Estimator · local calculation", Cell(Build(), "Estimator", "Tier").Value);

    private static void Values()
    {
        SectionView v = Build();
        Equal(F.Gov.Estimate.Cl.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Cell(v, "Estimator", "cl (panel)").Value, "cl");
        Equal(F.Gov.Estimate.AlphaL0Deg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Cell(v, "Estimator", "α_L0 (panel)").Value, "α_L0");
        Equal(true, Cell(v, "Estimator", "Cm c/4").Value.Length > 0, "Cm");
        ResultRow bound = Cell(v, "Estimator", "Cd (turbulent bound)");
        Equal(true, bound.Note!.Contains("a bound, not a polar value"), "cd bound note");
        Equal(true, v.Charts.Single(c => c.Id == "cp").Legend.StartsWith("Cp · vik pinned at 0"), "legend");
    }

    private static void ProfileView()
    {
        ChartPlot plot = Build().Charts.Single(c => c.Id == "profile").Plots.Single();
        ChartSeries upper = plot.Series.Single(s => s.Name == "upper"), comb = plot.Series.Single(s => s.Name == "Cp upper");
        Equal(upper.Points.Count, comb.Points.Count, "a comb point per contour point");
        ChartPoint last = upper.Points[^1], combLast = comb.Points[^1];
        Equal(true, Math.Abs(last.X - combLast.X) < 1e-12, "comb shares the contour's x");
        Equal(true, plot.Markers.Any(m => m.Name == "Cp_min"), "Cp_min marker");
        Equal(true, upper.Points.Zip(comb.Points).Any(p => Math.Abs(p.First.Y - p.Second.Y) > 1e-4), "Cp offsets the comb from the contour");
        Equal(false, plot.InvertY, "the profile is drawn upright");
    }

    private static void ScreenValues()
    {
        CavitationResult c = F.Tier.Cavitation;
        SectionView v = Build();
        Equal(c.Sigma!.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Cell(v, "Cavitation", "σ (cavitation number)").Value, "σ");
        Equal((-c.CpMin!.Value).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Cell(v, "Cavitation", "−Cp_min").Value, "−Cp_min");
        Equal(true, Cell(v, "Cavitation", "V_crit (inception speed)").Value.Length > 0, "V_crit");
        Equal(c.ScreenText, Cell(v, "Cavitation", "Screen").Value, "fixed string");
        Equal(true, c.ScreenText.Contains(c.StationCount + " stations"), "names N");
        Equal("15 % margin — practitioner assumption, not sourced", Cell(v, "Cavitation", "Margin").Note, "margin label");
        Equal("15", Cell(v, "Cavitation", "Margin").Value, "margin 15");
    }

    private static CavitationResult Screen(double sigmaOverSuction)
    {
        // σ = k × (−Cp_min) by choosing the speed that gives it at one fixed depth.
        CavitationResult g = F.Tier.Cavitation;
        double suction = -g.CpMin!.Value, p = F.Run.Op.PAtm + F.Run.Water.Rho * OperatingPoints.Gravity * g.GoverningDepth!.Value - F.Run.Water.Pv;
        double speed = Math.Sqrt(2 * p / (F.Run.Water.Rho * sigmaOverSuction * suction));
        return Cavitation.Screen(g.CpMin!.Value, g.StationCount, g.GoverningDepth, speed, F.Run.Water.Rho, F.Run.Op.PAtm, F.Run.Water.Pv, g.GoverningStation!);
    }

    private static string Sentence(CavitationResult c) => Cell(Build(WithCavitation(c)), "Cavitation", "Cavitation screen").Value;

    private static void MarginStates()
    {
        Equal("Clear — σ is above −Cp_min plus the margin", Sentence(Screen(1.20)), "clear");
        Equal("Inside the margin — σ is above −Cp_min but within the margin", Sentence(Screen(1.07)), "inside");
        Equal("Possible — σ is at or below −Cp_min; speed is above V_crit", Sentence(Screen(0.95)), "possible");
        Equal("Possible — σ is at or below −Cp_min; speed is above V_crit", Sentence(Screen(1.0 - 1e-9)), "σ = −Cp_min boundary");
        Equal("Inside the margin — σ is above −Cp_min but within the margin", Sentence(Screen(1.15 - 1e-9)), "margin edge");
    }

    private static void GoverningLine()
    {
        double depth = F.Tier.Cavitation.GoverningDepth!.Value;
        string expected = $"Governing station: η {F.Tier.GoverningEta.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} · depth {depth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} m · smallest σ / (−Cp_min) of {F.Tier.Stations.Count} stations";
        Equal(expected, Cell(Build(), "Cavitation", "Governing station").Value, "governing, unselected");
        Equal(expected, Cell(Build(selected: F.Other.Eta), "Cavitation", "Governing station").Value, "governing, another station shown");
    }

    private static void ReasonRows()
    {
        CavitationResult g = F.Tier.Cavitation;
        double depth = g.GoverningDepth!.Value, v = F.Run.Op.Speed, rho = F.Run.Water.Rho, pa = F.Run.Op.PAtm;
        CavitationResult S(double cp, double? d, double? pv) => Cavitation.Screen(cp, g.StationCount, d, v, rho, pa, pv, "η 0.5");
        (CavitationResult Screen, string Row)[] cases =
        [
            (S(-1, null, F.Run.Water.Pv), "Unavailable — depth not set"),
            (S(-1, depth, null), "Unavailable — vapour pressure missing"),
            (S(-1, -0.1, F.Run.Water.Pv), "Unavailable — local station is surface piercing"),
            (S(-1, depth, -5), "Unavailable — water is invalid"),
            (S(-1, double.NaN, F.Run.Water.Pv), "Unavailable — local depth is invalid"),
            (S(0.3, depth, F.Run.Water.Pv), "Undefined — −Cp_min ≤ 0"),
            (S(-1, depth, 1e9), "Undefined — static pressure does not exceed vapour pressure"),
        ];
        foreach ((CavitationResult screen, string row) in cases)
        {
            SectionView view = Build(WithCavitation(screen));
            Equal(row, Cell(view, "Cavitation", "Cavitation screen").Value, row);
            Equal(row, Cell(view, "Cavitation", "σ (cavitation number)").Value, "σ cell carries the reason, no number: " + row);
            Equal(false, Regex.IsMatch(Cell(view, "Cavitation", "V_crit (inception speed)").Value, @"^[0-9]"), "no V_crit number: " + row);
        }
        Equal(true, Cell(Build(WithCavitation(cases[0].Screen)), "Estimator", "cl (panel)").Value.Length > 0, "Cp still shows without depth");
    }

    private static void UnderreadMeasured()
    {
        SectionStationResult measured = F.Gov with { PanelUnderread = 0.0371 };
        SectionView v = Build(WithStation(F.Gov, measured));
        Equal("3.71", Cell(v, "Under-read", "Cp_min under-read, 200 vs 400 panels").Value, "measured value");
        Equal(false, Text(v).Contains("1.61"), "the 1.61 % constant appears nowhere");
    }

    private static void ProvisionalBoundary()
    {
        bool Shows(double u) => Build(WithStation(F.Gov, F.Gov with { PanelUnderread = u })).Groups.Single(g => g.Title == "Under-read").Rows.Any(r => r.Label == "Provisional");
        Equal(true, Shows(0.124), "12.4 %");
        Equal(false, Shows(0.0999), "9.99 %");
        Equal(false, Shows(0.10), "exactly 10 %");
        SectionView v = Build(WithStation(F.Gov, F.Gov with { PanelUnderread = 0.124 }));
        Equal("Provisional — Cp_min under-read at this station is above 10 % (200 vs 400 panels)", Cell(v, "Under-read", "Provisional").Value);
    }

    private static void NotMeasured()
    {
        const string text = "Cp_min under-read not measured at this station (200 panels only)";
        SectionStationResult unmeasured = F.Other with { PanelUnderread = null };
        SectionTierResult tier = WithStation(F.Other, unmeasured);
        SectionView shown = Build(tier, selected: unmeasured.Eta);
        Equal(text, Cell(shown, "Under-read", "Cp_min under-read").Value, "shown but not yet solved: exact text");
        Equal(true, shown.Groups.Single(g => g.Title == "Stations").Rows.Any(r => r.Note == text), "the Stations table names it");
        SectionStationResult solved = F.Gov with { PanelUnderread = 0.0371 };
        SectionView beside = Build(WithStation(F.Gov, solved).WithStationSwap(F.Other, unmeasured));
        ResultRow[] stations = beside.Groups.Single(g => g.Title == "Stations").Rows.ToArray();
        Equal(true, stations.Any(r => r.Note!.Contains("3.71 %")), "a 400-solved station shows its measured value");
        Equal(true, stations.Any(r => r.Note == text), "beside a station that shows the exact not-measured text");
        Equal(false, text.Contains("Provisional") || stations.Any(r => r.Note == text && r.Note.Contains("Provisional")), "never COPY-312");
    }

    private static SectionTierResult WithStationSwap(this SectionTierResult tier, SectionStationResult from, SectionStationResult to) =>
        tier with { Stations = tier.Stations.Select(s => s == from ? to : s).ToArray() };

    private static void CpRetired()
    {
        AnalysisViewModel with = ProjectionTests.View(F.Run, new ProjectionContext(SectionTier: F.Tier));
        ResultRow cp = ProjectionTests.Cell(with, "Section (2D)", "Cp_min");
        Equal(true, double.TryParse(cp.Value, System.Globalization.CultureInfo.InvariantCulture, out double value) && value == Math.Round(F.Gov.Estimate.Panel.CpMin, 3), "Cp_min shows the value when a panel result exists");
        Equal(false, Text(with).Contains("no section Cp method"), "COPY-212 absent from the projection");
        Equal(false, Text(ProjectionTests.View(F.Run, new ProjectionContext())).Contains("no section Cp method"), "and absent without a result");
    }

    /// <summary>Ruling 117. The example foil's section is not NACA 0012 and lies inside the training range: strips get a cd, the wing drag
    /// band shows, and every value it feeds carries COPY-364. Ring: readiness, a product run (about 1 s).</summary>
    private static void NonNacaComputedAndFlagged()
    {
        using var session = new AuthoringSession();
        session.Open(File.ReadAllBytes(Path.Combine(StripFixtureTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil")), Fixture.Id(), true);
        RunSettings settings = Settings.Default with { NSpanPerHalf = 4, NChord = 2,
            SectionEtas = [0d, 0.5, 1d], SectionXs = Settings.ChordXs(2, "cosine") };
        var method = new ProductWingMethod(settings);
        AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, method), Fixture.Op(3));
        Equal(true, run.Strips.Any(strip => strip.CdNcrit2.Value is > 0), "a strip cd is computed, not refused");
        Equal(true, run.Strips.Where(strip => strip.CdNcrit2.Value is not null)
            .All(strip => strip.CdNcrit2.FlagCode?.Contains("ANA-POLAR-SECTION-UNVALIDATED", StringComparison.Ordinal) == true), "every computed strip cd carries the family flag");
        Equal(true, run.Strips.All(strip => strip.CdNcrit2.UnavailableReason != "ANA-POLAR-SECTION-UNVALIDATED"), "no strip is refused for its family");
        AnalysisViewModel view = AnalysisProjection.Build(run, Freshness.Current(session.Snapshot(), Fixture.Salt, Fixture.Op(3), method.Method, method.Settings),
            Units.Metric, new ProjectionContext(Source: session.AcceptedSourceOf(run.Inputs.AcceptedId)));
        ResultRow wing = ProjectionTests.Cell(view, "Loads", "Drag (Wing only)");
        Equal(true, wing.Value.Contains('–'), "the wing drag band has a value: " + wing.Value);
        Equal(true, wing.Note!.Contains("were not validated (NACA 0012 only). Computed, not validated."), "the band carries COPY-364");
        Equal(true, view.StripDetails[0].Rows.Single(r => r.Label == "cd (profile)").Note!.Contains("were not validated (NACA 0012 only)"), "the strip cd carries COPY-364");
    }

    private static void Row10()
    {
        string none = ProjectionTests.Cell(ProjectionTests.View(F.Run, new ProjectionContext()), "Section (2D)", "Cp_min").Value;
        string failed = ProjectionTests.Cell(ProjectionTests.View(F.Run, new ProjectionContext(SectionFailureCode: "ANA-PANEL-GEOMETRY")), "Section (2D)", "Cp_min").Value;
        Equal("Unavailable — no accepted section profile at this station", none, "no accepted profile");
        Equal("Unavailable — the panel solve failed at this station. Evaluate again.", failed, "panel solve failed");
        Equal(true, none != failed, "the two texts differ");
        foreach (string text in new[] { none, failed }) Equal(false, Code.IsMatch(text) || Regex.IsMatch(text, @"[0-9]"), "no number, no raw code: " + text);
    }

    private static void StripCdNoPolar()
    {
        AnalysisViewModel v = ProjectionTests.View(RunWith(null, polar: false));
        StripDetail strip = v.StripDetails[0];
        Equal("Unavailable — no polar method installed", strip.Rows.Single(r => r.Label == "cd (profile)").Value, "cd");
        Equal("Unavailable — no polar method installed", strip.Rows.Single(r => r.Label == "Polar Re range").Value, "Re range");
        Equal(false, strip.Rows.Single(r => r.Label == "Polar Re range").Value.Contains("inside"), "never inside");
    }

    private static void ShownStation()
    {
        Equal(true, F.Other.Eta != F.Gov.Eta, "the shown station is not the governing one");
        int calls = 0; (double Eta, double Alpha, double Re) seen = default;
        SectionStationResult unsolved = F.Other with { PanelUnderread = null };
        SectionTierResult tier = WithStation(F.Other, unsolved);
        SectionDisplay.UnderreadSeam spy = (_, eta, alpha, re) => { calls++; seen = (eta, alpha, re); return 0.124; };
        SectionView plain = Build(tier, selected: unsolved.Eta), demand = Build(tier, selected: unsolved.Eta, seam: spy);
        Equal(1, calls, "UnderreadAt called once");
        Equal((unsolved.Eta, unsolved.AlphaEffDeg, unsolved.Reynolds), seen, "with the shown station's own η, α_eff and Re");
        Equal(string.Join("|", plain.Groups.Single(g => g.Title == "Cavitation").Rows.Select(r => r.Value + r.Note)),
            string.Join("|", demand.Groups.Single(g => g.Title == "Cavitation").Rows.Select(r => r.Value + r.Note)), "cavitation rows byte-equal with and without the call");
        Equal(tier.Cavitation, tier.Cavitation, "the screen record is untouched");
        Equal(true, demand.Groups.Single(g => g.Title == "Under-read").Rows.Any(r => r.Label == "Provisional"), "above 10 % gives COPY-312 on the shown station");
        Equal(false, plain.Groups.Single(g => g.Title == "Under-read").Rows.Any(r => r.Label == "Provisional"), "and not without the measurement");
        Equal(true, demand.Groups.Single(g => g.Title == "Stations").Rows.Count(r => r.Note!.Contains("Provisional")) == 0, "no other station gets it");
        calls = 0;
        Build(WithStation(F.Other, F.Other with { PanelUnderread = 0.02 }), selected: F.Other.Eta, seam: spy);
        Equal(0, calls, "no second call when the shown station is already 400-solved");
        Equal(true, Build().IsGoverning && !Build(selected: F.Other.Eta).IsGoverning, "governing fallback versus selected");
        Equal(true, Cell(Build(), "Station", "Station").Value.EndsWith("governing cavitation station") &&
            Cell(Build(selected: F.Other.Eta), "Station", "Station").Value.EndsWith("selected strip"), "both are named");
    }

    // ---- A3c: polar ----

    private static void PolarChart()
    {
        ChartModel chart = Build().Charts.Single(c => c.Id == "polar");
        Equal(2, chart.Plots.Count, "cl and cd");
        ChartPlot cd = chart.Plots[1];
        Equal(13, cd.Series[0].Points.Count, "−6° and 6° inclusive");
        Equal(-6.0, cd.Series[0].Points[0].X, "lower edge");
        Equal(6.0, cd.Series[0].Points[^1].X, "upper edge");
        Equal(true, cd.Series.All(s => s.Points.All(p => p.X is >= -6 and <= 6)), "nothing outside the bracket is drawn");
        Equal(false, cd.Series[0].Dashed, "Ncrit 4 solid");
        Equal(true, cd.Series[1].Dashed, "Ncrit 2 dashed");
        Equal(true, cd.Band is not null, "cd band between the two");
        Equal(F.Gov.AlphaEffDeg, cd.Markers.Single(m => m.Name == "α_eff").X, "α_eff marker");
        Equal(true, chart.Legend.Contains("nothing is extrapolated"), "legend");
    }

    private static void PolarChipAndLabels()
    {
        ResultRow tier = Cell(Build(), "Polar", "Tier");
        Equal("Polar · local calculation", tier.Value, "chip");
        Equal(true, tier.Note!.Contains("XFOIL-class surrogate; accuracy relative to XFOIL, not experiment") &&
            tier.Note.Contains("surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only"), "COPY-66 and the surrogate label");
    }

    private static void PolarMethodId()
    {
        AnalysisViewModel v = ProjectionTests.View(F.Run);
        Equal(NeuralFoilPolarSource.Method.Id, ProjectionTests.Cell(v, "Provenance", "Polar method").Value, "polar method id");
        Equal(true, ProjectionTests.Cell(v, "Provenance", "Polar method").Value.StartsWith("NeuralFoil-0.3.2/xxxlarge/"), "id form");
        Equal("Unavailable — no polar method installed", ProjectionTests.Cell(ProjectionTests.View(RunWith(null, polar: false)), "Provenance", "Polar method").Value, "none installed");
    }

    private static SectionTierResult WithPolar(double? confidence, bool low, double max = 1.09e-4, double rms = 2.93e-5)
    {
        var sample = new PolarSample("p", NeuralFoilPolarSource.Method.Id, NeuralFoilPolarSource.Method.Version, 5e5, 2, "clean", 2, "w",
            0.3, 0.01, -0.02, 0.4, 0.5, null, null, confidence, true);
        var polar = new PolarResult(sample, [], low, rms, max);
        return F.Tier with { PolarNcrit2 = polar, PolarNcrit4 = polar };
    }

    private static void Confidence()
    {
        ResultRow Row(SectionTierResult t) => ProjectionTests.Cell(ProjectionTests.View(F.Run, new ProjectionContext(SectionTier: t)), "Section (2D)", "analysis_confidence");
        Equal("analysis_confidence 0.97 · advisory, not an error bar", Row(WithPolar(0.97, false)).Value, "present");
        Equal("analysis_confidence: not recorded", Row(WithPolar(null, false)).Value, "not recorded");
        Equal(true, Row(WithPolar(0.31, true)).Note!.Contains("Low confidence — analysis_confidence 0.31 is below 0.5. Computed and flagged, never refused; not an error bar."), "low advisory");
        Equal("analysis_confidence 0.31 · advisory, not an error bar", Row(WithPolar(0.31, true)).Value, "the value is still shown, never refused");
        Equal(null, NeuralFoilPolarSource.ConfidenceWarning(0.5), "0.5 exactly is not low");
        Equal(true, NeuralFoilPolarSource.ConfidenceWarning(0.499) is not null, "0.499 is low");
    }

    private static void CstResidual()
    {
        ProjectionContext ctx = new(SectionTier: WithPolar(0.97, false));
        Equal("CST fit residual: max 1.09 × 10⁻⁴ c · RMS 2.93 × 10⁻⁵ c (shape residual, not an aerodynamic error)",
            ProjectionTests.Cell(ProjectionTests.View(F.Run, ctx), "Section (2D)", "CST residual").Value, "residual");
        // a section the fit cannot represent: the point is Unavailable with the limit named, and no number is plotted
        SectionView thick = Build(source: SectionSeamTests.ThicknessSource(_ => 0.45));
        string row = Cell(thick, "Polar", "Ncrit 4").Value;
        Equal(true, row.StartsWith("Unavailable — section fit residual") && row.EndsWith("exceeds the limit 3.6 × 10⁻⁴ c"), "over the limit: " + row);
        Equal(true, thick.Charts.Single(c => c.Id == "polar").Plots.All(p => p.Series.All(s => s.Points.Count == 0)), "no polar number is drawn");
    }

    private static void BracketAxes()
    {
        string Row(SectionStationResult s, SectionTierResult t, byte[]? source = null) => Cell(Build(t, s.Eta, source: source), "Polar", "Polar bracket:").Value;
        SectionStationResult at(double alpha, double re) => F.Other with { AlphaEffDeg = alpha, Reynolds = re, PanelUnderread = 0.02 };
        string inside = "Inside the validated bracket (α −6° to 6°, Re 2 × 10⁵ to 10⁶, Ncrit 2, 4, 9, NACA 0012 family)";
        foreach ((double a, double re) in new[] { (6.0, 5e5), (-6.0, 5e5), (3.0, 2e5), (3.0, 1e6) })
            Equal(inside, Row(at(a, re), WithStation(F.Other, at(a, re))), $"inside at α {a}, Re {re}");
        Equal("Outside the validated bracket — α 7.50° is beyond ±6°. Computed, not validated.", Row(at(7.5, 5e5), WithStation(F.Other, at(7.5, 5e5))), "α axis");
        Equal("Outside the validated bracket — α 6.01° is beyond ±6°. Computed, not validated.", Row(at(6.01, 5e5), WithStation(F.Other, at(6.01, 5e5))), "α just past the edge");
        Equal("Outside the validated bracket — Re 1.70 × 10⁵ is beyond 2 × 10⁵ to 10⁶. Computed, not validated.", Row(at(3, 1.7e5), WithStation(F.Other, at(3, 1.7e5))), "Re axis");
        string family = Row(F.Other, F.Tier, SectionSeamTests.ThicknessSource(_ => 0.06));
        Equal(true, family.StartsWith("Outside the validated bracket — ") && family.EndsWith("sections were not validated (NACA 0012 only). Computed, not validated."), "family axis: " + family);
        // the attached-flow envelope verdict stays beside it, unchanged
        ResultRow verdict = new("Envelope (this strip)", "Outside the method envelope at this strip", null, null);
        SectionView beside = SectionDisplay.Build(F.Run, F.Tier, F.Source, F.Other.Eta, "r1", null, [verdict]);
        Equal("Outside the method envelope at this strip", Cell(beside, "Polar", "Envelope (this strip)").Value, "verdict row unchanged");
    }

    private static void TrainingRange()
    {
        string Row(double alpha, double re) { SectionStationResult s = F.Other with { AlphaEffDeg = alpha, Reynolds = re, PanelUnderread = 0.02 }; return Cell(Build(WithStation(F.Other, s), s.Eta), "Polar", "Ncrit 4").Value; }
        Equal("Unavailable — α 31.00° is outside the surrogate’s training range (−27.9° to 28.6°)", Row(31, 5e5), "α 31");
        Equal(true, Row(28.6, 5e5).StartsWith("cl "), "28.6° is the edge and is computed");
        Equal(true, Row(28.7, 5e5).StartsWith("Unavailable — α 28.70°"), "28.7° is outside");
        Equal(true, Row(-27.9, 5e5).StartsWith("cl "), "−27.9° is computed");
        Equal("Unavailable — Re 8.00 × 10¹ is outside the surrogate’s training range (1 × 10² to 1 × 10¹⁰)", Row(3, 80), "Re 80");
        Equal(true, Row(3, 100).StartsWith("cl "), "Re 100 is the edge");
    }

    private static void StripRe()
    {
        Equal("Re_local 5.00 × 10⁵ inside 2.00 × 10⁵ to 1.00 × 10⁶",
            ProjectionTests.View(RunWith(s => s with { CdNcrit2 = new StripValue(0.01, null), CdNcrit4 = new StripValue(0.008, null) })).StripDetails[0].Rows.Single(r => r.Label == "Polar Re range").Value, "inside");
        AnalysisViewModel outside = ProjectionTests.View(RunWith(s => s with { ReLocal = 1.7e5, CdNcrit2 = new StripValue(null, "ANA-POLAR-RE-OUTSIDE"), CdNcrit4 = new StripValue(null, "ANA-POLAR-RE-OUTSIDE") }));
        Equal("Re_local 1.70 × 10⁵ outside the polar’s Re range 2.00 × 10⁵ to 1.00 × 10⁶ — cd not extrapolated", outside.StripDetails[0].Rows.Single(r => r.Label == "Polar Re range").Value, "outside");
        Equal(true, outside.StripDetails[0].Rows.Single(r => r.Label == "cd (profile)").Value.StartsWith("Unavailable"), "cd is not extrapolated");
        var consistency = new PolarConsistencyResult(0.9, [new PolarConsistencyStrip(F.Gov.Eta, 1.0, 0.034, "ANA-POLAR-CONSISTENCY-JUDGED")]);
        ResultRow delta = Cell(Build(F.Tier with { PolarConsistency = consistency }), "Polar", "Δ vs lattice Cl_local");
        Equal("0.034", delta.Value, "Δ value");
        Equal("polar cl at α_eff against the lattice, a per-strip consistency check", delta.Note, "Δ note");
    }

    private static void Tripped() => Equal("Unavailable — not computed", Cell(Build(), "Polar", "Tripped").Value);

    private static void TransitionAndBucket()
    {
        SectionView v = Build();
        ChartModel tr = v.Charts.Single(c => c.Id == "transition");
        ChartSeries[] lines = tr.Plots.Single().Series.ToArray();
        Equal(4, lines.Length, "upper and lower at both Ncrit");
        Equal(4, lines.Select(s => (s.Dashed, s.Marker)).Distinct().Count(), "told apart by dash and marker as well as colour");
        foreach (string part in new[] { "Transition x_tr/c, upper and lower", "Ncrit 2 and 4", "r1", "Re ", "Overlay a section ▾" })
            Equal(true, tr.Legend.Contains(part), "legend names " + part);
        ChartModel bucket = v.Charts.Single(c => c.Id == "bucket");
        Equal(2, bucket.Plots.Count, "σ required and V_crit");
        Equal(true, bucket.Plots.All(p => p.Series.Single().Points.Count > 3 && p.Markers.Count == 1), "curves and the operating dot");
        Equal(F.Gov.Estimate.Cl, bucket.Plots[0].Markers[0].X, "operating Cl");
        Equal(true, bucket.Legend.StartsWith("σ required and V_crit against Cl · dot: this operating point") && bucket.Legend.Contains("Cavitation screening"), "legend and COPY-48 beside");
        Equal(true, bucket.Label.Contains("inviscid; no boundary layer"), "labelled inviscid");
        Equal("Unavailable — depth not set", Build(WithStation(F.Gov, F.Gov with { Depth = null })).Charts.Single(c => c.Id == "bucket").Unavailable, "no depth: Unavailable, no curve");
    }

    // ---- drag ----

    private static AnalysisRun Cds(double cd2, double cd4, Func<StripLoad, StripLoad>? more = null) =>
        RunWith(s => (more?.Invoke(s) ?? s) with { CdNcrit2 = new StripValue(cd2, null), CdNcrit4 = new StripValue(cd4, null) });

    private static void ProfileSources()
    {
        AnalysisRun run = Cds(0.01, 0.008);
        AnalysisViewModel v = ProjectionTests.View(run);
        ResultRow profile = ProjectionTests.Cell(v, "Loads", "Profile drag");
        StripValue n2 = Loads.ProfileDrag(run, 2), n4 = Loads.ProfileDrag(run, 4);
        Equal(Math.Min(n2.Value!.Value, n4.Value!.Value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "–" +
            Math.Max(n2.Value.Value, n4.Value.Value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), profile.Value, "the band is the polar's, both Ncrit");
        Equal(true, profile.Note!.StartsWith("Profile drag from the polar at α_eff, both Ncrit; band, not a prediction"), "COPY-328 note");
        double bound = F.Gov.Estimate.CdTurbulentBound;
        Equal(false, profile.Value.Contains(bound.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)), "the estimator bound is not in the band");
        Equal(true, Cell(Build(), "Estimator", "Cd (turbulent bound)").Value.Length > 0, "the bound is its own cell");
    }

    private static void MissingCd()
    {
        AnalysisRun run = RunWith(s => s.J >= 1 ? s : s with { CdNcrit2 = new StripValue(0.01, null), CdNcrit4 = new StripValue(0.008, null) });
        string profile = ProjectionTests.Cell(ProjectionTests.View(run), "Loads", "Profile drag").Value;
        Equal("Unavailable — cd missing at 3 strips; the estimator bound is not substituted", profile, "count named, bound not substituted");
        Equal(false, Regex.IsMatch(profile, @"^[0-9]"), "no number");
    }

    private static void WingOnly()
    {
        AnalysisRun run = Cds(0.01, 0.008);
        AnalysisViewModel metric = ProjectionTests.View(run);
        foreach (string group in new[] { "Wing result", "Loads" })
        {
            IReadOnlyList<ResultRow> rows = metric.Groups.Single(g => g.Title == group).Rows;
            Equal(1, rows.Count(r => r.Label == "Drag (Wing only)"), "one row in " + group);
            Equal(false, rows.Any(r => r.Label is "Total drag" or "Wing drag" or "Wing-only drag"), "no separate total in " + group);
        }
        ResultRow wing = ProjectionTests.Cell(metric, "Wing result", "Drag (Wing only)");
        Equal("N", wing.Unit, "Metric unit");
        Equal(true, wing.Note!.Contains("Wing only: induced (VLM + strip) plus profile (polar). Not a total.") && wing.Note.Contains("Not included: junction, mast, wave, spray") &&
            wing.Note.Contains("XFOIL-class surrogate"), "note, reason line and surrogate label");
        AnalysisViewModel imperial = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Imperial);
        ResultRow lbf = ProjectionTests.Cell(imperial, "Wing result", "Drag (Wing only)");
        Equal("lbf", lbf.Unit, "Imperial unit");
        double[] n = wing.Value.Split('–').Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray(), l = lbf.Value.Split('–').Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Equal(true, Math.Abs(n[0] / 4.4482216152605 - l[0]) < 2e-3 && Math.Abs(n[1] / 4.4482216152605 - l[1]) < 2e-3, "lbf is N over 4.448");
        Equal("Unavailable — missing: junction, mast, wave, spray", ProjectionTests.Cell(metric, "Wing result", "CL/CD").Value, "craft CL/CD stays Unavailable");
        Equal(true, double.TryParse(ProjectionTests.Cell(metric, "Wing result", "Wing-only CL/CD").Value.Split('–')[0], System.Globalization.CultureInfo.InvariantCulture, out _), "only Wing-only CL/CD shows a number");
    }

    private static void TotalNoProfile()
    {
        AnalysisViewModel v = ProjectionTests.View(RunWith(null, polar: false));
        Equal("Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray", ProjectionTests.Cell(v, "Wing result", "Drag (Wing only)").Value, "no polar names profile");
        Equal("Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray", ProjectionTests.Cell(v, "Wing result", "CL/CD").Value, "CL/CD too");
    }

    private static void CdNonPositive()
    {
        AnalysisRun run = Cds(0, 0, s => s with { DownwashTrefftz = 0 });
        Equal("Undefined — CD ≤ 0", ProjectionTests.Cell(ProjectionTests.View(run), "Wing result", "Wing-only CL/CD").Value);
    }

    // ---- Find alpha ----

    private static void FindFound()
    {
        // CL(α) = 0.1 α: target 0.3 meets at 3°
        FindAlphaOutcome ok = FindAlpha.Run(a => new StripValue(0.1 * a, null), 0.3, -2, 8, depthOverChord: 4.17);
        Equal(true, ok.Alpha is { } root && Math.Abs(root - 3) < 0.05, "root within tolerance");
        Equal($"α {ok.Alpha!.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}° meets CL 0.3 within 1 %", ok.Sentence, "sentence");
        foreach (string label in new[] { "Target CL", "Bracket", "Iterations", "Stopped because", "Basis", "Polar limit" })
            Equal(1, ok.Rows.Count(r => r.Label == label), label);
        Equal(true, ok.Rows.Single(r => r.Label == "Basis").Value.Contains("free surface not modelled") && ok.Rows.Single(r => r.Label == "Basis").Value.Contains("h/c = 4.17"), "A5.1 string at h/c below 5");
        Equal("attached-flow polar limit, not measured stall; pumping not modelled", ok.Rows.Single(r => r.Label == "Polar limit").Value, "CL_max string");
        Equal(true, ok.Solves >= 3, "lattice solves counted");
        Equal(true, FindAlpha.Run(a => new StripValue(0.1 * a, null), 0.8, -2, 8).Alpha is { } edge && Math.Abs(edge - 8) < 1e-9, "a root at the bracket edge is found");
    }

    private static void FindNoRoot()
    {
        (string sentence, string reason)[] cases =
        [
            (FindAlpha.Run(a => new StripValue(0.1 * a, null), 1.6, -2, 8).Sentence, "CL never reaches the target in the bracket"),
            (FindAlpha.Run(_ => new StripValue(null, "ANA-POLAR-NOT-CONVERGED"), 0.3, -2, 8).Sentence, "the polar did not converge"),
            (FindAlpha.Run(_ => new StripValue(null, "ANA-POLAR-LOW-CONFIDENCE"), 0.3, -2, 8).Sentence, "polar confidence is below the floor"),
            (FindAlpha.Run(_ => new StripValue(null, "ANA-POLAR-RE-OUTSIDE"), 0.3, -2, 8).Sentence, "Re is outside the polar's range"),
            (FindAlpha.Run(a => new StripValue(0.1 * a, null), 0.3, -2, 8, depthOverChord: 0.4).Sentence, "the foil is too shallow (h/c below 0.5)"),
        ];
        foreach ((string sentence, string reason) in cases)
            Equal($"Find α found no α — {reason}. Nothing was extrapolated.", sentence, reason);
        Equal(null, FindAlpha.Run(a => new StripValue(0.1 * a, null), 1.6, -2, 8).Alpha, "no α is returned");
    }

    // ---- copy ----

    private static void LabelsMatchDesign()
    {
        string design = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"));
        int checkedCount = 0;
        foreach (string text in Labels.ReasonTexts.Values.Concat([Labels.CpNoProfile, Labels.CpSolveFailed, Labels.UnderreadNotMeasured, Labels.Provisional,
            Labels.CavClear, Labels.CavInside, Labels.CavPossible, Labels.MarginLabel, Labels.PolarSurrogate, Labels.BracketInside, Labels.CdNonPositive,
            Labels.UnderreadMeasured, Labels.SigmaLabel, Labels.CpMinLabel, Labels.VcritLabel, Labels.BucketLegend, Labels.OverlayMenu,
            Labels.ProfileDragNote, Labels.DeltaVsLattice, Labels.DeltaVsLatticeNote, Labels.BracketLabel]).Distinct())
        {
            if (text.StartsWith("Unavailable — α is") || text.StartsWith("Unavailable — Re is") || text.StartsWith("Unavailable — Ncrit is") ||
                text.StartsWith("Unavailable — section fit") || text.StartsWith("Low confidence")) continue;   // the templated rows carry fixture values in DESIGN.md
            Equal(true, design.Contains(text), "DESIGN.md lacks: " + text);
            checkedCount++;
        }
        Equal(true, checkedCount > 40, "the scan covered " + checkedCount + " strings, not none");
    }

    // The reason-code registry is read from the built assembly's own string literals (user strings), not from a hand list.
    private static IReadOnlyList<string> CodesInAssembly()
    {
        using var stream = File.OpenRead(typeof(AnalysisService).Assembly.Location);
        using var pe = new PEReader(stream);
        MetadataReader md = pe.GetMetadataReader();
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        UserStringHandle handle = MetadataTokens.UserStringHandle(1);
        while (!handle.IsNil)
        {
            string text = md.GetUserString(handle);
            if (Regex.IsMatch(text, @"^ANA-[A-Z0-9-]+$")) codes.Add(text);
            handle = md.GetNextHandle(handle);
        }
        return [.. codes];
    }

    // Codes that are thrown, refused or kept in the run store and never become a cell's text.
    private static readonly string[] NotACell = ["ANA-INPUT-", "ANA-FIND-", "ANA-FREE-SURFACE-", "ANA-PANEL-", "ANA-SOLVE-", "ANA-TIP-", "ANA-UNEXPECTED",
        "ANA-CANCELLED", "ANA-NONFINITE", "ANA-PROFILE-DRAG-MISSING-CD", "ANA-CAV-INPUT", "ANA-EDIT-INERT", "ANA-POLAR-CONSISTENCY-", "ANA-POLAR-", "ANA-SECTION-", "ANA-CAV-DEPTH-NOT-SET-"];

    private static void ReasonCodesComplete()
    {
        IReadOnlyList<string> found = CodesInAssembly();
        Equal(true, found.Count > 30, "the registry scan found " + found.Count + " codes");
        string[] unregistered = found.Where(c => !Labels.ReasonTexts.ContainsKey(c) && !NotACell.Any(p => c.StartsWith(p, StringComparison.Ordinal) && c != "ANA-POLAR-")).ToArray();
        Equal("", string.Join(",", unregistered.Where(c => !c.StartsWith("ANA-POLAR-", StringComparison.Ordinal) && !c.StartsWith("ANA-SECTION-", StringComparison.Ordinal))), "a code with no approved text");
        foreach (KeyValuePair<string, string> pair in Labels.ReasonTexts)
        {
            Equal(true, pair.Value.Length > 0 && !Code.IsMatch(pair.Value), "text of " + pair.Key);
            Equal(pair.Value, Labels.UnavailableBecause(pair.Key), "lookup of " + pair.Key);
        }
    }

    private static void NoRawCode()
    {
        // one fixture per registered code; each first proves the run carries the code, then scans every cell, note, legend and table
        var fixtures = new Dictionary<string, Func<(bool Carries, string Text)>>(StringComparer.Ordinal);
        foreach (string code in Labels.ReasonTexts.Keys)
        {
            string c = code;
            fixtures[c] = c switch
            {
                _ when c.StartsWith("ANA-CAV-", StringComparison.Ordinal) => () =>
                {
                    CavitationResult g = F.Tier.Cavitation;
                    CavitationResult s = c switch
                    {
                        "ANA-CAV-DEPTH-NOT-SET" => Cavitation.Screen(-1, 200, null, 5, 1000, 101325, 1700, "η 0.5"),
                        "ANA-CAV-PV-MISSING" => Cavitation.Screen(-1, 200, 0.5, 5, 1000, 101325, null, "η 0.5"),
                        "ANA-CAV-SURFACE-PIERCING" => Cavitation.Screen(-1, 200, -0.1, 5, 1000, 101325, 1700, "η 0.5"),
                        "ANA-CAV-WATER-INVALID" => Cavitation.Screen(-1, 200, 0.5, 5, 1000, 101325, -1, "η 0.5"),
                        "ANA-CAV-DEPTH-INVALID" => Cavitation.Screen(-1, 200, double.NaN, 5, 1000, 101325, 1700, "η 0.5"),
                        "ANA-CAV-NO-SUCTION" => Cavitation.Screen(0.2, 200, 0.5, 5, 1000, 101325, 1700, "η 0.5"),
                        _ => Cavitation.Screen(-1, 200, 0.5, 5, 1000, 101325, 1e9, "η 0.5")
                    };
                    return (s.Reason == c, Text(Build(F.Tier with { Cavitation = s })) + Text(ProjectionTests.View(F.Run, new ProjectionContext(SectionTier: F.Tier with { Cavitation = s }))));
                },
                "ANA-SPEED-NOT-POSITIVE" => () => { AnalysisRun r = RunWith(null, F.Run.Op with { Speed = 0.001 }); return (true, Text(ProjectionTests.View(r))); },
                "ANA-REFERENCE-AREA-MISSING" or "ANA-INDUCED-DRAG-MISSING-AREA" => () =>
                {
                    AnalysisRun r = ProjectionTests.Rehash(RunWith(null) with { Reference = new RunReference(0, 0.8, 0.1, "frame origin", "body; wind") });
                    return (r.Reference.SRef == 0, Text(ProjectionTests.View(r)));
                },
                "ANA-OSWALD-UNDEFINED" => () => { AnalysisRun r = RunWith(s => s with { DownwashTrefftz = 0 }); return (Loads.InducedDrag(r).Value == 0, Text(ProjectionTests.View(r))); },
                "ANA-CENTRE-OF-LIFT-UNDEFINED" => () => { AnalysisRun r = RunWith(s => s with { Fx = 0, Fz = 0 }); return (r.Strips.All(s => s.Fz == 0), Text(ProjectionTests.View(r))); },
                "ANA-FORCE-NOT-FINITE" => () => { AnalysisRun r = RunWith(null) with { Strips = [] }; return (r.Strips.Count == 0, Text(ProjectionTests.View(r))); },
                "ANA-ROOT-MOMENT-MISSING-WIDTH" or "ANA-PROFILE-DRAG-MISSING-WIDTH" or "ANA-INDUCED-DRAG-MISSING-WIDTH" => () =>
                {
                    AnalysisRun r = RunWith(s => s with { YLow = 0.2, YHigh = 0.2 });
                    return (Loads.InducedDrag(r).UnavailableReason == "ANA-INDUCED-DRAG-MISSING-WIDTH", Text(ProjectionTests.View(r)));
                },
                "ANA-VERDICT-MISSING" => () => (true, Text(ProjectionTests.View(RunWith(null), new ProjectionContext(Verdicts: null)))),
                "ANA-PROFILE-DRAG-MISSING-STRIPS" or "ANA-INDUCED-DRAG-MISSING-STRIPS" or "ANA-DRAG-UNAVAILABLE" or "ANA-WING-RATIO-UNAVAILABLE" or "ANA-WING-ONLY-RATIO" or
                    "ANA-TOTAL-DRAG-MISSING-INDUCED" or "ANA-TOTAL-DRAG-MISSING-PROFILE" or "ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY" or "ANA-INDUCED-DRAG-NONFINITE" or
                    "ANA-PROFILE-DRAG-NONFINITE" or "ANA-SECTION-ITTC1957-BOUND" => () =>
                {
                    AnalysisRun r = c.EndsWith("STRIPS", StringComparison.Ordinal) ? RunWith(null) with { Strips = [] } : Cds(0.01, 0.008);
                    return (true, Text(ProjectionTests.View(r)) + Text(Build()));
                },
                _ => () =>
                {
                    // the polar family: a strip whose cd carries the code
                    AnalysisRun r = RunWith(s => s with { CdNcrit2 = new StripValue(null, c), CdNcrit4 = new StripValue(null, c) });
                    return (r.Strips.All(s => s.CdNcrit2.UnavailableReason == c), Text(ProjectionTests.View(r)));
                }
            };
        }
        Equal(Labels.ReasonTexts.Count, fixtures.Count, "one fixture per code");
        foreach ((string code, Func<(bool, string)> make) in fixtures)
        {
            (bool carries, string text) = make();
            Equal(true, carries, "the fixture's run carries " + code);
            Equal(true, text.Length > 0, "the scan of " + code + " covered text");
            Equal("", string.Join(",", Code.Matches(text).Select(m => m.Value + " in " + text.Substring(Math.Max(0, m.Index - 30), Math.Min(60, text.Length - Math.Max(0, m.Index - 30))).Replace("\n", "/")).Distinct()), "raw code shown for fixture " + code);
        }
        // the Section tab's error states 10, 18-23, 37, 48 and 54 are scanned above (Row10, ReasonRows, TrainingRange, MissingCd, FindNoRoot) and here
        string states = Text(ProjectionTests.View(F.Run, new ProjectionContext())) + Text(ProjectionTests.View(F.Run, new ProjectionContext(SectionFailureCode: "ANA-PANEL-GEOMETRY"))) +
            Text(Build(source: SectionSeamTests.ThicknessSource(_ => 0.45))) + FindAlpha.Run(_ => new StripValue(null, "ANA-POLAR-NOT-CONVERGED"), 0.3, -2, 8).Sentence;
        Equal("", string.Join(",", Code.Matches(states).Select(m => m.Value).Distinct()), "raw code in an error state");
    }
}
