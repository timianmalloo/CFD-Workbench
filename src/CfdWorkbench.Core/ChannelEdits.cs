using System.Globalization;

namespace CfdWorkbench.Core;

/// <summary>
/// Planform point verbs. Add is one Boehm insertion. Remove drops one knot and refits two
/// points. Rebuild fits 4–10 points on the curve's own knot spacing. Floor 4, ceiling 16.
/// </summary>
internal static class ChannelEdits
{
    internal const int Floor = 4;
    internal const int Ceiling = 16;
    internal const string TooCloseToRefit = "These points are too close together to refit. Move them apart, then try again.";

    internal readonly record struct Refusal(string Code, string Reason);
    internal readonly record struct AddResult(Curve Curve, int Index, string? Notice);
    internal readonly record struct RemoveResult(Curve Curve, int SelectIndex, string? Notice);
    internal readonly record struct RebuildResult(Curve Curve, bool Identity, string? Notice);
    internal readonly record struct Change(double Max, double AtEta);

    private static readonly double[] Gauss4 =
    [
        -0.8611363115940525752239465,
        -0.3399810435848562648026658,
        0.3399810435848562648026658,
        0.8611363115940525752239465
    ];

    internal static AddResult Add(Curve curve, double eta)
    {
        if (!double.IsFinite(eta)) throw new ContractError("DSL-CURVE", TooClose(Nearest(curve, 0)));
        if (curve.Points.Length >= Ceiling) throw new ContractError("DSL-CURVE", AtCeiling(curve));
        double t = ChannelEvaluator.Parameter(curve.Knots, curve.Degree, curve.Points, eta);
        foreach (double knot in curve.Knots)
            if (Math.Abs(knot - t) <= 1e-9)
                throw new ContractError("DSL-CURVE", TooClose(Nearest(curve, eta)));
        var (knots, points, inserted) = FoilSource.InsertOnce(curve.Knots, curve.Points, curve.Degree, t);
        for (int index = 1; index < points.Length; index++)
            if (points[index][0] - points[index - 1][0] < 1e-7)
                throw new ContractError("DSL-CURVE", TooClose(Nearest(curve, eta)));
        var ids = new List<string>(curve.Ids);
        ids.Insert(inserted, FreshOne(curve.Ids));
        var tangents = curve.Tangents.ToArray();
        string? notice = RewriteSymmetric(points, ids.ToArray(), ref tangents);
        return new(Shape(curve, knots, points, ids.ToArray(), tangents), inserted, notice);
    }

    internal static Refusal? RemoveRefusal(CurveView view, string id)
    {
        var point = view.Points.FirstOrDefault(item => item.Id == id);
        if (point is null) return new("DSL-TARGET", "DSL-TARGET");
        if (view.Points.Count <= Floor)
            return new("DSL-CURVE", $"A curve needs at least 4 points. The {Label(view.Curve)} has {view.Points.Count}, so point {point.Index + 1} stays.");
        switch (point.Role)
        {
            case PointRole.RootEnd:
                return new("DSL-LOCK", "The root end can't be removed: the curve starts there.");
            case PointRole.TipEnd:
                return new("DSL-LOCK", "The tip end can't be removed: the curve ends there.");
            case PointRole.RootHandle:
                return new("DSL-LOCK", "This handle sets the curve's direction at the root. Move it, or rebuild the curve with fewer points.");
            case PointRole.TipHandle:
                return new("DSL-LOCK", "This handle sets the curve's direction at the tip. Move it, or rebuild the curve with fewer points.");
            case PointRole.Anchor:
                return new("DSL-LOCK", $"Point {point.Index + 1} is an anchor. Make it a control point first, then remove it.");
            case PointRole.AnchorHandle:
                int anchor = view.Points.First(item => item.Id == point.AnchorId).Index + 1;
                return new("DSL-LOCK", $"Point {point.Index + 1} is a handle of the anchor at point {anchor}. Make that anchor a control point first.");
        }
        string? named = point.Locks.FirstOrDefault(item => item != "root_mirror");
        if (named is not null)
            return new("DSL-LOCK", $"Point {point.Index + 1} is locked ({named}).");
        return null;
    }

    internal static RemoveResult Remove(Curve curve, int index, bool rootMirror)
    {
        int count = curve.Points.Length;
        if (index < 2 || index > count - 3) throw new ContractError("DSL-LOCK");
        int knotIndex = index + 2;
        var knots = new double[curve.Knots.Length - 1];
        Array.Copy(curve.Knots, knots, knotIndex);
        Array.Copy(curve.Knots, knotIndex + 1, knots, knotIndex, curve.Knots.Length - knotIndex - 1);
        int nextCount = count - 1;
        var points = new double[nextCount][];
        var ids = new string[nextCount];
        for (int slot = 0; slot < nextCount; slot++)
        {
            int source = slot < index ? slot : slot + 1;
            points[slot] = (double[])curve.Points[source].Clone();
            ids[slot] = curve.Ids[source];
        }
        int left = index - 1;
        int right = index;
        points[left][0] = Abscissa(curve, Greville(knots, curve.Degree, left));
        points[right][0] = Abscissa(curve, Greville(knots, curve.Degree, right));
        if (!Strict(points)) throw new ContractError("DSL-CURVE", Fold(index));
        var pinned = new bool[nextCount];
        var values = new double[nextCount];
        for (int slot = 0; slot < nextCount; slot++)
        {
            if (slot == left || slot == right) continue;
            pinned[slot] = true;
            values[slot] = points[slot][1];
        }
        PinEndsAndRows(curve, index, points, pinned, values, rootMirror);
        var fitted = Fit(Shape(curve, knots, points, ids, curve.Tangents), curve, pinned, values);
        for (int slot = 0; slot < nextCount; slot++)
            points[slot][1] = pinned[slot] ? values[slot] : fitted[slot];
        if (!Strict(points)) throw new ContractError("DSL-CURVE", Fold(index));
        var tangents = curve.Tangents.Where(row => ids.Contains(row.Id)).ToArray();
        string? broken = RewriteSymmetric(points, ids, ref tangents);
        double removedEta = curve.Points[index][0];
        int select = 0;
        double best = double.MaxValue;
        for (int slot = 0; slot < nextCount; slot++)
        {
            double distance = Math.Abs(points[slot][0] - removedEta);
            if (distance < best || distance == best && points[slot][0] > points[select][0])
            {
                best = distance;
                select = slot;
            }
        }
        return new(Shape(curve, knots, points, ids, tangents), select, broken);
    }

    internal static RebuildResult Rebuild(Curve curve, int count, bool rootMirror)
    {
        if (count < Floor || count > 10)
            throw new ContractError("DSL-CURVE", "Enter a whole number from 4 to 10.");
        double[] knots = KnotsFor(curve, count);
        var points = new double[count][];
        for (int index = 0; index < count; index++) points[index] = new double[2];
        points[0][0] = 0;
        points[^1][0] = 1;
        points[0][1] = curve.Points[0][1];
        points[^1][1] = curve.Points[^1][1];
        for (int index = 1; index < count - 1; index++)
            points[index][0] = Abscissa(curve, Greville(knots, curve.Degree, index));
        if (!Strict(points)) throw new ContractError("DSL-CURVE", TooCloseToRefit);
        bool square = rootMirror || curve.Points[0][1] == curve.Points[1][1];
        var pinned = new bool[count];
        var values = new double[count];
        pinned[0] = pinned[^1] = true;
        values[0] = points[0][1];
        values[^1] = points[^1][1];
        if (square && count > 2)
        {
            pinned[1] = true;
            values[1] = curve.Points[0][1];
        }
        var draft = Shape(curve, knots, points, curve.Ids, []);
        double[] fitted = Fit(draft, curve, pinned, values);
        for (int index = 0; index < count; index++)
            points[index][1] = pinned[index] ? values[index] : fitted[index];
        bool sameKnots = count == curve.Points.Length && KnotsEqual(knots, curve.Knots);
        var candidate = Shape(curve, knots, points, FreshKeepingEnds(curve, count), []);
        if (sameKnots && WithinIdentity(MaxChange(curve, candidate).Max, curve))
            return new(curve, true, null);
        return new(candidate, false, null);
    }

    internal static Change MaxChange(Curve before, Curve after)
    {
        var etas = new List<double>();
        for (int step = 0; step <= 200; step++) etas.Add(step / 200d);
        CollectKnotEtas(before, etas);
        CollectKnotEtas(after, etas);
        double max = 0, at = 0;
        foreach (double eta in etas)
        {
            double delta = Math.Abs(Ordinate(after, eta) - Ordinate(before, eta));
            if (delta > max) { max = delta; at = eta; }
        }
        return new(max, at);
    }

    internal static int CurvatureBreaks(Curve curve)
    {
        var kappas = new List<double>();
        for (int step = 0; step <= 200; step++) kappas.Add(Math.Abs(Kappa(curve, step / 200d)));
        double scale = 0;
        foreach (double value in kappas) scale = Math.Max(scale, value);
        double limit = 1e-6 * Math.Max(scale, 1e-15);
        int breaks = 0;
        double previous = double.NaN;
        for (int index = curve.Degree + 1; index < curve.Knots.Length - curve.Degree - 1; index++)
        {
            double knot = curve.Knots[index];
            if (knot <= 0 || knot >= 1 || knot == previous) continue;
            previous = knot;
            double left = Kappa(curve, Math.Max(0, knot - 1e-9));
            double right = Kappa(curve, Math.Min(1, knot + 1e-9));
            if (Math.Abs(right - left) > limit) breaks++;
        }
        return breaks;
    }

    internal static double DefaultEta(Curve curve, int index)
    {
        double greville = Greville(curve.Knots, curve.Degree, index);
        int span = curve.Degree;
        while (span + 1 < curve.Points.Length && curve.Knots[span + 1] <= greville) span++;
        if (span + 1 >= curve.Knots.Length) span = curve.Knots.Length - 2;
        double mid = (curve.Knots[span] + curve.Knots[span + 1]) / 2;
        return Abscissa(curve, mid);
    }

    internal static double TipAngleDegrees(Curve curve, double halfSpanMeters)
    {
        double span = (curve.Points[^1][0] - curve.Points[^2][0]) * halfSpanMeters;
        double ordinate = curve.Points[^1][1] - curve.Points[^2][1];
        return Math.Atan2(ordinate, span) * 180 / Math.PI;
    }

    internal static bool RailsCross(Curve leading, Curve trailing, out double atEta)
    {
        atEta = 0;
        bool crossed = false;
        double worst = 0;
        for (int step = 0; step <= 400; step++)
        {
            double eta = step / 400d;
            double gap = Ordinate(trailing, eta) - Ordinate(leading, eta);
            if (gap < -1e-9 && (!crossed || gap < worst))
            {
                crossed = true;
                worst = gap;
                atEta = eta;
            }
        }
        return crossed;
    }

    internal static string CrossingCopy(int count, double fromRootMillimetres) =>
        "With " + count.ToString(CultureInfo.InvariantCulture) + " points the leading and trailing edges would cross at " +
        fromRootMillimetres.ToString("0.0", CultureInfo.InvariantCulture) + " mm from root. Choose more points.";

    internal static int InflectionCount(Curve curve, double halfSpanMeters, out double firstFromRootMillimetres)
    {
        firstFromRootMillimetres = 0;
        const int samples = 2001;
        var kappa = new double[samples];
        double scale = 0;
        for (int step = 0; step < samples; step++)
        {
            kappa[step] = KappaPhysical(curve, step / (double)(samples - 1), halfSpanMeters);
            scale = Math.Max(scale, Math.Abs(kappa[step]));
        }
        double floor = Math.Max(1e-6 * scale, 1e-9);
        int sign = 0, changes = 0;
        for (int step = 0; step < samples; step++)
        {
            if (Math.Abs(kappa[step]) <= floor) continue;
            int next = kappa[step] > 0 ? 1 : -1;
            if (sign != 0 && next != sign)
            {
                changes++;
                if (changes == 1) firstFromRootMillimetres = step / (double)(samples - 1) * halfSpanMeters * 1000;
            }
            sign = next;
        }
        return changes;
    }

    private static void PinEndsAndRows(Curve curve, int removed, double[][] points, bool[] pinned, double[] values, bool rootMirror)
    {
        int count = curve.Points.Length;
        int left = removed - 1;
        int right = removed;
        if (left == 1)
            PinHandle(curve, 0, 1, points[left][0], left, pinned, values, rootMirror || curve.Points[0][1] == curve.Points[1][1]);
        if (right == count - 3)
            PinHandle(curve, count - 2, count - 1, points[right][0], right, pinned, values, false);
        PinAnchorHandle(curve, removed - 1, left, points, pinned, values);
        PinAnchorHandle(curve, removed + 1, right, points, pinned, values);
    }

    private static void PinHandle(Curve curve, int end, int handle, double abscissa, int slot, bool[] pinned, double[] values, bool square)
    {
        pinned[slot] = true;
        if (square) { values[slot] = curve.Points[end][1]; return; }
        double dx = curve.Points[handle][0] - curve.Points[end][0];
        double slope = dx == 0 ? 0 : (curve.Points[handle][1] - curve.Points[end][1]) / dx;
        values[slot] = curve.Points[end][1] + slope * (abscissa - curve.Points[end][0]);
    }

    private static void PinAnchorHandle(Curve curve, int oldIndex, int slot, double[][] points, bool[] pinned, double[] values)
    {
        if (oldIndex <= 0 || oldIndex >= curve.Points.Length - 1) return;
        int anchor = -1, other = -1;
        if (FoilSource.IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, oldIndex - 1))
        {
            anchor = oldIndex - 1;
            other = oldIndex - 2;
        }
        else if (FoilSource.IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, oldIndex + 1))
        {
            anchor = oldIndex + 1;
            other = oldIndex + 2;
        }
        if (anchor < 0 || other < 0 || other >= curve.Points.Length) return;
        double dx = curve.Points[other][0] - curve.Points[anchor][0];
        double slope = dx == 0 ? 0 : (curve.Points[other][1] - curve.Points[anchor][1]) / dx;
        pinned[slot] = true;
        values[slot] = curve.Points[anchor][1] + slope * (points[slot][0] - curve.Points[anchor][0]);
    }

    private static double[] Fit(Curve candidate, Curve target, bool[] pinned, double[] pinnedValue)
    {
        int count = candidate.Points.Length;
        var free = new List<int>();
        for (int index = 0; index < count; index++)
            if (!pinned[index]) free.Add(index);
        if (free.Count == 0) return (double[])pinnedValue.Clone();
        var samples = Samples(candidate);
        if (!SchoenbergWhitney(candidate.Knots, candidate.Degree, free, samples))
            throw new ContractError("DSL-CURVE", TooCloseToRefit);
        int rows = samples.Count;
        var design = new double[rows, count];
        var load = new double[rows];
        var weight = new double[rows];
        for (int row = 0; row < rows; row++)
        {
            double[] basis = SplineBasis.Values(candidate.Knots, candidate.Degree, samples[row].T);
            for (int column = 0; column < count; column++) design[row, column] = basis[column];
            load[row] = Ordinate(target, samples[row].Eta);
            weight[row] = 1;
        }
        var held = new List<int>();
        for (int index = 0; index < count; index++)
            if (pinned[index]) held.Add(index);
        var matrix = new double[held.Count, count];
        var rhs = new double[held.Count];
        for (int row = 0; row < held.Count; row++)
        {
            matrix[row, held[row]] = 1;
            rhs[row] = pinnedValue[held[row]];
        }
        try
        {
            return ConstrainedFit.Solve(design, load, weight, new double[count, count], 0, matrix, rhs);
        }
        catch (ContractError error) when (error.Code == "GEOMETRY-FIT-SINGULAR")
        {
            throw new ContractError("DSL-CURVE", TooCloseToRefit);
        }
    }

    private static List<(double Eta, double T)> Samples(Curve candidate)
    {
        var samples = new List<(double Eta, double T)>();
        for (int step = 0; step <= 400; step++)
        {
            double eta = step / 400d;
            samples.Add((eta, ChannelEvaluator.Parameter(candidate.Knots, candidate.Degree, candidate.Points, eta)));
        }
        for (int span = candidate.Degree; span < candidate.Points.Length; span++)
        {
            double start = candidate.Knots[span], end = candidate.Knots[span + 1];
            if (end <= start) continue;
            foreach (double node in Gauss4)
            {
                double t = 0.5 * ((start + end) + (end - start) * node);
                double eta = Math.Clamp(Abscissa(candidate, t), 0, 1);
                samples.Add((eta, t));
            }
        }
        return samples;
    }

    private static bool SchoenbergWhitney(double[] knots, int degree, List<int> free, List<(double Eta, double T)> samples)
    {
        var parameters = samples.Select(sample => sample.T).Order().ToArray();
        double previous = double.NegativeInfinity;
        foreach (int index in free)
        {
            double lower = knots[index], upper = knots[index + degree + 1];
            if (upper <= lower) return false;
            double pick = double.NaN;
            foreach (double t in parameters)
                if (t > previous && t > lower && t < upper) { pick = t; break; }
            if (double.IsNaN(pick)) return false;
            previous = pick;
        }
        return true;
    }

    private static double[] KnotsFor(Curve curve, int count)
    {
        var distinct = new List<double> { curve.Knots[0] };
        foreach (double knot in curve.Knots)
            if (knot != distinct[^1]) distinct.Add(knot);
        int degree = curve.Degree;
        var knots = new double[count + degree + 1];
        for (int index = 0; index <= degree; index++) knots[index] = 0;
        for (int index = 0; index <= degree; index++) knots[knots.Length - 1 - index] = 1;
        int interior = count - degree - 1;
        double steps = count - 3;
        for (int place = 1; place <= interior; place++)
        {
            double position = place / steps * (distinct.Count - 1);
            int lower = (int)Math.Floor(position);
            int upper = Math.Min(distinct.Count - 1, lower + 1);
            double fraction = position - lower;
            knots[degree + place] = lower == upper ? distinct[lower] : distinct[lower] * (1 - fraction) + distinct[upper] * fraction;
        }
        return knots;
    }

    private static string[] FreshKeepingEnds(Curve curve, int count)
    {
        int max = MaxNumber(curve.Ids);
        var ids = new string[count];
        ids[0] = curve.Ids[0];
        ids[^1] = curve.Ids[^1];
        for (int index = 1; index < count - 1; index++)
            ids[index] = "cv-" + (++max).ToString(CultureInfo.InvariantCulture);
        return ids;
    }

    private static string? RewriteSymmetric(double[][] points, string[] ids, ref TangentRow[] tangents)
    {
        var notes = new List<string>();
        var rows = tangents.ToArray();
        for (int row = 0; row < rows.Length; row++)
        {
            if (rows[row].Kind != "symmetric") continue;
            int anchor = Array.IndexOf(ids, rows[row].Id);
            if (anchor <= 0 || anchor >= ids.Length - 1) continue;
            double left = Distance(points[anchor], points[anchor - 1]);
            double right = Distance(points[anchor], points[anchor + 1]);
            double scale = Math.Max(1, Math.Max(left, right));
            if (Math.Abs(left - right) <= 1e-9 * scale) continue;
            rows[row] = rows[row] with { Kind = "smooth" };
            notes.Add(SmoothNotice(anchor + 1));
        }
        tangents = rows;
        return notes.Count == 0 ? null : string.Join(" ", notes);
    }

    private static string SmoothNotice(int anchorNumber) =>
        "The anchor at point " + anchorNumber.ToString(CultureInfo.InvariantCulture) + " is now Smooth: its handles are no longer the same length.";

    private static void CollectKnotEtas(Curve curve, List<double> etas)
    {
        var seen = new HashSet<long>();
        for (int index = curve.Degree + 1; index < curve.Knots.Length - curve.Degree - 1; index++)
        {
            double knot = curve.Knots[index];
            if (knot <= 0 || knot >= 1) continue;
            if (!seen.Add(BitConverter.DoubleToInt64Bits(knot))) continue;
            etas.Add(Math.Clamp(Abscissa(curve, knot), 0, 1));
        }
    }

    private static bool WithinIdentity(double delta, Curve curve)
    {
        double scale = 1;
        foreach (double[] point in curve.Points) scale = Math.Max(scale, Math.Abs(point[1]));
        return delta <= 1e-12 * scale;
    }

    private static bool KnotsEqual(double[] left, double[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
            if (BitConverter.DoubleToInt64Bits(left[index]) != BitConverter.DoubleToInt64Bits(right[index])) return false;
        return true;
    }

    private static bool Strict(double[][] points)
    {
        for (int index = 1; index < points.Length; index++)
            if (points[index][0] - points[index - 1][0] < 1e-7) return false;
        return true;
    }

    private static double Greville(double[] knots, int degree, int index)
    {
        double sum = 0;
        for (int offset = 1; offset <= degree; offset++) sum += knots[index + offset];
        return sum / degree;
    }

    private static double Abscissa(Curve curve, double t)
    {
        double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, Math.Clamp(t, 0, 1));
        double value = 0;
        for (int index = 0; index < curve.Points.Length; index++) value += basis[index] * curve.Points[index][0];
        return value;
    }

    private static double Ordinate(Curve curve, double eta) =>
        ChannelEvaluator.Value(curve.Knots, curve.Degree, curve.Points, eta);

    private static double Kappa(Curve curve, double t) => KappaPhysical(curve, t, 1);

    private static double KappaPhysical(Curve curve, double t, double halfSpanMeters)
    {
        var jet = SplineBasis.Evaluate(curve.Knots, curve.Degree, Math.Clamp(t, 0, 1));
        double xt = 0, yt = 0, xtt = 0, ytt = 0;
        double span = halfSpanMeters * 1000;
        for (int index = 0; index < curve.Points.Length; index++)
        {
            xt += jet.D1[index] * curve.Points[index][0] * span;
            yt += jet.D1[index] * curve.Points[index][1] * 1000;
            xtt += jet.D2[index] * curve.Points[index][0] * span;
            ytt += jet.D2[index] * curve.Points[index][1] * 1000;
        }
        double speed2 = xt * xt + yt * yt;
        if (speed2 <= 0) return 0;
        return (xt * ytt - yt * xtt) / Math.Pow(speed2, 1.5);
    }

    private static Curve Shape(Curve curve, double[] knots, double[][] points, string[] ids, TangentRow[] tangents) =>
        curve with { Knots = knots, Points = points, Ids = ids, Tangents = tangents };

    private static int Nearest(Curve curve, double eta)
    {
        int best = 0;
        double distance = double.MaxValue;
        for (int index = 0; index < curve.Points.Length; index++)
        {
            double gap = Math.Abs(curve.Points[index][0] - eta);
            if (gap < distance) { distance = gap; best = index; }
        }
        return best;
    }

    private static string TooClose(int index) =>
        "Too close to point " + (index + 1).ToString(CultureInfo.InvariantCulture) + " to add one here. Double-click further along the curve.";

    private static string AtCeiling(Curve curve) =>
        "The " + Label(curve.Path) + " has 16 points, the most a curve can have. Remove a point or rebuild with fewer.";

    private static string Fold(int index) =>
        "Removing point " + (index + 1).ToString(CultureInfo.InvariantCulture) + " would fold the curve. Move its neighbours apart first.";

    private static string Label(string curve) => curve switch
    {
        "leading" => "leading edge",
        "trailing" => "trailing edge",
        _ => curve
    };

    private static string FreshOne(string[] existing) => "cv-" + (MaxNumber(existing) + 1).ToString(CultureInfo.InvariantCulture);

    private static int MaxNumber(string[] ids)
    {
        int max = -1;
        foreach (string id in ids)
            if (id.StartsWith("cv-", StringComparison.Ordinal) && int.TryParse(id.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                max = Math.Max(max, number);
        return max;
    }

    private static double Distance(double[] left, double[] right)
    {
        double dx = left[0] - right[0], dy = left[1] - right[1];
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
