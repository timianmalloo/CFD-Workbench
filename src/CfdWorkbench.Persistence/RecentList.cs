using System.Text.Json.Serialization;

namespace CfdWorkbench.Persistence;

public sealed record RecentEntry(string Path);

public abstract record RecentOp
{
    public sealed record Add(string Path) : RecentOp;
    public sealed record Clear : RecentOp;
}

public sealed record RecentDocument(IReadOnlyList<RecentEntry> Entries)
{
    public const string FormatName = "cfdw-recent";
    public const int CurrentVersion = 1;
    [JsonPropertyOrder(-2)] public string Format => FormatName;
    [JsonPropertyOrder(-1)] public int Version => CurrentVersion;
}

/// <summary>Red stub. The recent-list codec lands after the named checks fail.</summary>
public static class RecentList
{
    public const int MaxEntries = 10;
    public sealed record RecentParse(IReadOnlyList<RecentEntry> Entries, IReadOnlyList<string> Codes, bool NeverWrite);
    public static bool Accepts(string? path) => throw new NotImplementedException();
    public static IReadOnlyList<RecentEntry> Apply(IReadOnlyList<RecentEntry> current, RecentOp op) => throw new NotImplementedException();
    public static RecentParse Parse(ReadOnlySpan<byte> bytes) => throw new NotImplementedException();
    public static byte[] Serialize(IReadOnlyList<RecentEntry> entries) => throw new NotImplementedException();
}
