using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

internal static class LoadsViewTests
{
    internal static void Run()
    {
        Check("Loads_PolarUnavailable_TotalDragNamesProfile", () => {
            var v = ProjectionTests.View(); Equal(true, ProjectionTests.Cell(v, "Loads", "Total drag").Value.Contains("profile"));
        });
        Check("Loads_RendersSafetyVerbatim", () => {
            Equal("Loads are hydrodynamic estimates. Not a structural assessment. Strength, stiffness and fatigue are not evaluated.",
                ProjectionTests.Cell(ProjectionTests.View(), "Loads", "Safety").Value);
        });
        Check("Loads_StiffnessReadoutBesideTc", () => {
            var run = ProjectionTests.Data().Run; var v = ProjectionTests.View(run, new ProjectionContext(RootThicknessRatio: 0.12));
            var rows = v.Groups.Single(g => g.Title == "Loads").Rows;
            int index = Array.FindIndex(rows.ToArray(), r => r.Label == "t/c (root)");
            Equal("12", rows[index].Value); Equal("1.728", rows[index + 1].Value);
        });
        Check("Loads_MissingTerm_UnavailableNeverZero", () => {
            var v = ProjectionTests.View();
            foreach (string label in new[] { "Profile drag", "Total drag", "Moment about attachment point" })
            {
                string value = ProjectionTests.Cell(v, "Loads", label).Value;
                Equal(true, value.StartsWith("Unavailable — ", StringComparison.Ordinal), label + ": " + value);
                Equal(false, value == "0" || value == "0.00", label);
            }
        });
        Check("Loads_StructuralNotAssessed_ListComplete", () => {
            var structural = ProjectionTests.Cell(ProjectionTests.View(), "Loads", "Structural");
            Equal("Structural: Not assessed", structural.Value);
            foreach (string part in new[] { "take-off", "pumping", "breach and slam", "ventilation shock", "impact", "fatigue" })
                Equal(true, structural.Note!.Contains(part), part);
        });
        Check("Section_EditedProfileNoPolar_Unavailable", () => {
            var v = ProjectionTests.View(); Equal(Labels.NoPolar, ProjectionTests.Cell(v, "Section (2D)", "Cl, Cd, Cm, x_tr").Value);
            Equal(Labels.SectionCp, ProjectionTests.Cell(v, "Section (2D)", "Cp_min").Value);
        });
        Check("Strips_OutsideEnvelope_SectionBasedInference", () => {
            var run = ProjectionTests.Data().Run;
            var verdicts = Enumerable.Range(0, 4).Select(_ => MethodRecord.JudgeStrip(12, 0, 0.4, 0)).ToArray();
            var v = ProjectionTests.View(run, new ProjectionContext(Verdicts: verdicts));
            Equal(true, v.Layers.Single(l => l.Id == "plan-gamma").Samples.All(s => s.Outside));
            Equal(true, ProjectionTests.Cell(v, "Labels", "Not modelled").Value.Contains("Section-based inference"));
        });
        Check("Loads_UnitsLbf_KeyUnchanged", () => {
            var (run, current) = ProjectionTests.Data(); var metric = AnalysisProjection.Build(run, current, Units.Metric);
            var imperial = AnalysisProjection.Build(run, current, Units.Imperial);
            Equal(metric.RunKey, imperial.RunKey); Equal("N", ProjectionTests.Cell(metric, "Loads", "Lift (Wing only)").Unit);
            Equal("lbf", ProjectionTests.Cell(imperial, "Loads", "Lift (Wing only)").Unit);
        });
        Check("Loads_MomentDatumAndBodyAxes_Visible", () => {
            var v = ProjectionTests.View(); Equal(true, ProjectionTests.Cell(v, "Wing result", "Basis").Value.Contains("frame origin"));
            Equal(true, ProjectionTests.Cell(v, "Wing result", "Basis").Value.Contains("+y starboard"));
        });
        Check("Loads_ChartTwin_SameSource", () => {
            var v = ProjectionTests.View(); Equal(true, v.Loading.Count > 0);
            Equal(true, v.Loading.All(p => p.ClChordOverMeanChord.HasValue && p.EllipticReference.HasValue));
        });
        Check("Loads_ProvisionalTip_NotOutside", () => {
            var (run, _) = ProjectionTests.Data();
            var strips = run.Strips.Select(s => s.J == 3 ? s with { Provisional = true, ProvisionalReason = StripLoad.TipProvisionalReason } : s).ToArray();
            run = ProjectionTests.Rehash(run with { Strips = strips });
            var verdicts = Enumerable.Range(0, 4).Select(j => j == 3 ? MethodRecord.JudgeStrip(12, 0, 0.4, 0, provisional: true) : MethodRecord.JudgeStrip(2, 0, 0.4, 0)).ToArray();
            var v = ProjectionTests.View(run, new ProjectionContext(Verdicts: verdicts));
            Equal(Labels.TipNotJudged, v.Groups.Single(g => g.Title == "Strips").Rows[3].Note);
            Equal(false, v.Layers.Single(l => l.Id == "plan-gamma").Samples[3].Outside);
        });
    }
}
