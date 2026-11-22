using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.VisualTree;
using CfdWorkbench.Persistence;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Shell;

/// <summary>
/// The in-window menu bar's keyboard contract off macOS (PR #6, Windows): F10 enters the menu bar, and Escape (or F10 again)
/// from the menu leaves it and returns focus to the control that had it before. On Windows Alt already reached the bar, but
/// F10 did nothing and Escape closed the drop-down while focus stayed on the menu item. The menu is found on each key, because
/// the bar's template builds it after the window is constructed.
/// </summary>
public sealed class MenuBarKeys
{
    private readonly Window window;
    private readonly Func<Menu?> find;
    private IInputElement? origin;

    public MenuBarKeys(Window window, Func<Menu?> find)
    {
        this.window = window;
        this.find = find;
        window.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private static bool InMenu(Menu menu, object? element) =>
        element is StyledElement styled && styled.GetSelfAndLogicalAncestors().Contains(menu);

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (find() is { } menu && !InMenu(menu, e.Source)) origin = e.Source as IInputElement;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None || find() is not { } menu) return;
        bool inMenu = menu.IsOpen || InMenu(menu, window.FocusManager?.GetFocusedElement());
        if (e.Key == Key.F10)
        {
            e.Handled = true;
            if (inMenu) Leave(menu); else menu.Open();
        }
        else if (e.Key == Key.Escape && inMenu)
            // After the menu's own Escape handling has closed its drop-down.
            Dispatcher.UIThread.Post(() => Leave(menu), DispatcherPriority.Input);
    }

    private void Leave(Menu menu)
    {
        menu.Close();
        (origin as InputElement)?.Focus();
    }
}

public static class NativeMenuBuilder
{
    public static void RefreshEditMenu(Window window, Func<string, bool> canExecute)
    {
        var host = FindHost(window);
        var rootMenu = NativeMenu.GetMenu(window);
        if (rootMenu is null) return;
        var edit = rootMenu.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "Edit"));
        if (edit?.Menu is not null)
        {
            foreach (var item in edit.Menu.Items.OfType<NativeMenuItem>())
            {
                if (item.Header?.ToString()?.StartsWith("Rebuild ", StringComparison.Ordinal) == true)
                {
                    string? curve = host?.Controller.Selection is Selection.Points { Items.Count: 1 } points
                        ? points.Items[0].Curve : null;
                    item.Header = curve is not null && PropertiesView.Curves.TryGetValue(curve, out var rows)
                        ? $"Rebuild {rows.Name.ToLowerInvariant()}…" : "Rebuild curve…";
                }
            }
        }
        // F-20: refresh IsEnabled and CanExecuteChanged across ALL menu commands (not only Edit).
        foreach (var item in Flatten(rootMenu))
        {
            if (item.Command is null) continue;
            item.IsEnabled = item.Command.CanExecute(null);
            (item.Command as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public static KeyGesture? ParseGesture(string? gestureString, bool? macOS = null)
    {
        if (string.IsNullOrWhiteSpace(gestureString)) return null;

        bool isMac = macOS ?? OperatingSystem.IsMacOS();
        // F-20: ⌃⌘F is macOS Fullscreen/Zoom; on Windows both ⌃ and ⌘ map to Control which would hijack Ctrl+F (Find).
        // Use standard Windows F11 instead of Ctrl+F.
        if (gestureString == "⌃⌘F" && !isMac)
            return new KeyGesture(Key.F11, KeyModifiers.None);

        var modifiers = KeyModifiers.None;
        var s = gestureString;

        if (s.Contains('⌥')) { modifiers |= KeyModifiers.Alt; s = s.Replace("⌥", ""); }
        if (s.Contains('⇧')) { modifiers |= KeyModifiers.Shift; s = s.Replace("⇧", ""); }
        if (s.Contains('⌃')) { modifiers |= KeyModifiers.Control; s = s.Replace("⌃", ""); }
        if (s.Contains('⌘'))
        {
            modifiers |= isMac ? KeyModifiers.Meta : KeyModifiers.Control;
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
            "E" => Key.E,
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
            "F11" => Key.F11,
            "↩" => Key.Return,
            "=" => Key.OemPlus,
            "-" => Key.OemMinus,
            "−" => Key.OemMinus,
            _ => Key.None
        };

        if (key == Key.None) return null;
        // A single-character key (C, F) is bound on the view control, not the window, so it never fires in a text field
        // (2.1.4); function keys (F11) are allowed unmodified.
        if (modifiers == KeyModifiers.None && key != Key.F11) return null;
        return new KeyGesture(key, modifiers);
    }

    private static bool IsPaneCommand(string id) => ShellHost.IsPaneCommand(id);

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
        var bar = host.ShowMenuBar();
        host.MenuKeys = new MenuBarKeys(window, () => bar.GetVisualDescendants().OfType<Menu>().FirstOrDefault());
        // F-20: Windows top-level menu bar mnemonics (_File, _Edit, _View, _Section, _Window) applied to the realized
        // in-window Menu items while keeping NativeMenu headers clean for macOS / lookup stability.
        bool mnemonicsApplied = false;
        void ApplyWindowsMnemonics()
        {
            if (mnemonicsApplied) return;
            var visualMenu = bar.GetVisualDescendants().OfType<Menu>().FirstOrDefault();
            if (visualMenu is null) return;
            var topItems = visualMenu.GetVisualDescendants().OfType<MenuItem>()
                .Where(m => m.FindAncestorOfType<MenuItem>() is null)
                .ToArray();
            if (topItems.Length == 0) return;
            foreach (var mi in topItems)
            {
                if (mi.Header is string header && header.Length > 0 && !header.StartsWith('_'))
                    mi.Header = "_" + header;
            }
            mnemonicsApplied = true;
        }
        bar.LayoutUpdated += (_, _) => ApplyWindowsMnemonics();
        ApplyWindowsMnemonics();

        foreach (var item in Flatten(menu).Where(item => item.Gesture is not null && item.Command is not null))
            window.KeyBindings.Add(new KeyBinding { Gesture = item.Gesture!, Command = item.Command! });

        // F-20: Windows standard Redo gesture (Ctrl+Y) alongside Ctrl+Shift+Z.
        var redo = menu.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "Edit"))?.Menu?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => Equals(item.Header, "Redo"));
        if (redo?.Command is not null)
            window.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Y, KeyModifiers.Control), Command = redo.Command });
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
                if (row.Id is "file.save" or "file.save-as" or "file.close") return FindHost(window)?.Controller.Inspection is not null;
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
