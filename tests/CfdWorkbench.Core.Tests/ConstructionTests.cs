using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class ConstructionTests
{
    private static string Id() => Guid.NewGuid().ToString("D");

    internal static void Run()
    {
        Check("Profile_Insert_PreservesShape_UndoRedo", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            var before = session.ProfileAt(0);
            double[] beforeKnots = Knots(Encoding.UTF8.GetString(original), "upper");
            double chord = before.Upper[^1].X - before.Upper[0].X;
            string draft = Id();
            var begun = session.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37);
            string text = Encoding.UTF8.GetString(session.Snapshot().Draft!.Bytes);
            string[] upperIds = Ids(text, "upper");
            string[] lowerIds = Ids(text, "lower");
            Equal(before.Upper.Count + 1, upperIds.Length);
            Equal(before.Lower.Count + 1, lowerIds.Length);
            string added = upperIds.Except(before.Upper.Select(vertex => vertex.Id)).Single();
            Equal("cv-8", added);
            int index = Array.IndexOf(upperIds, added);
            Equal(added, lowerIds[index]);
            double[][] upperPoints = Points(text, "upper");
            double[][] lowerPoints = Points(text, "lower");
            for (int i = 0; i < upperPoints.Length; i++) Equal(upperPoints[i][0], lowerPoints[i][0]);
            double[] upper = Knots(text, "upper");
            double[] lower = Knots(text, "lower");
            Equal(beforeKnots.Length + 1, upper.Length);
            Equal(upper.Length, lower.Length);
            for (int i = 0; i < upper.Length; i++)
                Equal(BitConverter.DoubleToUInt64Bits(upper[i]), BitConverter.DoubleToUInt64Bits(lower[i]));
            var assessment = session.Validate(draft, begun.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, assessment.Construction is not null);
            Equal(null, assessment.Construction!.Tolerance);
            Equal(upperIds.Length, assessment.Construction.VertexCount);
            Equal(true, assessment.Construction.MaxDeviation <= 1e-12 * chord);
            string operation = Id();
            session.Apply(operation, assessment);
            byte[] inserted = session.Snapshot().Source.ToArray();
            Equal(2, session.Envelope().Accepted.Length);
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, inserted.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Profile_InsertThenDelete_RestoresShape", () =>
        {
            using var session = Opened();
            string original = Encoding.UTF8.GetString(session.Snapshot().Source);
            double[] beforeKnots = Knots(original, "upper");
            double[][] beforeUpper = Points(original, "upper");
            double[][] beforeLower = Points(original, "lower");
            string draft = Id();
            var begun = session.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37);
            string inserted = Encoding.UTF8.GetString(session.Snapshot().Draft!.Bytes);
            int index = Array.IndexOf(Ids(inserted, "upper"), "cv-8");
            Equal(true, index > 0);
            session.Apply(Id(), session.Validate(draft, begun.Generation));
            draft = Id();
            begun = session.BeginProfileDelete(draft, 0, SectionScope.Shared, index);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(true, assessment.Construction is not null && assessment.Construction!.MaxDeviation <= 1e-12);
            string text = Encoding.UTF8.GetString(session.Snapshot().Draft!.Bytes);
            double[] knots = Knots(text, "upper");
            double[][] restoredUpper = Points(text, "upper");
            double[][] restoredLower = Points(text, "lower");
            Equal(beforeUpper.Length, restoredUpper.Length);
            Equal(true, MaxDelta(beforeUpper, beforeKnots, restoredUpper, knots) <= 1e-12);
            Equal(true, MaxDelta(beforeLower, beforeKnots, restoredLower, knots) <= 1e-12);
        });
        Check("Profile_DeleteInterior_ReportsDeviation", () =>
        {
            using var session = Opened();
            string draft = Id();
            var begun = session.BeginProfileDelete(draft, 0, SectionScope.Shared, 3);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(true, assessment.Construction is { MaxDeviation: > 0 });
            Equal(true, assessment.Status == GeometryStatus.Certified || assessment.Code is "DSL-GEOMETRY" or "DSL-PROFILE-CROSS" or "DSL-PROFILE-ORDER" or "DSL-CURVE" or "GEOMETRY-CERTIFICATE-DEFECT");
            if (assessment.Status == GeometryStatus.Certified)
            {
                session.Apply(Id(), assessment);
                var remaining = Ids(Encoding.UTF8.GetString(session.Snapshot().Source), "upper").ToHashSet(StringComparer.Ordinal);
                Equal(false, remaining.Contains("cv-3"));
                session.BeginProfileInsert(Id(), 0, SectionScope.Shared, 0.37);
                string added = Ids(Encoding.UTF8.GetString(session.Snapshot().Draft!.Bytes), "upper").Except(remaining).Single();
                Equal(false, added == "cv-3");
                Equal(true, added.StartsWith("cv-", StringComparison.Ordinal));
            }
        });
        Check("Profile_Delete_RefusesFloorAndFixedEnds", () =>
        {
            using var session = Opened(Six());
            string draft = Id();
            var begun = session.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.4);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            session.Apply(Id(), assessment);
            Equal(7, Ids(Encoding.UTF8.GetString(session.Snapshot().Source), "upper").Length);
            try
            {
                session.BeginProfileDelete(Id(), 0, SectionScope.Shared, 3);
                throw new InvalidOperationException("expected refusal");
            }
            catch (ContractError error)
            {
                Equal("DSL-CURVE", error.Code);
                Equal("Delete would leave fewer than p + 2 = 7 vertices", error.Reason);
            }
            using var example = Opened();
            Refuses("DSL-LOCK", () => example.BeginProfileDelete(Id(), 0, SectionScope.Shared, 0));
            Refuses("DSL-LOCK", () => example.BeginProfileDelete(Id(), 0, SectionScope.Shared, example.ProfileAt(0).Upper.Count - 1));
        });
        Check("Profile_Insert_CancelRestoresBytes_OneUndoItem", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            string draft = Id();
            session.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37);
            session.Cancel(draft);
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(null, session.Snapshot().Draft);
            Equal(1, session.Envelope().Accepted.Length);
            draft = Id();
            var begun = session.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37);
            session.Apply(Id(), session.Validate(draft, begun.Generation));
            Equal(2, session.Envelope().Accepted.Length);
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(1, session.Envelope().Cursors.Count(row => row.Reason == "undo"));
        });
        Check("Profile_Insert_RefusesOutsideChord", () =>
        {
            using var session = Opened();
            Refuses("DSL-PROFILE-TARGET", () => session.BeginProfileInsert(Id(), 0, SectionScope.Shared, 0));
            Refuses("DSL-PROFILE-TARGET", () => session.BeginProfileInsert(Id(), 0, SectionScope.Shared, 1));
            Refuses("DSL-PROFILE-TARGET", () => session.BeginProfileInsert(Id(), 0, SectionScope.Shared, -0.2));
            Refuses("DSL-PROFILE-TARGET", () => session.BeginProfileInsert(Id(), 0, SectionScope.Shared, 1.2));
        });
        Check("Profile_Insert_IndependentCopiesFirst", () =>
        {
            using var session = Opened();
            var begun = session.BeginProfileInsert(Id(), 0, SectionScope.Independent, 0.37);
            string text = Encoding.UTF8.GetString(begun.Bytes);
            int copy = text.IndexOf("profile \"section-a-i1\"", StringComparison.Ordinal);
            Equal(true, copy > 0);
            Equal(9, Ids(text[copy..], "upper").Length);
            Equal(9, Ids(text[copy..], "lower").Length);
            Equal(8, Ids(text, "upper").Length);
            Equal(true, text.Contains("at root profile \"section-a-i1\"", StringComparison.Ordinal));
            Equal(true, text.Contains("at tip profile \"section-a\"", StringComparison.Ordinal));
        });
    }

    private static AuthoringSession Opened() => Opened(FoilSourceTests.Example);

    private static AuthoringSession Opened(byte[] source)
    {
        var session = new AuthoringSession();
        session.Open(source, Id(), true);
        return session;
    }

    private static byte[] Six()
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
        text = text.Replace(
            "upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.025), (0.15, 0.055), (0.35, 0.065), (0.55, 0.05), (0.75, 0.03), (0.9, 0.01), (1, 0)] }",
            "upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.03), (0.25, 0.06), (0.6, 0.05), (0.85, 0.02), (1, 0)] }",
            StringComparison.Ordinal);
        text = text.Replace(
            "lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.025), (0.15, -0.055), (0.35, -0.065), (0.55, -0.05), (0.75, -0.03), (0.9, -0.01), (1, 0)] }",
            "lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.03), (0.25, -0.06), (0.6, -0.05), (0.85, -0.02), (1, 0)] }",
            StringComparison.Ordinal);
        return Encoding.UTF8.GetBytes(text);
    }

    private static int CurveAt(string text, string side) => text.IndexOf(side + " cv", StringComparison.Ordinal);

    private static double[] Knots(string text, string side)
    {
        int at = CurveAt(text, side);
        int start = text.IndexOf('[', at) + 1;
        int end = text.IndexOf(']', start);
        return text[start..end].Split(',').Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray();
    }

    private static string[] Ids(string text, string side)
    {
        int at = text.IndexOf("ids [", CurveAt(text, side), StringComparison.Ordinal);
        int start = text.IndexOf('[', at) + 1;
        int end = text.IndexOf(']', start);
        return text[start..end].Split(',').Select(part => part.Trim().Trim('"')).ToArray();
    }

    private static double[][] Points(string text, string side)
    {
        int at = text.IndexOf("points [", CurveAt(text, side), StringComparison.Ordinal);
        int start = text.IndexOf('[', at) + 1;
        int end = text.IndexOf(']', start);
        return text[start..end].Split("),", StringSplitOptions.TrimEntries).Select(part =>
        {
            string body = part.Trim().Trim('(', ')');
            string[] xy = body.Split(',');
            return new[] { double.Parse(xy[0], CultureInfo.InvariantCulture), double.Parse(xy[1], CultureInfo.InvariantCulture) };
        }).ToArray();
    }

    private static double MaxDelta(double[][] before, double[] beforeKnots, double[][] after, double[] afterKnots)
    {
        int degree = beforeKnots.Length - before.Length - 1;
        int afterDegree = afterKnots.Length - after.Length - 1;
        double worst = 0;
        for (int sample = 0; sample <= 2000; sample++)
        {
            double x = sample / 2000d;
            double delta = Math.Abs(Ordinate(before, beforeKnots, degree, x) - Ordinate(after, afterKnots, afterDegree, x));
            if (delta > worst) worst = delta;
        }
        return worst;
    }

    private static double Ordinate(double[][] points, double[] knots, int degree, double x)
    {
        double lo = 0, hi = 1;
        for (int step = 0; step < 80; step++)
        {
            double mid = (lo + hi) / 2;
            if (DeBoor(points, knots, degree, mid)[0] < x) lo = mid;
            else hi = mid;
        }
        return DeBoor(points, knots, degree, (lo + hi) / 2)[1];
    }

    private static double[] DeBoor(double[][] points, double[] knots, int degree, double t)
    {
        if (t >= 1) return points[^1];
        int span = degree;
        while (span + 1 < points.Length && knots[span + 1] <= t) span++;
        var values = Enumerable.Range(0, degree + 1).Select(index => (double[])points[span - degree + index].Clone()).ToArray();
        for (int level = 1; level <= degree; level++)
            for (int index = degree; index >= level; index--)
            {
                int knot = span - degree + index;
                double width = knots[knot + degree - level + 1] - knots[knot];
                double alpha = width > 0 ? (t - knots[knot]) / width : 0;
                values[index][0] = (1 - alpha) * values[index - 1][0] + alpha * values[index][0];
                values[index][1] = (1 - alpha) * values[index - 1][1] + alpha * values[index][1];
            }
        return values[degree];
    }
}
