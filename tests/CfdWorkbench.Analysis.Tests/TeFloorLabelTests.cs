using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// B10 (docs/design/export.md section 12, Ruling 195): the trailing-edge floor carries one named label constant and the
/// analysis settings hash and numbers do not move. Ring: fast, every push. Cost: 0.2 s cold for the first (JIT), under 1 ms for the others.
/// The three hashes below were read from the code before the label constant was added (the golden).
/// </summary>
internal static class TeFloorLabelTests
{
    // Settings.Default, the small one the Desktop checks use, and Default without stations: captured on main affddb89 before B10.
    private const string DefaultHash = "9713863597250cd245c4d70560abee8840c002f6ff33501127ba331ac8efad62";
    private const string SmallHash = "aa39f244f4235616ded920b58a1599a768765de16f74ba76e225cbe9c81543d7";
    private const string NoStationsHash = "9b402c74273eea9dcd0c3ca47cdcced2ec6764536434e462ba837150281fe58a";

    internal static void Run()
    {
        Check("TeFloor_B10_SettingsHash_GoldenUnchanged", () =>
        {
            Equal(DefaultHash, RunRecord.SettingsHash(Settings.Default));
            Equal(SmallHash, RunRecord.SettingsHash(Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
            Equal(NoStationsHash, RunRecord.SettingsHash(Settings.Default with { SectionEtas = null, SectionXs = null }));
        });
        Check("TeFloor_B10_Numerics_DefaultRecordFieldsUnchanged", () =>
        {
            var expected = Settings.WithStations(new RunSettings(64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, new[] { 2, 4 }, "clean", 0.3));
            Equal(RunRecord.SettingsHash(expected), RunRecord.SettingsHash(Settings.Default));
            Equal(0.3, Settings.TrailingEdgeFloorMm);
            Equal(Settings.TrailingEdgeFloorMm, Settings.Default.TeFloorMm);
        });
        Check("TeFloor_B10_Label_OneNamedConstant_AppDefaultNoSource", () =>
        {
            Equal("app default, no source", Settings.TrailingEdgeFloorLabel);
            Equal(false, Settings.TrailingEdgeFloorLabel.Contains("practitioner", StringComparison.OrdinalIgnoreCase));
        });
    }
}
