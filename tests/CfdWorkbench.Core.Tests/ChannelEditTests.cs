using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

internal static class ChannelEditTests
{
    internal static CurveView Channel(AuthoringSession session, string curve)
    {
        var snap = session.Snapshot();
        return Channels.View(snap.Draft?.Bytes ?? snap.Source, curve, snap.Draft is null ? "Accepted" : "Draft", snap.Draft?.Generation ?? 0);
    }

    internal static PointView ChannelPoint(AuthoringSession session, string curve, int index) =>
        Channel(session, curve).Points[index];

    private static GestureFrame Move(AuthoringSession session, string curve, int index, double ordinate)
    {
        var point = ChannelPoint(session, curve, index);
        var draft = session.BeginPointGesture(Id(), curve, point.Id);
        return session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, ordinate);
    }

    private static void Release(AuthoringSession session, GestureFrame frame) =>
        session.Apply(Id(), session.Validate(frame.Draft.Id, frame.Draft.Generation));

    private static byte[] SixteenTwist(string? kind = null)
    {
        string text = File.ReadAllText(M12bFixtures.Path("foil-41-sixteen-three-anchors.foil"));
        if (kind is null) return Encoding.UTF8.GetBytes(text);
        int start = text.IndexOf("twist cv {", StringComparison.Ordinal);
        int end = text.IndexOf('}', start);
        return Encoding.UTF8.GetBytes(text.Insert(end, " tangents { \"cv-7\" " + kind + " }"));
    }

    private static void SamePoint(PointView left, PointView right)
    {
        Equal(left.Id, right.Id);
        Equal(left.Index, right.Index);
        Equal(left.Eta, right.Eta);
        Equal(left.SpanMeters, right.SpanMeters);
        Equal(left.Ordinate, right.Ordinate);
        Equal(left.Role, right.Role);
        Equal(left.Kind, right.Kind);
        Equal(left.Freedom, right.Freedom);
        Equal(left.Locks.Count, right.Locks.Count);
        for (int i = 0; i < left.Locks.Count; i++) Equal(left.Locks[i], right.Locks[i]);
    }

    private static void SameCurve(CurveView left, CurveView right)
    {
        Equal(left.Curve, right.Curve);
        Equal(left.Ceiling, right.Ceiling);
        Equal(left.Knots.Count, right.Knots.Count);
        for (int i = 0; i < left.Knots.Count; i++) Equal(left.Knots[i], right.Knots[i]);
        Equal(left.Points.Count, right.Points.Count);
        for (int i = 0; i < left.Points.Count; i++) SamePoint(left.Points[i], right.Points[i]);
        Equal(left.Samples.Count, right.Samples.Count);
        for (int i = 0; i < left.Samples.Count; i++)
        {
            Equal(left.Samples[i].SpanMeters, right.Samples[i].SpanMeters);
            Equal(left.Samples[i].Ordinate, right.Samples[i].Ordinate);
        }
    }

    private static double PassedThrough(byte[] source, string curve, double eta)
    {
        var view = Channels.View(source, curve, "Accepted", 0);
        var points = view.Points.Select(point => new[] { point.Eta, point.Ordinate }).ToArray();
        return ChannelEvaluator.Value(view.Knots.ToArray(), 3, points, eta);
    }

    internal static void Run()
    {
        Check("PointModel_Dihedral_RootEndFixedTipEndValueOnly", () =>
        {
            using var session = Open();
            var curve = Channel(session, "dihedral");
            Equal(PointRole.RootEnd, curve.Points[0].Role);
            Equal(PointFreedom.Fixed, curve.Points[0].Freedom);
            Equal(PointRole.TipEnd, curve.Points[^1].Role);
            Equal(PointFreedom.ValueOnly, curve.Points[^1].Freedom);
        });
        Check("PointModel_DihedralRootMirror_RootHandleZeroSpanOnly", () =>
        {
            using var session = Open();
            var handle = ChannelPoint(session, "dihedral", 1);
            Equal(PointRole.RootHandle, handle.Role);
            Equal(PointFreedom.SpanOnly, handle.Freedom);
            Equal(0d, handle.Ordinate);
            True(handle.Locks.Contains("root_mirror"), "root mirror");
            var draft = session.BeginPointGesture(Id(), "dihedral", handle.Id);
            var frame = session.UpdatePointGesture(draft.Id, draft.Generation, handle.SpanMeters + 0.01, 0.05);
            Equal(0d, frame.Ordinate);
            True(Math.Abs(frame.SpanMeters - handle.SpanMeters) > 1e-6, "span still moves");
        });
        Check("PointModel_TwistAndThicknessRootMirror_RootEndValueOnlyCoupled", () =>
        {
            using var session = Open();
            foreach (string curve in new[] { "twist", "thickness" })
            {
                var root = ChannelPoint(session, curve, 0);
                var handle = ChannelPoint(session, curve, 1);
                Equal(PointFreedom.ValueOnly, root.Freedom);
                Equal(PointFreedom.SpanOnly, handle.Freedom);
                True(root.Locks.Contains("root_mirror"), curve + " root mirror");
                Equal(root.Ordinate, handle.Ordinate);
            }
            var twist = ChannelPoint(session, "twist", 0);
            var twistHandle = ChannelPoint(session, "twist", 1);
            var twistFrame = Move(session, "twist", 0, twist.Ordinate + 1);
            Equal(twist.SpanMeters, twistFrame.SpanMeters);
            Equal(twist.Ordinate + 1, twistFrame.Ordinate);
            Equal(twistHandle.Eta, ChannelPoint(session, "twist", 1).Eta);
            Equal(twistHandle.Ordinate + 1, ChannelPoint(session, "twist", 1).Ordinate);
            session.Cancel(twistFrame.Draft.Id);
            var thickness = ChannelPoint(session, "thickness", 0);
            var thicknessHandle = ChannelPoint(session, "thickness", 1);
            var thicknessFrame = Move(session, "thickness", 0, thickness.Ordinate + 0.001);
            Equal(thickness.SpanMeters, thicknessFrame.SpanMeters);
            Equal(thicknessHandle.Eta, ChannelPoint(session, "thickness", 1).Eta);
            Equal(thicknessHandle.Ordinate + 0.001, ChannelPoint(session, "thickness", 1).Ordinate);
        });
        Check("PointModel_NoLocks_RootHandleFree", () =>
        {
            string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
            byte[] source = Encoding.UTF8.GetBytes(text.Insert(text.LastIndexOf('}'), "  locks { }\n"));
            using var session = Open(source);
            Equal(PointFreedom.Fixed, ChannelPoint(session, "dihedral", 0).Freedom);
            Equal(PointFreedom.Free, ChannelPoint(session, "dihedral", 1).Freedom);
            Equal(PointFreedom.ValueOnly, ChannelPoint(session, "twist", 0).Freedom);
            Equal(PointFreedom.Free, ChannelPoint(session, "twist", 1).Freedom);
            Equal(0, ChannelPoint(session, "twist", 1).Locks.Count);
        });
        Check("Channels_UnitTable_QuantumLadderToleranceDomainPerCurve", () =>
        {
            foreach (string curve in new[] { "leading", "trailing" })
            {
                var unit = Channels.Unit(curve);
                Equal(curve, unit.Curve);
                Equal("m", unit.SiUnit);
                Equal("mm", unit.DisplayUnit);
                Equal(1e-6, unit.Quantum);
                Equal(1e-5, unit.NudgeFine);
                Equal(1e-4, unit.NudgePlain);
                Equal(1e-3, unit.NudgeCoarse);
                Equal(null, unit.RowTolerance);
                Equal(null, unit.DomainLower);
                Equal(null, unit.DomainUpper);
                Equal(Channels.AngleAndLength, unit.HandleTyping);
            }
            var dihedral = Channels.Unit("dihedral");
            Equal("m", dihedral.SiUnit);
            Equal("mm", dihedral.DisplayUnit);
            Equal(1e-6, dihedral.Quantum);
            Equal(1e-5, dihedral.NudgeFine);
            Equal(1e-4, dihedral.NudgePlain);
            Equal(1e-3, dihedral.NudgeCoarse);
            Equal(1e-6, dihedral.RowTolerance);
            Equal(null, dihedral.DomainLower);
            Equal(Channels.AngleAndLength, dihedral.HandleTyping);
            var twist = Channels.Unit("twist");
            Equal("deg", twist.SiUnit);
            Equal("°", twist.DisplayUnit);
            Equal(1e-5, twist.Quantum);
            Equal(0.01, twist.NudgeFine);
            Equal(0.1, twist.NudgePlain);
            Equal(1d, twist.NudgeCoarse);
            Equal(1e-6, twist.RowTolerance);
            Equal(0 - Geometry.TwistDomainDegrees, twist.DomainLower);
            Equal(Geometry.TwistDomainDegrees, twist.DomainUpper);
            Equal(Channels.SpanAndValue, twist.HandleTyping);
            var thickness = Channels.Unit("thickness");
            Equal("1", thickness.SiUnit);
            Equal("%", thickness.DisplayUnit);
            Equal(1e-7, thickness.Quantum);
            Equal(1e-4, thickness.NudgeFine);
            Equal(1e-3, thickness.NudgePlain);
            Equal(1e-2, thickness.NudgeCoarse);
            Equal(1e-8, thickness.RowTolerance);
            Equal(Geometry.ThicknessDomain.Lower, thickness.DomainLower);
            Equal(Geometry.ThicknessDomain.Upper, thickness.DomainUpper);
            Equal(Channels.SpanAndValue, thickness.HandleTyping);
        });
        Check("Channels_View_AnyChannelSameShapeAsRails", () =>
        {
            byte[] source = FoilSourceTests.Example;
            var plan = Planform.View(source, "Accepted", 0);
            SameCurve(plan.Leading, Channels.View(source, "leading", "Accepted", 0));
            var twist = Channels.View(source, "twist", "Accepted", 0);
            Equal(plan.Leading.Points.Count, twist.Points.Count);
            Equal(plan.Leading.Knots.Count, twist.Knots.Count);
            Equal(plan.Leading.Samples.Count, twist.Samples.Count);
            Equal(plan.Leading.Ceiling, twist.Ceiling);
            Equal(PointRole.RootEnd, twist.Points[0].Role);
            Equal(PointRole.TipEnd, twist.Points[^1].Role);
            Equal(-2d, twist.Points[^1].Ordinate);
            Equal(0d, plan.Leading.Points[^1].Ordinate);
        });
        Check("Planform_View_ComposesChannelsView", () =>
        {
            byte[] source = FoilSourceTests.Example;
            var plan = Planform.View(source, "Accepted", 0);
            SameCurve(plan.Leading, Channels.View(source, "leading", "Accepted", 0));
            SameCurve(plan.Trailing, Channels.View(source, "trailing", "Accepted", 0));
        });
        Check("UpdatePointGesture_ChannelPoints_QuantizedPerUnitTable", () =>
        {
            (string Curve, int Index, double Delta, double Scale)[] rows =
            [
                ("dihedral", 2, 0.002345678, 1e6),
                ("twist", 3, 0.0000126, 1e5),
                ("thickness", 3, 1.26e-7, 1e7)
            ];
            foreach (var row in rows)
            {
                using var session = Open();
                var point = ChannelPoint(session, row.Curve, row.Index);
                double expected = point.Ordinate + Math.Round(row.Delta * row.Scale, 0, MidpointRounding.ToEven) / row.Scale;
                var frame = Move(session, row.Curve, row.Index, point.Ordinate + row.Delta);
                Equal(expected, frame.Ordinate);
                Equal(false, frame.Clamped);
            }
        });
        Check("UpdatePointGesture_TwistBeyondDomain_ClampedAtDomain", () =>
        {
            using var session = Open();
            var frame = Move(session, "twist", 3, 1000);
            Equal(true, frame.Clamped);
            Equal(Geometry.TwistDomainDegrees, frame.Ordinate);
            Equal(Geometry.TwistDomainDegrees, ChannelPoint(session, "twist", 3).Ordinate);
        });
        Check("UpdatePointGesture_TwistBelowNegativeDomain_ClampedAtNegativeDomain", () =>
        {
            using var session = Open();
            var frame = Move(session, "twist", 3, -1000);
            Equal(true, frame.Clamped);
            Equal(0 - Geometry.TwistDomainDegrees, frame.Ordinate);
            Equal(0 - Geometry.TwistDomainDegrees, ChannelPoint(session, "twist", 3).Ordinate);
        });
        Check("UpdatePointGesture_ThicknessBelowZero_ClampedInsideOpenInterval", () =>
        {
            using var session = Open();
            var frame = Move(session, "thickness", 3, -1);
            Equal(true, frame.Clamped);
            Equal(Geometry.ThicknessDomain.Lower, frame.Ordinate);
            Equal(Geometry.ThicknessDomain.Lower, ChannelPoint(session, "thickness", 3).Ordinate);
        });
        Check("UpdatePointGesture_ThicknessAboveOne_ClampedBelowOne", () =>
        {
            using var session = Open();
            var frame = Move(session, "thickness", 3, 2);
            Equal(true, frame.Clamped);
            Equal(Geometry.ThicknessDomain.Upper, frame.Ordinate);
            Equal(Geometry.ThicknessDomain.Upper, ChannelPoint(session, "thickness", 3).Ordinate);
        });
        Check("UpdatePointGesture_OutOfDomainCvAtBegin_NotSnappedMovesInward", () =>
        {
            // CV 3 weight in the Bézier polygon is 2/3, so 80 stays outside ±D while the Bernstein span stays inside.
            const double planted = 80;
            byte[] source = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(FoilSourceTests.Example)
                .Replace("(0.5, -0.5)", "(0.5, 80)", StringComparison.Ordinal));
            using var session = Open(source);
            byte[] admitted = session.Snapshot().Source.ToArray();
            var point = ChannelPoint(session, "twist", 3);
            Equal(planted, point.Ordinate);
            var draft = session.BeginPointGesture(Id(), "twist", point.Id);
            True(draft.Bytes.AsSpan().SequenceEqual(admitted), "begin does not snap an out-of-domain control");
            Equal(planted, ChannelPoint(session, "twist", 3).Ordinate);
            var outward = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, planted + 20);
            Equal(true, outward.Clamped);
            Equal(planted, outward.Ordinate);
            var inward = session.UpdatePointGesture(outward.Draft.Id, outward.Draft.Generation, point.SpanMeters, 0);
            Equal(false, inward.Clamped);
            Equal(0d, inward.Ordinate);
            True(inward.Ordinate < planted, "the control moved inward");
        });
        Check("UpdatePointGesture_CoMovedHandleBeyondDomain_Clamped", () =>
        {
            using var session = Open(SixteenTwist());
            string id = ChannelPoint(session, "twist", 7).Id;
            session.ApplyPointCommand(Id(), new PointCommand.SetTangent("twist", id, TangentKind.Symmetric, null));
            Equal(TangentKind.Symmetric, Channel(session, "twist").Points.Single(point => point.Id == id).Kind);
            var frame = Move(session, "twist", 8, 80);
            Equal(true, frame.Clamped);
            Equal(Geometry.TwistDomainDegrees, ChannelPoint(session, "twist", 8).Ordinate);
            Equal(0 - Geometry.TwistDomainDegrees, ChannelPoint(session, "twist", 6).Ordinate);
        });
        Check("UpdatePointGesture_TwistSmoothHandleDrag_OppositeKeepsEtaOnLine", () =>
        {
            using var session = Open(SixteenTwist("smooth"));
            double oppositeEta = ChannelPoint(session, "twist", 6).Eta;
            Move(session, "twist", 8, 2);
            var left = ChannelPoint(session, "twist", 6);
            var anchor = ChannelPoint(session, "twist", 7);
            var right = ChannelPoint(session, "twist", 8);
            Equal(oppositeEta, left.Eta);
            double fraction = (anchor.Eta - left.Eta) / (right.Eta - left.Eta);
            double line = left.Ordinate + fraction * (right.Ordinate - left.Ordinate);
            Near(anchor.Ordinate, line, 1e-6);
        });
        Check("UpdatePointGesture_TwistRootEnd_HandleFollowsRootMirror", () =>
        {
            using var session = Open();
            var root = ChannelPoint(session, "twist", 0);
            var handle = ChannelPoint(session, "twist", 1);
            var frame = Move(session, "twist", 0, root.Ordinate + 1);
            Equal(root.SpanMeters, frame.SpanMeters);
            Equal(root.Ordinate + 1, frame.Ordinate);
            Equal(handle.Eta, ChannelPoint(session, "twist", 1).Eta);
            Equal(handle.Ordinate + 1, ChannelPoint(session, "twist", 1).Ordinate);
        });
        Check("Gesture_DihedralDrag_OneAcceptedRowUndoExact", () =>
        {
            using var session = Open();
            byte[] before = session.Snapshot().Source.ToArray();
            int rows = session.Envelope().Accepted.Length;
            var frame = Move(session, "dihedral", 2, ChannelPoint(session, "dihedral", 2).Ordinate + 0.002);
            Release(session, frame);
            Equal(rows + 1, session.Envelope().Accepted.Length);
            var edit = session.Envelope().Accepted[^1].Edit!;
            Equal("dihedral", edit.Rail);
            Equal(null, edit.Curve);
            session.Undo(Id());
            True(before.AsSpan().SequenceEqual(session.Snapshot().Source), "undo restores the source");
        });
        Check("Gesture_TwistReleaseAtDomainLimit_CertifiedApplied", () =>
        {
            using var session = Open();
            var frame = Move(session, "twist", 3, 1000);
            Equal(true, frame.Clamped);
            Equal(Geometry.TwistDomainDegrees, frame.Ordinate);
            Release(session, frame);
            Equal(Geometry.TwistDomainDegrees, ChannelPoint(session, "twist", 3).Ordinate);
        });
        Check("MakeAnchor_TwistControlPoint_PassesThroughWithinIdentity", () =>
        {
            using var session = Open();
            var point = ChannelPoint(session, "twist", 3);
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("twist", point.Id));
            var anchor = Channel(session, "twist").Points.Single(item => item.Id == point.Id);
            Equal(PointRole.Anchor, anchor.Role);
            True(outcome.PointsAfter > outcome.PointsBefore, "anchor adds handles");
            Near(point.Ordinate, anchor.Ordinate, 1e-6);
            Near(point.Ordinate, PassedThrough(session.Snapshot().Source, "twist", anchor.Eta), 1e-6);
        });
        Check("MakeAnchor_ThicknessAtCeiling_RefusedNamesCeiling", () =>
        {
            using var session = Open(SixteenTwist());
            session.ApplyPointCommand(Id(), new PointCommand.MakeControl("thickness", ChannelPoint(session, "thickness", 7).Id));
            Equal(14, Channel(session, "thickness").Points.Count);
            string message = "";
            try { session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("thickness", ChannelPoint(session, "thickness", 7).Id)); }
            catch (ContractError error) { Equal("DSL-CURVE", error.Code); message = error.Message; }
            True(message.Contains("16", StringComparison.Ordinal), "refusal names the 16-point ceiling");
        });
        Check("MakeControl_DihedralAnchor_LocalityWithinIdentity", () =>
        {
            using var session = Open(SixteenTwist());
            var before = Channel(session, "dihedral");
            session.ApplyPointCommand(Id(), new PointCommand.MakeControl("dihedral", before.Points[7].Id));
            var after = Channel(session, "dihedral");
            True(after.Points.Count < before.Points.Count, "control removes the handles");
            Equal(before.Points[0].Ordinate, after.Points[0].Ordinate);
            Equal(before.Points[^1].Ordinate, after.Points[^1].Ordinate);
            Equal(before.Points[0].Eta, after.Points[0].Eta);
            Equal(before.Points[^1].Eta, after.Points[^1].Eta);
        });
        Check("SetTangent_TwistSymmetric_MidpointRowHolds", () =>
        {
            using var session = Open(SixteenTwist());
            string id = ChannelPoint(session, "twist", 7).Id;
            session.ApplyPointCommand(Id(), new PointCommand.SetTangent("twist", id, TangentKind.Symmetric, null));
            var anchor = Channel(session, "twist").Points.Single(point => point.Id == id);
            Equal(TangentKind.Symmetric, anchor.Kind);
            var left = ChannelPoint(session, "twist", anchor.Index - 1);
            var right = ChannelPoint(session, "twist", anchor.Index + 1);
            Near(anchor.Eta, (left.Eta + right.Eta) / 2, 1e-9);
            Near(anchor.Ordinate, (left.Ordinate + right.Ordinate) / 2, 1e-6);
        });
        Check("ApplyPointCommand_DihedralRootEnd_DslLock", () =>
        {
            using var session = Open();
            byte[] before = session.Snapshot().Source.ToArray();
            int rows = session.Envelope().Accepted.Length;
            Refuses("DSL-LOCK", () => session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("dihedral", ChannelPoint(session, "dihedral", 0).Id)));
            True(before.AsSpan().SequenceEqual(session.Snapshot().Source), "source unchanged");
            Equal(rows, session.Envelope().Accepted.Length);
        });
        Check("Telemetry_ChannelEdits_CurveFamilyNoIdsOrPositions", () =>
        {
            using var session = Open();
            string twistId = ChannelPoint(session, "twist", 3).Id;
            var twist = Move(session, "twist", 3, ChannelPoint(session, "twist", 3).Ordinate + 1);
            Release(session, twist);
            var afterTwist = session.ReadLocalEvents();
            Equal("twist", afterTwist.Last(item => item.Operation == "gesture.end").CurveFamily);
            Equal("twist", afterTwist.Last(item => item.Operation == "document.apply" && item.Outcome == "OK").CurveFamily);
            string dihedralId = ChannelPoint(session, "dihedral", 2).Id;
            var dihedral = Move(session, "dihedral", 2, ChannelPoint(session, "dihedral", 2).Ordinate + 0.002);
            Release(session, dihedral);
            var events = session.ReadLocalEvents();
            Equal("dihedral", events.Last(item => item.Operation == "gesture.end").CurveFamily);
            Equal("dihedral", events.Last(item => item.Operation == "document.apply" && item.Outcome == "OK").CurveFamily);
            string json = JsonSerializer.Serialize(events);
            True(!json.Contains(twistId, StringComparison.Ordinal) && !json.Contains(dihedralId, StringComparison.Ordinal), "no vertex id");
            True(!json.Contains("\"Ordinate\"", StringComparison.Ordinal) && !json.Contains("\"SpanMeters\"", StringComparison.Ordinal), "no position");
        });
    }
}
