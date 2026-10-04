using CfdWorkbench.Core;

namespace CfdWorkbench.Core.Tests;

internal static class IdentityTests
{
    private static int failures;
    // Subset selector owned by tools/verify-application-core.py: comma-separated check-name prefixes.
    // Unset runs every check. Set, only matching checks run and print; a prefix that selects no check
    // fails the run, so a subset can never pass empty (HARNESS-SILENT-EXIT).
    private static readonly string[]? only = Environment.GetEnvironmentVariable("CFD_TEST_ONLY")?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static readonly HashSet<string> matched = [];
    private static int selected, skipped;
    // tools/run-tests.sh splits this harness across processes with `--part=k/n` (an argument, never an inherited
    // environment variable): a part runs the checks whose registration index i has i % n == k - 1. Every part
    // enumerates the same registrations, so together they run each check once; run-tests.sh compares the PARTITION
    // lines (docs/reviews/test-ci-waste.md §12). The Desktop harness's DesktopChecks carries the same rule.
    private static (int Index, int Count)? part;
    private static int registered, ran;
    private static int Main(string[] args)
    {
        try { part = ParsePart(args); }
        catch (ArgumentException error)
        {
            Console.WriteLine("FAIL PARTITION " + error.Message);
            return 1;
        }
        // Readiness tier (docs/design/m12b-points.md §12.3): never spawned by run-tests.sh, which
        // calls this harness with no arguments. PRE adds the switch; B0 fills PointModelTests.RunReadiness.
        if (args.Contains("--readiness"))
        {
            PointModelTests.RunReadiness();
            PlacementTests.RunReadiness();
            ProofBudgetTests.RunReadiness();
            DisplayProfileTests.RunReadiness();
            ProjectStoreTests.RunReadiness();
            SectionEditsTests.RunReadiness();
            SectionDraftTests.RunReadiness();
            Console.WriteLine($"RESULT failures={failures}");
            return failures == 0 ? 0 : 1;
        }
        // Independent published binary64/canonical pairs, RFC 8785 Appendix B:
        // https://www.rfc-editor.org/rfc/rfc8785.txt
        (ulong Bits, string Canonical)[] rfcVectors =
        [
            (0x0000000000000000, "0"), (0x8000000000000000, "0"),
            (0x0000000000000001, "5e-324"), (0x8000000000000001, "-5e-324"),
            (0x7fefffffffffffff, "1.7976931348623157e+308"), (0xffefffffffffffff, "-1.7976931348623157e+308"),
            (0x4340000000000000, "9007199254740992"), (0xc340000000000000, "-9007199254740992"),
            (0x4430000000000000, "295147905179352830000"),
            (0x44b52d02c7e14af5, "9.999999999999997e+22"), (0x44b52d02c7e14af6, "1e+23"), (0x44b52d02c7e14af7, "1.0000000000000001e+23"),
            (0x444b1ae4d6e2ef4e, "999999999999999700000"), (0x444b1ae4d6e2ef4f, "999999999999999900000"), (0x444b1ae4d6e2ef50, "1e+21"),
            (0x3eb0c6f7a0b5ed8c, "9.999999999999997e-7"), (0x3eb0c6f7a0b5ed8d, "0.000001"),
            (0x41b3de4355555553, "333333333.3333332"), (0x41b3de4355555554, "333333333.33333325"), (0x41b3de4355555555, "333333333.3333333"),
            (0x41b3de4355555556, "333333333.3333334"), (0x41b3de4355555557, "333333333.33333343"),
            (0xbecbf647612f3696, "-0.0000033333333333333333"), (0x43143ff3c1cb0959, "1424953923781206.2")
        ];
        foreach (var vector in rfcVectors)
            Check("Canonical_Rfc8785_" + vector.Bits.ToString("x16"), () => Equal(vector.Canonical, Jcs.Number(BitConverter.UInt64BitsToDouble(vector.Bits))));
        Check("Decimal_UnitEquivalent_ExactlyOneRounding", () =>
            Equal(0x3f8cc5b8dc55000dUL, BitConverter.DoubleToUInt64Bits(DecimalSi.Parse("14.049", -3))));
        Check("Decimal_Halfway_RoundsToEven", () =>
            Equal(1d, DecimalSi.Parse("1.00000000000000011102230246251565404236316680908203125")));
        Check("Decimal_HugeZeroExponent_NormalizesZero", () => Equal(0d, DecimalSi.Parse("-0e1000000000")));
        Check("Decimal_Subnormal_RoundsCorrectly", () =>
            Equal(1UL, BitConverter.DoubleToUInt64Bits(DecimalSi.Parse("5e-324"))));
        Check("Decimal_HugeNonzeroExponent_RefusesBeforeAllocation", () => Refuses("DSL-LIMIT", () => DecimalSi.Parse("1e1000000000")));
        Check("Decimal_UnicodeDigits_RefusesLexically", () => Refuses("DSL-LEX", () => DecimalSi.Parse("١")));
        Check("Decimal_UnsupportedScale_RefusesBeforeScaling", () => Refuses("DSL-UNIT", () => DecimalSi.Parse("1", 1)));
        Check("Decimal_TrailingNewline_RefusesNonToken", () => Refuses("DSL-LEX", () => DecimalSi.Parse("1\n")));
        Check("Decimal_MaximumIntegerScale_RefusesBeforeAllocation", () => Refuses("DSL-UNIT", () => DecimalSi.Parse("1", int.MaxValue)));
        Check("Decimal_MinimumIntegerScale_RefusesBeforeAllocation", () => Refuses("DSL-UNIT", () => DecimalSi.Parse("1", int.MinValue)));
        Check("Canonical_NegativeZero_Normalizes", () => Equal("0", Jcs.Number(-0d)));
        Check("Canonical_ExponentBoundary_UsesEcmaSpelling", () => Equal("1e+21", Jcs.Number(1e21)));
        Check("Blake3_OfficialEmptyVector_Matches", () =>
            Equal("af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262", Identity.Blake3([])));
        FoilSourceTests.Run();
        GeometryTests.Run();
        ProofBudgetTests.Run();
        PlacementTests.Run();
        PlacementTraceTests.Run();
        BlendTests.Run();
        AuthoringSessionTests.Run();
        ProjectStoreTests.Run();
        SectionEditTests.Run();
        SectionEditTests.RunMultiProfile();
        FitTests.Run();
        ConstructionTests.Run();
        FairSessionTests.Run();
        DatImportTests.Run();
        SectionReplaceTests.Run();
        ReopenConstructionTests.Run();
        ThicknessIntentTests.Run();
        WingEstimatesTests.Run();
        DimensionTests.Run();
        LayoutFileTests.Run();
        PreferenceStoreTests.Run();
        PointModelTests.Run();
        LengthExpressionTests.Run();
        PointGestureTests.Run();
        PointCommandTests.Run();
        ReopenPointEditTests.Run();
        ChannelEditTests.Run();
        ReopenChannelEditTests.Run();
        DisplayProfileTests.Run();
        SectionPointTests.Run();
        SectionEditsTests.Run();
        SectionDraftTests.Run();
        ReopenSectionDraftTests.Run();
        OverlayTests.Run();
        bool emptyPart = part is not null && only is null && ran == 0;
        if (part is { } p)
        {
            Console.WriteLine($"PARTITION {p.Index}/{p.Count} of {registered} checks");
            if (emptyPart) Console.WriteLine($"FAIL PARTITION {p.Index}/{p.Count} ran no check");
        }
        Console.WriteLine($"RESULT failures={failures}");
        return SelectionMatched() && failures == 0 && !emptyPart ? 0 : 1;
    }

    private static (int Index, int Count)? ParsePart(string[] args)
    {
        string? value = args.LastOrDefault(arg => arg.StartsWith("--part=", StringComparison.Ordinal))?["--part=".Length..];
        if (value is null) return null;
        string[] fields = value.Split('/');
        if (fields.Length == 2 && int.TryParse(fields[0], out int index) && int.TryParse(fields[1], out int count) &&
            index >= 1 && index <= count)
            return (index, count);
        throw new ArgumentException($"--part={value} is not k/n with 1 <= k <= n");
    }

    private static bool SelectionMatched()
    {
        if (only is null) return true;
        Console.WriteLine($"SUBSET CFD_TEST_ONLY={string.Join(',', only)} ran={selected} skipped={skipped}");
        string[] unmatched = only.Length == 0 ? ["(empty selector)"] : only.Where(prefix => !matched.Contains(prefix)).ToArray();
        foreach (string prefix in unmatched) Console.WriteLine("FAIL SELECTOR " + prefix + " matched no check");
        return unmatched.Length == 0;
    }

    internal static void Check(string name, Action assertion)
    {
        if (only is not null)
        {
            string[] hits = only.Where(prefix => name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            if (hits.Length == 0) { skipped++; return; }
            matched.UnionWith(hits); selected++;
        }
        // After the selector, so a prefix counts as matched in every part and a subset run splits like a full one.
        if (part is { } p && registered++ % p.Count != p.Index - 1) return;
        ran++;
        // Test-runner boundary: report unexpected exceptions as failures and continue.
        try { assertion(); Console.WriteLine("PASS " + name); }
        catch (Exception failure) { failures++; Console.WriteLine("FAIL " + name + " " + failure.GetType().Name + ": " + failure.Message); }
    }

    internal static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}; actual {actual}");
    }

    internal static void Refuses(string code, Action action)
    {
        try { action(); }
        catch (ContractError failure) { Equal(code, failure.Code); return; }
        throw new InvalidOperationException("Expected refusal " + code);
    }

    /// <summary>Opens a one-point draft through the shipped point-gesture path (the rail-ordinate draft API is retired).</summary>
    internal static SessionDraft BeginGestureDraft(this AuthoringSession session, string draftId, string curve, string vertexId) =>
        session.BeginPointGesture(draftId, curve, vertexId);

    /// <summary>Moves the gesture's point to <paramref name="aftSi"/> metres aft at its current span, as a drag along the aft axis.</summary>
    internal static SessionDraft GestureToAft(this AuthoringSession session, string draftId, long generation, double aftSi)
    {
        double span = 0;
        if (session.Snapshot().Draft is { } owned && owned.Id == draftId)
        {
            var view = Planform.View(owned.Bytes, "Draft", owned.Generation);
            var curve = owned.Rail == "leading" ? view.Leading : view.Trailing;
            span = curve.Points.Single(point => point.Id == owned.VertexId).SpanMeters;
        }
        return session.UpdatePointGesture(draftId, generation, span, aftSi).Draft;
    }
}
