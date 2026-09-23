using CfdWorkbench.Desktop;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using Avalonia.Input;
using Avalonia.Automation.Peers;
using System.Text;

using var workbench = new WorkbenchController();
await workbench.OpenExampleAsync();
var accessibleViewport = new Viewport { Frame = workbench.Frame };
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!);
var viewportPeer = ControlAutomationPeer.CreatePeerForElement(accessibleViewport);
var visualChildren = accessibleViewport.SemanticControls;
var semanticChildren = visualChildren.Select(ControlAutomationPeer.CreatePeerForElement).ToArray();
if (viewportPeer.GetAutomationControlType() != AutomationControlType.Group ||
    semanticChildren.Length != accessibleViewport.Semantics.Count ||
    !semanticChildren.Any(child => child.GetName().Contains("station", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("locked", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("editable", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("trailing control vertex", StringComparison.OrdinalIgnoreCase)))
    throw new Exception("Viewport peer lacks semantic station and constrained CV children");
var stableChild = visualChildren.First();
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!);
if (!ReferenceEquals(stableChild, accessibleViewport.SemanticControls.First()))
    throw new Exception("Refresh replaced a stable semantic station peer");
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
var rail = workbench.Inspection.Authored.Rails.Single(r => r.Name == "leading");
var control = rail.Controls.First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
workbench.BeginEdit("leading", control.Id);
workbench.UpdateDraft(control.OrdinateSi + .005);
await workbench.PreviewAsync();
if (workbench.Provenance != "preview") throw new Exception("Preview was not shown");
if (workbench.Points.Count != 15) throw new Exception("Preview did not sample bounded geometry");
workbench.Cancel();
if (workbench.AcceptedSource != source) throw new Exception("Cancel changed accepted source");
for (int attempt = 0; attempt < 100 && workbench.Provenance != "accepted"; attempt++) await Task.Delay(10);
if (workbench.Provenance != "accepted") throw new Exception("Cancel did not restore accepted view");
workbench.BeginEdit("leading", control.Id);
workbench.UpdateDraft(control.OrdinateSi + .005);
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
try
{
    using var savingDraft = new WorkbenchController();
    await savingDraft.OpenExampleAsync();
    string acceptedBeforeDraft = savingDraft.Inspection!.Authored.Binding.SourceHash;
    var recoveryTarget = savingDraft.Inspection.Authored.Rails.Single(r => r.Name == "leading").Controls
        .First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
    savingDraft.BeginEdit("leading", recoveryTarget.Id);
    savingDraft.UpdateDraft(recoveryTarget.OrdinateSi + .005);
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
    reopenedDraft.ResumeRecovery();
    if (reopenedDraft.Draft is null || !reopenedDraft.Draft.Bytes.SequenceEqual(retainedDraftBytes) ||
        reopenedDraft.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Resume did not keep recovery separate from accepted identity");
}
finally { File.Delete(recoveryPath); }
foreach (string railName in new[] { "leading", "trailing" })
{
    using var targeted = new WorkbenchController();
    await targeted.OpenExampleAsync();
    var controlAtTip = targeted.Inspection!.Authored.Rails.Single(r => r.Name == railName).Controls
        .Where(c => c.Editable && c.Eta > 0 && c.Eta < 1).OrderByDescending(c => c.Eta).First();
    bool upper = true;
    double normalizedX = railName == "leading" ? 0 : 1;
    var baseline = Geometry.PointAt(targeted.Inspection.Geometry.Certificate!, controlAtTip.Eta, normalizedX, upper);
    double baselineX = (baseline.X.Lower + baseline.X.Upper) / 2;
    targeted.BeginEdit(railName, controlAtTip.Id);
    for (int attempt = 0; attempt < 100 && targeted.Frame?.InteriorEta != controlAtTip.Eta; attempt++) await Task.Delay(10);
    if (targeted.Frame?.InteriorEta != controlAtTip.Eta)
        throw new Exception("Selected target did not retarget the accepted section slice");
    targeted.UpdateDraft(controlAtTip.OrdinateSi + .005);
    await targeted.PreviewAsync();
    var displayed = targeted.Points.Single(p => p.Eta == controlAtTip.Eta && p.NormalizedX == normalizedX && p.Upper);
    if (Math.Abs(displayed.X - baselineX) < 1e-6)
        throw new Exception($"Tip-side {railName} edit does not move its certified interior display sample");
    targeted.Cancel();
    for (int attempt = 0; attempt < 100 && targeted.Frame?.InteriorEta != controlAtTip.Eta; attempt++) await Task.Delay(10);
    if (targeted.Frame?.InteriorEta != controlAtTip.Eta || targeted.Frame.Provenance != "accepted")
        throw new Exception("Cancel did not return to the same accepted eta slice");
}
using (var invalidInput = new WorkbenchController())
{
    await invalidInput.OpenExampleAsync();
    var editable = invalidInput.Inspection!.Authored.Rails.Single(r => r.Name == "leading").Controls
        .First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
    invalidInput.BeginEdit("leading", editable.Id);
    invalidInput.UpdateDraft(editable.OrdinateSi + .005);
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
    invalidInput.UpdateDraft(editable.OrdinateSi + .006);
    await invalidInput.PreviewAsync();
    if (invalidInput.Provenance != "preview") throw new Exception("Corrected numeric input did not restore Preview");
}
if (MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Meta, macOS: true) != "undo" ||
    MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Meta | KeyModifiers.Shift, macOS: true) != "redo" ||
    MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Control, macOS: true) is not null ||
    MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Control, macOS: false) != "undo" ||
    MainWindow.AcceptedHistoryShortcut(Key.Y, KeyModifiers.Control, macOS: false) != "redo")
    throw new Exception("Native accepted-history shortcuts do not match macOS and Windows modifiers");
if (MainWindow.NextRegionIndex(-1, false, [true, true, true, true]) != 0 ||
    MainWindow.NextRegionIndex(-1, true, [true, true, true, true]) != 3 ||
    MainWindow.NextRegionIndex(0, true, [true, true, true, true]) != 3 ||
    MainWindow.NextRegionIndex(2, false, [true, false, true, true]) != 3 ||
    MainWindow.NextRegionIndex(2, false, [true, false, true, false]) != 0)
    throw new Exception("F6 region cycling did not skip unavailable regions in both directions");
Console.WriteLine("Desktop Example, bounded preview, cancel, apply, undo and redo passed.");

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
