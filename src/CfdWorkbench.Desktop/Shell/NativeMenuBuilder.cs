using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using CfdWorkbench.Persistence;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Shell;

public static class NativeMenuBuilder
{
    public static void RefreshEditMenu(Window window, Func<string, bool> canExecute)
    {
        var host = FindHost(window);
        var edit = NativeMenu.GetMenu(window)?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "Edit"));
        if (edit?.Menu is null) return;
        foreach (var item in edit.Menu.Items.OfType<NativeMenuItem>())
        {
            string? id = item.Header?.ToString() switch
            {
                "Undo" => "edit.undo",
                "Redo" => "edit.redo",
                "Add point…" => "point.add",
                "Remove point" => "point.remove",
                var title when title?.StartsWith("Rebuild ", StringComparison.Ordinal) == true => "point.rebuild",
                _ => null
            };
            if (id is null) continue;
            if (id == "point.rebuild")
            {
                string? curve = host?.Controller.Selection is Selection.Points { Items.Count: 1 } points
                    ? points.Items[0].Curve : null;
                item.Header = curve is not null && PropertiesView.Curves.TryGetValue(curve, out var rows)
                    ? $"Rebuild {rows.Name.ToLowerInvariant()}…" : "Rebuild curve…";
            }
            item.IsEnabled = id.StartsWith("point.", StringComparison.Ordinal) ? host?.CanRun(id) == true : canExecute(id);
            (item.Command as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }
    public static KeyGesture? ParseGesture(string? gestureString, bool? macOS = null)
    {
        if (string.IsNullOrWhiteSpace(gestureString)) return null;

        var modifiers = KeyModifiers.None;
        var s = gestureString;

        if (s.Contains('⌥')) { modifiers |= KeyModifiers.Alt; s = s.Replace("⌥", ""); }
        if (s.Contains('⇧')) { modifiers |= KeyModifiers.Shift; s = s.Replace("⇧", ""); }
        if (s.Contains('⌃')) { modifiers |= KeyModifiers.Control; s = s.Replace("⌃", ""); }
        if (s.Contains('⌘'))
        {
            modifiers |= macOS ?? OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
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
            "↩" => Key.Return,
            "=" => Key.OemPlus,
            "-" => Key.OemMinus,
            "−" => Key.OemMinus,
            _ => Key.None
        };

        if (key == Key.None) return null;
        // A single-character key (C, F) is bound on the view control, not the window, so it never fires in a text field
        // (2.1.4); the menu shows it through the palette instead of claiming it as a key equivalent.
        if (modifiers == KeyModifiers.None) return null;
        return new KeyGesture(key, modifiers);
    }

    private static bool IsPaneCommand(string id) =>
        id.StartsWith("point.", StringComparison.Ordinal) || id.StartsWith("view.text-", StringComparison.Ordinal) ||
        id is "view.comb" || CommandTable.UnitsOf(id) is not null || ViewCommands.Handles(id) || ShellHost.IsShellCommand(id);

    /// <summary>The View submenus built from the table after Fit Selection (M1.2b2 §5.2), in this order.</summary>
    private static readonly string[] ViewSubmenus = [ViewCommands.ViewsMenu, ViewCommands.DisplayMenu, ViewCommands.CameraMenu, ViewCommands.PanMenu];

    private static ShellHost? FindHost(Window window)
    {
        if (window.Content is ShellHost direct) return direct;
        return window.Content is Control root ? root.GetVisualDescendants().OfType<ShellHost>().FirstOrDefault() : null;
    }

    public static NativeMenu BuildForWindow(Window window) => BuildMenu(window);

    /// <summary>
    /// Off macOS: the menu built from the table shows in the window (ADR-0009 S1). The bar is a <see cref="Menu"/>, so Alt and
    /// F10 reach it. Its gestures are also bound on the window, each to the menu item's own command object, so a key and a
    /// click run one command. Without this a gesture lives only on the macOS system menu (W-1 defects b and c: Ctrl+Z and
    /// Ctrl+S did nothing on Windows). A key handled by a text box (its own undo, copy, paste) never reaches the window.
    /// </summary>
    public static void ShowInWindow(Window window, ShellHost host, NativeMenu menu)
    {
        host.ShowMenuBar();
        foreach (var item in Flatten(menu).Where(item => item.Gesture is not null && item.Command is not null))
            window.KeyBindings.Add(new KeyBinding { Gesture = item.Gesture!, Command = item.Command! });
    }

    private static IEnumerable<NativeMenuItem> Flatten(NativeMenu? level) =>
        level?.Items.OfType<NativeMenuItem>().SelectMany(item => Flatten(item.Menu).Prepend(item)) ?? [];

    public static NativeMenu BuildMenu(
        Window window,
        Action<string>? onAction = null,
        IReadOnlyList<RecentEntry>? recentEntries = null,
        Action<string>? onOpenRecent = null,
        Action? onClearRecent = null,
        Action<string>? onSelectPane = null,
        Func<string, bool>? canExecute = null,
        bool? macOS = null)
    {
        var rootMenu = new NativeMenu();

        var menuGroups = CommandTable.Rows
            .GroupBy(r => r.Menu, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Standard menu order: File, Edit, View, then the section editor's menu (M1.2c §5.2), then Window
        foreach (var menuTitle in new[] { "File", "Edit", "View", CommandTable.SectionMenu, "Window" })
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

                // View ▸ Units (Ruling 115) follows Text size: Metric and Imperial as radio items that follow the controller,
                // whichever control (this menu, the status-bar item, the palette) changed the units.
                if (row.Id == "view.zoom-out" && menuGroups.TryGetValue(CommandTable.UnitsMenu, out var unitRows))
                {
                    var unitsMenu = new NativeMenu();
                    var unitItems = new List<NativeMenuItem>();
                    foreach (var unitRow in unitRows)
                    {
                        var unitItem = Item(unitRow);
                        unitsMenu.Add(unitItem);
                        unitItems.Add(unitItem);
                    }
                    menu.Add(new NativeMenuItem(CommandTable.UnitsMenu) { Menu = unitsMenu });
                    if (FindHost(window) is { } unitsHost)
                    {
                        SyncUnitItems(unitRows, unitItems, unitsHost.Controller.AnalysisUnits);
                        unitsHost.Controller.UnitsChanged += () => SyncUnitItems(unitRows, unitItems, unitsHost.Controller.AnalysisUnits);
                    }
                }

                // Views, Display, Camera and Pan after Fit Selection; the radio items follow the controller, whichever
                // control changed the layout or the target view's display.
                if (row.Id == "view.fit-selection")
                {
                    var built = new List<NativeMenuItem>();
                    foreach (var name in ViewSubmenus)
                    {
                        if (!menuGroups.TryGetValue(name, out var choices)) continue;
                        var subMenu = new NativeMenu();
                        foreach (var choice in choices)
                        {
                            var choiceItem = Item(choice);
                            subMenu.Add(choiceItem);
                            built.Add(choiceItem);
                        }
                        menu.Add(new NativeMenuItem(name) { Menu = subMenu });
                    }
                    if (FindHost(window) is { } viewHost)
                    {
                        SyncViewItems(built, viewHost.Controller);
                        viewHost.Controller.Changed += () =>
                        {
                            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) SyncViewItems(built, viewHost.Controller);
                            else Avalonia.Threading.Dispatcher.UIThread.Post(() => SyncViewItems(built, viewHost.Controller));
                        };
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
                item.Gesture = ParseGesture(row.Gesture, macOS);
            }
            if (CommandTable.TextSizeOf(row.Id) is not null || row.Menu is ViewCommands.ViewsMenu or ViewCommands.DisplayMenu or CommandTable.UnitsMenu)
                item.ToggleType = NativeMenuItemToggleType.Radio;

            item.Command = new DelegateCommand(() =>
            {
                if (row.IsEditVerb)
                {
                    var focus = TopLevel.GetTopLevel(window)?.FocusManager?.GetFocusedElement();
                    EditVerbRouter.Execute(row.Id.Replace("edit.", ""), focus, () => onAction?.Invoke(row.Id));
                }
                else if (row.Id == "view.analysis" && FindHost(window) is { } analysisHost)
                {
                    analysisHost.Controller.ToggleAnalysis();
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

    /// <summary>Checks the Views and Display radio items that match the controller, and refreshes every view item's enablement.</summary>
    private static void SyncViewItems(IEnumerable<NativeMenuItem> items, WorkbenchController controller)
    {
        foreach (var item in items)
        {
            if (CommandTable.Rows.FirstOrDefault(row => ViewCommands.Handles(row.Id) && Equals(row.Title, item.Header)) is not { } row) continue;
            if (ViewCommands.IsChecked(row.Id, controller) is { } isChecked) item.IsChecked = isChecked;
            item.IsEnabled = item.Command?.CanExecute(null) ?? true;
            (item.Command as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Checks the Units radio item that matches the controller's units.</summary>
    private static void SyncUnitItems(IReadOnlyList<CommandRow> rows, IReadOnlyList<NativeMenuItem> items, CfdWorkbench.Analysis.Units units)
    {
        for (int i = 0; i < rows.Count; i++)
            items[i].IsChecked = CommandTable.UnitsOf(rows[i].Id) == units;
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
