using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;

namespace CfdWorkbench.Persistence;

public sealed record LibraryEntry(string Name, string Hash, Provenance Provenance, byte[] Bytes);
public sealed record LibraryScan(IReadOnlyList<LibraryEntry> Entries, IReadOnlyList<(string File, string Reason)> Problems);

public sealed class SectionLibrary(string root)
{
    private const int MaxBytes = 1_048_576;
    private static readonly Regex FileName = new(@"\A[0-9a-f]{64}\.foil\z", RegexOptions.CultureInvariant);
    private static readonly Regex ProfileStart = new(@"\A\s*profile\s+""(?:\\.|[^""\\])*""\s*\{", RegexOptions.CultureInvariant);
    private static readonly Regex ProvenanceField = new(@"\bprovenance\s+(?<quoted>""(?:\\.|[^""\\])*"")", RegexOptions.CultureInvariant);
    private static readonly Lazy<byte[]> GeometryFixture = new(FoilSource.NewDefault);
    private Func<ProjectStore> storeFactory = static () => new ProjectStore();

    internal SectionLibrary(string root, Func<ProjectStore> storeFactory) : this(root) => this.storeFactory = storeFactory;
    public static string Root(string preferenceRoot) => Path.Combine(preferenceRoot, "sections");

    public LibraryScan Scan() => ScanCore(false);

    private LibraryScan ScanCore(bool includeCollisions)
    {
        var entries = new List<LibraryEntry>();
        var problems = new List<(string File, string Reason)>();
        if (!Directory.Exists(root)) return new(entries, problems);
        foreach (string path in Directory.EnumerateFiles(root, "*.foil", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);
            try
            {
                if (!FileName.IsMatch(file) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException();
                using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (input.Length > MaxBytes) throw new InvalidDataException();
                using var buffer = new MemoryStream();
                byte[] chunk = new byte[8192];
                int count;
                while ((count = input.Read(chunk)) != 0)
                {
                    if (buffer.Length + count > MaxBytes) throw new InvalidDataException();
                    buffer.Write(chunk, 0, count);
                }
                byte[] bytes = buffer.ToArray();
                if (Identity.Sha256(bytes) + ".foil" != file) throw new InvalidDataException();
                var parsed = FoilSource.Parse(bytes);
                if (!parsed.IsParsed) throw new InvalidDataException();
                var authored = parsed.Authored();
                if (authored.SourceUnit is not null || authored.Name is null) throw new InvalidDataException();
                Match field = ProvenanceField.Match(Encoding.UTF8.GetString(bytes));
                string? value = field.Success ? JsonSerializer.Deserialize<string>(field.Groups["quoted"].Value) : null;
                var provenance = Provenance.Parse(value);
                if (provenance.Rights == RightsClass.Vend) throw new InvalidDataException();
                entries.Add(new(authored.Name, file[..64], provenance, bytes));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
            { problems.Add((file, $"“{file}” is damaged and was skipped.")); }
        }
        var collisions = entries.GroupBy(entry => entry.Name.Normalize(NormalizationForm.FormC), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).SelectMany(group => group).ToHashSet();
        foreach (var entry in collisions)
            problems.Add((entry.Hash + ".foil", $"“{entry.Name}” appears twice in My sections. Neither can be used until one file is removed."));
        return new(includeCollisions ? entries.ToArray() : entries.Where(entry => !collisions.Contains(entry)).ToArray(), problems);
    }

    public LibraryEntry Save(string name, byte[] profileBlockBytes, Provenance provenance)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(profileBlockBytes);
        ArgumentNullException.ThrowIfNull(provenance);
        name = name.Trim().Normalize(NormalizationForm.FormC);
        if (name.Length == 0) throw new ContractError("LIB-NAME-EMPTY", "Name the section to save it.");
        if (provenance.Rights == RightsClass.Vend) throw new ContractError("LIB-SECTION-INVALID");
        byte[] bytes = Standalone(name, profileBlockBytes, provenance);
        if (bytes.Length > MaxBytes) throw new ContractError("LIB-SECTION-INVALID");
        CertifySection(bytes);
        string hash = Identity.Sha256(bytes);
        try { Directory.CreateDirectory(root); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { throw new ContractError("LIB-IO", "Couldn't save to My sections: " + error.Message + ". Nothing was saved."); }
        bool duplicate = false;
        using var store = storeFactory();
        var result = store.PublishUnderClaim(root, hash + ".foil", bytes, () =>
        {
            duplicate = ScanCore(true).Entries.Any(entry => string.Equals(entry.Name.Normalize(NormalizationForm.FormC), name, StringComparison.OrdinalIgnoreCase));
            return !duplicate;
        });
        if (result.Code != "OK")
        {
            if (duplicate) throw new ContractError("LIB-NAME-DUPLICATE");
            if (result.Code == "DOC-CONFLICT" && File.Exists(Path.Combine(root, ProjectStore.ClaimName())))
                throw new ContractError("LIB-CLAIM-HELD");
            if (result.Code == "DOC-UNSUPPORTED-PERSISTENCE") throw new ContractError(result.Code);
            if (result.PublicationKnown) throw new ContractError("DOC-SAVE-UNCERTAIN");
            throw new ContractError("LIB-IO");
        }
        return new(name, hash, provenance, bytes);
    }

    private static byte[] Standalone(string name, byte[] profileBlockBytes, Provenance provenance)
    {
        string block;
        try { block = new UTF8Encoding(false, true).GetString(profileBlockBytes); }
        catch (DecoderFallbackException) { throw new ContractError("LIB-SECTION-INVALID"); }
        Match start = ProfileStart.Match(block);
        int close = block.LastIndexOf('}');
        if (!start.Success || close < start.Length || !string.IsNullOrWhiteSpace(block[(close + 1)..]))
            throw new ContractError("LIB-SECTION-INVALID");
        string body = ProvenanceField.Replace(block[start.Length..close], "");
        string text = "foildsl \"4.1\"\nsection " + Jcs.Quote(name) + " {\n  evaluator \"cfdw-cv\" \"2\"\n" +
            body.Trim() + "\n  provenance " + Jcs.Quote(provenance.Format()) + "\n}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var parsed = FoilSource.Parse(bytes);
        if (!parsed.IsParsed || parsed.Authored().SourceUnit is not null) throw new ContractError("LIB-SECTION-INVALID");
        return bytes;
    }

    private static void CertifySection(byte[] bytes)
    {
        // A standalone section has no planform. Insert its profile into a two-station
        // foil to reuse the shipped exact geometry assessor for separation.
        string section = Encoding.UTF8.GetString(bytes);
        const string evaluator = "evaluator \"cfdw-cv\" \"2\"";
        int bodyAt = section.IndexOf(evaluator, StringComparison.Ordinal);
        if (bodyAt < 0) throw new ContractError("LIB-SECTION-INVALID");
        string body = section[(bodyAt + evaluator.Length)..section.LastIndexOf('}')];
        string foil = Encoding.UTF8.GetString(GeometryFixture.Value).Replace("foildsl \"4.0\"", "foildsl \"4.1\"", StringComparison.Ordinal);
        Match profile = Regex.Match(foil, @"profile ""[^""]+"" \{", RegexOptions.CultureInvariant);
        if (!profile.Success) throw new ContractError("LIB-SECTION-INVALID");
        int open = foil.IndexOf('{', profile.Index), depth = 0, end = -1;
        for (int index = open; index < foil.Length; index++)
        {
            if (foil[index] == '{') depth++;
            else if (foil[index] == '}' && --depth == 0) { end = index + 1; break; }
        }
        if (end < 0) throw new ContractError("LIB-SECTION-INVALID");
        foil = foil[..profile.Index] + "profile \"naca-0012\" { " + body + " }" + foil[end..];
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(foil));
        if (!parsed.IsParsed) throw new ContractError("LIB-SECTION-INVALID");
        parsed = FoilSource.Parse(FoilSource.MaterializeIds(parsed));
        if (Geometry.Assess(parsed).Status != GeometryStatus.Certified) throw new ContractError("LIB-SECTION-INVALID");
    }
}
