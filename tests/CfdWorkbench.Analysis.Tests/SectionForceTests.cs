using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Track SFV (Rulings 127, 128, 130): the Lift and Drag vectors on the Section profile, as pure data. Fast ring: synthetic strips
/// whose numbers are chosen so the expected value is arithmetic, not a copy of the code under test (each check under 0.05 s).
/// Readiness ring: one real lattice run on a cambered catalogue section (about 1 s), where the strip shares must sum to the wing D_i.
/// </summary>
internal static class SectionForceTests
{
    private const double Chord = 0.1, LeadingX = 0.07, LeadingZ = 0.01;

    private static void Near(double expected, double actual, string what, double tolerance)
    {
        if (!(Math.Abs(expected - actual) <= tolerance)) throw new InvalidOperationException($"{what} expected {expected}; actual {actual} (tolerance {tolerance})");
    }

    internal static void Run()
    {
        Check("SectionForce_FreeStreamAxes_LiftPerpendicularDragParallel", FreeStreamAxes);
        Check("SectionForce_Anchor_CpNearZeroAndOffSection_Ruling128And130", Anchor);
        Check("SectionForce_Imperial_ForceAndMomentPerSpan", Imperial);
        Check("SectionForce_RunScale_LiftFixedByLargestStripDragByRule", RunScale);
        Check("SectionForce_Labels_EqualTheirDesignRows_Ruling130", LabelRows);
    }

    internal static void RunReadiness()
    {
        Check("SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip", LatticeRun);
        Check("SectionForce_EllipticWing_InducedShareFollowsSqrtOneMinusEtaSquared", EllipticDistribution);
        Check("SectionForce_ChordwiseConvergence_MeasuredNotGated_Ruling131", ChordwiseConvergence);
        Check("SectionForce_TipStrip_NotJudged_NoAnchorNoJudgedValues_Ruling131", TipStrip);
    }

    // ---- synthetic strips -------------------------------------------------------------------------------------------------

    /// <summary>A strip whose centre of pressure sits at <paramref name="xcp"/> c: with the strip's bound segments on the plane z = LeadingZ,
    /// M_origin,y = z_LE Fx - (x_LE + xcp c) Fz exactly.</summary>
    private static StripLoad Strip(double xcp, double cl, double fz = 10, double fx = 0.5, double gamma = 0.01, double downwash = -0.1,
        double alphaI = 1, double alphaEff = 2, StripValue? cd2 = null, StripValue? cd4 = null) =>
        new(0, 0, 0.5, Chord, gamma, alphaI, alphaEff, 500000, cl, cd2 ?? new StripValue(0.0080, null), cd4 ?? new StripValue(0.0084, null),
            fx, 0, fz, 0, LeadingZ * fx - (LeadingX + xcp * Chord) * fz, 0, downwash);

    private static AnalysisRun RunOf(StripLoad strip, int nChord = 4, double alpha = 3)
    {
        AnalysisRun run = ProjectionTests.Data(_ => strip with { J = _.J, Y = _.Y, Eta = _.Eta }, alpha).Run;
        return ProjectionTests.Rehash(run with { Settings = run.Settings with { NChord = nChord } });
    }

    private static SectionForces Forces(StripLoad strip, int nChord = 4, double alpha = 3, Units units = Units.Metric)
    {
        AnalysisRun run = RunOf(strip, nChord, alpha);
        return SectionForceModel.Compute(run, run.Strips[0], LeadingX, LeadingZ, units)!;
    }

    private static double Width(AnalysisRun run) => Loads.StripWidth(run, run.Strips[0]);

    // ---- behaviours -------------------------------------------------------------------------------------------------------

    /// <summary>Ruling 128 (1): V∞ at α_geo (the strip's own twist included), lift perpendicular and drag parallel to it.</summary>
    private static void FreeStreamAxes()
    {
        // alpha_geo = alpha_eff + alpha_i = 3.5 deg with the operating alpha 3 deg: the 0.5 deg is twist, read from the strip, never assumed zero.
        StripLoad strip = Strip(0.3, 0.4, alphaEff: 2.5, alphaI: 1);
        AnalysisRun run = RunOf(strip);
        SectionForces f = SectionForceModel.Compute(run, run.Strips[0], LeadingX, LeadingZ, Units.Metric)!;
        Near(3.5, f.AlphaGeoDeg, "alpha_geo = alpha_eff + alpha_i", 1e-12);
        double angle = Math.Atan2(f.FreeStream.Z, f.FreeStream.X) * 180 / Math.PI;
        Near(3.5, angle, "the drag direction lies at alpha_geo to the chord", 1e-9);
        Near(0.0, f.LiftDirection.X * f.FreeStream.X + f.LiftDirection.Z * f.FreeStream.Z, "lift is perpendicular to V-inf", 1e-12);
        Equal(true, f.LiftDirection.Z > 0 && f.LiftDirection.X < 0, "lift leans forward of the chord normal at positive alpha");
        // L' is the body-axes strip lift per span at the operating alpha, the number AnalysisProjection.StripDetails shows.
        double a = 3 * Math.PI / 180, width = Width(run);
        Near((-strip.Fx * Math.Sin(a) + strip.Fz * Math.Cos(a)) / width, f.LiftPerSpan, "L' per span", 1e-9);
        // the induced share is the lifting-line d' = 0.5 rho Gamma (-w_T), not the near-field Fx
        Near(0.5 * run.Water.Rho * strip.Gamma * 0.1, f.InducedPerSpan, "d' induced per span", 1e-12);
        double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
        Near(q * Chord * 0.0080, f.ProfileLow!.Value, "profile low = q c cd(Ncrit 2 or 4, the smaller)", 1e-9);
        Near(q * Chord * 0.0084, f.ProfileHigh!.Value, "profile high", 1e-9);
    }

    /// <summary>Ruling 128 (3), Ruling 130: CP on the chord with |Cl_local (lattice)| >= 0.05, else c/4 with the couple and x_cp Undefined.</summary>
    private static void Anchor()
    {
        SectionForces cp = Forces(Strip(0.30, 0.40));
        Equal(ForceAnchor.CentreOfPressure, cp.Anchor, "CP case");
        Near(0.30, cp.XcpOverC!.Value, "x_cp from the strip's own moment and normal force, moved from the frame origin to the leading edge", 1e-9);
        Near(0.30, cp.AnchorX, "the arrows start at CP", 1e-9);
        Equal("0.30", cp.XcpText, "the table reads the number");

        SectionForces zero = Forces(Strip(0.42, 0.012));
        Equal(ForceAnchor.QuarterChord, zero.Anchor, "|Cl_local| < 0.05: c/4, though x_cp = 0.42 is on the chord");
        Equal(0.25, zero.AnchorX, "c/4");
        Equal(Labels.XcpNearZeroLift, zero.XcpText, "x_cp Undefined near zero lift");
        Near(0.42, zero.XcpOverC!.Value, "the quotient is kept for the couple, not shown as a result", 1e-9);
        // M' about c/4 per span = (M_LE + c/4 Fz) / width = -(x_cp - 0.25) c Fz / width
        AnalysisRun run = RunOf(Strip(0.42, 0.012));
        Near(-(0.42 - 0.25) * Chord * 10 / Width(run), zero.CouplePerSpan, "the pitching-moment couple about c/4", 1e-9);

        // Ruling 130: a cambered section at cl 0.10 (Cm c/4 -0.08) puts x_cp at 0.25 + 0.08/0.10 = 1.05, off the section.
        SectionForces cambered = Forces(Strip(1.05, 0.10));
        Equal(ForceAnchor.QuarterChord, cambered.Anchor, "x_cp 1.05 is off the chord");
        Equal(Labels.XcpOffSection, cambered.XcpText, "x_cp Undefined, off the section");
        Equal(0.25, cambered.AnchorX, "c/4");
        Near(0.78, Forces(Strip(0.78, 0.15)).XcpOverC!.Value, "at cl 0.15 (Cm -0.08) x_cp is 0.78, still on the chord", 1e-9);
        Equal(ForceAnchor.CentreOfPressure, Forces(Strip(0.78, 0.15)).Anchor, "so it is the anchor");

        // one chordwise panel has its bound vortex at c/4 by construction: x_cp carries no information and is never shown as a result
        SectionForces onePanel = Forces(Strip(0.30, 0.40), nChord: 1);
        Equal(ForceAnchor.QuarterChord, onePanel.Anchor, "nc = 1");
        Equal(null, onePanel.XcpOverC, "x_cp is not defined by a one-panel lattice");
    }

    /// <summary>Units follow the switch: N/m to lbf/ft, and N·m/m to lbf·ft/ft (a moment per span has the dimension of a force).</summary>
    private static void Imperial()
    {
        Near(0.0685218, Labels.ForcePerSpan(1, Units.Imperial), "1 N/m in lbf/ft", 1e-6);
        Near(0.224809, Labels.MomentPerSpan(1, Units.Imperial), "1 N·m/m in lbf·ft/ft", 1e-6);
        Equal("lbf/ft", Labels.ForcePerSpanUnit(Units.Imperial), "force unit");
        Equal("lbf·ft/ft", Labels.MomentPerSpanUnit(Units.Imperial), "moment unit");
        SectionForces metric = Forces(Strip(0.42, 0.012)), imperial = Forces(Strip(0.42, 0.012), units: Units.Imperial);
        // the scale rounds 1-2-5 in the unit shown, so the arrows differ from the metric ones (mockup state D); neither exceeds 0.3 chord
        Equal(true, imperial.LiftChords <= SectionForceModel.LiftArrowChords + 1e-12 && metric.LiftChords <= SectionForceModel.LiftArrowChords + 1e-12, "lift arrow within 0.3 chord");
        Equal(true, Math.Abs(imperial.LiftChords - metric.LiftChords) > 1e-6, "the scale is re-derived in the unit shown");
        double shownScale = imperial.LiftScale * Labels.ForcePerSpan(1, Units.Imperial);
        Near(SectionForceModel.RoundUp125(shownScale), shownScale, "the scale in lbf/ft is a 1-2-5 number", 1e-6 * shownScale);
        Equal(Labels.Sig3(metric.LiftPerSpan * 0.0685218), Labels.LiftLabel(imperial.LiftPerSpan, Units.Imperial).Split(' ')[1], "L' reads in lbf/ft");
        Equal(true, Labels.CoupleLabel(imperial.CouplePerSpan, Units.Imperial).EndsWith(" lbf·ft/ft"), "the couple label unit");
    }

    /// <summary>Ruling 128 (4): one lift scale per run from the largest strip |L'| (1-2-5 rounded); one drag multiple per run by a stated rule.</summary>
    private static void RunScale()
    {
        Equal(2000.0, SectionForceModel.RoundUp125(1733), "1-2-5 up");
        Equal(0.5, SectionForceModel.RoundUp125(0.31), "1-2-5 up, below one");
        Equal(200.0, SectionForceModel.RoundUp125(200), "an exact member stays");
        AnalysisRun run = ProjectionTests.Data(s => s with { Fz = 10 * (1 + s.J) }).Run;   // strips 0..3 carry 10, 20, 30, 40 N
        (double scale, int multiple) = SectionForceModel.RunScale(run, Units.Metric);
        double top = run.Strips.Max(s => (-s.Fx * Math.Sin(3 * Math.PI / 180) + s.Fz * Math.Cos(3 * Math.PI / 180)) / Loads.StripWidth(run, s));
        Near(SectionForceModel.RoundUp125(top / SectionForceModel.LiftArrowChords), scale, "the scale reads the largest strip", 1e-9);
        Equal(true, scale >= top / SectionForceModel.LiftArrowChords, "the largest lift arrow is at most 0.3 chord");
        Equal(true, new[] { 10, 5, 2, 1 }.Contains(multiple), "the multiple is one of 10, 5, 2, 1");
        // the rule: no strip's total drag arrow is longer than 0.15 chord at the chosen multiple, and the next larger multiple would be
        foreach (StripLoad s in run.Strips)
        {
            double total = 0.5 * run.Water.Rho * s.Gamma * -s.DownwashTrefftz;
            Equal(true, Math.Abs(total) * multiple / scale <= SectionForceModel.DragArrowMaxChords + 1e-12, "drag arrow fits at the chosen multiple");
        }
        // a lift scale in the unit shown: the label number is the rounded one
        (double imperialScale, _) = SectionForceModel.RunScale(run, Units.Imperial);
        double shown = imperialScale * Labels.ForcePerSpan(1, Units.Imperial);
        Near(shown, SectionForceModel.RoundUp125(shown), "the scale shown in lbf/ft is a 1-2-5 number", 1e-9);
    }

    /// <summary>Ruling 130: all 16 labels are approved as drawn; each Labels output equals its DESIGN.md row (COPY-SF1..SF16).</summary>
    private static void LabelRows()
    {
        string design = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DESIGN.md")).Replace("\\|", "|");
        const string Tail = " — approved — Ruling 130";
        (string Row, string Template, string Expected, string Actual)[] rows =
        [
            ("COPY-SF1", "L′ <v> <unit> (lattice) ⏎ 1 c = <s> <unit>, fixed per run", "L′ 479 N/m (lattice) ⏎ 1 c = 2000 N/m, fixed per run", Labels.LiftLabel(479.4, Units.Metric) + " ⏎ " + Labels.LiftScale(2000, Units.Metric)),
            ("COPY-SF2", "V∞ at α_geo <a>°", "V∞ at α_geo 3.00°", Labels.FreeStream(3)),
            ("COPY-SF3", "local inflow α_eff <a>° ⏎ tilts the flow by α_i", "local inflow α_eff 1.97° ⏎ tilts the flow by α_i", Labels.LocalInflow(1.97) + " ⏎ " + Labels.LocalInflowWhy),
            ("COPY-SF4", "CP (lattice) · x/c <x>", "CP (lattice) · x/c 0.27", Labels.AnchorCp(0.27)),
            ("COPY-SF5", "c/4 · arrows start here · x_cp Undefined", "c/4 · arrows start here · x_cp Undefined", Labels.AnchorQuarter),
            ("COPY-SF6", "M′ c/4 (lattice) <v> <unit>", "M′ c/4 (lattice) −27.8 N·m/m", Labels.CoupleLabel(-27.84, Units.Metric)),
            ("COPY-SF7", "D′ profile (polar, Ncrit 2–4) <min>–<max> <unit> · ×<k>", "D′ profile (polar, Ncrit 2–4) 17.1–18.0 N/m · ×10", Labels.ProfileDragLabel(17.1, 18.0, Units.Metric, 10, false)),
            ("COPY-SF8", "D′ induced, lifting-line share (lattice) <v> <unit> · ×<k>", "D′ induced, lifting-line share (lattice) 8.40 N/m · ×10", Labels.InducedDragLabel(8.4, Units.Metric, 10)),
            ("COPY-SF9", "D′ profile + induced (band centre), free-stream axes <v> <unit> · ×<k>", "D′ profile + induced (band centre), free-stream axes 25.9 N/m · ×10", Labels.TotalDragLabel(25.9, Units.Metric, 10)),
            ("COPY-SF10", "<SF7 text> · low confidence (profile label suffix) ⏎ <min>–<max> · flagged (table suffix)",
                "<SF7 text> · low confidence (profile label suffix) ⏎ 17.1–18.0 · flagged (table suffix)",
                Labels.ProfileDragLabel(17.1, 18.0, Units.Metric, 10, true).Replace("D′ profile (polar, Ncrit 2–4) 17.1–18.0 N/m · ×10", "<SF7 text>") + " (profile label suffix) ⏎ " +
                Labels.DragBand(17.1, 18.0, Units.Metric) + " " + Labels.FlaggedSuffix + " (table suffix)"),
            ("COPY-SF11", "Wing strip, per span; not the wing total", "Wing strip, per span; not the wing total", Labels.StripTableHeading),
            ("COPY-SF12", "cl (panel, 2D inviscid at α_eff) · Cm c/4 (panel, 2D inviscid) · Cl_local (lattice) · α_geo · α_eff (lattice) · α_i (lattice) · x_cp/c (lattice, <n> chordwise panels; biased forward at low lift) · L′ (lattice) · M′ c/4 (lattice, <n> chordwise panels; biased forward at low lift) · D′ profile (polar, Ncrit 2–4) · D′ induced (lattice) · D′ profile + induced (band centre), free-stream axes",
                "cl (panel, 2D inviscid at α_eff) · Cm c/4 (panel, 2D inviscid) · Cl_local (lattice) · α_geo · α_eff (lattice) · α_i (lattice) · x_cp/c (lattice, 4 chordwise panels; biased forward at low lift) · L′ (lattice) · M′ c/4 (lattice, 4 chordwise panels; biased forward at low lift) · D′ profile (polar, Ncrit 2–4) · D′ induced (lattice) · D′ profile + induced (band centre), free-stream axes",
                string.Join(" · ", Labels.ClPanelRow, Labels.CmPanelRow, Labels.ClLatticeRow, Labels.AlphaGeoRow, Labels.AlphaEffRow, Labels.AlphaIRow, Labels.XcpRowLabel(4),
                    Labels.LiftRow, Labels.CoupleRowLabel(4), Labels.ProfileDragRow, Labels.InducedDragRow, Labels.TotalDragRow)),
            ("COPY-SF13", "Undefined · near zero lift: |cl| is below 0.05", "Undefined · near zero lift: |cl| is below 0.05", Labels.XcpNearZeroLift),
            ("COPY-SF14", "Undefined · the centre of pressure is off the section", "Undefined · the centre of pressure is off the section", Labels.XcpOffSection),
            ("COPY-SF15", "The centre of pressure is undefined here, so the arrows start at the quarter chord and the pitching-moment couple is drawn.",
                "The centre of pressure is undefined here, so the arrows start at the quarter chord and the pitching-moment couple is drawn.", Labels.CouplePlaceNote),
            ("COPY-SF17", "lattice, <n> chordwise panels; biased forward at low lift", "lattice, 4 chordwise panels; biased forward at low lift", Labels.LatticeBias(4)),
            ("COPY-SF16", "V∞ · Local inflow · Lift · Drag, profile (cap: Ncrit 2–4 band) · Drag, induced · Pitching-moment couple",
                "V∞ · Local inflow · Lift · Drag, profile (cap: Ncrit 2–4 band) · Drag, induced · Pitching-moment couple", string.Join(" · ", Labels.VectorKey))
        ];
        foreach ((string row, string template, string expected, string actual) in rows)
        {
            string tail = row is "COPY-SF9" or "COPY-SF12" or "COPY-SF17" ? " — approved — Ruling 131" : Tail;
            Equal(true, design.Contains("| " + row + " | " + template + tail), row + " row in DESIGN.md");
            Equal(expected, actual, row + " text");
        }
        Equal("479", Labels.Sig3(479.4), "three significant figures");
        Equal("0.230", Labels.Sig3(0.2304), "trailing zero kept");
        Equal("1230", Labels.Sig3(1234), "above 999 rounds to the hundreds-tens");
        Equal("10.0", Labels.Sig3(9.996), "rounding up across a decade keeps three figures");
        Equal("−0.0440", Labels.Sig3(-0.04403), "U+2212 minus");
    }

    // ---- readiness: one real lattice run on a cambered catalogue section ---------------------------------------------------

    /// <summary>One real lattice run on the cambered section, shared by the readiness checks of this class (about 1 s once).</summary>
    internal static (AnalysisRun Run, byte[] Source) CamberedRun()
    {
        if (camberedRun is { } held) return held;
        using var session = new AuthoringSession();
        byte[] source = CamberedSource();
        session.Open(source, Fixture.Id(), true);
        RunSettings settings = Settings.Default with { NSpanPerHalf = 8, NChord = 4,
            SectionEtas = [0d, 0.5, 1d], SectionXs = Settings.ChordXs(4, "cosine") };
        var service = new AnalysisService(session, new ProductWingMethod(settings));
        return (camberedRun = (Fixture.Evaluate(service, Fixture.Op(1)), source)).Value;
    }
    private static (AnalysisRun Run, byte[] Source)? camberedRun;

    /// <summary>Ruling 131: a provisional (tip) strip reads Not judged - tip strip; no CP anchor, no L' or x_cp presented as judged values.</summary>
    private static void TipStrip()
    {
        (AnalysisRun run, byte[] source) = CamberedRun();
        StripLoad tip = run.Strips.MaxBy(s => Math.Abs(s.Eta))!;
        Equal(StripLoad.TipProvisionalReason, tip.ProvisionalReason, "the outermost strip is the provisional tip strip");
        SectionTierResult tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], [], Fixture.Op(1), Fixture.Salt);
        // eta 1 and the outermost strip's own eta both select the tip station
        foreach (double eta in new[] { 1.0, Math.Abs(tip.Eta) })
        {
            SectionView view = SectionDisplay.Build(run, tier, source, eta, "r1", null, null, default, Units.Metric);
            Equal(null, view.Profile!.Forces, "tip strip, eta " + eta + ": no force vectors, so no CP anchor is drawn");
            Equal(Labels.TipNotJudged, view.Profile.ForcesNotJudged, "tip strip, eta " + eta + ": the profile reads Not judged - tip strip");
            ResultGroup table = view.Groups.Single(g => g.Title == Labels.StripTableHeading);
            Equal(Labels.TipNotJudged, table.Rows.Single(r => r.Label == "Result").Value, "tip strip, eta " + eta + ": the table reads Not judged - tip strip");
            Equal(false, table.Rows.Any(r => r.Label is Labels.LiftRow or Labels.XcpRow or Labels.CoupleRow), "tip strip: no L', x_cp or M' row");
        }
        Equal(true, SectionDisplay.Build(run, tier, source, 0.5, "r1", null, null, default, Units.Metric).Profile!.Forces is not null, "interior strip keeps its vectors");
    }


    /// <summary>
    /// The distribution check (Ruling 131). The sum check of <see cref="LatticeRun"/> only shows the strips share the wing's D_i; it would
    /// pass for any weights that add up. On the exact elliptic planform (zero twist, uniform alpha) the lifting-line theory says the induced
    /// share d'(y) = 1/2 rho Gamma (-w_T) is proportional to sqrt(1 - eta^2), because Gamma is and w_T is uniform. The lattice is a lifting
    /// surface of finite aspect ratio (8), so w_T is not uniform: it falls off toward the tip (F-15: alpha_i / (CL/(pi AR)) is 1.016 at the
    /// centre, 1.001 at eta 0.5, 0.937 at 0.8, 0.837 at 0.9). The tolerances below are stated from that measured profile, not widened to pass.
    /// </summary>
    private static void EllipticDistribution()
    {
        LatticeSolution wing = LatticeFixtureTests.Elliptic(32, LatticePlant.None, "cosine");
        AnalysisRun shell = ProjectionTests.Data().Run;
        StripLoad[] loads = wing.Strips.Select((s, j) => new StripLoad(s.J, s.Y, s.Eta, s.Chord, s.Gamma, s.InducedAngleDeg, 5 - s.InducedAngleDeg, 1e6, s.ClLocal,
            new StripValue(null, "no polar"), new StripValue(null, "no polar"), wing.Forces[j].Fx, wing.Forces[j].Fy, wing.Forces[j].Fz,
            wing.Forces[j].Mx, wing.Forces[j].My, wing.Forces[j].Mz, wing.DownwashTrefftz[j], YLow: s.YInboard, YHigh: s.YOutboard)).ToArray();
        AnalysisRun run = ProjectionTests.Rehash(shell with { Strips = loads });
        double[] share = loads.Select(l => SectionForceModel.Compute(run, l, 0, 0, Units.Metric)!.InducedPerSpan).ToArray();
        // normalise at the centre pair, then compare the shape with sqrt(1 - eta^2)
        int centre = loads.Select((l, j) => (Eta: Math.Abs(l.Eta), j)).MinBy(t => t.Eta).j;
        double reference = share[centre] / Math.Sqrt(1 - loads[centre].Eta * loads[centre].Eta);
        double worstInner = 0, worstOuter = 0;
        for (int j = 0; j < loads.Length; j++)
        {
            double eta = Math.Abs(loads[j].Eta), deviation = Math.Abs(share[j] / (reference * Math.Sqrt(1 - eta * eta)) - 1);
            if (eta <= InnerEta) worstInner = Math.Max(worstInner, deviation);
            else if (eta <= OuterEta) worstOuter = Math.Max(worstOuter, deviation);
        }
        Console.WriteLine($"MEASURE SFV elliptic AR 8, 32 per half: worst |d'/(c sqrt(1-eta^2)) - 1| {worstInner:F4} for eta <= {InnerEta}, {worstOuter:F4} for {InnerEta} < eta <= {OuterEta}");
        Equal(true, worstInner <= InnerTolerance, $"d'(y) follows sqrt(1 - eta^2) within {InnerTolerance:P0} for eta <= {InnerEta}: {worstInner:P2}");
        Equal(true, worstOuter <= OuterTolerance, $"d'(y) follows sqrt(1 - eta^2) within {OuterTolerance:P0} for {InnerEta} < eta <= {OuterEta}: {worstOuter:P2}");
        // and the shares still add up to the wing's lifting-line D_i, here independently from the Trefftz plane
        double sum = 0;
        for (int j = 0; j < loads.Length; j++) sum += share[j] * Loads.StripWidth(run, loads[j]);
        double trefftz = 0;
        for (int j = 0; j < loads.Length; j++) trefftz += 0.5 * run.Water.Rho * wing.Gamma[j] * -wing.DownwashTrefftz[j] * (loads[j].YHigh!.Value - loads[j].YLow!.Value);
        Near(trefftz, sum, "the strip shares sum to the Trefftz-plane induced drag of the elliptic wing", 1e-9 * Math.Abs(trefftz));
    }

    private const double InnerEta = 0.5, OuterEta = 0.8, InnerTolerance = 0.03, OuterTolerance = 0.10;


    /// <summary>
    /// Ruling 131: the chordwise panel count nc decides x_cp, so its bias is measured, not argued. The cambered section (NACA 2412 shape)
    /// at nc 2, 4, 8, 16 and three angles of attack (low to moderate lift), strip at eta 0.5, 32 strips per half (at 8 or 16 the nc study is not yet in its asymptotic range: Cl_local itself drifts with nc). Records Cm c/4 and x_cp per
    /// nc and the observed order. A measurement for the next ruling, not a gate: the only assertions are that every value is finite and
    /// x_cp is defined. The table is in docs/proof/sfv/nc-convergence.md. Ring: readiness; cost about 7 s wall (12 lattice runs).
    /// </summary>
    private const int Span = 32;

    private static void ChordwiseConvergence()
    {
        byte[] source = CamberedSource();
        SectionTierResult tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], [], Fixture.Op(1), Fixture.Salt);
        int[] panels = [2, 4, 8, 16];
        foreach (double alpha in new[] { -1.0, 1.0, 4.0 })
        {
            var cm = new double[panels.Length];
            var xcp = new double[panels.Length];
            for (int i = 0; i < panels.Length; i++)
            {
                using var session = new AuthoringSession();
                session.Open(source, Fixture.Id(), true);
                RunSettings settings = Settings.Default with { NSpanPerHalf = Span, NChord = panels[i],
                    SectionEtas = [0d, 0.5, 1d], SectionXs = Settings.ChordXs(panels[i], "cosine") };
                AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, new ProductWingMethod(settings)), Fixture.Op(alpha));
                SectionForces f = SectionDisplay.Build(run, tier, source, 0.5, "r1", null, null, default, Units.Metric).Profile!.Forces!;
                double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
                cm[i] = f.CouplePerSpan / (q * f.ChordMeters * f.ChordMeters);
                xcp[i] = f.XcpOverC ?? double.NaN;
                Equal(true, double.IsFinite(cm[i]) && double.IsFinite(xcp[i]), $"nc {panels[i]}, alpha {alpha}: Cm c/4 and x_cp are finite");
                Console.WriteLine($"MEASURE SFV nc {panels[i],2} alpha {alpha,4}: Cl_local {f.ClLattice:F4} Cm c/4 {cm[i]:F5} x_cp/c {xcp[i]:F4}");
            }
            // observed order from three successive doublings, p = log2(|C(nc/2) - C(nc/4)| / |C(nc) - C(nc/2)|)
            double Order(double[] v, int k) => Math.Log2(Math.Abs(v[k - 1] - v[k - 2]) / Math.Abs(v[k] - v[k - 1]));
            Console.WriteLine($"MEASURE SFV alpha {alpha,4}: observed order Cm c/4 {Order(cm, 2):F2} (2,4,8) {Order(cm, 3):F2} (4,8,16); x_cp {Order(xcp, 2):F2} (2,4,8) {Order(xcp, 3):F2} (4,8,16)");
        }
    }

    private static void LatticeRun()
    {
        (AnalysisRun run, byte[] source) = CamberedRun();
        // (1) a consistency check, not verification: the strips' induced shares sum to the wing D_i (Loads.InducedDrag), both read from
        // the same Gamma and w_T, so it holds for any weights that add up. The distribution is checked in EllipticDistribution.
        double sum = 0;
        foreach (StripLoad strip in run.Strips)
            sum += SectionForceModel.Compute(run, strip, 0, 0, Units.Metric)!.InducedPerSpan * Loads.StripWidth(run, strip);
        double wing = Loads.InducedDrag(run).Value!.Value;
        Near(wing, sum, "sum of strip d' x width equals the wing D_i", 1e-9 * Math.Abs(wing));
        // (2) the shown strip from the real lattice: the centre of pressure is the Fz-weighted chordwise position about the strip's own
        // leading edge. Cross-check against the lattice solved again with the strips' own panel data: x_cp must lie near c/4..c/2 for a
        // thin section at small alpha, and every strip's table must read one model.
        SectionTierResult tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], [], Fixture.Op(1), Fixture.Salt);
        SectionView view = SectionDisplay.Build(run, tier, source, 0.5, "r1", null, null, default, Units.Metric);
        SectionForces f = view.Profile!.Forces!;
        Console.WriteLine($"MEASURE SFV cambered 2412 eta {f.Eta:F3}: cl_lattice {f.ClLattice:F4} x_cp {f.XcpOverC:F4} anchor {f.Anchor} L' {f.LiftPerSpan:F3} N/m couple {f.CouplePerSpan:F4} N.m/m d'_i {f.InducedPerSpan:F4} N/m");
        Equal(true, f.XcpOverC is > 0.1 and < 0.6, "the lattice x_cp of a thin cambered strip lies on the front half of the chord: " + f.XcpOverC);
        ResultGroup table = view.Groups.Single(g => g.Title == Labels.StripTableHeading);
        Equal(true, table.Rows.Any(r => r.Label == Labels.XcpRowLabel(run.Settings.NChord)), "Ruling 131: the x_cp row names the lattice, its panels and the forward bias");
        Equal(true, table.Rows.Any(r => r.Label == Labels.CoupleRowLabel(run.Settings.NChord)), "Ruling 131: the M' c/4 row names the lattice, its panels and the forward bias");
    }

    private static byte[] CamberedSource()
    {
        const int n = 40;
        var dat = new System.Text.StringBuilder("NACA 2412\n");
        for (int i = n; i >= 0; i--) Point(i, true);
        for (int i = 1; i <= n; i++) Point(i, false);
        ImportedProfile fitted = DatImport.Fit(DatImport.Parse(System.Text.Encoding.UTF8.GetBytes(dat.ToString())), "naca-2412");
        string dsl = System.Text.Encoding.UTF8.GetString(FoilSource.NewDefault());
        dsl = System.Text.RegularExpressions.Regex.Replace(dsl, @"(?s)  profiles \{.*?\n  \}\n  sections",
            "  profiles {\n" + fitted.ProfileBlock + "\n  }\n  sections");
        dsl = dsl.Replace("naca-0012", "naca-2412", StringComparison.Ordinal);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(dsl);
        if (!FoilSource.Parse(bytes).IsParsed) throw new InvalidOperationException("cambered wing source did not parse");
        return bytes;

        void Point(int i, bool upper)
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * i / n));
            double yc = x < 0.4 ? 0.02 / 0.16 * (0.8 * x - x * x) : 0.02 / 0.36 * (0.2 + 0.8 * x - x * x);
            double yt = 5 * 0.12 * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * Math.Pow(x, 3) - 0.1036 * Math.Pow(x, 4));
            dat.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x, yc + (upper ? yt : -yt)));
        }
    }
}
