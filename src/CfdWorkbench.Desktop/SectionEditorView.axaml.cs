using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Automation;
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

    public SectionEditorView()
    {
        InitializeComponent();
        ModeCancelButton.Click += (_, _) => controller?.CancelSection();
        ModeFinishButton.Click += (_, _) => _ = FinishAsync();
        CurvatureToggle.Click += (_, _) => { ModeCanvas.CurvatureVisible = CurvatureToggle.IsChecked == true; ModeCanvas.InvalidateVisual(); };
        ThicknessToggle.Click += (_, _) =>
        {
            ModeCanvas.ThicknessDoubled = ThicknessToggle.IsChecked == true;
            ModePlate.Text = PlateText();
            ModeCanvas.InvalidateVisual();
        };
        ModeFitButton.Click += (_, _) => ModeCanvas.Fit();
        ModeFitSelectionButton.Click += (_, _) => ModeCanvas.FitSelection();
        ModeCanvas.CancelTarget = ModeCancelButton;
        ModeCanvas.ReasonTarget = ModeReason;
        ModeCanvas.ReasonContainer = ModeReasonBox;
        ModeCanvas.ProbeTarget = ModeProbe;
        // DR-NAV-1 and §11.3: Tab goes to the point's Type and Return to its x, both in the shell's Properties pane.
        ModeCanvas.TabOut = () => this.FindAncestorOfType<ShellHost>()?.Properties.FocusFirstValue() == true;
        ModeCanvas.ValueOut = () => this.FindAncestorOfType<ShellHost>()?.Properties
            .FindControl<TextBox>("PointSpanInput") is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } x && x.Focus();
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
                var assignmentRow = controller.Inspection.Authored.Assignments[assignment.Value];
                ModeTitle.Text = $"Editing {ElevationView.StationName(assignment.Value, assignmentRow.Eta)} section";
                ScopeChip.Text = mode.Draft.Scope == SectionScope.Shared ? "shared profile" : "this station only";
                ModePlate.Text = PlateText();
                ModeFinishButton.IsEnabled = mode.CanFinish;
                string? reason = mode.FinishReason;
                AutomationProperties.SetHelpText(ModeFinishButton, reason);
                // A state reason (a refused refit, or why Finish is off) always shows. A gesture's refusal (⌫ on a named
                // point, a refused step, a refused strip switch) has no state behind it: it stays until the draft changes.
                string? stateReason = controller.SectionRefitRefusal is not null ? controller.Status
                    : mode.IsDirty && !mode.CanFinish ? reason : null;
                if (stateReason is not null)
                {
                    ModeReason.Text = stateReason;
                    ModeReasonBox.IsVisible = true;
                }
                else if (reasonDraft != $"{mode.Draft.DraftId}:{mode.Draft.Generation}")
                {
                    ModeReason.Text = null;
                    ModeReasonBox.IsVisible = false;
                }
                reasonDraft = $"{mode.Draft.DraftId}:{mode.Draft.Generation}";
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
                ? $"{measured.Side.ToString().ToLowerInvariant()} would move {measured.DeviationMeters * 1000:F3} mm here (limit {measured.LimitMeters * 1000:F3} mm)"
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

    private void RefreshStationStrip(WorkbenchController controller, int active)
    {
        StationStrip.Children.Clear();
        foreach (var (assignment, index) in controller.Inspection!.Authored.Assignments.Select((item, index) => (item, index)))
        {
            var facts = Sections.Facts(controller.Section!.Draft.Bytes, index);
            string name = ElevationView.StationName(index, assignment.Eta);
            var button = new Button
            {
                Content = $"{name} · {assignment.SpanMeters * 1000:F2} mm from root\n{facts.StationChordMeters * 1000:F2} mm chord · {facts.StationThicknessRatio * 100:F2} % t/c",
                MinWidth = 170
            };
            AutomationProperties.SetName(button, $"{name} section thumbnail");
            if (index == active) AutomationProperties.SetItemStatus(button, "current");
            int target = index;
            button.Click += (_, _) => _ = SwitchStationAsync(target);
            StationStrip.Children.Add(button);
        }
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
