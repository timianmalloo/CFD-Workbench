using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Globalization;

namespace CfdWorkbench.Desktop.Panes;

public partial class PropertiesPane : UserControl
{
    private WorkbenchController? boundController;
    private string lastCommittedSpan = "";

    public PropertiesPane()
    {
        InitializeComponent();

        SpanInput.KeyDown += OnSpanKeyDown;
        TryAgainButton.Click += (_, _) =>
        {
            ErrorPanel.IsVisible = false;
            if (boundController != null) Bind(boundController);
        };
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
        }
        catch (Exception ex)
        {
            BlocksPanel.Children.Clear();
            ContentPanel.IsVisible = false;
            EmptyPanel.IsVisible = false;
            ErrorText.Text = "Properties couldn't be shown.";
            ErrorPanel.IsVisible = true;
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
                SpanInput.Focus();
            }
        }
    }

    public bool CommitSpan()
    {
        if (boundController == null) return false;
        var text = SpanInput.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(text) || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val) || val <= 0)
        {
            SpanErrorText.Text = "Invalid span. Enter a positive number.";
            SpanErrorPanel.IsVisible = true;
            SpanInput.Focus();
            return false;
        }

        try
        {
            boundController.ApplySpan(text);
            lastCommittedSpan = text;
            SpanErrorPanel.IsVisible = false;
            return true;
        }
        catch (Exception ex)
        {
            SpanErrorText.Text = ex is ContractError ce ? $"{ce.Code}: {ce.Message}" : ex.Message;
            SpanErrorPanel.IsVisible = true;
            SpanInput.Focus();
            return false;
        }
    }
}
