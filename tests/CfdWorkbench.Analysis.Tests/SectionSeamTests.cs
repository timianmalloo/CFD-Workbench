using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class SectionSeamTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("Section_WingRun_PanelValuesAtEveryStation", WingRows);
        AnalysisChecks.Check("Section_ProvisionalAndProjectionRows", ProjectionRows);
        AnalysisChecks.Check("Section_SurfacePiercing_EstimatorUnavailable", Piercing);
        AnalysisChecks.Check("Section_AnalysisRunEvent_CarriesPanelCandidateCount", CandidateCountOnEvent);
        AnalysisChecks.Check("Section_NearTie_GoverningReSelectedAt400", NearTieReSelected);
        AnalysisChecks.Check("Section_ThinStationOutsideNearTie_NotMeasured", NotMeasuredStation);
        AnalysisChecks.Check("Section_UniformWing_CandidateCountCappedAtFour", CandidateCap);
        AnalysisChecks.Check("Section_GoverningEstimate_Cambered_Matches400Panels", GoverningEstimateIs400);
        AnalysisChecks.Check("Section_SixPercentThick_TwoGridUnderread_Measured", SixPercentUnderread);
        AnalysisChecks.Check("Section_GoverningStation_ScreenAndCpMinFrom400Panels", GoverningFine);
        AnalysisChecks.Check("Section_AdaptivePanelMethod_OldRunReadsHistorical", AdaptiveMethodIdentity);
    }

    internal static StripLoad Strip(double eta, double alphaEff) =>
        new(0, eta, eta, 1, 0, 0, alphaEff, 5e5, 0.4, new StripValue(null, Labels.NoPolar), new StripValue(null, Labels.NoPolar),
            0, 0, 0, 0, 0, 0, 0);

    /// <summary>The default wing with its thickness channel replaced: t/c as a function of the CV abscissa (eta).</summary>
    internal static byte[] ThicknessSource(Func<double, double> tc)
    {
        string dsl = Encoding.UTF8.GetString(FoilSource.NewDefault());
        dsl = Regex.Replace(dsl, @"(?m)^(  thickness cv \{.*?points \[)(.*?)(\] ids)", match =>
            match.Groups[1].Value + Regex.Replace(match.Groups[2].Value, @"\(([^,]+), ([^)]+)\)", point =>
                "(" + point.Groups[1].Value + ", " + tc(double.Parse(point.Groups[1].Value, CultureInfo.InvariantCulture))
                    .ToString("R", CultureInfo.InvariantCulture) + ")") + match.Groups[3].Value);
        byte[] source = Encoding.UTF8.GetBytes(dsl);
        if (!FoilSource.Parse(source).IsParsed) throw new InvalidOperationException("thickness-varied source did not parse");
        return source;
    }

    private static double Suction200(byte[] source, double eta, double alphaDeg) =>
        -PanelMethod.Solve(PanelMethod.SampleSection(source, eta, PanelMethod.DefaultPanelCount), alphaDeg).CpMin;

    /// <summary>The α at which the section at <paramref name="eta"/> reads <paramref name="suction"/> at 200 panels (bisection).</summary>
    private static double AlphaForSuction200(byte[] source, double eta, double suction)
    {
        double low = 0.2, high = 12;
        for (int i = 0; i < 16; i++)
        {
            double mid = 0.5 * (low + high);
            if (Suction200(source, eta, mid) < suction) low = mid; else high = mid;
        }
        return 0.5 * (low + high);
    }

    private static string StationName(double eta) => "η " + eta.ToString("0.###", CultureInfo.InvariantCulture);

    // Planted near-tie: a thick station (eta 0.5) reads 1.03 times the suction of a thin one (eta 1.0, alpha 3) at 200 panels,
    // so it wins at 200. The thin station's 200-vs-400 under-read is large, so at 400 the thin station wins.
    private static void NearTieReSelected()
    {
        byte[] source = ThicknessSource(x => 0.12 + (0.02 - 0.12) * x);
        double thinSuction = Suction200(source, 1.0, 3);
        double thickAlpha = AlphaForSuction200(source, 0.5, 1.03 * thinSuction);
        SectionTierResult tier = SectionTier.Evaluate(source, [0.5, 1.0], [Strip(0.5, thickAlpha), Strip(1.0, 3)],
            Fixture.Op(3), Fixture.Salt);
        SectionStationResult thick = tier.Stations[0], thin = tier.Stations[1];
        // The 200-pass winner is the thick station: its 200 ratio is smaller. Rebuilt from independent solves.
        double thickRatio200 = thick.Cavitation.Sigma!.Value / Suction200(source, 0.5, thickAlpha);
        double thinRatio200 = thin.Cavitation.Sigma!.Value / thinSuction;
        if (!(thickRatio200 < thinRatio200)) throw new InvalidOperationException("fixture: the thick station must win at 200 panels");
        AnalysisChecks.Equal(1.0, tier.GoverningEta, "governing re-selected at 400 panels");
        AnalysisChecks.Equal(StationName(tier.GoverningEta), tier.Cavitation.GoverningStation, "the wing screen's station is GoverningEta");
        AnalysisChecks.Equal(2, tier.PanelCandidateCount, "both stations were solved at 400");
        AnalysisChecks.Equal(400, thin.Estimate.Panel.StationCount, "thin station panel count");
        if (!thin.Provisional || !(tier.PanelUnderreadFraction > 0.10) || !tier.GoverningProvisional)
            throw new InvalidOperationException("the thin governing station above 10 % is not provisional: " + tier.PanelUnderreadFraction);
        if (Math.Abs(tier.PanelUnderreadFraction - thin.PanelUnderread!.Value) > 1e-15)
            throw new InvalidOperationException("the run's under-read is not the re-selected governing station's");
        Console.WriteLine($"OBSERVED near-tie: thick 200-ratio {thickRatio200:F4}, thin 200-ratio {thinRatio200:F4}; thin under-read {thin.PanelUnderread:P2}; thick {thick.PanelUnderread:P2}; governing eta {tier.GoverningEta}");
    }

    // Ruling 114: a 2 %-thick station at 1.12x the best 200 ratio, behind three thicker stations that fill the lowest-ratio
    // slots (1.03x, 1.05x, 1.07x), is solved through the reserved thinnest-station slot and wins at 400. Under the 2x width
    // (about 8 %) it is outside the window and the governing station stays wrong.
    private static void ThinStationAt112Wins()
    {
        byte[] source = ThicknessSource(x => 0.12 + (0.02 - 0.12) * x);
        double[] etas = [0.5, 0.6, 0.7, 0.8, 1.0];
        double thinSuction = Suction200(source, 1.0, 3);
        double[] k = [1.0, 1.03, 1.05, 1.07];
        var strips = new List<StripLoad> { Strip(1.0, 3) };
        for (int i = 0; i < k.Length; i++)
            strips.Add(Strip(etas[i], AlphaForSuction200(source, etas[i], 1.12 * thinSuction / k[i])));
        SectionTierResult tier = SectionTier.Evaluate(source, etas, strips, Fixture.Op(3), Fixture.Salt);
        SectionStationResult thin = tier.Stations[4];
        double best = tier.Stations.Take(4).Min(s => s.Cavitation.Sigma!.Value / Suction200(source, s.Eta, s.AlphaEffDeg));
        double thinRatio = thin.Cavitation.Sigma!.Value / thinSuction;
        if (thinRatio / best < 1.10 || thinRatio / best > 1.15)
            throw new InvalidOperationException($"fixture: thin station at {thinRatio / best:F3}x the best 200 ratio, not 1.10-1.15x");
        AnalysisChecks.Equal(400, thin.Estimate.Panel.StationCount, "the thin station was solved at 400");
        AnalysisChecks.Equal(SectionTier.MaxPanelCandidates, tier.PanelCandidateCount, "cap");
        AnalysisChecks.Equal(1.0, tier.GoverningEta, "the thin station wins at 400");
        AnalysisChecks.Equal(StationName(1.0), tier.Cavitation.GoverningStation, "the wing screen's station");
        Console.WriteLine($"OBSERVED thin station at {thinRatio / best:F3}x best 200 ratio solved; under-read {thin.PanelUnderread:P2}; governing eta {tier.GoverningEta}");
    }

    // A thin station far from the governing ratio is not solved at 400: "not measured", never provisional.
    private static void NotMeasuredStation()
    {
        byte[] source = ThicknessSource(x => 0.12 + (0.02 - 0.12) * x);
        SectionTierResult tier = SectionTier.Evaluate(source, [0.5, 1.0], [Strip(0.5, 6), Strip(1.0, 0.5)], Fixture.Op(3), Fixture.Salt);
        AnalysisChecks.Equal(0.5, tier.GoverningEta, "the thick, high-alpha station governs");
        AnalysisChecks.Equal(1, tier.PanelCandidateCount, "only the governing station is solved at 400");
        SectionStationResult thin = tier.Stations[1];
        if (thin.PanelUnderreadMeasured || thin.PanelUnderread is not null || thin.Provisional)
            throw new InvalidOperationException("an unsolved station must read not measured, and not provisional");
        AnalysisChecks.Equal(200, thin.Estimate.Panel.StationCount, "not-measured station stays at 200 panels");
        double onDemand = SectionTier.UnderreadAt(source, 1.0, thin.AlphaEffDeg, thin.Reynolds);
        if (!(onDemand > 0.10)) throw new InvalidOperationException("the on-demand under-read of the thin station is " + onDemand);
        Console.WriteLine($"OBSERVED thin station not measured in the run; UnderreadAt = {onDemand:P2} at alpha {thin.AlphaEffDeg}");
    }

    // At most four stations are re-solved, however many tie (a uniform wing ties at every station).
    private static void CandidateCap()
    {
        byte[] source = FoilSource.NewDefault();
        SectionTierResult tier = SectionTier.Evaluate(source, Settings.SpanEtas(8, "cosine"), [], Fixture.Op(3), Fixture.Salt);
        AnalysisChecks.Equal(SectionTier.MaxPanelCandidates, tier.PanelCandidateCount, "candidate cap");
        AnalysisChecks.Equal(4, tier.Stations.Count(s => s.PanelUnderreadMeasured), "stations carrying a measured under-read");
        AnalysisChecks.Equal(4, tier.Stations.Count(s => s.Estimate.Panel.StationCount == 400), "stations solved at 400");
    }

    // The governing station's Cl, Cm, alpha_L0 and lift per span are the 400-panel estimate (cambered source).
    private static void GoverningEstimateIs400()
    {
        byte[] source = CamberedSource();
        SectionTierResult tier = SectionTier.Evaluate(source, Settings.SpanEtas(8, "cosine"), [], Fixture.Op(3), Fixture.Salt);
        SectionStationResult governing = tier.Stations.MinBy(s => Math.Abs(s.Eta - tier.GoverningEta))!;
        SectionSample fine = PanelMethod.SampleSection(source, governing.Eta, 400);
        SectionEstimate independent = SectionEstimator.Estimate(fine, governing.AlphaEffDeg, governing.Reynolds);
        AnalysisChecks.Equal(independent.Cl, governing.Estimate.Cl, "Cl");
        AnalysisChecks.Equal(independent.CmQuarter, governing.Estimate.CmQuarter, "Cm_c/4");
        AnalysisChecks.Equal(independent.AlphaL0Deg, governing.Estimate.AlphaL0Deg, "alpha_L0");
        double chord = fine.Frame.TrailingMeters - fine.Frame.LeadingMeters;
        AnalysisChecks.Equal(0.5 * Fixture.Salt.Rho * Fixture.Op(3).Speed * Fixture.Op(3).Speed * chord * independent.Cl,
            governing.LiftPerSpan, "lift per span");
        if (governing.Estimate.AlphaL0Deg >= -0.1) throw new InvalidOperationException("the fixture did not exercise a cambered zero-lift solve");
    }

    // Ruling 110 (6): a 6 %-thick station, alpha 3 and 6 deg: the measured 200-vs-400 under-read. A value above the
    // 3.5-3.9 % expectation is reported, not tuned (it reopens the 2x width).
    private static void SixPercentUnderread()
    {
        byte[] source = ThicknessSource(_ => 0.06);
        SectionSample s = PanelMethod.SampleSection(source, 0.5, 200);
        AnalysisChecks.Equal(0.06, Math.Round(s.Frame.ThicknessRatio, 4), "t/c of the 6 %-thick station");
        foreach (double alpha in new[] { 3.0, 6.0 })
        {
            double u = SectionTier.UnderreadAt(source, 0.5, alpha, 5e5);
            if (!(u > 0 && u < 0.10)) throw new InvalidOperationException($"6 % thick, alpha {alpha}: under-read {u:P3}");
            Console.WriteLine($"OBSERVED 6%-thick 200-vs-400 under-read at alpha {alpha}: {u:P3}");
        }
    }

    // Ruling 110 (5): analysis.run carries the number of stations solved at 400 panels.
    private static void CandidateCountOnEvent()
    {
        using var session = Fixture.Opened();
        RunSettings settings = Settings.Default with { NSpanPerHalf = 4, NChord = 2,
            SectionEtas = [0d, 0.5, 1d], SectionXs = Settings.ChordXs(2, "cosine") };
        AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, new ProductWingMethod(settings)), Fixture.Op(3));
        AnalysisChecks.Equal(true, run.Outcome is RunOutcome.Completed, "run completed");
        AnalysisEvent emitted = Fixture.RunEvents(session).Last().Analysis!;
        if (emitted.PanelCandidates is not int count || count < 1 || count > SectionTier.MaxPanelCandidates)
            throw new InvalidOperationException("analysis.run omitted the 400-panel candidate count: " + emitted.PanelCandidates);
        Console.WriteLine($"OBSERVED analysis.run panelCandidates {count}");
    }

    // Ruling 103: the governing station is re-solved at 400; the screen and Cp_min use that value.
    private static void GoverningFine()
    {
        byte[] source = FoilSource.NewDefault();
        double[] etas = Settings.SpanEtas(8, "cosine");
        SectionTierResult tier = SectionTier.Evaluate(source, etas, [], Fixture.Op(3), Fixture.Salt);
        SectionStationResult governing = tier.Stations.MinBy(s => Math.Abs(s.Eta - tier.GoverningEta))!;
        SectionSample coarse = PanelMethod.SampleSection(source, governing.Eta, 200);
        SectionSample fine = PanelMethod.SampleSection(source, governing.Eta, 400);
        double cp200 = PanelMethod.Solve(coarse, governing.AlphaEffDeg).CpMin;
        double cp400 = PanelMethod.Solve(fine, governing.AlphaEffDeg).CpMin;
        if (Math.Abs(cp400 - cp200) < 1e-3)
            throw new InvalidOperationException($"fixture does not separate 200 from 400: {cp200} vs {cp400}");
        AnalysisChecks.Equal(400, governing.Estimate.Panel.StationCount, "governing station panel count");
        if (Math.Abs(governing.Estimate.Panel.CpMin - cp400) > 1e-12)
            throw new InvalidOperationException("governing Cp_min is not the 400-panel value");
        AnalysisChecks.Equal(400, governing.Cavitation.StationCount, "governing screen resolution");
        if (governing.Cavitation.CpMin is not double screenCp || Math.Abs(screenCp - cp400) > 1e-12)
            throw new InvalidOperationException("governing screen did not use the 400-panel Cp_min");
        if (tier.Cavitation.CpMin is not double wingCp || Math.Abs(wingCp - cp400) > 1e-12)
            throw new InvalidOperationException("the wing screen did not use the governing 400-panel Cp_min");
        foreach (SectionStationResult other in tier.Stations.Where(s => s != governing))
        {
            int expected = other.PanelUnderreadMeasured ? 400 : 200;
            AnalysisChecks.Equal(expected, other.Estimate.Panel.StationCount, "other station panel count");
            AnalysisChecks.Equal(expected, other.Cavitation.StationCount, "other station screen resolution");
        }
        double expectedUnderread = (-cp400 - -cp200) / -cp400;
        if (Math.Abs(tier.PanelUnderreadFraction - expectedUnderread) > 1e-12)
            throw new InvalidOperationException($"under-read {tier.PanelUnderreadFraction} != {expectedUnderread}");
    }

    // A run stored under the 200-everywhere method must read Historical against the adaptive method.
    private static void AdaptiveMethodIdentity()
    {
        RunMethod oldMethod = MethodRecord.VlmStrip.Method with { Version = "1.2.0/panel200-te3" };
        if (MethodRecord.VlmStrip.Method.Version == oldMethod.Version)
            throw new InvalidOperationException("the method version still names the 200-everywhere method");
        AnalysisRun run = ProjectionTests.Data().Run;
        AnalysisRun old = ProjectionTests.Rehash(run with
        {
            Method = oldMethod,
            RunKey = RunRecord.Key(run.Inputs, run.Water, run.Op, oldMethod, run.SettingsHash)
        });
        CurrentInputs now = ProjectionTests.Current(run) with { Method = MethodRecord.VlmStrip.Method };
        AnalysisChecks.Equal(RunState.Historical, Freshness.State(new StoredRun(old, RunIntegrity.Intact), now), "old-method run");
        AnalysisChecks.Equal(RunState.Current, Freshness.State(new StoredRun(
            ProjectionTests.Rehash(run with
            {
                Method = MethodRecord.VlmStrip.Method,
                RunKey = RunRecord.Key(run.Inputs, run.Water, run.Op, MethodRecord.VlmStrip.Method, run.SettingsHash)
            }), RunIntegrity.Intact), now), "adaptive-method run");
    }

    // Cost 582 ms (C-5 limit 500 ms): five stations, four of them 400-panel estimates. Ring: readiness.
    internal static void RunReadiness()
    {
        AnalysisChecks.Check("Section_ThinStationAt112PercentOfBest_SolvedAndWins", ThinStationAt112Wins);
        AnalysisChecks.Check("Section_CamberedWing129_WarmTime", CamberedWing);
    }

    private static void CamberedWing()
    {
        byte[] source = CamberedSource();
        double[] etas = Settings.SpanEtas(Settings.Default.NSpanPerHalf, Settings.Default.SpanSpacing);
        var firstWatch = Stopwatch.StartNew();
        SectionTierResult first = SectionTier.Evaluate(source, etas, [], Fixture.Op(3), Fixture.Salt);
        firstWatch.Stop();
        var watch = Stopwatch.StartNew();
        SectionTierResult warm = SectionTier.Evaluate(source, etas, [], Fixture.Op(3), Fixture.Salt);
        watch.Stop();
        AnalysisChecks.Equal(129, warm.Stations.Count, "default whole-wing station count");
        if (warm.Stations.Any(station => !double.IsFinite(station.Estimate.Cl) ||
            !double.IsFinite(station.Estimate.Panel.CpMin) || station.Estimate.AlphaL0Deg >= -0.1))
            throw new InvalidOperationException("the timing foil did not exercise cambered zero-lift solves");
        Console.WriteLine($"MEASURE cambered 2% warm whole-wing section tier {watch.Elapsed.TotalMilliseconds:F3} ms (first run {firstWatch.Elapsed.TotalMilliseconds:F3} ms) at 129 stations; " +
            $"first-run delta {first.PanelUnderreadFraction:P3}; warm delta {warm.PanelUnderreadFraction:P3}");
    }

    private static byte[] CamberedSource()
    {
        const int n = 40;
        var dat = new StringBuilder("NACA 2412\n");
        for (int i = n; i >= 0; i--) Point(i, true);
        for (int i = 1; i <= n; i++) Point(i, false);
        ImportedProfile fitted = DatImport.Fit(DatImport.Parse(Encoding.UTF8.GetBytes(dat.ToString())), "naca-2412");
        string dsl = Encoding.UTF8.GetString(FoilSource.NewDefault());
        dsl = Regex.Replace(dsl, @"(?s)  profiles \{.*?\n  \}\n  sections",
            "  profiles {\n" + fitted.ProfileBlock + "\n  }\n  sections");
        dsl = dsl.Replace("naca-0012", "naca-2412", StringComparison.Ordinal);
        byte[] source = Encoding.UTF8.GetBytes(dsl);
        if (!FoilSource.Parse(source).IsParsed) throw new InvalidOperationException("cambered wing source did not parse");
        return source;

        void Point(int i, bool upper)
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * i / n));
            double yc = x < 0.4 ? 0.02 / 0.16 * (0.8 * x - x * x) :
                0.02 / 0.36 * (0.2 + 0.8 * x - x * x);
            double yt = 5 * 0.12 * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x +
                0.2843 * Math.Pow(x, 3) - 0.1036 * Math.Pow(x, 4));
            dat.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x, yc + (upper ? yt : -yt)));
        }
    }

    private static void WingRows()
    {
        byte[] source = FoilSource.NewDefault();
        OperatingPoint op = Fixture.Op(3);
        // C-5 (Ruling 110 adds up to three 400-panel solves): 65 complete wing stations exercise the every-station invariant without the default 129-station
        // fixture's measured 515 ms cost under the concurrent ring. The product default remains 129 stations.
        double[] etas = Settings.SpanEtas(32, "cosine");
        var watch = Stopwatch.StartNew();
        SectionTierResult section = SectionTier.Evaluate(source, etas, [], op, Fixture.Salt);
        watch.Stop();
        Console.WriteLine($"MEASURE warm whole-wing section tier {watch.Elapsed.TotalMilliseconds:F3} ms at {PanelMethod.DefaultPanelCount} panels");
        if (watch.Elapsed.TotalMilliseconds > 1000) throw new InvalidOperationException("warm 200-panel whole-wing section tier exceeded 1 s");
        AnalysisChecks.Equal(etas.Length, section.Stations.Count, "all run stations sampled");
        if (section.Stations.Any(station => station.Estimate.Panel.StationCount != (station.PanelUnderreadMeasured ? 400 : 200) ||
            !double.IsFinite(station.Estimate.Cl) || !double.IsFinite(station.Estimate.CmQuarter) ||
            !double.IsFinite(station.Estimate.AlphaL0Deg) || !double.IsFinite(station.Estimate.CdTurbulentBound) ||
            !double.IsFinite(station.LiftPerSpan)))
            throw new InvalidOperationException("a section station lacks a 200-panel estimate");
        if (!double.IsFinite(section.PanelUnderreadFraction))
            throw new InvalidOperationException("the governing-station two-grid delta was not measured");
        Console.WriteLine($"MEASURE governing-station eta {section.GoverningEta:F3} 200-vs-400 suction under-read {section.PanelUnderreadFraction:P3}");
    }

    private static void ProjectionRows()
    {
        AnalysisRun run = ProjectionTests.Data().Run;
        var panel = new PanelResult([], [], -1, PanelMethod.DefaultPanelCount, 0.3, -0.02);
        var estimate = new SectionEstimate(panel, 0, 0.01);
        CavitationResult cavitation = Cavitation.Screen(-1, PanelMethod.DefaultPanelCount, 0.5,
            run.Op.Speed, run.Water.Rho, run.Op.PAtm, run.Water.Pv, "η 0.5");
        var sample = new PolarSample("profile", "neuralfoil", "test", 5e5, 2, "clean", 2, "water",
            0.3, 0.01, -0.02, 0.4, 0.5, -1, 200, 0.9, true);
        var polar = new PolarResult(sample, [], false, 0.0001, 0.0002);
        var section = new SectionTierResult([new SectionStationResult(0.5, 2, 5e5, 0.5, estimate, cavitation)],
            cavitation, 0.5, 0.11) { PolarNcrit2 = polar, PolarNcrit4 = polar };
        var legacy = run.Strips.Select(s => s.J == 1 ? s with
            { Provisional = true, ProvisionalReason = StripLoad.PanelUnderreadReason } : s).ToArray();
        AnalysisRun oldRun = ProjectionTests.Rehash(run with { Strips = legacy });
        var verdicts = Enumerable.Range(0, legacy.Length)
            .Select(_ => new StripVerdict(false, ["Cl_local"], "Outside the method envelope at this strip")).ToArray();
        var oldView = AnalysisProjection.Build(oldRun, ProjectionTests.Current(oldRun), Units.Metric,
            new ProjectionContext(Verdicts: verdicts, SectionTier: section));
        ResultRow envelope = AnalysisProjection.StripAt(oldView, legacy[1].Eta).Rows
            .Single(row => row.Label == "Envelope (this strip)");
        AnalysisChecks.Equal("Outside the method envelope at this strip", envelope.Value,
            "panel under-read does not replace envelope verdict");
        AnalysisChecks.Equal(StripLoad.PanelUnderreadReason, envelope.Note, "separate legacy under-read note");
        var view = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric,
            new ProjectionContext(SectionTier: section));
        var rows = view.Groups.Single(group => group.Title == "Section (2D)").Rows;
        if (!rows.Any(row => row.Label == "Cl" && row.Value != Labels.NoPolar) ||
            !rows.Any(row => row.Label == "Cp_min" && row.Value != Labels.SectionCp))
            throw new InvalidOperationException("section projection still shows the A3a stub");
        foreach (string label in new[] { "Cl", "Cm_c/4", "α_L0", "Cp_min" })
            if (rows.Single(row => row.Label == label).Note?.Contains(PanelMethod.ModelLabel, StringComparison.Ordinal) != true)
                throw new InvalidOperationException(label + " lost panel tier label");
        foreach (string label in new[] { "Cp_min", "Cavitation" })
            if (rows.Single(row => row.Label == label).Note?.Contains("ANA-PANEL-UNDERREAD", StringComparison.Ordinal) != true ||
                rows.Single(row => row.Label == label).Note?.Contains("η 0.5", StringComparison.Ordinal) != true)
                throw new InvalidOperationException(label + " lost governing-station provisional flag");
        ResultRow vcrit = view.Groups.Single(group => group.Title == "Conditions").Rows.Single(row => row.Label == "V_crit");
        if (vcrit.Value.StartsWith("Unavailable", StringComparison.Ordinal) ||
            vcrit.Note?.Contains("ANA-PANEL-UNDERREAD", StringComparison.Ordinal) != true)
            throw new InvalidOperationException("governing V_crit was not marked provisional");
        if (rows.Any(row => row.Label == "Cd") ||
            rows.Single(row => row.Label == "ANA-SECTION-ITTC1957-BOUND").Note is null)
            throw new InvalidOperationException("the turbulent estimator bound is labelled as polar Cd");
        foreach (string label in new[] { "Ncrit 2", "Ncrit 4", "CST residual", "analysis_confidence" })
            AnalysisChecks.Equal("XFOIL-class surrogate; accuracy relative to XFOIL, not experiment",
                rows.Single(row => row.Label == label).Note, label + " COPY-66");
        foreach (ResultRow row in rows.Where(row => row.Value.Any(char.IsDigit)))
            if (string.IsNullOrWhiteSpace(row.Note) ||
                !row.Note.Contains(PanelMethod.ModelLabel, StringComparison.Ordinal) &&
                !row.Note.Contains("ITTC-1957", StringComparison.Ordinal) &&
                !row.Note.Contains("XFOIL-class surrogate", StringComparison.Ordinal))
                throw new InvalidOperationException("numeric section/polar row lacks its tier label: " + row.Label);
        if (vcrit.Note?.Contains(PanelMethod.ModelLabel, StringComparison.Ordinal) != true)
            throw new InvalidOperationException("panel-derived V_crit lacks its tier label");
    }

    private static void Piercing()
    {
        AnalysisRun run = ProjectionTests.Data().Run;
        var panel = new PanelResult([], [], -1, 200, 0.3, -0.02);
        var estimate = new SectionEstimate(panel, 0, 0.01);
        CavitationResult cavitation = Cavitation.Screen(-1, 200, -0.1, run.Op.Speed,
            run.Water.Rho, run.Op.PAtm, run.Water.Pv, "η 0.5");
        var station = new SectionStationResult(0.5, 2, 5e5, -0.1, estimate, cavitation);
        if (station.EstimatorAvailabilityCode != Cavitation.SurfacePiercing)
            throw new InvalidOperationException("surface-piercing station did not flag estimator unavailable");
        var tier = new SectionTierResult([station], cavitation, 0.5, 0);
        var projected = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric,
            new ProjectionContext(SectionTier: tier));
        if (projected.Groups.Single(group => group.Title == "Section (2D)").Rows.Single(row => row.Label == "Cl").Value !=
            Cavitation.SurfacePiercing)
            throw new InvalidOperationException("surface-piercing estimator still projected a number");
    }
}
