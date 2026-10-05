using CfdWorkbench.Desktop;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>M1.2d fast-ring dialog and rendered-surface checks.</summary>
public static class CatalogDialogTests
{
    private static readonly string[] CatalogNames =
    [
        "CatalogDialog_Open_FocusInSearch", "CatalogDialog_FamiliesGrouped_ListboxShape",
        "CatalogDialog_ArrowsReachDisabledRows_Quiet", "CatalogDialog_TypingPause_AnnouncesCountOnce",
        "CatalogDialog_NoMatch_Copy115ReplaceDisabled", "CatalogDialog_PendingRowEnter_NothingChanges",
        "CatalogDialog_SharedPreview_DetailNamesStationsFitLimit", "CatalogDialog_Replace_FocusToSectionMenu",
        "CatalogDialog_Escape_NothingChangedFocusToSectionMenu", "CatalogDialog_SpacingRefusal_ReasonAndChainOffered",
        "CatalogDialog_ChainButton_AppliesAllStations", "CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel",
        "CatalogDialog_MySectionsEmpty_ShowsNextAction", "CatalogDialog_DoubleClickRow_Replaces",
        "CatalogDialog_Preview_LargestChangeMarkerAtMeasuredX"
    ];
    private static readonly string[] SaveNames =
    [
        "SaveDialog_Open_FocusInName", "SaveDialog_EmptyOrDuplicate_ErrorFocusStays",
        "SaveDialog_Escape_NothingSavedFocusToSectionMenu", "SaveDialog_Save_LiveRegionFocusToSectionMenu"
    ];

    public static void Run()
    {
        foreach (string name in CatalogNames)
            DesktopChecks.Check(name, () => NeedType("CfdWorkbench.Desktop.CatalogDialog"));
        foreach (string name in SaveNames)
            DesktopChecks.Check(name, () => NeedType("CfdWorkbench.Desktop.SaveSectionDialog"));
        DesktopChecks.Check("SectionCanvas_Preview_DashedAccentPixelsOverCurrent", () =>
            NeedProperty(typeof(SectionCanvas), "PreviewProfile"));
        DesktopChecks.Check("SourceChip_AfterReplaceAndEdit_TextNotColour", () =>
            NeedProperty(typeof(SectionEditorView), "SourceChipText"));
        DesktopChecks.Check("SourceChip_StationTcDiffers_SaysScaled", () =>
            NeedProperty(typeof(SectionEditorView), "SourceChipText"));
        DesktopChecks.Check("BrowserRow_AfterReplace_NameThenSource", () =>
            NeedMethod(typeof(Panes.BrowserPane), "StationSourceText"));
    }

    private static void NeedType(string name)
    {
        if (typeof(SectionEditorView).Assembly.GetType(name) is null)
            throw new Exception($"The realized {name} surface is missing");
    }

    private static void NeedProperty(Type type, string name)
    {
        if (type.GetProperty(name) is null) throw new Exception($"{type.Name}.{name} is missing");
    }

    private static void NeedMethod(Type type, string name)
    {
        if (type.GetMethod(name) is null) throw new Exception($"{type.Name}.{name} is missing");
    }
}
