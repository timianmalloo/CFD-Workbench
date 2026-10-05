using System.Diagnostics;
using System.Globalization;
using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class NeuralFoilTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("NeuralFoil_Fidelity_Python032", Fidelity);
        AnalysisChecks.Check("NeuralFoil_CorruptWeights_Refused", CorruptWeights);
        AnalysisChecks.Check("NeuralFoil_Envelope_Alpha", () => Refused(30, 500000, 4, "naca0012", false, "alpha"));
        AnalysisChecks.Check("NeuralFoil_Envelope_Re", () => Refused(0, 10000, 4, "naca0012", false, "Re"));
        AnalysisChecks.Check("NeuralFoil_Envelope_Ncrit", () => Refused(0, 500000, 0, "naca0012", false, "Ncrit"));
        AnalysisChecks.Check("NeuralFoil_Envelope_Family", () => Refused(0, 500000, 4, "edited", false, "family"));
        AnalysisChecks.Check("NeuralFoil_Envelope_CstResidual", () => Refused(0, 500000, 4, "naca0012", true, "CST"));
        AnalysisChecks.Check("NeuralFoil_Envelope_OutsideValidatedBracket", OutsideBracket);
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
        var cst = NeuralFoilCaseTable.Rows[0];
        var parameters = new CstParameters(cst.Upper, cst.Lower, cst.Leading, cst.Trailing);
        double[] x = Enumerable.Range(0, 200).Select(i => 0.5 * (1 - Math.Cos(Math.PI * i / 199))).ToArray();
        double[] upper = x.Select(value => CstFit.Ordinate(parameters, value, upper: true)).ToArray();
        double[] lower = x.Select(value => CstFit.Ordinate(parameters, value, upper: false)).ToArray();
        if (badResidual) upper[100] += 0.03;
        return new NeuralFoilSection("fixture", family, x, upper, lower);
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
        NeuralFoilEvaluation result = source.Evaluate(Section(false, "naca0012"), 7, 500000, 4, CancellationToken.None);
        if (!result.Computable || !result.OutsideValidatedBracket || result.Prediction is null)
            throw new Exception("outside-bracket prediction or flag missing");
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
