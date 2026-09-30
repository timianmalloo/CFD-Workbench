namespace CfdWorkbench.Desktop.Shell;

public sealed record ShellEvent(
    long Sequence,
    string Name,
    string Outcome,
    double DurationMilliseconds,
    string TraceId,
    string? Code = null,
    int? Bytes = null,
    int? DroppedCount = null,
    int? ClampedCount = null,
    string? Trigger = null,
    string? Pane = null,
    string? From = null,
    string? To = null,
    string? Corner = null,
    string? ExceptionType = null,
    bool? PublicationKnown = null,
    bool? DurabilityConfirmed = null,
    bool? Retried = null,
    int? Frames = null,
    double? UpdateP95Ms = null,
    double? EstimatesP95Ms = null,
    double? RenderP95Ms = null,
    double? CommitMs = null,
    string? EditKind = null,
    string? OperationId = null);

public static class ShellEvents
{
    private static readonly object sync = new();
    private static readonly Queue<ShellEvent> ring = new(256);
    private static long sequence;

    public static void Record(ShellEvent ev)
    {
        lock (sync)
        {
            if (ring.Count >= 256) ring.Dequeue();
            ring.Enqueue(ev);
        }
    }

    public static void Record(
        string name,
        string outcome,
        double durationMs,
        string traceId,
        string? code = null,
        int? bytes = null,
        int? droppedCount = null,
        int? clampedCount = null,
        string? trigger = null,
        string? pane = null,
        string? from = null,
        string? to = null,
        string? corner = null,
        string? exceptionType = null,
        bool? publicationKnown = null,
        bool? durabilityConfirmed = null,
        bool? retried = null,
        int? frames = null,
        double? updateP95Ms = null,
        double? estimatesP95Ms = null,
        double? renderP95Ms = null,
        double? commitMs = null,
        string? editKind = null,
        string? operationId = null)
    {
        lock (sync)
        {
            long seq = sequence++;
            Record(new ShellEvent(
                seq, name, outcome, durationMs, traceId, code, bytes, droppedCount, clampedCount,
                trigger, pane, from, to, corner, exceptionType, publicationKnown, durabilityConfirmed, retried,
                frames, updateP95Ms, estimatesP95Ms, renderP95Ms, commitMs, editKind, operationId));
        }
    }

    public static IReadOnlyList<ShellEvent> Read()
    {
        lock (sync)
        {
            return ring.ToArray();
        }
    }

    public static void Clear()
    {
        lock (sync)
        {
            ring.Clear();
            sequence = 0;
        }
    }
}
