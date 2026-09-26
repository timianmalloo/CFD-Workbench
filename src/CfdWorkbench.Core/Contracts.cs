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

/// <summary>Section editing scope: edit the shared profile, or copy it for one assignment.</summary>
public enum SectionScope { Shared, Independent }
public sealed record BlendInterval(double EtaStart, double EtaEnd, double RootDistanceStartMeters, double RootDistanceEndMeters);
public sealed record ScopeImpact(string Profile, SectionScope Scope, IReadOnlyList<int> AffectedAssignments, IReadOnlyList<BlendInterval> Intervals);
/// <summary>One profile control vertex in normalized chord coordinates. Side is "upper" or "lower".</summary>
public sealed record ProfileVertex(string Side, string Id, double X, double Y, bool Fixed);
public sealed record ProfilePoint(double X, double Y);
/// <summary>A read-only projection of one profile for display and editing; the source stays the only authority.</summary>
public sealed record ProfileView(string Name, string Identity, IReadOnlyList<ProfileVertex> Upper, IReadOnlyList<ProfileVertex> Lower,
    IReadOnlyList<ProfilePoint> UpperCurve, IReadOnlyList<ProfilePoint> LowerCurve, string Closure);

/// <summary>Whether a profile edit may retarget the foil-wide thickness channel. Keep current is the source-compatible default.</summary>
public enum ThicknessIntent { KeepCurrent, UseSource }

/// <summary>One preview of a thickness-channel edit. Targets are t/c at the scoped assignment stations; residuals are channel minus target.</summary>
public sealed record ThicknessProposal(IReadOnlyList<double> TargetEta, IReadOnlyList<double> TargetThickness, IReadOnlyList<double> Residuals,
    double AffectedEtaStart, double AffectedEtaEnd);
