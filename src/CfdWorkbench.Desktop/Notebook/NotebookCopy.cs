using System.Globalization;

namespace CfdWorkbench.Desktop.Notebook;

/// <summary>
/// Seven locked mockup states of <see cref="NotebookWorkspaceView"/>
/// (<c>docs/mockups/ai-guided-design-notebook.html</c>, <c>docs/design/ai-guided-design-notebook.md §8</c>).
/// </summary>
public enum NotebookWorkspaceState
{
    Empty,
    Grill,
    Simulating,
    Iterated,
    Refused,
    NoKey,
    Overflow
}

/// <summary>
/// Single source of truth for Wave 13B AI-Guided Design Notebook copy (<c>COPY-924..934</c>,
/// operator-approved 2026-11-27 in <c>DESIGN.md</c>; zero <c>LAB-01</c> banned superlatives).
/// </summary>
public static class NotebookCopy
{
    // COPY-924 (NB-COPY-01)
    public const string EntryTitle = "New AI-guided design";

    public const string StartCardSubtitle =
        "Describe your foil, resolve tradeoffs via grill-me, and iterate with local physics.";

    // COPY-925 (NB-COPY-02)
    public const string EmptyPromptGuide =
        "Describe the foil you want to design, how you ride it, and any reference foil concepts to reason over.";

    // COPY-926 (NB-COPY-03)
    public static string UnitSanityAlert(string raw, string converted, string suggested) =>
        $"Unit check: \"{raw}\" ({converted}) is outside hydrofoil wing spans (350–2,200 mm). Did you mean {suggested}?";

    // COPY-927 (NB-COPY-04)
    public const string ReferenceDisclosure =
        "Reference traits are conceptual only (Inferred / Flagged from public specs) — no proprietary geometry is copied.";

    // COPY-928 (NB-COPY-05)
    public const string SuggestedDefaultPill = "Suggested default";

    // COPY-929 (NB-COPY-06)
    public const string ArtifactsProvenanceStrip =
        "Local calculation (VLM + strip · NeuralFoil 0.3.2 XFOIL-class surrogate · PanelMethod 200p) — computed, not tank-measured. Ventilation not modelled.";

    // COPY-930 (NB-COPY-07)
    public const string OpenInCadButton = "Open in CAD Mode →";

    // COPY-931 (NB-COPY-08)
    public const string OpenInCadDisabledReason =
        "Generate a geometry-checked 3D candidate first to open in CAD Mode.";

    // COPY-932 (NB-COPY-09)
    public static string CadHandoffConfirmBody(string candidateId, int revisionOrdinal) =>
        string.Create(CultureInfo.InvariantCulture,
            $"Opens candidate {candidateId} as Design revision r{revisionOrdinal} in Area 2 CAD. Your notebook transcript and FoilReq stay saved in project history.");

    // COPY-933 (NB-COPY-10)
    public const string NoKeyBanner =
        "No API key configured — AI-guided design needs a BYOK key. Manual Setup, CAD, and local Analysis work offline without one.";

    // COPY-934 (NB-COPY-11)
    public const string ResponseWithheldBanner =
        "Response withheld — cited a numeral not present in local solver output or breached LAB-01 copy policy.";

    // Supporting UI chrome labels (LAB-01 clean)
    public const string BackToWorkbenchButton = "← Workbench";
    public const string ComposerHint = "Local C# solvers own geometry & physics · AI proposes typed deltas only";
    public const string PreviewPayloadButton = "Preview redacted payload (AI-04)";
    public const string SendButton = "Send";
    public const string StopButton = "Stop";
    public const string ConfigureKeyButton = "Configure key";
    public const string UseManualSetupButton = "Use manual Setup / CAD";
    public const string AskAiToRepairButton = "Ask AI to repair";
    public const string SkipAssumptionButton = "Skip & keep assumption";
    public const string GenerateNowButton = "Generate candidate now";
    public const string StayInNotebookButton = "Stay in Notebook";
    public const string ConfirmOpenInCadButton = "Confirm & Open in CAD";
    public const string CadHandoffDialogTitle = "Open in CAD Mode";
    public const string WingOnlyLdLabel = "Wing-only L/D";
    public const string StaleCandidatesNotice =
        "Requirements changed after a grill branch was re-opened — active candidate artifacts are Stale until regenerated.";

    public const string DefaultExampleBrief =
        "Design a foil for wingfoiling in large waves for a 90 kg advanced rider who wants a span less than 950 cm but still wants maximum glide and a low takeoff speed, for controlled drops on steep waves and keeping speed in the bottom turn and then powered cut backs using the foil speed or power in the wing, use the concepts in the F4 Orca as the starting point.";

    public const string DownwindExampleBrief =
        "Design a downwind SUP foil for an 80 kg intermediate-advanced rider, span under 1,050 mm, easy bump-to-bump glide at 11–16 kn with stable pitch.";

    public const string FreerideExampleBrief =
        "Design a freeride wingfoil for a 75 kg intermediate rider, span around 850 mm, forgiving pitch and early takeoff under 10 kn in 15 C salt water.";

    public static readonly string[] QuickTunerPrompts =
    [
        "More glide, 15 mm narrower span if takeoff stays under 11 kn",
        "Lower takeoff speed with fuller root chord",
        "Higher steep-drop cavitation margin at 24 kn",
        "Looser cutback roll with -6 deg anhedral"
    ];
}
