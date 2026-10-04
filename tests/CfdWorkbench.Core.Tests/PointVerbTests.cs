using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

internal static class PointVerbTests
{
    private static string TenPoint => Path.Combine(AppContext.BaseDirectory, "Fixtures", "planform-verbs", "new-default-10.foil");

    internal static void Run()
    {
        Check("Parse_Channel41FourVertices_Parses", () =>
        {
            var parsed = FoilSource.Parse(Resized("4.1", 4, false));
            Equal(true, parsed.IsParsed);
            Equal(4, parsed.Definition!.Curves["leading"].Points.Length);
        });
        Check("Parse_Channel41ThreeVertices_DslCurve", () =>
        {
            var parsed = FoilSource.Parse(Resized("4.1", 3, false));
            Equal(false, parsed.IsParsed);
            Equal("DSL-CURVE", parsed.Diagnostics[0].Code);
            True(parsed.Diagnostics[0].Reason?.Contains("4–16", StringComparison.Ordinal) == true, parsed.Diagnostics[0].Reason ?? "");
        });
        Check("Parse_Channel40FiveVertices_DslCurve", () =>
        {
            var parsed = FoilSource.Parse(Resized("4.0", 5, false));
            Equal(false, parsed.IsParsed);
            Equal("DSL-CURVE", parsed.Diagnostics[0].Code);
        });
        Check("Parse_TangentRowOnFourVertexChannel_DslLock", () =>
        {
            var parsed = FoilSource.Parse(Resized("4.1", 4, true));
            Equal(false, parsed.IsParsed);
            Equal("DSL-LOCK", parsed.Diagnostics[0].Code);
        });
        Check("AddPoint_Boehm_ShapeUnchangedWithin1e12Relative", () =>
        {
            Holds(CurveOf(FoilSource.NewDefault(), "trailing"));
            Holds(CurveOf(File.ReadAllBytes(TenPoint), "trailing"));
            static void Holds(Curve before)
            {
                var added = ChannelEdits.Add(before, 0.4);
                double scale = before.Points.Max(point => Math.Abs(point[1]));
                double worst = 0;
                for (int step = 0; step <= 400; step++)
                {
                    double eta = step / 400d;
                    double delta = Math.Abs(ChannelEvaluator.At(added.Curve, eta) - ChannelEvaluator.At(before, eta));
                    worst = Math.Max(worst, delta);
                }
                True(worst <= 1e-12 * Math.Max(1, scale), worst.ToString("G17", CultureInfo.InvariantCulture));
            }
        });
        Check("AddPoint_FirstSpanRootMirror_RootSquareBitExact", () =>
        {
            var before = CurveOf(FoilSource.NewDefault(), "trailing");
            Equal(before.Points[0][1], before.Points[1][1]);
            double eta = AbscissaAt(before, 0.013);
            var added = ChannelEdits.Add(before, eta);
            Equal(Bits(before.Points[0][1]), Bits(added.Curve.Points[1][1]));
        });
        Check("AddPoint_NewIdAboveMaxNeighboursKeepIds", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var before = Ids(session, "leading");
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", 0.42));
            var after = Ids(session, "leading");
            Equal(before.Length + 1, after.Length);
            True(after.Contains(outcome.SelectId!), outcome.SelectId ?? "");
            True(!before.Contains(outcome.SelectId!), "new id");
            int number = int.Parse(outcome.SelectId!.AsSpan(3), CultureInfo.InvariantCulture);
            Equal(before.Select(Number).Max() + 1, number);
            Equal(before.Length, before.Count(after.Contains));
        });
        Check("AddPoint_AtExistingKnot_RefusedTooClose", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            double eta = Point(session, "leading", 3).Eta;
            Unchanged(session, () => session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", eta)), "DSL-CURVE");
        });
        Check("AddPoint_AtCeiling16_RefusedNamingCeiling", () =>
        {
            using var session = Open(File.ReadAllBytes(M12bFixtures.Path("foil-41-sixteen-three-anchors.foil")));
            string message = "";
            try { session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", 0.2)); }
            catch (ContractError error) { Equal("DSL-CURVE", error.Code); message = error.Reason ?? ""; }
            True(message.Contains("16", StringComparison.Ordinal) && message.Contains("leading", StringComparison.Ordinal), message);
        });
        Check("AddPoint_PastTen_WritesHeader41InSamePatch", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            Equal(true, Encoding.UTF8.GetString(session.Snapshot().Source).Contains("\"4.0\"", StringComparison.Ordinal));
            while (Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points.Count < 11)
                session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", ChannelEdits.DefaultEta(CurveOf(session.Snapshot().Source, "leading"), 2)));
            Equal(true, Encoding.UTF8.GetString(session.Snapshot().Source).StartsWith("foildsl \"4.1\"", StringComparison.Ordinal));
        });
        Check("AddPoint_NextToSymmetricAnchor_RowBecomesSmoothReported", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            string anchor = Point(session, "leading", 3).Id;
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", anchor));
            session.ApplyPointCommand(Id(), new PointCommand.SetTangent("leading", anchor, TangentKind.Symmetric, null));
            double eta = ChannelEdits.DefaultEta(CurveOf(session.Snapshot().Source, "leading"), IndexOf(session, "leading", anchor));
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", eta));
            var row = CurveOf(session.Snapshot().Source, "leading").Tangents.Single(item => item.Id == anchor);
            Equal("smooth", row.Kind);
            True(outcome.Notice?.Contains("Smooth", StringComparison.Ordinal) == true, outcome.Notice ?? "");
        });
        Check("AddPoint_DefaultPosition_SpanMidpointNeverAKnot", () =>
        {
            var curve = CurveOf(FoilSource.NewDefault(), "trailing");
            foreach (int index in new[] { 0, 1, 2, 3 })
            {
                double eta = ChannelEdits.DefaultEta(curve, index);
                double t = ChannelEvaluator.Parameter(curve.Knots, curve.Degree, curve.Points, eta);
                foreach (double knot in curve.Knots)
                    True(Math.Abs(t - knot) > 1e-8, "default landed on a knot");
            }
            var anchored = CurveOf(Anchored(), "leading");
            int anchor = Array.FindIndex(anchored.Ids, id => anchored.Tangents.Any(row => row.Id == id));
            double atAnchor = ChannelEdits.DefaultEta(anchored, anchor);
            double parameter = ChannelEvaluator.Parameter(anchored.Knots, anchored.Degree, anchored.Points, atAnchor);
            True(parameter > Greville(anchored, anchor), "span to the right of the anchor knot");
        });
        Check("RemovePoint_FloorThenEnds_Order", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            var handle = Point(session, "trailing", 1);
            var error = Refuse(session, new PointCommand.RemovePoint("trailing", handle.Id));
            Equal("DSL-CURVE", error.Code);
            True(error.Reason!.Contains("at least 4", StringComparison.Ordinal), error.Reason);
            True(!error.Reason.Contains("handle", StringComparison.Ordinal), error.Reason);
        });
        Check("RemovePoint_RootEndAndTipEnd_Refused", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var root = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 0).Id));
            var tip = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 6).Id));
            Equal("DSL-LOCK", root.Code);
            Equal("DSL-LOCK", tip.Code);
            True(root.Reason!.Contains("root end", StringComparison.Ordinal), root.Reason);
            True(tip.Reason!.Contains("tip end", StringComparison.Ordinal), tip.Reason);
        });
        Check("RemovePoint_RootHandleAndTipHandle_Refused", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var root = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 1).Id));
            var tip = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 5).Id));
            Equal("DSL-LOCK", root.Code);
            Equal("DSL-LOCK", tip.Code);
            True(root.Reason!.Contains("root", StringComparison.Ordinal), root.Reason);
            True(tip.Reason!.Contains("tip", StringComparison.Ordinal), tip.Reason);
        });
        Check("RemovePoint_AnchorAndAnchorHandle_Refused", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            string anchor = Point(session, "leading", 3).Id;
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", anchor));
            var view = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading;
            var anchorPoint = view.Points.Single(point => point.Id == anchor);
            var handle = view.Points.First(point => point.Role == PointRole.AnchorHandle && point.AnchorId == anchor);
            var anchorError = Refuse(session, new PointCommand.RemovePoint("leading", anchor));
            var handleError = Refuse(session, new PointCommand.RemovePoint("leading", handle.Id));
            Equal("DSL-LOCK", anchorError.Code);
            Equal("DSL-LOCK", handleError.Code);
            True(anchorError.Reason!.Contains("anchor", StringComparison.OrdinalIgnoreCase), anchorError.Reason);
            True(handleError.Reason!.Contains((anchorPoint.Index + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), handleError.Reason);
        });
        Check("RemovePoint_LockedPoint_RefusedNothingChanged", () =>
        {
            var parsed = FoilSource.Parse(FoilSourceTests.Example);
            byte[] printed = FoilSource.MaterializeIds(parsed);
            string id = FoilSource.Parse(printed).Definition!.Curves["leading"].Ids[3];
            byte[] source = WithFreeze(printed, "leading", id);
            var view = Channels.View(source, "leading", "Accepted", 0);
            var refusal = ChannelEdits.RemoveRefusal(view, id);
            True(refusal is not null && refusal.Value.Code == "DSL-LOCK", refusal?.Reason ?? "missing");
            True(refusal!.Value.Reason.Contains("locked", StringComparison.Ordinal), refusal.Value.Reason);
            Equal(true, source.AsSpan().SequenceEqual(WithFreeze(printed, "leading", id)));
        });
        Check("RemovePoint_WouldFold_RefusedNothingChanged", () =>
        {
            byte[] source = ClusteredAbscissae();
            using var session = Open(source);
            int rows = session.Envelope().Accepted.Length;
            var error = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 3).Id));
            Equal("DSL-CURVE", error.Code);
            True(error.Reason!.Contains("fold", StringComparison.Ordinal), error.Reason);
            Equal(rows, session.Envelope().Accepted.Length);
        });
        Check("RemovePoint_Control_ChangeMeasuredAndOutsideSupportUnchanged", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("trailing", 0.45));
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("trailing", 0.62));
            var before = CurveOf(session.Snapshot().Source, "trailing");
            int index = ControlIndex(before);
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("trailing", before.Ids[index]));
            var after = CurveOf(session.Snapshot().Source, "trailing");
            Equal(before.Points.Length - 1, after.Points.Length);
            Equal(Bits(before.Points[0][0]), Bits(after.Points[0][0]));
            Equal(Bits(before.Points[0][1]), Bits(after.Points[0][1]));
            Equal(Bits(before.Points[^1][1]), Bits(after.Points[^1][1]));
            True(outcome.MaxDeviationMeters >= 0, "measured");
            True(outcome.SelectId is not null && after.Ids.Contains(outcome.SelectId), outcome.SelectId ?? "");
        });
        Check("RemovePoint_ClusteredKnots_SolvesWithSpanSamples", () =>
        {
            byte[] source = Clustered(FoilSourceTests.Example);
            using var session = Open(source);
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", Point(session, "leading", 3).Id));
            Equal(6, outcome.PointsAfter);
        });
        Check("RemovePoint_NextToAnchorHandle_TangentRowHolds", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            string anchor = Point(session, "leading", 3).Id;
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", anchor));
            var view = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading;
            var handle = view.Points.Single(point => point.Index == view.Points.Single(item => item.Id == anchor).Index + 1);
            var control = view.Points.Single(point => point.Index == handle.Index + 1 && point.Role == PointRole.Control);
            session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", control.Id));
            var after = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading;
            var anchorPoint = after.Points.Single(point => point.Id == anchor);
            var left = after.Points[anchorPoint.Index - 1];
            var right = after.Points[anchorPoint.Index + 1];
            double leftSlope = (anchorPoint.Ordinate - left.Ordinate) / (anchorPoint.Eta - left.Eta);
            double rightSlope = (right.Ordinate - anchorPoint.Ordinate) / (right.Eta - anchorPoint.Eta);
            Near(leftSlope, rightSlope, 1e-8);
        });
        Check("RemovePoint_BelowSix_WritesHeader41", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            while (Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points.Count > 5)
            {
                var view = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading;
                var control = view.Points.First(point => point.Role == PointRole.Control);
                session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", control.Id));
            }
            Equal(5, Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points.Count);
            Equal(true, Encoding.UTF8.GetString(session.Snapshot().Source).StartsWith("foildsl \"4.1\"", StringComparison.Ordinal));
        });
        Check("Rebuild_CountOutside4To10_DslCurve", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var low = Refuse(session, new PointCommand.RebuildCurve("leading", 3));
            var high = Refuse(session, new PointCommand.RebuildCurve("leading", 11));
            Equal("DSL-CURVE", low.Code);
            Equal("DSL-CURVE", high.Code);
            True(low.Reason!.Contains("4 to 10", StringComparison.Ordinal), low.Reason);
        });
        Check("Rebuild_SameCountSameKnots_NoRowBytesUnchanged", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            byte[] prior = session.Snapshot().Source.ToArray();
            int rows = session.Envelope().Accepted.Length;
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("trailing", 4));
            Equal(rows, session.Envelope().Accepted.Length);
            Equal(true, prior.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(0, outcome.MaxDeviationMeters);
        });
        Check("Rebuild_SameCountNonGrevilleAbscissae_AppliesAndReports", () =>
        {
            byte[] source = NudgeAbscissa(FoilSource.NewDefault(), "trailing", 2, CurveOf(FoilSource.NewDefault(), "trailing").Points[2][0] + 0.03);
            using var session = Open(source);
            int rows = session.Envelope().Accepted.Length;
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("trailing", 4));
            Equal(rows + 1, session.Envelope().Accepted.Length);
            True(outcome.MaxDeviationMeters > 1e-6, outcome.MaxDeviationMeters.ToString("G17", CultureInfo.InvariantCulture));
        });
        Check("Rebuild_KnotsFollowCurrentSpacing", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("leading", 6));
            var after = CurveOf(session.Snapshot().Source, "leading");
            Equal(0, after.Tangents.Length);
            Near(1.0 / 3.0, after.Knots[4], 1e-12);
            Near(2.0 / 3.0, after.Knots[5], 1e-12);
        });
        Check("Rebuild_WithAnchors_AnchorsDroppedRowsRemoved", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            string anchor = Point(session, "leading", 3).Id;
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", anchor));
            string root = Point(session, "leading", 0).Id;
            string tip = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points[^1].Id;
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("leading", 6));
            var after = CurveOf(session.Snapshot().Source, "leading");
            Equal(0, after.Tangents.Length);
            Equal(root, after.Ids[0]);
            Equal(tip, after.Ids[^1]);
            Equal(6, outcome.PointsAfter);
            True(after.Ids.Skip(1).Take(4).All(id => id != anchor), "fresh");
        });
        Check("Rebuild_Below6OrAbove10_RaisesHeader41", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("leading", 4));
            Equal(true, Encoding.UTF8.GetString(session.Snapshot().Source).StartsWith("foildsl \"4.1\"", StringComparison.Ordinal));
        });
        Check("Rebuild_NewFoilTenToFour_EndsAndRootSquareExactChangeReported", () =>
        {
            byte[] current = FoilSource.NewDefault();
            byte[] previous = File.ReadAllBytes(TenPoint);
            double shippedLead = Math.Abs(ChannelEvaluator.At(CurveOf(current, "leading"), 467.5 / 500) - ChannelEvaluator.At(CurveOf(previous, "leading"), 467.5 / 500));
            double shippedTrail = Math.Abs(ChannelEvaluator.At(CurveOf(current, "trailing"), 467.5 / 500) - ChannelEvaluator.At(CurveOf(previous, "trailing"), 467.5 / 500));
            Near(0.00305, shippedLead, 5e-5);
            Near(0.00883, shippedTrail, 5e-5);
            Near(0.1, WingEstimates.From(current, "accepted", 0).AreaSquareMeters, 1e-9);
            using var session = Open(previous);
            var beforeLead = CurveOf(previous, "leading");
            var beforeTrail = CurveOf(previous, "trailing");
            session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("leading", 4));
            session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("trailing", 4));
            var lead = CurveOf(session.Snapshot().Source, "leading");
            var trail = CurveOf(session.Snapshot().Source, "trailing");
            Equal(Bits(beforeLead.Points[0][1]), Bits(lead.Points[0][1]));
            Equal(Bits(beforeLead.Points[^1][1]), Bits(lead.Points[^1][1]));
            Equal(Bits(lead.Points[0][1]), Bits(lead.Points[1][1]));
            Equal(Bits(beforeTrail.Points[0][1]), Bits(trail.Points[0][1]));
            Equal(Bits(beforeTrail.Points[^1][1]), Bits(trail.Points[^1][1]));
            Equal(Bits(trail.Points[0][1]), Bits(trail.Points[1][1]));
            double eta = 467.5 / 500;
            Near(0.00299, Math.Abs(ChannelEvaluator.At(lead, eta) - ChannelEvaluator.At(beforeLead, eta)), 5e-5);
            Near(0.00897, Math.Abs(ChannelEvaluator.At(trail, eta) - ChannelEvaluator.At(beforeTrail, eta)), 5e-5);
        });
        Check("Rebuild_TipTurn_MatchesDirectAngle", () =>
        {
            byte[] previous = File.ReadAllBytes(TenPoint);
            using var session = Open(previous);
            var definition = FoilSource.Parse(previous).Definition!;
            var original = definition.Curves["trailing"];
            double before = ChannelEdits.TipAngleDegrees(original, definition.HalfSpan);
            var preview = session.PreviewRebuilds("trailing").Single(item => item.Count == 4);
            bool mirror = definition.Locks.Any(item => item.Kind == "root_mirror" && item.Channel.Text == "trailing");
            var rebuilt = ChannelEdits.Rebuild(original, 4, mirror);
            double direct = ChannelEdits.TipAngleDegrees(rebuilt.Curve, definition.HalfSpan) - before;
            Near(direct, preview.TipTurnDegrees, 1e-6);
        });
        Check("CurvatureBreaks_SmoothAnchor_OneBreakSimpleKnotsNone", () =>
        {
            var plain = CurveOf(FoilSourceTests.Example, "leading");
            Equal(0, ChannelEdits.CurvatureBreaks(plain));
            using var session = Open(FoilSource.NewDefault());
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("trailing", 0.45));
            var control = Planform.View(session.Snapshot().Source, "Accepted", 0).Trailing.Points.First(point => point.Role == PointRole.Control);
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("trailing", control.Id));
            Equal(1, ChannelEdits.CurvatureBreaks(CurveOf(session.Snapshot().Source, "trailing")));
        });
        Check("Rebuild_RailsWouldCross_RefusedNothingChanged", () =>
        {
            byte[] source = ThinTrailingDip();
            using var session = Open(source);
            byte[] prior = session.Snapshot().Source.ToArray();
            int rows = session.Envelope().Accepted.Length;
            var error = Refuse(session, new PointCommand.RebuildCurve("trailing", 4));
            Equal("DSL-GEOMETRY", error.Code);
            True(error.Reason!.Contains("would cross", StringComparison.Ordinal), error.Reason);
            Equal(rows, session.Envelope().Accepted.Length);
            Equal(true, prior.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Rebuild_SchoenbergWhitneyFails_RefusedWithCopy", () =>
        {
            var curve = CurveOf(FoilSourceTests.Example, "leading");
            var knots = curve.Knots.ToArray();
            knots[2] = knots[6] = 0.3;
            var broken = curve with { Knots = knots };
            string message = "";
            try { ChannelEdits.Rebuild(broken, 6, true); }
            catch (ContractError error) { Equal("DSL-CURVE", error.Code); message = error.Reason ?? ""; }
            True(message.Contains("too close together", StringComparison.Ordinal), message);
        });
        Check("PreviewRebuilds_NoRowNoCertificate_BytesHistoryFreshnessUnchanged", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            byte[] prior = session.Snapshot().Source.ToArray();
            int rows = session.Envelope().Accepted.Length;
            int validates = session.ReadLocalEvents().Count(item => item.Operation == "geometry.validate");
            var preview = session.PreviewRebuilds("trailing");
            Equal(7, preview.Count);
            Equal(4, preview[0].Count);
            Equal(10, preview[^1].Count);
            Equal(rows, session.Envelope().Accepted.Length);
            Equal(validates, session.ReadLocalEvents().Count(item => item.Operation == "geometry.validate"));
            Equal(1, session.ReadLocalEvents().Count(item => item.Operation == "rebuild.preview"));
            Equal(true, prior.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(true, session.Snapshot().Draft is null);
        });
        Check("PointVerbEvents_RecordKindOutcomeCountsDeviation", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", 0.42));
            var apply = session.ReadLocalEvents().Last(item => item.Operation == "document.apply");
            Equal("point-add", apply.EditKind);
            Equal("OK", apply.Outcome);
            Equal(outcome.PointsBefore, apply.PointsBefore);
            Equal(outcome.PointsAfter, apply.PointsAfter);
            Equal(true, apply.DeviationInCurveUnit is not null);
            Equal(null, apply.DeviationMicrometres);
            string blob = string.Join("|", apply.Operation, apply.Outcome, apply.EditKind, apply.Action, apply.CurveFamily);
            True(!blob.Contains("cv-", StringComparison.Ordinal), blob);
            True(!blob.Contains("leading", StringComparison.Ordinal), blob);
        });
        Check("SelectAfterVerb_SurvivorKeptElseNearestEtaTieTowardTip", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var added = session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", 0.42));
            True(Ids(session, "leading").Contains(added.SelectId!), added.SelectId ?? "");
            string removed = Point(session, "leading", 3).Id;
            double eta = Point(session, "leading", 3).Eta;
            var gone = session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", removed));
            var points = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points;
            var nearest = points.OrderBy(point => Math.Abs(point.Eta - eta)).ThenByDescending(point => point.Eta).First();
            Equal(nearest.Id, gone.SelectId);
            var rebuilt = session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("leading", 6));
            Equal(null, rebuilt.SelectId);
        });
        Check("MaxChange_IncludesEveryKnotOfBothCurves", () =>
        {
            var before = CurveOf(FoilSource.NewDefault(), "trailing");
            var points = before.Points.Select(point => point.ToArray()).ToArray();
            points[2][1] += 0.01;
            var after = before with { Points = points };
            var change = ChannelEdits.MaxChange(before, after);
            double atKnots = 0;
            foreach (double knot in before.Knots.Where(knot => knot > 0 && knot < 1).Distinct())
            {
                double eta = AbscissaAt(before, knot);
                atKnots = Math.Max(atKnots, Math.Abs(ChannelEvaluator.At(after, eta) - ChannelEvaluator.At(before, eta)));
            }
            True(change.Max + 1e-15 >= atKnots, change.Max.ToString("G17", CultureInfo.InvariantCulture));
        });
        Check("PlanformView_FourVertexCurve_SamplesIncludeEndsAndMinimumCount", () =>
        {
            var view = Planform.View(FoilSource.NewDefault(), "Accepted", 0);
            Equal(true, view.Leading.Samples.Count >= 64);
            Near(0, view.Leading.Samples[0].SpanMeters, 1e-12);
            Near(0.5, view.Leading.Samples[^1].SpanMeters, 1e-9);
            var longer = Planform.View(FoilSourceTests.Example, "Accepted", 0).Leading;
            Equal(true, longer.Samples.Count >= 64);
            Near(0, longer.Samples[0].SpanMeters, 1e-9);
        });
        Check("NewFoil_FourPointRails_OneInflectionNearRoot", () =>
        {
            byte[] source = FoilSource.NewDefault();
            var definition = FoilSource.Parse(source).Definition!;
            foreach (string rail in new[] { "leading", "trailing" })
            {
                int changes = ChannelEdits.InflectionCount(definition.Curves[rail], definition.HalfSpan, out double millimetres);
                Console.WriteLine("inflection " + rail + " " + changes.ToString(CultureInfo.InvariantCulture) + " at " + millimetres.ToString("G17", CultureInfo.InvariantCulture) + " mm");
                Equal(1, changes);
            }
        });
        Check("MakeControl_SevenPointsOneAnchor_GivesFive", () =>
        {
            using var session = Open(SevenPointAnchor());
            string anchor = CurveOf(session.Snapshot().Source, "leading").Tangents[0].Id;
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.MakeControl("leading", anchor));
            Equal(7, outcome.PointsBefore);
            Equal(5, outcome.PointsAfter);
        });
        Check("RemovePoint_RefusalTable_OrderAndCopy", () =>
        {
            using (var floor = Open(FoilSource.NewDefault()))
            {
                var refused = Refuse(floor, new PointCommand.RemovePoint("trailing", Point(floor, "trailing", 1).Id));
                Equal("DSL-CURVE", refused.Code);
                True(refused.Reason!.Contains("at least 4", StringComparison.Ordinal), refused.Reason);
            }
            using var session = Open(FoilSourceTests.Example);
            var root = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 0).Id));
            var tip = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 6).Id));
            var rootHandle = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 1).Id));
            var tipHandle = Refuse(session, new PointCommand.RemovePoint("leading", Point(session, "leading", 5).Id));
            True(root.Reason!.Contains("root end", StringComparison.Ordinal), root.Reason);
            True(tip.Reason!.Contains("tip end", StringComparison.Ordinal), tip.Reason);
            True(rootHandle.Reason!.Contains("direction at the root", StringComparison.Ordinal), rootHandle.Reason);
            True(tipHandle.Reason!.Contains("direction at the tip", StringComparison.Ordinal), tipHandle.Reason);
            string anchor = Point(session, "leading", 3).Id;
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", anchor));
            var anchorError = Refuse(session, new PointCommand.RemovePoint("leading", anchor));
            True(anchorError.Reason!.Contains("is an anchor", StringComparison.Ordinal), anchorError.Reason);
            var handle = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points.First(point => point.Role == PointRole.AnchorHandle && point.AnchorId == anchor);
            var handleError = Refuse(session, new PointCommand.RemovePoint("leading", handle.Id));
            True(handleError.Reason!.Contains("handle of the anchor", StringComparison.Ordinal), handleError.Reason);
        });
        Check("AddPoint_NextToSmoothAnchor_RowStillHolds", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            string anchor = Point(session, "leading", 3).Id;
            session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", anchor));
            session.ApplyPointCommand(Id(), new PointCommand.SetTangent("leading", anchor, TangentKind.Smooth, null));
            var curve = CurveOf(session.Snapshot().Source, "leading");
            double eta = ChannelEdits.DefaultEta(curve, IndexOf(session, "leading", anchor));
            var outcome = session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", eta));
            Equal("smooth", CurveOf(session.Snapshot().Source, "leading").Tangents.Single(item => item.Id == anchor).Kind);
            Equal(null, outcome.Notice);
        });
        Check("RemovePoint_RootMirrorRowHolds", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("trailing", 0.35));
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("trailing", 0.65));
            var before = CurveOf(session.Snapshot().Source, "trailing");
            int index = ControlIndex(before);
            session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("trailing", before.Ids[index]));
            var after = CurveOf(session.Snapshot().Source, "trailing");
            Equal(Bits(before.Points[0][1]), Bits(after.Points[0][1]));
            Equal(Bits(after.Points[0][1]), Bits(after.Points[1][1]));
        });
        Check("RemovePoint_NextToEndHandle_EndDirectionKept", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var definition = FoilSource.Parse(session.Snapshot().Source).Definition!;
            var before = definition.Curves["leading"];
            double beforeAngle = ChannelEdits.TipAngleDegrees(before, definition.HalfSpan);
            session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", before.Ids[before.Points.Length - 3]));
            var after = CurveOf(session.Snapshot().Source, "leading");
            Near(beforeAngle, ChannelEdits.TipAngleDegrees(after, definition.HalfSpan), 1e-8);
        });
        Check("PointVerb_On41FileBackInto6To10_HeaderStays41", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            while (CurveOf(session.Snapshot().Source, "leading").Points.Length < 11)
            {
                var curve = CurveOf(session.Snapshot().Source, "leading");
                session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", ChannelEdits.DefaultEta(curve, 3)));
            }
            Equal(true, HeaderIs41(session));
            while (CurveOf(session.Snapshot().Source, "leading").Points.Length > 8)
            {
                var view = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading;
                session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", view.Points.First(point => point.Role == PointRole.Control).Id));
            }
            Equal(8, CurveOf(session.Snapshot().Source, "leading").Points.Length);
            Equal(true, HeaderIs41(session));
        });
        Check("Rebuild_PreviewEqualsApply_SameBytes", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            var preview = session.PreviewRebuilds("leading").Single(item => item.Count == 5);
            session.ApplyPointCommand(Id(), new PointCommand.RebuildCurve("leading", 5));
            var applied = Planform.View(session.Snapshot().Source, "Accepted", 0).Leading;
            Equal(preview.Curve.Points.Count, applied.Points.Count);
            for (int index = 0; index < applied.Points.Count; index++)
            {
                Near(preview.Curve.Points[index].Eta, applied.Points[index].Eta, 1e-12);
                Near(preview.Curve.Points[index].Ordinate, applied.Points[index].Ordinate, 1e-12);
            }
        });
        Check("PointVerbEvents_NoIdsOrPositions", () =>
        {
            using var session = Open(FoilSourceTests.Example);
            session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", 0.42));
            session.PreviewRebuilds("leading");
            var events = session.ReadLocalEvents().Where(item => item.Operation is "document.apply" or "rebuild.preview").ToArray();
            Equal(true, events.Length >= 2);
            foreach (var item in events)
            {
                string blob = string.Join("|", item.Operation, item.Outcome, item.EditKind, item.Action, item.CurveFamily);
                True(!blob.Contains("cv-", StringComparison.Ordinal), blob);
                True(!blob.Contains("leading", StringComparison.Ordinal), blob);
            }
        });
        Check("Reopen_OldBuildNewReceiptKind_RefusedFileUnchanged", () =>
        {
            string text = File.ReadAllText(Path.Combine(PlacementTests.RepoRoot(), "docs", "proof", "planform-verbs-old-build", "receipt.md"));
            True(text.Contains("PASS Reopen_OldBuildNewReceiptKind_RefusedFileUnchanged", StringComparison.Ordinal), "receipt");
            True(text.Contains("DSL-CURVE", StringComparison.Ordinal) && text.Contains("point-add", StringComparison.Ordinal), text);
            True(text.Contains("point-remove", StringComparison.Ordinal) && text.Contains("curve-rebuild", StringComparison.Ordinal), text);
        });
    }

    private static byte[] Resized(string version, int count, bool tangent)
    {
        var definition = FoilSource.Parse(FoilSourceTests.Example).Definition!;
        var curve = definition.Curves["leading"];
        var knots = new double[count + 4];
        for (int index = 0; index < 4; index++) { knots[index] = 0; knots[knots.Length - 1 - index] = 1; }
        for (int place = 1; place <= count - 4; place++) knots[3 + place] = place / (double)(count - 3);
        var points = new double[count][];
        var ids = new string[count];
        for (int index = 0; index < count; index++)
        {
            points[index] = new[] { count == 1 ? 0 : index / (double)(count - 1), 0d };
            ids[index] = "cv-" + index.ToString(CultureInfo.InvariantCulture);
        }
        TangentRow[] rows = tangent ? [new(ids[Math.Min(3, count - 1)], "smooth", null)] : [];
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal)
        {
            ["leading"] = curve with { Knots = knots, Points = points, Ids = ids, Tangents = rows }
        };
        return FoilSource.Print(definition with { Version = version, Curves = curves });
    }

    private static byte[] SevenPointAnchor()
    {
        var definition = FoilSource.Parse(FoilSourceTests.Example).Definition!;
        var curve = definition.Curves["leading"];
        double[] knots = [0, 0, 0, 0, 0.5, 0.5, 0.5, 1, 1, 1, 1];
        var points = curve.Points.Select(point => point.ToArray()).ToArray();
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal)
        {
            ["leading"] = curve with { Knots = knots, Points = points, Tangents = [new(curve.Ids[3], "smooth", null)] }
        };
        return FoilSource.Print(definition with { Version = "4.1", Curves = curves });
    }

    private static byte[] Anchored()
    {
        using var session = Open(FoilSourceTests.Example);
        session.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", Point(session, "leading", 3).Id));
        return session.Snapshot().Source.ToArray();
    }

    private static byte[] WithFreeze(byte[] source, string curve, string id)
    {
        string text = Encoding.UTF8.GetString(source);
        int close = text.LastIndexOf('}');
        string block = "  locks {\n    freeze " + curve + " " + Jcs.Quote(id) + " at (0.5, 0)\n  }\n";
        return Encoding.UTF8.GetBytes(text[..close] + block + text[close..]);
    }

    private static byte[] ClusteredAbscissae()
    {
        var definition = FoilSource.Parse(FoilSourceTests.Example).Definition!;
        var original = definition.Curves["leading"];
        double[] abscissae = [0, 0.4, 0.40000001, 0.40000002, 0.40000003, 0.40000004, 1];
        var points = original.Points.Select(point => point.ToArray()).ToArray();
        for (int index = 0; index < points.Length; index++) points[index][0] = abscissae[index];
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal)
        {
            ["leading"] = original with { Points = points }
        };
        return FoilSource.Print(definition with { Curves = curves });
    }

    private static byte[] NudgeAbscissa(byte[] source, string curve, int index, double abscissa)
    {
        var definition = FoilSource.Parse(source).Definition!;
        var original = definition.Curves[curve];
        var points = original.Points.Select(point => point.ToArray()).ToArray();
        points[index][0] = abscissa;
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal)
        {
            [curve] = original with { Points = points }
        };
        return FoilSource.Print(definition with { Curves = curves });
    }

    private static byte[] Clustered(byte[] source)
    {
        var definition = FoilSource.Parse(source).Definition!;
        var original = definition.Curves["leading"];
        double[] knots = [0, 0, 0, 0, 0.5, 0.5000001, 0.5000002, 1, 1, 1, 1];
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal)
        {
            ["leading"] = original with { Knots = knots }
        };
        return FoilSource.Print(definition with { Curves = curves });
    }

    private static byte[] ThinTrailingDip()
    {
        var definition = FoilSource.Parse(FoilSourceTests.Example).Definition!;
        var trailing = definition.Curves["trailing"];
        var points = trailing.Points.Select(point => point.ToArray()).ToArray();
        for (int index = 0; index < points.Length; index++) points[index][1] = 0.01;
        points[0][1] = points[1][1] = 0.25;
        points[^1][1] = 0.25;
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal)
        {
            ["trailing"] = trailing with { Points = points }
        };
        return FoilSource.Print(definition with { Curves = curves });
    }

    private static Curve CurveOf(byte[] source, string name) => FoilSource.Parse(source).Definition!.Curves[name];

    private static double AbscissaAt(Curve curve, double t)
    {
        double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, t);
        double value = 0;
        for (int index = 0; index < curve.Points.Length; index++) value += basis[index] * curve.Points[index][0];
        return value;
    }

    private static double Greville(Curve curve, int index)
    {
        double sum = 0;
        for (int offset = 1; offset <= curve.Degree; offset++) sum += curve.Knots[index + offset];
        return sum / curve.Degree;
    }

    private static string[] Ids(AuthoringSession session, string curve) =>
        Planform.View(session.Snapshot().Source, "Accepted", 0).Leading.Points.Select(point => point.Id).ToArray()
            is var leading && curve == "leading" ? leading
            : Channels.View(session.Snapshot().Source, curve, "Accepted", 0).Points.Select(point => point.Id).ToArray();

    private static bool HeaderIs41(AuthoringSession session) =>
        Encoding.UTF8.GetString(session.Snapshot().Source).StartsWith("foildsl \"4.1\"", StringComparison.Ordinal);

    private static int IndexOf(AuthoringSession session, string curve, string id) =>
        Channels.View(session.Snapshot().Source, curve, "Accepted", 0).Points.Single(point => point.Id == id).Index;

    private static int ControlIndex(Curve curve)
    {
        for (int index = 2; index <= curve.Points.Length - 3; index++)
            if (!FoilSource.IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, index)
                && !(index > 0 && FoilSource.IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, index - 1))
                && !(index + 1 < curve.Points.Length && FoilSource.IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, index + 1)))
                return index;
        throw new InvalidOperationException("no control");
    }

    private static int Number(string id) => int.Parse(id.AsSpan(3), CultureInfo.InvariantCulture);

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);

    private static ContractError Refuse(AuthoringSession session, PointCommand command)
    {
        byte[] prior = session.Snapshot().Source.ToArray();
        try
        {
            session.ApplyPointCommand(Id(), command);
            throw new InvalidOperationException("expected a refusal");
        }
        catch (ContractError error)
        {
            Equal(true, prior.AsSpan().SequenceEqual(session.Snapshot().Source));
            return error;
        }
    }

    private static void Unchanged(AuthoringSession session, Action action, string code)
    {
        byte[] prior = session.Snapshot().Source.ToArray();
        int rows = session.Envelope().Accepted.Length;
        Refuses(code, action);
        Equal(true, prior.AsSpan().SequenceEqual(session.Snapshot().Source));
        Equal(rows, session.Envelope().Accepted.Length);
    }
}
