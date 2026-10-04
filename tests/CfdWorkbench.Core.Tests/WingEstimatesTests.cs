using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class WingEstimatesTests
{
    internal static void Run()
    {
        Check("WingEstimates_Compute_EmitsEvent", () =>
        {
            var estimate = WingEstimates.From(ConstantChord(), "accepted", 4);
            Equal("estimates.compute", estimate.Compute.Name);
            Equal("ok", estimate.Compute.Outcome);
            Equal(true, estimate.Converged);
            Equal(true, double.IsFinite(estimate.Compute.DurationMilliseconds) && estimate.Compute.DurationMilliseconds >= 0);
            Equal(true, estimate.Compute.IntervalCount is > 0 and <= 8);
            Equal(true, estimate.Compute.Iterations >= 1);
            Equal("accepted", estimate.Basis);
            Equal(4L, estimate.Generation);
        });
        Check("WingEstimates_ConstantChord_AreaArMeanMac", () =>
        {
            var estimate = WingEstimates.From(ConstantChord(), "accepted", 0);
            Near(1.2, estimate.SpanMeters);
            Near(0.18, estimate.RootChordMeters);
            Near(0.18, estimate.TipChordMeters);
            Near(0.18, estimate.MeanChordMeters);
            Near(0.18, estimate.MacMeters);
            Near(0.216, estimate.AreaSquareMeters);
            Near(1.44 / 0.216, estimate.AspectRatio);
            Near(0.12, estimate.MaxThicknessRatio);
        });
        Check("WingEstimates_LinearTaper_MacDiffersFromMean", () =>
        {
            var estimate = WingEstimates.From(LinearTaper(), "accepted", 0);
            Near(1.0, estimate.SpanMeters);
            Near(0.20, estimate.RootChordMeters);
            Near(0.05, estimate.TipChordMeters);
            Near(0.125, estimate.AreaSquareMeters);
            Near(8.0, estimate.AspectRatio);
            Near(0.125, estimate.MeanChordMeters);
            Near(0.140, estimate.MacMeters);
            Equal(true, Math.Abs(estimate.MacMeters - estimate.MeanChordMeters) > 1e-4);
        });
        Check("WingEstimates_DraftBytes_DifferFromAccepted", () =>
        {
            using var session = new AuthoringSession();
            session.Open(ConstantChord(), Id(), false);
            var accepted = WingEstimates.From(session.Snapshot().Source, "accepted", 0);
            string draft = Id();
            session.BeginGestureDraft(draft, "trailing", "cv-4");
            var updated = session.GestureToAft(draft, 0, 0.30);
            var preview = WingEstimates.From(session.Snapshot().Draft!.Bytes, "preview", updated.Generation);
            Equal("preview", preview.Basis);
            Equal(updated.Generation, preview.Generation);
            Equal("accepted", accepted.Basis);
            Equal(true, preview.AreaSquareMeters != accepted.AreaSquareMeters);
            Equal(true, preview.MacMeters != accepted.MacMeters);
        });
        Check("WingEstimates_AnchorSymmetricTangent_MacConverges", () =>
        {
            // D-4 (docs/reviews/m12b-native.md §3.1): a short handle beside the anchor makes chord²(η) near-singular
            // at a piece end; the fixed Gauss ladder never agreed, so MAC and AR went NaN after the first edit.
            using var session = PointGestureTests.Open(FoilSource.NewDefault());
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("trailing", 0.45));
            var control = Planform.View(session.Snapshot().Source, "Accepted", 0).Trailing.Points.First(point => point.Role == PointRole.Control);
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("trailing", control.Id));
            session.ApplyPointCommand(Id(), new PointCommand.SetTangent("trailing",
                Planform.View(session.Snapshot().Source, "Accepted", 0).Trailing.Points.Single(p => p.Role == PointRole.Anchor).Id, TangentKind.Symmetric, null));
            var estimate = WingEstimates.From(session.Snapshot().Source.ToArray(), "accepted", 0);
            Console.WriteLine($"  conv={estimate.Converged} outcome={estimate.Compute.Outcome} mac={estimate.MacMeters} ar={estimate.AspectRatio} iterations={estimate.Compute.Iterations} ms={estimate.Compute.DurationMilliseconds:F2}");
            Equal(true, estimate.Converged);
            Equal("ok", estimate.Compute.Outcome);
            Equal(true, double.IsFinite(estimate.MacMeters) && double.IsFinite(estimate.AspectRatio));
            Equal(true, estimate.MeanChordMeters <= estimate.MacMeters && estimate.MacMeters <= estimate.RootChordMeters);
        });
        Check("WingEstimates_Save_NoEstimateInFile", () =>
        {
            using var session = new AuthoringSession();
            session.Open(ConstantChord(), Id(), false);
            byte[] source = session.Snapshot().Source.ToArray();
            _ = WingEstimates.From(source, "accepted", 0);
            byte[] image = session.SaveImage();
            Equal(true, source.AsSpan().SequenceEqual(session.Snapshot().Source));
            AssertAbsent(Encoding.UTF8.GetString(source), "derived_check", "mean_chord", "mac", "aspect", "root_chord", "tip_chord", "max_thickness");
            var names = new List<string>();
            using var document = System.Text.Json.JsonDocument.Parse(image);
            Walk(document.RootElement, names);
            AssertAbsent(string.Join("\n", names), "derived_check", "derivedCheck", "meanChord", "mean_chord", "mac", "aspectRatio", "rootChord", "tipChord", "maxThickness", "area");
        });
    }

    private static string Id() => Guid.NewGuid().ToString("D");

    private static void Near(double expected, double actual)
    {
        double scale = Math.Max(1e-15, Math.Abs(expected));
        if (Math.Abs(actual - expected) / scale > 1e-6)
            throw new InvalidOperationException($"Expected {expected}; actual {actual}");
    }

    private static void AssertAbsent(string text, params string[] words)
    {
        foreach (string word in words)
            if (text.Contains(word, StringComparison.Ordinal))
                throw new InvalidOperationException("Stored estimate field " + word);
    }

    private static void Walk(System.Text.Json.JsonElement element, List<string> names)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) { names.Add(property.Name); Walk(property.Value, names); }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) Walk(item, names);
    }

    private static byte[] ConstantChord() => Foil("m", "0.6", Flat(0), Flat(0.18), Flat(0), Flat(0), Flat(0.12),
        "at root profile \"section-a\" at tip profile \"section-a\"");

    private static byte[] LinearTaper()
    {
        double[] knots = [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1];
        var greville = new double[7];
        for (int i = 0; i < greville.Length; i++) greville[i] = (knots[i + 1] + knots[i + 2] + knots[i + 3]) / 3.0;
        string leading = Points(greville, greville.Select(_ => 0d).ToArray());
        string trailing = Points(greville, greville.Select(eta => 0.20 - 0.15 * eta).ToArray());
        return Foil("m", "0.5", leading, trailing, Flat(0), Flat(0), Flat(0.12),
            "at root profile \"section-a\" at tip profile \"section-a\"");
    }

    private static string Flat(double ordinate) =>
        Points([0, 0.1, 0.3, 0.5, 0.7, 0.9, 1], [ordinate, ordinate, ordinate, ordinate, ordinate, ordinate, ordinate]);

    private static string Points(double[] abscissae, double[] ordinates) =>
        string.Join(", ", abscissae.Zip(ordinates, (x, y) =>
            "(" + x.ToString("R", CultureInfo.InvariantCulture) + ", " + y.ToString("R", CultureInfo.InvariantCulture) + ")"));

    private static byte[] Foil(string unit, string half, string leading, string trailing, string dihedral, string twist, string thickness, string sections)
    {
        const string knots = "0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1";
        const string ids = "\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"";
        string curve(string points) => "cv { degree 3 knots [" + knots + "] points [" + points + "] ids [" + ids + "] }";
        string text =
            "foildsl \"4.0\"\n" +
            "foil \"Estimate fixture\" {\n" +
            "  units " + unit + "\n" +
            "  half_span " + half + " " + unit + "\n" +
            "  evaluator \"cfdw-cv\" \"2\"\n" +
            "  symmetry mirror_y\n" +
            "  planform {\n" +
            "    leading " + curve(leading) + "\n" +
            "    trailing " + curve(trailing) + "\n" +
            "  }\n" +
            "  dihedral " + curve(dihedral) + "\n" +
            "  twist " + curve(twist) + "\n" +
            "  thickness " + curve(thickness) + "\n" +
            "  profiles {\n" +
            "    profile \"section-a\" {\n" +
            "      upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.025), (0.15, 0.055), (0.35, 0.065), (0.55, 0.05), (0.75, 0.03), (0.9, 0.01), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\", \"cv-7\"] }\n" +
            "      lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.025), (0.15, -0.055), (0.35, -0.065), (0.55, -0.05), (0.75, -0.03), (0.9, -0.01), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\", \"cv-7\"] }\n" +
            "    }\n" +
            "  }\n" +
            "  sections { " + sections + " }\n" +
            "}\n";
        return Encoding.UTF8.GetBytes(text);
    }
}
