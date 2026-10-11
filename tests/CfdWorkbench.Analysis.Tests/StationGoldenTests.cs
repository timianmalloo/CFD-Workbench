using System.Globalization;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Ruling 214 control 4.2 (class CRT-GOLDEN): the section stations in the run key and the panel chord grid in the polar profile key
/// are built from double.CosPi, whose bits are pinned in Fixtures/stations/station-cospi.tsv for every argument set in use
/// (i/128, k/4, i/8, i/100, i/200). The check compares bits exactly. A row that differs on another host means the cosine kernel
/// is copied into Core (Ruling 156 (2)). Ring: fast, every push. Cost: under 20 ms (one panel sample per set).
/// </summary>
internal static class StationGoldenTests
{
    internal static void Run()
    {
        Check("StationGolden_CosPiBits_MatchFixture", CosPiBits);
        Check("StationGolden_SpanEtas_AreMinusCosPiBits", SpanEtas);
        Check("StationGolden_ChordXs_AreHalfOneMinusCosPiBits", ChordXs);
        Check("StationGolden_PanelGrid_IsOneMinusCosPiOverTwoBits", PanelGrid);
        Check("StationGolden_LatticeSpanStations_EqualSettingsEtasBits", LatticeSpan);
        Check("StationGolden_LatticeChordFractions_EqualSettingsChordXsBits", LatticeChord);
        Check("StationGolden_LoadsStripWidthFallback_UsesCosPiEdges", LoadsWidth);
    }

    private static Dictionary<int, double[]> Golden()
    {
        string path = Path.Combine(StripFixtureTests.RepoRoot(), "tests", "CfdWorkbench.Analysis.Tests", "Fixtures", "stations", "station-cospi.tsv");
        var sets = new Dictionary<int, double[]>();
        foreach (string row in File.ReadAllLines(path).Where(line => line.Length > 0 && line[0] != '#'))
        {
            string[] field = row.Split('\t');
            int n = int.Parse(field[0], CultureInfo.InvariantCulture), i = int.Parse(field[1], CultureInfo.InvariantCulture);
            if (!sets.TryGetValue(n, out double[]? values)) sets[n] = values = new double[n + 1];
            values[i] = BitConverter.Int64BitsToDouble(long.Parse(field[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }
        Equal("4,8,100,128,200", string.Join(",", sets.Keys.Order()), "argument sets in the fixture");
        return sets;
    }

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);

    private static void CosPiBits()
    {
        foreach (var (n, cosines) in Golden())
            for (int i = 0; i <= n; i++)
                Equal(Bits(cosines[i]), Bits(double.CosPi(i / (double)n)), $"CosPi({i}/{n}) bits");
    }

    // The span edges are -CosPi(i/n); the output keeps those at or above the root (the half-span) and drops duplicates within 1e-12.
    private static void SpanEtas()
    {
        var golden = Golden();
        foreach (int nPerHalf in new[] { 64, 50, 100, 4 })
        {
            int n = 2 * nPerHalf;
            if (!golden.TryGetValue(n, out double[]? cosines)) continue;
            var etas = Settings.SpanEtas(nPerHalf, "cosine");
            for (int i = n / 2 + 1; i <= n; i++)
                Equal(true, etas.Any(eta => Bits(eta) == Bits(-cosines[i])), $"eta edge {i}/{n} is -CosPi bits");
        }
    }

    private static void ChordXs()
    {
        var golden = Golden();
        foreach (int nChord in new[] { 4, 8 })
        {
            var xs = Settings.ChordXs(nChord, "cosine");
            for (int k = 0; k <= nChord; k++)
                Equal(true, xs.Any(x => Bits(x) == Bits(0.5 * (1 - golden[nChord][k]))), $"chord edge {k}/{nChord} is 0.5*(1-CosPi) bits");
        }
    }

    // Track LSP (Ruling 214 item 3 follow-up): the lattice span stations are the Settings etas, bit for bit, for the same n and spacing.
    // A unit span [-1, 1] makes mid = 0 and half = 1, so y[i] must be the Settings edge -CosPi(i/n) exactly.
    private static void LatticeSpan()
    {
        foreach (int nPerHalf in new[] { 64, 50, 100, 4 })
        {
            int n = 2 * nPerHalf;
            var etas = Settings.SpanEtas(nPerHalf, "cosine");
            double[] y = VortexLattice.SpanStations(-1, 1, nPerHalf, "cosine");
            for (int i = n / 2 + 1; i <= n; i++)
                Equal(true, etas.Any(eta => Bits(eta) == Bits(y[i])), $"lattice span station {i}/{n} equals a Settings eta bit for bit");
        }
    }

    // Fractions is private; the chord fractions are the edges Settings.ChordXs keeps.
    private static void LatticeChord()
    {
        var fractions = typeof(VortexLattice).GetMethod("Fractions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        foreach (int nChord in new[] { 4, 8 })
        {
            var xs = Settings.ChordXs(nChord, "cosine");
            var f = (double[])fractions.Invoke(null, new object[] { nChord, "cosine" })!;
            for (int k = 0; k <= nChord; k++)
                Equal(true, xs.Any(x => Bits(x) == Bits(f[k])), $"lattice chord fraction {k}/{nChord} equals a Settings chord x bit for bit");
        }
    }

    // Loads.StripWidth without stored edges (older lattices) uses the spacing law: (eta[j+1] - eta[j]) * b / 2 with eta = -CosPi(i/total).
    private static void LoadsWidth()
    {
        AnalysisRun run = ProjectionTests.Data().Run;
        int total = 2 * run.Settings.NSpanPerHalf;
        for (int j = 0; j < total; j++)
        {
            double expected = (-double.CosPi((j + 1) / (double)total) - -double.CosPi(j / (double)total)) * run.Reference.BRef / 2;
            Equal(Bits(expected), Bits(Loads.StripWidth(run, run.Strips[j])), $"strip width {j}/{total} bits");
        }
    }

    // Panel counts 16, 200, 400 give half counts 8, 100, 200.
    private static void PanelGrid()
    {
        var golden = Golden();
        byte[] source = File.ReadAllBytes(Path.Combine(StripFixtureTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil"));
        foreach (int half in new[] { 8, 100, 200 })
        {
            var x = PanelMethod.SampleSection(source, 0.0, 2 * half).X;
            Equal(half + 1, x.Count, "grid size");
            for (int i = 0; i <= half; i++)
                Equal(Bits((1 - golden[half][i]) / 2), Bits(x[i]), $"panel x {i}/{half} bits");
        }
    }
}
