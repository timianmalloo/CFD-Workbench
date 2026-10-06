using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Analysis.NeuralFoil;

namespace CfdWorkbench.Analysis;

/// <summary>Read-only inputs absent from the durable run. A missing verdict remains indeterminate.</summary>
public sealed record ProjectionContext(IReadOnlyList<StripVerdict>? Verdicts = null,
    IReadOnlyList<StationFrame>? Stations = null, double? RootThicknessRatio = null,
    RunIntegrity Integrity = RunIntegrity.Intact, AnalysisRun? PreviousCompleted = null,
    IReadOnlySet<string>? HiddenLayers = null, IReadOnlyList<Loads.Vec>? StripNormals = null, string? FeedUnavailable = null,
    byte[]? Source = null, SectionTierResult? SectionTier = null,
    RevisionLabel? Revision = null, string? HistoricalText = null);

/// <summary>Pure projection of the selected run into rows, chart points and layer data.</summary>
public static class AnalysisProjection
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    // COPY-250/251 (Ruling 101): a value cell that cannot be computed reads "Unavailable — <reason>", the reason being the code's own.
    private const string NoVerdictReason = "ANA-VERDICT-MISSING";
    private const string SurrogateLabel = "XFOIL-class surrogate; accuracy relative to XFOIL, not experiment";
    private enum VerdictState { Inside, Outside, Provisional, Indeterminate }

    public static AnalysisViewModel Build(AnalysisRun? run, CurrentInputs current, Units units) => Build(run, current, units, null);

    public static AnalysisViewModel Build(AnalysisRun? run, CurrentInputs current, Units units, ProjectionContext? context)
    {
        ArgumentNullException.ThrowIfNull(current);
        context ??= new ProjectionContext();
        if (run is null) return new(RunState.NoResult, "Analysis: no result", null, null,
            [new ResultGroup("Wing result", [Row("Result", Labels.NoResult)])], [], null);
        if (context.Integrity != RunIntegrity.Intact || RunRecord.ContentHash(run) != run.ContentHash)
            return new(RunState.Unavailable, "Analysis: Unavailable", null, null,
                [new ResultGroup("Wing result", [Row("Result", Labels.PayloadFailed, note: Labels.TamperedNote)])], [], run.RunKey);
        if (run.Outcome is RunOutcome.Failed failed)
        {
            string error = $"Analysis failed — {failed.Reason} ({failed.Code}). The previous result is kept as Historical.";
            if (context.PreviousCompleted is { } previous)
            {
                var old = Build(previous, current, units, context with { PreviousCompleted = null });
                // COPY-279: the chip reads Historical while a failed attempt shows the previous result.
                var kept = old.Groups.Select(group => group with
                {
                    Rows = [.. group.Rows.Select(row => row.Label == "Tier" ? row with { Value = Labels.HistoricalChip } : row)]
                }).ToArray();
                return old with { State = RunState.Failed, StatusText = "Analysis: Failed", ErrorCard = error,
                    Banner = "Historical — previous result", Groups = kept };
            }
            return new(RunState.Failed, "Analysis: Failed", null, error,
                [new ResultGroup("Wing result", [Row("Result", Labels.UnavailableBecause(failed.Reason), note: error)])], [], run.RunKey);
        }

        bool isCurrent = RunRecord.RecomputedKey(run) == Freshness.CurrentKey(current);
        string? banner = isCurrent ? null : context.HistoricalText ?? "Historical — " + string.Join(", ", Freshness.WhatChanged(run, current));
        double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
        double a = VortexLattice.ToRadians(run.Op.AlphaDeg);
        double lift = run.Strips.Sum(s => -s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a));
        double? cl = q > 0 && run.Reference.SRef > 0 ? lift / (q * run.Reference.SRef) : null;
        StripValue induced = Loads.InducedDrag(run);
        double? drag = induced.Value;
        string inducedReason = induced.UnavailableReason ?? "ANA-INDUCED-DRAG-MISSING-AREA";
        StripValue profile2 = Loads.ProfileDrag(run, 2), profile4 = Loads.ProfileDrag(run, 4);
        StripValue wing2 = Loads.WingDrag(run, 2), wing4 = Loads.WingDrag(run, 4);
        double? cdi = drag.HasValue && q > 0 && run.Reference.SRef > 0 ? drag / (q * run.Reference.SRef) : null;
        double? trefftzLift = run.Strips.Count > 0 && run.Strips.All(s => Width(run, s) > 0)
            ? run.Water.Rho * run.Op.Speed * run.Strips.Sum(s => s.Gamma * Width(run, s)) : null;
        double? clTrefftz = trefftzLift.HasValue && q > 0 && run.Reference.SRef > 0
            ? trefftzLift / (q * run.Reference.SRef) : null;
        double ar = run.Reference.SRef > 0 ? run.Reference.BRef * run.Reference.BRef / run.Reference.SRef : 0;
        double? e = clTrefftz.HasValue && cdi > 0 && ar > 0 ? Trefftz.Oswald(clTrefftz.Value, ar, cdi.Value) : null;
        var derived = OperatingPoints.Derive(run.Op, run.Water, run.Reference.CRef);
        SectionTierResult? section = context.SectionTier;
        if (section is null && context.Source is { } source)
        {
            try { section = SectionTier.Derive(run, source); }
            catch (ContractError) { }
        }
        bool depth = run.Op.HRef.HasValue;
        double? shallowHc = ShallowDepthOverChord(run, context.Stations);
        bool shallow = shallowHc.HasValue;
        var groups = new List<ResultGroup>();
        groups.Add(new("Wing result", [
            Row("Tier", isCurrent ? Labels.VlmChip : Labels.HistoricalChip),
            Row("CL", Val(cl, "0.000", q > 0 ? "ANA-REFERENCE-AREA-MISSING" : "ANA-SPEED-NOT-POSITIVE"), note: "Wing only · S_ref " + Num(run.Reference.SRef, "0.####") + " m²"),
            Row("Envelope", RunVerdict(context, run.Strips)),
            Row("CDi (Trefftz)", Val(cdi, "0.00000", inducedReason)),
            Row("e (computed)", Val(e, "0.000", "ANA-OSWALD-UNDEFINED"), note: EAdvisory(e, run.Settings)),
            Force("Lift L", lift, units), Force("Induced drag", drag, units, reason: inducedReason),
            WingDragRow(wing2, wing4, units, run.Settings.Polar is null),
            WingRatioRow(lift, wing2, wing4),
            Row("CL/CD", Loads.TotalDragReason),
            Row("Basis", "b " + Num(run.Reference.BRef, "0.###") + " m · moment datum: " + run.Reference.MomentDatum + " · " + Labels.BodyAxes)
        ]));
        groups.Add(new("Conditions", [
            Row("Speed", Num(units == Units.Imperial ? run.Op.Speed * 1.9438444924406 : run.Op.Speed, "0.00"), units == Units.Imperial ? "kn" : "m/s"),
            Row("α", Num(run.Op.AlphaDeg, "0.00"), "°"),
            Row("q", Derived(derived.Q, "0.##"), "Pa"), Row("Re_ref", Derived(derived.ReRef, "0.###E+0")),
            Row("h/c", Derived(derived.DepthOverChord, "0.00")), Row("Fr_h", Derived(derived.FroudeDepth, "0.00")),
            Row("σ", Derived(derived.Sigma, "0.00")),
            Row("V_crit", section?.Cavitation.CriticalSpeed is { } vcrit
                ? Num(units == Units.Imperial ? vcrit * 1.9438444924406 : vcrit, "0.###")
                : depth ? Labels.NoVcrit : "Unavailable — depth not set",
                section?.Cavitation.CriticalSpeed is null ? null : units == Units.Imperial ? "kn" : "m/s",
                section?.Cavitation.CriticalSpeed is null ? null : PanelMethod.ModelLabel +
                    (section.GoverningProvisional ? " · " + StripLoad.PanelUnderreadReason : ""))
        ]));
        var basis = new List<ResultRow>
        {
            Row("Confidence", "Computed estimate · Model uncertainty not quantified"),
            Row("Method", run.Method.Id + " " + run.Method.Version),
            Row("Lattice", run.Settings.NSpanPerHalf + " × " + run.Settings.NChord + " " + run.Settings.SpanSpacing + "/" + run.Settings.ChordSpacing,
                note: Labels.DefaultLattice(run.Settings) ? Labels.VerifiedLattice : Labels.NearFieldFlag),
            Row("Basis", Labels.FixedVlm(depth && !shallow)), Row("Not modelled", Labels.NotModelled(depth && !shallow))
        };
        if (depth)
        {
            double h = run.Op.HRef!.Value;
            if (shallowHc is { } hc) basis.Add(Row("Depth basis", $"Deep-water result; free surface not modelled (h/c = {Num(hc, "0.00")}, Fr_h = {Derived(derived.FroudeDepth, "0.00")}; effects measured below h/c 5)"));
            basis.Add(Row("Ventilation", "Static geometry; steady analysis cannot predict ventilation onset; onset is dynamic and hysteretic"));
            if (context.Stations is { Count: > 0 })
            {
                double tipDepth = h - context.Stations.Max(s => s.ElevationMeters);
                basis.Add(Row("Tip depth", tipDepth > 0 ? Num(tipDepth, "0.###") : "Unavailable — surface piercing", tipDepth > 0 ? "m" : null));
            }
            else basis.Add(Row("Tip depth", Labels.TipDepthMissing));
        }
        groups.Add(new("Labels", basis));

        double? centre = CentreOfLift(run, a), rootMoment = RootBending(run);
        groups.Add(new("Loads", [
            Row("Safety", "Loads are hydrodynamic estimates. Not a structural assessment. Strength, stiffness and fatigue are not evaluated."),
            Force("Lift (Wing only)", lift, units, "near field, Σ strips"),
            Row("Centre of lift, half span", Val(centre, "0.###", "ANA-CENTRE-OF-LIFT-UNDEFINED"), "m", "from the root plane"),
            Row("Root bending moment", Val(rootMoment, "0.###", "ANA-ROOT-MOMENT-MISSING-WIDTH"), "N·m", Labels.RootMoment),
            Row("Wing loading L/S_ref", run.Reference.SRef > 0 ? Num(lift / run.Reference.SRef / 1000, "0.###") : Labels.UnavailableBecause("ANA-REFERENCE-AREA-MISSING"), "kPa"),
            Row("Moment about attachment point", Loads.AttachmentReason),
            DragBandRow("Profile drag", profile2, profile4, units, run.Settings.Polar is null ? Labels.NoPolar : null,
                SurrogateLabel),
            WingDragRow(wing2, wing4, units, run.Settings.Polar is null),
            WingRatioRow(lift, wing2, wing4),
            Row("Structural", "Structural: Not assessed", note: Labels.StructuralList),
            Row("t/c (root)", context.RootThicknessRatio.HasValue ? Num(context.RootThicknessRatio.Value * 100, "0.#") : Labels.ThicknessMissing,
                context.RootThicknessRatio.HasValue ? "%" : null),
            Row("EI/EI_ref ∝ (t/c)³", context.RootThicknessRatio.HasValue ? Num(Math.Pow(context.RootThicknessRatio.Value / 0.10, 3), "0.###") : Labels.ThicknessMissing)
        ]));
        groups.Add(new("Strips", run.Strips.Select(s => Row("Strip " + s.J + " · η " + Num(s.Eta, "0.###"),
            "Cl_local " + Num(s.ClLocal, "0.###") + " · α_eff " + Num(s.AlphaEff, "0.##") + "° · Re_local " + Num(s.ReLocal, "0.###E+0"),
            note: VerdictText(s, context))).ToArray()));
        groups.Add(new("Section (2D)", SectionRows(section)));
        var provenance = new List<ResultRow>
        {
            Row("Run", run.RunId + " · " + run.Outcome.GetType().Name + " · " + run.RunKey),
            Row("Method", run.Method.Id + " " + run.Method.Version + " · " + run.Settings.NSpanPerHalf + " × " + run.Settings.NChord),
            Row("Inputs", "revision " + (context.Revision is { } revision ? "r" + revision.Ordinal.ToString(Inv) : run.Inputs.AcceptedId) +
                " · surface " + run.Inputs.SurfaceHash + " · profiles " + string.Join(", ", run.Inputs.ProfileHashes) +
                " · evaluator " + run.Inputs.Evaluator),
            Row("Water", Num(run.Water.SalinityGPerKg, "0.#####") + " g/kg · " + Num(run.Water.TemperatureC, "0.##") +
                " °C · ρ " + Num(run.Water.Rho, "0.###") + " kg/m³ · ν " + Num(run.Water.Nu, "0.#####E+0") +
                " m²/s · p_v " + Num(run.Water.Pv / 1000, "0.####") + " kPa · " + run.Water.Source),
            Row("Operating point", Num(run.Op.Speed, "0.###") + " m/s · p_atm " + Num(run.Op.PAtm / 1000, "0.###") +
                " kPa · α " + Num(run.Op.AlphaDeg, "0.##") + "° · datum " + run.Op.Datum),
            Row("Run key", RunRecord.RecomputedKey(run)), Row("Content hash", RunRecord.ContentHash(run)),
            Row("Water table hash", run.Water.TableHash), Row("Settings hash", run.SettingsHash)
        };
        if (banner is not null) provenance.Add(Row("Changed since", banner));
        groups.Add(new("Provenance", provenance));
        return new(isCurrent ? RunState.Current : RunState.Historical, isCurrent ? "Analysis: Current" : "Analysis: Historical",
            banner, null, groups, Layers(run, context, rootMoment), run.RunKey)
        {
            Loading = Loading(run, cl, a), StripDetails = StripDetails(run, context, a),
            SectionTier = section,
            PolarConsistency = section?.PolarConsistency,
            WingDragNcrit2 = wing2, WingDragNcrit4 = wing4,
            FreeSurface = run.Op.HRef is { } correctionDepth ? FreeSurfaceCorrection.Evaluate(lift, wing2.Value,
                wing4.Value,
                run.Strips.Count > 0 ? run.Strips.Sum(strip => strip.My) : null,
                correctionDepth, run.Reference.CRef, run.Op.Speed,
                run.Op.Speed * run.Reference.CRef / run.Water.Nu, run.Op.AlphaDeg) : null
        };
    }

    private static IReadOnlyList<ResultRow> SectionRows(SectionTierResult? section)
    {
        if (section is null) return [Row("Cl, Cd, Cm, x_tr", Labels.NoPolar), Row("Cp_min", Labels.SectionCp)];
        SectionStationResult station = section.Stations.MinBy(item => Math.Abs(item.Eta - section.GoverningEta))!;
        string stationLabel = "η " + Num(station.Eta, "0.###");
        string provisional = section.GoverningProvisional ? " · " + StripLoad.PanelUnderreadReason : "";
        if (station.EstimatorAvailabilityCode is { } unavailable)
            return [Row("Cl", unavailable), Row("Cm_c/4", unavailable), Row("α_L0", unavailable),
                Row("Cd", unavailable), Row("Cp_min", unavailable), Row("Cavitation", unavailable)];
        var rows = new List<ResultRow>
        {
            Row("Cl", Num(station.Estimate.Cl, "0.###"), note: PanelMethod.ModelLabel),
            Row("Cm_c/4", Num(station.Estimate.CmQuarter, "0.###"), note: PanelMethod.ModelLabel),
            Row("α_L0", Num(station.Estimate.AlphaL0Deg, "0.###"), "°", PanelMethod.ModelLabel),
            Row("ANA-SECTION-ITTC1957-BOUND", Num(station.Estimate.CdTurbulentBound, "0.#####"),
                note: "ITTC-1957 fully turbulent bound; no lift-dependent profile drag"),
            Row("Cp_min", Num(station.Estimate.Panel.CpMin, "0.###"),
                note: PanelMethod.ModelLabel + " · " + stationLabel + provisional),
            Row("N", station.Estimate.Panel.StationCount.ToString(Inv), note: PanelMethod.ModelLabel),
            Row("η", Num(station.Eta, "0.###"), note: PanelMethod.ModelLabel),
            Row("Cavitation", section.Cavitation.Reason == Cavitation.DepthNotSet ? "Unavailable — depth not set" :
                section.Cavitation.Reason ?? section.Cavitation.State.ToString(),
                note: PanelMethod.ModelLabel + " · " + section.Cavitation.ScreenText + " · " + stationLabel + provisional)
        };
        if (section.PolarNcrit2 is not null || section.PolarNcrit4 is not null ||
            section.PolarReason2 is not null || section.PolarReason4 is not null)
        {
            rows.Add(Row("Ncrit 2", PolarText(section.PolarNcrit2, section.PolarReason2), note: SurrogateLabel));
            rows.Add(Row("Ncrit 4", PolarText(section.PolarNcrit4, section.PolarReason4), note: SurrogateLabel));
            PolarResult? polar = section.PolarNcrit2 ?? section.PolarNcrit4;
            if (polar is not null)
            {
                rows.Add(Row("CST residual", Num(polar.CstResidualMax, "0.#####E+0"), note: SurrogateLabel));
                rows.Add(Row("analysis_confidence", Val(polar.Sample.Confidence, "0.###"),
                    note: SurrogateLabel + (polar.LowConfidence ? " · ANA-POLAR-LOW-CONFIDENCE" : "")));
            }
        }
        return rows;
    }

    private static string PolarText(PolarResult? result, string? reason)
    {
        if (reason is not null) return reason;
        if (result is null) return Labels.NoPolar;
        PolarSample sample = result.Sample;
        return "Cl " + Val(sample.Cl, "0.###") + " · Cd " + Val(sample.Cd, "0.#####") +
            " · Cm " + Val(sample.Cm, "0.###") + " · x_tr " + Val(sample.XtrUpper, "0.###") +
            "/" + Val(sample.XtrLower, "0.###");
    }

    private static ResultRow DragBandRow(string label, StripValue n2, StripValue n4, Units units,
        string? legacyReason, string? tierNote = null, string? reasonLine = null)
    {
        if (n2.Value is not { } low || n4.Value is not { } high)
            return Row(label, legacyReason ?? n2.UnavailableReason ?? n4.UnavailableReason ?? "ANA-DRAG-UNAVAILABLE");
        double factor = units == Units.Imperial ? 4.4482216152605 : 1;
        string? note = n2.FlagCode == "ANA-POLAR-LOW-CONFIDENCE" || n4.FlagCode == "ANA-POLAR-LOW-CONFIDENCE"
            ? string.Join(" · ", new[] { tierNote, "ANA-POLAR-LOW-CONFIDENCE" }.Where(part => part is not null))
            : tierNote;
        if (reasonLine is not null) note = note is null ? reasonLine : note + "\n" + reasonLine;
        return Row(label, Num(Math.Min(low, high) / factor, "0.###") + "–" +
            Num(Math.Max(low, high) / factor, "0.###"),
            units == Units.Imperial ? "lbf" : "N", note);
    }

    /// <summary>Ruling 109: the one Drag (Wing only) row (COPY-354..356). The craft total is never shown; CL/CD for the craft is Unavailable.</summary>
    private static ResultRow WingDragRow(StripValue n2, StripValue n4, Units units, bool noPolar) =>
        DragBandRow(Labels.WingDragLabel, n2, n4, units, noPolar ? Loads.TotalDragReason : null,
            Labels.WingDragNote + " · " + SurrogateLabel, Labels.WingDragNotIncluded);

    /// <summary>Shallowest h/c over the root reference chord and every station; null when depth is unset or every h/c is at least 5 (Ruling 101 3a).</summary>
    private static double? ShallowDepthOverChord(AnalysisRun run, IReadOnlyList<StationFrame>? stations)
    {
        if (run.Op.HRef is not { } h) return null;
        double least = run.Reference.CRef > 0 ? h / run.Reference.CRef : double.PositiveInfinity;
        if (stations is not null)
            foreach (StationFrame station in stations)
                if (station.ChordMeters > 0) least = Math.Min(least, (h - station.ElevationMeters) / station.ChordMeters);
        return least < Labels.DeepWaterHc ? least : null;
    }

    private static ResultRow WingRatioRow(double lift, StripValue n2, StripValue n4)
    {
        if (n2.Value is not > 0 || n4.Value is not > 0)
            return Row("Wing-only CL/CD", Labels.UnavailableBecause("ANA-WING-RATIO-UNAVAILABLE"));
        double a = lift / n2.Value.Value, b = lift / n4.Value.Value;
        return Row("Wing-only CL/CD", Num(Math.Min(a, b), "0.###") + "–" + Num(Math.Max(a, b), "0.###"),
            note: "ANA-WING-ONLY-RATIO · " + SurrogateLabel +
                (n2.FlagCode == "ANA-POLAR-LOW-CONFIDENCE" || n4.FlagCode == "ANA-POLAR-LOW-CONFIDENCE"
                    ? " · ANA-POLAR-LOW-CONFIDENCE" : ""));
    }

    public static ResultGroup StripAt(AnalysisViewModel view, double eta)
    {
        if (view.StripDetails.Count == 0) return new("Strip result", [Row("Result", Labels.NoResult)]);
        StripDetail nearest = view.StripDetails.MinBy(strip => Math.Abs(strip.Eta - eta))!;
        return new(Labels.StripHeader + " " + Num(nearest.Eta, "0.###"), nearest.Rows);
    }

    private static IReadOnlyList<StripDetail> StripDetails(AnalysisRun run, ProjectionContext context, double alpha)
    {
        var details = new List<StripDetail>();
        foreach (var s in run.Strips)
        {
            double width = Width(run, s);
            double localLift = -s.Fx * Math.Sin(alpha) + s.Fz * Math.Cos(alpha);
            details.Add(new(s.Eta, [
                Row("Cl_local", Num(s.ClLocal, "0.###")), Row("α_eff", Num(s.AlphaEff, "0.##"), "°"),
                Row("Re_local", Num(s.ReLocal, "0.###E+0")),
                Row("Lift / span", width > 0 ? Num(localLift / width, "0.###") : Labels.StripWidthMissing, width > 0 ? "N/m" : null),
                Row("Envelope (this strip)", VerdictText(s, context),
                    note: s.ProvisionalReason == StripLoad.PanelUnderreadReason ? StripLoad.PanelUnderreadReason : null),
                Row("Polar Re range", s.ProvisionalReason == StripLoad.TipProvisionalReason ? Labels.TipNotJudged :
                    s.CdNcrit2.UnavailableReason ?? (s.CdNcrit2.Value.HasValue
                        ? Num(NeuralFoilPolarSource.ReynoldsMin, "0.###E+0") + "–" +
                            Num(NeuralFoilPolarSource.ReynoldsMax, "0.###E+0") : Labels.NoPolar),
                    note: s.CdNcrit2.Value.HasValue ? SurrogateLabel : null),
                Row("cd (profile)", s.CdNcrit2.Value.HasValue ? Num(s.CdNcrit2.Value.Value, "0.#####") : s.CdNcrit2.UnavailableReason ?? Labels.NoPolar,
                    note: s.CdNcrit2.Value.HasValue ? SurrogateLabel + (s.CdNcrit2.FlagCode is { } code ? " · " + code : "") : null),
                Row("Not modelled", Labels.NotModelled(run.Op.HRef.HasValue))
            ]));
        }
        return details;
    }

    private static VerdictState State(StripLoad strip, StripVerdict? verdict)
    {
        if ((strip.Provisional && strip.ProvisionalReason == StripLoad.TipProvisionalReason) || verdict?.Provisional == true)
            return VerdictState.Provisional;
        if (verdict is null) return VerdictState.Indeterminate;
        if (verdict.Inside) return VerdictState.Inside;
        return verdict.Exceeded.Count > 0 ? VerdictState.Outside : VerdictState.Indeterminate;
    }

    private static StripVerdict? At(IReadOnlyList<StripVerdict>? verdicts, StripLoad strip) =>
        verdicts is { } v && strip.J >= 0 && strip.J < v.Count ? v[strip.J] : null;

    private static string NoVerdict(ProjectionContext context) => context.FeedUnavailable ?? Labels.UnavailableBecause(NoVerdictReason);

    private static string VerdictText(StripLoad strip, ProjectionContext context)
    {
        IReadOnlyList<StripVerdict>? verdicts = context.Verdicts;
        StripVerdict? verdict = At(verdicts, strip);
        return State(strip, verdict) switch
        {
            VerdictState.Provisional => Labels.TipNotJudged,
            VerdictState.Indeterminate => verdict?.Text ?? NoVerdict(context),
            _ => verdict!.Text
        };
    }

    private static string RunVerdict(ProjectionContext context, IReadOnlyList<StripLoad> strips)
    {
        IReadOnlyList<StripVerdict>? verdicts = context.Verdicts;
        // Only the outermost strip of each half reads "Not judged — tip strip" (Ruling 78); a missing verdict is Unavailable.
        if (verdicts is not null && verdicts.Count != strips.Count) verdicts = null;
        var judged = new List<StripVerdict>(strips.Count);
        int provisional = 0, indeterminate = 0;
        foreach (StripLoad strip in strips)
        {
            StripVerdict? verdict = At(verdicts, strip);
            switch (State(strip, verdict))
            {
                case VerdictState.Provisional:
                    provisional++;
                    judged.Add(new StripVerdict(false, [], Labels.TipNotJudged) { Provisional = true });
                    break;
                case VerdictState.Indeterminate:
                    indeterminate++;
                    break;
                default:
                    judged.Add(verdict!);
                    break;
            }
        }
        int decided = judged.Count(v => !v.Provisional);
        string sentence = decided > 0 ? MethodRecord.JudgeRun(judged) : indeterminate > 0 ? NoVerdict(context) : Labels.TipNotJudged;
        if (provisional > 0 && (decided > 0 || indeterminate > 0)) sentence += $"; {provisional} {Labels.TipNotJudged}";
        if (indeterminate > 0 && decided > 0) sentence += $"; {indeterminate} strips: {NoVerdict(context)}";
        return sentence;
    }

    private static double Width(AnalysisRun run, StripLoad strip) => Loads.StripWidth(run, strip);
    private static double? RootBending(AnalysisRun run) => run.Strips.Count == 0 ? null :
        run.Strips.Where(s => s.Y >= 0).All(s => Width(run, s) > 0)
            ? run.Water.Rho * run.Op.Speed * run.Strips.Where(s => s.Y >= 0).Sum(s => s.Gamma * s.Y * Width(run, s)) : null;
    private static double? CentreOfLift(AnalysisRun run, double a)
    {
        double weighted = 0, total = 0;
        foreach (var s in run.Strips.Where(s => s.Y >= 0))
        {
            double local = -s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a);
            weighted += local * s.Y; total += local;
        }
        return total == 0 ? null : weighted / total;
    }
    private static IReadOnlyList<LoadingPoint> Loading(AnalysisRun run, double? cl, double a) =>
        run.Strips.Where(s => s.Y >= 0).OrderBy(s => s.Eta).Select(s => new LoadingPoint(s.Eta,
            run.Reference.CRef > 0 ? s.ClLocal * s.Chord / run.Reference.CRef : null,
            cl * 4 / Math.PI * Math.Sqrt(Math.Max(0, 1 - s.Eta * s.Eta)), s.AlphaEff,
            Width(run, s) > 0 ? (-s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a)) / Width(run, s) : null)).ToArray();
    private static IReadOnlyList<LayerData> Layers(AnalysisRun run, ProjectionContext context, double? rootMoment)
    {
        bool Shown(string id) => context.HiddenLayers?.Contains(id) != true;
        double max = run.Strips.Count == 0 ? 0 : run.Strips.Max(s => Math.Abs(s.Gamma));
        string key = run.RunKey.Length >= 12 ? run.RunKey[..12] : run.RunKey;
        var layers = new List<LayerData>
        {
            new LayerData("plan-gamma", "Γ per strip", Shown("plan-gamma"), "Γ per strip · batlow 1.0 · 0–" + Num(max, "0.###") + " m²/s · run " + key, "strips-table")
            {
                Samples = run.Strips.Select(s => new LayerSample(s.Eta, s.Y, s.Gamma, null,
                    State(s, At(context.Verdicts, s)) == VerdictState.Outside, s.Provisional)
                    { Verdict = VerdictText(s, context), YLow = s.YLow, YHigh = s.YHigh }).ToArray(),
                Note = "Outside strips have dashed outlines and a text count."
            },
            new LayerData("strip-lift", "Lift per strip", Shown("strip-lift"), "Lift per strip · N/m · " + Labels.BodyAxes + " · " + Labels.VlmChip, "loads-table")
            {
                Samples = run.Strips.Select(s => new LayerSample(s.Eta, s.Y,
                    Width(run, s) > 0 ? s.Fz / Width(run, s) : null,
                    Width(run, s) > 0 ? new Loads.Vec(s.Fx / Width(run, s), s.Fy / Width(run, s), s.Fz / Width(run, s)) : null,
                    false, s.Provisional) { Normal = context.StripNormals is { } n && s.J >= 0 && s.J < n.Count ? n[s.J] : null }).ToArray(),
                Note = run.Strips.Any(s => Width(run, s) <= 0) ? Labels.StripWidthMissing : context.FeedUnavailable
            }
        };
        if (rootMoment.HasValue) layers.Add(new LayerData("root-moment", "Root moment arc", Shown("root-moment"),
            Labels.RootMoment + " · " + Num(rootMoment.Value, "0.###") + " N·m", "loads-table")
        {
            Samples = [new LayerSample(0, 0, rootMoment.Value, new Loads.Vec(rootMoment.Value, 0, 0), false, false)]
        });
        if (run.Op.HRef.HasValue && context.Stations is { Count: > 0 })
            // Margin = HRef − Elevation, so each view's z = Elevation + Margin recovers the same free-surface HRef.
            layers.Add(new LayerData("depth-band", "Free surface and tip depth", Shown("depth-band"),
                "h_ref " + Num(run.Op.HRef.Value, "0.###") + " m · datum " + run.Op.Datum, "conditions-table")
            { Samples = context.Stations.Select(s => new LayerSample(s.Eta, s.SpanMeters,
                run.Op.HRef.Value - s.ElevationMeters, null, false, false) { Elevation = s.ElevationMeters }).ToArray() });
        return layers;
    }
    private static string? EAdvisory(double? e, RunSettings settings) => e is null ? null
        : e < 0.85 ? Labels.EBelowBand
        : e > 1 && e <= 1.02 && Labels.DefaultLattice(settings) ? Labels.EAboveOne
        : e > 1.02 || (e > 1 && !Labels.DefaultLattice(settings)) ? Labels.EAboveLatticeCheck : null;
    private static string Derived(DerivedValue value, string format) => value.Value.HasValue ? Num(value.Value.Value, format)
        : value.Reason == DerivedReason.DepthNotSet ? "Unavailable — depth not set" : "Undefined — speed ≤ 0";
    private static string Val(double? value, string format, string reason = "ANA-POLAR-VALUE-MISSING") =>
        value.HasValue && double.IsFinite(value.Value) ? Num(value.Value, format) : Labels.UnavailableBecause(reason);
    private static string Num(double value, string format) => value.ToString(format, Inv);
    private static ResultRow Row(string label, string value, string? unit = null, string? note = null) => new(label, value, unit, note);
    private static ResultRow Force(string label, double? value, Units units, string? note = null, string? reason = null) =>
        Row(label, Val(value / (units == Units.Imperial ? 4.4482216152605 : 1), "0.###", reason ?? "ANA-FORCE-NOT-FINITE"), units == Units.Imperial ? "lbf" : "N", note);
}
