using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using CfdWorkbench.Persistence;

internal static class Program
{
    private static readonly byte[] Replacement = "COPY447 replacement image\n"u8.ToArray();
    private static readonly byte[] Existing = "COPY447 known existing target\n"u8.ToArray();

    private static int Main(string[] args)
    {
        try
        {
            if (args is ["--self-test"])
            {
                SelfTest();
                Console.WriteLine("SELFTEST PASS inventory hashes known bytes and records absent/file entries");
                return 0;
            }
            if (args is not ["--case", "create-only" or "overwrite", var root])
            {
                Console.Error.WriteLine("FAIL usage: Probe --case create-only|overwrite <isolated-root>");
                return 2;
            }
            RunCase(args[1], Path.GetFullPath(root));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL {error.GetType().Name}: {error.Message}");
            return 1;
        }
    }

    private static void SelfTest()
    {
        string root = Path.Combine(Path.GetTempPath(), "copy447-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            byte[] known = "copy447 inventory oracle\n"u8.ToArray();
            string file = Path.Combine(root, "known.bin");
            File.WriteAllBytes(file, known);
            try
            {
                var rows = Inventory(root);
                if (rows.Length != 1 || rows[0].RelativeName != "known.bin" || rows[0].Type != "file" ||
                    rows[0].Bytes != known.Length || rows[0].Sha256 != Sha256(known))
                    throw new InvalidOperationException("inventory did not match the known-file oracle");
            }
            finally { File.Delete(file); }
        }
        finally { Directory.Delete(root); }
    }

    private static void RunCase(string variant, string root)
    {
        Directory.CreateDirectory(root);
        string target = Path.Combine(root, "project.cfdw");
        byte[]? expectedBefore = variant == "overwrite" ? Existing : null;
        if (expectedBefore is not null) File.WriteAllBytes(target, expectedBefore);

        var before = Inventory(root);
        bool expectedExistsBefore = expectedBefore is not null;
        bool actualExistsBefore = File.Exists(target);
        string? expectedHashBefore = expectedBefore is null ? null : Sha256(expectedBefore);
        string? actualHashBefore = actualExistsBefore ? HashFile(target) : null;
        if (actualExistsBefore != expectedExistsBefore || actualHashBefore != expectedHashBefore)
            throw new InvalidOperationException("precondition target did not match the case oracle");

        using var store = new ProjectStore();
        var request = new SaveRequest(Replacement, expectedHashBefore, Guid.NewGuid().ToString("D"));
        var wall = Stopwatch.StartNew();
        SaveResult result = store.SaveAsync(target, request).GetAwaiter().GetResult();
        wall.Stop();
        var events = store.ReadLocalEvents();

        var after = Inventory(root);
        bool existsAfter = File.Exists(target);
        string? hashAfter = existsAfter ? HashFile(target) : null;
        long? bytesAfter = existsAfter ? new FileInfo(target).Length : null;
        bool sameTarget = actualExistsBefore == existsAfter && actualHashBefore == hashAfter;
        string conclusion = result.Code == "DOC-UNSUPPORTED-PERSISTENCE"
            ? "NOT ASSESSED: public ProjectStore.SaveAsync refused this Windows operation; crash-left artifacts are not measured."
            : "NOT ASSESSED: normal completion does not measure crash-left artifacts.";

        var record = new
        {
            schema = "copy447-public-projectstore-reachability/v1",
            variant,
            publicApi = "ProjectStore.SaveAsync",
            conclusion,
            environment = new
            {
                runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                os = Environment.OSVersion.Version.ToString(),
                platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                filesystem = new DriveInfo(Path.GetPathRoot(root)!).DriveFormat
            },
            target = new
            {
                relativeName = "project.cfdw",
                expectedExistsBefore,
                actualExistsBefore,
                expectedSha256Before = expectedHashBefore,
                actualSha256Before = actualHashBefore,
                existsAfter,
                bytesAfter,
                sha256After = hashAfter,
                unchangedFromBefore = sameTarget,
                replacementSha256 = Sha256(Replacement)
            },
            saveResult = new
            {
                result.Code,
                result.PublishedSha256,
                result.PublicationKnown,
                result.DurabilityConfirmed
            },
            timing = new
            {
                wallMilliseconds = wall.Elapsed.TotalMilliseconds,
                localStoreMilliseconds = events.LastOrDefault(item => item.Action == "store.save")?.DurationMilliseconds
            },
            localTelemetry = events,
            before,
            after
        };
        Console.WriteLine(JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        }));
    }

    private static InventoryRow[] Inventory(string root) => Directory
        .EnumerateFileSystemEntries(root)
        .Select(path =>
        {
            string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            var attributes = File.GetAttributes(path);
            bool directory = (attributes & FileAttributes.Directory) != 0;
            bool reparse = (attributes & FileAttributes.ReparsePoint) != 0;
            string type = reparse ? (directory ? "reparse-directory" : "reparse-file") : directory ? "directory" : "file";
            if (directory || reparse) return new InventoryRow(relative, type, null, null);
            var info = new FileInfo(path);
            return new InventoryRow(relative, type, info.Length, HashFile(path));
        })
        .OrderBy(row => row.RelativeName, StringComparer.Ordinal)
        .ToArray();

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed record InventoryRow(string RelativeName, string Type, long? Bytes, string? Sha256);
}
