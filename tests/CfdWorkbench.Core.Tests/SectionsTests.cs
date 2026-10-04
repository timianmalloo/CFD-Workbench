using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Ring: C0 (every join, Core harness). Cost: the bitwise check is one Surface plus one Sections on the
// pinned m12b2 foils; the counter check is 8 threads. Both read repository foils through PlacementTests.RepoRoot.
internal static class SectionsTests
{
    internal static void Run()
    {
        Check(nameof(Sections_PlaceEqualsSurfaceMidline_Bitwise), Sections_PlaceEqualsSurfaceMidline_Bitwise);
        Check(nameof(Placement_Counters_ExactUnderConcurrentSections), Placement_Counters_ExactUnderConcurrentSections);
    }

    // FM-2. PlacedCamber is PlacementRule.Place of the Section/Blend camber, so it matches the drawn
    // upper/lower midline at the same η and the same cosine x. An own placement formula goes red here.
    private static void Sections_PlaceEqualsSurfaceMidline_Bitwise()
    {
        Equal("foildsl-6/1", Placement.PlacementRuleVersion);
        string root = PlacementTests.RepoRoot();
        string folder = Path.Combine(root, "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2");
        foreach (string name in new[] { "example", "blended-dihedral", "blended-peaks", "c0-peak", "chord-2m" })
            SameMidline(Path.Combine(folder, name + ".foil"), 11, 21);
        SameMidline(Path.Combine(folder, "example.foil"), 41, 101);
        SameMidline(Path.Combine(folder, "blended-dihedral.foil"), 41, 101);

        byte[] foil = File.ReadAllBytes(Path.Combine(folder, "example.foil"));
        Refuses("DSL-RANGE", () => Placement.Sections(foil, [], [0d], CancellationToken.None));
        Refuses("DSL-RANGE", () => Placement.Sections(foil, [1.1], [0.5], CancellationToken.None));
        Refuses("DSL-RANGE", () => Placement.Sections(foil, [0.5], [-0.01], CancellationToken.None));
        byte[] section = File.ReadAllBytes(Path.Combine(root, "docs", "examples", "foildsl", "section-basic.foil"));
        Refuses("DSL-PATCH", () => Placement.Sections(section, [0d], [0d], CancellationToken.None));

        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        try
        {
            Placement.Sections(foil, [0.5], [0.5], cancel.Token);
            throw new InvalidOperationException("Sections ran after cancellation.");
        }
        catch (OperationCanceledException) { }
    }

    // FM-12. Eight threads call Sections together. Interlocked counters equal the single-call count times
    // the calls; restoring ++ loses updates.
    private static void Placement_Counters_ExactUnderConcurrentSections()
    {
        byte[] source = File.ReadAllBytes(Path.Combine(PlacementTests.RepoRoot(),
            "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2", "example.foil"));
        double[] xs = Cosine(17);
        double[] etas = [0d, 0.25, 0.5, 0.75, 1d];
        Placement.ResetEvaluatorCounts();
        _ = Placement.Sections(source, etas, xs, CancellationToken.None);
        int channelOnce = Placement.ChannelEvaluations;
        int profileOnce = Placement.ProfileEvaluations;
        Equal(true, channelOnce > 0 && profileOnce > 0);

        const int threads = 8;
        const int repeats = 2;
        Placement.ResetEvaluatorCounts();
        using var start = new Barrier(threads);
        var errors = new Exception?[threads];
        var workers = new Thread[threads];
        for (int index = 0; index < threads; index++)
        {
            int id = index;
            workers[id] = new Thread(() =>
            {
                try
                {
                    start.SignalAndWait();
                    for (int n = 0; n < repeats; n++)
                        _ = Placement.Sections(source, etas, xs, CancellationToken.None);
                }
                catch (Exception error) { errors[id] = error; }
            });
            workers[id].Start();
        }
        foreach (Thread worker in workers) worker.Join();
        foreach (Exception? error in errors)
            if (error is not null) throw error;

        int expectedChannel = channelOnce * threads * repeats;
        int expectedProfile = profileOnce * threads * repeats;
        Console.WriteLine("MEASURE concurrent_sections channel=" + Placement.ChannelEvaluations
            + " profile=" + Placement.ProfileEvaluations
            + " expected_channel=" + expectedChannel + " expected_profile=" + expectedProfile);
        Equal(expectedChannel, Placement.ChannelEvaluations);
        Equal(expectedProfile, Placement.ProfileEvaluations);
    }

    private static void SameMidline(string path, int stations, int chordSamples)
    {
        byte[] source = File.ReadAllBytes(path);
        var view = Placement.Surface(source, "accepted", 0, CancellationToken.None, stations, chordSamples);
        double[] xs = Cosine(chordSamples);
        double[] etas = view.Sections.Select(section => section.Eta).ToArray();
        int budget = ProofBudget.Entries;
        var samples = Placement.Sections(source, etas, xs, CancellationToken.None);
        Equal(budget, ProofBudget.Entries);
        Equal(view.Sections.Count, samples.Count);
        string foil = Path.GetFileName(path);
        for (int station = 0; station < samples.Count; station++)
        {
            var drawn = view.Sections[station];
            var sample = samples[station];
            Equal(drawn.Eta, sample.Frame.Eta);
            Equal(drawn.Upper.Count, sample.PlacedCamber.Count);
            for (int index = 0; index < xs.Length; index++)
            {
                var upper = drawn.Upper[index];
                var lower = drawn.Lower[index];
                var camber = sample.PlacedCamber[index];
                string where = foil + " s" + station + " i" + index;
                SameBits(where + " X", (upper.X + lower.X) / 2, camber.X);
                SameBits(where + " Y", (upper.Y + lower.Y) / 2, camber.Y);
                SameBits(where + " Z", (upper.Z + lower.Z) / 2, camber.Z);
                if (!double.IsFinite(sample.Camber[index]) || !double.IsFinite(sample.Thickness[index]) || !double.IsFinite(sample.CamberSlope[index]))
                    throw new InvalidOperationException(where + " section sample is not finite");
            }
        }
    }

    // Same cosine stations as Placement.ChordSamples, so Sections sees the x Surface placed.
    private static double[] Cosine(int count)
    {
        var xs = new double[count];
        double step = count - 1;
        for (int index = 0; index < count; index++)
            xs[index] = (1 - Math.Cos(Math.PI * index / step)) / 2;
        xs[0] = 0;
        xs[^1] = 1;
        return xs;
    }

    private static void SameBits(string where, double expected, double actual)
    {
        ulong left = BitConverter.DoubleToUInt64Bits(expected);
        ulong right = BitConverter.DoubleToUInt64Bits(actual);
        if (left != right)
            throw new InvalidOperationException(where + " bits " + left.ToString("x16") + " -> " + right.ToString("x16"));
    }
}
