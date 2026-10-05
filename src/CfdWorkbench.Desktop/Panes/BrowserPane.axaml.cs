using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Globalization;
using System.Text;

namespace CfdWorkbench.Desktop.Panes;

public partial class BrowserPane : UserControl
{
    private WorkbenchController? boundController;
    private string? lastAcceptedIdentity;
    private string? lastSectionSourceKey;
    private bool refreshing;

    public BrowserPane()
    {
        InitializeComponent();

        StationList.KeyDown += OnStationKeyDown;
        StationList.SelectionChanged += OnStationSelectionChanged;
        foreach (var (list, curve) in CurveLists())
            list.SelectionChanged += (_, _) => OnPointSelected(list, curve);
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
                lastSectionSourceKey = null;
                return;
            }

            EmptyPanel.IsVisible = false;
            ContentPanel.IsVisible = true;

            string? currentIdentity = controller.Inspection?.Authored.Binding.AcceptedId;
            string? sourceKey = controller.Section is { } mode
                ? mode.Draft.DraftId + ":" + mode.Draft.Generation.ToString(CultureInfo.InvariantCulture) : null;
            bool acceptedChanged = currentIdentity != lastAcceptedIdentity || sourceKey != lastSectionSourceKey;

            if (acceptedChanged || StationList.ItemsSource == null)
            {
                var items = new List<ListBoxItem>();
                int index = 0;
                foreach (var station in authored.Assignments)
                {
                    byte[] source = controller.Section?.Draft.Bytes ?? Encoding.UTF8.GetBytes(controller.AcceptedSource);
                    string text = $"η {station.Eta.ToString("G3", CultureInfo.InvariantCulture)} · " +
                        StationSourceText(station.ProfileName, source);
                    var item = new ListBoxItem { Content = text, Tag = (index, station.Eta) };
                    items.Add(item);
                    index++;
                }
                BindStations(StationList, items, acceptedChanged: true);
                lastAcceptedIdentity = currentIdentity;
                lastSectionSourceKey = sourceKey;
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

    public static string StationSourceText(string profile, byte[] foil)
    {
        string source = PropertiesView.SourceName(foil, profile);
        return source == "Source not recorded" ? profile : profile + " · " + source;
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

    /// <summary>One list per curve: the rails, then the Dihedral, Twist and Thickness groups (M1.2b2 §11.4).</summary>
    private IEnumerable<(ListBox List, string Curve)> CurveLists() =>
    [
        (LeadingEdgeList, "leading"), (TrailingEdgeList, "trailing"), (DihedralList, "dihedral"), (TwistList, "twist"),
        (ThicknessList, "thickness")
    ];

    private void BindRails(WorkbenchController controller)
    {
        refreshing = true;
        try
        {
            foreach (var (list, curve) in CurveLists()) FillRail(list, controller.CurveFor(curve), curve);
            if (controller.Selection is Selection.Points { Items.Count: 1 } selected &&
                CurveLists().FirstOrDefault(entry => entry.Curve == selected.Items[0].Curve).List is { } selectedList)
                selectedList.SelectedItem = selectedList.Items.OfType<ListBoxItem>().FirstOrDefault(item => Equals(item.Tag, selected.Items[0].VertexId));
        }
        finally { refreshing = false; }
    }

    /// <summary>A point's row text: its role, From root in mm, and its value in the curve's unit.</summary>
    private static string RowText(PointView point)
    {
        string from = (point.SpanMeters * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " mm";
        if (point.Curve is "leading" or "trailing")
            return point.Role + "  " + from + "  " + (point.Ordinate * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " mm";
        var rows = PropertiesView.Curves[point.Curve];
        return point.Role + "  " + from + "  " +
               Quantity.WithUnit(Quantity.Typed(point.Ordinate * PropertiesView.FieldScale[rows.ValueFamily]), rows.ValueUnit);
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
                Text = RowText(point),
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
