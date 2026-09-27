using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Panes;

public partial class StartView : UserControl
{
    private Control? originatingControl;

    public StartView()
    {
        InitializeComponent();

        AlertDismissButton.Click += (_, _) => DismissAlert();
        OpenCancelButton.Click += (_, _) => CancelOpening();
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
            RecentListBox.ItemsSource = paths.Select(p => new ListBoxItem { Content = p }).ToList();
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
        originatingControl?.Focus();
    }

    public void HideOpening()
    {
        OpeningPanel.IsVisible = false;
    }

    public void ShowAlert(string fileName, OpenFailure? failure, string? customMessage = null, bool fromRecent = false, bool isMissingFixture = false)
    {
        AlertTitle.Text = $"“{fileName}” didn't open.";
        AlertLocateButton.IsVisible = false;
        AlertOpenAnotherButton.IsVisible = false;
        AlertTryAgainButton.IsVisible = false;
        AlertRemoveRecentButton.IsVisible = false;
        AlertNewFoilButton.IsVisible = false;

        if (isMissingFixture)
        {
            AlertMessage.Text = "It isn't where it was — it may have been moved, renamed or deleted. The file hasn't been changed.";
            AlertNewFoilButton.IsVisible = true;
            AlertOpenAnotherButton.IsVisible = true;
            AlertPanel.IsVisible = true;
            AlertNewFoilButton.Focus();
            return;
        }

        if (failure is null)
        {
            AlertMessage.Text = customMessage ?? "An error occurred while opening the file.";
            AlertOpenAnotherButton.IsVisible = true;
        }
        else
        {
            switch (failure)
            {
                case OpenFailure.Missing:
                    AlertMessage.Text = "It isn't where it was — it may have been moved, renamed or deleted. The file hasn't been changed.";
                    AlertLocateButton.IsVisible = true;
                    AlertOpenAnotherButton.IsVisible = true;
                    if (fromRecent) AlertRemoveRecentButton.IsVisible = true;
                    break;
                case OpenFailure.AccessDenied:
                    AlertMessage.Text = "CFD Workbench isn't allowed to read it. The file hasn't been changed. Check its permissions in Finder, or open another file.";
                    AlertOpenAnotherButton.IsVisible = true;
                    break;
                case OpenFailure.Unreadable:
                    AlertMessage.Text = "It couldn't be read from the disk. The file hasn't been changed.";
                    AlertTryAgainButton.IsVisible = true;
                    AlertOpenAnotherButton.IsVisible = true;
                    break;
                case OpenFailure.NotRecognised:
                    AlertMessage.Text = "It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed.";
                    AlertOpenAnotherButton.IsVisible = true;
                    break;
                case OpenFailure.TooLarge:
                    AlertMessage.Text = "It is larger than CFD Workbench can open. The file hasn't been changed.";
                    AlertOpenAnotherButton.IsVisible = true;
                    break;
                case OpenFailure.UnknownContent:
                    AlertMessage.Text = "It contains parts this version doesn't understand. The file hasn't been changed. A newer version of CFD Workbench may open it.";
                    AlertOpenAnotherButton.IsVisible = true;
                    break;
                case OpenFailure.Newer:
                    AlertMessage.Text = "It was saved by a newer version of CFD Workbench. The file hasn't been changed.";
                    AlertOpenAnotherButton.IsVisible = true;
                    break;
            }
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
