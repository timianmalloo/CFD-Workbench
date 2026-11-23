using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.SolverSetup;

public sealed record CopyLintViolation(string Rule, string Snippet);

public sealed record CopyLintResult(bool Passed, IReadOnlyList<CopyLintViolation> Violations);

/// <summary>
/// Shared SETUP-04 no-command copy lint (docs/design/guided-solver-setup.md §7; Ruling 69).
/// Used identically by both the AI-11 assistant validator and the SETUP-04 UI copy tests.
/// Permits plain prose mentioning "WSL" and OS click paths; rejects code spans, prompt prefixes,
/// and shell-command invocations with flags or arguments.
/// </summary>
public static class SetupCopyLint
{
    private static readonly Regex CodeBlockOrSpan = new(@"```|`[^`\n]+`", RegexOptions.CultureInvariant);

    private static readonly Regex PromptPrefixedLine = new(
        @"(?m)^\s*(?:PS>|\$|#|C:\\>|>)[\t ]+\S",
        RegexOptions.CultureInvariant);

    // Command tokens followed by a flag (-x / --flag / /flag) or a known subcommand/path/URL argument.
    private static readonly Regex ShellCommandWithArgs = new(
        @"(?i)\b(?:wsl(?:\.exe)?|apt(?:-get)?|sudo|xattr|spctl|powershell(?:\.exe)?|pwsh|cmd(?:\.exe)?|bcdedit(?:\.exe)?|dism(?:\.exe)?|brew|curl|wget|chmod|chown|Set-ExecutionPolicy|reg(?:\.exe)?|msiexec|winget|choco)\b(?:\s+(?:--?[A-Za-z0-9]|/[A-Za-z]|https?://|install\b|update\b|upgrade\b|remove\b|uninstall\b|unregister\b|import\b|export\b|bypass\b|unrestricted\b|remotesigned\b|rm\b|mv\b|cp\b|ls\b|cat\b|echo\b|sh\b|bash\b|zsh\b|com\.apple\.quarantine\b|/[^\s]+))",
        RegexOptions.CultureInvariant);

    private static readonly Regex DirectFlaggedShell = new(
        @"(?i)\b(?:rm|mv|cp|mkdir)\s+-[A-Za-z]+",
        RegexOptions.CultureInvariant);

    public static CopyLintResult Lint(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new CopyLintResult(true, []);

        var violations = new List<CopyLintViolation>();

        foreach (Match m in CodeBlockOrSpan.Matches(text))
            violations.Add(new("code-span-or-block", m.Value));

        foreach (Match m in PromptPrefixedLine.Matches(text))
            violations.Add(new("prompt-prefixed-line", m.Value.Trim()));

        foreach (Match m in ShellCommandWithArgs.Matches(text))
            violations.Add(new("shell-command-invocation", m.Value));

        foreach (Match m in DirectFlaggedShell.Matches(text))
            violations.Add(new("shell-command-invocation", m.Value));

        return new CopyLintResult(violations.Count == 0, violations);
    }
}
