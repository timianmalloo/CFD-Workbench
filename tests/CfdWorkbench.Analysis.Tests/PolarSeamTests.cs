using System.Text.Json;
using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class PolarSeamTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("PolarSample_DerivedFlags_EqualEvaluateAtWriteTime", DerivedFlags);
    }

    private static void DerivedFlags()
    {
        var section = Naca0012Reference.FromSelig(new string('a', 64), "catalog", CatalogGenerator.Naca4("0012"));
        var source = new NeuralFoilPolarSource(hash => hash == section.ProfileHash ? section : null);
        var water = new WaterRecord(15, 0, 999, 1e-6, 1700, "fixture", new string('b', 64));
        NeuralFoilEvaluation atWrite = source.Evaluate(section, 7, 500000, 4, CancellationToken.None);
        object? returned = source.Sample(section.ProfileHash, 500000, 4, 7, water, CancellationToken.None);
        if (returned is null || returned.GetType().Name != "PolarResult")
            throw new InvalidOperationException("Sample must return a non-persisted result carrying the derived flags");
        var stored = (PolarSample)returned.GetType().GetProperty("Sample")!.GetValue(returned)!;
        var reasons = (IReadOnlyList<string>)returned.GetType().GetProperty("OutsideBracketReasons")!.GetValue(returned)!;
        AnalysisChecks.Equal(string.Join("|", atWrite.OutsideBracketReasons), string.Join("|", reasons), "write-time bracket flags");
        AnalysisChecks.Equal(atWrite.LowConfidence, (bool)returned.GetType().GetProperty("LowConfidence")!.GetValue(returned)!, "write-time confidence flag");
        string json = JsonSerializer.Serialize(stored);
        if (json.Contains("OutsideBracket", StringComparison.Ordinal) || json.Contains("CstResidual", StringComparison.Ordinal))
            throw new InvalidOperationException("derived metadata was persisted");
    }
}
