---
id: proof-c-copy-findings-red-first
title: "Track C red-first receipt"
type: proof-pack
status: active
owner: "@track-c"
phase: implementation
tags: [round-oct05, copy, ruling-82, security-probe, store-subset, analysis, cli]
links:
  - { to: proof-a3a-svc2-red-first, rel: relates-to }
  - { to: design-area3-analysis, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  Track C of round-oct05: Ruling 82 copy markers, the security probe lock path, the STORE-SUBSET static check, four
  analysis-service nits and the F-4 helper, each with its red run and its green run.
---

# Track C: red-first record (copy markers and small findings)

Branch `chore/c-copy-findings`. Every command ran from the worktree root with `TMPDIR=$PWD/.tmp-tests/` (the store
refuses the symlinked macOS temp path, which is why `inspect --runs` returns `DOC-UNSUPPORTED-PERSISTENCE` under the
default `TMPDIR`; `tools/run-tests.sh` sets the same).

| # | Item | Test or run | What it catches | Red | Green |
|---|------|-------------|-----------------|-----|-------|
| 1 | Ruling 82 markers | `Copy_AnalysisStrings_MatchDesignMd` (`LabelsTests.cs`) | COPY-206..217 and 221..240 must read "approved - Ruling 82"; COPY-218..220 must still read "proposed - awaiting operator" | `83371d0`: `FAIL Copy_AnalysisStrings_MatchDesignMd ... COPY-206 expected True; actual False` (Analysis suite exit 1) | `4fa3fd0` (Analysis suite exit 0, 110 PASS at HEAD) |
| 2 | security probe lock path | `bash cases/tools/security-probe.sh --check-pins` | the probe's pin check and its join-lock path | planted change: one line appended to `cases/tools/of-run.sh` gives `ABORT: ... sha256 01e9b016... != pinned 6c6b4e1e...`, exit 2 | clean tree: `all pinned inputs verified ... join lock: <git common dir>/coord/join.lock`, exit 0. That path is the one `tools/coordination/join-when-quiet.sh` takes |
| 3 | STORE-SUBSET static check | `python3 tools/check-docs.py` | umask or native-sensitive code (`File.WriteAllText`) outside the four store test files and `src/CfdWorkbench.Core` | planted `System.IO.File.WriteAllText` in `src/CfdWorkbench.Core/AuthoringSession.cs`: `STORE-SUBSET: ... ['CfdWorkbench.Core/AuthoringSession.cs:2087']`, non-zero exit (a `SystemExit` message) | clean tree: `store subset ok`, exit 0 (before and after the plant) |
| 4a | `IsFaulted` tautology | `Evaluate_CloseMidCompute_DocClosedNoRow` | `Fixture.Throws` already proves the task faulted, so `IsFaulted` could not fail. It now asserts one evaluation in flight at the hold point and none after the `DOC-CLOSED` refusal | mutant: the `finally` `Release(...)` call removed gives `FAIL ... evaluations in flight after the refusal (the source is released); expected 0; actual 1` | HEAD, mutant reverted |
| 4b | linked source leak | `Evaluate_ThrowingCancelCallback_NewerSourceReleased` | a cancel callback of the older evaluation throws inside `Supersede`; the newer source stayed in `inFlight` and undisposed | `6174491`: `FAIL ... evaluations in flight after the refusal; expected 0; actual 1` | `07b40ed` (map empty, `newerSource.Token.WaitHandle` throws `ObjectDisposedException`) |
| 4c | CLI vs the band's key | `Cli_AnalyseRunKey_EqualsServiceOnCustomOp` | the CLI key was compared with `OperatingPoints.Custom`, the builder the CLI itself calls. It is now also compared with the Custom point written out field by field (1 atm, datum "root LE", no load) | mutant: `StandardAtmosphere = 101326` gives `FAIL ... CLI key fb776f3f... differs from the key of the Custom point written out cabffb7d...` (the old comparison stayed green, both sides moved together) | `c29e898` |
| 4d | `inspect --runs` revision | `Cli_InspectRuns_RevisionIsSessionLabel` (`tests/CfdWorkbench.Cli.Tests/CliTests.cs`) | the listing printed the bare ordinal and dropped the edit rail that `RevisionOf` returns, so a run made on a twist edit read like the opening revision. The test evaluates on revision 1 and on revision 3 (two twist edits), saves, runs `inspect --runs`, and compares each run's printed revision with the session's `RevisionOf` label | `96aa063`: `FAIL ... run cabffb7d6224 printed revision 1, session label RevisionLabel { Ordinal = 1, Rail = }` | `c29e898`: prints `{ordinal, rail}` |
| 5 | F-4 helper | `F4_MirroredWing_NoSideForceRollYaw` | helper only: the tent camber slope is analytic (`TentSlope`: +0.08, 0 at the kink, -0.08) instead of a ±1e-6 finite difference across the kink. No expected value or tolerance changed | not a behaviour change, so no red. Evidence is equality: `MEASURE F4 old/new CL` and `e` before `0.60575055333946681/0.6045236489321153`, `1.0395712302981897/1.0424404059938661`; after `0.60575055333949734/0.60452364893213451`, `1.0395712302983457/1.0424404059939647` (differences at 1e-13) | `949e04f` |

## Findings that did not close as the brief wrote them

- **4c, the GUI's builder.** The conditions band (`ConditionsBand`, track TGL) is not in the tree: nothing under
  `src/CfdWorkbench.Desktop` builds an operating point. The shared builder `OperatingPoints.Custom` is the one the design
  names for the band (design area3-analysis.md section 3.5, CLI-01). A comparison against the band's own call has to wait for TGL;
  this record compares against the written-out Custom point instead. That is the strongest check available without the band.
- **4d, what was wrong.** `docs/proof/a3a-svc2/red-first.md` (finding 9b) showed `RevisionOf` cannot throw in the listing.
  The defect found here is the shape: the listing printed the ordinal only.
