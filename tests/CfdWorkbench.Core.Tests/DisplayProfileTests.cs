using System.Buffers.Binary;
using System.Security.Cryptography;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// M1.2c display profile (docs/design/m12c-section-editor.md §12.4, track DSP).
internal static class DisplayProfileTests
{
    internal static void Run()
    {
        Check("Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged", SurfaceBitsUnchanged);
    }

    internal static void RunReadiness() { }

    // Captured before ProfileEvaluator left Placement's private methods. A bit change in the placed
    // mesh fails here even when the certificate golden stays green.
    private static void SurfaceBitsUnchanged()
    {
        string root = PlacementTests.RepoRoot();
        string folder = Path.Combine(root, "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2");
        string golden = Path.Combine(root, "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12c", "display", "placement-surface-bits.txt");
        var lines = new List<string>();
        foreach (string path in Directory.EnumerateFiles(folder, "*.foil").Order(StringComparer.Ordinal))
        {
            var view = Placement.Surface(File.ReadAllBytes(path), "accepted", 0, CancellationToken.None);
            lines.Add(Path.GetFileName(path) + " " + Hash(view));
        }
        Equal(string.Join("\n", File.ReadAllLines(golden)), string.Join("\n", lines));
    }

    private static string Hash(SurfaceView view)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buf = new byte[8];
        void D(double value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buf, BitConverter.DoubleToInt64Bits(value));
            sha.AppendData(buf);
        }
        D(view.MinimumX); D(view.MinimumY); D(view.MinimumZ);
        D(view.MaximumX); D(view.MaximumY); D(view.MaximumZ);
        foreach (var section in view.Sections)
        {
            D(section.Eta);
            BinaryPrimitives.WriteInt32LittleEndian(buf, section.Assignment ?? int.MinValue);
            sha.AppendData(buf.AsSpan(0, 4));
            foreach (var point in section.Upper.Concat(section.Lower))
            {
                D(point.X); D(point.Y); D(point.Z);
            }
        }
        return Convert.ToHexString(sha.GetHashAndReset());
    }
}
