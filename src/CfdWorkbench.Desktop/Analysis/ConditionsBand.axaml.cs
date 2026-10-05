using System.Globalization;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>The Custom operating point used by the GUI and CLI is built by OperatingPoints.Custom.</summary>
public partial class ConditionsBand : UserControl
{
    private const double CompactWidth = 1100;

    public ConditionsBand()
    {
        InitializeComponent();
        EvaluateButton.Click += (_, _) => Activate();
        SpeedInput.PropertyChanged += (_, args) => { if (args.Property == TextBox.TextProperty) RefreshDerived(); };
        DepthInput.PropertyChanged += (_, args) => { if (args.Property == TextBox.TextProperty) RefreshDerived(); };
        AlphaInput.PropertyChanged += (_, args) => { if (args.Property == TextBox.TextProperty) RefreshDerived(); };
        WaterInput.SelectionChanged += (_, _) => RefreshDerived();
        SizeChanged += (_, _) => SetAvailableWidth(Bounds.Width);
    }

    public event Action<OperatingPoint, WaterRecord>? EvaluateRequested;
    public event Action? CancelRequested;
    public double? ReferenceChordMeters { get; set; }
    public bool Running { get; private set; }

    /// <summary>One builder for GUI and CLI run-key inputs (CLI-01).</summary>
    public OperatingPoint BuildOperatingPoint()
    {
        double speed = Parse(SpeedInput.Text, "ANA-INPUT-SPEED");
        double alpha = Parse(AlphaInput.Text, "ANA-INPUT-ALPHA");
        double? depth = string.IsNullOrWhiteSpace(DepthInput.Text) ? null : Parse(DepthInput.Text, "ANA-INPUT-DEPTH");
        var op = OperatingPoints.Custom(speed, alpha + 1, depth);
        OperatingPoints.Validate(op);
        return op;
    }

    public WaterRecord BuildWater() => WaterTable.At(OperatingPoints.DefaultTemperatureC,
        WaterInput.SelectedIndex == 1 ? 0 : OperatingPoints.SaltSalinityGPerKg);

    public void ShowRunState(RunState state)
    {
        Running = state == RunState.Running;
        EvaluateButton.Content = Running ? "Cancel" : "Evaluate";
        Avalonia.Automation.AutomationProperties.SetName(EvaluateButton, Running ? "Cancel evaluation" : "Evaluate");
    }

    public void SetAvailableWidth(double width)
    {
        bool compact = width > 0 && width <= CompactWidth;
        WideDerived.IsVisible = !compact;
        MoreButton.IsVisible = compact;
        DerivedRe.IsVisible = DerivedDepth.IsVisible = DerivedFroude.IsVisible = !compact;
    }

    public void RefreshDerived()
    {
        if (ReferenceChordMeters is not > 0) return;
        try
        {
            var derived = OperatingPoints.Derive(BuildOperatingPoint(), BuildWater(), ReferenceChordMeters.Value);
            Set(DerivedQ, "q", derived.Q, "Pa");
            Set(DerivedRe, "Re_ref", derived.ReRef);
            Set(DerivedDepth, "h/c", derived.DepthOverChord);
            Set(DerivedFroude, "Fr_h", derived.FroudeDepth);
            Set(DerivedSigma, "σ", derived.Sigma);
            MoreRe.Header = DerivedRe.Text;
            MoreDepth.Header = DerivedDepth.Text;
            MoreFroude.Header = DerivedFroude.Text;
            InputError.IsVisible = false;
        }
        catch (ContractError error)
        {
            InputError.Text = error.Code;
            InputError.IsVisible = true;
        }
    }

    private void Activate()
    {
        if (Running) { CancelRequested?.Invoke(); return; }
        try
        {
            var op = BuildOperatingPoint();
            var water = BuildWater();
            InputError.IsVisible = false;
            EvaluateRequested?.Invoke(op, water);
        }
        catch (ContractError error)
        {
            InputError.Text = error.Code;
            InputError.IsVisible = true;
        }
    }

    private static double Parse(string? text, string code) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value : throw new ContractError(code, "the input is not a finite number");

    private static void Set(TextBlock cell, string name, DerivedValue value, string unit = "") =>
        cell.Text = name + " " + (value.Value is double number
            ? number.ToString("0.###", CultureInfo.InvariantCulture) + (unit.Length > 0 ? " " + unit : "")
            : value.Reason == DerivedReason.DepthNotSet ? "Unavailable — depth not set" : "Undefined — speed ≤ 0");
}
