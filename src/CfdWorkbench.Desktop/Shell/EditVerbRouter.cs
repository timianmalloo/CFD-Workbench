using Avalonia.Controls;
using Avalonia.Input;

namespace CfdWorkbench.Desktop.Shell;

public enum EditVerbTarget
{
    ToText,
    ToDocument,
    Ignored
}

/// <summary>
/// Routes Edit verbs (Undo, Redo, Cut, Copy, Paste, Select All) to a focused text field first.
/// With a TextBox focused in the key window, Edit verbs never reach the document.
/// </summary>
public static class EditVerbRouter
{
    public static EditVerbTarget Route(string verb, IInputElement? keyWindowFocus)
    {
        if (keyWindowFocus is TextBox)
        {
            return EditVerbTarget.ToText;
        }

        if (string.Equals(verb, "undo", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(verb, "redo", StringComparison.OrdinalIgnoreCase))
        {
            return EditVerbTarget.ToDocument;
        }

        return EditVerbTarget.Ignored;
    }

    public static void Execute(string verb, IInputElement? keyWindowFocus, Action? documentAction = null)
    {
        var target = Route(verb, keyWindowFocus);
        if (target == EditVerbTarget.ToText && keyWindowFocus is TextBox tb)
        {
            switch (verb.ToLowerInvariant())
            {
                case "undo":
                    tb.Undo();
                    break;
                case "redo":
                    tb.Redo();
                    break;
                case "cut":
                    tb.Cut();
                    break;
                case "copy":
                    tb.Copy();
                    break;
                case "paste":
                    tb.Paste();
                    break;
                case "select-all":
                case "selectall":
                    tb.SelectAll();
                    break;
            }
        }
        else if (target == EditVerbTarget.ToDocument)
        {
            documentAction?.Invoke();
        }
    }
}
