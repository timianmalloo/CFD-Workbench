using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
        Plan3DContent.IsVisible = isOpen;
    }

    public void ShowAlertBand(string message, bool showAcceptIds = false, bool showResumeRecovery = false)
    {
        AlertBandText.Text = message;
        BandLocateButton.IsVisible = false;
        BandTryAgainButton.IsVisible = false;
        BandOpenAnotherButton.IsVisible = false;
        BandRemoveRecentButton.IsVisible = false;
        AcceptIdsButton.IsVisible = showAcceptIds;
        ResumeRecoveryButton.IsVisible = showResumeRecovery;
        DiscardRecoveryButton.IsVisible = showResumeRecovery;
        DismissAlertBandButton.IsVisible = !showAcceptIds && !showResumeRecovery;
        AlertBand.IsVisible = true;
        if (showAcceptIds) AcceptIdsButton.Focus();
        else if (showResumeRecovery) ResumeRecoveryButton.Focus();
        else DismissAlertBandButton.Focus();
    }

    public void ShowOpenFailure(string fileName, OpenFailure failure, bool fromRecent)
    {
        ShowAlertBand($"“{fileName}” didn't open. {StartView.FailureMessage(failure, fileName)}");
        DismissAlertBandButton.IsVisible = false;
        BandLocateButton.IsVisible = failure is OpenFailure.Missing;
        BandTryAgainButton.IsVisible = failure is OpenFailure.Unreadable;
        BandOpenAnotherButton.IsVisible = failure is not OpenFailure.Newer;
        BandRemoveRecentButton.IsVisible = fromRecent && failure is OpenFailure.Missing;
        Dispatcher.UIThread.Post(() =>
        {
            if (BandLocateButton.IsVisible) BandLocateButton.Focus();
            else if (BandTryAgainButton.IsVisible) BandTryAgainButton.Focus();
            else if (BandOpenAnotherButton.IsVisible) BandOpenAnotherButton.Focus();
        }, DispatcherPriority.Input);
    }

    public void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.IsVisible = true;
    }

    public void HideAlertBand()
    {
        AlertBand.IsVisible = false;
    }
}
