using System.Globalization;

namespace CfdWorkbench.Core;

/// <summary>
/// The Replace step (docs/design/m12d-catalog.md §3.6, DR-M12D-1 A + B, Ruling 72): the section block is rewritten in place
/// and keeps its name. The replaced set is every station the block serves (or, for the blend chain, every station of the
/// foil, re-pointed to the one block). Beside a station outside the set the shape is fitted on the current spacing only;
/// with no such neighbour the current spacing is tried first, then the own sqrt spacings of 8 to 16 points in order.
/// Acceptance is 10 µm at the largest local chord of the stations replaced. Pure over bytes.
/// </summary>
public static class SectionReplace
{
    private const double AcceptanceMeters = 10e-6; // A4.6
    private const int SourceSamples = 201;         // rule 5
    private const int SmallestOwnSpacing = 8, LargestOwnSpacing = 16;
    private const int DenseSegments = 2000;

    public static ReplacePreview Preview(byte[] draftBytes, int assignment, SectionScope scope, ReplaceSource source, ReplaceScope replaceScope) =>
        Run(draftBytes, assignment, scope, source, replaceScope);

    // The step: the preview's bytes and report, or the preview's refusal as a ContractError. Nothing changes on a refusal.
    internal static (byte[] Bytes, ImportReport Report) Patch(byte[] bytes, int assignment, SectionScope scope, SectionStep.Replace replace)
    {
        var preview = Run(bytes, assignment, scope, replace.Source, replace.Scope);
        if (preview.RefusalCode is string code) throw new ContractError(code, preview.RefusalReason ?? code);
        return (preview.Bytes!, preview.Report!);
    }

    // Import .dat… (DR-M12D-3 a): the file's coordinates at the draft's scope.
    internal static SectionStep.Replace FromDat(byte[] dat) =>
        new(new ReplaceSource.Coordinates("a .dat file", new Provenance("dat:sha256:" + Identity.Sha256(dat), false), dat), ReplaceScope.Draft);

    private static ReplacePreview Run(byte[] draftBytes, int assignment, SectionScope scope, ReplaceSource source, ReplaceScope replaceScope)
    {
        ArgumentNullException.ThrowIfNull(draftBytes);
        ArgumentNullException.ThrowIfNull(source);
        var before = SessionSource.Parse(draftBytes).Definition!;
        Guard.Require((uint)assignment < (uint)before.Assignments.Length, "DSL-PROFILE-TARGET");
        int edited = before.Assignments[assignment].Profile;
        int[] serving = Enumerable.Range(0, before.Assignments.Length).Where(index => before.Assignments[index].Profile == edited).ToArray();
        Guard.Require(scope == SectionScope.Shared || serving.Length == 1, "DSL-PROFILE-TARGET");
        int[] stations = replaceScope == ReplaceScope.BlendChain ? Enumerable.Range(0, before.Assignments.Length).ToArray() : serving;
        int[] outside = Enumerable.Range(0, before.Assignments.Length)
            .Where(index => !stations.Contains(index) && (stations.Contains(index - 1) || stations.Contains(index + 1))).ToArray();
        double acceptanceChord = stations.Max(index => Placement.Frame(draftBytes, before.Assignments[index].Eta).ChordMeters);
        double limit = AcceptanceMeters / acceptanceChord;
        var target = before.Profiles[edited];
        var shape = Shape.Read(source);

        // The current spacing always yields a candidate or refuses (rule 6b), so every number below is finite.
        Candidate current = shape.Exact(target) ?? FitOn(shape, target, target.Upper.Knots, target.Upper.Points.Select(point => point[0]).ToArray(), keepIds: true, "current", source.DisplayName)
            ?? throw new System.Diagnostics.UnreachableException();
        Candidate? chosen = current.Residual <= limit ? current : null;
        double best = current.Residual;
        Candidate bestCandidate = current;
        if (chosen is null && outside.Length > 0)
            return Refused("CAT-SPACING", SpacingReason(before, stations, outside, source.DisplayName, best, acceptanceChord, target.Upper.Points.Length),
                stations, best, acceptanceChord, target.Upper.Points.Length, Enumerable.Range(0, before.Assignments.Length).ToArray(),
                Write(draftBytes, target, current, source.Provenance, before, stations));
        for (int count = SmallestOwnSpacing; chosen is null && count <= LargestOwnSpacing; count++)
        {
            var (knots, x) = DatImport.OwnSqrtBasis(count);
            var own = FitOn(shape, target, knots, x, keepIds: false, "own-" + count.ToString(CultureInfo.InvariantCulture), source.DisplayName);
            if (own is null) continue;
            if (own.Residual < best) { best = own.Residual; bestCandidate = own; }
            if (own.Residual <= limit) chosen = own;
        }
        if (chosen is null)
            return Refused("CAT-RESIDUAL", ResidualReason(source.DisplayName, best, acceptanceChord), stations, best,
                acceptanceChord, bestCandidate.Upper.Length, null,
                Write(draftBytes, target, bestCandidate, source.Provenance, before, stations));

        byte[] next = Write(draftBytes, target, chosen, source.Provenance, before, stations);
        var after = SessionSource.Parse(next).Definition!;
        try
        {
            // Ruling 71 and its budget clause (F-1): the one predicate for every section step, called once here for Replace.
            SectionEdits.RequireNeighbourAbscissa(before, after, assignment);
        }
        catch (ContractError refused)
        {
            return Refused("CAT-SPACING", refused.Reason ?? refused.Code, stations, chosen.Residual, acceptanceChord, chosen.Upper.Length,
                Enumerable.Range(0, before.Assignments.Length).ToArray(), next);
        }
        var replaced = after.Profiles.Single(profile => profile.Name == target.Name);
        var (change, at) = LargestChange(stations.Select(index => before.Profiles[before.Assignments[index].Profile]).Distinct(), replaced);
        var report = new ImportReport(chosen.Residual, chosen.Upper.Length, true, source.Provenance.Format(), chosen.Basis, stations,
            chosen.Dropped, shape.LeShift, shape.RotationDegrees, shape.Scale, shape.Thickness, shape.FrameResidual);
        return new ReplacePreview(stations, chosen.Spacing, chosen.Residual, acceptanceChord, change, at, chosen.Upper.Length, null, null, next)
        { Report = report };
    }

    // A refused candidate is visual evidence only. Patch reads Bytes only after checking RefusalCode.
    private static ReplacePreview Refused(string code, string reason, int[] stations, double residual, double acceptanceChord,
        int points, int[]? chain, byte[] candidate) =>
        new(stations, "current", residual, acceptanceChord, 0, 0, points, code, chain, null)
        { RefusalReason = reason, RefusedBytes = candidate, RefusedResidual = residual };

    // A fitted record on one spacing. Residual is the rule-5 Euclidean residual in chord fractions.
    private sealed record Candidate(string Spacing, string Basis, double[] Knots, double[][] Upper, double[][] Lower, string[] UpperIds,
        string[] LowerIds, TangentRow[] UpperRows, TangentRow[] LowerRows, string Closure, double Residual, IReadOnlyList<string> Dropped);

    // On the current spacing (keepIds) a fit that cannot keep the spacing or its point types refuses with DSL-LOCK naming the
    // conflict (rule 6b): never a silent fall back to an own spacing that would drop the rows. On an own spacing a singular
    // fit is only that spacing failing, and the scan goes on.
    private static Candidate? FitOn(Shape shape, ProfileDefinition target, double[] knots, double[] x, bool keepIds, string spacing, string source)
    {
        int degree = target.Upper.Degree;
        if (keepIds && (!target.Upper.Knots.SequenceEqual(target.Lower.Knots) ||
                        !target.Upper.Points.Select(point => point[0]).SequenceEqual(target.Lower.Points.Select(point => point[0]))))
            throw new ContractError("DSL-LOCK", $"This section's upper and lower points sit at different chord positions, so {source} can't be fitted " +
                "on its spacing. Nothing changed.");
        string[] upperIds = keepIds ? target.Upper.Ids : Enumerable.Range(0, x.Length).Select(index => "cv-" + index.ToString(CultureInfo.InvariantCulture)).ToArray();
        string[] lowerIds = keepIds ? target.Lower.Ids : upperIds;
        TangentRow[] upperRows = keepIds ? target.Upper.Tangents : [];
        TangentRow[] lowerRows = keepIds ? target.Lower.Tangents : [];
        bool closed = shape.Closed;
        var fit = DatImport.FitToBasis(shape.Samples, knots, x, keepIds ? degree : 5, closed, upperRows, upperIds, lowerRows, lowerIds);
        if (fit is null && keepIds)
        {
            string[] rows = upperRows.Concat(lowerRows).Select(row => row.Id).Distinct(StringComparer.Ordinal).ToArray();
            throw new ContractError("DSL-LOCK", rows.Length == 0
                ? $"{source} can't be fitted on this section's spacing. Nothing changed."
                : $"The point types at {SectionEdits.JoinNames(rows)} can't all hold for {source} on this section's spacing. Nothing changed.");
        }
        if (fit is null) return null;
        int curveDegree = keepIds ? degree : 5;
        double residual = Math.Max(
            DatImport.EuclideanResidual(shape.Samples.Upper, shape.UpperCurve, knots, curveDegree, x, fit.Upper),
            DatImport.EuclideanResidual(shape.Samples.Lower, shape.LowerCurve, knots, curveDegree, x, fit.Lower));
        var dropped = keepIds ? Array.Empty<string>() : target.Upper.Tangents.Select(row => row.Id).Concat(target.Lower.Tangents.Select(row => row.Id))
            .Distinct(StringComparer.Ordinal).ToArray();
        return new(spacing, keepIds ? "current" : "own", knots, Points(x, fit.Upper), Points(x, fit.Lower), upperIds, lowerIds,
            upperRows, lowerRows, closed ? "closed" : "open", residual, dropped);
    }

    private static double[][] Points(double[] x, double[] y) => x.Select((value, index) => new[] { value, y[index] }).ToArray();

    // Rewrites the edited block in place (name kept), then for the blend chain re-points every station to it and deletes the
    // blocks that leaves unreferenced (P-B3).
    private static byte[] Write(byte[] bytes, ProfileDefinition target, Candidate chosen, Provenance provenance, Definition before, int[] stations)
    {
        string name = target.Name;
        byte[] next = bytes;
        bool rowsChange = !chosen.UpperRows.SequenceEqual(target.Upper.Tangents) || !chosen.LowerRows.SequenceEqual(target.Lower.Tangents);
        if (rowsChange)
        {
            next = FoilSource.WriteSideTangents(next, name, SurfaceSide.Upper, []);
            next = FoilSource.WriteSideTangents(next, name, SurfaceSide.Lower, []);
        }
        next = FoilSource.WriteSurfaces(next, name, chosen.Knots, chosen.Upper, chosen.UpperIds, chosen.Lower, chosen.LowerIds);
        if (rowsChange)
        {
            next = FoilSource.WriteSideTangents(next, name, SurfaceSide.Upper, chosen.UpperRows);
            next = FoilSource.WriteSideTangents(next, name, SurfaceSide.Lower, chosen.LowerRows);
        }
        next = WriteTail(next, name, chosen.Closure, provenance.Format());
        if (stations.Length == before.Assignments.Length && stations.Any(index => before.Assignments[index].Profile != before.Assignments[stations[0]].Profile))
            next = RepointAll(next, name);
        Guard.Require(FoilSource.Parse(next).IsParsed, "DSL-PATCH");
        return next;
    }

    // The block's closure and provenance lines, after the lower curve.
    private static byte[] WriteTail(byte[] bytes, string name, string closure, string provenance)
    {
        var definition = SessionSource.Parse(bytes).Definition!;
        var profile = definition.Profiles.Single(item => item.Name == name);
        string text = FoilSource.Utf8.GetString(bytes);
        int lowerClose = profile.Lower.InsertAt;
        int blockClose = text.LastIndexOf('}', profile.BlockEnd - 1);
        Guard.Require(text[lowerClose] == '}' && blockClose > lowerClose, "DSL-PATCH");
        int newline = text.LastIndexOf('\n', profile.BlockStart);
        string indent = newline < 0 ? "" : text[(newline + 1)..profile.BlockStart];
        string tail = "\n" + indent + "  closure " + closure + (provenance.Length == 0 ? "" : "\n" + indent + "  provenance " + Jcs.Quote(provenance)) + "\n" + indent;
        return FoilSource.Utf8.GetBytes(text[..(lowerClose + 1)] + tail + text[blockClose..]);
    }

    private static byte[] RepointAll(byte[] bytes, string name)
    {
        var definition = SessionSource.Parse(bytes).Definition!;
        string text = FoilSource.Utf8.GetString(bytes);
        var referenced = definition.Assignments.Select(item => definition.Profiles[item.Profile].Name).ToHashSet(StringComparer.Ordinal);
        var edits = new List<(int Start, int End, string Value)>();
        for (int index = 0; index < definition.Assignments.Length; index++)
            if (definition.Profiles[definition.Assignments[index].Profile].Name != name)
                edits.Add((definition.AssignmentProfiles[index].Start, definition.AssignmentProfiles[index].End, Jcs.Quote(name)));
        foreach (var profile in definition.Profiles.Where(item => item.Name != name && referenced.Contains(item.Name)))
        {
            int newline = text.LastIndexOf('\n', profile.BlockStart);
            edits.Add((newline < 0 ? profile.BlockStart : newline, profile.BlockEnd, ""));
        }
        foreach (var edit in edits.OrderByDescending(item => item.Start))
            text = text[..edit.Start] + edit.Value + text[edit.End..];
        return FoilSource.Utf8.GetBytes(text);
    }

    // The largest |Δy| between any replaced station's old section and the new one, and the chord position where it is. This
    // is the vertical change at equal x on purpose: it answers "how much does the section move, and where" for the canvas
    // marker and the detail line, in the A4.5 profile oracle's terms (FoilSource.MaxOrdinateDeviation). Acceptance is a
    // different question — how far the fit is from the source shape — and is Euclidean (rule 5), because at the vertical
    // nose a gap at equal x overstates the true distance many times.
    private static (double Change, double AtX) LargestChange(IEnumerable<ProfileDefinition> olds, ProfileDefinition replaced)
    {
        double worst = 0, at = 0;
        var newUpper = Dense(replaced.Upper);
        var newLower = Dense(replaced.Lower);
        foreach (var old in olds)
        {
            var oldUpper = Dense(old.Upper);
            var oldLower = Dense(old.Lower);
            for (int i = 0; i <= 2000; i++)
            {
                double x = 0.5 * (1 - Math.Cos(Math.PI * i / 2000));
                double change = Math.Max(Math.Abs(YAt(oldUpper, x) - YAt(newUpper, x)), Math.Abs(YAt(oldLower, x) - YAt(newLower, x)));
                if (change > worst) { worst = change; at = x; }
            }
        }
        return (worst, at);
    }

    private static (double X, double Y)[] Dense(Curve curve) =>
        DatImport.Dense(curve.Knots, curve.Degree, curve.Points.Select(point => point[0]).ToArray(), curve.Points.Select(point => point[1]).ToArray(), DenseSegments);

    // y at x on a surface polyline whose x rises from the nose.
    private static double YAt((double X, double Y)[] curve, double x)
    {
        int low = 0, high = curve.Length - 1;
        if (x <= curve[0].X) return curve[0].Y;
        if (x >= curve[high].X) return curve[high].Y;
        while (high - low > 1)
        {
            int mid = (low + high) / 2;
            if (curve[mid].X <= x) low = mid; else high = mid;
        }
        double span = curve[high].X - curve[low].X;
        return span == 0 ? curve[low].Y : curve[low].Y + (curve[high].Y - curve[low].Y) * (x - curve[low].X) / span;
    }

    // catalog.preview's family and class (m12d §10): a My sections entry, or the catalog row its origin names; a .dat file
    // has no family. Class is the rights its provenance derives.
    internal static (string? Family, string Class) Describe(ReplaceSource source)
    {
        var rights = source.Provenance.Rights;
        string? family = source is ReplaceSource.Record ? nameof(CatalogFamily.MySections)
            : rights is RightsClass.Gen or RightsClass.Vend ? CatalogFamilies.Value?.GetValueOrDefault(source.Provenance.Origin![(source.Provenance.Origin!.IndexOf(':') + 1)..])
            : null;
        return (family, rights.ToString());
    }

    // The catalog's families by row id, read once; null when the catalog is unavailable (the event then records no family).
    private static readonly Lazy<Dictionary<string, string>?> CatalogFamilies = new(() =>
    {
        try { return Catalog.Load().ToDictionary(entry => entry.Id, entry => entry.Family.ToString(), StringComparer.Ordinal); }
        catch (ContractError) { return null; }
    });

    // COPY-191 (m12d design §11.2).
    private static string SpacingReason(Definition definition, int[] stations, int[] outside, string source, double residual, double chord, int points)
    {
        string station = SectionEdits.JoinNames(stations.Select(index => SectionEdits.StationName(index, definition.Assignments[index].Eta)).ToArray());
        string neighbour = SectionEdits.JoinNames(outside.Select(index => SectionEdits.StationName(index, definition.Assignments[index].Eta)).ToArray());
        return $"{station} blends point-to-point with {neighbour}, so {station} must keep {neighbour}'s {points} points at the same chord positions. " +
            $"On those points {source} is {Micrometres(residual, chord)} µm off ({Percent(residual)} % chord; limit 10 µm at {Millimetres(chord)} mm). Nothing changed.";
    }

    // Proposed copy (A4.6, the CAT-RESIDUAL row of m12d §7): Replace disabled with the best number.
    private static string ResidualReason(string source, double residual, double chord) =>
        $"No point spacing from {SmallestOwnSpacing} to {LargestOwnSpacing} points holds {source} within 10 µm: the closest is " +
        $"{Micrometres(residual, chord)} µm ({Percent(residual)} % chord; limit 10 µm at {Millimetres(chord)} mm). Nothing changed.";

    private static string Micrometres(double residual, double chord) => (residual * chord * 1e6).ToString("0.00", CultureInfo.InvariantCulture);
    private static string Percent(double residual) => (residual * 100).ToString("0.0000", CultureInfo.InvariantCulture);
    private static string Millimetres(double chord) => (chord * 1e3).ToString("0.##", CultureInfo.InvariantCulture);

    // The source in the chord frame: the samples a fit uses, the curve the residual is measured against, and what is reported.
    private sealed record Shape(DatProfile Samples, (double X, double Y)[] UpperCurve, (double X, double Y)[] LowerCurve, bool Closed,
        double? LeShift, double? RotationDegrees, double? Scale, double Thickness, ProfileDefinition? Record)
    {
        internal double? FrameResidual => LeShift is null ? null : -Math.Min(0, Math.Min(UpperCurve.Min(point => point.X), LowerCurve.Min(point => point.X)));

        internal static Shape Read(ReplaceSource source)
        {
            switch (source)
            {
                case ReplaceSource.Coordinates { Selig: { Length: > 0 } selig }:
                {
                    var (profile, shift, rotation, scale) = DatImport.ParseInChordFrame(selig);
                    var (upper, lower) = DatImport.SourceCurve(profile);
                    bool closed = Math.Abs(profile.Upper[^1].Y) <= 1e-9 && Math.Abs(profile.Lower[^1].Y) <= 1e-9;
                    return new(profile, upper, lower, closed, shift, rotation, scale, ThicknessOf(upper, lower), null);
                }
                case ReplaceSource.Record { SectionDocument: { Length: > 0 } document }:
                {
                    var parsed = FoilSource.Parse(document);
                    var entry = parsed.IsParsed && parsed.Definition!.Profiles.Length == 1 ? parsed.Definition.Profiles[0] : throw new ContractError("CAT-NOT-ADMITTED");
                    var upper = Dense(entry.Upper);
                    var lower = Dense(entry.Lower);
                    var samples = new DatProfile(source.DisplayName, "record", Cosine(upper), Cosine(lower), document, 2 * SourceSamples);
                    return new(samples, upper, lower, entry.Closure == "closed", null, null, null, ThicknessOf(upper, lower), entry);
                }
                default:
                    throw new ContractError("CAT-NOT-ADMITTED", "This section has no coordinates in this build. Nothing changed.");
            }
        }

        // P-E7: an entry already on the target's spacing is copied exactly; its point types come with it, on the target's ids.
        internal Candidate? Exact(ProfileDefinition target)
        {
            if (Record is not { } entry || entry.Upper.Degree != target.Upper.Degree || !entry.Upper.Knots.SequenceEqual(target.Upper.Knots) ||
                !entry.Lower.Knots.SequenceEqual(target.Lower.Knots) || entry.Upper.Points.Length != target.Upper.Points.Length ||
                !SameX(entry.Upper, target.Upper) || !SameX(entry.Lower, target.Lower))
                return null;
            TangentRow[] Rows(Curve from, Curve to) => from.Tangents.Select(row => row with { Id = to.Ids[Array.IndexOf(from.Ids, row.Id)] }).ToArray();
            return new("exact", "exact", target.Upper.Knots, entry.Upper.Points.Select(point => point.ToArray()).ToArray(),
                entry.Lower.Points.Select(point => point.ToArray()).ToArray(), target.Upper.Ids, target.Lower.Ids,
                Rows(entry.Upper, target.Upper), Rows(entry.Lower, target.Lower), entry.Closure, 0, []);
        }

        private static bool SameX(Curve left, Curve right) => left.Points.Select(point => point[0]).SequenceEqual(right.Points.Select(point => point[0]));

        // 201 cosine samples in x along a surface polyline, nose first (rule 5).
        private static List<ProfilePoint> Cosine((double X, double Y)[] curve) =>
            Enumerable.Range(0, SourceSamples).Select(i =>
            {
                double x = 0.5 * (1 - Math.Cos(Math.PI * i / (SourceSamples - 1)));
                return new ProfilePoint(x, YAt(curve, x));
            }).ToList();

        private static double ThicknessOf((double X, double Y)[] upper, (double X, double Y)[] lower)
        {
            double worst = 0;
            for (int i = 1; i < 1000; i++)
            {
                double x = i / 1000.0;
                worst = Math.Max(worst, YAt(upper, x) - YAt(lower, x));
            }
            return worst;
        }
    }
}
