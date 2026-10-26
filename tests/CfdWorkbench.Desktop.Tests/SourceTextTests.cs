using Avalonia.Controls;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;

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
    }

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
