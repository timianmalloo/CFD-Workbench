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
if (args.Length == 0) CfdWorkbench.Desktop.Tests.SelfLaunchTests.Run();

if (args.Contains("--section-canvas", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.SectionCanvasTests.Run();
    Console.WriteLine("SectionCanvas tests passed.");
    Environment.Exit(0);
}

if (args.Contains("--section-flow", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.SectionFlowTests.Run();
    Console.WriteLine("Section flow tests passed.");
    Environment.Exit(0);
}

if (args.Contains("--section-tools", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.SectionToolsTests.Run();
    Console.WriteLine("Section tools tests passed.");
    Environment.Exit(0);
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

// Readiness tier (docs/design/m12b-points.md §12.3): never spawned by run-tests.sh or DesktopChecks.Spawn.
// PRE adds the switch; U1a and U1b fill the RunReadiness members it calls.
if (args.Contains("--readiness", StringComparer.Ordinal))
{
    CfdWorkbench.Desktop.Tests.ControllerShellTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.PlanCanvasTests.RunReadiness();
    CfdWorkbench.Desktop.Tests.ControllerViewTests.RunReadiness();
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
var accessibleViewport = new Viewport { Frame = workbench.Frame };
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!);
var viewportPeer = ControlAutomationPeer.CreatePeerForElement(accessibleViewport);
var visualChildren = accessibleViewport.SemanticControls;
var semanticChildren = visualChildren.Select(ControlAutomationPeer.CreatePeerForElement).ToArray();
int authoredSemanticCount = workbench.Inspection!.Authored.Assignments.Count +
    workbench.Inspection.Authored.Rails.Sum(rail => rail.Controls.Count);
if (viewportPeer.GetAutomationControlType() != AutomationControlType.Group ||
    semanticChildren.Length != authoredSemanticCount ||
    !semanticChildren.Any(child => child.GetName().Contains("station", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("locked", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("editable", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("trailing control vertex", StringComparison.OrdinalIgnoreCase)))
    throw new Exception("Viewport peer lacks semantic station and constrained CV children");
var stableChild = visualChildren.First();
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!);
if (!ReferenceEquals(stableChild, accessibleViewport.SemanticControls.First()))
    throw new Exception("Refresh replaced a stable semantic station peer");
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!, "trailing", "cv-5");
if (accessibleViewport.SemanticControls.Count != authoredSemanticCount ||
    !accessibleViewport.SemanticControls[workbench.Inspection.Authored.Assignments.Count].Text!.Contains("trailing control vertex cv-5") ||
    !ReferenceEquals(stableChild, accessibleViewport.SemanticControls.First()))
    throw new Exception("Selected tip CV did not remain accessible with all authored controls");
if (accessibleViewport.AnnotationScroller.VerticalScrollBarVisibility != ScrollBarVisibility.Auto)
    throw new Exception("Minimum-window geometry or dense annotation scrolling regressed");
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
// The draft lifecycle (Preview, Cancel, Apply, Undo, Redo) runs on a station-section draft; the per-control draft is retired.
var sectionVertex = workbench.SectionView(0).Upper.First(vertex => !vertex.Fixed);
workbench.BeginSectionEdit(0, SectionScope.Shared, sectionVertex.Side, sectionVertex.Id);
workbench.UpdateSectionDraft(sectionVertex.X, sectionVertex.Y + .01);
await workbench.PreviewAsync();
if (workbench.Provenance != "preview") throw new Exception("Preview was not shown");
if (workbench.Points.Count != 15) throw new Exception("Preview did not sample bounded geometry");
workbench.Cancel();
if (workbench.AcceptedSource != source) throw new Exception("Cancel changed accepted source");
for (int attempt = 0; attempt < 100 && workbench.Provenance != "accepted"; attempt++) await Task.Delay(10);
if (workbench.Provenance != "accepted") throw new Exception("Cancel did not restore accepted view");
workbench.BeginSectionEdit(0, SectionScope.Shared, sectionVertex.Side, sectionVertex.Id);
workbench.UpdateSectionDraft(sectionVertex.X, sectionVertex.Y + .01);
await workbench.PreviewAsync();
workbench.Apply();
if (workbench.AcceptedSource == source) throw new Exception("Apply did not change source");
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
using (var invalidInput = new WorkbenchController())
{
    await invalidInput.OpenExampleAsync();
    var invalidVertex = invalidInput.SectionView(0).Upper.First(vertex => !vertex.Fixed);
    invalidInput.BeginSectionEdit(0, SectionScope.Shared, invalidVertex.Side, invalidVertex.Id);
    invalidInput.UpdateSectionDraft(invalidVertex.X, invalidVertex.Y + .01);
    invalidInput.InvalidateDraftInput();
    if (invalidInput.DraftInputValid) throw new Exception("Invalid visible numeric input still permits preview");
    try { await invalidInput.PreviewAsync(); throw new Exception("Preview accepted invalid visible numeric input"); }
    catch (ContractError error) when (error.Code == "DSL-INVALID-NUMERIC") { }
    try
    {
        await invalidInput.SaveAsync(Path.Combine(Path.GetTempPath(), $"invalid-input-{Guid.NewGuid():N}.cfdw.json"));
        throw new Exception("Save persisted an earlier draft value while visible input was invalid");
    }
    catch (ContractError error) when (error.Code == "DSL-INVALID-NUMERIC") { }
    invalidInput.UpdateSectionDraft(invalidVertex.X, invalidVertex.Y + .011);
    await invalidInput.PreviewAsync();
    if (invalidInput.Provenance != "preview") throw new Exception("Corrected numeric input did not restore Preview");
}
var renderFrame = new DisplayFrame([], default!, .5, 0, "source", "accepted");
var renderViewport = new Viewport { Frame = renderFrame };
long initialRevision = renderViewport.FrameRevision;
renderViewport.Frame = renderFrame;
if (renderViewport.FrameRevision != initialRevision)
    throw new Exception("Repeated Refresh advanced the same viewport frame revision");
renderViewport.InvalidateFrameForMetric();
if (renderViewport.FrameRevision != initialRevision + 1)
    throw new Exception("Metric invalidation did not create a fresh target revision");
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
Environment.Exit(CfdWorkbench.Desktop.Tests.DesktopChecks.Spawn(
    "--section-flow", "--section-tools", "--shell-model", "--controller-shell", "--shell-window", "--plan-canvas", "--views",
    "--properties-view", "--properties-cells"));

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
        // The Core harness's subset selector (comma-separated check-name prefixes), so one check can run in a loop.
        // A prefix that selects no check fails the run, so a subset can never pass empty (HARNESS-SILENT-EXIT).
        private static readonly string[]? only = Environment.GetEnvironmentVariable("CFD_TEST_ONLY")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        private static readonly HashSet<string> matched = [];

        /// <summary>The exit code of a named-check suite process: nonzero when any check failed or a selector prefix matched none.</summary>
        public static int ExitCode
        {
            get
            {
                string[] unmatched = only is null ? [] : only.Length == 0 ? ["(empty selector)"] : only.Where(prefix => !matched.Contains(prefix)).ToArray();
                foreach (string prefix in unmatched) Console.WriteLine("FAIL SELECTOR " + prefix + " matched no check");
                return failures == 0 && unmatched.Length == 0 ? 0 : 1;
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
            // Test-runner boundary: report unexpected exceptions as failures and continue.
            try { assertion(); Console.WriteLine("PASS " + name); }
            catch (Exception failure) { failures++; Console.WriteLine("FAIL " + name + " " + failure.GetType().Name + ": " + failure.Message); }
        }

        /// <summary>
        /// Runs every suite mode as a child process, at most half the processors (capped at 4) at a time, and returns the
        /// first nonzero child exit code in mode order, else 0. Each child's output is buffered and printed in mode order
        /// once it finishes, so the log reads as a sequential run would. The children share no files, ports or state:
        /// every scratch path is a GUID name under the temp directory (docs/reviews/test-ci-waste.md, Desktop suite profile).
        /// </summary>
        public static int Spawn(params string[] modes)
        {
            using var slots = new SemaphoreSlim(Math.Clamp(Environment.ProcessorCount / 2, 1, 4));
            var runs = new Task<(List<(bool Error, string Text)> Lines, int ExitCode, double Seconds)>[modes.Length];
            for (int index = 0; index < modes.Length; index++)
            {
                string mode = modes[index];
                slots.Wait(); // start strictly in mode order; the thread pool alone would not keep that order
                runs[index] = Task.Run(() => { try { return RunBuffered(mode); } finally { slots.Release(); } });
            }
            int exitCode = 0;
            for (int index = 0; index < modes.Length; index++)
            {
                var (lines, childExit, seconds) = runs[index].GetAwaiter().GetResult();
                foreach (var (error, text) in lines) (error ? Console.Error : Console.Out).WriteLine(text);
                Console.WriteLine($"SUITE {modes[index]} exit {childExit}");
                Console.WriteLine(FormattableString.Invariant($"SUITE-TIME {modes[index]} {seconds:F1} s"));
                if (childExit == 0) continue;
                Console.WriteLine($"FAIL {modes[index]} exited {childExit}");
                if (exitCode == 0) exitCode = childExit;
            }
            return exitCode;
        }

        private static (List<(bool Error, string Text)> Lines, int ExitCode, double Seconds) RunBuffered(string mode)
        {
            var info = SelfLaunch.StartInfo(mode);
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
            child.WaitForExit(); // with no timeout this also waits until both redirected streams reach end of file
            return (lines, child.ExitCode, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds);
        }
    }
}
