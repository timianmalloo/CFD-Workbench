using Avalonia;
using Avalonia.Automation;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using System.Globalization;

namespace CfdWorkbench.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly WorkbenchController workbench = new();
    private readonly List<(string Rail, AuthoredControl Control, string Unit)> targets = [];
    private readonly Button exampleButton, openButton, saveButton, undoButton, redoButton;
    private readonly Button previewButton, applyButton, cancelButton, acceptIdsButton, resumeRecoveryButton, discardRecoveryButton;
    private readonly TextBlock stateBanner, viewportProvenance, sectionReadout, sectionPosition, sourceLabel, identityReadout, unitLabel,
        draftReadout, importReadout, recoveryReadout, eventReadout, statusBar;
    private readonly ListBox stationList, controlList, sampleList;
    private readonly TextBox numericInput, sourceText;
    private readonly TabControl documentTabs;
    private readonly Viewport viewport, sectionViewport;
    private bool refreshing;
    private bool closeApproved;
    private string? adapterError;
    private string? boundDraftId;
    private readonly NativeReviewOptions? review = NativeReviewOptions.Current;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        if (review is not null)
        {
            Width = review.Width;
            Height = review.Height;
            Title += $" · REVIEW {review.Persona} / {review.State} / {review.Theme} / {(review.ReducedMotion ? "reduced motion" : "motion default")}";
            RequestedThemeVariant = review.Theme switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                "high-contrast" => NativeReviewThemes.HighContrast,
                _ => ThemeVariant.Default
            };
        }
        T Find<T>(string name) where T : Control => this.FindControl<T>(name) ?? throw new InvalidOperationException($"Missing {name}");
        exampleButton = Find<Button>("ExampleButton"); openButton = Find<Button>("OpenButton");
        saveButton = Find<Button>("SaveButton"); undoButton = Find<Button>("UndoButton"); redoButton = Find<Button>("RedoButton");
        previewButton = Find<Button>("PreviewButton"); applyButton = Find<Button>("ApplyButton");
        cancelButton = Find<Button>("CancelButton"); acceptIdsButton = Find<Button>("AcceptIdsButton");
        resumeRecoveryButton = Find<Button>("ResumeRecoveryButton"); discardRecoveryButton = Find<Button>("DiscardRecoveryButton");
        stateBanner = Find<TextBlock>("StateBanner"); viewportProvenance = Find<TextBlock>("ViewportProvenance");
        sectionReadout = Find<TextBlock>("SectionReadout"); sectionPosition = Find<TextBlock>("SectionPosition");
        sourceLabel = Find<TextBlock>("SourceLabel");
        identityReadout = Find<TextBlock>("IdentityReadout"); unitLabel = Find<TextBlock>("UnitLabel");
        draftReadout = Find<TextBlock>("DraftReadout"); importReadout = Find<TextBlock>("ImportReadout");
        recoveryReadout = Find<TextBlock>("RecoveryReadout");
        eventReadout = Find<TextBlock>("EventReadout"); statusBar = Find<TextBlock>("StatusBar");
        stationList = Find<ListBox>("StationList"); controlList = Find<ListBox>("ControlList");
        sampleList = Find<ListBox>("SampleList"); numericInput = Find<TextBox>("NumericInput");
        sourceText = Find<TextBox>("SourceText"); documentTabs = Find<TabControl>("DocumentTabs");
        viewport = Find<Viewport>("FoilViewport");
        sectionViewport = Find<Viewport>("SectionViewport");
        workbench.Changed += () => Dispatcher.UIThread.Post(Refresh);
        exampleButton.Click += async (_, _) => await Guarded(async () => { if (await MayReplaceAsync()) await workbench.OpenExampleAsync(); });
        openButton.Click += async (_, _) => await Guarded(OpenAsync);
        saveButton.Click += async (_, _) => await Guarded(SaveWithPickerAsync);
        undoButton.Click += async (_, _) => await Guarded(() => { workbench.Undo(); return Task.CompletedTask; });
        redoButton.Click += async (_, _) => await Guarded(() => { workbench.Redo(); return Task.CompletedTask; });
        previewButton.Click += async (_, _) => await Guarded(() => workbench.PreviewAsync());
        applyButton.Click += async (_, _) => await Guarded(() => { workbench.Apply(); return Task.CompletedTask; });
        cancelButton.Click += (_, _) => { workbench.Cancel(); Refresh(); };
        acceptIdsButton.Click += async (_, _) => await Guarded(() => workbench.AcceptCandidateAsync());
        resumeRecoveryButton.Click += async (_, _) => await Guarded(() => { workbench.ResumeRecovery(); return Task.CompletedTask; });
        discardRecoveryButton.Click += async (_, _) => await Guarded(() => { workbench.DiscardRecovery(); return Task.CompletedTask; });
        controlList.SelectionChanged += OnControlSelection;
        numericInput.TextChanged += OnNumericChanged;
        numericInput.KeyDown += OnNumericKeyDown;
        KeyDown += OnWindowKeyDown;
        Closing += OnClosing;
        Closed += (_, _) => workbench.Dispose();
        Opened += async (_, _) =>
        {
            Console.Error.WriteLine("NATIVE-STARTUP window-opened");
            if (Environment.GetEnvironmentVariable("CFDW_STARTUP_SMOKE") == "1")
            {
                Console.Error.WriteLine("NATIVE-STARTUP smoke-opened");
                Dispatcher.UIThread.Post(Close, DispatcherPriority.Background);
                return;
            }
            await Guarded(() => review is null ? workbench.OpenExampleAsync() : review.ApplyStateAsync(workbench));
            if (review is not null)
            {
                if (review.State == "invalid-input" && workbench.Draft is not null)
                    numericInput.Text = "-";
                ApplyReviewMotionPreference();
                Dispatcher.UIThread.Post(() => ReviewFocusControl(review.Persona).Focus(), DispatcherPriority.Input);
            }
        };
        if (review?.ReducedMotion == true) LayoutUpdated += (_, _) => ApplyReviewMotionPreference();
        Refresh();
    }

    private async Task Guarded(Func<Task> action)
    {
        adapterError = null;
        try { await action(); }
        catch (ContractError error) { adapterError = $"{error.Code}: Action refused; accepted source retained."; }
        catch (OperationCanceledException) { adapterError = "Operation cancelled. Accepted source retained."; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { adapterError = "DOC-IO: File operation failed. Accepted source retained."; }
        catch (Exception error) when (review is not null && (error is ArgumentException or InvalidOperationException))
        { adapterError = $"REVIEW-REFUSED: {error.Message}"; }
        Refresh();
    }

    private async Task OpenAsync()
    {
        if (!await MayReplaceAsync()) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        { Title = "Open FoilDSL or CFD Workbench project", AllowMultiple = false });
        if (files.Count == 0) return;
        using var file = files[0];
        await workbench.OpenPathAsync(file.Path.LocalPath);
    }

    private async Task SaveWithPickerAsync()
    {
        string? destination = workbench.NativePath;
        if (destination is null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            { Title = "Save native CFD Workbench project", SuggestedFileName = "foil.cfdw.json" });
            if (file is null) return;
            using (file) destination = file.Path.LocalPath;
        }
        if (!destination.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
            throw new ContractError("DOC-TYPE");
        await workbench.SaveAsync(destination);
    }

    private void OnControlSelection(object? sender, SelectionChangedEventArgs args)
    {
        if (refreshing || controlList.SelectedIndex < 0 || controlList.SelectedIndex >= targets.Count) return;
        adapterError = null;
        var selected = targets[controlList.SelectedIndex];
        if (workbench.Draft is { } owned)
        {
            if (owned.Rail != selected.Rail || owned.VertexId != selected.Control.Id)
                stateBanner.Text = $"Inspecting {selected.Rail} {selected.Control.Id}; numeric field still owns {owned.Rail} {owned.VertexId}.";
            return;
        }
        refreshing = true;
        unitLabel.Text = selected.Unit;
        double scale = selected.Unit switch { "mm" => 1000, "cm" => 100, _ => 1 };
        numericInput.Text = (selected.Control.OrdinateSi * scale).ToString("G9", CultureInfo.InvariantCulture);
        refreshing = false;
        if (selected.Control.Editable)
            try { workbench.BeginEdit(selected.Rail, selected.Control.Id); }
            catch (ContractError error) { stateBanner.Text = $"{error.Code}: control is read-only."; }
        Refresh();
    }

    private void OnNumericChanged(object? sender, TextChangedEventArgs args)
    {
        if (refreshing || workbench.Draft is not { } owned) return;
        adapterError = null;
        var selected = targets.SingleOrDefault(item => item.Rail == owned.Rail && item.Control.Id == owned.VertexId);
        if (selected.Control is null) { workbench.InvalidateDraftInput(); Refresh(); return; }
        if (!double.TryParse(numericInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double display) || !double.IsFinite(display))
        {
            workbench.InvalidateDraftInput();
            Refresh();
            return;
        }
        double scale = selected.Unit switch { "mm" => 1000, "cm" => 100, _ => 1 };
        try { workbench.UpdateDraft(display / scale); }
        catch (ContractError error) { workbench.InvalidateDraftInput(); stateBanner.Text = $"{error.Code}: draft update refused."; }
        Refresh();
    }

    private async void OnNumericKeyDown(object? sender, KeyEventArgs args)
    {
        if (workbench.Draft is null) return;
        if (args.Key == Key.Escape)
        {
            args.Handled = true;
            workbench.Cancel();
            Refresh();
        }
        else if (args.Key == Key.Enter)
        {
            args.Handled = true;
            await Guarded(async () =>
            {
                if (workbench.Provenance == "preview") workbench.Apply();
                else await workbench.PreviewAsync();
            });
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.F6)
        {
            var groups = new Control[][]
            {
                [stationList, controlList, sampleList], [viewport], [documentTabs],
                [numericInput, previewButton, openButton]
            };
            int current = Array.FindIndex(groups, group => group.Any(control => control.IsKeyboardFocusWithin));
            bool[] available = groups.Select(group => group.Any(control => control.Focusable && control.IsVisible && control.IsEnabled)).ToArray();
            bool reverse = args.KeyModifiers.HasFlag(KeyModifiers.Shift);
            for (int attempt = 0; attempt < groups.Length; attempt++)
            {
                int next = NextRegionIndex(current, reverse, available);
                if (next < 0) break;
                foreach (var candidate in groups[next])
                    if (candidate.Focusable && candidate.IsVisible && candidate.IsEnabled && candidate.Focus())
                    {
                        args.Handled = true;
                        return;
                    }
                available[next] = false;
                current = next;
            }
        }
        else if (workbench.Draft is null && AcceptedHistoryShortcut(args.Key, args.KeyModifiers, OperatingSystem.IsMacOS()) is { } action)
        {
            args.Handled = true;
            _ = Guarded(() => { if (action == "undo") workbench.Undo(); else workbench.Redo(); return Task.CompletedTask; });
        }
    }

    public static string? AcceptedHistoryShortcut(Key key, KeyModifiers modifiers, bool macOS)
    {
        var command = macOS ? KeyModifiers.Meta : KeyModifiers.Control;
        if (modifiers == command && key == Key.Z) return "undo";
        if (modifiers == (command | KeyModifiers.Shift) && key == Key.Z) return "redo";
        if (!macOS && modifiers == command && key == Key.Y) return "redo";
        return null;
    }

    public static int NextRegionIndex(int current, bool reverse, IReadOnlyList<bool> available)
    {
        if (available.Count == 0) return -1;
        int first = current < 0 ? (reverse ? available.Count - 1 : 0)
            : (current + (reverse ? -1 : 1) + available.Count) % available.Count;
        for (int offset = 0; offset < available.Count; offset++)
        {
            int index = (first + (reverse ? -offset : offset) + available.Count) % available.Count;
            if (available[index]) return index;
        }
        return -1;
    }

    private async Task<bool> MayReplaceAsync()
    {
        if (!workbench.IsDirty) return true;
        string answer = await UnsavedDialogAsync();
        if (answer == "Cancel") return false;
        if (answer == "Save")
        {
            await SaveWithPickerAsync();
            return !workbench.IsDirty;
        }
        return true;
    }

    private async Task<string> UnsavedDialogAsync()
    {
        var dialog = new Window { Title = "Unsaved foil changes", Width = 420, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false };
        var result = "Cancel";
        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        Button? cancelButton = null;
        foreach (string choice in new[] { "Save", "Discard", "Cancel" })
        {
            var button = new Button { Content = choice, IsCancel = choice == "Cancel", IsDefault = choice == "Cancel" };
            if (choice == "Cancel") cancelButton = button;
            AutomationProperties.SetName(button, choice + " unsaved changes");
            button.Click += (_, _) => { result = choice; dialog.Close(); };
            buttons.Children.Add(button);
        }
        dialog.Content = new StackPanel { Margin = new Thickness(20), Spacing = 18,
            Children = { new TextBlock { Text = "Save this foil before closing or opening another file?", TextWrapping = Avalonia.Media.TextWrapping.Wrap }, buttons } };
        dialog.Opened += (_, _) => cancelButton?.Focus();
        if (review?.ReducedMotion == true)
            dialog.LayoutUpdated += (_, _) => SuppressTransitions(dialog);
        await dialog.ShowDialog(this);
        return result;
    }

    public static string ReviewFocusTarget(string persona) => persona switch
    {
        "designer" => "viewport",
        "keyboard" => "numeric-or-open",
        "screen-reader" => "stations",
        "dense" => "controls",
        _ => throw new ArgumentException("Unknown review persona")
    };

    private Control ReviewFocusControl(string persona) => ReviewFocusTarget(persona) switch
    {
        "viewport" => viewport,
        "stations" => stationList,
        "controls" => controlList,
        _ => numericInput.IsEnabled ? numericInput : openButton
    };

    private void ApplyReviewMotionPreference()
    {
        if (review?.ReducedMotion == true) SuppressTransitions(this);
    }

    public static void SuppressTransitions(Visual root)
    {
        foreach (var animation in root.GetVisualDescendants().OfType<Animatable>().Prepend(root).OfType<Animatable>())
            if (animation.Transitions is { Count: > 0 }) animation.Transitions = null;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (closeApproved || !workbench.IsDirty) return;
        args.Cancel = true;
        try
        {
            if (!await MayReplaceAsync())
            {
                (numericInput.IsEnabled ? (Control)numericInput : openButton).Focus();
                return;
            }
            closeApproved = true;
            Close();
        }
        catch (ContractError error) { adapterError = $"{error.Code}: Save failed; window remains open.";
            Refresh();
            (numericInput.IsEnabled ? (Control)numericInput : openButton).Focus(); }
    }

    private void Refresh()
    {
        if (refreshing) return;
        refreshing = true;
        try
        {
        stateBanner.Text = adapterError ?? workbench.Status;
        viewportProvenance.Text = workbench.Provenance;
        viewport.Frame = workbench.Frame;
        sectionViewport.Frame = workbench.Frame;
        sectionViewport.Semantics = ViewportSemantics.FromSection(workbench.Frame);
        saveButton.IsEnabled = workbench.Inspection is not null && workbench.DraftInputValid;
        undoButton.IsEnabled = workbench.Inspection is not null && workbench.Draft is null;
        redoButton.IsEnabled = undoButton.IsEnabled;
        previewButton.IsEnabled = workbench.Draft is not null && workbench.DraftInputValid;
        applyButton.IsEnabled = workbench.Draft is not null && workbench.Provenance == "preview";
        cancelButton.IsEnabled = workbench.Draft is not null;
        acceptIdsButton.IsEnabled = workbench.PendingCandidate is not null;
        resumeRecoveryButton.IsEnabled = workbench.HasRecovery && workbench.Draft is null;
        discardRecoveryButton.IsEnabled = workbench.HasRecovery && workbench.Draft is null;
        numericInput.IsEnabled = workbench.Draft is { } numericDraft &&
            workbench.DraftProjection is { } numericProjection && TryDraftField(numericProjection, numericDraft) is not null;
        if (workbench.Inspection is { } inspected)
        {
            viewport.Semantics = ViewportSemantics.FromInspection(inspected,
                workbench.Draft?.Rail, workbench.Draft?.VertexId);
            identityReadout.Text = $"Source SHA-256 {inspected.Authored.Binding.SourceHash}\nSurface {inspected.Authored.Binding.SurfaceHash}\nAccepted {inspected.Authored.Binding.AcceptedId}\nEvaluator {inspected.Authored.Binding.Evaluator}";
            stationList.ItemsSource = inspected.Authored.Assignments.Select(a => NamedItem($"η {a.Eta:G3} · {a.SpanMeters:G4} m · {a.ProfileName}",
                $"Station eta {a.Eta:G3}, profile {a.ProfileName}, identity {a.ProfileIdentity}")).ToArray();
            var priorTarget = controlList.SelectedIndex >= 0 && controlList.SelectedIndex < targets.Count
                ? (targets[controlList.SelectedIndex].Rail, targets[controlList.SelectedIndex].Control.Id) : ("", "");
            targets.Clear();
            foreach (var rail in inspected.Authored.Rails)
                foreach (var control in rail.Controls) targets.Add((rail.Name, control, rail.SourceUnit));
            controlList.ItemsSource = targets.Select(item => NamedItem($"{item.Rail} · {item.Control.Id} · η {item.Control.Eta:G2} · {(item.Control.Editable ? "editable" : "locked")}",
                $"{item.Rail} control vertex {item.Control.Id}; eta {item.Control.Eta:G2}; aft position {item.Control.OrdinateSi:G6} metres; {(item.Control.Editable ? "editable" : "locked " + string.Join(",", item.Control.ApplicableLocks))}")).ToArray();
            int selected = targets.FindIndex(item => item.Rail == priorTarget.Item1 && item.Control.Id == priorTarget.Item2);
            if (selected >= 0) controlList.SelectedIndex = selected;
        }
        else
        {
            viewport.Semantics = [];
            identityReadout.Text = "No accepted source or geometry.";
            stationList.ItemsSource = Array.Empty<ListBoxItem>();
            controlList.ItemsSource = Array.Empty<ListBoxItem>();
            targets.Clear();
        }
        if (workbench.Draft is { } activeDraft && boundDraftId != activeDraft.Id)
        {
            var projection = workbench.DraftProjection;
            var field = projection is null ? null : TryDraftField(projection, activeDraft);
            if (field is null)
            {
                numericInput.Text = "";
                unitLabel.Text = "—";
            }
            else
            {
                numericInput.Text = field.Value.Text;
                unitLabel.Text = field.Value.Unit;
            }
            boundDraftId = activeDraft.Id;
        }
        else if (workbench.Draft is null) boundDraftId = null;
        sourceLabel.Text = workbench.PendingCandidate is not null ? "Pending import: original and explicit-ID candidate — compare before accepting"
            : workbench.PendingOriginal is not null ? "Rejected import: original bytes retained; accepted document unchanged"
            : workbench.HasRecovery ? "Separate recovery draft and unchanged accepted source"
            : "Accepted FoilDSL source bytes";
        sourceText.Text = workbench.PendingCandidate is not null
            ? "ORIGINAL\n" + workbench.OriginalSource + "\nCANDIDATE\n" + workbench.CandidateSource +
              "\nACCEPTED DOCUMENT (UNCHANGED)\n" + workbench.AcceptedSource
            : workbench.PendingOriginal is not null
                ? "UNACCEPTED IMPORT\n" + workbench.OriginalSource + "\nACCEPTED DOCUMENT (UNCHANGED)\n" + workbench.AcceptedSource
                : workbench.HasRecovery
                    ? "RECOVERY DRAFT (NOT ACCEPTED)\n" + workbench.RecoverySource +
                      "\nACCEPTED DOCUMENT (UNCHANGED)\n" + workbench.AcceptedSource
                : workbench.AcceptedSource;
        sampleList.ItemsSource = workbench.Points.Select(point => NamedItem($"η {point.Eta:G2} · x/c {point.NormalizedX:G2} · {(point.Upper ? "upper" : "lower")} · x {point.X:G5} m",
            $"Sample eta {point.Eta:G2}, normalized x {point.NormalizedX:G2}, {(point.Upper ? "upper" : "lower")}; placed x {point.X:G6} metres, y {point.Y:G6} metres, z {point.Z:G6} metres; point enclosure only")).ToArray();
        var section = workbench.CenterSection;
        sectionPosition.Text = workbench.Frame is { } view
            ? $"Physical X/Z section · η {view.InteriorEta:G3}"
            : "Physical X/Z section · awaiting certified slice";
        sectionReadout.Text = section is null ? "Section sample unavailable."
            : $"Upper z/c [{section.Upper.Lower:G7}, {section.Upper.Upper:G7}] · Lower z/c [{section.Lower.Lower:G7}, {section.Lower.Upper:G7}]";
        draftReadout.Text = workbench.Draft is null ? "No draft." :
            workbench.DraftProjection is not { } displayProjection || TryDraftField(displayProjection, workbench.Draft) is null
                ? $"Draft {workbench.Draft.Id} cannot be projected. Inspect retained source bytes and Preview diagnostics; numeric editing is unavailable."
                : $"Draft {workbench.Draft.Id} · {workbench.Draft.Rail} {workbench.Draft.VertexId} · generation {workbench.Draft.Generation}";
        importReadout.Text = workbench.PendingCandidate is not null ? "Original bytes retained; candidate adds explicit control IDs only. Accepted source remains unchanged until acceptance."
            : workbench.PendingOriginal is not null ? "Import refused. Original bytes retained; accepted source remains unchanged."
            : "No import pending.";
        recoveryReadout.Text = workbench.HasRecovery
            ? workbench.Draft is null
                ? "Saved recovery is separate. Inspect its exact bytes in FoilDSL source, then Resume or Discard."
                : "Recovery is resumed as a draft. Preview reports geometry diagnostics; Cancel discards the draft and retains the accepted source."
            : "No recovery draft.";
        var last = workbench.LocalEvents.LastOrDefault();
        eventReadout.Text = last is null ? "No operation recorded." : $"{last.Operation} · {last.Outcome} · {last.DurationMilliseconds:F1} ms · {last.InputBytes?.ToString() ?? "not recorded"} input bytes";
        statusBar.Text = $"{workbench.Provenance} · {workbench.Status} · Analysis Unavailable — no method implemented";
        }
        finally { refreshing = false; }
    }

    private static ListBoxItem NamedItem(string text, string name)
    {
        var item = new ListBoxItem { Content = text };
        AutomationProperties.SetName(item, name);
        return item;
    }

    public static (string Text, string Unit) DraftField(AuthoredProjection projection, SessionDraft draft)
    {
        return TryDraftField(projection, draft) ?? throw new ContractError("DSL-NOT-ASSESSED");
    }

    public static (string Text, string Unit)? TryDraftField(AuthoredProjection projection, SessionDraft draft)
    {
        var rail = projection.Rails.SingleOrDefault(item => item.Name == draft.Rail);
        var control = rail?.Controls.SingleOrDefault(item => item.Id == draft.VertexId);
        if (rail is null || control is null) return null;
        double scale = rail.SourceUnit switch { "mm" => 1000, "cm" => 100, _ => 1 };
        return ((control.OrdinateSi * scale).ToString("G9", CultureInfo.InvariantCulture), rail.SourceUnit);
    }
}
