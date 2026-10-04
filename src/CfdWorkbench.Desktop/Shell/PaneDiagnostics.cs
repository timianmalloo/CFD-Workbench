using System.Text;

namespace CfdWorkbench.Desktop.Shell;

/// <summary>
/// A native-only trace of pane binds and shell refreshes, off unless <c>CFDW_DIAG_PANES</c> names a file. Each line is
/// appended and flushed on its own, so a frozen or killed app still leaves every line it wrote. It never throws.
/// simplify: a diagnostic for the blank-Properties report (fix/m12c-properties-blank) that the harness could not reproduce;
/// the ceiling is one investigation, and the trigger to remove it is the cause being found.
/// </summary>
public static class PaneDiagnostics
{
    public const string Variable = "CFDW_DIAG_PANES";

    private static readonly object sync = new();
    private static readonly string? path = PathFromEnvironment();

    public static bool Enabled => path is not null;

    /// <summary>Appends one line (prefixed with the Unix time in ms); the line is built only when the trace is on.</summary>
    public static void Write(Func<string> line)
    {
        if (path is null) return;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()} {line()}{Environment.NewLine}");
            lock (sync)
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
        }
        catch (Exception)
        {
            // A diagnostic must never change what the app does: a failed write (or a failed line) is dropped.
        }
    }

    /// <summary>The selection's kind and ids, on one line.</summary>
    public static string Describe(Selection selection) => selection switch
    {
        Selection.Foil => "foil",
        Selection.Station station => $"station:{station.Index}@{station.Eta:R}",
        Selection.Points points => "points:" + string.Join(",", points.Items.Select(item => $"{item.Curve}/{item.VertexId}/{item.Profile ?? "-"}")),
        _ => selection.GetType().Name
    };

    /// <summary>An exception with its message and stack, on one line.</summary>
    public static string Describe(Exception error) =>
        $"exception={error.GetType().FullName} message=\"{error.Message}\" stack=\"{error.StackTrace?.ReplaceLineEndings(" | ")}\"";

    private static string? PathFromEnvironment()
    {
        try { return Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } value ? value : null; }
        catch (Exception) { return null; }
    }
}
