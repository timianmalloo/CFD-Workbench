using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Globalization;

namespace CfdWorkbench.Desktop.Panes;

public partial class BrowserPane : UserControl
{
    private WorkbenchController? boundController;
    private string? lastAcceptedIdentity;
    private bool refreshing;

    public BrowserPane()
    {
        InitializeComponent();

        StationList.KeyDown += OnStationKeyDown;
        StationList.SelectionChanged += OnStationSelectionChanged;
        LeadingEdgeList.SelectionChanged += (_, _) => OnPointSelected(LeadingEdgeList, "leading");
        TrailingEdgeList.SelectionChanged += (_, _) => OnPointSelected(TrailingEdgeList, "trailing");
        TryAgainButton.Click += (_, _) =>
        {
            ErrorPanel.IsVisible = false;
            if (boundController != null) Bind(boundController);
        };
    }

    public void ShowRenderFailure(bool foilOpen)
    {
        ErrorText.Text = "Browser couldn't be shown." + (foilOpen ? " Your foil hasn't changed." : "");
        ErrorPanel.IsVisible = true;
    }

    public void Bind(WorkbenchController controller)
    {
        boundController = controller;
        try
        {
            ErrorPanel.IsVisible = false;
            var authored = controller.CurrentProjection;
            if (controller.Inspection is null || authored is null || authored.Assignments.Count == 0)
            {
                EmptyPanel.IsVisible = true;
                ContentPanel.IsVisible = false;
                StationList.ItemsSource = null;
                lastAcceptedIdentity = null;
                return;
            }

            EmptyPanel.IsVisible = false;
            ContentPanel.IsVisible = true;

            string? currentIdentity = controller.Inspection?.Authored.Binding.AcceptedId;
            bool acceptedChanged = currentIdentity != lastAcceptedIdentity;

            if (acceptedChanged || StationList.ItemsSource == null)
            {
                var items = new List<ListBoxItem>();
                int index = 0;
                foreach (var station in authored.Assignments)
                {
                    string text = $"η {station.Eta.ToString("G3", CultureInfo.InvariantCulture)} · {station.ProfileName}";
                    var item = new ListBoxItem { Content = text, Tag = (index, station.Eta) };
                    items.Add(item);
                    index++;
                }
                BindStations(StationList, items, acceptedChanged: true);
                lastAcceptedIdentity = currentIdentity;
            }

            // Sync selection
            refreshing = true;
            if (controller.Selection is Selection.Station st)
            {
                if (st.Index >= 0 && st.Index < StationList.ItemCount)
                {
                    StationList.SelectedIndex = st.Index;
                }
            }
            refreshing = false;
            BindRails(controller);
        }
        catch (Exception ex)
        {
            StationList.ItemsSource = null;
            ContentPanel.IsVisible = false;
            EmptyPanel.IsVisible = false;
            ShowRenderFailure(controller?.Inspection is not null);
            ShellEvents.Record("shell.pane.render", "error", 0, "pane-bind", exceptionType: ex.GetType().Name);
            refreshing = false;
            return;
        }
    }

    public static void BindStations(ListBox list, IReadOnlyList<ListBoxItem> items, bool acceptedChanged)
    {
        if (acceptedChanged)
        {
            list.ItemsSource = items;
        }
    }

    private void OnStationKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && StationList.SelectedItem is ListBoxItem item && item.Tag is ValueTuple<int, double> tag)
        {
            e.Handled = true;
            boundController?.Select(new Selection.Station(tag.Item1, tag.Item2));
            item.Focus();
        }
    }

    private void BindRails(WorkbenchController controller)
    {
        refreshing = true;
        try
        {
            FillRail(LeadingEdgeList, controller.Planform?.Leading, "leading");
            FillRail(TrailingEdgeList, controller.Planform?.Trailing, "trailing");
            if (controller.Selection is Selection.Points { Items.Count: 1 } selected)
            {
                var list = selected.Items[0].Curve == "leading" ? LeadingEdgeList : TrailingEdgeList;
                list.SelectedItem = list.Items.OfType<ListBoxItem>().FirstOrDefault(item => Equals(item.Tag, selected.Items[0].VertexId));
            }
        }
        finally { refreshing = false; }
    }

    private void FillRail(ListBox list, CurveView? curve, string curveName)
    {
        list.Items.Clear();
        if (curve is null) return;
        foreach (var point in curve.Points)
        {
            var glyph = new Ellipse
            {
                Name = "RoleGlyph",
                Width = 8,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = this.FindResource("InkBrush") as IBrush
            };
            var text = new TextBlock
            {
                Text = point.Role + "  " + (point.SpanMeters * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " mm  " +
                       (point.Ordinate * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " mm",
                VerticalAlignment = VerticalAlignment.Center
            };
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            panel.Children.Add(glyph);
            panel.Children.Add(text);
            var item = new ListBoxItem { Content = panel, Tag = point.Id };
            var menu = new ContextMenu();
            var makeAnchor = new MenuItem { Header = "Make anchor" };
            string id = point.Id;
            makeAnchor.Click += (_, _) =>
            {
                if (boundController is null) return;
                var task = boundController.ApplyPointCommandAsync(new PointCommand.MakeAnchor(curveName, id));
                var start = DateTime.UtcNow;
                while (!task.IsCompleted && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
            };
            menu.Items.Add(makeAnchor);
            item.ContextMenu = menu;
            list.Items.Add(item);
        }
    }

    private void OnPointSelected(ListBox list, string curve)
    {
        if (refreshing || boundController is null) return;
        if (list.SelectedItem is ListBoxItem item && item.Tag is string id)
            boundController.Select(new Selection.Points(new[] { new PointRef(curve, id) }));
    }

    private void OnStationSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (refreshing || boundController == null) return;
        if (StationList.SelectedItem is ListBoxItem item && item.Tag is ValueTuple<int, double> tag)
        {
            boundController.Select(new Selection.Station(tag.Item1, tag.Item2));
        }
    }
}
