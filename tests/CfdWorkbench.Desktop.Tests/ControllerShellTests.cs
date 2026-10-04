using System.IO;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

// Track D2 owns this class and adds its named checks with DesktopChecks.Check (docs/design/app-shell.md §9, §12.4, §14).
public static class ControllerShellTests
{
    public static void Run()
    {
        DesktopChecks.Check("StatusStrip_BackgroundCompletion_DoesNotReplaceNewerReport", () =>
        {
            // STATUS-CLOBBER at the strip (docs/reviews/ui-status-bar.md §2.3): a report the strip shows from outside the
            // controller while sampling runs (a Properties report) supersedes the sampling placeholder, so the completion does
            // not replace it. The shell's sink does exactly this for every such report.
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            var point = controller.Planform!.Trailing.Points.First(item => item.Role == PointRole.Control);
            controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)).GetAwaiter().GetResult();
            Require(controller.Status.StartsWith("Sampling accepted geometry", StringComparison.Ordinal),
                "setup: sampling finished before the report: " + controller.Status);
            dynamic strip = controller;   // by name, so the check ran red before the API existed
            strip.SupersedeStatus();
            long shown = (long)strip.StatusVersion;
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(8);
            while (controller.Provenance != "accepted" && DateTime.UtcNow < deadline) Thread.Sleep(5);
            Require(controller.Provenance == "accepted", "sampling did not finish: " + controller.Provenance);
            Require((long)strip.StatusVersion == shown && !controller.Status.StartsWith("Accepted η", StringComparison.Ordinal),
                $"the sampling completion replaced the newer report: '{controller.Status}'");
        });

        DesktopChecks.Check("StatusSlot_CompletionRacingNewerWrite_NewerAlwaysWins", () =>
        {
            // STATUS-CLOBBER across threads: each round writes a placeholder, then a thread-pool completion of that
            // placeholder and a newer write on this thread (the UI thread's role) start together. Whichever runs first, the
            // newer report must be what the slot shows. The interleaving is not forced; many rounds make it likely.
            const int rounds = 20000;
            var slot = new StatusSlot("");
            var placeholders = new long[rounds];
            using var start = new Barrier(2);
            using var done = new Barrier(2);
            var completion = Task.Run(() =>
            {
                for (int round = 0; round < rounds; round++)
                {
                    start.SignalAndWait();
                    Thread.SpinWait(round % 97);   // jitter the start so the two writers overlap at every offset
                    slot.TryReplace(placeholders[round], "completion " + round);
                    done.SignalAndWait();
                }
            });
            int lost = 0;
            for (int round = 0; round < rounds; round++)
            {
                placeholders[round] = slot.Write("placeholder " + round);
                start.SignalAndWait();
                Thread.SpinWait(round * 31 % 89);
                slot.Write("newer " + round);
                done.SignalAndWait();
                if (slot.Snapshot().Text != "newer " + round) lost++;
            }
            completion.GetAwaiter().GetResult();
            Console.WriteLine($"MEASURE status-slot race rounds={rounds} lost={lost}");
            Require(lost == 0, $"a background completion replaced a newer report in {lost} of {rounds} rounds");
        });

        DesktopChecks.Check("ApplySpan_EdgesCross_Refused", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            string initial = controller.AcceptedSource;
            try
            {
                controller.ApplySpan("1000000000");
                throw new InvalidOperationException("Expected ApplySpan to be refused when edges cross.");
            }
            catch (ContractError) { }
            if (controller.AcceptedSource != initial)
                throw new InvalidOperationException("Accepted source changed after refused ApplySpan.");
        });

        DesktopChecks.Check("ApplySpan_NonPositive_Refused", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            string initial = controller.AcceptedSource;
            try
            {
                controller.ApplySpan("0");
                throw new InvalidOperationException("Expected ApplySpan to refuse 0.");
            }
            catch (ContractError) { }

            try
            {
                controller.ApplySpan("-500");
                throw new InvalidOperationException("Expected ApplySpan to refuse negative span.");
            }
            catch (ContractError) { }

            if (controller.AcceptedSource != initial)
                throw new InvalidOperationException("Accepted source changed after refused ApplySpan.");
        });

        DesktopChecks.Check("ApplySpan_NotANumber_Refused", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            string initial = controller.AcceptedSource;
            try
            {
                controller.ApplySpan("not-a-number");
                throw new InvalidOperationException("Expected ApplySpan to refuse non-numeric text.");
            }
            catch (ContractError) { }

            try
            {
                controller.ApplySpan("");
                throw new InvalidOperationException("Expected ApplySpan to refuse empty text.");
            }
            catch (ContractError) { }

            if (controller.AcceptedSource != initial)
                throw new InvalidOperationException("Accepted source changed after refused ApplySpan.");
        });

        DesktopChecks.Check("Controller_ApplySpanAsync_CoreBoundaryRefusalUnchanged", () =>
        {
            using var controller = new WorkbenchController();
            controller.NewFoilAsync().GetAwaiter().GetResult();
            string before = controller.AcceptedSource;
            var result = controller.ApplySpanAsync("1000000000").GetAwaiter().GetResult();
            Require(result is CommitOutcome.Refused { Code: "DSL-EDGES-CROSS" },
                "Async Span did not return Core's boundary refusal.");
            Require(controller.Gesture == GestureState.Idle && controller.AcceptedSource == before && !controller.CanUndo,
                "Refused async Span changed source, history, or Busy state.");
        });

        // STATUS-CLOBBER: the accepted-slice sampling started by an open finishes ~0.2 s later on the pool. Its report
        // must replace only its own "Sampling…" placeholder, never a newer message such as the lock copy of a nudge.
        DesktopChecks.Check("LockedNudge_DuringSampling_LockCopySurvivesCompletion", () =>
        {
            using var controller = new WorkbenchController();
            controller.NewFoilAsync().GetAwaiter().GetResult();
            var sampled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            controller.Changed += () => { if (controller.Provenance == "accepted") sampled.TrySetResult(); };
            Require(controller.Provenance == "accepted — sampling", "Precondition: sampling finished before the nudge.");
            var point = controller.Planform!.Leading.Points.First(item => item.Freedom == PointFreedom.Fixed);
            Require(!controller.BeginGesture(new PointRef(point.Curve, point.Id), GestureInput.Keyboard) &&
                controller.Status.Contains("fixed", StringComparison.OrdinalIgnoreCase), "Locked nudge did not report its lock.");
            Require(sampled.Task.Wait(TimeSpan.FromSeconds(30)), "Accepted sampling did not complete.");
            Require(controller.Status.Contains("fixed", StringComparison.OrdinalIgnoreCase),
                $"Sampling completion overwrote the lock copy: '{controller.Status}'.");
        });

        DesktopChecks.Check("OpenFailure_AccessDenied_Classified", () =>
        {
            var failure = OpenFailure.Classify(new UnauthorizedAccessException("Permission denied"), "/test/locked.foil");
            if (failure is not OpenFailure.AccessDenied || failure.Kind != OpenFailureKind.AccessDenied)
                throw new InvalidOperationException($"Expected AccessDenied, got {failure.GetType().Name}");
        });

        DesktopChecks.Check("OpenFailure_Missing_Classified", () =>
        {
            var failure = OpenFailure.Classify(new FileNotFoundException("File not found"), "/nonexistent/path.foil");
            if (failure is not OpenFailure.Missing || failure.Kind != OpenFailureKind.Missing)
                throw new InvalidOperationException($"Expected Missing, got {failure.GetType().Name}");
        });

        DesktopChecks.Check("OpenFailure_Newer_Classified", () =>
        {
            var failure = OpenFailure.Classify(new ContractError("DOC-VERSION"), "/test/v2.cfdw.json");
            if (failure is not OpenFailure.Newer || failure.Kind != OpenFailureKind.Newer)
                throw new InvalidOperationException($"Expected Newer, got {failure.GetType().Name}");

            var dslFailure = OpenFailure.Classify(new ContractError("DSL-VERSION"), "/test/v2.foil");
            if (dslFailure is not OpenFailure.Newer)
                throw new InvalidOperationException($"Expected Newer for DSL-VERSION, got {dslFailure.GetType().Name}");
        });

        DesktopChecks.Check("OpenFailure_NotRecognised_Classified", () =>
        {
            foreach (string code in new[] { "DOC-SCHEMA", "DOC-TYPE", "DOC-REFERENCE", "DSL-SYNTAX" })
            {
                var failure = OpenFailure.Classify(new ContractError(code), "/test/invalid.foil");
                if (failure is not OpenFailure.NotRecognised || failure.Kind != OpenFailureKind.NotRecognised)
                    throw new InvalidOperationException($"Expected NotRecognised for {code}, got {failure.GetType().Name}");
            }
        });

        DesktopChecks.Check("OpenFailure_TooLarge_Classified", () =>
        {
            var failure = OpenFailure.Classify(new ContractError("DOC-SIZE"), "/test/large.cfdw.json");
            if (failure is not OpenFailure.TooLarge || failure.Kind != OpenFailureKind.TooLarge)
                throw new InvalidOperationException($"Expected TooLarge for DOC-SIZE, got {failure.GetType().Name}");

            var dslFailure = OpenFailure.Classify(new ContractError("DSL-LIMIT"), "/test/large.foil");
            if (dslFailure is not OpenFailure.TooLarge)
                throw new InvalidOperationException($"Expected TooLarge for DSL-LIMIT, got {dslFailure.GetType().Name}");
        });

        DesktopChecks.Check("OpenFailure_UnknownContent_Classified", () =>
        {
            var failure = OpenFailure.Classify(new ContractError("DOC-UNSUPPORTED-FIELD"), "/test/future.cfdw.json");
            if (failure is not OpenFailure.UnknownContent || failure.Kind != OpenFailureKind.UnknownContent)
                throw new InvalidOperationException($"Expected UnknownContent, got {failure.GetType().Name}");
        });

        DesktopChecks.Check("OpenFailure_Unreadable_Classified", () =>
        {
            var failure = OpenFailure.Classify(new IOException("Disk read failure"), "/test/disk-error.foil");
            if (failure is not OpenFailure.Unreadable || failure.Kind != OpenFailureKind.Unreadable)
                throw new InvalidOperationException($"Expected Unreadable for IOException, got {failure.GetType().Name}");

            var docFailure = OpenFailure.Classify(new ContractError("DOC-IO"), "/test/doc-io.cfdw.json");
            if (docFailure is not OpenFailure.Unreadable)
                throw new InvalidOperationException($"Expected Unreadable for DOC-IO, got {docFailure.GetType().Name}");
        });

        DesktopChecks.Check("Open_CancelAfterCommit_Ignored", () =>
        {
            using var controller = new WorkbenchController();
            using var cts = new CancellationTokenSource();
            const string path = "src/CfdWorkbench.Desktop/Assets/example.foil";
            var task = controller.OpenAsync(path, cts.Token);
            var outcome = task.GetAwaiter().GetResult();
            if (outcome is not OpenOutcome.Opened)
                throw new InvalidOperationException($"Expected Opened outcome, got {outcome.GetType().Name}");

            cts.Cancel();
            if (controller.AcceptedSource == "")
                throw new InvalidOperationException("Foil was discarded after cancel-after-commit.");
            if (controller.OpenedPath != path)
                throw new InvalidOperationException("OpenedPath was reset after cancel-after-commit.");
        });

        DesktopChecks.Check("Open_CancelDuringPrepare_CurrentFoilUnchanged", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            string initial = controller.AcceptedSource;
            if (string.IsNullOrEmpty(initial))
                throw new InvalidOperationException("Initial example foil failed to open.");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var outcome = controller.OpenAsync("docs/examples/foildsl/foil-basic.foil", cts.Token).GetAwaiter().GetResult();
            if (outcome is not OpenOutcome.Cancelled)
                throw new InvalidOperationException($"Expected Cancelled outcome, got {outcome.GetType().Name}");

            if (controller.AcceptedSource != initial)
                throw new InvalidOperationException("Current foil changed after cancelled open.");
        });

        DesktopChecks.Check("Open_IdCandidate_NeedsIds", () =>
        {
            using var controller = new WorkbenchController();
            var outcome = controller.OpenAsync("docs/examples/foildsl/foil-comment.foil").GetAwaiter().GetResult();
            if (outcome is not OpenOutcome.NeedsIds needsIds)
                throw new InvalidOperationException($"Expected NeedsIds outcome, got {outcome.GetType().Name}");

            if (needsIds.Candidate is null || needsIds.Candidate.Length == 0)
                throw new InvalidOperationException("Candidate bytes missing from NeedsIds outcome.");
        });

        DesktopChecks.Check("Open_SecondRequest_SupersedesFirst", () =>
        {
            using var controller = new WorkbenchController();
            var first = controller.OpenAsync("docs/examples/foildsl/foil-precision.foil");
            var second = controller.OpenAsync("src/CfdWorkbench.Desktop/Assets/example.foil");

            var outcome1 = first.GetAwaiter().GetResult();
            var outcome2 = second.GetAwaiter().GetResult();

            if (outcome1 is not OpenOutcome.Superseded)
                throw new InvalidOperationException($"Expected first open to be Superseded, got {outcome1.GetType().Name}");
            if (outcome2 is not OpenOutcome.Opened)
                throw new InvalidOperationException($"Expected second open to be Opened, got {outcome2.GetType().Name}");
        });

        DesktopChecks.Check("Open_Uncertified_RefusedReadOnly", () =>
        {
            using var controller = new WorkbenchController();
            var parsed = FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/invalid-geometry.foil"));
            var candidateWithIds = FoilSource.MaterializeIds(parsed);
            string tempPath = Path.Combine(Path.GetTempPath(), $"uncertified-{Guid.NewGuid():N}.foil");
            File.WriteAllBytes(tempPath, candidateWithIds);
            try
            {
                var outcome = controller.OpenAsync(tempPath).GetAwaiter().GetResult();
                if (outcome is not OpenOutcome.Refused refused)
                    throw new InvalidOperationException($"Expected Refused outcome, got {outcome.GetType().Name}");

                if (refused.Original is null || refused.Original.Length == 0)
                    throw new InvalidOperationException("Original bytes missing from Refused outcome.");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        });

        DesktopChecks.Check("Properties_SectionMode_WingAsText", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            var estimates = WingEstimates.From(File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil"), "accepted", 0);
            var model = PropertiesView.Build(new Selection.Foil(), controller.Inspection?.Authored, estimates, ShellMode.SectionEditor);

            var wingBlock = model.Blocks.FirstOrDefault(b => b.Title == "Wing");
            if (wingBlock is null)
                throw new InvalidOperationException("Wing block missing in SectionEditor mode.");

            var spanRow = wingBlock.Rows.FirstOrDefault(r => r.Label == "Span");
            if (spanRow is null)
                throw new InvalidOperationException("Span row missing in Wing block.");
            if (spanRow.IsEditable)
                throw new InvalidOperationException("Span row must be non-editable text in SectionEditor mode.");
            if (spanRow.HelperText != "Set these in the workspace.")
                throw new InvalidOperationException($"Expected COPY-122 'Set these in the workspace.', got '{spanRow.HelperText}'");
        });

        DesktopChecks.Check("Properties_TipCloses_TipChordIsText", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            var baseEstimates = WingEstimates.From(File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil"), "accepted", 0);
            var closingEstimates = baseEstimates with { TipChordMeters = 0 };
            var model = PropertiesView.Build(new Selection.Foil(), controller.Inspection?.Authored, closingEstimates, ShellMode.Workspace);

            var wingBlock = model.Blocks.First(b => b.Title == "Wing");
            var tipChordRow = wingBlock.Rows.First(r => r.Label == "Tip chord");
            if (tipChordRow.IsEditable)
                throw new InvalidOperationException("Tip chord row must be non-editable text.");
            if (tipChordRow.Value != "Tip closes — edit the tip station")
                throw new InvalidOperationException($"Expected COPY-108 'Tip closes — edit the tip station', got '{tipChordRow.Value}'");
        });

        DesktopChecks.Check("Properties_WingBlockLast_WhenFoilOpen", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            var estimates = WingEstimates.From(File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil"), "accepted", 0);

            var modelFoil = PropertiesView.Build(new Selection.Foil(), controller.Inspection?.Authored, estimates, ShellMode.Workspace);
            if (modelFoil.Blocks.Count == 0 || modelFoil.Blocks[^1].Title != "Wing")
                throw new InvalidOperationException($"Wing block must be last when Foil selected. Last block was: {modelFoil.Blocks.LastOrDefault()?.Title}");

            var modelStation = PropertiesView.Build(new Selection.Station(0, 0.0), controller.Inspection?.Authored, estimates, ShellMode.Workspace);
            if (modelStation.Blocks.Count == 0 || modelStation.Blocks[^1].Title != "Wing")
                throw new InvalidOperationException($"Wing block must be last when Station selected. Last block was: {modelStation.Blocks.LastOrDefault()?.Title}");
        });

        DesktopChecks.Check("Selection_Reconcile_PointsDropped", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();

            var existing = new PointRef("leading", "cv-2");
            var nonExisting = new PointRef("leading", "non-existent-id");
            controller.Select(new Selection.Points(new[] { existing, nonExisting }));

            if (controller.Selection is not Selection.Points pts || pts.Items.Count != 1 || pts.Items[0].VertexId != "cv-2")
                throw new InvalidOperationException($"Expected 1 reconciled point (cv-2), got {controller.Selection}");

            controller.Select(new Selection.Points(new[] { nonExisting }));
            if (controller.Selection is not Selection.Foil)
                throw new InvalidOperationException($"Expected reconcile to Foil when all points dropped, got {controller.Selection}");
        });

        DesktopChecks.Check("Selection_Reconcile_StationByExactEta", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();

            controller.Select(new Selection.Station(0, 1.0));
            if (controller.Selection is not Selection.Station s1 || s1.Index != 1 || s1.Eta != 1.0)
                throw new InvalidOperationException($"Expected station reconciled to index 1 by exact eta, got {controller.Selection}");

            controller.Select(new Selection.Station(99, 0.42));
            if (controller.Selection is not Selection.Foil)
                throw new InvalidOperationException($"Expected reconcile to Foil for unknown eta, got {controller.Selection}");
        });

        DesktopChecks.Check("Selection_ReconciledBeforeChanged", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();

            bool selectionChangedFired = false;
            bool checkedInChanged = false;

            controller.SelectionChanged += () =>
            {
                selectionChangedFired = true;
            };

            controller.Changed += () =>
            {
                if (selectionChangedFired)
                {
                    if (controller.Selection is Selection.Station s && s.Index != 1)
                        throw new InvalidOperationException("Selection was not reconciled before Changed fired.");
                    checkedInChanged = true;
                }
            };

            controller.Select(new Selection.Station(0, 1.0));

            if (!selectionChangedFired || !checkedInChanged)
                throw new InvalidOperationException($"Event order check failed. SelectionChanged: {selectionChangedFired}, Checked in Changed: {checkedInChanged}");
        });

        DesktopChecks.Check("NewFoil_Opened_UntitledNoPath", () =>
        {
            using var controller = new WorkbenchController();
            var outcome = controller.NewFoilAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (outcome is not OpenOutcome.Opened opened)
                throw new InvalidOperationException($"Expected Opened outcome, got {outcome.GetType().Name}");
            if (opened.Path.Length != 0)
                throw new InvalidOperationException($"Expected an empty path on a new foil, got '{opened.Path}'.");
            if (controller.OpenedPath is not null)
                throw new InvalidOperationException($"New foil has a path '{controller.OpenedPath}'.");
            if (controller.NativePath is not null)
                throw new InvalidOperationException($"New foil has a native path '{controller.NativePath}'.");
            if (controller.Inspection?.Geometry.Status != GeometryStatus.Certified)
                throw new InvalidOperationException($"New foil is not certified ({controller.Inspection?.Geometry.Status.ToString() ?? "none"}).");
            string expected = System.Text.Encoding.UTF8.GetString(FoilSource.NewDefault());
            if (controller.AcceptedSource != expected)
                throw new InvalidOperationException("Opened source is not FoilSource.NewDefault().");
        });

        DesktopChecks.Check("NewFoil_CancelDuringPrepare_CurrentFoilUnchanged", () =>
        {
            using var controller = new WorkbenchController();
            controller.OpenExampleAsync().GetAwaiter().GetResult();
            string initial = controller.AcceptedSource;
            string? path = controller.OpenedPath;
            if (string.IsNullOrEmpty(initial))
                throw new InvalidOperationException("Initial example foil failed to open.");

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var outcome = controller.NewFoilAsync(cts.Token).GetAwaiter().GetResult();
            if (outcome is not OpenOutcome.Cancelled)
                throw new InvalidOperationException($"Expected Cancelled outcome, got {outcome.GetType().Name}");
            if (controller.AcceptedSource != initial)
                throw new InvalidOperationException("Current foil changed after cancelled new foil.");
            if (controller.OpenedPath != path)
                throw new InvalidOperationException("Opened path changed after cancelled new foil.");
        });

        DesktopChecks.Check("NewFoil_WorksWithoutExample", () =>
        {
            using var controller = new WorkbenchController();
            var outcome = controller.NewFoilAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (outcome is not OpenOutcome.Opened)
                throw new InvalidOperationException($"Expected Opened outcome, got {outcome.GetType().Name}");
            if (controller.OpenedPath is not null || controller.NativePath is not null)
                throw new InvalidOperationException("New foil without the example acquired a path.");
            string text = controller.AcceptedSource;
            byte[] example = File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil");
            if (System.Text.Encoding.UTF8.GetBytes(text).AsSpan().SequenceEqual(example))
                throw new InvalidOperationException("New foil returned the example fixture.");
            foreach (string token in new[] { "Basic foil", "section-a", "Embedded Example", "example.foil" })
            {
                if (text.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"New foil source contains example identifier '{token}'.");
            }
            if (controller.Inspection?.Geometry.Status != GeometryStatus.Certified)
                throw new InvalidOperationException("New foil without the example is not certified.");
        });

        RunPointControllerChecks();
        RunGestureCellChecks();
    }

    private static object GestureEnum(string type, string member) => Enum.Parse(
        typeof(WorkbenchController).Assembly.GetType($"CfdWorkbench.Desktop.{type}")
            ?? throw new InvalidOperationException($"Missing {type} controller contract"), member);

    private static (WorkbenchController Controller, PointRef Reference, PointView Point) OpenPoint(int index = 3, string curve = "trailing")
    {
        var controller = new WorkbenchController();
        // New foil ships 4 control vertices per rail (Ruling 64); these checks drag a real interior control point of the saved 10-point foil.
        controller.OpenFoilAsync(DesktopChecks.TenPointFoil(), "New foil 10").GetAwaiter().GetResult();
        var plan = Planform.View(System.Text.Encoding.UTF8.GetBytes(controller.AcceptedSource), "accepted", 0);
        var point = (curve == "leading" ? plan.Leading : plan.Trailing).Points[index];
        if (point.Freedom == PointFreedom.Fixed) throw new InvalidOperationException("Fixture point is fixed.");
        return (controller, new PointRef(curve, point.Id), point);
    }

    private static bool Begin(WorkbenchController controller, PointRef point, string input = "Pointer")
    {
        dynamic target = controller;
        dynamic value = GestureEnum("GestureInput", input);
        return target.BeginGesture(point, value);
    }

    private static void Move(WorkbenchController controller, PointView point, double spanDelta, double aftDelta = 0)
    {
        dynamic target = controller;
        target.UpdateGesture(point.SpanMeters + spanDelta, point.Ordinate + aftDelta);
    }

    private static dynamic End(WorkbenchController controller, string reason = "Release")
    {
        dynamic target = controller;
        dynamic value = GestureEnum("GestureEnd", reason);
        return target.EndGestureAsync(value).GetAwaiter().GetResult();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void RunPointControllerChecks()
    {
        DesktopChecks.Check("GestureStateTable_EveryCell_TransitionOrIgnored", () =>
        {
            using var fixture = OpenPoint().Controller;
            var plan = Planform.View(System.Text.Encoding.UTF8.GetBytes(fixture.AcceptedSource), "accepted", 0);
            var point = plan.Trailing.Points[3];
            var reference = new PointRef("trailing", point.Id);
            dynamic c = fixture;
            Require(c.Gesture.ToString() == "Idle", "Initial state is not Idle.");
            Require(Begin(fixture, reference), "Idle pointer down was ignored.");
            Require(c.Gesture.ToString() == "Pressed", "Pointer down did not enter Pressed.");
            Require(!Begin(fixture, reference), "Second pointer down was not ignored.");
            Move(fixture, point, 0.01);
            Require(c.Gesture.ToString() == "Dragging", "Move did not enter Dragging.");
            Require(!Begin(fixture, reference), "Pointer down during Dragging was not ignored.");
            End(fixture, "Escape");
            Require(c.Gesture.ToString() == "Idle", "Escape did not return to Idle.");
            Require(Begin(fixture, reference, "Keyboard"), "Keyboard begin was ignored.");
            Move(fixture, point, 0.001);
            Require(c.Gesture.ToString() == "Nudging", "Arrow did not enter Nudging.");
            End(fixture, "FocusLost");
            Require(c.Gesture.ToString() == "Idle", "Nudge completion did not return to Idle.");
        });

        DesktopChecks.Check("Controller_MoveTwoPixels_NoDraft", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                Require(Begin(controller, pointRef), "Pointer begin failed.");
                Move(controller, point, 0.002);
                Require(c.Gesture.ToString() == "Pressed", "Two-pixel move opened a drag.");
                Require(controller.Draft is null, "Two-pixel move opened a Core draft.");
                Require(End(controller).GetType().Name == "NoChange", "Click did not end as NoChange.");
            }
        });

        DesktopChecks.Check("Controller_MoveFourPixels_DraftOpened", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                Begin(controller, pointRef);
                Move(controller, point, 0.004);
                Require(c.Gesture.ToString() == "Dragging" && controller.Draft is not null, "Four-pixel move did not open draft.");
                End(controller, "Escape");
            }
        });

        DesktopChecks.Check("Controller_DragPoint_OneUndoStepUndoExact", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                string before = controller.AcceptedSource;
                Begin(controller, pointRef); Move(controller, point, 0, 0.005);
                Require(End(controller).GetType().Name == "Committed", "Drag did not commit.");
                Require(controller.AcceptedSource != before && controller.CanUndo, "Drag did not add one accepted edit.");
                controller.Undo();
                Require(controller.AcceptedSource == before && !controller.CanUndo, "One undo did not restore exact source.");
            }
        });

        DesktopChecks.Check("Controller_ReleaseWithPendingFrame_CommitsLastPointerTarget", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                Begin(controller, pointRef);
                Move(controller, point, 0, 0.001);
                Move(controller, point, 0, 0.004);
                Require(End(controller).GetType().Name == "Committed", "Release did not commit.");
                var now = Planform.View(System.Text.Encoding.UTF8.GetBytes(controller.AcceptedSource), "accepted", 0).Trailing.Points[3];
                Require(Math.Abs(now.Ordinate - (point.Ordinate + 0.004)) < 0.000002, "Last pointer target was not committed.");
            }
        });

        DesktopChecks.Check("Controller_NudgeLadder_CommandPlainShift", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                foreach (var (modifier, expected) in new[] { ("Command", 0.00001), ("Plain", 0.0001), ("Shift", 0.001) })
                {
                    Require(Begin(controller, pointRef, "Keyboard"), "Nudge begin failed.");
                    dynamic value = GestureEnum("NudgeModifier", modifier);
                    c.Nudge(0, 1, value);
                    var draftPoint = ((PlanformView)c.Planform).Trailing.Points[3];
                    Require(Math.Abs(draftPoint.Ordinate - (point.Ordinate + expected)) < 0.000002, $"{modifier} ladder wrong.");
                    End(controller, "Escape");
                }
            }
        });

        DesktopChecks.Check("Controller_NudgeRunHeld_OneRowOnKeyUp", () => CheckNudgeCommit("KeyUp"));
        DesktopChecks.Check("Controller_NudgeRunFocusLost_CommitsOneRow", () => CheckNudgeCommit("FocusLost"));
        DesktopChecks.Check("Controller_NudgeRunWindowDeactivated_CommitsOneRow", () => CheckNudgeCommit("Deactivated"));

        DesktopChecks.Check("Controller_CaptureLostDuringDrag_Cancelled", () => CheckCancellation("CaptureLost"));
        DesktopChecks.Check("Controller_EscapeDuringDrag_NoRowGeometryBack", () => CheckCancellation("Escape"));

        DesktopChecks.Check("Controller_SaveDuringDrag_CommitsThenSaves", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                string path = Path.Combine(Path.GetTempPath(), $"u1a-{Guid.NewGuid():N}.cfdw.json");
                try
                {
                    Begin(controller, pointRef); Move(controller, point, 0, 0.004);
                    var result = controller.SaveAsync(path).GetAwaiter().GetResult();
                    Require(result.Code == "OK" && controller.Draft is null && controller.CanUndo,
                        $"Save did not commit drag first: code={result.Code}, draft={controller.Draft is not null}, undo={controller.CanUndo}.");
                    Require(File.Exists(path), "Save did not publish a project.");
                }
                finally { if (File.Exists(path)) File.Delete(path); }
            }
        });

        DesktopChecks.Check("Controller_StaleCommitCompletion_ReturnsToIdle", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                Begin(controller, pointRef); Move(controller, point, 0, 0.004);
                dynamic release = GestureEnum("GestureEnd", "Release");
                var pending = c.EndGestureAsync(release);
                pending.GetAwaiter().GetResult();
                Require(c.Gesture.ToString() == "Idle", "Completion left controller Busy.");
            }
        });

        DesktopChecks.Check("Controller_PointerDownDuringChordCommit_NoDraft", () =>
        {
            var (controller, pointRef, _) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                var pending = c.ApplyChordAsync("root-chord", "190 mm");
                if (c.Gesture.ToString() == "Busy")
                    Require(!Begin(controller, pointRef) && controller.Draft is null, "Pointer down opened draft during chord commit.");
                pending.GetAwaiter().GetResult();
                Require(c.Gesture.ToString() == "Idle", "Chord commit did not leave Busy.");
            }
        });

        DesktopChecks.Check("Gesture_ReleaseEdgesCross_RefusedGeometryUnchanged", () =>
        {
            var (controller, pointRef, point) = OpenPoint(5); using (controller)
            {
                string before = controller.AcceptedSource;
                Begin(controller, pointRef); Move(controller, point, 0, -1);
                Require(End(controller).GetType().Name == "Refused", "Crossing was not refused.");
                Require(controller.AcceptedSource == before && !controller.CanUndo, "Crossing changed accepted geometry.");
            }
        });

        DesktopChecks.Check("Gesture_ReleaseNotAssessed_RefusedDistinctCopy", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                Begin(controller, pointRef); Move(controller, point, 0, 0.004);
                using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
                dynamic release = GestureEnum("GestureEnd", "Release");
                dynamic outcome = c.EndGestureAsync(release, cancelled.Token).GetAwaiter().GetResult();
                Require(outcome.GetType().Name == "Refused", "Cancelled validation did not refuse.");
                Require(((string)outcome.Copy).Contains("couldn't be checked", StringComparison.OrdinalIgnoreCase), "Not-assessed copy is not distinct.");
            }
        });

        DesktopChecks.Check("Gesture_BeginDuringCommit_NoDraftNoBusy", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                Begin(controller, pointRef); Move(controller, point, 0, 0.004);
                dynamic release = GestureEnum("GestureEnd", "Release");
                var pending = c.EndGestureAsync(release);
                if (c.Gesture.ToString() == "Busy") Require(!Begin(controller, pointRef), "Busy accepted another begin.");
                pending.GetAwaiter().GetResult();
                Require(controller.Draft is null && c.Gesture.ToString() == "Idle", "Commit left a draft or Busy state.");
            }
        });

        DesktopChecks.Check("Gesture_ThousandMoves_CoalescedFramesBounded", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                Begin(controller, pointRef);
                for (int i = 0; i < 1000; i++) Move(controller, point, 0, 0.004 + i * 0.000001);
                End(controller);
                dynamic c = controller;
                Require((int)c.LastGestureFrames < 1000, "Every pointer move became a Core frame.");
            }
        });

        DesktopChecks.Check("Controller_DuringDrag_EstimatesFromDraftGeneration", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                Begin(controller, pointRef); Move(controller, point, 0, 0.004);
                dynamic c = controller; c.FlushGestureFrame();
                Require(controller.Draft is not null && controller.Estimates?.Basis == "preview" &&
                    controller.Estimates.Generation == controller.Draft.Generation, "Estimates did not follow draft generation.");
                End(controller, "Escape");
            }
        });

        DesktopChecks.Check("Controller_UndoDisabledDuringGesture_EnabledAfter", () =>
        {
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                Begin(controller, pointRef); Move(controller, point, 0, 0.004);
                Require(!controller.CanUndo && !controller.CanRedo, "History was enabled during gesture.");
                End(controller);
                Require(controller.CanUndo, "Undo was not enabled after commit.");
            }
        });

        DesktopChecks.Check("Controller_Reconcile_MakeControlDropsHandleSelection", () =>
        {
            var (controller, pointRef, _) = OpenPoint(); using (controller)
            {
                dynamic c = controller;
                var anchor = new CfdWorkbench.Core.PointCommand.MakeAnchor(pointRef.Curve, pointRef.VertexId);
                dynamic made = c.ApplyPointCommandAsync(anchor).GetAwaiter().GetResult();
                Require(made.GetType().Name == "Committed", "Anchor command failed.");
                var plan = (PlanformView)c.Planform;
                var points = plan.Trailing.Points;
                int i = points.ToList().FindIndex(p => p.Id == pointRef.VertexId);
                controller.Select(new Selection.Points(new[] { new PointRef(pointRef.Curve, points[i - 1].Id) }));
                dynamic removed = c.ApplyPointCommandAsync(new CfdWorkbench.Core.PointCommand.MakeControl(pointRef.Curve, pointRef.VertexId)).GetAwaiter().GetResult();
                Require(removed.GetType().Name == "Committed", "Control command failed.");
                Require(controller.Selection is Selection.Foil, "Removed handle stayed selected.");
            }
        });

        DesktopChecks.Check("GestureEnd_Committed_EmitsFramesAndP95", () =>
        {
            CfdWorkbench.Desktop.Shell.ShellEvents.Clear();
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                Begin(controller, pointRef); Move(controller, point, 0, 0.004); End(controller);
                var ev = CfdWorkbench.Desktop.Shell.ShellEvents.Read().LastOrDefault(e => e.Name == "gesture.end");
                Require(ev is { Outcome: "committed", Frames: > 0, UpdateP95Ms: >= 0 },
                    $"Gesture end event lacks frame and p95 measurements: outcome={ev?.Outcome}, frames={ev?.Frames}, p95={ev?.UpdateP95Ms}.");
            }
        });

        DesktopChecks.Check("Telemetry_PointEdits_NoIdsOrPositions", () =>
        {
            CfdWorkbench.Desktop.Shell.ShellEvents.Clear();
            var (controller, pointRef, point) = OpenPoint(); using (controller)
            {
                Begin(controller, pointRef); Move(controller, point, 0, 0.004); End(controller);
                string payload = System.Text.Json.JsonSerializer.Serialize(CfdWorkbench.Desktop.Shell.ShellEvents.Read());
                Require(!payload.Contains(pointRef.VertexId, StringComparison.Ordinal) &&
                    !payload.Contains(point.Ordinate.ToString("G17", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal),
                    "Telemetry leaked a point id or position.");
            }
        });
    }

    private static void CheckNudgeCommit(string reason)
    {
        var (controller, pointRef, point) = OpenPoint(); using (controller)
        {
            dynamic c = controller;
            Begin(controller, pointRef, "Keyboard");
            dynamic plain = GestureEnum("NudgeModifier", "Plain");
            c.Nudge(0, 1, plain);
            c.Nudge(0, 1, plain);
            Require(End(controller, reason).GetType().Name == "Committed", $"{reason} did not commit nudge run.");
            Require(controller.CanUndo, "Nudge run has no undo row.");
            controller.Undo();
            var restored = Planform.View(System.Text.Encoding.UTF8.GetBytes(controller.AcceptedSource), "accepted", 0).Trailing.Points[3];
            Require(Math.Abs(restored.Ordinate - point.Ordinate) < 0.0000001 && !controller.CanUndo,
                "Nudge run was not one undo row.");
        }
    }

    private static void CheckCancellation(string reason)
    {
        var (controller, pointRef, point) = OpenPoint(); using (controller)
        {
            string before = controller.AcceptedSource;
            Begin(controller, pointRef); Move(controller, point, 0, 0.004);
            Require(End(controller, reason).GetType().Name == "Cancelled", $"{reason} did not cancel.");
            Require(controller.AcceptedSource == before && controller.Draft is null && !controller.CanUndo,
                $"{reason} changed accepted geometry or history.");
        }
    }

    // Every column of every §6.2 row has its own named check. The fixture is shared so
    // the controller tier adds transitions, rather than 70 certificate preparations.
    private static void RunGestureCellChecks()
    {
        var (controller, reference, _) = OpenPoint();
        using (controller)
        {
            var rows = new (string Event, string[] Outcomes)[]
            {
                ("PointerMovable", ["Pressed", "Ignored", "Ignored", "BusySelect", "SelectedWhileBusy"]),
                ("PointerFixed", ["SelectedLocked", "Ignored", "Ignored", "Busy", "SelectedWhileBusy"]),
                ("MoveUnder3", ["Ignored", "Pressed", "Updated", "Ignored", "Ignored"]),
                ("MoveAtLeast3", ["Ignored", "Dragging", "Updated", "Ignored", "Ignored"]),
                ("Release", ["Ignored", "NoChange", "BusyOrNoChange", "Ignored", "Ignored"]),
                ("ArrowDown", ["Nudging", "Ignored", "Ignored", "Updated", "IgnoredStatus"]),
                ("KeyUp", ["Ignored", "Ignored", "Ignored", "Busy", "Ignored"]),
                ("Escape", ["SelectionCleared", "Cancelled", "Cancelled", "Cancelled", "Ignored"]),
                ("CaptureLost", ["Ignored", "Cancelled", "Cancelled", "Ignored", "Ignored"]),
                ("FocusLost", ["Ignored", "Cancelled", "Cancelled", "Busy", "Continues"]),
                ("DocumentAction", ["Proceed", "CancelProceed", "CommitProceed", "CommitProceed", "WaitProceed"]),
                ("TypedPosition", ["Busy", "Ignored", "Ignored", "Ignored", "Ignored"]),
                ("DirectCommand", ["Busy", "Ignored", "Ignored", "Ignored", "Refused"]),
                ("BusyComplete", ["Ignored", "Ignored", "Ignored", "Ignored", "Idle"])
            };
            foreach (var row in rows)
            foreach (var state in Enum.GetValues<GestureState>())
            {
                string eventName = row.Event;
                string outcome = row.Outcomes[(int)state];
                DesktopChecks.Check($"Controller_Gesture_{state}_{eventName}_{outcome}",
                    () => CheckGestureCell(controller, reference, state, eventName, outcome));
            }
        }
    }

    private static void CheckGestureCell(WorkbenchController controller, PointRef reference,
        GestureState from, string eventName, string expected)
    {
        if (controller.Gesture == GestureState.Busy)
        {
            var pending = (Task<GestureOutcome>?)typeof(WorkbenchController)
                .GetField("pendingCommit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(controller);
            if (pending is null) SetCellState(controller, GestureState.Idle);
            else pending.GetAwaiter().GetResult();
        }
        else if (controller.Gesture != GestureState.Idle)
            End(controller, "Escape");
        var point = controller.Planform!.Trailing.Points.First(item => item.Id == reference.VertexId);
        var fixedPoint = controller.Planform.Leading.Points.First(item => item.Freedom == PointFreedom.Fixed);
        var fixedReference = new PointRef("leading", fixedPoint.Id);
        controller.Select(new Selection.Points([reference]));
        if (from == GestureState.Pressed || from == GestureState.Dragging)
        {
            Require(Begin(controller, reference), "Cell setup: pointer begin failed.");
            if (from == GestureState.Dragging) Move(controller, point, 0, 0.004);
        }
        else if (from == GestureState.Nudging)
        {
            Require(Begin(controller, reference, "Keyboard"), "Cell setup: keyboard begin failed.");
            controller.Nudge(0, 1, NudgeModifier.Plain);
        }
        else if (from == GestureState.Busy)
            SetCellState(controller, GestureState.Busy);
        if (eventName is ("PointerMovable" or "PointerFixed") && from is (GestureState.Nudging or GestureState.Busy))
            controller.Select(new Selection.Foil());
        Require(controller.Gesture == from, $"Cell setup produced {controller.Gesture}, wanted {from}.");
        var originalDraft = controller.Draft;
        var before = controller.AcceptedSource;
        var beforeSelection = controller.Selection;
        GestureOutcome? gestureResult = null;
        CommitOutcome? commandResult = null;
        bool began = false;
        switch (eventName)
        {
            case "PointerMovable": began = Begin(controller, reference); break;
            case "PointerFixed": began = Begin(controller, fixedReference); break;
            case "MoveUnder3": Move(controller, point, 0, 0.002); controller.FlushGestureFrame(); break;
            case "MoveAtLeast3": Move(controller, point, 0, 0.004); controller.FlushGestureFrame(); break;
            case "Release": gestureResult = controller.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult(); break;
            case "ArrowDown":
                began = Begin(controller, reference, "Keyboard");
                if (from is GestureState.Idle or GestureState.Nudging)
                    controller.Nudge(0, 1, NudgeModifier.Plain);
                break;
            case "KeyUp": gestureResult = controller.EndGestureAsync(GestureEnd.KeyUp).GetAwaiter().GetResult(); break;
            case "Escape": gestureResult = controller.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult(); break;
            case "CaptureLost": gestureResult = controller.EndGestureAsync(GestureEnd.CaptureLost).GetAwaiter().GetResult(); break;
            case "FocusLost": gestureResult = controller.EndGestureAsync(GestureEnd.FocusLost).GetAwaiter().GetResult(); break;
            case "DocumentAction":
                if (from == GestureState.Busy)
                {
                    var pendingField = typeof(WorkbenchController).GetField("pendingDirectCommand",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    Require(pendingField is not null, "Busy direct command has no document-action wait handle.");
                    var completion = new TaskCompletionSource<CommitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
                    pendingField!.SetValue(controller, completion.Task);
                    var document = controller.NewFoilAsync();
                    Require(!document.IsCompleted && controller.AcceptedSource == before,
                        "Document action passed an unfinished direct command.");
                    SetCellState(controller, GestureState.Idle);
                    completion.SetResult(new CommitOutcome.Refused("DSL-TARGET", "Synthetic completion"));
                    document.GetAwaiter().GetResult();
                    pendingField!.SetValue(controller, null);
                }
                else controller.NewFoilAsync().GetAwaiter().GetResult();
                break;
            case "TypedPosition":
                began = Begin(controller, reference, "Typed");
                if (began)
                {
                    Move(controller, point, 0, 0.004);
                    gestureResult = controller.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
                }
                break;
            case "DirectCommand": commandResult = controller.ApplyChordAsync("root-chord", "190 mm").GetAwaiter().GetResult(); break;
            case "BusyComplete":
                if (from == GestureState.Busy)
                {
                    SetCellState(controller, GestureState.Idle);
                    commandResult = controller.ApplyChordAsync("root-chord", "190 mm").GetAwaiter().GetResult();
                }
                break;
        }
        if (from == GestureState.Nudging && eventName is ("PointerMovable" or "PointerFixed"))
        {
            var pending = (Task<GestureOutcome>?)typeof(WorkbenchController)
                .GetField("pendingCommit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(controller);
            pending?.GetAwaiter().GetResult();
        }
        switch (expected)
        {
            case "Pressed": Require(controller.Gesture == GestureState.Pressed && controller.Draft is null, "Pointer did not enter Pressed without draft."); break;
            case "SelectedLocked": Require(!began && controller.Gesture == GestureState.Idle && controller.Status.Contains("fixed", StringComparison.OrdinalIgnoreCase), "Fixed point was not selected with lock status."); break;
            case "Dragging": Require(controller.Gesture == GestureState.Dragging && controller.Draft is not null, "Threshold did not start drag."); break;
            case "Nudging": Require(began && controller.Gesture == GestureState.Nudging, "Arrow did not start nudge."); break;
            case "Updated": Require(controller.Gesture == from && controller.Draft?.Generation > originalDraft?.Generation, "Move did not update active draft."); break;
            case "NoChange": Require(gestureResult is GestureOutcome.NoChange && controller.Gesture == GestureState.Idle && controller.AcceptedSource == before, "Click did not cancel unchanged."); break;
            case "BusyOrNoChange": Require(gestureResult is GestureOutcome.Committed or GestureOutcome.NoChange && controller.Gesture == GestureState.Idle, "Drag release did not finish."); break;
            case "Cancelled": Require(gestureResult is GestureOutcome.Cancelled or GestureOutcome.NoChange && controller.Gesture == GestureState.Idle && controller.AcceptedSource == before, "Cancellation changed geometry or stayed active."); break;
            case "SelectionCleared": Require(controller.Selection is Selection.Foil or Selection.None, "Escape did not clear point selection."); break;
            case "Busy": Require(controller.Gesture == GestureState.Busy || gestureResult is GestureOutcome.Committed ||
                    commandResult is CommitOutcome.Committed || from == GestureState.Nudging &&
                    controller.Gesture == GestureState.Idle && controller.AcceptedSource != before,
                    "Event did not enter Busy/commit."); break;
            case "BusySelect": Require(!began && controller.Gesture is (GestureState.Busy or GestureState.Idle) &&
                    HasSelectedPoint(controller, reference), "Nudge pointer down did not end run and select."); break;
            case "SelectedWhileBusy": Require(!began && HasSelectedPoint(controller,
                    eventName == "PointerFixed" ? fixedReference : reference) && controller.Gesture == GestureState.Busy,
                    "Busy pointer down did not select only."); break;
            case "IgnoredStatus": Require(!began && controller.Gesture == GestureState.Busy && controller.Status.Contains("Checking", StringComparison.Ordinal), "Busy arrow did not report checking status."); break;
            case "Proceed": case "CancelProceed": case "CommitProceed": case "WaitProceed":
                Require(controller.Gesture == GestureState.Idle && controller.Draft is null && controller.Inspection is not null, "Document action did not finish cleanly.");
                break;
            case "Refused": Require(commandResult is CommitOutcome.Refused && controller.Gesture == GestureState.Busy, "Busy command was not refused."); break;
            case "Idle": Require(controller.Gesture == GestureState.Idle && commandResult is CommitOutcome.Committed, "Busy completion did not enter Idle."); break;
            default:
                Require(controller.Gesture == from && controller.Draft?.Id == originalDraft?.Id &&
                    controller.AcceptedSource == before && !began && commandResult is not CommitOutcome.Committed &&
                    SameSelection(controller.Selection, beforeSelection), $"{eventName} was not ignored in {from}.");
                break;
        }
    }

    private static bool HasSelectedPoint(WorkbenchController controller, PointRef reference) =>
        controller.Selection is Selection.Points selected && selected.Items.Any(item => item == reference);

    private static bool SameSelection(Selection left, Selection right) =>
        left is Selection.Points a && right is Selection.Points b
            ? a.Items.SequenceEqual(b.Items)
            : left.GetType() == right.GetType();

    private static void SetCellState(WorkbenchController controller, GestureState value) =>
        typeof(WorkbenchController).GetProperty(nameof(WorkbenchController.Gesture))!.SetValue(controller, value);

    // Readiness only: these measurements are reported, never used as an on-screen timing gate.
    public static void RunReadiness()
    {
        var frameMilliseconds = new List<double>();
        var commitMilliseconds = new List<double>();
        for (int gesture = 0; gesture < 5; gesture++)
        {
            var (controller, pointRef, point) = OpenPoint();
            using (controller)
            {
                Begin(controller, pointRef);
                for (int frame = 0; frame < 8; frame++)
                {
                    Move(controller, point, 0, 0.004 + frame * 0.0001);
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    dynamic c = controller;
                    c.FlushGestureFrame();
                    frameMilliseconds.Add(timer.Elapsed.TotalMilliseconds);
                }
                var timerCommit = System.Diagnostics.Stopwatch.StartNew();
                End(controller);
                commitMilliseconds.Add(timerCommit.Elapsed.TotalMilliseconds);
            }
        }
        static double P95(List<double> values) => values.OrderBy(value => value).ElementAt((int)Math.Ceiling(values.Count * .95) - 1);
        Console.WriteLine($"READINESS Readiness_NewFoilDrag_FrameP95Under100Ms value_ms={P95(frameMilliseconds):F3} target_ms=100 samples={frameMilliseconds.Count}");
        Console.WriteLine($"READINESS Readiness_NewFoilCommit_P95Under250Ms value_ms={P95(commitMilliseconds):F3} target_ms=250 samples={commitMilliseconds.Count}");
    }
}
