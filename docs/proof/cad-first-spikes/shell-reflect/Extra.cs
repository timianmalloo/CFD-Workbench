// Spike addendum 2: Avalonia 11.3.14 APIs the shell design relies on (TextBox edit verbs, NativeMenuItem, focus, screens).
using System.Reflection;
static class Extra
{
    static void Show(Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var ms = t.GetMember(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Console.WriteLine($"API {t.Name}.{n} = {(ms.Length == 0 ? "MISSING" : string.Join(" | ", ms.Select(m => m.ToString())))}");
        }
    }
    public static void Run()
    {
        Show(typeof(Avalonia.Controls.TextBox), "Undo", "Redo", "CanUndo", "CanRedo", "Cut", "Copy", "Paste", "SelectAll", "IsUndoEnabled");
        Show(typeof(Avalonia.Controls.NativeMenuItem), "Gesture", "Command", "IsEnabled", "Click", "Header", "ToggleType", "IsChecked");
        Show(typeof(Avalonia.Input.IFocusManager), "GetFocusedElement");
        Show(typeof(Avalonia.Controls.TopLevel), "FocusManager", "Screens", "PlatformSettings", "GetTopLevel");
        Show(typeof(Avalonia.Controls.Screens), "Changed", "ScreenFromWindow", "ScreenFromPoint", "All");
        Show(typeof(Avalonia.Platform.Screen), "WorkingArea", "Bounds", "Scaling", "DisplayName", "TryGetPlatformHandle");
        Show(typeof(Avalonia.Controls.Window), "Owner", "Position", "FrameSize", "ShowActivated", "PositionChanged", "Activated", "Deactivated");
        Show(typeof(Avalonia.VisualExtensions), "PointToScreen", "PointToClient"); Show(typeof(Avalonia.Input.InputElement), "IsHitTestVisible");
        Show(typeof(Avalonia.Input.InputElement), "GotFocusEvent", "IsKeyboardFocusWithin");
        Show(typeof(Avalonia.Controls.Control), "IsEffectivelyEnabled");
        Show(typeof(Avalonia.Input.InputElement), "IsEffectivelyEnabled");
    }
}
