using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Desktop.Analysis;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track SFV (Rulings 127, 128, 130): the Lift and Drag vectors as drawn. Ring: readiness (one window, three renders of one control;
/// about 0.3 s). The angles are read from the drawn geometry (<see cref="SectionProfileView.Vectors"/>), not from the model; the model's
/// numbers and labels are checked in the Analysis harness (SectionForceTests).
/// </summary>
public static class SectionForceViewTests
{
    public static void RunReadiness()
    {
        DesktopChecks.Check("SectionForceVectors_Drawn_LiftPerpendicularDragParallelToFreeStream_AnchorByRule", () =>
        {
            // alpha_geo 3.5 deg (twist 0.5), alpha_eff 2.5, alpha_i 1: the V-inf arrow, the drag arrows and the lift arrow are read in pixels.
            var view = new SectionProfileView();
            var window = new Window { Content = view, Width = 974, Height = 480 };
            window.Show();
            try
            {
                view.Model = Profile(Forces(xcp: 0.30, cl: 0.40, anchor: ForceAnchor.CentreOfPressure));
                Settle(window);
                Render(view);
                double ang(SectionProfileView.DrawnVector v) => Math.Atan2(-(v.To.Y - v.From.Y), v.To.X - v.From.X) * 180 / Math.PI;
                var vectors = view.Vectors.ToDictionary(v => v.Name);
                Equal(true, vectors.Count == 5, "five vectors: " + string.Join(",", vectors.Keys));
                Near(3.5, ang(vectors["V∞"]), "V∞ is drawn at α_geo to the chord", 1e-6);
                Near(2.5, ang(vectors["inflow"]), "the local inflow is drawn at α_eff", 1e-6);
                Near(3.5, ang(vectors["drag-profile"]), "profile drag is parallel to V∞", 1e-6);
                Near(3.5, ang(vectors["drag-induced"]), "induced drag is parallel to V∞", 1e-6);
                var lift = vectors["lift"];
                Near(3.5 + 90, ang(lift), "lift is perpendicular to V∞ and leans forward of the chord normal", 1e-6);
                double dot = (lift.To.X - lift.From.X) * (vectors["drag-profile"].To.X - vectors["drag-profile"].From.X) +
                    (lift.To.Y - lift.From.Y) * (vectors["drag-profile"].To.Y - vectors["drag-profile"].From.Y);
                Near(0, dot, "lift · drag = 0 in pixels", 1e-6);
                Equal(true, lift.To.Y < lift.From.Y, "positive lift points up the screen");
                Equal(vectors["drag-profile"].To, vectors["drag-induced"].From, "the induced segment continues the profile segment");
                Equal(false, view.CoupleDrawn, "CP case: no couple");
                Near(view.ForceStart!.Value.X, vectors["lift"].From.X, "the arrows start at the anchor", 1e-9);

                // c/4 case: arrows at a quarter of the chord, the couple drawn. The chord is 0.72 of the viewport width from 0.154 of it.
                view.Model = Profile(Forces(xcp: 0.42, cl: 0.012, anchor: ForceAnchor.QuarterChord));
                Settle(window);
                Render(view);
                double ox = 0.154 * 974, s = 0.72 * 974;
                Near(ox + 0.25 * s, view.ForceStart!.Value.X, "c/4 anchor in pixels", 1e-6);
                Equal(true, view.CoupleDrawn, "c/4 case: the pitching-moment couple is drawn");
                Equal(464.0, view.Bounds.Height, "the 420 px viewport and the 44 px key strip are in the control height");

                // the Cp-only profile keeps its height and draws no vectors
                view.Model = Profile(null);
                Settle(window);
                Render(view);
                Equal(0, view.Vectors.Count, "no forces, no vectors");
                Equal(240.0, view.Height, "the Cp-only view keeps its 240 px");
            }
            finally { window.Close(); }
        });
    }

    private static SectionForces Forces(double xcp, double cl, ForceAnchor anchor) =>
        new(Eta: 0.5, ChordMeters: 0.16, AlphaGeoDeg: 3.5, AlphaEffDeg: 2.5, AlphaIDeg: 1, ClLattice: cl, LiftPerSpan: 479, CouplePerSpan: -27.8,
            XcpOverC: xcp, Anchor: anchor, XcpText: "x", ProfileLow: 17.1, ProfileHigh: 18.0, ProfileFlags: null, ProfileUnavailable: null,
            InducedPerSpan: 8.4, LiftScale: 2000, DragMultiple: 10, Units: Units.Metric);

    private static SectionProfile Profile(SectionForces? forces)
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
