namespace CfdWorkbench.Core;

public sealed record DimensionReport(string Dimension, double TypedMeters, string Rule, double FitResidualMeters,
    double ToleranceMeters, double DeviationFromLinearMeters, double PlanformShiftMeters, bool FitAboveLimit);

public sealed record DimensionOutcome(string AcceptedId, DimensionReport Report);

public static class ChordDimension
{
    public const string RootFlat = "chord-blend-rootflat/1";
    public const string Linear = "chord-blend-linear/1";

    /// <summary>Model/join tolerance. A fit above this is accepted with a warning (Ruling 56, DR-12).</summary>
    internal const double FitToleranceMeters = 10e-6;

    public static (DimensionReport Report, byte[] Patched) Evaluate(byte[] source, DimensionCommand command)
    {
        var parsed = FoilSource.Parse(source);
        Guard.Require(parsed.IsParsed && parsed.Definition is { Kind: "foil" }, parsed.IsParsed ? "DSL-TARGET" : parsed.Diagnostics[0].Code);
        var definition = parsed.Definition!;
        Guard.Require(command.Name is "root-chord" or "tip-chord", "DSL-TARGET");
        if (command.Name == "tip-chord") Guard.Require(definition.Tip == "open", "DSL-TARGET");
        var leading = definition.Curves["leading"];
        var trailing = definition.Curves["trailing"];
        double root = Chord(leading, trailing, 0);
        double tip = Chord(leading, trailing, 1);
        double typed = LengthExpression.ParseMeters(command.Text, new Dictionary<string, double>
        {
            ["span"] = definition.HalfSpan * 2,
            ["root_chord"] = root,
            ["tip_chord"] = tip
        });
        Guard.Require(typed > 0 && double.IsFinite(typed), "DSL-UNIT");
        bool rootEdit = command.Name == "root-chord";
        double end = rootEdit ? root : tip;
        Guard.Require(end > 0 && double.IsFinite(end), "DSL-EDGES-CROSS");
        double factor = typed / end;
        bool leadingMirror = Mirrored(definition, "leading");
        bool trailingMirror = Mirrored(definition, "trailing");
        bool rootFlat = leadingMirror || trailingMirror;
        string rule = rootFlat ? RootFlat : Linear;
        if (!TargetsStayPositive(leading, trailing, factor, rootEdit, rootFlat))
            throw new ContractError("DSL-EDGES-CROSS");
        double[] lead = FitOrdinates(leading, eta => LeadingTarget(leading, trailing, eta, factor, rootEdit, rootFlat), leadingMirror);
        double[] trail = FitOrdinates(trailing, eta => TrailingTarget(leading, trailing, eta, factor, rootEdit, rootFlat), trailingMirror);
        double delta = rootEdit ? lead[0] : 0;
        Shift(lead, delta);
        Shift(trail, delta);
        if (rootEdit)
        {
            lead[0] = +0d;
            if (leadingMirror) lead[1] = lead[0];
            trail[0] = typed;
            if (trailingMirror) trail[1] = trail[0];
        }
        else trail[^1] = lead[^1] + typed;
        byte[] patched = WriteOrdinates(source, "leading", lead);
        patched = WriteOrdinates(patched, "trailing", trail);
        var again = FoilSource.Parse(patched);
        Guard.Require(again.IsParsed && again.Definition is not null, "DSL-PATCH");
        var fittedLead = again.Definition!.Curves["leading"];
        var fittedTrail = again.Definition.Curves["trailing"];
        Same(fittedLead, lead);
        Same(fittedTrail, trail);
        double residual = Departure(leading, trailing, fittedLead, fittedTrail, factor, rootEdit, rootFlat, delta, linear: false);
        double deviation = Departure(leading, trailing, fittedLead, fittedTrail, factor, rootEdit, false, delta, linear: true);
        bool above = residual > FitToleranceMeters;
        double shift = rootEdit ? -delta : 0;
        var assessment = Geometry.Assess(again);
        if (assessment.Status != GeometryStatus.Certified)
        {
            if (assessment.Reason is "Continuous chord is nonpositive." or "Rail hulls do not certify strictly positive chord.")
                throw new ContractError("DSL-EDGES-CROSS");
            throw new ContractError("DSL-NOT-ASSESSED");
        }
        return (new DimensionReport(command.Name, typed, rule, residual, FitToleranceMeters, deviation, shift, above), patched);
    }

    internal static double[] FitOrdinates(Curve curve, Func<double, double> target, bool mirror,
        IReadOnlyList<(double[] Coefficients, double Bound)>? extra = null)
    {
        var eta = Samples(curve);
        int count = curve.Points.Length;
        Guard.Require(count >= 2, "DSL-CURVE");
        int rows = eta.Count;
        var design = new double[rows, count];
        var values = new double[rows];
        var weight = new double[rows];
        for (int row = 0; row < rows; row++)
        {
            double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, ParameterAt(curve, eta[row]));
            for (int column = 0; column < count; column++) design[row, column] = basis[column];
            values[row] = target(eta[row]);
            weight[row] = 1;
        }
        int hard = 2 + (mirror ? 1 : 0) + (extra?.Count ?? 0);
        var coefficients = new double[hard, count];
        var bounds = new double[hard];
        coefficients[0, 0] = 1;
        bounds[0] = target(0);
        coefficients[1, count - 1] = 1;
        bounds[1] = target(1);
        int next = 2;
        if (mirror)
        {
            coefficients[next, 0] = 1;
            coefficients[next, 1] = -1;
            next++;
        }
        if (extra is not null)
        {
            foreach (var (row, bound) in extra)
            {
                Guard.Require(row.Length == count, "DSL-CURVE");
                for (int column = 0; column < count; column++) coefficients[next, column] = row[column];
                bounds[next] = bound;
                next++;
            }
        }
        double[] solved = ConstrainedFit.Solve(design, values, weight, new double[count, count], 0, coefficients, bounds);
        solved[0] = target(0) == 0 ? 0 : target(0);
        solved[count - 1] = target(1) == 0 ? 0 : target(1);
        if (mirror) solved[1] = solved[0];
        for (int index = 0; index < count; index++) if (solved[index] == 0) solved[index] = 0;
        return solved;
    }

    private static bool TargetsStayPositive(Curve leading, Curve trailing, double factor, bool rootEdit, bool rootFlat)
    {
        var seen = new HashSet<double>();
        foreach (double eta in Samples(leading)) seen.Add(eta);
        foreach (double eta in Samples(trailing)) seen.Add(eta);
        foreach (double eta in seen)
        {
            double chord = Chord(leading, trailing, eta);
            double scale = Blend(eta, factor, rootEdit, rootFlat);
            if (!double.IsFinite(chord) || !double.IsFinite(scale) || chord <= 0 || scale <= 0 || chord * scale <= 0)
                return false;
        }
        return true;
    }

    private static double Departure(Curve originalLead, Curve originalTrail, Curve fittedLead, Curve fittedTrail,
        double factor, bool rootEdit, bool rootFlat, double delta, bool linear)
    {
        bool flat = !linear && rootFlat;
        double max = 0;
        void Scan(Curve fitted, bool lead)
        {
            foreach (double eta in Samples(fitted))
            {
                double actual = Ordinate(fitted, eta);
                double goal = (lead
                    ? LeadingTarget(originalLead, originalTrail, eta, factor, rootEdit, flat)
                    : TrailingTarget(originalLead, originalTrail, eta, factor, rootEdit, flat)) - delta;
                max = Math.Max(max, Math.Abs(actual - goal));
            }
        }
        Scan(fittedLead, true);
        Scan(fittedTrail, false);
        return max;
    }

    private static double LeadingTarget(Curve leading, Curve trailing, double eta, double factor, bool rootEdit, bool rootFlat)
    {
        double le = Ordinate(leading, eta);
        double chord = Ordinate(trailing, eta) - le;
        double scale = Blend(eta, factor, rootEdit, rootFlat);
        return le + (1 - scale) * chord / 4;
    }

    private static double TrailingTarget(Curve leading, Curve trailing, double eta, double factor, bool rootEdit, bool rootFlat)
    {
        double le = Ordinate(leading, eta);
        double chord = Ordinate(trailing, eta) - le;
        double scale = Blend(eta, factor, rootEdit, rootFlat);
        return le + chord / 4 + 3 * scale * chord / 4;
    }

    private static double Blend(double eta, double factor, bool rootEdit, bool rootFlat)
    {
        if (rootFlat)
            return rootEdit ? factor + (1 - factor) * eta * eta : 1 + (factor - 1) * eta * eta;
        return rootEdit ? factor + (1 - factor) * eta : 1 + (factor - 1) * eta;
    }

    private static bool Mirrored(Definition definition, string channel) =>
        definition.Locks.Any(item => item.Kind == "root_mirror" && item.Channel.Text == channel);

    private static void Shift(double[] values, double delta)
    {
        if (delta == 0) return;
        for (int index = 0; index < values.Length; index++)
        {
            values[index] -= delta;
            if (values[index] == 0) values[index] = 0;
        }
    }

    private static byte[] WriteOrdinates(byte[] source, string rail, double[] ordinates)
    {
        var parsed = FoilSource.Parse(source);
        Guard.Require(parsed.IsParsed && parsed.Definition is not null, "DSL-PATCH");
        var definition = parsed.Definition!;
        var curve = definition.Curves[rail];
        Guard.Require(ordinates.Length == curve.Points.Length, "DSL-PATCH");
        string text = FoilSource.Utf8.GetString(parsed.Source);
        var edits = new List<(int Start, int End, string Value)>();
        for (int index = 0; index < ordinates.Length; index++)
        {
            double ordinate = ordinates[index] == 0 ? 0 : ordinates[index];
            if (Bits(curve.Points[index][1]) == Bits(ordinate)) continue;
            edits.Add((curve.Ordinates[index].Start, curve.Ordinates[index].End, FoilSource.ExactDecimal(ordinate, definition.UnitScale)));
        }
        foreach (var edit in edits.OrderByDescending(item => item.Start))
            text = string.Concat(text.AsSpan(0, edit.Start), edit.Value, text.AsSpan(edit.End));
        return FoilSource.Utf8.GetBytes(text);
    }

    private static void Same(Curve curve, double[] ordinates)
    {
        Guard.Require(curve.Points.Length == ordinates.Length, "DSL-PATCH");
        for (int index = 0; index < ordinates.Length; index++)
        {
            double ordinate = ordinates[index] == 0 ? 0 : ordinates[index];
            Guard.Require(Bits(curve.Points[index][1]) == Bits(ordinate), "DSL-PATCH");
        }
    }

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);

    private static List<double> Samples(Curve curve)
    {
        var samples = new List<double>(220);
        void Add(double eta)
        {
            if (!double.IsFinite(eta)) return;
            eta = Math.Clamp(eta, 0, 1);
            for (int index = 0; index < samples.Count; index++) if (samples[index] == eta) return;
            samples.Add(eta);
        }
        for (int index = 0; index <= 200; index++) Add(index / 200d);
        foreach (double knot in curve.Knots) Add(Abscissa(curve, Math.Clamp(knot, 0, 1)));
        return samples;
    }

    private static double Chord(Curve leading, Curve trailing, double eta) => Ordinate(trailing, eta) - Ordinate(leading, eta);
    private static double Ordinate(Curve curve, double eta) => Dot(curve, ParameterAt(curve, eta), 1);
    private static double Abscissa(Curve curve, double parameter) => Dot(curve, parameter, 0);

    private static double ParameterAt(Curve curve, double eta)
    {
        if (eta <= 0) return 0;
        if (eta >= 1) return 1;
        double low = 0, high = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (low + high) / 2;
            if (Abscissa(curve, mid) < eta) low = mid;
            else high = mid;
        }
        return (low + high) / 2;
    }

    private static double Dot(Curve curve, double parameter, int coordinate)
    {
        double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, parameter);
        double value = 0;
        for (int index = 0; index < basis.Length; index++) value += basis[index] * curve.Points[index][coordinate];
        return value;
    }
}
