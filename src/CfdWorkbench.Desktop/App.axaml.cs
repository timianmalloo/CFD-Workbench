using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace CfdWorkbench.Desktop;

public sealed class App : Application
{
    public override void Initialize()
    {
        Console.Error.WriteLine("NATIVE-STARTUP app-initialize");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.Error.WriteLine($"NATIVE-STARTUP lifetime={ApplicationLifetime?.GetType().Name ?? "none"}");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.MainWindow = new MainWindow();
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
}
