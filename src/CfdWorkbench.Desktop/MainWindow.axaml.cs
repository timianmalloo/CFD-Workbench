using Avalonia;
using Avalonia.Automation;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using System.Globalization;
using System.Diagnostics;
using System.Text.Json;

namespace CfdWorkbench.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly WorkbenchController workbench = new();
    private readonly List<(string Rail, AuthoredControl Control, string Unit)> targets = [];
    private readonly Button exampleButton, openButton, saveButton, undoButton, redoButton;
    private readonly Button previewButton, applyButton, cancelButton, acceptIdsButton, resumeRecoveryButton, discardRecoveryButton;
    private readonly Button editSectionButton, sectionPreviewButton, sectionApplyButton, sectionCancelButton;
    private readonly RadioButton scopeSharedRadio, scopeIndependentRadio, thicknessKeepRadio, thicknessSourceRadio;
    private readonly TextBlock stateBanner, viewportProvenance, sectionReadout, sectionPosition, sourceLabel, identityReadout, unitLabel,
        draftReadout, importReadout, recoveryReadout, eventReadout, statusBar, stationCardText, scopeImpactText;
    private readonly ListBox stationList, controlList, sampleList, sectionVertexList;
    private readonly TextBox numericInput, sourceText, sectionXInput, sectionYInput;
    private readonly TabControl documentTabs;
    private readonly TabItem sectionTab;
    private readonly Viewport viewport, sectionViewport;
    private readonly SectionCanvas stationThumbnail, editableSectionCanvas;
    private readonly List<ProfileVertex> sectionVertices = [];
    private (string Side, string Id)? selectedSectionVertex;
    private string? boundSectionDraftId;
    private long boundSectionGeneration;
    private (string Side, string Id)? boundSectionVertex;
    private bool refreshing;
    private bool closeApproved;
    private bool closed;
    private string? adapterError;
    private string? boundDraftId;
    private string? navigatorKey;
    private DisplayFrame? boundSampleFrame;
    private NativeMetric? nativeMetric;
    private DispatcherTimer? nativeMetricTimeout;
    private long nativeMetricSequence;
    private readonly NumericBindingGuard numericBinding = new();
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
        editSectionButton = Find<Button>("EditSectionButton");
        sectionPreviewButton = Find<Button>("SectionPreviewButton");
        sectionApplyButton = Find<Button>("SectionApplyButton");
        sectionCancelButton = Find<Button>("SectionCancelButton");
        scopeSharedRadio = Find<RadioButton>("ScopeSharedRadio");
        scopeIndependentRadio = Find<RadioButton>("ScopeIndependentRadio");
        thicknessKeepRadio = Find<RadioButton>("ThicknessKeepRadio");
        thicknessSourceRadio = Find<RadioButton>("ThicknessSourceRadio");
        stationCardText = Find<TextBlock>("StationCardText");
        scopeImpactText = Find<TextBlock>("ScopeImpactText");
        sectionVertexList = Find<ListBox>("SectionVertexList");
        sectionXInput = Find<TextBox>("SectionXInput");
        sectionYInput = Find<TextBox>("SectionYInput");
        sectionTab = Find<TabItem>("SectionTab");
        stationThumbnail = Find<SectionCanvas>("StationThumbnail");
        editableSectionCanvas = Find<SectionCanvas>("EditableSectionCanvas");
        workbench.Changed += OnWorkbenchChanged;
        exampleButton.Click += async (_, _) => await Guarded(async () => { if (await MayReplaceAsync()) await workbench.OpenExampleAsync(); });
        openButton.Click += async (_, _) => await Guarded(OpenAsync);
        saveButton.Click += async (_, _) => await Guarded(SaveWithPickerAsync);
        undoButton.Click += async (_, _) => await Guarded(() => { workbench.Undo(); return Task.CompletedTask; });
        redoButton.Click += async (_, _) => await Guarded(() => { workbench.Redo(); return Task.CompletedTask; });
        previewButton.Click += async (_, _) => await Guarded(TimedPreviewAsync);
        applyButton.Click += async (_, _) => await Guarded(() => { workbench.Apply(); return Task.CompletedTask; });
        cancelButton.Click += (_, _) => TimedCancel();
        acceptIdsButton.Click += async (_, _) => await Guarded(() => workbench.AcceptCandidateAsync());
        resumeRecoveryButton.Click += async (_, _) => await Guarded(() => { workbench.ResumeRecovery(); return Task.CompletedTask; });
        discardRecoveryButton.Click += async (_, _) => await Guarded(() => { workbench.DiscardRecovery(); return Task.CompletedTask; });
        controlList.SelectionChanged += OnControlSelection;
        numericInput.TextChanged += OnNumericChanged;
        stationList.SelectionChanged += OnStationSelection;
        editSectionButton.Click += OnEditSectionClick;
        sectionPreviewButton.Click += async (_, _) => await Guarded(TimedPreviewAsync);
        sectionApplyButton.Click += async (_, _) => await Guarded(() => { workbench.Apply(); return Task.CompletedTask; });
        sectionCancelButton.Click += (_, _) => TimedCancel();
        scopeSharedRadio.IsCheckedChanged += OnScopeChanged;
        scopeIndependentRadio.IsCheckedChanged += OnScopeChanged;
        sectionVertexList.SelectionChanged += OnSectionVertexSelection;
        sectionXInput.TextChanged += OnSectionNumericChanged;
        sectionYInput.TextChanged += OnSectionNumericChanged;
        editableSectionCanvas.VertexSelected += OnCanvasVertexSelected;
        editableSectionCanvas.VertexMoved += OnCanvasVertexMoved;
        numericInput.KeyDown += OnNumericKeyDown;
        KeyDown += OnWindowKeyDown;
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            closed = true;
            workbench.Changed -= OnWorkbenchChanged;
            workbench.Dispose();
        };
        Opened += async (_, _) =>
        {
            Console.Error.WriteLine("NATIVE-STARTUP window-opened");
            if (Environment.GetEnvironmentVariable("CFDW_STARTUP_SMOKE") == "1")
            {
                Console.Error.WriteLine("NATIVE-STARTUP smoke-opened");
                Dispatcher.UIThread.Post(Close, DispatcherPriority.Background);
                return;
            }
            NativeMetric? start = review is null || review.State == "example"
                ? BeginNativeMetric("example-ready", Program.ManagedStartTicks == 0
                    ? Stopwatch.GetTimestamp() : Program.ManagedStartTicks) : null;
            await Guarded(() => review is null ? workbench.OpenExampleAsync() : review.ApplyStateAsync(workbench));
            if (closed) return;
            if (start is not null) CaptureNativeMetric(start);
            Refresh();
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
        if (!closed) Refresh();
    }

    private void OnWorkbenchChanged() => Dispatcher.UIThread.Post(() =>
    {
        if (!closed) Refresh();
    });

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
        var field = AcceptedControlField(selected.Control, selected.Unit);
        unitLabel.Text = field.Unit;
        SetNumericText(field.Text);
        refreshing = false;
        if (selected.Control.Editable)
            try { workbench.BeginEdit(selected.Rail, selected.Control.Id); }
            catch (ContractError error) { stateBanner.Text = $"{error.Code}: control is read-only."; }
        Refresh();
    }

    private void OnControlPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        RestartSelectedEdit(sender);
    }

    private void OnControlItemKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.KeyModifiers == KeyModifiers.None && IsReeditKey(args.Key) && RestartSelectedEdit(sender))
            args.Handled = true;
    }

    public static bool IsReeditKey(Key key) => key is Key.Enter or Key.Space;

    private bool RestartSelectedEdit(object? sender)
    {
        if (refreshing || sender is not ListBoxItem item || !ReferenceEquals(controlList.SelectedItem, item) ||
            controlList.SelectedIndex < 0 || controlList.SelectedIndex >= targets.Count) return false;
        var selected = targets[controlList.SelectedIndex];
        if (!CanRestartSelectedEdit(workbench.Draft, selectedItemMatches: true, editable: selected.Control.Editable)) return false;
        try { workbench.BeginEdit(selected.Rail, selected.Control.Id); }
        catch (ContractError error) { adapterError = $"{error.Code}: control is read-only."; }
        Refresh();
        return workbench.Draft is not null;
    }

    public static bool CanRestartSelectedEdit(SessionDraft? draft, bool selectedItemMatches, bool editable) =>
        draft is null && selectedItemMatches && editable;

    public static (string Text, string Unit) AcceptedControlField(AuthoredControl control, string unit)
    {
        double scale = unit switch { "mm" => 1000, "cm" => 100, _ => 1 };
        return ((control.OrdinateSi * scale).ToString("G9", CultureInfo.InvariantCulture), unit);
    }

    private void OnNumericChanged(object? sender, TextChangedEventArgs args)
    {
        if (refreshing || !numericBinding.ShouldProcess(numericInput.Text,
            editingEnabled: numericInput.IsEnabled && workbench.Draft is not null)) return;
        var owned = workbench.Draft!;
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
        var editMetric = BeginNativeMetric("edit");
        try { workbench.UpdateDraft(display / scale); CaptureNativeMetric(editMetric); }
        catch (ContractError error) { workbench.InvalidateDraftInput(); stateBanner.Text = $"{error.Code}: draft update refused."; }
        Refresh();
    }

    private void SetNumericText(string value)
    {
        numericBinding.NoteProgrammatic(value);
        numericInput.Text = value;
    }

    private async void OnNumericKeyDown(object? sender, KeyEventArgs args)
    {
        if (workbench.Draft is null) return;
        if (args.Key == Key.Escape)
        {
            args.Handled = true;
            TimedCancel();
        }
        else if (args.Key == Key.Enter)
        {
            args.Handled = true;
            await Guarded(async () =>
            {
                if (workbench.Provenance == "preview") workbench.Apply();
                else await TimedPreviewAsync();
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
            bool[] available = groups.Select(group => group.Any(control => FocusCandidates(control)
                .Any(candidate => candidate.Focusable && candidate.IsVisible && candidate.IsEnabled))).ToArray();
            bool reverse = args.KeyModifiers.HasFlag(KeyModifiers.Shift);
            for (int attempt = 0; attempt < groups.Length; attempt++)
            {
                int next = NextRegionIndex(current, reverse, available);
                if (next < 0) break;
                foreach (var control in groups[next])
                    foreach (var candidate in FocusCandidates(control))
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

    public static IEnumerable<Control> FocusCandidates(Control region)
    {
        if (region is ListBox list)
            foreach (var item in list.Items.OfType<ListBoxItem>()) yield return item;
        if (region is TabControl tabs)
        {
            if (tabs.SelectedItem is TabItem selected) yield return selected;
            foreach (var item in tabs.Items.OfType<TabItem>())
                if (!ReferenceEquals(item, tabs.SelectedItem)) yield return item;
        }
        yield return region;
    }

    public static void BindNavigatorItems(ListBox list, IReadOnlyList<ListBoxItem> items, bool acceptedChanged)
    {
        if (acceptedChanged) list.ItemsSource = items;
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
            CanResize = false, RequestedThemeVariant = ActualThemeVariant };
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

    private sealed class NativeMetric(string operation, long sequence, long startedTicks)
    {
        public string Operation { get; } = operation;
        public long Sequence { get; } = sequence;
        public long StartedTicks { get; } = startedTicks;
        public string? AcceptedId { get; set; }
        public string? SourceHash { get; set; }
        public string? DraftId { get; set; }
        public long Generation { get; set; }
        public bool Armed { get; set; }
        public bool Queued { get; set; }
        public DisplayFrame? ExpectedFrame { get; set; }
        public string? ExpectedProvenance { get; set; }
        public double? ExpectedEta { get; set; }
    }

    private NativeMetric BeginNativeMetric(string operation, long? startedTicks = null)
    {
        if (nativeMetric is { } previous) FinishNativeMetric(previous, "not_assessed", "superseded");
        var metric = new NativeMetric(operation, ++nativeMetricSequence, startedTicks ?? Stopwatch.GetTimestamp());
        nativeMetric = metric;
        nativeMetricTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(operation == "example-ready" ? 10 : 5) };
        nativeMetricTimeout.Tick += (_, _) => FinishNativeMetric(metric, "not_assessed", "timeout");
        nativeMetricTimeout.Start();
        return metric;
    }

    private void CaptureNativeMetric(NativeMetric metric)
    {
        if (!ReferenceEquals(nativeMetric, metric)) return;
        var binding = workbench.Inspection?.Authored.Binding;
        metric.AcceptedId = binding?.AcceptedId;
        metric.SourceHash = binding?.SourceHash;
        metric.DraftId = workbench.Draft?.Id;
        metric.Generation = workbench.Draft?.Generation ?? 0;
        metric.Armed = true;
    }

    private bool NativeMetricReady(NativeMetric metric)
    {
        var binding = workbench.Inspection?.Authored.Binding;
        if (!metric.Armed || !NativeRenderCorrelation.SameState(metric.AcceptedId, metric.SourceHash,
            metric.DraftId, metric.Generation, binding?.AcceptedId, binding?.SourceHash,
            workbench.Draft?.Id, workbench.Draft?.Generation ?? 0))
            return false;
        if (metric.Queued && !NativeRenderCorrelation.TargetSnapshotMatches(metric.ExpectedFrame,
            metric.ExpectedProvenance, metric.ExpectedEta, workbench.Frame, workbench.Provenance,
            workbench.Frame?.InteriorEta)) return false;
        return metric.Operation switch
        {
            "example-ready" or "cancel" => workbench.Draft is null && workbench.Frame?.Provenance == "accepted",
            "edit" => workbench.Draft is not null && workbench.Provenance == "draft — accepted geometry shown",
            "preview" => workbench.Draft is not null && workbench.Frame?.Provenance == "preview" && workbench.Provenance == "preview",
            _ => false
        };
    }

    private void QueueNativeMetric(NativeMetric metric, long mainBefore, long sectionBefore)
    {
        metric.Queued = true;
        var expectedFrame = workbench.Frame;
        metric.ExpectedFrame = expectedFrame;
        metric.ExpectedProvenance = workbench.Provenance;
        metric.ExpectedEta = expectedFrame?.InteriorEta;
        bool sectionVisible = NativeRenderCorrelation.SectionRequired(sectionViewport, this);
        viewport.InvalidateFrameForMetric();
        if (sectionVisible) sectionViewport.InvalidateFrameForMetric();
        long mainRevision = viewport.FrameRevision;
        long sectionRevision = sectionViewport.FrameRevision;
        var compositor = ElementComposition.GetElementVisual(viewport)?.Compositor;
        if (compositor is null)
        {
            FinishNativeMetric(metric, "not_assessed", "target_compositor_unavailable");
            return;
        }
        compositor.RequestCompositionUpdate(() =>
        {
            if (!ReferenceEquals(nativeMetric, metric) || !NativeMetricReady(metric))
            {
                FinishNativeMetric(metric, "not_assessed", "stale_before_commit");
                return;
            }
            bool mainFresh = NativeRenderCorrelation.Fresh(mainBefore, viewport.RenderSerial,
                mainRevision, viewport.LastRecordedRevision, expectedFrame, viewport.LastRecordedFrame);
            bool sectionFresh = !sectionVisible || NativeRenderCorrelation.Fresh(sectionBefore, sectionViewport.RenderSerial,
                sectionRevision, sectionViewport.LastRecordedRevision, expectedFrame, sectionViewport.LastRecordedFrame);
            if (!mainFresh || !sectionFresh)
            {
                FinishNativeMetric(metric, "not_assessed", "target_scene_not_recorded");
                return;
            }
            var batch = compositor.RequestCompositionBatchCommitAsync();
            _ = batch.Rendered.ContinueWith(completion => Dispatcher.UIThread.Post(() =>
            {
                bool stillCurrent = NativeMetricReady(metric) &&
                    viewport.LastRecordedRevision == mainRevision &&
                    (!sectionVisible || sectionViewport.LastRecordedRevision == sectionRevision);
                FinishNativeMetric(metric, completion.IsCompletedSuccessfully && stillCurrent
                    ? "batch_cycle_complete" : "not_assessed",
                    !completion.IsCompletedSuccessfully ? "batch_failed" : stillCurrent
                        ? "target_batch_completed" : "stale_after_batch",
                    sectionVisible ? "fresh" : "not_recorded_hidden");
            }));
        });
    }

    private void FinishNativeMetric(NativeMetric metric, string outcome, string reason, string section = "not_recorded")
    {
        if (!ReferenceEquals(nativeMetric, metric)) return;
        nativeMetricTimeout?.Stop();
        nativeMetricTimeout = null;
        nativeMetric = null;
        Console.Error.WriteLine("NATIVE-METRIC " + NativeMetricRecord.Serialize(metric.Sequence,
            metric.Operation, outcome, reason,
            Math.Round(Stopwatch.GetElapsedTime(metric.StartedTicks).TotalMilliseconds, 3),
            metric.Generation, section));
    }

    private async Task TimedPreviewAsync()
    {
        var metric = BeginNativeMetric("preview");
        try
        {
            var task = workbench.PreviewAsync();
            CaptureNativeMetric(metric);
            await task;
        }
        catch
        {
            FinishNativeMetric(metric, "not_assessed", "preview_refused");
            throw;
        }
        Refresh();
    }

    private void TimedCancel()
    {
        var metric = BeginNativeMetric("cancel");
        workbench.Cancel();
        CaptureNativeMetric(metric);
        Refresh();
    }

    private void Refresh()
    {
        if (closed) return;
        if (refreshing) return;
        long mainRenderBefore = viewport.RenderSerial;
        long sectionRenderBefore = sectionViewport.RenderSerial;
        refreshing = true;
        try
        {
        stateBanner.Text = adapterError ?? SectionBanner(workbench.Status);
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
        sectionPreviewButton.IsEnabled = previewButton.IsEnabled;
        sectionApplyButton.IsEnabled = applyButton.IsEnabled;
        sectionCancelButton.IsEnabled = cancelButton.IsEnabled;
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
            string nextKey = inspected.Authored.Binding.AcceptedId + ":" + inspected.Authored.Binding.SourceHash;
            if (navigatorKey != nextKey)
            {
                var priorTarget = controlList.SelectedIndex >= 0 && controlList.SelectedIndex < targets.Count
                    ? (targets[controlList.SelectedIndex].Rail, targets[controlList.SelectedIndex].Control.Id) : ("", "");
                BindNavigatorItems(stationList, inspected.Authored.Assignments.Select(a => NamedItem($"η {a.Eta:G3} · {a.SpanMeters:G4} m · {a.ProfileName}",
                    $"Station eta {a.Eta:G3}, profile {a.ProfileName}, identity {a.ProfileIdentity}")).ToArray(), acceptedChanged: true);
                targets.Clear();
                foreach (var rail in inspected.Authored.Rails)
                    foreach (var control in rail.Controls) targets.Add((rail.Name, control, rail.SourceUnit));
                var controlItems = targets.Select(item => NamedItem($"{item.Rail} · {item.Control.Id} · η {item.Control.Eta:G2} · {(item.Control.Editable ? "editable" : "locked")}",
                    $"{item.Rail} control vertex {item.Control.Id}; eta {item.Control.Eta:G2}; aft position {item.Control.OrdinateSi:G6} metres; {(item.Control.Editable ? "editable" : "locked " + string.Join(",", item.Control.ApplicableLocks))}")).ToArray();
                foreach (var item in controlItems)
                {
                    item.AddHandler(InputElement.PointerReleasedEvent, OnControlPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
                    item.AddHandler(InputElement.KeyDownEvent, OnControlItemKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
                }
                BindNavigatorItems(controlList, controlItems, acceptedChanged: true);
                int selected = targets.FindIndex(item => item.Rail == priorTarget.Item1 && item.Control.Id == priorTarget.Item2);
                if (selected >= 0) controlList.SelectedIndex = selected;
                navigatorKey = nextKey;
            }
            if (stationList.SelectedIndex < 0 && stationList.ItemCount > 0) stationList.SelectedIndex = 0;
        }
        else
        {
            viewport.Semantics = [];
            identityReadout.Text = "No accepted source or geometry.";
            if (navigatorKey is not null)
            {
                stationList.ItemsSource = Array.Empty<ListBoxItem>();
                controlList.ItemsSource = Array.Empty<ListBoxItem>();
                targets.Clear();
                navigatorKey = null;
            }
        }
        if (workbench.Draft is { } activeDraft && boundDraftId != activeDraft.Id)
        {
            var projection = workbench.DraftProjection;
            var field = projection is null ? null : TryDraftField(projection, activeDraft);
            if (field is null)
            {
                SetNumericText("");
                unitLabel.Text = "—";
            }
            else
            {
                SetNumericText(field.Value.Text);
                unitLabel.Text = field.Value.Unit;
            }
            boundDraftId = activeDraft.Id;
        }
        else if (workbench.Draft is null)
        {
            boundDraftId = null;
            if (controlList.SelectedIndex >= 0 && controlList.SelectedIndex < targets.Count)
            {
                var selected = targets[controlList.SelectedIndex];
                var field = AcceptedControlField(selected.Control, selected.Unit);
                SetNumericText(field.Text);
                unitLabel.Text = field.Unit;
            }
            else
            {
                SetNumericText("");
                unitLabel.Text = "—";
            }
        }
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
        if (!ReferenceEquals(boundSampleFrame, workbench.Frame))
        {
            sampleList.ItemsSource = workbench.Points.Select(point => NamedItem($"η {point.Eta:G2} · x/c {point.NormalizedX:G2} · {(point.Upper ? "upper" : "lower")} · x {point.X:G5} m",
                $"Sample eta {point.Eta:G2}, normalized x {point.NormalizedX:G2}, {(point.Upper ? "upper" : "lower")}; placed x {point.X:G6} metres, y {point.Y:G6} metres, z {point.Z:G6} metres; point enclosure only")).ToArray();
            boundSampleFrame = workbench.Frame;
        }
        var section = workbench.CenterSection;
        sectionPosition.Text = workbench.Frame is { } view
            ? $"Physical X/Z section · η {view.InteriorEta:G3}"
            : "Physical X/Z section · awaiting certified slice";
        sectionReadout.Text = section is null ? "Section sample unavailable."
            : $"Upper z/c [{section.Upper.Lower:G7}, {section.Upper.Upper:G7}] · Lower z/c [{section.Lower.Lower:G7}, {section.Lower.Upper:G7}]";
        draftReadout.Text = workbench.Draft is null ? "No draft." :
            workbench.Draft.Profile is not null
                ? $"Draft {workbench.Draft.Id} · station {workbench.Draft.Assignment} {workbench.Draft.Rail} {workbench.Draft.VertexId} · generation {workbench.Draft.Generation}"
                : workbench.DraftProjection is not { } displayProjection || TryDraftField(displayProjection, workbench.Draft) is null
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
        BindSectionEditor();
        }
        finally { refreshing = false; }
        if (nativeMetric is { Armed: true, Queued: false } metric && NativeMetricReady(metric))
            QueueNativeMetric(metric, mainRenderBefore, sectionRenderBefore);
    }

    private string SectionBanner(string status)
    {
        if (workbench.Draft is not { Profile: not null, Assignment: >= 0 } owned) return status;
        string marker = $"station {owned.Assignment}";
        return status.Contains(marker, StringComparison.Ordinal) ? status : $"Draft owns {marker}. {status}";
    }

    private int InspectedAssignment()
    {
        int count = workbench.Inspection?.Authored.Assignments.Count ?? 0;
        if (count == 0) return 0;
        int selected = stationList.SelectedIndex;
        return (uint)selected < (uint)count ? selected : 0;
    }

    private void OnStationSelection(object? sender, SelectionChangedEventArgs args)
    {
        if (refreshing) return;
        Refresh();
    }

    private void OnEditSectionClick(object? sender, RoutedEventArgs args)
    {
        documentTabs.SelectedItem = sectionTab;
        Refresh();
    }

    private void OnScopeChanged(object? sender, RoutedEventArgs args)
    {
        if (refreshing || workbench.Draft is not null) return;
        Refresh();
    }

    private void OnSectionVertexSelection(object? sender, SelectionChangedEventArgs args)
    {
        if (refreshing) return;
        if (sectionVertexList.SelectedIndex < 0 || sectionVertexList.SelectedIndex >= sectionVertices.Count) return;
        SelectSectionVertex(sectionVertices[sectionVertexList.SelectedIndex]);
    }

    private void OnCanvasVertexSelected(string side, string id)
    {
        if (refreshing) return;
        int index = sectionVertices.FindIndex(item => item.Side == side && item.Id == id);
        if (index < 0) return;
        if (sectionVertexList.SelectedIndex != index) sectionVertexList.SelectedIndex = index;
        else SelectSectionVertex(sectionVertices[index]);
    }

    private void OnCanvasVertexMoved(string side, string id, double x, double y)
    {
        if (refreshing) return;
        if (workbench.Draft is null)
        {
            int index = sectionVertices.FindIndex(item => item.Side == side && item.Id == id);
            if (index < 0) return;
            SelectSectionVertex(sectionVertices[index]);
            if (workbench.Draft is null) return;
        }
        if (workbench.Draft is not { } owned || owned.Profile is null || owned.Rail != side || owned.VertexId != id)
        {
            if (workbench.Draft is { Profile: not null, Assignment: >= 0 } pinned)
                adapterError = $"Draft owns station {pinned.Assignment} {pinned.Rail} {pinned.VertexId}.";
            Refresh();
            return;
        }
        try { workbench.UpdateSectionDraft(x, y); adapterError = null; }
        catch (ContractError error)
        {
            workbench.InvalidateDraftInput($"Draft owns station {owned.Assignment}. Enter a finite chord X and Y the profile accepts.");
            adapterError = $"{error.Code}: section draft update refused.";
        }
        Refresh();
    }

    private void SelectSectionVertex(ProfileVertex vertex)
    {
        selectedSectionVertex = (vertex.Side, vertex.Id);
        if (workbench.Draft is { Profile: not null, Assignment: >= 0 } owned)
        {
            adapterError = owned.Rail == vertex.Side && owned.VertexId == vertex.Id
                ? null
                : $"Draft owns station {owned.Assignment} {owned.Rail} {owned.VertexId}.";
            Refresh();
            return;
        }
        adapterError = null;
        var scope = scopeIndependentRadio.IsChecked == true ? SectionScope.Independent : SectionScope.Shared;
        try { workbench.BeginSectionEdit(InspectedAssignment(), scope, vertex.Side, vertex.Id); }
        catch (ContractError error) { adapterError = $"{error.Code}: fixed vertex cannot start an edit."; }
        Refresh();
    }

    private void OnSectionNumericChanged(object? sender, TextChangedEventArgs args)
    {
        if (refreshing || workbench.Draft is not { Profile: not null, Assignment: >= 0 } owned) return;
        if (selectedSectionVertex is not { } selected || owned.Rail != selected.Side || owned.VertexId != selected.Id) return;
        if (!sectionXInput.IsEnabled) return;
        if (!double.TryParse(sectionXInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
            !double.TryParse(sectionYInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double y) ||
            !double.IsFinite(x) || !double.IsFinite(y))
        {
            workbench.InvalidateDraftInput($"Draft owns station {owned.Assignment}. Enter finite chord X and Y. Preview and Save are blocked until corrected.");
            adapterError = null;
            Refresh();
            return;
        }
        try { workbench.UpdateSectionDraft(x, y); adapterError = null; }
        catch (ContractError error)
        {
            workbench.InvalidateDraftInput($"Draft owns station {owned.Assignment}. Enter a finite chord X and Y the profile accepts.");
            adapterError = $"{error.Code}: section draft update refused.";
        }
        Refresh();
    }

    private void BindSectionEditor()
    {
        bool scopeOpen = workbench.Draft is null;
        scopeSharedRadio.IsEnabled = scopeOpen;
        scopeIndependentRadio.IsEnabled = scopeOpen;
        thicknessKeepRadio.IsChecked = true;
        thicknessSourceRadio.IsEnabled = false;
        thicknessSourceRadio.IsChecked = false;
        if (workbench.Inspection is not { } inspected)
        {
            stationThumbnail.Profile = null;
            editableSectionCanvas.Profile = null;
            editableSectionCanvas.SelectedVertex = null;
            stationCardText.Text = "No station selected.";
            scopeImpactText.Text = "Open a certified foil to see affected assignments.";
            editSectionButton.IsEnabled = false;
            sectionXInput.IsEnabled = false;
            sectionYInput.IsEnabled = false;
            sectionVertexList.ItemsSource = Array.Empty<ListBoxItem>();
            sectionVertices.Clear();
            return;
        }
        editSectionButton.IsEnabled = true;
        int inspectedIndex = InspectedAssignment();
        var assignment = inspected.Authored.Assignments[inspectedIndex];
        int editIndex = workbench.Draft is { Profile: not null, Assignment: >= 0 } sectionDraft ? sectionDraft.Assignment : inspectedIndex;
        ProfileView? card = TrySection(inspectedIndex);
        ProfileView? editing = editIndex == inspectedIndex ? card : TrySection(editIndex);
        stationThumbnail.Profile = card;
        editableSectionCanvas.Profile = editing;
        stationCardText.Text = card is null
            ? $"{assignment.ProfileName}\nη {assignment.Eta:G3} · {assignment.SpanMeters:G4} m\nSection view unavailable."
            : $"{card.Name}\nη {assignment.Eta:G3} · {assignment.SpanMeters:G4} m\n{ThicknessReadout(inspected.Authored)}";
        BindScopeImpact(editIndex);
        BindSectionVertices(editing);
        var selected = selectedSectionVertex is { } key
            ? sectionVertices.FirstOrDefault(item => item.Side == key.Side && item.Id == key.Id)
            : null;
        editableSectionCanvas.SelectedVertex = selected is null ? null : (selected.Side, selected.Id);
        bool ownedVertex = workbench.Draft is { Profile: not null } owned && selected is not null &&
            owned.Rail == selected.Side && owned.VertexId == selected.Id && !selected.Fixed;
        sectionXInput.IsEnabled = ownedVertex;
        sectionYInput.IsEnabled = ownedVertex;
        PushSectionNumeric(selected);
    }

    private ProfileView? TrySection(int assignmentIndex)
    {
        try { return workbench.SectionView(assignmentIndex); }
        catch (ContractError) { return null; }
    }

    private void BindScopeImpact(int assignmentIndex)
    {
        var scope = scopeIndependentRadio.IsChecked == true ? SectionScope.Independent : SectionScope.Shared;
        try { scopeImpactText.Text = FormatScope(workbench.DescribeScope(assignmentIndex, scope)); }
        catch (ContractError error) { scopeImpactText.Text = $"{error.Code}: scope is unavailable."; }
    }

    private static string FormatScope(ScopeImpact impact)
    {
        string assignments = impact.AffectedAssignments.Count == 0 ? "none" : string.Join(", ", impact.AffectedAssignments);
        string intervals = impact.Intervals.Count == 0 ? "none" : string.Join("; ", impact.Intervals.Select(interval =>
            $"η {interval.EtaStart:G3}–{interval.EtaEnd:G3} ({interval.RootDistanceStartMeters:G4}–{interval.RootDistanceEndMeters:G4} m)"));
        string choice = impact.Scope == SectionScope.Independent ? "Make independent" : "Edit shared";
        return $"{choice} · profile {impact.Profile}\nAffected assignments: {assignments}\nBlend intervals: {intervals}";
    }

    // assume: a constant thickness channel evaluates to that ordinate at every station.
    // A non-constant channel has no public pointwise query, so the card does not invent a station value.
    private static string ThicknessReadout(AuthoredProjection authored)
    {
        var rail = authored.Rails.FirstOrDefault(item => item.Name == "thickness");
        if (rail is null || rail.Controls.Count == 0) return "t/c unavailable";
        double value = rail.Controls[0].OrdinateSi;
        if (rail.Controls.Any(item => item.OrdinateSi != value)) return "t/c varies along the thickness channel";
        return "t/c " + value.ToString("G6", CultureInfo.InvariantCulture);
    }

    private void BindSectionVertices(ProfileView? view)
    {
        var prior = selectedSectionVertex;
        sectionVertices.Clear();
        if (view is null)
        {
            sectionVertexList.ItemsSource = Array.Empty<ListBoxItem>();
            return;
        }
        sectionVertices.AddRange(view.Upper);
        sectionVertices.AddRange(view.Lower);
        var items = sectionVertices.Select(vertex =>
        {
            string state = vertex.Fixed ? "fixed" : "editable";
            return NamedItem($"{vertex.Side} {vertex.Id} x {vertex.X.ToString("G6", CultureInfo.InvariantCulture)} y {vertex.Y.ToString("G6", CultureInfo.InvariantCulture)} {state}",
                $"{vertex.Side} control {vertex.Id}, x {vertex.X.ToString("G6", CultureInfo.InvariantCulture)}, y {vertex.Y.ToString("G6", CultureInfo.InvariantCulture)}, {state}");
        }).ToArray();
        sectionVertexList.ItemsSource = items;
        int restore = prior is { } key ? sectionVertices.FindIndex(item => item.Side == key.Side && item.Id == key.Id) : -1;
        sectionVertexList.SelectedIndex = restore;
    }

    private void PushSectionNumeric(ProfileVertex? vertex)
    {
        if (vertex is null)
        {
            if (sectionXInput.Text?.Length > 0) sectionXInput.Text = "";
            if (sectionYInput.Text?.Length > 0) sectionYInput.Text = "";
            boundSectionDraftId = null;
            boundSectionGeneration = 0;
            boundSectionVertex = null;
            return;
        }
        var key = (vertex.Side, vertex.Id);
        bool sameEdit = workbench.Draft?.Id == boundSectionDraftId &&
            (workbench.Draft?.Generation ?? 0) == boundSectionGeneration &&
            boundSectionVertex is { } current && current.Side == key.Side && current.Id == key.Id;
        if (sameEdit) return;
        sectionXInput.Text = vertex.X.ToString("G17", CultureInfo.InvariantCulture);
        sectionYInput.Text = vertex.Y.ToString("G17", CultureInfo.InvariantCulture);
        boundSectionDraftId = workbench.Draft?.Id;
        boundSectionGeneration = workbench.Draft?.Generation ?? 0;
        boundSectionVertex = key;
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

public static class NativeRenderCorrelation
{
    public static bool SectionRequired(Visual section, TopLevel target) =>
        SectionEligible(ReferenceEquals(TopLevel.GetTopLevel(section), target), section.IsEffectivelyVisible);

    public static bool SectionEligible(bool attachedToTarget, bool effectivelyVisible) =>
        attachedToTarget && effectivelyVisible;

    public static bool SameState(string? acceptedId, string? sourceHash, string? draftId, long generation,
        string? actualAcceptedId, string? actualSourceHash, string? actualDraftId, long actualGeneration) =>
        acceptedId == actualAcceptedId && sourceHash == actualSourceHash && draftId == actualDraftId &&
        generation == actualGeneration;

    public static bool Fresh(long baselineSerial, long recordedSerial, long expectedRevision, long recordedRevision,
        DisplayFrame? expectedFrame, DisplayFrame? recordedFrame) =>
        recordedSerial > baselineSerial && recordedRevision == expectedRevision &&
        ReferenceEquals(expectedFrame, recordedFrame);

    public static bool TargetSnapshotMatches(DisplayFrame? expectedFrame, string? expectedProvenance,
        double? expectedEta, DisplayFrame? actualFrame, string? actualProvenance, double? actualEta) =>
        ReferenceEquals(expectedFrame, actualFrame) && expectedProvenance == actualProvenance &&
        expectedEta == actualEta;
}

public sealed record NativeMetricRecord(long Sequence, string Operation, string Outcome, string Reason,
    double ElapsedMilliseconds, long DraftGeneration, string Section, string Endpoint)
{
    public static string Serialize(long sequence, string operation, string outcome, string reason,
        double elapsedMilliseconds, long draftGeneration, string section) =>
        JsonSerializer.Serialize(new NativeMetricRecord(sequence, operation, outcome, reason,
            elapsedMilliseconds, draftGeneration, section, "fresh_target_batch_cycle_not_presentation"));
}

/// <summary>Suppresses a delayed TextChanged raised by a programmatic binding, while retaining subsequent user edits.</summary>
public sealed class NumericBindingGuard
{
    private bool bound;
    private string? boundText;
    private bool hasProcessedUserText;
    private string? lastProcessedUserText;

    public void NoteProgrammatic(string? text)
    {
        boundText = text;
        bound = true;
        hasProcessedUserText = false;
        lastProcessedUserText = null;
    }

    public bool ShouldProcess(string? currentText, bool editingEnabled)
    {
        if (!editingEnabled || bound && currentText == boundText ||
            hasProcessedUserText && currentText == lastProcessedUserText) return false;
        bound = false;
        boundText = null;
        hasProcessedUserText = true;
        lastProcessedUserText = currentText;
        return true;
    }
}
