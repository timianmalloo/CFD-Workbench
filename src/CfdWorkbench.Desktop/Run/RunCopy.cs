namespace CfdWorkbench.Desktop.CfdRun;

/// <summary>
/// Plain-words Area 5 Run workspace copy constants (DESIGN.md COPY-816..832 proposed).
/// LAB-01 compliant: never emits a bare "Converged" string without all three parts; zero shell commands.
/// </summary>
public static class RunCopy
{
    public const string WorkspaceTitle = "CFD Run Pipeline";                                                // COPY-816
    public const string QueueHeading = "Run queue & history";                                               // COPY-817
    public const string EmptyQueueNote = "No CFD runs recorded yet. Queue a case from Experiment or run a sample when a solver is ready."; // COPY-818
    public const string PipelineHeading = "11-state pipeline status";                                       // COPY-819
    public const string MeshGateHeading = "Mesh gate (DR-F3-1 A / DR-F2-3)";                                // COPY-820
    public const string ResidualsHeading = "Residuals & force coefficients (DR-F2-1)";                      // COPY-821
    public const string OfflineAssistantHeading = "Failure diagnostics & case diff (offline)";              // COPY-822
    public const string OfflineNoKeyNotice = "No API key configured — deterministic offline diagnostics and typed case-diff validation work without network calls."; // COPY-823

    public const string RunButton = "Run";                                                                  // COPY-824
    public const string CancelButton = "Cancel";                                                            // COPY-825
    public const string RetrySampleButton = "Retry sample";                                                 // COPY-826
    public const string SetUpSolverButton = "Set up a solver";                                              // COPY-827
    public const string ExplainFailureButton = "Explain this failure";                                      // COPY-828
    public const string PreviewCaseDiffButton = "Preview case diff";                                        // COPY-829

    public const string SolverReadyBadge = "Solver: Ready";                                                 // COPY-830
    public const string SolverNotReadyBadge = "Solver: Not ready";                                          // COPY-830
    public const string ScreenOnlyNotice = "Mesh screen only — not the y+ gate (awaiting stationary field)."; // COPY-831
    public const string NoRunSelectedNote = "Select a CFD run from the queue to inspect its state, mesh gate, and residuals."; // COPY-832
}
