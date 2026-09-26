using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;

namespace CfdWorkbench.Desktop.Tests;

public static class SectionFlowTests
{
    public static void Run()
    {
        Console.WriteLine("Running SectionFlowTests...");
        TestSharedFlowOnExample();
        TestCancelBeforeApplyRestoresPriorView();
        TestFixedVertexCannotStartEdit();
        TestScopeRadiosDisabledWhileDraftOpenAndBannerNamesStation();
        TestWindowLevelEditSectionSwitchesTabAndCanvasProfileNonNull();
        Console.WriteLine("SectionFlowTests: all 5 scenarios passed.");
    }

    private static void Wait(Task task)
    {
        var dispatcher = Dispatcher.UIThread;
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Section flow wait exceeded 120s");
            dispatcher.RunJobs();
            if (!task.IsCompleted) Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
    }

    private static void TestSharedFlowOnExample()
    {
        Console.WriteLine("section-flow shared");
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());

        var impact = controller.DescribeScope(0, SectionScope.Shared);
        if (impact.AffectedAssignments.Count != 2 || impact.AffectedAssignments[0] != 0 || impact.AffectedAssignments[1] != 1)
            throw new Exception($"DescribeScope(Shared) did not list both assignments; got count={impact.AffectedAssignments.Count}");

        var originalView = controller.SectionView(0);
        var targetVertex = originalView.Upper.First(v => !v.Fixed);
        double originalY = targetVertex.Y;
        double updatedY = originalY + 0.015;

        controller.BeginSectionEdit(0, SectionScope.Shared, targetVertex.Side, targetVertex.Id);
        controller.UpdateSectionDraft(targetVertex.X, updatedY);
        Wait(controller.PreviewAsync());
        controller.Apply();

        var appliedView = controller.SectionView(0);
        var appliedVertex = appliedView.Upper.Single(v => v.Id == targetVertex.Id);
        if (Math.Abs(appliedVertex.Y - updatedY) > 1e-9)
            throw new Exception($"Apply did not update SectionView vertex; expected {updatedY}, got {appliedVertex.Y}");

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

        controller.Redo();
        var redoneView = controller.SectionView(0);
        var redoneVertex = redoneView.Upper.Single(v => v.Id == targetVertex.Id);
        if (Math.Abs(redoneVertex.Y - updatedY) > 1e-9)
            throw new Exception($"Redo did not re-apply section edit; expected {updatedY}, got {redoneVertex.Y}");
    }

    private static void TestCancelBeforeApplyRestoresPriorView()
    {
        Console.WriteLine("section-flow cancel");
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());

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

    private static void TestFixedVertexCannotStartEdit()
    {
        Console.WriteLine("section-flow fixed");
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());

        var view = controller.SectionView(0);
        var fixedVertex = view.Upper.First(v => v.Fixed);

        bool controllerRefused = false;
        try { controller.BeginSectionEdit(0, SectionScope.Shared, fixedVertex.Side, fixedVertex.Id); }
        catch (ContractError error) when (error.Code == "DSL-LOCK") { controllerRefused = true; }
        if (!controllerRefused)
            throw new Exception("Controller did not surface DSL-LOCK when attempting to edit a fixed vertex");

        var window = OpenExampleWindow();
        var vertexList = window.FindControl<ListBox>("SectionVertexList")
            ?? throw new Exception("SectionVertexList missing");
        var stateBanner = window.FindControl<TextBlock>("StateBanner")
            ?? throw new Exception("StateBanner missing");
        if (vertexList.ItemCount < 1) throw new Exception("SectionVertexList has no vertices");
        vertexList.SelectedIndex = 0;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        if (stateBanner.Text is null || !stateBanner.Text.Contains("DSL-LOCK", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Window status banner did not show DSL-LOCK; observed text: '{stateBanner.Text}'");
        Close(window);
    }

    private static void TestScopeRadiosDisabledWhileDraftOpenAndBannerNamesStation()
    {
        Console.WriteLine("section-flow scope-banner");
        var window = OpenExampleWindow();
        var winController = Controller(window);
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

        int editableIndex = -1;
        var items = vertexList.Items.OfType<ListBoxItem>().ToArray();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Content is string s && s.Contains("editable", StringComparison.Ordinal))
            {
                editableIndex = i;
                break;
            }
        }
        if (editableIndex < 0) throw new Exception("No editable vertex in SectionVertexList");

        vertexList.SelectedIndex = editableIndex;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        if (winController.Draft is null)
            throw new Exception("Draft was not started after selecting editable vertex");
        if (scopeSharedRadio.IsEnabled || scopeIndepRadio.IsEnabled)
            throw new Exception("Scope radios must be disabled while a draft is open");

        stationList.SelectedIndex = 1;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        if (stateBanner.Text is null || !stateBanner.Text.Contains("station 0", StringComparison.Ordinal))
            throw new Exception($"Draft banner did not name original station 0; banner text: '{stateBanner.Text}'");
        Close(window);
    }

    private static void TestWindowLevelEditSectionSwitchesTabAndCanvasProfileNonNull()
    {
        Console.WriteLine("section-flow edit-tab");
        var window = OpenExampleWindow();
        var editSectionBtn = window.FindControl<Button>("EditSectionButton")
            ?? throw new Exception("EditSectionButton missing");
        var documentTabs = window.FindControl<TabControl>("DocumentTabs")
            ?? throw new Exception("DocumentTabs missing");
        var sectionTab = window.FindControl<TabItem>("SectionTab")
            ?? throw new Exception("SectionTab missing");
        var canvas = window.FindControl<SectionCanvas>("EditableSectionCanvas")
            ?? throw new Exception("EditableSectionCanvas missing");

        editSectionBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        if (!ReferenceEquals(documentTabs.SelectedItem, sectionTab))
            throw new Exception($"Edit section button did not switch DocumentTabs to SectionTab; selected was {documentTabs.SelectedItem}");
        if (canvas.Profile is null)
            throw new Exception("EditableSectionCanvas.Profile is null after pressing Edit section");
        Close(window);
    }

    private static MainWindow OpenExampleWindow()
    {
        var window = new MainWindow();
        window.Show();
        window.UpdateLayout();
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (Controller(window).Inspection is null ||
               window.FindControl<ListBox>("SectionVertexList") is not { ItemCount: > 0 })
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Example window did not publish a section");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        return window;
    }

    private static WorkbenchController Controller(MainWindow window) =>
        (WorkbenchController)typeof(MainWindow).GetField("workbench", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(window)!;

    private static void Close(MainWindow window)
    {
        typeof(MainWindow).GetField("closeApproved", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
        window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
