using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Shell;

public static class NativeMenuBuilder
{
    public static void RefreshEditMenu(Window window, Func<string, bool> canExecute)
    {
        var edit = NativeMenu.GetMenu(window)?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "Edit"));
        if (edit?.Menu is null) return;
        foreach (var item in edit.Menu.Items.OfType<NativeMenuItem>())
        {
            string? id = item.Header?.ToString() switch
            {
                "Undo" => "edit.undo",
                "Redo" => "edit.redo",
                _ => null
            };
            if (id is null) continue;
            item.IsEnabled = canExecute(id);
            (item.Command as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }
    public static KeyGesture? ParseGesture(string? gestureString)
    {
        if (string.IsNullOrWhiteSpace(gestureString)) return null;

        var modifiers = KeyModifiers.None;
        var s = gestureString;

        if (s.Contains('⌥')) { modifiers |= KeyModifiers.Alt; s = s.Replace("⌥", ""); }
        if (s.Contains('⇧')) { modifiers |= KeyModifiers.Shift; s = s.Replace("⇧", ""); }
        if (s.Contains('⌃')) { modifiers |= KeyModifiers.Control; s = s.Replace("⌃", ""); }
        if (s.Contains('⌘'))
        {
            modifiers |= OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
            s = s.Replace("⌘", "");
        }

        Key key = s switch
        {
            "N" => Key.N,
            "O" => Key.O,
            "S" => Key.S,
            "W" => Key.W,
            "Z" => Key.Z,
            "Y" => Key.Y,
            "X" => Key.X,
            "C" => Key.C,
            "V" => Key.V,
            "A" => Key.A,
            "B" => Key.B,
            "J" => Key.J,
            "K" => Key.K,
            "0" => Key.D0,
            "1" => Key.D1,
            "2" => Key.D2,
            "3" => Key.D3,
            "M" => Key.M,
            "F" => Key.F,
            "R" => Key.R,
            "=" => Key.OemPlus,
            "-" => Key.OemMinus,
            "−" => Key.OemMinus,
            _ => Key.None
        };

        if (key == Key.None) return null;
        if (modifiers == KeyModifiers.None && key == Key.C) return null;
        return new KeyGesture(key, modifiers);
    }

    private static bool IsPaneCommand(string id) =>
        id.StartsWith("point.", StringComparison.Ordinal) || id.StartsWith("view.text-", StringComparison.Ordinal) ||
        id is "view.comb" or "view.zoom-in" or "view.zoom-out" or "view.fit";

    private static ShellHost? FindHost(Window window)
    {
        if (window.Content is ShellHost direct) return direct;
        return window.Content is Control root ? root.GetVisualDescendants().OfType<ShellHost>().FirstOrDefault() : null;
    }

    public static NativeMenu BuildForWindow(Window window) => BuildMenu(window);

    public static NativeMenu BuildMenu(
        Window window,
        Action<string>? onAction = null,
        IReadOnlyList<RecentEntry>? recentEntries = null,
        Action<string>? onOpenRecent = null,
        Action? onClearRecent = null,
        Action<string>? onSelectPane = null,
        Func<string, bool>? canExecute = null)
    {
        var rootMenu = new NativeMenu();

        var menuGroups = CommandTable.Rows
            .GroupBy(r => r.Menu, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Standard menu order: File, Edit, View, Window
        foreach (var menuTitle in new[] { "File", "Edit", "View", "Window" })
        {
            var menu = new NativeMenu();
            var topItem = new NativeMenuItem(menuTitle) { Menu = menu };
            rootMenu.Add(topItem);

            if (!menuGroups.TryGetValue(menuTitle, out var rows)) continue;

            foreach (var row in rows)
            {
                menu.Add(Item(row));

                // Insert the Text size submenu after "view.zoom-out" (DN-5): Bigger, Smaller and the ladder as radio items.
                if (row.Id == "view.zoom-out" && menuGroups.TryGetValue(CommandTable.TextSizeMenu, out var sizes))
                {
                    var sizeMenu = new NativeMenu();
                    foreach (var size in sizes) sizeMenu.Add(Item(size));
                    menu.Add(new NativeMenuItem(CommandTable.TextSizeMenu) { Menu = sizeMenu });
                    if (FindHost(window) is { } shell)
                    {
                        CheckTextSize(sizeMenu, shell.TextScale);
                        shell.TextScaleChanged += scale => CheckTextSize(sizeMenu, scale);
                    }
                }

                // Insert Open Recent submenu after "file.open"
                if (row.Id == "file.open")
                {
                    var openRecentMenu = new NativeMenu();
                    var openRecentItem = new NativeMenuItem("Open Recent") { Menu = openRecentMenu };
                    PopulateRecentMenu(openRecentMenu, recentEntries, onOpenRecent, onClearRecent);
                    menu.Add(openRecentItem);
                }

                // Insert Panes submenu in Window menu before "window.reset-layout"
                if (menuTitle == "Window" && row.Id == "window.workspace-review")
                {
                    var panesMenu = new NativeMenu();
                    var panesItem = new NativeMenuItem("Panes") { Menu = panesMenu };
                    foreach (var pane in WorkspacePresets.RegisteredPanes)
                    {
                        string paneTitle = char.ToUpperInvariant(pane[0]) + pane.Substring(1);
                        var paneItem = new NativeMenuItem(paneTitle)
                        {
                            Command = new DelegateCommand(() => onSelectPane?.Invoke(pane))
                        };
                        panesMenu.Add(paneItem);
                    }
                    menu.Add(panesItem);
                }
            }
        }

        NativeMenu.SetMenu(window, rootMenu);
        return rootMenu;

        NativeMenuItem Item(CommandRow row)
        {
            var item = new NativeMenuItem(row.Title);
            if (row.Gesture is not null)
            {
                item.Gesture = ParseGesture(row.Gesture);
            }
            if (CommandTable.TextSizeOf(row.Id) is not null) item.ToggleType = NativeMenuItemToggleType.Radio;

            item.Command = new DelegateCommand(() =>
            {
                if (row.IsEditVerb)
                {
                    var focus = TopLevel.GetTopLevel(window)?.FocusManager?.GetFocusedElement();
                    EditVerbRouter.Execute(row.Id.Replace("edit.", ""), focus, () => onAction?.Invoke(row.Id));
                }
                else if (IsPaneCommand(row.Id) && FindHost(window) is { } host)
                {
                    _ = host.RunCommand(row.Id);
                }
                else
                {
                    onAction?.Invoke(row.Id);
                }
            }, () =>
            {
                if (row.Id is "edit.undo" or "edit.redo") return canExecute?.Invoke(row.Id) ?? true;
                if (IsPaneCommand(row.Id) && FindHost(window) is { } host) return host.CanRun(row.Id);
                return true;
            });
            return item;
        }
    }

    /// <summary>Checks the Text size radio item that matches the current multiplier.</summary>
    private static void CheckTextSize(NativeMenu menu, double scale)
    {
        foreach (var item in menu.Items.OfType<NativeMenuItem>())
            if (CommandTable.Rows.FirstOrDefault(row => row.Menu == CommandTable.TextSizeMenu && Equals(row.Title, item.Header)) is { } row &&
                CommandTable.TextSizeOf(row.Id) is { } size)
                item.IsChecked = Math.Abs(size - scale) < 1e-9;
    }

    public static void PopulateRecentMenu(
        NativeMenu menu,
        IReadOnlyList<RecentEntry>? recentEntries,
        Action<string>? onOpenRecent,
        Action? onClearRecent)
    {
        menu.Items.Clear();

        if (recentEntries != null && recentEntries.Count > 0)
        {
            foreach (var entry in recentEntries)
            {
                var recentItem = new NativeMenuItem(entry.Path)
                {
                    Command = new DelegateCommand(() => onOpenRecent?.Invoke(entry.Path))
                };
                menu.Add(recentItem);
            }
            menu.Add(new NativeMenuItemSeparator());
        }

        var clearItem = new NativeMenuItem("Clear Menu")
        {
            Command = new DelegateCommand(() => onClearRecent?.Invoke())
        };
        menu.Add(clearItem);
    }

    public static void RefreshRecentMenu(Window window, IReadOnlyList<RecentEntry> entries,
        Action<string> onOpenRecent, Action onClearRecent)
    {
        var file = NativeMenu.GetMenu(window)?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "File"));
        var recent = file?.Menu?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "Open Recent"));
        if (recent?.Menu is { } menu)
            PopulateRecentMenu(menu, entries, onOpenRecent, onClearRecent);
    }
}
