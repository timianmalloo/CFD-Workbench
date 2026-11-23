using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core;

/// <summary>
/// Pure-C# Option B ISO 10303-21 Part 21 STEP AP203 writer (ADR-0013 §Decision 3, Ruling 270 DR-KRN-3 &amp; DR-KRN-6).
/// Emits millimetre unscaled geometry (<c>SI_UNIT(.MILLI., .METRE.)</c>) with fixed zero-PII headers
/// (<c>1980-01-01T00:00:00</c>, blank author/organization/path, sanitized <see cref="DatImport.Slug"/>) for
/// <c>Whole</c>/<c>Half</c> × <c>ClosedShell</c> (<c>MANIFOLD_SOLID_BREP</c>) / <c>Untrimmed</c> (<c>OPEN_SHELL</c>).
/// </summary>
public sealed class StepAp203Export : ICadStepExporter
{
    public const string FixedZeroPiiTimestamp = "1980-01-01T00:00:00";

    public string EngineId => "option-b-pure-csharp-ap203";

    public static StepExportResult Build(
        byte[] source,
        string foilName,
        int revision,
        StlScope scope,
        StepVariant variant,
        CancellationToken cancellation = default,
        StepHeaderOverrides? headerOverrides = null) =>
        new StepAp203Export().Export(source, foilName, revision, scope, variant, cancellation, headerOverrides);

    /// <summary>
    /// Suggested file name: <c>&lt;slug&gt;-r&lt;n&gt;[-half][-untrimmed].step</c>.
    /// </summary>
    public static string FileName(string foilName, int revision, StlScope scope, StepVariant variant = StepVariant.ClosedShell) =>
        $"{DatImport.Slug(foilName)}-r{revision.ToString(CultureInfo.InvariantCulture)}" +
        $"{(scope == StlScope.Half ? "-half" : "")}" +
        $"{(variant == StepVariant.Untrimmed ? "-untrimmed" : "")}.step";

    public StepExportResult Export(
        byte[] source,
        string foilName,
        int revision,
        StlScope scope,
        StepVariant variant,
        CancellationToken cancellation = default,
        StepHeaderOverrides? headerOverrides = null)
    {
        var skin = CadExportExtractor.BuildSkin(source, cancellation);
        string stepText = WriteStepText(skin, foilName, revision, scope, variant, headerOverrides);
        var verification = StepVerifier.Verify(stepText, variant);
        if (headerOverrides is null && !verification.Passed)
            throw new ContractError("EXPORT-STEP-INVALID", string.Join("; ", verification.Errors));

        byte[] bytes = new UTF8Encoding(false).GetBytes(stepText);
        return new StepExportResult(
            bytes,
            scope,
            variant,
            skin.SectionCount,
            skin.ChordCount,
            skin.UpperStarboard.DegreeU,
            skin.UpperStarboard.DegreeV,
            skin.RefinementRounds,
            skin.Deviation.MaxClosestPointMm,
            skin.Deviation.TipPlanarityErrorMm,
            skin.Deviation.RootPlanarityErrorMm,
            skin.TrailingEdgeMinMm,
            skin.TrailingEdgeMinYMm,
            skin.TrailingEdgeAtTip,
            skin.TrailingEdgeAlongSpan,
            skin.IsOpenTrailingEdge,
            skin.SizeXMm,
            skin.SizeYMm(scope),
            skin.SizeZMm,
            verification);
    }

    /// <summary>Serializes a pre-extracted <see cref="WingCadSkin"/> into ISO 10303-21 Part 21 AP203 text.</summary>
    public static string WriteStepText(
        WingCadSkin skin,
        string foilName,
        int revision,
        StlScope scope,
        StepVariant variant,
        StepHeaderOverrides? overrides = null)
    {
        string slug = DatImport.Slug(foilName);
        string variantTag = variant == StepVariant.ClosedShell ? "closed-shell" : "untrimmed";
        string fileName = FileName(foilName, revision, scope, variant);
        string timestamp = overrides?.Timestamp ?? FixedZeroPiiTimestamp;
        string author = overrides?.Author ?? "";
        string org = overrides?.Organization ?? "";
        var version = typeof(StepAp203Export).Assembly.GetName().Version;
        string appVersion = $"CFD Workbench {version?.Major ?? 0}.{version?.Minor ?? 0}.{Math.Max(0, version?.Build ?? 0)}";

        var sb = new StringBuilder(128 * 1024);
        sb.Append("ISO-10303-21;\n");
        sb.Append("HEADER;\n");
        sb.Append($"FILE_DESCRIPTION(('CFD Workbench STEP AP203 export ({variantTag}, {scope.ToString().ToLowerInvariant()}, mm, unscaled)'), '2;1');\n");
        sb.Append($"FILE_NAME('{Esc(fileName)}', '{Esc(timestamp)}', ('{Esc(author)}'), ('{Esc(org)}'), '{Esc(appVersion)}', '{Esc(appVersion)}', '');\n");
        sb.Append("FILE_SCHEMA(('CONFIG_CONTROL_DESIGN'));\n");
        sb.Append("ENDSEC;\n");
        sb.Append("DATA;\n");

        var b = new EntityBuilder(sb);

        int appCtx = b.Add("APPLICATION_CONTEXT('configuration controlled 3d designs of mechanical parts and assemblies')");
        b.Add($"APPLICATION_PROTOCOL_DEFINITION('international standard', 'config_control_design', 1994, #{appCtx})");
        int prodCtx = b.Add($"PRODUCT_CONTEXT('', #{appCtx}, 'mechanical')");
        int product = b.Add($"PRODUCT('{Esc(slug)}', '{Esc(slug)}', '', (#{prodCtx}))");
        int pdf = b.Add($"PRODUCT_DEFINITION_FORMATION('r{revision.ToString(CultureInfo.InvariantCulture)}', '', #{product})");
        int pdCtx = b.Add($"PRODUCT_DEFINITION_CONTEXT('part definition', #{appCtx}, 'design')");
        int pd = b.Add($"PRODUCT_DEFINITION('design', '', #{pdf}, #{pdCtx})");
        int pds = b.Add($"PRODUCT_DEFINITION_SHAPE('', '', #{pd})");

        int lenUnit = b.Add("(LENGTH_UNIT() NAMED_UNIT(*) SI_UNIT(.MILLI., .METRE.))");
        int angUnit = b.Add("(NAMED_UNIT(*) PLANE_ANGLE_UNIT() SI_UNIT($, .RADIAN.))");
        int solidAngUnit = b.Add("(NAMED_UNIT(*) SI_UNIT($, .STERADIAN.) SOLID_ANGLE_UNIT())");
        int uncertainty = b.Add($"UNCERTAINTY_MEASURE_WITH_UNIT(LENGTH_MEASURE(1.E-04), #{lenUnit}, 'distance_accuracy_value', 'confusion accuracy')");
        int geomCtx = b.Add($"(GEOMETRIC_REPRESENTATION_CONTEXT(3) GLOBAL_UNCERTAINTY_ASSIGNED_CONTEXT((#{uncertainty})) GLOBAL_UNIT_ASSIGNED_CONTEXT((#{lenUnit}, #{angUnit}, #{solidAngUnit})) REPRESENTATION_CONTEXT('{Esc(slug)}', '3D'))");

        int origin = b.AddPoint(new CadVec3(0, 0, 0));
        int axisZ = b.AddDir(new CadVec3(0, 0, 1));
        int axisX = b.AddDir(new CadVec3(1, 0, 0));
        int placement = b.Add($"AXIS2_PLACEMENT_3D('world', #{origin}, #{axisZ}, #{axisX})");

        var upperS = skin.UpperStarboard;
        var lowerS = skin.LowerStarboard;
        int nu = upperS.CountU;
        int nv = upperS.CountV;

        var rootCurveU = upperS.IsoV(0);
        var rootCurveL = lowerS.IsoV(0);
        var stbdTipCurveU = upperS.IsoV(nv - 1);
        var stbdTipCurveL = lowerS.IsoV(nv - 1);
        var stbdLeCurve = upperS.IsoU(0);
        var stbdTeCurveU = upperS.IsoU(nu - 1);
        var stbdTeCurveL = lowerS.IsoU(nu - 1);

        int vRootLe = b.AddVertex("v_root_le", rootCurveU.Poles[0]);
        int vRootTeU = b.AddVertex("v_root_te_u", rootCurveU.Poles[^1]);
        int vRootTeL = skin.IsOpenTrailingEdge
            ? b.AddVertex("v_root_te_l", rootCurveL.Poles[^1])
            : vRootTeU;

        int eRootU = b.AddBSplineEdge("e_root_u", rootCurveU, vRootLe, vRootTeU);
        int eRootL = b.AddBSplineEdge("e_root_l", rootCurveL, vRootLe, vRootTeL);
        int eRootTe = skin.IsOpenTrailingEdge
            ? b.AddLineEdge("e_root_te", rootCurveU.Poles[^1], rootCurveL.Poles[^1], vRootTeU, vRootTeL)
            : -1;

        int vStbdTipLe = b.AddVertex("v_stbd_tip_le", stbdTipCurveU.Poles[0]);
        int vStbdTipTeU = b.AddVertex("v_stbd_tip_te_u", stbdTipCurveU.Poles[^1]);
        int vStbdTipTeL = skin.IsOpenTrailingEdge
            ? b.AddVertex("v_stbd_tip_te_l", stbdTipCurveL.Poles[^1])
            : vStbdTipTeU;

        int eStbdLe = b.AddBSplineEdge("e_stbd_le", stbdLeCurve, vRootLe, vStbdTipLe);
        int eStbdTeU = b.AddBSplineEdge("e_stbd_te_u", stbdTeCurveU, vRootTeU, vStbdTipTeU);
        int eStbdTeL = skin.IsOpenTrailingEdge
            ? b.AddBSplineEdge("e_stbd_te_l", stbdTeCurveL, vRootTeL, vStbdTipTeL)
            : eStbdTeU;
        int eStbdTipU = b.AddBSplineEdge("e_stbd_tip_u", stbdTipCurveU, vStbdTipLe, vStbdTipTeU);
        int eStbdTipL = b.AddBSplineEdge("e_stbd_tip_l", stbdTipCurveL, vStbdTipLe, vStbdTipTeL);
        int eStbdTipTe = skin.IsOpenTrailingEdge
            ? b.AddLineEdge("e_stbd_tip_te", stbdTipCurveU.Poles[^1], stbdTipCurveL.Poles[^1], vStbdTipTeU, vStbdTipTeL)
            : -1;

        int srfStbdU = b.AddBSplineSurface("srf_stbd_upper", upperS);
        int srfStbdL = b.AddBSplineSurface("srf_stbd_lower", lowerS);

        int fStbdU = b.AddFace("f_stbd_upper", srfStbdU, true,
            [(eRootU, true), (eStbdTeU, true), (eStbdTipU, false), (eStbdLe, false)]);
        int fStbdL = b.AddFace("f_stbd_lower", srfStbdL, false,
            [(eStbdLe, true), (eStbdTipL, true), (eStbdTeL, false), (eRootL, false)]);

        var faces = new List<int> { fStbdU, fStbdL };

        if (skin.IsOpenTrailingEdge)
        {
            var teStripS = CadNurbsMath.RuledStrip(stbdTeCurveU, stbdTeCurveL);
            int srfStbdTe = b.AddBSplineSurface("srf_stbd_te", teStripS);
            int fStbdTe = b.AddFace("f_stbd_te", srfStbdTe, true,
                [(eRootTe, true), (eStbdTeL, true), (eStbdTipTe, false), (eStbdTeU, false)]);
            faces.Add(fStbdTe);
        }

        if (variant == StepVariant.ClosedShell)
        {
            int plnStbdTip = b.AddPlane("pln_stbd_tip", new CadVec3(0, skin.HalfSpanMm, 0), new CadVec3(0, 1, 0), new CadVec3(0, 0, 1));
            var stbdTipLoop = skin.IsOpenTrailingEdge
                ? new[] { (eStbdTipU, true), (eStbdTipTe, true), (eStbdTipL, false) }
                : new[] { (eStbdTipU, true), (eStbdTipL, false) };
            faces.Add(b.AddFace("f_stbd_tip", plnStbdTip, true, stbdTipLoop));
        }

        if (scope == StlScope.Half)
        {
            if (variant == StepVariant.ClosedShell)
            {
                int plnRoot = b.AddPlane("pln_root", new CadVec3(0, 0, 0), new CadVec3(0, -1, 0), new CadVec3(0, 0, 1));
                var rootLoop = skin.IsOpenTrailingEdge
                    ? new[] { (eRootL, true), (eRootTe, false), (eRootU, false) }
                    : new[] { (eRootL, true), (eRootU, false) };
                faces.Add(b.AddFace("f_root_cap", plnRoot, true, rootLoop));
            }
        }
        else
        {
            var upperP = upperS.MirrorY();
            var lowerP = lowerS.MirrorY();
            var portTipCurveU = upperP.IsoV(nv - 1);
            var portTipCurveL = lowerP.IsoV(nv - 1);
            var portLeCurve = upperP.IsoU(0);
            var portTeCurveU = upperP.IsoU(nu - 1);
            var portTeCurveL = lowerP.IsoU(nu - 1);

            int vPortTipLe = b.AddVertex("v_port_tip_le", portTipCurveU.Poles[0]);
            int vPortTipTeU = b.AddVertex("v_port_tip_te_u", portTipCurveU.Poles[^1]);
            int vPortTipTeL = skin.IsOpenTrailingEdge
                ? b.AddVertex("v_port_tip_te_l", portTipCurveL.Poles[^1])
                : vPortTipTeU;

            int ePortLe = b.AddBSplineEdge("e_port_le", portLeCurve, vRootLe, vPortTipLe);
            int ePortTeU = b.AddBSplineEdge("e_port_te_u", portTeCurveU, vRootTeU, vPortTipTeU);
            int ePortTeL = skin.IsOpenTrailingEdge
                ? b.AddBSplineEdge("e_port_te_l", portTeCurveL, vRootTeL, vPortTipTeL)
                : ePortTeU;
            int ePortTipU = b.AddBSplineEdge("e_port_tip_u", portTipCurveU, vPortTipLe, vPortTipTeU);
            int ePortTipL = b.AddBSplineEdge("e_port_tip_l", portTipCurveL, vPortTipLe, vPortTipTeL);
            int ePortTipTe = skin.IsOpenTrailingEdge
                ? b.AddLineEdge("e_port_tip_te", portTipCurveU.Poles[^1], portTipCurveL.Poles[^1], vPortTipTeU, vPortTipTeL)
                : -1;

            int srfPortU = b.AddBSplineSurface("srf_port_upper", upperP);
            int srfPortL = b.AddBSplineSurface("srf_port_lower", lowerP);

            int fPortU = b.AddFace("f_port_upper", srfPortU, false,
                [(ePortLe, true), (ePortTipU, true), (ePortTeU, false), (eRootU, false)]);
            int fPortL = b.AddFace("f_port_lower", srfPortL, true,
                [(eRootL, true), (ePortTeL, true), (ePortTipL, false), (ePortLe, false)]);

            faces.Add(fPortU);
            faces.Add(fPortL);

            if (skin.IsOpenTrailingEdge)
            {
                var teStripP = CadNurbsMath.RuledStrip(portTeCurveU, portTeCurveL);
                int srfPortTe = b.AddBSplineSurface("srf_port_te", teStripP);
                int fPortTe = b.AddFace("f_port_te", srfPortTe, false,
                    [(ePortTeU, true), (ePortTipTe, true), (ePortTeL, false), (eRootTe, false)]);
                faces.Add(fPortTe);
            }

            if (variant == StepVariant.ClosedShell)
            {
                int plnPortTip = b.AddPlane("pln_port_tip", new CadVec3(0, -skin.HalfSpanMm, 0), new CadVec3(0, -1, 0), new CadVec3(0, 0, 1));
                var portTipLoop = skin.IsOpenTrailingEdge
                    ? new[] { (ePortTipL, true), (ePortTipTe, false), (ePortTipU, false) }
                    : new[] { (ePortTipL, true), (ePortTipU, false) };
                faces.Add(b.AddFace("f_port_tip", plnPortTip, true, portTipLoop));
            }
        }

        string faceList = string.Join(", ", faces.Select(id => $"#{id}"));
        if (variant == StepVariant.ClosedShell)
        {
            int shell = b.Add($"CLOSED_SHELL('wing_shell', ({faceList}))");
            int solid = b.Add($"MANIFOLD_SOLID_BREP('{Esc(slug)}', #{shell})");
            int rep = b.Add($"ADVANCED_BREP_SHAPE_REPRESENTATION('{Esc(slug)}', (#{placement}, #{solid}), #{geomCtx})");
            b.Add($"SHAPE_DEFINITION_REPRESENTATION(#{pds}, #{rep})");
        }
        else
        {
            int shell = b.Add($"OPEN_SHELL('wing_surfaces', ({faceList}))");
            int model = b.Add($"SHELL_BASED_SURFACE_MODEL('{Esc(slug)}', (#{shell}))");
            int rep = b.Add($"MANIFOLD_SURFACE_SHAPE_REPRESENTATION('{Esc(slug)}', (#{placement}, #{model}), #{geomCtx})");
            b.Add($"SHAPE_DEFINITION_REPRESENTATION(#{pds}, #{rep})");
        }

        sb.Append("ENDSEC;\n");
        sb.Append("END-ISO-10303-21;\n");
        return sb.ToString();
    }

    private static string Esc(string s) => s.Replace("'", "''", StringComparison.Ordinal);

    private static string Fmt(double v)
    {
        if (Math.Abs(v) < 1e-15) v = 0.0;
        string s = v.ToString("G17", CultureInfo.InvariantCulture);
        if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
            s += ".";
        return s;
    }

    private sealed class EntityBuilder(StringBuilder sb)
    {
        private int _nextId = 1;

        public int Add(string body)
        {
            int id = _nextId++;
            sb.Append('#').Append(id).Append('=').Append(body).Append(";\n");
            return id;
        }

        public int AddPoint(CadVec3 p, string name = "") =>
            Add($"CARTESIAN_POINT('{name}', ({Fmt(p.X)}, {Fmt(p.Y)}, {Fmt(p.Z)}))");

        public int AddDir(CadVec3 d, string name = "")
        {
            double len = d.Length();
            CadVec3 n = len > 0 ? (1.0 / len) * d : new(0, 0, 1);
            return Add($"DIRECTION('{name}', ({Fmt(n.X)}, {Fmt(n.Y)}, {Fmt(n.Z)}))");
        }

        public int AddVertex(string name, CadVec3 p)
        {
            int pt = AddPoint(p, name);
            return Add($"VERTEX_POINT('{name}', #{pt})");
        }

        public int AddPlane(string name, CadVec3 origin, CadVec3 normal, CadVec3 refDir)
        {
            int o = AddPoint(origin);
            int n = AddDir(normal);
            int r = AddDir(refDir);
            int ax = Add($"AXIS2_PLACEMENT_3D('{name}_ax', #{o}, #{n}, #{r})");
            return Add($"PLANE('{name}', #{ax})");
        }

        public int AddLineEdge(string name, CadVec3 start, CadVec3 end, int vStart, int vEnd)
        {
            CadVec3 diff = end - start;
            double len = diff.Length();
            int pt = AddPoint(start);
            int dir = AddDir(diff);
            int vec = Add($"VECTOR('{name}_v', #{dir}, {Fmt(len)})");
            int line = Add($"LINE('{name}_ln', #{pt}, #{vec})");
            return Add($"EDGE_CURVE('{name}', #{vStart}, #{vEnd}, #{line}, .T.)");
        }

        public int AddBSplineEdge(string name, CadNurbsCurve curve, int vStart, int vEnd)
        {
            var ptIds = new int[curve.Poles.Length];
            for (int i = 0; i < curve.Poles.Length; i++)
                ptIds[i] = AddPoint(curve.Poles[i]);
            var (uk, mult) = CadNurbsMath.CompressKnots(curve.Knots);
            string polesStr = string.Join(", ", ptIds.Select(id => $"#{id}"));
            string multStr = string.Join(", ", mult.Select(m => m.ToString(CultureInfo.InvariantCulture)));
            string knotStr = string.Join(", ", uk.Select(Fmt));
            int crv = Add($"B_SPLINE_CURVE_WITH_KNOTS('{name}_c', {curve.Degree}, ({polesStr}), .UNSPECIFIED., .F., .F., ({multStr}), ({knotStr}), .UNSPECIFIED.)");
            return Add($"EDGE_CURVE('{name}', #{vStart}, #{vEnd}, #{crv}, .T.)");
        }

        public int AddBSplineSurface(string name, CadNurbsSurface surface)
        {
            int nu = surface.CountU;
            int nv = surface.CountV;
            var rows = new string[nu];
            for (int i = 0; i < nu; i++)
            {
                var rowIds = new string[nv];
                for (int j = 0; j < nv; j++)
                    rowIds[j] = $"#{AddPoint(surface.Poles[i, j])}";
                rows[i] = $"({string.Join(", ", rowIds)})";
            }
            var (uk, um) = CadNurbsMath.CompressKnots(surface.KnotsU);
            var (vk, vm) = CadNurbsMath.CompressKnots(surface.KnotsV);
            string gridStr = string.Join(", ", rows);
            string umStr = string.Join(", ", um.Select(m => m.ToString(CultureInfo.InvariantCulture)));
            string vmStr = string.Join(", ", vm.Select(m => m.ToString(CultureInfo.InvariantCulture)));
            string ukStr = string.Join(", ", uk.Select(Fmt));
            string vkStr = string.Join(", ", vk.Select(Fmt));
            return Add($"B_SPLINE_SURFACE_WITH_KNOTS('{name}', {surface.DegreeU}, {surface.DegreeV}, ({gridStr}), .UNSPECIFIED., .F., .F., .F., ({umStr}), ({vmStr}), ({ukStr}), ({vkStr}), .UNSPECIFIED.)");
        }

        public int AddFace(string name, int surfaceId, bool sameSense, (int EdgeId, bool Forward)[] loopEdges)
        {
            var oeIds = new int[loopEdges.Length];
            for (int i = 0; i < loopEdges.Length; i++)
            {
                string sense = loopEdges[i].Forward ? ".T." : ".F.";
                oeIds[i] = Add($"ORIENTED_EDGE('', *, *, #{loopEdges[i].EdgeId}, {sense})");
            }
            string oeList = string.Join(", ", oeIds.Select(id => $"#{id}"));
            int loop = Add($"EDGE_LOOP('{name}_loop', ({oeList}))");
            int bound = Add($"FACE_OUTER_BOUND('{name}_bound', #{loop}, .T.)");
            string faceSense = sameSense ? ".T." : ".F.";
            return Add($"ADVANCED_FACE('{name}', (#{bound}), #{surfaceId}, {faceSense})");
        }
    }
}
