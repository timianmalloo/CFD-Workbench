namespace CfdWorkbench.Persistence;

/// <summary>
/// The one opener for a product read of a user file. It shares Read and Delete, so a concurrent Windows save
/// (a POSIX handle-relative replace) succeeds while the reader keeps the bytes it opened; FileShare.Read alone makes
/// that replace fail with Win32 32 (Ruling 145 (1)). tools/check-reader-sharing.py fails any other reader in src/.
/// </summary>
public static class UserFile
{
    public static FileStream OpenRead(string path, FileOptions options = FileOptions.None, int bufferSize = 4096) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, bufferSize, options);

    public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellation = default)
    {
        await using var stream = OpenRead(path, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellation);
        return memory.ToArray();
    }
}
