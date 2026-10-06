using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Ruling 93: minimum tip chord max(5 mm, 2 % of root chord), one Core definition, enforced where every edit is accepted
// (AuthoringSession.Commit). Ring: fast (every join); cost: under 1 s, in-memory sessions only.
internal static class TipChordTests
{
    private const string Code = "DSL-TIP-CHORD-MIN";

    internal static void Run()
    {
        Check("TipChordMinimum_Definition_LargerOfFiveMmAndTwoPercent", () =>
        {
            Within(0.005, TipChord.MinimumMeters(0.120));
            Within(0.005, TipChord.MinimumMeters(0.250));
            Within(0.008, TipChord.MinimumMeters(0.400), 1e-12);
            Within(0.005, TipChord.MinimumMeters(double.NaN));
        });
        Check("TipChord_Drag_BelowMinimum_HoldsAtMinimum_ReleaseCommitsHeldValue", () =>
        {
            using var s = Open();
            var tip = TipPoint(s);
            int accepted = s.Envelope().Accepted.Length;
            var draft = s.BeginPointGesture(Id(), "trailing", tip.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, tip.SpanMeters, 0.003);
            if (!frame.Clamped) throw new InvalidOperationException("the frame was not marked clamped");
            Equal(GestureLimitKind.TipMinimum, frame.Limit!.Kind);
            Near(0.005, frame.Limit.LimitMeters);
            double held = WingEstimates.ChordMeters(frame.Draft.Bytes, 1);
            Within(0.005, held, 1.1e-6);
            if (held < 0.005 - 1e-9) throw new InvalidOperationException($"held under the minimum: {held}");
            s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation));
            Equal(accepted + 1, s.Envelope().Accepted.Length);
            Equal(held, WingEstimates.ChordMeters(s.Snapshot().Source, 1));
        });
        Check("TipChord_Drag_LeadingTip_TowardTrailing_HoldsAtMinimum", () =>
        {
            using var s = Open();
            var tip = Planform.View(s.Snapshot().Source, "test", 0).Leading.Points[^1];
            var draft = s.BeginPointGesture(Id(), "leading", tip.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, tip.SpanMeters, tip.Ordinate + 0.5);
            Equal(GestureLimitKind.TipMinimum, frame.Limit!.Kind);
            Within(0.005, WingEstimates.ChordMeters(frame.Draft.Bytes, 1), 1.1e-6);
            s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation));
        });
        Check("TipChord_Drag_Escape_AfterHold_AddsNoRevision", () =>
        {
            using var s = Open();
            var tip = TipPoint(s);
            int accepted = s.Envelope().Accepted.Length;
            var draft = s.BeginPointGesture(Id(), "trailing", tip.Id);
            s.UpdatePointGesture(draft.Id, draft.Generation, tip.SpanMeters, 0.003);
            s.Cancel(draft.Id);
            Equal(accepted, s.Envelope().Accepted.Length);
        });
        Check("TipChord_RootDrag_PastTwoPercentOfTip_HoldsAtRootMaximum", () =>
        {
            using var s = Open(tipMm: 6);
            var root = Planform.View(s.Snapshot().Source, "test", 0).Trailing.Points[0];
            var draft = s.BeginPointGesture(Id(), "trailing", root.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, root.SpanMeters, root.Ordinate + 0.5);
            Equal(GestureLimitKind.RootMaximum, frame.Limit!.Kind);
            Within(0.3, frame.Limit.LimitMeters, 1e-9);
            Near(0.006, frame.Limit.OtherChordMeters);
            Within(0.3, WingEstimates.ChordMeters(frame.Draft.Bytes, 0), 1.1e-6);
            Within(0.006, WingEstimates.ChordMeters(frame.Draft.Bytes, 1), 1e-9);
            s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation));
        });
        Check("TipChord_RootDrag_AtOrUnder250mm_NeverClamps", () =>
        {
            using var s = Open(tipMm: 6);
            var root = Planform.View(s.Snapshot().Source, "test", 0).Trailing.Points[0];
            double chord = WingEstimates.ChordMeters(s.Snapshot().Source, 0);
            var draft = s.BeginPointGesture(Id(), "trailing", root.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, root.SpanMeters, root.Ordinate + (0.25 - chord));
            if (frame.Limit is not null) throw new InvalidOperationException("a 250 mm root was limited by a 6 mm tip");
            Within(0.25, WingEstimates.ChordMeters(frame.Draft.Bytes, 0), 1.1e-6);
        });
        Check("TipChord_LegacyTipUnderMinimum_DragCanOnlyMoveTowardLegality", () =>
        {
            using var s = Open(tipMm: 2);
            var tip = TipPoint(s);
            var draft = s.BeginPointGesture(Id(), "trailing", tip.Id);
            var down = s.UpdatePointGesture(draft.Id, draft.Generation, tip.SpanMeters, 0.001);
            Equal(GestureLimitKind.TipAlreadyUnder, down.Limit!.Kind);
            Near(0.002, down.Limit.LimitMeters);
            Within(0.002, WingEstimates.ChordMeters(down.Draft.Bytes, 1), 1.1e-6);
            var up = s.UpdatePointGesture(draft.Id, down.Draft.Generation, tip.SpanMeters, 0.04);
            if (up.Limit is not null) throw new InvalidOperationException("an upward move was held");
            Within(0.04, WingEstimates.ChordMeters(up.Draft.Bytes, 1), 1.1e-6);
        });
        Check("TipChord_Drag_EveryHeldFrame_IsAdmitted_ReleaseNeverRefused", () =>
        {
            foreach (double tipMm in new[] { 120.0, 6, 2 })
            foreach (string rail in new[] { "leading", "trailing" })
            foreach (int end in new[] { 0, -1 })
            foreach (double aft in new[] { -0.5, -0.05, -0.0123456, 0.0003, 0.0049, 0.00501, 0.1, 0.31, 0.5 })
            {
                using var s = Open(tipMm);
                var plan = Planform.View(s.Snapshot().Source, "test", 0);
                var curve = rail == "leading" ? plan.Leading : plan.Trailing;
                var vertex = end == 0 ? curve.Points[0] : curve.Points[^1];
                if (vertex.Freedom == PointFreedom.Fixed) continue;
                var draft = s.BeginPointGesture(Id(), rail, vertex.Id);
                var frame = s.UpdatePointGesture(draft.Id, draft.Generation, vertex.SpanMeters, aft);
                double root = WingEstimates.ChordMeters(frame.Draft.Bytes, 0), tip = WingEstimates.ChordMeters(frame.Draft.Bytes, 1);
                double oldRoot = WingEstimates.ChordMeters(s.Snapshot().Source, 0), oldTip = WingEstimates.ChordMeters(s.Snapshot().Source, 1);
                if (!TipChord.Admits(oldTip, oldRoot, tip, root))
                    throw new InvalidOperationException($"{rail}/{end}/{aft} tip {tipMm}: held frame is not admitted (tip {tip}, root {root})");
                try { s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation)); }
                catch (ContractError error) when (error.Code == Code) { throw new InvalidOperationException($"{rail}/{end}/{aft} tip {tipMm}: release refused after a hold"); }
                catch (ContractError) { s.Cancel(draft.Id); } // another rule (for example crossing edges) is not this track's
            }
        });
        Check("TipChord_InteriorVerticesAndHandles_NeverChangeEitherChord", () =>
        {
            using var s = Open();
            double root = WingEstimates.ChordMeters(s.Snapshot().Source, 0), tip = WingEstimates.ChordMeters(s.Snapshot().Source, 1);
            var plan = Planform.View(s.Snapshot().Source, "test", 0);
            foreach (var (rail, curve) in new[] { ("leading", plan.Leading), ("trailing", plan.Trailing) })
            for (int i = 1; i < curve.Points.Count - 1; i++)
            foreach (double aft in new[] { -1.0, 1.0 })
            foreach (double span in new[] { curve.Points[i].SpanMeters, -1.0, 1.0 })
            {
                var point = curve.Points[i];
                if (point.Freedom == PointFreedom.Fixed) continue;
                using var t = Open();
                var draft = t.BeginPointGesture(Id(), rail, point.Id);
                var frame = t.UpdatePointGesture(draft.Id, draft.Generation, span, aft);
                Within(root, WingEstimates.ChordMeters(frame.Draft.Bytes, 0), 1e-9);
                Within(tip, WingEstimates.ChordMeters(frame.Draft.Bytes, 1), 1e-9);
                if (frame.Limit is not null) throw new InvalidOperationException($"{rail} point {i} reported a chord limit");
            }
        });
        Check("TipChord_NumericEntry_BelowMinimum_Refused", () =>
        {
            using var s = Open();
            string before = s.Snapshot().AcceptedId;
            var error = Throws(() => s.ApplyChord(Id(), new("tip-chord", "3")));
            Equal(Code, error.Code);
            Equal(before, s.Snapshot().AcceptedId);
            Equal(null, s.Snapshot().Draft);
        });
        Check("TipChord_DrivingDimension_NamesTheLargerMinimum", () =>
        {
            using var s = Open();
            s.ApplyChord(Id(), new("root-chord", "400"));
            var error = Throws(() => s.ApplyChord(Id(), new("tip-chord", "7")));
            Equal(Code, error.Code);
            Equal("Tip chord can't go below 8 mm (the larger of 5 mm and 2 % of the root chord). Enter 8 mm or more.", error.Reason!);
            s.ApplyChord(Id(), new("tip-chord", "8"));
        });
        Check("TipChord_RootChordRaise_ThatPushesTipUnder_Refused", () =>
        {
            using var s = Open(tipMm: 6);
            Equal(Code, Throws(() => s.ApplyChord(Id(), new("root-chord", "400"))).Code);
        });
        Check("TipChord_RootChordRefusal_NamesRootAndMaximum_TipChordRefusalNamesTipAndWayOut", () =>
        {
            using var s = Open(tipMm: 6);
            Equal("Root chord can't go above 300 mm while the tip chord is 6 mm (the tip must stay at least 2 % of the root). Widen the tip first.",
                Throws(() => s.ApplyChord(Id(), new("root-chord", "400"))).Reason!);
            using var t = Open();
            Equal("Tip chord can't go below 5 mm (the larger of 5 mm and 2 % of the root chord). Enter 5 mm or more.",
                Throws(() => t.ApplyChord(Id(), new("tip-chord", "3"))).Reason!);
        });
        Check("TipChord_RootMaximum_IsTheLargestRootAdmitted", () =>
        {
            foreach (double tip in new[] { 0.006, 0.01, 0.0123 })
            {
                double max = TipChord.MaximumRootMeters(tip, 0.1);
                if (!TipChord.Admits(tip, 0.1, tip, max)) throw new InvalidOperationException($"{max} refused for tip {tip}");
                if (TipChord.Admits(tip, 0.1, tip, max + 1e-6)) throw new InvalidOperationException($"{max + 1e-6} admitted for tip {tip}");
            }
        });
        Check("TipChord_ExactlyMinimum_Admitted", () =>
        {
            using var s = Open();
            s.ApplyChord(Id(), new("tip-chord", "5"));
            Near(0.005, WingEstimates.ChordMeters(s.Snapshot().Source, 1));
        });
        Check("TipChord_DragToExactlyMinimum_Admitted", () =>
        {
            using var s = Open();
            var tip = TipPoint(s);
            var draft = s.BeginPointGesture(Id(), "trailing", tip.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, tip.SpanMeters, 0.005);
            s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation));
            Near(0.005, WingEstimates.ChordMeters(s.Snapshot().Source, 1));
        });
        Check("TipChord_OldFileBelowMinimum_OpensAndEditsUpAreAdmitted", () =>
        {
            using var s = Open(tipMm: 2);
            Equal(GeometryStatus.Certified, s.InspectAccepted().Geometry.Status);
            Equal(Code, Throws(() => s.ApplyChord(Id(), new("tip-chord", "1.9"))).Code);
            s.ApplyChord(Id(), new("tip-chord", "3"));
            s.ApplyChord(Id(), new("tip-chord", "6"));
        });
        Check("TipChord_Admits_RepeatedTinyDownwardEdits_CannotRatchetBelowMinimum", () =>
        {
            double root = 0.120, min = TipChord.MinimumMeters(root), tip = min;
            int admitted = 0;
            for (int i = 0; i < 10; i++)
            {
                double next = tip - 0.9e-9;
                if (!TipChord.Admits(tip, root, next, root)) break;
                tip = next; admitted++;
            }
            if (tip < min - 1.1e-9) throw new InvalidOperationException($"ratcheted {admitted} edits to {min - tip} m under the minimum");
        });
        Check("TipChord_Admits_NonFiniteOldTip_AdmitsRepairingEdit", () =>
        {
            if (!TipChord.Admits(double.NaN, 0.120, 0.001, 0.120)) throw new InvalidOperationException("repair refused");
            if (!TipChord.Admits(0.001, double.NaN, 0.001, 0.120)) throw new InvalidOperationException("repair refused (root)");
        });
        Check("TipChord_RefusedDimension_RetryReportsRefusalThenLegalEditAccepted", () =>
        {
            using var s = Open();
            string op = Id();
            Equal(Code, Throws(() => s.ApplyChord(op, new("tip-chord", "3"))).Code);
            Equal(Code, Throws(() => s.ApplyChord(op, new("tip-chord", "3"))).Code);
            Equal(null, s.Snapshot().Draft);
            s.ApplyChord(Id(), new("tip-chord", "50"));
        });
        Check("TipChord_RefusedTypedEdit_SessionStaysUsableAndHeldGestureAccepted", () =>
        {
            using var s = Open();
            Equal(Code, Throws(() => s.ApplyChord(Id(), new("tip-chord", "3"))).Code);
            Equal(null, s.Snapshot().Draft);
            var draft = s.BeginPointGesture(Id(), "trailing", TipPoint(s).Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, TipPoint(s).SpanMeters, 0.003);
            s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation));
            s.ApplyChord(Id(), new("tip-chord", "50"));
        });
        Check("TipChord_EditThatDoesNotTouchThePlanform_AdmittedOnOldFile", () =>
        {
            using var s = Open(tipMm: 2);
            var point = Channels.View(s.Snapshot().Source, "twist", "Accepted", 0).Points[3];
            var draft = s.BeginPointGesture(Id(), "twist", point.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + 1);
            s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation));
        });
    }

    private static AuthoringSession Open(double tipMm = 120)
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
        int rail = text.IndexOf("trailing cv", StringComparison.Ordinal);
        int last = text.IndexOf("(1, 120)", rail, StringComparison.Ordinal);
        byte[] bytes = Encoding.UTF8.GetBytes(text[..last] + "(1, " + tipMm.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" + text[(last + "(1, 120)".Length)..]);
        var session = new AuthoringSession();
        session.Open(bytes, Guid.NewGuid().ToString("D"), true);
        return session;
    }

    private static PointView TipPoint(AuthoringSession s)
    {
        var snap = s.Snapshot();
        return Planform.View(snap.Source, "test", 0).Trailing.Points[^1];
    }

    private static string Id() => Guid.NewGuid().ToString("D");

    private static void Within(double expected, double actual, double tolerance = 0)
    {
        if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException($"expected {expected}; actual {actual}");
    }

    private static void Near(double expected, double actual) => Within(expected, actual, 1e-9);

    private static ContractError Throws(Action action)
    {
        try { action(); }
        catch (ContractError error) { return error; }
        throw new InvalidOperationException("expected a ContractError");
    }
}
