using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

public enum ShellMode
{
    Start,
    Opening,
    Workspace,
    SectionEditor
}

public enum Mode
{
    Start,
    Opening,
    Workspace,
    SectionEditor
}

public sealed record PropertiesModel(
    string Heading,
    IReadOnlyList<PropertiesBlock> Blocks,
    string? EmptyDescription = null);

public sealed record PropertiesBlock(
    string Title,
    IReadOnlyList<PropertyRow> Rows);

public abstract record PropertyRow(string Label, string Value, bool IsEditable = false, string? Unit = null, string? HelperText = null)
{
    public sealed record Text(string Label, string Value, string? HelperText = null)
        : PropertyRow(Label, Value, false, null, HelperText);

    public sealed record Input(string Label, string Value, string Unit = "mm", bool IsEditable = true, string? HelperText = null)
        : PropertyRow(Label, Value, IsEditable, Unit, HelperText);
}

public static class PropertiesView
{
    public static PropertiesModel Build(
        Selection selection,
        AuthoredProjection? projection,
        WingEstimates? estimates,
        ShellMode mode)
    {
        if (projection is null)
        {
            return new PropertiesModel(
                "No foil open",
                Array.Empty<PropertiesBlock>(),
                "Open or start a foil. Whatever you select in it shows its properties here.");
        }

        var blocks = new List<PropertiesBlock>();

        // 1. Selection-specific blocks
        switch (selection)
        {
            case Selection.Station station:
                if (station.Index >= 0 && station.Index < projection.Assignments.Count)
                {
                    var assignment = projection.Assignments[station.Index];
                    blocks.Add(new PropertiesBlock("Station", new List<PropertyRow>
                    {
                        new PropertyRow.Text("Index", station.Index.ToString(CultureInfo.InvariantCulture)),
                        new PropertyRow.Text("η", station.Eta.ToString("G4", CultureInfo.InvariantCulture)),
                        new PropertyRow.Text("Span pos", $"{assignment.SpanMeters * 1000:F1} mm"),
                        new PropertyRow.Text("Profile", assignment.ProfileName)
                    }));
                }
                break;

            case Selection.Foil:
            case Selection.None:
            default:
                blocks.Add(new PropertiesBlock("Foil", new List<PropertyRow>
                {
                    new PropertyRow.Text("Name", projection.Name ?? "Unnamed"),
                    new PropertyRow.Text("Stations", projection.Assignments.Count.ToString(CultureInfo.InvariantCulture))
                }));
                break;
        }

        // 2. Wing block ALWAYS LAST whenever a foil is open (CAD-17, UI-36)
        var wingRows = new List<PropertyRow>();
        double spanMm = (projection.HalfSpanMeters ?? 0) * 2000;
        string spanFormatted = spanMm.ToString("F1", CultureInfo.InvariantCulture);

        if (mode == ShellMode.SectionEditor)
        {
            // Section mode: Wing dimensions as text with COPY-122
            wingRows.Add(new PropertyRow.Text("Span", $"{spanFormatted} mm", "Set these in the workspace."));
            wingRows.Add(new PropertyRow.Text("Root chord", FormatChord(estimates?.RootChordMeters), "Set these in the workspace."));
            wingRows.Add(new PropertyRow.Text("Tip chord", FormatTipChord(estimates?.TipChordMeters, isClosed: false), "Set these in the workspace."));
        }
        else
        {
            // Workspace mode: typed Span input, Root chord and Tip chord as text
            wingRows.Add(new PropertyRow.Input("Span", spanFormatted, "mm", true));
            wingRows.Add(new PropertyRow.Text("Root chord", FormatChord(estimates?.RootChordMeters)));

            // A closing tip shows COPY-108 "Tip closes — edit the tip station"
            bool tipCloses = estimates is not null && estimates.TipChordMeters <= 1e-9;
            string tipChordText = FormatTipChord(estimates?.TipChordMeters, tipCloses);
            wingRows.Add(new PropertyRow.Text("Tip chord", tipChordText));
        }

        if (estimates is not null)
        {
            wingRows.Add(new PropertyRow.Text("Area", $"{estimates.AreaSquareMeters:F3} m²"));
            wingRows.Add(new PropertyRow.Text("Aspect ratio", $"{estimates.AspectRatio:F2}"));
        }

        blocks.Add(new PropertiesBlock("Wing", wingRows));

        string heading = selection switch
        {
            Selection.Station s => $"Station {s.Index} (η {s.Eta:G3})",
            _ => projection.Name ?? "Foil Properties"
        };

        return new PropertiesModel(heading, blocks);
    }

    public static PropertiesModel Build(
        Selection selection,
        AuthoredProjection? projection,
        WingEstimates? estimates,
        Mode mode) => Build(selection, projection, estimates, (ShellMode)mode);

    private static string FormatChord(double? meters) =>
        meters is double m ? $"{(m * 1000):F1} mm" : "—";

    private static string FormatTipChord(double? meters, bool isClosed)
    {
        if (isClosed) return "Tip closes — edit the tip station";
        return FormatChord(meters);
    }
}
