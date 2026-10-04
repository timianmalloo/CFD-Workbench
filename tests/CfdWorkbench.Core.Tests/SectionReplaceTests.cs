using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// The RPL rows of docs/design/m12d-catalog.md §12.4: the Replace step (§3.6, DR-M12D-1 A + B, Ruling 72).
internal static class SectionReplaceTests
{
    private const double ExampleChord = 0.120;
    private const double Limit = 10e-6;

    internal static void Run()
    {
        Check("ImportDat_SharedExample_OwnSpacingNotUnsupported", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Import(Selig(Naca4("4412"))));
            Equal("replace", view.Last!.Kind);
            Equal("own", view.Last.Import!.Basis);
            Equal(SectionScope.Shared, view.Scope);
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            Equal(GeometryStatus.Certified, assessment.Status);
        });
        Check("ImportDat_AfterMakeUnique_NoDslPatch", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            int profiles = Definition(view.Bytes).Profiles.Length;
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Import(Selig(Naca4("0012"))));
            Equal(profiles, Definition(view.Bytes).Profiles.Length);
            Equal("current", view.Last!.Import!.Basis);
            Equal(GeometryStatus.Certified, session.AssessSection(id, view.Generation, CancellationToken.None).Status);
        });
        Check("Replace_SharedExample4412_OwnSpacing15PointsCertified", () =>
        {
            var preview = Preview(Example(), 0, Gen("4412"));
            Equal(null, preview.RefusalCode);
            Equal("own-15", preview.Spacing);
            Equal(15, preview.PointsPerSurface);
            Equal(true, preview.FitResidual * ExampleChord <= Limit);
            Console.WriteLine(FormattableString.Invariant($"RPL 4412 own-15 residual {preview.FitResidual * ExampleChord * 1e6:F2} um; largest change {preview.LargestChangeChord * ExampleChord * 1e3:F2} mm at x {preview.LargestChangeAtX:F3}"));
            Equal(GeometryStatus.Certified, Certificate(preview.Bytes!));
        });
        Check("Replace_SharedExampleSymmetric_KeepsCurrentSpacingAndPoints", () =>
        {
            byte[] example = Example();
            var preview = Preview(example, 0, Gen("0012"));
            Equal("current", preview.Spacing);
            Equal(8, preview.PointsPerSurface);
            Equal(true, preview.FitResidual * ExampleChord <= Limit);
            Console.WriteLine(FormattableString.Invariant($"RPL 0012 current residual {preview.FitResidual * ExampleChord * 1e6:F2} um (design P-CF-0012: 9.75 um one-way)"));
            var before = Profile(example, "section-a");
            var after = Profile(preview.Bytes!, "section-a");
            Equal(true, before.Upper.Knots.SequenceEqual(after.Upper.Knots));
            Equal(true, before.Upper.Points.Select(p => p[0]).SequenceEqual(after.Upper.Points.Select(p => p[0])));
            Equal(GeometryStatus.Certified, Certificate(preview.Bytes!));
        });
        Check("Replace_NoOwnSpacingWithin10um_DisabledBestNumber", () =>
        {
            var preview = Preview(Example(), 0, Coordinates("Bump", BumpSelig()));
            Equal("CAT-RESIDUAL", preview.RefusalCode);
            Equal(null, preview.Bytes);
            Equal(true, double.IsFinite(preview.FitResidual) && preview.FitResidual * ExampleChord > Limit);
            string micrometres = (preview.FitResidual * ExampleChord * 1e6).ToString("0.00", CultureInfo.InvariantCulture);
            Equal(true, preview.RefusalReason!.Contains(micrometres + " µm", StringComparison.Ordinal));
        });
        Check("Replace_AfterMakeUnique_NoOrphanParses", () =>
        {
            byte[] unique = FoilSource.MakeIndependent(Example(), "section-a", 0).Source;
            var preview = Preview(unique, 0, Gen("0012"));
            var definition = Definition(preview.Bytes!);
            Equal(2, definition.Profiles.Length);
            Equal(true, Enumerable.Range(0, definition.Profiles.Length).All(index => definition.Assignments.Any(item => item.Profile == index)));
        });
        Check("Replace_SharedKeepsName_ScopeStaysShared", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Replace(Gen("0012"), ReplaceScope.Draft));
            Equal("section-a", view.Profile);
            Equal(SectionScope.Shared, view.Scope);
            Equal("section-a", SectionDraftTests.Assigned(view.Bytes, 1));
        });
        Check("Replace_UniqueRootSymmetric_FitsCurrentSpacingKeepsPoints", () =>
        {
            byte[] unique = FoilSource.MakeIndependent(Example(), "section-a", 0).Source;
            var preview = Preview(unique, 0, Gen("0012"));
            Equal(null, preview.RefusalCode);
            Equal("current", preview.Spacing);
            var before = Profile(unique, "section-a-i1");
            var after = Profile(preview.Bytes!, "section-a-i1");
            Equal(true, before.Upper.Ids.SequenceEqual(after.Upper.Ids));
            Equal(true, before.Upper.Points.Select(p => p[0]).SequenceEqual(after.Upper.Points.Select(p => p[0])));
            Equal(GeometryStatus.Certified, Certificate(preview.Bytes!));
        });
        Check("Replace_UniqueRootCambered_RefusedCatSpacingNothingChanged", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            var preview = session.PreviewReplace(id, view.Generation, Gen("4412"), ReplaceScope.Draft);
            Equal("CAT-SPACING", preview.RefusalCode);
            Equal(true, preview.BlendChain!.SequenceEqual([0, 1]));
            Equal(true, preview.RefusalReason!.StartsWith("Root blends point-to-point with Tip, so Root must keep Tip's 8 points", StringComparison.Ordinal));
            var refused = Throws(() => session.ApplySectionStep(id, view.Generation, new SectionStep.Replace(Gen("4412"), ReplaceScope.Draft)));
            Equal("CAT-SPACING", refused.Code);
            Equal(preview.RefusalReason, refused.Reason);
            var draft = session.CurrentSectionDraft()!;
            Equal(view.Generation, draft.Generation);
            SectionDraftTests.Same(view.Bytes, draft.Bytes);
        });
        Check("Replace_BlendChain_AllStationsReplacedCertified", () =>
        {
            byte[] unique = FoilSource.MakeIndependent(Example(), "section-a", 0).Source;
            var preview = SectionReplace.Preview(unique, 0, SectionScope.Independent, Gen("4412"), ReplaceScope.BlendChain);
            Equal(null, preview.RefusalCode);
            Equal(true, preview.Stations.SequenceEqual([0, 1]));
            var definition = Definition(preview.Bytes!);
            Equal(1, definition.Profiles.Length);
            Equal("section-a-i1", definition.Profiles[0].Name);
            Equal(GeometryStatus.Certified, Certificate(preview.Bytes!));
        });
        Check("Replace_ChainOwnFifteen_DisclosesCount", () =>
        {
            byte[] unique = FoilSource.MakeIndependent(Example(), "section-a", 0).Source;
            var preview = SectionReplace.Preview(unique, 0, SectionScope.Independent, Gen("4412"), ReplaceScope.BlendChain);
            Equal(15, preview.PointsPerSurface);
            Equal(15, preview.Report!.VertexCount);
            Equal("own", preview.Report.Basis);
        });
        Check("Replace_AcceptanceUsesLargestReplacedChord", () =>
        {
            // Measure 0012 on the Example's spacing, then taper the foil so the root chord is over and the tip chord is under.
            double residual = Preview(Example(), 0, Gen("0012")).FitResidual;
            double root = 1.5 * Limit / residual, tip = 0.5 * Limit / residual;
            var preview = Preview(WithChords(root, tip), 0, Gen("0012"));
            Equal(true, Math.Abs(preview.AcceptanceChord - root) < 1e-12);
            Equal(false, preview.Spacing == "current");
        });
        Check("Replace_ResidualJustOver10um_Refused", () =>
        {
            byte[] unique = FoilSource.MakeIndependent(Example(), "section-a", 0).Source;
            double residual = Preview(unique, 0, Gen("0012")).FitResidual;
            double chord = 10.01e-6 / residual;
            var preview = Preview(FoilSource.MakeIndependent(WithChords(chord, chord), "section-a", 0).Source, 0, Gen("0012"));
            Equal("CAT-SPACING", preview.RefusalCode);
            Equal(true, preview.RefusalReason!.Contains("10.01 µm", StringComparison.Ordinal));
        });
        Check("Replace_ResidualJustUnder10um_Accepted", () =>
        {
            byte[] unique = FoilSource.MakeIndependent(Example(), "section-a", 0).Source;
            double residual = Preview(unique, 0, Gen("0012")).FitResidual;
            double chord = 9.99e-6 / residual;
            var preview = Preview(FoilSource.MakeIndependent(WithChords(chord, chord), "section-a", 0).Source, 0, Gen("0012"));
            Equal(null, preview.RefusalCode);
            Equal("current", preview.Spacing);
        });
        Check("Replace_MySectionsEntrySameSpacing_ExactCopy", () =>
        {
            var entry = Profile(Example(), "section-a");
            double[][] Scaled(Curve curve) => curve.Points.Select(p => new[] { p[0], p[1] * 1.25 }).ToArray();
            byte[] document = Encoding.UTF8.GetBytes("foildsl \"4.1\"\nsection \"mine\" {\n  evaluator \"cfdw-cv\" \"2\"\n" +
                "  upper cv { " + CurveText(entry.Upper.Knots, Scaled(entry.Upper)) + " }\n" +
                "  lower cv { " + CurveText(entry.Lower.Knots, Scaled(entry.Lower)) + " }\n}\n");
            var preview = Preview(FoilSource.MakeIndependent(Example(), "section-a", 0).Source, 0,
                new ReplaceSource.Record("Mine", new Provenance("lib:mine", false), document));
            Equal("exact", preview.Spacing);
            Equal(0.0, preview.FitResidual);
            var after = Profile(preview.Bytes!, "section-a-i1");
            Equal(true, after.Upper.Points.Select(p => p[1]).SequenceEqual(Scaled(entry.Upper).Select(p => p[1])));
        });
        Check("Replace_VendEntry_NoCoordinatesNothingChanges", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            var vend = new ReplaceSource.Coordinates("Eppler 817", new Provenance("vend:eppler/e817", false), []);
            Refuses("CAT-NOT-ADMITTED", () => session.PreviewReplace(id, view.Generation, vend, ReplaceScope.Draft));
            Refuses("CAT-NOT-ADMITTED", () => session.ApplySectionStep(id, view.Generation, new SectionStep.Replace(vend, ReplaceScope.Draft)));
            var draft = session.CurrentSectionDraft()!;
            Equal(view.Generation, draft.Generation);
            SectionDraftTests.Same(view.Bytes, draft.Bytes);
        });
        Check("Replace_InnerUndo_RestoresBytesAndChip", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var opened = session.BeginSectionDraft(id, 0);
            var replaced = session.ApplySectionStep(id, opened.Generation, new SectionStep.Replace(Gen("0012"), ReplaceScope.Draft));
            Equal(true, ProvenanceLine(replaced.Bytes) == "gen:naca4-closed/1:0012");
            var undone = session.UndoSectionStep(id);
            SectionDraftTests.Same(opened.Bytes, undone.Bytes);
            Equal(null, ProvenanceLine(undone.Bytes));
        });
        Check("Replace_InnerRedo_ReappliesSource", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var opened = session.BeginSectionDraft(id, 0);
            var replaced = session.ApplySectionStep(id, opened.Generation, new SectionStep.Replace(Gen("0012"), ReplaceScope.Draft));
            session.UndoSectionStep(id);
            var redone = session.RedoSectionStep(id);
            SectionDraftTests.Same(replaced.Bytes, redone.Bytes);
            Equal(replaced.Last, redone.Last);
        });
        Check("Replace_CancelSection_AssignmentsAtEntry", () =>
        {
            using var session = SectionDraftTests.Opened();
            byte[] entry = session.Snapshot().Source;
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Replace(Gen("4412"), ReplaceScope.BlendChain));
            Equal("section-a-i1", SectionDraftTests.Assigned(view.Bytes, 1));
            session.Cancel(id);
            Equal(null, session.CurrentSectionDraft());
            SectionDraftTests.Same(entry, session.Snapshot().Source);
            Equal("section-a", SectionDraftTests.Assigned(session.Snapshot().Source, 0));
            Equal("section-a", SectionDraftTests.Assigned(session.Snapshot().Source, 1));
        });
        Check("Replace_FinishSection_OneUndoStep", () =>
        {
            using var session = SectionDraftTests.Opened();
            byte[] entry = session.Snapshot().Source;
            int rows = session.Envelope().Accepted.Length;
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Replace(Gen("0012"), ReplaceScope.Draft));
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, session.FinishSection(SectionDraftTests.Id(), assessment) is not null);
            Equal(rows + 1, session.Envelope().Accepted.Length);
            session.Undo(SectionDraftTests.Id());
            SectionDraftTests.Same(entry, session.Snapshot().Source);
        });
        Check("Replace_StalePreviewAfterTypingOrStep_Refused", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            var preview = session.PreviewReplace(id, view.Generation, Gen("0012"), ReplaceScope.Draft);
            Equal(null, preview.RefusalCode);
            var moved = session.ApplySectionStep(id, view.Generation, SectionDraftTests.Raise(view, "cv-3", 0.002));
            Refuses("DSL-CONFLICT", () => session.ApplySectionStep(id, view.Generation, new SectionStep.Replace(Gen("0012"), ReplaceScope.Draft)));
            Refuses("DSL-CONFLICT", () => session.PreviewReplace(id, view.Generation, Gen("0012"), ReplaceScope.Draft));
            SectionDraftTests.Same(moved.Bytes, session.CurrentSectionDraft()!.Bytes);
        });
        Check("Replace_Preview_EmitsCatalogPreviewOutcome", () =>
        {
            using var session = SectionDraftTests.Opened();
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            var accepted = session.PreviewReplace(id, view.Generation, Gen("0012"), ReplaceScope.Draft);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            session.PreviewReplace(id, view.Generation, Gen("4412"), ReplaceScope.Draft);
            var events = session.ReadLocalEvents().Where(item => item.Operation == "catalog.preview").ToArray();
            Equal(2, events.Length);
            Equal("ok", events[0].Outcome);
            Equal("current", events[0].StepKind);
            Equal(8, events[0].PointsAfter);
            Equal(false, events[0].FitAboveLimit);
            Equal(true, Math.Abs(events[0].FitMicrometres!.Value - accepted.FitResidual * ExampleChord * 1e6) < 1e-9);
            Equal(true, events[0].DurationMilliseconds is >= 0);
            Equal("cat-spacing", events[1].Outcome);
            Equal(true, events[1].FitAboveLimit);
        });
        Check("SectionEdits_ReplaceStep_NeverApplied", () =>
            Refuses("DSL-PATCH", () => SectionEdits.Apply(Example(), 0, new SectionStep.Replace(Gen("0012"), ReplaceScope.Draft))));
        Check("Guard_FivePiecesDiffering_Certifies", () =>
        {
            // New foil: 10 points, single interior knots = 5 pieces per surface, the limit.
            using var session = new AuthoringSession();
            session.Open(FoilSource.NewDefault(), SectionDraftTests.Id(), true);
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            Equal(10, view.Last!.UpperPoints);
            view = session.ApplySectionStep(id, view.Generation, SectionDraftTests.Raise(view, MiddleId(view), 0.002));
            Equal(GeometryStatus.Certified, session.AssessSection(id, view.Generation, CancellationToken.None).Status);
        });
        Check("Guard_SixPiecesDiffering_RefusedCopy194", () =>
        {
            using var session = new AuthoringSession();
            session.Open(FoilSource.NewDefault(), SectionDraftTests.Id(), true);
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Insert(SurfaceSide.Upper, 0.62));
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            Equal(11, view.Last!.UpperPoints);
            var refused = Throws(() => session.ApplySectionStep(id, view.Generation, SectionDraftTests.Raise(view, MiddleId(view), 0.002)));
            Equal("DSL-GEOMETRY", refused.Code);
            Equal("Root and Tip have 11 points; neighbouring sections that differ can have at most 10. Rebuild to 10 points first, or edit Root and Tip together.",
                refused.Reason);
            SectionDraftTests.Same(view.Bytes, session.CurrentSectionDraft()!.Bytes);
        });
        Check("Replace_ThreeStationsEveryStation_OneBlockCertified", () =>
        {
            string text = Encoding.UTF8.GetString(Example()).Replace("sections { at root profile \"section-a\" at tip profile \"section-a\" }",
                "sections { at root profile \"section-a\" at 50 % profile \"section-a\" at tip profile \"section-a\" }", StringComparison.Ordinal);
            byte[] tipUnique = FoilSource.MakeIndependent(Encoding.UTF8.GetBytes(text), "section-a", 2).Source;
            var tip = Profile(tipUnique, "section-a-i1");
            byte[] tipY = FoilSource.PatchProfilePoint(tipUnique, "section-a-i1", "upper", tip.Upper.Ids[3], tip.Upper.Points[3][0], tip.Upper.Points[3][1] + 0.004);
            var preview = SectionReplace.Preview(tipY, 2, SectionScope.Independent, Gen("4412"), ReplaceScope.BlendChain);
            Equal(null, preview.RefusalCode);
            Equal(true, preview.Stations.SequenceEqual([0, 1, 2]));
            var definition = Definition(preview.Bytes!);
            Equal(1, definition.Profiles.Length);
            Equal(true, definition.Assignments.All(item => item.Profile == 0));
            Equal(GeometryStatus.Certified, Certificate(preview.Bytes!));
        });
        Check("Replace_ResidualEuclidean201PlusKnots_NotVerticalGap", () =>
        {
            // A fitted 0012 against the same shape slid aft by δ (x + δ(1 − x)): the true distance is at most δ, but at the
            // vertical nose the gap at equal x is many times larger (geometry lens F7: 94 µm vertical vs 7 µm true).
            const double delta = 1e-4;
            var source = DatImport.ParseInChordFrame(Selig(Naca4("0012"))).Profile;
            var (knots, x) = DatImport.OwnSqrtBasis(16);
            var fit = DatImport.FitToBasis(source, knots, x, 5, true)!;
            var slid = new DatProfile("slid", "test", Slide(source.Upper, delta), Slide(source.Lower, delta), [], 0);
            var (upperCurve, lowerCurve) = DatImport.SourceCurve(slid);
            double euclidean = Math.Max(DatImport.EuclideanResidual(slid.Upper, upperCurve, knots, 5, x, fit.Upper),
                DatImport.EuclideanResidual(slid.Lower, lowerCurve, knots, 5, x, fit.Lower));
            double vertical = Math.Max(VerticalGap(slid.Upper, knots, x, fit.Upper), VerticalGap(slid.Lower, knots, x, fit.Lower));
            Console.WriteLine(FormattableString.Invariant($"RPL slid 0012 euclidean {euclidean * ExampleChord * 1e6:F2} um; vertical {vertical * ExampleChord * 1e6:F2} um"));
            Equal(true, euclidean <= delta * 1.01 && euclidean >= delta * 0.5);
            Equal(true, vertical > 5 * euclidean);
            // Both ways: a bulge between sparse source samples, which the samples cannot see, is still measured.
            var bulged = fit.Upper.ToArray();
            bulged[8] += 1e-3;
            var sparse = source.Upper.Where((_, index) => index % 50 == 0).ToList();
            var (sourceUpper, _) = DatImport.SourceCurve(source);
            double withBulge = DatImport.EuclideanResidual(sparse, sourceUpper, knots, 5, x, bulged);
            double seenBySamples = OneWay(sparse, knots, x, bulged), truth = OneWay(source.Upper, knots, x, bulged);
            Console.WriteLine(FormattableString.Invariant($"RPL bulge both-ways {withBulge:E3}; sparse one-way {seenBySamples:E3}; dense one-way {truth:E3}"));
            Equal(true, seenBySamples < 0.5 * truth && withBulge >= 0.95 * truth);
        });
        Check("Replace_CurrentSpacing_KeepsIdsAndTangentRows", () =>
        {
            byte[] anchored = HorizontalAnchor();
            var before = Profile(anchored, "section-a");
            var preview = Preview(anchored, 0, Coordinates("Thicker", SampledSelig(before, 1.05)));
            Equal("current", preview.Spacing);
            var after = Profile(preview.Bytes!, "section-a");
            Equal(true, before.Upper.Ids.SequenceEqual(after.Upper.Ids));
            Equal(true, before.Lower.Ids.SequenceEqual(after.Lower.Ids));
            Equal(true, before.Upper.Tangents.SequenceEqual(after.Upper.Tangents));
            Equal(true, before.Lower.Tangents.SequenceEqual(after.Lower.Tangents));
            Equal(0, preview.Report!.DroppedRows!.Count);
            Equal(GeometryStatus.Certified, Certificate(preview.Bytes!));
        });
        Check("Replace_OwnSpacing_ListsDroppedRows", () =>
        {
            byte[] anchored = HorizontalAnchor();
            var before = Profile(anchored, "section-a");
            var preview = Preview(anchored, 0, Gen("4412"));
            Equal(true, preview.Spacing.StartsWith("own-", StringComparison.Ordinal));
            Equal(true, preview.Report!.DroppedRows!.SequenceEqual(before.Upper.Tangents.Select(row => row.Id).Concat(before.Lower.Tangents.Select(row => row.Id)).Distinct()));
            var after = Profile(preview.Bytes!, "section-a");
            Equal(0, after.Upper.Tangents.Length + after.Lower.Tangents.Length);
        });
        Check("Replace_SourceThicknessDiffers_ReportsScaledTc", () =>
        {
            var preview = Preview(Example(), 0, Gen("0009"));
            double source = preview.Report!.SourceThickness!.Value;
            Equal(true, Math.Abs(source - 0.09) < 1e-3);
            double station = Definition(preview.Bytes!).Curves["thickness"].Points[0][1];
            Equal(0.12, station);
            Equal(true, station - source > 0.02);
        });
        Check("Replace_Frame_ReportsLeShiftRotationScale", () =>
        {
            var plain = Preview(Example(), 0, Gen("0012"));
            const double scale = 2, degrees = 0.2, shiftX = 0.5, shiftY = 0.2;
            var moved = Preview(Example(), 0, Coordinates("NACA 0012 moved", Selig(Naca4("0012"), scale, degrees, shiftX, shiftY)));
            Equal(true, Math.Abs(moved.Report!.FrameScale!.Value - scale) < 1e-9);
            Equal(true, Math.Abs(moved.Report.FrameRotationDegrees!.Value - degrees) < 1e-9);
            Equal(true, Math.Abs(moved.Report.FrameLeShift!.Value - Math.Sqrt(shiftX * shiftX + shiftY * shiftY) / scale) < 1e-9);
            Equal(true, Math.Abs(moved.FitResidual - plain.FitResidual) < 1e-9);
            Equal(0.0, plain.Report!.FrameLeShift);
            Equal(0.0, plain.Report.FrameRotationDegrees);
            Equal(1.0, plain.Report.FrameScale);
        });
    }

    // ---- fixtures ----

    private static byte[] Example() => FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));

    private static ReplacePreview Preview(byte[] bytes, int assignment, ReplaceSource source)
    {
        var definition = Definition(bytes);
        int profile = definition.Assignments[assignment].Profile;
        var scope = definition.Assignments.Count(item => item.Profile == profile) > 1 ? SectionScope.Shared : SectionScope.Independent;
        return SectionReplace.Preview(bytes, assignment, scope, source, ReplaceScope.Draft);
    }

    private static ReplaceSource Gen(string digits) =>
        new ReplaceSource.Coordinates("NACA " + digits, new Provenance("gen:naca4-closed/1:" + digits, false), Selig(Naca4(digits)));

    private static ReplaceSource Coordinates(string name, byte[] selig) => new ReplaceSource.Coordinates(name, new Provenance("dat:sha256:test", false), selig);

    private static Definition Definition(byte[] bytes) => FoilSource.Parse(bytes).Definition!;

    private static ProfileDefinition Profile(byte[] bytes, string name) => Definition(bytes).Profiles.Single(item => item.Name == name);

    private static GeometryStatus Certificate(byte[] bytes) => Geometry.Assess(FoilSource.Parse(bytes)).Status;

    private static ContractError Throws(Action action)
    {
        try { action(); }
        catch (ContractError error) { return error; }
        throw new InvalidOperationException("Expected a ContractError.");
    }

    private static string? ProvenanceLine(byte[] bytes)
    {
        string block = SectionDraftTests.ProfileBlock(Encoding.UTF8.GetString(bytes), "section-a");
        int at = block.IndexOf("provenance \"", StringComparison.Ordinal);
        return at < 0 ? null : block[(at + 12)..block.IndexOf('"', at + 12)];
    }

    // The interior point nearest mid-chord, by id.
    private static string MiddleId(SectionDraftView view)
    {
        var profile = Definition(view.Bytes).Profiles.Single(item => item.Name == view.Profile);
        int index = Enumerable.Range(1, profile.Upper.Points.Length - 2).MinBy(i => Math.Abs(profile.Upper.Points[i][0] - 0.5));
        return profile.Upper.Ids[index];
    }

    private static byte[] WithChords(double rootMeters, double tipMeters)
    {
        string root = (rootMeters * 1e3).ToString("R", CultureInfo.InvariantCulture), tip = (tipMeters * 1e3).ToString("R", CultureInfo.InvariantCulture);
        string mid(double t) => ((rootMeters + (tipMeters - rootMeters) * t) * 1e3).ToString("R", CultureInfo.InvariantCulture);
        string text = Encoding.UTF8.GetString(Example()).Replace(
            "points [(0, 120), (0.1, 120), (0.3, 120), (0.5, 120), (0.7, 120), (0.9, 120), (1, 120)]",
            $"points [(0, {root}), (0.1, {mid(0.1)}), (0.3, {mid(0.3)}), (0.5, {mid(0.5)}), (0.7, {mid(0.7)}), (0.9, {mid(0.9)}), (1, {tip})]",
            StringComparison.Ordinal);
        if (text == Encoding.UTF8.GetString(Example())) throw new InvalidOperationException("The trailing rail did not change.");
        return Encoding.UTF8.GetBytes(text);
    }

    // The Example with an anchor at upper cv-4 (both surfaces) carrying a horizontal row.
    private static byte[] HorizontalAnchor()
    {
        byte[] anchored = SectionEdits.Apply(Example(), 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;
        return SectionEdits.Apply(anchored, 0, new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Horizontal, null, null)).Bytes;
    }

    private static string CurveText(double[] knots, double[][] points) =>
        "degree 5 knots [" + string.Join(", ", knots.Select(R)) + "] points [" + string.Join(", ", points.Select(p => "(" + R(p[0]) + ", " + R(p[1]) + ")")) + "]";

    private static string R(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    // A record profile sampled densely (cosine in its parameter) with its ordinates scaled, as Selig rows.
    private static byte[] SampledSelig(ProfileDefinition profile, double factor)
    {
        List<(double X, double Y)> Side(Curve curve) => Enumerable.Range(0, 201).Select(i =>
        {
            double u = 0.5 * (1 - Math.Cos(Math.PI * i / 200));
            double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, u);
            return (curve.Points.Select((p, j) => p[0] * basis[j]).Sum(), factor * curve.Points.Select((p, j) => p[1] * basis[j]).Sum());
        }).ToList();
        return SeligText("Sampled", Side(profile.Upper), Side(profile.Lower));
    }

    // A symmetric section with a 1 % chord bump at 40 % chord that no sqrt spacing of 8–16 points follows within 10 µm.
    private static byte[] BumpSelig()
    {
        List<(double X, double Y)> Side(int sign) => Enumerable.Range(0, 201).Select(i =>
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * i / 200));
            double half = 0.6 * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * x * x * x - 0.1036 * x * x * x * x);
            return (x, sign * (half + 0.01 * Math.Exp(-Math.Pow((x - 0.4) / 0.02, 2))));
        }).ToList();
        return SeligText("Bump", Side(1), Side(-1));
    }

    private static List<ProfilePoint> Slide(IReadOnlyList<ProfilePoint> side, double delta) =>
        side.Select(point => new ProfilePoint(point.X + delta * (1 - point.X), point.Y)).ToList();

    // |Δy| at each sample's own x: the as-built vertical measure.
    private static double VerticalGap(IReadOnlyList<ProfilePoint> samples, double[] knots, double[] x, double[] y)
    {
        var dense = DatImport.Dense(knots, 5, x, y, 20000);
        double worst = 0;
        foreach (var point in samples)
        {
            int index = Math.Max(1, Array.FindIndex(dense, q => q.X >= point.X));
            var (ax, ay) = dense[index - 1];
            var (bx, by) = dense[index];
            double fitted = bx == ax ? by : ay + (by - ay) * (point.X - ax) / (bx - ax);
            worst = Math.Max(worst, Math.Abs(fitted - point.Y));
        }
        return worst;
    }

    // Source samples to the fitted curve only (the one-way measure the design replaced).
    private static double OneWay(IReadOnlyList<ProfilePoint> samples, double[] knots, double[] x, double[] y)
    {
        var dense = DatImport.Dense(knots, 5, x, y, 20000);
        return samples.Max(point => dense.Min(q => Math.Sqrt((q.X - point.X) * (q.X - point.X) + (q.Y - point.Y) * (q.Y - point.Y))));
    }

    // NACA 4-digit with the closed trailing edge (DR-M12D-7 a), in the chord frame: the leading edge at the continuous
    // minimum-x point, the chord to the trailing-edge point at unit length; 201 cosine samples per surface (rule 5).
    // The P-CF closed form of docs/proof/m12d-catalog/probe (ClosedForm), restated for a test fixture.
    internal static (List<(double X, double Y)> Upper, List<(double X, double Y)> Lower) Naca4(string digits)
    {
        double m = (digits[0] - '0') / 100.0, p = (digits[1] - '0') / 10.0, t = int.Parse(digits[2..], CultureInfo.InvariantCulture) / 100.0;
        (double X, double Y) Surface(double x, int sign)
        {
            double yt = 5 * t * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * x * x * x - 0.1036 * x * x * x * x);
            double yc = m == 0 ? 0 : x < p ? m / (p * p) * (2 * p * x - x * x) : m / ((1 - p) * (1 - p)) * (1 - 2 * p + 2 * p * x - x * x);
            double dy = m == 0 ? 0 : x < p ? 2 * m / (p * p) * (p - x) : 2 * m / ((1 - p) * (1 - p)) * (p - x);
            double th = Math.Atan(dy);
            return (x - sign * yt * Math.Sin(th), yc + sign * yt * Math.Cos(th));
        }
        double a = 0, b = 0.05;
        for (int i = 0; i < 200; i++)
        {
            double c1 = b - (b - a) / 1.618033988749895, c2 = a + (b - a) / 1.618033988749895;
            if (Surface(c1, 1).X < Surface(c2, 1).X) b = c2; else a = c1;
        }
        double xs = (a + b) / 2;
        if (Surface(xs, 1).X > 0) xs = 0;
        var le = Surface(xs, 1);
        var te = Surface(1, 1);
        double mx = te.X - le.X, my = te.Y - le.Y, length = Math.Sqrt(mx * mx + my * my), angle = Math.Atan2(my, mx);
        (double X, double Y) Frame((double X, double Y) q)
        {
            double dx = q.X - le.X, dy = q.Y - le.Y;
            return ((dx * Math.Cos(angle) + dy * Math.Sin(angle)) / length, (-dx * Math.Sin(angle) + dy * Math.Cos(angle)) / length);
        }
        List<(double X, double Y)> Side(bool upper)
        {
            var list = new List<(double X, double Y)>();
            for (int i = 0; i <= 200; i++)
            {
                double s = 0.5 * (1 - Math.Cos(Math.PI * i / 200));
                if (upper) list.Add(Frame(Surface(xs + (1 - xs) * s, 1)));
                else
                {
                    double d = s * (xs + 1);
                    list.Add(Frame(d <= xs ? Surface(xs - d, 1) : Surface(d - xs, -1)));
                }
            }
            list[0] = (0, 0);
            list[^1] = (1, 0);
            return list;
        }
        return (Side(true), Side(false));
    }

    private static byte[] Selig((List<(double X, double Y)> Upper, List<(double X, double Y)> Lower) shape, double scale = 1, double degrees = 0,
        double shiftX = 0, double shiftY = 0)
    {
        double radians = degrees * Math.PI / 180, cos = Math.Cos(radians), sin = Math.Sin(radians);
        (double X, double Y) Move((double X, double Y) q) => (shiftX + scale * (q.X * cos - q.Y * sin), shiftY + scale * (q.X * sin + q.Y * cos));
        return SeligText("NACA", shape.Upper.Select(Move).ToList(), shape.Lower.Select(Move).ToList());
    }

    private static byte[] SeligText(string name, List<(double X, double Y)> upper, List<(double X, double Y)> lower)
    {
        var text = new StringBuilder(name).Append('\n');
        for (int i = upper.Count - 1; i >= 0; i--) text.Append(R(upper[i].X)).Append(' ').Append(R(upper[i].Y)).Append('\n');
        for (int i = 1; i < lower.Count; i++) text.Append(R(lower[i].X)).Append(' ').Append(R(lower[i].Y)).Append('\n');
        return Encoding.UTF8.GetBytes(text.ToString());
    }
}
