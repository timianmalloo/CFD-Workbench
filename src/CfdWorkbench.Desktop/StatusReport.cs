namespace CfdWorkbench.Desktop;

/// <summary>How the status strip draws a report (DR-STATUS-1): its icon, its colour and, for a warning or error, its rail.</summary>
public enum ReportKind { Info, Warning, Error }

/// <summary>
/// One report for the status strip, the window's one polite status region (docs/reviews/ui-status-bar.md §2.3).
/// <paramref name="Toast"/> also opens the warning toast: only a warning that results from a commit sets it.
/// </summary>
public sealed record StatusReport(string Text, ReportKind Kind = ReportKind.Info, bool Toast = false);
