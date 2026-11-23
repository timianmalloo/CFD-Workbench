using System.Text;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Core;
using static CfdWorkbench.Desktop.Tests.ExportTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Export Slice B: STEP AP203 &amp; Rhino 3DM release-gate honesty (COPY-481) and unlocked dialog workflow
/// (ADR-0013 §Decision 4, Ruling 273, docs/design/export-step-3dm.md). Registered as --export-cad in WorkbenchTests.
/// </summary>
public static class ExportCadDialogTests
{
    public static void Run()
    {
        DesktopChecks.Check("ExportCad_ReleaseGate_DefaultProductionIsGatedWithCopy481", DefaultProductionGated);
        DesktopChecks.Check("ExportCad_ReleaseGate_SchemaRejectsIncompleteOrOutOfTolReceipts", ReleaseGateSchema);
        DesktopChecks.Check("ExportCad_Dialog_UnlockedStep_ExportsClosedAndUntrimmed_WithTeAndDeviation", UnlockedStepDialog);
        DesktopChecks.Check("ExportCad_Dialog_UnlockedThreeDm_ExportsRoundTrip3dm_WithTeAndDeviation", UnlockedThreeDmDialog);
    }

    private const string ValidReceiptJson = """
    {
      "schemaVersion": 1,
      "fixture": "SPIKE-CAD-02",
      "targets": [
        {
          "id": "autodesk-fusion",
          "name": "Autodesk Fusion",
          "format": "step",
          "redFirstPassed": true,
          "redFirstDefect": "twist-omission-16.7mm",
          "greenPassed": true,
          "unitsMmUnscaled": true,
          "chordErrorUm": 1.2,
          "spanErrorUm": 0.4,
          "thicknessErrorUm": 1.5,
          "trailingEdgeErrorUm": 0.3,
          "singleWatertightSolid": true,
          "repairWarnings": 0,
          "volumeErrorPercent": 0.02,
          "zeroPiiVerified": true
        },
        {
          "id": "freecad-cam",
          "name": "FreeCAD CAM (Path)",
          "format": "step",
          "redFirstPassed": true,
          "redFirstDefect": "twist-omission-16.7mm",
          "greenPassed": true,
          "unitsMmUnscaled": true,
          "chordErrorUm": 1.1,
          "spanErrorUm": 0.2,
          "thicknessErrorUm": 1.4,
          "trailingEdgeErrorUm": 0.2,
          "singleWatertightSolid": true,
          "repairWarnings": 0,
          "volumeErrorPercent": 0.01,
          "zeroPiiVerified": true
        },
        {
          "id": "vectric-vcarve",
          "name": "Vectric VCarve/Aspire",
          "format": "step",
          "redFirstPassed": true,
          "redFirstDefect": "twist-omission-16.7mm",
          "greenPassed": true,
          "unitsMmUnscaled": true,
          "chordErrorUm": 2.0,
          "spanErrorUm": 0.5,
          "thicknessErrorUm": 1.8,
          "trailingEdgeErrorUm": 0.4,
          "singleWatertightSolid": true,
          "repairWarnings": 0,
          "volumeErrorPercent": 0.03,
          "zeroPiiVerified": true
        },
        {
          "id": "rhino-8",
          "name": "Rhino 8",
          "format": "3dm",
          "redFirstPassed": true,
          "redFirstDefect": "twist-omission-16.7mm",
          "greenPassed": true,
          "unitsMmUnscaled": true,
          "chordErrorUm": 0.9,
          "spanErrorUm": 0.2,
          "thicknessErrorUm": 1.1,
          "trailingEdgeErrorUm": 0.2,
          "singleWatertightSolid": true,
          "repairWarnings": 0,
          "volumeErrorPercent": 0.01,
          "zeroPiiVerified": true
        }
      ]
    }
    """;

    private static void DefaultProductionGated()
    {
        // Production repo intentionally has no fabricated docs/proof/cad-export-measure/cad-open-measure.json.
        var gate = CadExportReleaseGate.LoadDefault();
        Equal(false, gate.StepUnlocked, "production STEP must stay gated");
        Equal(false, gate.ThreeDmUnlocked, "production 3DM must stay gated");
        Equal(ExportCopy.StepUnavailable, gate.StepReason);

        using var controller = OpenExample();
        var session = new ExportSession(controller.ExportSnapshot()!);
        var dialog = new ExportDialog(session, (_, _) => Task.FromResult<string?>(null));
        dialog.Show();
        Settle();
        try
        {
            var formats = dialog.GetVisualDescendants().OfType<ListBoxItem>().ToArray();
            Equal(3, formats.Length, "gated dialog must show only dat, stl, 3mf");
            var stepBtn = dialog.FindControl<Button>("StepButton")!;
            Equal(true, stepBtn.IsVisible);
            Equal(false, stepBtn.IsEnabled);
            Equal(ExportCopy.StepUnavailable, dialog.FindControl<TextBlock>("StepWhy")!.Text);
        }
        finally { dialog.Close(); Settle(); }
    }

    private static void ReleaseGateSchema()
    {
        var ok = CadExportReleaseGate.EvaluateJson(ValidReceiptJson);
        Equal(true, ok.StepUnlocked, "valid 3-CAM receipt must unlock STEP");
        Equal(true, ok.ThreeDmUnlocked, "valid Rhino 8 receipt must unlock 3DM");
        Equal(0, ok.MissingOrFailedTargets.Count);

        // Missing red-first fails that target.
        var noRed = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"redFirstPassed\": true", "\"redFirstPassed\": false", StringComparison.Ordinal));
        Equal(false, noRed.StepUnlocked);
        Equal(false, noRed.ThreeDmUnlocked);

        // Chord error > 10 um fails Fusion -> STEP stays gated, Rhino 8 still unlocks 3DM.
        var badChord = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"chordErrorUm\": 1.2", "\"chordErrorUm\": 10.1", StringComparison.Ordinal));
        Equal(false, badChord.StepUnlocked);
        Equal(true, badChord.ThreeDmUnlocked);

        // TE error > 5 um fails FreeCAD CAM -> STEP gated.
        var badTe = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"trailingEdgeErrorUm\": 0.2", "\"trailingEdgeErrorUm\": 5.1", StringComparison.Ordinal));
        Equal(false, badTe.StepUnlocked);

        // Repair warnings > 0 fails STEP.
        var badRepair = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"repairWarnings\": 0", "\"repairWarnings\": 1", StringComparison.Ordinal));
        Equal(false, badRepair.StepUnlocked);
        Equal(false, badRepair.ThreeDmUnlocked);

        // Volume error > 0.1% fails STEP.
        var badVol = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"volumeErrorPercent\": 0.03", "\"volumeErrorPercent\": 0.15", StringComparison.Ordinal));
        Equal(false, badVol.StepUnlocked);

        // Zero-PII failure blocks unlock.
        var badPii = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"zeroPiiVerified\": true", "\"zeroPiiVerified\": false", StringComparison.Ordinal));
        Equal(false, badPii.StepUnlocked);
        Equal(false, badPii.ThreeDmUnlocked);

        // Missing Vectric target blocks STEP.
        var missingVectric = CadExportReleaseGate.EvaluateJson(ValidReceiptJson.Replace("\"vectric-vcarve\"", "\"other-cam\"", StringComparison.Ordinal));
        Equal(false, missingVectric.StepUnlocked);
        Equal(true, missingVectric.ThreeDmUnlocked);

        // Malformed JSON fails closed cleanly.
        var malformed = CadExportReleaseGate.EvaluateJson("{not-json");
        Equal(false, malformed.StepUnlocked);
        Equal(false, malformed.ThreeDmUnlocked);
        Equal(ExportCopy.StepUnavailable, malformed.StepReason);
    }

    private static void UnlockedStepDialog()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var source = controller.ExportSnapshot()!;
            var openSource = WithSource(source, OpenTe(source, 0.001));
            var unlocked = CadExportReleaseGate.EvaluateJson(ValidReceiptJson);
            var session = new ExportSession(openSource, cadGate: unlocked);
            var dialog = new ExportDialog(session, (name, _) => Task.FromResult<string?>(Path.Combine(folder, name)));
            dialog.Show();
            Settle();
            try
            {
                var formats = dialog.GetVisualDescendants().OfType<ListBoxItem>().ToArray();
                Equal(5, formats.Length, "unlocked dialog must show dat, stl, 3mf, step, 3dm");
                Equal(false, dialog.FindControl<Button>("StepButton")!.IsVisible);
                Equal(false, dialog.FindControl<TextBlock>("StepWhy")!.IsVisible);

                var stepItem = formats.Single(i => (i.Content as string) == ExportCopy.FormatStep);
                stepItem.IsSelected = true;
                Settle();
                Wait(dialog.Preparation);
                Settle();

                Equal(ExportFormat.Step, session.Format);
                Equal(StepVariant.ClosedShell, session.StepVariant);
                Equal(true, dialog.FindControl<StackPanel>("CadOptions")!.IsVisible);
                Equal(true, dialog.FindControl<StackPanel>("StepVariantPanel")!.IsVisible);
                Equal(true, dialog.FindControl<Button>("ExportButton")!.IsEnabled);
                string all = string.Join("\n", Texts(dialog));
                True(all.Contains("Trailing edge", StringComparison.Ordinal), "always-on TE row missing");
                True(all.Contains("Manufacturing: not assessed", StringComparison.Ordinal), "manufacturing note missing");
                True(all.Contains("µm", StringComparison.Ordinal), "measured deviation readout missing");

                // Switch to Half + Untrimmed and export.
                dialog.FindControl<RadioButton>("CadScopeHalf")!.IsChecked = true;
                dialog.FindControl<RadioButton>("StepVariantUntrimmed")!.IsChecked = true;
                Settle();
                Wait(dialog.Preparation);
                Settle();

                Equal(StlScope.Half, session.Scope);
                Equal(StepVariant.Untrimmed, session.StepVariant);
                Equal("basic-foil-r1-half-untrimmed.step", session.FileName);

                Wait(dialog.ExportAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name))));
                Settle();
                Equal(ExportOutcomeKind.Written, dialog.Outcome!.Kind);
                string writtenPath = Path.Combine(folder, "basic-foil-r1-half-untrimmed.step");
                True(File.Exists(writtenPath), "STEP file was not written");
                var ver = StepVerifier.Verify(File.ReadAllText(writtenPath, Encoding.UTF8), StepVariant.Untrimmed);
                True(ver.Passed && ver.BoundaryEdgeCount == 6 && ver.InteriorEdgeCount == 3 && ver.NonManifoldEdgeCount == 0,
                    "written STEP did not pass verifier: " + string.Join("; ", ver.Errors));
            }
            finally { dialog.Close(); Settle(); }
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void UnlockedThreeDmDialog()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var source = controller.ExportSnapshot()!;
            var openSource = WithSource(source, OpenTe(source, 0.001));
            var unlocked = CadExportReleaseGate.EvaluateJson(ValidReceiptJson);
            var session = new ExportSession(openSource, cadGate: unlocked);
            var dialog = new ExportDialog(session, (name, _) => Task.FromResult<string?>(Path.Combine(folder, name)));
            dialog.Show();
            Settle();
            try
            {
                var formats = dialog.GetVisualDescendants().OfType<ListBoxItem>().ToArray();
                var dmItem = formats.Single(i => (i.Content as string) == ExportCopy.FormatThreeDm);
                dmItem.IsSelected = true;
                Settle();
                Wait(dialog.Preparation);
                Settle();

                Equal(ExportFormat.ThreeDm, session.Format);
                Equal(true, dialog.FindControl<StackPanel>("CadOptions")!.IsVisible);
                Equal(false, dialog.FindControl<StackPanel>("StepVariantPanel")!.IsVisible);
                Equal(true, dialog.FindControl<Button>("ExportButton")!.IsEnabled);
                string all = string.Join("\n", Texts(dialog));
                True(all.Contains("Wing Skin", StringComparison.Ordinal), "3DM layers note missing");
                True(all.Contains("Trailing edge", StringComparison.Ordinal), "always-on TE row missing");
                True(all.Contains("µm", StringComparison.Ordinal), "measured deviation readout missing");

                Wait(dialog.ExportAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name))));
                Settle();
                Equal(ExportOutcomeKind.Written, dialog.Outcome!.Kind);
                string writtenPath = Path.Combine(folder, "basic-foil-r1.3dm");
                True(File.Exists(writtenPath), "3DM file was not written");
                var ver = Rhino3dmExport.Verify(File.ReadAllBytes(writtenPath), StlScope.Whole);
                True(ver.Passed && ver.UnitSystem == "Millimeters" && ver.ZeroPiiVerified,
                    "written 3DM did not pass round-trip verification: " + string.Join("; ", ver.Errors));
            }
            finally { dialog.Close(); Settle(); }
        }
        finally { Directory.Delete(folder, true); }
    }
}
