---
id: proof-etc-red-first
title: "ETC red-first receipt"
type: proof-pack
status: active
owner: "@trk-etc"
phase: implementation
tags: [etc, export, telemetry, red-first]
links:
  - { to: proof-etc-c2-cost, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  Checks for export telemetry and Ruling 204 fail on the old behaviour and pass on the new, with the commands and the observed lines.
---

# ETC red-first receipt

Method: the behaviours were neutralised in place (not the new API, which would only fail to compile), the affected checks run, then the
code restored (`scratch/etc/red.sh`; the restored file is byte-identical, `git status` shows only the intended changes).
Neutralised: `bool earlierFile = true;` (old COPY-507), the Summary label-repeat guard removed (old CLI row), and the event sink never
invoked (no telemetry). Commands:

    CFD_TEST_ONLY=Export dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --section-editor
    CFD_TEST_ONLY=Cli_Export dotnet run -c Release --no-build --project tests/CfdWorkbench.Cli.Tests

## Red (observed)

    FAIL Export_WriteFailure_CauseCopy_EarlierFileUntouched_NoTemp_H6  expected "Can't write the file. The folder no longer exists. Nothing was changed."; actual ... The earlier file is still there.
    FAIL Export_Dialog_Failure_StaysOpenWithTwoWaysOut                 same
    FAIL ExportTelemetry_Dialog_WrittenDat_OneWriteEventOnTheSessionRing   one event for one write expected 1; actual 0
    FAIL ExportTelemetry_MeshFormats_WrittenEvent_CarriesScopePresetTrianglesDeviationBytes   Sequence contains no elements
    FAIL ExportTelemetry_Refused_GeometryAndClosure_OneValidateEventEach_NoWrite   expected 1; actual 0
    FAIL ExportTelemetry_FailedWrite_OneCodedEventPerCause             one event per failed write expected 1; actual 0
    FAIL ExportTelemetry_PickerCancel_NoEvent_WriteCancel_Cancelled    expected 1; actual 0
    FAIL ExportTelemetry_Events_CarryNoPathNameUserOrHost              expected 3; actual 0
    FAIL Cli_Export_DatEqualsFixtureAndPrintsSummary                   the Revision row repeats its label
    FAIL Cli_Export_UnwritablePathExitsIo                              a missing folder has no earlier file
    FAIL Cli_Export_EmitsOneTelemetryEventPerOutcome_NoPathNoName      a written export gives one event, got 0

`Export_Copy_MatchesRegistry_Ids474To519` (COPY-507 both forms, COPY-520, COPY-521) is red against the old registry by construction
(`ExportCopy.UnitFixedInName`, `UnitFixedInFile` and the two-form `WriteFailed(cause, earlierFile)` do not exist before this change).

## Green (observed, restored code)

All `Export*` Desktop checks PASS (0 FAIL, including the 6 ExportTelemetry checks); all `Cli_Export_*` PASS, 0 FAIL.

## What the new checks pin

- One `export.write` per write attempt (written, cancelled mid-write, or one of four coded failures); none for a cancelled panel.
- One `export.validate` per refusal (geometry not accepted once at session open; closure check), and none added by the write that cannot happen.
- Fields: format, scope and preset (mesh only), outcome, milliseconds, bytes, triangles and largest deviation (mesh only).
- Run-time fixture: a hostile folder, file and mesh name, the user name and the host name are searched in every string the event holds
  and in its `ToString()`; the string fields are pinned to `Operation, Format, Scope, Preset, Outcome` so a new string field fails the check.
- The dialog path (`ShellHost` to `WorkbenchController.RecordExport` to the session ring) and the CLI path (`ExportVerb`) both emit.
