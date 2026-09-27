using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Desktop.Panes;

namespace CfdWorkbench.Desktop;

public partial class ModelArea : UserControl
{
    public ModelArea()
    {
        InitializeComponent();

        DismissAlertBandButton.Click += (_, _) => AlertBand.IsVisible = false;
    }

    public void ShowFoilOpen(bool isOpen)
    {
        StartCardView.IsVisible = !isOpen;
        DocumentTabs.IsVisible = isOpen;
    }

    public void ShowAlertBand(string message, bool showAcceptIds = false, bool showResumeRecovery = false)
    {
        AlertBandText.Text = message;
        AcceptIdsButton.IsVisible = showAcceptIds;
        ResumeRecoveryButton.IsVisible = showResumeRecovery;
        DiscardRecoveryButton.IsVisible = showResumeRecovery;
        DismissAlertBandButton.IsVisible = !showAcceptIds && !showResumeRecovery;
        AlertBand.IsVisible = true;
    }

    public void HideAlertBand()
    {
        AlertBand.IsVisible = false;
    }
}
