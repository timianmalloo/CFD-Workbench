---
id: proof-frz-red-first
title: FRZ red-first - frozen-paths registry and the Python recovery suite in the ring
type: proof-pack
status: draft
owner: "@trk-frz"
tags: [frz, frozen-paths, ring]
links:
  - { to: proof-application-core, rel: relates-to }
review-by: 2026-11-12
summary: >-
  The frozen-path check refuses the OFN commit and allows it once Ruling 222 is in range; the macOS temp-symlink test is fixed and the Python suite runs in the test ring.
---
# FRZ red-first (2026-10-12, track trk-frz, branch fix/frz-frozen-paths)

## Item 1: FROZEN-PATH-UNNAMED-IN-BRIEF (Ruling 222)

Registry: `docs/coordination/frozen-paths.json`. One freeze is stated as a path freeze in `docs/notes/rulings.md`: Ruling 40 ("Freeze exactly six authored files"), registered as six entries. Each admitting ruling listed was read in the register and names the path as authored scope: 42 (`tools/qualify-windows-runtime.py`), 43 and 44 (the workflow), 45, 48, 51 (Program.cs, qualify driver, design, proof; 45 also the workflow), 222 (title names the driver). The csproj has none.

Red (the check run over the OFN commit; no ruling cited in its message or in its changed docs/proof files):

    $ python3 tools/check-frozen-paths.py --base e6655114^ --head e6655114      (exit 1)
    FROZEN-PATH tools/qualify-windows-runtime.py: frozen by Ruling 40 (Ruling 40 froze exactly six authored W0 files; the qualification driver). Fix: cite a newer ruling that admits the edit (a commit message, or a changed file under docs/proof/ or docs/notes/, with `Ruling N`, N > 40, and the path tools/qualify-windows-runtime.py).

Green (the range from that commit to this branch's HEAD contains Ruling 222, whose heading names the path):

    $ python3 tools/check-frozen-paths.py --base e6655114^ --head HEAD          (exit 0)
    check-frozen-paths: ok (e6655114^..HEAD).

Self-test (`python3 tools/check-frozen-paths.py --self-test`, 9/9, 0.8 s wall): refused without a citation; allowed with a newer citation in a commit message; refused when the citation is older than the freeze; refused when the newer citation does not name the path; allowed by an added ruling section in a changed docs/notes file; refused when the citation and the path sit in different ruling sections; an unfrozen path allowed; `--paths` finds the entry under an owned directory; a registry whose admitting ruling is not newer than the freeze fails validation.

Wiring and cost: `docs/coordination/join.json` checks (every join, after `tools/join-ring.sh`), about 0.05 s (two git calls plus one per changed note file). `tools/check-docs.py` `run_lesson_controls`: `--self-test` about 0.8 s and `--validate` about 0.05 s.

Brief line: one line added to `briefs/common.md` rule 1 (`--paths <owned paths>` at start). Register: `docs/lessons/defect-classes.md`, FROZEN-PATH-UNNAMED-IN-BRIEF.

## Item 2: the Python recovery suite in no ring

Cause (observed): the failing `test_Cache_LinkMountUnexpectedEntry_Refuses` builds its fixture under `tempfile.TemporaryDirectory()`, which on macOS is `/var/folders/...`; `/var` is a symlink to `/private/var`, and `docs/proof/win-gpu-f1-preflight/recovery/check_cache.py` correctly refuses a linked ancestor (`cache ancestry is linked`). A test-side path assumption; the validator is right. Fix: the fixture root is `Path(temporary).resolve()`.

Red (test fix stashed): `python3 -m unittest tests.test_f1_cleanup_recovery` -> `Ran 50 tests ... FAILED (errors=1)`.
Green: same command -> `Ran 50 tests in 0.228s  OK`.

Ring: `tools/run-tests.sh` now runs `py -m unittest discover -s tests -p 'test_*.py'` serially after the harnesses and prints `== Python unittest tests/test_*.py, N PASS`; a non-zero exit or zero tests ran fails the ring. Red of the step itself: the first draft used `-t .` and the step printed `FAILED: Python unittest (exit 1, 0 tests ran)` and the ring exited 1 (the guard works). Green: `tools/run-tests.sh` exit 0, `== Python unittest tests/test_*.py, 50 PASS`, `wall 47 s (budget 60 s)`, `test costs: 0 failures, 0 COST-MISS`. Cost of the step: 0.49 s wall alone (measured). It is not a cost job, so `tools/check-test-costs.py`'s job list is unchanged; its wall is inside the ring wall.
