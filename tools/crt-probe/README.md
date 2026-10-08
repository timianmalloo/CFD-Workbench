# crt-probe: does a 1-ulp change in a C-runtime call move a committed byte?

On-demand tool (Ruling 157). It is in no ring and no gate. It measures; it does not assert.

`.NET` forwards `Math.Sin/Cos/Tan/Atan2/Exp/Log/Pow` to the platform C runtime, which may differ in the last bit between
macOS, Windows and Linux. The probe simulates that on one machine: it wraps chosen `Math.*` calls so each returns the next
double up (+1 ulp), then you run the tests and read what moved.

## Files

- `CrtProbe.cs`: `CfdWorkbench.Core.CrtProbe.Up(tag, value)` returns `double.BitIncrement(value)` (not for 0, 1, -1 or a
  non-finite value, which a real runtime returns exactly) and counts calls per tag. With `CRT_PROBE_OUT=<file>` set, the
  counts are appended to that file at process exit (`tag<TAB>calls`). A tag with 0 calls means the line never ran, so a green
  result there proves nothing.
- `perturb.py`: rewrites the named source lines in place.

## Run it

Always on a scratch copy, never in a worktree (`perturb.py` refuses a folder that has `.git`).

1. Copy the tree: `rsync -a --exclude .git --exclude bin --exclude obj --exclude .tmp-tests <worktree>/ <scratch>/copy/`
2. `cp tools/crt-probe/CrtProbe.cs <scratch>/copy/src/CfdWorkbench.Core/`
3. `python3 tools/crt-probe/perturb.py <scratch>/copy src/CfdWorkbench.Core/ConstrainedFit.cs:281 src/CfdWorkbench.Core/DatImport.cs:524`
   (line numbers are those of the copy; the script prints what it wrapped).
4. Build and run one check or the ring in the copy, with a non-symlinked `TMPDIR` (the store tests refuse `/var`):
   `CRT_PROBE_OUT=<scratch>/hits.tsv CFD_TEST_ONLY=RecordPath_ dotnet <scratch>/copy/tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll`
   After `dotnet build tests/CfdWorkbench.Core.Tests -c Release` in the copy. The family checks print
   `DRIFT <family> max_chord=<value> limit=1e-06`: with the probe on, that line is the drift a 1-ulp C-runtime difference
   causes in that family.
5. Read `hits.tsv`: every wrapped tag should have a non-zero count.

## Limits

One direction (+1 ulp), one machine, no Windows run. A 1-ulp model is a model, not a measurement of Windows: the real
cross-OS number is the `DRIFT` line printed by the PC ring (Ruling 157).
