using System.Globalization;
using System.Text;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;

namespace CfdWorkbench.Analysis.Results;

/// <summary>
/// Unifies completed local VLM/Sweep runs and harvested Area 5 <see cref="CfdRunRecord"/>s into surface Cp/Cf scalar
/// arrays mapped cleanly over <c>Placement.Surface</c> (ADR-0010), small multiples, difference floods, replay series,
/// 9-field <see cref="ProvenanceStrip"/>s, linked <see cref="TableTwin"/>s, and <see cref="CandidateLadder"/> views.
/// Never averages values across tiers; all file reads use explicit <c>FileShare.Read | FileShare.Delete</c>.
/// </summary>
public static class ResultsService
{
    public static ResultsWorkspaceModel ProjectWorkspace(
        AuthoringSession session,
        string? caseRootDirectory = null,
        IParaViewLocator? paraViewLocator = null,
        string? paraViewOverridePath = null,
        string? experimentTerminalReason = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        var snapshot = session.Snapshot();
        var runLedger = session.ReadRuns();
        var cfdLedger = session.ReadCfdLedger();

        var samples = new List<UnifiedResultSample>();

        // 1. Project local VLM + strip / Estimator runs (never merged or averaged with CFD runs).
        foreach (var stored in runLedger.Runs)
        {
            samples.Add(ProjectLocalRun(stored));
        }

        // 2. Project Area 5 CFD runs + harvested FieldEvidenceRecords.
        var evidenceByRun = cfdLedger.FieldEvidence
            .Where(e => e.Integrity == CfdRecordIntegrity.Intact)
            .GroupBy(e => e.Evidence.RunId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FieldEvidenceRecord>)g.Select(x => x.Evidence).ToArray(), StringComparer.Ordinal);

        foreach (var stored in cfdLedger.Runs)
        {
            evidenceByRun.TryGetValue(stored.Run.RunId, out var runEvidence);
            samples.Add(ProjectCfdRun(stored, runEvidence ?? [], caseRootDirectory));
        }

        var smallMultiples = ResultsProjection.BuildSmallMultiples(samples);
        var ladder = CandidateLadder.RankCandidates(
            cfdLedger.Candidates,
            snapshot.AcceptedId,
            snapshot.SurfaceHash,
            experimentTerminalReason);
        var paraView = ParaViewHandoff.Prepare(paraViewLocator, paraViewOverridePath);

        return new ResultsWorkspaceModel(
            CurrentAcceptedId: snapshot.AcceptedId,
            CurrentSurfaceHash: snapshot.SurfaceHash,
            Samples: samples,
            SmallMultiples: smallMultiples,
            CandidateLadder: ladder,
            ParaView: paraView,
            NoCrossTierAveragingNote: Labels.CompareFootnote);
    }

    /// <summary>
    /// Maps surface Cp or Cf scalar values cleanly over <see cref="Placement.Surface"/> (ADR-0010 single placement authority).
    /// Desktop receives these placed starboard nodes and applies only camera + <see cref="Point3.Port"/>.
    /// </summary>
    public static SurfaceFieldProjection ProjectSurfaceField(
        AuthoringSession session,
        UnifiedResultSample sample,
        string variable,
        CancellationToken cancellation = default,
        int stations = 17,
        int chordSamples = 21)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sample);
        if (string.IsNullOrWhiteSpace(variable))
            throw new ContractError("RES-VARIABLE-REQUIRED", "Surface variable name is required.");

        var snapshot = session.Snapshot();
        var surface = Placement.Surface(snapshot.Source, "Accepted", 0, cancellation, stations: stations, chordSamples: chordSamples);

        var sourceField = string.Equals(variable, "Cf", StringComparison.Ordinal)
            ? sample.SurfaceCfData
            : sample.SurfaceCpData;

        var nodes = new List<SurfaceScalarNode>(surface.Sections.Count * chordSamples);
        var tableEntries = new List<(string Location, double? Value, string? UnavailableReason, string? Flag)>(surface.Sections.Count * chordSamples);
        double minVal = double.PositiveInfinity;
        double maxVal = double.NegativeInfinity;
        bool anyFinite = false;

        int flatIndex = 0;
        foreach (var section in surface.Sections)
        {
            int count = section.Upper.Count;
            for (int c = 0; c < count; c++)
            {
                var pt = section.Upper[c];
                double? val = null;
                if (sourceField is not null && sourceField.Values.Count > 0)
                {
                    int mappedIdx = sourceField.Values.Count == 1
                        ? 0
                        : (int)((long)flatIndex * (sourceField.Values.Count - 1) / Math.Max(1, surface.Sections.Count * count - 1));
                    double raw = sourceField.Values[Math.Clamp(mappedIdx, 0, sourceField.Values.Count - 1)];
                    if (double.IsFinite(raw))
                    {
                        val = raw;
                        if (raw < minVal) minVal = raw;
                        if (raw > maxVal) maxVal = raw;
                        anyFinite = true;
                    }
                }

                nodes.Add(new SurfaceScalarNode(section.Eta, pt.X, pt.Y, pt.Z, val));
                double xFrac = count > 1 ? (double)c / (count - 1) : 0.0;
                string loc = string.Create(CultureInfo.InvariantCulture, $"η {section.Eta:0.00} · x/c {xFrac:0.00}");
                tableEntries.Add((loc, val, val is null ? $"{variable} field not present in this sample" : null, val is null ? "Unavailable" : "OK"));
                flatIndex++;
            }
        }

        string unit = sourceField?.Unit ?? "dimensionless";
        string cmap = string.Equals(variable, "Cp", StringComparison.Ordinal)
            ? ProvenanceStrip.VikCrameri
            : ProvenanceStrip.BatlowCrameri;
        var tableTwin = ProvenanceStrip.BuildTableTwin(variable, unit, tableEntries);
        var provenance = ProvenanceStrip.BuildProvenance(
            variable: variable,
            unit: unit,
            rangeWithBasis: anyFinite ? ProvenanceStrip.FormatRange(minVal, maxVal, "sample-range") : ProvenanceStrip.NotRecorded,
            methodTierAndVersion: sample.MethodTierAndVersion,
            sourceRunAndSample: ResultsProjection.FormatRunSampleLabel(sample.RunId, sample.AlphaDeg, sample.SpeedMs),
            depthBasis: sample.Provenance.DepthBasis,
            evidenceLabel: sample.EvidenceLabel,
            colormapNameAndVersion: cmap,
            associationAndTimeBasis: sample.Provenance.AssociationAndTimeBasis,
            tableTwinRowCount: tableTwin.Rows.Count);

        return new SurfaceFieldProjection(
            RunId: sample.RunId,
            Variable: variable,
            Unit: unit,
            SurfaceHash: snapshot.SurfaceHash,
            MinValue: anyFinite ? minVal : null,
            MaxValue: anyFinite ? maxVal : null,
            Nodes: nodes,
            Provenance: provenance,
            TableTwin: tableTwin);
    }

    /// <summary>
    /// Builds a compatible replay sequence over the given samples at held speed or held alpha.
    /// </summary>
    public static ReplaySeries BuildReplaySeries(
        IReadOnlyList<UnifiedResultSample> samples,
        WaterRecord water,
        string variable,
        ReplayHeldAxis heldAxis,
        ReplayCameraState? cameraState = null,
        bool reducedMotion = false)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(water);

        var slots = new List<ReplaySampleSlot>(samples.Count);
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            var field = string.Equals(variable, "Cf", StringComparison.Ordinal) ? s.SurfaceCfData : s.SurfaceCpData;
            string inventoryHash = string.Join(",", s.Manifest.Layers.Where(l => l.Available).Select(l => l.Kind.ToString()));
            if (string.IsNullOrEmpty(inventoryHash)) inventoryHash = "none";

            IReadOnlyList<double?>? fieldValues = s.Admitted && field is not null
                ? field.Values.Select(v => (double?)v).ToArray()
                : null;
            IReadOnlyDictionary<string, double?>? metrics = s.Admitted
                ? new Dictionary<string, double?> { ["CL"] = s.Cl, ["CD"] = s.Cd, ["CM"] = s.Cm }
                : null;

            slots.Add(new ReplaySampleSlot(
                CaseIndex: i,
                RunId: s.RunId,
                SurfaceHash: s.SurfaceHash,
                MeshHash: s.MeshHash,
                MethodId: s.Tier,
                MethodVersion: s.MethodTierAndVersion,
                Water: water,
                FieldInventoryHash: inventoryHash,
                SpeedMs: s.SpeedMs,
                AlphaDeg: s.AlphaDeg,
                Admitted: s.Admitted,
                FailureReason: s.Admitted ? null : s.StatusLabel,
                RetrySampleRunId: s.Admitted ? null : s.RunId,
                FieldValues: fieldValues,
                Metrics: metrics,
                Provenance: s.Provenance));
        }

        return ReplaySequence.CreateSeries(slots, heldAxis, cameraState, reducedMotion);
    }

    /// <summary>Computes a zero-pinned difference flood between two unified samples (refuses mismatched meshes).</summary>
    public static DifferenceFloodResult ComputeDifferenceFlood(
        UnifiedResultSample baseSample,
        UnifiedResultSample targetSample,
        string variable)
    {
        ArgumentNullException.ThrowIfNull(baseSample);
        ArgumentNullException.ThrowIfNull(targetSample);

        var baseField = string.Equals(variable, "Cf", StringComparison.Ordinal) ? baseSample.SurfaceCfData : baseSample.SurfaceCpData;
        var targetField = string.Equals(variable, "Cf", StringComparison.Ordinal) ? targetSample.SurfaceCfData : targetSample.SurfaceCpData;

        if (baseField is null || targetField is null)
        {
            return new DifferenceFloodResult(
                Available: false,
                UnavailableReason: $"Unavailable — {variable} field not present in both samples",
                Variable: $"Δ{variable}",
                Unit: "dimensionless",
                BaseRunId: baseSample.RunId,
                TargetRunId: targetSample.RunId,
                SymmetricAbsMax: null,
                ScaleMin: null,
                ScaleMax: null,
                DeltaValues: [],
                Provenance: null,
                TableTwin: null);
        }

        return DifferenceFlood.ComputeDifference(baseField, targetField, variable);
    }

    /// <summary>Deterministic offline key-free AI-10 validator for "Ask about this result".</summary>
    public static AskSeparationAnswer AskAboutResult(UnifiedResultSample sample, string question)
    {
        ArgumentNullException.ThrowIfNull(sample);
        return FieldEvidenceManifest.EvaluateAskAboutSeparation(sample.Manifest, question);
    }

    private static UnifiedResultSample ProjectLocalRun(StoredRun stored)
    {
        var run = stored.Run;
        bool intact = stored.Integrity == RunIntegrity.Intact;
        bool completed = intact && run.Outcome is RunOutcome.Completed;

        double? cl = null;
        double? cd = null;
        double? cm = null;
        var tableEntries = new List<(string Location, double? Value, string? UnavailableReason, string? Flag)>();
        var cpValues = new List<double>();
        var cpLocations = new List<string>();

        if (completed && run.Strips.Count > 0)
        {
            double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
            double qS = q * run.Reference.SRef;
            if (qS > 0 && double.IsFinite(qS))
            {
                var (sin, cos) = double.SinCosPi(run.Op.AlphaDeg / 180.0);
                double lift = 0.0;
                double momentY = 0.0;
                foreach (var strip in run.Strips)
                {
                    lift += strip.Fz * cos - strip.Fx * sin;
                    momentY += strip.My;
                }
                cl = lift / qS;
                if (run.Reference.CRef > 0) cm = momentY / (qS * run.Reference.CRef);

                var wingDrag = Loads.WingDrag(run, 4);
                var inducedDrag = Loads.InducedDrag(run);
                double? dragN = wingDrag.Value ?? inducedDrag.Value;
                if (dragN is { } d) cd = d / qS;
            }

            foreach (var strip in run.Strips)
            {
                double loading = run.Reference.CRef > 0 ? strip.ClLocal * strip.Chord / run.Reference.CRef : strip.ClLocal;
                string loc = string.Create(CultureInfo.InvariantCulture, $"η {strip.Eta:0.000}");
                string? flag = strip.Provisional ? Labels.TipNotJudged : "OK";
                tableEntries.Add((loc, loading, null, flag));
                cpValues.Add(-strip.ClLocal);
                cpLocations.Add(loc);
            }
        }
        else if (!intact)
        {
            tableEntries.Add(("run", null, Labels.PayloadFailed, "Tampered"));
        }
        else if (run.Outcome is RunOutcome.Failed failed)
        {
            tableEntries.Add(("run", null, failed.Reason, failed.Code));
        }

        var manifest = FieldEvidenceManifest.BuildLocalVlmManifest(
            run.RunId,
            hasPanelCp: completed && cpValues.Count > 0,
            hasForceVectors: completed,
            hasSpanwiseLoading: completed && run.Strips.Count > 0);

        var tableTwin = ProvenanceStrip.BuildTableTwin("Cl·c/c̄", "dimensionless", tableEntries);
        double? rangeMin = tableEntries.Where(e => e.Value is not null).Select(e => e.Value).DefaultIfEmpty(null).Min();
        double? rangeMax = tableEntries.Where(e => e.Value is not null).Select(e => e.Value).DefaultIfEmpty(null).Max();

        string methodLabel = run.Tier == "estimator"
            ? $"{Labels.EstimatorChip} ({run.Method.Id} {run.Method.Version})"
            : $"{Labels.VlmChip} ({run.Method.Id} {run.Method.Version})";
        string depthBasis = ResultsProjection.FormatDepthBasis(run.Op.HRef, run.Op.Datum);
        string evidenceLabel = !intact
            ? Labels.PayloadFailed
            : run.Outcome is RunOutcome.Failed f
                ? $"Failed ({f.Code}): {f.Reason}"
                : "Computed estimate · Model uncertainty not quantified";

        var provenance = ProvenanceStrip.BuildProvenance(
            variable: "Cl·c/c̄",
            unit: "dimensionless",
            rangeWithBasis: ProvenanceStrip.FormatRange(rangeMin, rangeMax, "sample-range"),
            methodTierAndVersion: methodLabel,
            sourceRunAndSample: ResultsProjection.FormatRunSampleLabel(run.RunId, run.Op.AlphaDeg, run.Op.Speed),
            depthBasis: depthBasis,
            evidenceLabel: evidenceLabel,
            colormapNameAndVersion: ProvenanceStrip.BatlowCrameri,
            associationAndTimeBasis: "strip · local calculation",
            tableTwinRowCount: tableTwin.Rows.Count);

        SampleFieldData? cpData = completed && cpValues.Count > 0
            ? new SampleFieldData(
                RunId: run.RunId,
                SampleLabel: string.Create(CultureInfo.InvariantCulture, $"α {run.Op.AlphaDeg:0.0}° · V {run.Op.Speed:0.00} m/s"),
                MeshHash: null,
                Variable: "Cp",
                Unit: "dimensionless",
                Association: "strip",
                MethodTierAndVersion: methodLabel,
                DepthBasis: depthBasis,
                EvidenceLabel: evidenceLabel,
                AssociationAndTimeBasis: "strip · local calculation",
                Values: cpValues,
                Locations: cpLocations)
            : null;

        return new UnifiedResultSample(
            SampleId: run.RunId,
            RunId: run.RunId,
            Tier: run.Tier,
            SourceKind: "local",
            MethodTierAndVersion: methodLabel,
            AlphaDeg: run.Op.AlphaDeg,
            SpeedMs: run.Op.Speed,
            Cl: cl,
            Cd: cd,
            Cm: cm,
            MeshHash: null,
            SurfaceHash: run.Inputs.SurfaceHash,
            Admitted: completed,
            StatusLabel: completed ? "Completed" : evidenceLabel,
            EvidenceLabel: evidenceLabel,
            Manifest: manifest,
            Provenance: provenance,
            TableTwin: tableTwin,
            SurfaceCpData: cpData,
            SurfaceCfData: null);
    }

    private static UnifiedResultSample ProjectCfdRun(
        StoredCfdRun stored,
        IReadOnlyList<FieldEvidenceRecord> runEvidence,
        string? caseRootDirectory)
    {
        var run = stored.Run;
        bool intact = stored.Integrity == CfdRecordIntegrity.Intact;
        bool admitted = intact && run.CurrentState is CfdRunState.Completed or CfdRunState.CompletedResidualNotMet;

        var manifest = FieldEvidenceManifest.BuildManifest(
            run.RunId,
            admitted ? runEvidence : [],
            hasForceVectors: admitted && run.ResidualSummary is not null,
            hasSpanwiseLoading: admitted && run.ResidualSummary is not null);

        string methodLabel = $"{run.BackendPin.Backend} {run.BackendPin.Version} ({run.BackendPin.Substrate})";
        string depthBasis = ResultsProjection.FormatDepthBasis(run.Op.HRef, run.Op.Datum);
        string? converged = admitted ? CfdRunStateMachine.FormatConvergedLabel(run.ResidualSummary, outputsComplete: true) : null;
        string stateDisplay = CfdRunStateMachine.DisplayString(run.CurrentState, run.Events[^1].Reason);
        string evidenceLabel = !intact
            ? Labels.CfdPayloadFailed
            : converged ?? stateDisplay;

        var surfaceEv = runEvidence.FirstOrDefault(e => e.Kind == "surface");
        var cpRange = surfaceEv?.Ranges.FirstOrDefault(r => string.Equals(r.Variable, "Cp", StringComparison.Ordinal));
        var cfRange = surfaceEv?.Ranges.FirstOrDefault(r => string.Equals(r.Variable, "Cf", StringComparison.Ordinal));

        string primaryVar = cpRange is not null ? "Cp" : "CL";
        string primaryUnit = cpRange?.Unit ?? "dimensionless";
        string rangeWithBasis = cpRange is not null
            ? ProvenanceStrip.FormatRange(cpRange.Min, cpRange.Max, cpRange.Basis)
            : run.ResidualSummary is { } rs
                ? ProvenanceStrip.FormatRange(rs.ClWindowMean - rs.ClHalfBandUi, rs.ClWindowMean + rs.ClHalfBandUi, "averaging-window")
                : ProvenanceStrip.NotRecorded;
        string cmap = primaryVar == "Cp" ? ProvenanceStrip.VikCrameri : ProvenanceStrip.BatlowCrameri;
        string assocBasis = surfaceEv is not null
            ? $"{surfaceEv.Association} · {(surfaceEv.IsTransient ? "transient" : "steady RANS (mean field)")}"
            : "face · steady RANS (mean field)";

        SampleFieldData? cpData = null;
        SampleFieldData? cfData = null;
        if (admitted && surfaceEv is not null)
        {
            var (diskCp, diskCf, diskLocs) = TryReadSurfaceRaw(caseRootDirectory, run.RunKey, surfaceEv.RelativePath);
            if (diskCp is { Count: > 0 })
            {
                cpData = new SampleFieldData(run.RunId, ResultsProjection.FormatRunSampleLabel(run.RunId, run.Op.AlphaDeg, run.Op.Speed),
                    run.MeshHash ?? surfaceEv.MeshHash, "Cp", "dimensionless", surfaceEv.Association, methodLabel, depthBasis, evidenceLabel, assocBasis, diskCp, diskLocs);
            }
            else if (cpRange is not null)
            {
                cpData = new SampleFieldData(run.RunId, ResultsProjection.FormatRunSampleLabel(run.RunId, run.Op.AlphaDeg, run.Op.Speed),
                    run.MeshHash ?? surfaceEv.MeshHash, "Cp", cpRange.Unit, surfaceEv.Association, methodLabel, depthBasis, evidenceLabel, assocBasis,
                    [cpRange.Min, 0.5 * (cpRange.Min + cpRange.Max), cpRange.Max], ["min", "mid", "max"]);
            }

            if (diskCf is { Count: > 0 })
            {
                cfData = new SampleFieldData(run.RunId, ResultsProjection.FormatRunSampleLabel(run.RunId, run.Op.AlphaDeg, run.Op.Speed),
                    run.MeshHash ?? surfaceEv.MeshHash, "Cf", "dimensionless", surfaceEv.Association, methodLabel, depthBasis, evidenceLabel, assocBasis, diskCf, diskLocs);
            }
            else if (cfRange is not null)
            {
                cfData = new SampleFieldData(run.RunId, ResultsProjection.FormatRunSampleLabel(run.RunId, run.Op.AlphaDeg, run.Op.Speed),
                    run.MeshHash ?? surfaceEv.MeshHash, "Cf", cfRange.Unit, surfaceEv.Association, methodLabel, depthBasis, evidenceLabel, assocBasis,
                    [cfRange.Min, 0.5 * (cfRange.Min + cfRange.Max), cfRange.Max], ["min", "mid", "max"]);
            }
        }

        var tableEntries = new List<(string Location, double? Value, string? UnavailableReason, string? Flag)>();
        if (admitted && cpData is not null)
        {
            for (int i = 0; i < cpData.Values.Count; i++)
            {
                string loc = cpData.Locations is { } locs && i < locs.Count ? locs[i] : $"face {i}";
                tableEntries.Add((loc, cpData.Values[i], null, "OK"));
            }
        }
        else if (admitted && run.ResidualSummary is { } rs2)
        {
            tableEntries.Add(("wing CL", rs2.ClWindowMean, null, rs2.CriterionMet ? "Converged" : "ResidualNotMet"));
            tableEntries.Add(("wing CD", rs2.CdWindowMean, null, rs2.CriterionMet ? "Converged" : "ResidualNotMet"));
            tableEntries.Add(("wing CM", rs2.CmWindowMean, null, rs2.CriterionMet ? "Converged" : "ResidualNotMet"));
        }
        else
        {
            tableEntries.Add(("sample", null, evidenceLabel, run.CurrentState.ToString()));
        }

        var tableTwin = ProvenanceStrip.BuildTableTwin(primaryVar, primaryUnit, tableEntries);
        var provenance = ProvenanceStrip.BuildProvenance(
            variable: primaryVar,
            unit: primaryUnit,
            rangeWithBasis: rangeWithBasis,
            methodTierAndVersion: methodLabel,
            sourceRunAndSample: ResultsProjection.FormatRunSampleLabel(run.RunId, run.Op.AlphaDeg, run.Op.Speed),
            depthBasis: depthBasis,
            evidenceLabel: evidenceLabel,
            colormapNameAndVersion: cmap,
            associationAndTimeBasis: assocBasis,
            tableTwinRowCount: tableTwin.Rows.Count);

        return new UnifiedResultSample(
            SampleId: run.RunId,
            RunId: run.RunId,
            Tier: "cfd",
            SourceKind: "cfd",
            MethodTierAndVersion: methodLabel,
            AlphaDeg: run.Op.AlphaDeg,
            SpeedMs: run.Op.Speed,
            Cl: admitted ? run.ResidualSummary?.ClWindowMean : null,
            Cd: admitted ? run.ResidualSummary?.CdWindowMean : null,
            Cm: admitted ? run.ResidualSummary?.CmWindowMean : null,
            MeshHash: run.MeshHash ?? surfaceEv?.MeshHash,
            SurfaceHash: run.Inputs.SurfaceHash,
            Admitted: admitted,
            StatusLabel: stateDisplay,
            EvidenceLabel: evidenceLabel,
            Manifest: manifest,
            Provenance: provenance,
            TableTwin: tableTwin,
            SurfaceCpData: cpData,
            SurfaceCfData: cfData);
    }

    private static (List<double>? Cp, List<double>? Cf, List<string>? Locations) TryReadSurfaceRaw(
        string? caseRootDirectory,
        string runKey,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(caseRootDirectory) || string.IsNullOrWhiteSpace(relativePath))
            return (null, null, null);

        // M5 fail-closed boundary: reject rooted paths, leading separators, or '..' traversal from untrusted .cfdw.json.
        if (Path.IsPathRooted(relativePath) || relativePath.StartsWith('/') || relativePath.StartsWith('\\'))
            return (null, null, null);
        string[] relSegments = relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (relSegments.Length == 0 || relSegments.Any(s => s == ".."))
            return (null, null, null);

        string fullRoot = Path.GetFullPath(caseRootDirectory);
        if (!Directory.Exists(fullRoot) || SolverLauncher.IsReparseOrLink(fullRoot))
            return (null, null, null);

        string rootWithSep = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        var pathCmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        string normalizedRel = Path.Combine(relSegments);
        var candidates = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(runKey) &&
            !Path.IsPathRooted(runKey) &&
            !runKey.Contains('/') && !runKey.Contains('\\') && runKey != "..")
        {
            candidates.Add(Path.Combine(fullRoot, runKey, normalizedRel));
        }
        candidates.Add(Path.Combine(fullRoot, normalizedRel));

        string? path = null;
        foreach (string candidate in candidates)
        {
            string full = Path.GetFullPath(candidate);
            if (!full.StartsWith(rootWithSep, pathCmp))
                continue;
            if (!File.Exists(full))
                continue;
            if (HasReparseWithinRoot(full, fullRoot, pathCmp))
                continue;
            path = full;
            break;
        }
        if (path is null) return (null, null, null);

        string text;
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
        using (var reader = new StreamReader(fs, Encoding.UTF8))
        {
            text = reader.ReadToEnd();
        }

        string? headerLine = null;
        var rows = new List<string[]>();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('#'))
            {
                string c = line.TrimStart('#').Trim();
                if (c.Length > 0) headerLine = c;
                continue;
            }
            rows.Add(line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        if (headerLine is null || rows.Count == 0) return (null, null, null);
        string[] cols = headerLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var idxMap = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < cols.Length; i++) idxMap[cols[i]] = i;

        bool hasCp = idxMap.TryGetValue("Cp", out int cpIdx);
        bool hasCf = idxMap.TryGetValue("Cf", out int cfIdx);
        if (!hasCp && !hasCf) return (null, null, null);

        var cp = hasCp ? new List<double>(rows.Count) : null;
        var cf = hasCf ? new List<double>(rows.Count) : null;
        var locs = new List<string>(rows.Count);

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            if (hasCp && cpIdx < row.Length && double.TryParse(row[cpIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out double vCp) && double.IsFinite(vCp))
                cp!.Add(vCp);
            if (hasCf && cfIdx < row.Length && double.TryParse(row[cfIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out double vCf) && double.IsFinite(vCf))
                cf!.Add(vCf);
            locs.Add(string.Create(CultureInfo.InvariantCulture, $"face {r}"));
        }

        return (cp, cf, locs);
    }

    private static bool HasReparseWithinRoot(string fullPath, string fullRoot, StringComparison pathCmp)
    {
        string? current = fullPath;
        while (!string.IsNullOrEmpty(current))
        {
            if (SolverLauncher.IsReparseOrLink(current))
                return true;
            if (string.Equals(current.TrimEnd(Path.DirectorySeparatorChar), fullRoot.TrimEnd(Path.DirectorySeparatorChar), pathCmp))
                break;
            current = Path.GetDirectoryName(current);
        }
        return false;
    }
}
