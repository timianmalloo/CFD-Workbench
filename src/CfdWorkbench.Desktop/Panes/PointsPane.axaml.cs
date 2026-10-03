using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Panes;

/// <summary>
/// The Points pane (design §11.4): the control net, typed by the Properties commit rules (one step in the section mode,
/// one undo step in the views), with selection both ways. It owns no model: <see cref="PointsView.Build"/> is the rows.
/// </summary>
public partial class PointsPane : UserControl
{
    private WorkbenchController? controller;
    private readonly Dictionary<string, (string Text, string Error)> errors = new(StringComparer.Ordinal);
    private readonly HashSet<string> collapsed = new(StringComparer.Ordinal);
    private PointsModel? shown;

    public PointsPane()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>Reports for the status strip (DR-STATUS-1): a typed x in mm is echoed in % c there.</summary>
    public event Action<StatusReport>? Reported;

    /// <summary>The last model drawn; tests read it for the rows' values.</summary>
    public PointsModel? Model => shown;

    /// <summary>The in-flight typed commit, or a completed task.</summary>
    public Task Pending { get; private set; } = Task.CompletedTask;

    public void Bind(WorkbenchController bound)
    {
        controller = bound;
        var model = PointsView.Build(bound);
        if (shown is { } before)
        {
            if (before.Section != model.Section || !Rows(before).Select(row => row.Target).SequenceEqual(Rows(model).Select(row => row.Target)))
                errors.Clear();
            // UI-LIFETIME: a refresh that changes nothing the pane shows keeps every control, so focus and typing survive.
            else if (Same(before, model)) return;
        }
        shown = model;
        // Focus survives a re-render: the focused field or point button is found again by its name.
        string? focused = (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control) is { Name: { } name } control &&
                          this.IsVisualAncestorOf(control) ? name : null;
        Render(model);
        if (focused is not null &&
            this.GetVisualDescendants().OfType<Control>().FirstOrDefault(candidate => candidate.Name == focused) is { } again)
            again.Focus();
    }

    private static IEnumerable<PointsRow> Rows(PointsModel model) => model.Groups.SelectMany(group => group.Rows);

    private static bool Same(PointsModel a, PointsModel b) =>
        a.Heading == b.Heading && a.Note == b.Note && a.Empty == b.Empty && a.Columns.SequenceEqual(b.Columns) &&
        a.Groups.Select(group => (group.Id, group.Title, group.Summary)).SequenceEqual(b.Groups.Select(group => (group.Id, group.Title, group.Summary))) &&
        Rows(a).SequenceEqual(Rows(b));

    private void Render(PointsModel model)
    {
        var empty = this.FindControl<StackPanel>("PointsEmpty")!;
        var head = this.FindControl<StackPanel>("PointsHead")!;
        var groups = this.FindControl<StackPanel>("PointsGroups")!;
        empty.IsVisible = model.Empty is not null;
        head.IsVisible = model.Empty is null;
        this.FindControl<TextBlock>("PointsEmptyTitle")!.Text = model.Empty?.Title ?? "";
        this.FindControl<TextBlock>("PointsEmptyBody")!.Text = model.Empty?.Body ?? "";
        var heading = this.FindControl<TextBlock>("PointsHeading")!;
        heading.Text = model.Heading ?? "";
        heading.IsVisible = model.Heading is not null;
        var note = this.FindControl<TextBlock>("PointsNote")!;
        note.Text = model.Note ?? "";
        note.IsVisible = model.Note is not null;
        var columns = this.FindControl<Grid>("PointsColumns")!;
        columns.Children.Clear();
        for (int index = 0; index < model.Columns.Count; index++)
        {
            var column = new TextBlock { Text = model.Columns[index] };
            column.Classes.Add("pts-col");
            if (index is 2 or 3) column.TextAlignment = Avalonia.Media.TextAlignment.Right;
            Grid.SetColumn(column, index);
            columns.Children.Add(column);
        }
        groups.Children.Clear();
        foreach (var group in model.Groups) groups.Children.Add(Group(group));
    }

    private Control Group(PointsGroup group)
    {
        bool open = !collapsed.Contains(group.Id);
        var header = new ToggleButton
        {
            Name = "PointsGroup_" + group.Id,
            IsChecked = open,
            Content = new DockPanel
            {
                Children =
                {
                    new TextBlock { Text = open ? "⌄" : "›", Classes = { "pts-note" }, Width = 12 },
                    new TextBlock { Text = group.Title, Classes = { "pts-heading" } },
                    new TextBlock { Text = group.Summary, Classes = { "pts-note" }, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                }
            }
        };
        header.Classes.Add("pts-group");
        AutomationProperties.SetName(header, group.Title);
        header.Click += (_, _) =>
        {
            if (!collapsed.Remove(group.Id)) collapsed.Add(group.Id);
            if (shown is not null) Render(shown);
        };
        var panel = new StackPanel { Children = { header } };
        if (open) foreach (var row in group.Rows) panel.Children.Add(Row(row));
        return panel;
    }

    private static string RowKey(PointRef target) => target.Curve + "_" + target.VertexId;

    private Control Row(PointsRow row)
    {
        string key = RowKey(row.Target);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,3*,3*,3*,3*"), RowDefinitions = new RowDefinitions("Auto,Auto") };
        var point = new Button { Name = "PointButton_" + key, Content = (row.Selected || row.Partner ? "⇄ " : "") + row.Point };
        point.Classes.Add("pts-point");
        AutomationProperties.SetName(point, row.Name + (row.Selected ? ", selected" : row.Partner ? ", paired" : ""));
        point.Click += (_, _) => SelectRow(row);
        grid.Children.Add(point);
        grid.Children.Add(Cell(row.Type, 1, false, true));
        grid.Children.Add(row.XEditable ? Field(row, key, isX: true) : Cell(row.X, 2, true, true));
        grid.Children.Add(row.YEditable ? Field(row, key, isX: false) : Cell(row.Y, 3, true, true));
        grid.Children.Add(Cell(row.Kind ?? "", 4, false, false));
        var error = new TextBlock { Name = "PointError_" + key, IsVisible = false };
        error.Classes.Add("pts-error");
        AutomationProperties.SetLiveSetting(error, AutomationLiveSetting.Assertive);
        Grid.SetRow(error, 1);
        Grid.SetColumnSpan(error, 5);
        if (errors.TryGetValue(key, out var held))
        {
            error.Text = held.Error;
            error.IsVisible = true;
        }
        grid.Children.Add(error);
        var border = new Border { Name = "PointRow_" + key, Child = grid, Tag = row };
        border.Classes.Add("pts-row");
        border.Classes.Set("on", row.Selected);
        border.Classes.Set("pair", row.Partner);
        border.Classes.Set("error", errors.ContainsKey(key));
        // DR-DEN-1: the whole 24 px row is the target; a press anywhere on it selects the point.
        border.AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (e.Source is TextBox) return;
            SelectRow(row);
        }, RoutingStrategies.Bubble);
        return border;
    }

    private static TextBlock Cell(string text, int column, bool number, bool readOnly)
    {
        var cell = new TextBlock { Text = text };
        cell.Classes.Add("pts-cell");
        cell.Classes.Set("num", number);
        cell.Classes.Set("ro", readOnly && number);
        Grid.SetColumn(cell, column);
        return cell;
    }

    private TextBox Field(PointsRow row, string key, bool isX)
    {
        string axis = isX ? "x" : "y";
        var box = new TextBox
        {
            Name = (isX ? "PointX_" : "PointY_") + key,
            Text = errors.TryGetValue(key + axis, out var held) ? held.Text : Quantity.ForField(isX ? row.X : row.Y),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        box.Classes.Add("prop-b");
        box.Classes.Set("error", errors.ContainsKey(key + axis));
        string unit = shown?.Section == true ? "% chord" : isX ? "millimetres" : "the curve's unit";
        AutomationProperties.SetName(box, $"{row.Name}, {(shown?.Section == true ? axis : isX ? "From root" : "value")} in {unit}");
        Grid.SetColumn(box, isX ? 2 : 3);
        box.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Pending = CommitAsync(row, key, isX, box.Text);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                errors.Remove(key + axis);
                errors.Remove(key);
                box.Text = Quantity.ForField(isX ? row.X : row.Y);
                shown = null;
                if (controller is not null) Bind(controller);
            }
        }, RoutingStrategies.Tunnel);
        return box;
    }

    private void SelectRow(PointsRow row)
    {
        if (controller is null) return;
        controller.Select(new Selection.Points([row.Target]));
    }

    /// <summary>
    /// One typed value: in the section mode one step (§11.4), in the views one undo step. An invalid value is a field
    /// error at the field (UI-39) and makes no step.
    /// </summary>
    private async Task CommitAsync(PointsRow row, string key, bool isX, string? text)
    {
        if (controller is not { } bound) return;
        string axis = isX ? "x" : "y";
        string? refusal;
        string? echo = null;
        if (shown?.Section == true)
        {
            var curve = bound.SectionCurve(SectionPoints.Side(row.Target.Curve));
            var point = curve?.Points.FirstOrDefault(item => item.Id == row.Target.VertexId);
            if (point is null || bound.Section is not { } mode) return;
            double chord = Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment).StationChordMeters;
            refusal = SectionPoints.Parse(text, isX, chord, out double fraction, out echo);
            if (refusal is null)
                refusal = await SectionPoints.CommitAsync(bound, row.Target, isX ? fraction : point.SpanMeters, isX ? point.Ordinate : fraction);
        }
        else
        {
            var point = PropertiesView.Find(bound.CurveFor, row.Target);
            var rows = PropertiesView.Curves.GetValueOrDefault(row.Target.Curve);
            if (point is null || rows is null) return;
            var family = isX ? UnitFamily.Length : rows.ValueFamily;
            string label = isX ? "From root" : rows.ValueLabel;
            if (!UnitEntry.TryParse(text, family, new Dictionary<string, double>(), out var entry))
                refusal = PropertyCopy.NotANumber(label);
            else
            {
                double value = entry.Value / PropertiesView.FieldScale[family];
                refusal = await ViewCommitAsync(bound, row.Target, isX ? value : point.SpanMeters, isX ? point.Ordinate : value);
            }
        }
        if (refusal is not null)
        {
            errors[key + axis] = (text ?? "", refusal);
            errors[key] = (text ?? "", refusal);
        }
        else
        {
            errors.Remove(key + axis);
            errors.Remove(key);
        }
        shown = null;   // a refusal changes no row, so the field error is drawn by a forced render
        Bind(bound);
        if (refusal is null && echo is not null) Reported?.Invoke(new StatusReport(echo));
    }

    private static async Task<string?> ViewCommitAsync(WorkbenchController bound, PointRef target, double span, double value)
    {
        var selection = bound.Selection;
        if (!bound.BeginGesture(target, GestureInput.Typed)) return bound.Status;
        bound.Select(selection);
        bound.UpdateGesture(span, value);
        bound.FlushGestureFrame();
        var outcome = await bound.EndGestureAsync(GestureEnd.Release);
        return outcome is GestureOutcome.Refused refused ? refused.Copy : null;
    }
}
