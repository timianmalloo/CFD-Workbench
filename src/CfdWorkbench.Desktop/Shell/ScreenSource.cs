namespace CfdWorkbench.Desktop.Shell;

public sealed record ScreenArea(PxRect Bounds, PxRect WorkingArea, bool IsPrimary);

public interface IScreenSource
{
    IReadOnlyList<ScreenArea> All { get; }
    event EventHandler? Changed;
}

public sealed class FakeScreenSource : IScreenSource
{
    private IReadOnlyList<ScreenArea> screens;

    public FakeScreenSource(IReadOnlyList<ScreenArea> screens)
    {
        this.screens = screens;
    }

    public IReadOnlyList<ScreenArea> All => screens;

    public event EventHandler? Changed;

    public void UpdateScreens(IReadOnlyList<ScreenArea> updated)
    {
        screens = updated;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
