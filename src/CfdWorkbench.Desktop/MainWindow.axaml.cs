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
    private readonly WorkbenchController workbench = new();
    private readonly NativeReviewOptions? review = NativeReviewOptions.Current;
    private readonly ShellHost shellHost;
    private bool closeApproved;
    private bool closed;

    public MainWindow() : this(null)
    {
    }

    /// <summary>
    /// <paramref name="macOS"/> null reads the running system. A test passes false to build the Windows and Linux shell on a Mac:
    /// ADR-0009 S1 renders the one menu table in the window there, and the table's gestures bind with Ctrl for ⌘.
    /// </summary>
    public MainWindow(PreferenceStore? preferences, bool? macOS = null)
    {
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
        if (!closed) RefreshShellEditMenu();
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
            var location = CloudFolder.Classify(Path.GetDirectoryName(destination) ?? destination);
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
    public async Task SaveToAsync(string destination)
    {
        try
        {
            if (!destination.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
                throw new ContractError("DOC-TYPE");
            var result = await workbench.SaveAsync(destination);
            // Ruling 253 (W2-Q3 c): a save refused because a claim file is in the folder is the "Save is blocked" dialog, not the generic line.
            if (OperatingSystem.IsWindows() && result.Code == "DOC-CONFLICT" && UnfinishedSave.BlocksFolder(destination))
            {
                var blocked = new SaveBlockedDialog();
                await blocked.ShowDialog(this);
                if (blocked.SaveAs && await PickSavePathAsync() is { } other) await SaveToAsync(other);
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
/// "Save is blocked" (Ruling 253 W2-Q3 c; Rulings 146 (2), 160): shown when a save is refused because a claim file (.cfd-writer.claim) is in
/// the folder, which an unfinished save leaves behind. Save As and Cancel only: there is no Clear action, and the dialog never says anything
/// was recovered. The document stays dirty.
/// </summary>
public sealed class SaveBlockedDialog : Window
{
    /// <summary>COPY-441.</summary>
    public const string TitleText = "Save is blocked";
    /// <summary>Ruling 146 (2), the lead sentences.</summary>
    public const string Lead = "An earlier save of this project didn't finish (the app may have closed unexpectedly). Your saved file is unchanged.";
    /// <summary>Ruling 146 (2) steps 1 and 2; step 3 is reworded by Ruling 253 because the claim blocks every save in the folder.</summary>
    public static readonly string[] Steps =
    [
        "Close any other CFD Workbench window using this project.",
        "Check the project shows your last saved work.",
        "Use Save As to keep your current work in a different folder."
    ];
    /// <summary>Ruling 253: said plainly; the files are the ones COPY-447 names.</summary>
    public const string StaysBlocked = "This folder stays blocked until the leftover files from the save that didn't finish are removed: the file .cfd-writer.claim and any file named .cfd-….tmp in this folder.";
    /// <summary>COPY-443.</summary>
    public const string SaveAsLabel = "Save As…";
    /// <summary>COPY-445.</summary>
    public const string CancelLabel = "Cancel";

    /// <summary>True when the user chose Save As…; false for Cancel, Escape or closing the dialog.</summary>
    public bool SaveAs { get; private set; }

    public SaveBlockedDialog()
    {
        Title = TitleText;
        Width = 480; Height = 330; CanResize = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var saveAs = new Button { Content = SaveAsLabel, IsDefault = true };
        var cancel = new Button { Content = CancelLabel, IsCancel = true };
        AutomationProperties.SetName(saveAs, SaveAsLabel);
        AutomationProperties.SetName(cancel, CancelLabel);
        saveAs.Click += (_, _) => { SaveAs = true; Close(); };
        cancel.Click += (_, _) => Close();
        var steps = new StackPanel { Spacing = 4, Margin = new Thickness(12, 0, 0, 0) };
        AutomationProperties.SetName(steps, "Steps");
        for (int i = 0; i < Steps.Length; i++)
            steps.Children.Add(new TextBlock { Text = $"{i + 1}. {Steps[i]}", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Children = { saveAs, cancel } };
        Content = new StackPanel { Margin = new Thickness(20), Spacing = 12, Children =
        {
            new TextBlock { Text = Lead, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            steps,
            new TextBlock { Text = StaysBlocked, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            buttons
        } };
        Opened += (_, _) => saveAs.Focus();
    }
}
