using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        TestDraftStatusNamesStation();
        Console.WriteLine("SectionFlowTests: all 4 scenarios passed.");
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
    }

    // Ported from the pre-shell window's banner check: the draft's own status names the station it owns.
    private static void TestDraftStatusNamesStation()
    {
        Console.WriteLine("section-flow status");
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        var vertex = controller.SectionView(0).Upper.First(v => !v.Fixed);
        controller.BeginSectionEdit(0, SectionScope.Shared, vertex.Side, vertex.Id);
        var draft = controller.Draft ?? throw new Exception("Draft was not started for an editable vertex");
        if (!controller.Status.Contains("station 0", StringComparison.Ordinal) || !controller.Status.Contains(draft.Id, StringComparison.Ordinal))
            throw new Exception($"Draft status did not name station 0 and the draft; status: '{controller.Status}'");
    }
}
