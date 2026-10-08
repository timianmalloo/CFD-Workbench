using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class PolarNumericsTests
{
    // Readiness only: checks moved out of the fast ring (round-oct08 RGM, Ruling 143); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        AnalysisChecks.Check("Polar_ProductRun_ReachesStripsAndSectionProjection", ProductRun);
    }

    internal static void Run()
    {
        AnalysisChecks.Check("F13b_WaterWithPolar_RetrievesAtBothNewRe", WaterRetrieval);
        AnalysisChecks.Check("RunKey_PolarWeightsHashAndSize_ChangeKey", WeightsKey);
        AnalysisChecks.Check("Loads_TotalDrag_InducedPlusProfileOrNamesMissing", TotalDrag);
        AnalysisChecks.Check("Polar_LowConfidence_AdvisoryReachesDragSums", LowConfidenceDrag);
        AnalysisChecks.Check("DragBand_NcritValueOrderAndWingRatio", DragBandOrder);
        AnalysisChecks.Check("PolarReRange_ShowsValidatedBounds", PolarReRange);
    }

    private static void WaterRetrieval()
    {
        var solution = new LatticeSolution([0.1], [0.01], new RunDiagnostics(0, 1))
        {
            Strips = [new LatticeStrip(0, 0.1, 0.5, 0.12, 0.2, 0.1, 0.01, 0.2, 0, 0, 0.4, 0, 0.2)],
            Forces = [new StripForce(0, 0.1, 0.5, 0.12, 0.1, 0, 0, 10, 0, 0, 0)]
        };
        var polar = new ReynoldsPolar();
        const string hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var fresh = StripCoupler.Couple([], solution, Fixture.Op(3), Fixture.Fresh, polar, _ => hash, CancellationToken.None);
        var salt = StripCoupler.Couple([], solution, Fixture.Op(3), Fixture.Salt, polar, _ => hash, CancellationToken.None);
        double freshRe = fresh[0].ReLocal, saltRe = salt[0].ReLocal;
        if (!(saltRe < freshRe && salt[0].CdNcrit2.Value < fresh[0].CdNcrit2.Value &&
            salt[0].CdNcrit4.Value < fresh[0].CdNcrit4.Value))
            throw new InvalidOperationException("water viscosity did not re-retrieve both Ncrit profile drags");
        AnalysisChecks.Equal(4, polar.Calls.Count, "both Ncrit retrieved for each water record");
        AnalysisChecks.Equal(freshRe, polar.Calls[0].Re, "fresh Re sent to polar");
        AnalysisChecks.Equal(saltRe, polar.Calls[2].Re, "salt Re sent to polar");
    }

    private static void WeightsKey()
    {
        var method = new ProductWingMethod();
        RunPolar polar = method.Settings.Polar ?? throw new InvalidOperationException("product run has no polar method");
        if (!polar.Id.Contains(NeuralFoilNetwork.WeightsSha256[..8], StringComparison.Ordinal) ||
            !polar.Version.Contains(NeuralFoilNetwork.WeightsBytes.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("polar weights hash or byte size absent from run settings");
        using var session = Fixture.Opened();
        var view = session.Snapshot();
        var inputs = Freshness.Inputs(view);
        string Key(RunSettings settings) => RunRecord.Key(inputs, Fixture.Salt, Fixture.Op(3), method.Method,
            RunRecord.SettingsHash(settings));
        string baseline = Key(method.Settings);
        string changedHash = Key(method.Settings with { Polar = polar with { Id = polar.Id.Replace(NeuralFoilNetwork.WeightsSha256[..8], "00000000", StringComparison.Ordinal) } });
        string changedSize = Key(method.Settings with { Polar = polar with { Version = polar.Version.Replace(NeuralFoilNetwork.WeightsBytes.ToString(), "1", StringComparison.Ordinal) } });
        if (baseline == changedHash || baseline == changedSize || changedHash == changedSize)
            throw new InvalidOperationException("polar weights hash or byte size did not change run key");
    }

    private static void TotalDrag()
    {
        AnalysisRun complete = ProjectionTests.Data(s => s with
        {
            CdNcrit2 = new StripValue(0.02, null), CdNcrit4 = new StripValue(0.03, null)
        }).Run;
        StripValue wing = Loads.WingDrag(complete, 2);
        if (wing.Value is not { } drag || Math.Abs(drag - 20.4) > 1e-9)
            throw new InvalidOperationException("wing-only drag must sum Trefftz induced and Ncrit 2 profile drag: " + wing);
        AnalysisChecks.Equal(30.4, Loads.WingDrag(complete, 4).Value!.Value, "Ncrit 4 wing drag");
        StripValue total = Loads.TotalDrag(complete, 2);
        if (total.Value is not null || total.UnavailableReason != "ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY")
            throw new InvalidOperationException("craft Total drag claimed the wing-only subtotal: " + total);
        RunSettings sourcedSettings = complete.Settings with { Polar = new RunPolar("fixture", "1", "clean") };
        string sourcedSettingsHash = RunRecord.SettingsHash(sourcedSettings);
        AnalysisRun sourced = ProjectionTests.Rehash(complete with
        {
            Settings = sourcedSettings, SettingsHash = sourcedSettingsHash,
            RunKey = RunRecord.Key(complete.Inputs, complete.Water, complete.Op, complete.Method, sourcedSettingsHash)
        });
        var projected = AnalysisProjection.Build(sourced, ProjectionTests.Current(sourced), Units.Metric);
        var loadRows = projected.Groups.Single(group => group.Title == "Loads").Rows;
        if (projected.WingDragNcrit2?.Value is not > 0 || loadRows.Any(row => row.Label is "Total drag" or "Wing-only drag"))
            throw new InvalidOperationException("Ruling 109: one Drag (Wing only) row, no separate Total drag row");
        int wingIndex = Array.FindIndex(loadRows.ToArray(), row => row.Label == "Drag (Wing only)");
        if (wingIndex < 0 || loadRows[wingIndex].Value.StartsWith("Unavailable", StringComparison.Ordinal) ||
            loadRows[wingIndex].Note?.Contains("Wing only: induced (VLM + strip) plus profile (polar). Not a total.", StringComparison.Ordinal) != true ||
            loadRows[wingIndex].Note?.EndsWith("\nNot included: junction, mast, wave, spray", StringComparison.Ordinal) != true)
            throw new InvalidOperationException("Drag (Wing only) lacks its COPY-330 note or COPY-356 reason line");
        AnalysisChecks.Equal(Labels.TotalDragMissingWithProfile, projected.Groups.Single(group => group.Title == "Wing result").Rows
            .Single(row => row.Label == "CL/CD").Value, "craft CL/CD stays Unavailable");
        AnalysisRun missing = ProjectionTests.Data().Run;
        if (Loads.TotalDrag(missing, 2).UnavailableReason?.Contains("PROFILE", StringComparison.Ordinal) != true)
            throw new InvalidOperationException("missing profile component was not named");
    }

    private static void ProductRun()
    {
        using var session = Fixture.Opened();
        RunSettings settings = Settings.Default with { NSpanPerHalf = 4, NChord = 2,
            SectionEtas = [0d, 0.5, 1d], SectionXs = Settings.ChordXs(2, "cosine") };
        var method = new ProductWingMethod(settings);
        AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, method), Fixture.Op(3));
        if (run.Outcome is not RunOutcome.Completed || run.Settings.Polar is null ||
            !run.Strips.Any(strip => strip.CdNcrit2.Value is > 0 && strip.CdNcrit4.Value is > 0))
            throw new InvalidOperationException("product run did not carry both polar drag values");
        AnalysisEvent emitted = Fixture.RunEvents(session).Last().Analysis!;
        if (emitted.PanelUnderreadFraction is not { } delta || !double.IsFinite(delta))
            throw new InvalidOperationException("analysis.run omitted the measured governing-station panel delta");
        byte[] source = session.AcceptedSourceOf(run.Inputs.AcceptedId)!;
        var current = Freshness.Current(session.Snapshot(), Fixture.Salt, Fixture.Op(3), method.Method, method.Settings);
        var view = AnalysisProjection.Build(run, current, Units.Metric, new ProjectionContext(Source: source));
        if (view.SectionTier is not { } projectedSection ||
            Math.Abs(projectedSection.PanelUnderreadFraction - emitted.PanelUnderreadFraction!.Value) > 1e-12 ||
            projectedSection.Stations.Any(station => station.Estimate.Panel.Upper.Count == 0))
            throw new InvalidOperationException("section Cp curves or exact event delta did not reach projection");
        PolarConsistencyResult? consistency = view.PolarConsistency;
        if (consistency is not { Strips.Count: > 0 } ||
            consistency.Strips.All(strip => strip.Code != "ANA-TIP-PROVISIONAL"))
            throw new InvalidOperationException("run polar consistency and tip exemption did not reach projection: " +
                (consistency is null ? "null" : string.Join(",", consistency.Strips.Select(strip => strip.Code))));
        var rows = view.Groups.Single(group => group.Title == "Section (2D)").Rows;
        if (!rows.Any(row => row.Label == "Ncrit 2" && row.Value != Labels.NoPolar) ||
            !rows.Any(row => row.Label == "Ncrit 4" && row.Value != Labels.NoPolar))
            throw new InvalidOperationException("polar source did not reach Section projection");
    }

    private static void LowConfidenceDrag()
    {
        var solution = new LatticeSolution([0.1], [0.01], new RunDiagnostics(0, 1))
        {
            Strips = [new LatticeStrip(0, 0.1, 0.5, 0.12, 0.2, 0.1, 0.01, 0.2, 0, 0, 0.4, 0, 0.2)],
            Forces = [new StripForce(0, 0.1, 0.5, 0.12, 0.1, 0, 0, 10, 0, 0, 0)]
        };
        var polar = new ReynoldsPolar { LowConfidence = true };
        StripLoad coupled = StripCoupler.Couple([], solution, Fixture.Op(3), Fixture.Fresh, polar,
            _ => new string('a', 64), CancellationToken.None)[0];
        AnalysisChecks.Equal("ANA-POLAR-LOW-CONFIDENCE", coupled.CdNcrit2.FlagCode, "strip Ncrit 2 advisory");
        AnalysisChecks.Equal("ANA-POLAR-LOW-CONFIDENCE", coupled.CdNcrit4.FlagCode, "strip Ncrit 4 advisory");
        AnalysisRun run = ProjectionTests.Data(s => s with
        {
            CdNcrit2 = coupled.CdNcrit2, CdNcrit4 = coupled.CdNcrit4
        }).Run;
        foreach (StripValue value in new[] { Loads.ProfileDrag(run, 2), Loads.ProfileDrag(run, 4),
            Loads.WingDrag(run, 2), Loads.WingDrag(run, 4) })
            if (value.Value is null || value.FlagCode != "ANA-POLAR-LOW-CONFIDENCE")
                throw new InvalidOperationException("advisory confidence was dropped from a numeric drag sum");
        var projected = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric);
        foreach (string label in new[] { "Profile drag" })
            if (projected.Groups.Single(group => group.Title == "Loads").Rows.Single(row => row.Label == label)
                .Note?.Contains("Low confidence — analysis_confidence below 0.5 at " + run.Strips.Count + " strips", StringComparison.Ordinal) != true)
                throw new InvalidOperationException(label + " lost the confidence flag in projection (Ruling 118: names the strip count)");
    }

    private static void DragBandOrder()
    {
        AnalysisRun run = ProjectionTests.Data(s => s with
        {
            CdNcrit2 = new StripValue(0.03, null), CdNcrit4 = new StripValue(0.02, null)
        }).Run;
        var view = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric);
        AnalysisChecks.Equal("20.00–30.00", view.Groups.Single(g => g.Title == "Loads").Rows
            .Single(r => r.Label == "Profile drag").Value, "profile drag band ordered by value");
        foreach (string group in new[] { "Loads", "Wing result" })
        {
            ResultRow row = view.Groups.Single(g => g.Title == group).Rows.Single(r => r.Label == "Drag (Wing only)");
            AnalysisChecks.Equal("20.40–30.40", row.Value, group + " band ordered by drag value");
            ResultRow ratio = view.Groups.Single(g => g.Title == group).Rows.Single(r => r.Label == "Wing-only CL/CD");
            double[] values = ratio.Value.Split('–').Select(double.Parse).ToArray();
            if (values.Length != 2 || values[0] > values[1])
                throw new InvalidOperationException(group + " CL/CD band is not ascending by value");
        }
    }

    private static void PolarReRange()
    {
        AnalysisRun run = ProjectionTests.Data(s => s with
        {
            ReLocal = 543210, CdNcrit2 = new StripValue(0.02, null)
        }).Run;
        ResultRow row = AnalysisProjection.StripAt(AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric),
            run.Strips[0].Eta).Rows.Single(item => item.Label == "Polar Re range");
        if (!row.Value.Contains("2.00 × 10⁵ to 1.00 × 10⁶", StringComparison.Ordinal))   // COPY-322 names the strip Re and the validated bounds
            throw new InvalidOperationException("polar Re range displayed the strip Re instead of validated bounds: " + row.Value);
    }

    private sealed class ReynoldsPolar : IPolarSource
    {
        public readonly List<(double Re, double Ncrit)> Calls = [];
        public bool LowConfidence { get; init; }
        public string? UnavailableReason => null;
        public PolarResult? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg,
            WaterRecord water, CancellationToken cancellation)
        {
            Calls.Add((reynolds, ncrit));
            var sample = new PolarSample(profileHash, "stub", "1", reynolds, ncrit, "clean", alphaDeg,
                new string('b', 64), 0.4, reynolds / 1e8 + ncrit / 1e4, 0, null, null, null, null,
                LowConfidence ? 0.2 : 1, true);
            return new PolarResult(sample, [], LowConfidence, 0, 0);
        }
    }
}
