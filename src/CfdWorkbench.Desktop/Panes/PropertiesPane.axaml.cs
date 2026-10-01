using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Globalization;

namespace CfdWorkbench.Desktop.Panes;

public partial class PropertiesPane : UserControl
{
    private const string ControlHelper = "A control point pulls the curve toward it. The curve does not pass through it.";
    private const string AnchorHelper = "An anchor point is on the curve. Its handles set the curve's direction on each side.";
    private const string TipClosedCopy = "Tip closes — edit the tip station";

    private WorkbenchController? boundController;
    private string lastCommittedSpan = "";
    private bool suppress;
    private bool resumingRecovery;
    private PointView? selectedPoint;

    public PropertiesPane()
    {
        InitializeComponent();

        SpanInput.KeyDown += OnSpanKeyDown;
        TryAgainButton.Click += (_, _) =>
        {
            ErrorPanel.IsVisible = false;
            if (boundController != null) Bind(boundController);
        };
        TypeControl.ItemsSource = new[] { "Anchor point", "Control point" };
        TypeControl.SelectionChanged += OnTypeChanged;
        PointSpanInput.KeyDown += OnPointKeyDown;
        PointAftInput.KeyDown += OnPointKeyDown;
        HandleAngleInput.KeyDown += OnHandleKeyDown;
        HandleLengthInput.KeyDown += OnHandleKeyDown;
        RootChordInput.KeyDown += (_, e) => OnChordKeyDown(e, "root-chord", RootChordInput, TipChordInput);
        TipChordInput.KeyDown += (_, e) => OnChordKeyDown(e, "tip-chord", TipChordInput, null);
        HowMeasuredButton.Click += (_, _) => HowMeasuredBody.IsVisible = !HowMeasuredBody.IsVisible;
        TangentSmoothButton.Click += (_, _) => CommitTangent(TangentKind.Smooth);
        TangentSymmetricButton.Click += (_, _) => CommitTangent(TangentKind.Symmetric);
        TangentCornerButton.Click += (_, _) => CommitTangent(TangentKind.Corner);
        RecoveryApplyButton.Click += (_, _) => ApplyRecovery();
        RecoveryDiscardButton.Click += (_, _) => boundController?.DiscardRecovery();
    }

    public void FocusTypeValue() => PointSpanInput.Focus();

    public void ShowRenderFailure(bool foilOpen)
    {
        ErrorText.Text = "Properties couldn't be shown." + (foilOpen ? " Your foil hasn't changed." : "");
        ErrorPanel.IsVisible = true;
    }

    public void Bind(WorkbenchController controller)
    {
        boundController = controller;
        try
        {
            ErrorPanel.IsVisible = false;
            var authored = controller.CurrentProjection;
            if (authored is null)
            {
                EmptyPanel.IsVisible = true;
                ContentPanel.IsVisible = false;
                return;
            }

            var model = PropertiesView.Build(controller.Selection, authored, controller.Estimates, ShellMode.Workspace);
            EmptyPanel.IsVisible = false;
            ContentPanel.IsVisible = true;

            SelectionHeading.Text = model.Heading;
            BlocksPanel.Children.Clear();

            foreach (var block in model.Blocks)
            {
                // Skip the Wing block because WingBlock is rendered as the permanent last block in AXAML
                if (block.Title == "Wing") continue;

                var blockBorder = new Border
                {
                    Background = this.FindResource("SurfaceSoftBrush") as Avalonia.Media.IBrush,
                    BorderBrush = this.FindResource("LineBrush") as Avalonia.Media.IBrush,
                    BorderThickness = new Avalonia.Thickness(1)
                };
                if (this.FindResource("Space3") is Avalonia.Thickness padding)
                    blockBorder.Padding = padding;
                var sp = new StackPanel { Spacing = 4 };
                sp.Children.Add(new TextBlock { Text = block.Title, FontWeight = Avalonia.Media.FontWeight.SemiBold });
                foreach (var row in block.Rows)
                {
                    var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
                    var label = new TextBlock
                    {
                        Text = row.Label,
                        Width = 100,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    label.Classes.Add("caption");
                    var val = new TextBlock
                    {
                        Text = row.Value,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    Grid.SetColumn(label, 0);
                    Grid.SetColumn(val, 1);
                    rowGrid.Children.Add(label);
                    rowGrid.Children.Add(val);
                    sp.Children.Add(rowGrid);
                }
                blockBorder.Child = sp;
                BlocksPanel.Children.Add(blockBorder);
            }

            // Populate Wing block
            var wingBlockModel = model.Blocks.FirstOrDefault(b => b.Title == "Wing");
            if (wingBlockModel != null)
            {
                var spanRow = wingBlockModel.Rows.FirstOrDefault(r => r.Label == "Span");
                if (spanRow != null && !SpanInput.IsKeyboardFocusWithin)
                {
                    var parts = spanRow.Value.Split(' ');
                    SpanInput.Text = parts[0];
                    lastCommittedSpan = parts[0];
                    if (parts.Length > 1) SpanUnit.Text = parts[1];
                }

                var rootRow = wingBlockModel.Rows.FirstOrDefault(r => r.Label == "Root chord");
                if (rootRow != null) RootChordText.Text = rootRow.Value;

                var tipRow = wingBlockModel.Rows.FirstOrDefault(r => r.Label == "Tip chord");
                if (tipRow != null) TipChordText.Text = tipRow.Value;
            }

            if (controller.Estimates is { } est)
            {
                EstimatesAreaText.Text = $"Area: {(est.AreaSquareMeters * 10000).ToString("F0", CultureInfo.InvariantCulture)} cm²";
                EstimatesAspectRatioText.Text = $"Aspect ratio: {est.AspectRatio.ToString("F2", CultureInfo.InvariantCulture)}";
            }
            BindPointAndWing(controller);
        }
        catch (Exception ex)
        {
            BlocksPanel.Children.Clear();
            ContentPanel.IsVisible = false;
            EmptyPanel.IsVisible = false;
            ShowRenderFailure(controller?.Inspection is not null);
            ShellEvents.Record("shell.pane.render", "error", 0, "pane-bind", exceptionType: ex.GetType().Name);
            return;
        }
    }

    private void OnSpanKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            SpanInput.Text = lastCommittedSpan;
            SpanErrorPanel.IsVisible = false;
        }
        else if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            bool ok = CommitSpan();
            if (!ok)
            {
                e.Handled = true;
                if (e.Key == Key.Tab)
                    (TopLevel.GetTopLevel(this) as IInputRoot)?.KeyboardNavigationHandler?.Move(SpanInput,
                        e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? NavigationDirection.Previous : NavigationDirection.Next,
                        e.KeyModifiers);
            }
            else if (e.Key == Key.Tab)
            {
                e.Handled = true;
                this.FindAncestorOfType<ShellHost>()?.MoveFocus(false);
            }
        }
    }

    public bool CommitSpan()
    {
        if (boundController == null) return false;
        var text = SpanInput.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(text)) return false;
        var task = boundController.ApplySpanAsync(text);
        PumpUi(task);
        if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Committed)
        {
            lastCommittedSpan = text;
            SpanErrorPanel.IsVisible = false;
            Avalonia.Automation.AutomationProperties.SetHelpText(SpanInput, "");
            return true;
        }
        string code = task.IsCompletedSuccessfully && task.Result is CommitOutcome.Refused refused ? refused.Code : "DSL-NOT-ASSESSED";
        SpanErrorText.Text = code switch
        {
            "DSL-UNIT" => "Enter a length greater than 0 mm. Span is unchanged.",
            "DSL-EDGES-CROSS" => "That would make the leading and trailing edges cross. Enter a different value.",
            "DSL-NOT-ASSESSED" => "The new span couldn't be checked. Span is unchanged. Try again or enter a different value.",
            _ => "Enter a number. Span is unchanged."
        };
        SpanErrorPanel.IsVisible = true;
        Avalonia.Automation.AutomationProperties.SetHelpText(SpanInput, SpanErrorText.Text);
        return false;
    }

    private void BindPointAndWing(WorkbenchController controller)
    {
        var plan = controller.Planform;
        var estimates = controller.Estimates;
        bool preview = plan?.Basis == "preview" || estimates?.Basis == "preview";
        WingHeading.Text = preview ? "Wing · ≈ preview" : "Wing";
        bool dash = estimates is null || !estimates.Converged || !double.IsFinite(estimates.MacMeters);
        MeanChordText.Text = dash || estimates is null ? "≈ —" : "≈ " + Mm(estimates.MeanChordMeters);
        MacText.Text = dash || estimates is null ? "≈ —" : "≈ " + Mm(estimates.MacMeters);
        MaxTcText.Text = dash || estimates is null ? "≈ —" : "≈ " + estimates.MaxThicknessRatio.ToString("0.00", CultureInfo.InvariantCulture);
        AspectText.Text = dash || estimates is null ? "≈ —" : "≈ " + estimates.AspectRatio.ToString("0.00", CultureInfo.InvariantCulture);
        AreaEstimateText.Text = dash || estimates is null ? "≈ —" : "≈ " + (estimates.AreaSquareMeters * 10000).ToString("0", CultureInfo.InvariantCulture) + " cm²";
        WingReasonText.IsVisible = dash && estimates is not null;
        WingReasonText.Text = estimates?.Compute.Outcome ?? "";
        bool tipClosed = estimates is not null && estimates.TipChordMeters <= 1e-9;
        TipClosedText.IsVisible = tipClosed;
        TipClosedText.Text = TipClosedCopy;
        TipChordInput.IsVisible = !tipClosed;
        if (estimates is not null && !RootChordInput.IsKeyboardFocusWithin)
            RootChordInput.Text = Mm(estimates.RootChordMeters);
        if (estimates is not null && !tipClosed && !TipChordInput.IsKeyboardFocusWithin)
            TipChordInput.Text = Mm(estimates.TipChordMeters);

        if (controller.HasRecovery && controller.Draft is null && !resumingRecovery)
        {
            resumingRecovery = true;
            try { controller.ResumeRecovery(); }
            finally { resumingRecovery = false; }
        }
        bool recovery = controller.HasRecovery;
        RecoveryPanel.IsVisible = recovery;
        RecoveryBanner.IsVisible = recovery;
        if (recovery) RecoveryBanner.Text = "A recovered edit is open.";

        selectedPoint = null;
        if (plan is null || controller.Selection is not Selection.Points points || points.Items.Count == 0)
        {
            PointBlock.IsVisible = false;
            return;
        }
        PointBlock.IsVisible = true;
        if (points.Items.Count != 1)
        {
            PointHeading.Text = points.Items.Count + " points";
            MixedBanner.IsVisible = true;
            MixedBanner.Text = "Select one point to change it.";
            TypeControl.IsVisible = false;
            TypeReadOnly.IsVisible = true;
            TypeReadOnly.Text = "Mixed";
            PointHelper.Text = "";
            ConstraintText.Text = "";
            PointSpanInput.IsEnabled = false;
            PointAftInput.IsEnabled = false;
            TangentGroup.IsVisible = false;
            HandleGroup.IsVisible = false;
            return;
        }
        MixedBanner.IsVisible = false;
        var reference = points.Items[0];
        var rail = reference.Curve == "leading" ? plan.Leading : plan.Trailing;
        var point = rail.Points.FirstOrDefault(item => item.Id == reference.VertexId);
        if (point is null)
        {
            PointBlock.IsVisible = false;
            return;
        }
        selectedPoint = point;
        string curveName = point.Curve == "leading" ? "Leading edge" : "Trailing edge";
        PointHeading.Text = curveName + " · point " + (point.Index + 1) + " of " + rail.Points.Count;
        bool named = point.Role is not (PointRole.Control or PointRole.Anchor);
        TypeControl.IsVisible = !named;
        TypeControl.IsEnabled = !named;
        TypeReadOnly.IsVisible = named;
        TypeReadOnly.Text = named ? RoleText(point.Role) : "";
        if (!named)
        {
            suppress = true;
            TypeControl.SelectedItem = point.Role == PointRole.Anchor ? "Anchor point" : "Control point";
            suppress = false;
        }
        ConstraintText.Text = point.Role == PointRole.RootEnd && point.Curve == "leading"
            ? "Fixed: the leading edge starts at the root."
            : point.Freedom switch
            {
                PointFreedom.Fixed => "Fixed.",
                PointFreedom.SpanOnly => "Moves in span only.",
                PointFreedom.AftOnly => "Moves in chord only.",
                _ => ""
            };
        PointHelper.Text = point.Role == PointRole.Control ? ControlHelper : AnchorHelper;
        bool spanOn = point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly;
        bool aftOn = point.Freedom is PointFreedom.Free or PointFreedom.AftOnly;
        PointSpanInput.IsEnabled = spanOn;
        PointAftInput.IsEnabled = aftOn;
        if (!PointSpanInput.IsKeyboardFocusWithin) PointSpanInput.Text = Mm(point.SpanMeters);
        if (!PointAftInput.IsKeyboardFocusWithin) PointAftInput.Text = Mm(point.AftMeters);
        bool tangent = point.Role is PointRole.Anchor or PointRole.RootEnd or PointRole.TipEnd;
        TangentGroup.IsVisible = tangent;
        bool handle = point.Role is PointRole.RootHandle or PointRole.TipHandle or PointRole.AnchorHandle;
        HandleGroup.IsVisible = handle;
        if (handle && point.AnchorId is not null)
        {
            var anchor = rail.Points.First(item => item.Id == point.AnchorId);
            double spanDelta = point.SpanMeters - anchor.SpanMeters;
            double aftDelta = point.AftMeters - anchor.AftMeters;
            if (point.Index < anchor.Index) { spanDelta = -spanDelta; aftDelta = -aftDelta; }
            if (!HandleAngleInput.IsKeyboardFocusWithin)
                HandleAngleInput.Text = (Math.Atan2(aftDelta, spanDelta) * 180 / Math.PI).ToString("0.00", CultureInfo.InvariantCulture);
            if (!HandleLengthInput.IsKeyboardFocusWithin)
                HandleLengthInput.Text = (Math.Sqrt(spanDelta * spanDelta + aftDelta * aftDelta) * 1000).ToString("0.00", CultureInfo.InvariantCulture);
            HandleLengthInput.IsEnabled = true;
            HandleAngleInput.IsEnabled = point.Freedom != PointFreedom.SpanOnly;
        }
    }

    private void OnTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suppress || boundController is null || selectedPoint is not { } point) return;
        string choice = TypeControl.SelectedItem?.ToString() ?? "";
        PointCommand? command = choice switch
        {
            "Anchor point" when point.Role != PointRole.Anchor => new PointCommand.MakeAnchor(point.Curve, point.Id),
            "Control point" when point.Role != PointRole.Control => new PointCommand.MakeControl(point.Curve, point.Id),
            _ => null
        };
        if (command is null) return;
        PumpUi(boundController.ApplyPointCommandAsync(command));
    }

    private void OnPointKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            this.FindAncestorOfType<ShellHost>()?.ModelView.FoilViewport.Focus();
            return;
        }
        if (e.Key is not (Key.Enter or Key.Return)) return;
        e.Handled = true;
        CommitPointFields();
    }

    private void OnHandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return)) return;
        e.Handled = true;
        CommitHandle();
    }

    private void OnChordKeyDown(KeyEventArgs e, string dimension, TextBox box, TextBox? next)
    {
        if (e.Key is not (Key.Enter or Key.Return or Key.Tab)) return;
        e.Handled = true;
        if (CommitChord(dimension, box) && e.Key == Key.Tab)
            next?.Focus();
    }

    private void CommitPointFields()
    {
        if (boundController is null || selectedPoint is not { } point) return;
        var dimensions = Dimensions();
        double span;
        double aft;
        try
        {
            span = LengthExpression.ParseMeters(PointSpanInput.Text ?? "", dimensions);
            aft = LengthExpression.ParseMeters(PointAftInput.Text ?? "", dimensions);
        }
        catch (ContractError)
        {
            ShowPointError("Enter a number. Span is unchanged.");
            return;
        }
        if (!boundController.BeginGesture(new PointRef(point.Curve, point.Id), GestureInput.Pointer))
        {
            ShowPointError(boundController.Status);
            return;
        }
        boundController.UpdateGesture(span, aft);
        boundController.FlushGestureFrame();
        PumpUi(boundController.EndGestureAsync(GestureEnd.Release));
        PointErrorText.IsVisible = false;
        EchoPoint(point.Curve, point.Id);
    }

    private void CommitHandle()
    {
        if (boundController?.Planform is not { } plan || selectedPoint is not { } point) return;
        if (!double.TryParse(HandleAngleInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double angle)) return;
        if (!double.TryParse(HandleLengthInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lengthMm)) return;
        var target = CfdWorkbench.Core.Planform.HandleTarget(plan, point.Curve, point.Id, angle, lengthMm / 1000d);
        if (!boundController.BeginGesture(new PointRef(point.Curve, point.Id), GestureInput.Pointer)) return;
        boundController.UpdateGesture(target.SpanMeters, target.AftMeters);
        boundController.FlushGestureFrame();
        PumpUi(boundController.EndGestureAsync(GestureEnd.Release));
        EchoPoint(point.Curve, point.Id);
    }

    private void CommitTangent(TangentKind kind)
    {
        if (boundController is null || selectedPoint is not { } point) return;
        PumpUi(boundController.ApplyPointCommandAsync(new PointCommand.SetTangent(point.Curve, point.Id, kind, null)));
    }

    private bool CommitChord(string dimension, TextBox box)
    {
        if (boundController is null) return false;
        var task = boundController.ApplyChordAsync(dimension, box.Text ?? "");
        PumpUi(task);
        if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Committed committed)
        {
            bool warning = committed.Report.Contains("above the limit", StringComparison.Ordinal);
            ChordWarningText.IsVisible = warning;
            ChordWarningText.Text = warning ? committed.Report : "";
            var estimates = boundController.Estimates;
            if (estimates is not null)
                box.Text = Mm(dimension == "root-chord" ? estimates.RootChordMeters : estimates.TipChordMeters);
            return true;
        }
        if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Refused { Code: "DSL-TARGET" })
        {
            TipClosedText.IsVisible = true;
            TipClosedText.Text = TipClosedCopy;
            TipChordInput.IsVisible = false;
        }
        return false;
    }

    private void ApplyRecovery()
    {
        if (boundController is null) return;
        if (boundController.Draft is null && boundController.HasRecovery)
            boundController.ResumeRecovery();
        PumpUi(boundController.PreviewAsync());
        boundController.Apply();
    }

    private void EchoPoint(string curve, string id)
    {
        if (boundController?.Planform is not { } plan) return;
        var rail = curve == "leading" ? plan.Leading : plan.Trailing;
        var point = rail.Points.FirstOrDefault(item => item.Id == id);
        if (point is null) return;
        selectedPoint = point;
        PointSpanInput.Text = Mm(point.SpanMeters);
        PointAftInput.Text = Mm(point.AftMeters);
        if (point.AnchorId is not null && rail.Points.FirstOrDefault(item => item.Id == point.AnchorId) is { } anchor)
        {
            double spanDelta = point.SpanMeters - anchor.SpanMeters;
            double aftDelta = point.AftMeters - anchor.AftMeters;
            if (point.Index < anchor.Index) { spanDelta = -spanDelta; aftDelta = -aftDelta; }
            HandleAngleInput.Text = (Math.Atan2(aftDelta, spanDelta) * 180 / Math.PI).ToString("0.00", CultureInfo.InvariantCulture);
            HandleLengthInput.Text = (Math.Sqrt(spanDelta * spanDelta + aftDelta * aftDelta) * 1000).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }

    private Dictionary<string, double> Dimensions()
    {
        var estimates = boundController?.Estimates;
        var plan = boundController?.Planform;
        return new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["span"] = estimates?.SpanMeters ?? plan?.HalfSpanMeters ?? 0,
            ["root_chord"] = estimates?.RootChordMeters ?? 0,
            ["tip_chord"] = estimates?.TipChordMeters ?? 0
        };
    }

    private void ShowPointError(string text)
    {
        PointErrorText.Text = text;
        PointErrorText.IsVisible = true;
    }

    private static string Mm(double meters) => (meters * 1000).ToString("0.00", CultureInfo.InvariantCulture);

    private static string RoleText(PointRole role) => role switch
    {
        PointRole.RootEnd => "Root end",
        PointRole.TipEnd => "Tip end",
        PointRole.RootHandle => "Root handle",
        PointRole.TipHandle => "Tip handle",
        PointRole.AnchorHandle => "Anchor handle",
        PointRole.Anchor => "Anchor point",
        _ => "Control point"
    };

    private static void PumpUi(Task task)
    {
        if (task.IsCompleted) return;
        var start = DateTime.UtcNow;
        while (!task.IsCompleted && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
    }
}
