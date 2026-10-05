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
            using var session = Opened();
            var vertex = session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3");
            string lower = CurveText(Encoding.UTF8.GetString(session.Snapshot().Source), "lower");
            string draft = Id();
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateSectionPoint(draft, begun.Generation, vertex.X, vertex.Y + 0.015);
            Equal(begun.Generation + 1, updated.Generation);
            Equal(lower, CurveText(Encoding.UTF8.GetString(session.Snapshot().Draft!.Bytes), "lower"));
            Equal(vertex.Y + 0.015, session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3").Y);
        });
        Check("Profile_PairedAbscissa_MovesSameIndexAndUndoRestores", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            var before = session.ProfileAt(0);
            int index = before.Upper.ToList().FindIndex(item => item.Id == "cv-3");
            const double moved = 0.4;
            string draft = Id();
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateSectionPoint(draft, begun.Generation, moved, before.Upper[index].Y);
            Equal(begun.Generation + 1, updated.Generation);
            var after = session.ProfileAt(0);
            Equal(moved, after.Upper[index].X);
            Equal(moved, after.Lower[index].X);
            Equal(before.Upper[index].Y, after.Upper[index].Y);
            Equal(before.Lower[index].Y, after.Lower[index].Y);
            var assessment = session.Validate(draft, updated.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            session.Apply(Id(), assessment);
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Profile_UpperCrossesLower_ReportsCross", () =>
        {
            using var session = Opened();
            var before = session.ProfileAt(0);
            int index = before.Upper.ToList().FindIndex(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            const double crossed = -3;
            Equal(true, crossed < before.Lower[index].Y);
            var updated = session.UpdateSectionPoint(draft, begun.Generation, before.Upper[index].X, crossed);
            var assessment = session.Validate(draft, updated.Generation);
            Equal("DSL-PROFILE-CROSS", assessment.Code);
            Equal("DSL-PROFILE-CROSS", assessment.Diagnostics[0].Code);
            Equal("Profile separation is not certified.", assessment.Diagnostics[0].Reason);
        });
        Check("Profile_FixedVertex_RefusesLock", () =>
        {
            using var session = Opened();
            foreach (var (side, id) in new[] { ("upper", "cv-0"), ("lower", "cv-7") })
            {
                string draft = Id();
                var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, side, id);
                var point = (side == "upper" ? session.ProfileAt(0).Upper : session.ProfileAt(0).Lower).Single(item => item.Id == id);
                Refuses("DSL-LOCK", () => session.UpdateSectionPoint(draft, begun.Generation, point.X, point.Y + .01));
                session.Cancel(draft);
            }
        });
        Check("Profile_AbscissaPastNeighbour_RefusesOrder", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            Refuses("DSL-PROFILE-ORDER", () => session.UpdateSectionPoint(draft, 0, 0.9, 0.08));
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
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateSectionPoint(draft, begun.Generation, vertex.X, vertex.Y + 0.015);
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
            session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            session.UpdateSectionPoint(draft, 0, 0.4, 0.08);
            session.Cancel(draft);
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(null, session.Snapshot().Draft);
        });
        Check("Profile_Retry_SameOperationIsIdempotent", () =>
        {
            using var session = Opened(); string draft = Id(), operation = Id();
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-4");
            var updated = session.UpdateSectionPoint(draft, begun.Generation, 0.55, 0.06);
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
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-6");
            session.UpdateSectionPoint(draft, begun.Generation, beforeView.Upper[index].X, beforeView.Upper[index].Y + 0.01);
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
        Check("MakeIndependent_TangentRow_NoDslPatch", MakeIndependent_TangentRow_NoDslPatch);
        Check("Parse_TangentsBeforeIds_RefusedDslSyntax", Parse_TangentsBeforeIds_RefusedDslSyntax);
        Check("MakeIndependent_CollidingIds_TangentNotCascaded", MakeIndependent_CollidingIds_TangentNotCascaded);
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
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateSectionPoint(draft, begun.Generation, vertex.X, vertex.Y + 0.01);
            session.CaptureRecovery(); byte[] saved = session.SaveImage();
            using var reopened = new AuthoringSession(); reopened.Reopen(saved); reopened.ResumeRecovery();
            Equal(updated.Generation, reopened.Snapshot().Draft!.Generation);
            var again = reopened.UpdateSectionPoint(draft, updated.Generation, vertex.X, vertex.Y + 0.02);
            var assessment = reopened.Validate(draft, again.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
        });
        Check("Session_RecoveryEnvelope_WithoutProfileFields_StillResumesRailDraft", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginGestureDraft(draft, "leading", "cv-2"); session.GestureToAft(draft, 0, 0.01);
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
            var begun = session.BeginSectionEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            session.UpdateSectionPoint(draft, begun.Generation, 0.36, 0.08);
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
            var begun = session.BeginSectionEdit(draft, 1, SectionScope.Independent, "upper", "cv-3");
            Equal(true, Encoding.UTF8.GetString(begun.Bytes).Contains("section-a-i1", StringComparison.Ordinal));
            var updated = session.UpdateSectionPoint(draft, begun.Generation, vertex.X, vertex.Y + 0.015);
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
        Check("Profile_IndependentAbscissaEdit_NamesNeighbour", () =>
        {
            using var session = new AuthoringSession();
            session.Open(ThreeStations(), Id(), true);
            var before = session.ProfileAt(1);
            int index = before.Upper.ToList().FindIndex(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginSectionEdit(draft, 1, SectionScope.Independent, "upper", "cv-3");
            // Ruling 71: was a landed step that the certificate refused at Validate ("Profile 'section-a-i1' abscissae differ
            // from neighbouring profile 'section-a'."); now the step is refused, naming the neighbour, and nothing changes.
            ContractError? error = null;
            try { session.UpdateSectionPoint(draft, begun.Generation, 0.4, before.Upper[index].Y); }
            catch (ContractError refused) { error = refused; }
            Equal("DSL-GEOMETRY", error?.Code);
            Equal("This edit would give Station 2's section different point positions from Root's, and the wing between them " +
                "can't be checked then. Move points up or down only, or keep the section shared.", error?.Reason);
            Equal(begun.Generation, session.Snapshot().Draft!.Generation);
        });
    }

    /// <summary>
    /// A section with a tangent row must become an independent profile. The row stays on the same
    /// vertex, including when that vertex's id is rewritten to the cv-N ids MakeIndependent assigns.
    /// </summary>
    private static void MakeIndependent_TangentRow_NoDslPatch()
    {
        byte[] canonical = SectionPointTests.Anchored("smooth", SectionPointTests.SmoothUpper(), SectionPointTests.SmoothLower());
        ExpectIndependentTangent(canonical, "smooth", null);

        byte[] renamed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(canonical).Replace("\"cv-", "\"pt-", StringComparison.Ordinal));
        ExpectIndependentTangent(renamed, "smooth", "cv-5");
    }

    // A tangents block is legal only in 4.1, after ids (docs/specs/foildsl.md). The same
    // fixture the retarget used, with the block moved in front of the ids list, is DSL-SYNTAX.
    private static void Parse_TangentsBeforeIds_RefusedDslSyntax()
    {
        byte[] canonical = SectionPointTests.Anchored("smooth", SectionPointTests.SmoothUpper(), SectionPointTests.SmoothLower());
        string text = Encoding.UTF8.GetString(canonical).Replace("\"cv-", "\"pt-", StringComparison.Ordinal);
        const string row = "tangents { \"pt-5\" smooth }";
        int rowAt = text.IndexOf(row, StringComparison.Ordinal);
        int idsAt = text.LastIndexOf("ids [", rowAt, StringComparison.Ordinal);
        if (rowAt < 0 || idsAt < 0) throw new InvalidOperationException("Fixture lost its tangent row or ids list");
        string idsClause = text[idsAt..rowAt];
        text = text[..idsAt] + row + " " + idsClause + text[(rowAt + row.Length)..];
        int movedRow = text.IndexOf(row, StringComparison.Ordinal);
        if (movedRow < 0 || text.IndexOf(idsClause.Trim(), movedRow, StringComparison.Ordinal) < movedRow)
            throw new InvalidOperationException("Tangents block did not move before the ids list");
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
        Equal(false, parsed.IsParsed);
        Equal("DSL-SYNTAX", parsed.Diagnostics[0].Code);
        Equal("Syntactic", parsed.Diagnostics[0].Phase);
        Equal("ids", Encoding.UTF8.GetString(parsed.Source.AsSpan(parsed.Diagnostics[0].ByteStart, parsed.Diagnostics[0].ByteLength)));
    }

    // The anchor's id is "cv-1" and the vertex at index 1 is "cv-5", which is that anchor's new id.
    // Index 0 is not an interior anchor, so the collision cannot be the literal list ["cv-1","cv-0",...].
    // A second pass over the rewritten text would point the row back at cv-1. The comment quotes the
    // same id and must stay, because it is not the row's id token.
    private static void MakeIndependent_CollidingIds_TangentNotCascaded()
    {
        byte[] canonical = SectionPointTests.Anchored("smooth", SectionPointTests.SmoothUpper(), SectionPointTests.SmoothLower());
        string text = Encoding.UTF8.GetString(canonical);
        int upper = text.IndexOf("upper cv {", StringComparison.Ordinal);
        int tangentAt = text.IndexOf("tangents {", upper, StringComparison.Ordinal);
        int curveClose = text.IndexOf('}', text.IndexOf('}', tangentAt) + 1);
        string upperCurve = text[upper..(curveClose + 1)];
        string swapped = upperCurve.Replace("\"cv-1\"", "\"cv-TMP\"", StringComparison.Ordinal)
            .Replace("\"cv-5\"", "\"cv-1\"", StringComparison.Ordinal)
            .Replace("\"cv-TMP\"", "\"cv-5\"", StringComparison.Ordinal);
        const string note = "# keep \"cv-1\" here";
        swapped = swapped.Replace("tangents { \"cv-1\" smooth } }", "tangents { \"cv-1\" smooth }\n      " + note + "\n    }", StringComparison.Ordinal);
        text = text[..upper] + swapped + text[(curveClose + 1)..];
        byte[] source = Encoding.UTF8.GetBytes(text);
        var before = FoilSource.Parse(source);
        if (!before.IsParsed || before.Definition is null) throw new InvalidOperationException("Fixture did not parse: " + before.Diagnostics[0].Code);
        var profile = before.Definition.Profiles[0];
        int vertex = Array.IndexOf(profile.Upper.Ids, "cv-1");
        Equal(5, vertex);
        Equal("cv-5", profile.Upper.Ids[1]);
        var made = FoilSource.MakeIndependent(source, profile.Name, 0);
        var after = FoilSource.Parse(made.Source);
        if (!after.IsParsed || after.Definition is null) throw new InvalidOperationException("Clone did not parse: " + after.Diagnostics[0].Code);
        var clone = after.Definition.Profiles.Single(item => item.Name == made.NewProfile);
        var kept = after.Definition.Profiles.Single(item => item.Name == profile.Name);
        Equal("cv-5", clone.Upper.Tangents[0].Id);
        Equal("cv-5", clone.Upper.Ids[vertex]);
        Equal("cv-1", clone.Upper.Ids[1]);
        Equal("smooth", clone.Upper.Tangents[0].Kind);
        string written = Encoding.UTF8.GetString(made.Source);
        Equal(true, ProfileBlock(written, made.NewProfile).Contains(note, StringComparison.Ordinal));
        Equal(true, ProfileBlock(written, profile.Name).Contains(note, StringComparison.Ordinal));
        Equal("cv-1", kept.Upper.Tangents[0].Id);
    }

    private static void ExpectIndependentTangent(byte[] source, string kind, string? translatedId)
    {
        var before = FoilSource.Parse(source);
        if (!before.IsParsed)
            throw new InvalidOperationException("Fixture did not parse: " + before.Diagnostics[0].Code + " " + before.Diagnostics[0].Reason);
        var profile = before.Definition!.Profiles[0];
        string oldTangentId = profile.Upper.Tangents[0].Id;
        int vertex = Array.IndexOf(profile.Upper.Ids, oldTangentId);
        if (vertex < 0) throw new InvalidOperationException("Tangent id " + oldTangentId + " is not an upper point");
        var made = FoilSource.MakeIndependent(source, profile.Name, 0);
        var after = FoilSource.Parse(made.Source);
        if (!after.IsParsed)
            throw new InvalidOperationException("Clone did not parse: " + after.Diagnostics[0].Code + " " + after.Diagnostics[0].Reason);
        var clone = after.Definition!.Profiles.Single(item => item.Name == made.NewProfile);
        var kept = after.Definition.Profiles.Single(item => item.Name == profile.Name);
        Equal(1, clone.Upper.Tangents.Length);
        Equal(kind, clone.Upper.Tangents[0].Kind);
        Equal(translatedId ?? oldTangentId, clone.Upper.Tangents[0].Id);
        Equal(clone.Upper.Ids[vertex], clone.Upper.Tangents[0].Id);
        Equal(1, kept.Upper.Tangents.Length);
        Equal(oldTangentId, kept.Upper.Tangents[0].Id);
        var stations = after.Authored().Assignments;
        Equal(made.NewProfile, stations[0].ProfileName);
        Equal(profile.Name, stations[1].ProfileName);
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

// The M1.1 test fixtures retain their original oracles while their setup now goes through the one-draft,
// step-based section API. This adapter holds only the selected point for a later Move step.
internal static class SectionDraftPort
{
    private static readonly Dictionary<string, (SurfaceSide Side, string VertexId)> targets = new(StringComparer.Ordinal);

    public static SessionDraft BeginSectionEdit(this AuthoringSession session, string id, int assignment,
        SectionScope scope, string side, string vertexId, ThicknessIntent intent = ThicknessIntent.KeepCurrent)
    {
        var view = session.BeginSectionDraft(id, assignment);
        if (scope == SectionScope.Independent)
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        if (intent == ThicknessIntent.UseSource)
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Thickness(intent));
        targets[id] = (side == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower, vertexId);
        return session.Snapshot().Draft!;
    }

    public static SessionDraft UpdateSectionPoint(this AuthoringSession session, string id, long generation, double x, double y)
    {
        var target = targets[id];
        session.ApplySectionStep(id, generation, new SectionStep.Move(target.Side, target.VertexId, x, y));
        return session.Snapshot().Draft!;
    }

    public static SessionDraft BeginSectionInsert(this AuthoringSession session, string id, int assignment, SectionScope scope, double x) =>
        Begin(session, id, assignment, scope, new SectionStep.Insert(SurfaceSide.Upper, x));

    public static SessionDraft BeginSectionDelete(this AuthoringSession session, string id, int assignment, SectionScope scope, int index)
    {
        var view = session.BeginSectionDraft(id, assignment);
        if (scope == SectionScope.Independent)
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        var points = Sections.View(view.Bytes, assignment, SurfaceSide.Upper, "preview", view.Generation).Points;
        if ((uint)index >= (uint)points.Count) throw new ContractError("DSL-PROFILE-TARGET");
        session.ApplySectionStep(id, view.Generation, new SectionStep.Delete(SurfaceSide.Upper, points[index].Id));
        return session.Snapshot().Draft!;
    }

    public static SessionDraft BeginSectionFair(this AuthoringSession session, string id, int assignment, SectionScope scope,
        double tolerance, PreserveEnds ends) =>
        Begin(session, id, assignment, scope, new SectionStep.Fair(null, tolerance, ends));

    public static SessionDraft BeginSectionRebuild(this AuthoringSession session, string id, int assignment, SectionScope scope,
        int count, double tolerance, PreserveEnds ends) =>
        Begin(session, id, assignment, scope, new SectionStep.Rebuild(null, count, tolerance, ends));

    public static SessionDraft BeginSectionImport(this AuthoringSession session, string id, int assignment, byte[] dat) =>
        Begin(session, id, assignment, SectionScope.Shared, new SectionStep.Import(dat));

    private static SessionDraft Begin(AuthoringSession session, string id, int assignment, SectionScope scope, SectionStep step)
    {
        var view = session.BeginSectionDraft(id, assignment);
        if (scope == SectionScope.Independent)
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        session.ApplySectionStep(id, view.Generation, step);
        return session.Snapshot().Draft!;
    }
}
