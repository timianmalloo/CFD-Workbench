using CfdWorkbench.Core;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>Where <c>ProjectStore.Supported()</c> is false (Windows until the native store lands, ProjectStore.cs:266) the view settings
/// last the session only (Ruling 206 O2, Ruling 207). A check that expects a restart to restore Text size, units or the comb asserts
/// that designed behaviour there instead. The host decides by asking the real store, never by an OS flag (TEST-FOREIGN-OS-BRANCH).</summary>
internal static class StoreSupport
{
    private static readonly Lazy<bool> supported = new(() =>
    {
        using var store = new ProjectStore();
        string code = store.SaveAsync(Path.Combine(TestTemp.NewDirectory("support-"), "probe"),
            new SaveRequest([1], null, Guid.NewGuid().ToString("D"))).GetAwaiter().GetResult().Code;
        if (code == "OK") return true;
        if (code != "DOC-UNSUPPORTED-PERSISTENCE") throw new InvalidOperationException("store support probe: " + code);
        // A macOS arm64 host that reports unsupported is a broken probe; throw rather than quietly take the Windows branch.
        if (OperatingSystem.IsMacOS() && System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
            throw new InvalidOperationException("the store reported unsupported on macOS arm64; the probe is wrong, not the host");
        Console.WriteLine("NOT ASSESSED view-settings persistence: this host has no project store (Ruling 206 O2); the session-only behaviour is asserted instead");
        return false;
    });

    /// <summary>The real store, as the shell builds it.</summary>
    internal static IProjectStore Real() => new ProjectStore();

    /// <summary>A store that cannot save or read, as <c>ProjectStore</c> is where it is unsupported.</summary>
    internal static IProjectStore Unsupported() => new UnsupportedStore();

    /// <summary>Runs <paramref name="persists"/> where the store works, and <paramref name="sessionOnly"/> (the designed behaviour) where it does not.</summary>
    internal static void OrSessionOnly(Action persists, Action sessionOnly)
    {
        if (supported.Value) persists();
        else sessionOnly();
    }

    private sealed class UnsupportedStore : IProjectStore
    {
        public void Dispose() { }
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default) =>
            Task.FromResult(new SaveResult("DOC-UNSUPPORTED-PERSISTENCE", null, false, false));
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) =>
            Task.FromException<ReadResult>(new ContractError("DOC-UNSUPPORTED-PERSISTENCE"));
    }
}
