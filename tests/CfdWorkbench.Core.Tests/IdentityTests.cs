using CfdWorkbench.Core;

namespace CfdWorkbench.Core.Tests;

internal static class IdentityTests
{
    private static int failures;
    private static int Main()
    {
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
        Console.WriteLine($"RESULT failures={failures}");
        return failures == 0 ? 0 : 1;
    }

    internal static void Check(string name, Action assertion)
    {
        // Test-runner boundary: report unexpected exceptions as failures and continue.
        try { assertion(); Console.WriteLine("PASS " + name); }
        catch (Exception failure) { failures++; Console.WriteLine("FAIL " + name + " " + failure.GetType().Name); }
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
}
