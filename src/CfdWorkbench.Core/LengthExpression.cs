using System.Globalization;

namespace CfdWorkbench.Core;

/// <summary>
/// A4.8 length entry. A lone bare number is millimetres. A result must be a first-power length.
/// Rounded half-even to 1 µm. Refusals are only DSL-INVALID-NUMERIC and DSL-UNIT.
/// </summary>
public static class LengthExpression
{
    public static double ParseMeters(string text, IReadOnlyDictionary<string, double> dimensionsMeters)
    {
        try { return ParseCore(text, dimensionsMeters); }
        catch (ContractError) { throw; }
        catch (OverflowException) { throw Invalid(); }
    }

    private static double ParseCore(string text, IReadOnlyDictionary<string, double> dimensionsMeters)
    {
        if (text is null || text.Length is 0 or > 256) throw Invalid();
        string source = text.Trim();
        if (source.Length == 0) throw Invalid();
        if (IsBareNumber(source)) return Round(Decimal(source) / 1000m);
        var parser = new Parser(source, dimensionsMeters);
        var (value, power) = parser.Parse();
        if (power != 1) throw Unit();
        return Round(value);
    }

    private static double Round(decimal metres)
    {
        decimal micrometres = decimal.Round(metres * 1_000_000m, 0, MidpointRounding.ToEven);
        return (double)(micrometres / 1_000_000m);
    }

    private static bool IsBareNumber(string text)
    {
        int index = 0;
        if (text[index] is '+' or '-') index++;
        int digits = CountDigits(text, ref index);
        if (index < text.Length && text[index] == '.') { index++; digits += CountDigits(text, ref index); }
        if (digits == 0) return false;
        if (index < text.Length && text[index] is 'e' or 'E')
        {
            index++;
            if (index < text.Length && text[index] is '+' or '-') index++;
            if (CountDigits(text, ref index) == 0) return false;
        }
        return index == text.Length;
    }

    private static int CountDigits(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && char.IsAsciiDigit(text[index])) index++;
        return index - start;
    }

    private static decimal Decimal(string token)
    {
        try { return decimal.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture); }
        catch (FormatException) { throw Invalid(); }
        catch (OverflowException) { throw Invalid(); }
    }

    private static ContractError Invalid() => new("DSL-INVALID-NUMERIC");
    private static ContractError Unit() => new("DSL-UNIT");

    private sealed class Parser
    {
        private readonly string text;
        private readonly IReadOnlyDictionary<string, double> dimensions;
        private int index;

        internal Parser(string text, IReadOnlyDictionary<string, double> dimensions)
        {
            this.text = text;
            this.dimensions = dimensions;
        }

        internal (decimal Value, int Power) Parse()
        {
            var value = Expression(0);
            Skip();
            if (index != text.Length) throw Invalid();
            return value;
        }

        private (decimal Value, int Power) Expression(int depth)
        {
            if (depth > 16) throw Invalid();
            var left = Term(depth);
            while (true)
            {
                Skip();
                if (Peek is not ('+' or '-')) return left;
                char op = Take();
                var right = Term(depth);
                if (left.Power != right.Power) throw Unit();
                left = (op == '+' ? left.Value + right.Value : left.Value - right.Value, left.Power);
            }
        }

        private (decimal Value, int Power) Term(int depth)
        {
            var left = Unary(depth);
            while (true)
            {
                Skip();
                if (Peek is not ('*' or '/')) return left;
                char op = Take();
                var right = Unary(depth);
                if (op == '*') left = (left.Value * right.Value, left.Power + right.Power);
                else
                {
                    if (right.Value == 0) throw Invalid();
                    left = (left.Value / right.Value, left.Power - right.Power);
                }
            }
        }

        private (decimal Value, int Power) Unary(int depth)
        {
            Skip();
            if (Peek is '-' or '+')
            {
                char op = Take();
                var inner = Unary(depth);
                return (op == '-' ? -inner.Value : inner.Value, inner.Power);
            }
            return Primary(depth);
        }

        private (decimal Value, int Power) Primary(int depth)
        {
            Skip();
            if (Peek == '(')
            {
                Take();
                var inner = Expression(depth + 1);
                Skip();
                if (Peek != ')') throw Invalid();
                Take();
                return inner;
            }
            if (Peek == '#')
            {
                Take();
                string name = Name();
                if (name is not ("span" or "root_chord" or "tip_chord")) throw Invalid();
                if (!dimensions.TryGetValue(name, out double metres) || !double.IsFinite(metres)) throw Invalid();
                return ((decimal)metres, 1);
            }
            if (Peek is not ( >= '0' and <= '9' or '.')) throw Invalid();
            string token = Number();
            decimal value = Decimal(token);
            Skip();
            if (Peek is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                string unit = Name();
                value = unit switch
                {
                    "mm" => value / 1000m,
                    "cm" => value / 100m,
                    "m" => value,
                    "in" => value * 0.0254m,
                    _ => throw Invalid()
                };
                return (value, 1);
            }
            return (value, 0);
        }

        private string Number()
        {
            int start = index;
            while (Peek is >= '0' and <= '9') Take();
            if (Peek == '.') { Take(); while (Peek is >= '0' and <= '9') Take(); }
            if (index == start) throw Invalid();
            if (Peek is 'e' or 'E')
            {
                Take();
                if (Peek is '+' or '-') Take();
                int exponent = index;
                while (Peek is >= '0' and <= '9') Take();
                if (index == exponent) throw Invalid();
            }
            return text[start..index];
        }

        private string Name()
        {
            int start = index;
            if (Peek is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_')) throw Invalid();
            while (Peek is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_') Take();
            return text[start..index];
        }

        private void Skip() { while (Peek != '\0' && char.IsWhiteSpace(Peek)) index++; }
        private char Peek => index < text.Length ? text[index] : '\0';
        private char Take() => text[index++];
    }
}
