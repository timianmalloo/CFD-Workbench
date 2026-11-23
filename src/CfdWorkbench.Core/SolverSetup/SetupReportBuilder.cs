using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.SolverSetup;

/// <summary>
/// Deterministic zero-PII support report builder (docs/design/guided-solver-setup.md §8, A8.5 credential negative test).
/// Strips user names, Windows/macOS/Linux home paths, machine SIDs, default hostnames, OpenFOAM host banners,
/// UNC host prefixes (preserving wsl.localhost), and API keys / bearer tokens.
/// </summary>
public static class SetupReportBuilder
{
    private static readonly Regex WinUserPath = new(
        @"(?:\b[A-Za-z]:|/mnt/[a-z])(?:\\\\|\\|/)+Users(?:\\\\|\\|/)+[^\\/\s""'`<>|*?:;,)\]}]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex UnixHomePath = new(
        @"/(?:Users|home)/[^/\s""'`<>|*?:;,)\]}]+",
        RegexOptions.CultureInvariant);

    private static readonly Regex MachineSid = new(
        @"S-1-5-21-\d+-\d+-\d+(?:-\d+)?",
        RegexOptions.CultureInvariant);

    private static readonly Regex DefaultWindowsHost = new(
        @"(?<![A-Za-z0-9-])(?:DESKTOP|LAPTOP)-[A-Z0-9]{7}(?![A-Za-z0-9])|(?<![A-Za-z0-9-])WIN-[A-Z0-9]{11}(?![A-Za-z0-9])",
        RegexOptions.CultureInvariant);

    private static readonly Regex ComputerNameEnv = new(
        @"(COMPUTERNAME[""']?\s*[=:]\s*[""']?)([A-Za-z0-9][^\s""'`;,\\]*)",
        RegexOptions.CultureInvariant);

    private static readonly Regex HostNameLine = new(
        @"(?m)^([ \t]*Host Name:[ \t]*)(\S[^\r\n]*)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex FoamBannerHost = new(
        @"(?m)^(Host {2,}:[ \t]*)(\S+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex UncHost = new(
        @"\\\\(?!wsl\.localhost\\|localhost\\|\.\\|\?\\)([A-Za-z0-9_][\w.-]+)\\",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ApiSecretTokens = new(
        @"\b(?:sk-[A-Za-z0-9_-]{8,}|AIza[A-Za-z0-9_-]{10,}|ghp_[A-Za-z0-9]{10,}|gho_[A-Za-z0-9]{10,}|xox[baprs]-[A-Za-z0-9-]{10,})\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex BearerHeader = new(
        @"(?i)\b(Bearer\s+)([A-Za-z0-9._~+/-]+=*)",
        RegexOptions.CultureInvariant);

    private static readonly Regex KeyValueSecret = new(
        @"(?i)\b((?:api[_-]?key|secret|token|password)\s*[=:]\s*[""']?)([^\s""',;]+)",
        RegexOptions.CultureInvariant);

    public static string Redact(
        string? text,
        string? currentUserName = null,
        string? currentMachineName = null,
        IReadOnlyList<string>? extraSecrets = null)
    {
        if (string.IsNullOrEmpty(text)) return "";

        string s = text;
        s = WinUserPath.Replace(s, "%USERPROFILE%");
        s = UnixHomePath.Replace(s, "$HOME");
        s = MachineSid.Replace(s, "S-1-5-21-<machine>-<RID>");
        s = DefaultWindowsHost.Replace(s, "<host>");
        s = ComputerNameEnv.Replace(s, "$1<host>");
        s = HostNameLine.Replace(s, "$1<host>");
        s = FoamBannerHost.Replace(s, "$1<host>");
        s = UncHost.Replace(s, @"\\<host>\");
        s = ApiSecretTokens.Replace(s, "<redacted-key>");
        s = BearerHeader.Replace(s, "$1<redacted-token>");
        s = KeyValueSecret.Replace(s, "$1<redacted>");

        if (!string.IsNullOrWhiteSpace(currentUserName) && currentUserName.Length >= 3)
        {
            s = Regex.Replace(s, Regex.Escape(currentUserName), "<user>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        if (!string.IsNullOrWhiteSpace(currentMachineName) && currentMachineName.Length >= 4 &&
            !currentMachineName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            s = Regex.Replace(s, Regex.Escape(currentMachineName), "<host>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        if (extraSecrets is not null)
        {
            foreach (string secret in extraSecrets)
            {
                if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 4)
                {
                    s = Regex.Replace(s, Regex.Escape(secret), "<redacted>",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
            }
        }

        return s;
    }

    public static string BuildReport(
        string appVersion,
        RouteDefinition? route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? currentUserName = null,
        string? currentMachineName = null,
        IReadOnlyList<string>? extraSecrets = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CFD Workbench Solver Setup Report (redacted)");
        sb.Append("App version: ").AppendLine(appVersion);
        sb.Append("Route: ").AppendLine(route is null ? "none" : $"{route.Id} ({route.Version})");

        if (survey is not null)
        {
            sb.AppendLine("Survey:");
            sb.Append("  OS: ").Append(survey.Os).Append(" (").Append(survey.OsName)
              .Append(", build ").Append(survey.OsBuild.ToString(CultureInfo.InvariantCulture))
              .Append(", arch ").Append(survey.Arch).AppendLine(")");
            sb.Append("  Free disk: ").AppendLine(survey.FreeDiskBytes is { } fb
                ? string.Create(CultureInfo.InvariantCulture, $"{fb / 1_073_741_824.0:F2} GB")
                : "Not recorded");
            sb.Append("  Reboot pending: ").AppendLine(FormatBool(survey.RebootPending));
            sb.Append("  Virtualization firmware: ").AppendLine(FormatBool(survey.VirtualizationFirmwareEnabled));
            sb.Append("  Hypervisor present: ").AppendLine(FormatBool(survey.HypervisorPresent));
            sb.Append("  WSL available: ").AppendLine(FormatBool(survey.WslAvailable));
            sb.Append("  WSL distros: ").AppendLine(survey.WslDistros.Count == 0
                ? "none"
                : string.Join(", ", survey.WslDistros.Select(d => $"{d.Name}({d.State},v{d.Version})")));
            sb.Append("  Survey duration ms: ").AppendLine(survey.DurationMs.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            sb.AppendLine("Survey: Not recorded");
        }

        sb.AppendLine("Steps:");
        int stepCount = 0;
        foreach (var env in facts)
        {
            if (env.Kind != SetupFactEnvelope.KindStep || env.Step is not { } step) continue;
            stepCount++;
            sb.Append("  - ").Append(step.StepId).Append(" [").Append(step.Outcome).Append(']');
            if (!string.IsNullOrWhiteSpace(step.CauseCode)) sb.Append(" cause=").Append(step.CauseCode);
            if (step.ExitCode is { } code) sb.Append(" exit=").Append(code.ToString(CultureInfo.InvariantCulture));
            if (step.DurationMs is { } ms) sb.Append(" ms=").Append(ms.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(step.OutputExcerptRedacted))
            {
                sb.AppendLine("    excerpt:");
                foreach (string line in step.OutputExcerptRedacted.Split('\n'))
                    sb.Append("      ").AppendLine(line.TrimEnd('\r'));
            }
        }
        if (stepCount == 0) sb.AppendLine("  (none)");

        sb.AppendLine("Backend checks:");
        int checkCount = 0;
        foreach (var env in facts)
        {
            if (env.Kind != SetupFactEnvelope.KindCheck || env.Check is not { } check) continue;
            checkCount++;
            sb.Append("  - ").Append(check.Kind).Append(" passed=").Append(check.Passed)
              .Append(" verified=").Append(check.VerifiedInstall)
              .Append(" banner=").Append(check.BannerStatus ?? "Not recorded")
              .Append(" build=").Append(check.BuildId ?? "Not recorded");
            if (check.SmokeScalarValue is { } sv)
                sb.Append(" scalar=").Append(sv.ToString("G6", CultureInfo.InvariantCulture));
            sb.AppendLine();
        }
        if (checkCount == 0) sb.AppendLine("  (none)");

        return Redact(sb.ToString(), currentUserName, currentMachineName, extraSecrets);
    }

    private static string FormatBool(bool? value) =>
        value is { } b ? (b ? "yes" : "no") : "Not recorded";
}
