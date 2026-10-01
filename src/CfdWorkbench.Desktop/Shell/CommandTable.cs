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
    private static readonly DelegateCommand NoOp = new(() => { });

    public static readonly IReadOnlyList<CommandRow> DefaultRows =
    [
        // File
        new("file.new", "New foil", "File", "⌘N", false, NoOp),
        new("file.new-example", "New from example", "File", "⇧⌘N", false, NoOp),
        new("file.open", "Open…", "File", "⌘O", false, NoOp),
        new("file.save", "Save", "File", "⌘S", false, NoOp),
        new("file.save-as", "Save As…", "File", "⇧⌘S", false, NoOp),
        new("file.close", "Close", "File", "⌘W", false, NoOp),

        // Edit
        new("edit.undo", "Undo", "Edit", "⌘Z", true, NoOp),
        new("edit.redo", "Redo", "Edit", "⇧⌘Z", true, NoOp),
        new("edit.cut", "Cut", "Edit", "⌘X", true, NoOp),
        new("edit.copy", "Copy", "Edit", "⌘C", true, NoOp),
        new("edit.paste", "Paste", "Edit", "⌘V", true, NoOp),
        new("edit.select-all", "Select All", "Edit", "⌘A", true, NoOp),

        // View
        new("view.toggle-left", "Left side bar", "View", "⌘B", false, NoOp),
        new("view.toggle-bottom", "Bottom panel", "View", "⌘J", false, NoOp),
        new("view.toggle-right", "Right side bar", "View", "⌥⌘B", false, NoOp),
        new("view.palette", "Command palette", "View", "⌘K", false, NoOp),
        new("view.fit", "Fit", "View", "⌘0", false, NoOp),
        new("view.comb", "Curvature comb", "View", "C", false, NoOp),
        new("view.zoom-in", "Zoom in", "View", "⌘=", false, NoOp),
        new("view.zoom-out", "Zoom out", "View", "⌘−", false, NoOp),
        new("point.make-anchor", "Make anchor", "Edit", null, false, NoOp),
        new("point.make-control", "Make control", "Edit", null, false, NoOp),
        new("point.tangent-smooth", "Smooth tangent", "Edit", null, false, NoOp),
        new("point.tangent-symmetric", "Symmetric tangent", "Edit", null, false, NoOp),
        new("point.tangent-corner", "Corner tangent", "Edit", null, false, NoOp),

        // Window
        new("window.minimize", "Minimize", "Window", "⌘M", false, NoOp),
        new("window.zoom", "Zoom", "Window", "⌃⌘F", false, NoOp),
        new("window.workspace-planform", "Planform", "Window", "⌘1", false, NoOp),
        new("window.workspace-precision", "Precision", "Window", "⌘2", false, NoOp),
        new("window.workspace-review", "Review", "Window", "⌘3", false, NoOp),
        new("window.reset-layout", "Reset layout", "Window", "⌥⌘R", false, NoOp),
        new("window.maximize-pane", "Maximize pane", "Window", "⇧⌘M", false, NoOp)
    ];

    public static IReadOnlyList<CommandRow> Rows => DefaultRows;

    public static IReadOnlyList<PaletteEntry> PaletteEntries() =>
        Rows.Select(r => new PaletteEntry(r.Id, r.Title, r.Gesture, r.Menu)).ToList();

    public static IReadOnlyList<CommandRow> MenuFor(string menuName) =>
        Rows.Where(r => string.Equals(r.Menu, menuName, StringComparison.OrdinalIgnoreCase)).ToList();

    public static IReadOnlyList<CommandRow> Bindings(IReadOnlySet<string>? exportedGestures = null) =>
        Rows.Where(r => !string.IsNullOrEmpty(r.Gesture) && (exportedGestures == null || !exportedGestures.Contains(r.Gesture))).ToList();
}
