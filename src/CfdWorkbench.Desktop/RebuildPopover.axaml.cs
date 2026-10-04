using Avalonia.Controls;
using Avalonia.Input;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Diagnostics;
using System.Globalization;

namespace CfdWorkbench.Desktop;

/// <summary>One measured, read-only set of seven Core previews. Only Apply writes an accepted row.</summary>
public partial class RebuildPopover : UserControl
{
    private WorkbenchController? controller;
    private string? curve;
    private IReadOnlyList<RebuildPreview> previews = [];
    private Control? focusReturn;
    private bool closing;
    private readonly TextBlock[] readoutValues = new TextBlock[7];

    public event Action<RebuildPreview?>? PreviewChanged;
    public int Count { get; private set; } = 4;
    public RebuildPreview? Preview => previews.FirstOrDefault(item => item.Count == Count);
    public string ReadoutText => string.Join(" ", ReadoutGrid.Children.OfType<TextBlock>().Select(item => item.Text));
    public string? FieldError => ErrorText.IsVisible ? ErrorText.Text : null;

    public RebuildPopover()
    {
        InitializeComponent();
        string[] labels = ["Points", "Largest change", "Curvature breaks", "Kept exactly", "Tip direction", "Area", "Anchors"];
        for (int row = 0; row < labels.Length; row++)
        {
            ReadoutGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = labels[row], VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            var value = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Right };
            Grid.SetRow(label, row);
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            ReadoutGrid.Children.Add(label);
            ReadoutGrid.Children.Add(value);
            readoutValues[row] = value;
            if (row == 1) value.Classes.Add("change");
        }
        FewerButton.Click += (_, _) => SetCount(Count - 1);
        MoreButton.Click += (_, _) => SetCount(Count + 1);
        CancelButton.Click += (_, _) => Close(false);
        ApplyButton.Click += async (_, _) => await ApplyAsync();
        CountInput.LostFocus += (_, _) => ReadCount();
        CountInput.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Escape) { args.Handled = true; Close(false); }
            else if (args.Key is Key.Up or Key.Down)
            {
                args.Handled = true;
                SetCount(Count + (args.Key == Key.Up ? 1 : -1));
            }
            else if (args.Key is Key.Return or Key.Enter)
            {
                args.Handled = true;
                if (ReadCount()) await ApplyAsync();
            }
        };
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key == Key.Escape) { args.Handled = true; Close(false); }
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    public void Open(WorkbenchController owner, string target, Control? returnTo)
    {
        if (IsVisible) Close(false);
        controller = owner;
        curve = target;
        focusReturn = returnTo;
        closing = false;
        TitleText.Text = "Rebuild " + PropertiesView.Curves[target].Name.ToLowerInvariant();
        Avalonia.Automation.AutomationProperties.SetName(this, TitleText.Text);
        var watch = Stopwatch.StartNew();
        previews = owner.PreviewRebuilds(target);
        ShellEvents.Record("rebuild.preview", "OK", watch.Elapsed.TotalMilliseconds,
            System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
            curveFamily: Channels.Family(target));
        Count = 4;
        CountInput.Text = "4";
        IsVisible = true;
        Refresh();
        CountInput.Focus();
        CountInput.SelectAll();
    }

    public void SetCount(int count)
    {
        if (count is < 4 or > 10) { ShowError("Enter a whole number from 4 to 10."); return; }
        Count = count;
        CountInput.Text = count.ToString(CultureInfo.InvariantCulture);
        Refresh();
    }

    private bool ReadCount()
    {
        if (!int.TryParse(CountInput.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count is < 4 or > 10)
        { ShowError("Enter a whole number from 4 to 10."); return false; }
        SetCount(count);
        return true;
    }

    private void ShowError(string reason)
    {
        ErrorText.Text = reason;
        ErrorText.IsVisible = true;
        ApplyButton.IsEnabled = false;
    }

    private void Refresh()
    {
        if (controller is null || curve is null || Preview is not { } preview) return;
        var current = controller.CurveFor(curve);
        string name = PropertiesView.Curves[curve].Name.ToLowerInvariant();
        double halfSpan = controller.Planform?.HalfSpanMeters ?? 0;
        string unit = PropertiesView.Curves[curve].ValueUnit;
        string along = PropertiesView.Curves[curve].ValueLabel.ToLowerInvariant();   // COPY-207: "126.74 mm aft"
        double scale = PropertiesView.FieldScale[PropertiesView.Curves[curve].ValueFamily];
        string anchors = current is null ? "none to remove" : string.Join(" ", current.Points
            .Where(item => item.Role == PointRole.Anchor)
            .Select(item => $"Anchor at point {item.Index + 1} becomes a control point."));
        if (anchors.Length == 0) anchors = "none to remove";
        readoutValues[0].Text = $"{current?.Points.Count ?? 0} → {Count}";
        readoutValues[1].Text = $"{preview.MaxChange * scale:0.00} {unit} at {preview.AtEta * halfSpan * 1000:0.00} mm from root";
        readoutValues[2].Text = $"{preview.BreaksBefore} → {preview.BreaksAfter} (C² throughout)";
        readoutValues[3].Text = $"root end {preview.Curve.Points[0].Ordinate * scale:0.00} {unit} {along}, tangent square to the centre line; " +
            $"tip end {preview.Curve.Points[^1].Ordinate * scale:0.00} {unit} {along}";
        readoutValues[4].Text = $"changed by {preview.TipTurnDegrees:0.00}°";
        readoutValues[5].Text = $"{preview.AreaBeforeSquareMeters * 10000:0.00} → {preview.AreaAfterSquareMeters * 10000:0.00} cm²";
        readoutValues[6].Text = anchors;
        ErrorText.Text = preview.Refusal;
        ErrorText.IsVisible = preview.Refusal is not null;
        ApplyButton.IsEnabled = preview.Refusal is null;
        FewerButton.IsEnabled = Count > 4;
        MoreButton.IsEnabled = Count < 10;
        PreviewChanged?.Invoke(preview);
        controller.ReportPointInfo($"Rebuild {name}: {current?.Points.Count ?? 0} → {Count} points. " +
            $"Largest change {preview.MaxChange * scale:0.00} {unit} at {preview.AtEta * halfSpan * 1000:0.00} mm from root.");
    }

    public async Task ApplyAsync()
    {
        if (!ReadCount() || controller is null || curve is null || Preview?.Refusal is not null) return;
        var outcome = await controller.ApplyPointCommandAsync(new PointCommand.RebuildCurve(curve, Count));
        if (outcome is CommitOutcome.Committed) Close(true);
        else if (outcome is CommitOutcome.Refused refused) ShowError(refused.Copy);
    }

    public void Close(bool applied)
    {
        if (closing || !IsVisible || curve is null) return;
        closing = true;
        IsVisible = false;
        PreviewChanged?.Invoke(null);
        ShellEvents.Record("rebuild.close", applied ? "applied" : "cancelled", 0,
            System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
            curveFamily: Channels.Family(curve), pointsAfter: applied ? Count : controller?.CurveFor(curve)?.Points.Count);
        if (!applied) controller?.ReportPointInfo($"Rebuild cancelled. The {PropertiesView.Curves[curve].Name.ToLowerInvariant()} is as it was.");
        focusReturn?.Focus();
        curve = null;
    }
}
