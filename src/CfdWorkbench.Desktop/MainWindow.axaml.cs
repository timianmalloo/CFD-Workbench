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
        catch (ContractError error) { refusal = $"{error.Code}: Action refused; accepted source retained."; }
        catch (OperationCanceledException) { refusal = "Operation cancelled. Accepted source retained."; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { refusal = "DOC-IO: File operation failed. Accepted source retained."; }
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
        if (destination is null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            { Title = "Save native CFD Workbench project", SuggestedFileName = "foil.cfdw.json" });
            if (file is null) return;
            using (file) destination = file.Path.LocalPath;
        }
        await SaveToAsync(destination);
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
            await workbench.SaveAsync(destination);
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
        catch (ContractError error) { Console.Error.WriteLine($"WINDOW-ACTION-REFUSED {error.Code}: Save failed; window remains open."); }
    }
}
