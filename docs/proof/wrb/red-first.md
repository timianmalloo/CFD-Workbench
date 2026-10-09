# Track WRB red-first record (Rulings 167, 168)

## Item 1 - CFD_RING_HOST override (Ruling 168 (3))

Self-test: `tools/check-test-costs.py --self-test` (ring: every join; cost: under 0.1 s), new group `self_test_ring_host` (9 cases).

- Red (cases added, `resolve_host` not yet written): `NameError: name 'resolve_host' is not defined`, exit 1.
- Green (after): `SELFTEST 50/50 cases`, exit 0. `CFD_RING_HOST=pc-win` resolves to `pc-win` (baseline `docs/proof/ring-pc-win/`); `Tim.PC`, `PC-Win`, `../x`, empty and 33 characters are refused naming CFD_RING_HOST.
- CLI: `CFD_RING_HOST=pc-win check-test-costs.py --resolve-host Tims-PC` prints `pc-win`, exit 0; `CFD_RING_HOST=Tim.PC` exits 2 with the refusal; unset prints the hostname passed in (behaviour unchanged).
- Sweep: `git grep -in "hostname|gethostname|platform.node|COMPUTERNAME" -- tools` finds one reader, `tools/run-tests.sh:73`. It now calls the one helper (`--resolve-host`); `check-test-costs.py` receives the key through `--host`.
- Report only (not built): PROOF-PII (`tools/check-proof-pii.py`) matches Windows home paths and SIDs only. It does not flag a `docs/proof/ring-*/` folder named after a raw hostname. A guard would need an allow-list of known keys (ring-b1, ring-b2, ring-b4, ring-oct05, ring-oct08, ring-split, plus the PC's chosen key), since a hostname is not pattern-detectable. Not a Mac-only fix: the Mac hostname already leaks the same way if a Mac baseline folder is named after it.

## Item 2 - capture-manifest check (Ruling 167)

`tools/check-capture-manifests.py --self-test` (9 cases; ring: fast, via `tools/check-docs.py`).

- Red: the byte-count comparison disabled in a scratch copy (`sed 's/if len(blob) != declared_bytes:/if False:/'`): `SELFTEST FAIL byte count off by one fails: expected bytes declared, got [] over 1 manifest(s)`, `SELFTEST 8/9 cases`, exit 1.
- Green: `SELFTEST 9/9 cases`, exit 0.
- Real run: `check-capture-manifests: 1 manifest(s) match their committed blobs` (the PC's 16 entries).
- Measured cost (wall, one run, this Mac): self-test 0.67 s, real run 0.31 s. Ring: fast (check-docs, every push and join).
- The PC's `docs/proof/r163-windows-ring/verify-captures.py` is untouched.
- Leader addition (closing manifests): the guard also reads `docs/proof/*/closing-manifest.json`. Schema read from `docs/proof/win-naca/closing-manifest.json`: same `{source_base_sha, scope, files:[{path, bytes, sha256}]}` as the capture form, but entries are not confined to the folder (it lists `cases/*.yaml`), so the folder rule applies to the capture form only; `..` and absolute paths fail in both. Five more self-test cases (one clean closing fixture with a `cases/` entry, byte count off by one, wrong SHA-256, `..` path, and a capture manifest still refusing a foreign path): `SELFTEST 14/14 cases`. Real run now `2 manifest(s) match their committed blobs` (16 + 119 entries). Re-measured cost: real run 1.1 s wall, self-test about 1.3 s.

## Item 3 - handle-target.tsv:7 re-recorded (Ruling 167)

`CFD_TEST_ONLY=RecordPath_HandlePolar_SetTangentAndHandleTargetWithinIdentityTolerance tools/run-suite.sh dotnet run -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj`

- Before: `DRIFT handle-polar-target max_abs=6.1232339957367663E-18 limit=1e-06` (the 180 degree row held `6.1232339957367663E-18`, recorded before SinCosPi).
- After: line 7 reads `180	0.17499999999999999	0`; `DRIFT handle-polar-target max_abs=0 limit=1e-06`, `RESULT failures=0`.
- Only that one field changed (one-line diff).

## Item 4 - held-reader test on Windows (Ruling 167 (a))

`Library_UserFileHeldReader_SurvivesReplaceByRename` (`tests/CfdWorkbench.Core.Tests/SectionLibraryTests.cs`).

- Reachability: `src/CfdWorkbench.Persistence/ProjectStore.cs:7` has `InternalsVisibleTo("CfdWorkbench.Core.Tests")`, and `WindowsProjectStoreTests.cs` already calls `WindowsNative.OpenFixtureDirectory`, `CreatePrivate` and `Rename`. No `src/` change.
- Change: on Windows (`OperatingSystem.IsWindows()`) the test creates the replacement through `WindowsNative.CreatePrivate`, writes and flushes it, then replaces with `WindowsNative.Rename(source, parent, "held.foil", replace: true)` and requires `RenameCompletion(0, 0, 0)`. On macOS it keeps `File.Move(..., overwrite: true)`. The held reader is `UserFile.OpenRead` (FILE_SHARE_READ | FILE_SHARE_DELETE) in both.
- Mac run (behaviour unchanged): `CFD_TEST_ONLY=Library_UserFileHeldReader tools/run-suite.sh dotnet run -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj` gives `PASS Library_UserFileHeldReader_SurvivesReplaceByRename`, `RESULT failures=0`. The build has no CA1416 warning (the helper carries `[SupportedOSPlatform("windows")]`).
- Red: not provable on a Mac. The red evidence is the PC's `docs/proof/r163-windows-ring/` `Core.part2of3.log:91` (`UnauthorizedAccessException` from `File.Move`).
- What the next PC ring must show: `PASS Library_UserFileHeldReader_SurvivesReplaceByRename` in Core.part2of3 (or whichever part holds it), with the Windows-only expected-failure manifest unchanged. A `RenameCompletion` mismatch or a `NativeFailure` there is a decision request, not a manifest entry (Ruling 167).
