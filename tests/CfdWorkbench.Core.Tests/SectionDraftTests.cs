using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// M1.2c section draft in Core (docs/design/m12c-section-editor.md §3.3, §5.1, §9, §10, §12.4 SDR). Fixtures are
// shared-basis until GCRT merges. The SetType/SetTangent/InsertAnchor checks register when SPT merges (seam S-8).
internal static class SectionDraftTests
{
    internal static void Run()
    {
        Check("SectionDraft_Begin_CursorZeroBytesEqualBase", () =>
        {
            using var session = Opened();
            var accepted = session.Snapshot();
            var view = session.BeginSectionDraft(Id(), 0);
            Equal(0, view.Cursor);
            Equal(0, view.StepCount);
            Equal(0L, view.Generation);
            Same(accepted.Source, view.Bytes);
            Equal(accepted.AcceptedId, view.BaseAcceptedId);
            Equal("section-a", view.Profile);
            Equal(SectionScope.Shared, view.Scope);
            Equal(ThicknessIntent.KeepCurrent, view.Intent);
            Equal(true, view.Last is null);
        });
        Check("SectionDraft_StepThenUndoThenRedo_BytesRestoredExactly", () =>
        {
            using var session = Opened();
            string id = Id();
            var begun = session.BeginSectionDraft(id, 0);
            var stepped = session.ApplySectionStep(id, begun.Generation, Raise(begun, "cv-3", 0.01));
            Equal(false, stepped.Bytes.AsSpan().SequenceEqual(begun.Bytes));
            var undone = session.UndoSectionStep(id);
            Same(begun.Bytes, undone.Bytes);
            Same(begun.Bytes, session.Snapshot().Draft!.Bytes);
            Equal(0, undone.Cursor);
            Equal(1, undone.StepCount);
            var redone = session.RedoSectionStep(id);
            Same(stepped.Bytes, redone.Bytes);
            Same(stepped.Bytes, session.Snapshot().Draft!.Bytes);
            Equal(1, redone.Cursor);
            Equal(true, stepped.Generation > begun.Generation && undone.Generation > stepped.Generation && redone.Generation > undone.Generation);
        });
        Check("SectionDraft_StepAfterUndo_DropsRedoTail", () =>
        {
            using var session = Opened();
            string id = Id();
            var begun = session.BeginSectionDraft(id, 0);
            var first = session.ApplySectionStep(id, begun.Generation, Raise(begun, "cv-3", 0.01));
            var second = session.ApplySectionStep(id, first.Generation, Raise(first, "cv-4", 0.01));
            Equal(2, second.StepCount);
            var undone = session.UndoSectionStep(id);
            Equal(1, undone.Cursor);
            var branch = session.ApplySectionStep(id, undone.Generation, Raise(undone, "cv-2", -0.005));
            Equal(2, branch.Cursor);
            Equal(2, branch.StepCount);
            Equal(false, branch.Bytes.AsSpan().SequenceEqual(second.Bytes));
            var redo = session.RedoSectionStep(id);
            Equal(2, redo.Cursor);
            Same(branch.Bytes, redo.Bytes);
            Same(first.Bytes, session.UndoSectionStep(id).Bytes);
        });
        Check("SectionDraft_CursorEnds_UndoRedoNoChange", () =>
        {
            using var session = Opened();
            string id = Id();
            var begun = session.BeginSectionDraft(id, 0);
            var atStart = session.UndoSectionStep(id);
            Same(begun.Bytes, atStart.Bytes);
            Equal(0, atStart.Cursor);
            Equal(begun.Generation, atStart.Generation);
            Equal(begun.Generation, session.RedoSectionStep(id).Generation);
            var stepped = session.ApplySectionStep(id, begun.Generation, Raise(begun, "cv-3", 0.01));
            var atEnd = session.RedoSectionStep(id);
            Same(stepped.Bytes, atEnd.Bytes);
            Equal(1, atEnd.Cursor);
            Equal(stepped.Generation, atEnd.Generation);
        });
        Check("SectionDraft_SecondDraftWhileOpen_DslDraftOwned", () =>
        {
            using var session = Opened();
            string id = Id();
            session.BeginSectionDraft(id, 0);
            Refuses("DSL-DRAFT-OWNED", () => session.BeginSectionDraft(Id(), 1));
            Refuses("DSL-DRAFT-OWNED", () => session.BeginPointGesture(Id(), "leading", "cv-2"));
            session.Cancel(id);
            Refuses("DSL-DRAFT-REUSED", () => session.BeginSectionDraft(id, 0));
        });
        Check("SectionDraft_RefusedStep_DraftBytesUnchanged", () =>
        {
            using var session = Opened();
            string id = Id();
            var begun = session.BeginSectionDraft(id, 0);
            var stepped = session.ApplySectionStep(id, begun.Generation, Raise(begun, "cv-3", 0.01));
            var (x, y) = Point(stepped, SurfaceSide.Upper, "cv-3");
            Refuses("DSL-LOCK", () => session.ApplySectionStep(id, stepped.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-0", 0, 0.01)));
            Refuses("DSL-PROFILE-ORDER", () => session.ApplySectionStep(id, stepped.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", 0.99, y)));
            Refuses("DSL-LOCK", () => session.ApplySectionStep(id, stepped.Generation, new SectionStep.Delete(SurfaceSide.Lower, "cv-0")));
            Refuses("DSL-CONFLICT", () => session.ApplySectionStep(id, begun.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", x, y)));
            var draft = session.Snapshot().Draft!;
            Same(stepped.Bytes, draft.Bytes);
            Equal(stepped.Generation, draft.Generation);
            var view = session.UndoSectionStep(id);
            Equal(0, view.Cursor);
            Equal(1, view.StepCount);
        });
        Check("SectionDraft_FinishUncertified_Refused", () =>
        {
            using var session = Opened();
            int rows = session.Envelope().Accepted.Length;
            string id = Id();
            var begun = session.BeginSectionDraft(id, 0);
            var (x, _) = Point(begun, SurfaceSide.Upper, "cv-3");
            var crossed = session.ApplySectionStep(id, begun.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", x, -0.3));
            var assessment = session.AssessSection(id, crossed.Generation, CancellationToken.None);
            Equal(false, assessment.Status == GeometryStatus.Certified);
            Equal("DSL-PROFILE-CROSS", assessment.Code);
            Refuses("DSL-NOT-ASSESSED", () => session.FinishSection(Id(), assessment));
            Equal(rows, session.Envelope().Accepted.Length);
            Equal(id, session.Snapshot().Draft!.Id);
        });
        Check("SectionDraft_FinishBytesEqualBase_NoRow", () =>
        {
            using var session = Opened();
            var before = session.Envelope();
            string id = Id();
            var begun = session.BeginSectionDraft(id, 0);
            session.ApplySectionStep(id, begun.Generation, Raise(begun, "cv-3", 0.01));
            var back = session.UndoSectionStep(id);
            var assessment = session.AssessSection(id, back.Generation, CancellationToken.None);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal<string?>(null, session.FinishSection(Id(), assessment));
            var after = session.Envelope();
            Equal(before.Accepted.Length, after.Accepted.Length);
            Equal(before.Cursors.Length, after.Cursors.Length);
            Equal(true, session.Snapshot().Draft is null);
            Same(begun.Bytes, session.Snapshot().Source);
        });
        Check("SectionDraft_FinishSixSteps_OneAcceptedRow", () =>
        {
            using var session = Opened();
            var before = session.Envelope();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            foreach (string vertex in new[] { "cv-2", "cv-3", "cv-4", "cv-5", "cv-3", "cv-2" })
                view = session.ApplySectionStep(id, view.Generation, Raise(view, vertex, 0.002));
            Equal(6, view.Cursor);
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            Equal(GeometryStatus.Certified, assessment.Status);
            string? accepted = session.FinishSection(Id(), assessment);
            var after = session.Envelope();
            Equal(before.Accepted.Length + 1, after.Accepted.Length);
            Equal(before.Cursors.Length + 1, after.Cursors.Length);
            Equal(accepted, after.Accepted[^1].Id);
            Equal("section", after.Accepted[^1].Edit!.Rail);
            Equal("section-a", after.Accepted[^1].Edit!.VertexId);
            Same(view.Bytes, session.Snapshot().Source);
            Equal(true, session.Snapshot().Draft is null);
        });
        Check("SectionDraft_Cancel_UndoDepthAndBytesUnchanged", () =>
        {
            using var session = Opened();
            var before = session.Envelope();
            var snapshot = session.Snapshot();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            session.Cancel(id);
            var after = session.Envelope();
            Equal(before.Accepted.Length, after.Accepted.Length);
            Equal(before.Cursors.Length, after.Cursors.Length);
            Equal(snapshot.AcceptedId, session.Snapshot().AcceptedId);
            Same(snapshot.Source, session.Snapshot().Source);
            Equal(true, session.Snapshot().Draft is null);
            Equal(snapshot.AcceptedId, session.Undo(Id()));
        });
        Check("SectionDraft_MakeUniqueThenCancel_NoCopyLeft", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            Equal("section-a-i1", view.Profile);
            session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            session.Cancel(id);
            Same(original, session.Snapshot().Source);
            Equal(false, Encoding.UTF8.GetString(session.Snapshot().Source).Contains("section-a-i1", StringComparison.Ordinal));
            Equal(1, FoilSource.Parse(session.Snapshot().Source).Definition!.Profiles.Length);
        });
        Check("SectionDraft_FinishThenUndo_SourceAndAssignmentsRestored", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None));
            Equal("section-a-i1", Assigned(session.Snapshot().Source, 0));
            session.Undo(Id());
            Same(original, session.Snapshot().Source);
            Equal("section-a", Assigned(session.Snapshot().Source, 0));
            Equal("section-a", Assigned(session.Snapshot().Source, 1));
        });
        Check("SectionDraft_SharedFinish_BothAssignmentsReadNewProfile", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            double raised = Point(view, SurfaceSide.Upper, "cv-3").Y;
            session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None));
            byte[] source = session.Snapshot().Source;
            Equal("section-a", Assigned(source, 0));
            Equal("section-a", Assigned(source, 1));
            Equal(raised, session.ProfileAt(0).Upper.Single(vertex => vertex.Id == "cv-3").Y);
            Equal(raised, session.ProfileAt(1).Upper.Single(vertex => vertex.Id == "cv-3").Y);
        });
        Check("SectionDraft_MakeUnique_RootGetsCopyTipKeepsSource", () =>
        {
            using var session = Opened();
            string originalBlock = ProfileBlock(Encoding.UTF8.GetString(session.Snapshot().Source), "section-a");
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            Equal(SectionScope.Independent, view.Scope);
            Equal("section-a-i1", view.Profile);
            Equal(0d, view.Last!.MaxChange);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None));
            byte[] source = session.Snapshot().Source;
            Equal("section-a-i1", Assigned(source, 0));
            Equal("section-a", Assigned(source, 1));
            Equal(originalBlock, ProfileBlock(Encoding.UTF8.GetString(source), "section-a"));
            Equal("section-a-i1", session.Envelope().Accepted[^1].Edit!.VertexId);
            Equal(false, session.ProfileAt(0).Upper.Single(v => v.Id == "cv-3").Y == session.ProfileAt(1).Upper.Single(v => v.Id == "cv-3").Y);
        });
        Check("SectionDraft_UseSourceThickness_ReportsTargetsAndAffectedEta", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source;
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.02));
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Thickness(ThicknessIntent.UseSource));
            Equal(ThicknessIntent.UseSource, view.Intent);
            var proposal = view.Last!.Thickness ?? throw new InvalidOperationException("Missing thickness proposal.");
            Equal(2, proposal.TargetEta.Count);
            Equal(0d, proposal.TargetEta[0]);
            Equal(1d, proposal.TargetEta[1]);
            Equal(proposal.TargetThickness[0], proposal.TargetThickness[1]);
            foreach (double residual in proposal.Residuals)
                if (Math.Abs(residual) > 1e-9) throw new InvalidOperationException("Residual " + residual);
            Equal(true, 0 <= proposal.AffectedEtaStart && proposal.AffectedEtaStart < proposal.AffectedEtaEnd && proposal.AffectedEtaEnd <= 1);
            Equal(false, CurveText(original, "thickness") == CurveText(view.Bytes, "thickness"));
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            Equal(GeometryStatus.Certified, assessment.Status);
            session.FinishSection(Id(), assessment);
            Equal(ThicknessIntent.UseSource, session.Envelope().Accepted[^1].Edit!.Intent);
            string kept = Id();
            var keptView = session.BeginSectionDraft(kept, 0);
            keptView = session.ApplySectionStep(kept, keptView.Generation, new SectionStep.Thickness(ThicknessIntent.KeepCurrent));
            Equal(CurveText(session.Snapshot().Source, "thickness"), CurveText(keptView.Bytes, "thickness"));
        });
        Check("SectionDraft_UseSourceInfeasible_FinishBlocked", () =>
        {
            // assume: Geometry.Assess rejects every non-root_mirror lock, so no admitted base holds a value lock; the lock is
            // written into the open draft as ThicknessIntentTests does. A false reading would be Open admitting that source.
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            ReplaceDraftBytes(session, WithValueLock(view.Bytes, 0.05));
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Thickness(ThicknessIntent.UseSource));
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            Equal(false, assessment.Status == GeometryStatus.Certified);
            Equal("DSL-LOCK", assessment.Code);
            int rows = session.Envelope().Accepted.Length;
            Refuses("DSL-NOT-ASSESSED", () => session.FinishSection(Id(), assessment));
            Equal(rows, session.Envelope().Accepted.Length);
        });
        Check("SectionDraft_ImportStep_ReportsResidualAndProvenance", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            byte[] dat = Naca0012Selig();
            var fitted = FoilSource.ImportDat(dat, "naca-0012");
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Import(dat));
            var report = view.Last!.Import ?? throw new InvalidOperationException("Missing import report.");
            Equal("import", view.Last.Kind);
            Equal(fitted.MaxResidual, report.MaxResidual);
            Equal(fitted.Provenance, report.Provenance);
            Equal(true, report.Provenance.Length > 0);
            Equal(view.Last.UpperPoints, report.VertexCount);
            Equal("naca-0012", view.Profile);
            Equal(SectionScope.Independent, view.Scope);
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            Equal(report, assessment.ImportReport);
        });
        Check("SectionDraft_FairStep_ReportsAchievedDeviation", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.004));
            var (_, expected) = FoilSource.FairProfile(view.Bytes, "section-a", 1e-3, PreserveEnds.Position);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Fair(null, 1e-3, PreserveEnds.Position));
            Equal("fair", view.Last!.Kind);
            Equal(expected.MaxDeviation, view.Last.MaxChange);
            Equal(true, view.Last.MaxChange <= 1e-3);
            Equal("ProfileFair.MaxDeviation", view.Last.MaxChangeOracle);
            Refuses("DSL-GEOMETRY", () => session.ApplySectionStep(id, view.Generation, new SectionStep.Fair(null, -1, PreserveEnds.Position)));
        });
        Check("SectionTelemetry_FinishEvent_CarriesStepsAndDuration", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            foreach (string vertex in new[] { "cv-2", "cv-3", "cv-4" })
                view = session.ApplySectionStep(id, view.Generation, Raise(view, vertex, 0.002));
            session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None));
            var finish = session.ReadLocalEvents().Last(item => item.Operation == "section.finish");
            Equal("OK", finish.Outcome);
            Equal<int?>(3, finish.Steps);
            Equal("section", finish.EditKind);
            Equal<bool?>(false, finish.Independent);
            Equal(true, finish.DurationMilliseconds > 0);
            var steps = session.ReadLocalEvents().Where(item => item.Operation == "section.step").ToArray();
            Equal(3, steps.Length);
            Equal(true, steps.All(item => item.StepKind == "move" && item.Outcome == "OK"));
            string cancelled = Id();
            var again = session.BeginSectionDraft(cancelled, 0);
            session.ApplySectionStep(cancelled, again.Generation, new SectionStep.MakeUnique());
            session.Cancel(cancelled);
            var cancel = session.ReadLocalEvents().Last(item => item.Operation == "section.cancel");
            Equal<int?>(1, cancel.Steps);
            Equal<bool?>(true, cancel.Independent);
        });
        Check("SectionTelemetry_Events_NoNamesIdsOrPositions", () =>
        {
            const string secret = "zz-private-name";
            using var session = new AuthoringSession();
            session.Open(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(FoilSourceTests.Example).Replace("section-a", secret)), Id(), true);
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            var (x, _) = Point(view, SurfaceSide.Upper, "cv-3");
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", x, 0.0712345));
            Refuses("DSL-LOCK", () => session.ApplySectionStep(id, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-0", 0, 0.0812345)));
            session.UndoSectionStep(id);
            view = session.RedoSectionStep(id);
            string finished = session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None))!;
            var events = session.ReadLocalEvents()
                .Where(item => item.Operation.StartsWith("section.", StringComparison.Ordinal) || item.EditKind == "section").ToArray();
            Equal(true, events.Length >= 8);
            string json = JsonSerializer.Serialize(events);
            foreach (string leak in new[] { secret, "cv-", id, finished, "0.0712345", "0.0812345" })
                Equal(false, json.Contains(leak, StringComparison.Ordinal));
            Equal(true, events.Where(item => item.Operation == "section.step").All(item => item.StepKind is "make-unique" or "move"));
        });
    }

    // §12.3 tier 6 (gathered at UXR): value_ms is the best of five warmed calls on the Example, so a collection pause in one
    // call does not decide the row. Never in the fast ring (run-tests.sh does not pass --readiness).
    internal static void RunReadiness()
    {
        Check("Readiness_SectionStepApply_Under5Ms", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.001));
            double best = Best(() => view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.001)));
            Console.WriteLine("READINESS SectionStepApply value_ms=" + best.ToString("G17", CultureInfo.InvariantCulture));
            // §6 Concurrency: "measured, not a gate" — the row tests the 5 ms assume:, and a miss is the trigger to move steps
            // off the UI thread, not a red readiness ring. UXR measured ~56 ms at load 28 (docs/reviews/m12c-native.md).
            if (best >= 5) Console.WriteLine("READINESS-MISS SectionStepApply assume: under 5 ms is false; move steps off-thread");
        });
        Check("Readiness_SectionAssessExample_Under50Ms", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.001));
            _ = session.AssessSection(id, view.Generation, CancellationToken.None);
            double best = Best(() => _ = session.AssessSection(id, view.Generation, CancellationToken.None));
            Console.WriteLine("READINESS SectionAssessExample value_ms=" + best.ToString("G17", CultureInfo.InvariantCulture));
            Equal(true, best < 50);
        });
    }

    private static double Best(Action call)
    {
        double best = double.PositiveInfinity;
        for (int index = 0; index < 5; index++)
        {
            var once = System.Diagnostics.Stopwatch.StartNew();
            call();
            best = Math.Min(best, once.Elapsed.TotalMilliseconds);
        }
        return best;
    }

    internal static string Id() => Guid.NewGuid().ToString("D");

    internal static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Open(FoilSourceTests.Example, Id(), true);
        return session;
    }

    internal static void Same(byte[] expected, byte[] actual)
    {
        if (!expected.AsSpan().SequenceEqual(actual)) throw new InvalidOperationException("Bytes differ.");
    }

    internal static (double X, double Y) Point(SectionDraftView view, SurfaceSide side, string vertexId)
    {
        var profile = FoilSource.Parse(view.Bytes).Definition!.Profiles.Single(item => item.Name == view.Profile);
        var curve = side == SurfaceSide.Upper ? profile.Upper : profile.Lower;
        var point = curve.Points[Array.IndexOf(curve.Ids, vertexId)];
        return (point[0], point[1]);
    }

    /// <summary>A Move step that raises one upper vertex in place by <paramref name="dy"/> chord.</summary>
    internal static SectionStep Raise(SectionDraftView view, string vertexId, double dy)
    {
        var (x, y) = Point(view, SurfaceSide.Upper, vertexId);
        return new SectionStep.Move(SurfaceSide.Upper, vertexId, x, y + dy);
    }

    internal static string Assigned(byte[] source, int assignment)
    {
        var definition = FoilSource.Parse(source).Definition!;
        return definition.Profiles[definition.Assignments[assignment].Profile].Name;
    }

    internal static string ProfileBlock(string text, string name)
    {
        int keyword = text.IndexOf("profile \"" + name + "\" {", StringComparison.Ordinal);
        int open = text.IndexOf('{', keyword), depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}' && --depth == 0) return text[keyword..(i + 1)];
        }
        throw new InvalidOperationException("Unclosed profile block.");
    }

    private static string CurveText(byte[] source, string name)
    {
        string text = Encoding.UTF8.GetString(source);
        int at = text.IndexOf(name + " cv", StringComparison.Ordinal);
        return text[at..(text.IndexOf('}', at) + 1)];
    }

    private static byte[] WithValueLock(byte[] source, double locked)
    {
        string text = Encoding.UTF8.GetString(source);
        int sections = text.LastIndexOf("sections {", StringComparison.Ordinal);
        int close = text.IndexOf('}', sections);
        string block = "\n  locks {\n    root_mirror leading\n    root_mirror trailing\n    root_mirror dihedral\n    root_mirror twist\n    root_mirror thickness\n    value thickness at root " +
            locked.ToString("R", CultureInfo.InvariantCulture) + "\n  }";
        return Encoding.UTF8.GetBytes(text.Insert(close + 1, block));
    }

    // The cursor bytes are the draft's bytes (§3.2); replacing them is ThicknessIntentTests' seam for a lock no base admits.
    private static void ReplaceDraftBytes(AuthoringSession session, byte[] bytes)
    {
        var field = typeof(AuthoringSession).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AuthoringSession.draft is missing.");
        var current = (SessionDraft)field.GetValue(session)!;
        field.SetValue(session, current with { Bytes = bytes });
    }

    internal static byte[] Naca0012Selig()
    {
        const int n = 40;
        var text = new StringBuilder("NACA 0012\n");
        static double X(int i) => 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
        static double Half(double x) => 0.6 * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * Math.Pow(x, 3) - 0.1036 * Math.Pow(x, 4));
        for (int i = n; i >= 0; i--) text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", X(i), Half(X(i))));
        for (int i = 1; i <= n; i++) text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", X(i), -Half(X(i))));
        return Encoding.UTF8.GetBytes(text.ToString());
    }
}
