namespace CfdWorkbench.Core;

// Signature stub (m12c-section-editor.md §5.1, seam S-8). The SPT track owns the body; one pure byte patch per step kind,
// each over the previous step's bytes.
internal static class SectionEdits
{
    internal static (byte[] Bytes, SectionStepReport Report) Apply(byte[] bytes, int assignment, SectionStep step) =>
        throw new NotImplementedException($"SectionEdits.Apply({step.GetType().Name}) is owned by the SPT track (seam S-8)");
}
