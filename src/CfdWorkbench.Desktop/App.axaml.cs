using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop;

public sealed class App : Application
{
    private static readonly Lazy<WindowIcon?> CachedIcon = new(() =>
    {
        try
        {
            var uri = new Uri("avares://CfdWorkbench.Desktop/Assets/app-icon.ico");
            if (!AssetLoader.Exists(uri)) return null;
            using var stream = AssetLoader.Open(uri);
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    });

    /// <summary>Application window icon loaded once from embedded Assets/app-icon.ico (F-35).</summary>
    public static WindowIcon? AppIcon => CachedIcon.Value;

    public const string AppVersion = "0.1.0";

    public override void Initialize()
    {
        Console.Error.WriteLine("NATIVE-STARTUP app-initialize");
        AvaloniaXamlLoader.Load(this);
        ShellHost.InstallTheme(this);
        var applicationMenu = new NativeMenu();
        applicationMenu.Add(new NativeMenuItem("About CFD Workbench")
        {
            Command = new DelegateCommand(() => ShowAboutDialog())
        });
        NativeMenu.SetMenu(this, applicationMenu);
    }

    public static Window CreateAboutDialog(Window? owner = null)
    {
        double padding = Current?.TryFindResource("DialogPadding", out var padObj) == true && padObj is Thickness pad ? pad.Left : 16;
        double spacing = Current?.TryFindResource("DialogSpacing", out var spObj) == true && spObj is double sp ? sp : 12;
        double ownerHeight = owner is { Bounds.Height: > 0 } ? owner.Bounds.Height : 700;
        double maxHeight = Math.Max(260, Math.Min(620, ownerHeight - 60));

        var dialog = new Window
        {
            Title = "About CFD Workbench",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            MaxHeight = maxHeight,
            CanResize = false,
            WindowStartupLocation = owner is not null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            RequestedThemeVariant = owner?.ActualThemeVariant ?? ThemeVariant.Default,
            Icon = AppIcon
        };

        var body = new StackPanel
        {
            Spacing = spacing,
            Children =
            {
                new TextBlock { Text = "CFD Workbench", Classes = { "heading" }, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Version {AppVersion}", Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
                new TextBlock
                {
                    Text = "Offline parametric hydrofoil design and aerodynamic analysis workbench.",
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        var closeButton = new Button { Content = "Close", IsDefault = true, IsCancel = true };
        AutomationProperties.SetName(closeButton, "Close About CFD Workbench");
        closeButton.Click += (_, _) => dialog.Close();

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, spacing, 0, 0),
            Children = { closeButton }
        };

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = body
        };

        var root = new Grid
        {
            Margin = new Thickness(padding),
            RowDefinitions = new RowDefinitions("*,Auto")
        };
        Grid.SetRow(scroller, 0);
        Grid.SetRow(footer, 1);
        root.Children.Add(scroller);
        root.Children.Add(footer);
        dialog.Content = root;
        dialog.Opened += (_, _) => closeButton.Focus();
        if (OsAccessibility.IsReducedMotion())
            dialog.LayoutUpdated += (_, _) => MainWindow.SuppressTransitions(dialog);
        return dialog;
    }

    public static void ShowAboutDialog(Window? owner = null)
    {
        var resolvedOwner = owner ?? (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var dialog = CreateAboutDialog(resolvedOwner);
        if (resolvedOwner is { IsVisible: true }) _ = dialog.ShowDialog(resolvedOwner);
        else dialog.Show();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.Error.WriteLine($"NATIVE-STARTUP lifetime={ApplicationLifetime?.GetType().Name ?? "none"}");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                string preferenceRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CFD Workbench");
                BindPreferenceRoot(preferenceRoot);
                var preferences = new PreferenceStore(preferenceRoot, () => new ProjectStore());
                desktop.MainWindow = CreateMainWindow(preferences);
                Console.Error.WriteLine($"NATIVE-STARTUP main-window-assigned={desktop.MainWindow is not null}");
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"NATIVE-STARTUP main-window-error={error.GetType().Name} inner={error.InnerException?.GetType().Name ?? "none"}");
                throw;
            }
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>My sections for the preference root this process bound (F-6, ADR-0008). One root, every window.</summary>
    public static SectionLibrary? Sections { get; private set; }

    public static void BindPreferenceRoot(string preferenceRoot) =>
        Sections = new SectionLibrary(SectionLibrary.Root(preferenceRoot));

    public static void ClearSectionLibrary() => Sections = null;

    public static MainWindow CreateMainWindow(PreferenceStore? preferences = null) =>
        new(preferences);
}
