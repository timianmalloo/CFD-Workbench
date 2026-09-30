using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Panes;

public partial class StartView : UserControl
{
    private Control? originatingControl;
    public event Action<string>? RecentRequested;
    public event Action? LocateRequested;
    public event Action? OpenAnotherRequested;
    public event Action? TryAgainRequested;
    public event Action? RemoveRecentRequested;
    public Control? SelectedRecentControl => RecentListBox.SelectedItem as Control;

    public StartView()
    {
        InitializeComponent();

        AlertDismissButton.Click += (_, _) => DismissAlert();
        AlertLocateButton.Click += (_, _) => LocateRequested?.Invoke();
        AlertOpenAnotherButton.Click += (_, _) => OpenAnotherRequested?.Invoke();
        AlertTryAgainButton.Click += (_, _) => TryAgainRequested?.Invoke();
        AlertRemoveRecentButton.Click += (_, _) => RemoveRecentRequested?.Invoke();
        OpenCancelButton.Click += (_, _) => CancelOpening();
        RecentListBox.DoubleTapped += (_, _) => RequestSelectedRecent();
        RecentListBox.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter) return;
            RequestSelectedRecent();
            args.Handled = true;
        };
    }

    private void RequestSelectedRecent()
    {
        if (RecentListBox.SelectedItem is ListBoxItem { Content: string path })
            RecentRequested?.Invoke(path);
    }

    public void PopulateRecent(IReadOnlyList<string> paths)
    {
        if (paths == null || paths.Count == 0)
        {
            RecentSection.IsVisible = false;
            RecentListBox.ItemsSource = null;
        }
        else
        {
            RecentSection.IsVisible = true;
            RecentListBox.ItemsSource = paths.Select(p => new ListBoxItem
            {
                Content = p,
                [Avalonia.Automation.AutomationProperties.NameProperty] = $"{Path.GetFileName(p)}, {Path.GetFileName(Path.GetDirectoryName(p))}",
                [Avalonia.Automation.AutomationProperties.HelpTextProperty] = p
            }).ToList();
        }
    }

    public void ShowOpening(string fileName, Control? origin = null)
    {
        originatingControl = origin;
        OpeningText.Text = $"Opening {fileName}…";
        OpeningPanel.IsVisible = true;
        OpenCancelButton.Focus();
    }

    public void CancelOpening()
    {
        OpeningPanel.IsVisible = false;
        (originatingControl ?? StartNewButton).Focus();
    }

    public void HideOpening()
    {
        OpeningPanel.IsVisible = false;
    }

    public static string FailureMessage(OpenFailure failure, string fileName) => failure switch
    {
        OpenFailure.Missing => "It isn't where it was — it may have been moved, renamed or deleted. The file hasn't been changed.",
        OpenFailure.AccessDenied => "CFD Workbench isn't allowed to read it. The file hasn't been changed. Check its permissions in Finder, or open another file.",
        OpenFailure.Unreadable => "It couldn't be read from the disk. The file hasn't been changed.",
        OpenFailure.NotRecognised => "It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed.",
        OpenFailure.TooLarge => $"It is larger than CFD Workbench can open ({(fileName.EndsWith(".foil", StringComparison.Ordinal) ? "1 MiB" : "8 MB")}). The file hasn't been changed.",
        OpenFailure.UnknownContent => "It contains parts this version doesn't understand. The file hasn't been changed. A newer version of CFD Workbench may open it.",
        OpenFailure.Newer => "It was saved by a newer version of CFD Workbench. The file hasn't been changed.",
        _ => "It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed."
    };

    public void ShowAlert(string fileName, OpenFailure failure, bool fromRecent = false, bool isMissingFixture = false)
    {
        AlertTitle.Text = $"“{fileName}” didn't open.";
        AlertLocateButton.IsVisible = false;
        AlertOpenAnotherButton.IsVisible = false;
        AlertTryAgainButton.IsVisible = false;
        AlertRemoveRecentButton.IsVisible = false;
        AlertNewFoilButton.IsVisible = false;

        if (isMissingFixture)
        {
            AlertMessage.Text = "The built-in example is missing or damaged. Nothing was overwritten.";
            AlertNewFoilButton.IsVisible = true;
            AlertOpenAnotherButton.IsVisible = true;
            AlertPanel.IsVisible = true;
            AlertNewFoilButton.Focus();
            return;
        }

        AlertMessage.Text = FailureMessage(failure, fileName);
        switch (failure)
        {
            case OpenFailure.Missing:
                AlertLocateButton.IsVisible = true;
                AlertOpenAnotherButton.IsVisible = true;
                if (fromRecent) AlertRemoveRecentButton.IsVisible = true;
                break;
            case OpenFailure.Unreadable:
                AlertTryAgainButton.IsVisible = true;
                AlertOpenAnotherButton.IsVisible = true;
                break;
            default:
                AlertOpenAnotherButton.IsVisible = true;
                break;
        }

        AlertPanel.IsVisible = true;
        if (AlertLocateButton.IsVisible) AlertLocateButton.Focus();
        else if (AlertOpenAnotherButton.IsVisible) AlertOpenAnotherButton.Focus();
        else if (AlertTryAgainButton.IsVisible) AlertTryAgainButton.Focus();
        else AlertDismissButton.Focus();
    }

    public void DismissAlert()
    {
        AlertPanel.IsVisible = false;
        StartNewButton.Focus();
    }
}
