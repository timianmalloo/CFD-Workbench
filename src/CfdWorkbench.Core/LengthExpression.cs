namespace CfdWorkbench.Core;

/// <summary>
/// A4.8 length entry. A bare number is millimetres; a result must be a length.
/// Rounded half-even to 1 µm. Refusals are only DSL-INVALID-NUMERIC and DSL-UNIT.
/// </summary>
public static class LengthExpression
{
    public static double ParseMeters(string text, IReadOnlyDictionary<string, double> dimensionsMeters) =>
        throw new NotImplementedException();
}
