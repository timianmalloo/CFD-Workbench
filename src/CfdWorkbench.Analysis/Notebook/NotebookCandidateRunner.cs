using System.Diagnostics;
using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Notebook;

namespace CfdWorkbench.Analysis.Notebook;

/// <summary>
/// Direct in-process local physics evaluator for unaccepted notebook candidates
/// (<c>docs/design/ai-guided-design-notebook.md §3.4</c>).
/// Uses <see cref="NotebookRecords.MaterializeCandidateDsl"/> + <see cref="FoilSource.Parse"/> + <see cref="Geometry.Assess"/> +
/// <see cref="Placement.Sections"/> + <see cref="ProductWingMethod"/> (<c>VortexLattice</c> + <c>StripCoupler</c> + <c>NeuralFoilPolarSource</c>) +
/// <see cref="SectionTier.Evaluate"/> (<c>PanelMethod</c> 200p/400p + <c>Cavitation.Screen</c>).
/// ZERO <c>Envelope.Analysis</c> mutation and ZERO banned <c>ArchitectureTests</c> Core calls.
/// </summary>
public sealed class NotebookCandidateRunner : INotebookSolverRunner
{
    private readonly ProductWingMethod wingMethod;

    public NotebookCandidateRunner(ProductWingMethod? wingMethod = null)
    {
        this.wingMethod = wingMethod ?? new ProductWingMethod();
    }

    public Task<NotebookCandidateRunResult> EvaluateCandidateAsync(
        FoilReqDocument req,
        CandidateIntentDelta delta,
        IProgress<NotebookProgressUpdate>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(delta);
        return Task.Run(() => EvaluateSync(req, delta, progress, ct), ct);
    }

    private NotebookCandidateRunResult EvaluateSync(
        FoilReqDocument req,
        CandidateIntentDelta delta,
        IProgress<NotebookProgressUpdate>? progress,
        CancellationToken ct)
    {
        var totalSw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();

        progress?.Report(new NotebookProgressUpdate("geometry", "Materializing two-profile FoilDSL and certifying geometry...", 0.1));

        byte[] foilBytes;
        try
        {
            foilBytes = NotebookRecords.MaterializeCandidateDsl(delta, "Notebook Candidate");
        }
        catch (ContractError ex)
        {
            totalSw.Stop();
            return new NotebookCandidateRunResult(
                Certified: false,
                SurfaceHash: "",
                GeometryStatus: "Refused",
                RefusalCode: ex.Code,
                FoilDslBytes: [],
                OperatingPoints: [],
                TotalWallMs: totalSw.Elapsed.TotalMilliseconds);
        }

        ct.ThrowIfCancellationRequested();
        var parsed = FoilSource.Parse(foilBytes);
        if (!parsed.IsParsed || parsed.SurfaceHash is null)
        {
            totalSw.Stop();
            return new NotebookCandidateRunResult(
                Certified: false,
                SurfaceHash: "",
                GeometryStatus: "Refused",
                RefusalCode: parsed.Diagnostics.FirstOrDefault()?.Code ?? "DSL-SYNTAX",
                FoilDslBytes: foilBytes,
                OperatingPoints: [],
                TotalWallMs: totalSw.Elapsed.TotalMilliseconds);
        }

        double rootChordM = delta.RootChordMeters;
        double tipChordM = delta.TipChordMeters;
        if (!TipChord.Meets(tipChordM, rootChordM))
        {
            totalSw.Stop();
            return new NotebookCandidateRunResult(
                Certified: false,
                SurfaceHash: parsed.SurfaceHash,
                GeometryStatus: "Refused",
                RefusalCode: TipChord.RefusalCode,
                FoilDslBytes: foilBytes,
                OperatingPoints: [],
                TotalWallMs: totalSw.Elapsed.TotalMilliseconds);
        }

        var assessment = Geometry.Assess(parsed);
        if (assessment.Status != GeometryStatus.Certified)
        {
            totalSw.Stop();
            return new NotebookCandidateRunResult(
                Certified: false,
                SurfaceHash: parsed.SurfaceHash,
                GeometryStatus: assessment.Status.ToString(),
                RefusalCode: assessment.Code,
                FoilDslBytes: foilBytes,
                OperatingPoints: [],
                TotalWallMs: totalSw.Elapsed.TotalMilliseconds);
        }

        ct.ThrowIfCancellationRequested();
        progress?.Report(new NotebookProgressUpdate("sections", "Sampling wing sections along span...", 0.25));

        var settings = wingMethod.Settings;
        var etas = settings.SectionEtas ?? throw new ContractError("ANA-INPUT-STATIONS", "SectionEtas missing.");
        var xs = settings.SectionXs ?? throw new ContractError("ANA-INPUT-STATIONS", "SectionXs missing.");
        var sections = Placement.Sections(foilBytes, etas, xs, ct);
        if (sections.Count >= 2)
        {
            double sampledRoot = sections[0].Frame.TrailingMeters - sections[0].Frame.LeadingMeters;
            double sampledTip = sections[^1].Frame.TrailingMeters - sections[^1].Frame.LeadingMeters;
            rootChordM = sampledRoot;
            if (!TipChord.Meets(sampledTip, sampledRoot))
            {
                totalSw.Stop();
                return new NotebookCandidateRunResult(
                    Certified: false,
                    SurfaceHash: parsed.SurfaceHash,
                    GeometryStatus: "Refused",
                    RefusalCode: TipChord.RefusalCode,
                    FoilDslBytes: foilBytes,
                    OperatingPoints: [],
                    TotalWallMs: totalSw.Elapsed.TotalMilliseconds);
            }
        }
        var reference = wingMethod.Reference(foilBytes);

        // Resolve water record from ITTC WaterTable.
        double tempC = req.Water.Temp.TryToCelsius(out double tc) ? Math.Clamp(tc, 0.0, 50.0) : 15.0;
        double salinity = string.Equals(req.Water.Kind, "fresh", StringComparison.OrdinalIgnoreCase)
            ? 0.0
            : OperatingPoints.SaltSalinityGPerKg;
        WaterRecord water = WaterTable.At(tempC, salinity);

        double pAtm = req.Water.PAtm is { } pQty && pQty.TryToPa(out double pa) ? pa : DisciplinePresets.StandardAtmospherePa;
        bool depthUnset = req.Water.IsDepthUnset;
        double? hRef = (!depthUnset && req.Water.DepthRef is { } dQty && dQty.TryToMeters(out double dm) && dm > 0.0)
            ? dm
            : null;
        if (hRef is null) depthUnset = true;

        var val = FoilReqValidator.Validate(req);
        double targetLoadN = val.ImpliedLoadNewtons ?? 1050.0;

        // Derive 3 operating points: takeoff, glide, steep_drop.
        var opSpecs = BuildOperatingPoints(req, targetLoadN, reference.SRef, water.Rho, pAtm, hRef);
        var summaries = new List<CandidateOperatingPointSummary>(opSpecs.Count);

        for (int idx = 0; idx < opSpecs.Count; idx++)
        {
            ct.ThrowIfCancellationRequested();
            var spec = opSpecs[idx];
            progress?.Report(new NotebookProgressUpdate(
                "solver",
                string.Create(CultureInfo.InvariantCulture, $"Solving {spec.OpId} ({spec.SpeedKn:0.0} kn, alpha {spec.AlphaDeg:0.0} deg)..."),
                0.35 + 0.55 * (idx + 1) / opSpecs.Count));

            var opSw = Stopwatch.StartNew();
            var op = new OperatingPoint(spec.SpeedMps, pAtm, hRef, DisciplinePresets.RootLeadingEdgeDatum, spec.AlphaDeg, targetLoadN);

            try
            {
                var solution = wingMethod.Solve(sections, op, water, ct);
                var strips = wingMethod.Couple(foilBytes, sections, solution, op, water, ct);
                var sectionTier = SectionTier.Evaluate(foilBytes, etas, strips, op, water, ct);
                opSw.Stop();

                double qDyn = 0.5 * water.Rho * spec.SpeedMps * spec.SpeedMps;
                double totalLift = strips.Sum(s => s.Fz);
                double cl = reference.SRef > 1e-9 && qDyn > 1e-9 ? totalLift / (qDyn * reference.SRef) : 0.0;

                // Induced + profile drag from coupled strips (Ncrit 2 and Ncrit 4 bands).
                double inducedDrag = ComputeInducedDrag(strips, qDyn);
                double? cdN2 = ComputeTotalCd(strips, inducedDrag, reference.SRef, qDyn, useNcrit4: false);
                double? cdN4 = ComputeTotalCd(strips, inducedDrag, reference.SRef, qDyn, useNcrit4: true);
                double cdMid = (cdN2, cdN4) switch
                {
                    ({ } a, { } b) => 0.5 * (a + b),
                    ({ } a, null) => a,
                    (null, { } b) => b,
                    _ => reference.SRef > 1e-9 && qDyn > 1e-9 ? Math.Max(1e-4, Math.Abs(strips.Sum(s => s.Fx)) / (qDyn * reference.SRef)) : 0.02
                };

                double? ldN2 = cdN2 is { } d2 && d2 > 1e-6 && cl > 0 ? cl / d2 : null;
                double? ldN4 = cdN4 is { } d4 && d4 > 1e-6 && cl > 0 ? cl / d4 : null;
                double? wingOnlyLd = cdMid > 1e-6 && cl > 0 ? cl / cdMid : null;

                double reMac = spec.SpeedMps * reference.CRef / water.Nu;
                double? cpMin = sectionTier.Cavitation.CpMin;
                double? sigma = depthUnset ? null : sectionTier.Cavitation.Sigma;
                double? cavMargin = (!depthUnset && sigma is { } s && cpMin is { } cp && cp < 0.0) ? s / -cp : null;
                double? vCritKn = (!depthUnset && sectionTier.Cavitation.CriticalSpeed is { } vcs)
                    ? vcs / DisciplinePresets.KnotsToMps
                    : null;
                double? hOverC = (!depthUnset && hRef is { } h && rootChordM > 1e-9) ? h / rootChordM : null;
                double? frH = (!depthUnset && hRef is { } hr && hr > 1e-9)
                    ? spec.SpeedMps / Math.Sqrt(OperatingPoints.Gravity * hr)
                    : null;

                summaries.Add(new CandidateOperatingPointSummary(
                    OpId: spec.OpId,
                    SpeedKn: spec.SpeedKn,
                    AlphaDeg: spec.AlphaDeg,
                    Cl: cl,
                    Cd: cdMid,
                    WingOnlyLd: wingOnlyLd,
                    Ncrit2Ld: ldN2,
                    Ncrit4Ld: ldN4,
                    ReMac: reMac,
                    CpMin: depthUnset ? null : cpMin,
                    Sigma: sigma,
                    CavMargin: cavMargin,
                    VCritKn: vCritKn,
                    GoverningEta: sectionTier.GoverningEta,
                    HOverCRoot: hOverC,
                    FrH: frH,
                    DepthUnset: depthUnset,
                    MethodVersion: $"{wingMethod.Method.Id}/{wingMethod.Method.Version}+PanelMethod200p",
                    UnavailableReason: null,
                    SolverWallMs: opSw.Elapsed.TotalMilliseconds));
            }
            catch (ContractError ex) when (ex.Code.StartsWith("ANA-", StringComparison.Ordinal))
            {
                opSw.Stop();
                summaries.Add(new CandidateOperatingPointSummary(
                    OpId: spec.OpId,
                    SpeedKn: spec.SpeedKn,
                    AlphaDeg: spec.AlphaDeg,
                    Cl: 0.0,
                    Cd: 0.0,
                    WingOnlyLd: null,
                    Ncrit2Ld: null,
                    Ncrit4Ld: null,
                    ReMac: spec.SpeedMps * reference.CRef / water.Nu,
                    CpMin: null,
                    Sigma: null,
                    CavMargin: null,
                    VCritKn: null,
                    GoverningEta: null,
                    HOverCRoot: null,
                    FrH: null,
                    DepthUnset: depthUnset,
                    MethodVersion: $"{wingMethod.Method.Id}/{wingMethod.Method.Version}",
                    UnavailableReason: $"Unavailable — {ex.Code}: {ex.Reason ?? ex.Message}",
                    SolverWallMs: opSw.Elapsed.TotalMilliseconds));
            }
        }

        totalSw.Stop();
        progress?.Report(new NotebookProgressUpdate("done", "Local physics evaluation complete.", 1.0));

        return new NotebookCandidateRunResult(
            Certified: true,
            SurfaceHash: parsed.SurfaceHash,
            GeometryStatus: nameof(GeometryStatus.Certified),
            RefusalCode: null,
            FoilDslBytes: foilBytes,
            OperatingPoints: summaries,
            TotalWallMs: totalSw.Elapsed.TotalMilliseconds);
    }

    private sealed record OpSpec(string OpId, double SpeedKn, double SpeedMps, double AlphaDeg);

    private static List<OpSpec> BuildOperatingPoints(
        FoilReqDocument req,
        double loadN,
        double areaM2,
        double rho,
        double pAtm,
        double? hRef)
    {
        double toKn = req.FindObjective("takeoff_speed") is { Quantity: { } tq } && tq.TryToKnots(out double tk) && tk > 3.0 ? tk : 10.5;
        double dropKn = req.FindObjective("drop_speed") is { Quantity: { } dq } && dq.TryToKnots(out double dk) && dk > toKn ? dk : 24.0;
        double glideKn = req.FindObjective("glide_speed") is { Quantity: { } gq } && gq.TryToKnots(out double gk) && gk > toKn ? gk : Math.Round(0.5 * (toKn + Math.Min(dropKn, 20.0)), 1);

        // Estimate reasonable attached-flow angle of attack from required CL = W / (q S) using 2*pi*AR/(AR+2) slope (clamped safely to [-1, 7.5] deg).
        double EstimateAlpha(double speedKn)
        {
            double v = speedKn * DisciplinePresets.KnotsToMps;
            double clReq = areaM2 > 1e-9 && v > 1e-6 ? loadN / (0.5 * rho * v * v * areaM2) : 0.4;
            double clSlopePerDeg = 0.088; // ~5.05 / rad for finite wing AR~10
            double alpha = (clReq - 0.18) / clSlopePerDeg;
            return Math.Round(Math.Clamp(alpha, -1.0, 7.5), 2);
        }

        return
        [
            new("takeoff", toKn, toKn * DisciplinePresets.KnotsToMps, EstimateAlpha(toKn)),
            new("glide", glideKn, glideKn * DisciplinePresets.KnotsToMps, EstimateAlpha(glideKn)),
            new("steep_drop", dropKn, dropKn * DisciplinePresets.KnotsToMps, EstimateAlpha(dropKn))
        ];
    }

    private static double ComputeInducedDrag(IReadOnlyList<StripLoad> strips, double qDyn)
    {
        // Sum near-field streamwise force Fx across strips (excluding negative numerical noise).
        double fxSum = 0.0;
        foreach (var s in strips)
            fxSum += s.Fx;
        return Math.Max(0.0, fxSum);
    }

    private static double? ComputeTotalCd(IReadOnlyList<StripLoad> strips, double inducedDragN, double sRef, double qDyn, bool useNcrit4)
    {
        if (sRef <= 1e-9 || qDyn <= 1e-9 || strips.Count == 0) return null;
        double profileDragN = 0.0;
        int valid = 0;
        foreach (var s in strips)
        {
            double? cdStrip = useNcrit4 ? s.CdNcrit4.Value : s.CdNcrit2.Value;
            if (cdStrip is null || !double.IsFinite(cdStrip.Value)) continue;
            double width = (s.YHigh is { } yh && s.YLow is { } yl) ? Math.Abs(yh - yl) : (sRef / Math.Max(1, strips.Count) / Math.Max(1e-6, s.Chord));
            profileDragN += qDyn * s.Chord * width * cdStrip.Value;
            valid++;
        }
        if (valid == 0) return null;
        return (inducedDragN + profileDragN) / (qDyn * sRef);
    }
}
