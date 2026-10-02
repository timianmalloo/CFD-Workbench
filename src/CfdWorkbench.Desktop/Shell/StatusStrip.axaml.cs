using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace CfdWorkbench.Desktop.Shell;

/// <summary>
/// The status strip along the bottom of the shell (DR-STATUS-1; DESIGN.md §4 Status strip). It shows the last report only:
/// each report replaces the one before. <see cref="ShellHost.Report"/> is the one way in; the strip itself keeps no rule
/// about which report wins (STATUS-CLOBBER is the shell's sequence check).
/// </summary>
public partial class StatusStrip : UserControl
{
    // 12 px icons by kind (docs/mockups/status-bar.html V2): a check, a triangle, a circled cross.
    private const string IconInfo = "M2,6.5 L5,9 L10,3";
    internal const string IconWarning = "M6,1 L11.5,11 L0.5,11 Z M6,4.5 L6,7.5 M6,9 L6,9.8";
    private const string IconError = "M1,6 A5,5 0 1 1 11,6 A5,5 0 1 1 1,6 Z M4,4 L8,8 M8,4 L4,8";

    // The scaled copies of the prop tokens the strip draws with (DN-5), as the Properties pane does.
    private static readonly string[] ScaledTokens = ["PropFontSize", "PropLineHeight"];

    public StatusStrip()
    {
        InitializeComponent();
        SizeChanged += (_, _) => FitItems();
    }

    /// <summary>The kind of the report the strip shows.</summary>
    public ReportKind Kind { get; private set; }

    /// <summary>The full text of the report the strip shows; empty until something happens (DR-STATUS-4).</summary>
    public string Text => StatusText.Text ?? "";

    /// <summary>Replaces the shown report. <paramref name="offerTryAgain"/> shows the one action after the message.</summary>
    public void Show(StatusReport report, bool offerTryAgain = false)
    {
        Kind = report.Kind;
        StatusText.Text = report.Text;
        // Nothing is lost when the strip trims: the full text is the tooltip and the accessible name.
        ToolTip.SetTip(StatusText, report.Text);
        AutomationProperties.SetName(StatusText, report.Text);
        foreach (var control in new Control[] { StatusText, StatusKindIcon, StatusRail })
        {
            control.Classes.Set("warning", report.Kind == ReportKind.Warning);
            control.Classes.Set("error", report.Kind == ReportKind.Error);
        }
        StatusKindIcon.Data = Geometry.Parse(report.Kind switch
        {
            ReportKind.Warning => IconWarning,
            ReportKind.Error => IconError,
            _ => IconInfo
        });
        StatusKindIcon.IsVisible = report.Text.Length > 0;
        StatusTryAgainButton.IsVisible = offerTryAgain;
    }

    /// <summary>Empties the strip (the recent-files list was cleared after all).</summary>
    public void Clear()
    {
        Show(new StatusReport(""));
        ToolTip.SetTip(StatusText, null);
    }

    /// <summary>The read-only items: selection, units, the estimate note and Text size (DESIGN.md §4).</summary>
    public void ShowItems(string? selection, bool foilOpen, bool estimates, double textScale)
    {
        SelectionItemText.Text = selection ?? "";
        AutomationProperties.SetName(SelectionItem, selection is null ? null : "Selection: " + selection);
        string size = string.Create(CultureInfo.InvariantCulture, $"Text {textScale * 100:0} %");
        if (TextSizeItemText.Text != size) TextSizeItemText.Text = size;
        wanted = (selection is not null, foilOpen, estimates && foilOpen, true);
        FitItems();
    }

    private (bool Selection, bool Units, bool Estimates, bool TextSize) wanted = (false, false, false, true);

    /// <summary>
    /// The strip is one line: when the items would crowd the message (large Text size, narrow window) they drop in the order
    /// Text size, ≈ estimates, units; the selection stays.
    /// </summary>
    private void FitItems()
    {
        SelectionItem.IsVisible = wanted.Selection;
        UnitsItem.IsVisible = wanted.Units;
        EstimateItem.IsVisible = wanted.Estimates;
        TextSizeItem.IsVisible = wanted.TextSize;
        double width = Bounds.Width;
        if (width <= 0) return;
        foreach (var item in new Control[] { TextSizeItem, EstimateItem, UnitsItem })
        {
            if (ItemsWidth() <= width / 2) break;
            item.IsVisible = false;
        }
    }

    private double ItemsWidth()
    {
        double total = 0;
        foreach (var item in StatusItems.Children.Where(child => child.IsVisible))
        {
            item.Measure(Size.Infinity);
            total += item.DesiredSize.Width;
        }
        return total;
    }

    /// <summary>DN-5: the strip's 11 px type scales with Text size, as the Properties pane's does.</summary>
    public void ApplyTextScale(double scale)
    {
        foreach (var key in ScaledTokens)
            if (Application.Current?.TryFindResource(key, out var value) == true && value is double size)
                StripRoot.Resources[key] = size * scale;
        FitItems();
    }
}
