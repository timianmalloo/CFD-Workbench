# DTG red-first: a folder at the export target name

Defect (track WRX): a folder at the target name failed at the rename. macOS gave an IOException (generic text); Windows gave
an access-denied error, worded as "You don't have permission to write to that folder", which is false (Inferred for Windows:
from the r199 ring text pinned in the old test; not re-run here).

Fix: `ExportSession.RunAsync` refuses an existing directory at the target name before any write, beside the link pre-check.
Code `EXPORT-TARGET-DIRECTORY`, an `export.validate` event, COPY-507 with no cause and no earlier-file sentence
("Can't write the file. Nothing was changed."). No new copy. CLI: reuses exit 5 (path not writable) and prints
`Error DOC-IO: <COPY-507>`; the usage text, `docs/design/application-foundation.md` and `docs/design/export.md` say so.

## Red (old code, new checks; macOS)

Command: `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet <test dll>`, built from the unchanged `src/`.

- Cli: `FAIL Export_DirectoryTargetHasOneCodeOnBothSurfaces Exception: the dialog outcome code is ` (the old code has no outcome code).
- Desktop: `FAIL Export_WriteFailure_CauseCopy_EarlierFileUntouched_NoTemp_H6 InvalidOperationException: expected EXPORT-TARGET-DIRECTORY; actual `.

The message check alone passes on macOS with the old code (the old macOS text was already generic), so the red is the code
and event assertions, as the brief expected.

## Green (fix applied; macOS)

- `PASS Export_DirectoryTargetHasOneCodeOnBothSurfaces` (CLI exit 5, dialog and CLI each emit one `export.validate` /
  `EXPORT-TARGET-DIRECTORY` event, one message, nothing written, no temp file left).
- `PASS Export_WriteFailure_CauseCopy_EarlierFileUntouched_NoTemp_H6` (the `OperatingSystem.IsWindows()` branch is gone; one expectation).

Windows: no `expected-failures.windows.json` entry names these checks. The Windows pass is Inferred until the PC rerun.
