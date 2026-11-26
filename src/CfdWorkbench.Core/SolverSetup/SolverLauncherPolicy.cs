using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.SolverSetup;

public sealed record LauncherCommand(string Executable, IReadOnlyList<string> Arguments);

public sealed record BannerCheckResult(
    bool Passed,
    bool BannerFound,
    string? BannerStatus,
    string? BuildId,
    string? CauseCode,
    string Reason);

/// <summary>
/// Pure zero-I/O security policy for the right-sized M1-M7 Product Launcher
/// (Ruling 68, docs/notes/solver-security-right-size.md §5).
/// </summary>
public static class SolverLauncherPolicy
{
    private static readonly Regex SafeRunKeyRegex = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant);
    private static readonly Regex BuildIdRegex = new(@"^Build\s*:\s*([^\s]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex CourantMeanRegex = new(@"Courant Number mean:\s*([+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)", RegexOptions.CultureInvariant);

    public static string BinaryName(OpenFoamApp app) => app switch
    {
        OpenFoamApp.FoamVersion => "foamVersion",
        OpenFoamApp.BlockMesh => "blockMesh",
        OpenFoamApp.IcoFoam => "icoFoam",
        OpenFoamApp.CheckMesh => "checkMesh",
        OpenFoamApp.SimpleFoam => "simpleFoam",
        OpenFoamApp.PotentialFoam => "potentialFoam",
        OpenFoamApp.PimpleFoam => "pimpleFoam",
        OpenFoamApp.DecomposePar => "decomposePar",
        OpenFoamApp.ReconstructPar => "reconstructPar",
        _ => throw new ArgumentOutOfRangeException(nameof(app))
    };

    /// <summary>M5 run-key validation: refuses traversal, path separators, NUL, or non-ASCII characters.</summary>
    public static void ValidateRunKey(string runKey)
    {
        if (string.IsNullOrEmpty(runKey) || !SafeRunKeyRegex.IsMatch(runKey))
            throw new ContractError("SETUP-RUN-KEY", $"Invalid solver run key '{runKey}'.");
    }

    /// <summary>M1: emits the app-owned HOME controlDict turning off case-supplied code execution.</summary>
    public static string EmitUserControlDict() =>
        "/*--------------------------------*- C++ -*----------------------------------*\\\n" +
        "| CFD Workbench app-owned OpenFOAM controlDict (Ruling 68, M1)                |\n" +
        "\\*---------------------------------------------------------------------------*/\n" +
        "FoamFile\n" +
        "{\n" +
        "    version     2.0;\n" +
        "    format      ascii;\n" +
        "    class       dictionary;\n" +
        "    object      controlDict;\n" +
        "}\n" +
        "\n" +
        "InfoSwitches\n" +
        "{\n" +
        "    allowSystemOperations 0;\n" +
        "}\n" +
        "\n" +
        "OptimisationSwitches\n" +
        "{\n" +
        "    stopAtWriteNowSignal 30;\n" +
        "}\n";

    /// <summary>
    /// M4: emits the bundled cavity smoke-test case strictly from typed values.
    /// Foreign design names never enter any OpenFOAM dictionary; all numbers are finite and invariant-culture.
    /// Output is verified in CI against cases/tools/foam-dict-lint.py.
    /// </summary>
    public static IReadOnlyDictionary<string, string> EmitCavityCaseFiles(
        string runKey,
        string? designName = null,
        double endTime = 0.5,
        double deltaT = 0.005,
        double kinematicViscosity = 0.01)
    {
        ValidateRunKey(runKey);
        _ = designName; // Foreign design names are intentionally never interpolated into solver dictionaries (M4).
        RequireFinitePositive(endTime, nameof(endTime));
        RequireFinitePositive(deltaT, nameof(deltaT));
        RequireFinitePositive(kinematicViscosity, nameof(kinematicViscosity));

        string endTimeStr = endTime.ToString("G17", CultureInfo.InvariantCulture);
        string deltaTStr = deltaT.ToString("G17", CultureInfo.InvariantCulture);
        string nuStr = kinematicViscosity.ToString("G17", CultureInfo.InvariantCulture);

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["system/controlDict"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       dictionary;\n    object      controlDict;\n}\n\n" +
                "application     icoFoam;\n" +
                "startFrom       startTime;\n" +
                "startTime       0;\n" +
                "stopAt          endTime;\n" +
                $"endTime         {endTimeStr};\n" +
                $"deltaT          {deltaTStr};\n" +
                "writeControl    timeStep;\n" +
                "writeInterval   20;\n" +
                "purgeWrite      0;\n" +
                "writeFormat     ascii;\n" +
                "writePrecision  6;\n" +
                "writeCompression off;\n" +
                "timeFormat      general;\n" +
                "timePrecision   6;\n" +
                "runTimeModifiable false;\n",

            ["system/fvSchemes"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       dictionary;\n    object      fvSchemes;\n}\n\n" +
                "ddtSchemes\n{\n    default         Euler;\n}\ngradSchemes\n{\n    default         Gauss linear;\n}\n" +
                "divSchemes\n{\n    default         none;\n    div(phi,U)      Gauss linear;\n}\n" +
                "laplacianSchemes\n{\n    default         Gauss linear orthogonal;\n}\n" +
                "interpolationSchemes\n{\n    default         linear;\n}\n" +
                "snGradSchemes\n{\n    default         orthogonal;\n}\n",

            ["system/fvSolution"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       dictionary;\n    object      fvSolution;\n}\n\n" +
                "solvers\n{\n    p\n    {\n        solver          PCG;\n        preconditioner  DIC;\n        tolerance       1e-06;\n        relTol          0.05;\n    }\n" +
                "    pFinal\n    {\n        solver          PCG;\n        preconditioner  DIC;\n        tolerance       1e-06;\n        relTol          0;\n    }\n" +
                "    U\n    {\n        solver          smoothSolver;\n        smoother        symGaussSeidel;\n        tolerance       1e-05;\n        relTol          0;\n    }\n}\n" +
                "PISO\n{\n    nCorrectors     2;\n    nNonOrthogonalCorrectors 0;\n    pRefCell        0;\n    pRefValue       0;\n}\n",

            ["system/blockMeshDict"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       dictionary;\n    object      blockMeshDict;\n}\n\n" +
                "scale   0.1;\n" +
                "vertices\n(\n    (0 0 0)\n    (1 0 0)\n    (1 1 0)\n    (0 1 0)\n    (0 0 0.1)\n    (1 0 0.1)\n    (1 1 0.1)\n    (0 1 0.1)\n);\n" +
                "blocks\n(\n    hex (0 1 2 3 4 5 6 7) (20 20 1) simpleGrading (1 1 1)\n);\n" +
                "edges\n(\n);\n" +
                "boundary\n(\n" +
                "    movingWall\n    {\n        type wall;\n        faces\n        (\n            (3 7 6 2)\n        );\n    }\n" +
                "    fixedWalls\n    {\n        type wall;\n        faces\n        (\n            (0 4 7 3)\n            (2 6 5 1)\n            (1 5 4 0)\n        );\n    }\n" +
                "    frontAndBack\n    {\n        type empty;\n        faces\n        (\n            (0 3 2 1)\n            (4 5 6 7)\n        );\n    }\n);\n",

            ["constant/transportProperties"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       dictionary;\n    object      transportProperties;\n}\n\n" +
                $"nu              [0 2 -1 0 0 0 0] {nuStr};\n",

            ["0/U"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       volVectorField;\n    object      U;\n}\n\n" +
                "dimensions      [0 1 -1 0 0 0 0];\n" +
                "internalField   uniform (0 0 0);\n" +
                "boundaryField\n{\n" +
                "    movingWall\n    {\n        type            fixedValue;\n        value           uniform (1 0 0);\n    }\n" +
                "    fixedWalls\n    {\n        type            noSlip;\n    }\n" +
                "    frontAndBack\n    {\n        type            empty;\n    }\n}\n",

            ["0/p"] =
                "FoamFile\n{\n    version     2.0;\n    format      ascii;\n    class       volScalarField;\n    object      p;\n}\n\n" +
                "dimensions      [0 2 -2 0 0 0 0];\n" +
                "internalField   uniform 0;\n" +
                "boundaryField\n{\n" +
                "    movingWall\n    {\n        type            zeroGradient;\n    }\n" +
                "    fixedWalls\n    {\n        type            zeroGradient;\n    }\n" +
                "    frontAndBack\n    {\n        type            empty;\n    }\n}\n"
        };

        var manifestKeys = files.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var manifestJson = new StringBuilder("[\n");
        for (int i = 0; i < manifestKeys.Length; i++)
        {
            manifestJson.Append("  \"").Append(manifestKeys[i]).Append('"');
            if (i + 1 < manifestKeys.Length) manifestJson.Append(',');
            manifestJson.Append('\n');
        }
        manifestJson.Append("]\n");
        files["cfdw-manifest.json"] = manifestJson.ToString();
        return files;
    }

    /// <summary>M2: inspects master-rank live output for the Disallowing banner and pinned build id.</summary>
    public static BannerCheckResult ParseMasterBanner(string liveOutput, string? expectedBuildId, bool allowUnverifiedBuild = false)
    {
        string text = liveOutput ?? "";
        var buildMatch = BuildIdRegex.Match(text);
        string? buildId = buildMatch.Success ? buildMatch.Groups[1].Value.Trim() : null;

        bool sawDisallowing = text.Contains("Disallowing", StringComparison.Ordinal);
        bool sawAllowing = Regex.IsMatch(text, @"\bAllowing\b", RegexOptions.CultureInvariant);

        if (sawAllowing)
        {
            return new BannerCheckResult(
                Passed: false, BannerFound: true, BannerStatus: "Allowing", BuildId: buildId,
                CauseCode: "smoke.banner", Reason: "OpenFOAM reported 'Allowing' instead of 'Disallowing'.");
        }

        if (!sawDisallowing)
        {
            return new BannerCheckResult(
                Passed: false, BannerFound: false, BannerStatus: null, BuildId: buildId,
                CauseCode: "smoke.banner", Reason: "Master-rank 'Disallowing' banner was missing from live output.");
        }

        if (!allowUnverifiedBuild && !string.IsNullOrEmpty(expectedBuildId) &&
            !string.Equals(buildId, expectedBuildId, StringComparison.Ordinal))
        {
            return new BannerCheckResult(
                Passed: false, BannerFound: true, BannerStatus: "Disallowing", BuildId: buildId,
                CauseCode: "smoke.build-mismatch",
                Reason: $"Build id '{buildId ?? "(missing)"}' does not match pinned '{expectedBuildId}'.");
        }

        return new BannerCheckResult(
            Passed: true, BannerFound: true, BannerStatus: "Disallowing", BuildId: buildId,
            CauseCode: null, Reason: "Master-rank Disallowing banner verified.");
    }

    public static double? ParseFinalCourantMean(string liveOutput)
    {
        if (string.IsNullOrEmpty(liveOutput)) return null;
        var matches = CourantMeanRegex.Matches(liveOutput);
        if (matches.Count == 0) return null;
        string raw = matches[^1].Groups[1].Value;
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value)
            ? value
            : null;
    }

    /// <summary>
    /// Pure zero-I/O conversion from an absolute Windows drive path (<c>D:\runs\k\.home</c>)
    /// to its WSL mount path (<c>/mnt/d/runs/k/.home</c>). Refuses relative paths, UNC paths, traversal, and control characters.
    /// </summary>
    public static string ToWslPath(string windowsPath)
    {
        if (string.IsNullOrWhiteSpace(windowsPath))
            throw new ContractError("SETUP-LAUNCHER-ARG", "Windows path is required for WSL conversion.");
        if (ContainsControlChar(windowsPath))
            throw new ContractError("SETUP-LAUNCHER-ARG", "Control characters are forbidden in WSL paths.");
        if (windowsPath.Length < 3 ||
            !char.IsAsciiLetter(windowsPath[0]) ||
            windowsPath[1] != ':' ||
            (windowsPath[2] != '\\' && windowsPath[2] != '/'))
        {
            throw new ContractError("SETUP-LAUNCHER-ARG", $"Expected rooted Windows drive path, got '{windowsPath}'.");
        }

        string rest = windowsPath[2..].Replace('\\', '/');
        string[] segments = rest.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (string seg in segments)
        {
            if (seg == "..")
                throw new ContractError("SETUP-LAUNCHER-ARG", "Path traversal ('..') is forbidden in WSL paths.");
        }

        char drive = char.ToLowerInvariant(windowsPath[0]);
        return segments.Length == 0
            ? $"/mnt/{drive}"
            : $"/mnt/{drive}/{string.Join('/', segments)}";
    }

    /// <summary>M3: argv-only launcher command builder from typed enum (never -c or shell strings).</summary>
    public static LauncherCommand BuildArgv(
        SetupOs os,
        string activationPathOrDistro,
        OpenFoamApp app,
        IReadOnlyList<string>? typedArgs = null,
        string? wslHomePath = null)
    {
        if (string.IsNullOrWhiteSpace(activationPathOrDistro))
            throw new ContractError("SETUP-LAUNCHER-ARG", "Activation path or WSL distro name is required.");

        string appName = BinaryName(app);
        var extra = ValidateTypedArgs(typedArgs);

        if (os == SetupOs.MacOs)
        {
            var args = new List<string> { "-f", "-e", activationPathOrDistro, "--", appName };
            args.AddRange(extra);
            return new LauncherCommand("/bin/zsh", args);
        }

        if (os == SetupOs.Windows)
        {
            // WSL ignores Windows HOME and restores /etc/passwd HOME unless set inside Linux via /usr/bin/env -i (WSL-CLEAN-ENV-M1).
            // Fail closed if wslHomePath is missing, relative, contains traversal, or contains control characters.
            if (string.IsNullOrWhiteSpace(wslHomePath) ||
                !wslHomePath.StartsWith('/') ||
                ContainsControlChar(wslHomePath) ||
                wslHomePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(seg => seg == ".."))
            {
                throw new ContractError("SETUP-LAUNCHER-ARG",
                    "Windows WSL launch requires a clean absolute POSIX HOME path without traversal or control characters.");
            }

            var args = new List<string>
            {
                "-d", activationPathOrDistro, "-e",
                "/usr/bin/env", "-i",
                "PATH=/usr/bin:/bin",
                "HOME=" + wslHomePath,
                "USER=cfdw",
                "LOGNAME=cfdw",
                "LANG=C",
                "OMP_NUM_THREADS=1",
                "OPENBLAS_NUM_THREADS=1",
                "/usr/bin/openfoam2512",
                appName
            };
            args.AddRange(extra);
            return new LauncherCommand("wsl.exe", args);
        }

        throw new ContractError("SETUP-OS-UNSUPPORTED", $"Unsupported launcher OS '{os}'.");
    }

    private static bool ContainsControlChar(string value)
    {
        foreach (char c in value)
        {
            if (char.IsControl(c)) return true;
        }
        return false;
    }

    /// <summary>M6: strictly allowlisted child environment stripping FOAM_CONTROLDICT, WM_*, and shell hooks.</summary>
    public static IReadOnlyDictionary<string, string> BuildCleanEnvironment(
        string appOwnedHome,
        string pathValue,
        string userName = "cfdw",
        string? windowsSystemRoot = null)
    {
        if (string.IsNullOrWhiteSpace(appOwnedHome))
            throw new ContractError("SETUP-HOME-EMPTY", "App-owned HOME is required.");
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PATH"] = pathValue,
            ["HOME"] = appOwnedHome,
            ["USER"] = userName,
            ["LOGNAME"] = userName,
            ["LANG"] = "C"
        };
        // wsl.exe RPC fails with RPC_S_SYSTEM_HANDLE_TYPE_MISMATCH if SystemRoot is omitted on Windows (WSL-CLEAN-ENV-M1).
        if (!string.IsNullOrWhiteSpace(windowsSystemRoot))
            env["SystemRoot"] = windowsSystemRoot;
        return env;
    }

    /// <summary>M7: core-based rank default (clamped 1..6), which the user may lower.</summary>
    public static int DefaultRankCount(int processorCount, int? userRequestedRanks = null)
    {
        int defaultRanks = Math.Clamp(Math.Max(1, processorCount / 2), 1, 6);
        if (userRequestedRanks is { } requested)
            return Math.Clamp(requested, 1, defaultRanks);
        return defaultRanks;
    }

    private static IReadOnlyList<string> ValidateTypedArgs(IReadOnlyList<string>? typedArgs)
    {
        if (typedArgs is null || typedArgs.Count == 0) return [];
        var list = new List<string>(typedArgs.Count);
        foreach (string arg in typedArgs)
        {
            if (arg.Contains('\0') || arg.Contains('\n') || arg.Contains('\r'))
                throw new ContractError("SETUP-LAUNCHER-ARG", "Control characters are forbidden in launcher arguments.");
            if (string.Equals(arg, "-c", StringComparison.Ordinal))
                throw new ContractError("SETUP-LAUNCHER-ARG", "Shell flag '-c' is forbidden (M3).");
            list.Add(arg);
        }
        return list;
    }

    private static void RequireFinitePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ContractError("SETUP-NONFINITE", $"Parameter '{name}' must be finite and positive.");
    }
}
