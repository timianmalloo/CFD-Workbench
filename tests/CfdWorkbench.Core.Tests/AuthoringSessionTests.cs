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
        Check("Ruling16_PublicConsumer_CustomIdsAssignmentsAndOwnedInspection", () =>
        {
            // Consumer discovers targets through public DTOs, never internal Definition or reparsing bytes.
            byte[] candidate = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
            string text = System.Text.Encoding.UTF8.GetString(candidate).Replace("cv-2", "rail-middle-custom").Replace("Basic foil", "My authored foil").Replace("section-a", "Custom profile");
            using var session = new AuthoringSession(); session.Open(System.Text.Encoding.UTF8.GetBytes(text), Id(), false);
            var old = session.InspectAccepted(); Equal("Accepted", old.Authored.Binding.State); Equal("My authored foil", old.Authored.Name);
            Equal("mm", old.Authored.SourceUnit); Equal(.45, old.Authored.HalfSpanMeters);
            var leading = old.Authored.Rails.Single(r => r.Name == "leading"); var selected = leading.Controls.Single(c => c.Id == "rail-middle-custom");
            Equal(.3, selected.Eta); Equal(0d, selected.OrdinateSi); Equal(true, selected.Editable);
            Equal(false, leading.Controls[0].Editable); Equal(true, leading.Controls[0].ApplicableLocks.Contains("root_mirror"));
            Equal("Custom profile", old.Authored.Assignments[1].ProfileName); Equal(.45, old.Authored.Assignments[1].SpanMeters);
            Equal(old.Authored.Binding.SourceHash, old.Geometry.Certificate!.SourceHash);
            _ = Geometry.PointAt(old.Geometry.Certificate, .4, .5, true);
            string draft = Id(); session.BeginRailEdit(draft, leading.Name, selected.Id); session.UpdateDraft(draft, 0, .001);
            var view = session.InspectDraft(); Equal(draft, view.Binding.DraftId); Equal(1L, view.Binding.Generation); Equal("Draft", view.Binding.State);
            Equal(.001, view.Rails.Single(r => r.Name == "leading").Controls.Single(c => c.Id == selected.Id).OrdinateSi);
            _ = session.InspectAccepted(); Equal(draft, session.Snapshot().Draft!.Id);
            Equal(0d, old.Authored.Rails[0].Controls[2].OrdinateSi); Equal(old.Authored.Binding.AcceptedId, session.Snapshot().AcceptedId);
            RefusesCollectionMutation(old.Authored.Rails); RefusesCollectionMutation(leading.Controls);
            RefusesCollectionMutation(leading.Controls[0].ApplicableLocks); RefusesCollectionMutation(old.Authored.Assignments);
            RefusesCollectionMutation(old.Authored.Constraints); RefusesCollectionMutation(old.Authored.Constraints[0].Values);
            var replaced = selected with { Id = "forged", OrdinateSi = 99 }; Equal("forged", replaced.Id);
            Equal(selected.Id, session.Snapshot().Draft!.VertexId);
        });
        Check("Ruling16_MissingIdsAreUnaccepted_AndInvalidSourceHasDiagnostics", () =>
        {
            var candidate = FoilSource.Parse(FoilSourceTests.Example).Authored(); Equal("IdCandidate", candidate.Binding.State);
            Equal(true, candidate.Rails.All(r => r.Controls.All(c => !c.Editable)));
            var invalid = FoilSource.Parse([0xff]).Authored(); Equal("Invalid", invalid.Binding.State); Equal(0, invalid.Rails.Count);
            Equal("DSL-LEX", invalid.Diagnostics[0].Code); Equal(null, invalid.HalfSpanMeters);
        });
        Check("Ruling16_IncompleteRecovery_DiagnosticsRemainSourceAndGenerationBound", () =>
        {
            using var original = Opened(); var envelope = original.Envelope(); string draft = Id();
            byte[] incomplete = System.Text.Encoding.UTF8.GetBytes("foildsl \"4.0\" foil");
            envelope = envelope with { Recovery = new(draft, original.Snapshot().AcceptedId, 4, "leading", "cv-2", AuthoringSession.Chunks(incomplete)) };
            using var session = new AuthoringSession(); session.Reopen(NativeProject.Encode(envelope)); session.ResumeRecovery();
            string accepted = session.Snapshot().AcceptedId; var projection = session.InspectDraft(); var assessment = session.Validate(draft, 4);
            Equal("Draft", projection.Binding.State); Equal(0, projection.Rails.Count); Equal(null, projection.HalfSpanMeters);
            Equal("DSL-SYNTAX", assessment.Code); Equal(Identity.Sha256(incomplete), assessment.SourceBinding!.SourceHash);
            Equal(draft, assessment.SourceBinding.DraftId); Equal(4L, assessment.SourceBinding.Generation); Equal(accepted, assessment.SourceBinding.BaseAcceptedId);
            Equal(null, assessment.Certificate); Equal(null, assessment.Key); Equal(true, assessment.Diagnostics.Count > 0);
            Equal(incomplete.Length, assessment.Diagnostics[0].ByteStart); Equal(true, assessment.Diagnostics[0].Recovery.Length > 0);
            RefusesCollectionMutation(assessment.Diagnostics); Equal(accepted, session.Snapshot().AcceptedId);
        });
        Check("Ruling16_OldAcceptedProjection_RemainsBoundAfterApply", () =>
        {
            using var session = Opened(); var old = session.InspectAccepted(); string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, .001); session.Apply(Id(), session.Validate(draft, 1));
            var current = session.InspectAccepted(); Equal(false, old.Authored.Binding.AcceptedId == current.Authored.Binding.AcceptedId);
            Equal(false, old.Authored.Binding.SourceHash == current.Authored.Binding.SourceHash);
            Equal(0d, old.Authored.Rails[0].Controls[2].OrdinateSi); Equal(.001, current.Authored.Rails[0].Controls[2].OrdinateSi);
        });
        Check("Ruling16_AuthoredAssertionsAndExactSi_AreInspectionOnly", () =>
        {
            var parsed = FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/foil-assertions.foil")); var view = parsed.Authored();
            Equal("mm", view.DisplayUnit); Equal("area", view.Assertions[0].Metric); Equal("==", view.Assertions[0].Comparison);
            Equal(.108, view.Assertions[0].ValueSi); Equal("cm2", view.Assertions[0].DeclaredUnit); Equal(.0000001, view.Assertions[0].ToleranceSi);
            RefusesCollectionMutation(view.Assertions);
            var trailing = view.Rails.Single(rail => rail.Name == "trailing").Controls[0];
            Equal(.12, trailing.OrdinateSi); Equal("1080863910568919", trailing.ExactOrdinateSi.Numerator); Equal("9007199254740992", trailing.ExactOrdinateSi.Denominator);
            Equal(null, Geometry.Assess(parsed).Certificate);
        });
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
    private static void RefusesCollectionMutation<T>(IReadOnlyList<T> values)
    {
        bool refused = false;
        try { ((IList<T>)values).Clear(); } catch (NotSupportedException) { refused = true; }
        Equal(true, refused);
    }
}
