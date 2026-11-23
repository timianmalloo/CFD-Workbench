using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;

namespace CfdWorkbench.Analysis.CfdRun;

public sealed record HarvestResult(
    ResidualSummary ResidualSummary,
    double? YPlusP95,
    double? YPlusMax,
    double? YPlusMin,
    bool HasWallShearStress,
    IReadOnlyList<FieldEvidenceRecord> FieldEvidence,
    string FinalTimeDirectory,
    int FilesParsed);

public sealed record CheckpointValidationResult(
    bool Valid,
    string? TimeDirectory,
    string? ManifestHash,
    string? FailureCode,
    string? FailureReason);

/// <summary>
/// Fail-closed Area 5 file harvester &amp; checkpoint validator (docs/design/cfd-run-pipeline.md §7.1, §7.2).
/// Every file read uses explicit <c>FileShare.Read | FileShare.Delete</c> (READER-SHARE compliant).
/// Never falls back to positional column indices; any missing or unknown header fails closed with CFD-HARVEST-COLUMNS.
/// </summary>
public static class CfdOutputHarvester
{
    private static readonly Regex BoundingRegex = new(
        @"\bbounding\s+(?:nuTilda|k|omega)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string ComputeFileBlake3(string path) => Identity.Blake3(ReadAllBytesShared(path));

    /// <summary>Harvests OpenFOAM postProcessing outputs and verifies final time directory exists.</summary>
    public static HarvestResult HarvestOpenFoam(
        string caseDir,
        string runId,
        string meshHash,
        TypedCaseModel model,
        string? solverLog = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        NativeProject.Uuid(runId);

        if (!Directory.Exists(caseDir))
            throw new ContractError("CFD-NO-OUTPUTS", Labels.CfdNoOutputs);

        string? finalTimeDir = FindLatestPositiveTimeDir(caseDir);
        if (finalTimeDir is null)
            throw new ContractError("CFD-NO-OUTPUTS", Labels.CfdNoOutputs);

        int filesParsed = 0;

        // 1. forceCoeffs
        string coeffPath = FindLatestPostProcessingFile(caseDir, "forceCoeffs", "coefficient.dat")
            ?? throw new ContractError("CFD-NO-OUTPUTS", "Missing postProcessing/forceCoeffs/<t>/coefficient.dat.");
        var (clSeries, cdSeries, cmSeries) = ParseForceCoeffs(coeffPath);
        filesParsed++;

        // 2. solverInfo
        string solverInfoPath = FindLatestPostProcessingFile(caseDir, "solverInfo", "solverInfo.dat")
            ?? throw new ContractError("CFD-NO-OUTPUTS", "Missing postProcessing/solverInfo/<t>/solverInfo.dat.");
        var eqResiduals = ParseSolverInfo(solverInfoPath);
        filesParsed++;

        // 3. yPlus
        string yPlusPath = FindLatestPostProcessingFile(caseDir, "yPlus", "yPlus.dat")
            ?? throw new ContractError("CFD-NO-OUTPUTS", "Missing postProcessing/yPlus/<t>/yPlus.dat.");
        var (yMin, yMax, yP95) = ParseYPlus(yPlusPath);
        filesParsed++;

        // 4. wallShearStress (optional; if absent hasWallShearStress = false; if present MUST match header contract)
        string? wssPath = FindLatestPostProcessingFile(caseDir, "wallShearStress", "wallShearStress.dat");
        bool hasWallShearStress = false;
        if (wssPath is not null)
        {
            ValidateWallShearStress(wssPath);
            hasWallShearStress = true;
            filesParsed++;
        }

        // 5. Evaluate DR-F2-1 three-part residual criterion
        int windowLen = Math.Min(model.AveragingWindowIterations, clSeries.Count);
        var clWindow = clSeries.Skip(clSeries.Count - windowLen).ToArray();
        var cdWindow = cdSeries.Skip(cdSeries.Count - windowLen).ToArray();
        var cmWindow = cmSeries.Skip(cmSeries.Count - windowLen).ToArray();
        int boundingCount = CountTurbulenceBounding(solverLog);

        var summary = CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(
            EquationInitialResiduals: eqResiduals,
            ClWindow: clWindow,
            CdWindow: cdWindow,
            CmWindow: cmWindow,
            ResidualDropTarget: model.ResidualDropTarget,
            ClHalfBandCap: model.ClHalfBandCap,
            CdHalfBandCap: model.CdHalfBandCap,
            TurbulenceBoundingCountInWindow: boundingCount,
            RanToIterationLimit: clSeries.Count >= model.MaxIterations));

        // 6. Harvest wingSurface .raw evidence if present
        var evidence = new List<FieldEvidenceRecord>();
        string? surfaceFile = FindLatestSurfaceSample(caseDir, "wingSurface");
        if (surfaceFile is not null)
        {
            var ev = ParseSurfaceSample(caseDir, surfaceFile, runId, meshHash, hasWallShearStress, model.TurbulenceModel == TurbulenceModelKind.KOmegaSst);
            evidence.Add(ev);
            filesParsed++;
        }

        return new HarvestResult(summary, yP95, yMax, yMin, hasWallShearStress, evidence, finalTimeDir, filesParsed);
    }

    /// <summary>Harvests SU2 history.csv (and optional surface_flow.csv), failing closed on unknown column layout.</summary>
    public static HarvestResult HarvestSu2(
        string caseDir,
        string runId,
        string meshHash,
        TypedCaseModel model,
        string? solverLog = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        NativeProject.Uuid(runId);

        string historyPath = Path.Combine(caseDir, "history.csv");
        if (!File.Exists(historyPath))
            throw new ContractError("CFD-NO-OUTPUTS", "Missing SU2 history.csv.");

        var lines = ReadAllTextShared(historyPath)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();
        if (lines.Length < 2)
            throw new ContractError("CFD-HARVEST-COLUMNS", "history.csv has no data rows.");

        string[] headers = SplitCsvLine(lines[0]);
        var col = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < headers.Length; i++)
            col[headers[i]] = i;

        bool hasIter = col.ContainsKey("Inner_Iter") || col.ContainsKey("Time_Iter");
        if (!hasIter)
            throw new ContractError("CFD-HARVEST-COLUMNS", "history.csv missing required column 'Inner_Iter' or 'Time_Iter'.");
        int rmsPIdx = RequireColumn(col, "rms[P]", "history.csv");
        int clIdx = RequireColumn(col, "CL", "history.csv");
        int cdIdx = RequireColumn(col, "CD", "history.csv");
        int cmIdx = RequireColumn(col, "CMz", "history.csv");

        var rmsCols = col.Where(kv => kv.Key.StartsWith("rms[", StringComparison.Ordinal) && kv.Key.EndsWith(']')).ToArray();
        var firstRms = new Dictionary<string, double>(StringComparer.Ordinal);
        var finalRms = new Dictionary<string, double>(StringComparer.Ordinal);
        var clList = new List<double>();
        var cdList = new List<double>();
        var cmList = new List<double>();

        for (int r = 1; r < lines.Length; r++)
        {
            string[] parts = SplitCsvLine(lines[r]);
            if (parts.Length < headers.Length) continue;
            double cl = ParseDouble(parts[clIdx], "history.csv", "CL");
            double cd = ParseDouble(parts[cdIdx], "history.csv", "CD");
            double cm = ParseDouble(parts[cmIdx], "history.csv", "CMz");
            clList.Add(cl);
            cdList.Add(cd);
            cmList.Add(cm);

            foreach (var (name, idx) in rmsCols)
            {
                double val = ParseDouble(parts[idx], "history.csv", name);
                if (!firstRms.ContainsKey(name)) firstRms[name] = val;
                finalRms[name] = val;
            }
        }
        _ = rmsPIdx;

        if (clList.Count == 0)
            throw new ContractError("CFD-NO-OUTPUTS", "history.csv contained zero valid rows.");

        var eqResiduals = new Dictionary<string, (double First, double Final)>(StringComparer.Ordinal);
        foreach (var (name, firstVal) in firstRms)
        {
            double finalVal = finalRms[name];
            // SU2 logs log10(rms) (<= 0); convert to linear residual equivalents so DR-F2-1 ratio final/first is exact.
            if (firstVal <= 0.0 && finalVal <= 0.0)
                eqResiduals[name] = (1.0, Math.Pow(10.0, finalVal - firstVal));
            else
                eqResiduals[name] = (firstVal, finalVal);
        }

        int windowLen = Math.Min(model.AveragingWindowIterations, clList.Count);
        var summary = CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(
            EquationInitialResiduals: eqResiduals,
            ClWindow: clList.Skip(clList.Count - windowLen).ToArray(),
            CdWindow: cdList.Skip(cdList.Count - windowLen).ToArray(),
            CmWindow: cmList.Skip(cmList.Count - windowLen).ToArray(),
            ResidualDropTarget: model.ResidualDropTarget,
            ClHalfBandCap: model.ClHalfBandCap,
            CdHalfBandCap: model.CdHalfBandCap,
            TurbulenceBoundingCountInWindow: CountTurbulenceBounding(solverLog),
            RanToIterationLimit: clList.Count >= model.MaxIterations));

        string histHash = ComputeFileBlake3(historyPath);
        var histEvidence = CfdRunRecords.Seal(new FieldEvidenceRecord(
            EvidenceId: Guid.NewGuid().ToString("D"),
            RunId: runId,
            ContentHash: "",
            Kind: "history",
            RelativePath: "history.csv",
            ArtifactHash: histHash,
            MeshHash: meshHash,
            Fields: ["CL", "CD", "CMz"],
            Association: "strip",
            Ranges:
            [
                new FieldRangeRecord("CL", "dimensionless", clList.Min(), clList.Max(), "sample-range"),
                new FieldRangeRecord("CD", "dimensionless", cdList.Min(), cdList.Max(), "sample-range")
            ],
            ColormapPolicyVersion: "crameri-8.0",
            ReductionTool: "cfdw-harvester",
            ReductionToolVersion: "1.0.0",
            HasWallShearStress: false,
            SeparationCriterion: null,
            HasTurbulentKineticEnergy: model.TurbulenceModel == TurbulenceModelKind.KOmegaSst,
            IsTransient: false,
            Timesteps: []));

        return new HarvestResult(summary, null, null, null, false, [histEvidence], clList.Count.ToString(CultureInfo.InvariantCulture), 1);
    }

    /// <summary>
    /// Harvester-validated checkpoint check (RUN-04, docs/design/cfd-run-pipeline.md §7.2):
    /// validates cfdw-manifest.json hash and non-empty header-valid latest time directory fields.
    /// </summary>
    public static CheckpointValidationResult ValidateCheckpoint(string caseDir, string expectedManifestHash, CfdBackendKind backend)
    {
        if (string.IsNullOrWhiteSpace(caseDir) || !Directory.Exists(caseDir))
            return Invalid("Case directory does not exist.");

        string manifestPath = Path.Combine(caseDir, "cfdw-manifest.json");
        if (!File.Exists(manifestPath))
            return Invalid("cfdw-manifest.json is missing.");

        string observedHash = ComputeFileBlake3(manifestPath);
        if (!string.Equals(observedHash, expectedManifestHash, StringComparison.Ordinal))
            return Invalid("cfdw-manifest.json hash does not match the recorded case manifest.");

        if (backend == CfdBackendKind.Su2)
        {
            string restartPath = Path.Combine(caseDir, "restart_flow.dat");
            if (!File.Exists(restartPath) || new FileInfo(restartPath).Length == 0)
                return Invalid("SU2 restart_flow.dat is missing or empty.");
            return new CheckpointValidationResult(true, "restart", observedHash, null, null);
        }

        string? latestTime = FindLatestPositiveTimeDir(caseDir);
        if (latestTime is null)
            return Invalid("No positive numeric time directory found.");

        string timePath = Path.Combine(caseDir, latestTime);
        string[] requiredBase = ["U", "p"];
        foreach (string field in requiredBase)
        {
            if (!HasValidFoamHeader(Path.Combine(timePath, field)))
                return Invalid($"Checkpoint time directory '{latestTime}' missing valid field '{field}'.");
        }

        bool hasSa = HasValidFoamHeader(Path.Combine(timePath, "nuTilda"));
        bool hasSst = HasValidFoamHeader(Path.Combine(timePath, "k")) && HasValidFoamHeader(Path.Combine(timePath, "omega"));
        if (!hasSa && !hasSst)
            return Invalid($"Checkpoint time directory '{latestTime}' missing valid turbulence field(s).");

        return new CheckpointValidationResult(true, latestTime, observedHash, null, null);

        static CheckpointValidationResult Invalid(string reason) =>
            new(false, null, null, "CFD-CHECKPOINT-INVALID", reason);
    }

    private static (List<double> Cl, List<double> Cd, List<double> Cm) ParseForceCoeffs(string path)
    {
        var (headers, rows) = ParseWhitespaceTable(path, "coefficient.dat");
        int timeIdx = RequireColumn(headers, "Time", "coefficient.dat");
        int cdIdx = RequireColumn(headers, "Cd", "coefficient.dat");
        int clIdx = RequireColumn(headers, "Cl", "coefficient.dat");
        int cmIdx = headers.TryGetValue("CmPitch", out int cmp) ? cmp
            : headers.TryGetValue("Cm", out int cmCol) ? cmCol
            : throw new ContractError("CFD-HARVEST-COLUMNS", "coefficient.dat missing required column 'CmPitch' or 'Cm'.");
        _ = timeIdx;

        var cl = new List<double>(rows.Count);
        var cd = new List<double>(rows.Count);
        var cm = new List<double>(rows.Count);
        foreach (var row in rows)
        {
            if (row.Length <= Math.Max(Math.Max(cdIdx, clIdx), cmIdx)) continue;
            cd.Add(ParseDouble(row[cdIdx], "coefficient.dat", "Cd"));
            cl.Add(ParseDouble(row[clIdx], "coefficient.dat", "Cl"));
            cm.Add(ParseDouble(row[cmIdx], "coefficient.dat", "CmPitch"));
        }
        if (cl.Count == 0)
            throw new ContractError("CFD-NO-OUTPUTS", "coefficient.dat contains no numeric rows.");
        return (cl, cd, cm);
    }

    private static Dictionary<string, (double First, double Final)> ParseSolverInfo(string path)
    {
        var (headers, rows) = ParseWhitespaceTable(path, "solverInfo.dat");
        RequireColumn(headers, "Time", "solverInfo.dat");

        // Require p_initial, p_final, p_converged and at least one velocity + turbulence initial/final pair.
        RequireColumn(headers, "p_initial", "solverInfo.dat");
        RequireColumn(headers, "p_final", "solverInfo.dat");
        RequireColumn(headers, "p_converged", "solverInfo.dat");

        bool hasU = (headers.ContainsKey("Ux_initial") && headers.ContainsKey("Ux_final")) ||
                    (headers.ContainsKey("U_initial") && headers.ContainsKey("U_final"));
        bool hasUConv = headers.ContainsKey("U_converged") || headers.ContainsKey("Ux_converged");
        if (!hasU || !hasUConv)
            throw new ContractError("CFD-HARVEST-COLUMNS", "solverInfo.dat missing required U initial/final/converged columns.");

        var eqCols = new List<(string Eq, int InitialIdx, int FinalIdx)>();
        foreach (var (name, idx) in headers)
        {
            if (name.EndsWith("_initial", StringComparison.Ordinal))
            {
                string eq = name[..^"_initial".Length];
                if (!headers.TryGetValue(eq + "_final", out int finalIdx))
                    throw new ContractError("CFD-HARVEST-COLUMNS", $"solverInfo.dat missing '{eq}_final' column.");
                eqCols.Add((eq, idx, finalIdx));
            }
        }

        if (rows.Count == 0 || eqCols.Count == 0)
            throw new ContractError("CFD-NO-OUTPUTS", "solverInfo.dat contains no data rows.");

        var result = new Dictionary<string, (double First, double Final)>(StringComparer.Ordinal);
        var firstRow = rows[0];
        var lastRow = rows[^1];
        foreach (var (eq, initIdx, _) in eqCols)
        {
            if (initIdx >= firstRow.Length || initIdx >= lastRow.Length)
                throw new ContractError("CFD-HARVEST-COLUMNS", $"solverInfo.dat row truncated for '{eq}_initial'.");
            double firstInit = ParseDouble(firstRow[initIdx], "solverInfo.dat", eq + "_initial");
            double finalInit = ParseDouble(lastRow[initIdx], "solverInfo.dat", eq + "_initial");
            result[eq] = (firstInit, finalInit);
        }
        return result;
    }

    private static (double Min, double Max, double P95) ParseYPlus(string path)
    {
        var (headers, rows) = ParseWhitespaceTable(path, "yPlus.dat");
        RequireColumn(headers, "Time", "yPlus.dat");
        RequireColumn(headers, "patch", "yPlus.dat");
        int minIdx = RequireColumn(headers, "min", "yPlus.dat");
        int maxIdx = RequireColumn(headers, "max", "yPlus.dat");
        int avgIdx = RequireColumn(headers, "average", "yPlus.dat");
        bool hasP95 = headers.TryGetValue("p95", out int p95Idx);
        _ = avgIdx;

        if (rows.Count == 0)
            throw new ContractError("CFD-NO-OUTPUTS", "yPlus.dat contains no data rows.");

        var last = rows[^1];
        double min = ParseDouble(last[minIdx], "yPlus.dat", "min");
        double max = ParseDouble(last[maxIdx], "yPlus.dat", "max");
        double p95 = hasP95 && p95Idx < last.Length ? ParseDouble(last[p95Idx], "yPlus.dat", "p95") : max;
        return (min, max, p95);
    }

    private static void ValidateWallShearStress(string path)
    {
        var (headers, rows) = ParseWhitespaceTable(path, "wallShearStress.dat");
        RequireColumn(headers, "Time", "wallShearStress.dat");
        RequireColumn(headers, "patch", "wallShearStress.dat");
        RequireColumn(headers, "min", "wallShearStress.dat");
        RequireColumn(headers, "max", "wallShearStress.dat");
        RequireColumn(headers, "integral", "wallShearStress.dat");
        if (rows.Count == 0)
            throw new ContractError("CFD-NO-OUTPUTS", "wallShearStress.dat contains no data rows.");
    }

    private static FieldEvidenceRecord ParseSurfaceSample(
        string caseDir,
        string fullPath,
        string runId,
        string meshHash,
        bool hasWssFile,
        bool hasTke)
    {
        var (headers, rows) = ParseWhitespaceTable(fullPath, Path.GetFileName(fullPath));
        RequireColumn(headers, "x", Path.GetFileName(fullPath));
        RequireColumn(headers, "y", Path.GetFileName(fullPath));
        RequireColumn(headers, "z", Path.GetFileName(fullPath));

        var fieldCols = headers.Where(kv => kv.Key is not ("x" or "y" or "z")).OrderBy(kv => kv.Value).ToArray();
        if (fieldCols.Length == 0 || rows.Count == 0)
            throw new ContractError("CFD-HARVEST-COLUMNS", $"{Path.GetFileName(fullPath)} missing field columns or sample rows.");

        var ranges = new List<FieldRangeRecord>();
        var fields = new List<string>();
        bool sampleHasWss = false;

        foreach (var (fieldName, idx) in fieldCols)
        {
            fields.Add(fieldName);
            if (string.Equals(fieldName, "wallShearStress", StringComparison.Ordinal))
                sampleHasWss = true;

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;
            foreach (var row in rows)
            {
                if (idx >= row.Length) continue;
                double v = ParseDouble(row[idx], Path.GetFileName(fullPath), fieldName);
                if (v < min) min = v;
                if (v > max) max = v;
            }
            string unit = fieldName is "Cp" or "Cf" ? "dimensionless" : fieldName is "p" or "wallShearStress" ? "Pa" : "SI";
            ranges.Add(new FieldRangeRecord(fieldName, unit, min, max, "sample-range"));
        }

        bool hasWallShearStress = hasWssFile && sampleHasWss;
        string relPath = Path.GetRelativePath(caseDir, fullPath).Replace('\\', '/');
        return CfdRunRecords.Seal(new FieldEvidenceRecord(
            EvidenceId: Guid.NewGuid().ToString("D"),
            RunId: runId,
            ContentHash: "",
            Kind: "surface",
            RelativePath: relPath,
            ArtifactHash: ComputeFileBlake3(fullPath),
            MeshHash: meshHash,
            Fields: fields,
            Association: "face",
            Ranges: ranges,
            ColormapPolicyVersion: "crameri-8.0",
            ReductionTool: "cfdw-harvester",
            ReductionToolVersion: "1.0.0",
            HasWallShearStress: hasWallShearStress,
            SeparationCriterion: hasWallShearStress ? "skin-friction-line convergence" : null,
            HasTurbulentKineticEnergy: hasTke,
            IsTransient: false,
            Timesteps: []));
    }

    private static (Dictionary<string, int> Headers, List<string[]> Rows) ParseWhitespaceTable(string path, string fileName)
    {
        string text = ReadAllTextShared(path);
        var lines = text.Split('\n');
        string? headerLine = null;
        var rows = new List<string[]>();

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('#'))
            {
                string candidate = line.TrimStart('#').Trim();
                if (candidate.Length > 0) headerLine = candidate;
                continue;
            }
            rows.Add(line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        if (headerLine is null)
            throw new ContractError("CFD-HARVEST-COLUMNS", $"{fileName} missing column header line.");

        string[] colNames = headerLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < colNames.Length; i++)
            headers[colNames[i]] = i;

        return (headers, rows);
    }

    private static int RequireColumn(Dictionary<string, int> headers, string name, string fileName)
    {
        if (!headers.TryGetValue(name, out int idx))
            throw new ContractError("CFD-HARVEST-COLUMNS", $"{fileName} missing required column '{name}'.");
        return idx;
    }

    private static double ParseDouble(string token, string fileName, string col)
    {
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) || !double.IsFinite(v))
            throw new ContractError("CFD-HARVEST-COLUMNS", $"{fileName} column '{col}' contains non-numeric value '{token}'.");
        return v;
    }

    private static string[] SplitCsvLine(string line) =>
        line.Split(',').Select(part => part.Trim().Trim('"').Trim()).ToArray();

    private static int CountTurbulenceBounding(string? solverLog)
    {
        if (string.IsNullOrEmpty(solverLog)) return 0;
        return BoundingRegex.Matches(solverLog).Count;
    }

    private static string? FindLatestPositiveTimeDir(string caseDir)
    {
        double bestTime = 0.0;
        string? bestName = null;
        foreach (string dir in Directory.GetDirectories(caseDir))
        {
            string name = Path.GetFileName(dir);
            if (double.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out double t) && double.IsFinite(t) && t > bestTime)
            {
                bestTime = t;
                bestName = name;
            }
        }
        return bestName;
    }

    private static string? FindLatestPostProcessingFile(string caseDir, string objectName, string fileName)
    {
        string objDir = Path.Combine(caseDir, "postProcessing", objectName);
        if (!Directory.Exists(objDir)) return null;
        double bestTime = double.NegativeInfinity;
        string? bestPath = null;
        foreach (string tDir in Directory.GetDirectories(objDir))
        {
            string name = Path.GetFileName(tDir);
            if (!double.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out double t) || !double.IsFinite(t))
                continue;
            string candidate = Path.Combine(tDir, fileName);
            if (File.Exists(candidate) && t >= bestTime)
            {
                bestTime = t;
                bestPath = candidate;
            }
        }
        return bestPath;
    }

    private static string? FindLatestSurfaceSample(string caseDir, string objectName)
    {
        string objDir = Path.Combine(caseDir, "postProcessing", objectName);
        if (!Directory.Exists(objDir)) return null;
        double bestTime = double.NegativeInfinity;
        string? bestPath = null;
        foreach (string tDir in Directory.GetDirectories(objDir))
        {
            string name = Path.GetFileName(tDir);
            if (!double.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out double t) || !double.IsFinite(t))
                continue;
            string? raw = Directory.GetFiles(tDir, "*.raw").OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (raw is not null && t >= bestTime)
            {
                bestTime = t;
                bestPath = raw;
            }
        }
        return bestPath;
    }

    private static bool HasValidFoamHeader(string path)
    {
        if (!File.Exists(path)) return false;
        string content = ReadAllTextShared(path);
        return content.Length > 0 && content.Contains("FoamFile", StringComparison.Ordinal);
    }

    /// <summary>READER-SHARE compliant text reader using explicit FileShare.Read | FileShare.Delete.</summary>
    internal static string ReadAllTextShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var reader = new StreamReader(fs, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>READER-SHARE compliant byte reader using explicit FileShare.Read | FileShare.Delete.</summary>
    internal static byte[] ReadAllBytesShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }
}
