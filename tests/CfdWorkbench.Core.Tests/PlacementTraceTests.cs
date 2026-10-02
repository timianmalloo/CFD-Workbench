using System.Text;
using CfdWorkbench.Core;

namespace CfdWorkbench.Core.Tests;

internal static class PlacementTraceTests
{
    internal static void Run() =>
        IdentityTests.Check("PlacementRule_OperationTree_TraceGolden", OperationTreeMatchesGolden);

    // The operation strings of the one PlacementRule over the recording scalar, plus Select on fixed stations.
    internal static string Render()
    {
        var text = new StringBuilder();
        Trace upper = Trace.Leaf("U"), lower = Trace.Leaf("L"), reciprocal = Trace.Leaf("r"), thickness = Trace.Leaf("t");
        var (camber, unit) = PlacementRule.Components(upper, lower, reciprocal);
        text.AppendLine("components.camber " + camber.Text);
        text.AppendLine("components.unit " + unit.Text);
        var section = PlacementRule.Section(upper, lower, reciprocal, thickness);
        text.AppendLine("section.upper " + section.Upper.Text);
        text.AppendLine("section.lower " + section.Lower.Text);
        var blend = PlacementRule.Blend((Trace.Leaf("Ca"), Trace.Leaf("Ua")), (Trace.Leaf("Cb"), Trace.Leaf("Ub")),
            Trace.Leaf("w"), Trace.Leaf("r0"), thickness);
        text.AppendLine("blend.upper " + blend.Upper.Text);
        text.AppendLine("blend.lower " + blend.Lower.Text);
        var placed = PlacementRule.Place(Trace.Leaf("le"), Trace.Leaf("te"), Trace.Leaf("el"),
            (Trace.Leaf("sin"), Trace.Leaf("cos")), Trace.Leaf("x"), Trace.Leaf("z"));
        text.AppendLine("place.x " + placed.X.Text);
        text.AppendLine("place.z " + placed.Z.Text);
        double[] etas = [0, 0.5, 1];
        int[] profiles = [0, 1, 1];
        foreach (double eta in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            var (left, right) = PlacementRule.Select(etas, profiles, eta, (a, b) => a == b);
            text.AppendLine("select " + eta.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + left + " " + right);
        }
        return text.ToString();
    }

    private static void OperationTreeMatchesGolden()
    {
        string path = Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2", "placement-operation-trace.golden.txt");
        string expected = File.ReadAllText(path).ReplaceLineEndings("\n");
        string actual = Render().ReplaceLineEndings("\n");
        if (actual != expected)
            throw new InvalidOperationException(
                "The PlacementRule operation tree changed. The three hand-kept bound models (Geometry.cs QueryFeasibility, PlacementWidth, "
                + "BlendPlacementWidth) mirror this tree and must be hand-reviewed before the golden is updated (design 3D elevations §6, §13 OI-11).\n"
                + "expected:\n" + expected + "actual:\n" + actual);
    }
}
