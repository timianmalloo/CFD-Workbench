using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;

namespace CfdWorkbench.Desktop.Tests;

public static class SectionToolsTests
{
    public static void Run()
    {
        Console.WriteLine("Running SectionToolsTests...");
        TestInsertReportsCountAndDeviationThenUndoRestores();
        TestFairAtDefaultToleranceEnablesApply();
        TestFairAtNegativeToleranceDisablesApply();
        TestImportDatReportsResidualAndProvenance();
        TestUseSourceThicknessReportsTargets();
        TestToolButtonsDisabledWhileDraftOpen();
        Console.WriteLine("SectionToolsTests: all 6 scenarios passed.");
    }

    private static void Wait(Task task)
    {
        var dispatcher = Dispatcher.UIThread;
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Section tools wait exceeded 120s");
            dispatcher.RunJobs();
            if (!task.IsCompleted) Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
    }

    private static void TestInsertReportsCountAndDeviationThenUndoRestores()
    {
        Console.WriteLine("section-tools insert");
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        var original = controller.SectionView(0);
        controller.BeginSectionInsert(0, SectionScope.Shared, 0.37);
        var report = RequireReport(controller, "construction");
        if (!report.Certified) throw new Exception("Insert report is not certified");
        int count = int.Parse(Line(report, "Vertex count"), CultureInfo.InvariantCulture);
        if (count != original.Upper.Count + 1)
            throw new Exception($"Insert vertex count {count} is not {original.Upper.Count + 1}");
        double deviation = Parse(Line(report, "Max deviation"));
        Console.WriteLine($"section-tools insert deviation={deviation.ToString("G6", CultureInfo.InvariantCulture)} count={count}");
        if (deviation > 1e-12) throw new Exception($"Insert deviation {deviation} exceeds 1e-12");
        controller.Apply();
        var applied = controller.SectionView(0);
        if (applied.Upper.Count != original.Upper.Count + 1)
            throw new Exception("Apply did not keep the inserted vertex");
        controller.Undo();
        var restored = controller.SectionView(0);
        SameShape(original, restored, "Undo did not restore the original SectionView");
    }

    private static void TestFairAtDefaultToleranceEnablesApply()
    {
        Console.WriteLine("section-tools fair");
        var window = OpenExampleWindow();
        ShowSection(window);
        var tolerance = Require<TextBox>(window, "SectionFairToleranceInput");
        var fair = Require<Button>(window, "SectionFairButton");
        var apply = Require<Button>(window, "SectionApplyButton");
        if (tolerance.Text != "1e-4") throw new Exception($"Fair tolerance default is '{tolerance.Text}', expected 1e-4");
        if (Require<ComboBox>(window, "SectionPreserveEnds").SelectedIndex != 0)
            throw new Exception("Preserve ends default is not Position");
        fair.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var report = RequireReport(Controller(window), "construction");
        string shown = ShownReport(Require<ItemsControl>(window, "SectionReportList"));
        if (!report.Certified) throw new Exception($"Fair at 1e-4 is not certified:\n{shown}");
        if (Line(report, "Tolerance") != "1e-4" || !shown.Contains("1e-4", StringComparison.Ordinal))
            throw new Exception($"Report did not show tolerance 1e-4:\n{shown}");
        double deviation = Parse(Line(report, "Max deviation"));
        Console.WriteLine($"section-tools fair deviation={deviation.ToString("G6", CultureInfo.InvariantCulture)}");
        if (deviation > 1e-4) throw new Exception($"Fair deviation {deviation} exceeds 1e-4");
        if (!shown.Contains(Line(report, "Max deviation"), StringComparison.Ordinal))
            throw new Exception($"Report panel omitted the deviation:\n{shown}");
        if (!apply.IsEnabled) throw new Exception("Apply is disabled after a certified fair");
        Close(window);
    }

    private static void TestFairAtNegativeToleranceDisablesApply()
    {
        Console.WriteLine("section-tools fair-refused");
        var window = OpenExampleWindow();
        ShowSection(window);
        Require<TextBox>(window, "SectionFairToleranceInput").Text = "-1";
        Require<Button>(window, "SectionFairButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var report = RequireReport(Controller(window), "construction");
        string shown = ShownReport(Require<ItemsControl>(window, "SectionReportList"));
        if (report.Certified) throw new Exception("Negative tolerance was certified");
        if (!shown.Contains("Fair result exceeds tolerance", StringComparison.Ordinal))
            throw new Exception($"Report did not state the refusal:\n{shown}");
        if (Require<Button>(window, "SectionApplyButton").IsEnabled)
            throw new Exception("Apply stayed enabled after the fair refusal");
        Close(window);
    }

    private static void TestImportDatReportsResidualAndProvenance()
    {
        Console.WriteLine("section-tools import");
        byte[] dat = Naca0012Selig();
        // The embedded example keeps a neighbour on section-a, and a fitted NACA basis is refused
        // (DSL-GEOMETRY, abscissae differ) until independent abscissae are certified. The host below
        // is the same shape the core import cycle applies: both stations already share that basis.
        var hostFit = DatImport.Fit(DatImport.Parse(dat), "base-section");
        if (!hostFit.Accepted) throw new Exception("NACA host fit was not accepted");
        byte[] host = FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(WrapFoil(hostFit.ProfileBlock, "base-section"))));
        var window = OpenExampleWindow();
        ShowSection(window);
        var controller = Controller(window);
        Wait(controller.OpenFoilAsync(host, "Import host"));
        controller.BeginSectionImport(0, dat);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var report = RequireReport(controller, "import");
        string shown = ShownReport(Require<ItemsControl>(window, "SectionReportList"));
        double residual = Parse(Line(report, "Max residual"));
        string provenance = Line(report, "Provenance");
        Console.WriteLine($"section-tools import residual={residual.ToString("G6", CultureInfo.InvariantCulture)} vertices={Line(report, "Vertex count")}");
        if (residual > 1e-5) throw new Exception($"Import residual {residual} exceeds 1e-5");
        if (provenance.Length == 0 || !shown.Contains(provenance, StringComparison.Ordinal))
            throw new Exception($"Report did not show provenance:\n{shown}");
        var apply = Require<Button>(window, "SectionApplyButton");
        if (!report.Certified || controller.Provenance != "preview" || !apply.IsEnabled)
            throw new Exception($"Apply not ready. certified={report.Certified} provenance={controller.Provenance} apply={apply.IsEnabled} status={controller.Status}");
        Close(window);
    }

    private static void TestUseSourceThicknessReportsTargets()
    {
        Console.WriteLine("section-tools thickness");
        var window = OpenExampleWindow();
        ShowSection(window);
        var source = Require<RadioButton>(window, "ThicknessSourceRadio");
        if (!source.IsEnabled) throw new Exception("Use source thickness is disabled");
        source.IsChecked = true;
        SelectEditable(window, "cv-3");
        var controller = Controller(window);
        var draft = controller.Draft ?? throw new Exception("Shared edit did not open a draft");
        if (draft.Intent != ThicknessIntent.UseSource)
            throw new Exception($"BeginProfileEdit received {draft.Intent}");
        var view = controller.SectionView(0);
        var vertex = view.Upper.Concat(view.Lower).Single(item => item.Side == draft.Rail && item.Id == draft.VertexId);
        controller.UpdateSectionDraft(vertex.X, vertex.Y + 0.02);
        Wait(controller.PreviewAsync());
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var report = RequireReport(controller, "thickness");
        string shown = ShownReport(Require<ItemsControl>(window, "SectionReportList"));
        var etas = Split(Line(report, "Target η"));
        var targets = Split(Line(report, "Target t/c"));
        var residuals = Split(Line(report, "Residuals"));
        if (etas.Length == 0 || etas.Length != targets.Length || etas.Length != residuals.Length)
            throw new Exception($"Thickness rows do not line up: η={etas.Length} t/c={targets.Length} residuals={residuals.Length}");
        if (string.IsNullOrWhiteSpace(Line(report, "Affected η")))
            throw new Exception("Affected η span is missing");
        if (!shown.Contains("Target η", StringComparison.Ordinal) || !shown.Contains("Residuals", StringComparison.Ordinal))
            throw new Exception($"Report panel omitted the thickness proposal:\n{shown}");
        Console.WriteLine($"section-tools thickness targets={targets.Length} span={Line(report, "Affected η")}");
        Close(window);
    }

    private static void TestToolButtonsDisabledWhileDraftOpen()
    {
        Console.WriteLine("section-tools draft-lock");
        var window = OpenExampleWindow();
        ShowSection(window);
        var buttons = new[] { "SectionInsertButton", "SectionDeleteButton", "SectionFairButton", "SectionRebuildButton", "SectionImportButton" }
            .Select(name => Require<Button>(window, name)).ToArray();
        if (buttons.Any(button => !button.IsEnabled))
            throw new Exception("A section tool is disabled before a draft is open");
        SelectEditable(window, null);
        var controller = Controller(window);
        var draft = controller.Draft ?? throw new Exception("Selecting a vertex did not open a draft");
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        if (buttons.Any(button => button.IsEnabled))
            throw new Exception("A section tool stayed enabled while a draft is open");
        var banner = Require<TextBlock>(window, "StateBanner").Text ?? "";
        if (!banner.Contains(draft.Id, StringComparison.Ordinal) || !banner.Contains("station 0", StringComparison.Ordinal))
            throw new Exception($"Status banner did not name the draft: '{banner}'");
        Close(window);
    }

    private static void SelectEditable(MainWindow window, string? id)
    {
        var list = Require<ListBox>(window, "SectionVertexList");
        var items = list.Items.OfType<ListBoxItem>().ToArray();
        int index = -1;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Content is not string text || !text.Contains("editable", StringComparison.Ordinal)) continue;
            if (id is null || text.Contains(id, StringComparison.Ordinal)) { index = i; break; }
        }
        if (index < 0) throw new Exception(id is null ? "No editable vertex" : $"Editable vertex {id} is missing");
        list.SelectedIndex = index;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static SectionReport RequireReport(WorkbenchController controller, string kind)
    {
        var report = controller.SectionReport ?? throw new Exception($"SectionReport is null; expected {kind}. Status: {controller.Status}");
        if (!report.Kind.Contains(kind, StringComparison.Ordinal))
            throw new Exception($"SectionReport kind '{report.Kind}' does not include {kind}");
        return report;
    }

    private static string Line(SectionReport report, string label)
    {
        var line = report.Lines.FirstOrDefault(item => item.Label == label);
        if (line is null)
            throw new Exception($"Report missing {label}. Lines: {string.Join("; ", report.Lines.Select(item => item.Label + "=" + item.Value))}");
        return line.Value;
    }

    private static string ShownReport(ItemsControl list)
    {
        if (!list.IsVisible) throw new Exception("Section report rows are hidden");
        var texts = new System.Collections.Generic.List<string>();
        foreach (var item in list.Items)
        {
            if (item is not Panel panel) continue;
            foreach (var child in panel.Children.OfType<TextBlock>())
                if (!string.IsNullOrEmpty(child.Text)) texts.Add(child.Text);
        }
        if (texts.Count == 0) throw new Exception($"Section report list has {list.ItemCount} items and no text");
        return string.Join("\n", texts);
    }

    private static double Parse(string text) =>
        double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static double[] Split(string text) =>
        text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray();

    private static void SameShape(ProfileView expected, ProfileView actual, string message)
    {
        if (expected.Upper.Count != actual.Upper.Count || expected.Lower.Count != actual.Lower.Count)
            throw new Exception(message);
        for (int i = 0; i < expected.Upper.Count; i++)
            if (Math.Abs(actual.Upper[i].X - expected.Upper[i].X) > 1e-9 || Math.Abs(actual.Upper[i].Y - expected.Upper[i].Y) > 1e-9)
                throw new Exception(message);
        for (int i = 0; i < expected.Lower.Count; i++)
            if (Math.Abs(actual.Lower[i].X - expected.Lower[i].X) > 1e-9 || Math.Abs(actual.Lower[i].Y - expected.Lower[i].Y) > 1e-9)
                throw new Exception(message);
    }

    private static T Require<T>(MainWindow window, string name) where T : Control =>
        window.FindControl<T>(name) ?? throw new Exception($"{name} missing");

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

    private static void ShowSection(MainWindow window)
    {
        var tabs = Require<TabControl>(window, "DocumentTabs");
        tabs.SelectedItem = Require<TabItem>(window, "SectionTab");
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
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

    private static string WrapFoil(string profileBlock, string profileName) =>
        "foildsl \"4.0\"\n" +
        "foil \"Import test\" {\n" +
        "  units mm\n" +
        "  half_span 450 mm\n" +
        "  evaluator \"cfdw-cv\" \"2\"\n" +
        "  symmetry mirror_y\n" +
        "  planform {\n" +
        "    leading cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }\n" +
        "    trailing cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 120), (0.1, 120), (0.3, 120), (0.5, 120), (0.7, 120), (0.9, 120), (1, 120)] }\n" +
        "  }\n" +
        "  dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }\n" +
        "  twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, -0.25), (0.5, -0.5), (0.7, -1), (0.9, -1.5), (1, -2)] }\n" +
        "  thickness cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0.12), (0.1, 0.12), (0.3, 0.12), (0.5, 0.12), (0.7, 0.12), (0.9, 0.12), (1, 0.12)] }\n" +
        "  profiles {\n" +
        profileBlock + "\n" +
        "  }\n" +
        $"  sections {{ at root profile \"{profileName}\" at tip profile \"{profileName}\" }}\n" +
        "}\n";

    private static byte[] Naca0012Selig()
    {
        const int n = 40;
        var x = new double[n + 1];
        var yt = new double[n + 1];
        for (int i = 0; i <= n; i++)
        {
            x[i] = 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
            yt[i] = 5.0 * 0.12 * (0.2969 * Math.Sqrt(x[i]) - 0.1260 * x[i] - 0.3516 * x[i] * x[i] + 0.2843 * Math.Pow(x[i], 3) - 0.1036 * Math.Pow(x[i], 4));
        }
        var builder = new StringBuilder();
        builder.AppendLine("NACA 0012");
        for (int i = n; i >= 0; i--)
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], yt[i]));
        for (int i = 1; i <= n; i++)
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], -yt[i]));
        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
