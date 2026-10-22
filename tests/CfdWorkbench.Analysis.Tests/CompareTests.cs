using System.Reflection;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// A3d-2 (design a3d-compare.md section 7.1 to 7.3; Ruling 250, Ruling 251): Compare, the Discrepancy record and its store.
/// Coefficients come from test doubles keyed by run id, so the Compare oracle does not depend on the VLM or on
/// <see cref="WingEstimator"/> (cycle-1 condition T2).
/// </summary>
internal static class CompareTests
{
    private const double Tolerance = 1e-9;
    // CDi = CL^2 / (pi A) at A 7.5, unrounded: the design's 10-digit deltas were computed from this value, not from its 10-digit print.
    private static readonly double EstimatorCdi = 0.4212557233 * 0.4212557233 / (Math.PI * 7.5);
    private static readonly RunReference DefaultReference = new(0.108, 0.9, 0.12, "frame origin", "body; wind for lift and drag");

    internal static void Run()
    {
        Check("Compare_TwoTiers_DeltaAndDiscrepancyRecord", TwoTiers);
        Check("Compare_SameKeyTwice_ReadNotNewRecord", SameKeyTwice);
        Check("Compare_EstimatorE_ExcludedFromEDeltas", EstimatorExcluded);
        Check("Compare_Inputs_SideBySide_AllFieldsBothRuns", InputsSideBySide);
        Check("Compare_Delta_NormalisedToBase", DeltaNormalised);
        Check("Compare_ReversedView_RecomputedFromRuns", ReversedView);
        Check("Compare_BaseZeroOrBelowFloor_Undefined", BaseZeroOrBelowFloor);
        Check("Compare_ELine_UndefinedWhenClOrCdiUndefined", ELineUndefined);
        Check("Compare_IncompatibleReference_BlocksDelta", IncompatibleReference);
        Check("Compare_DifferentPointOrWater_BlocksDelta", PointOrWater);
        Check("Compare_CompatiblePair_StoresOneRecord", CompatibleStoresOne);
        Check("Compare_NoAveraging_NoPreferredValueWithoutUncertainty", NoAveraging);
        Check("Discrepancy_SaveReopen_RoundTripByteEqual", RoundTrip);
        Check("Discrepancy_OmittedWhenEmpty_OlderDocumentBytesUnchanged", OmittedWhenEmpty);
        Check("Retention_RunReferencedByDiscrepancy_NeverPruned", ReferencedNeverPruned);
        Check("Retention_ReferencedRunInLatestTwenty_TakesNoQuotaSlot", ReferencedTakesNoQuota);
        Check("Discrepancy_CompareEvent_CarriesDocumentBytes", CompareEventBytes);
        Check("Compare_TierHeadline_OnlyWhenBothRunsShareARevision", TierHeadlineNeedsOneRevision);
        Check("Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted", ReachDocSize);
        Check("Discrepancy_StoreInvariants_Refused", StoreInvariants);
        Check("Discrepancy_TamperedLine_ReadsNoLongerReproduces", TamperedLine);
        Check("Discrepancy_TamperedHash_ReadsPayloadFailedCheck", TamperedHash);
        Check("Discrepancy_UnknownRuleVersion_ReadsUnavailable", UnknownRule);
        Check("Discrepancy_OldRuleVersion_StillReproduces", OldRuleStillReproduces);
        Check("NativeReader_AnalysisMember_ClosedSet", ClosedSet);
        Check("Discrepancy_SaveEvent_CarriesPinnedRunsAndBytes", SaveEvent);
    }

    // ---- 7.1 the three section 13.5 tests ----

    private static void TwoTiers()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        var values = Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0));
        var shown = Compare.Lines(vlm, estimator, CompareRule.Current, values);
        Near(0.0224653479, shown[0].Delta!.Value, "shown dCL, base = VLM");
        Near(-0.0090154117, shown[1].Delta!.Value, "shown dCDi, base = VLM");
        var result = Compare.Record(session, vlm, estimator, values);
        Equal(1, session.ReadDiscrepancies().Count, "records");
        var record = result.Record!;
        Equal(estimator.RunId, record.BaseRunId, "stored orientation: estimator first");
        Equal(vlm.RunId, record.ComparedRunId, "stored orientation: VLM second");
        Near(-0.0219717450, record.Lines[0].Delta!.Value, "stored dCL");
        Near(0.0090974287, record.Lines[1].Delta!.Value, "stored dCDi");
        Equal(true, result.Created, "created;");
    }

    private static void SameKeyTwice()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        var values = Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0));
        var first = Compare.Record(session, vlm, estimator, values);
        byte[] image = session.SaveImage();
        session.AcknowledgeSaved(image);
        Equal(false, session.Snapshot().Dirty, "clean after the save;");
        var again = Compare.Record(session, vlm, estimator, values);
        var reversed = Compare.Record(session, estimator, vlm, values);
        Equal(1, session.ReadDiscrepancies().Count, "records after three compares");
        Equal(first.Record!.Id, again.Record!.Id, "the same compare returns the first record");
        Equal(first.Record.Id, reversed.Record!.Id, "the reversed order returns the first record");
        Equal(false, again.Created || reversed.Created, "neither is a creation;");
        Equal(false, session.Snapshot().Dirty, "a read leaves the document clean;");
    }

    private static void EstimatorExcluded()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        var values = Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0));
        foreach (var (first, second) in new[] { (vlm, estimator), (estimator, vlm) })
        {
            var e = Compare.Lines(first, second, CompareRule.Current, values)[2];
            Equal("e", e.Quantity, "quantity");
            Equal(Compare.EstimatorE, e.NotCompared, "not compared code");
            Equal(true, e.Delta is null && e.Undefined is null, "no delta;");
            Equal(first.Tier == "estimator", e.Base is null, "the estimator side is null (base);");
            Equal(second.Tier == "estimator", e.Compared is null, "the estimator side is null (compared);");
        }
    }

    // ---- 7.2 one test per ANA-06 clause, and the delta rules ----

    private static void InputsSideBySide()
    {
        using var session = Fixture.Opened();
        var r4 = Row(session, "vlm-strip"); Fixture.TwistEdit(session, 1.0); var r5 = Row(session, "vlm-strip");
        var view = Project(session, r4, r5, Values((r4, 0.4035, 0.00731, 0.9452759), (r5, 0.4120, 0.0076, 0.9479157)));
        string[] expected = ["Revision", "Tier", "Method", "Envelope", "Settings", "Lattice", "Speed", "α", "Depth h_ref", "Datum", "Water", "ρ", "ν",
            "S_ref", "b_ref", "c_ref", "Moment datum", "Axes", "Omissions", "Freshness"];
        Equal(string.Join("|", expected), string.Join("|", view.Inputs.Select(row => row.Field)), "every section 2.1 field;");
        Equal(true, view.Inputs.Single(row => row.Field == "Revision").Differs, "the differing revision is marked;");
        Equal(false, view.Inputs.Single(row => row.Field == "Speed").Differs, "an equal field is not marked;");
        Equal(true, view.Inputs.Single(row => row.Field == "Envelope").Base.StartsWith("vlm-envelope", StringComparison.Ordinal), "envelope row carries the envelope id;");
        Equal(CompareState.Compatible, view.State, "state");
    }

    private static void DeltaNormalised()
    {
        var a = Plain(); var b = Plain(alpha: 3);
        Near(0.0210656753, Delta(a, b, 0.4035, 0.4120), "base 0.4035, compared 0.4120");
        Near(0.5, Delta(a, b, -0.2, -0.1), "negative base: (c - b) / |b|");
    }

    private static void ReversedView()
    {
        var a = Plain(); var b = Plain(alpha: 3);
        Near(-0.0206310680, Delta(b, a, 0.412, 0.4035), "row 1: the r4 -> r5 record viewed r5 -> r4, from the runs");
        Near(-1.0, Delta(a, b, -1, -2), "row 2 canonical: base -1, compared -2");
        Near(0.5, Delta(b, a, -2, -1), "row 2 reversed: (-1 - -2) / |-2|, not -d / (1 + d)");
        var zero = Compare.Lines(b, a, CompareRule.Current, Values((b, 0.0, 0.0076, 0.9), (a, 0.4, 0.0076, 0.9)))[0];
        Equal(Compare.BaseZero, zero.Undefined, "row 3: a canonical compared value of 0, reversed, is Undefined");
    }

    private static void BaseZeroOrBelowFloor()
    {
        var a = Plain(); var b = Plain(alpha: 3);
        var zero = Compare.Lines(a, b, CompareRule.Current, Values((a, 0.0, 0.0076, 0.9), (b, 0.0001, 0.0076, 0.9)))[0];
        Equal(Compare.BaseZero, zero.Undefined, "base 0");
        Equal(true, zero.Delta is null, "no delta at base 0;");
        var tiny = Compare.Lines(a, b, CompareRule.Current, Values((a, 5e-5, 0.0076, 0.9), (b, 0.4, 0.0076, 0.9)))[0];
        Equal(Compare.BaseFloor, tiny.Undefined, "base below the CL floor");
        Equal(true, tiny.Delta is null, "no delta below the floor;");
    }

    private static void ELineUndefined()
    {
        var a = Plain(); var b = Plain(alpha: 3);
        var lines = Compare.Lines(a, b, CompareRule.Current, Values((a, 5e-5, 0.0076, 0.93), (b, 0.4, 0.0076, 0.96)));
        Equal(Compare.EIllConditioned, lines[2].Undefined, "CL Undefined makes e Undefined");
        var cdi = Compare.Lines(a, b, CompareRule.Current, Values((a, 0.4, 5e-7, 0.93), (b, 0.4, 0.0076, 0.96)));
        Equal(Compare.BaseFloor, cdi[1].Undefined, "CDi base below its floor");
        Equal(Compare.EIllConditioned, cdi[2].Undefined, "CDi below its floor makes e Undefined");
        Equal(true, cdi[2].Delta is null, "no e delta;");
    }

    private static void IncompatibleReference()
    {
        var baseRun = Plain();
        (string Row, string Field, Func<RunReference, RunReference> Change)[] rows =
        [
            ("S_ref 0.100", "S_ref", r => r with { SRef = 0.100 }),
            ("S_ref near-equal", "S_ref", r => r with { SRef = 0.1085 }),
            ("S_ref last ulp", "S_ref", r => r with { SRef = Math.BitIncrement(0.108) }),
            ("b_ref", "b_ref", r => r with { BRef = 0.91 }),
            ("c_ref", "c_ref", r => r with { CRef = 0.121 }),
            ("moment datum", "moment datum", r => r with { MomentDatum = "tip" }),
            ("axes", "axes", r => r with { Axes = "body" })
        ];
        foreach (var row in rows)
        {
            var blocks = Compare.Compatibility(baseRun, Plain(reference: row.Change(DefaultReference)));
            Equal(1, blocks.Count, row.Row + ": one block;");
            Equal(BlockReason.Reference, blocks[0].Reason, row.Row + " reason");
            Equal(row.Field, blocks[0].Fields.Single(), row.Row + ": exactly the differing field");
        }
        using var session = Fixture.Opened();
        var a = Row(session, "vlm-strip", reference: DefaultReference);
        var b = Row(session, "estimator", reference: DefaultReference with { SRef = 0.1085 });
        var result = Compare.Record(session, a, b, Values((a, 0.4, 0.0076, 0.9), (b, 0.4, 0.0075, 1.0)));
        Equal(true, result.Record is null && result.Blocks.Count == 1, "blocked, no record;");
        Equal(0, session.ReadDiscrepancies().Count, "nothing stored");
        var view = Project(session, a, b, Values((a, 0.4, 0.0076, 0.9), (b, 0.4, 0.0075, 1.0)));
        Equal(CompareState.Blocked, view.State, "pane state");
        Equal(0, view.Outputs.Count, "no output row, so no delta cell;");
        Equal(Labels.DeltaBlockedReference("S_ref"), view.Notices[0], "the block copy names exactly the differing field");
    }

    private static void PointOrWater()
    {
        var baseRun = Plain();
        (string Name, Func<AnalysisRun, AnalysisRun> Change)[] point =
        [
            ("speed", r => r with { Op = r.Op with { Speed = 11 } }), ("pAtm", r => r with { Op = r.Op with { PAtm = 100000 } }),
            ("hRef", r => r with { Op = r.Op with { HRef = 0.7 } }), ("datum", r => r with { Op = r.Op with { Datum = "mast" } }),
            ("alphaDeg", r => r with { Op = r.Op with { AlphaDeg = 3 } }), ("load", r => r with { Op = r.Op with { Load = 100 } })
        ];
        foreach (var (name, change) in point)
        {
            var blocks = Compare.Compatibility(baseRun, change(baseRun));
            Equal(1, blocks.Count, name + ": one block;");
            Equal(BlockReason.Point, blocks[0].Reason, name + " is a point reason");
        }
        (string Name, Func<AnalysisRun, AnalysisRun> Change)[] water =
        [
            ("temperatureC", r => r with { Water = r.Water with { TemperatureC = 20 } }), ("salinity", r => r with { Water = r.Water with { SalinityGPerKg = 0 } }),
            ("rho", r => r with { Water = r.Water with { Rho = 999 } }), ("nu", r => r with { Water = r.Water with { Nu = 1.1e-6 } }),
            ("pv", r => r with { Water = r.Water with { Pv = 2000 } }), ("source", r => r with { Water = r.Water with { Source = "other" } }),
            ("tableHash", r => r with { Water = r.Water with { TableHash = new string('b', 64) } })
        ];
        foreach (var (name, change) in water)
        {
            var blocks = Compare.Compatibility(baseRun, change(baseRun));
            Equal(1, blocks.Count, name + ": one block;");
            Equal(BlockReason.Water, blocks[0].Reason, name + " is a water reason");
        }
    }

    private static void CompatibleStoresOne()
    {
        using var session = Fixture.Opened();
        var r4 = Row(session, "vlm-strip"); Fixture.TwistEdit(session, 1.0); var r5 = Row(session, "vlm-strip");
        var values = Values((r4, 0.4035, 0.00731, 0.9452759), (r5, 0.4120, 0.0076, 0.9479157));
        var before = session.ReadRuns().Runs.Select(stored => stored.Run).ToArray();
        var result = Compare.Record(session, r5, r4, values);
        var record = result.Record!;
        Equal(1, session.ReadDiscrepancies().Count, "records");
        Equal(r4.RunId, record.BaseRunId, "canonical orientation: earlier revision first");
        Equal(r5.RunId, record.ComparedRunId, "the second run");
        Equal(DiscrepancyIntegrity.Intact, Compare.Verify(record, r4, r5, values), "the lines reproduce;");
        Equal(true, before.SequenceEqual(session.ReadRuns().Runs.Select(stored => stored.Run)), "the runs are untouched;");
    }

    private static void NoAveraging()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        var values = Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0));
        var result = Compare.Record(session, vlm, estimator, values);
        var view = Project(session, vlm, estimator, values, result);
        var lines = Compare.Lines(vlm, estimator, CompareRule.Current, values);
        var traceable = new List<double>();
        foreach (var run in new[] { vlm, estimator })
        {
            var c = values(run);
            traceable.AddRange(new[] { c.Cl, c.Cdi, c.E }.Where(v => v is not null).Select(v => v!.Value));
        }
        traceable.AddRange(lines.Where(line => line.Delta is not null).Select(line => line.Delta!.Value));
        foreach (var row in view.Outputs.Where(row => row.Quantity is "CL" or "CDi" or "e"))
            foreach (double? number in new[] { row.BaseValue, row.ComparedValue, row.DeltaValue })
                if (number is { } value)
                    Equal(true, traceable.Any(known => BitConverter.DoubleToInt64Bits(known) == BitConverter.DoubleToInt64Bits(value)),
                        row.Quantity + " " + value.ToString("R") + " traces to one run or one delta;");
        var strings = new List<string> { view.Headline, view.RecordStatus ?? "", view.Footnote ?? "" };
        strings.AddRange(view.Notices);
        foreach (var row in view.Outputs) strings.AddRange([row.Base, row.Compared, row.Delta, row.Note ?? "", row.AccessibleName]);
        foreach (var row in view.Inputs) strings.AddRange([row.Field, row.Base, row.Compared]);
        // The footnote (A3D-COPY-12) is the one approved sentence that names the words, to deny them.
        foreach (string text in strings.Where(text => text != Labels.CompareFootnote))
            foreach (string word in new[] { "average", "mean", "best", "recommended" })
                Equal(false, text.Contains(word, StringComparison.OrdinalIgnoreCase), $"'{text}' contains '{word}';");
        Equal(true, view.Headline.Contains(Labels.NoUncertainty, StringComparison.Ordinal), "the preferred-value clause reads the no-uncertainty copy;");
        // Shape: no member of the view model types can hold a combined or preferred value.
        foreach (var type in new[] { typeof(CompareViewModel), typeof(CompareOutputRow), typeof(CompareColumn), typeof(CompareInputRow), typeof(DiscrepancyRecord), typeof(DiscrepancyLine) })
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                Equal(false, new[] { "mean", "average", "blend", "preferred", "best", "winner", "rank" }.Any(word => property.Name.Contains(word, StringComparison.OrdinalIgnoreCase)),
                    type.Name + "." + property.Name + " names a combined value;");
    }

    // ---- 7.3 persistence and integrity ----

    private static void RoundTrip()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        Compare.Record(session, vlm, estimator, Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0)));
        byte[] first = session.SaveImage();
        Equal(true, Encoding.UTF8.GetString(first).Contains("\"discrepancies\"", StringComparison.Ordinal), "the member is written;");
        using var reopened = new AuthoringSession();
        reopened.Reopen(first);
        Equal(1, reopened.ReadDiscrepancies().Count, "records after reopen");
        Equal(true, first.AsSpan().SequenceEqual(reopened.SaveImage()), "the second image is byte-equal;");
    }

    private static void OmittedWhenEmpty()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        byte[] first = session.SaveImage();
        Equal(false, Encoding.UTF8.GetString(first).Contains("discrepancies", StringComparison.Ordinal), "no member for no record;");
        Equal("cfdw-project-2", NativeProject.FormatOf(first), "format");
        using var reopened = new AuthoringSession();
        reopened.Reopen(first);
        Equal(true, first.AsSpan().SequenceEqual(reopened.SaveImage()), "bytes unchanged by a reopen and save;");
        Equal(true, reopened.ReadRuns().Runs.Select(stored => stored.Run.ContentHash).SequenceEqual(new[] { vlm.ContentHash, estimator.ContentHash }), "every run content hash unchanged;");
    }

    private static void ReferencedNeverPruned()
    {
        using var session = Fixture.Opened();
        Fixture.TwistEdit(session, 1.0);
        var onB = Enumerable.Range(0, AuthoringSession.RetainedOthersPerTier + 2).Select(step => Row(session, "vlm-strip", alpha: step)).ToArray();
        // Same-op pairs need equal alpha, so the oldest is paired with an estimator row at its own inputs.
        var estimator = Row(session, "estimator", alpha: 0);
        var values = Values((onB[0], 0.4, 0.0076, 0.9), (estimator, 0.41, 0.0075, 1.0));
        Compare.Record(session, onB[0], estimator, values);
        session.Undo(Fixture.Id());
        Fixture.TwistEdit(session, 2.0);
        _ = session.SaveImage();
        var ledger = session.ReadRuns();
        Equal(true, ledger.Runs.Any(stored => stored.Run.RunId == onB[0].RunId), "the referenced run survives the save;");
        Equal(false, ledger.Pruned.Any(tombstone => tombstone.RunId == onB[0].RunId), "no tombstone for it;");
        Equal(true, ledger.Runs.Any(stored => stored.Run.RunId == estimator.RunId), "the estimator run survives too;");
        Equal(false, ledger.Runs.Any(stored => stored.Run.RunId == onB[1].RunId), "an unreferenced old run is still pruned;");
    }

    private static void ReferencedTakesNoQuota()
    {
        using var session = Fixture.Opened();
        Fixture.TwistEdit(session, 1.0);
        int total = AuthoringSession.RetainedOthersPerTier + 2;
        var onB = Enumerable.Range(0, total).Select(step => Row(session, "vlm-strip", alpha: step)).ToArray();
        int referencedIndex = total / 2; // inside the latest RetainedOthersPerTier runs
        session.Undo(Fixture.Id());
        Fixture.TwistEdit(session, 2.0);
        var estimator = Row(session, "estimator", alpha: referencedIndex);
        Compare.Record(session, onB[referencedIndex], estimator, Values((onB[referencedIndex], 0.4, 0.0076, 0.9), (estimator, 0.41, 0.0075, 1.0)));
        _ = session.SaveImage();
        var ledger = session.ReadRuns();
        var survivors = ledger.Runs.Select(stored => stored.Run.RunId).ToHashSet();
        int unreferencedKept = onB.Where((run, index) => index != referencedIndex && survivors.Contains(run.RunId)).Count();
        Equal(AuthoringSession.RetainedOthersPerTier, unreferencedKept, "the latest quota of unreferenced runs all survive;");
        Equal(true, survivors.Contains(onB[referencedIndex].RunId), "the referenced run survives on top of the quota;");
        Equal(1, ledger.Pruned.Count, "exactly one run is pruned;");
        Equal(1, session.ReadLocalEvents().Last(item => item.Operation == "analysis.save").Compare?.PinnedRuns, "pinnedRuns on the save");
    }

    private static void TierHeadlineNeedsOneRevision()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip");
        Fixture.TwistEdit(session, 1.0);
        var estimator = Row(session, "estimator");
        Equal(false, vlm.Inputs.AcceptedId == estimator.Inputs.AcceptedId, "the pair is on two revisions;");
        var view = Project(session, vlm, estimator, Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0)));
        Equal(CompareState.Compatible, view.State, "state");
        Equal(true, view.Headline.StartsWith(Labels.RevisionsHeadline("").Split('=')[0], StringComparison.Ordinal), "the revision headline, not the tier headline: " + view.Headline);
        Equal(false, view.Headline.StartsWith("Tiers disagree", StringComparison.Ordinal), "no tier headline across revisions;");
    }

    private static void CompareEventBytes()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        Compare.Record(session, vlm, estimator, Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0)));
        byte[] image = session.SaveImage();
        var compared = session.ReadLocalEvents().Last(item => item.Operation == "analysis.compare");
        Equal(image.Length, compared.Compare?.DocumentBytes, "documentBytes on the compare event is the image size");
    }

    // Ruling 251 and design 1.8: records pin runs with no cap. The cap is small here so the refusal comes fast; the bytes per
    // run (128 strips, 10-digit synthetic values: a lower bound, a solver prints up to 17 digits) and per record are measured, and the proof note takes them from this line.
    private static void ReachDocSize()
    {
        using var session = new AuthoringSession(700_000);
        session.Open(FoilSource.NewDefault(), Fixture.Id(), true);
        var values = new Dictionary<string, Coefficients>();
        Func<AnalysisRun, Coefficients> lookup = run => values[run.RunId];
        long perRun = 0, perRecord = 0;
        int pairs = 0;
        ContractError? refusal = null;
        byte[]? lastGood = null;
        AnalysisRun? previous = null;
        for (int step = 0; step < 300 && refusal is null; step++)
        {
            Fixture.TwistEdit(session, 1.0);
            try
            {
                lastGood = session.SaveImage();
                long before = lastGood.Length;
                var run = Row(session, "vlm-strip", strips: 128);
                values[run.RunId] = new Coefficients(0.4 + step * 1e-3, 0.0076, 0.9);
                if (perRun == 0) perRun = session.SaveImage().Length - before;
                if (previous is not null && step % 2 == 1)
                {
                    lastGood = session.SaveImage();
                    before = lastGood.Length;
                    Compare.Record(session, previous, run, lookup);
                    if (perRecord == 0) perRecord = session.SaveImage().Length - before;
                    pairs++;
                }
                previous = run;
            }
            catch (ContractError error) { refusal = error; }
        }
        Equal("DOC-SIZE", refusal?.Code, "the document reaches its size cap and refuses;");
        Equal(true, pairs > 0 && lastGood is not null, "records were stored before the cap;");
        Equal(true, lastGood!.AsSpan().SequenceEqual(session.SaveImage()), "the saved image is unchanged by the refusal;");
        Equal(1, session.ReadLocalEvents().Count(item => item.Operation == "analysis.size" && item.Compare?.PinnedRuns is not null && item.Compare.Records > 0), "the refusal event carries its pinned-run count;");
        // Phase B: the same four runs and two records fit a cap set just above their image; the next record is the call that
        // pushes the document over, and it is the call that refuses (design 7.3).
        using var roomy = new AuthoringSession();
        roomy.Open(FoilSource.NewDefault(), Fixture.Id(), true);
        FillFour(roomy);
        int fitted = roomy.SaveImage().Length;
        using var tight = new AuthoringSession(fitted + (int)perRecord / 2);
        tight.Open(FoilSource.NewDefault(), Fixture.Id(), true);
        var (four, tightValues) = FillFour(tight);
        byte[] tightImage = tight.SaveImage();
        string call = "none";
        ContractError? over = null;
        try { call = "RecordDiscrepancy"; Compare.Record(tight, four[1], four[2], tightValues); }
        catch (ContractError error) { over = error; }
        Equal("DOC-SIZE", over?.Code, "the next record pushes the document over the cap;");
        Equal("RecordDiscrepancy", call, "the refusing call is RecordDiscrepancy;");
        Equal(true, tightImage.AsSpan().SequenceEqual(tight.SaveImage()), "the image is unchanged by the refusal;");
        Equal(1, tight.ReadLocalEvents().Count(item => item.Operation == "analysis.size"), "the size refusal is reported;");
        Console.WriteLine($"MEASURED bytesPerRun128Strips={perRun} (lower bound: 10-digit synthetic strips) bytesPerRecord={perRecord} recordsAtCap={pairs}");
    }

    // Four 128-strip runs, each on its own revision, and the records (0,1) and (2,3).
    private static (AnalysisRun[] Runs, Func<AnalysisRun, Coefficients> Values) FillFour(AuthoringSession session)
    {
        var values = new Dictionary<string, Coefficients>();
        var runs = new AnalysisRun[4];
        for (int i = 0; i < 4; i++)
        {
            Fixture.TwistEdit(session, 1.0);
            runs[i] = Row(session, "vlm-strip", strips: 128);
            values[runs[i].RunId] = new Coefficients(0.4 + i * 1e-3, 0.0076, 0.9);
        }
        Func<AnalysisRun, Coefficients> lookup = run => values[run.RunId];
        Compare.Record(session, runs[0], runs[1], lookup);
        Compare.Record(session, runs[2], runs[3], lookup);
        return (runs, lookup);
    }

    private static void StoreInvariants()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        var later = Row(session, "vlm-strip", alpha: 3);
        var failed = Row(session, "vlm-strip", alpha: 4, outcome: new RunOutcome.Failed("ANA-X", "x"));
        var values = Values((vlm, 0.4, 0.0076, 0.9), (estimator, 0.41, 0.0075, 1.0), (later, 0.42, 0.0077, 0.9));
        var good = Compare.Build(estimator, vlm, values);
        var ordinal = Compare.AcceptedOrdinal(session);
        void Refused(string code, string what, params DiscrepancyRecord[] records)
        {
            var all = new AnalysisRecords(session.ReadRuns().Runs.Select(stored => stored.Run).ToArray(), [], [], records);
            try { RunRecord.CheckStore(all, _ => true, id => ordinal(id)); }
            catch (ContractError error) { Equal(code, error.Code, what); return; }
            throw new InvalidOperationException("expected " + code + " for " + what + "; none");
        }
        Refused("DOC-DISCREPANCY-KEY", "duplicate key", good, good with { Id = Fixture.Id() });
        Refused("DOC-DISCREPANCY-ID", "duplicate id", good, Compare.Build(vlm, later, values) with { Id = good.Id });
        Refused("DOC-SCHEMA", "base = compared", good with { ComparedRunId = good.BaseRunId });
        Refused("DOC-SCHEMA", "a tier pair stored higher to lower", Sealed(good with { BaseRunId = vlm.RunId, ComparedRunId = estimator.RunId }));
        Refused("DOC-SCHEMA", "a same-tier, same-revision pair stored against document order", Sealed(Compare.Build(vlm, later, values) with { BaseRunId = later.RunId, ComparedRunId = vlm.RunId }));
        Refused("DOC-REFERENCE", "an unknown run id", Sealed(good with { ComparedRunId = Fixture.Id() }));
        Refused("DOC-REFERENCE", "a Failed run", Sealed(good with { BaseRunId = estimator.RunId, ComparedRunId = failed.RunId }));
        var refusal = Fixture.Throws<ContractError>(Task.Run(() => session.RecordDiscrepancy(Sealed(good with { BaseRunId = vlm.RunId, ComparedRunId = estimator.RunId }))));
        Equal("DOC-SCHEMA", refusal.Code, "RecordDiscrepancy refuses the same row with the same code");
        Equal(0, session.ReadDiscrepancies().Count, "nothing stored by a refusal");
        session.RecordDiscrepancy(good);
        Equal(1, session.ReadDiscrepancies().Count, "the good record stores");
    }

    private static void TamperedLine()
    {
        var (record, a, b, values) = StoredPair();
        var lines = record.Lines.ToArray();
        lines[0] = lines[0] with { Delta = lines[0].Delta + 1e-9 };
        var tampered = DiscrepancyRecords.Seal(record with { Lines = lines });
        Equal(DiscrepancyIntegrity.NoLongerReproduces, Compare.Verify(tampered, a, b, values), "a changed line with a fixed hash");
    }

    private static void TamperedHash()
    {
        var (record, a, b, values) = StoredPair();
        var lines = record.Lines.ToArray();
        lines[0] = lines[0] with { Delta = lines[0].Delta + 1e-9 };
        Equal(DiscrepancyIntegrity.PayloadFailedCheck, Compare.Verify(record with { Lines = lines }, a, b, values), "a changed line, the stored hash kept");
        Equal(DiscrepancyIntegrity.Intact, Compare.Verify(record, a, b, values), "the untouched record");
    }

    private static void UnknownRule()
    {
        var (record, a, b, values) = StoredPair();
        var unknown = DiscrepancyRecords.Seal(record with { Rule = new CompareRule(CompareRule.RuleId, "9.9.9") });
        Equal(DiscrepancyIntegrity.RuleUnknown, Compare.Verify(unknown, a, b, values), "an unknown rule version, not verified under another rule");
    }

    private static void OldRuleStillReproduces()
    {
        var (record, a, b, values) = StoredPair();
        var rules = new Dictionary<string, Compare.RuleFunction>
        {
            [CompareRule.V100] = Compare.Rules[CompareRule.V100],
            ["1.1.0"] = (first, second, v) => [new DiscrepancyLine("CL", 1, 2, Delta: 1.0)]
        };
        Equal(DiscrepancyIntegrity.Intact, Compare.Verify(record, a, b, values, rules), "a 1.0.0 record under a build whose newest rule is 1.1.0");
    }

    private static void ClosedSet()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        Compare.Record(session, vlm, estimator, Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0)));
        string text = Encoding.UTF8.GetString(session.SaveImage());
        using (var accepted = new AuthoringSession()) accepted.Reopen(Encoding.UTF8.GetBytes(text));
        using var refused = new AuthoringSession();
        var error = Fixture.Throws<ContractError>(Task.Run(() => refused.Reopen(Encoding.UTF8.GetBytes(text.Replace("\"discrepancies\"", "\"discrepanciesX\"")))));
        Equal("DOC-UNSUPPORTED-FIELD", error.Code, "an unknown analysis member is refused");
    }

    private static void SaveEvent()
    {
        using var session = Fixture.Opened();
        var vlm = Row(session, "vlm-strip"); var estimator = Row(session, "estimator");
        Compare.Record(session, vlm, estimator, Values((vlm, 0.4120, 0.00760, 0.9479157), (estimator, 0.4212557233, EstimatorCdi, 1.0)));
        byte[] image = session.SaveImage();
        var saved = session.ReadLocalEvents().Last(item => item.Operation == "analysis.save");
        Equal(0, saved.Compare?.PinnedRuns, "pinnedRuns on a save");
        Equal("captured", saved.Outcome, "the save report claims a capture, not a completed save");
        Equal(image.Length, saved.Compare?.DocumentBytes, "documentBytes is the save image's size");
        Equal(1, saved.Compare?.Records, "records");
        using var plain = Fixture.Opened();
        Equal(0, plain.ReadLocalEvents().Count(item => item.Operation == "analysis.save"), "a document with no run emits no save report");
    }

    // ---- fixtures ----

    private static void Near(double expected, double actual, string what)
    {
        if (Math.Abs(expected - actual) > Tolerance) throw new InvalidOperationException($"{what} expected {expected:F10}; actual {actual:F10}");
    }

    private static double Delta(AnalysisRun first, AnalysisRun second, double firstCl, double secondCl) =>
        Compare.Lines(first, second, CompareRule.Current, Values((first, firstCl, 0.0076, 0.9), (second, secondCl, 0.0076, 0.9)))[0].Delta!.Value;

    private static Func<AnalysisRun, Coefficients> Values(params (AnalysisRun Run, double Cl, double Cdi, double E)[] table) =>
        run => table.Where(item => item.Run.RunId == run.RunId).Select(item => new Coefficients(item.Cl, item.Cdi, item.E)).Single();

    private static DiscrepancyRecord Sealed(DiscrepancyRecord record) => DiscrepancyRecords.Seal(record);

    private static (DiscrepancyRecord Record, AnalysisRun A, AnalysisRun B, Func<AnalysisRun, Coefficients> Values) StoredPair()
    {
        var a = Plain(); var b = Plain(alpha: 2.0, tier: "estimator");
        var values = Values((a, 0.4120, 0.0076, 0.9479157), (b, 0.4212557233, EstimatorCdi, 1.0));
        return (Compare.Build(b, a, values), b, a, values);
    }

    private static CompareViewModel Project(AuthoringSession session, AnalysisRun a, AnalysisRun b, Func<AnalysisRun, Coefficients> values,
        Compare.CompareResult? result = null)
    {
        var wing = new FakeWing();
        var current = Freshness.Current(session.Snapshot(), Fixture.Salt, Fixture.Op(2.0), wing.Method, wing.Settings);
        return CompareProjection.Build(a, b, new CompareContext(current, Units.Metric, values, Compare.AcceptedOrdinal(session)) { Result = result });
    }

    // A recorded run on the session's current accepted revision.
    private static AnalysisRun Row(AuthoringSession session, string tier, double alpha = 2.0, RunReference? reference = null,
        RunOutcome? outcome = null, int strips = 0)
    {
        var snapshot = session.Snapshot();
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(assignment => assignment.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        var run = Build(tier, alpha, inputs, Fixture.Salt, reference ?? DefaultReference, outcome ?? new RunOutcome.Completed(), strips);
        session.RecordRun(run);
        return run;
    }

    // A sealed run with no session (for the pure functions); its hashes are real, so Verify holds.
    private static AnalysisRun Plain(double alpha = 2.0, string tier = "vlm-strip", RunReference? reference = null)
    {
        string hash = new('a', 64);
        return Build(tier, alpha, new RunInputs(Fixture.Id(), hash, [hash], "cfdw-cv/2", "rule-a/1"), Fixture.Salt, reference ?? DefaultReference,
            new RunOutcome.Completed(), 0);
    }

    private static AnalysisRun Build(string tier, double alpha, RunInputs inputs, WaterRecord water, RunReference reference, RunOutcome outcome, int strips)
    {
        bool estimator = tier == "estimator";
        var settings = new RunSettings(estimator ? 0 : 64, estimator ? 0 : 4, "cosine", "cosine", 20, "+x", 1e-8, estimator ? "estimator-envelope/1" : "vlm-envelope/1",
            null, [2, 4], "clean", 0.3, SectionEtas: [0, 0.5, 1], SectionXs: [0, 1]);
        string settingsHash = RunRecord.SettingsHash(settings);
        var method = new RunMethod(estimator ? "cfdw.estimator.wing" : "cfdw.vlm-strip", "1.0.0", estimator ? 0 : 1);
        var op = Fixture.Op(alpha);
        var rows = Enumerable.Range(0, strips).Select(j => new StripLoad(j, 0.01 * j, j / (double)Math.Max(1, strips), 0.12 - 0.0001 * j, 0.31 + 0.001 * j, 0.0123456789,
            0.0345678901 + alpha, 4.2345678e5, 0.4123456789, new StripValue(0.0123456789, null), new StripValue(0.0112345678, null),
            0.1234567891, 0.2345678912, 40.5123456789 + j, 1.5123456789, -0.2512345678, 0.7512345678, -0.0125123456, YLow: 0.01 * j, YHigh: 0.01 * (j + 1))).ToArray();
        var run = new AnalysisRun(Fixture.Id(), RunRecord.Key(inputs, water, op, method, settingsHash), "", outcome, tier, method, settings, settingsHash, inputs,
            water, op, reference, 0.01, rows, 12.5, new RunPlatform("osx", "arm64", "10.0"),
            tier == "vlm-strip" && outcome is RunOutcome.Completed ? new RunDiagnostics(1e-13, 42.5) : null);
        return run with { ContentHash = RunRecord.ContentHash(run) };
    }
}
