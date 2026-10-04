using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>What one evaluation covers: the whole wing, or one authored station at its η (design §6.1).</summary>
public abstract record Scope
{
    private Scope() { }
    public sealed record Wing : Scope;
    public sealed record Station(int Index, double Eta) : Scope;
}

/// <summary>
/// Evaluates the accepted revision: snapshot once, compute, record (design §4, §6.1). Idempotent by run key: an existing
/// Completed run with the same key is returned. The only write is <c>AuthoringSession.RecordRun</c>. SVC owns the body.
/// </summary>
public sealed class AnalysisService(AuthoringSession session)
{
    private readonly AuthoringSession session = session;

    public Task<AnalysisRun> EvaluateAsync(OperatingPoint op, Tier tier, Scope scope, CancellationToken cancellation) =>
        throw new NotImplementedException("SVC: AnalysisService.EvaluateAsync");
}
