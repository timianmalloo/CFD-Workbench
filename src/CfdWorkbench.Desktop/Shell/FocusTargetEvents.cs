namespace CfdWorkbench.Desktop.Shell;

public sealed class FocusedTargetEventArgs : EventArgs
{
    public FocusedTargetEventArgs(PxRect bounds, string accessibleName)
    {
        Bounds = bounds;
        AccessibleName = accessibleName;
    }

    public PxRect Bounds { get; }
    public string AccessibleName { get; }
}

public delegate void FocusedTargetChangedEventHandler(object? sender, FocusedTargetEventArgs e);
