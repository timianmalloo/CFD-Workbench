using System.Diagnostics;
using System.Globalization;
using System.Diagnostics.Metrics;
using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class NeuralFoilTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("NeuralFoil_Fidelity_Python032", Fidelity);
        AnalysisChecks.Check("NeuralFoil_CorruptWeights_Refused", CorruptWeights);
        AnalysisChecks.Check("NeuralFoil_CstFit_RecoversSection", CstFitRecoversSection);
        AnalysisChecks.Check("NeuralFoil_Source_ProducesPolarSample", ProducesPolarSample);
        AnalysisChecks.Check("NeuralFoil_Source_NoncomputableNamesReason", NoncomputableNamesReason);
        AnalysisChecks.Check("NeuralFoil_Envelope_Alpha", () => Refused(30, 500000, 4, "naca0012", false, "alpha"));
        AnalysisChecks.Check("NeuralFoil_Envelope_Re", () => Refused(0, 10000, 4, "naca0012", false, "Re"));
        AnalysisChecks.Check("NeuralFoil_Envelope_Ncrit", () => Refused(0, 500000, 0, "naca0012", false, "Ncrit"));
        AnalysisChecks.Check("NeuralFoil_Envelope_Family", () => Refused(0, 500000, 4, "edited", false, "family"));
        AnalysisChecks.Check("NeuralFoil_Envelope_CstResidual", () => Refused(0, 500000, 4, "naca0012", true, "CST"));
        AnalysisChecks.Check("NeuralFoil_Envelope_OutsideValidatedBracket", OutsideBracket);
        AnalysisChecks.Check("NeuralFoil_Confidence_BelowFloorNonComputable", ConfidenceGate);
        AnalysisChecks.Check("NeuralFoil_Telemetry_EmittedOnNormalPath", Telemetry);
        AnalysisChecks.Check("NeuralFoil_InferenceCost", InferenceCost);
    }

    private static void Fidelity()
    {
        NeuralFoilNetwork network = NeuralFoilNetwork.FromEmbedded();
        double maxDifference = 0;
        foreach (NeuralFoilCaseTable.Case row in NeuralFoilCaseTable.Rows)
        {
            var parameters = new CstParameters(row.Upper, row.Lower, row.Leading, row.Trailing);
            NeuralFoilPrediction value = network.Predict(parameters, row.Alpha, row.Reynolds, row.Ncrit);
            double[] expected = [row.Confidence, row.Cl, row.Cd, row.Cm, row.XtrUpper, row.XtrLower];
            double[] actual = [value.AnalysisConfidence, value.Cl, value.Cd, value.Cm, value.XtrUpper, value.XtrLower];
            for (int i = 0; i < expected.Length; i++)
            {
                double difference = Math.Abs(expected[i] - actual[i]);
                maxDifference = Math.Max(maxDifference, difference);
                if (difference > 1e-10) throw new Exception($"{row.Name} output {i} difference {difference:G17}");
            }
        }
        Console.WriteLine("NeuralFoil fidelity max difference " + maxDifference.ToString("G17", CultureInfo.InvariantCulture));
    }

    private static void CorruptWeights()
    {
        using Stream stream = typeof(NeuralFoilNetwork).Assembly.GetManifestResourceStream(NeuralFoilNetwork.ResourceName)!;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        byte[] bytes = copy.ToArray();
        bytes[^1] ^= 1;
        try { NeuralFoilNetwork.FromBytes(bytes); }
        catch (ContractError error) when (error.Code == "ANA-POLAR-WEIGHTS-HASH") { return; }
        throw new Exception("corrupted weights were accepted");
    }

    private static NeuralFoilSection Section(bool badResidual, string family)
    {
        var cst = family switch
        {
            "naca2412" => NeuralFoilCaseTable.Rows[3],
            "naca4412" => NeuralFoilCaseTable.Rows[4],
            _ => NeuralFoilCaseTable.Rows[0]
        };
        var parameters = new CstParameters(cst.Upper, cst.Lower, cst.Leading, cst.Trailing);
        double[] x = Enumerable.Range(0, 200).Select(i => 0.5 * (1 - Math.Cos(Math.PI * i / 199))).ToArray();
        double[] upper = x.Select(value => CstFit.Ordinate(parameters, value, upper: true)).ToArray();
        double[] lower = x.Select(value => CstFit.Ordinate(parameters, value, upper: false)).ToArray();
        if (badResidual) upper[100] += 0.03;
        return new NeuralFoilSection(new string('a', 64), family, x, upper, lower);
    }

    private static void CstFitRecoversSection()
    {
        NeuralFoilSection section = Section(false, "naca2412");
        CstFitResult fit = CstFit.Fit(section);
        if (fit.MaxResidual > 1e-8 || fit.RmsResidual > 1e-8)
            throw new Exception($"exact CST section fit residual {fit.MaxResidual:G17}");
        var expected = NeuralFoilCaseTable.Rows[3];
        if (Math.Abs(fit.Parameters.Upper[2] - expected.Upper[2]) > 1e-6 ||
            Math.Abs(fit.Parameters.TrailingEdge - expected.Trailing) > 1e-6)
            throw new Exception("CST coefficient recovery changed");
    }

    private static void ProducesPolarSample()
    {
        NeuralFoilSection section = Section(false, "naca0012");
        IPolarSource source = new NeuralFoilPolarSource(hash => hash == section.ProfileHash ? section : null);
        var water = new WaterRecord(15, 0, 999, 1e-6, 1000, "fixture", new string('b', 64));
        PolarSample? sample = source.Sample(section.ProfileHash, 1000000, 4, 0, water, CancellationToken.None);
        var expected = NeuralFoilCaseTable.Rows[1];
        if (sample is null || Math.Abs(sample.Cl!.Value - expected.Cl) > 1e-8 ||
            Math.Abs(sample.Cd!.Value - expected.Cd) > 1e-8 ||
            Math.Abs(sample.Confidence!.Value - expected.Confidence) > 1e-8 ||
            sample.MethodVersion != NeuralFoilNetwork.MethodVersion || sample.WaterHash.Length != 64)
            throw new Exception("polar source lost a coefficient, confidence, or method key");
    }

    private static void NoncomputableNamesReason()
    {
        NeuralFoilSection section = Section(false, "naca0012");
        IPolarSource source = new NeuralFoilPolarSource(_ => section);
        var water = new WaterRecord(15, 0, 999, 1e-6, 1000, "fixture", new string('b', 64));
        try { source.Sample(section.ProfileHash, 500000, 4, 30, water, CancellationToken.None); }
        catch (ContractError error) when (error.Code == "ANA-POLAR-NONCOMPUTABLE" && error.Reason?.Contains("alpha") == true)
        {
            return;
        }
        throw new Exception("IPolarSource silently dropped a non-computable point");
    }

    private static void Refused(double alpha, double reynolds, double ncrit, string family, bool badResidual, string reason)
    {
        var source = new NeuralFoilPolarSource(_ => Section(badResidual, family));
        NeuralFoilEvaluation result = source.Evaluate(Section(badResidual, family), alpha, reynolds, ncrit, CancellationToken.None);
        if (result.Computable || result.Reason is null || !result.Reason.Contains(reason, StringComparison.OrdinalIgnoreCase))
            throw new Exception("point was not marked non-computable: " + result.Reason);
        if (!double.IsFinite(result.CstResidualMax)) throw new Exception("CST residual missing");
    }

    private static void OutsideBracket()
    {
        var source = new NeuralFoilPolarSource(_ => Section(false, "naca0012"));
        NeuralFoilEvaluation result = source.Evaluate(Section(false, "naca2412"), 3, 500000, 5, CancellationToken.None);
        if (!result.Computable || !result.OutsideValidatedBracket || result.Prediction is null)
            throw new Exception("outside-bracket prediction or flag missing");
    }

    private static void ConfidenceGate()
    {
        if (NeuralFoilPolarSource.ConfidenceReason(0.49) is null ||
            NeuralFoilPolarSource.ConfidenceReason(0.5) is not null)
            throw new Exception("advisory confidence floor does not gate the polar point");
    }

    private static void Telemetry()
    {
        int durations = 0, calls = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, self) =>
        {
            if (instrument.Meter.Name == "CfdWorkbench.Analysis.NeuralFoil") self.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
        {
            if (instrument.Name == "neuralfoil.evaluate.duration_ms") durations++;
        });
        listener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
        {
            if (instrument.Name == "neuralfoil.evaluate.calls") calls++;
        });
        listener.Start();
        NeuralFoilSection section = Section(false, "naca0012");
        var source = new NeuralFoilPolarSource(_ => section);
        source.Evaluate(section, 0, 500000, 4, CancellationToken.None);
        if (durations != 1 || calls != 1) throw new Exception($"telemetry missing: durations={durations}, calls={calls}");
    }

    private static void InferenceCost()
    {
        var row = NeuralFoilCaseTable.Rows[0];
        var parameters = new CstParameters(row.Upper, row.Lower, row.Leading, row.Trailing);
        NeuralFoilNetwork network = NeuralFoilNetwork.FromEmbedded();
        for (int i = 0; i < 3; i++) network.Predict(parameters, 0, 500000, 4);
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++) network.Predict(parameters, 0, 500000, 4);
        watch.Stop();
        Console.WriteLine("COST NeuralFoil_InferencePerCall " + (watch.Elapsed.TotalMilliseconds / 10).ToString("F3", CultureInfo.InvariantCulture));
    }
}
