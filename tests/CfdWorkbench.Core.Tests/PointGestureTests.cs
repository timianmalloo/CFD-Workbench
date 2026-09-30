using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class PointGestureTests
{
    internal static string Id() => Guid.NewGuid().ToString("D");
    internal static AuthoringSession Open(byte[]? bytes = null)
    {
        var session = new AuthoringSession();
        session.Open(bytes ?? FoilSourceTests.Example, Id(), true);
        return session;
    }
    internal static byte[] Row(string kind = "smooth") => Encoding.UTF8.GetBytes(
        File.ReadAllText("tests/CfdWorkbench.Core.Tests/Fixtures/m12b/foil-41-tangents.foil")
            .Replace("\"cv-3\" smooth", "\"cv-3\" " + kind, StringComparison.Ordinal));
    internal static PointView Point(AuthoringSession s, string curve, int index)
    {
        var snap = s.Snapshot();
        var view = Planform.View(snap.Draft?.Bytes ?? snap.Source, "test", snap.Draft?.Generation ?? 0);
        return (curve == "leading" ? view.Leading : view.Trailing).Points[index];
    }
    internal static void True(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    internal static void Near(double expected, double actual, double tolerance = 1e-8) =>
        True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}; actual {actual}");
    private static void Order(AuthoringSession s, string curve)
    {
        var view = Planform.View(s.Snapshot().Draft!.Bytes, "Draft", s.Snapshot().Draft!.Generation);
        var points = (curve == "leading" ? view.Leading : view.Trailing).Points;
        for (int i = 1; i < points.Count; i++) True(points[i].Eta > points[i - 1].Eta, "point order");
    }
    private static void Symmetric(AuthoringSession s)
    {
        var a = Point(s, "leading", 3); var l = Point(s, "leading", 2); var r = Point(s, "leading", 4);
        Near(2 * a.SpanMeters, l.SpanMeters + r.SpanMeters);
        Near(2 * a.AftMeters, l.AftMeters + r.AftMeters);
    }
    internal static void Run()
    {
        Check("BeginPointGesture_FixedPoint_DslLock", () =>
        {
            using var s = Open();
            Refuses("DSL-LOCK", () => s.BeginPointGesture(Id(), "leading", Point(s, "leading", 0).Id));
            Equal(null, s.Snapshot().Draft);
        });
        Check("UpdatePointGesture_ControlPoint_QuantizedDeltaExactPosition", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            var f = s.UpdatePointGesture(d.Id, d.Generation, p.SpanMeters + 0.001234567, p.AftMeters + 0.002345678);
            Near(p.AftMeters + 0.002346, f.AftMeters, 1e-9); Equal(1L, f.Draft.Generation);
        });
        Check("UpdatePointGesture_Anchor_HandlesMoveBySameDelta", () =>
        {
            using var s = Open(Row()); var prior = Enumerable.Range(2, 3).Select(i => Point(s, "leading", i)).ToArray();
            var d = s.BeginPointGesture(Id(), "leading", prior[1].Id);
            s.UpdatePointGesture(d.Id, d.Generation, prior[1].SpanMeters + 0.003, prior[1].AftMeters + 0.002);
            for (int i = 0; i < 3; i++) { Near(0.003, Point(s, "leading", i + 2).SpanMeters - prior[i].SpanMeters); Near(0.002, Point(s, "leading", i + 2).AftMeters - prior[i].AftMeters); }
        });
        Check("UpdatePointGesture_SmoothHandleDrag_OppositeCollinear", () =>
        {
            using var s = Open(Row()); var h = Point(s, "leading", 2); var d = s.BeginPointGesture(Id(), "leading", h.Id);
            s.UpdatePointGesture(d.Id, d.Generation, h.SpanMeters, h.AftMeters + 0.008);
            var a = Point(s, "leading", 3); var l = Point(s, "leading", 2); var r = Point(s, "leading", 4);
            Near(0, (l.SpanMeters - a.SpanMeters) * (r.AftMeters - a.AftMeters) - (r.SpanMeters - a.SpanMeters) * (l.AftMeters - a.AftMeters));
        });
        Check("UpdatePointGesture_SymmetricHandleDrag_Midpoint", () =>
        {
            using var s = Open(Row("symmetric")); var h = Point(s, "leading", 2); var d = s.BeginPointGesture(Id(), "leading", h.Id);
            s.UpdatePointGesture(d.Id, d.Generation, h.SpanMeters, h.AftMeters + 0.008); Symmetric(s);
        });
        Check("UpdatePointGesture_SymmetricAnchorDragAfterRefit_RowHolds", () =>
        {
            using var s = Open(Row("symmetric")); var a = Point(s, "leading", 3); var d = s.BeginPointGesture(Id(), "leading", a.Id);
            s.UpdatePointGesture(d.Id, d.Generation, a.SpanMeters + 0.003, a.AftMeters + 0.001); Symmetric(s);
            Equal(true, FoilSource.Parse(s.Snapshot().Draft!.Bytes).IsParsed);
        });
        Check("UpdatePointGesture_TeRootEnd_HandleFollowsRootMirror", () =>
        {
            using var s = Open(FoilSource.NewDefault()); var p = Point(s, "trailing", 0); var h = Point(s, "trailing", 1);
            var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            s.UpdatePointGesture(d.Id, d.Generation, p.SpanMeters + 0.01, p.AftMeters + 0.005);
            Near(p.SpanMeters, Point(s, "trailing", 0).SpanMeters); Near(h.AftMeters + 0.005, Point(s, "trailing", 1).AftMeters);
        });
        Check("UpdatePointGesture_PastNeighbour_ClampedOrderKept", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            Equal(true, s.UpdatePointGesture(d.Id, d.Generation, 1, p.AftMeters).Clamped); Order(s, "trailing");
        });
        Check("UpdatePointGesture_GapBelowOneMillimetre_ClampKeepsCurrentGap", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); var n = Point(s, "trailing", 3); var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            Equal(true, s.UpdatePointGesture(d.Id, d.Generation, n.SpanMeters, p.AftMeters).Clamped);
            True(n.SpanMeters - Point(s, "trailing", 2).SpanMeters >= 0.001 - 1e-9, "minimum gap");
        });
        Check("UpdatePointGesture_RandomTargets_OrderAndRowsHold", () =>
        {
            using var s = Open(Row("symmetric")); var a = Point(s, "leading", 3); var d = s.BeginPointGesture(Id(), "leading", a.Id);
            var random = new Random(1041);
            for (int i = 0; i < 32; i++)
            {
                d = s.Snapshot().Draft!;
                s.UpdatePointGesture(d.Id, d.Generation, a.SpanMeters + (random.NextDouble() - 0.5) * 0.05, a.AftMeters + (random.NextDouble() - 0.5) * 0.04);
                Order(s, "leading"); Symmetric(s);
            }
        });
        Check("UpdatePointGesture_StaleGeneration_DslConflict", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            s.UpdatePointGesture(d.Id, d.Generation, p.SpanMeters, p.AftMeters + 0.001);
            Refuses("DSL-CONFLICT", () => s.UpdatePointGesture(d.Id, d.Generation, p.SpanMeters, p.AftMeters + 0.002));
        });
        Check("UpdatePointGesture_BypassedClamp_DslPatch", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            Equal(true, s.UpdatePointGesture(d.Id, d.Generation, double.PositiveInfinity, p.AftMeters).Clamped); Order(s, "trailing");
        });
        Check("Gesture_DragManyFrames_OneAcceptedRow", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", p.Id);
            for (int i = 1; i <= 5; i++) { d = s.Snapshot().Draft!; s.UpdatePointGesture(d.Id, d.Generation, p.SpanMeters, p.AftMeters + i * 0.001); }
            d = s.Snapshot().Draft!; int before = s.Envelope().Accepted.Length;
            s.Apply(Id(), s.Validate(d.Id, d.Generation)); Equal(before + 1, s.Envelope().Accepted.Length);
        });
        Check("Gesture_ReleaseWithoutMove_NoAcceptedRow", () =>
        {
            using var s = Open(); var p = Point(s, "trailing", 2); int before = s.Envelope().Accepted.Length;
            var d = s.BeginPointGesture(Id(), "trailing", p.Id); s.Cancel(d.Id);
            Equal(before, s.Envelope().Accepted.Length); Equal(null, s.Snapshot().Draft);
        });
    }
}
