using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace CfdWorkbench.Core;

public static class DecimalSi
{
    // Bound lexical work BEFORE parsing BigInteger or constructing powers of ten.
    public static double Parse(string token, int decimalScale = 0)
    {
        Guard.Require(decimalScale is 0 or -2 or -3 or -4 or -6, "DSL-UNIT");
        Guard.Require(token.Length <= 4096, "DSL-LIMIT");
        var m = Regex.Match(token, @"\A([+-]?)(?:([0-9]+)(?:\.([0-9]+))?|\.([0-9]+))(?:[eE]([+-]?[0-9]+))?\z", RegexOptions.CultureInvariant);
        Guard.Require(m.Success, "DSL-LEX");
        var exponentText = m.Groups[5].Success ? m.Groups[5].Value : "0";
        var fraction = m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value;
        var digits = (m.Groups[2].Value + fraction).TrimStart('0');
        // All-zero significands need no exponent evaluation, regardless of spelling.
        if (digits.Length == 0) return 0;
        var expDigits = exponentText.TrimStart('+', '-').TrimStart('0');
        // A <=4096-byte significand cannot compensate an exponent of magnitude >=10000.
        Guard.Require(expDigits.Length <= 4, "DSL-LIMIT");
        int exponent = expDigits.Length == 0 ? 0 : int.Parse(expDigits, CultureInfo.InvariantCulture) * (exponentText[0] == '-' ? -1 : 1);
        int power = exponent - fraction.Length;
        while (digits.EndsWith('0')) { digits = digits[..^1]; power++; }
        int magnitude = digits.Length - 1 + power;
        Guard.Require(magnitude is >= -400 and <= 400, "DSL-LIMIT");
        power += decimalScale;
        BigInteger numerator = BigInteger.Parse(digits, CultureInfo.InvariantCulture), denominator = BigInteger.One;
        if (power >= 0) numerator *= BigInteger.Pow(10, power); else denominator = BigInteger.Pow(10, -power);
        double result = Round(numerator, denominator);
        Guard.Require(double.IsFinite(result), "DSL-LEX");
        return result == 0 ? 0 : m.Groups[1].Value == "-" ? -result : result;
    }

    static BigInteger Quotient(BigInteger n, BigInteger d)
    {
        var q = BigInteger.DivRem(n, d, out var r);
        int cmp = (r * 2).CompareTo(d);
        return cmp > 0 || cmp == 0 && !q.IsEven ? q + 1 : q;
    }

    internal static double Round(BigInteger n, BigInteger d)
    {
        int e = (int)n.GetBitLength() - (int)d.GetBitLength();
        if (e >= 0 ? n < (d << e) : (n << -e) < d) e--;
        if (e > 1023) return double.PositiveInfinity;
        int shift = e < -1022 ? 1074 : 52 - e;
        BigInteger q = shift >= 0 ? Quotient(n << shift, d) : Quotient(n, d << -shift);
        if (e < -1022) return BitConverter.Int64BitsToDouble((long)q);
        if (q == (BigInteger.One << 53)) { q >>= 1; e++; }
        if (e > 1023) return double.PositiveInfinity;
        ulong bits = ((ulong)(e + 1023) << 52) | ((ulong)q - (1UL << 52));
        return BitConverter.UInt64BitsToDouble(bits);
    }
}

public static class Jcs
{
    public static string Number(double value)
    {
        Guard.Require(double.IsFinite(value), "DSL-LEX");
        if (value == 0) return "0";
        bool negative = value < 0;
        string r = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
        var parts = r.Split('e');
        int exponent = parts.Length == 2 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
        int dot = parts[0].IndexOf('.');
        int decimalPosition = (dot < 0 ? parts[0].Length : dot) + exponent;
        string digits = parts[0].Replace(".", "");
        while (digits.Length > 1 && digits[0] == '0') { digits = digits[1..]; decimalPosition--; }
        digits = digits.TrimEnd('0');
        string body;
        if (decimalPosition > 0 && decimalPosition <= 21)
            body = decimalPosition >= digits.Length ? digits + new string('0', decimalPosition - digits.Length) : digits.Insert(decimalPosition, ".");
        else if (decimalPosition <= 0 && decimalPosition > -6)
            body = "0." + new string('0', -decimalPosition) + digits;
        else
        {
            int e = decimalPosition - 1;
            body = digits[0] + (digits.Length > 1 ? "." + digits[1..] : "") + "e" + (e >= 0 ? "+" : "") + e.ToString(CultureInfo.InvariantCulture);
        }
        return (negative ? "-" : "") + body;
    }

    public static string Quote(string value)
    {
        var b = new StringBuilder("\"");
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsSurrogate(c))
            {
                Guard.Require(char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]), "DSL-LEX");
                b.Append(c).Append(value[++i]); continue;
            }
            b.Append(c switch { '"' => "\\\"", '\\' => "\\\\", '\b' => "\\b", '\t' => "\\t", '\n' => "\\n", '\f' => "\\f", '\r' => "\\r", < ' ' => "\\u" + ((int)c).ToString("x4"), _ => c.ToString() });
        }
        return b.Append('"').ToString();
    }

    public static string Write(object? value) => value switch
    {
        null => "null", string s => Quote(s), bool b => b ? "true" : "false",
        double d => Number(d), int i => i.ToString(CultureInfo.InvariantCulture), long l => l.ToString(CultureInfo.InvariantCulture),
        IDictionary<string, object?> map => "{" + string.Join(",", map.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Quote(x.Key) + ":" + Write(x.Value))) + "}",
        System.Collections.IEnumerable list => "[" + string.Join(",", list.Cast<object?>().Select(Write)) + "]",
        _ => throw new ArgumentException("Unsupported canonical value type")
    };
}

public static class Identity
{
    public static string Blake3(byte[] bytes) => global::Blake3.Hasher.Hash(bytes).ToString();
    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
