namespace CfdWorkbench.Core;

// One paired section-point step (m12c §3.4–§3.5, OD-4). Both surfaces keep the same knots.
// Anchor → Control refits only the other surface, and only on the affected segment.
internal static class SectionEdits
{
    internal static (byte[] Bytes, SectionStepReport Report) Apply(byte[] bytes, int assignment, SectionStep step)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(step);
        var parsed = FoilSource.Parse(bytes);
        if (!parsed.IsParsed || parsed.Definition is null)
            throw new ContractError(parsed.Diagnostics.Count == 0 ? "DSL-INVALID" : parsed.Diagnostics[0].Code);
        if (parsed.Definition.Curves.Values.Any(curve => curve.MissingIds))
            bytes = FoilSource.MaterializeIds(parsed);
        return step switch
        {
            SectionStep.Move move => Move(bytes, assignment, move),
            SectionStep.SetType setType => setType.Anchor ? ToAnchor(bytes, assignment, setType) : ToControl(bytes, assignment, setType),
            SectionStep.InsertAnchor insert => InsertAnchor(bytes, assignment, insert),
            SectionStep.SetTangent tangent => SetTangent(bytes, assignment, tangent),
            SectionStep.Insert insert => Insert(bytes, assignment, insert),
            SectionStep.Delete delete => Delete(bytes, assignment, delete),
            SectionStep.Fair fair => Fair(bytes, assignment, fair),
            SectionStep.Rebuild rebuild => Rebuild(bytes, assignment, rebuild),
            _ => throw new ContractError("DSL-PATCH", "SectionEdits does not apply " + step.GetType().Name + ".")
        };
    }

    private static (byte[] Bytes, SectionStepReport Report) Move(byte[] bytes, int assignment, SectionStep.Move move)
    {
        var (profile, upper, lower) = Sides(bytes, assignment);
        var edited = move.Side == SurfaceSide.Upper ? upper : lower;
        int index = IndexOf(edited, move.VertexId);
        if (index == 0 || index == edited.Points.Length - 1)
            throw new ContractError("DSL-LOCK", "The nose and the trailing end are fixed.");
        if (index == 1 && move.X != edited.Points[index][0])
            throw new ContractError("DSL-LOCK", "A nose handle moves in y only.");
        var upperPoints = Copy(upper.Points);
        var lowerPoints = Copy(lower.Points);
        var editedPoints = move.Side == SurfaceSide.Upper ? upperPoints : lowerPoints;
        var otherPoints = move.Side == SurfaceSide.Upper ? lowerPoints : upperPoints;
        editedPoints[index][0] = move.X;
        otherPoints[index][0] = move.X;
        editedPoints[index][1] = move.Y;
        RequireOrdered(editedPoints);
        RequireOrdered(otherPoints);
        byte[] next = FoilSource.WriteSurfaces(bytes, profile.Name, upper.Knots, upperPoints, upper.Ids, lowerPoints, lower.Ids);
        return (next, ReportOf("move", bytes, next, assignment, Array.Empty<string>()));
    }

    private static (byte[] Bytes, SectionStepReport Report) ToAnchor(byte[] bytes, int assignment, SectionStep.SetType step)
    {
        var (profile, upper, lower) = Sides(bytes, assignment);
        var edited = step.Side == SurfaceSide.Upper ? upper : lower;
        int index = IndexOf(edited, step.VertexId);
        int degree = edited.Degree;
        if (index <= 0 || index >= edited.Points.Length - 1 || FoilSource.IsAnchor(edited.Knots, edited.Points.Length, degree, index))
            throw new ContractError("DSL-LOCK", "Only an interior control can become an anchor.");
        double targetX = edited.Points[index][0];
        double targetY = edited.Points[index][1];
        double knot = Snap(edited.Knots, ProfileEvaluator.ParameterFor(edited, targetX));
        if (knot <= 0 || knot >= 1) throw new ContractError("DSL-CURVE", "The anchor parameter is not interior.");
        int multiplicity = edited.Knots.Count(value => value == knot);
        int additions = degree - multiplicity;
        if (additions <= 0) throw new ContractError("DSL-LOCK", "The point is already an anchor.");
        if (edited.Points.Length + additions > 32)
            throw new ContractError("DSL-CURVE", "Making this an anchor needs " + additions + " more points and would pass 32.");
        var upperKnots = upper.Knots;
        var lowerKnots = lower.Knots;
        var upperPoints = Copy(upper.Points);
        var lowerPoints = Copy(lower.Points);
        var upperIds = upper.Ids.ToArray();
        var lowerIds = lower.Ids.ToArray();
        var taken = new HashSet<string>(upperIds.Concat(lowerIds), StringComparer.Ordinal);
        for (int turn = 0; turn < additions; turn++)
        {
            var upperInsert = FoilSource.InsertOnce(upperKnots, upperPoints, degree, knot);
            var lowerInsert = FoilSource.InsertOnce(lowerKnots, lowerPoints, degree, knot);
            if (upperInsert.Inserted != lowerInsert.Inserted)
                throw new ContractError("DSL-CURVE", "The paired insert did not land on the same index.");
            string id = Fresh(taken);
            upperKnots = upperInsert.Knots;
            lowerKnots = lowerInsert.Knots;
            upperPoints = upperInsert.Points;
            lowerPoints = lowerInsert.Points;
            upperIds = Splice(upperIds, upperInsert.Inserted, id);
            lowerIds = Splice(lowerIds, lowerInsert.Inserted, id);
        }
        if (!upperKnots.SequenceEqual(lowerKnots))
            throw new ContractError("DSL-CURVE", "The paired surfaces no longer share knots.");
        var editedPoints = step.Side == SurfaceSide.Upper ? upperPoints : lowerPoints;
        var editedIds = step.Side == SurfaceSide.Upper ? upperIds : lowerIds;
        int anchor = AnchorIndex(upperKnots, editedPoints.Length, degree, knot);
        int named = Array.IndexOf(editedIds, step.VertexId);
        if (named >= 0 && named != anchor) (editedIds[anchor], editedIds[named]) = (editedIds[named], editedIds[anchor]);
        double delta = targetY - editedPoints[anchor][1];
        for (int offset = -1; offset <= 1; offset++) editedPoints[anchor + offset][1] += delta;
        var rows = edited.Tangents.Where(row => row.Id != step.VertexId).ToList();
        rows.Add(new TangentRow(step.VertexId, "smooth", null));
        byte[] geometry = FoilSource.WriteSurfaces(bytes, profile.Name, upperKnots, upperPoints, upperIds, lowerPoints, lowerIds);
        byte[] next = FoilSource.WriteSideTangents(geometry, profile.Name, step.Side, rows.ToArray());
        return (next, ReportOf("set-type", bytes, next, assignment, Array.Empty<string>()));
    }

    private static (byte[] Bytes, SectionStepReport Report) InsertAnchor(byte[] bytes, int assignment, SectionStep.InsertAnchor step)
    {
        var (profile, upper, lower) = Sides(bytes, assignment);
        var edited = step.Side == SurfaceSide.Upper ? upper : lower;
        int degree = edited.Degree;
        double knot = Snap(edited.Knots, ProfileEvaluator.ParameterFor(edited, step.X));
        int additions = degree - edited.Knots.Count(value => value == knot);
        if (edited.Points.Length + additions > 32)
            throw new ContractError("DSL-CURVE", "Making this an anchor needs " + additions + " more points and would pass 32.");
        var state = Raise(upper, lower, knot, additions);
        byte[] next = FoilSource.WriteSurfaces(bytes, profile.Name, state.Knots, state.Upper, state.UpperIds, state.Lower, state.LowerIds);
        return (next, ReportOf("insert-anchor", bytes, next, assignment, Array.Empty<string>()));
    }

    private static (byte[] Bytes, SectionStepReport Report) ToControl(byte[] bytes, int assignment, SectionStep.SetType step)
    {
        var (profile, upper, lower) = Sides(bytes, assignment);
        var edited = step.Side == SurfaceSide.Upper ? upper : lower;
        var other = step.Side == SurfaceSide.Upper ? lower : upper;
        int index = IndexOf(edited, step.VertexId);
        if (!FoilSource.IsAnchor(edited.Knots, edited.Points.Length, edited.Degree, index))
            throw new ContractError("DSL-LOCK", "Only an interior anchor can become a control.");
        var kept = edited.Tangents.Where(row => row.Id != step.VertexId).ToArray();
        byte[] stripped = FoilSource.WriteSideTangents(bytes, profile.Name, step.Side, kept);
        (profile, upper, lower) = Sides(stripped, assignment);
        edited = step.Side == SurfaceSide.Upper ? upper : lower;
        other = step.Side == SurfaceSide.Upper ? lower : upper;
        index = IndexOf(edited, step.VertexId);
        double knot = edited.Knots[index + 1];
        double leftX = SegmentBound(edited, index, knot, left: true);
        double rightX = SegmentBound(edited, index, knot, left: false);
        var upperKnots = upper.Knots;
        var lowerKnots = lower.Knots;
        var upperPoints = Copy(upper.Points);
        var lowerPoints = Copy(lower.Points);
        var upperIds = upper.Ids.ToArray();
        var lowerIds = lower.Ids.ToArray();
        for (int pass = 0; pass < 2; pass++)
        {
            int r = FoilSource.SelectRemovable(upperKnots, edited.Degree, knot);
            var upperDrop = FoilSource.RemoveOnce(upperKnots, upperPoints, edited.Degree, r);
            var lowerDrop = FoilSource.RemoveOnce(lowerKnots, lowerPoints, edited.Degree, r);
            upperKnots = upperDrop.Knots;
            lowerKnots = lowerDrop.Knots;
            upperPoints = upperDrop.Points;
            lowerPoints = lowerDrop.Points;
            upperIds = Drop(upperIds, upperDrop.Removed);
            lowerIds = Drop(lowerIds, lowerDrop.Removed);
        }
        var otherPoints = step.Side == SurfaceSide.Upper ? lowerPoints : upperPoints;
        double chord = LargestChord(stripped, assignment);
        var removedCurve = other with { Knots = upperKnots, Points = otherPoints };
        var (deviation, _) = SegmentDeviation(other, removedCurve, leftX, rightX);
        if (deviation * chord > 10e-6)
        {
            var fitted = Refit(other, upperKnots, otherPoints, leftX, rightX);
            var fittedCurve = other with { Knots = upperKnots, Points = fitted };
            var (fittedDeviation, maximumX) = SegmentDeviation(other, fittedCurve, leftX, rightX);
            if (fittedDeviation * chord > 10e-6)
            {
                double microns = fittedDeviation * chord * 1e6;
                var error = new ContractError("DSL-CURVE", "refit " + microns.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                    + " µm exceeds 10 µm at the largest chord " + chord.ToString("G17", System.Globalization.CultureInfo.InvariantCulture) + " m");
                error.Data["RefitMaximumChordX"] = maximumX;
                throw error;
            }
            if (step.Side == SurfaceSide.Upper) lowerPoints = fitted;
            else upperPoints = fitted;
        }
        byte[] next = FoilSource.WriteSurfaces(stripped, profile.Name, upperKnots, upperPoints, upperIds, lowerPoints, lowerIds);
        return (next, ReportOf("set-type", bytes, next, assignment, new[] { step.VertexId }));
    }

    private static (byte[] Bytes, SectionStepReport Report) SetTangent(byte[] bytes, int assignment, SectionStep.SetTangent step)
    {
        var (profile, upper, lower) = Sides(bytes, assignment);
        var edited = step.Side == SurfaceSide.Upper ? upper : lower;
        int index = IndexOf(edited, step.VertexId);
        if (!FoilSource.IsAnchor(edited.Knots, edited.Points.Length, edited.Degree, index))
            throw new ContractError("DSL-LOCK", "A tangent row names an interior anchor.");
        if (step.Kind == TangentKind.Vertical)
            throw new ContractError("DSL-LOCK", Sections.VerticalInteriorReason);
        var upperPoints = Copy(upper.Points);
        var lowerPoints = Copy(lower.Points);
        var points = step.Side == SurfaceSide.Upper ? upperPoints : lowerPoints;
        PlaceHandles(points, edited.Ids, index, step);
        var rows = edited.Tangents.Where(row => row.Id != step.VertexId).ToList();
        if (step.Kind != TangentKind.Corner)
            rows.Add(new TangentRow(step.VertexId, KindName(step.Kind), step.Kind == TangentKind.Angle ? step.AngleDegrees : null));
        byte[] geometry = FoilSource.WriteSurfaces(bytes, profile.Name, upper.Knots, upperPoints, upper.Ids, lowerPoints, lower.Ids);
        byte[] next = FoilSource.WriteSideTangents(geometry, profile.Name, step.Side, rows.ToArray());
        return (next, ReportOf("set-tangent", bytes, next, assignment, step.Kind == TangentKind.Corner ? new[] { step.VertexId } : Array.Empty<string>()));
    }

    private static (byte[] Bytes, SectionStepReport Report) Insert(byte[] bytes, int assignment, SectionStep.Insert step)
    {
        var (profile, _, _) = Sides(bytes, assignment);
        var (next, _) = FoilSource.InsertProfileKnot(bytes, profile.Name, step.X);
        return (next, ReportOf("insert", bytes, next, assignment, Array.Empty<string>()));
    }

    private static (byte[] Bytes, SectionStepReport Report) Delete(byte[] bytes, int assignment, SectionStep.Delete step)
    {
        var (profile, upper, lower) = Sides(bytes, assignment);
        var edited = step.Side == SurfaceSide.Upper ? upper : lower;
        if (edited.Points.Length <= 7)
            throw new ContractError("DSL-CURVE", "Delete would leave fewer than p + 2 = 7 vertices");
        int index = IndexOf(edited, step.VertexId);
        bytes = FoilSource.WriteSideTangents(bytes, profile.Name, SurfaceSide.Upper, upper.Tangents.Where(row => row.Id != step.VertexId).ToArray());
        bytes = FoilSource.WriteSideTangents(bytes, profile.Name, SurfaceSide.Lower, lower.Tangents.Where(row => row.Id != step.VertexId).ToArray());
        var (next, _) = FoilSource.DeleteProfileVertex(bytes, profile.Name, index);
        return (next, ReportOf("delete", bytes, next, assignment, new[] { step.VertexId }));
    }

    private static (byte[] Bytes, SectionStepReport Report) Fair(byte[] bytes, int assignment, SectionStep.Fair step)
    {
        var (profile, _, _) = Sides(bytes, assignment);
        var result = ProfileFair.Fair(profile, step.Tolerance, step.Ends, step.Side);
        if (!result.WithinTolerance) throw new ContractError("DSL-GEOMETRY", "Fair result exceeds tolerance");
        if (result.MonotonePiecesAfter > result.MonotonePiecesBefore) throw new ContractError("DSL-GEOMETRY", "Fair increased monotone pieces");
        byte[] next = FoilSource.WriteSurfaces(bytes, profile.Name, result.Upper.Knots, Copy(result.Upper.Points), result.Upper.Ids,
            Copy(result.Lower.Points), result.Lower.Ids);
        return (next, ReportOf("fair", bytes, next, assignment, Array.Empty<string>()));
    }

    private static (byte[] Bytes, SectionStepReport Report) Rebuild(byte[] bytes, int assignment, SectionStep.Rebuild step)
    {
        var (profile, _, _) = Sides(bytes, assignment);
        var result = ProfileFair.Rebuild(profile, step.VertexCount, step.Tolerance, step.Ends);
        if (!result.WithinTolerance) throw new ContractError("DSL-GEOMETRY", "Rebuild result exceeds tolerance");
        byte[] next = FoilSource.WriteSurfaces(bytes, profile.Name, result.Upper.Knots, Copy(result.Upper.Points), result.Upper.Ids,
            Copy(result.Lower.Points), result.Lower.Ids);
        next = FoilSource.WriteSideTangents(next, profile.Name, SurfaceSide.Upper, Array.Empty<TangentRow>());
        next = FoilSource.WriteSideTangents(next, profile.Name, SurfaceSide.Lower, Array.Empty<TangentRow>());
        return (next, ReportOf("rebuild", bytes, next, assignment, Array.Empty<string>()));
    }

    private static void PlaceHandles(double[][] points, string[] ids, int index, SectionStep.SetTangent step)
    {
        double ax = points[index][0], ay = points[index][1];
        if (step.Kind == TangentKind.Horizontal)
        {
            points[index - 1][1] = ay;
            points[index + 1][1] = ay;
            return;
        }
        if (step.Kind == TangentKind.Symmetric)
        {
            double dx = points[index + 1][0] - points[index - 1][0];
            double dy = points[index + 1][1] - points[index - 1][1];
            double norm = Math.Sqrt(dx * dx + dy * dy);
            if (norm == 0) { dx = 1; norm = 1; }
            dx /= norm;
            dy /= norm;
            double left = Math.Sqrt(Math.Pow(ax - points[index - 1][0], 2) + Math.Pow(ay - points[index - 1][1], 2));
            double right = Math.Sqrt(Math.Pow(points[index + 1][0] - ax, 2) + Math.Pow(points[index + 1][1] - ay, 2));
            double length = Math.Min(left, right);
            points[index - 1][0] = ax - dx * length;
            points[index - 1][1] = ay - dy * length;
            points[index + 1][0] = ax + dx * length;
            points[index + 1][1] = ay + dy * length;
            return;
        }
        if (step.Kind == TangentKind.Angle)
        {
            double radians = (step.AngleDegrees ?? 0) * PlacementRule.RadiansPerDegree;
            double cos = Math.Cos(radians), sin = Math.Sin(radians);
            bool keepLeft = step.KeepHandleId == ids[index - 1];
            bool keepRight = step.KeepHandleId == ids[index + 1];
            if (!keepRight)
            {
                double distance = Math.Sqrt(Math.Pow(points[index + 1][0] - ax, 2) + Math.Pow(points[index + 1][1] - ay, 2));
                points[index + 1][0] = ax + cos * distance;
                points[index + 1][1] = ay + sin * distance;
            }
            if (!keepLeft)
            {
                double distance = Math.Sqrt(Math.Pow(points[index - 1][0] - ax, 2) + Math.Pow(points[index - 1][1] - ay, 2));
                points[index - 1][0] = ax - cos * distance;
                points[index - 1][1] = ay - sin * distance;
            }
        }
    }

    private static (double[] Knots, double[][] Upper, string[] UpperIds, double[][] Lower, string[] LowerIds) Raise(Curve upper, Curve lower, double knot, int additions)
    {
        var upperKnots = upper.Knots;
        var lowerKnots = lower.Knots;
        var upperPoints = Copy(upper.Points);
        var lowerPoints = Copy(lower.Points);
        var upperIds = upper.Ids.ToArray();
        var lowerIds = lower.Ids.ToArray();
        var taken = new HashSet<string>(upperIds.Concat(lowerIds), StringComparer.Ordinal);
        for (int turn = 0; turn < additions; turn++)
        {
            var upperInsert = FoilSource.InsertOnce(upperKnots, upperPoints, upper.Degree, knot);
            var lowerInsert = FoilSource.InsertOnce(lowerKnots, lowerPoints, lower.Degree, knot);
            string id = Fresh(taken);
            upperKnots = upperInsert.Knots;
            lowerKnots = lowerInsert.Knots;
            upperPoints = upperInsert.Points;
            lowerPoints = lowerInsert.Points;
            upperIds = Splice(upperIds, upperInsert.Inserted, id);
            lowerIds = Splice(lowerIds, lowerInsert.Inserted, id);
        }
        return (upperKnots, upperPoints, upperIds, lowerPoints, lowerIds);
    }

    private static double[][] Refit(Curve original, double[] knots, double[][] removed, double xLeft, double xRight)
    {
        int count = removed.Length;
        int degree = original.Degree;
        var pinned = new List<int>();
        for (int index = 0; index < count; index++)
            if (index == 0 || index == count - 1 || removed[index][0] <= xLeft + 1e-12 || removed[index][0] >= xRight - 1e-12)
                pinned.Add(index);
        if (pinned.Count >= count) return removed;
        const int samples = 48;
        var basis = new double[samples, count];
        var targets = new double[samples];
        var weights = new double[samples];
        var probe = original with { Knots = knots, Points = removed };
        for (int sample = 0; sample < samples; sample++)
        {
            double x = xLeft + (xRight - xLeft) * ((sample + 0.5) / samples);
            x = Math.Clamp(x, 1e-6, 1 - 1e-6);
            double[] values = SplineBasis.Values(knots, degree, ProfileEvaluator.ParameterFor(probe, x));
            for (int column = 0; column < count; column++) basis[sample, column] = values[column];
            targets[sample] = ProfileEvaluator.OrdinateAt(original, x);
            weights[sample] = 1;
        }
        var constraints = new double[pinned.Count, count];
        var bound = new double[pinned.Count];
        for (int row = 0; row < pinned.Count; row++)
        {
            constraints[row, pinned[row]] = 1;
            bound[row] = removed[pinned[row]][1];
        }
        double[] ordinates;
        try { ordinates = ConstrainedFit.Solve(basis, targets, weights, new double[count, count], 0, constraints, bound); }
        catch (ContractError) { return removed; }
        var next = new double[count][];
        for (int index = 0; index < count; index++) next[index] = new[] { removed[index][0], ordinates[index] };
        return next;
    }

    private static (double Deviation, double ChordX) SegmentDeviation(Curve before, Curve after, double x0, double x1)
    {
        double worst = 0;
        double maximumX = x0;
        const int samples = 81;
        for (int sample = 0; sample < samples; sample++)
        {
            double x = x0 + (x1 - x0) * sample / (samples - 1.0);
            x = Math.Clamp(x, 0, 1);
            double deviation = Math.Abs(ProfileEvaluator.OrdinateAt(before, x) - ProfileEvaluator.OrdinateAt(after, x));
            if (deviation > worst) (worst, maximumX) = (deviation, x);
        }
        return (worst, maximumX);
    }

    private static double SegmentBound(Curve curve, int anchor, double knot, bool left)
    {
        double bound = left ? 0 : 1;
        int count = curve.Points.Length;
        for (int index = curve.Degree; index <= count - curve.Degree - 1; index++)
        {
            if (index == anchor || !FoilSource.IsAnchor(curve.Knots, count, curve.Degree, index)) continue;
            double other = curve.Knots[index + 1];
            if (left && other < knot && curve.Points[index][0] > bound) bound = curve.Points[index][0];
            if (!left && other > knot && curve.Points[index][0] < bound) bound = curve.Points[index][0];
        }
        return bound;
    }

    private static double LargestChord(byte[] bytes, int assignment)
    {
        var definition = FoilSource.Parse(bytes).Definition!;
        int profile = definition.Assignments[assignment].Profile;
        double largest = 0;
        foreach (var station in definition.Assignments)
        {
            if (station.Profile != profile) continue;
            double chord = Placement.Frame(bytes, station.Eta).ChordMeters;
            if (chord > largest) largest = chord;
        }
        return largest > 0 ? largest : double.PositiveInfinity;
    }

    private static double Snap(double[] knots, double t)
    {
        foreach (double existing in knots)
            if (existing > 0 && existing < 1 && Math.Abs(existing - t) <= 1e-12 * Math.Max(1, Math.Abs(t)))
                return existing;
        return t;
    }

    private static int AnchorIndex(double[] knots, int count, int degree, double knot)
    {
        for (int index = degree; index <= count - degree - 1; index++)
            if (FoilSource.IsAnchor(knots, count, degree, index) && knots[index + 1] == knot)
                return index;
        throw new ContractError("DSL-CURVE", "The inserted knot is not an anchor.");
    }

    private static void RequireOrdered(double[][] points)
    {
        for (int index = 1; index < points.Length; index++)
            if (points[index][0] < points[index - 1][0])
                throw new ContractError("DSL-CURVE", "Abscissae must stay ordered.");
    }

    private static (ProfileDefinition Profile, Curve Upper, Curve Lower) Sides(byte[] bytes, int assignment)
    {
        var definition = FoilSource.Parse(bytes).Definition ?? throw new ContractError("DSL-INVALID");
        if (assignment < 0 || assignment >= definition.Assignments.Length) throw new ContractError("DSL-PROFILE-TARGET");
        var profile = definition.Profiles[definition.Assignments[assignment].Profile];
        return (profile, profile.Upper, profile.Lower);
    }

    private static int IndexOf(Curve curve, string id)
    {
        int index = Array.IndexOf(curve.Ids, id);
        if (index < 0) throw new ContractError("DSL-ID", "No section point has that id.");
        return index;
    }

    private static SectionStepReport ReportOf(string kind, byte[] before, byte[] after, int assignment, IReadOnlyList<string> removed)
    {
        var (prior, _, _) = Sides(before, assignment);
        var (next, upper, lower) = Sides(after, assignment);
        return new(kind, FoilSource.MaxOrdinateDeviation(prior, next), "FoilSource.MaxOrdinateDeviation",
            upper.Points.Length, lower.Points.Length, null, null, removed);
    }

    private static string KindName(TangentKind kind) => kind switch
    {
        TangentKind.Smooth => "smooth",
        TangentKind.Symmetric => "symmetric",
        TangentKind.Horizontal => "horizontal",
        TangentKind.Vertical => "vertical",
        TangentKind.Angle => "angle",
        _ => "corner"
    };

    private static string Fresh(HashSet<string> taken)
    {
        int suffix = 0;
        foreach (string id in taken)
            if (id.StartsWith("cv-", StringComparison.Ordinal) && int.TryParse(id.AsSpan(3), out int value))
                suffix = Math.Max(suffix, value + 1);
        while (!taken.Add("cv-" + suffix)) suffix++;
        return "cv-" + suffix;
    }

    private static string[] Splice(string[] ids, int index, string id)
    {
        var next = new string[ids.Length + 1];
        Array.Copy(ids, next, index);
        next[index] = id;
        Array.Copy(ids, index, next, index + 1, ids.Length - index);
        return next;
    }

    private static string[] Drop(string[] ids, int index)
    {
        var next = new string[ids.Length - 1];
        Array.Copy(ids, next, index);
        Array.Copy(ids, index + 1, next, index, ids.Length - index - 1);
        return next;
    }

    private static double[][] Copy(double[][] points) => points.Select(point => new[] { point[0], point[1] }).ToArray();
}
