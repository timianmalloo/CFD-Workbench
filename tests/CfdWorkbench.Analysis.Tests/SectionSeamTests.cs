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
    }

    internal static void RunReadiness() =>
        AnalysisChecks.Check("Section_CamberedWing129_WarmTime", CamberedWing);

    private static void CamberedWing()
    {
        byte[] source = CamberedSource();
        double[] etas = Settings.SpanEtas(Settings.Default.NSpanPerHalf, Settings.Default.SpanSpacing);
        SectionTierResult first = SectionTier.Evaluate(source, etas, [], Fixture.Op(3), Fixture.Salt);
        var watch = Stopwatch.StartNew();
        SectionTierResult warm = SectionTier.Evaluate(source, etas, [], Fixture.Op(3), Fixture.Salt);
        watch.Stop();
        AnalysisChecks.Equal(129, warm.Stations.Count, "default whole-wing station count");
        if (warm.Stations.Any(station => !double.IsFinite(station.Estimate.Cl) ||
            !double.IsFinite(station.Estimate.Panel.CpMin) || station.Estimate.AlphaL0Deg >= -0.1))
            throw new InvalidOperationException("the timing foil did not exercise cambered zero-lift solves");
        Console.WriteLine($"MEASURE cambered 2% warm whole-wing section tier {watch.Elapsed.TotalMilliseconds:F3} ms at 129 stations; " +
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
        // C-5: 97 complete half-wing stations exercise the every-station invariant without the default 129-station
        // fixture's measured 515 ms cost under the concurrent ring. The product default remains 129 stations.
        double[] etas = Settings.SpanEtas(48, "cosine");
        var watch = Stopwatch.StartNew();
        SectionTierResult section = SectionTier.Evaluate(source, etas, [], op, Fixture.Salt);
        watch.Stop();
        Console.WriteLine($"MEASURE warm whole-wing section tier {watch.Elapsed.TotalMilliseconds:F3} ms at {PanelMethod.DefaultPanelCount} panels");
        if (watch.Elapsed.TotalMilliseconds > 1000) throw new InvalidOperationException("warm 200-panel whole-wing section tier exceeded 1 s");
        AnalysisChecks.Equal(etas.Length, section.Stations.Count, "all run stations sampled");
        if (section.Stations.Any(station => station.Estimate.Panel.StationCount != 200 ||
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
            AnalysisChecks.Equal(PanelMethod.ModelLabel, rows.Single(row => row.Label == label).Note,
                label + " panel tier label");
        if (rows.Any(row => row.Label == "Cd") ||
            rows.Single(row => row.Label == "ANA-SECTION-ITTC1957-BOUND").Note is null)
            throw new InvalidOperationException("the turbulent estimator bound is labelled as polar Cd");
        foreach (string label in new[] { "Ncrit 2", "Ncrit 4", "CST residual", "analysis_confidence" })
            AnalysisChecks.Equal("XFOIL-class surrogate; accuracy relative to XFOIL, not experiment",
                rows.Single(row => row.Label == label).Note, label + " COPY-66");
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
