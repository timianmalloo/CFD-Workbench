using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

public partial class SectionEditorView : UserControl
{
    public SectionEditorView()
    {
        InitializeComponent();
    }

    public void Bind(WorkbenchController controller)
    {
        var mode = controller.Section;
        int? assignment = mode?.Draft.Assignment ?? (controller.Selection is Selection.Station station ? station.Index : null);
        if (controller.Inspection is null || assignment is null)
        {
            EditableSectionCanvas.Profile = null;
            EditableSectionCanvas.RefitMarker = null;
            SectionEmptyText.IsVisible = true;
            return;
        }

        try
        {
            EditableSectionCanvas.Profile = controller.SectionView(assignment.Value);
            EditableSectionCanvas.RefitMarker = mode is not null && controller.SectionRefitRefusal is { } refusal
                ? new Point(refusal.ChordX, refusal.Side == SurfaceSide.Upper
                    ? Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, refusal.ChordX).UpperY
                    : Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, refusal.ChordX).LowerY)
                : null;
            SectionEmptyText.IsVisible = false;
        }
        catch (ContractError)
        {
            EditableSectionCanvas.Profile = null;
            EditableSectionCanvas.RefitMarker = null;
            SectionEmptyText.IsVisible = true;
        }
    }
}
