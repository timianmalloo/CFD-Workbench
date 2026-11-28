using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Notebook;

namespace CfdWorkbench.Desktop.Notebook;

/// <summary>
/// <see cref="DialogChrome"/>-conformant 200%-scaling CAD Handoff confirmation dialog
/// (<c>docs/design/ai-guided-design-notebook.md §8</c>, <c>COPY-932</c>).
/// Refuses confirmation unless the candidate's <c>GeometryStatus</c> is <see cref="GeometryStatus.Certified"/>.
/// </summary>
public partial class CadHandoffDialog : Window
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public CadHandoffDialog()
    {
        InitializeComponent();
        Icon = App.AppIcon;
        Title = NotebookCopy.CadHandoffDialogTitle;
        TitleText.Text = NotebookCopy.CadHandoffDialogTitle;
        ConfirmButton.Content = NotebookCopy.ConfirmOpenInCadButton;
        StayButton.Content = NotebookCopy.StayInNotebookButton;
        OrderFooterButtons();

        StayButton.Click += (_, _) =>
        {
            Confirmed = false;
            Close();
        };
        ConfirmButton.Click += (_, _) =>
        {
            if (!ConfirmButton.IsEnabled) return;
            Confirmed = true;
            Close();
        };
        Opened += (_, _) =>
        {
            MaxHeight = DialogChrome.BoundedMaxHeight(Owner as Window);
            (ConfirmButton.IsEnabled ? ConfirmButton : StayButton).Focus();
        };
    }

    public CadHandoffDialog(
        CandidateIterationRecord candidate,
        int nextRevisionOrdinal,
        string? reqVersionLabel = null,
        string? discipline = null)
        : this()
    {
        ArgumentNullException.ThrowIfNull(candidate);
        BindCandidate(candidate, nextRevisionOrdinal, reqVersionLabel, discipline);
    }

    public bool Confirmed { get; private set; }

    public void BindCandidate(
        CandidateIterationRecord candidate,
        int nextRevisionOrdinal,
        string? reqVersionLabel = null,
        string? discipline = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        bool certified = string.Equals(candidate.GeometryStatus, nameof(GeometryStatus.Certified), StringComparison.Ordinal);

        ConfirmBodyText.Text = NotebookCopy.CadHandoffConfirmBody(candidate.CandidateId, Math.Max(1, nextRevisionOrdinal));
        CandidateSummaryText.Text = certified
            ? $"{candidate.CandidateId} · Geometry checked"
            : $"{candidate.CandidateId} · {candidate.GeometryStatus} ({candidate.RefusalCode ?? "not checked"})";

        SurfaceHashText.Text = string.IsNullOrWhiteSpace(candidate.SurfaceHash)
            ? "None (not geometry checked)"
            : candidate.SurfaceHash;

        var d = candidate.Delta;
        PlanformSummaryText.Text = string.Create(Inv,
            $"Span {d.SpanMeters * 1000:0.0} mm · Area {d.AreaSquareMeters * 1e4:0} cm² · AR {d.AspectRatio:0.00} · Taper {d.Taper:0.00} · Anhedral {d.AnhedralDeg:0.0}° · Washout {d.WashoutDeg:0.0}°");

        SectionsSummaryText.Text = $"{d.RootProfileId} → {d.TipProfileId} (t/c {d.RootThicknessRatio * 100:0.0}%)";

        string reqPart = string.IsNullOrWhiteSpace(reqVersionLabel) ? candidate.ReqVersionId : reqVersionLabel;
        string discPart = string.IsNullOrWhiteSpace(discipline) ? "wingfoil" : discipline;
        GoalStateSummaryText.Text = $"Derived GoalState from FoilReq {reqPart} ({discPart}) saved to Envelope.setup";

        RefusedWarningBanner.IsVisible = !certified;
        RefusedWarningText.Text = certified ? "" : NotebookCopy.OpenInCadDisabledReason;

        ConfirmButton.IsEnabled = certified;
        AutomationProperties.SetItemStatus(ConfirmButton, certified ? "" : "disabled");
        ConfirmButton.Classes.Set("disabled", !certified);
    }

    private void OrderFooterButtons()
    {
        if (OperatingSystem.IsMacOS())
        {
            FooterButtons.Children.Clear();
            FooterButtons.Children.Add(StayButton);
            FooterButtons.Children.Add(ConfirmButton);
        }
    }
}
