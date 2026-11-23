using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.CfdRun;

/// <summary>
/// Pure deterministic OpenFOAM ESI v2512 case emitter (docs/design/cfd-run-pipeline.md §4.2, C2, M4).
/// Strictly honors cases/tools/foam-dict-lint.py 21-entry TYPES_ALLOWED, LIBS_ALLOWED, runTimeModifiable false,
/// and cfdw-manifest.json. M1 allowSystemOperations 0 lives strictly in SolverLauncher HOME controlDict.
/// </summary>
public static class OpenFoamCaseEmitter
{
    public static readonly IReadOnlySet<string> TypesAllowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "forceCoeffs", "solverInfo", "yPlus", "surfaces", "wallShearStress",
        "patch", "wall", "empty", "symmetryPlane", "freestreamVelocity",
        "freestreamPressure", "noSlip", "zeroGradient", "inletOutlet", "fixedValue",
        "calculated", "triSurfaceMesh", "box", "cellSet", "hePsiThermo", "symmetry"
    };

    public static readonly IReadOnlySet<string> LibsAllowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "forces", "utilityFunctionObjects", "fieldFunctionObjects", "sampling"
    };

    private static readonly string[] RefusedWords =
        ["systemCall", "InfoSwitches", "OptimisationSwitches", "DebugSwitches", "DimensionSets"];

    private static readonly Regex TokenRegex = new(
        @"""(?:[^""\\]|\\.)*""|/\*.*?\*/|//[^\n]*|#\{|#\}|#[A-Za-z_]*|[A-Za-z_][\w.:<>,|*+-]*|-?[\d.][\w.+-]*|\S",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>Emits a complete deterministic OpenFOAM ESI v2512 case file map (LF endings, invariant-culture G17).</summary>
    public static IReadOnlyDictionary<string, string> EmitCaseFiles(TypedCaseModel model, bool startFromLatestTime = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.ValidateInputs();

        if (model.WallTreatment == WallTreatmentKind.WallFunction)
        {
            throw new ContractError("CFD-UNSUPPORTED-BC",
                "OpenFOAM Wave 10 cases emit wall-resolved BCs only (wall-function BC types are outside foam-dict-lint TYPES_ALLOWED).");
        }

        var uInf = model.InflowVelocity();
        var liftDir = model.LiftDirection();
        var dragDir = model.DragDirection();
        var dom = model.EffectiveDomain;
        var mesh = model.EffectiveMesh;

        double cRef = model.Reference.CRef;
        double bRef = model.Reference.BRef;
        double xMin = model.Bounds.MinX - dom.UpstreamChords * cRef;
        double xMax = model.Bounds.MaxX + dom.DownstreamChords * cRef;
        double yMin = 0.0; // Symmetry plane at root y = 0
        double yMax = Math.Max(model.Bounds.MaxY, bRef * 0.5) + dom.LateralHalfSpans * (bRef * 0.5);
        double zMin = model.Bounds.MinZ - dom.VerticalChords * cRef;
        double zMax = model.Bounds.MaxZ + dom.VerticalChords * cRef;

        double refBoxMinX = model.Bounds.MinX - 1.5 * cRef;
        double refBoxMaxX = model.Bounds.MaxX + 4.0 * cRef;
        double refBoxMaxY = Math.Max(model.Bounds.MaxY, bRef * 0.5) + 1.0 * cRef;
        double refBoxMinZ = model.Bounds.MinZ - 1.5 * cRef;
        double refBoxMaxZ = model.Bounds.MaxZ + 1.5 * cRef;

        int nx = Math.Max(12, (int)Math.Round((xMax - xMin) / (mesh.BaseCellSizeMeters * 4.0)));
        int ny = Math.Max(6, (int)Math.Round((yMax - yMin) / (mesh.BaseCellSizeMeters * 4.0)));
        int nz = Math.Max(10, (int)Math.Round((zMax - zMin) / (mesh.BaseCellSizeMeters * 4.0)));

        double nuTildaInf = model.ViscosityRatio * model.Water.Nu;
        double kInf = 1.5 * Math.Pow(model.Op.Speed * model.TurbulenceIntensity, 2.0);
        double omegaInf = kInf / (model.ViscosityRatio * model.Water.Nu);

        string startFrom = startFromLatestTime ? "latestTime" : "startTime";
        string rasModel = model.TurbulenceModel == TurbulenceModelKind.KOmegaSst ? "kOmegaSST" : "SpalartAllmaras";
        string turbSolverFields = model.TurbulenceModel == TurbulenceModelKind.KOmegaSst ? "(U p k omega)" : "(U p nuTilda)";

        var files = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["system/controlDict"] = BuildControlDict(model, startFrom, liftDir, dragDir, turbSolverFields),
            ["system/fvSchemes"] = BuildFvSchemes(model.TurbulenceModel),
            ["system/fvSolution"] = BuildFvSolution(model.TurbulenceModel),
            ["system/blockMeshDict"] = BuildBlockMeshDict(xMin, xMax, yMin, yMax, zMin, zMax, nx, ny, nz),
            ["system/snappyHexMeshDict"] = BuildSnappyHexMeshDict(mesh, refBoxMinX, refBoxMaxX, refBoxMaxY, refBoxMinZ, refBoxMaxZ),
            ["constant/transportProperties"] = BuildTransportProperties(model.Water.Nu),
            ["constant/turbulenceProperties"] = BuildTurbulenceProperties(rasModel),
            ["0/U"] = BuildVelocityField(uInf),
            ["0/p"] = BuildPressureField(),
            ["0/nut"] = BuildNutField()
        };

        if (model.TurbulenceModel == TurbulenceModelKind.KOmegaSst)
        {
            files["0/k"] = BuildScalarTurbField("k", "[0 2 -2 0 0 0 0]", kInf, wallValue: 1e-10);
            files["0/omega"] = BuildScalarTurbField("omega", "[0 0 -1 0 0 0 0]", omegaInf, wallValue: Math.Max(omegaInf * 100.0, 1.0));
        }
        else
        {
            files["0/nuTilda"] = BuildScalarTurbField("nuTilda", "[0 2 -1 0 0 0 0]", nuTildaInf, wallValue: 0.0);
        }

        // Write cfdw-manifest.json listing every emitted file in deterministic sorted order.
        var manifestKeys = files.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var sb = new StringBuilder("[\n");
        for (int i = 0; i < manifestKeys.Length; i++)
        {
            sb.Append("  \"").Append(manifestKeys[i]).Append('"');
            if (i + 1 < manifestKeys.Length) sb.Append(',');
            sb.Append('\n');
        }
        sb.Append("]\n");
        files["cfdw-manifest.json"] = sb.ToString();

        ValidateNoUnsafeTokens(files);
        return files;
    }

    /// <summary>Pure in-process pre-flight mirror of cases/tools/foam-dict-lint.py (M4).</summary>
    public static void ValidateNoUnsafeTokens(IReadOnlyDictionary<string, string> files)
    {
        Guard.Require(files.ContainsKey("cfdw-manifest.json"), "CFD-LINT-REFUSED");
        bool sawRunTimeModifiableFalse = false;

        foreach (var (rel, text) in files)
        {
            if (rel == "cfdw-manifest.json") continue;
            var toks = new List<string>();
            foreach (Match m in TokenRegex.Matches(text))
            {
                string t = m.Value;
                if (t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("/*", StringComparison.Ordinal))
                    continue;
                toks.Add(t);
            }

            string prev = ";";
            for (int i = 0; i < toks.Count; i++)
            {
                string t = toks[i];
                if (t.StartsWith('#'))
                    throw new ContractError("CFD-LINT-REFUSED", $"{rel}: '#' directive '{t}' is refused.");
                if (t.StartsWith('"'))
                {
                    if (prev is "{" or ";" or "}")
                        throw new ContractError("CFD-LINT-REFUSED", $"{rel}: quoted keyword {t} is refused.");
                    string inner = t.Trim('"');
                    if (inner.StartsWith('/') || inner.StartsWith('~') || inner.Contains('$') || inner.Contains("..", StringComparison.Ordinal))
                        throw new ContractError("CFD-LINT-REFUSED", $"{rel}: unsafe string path {t}.");
                }
                else if (t is "$" or "~" or "/")
                {
                    throw new ContractError("CFD-LINT-REFUSED", $"{rel}: '{t}' outside a string is refused.");
                }
                else if (char.IsLetter(t[0]) || t[0] == '_')
                {
                    if (t.Contains("coded", StringComparison.OrdinalIgnoreCase))
                        throw new ContractError("CFD-LINT-REFUSED", $"{rel}: coded keyword '{t}' is refused.");
                    if (Regex.IsMatch(t, @"Libs(\W|$)", RegexOptions.CultureInvariant) ||
                        RefusedWords.Any(w => t.Contains(w, StringComparison.Ordinal)))
                        throw new ContractError("CFD-LINT-REFUSED", $"{rel}: refused keyword '{t}'.");
                    if (t == "type" && prev is "{" or ";" or "}")
                    {
                        string v = i + 1 < toks.Count ? toks[i + 1] : "";
                        if (!TypesAllowed.Contains(v))
                            throw new ContractError("CFD-LINT-REFUSED", $"{rel}: type '{v}' is outside TYPES_ALLOWED.");
                    }
                    else if (t == "libs" && prev is "{" or ";" or "}")
                    {
                        if (i + 1 >= toks.Count || toks[i + 1] != "(")
                            throw new ContractError("CFD-LINT-REFUSED", $"{rel}: libs not followed by '('.");
                        int j = i + 2;
                        int count = 0;
                        while (j < toks.Count && toks[j] != ")")
                        {
                            string lib = toks[j];
                            if (lib.StartsWith('"') || !LibsAllowed.Contains(lib))
                                throw new ContractError("CFD-LINT-REFUSED", $"{rel}: disallowed lib '{lib}'.");
                            count++;
                            j++;
                        }
                        if (count == 0 || j >= toks.Count)
                            throw new ContractError("CFD-LINT-REFUSED", $"{rel}: empty or unterminated libs list.");
                        i = j;
                    }
                    else if (t == "runTimeModifiable" && prev is "{" or ";" or "}")
                    {
                        string v = i + 1 < toks.Count ? toks[i + 1] : "";
                        if (v is not ("false" or "no" or "off"))
                            throw new ContractError("CFD-LINT-REFUSED", $"{rel}: runTimeModifiable must be false.");
                        if (rel == "system/controlDict") sawRunTimeModifiableFalse = true;
                    }
                }
                prev = toks[i];
            }
        }

        if (!sawRunTimeModifiableFalse)
            throw new ContractError("CFD-LINT-REFUSED", "system/controlDict: runTimeModifiable false is required.");
    }

    private static string BuildControlDict(TypedCaseModel model, string startFrom, Vector3D liftDir, Vector3D dragDir, string solverFields)
    {
        int writeInterval = Math.Max(50, model.MaxIterations / 4);
        return
            Header("dictionary", "controlDict") +
            "application     simpleFoam;\n" +
            $"startFrom       {startFrom};\n" +
            "startTime       0;\n" +
            "stopAt          endTime;\n" +
            $"endTime         {model.MaxIterations.ToString(CultureInfo.InvariantCulture)};\n" +
            "deltaT          1;\n" +
            "writeControl    timeStep;\n" +
            $"writeInterval   {writeInterval.ToString(CultureInfo.InvariantCulture)};\n" +
            "purgeWrite      2;\n" +
            "writeFormat     ascii;\n" +
            "writePrecision  8;\n" +
            "writeCompression off;\n" +
            "timeFormat      general;\n" +
            "timePrecision   6;\n" +
            "runTimeModifiable false;\n\n" +
            "functions\n{\n" +
            "    forceCoeffs\n    {\n" +
            "        type            forceCoeffs;\n" +
            "        libs            (forces);\n" +
            "        writeControl    timeStep;\n" +
            "        writeInterval   1;\n" +
            "        patches         (wing);\n" +
            "        rho             rhoInf;\n" +
            $"        rhoInf          {F(model.Water.Rho)};\n" +
            "        CofR            (0 0 0);\n" +
            $"        liftDir         ({F(liftDir.X)} {F(liftDir.Y)} {F(liftDir.Z)});\n" +
            $"        dragDir         ({F(dragDir.X)} {F(dragDir.Y)} {F(dragDir.Z)});\n" +
            "        pitchAxis       (0 1 0);\n" +
            $"        magUInf         {F(model.Op.Speed)};\n" +
            $"        lRef            {F(model.Reference.CRef)};\n" +
            $"        Aref            {F(model.Reference.SRef * 0.5)};\n" +
            "    }\n\n" +
            "    solverInfo\n    {\n" +
            "        type            solverInfo;\n" +
            "        libs            (utilityFunctionObjects);\n" +
            "        writeControl    timeStep;\n" +
            "        writeInterval   1;\n" +
            $"        fields          {solverFields};\n" +
            "    }\n\n" +
            "    yPlus\n    {\n" +
            "        type            yPlus;\n" +
            "        libs            (fieldFunctionObjects);\n" +
            "        writeControl    writeTime;\n" +
            "        patches         (wing);\n" +
            "    }\n\n" +
            "    wallShearStress\n    {\n" +
            "        type            wallShearStress;\n" +
            "        libs            (fieldFunctionObjects);\n" +
            "        writeControl    writeTime;\n" +
            "        patches         (wing);\n" +
            "    }\n\n" +
            "    wingSurface\n    {\n" +
            "        type            surfaces;\n" +
            "        libs            (sampling);\n" +
            "        writeControl    writeTime;\n" +
            "        surfaceFormat   raw;\n" +
            "        fields          (p wallShearStress);\n" +
            "        interpolationScheme cellPoint;\n" +
            "        surfaces\n        {\n" +
            "            wing\n            {\n" +
            "                type        patch;\n" +
            "                patches     (wing);\n" +
            "                interpolate true;\n" +
            "            }\n" +
            "        }\n" +
            "    }\n" +
            "}\n";
    }

    private static string BuildFvSchemes(TurbulenceModelKind turb)
    {
        string divTurb = turb == TurbulenceModelKind.KOmegaSst
            ? "    div(phi,k)          bounded Gauss upwind;\n    div(phi,omega)      bounded Gauss upwind;\n"
            : "    div(phi,nuTilda)    bounded Gauss upwind;\n";
        return
            Header("dictionary", "fvSchemes") +
            "ddtSchemes\n{\n    default             steadyState;\n}\n\n" +
            "gradSchemes\n{\n    default             Gauss linear;\n    grad(U)             cellLimited Gauss linear 1;\n}\n\n" +
            "divSchemes\n{\n    default             none;\n" +
            "    div(phi,U)          bounded Gauss linearUpwind grad(U);\n" +
            divTurb +
            "    div((nuEff*dev2(T(grad(U))))) Gauss linear;\n}\n\n" +
            "laplacianSchemes\n{\n    default             Gauss linear limited corrected 0.5;\n}\n\n" +
            "interpolationSchemes\n{\n    default             linear;\n}\n\n" +
            "snGradSchemes\n{\n    default             limited corrected 0.5;\n}\n\n" +
            "wallDist\n{\n    method              meshWave;\n}\n";
    }

    private static string BuildFvSolution(TurbulenceModelKind turb)
    {
        string turbSolvers = turb == TurbulenceModelKind.KOmegaSst
            ? "    k\n    {\n        solver          smoothSolver;\n        smoother        GaussSeidel;\n        tolerance       1e-07;\n        relTol          0.1;\n    }\n" +
              "    omega\n    {\n        solver          smoothSolver;\n        smoother        GaussSeidel;\n        tolerance       1e-07;\n        relTol          0.1;\n    }\n"
            : "    nuTilda\n    {\n        solver          smoothSolver;\n        smoother        GaussSeidel;\n        tolerance       1e-07;\n        relTol          0.1;\n    }\n";
        string turbRes = turb == TurbulenceModelKind.KOmegaSst
            ? "        k               1e-05;\n        omega           1e-05;\n"
            : "        nuTilda         1e-05;\n";
        string turbRelax = turb == TurbulenceModelKind.KOmegaSst
            ? "        k               0.5;\n        omega           0.5;\n"
            : "        nuTilda         0.5;\n";
        return
            Header("dictionary", "fvSolution") +
            "solvers\n{\n" +
            "    p\n    {\n        solver          GAMG;\n        smoother        GaussSeidel;\n        tolerance       1e-07;\n        relTol          0.01;\n    }\n" +
            "    U\n    {\n        solver          smoothSolver;\n        smoother        GaussSeidel;\n        tolerance       1e-07;\n        relTol          0.1;\n    }\n" +
            turbSolvers +
            "}\n\n" +
            "SIMPLE\n{\n" +
            "    nNonOrthogonalCorrectors 2;\n" +
            "    consistent      yes;\n" +
            "    residualControl\n    {\n" +
            "        p               1e-05;\n" +
            "        U               1e-05;\n" +
            turbRes +
            "    }\n}\n\n" +
            "relaxationFactors\n{\n" +
            "    fields\n    {\n        p               0.3;\n    }\n" +
            "    equations\n    {\n        U               0.7;\n" +
            turbRelax +
            "    }\n}\n";
    }

    private static string BuildBlockMeshDict(
        double xMin, double xMax, double yMin, double yMax, double zMin, double zMax, int nx, int ny, int nz) =>
        Header("dictionary", "blockMeshDict") +
        "scale   1;\n\n" +
        "vertices\n(\n" +
        $"    ({F(xMin)} {F(yMin)} {F(zMin)})\n" +
        $"    ({F(xMax)} {F(yMin)} {F(zMin)})\n" +
        $"    ({F(xMax)} {F(yMax)} {F(zMin)})\n" +
        $"    ({F(xMin)} {F(yMax)} {F(zMin)})\n" +
        $"    ({F(xMin)} {F(yMin)} {F(zMax)})\n" +
        $"    ({F(xMax)} {F(yMin)} {F(zMax)})\n" +
        $"    ({F(xMax)} {F(yMax)} {F(zMax)})\n" +
        $"    ({F(xMin)} {F(yMax)} {F(zMax)})\n" +
        ");\n\n" +
        "blocks\n(\n" +
        $"    hex (0 1 2 3 4 5 6 7) ({nx.ToString(CultureInfo.InvariantCulture)} {ny.ToString(CultureInfo.InvariantCulture)} {nz.ToString(CultureInfo.InvariantCulture)}) simpleGrading (1 1 1)\n" +
        ");\n\n" +
        "edges\n(\n);\n\n" +
        "boundary\n(\n" +
        "    farfield\n    {\n        type patch;\n        faces\n        (\n" +
        "            (0 4 7 3)\n            (2 6 5 1)\n            (3 7 6 2)\n            (0 3 2 1)\n            (4 5 6 7)\n" +
        "        );\n    }\n" +
        "    symmetry\n    {\n        type symmetryPlane;\n        faces\n        (\n" +
        "            (0 1 5 4)\n" +
        "        );\n    }\n" +
        ");\n";

    private static string BuildSnappyHexMeshDict(
        MeshSettings mesh, double minX, double maxX, double maxY, double minZ, double maxZ)
    {
        int lvl = Math.Max(1, mesh.RefinementLevel);
        int lvlMax = lvl + 1;
        return
            Header("dictionary", "snappyHexMeshDict") +
            "castellatedMesh true;\n" +
            "snap            true;\n" +
            "addLayers       true;\n\n" +
            "geometry\n{\n" +
            "    wing.stl\n    {\n        type triSurfaceMesh;\n        name wing;\n    }\n" +
            "    refinementBox\n    {\n        type box;\n" +
            $"        min ({F(minX)} 0 {F(minZ)});\n" +
            $"        max ({F(maxX)} {F(maxY)} {F(maxZ)});\n" +
            "    }\n}\n\n" +
            "castellatedMeshControls\n{\n" +
            "    maxLocalCells       2000000;\n" +
            "    maxGlobalCells      8000000;\n" +
            "    minRefinementCells  10;\n" +
            "    nCellsBetweenLevels 3;\n" +
            "    features ();\n" +
            "    refinementSurfaces\n    {\n" +
            $"        wing\n        {{\n            level ({lvl.ToString(CultureInfo.InvariantCulture)} {lvlMax.ToString(CultureInfo.InvariantCulture)});\n            patchInfo {{ type wall; }}\n        }}\n" +
            "    }\n" +
            "    resolveFeatureAngle 30;\n" +
            "    refinementRegions\n    {\n" +
            $"        refinementBox\n        {{\n            mode inside;\n            levels ((1e15 {lvl.ToString(CultureInfo.InvariantCulture)}));\n        }}\n" +
            "    }\n" +
            $"    locationInMesh ({F(maxX)} {F(maxY)} {F(maxZ)});\n" +
            "    allowFreeStandingZoneFaces true;\n" +
            "}\n\n" +
            "snapControls\n{\n" +
            "    nSmoothPatch    3;\n" +
            "    tolerance       2.0;\n" +
            "    nSolveIter      30;\n" +
            "    nRelaxIter      5;\n" +
            "}\n\n" +
            "addLayersControls\n{\n" +
            "    relativeSizes   false;\n" +
            "    layers\n    {\n" +
            $"        wing\n        {{\n            nSurfaceLayers {mesh.SurfaceLayers.ToString(CultureInfo.InvariantCulture)};\n        }}\n" +
            "    }\n" +
            $"    expansionRatio  {F(mesh.ExpansionRatio)};\n" +
            $"    firstLayerThickness {F(mesh.FirstLayerHeightMeters * 2.0)};\n" +
            $"    minThickness    {F(mesh.FirstLayerHeightMeters * 0.2)};\n" +
            "    nGrow           0;\n" +
            "    featureAngle    60;\n" +
            "    nRelaxIter      5;\n" +
            "    nSmoothSurfaceNormals 1;\n" +
            "    nSmoothNormals  3;\n" +
            "    nSmoothThickness 10;\n" +
            "    maxFaceThicknessRatio 0.5;\n" +
            "    maxThicknessToMedialRatio 0.3;\n" +
            "    minMedialAxisAngle 90;\n" +
            "    nBufferCellsNoExtrude 0;\n" +
            "    nLayerIter      50;\n" +
            "}\n\n" +
            "meshQualityControls\n{\n" +
            "    maxNonOrtho         65;\n" +
            "    maxBoundarySkewness 15;\n" +
            "    maxInternalSkewness 4;\n" +
            "    maxConcave          80;\n" +
            "    minVol              1e-13;\n" +
            "    minTetQuality       1e-15;\n" +
            "    minArea             -1;\n" +
            "    minTwist            0.02;\n" +
            "    minDeterminant      0.001;\n" +
            "    minFaceWeight       0.05;\n" +
            "    minVolRatio         0.01;\n" +
            "    minTriangleTwist    -1;\n" +
            "    nSmoothScale        4;\n" +
            "    errorReduction      0.75;\n" +
            "}\n\n" +
            "mergeTolerance 1e-06;\n";
    }

    private static string BuildTransportProperties(double nu) =>
        Header("dictionary", "transportProperties") +
        "transportModel  Newtonian;\n" +
        $"nu              [0 2 -1 0 0 0 0] {F(nu)};\n";

    private static string BuildTurbulenceProperties(string rasModel) =>
        Header("dictionary", "turbulenceProperties") +
        "simulationType  RAS;\n\n" +
        "RAS\n{\n" +
        $"    RASModel        {rasModel};\n" +
        "    turbulence      on;\n" +
        "    printCoeffs     on;\n" +
        "}\n";

    private static string BuildVelocityField(Vector3D uInf)
    {
        string vec = $"({F(uInf.X)} {F(uInf.Y)} {F(uInf.Z)})";
        return
            Header("volVectorField", "U") +
            "dimensions      [0 1 -1 0 0 0 0];\n" +
            $"internalField   uniform {vec};\n\n" +
            "boundaryField\n{\n" +
            "    farfield\n    {\n        type            freestreamVelocity;\n" +
            $"        freestreamValue uniform {vec};\n    }}\n" +
            "    wing\n    {\n        type            noSlip;\n    }\n" +
            "    symmetry\n    {\n        type            symmetryPlane;\n    }\n" +
            "}\n";
    }

    private static string BuildPressureField() =>
        Header("volScalarField", "p") +
        "dimensions      [0 2 -2 0 0 0 0];\n" +
        "internalField   uniform 0;\n\n" +
        "boundaryField\n{\n" +
        "    farfield\n    {\n        type            freestreamPressure;\n        freestreamValue uniform 0;\n    }\n" +
        "    wing\n    {\n        type            zeroGradient;\n    }\n" +
        "    symmetry\n    {\n        type            symmetryPlane;\n    }\n" +
        "}\n";

    private static string BuildNutField() =>
        Header("volScalarField", "nut") +
        "dimensions      [0 2 -1 0 0 0 0];\n" +
        "internalField   uniform 0;\n\n" +
        "boundaryField\n{\n" +
        "    farfield\n    {\n        type            calculated;\n        value           uniform 0;\n    }\n" +
        "    wing\n    {\n        type            calculated;\n        value           uniform 0;\n    }\n" +
        "    symmetry\n    {\n        type            symmetryPlane;\n    }\n" +
        "}\n";

    private static string BuildScalarTurbField(string name, string dims, double infVal, double wallValue) =>
        Header("volScalarField", name) +
        $"dimensions      {dims};\n" +
        $"internalField   uniform {F(infVal)};\n\n" +
        "boundaryField\n{\n" +
        "    farfield\n    {\n        type            inletOutlet;\n" +
        $"        inletValue      uniform {F(infVal)};\n" +
        $"        value           uniform {F(infVal)};\n    }}\n" +
        "    wing\n    {\n        type            fixedValue;\n" +
        $"        value           uniform {F(wallValue)};\n    }}\n" +
        "    symmetry\n    {\n        type            symmetryPlane;\n    }\n" +
        "}\n";

    private static string Header(string cls, string obj) =>
        "FoamFile\n{\n    version     2.0;\n    format      ascii;\n" +
        $"    class       {cls};\n    object      {obj};\n}}\n\n";

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
