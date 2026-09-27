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
    NotRecognised
}

public abstract record OpenFailure(string Code, string Path)
{
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

    public static OpenFailure Classify(Exception exception, string path)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Check if file is absent on disk
        if (exception is FileNotFoundException or DirectoryNotFoundException || !File.Exists(path))
        {
            string code = exception is ContractError ce ? ce.Code : "DOC-NOT-FOUND";
            return new Missing(code, path);
        }

        if (exception is UnauthorizedAccessException)
            return new AccessDenied("DOC-ACCESS", path);

        if (exception is ContractError contractError)
            return ClassifyCode(contractError.Code, path);

        if (exception is IOException)
            return new Unreadable("DOC-IO", path);

        return ClassifyCode(exception.Message, path);
    }

    public static OpenFailure ClassifyCode(string code, string path)
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
        if (code is "DOC-UNSUPPORTED-FIELD")
            return new UnknownContent(code, path);
        if (code is "DOC-SCHEMA" or "DOC-TYPE" or "DOC-REFERENCE" || code.StartsWith("DSL-", StringComparison.OrdinalIgnoreCase))
            return new NotRecognised(code, path);

        return new NotRecognised(code, path);
    }
}
