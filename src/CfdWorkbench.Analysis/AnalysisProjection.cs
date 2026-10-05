using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Read-only inputs absent from the durable run. A missing verdict remains indeterminate.</summary>
public sealed record ProjectionContext(IReadOnlyList<StripVerdict>? Verdicts = null,
    IReadOnlyList<StationFrame>? Stations = null, double? RootThicknessRatio = null,
    RunIntegrity Integrity = RunIntegrity.Intact, AnalysisRun? PreviousCompleted = null);

/// <summary>Pure projection of the selected run into rows, chart points and layer data.</summary>
public static class AnalysisProjection
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

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
        double a = run.Op.AlphaDeg * Math.PI / 180;
        double lift = run.Strips.Sum(s => -s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a));
        double? cl = q > 0 && run.Reference.SRef > 0 ? lift / (q * run.Reference.SRef) : null;
        double? drag = TrefftzDrag(run);
        double? cdi = drag.HasValue && q > 0 && run.Reference.SRef > 0 ? drag / (q * run.Reference.SRef) : null;
        double ar = run.Reference.SRef > 0 ? run.Reference.BRef * run.Reference.BRef / run.Reference.SRef : 0;
        double? e = cl.HasValue && cdi > 0 && ar > 0 ? Trefftz.Oswald(cl.Value, ar, cdi.Value) : null;
        var derived = OperatingPoints.Derive(run.Op, run.Water, run.Reference.CRef);
        bool depth = run.Op.HRef.HasValue;
        var groups = new List<ResultGroup>();
        groups.Add(new("Wing result", [
            Row("Tier", Labels.VlmChip),
            Row("CL", Val(cl, "0.000"), note: "Wing only · S_ref " + Num(run.Reference.SRef, "0.####") + " m²"),
            Row("Envelope", RunVerdict(context.Verdicts, run.Strips.Count)),
            Row("CDi (Trefftz)", Val(cdi, "0.00000")),
            Row("e (computed)", Val(e, "0.000"), note: EAdvisory(e, run.Settings)),
            Force("Lift L", lift, units), Force("Induced drag", drag, units),
            Row("Total drag", Loads.TotalDragReason),
            Row("CL/CD", cdi > 0 && cl.HasValue ? Val(cl / cdi, "0.###") : Labels.ClCdUndefined),
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
            note: s.Provisional ? Labels.Provisional : context.Verdicts is { } v && s.J >= 0 && s.J < v.Count ? v[s.J].Text : Labels.Indeterminate)).ToArray()));
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
            double width = Width(run, s.J);
            double localLift = -s.Fx * Math.Sin(alpha) + s.Fz * Math.Cos(alpha);
            StripVerdict? verdict = context.Verdicts is { } v && s.J >= 0 && s.J < v.Count ? v[s.J] : null;
            details.Add(new(s.Eta, [
                Row("Cl_local", Num(s.ClLocal, "0.###")), Row("α_eff", Num(s.AlphaEff, "0.##"), "°"),
                Row("Re_local", Num(s.ReLocal, "0.###E+0")),
                Row("Lift / span", width > 0 ? Num(localLift / width, "0.###") : Labels.StripWidthMissing, width > 0 ? "N/m" : null),
                Row("cd (profile)", s.CdNcrit2.Value.HasValue ? Num(s.CdNcrit2.Value.Value, "0.#####") : s.CdNcrit2.UnavailableReason ?? Labels.NoPolar),
                Row("Envelope (this strip)", s.Provisional ? Labels.Provisional : verdict?.Text ?? Labels.Indeterminate),
                Row("Polar Re range", Labels.NoPolar), Row("Not modelled", Labels.NotModelled(run.Op.HRef.HasValue))
            ]));
        }
        return details;
    }

    private static string RunVerdict(IReadOnlyList<StripVerdict>? verdicts, int count)
    {
        if (verdicts is null || verdicts.Count != count) return Labels.Indeterminate;
        string bound = "(|α_eff − α_L0| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°)";
        int provisional = verdicts.Count(v => v.Provisional);
        int atBound = verdicts.Count(v => v.Text == Labels.AtBound);
        int outside = verdicts.Count(v => !v.Inside && !v.Provisional && v.Text != Labels.AtBound);
        int judged = count - provisional - atBound;
        string outcome = outside > 0 ? $"Outside the method envelope {bound} — {outside} of {judged} strips; exceeded: "
            + string.Join(", ", verdicts.SelectMany(v => v.Exceeded).Distinct())
            : $"Inside the method envelope {bound} at all {judged} judged strips";
        if (atBound > 0) outcome += $"; {atBound} {Labels.AtBound}";
        if (provisional > 0) outcome += $"; {provisional} {Labels.Provisional}";
        return outcome;
    }
    private static double? TrefftzDrag(AnalysisRun run) => run.Strips.Count == 0 ? null :
        0.5 * run.Water.Rho * run.Strips.Sum(s => s.Gamma * -s.DownwashTrefftz * Width(run, s.J));
    private static double Width(AnalysisRun run, int j)
    {
        int n = run.Settings.NSpanPerHalf, total = 2 * n;
        if (j < 0 || j >= total) return 0;
        double Edge(int i) => run.Settings.SpanSpacing == "cosine" ? -Math.Cos(Math.PI * i / total) : -1 + 2.0 * i / total;
        return (Edge(j + 1) - Edge(j)) * run.Reference.BRef / 2;
    }
    private static double? RootBending(AnalysisRun run) => run.Strips.Count == 0 ? null :
        run.Water.Rho * run.Op.Speed * run.Strips.Where(s => s.Y >= 0).Sum(s => s.Gamma * s.Y * Width(run, s.J));
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
            Width(run, s.J) > 0 ? (-s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a)) / Width(run, s.J) : null)).ToArray();
    private static IReadOnlyList<LayerData> Layers(AnalysisRun run, ProjectionContext context, double? rootMoment)
    {
        double max = run.Strips.Count == 0 ? 0 : run.Strips.Max(s => Math.Abs(s.Gamma));
        string key = run.RunKey.Length >= 12 ? run.RunKey[..12] : run.RunKey;
        var layers = new List<LayerData>
        {
            new LayerData("plan-gamma", "Γ per strip", true, "Γ per strip · batlow 1.0 · 0–" + Num(max, "0.###") + " m²/s · run " + key, "strips-table")
            {
                Samples = run.Strips.Select(s => new LayerSample(s.Eta, s.Y, s.Gamma, null,
                    context.Verdicts is { } v && s.J >= 0 && s.J < v.Count && !v[s.J].Inside && !v[s.J].Provisional
                        && v[s.J].Text != Labels.AtBound, s.Provisional)
                    { Verdict = context.Verdicts is { } all && s.J >= 0 && s.J < all.Count ? all[s.J].Text : Labels.Indeterminate }).ToArray(),
                Note = "Outside strips have dashed outlines and a text count."
            },
            new LayerData("strip-lift", "Lift per strip", true, "Lift per strip · N/m · " + Labels.BodyAxes + " · " + Labels.VlmChip, "loads-table")
            {
                Samples = run.Strips.Select(s => new LayerSample(s.Eta, s.Y,
                    Width(run, s.J) > 0 ? s.Fz / Width(run, s.J) : null,
                    Width(run, s.J) > 0 ? new Loads.Vec(s.Fx / Width(run, s.J), s.Fy / Width(run, s.J), s.Fz / Width(run, s.J)) : null,
                    false, s.Provisional)).ToArray(),
                Note = run.Strips.Any(s => Width(run, s.J) <= 0) ? Labels.StripWidthMissing : null
            }
        };
        if (rootMoment.HasValue) layers.Add(new LayerData("root-moment", "Root moment arc", true,
            Labels.RootMoment + " · " + Num(rootMoment.Value, "0.###") + " N·m", "loads-table")
        {
            Samples = [new LayerSample(0, 0, rootMoment.Value, new Loads.Vec(rootMoment.Value, 0, 0), false, false)]
        });
        if (run.Op.HRef.HasValue && context.Stations is { Count: > 0 })
            layers.Add(new LayerData("depth-band", "Free surface and tip depth", true,
                "h_ref " + Num(run.Op.HRef.Value, "0.###") + " m · datum " + run.Op.Datum, "conditions-table")
            { Samples = context.Stations.Select(s => new LayerSample(s.Eta, s.SpanMeters,
                run.Op.HRef.Value - s.ElevationMeters, null, false, false)).ToArray() });
        return layers;
    }
    private static string? EAdvisory(double? e, RunSettings settings) => e is null ? null
        : e > 1 && Labels.DefaultLattice(settings) ? Labels.EAboveOne
        : e < 0.85 || e > 1 ? Labels.EAdvisory : null;
    private static string Derived(DerivedValue value, string format) => value.Value.HasValue ? Num(value.Value.Value, format)
        : value.Reason == DerivedReason.DepthNotSet ? "Unavailable — depth not set" : "Undefined — speed ≤ 0";
    private static string Val(double? value, string format) => value.HasValue && double.IsFinite(value.Value) ? Num(value.Value, format) : "Unavailable";
    private static string Num(double value, string format) => value.ToString(format, Inv);
    private static ResultRow Row(string label, string value, string? unit = null, string? note = null) => new(label, value, unit, note);
    private static ResultRow Force(string label, double? value, Units units, string? note = null) =>
        Row(label, Val(value / (units == Units.Imperial ? 4.4482216152605 : 1), "0.###"), units == Units.Imperial ? "lbf" : "N", note);
}
