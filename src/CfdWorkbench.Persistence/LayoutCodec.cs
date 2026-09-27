using System.Text.Json.Serialization;

namespace CfdWorkbench.Persistence;

/// <summary>Red stub. The codec lands after the named checks fail.</summary>
public static class LayoutCodec
{
    public const int MaxBytes = 64 * 1024;
    public const int ReaderMaxDepth = 8;
    public const int WriterMaxDepth = 9;

    public readonly record struct LayoutPeek(string? Format, int? Version, bool Failed);
    public sealed record LayoutParse(LayoutDocument Document, IReadOnlyList<string> Codes, int DroppedPanes, int Clamped, bool NeverWrite);

    public static LayoutPeek Peek(ReadOnlySpan<byte> bytes) => throw new NotImplementedException();
    public static LayoutParse Parse(ReadOnlySpan<byte> bytes, IReadOnlySet<string> registeredPanes) => throw new NotImplementedException();
    public static byte[] Serialize(LayoutDocument document) => throw new NotImplementedException();
    public static LayoutDocument Presets(WorkspaceId active, IReadOnlySet<string> registeredPanes) => throw new NotImplementedException();
    public static WorkspaceLayout Preset(WorkspaceId id, IReadOnlySet<string> registeredPanes) => throw new NotImplementedException();
    internal static bool TryWorkspace(string? name, out WorkspaceId id) => throw new NotImplementedException();
}
