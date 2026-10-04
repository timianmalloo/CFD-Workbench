using System.Globalization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

public sealed record DatProfile(string Name, string Format, IReadOnlyList<ProfilePoint> Upper, IReadOnlyList<ProfilePoint> Lower, byte[] OriginalBytes, int OriginalPointCount);

public sealed record ImportedProfile(string ProfileBlock, double MaxResidual, int VertexCount, string Provenance, bool Accepted);

/// <summary>Ordinates fitted on a fixed basis; VerticalResidual is the gap at the source points.</summary>
internal sealed record BasisFit(double[] Upper, double[] Lower, double VerticalResidual);

public static class DatImport
{
    public static string Slug(string name)
    {
        string slug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "profile" : slug;
    }

    public static DatProfile Parse(byte[] dat)
    {
        var raw = ParseRaw(dat);
        var upperRaw = raw.Upper;
        var lowerRaw = raw.Lower;
        double xMin = double.PositiveInfinity;
        double xMax = double.NegativeInfinity;
        foreach (var pt in upperRaw)
        {
            if (pt.X < xMin) xMin = pt.X;
            if (pt.X > xMax) xMax = pt.X;
        }
        foreach (var pt in lowerRaw)
        {
            if (pt.X < xMin) xMin = pt.X;
            if (pt.X > xMax) xMax = pt.X;
        }

        double chord = xMax - xMin;
        if (chord <= 1e-12)
            throw new ContractError("DSL-IMPORT", raw.FirstLine);

        double yLe = upperRaw[0].Y;

        var upperNorm = new List<ProfilePoint>(upperRaw.Count);
        for (int i = 0; i < upperRaw.Count; i++)
        {
            double nx = (upperRaw[i].X - xMin) / chord;
            double ny = (upperRaw[i].Y - yLe) / chord;
            if (i == 0) { nx = 0; ny = 0; }
            upperNorm.Add(new(nx, ny));
        }

        var lowerNorm = new List<ProfilePoint>(lowerRaw.Count);
        for (int i = 0; i < lowerRaw.Count; i++)
        {
            double nx = (lowerRaw[i].X - xMin) / chord;
            double ny = (lowerRaw[i].Y - yLe) / chord;
            if (i == 0) { nx = 0; ny = 0; }
            lowerNorm.Add(new(nx, ny));
        }

        return new DatProfile(raw.Name, raw.Format, upperNorm.AsReadOnly(), lowerNorm.AsReadOnly(), dat, raw.Count);
    }

    // The file's rows split at the leading edge (minimum-x sample), each surface from the nose, in the file's own frame.
    private static (string Name, string Format, List<ProfilePoint> Upper, List<ProfilePoint> Lower, int Count, int FirstLine) ParseRaw(byte[] dat)
    {
        ArgumentNullException.ThrowIfNull(dat);
        if (dat.Length == 0) throw new ContractError("DSL-IMPORT", 1);

        string text;
        try { text = FoilSource.Utf8.GetString(dat); }
        catch (Exception) { throw new ContractError("DSL-IMPORT", 1); }

        if (text.StartsWith('\uFEFF')) text = text[1..];

        var rawLines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var validLines = new List<(int LineNumber, string Text)>();
        for (int i = 0; i < rawLines.Length; i++)
        {
            string trimmed = rawLines[i].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            validLines.Add((i + 1, trimmed));
        }

        if (validLines.Count < 2)
            throw new ContractError("DSL-IMPORT", validLines.Count > 0 ? validLines[0].LineNumber : 1);

        string rawName = validLines[0].Text;
        var firstRow = ParseRowNumbers(validLines[1].Text, validLines[1].LineNumber);
        if (firstRow.Length != 2)
            throw new ContractError("DSL-IMPORT", validLines[1].LineNumber);

        bool isLednicer = false;
        if (firstRow[0] >= 2 && firstRow[1] >= 2 &&
            Math.Abs(firstRow[0] - Math.Round(firstRow[0])) < 1e-9 &&
            Math.Abs(firstRow[1] - Math.Round(firstRow[1])) < 1e-9 &&
            validLines.Count > 2)
        {
            var nextRow = ParseRowNumbers(validLines[2].Text, validLines[2].LineNumber);
            if (nextRow.Length == 2 && nextRow[0] < 0.5)
            {
                isLednicer = true;
            }
        }

        List<ProfilePoint> upperRaw, lowerRaw;
        int originalPointCount;
        string format;

        if (isLednicer)
        {
            format = "lednicer";
            int nu = (int)Math.Round(firstRow[0]);
            int nl = (int)Math.Round(firstRow[1]);
            originalPointCount = nu + nl;
            if (nu < 2 || nl < 2 || originalPointCount < 10)
                throw new ContractError("DSL-IMPORT", validLines[1].LineNumber);

            if (validLines.Count - 2 < originalPointCount)
                throw new ContractError("DSL-IMPORT", validLines[^1].LineNumber);

            upperRaw = new List<ProfilePoint>(nu);
            for (int i = 0; i < nu; i++)
            {
                var row = ParseRowNumbers(validLines[2 + i].Text, validLines[2 + i].LineNumber);
                if (row.Length != 2) throw new ContractError("DSL-IMPORT", validLines[2 + i].LineNumber);
                upperRaw.Add(new(row[0], row[1]));
            }

            lowerRaw = new List<ProfilePoint>(nl);
            for (int i = 0; i < nl; i++)
            {
                var row = ParseRowNumbers(validLines[2 + nu + i].Text, validLines[2 + nu + i].LineNumber);
                if (row.Length != 2) throw new ContractError("DSL-IMPORT", validLines[2 + nu + i].LineNumber);
                lowerRaw.Add(new(row[0], row[1]));
            }
        }
        else
        {
            format = "selig";
            int count = validLines.Count - 1;
            var points = new List<ProfilePoint>(count);
            for (int i = 1; i < validLines.Count; i++)
            {
                var row = ParseRowNumbers(validLines[i].Text, validLines[i].LineNumber);
                if (row.Length != 2) throw new ContractError("DSL-IMPORT", validLines[i].LineNumber);
                points.Add(new(row[0], row[1]));
            }

            if (points.Count < 10)
                throw new ContractError("DSL-IMPORT", validLines[^1].LineNumber);

            originalPointCount = points.Count;

            int minIdx = 0;
            double minX = points[0].X;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].X < minX)
                {
                    minX = points[i].X;
                    minIdx = i;
                }
            }

            if (minIdx == 0 || minIdx == points.Count - 1)
                throw new ContractError("DSL-IMPORT", validLines[1].LineNumber);

            upperRaw = points.Take(minIdx + 1).Reverse().ToList();
            if (minIdx + 1 < points.Count && Math.Abs(points[minIdx + 1].X - minX) < 1e-9)
                lowerRaw = points.Skip(minIdx + 1).ToList();
            else
                lowerRaw = points.Skip(minIdx).ToList();
        }

        return (rawName, format, upperRaw, lowerRaw, originalPointCount, validLines[1].LineNumber);
    }

    public static ImportedProfile Fit(DatProfile p, string name)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        bool closed = IsClosed(p);
        string closure = closed ? "closed" : "open";

        int bestN = 16;
        double[] bestKnots = [];
        double[] bestCvX = [];
        double[] bestPyUpper = [];
        double[] bestPyLower = [];
        double bestMaxRes = double.PositiveInfinity;
        bool accepted = false;

        for (int n = 8; n <= 16; n++)
        {
            var (knots, cv_x) = OwnSqrtBasis(n);
            var (a, b) = EndpointPins(n, closed);

            int mUpper = p.Upper.Count;
            double[,] nUpper = new double[mUpper, n];
            double[] qUpper = new double[mUpper];
            double[] wUpper = new double[mUpper];
            for (int k = 0; k < mUpper; k++)
            {
                double uk = Math.Sqrt(Math.Max(0, p.Upper[k].X));
                qUpper[k] = p.Upper[k].Y;
                wUpper[k] = 1.0;
                double[] basis = SplineBasis.Values(knots, 5, uk);
                for (int j = 0; j < n; j++) nUpper[k, j] = basis[j];
            }

            var f = new double[n, n];
            double[] pyUpper = ConstrainedFit.Solve(nUpper, qUpper, wUpper, f, 0, a, b);
            pyUpper[0] = 0;
            if (closed) pyUpper[n - 1] = 0;

            double maxResUpper = 0;
            for (int k = 0; k < mUpper; k++)
            {
                double uk = Math.Sqrt(Math.Max(0, p.Upper[k].X));
                double[] basis = SplineBasis.Values(knots, 5, uk);
                double yFit = 0;
                for (int j = 0; j < n; j++) yFit += pyUpper[j] * basis[j];
                double res = Math.Abs(yFit - p.Upper[k].Y);
                if (res > maxResUpper) maxResUpper = res;
            }

            int mLower = p.Lower.Count;
            double[,] nLower = new double[mLower, n];
            double[] qLower = new double[mLower];
            double[] wLower = new double[mLower];
            for (int k = 0; k < mLower; k++)
            {
                double uk = Math.Sqrt(Math.Max(0, p.Lower[k].X));
                qLower[k] = p.Lower[k].Y;
                wLower[k] = 1.0;
                double[] basis = SplineBasis.Values(knots, 5, uk);
                for (int j = 0; j < n; j++) nLower[k, j] = basis[j];
            }

            double[] pyLower = ConstrainedFit.Solve(nLower, qLower, wLower, f, 0, a, b);
            pyLower[0] = 0;
            if (closed) pyLower[n - 1] = 0;

            double maxResLower = 0;
            for (int k = 0; k < mLower; k++)
            {
                double uk = Math.Sqrt(Math.Max(0, p.Lower[k].X));
                double[] basis = SplineBasis.Values(knots, 5, uk);
                double yFit = 0;
                for (int j = 0; j < n; j++) yFit += pyLower[j] * basis[j];
                double res = Math.Abs(yFit - p.Lower[k].Y);
                if (res > maxResLower) maxResLower = res;
            }

            double maxRes = Math.Max(maxResUpper, maxResLower);
            bestN = n;
            bestKnots = knots;
            bestCvX = cv_x;
            bestPyUpper = pyUpper;
            bestPyLower = pyLower;
            bestMaxRes = maxRes;

            if (maxRes <= 1e-5)
            {
                accepted = true;
                break;
            }
        }

        string provenance = ProvenanceOf(p);
        string blockText = ProfileBlock(name, 5, closure, provenance, bestKnots, bestCvX, bestPyUpper, bestPyLower);
        return new ImportedProfile(blockText, bestMaxRes, bestN, provenance, accepted);
    }

    /// <summary>
    /// Fits ordinates only: knots, control x and ids stay on the supplied basis (m12d §3.6 rule 6b). Every interior tangent
    /// row except vertical is linear in y once x is fixed, so it joins the endpoint pins as a KKT equality row; a vertical row's
    /// sign condition is checked after the solve. A row that cannot hold refuses with DSL-LOCK naming it. Null when the system
    /// is singular. The residual here is the vertical gap at the source points; Replace reports the Euclidean one
    /// (<see cref="EuclideanResidual"/>).
    /// </summary>
    internal static BasisFit? FitToBasis(DatProfile profile, double[] knots, double[] controlX, int degree, bool closed,
        IReadOnlyList<TangentRow>? upperRows = null, string[]? upperIds = null, IReadOnlyList<TangentRow>? lowerRows = null, string[]? lowerIds = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(knots);
        ArgumentNullException.ThrowIfNull(controlX);
        int count = controlX.Length;
        if (degree < 1 || count < 2 || knots.Length != count + degree + 1) return null;
        if (profile.Upper.Count == 0 || profile.Lower.Count == 0) return null;
        bool sqrtParameter = degree == 5 && MatchesOwnSqrtBasis(knots, controlX);
        var smoothing = new double[count, count];
        var upperConstraints = Constraints(count, closed, controlX, upperRows ?? [], upperIds);
        var lowerConstraints = Constraints(count, closed, controlX, lowerRows ?? [], lowerIds);
        if (!TryOrdinates(profile.Upper, knots, controlX, degree, smoothing, upperConstraints.A, upperConstraints.B, closed, sqrtParameter, out double[] upper, out double upperResidual))
            return null;
        if (!TryOrdinates(profile.Lower, knots, controlX, degree, smoothing, lowerConstraints.A, lowerConstraints.B, closed, sqrtParameter, out double[] lower, out double lowerResidual))
            return null;
        Settle(upper, upperRows ?? [], upperIds);
        Settle(lower, lowerRows ?? [], lowerIds);
        double maxResidual = Math.Max(upperResidual, lowerResidual);
        return double.IsFinite(maxResidual) ? new BasisFit(upper, lower, maxResidual) : null;
    }

    // Endpoint pins, then one equality row per linear tangent condition (the Geometry.CheckProfileRow conditions with x fixed).
    private static (double[,] A, double[] B) Constraints(int count, bool closed, double[] x, IReadOnlyList<TangentRow> rows, string[]? ids)
    {
        var (pins, pinned) = EndpointPins(count, closed);
        var matrix = new List<double[]>();
        var bound = new List<double>();
        for (int row = 0; row < pins.GetLength(0); row++)
        {
            matrix.Add(Enumerable.Range(0, count).Select(column => pins[row, column]).ToArray());
            bound.Add(pinned[row]);
        }
        foreach (var tangent in rows)
        {
            int i = ids is null ? -1 : Array.IndexOf(ids, tangent.Id);
            if (i <= 0 || i >= count - 1) throw new ContractError("DSL-LOCK", $"Point {tangent.Id}'s type has no anchor on this spacing.");
            switch (tangent.Kind)
            {
                case "horizontal":
                    matrix.Add(Row(count, (i - 1, 1), (i, -1))); bound.Add(0);
                    matrix.Add(Row(count, (i + 1, 1), (i, -1))); bound.Add(0);
                    break;
                case "angle":
                    double slope = Math.Tan((tangent.Angle ?? double.NaN) * PlacementRule.RadiansPerDegree);
                    if (!double.IsFinite(slope)) throw new ContractError("DSL-LOCK", $"Point {tangent.Id}'s angle cannot hold on this spacing.");
                    matrix.Add(Row(count, (i + 1, 1), (i, -1))); bound.Add(slope * (x[i + 1] - x[i]));
                    matrix.Add(Row(count, (i - 1, 1), (i, -1))); bound.Add(slope * (x[i - 1] - x[i]));
                    break;
                case "smooth":
                    // (x_i - x_l)(y_r - y_l) = (x_r - x_l)(y_i - y_l)
                    matrix.Add(Row(count, (i + 1, x[i] - x[i - 1]), (i - 1, -(x[i] - x[i - 1]) + (x[i + 1] - x[i - 1])), (i, -(x[i + 1] - x[i - 1]))));
                    bound.Add(0);
                    break;
                case "symmetric":
                    matrix.Add(Row(count, (i, 1), (i - 1, -0.5), (i + 1, -0.5))); bound.Add(0);
                    break;
                case "vertical":
                    break;
                default:
                    throw new ContractError("DSL-LOCK", $"Point {tangent.Id}'s type cannot hold on this spacing.");
            }
        }
        var a = new double[matrix.Count, count];
        for (int row = 0; row < matrix.Count; row++)
            for (int column = 0; column < count; column++) a[row, column] = matrix[row][column];
        return (a, bound.ToArray());
    }

    private static double[] Row(int count, params (int Index, double Value)[] entries)
    {
        var row = new double[count];
        foreach (var (index, value) in entries) row[index] += value;
        return row;
    }

    // Exact equalities the certificate compares bit for bit (a horizontal row), and the vertical rows' sign condition.
    private static void Settle(double[] y, IReadOnlyList<TangentRow> rows, string[]? ids)
    {
        foreach (var tangent in rows)
        {
            int i = Array.IndexOf(ids!, tangent.Id);
            if (tangent.Kind == "horizontal") y[i - 1] = y[i + 1] = y[i];
            if (tangent.Kind == "vertical" && (y[i - 1] - y[i]) * (y[i + 1] - y[i]) >= 0)
                throw new ContractError("DSL-LOCK", $"Point {tangent.Id} is vertical, and this shape cannot keep it vertical on this spacing.");
        }
    }

    /// <summary>
    /// m12d §3.6 rule 5: the largest Euclidean distance, both ways, between the source and a fitted surface — every source
    /// sample to the fitted curve, then 201 cosine samples of the fitted curve plus its knots to the source curve.
    /// </summary>
    internal static double EuclideanResidual(IReadOnlyList<ProfilePoint> sourceSamples, (double X, double Y)[] sourceCurve,
        double[] knots, int degree, double[] controlX, double[] controlY)
    {
        throw new ContractError("RPL-NOT-BUILT", "The Euclidean residual is not built yet.");
#pragma warning disable CS0162
        var fitted = Dense(knots, degree, controlX, controlY, 4000);
        double worst = 0;
        foreach (var point in sourceSamples) worst = Math.Max(worst, Distance(fitted, point.X, point.Y));
        var parameters = Enumerable.Range(0, 201).Select(i => 0.5 * (1 - Math.Cos(Math.PI * i / 200)))
            .Concat(knots.Where(knot => knot > 0 && knot < 1).Distinct());
        foreach (double u in parameters)
        {
            var (x, y) = Evaluate(knots, degree, controlX, controlY, u);
            worst = Math.Max(worst, Distance(sourceCurve, x, y));
        }
        return worst;
    }

    internal static (double X, double Y)[] Dense(double[] knots, int degree, double[] controlX, double[] controlY, int segments) =>
        Enumerable.Range(0, segments + 1).Select(i => Evaluate(knots, degree, controlX, controlY, (double)i / segments)).ToArray();

    private static (double X, double Y) Evaluate(double[] knots, int degree, double[] controlX, double[] controlY, double u)
    {
        double[] values = SplineBasis.Values(knots, degree, u);
        double x = 0, y = 0;
        for (int index = 0; index < controlX.Length; index++) { x += values[index] * controlX[index]; y += values[index] * controlY[index]; }
        return (x, y);
    }

    // The nearest vertex, then the two segments beside it: exact for a polyline dense against its curvature.
    private static double Distance((double X, double Y)[] polyline, double x, double y)
    {
        int nearest = 0;
        double best = double.PositiveInfinity;
        for (int index = 0; index < polyline.Length; index++)
        {
            double dx = polyline[index].X - x, dy = polyline[index].Y - y, squared = dx * dx + dy * dy;
            if (squared < best) { best = squared; nearest = index; }
        }
        for (int index = Math.Max(0, nearest - 1); index < Math.Min(polyline.Length - 1, nearest + 1); index++)
        {
            var (ax, ay) = polyline[index];
            var (bx, by) = polyline[index + 1];
            double vx = bx - ax, vy = by - ay, length = vx * vx + vy * vy;
            double t = length == 0 ? 0 : Math.Clamp(((x - ax) * vx + (y - ay) * vy) / length, 0, 1);
            double ex = ax + t * vx - x, ey = ay + t * vy - y;
            best = Math.Min(best, ex * ex + ey * ey);
        }
        return Math.Sqrt(best);
    }

    /// <summary>
    /// The source curve through a coordinate set: one natural cubic spline (chord-length parameter) through the whole
    /// trailing edge → nose → trailing edge loop, so the nose is interior, densified and split back into surfaces from the nose.
    /// </summary>
    internal static ((double X, double Y)[] Upper, (double X, double Y)[] Lower) SourceCurve(DatProfile profile, int perInterval = 16)
    {
        var loop = profile.Upper.Reverse().Concat(profile.Lower.Skip(1)).ToList();
        var points = new List<ProfilePoint> { loop[0] };
        foreach (var point in loop.Skip(1))
            if (point.X != points[^1].X || point.Y != points[^1].Y) points.Add(point);
        int nose = points.FindIndex(point => point.X == profile.Upper[0].X && point.Y == profile.Upper[0].Y);
        var t = new double[points.Count];
        for (int i = 1; i < points.Count; i++) t[i] = t[i - 1] + Math.Sqrt(Math.Pow(points[i].X - points[i - 1].X, 2) + Math.Pow(points[i].Y - points[i - 1].Y, 2));
        double[] mx = NaturalSecondDerivatives(t, points.Select(point => point.X).ToArray());
        double[] my = NaturalSecondDerivatives(t, points.Select(point => point.Y).ToArray());
        var dense = new List<(double X, double Y)>();
        int noseDense = 0;
        for (int i = 0; i + 1 < points.Count; i++)
        {
            if (i == nose) noseDense = dense.Count;
            for (int k = 0; k < perInterval; k++)
            {
                double s = t[i] + (t[i + 1] - t[i]) * k / perInterval;
                dense.Add((Cubic(t, points.Select(point => point.X).ToArray(), mx, i, s), Cubic(t, points.Select(point => point.Y).ToArray(), my, i, s)));
            }
        }
        dense.Add((points[^1].X, points[^1].Y));
        var upper = dense.Take(noseDense + 1).Reverse().ToArray();
        var lower = dense.Skip(noseDense).ToArray();
        return (upper, lower);
    }

    private static double[] NaturalSecondDerivatives(double[] t, double[] v)
    {
        int n = t.Length;
        var m = new double[n];
        if (n < 3) return m;
        var c = new double[n];
        var d = new double[n];
        for (int i = 1; i < n - 1; i++)
        {
            double h0 = t[i] - t[i - 1], h1 = t[i + 1] - t[i];
            double a = h0 / 6, b = (h0 + h1) / 3, cc = h1 / 6;
            double r = (v[i + 1] - v[i]) / h1 - (v[i] - v[i - 1]) / h0;
            double denominator = b - a * c[i - 1];
            c[i] = cc / denominator;
            d[i] = (r - a * d[i - 1]) / denominator;
        }
        for (int i = n - 2; i >= 1; i--) m[i] = d[i] - c[i] * m[i + 1];
        return m;
    }

    private static double Cubic(double[] t, double[] v, double[] m, int i, double s)
    {
        double h = t[i + 1] - t[i], a = (t[i + 1] - s) / h, b = (s - t[i]) / h;
        return a * v[i] + b * v[i + 1] + ((a * a * a - a) * m[i] + (b * b * b - b) * m[i + 1]) * h * h / 6;
    }

    /// <summary>
    /// Reads coordinates into the chord frame (m12d F-5): the leading edge is the minimum-x sample, the chord runs to the
    /// trailing-edge midpoint at unit length, and the shift, turn and scale are returned so Replace can report them.
    /// </summary>
    internal static (DatProfile Profile, double LeShift, double RotationDegrees, double Scale) ParseInChordFrame(byte[] dat)
    {
        var raw = ParseRaw(dat);
        var le = raw.Upper[0];
        double mx = (raw.Upper[^1].X + raw.Lower[^1].X) / 2 - le.X, my = (raw.Upper[^1].Y + raw.Lower[^1].Y) / 2 - le.Y;
        double scale = Math.Sqrt(mx * mx + my * my);
        if (!(scale > 1e-12)) throw new ContractError("DSL-IMPORT", raw.FirstLine);
        double angle = Math.Atan2(my, mx), cos = Math.Cos(angle), sin = Math.Sin(angle);
        List<ProfilePoint> Frame(IReadOnlyList<ProfilePoint> side)
        {
            var list = side.Select(point =>
            {
                double dx = point.X - le.X, dy = point.Y - le.Y;
                return new ProfilePoint((dx * cos + dy * sin) / scale, (-dx * sin + dy * cos) / scale);
            }).ToList();
            list[0] = new(0, 0);
            return list;
        }
        var profile = new DatProfile(raw.Name, raw.Format, Frame(raw.Upper).AsReadOnly(), Frame(raw.Lower).AsReadOnly(), dat, raw.Count);
        return (profile, Math.Sqrt(le.X * le.X + le.Y * le.Y) / scale, angle / PlacementRule.RadiansPerDegree, scale);
    }

    internal static (double[] Knots, double[] ControlX) OwnSqrtBasis(int count)
    {
        var knots = new double[count + 6];
        for (int i = 0; i <= 5; i++) knots[i] = 0;
        int interior = count - 6;
        for (int i = 1; i <= interior; i++) knots[5 + i] = (double)i / (interior + 1);
        for (int i = knots.Length - 6; i < knots.Length; i++) knots[i] = 1;

        var controlX = new double[count];
        for (int i = 0; i < count; i++)
        {
            double sum = 0;
            for (int j = 1; j <= 5; j++)
                for (int k = j + 1; k <= 5; k++)
                    sum += knots[i + j] * knots[i + k];
            controlX[i] = sum / 10.0;
        }
        controlX[0] = 0;
        controlX[1] = 0;
        controlX[count - 1] = 1.0;
        return (knots, controlX);
    }

    private static bool MatchesOwnSqrtBasis(double[] knots, double[] controlX)
    {
        int count = controlX.Length;
        if (count is < 8 or > 16 || knots.Length != count + 6) return false;
        var (ownKnots, ownX) = OwnSqrtBasis(count);
        return SameNumbers(ownKnots, knots) && SameNumbers(ownX, controlX);
    }

    private static bool SameNumbers(double[] left, double[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
            if (DecimalSi.Parse(FormatNumber(left[index])) != right[index]) return false;
        return true;
    }

    private static bool IsClosed(DatProfile profile) =>
        Math.Abs(profile.Upper[^1].Y) <= 1e-9 && Math.Abs(profile.Lower[^1].Y) <= 1e-9;

    private static string ProvenanceOf(DatProfile profile) =>
        $"{profile.Format} sha256:{Identity.Sha256(profile.OriginalBytes)} points:{profile.OriginalPointCount}";

    private static (double[,] A, double[] B) EndpointPins(int count, bool closed)
    {
        if (closed)
        {
            var pinned = new double[2, count];
            pinned[0, 0] = 1;
            pinned[1, count - 1] = 1;
            return (pinned, [0, 0]);
        }
        var leading = new double[1, count];
        leading[0, 0] = 1;
        return (leading, [0]);
    }

    private static bool TryOrdinates(IReadOnlyList<ProfilePoint> points, double[] knots, double[] controlX, int degree,
        double[,] smoothing, double[,] constraints, double[] bound, bool closed, bool sqrtParameter, out double[] ordinates, out double maxResidual)
    {
        ordinates = [];
        maxResidual = double.PositiveInfinity;
        int count = controlX.Length;
        int samples = points.Count;
        var design = new double[samples, count];
        var target = new double[samples];
        var weight = new double[samples];
        var parameter = new double[samples];
        for (int row = 0; row < samples; row++)
        {
            parameter[row] = sqrtParameter ? Math.Sqrt(Math.Max(0, points[row].X)) : ParameterAt(knots, degree, controlX, points[row].X);
            target[row] = points[row].Y;
            weight[row] = 1;
            double[] values = SplineBasis.Values(knots, degree, parameter[row]);
            for (int column = 0; column < count; column++) design[row, column] = values[column];
        }

        double[] solved;
        try { solved = ConstrainedFit.Solve(design, target, weight, smoothing, 0, constraints, bound); }
        catch (ContractError error) when (error.Code == "GEOMETRY-FIT-SINGULAR") { return false; }
        if (solved.Any(value => !double.IsFinite(value))) return false;
        solved[0] = 0;
        if (closed) solved[count - 1] = 0;

        double max = 0;
        for (int row = 0; row < samples; row++)
        {
            double[] values = SplineBasis.Values(knots, degree, parameter[row]);
            double fitted = 0;
            for (int column = 0; column < count; column++) fitted += solved[column] * values[column];
            double gap = Math.Abs(fitted - points[row].Y);
            if (gap > max) max = gap;
        }
        ordinates = solved;
        maxResidual = max;
        return true;
    }

    private static double ParameterAt(double[] knots, int degree, double[] controlX, double target)
    {
        double At(double parameter)
        {
            double[] values = SplineBasis.Values(knots, degree, parameter);
            double sum = 0;
            for (int index = 0; index < controlX.Length; index++) sum += values[index] * controlX[index];
            return sum;
        }
        if (target <= At(0)) return 0;
        if (target >= At(1)) return 1;
        double low = 0, high = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = 0.5 * (low + high);
            if (At(mid) < target) low = mid;
            else high = mid;
        }
        return 0.5 * (low + high);
    }

    private static string ProfileBlock(string name, int degree, string closure, string provenance, double[] knots, double[] controlX, double[] upper, double[] lower)
    {
        int count = controlX.Length;
        string knotsText = string.Join(", ", knots.Select(FormatNumber));
        string idsText = string.Join(", ", Enumerable.Range(0, count).Select(index => Jcs.Quote($"cv-{index}")));
        string upperText = string.Join(", ", Enumerable.Range(0, count).Select(index => $"({FormatNumber(controlX[index])}, {FormatNumber(upper[index])})"));
        string lowerText = string.Join(", ", Enumerable.Range(0, count).Select(index => $"({FormatNumber(controlX[index])}, {FormatNumber(lower[index])})"));
        return
            $"profile {Jcs.Quote(name)} {{\n" +
            $"  upper cv {{ degree {degree.ToString(CultureInfo.InvariantCulture)} knots [{knotsText}] points [{upperText}] ids [{idsText}] }}\n" +
            $"  lower cv {{ degree {degree.ToString(CultureInfo.InvariantCulture)} knots [{knotsText}] points [{lowerText}] ids [{idsText}] }}\n" +
            $"  closure {closure}\n" +
            $"  provenance {Jcs.Quote(provenance)}\n" +
            "}";
    }

    private static string FormatNumber(double v)
    {
        if (v == 0 || Math.Abs(v) < 1e-15) return "0";
        if (v == 1) return "1";
        return v.ToString("R", CultureInfo.InvariantCulture);
    }

    private static double[] ParseRowNumbers(string line, int lineNumber)
    {
        var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var numbers = new double[parts.Length];
        for (int k = 0; k < parts.Length; k++)
        {
            string token = parts[k];
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                if (token.EndsWith('.') && double.TryParse(token[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                {
                    // parsed without trailing dot
                }
                else
                {
                    throw new ContractError("DSL-IMPORT", lineNumber);
                }
            }
            if (!double.IsFinite(val))
                throw new ContractError("DSL-IMPORT", lineNumber);
            numbers[k] = val;
        }
        return numbers;
    }
}
