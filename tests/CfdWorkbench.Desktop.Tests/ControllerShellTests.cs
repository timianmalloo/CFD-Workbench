using System.IO;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

// Track D2 owns this class and adds its named checks with DesktopChecks.Check (docs/design/app-shell.md §9, §12.4, §14).
public static class ControllerShellTests
{
    public static void Run()
    {
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
    }
}
