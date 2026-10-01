using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Panes;

/// <summary>
/// The field nudge (DR-UID-2) ships disabled until its VoiceOver trace passes and the macOS caret chords are ruled (B7).
/// It stays off on Windows until a Narrator/UIA pass exists (B8), whatever <see cref="Enabled"/> says.
/// </summary>
public static class PropertiesFieldNudge
{
    public static bool Enabled { get; set; }
    public static bool OnWindows { get; set; } = OperatingSystem.IsWindows();
    public static bool Active => Enabled && !OnWindows;
}

/// <summary>
/// The Properties pane as a property grid (docs/reviews/ui-property-grid.md §10). It renders <see cref="PropertiesModel"/>
/// generically: a selection identity, collapsible groups and label | value | unit rows on one shared column, with the Wing
/// pinned at the foot (DR-UID-5). Editors persist per row key so a re-render never moves focus (UI-C).
/// </summary>
public partial class PropertiesPane : UserControl
{
    private const double WingShare = 0.55;   // DR-UID-5: the Wing keeps at most 55 % of the pane's height

    private readonly Dictionary<string, TextBox> pooledInputs;
    private readonly Dictionary<string, RowView> rows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBlock> subheads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GroupView> groupViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TextBlock>> noteViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> collapsed = new(StringComparer.Ordinal) { ["rail"] = true };
    private readonly Dictionary<TextBox, RowView> inputOwners = [];
    private readonly Dictionary<TextBox, string> shown = [];
    private readonly Dictionary<string, RowMessage> messages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Text, string Error)> errors = new(StringComparer.Ordinal);
    private readonly RadioButton[] kindButtons;

    private WorkbenchController? boundController;
    private string selectionKey = "";
    private int holds;
    private bool bindPending;
    private bool rendering;
    private bool narrow;
    private bool syncingChoice;
    private bool resumingRecovery;
    private string? pendingType;
    private TangentKind? pendingKind;
    private NudgeRun? run;
    private string? copyTarget;
    private PropertiesModel? shownModel;
    private string? lastAvailability;

    public PropertiesPane()
    {
        InitializeComponent();
        pooledInputs = new Dictionary<string, TextBox>(StringComparer.Ordinal)
        {
            ["w:span"] = SpanInput,
            ["w:root"] = RootChordInput,
            ["w:tip"] = TipChordInput,
            ["p:from"] = PointSpanInput,
            ["p:aft"] = PointAftInput,
            ["h:angle"] = HandleAngleInput,
            ["h:length"] = HandleLengthInput
        };
        kindButtons = [TangentSmoothButton, TangentSymmetricButton, TangentCornerButton];
        foreach (var box in pooledInputs.Values) box.IsEnabled = false;

        TryAgainButton.Click += (_, _) =>
        {
            ErrorPanel.IsVisible = false;
            if (boundController != null) Bind(boundController);
        };
        HowMeasuredButton.Click += (_, _) =>
        {
            // PG-31: a disclosure whose checked state is its expanded state.
            HowMeasuredBody.IsVisible = !HowMeasuredBody.IsVisible;
            HowMeasuredButton.IsChecked = HowMeasuredBody.IsVisible;
        };
        RecoveryApplyButton.Click += (_, _) => ApplyRecovery();
        RecoveryDiscardButton.Click += (_, _) => boundController?.DiscardRecovery();
        IdentityCrumbLink.Click += (_, _) => GoToCrumb();

        TypeControl.SelectionChanged += OnTypeSelectionChanged;
        TypeControl.DropDownClosed += (_, _) => OnTypeDropDownClosed();
        TypeControl.AddHandler(KeyDownEvent, OnTypeKeyDown, RoutingStrategies.Tunnel);
        TypeControl.LostFocus += (_, _) => DropPendingType();

        foreach (var button in kindButtons)
            button.Click += (_, _) => CommitKind(KindOf(button));
        TangentGroup.AddHandler(KeyDownEvent, OnKindKeyDown, RoutingStrategies.Tunnel);
        TangentGroup.LostFocus += (_, _) => Dispatcher.UIThread.Post(CommitKindOnLeave, DispatcherPriority.Input);

        AddHandler(KeyDownEvent, OnPaneKeyDown, RoutingStrategies.Bubble);
        // PG-25: a row the pointer chose is the Copy target only until focus moves.
        AddHandler(GotFocusEvent, (_, _) => copyTarget = null, RoutingStrategies.Bubble, handledEventsToo: true);
        SizeChanged += (_, _) => FitToPane();
    }

    /// <summary>Every announcement the pane makes: an error (assertive) once per failed commit, a report (polite).</summary>
    public event Action<string, AutomationLiveSetting>? Announced;

    /// <summary>Where the Copy command writes; null writes to the window's clipboard.</summary>
    public Func<string, Task>? ClipboardWriter { get; set; }

    public void FocusTypeValue() => PointSpanInput.Focus();

    public void ShowRenderFailure(bool foilOpen)
    {
        ErrorText.Text = "Properties couldn't be shown." + (foilOpen ? " Your foil hasn't changed." : "");
        ErrorPanel.IsVisible = true;
    }

    public void Bind(WorkbenchController controller, WingEstimates? projectedEstimates = null)
    {
        boundController = controller;
        if (holds > 0)
        {
            bindPending = true;
            return;
        }
        try
        {
            ErrorPanel.IsVisible = false;
            var authored = controller.CurrentProjection;
            if (authored is null)
            {
                EmptyPanel.IsVisible = true;
                ContentPanel.IsVisible = false;
                foreach (var box in pooledInputs.Values) box.IsEnabled = false;
                return;
            }
            ResumeRecoveryIfNeeded(controller);
            var estimates = projectedEstimates ?? controller.Estimates;
            var plan = controller.Planform;
            var context = new PropertiesContext(
                plan,
                Preview: plan?.Basis == "preview" || estimates?.Basis == "preview",
                Checking: controller.Gesture == GestureState.Busy,
                NotChecked: controller.Inspection is { } inspection && inspection.Geometry.Status != GeometryStatus.Certified);
            string key = SelectionKey(controller.Selection);
            if (key != selectionKey)
            {
                selectionKey = key;
                copyTarget = null;
                messages.Clear();
                errors.Clear();
                pendingType = null;
                pendingKind = null;
            }
            var model = PropertiesView.Build(controller.Selection, authored, estimates, ShellMode.Workspace, context);
            EmptyPanel.IsVisible = false;
            ContentPanel.IsVisible = true;
            Render(model, controller);
        }
        catch (Exception ex)
        {
            BlocksPanel.Children.Clear();
            ContentPanel.IsVisible = false;
            EmptyPanel.IsVisible = false;
            ShowRenderFailure(controller?.Inspection is not null);
            ShellEvents.Record("shell.pane.render", "error", 0, "pane-bind", exceptionType: ex.GetType().Name);
        }
    }

    /// <summary>Commits the Wing's Span field (COPY-106 / COPY-118 on refusal). Kept for the shell's edit routing.</summary>
    public bool CommitSpan()
    {
        if (boundController is null) return false;
        // With no foil open the field still answers: the controller refuses, and the refusal is shown (COPY-145).
        string typed = SpanInput.Text ?? "";
        var view = rows.GetValueOrDefault("w:span|Input") ?? RenderRow(PropertiesView.SpanField(double.NaN), []);
        SpanInput.Text = typed;
        return CommitWing(view, SpanInput);
    }

    /// <summary>The text the Copy command puts on the clipboard for a fact or estimate row (PG-20 / D2).</summary>
    public static string CopyText(PropertyRow row, bool withUnit) =>
        withUnit && row.Unit is { } unit && row.State is not (RowState.Mixed or RowState.Unavailable)
            ? (unit == "°" ? row.Value + "°" : row.Value + " " + unit)
            : row.Value;

    // ---------------- rendering ----------------

    private void Render(PropertiesModel model, WorkbenchController controller)
    {
        rendering = true;
        try
        {
            RenderIdentity(model.Identity);
            AlertBanner.IsVisible = model.Banner is not null;
            AlertBannerText.Text = model.Banner?.Text ?? "";
            bool recovery = controller.HasRecovery;
            RecoveryPanel.IsVisible = recovery;
            RecoveryBanner.IsVisible = recovery;
            if (recovery) RecoveryBanner.Text = "A recovered edit is open.";
            shownModel = model;
            // COPY-160: a change in availability is announced once on the status line, never on a re-render.
            if (model.AvailabilityStatus != lastAvailability && model.AvailabilityStatus is { } availability)
                Announced?.Invoke(availability, AutomationLiveSetting.Polite);
            lastAvailability = model.AvailabilityStatus;

            var used = new HashSet<Control>();
            var groupControls = new List<Control>();
            foreach (var group in model.Groups)
                groupControls.Add(RenderGroup(group, used));
            Sync(BlocksPanel, groupControls);
            if (model.Wing is { } wing) RenderWing(wing, used);
            // A pooled editor this selection does not show is neither enabled nor visible, wherever it was last placed.
            foreach (var control in pooledInputs.Values.Cast<Control>().Append(TypeControl).Append(TangentGroup).Where(control => !used.Contains(control)))
            {
                control.IsEnabled = false;
                control.IsVisible = false;
            }
            FitToPane();
        }
        finally { rendering = false; }
    }

    private void RenderIdentity(SelectionIdentity? identity)
    {
        IdentityPanel.IsVisible = identity is not null;
        if (identity is null) return;
        IdentityTitle.Text = identity.Title;
        IdentityGlyphPath.Data = Avalonia.Media.Geometry.Parse(GlyphPath(identity.Glyph));
        IdentityGlyphPath.Classes.Set("filled", identity.Glyph is IdentityGlyph.Control or IdentityGlyph.Several);
        IdentityGlyphPath.StrokeDashArray = identity.Glyph == IdentityGlyph.Station ? [2, 2] : null;
        AutomationProperties.SetAccessibilityView(IdentityGlyphPath, AccessibilityView.Raw);
        bool link = identity.CrumbTarget is not null && identity.Crumb is not null;
        IdentityCrumbLink.IsVisible = link;
        IdentityCrumbLink.Content = link ? $"of {identity.Crumb} · Esc" : null;
        IdentityCrumbLink.Tag = identity.CrumbTarget;
        IdentityCrumb.IsVisible = !link && identity.Crumb is not null;
        IdentityCrumb.Text = link ? "" : identity.Crumb ?? "";
    }

    private Control RenderGroup(PropertyGroup group, HashSet<Control> used)
    {
        if (!groupViews.TryGetValue(group.Id, out var view))
            groupViews[group.Id] = view = CreateGroup(group.Id);
        view.Title.Text = group.Title;
        view.Summary.Text = group.Summary;
        bool expanded = !collapsed.GetValueOrDefault(group.Id);
        if (view.Expander.IsExpanded != expanded) view.Expander.IsExpanded = expanded;
        view.Summary.IsVisible = !expanded && group.Summary.Length > 0;
        AutomationProperties.SetName(view.Expander, group.Title);
        AutomationProperties.SetHelpText(view.Expander, group.Summary);
        if (view.Header is { } header)
        {
            AutomationProperties.SetName(header, group.Title);
            AutomationProperties.SetHelpText(header, group.Summary);
        }
        Sync(view.Body, BodyControls(group, used));
        return view.Expander;
    }

    private List<Control> BodyControls(PropertyGroup group, HashSet<Control> used)
    {
        var body = new List<Control>();
        var notes = Notes(group.Id, group.Lead is null ? group.Notes : [group.Lead, .. group.Notes]);
        if (group.Lead is not null) body.Add(notes[0]);
        foreach (var row in group.Rows)
        {
            if (row.Subhead is { } subhead)
            {
                if (!subheads.TryGetValue(row.Key, out var title))
                {
                    subheads[row.Key] = title = new TextBlock { Name = Part("Subhead", row.Key) };
                    title.Classes.Add("prop-subhead");
                }
                title.Text = subhead;
                body.Add(title);
            }
            body.Add(RenderRow(row, used).Root);
        }
        body.AddRange(group.Lead is null ? notes : notes.Skip(1));
        return body;
    }

    private void RenderWing(PropertyGroup wing, HashSet<Control> used)
    {
        WingHeading.Text = wing.Title;
        WingChip.IsVisible = wing.Chip is not null;
        WingChipText.Text = wing.Chip switch
        {
            GroupChip.Preview => "≈ preview",
            GroupChip.Checking => "Checking…",
            GroupChip.Unavailable => "Unavailable",
            _ => ""
        };
        Sync(WingDimensions, wing.Rows.Where(row => row.Key.StartsWith("w:", StringComparison.Ordinal))
            .Select(row => (Control)RenderRow(row, used).Root).ToList());
        Sync(WingEstimates, wing.Rows.Where(row => !row.Key.StartsWith("w:", StringComparison.Ordinal))
            .Select(row => (Control)RenderRow(row, used).Root).ToList());
        // PG-27: the first Wing note stays attached (hidden while empty) so a change is only a text change.
        var notes = Notes("wing", wing.Notes.Count > 0 ? wing.Notes : [new RowMessage("", MessageKind.Info)]);
        foreach (var note in notes)
        {
            AutomationProperties.SetLiveSetting(note, AutomationLiveSetting.Polite);
            note.IsVisible = note.Text?.Length > 0;
        }
        Sync(WingNotes, [.. notes]);
    }

    private List<TextBlock> Notes(string groupId, IReadOnlyList<RowMessage> notes)
    {
        if (!noteViews.TryGetValue(groupId, out var views)) noteViews[groupId] = views = [];
        while (views.Count < notes.Count)
        {
            var block = new TextBlock { Name = $"Note_{groupId}_{views.Count}" };
            block.Classes.Add("prop-note");
            views.Add(block);
        }
        for (int index = 0; index < notes.Count; index++)
        {
            if (views[index].Text != notes[index].Text) views[index].Text = notes[index].Text;
            views[index].Classes.Set("warning", notes[index].Kind == MessageKind.Warning);
        }
        return views.Take(notes.Count).ToList();
    }

    private RowView RenderRow(PropertyRow row, HashSet<Control> used)
    {
        string cacheKey = row.Key + "|" + row.Kind;
        if (!rows.TryGetValue(cacheKey, out var view)) rows[cacheKey] = view = CreateRow(row);
        view.Row = row;
        view.Label.Text = row.Label;
        SetColumns(view.Grid);
        bool hasError = errors.TryGetValue(row.Key, out var error);
        var message = hasError ? new RowMessage(error.Error, MessageKind.Error)
            : messages.GetValueOrDefault(row.Key) ?? row.Message;
        var state = hasError ? RowState.Error
            : message?.Kind == MessageKind.Warning && row.State == RowState.Normal ? RowState.Warning
            : row.State;
        view.Root.Classes.Set("warning", state == RowState.Warning);
        view.Root.Classes.Set("error", state == RowState.Error);
        view.Root.Classes.Set("unavailable", state == RowState.Unavailable);
        view.Description.IsVisible = row.Description is not null;
        view.Description.Text = row.Description ?? "";
        ShowMessage(view, message);
        bool showUnit = row.State is not (RowState.Mixed or RowState.Unavailable);
        view.Unit.Text = showUnit ? row.Unit ?? "" : "";

        switch (row.Kind)
        {
            case RowKind.Input:
                RenderInput(view, row, hasError ? error.Text : null, used);
                break;
            case RowKind.Choice:
                used.Add(TypeControl);
                TypeControl.IsVisible = true;
                RenderChoice(view, row);
                break;
            case RowKind.KindList:
                used.Add(TangentGroup);
                TangentGroup.IsVisible = true;
                TangentGroup.IsEnabled = true;
                RenderKinds(view, row);
                break;
            default:
                string text = row.Kind == RowKind.Estimate && row.State == RowState.Normal ? "≈ " + row.Value : row.Value;
                if (view.Value!.Text != text) view.Value.Text = text;
                AutomationProperties.SetName(view.Root, row.SpokenText);
                AutomationProperties.SetHelpText(view.Root, row.HelperText);
                break;
        }
        return view;
    }

    private void RenderInput(RowView view, PropertyRow row, string? invalidText, HashSet<Control> used)
    {
        var box = view.Input!;
        used.Add(box);
        inputOwners[box] = view;
        box.IsEnabled = true;
        box.IsVisible = true;
        box.Classes.Set("error", invalidText is not null);
        AutomationProperties.SetName(box, row.AutomationName ?? row.Label);
        AutomationProperties.SetHelpText(box, HelpText(view));
        string modelText = Quantity.ForField(row.Value);
        if (invalidText is not null) return;   // keep what the user typed until Escape or the next commit
        if (run?.Box == box) return;            // a field run owns its text
        if (!box.IsKeyboardFocusWithin || !Dirty(box)) SetShown(box, modelText);
    }

    private void RenderChoice(RowView view, PropertyRow row)
    {
        var options = row.Options ?? [];
        if (TypeControl.ItemCount != options.Count)
            TypeControl.ItemsSource = options.Select(option => new ComboBoxItem { Content = option.Text, Tag = option.Value }).ToList();
        string shownValue = pendingType ?? row.Value;
        int index = options.ToList().FindIndex(option => option.Value == shownValue);
        if (TypeControl.SelectedIndex != index)
        {
            syncingChoice = true;
            try { TypeControl.SelectedIndex = index; }
            finally { syncingChoice = false; }
        }
        TypeControl.IsEnabled = true;
        AutomationProperties.SetName(TypeControl, row.AutomationName ?? row.Label);
        string committedText = options.FirstOrDefault(option => option.Value == row.Value)?.Text ?? row.Value;
        AutomationProperties.SetHelpText(TypeControl, pendingType is not null
            ? $"Return applies; Esc keeps {committedText.ToLowerInvariant()}" : row.Description);
        if (pendingType is not null) ShowMessage(view, new RowMessage(PropertyCopy.PendingType, MessageKind.Info));
    }

    private void RenderKinds(RowView view, PropertyRow row)
    {
        var committed = Enum.Parse<TangentKind>(row.Value);
        var shownKind = pendingKind ?? committed;
        AutomationProperties.SetName(TangentGroup, row.AutomationName);
        AutomationProperties.SetControlTypeOverride(TangentGroup, AutomationControlType.Group);
        AutomationProperties.SetHelpText(TangentGroup, PropertyCopy.KindDescription(shownKind));
        for (int index = 0; index < kindButtons.Length; index++)
        {
            var button = kindButtons[index];
            bool isChecked = KindOf(button) == shownKind;
            if (button.IsChecked != isChecked) button.IsChecked = isChecked;
            button.IsTabStop = isChecked;   // Tab lands on the checked option (§10.4)
            AutomationProperties.SetPositionInSet(button, index + 1);
            AutomationProperties.SetSizeOfSet(button, kindButtons.Length);
            AutomationProperties.SetHelpText(button, pendingKind is not null
                ? $"Return applies; Esc keeps {committed.ToString().ToLowerInvariant()}" : PropertyCopy.KindDescription(KindOf(button)));
        }
        view.Description.Text = PropertyCopy.KindDescription(shownKind);
        if (pendingKind is { } pending && pending != committed)
            ShowMessage(view, new RowMessage(PropertyCopy.PendingKind(pending.ToString().ToLowerInvariant(),
                committed.ToString().ToLowerInvariant()), MessageKind.Info));
    }

    private static void ShowMessage(RowView view, RowMessage? message)
    {
        view.MessageBox.IsVisible = message is not null;
        string text = message?.Text ?? "";
        if (view.Message.Text != text) view.Message.Text = text;
        view.Message.Classes.Set("error", message?.Kind == MessageKind.Error);
        view.Message.Classes.Set("warning", message?.Kind == MessageKind.Warning);
        var live = message?.Kind == MessageKind.Error ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite;
        AutomationProperties.SetLiveSetting(view.MessageBox, live);
        AutomationProperties.SetLiveSetting(view.Message, live);
    }

    private string HelpText(RowView view)
    {
        var row = view.Row;
        var parts = new List<string>();
        if (row.Nudge && PropertiesFieldNudge.Active && row.Unit is { } unit) parts.Add(PropertyCopy.NudgeHelp(unit));
        if (row.Description is { } description) parts.Add(description);
        if (errors.TryGetValue(row.Key, out var error)) parts.Add(error.Error);
        else if ((messages.GetValueOrDefault(row.Key) ?? row.Message) is { } message) parts.Add(message.Text);
        return string.Join(" ", parts);
    }

    private RowView CreateRow(PropertyRow row)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto") };
        var label = new TextBlock { Name = Part("Label", row.Key) };
        label.Classes.Add("prop-label");
        var unit = new TextBlock { Name = UnitName(row.Key) };
        unit.Classes.Add("prop-unit");
        Grid.SetColumn(unit, 2);
        var description = new TextBlock { Name = Part("Description", row.Key), IsVisible = false };
        description.Classes.Add("prop-note");
        Grid.SetRow(description, 1);
        Grid.SetColumnSpan(description, 3);
        TextBlock messageText;
        Border messageBox;
        if (row.Key == "w:span")
        {
            messageBox = Detach(SpanErrorPanel);
            messageText = SpanErrorText;
        }
        else
        {
            messageText = new TextBlock { Name = MessageName(row.Key) };
            messageText.Classes.Add("prop-message");
            messageBox = new Border { Child = messageText, IsVisible = false };
        }
        Grid.SetRow(messageBox, 2);
        Grid.SetColumnSpan(messageBox, 3);
        var root = new Border { Child = grid, Name = Part("Row", row.Key) };
        root.Classes.Add("prop-row");
        var view = new RowView(root, grid, label, unit, description, messageBox, messageText) { Row = row };

        Control value;
        switch (row.Kind)
        {
            case RowKind.Input:
                var box = pooledInputs.TryGetValue(row.Key, out var pooled) ? Detach(pooled) : new TextBox { Name = Part("Input", row.Key) };
                box.Classes.Add("prop-input");
                WireInput(box);
                view.Input = box;
                value = box;
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                AutomationProperties.SetAccessibilityView(unit, AccessibilityView.Raw);   // the name carries the unit (PG-01)
                break;
            case RowKind.Choice:
                value = Detach(TypeControl);
                Grid.SetColumnSpan(value, 2);
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                description.Name = row.Key == "p:type" ? "PointHelper" : description.Name;
                break;
            case RowKind.KindList:
                value = Detach(TangentGroup);
                Grid.SetColumnSpan(value, 2);
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                label.Name = "TangentLabel";
                break;
            default:
                var text = new TextBlock { Name = ValueName(row.Key) };
                text.Classes.Add("prop-value");
                view.Value = text;
                value = text;
                // PG-20 / D2: a fact is not a Tab stop; its container speaks one line and its parts are Raw (B9).
                foreach (var part in new Control[] { label, text, unit, description })
                    AutomationProperties.SetAccessibilityView(part, AccessibilityView.Raw);
                AutomationProperties.SetAccessibilityView(root, AccessibilityView.Content);
                AutomationProperties.SetControlTypeOverride(root, AutomationControlType.Text);
                root.ContextMenu = CopyMenu(row.Key);
                root.PointerPressed += (_, _) => copyTarget = row.Key + "|" + row.Kind;
                break;
        }
        Grid.SetColumn(value, 1);
        grid.Children.AddRange([label, value, unit, description, messageBox]);
        return view;
    }

    private ContextMenu CopyMenu(string key)
    {
        var menu = new ContextMenu { Name = Part("CopyMenu", key) };
        foreach (var (header, withUnit) in new[] { ("Copy value", false), ("Copy value with unit", true) })
        {
            var item = new MenuItem { Header = header };
            item.Click += (_, _) => CopyRow(key, withUnit);
            menu.Items.Add(item);
        }
        return menu;
    }

    private void CopyRow(string key, bool withUnit)
    {
        var view = rows.Values.FirstOrDefault(item => item.Row.Key == key && item.Value is not null);
        if (view is null) return;
        string text = CopyText(view.Row, withUnit);
        var write = ClipboardWriter ?? (value => TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(value) ?? Task.CompletedTask);
        _ = write(text);
    }

    private GroupView CreateGroup(string id)
    {
        var title = new TextBlock { Name = Part("GroupTitle", id) };
        title.Classes.Add("prop-group-title");
        var summary = new TextBlock { Name = Part("GroupSummary", id) };
        summary.Classes.Add("prop-summary");
        var header = new StackPanel { Orientation = Orientation.Horizontal, Children = { title, summary } };
        AutomationProperties.SetAccessibilityView(title, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(summary, AccessibilityView.Raw);
        var body = new StackPanel();
        body.Classes.Add("prop-body");
        var expander = new Expander
        {
            Name = Part("Group", id),
            Header = header,
            Content = body,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ContentTransition = null   // PG-16 / B6: no native motion
        };
        expander.Classes.Add("prop-group");
        // PG-25: a header's menu (Shift+F10 or the menu key on a focused header) copies its group as text.
        var copy = new MenuItem { Header = "Copy values" };
        copy.Click += (_, _) => CopyGroup(id);
        expander.ContextMenu = new ContextMenu { Name = Part("GroupMenu", id), Items = { copy } };
        var view = new GroupView(expander, title, summary, body);
        expander.TemplateApplied += (_, args) =>
        {
            view.Header = args.NameScope.Find<ToggleButton>("ExpanderHeader")
                ?? expander.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault();
            if (view.Header is { } toggle)
            {
                AutomationProperties.SetName(toggle, title.Text);
                AutomationProperties.SetHelpText(toggle, summary.Text);
                toggle.TemplateApplied += (_, _) => StripMotion(toggle);
            }
            StripMotion(expander);
        };
        expander.Expanded += (_, _) => Toggled(id, view, expanded: true);
        expander.Collapsed += (_, _) => Toggled(id, view, expanded: false);
        return view;
    }

    private void Toggled(string id, GroupView view, bool expanded)
    {
        if (rendering) return;
        collapsed[id] = !expanded;
        view.Summary.IsVisible = !expanded && view.Summary.Text?.Length > 0;
    }

    /// <summary>B6: no transition anywhere in a group's template (the Fluent chevron and content motion).</summary>
    private static void StripMotion(Control root)
    {
        root.Transitions = null;
        foreach (var visual in root.GetVisualDescendants().OfType<Animatable>())
            visual.Transitions = null;
    }

    private void FitToPane()
    {
        if (Bounds.Height > 0) WingBlock.MaxHeight = Bounds.Height * WingShare;
        bool now = Bounds.Width > 0 && Bounds.Width < Token("PropNarrowPaneWidth");
        if (now == narrow) return;
        narrow = now;
        foreach (var view in rows.Values) SetColumns(view.Grid);
    }

    private void SetColumns(Grid grid)
    {
        double label = Token(narrow ? "PropLabelNarrowWidth" : "PropLabelWidth");
        double unit = Token(narrow ? "PropUnitNarrowWidth" : "PropUnitWidth");
        if (grid.ColumnDefinitions.Count == 3 && grid.ColumnDefinitions[0].Width.Value == label &&
            grid.ColumnDefinitions[2].Width.Value == unit) return;
        grid.ColumnDefinitions = new ColumnDefinitions
        {
            new(label, GridUnitType.Pixel),
            new(1, GridUnitType.Star),
            new(unit, GridUnitType.Pixel)
        };
    }

    // A pane bound before it is attached reads the application's tokens; it re-reads them on its first render attached.
    private double Token(string key) =>
        (this.TryFindResource(key, out var value) || Application.Current?.TryFindResource(key, out value) == true) && value is double number
            ? number : 0;

    // ---------------- inputs: commit, refuse, escape, nudge ----------------

    private void WireInput(TextBox box)
    {
        // Tunnel: the field sees Return, Tab, Esc and the run arrows before TextBox moves the caret with them.
        box.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        box.AddHandler(KeyUpEvent, OnInputKeyUp, RoutingStrategies.Tunnel);
        box.LostFocus += OnInputLostFocus;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || !inputOwners.TryGetValue(box, out var view)) return;
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                Commit(view, box);
                break;
            case Key.Tab:
                e.Handled = true;
                if (Dirty(box) && !RefusedAlready(view, box)) Commit(view, box);
                var direction = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? NavigationDirection.Previous : NavigationDirection.Next;
                (TopLevel.GetTopLevel(this) as IInputRoot)?.KeyboardNavigationHandler?.Move(box, direction, e.KeyModifiers);
                break;
            case Key.Escape:
                e.Handled = true;
                Escape(view, box);
                break;
            case Key.Up or Key.Down when view.Row.Nudge && PropertiesFieldNudge.Active && !Dirty(box) && !errors.ContainsKey(view.Row.Key):
                e.Handled = true;
                NudgeStep(view, box, e.Key == Key.Up ? 1 : -1, e.KeyModifiers);
                break;
        }
    }

    private void OnInputKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down) || run is not { } active || !ReferenceEquals(active.Box, sender)) return;
        e.Handled = true;
        EndRun(GestureEnd.KeyUp);
    }

    private void OnInputLostFocus(object? sender, RoutedEventArgs e)
    {
        if (rendering || sender is not TextBox box || !inputOwners.TryGetValue(box, out var view)) return;
        if (run?.Box == box)
        {
            EndRun(GestureEnd.KeyUp);
            return;
        }
        // UI-39: leaving a field commits it; focus goes where it was sent.
        if (Dirty(box) && !RefusedAlready(view, box)) Commit(view, box);
    }

    private bool RefusedAlready(RowView view, TextBox box) =>
        errors.TryGetValue(view.Row.Key, out var error) && error.Text == (box.Text ?? "");

    private void Escape(RowView view, TextBox box)
    {
        if (run?.Box == box)
        {
            EndRun(GestureEnd.Escape);
            return;
        }
        if (Dirty(box) || errors.ContainsKey(view.Row.Key))
        {
            errors.Remove(view.Row.Key);
            box.Text = shown.GetValueOrDefault(box, Quantity.ForField(view.Row.Value));
            if (boundController is { } controller) Bind(controller);
            return;
        }
        // A second Escape returns focus to the canvas target (§10.4).
        if (view.Row.Target is not null)
            this.FindAncestorOfType<ShellHost>()?.ModelView.PlanCanvas.Focus();
    }

    private void Commit(RowView view, TextBox box)
    {
        if (view.Row.Key.StartsWith("w:", StringComparison.Ordinal)) CommitWing(view, box);
        else CommitPoint(view, box);
    }

    private bool CommitWing(RowView view, TextBox box)
    {
        if (boundController is not { } controller) return false;
        var row = view.Row;
        string text = (box.Text ?? "").Trim();
        if (text.Length == 0) return Refuse(view, box, PropertyCopy.NotANumber(row.Label));
        bool parsed = UnitEntry.TryParse(text, UnitFamily.Length, Dimensions(), out var entry);
        Task<CommitOutcome> task = row.Key == "w:span"
            ? controller.ApplySpanAsync(text)
            : controller.ApplyChordAsync(row.Key == "w:root" ? "root-chord" : "tip-chord", text);
        using (Hold()) PumpUi(task);
        var outcome = task.IsCompletedSuccessfully ? task.Result : new CommitOutcome.Refused("DSL-NOT-ASSESSED", "");
        if (outcome is CommitOutcome.Committed committed)
        {
            errors.Remove(row.Key);
            bool aboveLimit = committed.Report.Contains("above the limit", StringComparison.Ordinal);
            string? echo = parsed ? UnitEntry.Echo(text, entry with { Value = CommittedWingValue(row.Key, entry.Value) }, "mm") : null;
            if (aboveLimit) messages[row.Key] = new RowMessage(committed.Report, MessageKind.Warning);
            else if (echo is not null) messages[row.Key] = new RowMessage(echo, MessageKind.Echo);
            else messages.Remove(row.Key);
            shown[box] = box.Text ?? "";
            Bind(controller);
            return true;
        }
        string code = ((CommitOutcome.Refused)outcome).Code;
        if (code == "DSL-TARGET" && row.Key == "w:tip")
        {
            messages[row.Key] = new RowMessage(PropertyCopy.TipCloses, MessageKind.Reason);
            return Refuse(view, box, PropertyCopy.TipCloses);
        }
        return Refuse(view, box, code switch
        {
            "DSL-UNIT" => PropertyCopy.NotPositive(row.Label),
            "DSL-EDGES-CROSS" => "That would make the leading and trailing edges cross. Enter a different value.",
            "DSL-NOT-ASSESSED" => $"The new {row.Label.ToLowerInvariant()} couldn't be checked. {row.Label} is unchanged. Try again or enter a different value.",
            _ => PropertyCopy.NotANumber(row.Label)
        });
    }

    private double CommittedWingValue(string key, double typed)
    {
        var estimates = boundController?.Estimates;
        double? meters = key switch
        {
            "w:span" => estimates?.SpanMeters,
            "w:root" => estimates?.RootChordMeters,
            "w:tip" => estimates?.TipChordMeters,
            _ => null
        };
        return meters is double value ? value * 1000 : typed;
    }

    private bool CommitPoint(RowView view, TextBox box)
    {
        if (boundController is not { } controller || controller.Planform is not { } plan || view.Row.Target is not { } target) return false;
        if (PropertiesView.Find(plan, target) is not { } point) return false;
        var dims = Dimensions();
        var echoes = new Dictionary<string, string>(StringComparer.Ordinal);
        (double Span, double Aft) destination;
        if (view.Row.Key is "p:from" or "p:aft")
        {
            // One intent, one undo row: a dirty From root and Aft commit together.
            double span = point.SpanMeters, aft = point.AftMeters;
            foreach (var other in new[] { "p:from", "p:aft" })
            {
                if (!rows.TryGetValue(other + "|Input", out var field) || field.Input is not { } input) continue;
                bool include = field == view || input.IsEnabled && Dirty(input);
                if (!include) continue;
                if (!Parse(field, input, dims, out double value)) return false;
                if (other == "p:from") span = value / 1000; else aft = value / 1000;
                if (UnitEntry.TryParse(input.Text, UnitFamily.Length, dims, out var entry) && UnitEntry.Echo(input.Text!, entry, "mm") is { } echo)
                    echoes[other] = echo;
            }
            destination = (span, aft);
        }
        else
        {
            if (!Parse(view, box, dims, out double value)) return false;
            var anchor = PropertiesView.Rail(plan, point.Curve)!.Points.First(item => item.Id == point.AnchorId);
            var (angle, length) = PropertiesView.HandleGeometry(point, anchor);
            bool isAngle = view.Row.Family == UnitFamily.Angle;
            destination = CfdWorkbench.Core.Planform.HandleTarget(plan, point.Curve, point.Id,
                isAngle ? value : angle, isAngle ? length : value / 1000);
            if (UnitEntry.TryParse(box.Text, view.Row.Family, dims, out var entry) &&
                UnitEntry.Echo(box.Text!, entry, view.Row.Unit ?? "") is { } echo)
                echoes[view.Row.Key] = echo;
        }
        var outcome = RunTypedGesture(controller, target, destination);
        switch (outcome)
        {
            case GestureOutcome.Committed:
                foreach (var key in new[] { view.Row.Key, "p:from", "p:aft" }) { errors.Remove(key); messages.Remove(key); }
                foreach (var (key, echo) in echoes) messages[key] = new RowMessage(echo, MessageKind.Echo);
                MarkShown(view.Row.Key is "p:from" or "p:aft" ? ["p:from", "p:aft"] : [view.Row.Key]);
                Bind(controller);
                return true;
            case GestureOutcome.Refused refused:
                return Refuse(view, box, refused.Copy);
            case null:
                return Refuse(view, box, controller.Status);
            default:
                MarkShown([view.Row.Key]);
                Bind(controller);
                return true;
        }
    }

    private bool Parse(RowView view, TextBox box, IReadOnlyDictionary<string, double> dims, out double value)
    {
        var row = view.Row;
        value = double.NaN;
        if (!UnitEntry.TryParse(box.Text, row.Family, dims, out var entry))
            return Refuse(view, box, PropertyCopy.NotANumber(row.Label));
        if (row.MustBePositive && entry.Value <= 0)
            return Refuse(view, box, PropertyCopy.NotPositive(row.Label));
        if (row.AngleBounded && Math.Abs(entry.Value) >= 90)
            return Refuse(view, box, PropertyCopy.AngleOutOfRange(row.Label));
        value = entry.Value;
        return true;
    }

    /// <summary>PG-22 / B10: an error is announced once per failed commit, never on a re-render.</summary>
    private bool Refuse(RowView view, TextBox box, string message)
    {
        string before = view.Message.Text ?? "";
        errors[view.Row.Key] = (box.Text ?? "", message);
        messages.Remove(view.Row.Key);
        RenderRow(view.Row, []);
        if (before == message)
        {
            // The same error again: clear and set, so the live region speaks this failed commit too.
            view.Message.Text = "";
            view.Message.Text = message;
        }
        box.Classes.Set("error", true);
        AutomationProperties.SetHelpText(box, HelpText(view));
        Announced?.Invoke(message, AutomationLiveSetting.Assertive);
        return false;
    }

    /// <summary>A typed edit is one gesture and one undo row; a handle edited from its anchor keeps the anchor selected.</summary>
    private GestureOutcome? RunTypedGesture(WorkbenchController controller, PointRef target, (double Span, double Aft) destination)
    {
        using (Hold())
        {
            var selection = controller.Selection;
            if (!controller.BeginGesture(target, GestureInput.Typed)) return null;
            controller.Select(selection);
            controller.UpdateGesture(destination.Span, destination.Aft);
            controller.FlushGestureFrame();
            var task = controller.EndGestureAsync(GestureEnd.Release);
            PumpUi(task);
            return task.IsCompletedSuccessfully ? task.Result : new GestureOutcome.Refused("DSL-NOT-ASSESSED",
                "This change couldn't be checked, so it wasn't applied. Nothing changed.");
        }
    }

    private void NudgeStep(RowView view, TextBox box, int direction, KeyModifiers modifiers)
    {
        if (boundController is not { } controller || view.Row.Target is not { } target) return;
        if (run is null)
        {
            if (controller.Planform is not { } plan || ReadValue(view.Row, plan) is not { } origin) return;
            using (Hold())
            {
                var selection = controller.Selection;
                if (!controller.BeginGesture(target, GestureInput.Typed)) return;
                controller.Select(selection);
            }
            var start = PropertiesView.Find(plan, target)!;
            run = new NudgeRun(view, box, target, plan, origin, origin, (start.SpanMeters, start.AftMeters));
        }
        var active = run;
        // COPY-163: ⌘ (Ctrl on Windows) 0.01, Shift 1, plain 0.1 — in the field's unit (mm, °, %).
        double step = modifiers.HasFlag(KeyModifiers.Meta) || modifiers.HasFlag(KeyModifiers.Control) ? 0.01
            : modifiers.HasFlag(KeyModifiers.Shift) ? 1 : 0.1;
        double next = Math.Round(active.Value + direction * step, 6);
        if (view.Row.AngleBounded && Math.Abs(next) >= 90)
        {
            StopsHere(view);
            return;
        }
        var destination = RunTarget(active, next);
        controller.UpdateGesture(destination.Span, destination.Aft);
        controller.FlushGestureFrame();
        double reached = controller.Planform is { } now && ReadValue(view.Row, now) is { } value ? value : next;
        if (view.Row.AngleBounded && Math.Abs(reached - active.Value) < step / 4)
        {
            // MC-23: Core's ordering clamp holds a handle short of ±90°. The run stops at its last position, so a run
            // that never got past the bound changes nothing and makes no undo row.
            controller.UpdateGesture(active.Accepted.Span, active.Accepted.Aft);
            controller.FlushGestureFrame();
            StopsHere(view);
            return;
        }
        run = active with { Value = reached, Accepted = destination };
        SetShown(box, Quantity.ForField(Quantity.Typed(reached)));
    }

    /// <summary>MC-23: a run held at the angle bound stops there and says so (COPY-170).</summary>
    private void StopsHere(RowView view)
    {
        messages[view.Row.Key] = new RowMessage(PropertyCopy.AngleRunStops, MessageKind.Warning);
        ShowMessage(view, messages[view.Row.Key]);
    }

    private (double Span, double Aft) RunTarget(NudgeRun active, double value)
    {
        var point = PropertiesView.Find(active.Plan, active.Target)!;
        switch (active.View.Row.Key)
        {
            case "p:from": return (value / 1000, point.AftMeters);
            case "p:aft": return (point.SpanMeters, value / 1000);
        }
        var anchor = PropertiesView.Rail(active.Plan, point.Curve)!.Points.First(item => item.Id == point.AnchorId);
        var (angle, length) = PropertiesView.HandleGeometry(point, anchor);
        bool isAngle = active.View.Row.Family == UnitFamily.Angle;
        return CfdWorkbench.Core.Planform.HandleTarget(active.Plan, point.Curve, point.Id,
            isAngle ? value : angle, isAngle ? length : value / 1000);
    }

    private static double? ReadValue(PropertyRow row, PlanformView plan)
    {
        if (row.Target is not { } target || PropertiesView.Find(plan, target) is not { } point) return null;
        switch (row.Key)
        {
            case "p:from": return point.SpanMeters * 1000;
            case "p:aft": return point.AftMeters * 1000;
        }
        if (point.AnchorId is null) return null;
        var anchor = PropertiesView.Rail(plan, point.Curve)!.Points.First(item => item.Id == point.AnchorId);
        var (angle, length) = PropertiesView.HandleGeometry(point, anchor);
        return row.Family == UnitFamily.Angle ? angle : length * 1000;
    }

    private void EndRun(GestureEnd reason)
    {
        if (run is not { } active || boundController is not { } controller) return;
        run = null;
        Task<GestureOutcome> task;
        using (Hold())
        {
            task = controller.EndGestureAsync(reason);
            PumpUi(task);
        }
        var row = active.View.Row;
        if (task.IsCompletedSuccessfully && task.Result is GestureOutcome.Committed)
        {
            // PG-08: the new value is announced once, politely, on release.
            string value = Quantity.Typed(controller.Planform is { } plan && ReadValue(row, plan) is { } now ? now : active.Value);
            string report = $"{row.Label} {(row.Unit == "°" ? value + "°" : value + " " + row.Unit)}.";
            messages[row.Key] = new RowMessage(report, MessageKind.Report);
            Announced?.Invoke(report, AutomationLiveSetting.Polite);
        }
        else if (reason == GestureEnd.Escape)
            messages.Remove(row.Key);
        shown.Remove(active.Box);
        Bind(controller);
    }

    private sealed record NudgeRun(RowView View, TextBox Box, PointRef Target, PlanformView Plan, double Origin, double Value,
        (double Span, double Aft) Accepted);

    // ---------------- Type (PG-07, PG-19) ----------------

    private void OnTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (syncingChoice || rendering || TypeRowView() is not { } view) return;
        string? value = (TypeControl.SelectedItem as ComboBoxItem)?.Tag as string;
        if (value is null) return;
        if (TypeControl.IsDropDownOpen) return;   // a pointer pick commits when the list closes
        // Arrows on the closed box are pending; Return commits, Esc keeps, leaving drops it.
        pendingType = value == view.Row.Value ? null : value;
        RenderChoice(view, view.Row);
        if (pendingType is null) ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
    }

    private void OnTypeDropDownClosed()
    {
        if (TypeRowView() is not { } view) return;
        string? value = (TypeControl.SelectedItem as ComboBoxItem)?.Tag as string;
        if (value is not null && value != view.Row.Value) CommitType(view, value);
    }

    private void OnTypeKeyDown(object? sender, KeyEventArgs e)
    {
        if (TypeRowView() is not { } view) return;
        if (e.Key == Key.Enter && !TypeControl.IsDropDownOpen)
        {
            e.Handled = true;
            if (pendingType is { } pending) CommitType(view, pending);
        }
        else if (e.Key == Key.Escape && pendingType is not null && !TypeControl.IsDropDownOpen)
        {
            e.Handled = true;
            pendingType = null;
            RenderChoice(view, view.Row);
            ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
        }
    }

    private void DropPendingType()
    {
        if (pendingType is null || TypeControl.IsDropDownOpen || TypeRowView() is not { } view) return;
        pendingType = null;   // D1 / PG-19: leaving the box with a pending type does not commit
        RenderChoice(view, view.Row);
        ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
    }

    private RowView? TypeRowView() =>
        rows.TryGetValue("p:type|Choice", out var view) && view.Root.IsAttachedToVisualTree() ? view : null;

    private void CommitType(RowView view, string value)
    {
        pendingType = null;
        if (boundController is not { } controller || controller.Planform is not { } plan || view.Row.Target is not { } target) return;
        var rail = PropertiesView.Rail(plan, target.Curve);
        var point = rail?.Points.FirstOrDefault(item => item.Id == target.VertexId);
        if (rail is null || point is null) return;
        PointCommand command = value == "anchor" ? new PointCommand.MakeAnchor(point.Curve, point.Id) : new PointCommand.MakeControl(point.Curve, point.Id);
        var task = controller.ApplyPointCommandAsync(command);
        using (Hold()) PumpUi(task);
        if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Committed committed)
        {
            int before = rail.Points.Count;
            int after = controller.Planform is { } now ? PropertiesView.Rail(now, target.Curve)!.Points.Count : before;
            string curve = PropertiesView.Curves[target.Curve].Name;
            // The deviation is the operation's own number (COPY-154 / COPY-165), read from its report.
            string largest = Deviation().Match(committed.Report) is { Success: true } match
                ? $" Largest change {match.Groups[1].Value} mm." : "";
            string report = value == "anchor"
                ? $"{curve} point {point.Index + 1} is now an anchor point with 2 handles. The rail gained {after - before} points ({before} → {after}).{largest}"
                : $"{curve} point {point.Index + 1} is now a control point. Its handles are removed; the rail has {after} points (was {before}).{largest}";
            messages["p:type"] = new RowMessage(report, MessageKind.Report);
            Announced?.Invoke(report, AutomationLiveSetting.Polite);
        }
        else if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Refused refused)
            messages["p:type"] = new RowMessage(refused.Copy, MessageKind.Error);
        Bind(controller);
    }

    [GeneratedRegex(@"Max deviation ([0-9.]+) mm")]
    private static partial Regex Deviation();

    // ---------------- Kind (PG-06 = MC-1, the ruled APG deviation) ----------------

    private void OnKindKeyDown(object? sender, KeyEventArgs e)
    {
        if (!rows.TryGetValue("t:kind|KindList", out var view)) return;
        var committed = Enum.Parse<TangentKind>(view.Row.Value);
        int current = Array.FindIndex(kindButtons, button => KindOf(button) == (pendingKind ?? committed));
        int next = e.Key switch
        {
            Key.Up or Key.Left => Math.Max(0, current - 1),     // no wrap
            Key.Down or Key.Right => Math.Min(kindButtons.Length - 1, current + 1),
            Key.Home => 0,
            Key.End => kindButtons.Length - 1,
            _ => -1
        };
        if (next >= 0)
        {
            e.Handled = true;
            var kind = KindOf(kindButtons[next]);
            pendingKind = kind == committed ? null : kind;   // arrows move the check only
            RenderKinds(view, view.Row);
            if (pendingKind is null) ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
            kindButtons[next].Focus(NavigationMethod.Directional);
        }
        else if (e.Key == Key.Escape && pendingKind is not null)
        {
            e.Handled = true;
            pendingKind = null;
            RenderKinds(view, view.Row);
            ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
            kindButtons.First(button => KindOf(button) == committed).Focus(NavigationMethod.Directional);
        }
    }

    private void CommitKindOnLeave()
    {
        if (pendingKind is { } kind && !TangentGroup.IsKeyboardFocusWithin) CommitKind(kind);
    }

    private void CommitKind(TangentKind kind)
    {
        pendingKind = null;
        if (rendering || boundController is not { } controller || controller.Planform is not { } plan ||
            !rows.TryGetValue("t:kind|KindList", out var view) || view.Row.Target is not { } target ||
            PropertiesView.Find(plan, target) is not { } anchor)
            return;
        bool hadFocus = TangentGroup.IsKeyboardFocusWithin;
        if (anchor.Kind != kind)
        {
            // On a handle the kind keeps the selected handle and moves the other one (COPY-162).
            string? keep = controller.Selection is Selection.Points { Items.Count: 1 } points &&
                           PropertiesView.Find(plan, points.Items[0]) is { AnchorId: { } owner } handle && owner == anchor.Id
                ? handle.Id : null;
            var task = controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, kind, keep));
            using (Hold()) PumpUi(task);
            if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Refused refused)
                messages["t:kind"] = new RowMessage(refused.Copy, MessageKind.Error);
            else if (task.IsCompletedSuccessfully)
            {
                // MC-11 / PG-33: the report comes from the operation — the kind, and on a handle which handle it kept.
                string kept = keep is null ? ""
                    : PropertiesView.Find(plan, new PointRef(anchor.Curve, keep)) is { } keptHandle && keptHandle.Index > anchor.Index
                        ? " Kept the handle toward the tip; the other one moved." : " Kept the handle toward the root; the other one moved.";
                string report = $"{PropertiesView.Curves[anchor.Curve].Name} point {anchor.Index + 1} is now {kind}.{kept}";
                messages["t:kind"] = new RowMessage(report, MessageKind.Report);
                Announced?.Invoke(report, AutomationLiveSetting.Polite);
            }
        }
        Bind(controller);
        // Tangent_KindChange_KeepsFocusOnChecked: focus stays on the checked option after the commit.
        if (hadFocus) kindButtons.FirstOrDefault(button => button.IsChecked == true)?.Focus(NavigationMethod.Directional);
    }

    private static TangentKind KindOf(RadioButton button) => Enum.Parse<TangentKind>(button.Content?.ToString() ?? "Corner");

    // ---------------- pane keys, crumb, recovery ----------------

    private void OnPaneKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || boundController is not { } controller) return;
        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (e.Key == Key.C && command && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // PG-25: ⌘⇧C (Ctrl+Shift+C) copies the selection's rows from anywhere in the pane, keyboard only.
            e.Handled = true;
            CopyLines(shownModel?.Groups ?? []);
            return;
        }
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox) return;
        if ((e.Key == Key.F10 && e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.Key == Key.Apps) &&
            focused is ToggleButton { TemplatedParent: Expander { ContextMenu: { } menu } group })
        {
            e.Handled = true;
            menu.Open(group);
        }
        else if (e.Key == Key.Escape && IdentityCrumbLink.Tag is PointRef parent)
        {
            // PG-12 / PG-28: Esc on a handle selection selects its anchor or end, and says so.
            e.Handled = true;
            controller.Select(new Selection.Points([parent]));
            Announced?.Invoke($"Selected {IdentityTitle.Text}.", AutomationLiveSetting.Polite);
        }
        else if (e.Key == Key.C && (e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control)) &&
                 copyTarget is { } key && rows.TryGetValue(key, out var view))
        {
            e.Handled = true;
            CopyRow(view.Row.Key, withUnit: true);
        }
    }

    private void CopyGroup(string id) =>
        CopyLines(shownModel?.Groups.Where(group => group.Id == id).ToList() ?? [], shownModel?.Wing is { } wing && wing.Id == id ? wing : null);

    /// <summary>The rows as "label value unit" lines, one per row (PG-25).</summary>
    private void CopyLines(IReadOnlyList<PropertyGroup> groups, PropertyGroup? wing = null)
    {
        var lines = groups.Append(wing).OfType<PropertyGroup>().SelectMany(group => group.Rows).Select(row =>
        {
            string value = row.Kind == RowKind.Choice
                ? row.Options?.FirstOrDefault(option => option.Value == row.Value)?.Text ?? row.Value
                : CopyText(row, withUnit: true);
            return $"{row.Label} {value}";
        }).ToList();
        if (lines.Count == 0) return;
        var write = ClipboardWriter ?? (text => TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask);
        _ = write(string.Join("\n", lines));
    }

    private void GoToCrumb()
    {
        if (IdentityCrumbLink.Tag is PointRef parent) boundController?.Select(new Selection.Points([parent]));
    }

    private void ResumeRecoveryIfNeeded(WorkbenchController controller)
    {
        if (!controller.HasRecovery || controller.Draft is not null || resumingRecovery) return;
        resumingRecovery = true;
        try { controller.ResumeRecovery(); }
        finally { resumingRecovery = false; }
    }

    private void ApplyRecovery()
    {
        if (boundController is null) return;
        if (boundController.Draft is null && boundController.HasRecovery)
            boundController.ResumeRecovery();
        PumpUi(boundController.PreviewAsync());
        boundController.Apply();
    }

    // ---------------- helpers ----------------

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

    private bool Dirty(TextBox box) => shown.TryGetValue(box, out var text) && (box.Text ?? "") != text;

    private void SetShown(TextBox box, string text)
    {
        shown[box] = text;
        if (box.Text != text) box.Text = text;
    }

    private void MarkShown(IEnumerable<string> keys)
    {
        foreach (var key in keys)
            if (rows.TryGetValue(key + "|Input", out var view) && view.Input is { } input)
                shown[input] = input.Text ?? "";
    }

    private static string SelectionKey(Selection selection) => selection switch
    {
        Selection.Points points => "points:" + string.Join(",", points.Items.Select(item => item.Curve + "/" + item.VertexId)),
        Selection.Station station => "station:" + station.Index.ToString(CultureInfo.InvariantCulture),
        _ => selection.GetType().Name
    };

    private static T Detach<T>(T control) where T : Control
    {
        switch (control.Parent)
        {
            case Panel panel: panel.Children.Remove(control); break;
            case Decorator decorator when ReferenceEquals(decorator.Child, control): decorator.Child = null; break;
        }
        return control;
    }

    /// <summary>Brings a panel's children to the desired list without detaching the ones already in place (focus stays).</summary>
    private static void Sync(Panel panel, IReadOnlyList<Control> desired)
    {
        var children = panel.Children;
        if (children.SequenceEqual(desired)) return;
        var keep = new HashSet<Control>(desired);
        for (int index = children.Count - 1; index >= 0; index--)
            if (!keep.Contains(children[index])) children.RemoveAt(index);
        for (int index = 0; index < desired.Count; index++)
        {
            var control = desired[index];
            if (index < children.Count && ReferenceEquals(children[index], control)) continue;
            int at = children.IndexOf(control);
            if (at >= 0) children.RemoveAt(at);
            else Detach(control);
            children.Insert(index, control);
        }
    }

    private static string Part(string prefix, string key) => prefix + "_" + key.Replace(':', '_').Replace('-', '_');

    private static string UnitName(string key) => key switch
    {
        "w:span" => "SpanUnit",
        "w:root" => "RootChordUnit",
        "w:tip" => "TipChordUnit",
        _ => Part("Unit", key)
    };

    private static string ValueName(string key) => key switch
    {
        "e:mean" => "MeanChordText",
        "e:mac" => "MacText",
        "e:maxtc" => "MaxTcText",
        "e:ar" => "AspectText",
        "e:area" => "AreaEstimateText",
        "w:tip" => "TipClosedText",
        "p:type" => "TypeReadOnly",
        _ => Part("Value", key)
    };

    private static string MessageName(string key) => key switch
    {
        "w:root" => "ChordWarningText",
        _ => Part("Message", key)
    };

    private static string GlyphPath(IdentityGlyph glyph) => glyph switch
    {
        IdentityGlyph.Foil => "M1,9 Q8,2 15,8 Q8,12 1,9 Z",
        IdentityGlyph.Control => "M8,3 A5,5 0 1 1 7.99,3 Z",
        IdentityGlyph.Anchor => "M3,3 L13,3 L13,13 L3,13 Z",
        IdentityGlyph.End => "M8,1 L15,8 L8,15 L1,8 Z",
        IdentityGlyph.Handle => "M8,5 A3,3 0 1 1 7.99,5 Z",
        IdentityGlyph.Several => "M3,6.5 A1.5,1.5 0 1 1 2.99,6.5 Z M8,6.5 A1.5,1.5 0 1 1 7.99,6.5 Z M13,6.5 A1.5,1.5 0 1 1 12.99,6.5 Z",
        _ => "M8,1 L8,15"
    };

    private IDisposable Hold() => new RenderHold(this);

    private sealed class RenderHold : IDisposable
    {
        private PropertiesPane? pane;

        public RenderHold(PropertiesPane pane)
        {
            this.pane = pane;
            pane.holds++;
        }

        public void Dispose()
        {
            if (pane is not { } owner) return;
            pane = null;
            if (--owner.holds > 0 || !owner.bindPending) return;
            owner.bindPending = false;
            if (owner.boundController is { } controller) owner.Bind(controller);
        }
    }

    private static void PumpUi(Task task)
    {
        if (task.IsCompleted) return;
        var start = DateTime.UtcNow;
        while (!task.IsCompleted && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
    }

    private sealed class RowView(Border root, Grid grid, TextBlock label, TextBlock unit, TextBlock description, Border messageBox,
        TextBlock message)
    {
        public Border Root { get; } = root;
        public Grid Grid { get; } = grid;
        public TextBlock Label { get; } = label;
        public TextBlock Unit { get; } = unit;
        public TextBlock Description { get; } = description;
        public Border MessageBox { get; } = messageBox;
        public TextBlock Message { get; } = message;
        public TextBlock? Value { get; set; }
        public TextBox? Input { get; set; }
        public required PropertyRow Row { get; set; }
    }

    private sealed class GroupView(Expander expander, TextBlock title, TextBlock summary, StackPanel body)
    {
        public Expander Expander { get; } = expander;
        public TextBlock Title { get; } = title;
        public TextBlock Summary { get; } = summary;
        public StackPanel Body { get; } = body;
        public ToggleButton? Header { get; set; }
    }
}
