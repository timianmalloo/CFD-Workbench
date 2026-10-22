using CfdWorkbench.Analysis;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop;

/// <summary>The application window. Its only content is the shell (<see cref="ShellHost"/>); there is no pre-shell path.</summary>
public sealed partial class MainWindow : Window
{
    private readonly WorkbenchController workbench;
    private readonly NativeReviewOptions? review = NativeReviewOptions.Current;
    private readonly ShellHost shellHost;
    private bool closeApproved;
    private bool closed;
    private readonly string baseTitle;
    private readonly Border readOnlyNotice;

    /// <summary>
    /// DS-COPY-2 (design windows-store-recovery 2.7, U3): the persistent notice of a project opened read-only from OneDrive, its own element and
    /// not the status strip's message slot, so no other report can replace or hide it. A polite live region whose text never changes.
    /// </summary>
    private static Border BuildReadOnlyNotice()
    {
        var text = new TextBlock { Text = Labels.ReadOnlyNotice, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var notice = new Border { Name = "ReadOnlyNotice", IsVisible = false, Padding = new Thickness(12, 6), Child = text };
        notice.Bind(Border.BackgroundProperty, notice.GetResourceObservable("SurfaceSoftBrush"));
        AutomationProperties.SetLiveSetting(notice, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(notice, "Read-only project notice");
        return notice;
    }

    /// <summary>
    /// Puts <paramref name="notice"/> in a new row of the shell directly above the status strip (the strip moves down one row). The notice's row
    /// is always the strip's row minus one: it follows the strip, so a menu bar shown later cannot leave it on another child's row.
    /// </summary>
    private void PlaceAboveStatusStrip(Control notice)
    {
        int row = Grid.GetRow(shellHost.StatusStrip);
        shellHost.RowDefinitions.Insert(row, new RowDefinition(GridLength.Auto));
        shellHost.StatusStrip.PropertyChanged += (_, e) =>
        {
            if (e.Property == Grid.RowProperty) Grid.SetRow(notice, Grid.GetRow(shellHost.StatusStrip) - 1);
        };
        Grid.SetRow(shellHost.StatusStrip, row + 1);
        shellHost.Children.Add(notice);
    }

    /// <summary>The title suffix (DS-COPY-1) and the notice row follow the document's origin; both go when a Save As makes it local.</summary>
    private void RefreshReadOnlyChrome()
    {
        bool readOnly = workbench.Origin == DocumentOrigin.OneDriveReadOnly;
        string title = readOnly ? baseTitle + " " + Labels.ReadOnlyTitleSuffix : baseTitle;
        if (Title != title) Title = title;
        readOnlyNotice.IsVisible = readOnly;
    }

    public MainWindow() : this(null)
    {
    }

    /// <summary>
    /// <paramref name="macOS"/> null reads the running system. A test passes false to build the Windows and Linux shell on a Mac:
    /// ADR-0009 S1 renders the one menu table in the window there, and the table's gestures bind with Ctrl for ⌘.
    /// </summary>
    public MainWindow(PreferenceStore? preferences, bool? macOS = null, WorkbenchController? controller = null)
    {
        workbench = controller ?? new WorkbenchController();
        bool mac = macOS ?? OperatingSystem.IsMacOS();
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
        baseTitle = Title ?? "";
        shellHost = new ShellHost(workbench, preferences);
        Content = shellHost;
        ShellHost.BindF6(this, shellHost);
        shellHost.PaletteCommand += id => _ = RunShellActionAsync(id);
        var menu = NativeMenuBuilder.BuildMenu(this,
            onAction: id => _ = RunShellActionAsync(id),
            onOpenRecent: path => _ = shellHost.OpenFileAsync(path),
            onClearRecent: () => _ = shellHost.ClearRecentAsync(),
            onSelectPane: shellHost.ShowPane,
            canExecute: CanExecuteShellEdit,
            macOS: mac);
        if (!mac) NativeMenuBuilder.ShowInWindow(this, shellHost, menu);
        readOnlyNotice = BuildReadOnlyNotice();
        PlaceAboveStatusStrip(readOnlyNotice);
        AddHandler(InputElement.GotFocusEvent, (_, _) => RefreshShellEditMenu(), RoutingStrategies.Bubble);
        RefreshShellEditMenu();
        shellHost.RecentLoaded += entries => NativeMenuBuilder.RefreshRecentMenu(this, entries,
            path => _ = shellHost.OpenFileAsync(path), () => _ = shellHost.ClearRecentAsync());
        _ = shellHost.LoadRecentAsync();
        workbench.Changed += OnWorkbenchChanged;
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
            }
            else if (review is not null)
            {
                await Guarded(() => review.ApplyStateAsync(workbench));
                shellHost.RefreshPanes();
                if (review.State == "invalid-input")
                    shellHost.Properties.FindControl<TextBox>("SpanInput")!.SetCurrentValue(TextBox.TextProperty, "-");
                ApplyReviewMotionPreference();
                Dispatcher.UIThread.Post(() =>
                {
                    if (workbench.Inspection is null) shellHost.ModelView.StartCardView.StartNewButton.Focus();
                    else shellHost.ModelView.PlanCanvas.Focus();
                }, DispatcherPriority.Input);
            }
            else shellHost.FocusStartWhenReady();
        };
        if (review?.ReducedMotion == true) LayoutUpdated += (_, _) => ApplyReviewMotionPreference();
    }

    private async Task RunShellActionAsync(string id)
    {
        switch (id)
        {
            case "file.new": await shellHost.OpenNewFoilAsync(); break;
            case "file.new-example": await shellHost.OpenExampleAsync(); break;
            case "file.open": await shellHost.OpenFileInteractiveAsync(); break;
            case "file.save": await Guarded(SaveWithPickerAsync); break;
            case "file.save-as": await Guarded(SaveWithPickerAsync); break;
            case "file.close": Close(); break;
            case "view.toggle-left": shellHost.ToggleLeftSidebar(); break;
            case "view.toggle-bottom": shellHost.ToggleBottomPanel(); break;
            case "view.analysis": workbench.ToggleAnalysis(); break;
            case "view.palette": shellHost.OpenPalette(); break;
            case "edit.undo": shellHost.RouteEditVerb("undo", FocusManager?.GetFocusedElement()); break;
            case "edit.redo": shellHost.RouteEditVerb("redo", FocusManager?.GetFocusedElement()); break;
            case "window.minimize": WindowState = WindowState.Minimized; break;
            case "window.zoom": WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; break;
            // The palette reaches the M1.2b2 view rows (layouts, display, cameras, pan, Fit Selection) through the shell.
            case not ("view.zoom-in" or "view.zoom-out" or "view.fit") when ViewCommands.Handles(id): await shellHost.RunCommand(id); break;
        }
    }

    /// <summary>Runs a window action. A refusal keeps the accepted source and is written to stderr: the shell has no banner for it.</summary>
    private async Task Guarded(Func<Task> action)
    {
        string? refusal = null;
        try { await action(); }
        catch (ContractError error) { refusal = Labels.Refusal(error.Code); }
        catch (OperationCanceledException) { refusal = "Operation cancelled. Accepted source retained."; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { refusal = Labels.Refusal("DOC-IO"); }
        catch (Exception error) when (review is not null && (error is ArgumentException or InvalidOperationException))
        { refusal = $"REVIEW-REFUSED: {error.Message}"; }
        if (refusal is not null) Console.Error.WriteLine("WINDOW-ACTION-REFUSED " + refusal);
    }

    private void OnWorkbenchChanged() => Dispatcher.UIThread.Post(() =>
    {
        if (closed) return;
        RefreshShellEditMenu();
        RefreshReadOnlyChrome();
    });

    private void RefreshShellEditMenu() => NativeMenuBuilder.RefreshEditMenu(this, CanExecuteShellEdit);

    private bool CanExecuteShellEdit(string id)
    {
        var focused = FocusManager?.GetFocusedElement();
        return focused is TextBox text
            ? id == "edit.undo" ? text.CanUndo : text.CanRedo
            : id == "edit.undo" ? workbench.CanUndo : workbench.CanRedo;
    }

    private async Task SaveWithPickerAsync()
    {
        string? destination = workbench.NativePath;
        if (destination is not null && OperatingSystem.IsWindows())
        {
            // A project opened from a OneDrive folder is saved in place: the same refusal as after a pick (Security veto, repair cycle 2).
            // A read-only OneDrive document is refused here before any store call (I-13), whatever the folder check says now.
            var location = workbench.Origin == DocumentOrigin.OneDriveReadOnly ? FolderLocation.Cloud
                : CloudFolder.Classify(Path.GetDirectoryName(destination) ?? destination);
            if (location != FolderLocation.Local)
            {
                var refusal = new FolderRefusalDialog(location);
                await refusal.ShowDialog(this);
                if (!refusal.ChooseAnotherFolder) return;
                destination = null;
            }
        }
        destination ??= await PickSavePathAsync();
        if (destination is null) return;
        await SaveToAsync(destination);
    }

    /// <summary>
    /// The native save picker. On Windows it opens in <c>%USERPROFILE%\CFD Workbench</c>, created just before it opens and only when missing
    /// (Rulings 146 (3), 160 (3)), and a folder synced by OneDrive, or one the check cannot read, is refused after the pick and before any
    /// write (Rulings 135, 160 (1) and (5)); "Choose another folder…" reopens the picker, Cancel ends the save. The check is
    /// <see cref="CloudFolder.Classify"/> in Persistence.
    /// </summary>
    private async Task<string?> PickSavePathAsync()
    {
        while (true)
        {
            var options = new FilePickerSaveOptions { Title = "Save native CFD Workbench project", SuggestedFileName = "foil.cfdw.json" };
            if (OperatingSystem.IsWindows() && EnsureDefaultSaveFolder() is { } start)
                options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(new Uri(start));
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file is null) return null;
            string path;
            using (file) path = file.Path.LocalPath;
            if (!OperatingSystem.IsWindows()) return path;
            var location = CloudFolder.Classify(Path.GetDirectoryName(path) ?? path);
            if (location == FolderLocation.Local) return path;
            var dialog = new FolderRefusalDialog(location);
            await dialog.ShowDialog(this);
            if (!dialog.ChooseAnotherFolder) return null;
        }
    }

    /// <summary>The Windows default save folder, named "CFD Workbench" (Ruling 146 (3)). Null when it cannot be created: the picker then uses its own default.</summary>
    private static string? EnsureDefaultSaveFolder()
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CFD Workbench");
            Directory.CreateDirectory(folder); // no-op when the folder exists
            return folder;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return null; }
    }

    /// <summary>
    /// Saves to <paramref name="destination"/>. A refusal or I/O failure is shown in the status strip and then rethrown, so
    /// <see cref="Guarded"/> still writes it to stderr: the strip alone left a user who saw nothing happen (W-1, Windows, where
    /// the store refuses a drive path with DOC-UNSUPPORTED-PERSISTENCE).
    /// </summary>
    public Task SaveToAsync(string destination) => SaveToAsync(destination, offerBlockedDialog: true);

    /// <param name="offerBlockedDialog">False for the one automatic retry after Clear (design windows-store-recovery A6, U4): a save blocked again
    /// reports through the ordinary save status and does not reopen the dialog by itself.</param>
    private async Task SaveToAsync(string destination, bool offerBlockedDialog)
    {
        try
        {
            if (!destination.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
                throw new ContractError("DOC-TYPE");
            var result = await workbench.SaveAsync(destination);
            // Ruling 253 (W2-Q3 c): a save refused because a claim file is in the folder is the "Save is blocked" dialog, not the generic line.
            if (offerBlockedDialog && OperatingSystem.IsWindows() && result.Code == "DOC-CONFLICT" && UnfinishedSave.BlocksFolder(destination))
            {
                shellHost.Report(new StatusReport("")); // the dialog replaces the generic COPY-414 line the controller set (Ruling 253)
                var blocked = new SaveBlockedDialog(() => workbench.ClearUnfinishedSaveAsync(destination));
                await blocked.ShowDialog(this);
                if (blocked.SaveAs && await PickSavePathAsync() is { } other) await SaveToAsync(other, offerBlockedDialog: true);
                else if (blocked.Cleared)
                {
                    shellHost.Report(new StatusReport("Saving…")); // COPY-452; no sentence says anything was cleared or recovered (Rulings 146, 160)
                    await SaveToAsync(destination, offerBlockedDialog: false);
                }
            }
        }
        catch (Exception error) when (error is ContractError or IOException or UnauthorizedAccessException)
        {
            shellHost.Report(new StatusReport(SaveFailureText(error), (error as ContractError)?.Code == "DOC-UNSUPPORTED-PERSISTENCE" ? ReportKind.Warning : ReportKind.Error));
            throw;
        }
    }

    /// <summary>
    /// The strip sentence for a save that threw: the Ruling 134 wording (COPY-31, and the Windows sentence), the one place
    /// a thrown save is worded. The controller words a returned refusal through the same <see cref="Labels.SaveRefusal"/>.
    /// </summary>
    public static string SaveFailureText(Exception error) => Labels.SaveRefusal((error as ContractError)?.Code ?? "DOC-IO");

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
            if (!await MayReplaceAsync()) return;
            closeApproved = true;
            Close();
        }
        catch (ContractError error) { Console.Error.WriteLine("WINDOW-ACTION-REFUSED " + Labels.SaveRefusal(error.Code)); }
    }
}

/// <summary>
/// The refusal after a save-folder pick (Ruling 160 (1) and (5), mockup S2-A and the S5 check-failed dialog): a modal dialog with the
/// primary "Choose another folder…" and Cancel. Cancel is the Escape button, the primary is the Enter button. Nothing has been written.
/// </summary>
public sealed class FolderRefusalDialog : Window
{
    /// <summary>COPY-440, the S2 dialog title.</summary>
    public const string CloudTitle = "Can't save to OneDrive";
    /// <summary>Ruling 146 (1), the S2 dialog body, verbatim.</summary>
    public const string CloudBody = "This folder is synced by OneDrive. CFD Workbench saves only to local folders, so a sync can't damage a save. Choose a folder outside OneDrive.";
    /// <summary>COPY-457, the check-failed dialog title.</summary>
    public const string CheckFailedTitle = "Can't check this folder";
    /// <summary>COPY-442, the primary button.</summary>
    public const string ChooseAnother = "Choose another folder…";
    /// <summary>COPY-445, the Cancel button.</summary>
    public const string CancelLabel = "Cancel";
    /// <summary>COPY-454, the check-failed dialog body; <paramref name="code"/> is the stable code of the failed check.</summary>
    public static string CheckFailedBody(string code) =>
        $"Couldn't check whether this folder is synced by OneDrive ({code}). Nothing was saved. Choose another folder.";

    /// <summary>True when the user chose "Choose another folder…"; false for Cancel, Escape or closing the dialog.</summary>
    public bool ChooseAnotherFolder { get; private set; }

    public FolderRefusalDialog(FolderLocation location, string code = "DOC-IO")
    {
        bool cloud = location == FolderLocation.Cloud;
        Title = cloud ? CloudTitle : CheckFailedTitle;
        Width = 440; Height = 210; CanResize = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var choose = new Button { Content = ChooseAnother, IsDefault = true };
        var cancel = new Button { Content = CancelLabel, IsCancel = true };
        AutomationProperties.SetName(choose, ChooseAnother);
        AutomationProperties.SetName(cancel, CancelLabel);
        choose.Click += (_, _) => { ChooseAnotherFolder = true; Close(); };
        cancel.Click += (_, _) => Close();
        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Children = { choose, cancel } };
        Content = new StackPanel { Margin = new Thickness(20), Spacing = 18, Children =
        {
            new TextBlock { Text = cloud ? CloudBody : CheckFailedBody(code), TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            buttons
        } };
        Opened += (_, _) => choose.Focus();
    }
}

/// <summary>
/// "Save is blocked" (Ruling 253 W2-Q3 c; Rulings 146 (2), 160, 259; docs/design/windows-store-recovery.md section 4): shown when a save is refused
/// because a claim file (.cfd-writer.claim) is in the folder, which an unfinished save leaves behind. Save As…, Clear unfinished save… and Cancel.
/// Clear swaps the content in place: a confirm (A2, first focus and Enter on Cancel), a busy state with focus on the dialog heading (A3, U1), then
/// either the dialog closes (<see cref="Cleared"/>) or an in-use / failed message with Try again and Cancel (A4, A5, focus on Cancel). Cancel and
/// Escape from those states return to the first state with focus on Clear unfinished save… (U2). The dialog never says anything was recovered.
/// The document stays dirty.
/// </summary>
public sealed class SaveBlockedDialog : Window
{
    /// <summary>COPY-441.</summary>
    public const string TitleText = "Save is blocked";
    /// <summary>Ruling 146 (2), the lead sentences.</summary>
    public const string Lead = "An earlier save of this project didn't finish (the app may have closed unexpectedly). Your saved file is unchanged.";
    /// <summary>Ruling 146 (2) steps 1 and 2; step 3 per Ruling 259 (Save As, or Clear unfinished save then Save).</summary>
    public static readonly string[] Steps =
    [
        "Close any other CFD Workbench window using this project.",
        "Check the project shows your last saved work.",
        "Use Save As to keep your current work in a different folder, or choose Clear unfinished save, then Save."
    ];
    /// <summary>DS-COPY-5 (proposed): replaces the W2 stays-blocked sentence.</summary>
    public const string StaysBlocked = "This folder stays blocked until the unfinished save is cleared.";
    /// <summary>COPY-443.</summary>
    public const string SaveAsLabel = "Save As…";
    /// <summary>COPY-444.</summary>
    public const string ClearLabel = "Clear unfinished save…";
    /// <summary>COPY-445.</summary>
    public const string CancelLabel = "Cancel";
    /// <summary>COPY-446.</summary>
    public const string ConfirmTitle = "Clear the unfinished save?";
    /// <summary>COPY-447 (the refined W2 text; shown for the operator to confirm).</summary>
    public const string ConfirmBody = "This removes the files the unfinished save left in this folder (.cfd-writer.claim and any .cfd-….tmp). Your saved file and your current work are not touched. It works only if no other CFD Workbench window is using this project.";
    /// <summary>COPY-448.</summary>
    public const string ConfirmLabel = "Clear unfinished save";
    /// <summary>COPY-449.</summary>
    public const string InUseText = "Can't clear it yet: another CFD Workbench window is still using this project. Close that window, then try again. Your saved file and your current work are unchanged.";
    /// <summary>COPY-450.</summary>
    public const string TryAgainLabel = "Try again";
    /// <summary>COPY-455.</summary>
    public static string FailedText(string code) => $"Couldn't clear the unfinished save ({code}). Your saved file and your current work are unchanged.";

    private readonly Func<Task<ClearResult>>? clear;
    private bool busy;
    private Control? pendingFocus;

    /// <summary>True when the user chose Save As…; false for Cancel, Escape or closing the dialog.</summary>
    public bool SaveAs { get; private set; }
    /// <summary>True when Clear removed the leftovers: the caller retries the blocked save once.</summary>
    public bool Cleared { get; private set; }

    /// <param name="clear">Runs Clear for the blocked folder; null offers no Clear button.</param>
    public SaveBlockedDialog(Func<Task<ClearResult>>? clear = null)
    {
        this.clear = clear;
        Width = 480; Height = 360; CanResize = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowBlocked(focusClear: false);
        Opened += (_, _) => pendingFocus?.Focus();
        Closing += (_, e) => { if (busy) e.Cancel = true; };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (busy && e.Key == Key.Escape) { e.Handled = true; return; } // A3: Escape is ignored while clearing
        base.OnKeyDown(e);
    }

    private static Button MakeButton(string label, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button { Content = label, IsDefault = isDefault, IsCancel = isCancel };
        AutomationProperties.SetName(button, label);
        return button;
    }

    private static StackPanel Row(params Button[] buttons)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        foreach (var button in buttons) row.Children.Add(button);
        return row;
    }

    private static TextBlock Wrapped(string text) => new() { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    /// <summary>Swaps the content; focus goes to <paramref name="focus"/> now when the dialog is open, else when it opens.</summary>
    private void Present(string title, Control content, Control focus)
    {
        Title = title; Content = content; pendingFocus = focus;
        if (!IsVisible) return;
        UpdateLayout();
        focus.Focus();
    }

    private void ShowBlocked(bool focusClear)
    {
        var saveAs = MakeButton(SaveAsLabel, isDefault: true);
        var cancel = MakeButton(CancelLabel, isCancel: true);
        saveAs.Click += (_, _) => { SaveAs = true; Close(); };
        cancel.Click += (_, _) => Close();
        var buttons = new List<Button> { saveAs };
        Button? clearButton = null;
        if (clear is not null)
        {
            clearButton = MakeButton(ClearLabel);
            clearButton.Click += (_, _) => ShowConfirm();
            buttons.Add(clearButton);
        }
        buttons.Add(cancel);
        var steps = new StackPanel { Spacing = 4, Margin = new Thickness(12, 0, 0, 0) };
        AutomationProperties.SetName(steps, "Steps");
        for (int i = 0; i < Steps.Length; i++) steps.Children.Add(Wrapped($"{i + 1}. {Steps[i]}"));
        Present(TitleText, new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { Wrapped(Lead), steps, Wrapped(StaysBlocked), Row(buttons.ToArray()) } },
            focusClear && clearButton is not null ? clearButton : saveAs);
    }

    /// <summary>A heading that can hold focus while the dialog is busy (U1), the message, and the buttons.</summary>
    private void ShowStep(string message, bool isBusy, params Button[] buttons)
    {
        var heading = new TextBlock { Text = ConfirmTitle, Focusable = true, FontWeight = Avalonia.Media.FontWeight.SemiBold, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        AutomationProperties.SetName(heading, ConfirmTitle);
        var text = Wrapped(message);
        if (message != ConfirmBody) AutomationProperties.SetLiveSetting(text, AutomationLiveSetting.Assertive); // an alert, not a silent swap
        Control focus = isBusy ? heading : buttons.Last(button => button.IsCancel);
        busy = isBusy;
        Present(ConfirmTitle, new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { heading, text, Row(buttons) } }, focus);
    }

    private void ShowConfirm()
    {
        var confirm = MakeButton(ConfirmLabel);
        var cancel = MakeButton(CancelLabel, isDefault: true, isCancel: true);
        confirm.Click += (_, _) => _ = RunClearAsync();
        cancel.Click += (_, _) => ShowBlocked(focusClear: true);
        ShowStep(ConfirmBody, isBusy: false, confirm, cancel);
    }

    private void ShowRefusal(string message)
    {
        var again = MakeButton(TryAgainLabel);
        var cancel = MakeButton(CancelLabel, isDefault: true, isCancel: true);
        again.Click += (_, _) => _ = RunClearAsync();
        cancel.Click += (_, _) => ShowBlocked(focusClear: true);
        ShowStep(message, isBusy: false, again, cancel);
    }

    private async Task RunClearAsync()
    {
        var confirm = MakeButton(ConfirmLabel); var cancel = MakeButton(CancelLabel, isCancel: true);
        confirm.IsEnabled = false; cancel.IsEnabled = false;
        ShowStep(ConfirmBody, isBusy: true, confirm, cancel); // A3: no text, both buttons disabled, focus on the heading
        ClearResult result;
        try { result = await clear!(); }
        catch (Exception error) // any exception ends in A5, never a dialog stuck busy (U1); a store that cannot say why is DOC-IO
        { result = new ClearResult(ClearOutcome.Failed, (error as ContractError)?.Code ?? "DOC-IO", 0); }
        busy = false;
        switch (result.Outcome)
        {
            case ClearOutcome.Cleared: Cleared = true; Close(); break;
            case ClearOutcome.InUse: ShowRefusal(InUseText); break;
            default: ShowRefusal(FailedText(result.Code)); break;
        }
    }
}
