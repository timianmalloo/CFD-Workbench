using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
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
        FindAlphaButton.Click += (_, _) => OpenFindAlpha();
        SpeedInput.PropertyChanged += (_, args) => { if (args.Property == TextBox.TextProperty) RefreshDerived(); };
        DepthInput.PropertyChanged += (_, args) => { if (args.Property == TextBox.TextProperty) RefreshDerived(); };
        AlphaInput.PropertyChanged += (_, args) => { if (args.Property == TextBox.TextProperty) RefreshDerived(); };
        WaterInput.SelectionChanged += (_, _) => RefreshDerived();
        SizeChanged += (_, args) => { if (args.WidthChanged) SetAvailableWidth(Bounds.Width); };
    }

    public event Action<OperatingPoint, WaterRecord>? EvaluateRequested;
    public event Action? CancelRequested;
    public double? ReferenceChordMeters { get; set; }

    private Units units = Units.Metric;
    private double? convertedSpeed;       // m/s behind SpeedInput while the box still shows the text a unit change wrote
    private string? convertedText;

    /// <summary>The display units (Ruling 101 3d): the speed box and its unit read kn in Imperial and m/s in Metric, as the results do.</summary>
    public Units Units
    {
        get => units;
        set
        {
            if (units == value) return;
            double? metersPerSecond = TryBuildSpeed();
            units = value;
            SpeedUnit.Text = SpeedUnitText(value);
            Avalonia.Automation.AutomationProperties.SetName(SpeedInput,
                value == Units.Imperial ? "Speed in knots" : "Speed in metres per second");
            if (metersPerSecond is { } speed)
            {
                convertedSpeed = speed;
                convertedText = Labels.Number(value == Units.Imperial ? speed * Labels.KnotsPerMeterSecond : speed, "0.##");
                SpeedInput.Text = convertedText;
            }
            RefreshDerived();
        }
    }

    /// <summary>The dialog this band last opened (null before one), for the shell's checks.</summary>
    public FindAlphaDialog? FindDialog { get; private set; }

    private void OpenFindAlpha()
    {
        if (this.FindAncestorOfType<Shell.ShellHost>()?.Controller is not { } controller || TryBuildSpeed() is not { } speed) return;
        FindDialog = new FindAlphaDialog(controller, speed, alpha =>
        {
            AlphaInput.Text = Labels.Number(alpha, "0.00");
            controller.ApplyFoundAlpha(alpha);   // the pending α only; Evaluate stays explicit
        });
        if (TopLevel.GetTopLevel(this) is Window owner) FindDialog.Show(owner);
        else FindDialog.Show();
    }

    public static string SpeedUnitText(Units value) => value == Units.Imperial ? "kn" : "m/s";

    private double? TryBuildSpeed()
    {
        try { return BuildSpeedMetersPerSecond(); }
        catch (ContractError) { return null; }
    }

    private double BuildSpeedMetersPerSecond()
    {
        if (convertedSpeed is { } kept && SpeedInput.Text == convertedText) return kept;
        double typed = Parse(SpeedInput.Text, "ANA-INPUT-SPEED");
        return units == Units.Imperial ? typed / Labels.KnotsPerMeterSecond : typed;
    }
    public bool Running { get; private set; }

    /// <summary>One builder for GUI and CLI run-key inputs (CLI-01).</summary>
    public OperatingPoint BuildOperatingPoint()
    {
        double speed = BuildSpeedMetersPerSecond();
        double alpha = Parse(AlphaInput.Text, "ANA-INPUT-ALPHA");
        double? depth = string.IsNullOrWhiteSpace(DepthInput.Text) ? null : Parse(DepthInput.Text, "ANA-INPUT-DEPTH");
        var op = OperatingPoints.Custom(speed, alpha, depth);
        OperatingPoints.Validate(op);
        return op;
    }

    public WaterRecord BuildWater() => WaterTable.At(OperatingPoints.DefaultTemperatureC,
        WaterInput.SelectedIndex == 1 ? 0 : OperatingPoints.SaltSalinityGPerKg);

    public void ShowRunState(RunState state)
    {
        Running = state == RunState.Running;
        EvaluateButton.Content = Running ? "Cancel" : "Evaluate";
        EvaluateButton.Classes.Set("primary", !Running);
        EvaluateButton.Classes.Set("outline", Running);
        Avalonia.Automation.AutomationProperties.SetName(EvaluateButton, Running ? "Cancel evaluation" : "Evaluate");
    }

    /// <summary>
    /// Re, h/c and Fr_h move under More below 1100 px, and also wherever the band's own content (the longer
    /// "Unavailable — depth not set" cells) would run past the right edge (Ruling 101 Q4).
    /// </summary>
    public void SetAvailableWidth(double width)
    {
        availableWidth = width;
        bool compact = width > 0 && (width <= CompactWidth || !WideContentFits(width));
        WideDerived.IsVisible = !compact;
        MoreButton.IsVisible = compact;
        DerivedRe.IsVisible = DerivedDepth.IsVisible = DerivedFroude.IsVisible = !compact;
    }

    private double availableWidth;

    private bool WideContentFits(double width)
    {
        WideDerived.IsVisible = true;
        DerivedRe.IsVisible = DerivedDepth.IsVisible = DerivedFroude.IsVisible = true;
        MoreButton.IsVisible = false;
        BandRow.Measure(Size.Infinity);
        double room = width - (BandRow.Parent is Border border ? border.Padding.Left + border.Padding.Right : 0);
        return BandRow.DesiredSize.Width <= room;
    }

    public void RefreshDerived()
    {
        if (ReferenceChordMeters is not > 0) return;
        try
        {
            var derived = OperatingPoints.Derive(BuildOperatingPoint(), BuildWater(), ReferenceChordMeters.Value);
            Set(DerivedQ, "q", derived.Q, "Pa");
            Set(DerivedRe, "Re_ref", derived.ReRef, format: "0.###E+0");
            Set(DerivedDepth, "h/c", derived.DepthOverChord);
            Set(DerivedFroude, "Fr_h", derived.FroudeDepth);
            Set(DerivedSigma, "σ", derived.Sigma);
            MoreRe.Header = DerivedRe.Text;
            MoreDepth.Header = DerivedDepth.Text;
            MoreFroude.Header = DerivedFroude.Text;
            InputError.IsVisible = false;
            if (availableWidth > 0) SetAvailableWidth(availableWidth);   // the cells' text changed width
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

    private static void Set(TextBlock cell, string name, DerivedValue value, string unit = "", string format = "0.###") =>
        cell.Text = name + " " + (value.Value is double number
            ? number.ToString(format, CultureInfo.InvariantCulture) + (unit.Length > 0 ? " " + unit : "")
            : value.Reason == DerivedReason.DepthNotSet ? "Unavailable — depth not set" : "Undefined — speed ≤ 0");
}
