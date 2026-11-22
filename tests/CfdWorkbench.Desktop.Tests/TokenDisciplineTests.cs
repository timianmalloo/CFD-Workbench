using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Defect-class control UI-CS-TOKEN: static token, typography, and color discipline across all C# files in
/// src/CfdWorkbench.Desktop (excluding bin/ and obj/).
/// Enforces:
/// 1. Every string resource key passed to TryFindResource / FindResource / ResolveBrush / Resource / Brush /
///    GetResourceObservable resolves in Styles.axaml or App.axaml.
/// 2. Zero hardcoded FontSize = &lt;n&gt;, DrawText(..., &lt;n&gt;, ...), or FormattedText(..., &lt;n&gt;, ...) literals &lt; 11 px
///    (DESIGN.md DR-DEN-3: nothing below 11 px).
/// 3. Zero raw Color.Parse(...) hex literals in Desktop C# (empty allowlist; theme tokens live in Styles.axaml).
/// </summary>
public static class TokenDisciplineTests
{
    private static readonly Regex XamlKeyRegex = new(@"x:Key=""([^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ResourceLookupRegex = new(
        @"(?:\bTryFindResource|\bFindResource|\bResolveBrush|\bResource|\bBrush|\bGetResourceObservable)\s*\(\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ColorParseRegex = new(
        @"\bColor\.Parse\s*\([^)]*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FontSizeLiteralRegex = new(
        @"\bFontSize\s*=\s*([0-9]+(?:\.[0-9]+)?)[dfmDFM]?\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Matches DrawText(ctx, ..., <sizeLiteral>, FontWeight... / IBrush...) and FormattedText/target-typed new(..., FlowDirection..., typeface, <sizeLiteral>, brush)
    private static readonly Regex DrawTextSizeRegex = new(
        @"\bDrawText\s*\([^;\r\n]*?,\s*([0-9]+(?:\.[0-9]+)?)[dfmDFM]?\s*,\s*(?:FontWeight|IBrush|ink|muted|mute|seriesBrush|brush)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormattedTextSizeRegex = new(
        @"(?:new\s+FormattedText|new)\s*\([^;]*?FlowDirection\.LeftToRight\s*,[^;]*?,\s*([0-9]+(?:\.[0-9]+)?)[dfmDFM]?\s*,",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex BlockCommentRegex = new(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex LineCommentRegex = new(@"//[^\r\n]*", RegexOptions.Compiled);

    public static void Run()
    {
        DesktopChecks.Check("UiCsToken_StaticScan_AllDesktopCs_KeysResolve_FontAtLeast11_NoRawColorParse", ScanDesktopTree);
        DesktopChecks.Check("UiCsToken_Mutants_BadKey_SmallFont_DrawTextSmall_ColorParse_Refused", VerifyMutantsKilled);
    }

    public static IReadOnlySet<string> LoadKnownXamlKeys(string desktopDir)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in new[] { "Styles.axaml", "App.axaml" })
        {
            string path = Path.Combine(desktopDir, file);
            if (!File.Exists(path))
                throw new InvalidOperationException($"UI-CS-TOKEN: required token dictionary missing: {file}");
            string text = File.ReadAllText(path);
            foreach (Match m in XamlKeyRegex.Matches(text))
                keys.Add(m.Groups[1].Value);
        }
        return keys;
    }

    public static IReadOnlyList<string> ScanSource(string relativePath, string source, IReadOnlySet<string> knownKeys)
    {
        var findings = new List<string>();
        string stripped = StripComments(source);
        string[] lines = stripped.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int lineNo = i + 1;

            foreach (Match m in ResourceLookupRegex.Matches(line))
            {
                string key = m.Groups[1].Value;
                if (!knownKeys.Contains(key))
                    findings.Add($"{relativePath}:{lineNo} unresolved resource key \"{key}\" ({m.Value.Trim()})");
            }

            foreach (Match m in ColorParseRegex.Matches(line))
                findings.Add($"{relativePath}:{lineNo} raw Color.Parse not allowed in Desktop C# ({m.Value.Trim()})");

            foreach (Match m in FontSizeLiteralRegex.Matches(line))
            {
                if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double size) && size < 11.0)
                    findings.Add($"{relativePath}:{lineNo} FontSize {size} < 11 px ({m.Value.Trim()})");
            }

            foreach (Match m in DrawTextSizeRegex.Matches(line))
            {
                if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double size) && size < 11.0)
                    findings.Add($"{relativePath}:{lineNo} DrawText size {size} < 11 px ({m.Value.Trim()})");
            }
        }

        foreach (Match m in FormattedTextSizeRegex.Matches(stripped))
        {
            if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double size) && size < 11.0)
            {
                int lineNo = stripped[..m.Index].Count(c => c == '\n') + 1;
                findings.Add($"{relativePath}:{lineNo} FormattedText size {size} < 11 px");
            }
        }

        return findings;
    }

    private static string StripComments(string source)
    {
        // Preserve line count when removing block comments so reported line numbers remain accurate.
        string noBlocks = BlockCommentRegex.Replace(source, m => new string('\n', m.Value.Count(c => c == '\n')));
        // Strip // comments only outside quoted string literals.
        string[] lines = noBlocks.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            bool inString = false;
            for (int j = 0; j < line.Length - 1; j++)
            {
                if (line[j] == '"' && (j == 0 || line[j - 1] != '\\')) inString = !inString;
                else if (!inString && line[j] == '/' && line[j + 1] == '/')
                {
                    lines[i] = line[..j];
                    break;
                }
            }
        }
        return string.Join('\n', lines);
    }

    private static void ScanDesktopTree()
    {
        string desktopDir = Path.Combine(RepoRoot(), "src", "CfdWorkbench.Desktop");
        var knownKeys = LoadKnownXamlKeys(desktopDir);
        if (knownKeys.Count < 100)
            throw new InvalidOperationException($"UI-CS-TOKEN: expected >= 100 resource keys in Styles.axaml/App.axaml, found {knownKeys.Count}");

        var allFindings = new List<string>();
        int filesScanned = 0;
        foreach (string file in Directory.EnumerateFiles(desktopDir, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(desktopDir, file).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase) ||
                rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase) ||
                rel.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
                rel.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
                continue;
            filesScanned++;
            string src = File.ReadAllText(file);
            allFindings.AddRange(ScanSource(rel, src, knownKeys));
        }

        Console.WriteLine($"UI-CS-TOKEN scanned={filesScanned} keys={knownKeys.Count} findings={allFindings.Count}");
        if (filesScanned < 40)
            throw new InvalidOperationException($"UI-CS-TOKEN: unexpectedly low Desktop C# file count ({filesScanned})");
        if (allFindings.Count > 0)
            throw new InvalidOperationException($"UI-CS-TOKEN violations ({allFindings.Count}):\n  " + string.Join("\n  ", allFindings));
    }

    private static void VerifyMutantsKilled()
    {
        string desktopDir = Path.Combine(RepoRoot(), "src", "CfdWorkbench.Desktop");
        var knownKeys = LoadKnownXamlKeys(desktopDir);

        var badKey = ScanSource("Mutant.cs", "var b = this.TryFindResource(\"AccentBrush\", ActualThemeVariant, out var v);", knownKeys);
        if (badKey.Count != 1 || !badKey[0].Contains("AccentBrush", StringComparison.Ordinal))
            throw new InvalidOperationException("UI-CS-TOKEN mutant (bad key) escaped: " + string.Join(" | ", badKey));

        var smallFont = ScanSource("Mutant.cs", "var tb = new TextBlock { FontSize = 10 };", knownKeys);
        if (smallFont.Count != 1 || !smallFont[0].Contains("< 11", StringComparison.Ordinal))
            throw new InvalidOperationException("UI-CS-TOKEN mutant (FontSize=10) escaped");

        var drawTextSmall = ScanSource("Mutant.cs", "DrawText(context, label, 10, FontWeight.Normal, muted, pt);", knownKeys);
        if (drawTextSmall.Count != 1 || !drawTextSmall[0].Contains("< 11", StringComparison.Ordinal))
            throw new InvalidOperationException("UI-CS-TOKEN mutant (DrawText 10) escaped");

        var formattedSmall = ScanSource("Mutant.cs", "FormattedText T(string s, IBrush b) => new(s, Inv, FlowDirection.LeftToRight, Typeface.Default, 10, b);", knownKeys);
        if (formattedSmall.Count != 1 || !formattedSmall[0].Contains("< 11", StringComparison.Ordinal))
            throw new InvalidOperationException("UI-CS-TOKEN mutant (FormattedText 10) escaped");

        var colorParse = ScanSource("Mutant.cs", "var c = Color.Parse(\"#001261\");", knownKeys);
        if (colorParse.Count != 1 || !colorParse[0].Contains("Color.Parse", StringComparison.Ordinal))
            throw new InvalidOperationException("UI-CS-TOKEN mutant (Color.Parse) escaped");
    }

    private static string RepoRoot([CallerFilePath] string self = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", ".."));
}
