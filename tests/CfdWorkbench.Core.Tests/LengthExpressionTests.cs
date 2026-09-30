using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class LengthExpressionTests
{
    internal static void Run()
    {
        var dimensions = new Dictionary<string, double> { ["span"] = 1.2, ["root_chord"] = 0.2, ["tip_chord"] = 0.05 };
        Check("LengthExpression_UnitsAndReferences_ResolvedToMetres", () =>
        {
            Metres("15 cm", 150000, dimensions);
            Metres("10 mm", 10000, dimensions);
            Metres("1 m", 1000000, dimensions);
            Metres("1 in", 25400, dimensions);
            Metres("#root_chord * 0.5", 100000, dimensions);
            Metres("(#root_chord + 10 mm) / 2", 105000, dimensions);
            Metres("#span", 1200000, dimensions);
            Metres("#tip_chord", 50000, dimensions);
            Metres("152.09", 152090, dimensions);
            Metres("152.090", 152090, dimensions);
            Equal(LengthExpression.ParseMeters("152.09", dimensions), LengthExpression.ParseMeters("152.090", dimensions));
        });
        Check("LengthExpression_NonLength_DslUnit", () =>
        {
            Refuses("DSL-UNIT", () => LengthExpression.ParseMeters("2 mm * 3 mm", dimensions));
            Refuses("DSL-UNIT", () => LengthExpression.ParseMeters("#span / #root_chord", dimensions));
            Refuses("DSL-UNIT", () => LengthExpression.ParseMeters("2 * 3", dimensions));
        });
        Check("LengthExpression_RandomText_NeverThrowsUnexpected", () =>
        {
            var random = new Random(20260930);
            var hostile = new[]
            {
                "", " ", "(", ")", "#", "#nope", "1e99999", "#span / 0", "1 mm / 0",
                new string('(', 17) + "1 mm" + new string(')', 17),
                new string('1', 257),
                new string('(', 512)
            };
            foreach (string text in hostile) Probe(text, dimensions);
            for (int sample = 0; sample < 200; sample++)
            {
                int length = random.Next(0, 513);
                var chars = new char[length];
                for (int index = 0; index < length; index++) chars[index] = (char)random.Next(32, 127);
                Probe(new string(chars), dimensions);
            }
        });
        Check("LengthExpression_SubMicrometreInput_RoundedToMicrometre", () =>
        {
            Metres("1.0004 mm", 1000, dimensions);
            Metres("1.0005 mm", 1000, dimensions);
            Metres("1.0015 mm", 1002, dimensions);
        });
    }

    private static double FromMicrometres(long micrometres) => (double)(micrometres / 1_000_000m);

    private static void Metres(string text, long micrometres, IReadOnlyDictionary<string, double> dimensions) =>
        Equal(FromMicrometres(micrometres), LengthExpression.ParseMeters(text, dimensions));

    private static void Probe(string text, IReadOnlyDictionary<string, double> dimensions)
    {
        if (text.Length > 512) throw new InvalidOperationException("probe longer than 512");
        try { LengthExpression.ParseMeters(text, dimensions); }
        catch (ContractError error) when (error.Code is "DSL-INVALID-NUMERIC" or "DSL-UNIT") { }
    }
}
