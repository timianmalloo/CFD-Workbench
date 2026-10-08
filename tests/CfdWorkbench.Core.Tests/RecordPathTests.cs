using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Ruling 157, track RWF: the record-write path. The New-project default is a bit golden (fast ring, under 0.1 s);
// the fitted-project families are tolerance checks that print a measured drift (docs/proof/rwf/red-first.md).
internal static partial class RecordPathTests
{
    private static readonly Regex Number = new(@"-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);

    internal static void Run()
    {
        Check("NewDefault_ControlPointDoubles_BitGolden", NewDefaultBitGolden);
    }

    // One row per curve: the curve's label, then the IEEE bits (hex) of every knot and control-point coordinate.
    // The text of a source prints each double with its exact decimal expansion, so parsing it back is lossless.
    internal static string BitRows(byte[] source)
    {
        var rows = new List<string>();
        string context = "";
        foreach (string line in Encoding.UTF8.GetString(source).ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("profile ", StringComparison.Ordinal)) context = trimmed.Split(' ')[1].Trim('"') + "/";
            int cv = trimmed.IndexOf(" cv {", StringComparison.Ordinal);
            int knots = trimmed.IndexOf("knots [", StringComparison.Ordinal);
            int ids = trimmed.IndexOf("] ids", StringComparison.Ordinal);
            if (cv < 0 || knots < 0 || ids < 0) continue;
            var bits = Number.Matches(trimmed[(knots + "knots [".Length)..ids])
                .Select(match => BitConverter.DoubleToInt64Bits(double.Parse(match.Value, CultureInfo.InvariantCulture)).ToString("X16", CultureInfo.InvariantCulture));
            rows.Add(context + trimmed[..cv] + " " + string.Join(" ", bits));
        }
        return string.Join("\n", rows);
    }

    private static void NewDefaultBitGolden()
    {
        string actual = BitRows(FoilSource.NewDefault());
        string golden = Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "record-path", "new-default-bits.txt");
        string recorded = File.Exists(golden) ? File.ReadAllText(golden).ReplaceLineEndings("\n").TrimEnd('\n') : "";
        Equal(recorded, actual);
    }
}
