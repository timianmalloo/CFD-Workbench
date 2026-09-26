using System.Reflection;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class ThicknessIntentTests
{
    private static string Id() => Guid.NewGuid().ToString("D");

    internal static void Run()
    {
        Check("Thickness_KeepCurrent_LeavesChannelBytes", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            string thickness = CurveBlock(Encoding.UTF8.GetString(original), "thickness");
            var vertex = session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3");
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.02);
            Equal(thickness, CurveBlock(Encoding.UTF8.GetString(session.Snapshot().Draft!.Bytes), "thickness"));
            var assessment = session.Validate(draft, updated.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, assessment.Thickness is null);
            session.Apply(Id(), assessment);
            Equal(thickness, CurveBlock(Encoding.UTF8.GetString(session.Snapshot().Source), "thickness"));
            Equal(ThicknessIntent.KeepCurrent, Receipt(session).Intent);
        });
        Check("Thickness_UseSource_SharedExample_FitsRootTipAndUndoRestores", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            var vertex = session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3", ThicknessIntent.UseSource);
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.02);
            var assessment = session.Validate(draft, updated.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            var proposal = assessment.Thickness ?? throw new InvalidOperationException("Missing thickness proposal.");
            byte[] drafted = session.Snapshot().Draft!.Bytes;
            double max = MaxThickness(drafted, "section-a");
            Near(max, ChannelAt(drafted, 0));
            Near(max, ChannelAt(drafted, 1));
            Equal(2, proposal.TargetEta.Count);
            Near(0, proposal.TargetEta[0]);
            Near(1, proposal.TargetEta[1]);
            foreach (double target in proposal.TargetThickness) Near(max, target);
            foreach (double residual in proposal.Residuals)
                if (Math.Abs(residual) > 1e-9) throw new InvalidOperationException("Residual " + residual);
            session.Apply(Id(), assessment);
            Equal(ThicknessIntent.UseSource, Receipt(session).Intent);
            byte[] image = session.SaveImage();
            using (var reopened = new AuthoringSession())
            {
                reopened.Reopen(image);
                Equal(ThicknessIntent.UseSource, Receipt(reopened).Intent);
            }
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Thickness_UseSource_IndependentMiddle_OnlyMiddleTargetInsideSupport", () =>
        {
            using var session = new AuthoringSession();
            session.Open(ThreeStations(), Id(), true);
            var vertex = session.ProfileAt(1).Upper.Single(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 1, SectionScope.Independent, "upper", "cv-3", ThicknessIntent.UseSource);
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.02);
            var assessment = session.Validate(draft, updated.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            var proposal = assessment.Thickness ?? throw new InvalidOperationException("Missing thickness proposal.");
            Equal(1, proposal.TargetEta.Count);
            Near(0.5, proposal.TargetEta[0]);
            byte[] drafted = session.Snapshot().Draft!.Bytes;
            string edited = session.ProfileAt(1).Name;
            double max = MaxThickness(drafted, edited);
            Near(max, proposal.TargetThickness[0]);
            Near(max, ChannelAt(drafted, proposal.TargetEta[0]));
            foreach (double residual in proposal.Residuals)
                if (Math.Abs(residual) > 1e-9) throw new InvalidOperationException("Residual " + residual);
            (double Start, double End) support = Support(drafted, proposal.TargetEta[0]);
            if (proposal.AffectedEtaStart < support.Start - 1e-9 || proposal.AffectedEtaEnd > support.End + 1e-9)
                throw new InvalidOperationException($"Affected [{proposal.AffectedEtaStart}, {proposal.AffectedEtaEnd}] outside support [{support.Start}, {support.End}].");
            if (proposal.AffectedEtaStart > proposal.AffectedEtaEnd)
                throw new InvalidOperationException("Affected span is reversed.");
        });
        Check("Thickness_UseSource_ConflictingValueLock_RefusesApply", () =>
        {
            // assume: Geometry.Assess rejects every non-root_mirror lock, so Open cannot admit a value lock.
            // The lock is written into the open draft; a false reading would be Open succeeding on that source.
            using var session = Opened();
            var vertex = session.ProfileAt(0).Upper.Single(item => item.Id == "cv-3");
            string draft = Id();
            var begun = session.BeginProfileEdit(draft, 0, SectionScope.Shared, "upper", "cv-3", ThicknessIntent.UseSource);
            ReplaceDraft(session, WithValueLock(begun.Bytes, 0.05));
            var updated = session.UpdateProfileDraft(draft, begun.Generation, vertex.X, vertex.Y + 0.02);
            var assessment = session.Validate(draft, updated.Generation);
            if (assessment.Status == GeometryStatus.Certified)
                throw new InvalidOperationException("Conflicting thickness lock was certified.");
            Equal("DSL-LOCK", assessment.Code);
            Equal(true, assessment.Diagnostics.Count > 0 && assessment.Diagnostics[0].Reason.Length > 0);
            Refuses("DSL-NOT-ASSESSED", () => session.Apply(Id(), assessment));
        });
    }

    private static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Open(FoilSourceTests.Example, Id(), true);
        return session;
    }

    private static byte[] ThreeStations()
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example).Replace(
            "at root profile \"section-a\" at tip profile \"section-a\"",
            "at root profile \"section-a\" at 50 % profile \"section-a\" at tip profile \"section-a\"");
        return Encoding.UTF8.GetBytes(text);
    }

    private static EditReceipt Receipt(AuthoringSession session) =>
        session.Envelope().Accepted.Last(row => row.Edit is not null).Edit!;

    private static void Near(double expected, double actual)
    {
        if (Math.Abs(expected - actual) > 1e-9)
            throw new InvalidOperationException($"Expected {expected}; actual {actual}");
    }

    private static string CurveBlock(string text, string name)
    {
        int at = text.IndexOf(name + " cv", StringComparison.Ordinal);
        return text[at..(text.IndexOf('}', at) + 1)];
    }

    private static byte[] WithValueLock(byte[] source, double locked)
    {
        string text = Encoding.UTF8.GetString(source);
        int sections = text.LastIndexOf("sections {", StringComparison.Ordinal);
        int close = text.IndexOf('}', sections);
        string block = "\n  locks {\n    root_mirror leading\n    root_mirror trailing\n    root_mirror dihedral\n    root_mirror twist\n    root_mirror thickness\n    value thickness at root " +
            locked.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "\n  }";
        return Encoding.UTF8.GetBytes(text.Insert(close + 1, block));
    }

    private static void ReplaceDraft(AuthoringSession session, byte[] bytes)
    {
        var field = typeof(AuthoringSession).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AuthoringSession.draft is missing.");
        var current = (SessionDraft)field.GetValue(session)!;
        field.SetValue(session, current with { Bytes = bytes });
    }

    private static double MaxThickness(byte[] source, string profile)
    {
        var found = FoilSource.Parse(source).Definition!.Profiles.First(item => item.Name == profile);
        var watch = new ProofBudget();
        var upper = Bernstein.Spans(found.Upper, watch);
        var lower = Bernstein.Spans(found.Lower, watch);
        var difference = new Rational[upper.Length][];
        for (int span = 0; span < upper.Length; span++)
        {
            difference[span] = new Rational[upper[span].Y.Length];
            for (int index = 0; index < difference[span].Length; index++)
                difference[span][index] = upper[span].Y[index] - lower[span].Y[index];
        }
        var maximum = Bernstein.Maximum(difference, watch);
        return (maximum.Lower.Down() + maximum.Upper.Up()) / 2;
    }

    private static double ChannelAt(byte[] source, double eta)
    {
        var curve = FoilSource.Parse(source).Definition!.Curves["thickness"];
        double[] abscissae = curve.Points.Select(point => point[0]).ToArray();
        double[] ordinates = curve.Points.Select(point => point[1]).ToArray();
        double parameter = Parameter(curve.Knots, curve.Degree, abscissae, eta);
        double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, parameter);
        double sum = 0;
        for (int index = 0; index < ordinates.Length; index++) sum += basis[index] * ordinates[index];
        return sum;
    }

    private static (double Start, double End) Support(byte[] source, double eta)
    {
        var curve = FoilSource.Parse(source).Definition!.Curves["thickness"];
        double[] abscissae = curve.Points.Select(point => point[0]).ToArray();
        double parameter = Parameter(curve.Knots, curve.Degree, abscissae, eta);
        double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, parameter);
        int first = -1, last = -1;
        for (int index = 0; index < basis.Length; index++)
        {
            if (basis[index] == 0) continue;
            if (first < 0) first = index;
            last = index;
        }
        double start = Abscissa(curve.Knots, curve.Degree, abscissae, curve.Knots[first]);
        double end = Abscissa(curve.Knots, curve.Degree, abscissae, curve.Knots[last + curve.Degree + 1]);
        return (start, end);
    }

    private static double Parameter(double[] knots, int degree, double[] abscissae, double eta)
    {
        if (eta <= 0) return 0;
        if (eta >= 1) return 1;
        double low = 0, high = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (low + high) / 2;
            if (Abscissa(knots, degree, abscissae, mid) < eta) low = mid;
            else high = mid;
        }
        return (low + high) / 2;
    }

    private static double Abscissa(double[] knots, int degree, double[] abscissae, double parameter)
    {
        double[] basis = SplineBasis.Values(knots, degree, parameter);
        double sum = 0;
        for (int index = 0; index < abscissae.Length; index++) sum += basis[index] * abscissae[index];
        return sum;
    }
}
