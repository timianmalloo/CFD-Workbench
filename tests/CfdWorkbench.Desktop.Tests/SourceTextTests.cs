using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// M1.2d F3: every surface that names a section's source reads the same string for the same bytes, and after an edit
/// that string is "Modified from NACA 0012" (DR-INV-1 a, COPY-112).
/// </summary>
public static class SourceTextTests
{
    private const string Modified = "Modified from NACA 0012";

    public static void Run()
    {
        DesktopChecks.Check("BrowserRow_AfterEdit_NotTheOriginal", () =>
        {
            using var controller = Edited();
            string profile = controller.Section!.Draft.Profile;
            string text = BrowserPane.StationSourceText(profile, controller.Section.Draft.Bytes);
            Require(text == profile + " · " + Modified, "Browser row after an edit: " + text);
        });
        DesktopChecks.Check("Properties_SectionTitle_AfterEdit_NotTheOriginal", () =>
        {
            using var controller = Edited();
            string title = SectionTitle(controller);
            string profile = controller.Section!.Draft.Profile;
            Require(title.StartsWith(profile + " · " + Modified + " · degree ", StringComparison.Ordinal),
                "Section title after an edit: " + title);
        });
        DesktopChecks.Check("SourceText_AllSurfacesAgree", () =>
        {
            using var controller = Edited();
            string profile = controller.Section!.Draft.Profile;
            string chip = controller.SourceChip ?? "<null>";
            string card = controller.StationSource(0);
            const string rights = " (GEN)";
            Require(card.EndsWith(rights, StringComparison.Ordinal), "Station card lacks rights: " + card);
            card = card[..^rights.Length];
            var dialog = new SaveSectionDialog(controller, _ => Task.CompletedTask);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();
            string save = dialog.FindControl<TextBlock>("ProvenanceLine")?.Text ?? "<null>";
            dialog.Close();
            const string prefix = "Provenance: ";
            Require(save.StartsWith(prefix, StringComparison.Ordinal), "Save line lacks its prefix: " + save);
            save = save[prefix.Length..];
            string row = BrowserPane.StationSourceText(profile, controller.Section.Draft.Bytes);
            Require(row.StartsWith(profile + " · ", StringComparison.Ordinal), "Browser row: " + row);
            row = row[(profile.Length + 3)..];
            string title = SectionTitle(controller);
            int degree = title.LastIndexOf(" · degree ", StringComparison.Ordinal);
            Require(title.StartsWith(profile + " · ", StringComparison.Ordinal) && degree > 0, "Section title: " + title);
            title = title[(profile.Length + 3)..degree];
            Require(chip == Modified && card == chip && save == chip && row == chip && title == chip,
                $"chip=\"{chip}\" card=\"{card}\" save=\"{save}\" row=\"{row}\" title=\"{title}\"");
        });
        DesktopChecks.Check("SourceText_DatOrigin_DesignerReadableAndAllSurfacesAgree", DatOriginAllSurfacesAgree);
        DesktopChecks.Check("SourceText_MySectionsOrigins_AllSurfacesAgree", MySectionsOriginsAllSurfacesAgree);
    }

    private static void DatOriginAllSurfacesAgree()
    {
        byte[] dat = Naca0012SeligDat();
        string expectedName = $"Imported .dat ({Identity.Sha256(dat)[..8]})";
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
        Wait(controller.ApplySectionStepAsync(new SectionStep.Import(dat)));
        AssertAllSurfacesAgree(controller, expectedName, modified: false, rightsTag: " (YOURFILE)");
        var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
        Wait(controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, point.Ordinate + .01)));
        AssertAllSurfacesAgree(controller, expectedName, modified: true, rightsTag: " (YOURFILE)");
    }

    private static void MySectionsOriginsAllSurfacesAgree()
    {
        string root = TestTemp.Combine("src-text-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var library = new SectionLibrary(root);
            byte[] dat = Naca0012SeligDat();
            string datName = $"Imported .dat ({Identity.Sha256(dat)[..8]})";
            // Save a vend: origin must be refused by SectionLibrary.
            using (var setup = new WorkbenchController(sections: library))
            {
                Wait(setup.OpenExampleAsync());
                Wait(setup.EnterSectionAsync(0, EntryOrigin.Palette));
                byte[] profileBlock = FoilSource.ProfileBlock(FoilSource.Parse(setup.Section!.Draft.Bytes).Profile(setup.Section.Draft.Profile)!);
                ContractError? vendRefusal = null;
                try { library.Save("VendRefused", profileBlock, new Provenance("vend:e387", false)); }
                catch (ContractError error) { vendRefusal = error; }
                Require(vendRefusal?.Code == "LIB-SECTION-INVALID", "vend: must be refused by SectionLibrary: " + (vendRefusal?.Code ?? "<none>"));

                // 1) Unrecorded origin saved to My sections.
                Wait(setup.SaveToMySectionsAsync("Mine Unrecorded"));
                // 2) gen: modified origin saved to My sections.
                var naca = Catalog.Load().Single(row => row.Designation == "NACA 0012");
                setup.PreviewReplace(new CatalogChoice.Catalog(naca));
                WaitFor(() => setup.CurrentPreview is not null);
                Wait(setup.ApplyReplaceAsync(ReplaceScope.Draft));
                var pt = setup.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
                Wait(setup.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, pt.Id, pt.SpanMeters, pt.Ordinate + .01)));
                Wait(setup.SaveToMySectionsAsync("Mine Gen Mod"));
                // 3) dat: origin saved to My sections.
                Wait(setup.ApplySectionStepAsync(new SectionStep.Import(dat)));
                Wait(setup.SaveToMySectionsAsync("Mine Dat"));
            }

            using var controller = new WorkbenchController(sections: library);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            var mine = controller.OpenCatalog().Mine;

            var mineGen = mine.Single(entry => entry.Name == "Mine Gen Mod");
            controller.PreviewReplace(new CatalogChoice.Mine(mineGen));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            AssertAllSurfacesAgree(controller, "NACA 0012", modified: true, rightsTag: " (GEN)");

            var mineDat = mine.Single(entry => entry.Name == "Mine Dat");
            controller.PreviewReplace(new CatalogChoice.Mine(mineDat));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            AssertAllSurfacesAgree(controller, datName, modified: false, rightsTag: " (YOURFILE)");

            var mineNull = mine.Single(entry => entry.Name == "Mine Unrecorded");
            controller.PreviewReplace(new CatalogChoice.Mine(mineNull));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            AssertUnrecordedSurfacesAgree(controller);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void AssertAllSurfacesAgree(WorkbenchController controller, string sourceName, bool modified, string rightsTag)
    {
        string profile = controller.Section!.Draft.Profile;
        byte[] bytes = controller.Section.Draft.Bytes;
        string expectedChip = modified ? "Modified from " + sourceName : "Catalog original · " + sourceName;
        string expectedLabel = modified ? "Modified from " + sourceName : sourceName;
        string chip = controller.SourceChip ?? "<null>";
        string card = controller.StationSource(0);
        string propSource = PropertiesView.SourceDisplay(bytes, profile);
        var dialog = new SaveSectionDialog(controller, _ => Task.CompletedTask);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        string save = dialog.FindControl<TextBlock>("ProvenanceLine")?.Text ?? "<null>";
        dialog.Close();
        string row = BrowserPane.StationSourceText(profile, bytes);
        string title = SectionTitle(controller);
        foreach (string surface in new[] { chip, card, propSource, save, row, title })
            Require(!surface.Contains("dat:sha256:", StringComparison.Ordinal), "raw dat hash leaked: " + surface);
        Require(chip == expectedChip, $"chip expected \"{expectedChip}\", got \"{chip}\"");
        Require(card == expectedChip + rightsTag, $"card expected \"{expectedChip + rightsTag}\", got \"{card}\"");
        Require(propSource == expectedChip + rightsTag, $"propSource expected \"{expectedChip + rightsTag}\", got \"{propSource}\"");
        Require(save == "Provenance: " + expectedChip, $"save expected \"Provenance: {expectedChip}\", got \"{save}\"");
        Require(row == profile + " · " + expectedLabel, $"row expected \"{profile} · {expectedLabel}\", got \"{row}\"");
        Require(title.StartsWith(profile + " · " + expectedLabel + " · degree ", StringComparison.Ordinal),
            $"title expected prefix \"{profile} · {expectedLabel} · degree \", got \"{title}\"");
    }

    private static void AssertUnrecordedSurfacesAgree(WorkbenchController controller)
    {
        string profile = controller.Section!.Draft.Profile;
        byte[] bytes = controller.Section.Draft.Bytes;
        const string unrecorded = "Source not recorded";
        string chip = controller.SourceChip ?? "<null>";
        string card = controller.StationSource(0);
        string propSource = PropertiesView.SourceDisplay(bytes, profile);
        var dialog = new SaveSectionDialog(controller, _ => Task.CompletedTask);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        string save = dialog.FindControl<TextBlock>("ProvenanceLine")?.Text ?? "<null>";
        dialog.Close();
        string row = BrowserPane.StationSourceText(profile, bytes);
        string title = SectionTitle(controller);
        Require(chip == unrecorded, "unrecorded chip: " + chip);
        Require(card == unrecorded, "unrecorded card: " + card);
        Require(propSource == unrecorded, "unrecorded propSource: " + propSource);
        Require(save == "Provenance: " + unrecorded, "unrecorded save: " + save);
        Require(row == profile, "unrecorded Browser row: " + row);
        Require(title.StartsWith(profile + " · " + unrecorded + " · degree ", StringComparison.Ordinal),
            "unrecorded title: " + title);
    }

    private static byte[] Naca0012SeligDat()
    {
        var sb = new StringBuilder();
        sb.AppendLine("NACA 0012");
        const int half = 40;
        for (int i = half; i >= 0; i--)
        {
            double x = 0.5 * (1.0 - Math.Cos(Math.PI * i / half));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{x:F6} {Naca0012HalfThickness(x):F6}"));
        }
        for (int i = 1; i <= half; i++)
        {
            double x = 0.5 * (1.0 - Math.Cos(Math.PI * i / half));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{x:F6} {-Naca0012HalfThickness(x):F6}"));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static double Naca0012HalfThickness(double x) =>
        5.0 * 0.12 * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * x * x * x - 0.1036 * x * x * x * x);

    private static string SectionTitle(WorkbenchController controller) =>
        PropertiesView.SectionGroups(SectionContext.Of(controller)!).Single(group => group.Id == "sec").Summary;

    private static WorkbenchController Edited()
    {
        var controller = new WorkbenchController();
        try
        {
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            var entry = Catalog.Load().Single(row => row.Designation == "NACA 0012");
            controller.PreviewReplace(new CatalogChoice.Catalog(entry));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            Wait(controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, point.Ordinate + .01)));
            return controller;
        }
        catch
        {
            controller.Dispose();
            throw;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("source text result did not settle");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static void Wait(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("source text setup did not complete");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        task.GetAwaiter().GetResult();
    }
}
