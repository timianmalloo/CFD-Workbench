using System.Globalization;
using Avalonia.Controls;
using CfdWorkbench.Core.Notebook;

namespace CfdWorkbench.Desktop.Notebook;

public sealed record ScorecardRowItem(
    string Label,
    string Target,
    string Actual,
    string Status,
    string MethodTier,
    string? Notice)
{
    public bool HasNotice => !string.IsNullOrWhiteSpace(Notice);
}

/// <summary>
/// Tab 5 (<c>Scorecard &amp; FoilReq</c>) view for <see cref="NotebookWorkspaceView"/>:
/// displays the derived <see cref="RequirementScorecardResult"/> clause table, comparison deltas,
/// mandatory hydrodynamic honesty notices, and copyable <c>FoilReq 1.0</c> monospace DSL.
/// </summary>
public partial class NotebookScorecardView : UserControl
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public NotebookScorecardView()
    {
        InitializeComponent();
        StaleBannerText.Text = NotebookCopy.StaleCandidatesNotice;
        CopyFoilReqButton.Click += async (_, _) => await CopyFoilReqAsync();
    }

    public string CurrentFoilReqText => FoilReqDslBox.Text ?? "";
    public int ClauseCount { get; private set; }
    public bool LastCopySucceeded { get; private set; }

    public void Bind(RequirementScorecardResult? scorecard, string foilReqDsl, string? versionLabel = null)
    {
        FoilReqDslBox.Text = foilReqDsl ?? "";
        FoilReqHeadingText.Text = string.IsNullOrWhiteSpace(versionLabel)
            ? "FoilReq 1.0 DSL"
            : $"FoilReq 1.0 DSL ({versionLabel})";
        CopyFoilReqButton.IsEnabled = !string.IsNullOrWhiteSpace(foilReqDsl);
        CopyReceiptText.IsVisible = false;

        if (scorecard is null)
        {
            StaleBanner.IsVisible = false;
            ComparisonCard.IsVisible = false;
            ClausesList.ItemsSource = Array.Empty<ScorecardRowItem>();
            ClauseCount = 0;
            EmptyClausesText.IsVisible = true;
            NoticesCard.IsVisible = false;
            return;
        }

        StaleBanner.IsVisible = scorecard.IsStale;

        if (scorecard.Comparison is { } cmp)
        {
            ComparisonCard.IsVisible = true;
            ComparisonTitleText.Text = $"Tier-0 comparison: {cmp.BaselineId} → {cmp.CandidateId}";
            string ldPart = cmp.DeltaLdPercent is { } dld
                ? string.Create(Inv, $" · ΔWing-only L/D {dld:+0.0;-0.0;0.0}%")
                : "";
            string toPart = cmp.DeltaTakeoffKn is { } dto
                ? string.Create(Inv, $" · ΔTakeoff {dto:+0.0;-0.0;0.0} kn")
                : "";
            ComparisonSummaryText.Text = string.Create(Inv,
                $"ΔSpan {cmp.DeltaSpanMm:+0.0;-0.0;0.0} mm · ΔArea {cmp.DeltaAreaCm2:+0.0;-0.0;0.0} cm² · Roll inertia {cmp.RollInertiaRatio:0.00}× ({cmp.DeltaRollInertiaPercent:+0.0;-0.0;0.0}%){ldPart}{toPart}");
        }
        else
        {
            ComparisonCard.IsVisible = false;
        }

        var rows = scorecard.Clauses
            .Select(c => new ScorecardRowItem(c.Label, c.TargetDisplay, c.ActualDisplay, c.Status, c.MethodTier, c.Notice))
            .ToArray();
        ClausesList.ItemsSource = rows;
        ClauseCount = rows.Length;
        EmptyClausesText.IsVisible = rows.Length == 0;

        if (scorecard.MandatoryNotices.Count > 0)
        {
            NoticesCard.IsVisible = true;
            NoticesText.Text = string.Join("\n", scorecard.MandatoryNotices);
        }
        else
        {
            NoticesCard.IsVisible = false;
        }
    }

    public async Task<bool> CopyFoilReqAsync()
    {
        string text = FoilReqDslBox.Text ?? "";
        if (string.IsNullOrEmpty(text))
        {
            LastCopySucceeded = false;
            return false;
        }

        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(text);
            LastCopySucceeded = true;
            CopyReceiptText.Text = "Copied FoilReq 1.0";
            CopyReceiptText.IsVisible = true;
            return true;
        }
        catch
        {
            LastCopySucceeded = false;
            CopyReceiptText.Text = "Clipboard unavailable";
            CopyReceiptText.IsVisible = true;
            return false;
        }
    }
}
