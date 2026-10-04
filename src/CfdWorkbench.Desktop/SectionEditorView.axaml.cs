using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Globalization;

namespace CfdWorkbench.Desktop;

public partial class SectionEditorView : UserControl
{
    private WorkbenchController? controller;
    private string? focusedDraft;
    private string? reasonDraft;   // the draft id and generation the reason box last followed
    private string? shownStateReason; // the state reason (Checking…, why Finish is off, a refused refit) the box last showed
    private ((SurfaceSide, double, double, double) Refusal, string? Copy)? refitCopy;

    public SectionEditorView()
    {
        InitializeComponent();
        ModeCancelButton.Click += (_, _) => controller?.CancelSection();
        ModeFinishButton.Click += (_, _) => _ = FinishAsync();
        CurvatureToggle.Click += (_, _) =>
        {
            ModeCanvas.CurvatureVisible = CurvatureToggle.IsChecked == true;
            ModeCombPlate.IsVisible = ModeCanvas.CurvatureVisible;
            ModeCanvas.InvalidateVisual();
        };
        ThicknessToggle.Click += (_, _) =>
        {
            ModeCanvas.ThicknessDoubled = ThicknessToggle.IsChecked == true;
            ModePlate.Text = PlateText();
            ModeCanvas.InvalidateVisual();
        };
        ModeSectionButton.Flyout = SectionMenu();
        ModeFitButton.Click += (_, _) => ModeCanvas.Fit();
        ModeFitSelectionButton.Click += (_, _) => ModeCanvas.FitSelection();
        ScopeChipLink.Click += (_, _) => { if (this.FindAncestorOfType<ShellHost>() is { } host) _ = host.RunCommand("section.make-unique"); };
        ModeCanvas.CancelTarget = ModeCancelButton;
        ModeCanvas.ReasonTarget = ModeReason;
        ModeCanvas.ReasonContainer = ModeReasonBox;
        ModeCanvas.ProbeTarget = ModeProbe;
        ModeCanvas.CombTarget = ModeCombLabel;
        // DR-NAV-1 and §11.3: Tab goes to the point's Type and Return to its x, both in the shell's Properties pane.
        ModeCanvas.TabOut = () => this.FindAncestorOfType<ShellHost>()?.Properties.FocusFirstValue() == true;
        ModeCanvas.ValueOut = () => this.FindAncestorOfType<ShellHost>()?.Properties
            .FindControl<TextBox>("PointSpanInput") is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } x && x.Focus();
    }

    // Section ▾ (§0.1 item 9): PNL's Section menu rows, less the three the mode bar already shows. Each item runs its row
    // through the shell, as the native menu does, so a row that cannot run names why in the strip (UI-DEAD-CONTROL).
    private static readonly string[] ModeBarRows = ["section.edit", "section.finish", "section.cancel"];
    private const string StationThicknessPrefix = "Station t/c from ";

    private MenuFlyout SectionMenu()
    {
        var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, FlyoutPresenterClasses = { "navbar-menu" } };
        var thickness = new MenuItem { Header = "Station t/c" };
        MenuItem? makeUnique = null;
        foreach (var row in CommandTable.Rows.Where(row => row.Menu == CommandTable.SectionMenu && !ModeBarRows.Contains(row.Id)))
        {
            bool stationThickness = row.Title.StartsWith(StationThicknessPrefix, StringComparison.Ordinal);
            var item = new MenuItem { Header = stationThickness ? "From " + row.Title[StationThicknessPrefix.Length..] : row.Title };
            item.Click += (_, _) => { if (this.FindAncestorOfType<ShellHost>() is { } host) _ = host.RunCommand(row.Id); };
            if (row.Id == "section.make-unique") makeUnique = item;
            (stationThickness ? thickness.Items : menu.Items).Add(item);
        }
        menu.Items.Add(thickness);
        // "Make unique to <station>" names the station being edited.
        menu.Opening += (_, _) =>
        {
            if (makeUnique is null || controller?.Section is not { } mode || controller.Inspection is not { } inspection) return;
            int station = mode.Draft.Assignment;
            makeUnique.Header = "Make unique to " + ElevationView.StationName(station, inspection.Authored.Assignments[station].Eta);
        };
        return menu;
    }

    private async Task FinishAsync()
    {
        if (controller is null) return;
        try { await controller.FinishSectionAsync(); }
        catch (ContractError)
        {
            ModeReason.Text = controller.Section?.FinishReason ?? "This section cannot Finish yet.";
            ModeReasonBox.IsVisible = true;
            ModeReason.Focus();
        }
    }

    private string PlateText()
    {
        if (controller?.Section is not { } mode || controller.Inspection is null) return "Section · display";
        var station = controller.Inspection.Authored.Assignments[mode.Draft.Assignment];
        string name = ElevationView.StationName(mode.Draft.Assignment, station.Eta);
        return $"Section · {name} · {station.SpanMeters * 1000:F2} mm from root · display" +
            (ThicknessToggle.IsChecked == true ? " · Thickness drawn ×2" : "");
    }

    public void Bind(WorkbenchController controller)
    {
        this.controller = controller;
        var mode = controller.Section;
        if (mode is null) focusedDraft = null;
        ModeEditor.IsVisible = mode is not null;
        LegacyEditor.IsVisible = mode is null;
        int? assignment = mode?.Draft.Assignment ?? (controller.Selection is Selection.Station station ? station.Index : null);
        if (controller.Inspection is null || assignment is null)
        {
            EditableSectionCanvas.Profile = null;
            EditableSectionCanvas.RefitMarker = null;
            SectionEmptyText.IsVisible = true;
            return;
        }

        try
        {
            EditableSectionCanvas.Profile = controller.SectionView(assignment.Value);
            if (mode is not null)
            {
                ModeCanvas.Controller = controller;
                ModeCanvas.Profile = EditableSectionCanvas.Profile;
                ModeCanvas.SelectedVertex = controller.Selection is Selection.Points picked && picked.Items.Count > 0
                    ? (picked.Items[0].Curve, picked.Items[0].VertexId) : null;
                var assignments = controller.Inspection.Authored.Assignments;
                string name = ElevationView.StationName(assignment.Value, assignments[assignment.Value].Eta);
                SetTitle(name);
                SetScopeChip(mode, assignments, assignment.Value, name);
                ModePlate.Text = PlateText();
                ModeFinishButton.IsEnabled = mode.CanFinish;
                string? reason = mode.FinishReason;
                AutomationProperties.SetHelpText(ModeFinishButton, reason);
                // A state reason (a refused refit, or why Finish is off, "Checking…" included) shows while its state holds
                // and goes when it ends, even within one draft generation. A gesture's refusal (⌫ on a named point, a refused
                // step, a refused strip switch) has no state behind it: it stays until the draft changes.
                string? stateReason = RefitCopy(controller) ?? (mode.IsDirty && !mode.CanFinish ? reason : null);
                string key = $"{mode.Draft.DraftId}:{mode.Draft.Generation}";
                if (stateReason is not null)
                {
                    ModeReason.Text = stateReason;
                    ModeReasonBox.IsVisible = true;
                    shownStateReason = stateReason;
                }
                else if (reasonDraft != key || shownStateReason is not null && ModeReason.Text == shownStateReason)
                {
                    // The state ended: its text goes. A gesture refusal written over it since then stays.
                    ModeReason.Text = null;
                    ModeReasonBox.IsVisible = false;
                    shownStateReason = null;
                }
                reasonDraft = key;
                RefreshStationStrip(controller, assignment.Value);
                if (focusedDraft != mode.Draft.DraftId)
                {
                    focusedDraft = mode.Draft.DraftId;
                    ModeCanvas.Focus();
                }
            }
            EditableSectionCanvas.RefitMarker = mode is not null && controller.SectionRefitRefusal is { } refusal
                ? new Point(refusal.ChordX, refusal.Side == SurfaceSide.Upper
                    ? Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, refusal.ChordX).UpperY
                    : Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, refusal.ChordX).LowerY)
                : null;
            ModeCanvas.RefitMarker = EditableSectionCanvas.RefitMarker;
            ModeCanvas.RefitMarkerLabel = controller.SectionRefitRefusal is { } measured
                ? $"{measured.Side.ToString().ToLowerInvariant()} would move {measured.DeviationMeters * 1000:F4} mm here (limit {measured.LimitMeters * 1000:F3} mm)"
                : null;
            SectionEmptyText.IsVisible = false;
        }
        catch (ContractError)
        {
            EditableSectionCanvas.Profile = null;
            EditableSectionCanvas.RefitMarker = null;
            SectionEmptyText.IsVisible = true;
        }
    }

    // COPY-173: "Editing <station> section", the station in the accent (mockup .ttl em).
    private void SetTitle(string station)
    {
        string text = $"Editing {station} section";
        if (ModeTitle.Text == text) return;
        ModeTitle.Text = text;
        ModeTitle.Inlines = [new Run("Editing "), new Run(station) { Foreground = Brush("PrimaryBrush") }, new Run(" section")];
    }

    // §0.1 steps 2 and 8: "Shared with <stations> · Make unique to <station>", or "Only <station> uses this section".
    private void SetScopeChip(SectionMode mode, IReadOnlyList<AuthoredAssignment> assignments, int active, string name)
    {
        var others = assignments.Select((row, index) => (row, index))
            .Where(item => item.index != active && item.row.ProfileName == mode.Draft.Profile)
            .Select(item => ElevationView.StationName(item.index, item.row.Eta)).ToArray();
        bool shared = mode.Draft.Scope == SectionScope.Shared && others.Length > 0;
        ScopeChip.Text = shared ? $"Shared with {string.Join(", ", others)} · " : $"Only {name} uses this section";
        ScopeChipLinkText.Text = $"Make unique to {name}";
        ScopeChipLink.IsVisible = shared;
        ToolTip.SetTip(ScopeChip.Parent as Control ?? ScopeChip, shared ? $"Shared with {string.Join(", ", others)}" : null);
    }

    // COPY-187 for a refused Anchor → Control, held for that refusal: the controller's status line moves on, the copy stays.
    private string? RefitCopy(WorkbenchController controller)
    {
        if (controller.SectionRefitRefusal is not { } refusal) { refitCopy = null; return null; }
        if (refitCopy is { } held && held.Refusal == refusal) return held.Copy;
        string? copy = null;
        if (controller.Selection is Selection.Points { Items.Count: > 0 } picked &&
            controller.SectionCurve(picked.Items[0].Curve == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower)?.Points
                .FirstOrDefault(point => point.Id == picked.Items[0].VertexId) is { } point)
            copy = ShellHost.RefitCopy(controller.Status, point.Index + 1, refusal.Side);
        refitCopy = (refusal, copy);
        return copy;
    }

    private IBrush? Brush(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) ? value as IBrush : null;

    // The strip's inputs: the accepted document, the draft's bytes and the active station. Bind runs on every shell
    // change, and rebuilding the strip there cost ~67 ms (Sections.Facts per station; edit-lag, 2026-10-04).
    private (object Inspection, byte[] Bytes, int Active)? stripInputs;

    private void RefreshStationStrip(WorkbenchController controller, int active)
    {
        var inputs = ((object)controller.Inspection!, controller.Section!.Draft.Bytes, active);
        if (stripInputs is { } shown && ReferenceEquals(shown.Inspection, inputs.Item1) && ReferenceEquals(shown.Bytes, inputs.Item2) &&
            shown.Active == active)
            return;
        stripInputs = inputs;
        StationStrip.Children.Clear();
        foreach (var (assignment, index) in controller.Inspection!.Authored.Assignments.Select((item, index) => (item, index)))
        {
            var facts = Sections.Facts(controller.Section!.Draft.Bytes, index);
            string name = ElevationView.StationName(index, assignment.Eta);
            string distance = string.Create(CultureInfo.InvariantCulture, $"{assignment.SpanMeters * 1000:F2} mm");
            string chord = string.Create(CultureInfo.InvariantCulture, $"c {facts.StationChordMeters * 1000:F2} mm");
            string ratio = string.Create(CultureInfo.InvariantCulture, $"t/c {facts.StationThicknessRatio * 100:F2} %");
            var button = new Button { Content = Thumbnail(controller.SectionView(SharesDraft(controller, index) ? active : index), name, distance, chord, ratio) };
            button.Classes.Add("station-thumb");
            button.Classes.Set("current", index == active);
            AutomationProperties.SetName(button, $"{name} section thumbnail");
            AutomationProperties.SetHelpText(button, $"{distance} from root, {chord[2..]} chord, {ratio}");
            if (index == active) AutomationProperties.SetItemStatus(button, "current");
            int target = index;
            button.Click += (_, _) => _ = SwitchStationAsync(target);
            StationStrip.Children.Add(button);
        }
    }

    // A station that shares the section being edited shows the draft shape too: the edit reaches it on Finish, and an old
    // outline there would say the edit is local (marine-CAD re-review, UXR).
    private static bool SharesDraft(WorkbenchController controller, int index) =>
        controller.Section is { Draft.Scope: SectionScope.Shared } mode &&
        controller.Inspection!.Authored.Assignments[index].ProfileName == mode.Draft.Profile;

    // Mockup .thumb: the station's section outline on the viewport, then "<name> · <distance>" and "c <chord> · t/c <t/c>".
    private static Control Thumbnail(ProfileView profile, string name, string distance, string chord, string ratio)
    {
        static Control Row(string left, string right, bool muted, bool bold)
        {
            var start = new TextBlock { Text = left, FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal };
            var end = new TextBlock { Text = right, HorizontalAlignment = HorizontalAlignment.Right };
            start.Classes.Set("muted", muted);
            end.Classes.Set("muted", muted);
            Grid.SetColumn(end, 1);
            var border = new Border { Child = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { start, end } } };
            border.Classes.Add("station-thumb-label");
            return border;
        }
        var outline = new SectionThumb { Profile = profile, IsHitTestVisible = false };
        var rows = new StackPanel { Children = { Row(name, distance, false, true), Row(chord, ratio, true, false) } };
        Grid.SetRow(rows, 1);
        return new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { outline, rows } };
    }

    private async Task SwitchStationAsync(int index)
    {
        if (controller is null) return;
        try { await controller.EnterSectionAsync(index, EntryOrigin.Side); }
        catch (ContractError error) when (error.Code == "DSL-DRAFT-OWNED" && controller.Section is { } open && controller.Inspection is { } inspection)
        {
            // §6.1: "Finish or cancel <station> before editing <other>."
            var assignments = inspection.Authored.Assignments;
            ModeReason.Text = $"Finish or cancel {ElevationView.StationName(open.Draft.Assignment, assignments[open.Draft.Assignment].Eta)} " +
                $"before editing {ElevationView.StationName(index, assignments[index].Eta)}.";
            ModeReasonBox.IsVisible = true;
        }
        catch (ContractError error)
        {
            ModeReason.Text = error.Message;
            ModeReasonBox.IsVisible = true;
        }
    }
}

/// <summary>A station strip thumbnail's section outline (mockup thumbDraw): both curves at ×2 thickness, inset 8 px.</summary>
public sealed class SectionThumb : Control
{
    public ProfileView? Profile { get; set; }

    public override void Render(DrawingContext context)
    {
        if (Profile is null || Bounds.Width <= 16 || Bounds.Height <= 0) return;
        var foil = this.TryFindResource("FoilBrush", ActualThemeVariant, out var value) ? value as IBrush : null;
        if (foil is null) return;
        double scale = Bounds.Width - 16, middle = Bounds.Height / 2;
        var pen = new Pen(foil, 1.5);
        foreach (var curve in new[] { Profile.UpperCurve, Profile.LowerCurve })
            for (int index = 1; index < curve.Count; index++)
                context.DrawLine(pen, new Point(8 + curve[index - 1].X * scale, middle - curve[index - 1].Y * scale * 2),
                    new Point(8 + curve[index].X * scale, middle - curve[index].Y * scale * 2));
    }
}
