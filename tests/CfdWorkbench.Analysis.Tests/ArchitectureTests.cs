using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Design §4 item 2: the Analysis assembly calls no edit verb and no <c>ProfileAt</c> on Core. Read from the built
/// assembly's metadata (its member-reference table), not from source files, so it holds wherever the binary runs.
/// </summary>
internal static class ArchitectureTests
{
    private static readonly string[] Banned = ["ProfileAt", "Cancel", "Undo", "Redo"];
    private static readonly string[] BannedPrefixes = ["Begin", "Update", "Apply"];

    internal static void Run() =>
        Check("Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt", NoEditVerbsNoProfileAt);

    private static void NoEditVerbsNoProfileAt()
    {
        using var stream = File.OpenRead(typeof(AnalysisService).Assembly.Location);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var offenders = new List<string>();
        foreach (var handle in metadata.MemberReferences)
        {
            var member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference) continue;
            var type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            if (!FromCore(metadata, type)) continue;
            string name = metadata.GetString(member.Name);
            if (Banned.Contains(name, StringComparer.Ordinal) || BannedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                offenders.Add(metadata.GetString(type.Name) + "." + name);
        }
        Equal(0, offenders.Count, "Core edit verbs or ProfileAt referenced by CfdWorkbench.Analysis: " + string.Join(", ", offenders) + ";");
    }

    // A nested type's resolution scope is its declaring type; walk out to the assembly that defines it.
    private static bool FromCore(MetadataReader metadata, TypeReference type)
    {
        while (type.ResolutionScope.Kind == HandleKind.TypeReference)
            type = metadata.GetTypeReference((TypeReferenceHandle)type.ResolutionScope);
        return type.ResolutionScope.Kind == HandleKind.AssemblyReference &&
               metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name) == "CfdWorkbench.Core";
    }
}
