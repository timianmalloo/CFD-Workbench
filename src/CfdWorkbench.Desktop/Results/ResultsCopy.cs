namespace CfdWorkbench.Desktop.Results;

/// <summary>
/// Plain-words Area 6 Results &amp; Candidate Ladder copy constants (DESIGN.md COPY-833..865 proposed).
/// Enforces VIZ truth rules (tau_w-gated separation, vortex-core candidates, exact UX-06 ParaView absence string,
/// exact "Unavailable — mesh differs", exact replay banners).
/// </summary>
public static class ResultsCopy
{
    public const string WorkspaceTitle = "Results & Candidate Ladder";                                      // COPY-833
    public const string SamplePickerLabel = "Sample";                                                       // COPY-834
    public const string EmptySamplesNote = "No completed or harvested samples yet. Evaluate in Analysis, Sweeps, or Run to inspect fields."; // COPY-835

    public const string LayerSurfaceCp = "Surface Cp";                                                      // COPY-836
    public const string LayerSurfaceCf = "Surface Cf";                                                      // COPY-837
    public const string LayerSliceVelocity = "Velocity slice";                                              // COPY-838
    public const string LayerStreamlines = "3D streamlines";                                                // COPY-839
    public const string LayerTke = "Turbulent kinetic energy";                                              // COPY-840
    public const string LayerForceVectors = "Force vectors";                                                // COPY-841
    public const string LayerSpanwiseLoading = "Spanwise loading";                                          // COPY-842
    public const string LayerVortexCore = "Vortex-core candidates";                                         // COPY-843
    public const string LayerSeparation = "Separation";                                                     // COPY-844
    public const string LayerPathlines = "Pathlines";                                                       // COPY-845

    public const string ProvenanceHeading = "Provenance strip (9 required fields)";                         // COPY-846
    public const string TableTwinToggle = "Linked table twin";                                              // COPY-847
    public const string ReplayHeading = "Parametric replay";                                                // COPY-853
    public const string PlayButton = "Play";                                                                // COPY-854
    public const string HoldButton = "Hold";                                                                // COPY-854
    public const string StepBackButton = "Step back";                                                       // COPY-854
    public const string StepForwardButton = "Step forward";                                                 // COPY-854
    public const string LockRangeToggle = "Lock range";                                                     // COPY-855
    public const string DifferenceHeading = "Difference flood (zero-pinned vik)";                           // COPY-856
    public const string ComputeDifferenceButton = "Compute difference";                                     // COPY-857
    public const string CandidateLadderHeading = "Candidate ladder (COMMIT-01)";                            // COPY-858
    public const string AcceptAsDraftButton = "Accept as draft";                                            // COPY-859
    public const string RebaseDraftButton = "Rebase";                                                       // COPY-860
    public const string OpenParaViewButton = "Open in ParaView";                                            // COPY-861
    public const string AskResultHeading = "Ask about this result (offline)";                               // COPY-862
    public const string AskResultButton = "Ask";                                                            // COPY-863

    public const string ManifestAvailableParaView = "Available · Inspect in ParaView";
    public const string ManifestUnavailable = "Unavailable";
    public const string ReplayNoSampleNote = "Replay unavailable — select a completed CFD sample with ≥ 2 compatible samples on the held axis.";
    public const string ReplayMissingMeshNote = "Replay unavailable — selected sample has no CFD mesh hash (requires ≥ 2 compatible samples sharing mesh, method, water, and held axis).";

    public static string ReplayNeedsTwoSamplesNote(int compatibleCount, string heldAxisLabel) =>
        $"Replay unavailable — only {compatibleCount} compatible sample at held {heldAxisLabel} (requires ≥ 2 samples sharing mesh, method, water, and held axis).";
}
