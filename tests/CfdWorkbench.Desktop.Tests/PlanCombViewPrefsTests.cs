using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

// Rail comb view preferences (docs/design/view-preferences.md section 8, Rulings 205-207). Ring: fast (headless Avalonia, two or
// three shell windows per check, a temp preference folder); under 2 s each. The restart checks compare the rendered plate and the
// drawn teeth, not only the controller.
public static partial class PlanCanvasTests
{
    private static string PrefRoot()
    {
        string path = Directory.CreateTempSubdirectory("comb-prefs-").FullName;
        return OperatingSystem.IsMacOS() && path.StartsWith("/var/", StringComparison.Ordinal) ? "/private" + path : path;
    }

    private static PreferenceStore PrefsAt(string root) => new(root, () => new ProjectStore());

    private static PlanFixture PrefFixture(string root, bool wait = true)
    {
        var fixture = new PlanFixture(source: CombFoil(), width: 1280, preferences: PrefsAt(root));
        if (wait) WaitLoaded(fixture);
        return fixture;
    }

    private static void WaitLoaded(PlanFixture fixture)
    {
        PropertiesViewTests.Pump(fixture.Host.CombLoaded);
        PropertiesViewTests.Pump(fixture.Host.TextSizeLoaded);
        fixture.Settle();
    }

    private static void WaitSaved(PlanFixture fixture)
    {
        PropertiesViewTests.Pump(fixture.Host.CombSaved);
        fixture.Settle();
    }

    /// <summary>What the plate and the canvas show: the scale and density lines, the Auto state, and pixels at the longest teeth.</summary>
    private sealed record CombShown(bool PlateVisible, string Legend, string Density, bool AutoChecked, (byte, byte, byte)[] Pixels);

    private static IReadOnlyList<Point> ToothPoints(PlanFixture fixture)
    {
        var plan = fixture.Controller.Planform!;
        var points = new List<Point>();
        foreach (var rail in new[] { plan.Leading, plan.Trailing })
            foreach (var sample in rail.Samples.Where((_, i) => i % 5 == 0))
                points.Add(fixture.Canvas.OutlineScreenPoint(sample) + new Vector(0, 12));
        return points;
    }

    private static CombShown Shown(PlanFixture fixture, IReadOnlyList<Point> at) => new(
        PlateOf(fixture).IsVisible,
        Part<TextBlock>(fixture, "LegendText").Text ?? "",
        Part<TextBlock>(fixture, "DensityText").Text ?? "",
        Part<ToggleButton>(fixture, "AutoButton").IsChecked == true,
        at.SelectMany(point => Neighbourhood(fixture, point)).ToArray());

    private static void RunCombView()
    {
        DesktopChecks.Check("Comb_Settings_Persist_ChoiceSurvivesShellRestart_PlateAndTeethRendered", () =>
        {
            string root = PrefRoot();
            CombShown before;
            double? gainBefore;
            IReadOnlyList<Point> at;
            ShellEvents.Clear();
            using (var first = PrefFixture(root))
            {
                Require(!first.Controller.CombVisible, "A fresh root did not start with the comb off");
                first.Host.RunCommand("view.comb").GetAwaiter().GetResult();   // the Curvature toggle (key C)
                first.Settle();
                Click(Part<Button>(first, "LargerButton"));
                Click(Part<Button>(first, "LargerButton"));
                Click(Part<Button>(first, "DenserButton"));
                first.Settle();
                WaitSaved(first);
                Require(first.Controller.CombVisible && first.Controller.CombGain is not null && first.Controller.CombDensity == 64, "The first session did not take its choice");
                at = ToothPoints(first);
                gainBefore = first.Controller.CombGain;
                before = Shown(first, at);
                Require(before.PlateVisible && !before.AutoChecked && before.Density == "64 per rail", "The first plate does not show the choice: " + before);
            }
            ShellEvents.Clear();
            using (var second = PrefFixture(root))
            {
                var after = Shown(second, at);
                Require(second.Controller.CombVisible, "The comb was off after a restart that saved it on");
                Require(second.Controller.CombGain == gainBefore && second.Controller.CombDensity == 64, "The controller did not restore the scale and density");
                Require(after.PlateVisible, "The plate is hidden after a restart that saved the comb on");
                Require(after.Legend == before.Legend && after.Density == before.Density && !after.AutoChecked, $"The plate differs: '{after.Legend}' / '{after.Density}' vs '{before.Legend}' / '{before.Density}'");
                Require(after.Pixels.SequenceEqual(before.Pixels), "The teeth drawn after a restart differ from those drawn before");
                Require(second.Controller.CombAnnouncements == 0, "Loading wrote a live-region sentence");
                Require(ShellEvents.Read().Count(item => item.Name == "display.load") == 1, "display.load was not recorded once");
                Require(!ShellEvents.Read().Any(item => item.Name == "display.save"), "Loading wrote back");
                // The restored state is not the default: a fresh session with no preferences draws differently.
                using var plain = new PlanFixture(source: CombFoil(), width: 1280);
                plain.Controller.CombVisible = true;
                plain.Settle();
                Require(!Shown(plain, at).Pixels.SequenceEqual(before.Pixels), "The default comb draws the same teeth, so the restart check proves nothing");
                // Turning the comb off is kept too.
                second.Host.RunCommand("view.comb").GetAwaiter().GetResult();
                second.Settle();
                WaitSaved(second);
            }
            using var third = PrefFixture(root);
            Require(!third.Controller.CombVisible && !PlateOf(third).IsVisible, "Comb off was not restored");
            Require(third.Controller.CombDensity == 64 && third.Controller.CombGain is not null, "Turning the comb off lost the scale or density");
            Require(File.ReadAllText(Path.Combine(root, "display", "display.json")).Contains("combScale"), "The file holds no scale");
        });

        DesktopChecks.Check("Comb_Settings_Auto_RestoredAsAuto_Visible", () =>
        {
            string root = PrefRoot();
            using (var first = PrefFixture(root))
            {
                first.Controller.CombVisible = true;
                Click(Part<Button>(first, "LargerButton"));
                first.Settle();
                Require(first.Controller.CombGain is not null, "Larger teeth left Auto on");
                first.Controller.SetCombAuto();
                first.Settle();
                WaitSaved(first);
            }
            using var second = PrefFixture(root);
            Require(second.Controller.CombVisible && second.Controller.CombGain is null && second.Controller.CombDensity == 32, "Auto, 32 and on were not restored");
            Require(Part<ToggleButton>(second, "AutoButton").IsChecked == true, "The plate does not show Auto");
        });

        DesktopChecks.Check("Comb_Settings_SaveKeepsTextSizeAndUnits", () =>
        {
            string root = PrefRoot();
            using (var first = PrefFixture(root))
            {
                first.Host.SetTextScale(1.5);
                PropertiesViewTests.Pump(first.Host.TextSizeSaved);
                first.Controller.AnalysisUnits = CfdWorkbench.Analysis.Units.Imperial;
                PropertiesViewTests.Pump(first.Host.UnitsSaved);
                first.Controller.CombVisible = true;
                Click(Part<Button>(first, "DenserButton"));
                WaitSaved(first);
            }
            using var second = PrefFixture(root);
            PropertiesViewTests.Pump(second.Host.UnitsLoaded);
            Require(Math.Abs(second.Host.TextScale - 1.5) < 1e-9 && second.Controller.AnalysisUnits == CfdWorkbench.Analysis.Units.Imperial, "A comb save reset Text size or units");
            Require(second.Controller.CombVisible && second.Controller.CombDensity == 64, "The comb did not come back");
        });

        DesktopChecks.Check("Comb_Settings_ChoiceBeforeLoadWins", () =>
        {
            string root = PrefRoot();
            Directory.CreateDirectory(Path.Combine(root, "display"));
            File.WriteAllText(Path.Combine(root, "display", "display.json"), "{\"format\":\"cfdw-display\",\"version\":1,\"textSize\":100,\"combDensity\":128,\"combVisible\":true}");
            using var fixture = PrefFixture(root, wait: false);
            Click(Part<Button>(fixture, "SparserButton"));   // before the startup read has been applied
            PropertiesViewTests.Pump(fixture.Host.CombLoaded);
            fixture.Settle();
            Require(fixture.Controller.CombDensity != 128, "The file overwrote a choice made before the read finished");
            WaitSaved(fixture);
            using var again = PrefFixture(root);
            Require(again.Controller.CombDensity == fixture.Controller.CombDensity, "The early choice was not kept");
        });

        DesktopChecks.Check("Comb_Settings_LimitPress_NoSave", () =>
        {
            string root = PrefRoot();
            using var fixture = PrefFixture(root);
            fixture.Controller.CombVisible = true;
            fixture.Settle();
            var larger = Part<Button>(fixture, "LargerButton");
            while (fixture.Controller.CanStepScale(larger: true)) Click(larger);
            WaitSaved(fixture);
            ShellEvents.Clear();
            var saved = fixture.Host.CombSaved;
            Click(larger);
            Click(larger);
            fixture.Settle();
            Require(ReferenceEquals(saved, fixture.Host.CombSaved), "A press at the limit started a save");
            Require(!ShellEvents.Read().Any(item => item.Name == "display.save"), "A press at the limit recorded a save");
        });

        DesktopChecks.Check("Comb_Settings_FoilAndProjectBytesUnchanged_Telemetry_NoPath", () =>
        {
            string root = PrefRoot();
            using var fixture = PrefFixture(root);
            string source = fixture.Controller.AcceptedSource;
            string hash = fixture.Controller.Planform!.SourceHash;
            bool undo = fixture.Controller.CanUndo;
            ShellEvents.Clear();
            fixture.Controller.CombVisible = true;
            Click(Part<Button>(fixture, "DenserButton"));
            Click(Part<Button>(fixture, "LargerButton"));
            WaitSaved(fixture);
            Require(fixture.Controller.AcceptedSource == source && fixture.Controller.Planform!.SourceHash == hash && fixture.Controller.CanUndo == undo,
                "A comb preference changed the foil, its hash or the undo stack");
            Require(Directory.GetFiles(root, "*", SearchOption.AllDirectories).All(path => Path.GetFileName(path) is "display.json" or ".cfd-writer.claim"),
                "The preference folder holds a file other than display.json: " + string.Join(", ", Directory.GetFiles(root, "*", SearchOption.AllDirectories)));
            var saves = ShellEvents.Read().Where(item => item.Name == "display.save").ToList();
            Require(saves.Count >= 1 && saves.All(item => item.Trigger == "comb" && item.Outcome == "saved"), "display.save rows lack trigger=comb");
            Require(!ShellEvents.Read().Any(item => item.ToString().Contains(root, StringComparison.Ordinal)), "A telemetry row holds the folder path");
        });

        DesktopChecks.Check("Comb_Settings_TextSizeAndUnitsSaves_CarryTheirTrigger", () =>
        {
            string root = PrefRoot();
            using var fixture = PrefFixture(root);
            ShellEvents.Clear();
            fixture.Host.SetTextScale(1.25);
            PropertiesViewTests.Pump(fixture.Host.TextSizeSaved);
            fixture.Controller.AnalysisUnits = CfdWorkbench.Analysis.Units.Imperial;
            PropertiesViewTests.Pump(fixture.Host.UnitsSaved);
            var saves = ShellEvents.Read().Where(item => item.Name == "display.save").Select(item => item.Trigger).ToList();
            Require(saves.Contains("text-size") && saves.Contains("units"), "Saves lack triggers: " + string.Join(",", saves));
        });

        DesktopChecks.Check("Comb_Settings_ValueFault_KeepsOtherMembers_NeverRewrites", () =>
        {
            string root = PrefRoot();
            Directory.CreateDirectory(Path.Combine(root, "display"));
            string path = Path.Combine(root, "display", "display.json");
            const string bad = "{\"format\":\"cfdw-display\",\"version\":1,\"textSize\":100,\"combScale\":3,\"combDensity\":64,\"combVisible\":true}";
            File.WriteAllText(path, bad);
            using (var fixture = PrefFixture(root))
            {
                Require(fixture.Controller.CombGain is null && fixture.Controller.CombDensity == 64 && fixture.Controller.CombVisible, "A bad scale did not default alone");
                Click(Part<Button>(fixture, "DenserButton"));
                WaitSaved(fixture);
                Require(fixture.Controller.CombDensity == 128, "The session choice was refused");
            }
            Require(File.ReadAllText(path) == bad, "The unreadable file was rewritten");
        });

        DesktopChecks.Check("Comb_Settings_Screenshot_WhenAsked", () =>
        {
            if (Environment.GetEnvironmentVariable("CFD_PROOF_COMB_PNG") is not { Length: > 0 } png) return;
            string root = PrefRoot();
            using (var first = PrefFixture(root))
            {
                first.Controller.CombVisible = true;
                Click(Part<Button>(first, "LargerButton"));
                Click(Part<Button>(first, "LargerButton"));
                Click(Part<Button>(first, "DenserButton"));
                first.Settle();
                WaitSaved(first);
            }
            using var restarted = PrefFixture(root);
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)restarted.Window.Bounds.Width, (int)restarted.Window.Bounds.Height), new Vector(96, 96));
            bitmap.Render(restarted.Window);
            bitmap.Save(png);
        });
    }

}
