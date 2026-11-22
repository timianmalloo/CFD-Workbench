using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// Find operating α (DR-DXM-6): target CL and a bracket in, the found α and its basis out. Find records no run. Apply hands α to
/// the caller (the conditions band writes its α box); Cancel and Close leave every condition alone. Evaluate stays explicit.
/// </summary>
public sealed class FindAlphaDialog : Window
{
    private static readonly FontFeatureCollection TabularFigures = [FontFeature.Parse("+tnum"), FontFeature.Parse("+lnum")];
    private readonly WorkbenchController controller;
    private readonly Action<double> applied;
    private CancellationTokenSource? running;

    public TextBox TargetInput { get; } = new() { Name = "FindTargetInput", Text = "0.40", Width = 70, FontFeatures = TabularFigures };
    public TextBox LowerInput { get; } = new() { Name = "FindLowerInput", Text = "-2", Width = 70, FontFeatures = TabularFigures };
    public TextBox UpperInput { get; } = new() { Name = "FindUpperInput", Text = "8", Width = 70, FontFeatures = TabularFigures };
    public Button FindButton { get; } = new() { Name = "FindRunButton", Content = "Find α", MinHeight = 24, IsDefault = true, Classes = { "primary" } };
    public Button ApplyButton { get; } = new() { Name = "FindApplyButton", Content = "Apply", MinHeight = 24, IsEnabled = false };
    public Button CancelButton { get; } = new() { Name = "FindCancelButton", Content = "Cancel", MinHeight = 24, IsCancel = true };
    public TextBlock Sentence { get; } = new() { Name = "FindSentence", TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, FontFeatures = TabularFigures };
    public StackPanel ResultRows { get; } = new() { Name = "FindResultRows", Spacing = 4 };
    public FindAlphaOutcome? Outcome { get; private set; }

    public FindAlphaDialog(WorkbenchController controller, double speedMetersPerSecond, Action<double> applied, Units units = Units.Metric)
    {
        this.controller = controller;
        this.applied = applied;
        Title = "Find operating α";
        Width = 480;
        MinHeight = 280;
        SizeToContent = SizeToContent.Height;
        MaxHeight = ResolveDouble("DialogMaxHeight", 620);
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var fontFamily = CanvasTypography.ResolveFontFamily(this);
        double fontSize = CanvasTypography.ResolveFontSize(this);
        FontFamily = fontFamily;
        FontSize = fontSize;

        AutomationProperties.SetName(this, "Find operating α");
        AutomationProperties.SetName(TargetInput, "Target CL");
        AutomationProperties.SetName(LowerInput, "Bracket lower α in degrees");
        AutomationProperties.SetName(UpperInput, "Bracket upper α in degrees");
        AutomationProperties.SetLiveSetting(Sentence, AutomationLiveSetting.Polite);
        Opened += (_, _) => TargetInput.Focus();

        Thickness padding = ResolveThickness("DialogPadding", new Thickness(16));
        double spacing = ResolveDouble("DialogSpacing", 12);

        var fields = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (Control item in new Control[]
        {
            new TextBlock { Text = "Target CL", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 4) },
            WithMargin(TargetInput, new Thickness(0, 0, 10, 4)),
            new TextBlock { Text = "α from", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 4) },
            WithMargin(LowerInput, new Thickness(0, 0, 6, 4)),
            new TextBlock { Text = "to", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 4) },
            WithMargin(UpperInput, new Thickness(0, 0, 4, 4)),
            new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) }
        })
        {
            fields.Children.Add(item);
        }

        string speedDisplay = Labels.Speed(speedMetersPerSecond, units).ToString("0.##", CultureInfo.InvariantCulture) + " " + Labels.SpeedUnit(units);
        var speedBanner = new TextBlock
        {
            Name = "FindSpeedBanner",
            Text = "Speed from the band: " + speedDisplay,
            Classes = { "pnl-note" },
            FontFeatures = TabularFigures,
            TextWrapping = TextWrapping.Wrap
        };

        var bodyStack = new StackPanel
        {
            Spacing = spacing,
            Children = { speedBanner, fields, Sentence, ResultRows }
        };

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = bodyStack
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, spacing, 0, 0),
            Children = { FindButton, ApplyButton, CancelButton }
        };
        var root = new Grid
        {
            Margin = padding,
            RowDefinitions = new RowDefinitions("*,Auto")
        };
        Grid.SetRow(scroller, 0);
        Grid.SetRow(buttons, 1);
        root.Children.Add(scroller);
        root.Children.Add(buttons);
        Content = root;

        FindButton.Click += async (_, _) => await FindAsync();
        ApplyButton.Click += (_, _) => { if (Outcome?.Alpha is { } alpha) { applied(alpha); Close(); } };
        CancelButton.Click += (_, _) => { running?.Cancel(); Close(); };
    }

    private static T WithMargin<T>(T control, Thickness margin) where T : Control
    {
        control.Margin = margin;
        return control;
    }

    private static double ResolveDouble(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var val) == true && val is double d ? d : fallback;

    private static Thickness ResolveThickness(string key, Thickness fallback) =>
        Application.Current?.TryFindResource(key, out var val) == true && val is Thickness t ? t : fallback;

    /// <summary>Runs the search for the typed target and bracket. A typed value that is not a number says so and runs nothing.</summary>
    public async Task FindAsync()
    {
        if (!double.TryParse(TargetInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double target) ||
            !double.TryParse(LowerInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lower) ||
            !double.TryParse(UpperInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double upper) || lower >= upper)
        {
            Sentence.Text = Labels.FindNeedsInput;
            return;
        }
        running?.Cancel();
        running = new CancellationTokenSource();
        FindButton.IsEnabled = false;
        ApplyButton.IsEnabled = false;
        Sentence.Text = Labels.FindRunning;
        try
        {
            Outcome = await controller.FindAlphaAsync(target, lower, upper, running.Token);
            Sentence.Text = Outcome.Sentence;
            ResultRows.Children.Clear();
            foreach (ResultRow row in Outcome.Rows)
            {
                string value = string.IsNullOrEmpty(row.Unit) ? row.Value : row.Value + " " + row.Unit;
                ResultRows.Children.Add(new TextBlock
                {
                    Text = row.Label + ": " + value,
                    Classes = { "pnl-cell" },
                    TextWrapping = TextWrapping.Wrap
                });
            }
            ApplyButton.IsEnabled = Outcome.Alpha is not null;
        }
        catch (OperationCanceledException) { Sentence.Text = ""; }
        finally { FindButton.IsEnabled = true; }
    }
}
