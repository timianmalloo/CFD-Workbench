using System.Diagnostics;
using System.Globalization;
using System.Text;
using CfdWorkbench.Core;

namespace CfdWorkbench.Core.Tests;

internal static class SectionEditsTests
{
    internal static void Run()
    {
        IdentityTests.Check(nameof(SectionEdits_HorizontalKind_HandlesLevelExactly), SectionEdits_HorizontalKind_HandlesLevelExactly);
        IdentityTests.Check(nameof(SectionEdits_SymmetricKind_EqualHandleLengths), SectionEdits_SymmetricKind_EqualHandleLengths);
        IdentityTests.Check(nameof(SectionEdits_AngleKind_HandlesOnRaysWithinTau), SectionEdits_AngleKind_HandlesOnRaysWithinTau);
        IdentityTests.Check(nameof(SectionEdits_InsertAnchorKeepShape_ShapeExactCornerAnchor), SectionEdits_InsertAnchorKeepShape_ShapeExactCornerAnchor);
        IdentityTests.Check(nameof(SectionEdits_ControlToAnchorBetweenTwoAnchors_OutsideUnchanged), SectionEdits_ControlToAnchorBetweenTwoAnchors_OutsideUnchanged);
        IdentityTests.Check(nameof(SectionEdits_DeleteBelowSevenPoints_Refused), SectionEdits_DeleteBelowSevenPoints_Refused);
        IdentityTests.Check(nameof(SectionEdits_FairWithAnchorRows_AnchorsKeptRowsHold), SectionEdits_FairWithAnchorRows_AnchorsKeptRowsHold);
        IdentityTests.Check(nameof(SectionEdits_FirstRow_HeaderBecomes41), SectionEdits_FirstRow_HeaderBecomes41);
        IdentityTests.Check(nameof(SectionEdits_AnchorPast32Points_RefusedNamingCeiling), SectionEdits_AnchorPast32Points_RefusedNamingCeiling);
        IdentityTests.Check(nameof(SectionEdits_TypeAndTangentSteps_ReportCountsAndMeasuredMaxChange), SectionEdits_TypeAndTangentSteps_ReportCountsAndMeasuredMaxChange);
        IdentityTests.Check(nameof(SectionEdits_PairedAnchor_BothSurfacesSameKnotsExact), SectionEdits_PairedAnchor_BothSurfacesSameKnotsExact);
        IdentityTests.Check(nameof(SectionEdits_PairedXMove_BothSurfacesSameAbscissa), SectionEdits_PairedXMove_BothSurfacesSameAbscissa);
        IdentityTests.Check(nameof(SectionEdits_PairedAnchorToControl_RefitLocalWithin10Um), SectionEdits_PairedAnchorToControl_RefitLocalWithin10Um);
        IdentityTests.Check(nameof(SectionEdits_PairedRefitSharedProfile_LargestChordSetsLimit), SectionEdits_PairedRefitSharedProfile_LargestChordSetsLimit);
        IdentityTests.Check(nameof(SectionEdits_PairedAnchorToControlRefitOverLimit_Refused), SectionEdits_PairedAnchorToControlRefitOverLimit_Refused);
        IdentityTests.Check(nameof(SectionEdits_RandomSectionsPaired_KnotsEqualAndLocalityHold), SectionEdits_RandomSectionsPaired_KnotsEqualAndLocalityHold);
        IdentityTests.Check(nameof(SectionEdits_PairedSetTangent_BothSurfacesSameKind), SectionEdits_PairedSetTangent_BothSurfacesSameKind);
        IdentityTests.Check(nameof(SectionEdits_PairedAngle_HandlesKeepPairedX_Certifies), SectionEdits_PairedAngle_HandlesKeepPairedX_Certifies);
        IdentityTests.Check(nameof(SectionEdits_PairedToAnchorWithKind_PartnerRowWritten), SectionEdits_PairedToAnchorWithKind_PartnerRowWritten);
        IdentityTests.Check(nameof(SectionEdits_PairedToControl_PartnerRowRemoved), SectionEdits_PairedToControl_PartnerRowRemoved);
        IdentityTests.Check(nameof(SectionEdits_TwoProfiles_ControlToAnchor_Observed), SectionEdits_TwoProfiles_ControlToAnchor_Observed);
        IdentityTests.Check(nameof(SectionEdits_UniqueProfile_AbscissaBreakingStepRefusedNothingChanged), SectionEdits_UniqueProfile_AbscissaBreakingStepRefusedNothingChanged);
        IdentityTests.Check(nameof(SectionEdits_UniqueProfile_YOnlyMoveAllowed), SectionEdits_UniqueProfile_YOnlyMoveAllowed);
        IdentityTests.Check(nameof(SectionEdits_SharedImport0012_LandsAsSharedReplaceCertified), SectionEdits_SharedImport0012_LandsAsSharedReplaceCertified);
        IdentityTests.Check(nameof(SectionEdits_UniqueImportOverLimit_RefusedNothingChanged), SectionEdits_UniqueImportOverLimit_RefusedNothingChanged);
    }

    // COPY-209 (Ruling 71): the Example's Root after Make unique; Tip keeps the shared profile.
    private const string UniqueRootReason = "This edit would give Root's section different point positions from Tip's, and the wing " +
        "between them can't be checked then. Move points up or down only, or keep the section shared.";

    internal static void RunReadiness()
    {
        var watch = Stopwatch.StartNew();
        _ = SectionEdits.Apply(SectionPointTests.Identified(), 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true));
        watch.Stop();
        Console.WriteLine("READINESS section_edit_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
    }

    private static void SectionEdits_HorizontalKind_HandlesLevelExactly()
    {
        var profile = After(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Horizontal, null, null), Anchor());
        var upper = profile.Upper;
        int index = Array.IndexOf(upper.Ids, "cv-4");
        IdentityTests.Equal(upper.Points[index][1], upper.Points[index - 1][1]);
        IdentityTests.Equal(upper.Points[index][1], upper.Points[index + 1][1]);
    }

    private static void SectionEdits_SymmetricKind_EqualHandleLengths()
    {
        var profile = After(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Symmetric, null, null), Anchor());
        var upper = profile.Upper;
        int index = Array.IndexOf(upper.Ids, "cv-4");
        double left = Distance(upper.Points[index], upper.Points[index - 1]);
        double right = Distance(upper.Points[index], upper.Points[index + 1]);
        SectionPointTests.Near(left, right, 1e-12);
        SectionPointTests.Near(upper.Points[index][0], (upper.Points[index - 1][0] + upper.Points[index + 1][0]) / 2, 1e-12);
        SectionPointTests.Near(upper.Points[index][1], (upper.Points[index - 1][1] + upper.Points[index + 1][1]) / 2, 1e-12);
    }

    private static void SectionEdits_AngleKind_HandlesOnRaysWithinTau()
    {
        var profile = After(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 20, null), Anchor());
        var upper = profile.Upper;
        int index = Array.IndexOf(upper.Ids, "cv-4");
        IdentityTests.Equal(true, OnRay(upper.Points[index], upper.Points[index + 1], 20));
        IdentityTests.Equal(true, OnRay(upper.Points[index], upper.Points[index - 1], 200));
    }

    private static void SectionEdits_InsertAnchorKeepShape_ShapeExactCornerAnchor()
    {
        byte[] before = SectionPointTests.Identified();
        var (after, _) = SectionEdits.Apply(before, 0, new SectionStep.InsertAnchor(SurfaceSide.Upper, 0.4));
        var prior = ProfileOf(before);
        var next = ProfileOf(after);
        IdentityTests.Equal(true, next.Upper.Knots.SequenceEqual(next.Lower.Knots));
        IdentityTests.Equal(true, FoilSource.MaxOrdinateDeviation(prior, next) < 1e-12);
        IdentityTests.Equal(0, next.Upper.Tangents.Length);
        IdentityTests.Equal(true, Enumerable.Range(0, next.Upper.Points.Length).Any(index =>
            FoilSource.IsAnchor(next.Upper.Knots, next.Upper.Points.Length, next.Upper.Degree, index)));
    }

    private static void SectionEdits_ControlToAnchorBetweenTwoAnchors_OutsideUnchanged()
    {
        byte[] before = TwoAnchorFoil();
        var prior = ProfileOf(before);
        var (after, _) = SectionEdits.Apply(before, 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-8", true));
        var next = ProfileOf(after);
        double left = prior.Upper.Points[5][0];
        double right = prior.Upper.Points[11][0];
        for (int sample = 0; sample <= 20; sample++)
        {
            double x = left * sample / 20.0;
            SectionPointTests.Near(ProfileEvaluator.OrdinateAt(prior.Upper, x), ProfileEvaluator.OrdinateAt(next.Upper, x), 1e-9);
            x = right + (1 - right) * sample / 20.0;
            SectionPointTests.Near(ProfileEvaluator.OrdinateAt(prior.Upper, x), ProfileEvaluator.OrdinateAt(next.Upper, x), 1e-9);
        }
        IdentityTests.Equal(true, LowerDeviation(prior, next) < 1e-9);
    }

    private static void SectionEdits_DeleteBelowSevenPoints_Refused()
    {
        byte[] foil = CountFoil(7);
        var error = Throws(() => SectionEdits.Apply(foil, 0, new SectionStep.Delete(SurfaceSide.Upper, "cv-3")));
        IdentityTests.Equal("DSL-CURVE", error.Code);
        IdentityTests.Equal(true, error.Message.Contains("p + 2 = 7", StringComparison.Ordinal));
    }

    private static void SectionEdits_FairWithAnchorRows_AnchorsKeptRowsHold()
    {
        byte[] anchored = AfterBytes(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Horizontal, null, null), Anchor());
        var before = ProfileOf(anchored);
        int index = Array.IndexOf(before.Upper.Ids, "cv-4");
        double anchorY = before.Upper.Points[index][1];
        byte[] wavy = SectionEdits.Apply(anchored, 0, new SectionStep.Move(SurfaceSide.Upper, "cv-2", before.Upper.Points[2][0], before.Upper.Points[2][1] + 0.02)).Bytes;
        byte[] faired = SectionEdits.Apply(wavy, 0, new SectionStep.Fair(SurfaceSide.Upper, 0.05, PreserveEnds.Position)).Bytes;
        var after = ProfileOf(faired);
        int kept = Array.IndexOf(after.Upper.Ids, "cv-4");
        IdentityTests.Equal(anchorY, after.Upper.Points[kept][1]);
        IdentityTests.Equal(after.Upper.Points[kept][1], after.Upper.Points[kept - 1][1]);
        IdentityTests.Equal(after.Upper.Points[kept][1], after.Upper.Points[kept + 1][1]);
    }

    private static void SectionEdits_FirstRow_HeaderBecomes41()
    {
        byte[] edited = Anchor();
        IdentityTests.Equal(true, Encoding.UTF8.GetString(edited).Contains("foildsl \"4.1\"", StringComparison.Ordinal));
    }

    private static void SectionEdits_AnchorPast32Points_RefusedNamingCeiling()
    {
        byte[] foil = SectionPointTests.Identified();
        var profile = ProfileOf(foil);
        for (int count = profile.Upper.Points.Length; count < 28; count++)
            foil = FoilSource.InsertProfileKnot(foil, profile.Name, 0.2 + 0.01 * (count - 8)).Source;
        var error = Throws(() => SectionEdits.Apply(foil, 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)));
        IdentityTests.Equal("DSL-CURVE", error.Code);
        IdentityTests.Equal(true, error.Message.Contains("32", StringComparison.Ordinal));
    }

    private static void SectionEdits_TypeAndTangentSteps_ReportCountsAndMeasuredMaxChange()
    {
        byte[] before = SectionPointTests.Identified();
        var (typed, typeReport) = SectionEdits.Apply(before, 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true));
        var next = ProfileOf(typed);
        IdentityTests.Equal("set-type", typeReport.Kind);
        IdentityTests.Equal(13, typeReport.UpperPoints);
        IdentityTests.Equal(13, typeReport.LowerPoints);
        IdentityTests.Equal(next.Upper.Points.Length, typeReport.UpperPoints);
        IdentityTests.Equal("FoilSource.MaxOrdinateDeviation", typeReport.MaxChangeOracle);
        SectionPointTests.Near(FoilSource.MaxOrdinateDeviation(ProfileOf(before), next), typeReport.MaxChange, 1e-15);
        IdentityTests.Equal(true, typeReport.MaxChange > 0);
        var (_, tangentReport) = SectionEdits.Apply(typed, 0, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Horizontal, null, null));
        IdentityTests.Equal("set-tangent", tangentReport.Kind);
        IdentityTests.Equal("FoilSource.MaxOrdinateDeviation", tangentReport.MaxChangeOracle);
        IdentityTests.Equal(true, tangentReport.MaxChange >= 0);
    }

    private static void SectionEdits_PairedAnchor_BothSurfacesSameKnotsExact()
    {
        var profile = ProfileOf(Anchor());
        IdentityTests.Equal(true, profile.Upper.Knots.SequenceEqual(profile.Lower.Knots));
        IdentityTests.Equal(13, profile.Upper.Points.Length);
        IdentityTests.Equal(13, profile.Lower.Points.Length);
    }

    private static void SectionEdits_PairedXMove_BothSurfacesSameAbscissa()
    {
        byte[] before = SectionPointTests.Identified();
        double x = ProfileOf(before).Upper.Points[3][0];
        var profile = After(new SectionStep.Move(SurfaceSide.Upper, "cv-3", x + 0.01, 0.07), before);
        IdentityTests.Equal(profile.Upper.Points[3][0], profile.Lower.Points[3][0]);
        IdentityTests.Equal(0.07, profile.Upper.Points[3][1]);
        IdentityTests.Equal(ProfileOf(before).Lower.Points[3][1], profile.Lower.Points[3][1]);
    }

    private static void SectionEdits_PairedAnchorToControl_RefitLocalWithin10Um()
    {
        byte[] anchored = SectionEdits.Apply(SectionPointTests.Identified(), 0, new SectionStep.InsertAnchor(SurfaceSide.Upper, 0.42)).Bytes;
        var before = ProfileOf(anchored);
        int anchor = Enumerable.Range(0, before.Upper.Points.Length).First(index =>
            FoilSource.IsAnchor(before.Upper.Knots, before.Upper.Points.Length, 5, index));
        byte[] edited = SectionEdits.Apply(anchored, 0, new SectionStep.SetType(SurfaceSide.Upper, before.Upper.Ids[anchor], false)).Bytes;
        var after = ProfileOf(edited);
        IdentityTests.Equal(true, after.Upper.Knots.SequenceEqual(after.Lower.Knots));
        double chord = Placement.Frame(edited, 0).ChordMeters;
        double change = FoilSource.MaxOrdinateDeviation(before, after) * chord * 1e6;
        IdentityTests.Equal(true, change <= 10);
    }

    private static void SectionEdits_PairedRefitSharedProfile_LargestChordSetsLimit()
    {
        byte[] kinked = KinkedLower();
        byte[] small = Trailing(kinked, "0.001");
        var accepted = Control(small);
        IdentityTests.Equal(true, accepted.Length > 0);
        byte[] mixed = Trailing(kinked, "0.001", "10000");
        var refused = Throws(() => Control(mixed));
        IdentityTests.Equal(true, Microns(refused.Message) > 10);
    }

    private static void SectionEdits_PairedAnchorToControlRefitOverLimit_Refused()
    {
        var error = Throws(() => Control(KinkedLower()));
        IdentityTests.Equal("DSL-CURVE", error.Code);
        IdentityTests.Equal(true, Microns(error.Message) > 10);
        IdentityTests.Equal(true, error.Data["RefitMaximumChordX"] is double x &&
            double.IsFinite(x) && x > 0 && x < 1);
        IdentityTests.Equal(true, error.Data["RefitDeviationMeters"] is double deviation && deviation > 10e-6);
        IdentityTests.Equal(10e-6, error.Data["RefitLimitMeters"]);
    }

    private static void SectionEdits_RandomSectionsPaired_KnotsEqualAndLocalityHold()
    {
        var random = new Random(12345);
        byte[] bytes = SectionPointTests.Identified();
        for (int turn = 0; turn < 3; turn++)
        {
            var profile = ProfileOf(bytes);
            int index = 2 + random.Next(profile.Upper.Points.Length - 4);
            double x = profile.Upper.Points[index][0];
            double next = profile.Upper.Points[index + 1][0];
            double previous = profile.Upper.Points[index - 1][0];
            double moved = previous + (next - previous) * (0.3 + 0.4 * random.NextDouble());
            if (moved == x) moved = Math.Min(next - 1e-4, x + 1e-3);
            bytes = SectionEdits.Apply(bytes, 0, new SectionStep.Move(SurfaceSide.Upper, profile.Upper.Ids[index], moved, profile.Upper.Points[index][1])).Bytes;
            var after = ProfileOf(bytes);
            IdentityTests.Equal(true, after.Upper.Knots.SequenceEqual(after.Lower.Knots));
            for (int point = 0; point < after.Upper.Points.Length; point++)
                IdentityTests.Equal(after.Upper.Points[point][0], after.Lower.Points[point][0]);
        }
        var ends = ProfileOf(bytes);
        bytes = SectionEdits.Apply(bytes, 0, new SectionStep.SetType(SurfaceSide.Upper, ends.Upper.Ids[4], true)).Bytes;
        var anchored = ProfileOf(bytes);
        IdentityTests.Equal(true, anchored.Upper.Knots.SequenceEqual(anchored.Lower.Knots));
        SectionPointTests.Near(ProfileEvaluator.OrdinateAt(ends.Upper, 0), ProfileEvaluator.OrdinateAt(anchored.Upper, 0), 1e-12);
        SectionPointTests.Near(ProfileEvaluator.OrdinateAt(ends.Upper, 1), ProfileEvaluator.OrdinateAt(anchored.Upper, 1), 1e-12);
        SectionPointTests.Near(0, LowerDeviation(ends, anchored), 1e-9);
    }

    private static void SectionEdits_PairedSetTangent_BothSurfacesSameKind()
    {
        var angled = ProfileOf(AfterBytes(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 20, null), Anchor()));
        SameKindOnBoth(angled, "angle", 20);
        byte[] anchored = Anchor();
        var anchoredProfile = ProfileOf(anchored);
        string lowerId = anchoredProfile.Lower.Ids[Array.IndexOf(anchoredProfile.Upper.Ids, "cv-4")];
        var level = ProfileOf(AfterBytes(new SectionStep.SetTangent(SurfaceSide.Lower, lowerId, TangentKind.Horizontal, null, null), anchored));
        SameKindOnBoth(level, "horizontal", null);
    }

    // F-XPA-1 (Ruling 73 DR-XPA-6 a): Angle is built y-only, so a paired point keeps one x on both surfaces.
    private static void SectionEdits_PairedAngle_HandlesKeepPairedX_Certifies()
    {
        // Lift the lower right handle in y only, so the two surfaces' handle distances differ and an x-moving build shows.
        byte[] anchored = Anchor();
        var before = ProfileOf(anchored);
        int at = Array.IndexOf(before.Upper.Ids, "cv-4");
        var lowerHandle = before.Lower.Points[at + 1];
        byte[] lifted = AfterBytes(new SectionStep.Move(SurfaceSide.Lower, before.Lower.Ids[at + 1], lowerHandle[0], lowerHandle[1] - 0.03), anchored);
        byte[] bytes = AfterBytes(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 20, null), lifted);
        var profile = ProfileOf(bytes);
        int index = Array.IndexOf(profile.Upper.Ids, "cv-4");
        foreach (int handle in new[] { index - 1, index, index + 1 })
            IdentityTests.Equal(profile.Upper.Points[handle][0], profile.Lower.Points[handle][0]);
        IdentityTests.Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(bytes)).Status);
    }

    private static void SectionEdits_PairedToAnchorWithKind_PartnerRowWritten()
    {
        SameKindOnBoth(ProfileOf(Anchor()), "smooth", null);
        var corner = ProfileOf(AfterBytes(new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Corner, null, null), Anchor()));
        IdentityTests.Equal(0, corner.Upper.Tangents.Length);
        IdentityTests.Equal(0, corner.Lower.Tangents.Length);
    }

    private static void SectionEdits_PairedToControl_PartnerRowRemoved()
    {
        byte[] anchored = Anchor();
        var profile = ProfileOf(anchored);
        byte[] control = SectionEdits.Apply(anchored, 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", false)).Bytes;
        var after = ProfileOf(control);
        IdentityTests.Equal(0, after.Upper.Tangents.Length);
        IdentityTests.Equal(0, after.Lower.Tangents.Length);
        IdentityTests.Equal(profile.Upper.Points.Length - 2, after.Lower.Points.Length);
    }

    private static void SameKindOnBoth(ProfileDefinition profile, string kind, double? angle)
    {
        int index = profile.Upper.Tangents.Select(row => Array.IndexOf(profile.Upper.Ids, row.Id)).Single();
        var upper = profile.Upper.Tangents.Single(row => row.Id == profile.Upper.Ids[index]);
        var lower = profile.Lower.Tangents.Single(row => row.Id == profile.Lower.Ids[index]);
        IdentityTests.Equal(kind, upper.Kind);
        IdentityTests.Equal(kind, lower.Kind);
        IdentityTests.Equal(angle, upper.Angle);
        IdentityTests.Equal(angle, lower.Angle);
    }

    private static void SectionEdits_TwoProfiles_ControlToAnchor_Observed()
    {
        string text = Encoding.UTF8.GetString(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example)));
        int start = text.IndexOf("    profile \"section-a\" {", StringComparison.Ordinal);
        int end = text.IndexOf("    }", start, StringComparison.Ordinal);
        string block = text.Substring(start, end + 6 - start);
        string copy = block.Replace("profile \"section-a\"", "profile \"section-b\"", StringComparison.Ordinal)
            .Replace("(0.55, 0.05)", "(0.55, 0.051)", StringComparison.Ordinal);
        text = text.Insert(end + 6, "\n" + copy);
        text = text.Replace("at tip profile \"section-a\"", "at tip profile \"section-b\"", StringComparison.Ordinal);
        byte[] foil = Encoding.UTF8.GetBytes(text);
        var before = Geometry.Assess(FoilSource.Parse(foil));
        if (before.Status != GeometryStatus.Certified)
            throw new InvalidOperationException("Fixture is not certified: " + before.Status + " " + before.Code + " " + before.Reason);
        byte[] edited = SectionEdits.Apply(foil, 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;
        var after = Geometry.Assess(FoilSource.Parse(edited));
        IdentityTests.Equal(GeometryStatus.Unsupported, after.Status);
        IdentityTests.Equal(true, after.Reason.Contains("abscissa", StringComparison.OrdinalIgnoreCase));
    }

    // Ruling 71: a step that would give a unique profile other abscissae than its neighbour's is refused at the step, with
    // nothing changed; before, it landed and the certificate refused the whole draft at Finish.
    private static void SectionEdits_UniqueProfile_AbscissaBreakingStepRefusedNothingChanged()
    {
        using var session = SectionDraftTests.Opened();
        string id = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(id, 0);
        view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        var (x, y) = SectionDraftTests.Point(view, SurfaceSide.Upper, "cv-3");
        var (nextX, _) = SectionDraftTests.Point(view, SurfaceSide.Upper, "cv-4");
        SectionStep[] breaking =
        [
            new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true),
            new SectionStep.Insert(SurfaceSide.Upper, 0.37),
            new SectionStep.Move(SurfaceSide.Upper, "cv-3", (x + nextX) / 2, y),
        ];
        foreach (var step in breaking)
        {
            var error = Throws(() => session.ApplySectionStep(id, view.Generation, step));
            IdentityTests.Equal("DSL-GEOMETRY", error.Code);
            IdentityTests.Equal(UniqueRootReason, error.Reason);
            var draft = session.Snapshot().Draft!;
            IdentityTests.Equal(view.Generation, draft.Generation);
            IdentityTests.Equal(true, draft.Bytes.AsSpan().SequenceEqual(view.Bytes));
        }
        var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
        IdentityTests.Equal(GeometryStatus.Certified, assessment.Status);
    }

    // The useful part of a unique section: a y-only move keeps the abscissae, lands, and the draft still certifies.
    private static void SectionEdits_UniqueProfile_YOnlyMoveAllowed()
    {
        using var session = SectionDraftTests.Opened();
        string id = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(id, 0);
        view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        view = session.ApplySectionStep(id, view.Generation, SectionDraftTests.Raise(view, "cv-3", 0.002));
        IdentityTests.Equal(2, view.Cursor);
        IdentityTests.Equal("section-a-i1", view.Profile);
        var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
        IdentityTests.Equal(GeometryStatus.Certified, assessment.Status);
    }

    // DR-M12D-3 a (was SectionEdits_UniqueProfile_ImportWithOwnSpacingRefusedNothingChanged): Import is a Replace in place at
    // the draft's scope. NACA 0012 at the Example's shared Root replaces Root and Tip together, fits the current spacing
    // within 10 µm (P-CF-0012), lands and certifies; nothing is refused and no profile is added.
    private static void SectionEdits_SharedImport0012_LandsAsSharedReplaceCertified()
    {
        using var session = SectionDraftTests.Opened();
        string id = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(id, 0);
        view = session.ApplySectionStep(id, view.Generation, new SectionStep.Import(SectionDraftTests.Naca0012Selig()));
        IdentityTests.Equal("current", view.Last!.Import!.Basis);
        IdentityTests.Equal(1, FoilSource.Parse(view.Bytes).Definition!.Profiles.Length);
        IdentityTests.Equal(GeometryStatus.Certified, session.AssessSection(id, view.Generation, CancellationToken.None).Status);
    }

    // The own-spacing case that the retired COPY-210 refused is now a unique Root beside Tip, fitted on the shared spacing only: a cambered
    // file over 10 µm there is refused with CAT-SPACING (COPY-191) and nothing changes.
    private static void SectionEdits_UniqueImportOverLimit_RefusedNothingChanged()
    {
        using var session = SectionDraftTests.Opened();
        string id = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(id, 0);
        view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        var error = Throws(() => session.ApplySectionStep(id, view.Generation, new SectionStep.Import(Naca4412Selig())));
        IdentityTests.Equal("CAT-SPACING", error.Code);
        IdentityTests.Equal(true, error.Reason!.StartsWith("Root blends point-to-point with Tip", StringComparison.Ordinal));
        var draft = session.Snapshot().Draft!;
        IdentityTests.Equal(view.Generation, draft.Generation);
        IdentityTests.Equal(true, draft.Bytes.AsSpan().SequenceEqual(view.Bytes));
    }

    private static byte[] Naca4412Selig()
    {
        const int n = 40;
        var text = new StringBuilder("NACA 4412\n");
        static double X(int i) => 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
        static double Half(double x) => 0.6 * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * Math.Pow(x, 3) - 0.1036 * Math.Pow(x, 4));
        static double Camber(double x) => x < 0.4 ? 0.04 / 0.16 * (0.8 * x - x * x) : 0.04 / 0.36 * (0.2 + 0.8 * x - x * x);
        for (int i = n; i >= 0; i--) text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", X(i), Camber(X(i)) + Half(X(i))));
        for (int i = 1; i <= n; i++) text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", X(i), Camber(X(i)) - Half(X(i))));
        return Encoding.UTF8.GetBytes(text.ToString());
    }

    private static byte[] Anchor() =>
        SectionEdits.Apply(SectionPointTests.Identified(), 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;

    private static byte[] AfterBytes(SectionStep step, byte[] bytes) => SectionEdits.Apply(bytes, 0, step).Bytes;

    private static ProfileDefinition After(SectionStep step, byte[] bytes) => ProfileOf(AfterBytes(step, bytes));

    private static ProfileDefinition ProfileOf(byte[] bytes)
    {
        var definition = FoilSource.Parse(bytes).Definition!;
        return definition.Profiles[definition.Assignments[0].Profile];
    }

    private static byte[] Control(byte[] bytes)
    {
        var profile = ProfileOf(bytes);
        int anchor = Enumerable.Range(0, profile.Upper.Points.Length).First(index =>
            FoilSource.IsAnchor(profile.Upper.Knots, profile.Upper.Points.Length, 5, index));
        return SectionEdits.Apply(bytes, 0, new SectionStep.SetType(SurfaceSide.Upper, profile.Upper.Ids[anchor], false)).Bytes;
    }

    private static byte[] KinkedLower()
    {
        byte[] anchored = Anchor();
        var profile = ProfileOf(anchored);
        int anchor = Array.IndexOf(profile.Upper.Ids, "cv-4");
        double left = profile.Lower.Points[anchor - 1][1];
        double right = profile.Lower.Points[anchor + 1][1];
        byte[] moved = SectionEdits.Apply(anchored, 0, new SectionStep.Move(SurfaceSide.Lower, profile.Lower.Ids[anchor - 1], profile.Lower.Points[anchor - 1][0], left - 0.08)).Bytes;
        return SectionEdits.Apply(moved, 0, new SectionStep.Move(SurfaceSide.Lower, profile.Lower.Ids[anchor + 1], profile.Lower.Points[anchor + 1][0], right + 0.08)).Bytes;
    }

    private static byte[] Trailing(byte[] bytes, string root, string? tip = null)
    {
        string text = Encoding.UTF8.GetString(bytes);
        string points = tip is null
            ? "[(0, " + root + "), (0.1, " + root + "), (0.3, " + root + "), (0.5, " + root + "), (0.7, " + root + "), (0.9, " + root + "), (1, " + root + ")]"
            : "[(0, " + root + "), (0.1, " + root + "), (0.3, 2500), (0.5, 5000), (0.7, 7500), (0.9, 9000), (1, " + tip + ")]";
        int start = text.IndexOf("trailing cv {", StringComparison.Ordinal);
        int pointsAt = text.IndexOf("points [", start, StringComparison.Ordinal);
        int close = text.IndexOf(']', pointsAt);
        text = string.Concat(text.AsSpan(0, pointsAt), "points ", points, text.AsSpan(close + 1));
        return Encoding.UTF8.GetBytes(text);
    }

    private static byte[] TwoAnchorFoil()
    {
        var xs = new[] { 0, 0, 0.08, 0.16, 0.24, 0.3, 0.36, 0.43, 0.5, 0.57, 0.64, 0.7, 0.76, 0.84, 0.92, 1, 1 };
        var upper = new (double X, double Y)[xs.Length];
        var lower = new (double X, double Y)[xs.Length];
        for (int index = 0; index < xs.Length; index++)
        {
            double y = index == 0 || index == xs.Length - 1 ? 0 : 0.02 + 0.04 * Math.Sin(Math.PI * xs[index]);
            upper[index] = (xs[index], y);
            lower[index] = (xs[index], -y);
        }
        string knots = "0, 0, 0, 0, 0, 0, 0.3, 0.3, 0.3, 0.3, 0.3, 0.5, 0.7, 0.7, 0.7, 0.7, 0.7, 1, 1, 1, 1, 1, 1";
        string ids = string.Join(", ", Enumerable.Range(0, xs.Length).Select(index => "\"cv-" + index + "\""));
        string upperText = "upper cv { degree 5 knots [" + knots + "] points [" + SectionPointTests.Points(upper) + "] ids [" + ids + "] tangents { \"cv-5\" smooth \"cv-11\" smooth } }";
        string lowerText = "lower cv { degree 5 knots [" + knots + "] points [" + SectionPointTests.Points(lower) + "] ids [" + ids + "] }";
        return Encoding.UTF8.GetBytes(SectionPointTests.ReplaceSurfaces(upperText, lowerText, true));
    }

    private static byte[] CountFoil(int count)
    {
        var points = new (double X, double Y)[count];
        for (int index = 0; index < count; index++)
        {
            double x = index == 0 ? 0 : index == count - 1 ? 1 : index / (double)(count - 1);
            if (index == 1) x = 0;
            double y = index == 0 || index == count - 1 ? 0 : (index < count / 2 ? 0.03 : -0.02);
            points[index] = (x, index == 1 ? 0.02 : y);
        }
        var knots = new List<string>();
        for (int index = 0; index < 6; index++) knots.Add("0");
        if (count > 6) knots.Add("0.5");
        for (int index = knots.Count; index < count + 6; index++) knots.Add("1");
        string ids = string.Join(", ", Enumerable.Range(0, count).Select(index => "\"cv-" + index + "\""));
        string upper = "upper cv { degree 5 knots [" + string.Join(", ", knots) + "] points [" + SectionPointTests.Points(points) + "] ids [" + ids + "] }";
        var low = points.Select(point => (point.X, -point.Y)).ToArray();
        string lower = "lower cv { degree 5 knots [" + string.Join(", ", knots) + "] points [" + SectionPointTests.Points(low) + "] ids [" + ids + "] }";
        return Encoding.UTF8.GetBytes(SectionPointTests.ReplaceSurfaces(upper, lower, false));
    }

    private static double LowerDeviation(ProfileDefinition before, ProfileDefinition after)
    {
        double worst = 0;
        for (int sample = 0; sample <= 40; sample++)
        {
            double x = sample / 40.0;
            worst = Math.Max(worst, Math.Abs(ProfileEvaluator.OrdinateAt(before.Lower, x) - ProfileEvaluator.OrdinateAt(after.Lower, x)));
        }
        return worst;
    }

    private static double Distance(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));

    private static bool OnRay(double[] origin, double[] handle, double degrees)
    {
        double radians = degrees * Math.PI / 180;
        double vx = handle[0] - origin[0], vy = handle[1] - origin[1];
        double cross = Math.Abs(vx * Math.Sin(radians) - vy * Math.Cos(radians));
        double dot = vx * Math.Cos(radians) + vy * Math.Sin(radians);
        return cross <= 1e-9 && dot > 0;
    }

    private static ContractError Throws(Action action)
    {
        try { action(); }
        catch (ContractError error) { return error; }
        throw new InvalidOperationException("Expected ContractError");
    }

    private static double Microns(string reason)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(reason, @"[0-9]+(?:\.[0-9]+)?");
        double best = 0;
        foreach (System.Text.RegularExpressions.Match match in matches)
            best = Math.Max(best, double.Parse(match.Value, CultureInfo.InvariantCulture));
        return best;
    }
}
