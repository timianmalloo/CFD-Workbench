using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Read-only inputs absent from the durable run. A missing verdict remains indeterminate.</summary>
public sealed record ProjectionContext(IReadOnlyList<StripVerdict>? Verdicts = null,
    IReadOnlyList<StationFrame>? Stations = null, double? RootThicknessRatio = null,
    RunIntegrity Integrity = RunIntegrity.Intact, AnalysisRun? PreviousCompleted = null,
    IReadOnlySet<string>? HiddenLayers = null, IReadOnlyList<Loads.Vec>? StripNormals = null, string? FeedUnavailable = null);

/// <summary>Pure projection of the selected run into rows, chart points and layer data.</summary>
public static class AnalysisProjection
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    // simplify: the generic "Unavailable" of COPY-210..229 stands in until a copy ruling names a no-verdict string.
    private const string VerdictUnavailable = "Unavailable";
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
                [new ResultGroup("Wing result", [Row("Result", Labels.PayloadFailed)])], [], run.RunKey);
        if (run.Outcome is RunOutcome.Failed failed)
        {
            string error = $"Analysis failed — {failed.Reason} ({failed.Code}). The previous result is kept as Historical.";
            if (context.PreviousCompleted is { } previous)
            {
                var old = Build(previous, current, units, context with { PreviousCompleted = null });
                return old with { State = RunState.Failed, StatusText = "Analysis: Failed", ErrorCard = error,
                    Banner = "Historical — previous result" };
            }
            return new(RunState.Failed, "Analysis: Failed", null, error,
                [new ResultGroup("Wing result", [Row("Result", "Unavailable", note: error)])], [], run.RunKey);
        }

        bool isCurrent = RunRecord.RecomputedKey(run) == Freshness.CurrentKey(current);
        string? banner = isCurrent ? null : "Historical — " + string.Join(", ", Freshness.WhatChanged(run, current));
        double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
        double a = VortexLattice.ToRadians(run.Op.AlphaDeg);
        double lift = run.Strips.Sum(s => -s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a));
        double? cl = q > 0 && run.Reference.SRef > 0 ? lift / (q * run.Reference.SRef) : null;
        double? drag = TrefftzDrag(run);
        double? cdi = drag.HasValue && q > 0 && run.Reference.SRef > 0 ? drag / (q * run.Reference.SRef) : null;
        double? trefftzLift = run.Strips.Count > 0 && run.Strips.All(s => Width(run, s) > 0)
            ? run.Water.Rho * run.Op.Speed * run.Strips.Sum(s => s.Gamma * Width(run, s)) : null;
        double? clTrefftz = trefftzLift.HasValue && q > 0 && run.Reference.SRef > 0
            ? trefftzLift / (q * run.Reference.SRef) : null;
        double ar = run.Reference.SRef > 0 ? run.Reference.BRef * run.Reference.BRef / run.Reference.SRef : 0;
        double? e = clTrefftz.HasValue && cdi > 0 && ar > 0 ? Trefftz.Oswald(clTrefftz.Value, ar, cdi.Value) : null;
        var derived = OperatingPoints.Derive(run.Op, run.Water, run.Reference.CRef);
        bool depth = run.Op.HRef.HasValue;
        var groups = new List<ResultGroup>();
        groups.Add(new("Wing result", [
            Row("Tier", Labels.VlmChip),
            Row("CL", Val(cl, "0.000"), note: "Wing only · S_ref " + Num(run.Reference.SRef, "0.####") + " m²"),
            Row("Envelope", RunVerdict(context.Verdicts, run.Strips)),
            Row("CDi (Trefftz)", Val(cdi, "0.00000")),
            Row("e (computed)", Val(e, "0.000"), note: EAdvisory(e, run.Settings)),
            Force("Lift L", lift, units), Force("Induced drag", drag, units),
            Row("Total drag", Loads.TotalDragReason),
            Row("CL/CD", "Unavailable — total drag missing"),
            Row("Basis", "b " + Num(run.Reference.BRef, "0.###") + " m · moment datum: " + run.Reference.MomentDatum + " · " + Labels.BodyAxes)
        ]));
        groups.Add(new("Conditions", [
            Row("Speed", Num(units == Units.Imperial ? run.Op.Speed * 1.9438444924406 : run.Op.Speed, "0.00"), units == Units.Imperial ? "kn" : "m/s"),
            Row("α", Num(run.Op.AlphaDeg, "0.00"), "°"),
            Row("q", Derived(derived.Q, "0.##"), "Pa"), Row("Re_ref", Derived(derived.ReRef, "0.###E+0")),
            Row("h/c", Derived(derived.DepthOverChord, "0.00")), Row("Fr_h", Derived(derived.FroudeDepth, "0.00")),
            Row("σ", Derived(derived.Sigma, "0.00")),
            Row("V_crit", depth ? Labels.NoVcrit : "Unavailable — depth not set")
        ]));
        var basis = new List<ResultRow>
        {
            Row("Confidence", "Computed estimate · Model uncertainty not quantified"),
            Row("Method", run.Method.Id + " " + run.Method.Version),
            Row("Lattice", run.Settings.NSpanPerHalf + " × " + run.Settings.NChord + " " + run.Settings.SpanSpacing + "/" + run.Settings.ChordSpacing,
                note: Labels.DefaultLattice(run.Settings) ? Labels.VerifiedLattice : Labels.NearFieldFlag),
            Row("Basis", Labels.FixedVlm(depth)), Row("Not modelled", Labels.NotModelled(depth))
        };
        if (depth)
        {
            double h = run.Op.HRef!.Value, hc = h / run.Reference.CRef;
            if (hc < 5) basis.Add(Row("Depth basis", $"Deep-water result; free surface not modelled (h/c = {Num(hc, "0.00")}, Fr_h = {Derived(derived.FroudeDepth, "0.00")}; effects measured below h/c 5)"));
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
            Row("Centre of lift, half span", Val(centre, "0.###"), "m", "from the root plane"),
            Row("Root bending moment", Val(rootMoment, "0.###"), "N·m", Labels.RootMoment),
            Row("Wing loading L/S_ref", run.Reference.SRef > 0 ? Num(lift / run.Reference.SRef / 1000, "0.###") : "Unavailable", "kPa"),
            Row("Moment about attachment point", Loads.AttachmentReason), Row("Profile drag", Labels.NoPolar),
            Row("Total drag", Loads.TotalDragReason),
            Row("Structural", "Structural: Not assessed", note: Labels.StructuralList),
            Row("t/c (root)", context.RootThicknessRatio.HasValue ? Num(context.RootThicknessRatio.Value * 100, "0.#") : Labels.ThicknessMissing,
                context.RootThicknessRatio.HasValue ? "%" : null),
            Row("EI/EI_ref ∝ (t/c)³", context.RootThicknessRatio.HasValue ? Num(Math.Pow(context.RootThicknessRatio.Value / 0.10, 3), "0.###") : Labels.ThicknessMissing)
        ]));
        groups.Add(new("Strips", run.Strips.Select(s => Row("Strip " + s.J + " · η " + Num(s.Eta, "0.###"),
            "Cl_local " + Num(s.ClLocal, "0.###") + " · α_eff " + Num(s.AlphaEff, "0.##") + "° · Re_local " + Num(s.ReLocal, "0.###E+0"),
            note: VerdictText(s, context.Verdicts))).ToArray()));
        groups.Add(new("Section (2D)", [Row("Cl, Cd, Cm, x_tr", Labels.NoPolar), Row("Cp_min", Labels.SectionCp)]));
        groups.Add(new("Provenance", [Row("Run key", RunRecord.RecomputedKey(run)), Row("Content hash", RunRecord.ContentHash(run)),
            Row("Water table hash", run.Water.TableHash), Row("Settings hash", run.SettingsHash)]));
        return new(isCurrent ? RunState.Current : RunState.Historical, isCurrent ? "Analysis: Current" : "Analysis: Historical",
            banner, null, groups, Layers(run, context, rootMoment), run.RunKey)
        {
            Loading = Loading(run, cl, a), StripDetails = StripDetails(run, context, a)
        };
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
                Row("cd (profile)", s.CdNcrit2.Value.HasValue ? Num(s.CdNcrit2.Value.Value, "0.#####") : s.CdNcrit2.UnavailableReason ?? Labels.NoPolar),
                Row("Envelope (this strip)", VerdictText(s, context.Verdicts)),
                Row("Polar Re range", Labels.NoPolar), Row("Not modelled", Labels.NotModelled(run.Op.HRef.HasValue))
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

    private static string VerdictText(StripLoad strip, IReadOnlyList<StripVerdict>? verdicts)
    {
        StripVerdict? verdict = At(verdicts, strip);
        return State(strip, verdict) switch
        {
            VerdictState.Provisional => Labels.TipNotJudged,
            VerdictState.Indeterminate => verdict?.Text ?? VerdictUnavailable,
            _ => verdict!.Text
        };
    }

    private static string RunVerdict(IReadOnlyList<StripVerdict>? verdicts, IReadOnlyList<StripLoad> strips)
    {
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
        string sentence = decided > 0 ? MethodRecord.JudgeRun(judged) : indeterminate > 0 ? VerdictUnavailable : Labels.TipNotJudged;
        if (provisional > 0 && (decided > 0 || indeterminate > 0)) sentence += $"; {provisional} {Labels.TipNotJudged}";
        if (indeterminate > 0 && decided > 0) sentence += $"; {indeterminate} strips: {VerdictUnavailable}";
        return sentence;
    }

    private static double? TrefftzDrag(AnalysisRun run) => run.Strips.Count == 0 || run.Strips.Any(s => Width(run, s) <= 0) ? null :
        0.5 * run.Water.Rho * run.Strips.Sum(s => s.Gamma * -s.DownwashTrefftz * Width(run, s));
    private static double Width(AnalysisRun run, StripLoad strip)
    {
        if (strip.YLow.HasValue != strip.YHigh.HasValue) return 0;
        if (strip.YLow.HasValue && strip.YHigh.HasValue) return strip.YHigh.Value - strip.YLow.Value;
        if (run.Strips.Count != 2 * run.Settings.NSpanPerHalf) return 0;
        int j = strip.J;
        int n = run.Settings.NSpanPerHalf, total = 2 * n;
        if (j < 0 || j >= total) return 0;
        double Edge(int i) => run.Settings.SpanSpacing == "cosine" ? -Math.Cos(Math.PI * i / total) : -1 + 2.0 * i / total;
        return (Edge(j + 1) - Edge(j)) * run.Reference.BRef / 2;
    }
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
                    { Verdict = VerdictText(s, context.Verdicts) }).ToArray(),
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
            layers.Add(new LayerData("depth-band", "Free surface and tip depth", Shown("depth-band"),
                "h_ref " + Num(run.Op.HRef.Value, "0.###") + " m · datum " + run.Op.Datum, "conditions-table")
            { Samples = context.Stations.Select(s => new LayerSample(s.Eta, s.SpanMeters,
                run.Op.HRef.Value - s.ElevationMeters, null, false, false)).ToArray() });
        return layers;
    }
    private static string? EAdvisory(double? e, RunSettings settings) => e is null ? null
        : e < 0.85 ? Labels.EBelowBand
        : e > 1 && e <= 1.02 && Labels.DefaultLattice(settings) ? Labels.EAboveOne
        : e > 1.02 || (e > 1 && !Labels.DefaultLattice(settings)) ? Labels.EAboveLatticeCheck : null;
    private static string Derived(DerivedValue value, string format) => value.Value.HasValue ? Num(value.Value.Value, format)
        : value.Reason == DerivedReason.DepthNotSet ? "Unavailable — depth not set" : "Undefined — speed ≤ 0";
    private static string Val(double? value, string format) => value.HasValue && double.IsFinite(value.Value) ? Num(value.Value, format) : "Unavailable";
    private static string Num(double value, string format) => value.ToString(format, Inv);
    private static ResultRow Row(string label, string value, string? unit = null, string? note = null) => new(label, value, unit, note);
    private static ResultRow Force(string label, double? value, Units units, string? note = null) =>
        Row(label, Val(value / (units == Units.Imperial ? 4.4482216152605 : 1), "0.###"), units == Units.Imperial ? "lbf" : "N", note);
}
