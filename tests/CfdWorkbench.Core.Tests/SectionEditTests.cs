using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class SectionEditTests
{
    private static string Id() => Guid.NewGuid().ToString("D");

    internal static void Run()
    {
        Check("Profile_PatchPoint_RoundTripsAndChangesOnlyThatCv", () =>
        {
            byte[] source = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
            const double x = 0.36, y = 0.08;
            byte[] patched = FoilSource.PatchProfilePoint(source, "section-a", "upper", "cv-3", x, y);
            byte[] twice = FoilSource.PatchProfilePoint(patched, "section-a", "upper", "cv-3", x, y);
            Equal(true, patched.AsSpan().SequenceEqual(twice));
            var before = Tuples(Encoding.UTF8.GetString(source));
            var after = Tuples(Encoding.UTF8.GetString(patched));
            Equal(before.Count, after.Count);
            Equal(1, before.Zip(after).Count(pair => pair.First != pair.Second));
            string changed = before.Zip(after).Single(pair => pair.First != pair.Second).Second;
            string[] parts = changed.Split(',');
            Equal(BitConverter.DoubleToUInt64Bits(x), BitConverter.DoubleToUInt64Bits(DecimalSi.Parse(parts[0].Trim(), 0)));
            Equal(BitConverter.DoubleToUInt64Bits(y), BitConverter.DoubleToUInt64Bits(DecimalSi.Parse(parts[1].Trim(), 0)));
        });
        Check("Profile_PatchUpper_LeavesLowerCurveBytes", () =>
        {
            byte[] source = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
            byte[] patched = FoilSource.PatchProfilePoint(source, "section-a", "upper", "cv-3", 0.4, 0.08);
            Equal(CurveText(Encoding.UTF8.GetString(source), "lower"), CurveText(Encoding.UTF8.GetString(patched), "lower"));
        });
        Check("Profile_FixedVertex_RefusesLock", () =>
        {
            using var session = Opened();
            Refuses("DSL-LOCK", () => session.BeginProfileEdit(Id(), 0, SectionScope.Shared, "upper", "cv-0"));
            Refuses("DSL-LOCK", () => session.BeginProfileEdit(Id(), 0, SectionScope.Shared, "lower", "cv-7"));
        });
        Check("Profile_AbscissaPastNeighbour_RefusesOrder", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            Refuses("DSL-PROFILE-ORDER", () => session.UpdateProfileDraft(draft, 0, 0.9, 0.08));
            Equal(0L, session.Snapshot().Draft!.Generation);
            Equal(true, session.Snapshot().Source.AsSpan().SequenceEqual(session.Snapshot().Draft!.Bytes));
        });
        Check("Profile_UnknownVertex_RefusesTarget", () =>
        {
            byte[] source = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
            Refuses("DSL-PROFILE-TARGET", () => FoilSource.PatchProfilePoint(source, "section-a", "upper", "missing", 0.4, 0.08));
        });
        Check("Profile_SharedEdit_ScopeApplyUndoRedo", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            var scope = session.DescribeScope("section-a", 0, SectionScope.Shared);
            Equal("section-a", scope.Profile); Equal(SectionScope.Shared, scope.Scope);
            Equal(2, scope.AffectedAssignments.Count); Equal(0, scope.AffectedAssignments[0]); Equal(1, scope.AffectedAssignments[1]);
            var vertex = session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3");
            double kept = session.ProfileAt(1).Upper.Single(item => item.Id == "cv-3").Y;
            Equal(101, session.ProfileAt(0).UpperCurve.Count); Equal(101, session.ProfileAt(0).LowerCurve.Count);
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.015);
            Equal(vertex.Y + 0.015, session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3").Y);
            Equal(kept, session.ProfileAt(1).Upper.Single(item => item.Id == "cv-3").Y);
            var assessment = session.Validate(draft, updated.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            string operation = Id();
            string applied = session.Apply(operation, assessment);
            byte[] edited = session.Snapshot().Source;
            Equal(false, original.AsSpan().SequenceEqual(edited));
            Equal(applied, session.Apply(operation, assessment));
            Equal(2, session.Envelope().Accepted.Length);
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, edited.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Profile_Cancel_RestoresExactBytes", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            string draft = Id();
            session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            session.UpdateProfileDraft(draft, 0, 0.4, 0.08);
            session.Cancel(draft);
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(null, session.Snapshot().Draft);
        });
        Check("Profile_Retry_SameOperationIsIdempotent", () =>
        {
            using var session = Opened(); string draft = Id(), operation = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-4");
            var updated = session.UpdateProfileDraft(draft, begun.Generation, 0.55, 0.06);
            var assessment = session.Validate(draft, updated.Generation);
            string first = session.Apply(operation, assessment);
            string second = session.Apply(operation, assessment);
            Equal(first, second); Equal(2, session.Envelope().Accepted.Length);
        });
        Check("Profile_LocalSupport_OutsideSpansUnchanged", () =>
        {
            using var session = Opened();
            var beforeView = session.ProfileAt(0);
            double[] knots = UpperKnots(Encoding.UTF8.GetString(session.Snapshot().Source));
            double[][] before = Controls(beforeView.Upper);
            int index = beforeView.Upper.ToList().FindIndex(item => item.Id == "cv-6");
            int degree = knots.Length - before.Length - 1;
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-6");
            session.UpdateProfileDraft(draft, begun.Generation, beforeView.Upper[index].X, beforeView.Upper[index].Y + 0.01);
            double[][] after = Controls(session.ProfileAt(0).Upper);
            double t0 = knots[index], t1 = knots[index + degree + 1];
            const double margin = 1e-6;
            int outside = 0, moved = 0;
            for (int sample = 0; sample <= 1000; sample++)
            {
                double t = ParameterAt(before, knots, degree, sample / 1000d);
                double delta = Math.Abs(DeBoor(before, knots, degree, t)[1] - DeBoor(after, knots, degree, t)[1]);
                if (t < t0 - margin || t > t1 + margin) { outside++; if (delta > 1e-12) throw new InvalidOperationException($"Δy {delta} at x {sample / 1000d}"); }
                else if (t > t0 + margin && t < t1 - margin && delta > 1e-6) moved++;
            }
            Equal(true, outside > 0); Equal(true, moved > 0);
        });
        Check("Profile_MakeIndependent_MiddleStationSplitsIntervals", () =>
        {
            byte[] raw = ThreeStations();
            var made = FoilSource.MakeIndependent(raw, "section-a", 1);
            Equal("section-a-i1", made.NewProfile);
            var names = FoilSource.Parse(made.Source).Authored().Assignments.Select(item => item.ProfileName).ToArray();
            Equal("section-a", names[0]); Equal("section-a-i1", names[1]); Equal("section-a", names[2]);
            string before = Encoding.UTF8.GetString(raw), after = Encoding.UTF8.GetString(made.Source);
            int inserted = after.IndexOf("\n    profile \"section-a-i1\"", StringComparison.Ordinal);
            int keyword = after.IndexOf("profile \"section-a-i1\"", inserted, StringComparison.Ordinal);
            string stripped = after.Remove(inserted, BlockEnd(after, keyword) - inserted).Replace("profile \"section-a-i1\"", "profile \"section-a\"");
            Equal(before, stripped);
            using var session = new AuthoringSession();
            session.Open(raw, Id(), true);
            var impact = session.DescribeScope("section-a", 1, SectionScope.Independent);
            Equal(1, impact.AffectedAssignments.Count); Equal(1, impact.AffectedAssignments[0]);
            Equal(2, impact.Intervals.Count);
            var stations = session.InspectAccepted().Authored.Assignments;
            Equal(stations[0].Eta, impact.Intervals[0].EtaStart); Equal(stations[1].Eta, impact.Intervals[0].EtaEnd);
            Equal(stations[1].Eta, impact.Intervals[1].EtaStart); Equal(stations[2].Eta, impact.Intervals[1].EtaEnd);
            Equal(stations[0].SpanMeters, impact.Intervals[0].RootDistanceStartMeters);
            Equal(stations[1].SpanMeters, impact.Intervals[0].RootDistanceEndMeters);
            Equal(stations[1].SpanMeters, impact.Intervals[1].RootDistanceStartMeters);
            Equal(stations[2].SpanMeters, impact.Intervals[1].RootDistanceEndMeters);
        });
        Check("Profile_RecoveryRoundtrip_ResumesSameProfileTarget", () =>
        {
            using var session = Opened();
            var vertex = session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.01);
            session.CaptureRecovery(); byte[] saved = session.SaveImage();
            using var reopened = new AuthoringSession(); reopened.Reopen(saved); reopened.ResumeRecovery();
            Equal(updated.Generation, reopened.Snapshot().Draft!.Generation);
            var again = reopened.UpdateProfileDraft(draft, updated.Generation, vertex.X, vertex.Y + 0.02);
            var assessment = reopened.Validate(draft, again.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
        });
        Check("Session_RecoveryEnvelope_WithoutProfileFields_StillResumesRailDraft", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, 0.01);
            session.CaptureRecovery(); byte[] saved = session.SaveImage();
            string json = Encoding.UTF8.GetString(saved);
            Equal(false, json.Contains("\"profile\"", StringComparison.Ordinal));
            Equal(false, json.Contains("\"assignment\"", StringComparison.Ordinal));
            using var reopened = new AuthoringSession(); reopened.Reopen(saved); reopened.ResumeRecovery();
            Equal(1L, reopened.Snapshot().Draft!.Generation); Equal("leading", reopened.Snapshot().Draft!.Rail);
        });
        Check("Session_RecoveryEnvelope_MissingProfileTarget_RefusesAtLoad", () =>
        {
            using var session = Opened(); string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            session.UpdateProfileDraft(draft, begun.Generation, 0.36, 0.08);
            var recovery = session.CaptureRecovery();
            var envelope = session.Envelope() with { Recovery = recovery with { Profile = "missing-profile" } };
            byte[] saved = NativeProject.Encode(envelope);
            using var reopened = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => reopened.Reopen(saved));
        });
    }

    internal static void RunMultiProfile()
    {
        Check("Profile_IndependentEdit_ApplyUndoRedoKeepsOtherProfiles", () =>
        {
            using var session = new AuthoringSession();
            session.Open(ThreeStations(), Id(), true);
            byte[] original = session.Snapshot().Source;
            string kept = ProfileBlock(Encoding.UTF8.GetString(original), "section-a");
            var vertex = session.ProfileAt(1).Upper.Single(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 1, SectionScope.Independent, "upper", "cv-3");
            Equal(true, Encoding.UTF8.GetString(begun.Bytes).Contains("section-a-i1", StringComparison.Ordinal));
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.015);
            var assessment = session.Validate(draft, updated.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            session.Apply(Id(), assessment);
            string edited = Encoding.UTF8.GetString(session.Snapshot().Source);
            Equal(kept, ProfileBlock(edited, "section-a"));
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(kept, ProfileBlock(Encoding.UTF8.GetString(session.Snapshot().Source), "section-a"));
        });
    }

    private static AuthoringSession Opened()
    { var session = new AuthoringSession(); session.Open(FoilSourceTests.Example, Id(), true); return session; }

    private static byte[] ThreeStations()
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example).Replace(
            "at root profile \"section-a\" at tip profile \"section-a\"",
            "at root profile \"section-a\" at 50 % profile \"section-a\" at tip profile \"section-a\"");
        return Encoding.UTF8.GetBytes(text);
    }

    private static List<string> Tuples(string text)
    {
        var found = new List<string>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '(') continue;
            int end = text.IndexOf(')', i);
            found.Add(text[(i + 1)..end]); i = end;
        }
        return found;
    }

    private static string CurveText(string text, string side)
    {
        int start = text.IndexOf(side + " cv", StringComparison.Ordinal);
        return text[start..text.IndexOf('\n', start)];
    }

    private static string ProfileBlock(string text, string name)
    {
        int keyword = text.IndexOf("profile \"" + name + "\" {", StringComparison.Ordinal);
        return text[keyword..BlockEnd(text, keyword)];
    }

    private static int BlockEnd(string text, int keyword)
    {
        int open = text.IndexOf('{', keyword); int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return i + 1; }
        }
        throw new InvalidOperationException("Unclosed profile block.");
    }

    private static double[] UpperKnots(string text)
    {
        int at = text.IndexOf("upper cv", StringComparison.Ordinal);
        int start = text.IndexOf('[', at) + 1, end = text.IndexOf(']', start);
        return text[start..end].Split(',').Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray();
    }

    private static double[][] Controls(IReadOnlyList<ProfileVertex> vertices) => vertices.Select(vertex => new[] { vertex.X, vertex.Y }).ToArray();

    private static double ParameterAt(double[][] points, double[] knots, int degree, double x)
    {
        double lo = 0, hi = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (lo + hi) / 2;
            if (DeBoor(points, knots, degree, mid)[0] < x) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2;
    }

    private static double[] DeBoor(double[][] points, double[] knots, int degree, double t)
    {
        if (t >= 1) return points[^1];
        int span = degree;
        while (span + 1 < points.Length && knots[span + 1] <= t) span++;
        var values = Enumerable.Range(0, degree + 1).Select(j => (double[])points[span - degree + j].Clone()).ToArray();
        for (int level = 1; level <= degree; level++)
            for (int j = degree; j >= level; j--)
            {
                int index = span - degree + j;
                double width = knots[index + degree - level + 1] - knots[index];
                double alpha = width > 0 ? (t - knots[index]) / width : 0;
                values[j][0] = (1 - alpha) * values[j - 1][0] + alpha * values[j][0];
                values[j][1] = (1 - alpha) * values[j - 1][1] + alpha * values[j][1];
            }
        return values[degree];
    }
}
