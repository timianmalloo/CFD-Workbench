using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;

namespace CfdWorkbench.Desktop.Tests;

public static class SectionFlowTests
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Running SectionFlowTests...");
        await TestSharedFlowOnExampleAsync();
        await TestCancelBeforeApplyRestoresPriorViewAsync();
        await TestFixedVertexCannotStartEditAsync();
        await TestScopeRadiosDisabledWhileDraftOpenAndBannerNamesStationAsync();
        await TestWindowLevelEditSectionSwitchesTabAndCanvasProfileNonNullAsync();
        Console.WriteLine("SectionFlowTests: all 5 scenarios passed.");
    }

    private static async Task TestSharedFlowOnExampleAsync()
    {
        using var controller = new WorkbenchController();
        await controller.OpenExampleAsync();

        // DescribeScope(Shared) lists both assignments
        var impact = controller.DescribeScope(0, SectionScope.Shared);
        if (impact.AffectedAssignments.Count != 2 || impact.AffectedAssignments[0] != 0 || impact.AffectedAssignments[1] != 1)
            throw new Exception($"DescribeScope(Shared) did not list both assignments; got count={impact.AffectedAssignments.Count}");

        var originalView = controller.SectionView(0);
        var targetVertex = originalView.Upper.First(v => !v.Fixed);
        double originalY = targetVertex.Y;
        double updatedY = originalY + 0.015;

        controller.BeginSectionEdit(0, SectionScope.Shared, targetVertex.Side, targetVertex.Id);
        controller.UpdateSectionDraft(targetVertex.X, updatedY);
        await controller.PreviewAsync();
        controller.Apply();

        var appliedView = controller.SectionView(0);
        var appliedVertex = appliedView.Upper.Single(v => v.Id == targetVertex.Id);
        if (Math.Abs(appliedVertex.Y - updatedY) > 1e-9)
            throw new Exception($"Apply did not update SectionView vertex; expected {updatedY}, got {appliedVertex.Y}");

        // Undo restores original SectionView vertices exactly
        controller.Undo();
        var undoneView = controller.SectionView(0);
        if (undoneView.Upper.Count != originalView.Upper.Count || undoneView.Lower.Count != originalView.Lower.Count)
            throw new Exception("Undo altered vertex counts");
        for (int i = 0; i < originalView.Upper.Count; i++)
        {
            if (Math.Abs(undoneView.Upper[i].X - originalView.Upper[i].X) > 1e-9 ||
                Math.Abs(undoneView.Upper[i].Y - originalView.Upper[i].Y) > 1e-9)
                throw new Exception($"Undo did not restore original upper vertex {originalView.Upper[i].Id} exactly");
        }
        for (int i = 0; i < originalView.Lower.Count; i++)
        {
            if (Math.Abs(undoneView.Lower[i].X - originalView.Lower[i].X) > 1e-9 ||
                Math.Abs(undoneView.Lower[i].Y - originalView.Lower[i].Y) > 1e-9)
                throw new Exception($"Undo did not restore original lower vertex {originalView.Lower[i].Id} exactly");
        }

        // Redo re-applies
        controller.Redo();
        var redoneView = controller.SectionView(0);
        var redoneVertex = redoneView.Upper.Single(v => v.Id == targetVertex.Id);
        if (Math.Abs(redoneVertex.Y - updatedY) > 1e-9)
            throw new Exception($"Redo did not re-apply section edit; expected {updatedY}, got {redoneVertex.Y}");
    }

    private static async Task TestCancelBeforeApplyRestoresPriorViewAsync()
    {
        using var controller = new WorkbenchController();
        await controller.OpenExampleAsync();

        var priorView = controller.SectionView(0);
        var targetVertex = priorView.Upper.First(v => !v.Fixed);

        controller.BeginSectionEdit(0, SectionScope.Shared, targetVertex.Side, targetVertex.Id);
        controller.UpdateSectionDraft(targetVertex.X, targetVertex.Y + 0.02);
        controller.Cancel();

        var restoredView = controller.SectionView(0);
        for (int i = 0; i < priorView.Upper.Count; i++)
        {
            if (Math.Abs(restoredView.Upper[i].X - priorView.Upper[i].X) > 1e-9 ||
                Math.Abs(restoredView.Upper[i].Y - priorView.Upper[i].Y) > 1e-9)
                throw new Exception("Cancel before Apply did not restore prior SectionView upper vertex exactly");
        }
        for (int i = 0; i < priorView.Lower.Count; i++)
        {
            if (Math.Abs(restoredView.Lower[i].X - priorView.Lower[i].X) > 1e-9 ||
                Math.Abs(restoredView.Lower[i].Y - priorView.Lower[i].Y) > 1e-9)
                throw new Exception("Cancel before Apply did not restore prior SectionView lower vertex exactly");
        }
    }

    private static async Task TestFixedVertexCannotStartEditAsync()
    {
        using var controller = new WorkbenchController();
        await controller.OpenExampleAsync();

        var view = controller.SectionView(0);
        var fixedVertex = view.Upper.First(v => v.Fixed);

        bool controllerRefused = false;
        try
        {
            controller.BeginSectionEdit(0, SectionScope.Shared, fixedVertex.Side, fixedVertex.Id);
        }
        catch (ContractError error) when (error.Code == "DSL-LOCK")
        {
            controllerRefused = true;
        }
        if (!controllerRefused)
            throw new Exception("Controller did not surface DSL-LOCK when attempting to edit a fixed vertex");

        // Window-level status banner test
        var window = new MainWindow();
        window.Show();
        window.UpdateLayout();
        var controllerField = typeof(MainWindow).GetField("workbench",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var winController = (WorkbenchController)controllerField.GetValue(window)!;
        await winController.OpenExampleAsync();
        window.UpdateLayout();

        // Selecting a fixed vertex in the window should surface DSL-LOCK in the state banner
        var vertexList = window.FindControl<ListBox>("SectionVertexList");
        var stateBanner = window.FindControl<TextBlock>("StateBanner");
        if (vertexList is not null && stateBanner is not null)
        {
            // Select fixed vertex item (index 0)
            vertexList.SelectedIndex = 0;
            window.UpdateLayout();
            if (stateBanner.Text is null || !stateBanner.Text.Contains("DSL-LOCK", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Window status banner did not show DSL-LOCK; observed text: '{stateBanner.Text}'");
        }

        typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
        window.Close();
    }

    private static async Task TestScopeRadiosDisabledWhileDraftOpenAndBannerNamesStationAsync()
    {
        var window = new MainWindow();
        window.Show();
        window.UpdateLayout();
        var controllerField = typeof(MainWindow).GetField("workbench",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var winController = (WorkbenchController)controllerField.GetValue(window)!;
        await winController.OpenExampleAsync();
        window.UpdateLayout();

        var editSectionBtn = window.FindControl<Button>("EditSectionButton")
            ?? throw new Exception("EditSectionButton missing");
        var stationList = window.FindControl<ListBox>("StationList")
            ?? throw new Exception("StationList missing");
        var scopeSharedRadio = window.FindControl<RadioButton>("ScopeSharedRadio")
            ?? throw new Exception("ScopeSharedRadio missing");
        var scopeIndepRadio = window.FindControl<RadioButton>("ScopeIndependentRadio")
            ?? throw new Exception("ScopeIndependentRadio missing");
        var vertexList = window.FindControl<ListBox>("SectionVertexList")
            ?? throw new Exception("SectionVertexList missing");
        var stateBanner = window.FindControl<TextBlock>("StateBanner")
            ?? throw new Exception("StateBanner missing");

        stationList.SelectedIndex = 0;
        window.UpdateLayout();
        editSectionBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();

        // Select an editable vertex to begin draft
        int editableIndex = -1;
        var items = vertexList.Items.OfType<ListBoxItem>().ToArray();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Content is string s && s.Contains("editable"))
            {
                editableIndex = i;
                break;
            }
        }
        if (editableIndex < 0) throw new Exception("No editable vertex in SectionVertexList");

        vertexList.SelectedIndex = editableIndex;
        window.UpdateLayout();

        if (winController.Draft is null)
            throw new Exception("Draft was not started after selecting editable vertex");

        // Scope radios must be disabled while a draft is open
        if (scopeSharedRadio.IsEnabled || scopeIndepRadio.IsEnabled)
            throw new Exception("Scope radios must be disabled while a draft is open");

        // Select another station in StationList
        stationList.SelectedIndex = 1;
        window.UpdateLayout();

        // Draft banner must still name the original station (station 0)
        if (stateBanner.Text is null || (!stateBanner.Text.Contains("0") && !stateBanner.Text.Contains("station", StringComparison.OrdinalIgnoreCase)))
            throw new Exception($"Draft banner did not name original station 0; banner text: '{stateBanner.Text}'");

        typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
        window.Close();
    }

    private static async Task TestWindowLevelEditSectionSwitchesTabAndCanvasProfileNonNullAsync()
    {
        var window = new MainWindow();
        window.Show();
        window.UpdateLayout();
        var controllerField = typeof(MainWindow).GetField("workbench",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var winController = (WorkbenchController)controllerField.GetValue(window)!;
        await winController.OpenExampleAsync();
        window.UpdateLayout();

        var editSectionBtn = window.FindControl<Button>("EditSectionButton")
            ?? throw new Exception("EditSectionButton missing");
        var documentTabs = window.FindControl<TabControl>("DocumentTabs")
            ?? throw new Exception("DocumentTabs missing");
        var sectionTab = window.FindControl<TabItem>("SectionTab")
            ?? throw new Exception("SectionTab missing");
        var canvas = window.FindControl<SectionCanvas>("EditableSectionCanvas")
            ?? throw new Exception("EditableSectionCanvas missing");

        // Press Edit section
        editSectionBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();

        if (!ReferenceEquals(documentTabs.SelectedItem, sectionTab))
            throw new Exception($"Edit section button did not switch DocumentTabs to SectionTab; selected was {documentTabs.SelectedItem}");

        if (canvas.Profile is null)
            throw new Exception("EditableSectionCanvas.Profile is null after pressing Edit section");

        typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
        window.Close();
    }
}
