using CfdWorkbench.Desktop;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using Avalonia.Input;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Color = Avalonia.Media.Color;
using ISolidColorBrush = Avalonia.Media.ISolidColorBrush;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using System.Text;
using System.Text.Json.Nodes;

StartupFailure.Install();
if (args.FirstOrDefault(arg => arg.StartsWith(CfdWorkbench.Desktop.Tests.SelfLaunchTests.FailureProbe, StringComparison.Ordinal)) is { } failureProbe)
    throw new InvalidOperationException(failureProbe);
// `--theme-evidence` (tools/verify-application-adapters.py, Debug): the same in-process prefix as a full run, then only the
// suite that prints the THEME-ROW matrix. The fast ring runs every mode in Release (docs/plans/test-cost.md L2).
bool themeEvidence = args is ["--theme-evidence"];
if (args.Length == 0 || themeEvidence) CfdWorkbench.Desktop.Tests.SelfLaunchTests.Run();

if (args.Contains("--section-canvas", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.SectionCanvasTests.Run();
    Console.WriteLine("SectionCanvas tests passed.");
    Environment.Exit(0);
}

if (args.Contains("--catalog-dialog", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.CatalogDialogTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

// Named-check suites (docs/design/app-shell.md §12.2): each prints PASS/FAIL lines and exits nonzero if any failed.
if (args.Contains("--shell-model", StringComparer.Ordinal))
{
    CfdWorkbench.Desktop.Tests.ShellModelTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--controller-shell", StringComparer.Ordinal))
{
    CfdWorkbench.Desktop.Tests.ControllerShellTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--shell-window", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.ShellWindowTests.Run();
    CfdWorkbench.Desktop.Tests.WindowsShellTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--plan-canvas", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.PlanCanvasTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--views", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.ViewCameraTests.Run();
    CfdWorkbench.Desktop.Tests.ControllerViewTests.Run();
    CfdWorkbench.Desktop.Tests.ElevationTests.Run();
    CfdWorkbench.Desktop.Tests.View3dTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--properties-view", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.PropertiesViewTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--properties-cells", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.PropertiesCellsTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--section-editor", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.ControllerSectionTests.Run();
    CfdWorkbench.Desktop.Tests.SectionEditorTests.Run();
    CfdWorkbench.Desktop.Tests.CatalogDialogTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

if (args.Contains("--status-strip", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.StatusStripTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

// Area 3 Analysis (docs/design/area3-analysis.md §18.2, seam S-A8): one child of the Desktop harness. PRE registers the mode
// with three empty suites; TGL, LAY and PNA fill their own suite files only.
if (args.Contains("--analysis", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.AnalysisToggleTests.Run();
    CfdWorkbench.Desktop.Tests.AnalysisFeedTests.Run();
    CfdWorkbench.Desktop.Tests.AnalysisHistoricalFeedTests.Run();
    CfdWorkbench.Desktop.Tests.AnalysisLayerTests.Run();
    CfdWorkbench.Desktop.Tests.AnalysisPanelTests.Run();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

// Readiness tier (docs/design/m12b-points.md §12.3): never spawned by run-tests.sh or DesktopChecks.Spawn.
// PRE adds the switch; U1a and U1b fill the RunReadiness members it calls.
if (args.Contains("--readiness", StringComparer.Ordinal))
{
    CfdWorkbench.Desktop.Tests.ControllerShellTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.PlanCanvasTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.ControllerViewTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.View3dTests.RunReadiness();
    // PlanCanvasTests.RunReadiness set up the Avalonia app above.
    CfdWorkbench.Desktop.Tests.SectionEditorTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.AnalysisToggleTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.AnalysisFeedTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.AnalysisPanelTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.DxSectionPanelTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.SectionForceViewTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.SectionMainAreaTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.PointsPaneTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.ElevationTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.PropertiesCellsTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.ShellWindowTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.GestureLimitTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.AnalysisLayerTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.ControllerSectionTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.PropertiesViewTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.StatusStripTests.RunReadiness();
    Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.ExitCode);
}

var reviewValues = new Dictionary<string, string>
{
    ["CFDW_REVIEW_MODE"] = "1", ["CFDW_REVIEW_PERSONA"] = "keyboard",
    ["CFDW_REVIEW_SIZE"] = "1024x700", ["CFDW_REVIEW_STATE"] = "invalid-input",
    ["CFDW_REVIEW_THEME"] = "dark", ["CFDW_REVIEW_REDUCED_MOTION"] = "1"
};
var review = NativeReviewOptions.Parse(key => reviewValues.GetValueOrDefault(key))
    ?? throw new Exception("Review selector was ignored");
if (review.Persona != "keyboard" || review.Width != 1024 || review.Height != 700 ||
    review.State != "invalid-input" || review.Theme != "dark" || !review.ReducedMotion ||
    NativeReviewOptions.Parse(_ => null) is not null)
    throw new Exception("Native review selectors did not round-trip");
var motionControl = new Button { Transitions = new Transitions { new DoubleTransition { Property = Button.OpacityProperty, Duration = TimeSpan.FromSeconds(1) } } };
MainWindow.SuppressTransitions(motionControl);
if (motionControl.Transitions is { Count: > 0 }) throw new Exception("Reduced-motion review retained a control transition");
using (var reviewController = new WorkbenchController())
{
    // The shell window types "-" into Span for this state; the controller keeps the certified Example and opens no draft.
    await review.ApplyStateAsync(reviewController);
    if (reviewController.Draft is not null || reviewController.Inspection?.Geometry.Status != GeometryStatus.Certified)
        throw new Exception("Invalid-input review state did not open the certified Example without a draft");
}
bool retiredDraftStateRefused = false;
try { NativeReviewOptions.Parse(key => key == "CFDW_REVIEW_MODE" ? "1" : key == "CFDW_REVIEW_STATE" ? "draft" : null); }
catch (ArgumentException) { retiredDraftStateRefused = true; }
if (!retiredDraftStateRefused) throw new Exception("Review harness still offers the retired per-control draft state");
bool reviewInvalidSizeRefused = false;
try { NativeReviewOptions.Parse(key => key == "CFDW_REVIEW_MODE" ? "1" : key == "CFDW_REVIEW_SIZE" ? "900x600" : null); }
catch (ArgumentException) { reviewInvalidSizeRefused = true; }
if (!reviewInvalidSizeRefused) throw new Exception("Review harness accepted a window below its minimum size");
using (var refusedReview = new WorkbenchController())
{
    await new NativeReviewOptions("designer", 1280, 800, "refused-open", "system", false, null)
        .ApplyStateAsync(refusedReview);
    if (refusedReview.Inspection?.Geometry.Status != GeometryStatus.Certified ||
        string.IsNullOrEmpty(refusedReview.AcceptedSource) || refusedReview.PendingOriginal is null)
        throw new Exception("Refused-open review state did not preserve a real accepted source and refused bytes");
}
using (var denseFileReview = new WorkbenchController())
{
    await new NativeReviewOptions("dense", 1024, 700, "file", "dark", true,
        "src/CfdWorkbench.Desktop/Assets/example.foil").ApplyStateAsync(denseFileReview);
    if (denseFileReview.Inspection?.Geometry.Status != GeometryStatus.Certified)
        throw new Exception("File review state did not admit the real certified source");
}
bool falseNotAssessedRefused = false;
try
{
    using var notAssessedReview = new WorkbenchController();
    await new NativeReviewOptions("designer", 1024, 700, "not-assessed", "system", false,
        "src/CfdWorkbench.Desktop/Assets/example.foil").ApplyStateAsync(notAssessedReview);
}
catch (ArgumentException) { falseNotAssessedRefused = true; }
if (!falseNotAssessedRefused) throw new Exception("Review harness falsely labelled certified source Not assessed");
string geometryPath = Path.Combine(Path.GetTempPath(), $"geometry-{Guid.NewGuid():N}.foil");
try
{
    var validBytes = await File.ReadAllTextAsync("src/CfdWorkbench.Desktop/Assets/example.foil");
    var invalidGeometry = validBytes.Replace("points [(0, 0), (0.1, 0)", "points [(0, 1), (0.1, 0)", StringComparison.Ordinal);
    if (invalidGeometry == validBytes) throw new Exception("Invalid geometry fixture construction failed");
    await File.WriteAllTextAsync(geometryPath, invalidGeometry);
    using var invalidGeometryReview = new WorkbenchController();
    await new NativeReviewOptions("designer", 1024, 700, "invalid-geometry", "system", false, geometryPath)
        .ApplyStateAsync(invalidGeometryReview);
    if (invalidGeometryReview.Inspection is not null || invalidGeometryReview.PendingOriginal is null)
        throw new Exception("Invalid geometry review was accepted or original bytes were lost");
}
finally { File.Delete(geometryPath); }

using var workbench = new WorkbenchController();
await workbench.OpenExampleAsync();
if (workbench.Inspection?.Geometry.Status != GeometryStatus.Certified) throw new Exception("Example is not certified");
if (workbench.Points.Count != 15) throw new Exception("Expected 15 certified samples");
string source = workbench.AcceptedSource;
string acceptedHash = workbench.Inspection.Authored.Binding.SourceHash;
await workbench.OpenFoilAsync(Encoding.UTF8.GetBytes("not FoilDSL"), "invalid.foil");
if (workbench.AcceptedSource != source || workbench.Inspection?.Authored.Binding.SourceHash != acceptedHash)
    throw new Exception("Rejected FoilDSL replaced the active accepted document");
string invalidNativePath = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid():N}.cfdw.json");
try
{
    await File.WriteAllTextAsync(invalidNativePath, "not a native project");
    try { await workbench.OpenPathAsync(invalidNativePath); }
    catch (ContractError) { }
    if (workbench.AcceptedSource != source || workbench.Inspection?.Authored.Binding.SourceHash != acceptedHash)
        throw new Exception("Rejected native project replaced the active accepted document");
}
finally { File.Delete(invalidNativePath); }
await workbench.EnterSectionAsync(0, EntryOrigin.Properties);
var sectionVertex = workbench.SectionCurve(SurfaceSide.Upper)!.Points.First(vertex => vertex.Freedom != PointFreedom.Fixed);
await workbench.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, sectionVertex.Id, sectionVertex.SpanMeters,
    sectionVertex.Ordinate + .01));
if (workbench.Section?.Draft.Cursor != 1) throw new Exception("Section step was not shown");
workbench.CancelSection();
if (workbench.AcceptedSource != source) throw new Exception("Cancel changed accepted source");
await workbench.EnterSectionAsync(0, EntryOrigin.Properties);
await workbench.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, sectionVertex.Id, sectionVertex.SpanMeters,
    sectionVertex.Ordinate + .01));
await workbench.FinishSectionAsync();
if (workbench.AcceptedSource == source) throw new Exception("Finish did not change source");
workbench.Changed += () =>
{
    if (workbench.Provenance.StartsWith("accepted", StringComparison.Ordinal) &&
        workbench.Frame is { } shown && workbench.Inspection is { } active &&
        shown.SourceHash != active.Authored.Binding.SourceHash)
        throw new Exception("A stale frame was displayed as the current accepted identity");
};
workbench.Undo();
if (workbench.AcceptedSource != source) throw new Exception("Undo did not restore source");
workbench.Redo();
if (workbench.AcceptedSource == source) throw new Exception("Redo did not restore edit");
string conflictPath = Path.Combine(Path.GetTempPath(), $"existing-{Guid.NewGuid():N}.cfdw.json");
byte[] foreignImage = Encoding.UTF8.GetBytes("foreign project bytes");
try
{
    await File.WriteAllBytesAsync(conflictPath, foreignImage);
    var conflict = await workbench.SaveAsync(conflictPath);
    if (conflict.Code != "DOC-CONFLICT") throw new Exception("Expected definite create-only save conflict");
    if (workbench.SaveUncertain) throw new Exception("Definite prepublication conflict entered uncertain-save state");
    if (!(await File.ReadAllBytesAsync(conflictPath)).SequenceEqual(foreignImage))
        throw new Exception("Save conflict changed existing disk bytes");
}
finally { File.Delete(conflictPath); }
var uncertainStore = new UncertainStore();
using (var uncertainWorkbench = new WorkbenchController(_ => uncertainStore))
{
    await uncertainWorkbench.OpenExampleAsync();
    string attemptedPath = Path.Combine(Path.GetTempPath(), $"uncertain-{Guid.NewGuid():N}.cfdw.json");
    var first = await uncertainWorkbench.SaveAsync(attemptedPath);
    if (first.Code != "DOC-SAVE-UNCERTAIN" || !uncertainWorkbench.SaveUncertain ||
        uncertainWorkbench.UncertainPath != attemptedPath || !uncertainWorkbench.IsDirty)
        throw new Exception("First Save As did not retain its uncertain path and dirty source");
    if (await uncertainWorkbench.ResolveUncertainSaveAsync() || !uncertainWorkbench.IsDirty)
        throw new Exception("Matching readback or conflict incorrectly acknowledged durability");
    if (!await uncertainWorkbench.ResolveUncertainSaveAsync() || uncertainWorkbench.IsDirty ||
        uncertainWorkbench.NativePath != attemptedPath)
        throw new Exception("Confirmed durable retry did not acknowledge exact first Save As image");
}
var delayedStore = new DelayedStore();
using (var replacingWorkbench = new WorkbenchController(_ => delayedStore))
{
    await replacingWorkbench.OpenExampleAsync();
    string attemptedPath = Path.Combine(Path.GetTempPath(), $"held-{Guid.NewGuid():N}.cfdw.json");
    var heldSave = replacingWorkbench.SaveAsync(attemptedPath);
    await delayedStore.Started.Task;
    await replacingWorkbench.OpenExampleAsync();
    string? replacementId = replacingWorkbench.Inspection!.Authored.Binding.AcceptedId;
    delayedStore.Release();
    await heldSave;
    if (replacingWorkbench.Inspection?.Authored.Binding.AcceptedId != replacementId ||
        replacingWorkbench.NativePath is not null || !replacingWorkbench.IsDirty)
        throw new Exception("Late save acknowledged or attached its path to a replacement session");
}
string recoveryPath = Path.Combine(Path.GetTempPath(), $"recovery-{Guid.NewGuid():N}.cfdw.json");
string seedRecoveryPath = Path.Combine(Path.GetTempPath(), $"seed-recovery-{Guid.NewGuid():N}.cfdw.json");
try
{
    // The rail draft left after the per-control draft API retired is a resumed M1.2a recovery (golden bytes).
    File.Copy("tests/CfdWorkbench.Core.Tests/Fixtures/m12b/m12a-rail-recovery.cfdw", seedRecoveryPath);
    using var savingDraft = new WorkbenchController();
    await savingDraft.OpenPathAsync(seedRecoveryPath);
    string acceptedBeforeDraft = savingDraft.Inspection!.Authored.Binding.SourceHash;
    savingDraft.ResumeRecovery();
    byte[] retainedDraftBytes = savingDraft.Draft!.Bytes;
    var savedRecovery = await savingDraft.SaveAsync(recoveryPath);
    if (savedRecovery.Code != "OK" || savingDraft.IsDirty)
        throw new Exception("Durable recovery save did not settle the unchanged visible draft");
    using var reopenedDraft = new WorkbenchController();
    await reopenedDraft.OpenPathAsync(recoveryPath);
    if (!reopenedDraft.HasRecovery || reopenedDraft.Draft is not null ||
        reopenedDraft.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Native reopen did not offer a separate draft beside the original accepted source");
    if (reopenedDraft.RecoverySource != Encoding.UTF8.GetString(retainedDraftBytes))
        throw new Exception("Native recovery offer does not expose exact retained draft bytes");
    using var recoveryReview = new WorkbenchController();
    await new NativeReviewOptions("screen-reader", 1024, 700, "recovery", "high-contrast", true, recoveryPath)
        .ApplyStateAsync(recoveryReview);
    if (!recoveryReview.HasRecovery || recoveryReview.Draft is not null ||
        recoveryReview.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Recovery review state bypassed the saved-project offer");
    recoveryReview.ResumeRecovery();
    if (recoveryReview.Draft is null || !recoveryReview.Draft.Bytes.SequenceEqual(retainedDraftBytes))
        throw new Exception("Resumed recovery did not bind its current draft bytes");
    reopenedDraft.ResumeRecovery();
    if (reopenedDraft.Draft is null || !reopenedDraft.Draft.Bytes.SequenceEqual(retainedDraftBytes) ||
        reopenedDraft.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Resume did not keep recovery separate from accepted identity");
    var invalidImage = JsonNode.Parse(await File.ReadAllTextAsync(recoveryPath))!;
    invalidImage["recovery"]!["utf8Base64Chunks"] = new JsonArray(Convert.ToBase64String(Encoding.UTF8.GetBytes("not FoilDSL")));
    string invalidRecoveryPath = Path.Combine(Path.GetTempPath(), $"invalid-recovery-{Guid.NewGuid():N}.cfdw.json");
    await File.WriteAllTextAsync(invalidRecoveryPath, invalidImage.ToJsonString());
    using var invalidRecovery = new WorkbenchController();
    try { await invalidRecovery.OpenPathAsync(invalidRecoveryPath); }
    finally { File.Delete(invalidRecoveryPath); }
    invalidRecovery.ResumeRecovery();
    if (invalidRecovery.Draft is null ||
        invalidRecovery.RecoverySource != "not FoilDSL" ||
        invalidRecovery.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Unprojectable recovery draft did not retain raw bytes and accepted source separately");
    if (!invalidRecovery.DraftInputValid) throw new Exception("Unprojectable recovery was marked as invalid numeric input");
    await invalidRecovery.PreviewAsync();
    if (invalidRecovery.Provenance != "draft — unavailable geometry" ||
        !invalidRecovery.DraftInputValid || invalidRecovery.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Unprojectable recovery could not report Preview diagnostics without changing accepted source");
}
finally { File.Delete(recoveryPath); File.Delete(seedRecoveryPath); }
Console.WriteLine("Desktop Example, bounded preview, cancel, apply, undo and redo passed.");

AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
var loadedStyles = (Styles)AvaloniaXamlLoader.Load(
    new Uri("avares://CfdWorkbench.Desktop/Styles.axaml"), null);
var expectedThemeBrushes = new (string Key, string Light, string Dark, string HighContrast)[]
{
    ("CanvasBrush", "#f0f2f1", "#172326", "#000000"),
    ("SurfaceBrush", "#fbfcfb", "#1e2d31", "#000000"),
    ("SurfaceSoftBrush", "#e8edeb", "#2a3d40", "#000000"),
    ("InkBrush", "#1b2929", "#ebf3f0", "#ffffff"),
    ("MutedBrush", "#526362", "#b2c4bf", "#ffffff"),
    ("LineBrush", "#c9d3cf", "#49605b", "#ffffff"),
    ("PrimaryBrush", "#006c67", "#88d8c6", "#ffee58"),
    ("OnPrimaryBrush", "#ffffff", "#172326", "#000000"),
    ("DangerBrush", "#a92e37", "#ffaeb5", "#ffee58"),
    ("ViewportBrush", "#17272c", "#17272c", "#000000"),
    ("ViewportGridBrush", "#344b50", "#344b50", "#ffffff"),
    ("ViewportInkBrush", "#edf4f2", "#edf4f2", "#ffffff"),
    ("FoilBrush", "#85c9c4", "#85c9c4", "#ffee58"),
    ("StationBrush", "#66ddc8", "#66ddc8", "#00ffff")
};
void AssertThemeBrushes(bool emit)
{
    foreach (var (key, light, dark, highContrast) in expectedThemeBrushes)
        foreach (var (variant, expected) in new[] {
            (ThemeVariant.Light, light), (ThemeVariant.Dark, dark),
            (NativeReviewThemes.HighContrast, highContrast) })
        {
            if (!loadedStyles.TryGetResource(key, variant, out var value) ||
                value is not ISolidColorBrush brush || brush.Color != Color.Parse(expected))
                throw new Exception($"Loaded XAML theme brush {variant.Key}/{key}: " +
                    $"expected {expected}, observed {((value as ISolidColorBrush)?.Color.ToString() ?? "missing/non-solid")}");
            if (emit)
                Console.WriteLine($"THEME-RESOURCE {variant.Key}/{key}=#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}");
        }
}
AssertThemeBrushes(emit: true);
foreach (var (key, light, dark, highContrast) in new[] {
    ("SystemControlFocusVisualPrimaryBrush", "#006c67", "#88d8c6", "#ffee58"),
    ("SystemControlFocusVisualSecondaryBrush", "#1b2929", "#ebf3f0", "#ffffff") })
    foreach (var (variant, expected) in new[] {
        (ThemeVariant.Light, light), (ThemeVariant.Dark, dark),
        (NativeReviewThemes.HighContrast, highContrast) })
    {
        if (!loadedStyles.TryGetResource(key, variant, out var value) ||
            value is not ISolidColorBrush brush || brush.Color != Color.Parse(expected) ||
            Math.Abs(brush.Opacity - 1) > 0.000001)
            throw new Exception($"Loaded XAML focus brush {variant.Key}/{key} is missing, translucent, or wrong");
        Console.WriteLine($"THEME-FOCUS-RESOURCE {variant.Key}/{key}=#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}");
    }
loadedStyles.Resources["SurfaceBrush"] = new SolidColorBrush(Color.Parse("#fbfcfb"));
bool shadowMutationRefused = false;
try { AssertThemeBrushes(emit: false); }
catch (Exception error) when (error.Message.Contains("Dark/SurfaceBrush", StringComparison.Ordinal))
{ shadowMutationRefused = true; }
loadedStyles.Resources.Remove("SurfaceBrush");
if (!shadowMutationRefused) throw new Exception("Root-key shadow mutation escaped loaded-resource control");
Console.WriteLine("THEME-SHADOW-MUTATION refused Dark/SurfaceBrush");
AssertThemeBrushes(emit: false);
Console.WriteLine("THEME-RESOURCE-CHECK loaded-XAML Light/Dark/HighContrast 42");
CfdWorkbench.Desktop.Tests.SectionCanvasTests.Run();
// Longest first by measured SUITE-TIME, so the slots never wait on a long part started last (docs/plans/test-cost.md
// L5/L6, B2 9.5). 2026-10-05, load 16-20, seconds: plan-canvas 33.6/28.6, properties-view 26.1/23.0, shell-window
// 24.4/21.5, views 20.2/16.8, section-editor 18.9/17.3, controller-shell 15.3, properties-cells 12.8/11.5,
// status-strip 12.0/10.3/7.0. Greedy on 8 slots in this order ends at 40.6 s; the earlier order ended at 44.7 s.
// SUITE-TIME shows when to re-order.
if (themeEvidence) Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.Spawn("--shell-window --part=1/2", "--shell-window --part=2/2"));
Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.Spawn(
    "--plan-canvas --part=1/2", "--plan-canvas --part=2/2", "--properties-view --part=1/2",
    "--shell-window --part=2/2", "--properties-view --part=2/2", "--shell-window --part=1/2",
    "--views --part=2/2", "--section-editor --part=2/2", "--section-editor --part=1/2",
    "--views --part=1/2", "--controller-shell", "--properties-cells --part=1/2",
    "--status-strip --part=1/3", "--properties-cells --part=2/2", "--status-strip --part=2/3",
    "--status-strip --part=3/3", "--shell-model", "--analysis"));

sealed class UncertainStore : IProjectStore
{
    private byte[]? image;
    private int saves;
    public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
    {
        image = request.Image;
        saves++;
        return Task.FromResult(saves switch
        {
            1 => new SaveResult("DOC-SAVE-UNCERTAIN", Identity.Sha256(image), true, false),
            2 => new SaveResult("DOC-CONFLICT", null, false, false),
            _ => new SaveResult("OK", Identity.Sha256(image), true, true)
        });
    }
    public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default)
    {
        if (image is null) throw new Exception("No captured image");
        return Task.FromResult(new ReadResult(image, Identity.Sha256(image)));
    }
    public void Dispose() { }
}

sealed class DelayedStore : IProjectStore
{
    private readonly TaskCompletionSource<SaveResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private byte[]? image;
    public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
    {
        image = request.Image;
        Started.SetResult(true);
        return completion.Task;
    }
    public void Release()
    {
        if (image is null) throw new Exception("No held save");
        completion.SetResult(new SaveResult("OK", Identity.Sha256(image), true, true));
    }
    public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) => throw new NotSupportedException();
    public void Dispose() { }
}

namespace CfdWorkbench.Desktop.Tests
{
    /// <summary>The Desktop named-check harness, like the Core one (docs/design/app-shell.md §12.2).</summary>
    public static class DesktopChecks
    {
        private static int failures;

        /// <summary>
        /// Ruling 81 (DR-RDY-1): a frame-time readiness budget fails only when the machine is quiet. Above the gate, or where the
        /// load is not recorded, the check prints <c>READINESS-MISS</c> with the load and neither passes nor fails: a loaded run
        /// is not evidence about the frame. The gate is Ruling 84/87's value (24) and it is kept on measured data: the quiet ring
        /// ends at load 12-18 and loaded runs at 40-125 (docs/proof/ring-b2/profile.md), so 24 separates them, and a frame
        /// median at load 12-18 sits at 9-14 ms against the 33 ms budget.
        /// </summary>
        public const double ReadinessLoadGate = 24.0;

        public enum FrameVerdict { Pass, Fail, Miss }

        /// <summary>Pure verdict: over the limit is Fail at a recorded load at or below the gate, else Miss; at or under it, Pass.</summary>
        public static FrameVerdict FrameBudgetVerdict(double valueMs, double limitMs, double? load) =>
            valueMs <= limitMs ? FrameVerdict.Pass
            : load is { } l && l <= ReadinessLoadGate ? FrameVerdict.Fail : FrameVerdict.Miss;

        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "getloadavg")]
        private static extern int GetLoadAverage(double[] values, int count);

        /// <summary>The 1-minute load average; null where libc has no getloadavg (Windows), never a guess.</summary>
        public static double? LoadAverage1()
        {
            try
            {
                var values = new double[1];
                return GetLoadAverage(values, 1) == 1 ? values[0] : null;
            }
            catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException) { return null; }
        }

        /// <summary>The larger of two load readings (before and after the timed frames), so a load spike inside the check counts.</summary>
        public static double? LoadDuring(double? before, double? after) =>
            before is { } b && after is { } a ? Math.Max(a, b) : null;

        private sealed class ReadinessMissException(string line) : Exception(line);

        /// <summary>Applies the Ruling 81 verdict to a measured value; a Miss ends the check without PASS or FAIL.</summary>
        public static void RequireFrameBudget(string name, double valueMs, double limitMs, double? loadBefore)
        {
            double? load = LoadDuring(loadBefore, LoadAverage1());
            string loadText = load is { } l ? l.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "not-recorded";
            switch (FrameBudgetVerdict(valueMs, limitMs, load))
            {
                case FrameVerdict.Fail:
                    throw new Exception(FormattableString.Invariant($"{name} {valueMs:F2} ms is over {limitMs:F0} ms at load {loadText} (gate {ReadinessLoadGate:F0})"));
                case FrameVerdict.Miss:
                    throw new ReadinessMissException(FormattableString.Invariant(
                        $"READINESS-MISS {name} value_ms={valueMs:F2} target_ms={limitMs:F0} load={loadText} gate={ReadinessLoadGate:F0}"));
            }
        }

        /// <summary>
        /// The Check boundary for a Miss, exposed so the planted-input check can drive it: runs the assertion and returns the
        /// printed outcome line, "PASS name", "FAIL name ..." or the READINESS-MISS line. Never prints.
        /// </summary>
        public static string Outcome(string name, Action assertion)
        {
            try { assertion(); return "PASS " + name; }
            catch (ReadinessMissException miss) { return miss.Message; }
            catch (Exception failure) { return "FAIL " + name + " " + failure.Message; }
        }
        /// <summary>
        /// The saved 10-point New foil of the build before Ruling 64 (the Core tests' fixture, copied by the csproj). A check that
        /// needs a real interior control point or a rail index beyond 3 opens it; New foil now ships 4 control vertices per rail.
        /// </summary>
        public static byte[] TenPointFoil() =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "planform-verbs", "new-default-10.foil"));
        // The Core harness's subset selector (comma-separated check-name prefixes), so one check can run in a loop.
        // A prefix that selects no check fails the run, so a subset can never pass empty (HARNESS-SILENT-EXIT).
        private static readonly string[]? only = Environment.GetEnvironmentVariable("CFD_TEST_ONLY")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        private static readonly HashSet<string> matched = [];
        // A suite split across processes (`--part=k/n`, an argument, never an environment variable an old shell could
        // leak): this process runs the checks whose registration index i has i % n == k - 1. Every part enumerates the
        // same registrations, so the parts together run each check exactly once; Spawn proves that from PARTITION lines.
        private static readonly (int Index, int Count)? part = ParsePart(Environment.GetCommandLineArgs());
        private static int registered, ran;

        public static (int Index, int Count)? ParsePart(string[] args)
        {
            string? value = args.LastOrDefault(arg => arg.StartsWith("--part=", StringComparison.Ordinal))?["--part=".Length..];
            if (value is null) return null;
            string[] fields = value.Split('/');
            if (fields.Length == 2 && int.TryParse(fields[0], out int index) && int.TryParse(fields[1], out int count) &&
                index >= 1 && index <= count)
                return (index, count);
            throw new ArgumentException($"--part={value} is not k/n with 1 <= k <= n");
        }

        /// <summary>
        /// The exit code of a named-check suite process: nonzero when any check failed, a selector prefix matched none, or
        /// a part of a split suite ran no check. A part also prints <c>PARTITION k/n of N checks</c> for Spawn to compare.
        /// </summary>
        public static int ExitCode
        {
            get
            {
                string[] unmatched = only is null ? [] : only.Length == 0 ? ["(empty selector)"] : only.Where(prefix => !matched.Contains(prefix)).ToArray();
                foreach (string prefix in unmatched) Console.WriteLine("FAIL SELECTOR " + prefix + " matched no check");
                bool emptyPart = part is not null && only is null && ran == 0;
                if (part is { } p)
                {
                    Console.WriteLine($"PARTITION {p.Index}/{p.Count} of {registered} checks");
                    if (emptyPart) Console.WriteLine($"FAIL PARTITION {p.Index}/{p.Count} ran no check");
                }
                return failures == 0 && unmatched.Length == 0 && !emptyPart ? 0 : 1;
            }
        }

        /// <summary>Runs one named check, prints <c>PASS name</c> or <c>FAIL name …</c>, and continues either way.</summary>
        public static void Check(string name, Action assertion)
        {
            if (only is not null)
            {
                string? prefix = only.FirstOrDefault(candidate => name.StartsWith(candidate, StringComparison.Ordinal));
                if (prefix is null) return;
                matched.Add(prefix);
            }
            // After the selector, so a prefix counts as matched in every part and a subset run splits like a full one.
            if (part is { } p && registered++ % p.Count != p.Index - 1) return;
            ran++;
            // Test-runner boundary: report unexpected exceptions as failures and continue. The STACK line keeps a one-off
            // flake debuggable from the log alone: a job queued by an earlier check surfaces inside this check's
            // RunJobs, and only the stack shows whose job it was (UI-LIFETIME, 2026-10-02).
            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            try { assertion(); Console.WriteLine("PASS " + name); }
            catch (ReadinessMissException miss) { Console.WriteLine(miss.Message); }   // Ruling 81: no PASS, no FAIL
            catch (Exception failure)
            {
                failures++;
                Console.WriteLine("FAIL " + name + " " + failure.GetType().Name + ": " + failure.Message);
                Console.WriteLine("STACK " + name + " " + failure.ToString().ReplaceLineEndings(" | "));
            }
            // Per-check wall time, for the Desktop CPU profile (B5); not read by check-test-costs.py.
            Console.WriteLine("COST " + name + " " + System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalMilliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Runs every suite mode as a child process, at most 3/8 of the processors at a time, and returns the first nonzero
        /// child exit code in mode order, else 0. A mode may carry a part (`--shell-window --part=1/2`); the parts of one
        /// suite must be 1..n and report one check count, or the run fails. Each child's output is buffered and printed in
        /// mode order once it finishes, so the log reads as a sequential run would. The children share no files, ports or
        /// state: every scratch path is a GUID name under the temp directory (docs/reviews/test-ci-waste.md §10 and §12).
        /// </summary>
        public static int Spawn(params string[] modes)
        {
            // A child uses about 1.4 cores. The proof budget now counts deterministic bit-work rather than wall time,
            // so thread contention cannot starve proofs into GEOMETRY-BUDGET failures. 1/2 (8 of 16) uses the available
            // headroom to parallelize child runs faster. B3: 5/8 (10 of 16). At quiet load, 3 runs each: 8 slots read Desktop
            // 46.0 / 46.4 / 43.7 s, 10 slots 40.5 / 40.4 / 40.3 s (the greedy fill of the measured child times predicts 40.5 and 34.5 s at 8 and 10).
            using var slots = new SemaphoreSlim(Math.Clamp(Environment.ProcessorCount * 5 / 8, 1, modes.Length));
            var runs = new Task<(List<(bool Error, string Text)> Lines, int ExitCode, double Seconds)>[modes.Length];
            for (int index = 0; index < modes.Length; index++)
            {
                string mode = modes[index];
                slots.Wait(); // start strictly in mode order; the thread pool alone would not keep that order
                runs[index] = Task.Run(() => { try { return RunBuffered(mode); } finally { slots.Release(); } });
            }
            int exitCode = 0;
            var partCounts = new Dictionary<string, List<string>>();
            for (int index = 0; index < modes.Length; index++)
            {
                var (lines, childExit, seconds) = runs[index].GetAwaiter().GetResult();
                foreach (var (error, text) in lines) (error ? Console.Error : Console.Out).WriteLine(text);
                Console.WriteLine($"SUITE {modes[index]} exit {childExit}");
                Console.WriteLine(FormattableString.Invariant($"SUITE-TIME {modes[index]} {seconds:F1} s"));
                string[] spec = modes[index].Split(' ');
                if (spec.Length > 1)
                {
                    string counts = string.Join(",", lines.Where(line => !line.Error && line.Text.StartsWith("PARTITION ", StringComparison.Ordinal))
                        .Select(line => line.Text.Split(" of ")[^1]));
                    partCounts.TryAdd(spec[0], []);
                    partCounts[spec[0]].Add(spec[1] + " " + counts);
                }
                if (childExit == 0) continue;
                Console.WriteLine($"FAIL {modes[index]} exited {childExit}");
                if (exitCode == 0) exitCode = childExit;
            }
            // The parts run each check once only if the mode list holds parts 1..n of one n, and each part enumerated the
            // same registrations (one PARTITION line each, one count). Otherwise a check could drop out silently.
            foreach (var (mode, reports) in partCounts)
            {
                var parts = reports.Select(report => report.Split(' ', 2)).ToArray();
                int count = parts.Length;
                bool complete = parts.Select(fields => fields[0]).Order(StringComparer.Ordinal)
                    .SequenceEqual(Enumerable.Range(1, count).Select(k => $"--part={k}/{count}").Order(StringComparer.Ordinal));
                var counts = parts.Select(fields => fields[1]).ToArray();
                if (complete && counts.All(value => value.Length > 0 && !value.Contains(',')) && counts.Distinct().Count() == 1) continue;
                Console.WriteLine($"FAIL PARTITION {mode} parts are incomplete or enumerated different checks: [{string.Join(" | ", reports)}]");
                if (exitCode == 0) exitCode = 1;
            }
            return exitCode;
        }

        private static (List<(bool Error, string Text)> Lines, int ExitCode, double Seconds) RunBuffered(string mode)
        {
            string[] spec = mode.Split(' ');
            var info = SelfLaunch.StartInfo(spec[0]);
            foreach (string argument in spec[1..]) info.ArgumentList.Add(argument);
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            var lines = new List<(bool Error, string Text)>();
            using var child = new System.Diagnostics.Process { StartInfo = info };
            child.OutputDataReceived += (_, line) => { if (line.Data is not null) lock (lines) lines.Add((false, line.Data)); };
            child.ErrorDataReceived += (_, line) => { if (line.Data is not null) lock (lines) lines.Add((true, line.Data)); };
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            child.Start();
            child.BeginOutputReadLine();
            child.BeginErrorReadLine();
            // A hung native-window check must not hang the join (test-cost F-2): past the limit the child's whole process
            // tree is killed and the log names the mode. 120 s is about 2.4x the slowest child under load 24 (49.5 s).
            int limit = int.TryParse(Environment.GetEnvironmentVariable("CFD_TEST_CHILD_TIMEOUT_SECONDS"), out int seconds) && seconds > 0 ? seconds : 120;
            bool timedOut = !child.WaitForExit(TimeSpan.FromSeconds(limit));
            if (timedOut) child.Kill(entireProcessTree: true);
            child.WaitForExit(); // after a true WaitForExit(timeout), this waits until both redirected streams reach end of file
            if (timedOut) lines.Add((false, $"FAIL TIMEOUT {mode} after {limit} s (CFD_TEST_CHILD_TIMEOUT_SECONDS); its process tree was killed"));
            return (lines, timedOut ? 124 : child.ExitCode, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds);
        }
    }
}
