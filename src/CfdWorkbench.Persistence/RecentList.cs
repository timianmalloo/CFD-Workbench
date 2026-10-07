using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CfdWorkbench.Persistence;

public sealed record RecentEntry(string Path);

public abstract record RecentOp
{
    public sealed record Add(string Path) : RecentOp;
    public sealed record Clear : RecentOp;
    public sealed record Remove(string Path) : RecentOp;
}

public sealed record RecentDocument(IReadOnlyList<RecentEntry> Entries)
{
    public const string FormatName = "cfdw-recent";
    public const int CurrentVersion = 1;
    [JsonPropertyOrder(-2)] public string Format => FormatName;
    [JsonPropertyOrder(-1)] public int Version => CurrentVersion;
}

/// <summary><c>cfdw-recent</c> version 1. At most 10 absolute paths, most recent first. Never throws.</summary>
public static class RecentList
{
    public const int MaxEntries = 10;
    public const int MaxPathBytes = 1024;
    public const int MaxBytes = 64 * 1024;

    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly JsonSerializerOptions Writer = Options(LayoutCodec.WriterMaxDepth);

    public sealed record RecentParse(IReadOnlyList<RecentEntry> Entries, IReadOnlyList<string> Codes, bool NeverWrite);

    public static bool Accepts(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Contains('\0') || !path.StartsWith('/')) return false;
        if (Encoding.UTF8.GetByteCount(path) > MaxPathBytes) return false;
        return path.EndsWith(".cfdw.json", StringComparison.Ordinal) || path.EndsWith(".foil", StringComparison.Ordinal);
    }

    public static IReadOnlyList<RecentEntry> Apply(IReadOnlyList<RecentEntry> current, RecentOp op)
    {
        if (op is RecentOp.Clear) return [];
        if (op is RecentOp.Remove remove)
            return current.Where(entry => !string.Equals(entry.Path, remove.Path, StringComparison.Ordinal)).ToArray();
        if (op is not RecentOp.Add add || !Accepts(add.Path)) return current;
        var list = new List<RecentEntry> { new(add.Path) };
        foreach (var entry in current)
            if (!string.Equals(entry.Path, add.Path, StringComparison.Ordinal)) list.Add(entry);
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        return list;
    }

    public static RecentParse Parse(ReadOnlySpan<byte> bytes)
    {
        try
        {
            bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var peek = LayoutCodec.Peek(bom ? bytes[3..] : bytes);
            if (!peek.Failed && peek.Format == RecentDocument.FormatName && peek.Version is > RecentDocument.CurrentVersion)
                return new RecentParse([], ["LAYOUT-VERSION"], true);
            if (bom || bytes.Length > MaxBytes || peek.Failed || peek.Format != RecentDocument.FormatName || peek.Version is null or < 1)
                return Schema();
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = LayoutCodec.ReaderMaxDepth });
            }
            catch (JsonException)
            {
                return Schema();
            }
            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return Schema();
                foreach (var property in root.EnumerateObject())
                    if (property.Name is not ("format" or "version" or "entries")) return Schema();
                if (!root.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array) return Schema();
                var kept = new List<RecentEntry>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                bool dropped = false;
                foreach (var element in entries.EnumerateArray())
                {
                    if (kept.Count == MaxEntries) break;
                    if (element.ValueKind != JsonValueKind.Object || element.EnumerateObject().Any(property => property.Name != "path")
                        || !element.TryGetProperty("path", out var path) || path.ValueKind != JsonValueKind.String
                        || !Accepts(path.GetString()) || path.GetString() is not string value || !seen.Add(value))
                    {
                        dropped = true;
                        continue;
                    }
                    kept.Add(new RecentEntry(value));
                }
                return new RecentParse(kept, dropped ? ["RECENT-SCHEMA"] : [], false);
            }
        }
        catch (Exception)
        {
            return Schema();
        }
    }

    public static byte[] Serialize(IReadOnlyList<RecentEntry> entries)
    {
        try
        {
            var capped = entries.Where(entry => Accepts(entry.Path)).Take(MaxEntries).ToArray();
            return JsonSerializer.SerializeToUtf8Bytes(new RecentDocument(capped), Writer);
        }
        catch (Exception)
        {
            return Utf8.GetBytes("{\"format\":\"cfdw-recent\",\"version\":1,\"entries\":[]}");
        }
    }

    private static RecentParse Schema() => new([], ["RECENT-SCHEMA"], false);

    private static JsonSerializerOptions Options(int depth)
    {
        var options = new JsonSerializerOptions(RecentJsonContext.Default.Options) { MaxDepth = depth };
        return options;
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    NewLine = "\n",
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(RecentDocument))]
internal partial class RecentJsonContext : JsonSerializerContext;
