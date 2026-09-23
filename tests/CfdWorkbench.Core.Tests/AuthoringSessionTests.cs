using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class AuthoringSessionTests
{
    private static string Id() => Guid.NewGuid().ToString("D");
    private static AuthoringSession Opened()
    { var session = new AuthoringSession(); session.Open(FoilSourceTests.Example, Id(), true); return session; }
    internal static void Run()
    {
        Check("Session_Preview_HasOwnedBindingAndMeasuredRedactedEvents", () =>
        {
            using var session = new AuthoringSession(); const string marker = "PRIVATE-SOURCE-MARKER";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(FoilSourceTests.Example).Replace("Basic foil", marker));
            session.Open(bytes, Id(), true); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            var preview = session.Preview(draft, 0, .5, .5, true); Equal(draft, preview.Binding.DraftId);
            var events = session.ReadLocalEvents(); var measured = events.Last(item => item.Operation == "geometry.preview");
            Equal(0L, measured.Generation); Equal(true, measured.TraceId is not null); Equal(true, measured.DurationMilliseconds >= 0);
            Equal(true, events.Any(item => item.Operation == "geometry.validate" && item.Evaluator == "cfdw-cv/1"));
            Equal(false, System.Text.Json.JsonSerializer.Serialize(events).Contains(marker, StringComparison.Ordinal));
            Equal(false, System.Text.Json.JsonSerializer.Serialize(events).Contains(draft, StringComparison.Ordinal));
        });
        Check("Session_CancelledValidation_TelemetryIsNotSuccess", () =>
        {
            using var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            session.Validate(draft, 0, new CancellationToken(true));
            Equal("DSL-CANCELLED", session.ReadLocalEvents().Last().Outcome);
        });
        Check("Session_OperationIdDifferentPayload_Conflicts", () =>
        {
            using var session = Opened(); string draft = Id(), operation = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); var assessment = session.Validate(draft, 0); session.Apply(operation, assessment);
            Refuses("DOC-OPERATION-CONFLICT", () => session.Undo(operation));
        });
        Check("Session_Telemetry_EmitsNamedInnerPhases", () =>
        {
            var session = Opened(); var events = session.ReadLocalEvents();
            Equal(true, events.Any(item => item.Operation == "language.parse"));
            Equal(true, events.Any(item => item.Operation == "identity.canonicalize"));
            Equal(true, events.Any(item => item.Operation == "geometry.validate"));
        });
        Check("Session_CancelledDraftId_CannotReuseOldAuthority", () =>
        {
            var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            _ = session.Validate(draft, 0); session.Cancel(draft);
            Refuses("DSL-DRAFT-REUSED", () => session.BeginRailEdit(draft, "leading", "cv-2"));
        });
        Check("Session_PublicSizeCap_CannotExceedReadableFormat", () => Refuses("DOC-SIZE", () => new AuthoringSession(NativeProject.MaxBytes + 1)));
        Check("Session_EmptyRecovery_RoundtripsWithoutAdoption", () =>
        {
            var session = Opened(); var envelope = session.Envelope(); string draft = Id();
            envelope = envelope with { Recovery = new(draft, session.Snapshot().AcceptedId, 0, "leading", "cv-2", []) };
            var reopened = new AuthoringSession(); reopened.Reopen(NativeProject.Encode(envelope)); reopened.ResumeRecovery();
            Equal(0, reopened.Snapshot().Draft!.Bytes.Length);
            Refuses("DSL-NOT-ASSESSED", () => reopened.Apply(Id(), reopened.Validate(draft, 0)));
        });
        Check("Session_UncapturedSaveAcknowledgement_Refuses", () =>
        {
            var session = Opened();
            Refuses("DOC-SAVE-CAPTURE", () => session.AcknowledgeSaved(NativeProject.Encode(session.Envelope())));
        });
        Check("Session_LocalTelemetry_BoundedAndNoSourceIdentity", () =>
        {
            using var session = Opened();
            for (int i = 0; i < 270; i++) _ = session.Snapshot();
            var events = session.ReadLocalEvents(); Equal(256, events.Count);
            Equal(true, events.All(item => item.DurationMilliseconds >= 0));
            Equal(true, events.All(item => item.Operation is "document.snapshot"));
            Equal(false, System.Text.Json.JsonSerializer.Serialize(events).Contains(session.Snapshot().SourceHash, StringComparison.Ordinal));
        });
        Check("Session_Close_DiscardsTelemetryAndRefusesMutation", () =>
        {
            var session = Opened(); session.Dispose(); Equal(0, session.ReadLocalEvents().Count);
            Refuses("DOC-CLOSED", () => session.BeginRailEdit(Id(), "leading", "cv-2"));
        });
        Check("Session_EnvelopeOutwardMutation_LeavesOwnedFacts", () =>
        {
            var session = Opened(); var before = session.SaveImage();
            session.Envelope().Sources[0].Utf8Base64Chunks[0] = "";
            Equal(true, before.AsSpan().SequenceEqual(session.SaveImage()));
        });
        Check("Session_ConcurrentSameGeneration_ExactlyOneMutation", () =>
        {
            var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            int successes = 0, conflicts = 0;
            Parallel.For(0, 2, i => { try { session.UpdateDraft(draft, 0, .01 + i * .01); Interlocked.Increment(ref successes); }
                catch (ContractError error) when (error.Code == "DSL-CONFLICT") { Interlocked.Increment(ref conflicts); } });
            Equal(1, successes); Equal(1, conflicts); Equal(1L, session.Snapshot().Draft!.Generation);
        });
        Check("Session_RecoveryRoundtrip_OfferedBeforeResume", () =>
        {
            var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, .01);
            session.CaptureRecovery(); var reopened = new AuthoringSession(); reopened.Reopen(session.SaveImage());
            Equal(true, reopened.Snapshot().Draft is null); Equal(draft, reopened.Snapshot().Recovery!.DraftId);
            reopened.ResumeRecovery(); Equal(1L, reopened.Snapshot().Draft!.Generation); reopened.Cancel(draft);
            Equal(true, reopened.Snapshot().Recovery is null);
        });
        Check("Session_LateSaveAcknowledgement_RemainsDirty", () =>
        {
            var session = Opened(); byte[] saved = session.SaveImage(); string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, .01); session.Apply(Id(), session.Validate(draft, 1));
            session.AcknowledgeSaved(saved); Equal(true, session.Snapshot().Dirty);
        });
        Check("Session_HistoryGrowthRefusal_IsAtomic", () =>
        {
            var baseline = Opened().SaveImage().Length; var session = new AuthoringSession(baseline + 200);
            session.Open(FoilSourceTests.Example, Id(), true); string before = session.Snapshot().AcceptedId, draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, .01);
            Refuses("DOC-SIZE", () => session.Apply(Id(), session.Validate(draft, 1)));
            Equal(before, session.Snapshot().AcceptedId); Equal(1L, session.Snapshot().Draft!.Generation);
        });
        Check("Native_FabricatedRedo_RefusesWholeImage", () =>
        {
            var session = Opened(); var envelope = session.Envelope();
            var bad = envelope with { Cursors = [.. envelope.Cursors, new(1, envelope.Accepted[0].Id, "redo", Id())] };
            var reopened = new AuthoringSession(); Refuses("DOC-REFERENCE", () => reopened.Reopen(NativeProject.Encode(bad)));
            Refuses("DOC-EMPTY", () => reopened.Snapshot());
        });
        Check("Native_UnknownField_RefusesWritableAdoption", () =>
        {
            string image = System.Text.Encoding.UTF8.GetString(Opened().SaveImage());
            Refuses("DOC-UNSUPPORTED-FIELD", () => new AuthoringSession().Reopen(System.Text.Encoding.UTF8.GetBytes("{\"future\":1," + image[1..])));
        });
        Check("Native_DuplicateKey_Refuses", () =>
        {
            string image = System.Text.Encoding.UTF8.GetString(Opened().SaveImage());
            Refuses("DOC-SCHEMA", () => new AuthoringSession().Reopen(System.Text.Encoding.UTF8.GetBytes("{\"format\":\"cfdw-project-1\"," + image[1..])));
        });
        Check("Session_IdCandidate_NeedsExplicitAcceptance", () =>
        {
            var session = new AuthoringSession();
            var candidate = session.Open(FoilSourceTests.Example, Id(), false);
            Equal(false, candidate.AsSpan().SequenceEqual(FoilSourceTests.Example));
            Refuses("DOC-EMPTY", () => session.Snapshot());
        });
        Check("Session_Cancel_LeavesAcceptedBytesAndHistory", () =>
        {
            var session = Opened(); var original = session.SaveImage();
            string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            session.UpdateDraft(draft, 0, .01); session.Cancel(draft);
            Equal(true, original.AsSpan().SequenceEqual(session.SaveImage()));
        });
        Check("Session_StaleGeneration_LeavesDraftUnchanged", () =>
        {
            var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            session.UpdateDraft(draft, 0, .01);
            Refuses("DSL-CONFLICT", () => session.UpdateDraft(draft, 0, .02));
            Equal(1L, session.Snapshot().Draft!.Generation);
        });
        Check("Session_OutwardDraftMutation_CannotRetargetOrRewrite", () =>
        {
            var session = Opened(); string draft = Id(); var view = session.BeginRailEdit(draft, "leading", "cv-2");
            view.Bytes[0] = 0;
            Equal((byte)'f', session.Snapshot().Draft!.Bytes[0]);
            Refuses("DSL-DRAFT-OWNED", () => session.BeginRailEdit(draft, "trailing", "cv-3"));
        });
        Check("Session_ApplyRetryAfterUndoAndReopen_DoesNotMoveCursor", () =>
        {
            var session = Opened(); string root = session.Snapshot().AcceptedId; string draft = Id(), operation = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, .01);
            var assessment = session.Validate(draft, 1); string applied = session.Apply(operation, assessment);
            session.Undo(Id()); var reopened = new AuthoringSession(); reopened.Reopen(session.SaveImage());
            Equal(applied, reopened.Apply(operation, assessment)); Equal(root, reopened.Snapshot().AcceptedId);
            Equal(applied, reopened.Redo(Id()));
        });
        Check("Session_OldAssessment_AfterDraftUpdateCannotApply", () =>
        {
            var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            session.UpdateDraft(draft, 0, .01); var assessment = session.Validate(draft, 1);
            session.UpdateDraft(draft, 1, .02);
            Refuses("DSL-CONFLICT", () => session.Apply(Id(), assessment));
        });
        Check("Session_CancelledValidation_CannotApply", () =>
        {
            var session = Opened(); string draft = Id(); session.BeginRailEdit(draft, "leading", "cv-2");
            var assessment = session.Validate(draft, 0, new CancellationToken(true));
            Refuses("DSL-NOT-ASSESSED", () => session.Apply(Id(), assessment));
        });
        Check("Session_InvalidGeometry_LeavesAcceptedState", () =>
        {
            var session = Opened(); string root = session.Snapshot().AcceptedId, draft = Id();
            session.BeginRailEdit(draft, "trailing", "cv-2"); session.UpdateDraft(draft, 0, -100);
            var assessment = session.Validate(draft, 1);
            Refuses("DSL-NOT-ASSESSED", () => session.Apply(Id(), assessment));
            Equal(root, session.Snapshot().AcceptedId);
        });
    }
}
