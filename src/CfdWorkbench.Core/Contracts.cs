namespace CfdWorkbench.Core;

/// <summary>A stable refusal at a document contract boundary.</summary>
public sealed class ContractError(string code) : Exception(code)
{
    public string Code { get; } = code;
}

internal static class Guard
{
    internal static void Require(bool condition, string code)
    {
        if (!condition) throw new ContractError(code);
    }
}

/// <summary>Authored facts only. Availability does not grant geometry or edit authority.</summary>
public sealed record AuthoredBinding(string SourceHash, string State, string? AcceptedId, string? DesignId,
    string? SurfaceHash, string? Evaluator, string? BaseAcceptedId, string? DraftId, long? Generation);
public sealed record AuthoredControl(string Id, double Eta, double OrdinateSi, ExactRatio ExactOrdinateSi,
    bool Editable, IReadOnlyList<string> ApplicableLocks);
public sealed record AuthoredRail(string Name, string SourceUnit, IReadOnlyList<AuthoredControl> Controls);
public sealed record AuthoredAssignment(double Eta, double SpanMeters, string ProfileName, string ProfileIdentity);
public sealed record AuthoredConstraint(string Kind, string Channel, string? VertexId, double? Eta, IReadOnlyList<double> Values);
public sealed record AuthoredAssertion(string Metric, string Comparison, double ValueSi, string? DeclaredUnit, double? ToleranceSi);
public sealed class AuthoredProjection
{
    internal AuthoredProjection(AuthoredBinding binding, string? name, string? unit, double? halfSpan,
        IEnumerable<AuthoredRail> rails, IEnumerable<AuthoredAssignment> assignments, IEnumerable<AuthoredConstraint> constraints, IEnumerable<Diagnostic> diagnostics,
        IEnumerable<AuthoredAssertion>? assertions = null)
    {
        Binding = binding; Name = name; SourceUnit = unit; HalfSpanMeters = halfSpan;
        Rails = Array.AsReadOnly(rails.Select(rail => rail with { Controls = Array.AsReadOnly(rail.Controls.Select(control => control with
            { ApplicableLocks = Array.AsReadOnly(control.ApplicableLocks.ToArray()) }).ToArray()) }).ToArray());
        Assignments = Array.AsReadOnly(assignments.ToArray());
        Constraints = Array.AsReadOnly(constraints.Select(item => item with { Values = Array.AsReadOnly(item.Values.ToArray()) }).ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        Assertions = Array.AsReadOnly((assertions ?? []).ToArray());
    }
    public AuthoredBinding Binding { get; }
    public string? Name { get; }
    public string? SourceUnit { get; }
    public string? DisplayUnit => SourceUnit;
    public double? HalfSpanMeters { get; }
    public IReadOnlyList<AuthoredRail> Rails { get; }
    public IReadOnlyList<AuthoredAssignment> Assignments { get; }
    public IReadOnlyList<AuthoredConstraint> Constraints { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<AuthoredAssertion> Assertions { get; }
    internal AuthoredProjection Rebind(AuthoredBinding binding) => new(binding, Name, SourceUnit, HalfSpanMeters, Rails, Assignments, Constraints, Diagnostics, Assertions);
}
public sealed record AcceptedInspection(AuthoredProjection Authored, GeometryAssessment Geometry);
