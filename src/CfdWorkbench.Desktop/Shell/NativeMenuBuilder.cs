using Avalonia.Controls;
using Avalonia.Input;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Shell;

public static class NativeMenuBuilder
{
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
            _ => Key.None
        };

        if (key == Key.None) return null;
        return new KeyGesture(key, modifiers);
    }

    public static NativeMenu BuildForWindow(Window window) => BuildMenu(window);

    public static NativeMenu BuildMenu(
        Window window,
        Action<string>? onAction = null,
        IReadOnlyList<RecentEntry>? recentEntries = null,
        Action<string>? onOpenRecent = null,
        Action? onClearRecent = null,
        Action<string>? onSelectPane = null)
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
                var item = new NativeMenuItem(row.Title);
                if (row.Gesture is not null)
                {
                    item.Gesture = ParseGesture(row.Gesture);
                }

                item.Command = new DelegateCommand(() =>
                {
                    if (row.IsEditVerb)
                    {
                        var focus = TopLevel.GetTopLevel(window)?.FocusManager?.GetFocusedElement();
                        EditVerbRouter.Execute(row.Id.Replace("edit.", ""), focus, () => onAction?.Invoke(row.Id));
                    }
                    else
                    {
                        onAction?.Invoke(row.Id);
                    }
                });
                // Hook point: NEWFOIL adds WorkbenchController.NewFoilAsync at the coordinator join.
                if (row.Id == "file.new") item.IsEnabled = false;

                menu.Add(item);

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
