using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// Find operating α (DR-DXM-6): target CL and a bracket in, the found α and its basis out. Find records no run. Apply hands α to
/// the caller (the conditions band writes its α box); Cancel and Close leave every condition alone. Evaluate stays explicit.
/// </summary>
public sealed class FindAlphaDialog : Window
{
    private readonly WorkbenchController controller;
    private readonly Action<double> applied;
    private CancellationTokenSource? running;

    public TextBox TargetInput { get; } = new() { Name = "FindTargetInput", Text = "0.40", Width = 70 };
    public TextBox LowerInput { get; } = new() { Name = "FindLowerInput", Text = "-2", Width = 70 };
    public TextBox UpperInput { get; } = new() { Name = "FindUpperInput", Text = "8", Width = 70 };
    public Button FindButton { get; } = new() { Name = "FindRunButton", Content = "Find α", MinHeight = 24 };
    public Button ApplyButton { get; } = new() { Name = "FindApplyButton", Content = "Apply", MinHeight = 24, IsEnabled = false };
    public Button CancelButton { get; } = new() { Name = "FindCancelButton", Content = "Cancel", MinHeight = 24 };
    public TextBlock Sentence { get; } = new() { Name = "FindSentence", TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontWeight = Avalonia.Media.FontWeight.SemiBold };
    public StackPanel ResultRows { get; } = new() { Name = "FindResultRows" };
    public FindAlphaOutcome? Outcome { get; private set; }

    public FindAlphaDialog(WorkbenchController controller, double speedMetersPerSecond, Action<double> applied)
    {
        this.controller = controller;
        this.applied = applied;
        Title = "Find operating α";
        Width = 440; Height = 380;
        AutomationProperties.SetName(this, "Find operating α");
        AutomationProperties.SetName(TargetInput, "Target CL");
        AutomationProperties.SetName(LowerInput, "Bracket lower α in degrees");
        AutomationProperties.SetName(UpperInput, "Bracket upper α in degrees");
        var fields = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        fields.Children.Add(new TextBlock { Text = "Target CL", VerticalAlignment = VerticalAlignment.Center });
        fields.Children.Add(TargetInput);
        fields.Children.Add(new TextBlock { Text = "α from", VerticalAlignment = VerticalAlignment.Center });
        fields.Children.Add(LowerInput);
        fields.Children.Add(new TextBlock { Text = "to", VerticalAlignment = VerticalAlignment.Center });
        fields.Children.Add(UpperInput);
        fields.Children.Add(new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        buttons.Children.Add(FindButton);
        buttons.Children.Add(ApplyButton);
        buttons.Children.Add(CancelButton);
        Content = new StackPanel
        {
            Margin = new Thickness(12), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Speed from the band: " + speedMetersPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + " m/s", Opacity = 0.8 },
                fields, buttons, Sentence, ResultRows
            }
        };
        FindButton.Click += async (_, _) => await FindAsync();
        ApplyButton.Click += (_, _) => { if (Outcome?.Alpha is { } alpha) { applied(alpha); Close(); } };
        CancelButton.Click += (_, _) => { running?.Cancel(); Close(); };
    }

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
                ResultRows.Children.Add(new TextBlock { Text = row.Label + ": " + row.Value, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            ApplyButton.IsEnabled = Outcome.Alpha is not null;
        }
        catch (OperationCanceledException) { Sentence.Text = ""; }
        finally { FindButton.IsEnabled = true; }
    }
}
