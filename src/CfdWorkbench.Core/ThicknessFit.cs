namespace CfdWorkbench.Core;

/// <summary>
/// Refits thickness-channel ordinates so scoped stations match the edited profile's max(upper − lower).
/// Knots and CV abscissae stay fixed. Inactive CVs are pinned so the change stays in the active B-spline support.
/// </summary>
internal static class ThicknessFit
{
    private const double Lambda = 1e-6;
    private const int Samples = 101;
    private const double ResidualLimit = 1e-9;
    private const double ChangeFloor = 1e-12;
    private const double BasisDust = 1e-15;

    internal readonly record struct View(ThicknessProposal Proposal, string? Fault);

    internal static byte[] Fit(byte[] source, string profileName)
    {
        if (!TryLoad(source, profileName, out var loaded, out string? fault) || fault is not null) return source;
        int count = loaded.Ordinates.Length;
        double[] etas = loaded.Etas;
        double target = loaded.Target;
        var rows = new List<double[]>();
        var bound = new List<double>();
        var free = new bool[count];
        for (int index = 0; index < etas.Length; index++)
        {
            double[] row = Row(loaded, etas[index]);
            rows.Add(row);
            bound.Add(target);
            Mark(free, row);
        }
        if (loaded.Mirror && (free[0] || free[1])) { free[0] = true; free[1] = true; }
        foreach (var constraint in loaded.Constraints)
        {
            if (constraint.Kind == "value" && constraint.Eta is double eta)
            {
                if (etas.Any(item => Math.Abs(item - eta) <= ResidualLimit)) continue;
                double[] row = Row(loaded, eta);
                rows.Add(row);
                bound.Add(constraint.Values[0]);
                Mark(free, row);
            }
            else if (constraint.Kind == "freeze" && constraint.VertexId is string id)
            {
                int index = Array.IndexOf(loaded.Ids, id);
                if ((uint)index >= (uint)count) return source;
                var row = Unit(count, index);
                rows.Add(row);
                bound.Add(constraint.Values[1]);
                free[index] = true;
            }
        }
        if (loaded.Mirror && free[0] && free[1])
        {
            var row = new double[count];
            row[0] = 1;
            row[1] = -1;
            rows.Add(row);
            bound.Add(0);
        }
        var pinned = new bool[count];
        for (int index = 0; index < count; index++)
        {
            if (free[index]) continue;
            rows.Add(Unit(count, index));
            bound.Add(loaded.Ordinates[index]);
            pinned[index] = true;
        }
        var basis = new double[Samples, count];
        var samples = new double[Samples];
        var weights = new double[Samples];
        for (int sample = 0; sample < Samples; sample++)
        {
            double[] row = Row(loaded, sample / (double)(Samples - 1));
            double value = 0;
            for (int column = 0; column < count; column++)
            {
                basis[sample, column] = row[column];
                value += row[column] * loaded.Ordinates[column];
            }
            samples[sample] = value;
            weights[sample] = 1;
        }
        // F is zero: a ridge toward the origin would move the whole channel by more than the affected-span floor.
        var smoothing = new double[count, count];
        double[] solved;
        try { solved = ConstrainedFit.Solve(basis, samples, weights, smoothing, Lambda, Matrix(rows), bound.ToArray()); }
        catch (ContractError error) when (error.Code == "GEOMETRY-FIT-SINGULAR") { return source; }
        catch (ArgumentException) { return source; }
        if (solved.Any(value => !double.IsFinite(value))) return source;
        for (int index = 0; index < etas.Length; index++) Project(solved, rows[index], target);
        Snap(solved, rows, bound);
        if (!EnforceMirror(solved, loaded.Mirror, pinned)) return source;
        for (int index = 0; index < etas.Length; index++) Project(solved, rows[index], target);
        if (!EnforceMirror(solved, loaded.Mirror, pinned)) return source;
        if (Residuals(loaded, solved, etas, target).Any(item => Math.Abs(item) > ResidualLimit)) return source;
        try { return FoilSource.PatchChannelOrdinates(source, solved); }
        catch (ContractError error) when (error.Code == "DSL-PATCH") { return source; }
    }

    internal static View Describe(byte[] source, string profileName, byte[] baseline)
    {
        if (!TryLoad(source, profileName, out var loaded, out string? fault))
            return new(Empty(), fault ?? "DSL-PROFILE-TARGET");
        double[] current = loaded.Ordinates;
        double[] residuals = Residuals(loaded, current, loaded.Etas, loaded.Target);
        fault ??= LockFault(loaded);
        if (fault is null && residuals.Any(item => Math.Abs(item) > ResidualLimit)) fault = "GEOMETRY-FIT-SINGULAR";
        double[] origin = Ordinates(baseline);
        var (start, end) = Affected(loaded, origin, current);
        double[] targets = new double[loaded.Etas.Length];
        Array.Fill(targets, loaded.Target);
        var proposal = new ThicknessProposal(Copy(loaded.Etas), targets, residuals, start, end);
        return new(proposal, fault);
    }

    private sealed class Loaded
    {
        internal required double Target { get; init; }
        internal required double[] Etas { get; init; }
        internal required double[] Knots { get; init; }
        internal required int Degree { get; init; }
        internal required double[] Abscissae { get; init; }
        internal required double[] Ordinates { get; init; }
        internal required string[] Ids { get; init; }
        internal required bool Mirror { get; init; }
        internal required IReadOnlyList<AuthoredConstraint> Constraints { get; init; }
    }

    private static bool TryLoad(byte[] source, string profileName, out Loaded loaded, out string? fault)
    {
        loaded = null!;
        fault = null;
        var parsed = FoilSource.Parse(source);
        var definition = parsed.Definition;
        if (definition is null || !definition.Curves.TryGetValue("thickness", out var channel))
        {
            fault = "DSL-PROFILE-TARGET";
            return false;
        }
        var profile = definition.Profiles.FirstOrDefault(item => item.Name == profileName);
        if (profile is null) { fault = "DSL-PROFILE-TARGET"; return false; }
        double[] etas = definition.Assignments.Where(item => definition.Profiles[item.Profile].Name == profileName).Select(item => item.Eta).ToArray();
        if (etas.Length == 0) { fault = "DSL-PROFILE-TARGET"; return false; }
        double target;
        try { target = Maximum(profile); }
        catch (ProofRefusal) { fault = "DSL-GEOMETRY"; return false; }
        if (!double.IsFinite(target) || target <= 0) { fault = "DSL-GEOMETRY"; return false; }
        var constraints = parsed.Authored().Constraints.Where(item => item.Channel == "thickness").ToArray();
        loaded = new Loaded
        {
            Target = target,
            Etas = etas,
            Knots = channel.Knots,
            Degree = channel.Degree,
            Abscissae = channel.Points.Select(point => point[0]).ToArray(),
            Ordinates = channel.Points.Select(point => point[1]).ToArray(),
            Ids = channel.Ids,
            Mirror = constraints.Any(item => item.Kind == "root_mirror"),
            Constraints = constraints
        };
        fault = LockFault(loaded);
        return true;
    }

    // assume: the two sides share knots and x(t), so the maximum over the spline parameter is the maximum over chord x.
    // A mismatched abscissa mapping is already refused by geometry certification.
    private static double Maximum(ProfileDefinition profile)
    {
        var watch = new ProofBudget();
        var upper = Bernstein.Spans(profile.Upper, watch);
        var lower = Bernstein.Spans(profile.Lower, watch);
        if (upper.Length != lower.Length) throw new ProofRefusal("Profile sides do not share a thickness parameter.");
        var difference = new Rational[upper.Length][];
        for (int span = 0; span < upper.Length; span++)
        {
            if (upper[span].Y.Length != lower[span].Y.Length) throw new ProofRefusal("Profile sides do not share a thickness degree.");
            difference[span] = new Rational[upper[span].Y.Length];
            for (int index = 0; index < difference[span].Length; index++)
                difference[span][index] = upper[span].Y[index] - lower[span].Y[index];
        }
        var maximum = Bernstein.Maximum(difference, watch);
        return (maximum.Lower.Down() + maximum.Upper.Up()) / 2;
    }

    private static string? LockFault(Loaded loaded)
    {
        foreach (var constraint in loaded.Constraints)
        {
            if (constraint.Kind == "value" && constraint.Eta is double eta)
            {
                bool station = loaded.Etas.Any(item => Math.Abs(item - eta) <= ResidualLimit);
                if (station && Math.Abs(constraint.Values[0] - loaded.Target) > ResidualLimit) return "DSL-LOCK";
            }
            else if (constraint.Kind == "freeze" && constraint.VertexId is string id)
            {
                int index = Array.IndexOf(loaded.Ids, id);
                if ((uint)index >= (uint)loaded.Ordinates.Length) return "DSL-LOCK";
                foreach (double station in loaded.Etas)
                {
                    double[] row = Row(loaded, station);
                    if (row[index] == 1 && row.Count(value => value != 0) == 1 && Math.Abs(constraint.Values[1] - loaded.Target) > ResidualLimit)
                        return "DSL-LOCK";
                }
            }
            else if (constraint.Kind == "bounds" && constraint.VertexId is string bounded)
            {
                int index = Array.IndexOf(loaded.Ids, bounded);
                if ((uint)index >= (uint)loaded.Ordinates.Length) return "DSL-LOCK";
                double low = constraint.Values[0], high = constraint.Values[1];
                foreach (double station in loaded.Etas)
                {
                    double[] row = Row(loaded, station);
                    if (row[index] == 1 && row.Count(value => value != 0) == 1 && (loaded.Target < low - ResidualLimit || loaded.Target > high + ResidualLimit))
                        return "DSL-LOCK";
                }
            }
        }
        return null;
    }

    private static double[] Residuals(Loaded loaded, double[] ordinates, double[] etas, double target)
    {
        var residuals = new double[etas.Length];
        for (int index = 0; index < etas.Length; index++)
            residuals[index] = Evaluate(loaded, ordinates, etas[index]) - target;
        return residuals;
    }

    private static (double Start, double End) Affected(Loaded loaded, double[] origin, double[] updated)
    {
        double start = double.NaN, end = double.NaN;
        const int grid = 1001;
        for (int index = 0; index < grid; index++)
        {
            double eta = index / (double)(grid - 1);
            double delta = Math.Abs(Evaluate(loaded, updated, eta) - Evaluate(loaded, origin, eta));
            if (delta <= ChangeFloor) continue;
            if (double.IsNaN(start)) start = eta;
            end = eta;
        }
        if (double.IsNaN(start)) return (loaded.Etas.Min(), loaded.Etas.Max());
        return (start, end);
    }

    private static double[] Ordinates(byte[] source)
    {
        var channel = FoilSource.Parse(source).Definition!.Curves["thickness"];
        return channel.Points.Select(point => point[1]).ToArray();
    }

    private static double Evaluate(Loaded loaded, double[] ordinates, double eta)
    {
        double[] row = Row(loaded, eta);
        double sum = 0;
        for (int index = 0; index < ordinates.Length; index++) sum += row[index] * ordinates[index];
        return sum;
    }

    private static double[] Row(Loaded loaded, double eta)
    {
        double[] row = SplineBasis.Values(loaded.Knots, loaded.Degree, Parameter(loaded, eta));
        for (int index = 0; index < row.Length; index++)
            if (Math.Abs(row[index]) <= BasisDust) row[index] = 0;
        return row;
    }

    private static double Parameter(Loaded loaded, double eta)
    {
        if (eta <= 0) return 0;
        if (eta >= 1) return 1;
        double low = 0, high = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (low + high) / 2;
            if (Abscissa(loaded, mid) < eta) low = mid;
            else high = mid;
        }
        return (low + high) / 2;
    }

    private static double Abscissa(Loaded loaded, double parameter)
    {
        double[] row = SplineBasis.Values(loaded.Knots, loaded.Degree, parameter);
        double sum = 0;
        for (int index = 0; index < loaded.Abscissae.Length; index++) sum += row[index] * loaded.Abscissae[index];
        return sum;
    }

    private static void Mark(bool[] free, double[] row)
    {
        for (int index = 0; index < free.Length; index++)
            if (row[index] != 0) free[index] = true;
    }

    private static double[] Unit(int count, int index)
    {
        var row = new double[count];
        row[index] = 1;
        return row;
    }

    private static void Project(double[] ordinates, double[] row, double target)
    {
        double dot = 0, norm = 0;
        for (int index = 0; index < ordinates.Length; index++)
        {
            dot += row[index] * ordinates[index];
            norm += row[index] * row[index];
        }
        if (norm == 0) return;
        double scale = (target - dot) / norm;
        for (int index = 0; index < ordinates.Length; index++)
            if (row[index] != 0) ordinates[index] += scale * row[index];
    }

    private static void Snap(double[] ordinates, List<double[]> rows, List<double> bound)
    {
        for (int row = 0; row < rows.Count; row++)
        {
            int hit = Hit(rows[row]);
            if (hit >= 0) ordinates[hit] = bound[row];
        }
    }

    private static int Hit(double[] row)
    {
        int hit = -1;
        for (int index = 0; index < row.Length; index++)
        {
            if (row[index] == 0) continue;
            if (row[index] != 1 || hit >= 0) return -1;
            hit = index;
        }
        return hit;
    }

    private static bool EnforceMirror(double[] ordinates, bool mirror, bool[] pinned)
    {
        if (!mirror || ordinates[0] == ordinates[1]) return true;
        if (pinned[0] && pinned[1]) return Math.Abs(ordinates[0] - ordinates[1]) <= ResidualLimit;
        if (pinned[1]) ordinates[0] = ordinates[1];
        else ordinates[1] = ordinates[0];
        return true;
    }

    private static double[,] Matrix(List<double[]> rows)
    {
        var matrix = new double[rows.Count, rows[0].Length];
        for (int row = 0; row < rows.Count; row++)
            for (int column = 0; column < rows[0].Length; column++)
                matrix[row, column] = rows[row][column];
        return matrix;
    }

    private static double[] Copy(double[] values)
    {
        var copy = new double[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    private static ThicknessProposal Empty() =>
        new(Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>(), 0, 0);
}
