using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CfdWorkbench.Desktop;

public partial class SectionEditorView : UserControl
{
    public SectionEditorView()
    {
        InitializeComponent();
    }

    public void Bind(WorkbenchController controller)
    {
        if (controller.Inspection is null || controller.Selection is not Selection.Station station)
        {
            EditableSectionCanvas.Profile = null;
            SectionEmptyText.IsVisible = true;
            return;
        }

        try
        {
            EditableSectionCanvas.Profile = controller.SectionView(station.Index);
            SectionEmptyText.IsVisible = false;
        }
        catch (CfdWorkbench.Core.ContractError)
        {
            EditableSectionCanvas.Profile = null;
            SectionEmptyText.IsVisible = true;
        }
    }
}
