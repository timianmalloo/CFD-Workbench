using System.Globalization;

namespace CfdWorkbench.Desktop;

/// <summary>
/// The Export copy the section .dat and the wing STL use, quoted from docs/design/export.md section 14 and registered in
/// DESIGN.md section 7 as COPY-474 to COPY-519 (Ruling 196: COPY-EXnn becomes COPY-(473 + nn)). The strings are the oracle;
/// a test compares them to the registry. A string a later slice owns (3MF, EX07) is not here.
/// </summary>
public static class ExportCopy
{
    public const string ExportMenu = "Export…";                                  // COPY-474 (EX01)
    public const string ExportDatMenu = "Export .dat…";                          // COPY-475 (EX02)
    public const string NeedsFoil = "Export needs an open foil.";                // COPY-476 (EX03)
    public const string Title = "Export";                                        // COPY-477 (EX04)
    public const string FormatDat = "Section (.dat)";                            // COPY-478 (EX05)
    public const string FormatStl = "Wing (STL)";                                // COPY-479 (EX06)
    public const string StepUnavailable = "STEP export unavailable until the open-and-measure fixture exists";   // COPY-481 (EX08)
    public const string ShapeLabel = "Section shape";                            // COPY-482 (EX09)
    public const string ShapeAtStation = "At station";                           // COPY-482 (EX09a)
    public const string ShapeOwn = "Own";                                        // COPY-482 (EX09b)
    public const string StationLabel = "Station";                                // COPY-483 (EX10a)
    public const string OrderLabel = "File order";                               // COPY-484 (EX11)
    public const string OrderSelig = "Selig";                                    // COPY-484 (EX11a)
    public const string OrderLednicer = "Lednicer";                              // COPY-484 (EX11b)
    public const string PointsLabel = "Points per surface";                      // COPY-485 (EX12)
    public const string ScopeLabel = "Scope";                                    // COPY-486 (EX13)
    public const string ScopeWhole = "Whole wing";                               // COPY-486 (EX13a)
    public const string ScopeHalf = "Starboard half";                            // COPY-486 (EX13b)
    public const string ScopeHalfHelp =                                          // COPY-486 (EX13c)
        "Starboard half: one half wing with a flat root face, for bonding and as a datum. Cut it further, and add connectors, in your slicer.";
    public const string ToleranceLabel = "Tolerance";                            // COPY-487 (EX14)
    public const string ToleranceDraft = "Draft 0.05 mm";                        // COPY-487 (EX14a)
    public const string TolerancePrint = "Print 0.02 mm";                        // COPY-487 (EX14b)
    public const string ToleranceFine = "Fine 0.005 mm";                         // COPY-487 (EX14c)
    public const string UnitLabel = "Unit";                                      // COPY-488 (EX15)
    public const string UnitFixed = "Millimetres, unscaled. Fixed for print.";   // COPY-488 (EX15a)
    public const string SummaryHeading = "What will be written";                 // COPY-489 (EX16)
    public const string AnalysisNote = "Analysis layers and results are not exported.";   // COPY-491 (EX18)
    public const string Safety =                                                 // COPY-497 (EX24)
        "This geometry has not been checked for strength, manufacturability or ride safety. No standard for hydrofoil-wing strength applies " +
        "(RCD 2013/53/EU excludes hydrofoils and surfboards; ISO 25649 excludes rigid surf-sport devices). Test before use.";
    public const string Preparing = "Preparing the mesh…";                       // COPY-492 (EX19)
    public const string ShowTrailingEdgeGap = "Show the trailing-edge gap";      // COPY-496 (EX23a)
    public const string ClosedCheck = "Closed: every edge joins two triangles (checked).";   // COPY-499 (EX26)
    public const string Rounded = "Coordinates are rounded to 0.00003 mm.";      // COPY-500 (EX27)
    public const string Limit = "Computed by this app. No other CAD program has opened this file.";   // COPY-501 (EX28)
    public const string ExportAnyway = "Export anyway…";                         // COPY-503 (EX30)
    public const string MeshNotClosed = "The mesh did not close, so nothing was written.";   // COPY-510 (EX37)
    public const string MeshNotClosedDetails =                                   // COPY-516 (EX43)
        "Technical details: the edge check found edges that are not shared by exactly two triangles.";
    public const string WritingTitle = "Writing…";                               // COPY-515 (EX42)
    public const string BlockedTitle = "Can't export yet";                       // COPY-505 (EX32)
    public const string Blocked =                                                // COPY-506 (EX33)
        "The shape is drawn, but its geometry check has not passed, so there is no accepted geometry to export. " +
        "Open the Checks drawer, fix the finding, then export.";
    public const string DiskFull = "The disk is full.";                          // COPY-508 (EX35a)
    public const string NoPermission = "You don't have permission to write to that folder.";   // COPY-508 (EX35b)
    public const string FolderGone = "The folder no longer exists.";             // COPY-508 (EX35c)
    public const string ChooseAnotherPlace = "Choose another place…";            // COPY-509 (EX36a)
    public const string TryAgain = "Try again";                                  // COPY-509 (EX36b)
    public const string Cancelled = "Export cancelled. Nothing was written.";    // COPY-511 (EX38)
    public const string ShowInFinder = "Show in Finder";                         // COPY-512 (EX39)
    public const string ShowInExplorer = "Show in Explorer";                     // COPY-512 (EX39, Windows)
    public const string Cancel = "Cancel";                                       // COPY-513 (EX40)
    public const string TrailingEdgeLabel = "Trailing edge";                     // COPY-517 (EX44)
    public const string ManufacturingNotAssessed = "Manufacturing: not assessed (no process chosen)";   // COPY-519 (EX46)

    // The strings below take values; the format is the registry's text with <name> replaced.
    public static string ShapeHelp(double tcPercent) =>                          // COPY-483 (EX10)
        $"At station: the section as the wing builds it here; its peak thickness is the station t/c, {Fixed(tcPercent, 1)} %. " +
        "Own: the profile as authored, unscaled. Both are in chord units, with no twist.";

    public static string RevisionAccepted(int revision) => $"Revision r{revision}, accepted.";                                     // COPY-490 (EX17)

    public static string RevisionDraft(int revision) => $"Exporting revision r{revision}. Your open draft is not included.";      // COPY-490 (EX17a)

    public static string Exported(string file, int points, double chordMm, double deviationMm) =>                                  // COPY-494 (EX21)
        $"Exported {file} · {points} points · x/c, y/c · chord {Fixed(chordMm, 2)} mm · largest deviation {Fixed(deviationMm, 4)} mm";

    public static string BelowFloor(double thicknessMm, double floorMm, string label) =>                                           // COPY-495 (EX22)
        $"Trailing edge {Fixed(thicknessMm, 2)} mm, below the floor of {Fixed(floorMm, 2)} mm ({label})";

    public static string ShowAt(string where) => $"Show at {where}";                                                               // COPY-496 (EX23)

    public static string Fidelity(double deviationMm) =>                                                                           // COPY-498 (EX25a)
        $"Largest deviation between the curve and the lines joining the points: {Fixed(deviationMm, 4)} mm at this chord, sampled at segment midpoints.";

    public static string WriteFailed(string cause) =>                                                                              // COPY-507 (EX34)
        cause.Length == 0 ? "Can't write the file. Nothing was changed. The earlier file is still there."
            : $"Can't write the file. {cause} Nothing was changed. The earlier file is still there.";

    public static string TrailingEdge(double thicknessMm, string where, double floorMm, string label) =>                           // COPY-518 (EX45)
        $"Least thickness {Fixed(thicknessMm, 2)} mm at {where}. Floor {Fixed(floorMm, 2)} mm ({label}).";

    public static string ExportedMesh(string file, string triangles, string megabytes, double deviationMm) =>                     // COPY-493 (EX20)
        $"Exported {file} · {triangles} triangles · {megabytes} MB · mm · largest measured deviation {Fixed(deviationMm, 4)} mm";

    public static string FidelityMesh(double deviationMm) =>                                                                       // COPY-498 (EX25)
        $"Largest deviation from the computed surface: {Fixed(deviationMm, 4)} mm, sampled at cell midpoints. Not a bound.";

    public static string LargeMesh(string triangles, string megabytes, string printTriangles) =>                                  // COPY-502 (EX29)
        $"This mesh has {triangles} triangles ({megabytes} MB). Some slicers and CAD programs open it slowly. Choose Print for {printTriangles} triangles, or write it anyway.";

    public static string ToleranceNotReached(double deviationMm, double toleranceMm) =>                                            // COPY-504 (EX31)
        $"The tolerance was not reached. The finest mesh has a largest measured deviation of {Fixed(deviationMm, 4)} mm, over the {toleranceMm.ToString("0.0##", CultureInfo.InvariantCulture)} mm you chose.";

    public static string Writing(string triangles, string megabytes) => $"Writing {triangles} triangles ({megabytes} MB)…";        // COPY-514 (EX41)

    public static string TrailingEdgeWholeSpan(double thicknessMm, double floorMm, string label) =>                                // COPY-518 (EX45a)
        $"{Fixed(thicknessMm, 2)} mm along the whole span. Floor {Fixed(floorMm, 2)} mm ({label}).";

    public static string Fixed(double value, int decimals) => value.ToString("F" + decimals, CultureInfo.InvariantCulture);
}
