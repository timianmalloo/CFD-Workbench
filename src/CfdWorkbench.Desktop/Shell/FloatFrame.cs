namespace CfdWorkbench.Desktop.Shell;

public readonly record struct PxPoint(int X, int Y);

public readonly record struct PxSize(int Width, int Height);

public readonly record struct DipSize(double Width, double Height);

public readonly record struct PxRect(int X, int Y, int Width, int Height)
{
    public static readonly PxRect Empty = new(0, 0, 0, 0);

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public double CenterX => X + Width / 2.0;
    public double CenterY => Y + Height / 2.0;
    public long Area => (long)Math.Max(0, Width) * Math.Max(0, Height);

    public bool Intersects(PxRect other) =>
        !IsEmpty && !other.IsEmpty && X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public bool Contains(PxRect other) =>
        !IsEmpty && !other.IsEmpty && other.X >= X && other.Right <= Right && other.Y >= Y && other.Bottom <= Bottom;

    public PxRect Intersect(PxRect other)
    {
        if (!Intersects(other)) return Empty;
        int x1 = Math.Max(X, other.X);
        int y1 = Math.Max(Y, other.Y);
        int x2 = Math.Min(Right, other.Right);
        int y2 = Math.Min(Bottom, other.Bottom);
        return new PxRect(x1, y1, x2 - x1, y2 - y1);
    }
}

public sealed record FloatFrame(string Id, PxRect Rect, CfdWorkbench.Persistence.FloatOrigin Origin, string ActivePane = "")
{
    public static PxRect From(int x, int y, double widthDip, double heightDip, double desktopScaling)
    {
        throw new NotImplementedException("track D1 stub");
    }

    public static PxRect From(PxPoint position, DipSize frameSizeDip, double desktopScaling) =>
        From(position.X, position.Y, frameSizeDip.Width, frameSizeDip.Height, desktopScaling);

    public static double ToDipWidth(int widthPx, double desktopScaling)
    {
        throw new NotImplementedException("track D1 stub");
    }

    public static double ToDipHeight(int heightPx, double desktopScaling)
    {
        throw new NotImplementedException("track D1 stub");
    }
}
