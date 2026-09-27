namespace CfdWorkbench.Desktop.Shell;

public enum FocusRegionKind
{
    Left,
    ModelArea,
    Bottom,
    Right,
    Float
}

public sealed record FocusRegion(string Id, FocusRegionKind Kind, bool Available);

public static class FocusRing
{
    public static int NextRegionIndex(int current, bool reverse, IReadOnlyList<bool> available)
    {
        if (available == null || available.Count == 0) return -1;
        int first = current < 0 ? (reverse ? available.Count - 1 : 0)
            : (current + (reverse ? -1 : 1) + available.Count) % available.Count;
        for (int offset = 0; offset < available.Count; offset++)
        {
            int index = (first + (reverse ? -offset : offset) + available.Count) % available.Count;
            if (available[index]) return index;
        }
        return -1;
    }

    public static int NextRegion(int current, bool reverse, IReadOnlyList<FocusRegion> regions)
    {
        if (regions == null || regions.Count == 0) return -1;
        var available = regions.Select(r => r.Available).ToArray();
        return NextRegionIndex(current, reverse, available);
    }
}
