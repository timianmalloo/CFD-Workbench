using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Track NCR (Ruling 161): at one chordwise panel the lattice cannot resolve x_cp or the pitching moment, so the strip table says
/// "Not resolved · 1 chordwise panel" and drops the bias suffix; at two or more panels nothing changes. Ring: fast (every join); two
/// real lattice runs on the Example foil at 2 spans, about 0.3 s each, built once per panel count.
/// </summary>
internal static class NotResolvedTests
{
    private const string NotResolved = "Not resolved · 1 chordwise panel";

    internal static void Run()
    {
        Check("NotResolved_OneChordwisePanel_TableValuesReadRuling161_LabelsDropBias", OnePanelTable);
        Check("NotResolved_OneChordwisePanel_NoteReadsRuling161", OnePanelNote);
        Check("NotResolved_FourChordwisePanels_TextsUnchanged", FourPanelsUnchanged);
        Check("NotResolved_TwoChordwisePanels_StillResolved_StructuralRuleNotMagnitude", TwoPanelsResolved);
    }

    private static readonly Dictionary<int, (SectionForces, ResultGroup)> Runs = [];

    // one real lattice run per panel count (about 0.3 s each), shared by the checks of this group
    private static (SectionForces Forces, ResultGroup Table) ExampleRun(int nChord) =>
        Runs.TryGetValue(nChord, out var held) ? held : Runs[nChord] = Build(nChord);

    private static (SectionForces Forces, ResultGroup Table) Build(int nChord)
    {
        using var session = new AuthoringSession();
        byte[] source = FoilSource.NewDefault();
        session.Open(source, Fixture.Id(), true);
        RunSettings settings = Settings.Default with { NSpanPerHalf = 2, NChord = nChord,
            SectionEtas = [0d, 0.5, 1d], SectionXs = Settings.ChordXs(nChord, "cosine") };
        var service = new AnalysisService(session, new ProductWingMethod(settings));
        AnalysisRun run = Fixture.Evaluate(service, Fixture.Op(3));
        SectionTierResult tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], [], Fixture.Op(3), Fixture.Salt);
        SectionView view = SectionDisplay.Build(run, tier, source, 0.5, "r1", null, null, default, Units.Metric);
        return (view.Profile!.Forces!, view.Groups.Single(g => g.Title == Labels.StripTableHeading));
    }

    private static void OnePanelTable()
    {
        (SectionForces f, ResultGroup table) = ExampleRun(1);
        ResultRow xcp = table.Rows.Single(r => r.Label == "x_cp/c (lattice, 1 chordwise panel)");
        ResultRow couple = table.Rows.Single(r => r.Label == "M′ c/4 (lattice, 1 chordwise panel)");
        Equal(NotResolved, xcp.Value, "x_cp/c reads Not resolved, not the bare Undefined");
        Equal(NotResolved, couple.Value, "M′ c/4 reads Not resolved, not 0.00");
        Equal(null, couple.Unit, "a value that is not a number carries no unit");
        Equal(NotResolved, f.XcpText, "the record carries the same text");
        Equal(ForceAnchor.QuarterChord, f.Anchor, "the arrows still start at c/4");
    }

    private static void OnePanelNote()
    {
        (_, ResultGroup table) = ExampleRun(1);
        Equal("With one chordwise panel the lattice can't resolve the centre of pressure or the pitching moment, so the arrows start at the quarter chord and no couple is drawn.",
            table.Rows.Single(r => r.Label == "x_cp/c (lattice, 1 chordwise panel)").Note, "the note under the table");
    }

    private static void FourPanelsUnchanged()
    {
        (SectionForces f, ResultGroup table) = ExampleRun(4);
        ResultRow xcp = table.Rows.Single(r => r.Label == "x_cp/c (lattice, 4 chordwise panels; biased forward at low lift)");
        ResultRow couple = table.Rows.Single(r => r.Label == "M′ c/4 (lattice, 4 chordwise panels; biased forward at low lift)");
        Equal(false, xcp.Value.StartsWith("Not resolved"), "x_cp keeps its number or its Undefined reason");
        Equal(Labels.Sig3(Labels.MomentPerSpan(f.CouplePerSpan, Units.Metric)), couple.Value, "the couple keeps its three significant figures");
        Equal("N·m/m", couple.Unit, "and its unit");
        Equal(f.Anchor == ForceAnchor.QuarterChord ? Labels.CouplePlaceNote : null, xcp.Note, "the old note stays where it was");
    }

    private static void TwoPanelsResolved()
    {
        (_, ResultGroup table) = ExampleRun(2);
        ResultRow xcp = table.Rows.Single(r => r.Label == "x_cp/c (lattice, 2 chordwise panels; biased forward at low lift)");
        ResultRow couple = table.Rows.Single(r => r.Label == "M′ c/4 (lattice, 2 chordwise panels; biased forward at low lift)");
        Equal(false, xcp.Value.StartsWith("Not resolved") || couple.Value.StartsWith("Not resolved"), "two panels resolve the moment");
    }
}
