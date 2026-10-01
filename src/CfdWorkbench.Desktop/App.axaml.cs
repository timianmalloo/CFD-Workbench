using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop;

public sealed class App : Application
{
    public override void Initialize()
    {
        Console.Error.WriteLine("NATIVE-STARTUP app-initialize");
        AvaloniaXamlLoader.Load(this);
        ShellHost.InstallTheme(this);
        var applicationMenu = new NativeMenu();
        applicationMenu.Add(new NativeMenuItem("About CFD Workbench"));
        NativeMenu.SetMenu(this, applicationMenu);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.Error.WriteLine($"NATIVE-STARTUP lifetime={ApplicationLifetime?.GetType().Name ?? "none"}");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var preferences = new PreferenceStore(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CFD Workbench"),
                    () => new ProjectStore());
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

    public static MainWindow CreateMainWindow(PreferenceStore? preferences = null) =>
        new(preferences);
}
