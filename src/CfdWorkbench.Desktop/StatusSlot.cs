namespace CfdWorkbench.Desktop;

/// <summary>
/// The controller's one status slot (STATUS-CLOBBER). Every write counts, and a background completion replaces only the
/// placeholder it wrote: its compare and its write happen under one lock, so a newer write on any thread always wins.
/// A lock, not the UI thread, is the one rule because the controller also runs with no Avalonia application (the
/// controller checks, the readiness ring), where there is no UI thread to marshal to.
/// </summary>
public sealed class StatusSlot(string text)
{
    private readonly object gate = new();
    private string text = text;
    private ReportKind kind;
    private long version;

    public string Text { get { lock (gate) return text; } }

    public ReportKind Kind { get { lock (gate) return kind; } }

    /// <summary>The write counter: it moves on every write and every supersede.</summary>
    public long Version { get { lock (gate) return version; } }

    /// <summary>Text, kind and version read together, so a reader never pairs one write's text with another's version.</summary>
    public (string Text, ReportKind Kind, long Version) Snapshot() { lock (gate) return (text, kind, version); }

    /// <summary>Writes a report and returns its version; a placeholder keeps that version to replace itself later.</summary>
    public long Write(string value, ReportKind reportKind = ReportKind.Info)
    {
        lock (gate)
        {
            text = value;
            kind = reportKind;
            return ++version;
        }
    }

    /// <summary>A report shown from outside the slot is newer than every write so far; returns the version it took.</summary>
    public long Supersede() { lock (gate) return ++version; }

    /// <summary>Replaces the placeholder written at <paramref name="placeholder"/> only if nothing was written since.</summary>
    public bool TryReplace(long placeholder, string value, ReportKind reportKind = ReportKind.Info)
    {
        lock (gate)
        {
            if (version != placeholder) return false;
            text = value;
            kind = reportKind;
            version++;
            return true;
        }
    }
}
