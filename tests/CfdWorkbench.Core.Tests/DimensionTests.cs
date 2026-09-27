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
        // Red-first: before EditReference learned "dimension", Reopen threw DOC-REFERENCE
        // (docs/proof/c1-red-runs.md). A span receipt now reopens. An unknown dimension name still refuses DOC-REFERENCE.
        Check("Receipt_Dimension_OldReaderRefusesDocReference", () =>
        {
            using var session = OpenedExample();
            string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2");
            session.UpdateDraft(draft, 0, 0.01);
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
            session.UpdateDraft(draft, 0, 0.01);
            var recovery = session.CaptureRecovery();
            byte[] before = session.Snapshot().Source.ToArray();
            var bad = session.Envelope() with { Recovery = recovery with { Rail = "dimension", VertexId = "span" } };
            using var reopened = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => reopened.Reopen(NativeProject.Encode(bad)));
            Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(draft, session.Snapshot().Draft!.Id);
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
}
