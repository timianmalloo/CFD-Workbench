using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
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
    private bool actionPending;

    /// <summary>The in-flight asynchronous context-menu action, or a completed task (F-21).</summary>
    public Task Pending { get; private set; } = Task.CompletedTask;

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
                // F-44: encode and parse FoilSource once before the station loop.
                byte[] source = controller.Section?.Draft.Bytes ?? Encoding.UTF8.GetBytes(controller.AcceptedSource);
                var parsed = FoilSource.Parse(source);
                var items = new List<ListBoxItem>(authored.Assignments.Count);
                int index = 0;
                foreach (var station in authored.Assignments)
                {
                    string text = $"η {station.Eta.ToString("G3", CultureInfo.InvariantCulture)} · " +
                        StationSourceText(station.ProfileName, parsed);
                    var item = new ListBoxItem { Content = text, Tag = (index, station.Eta) };
                    items.Add(item);
                    index++;
                }
                BindStations(StationList, items, acceptedChanged: true);
                lastAcceptedIdentity = currentIdentity;
                lastSectionSourceKey = sourceKey;
            }

            // F-27: sync station selection and clear it whenever selection is not a station (no dual selection).
            refreshing = true;
            if (controller.Selection is Selection.Station st && st.Index >= 0 && st.Index < StationList.ItemCount)
                StationList.SelectedIndex = st.Index;
            else
                StationList.SelectedItem = null;
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

    public static string StationSourceText(string profile, byte[] foil) =>
        StationSourceText(profile, FoilSource.Parse(foil));

    private static string StationSourceText(string profile, SourceParse parsed)
    {
        var provenance = Provenance.Parse(parsed.Profile(profile)?.Provenance);
        return provenance.RowText(profile, PropertiesView.SourceName(provenance));
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
            var selectedPoint = controller.Selection is Selection.Points { Items.Count: 1 } one ? one.Items[0] : null;
            foreach (var (list, curve) in CurveLists())
            {
                FillRail(list, controller.CurveFor(curve), curve);
                if (selectedPoint is { } picked && picked.Curve == curve)
                    list.SelectedItem = list.Items.OfType<ListBoxItem>().FirstOrDefault(item => Equals(item.Tag, picked.VertexId));
                else
                    list.SelectedItem = null;
            }
        }
        finally { refreshing = false; }
    }

    /// <summary>A point's row text: its role, From root in mm, and its value in the curve's unit.</summary>
    private static string RowText(PointView point)
    {
        string from = (point.SpanMeters * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " mm";
        string role = PropertiesView.RoleText(point.Role);
        if (point.Curve is "leading" or "trailing")
            return role + "  " + from + "  " + (point.Ordinate * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " mm";
        var rows = PropertiesView.Curves[point.Curve];
        return role + "  " + from + "  " +
               Quantity.WithUnit(Quantity.Typed(point.Ordinate * PropertiesView.FieldScale[rows.ValueFamily]), rows.ValueUnit);
    }

    private Task RunPendingAsync(Func<Task> action)
    {
        if (actionPending) return Task.CompletedTask;
        var task = ExecuteAsync(action);
        Pending = task;
        return task;
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        actionPending = true;
        try { await action(); }
        finally { actionPending = false; }
    }

    /// <summary>
    /// F-41: role-matched vector glyphs matching the CAD canvas (filled circle Ellipse for Control, square Rectangle
    /// for Anchor, diamond Path for End, hollow circle Path for Handle), all live-bound to InkBrush.
    /// </summary>
    private Shape CreateRoleGlyph(PointRole role)
    {
        Shape glyph = role switch
        {
            PointRole.Anchor => new Rectangle
            {
                Name = "RoleGlyph",
                Width = 8,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = role
            },
            PointRole.RootEnd or PointRole.TipEnd => new Avalonia.Controls.Shapes.Path
            {
                Name = "RoleGlyph",
                Width = 8,
                Height = 8,
                Data = Avalonia.Media.Geometry.Parse("M4,0 L8,4 L4,8 L0,4 Z"),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = role
            },
            PointRole.RootHandle or PointRole.TipHandle or PointRole.AnchorHandle => new Avalonia.Controls.Shapes.Path
            {
                Name = "RoleGlyph",
                Width = 8,
                Height = 8,
                Data = Avalonia.Media.Geometry.Parse("M4,1 A3,3 0 1 1 3.99,1 Z"),
                StrokeThickness = 1.5,
                Fill = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = role
            },
            _ => new Ellipse
            {
                Name = "RoleGlyph",
                Width = 8,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = role
            }
        };
        if (role is PointRole.RootHandle or PointRole.TipHandle or PointRole.AnchorHandle)
            glyph.Bind(Shape.StrokeProperty, this.GetResourceObservable("InkBrush"));
        else
            glyph.Bind(Shape.FillProperty, this.GetResourceObservable("InkBrush"));
        return glyph;
    }

    private void FillRail(ListBox list, CurveView? curve, string curveName)
    {
        if (curve is null)
        {
            list.Items.Clear();
            return;
        }
        // F-27: diff rail items in place so ListBoxItem controls and keyboard focus survive Bind().
        var points = curve.Points;
        for (int index = 0; index < points.Count; index++)
        {
            var point = points[index];
            string rowText = RowText(point);
            if (index < list.Items.Count && list.Items[index] is ListBoxItem existing &&
                existing.Content is StackPanel panel && panel.Children.Count == 2 &&
                panel.Children[0] is Shape oldGlyph && panel.Children[1] is TextBlock oldText)
            {
                existing.Tag = point.Id;
                if (!Equals(oldGlyph.Tag, point.Role))
                    panel.Children[0] = CreateRoleGlyph(point.Role);
                if (oldText.Text != rowText)
                    oldText.Text = rowText;
                if (existing.ContextMenu is { } existingMenu && existingMenu.Tag is not ShellHost && this.FindAncestorOfType<ShellHost>() is not null)
                    PopulatePointMenu(existingMenu, existing, curveName, selectRow: false);
                continue;
            }
            var newItem = CreateRailItem(point, curveName, rowText);
            if (index < list.Items.Count) list.Items[index] = newItem;
            else list.Items.Add(newItem);
        }
        while (list.Items.Count > points.Count)
            list.Items.RemoveAt(list.Items.Count - 1);
    }

    private ListBoxItem CreateRailItem(PointView point, string curveName, string rowText)
    {
        var glyph = CreateRoleGlyph(point.Role);
        var text = new TextBlock
        {
            Text = rowText,
            VerticalAlignment = VerticalAlignment.Center
        };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(glyph);
        panel.Children.Add(text);
        var item = new ListBoxItem { Content = panel, Tag = point.Id };
        // F-27 + F-42 + F-21: shared PointContextMenu gated via ShellHost.CanRun, eagerly populated and refreshed on open.
        var menu = new ContextMenu();
        bool populatedForOpen = false;
        menu.Opening += (_, _) => { populatedForOpen = true; PopulatePointMenu(menu, item, curveName, selectRow: true); };
        menu.Opened += (_, _) => { if (!populatedForOpen) PopulatePointMenu(menu, item, curveName, selectRow: true); populatedForOpen = false; };
        menu.Closed += (_, _) => populatedForOpen = false;
        item.ContextMenu = menu;
        PopulatePointMenu(menu, item, curveName, selectRow: false);
        return item;
    }

    private void PopulatePointMenu(ContextMenu menu, ListBoxItem item, string curveName, bool selectRow)
    {
        if (boundController is null || item.Tag is not string id) return;
        var reference = new PointRef(curveName, id);
        void SelectIfNeeded()
        {
            if (boundController is { } controller &&
                (controller.Selection is not Selection.Points { Items: [var only] } || only != reference))
                controller.Select(new Selection.Points([reference]));
        }
        if (selectRow) SelectIfNeeded();
        if (this.FindAncestorOfType<ShellHost>() is { } host)
        {
            menu.Tag = host;
            menu.ItemsSource = PointContextMenu.BuildItems(host, reference, RunPendingAsync, sentenceCase: true, beforeRun: SelectIfNeeded);
            return;
        }
        menu.ItemsSource = new Control[]
        {
            new MenuItem { Header = "Make anchor", IsEnabled = false },
            new MenuItem { Header = "Make control", IsEnabled = false },
            new MenuItem { Header = "Remove point", IsEnabled = false }
        };
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
