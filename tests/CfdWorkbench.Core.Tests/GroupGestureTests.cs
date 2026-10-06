using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

// Design group-move-node-m §6, §9 (Rulings 107, 111): several points of one curve move as one rigid gesture and one typed
// command. Ring: fast (every join); cost: under 2 s, in-memory sessions only. Mutants are planted by hand and recorded in
// docs/proof/grp/red-first.md (clamp per member instead of rigid; a handle seeded twice).
internal static class GroupGestureTests
{
    private static AuthoringSession OpenTip(double tipMm = 120)
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
        int rail = text.IndexOf("trailing cv", StringComparison.Ordinal);
        int last = text.IndexOf("(1, 120)", rail, StringComparison.Ordinal);
        return Open(Encoding.UTF8.GetBytes(text[..last] + "(1, " + tipMm.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" + text[(last + "(1, 120)".Length)..]));
    }

    private static string[] Ids(AuthoringSession s, string curve, params int[] indices) => indices.Select(i => Point(s, curve, i).Id).ToArray();

    private static CurveView Channel(byte[] bytes, string curve) => Channels.View(bytes, curve, "Draft", 0);

    private static double Chord(AuthoringSession s, GestureFrame frame, int end) => WingEstimates.ChordMeters(frame.Draft.Bytes, end);

    private static void Move(AuthoringSession s, string curve, int[] indices, int grab, double dSpan, double dOrd, out GestureFrame frame, out PointView[] before)
    {
        before = indices.Select(i => Point(s, curve, i)).ToArray();
        var g = Point(s, curve, grab);
        string[] ids = indices.Select(i => Point(s, curve, i).Id).ToArray();
        var d = s.BeginGroupGesture(Id(), curve, ids);
        frame = s.UpdateGroupGesture(d.Id, d.Generation, g.Id, g.SpanMeters + dSpan, g.Ordinate + dOrd);
    }

    private static PointView[] After(GestureFrame frame, string curve) =>
        (curve == "leading" ? Planform.View(frame.Draft.Bytes, "Draft", frame.Draft.Generation).Leading : Planform.View(frame.Draft.Bytes, "Draft", frame.Draft.Generation).Trailing).Points.ToArray();

    internal static void Run()
    {
        // Confirms the design's first assume: a one-member group is the single-point gesture. The oracle is the patch of the
        // translated point, written here from the drag's own quantum, so the check is not the same body compared with itself.
        Check("GroupGesture_OneMember_EqualsSinglePointGesture", () =>
        {
            foreach (var (bytes, curve, index) in new (byte[]?, string, int)[] { (null, "trailing", 2), (null, "trailing", 3), (null, "leading", 2), (Row(), "leading", 3), (Row(), "leading", 2) })
            foreach (double dSpan in new[] { 0, 0.0031, -0.0127 })
            foreach (double dOrd in new[] { 0, 0.0021, -0.0043, 0.0000004 })
            {
                using var s = Open(bytes); using var t = Open(bytes);
                var p = Point(s, curve, index);
                var d = s.BeginGroupGesture(Id(), curve, [p.Id]);
                var f = s.UpdateGroupGesture(d.Id, d.Generation, p.Id, p.SpanMeters + dSpan, p.Ordinate + dOrd);
                var d2 = t.BeginPointGesture(Id(), curve, p.Id);
                var f2 = t.UpdatePointGesture(d2.Id, d2.Generation, p.SpanMeters + dSpan, p.Ordinate + dOrd);
                True(f.Draft.Bytes.AsSpan().SequenceEqual(f2.Draft.Bytes), $"{curve}/{index}/{dSpan}/{dOrd}: group differs from the point gesture");
                var parsed = FoilSource.Parse(s.Snapshot().Source);
                var rail = Channels.View(s.Snapshot().Source, curve, "Accepted", 0).Points;
                double halfSpan = rail[^1].SpanMeters / rail[^1].Eta;
                double eta = Math.Round(dSpan / halfSpan, 7, MidpointRounding.ToEven), aft = Math.Round(dOrd * 1e6, 0, MidpointRounding.ToEven) / 1e6;
                var expected = new Dictionary<int, (double Eta, double Aft)>();
                for (int i = rail[index].Role == PointRole.Anchor ? index - 1 : index; i <= (rail[index].Role == PointRole.Anchor ? index + 1 : index); i++)
                    expected[i] = (rail[i].Eta + eta, rail[i].Ordinate + aft);
                if (rail[index].Role != PointRole.AnchorHandle) // a lone handle rotates about its anchor: equality with the point gesture is its oracle
                    True(f.Draft.Bytes.AsSpan().SequenceEqual(AuthoringSession.PatchGesture(parsed, curve, expected)), $"{curve}/{index}/{dSpan}/{dOrd}: group differs from the translated-set oracle");
            }
        });
        Check("GroupGesture_Translates_AllMembers_OneDraft", () =>
        {
            using var s = Open(); int accepted = s.Envelope().Accepted.Length;
            Move(s, "trailing", [2, 3, 4], 3, 0.004, 0.006, out var frame, out var before);
            var now = After(frame, "trailing");
            for (int k = 0; k < 3; k++)
            {
                Near(0.004, now[k + 2].SpanMeters - before[k].SpanMeters, 1e-7); Near(0.006, now[k + 2].Ordinate - before[k].Ordinate);
            }
            Near(before[1].SpanMeters - before[0].SpanMeters, now[3].SpanMeters - now[2].SpanMeters, 1e-9);
            Equal(3, frame.MovedIds.Count); Equal(1L, frame.Draft.Generation);
            var draft = s.Snapshot().Draft!;
            s.Apply(Id(), s.Validate(draft.Id, draft.Generation));
            Equal(accepted + 1, s.Envelope().Accepted.Length);
            s.Undo(Id()); True(Point(s, "trailing", 3).Ordinate == before[1].Ordinate, "undo restores every point exactly");
        });
        Check("GroupGesture_IncludesTipVertex_HoldsWholeGroupAtMinimum", () =>
        {
            using var s = OpenTip(6);
            Move(s, "trailing", [5, 6], 6, 0, -0.5 + 0.0, out var frame, out var before);
            Equal(GestureLimitKind.TipMinimum, frame.Limit!.Kind);
            Within(0.005, Chord(s, frame, 1), 1.1e-6);
            var now = After(frame, "trailing");
            Near(now[6].Ordinate - before[1].Ordinate, now[5].Ordinate - before[0].Ordinate);
            double oldRoot = WingEstimates.ChordMeters(s.Snapshot().Source, 0), oldTip = WingEstimates.ChordMeters(s.Snapshot().Source, 1);
            True(TipChord.Admits(oldTip, oldRoot, Chord(s, frame, 1), Chord(s, frame, 0)), "TipChord.Admits agrees on the held frame");
            var draft = s.Snapshot().Draft!;
            s.Apply(Id(), s.Validate(draft.Id, draft.Generation));
        });
        Check("GroupGesture_IncludesRootEnd_HoldsWholeGroupAtRootMax", () =>
        {
            using var s = OpenTip(6);
            Move(s, "trailing", [0, 2], 0, 0, 0.5, out var frame, out var before);
            Equal(GestureLimitKind.RootMaximum, frame.Limit!.Kind);
            Within(0.3, Chord(s, frame, 0), 1.1e-6);
            var now = After(frame, "trailing");
            Near(now[0].Ordinate - before[0].Ordinate, now[2].Ordinate - before[1].Ordinate);
            var draft = s.Snapshot().Draft!;
            s.Apply(Id(), s.Validate(draft.Id, draft.Generation));
        });
        Check("GroupGesture_RootBelow250_NeverClamps", () =>
        {
            using var s = OpenTip(6);
            double chord = WingEstimates.ChordMeters(s.Snapshot().Source, 0);
            Move(s, "trailing", [0, 2], 0, 0, 0.25 - chord, out var frame, out _);
            True(frame.Limit is null, "a 250 mm root was limited by a 6 mm tip");
            Within(0.25, Chord(s, frame, 0), 1.1e-6);
        });
        Check("GroupGesture_NeighbourSpacing_HoldsWholeGroupRigid", () =>
        {
            using var s = Open();
            Move(s, "trailing", [2, 3], 3, 1, 0, out var frame, out var before);
            True(frame.Clamped, "past the unselected neighbour is a clamp");
            var now = After(frame, "trailing");
            Near(before[1].SpanMeters - before[0].SpanMeters, now[3].SpanMeters - now[2].SpanMeters, 1e-9);
            True(now[3].SpanMeters < now[4].SpanMeters, "order kept");
        });
        Check("GroupGesture_ChannelDomain_HoldsGroupNotMember", () =>
        {
            string text = Encoding.UTF8.GetString(FoilSourceTests.Example).Replace("(0.5, 0.12), (0.7, 0.12)", "(0.5, 0.12), (0.7, 0.10)", StringComparison.Ordinal);
            using var s = Open(Encoding.UTF8.GetBytes(text));
            var unit = Channels.Unit("thickness");
            var points = Channels.View(s.Snapshot().Source, "thickness", "Accepted", 0).Points;
            var a = points[3]; var b = points[4];
            True(a.Ordinate != b.Ordinate, "fixture members sit at different distances from the bound");
            var d = s.BeginGroupGesture(Id(), "thickness", [a.Id, b.Id]);
            double target = (unit.DomainUpper ?? 1) * 4 - a.Ordinate;
            var frame = s.UpdateGroupGesture(d.Id, d.Generation, a.Id, a.SpanMeters, a.Ordinate + target);
            var now = Channels.View(frame.Draft.Bytes, "thickness", "Draft", frame.Draft.Generation).Points;
            True(frame.Clamped, "the domain clamp bound");
            Near(now[3].Ordinate - a.Ordinate, now[4].Ordinate - b.Ordinate, 1e-9);
        });
        Check("GroupGesture_FixedMember_RefusedNamingLock", () =>
        {
            using var s = Open();
            var root = Point(s, "leading", 0);
            var error = Throws(() => s.BeginGroupGesture(Id(), "leading", [root.Id, Point(s, "leading", 2).Id]));
            Equal("DSL-LOCK", error.Code); True(error.Message.Contains(root.Id, StringComparison.Ordinal), "the refusal names the locked point");
            Equal(null, s.Snapshot().Draft);
        });
        Check("GroupGesture_ValueOnlyMember_HoldsSpanForGroup", () =>
        {
            using var s = Open();
            Move(s, "trailing", [0, 2], 2, 0.01, 0.004, out var frame, out var before);
            var now = After(frame, "trailing");
            Near(before[0].SpanMeters, now[0].SpanMeters, 1e-12); Near(before[1].SpanMeters, now[2].SpanMeters, 1e-12);
            Near(0.004, now[2].Ordinate - before[1].Ordinate);
        });
        Check("GroupGesture_RootSeeded_WhenPointOneSelected", () =>
        {
            using var s = Open(FoilSource.NewDefault());
            foreach (int[] selection in new[] { new[] { 0, 1 }, new[] { 1, 2 } })
            {
                var plan = Planform.View(s.Snapshot().Source, "test", 0).Trailing.Points.ToArray();
                True(plan[1].Locks.Contains("root_mirror"), "fixture has the root mirror");
                var before = plan.Select(p => (p.SpanMeters, p.Ordinate)).ToArray();
                var d = s.BeginGroupGesture(Id(), "trailing", selection.Select(i => plan[i].Id).ToArray());
                var f = s.UpdateGroupGesture(d.Id, d.Generation, plan[selection[^1]].Id, plan[selection[^1]].SpanMeters + 0.01, plan[selection[^1]].Ordinate + 0.003);
                True(f.MovedIds.Contains(plan[0].Id), $"{string.Join(",", selection)}: the root rides in the group");
                var now = After(f, "trailing");
                foreach (int i in new[] { 0, 1, selection[^1] })
                {
                    Near(before[0].SpanMeters, now[0].SpanMeters, 1e-12);
                    Near(before[i].Ordinate + 0.003, now[i].Ordinate);
                    Near(before[i].SpanMeters, now[i].SpanMeters, 1e-9); // the root's span lock holds the group's span delta (DR-GM-5)
                }
                s.Cancel(d.Id);
            }
        });
        Check("GroupGesture_AnchorBringsHandles", () =>
        {
            using var s = Open(Row());
            var plan = Planform.View(s.Snapshot().Source, "test", 0).Leading.Points.ToArray();
            int a = Array.FindIndex(plan, p => p.Role == PointRole.Anchor);
            True(a > 0, "fixture has an anchor");
            int[] ids = [a, plan.Length - 2]; // the anchor and the tip handle, a plain free point
            Move(s, "leading", ids, a, 0.003, 0.002, out var frame, out var before);
            var now = After(frame, "leading");
            for (int i = a - 1; i <= a + 1; i++) { Near(0.003, now[i].SpanMeters - plan[i].SpanMeters, 1e-7); Near(0.002, now[i].Ordinate - plan[i].Ordinate); }
        });
        Check("GroupGesture_HandleWithoutAnchor_Refused", () =>
        {
            using var s = Open(Row());
            var plan = Planform.View(s.Snapshot().Source, "test", 0).Leading.Points.ToArray();
            int h = Array.FindIndex(plan, p => p.Role == PointRole.AnchorHandle), other = plan.Length - 2;
            True(h > 0, "fixture has a handle");
            Refuses("DSL-GROUP-HANDLE", () => s.BeginGroupGesture(Id(), "leading", [plan[h].Id, plan[other].Id]));
            Equal(null, s.Snapshot().Draft);
        });
        Check("GroupGesture_HandleNeverSeededTwice", () =>
        {
            using var s = Open(Row());
            var plan = Planform.View(s.Snapshot().Source, "test", 0).Leading.Points.ToArray();
            int a = Array.FindIndex(plan, p => p.Role == PointRole.Anchor);
            int[] ids = [a - 1, a, a + 1];
            Move(s, "leading", ids, a, 0.003, 0.002, out var frame, out _);
            var now = After(frame, "leading");
            foreach (int i in ids) { Near(0.003, now[i].SpanMeters - plan[i].SpanMeters, 1e-7); Near(0.002, now[i].Ordinate - plan[i].Ordinate); }
            Equal(3, frame.MovedIds.Count);
        });
        Check("GroupGesture_LegacyTipUnderMinimum_HoldsAtPressChord", () =>
        {
            using var s = OpenTip(2);
            Move(s, "trailing", [5, 6], 6, 0, 0.001 - Point(s, "trailing", 6).Ordinate, out var down, out _);
            Equal(GestureLimitKind.TipAlreadyUnder, down.Limit!.Kind);
            Within(0.002, Chord(s, down, 1), 1.1e-6);
            var tip = Point(s, "trailing", 6);
            var up = s.UpdateGroupGesture(down.Draft.Id, down.Draft.Generation, tip.Id, tip.SpanMeters, 0.04);
            True(up.Limit is null, "an upward move was held");
            Within(0.04, Chord(s, up, 1), 1.1e-6);
        });
        Check("GroupGesture_ReleaseNeverThrowsTipChordMin", () =>
        {
            foreach (double tipMm in new[] { 120.0, 6, 2 })
            foreach (string rail in new[] { "leading", "trailing" })
            foreach (int[] group in new[] { new[] { 5, 6 }, new[] { 4, 6 }, new[] { 2, 3 }, new[] { 0, 2 }, new[] { 0, 6 }, new[] { 3, 4, 5, 6 } })
            foreach (double aft in new[] { -0.5, -0.0123456, 0.0003, 0.00501, 0.1, 0.5 })
            {
                using var s = OpenTip(tipMm);
                var plan = Planform.View(s.Snapshot().Source, "test", 0);
                var points = (rail == "leading" ? plan.Leading : plan.Trailing).Points;
                if (group.Any(i => points[i].Freedom == PointFreedom.Fixed)) continue;
                var grabbed = points[group[^1]];
                var d = s.BeginGroupGesture(Id(), rail, group.Select(i => points[i].Id).ToArray());
                var frame = s.UpdateGroupGesture(d.Id, d.Generation, grabbed.Id, grabbed.SpanMeters, aft);
                try { s.Apply(Id(), s.Validate(d.Id, frame.Draft.Generation)); }
                catch (ContractError error) when (error.Code == "DSL-TIP-CHORD-MIN") { throw new InvalidOperationException($"{rail}/{string.Join(",", group)}/{aft} tip {tipMm}: release refused after a hold"); }
                catch (ContractError) { s.Cancel(d.Id); } // another rule (crossing edges) is not this track's
            }
        });
        Check("GroupGesture_End_CarriesMembers", () =>
        {
            using var s = Open();
            Move(s, "trailing", [2, 3, 4], 3, 0, 0.002, out _, out _);
            var d = s.Snapshot().Draft!;
            s.Apply(Id(), s.Validate(d.Id, d.Generation));
            Equal(3, s.ReadLocalEvents().Last(e => e.Operation == "gesture.end").Members);
            var p = Point(s, "trailing", 2); var single = s.BeginPointGesture(Id(), "trailing", p.Id); s.Cancel(single.Id);
            Equal(1, s.ReadLocalEvents().Last(e => e.Operation == "gesture.end").Members);
        });

        Check("ApplyGroupValue_SetTo_AllEqual", () =>
        {
            using var s = Open(); int accepted = s.Envelope().Accepted.Length;
            var outcome = s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 2, 3, 4), GroupValueMode.SetTo, GroupValueAxis.Value, 0.130));
            Equal(accepted + 1, s.Envelope().Accepted.Length); Equal(3, outcome.Members);
            foreach (int i in new[] { 2, 3, 4 }) Near(0.130, Point(s, "trailing", i).Ordinate, 1e-9);
            Equal(3, s.ReadLocalEvents().Last(e => e.Operation == "document.apply").Members);
            s.Undo(Id()); Near(0.120, Point(s, "trailing", 3).Ordinate, 1e-9);
        });
        Check("ApplyGroupValue_MoveBy_AllShifted", () =>
        {
            using var s = Open(); var before = new[] { 2, 3, 4 }.Select(i => Point(s, "trailing", i)).ToArray();
            s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 2, 3, 4), GroupValueMode.MoveBy, GroupValueAxis.Value, 0.005));
            for (int k = 0; k < 3; k++) Near(before[k].Ordinate + 0.005, Point(s, "trailing", k + 2).Ordinate, 1e-9);
            s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 2, 3), GroupValueMode.MoveBy, GroupValueAxis.Span, 0.004));
            for (int k = 0; k < 2; k++) Near(before[k].SpanMeters + 0.004, Point(s, "trailing", k + 2).SpanMeters, 1e-6);
        });
        Check("ApplyGroupValue_AnyViolation_NothingApplied", () =>
        {
            using var s = Open(); int accepted = s.Envelope().Accepted.Length; var source = s.Snapshot().Source;
            Refuses("DSL-GROUP-NEIGHBOUR", () => s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 2, 3), GroupValueMode.MoveBy, GroupValueAxis.Span, 5)));
            Refuses("DSL-GROUP-SPAN-SET", () => s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 2, 3), GroupValueMode.SetTo, GroupValueAxis.Span, 0.1)));
            Refuses("DSL-LOCK", () => s.ApplyGroupValue(Id(), new("leading", Ids(s, "leading", 0, 2), GroupValueMode.MoveBy, GroupValueAxis.Value, 0.001)));
            Refuses("DSL-GROUP-HANDLE", () =>
            {
                using var t = Open(Row()); var plan = Planform.View(t.Snapshot().Source, "test", 0).Leading.Points.ToArray();
                int h = Array.FindIndex(plan, p => p.Role == PointRole.AnchorHandle), c = plan.Length - 2;
                t.ApplyGroupValue(Id(), new("leading", [plan[h].Id, plan[c].Id], GroupValueMode.MoveBy, GroupValueAxis.Value, 0.001));
            });
            Refuses("DSL-TIP-CHORD-MIN", () => s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 5, 6), GroupValueMode.SetTo, GroupValueAxis.Value, 0.003)));
            Equal(accepted, s.Envelope().Accepted.Length); Equal(null, s.Snapshot().Draft);
            True(source.AsSpan().SequenceEqual(s.Snapshot().Source), "no byte changed");
            s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 2, 3), GroupValueMode.MoveBy, GroupValueAxis.Value, 0.001)); // the session is still usable
        });
        Check("ApplyGroupValue_TwistSpanMoveBy_NamesTheUnitAndThePointNumber_UseAmountIsAdmitted", () =>
        {
            // Repair 2: a twist span amount was bare metres ("0.2") and the neighbour was its internal id ("cv-4").
            using var s = Open();
            var interior = Channel(s.Snapshot().Source, "twist").Points
                .Where(p => p.Role is PointRole.Control or PointRole.Anchor && p.Freedom == PointFreedom.Free).Take(2).Select(p => p.Id).ToArray();
            Equal(2, interior.Length);
            var error = Throws(() => s.ApplyGroupValue(Id(), new("twist", interior, GroupValueMode.MoveBy, GroupValueAxis.Span, 0.2)));
            Equal("DSL-GROUP-NEIGHBOUR", error.Code);
            var match = System.Text.RegularExpressions.Regex.Match(error.Message,
                @"^Moving these points by 200\.00 mm would pass point (\d+)\. The most they can move that way is (\d+\.\d\d) mm\.$");
            True(match.Success, error.Message);
            True(!error.Message.Contains("cv-", StringComparison.Ordinal), error.Message);
            double most = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) / 1000;
            s.ApplyGroupValue(Id(), new("twist", interior, GroupValueMode.MoveBy, GroupValueAxis.Span, most)); // the named amount is admitted
        });
        Check("ApplyGroupValue_TwistSetToOutOfDomain_ReportsThePointAndTheRangeAsData", () =>
        {
            using var s = Open();
            var points = Channel(s.Snapshot().Source, "twist").Points
                .Where(p => p.Role is PointRole.Control or PointRole.Anchor && p.Freedom == PointFreedom.Free).Take(2).ToArray();
            var error = Throws(() => s.ApplyGroupValue(Id(), new("twist", points.Select(p => p.Id).ToArray(), GroupValueMode.SetTo, GroupValueAxis.Value, 100)));
            Equal("DSL-GROUP-RANGE", error.Code);
            True(System.Text.RegularExpressions.Regex.IsMatch(error.Message, @"^point=\d+;min=-57\.29\d*;max=57\.29\d*;unit=°$"), error.Message);
        });
        Check("ApplyGroupValue_MoveByPastTip_NamesMaxAmount", () =>
        {
            using var s = OpenTip(30);
            var error = Throws(() => s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 5, 6), GroupValueMode.MoveBy, GroupValueAxis.Value, -0.028)));
            Equal("DSL-TIP-CHORD-MIN", error.Code);
            True(error.Message.Contains("The most they can move that way is 25.00 mm.", StringComparison.Ordinal), error.Message);
            True(error.Message.Contains("below 5 mm", StringComparison.Ordinal), error.Message);
            s.ApplyGroupValue(Id(), new("trailing", Ids(s, "trailing", 5, 6), GroupValueMode.MoveBy, GroupValueAxis.Value, -0.025)); // the named amount is admitted
            Within(0.005, WingEstimates.ChordMeters(s.Snapshot().Source, 1), 1.1e-6);
        });
    }

    private static void Within(double expected, double actual, double tolerance) =>
        True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}; actual {actual}");

    private static ContractError Throws(Action action)
    {
        try { action(); }
        catch (ContractError error) { return error; }
        throw new InvalidOperationException("expected a ContractError");
    }
}
