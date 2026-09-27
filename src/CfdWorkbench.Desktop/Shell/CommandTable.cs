using System.Windows.Input;

namespace CfdWorkbench.Desktop.Shell;

public sealed record CommandRow(
    string Id,
    string Title,
    string Menu,
    string? Gesture,
    bool IsEditVerb,
    ICommand Command);

public sealed record PaletteEntry(string Id, string Title, string? Gesture, string Menu);

public sealed class DelegateCommand : ICommand
{
    private readonly Action execute;
    private readonly Func<bool>? canExecute;

    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public static class CommandTable
{
    public static readonly IReadOnlyList<CommandRow> DefaultRows = [];

    public static IReadOnlyList<CommandRow> Rows => throw new NotImplementedException("track D1 stub");

    public static IReadOnlyList<PaletteEntry> PaletteEntries() => throw new NotImplementedException("track D1 stub");

    public static IReadOnlyList<CommandRow> MenuFor(string menuName) => throw new NotImplementedException("track D1 stub");
}
