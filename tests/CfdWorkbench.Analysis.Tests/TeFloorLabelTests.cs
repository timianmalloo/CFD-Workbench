using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
        // Split by TFH (the finding: the Windows ring fails the first golden and never reaches the others). Each reports alone;
        // a mismatch prints DIAG lines (canonical JCS text, station bits) that the other platform's output is diffed against.
        Check("TeFloor_B10_SettingsHash_Golden_Default", () => GoldenOrDiagnose("default", DefaultHash, Settings.Default));
        Check("TeFloor_B10_SettingsHash_Golden_Small", () => GoldenOrDiagnose("small", SmallHash,
            Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
        Check("TeFloor_B10_SettingsHash_Golden_NoStations", () => GoldenOrDiagnose("nostations", NoStationsHash,
            Settings.Default with { SectionEtas = null, SectionXs = null }));
        Check("TeFloor_B10_SettingsHash_DiagnosticTextIsTheHashedText", () =>
        {
            // The diagnostic must print exactly what the hash reads, or a diff of it proves nothing.
            Equal(RunRecord.SettingsHash(Settings.Default), Identity.Blake3(Encoding.UTF8.GetBytes(CanonicalText(Settings.Default))));
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

    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // The text RunRecord.SettingsHash hashes: JCS of the camelCase JSON of the settings. Mirrors RunRecord.Canonical (private).
    private static string CanonicalText(RunSettings settings) => Jcs.Write(Tree(JsonSerializer.SerializeToElement(settings, Options)));

    private static object? Tree(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(member => member.Name, member => Tree(member.Value), StringComparer.Ordinal) as IDictionary<string, object?>,
        JsonValueKind.Array => element.EnumerateArray().Select(Tree).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static void GoldenOrDiagnose(string label, string golden, RunSettings settings)
    {
        string actual = RunRecord.SettingsHash(settings);
        if (actual != golden)
        {
            // No path, host or identity in these lines: settings numbers only. Station bits show a last-bit difference exactly.
            Console.WriteLine($"DIAG {label} runtime={RuntimeInformation.RuntimeIdentifier} arch={RuntimeInformation.ProcessArchitecture} hash={actual}");
            Console.WriteLine($"DIAG {label} jcs-without-stations=" + Jcs.Write(Tree(JsonSerializer.SerializeToElement(settings with { SectionEtas = null, SectionXs = null }, Options))));
            WriteBits(label, "etas", settings.SectionEtas);
            WriteBits(label, "xs", settings.SectionXs);
            Console.WriteLine($"DIAG {label} jcs-sha256=" + Identity.Sha256(Encoding.UTF8.GetBytes(CanonicalText(settings))));
        }
        Equal(golden, actual, label);
    }

    private static void WriteBits(string label, string name, IReadOnlyList<double>? values)
    {
        if (values is null) { Console.WriteLine($"DIAG {label} {name} none"); return; }
        for (int i = 0; i < values.Count; i++)
            Console.WriteLine($"DIAG {label} {name}[{i}] {BitConverter.DoubleToInt64Bits(values[i]):x16} {Jcs.Number(values[i])}");
    }
}
