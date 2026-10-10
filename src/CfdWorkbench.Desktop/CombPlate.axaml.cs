using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace CfdWorkbench.Desktop;

/// <summary>
/// The rail comb's plate (docs/design/rail-comb.md sections 3 and 6): scale and density steppers, the pieces lines and the
/// threshold. Shown while Curvature is on in CAD. At narrow width it is a one-line summary button, and the open steppers sit in
/// flow between the viewport and the Tracing strip (a disclosure, Escape closes), never over the planform.
/// </summary>
public partial class CombPlate : UserControl
{
    public const double NarrowWidth = 560;
    private const int FlashMilliseconds = 900;

    private WorkbenchController? controller;
    private PlanCanvas? canvas;
    private bool narrow, open;
    private int announcementsSeen;
    private DispatcherTimer? flash;

    /// <summary>The in-flow row beneath the viewport where the open narrow card sits.</summary>
    public Border? DisclosureHost { get; set; }

    /// <summary>The last control's Tab leaves for Properties (AM-RC-4).</summary>
    public Func<bool>? TabOut { get; set; }

    /// <summary>How many one-shot emphasis timers have run: a refit emphasises the legend once, and not at all under reduced motion.</summary>
    public int FlashTimers { get; private set; }

    public bool IsNarrow => narrow;
    public bool IsOpen => open;
    public string LiveAnnouncement => LiveText.Text ?? "";
    public bool LegendEmphasised => LegendText.Classes.Contains("emphasis");

    public CombPlate()
    {
        InitializeComponent();
        SmallerButton.Click += (_, _) => controller?.StepCombScale(larger: false);
        LargerButton.Click += (_, _) => controller?.StepCombScale(larger: true);
        AutoButton.Click += (_, _) => { controller?.SetCombAuto(); Refresh(); };
        SparserButton.Click += (_, _) => controller?.StepCombDensity(denser: false);
        DenserButton.Click += (_, _) => controller?.StepCombDensity(denser: true);
        SummaryButton.Click += (_, _) => { open = !open; ApplyMode(); };
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Card.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    public WorkbenchController? Controller
    {
        get => controller;
        set
        {
            if (ReferenceEquals(controller, value)) return;
            if (controller is not null) controller.CombChanged -= OnChanged;
            controller = value;
            if (controller is not null) controller.CombChanged += OnChanged;
            announcementsSeen = controller?.CombAnnouncements ?? 0;
            Refresh();
        }
    }

    public PlanCanvas? Canvas
    {
        get => canvas;
        set
        {
            if (ReferenceEquals(canvas, value)) return;
            if (canvas is not null) canvas.CombStatsChanged -= Refresh;
            canvas = value;
            if (canvas is not null) canvas.CombStatsChanged += Refresh;
        }
    }

    private void OnChanged()
    {
        if (Dispatcher.UIThread.CheckAccess()) Refresh();
        else Dispatcher.UIThread.Post(Refresh);
    }

    /// <summary>Width of the Plan viewport: at or under 560 px the plate is the summary button.</summary>
    public void SetWidth(double width)
    {
        bool next = width <= NarrowWidth;
        if (next == narrow) return;
        narrow = next;
        open = false;
        ApplyMode();
    }

    private void ApplyMode()
    {
        bool inDisclosure = Card.Parent == DisclosureHost && DisclosureHost is not null;
        if (inDisclosure) DisclosureHost!.Child = null;
        else if (Card.Parent is Panel parent) parent.Children.Remove(Card);
        SummaryButton.IsVisible = narrow;
        if (!narrow) Root.Children.Insert(1, Card);
        else if (open && DisclosureHost is not null) DisclosureHost.Child = Card;
        if (DisclosureHost is not null) DisclosureHost.IsVisible = narrow && open && controller is { CombVisible: true };
    }

    /// <summary>The plate's own size at this width, whatever margin it is placed with.</summary>
    public Size PreferredSize(double width)
    {
        Root.Measure(new Size(width, double.PositiveInfinity));
        return Root.DesiredSize;
    }

    public bool FocusFirst()
    {
        if (!IsVisible || !IsEffectivelyVisible) return false;
        return (narrow ? SummaryButton : SmallerButton).Focus();
    }

    private Control LastControl => narrow && !open ? SummaryButton : DenserButton;

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && narrow && open)
        {
            open = false;
            ApplyMode();
            SummaryButton.Focus();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Shift) && ReferenceEquals(e.Source, LastControl) && TabOut?.Invoke() == true)
            e.Handled = true;
    }

    private static void Limit(Button button, bool available, string limit, string pair)
    {
        button.Classes.Set("limit", !available);
        string help = available ? "" : $"{limit}. {pair} is available.";
        AutomationProperties.SetHelpText(button, help);
        ToolTip.SetTip(button, available ? null : help);
    }

    /// <summary>The plate's content or visibility changed: its size may have, so its place is worked out again.</summary>
    public event Action? Refreshed;

    public void Refresh()
    {
        Update();
        Refreshed?.Invoke();
    }

    private void Update()
    {
        if (controller is null) { IsVisible = false; return; }
        bool shown = controller is { CombVisible: true, IsAnalysis: false, Inspection: not null };
        IsVisible = shown;
        if (DisclosureHost is not null) DisclosureHost.IsVisible = shown && narrow && open;
        if (!shown) return;
        var frame = controller.Comb;
        bool auto = controller.CombGain is null;
        LegendText.Text = (auto ? "Auto · " : "") + RailComb.Legend(controller.CombPerMetre);
        CentreText.Text = RailComb.CentreNote;
        AutoButton.IsChecked = auto;
        AutoCheck.IsVisible = auto;
        Limit(SmallerButton, controller.CanStepScale(larger: false), "Smallest teeth", "Larger teeth");
        Limit(LargerButton, controller.CanStepScale(larger: true), "Largest teeth", "Smaller teeth");
        Limit(SparserButton, controller.CanStepDensity(denser: false), "Sparsest density", "Denser");
        Limit(DenserButton, controller.CanStepDensity(denser: true), "Densest density", "Sparser");
        DensityText.Text = controller.CombDensity + " per rail";
        string summary = RailComb.Summary(controller.CombGain, controller.CombDensity);
        SummaryButton.Content = summary + " ▾";
        AutomationProperties.SetName(SummaryButton, summary);
        LeadingPiecesText.Text = frame is null ? "" : RailComb.PiecesLine("leading", frame.LeadingPieces);
        TrailingPiecesText.Text = frame is null ? "" : RailComb.PiecesLine("trailing", frame.TrailingPieces);
        ThresholdText.Text = frame is null ? "" : RailComb.ThresholdLine(frame.LeadingPieces, frame.TrailingPieces);
        var stats = canvas?.CombStats;
        string? note = stats is null ? null : stats.Clipped > 0 ? RailComb.ClippedNote(stats.Clipped)
            : stats.Drawn > 0 && stats.LongestPixels < RailComb.VisibleFloorPixels ? RailComb.NoVisibleTooth : null;
        NoteText.Text = note;
        NoteText.IsVisible = note is not null;
        if (controller.CombAnnouncements != announcementsSeen)
        {
            announcementsSeen = controller.CombAnnouncements;
            LiveText.Text = "";
            LiveText.Text = controller.CombAnnouncement;
        }
        LegendText.Classes.Set("emphasis", controller.CombRefitEmphasis);
        if (controller.CombRefitEmphasis && !controller.ReducedMotion && flash is null)
        {
            // One flash: the emphasis runs once and ends. Under reduced motion no timer runs and the emphasis stays static.
            FlashTimers++;
            flash = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FlashMilliseconds) };
            flash.Tick += (_, _) =>
            {
                flash?.Stop();
                flash = null;
                controller?.EndCombEmphasis();
            };
            flash.Start();
        }
    }
}
