using System.Text;
using System.Text.Json;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.SolverSetup;

/// <summary>
/// Append-only JSONL store for Guided Solver Setup facts (docs/design/guided-solver-setup.md §2).
/// Every file read uses explicit <c>FileShare.Read | FileShare.Delete</c> (READER-SHARE compliant).
/// </summary>
public sealed class SetupFactStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string directoryPath;
    private readonly string filePath;

    public SetupFactStore(string? directoryPath = null)
    {
        this.directoryPath = directoryPath ?? DefaultStoreDirectory();
        filePath = Path.Combine(this.directoryPath, "setup-facts.jsonl");
    }

    public string DirectoryPath => directoryPath;
    public string FilePath => filePath;

    public static string DefaultStoreDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = Path.GetTempPath();
        return Path.Combine(localAppData, "CFD Workbench", "solver-setup");
    }

    public void Append(SetupFactEnvelope fact)
    {
        Directory.CreateDirectory(directoryPath);
        string line = JsonSerializer.Serialize(fact, JsonOptions) + "\n";
        byte[] bytes = Encoding.UTF8.GetBytes(line);
        using var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read | FileShare.Delete);
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush(flushToDisk: true);
    }

    public IReadOnlyList<SetupFactEnvelope> ReadAll()
    {
        if (!File.Exists(filePath)) return [];

        var list = new List<SetupFactEnvelope>();
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var env = JsonSerializer.Deserialize<SetupFactEnvelope>(line, JsonOptions);
                if (env is not null) list.Add(env);
            }
            catch (JsonException)
            {
                // A crash mid-write can leave a truncated trailing line; ignore it honestly so recovery proceeds.
            }
        }
        return list;
    }

    /// <summary>
    /// On relaunch: closes any open <see cref="StepOutcome.Started"/> attempt as <see cref="StepOutcome.Interrupted"/>
    /// so resume is a pure re-derivation (SETUP-05, R5).
    /// </summary>
    public IReadOnlyList<SetupFactEnvelope> RecoverInterruptedOnLaunch(DateTimeOffset? nowUtc = null)
    {
        var existing = ReadAll();
        var toClose = StepSelector.CloseInterrupted(existing, nowUtc ?? DateTimeOffset.UtcNow);
        if (toClose.Count == 0) return existing;

        var combined = new List<SetupFactEnvelope>(existing.Count + toClose.Count);
        combined.AddRange(existing);
        foreach (var closed in toClose)
        {
            Append(closed);
            combined.Add(closed);
        }
        return combined;
    }
}
