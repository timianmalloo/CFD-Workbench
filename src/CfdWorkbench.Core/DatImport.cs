using System.Globalization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

public sealed record DatProfile(string Name, string Format, IReadOnlyList<ProfilePoint> Upper, IReadOnlyList<ProfilePoint> Lower, byte[] OriginalBytes, int OriginalPointCount);

public sealed record ImportedProfile(string ProfileBlock, double MaxResidual, int VertexCount, string Provenance, bool Accepted);

public static class DatImport
{
    public static string Slug(string name)
    {
        string slug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "profile" : slug;
    }

    public static DatProfile Parse(byte[] dat)
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
            throw new ContractError("DSL-IMPORT", validLines[1].LineNumber);

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

        return new DatProfile(rawName, format, upperNorm.AsReadOnly(), lowerNorm.AsReadOnly(), dat, originalPointCount);
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

    /// <summary>Fit ordinates only. Knots and control-vertex x stay on the supplied basis.</summary>
    public static ImportedProfile? FitToBasis(DatProfile profile, string name, double[] knots, double[] controlX, int degree)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (knots is null || controlX is null || degree < 1) return null;
        int count = controlX.Length;
        if (count < 2 || knots.Length != count + degree + 1) return null;
        if (profile.Upper.Count == 0 || profile.Lower.Count == 0) return null;

        bool closed = IsClosed(profile);
        bool sqrtParameter = degree == 5 && MatchesOwnSqrtBasis(knots, controlX);
        var (constraints, bound) = EndpointPins(count, closed);
        var smoothing = new double[count, count];
        if (!TryOrdinates(profile.Upper, knots, controlX, degree, smoothing, constraints, bound, closed, sqrtParameter, out double[] upper, out double upperResidual))
            return null;
        if (!TryOrdinates(profile.Lower, knots, controlX, degree, smoothing, constraints, bound, closed, sqrtParameter, out double[] lower, out double lowerResidual))
            return null;

        double maxResidual = Math.Max(upperResidual, lowerResidual);
        if (!double.IsFinite(maxResidual)) return null;
        string provenance = ProvenanceOf(profile);
        string block = ProfileBlock(name, degree, closed ? "closed" : "open", provenance, knots, controlX, upper, lower);
        return new ImportedProfile(block, maxResidual, count, provenance, maxResidual <= 1e-5);
    }

    internal static string OwnSpacingReason(double neighbourResidual)
    {
        string residual = neighbourResidual.ToString("G17", CultureInfo.InvariantCulture);
        return "The imported shape needs its own vertex spacing (residual " + residual + " on the neighbour basis). Blending across different spacings is not certified yet: import it at every station that shares this profile, or Rebuild the neighbouring profiles.";
    }

    private static (double[] Knots, double[] ControlX) OwnSqrtBasis(int count)
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
