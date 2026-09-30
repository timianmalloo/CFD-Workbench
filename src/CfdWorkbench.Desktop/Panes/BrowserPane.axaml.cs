using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
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

    private void OnStationSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (refreshing || boundController == null) return;
        if (StationList.SelectedItem is ListBoxItem item && item.Tag is ValueTuple<int, double> tag)
        {
            boundController.Select(new Selection.Station(tag.Item1, tag.Item2));
        }
    }
}
