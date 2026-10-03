using System.Numerics;
using CfdWorkbench.Core;

namespace GspkProbe;

/// <summary>One side's sub-Bézier piece (x and y Bernstein coefficients on a dyadic parameter cut). The vertical
/// deviation from the chord line through its end points, y − ℓ(x), is itself a Bézier in the same parameter with
/// coefficients y_j − ℓ(x_j) (ℓ is affine in x), so its hull [ELo, EHi] encloses the side over its whole x-range.
/// That bound is second order in the piece length, which is what makes the overlay converge.</summary>
internal sealed class Piece
{
    internal Piece(Rational[] x, Rational[] y, int depth)
    {
        X = x; Y = y; Depth = depth;
        var dx = X[^1] - X[0];
        Geometry.Require(dx > 0, "A piece has no x extent (abscissa not strictly monotone).");
        // Any affine reference is sound (the e_j are exact for it), so the chord slope is rounded to keep bits small.
        Slope = ((Y[^1] - Y[0]) / dx).DyadicDown(Overlay.SlopeBits);
        Rational lo = 0, hi = 0;
        for (int j = 1; j < X.Length; j++) // j = n is no longer 0 once the slope is rounded
        {
            var e = Y[j] - Y[0] - Slope * (X[j] - X[0]);
            if (e < lo) lo = e;
            if (e > hi) hi = e;
        }
        ELo = lo.DyadicDown(Overlay.BoundBits); EHi = hi.DyadicUp(Overlay.BoundBits);
    }
    private (Piece, Piece)? children;
    internal Rational[] X { get; }
    internal Rational[] Y { get; }
    internal int Depth { get; }
    internal Rational Slope { get; }
    internal Rational ELo { get; }
    internal Rational EHi { get; }
    internal Rational X0 => X[0];
    internal Rational X1 => X[^1];
    internal Rational Width => EHi - ELo;
    internal Rational Line(Rational x) => Y[0] + Slope * (x - X[0]);

    internal (Piece Left, Piece Right) Split()
    {
        Geometry.Require(Depth < 64, "Overlay piece depth budget exhausted.");
        // Memoised: atoms share pieces, so a piece is split at most once however many atoms hold it.
        if (children is { } done) return done;
        var (lx, rx) = Halve(X); var (ly, ry) = Halve(Y);
        children = (new Piece(lx, ly, Depth + 1), new Piece(rx, ry, Depth + 1));
        return children.Value;
    }

    // The same de Casteljau midpoint split as Bernstein.Split (private in Core).
    internal static (Rational[] Left, Rational[] Right) Halve(Rational[] coefficients)
    {
        var working = coefficients.ToArray();
        var left = new Rational[working.Length]; var right = new Rational[working.Length];
        left[0] = working[0]; right[^1] = working[^1];
        for (int level = 1; level < working.Length; level++)
        {
            for (int j = 0; j < working.Length - level; j++) working[j] = Midpoint(working[j], working[j + 1]);
            left[level] = working[0]; right[^(level + 1)] = working[working.Length - level - 1];
        }
        return (left, right);
    }

    // (a + b)/2 as one exact construction (the Rational operators would construct twice).
    private static Rational Midpoint(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, 2 * a.Denominator * b.Denominator);

    internal static Piece[] From(PolynomialSpan[] spans) => spans.Select(span => new Piece(span.X, span.Y, 0)).ToArray();
}

/// <summary>A cell of the x-overlay: one piece per side and the x window [Lo, Hi] they all cover.</summary>
internal sealed class Atom(Piece[] pieces, Rational lo, Rational hi)
{
    internal Piece[] Pieces { get; } = pieces;
    internal Rational Lo { get; } = lo;
    internal Rational Hi { get; } = hi;

    /// <summary>Enclosure of upper_k − lower_k at an end of the window (profile k uses sides 2k and 2k+1).</summary>
    internal (Rational Low, Rational High) Difference(int profile, Rational x)
    {
        var u = Pieces[2 * profile]; var l = Pieces[2 * profile + 1];
        var affine = u.Line(x) - l.Line(x);
        // Outward dyadic rounding keeps every stored bound at BoundBits fractional bits.
        return ((affine + u.ELo - l.EHi).DyadicDown(Overlay.BoundBits), (affine + u.EHi - l.ELo).DyadicUp(Overlay.BoundBits));
    }

    /// <summary>Lower bound of upper − lower over the whole window (separation).</summary>
    internal Rational SeparationLower()
    {
        var u = Pieces[0]; var l = Pieces[1];
        var a = u.Line(Lo) - l.Line(Lo); var b = u.Line(Hi) - l.Line(Hi);
        return (a < b ? a : b) + u.ELo - l.EHi;
    }

    internal IEnumerable<Atom> SplitSide(int side)
    {
        var (left, right) = Pieces[side].Split();
        foreach (var piece in new[] { left, right })
        {
            var lo = piece.X0 > Lo ? piece.X0 : Lo;
            var hi = piece.X1 < Hi ? piece.X1 : Hi;
            if (lo >= hi) continue;
            var next = Pieces.ToArray(); next[side] = piece;
            yield return new Atom(next, lo, hi);
        }
    }

    internal static List<Atom> Initial(Piece[][] sides, Rational lo, Rational hi)
    {
        var cuts = new List<Rational> { lo, hi };
        foreach (var side in sides)
            foreach (var piece in side)
                foreach (var x in new[] { piece.X0, piece.X1 })
                    if (x > lo && x < hi) cuts.Add(x);
        cuts.Sort((a, b) => a.CompareTo(b));
        var atoms = new List<Atom>();
        for (int i = 0; i + 1 < cuts.Count; i++)
        {
            var a = cuts[i]; var b = cuts[i + 1];
            if (a >= b) continue;
            var chosen = sides.Select(side => side.First(piece => piece.X0 <= a && piece.X1 >= b)).ToArray();
            atoms.Add(new Atom(chosen, a, b));
        }
        return atoms;
    }
}

/// <summary>A line in the blend weight w on [0,1], stored by its values at w = 0 and w = 1.</summary>
internal readonly record struct WLine(Rational V0, Rational V1, Rational Slope)
{
    internal static WLine Of(Rational v0, Rational v1) => new(v0, v1, v1 - v0);
    internal Rational At(Rational w) => V0 + Slope * w;
}

/// <summary>Upper envelope of lines on w ∈ [0,1], exact.</summary>
internal sealed class Envelope
{
    internal List<WLine> Lines { get; } = new();
    internal List<Rational> Points { get; } = new(); // 0, breakpoints, 1; segment i lies between Points[i] and Points[i+1]

    internal static Envelope Of(IEnumerable<WLine> input, ProofBudget watch)
    {
        var lines = input.ToList();
        lines.Sort((a, b) => { int c = a.Slope.CompareTo(b.Slope); return c != 0 ? c : b.V0.CompareTo(a.V0); });
        var hull = new List<WLine>();
        foreach (var line in lines)
        {
            if (hull.Count > 0 && hull[^1].Slope.CompareTo(line.Slope) == 0) continue; // same slope, lower intercept
            while (hull.Count >= 2)
            {
                var a = hull[^2]; var b = hull[^1];
                // b is useless when a and line meet at or before a and b meet.
                if ((a.V0 - line.V0) * (b.Slope - a.Slope) <= (a.V0 - b.V0) * (line.Slope - a.Slope)) hull.RemoveAt(hull.Count - 1);
                else break;
            }
            hull.Add(line);
        }
        watch.Check();
        var result = new Envelope();
        Rational zero = 0, one = 1;
        for (int i = 0; i < hull.Count; i++)
        {
            Rational? start = i == 0 ? null : (hull[i - 1].V0 - hull[i].V0) / (hull[i].Slope - hull[i - 1].Slope);
            Rational? end = i + 1 == hull.Count ? null : (hull[i].V0 - hull[i + 1].V0) / (hull[i + 1].Slope - hull[i].Slope);
            if (start is { } s && s >= one) continue;
            if (end is { } e && e <= zero) continue;
            result.Lines.Add(hull[i]);
            result.Points.Add(result.Points.Count == 0 ? zero : start!.Value);
        }
        result.Points.Add(one);
        return result;
    }

    internal Rational At(Rational w)
    {
        int lo = 0, hi = Lines.Count - 1;
        while (lo < hi) { int mid = (lo + hi + 1) / 2; if (Points[mid] <= w) lo = mid; else hi = mid - 1; }
        return Lines[lo].At(w);
    }

    /// <summary>max over w ∈ [0,1] of line(w) − factor·this(w). Concave, so the maximum sits at the first breakpoint
    /// where factor·slope reaches the line's slope.</summary>
    internal Rational MaxExcess(WLine line, Rational factor)
    {
        int lo = 0, hi = Lines.Count; // first segment whose g-slope ≤ 0; Lines.Count means w = 1
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (line.Slope - factor * Lines[mid].Slope <= 0) hi = mid; else lo = mid + 1;
        }
        var w = Points[lo];
        return line.At(w) - factor * At(w);
    }
}

internal sealed record MaxResult(bool Certified, string Reason, int FinalAtoms, int PeakAtoms, int Splits, int Rounds,
    Rational[] GapPerScale, Envelope[] Lower, Envelope[] Upper, int MaxLineNumeratorBits, int MaxLineDenominatorBits, int MaxDepth);

internal sealed record SeparationResult(bool Certified, string Reason, int Atoms, int Splits, Rational Lo, Rational Hi, Rational MinLower);

internal static class Overlay
{
    /// <summary>Separation of one profile on [lo, hi]: every atom's lower bound of upper − lower is &gt; 0.</summary>
    internal static SeparationResult Separation(Piece[] upper, Piece[] lower, Rational lo, Rational hi, ProofBudget watch, int atomCap)
    {
        var pending = Atom.Initial(new[] { upper, lower }, lo, hi);
        int certified = 0, splits = 0;
        Rational minLower = 1;
        while (pending.Count > 0)
        {
            watch.Check();
            var atom = pending[^1]; pending.RemoveAt(pending.Count - 1);
            var bound = atom.SeparationLower();
            if (bound > 0) { certified++; if (bound < minLower) minLower = bound; continue; }
            var (a, b) = atom.Difference(0, atom.Lo);
            if (b < 0) return new(false, "Sides cross (upper − lower < 0 at x = " + atom.Lo.Nearest().ToString("R") + ").", certified + pending.Count, splits, lo, hi, minLower);
            int side = atom.Pieces[0].Width >= atom.Pieces[1].Width ? 0 : 1;
            if (atom.Pieces[side].Width.CompareTo(0) == 0 && atom.Pieces[1 - side].Width.CompareTo(0) == 0)
                return new(false, "Separation unresolved on straight pieces.", certified + pending.Count, splits, lo, hi, minLower);
            pending.AddRange(atom.SplitSide(side)); splits++;
            if (certified + pending.Count > atomCap)
                return new(false, "GEOMETRY-BUDGET: separation atoms exceed " + atomCap + ".", certified + pending.Count, splits, lo, hi, minLower);
        }
        return new(true, "separated", certified, splits, lo, hi, minLower);
    }

    internal const int SlopeBits = 96;
    internal const int BoundBits = 128;
    /// <summary>Per-round trace of the last Maximum call: round, atoms, violators, worst gap, work so far.</summary>
    internal static List<string> Trace { get; } = new();

    private sealed class Cell
    {
        internal required Atom Atom { get; init; }
        internal required WLine[] Low { get; init; }  // 2 per scale pair (window ends), scale-major
        internal required WLine[] High { get; init; }
    }

    private static Cell Lines(Atom atom, bool blend, (Rational A, Rational B)[] scales)
    {
        var low = new WLine[2 * scales.Length]; var high = new WLine[2 * scales.Length];
        var ends = new[] { atom.Lo, atom.Hi };
        for (int e = 0; e < 2; e++)
        {
            var a = atom.Difference(0, ends[e]);
            var b = blend ? atom.Difference(1, ends[e]) : a;
            // Separation is already proved, so f ≥ 0 and a negative lower bound may be raised to 0. That keeps every
            // scale·bound product monotone, so outward rounding of scale and product stays sound.
            Rational aLow = a.Low < 0 ? 0 : a.Low, bLow = b.Low < 0 ? 0 : b.Low;
            for (int s = 0; s < scales.Length; s++)
            {
                low[2 * s + e] = WLine.Of((scales[s].A * aLow).DyadicDown(BoundBits), (scales[s].B * bLow).DyadicDown(BoundBits));
                high[2 * s + e] = WLine.Of((scales[s].A * a.High).DyadicUp(BoundBits), (scales[s].B * b.High).DyadicUp(BoundBits));
            }
        }
        return new Cell { Atom = atom, Low = low, High = high };
    }

    /// <summary>
    /// Enclose max_x T0(x, w) uniformly for w ∈ [0,1], where T0 = (1 − w)·sA·(u_A − l_A) + w·sB·(u_B − l_B), for every
    /// scale pair given (the caller rounds the scales outward). A single profile is A = B (two sides), scale 1.
    /// Termination: U_s(w) ≤ (1 + δ)L_s(w) for all w and every s. Atoms whose upper lines exceed (1 + δ)L somewhere are
    /// refined (all of them per round); atoms wholly under L are dropped (their lower lines stay in the envelope).
    /// </summary>
    internal static MaxResult Maximum(Piece[][] sides, (Rational A, Rational B)[] scales, Rational delta, ProofBudget watch, int atomCap)
    {
        Trace.Clear();
        bool blend = sides.Length == 4;
        var cells = Atom.Initial(sides, 0, 1).Select(atom => Lines(atom, blend, scales)).ToList();
        var archive = scales.Select(_ => new List<WLine>()).ToArray();
        int peak = cells.Count, splits = 0, rounds = 0;
        var factor = 1 + delta;
        while (true)
        {
            rounds++;
            watch.Check();
            var lower = new Envelope[scales.Length];
            var upper = new Envelope[scales.Length];
            var gaps = new Rational[scales.Length];
            for (int s = 0; s < scales.Length; s++)
            {
                var lows = new List<WLine>(archive[s]);
                var highs = new List<WLine>();
                foreach (var cell in cells) { lows.Add(cell.Low[2 * s]); lows.Add(cell.Low[2 * s + 1]); highs.Add(cell.High[2 * s]); highs.Add(cell.High[2 * s + 1]); }
                lower[s] = Envelope.Of(lows, watch);
                archive[s] = lower[s].Lines.ToList();
                upper[s] = Envelope.Of(highs, watch);
                Geometry.Require(lower[s].Lines.All(line => line.V0 > 0 && line.V1 > 0), "Maximum lower bound is not positive.");
                Rational gap = 0;
                foreach (var w in upper[s].Points.Concat(lower[s].Points))
                {
                    var l = lower[s].At(w);
                    var g = (upper[s].At(w) - l) / l;
                    if (g > gap) gap = g;
                }
                gaps[s] = gap;
            }
            var next = new List<Cell>();
            int violators = 0;
            foreach (var cell in cells)
            {
                watch.Check();
                bool violates = false, alive = false;
                for (int s = 0; s < scales.Length && !violates; s++)
                    for (int k = 0; k < 2; k++)
                    {
                        var line = cell.High[2 * s + k];
                        if (lower[s].MaxExcess(line, factor) > 0) { violates = true; break; }
                        if (lower[s].MaxExcess(line, 1) > 0) alive = true;
                    }
                if (!violates) { if (alive) next.Add(cell); continue; }
                violators++;
                int side = 0; Rational worst = -1;
                for (int k = 0; k < sides.Length; k++)
                {
                    var weight = cell.Atom.Pieces[k].Width * (k < 2 ? scales[0].A : scales[0].B);
                    if (weight > worst) { worst = weight; side = k; }
                }
                foreach (var child in cell.Atom.SplitSide(side)) next.Add(Lines(child, blend, scales));
                splits++;
            }
            Trace.Add($"round {rounds}: atoms {cells.Count}, violators {violators}, gap {gaps.Max().Nearest():E3}, work {watch.Spent}");
            if (violators == 0)
                return Finish(true, "certified", cells.Select(c => c.Atom).ToList(), peak, splits, rounds, gaps, lower, upper);
            cells = next;
            if (cells.Count > peak) peak = cells.Count;
            if (cells.Count > atomCap)
                return Finish(false, "GEOMETRY-BUDGET: max atoms exceed " + atomCap + " (round " + rounds + ", gap " + gaps.Max().Nearest().ToString("E3") + ").",
                    cells.Select(c => c.Atom).ToList(), peak, splits, rounds, gaps, lower, upper);
        }
    }

    private static MaxResult Finish(bool ok, string reason, List<Atom> atoms, int peak, int splits, int rounds, Rational[] gaps, Envelope[] lower, Envelope[] upper)
    {
        int n = 0, d = 0, depth = 0;
        foreach (var env in lower.Concat(upper))
            foreach (var line in env.Lines)
                foreach (var v in new[] { line.V0, line.V1 })
                {
                    n = Math.Max(n, (int)BigInteger.Abs(v.Numerator).GetBitLength());
                    d = Math.Max(d, (int)v.Denominator.GetBitLength());
                }
        foreach (var atom in atoms) foreach (var piece in atom.Pieces) depth = Math.Max(depth, piece.Depth);
        return new(ok, reason, atoms.Count, peak, splits, rounds, gaps, lower, upper, n, d, depth);
    }

    /// <summary>Nose rule: a line y = m·x through the origin with the upper first-piece coefficients y_j − m·x_j ≥ 0
    /// (nose-handle coefficient &gt; 0) and the lower ≤ 0 (&lt; 0). Tries the first pieces, then their dyadic left halves.
    /// Returns the x up to which separation is proved, the m used, and whether m = 0 (the y = 0 rule) would have held.</summary>
    internal static (bool Ok, Rational CoveredTo, Rational M, bool ZeroLineHolds, int Depth) Nose(Piece upper, Piece lower, ProofBudget watch)
    {
        bool? zeroAtFirst = null;
        for (int depth = 0; depth <= 24; depth++)
        {
            watch.Check();
            var (ok, m, zero) = LineRule(upper, lower, origin: true, yTe: 0);
            zeroAtFirst ??= zero;
            if (ok) return (true, upper.X1 < lower.X1 ? upper.X1 : lower.X1, m, zeroAtFirst.Value, depth);
            upper = upper.Split().Left; lower = lower.Split().Left;
        }
        return (false, 0, 0, zeroAtFirst ?? false, 24);
    }

    /// <summary>Trailing-edge rule for a closed TE: a line through (1, y_te), upper last-piece coefficients above it
    /// (strict at the second-to-last), lower below. Tries the last pieces, then their dyadic right halves.</summary>
    internal static (bool Ok, Rational CoveredFrom, Rational M, int Depth) TrailingEdge(Piece upper, Piece lower, ProofBudget watch)
    {
        Geometry.Require(upper.Y[^1].CompareTo(lower.Y[^1]) == 0 && upper.X1.CompareTo(1) == 0 && lower.X1.CompareTo(1) == 0, "TE is not closed at x = 1.");
        for (int depth = 0; depth <= 24; depth++)
        {
            watch.Check();
            var (ok, m, _) = LineRule(upper, lower, origin: false, yTe: upper.Y[^1]);
            if (ok) return (true, upper.X0 > lower.X0 ? upper.X0 : lower.X0, m, depth);
            upper = upper.Split().Right; lower = lower.Split().Right;
        }
        return (false, 1, 0, 24);
    }

    private static (bool Ok, Rational M, bool ZeroHolds) LineRule(Piece upper, Piece lower, bool origin, Rational yTe)
    {
        // Shifted coordinates: X' = x (nose) or x − 1 (TE); Y' = y − y_te. Upper needs Y' − m·X' ≥ 0, lower ≤ 0.
        // For X' > 0 that bounds m above (upper) / below (lower); for X' < 0 the reverse.
        Rational? mLow = null, mHigh = null;
        bool feasible = true;
        // m is chosen strictly inside (mLow, mHigh), so every coefficient with x' ≠ 0 holds strictly.
        void Bound(Rational ratio, bool isUpperBound)
        {
            if (isUpperBound) { if (mHigh is null || ratio < mHigh) mHigh = ratio; }
            else { if (mLow is null || ratio > mLow) mLow = ratio; }
        }
        foreach (var (piece, sign) in new[] { (upper, 1), (lower, -1) })
        {
            int count = piece.X.Length - 1;
            for (int j = 0; j <= count; j++)
            {
                var xs = origin ? piece.X[j] : piece.X[j] - 1;
                var ys = piece.Y[j] - yTe;
                bool anchor = origin ? j == 0 : j == count;
                if (anchor) continue; // the shared point itself, value 0
                bool strict = origin ? j == 1 : j == count - 1;
                if (xs.CompareTo(0) == 0)
                {
                    // Independent of m: sign·y' must be ≥ 0 (> 0 at the handle).
                    var v = ys * sign;
                    if (v < 0 || (strict && v.CompareTo(0) == 0)) feasible = false;
                    continue;
                }
                var ratio = ys / xs;
                // sign·(y' − m·x') ≥ 0. With x' > 0: upper ⇒ m ≤ ratio; lower ⇒ m ≥ ratio. x' < 0 flips.
                bool upperBoundOnM = (sign > 0) == (xs > 0);
                Bound(ratio, upperBoundOnM);
            }
        }
        bool zero = feasible && (mLow is null || mLow < 0) && (mHigh is null || mHigh > 0);
        if (!feasible) return (false, 0, zero);
        if (mLow is { } lo && mHigh is { } hi && !(lo < hi)) return (false, 0, zero);
        Rational m = mLow is { } a && mHigh is { } b ? (a + b) / 2 : mLow is { } c ? c + 1 : mHigh is { } d ? d - 1 : 0;
        return (true, m, zero);
    }
}
