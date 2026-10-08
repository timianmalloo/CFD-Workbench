using System.Collections.Concurrent;

namespace CfdWorkbench.Core;

public static class CrtProbe
{
    private static readonly ConcurrentDictionary<string, long> Hits = new();
    private static int registered;

    public static double Up(string tag, double v)
    {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                string? path = Environment.GetEnvironmentVariable("CRT_PROBE_OUT");
                if (path is null) return;
                File.AppendAllLines(path, Hits.Select(pair => $"{pair.Key}\t{pair.Value}"));
            };
        Hits.AddOrUpdate(tag, 1, (_, n) => n + 1);
        return v is 0 or 1 or -1 || !double.IsFinite(v) ? v : double.BitIncrement(v);
    }
}
