using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core;

/// <summary>Which section a .dat holds: the one the wing builds at the station, or the profile as authored (Export design 4.1).</summary>
public enum DatShape { AtStation, Own }

/// <summary>Selig: trailing edge over the top to the nose and back under. Lednicer: counts, upper nose to tail, blank line, lower nose to tail.</summary>
public enum DatOrder { Selig, Lednicer }

/// <summary>
/// One section .dat: its bytes and everything the dialog states about it. Points are chord fractions, nose first, per surface.
/// <paramref name="TrailingEdgeMm"/> is read from the points as written (last upper minus last lower, times the chord);
/// <paramref name="DeviationMm"/> is the largest distance between the curve and the lines joining the points, sampled at the
/// segment midpoints, at this chord (EXP-02, "deviation"). Neither is a bound or a certificate.
/// </summary>
public sealed record DatExportResult(byte[] Bytes, string NameLine, int PointCount, int PointsPerSurface, double ChordMeters,
    double StationThicknessRatio, double TrailingEdgeMm, double DeviationMm,
    IReadOnlyList<ProfilePoint> Upper, IReadOnlyList<ProfilePoint> Lower);

/// <summary>The section .dat writer (Export design 4.1; build conditions B5, B6, B9).</summary>
public static class DatExport
{
    public static IReadOnlyList<int> PointChoices { get; } = [61, 101, 201];

    public const int NameLineLimit = 80;

    /// <summary>The .dat of authored station <paramref name="assignment"/> of the accepted <paramref name="source"/>.</summary>
    public static DatExportResult Build(byte[] source, string foilName, int assignment, string stationName, DatShape shape,
        DatOrder order, int pointsPerSurface, int revision, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        Guard.Require(PointChoices.Contains(pointsPerSurface), "DSL-RANGE");
        return BuildAt(source, Placement.StationEta(source, assignment), assignment, foilName, stationName, shape, order, pointsPerSurface,
            revision, cancellation);
    }

    // The At-station section at any eta (a blended eta is not an authored station, so Own needs an assignment).
    internal static DatExportResult BuildAt(byte[] source, double eta, int assignment, string foilName, string stationName,
        DatShape shape, DatOrder order, int pointsPerSurface, int revision, CancellationToken cancellation = default)
    {
        int fine = 2 * pointsPerSurface - 1;
        var coarseX = Placement.ChordGrid(pointsPerSurface);
        var fineX = Placement.ChordGrid(fine);
        var frame = Placement.Frame(source, eta);
        var (upperAtCoarse, lowerAtCoarse) = Ordinates(source, eta, assignment, shape, coarseX, cancellation);
        var (upperAtFine, lowerAtFine) = Ordinates(source, eta, assignment, shape, fineX, cancellation);
        var upper = new ProfilePoint[pointsPerSurface];
        var lower = new ProfilePoint[pointsPerSurface];
        for (int index = 0; index < pointsPerSurface; index++)
        {
            upper[index] = new(coarseX[index], upperAtCoarse[index]);
            lower[index] = new(coarseX[index], lowerAtCoarse[index]);
        }
        string nameLine = NameLine(foilName, stationName, revision);
        byte[] bytes = Encoding.UTF8.GetBytes(Text(nameLine, upper, lower, order));
        double chord = frame.ChordMeters;
        double trailingEdgeMm = (upper[^1].Y - lower[^1].Y) * chord * 1000;
        double deviation = Math.Max(Deviation(fineX, upperAtFine), Deviation(fineX, lowerAtFine)) * chord * 1000;
        int count = order == DatOrder.Selig ? 2 * pointsPerSurface - 1 : 2 * pointsPerSurface;
        return new(bytes, nameLine, count, pointsPerSurface, chord, frame.ThicknessRatio, trailingEdgeMm, deviation, upper, lower);
    }

    private static (double[] Upper, double[] Lower) Ordinates(byte[] source, double eta, int assignment, DatShape shape,
        IReadOnlyList<double> xs, CancellationToken cancellation)
    {
        if (shape == DatShape.Own)
        {
            var own = Placement.OwnProfile(source, assignment, xs);
            return (own.Upper.ToArray(), own.Lower.ToArray());
        }
        // At station: camber plus and minus half the thickness at the same x (vertical), the thickness peak being the station t/c.
        var section = Placement.Sections(source, [eta], xs, cancellation)[0];
        var upper = new double[xs.Count];
        var lower = new double[xs.Count];
        for (int index = 0; index < upper.Length; index++)
        {
            upper[index] = section.Camber[index] + section.Thickness[index] / 2;
            lower[index] = section.Camber[index] - section.Thickness[index] / 2;
        }
        return (upper, lower);
    }

    // Largest perpendicular distance from each odd (midpoint) sample of the fine grid to the line joining its two even neighbours.
    private static double Deviation(IReadOnlyList<double> xs, double[] z)
    {
        double deviation = 0;
        for (int index = 1; index + 1 < xs.Count; index += 2)
        {
            double ax = xs[index - 1], ay = z[index - 1], bx = xs[index + 1], by = z[index + 1];
            double dx = bx - ax, dy = by - ay, length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) continue;
            deviation = Math.Max(deviation, Math.Abs(dy * (xs[index] - ax) - dx * (z[index] - ay)) / length);
        }
        return deviation;
    }

    private static string Text(string nameLine, ProfilePoint[] upper, ProfilePoint[] lower, DatOrder order)
    {
        var text = new StringBuilder();
        text.Append(nameLine).Append('\n');
        if (order == DatOrder.Selig)
        {
            for (int index = upper.Length - 1; index >= 0; index--) Row(text, upper[index]);
            for (int index = 1; index < lower.Length; index++) Row(text, lower[index]);
            return text.ToString();
        }
        text.Append(upper.Length.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(lower.Length.ToString(CultureInfo.InvariantCulture)).Append('\n');
        foreach (var point in upper) Row(text, point);
        text.Append('\n');
        foreach (var point in lower) Row(text, point);
        return text.ToString();
    }

    private static void Row(StringBuilder text, ProfilePoint point) =>
        text.Append(Number(point.X)).Append(' ').Append(Number(point.Y)).Append('\n');

    /// <summary>The shortest text that parses back to the same bits (round-trip digits), in positional notation, never exponent form (B5).</summary>
    internal static string Number(double value)
    {
        Guard.Require(double.IsFinite(value), "DSL-RANGE");
        if (value == 0) return "0";   // also turns -0 into 0
        string shortest = value.ToString("R", CultureInfo.InvariantCulture);
        int mark = shortest.IndexOfAny(['E', 'e']);
        if (mark < 0) return shortest;
        // The digits and exponent of d.dddE+xx: slide the decimal point, never change a digit.
        bool negative = shortest[0] == '-';
        string mantissa = shortest[(negative ? 1 : 0)..mark];
        int exponent = int.Parse(shortest[(mark + 1)..], CultureInfo.InvariantCulture);
        int point = mantissa.IndexOf('.');
        string digits = mantissa.Replace(".", "", StringComparison.Ordinal);
        int integerDigits = (point < 0 ? mantissa.Length : point) + exponent;
        string text = integerDigits <= 0
            ? "0." + new string('0', -integerDigits) + digits
            : integerDigits >= digits.Length
                ? digits + new string('0', integerDigits - digits.Length)
                : digits[..integerDigits] + "." + digits[integerDigits..];
        return negative ? "-" + text : text;
    }

    /// <summary>
    /// <c>foil | station | rN</c> without control or format (bidi) characters and at most 80 characters. It always starts with a letter (B6), so
    /// no reader can take it for a point row or the Lednicer counts.
    /// </summary>
    internal static string NameLine(string foilName, string stationName, int revision)
    {
        string line = Clean($"{foilName} | {stationName} | r{revision.ToString(CultureInfo.InvariantCulture)}");
        // A letter first: no reader (a Fortran list-directed read takes 1,2 and 2*0.5 and 1d0 as numbers) can take the line for a row.
        string first = line.TrimStart();
        return Truncate(first.Length > 0 && char.IsLetter(first[0]) ? line : "foil " + line);
    }

    private static string Clean(string text) => new(text.Where(character => !char.IsControl(character) && char.GetUnicodeCategory(character) != UnicodeCategory.Format).ToArray());

    private static string Truncate(string text)
    {
        if (text.Length <= NameLineLimit) return text;
        return char.IsHighSurrogate(text[NameLineLimit - 1]) ? text[..(NameLineLimit - 1)] : text[..NameLineLimit];
    }
}
