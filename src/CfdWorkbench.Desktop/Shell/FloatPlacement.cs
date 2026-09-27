namespace CfdWorkbench.Desktop.Shell;

public enum RelocationKind
{
    Moved,
    DockedBack
}

public enum CornerPlacement
{
    TopRight,
    BottomRight,
    BottomLeft,
    TopLeft
}

public sealed record Relocation(
    string FloatId,
    RelocationKind Kind,
    PxRect Rect,
    CfdWorkbench.Persistence.FloatOrigin Origin,
    CornerPlacement? Corner,
    string ActivePane = "");

public static class FloatPlacement
{
    public static PxRect Clamp(PxRect rect, IReadOnlyList<ScreenArea> screens, PxRect? hint = null)
    {
        throw new NotImplementedException("track D1 stub");
    }

    public static IReadOnlyList<Relocation> Clear(PxRect target, IReadOnlyList<FloatFrame> floats, PxRect modelArea)
    {
        throw new NotImplementedException("track D1 stub");
    }
}
