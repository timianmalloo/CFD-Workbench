using System.Diagnostics;
using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class PointModelTests
{
    private static string Fx(string name) => M12bFixtures.Path(name);
    private const string Example = "docs/examples/foildsl/foil-basic.foil";

    internal static void Run()
    {
        Check("PointModel_DefaultExample_RolesEndsHandlesControls", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Example), "accepted", 7);
            Equal("4.0", view.Version);
            Equal(10, view.Leading.Ceiling);
            Equal("accepted", view.Basis);
            Equal(7L, view.Generation);
            PointRole[] roles = [PointRole.RootEnd, PointRole.RootHandle, PointRole.Control, PointRole.Control, PointRole.Control, PointRole.TipHandle, PointRole.TipEnd];
            for (int index = 0; index < roles.Length; index++)
                Equal(roles[index], view.Leading.Points[index].Role);
            Equal(PointFreedom.Fixed, view.Leading.Points[0].Freedom);
            Equal(PointFreedom.SpanOnly, view.Leading.Points[1].Freedom);
            Equal(PointFreedom.Free, view.Leading.Points[4].Freedom);
            Equal(PointFreedom.Free, view.Leading.Points[5].Freedom);
            Equal(PointFreedom.ValueOnly, view.Leading.Points[6].Freedom);
            Equal(true, view.Leading.Points[0].Locks.Contains("root_mirror"));
            Equal(true, view.Leading.Points[1].Locks.Contains("root_mirror"));
            Equal(false, view.Leading.Points[2].Locks.Contains("root_mirror"));
            Equal(true, view.Leading.Points[3].Kind is null);
            Near(0.45, view.HalfSpanMeters, 1e-12);
        });
        Check("PointModel_SrOneAliases_RemovedAtExit", () =>
        {
            // Reflection over every Core type (public and internal), then a source scan of src/ for the Desktop tuple names.
            var members = typeof(PointView).Assembly.GetTypes()
                .SelectMany(type => type.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                    .Select(member => type.Name + "." + member.Name))
                .Where(name => name.EndsWith(".AftMeters", StringComparison.Ordinal) || name.EndsWith(".AftOnly", StringComparison.Ordinal)
                    || name == "HandleTargetPoint.op_Implicit")
                .ToArray();
            Equal(0, members.Length);
            Equal(false, Enum.GetNames<PointFreedom>().Contains("AftOnly"));
            string root = PlacementTests.RepoRoot();
            var alias = new System.Text.RegularExpressions.Regex(@"\b(AftMeters|AftOnly)\b");
            var hits = Directory.EnumerateFiles(System.IO.Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(System.IO.Path.DirectorySeparatorChar + "obj" + System.IO.Path.DirectorySeparatorChar))
                .Where(file => alias.IsMatch(File.ReadAllText(file))).ToArray();
            Equal(0, hits.Length);
        });
        Check("PointModel_AnchorAtMultiplicityThree_HandlesAssigned", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Fx("foil-41-tangents.foil")), "spline", 1);
            var points = view.Leading.Points;
            Equal("4.1", view.Version);
            Equal(16, view.Leading.Ceiling);
            Equal(PointRole.Anchor, points[3].Role);
            Equal(TangentKind.Smooth, points[3].Kind);
            Equal(PointRole.AnchorHandle, points[2].Role);
            Equal(PointRole.AnchorHandle, points[4].Role);
            Equal("cv-3", points[2].AnchorId);
            Equal("cv-3", points[4].AnchorId);
            Equal(false, points.Any(point => point.Role == PointRole.Control));
        });
        Check("PointModel_RootMirror_TeRootAftOnlyLeRootFixed", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Example), "accepted", 1);
            Equal(PointFreedom.Fixed, view.Leading.Points[0].Freedom);
            Equal(PointFreedom.ValueOnly, view.Trailing.Points[0].Freedom);
            Equal(PointFreedom.SpanOnly, view.Leading.Points[1].Freedom);
            Equal(PointFreedom.ValueOnly, view.Leading.Points[^1].Freedom);
            Equal(PointFreedom.ValueOnly, view.Trailing.Points[^1].Freedom);
        });
        Check("PointModel_MultiplicityTwoKnot_ControlPointsAndC1Marker", () =>
        {
            byte[] source = File.ReadAllBytes(Fx("foil-41-multiplicity-two.foil"));
            var view = Planform.View(source, "spline", 1);
            Equal(PointRole.Control, view.Leading.Points[3].Role);
            Equal(true, view.Leading.Points[3].Kind is null);
            var parsed = FoilSource.Parse(source);
            var curve = parsed.Definition!.Curves["leading"];
            var place = At(curve, 0.4, view.HalfSpanMeters);
            var marks = Planform.Comb(view.Leading).Where(tooth => Distance(tooth, place) < 1e-6).ToArray();
            Equal(1, marks.Length);
            Equal(false, marks[0].BreakBefore);
        });
        Check("PlanformView_Sampling_NeverUsesProofBudget", () =>
        {
            int before = ProofBudget.Entries;
            _ = Planform.View(File.ReadAllBytes(Example), "spline", 1);
            Equal(before, ProofBudget.Entries);
        });
        Check("PlanformView_SplineBasisVsBernstein_Within1e12", () =>
        {
            byte[] source = File.ReadAllBytes(Fx("foil-41-multiplicity-two.foil"));
            var parsed = FoilSource.Parse(source);
            var curve = parsed.Definition!.Curves["leading"];
            var view = Planform.View(source, "spline", 1);
            var spans = Bernstein.Spans(curve, new ProofBudget());
            var parameters = SampleParameters(curve);
            Equal(parameters.Count, view.Leading.Samples.Count);
            for (int index = 0; index < parameters.Count; index++)
            {
                double t = parameters[index];
                var spline = At(curve, t, view.HalfSpanMeters);
                var span = spans.Last(item => t >= item.Start.Nearest() && t <= item.End.Nearest());
                double u = (t - span.Start.Nearest()) / (span.End.Nearest() - span.Start.Nearest());
                Rel(spline.Span, BernsteinOrdinate(span.X, u) * view.HalfSpanMeters);
                Rel(spline.Aft, BernsteinOrdinate(span.Y, u));
                Rel(spline.Span, view.Leading.Samples[index].SpanMeters);
                Rel(spline.Aft, view.Leading.Samples[index].Ordinate);
            }
        });
        Check("Planform_Probe_ChordAndRailsAtEta", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Example), "accepted", 1);
            var reading = Planform.Probe(view, 0.5);
            Near(0.5, reading.Eta, 1e-15);
            Near(0.225, reading.SpanMeters, 1e-12);
            Near(0, reading.LeadingAftMeters, 1e-12);
            Near(0.12, reading.TrailingAftMeters, 1e-12);
            Near(0.12, reading.ChordMeters, 1e-12);
        });
        Check("Planform_HandleTarget_AngleFromSpanAxis", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Fx("foil-41-tangents.foil")), "spline", 1);
            var right = Planform.HandleTarget(view, "leading", "cv-4", 0, 0.05);
            var left = Planform.HandleTarget(view, "leading", "cv-2", 0, 0.05);
            Near(0.275, right.SpanMeters, 1e-12);
            Near(0, right.Ordinate, 1e-12);
            Near(0.175, left.SpanMeters, 1e-12);
            Near(0, left.Ordinate, 1e-12);
        });
        Check("Comb_Anchor_TwoOneSidedTeeth", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Fx("foil-41-tangents.foil")), "spline", 1);
            var anchor = view.Leading.Points[3];
            var near = Planform.Comb(view.Leading).Where(tooth => Distance(tooth, (anchor.SpanMeters, anchor.Ordinate)) < 1e-6)
                .OrderBy(tooth => tooth.SpanMeters).ToArray();
            Equal(2, near.Length);
            Equal(false, near[0].BreakBefore);
            Equal(false, near[1].BreakBefore);
        });
        Check("Comb_CornerAnchor_BreakReported", () =>
        {
            string text = File.ReadAllText(Fx("foil-41-tangents.foil")).Replace(" tangents { \"cv-3\" smooth }", "", StringComparison.Ordinal);
            var view = Planform.View(Encoding.UTF8.GetBytes(text), "spline", 1);
            Equal(TangentKind.Corner, view.Leading.Points[3].Kind);
            var anchor = view.Leading.Points[3];
            var near = Planform.Comb(view.Leading).Where(tooth => Distance(tooth, (anchor.SpanMeters, anchor.Ordinate)) < 1e-6)
                .OrderBy(tooth => tooth.SpanMeters).ToArray();
            Equal(2, near.Length);
            Equal(false, near[0].BreakBefore);
            Equal(true, near[1].BreakBefore);
        });
        Check("Comb_SixteenPointRail_FairnessFixture", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Fx("foil-41-sixteen-three-anchors.foil")), "spline", 1);
            var comb = Planform.Comb(view.Leading);
            Equal(true, comb.All(tooth => Math.Abs(tooth.Curvature) < 1e-6));
            int nonempty = 0;
            var knots = view.Leading.Knots;
            int count = view.Leading.Points.Count;
            for (int span = 3; span < count; span++)
                if (knots[span] < knots[span + 1]) nonempty++;
            Equal(true, nonempty > 0 && comb.Count >= 8 * nonempty);
        });
    }

    internal static void RunReadiness()
    {
        Check("Readiness_SixteenPointThreeAnchors_AssessUnderProofBudget", () =>
        {
            var parsed = FoilSource.Parse(File.ReadAllBytes(Fx("foil-41-sixteen-three-anchors.foil")));
            if (parsed.IsParsed && parsed.Definition!.Curves.Values.Any(curve => curve.MissingIds))
                parsed = FoilSource.Parse(FoilSource.MaterializeIds(parsed));
            var watch = Stopwatch.StartNew();
            var assessment = Geometry.Assess(parsed);
            watch.Stop();
            Console.WriteLine("READINESS assess_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, watch.Elapsed < TimeSpan.FromSeconds(1));
        });
    }

    private static List<double> SampleParameters(Curve curve)
    {
        int spans = 0;
        for (int span = curve.Degree; span < curve.Points.Length; span++)
            if (curve.Knots[span] < curve.Knots[span + 1]) spans++;
        int per = 8;
        if (spans > 0)
            while (per + (spans - 1) * (per - 1) < 64) per++;
        var parameters = new List<double>();
        bool firstSpan = true;
        for (int span = curve.Degree; span < curve.Points.Length; span++)
        {
            if (curve.Knots[span] >= curve.Knots[span + 1]) continue;
            double start = curve.Knots[span], end = curve.Knots[span + 1];
            for (int step = firstSpan ? 0 : 1; step < per; step++)
                parameters.Add(start + step / (double)(per - 1) * (end - start));
            firstSpan = false;
        }
        return parameters;
    }

    private static (double Span, double Aft) At(Curve curve, double t, double halfSpan)
    {
        var jet = SplineBasis.Evaluate(curve.Knots, curve.Degree, t);
        double eta = 0, ordinate = 0;
        for (int index = 0; index < curve.Points.Length; index++)
        {
            eta += jet.N[index] * curve.Points[index][0];
            ordinate += jet.N[index] * curve.Points[index][1];
        }
        return (eta * halfSpan, ordinate);
    }

    private static double BernsteinOrdinate(Rational[] coefficients, double u)
    {
        int degree = coefficients.Length - 1;
        double sum = 0;
        for (int index = 0; index <= degree; index++)
            sum += Choose(degree, index) * Math.Pow(u, index) * Math.Pow(1 - u, degree - index) * coefficients[index].Nearest();
        return sum;
    }

    private static double Choose(int n, int k)
    {
        double value = 1;
        for (int index = 1; index <= k; index++)
            value = value * (n - k + index) / index;
        return value;
    }

    private static double Distance(CombTooth tooth, (double Span, double Aft) place) =>
        Math.Sqrt(Math.Pow(tooth.SpanMeters - place.Span, 2) + Math.Pow(tooth.Ordinate - place.Aft, 2));

    private static void Near(double expected, double actual, double absolute)
    {
        if (Math.Abs(actual - expected) > absolute)
            throw new InvalidOperationException("Expected " + expected.ToString("G17", CultureInfo.InvariantCulture) + "; actual " + actual.ToString("G17", CultureInfo.InvariantCulture));
    }

    private static void Rel(double expected, double actual)
    {
        double scale = Math.Max(1, Math.Abs(expected));
        if (Math.Abs(actual - expected) / scale > 1e-12)
            throw new InvalidOperationException("Expected " + expected.ToString("G17", CultureInfo.InvariantCulture) + "; actual " + actual.ToString("G17", CultureInfo.InvariantCulture));
    }
}
