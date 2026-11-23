using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.SolverSetup;

public sealed record AssistantValidationContext(
    IReadOnlySet<string> AllowedNextStepIds,
    IReadOnlyDictionary<int, string> ExcerptLines,
    IReadOnlySet<string> KnownKnowledgeIds,
    string SharedContextText,
    bool CauseIsUnknown,
    string FallbackFixedExplanation);

public sealed record AssistantValidationResult(
    AssistantVerdict Verdict,
    string Reason,
    string Message,
    string DisplayText,
    string? SuggestedStepId,
    IReadOnlyList<string> Citations);

public sealed record AssistantOfflineStatus(
    bool Available,
    bool RailsUnaffected,
    string Notice);

/// <summary>
/// T4 deterministic T0 authority for Guided Solver Setup assistant responses (AI-11 amended, AI-01, AI-03, SETUP-04).
/// Strictly validates `{ kind: "explanation", text, citations[] }` and `{ kind: "environment-step", stepId }`:
/// refuses extra fields and disallowed step ids; withholds unresolved citations, command lint hits,
/// success claims about this machine, OS-protection weakening verbs, and unsupported numerals.
/// Works 100% key-free and offline.
/// </summary>
public static class SetupAssistantValidator
{
    private static readonly Regex LineCitation = new(@"^line:(\d+)$", RegexOptions.CultureInvariant);
    private static readonly Regex LinesRangeCitation = new(@"^lines:(\d+)-(\d+)$", RegexOptions.CultureInvariant);

    private static readonly Regex SuccessClaimRegex = new(
        @"(?i)\b(?:(?:is|are|now|already|setup\s+is|solver\s+is|openfoam\s+is|su2\s+is|problem\s+is|it\s+is)\s+(?:ready|installed|fixed|done|working)|setup\s+worked|you\s+are\s+ready|all\s+done)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex NegatedSuccessRegex = new(
        @"(?i)\b(?:not|never|isn't|is\s+not|aren't|are\s+not|until|before|once)\s+(?:yet\s+)?(?:ready|installed|fixed|done|working)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex ProtectionTargetRegex = new(
        @"(?i)\b(?:gatekeeper|smartscreen|defender|antivirus|anti-virus|windows\s+security|secure\s+boot|quarantine|firewall)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex WeakeningVerbRegex = new(
        @"(?i)\b(?:disable|turn\s+off|switch\s+off|bypass|strip|remove|skip|exclude|whitelist|unblock|allow\s+anyway)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex NumeralTokenRegex = new(
        @"(?<![A-Za-z0-9_.-])\d+(?:\.\d+)?(?![A-Za-z0-9_.-])",
        RegexOptions.CultureInvariant);

    public static AssistantOfflineStatus OfflineStatus(bool hasApiKey) => hasApiKey
        ? new AssistantOfflineStatus(true, true, "Assistant available. Setup works the same without it.")
        : new AssistantOfflineStatus(false, true, "No API key configured — every setup step, known error, and Copy a report work without it.");

    public static AssistantValidationResult Validate(string rawJson, AssistantValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return Refused("invalid-json", "Step refused: outside the allow-list — empty response.", context);

        JsonDocument doc;
        try { doc = JsonDocument.Parse(rawJson); }
        catch (JsonException)
        {
            return Refused("invalid-json", "Step refused: outside the allow-list — malformed JSON.", context);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Refused("invalid-schema", "Step refused: outside the allow-list — root must be an object.", context);

            if (!root.TryGetProperty("kind", out var kindEl) || kindEl.ValueKind != JsonValueKind.String)
                return Refused("missing-kind", "Step refused: outside the allow-list — kind is required.", context);

            string kind = kindEl.GetString()!;
            return kind switch
            {
                "environment-step" => ValidateEnvironmentStep(root, context),
                "explanation" => ValidateExplanation(root, context),
                _ => Refused("unknown-kind", $"Step refused: outside the allow-list — unknown kind '{kind}'.", context)
            };
        }
    }

    private static AssistantValidationResult ValidateEnvironmentStep(JsonElement root, AssistantValidationContext context)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name is not ("kind" or "stepId"))
                return Refused("extra-field:" + prop.Name,
                    $"Step refused: outside the allow-list — extra field '{prop.Name}' is not allowed.", context);
        }

        if (!root.TryGetProperty("stepId", out var stepEl) || stepEl.ValueKind != JsonValueKind.String)
            return Refused("missing-step-id", "Step refused: outside the allow-list — stepId must be a string.", context);

        string stepId = stepEl.GetString()!;
        if (string.IsNullOrWhiteSpace(stepId) || !context.AllowedNextStepIds.Contains(stepId))
            return Refused("step-not-allowed",
                $"Step refused: outside the allow-list — step '{stepId}' is not allowed in this state.", context);

        return new AssistantValidationResult(
            AssistantVerdict.Shown,
            "ok",
            $"Assistant suggested step '{stepId}'.",
            $"Assistant suggestion: {stepId}",
            stepId,
            []);
    }

    private static AssistantValidationResult ValidateExplanation(JsonElement root, AssistantValidationContext context)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name is not ("kind" or "text" or "citations"))
                return Refused("extra-field:" + prop.Name,
                    $"Step refused: outside the allow-list — extra field '{prop.Name}' is not allowed.", context);
        }

        if (!root.TryGetProperty("text", out var textEl) || textEl.ValueKind != JsonValueKind.String)
            return Refused("missing-text", "Step refused: outside the allow-list — text is required.", context);

        string text = textEl.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return Withheld("empty-text", "Response withheld: empty explanation.", context);

        if (!root.TryGetProperty("citations", out var citEl) || citEl.ValueKind != JsonValueKind.Array)
            return Withheld("unresolved-citation", "Response withheld: citations array is required.", context);

        var citations = new List<string>();
        var citedLineNumbers = new HashSet<int>();
        foreach (var item in citEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                return Withheld("unresolved-citation", "Response withheld: citation must be a string.", context);
            string cid = item.GetString() ?? "";
            if (!TryResolveCitation(cid, context, citedLineNumbers))
                return Withheld("unresolved-citation", $"Response withheld: citation '{cid}' does not resolve.", context);
            citations.Add(cid);
        }

        if (citations.Count == 0)
            return Withheld("unresolved-citation", "Response withheld: at least one citation is required.", context);

        // Shared SETUP-04 no-command lint.
        var lint = SetupCopyLint.Lint(text);
        if (!lint.Passed)
            return Withheld("command-to-type", "Response withheld: it told you to type a command.", context);

        // No success claims about this machine (R4).
        if (ContainsSuccessClaim(text))
            return Withheld("claimed-success", "Response withheld: claimed setup succeeded without a verified check.", context);

        // No OS protection weakening verbs (M8 / AI-11).
        if (ContainsWeakeningAdvice(text))
            return Withheld("weakens-os-protection", "Response withheld: suggested weakening OS protection.", context);

        // AI-03: numerals in explanation must appear in shared context, cited excerpts, or cited line numbers.
        if (FindUnsupportedNumeral(text, context, citedLineNumbers) is { } badNum)
            return Withheld("unsupported-numeral", $"Response withheld: numeral '{badNum}' is not supported by the shared context.", context);

        return new AssistantValidationResult(
            AssistantVerdict.Shown,
            "ok",
            "Explanation verified.",
            text,
            null,
            citations);
    }

    private static bool TryResolveCitation(string citation, AssistantValidationContext context, HashSet<int> citedLines)
    {
        if (string.IsNullOrWhiteSpace(citation)) return false;

        var single = LineCitation.Match(citation);
        if (single.Success && int.TryParse(single.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int lineNum))
        {
            if (!context.ExcerptLines.ContainsKey(lineNum)) return false;
            citedLines.Add(lineNum);
            return true;
        }

        var range = LinesRangeCitation.Match(citation);
        if (range.Success &&
            int.TryParse(range.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int start) &&
            int.TryParse(range.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int end) &&
            start <= end && end - start <= 100)
        {
            for (int n = start; n <= end; n++)
            {
                if (!context.ExcerptLines.ContainsKey(n)) return false;
                citedLines.Add(n);
            }
            return true;
        }

        return context.KnownKnowledgeIds.Contains(citation);
    }

    private static bool ContainsSuccessClaim(string text)
    {
        foreach (Match m in SuccessClaimRegex.Matches(text))
        {
            int windowStart = Math.Max(0, m.Index - 20);
            string window = text.Substring(windowStart, Math.Min(text.Length - windowStart, m.Length + 25));
            if (!NegatedSuccessRegex.IsMatch(window))
                return true;
        }
        return false;
    }

    private static bool ContainsWeakeningAdvice(string text) =>
        ProtectionTargetRegex.IsMatch(text) && WeakeningVerbRegex.IsMatch(text);

    private static string? FindUnsupportedNumeral(string text, AssistantValidationContext context, HashSet<int> citedLines)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in NumeralTokenRegex.Matches(context.SharedContextText ?? ""))
            allowed.Add(m.Value);
        foreach (var kv in context.ExcerptLines)
        {
            allowed.Add(kv.Key.ToString(CultureInfo.InvariantCulture));
            foreach (Match m in NumeralTokenRegex.Matches(kv.Value))
                allowed.Add(m.Value);
        }
        foreach (int line in citedLines)
            allowed.Add(line.ToString(CultureInfo.InvariantCulture));

        foreach (Match m in NumeralTokenRegex.Matches(text))
        {
            if (!allowed.Contains(m.Value))
                return m.Value;
        }
        return null;
    }

    private static AssistantValidationResult Refused(string reason, string message, AssistantValidationContext context) =>
        new(AssistantVerdict.Refused, reason, message, context.FallbackFixedExplanation, null, []);

    private static AssistantValidationResult Withheld(string reason, string message, AssistantValidationContext context) =>
        new(AssistantVerdict.Withheld, reason, message, context.FallbackFixedExplanation, null, []);
}
