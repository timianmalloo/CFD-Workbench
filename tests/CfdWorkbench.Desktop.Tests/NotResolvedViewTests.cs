using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Desktop.Analysis;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track NCR (Ruling 161): the Section profile at one chordwise panel draws no pitching-moment couple, the arrows still start at c/4,
/// and the couple label reads "M′ c/4 (lattice) Not resolved · 1 chordwise panel"; at four panels the couple is drawn as before. Harness:
/// the Desktop harness, mode --analysis (one window, two renders of one control; about 0.3 s).
/// </summary>
public static class NotResolvedViewTests
{
    public static void Run()
    {
        RunAnchor();
        DesktopChecks.Check("SectionProfile_OneChordwisePanel_NoCoupleGlyph_LabelReadsNotResolved_FourPanelsDrawsIt_Ruling161", () =>
        {
            var view = new SectionProfileView();
            var window = new Window { Content = view, Width = 974, Height = 480 };
            window.Show();
            try
            {
                view.Model = Profile(Forces(nChord: 1));
                Settle(window);
                Render(view);
                Equal(false, view.CoupleDrawn, "nc = 1: no couple glyph");
                Near(0.154 * 974 + 0.25 * 0.72 * 974, view.ForceStart!.Value.X, "nc = 1: the arrows start at c/4", 1e-6);
                Equal(true, view.Plates.Any(p => p.Name == "M′ c/4 (lattice) Not resolved · 1 chordwise panel"),
                    "nc = 1: the couple label reads Not resolved: " + string.Join(" | ", view.Plates.Select(p => p.Name)));

                view.Model = Profile(Forces(nChord: 4));
                Settle(window);
                Render(view);
                Equal(true, view.CoupleDrawn, "nc = 4: the couple glyph is drawn");
                Equal(true, view.Plates.Any(p => p.Name.StartsWith("M′ c/4 (lattice) −27.8 N·m/m")), "nc = 4: the couple label keeps its number");
            }
            finally { window.Close(); }
        });
    }

    public static void RunAnchor()
    {
        DesktopChecks.Check("SectionProfile_QuarterChordPlateAndAccessibleName_ReadSf22AtOnePanel_Sf5AtFour_Ruling162", () =>
        {
            var view = new SectionProfileView();
            var window = new Window { Content = view, Width = 974, Height = 480 };
            window.Show();
            try
            {
                view.Model = Profile(Forces(nChord: 1));
                Settle(window);
                Render(view);
                Equal(true, view.Plates.Any(p => p.Name == "c/4 · arrows start here · x_cp not resolved"), "nc = 1: the plate reads SF22: " + string.Join(" | ", view.Plates.Select(p => p.Name)));
                Equal(false, view.Plates.Any(p => p.Name.Contains("x_cp Undefined")), "nc = 1: no Undefined plate");
                string name1 = Avalonia.Automation.AutomationProperties.GetName(view) ?? "";
                Equal(true, name1.Contains("c/4 · arrows start here · x_cp not resolved") && !name1.Contains("Undefined"), "nc = 1: the accessible name reads SF22: " + name1);

                view.Model = Profile(Forces(nChord: 4));
                Settle(window);
                Render(view);
                Equal(true, view.Plates.Any(p => p.Name == "c/4 · arrows start here · x_cp Undefined"), "nc = 4: the plate keeps SF5");
                Equal(true, (Avalonia.Automation.AutomationProperties.GetName(view) ?? "").Contains("c/4 · arrows start here · x_cp Undefined"), "nc = 4: the accessible name keeps SF5");
            }
            finally { window.Close(); }
        });
    }

    private static SectionForces Forces(int nChord) =>
        new(Eta: 0.5, ChordMeters: 0.16, AlphaGeoDeg: 3.5, AlphaEffDeg: 2.5, AlphaIDeg: 1, ClLattice: 0.4, LiftPerSpan: 479, CouplePerSpan: -27.8,
            XcpOverC: null, Anchor: ForceAnchor.QuarterChord, XcpText: "x", ProfileLow: 17.1, ProfileHigh: 18.0, ProfileFlags: null, ProfileUnavailable: null,
            InducedPerSpan: 8.4, LiftScale: 2000, DragMultiple: 10, Units: Units.Metric, NChord: nChord, CoupleScale: 1000);

    private static SectionProfile Profile(SectionForces forces)
    {
        var outline = new[] { new PanelCp(1, 0, 0.1), new PanelCp(0.5, 0.06, -0.5), new PanelCp(0, 0, 0.99), new PanelCp(0.5, -0.06, -0.3), new PanelCp(1, 0, 0.1) };
        return new SectionProfile(outline, outline[1], "upper", -0.5, 0.99, "Section · η 0.5", "Estimator · local calculation · inviscid; no boundary layer", null, forces);
    }

    private static void Render(Control control)
    {
        PixelSize size = new((int)Math.Max(20, control.Bounds.Width), (int)Math.Max(20, control.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
        bitmap.Render(control);
    }

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }

    private static void Near(double expected, double actual, string what, double tolerance)
    {
        if (!(Math.Abs(expected - actual) <= tolerance)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }
}
