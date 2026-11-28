using System.Globalization;
using System.Text;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Wave 13A Analysis &amp; ExportSession tests (T-RT-08, docs/design/rounded-wing-tips.md §9):
/// verifies Area 3 AnalysisService succeeds on <c>tip rounded</c>, AnalysisProjection emits COPY-941
/// (<see cref="Labels.RoundedTipCapNotice"/>) in the <c>Labels</c> group while preserving COPY-220,
/// and ExportSession emits the tip-closure summary row across STL, 3MF, STEP, and 3DM.
/// </summary>
internal static class RoundedTipAnalysisTests
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static void Run()
    {
        Check("RoundedTip_08_AnalysisServiceAndCopy941Disclosure", AnalysisServiceAndCopy941);
        Check("RoundedTip_08_ExportSessionTipClosureSummaryRows", ExportSessionTipClosureRows);
    }

    private static byte[] BuildRoundedTipWing(string tipClause = "tip rounded 1")
    {
        double dy = 457.5 * (1.0 - 0.85);
        double dxLe = Math.Sqrt(0.2 * 25.0 * dy);
        double dxTe = Math.Sqrt(0.2 * 10.0 * dy);
        string F(double v) => v.ToString("G17", CultureInfo.InvariantCulture);
        string text =
            "foildsl \"4.2\"\n" +
            "foil \"Rounded Tip Test\" {\n" +
            "  units mm\n" +
            "  half_span 457.5 mm\n" +
            "  evaluator \"cfdw-cv\" \"2\"\n" +
            "  symmetry mirror_y\n" +
            "  planform {\n" +
            $"    leading cv {{ degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, {F(42.0 - dxLe)}), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents {{ \"cv-4\" vertical }} }}\n" +
            $"    trailing cv {{ degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 112.4), (0.35, 112.4), (0.85, 94), (1, {F(67.0 + dxTe)}), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents {{ \"cv-4\" vertical }} }}\n" +
            "  }\n" +
            "  dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.08, 0), (0.25, 0), (0.5, 0), (0.75, 0), (0.92, 0), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] }\n" +
            "  twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.08, 0), (0.25, 0), (0.5, 0), (0.75, 0), (0.92, 0), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] }\n" +
            "  thickness cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0.10), (0.08, 0.10), (0.25, 0.10), (0.5, 0.098), (0.75, 0.095), (0.92, 0.093), (1, 0.092)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] }\n" +
            "  profiles {\n" +
            "    profile \"sym\" {\n" +
            "      upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.03), (0.2, 0.06), (0.5, 0.05), (0.8, 0.025), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"] }\n" +
            "      lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.03), (0.2, -0.06), (0.5, -0.05), (0.8, -0.025), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"] }\n" +
            "      closure closed\n" +
            "    }\n" +
            "  }\n" +
            "  sections { at root profile \"sym\" at tip profile \"sym\" }\n" +
            $"  {tipClause}\n" +
            "}\n";
        return Utf8.GetBytes(text);
    }

    private static void AnalysisServiceAndCopy941()
    {
        byte[] roundedBytes = BuildRoundedTipWing("tip rounded 1");
        using var session = new AuthoringSession();
        session.Open(roundedBytes, Guid.NewGuid().ToString("D"), acceptIdInsertion: true);
        var service = new AnalysisService(session, new ProductWingMethod());
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(service, op);
        Equal(true, run.Outcome is RunOutcome.Completed, "Area 3 VLM+strip must succeed on tip rounded with c_tip >= min");

        // Tip strip retains COPY-220 ("Not judged — tip strip")
        Equal(true, run.Strips.Any(s => s.Provisional && s.ProvisionalReason == StripLoad.TipProvisionalReason), "outermost tip strip remains provisional");

        var current = new CurrentInputs(run.Inputs, run.Water, run.Op, run.Method, run.SettingsHash);

        // Path A: ProjectionContext.RoundedTipCap = true (used by Desktop DeriveFeed which omits Source)
        var vmFromFlag = AnalysisProjection.Build(run, current, Units.Metric, new ProjectionContext(RoundedTipCap: true));
        var labelsFromFlag = vmFromFlag.Groups.Single(g => g.Title == "Labels").Rows;
        var tipCapRow = labelsFromFlag.SingleOrDefault(r => r.Label == "Tip cap");
        Equal(true, tipCapRow is not null, "Labels group must include Tip cap row when RoundedTipCap is true");
        Equal(Labels.RoundedTipCapNotice, tipCapRow!.Value, "Tip cap row must carry verbatim COPY-941");

        // Verify COPY-220 preserved in Strips group
        var stripsGroup = vmFromFlag.Groups.Single(g => g.Title == "Strips").Rows;
        Equal(true, stripsGroup.Any(r => r.Note == Labels.TipNotJudged), "Strips must preserve COPY-220");

        // Path B: ProjectionContext(Source: roundedBytes) also emits COPY-941
        var vmFromSource = AnalysisProjection.Build(run, current, Units.Metric, new ProjectionContext(Source: roundedBytes));
        var labelsFromSource = vmFromSource.Groups.Single(g => g.Title == "Labels").Rows;
        Equal(Labels.RoundedTipCapNotice, labelsFromSource.Single(r => r.Label == "Tip cap").Value, "COPY-941 emitted from rounded Source");

        // Open tip does NOT emit Tip cap row
        byte[] openBytes = BuildRoundedTipWing("tip open");
        var vmOpen = AnalysisProjection.Build(run, current, Units.Metric, new ProjectionContext(Source: openBytes, RoundedTipCap: false));
        var labelsOpen = vmOpen.Groups.Single(g => g.Title == "Labels").Rows;
        Equal(false, labelsOpen.Any(r => r.Label == "Tip cap"), "tip open must not emit rounded Tip cap notice");
    }

    private static void ExportSessionTipClosureRows()
    {
        byte[] roundedBytes = BuildRoundedTipWing("tip rounded 1");
        var src = new ExportSource(
            roundedBytes,
            "Rounded Tip Test",
            1,
            GeometryStatus.Certified,
            DraftOpen: false,
            Analysis: false,
            Stations: [new ExportStation("Root", 112.4, 10.0), new ExportStation("Tip", 25.0, 9.2)],
            DefaultStation: 0,
            ProjectFolder: null);
        var session = new ExportSession(src, cadGate: new CadExportGateStatus(true, true, "test", "test", []));

        const string expected = "Rounded 3D cap (ratio 1.00) · Overall span 917.3 mm";
        foreach (var fmt in new[] { ExportFormat.Stl, ExportFormat.ThreeMf, ExportFormat.Step, ExportFormat.ThreeDm })
        {
            session.Set(format: fmt, scope: StlScope.Whole);
            // Skeleton row while preparing
            Equal(expected, session.SummaryRows.Single(r => r.Label == ExportCopy.TipClosureLabel).Value, $"{fmt} preparing Tip closure");
            session.PrepareAsync(CancellationToken.None).GetAwaiter().GetResult();
            Equal(true, session.CanExport, $"{fmt} prepared cleanly");
            Equal(expected, session.SummaryRows.Single(r => r.Label == ExportCopy.TipClosureLabel).Value, $"{fmt} built Tip closure");
        }
    }
}
