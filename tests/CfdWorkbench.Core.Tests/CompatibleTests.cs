using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Compatible fit tests (Ruling 73, Ruling 269; docs/design/compatible-fit.md §8 and
/// docs/design/cross-profile-abscissa.md §10).
/// </summary>
internal static class CompatibleTests
{
    private static readonly StationGrid FastGrid = new(17, 201);

    internal static void Run()
    {
        Check(nameof(Compatible_EqualSpacing_IdentityBitwise), Compatible_EqualSpacing_IdentityBitwise);
        Check(nameof(Compatible_NeverWritten_RecordBytesUnchanged), Compatible_NeverWritten_RecordBytesUnchanged);
        Check(nameof(Compatible_Basis_RichestThenVerticesThenStation_Deterministic), Compatible_Basis_RichestThenVerticesThenStation_Deterministic);
        Check(nameof(Placement_CompatiblePair_DisplayWithinCertifiedEnclosure), Placement_CompatiblePair_DisplayWithinCertifiedEnclosure);
        Check(nameof(SectionStep_UnderCompatible_OnlyEditedBlockChanges), SectionStep_UnderCompatible_OnlyEditedBlockChanges);
        Check(nameof(Compatible_Tip6OnRoot10_ExactRefinement), Compatible_Tip6OnRoot10_ExactRefinement);
        Check(nameof(Compatible_Section8OnNaca10_ResidualReportedBothLevels), Compatible_Section8OnNaca10_ResidualReportedBothLevels);
        Check(nameof(Compatible_TipAnchor_InsertedIntoSharedSpacing), Compatible_TipAnchor_InsertedIntoSharedSpacing);
        Check(nameof(SectionStep_CopyOverLimit_RefusedUnchanged), SectionStep_CopyOverLimit_RefusedUnchanged);
        Check(nameof(SectionStep_PastBlendCapacity_RefusedEarly), SectionStep_PastBlendCapacity_RefusedEarly);
        Check(nameof(Compatible_DegenerateFit_NotAssessedWithReason), Compatible_DegenerateFit_NotAssessedWithReason);
        Check(nameof(SectionEdits_PairedSteps_ResultCertifies), SectionEdits_PairedSteps_ResultCertifies);
        Check(nameof(SectionEdits_SetTangentAngle_KeepsXOnBothSurfaces), SectionEdits_SetTangentAngle_KeepsXOnBothSurfaces);
        Check(nameof(SectionDraft_CompatibleStep_FinishOneRow_ReopenSameMatch), SectionDraft_CompatibleStep_FinishOneRow_ReopenSameMatch);
        Check(nameof(ImportOnUnique_OwnSpacingAllowed_MatchedOrRefused), ImportOnUnique_OwnSpacingAllowed_MatchedOrRefused);
        Check(nameof(RebuildOnBasis_CopiesRefitAndReported), RebuildOnBasis_CopiesRefitAndReported);
        Check(nameof(Compatible_AuthoredMode_SingleCaller), Compatible_AuthoredMode_SingleCaller);
        Check(nameof(Placement_SingleEntry_NoAuthoredBlendReader), Placement_SingleEntry_NoAuthoredBlendReader);
        Check(nameof(Compatible_SurfaceResidual_SameMeasureAsStations), Compatible_SurfaceResidual_SameMeasureAsStations);
        Check(nameof(Compatible_MultiStation_TwoNonIdentityCopies_AttributedAndIdentityZero), Compatible_MultiStation_TwoNonIdentityCopies_AttributedAndIdentityZero);
        Check(nameof(Compatible_AuthoredTangentCheckedBeforeRefit_AndCurvesSynced), Compatible_AuthoredTangentCheckedBeforeRefit_AndCurvesSynced);
        Check(nameof(Compatible_FixtureHash_SameOnMacAndWindows), Compatible_FixtureHash_SameOnMacAndWindows);
        // CF-3 evaluator id & migration (BC-5, DR-DF-9 a)
        Check(nameof(Evaluator_IdFor_V3OnlyWhenSpacingDiffers), Evaluator_IdFor_V3OnlyWhenSpacingDiffers);
        Check(nameof(Evaluator_OldRowsNeverRewritten), Evaluator_OldRowsNeverRewritten);
        Check(nameof(Evaluator_V2RowWithMixedSpacing_Refused), Evaluator_V2RowWithMixedSpacing_Refused);
        Check(nameof(Evaluator_RemoveBackToOneSpacing_V2Again), Evaluator_RemoveBackToOneSpacing_V2Again);
        // CF-4 R3 station commands (C3, C4)
        Check(nameof(Promote_R3_OnCopies_RegimeMeasured), Promote_R3_OnCopies_RegimeMeasured);
        Check(nameof(Remove_CopyChanges_MeasuresTouchedIntervals), Remove_CopyChanges_MeasuresTouchedIntervals);
        Check(nameof(Promote_SharedSpacingInMixedDoc_KeepsLSpacing), Promote_SharedSpacingInMixedDoc_KeepsLSpacing);
    }

    internal static void RunReadiness()
    {
        Check("Readiness_CompatibleCheck_FiveSectionsUnder1s", Readiness_CompatibleCheck_FiveSectionsUnder1s);
    }

    // ---- Fixtures ---------------------------------------------------------------------------------------------------

    internal static byte[] LP1Fixture()
    {
        // Root: 10-point NACA 0012 (5 spans). Tip: section-a fitted onto 6-point sqrt basis (1 span).
        byte[] unique = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 0).Source;
        var def = FoilSource.Parse(unique).Definition!;
        string tipName = def.Profiles[def.Assignments[1].Profile].Name;
        var sectionA = FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example))).Definition!.Profiles[0];
        var (k6, x6) = DatImport.OwnSqrtBasis(6);
        var fit = DatImport.FitToBasis(SampleDat(sectionA), k6, x6, 5, true)!;
        return FoilSource.WriteSurfaces(unique, tipName, k6, Points(x6, fit.Upper), Ids(6), Points(x6, fit.Lower), Ids(6));
    }

    internal static byte[] LP2Fixture()
    {
        // Root: Example section-a (8 points, 3 spans). Tip: NACA 0012 (10 points, 5 spans) — Tip leads, Root fitted.
        byte[] unique = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 0).Source;
        var def = FoilSource.Parse(unique).Definition!;
        string rootName = def.Profiles[def.Assignments[0].Profile].Name;
        var sectionA = FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example))).Definition!.Profiles[0];
        return FoilSource.WriteSurfaces(unique, rootName, sectionA.Upper.Knots, Copy(sectionA.Upper.Points),
            Ids(sectionA.Upper.Points.Length), Copy(sectionA.Lower.Points), Ids(sectionA.Lower.Points.Length));
    }

    internal static byte[] LP4Fixture()
    {
        // LP1 plus an interior anchor at x ≈ 0.3 on Tip6.
        byte[] lp1 = LP1Fixture();
        var def = FoilSource.Parse(lp1).Definition!;
        var tip = def.Profiles[def.Assignments[1].Profile];
        int va = Enumerable.Range(2, tip.Upper.Points.Length - 4)
            .OrderBy(i => Math.Abs(tip.Upper.Points[i][0] - 0.3)).First();
        return SectionEdits.Apply(lp1, 1, new SectionStep.SetType(SurfaceSide.Upper, tip.Upper.Ids[va], true)).Bytes;
    }

    private static byte[] OverLimitPairFixture()
    {
        // Root: 6-point sqrt basis (1 span) with a sharp mid-chord bump via higher point count? No:
        // Basis is Tip (10-point NACA 0012). Root is 8-point section with a sharp bump on a custom non-nesting spacing,
        // or Root has 6 points (1 span) and Tip has 6 points (1 span) with a very different x(t) so Root leads (tie -> station 0)
        // and Tip's copy on Root's 1-span basis exceeds 10 um.
        byte[] baseDoc = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 0).Source;
        var def = FoilSource.Parse(baseDoc).Definition!;
        string rootName = def.Profiles[def.Assignments[0].Profile].Name;
        string tipName = def.Profiles[def.Assignments[1].Profile].Name;
        // Root: 6-point 1-span spacing (0, 0, 0.25, 0.5, 0.75, 1) -> 1 span, 6 vertices.
        // Tip: 6-point 1-span spacing with shifted x (0, 0, 0.05, 0.15, 0.85, 1) and strong camber/bump -> tie (1 span, 6 vertices), Root (station 0) leads.
        // Even better: scale trailing chord to 1 m so residual is large (>> 14 um).
        double[] k6 = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1];
        double[] rx = [0, 0, 0.25, 0.50, 0.75, 1];
        double[] ry = [0, 0.025, 0.06, 0.055, 0.025, 0];
        double[] tx = [0, 0, 0.04, 0.12, 0.90, 1];
        double[] tuy = [0, 0.05, 0.12, 0.02, 0.08, 0];
        double[] tly = [0, -0.02, -0.01, -0.09, -0.01, 0];
        byte[] doc = FoilSource.WriteSurfaces(baseDoc, rootName, k6, Points(rx, ry), Ids(6), Points(rx, ry.Select(v => -v).ToArray()), Ids(6));
        doc = FoilSource.WriteSurfaces(doc, tipName, k6, Points(tx, tuy), Ids(6), Points(tx, tly), Ids(6));
        return doc;
    }

    private static DatProfile SampleDat(ProfileDefinition profile)
    {
        const int n = 161;
        var upper = new List<ProfilePoint>(n);
        var lower = new List<ProfilePoint>(n);
        for (int i = 0; i < n; i++)
        {
            double x = i == 0 ? 0d : i == n - 1 ? 1d : 0.5 * (1d - double.CosPi(i / (double)(n - 1)));
            upper.Add(new ProfilePoint(x, i == 0 ? 0d : ProfileEvaluator.OrdinateAt(profile.Upper, x)));
            lower.Add(new ProfilePoint(x, i == 0 ? 0d : ProfileEvaluator.OrdinateAt(profile.Lower, x)));
        }
        return new DatProfile(profile.Name, "fixture", upper, lower, Array.Empty<byte>(), 2 * n - 1);
    }

    private static double[][] Points(double[] x, double[] y) => x.Select((v, i) => new[] { v, y[i] }).ToArray();
    private static double[][] Copy(double[][] pts) => pts.Select(p => new[] { p[0], p[1] }).ToArray();
    private static string[] Ids(int count) => Enumerable.Range(0, count).Select(i => "cv-" + i.ToString(CultureInfo.InvariantCulture)).ToArray();

    // ---- Test 1: XPA-I3 equal-spacing identity bitwise --------------------------------------------------------------

    private static void Compatible_EqualSpacing_IdentityBitwise()
    {
        foreach (byte[] fixture in new[] { FoilSource.NewDefault(), FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example)) })
        {
            var parsed = FoilSource.Parse(fixture);
            var compat = Compatible.Measure(parsed);
            Equal(false, compat.NeedsFit);
            Equal(false, compat.Failed);
            Equal(true, ReferenceEquals(parsed.Definition, compat.Definition));
            foreach (var match in compat.Matches)
            {
                Equal("identity", match.Kind);
                Equal(0d, match.RecordDeviation);
                Equal(0d, match.SurfaceDeviationMeters);
                Equal(0d, match.SurfaceEta);
            }
            Equal(Evaluator.V2, Evaluator.IdFor(parsed));
            Equal(GeometryStatus.Certified, Geometry.Assess(parsed).Status);
        }
    }

    // ---- Test 3: XPA-I1 record bytes unchanged ----------------------------------------------------------------------

    private static void Compatible_NeverWritten_RecordBytesUnchanged()
    {
        byte[] lp2 = LP2Fixture();
        byte[] snapshot = lp2.ToArray();
        var parsed = FoilSource.Parse(lp2);
        var compat = Compatible.Measure(parsed, FastGrid);
        Equal(true, compat.NeedsFit);
        Equal(GeometryStatus.Certified, Geometry.Assess(parsed).Status);
        _ = Placement.Surface(lp2, "accepted", 0, CancellationToken.None, 5, 11);
        Equal(true, snapshot.AsSpan().SequenceEqual(lp2));
        Equal(true, snapshot.AsSpan().SequenceEqual(parsed.Source));
    }

    // ---- Test 4: Basis choice (richest spans -> vertices -> lowest station) & determinism ---------------------------

    private static void Compatible_Basis_RichestThenVerticesThenStation_Deterministic()
    {
        // Case A: Tip (5 spans, 10 vertices) beats Root (3 spans, 8 vertices) on L-P2.
        var lp2 = FoilSource.Parse(LP2Fixture());
        var c1 = Compatible.Of(lp2.Definition!);
        Compatible.ClearMemos();
        var c2 = Compatible.Of(lp2.Definition!);
        Equal("naca-0012", c1.BasisProfile);
        Equal(c1.BasisProfile, c2.BasisProfile);
        Equal(c1.SharedSpans, c2.SharedSpans);
        // Repeat gives equal bits on every copy control point.
        for (int p = 0; p < c1.Definition.Profiles.Length; p++)
        {
            SameCurveBits(c1.Definition.Profiles[p].Upper, c2.Definition.Profiles[p].Upper);
            SameCurveBits(c1.Definition.Profiles[p].Lower, c2.Definition.Profiles[p].Lower);
        }

        // Case B: Tie in spans (both 5 spans) broken by vertex count (anchor raises vertices from 10 to 15? No, anchor adds 1 span too;
        // raising an existing simple knot from mult 1 to mult 3 keeps 5 spans and adds 2 vertices: 10 -> 12!).
        byte[] baseDoc = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 0).Source;
        var def = FoilSource.Parse(baseDoc).Definition!;
        var tip = def.Profiles[def.Assignments[1].Profile];
        // Insert existing interior knot 0.4 twice on Tip -> still 5 spans, 12 vertices > Root's 10 vertices.
        var u1 = FoilSource.InsertOnce(tip.Upper.Knots, tip.Upper.Points, 5, 0.4);
        var l1 = FoilSource.InsertOnce(tip.Lower.Knots, tip.Lower.Points, 5, 0.4);
        var u2 = FoilSource.InsertOnce(u1.Knots, u1.Points, 5, 0.4);
        var l2 = FoilSource.InsertOnce(l1.Knots, l1.Points, 5, 0.4);
        byte[] tieVertices = FoilSource.WriteSurfaces(baseDoc, tip.Name, u2.Knots, u2.Points, Ids(u2.Points.Length), l2.Points, Ids(l2.Points.Length));
        var cVert = Compatible.Of(FoilSource.Parse(tieVertices).Definition!);
        Equal(tip.Name, cVert.BasisProfile);

        // Case C: Tie in spans (1 span) AND vertices (6 vertices) -> lowest station index (station 0 = Root) wins.
        var over = FoilSource.Parse(OverLimitPairFixture());
        var cTie = Compatible.Of(over.Definition!);
        Equal(over.Definition!.Profiles[over.Definition.Assignments[0].Profile].Name, cTie.BasisProfile);
    }

    // ---- Test 5: ADR-0010 binding test extended to CF documents (<= 1 nm outside certified enclosure) ---------------

    private static void Placement_CompatiblePair_DisplayWithinCertifiedEnclosure()
    {
        foreach (var (name, bytes) in new[] { ("L-P1", LP1Fixture()), ("L-P2", LP2Fixture()) })
        {
            var parsed = FoilSource.Parse(bytes);
            var assessment = Geometry.Assess(parsed);
            Equal(GeometryStatus.Certified, assessment.Status);
            var certificate = assessment.Certificate!;
            var view = Placement.Surface(bytes, "accepted", 0, CancellationToken.None, 9, 11);
            var xs = Placement.ChordGrid(view.Sections[0].Upper.Count);
            double worst = 0;
            for (int s = 0; s < view.Sections.Count; s += 2)
            {
                var section = view.Sections[s];
                foreach (int i in new[] { 0, 2, 5, 8, xs.Count - 1 })
                {
                    worst = Math.Max(worst, Outside(Geometry.PointAt(certificate, section.Eta, xs[i], true, false), section.Upper[i]));
                    worst = Math.Max(worst, Outside(Geometry.PointAt(certificate, section.Eta, xs[i], false, false), section.Lower[i]));
                }
            }
            Console.WriteLine($"MEASURE cf_enclosure fixture={name} max_outside_m={worst:G17}");
            True(worst <= 1e-9, $"{name}: display point {worst:G17} m outside certified enclosure (> 1 nm)");
        }
    }

    // ---- Test 6: XPA-I7 only the edited section block changes -------------------------------------------------------

    private static void SectionStep_UnderCompatible_OnlyEditedBlockChanges()
    {
        // On Example foil, MakeUnique on Root, then run steps on Root: Tip's block ("section-a") stays byte-identical.
        using var session = SectionDraftTests.Opened();
        string draftId = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(draftId, 0);
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.MakeUnique());
        string tipBefore = ProfileBlock(view.Bytes, "section-a");

        // 1. Move (x and y)
        var (x3, y3) = SectionDraftTests.Point(view, SurfaceSide.Upper, "cv-3");
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", x3 + 0.005, y3 + 0.001));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 2. Insert
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Insert(SurfaceSide.Upper, 0.45));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 3. SetType Control -> Anchor
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 4. SetTangent Angle
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 2, null));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 5. Fair
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Fair(null, 0.02, PreserveEnds.Position));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 6. InsertAnchor (exact shape) then SetType Anchor -> Control (within 10 um paired refit)
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.InsertAnchor(SurfaceSide.Upper, 0.52));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));
        var anchors = Sections.View(view.Bytes, 0, SurfaceSide.Upper, "preview", view.Generation).Points
            .Where(p => p.Role == PointRole.Anchor && p.Id != "cv-4").ToArray();
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.SetType(SurfaceSide.Upper, anchors[0].Id, false));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 7. Delete
        var pts = Sections.View(view.Bytes, 0, SurfaceSide.Upper, "preview", view.Generation).Points;
        string deletable = pts.First(p => p.Index >= 2 && p.Index <= pts.Count - 3 && p.Role == PointRole.Control).Id;
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Delete(SurfaceSide.Upper, deletable));
        Equal(tipBefore, ProfileBlock(view.Bytes, "section-a"));

        // 8. Rebuild (on LP1Fixture where Tip chord is 12.7 mm so rebuilt basis keeps Tip matched within 10 um)
        session.Cancel(draftId);
        using var rebuildSession = new AuthoringSession();
        rebuildSession.Open(LP1Fixture(), SectionDraftTests.Id(), true);
        string rebuildDraft = SectionDraftTests.Id();
        var rView = rebuildSession.BeginSectionDraft(rebuildDraft, 0);
        string lp1TipBefore = ProfileBlock(rView.Bytes, "naca-0012");
        rView = rebuildSession.ApplySectionStep(rebuildDraft, rView.Generation, new SectionStep.Rebuild(null, 16, 0.05, PreserveEnds.Position));
        Equal(lp1TipBefore, ProfileBlock(rView.Bytes, "naca-0012"));
        Equal(GeometryStatus.Certified, rebuildSession.AssessSection(rebuildDraft, rView.Generation, CancellationToken.None).Status);
        return;
    }

    // ---- Test 7: L-P1 Tip6 on Root10 exact Boehm refinement (< 1e-12 chord) -----------------------------------------

    private static void Compatible_Tip6OnRoot10_ExactRefinement()
    {
        var parsed = FoilSource.Parse(LP1Fixture());
        var compat = Compatible.Measure(parsed, FastGrid);
        Equal(false, compat.Failed);
        Equal(true, compat.NeedsFit);
        var tipMatch = compat.Matches.Single(m => !m.Basis);
        Equal("refined", tipMatch.Kind);
        True(tipMatch.RecordDeviation < 1e-12, $"L-P1 record deviation {tipMatch.RecordDeviation:E3} >= 1e-12 (sampled lower bound)");
        True(tipMatch.SurfaceDeviationMeters < 1e-9, $"L-P1 surface deviation {tipMatch.SurfaceDeviationMeters:E3} m >= 1 nm (sampled lower bound)");
        Equal(GeometryStatus.Certified, Geometry.Assess(parsed).Status);
    }

    // ---- Test 8: L-P2 section-a 8 on NACA 10 residual at both levels + BC-3 cosine nose refinement check ------------

    private static void Compatible_Section8OnNaca10_ResidualReportedBothLevels()
    {
        var parsed = FoilSource.Parse(LP2Fixture());
        var compat = Compatible.Measure(parsed); // production grid: 129 eta x 2001 x
        Equal(false, compat.Failed);
        Equal(true, compat.NeedsFit);
        var rootMatch = compat.Matches.Single(m => !m.Basis);
        var tipMatch = compat.Matches.Single(m => m.Basis);
        Equal("identity", tipMatch.Kind);
        Equal(0d, tipMatch.SurfaceDeviationMeters);
        Equal("fitted", rootMatch.Kind);
        // L-P2 probe values: record ~3.9e-5 c, surface ~5.7 um (sampled lower bounds).
        True(rootMatch.RecordDeviation is > 2.5e-5 and < 6.0e-5,
            $"L-P2 record deviation {rootMatch.RecordDeviation:E4} outside [2.5e-5, 6.0e-5] chord (sampled lower bound)");
        True(rootMatch.SurfaceDeviationMeters is > 4.0e-6 and < 8.0e-6,
            $"L-P2 surface deviation {rootMatch.SurfaceDeviationMeters * 1e6:F3} um outside [4.0, 8.0] um (sampled lower bound)");
        True(rootMatch.SurfaceDeviationMeters <= Compatible.LimitMeters, "L-P2 must stay within 10 um");

        // BC-3 (CG-2): cosine-clustered nose x grid-refinement check (clusters samples tightly around x = 0 nose).
        double[] cosineXs = Placement.ChordGrid(2001).ToArray();
        var refineEtas = StationEdits.Grid(new StationGrid(65, 2001), (0, 1));
        var authRef = Placement.SectionsForMeasureAuthored(parsed.Definition!, refineEtas, cosineXs, CancellationToken.None).ToArray();
        var copyRef = Placement.SectionsForMeasure(parsed.Definition!, refineEtas, cosineXs, CancellationToken.None).ToArray();
        var (_, cosineMeters, _, _) = StationEdits.CompareMeasures(authRef, copyRef, refineEtas, cosineXs.Length);
        Console.WriteLine($"MEASURE lp2_residual prod_um={rootMatch.SurfaceDeviationMeters * 1e6:F4} cosine_nose_um={cosineMeters * 1e6:F4} record_c={rootMatch.RecordDeviation:E4}");
        True(Math.Abs(cosineMeters - rootMatch.SurfaceDeviationMeters) <= 1.0e-6,
            $"Cosine-clustered nose refinement ({cosineMeters * 1e6:F4} um) diverges from production grid ({rootMatch.SurfaceDeviationMeters * 1e6:F4} um) by > 1 um");
        True(cosineMeters <= Compatible.LimitMeters, "Cosine-clustered nose peak must stay within 10 um");
    }

    // ---- Test 9: Tip anchor inserted into S* (L-P4 vs L-P4n) + skip-if-present mult-5 (BC-3 CG-4) -------------------

    private static void Compatible_TipAnchor_InsertedIntoSharedSpacing()
    {
        var lp4 = FoilSource.Parse(LP4Fixture());
        var compat = Compatible.Measure(lp4, FastGrid);
        Equal(false, compat.Failed);
        Equal(true, compat.HasOtherAnchors);
        Equal(6, compat.SharedSpans);
        // Basis (Root10) was refined by Boehm insertion of Tip's anchor -> rounding-only change (< 1e-12 c).
        var rootMatch = compat.Matches.Single(m => m.Basis);
        var tipMatch = compat.Matches.Single(m => !m.Basis);
        Equal("refined", rootMatch.Kind);
        True(rootMatch.RecordDeviation < 1e-12, $"Root basis Boehm anchor insertion changed by {rootMatch.RecordDeviation:E3}");
        True(tipMatch.SurfaceDeviationMeters < 1e-6,
            $"L-P4 Tip6+anchor surface deviation {tipMatch.SurfaceDeviationMeters * 1e6:F4} um should be < 1 um (without anchor L-P4n is ~85 um)");
        Equal(GeometryStatus.Certified, Geometry.Assess(lp4).Status);

        // BC-3 (CG-4) skip-if-present: when both stations already have a mult-5 anchor at the same x,
        // S* does NOT double-insert or fail, and equal-spacing remains identity.
        byte[] sharedAnchor = SectionEdits.Apply(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example)), 0,
            new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;
        byte[] twoSameAnchor = FoilSource.MakeIndependent(sharedAnchor, "section-a", 0).Source;
        // Move a control y on station 0 so profiles differ, same spacing with mult-5 anchor already present.
        var defSame = FoilSource.Parse(twoSameAnchor).Definition!;
        string rootName = defSame.Profiles[defSame.Assignments[0].Profile].Name;
        var rootP = defSame.Profiles[defSame.Assignments[0].Profile];
        byte[] diffY = FoilSource.PatchProfilePoint(twoSameAnchor, rootName, "upper", rootP.Upper.Ids[2],
            rootP.Upper.Points[2][0], rootP.Upper.Points[2][1] + 0.002);
        var parsedSame = FoilSource.Parse(diffY);
        var compatSame = Compatible.Of(parsedSame.Definition!);
        Equal(false, compatSame.NeedsFit);
        Equal(true, ReferenceEquals(parsedSame.Definition, compatSame.Definition));

        // Now insert one extra simple knot on Root so spacings differ while BOTH share the exact same mult-5 anchor!
        byte[] diffKnots = FoilSource.InsertProfileKnot(diffY, rootName, 0.25).Source;
        var compatSkip = Compatible.Of(FoilSource.Parse(diffKnots).Definition!);
        Equal(false, compatSkip.Failed);
        Equal(false, compatSkip.HasOtherAnchors); // anchor was already present at mult 5 -> skipped!
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(diffKnots)).Status);
    }

    // ---- Test 10: XPA-I5 step over 10 um refused unchanged with Match* Data keys ------------------------------------

    private static void SectionStep_CopyOverLimit_RefusedUnchanged()
    {
        // Start from L-P2 (valid mixed doc, Root 8 pts on Tip 10 pts, ~5.7 um).
        // Rebuild Root (or move a point / create extreme kink on non-basis or shrink basis) so residual > 10 um.
        // Here: open Example foil (8 pts, 3 spans, 120 mm chord). MakeUnique on Root.
        // Rebuild Root to 6 pts (1 span) -> Tip (8 pts, 3 spans) leads; Root is fitted onto Tip's 8-pt spacing.
        // Actually: start from OverLimit-adjacent: Root 6-pt (1 span, x=[0,0,0.25,0.5,0.75,1]) + Tip 6-pt (1 span, shifted x)
        // where both start identical (equal spacing -> Certified), then Move Tip's control x+y so Tip no longer fits Root within 10 um!
        byte[] baseDoc = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 0).Source;
        var def = FoilSource.Parse(baseDoc).Definition!;
        string rootName = def.Profiles[def.Assignments[0].Profile].Name;
        string tipName = def.Profiles[def.Assignments[1].Profile].Name;
        double[] k6 = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1];
        double[] rx = [0, 0, 0.25, 0.50, 0.75, 1];
        double[] ry = [0, 0.025, 0.06, 0.055, 0.025, 0];
        // Both start on rx (1 span, 6 pts) — equal spacing, certified!
        byte[] doc = FoilSource.WriteSurfaces(baseDoc, rootName, k6, Points(rx, ry), Ids(6), Points(rx, ry.Select(v => -v).ToArray()), Ids(6));
        doc = FoilSource.WriteSurfaces(doc, tipName, k6, Points(rx, [0, 0.04, 0.09, 0.02, 0.06, 0]), Ids(6),
            Points(rx, [0, -0.02, -0.01, -0.07, -0.01, 0]), Ids(6));
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(doc)).Status);

        using var session = new AuthoringSession();
        session.Open(doc, SectionDraftTests.Id(), true);
        string draftId = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(draftId, 1); // edit Tip
        byte[] beforeBytes = view.Bytes.ToArray();
        long beforeGen = view.Generation;

        // Move Tip's cv-2 sharply left to x=0.05 (breaks shared 1-span x(t) -> Tip must fit onto Root's 1-span basis, residual >> 14 um).
        ContractError? refused = null;
        try
        {
            session.ApplySectionStep(draftId, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-2", 0.05, 0.11));
        }
        catch (ContractError error) { refused = error; }

        True(refused is not null, "Expected over-limit CF step refusal");
        Equal("DSL-GEOMETRY", refused!.Code);
        True(refused.Data["MatchProfile"] is string prof && prof == tipName, "MatchProfile data key missing");
        True(refused.Data["MatchDeviationMeters"] is double dev && dev > Compatible.LimitMeters, "MatchDeviationMeters <= 10 um");
        Equal(Compatible.LimitMeters, refused.Data["MatchLimitMeters"]);
        True(refused.Data["MatchEta"] is double eta && double.IsFinite(eta) && eta >= 0 && eta <= 1, "MatchEta invalid");
        // Draft unchanged.
        var draft = session.Snapshot().Draft!;
        Equal(beforeGen, draft.Generation);
        Equal(true, beforeBytes.AsSpan().SequenceEqual(draft.Bytes));
    }

    // ---- Test 11: Past blend capacity refused early (decided by Assess on candidate) --------------------------------

    private static void SectionStep_PastBlendCapacity_RefusedEarly()
    {
        // Build a 6-station foil where 6 differing stations certify (Ruling 74), then add a 7th differing station (via MakeUnique + y/x step):
        // Assess refuses with GEOMETRY-QUERY-OPERATIONS, so RequireNeighbourAbscissa refuses the step early!
        var baseDef = FoilSource.Parse(FoilSource.NewDefault()).Definition!;
        var baseUpper = baseDef.Profiles[0].Upper;
        var baseLower = baseDef.Profiles[0].Lower;
        double[] rootU = baseUpper.Points.Select(p => p[1]).ToArray();
        double[] rootL = baseLower.Points.Select(p => p[1]).ToArray();
        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var sb = new StringBuilder("  profiles {\n");
        string[] etas = ["root", "15 %", "30 %", "50 %", "70 %", "85 %", "tip"];
        // 6 distinct profiles; station 5 and 6 share s5 initially so 6 differing stations certify.
        for (int i = 0; i < 6; i++)
        {
            double amp = 0.008 * i;
            var uPts = baseUpper.Points.Select(p => new[] { p[0], p[1] + amp * 4 * p[0] * (1 - p[0]) }).ToArray();
            var lPts = baseLower.Points.Select(p => new[] { p[0], p[1] + amp * 4 * p[0] * (1 - p[0]) }).ToArray();
            var prof = new ProfileDefinition("s" + i, baseUpper with { Points = uPts }, baseLower with { Points = lPts }, "closed", 0, 0);
            FoilSource.AppendProfile(sb, prof);
        }
        sb.Append("  }\n  sections { at root profile \"s0\" at 15 % profile \"s1\" at 30 % profile \"s2\" at 50 % profile \"s3\" at 70 % profile \"s4\" at 85 % profile \"s5\" at tip profile \"s5\" }\n");
        byte[] sixDistinct = Encoding.UTF8.GetBytes(text[..start] + sb + text[end..]);
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(sixDistinct)).Status);

        using var session = new AuthoringSession();
        session.Open(sixDistinct, SectionDraftTests.Id(), true);
        string draftId = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(draftId, 6); // tip (shares s5 with 85 %)
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.MakeUnique());
        byte[] beforeBytes = view.Bytes.ToArray();
        var (x3, y3) = SectionDraftTests.Point(view, SurfaceSide.Upper, "cv-3");
        // Changing tip makes 7 differing stations -> Assess returns GEOMETRY-QUERY-OPERATIONS -> refused early!
        Refuses("DSL-GEOMETRY", () => session.ApplySectionStep(draftId, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", x3, y3 + 0.005)));
        Equal(true, beforeBytes.AsSpan().SequenceEqual(session.Snapshot().Draft!.Bytes));
    }

    // ---- Test 12: Degenerate fit returns NotAssessed with reason ----------------------------------------------------

    private static void Compatible_DegenerateFit_NotAssessedWithReason()
    {
        // Construct a foil where non-basis profile has a vertical interior tangent row that satisfies CheckTangentRows on
        // its own spacing, while S* has strictly ordered x around that anchor (so vertical cannot hold on S* -> FitToBasis throws DSL-LOCK -> Compatible.Of fails).
        byte[] baseDoc = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 0).Source;
        var def = FoilSource.Parse(baseDoc).Definition!;
        string rootName = def.Profiles[def.Assignments[0].Profile].Name;
        string tipName = def.Profiles[def.Assignments[1].Profile].Name;

        // Tip: 11-point curve with mult-5 anchor at cv-5 (x=0.5), vertical handles at x=0.5 (left y < anchor y < right y? Wait: CheckProfileRow vertical requires
        // left[0] == anchor[0] && right[0] == anchor[0] && (left[1]-anchor[1])*(right[1]-anchor[1]) < 0).
        // Root: 10-point NACA 0012 (5 spans) + 1 simple knot -> 6 spans (leads over Tip's 2 spans!).
        // When Root inserts Tip's anchor at x=0.5, S*'s handles at anchor-1 and anchor+1 have x != 0.5, and FitToBasis's Settle checks vertical sign or DSL-LOCK!
        // Even simpler: Tip has a vertical row on cv-5 where y[4] > y[5] > y[6] (monotone decreasing) on a profile where least-squares yields increasing y around 0.5, OR
        // Tip has a steep angle row 89.99999999999999? Wait: Math.Tan(90 deg * RadiansPerDegree) isn't infinite in double, but vertical row with concave sign or singular knots is!
        // Let's test a direct degenerate profile: Tip with 6-point 1-span curve whose interior knots/points cause FitToBasis to fail, OR a Tip whose anchor has angle=90!
        // Wait: CheckProfileRow accepts angle=90 (NearRay uses Sin/Cos, valid!), while DatImport.FitToBasis uses Math.Tan(90*RadiansPerDegree) -> finite huge slope -> wait, let's check vertical!
        // On Tip (11 pts, 2 spans): anchor at cv-5 (x=0.5, y=0.05), left handle (0.5, 0.049), right handle (0.5, 0.051) -> satisfies vertical on Tip!
        // Wait: if the overall upper shape is decreasing around x=0.5 (e.g. peak at x=0.25), then on S* the least-squares fit gives y[j-1] > y[j] > y[j+1] which still has (y[j-1]-y[j])*(y[j+1]-y[j]) < 0!
        // To make Settle throw DSL-LOCK on vertical: at the peak of the airfoil (x=0.35), the LSQ fit has y[j-1] < y[j] AND y[j+1] < y[j], so (y[j-1]-y[j])*(y[j+1]-y[j]) > 0 -> DSL-LOCK!
        double[] k11 = [0, 0, 0, 0, 0, 0, 0.5, 0.5, 0.5, 0.5, 0.5, 1, 1, 1, 1, 1, 1];
        double[] x11 = [0, 0, 0.1, 0.2, 0.35, 0.35, 0.35, 0.55, 0.75, 0.9, 1];
        // Peak sits at cv-5 (x = 0.35): authored vertical handles pass CheckProfileRow (0.0799 < 0.0800 < 0.0801),
        // while the LSQ fit on S* peaks at 0.35 so both handles fall below y[anchor] and vertical cannot hold (DSL-LOCK).
        double[] uy11 = [0, 0.03, 0.06, 0.075, 0.0799, 0.0800, 0.0801, 0.065, 0.04, 0.015, 0];
        double[] ly11 = [0, -0.03, -0.06, -0.075, -0.0799, -0.0800, -0.0801, -0.065, -0.04, -0.015, 0];
        byte[] deg = FoilSource.WriteSurfaces(baseDoc, tipName, k11, Points(x11, uy11), Ids(11), Points(x11, ly11), Ids(11));
        deg = FoilSource.WriteSideTangents(deg, tipName, SurfaceSide.Upper, [new TangentRow("cv-5", "vertical", null)]);
        var parsed = FoilSource.Parse(deg);
        Equal(true, parsed.IsParsed);
        var compat = Compatible.Of(parsed.Definition!);
        Equal(true, compat.Failed);
        True(!string.IsNullOrWhiteSpace(compat.FailureReason), "FailureReason must be populated");
        var assessment = Geometry.Assess(parsed);
        Equal(GeometryStatus.NotAssessed, assessment.Status);
        Equal("GEOMETRY-FIT-SINGULAR", assessment.Code);
        True(!string.IsNullOrWhiteSpace(assessment.Reason), "NotAssessed reason must be populated");
    }

    // ---- Test 13: Class sweep — paired steps result certifies -------------------------------------------------------

    private static void SectionEdits_PairedSteps_ResultCertifies()
    {
        byte[] identified = SectionPointTests.Identified();
        byte[] anchored = SectionEdits.Apply(identified, 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;

        // 1. Angle
        byte[] angled = SectionEdits.Apply(anchored, 0, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 20, null)).Bytes;
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(angled)).Status);

        // 2. Symmetric
        byte[] sym = SectionEdits.Apply(anchored, 0, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Symmetric, null, null)).Bytes;
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(sym)).Status);

        // 3. PairedXMove
        double x3 = FoilSource.Parse(identified).Definition!.Profiles[0].Upper.Points[3][0];
        byte[] moved = SectionEdits.Apply(identified, 0, new SectionStep.Move(SurfaceSide.Upper, "cv-3", x3 + 0.01, 0.07)).Bytes;
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(moved)).Status);

        // 4. PairedSetTangent Horizontal
        byte[] horiz = SectionEdits.Apply(anchored, 0, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Horizontal, null, null)).Bytes;
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(horiz)).Status);
    }

    // ---- Test 14: F-XPA-1 Angle keeps paired x on both surfaces -----------------------------------------------------

    private static void SectionEdits_SetTangentAngle_KeepsXOnBothSurfaces()
    {
        byte[] anchored = SectionEdits.Apply(SectionPointTests.Identified(), 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;
        var before = FoilSource.Parse(anchored).Definition!.Profiles[0];
        int at = Array.IndexOf(before.Upper.Ids, "cv-4");
        var lowerHandle = before.Lower.Points[at + 1];
        byte[] lifted = SectionEdits.Apply(anchored, 0, new SectionStep.Move(SurfaceSide.Lower, before.Lower.Ids[at + 1], lowerHandle[0], lowerHandle[1] - 0.025)).Bytes;
        byte[] angled = SectionEdits.Apply(lifted, 0, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 15, null)).Bytes;
        var profile = FoilSource.Parse(angled).Definition!.Profiles[0];
        for (int i = 0; i < profile.Upper.Points.Length; i++)
            Equal(profile.Upper.Points[i][0], profile.Lower.Points[i][0]);
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(angled)).Status);
    }

    // ---- Test 15: Finish one row, reopen same BlendMatch; BC-3 reopen reports rather than refuses over-limit --------

    private static void SectionDraft_CompatibleStep_FinishOneRow_ReopenSameMatch()
    {
        using var session = SectionDraftTests.Opened();
        int rowsBefore = session.Envelope().Accepted.Length;
        string draftId = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(draftId, 0);
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.MakeUnique());
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true));
        True(view.Last?.Matches is { Count: 2 }, "Step report must carry BlendMatch list");
        var assessment = session.AssessSection(draftId, view.Generation, CancellationToken.None);
        Equal(GeometryStatus.Certified, assessment.Status);
        string? acceptedId = session.FinishSection(SectionDraftTests.Id(), assessment);
        True(acceptedId is not null, "FinishSection must commit one row");
        Equal(rowsBefore + 1, session.Envelope().Accepted.Length);

        var liveMatches = session.InspectAccepted().Matches;
        Equal(2, liveMatches.Count);
        byte[] image = session.SaveImage();

        Compatible.ClearMemos();
        using var reopened = new AuthoringSession();
        reopened.Reopen(image);
        var reopenedMatches = reopened.InspectAccepted().Matches;
        Equal(liveMatches.Count, reopenedMatches.Count);
        for (int i = 0; i < liveMatches.Count; i++)
        {
            Equal(liveMatches[i].Profile, reopenedMatches[i].Profile);
            Equal(liveMatches[i].Basis, reopenedMatches[i].Basis);
            Equal(liveMatches[i].Kind, reopenedMatches[i].Kind);
            Equal(liveMatches[i].SharedSpans, reopenedMatches[i].SharedSpans);
            Equal(BitConverter.DoubleToInt64Bits(liveMatches[i].RecordDeviation), BitConverter.DoubleToInt64Bits(reopenedMatches[i].RecordDeviation));
            Equal(BitConverter.DoubleToInt64Bits(liveMatches[i].SurfaceDeviationMeters), BitConverter.DoubleToInt64Bits(reopenedMatches[i].SurfaceDeviationMeters));
        }

        // BC-3: on reopen, an over-limit copy is REPORTED, never refused!
        byte[] overBytes = OverLimitPairFixture();
        var overParsed = FoilSource.Parse(overBytes);
        var overCheck = Compatible.Measure(overParsed, FastGrid);
        True(overCheck.Matches.Any(m => m.SurfaceDeviationMeters > Compatible.LimitMeters), "Fixture must be over 10 um");
        Equal(GeometryStatus.Certified, Geometry.Assess(overParsed).Status);
        using var overSession = new AuthoringSession();
        overSession.Open(overBytes, SectionDraftTests.Id(), true);
        byte[] overImage = overSession.SaveImage();
        using var overReopened = new AuthoringSession();
        overReopened.Reopen(overImage);
        var overMatches = overReopened.InspectAccepted().Matches;
        True(overMatches.Any(m => m.SurfaceDeviationMeters > m.LimitMeters), "Reopen must report the over-limit copy");
    }

    // ---- Test 16: Import on unique station of a mixed-spacing foil — matched or refused -----------------------------

    private static void ImportOnUnique_OwnSpacingAllowed_MatchedOrRefused()
    {
        // Start with L-P2 (Root 8-pt section-a, Tip 10-pt NACA 0012).
        // Importing NACA 0009 onto unique Tip (10-pt sqrt spacing) lands within 10 um, CF keeps Root matched within 10 um, and certifies!
        using var session = new AuthoringSession();
        session.Open(LP2Fixture(), SectionDraftTests.Id(), true);
        string draftId = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(draftId, 1); // Tip (10-pt NACA basis)
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Import(CatalogGenerator.Naca4("0009")));
        Equal(GeometryStatus.Certified, session.AssessSection(draftId, view.Generation, CancellationToken.None).Status);
        True(view.Last?.Matches is { Count: 2 }, "Import on unique mixed-spacing foil reports BlendMatches");
        True(view.Last!.Matches!.All(m => m.SurfaceDeviationMeters <= m.LimitMeters), "Matched within 10 um");
        session.Cancel(draftId);

        // Second fixture: importing cambered NACA 4412 onto unique 8-pt Root beside 10-pt Tip is refused (CAT-SPACING), draft unchanged.
        string draft2 = SectionDraftTests.Id();
        var view2 = session.BeginSectionDraft(draft2, 0);
        byte[] before = view2.Bytes.ToArray();
        Refuses("CAT-SPACING", () => session.ApplySectionStep(draft2, view2.Generation, new SectionStep.Import(CatalogGenerator.Naca4("4412"))));
        Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Draft!.Bytes));
    }

    // ---- Test 17: Rebuild on basis changes S* and refits copies -----------------------------------------------------

    private static void RebuildOnBasis_CopiesRefitAndReported()
    {
        // LP1 (Root 10-pt NACA basis, Tip 6-pt sqrt): Rebuild Root to 16 points -> Root stays basis with 11 spans,
        // S* changes, and Tip is refit onto S* within 10 um and reported.
        using var session = new AuthoringSession();
        session.Open(LP1Fixture(), SectionDraftTests.Id(), true);
        string draftId = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(draftId, 0);
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Rebuild(null, 16, 0.05, PreserveEnds.Position));
        True(view.Last?.Matches is { Count: 2 }, "Rebuild reports BlendMatches");
        var basis = view.Last!.Matches!.Single(m => m.Basis);
        var fitted = view.Last!.Matches!.Single(m => !m.Basis);
        Equal(11, basis.SharedSpans);
        Equal("fitted", fitted.Kind);
        True(fitted.SurfaceDeviationMeters <= Compatible.LimitMeters, "Refit after Rebuild within 10 um");
        Equal(GeometryStatus.Certified, session.AssessSection(draftId, view.Generation, CancellationToken.None).Status);
    }

    // ---- C1: Compatible_AuthoredMode_SingleCaller (source + reflection/IL scan) -------------------------------------

    private static void Compatible_AuthoredMode_SingleCaller()
    {
        // 1. Source scan across all src/**/*.cs
        string srcRoot = Path.Combine(PlacementTests.RepoRoot(), "src");
        string objSep = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
        var sourceHits = new List<string>();
        foreach (string path in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains(objSep, StringComparison.Ordinal)))
        {
            string rel = Path.GetRelativePath(srcRoot, path).Replace(Path.DirectorySeparatorChar, '/');
            if (rel == "CfdWorkbench.Core/Placement.cs") continue;
            string text = File.ReadAllText(path);
            if (text.Contains("SectionsForMeasureAuthored", StringComparison.Ordinal))
                sourceHits.Add(rel);
        }
        Equal(1, sourceHits.Count);
        Equal("CfdWorkbench.Core/Compatible.cs", sourceHits[0]);

        // 2. Reflection + IL scan over Core assembly: ONLY Compatible calls SectionsForMeasureAuthored.
        var coreAsm = typeof(Placement).Assembly;
        var authoredMethod = typeof(Placement).GetMethod("SectionsForMeasureAuthored", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("Placement.SectionsForMeasureAuthored not found");
        var callers = FindIlCallers(coreAsm, authoredMethod);
        True(callers.Count > 0, "Expected at least one caller of SectionsForMeasureAuthored");
        foreach (var caller in callers)
            True(IsInsideType(caller.DeclaringType, typeof(Compatible)),
                $"C1 violation: {caller.DeclaringType?.FullName}.{caller.Name} calls Placement.SectionsForMeasureAuthored");
    }

    // ---- C2 + BC-1: Placement_SingleEntry_NoAuthoredBlendReader -----------------------------------------------------

    private static void Placement_SingleEntry_NoAuthoredBlendReader()
    {
        var placementType = typeof(Placement);
        var prepareCompat = placementType.GetMethod("PrepareCompatible", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Placement.PrepareCompatible not found");

        // Every declared method on Placement (excluding property accessors/compiler-generated) must be classified:
        // (a) placed/blend reader calling PrepareCompatible (directly or via another classified Placement entry),
        // (b) explicitly allowlisted authored-record reader/predicate per Ruling 269 BC-1,
        // (c) non-profile helper (channels/grid/eta/counters).
        var mustUseCompatible = new HashSet<string>(StringComparer.Ordinal)
        {
            "Surface", "Sections", "SectionsForMeasure"
        };
        var allowlistedAuthored = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OwnProfile"] = "Ruling 269 BC-1: intentional authored-record reader for DatExport DatShape.Own (unscaled as drawn).",
            ["ProfileDifferenceMaximum"] = "Ruling 269 BC-1: pure utility on caller-supplied curves (StationEdits.Blend passes compatible copies for R3).",
            ["SameAbscissa"] = "Ruling 269 BC-1: intentional authored-record spacing predicate (Evaluator.NeedsCompatibleFit, StationEdits).",
            ["SameRecord"] = "Ruling 269 BC-1: intentional record comparison predicate (ReadStation calls it on the compatible definition).",
            ["SectionsForMeasureAuthored"] = "Ruling 269 C1: single-caller authored-reference mode called exclusively by Compatible.cs."
        };
        var nonProfileEntries = new HashSet<string>(StringComparer.Ordinal)
        {
            "Frame", "StationEta", "ChordGrid", "ResetEvaluatorCounts"
        };

        var publicAndInternal = placementType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsPrivate && !m.IsSpecialName && !m.Name.Contains('<', StringComparison.Ordinal))
            .ToArray();

        foreach (var method in publicAndInternal)
        {
            if (mustUseCompatible.Contains(method.Name))
            {
                True(CallsMethod(method, prepareCompat),
                    $"C2 violation: Placement.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}) does not call PrepareCompatible");
            }
            else if (allowlistedAuthored.ContainsKey(method.Name))
            {
                // Allowlisted with documented reason above.
            }
            else if (nonProfileEntries.Contains(method.Name))
            {
                // Reads no profile ordinates.
            }
            else
            {
                throw new InvalidOperationException($"C2 violation: unclassified public/internal Placement member '{method.Name}'");
            }
        }

        // Also verify StationEdits.Blend is not bypassed with authored mismatched-spacing curves:
        // StationEdits.Blend is private and takes compatible copies on R3 (checked directly in Promote_R3_OnCopies_RegimeMeasured / BC-1).
        var stationEditsType = typeof(AuthoringSession).Assembly.GetType("CfdWorkbench.Core.StationEdits")
            ?? throw new InvalidOperationException("StationEdits not found");
        var blendMethod = stationEditsType.GetMethod("Blend", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("StationEdits.Blend not found");
        Equal(true, blendMethod.IsPrivate);
    }

    // ---- Compatible_SurfaceResidual_SameMeasureAsStations (bit-for-bit agreement) -----------------------------------

    private static void Compatible_SurfaceResidual_SameMeasureAsStations()
    {
        byte[] lp2 = LP2Fixture();
        var parsed = FoilSource.Parse(lp2);
        var compat = Compatible.Measure(parsed, FastGrid);
        var rootMatch = compat.Matches.Single(m => !m.Basis);

        // Compute the exact same authored vs copies comparison via StationEdits.CompareMeasures on the exact same grid.
        var etas = StationEdits.Grid(FastGrid, (0, 1));
        double[] xs = Enumerable.Range(0, FastGrid.XSamples).Select(i => i / (double)(FastGrid.XSamples - 1)).ToArray();
        var authored = Placement.SectionsForMeasureAuthored(parsed.Definition!, etas, xs, CancellationToken.None).ToArray();
        var copies = Placement.SectionsForMeasure(lp2, etas, xs, CancellationToken.None).ToArray();
        var (_, meters, atEta, _) = StationEdits.CompareMeasures(authored, copies, etas, xs.Length);

        Equal(BitConverter.DoubleToInt64Bits(meters), BitConverter.DoubleToInt64Bits(rootMatch.SurfaceDeviationMeters));
        Equal(BitConverter.DoubleToInt64Bits(atEta), BitConverter.DoubleToInt64Bits(rootMatch.SurfaceEta));
    }

    // ---- BC-4: 3-station two-non-identity-copy fixture --------------------------------------------------------------

    private static void Compatible_MultiStation_TwoNonIdentityCopies_AttributedAndIdentityZero()
    {
        // Station 0 (root): NACA 0012 10-pt (5 spans, BASIS -> identity, residual 0).
        // Station 1 (50 %): section-a 8-pt (3 spans -> fitted copy #1).
        // Station 2 (tip):  Tip6 6-pt sqrt (1 span -> refined copy #2, or 7-pt custom fitted copy #2).
        var baseDef = FoilSource.Parse(FoilSource.NewDefault()).Definition!;
        var root = baseDef.Profiles[0]; // 10-pt NACA 0012
        var sectionA = FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example))).Definition!.Profiles[0];
        var (k8, x8) = DatImport.OwnSqrtBasis(8);
        // Make station 2 an 8-point sqrt spacing (knots 1/3, 2/3 — does NOT nest in 10-pt 0.2..0.8 -> fitted copy #2!).
        var fit8 = DatImport.FitToBasis(SampleDat(sectionA), k8, x8, 5, true)!;
        var tip8 = new ProfileDefinition("tip-8",
            root.Upper with { Knots = k8, Points = Points(x8, fit8.Upper), Ids = Ids(8) },
            root.Lower with { Knots = k8, Points = Points(x8, fit8.Lower), Ids = Ids(8) },
            "closed", 0, 0);
        var mid8 = new ProfileDefinition("mid-8",
            sectionA.Upper with { Ids = Ids(sectionA.Upper.Points.Length) },
            sectionA.Lower with { Ids = Ids(sectionA.Lower.Points.Length) },
            sectionA.Closure, 0, 0);

        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var sb = new StringBuilder("  profiles {\n");
        FoilSource.AppendProfile(sb, root);
        FoilSource.AppendProfile(sb, mid8);
        FoilSource.AppendProfile(sb, tip8);
        sb.Append("  }\n  sections { at root profile \"naca-0012\" at 50 % profile \"mid-8\" at tip profile \"tip-8\" }\n");
        byte[] threeStation = Encoding.UTF8.GetBytes(text[..start] + sb + text[end..]);

        var parsed = FoilSource.Parse(threeStation);
        var compat = Compatible.Measure(parsed, FastGrid);
        Equal(false, compat.Failed);
        Equal(3, compat.Matches.Count);

        var m0 = compat.Matches.Single(m => m.Profile == "naca-0012");
        var m1 = compat.Matches.Single(m => m.Profile == "mid-8");
        var m2 = compat.Matches.Single(m => m.Profile == "tip-8");
        Equal(true, m0.Basis);
        Equal("identity", m0.Kind);
        Equal(0d, m0.RecordDeviation);
        Equal(0d, m0.SurfaceDeviationMeters);
        Equal(0d, m0.SurfaceEta);

        Equal("fitted", m1.Kind);
        Equal("fitted", m2.Kind);
        True(m1.SurfaceDeviationMeters > 0, "Non-identity mid-8 must have positive surface residual");
        True(m2.SurfaceDeviationMeters > 0, "Non-identity tip-8 must have positive surface residual");
        // Verify all-authored vs all-copies over both intervals [0, 0.5] and [0.5, 1] equals max(m1, m2) bitwise.
        var allEtas = StationEdits.Grid(FastGrid, (0, 0.5), (0.5, 1));
        double[] xs = Enumerable.Range(0, FastGrid.XSamples).Select(i => i / (double)(FastGrid.XSamples - 1)).ToArray();
        var auth = Placement.SectionsForMeasureAuthored(parsed.Definition!, allEtas, xs, CancellationToken.None).ToArray();
        var copy = Placement.SectionsForMeasure(threeStation, allEtas, xs, CancellationToken.None).ToArray();
        var (_, globalMeters, _, _) = StationEdits.CompareMeasures(auth, copy, allEtas, xs.Length);
        Equal(BitConverter.DoubleToInt64Bits(globalMeters), BitConverter.DoubleToInt64Bits(Math.Max(m1.SurfaceDeviationMeters, m2.SurfaceDeviationMeters)));
        Equal(GeometryStatus.Certified, Geometry.Assess(parsed).Status);
    }

    // ---- BC-2: Authored tangent rows checked before refit + definition.Curves synced --------------------------------

    private static void Compatible_AuthoredTangentCheckedBeforeRefit_AndCurvesSynced()
    {
        // 1. Compatible.Of MUST update both definition.Profiles AND matching definition.Curves entries.
        var lp2 = FoilSource.Parse(LP2Fixture());
        var compat = Compatible.Of(lp2.Definition!);
        foreach (var profile in compat.Definition.Profiles)
        {
            Equal(true, ReferenceEquals(profile.Upper, compat.Definition.Curves[profile.Upper.Path]));
            Equal(true, ReferenceEquals(profile.Lower, compat.Definition.Curves[profile.Lower.Path]));
        }
        // Geometry.Assess certifies L-P2 via definition.Curves — if Curves were left authored, SharedAbscissa would refuse!
        Equal(GeometryStatus.Certified, Geometry.Assess(lp2).Status);

        // 2. Authored tangent violation on a non-basis profile MUST still be refused (Invalid / DSL-LOCK) even under CF.
        byte[] lp4 = LP4Fixture();
        // Add a horizontal tangent row to Tip's anchor whose handles are NOT level -> authored CheckTangentRows must catch it!
        var lp4Def = FoilSource.Parse(lp4).Definition!;
        var tip = lp4Def.Profiles[lp4Def.Assignments[1].Profile];
        int anchorIdx = Enumerable.Range(5, tip.Upper.Points.Length - 10)
            .First(i => FoilSource.IsAnchor(tip.Upper.Knots, tip.Upper.Points.Length, 5, i));
        string anchorId = tip.Upper.Ids[anchorIdx];
        byte[] badTangent = FoilSource.WriteSideTangents(lp4, tip.Name, SurfaceSide.Upper, [new TangentRow(anchorId, "horizontal", null)]);
        var badAssessment = Geometry.Assess(FoilSource.Parse(badTangent));
        Equal(GeometryStatus.Invalid, badAssessment.Status);
        Equal("DSL-LOCK", badAssessment.Code);
    }

    // ---- Test 23 (BC-3): Cross-platform deterministic residual & equal verdict (<= 1 um, 1e-6 relative) -------------

    private static void Compatible_FixtureHash_SameOnMacAndWindows()
    {
        var lp2 = FoilSource.Parse(LP2Fixture());
        var compat = Compatible.Measure(lp2);
        var assessment = Geometry.Assess(lp2);
        Equal(GeometryStatus.Certified, assessment.Status);
        var rootMatch = compat.Matches.Single(m => !m.Basis);
        // Pinned reference values for L-P2 under cfdw-cv/3 (161 double.CosPi fit samples, 129 x 2001 residual grid):
        // Cross-platform tolerance per BC-3 / CG-3: <= 1 um absolute, <= 1e-6 relative + equal verdict.
        const double expectedRecordChord = 3.9e-5;
        const double expectedSurfaceMeters = 5.7e-6;
        True(Math.Abs(rootMatch.RecordDeviation - expectedRecordChord) <= 1.5e-5,
            $"Record deviation {rootMatch.RecordDeviation:E6} drifted from reference {expectedRecordChord:E6}");
        True(Math.Abs(rootMatch.SurfaceDeviationMeters - expectedSurfaceMeters) <= 1.0e-6,
            $"Surface deviation {rootMatch.SurfaceDeviationMeters * 1e6:F4} um drifted > 1 um from reference {expectedSurfaceMeters * 1e6:F4} um");

        // Repeat after clearing memos must match bit-for-bit on the same platform.
        Compatible.ClearMemos();
        var repeat = Compatible.Measure(lp2);
        var repeatMatch = repeat.Matches.Single(m => !m.Basis);
        Equal(BitConverter.DoubleToInt64Bits(rootMatch.RecordDeviation), BitConverter.DoubleToInt64Bits(repeatMatch.RecordDeviation));
        Equal(BitConverter.DoubleToInt64Bits(rootMatch.SurfaceDeviationMeters), BitConverter.DoubleToInt64Bits(repeatMatch.SurfaceDeviationMeters));
    }

    // ---- CF-3: Evaluator.IdFor (BC-5 21-site sweep + zero stray literals) -------------------------------------------

    private static void Evaluator_IdFor_V3OnlyWhenSpacingDiffers()
    {
        Equal(Evaluator.V2, Evaluator.IdFor(FoilSource.NewDefault()));
        Equal(Evaluator.V2, Evaluator.IdFor(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example))));
        Equal(Evaluator.V3, Evaluator.IdFor(LP1Fixture()));
        Equal(Evaluator.V3, Evaluator.IdFor(LP2Fixture()));

        // BC-5: zero stray "cfdw-cv/..." literals anywhere under src/ outside Compatible.cs (Evaluator).
        string srcRoot = Path.Combine(PlacementTests.RepoRoot(), "src");
        string objSep = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
        var stray = new List<string>();
        foreach (string path in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains(objSep, StringComparison.Ordinal)))
        {
            string rel = Path.GetRelativePath(srcRoot, path).Replace(Path.DirectorySeparatorChar, '/');
            if (rel == "CfdWorkbench.Core/Compatible.cs") continue;
            string text = File.ReadAllText(path);
            if (text.Contains("\"cfdw-cv/", StringComparison.Ordinal))
                stray.Add(rel);
        }
        Equal("", string.Join(", ", stray));

        // BC-5: FoilSource.cs:139 single-profile Semantic stays ["cfdw-cv", "2"] so single-profile ProfileIdentity is unchanged.
        var exampleParsed = FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example)));
        Equal(Evaluator.V2, exampleParsed.Authored().Binding.Evaluator);
        var lp2Parsed = FoilSource.Parse(LP2Fixture());
        Equal(Evaluator.V3, lp2Parsed.Authored().Binding.Evaluator);
    }

    // ---- CF-3: Existing cfdw-cv/2 rows are never rewritten when a mixed-spacing step lands --------------------------

    private static void Evaluator_OldRowsNeverRewritten()
    {
        using var session = SectionDraftTests.Opened();
        // Create 3 cfdw-cv/2 design rows (open + 2 equal-spacing y-only edits).
        for (int edit = 0; edit < 2; edit++)
        {
            string dId = SectionDraftTests.Id();
            var v = session.BeginSectionDraft(dId, 0);
            v = session.ApplySectionStep(dId, v.Generation, SectionDraftTests.Raise(v, "cv-3", 0.002));
            session.FinishSection(SectionDraftTests.Id(), session.AssessSection(dId, v.Generation, CancellationToken.None));
        }
        var beforeDesigns = session.Envelope().Designs.ToArray();
        Equal(3, beforeDesigns.Length);
        foreach (var d in beforeDesigns) Equal(Evaluator.V2, d.Evaluator);

        // Now perform a mixed-spacing step (MakeUnique + Control->Anchor on Root) -> 4th design row is cfdw-cv/3.
        string mixDraft = SectionDraftTests.Id();
        var view = session.BeginSectionDraft(mixDraft, 0);
        view = session.ApplySectionStep(mixDraft, view.Generation, new SectionStep.MakeUnique());
        view = session.ApplySectionStep(mixDraft, view.Generation, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true));
        session.FinishSection(SectionDraftTests.Id(), session.AssessSection(mixDraft, view.Generation, CancellationToken.None));

        var afterDesigns = session.Envelope().Designs;
        Equal(4, afterDesigns.Length);
        for (int i = 0; i < 3; i++) Equal(beforeDesigns[i], afterDesigns[i]);
        Equal(Evaluator.V3, afterDesigns[3].Evaluator);

        // Save and reopen preserves all 4 design rows cleanly.
        byte[] image = session.SaveImage();
        using var reopened = new AuthoringSession();
        reopened.Reopen(image);
        Equal(4, reopened.Envelope().Designs.Length);
        Equal(Evaluator.V2, reopened.Envelope().Designs[0].Evaluator);
        Equal(Evaluator.V3, reopened.Envelope().Designs[3].Evaluator);
    }

    // ---- CF-3 (DR-DF-9 a): A cfdw-cv/2 row whose source needs CF is refused with DOC-VERSION on read ----------------

    private static void Evaluator_V2RowWithMixedSpacing_Refused()
    {
        using var session = new AuthoringSession();
        session.Open(LP2Fixture(), SectionDraftTests.Id(), true);
        var env = session.Envelope();
        Equal(Evaluator.V3, env.Designs[0].Evaluator);

        // Hand-forge the design row's evaluator back to cfdw-cv/2 while keeping the mixed-spacing source.
        var forged = env with { Designs = [env.Designs[0] with { Evaluator = Evaluator.V2 }] };
        byte[] forgedImage = NativeProject.Encode(forged);
        Refuses("DOC-VERSION", () => NativeProject.Read(forgedImage));
        Refuses("DOC-VERSION", () => new AuthoringSession().Reopen(forgedImage));

        // Unknown evaluator version also refuses with DOC-VERSION.
        var unknown = env with { Designs = [env.Designs[0] with { Evaluator = "cfdw-cv/99" }] };
        Refuses("DOC-VERSION", () => NativeProject.Read(NativeProject.Encode(unknown)));
    }

    // ---- CF-3: Removing the odd-spacing station returns the next design row to cfdw-cv/2 ----------------------------

    private static void Evaluator_RemoveBackToOneSpacing_V2Again()
    {
        // Build a 3-station wing: Root & Tip share 10-pt NACA 0012; Station 1 at 50 % is a 6-pt sqrt copy of NACA 0012
        // (same shape, different spacing -> mixed-spacing cfdw-cv/3; removing Station 1 changes loft by ~0 -> admitted, back to cfdw-cv/2!).
        var baseDef = FoilSource.Parse(FoilSource.NewDefault()).Definition!;
        var naca = baseDef.Profiles[0];
        var (k6, x6) = DatImport.OwnSqrtBasis(6);
        var fit6 = DatImport.FitToBasis(SampleDat(naca), k6, x6, 5, true)!;
        var mid6 = new ProfileDefinition("mid-6",
            naca.Upper with { Knots = k6, Points = Points(x6, fit6.Upper), Ids = Ids(6) },
            naca.Lower with { Knots = k6, Points = Points(x6, fit6.Lower), Ids = Ids(6) },
            "closed", 0, 0);
        // Check 6-pt NACA 0012 residual on 10-pt NACA: wait — use 10-pt with an exact Boehm inserted knot at 0.5 on mid so Remove deviation is ~0!
        var uIns = FoilSource.InsertOnce(naca.Upper.Knots, naca.Upper.Points, 5, 0.5);
        var lIns = FoilSource.InsertOnce(naca.Lower.Knots, naca.Lower.Points, 5, 0.5);
        var mid11 = new ProfileDefinition("mid-11",
            naca.Upper with { Knots = uIns.Knots, Points = uIns.Points, Ids = Ids(uIns.Points.Length) },
            naca.Lower with { Knots = lIns.Knots, Points = lIns.Points, Ids = Ids(lIns.Points.Length) },
            "closed", 0, 0);
        _ = mid6;

        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var sb = new StringBuilder("  profiles {\n");
        FoilSource.AppendProfile(sb, naca);
        FoilSource.AppendProfile(sb, mid11);
        sb.Append("  }\n  sections { at root profile \"naca-0012\" at 50 % profile \"mid-11\" at tip profile \"naca-0012\" }\n");
        byte[] mixed3 = Encoding.UTF8.GetBytes(text[..start] + sb + text[end..]);

        using var session = new AuthoringSession { StationGrid = FastGrid };
        session.Open(mixed3, SectionDraftTests.Id(), true);
        Equal(Evaluator.V3, session.Envelope().Designs[^1].Evaluator);
        Equal(Evaluator.V3, session.InspectAccepted().Authored.Binding.Evaluator);

        var removal = session.PreviewRemove(1);
        Equal(true, removal.Admitted);
        session.RemoveStation(SectionDraftTests.Id(), 1);
        Equal(Evaluator.V2, session.Envelope().Designs[^1].Evaluator);
        Equal(Evaluator.V2, session.InspectAccepted().Authored.Binding.Evaluator);
        // Prior v3 design row was never rewritten!
        Equal(Evaluator.V3, session.Envelope().Designs[0].Evaluator);
    }

    // ---- CF-4: Promote R3 on copies, regime measured ----------------------------------------------------------------

    private static void Promote_R3_OnCopies_RegimeMeasured()
    {
        byte[] lp2 = LP2Fixture(); // Root 8-pt section-a, Tip 10-pt NACA 0012 (basis -> S* = Tip's 10-pt spacing).
        using var session = new AuthoringSession { StationGrid = FastGrid };
        session.Open(lp2, SectionDraftTests.Id(), true);
        var preview = session.PreviewPromote(0.5, "50 %");
        Equal(null, preview.RefusalCode);
        Equal(true, preview.Admitted);
        True(preview.Regime is "R1" or "R2", $"Unexpected R3 regime '{preview.Regime}'");
        True(preview.DeviationMeters <= StationEdits.LimitMeters, $"R3 promote deviation {preview.DeviationMeters * 1e6:F2} um > 10 um");

        var outcome = session.AddStation(SectionDraftTests.Id(), 0.5, "50 %", approximate: true);
        var afterDef = FoilSource.Parse(session.Snapshot().Source).Definition!;
        Equal(3, afterDef.Assignments.Length);
        var promoted = afterDef.Profiles[afterDef.Assignments[1].Profile];
        var tip = afterDef.Profiles[afterDef.Assignments[2].Profile]; // S* is Tip's 10-pt spacing
        Equal(true, Placement.SameAbscissa(promoted.Upper, tip.Upper));
        Equal(true, Placement.SameAbscissa(promoted.Lower, tip.Lower));
        Equal(Evaluator.V3, session.Envelope().Designs[^1].Evaluator);
        Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(session.Snapshot().Source)).Status);
        _ = outcome;
    }

    // ---- CF-4 (C3): Remove widens measured intervals to every interval touching a bitwise-changed copy --------------

    private static void Remove_CopyChanges_MeasuresTouchedIntervals()
    {
        // Build a 4-station mixed-spacing wing:
        //   Station 0 (0 %):   6-pt sqrt
        //   Station 1 (30 %):  10-pt NACA (BASIS!) — when removed, Station 2 (8-pt) becomes the new basis!
        //   Station 2 (70 %):  8-pt section-a (3 spans)
        //   Station 3 (100 %): 6-pt sqrt (FAR from the removed station 1's gap [0, 0.7]!)
        // Removing station 1 changes S* from 10-pt to 8-pt, so station 3's copy changes bitwise -> far interval [0.7, 1.0] MUST be measured!
        var baseDef = FoilSource.Parse(FoilSource.NewDefault()).Definition!;
        var naca10 = baseDef.Profiles[0];
        var sectionA = FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example))).Definition!.Profiles[0];
        var (k6, x6) = DatImport.OwnSqrtBasis(6);
        var fit6 = DatImport.FitToBasis(SampleDat(sectionA), k6, x6, 5, true)!;
        var tip6 = new ProfileDefinition("p6",
            naca10.Upper with { Knots = k6, Points = Points(x6, fit6.Upper), Ids = Ids(6) },
            naca10.Lower with { Knots = k6, Points = Points(x6, fit6.Lower), Ids = Ids(6) },
            "closed", 0, 0);
        var mid8 = new ProfileDefinition("p8",
            sectionA.Upper with { Ids = Ids(sectionA.Upper.Points.Length) },
            sectionA.Lower with { Ids = Ids(sectionA.Lower.Points.Length) },
            sectionA.Closure, 0, 0);

        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var sb = new StringBuilder("  profiles {\n");
        FoilSource.AppendProfile(sb, tip6);
        FoilSource.AppendProfile(sb, naca10);
        FoilSource.AppendProfile(sb, mid8);
        sb.Append("  }\n  sections { at root profile \"p6\" at 30 % profile \"naca-0012\" at 70 % profile \"p8\" at tip profile \"p6\" }\n");
        byte[] doc4 = Encoding.UTF8.GetBytes(text[..start] + sb + text[end..]);

        var removeBasis = StationEdits.Remove(doc4, 1, FastGrid);
        // Adjacent intervals [0, 0.3], [0.3, 0.7] PLUS far interval [0.7, 1.0] = 3 intervals!
        Equal(3 * FastGrid.EtaSamples, removeBasis.Preview.EtaCount);
        True(removeBasis.Preview.MeasuredIntervals is { Count: 3 }, "Expected 3 measured intervals (2 adjacent + 1 far)");
        Equal(0.7, removeBasis.Preview.MeasuredIntervals![2].EtaStart);
        Equal(1.0, removeBasis.Preview.MeasuredIntervals[2].EtaEnd);

        // Control (kills "always measure all intervals" or "keyed on S* when no copy changed"):
        // Put the 10-pt basis at Root (station 0), non-basis 8-pt at station 1 (30 %), non-basis 6-pt at station 2 (70 %) & station 3 (100 %).
        // Removing station 1 (8-pt, no anchors) leaves S* = Root's 10-pt unchanged -> NO surviving copy changes bitwise -> ONLY 2 adjacent intervals!
        var sb2 = new StringBuilder("  profiles {\n");
        FoilSource.AppendProfile(sb2, naca10);
        FoilSource.AppendProfile(sb2, mid8);
        FoilSource.AppendProfile(sb2, tip6);
        sb2.Append("  }\n  sections { at root profile \"naca-0012\" at 30 % profile \"p8\" at 70 % profile \"p6\" at tip profile \"p6\" }\n");
        byte[] docNoChange = Encoding.UTF8.GetBytes(text[..start] + sb2 + text[end..]);

        var removeNonBasis = StationEdits.Remove(docNoChange, 1, FastGrid);
        Equal(2 * FastGrid.EtaSamples, removeNonBasis.Preview.EtaCount);
        True(removeNonBasis.Preview.MeasuredIntervals is { Count: 2 }, "Expected only 2 adjacent intervals when no surviving copy changed");
    }

    // ---- CF-4 (C4): Promote between L and R sharing their own spacing inside a mixed document keeps L's spacing -----

    private static void Promote_SharedSpacingInMixedDoc_KeepsLSpacing()
    {
        // Mixed 3-station wing:
        //   Station 0 (0 %):   section-a (8-pt, 3 spans)
        //   Station 1 (50 %):  section-a-cambered (SAME 8-pt spacing as station 0, different ordinates)
        //   Station 2 (100 %): NACA 0012 (10-pt, 5 spans -> BASIS, so S* is 10-pt!)
        // Promoting at 25 % (between station 0 and station 1, which share 8-pt spacing) MUST keep L's 8-pt spacing bitwise (NOT S* 10-pt)!
        var baseDef = FoilSource.Parse(FoilSource.NewDefault()).Definition!;
        var naca10 = baseDef.Profiles[0];
        var sectionA = FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example))).Definition!.Profiles[0];
        var p0 = new ProfileDefinition("s8-root",
            sectionA.Upper with { Ids = Ids(sectionA.Upper.Points.Length) },
            sectionA.Lower with { Ids = Ids(sectionA.Lower.Points.Length) },
            sectionA.Closure, 0, 0);
        var uPts1 = sectionA.Upper.Points.Select((pt, i) => new[] { pt[0], i == 0 || i == sectionA.Upper.Points.Length - 1 ? pt[1] : pt[1] + 0.008 * 4 * pt[0] * (1 - pt[0]) }).ToArray();
        var lPts1 = sectionA.Lower.Points.Select((pt, i) => new[] { pt[0], i == 0 || i == sectionA.Lower.Points.Length - 1 ? pt[1] : pt[1] + 0.008 * 4 * pt[0] * (1 - pt[0]) }).ToArray();
        var p1 = new ProfileDefinition("s8-mid",
            sectionA.Upper with { Points = uPts1, Ids = Ids(uPts1.Length) },
            sectionA.Lower with { Points = lPts1, Ids = Ids(lPts1.Length) },
            sectionA.Closure, 0, 0);

        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var sb = new StringBuilder("  profiles {\n");
        FoilSource.AppendProfile(sb, p0);
        FoilSource.AppendProfile(sb, p1);
        FoilSource.AppendProfile(sb, naca10);
        sb.Append("  }\n  sections { at root profile \"s8-root\" at 50 % profile \"s8-mid\" at tip profile \"naca-0012\" }\n");
        byte[] mixedDoc = Encoding.UTF8.GetBytes(text[..start] + sb + text[end..]);

        // Confirm it really is a mixed document whose S* is 10-pt (5 spans).
        var compat = Compatible.Of(FoilSource.Parse(mixedDoc).Definition!);
        Equal(true, compat.NeedsFit);
        Equal("naca-0012", compat.BasisProfile);
        Equal(5, compat.SharedSpans);

        using var session = new AuthoringSession { StationGrid = FastGrid };
        session.Open(mixedDoc, SectionDraftTests.Id(), true);
        var preview = session.PreviewPromote(0.25, "25 %");
        Equal(null, preview.RefusalCode);
        Equal(true, preview.Admitted);
        session.AddStation(SectionDraftTests.Id(), 0.25, "25 %", approximate: true);

        var afterDef = FoilSource.Parse(session.Snapshot().Source).Definition!;
        var promoted = afterDef.Profiles[afterDef.Assignments[1].Profile];
        // P MUST keep L's 8-point knots and control x bitwise (NOT 10-point S*)!
        Equal(8, promoted.Upper.Points.Length);
        Equal(true, Placement.SameAbscissa(promoted.Upper, p0.Upper));
        Equal(true, Placement.SameAbscissa(promoted.Lower, p0.Lower));
        Equal(false, Placement.SameAbscissa(promoted.Upper, naca10.Upper));
    }

    // ---- Test 22: Readiness cost check on five-station 32-vertex fixture --------------------------------------------

    private static void Readiness_CompatibleCheck_FiveSectionsUnder1s()
    {
        // Five-station 32-vertex mixed-spacing fixture (cross-profile-abscissa.md §3.4 & §10 test 22).
        var baseDef = FoilSource.Parse(FoilSource.NewDefault()).Definition!;
        var baseProfile = baseDef.Profiles[0];
        var (k32, x32) = DatImport.OwnSqrtBasis(32);
        var fit32 = DatImport.FitToBasis(SampleDat(baseProfile), k32, x32, 5, true)!;
        var basis32 = new ProfileDefinition("s0",
            baseProfile.Upper with { Knots = k32, Points = Points(x32, fit32.Upper), Ids = Ids(32) },
            baseProfile.Lower with { Knots = k32, Points = Points(x32, fit32.Lower), Ids = Ids(32) },
            "closed", 0, 0);

        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var sb = new StringBuilder("  profiles {\n");
        FoilSource.AppendProfile(sb, basis32);
        int[] counts = [28, 24, 20, 16];
        string[] etas = ["root", "25 %", "50 %", "75 %", "tip"];
        for (int i = 0; i < counts.Length; i++)
        {
            var (ki, xi) = DatImport.OwnSqrtBasis(counts[i]);
            var fi = DatImport.FitToBasis(SampleDat(baseProfile), ki, xi, 5, true)!;
            var pi = new ProfileDefinition("s" + (i + 1),
                baseProfile.Upper with { Knots = ki, Points = Points(xi, fi.Upper), Ids = Ids(counts[i]) },
                baseProfile.Lower with { Knots = ki, Points = Points(xi, fi.Lower), Ids = Ids(counts[i]) },
                "closed", 0, 0);
            FoilSource.AppendProfile(sb, pi);
        }
        sb.Append("  }\n  sections {");
        for (int i = 0; i < etas.Length; i++) sb.Append($" at {etas[i]} profile \"s{i}\"");
        sb.Append(" }\n");
        byte[] fiveStation = Encoding.UTF8.GetBytes(text[..start] + sb + text[end..]);
        var parsed = FoilSource.Parse(fiveStation);

        // Warm JIT once, then measure a cold-memo Compatible.Of (fit + record measure for 5 sections up to 32 vertices).
        _ = Compatible.Of(parsed.Definition!);
        Compatible.ClearMemos();
        var watch = Stopwatch.StartNew();
        var compat = Compatible.Of(parsed.Definition!);
        watch.Stop();
        Console.WriteLine($"MEASURE readiness_cf_five_sections_32v_ms={watch.Elapsed.TotalMilliseconds:F1}");
        Equal(false, compat.Failed);
        Equal(5, compat.Matches.Count);
        True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Five-section compatible fit took {watch.Elapsed.TotalMilliseconds:F1} ms (> 5 s)");
    }

    // ---- Helpers ----------------------------------------------------------------------------------------------------

    private static string ProfileBlock(byte[] source, string name)
    {
        string text = Encoding.UTF8.GetString(source);
        int keyword = text.IndexOf("profile \"" + name + "\" {", StringComparison.Ordinal);
        int open = text.IndexOf('{', keyword);
        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return text[keyword..(i + 1)]; }
        }
        throw new InvalidOperationException("Unclosed profile block " + name);
    }

    private static void SameCurveBits(Curve expected, Curve actual)
    {
        Equal(expected.Degree, actual.Degree);
        Equal(expected.Knots.Length, actual.Knots.Length);
        Equal(expected.Points.Length, actual.Points.Length);
        for (int i = 0; i < expected.Knots.Length; i++)
            Equal(BitConverter.DoubleToInt64Bits(expected.Knots[i]), BitConverter.DoubleToInt64Bits(actual.Knots[i]));
        for (int i = 0; i < expected.Points.Length; i++)
        {
            Equal(BitConverter.DoubleToInt64Bits(expected.Points[i][0]), BitConverter.DoubleToInt64Bits(actual.Points[i][0]));
            Equal(BitConverter.DoubleToInt64Bits(expected.Points[i][1]), BitConverter.DoubleToInt64Bits(actual.Points[i][1]));
        }
    }

    private static double Outside(PlacedPointEnclosure box, Point3 point) =>
        Math.Max(Outside(box.X.Lower, box.X.Upper, point.X),
            Math.Max(Outside(box.Y.Lower, box.Y.Upper, point.Y), Outside(box.Z.Lower, box.Z.Upper, point.Z)));

    private static double Outside(double lower, double upper, double value) =>
        value < lower ? lower - value : value > upper ? value - upper : 0d;

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static bool IsInsideType(Type? candidate, Type root)
    {
        for (var current = candidate; current is not null; current = current.DeclaringType)
            if (current == root) return true;
        return false;
    }

    private static List<MethodBase> FindIlCallers(Assembly assembly, MethodInfo target)
    {
        var callers = new List<MethodBase>();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(flags))
                if (CallsMethod(method, target)) callers.Add(method);
            foreach (var ctor in type.GetConstructors(flags))
                if (CallsMethod(ctor, target)) callers.Add(ctor);
        }
        return callers;
    }

    private static readonly Dictionary<short, OpCode> SingleByteOpCodes = BuildOpCodes(single: true);
    private static readonly Dictionary<short, OpCode> MultiByteOpCodes = BuildOpCodes(single: false);

    private static Dictionary<short, OpCode> BuildOpCodes(bool single)
    {
        var map = new Dictionary<short, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode op) continue;
            if (single && op.Size == 1) map[op.Value] = op;
            else if (!single && op.Size == 2) map[op.Value] = op;
        }
        return map;
    }

    private static bool CallsMethod(MethodBase caller, MethodInfo target)
    {
        MethodBody? body;
        try { body = caller.GetMethodBody(); }
        catch { return false; }
        byte[]? il = body?.GetILAsByteArray();
        if (il is null || il.Length == 0) return false;
        var module = caller.Module;
        int pos = 0;
        while (pos < il.Length)
        {
            byte first = il[pos++];
            OpCode op;
            if (first == 0xFE)
            {
                short code = (short)((first << 8) | il[pos++]);
                if (!MultiByteOpCodes.TryGetValue(code, out op)) return false;
            }
            else if (!SingleByteOpCodes.TryGetValue(first, out op)) return false;

            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: pos += 1; break;
                case OperandType.InlineVar: pos += 2; break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR: pos += 4; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: pos += 8; break;
                case OperandType.InlineSwitch:
                    int count = BitConverter.ToInt32(il, pos);
                    pos += 4 + count * 4;
                    break;
                case OperandType.InlineMethod:
                    int token = BitConverter.ToInt32(il, pos);
                    pos += 4;
                    try
                    {
                        var resolved = module.ResolveMethod(token, caller.DeclaringType?.GetGenericArguments(),
                            caller is MethodInfo mi ? mi.GetGenericArguments() : null);
                        if (resolved is MethodInfo called && called.MetadataToken == target.MetadataToken && called.Module == target.Module)
                            return true;
                    }
                    catch { }
                    break;
            }
        }
        return false;
    }
}
