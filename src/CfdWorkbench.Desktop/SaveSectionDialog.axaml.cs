using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>Owns the save attempt so a LIB refusal stays beside Name with focus still in the field.</summary>
public partial class SaveSectionDialog : Window
{
    private readonly Func<string, Task> save;
    private readonly Control? returnFocus;
    private bool saving;

    public string? SavedName { get; private set; }

    public SaveSectionDialog(WorkbenchController controller, Func<string, Task> save, Control? returnFocus = null)
    {
        this.save = save;
        this.returnFocus = returnFocus;
        InitializeComponent();
        string source = controller.SourceChip ?? "Source not recorded";
        var mode = controller.Section;
        ProvenanceLine.Text = "Provenance: " + source;
        var raw = mode is null ? null : FoilSource.Parse(mode.Draft.Bytes).Profile(mode.Draft.Profile)?.Provenance;
        var rights = Provenance.Parse(raw).Rights;
        int points = mode is null ? 0 : Sections.View(mode.Draft.Bytes, mode.Draft.Assignment, SurfaceSide.Upper, "display", 0).Points.Count;
        RightsLine.Text = $"Rights: {rights.ToString().ToUpperInvariant()} · What is saved: {points} points per surface · its own t/c";
        SaveButton.Click += async (_, _) => await SaveAsync();
        CancelButton.Click += (_, _) => Close();
        NameBox.KeyDown += async (_, args) =>
        {
            if (args.Key is not (Key.Enter or Key.Return)) return;
            args.Handled = true;
            await SaveAsync();
        };
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape || saving) return;
            args.Handled = true;
            Close();
        }, RoutingStrategies.Tunnel);
        Opened += (_, _) => NameBox.Focus();
        Closed += (_, _) => returnFocus?.Focus();
    }

    public async Task SaveAsync()
    {
        if (saving) return;
        string name = NameBox.Text?.Trim() ?? "";
        if (name.Length == 0)
        {
            ShowError("Name the section to save it.");
            return;
        }
        saving = true;
        SaveButton.IsEnabled = false;
        SaveButton.Content = "Saving…";
        SaveError.IsVisible = false;
        try
        {
            await save(name);
            SavedName = name;
            SavedRegion.Text = $"Saved “{name}” to My sections";
            SavedRegion.IsVisible = true;
            Close(name);
        }
        catch (ContractError error)
        {
            string copy = error.Code switch
            {
                "LIB-NAME-EMPTY" => "Name the section to save it.",
                "LIB-NAME-DUPLICATE" => $"“{name}” is already in My sections. Choose another name.",
                "LIB-SECTION-INVALID" => "Fix the crossing before saving this section.",
                "LIB-CLAIM-HELD" => "Another save is in progress. Try again in a moment.",
                "LIB-IO" => "Couldn't save to My sections: the file couldn't be written. Nothing was saved.",
                "DOC-UNSUPPORTED-PERSISTENCE" => "This system can't save to My sections safely. Nothing was saved.",
                "DOC-SAVE-UNCERTAIN" => "CFD Workbench couldn't confirm whether this section was saved. Check My sections before trying again.",
                _ => $"Couldn't finish saving this section ({error.Code}). Check My sections before trying again."
            };
            ShowError(copy);
        }
        finally
        {
            saving = false;
            SaveButton.IsEnabled = true;
            SaveButton.Content = "Save";
        }
    }

    private void ShowError(string copy)
    {
        SaveError.Text = copy;
        SaveError.IsVisible = true;
        AutomationProperties.SetItemStatus(NameBox, "invalid");
        AutomationProperties.SetHelpText(NameBox, copy);
        NameBox.Focus();
    }
}
