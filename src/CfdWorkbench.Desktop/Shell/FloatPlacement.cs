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
        if (screens == null || screens.Count == 0) return rect;

        // 1. screen whose bounds equal hint
        ScreenArea? targetScreen = null;
        if (hint.HasValue)
        {
            targetScreen = screens.FirstOrDefault(s => s.Bounds == hint.Value);
        }

        // 2. else largest intersection
        if (targetScreen == null)
        {
            long bestArea = 0;
            foreach (var screen in screens)
            {
                var isect = rect.Intersect(screen.WorkingArea);
                long area = isect.Area;
                if (area > bestArea)
                {
                    bestArea = area;
                    targetScreen = screen;
                }
            }
        }

        // 3. else primary (or first)
        targetScreen ??= screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

        var wa = targetScreen.WorkingArea;
        int w = Math.Min(rect.Width, wa.Width);
        int h = Math.Min(rect.Height, wa.Height);
        int x = rect.X;
        int y = rect.Y;

        if (x < wa.X) x = wa.X;
        if (x + w > wa.Right) x = wa.Right - w;
        if (y < wa.Y) y = wa.Y;
        if (y + h > wa.Bottom) y = wa.Bottom - h;

        return new PxRect(x, y, w, h);
    }

    public static IReadOnlyList<Relocation> Clear(PxRect target, IReadOnlyList<FloatFrame> floats, PxRect modelArea)
    {
        var relocations = new List<Relocation>();
        if (floats == null || floats.Count == 0) return relocations;

        var occupiedRects = new List<PxRect>();
        foreach (var fl in floats)
        {
            if (!fl.Rect.Intersects(target))
            {
                occupiedRects.Add(fl.Rect);
            }
        }

        foreach (var fl in floats)
        {
            if (!fl.Rect.Intersects(target)) continue;

            int w = fl.Rect.Width;
            int h = fl.Rect.Height;

            var candidateCorners = new (CornerPlacement Corner, PxRect Rect)[]
            {
                (CornerPlacement.TopRight, new PxRect(modelArea.Right - 8 - w, modelArea.Y + 8, w, h)),
                (CornerPlacement.BottomRight, new PxRect(modelArea.Right - 8 - w, modelArea.Bottom - 8 - h, w, h)),
                (CornerPlacement.BottomLeft, new PxRect(modelArea.X + 8, modelArea.Bottom - 8 - h, w, h)),
                (CornerPlacement.TopLeft, new PxRect(modelArea.X + 8, modelArea.Y + 8, w, h))
            };

            (CornerPlacement Corner, PxRect Rect)? best = null;
            double bestDistSq = double.MaxValue;

            double fcx = fl.Rect.CenterX;
            double fcy = fl.Rect.CenterY;

            foreach (var cand in candidateCorners)
            {
                if (!modelArea.Contains(cand.Rect)) continue;
                if (cand.Rect.Intersects(target)) continue;
                if (occupiedRects.Any(occ => cand.Rect.Intersects(occ))) continue;

                double dx = cand.Rect.CenterX - fcx;
                double dy = cand.Rect.CenterY - fcy;
                double distSq = dx * dx + dy * dy;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = cand;
                }
            }

            if (best.HasValue)
            {
                occupiedRects.Add(best.Value.Rect);
                relocations.Add(new Relocation(fl.Id, RelocationKind.Moved, best.Value.Rect, fl.Origin, best.Value.Corner, fl.ActivePane));
            }
            else
            {
                relocations.Add(new Relocation(fl.Id, RelocationKind.DockedBack, fl.Rect, fl.Origin, null, fl.ActivePane));
            }
        }

        return relocations;
    }
}
