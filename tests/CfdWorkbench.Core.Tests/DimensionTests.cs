using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class DimensionTests
{
    internal static void Run()
    {
        Check("ApplyDimension_Span15b_HalfSpanExact", () =>
        {
            using var session = Opened();
            session.ApplyDimension(Id(), new("span", "1350"));
            string text = Encoding.UTF8.GetString(session.Snapshot().Source);
            Equal(true, text.Contains("half_span 675 mm", StringComparison.Ordinal));
            Near(DecimalSi.Parse("675", -3), session.InspectAccepted().Authored.HalfSpanMeters!.Value);
            Near(DecimalSi.Parse("1350", -3), session.InspectAccepted().Authored.HalfSpanMeters!.Value * 2);
        });
        Check("ApplyDimension_Span_ChordAt201EtaUnchanged", () =>
        {
            using var session = Opened();
            double[] before = Chords(session.Snapshot().Source);
            session.ApplyDimension(Id(), new("span", "1350"));
            double[] after = Chords(session.Snapshot().Source);
            Equal(201, before.Length);
            for (int i = 0; i < before.Length; i++) Equal(before[i], after[i]);
        });
        Check("ApplyDimension_Span_StationEtaUnchanged", () =>
        {
            using var session = Opened();
            var before = session.InspectAccepted().Authored.Assignments.Select(item => item.Eta).ToArray();
            session.ApplyDimension(Id(), new("span", "1350"));
            var after = session.InspectAccepted().Authored.Assignments.Select(item => item.Eta).ToArray();
            Equal(3, after.Length);
            Equal(before.Length, after.Length);
            for (int i = 0; i < before.Length; i++) NearEta(before[i], after[i]);
            string locked = Encoding.UTF8.GetString(FoilSourceTests.Example)
                .Replace("sections { at root profile \"section-a\" at tip profile \"section-a\" }",
                    "sections { at root profile \"section-a\" at 225 mm profile \"section-a\" at tip profile \"section-a\" }\n" +
                    "  locks {\n" +
                    "    root_mirror leading\n    root_mirror trailing\n    root_mirror dihedral\n    root_mirror twist\n    root_mirror thickness\n" +
                    "    value trailing at 100 mm 120\n" +
                    "  }");
            byte[] source = Encoding.UTF8.GetBytes(locked);
            double[] lockBefore = LockEta(source);
            byte[] patched = FoilSource.PatchSpan(source, "1350");
            string patchedText = Encoding.UTF8.GetString(patched);
            Equal(true, patchedText.Contains("at 150 mm", StringComparison.Ordinal));
            Equal(true, patchedText.Contains("at 337.5 mm", StringComparison.Ordinal));
            double[] lockAfter = LockEta(patched);
            Equal(lockBefore.Length, lockAfter.Length);
            for (int i = 0; i < lockBefore.Length; i++) NearEta(lockBefore[i], lockAfter[i]);
        });
        Check("ApplyDimension_Span_OneUndoStepUndoExact", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            int accepted = session.Envelope().Accepted.Length;
            string operation = Id();
            string id = session.ApplyDimension(operation, new("span", "1350"));
            Equal(accepted + 1, session.Envelope().Accepted.Length);
            var edit = session.Envelope().Accepted.Single(row => row.Id == id).Edit!;
            Equal("dimension", edit.Rail);
            Equal("span", edit.VertexId);
            Equal(operation, edit.DraftId);
            Equal(0L, edit.Generation);
            var apply = session.ReadLocalEvents().Last(item => item.Operation == "document.apply" && item.Outcome == "OK");
            Equal("dimension", apply.EditKind);
            byte[] patched = session.Snapshot().Source.ToArray();
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, patched.AsSpan().SequenceEqual(session.Snapshot().Source));
            byte[] saved = session.SaveImage();
            using var reopened = new AuthoringSession();
            reopened.Reopen(saved);
            Equal(true, patched.AsSpan().SequenceEqual(reopened.Snapshot().Source));
            Equal("dimension", reopened.Envelope().Accepted.Single(row => row.Id == id).Edit!.Rail);
            reopened.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(reopened.Snapshot().Source));
        });
        Check("ApplyDimension_SameOperationId_Memoized", () =>
        {
            using var session = Opened();
            string operation = Id();
            string first = session.ApplyDimension(operation, new("span", "1350"));
            int accepted = session.Envelope().Accepted.Length;
            byte[] bytes = session.Snapshot().Source.ToArray();
            string second = session.ApplyDimension(operation, new("span", "1350"));
            Equal(first, second);
            Equal(accepted, session.Envelope().Accepted.Length);
            Equal(true, bytes.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("ApplyDimension_SpanNotNumber_RefusedUnchanged", () =>
        {
            using var session = Opened();
            RefuseUnchanged(session, "DSL-LEX", "12abc");
            RefuseUnchanged(session, "DSL-LEX", "");
        });
        Check("ApplyDimension_SpanNonPositive_RefusedUnchanged", () =>
        {
            using var session = Opened();
            RefuseUnchanged(session, "DSL-UNIT", "0");
            RefuseUnchanged(session, "DSL-UNIT", "-5");
        });
        Check("ApplyDimension_SpanAtOneMillionMeters_RefusedEdgesCross", () =>
        {
            using var session = Opened();
            RefuseUnchanged(session, "DSL-EDGES-CROSS", "1000000000");
            RefuseUnchanged(session, "DSL-EDGES-CROSS", "1000000001");
        });
        // Red-first: before EditReference learned "dimension", Reopen threw DOC-REFERENCE
        // (docs/proof/c1-red-runs.md). A span receipt now reopens. An unknown dimension name still refuses DOC-REFERENCE.
        Check("Receipt_Dimension_OldReaderRefusesDocReference", () =>
        {
            using var session = OpenedExample();
            string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2");
            session.ReviseOrdinate(draft, 0, 0.01);
            session.Apply(Id(), session.Validate(draft, 1));
            byte[] applied = session.Snapshot().Source.ToArray();
            var envelope = session.Envelope();
            var row = envelope.Accepted[1];
            var dimension = row.Edit! with { Rail = "dimension", VertexId = "span", Generation = 0 };
            var accepted = envelope.Accepted.ToArray();
            accepted[1] = row with { Edit = dimension };
            using var reopened = new AuthoringSession();
            reopened.Reopen(NativeProject.Encode(envelope with { Accepted = accepted }));
            Equal(true, applied.AsSpan().SequenceEqual(reopened.Snapshot().Source));
            Equal("dimension", reopened.Envelope().Accepted[1].Edit!.Rail);
            Equal("span", reopened.Envelope().Accepted[1].Edit!.VertexId);
            reopened.Undo(Id());
            var bogus = envelope.Accepted.ToArray();
            bogus[1] = row with { Edit = dimension with { VertexId = "not-a-dimension" } };
            using var refused = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => refused.Reopen(NativeProject.Encode(envelope with { Accepted = bogus })));
        });
        Check("Recovery_Dimension_Refused", () =>
        {
            using var session = OpenedExample();
            string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2");
            session.ReviseOrdinate(draft, 0, 0.01);
            var recovery = session.CaptureRecovery();
            byte[] before = session.Snapshot().Source.ToArray();
            var bad = session.Envelope() with { Recovery = recovery with { Rail = "dimension", VertexId = "span" } };
            using var reopened = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => reopened.Reopen(NativeProject.Encode(bad)));
            Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(draft, session.Snapshot().Draft!.Id);
        });
        Check("ApplyChord_NewFoilRootX12_AcceptedBothNumbers", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            double tip = WingEstimates.ChordMeters(before, 1);
            var outcome = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.2)));
            var report = outcome.Report;
            Equal("root-chord", report.Dimension);
            Equal(ChordDimension.RootFlat, report.Rule);
            Equal(false, report.FitAboveLimit);
            Equal(10e-6, report.ToleranceMeters);
            True(report.FitResidualMeters > 1e-6 && report.FitResidualMeters <= 10e-6, "residual " + report.FitResidualMeters);
            True(report.DeviationFromLinearMeters > 1e-4, "deviation " + report.DeviationFromLinearMeters);
            True(report.PlanformShiftMeters > 1e-4, "shift " + report.PlanformShiftMeters);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
            WithinUm(tip, WingEstimates.ChordMeters(session.Snapshot().Source, 1), 0.01);
        });
        Check("ApplyDimension_FitJustBelowLimit_Accepted", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, Factor(9.9)))).Report;
            Equal(false, report.FitAboveLimit);
            True(report.FitResidualMeters > 5e-6 && report.FitResidualMeters < 10e-6, "residual " + report.FitResidualMeters);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
        });
        Check("ApplyDimension_FitJustAboveLimit_AcceptedWithWarning", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, Factor(10.1)))).Report;
            Equal(true, report.FitAboveLimit);
            Equal(10e-6, report.ToleranceMeters);
            True(report.FitResidualMeters > 10e-6 && report.FitResidualMeters < 20e-6, "residual " + report.FitResidualMeters);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
        });
        Check("ApplyDimension_NewFoilRootX15_AcceptedWithFitWarning", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.5))).Report;
            Equal(ChordDimension.RootFlat, report.Rule);
            Equal(true, report.FitAboveLimit);
            Equal(10e-6, report.ToleranceMeters);
            True(report.FitResidualMeters > 10e-6 && report.FitResidualMeters < 100e-6, "residual " + report.FitResidualMeters);
            True(report.DeviationFromLinearMeters > 1e-3, "deviation " + report.DeviationFromLinearMeters);
            True(report.PlanformShiftMeters > 1e-3, "shift " + report.PlanformShiftMeters);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
        });
        Check("ApplyDimension_RootChordShift_P0EqualsP1BitsZero", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.2)));
            var definition = FoilSource.Parse(session.Snapshot().Source).Definition!;
            ulong zero = BitConverter.DoubleToUInt64Bits(+0d);
            var leading = definition.Curves["leading"];
            var trailing = definition.Curves["trailing"];
            Equal(zero, Bits(leading.Points[0][1]));
            Equal(Bits(leading.Points[0][1]), Bits(leading.Points[1][1]));
            Equal(Bits(trailing.Points[0][1]), Bits(trailing.Points[1][1]));
        });
        Check("ApplyDimension_TipChord_NoShiftRootChordUnchanged", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            double root = WingEstimates.ChordMeters(before, 0);
            var report = session.ApplyChord(Id(), new("tip-chord", ScaledMillimetres(before, 1, 0.8))).Report;
            Equal(0d, report.PlanformShiftMeters);
            Equal(false, report.FitAboveLimit);
            WithinUm(root, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 1), 0.01);
        });
        Check("ApplyDimension_LocksOff_LinearRuleExact", () =>
        {
            using var session = OpenedBytes(GrevilleChord(450, _ => 120, ""));
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.2))).Report;
            Equal(ChordDimension.Linear, report.Rule);
            Equal(false, report.FitAboveLimit);
            True(report.FitResidualMeters < 1e-8, "residual " + report.FitResidualMeters);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
        });
        Check("ApplyDimension_OneRailLocked_ResidualReported", () =>
        {
            using var session = OpenedBytes(WithLocks(FoilSource.NewDefault(), "    root_mirror leading\n"));
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.2))).Report;
            Equal(ChordDimension.RootFlat, report.Rule);
            True(report.FitResidualMeters > 1e-9 && double.IsFinite(report.DeviationFromLinearMeters), "residual " + report.FitResidualMeters);
        });
        Check("ApplyDimension_Cad17Taper_ChordRuleWithinTolerance", () =>
        {
            using var session = OpenedBytes(GrevilleChord(500, eta => 200 - 150 * eta, ""));
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.2))).Report;
            Equal(ChordDimension.Linear, report.Rule);
            Equal(false, report.FitAboveLimit);
            True(report.FitResidualMeters <= 10e-6, "residual " + report.FitResidualMeters);
            WithinUm(report.TypedMeters, WingEstimates.ChordMeters(session.Snapshot().Source, 0), 0.01);
        });
        Check("ApplyDimension_Refused_HistoryUnchanged", () =>
        {
            using var session = OpenedNew();
            RefuseChord(session, "DSL-INVALID-NUMERIC", "root-chord", "12abc");
            RefuseChord(session, "DSL-UNIT", "root-chord", "0");
            RefuseChord(session, "DSL-UNIT", "root-chord", "-5");
            RefuseChord(session, "DSL-TARGET", "camber", "10");
        });
        Check("ApplyDimension_TipChordClosingTip_DslTarget", () =>
        {
            string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
            int close = text.LastIndexOf('}');
            byte[] closing = Encoding.UTF8.GetBytes(text[..close] + "  tip point\n" + text[close..]);
            Refuses("DSL-TARGET", () => ChordDimension.Evaluate(closing, new("tip-chord", "80")));
        });
        Check("ApplyDimension_EdgesWouldCross_DslEdgesCross", () =>
        {
            using var session = OpenedBytes(WithLocks(FoilSource.NewDefault(), "    root_mirror leading\n"));
            byte[] before = session.Snapshot().Source.ToArray();
            int accepted = session.Envelope().Accepted.Length;
            int cursors = session.Envelope().Cursors.Length;
            Refuses("DSL-EDGES-CROSS", () => session.ApplyChord(Id(), new("root-chord", "0.01")));
            Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(accepted, session.Envelope().Accepted.Length);
            Equal(cursors, session.Envelope().Cursors.Length);
        });
        Check("ChordRefit_SyntheticLinearRows_HeldExactly", () =>
        {
            var curve = FoilSource.Parse(FoilSourceTests.Example).Definition!.Curves["leading"];
            var row = new double[curve.Points.Length];
            row[2] = 1;
            double[] solved = ChordDimension.FitOrdinates(curve, _ => 0d, false, [(row, 0.01)]);
            Equal(0d, solved[0]);
            Equal(0d, solved[^1]);
            True(Math.Abs(solved[2] - 0.01) < 1e-9, "cv-2 " + solved[2]);
        });
        Check("ApplyDimension_Receipt_CarriesRuleId", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            string id = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.2))).AcceptedId;
            var edit = session.Envelope().Accepted.Single(row => row.Id == id).Edit!;
            Equal("dimension", edit.Rail);
            Equal("root-chord", edit.VertexId);
            Equal(ChordDimension.RootFlat, edit.Rule);
        });
        Check("BlendRule_LockStateToRuleId_Pinned", () =>
        {
            using var locked = OpenedNew();
            byte[] lockedBytes = locked.Snapshot().Source.ToArray();
            string lockedId = locked.ApplyChord(Id(), new("root-chord", ScaledMillimetres(lockedBytes, 0, 1.2))).AcceptedId;
            Equal(ChordDimension.RootFlat, locked.Envelope().Accepted.Single(row => row.Id == lockedId).Edit!.Rule);
            using var free = OpenedBytes(GrevilleChord(450, _ => 120, ""));
            byte[] freeBytes = free.Snapshot().Source.ToArray();
            string freeId = free.ApplyChord(Id(), new("root-chord", ScaledMillimetres(freeBytes, 0, 1.2))).AcceptedId;
            Equal(ChordDimension.Linear, free.Envelope().Accepted.Single(row => row.Id == freeId).Edit!.Rule);
        });
        Check("ApplyChord_FitAboveLimit_ApplyEventCarriesFitAndWarning", () =>
        {
            using var session = OpenedNew();
            byte[] before = session.Snapshot().Source.ToArray();
            var report = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(before, 0, 1.5))).Report;
            var apply = session.ReadLocalEvents().Last(item => item.Operation == "document.apply" && item.Outcome == "OK");
            Equal("dimension", apply.EditKind);
            Equal(true, apply.FitAboveLimit);
            Equal(report.FitResidualMeters * 1e6, apply.FitMicrometres);
            Equal(report.DeviationFromLinearMeters * 1e6, apply.DeviationMicrometres);
            Equal(report.PlanformShiftMeters * 1e6, apply.ShiftMicrometres);
        });
        Check("Reopen_ChordRows_UndoRedoRoundTrip", () =>
        {
            using var session = OpenedNew();
            byte[] original = session.Snapshot().Source.ToArray();
            string id = session.ApplyChord(Id(), new("root-chord", ScaledMillimetres(original, 0, 1.2))).AcceptedId;
            byte[] patched = session.Snapshot().Source.ToArray();
            string rule = session.Envelope().Accepted.Single(row => row.Id == id).Edit!.Rule!;
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, patched.AsSpan().SequenceEqual(session.Snapshot().Source));
            byte[] saved = session.SaveImage();
            using var reopened = new AuthoringSession();
            reopened.Reopen(saved);
            Equal(true, patched.AsSpan().SequenceEqual(reopened.Snapshot().Source));
            Equal(rule, reopened.Envelope().Accepted.Single(row => row.Id == id).Edit!.Rule);
            reopened.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(reopened.Snapshot().Source));
        });
        Check("Reopen_ForgedRuleValue_DocReference", () =>
        {
            using var session = OpenedExample();
            string id = session.ApplyDimension(Id(), new("span", "1350"));
            var envelope = session.Envelope();
            var row = envelope.Accepted.Single(item => item.Id == id);
            var accepted = envelope.Accepted.ToArray();
            int index = Array.FindIndex(accepted, item => item.Id == id);
            accepted[index] = row with { Edit = row.Edit! with { VertexId = "root-chord", Rule = "not-a-rule" } };
            using var reopened = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => reopened.Reopen(NativeProject.Encode(envelope with { Accepted = accepted })));
        });
        Check("Reopen_ChordRowWithoutRule_DocReference", () =>
        {
            using var session = OpenedExample();
            string id = session.ApplyDimension(Id(), new("span", "1350"));
            var envelope = session.Envelope();
            var row = envelope.Accepted.Single(item => item.Id == id);
            var accepted = envelope.Accepted.ToArray();
            int index = Array.FindIndex(accepted, item => item.Id == id);
            accepted[index] = row with { Edit = row.Edit! with { VertexId = "root-chord" } };
            using var reopened = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => reopened.Reopen(NativeProject.Encode(envelope with { Accepted = accepted })));
        });
        Check("Reopen_RetrySameDimensionOperationId_ReturnsPriorId", () =>
        {
            using var session = Opened();
            string operation = Id();
            string first = session.ApplyDimension(operation, new("span", "1350"));
            byte[] saved = session.SaveImage();
            using var reopened = new AuthoringSession();
            reopened.Reopen(saved);
            string second = reopened.ApplyDimension(operation, new("span", "1350"));
            Equal(first, second);
            var spanEvent = reopened.ReadLocalEvents().Last(item => item.Operation == "document.apply" && item.Outcome == "OK");
            Equal(null, spanEvent.FitMicrometres);
            Equal(null, spanEvent.DeviationMicrometres);
            Equal(null, spanEvent.ShiftMicrometres);
            Equal(null, spanEvent.FitAboveLimit);
            Refuses("DOC-OPERATION-CONFLICT", () => reopened.ApplyDimension(operation, new("span", "1400")));
            string chordOp = Id();
            var outcome = reopened.ApplyChord(chordOp, new("root-chord", "152.09"));
            byte[] image = reopened.SaveImage();
            using var again = new AuthoringSession();
            again.Reopen(image);
            var replay = again.ApplyChord(chordOp, new("root-chord", "152.090"));
            Equal(outcome.AcceptedId, replay.AcceptedId);
            Equal(outcome.Report.TypedMeters, replay.Report.TypedMeters);
        });
    }

    private static string Id() => Guid.NewGuid().ToString("D");

    private static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Open(ThreeStation(), Id(), true);
        return session;
    }

    private static AuthoringSession OpenedExample()
    {
        var session = new AuthoringSession();
        session.Open(FoilSourceTests.Example, Id(), true);
        return session;
    }

    private static byte[] ThreeStation() => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(FoilSourceTests.Example)
        .Replace("sections { at root profile \"section-a\" at tip profile \"section-a\" }",
            "sections { at root profile \"section-a\" at 225 mm profile \"section-a\" at tip profile \"section-a\" }"));

    private static double[] Chords(byte[] source)
    {
        var samples = new double[201];
        for (int i = 0; i < samples.Length; i++) samples[i] = WingEstimates.ChordMeters(source, i / 200d);
        return samples;
    }

    private static double[] LockEta(byte[] source) =>
        FoilSource.Parse(source).Authored().Constraints.Where(item => item.Kind == "value").Select(item => item.Eta!.Value).ToArray();

    private static void Near(double expected, double actual)
    {
        double scale = Math.Max(1e-15, Math.Abs(expected));
        if (Math.Abs(actual - expected) / scale > 1e-6)
            throw new InvalidOperationException($"Expected {expected}; actual {actual}");
    }

    private static void NearEta(double expected, double actual)
    {
        if (Math.Abs(expected - actual) > 1e-9)
            throw new InvalidOperationException($"Expected eta {expected}; actual {actual}");
    }

    private static void RefuseUnchanged(AuthoringSession session, string code, string text)
    {
        var before = session.Snapshot();
        int accepted = session.Envelope().Accepted.Length;
        int cursors = session.Envelope().Cursors.Length;
        Refuses(code, () => session.ApplyDimension(Id(), new(Name: "span", Text: text)));
        var after = session.Snapshot();
        Equal(before.AcceptedId, after.AcceptedId);
        Equal(true, before.Source.AsSpan().SequenceEqual(after.Source));
        Equal(null, after.Draft);
        Equal(accepted, session.Envelope().Accepted.Length);
        Equal(cursors, session.Envelope().Cursors.Length);
    }

    private static AuthoringSession OpenedNew()
    {
        var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Id(), true);
        return session;
    }

    private static AuthoringSession OpenedBytes(byte[] source)
    {
        var session = new AuthoringSession();
        session.Open(source, Id(), true);
        return session;
    }

    // Spike linearity on the New foil: trailing residual is about 45.05 µm per unit of |f-1|.
    // 9.9 µm stays under the 10 µm limit; 10.1 µm sits strictly between 10 µm and 20 µm.
    private const double PerUnitMicrons = 45.05;
    private static double Factor(double microns) => 1 + microns / PerUnitMicrons;

    private static string ScaledMillimetres(byte[] source, double eta, double factor) =>
        (WingEstimates.ChordMeters(source, eta) * factor * 1000d).ToString("G17", CultureInfo.InvariantCulture);

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void WithinUm(double expected, double actual, double micrometres)
    {
        if (Math.Abs(actual - expected) > micrometres * 1e-6)
            throw new InvalidOperationException($"expected {expected} m, actual {actual} m");
    }

    private static ulong Bits(double value) => BitConverter.DoubleToUInt64Bits(value);

    private static void RefuseChord(AuthoringSession session, string code, string name, string text)
    {
        byte[] before = session.Snapshot().Source.ToArray();
        string acceptedId = session.Snapshot().AcceptedId;
        int accepted = session.Envelope().Accepted.Length;
        int cursors = session.Envelope().Cursors.Length;
        Refuses(code, () => session.ApplyChord(Id(), new(name, text)));
        Equal(acceptedId, session.Snapshot().AcceptedId);
        Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
        Equal(accepted, session.Envelope().Accepted.Length);
        Equal(cursors, session.Envelope().Cursors.Length);
    }

    private static byte[] WithLocks(byte[] source, string body)
    {
        string text = Encoding.UTF8.GetString(source);
        int close = text.LastIndexOf('}');
        return Encoding.UTF8.GetBytes(text[..close] + "  locks {\n" + body + "  }\n" + text[close..]);
    }

    private static byte[] GrevilleChord(double halfSpanMillimetres, Func<double, double> trailingMillimetres, string locksBody)
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
        double[] knots = [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1];
        const int degree = 3;
        int count = knots.Length - degree - 1;
        var abscissa = new double[count];
        for (int index = 0; index < count; index++)
        {
            double sum = 0;
            for (int offset = 1; offset <= degree; offset++) sum += knots[index + offset];
            abscissa[index] = sum / degree;
        }
        string knotText = string.Join(", ", knots.Select(value => FoilSource.ExactDecimal(value)));
        string Points(Func<double, double> ordinateMillimetres) => string.Join(", ", abscissa.Select(x =>
            "(" + FoilSource.ExactDecimal(x) + ", " + FoilSource.ExactDecimal(ordinateMillimetres(x) / 1000d, -3) + ")"));
        string leading = "leading cv { degree 3 knots [" + knotText + "] points [" + Points(_ => 0) + "] }";
        string trailing = "trailing cv { degree 3 knots [" + knotText + "] points [" + Points(trailingMillimetres) + "] }";
        const string oldLeading = "leading cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }";
        const string oldTrailing = "trailing cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 120), (0.1, 120), (0.3, 120), (0.5, 120), (0.7, 120), (0.9, 120), (1, 120)] }";
        text = text.Replace(oldLeading, leading, StringComparison.Ordinal).Replace(oldTrailing, trailing, StringComparison.Ordinal);
        text = text.Replace("half_span 450 mm", "half_span " + halfSpanMillimetres.ToString("G17", CultureInfo.InvariantCulture) + " mm", StringComparison.Ordinal);
        return WithLocks(Encoding.UTF8.GetBytes(text), locksBody);
    }
}
