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
        Check("TipChord_Drag_BelowMinimum_RefusedNothingAccepted", () =>
        {
            using var s = Open();
            var tip = TipPoint(s);
            var draft = s.BeginPointGesture(Id(), "trailing", tip.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, tip.SpanMeters, 0.003);
            int accepted = s.Envelope().Accepted.Length;
            var error = Throws(() => s.Apply(Id(), s.Validate(draft.Id, frame.Draft.Generation)));
            Equal(Code, error.Code);
            Equal("Tip chord can't go below 5 mm (the larger of 5 mm and 2 % of the root chord).", error.Reason!);
            Equal(accepted, s.Envelope().Accepted.Length);
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
            Equal("Tip chord can't go below 8 mm (the larger of 5 mm and 2 % of the root chord).", error.Reason!);
            s.ApplyChord(Id(), new("tip-chord", "8"));
        });
        Check("TipChord_RootChordRaise_ThatPushesTipUnder_Refused", () =>
        {
            using var s = Open(tipMm: 6);
            Equal(Code, Throws(() => s.ApplyChord(Id(), new("root-chord", "400"))).Code);
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
