using System.IO;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

public abstract record OpenOutcome
{
    public sealed record Opened(string Path) : OpenOutcome;
    public sealed record NeedsIds(byte[] Candidate, byte[] Original) : OpenOutcome;
    public sealed record Refused(string Code, byte[] Original) : OpenOutcome;
    public sealed record Failed(OpenFailure Failure) : OpenOutcome;
    public sealed record Cancelled : OpenOutcome;
    public sealed record Superseded : OpenOutcome;
}

public enum OpenFailureKind
{
    Missing,
    AccessDenied,
    Unreadable,
    TooLarge,
    Newer,
    UnknownContent,
    NotRecognised,
    UnsupportedLocation,
    UnsupportedPlatform,
    NotAFile,
    Changed,
    CloudOnly
}

public abstract record OpenFailure(string Code, string Path)
{
    public string? UnsupportedReason { get; init; }
    public abstract OpenFailureKind Kind { get; }

    public sealed record Missing(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.Missing;
    }

    public sealed record AccessDenied(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.AccessDenied;
    }

    public sealed record Unreadable(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.Unreadable;
    }

    public sealed record TooLarge(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.TooLarge;
    }

    public sealed record Newer(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.Newer;
    }

    public sealed record UnknownContent(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.UnknownContent;
    }

    public sealed record NotRecognised(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.NotRecognised;
    }

    public sealed record UnsupportedLocation(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.UnsupportedLocation;
    }

    public sealed record UnsupportedPlatform(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.UnsupportedPlatform;
    }

    public sealed record NotAFile(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.NotAFile;
    }

    /// <summary>The file changed or was in use while it was read (DOC-CONFLICT with the file still present; Ruling 261).</summary>
    public sealed record Changed(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.Changed;
    }

    /// <summary>A OneDrive file that is not on this PC (DOC-CLOUD-ONLY, refused before any data handle: design windows-store-recovery 2.6 O3, DR-DS-6).</summary>
    public sealed record CloudOnly(string Code, string Path) : OpenFailure(Code, Path)
    {
        public override OpenFailureKind Kind => OpenFailureKind.CloudOnly;
    }

    public static OpenFailure Classify(Exception exception, string path, Func<string, bool>? fileExists = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is FileNotFoundException or DirectoryNotFoundException)
            return new Missing("FILE-NOT-FOUND", path);

        if (exception is UnauthorizedAccessException)
            return new AccessDenied("DOC-ACCESS", path);

        if (exception is ContractError contractError)
            return ClassifyCode(contractError.Code, path, fileExists) with { UnsupportedReason = contractError.Reason };

        if (exception is IOException)
            return new Unreadable("DOC-IO", path);

        return ClassifyCode(exception.Message, path, fileExists);
    }

    public static OpenFailure ClassifyCode(string code, string path, Func<string, bool>? fileExists = null)
    {
        if (code is "DOC-NOT-FOUND")
            return new Missing(code, path);
        if (code is "DOC-ACCESS" or "EACCES")
            return new AccessDenied(code, path);
        if (code is "DOC-IO")
            return new Unreadable(code, path);
        if (code is "DSL-LIMIT" or "DOC-SIZE")
            return new TooLarge(code, path);
        if (code is "DOC-VERSION" or "DSL-VERSION")
            return new Newer(code, path);
        if (code is "DOC-UNSUPPORTED-PERSISTENCE")
            return new UnsupportedLocation(code, path);
        if (code is "DOC-UNSUPPORTED-PLATFORM")
            return new UnsupportedPlatform(code, path);
        if (code is "DOC-NOT-A-FILE")
            return new NotAFile(code, path);
        // The store reports a missing file as DOC-CONFLICT too (its Win32 2 and 3, errno 2); the file system tells the two apart, so the store stays unchanged.
        if (code is "DOC-CONFLICT")
            return path.Length > 0 && !(fileExists ?? File.Exists)(path) ? new Missing(code, path) : new Changed(code, path);
        if (code is "DOC-CLOUD-ONLY")
            return new CloudOnly(code, path);
        if (code is "DOC-UNSUPPORTED-FIELD")
            return new UnknownContent(code, path);
        if (code is "DOC-SCHEMA" or "DOC-TYPE" or "DOC-REFERENCE" || code.StartsWith("DSL-", StringComparison.OrdinalIgnoreCase))
            return new NotRecognised(code, path);

        return new NotRecognised(code, path);
    }
}
