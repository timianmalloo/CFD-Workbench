using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Path = Avalonia.Controls.Shapes.Path;
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
/// The Properties pane as a property sheet in structure B, Premiere Pro Effect Controls (DR-CELL-1;
/// docs/reviews/ui-property-grid-cells.md §5). It renders <see cref="PropertiesModel"/> generically: a selection identity,
/// twirl groups and label | value | unit rows, with the Wing pinned at the foot (DR-UID-5). Editors persist per row key so a
/// re-render never moves focus (UI-C).
/// </summary>
public partial class PropertiesPane : UserControl
{
    private const double WingShare = 0.55;     // DR-UID-5: the Wing keeps at most 55 % of the pane's height
    private const double StackedFrom = 1.5;    // DN-5: at 150 % text and above the value drops under its label

    /// <summary>The size tokens one Text size multiplier scales (DN-5; DESIGN.md typography.prop-text-scale).</summary>
    public static readonly IReadOnlyList<string> ScaledTokens =
    [
        "PropFontSize", "PropLineHeight", "PropNoteSize", "PropNoteLineHeight", "PropTitleSize", "PropTitleLineHeight",
        "PropHeadHeight", "PropRowReadOnlyHeight", "PropRowInputHeight", "PropEditBoxHeight", "PropValueMinWidth", "PropUnitWidth"
    ];

    private const string ChevronClosed = "M3,1 L7,5 L3,9";
    private const string ChevronOpen = "M1,3 L5,7 L9,3";
    private const string DisclosureClosed = "M0,0 L3,2.5 L0,5 Z";   // the definitions link's ▸ / ▾
    private const string DisclosureOpen = "M0,0 L5,0 L2.5,3 Z";
    private const string IconError = "M5.5,0.5 A5,5 0 1 1 5.49,0.5 Z M3.5,3.5 L7.5,7.5 M7.5,3.5 L3.5,7.5";
    private const string IconWarning = "M5.5,0.75 L10.5,10 L0.5,10 Z M5.5,4 L5.5,7 M5.5,8.25 L5.5,8.75";
    private const string IconInfo = "M5.5,0.5 A5,5 0 1 1 5.49,0.5 Z M5.5,5 L5.5,8 M5.5,3 L5.5,3.5";
    private const string IconReport = "M1.5,6 L4.5,9 L9.5,2.5";
    private const string LockGlyph = "M3,5 L3,3.5 A2,2 0 0 1 7,3.5 L7,5 M2,5 L8,5 L8,9.5 L2,9.5 Z";

    private readonly Dictionary<string, TextBox> pooledInputs;
    private readonly Dictionary<string, RowView> rows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBlock> subheads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GroupView> groupViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SectionView> sectionViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TextBlock>> noteViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> collapsed = new(StringComparer.Ordinal) { ["rail"] = true };
    private readonly Dictionary<TextBox, RowView> inputOwners = [];
    private readonly Dictionary<TextBox, string> shown = [];
    private readonly Dictionary<TextBox, (EditCue Cue, Border Ring)> cues = [];
    private readonly Dictionary<string, RowMessage> messages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Text, string Error)> errors = new(StringComparer.Ordinal);
    private readonly EnumField typeField;
    private readonly EnumField kindField;
    // The section mode's third enum (Station t/c, §11.4), pooled like Type and Kind so focus survives a re-render.
    private readonly ComboBox intentControl = new() { Name = "StationTcControl" };
    private readonly EnumField intentField;

    private WorkbenchController? boundController;
    private string selectionKey = "";
    private int holds;
    private bool bindPending;
    private bool rendering;
    private bool syncingChoice;
    private bool resumingRecovery;
    private double textScale = 1;
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
        foreach (var box in pooledInputs.Values)
        {
            box.IsEnabled = false;
            WireCue(box);
        }
        typeField = new EnumField(TypeControl, "p:type|Choice", "Type");
        kindField = new EnumField(KindControl, "t:kind|KindList", "Tangent kind");
        intentField = new EnumField(intentControl, "sec:intent|Choice", "Station t/c");
        intentControl.Classes.Add("prop-b");
        FieldPool.Children.Add(intentControl);

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
            HowMeasuredChevron.Data = Avalonia.Media.Geometry.Parse(HowMeasuredBody.IsVisible ? DisclosureOpen : DisclosureClosed);
        };
        RecoveryApplyButton.Click += (_, _) => ApplyRecovery();
        RecoveryDiscardButton.Click += (_, _) => boundController?.DiscardRecovery();
        IdentityCrumbLink.Click += (_, _) => GoToCrumb();

        foreach (var field in new[] { typeField, kindField, intentField })
        {
            var box = field.Box;
            box.SelectionChanged += (_, _) => OnEnumSelectionChanged(field);
            box.DropDownClosed += (_, _) => OnEnumDropDownClosed(field);
            box.AddHandler(KeyDownEvent, (_, e) => OnEnumKeyDown(field, e), RoutingStrategies.Tunnel);
            box.LostFocus += (_, _) => DropPending(field);
            box.TemplateApplied += (_, e) => FitEnumTemplate(e.NameScope);
        }

        AddHandler(KeyDownEvent, OnPaneKeyDown, RoutingStrategies.Bubble);
        AddHandler(KeyDownEvent, OnShiftTabFromFirstValue, RoutingStrategies.Tunnel);
        // PG-25: a row the pointer chose is the Copy target only until focus moves.
        AddHandler(GotFocusEvent, (_, _) => copyTarget = null, RoutingStrategies.Bubble, handledEventsToo: true);
        SizeChanged += (_, _) => FitToPane();
    }

    /// <summary>
    /// Every report the pane makes for the status strip (DR-STATUS-1): type, kind, nudge value, echoes, the chord fit
    /// warning, the angle-run stop, a dropped pending value, "Selected …" and estimate availability. A field error is not
    /// reported: it stays under its field, where its assertive message line speaks it.
    /// </summary>
    public event Action<StatusReport>? Reported;

    /// <summary>The Station group's "Edit section…" link (COPY-172, CAD-20): the shell opens the section editor.</summary>
    public event Action? EditSectionRequested;

    /// <summary>The selected curve group's Rebuild link opens its measured preview.</summary>
    public event Action<string>? RebuildRequested;

    /// <summary>A section step the pane asks the shell to apply (Station t/c; the shell reports it or its refusal).</summary>
    public event Func<SectionStep, Task>? SectionStepRequested;

    /// <summary>Where the Copy command writes; null writes to the window's clipboard.</summary>
    public Func<string, Task>? ClipboardWriter { get; set; }

    /// <summary>The Text size multiplier the pane is drawn at (DN-5): 1, 1.25, 1.5 or 2.</summary>
    public double TextScale => textScale;

    /// <summary>The model the pane last drew (the rendered checks read its rows).</summary>
    public PropertiesModel? ShownModel => shownModel;

    public void FocusTypeValue() => PointSpanInput.Focus();

    // DR-NAV-1: the pane's first value is where Tab from a selected Plan point lands (Type for a point), and the one value
    // Shift+Tab leaves to go back to that point.
    private Control? FirstValue() => this.GetVisualDescendants().OfType<InputElement>()
        .FirstOrDefault(item => item is TextBox or ComboBox && item.Focusable && item.IsEffectivelyVisible && item.IsEffectivelyEnabled) as Control;

    /// <summary>DR-NAV-1: focuses the pane's first value, as Tab from a selected Plan point does.</summary>
    public bool FocusFirstValue() => FirstValue() is { } value && value.Focus(NavigationMethod.Tab);

    // The pane sees Shift+Tab before its first value does, and asks the Plan to focus the selected point again.
    private void OnShiftTabFromFirstValue(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Tab || !e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;
        if (boundController?.Selection is not Selection.Points) return;
        if (!ReferenceEquals(TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement(), FirstValue())) return;
        if (this.FindAncestorOfType<ShellHost>()?.ModelView.PlanCanvas.FocusSelectedPoint() == true) e.Handled = true;
    }

    public void ShowRenderFailure(bool foilOpen)
    {
        ErrorText.Text = "Properties couldn't be shown." + (foilOpen ? " Your foil hasn't changed." : "");
        ErrorPanel.IsVisible = true;
    }

    /// <summary>
    /// DN-5: one multiplier scales every Prop type and row token. The pane writes base × scale into its own resources, so
    /// the styles' DynamicResource reads follow; at 150 % and above the rows stack.
    /// </summary>
    public void ApplyTextScale(double scale)
    {
        textScale = scale;
        foreach (var key in ScaledTokens) Resources[key] = BaseToken(key) * scale;
        var padding = Application.Current?.TryFindResource("PropEditPadding", out var value) == true && value is Thickness thickness
            ? thickness : default;
        Resources["PropEditPadding"] = new Thickness(padding.Left, padding.Top * scale, padding.Right, padding.Bottom * scale);
        Resources["PropTextScale"] = scale;
        Classes.Set("prop-stacked", scale >= StackedFrom);
        foreach (var view in rows.Values) Layout(view);
        if (boundController is { } controller && ContentPanel.IsVisible) Bind(controller);
    }

    private static double BaseToken(string key) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double number ? number : 0;

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
                NotChecked: controller.Inspection is { } inspection && inspection.Geometry.Status != GeometryStatus.Certified,
                Curves: controller.CurveFor,
                Frame: eta => StationFrameAt(controller, eta),
                Section: SectionContext.Of(controller));
            string key = SelectionKey(controller.Selection);
            if (key != selectionKey)
            {
                selectionKey = key;
                copyTarget = null;
                messages.Clear();
                errors.Clear();
                typeField.Pending = null;
                kindField.Pending = null;
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

    /// <summary>The copy text of any row: an enum copies its option's words, a number its value (with its unit).</summary>
    private static string RowCopyText(PropertyRow row, bool withUnit) => row.Kind is RowKind.Choice or RowKind.KindList
        ? row.Options?.FirstOrDefault(option => option.Value == row.Value)?.Text ?? row.Value
        : CopyText(row, withUnit);

    private static bool CopiesWithUnit(PropertyRow row) =>
        row.Unit is not null && row.State is not (RowState.Mixed or RowState.Unavailable) && row.Kind is not (RowKind.Choice or RowKind.KindList);

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
            // COPY-160: a change in availability is reported once in the status strip, never on a re-render.
            if (model.AvailabilityStatus != lastAvailability && model.AvailabilityStatus is { } availability)
                Reported?.Invoke(new StatusReport(availability));
            lastAvailability = model.AvailabilityStatus;

            var used = new HashSet<Control>();
            var groupControls = new List<Control>();
            var groups = model.Groups;
            for (int index = 0; index < groups.Count; index++)
            {
                if (groups[index].Continues) continue;   // drawn inside the group before it
                var continued = groups.Skip(index + 1).TakeWhile(group => group.Continues).ToList();
                groupControls.Add(RenderGroup(groups[index], continued, used));
            }
            Sync(BlocksPanel, groupControls);
            if (model.Wing is { } wing) RenderWing(wing, used);
            // A pooled editor this selection does not show is neither enabled nor visible, wherever it was last placed.
            foreach (var control in pooledInputs.Values.Cast<Control>().Append(TypeControl).Append(KindControl).Where(control => !used.Contains(control)))
            {
                control.IsEnabled = false;
                control.IsVisible = false;
            }
            FitToPane();
        }
        finally { rendering = false; }
        // DN-6: after a commit re-renders, the focused row and its message line are brought into view again.
        if (FocusedRow() is { } focused) BringRowIntoView(focused);
    }

    private void RenderIdentity(SelectionIdentity? identity)
    {
        IdentityBlock.IsVisible = identity is not null;
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

    private Control RenderGroup(PropertyGroup group, IReadOnlyList<PropertyGroup> continued, HashSet<Control> used)
    {
        if (!groupViews.TryGetValue(group.Id, out var view))
            groupViews[group.Id] = view = CreateGroup(group.Id);
        view.Title.Text = group.Title;
        view.Summary.Text = group.Summary;
        bool expanded = !collapsed.GetValueOrDefault(group.Id);
        if (view.Expander.IsExpanded != expanded) view.Expander.IsExpanded = expanded;
        view.Summary.IsVisible = !expanded && group.Summary.Length > 0;
        view.Chevron.Data = Avalonia.Media.Geometry.Parse(expanded ? ChevronOpen : ChevronClosed);
        AutomationProperties.SetName(view.Expander, group.Title);
        AutomationProperties.SetHelpText(view.Expander, group.Summary);
        if (view.Header is { } header)
        {
            AutomationProperties.SetName(header, group.Title);
            AutomationProperties.SetHelpText(header, group.Summary);
        }
        var body = BodyControls(group, view.Body, used);
        foreach (var section in continued) body.Add(RenderSection(section, used));
        Sync(view.Body, body);
        RuleRows(view.Body);
        FillCopyMenu(view.Menu, [group, .. continued]);
        return view.Root;
    }

    /// <summary>B: the tangent rows continue the Point group under its twirl, after a half-strength rule (DR-CELL-1).</summary>
    private Control RenderSection(PropertyGroup group, HashSet<Control> used)
    {
        if (!sectionViews.TryGetValue(group.Id, out var section))
        {
            var body = new StackPanel { Name = Part("Section", group.Id) };
            body.Classes.Add("prop-body");
            var root = new StackPanel { Children = { RuleLine(), body } };
            sectionViews[group.Id] = section = new SectionView(root, body);
            WatchLead(body);
        }
        AutomationProperties.SetName(section.Body, group.Title);
        Sync(section.Body, BodyControls(group, section.Body, used));
        RuleRows(section.Body);
        return section.Root;
    }

    private List<Control> BodyControls(PropertyGroup group, StackPanel container, HashSet<Control> used)
    {
        var body = new List<Control>();
        var notes = Notes(group.Id, group.Lead is null ? group.Notes : [group.Lead, .. group.Notes]);
        if (group.Lead is not null)
        {
            // DR-CELL-4: the angle reference shows only while focus is in its group; it stays in the tree.
            notes[0].Classes.Set("lead", true);
            notes[0].IsVisible = container.IsKeyboardFocusWithin;
            body.Add(notes[0]);
        }
        bool underSubhead = false;
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
                underSubhead = true;
            }
            var view = RenderRow(row, used);
            view.Root.Classes.Set("sub", underSubhead);
            body.Add(view.Outer);
        }
        body.AddRange(group.Lead is null ? notes : notes.Skip(1));
        return body;
    }

    /// <summary>B: a half-strength rule between two adjacent rows, none above the first row after a header or subhead.</summary>
    private void RuleRows(StackPanel body)
    {
        Control? previous = null;
        foreach (var child in body.Children)
        {
            if (child is Border { Tag: RowView view })
                view.Rule.IsVisible = previous is Border { Tag: RowView };
            if (child.IsVisible) previous = child;
        }
    }

    /// <summary>B's half-strength rule: 1 px of the line colour at 50 % (no extra theme brush; PG-11 keeps the brush set fixed).</summary>
    private static Border RuleLine()
    {
        var rule = new Border();
        rule.Classes.Add("prop-rule");
        AutomationProperties.SetAccessibilityView(rule, AccessibilityView.Raw);
        return rule;
    }

    private void WatchLead(StackPanel container) =>
        container.PropertyChanged += (_, change) =>
        {
            if (change.Property != IsKeyboardFocusWithinProperty) return;
            foreach (var note in container.Children.OfType<TextBlock>().Where(note => note.Classes.Contains("lead")))
                note.IsVisible = container.IsKeyboardFocusWithin;
        };

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
            .Select(row => (Control)RenderRow(row, used).Outer).ToList());
        Sync(WingEstimates, wing.Rows.Where(row => !row.Key.StartsWith("w:", StringComparison.Ordinal))
            .Select(row => (Control)RenderRow(row, used).Outer).ToList());
        RuleRows(WingDimensions);
        RuleRows(WingEstimates);
        // PG-27: the first Wing note stays attached (hidden while empty) so a change is only a text change.
        var notes = Notes("wing", wing.Notes.Count > 0 ? wing.Notes : [new RowMessage("", MessageKind.Info)]);
        foreach (var note in notes)
        {
            AutomationProperties.SetLiveSetting(note, AutomationLiveSetting.Polite);
            note.IsVisible = note.Text?.Length > 0;
        }
        Sync(WingNotes, [.. notes]);
        WingHeader.ContextMenu ??= new ContextMenu { Name = "WingMenu" };
        FillCopyMenu(WingHeader.ContextMenu, [wing]);
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
            views[index].Classes.Set("lead", false);
            views[index].IsVisible = true;
        }
        return views.Take(notes.Count).ToList();
    }

    private RowView RenderRow(PropertyRow row, HashSet<Control> used)
    {
        string cacheKey = row.Key + "|" + row.Kind;
        if (!rows.TryGetValue(cacheKey, out var view)) rows[cacheKey] = view = CreateRow(row);
        view.Row = row;
        view.Label.Text = row.Label;
        bool hasError = errors.TryGetValue(row.Key, out var error);
        var held = messages.GetValueOrDefault(row.Key);
        var message = hasError ? new RowMessage(error.Error, MessageKind.Error) : held ?? row.Message;
        var state = hasError ? RowState.Error
            : message?.Kind == MessageKind.Warning && row.State == RowState.Normal ? RowState.Warning
            : row.State;
        view.Root.Classes.Set("warning", state == RowState.Warning);
        view.Root.Classes.Set("error", state == RowState.Error);
        view.Root.Classes.Set("unavailable", state == RowState.Unavailable);
        view.Description.Text = row.Description ?? "";
        // DR-STATUS-1: a commit warning's report is in the status strip; the row keeps its warning state only — the rail
        // and an icon whose tooltip (and the field's help text) carry the report.
        bool stateOnly = !hasError && held?.Kind == MessageKind.Warning;
        ShowMessage(view, stateOnly ? null : message);
        ShowStateIcon(view, stateOnly ? held!.Text : null);
        bool showUnit = row.State is not (RowState.Mixed or RowState.Unavailable);
        view.Unit.Text = showUnit ? row.Unit ?? "" : "";

        switch (row.Kind)
        {
            case RowKind.Input:
                RenderInput(view, row, hasError ? error.Text : null, used);
                break;
            case RowKind.Choice when row.Key == "sec:intent":
                used.Add(intentControl);
                RenderEnum(intentField, view, row);
                break;
            case RowKind.Choice:
                used.Add(TypeControl);
                RenderEnum(typeField, view, row);
                break;
            case RowKind.Action:
                view.Link!.Content = row.Value;
                AutomationProperties.SetName(view.Link, row.AutomationName ?? row.Value);
                break;
            case RowKind.KindList:
                used.Add(KindControl);
                RenderEnum(kindField, view, row);
                break;
            default:
                string text = row.Kind == RowKind.Estimate && row.State == RowState.Normal ? "≈ " + row.Value : row.Value;
                if (view.Value!.Text != text) view.Value.Text = text;
                view.Lock!.IsVisible = row.State == RowState.Locked;
                AutomationProperties.SetName(view.Root, row.SpokenText);
                AutomationProperties.SetHelpText(view.Root, row.HelperText);
                break;
        }
        ShowDescription(view);
        Layout(view);
        return view;
    }

    private void RenderInput(RowView view, PropertyRow row, string? invalidText, HashSet<Control> used)
    {
        var box = view.Input!;
        used.Add(box);
        inputOwners[box] = view;
        box.IsEnabled = true;
        box.IsVisible = true;
        SetError(box, invalidText is not null);
        AutomationProperties.SetName(box, row.AutomationName ?? row.Label);
        AutomationProperties.SetHelpText(box, HelpText(view));
        string modelText = Quantity.ForField(row.Value);
        if (invalidText is not null) return;   // keep what the user typed until Escape or the next commit
        if (run?.Box == box) return;            // a field run owns its text
        if (!box.IsKeyboardFocusWithin || !Dirty(box)) SetShown(box, modelText);
    }

    /// <summary>The inline warning icon beside the value (the mockup's state-only row); null hides it.</summary>
    private static void ShowStateIcon(RowView view, string? report)
    {
        if (report is null && view.StateIcon is null) return;
        if (view.StateIcon is null)
        {
            var icon = new Path { Name = Part("StateIcon", view.Row.Key), Data = Avalonia.Media.Geometry.Parse(IconWarning),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            icon.Classes.Add("prop-icon");
            icon.Classes.Add("warning");
            // Decoration for assistive technology: the field's help text carries the report.
            AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw);
            view.Grid.Children.Add(icon);
            view.StateIcon = icon;
        }
        var stateIcon = view.StateIcon;
        stateIcon.IsVisible = report is not null;
        ToolTip.SetTip(stateIcon, report);
        Grid.SetRow(stateIcon, Grid.GetRow(view.Cell));
        Grid.SetColumn(stateIcon, 0);
    }

    private static void ShowMessage(RowView view, RowMessage? message)
    {
        view.MessageBox.IsVisible = message is not null;
        string text = message?.Text ?? "";
        if (view.Message.Text != text) view.Message.Text = text;
        view.Message.Classes.Set("error", message?.Kind == MessageKind.Error);
        view.Message.Classes.Set("warning", message?.Kind == MessageKind.Warning);
        if (view.MessageIcon is { } icon && message is not null)
        {
            icon.Data = Avalonia.Media.Geometry.Parse(message.Kind switch
            {
                MessageKind.Error => IconError,
                MessageKind.Warning => IconWarning,
                MessageKind.Report => IconReport,
                MessageKind.Reason => LockGlyph,
                _ => IconInfo
            });
            icon.Classes.Set("error", message.Kind == MessageKind.Error);
            icon.Classes.Set("warning", message.Kind == MessageKind.Warning);
        }
        var live = message?.Kind == MessageKind.Error ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite;
        AutomationProperties.SetLiveSetting(view.MessageBox, live);
        AutomationProperties.SetLiveSetting(view.Message, live);
    }

    /// <summary>B: help shows under a row while it has keyboard focus, or always for a ruled authority line (COPY-159).</summary>
    private static void ShowDescription(RowView view) =>
        view.Description.IsVisible = view.Description.Text?.Length > 0 &&
                                     (view.Row.DescriptionAlwaysVisible || view.Root.IsKeyboardFocusWithin);

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
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto") };
        var label = new TextBlock { Name = Part("Label", row.Key) };
        label.Classes.Add("prop-label");
        var unit = new TextBlock { Name = UnitName(row.Key), Margin = new Thickness(Token("PropColumnGap"), 0, 0, 0) };
        unit.Classes.Add("prop-unit");
        var description = new TextBlock { Name = Part("Description", row.Key), IsVisible = false };
        description.Classes.Add("prop-desc");
        Grid.SetRow(description, 2);
        Grid.SetColumnSpan(description, 3);
        TextBlock messageText;
        Border messageBox;
        // The Span field owns the pooled error line; the read-only Span of the section mode (COPY-122) gets its own.
        if (row.Key == "w:span" && row.Kind == RowKind.Input)
        {
            messageBox = Detach(SpanErrorPanel);
            messageText = SpanErrorText;
            messageBox.Child = null;
        }
        else
        {
            messageText = new TextBlock { Name = MessageName(row.Key) };
            messageText.Classes.Add("prop-message");
            messageBox = new Border { IsVisible = false };
        }
        // The state line: icon + text (DESIGN.md §12.0f: "always rail + icon + text"); the icon is decoration (Raw).
        var messageIcon = new Path { Name = Part("MessageIcon", row.Key) };
        messageIcon.Classes.Add("prop-icon");
        AutomationProperties.SetAccessibilityView(messageIcon, AccessibilityView.Raw);
        Grid.SetColumn(messageText, 1);
        messageBox.Child = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Children = { messageIcon, messageText } };
        Grid.SetRow(messageBox, 3);
        Grid.SetColumnSpan(messageBox, 3);
        var root = new Border { Child = grid, Name = Part("Row", row.Key) };
        root.Classes.Add("prop-row");
        var rule = RuleLine();
        var outer = new Border { Child = new StackPanel { Children = { rule, root } } };
        var view = new RowView(outer, root, grid, label, unit, description, messageBox, messageText) { Row = row, MessageIcon = messageIcon, Rule = rule };
        outer.Tag = view;

        Control value;
        switch (row.Kind)
        {
            case RowKind.Input:
                var box = pooledInputs.TryGetValue(row.Key, out var pooled) ? Detach(pooled) : new TextBox { Name = Part("Input", row.Key) };
                box.Classes.Add("prop-b");
                WireInput(box);
                view.Input = box;
                value = box;
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                AutomationProperties.SetAccessibilityView(unit, AccessibilityView.Raw);   // the name carries the unit (PG-01)
                break;
            case RowKind.Choice when row.Key == "sec:intent":
                value = Detach(intentControl);
                view.Enum = intentControl;
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                break;
            case RowKind.Action:
                // CAD-20: the Station group's last row is a link, not a value (DR-CELL-5 solid underline).
                var link = new HyperlinkButton { Name = Part("Link", row.Key), HorizontalAlignment = HorizontalAlignment.Left };
                link.Classes.Add("prop-crumb");
                link.Click += (_, _) =>
                {
                    if (row.Key == "s:edit") EditSectionRequested?.Invoke();
                    else if (row.Key == "r:rebuild" && row.Target is { } target) RebuildRequested?.Invoke(target.Curve);
                };
                view.Link = link;
                value = link;
                Grid.SetColumnSpan(link, 3);
                break;
            case RowKind.Choice:
                value = Detach(TypeControl);
                view.Enum = TypeControl;
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                description.Name = row.Key == "p:type" ? "PointHelper" : description.Name;
                break;
            case RowKind.KindList:
                value = Detach(KindControl);
                view.Enum = KindControl;
                AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
                label.Name = "TangentLabel";
                break;
            default:
                var text = new TextBlock { Name = ValueName(row.Key) };
                text.Classes.Add("prop-value");
                var lockGlyph = new Path { Data = Avalonia.Media.Geometry.Parse(LockGlyph), IsVisible = false };
                lockGlyph.Classes.Add("prop-lock");
                var cell = new StackPanel { Children = { lockGlyph, text } };
                cell.Classes.Add("prop-value-cell");
                view.Value = text;
                view.Lock = lockGlyph;
                value = cell;
                // PG-20 / D2: a fact is not a Tab stop; its container speaks one line and its parts are Raw (B9).
                foreach (var part in new Control[] { label, text, unit, description, lockGlyph, cell })
                    AutomationProperties.SetAccessibilityView(part, AccessibilityView.Raw);
                AutomationProperties.SetAccessibilityView(root, AccessibilityView.Content);
                AutomationProperties.SetControlTypeOverride(root, AutomationControlType.Text);
                root.PointerPressed += (_, _) => copyTarget = row.Key + "|" + row.Kind;
                break;
        }
        view.Cell = value;
        // A wide value (an enum, or words rather than a number) sits right-aligned across the row; the label wraps short of it.
        view.Wide = row.Kind is RowKind.Choice or RowKind.KindList || row.Kind == RowKind.Fact && row.Unit is null && !row.Dimensionless;
        if (view.Wide)
        {
            value.SizeChanged += (_, _) => Layout(view);
            grid.SizeChanged += (_, change) => { if (change.WidthChanged) Layout(view); };
        }
        if (row.IsEditable)
        {
            // DR-DEN-1 / SC 2.5.8: the whole 24 px row is the target; a press on the label or the gap focuses the value.
            label.Cursor = new Cursor(StandardCursorType.Hand);
            root.AddHandler(PointerPressedEvent, (_, e) =>
            {
                if (view.Editor is not { IsEffectivelyEnabled: true } editor) return;
                editor.Focus(NavigationMethod.Pointer);
                e.Handled = true;
            }, RoutingStrategies.Bubble);
        }
        root.PropertyChanged += (_, change) =>
        {
            if (change.Property != IsKeyboardFocusWithinProperty) return;
            ShowDescription(view);
            if (view.Root.IsKeyboardFocusWithin) BringRowIntoView(view);
        };
        grid.Children.AddRange([label, value, unit, description, messageBox]);
        return view;
    }

    /// <summary>
    /// B's row: label (wraps, never trims) · value (right-aligned, at least 62 px) · unit (24 px). DC-1: while the text
    /// differs from the committed value the label column is Auto, the value takes the rest and the unit collapses.
    /// DN-5: at 150 % text and above the value drops under its label, still right-aligned.
    /// </summary>
    private void Layout(RowView view)
    {
        var grid = view.Grid;
        bool dirty = view.Input?.Classes.Contains("dirty") == true;
        double gap = Token("PropColumnGap");
        // A wide value that leaves its label less room than the label's longest word would wrap it a letter per line
        // ("Station t/c" beside "From the Thickness curve ▾" in a narrow pane): that row stacks, as DN-5 does at 150 %.
        bool stacked = textScale >= StackedFrom || view.Wide && !dirty && grid.Bounds.Width > 0 && view.Cell.Bounds.Width > 0 &&
            grid.Bounds.Width - view.Cell.Bounds.Width - 2 * gap < LongestWord(view.Label);
        double valueWidth = Token("PropValueMinWidth");
        double unitWidth = Token("PropUnitWidth") + gap;
        double height = Token(view.Row.IsEditable ? "PropRowInputHeight" : "PropRowReadOnlyHeight");
        // DC-1: the label keeps its one-line width (B: flex 0 0 auto); an Auto column beside a star one measured it at 0.
        if (dirty) view.Label.Measure(Size.Infinity);
        var columns = dirty
            ? new ColumnDefinitions { new(Math.Ceiling(view.Label.DesiredSize.Width), GridUnitType.Pixel), new(1, GridUnitType.Star) { MinWidth = valueWidth }, new(0, GridUnitType.Pixel) }
            // A wide value spans every column, so its row fixes the value column at 62 px to keep one value edge (F-1).
            : new ColumnDefinitions { new(1, GridUnitType.Star), view.Wide ? new(valueWidth, GridUnitType.Pixel) : new(GridLength.Auto) { MinWidth = valueWidth }, new(unitWidth, GridUnitType.Pixel) };
        if (!SameColumns(grid.ColumnDefinitions, columns)) grid.ColumnDefinitions = columns;
        grid.RowDefinitions[0].MinHeight = stacked ? 0 : height;
        grid.RowDefinitions[1].MinHeight = stacked ? height : 0;
        Grid.SetRow(view.Label, 0);
        Grid.SetColumnSpan(view.Label, stacked ? 3 : 1);
        Grid.SetRow(view.Cell, stacked ? 1 : 0);
        Grid.SetColumn(view.Cell, view.Wide ? 0 : 1);
        Grid.SetColumnSpan(view.Cell, view.Wide ? 3 : 1);
        Grid.SetRow(view.Unit, stacked ? 1 : 0);
        Grid.SetColumn(view.Unit, 2);
        view.Unit.IsVisible = !dirty && !view.Wide;
        // A wide value reaches into the label column only by what the value and unit columns cannot hold.
        double reserve = 2 * gap + (view.Wide && !stacked ? Math.Max(0, view.Cell.Bounds.Width - valueWidth - unitWidth) : 0);
        var margin = new Thickness(0, 0, stacked ? 0 : reserve, 0);
        if (view.Label.Margin != margin) view.Label.Margin = margin;
    }

    private static double LongestWord(TextBlock label) =>
        (label.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => new FormattedText(word,
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(label.FontFamily, label.FontStyle, label.FontWeight),
            label.FontSize, null).WidthIncludingTrailingWhitespace).DefaultIfEmpty(0).Max();

    private static bool SameColumns(ColumnDefinitions current, ColumnDefinitions wanted) =>
        current.Count == wanted.Count && current.Zip(wanted).All(pair =>
            pair.First.Width == pair.Second.Width && Math.Abs(pair.First.MinWidth - pair.Second.MinWidth) < 0.01);

    private RowView? FocusedRow() =>
        rows.Values.FirstOrDefault(view => view.Root.IsKeyboardFocusWithin && view.Outer.IsAttachedToVisualTree());

    /// <summary>DN-6 / CL-5: the focused row, with its help and message lines, is brought into view once laid out.</summary>
    private void BringRowIntoView(RowView view) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (view.Outer.IsAttachedToVisualTree()) view.Outer.BringIntoView();
        }, DispatcherPriority.Background);

    private void FillCopyMenu(ContextMenu menu, IReadOnlyList<PropertyGroup> groups)
    {
        // DN-3: no row context menus; the group header's menu copies the group, or one row with or without its unit.
        // An action row (Rebuild…, Edit section…) is a link, not a value: it has nothing to copy.
        var keys = Copyable(groups).Select(row => (row.Key, row.Label, CopiesWithUnit(row))).ToList();
        var signature = string.Join("|", keys.Select(key => key.Key + key.Item3));
        if (menu.Tag as string == signature) return;
        menu.Tag = signature;
        var ids = groups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        var items = new List<MenuItem>();
        var all = new MenuItem { Header = "Copy values" };
        all.Click += (_, _) => CopyLines(CurrentGroups(ids));
        items.Add(all);
        foreach (var (key, label, withUnit) in keys)
        {
            var plain = new MenuItem { Header = $"Copy {label}" };
            plain.Click += (_, _) => CopyRow(key, withUnit: false);
            items.Add(plain);
            if (!withUnit) continue;
            var united = new MenuItem { Header = $"Copy {label} with unit" };
            united.Click += (_, _) => CopyRow(key, withUnit: true);
            items.Add(united);
        }
        menu.ItemsSource = items;
    }

    private static IEnumerable<PropertyRow> Copyable(IReadOnlyList<PropertyGroup> groups) =>
        groups.SelectMany(group => group.Rows).Where(row => row.Kind is not RowKind.Action);

    private List<PropertyGroup> CurrentGroups(IReadOnlySet<string> ids) =>
        shownModel?.Blocks.Where(group => ids.Contains(group.Id)).ToList() ?? [];

    private void CopyRow(string key, bool withUnit)
    {
        var row = shownModel?.Blocks.SelectMany(group => group.Rows).FirstOrDefault(item => item.Key == key);
        if (row is null) return;
        Write(RowCopyText(row, withUnit));
    }

    private void Write(string text)
    {
        var write = ClipboardWriter ?? (value => TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(value) ?? Task.CompletedTask);
        _ = write(text);
    }

    private GroupView CreateGroup(string id)
    {
        var chevron = new Path { Name = Part("GroupChevron", id), Data = Avalonia.Media.Geometry.Parse(ChevronOpen) };
        chevron.Classes.Add("prop-chevron");
        var title = new TextBlock { Name = Part("GroupTitle", id), Margin = Thickness("PropTitleGap") };
        title.Classes.Add("prop-group-title");
        var summary = new TextBlock { Name = Part("GroupSummary", id) };
        summary.Classes.Add("prop-summary");
        // The summary sits at the right edge and truncates; the name never does (DESIGN.md §12.0f, property group).
        summary.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(title, 1);
        Grid.SetColumn(summary, 2);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), Children = { chevron, title, summary } };
        foreach (var part in new Control[] { chevron, title, summary })
            AutomationProperties.SetAccessibilityView(part, AccessibilityView.Raw);
        var body = new StackPanel { Name = Part("Body", id) };
        body.Classes.Add("prop-body");
        WatchLead(body);
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
        // PG-25 / DN-3: a header's menu (Shift+F10 or the menu key on a focused header) copies its group or one row.
        var menu = new ContextMenu { Name = Part("GroupMenu", id) };
        expander.ContextMenu = menu;
        var root = new Border { Child = expander, BorderThickness = Thickness("PropRuleBottom") };
        root.Bind(Border.BorderBrushProperty, root.GetResourceObservable("LineBrush"));
        var view = new GroupView(root, expander, chevron, title, summary, body, menu);
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
        view.Chevron.Data = Avalonia.Media.Geometry.Parse(expanded ? ChevronOpen : ChevronClosed);
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
    }

    // A pane bound before it is attached reads the application's tokens; it re-reads them on its first render attached.
    private double Token(string key) =>
        (this.TryFindResource(key, out var value) || Application.Current?.TryFindResource(key, out value) == true) && value is double number
            ? number : 0;

    private Thickness Thickness(string key) =>
        (this.TryFindResource(key, out var value) || Application.Current?.TryFindResource(key, out value) == true) && value is Thickness thickness
            ? thickness : default;

    // ---------------- the editable value's template parts (CL-3 option A, DR-CELL-3) ----------------

    private void WireCue(TextBox box)
    {
        box.TemplateApplied += (_, e) =>
        {
            if (e.NameScope.Find<Border>("PART_BorderElement") is not { Parent: Panel panel } ||
                e.NameScope.Find<TextPresenter>("PART_TextPresenter") is not { } presenter) return;
            if (cues.TryGetValue(box, out var old)) panel.Children.RemoveAll([old.Cue, old.Ring]);
            // DR-CELL-3: a focused value in error keeps its accent box; the danger box is drawn 1 px outside it.
            var ring = new Border { Name = "PART_ErrorRing", IsHitTestVisible = false, IsVisible = false };
            ring.Classes.Add("prop-error-ring");
            var cue = new EditCue(box, presenter, Token("PropCueOffset"));
            cue.Bind(EditCue.StrokeProperty, cue.GetResourceObservable("PrimaryBrush"));
            panel.Children.Add(ring);
            panel.Children.Add(cue);
            cues[box] = (cue, ring);
            UpdateCue(box);
        };
        box.PropertyChanged += (_, change) =>
        {
            if (change.Property == IsFocusedProperty || change.Property == IsEnabledProperty) UpdateCue(box);
        };
        box.TextChanged += (_, _) =>
        {
            UpdateCue(box);
            UpdateDirty(box);
        };
    }

    private void SetError(TextBox box, bool error)
    {
        box.Classes.Set("error", error);
        UpdateCue(box);
    }

    /// <summary>The dotted underline shows at rest only; focus or an error replaces it with a box (SC 1.4.1, 2.4.7).</summary>
    private void UpdateCue(TextBox box)
    {
        if (!cues.TryGetValue(box, out var parts)) return;
        bool error = box.Classes.Contains("error");
        parts.Cue.IsVisible = !box.IsFocused && !error && !string.IsNullOrEmpty(box.Text);
        parts.Ring.IsVisible = box.IsFocused && error;
        parts.Cue.InvalidateVisual();
    }

    private void UpdateDirty(TextBox box)
    {
        if (!inputOwners.TryGetValue(box, out var view)) return;
        // A refused value keeps the 62 px box (the error state); only a fresh edit widens the field.
        bool dirty = box.IsEnabled && Dirty(box) && !RefusedAlready(view, box);
        if (box.Classes.Contains("dirty") == dirty) return;
        box.Classes.Set("dirty", dirty);
        Layout(view);
    }

    private static void FitEnumTemplate(INameScope scope)
    {
        // Fluent reserves a 32 px column for the drop-down glyph; B draws a small ▾ right after the text.
        if (scope.Find<PathIcon>("DropDownGlyph") is { Parent: Grid grid } && grid.ColumnDefinitions.Count == 2)
            grid.ColumnDefinitions = new ColumnDefinitions("*,Auto");
    }

    /// <summary>
    /// CL-3 option A: the dotted underline under an editable value's text, drawn inside the TextBox template. It is sized
    /// to the text (the presenter's layout, after right alignment and scrolling) and snapped to whole DIPs, so the 1 px line
    /// covers whole device pixels at 1× and 2×.
    /// </summary>
    public sealed class EditCue : Control
    {
        public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<EditCue, IBrush?>(nameof(Stroke));

        private readonly TextBox box;
        private readonly TextPresenter presenter;
        private readonly double offset;

        static EditCue() => AffectsRender<EditCue>(StrokeProperty);

        public EditCue(TextBox box, TextPresenter presenter, double offset)
        {
            this.box = box;
            this.presenter = presenter;
            this.offset = offset;
            Name = "PART_EditCue";
            IsHitTestVisible = false;
            presenter.LayoutUpdated += (_, _) => InvalidateVisual();
        }

        public IBrush? Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        /// <summary>The underline as drawn, in this control's coordinates; null when there is no text to underline.</summary>
        public (Point Start, Point End)? Line()
        {
            string text = presenter.Text ?? box.Text ?? "";
            if (text.Length == 0 || TopLevel.GetTopLevel(this) is not { } root) return null;
            var layout = presenter.TextLayout;
            var rects = layout.HitTestTextRange(0, text.Length).ToList();
            if (rects.Count == 0) return null;
            double left = rects.Min(rect => rect.Left), right = rects.Max(rect => rect.Right);
            if (presenter.TranslatePoint(default, this) is not { } origin || this.TranslatePoint(default, root) is not { } absolute) return null;
            double start = origin.X + left, end = origin.X + right;
            if (presenter.FindAncestorOfType<ScrollViewer>() is { } viewer && viewer.TranslatePoint(default, this) is { } port)
            {
                start = Math.Max(start, port.X);
                end = Math.Min(end, port.X + viewer.Bounds.Width);
            }
            double x0 = Math.Round(absolute.X + start) - absolute.X;
            double x1 = Math.Round(absolute.X + end) - absolute.X;
            double top = Math.Round(absolute.Y + origin.Y + layout.Baseline + offset) - absolute.Y;
            return x1 > x0 ? (new Point(x0, top + 0.5), new Point(x1, top + 0.5)) : null;
        }

        public override void Render(DrawingContext context)
        {
            if (Stroke is not { } stroke || Line() is not { } line) return;
            context.DrawLine(new Pen(stroke, 1, new DashStyle([1, 1], 0)), line.Start, line.End);
        }
    }

    // ---------------- inputs: commit, refuse, escape, nudge ----------------

    private void WireInput(TextBox box)
    {
        // Tunnel: the field sees Return, Tab, Esc and the run arrows before TextBox moves the caret with them.
        box.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        box.AddHandler(KeyUpEvent, OnInputKeyUp, RoutingStrategies.Tunnel);
        box.LostFocus += OnInputLostFocus;
        if (!pooledInputs.ContainsValue(box)) WireCue(box);
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
            // DR-STATUS-1 / DR-STATUS-3: the fit warning goes to the strip and the toast, the row keeps its state; an echo
            // goes to the strip.
            if (aboveLimit) messages[row.Key] = new RowMessage(committed.Report, MessageKind.Warning);
            else messages.Remove(row.Key);
            shown[box] = box.Text ?? "";
            Bind(controller);
            if (aboveLimit) Reported?.Invoke(new StatusReport(committed.Report, ReportKind.Warning, Toast: true));
            else if (echo is not null) Reported?.Invoke(new StatusReport(echo));
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

    // ---------------- the section mode (§11.3–§11.4): one typed value is one step ----------------

    /// <summary>
    /// A typed x, y, handle angle or handle length of a section point: one step (§11.4). x takes % c or mm (converted at the
    /// edited station's chord and echoed in % in the strip); y refuses mm with its reason. A refusal is a field error at the
    /// field (UI-39) and makes no step.
    /// </summary>
    private bool CommitSectionPoint(RowView view, TextBox box, WorkbenchController controller, PointRef target)
    {
        if (controller.Section is not { } mode) return false;
        var curve = controller.SectionCurve(SectionPoints.Side(target.Curve));
        var point = curve?.Points.FirstOrDefault(item => item.Id == target.VertexId);
        if (curve is null || point is null) return false;
        double chord = Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment).StationChordMeters;
        double x = point.SpanMeters, y = point.Ordinate;
        string? echo = null;
        var row = view.Row;
        if (row.Axis is RowAxis.Span or RowAxis.Value)
        {
            string? refusal = SectionPoints.Parse(box.Text, row.Axis == RowAxis.Span, chord, out double fraction, out echo);
            if (refusal is not null) return Refuse(view, box, refusal);
            if (row.Axis == RowAxis.Span) x = fraction; else y = fraction;
        }
        else
        {
            var anchor = curve.Points.FirstOrDefault(item => Math.Abs(item.Index - point.Index) == 1 && item.Role == PointRole.Anchor);
            if (anchor is null) return false;
            if (!Parse(view, box, Dimensions(), out double typed)) return false;
            var (angle, length) = PropertiesView.SectionHandle(point, anchor);
            if (row.Axis == RowAxis.Angle) angle = typed;
            else length = typed / 1000 / chord;
            (x, y) = PropertiesView.SectionHandleAt(anchor, angle, length);
        }
        Task<string?> task;
        using (Hold())
        {
            task = SectionPoints.CommitAsync(controller, target, x, y);
            PumpUi(task);
        }
        string? refused = task.IsCompletedSuccessfully ? task.Result : "This change couldn't be checked, so it wasn't applied. Nothing changed.";
        if (refused is not null) return Refuse(view, box, refused);
        errors.Remove(row.Key);
        messages.Remove(row.Key);
        MarkShown([row.Key]);
        Bind(controller);
        if (echo is not null) Reported?.Invoke(new StatusReport(echo));
        return true;
    }

    /// <summary>Type · both surfaces (Ruling 60): one step that sets the type on both surfaces.</summary>
    private void CommitSectionType(PointRef target, string value) =>
        RunSectionStep("p:type", new SectionStep.SetType(SectionPoints.Side(target.Curve), target.VertexId, value == "anchor"));

    /// <summary>Kind · both surfaces: one step. A Fixed angle keeps the tail handle's present angle.</summary>
    private void CommitSectionKind(PointRef target, TangentKind kind)
    {
        if (boundController is not { } controller) return;
        double? angle = null;
        if (kind == TangentKind.Angle && controller.SectionCurve(SectionPoints.Side(target.Curve)) is { } curve &&
            curve.Points.FirstOrDefault(item => item.Id == target.VertexId) is { } anchor && anchor.Index + 1 < curve.Points.Count)
            angle = PropertiesView.SectionHandle(curve.Points[anchor.Index + 1], anchor).AngleDegrees;
        RunSectionStep("t:kind", new SectionStep.SetTangent(SectionPoints.Side(target.Curve), target.VertexId, kind, angle, null));
    }

    /// <summary>Station t/c (§11.4): From the Thickness curve, or From this section.</summary>
    private void CommitIntent(string value) =>
        RunSectionStep("sec:intent", new SectionStep.Thickness(value == "source" ? ThicknessIntent.UseSource : ThicknessIntent.KeepCurrent));

    private void RunSectionStep(string key, SectionStep step)
    {
        if (boundController is not { } controller) return;
        if (SectionStepRequested is { } shell)
        {
            using (Hold()) PumpUi(shell(step));
        }
        else
        {
            var task = controller.ApplySectionStepAsync(step);
            using (Hold()) PumpUi(task);
            if (task.Exception?.InnerException is ContractError error)
                messages[key] = new RowMessage($"{error.Reason ?? error.Code} Nothing changed.", MessageKind.Error);
        }
        Bind(controller);
    }


    private bool CommitPoint(RowView view, TextBox box)
    {
        if (boundController is not { } controller || view.Row.Target is not { } target) return false;
        if (target.Curve is "upper" or "lower") return CommitSectionPoint(view, box, controller, target);
        Func<string, CurveView?> curves = controller.CurveFor;
        if (PropertiesView.Find(curves, target) is not { } point) return false;
        var dims = Dimensions();
        var echoes = new Dictionary<string, string>(StringComparer.Ordinal);
        var committedKeys = new List<string> { view.Row.Key };
        (double Span, double Aft) destination;
        RowMessage? hint = null;
        if (view.Row.Axis is RowAxis.Span or RowAxis.Value)
        {
            // One intent, one undo row: a dirty From root and value of the same point commit together.
            double span = point.SpanMeters, value = point.Ordinate;
            foreach (var field in PositionFields(target))
            {
                if (field.Input is not { } input) continue;
                bool include = field == view || input.IsEnabled && Dirty(input);
                if (!include) continue;
                if (!Parse(field, input, dims, out double typed)) return false;
                (span, value) = field.Row.Axis == RowAxis.Span ? (typed / PropertiesView.FieldScale[UnitFamily.Length], value)
                    : (span, typed / PropertiesView.FieldScale[field.Row.Family]);
                if (!committedKeys.Contains(field.Row.Key)) committedKeys.Add(field.Row.Key);
                if (UnitEntry.TryParse(input.Text, field.Row.Family, dims, out var entry))
                {
                    if (UnitEntry.Echo(input.Text!, entry, field.Row.Unit ?? "") is { } echo) echoes[field.Row.Key] = echo;
                    hint ??= UnitEntry.Hint(entry, field.Row.Family);
                }
            }
            destination = (span, value);
        }
        else
        {
            if (!Parse(view, box, dims, out double value)) return false;
            destination = PropertiesView.Destination(view.Row, point, value, curves, controller.Planform);
            if (UnitEntry.TryParse(box.Text, view.Row.Family, dims, out var entry) &&
                UnitEntry.Echo(box.Text!, entry, view.Row.Unit ?? "") is { } echo)
                echoes[view.Row.Key] = echo;
        }
        string typedText = box.Text ?? "";
        var outcome = RunTypedGesture(controller, target, destination);
        switch (outcome)
        {
            case GestureOutcome.Committed:
                foreach (var key in committedKeys) { errors.Remove(key); messages.Remove(key); }
                MarkShown(committedKeys);
                Bind(controller);
                // DR-STATUS-3: unit and expression echoes go to the strip, not under the field. MC-19: a value Core clamped
                // (twist or t/c past the domain) says so with Core's number, as a warning (COPY-168); MC-20: a t/c under
                // 1 % warns with the fraction hint (COPY-169).
                if (view.Row.Axis == RowAxis.Value && controller.LastGestureClamped &&
                    PropertiesView.ReadValue(view.Row, curves) is { } reached &&
                    UnitEntry.TryParse(typedText, view.Row.Family, dims, out var asked))
                {
                    string copy = PropertyCopy.Clamped(typedText, Quantity.WithUnit(Quantity.Typed(reached), view.Row.Unit ?? ""), asked.Value > reached);
                    messages[view.Row.Key] = new RowMessage(copy, MessageKind.Warning);
                    RenderRow(view.Row, []);
                    Reported?.Invoke(new StatusReport(copy, ReportKind.Warning));
                }
                else if (hint is not null)
                {
                    messages[view.Row.Key] = hint;
                    RenderRow(view.Row, []);
                    Reported?.Invoke(new StatusReport(hint.Text, ReportKind.Warning));
                }
                else if (echoes.Count > 0) Reported?.Invoke(new StatusReport(string.Join(" ", echoes.Values)));
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

    /// <summary>The shown From root and value fields of one point (or handle), which commit as one gesture.</summary>
    private IEnumerable<RowView> PositionFields(PointRef target) =>
        (shownModel?.Blocks ?? []).SelectMany(group => group.Rows)
            .Where(row => row.Kind == RowKind.Input && row.Axis is RowAxis.Span or RowAxis.Value && row.Target == target)
            .Select(row => rows.GetValueOrDefault(row.Key + "|Input")).OfType<RowView>();


    private bool Parse(RowView view, TextBox box, IReadOnlyDictionary<string, double> dims, out double value)
    {
        var row = view.Row;
        value = double.NaN;
        if (!UnitEntry.TryParse(box.Text, row.Family, dims, out var entry))
            return Refuse(view, box, PropertyCopy.NotANumber(row.Label));
        if (row.MustBePositive && entry.Value <= 0)
            return Refuse(view, box, PropertyCopy.NotPositive(row.Label));
        if (row.AngleBounded && Math.Abs(entry.Value) >= 90)
            return Refuse(view, box, PropertyCopy.AngleOutOfRange(row.Label, PropertiesView.AngleSense(row)));
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
        SetError(box, true);
        UpdateDirty(box);
        AutomationProperties.SetHelpText(box, HelpText(view));
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
            // The run steps from where the point was when it began: a snapshot of its curve and the plan.
            if (controller.CurveFor(target.Curve) is not { } startCurve) return;
            CurveView? Start(string curve) => curve == target.Curve ? startCurve : null;
            if (PropertiesView.ReadValue(view.Row, Start) is not { } origin) return;
            using (Hold())
            {
                var selection = controller.Selection;
                if (!controller.BeginGesture(target, GestureInput.Typed)) return;
                controller.Select(selection);
            }
            var start = PropertiesView.Find(Start, target)!;
            run = new NudgeRun(view, box, target, Start, controller.Planform, origin, origin, (start.SpanMeters, start.Ordinate));
        }
        var active = run;
        // COPY-163: ⌘ (Ctrl on Windows) 0.01, Shift 1, plain 0.1 — in the field's unit (mm, °, %).
        double step = modifiers.HasFlag(KeyModifiers.Meta) || modifiers.HasFlag(KeyModifiers.Control) ? 0.01
            : modifiers.HasFlag(KeyModifiers.Shift) ? 1 : 0.1;
        double next = Math.Round(active.Value + direction * step, 6);
        if (view.Row.AngleBounded && Math.Abs(next) >= 90)
        {
            StopsHere();
            return;
        }
        var destination = RunTarget(active, next);
        controller.UpdateGesture(destination.Span, destination.Aft);
        controller.FlushGestureFrame();
        double reached = PropertiesView.ReadValue(view.Row, controller.CurveFor) is { } value ? value : next;
        if (view.Row.AngleBounded && Math.Abs(reached - active.Value) < step / 4)
        {
            // MC-23: Core's ordering clamp holds a handle short of ±90°. The run stops at its last position, so a run
            // that never got past the bound changes nothing and makes no undo row.
            controller.UpdateGesture(active.Accepted.Span, active.Accepted.Aft);
            controller.FlushGestureFrame();
            StopsHere();
            return;
        }
        run = active with { Value = reached, Accepted = destination };
        SetShown(box, Quantity.ForField(Quantity.Typed(reached)));
    }

    /// <summary>MC-23: a run held at the angle bound stops there and says so (COPY-170) in the strip; a gesture warning never toasts.</summary>
    private void StopsHere() => Reported?.Invoke(new StatusReport(PropertyCopy.AngleRunStops, ReportKind.Warning));

    private static (double Span, double Aft) RunTarget(NudgeRun active, double value) =>
        PropertiesView.Destination(active.View.Row, PropertiesView.Find(active.Start, active.Target)!, value, active.Start, active.Plan);

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
        string? report = null;
        if (task.IsCompletedSuccessfully && task.Result is GestureOutcome.Committed)
        {
            // PG-08: the new value is reported once, in the strip, on release.
            string value = Quantity.Typed(PropertiesView.ReadValue(row, controller.CurveFor) is { } now ? now : active.Value);
            report = $"{row.Label} {Quantity.WithUnit(value, row.Unit ?? "")}.";
            messages.Remove(row.Key);
        }
        else if (reason == GestureEnd.Escape)
            messages.Remove(row.Key);
        shown.Remove(active.Box);
        Bind(controller);
        if (report is not null) Reported?.Invoke(new StatusReport(report));
    }

    private sealed record NudgeRun(RowView View, TextBox Box, PointRef Target, Func<string, CurveView?> Start, PlanformView? Plan,
        double Origin, double Value, (double Span, double Aft) Accepted);


    // ---------------- Type and Tangent kind: one enum rule (PG-07, PG-19, DR-CELL-2) ----------------

    /// <summary>An enum value and what it has staged: arrows on the closed box stage, Return or a pick applies, Esc keeps,
    /// leaving drops and says so (DR-CELL-2 amends PG-06 / MC-1).</summary>
    private sealed class EnumField(ComboBox box, string cacheKey, string noun)
    {
        public ComboBox Box { get; } = box;
        public string CacheKey { get; } = cacheKey;
        public string Noun { get; } = noun;
        public string? Pending { get; set; }
    }

    private void RenderEnum(EnumField field, RowView view, PropertyRow row)
    {
        var box = field.Box;
        box.IsVisible = true;
        var options = row.Options ?? [];
        if (box.ItemCount != options.Count || !box.Items.OfType<ComboBoxItem>().Select(item => (item.Tag as string, item.IsEnabled))
                .SequenceEqual(options.Select(option => ((string?)option.Value, option.Enabled))))
            box.ItemsSource = options.Select(option =>
            {
                var item = new ComboBoxItem { Content = option.Text, Tag = option.Value, IsEnabled = option.Enabled };
                if (option.Reason is { } reason)
                {
                    ToolTip.SetTip(item, reason);
                    AutomationProperties.SetHelpText(item, reason);
                }
                return item;
            }).ToList();
        string shownValue = field.Pending ?? row.Value;
        int index = options.ToList().FindIndex(option => option.Value == shownValue);
        if (box.SelectedIndex != index)
        {
            syncingChoice = true;
            try { box.SelectedIndex = index; }
            finally { syncingChoice = false; }
        }
        box.IsEnabled = true;
        AutomationProperties.SetName(box, row.AutomationName ?? row.Label);
        string committed = OptionText(row, row.Value);
        string? description = field == kindField && row.Target is not { Curve: "upper" or "lower" }
            ? PropertyCopy.KindDescription(Enum.Parse<TangentKind>(shownValue)) : row.Description;
        view.Description.Text = description ?? "";
        // CB-4: while a value is staged the box says which keys apply it and which keep the committed one.
        AutomationProperties.SetHelpText(box, field.Pending is not null ? $"Return applies; Esc keeps {committed.ToLowerInvariant()}" : description);
        if (field.Pending is { } pending)
            ShowMessage(view, new RowMessage(field == kindField
                ? PropertyCopy.PendingKind(OptionText(row, pending).ToLowerInvariant(), committed.ToLowerInvariant())
                : PropertyCopy.PendingType, MessageKind.Info));
    }

    private static string OptionText(PropertyRow row, string value) =>
        row.Options?.FirstOrDefault(option => option.Value == value)?.Text ?? value;

    private RowView? EnumRowView(EnumField field) =>
        rows.TryGetValue(field.CacheKey, out var view) && view.Root.IsAttachedToVisualTree() ? view : null;

    private void OnEnumSelectionChanged(EnumField field)
    {
        if (syncingChoice || rendering || EnumRowView(field) is not { } view) return;
        if ((field.Box.SelectedItem as ComboBoxItem)?.Tag is not string value) return;
        if (field.Box.IsDropDownOpen) return;   // a pick in the open list applies when the list closes (CB-3)
        // Arrows on the closed box stage the value; Return applies it, Esc keeps the committed one, leaving drops it.
        field.Pending = value == view.Row.Value ? null : value;
        RenderEnum(field, view, view.Row);
        if (field.Pending is null) ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
    }

    private void OnEnumDropDownClosed(EnumField field)
    {
        if (EnumRowView(field) is not { } view) return;
        // CB-3: a pick that closes the list applies once; arrows then Esc closed it on the committed value (no row).
        if ((field.Box.SelectedItem as ComboBoxItem)?.Tag is string value && value != view.Row.Value) CommitEnum(field, view, value);
    }

    private void OnEnumKeyDown(EnumField field, KeyEventArgs e)
    {
        if (EnumRowView(field) is not { } view || field.Box.IsDropDownOpen) return;
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            if (field.Pending is { } pending) CommitEnum(field, view, pending);
        }
        else if (e.Key == Key.Escape && field.Pending is not null)
        {
            e.Handled = true;
            field.Pending = null;
            RenderEnum(field, view, view.Row);
            ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
        }
    }

    private void DropPending(EnumField field)
    {
        if (field.Pending is null || field.Box.IsDropDownOpen || EnumRowView(field) is not { } view) return;
        field.Pending = null;   // D1 / PG-19 / DR-CELL-2: leaving the box with a staged value does not apply it
        RenderEnum(field, view, view.Row);
        ShowMessage(view, messages.GetValueOrDefault(view.Row.Key));
        // CL-2: the drop is reported in the strip, so leaving is never a silent loss.
        Reported?.Invoke(new StatusReport($"{field.Noun} unchanged: {OptionText(view.Row, view.Row.Value)}."));
    }

    private void CommitEnum(EnumField field, RowView view, string value)
    {
        field.Pending = null;
        if (field == intentField) CommitIntent(value);
        else if (view.Row.Target is { Curve: "upper" or "lower" } section)
        {
            if (field == kindField) CommitSectionKind(section, Enum.Parse<TangentKind>(value));
            else CommitSectionType(section, value);
        }
        else if (field == kindField) CommitKind(Enum.Parse<TangentKind>(value));
        else CommitType(view, value);
    }

    private void CommitType(RowView view, string value)
    {
        if (boundController is not { } controller || view.Row.Target is not { } target) return;
        var rail = controller.CurveFor(target.Curve);
        var point = rail?.Points.FirstOrDefault(item => item.Id == target.VertexId);
        if (rail is null || point is null) return;
        PointCommand command = value == "anchor" ? new PointCommand.MakeAnchor(point.Curve, point.Id) : new PointCommand.MakeControl(point.Curve, point.Id);
        var task = controller.ApplyPointCommandAsync(command);
        using (Hold()) PumpUi(task);
        if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Committed committed)
        {
            int before = rail.Points.Count;
            int after = controller.CurveFor(target.Curve)?.Points.Count ?? before;
            var rows = PropertiesView.Curves[target.Curve];
            string curve = rows.Name;
            // The deviation is the operation's own number in the curve's unit (COPY-154 / COPY-165), read from its report.
            string largest = Deviation().Match(committed.Report) is { Success: true } match
                ? $" Largest change {match.Groups[1].Value}." : "";
            string report = value == "anchor"
                ? $"{curve} point {point.Index + 1} is now an anchor point with 2 handles. The {rows.Noun} gained {after - before} points ({before} → {after}).{largest}"
                : $"{curve} point {point.Index + 1} is now a control point. Its handles are removed; the {rows.Noun} has {after} points (was {before}).{largest}";
            messages.Remove("p:type");
            Reported?.Invoke(new StatusReport(report));
        }
        else if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Refused refused)
            messages["p:type"] = new RowMessage(refused.Copy, MessageKind.Error);
        Bind(controller);
    }

    [GeneratedRegex(@"Max deviation ([0-9.]+(?: mm|°| %))")]
    private static partial Regex Deviation();

    private void CommitKind(TangentKind kind)
    {
        if (rendering || boundController is not { } controller ||
            !rows.TryGetValue(kindField.CacheKey, out var view) || view.Row.Target is not { } target ||
            PropertiesView.Find(controller.CurveFor, target) is not { } anchor)
            return;
        Func<string, CurveView?> curves = controller.CurveFor;
        if (anchor.Kind != kind)
        {
            // On a handle the kind keeps the selected handle and moves the other one (COPY-162).
            string? keep = controller.Selection is Selection.Points { Items.Count: 1 } points &&
                           PropertiesView.Find(curves, points.Items[0]) is { AnchorId: { } owner } handle && owner == anchor.Id
                ? handle.Id : null;
            var task = controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, kind, keep));
            using (Hold()) PumpUi(task);
            if (task.IsCompletedSuccessfully && task.Result is CommitOutcome.Refused refused)
                messages["t:kind"] = new RowMessage(refused.Copy, MessageKind.Error);
            else if (task.IsCompletedSuccessfully)
            {
                // MC-11 / PG-33: the report comes from the operation — the kind, and on a handle which handle it kept.
                string kept = keep is null ? ""
                    : PropertiesView.Find(curves, new PointRef(anchor.Curve, keep)) is { } keptHandle && keptHandle.Index > anchor.Index
                        ? " Kept the handle toward the tip; the other one moved." : " Kept the handle toward the root; the other one moved.";
                string report = $"{PropertiesView.Curves[anchor.Curve].Name} point {anchor.Index + 1} is now {kind}.{kept}";
                messages.Remove("t:kind");
                Reported?.Invoke(new StatusReport(report));
            }
        }
        // Tangent_KindChange_KeepsFocusOnKindBox: the box is persistent, so a re-render leaves focus on it.
        Bind(controller);
    }

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
            Reported?.Invoke(new StatusReport($"Selected {IdentityTitle.Text}."));
        }
        else if (e.Key == Key.C && (e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control)) &&
                 copyTarget is { } key && rows.TryGetValue(key, out var view))
        {
            e.Handled = true;
            CopyRow(view.Row.Key, withUnit: true);
        }
    }

    /// <summary>The rows as "label value unit" lines, one per row (PG-25).</summary>
    private void CopyLines(IReadOnlyList<PropertyGroup> groups)
    {
        var lines = Copyable(groups).Select(row => $"{row.Label} {RowCopyText(row, withUnit: true)}").ToList();
        if (lines.Count > 0) Write(string.Join("\n", lines));
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

    /// <summary>F-12: the placed frame at a station of the accepted revision (Core's Placement.Frame), or null when it cannot be placed.</summary>
    private static StationFrame? StationFrameAt(WorkbenchController controller, double eta)
    {
        if (controller.Inspection is null) return null;
        try { return Placement.Frame(System.Text.Encoding.UTF8.GetBytes(controller.AcceptedSource), eta); }
        catch (ContractError) { return null; }
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

    private sealed class RowView(Border outer, Border root, Grid grid, TextBlock label, TextBlock unit, TextBlock description,
        Border messageBox, TextBlock message)
    {
        public Border Outer { get; } = outer;          // carries the half-strength rule above the row
        public Border Root { get; } = root;            // the rail, the state classes and a fact's spoken name
        public Grid Grid { get; } = grid;
        public TextBlock Label { get; } = label;
        public TextBlock Unit { get; } = unit;
        public TextBlock Description { get; } = description;
        public Border MessageBox { get; } = messageBox;
        public TextBlock Message { get; } = message;
        public Control Cell { get; set; } = label;
        public bool Wide { get; set; }
        public TextBlock? Value { get; set; }
        public Path? Lock { get; set; }
        public Path? MessageIcon { get; init; }
        public Path? StateIcon { get; set; }           // the inline warning icon of a state-only commit warning
        public required Border Rule { get; init; }     // the half-strength rule above the row, shown after another row
        public TextBox? Input { get; set; }
        public ComboBox? Enum { get; set; }
        public HyperlinkButton? Link { get; set; }
        public InputElement? Editor => (InputElement?)Input ?? (InputElement?)Enum ?? Link;
        public required PropertyRow Row { get; set; }
    }

    private sealed class GroupView(Border root, Expander expander, Path chevron, TextBlock title, TextBlock summary, StackPanel body,
        ContextMenu menu)
    {
        public Border Root { get; } = root;            // carries the full-strength rule under the group
        public Expander Expander { get; } = expander;
        public Path Chevron { get; } = chevron;
        public TextBlock Title { get; } = title;
        public TextBlock Summary { get; } = summary;
        public StackPanel Body { get; } = body;
        public ContextMenu Menu { get; } = menu;
        public ToggleButton? Header { get; set; }
    }

    private sealed record SectionView(StackPanel Root, StackPanel Body);
}
