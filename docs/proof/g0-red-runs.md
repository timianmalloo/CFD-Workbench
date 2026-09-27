---
id: proof-g0-red-runs
title: G0 glue red-first runs
type: proof-pack
status: in-review
owner: "@track-g0"
phase: implementation
tags: [app-shell, harness, named-tests, proof]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: Red-first and green runs for G0 of the app-shell build — the named-test checker's self-test, the Desktop suite spawns, and the LayoutDocument wire-shape probe. The planted-suite red run was not performed (permission denied).
---

# G0 glue: red-first runs

G0 freezes the interfaces the later app-shell tracks build on (design `docs/design/app-shell.md` §3.4, §12.2, §14).
This file records what was observed on branch `g0-shell-glue`, macOS arm64, on 2026-09-27.

## 1. `tools/check-named-tests.py --self-test`: two plants red, then green

The self-test runs the checker on a planted design and planted logs. Each case asserts one specific error. It does not
only assert "some failure", so a script that always fails cannot pass it.

**Red run.** The self-test and the planted cases were written first. The extractor did not yet have the no-track rule
or the glob rule. Each plant also printed its own `PASS` line, so only the missing rule could make it red.

```text
$ python3 tools/check-named-tests.py --self-test      # exit 1
SELFTEST PASS green control
SELFTEST FAIL planted name with no track: expected no (<track>), got green
SELFTEST FAIL planted glob: expected is a glob, got green
SELFTEST PASS FAIL line in a log
SELFTEST PASS missing PASS
SELFTEST PASS empty list
SELFTEST PASS D3a ported column empty
SELFTEST 5/7 cases
```

**Green run.** Both rules were added to `extract()`. Nothing else changed.

```text
$ python3 tools/check-named-tests.py --self-test      # exit 0
SELFTEST PASS green control
SELFTEST PASS planted name with no track
SELFTEST PASS planted glob
SELFTEST PASS FAIL line in a log
SELFTEST PASS missing PASS
SELFTEST PASS empty list
SELFTEST PASS D3a ported column empty
SELFTEST 7/7 cases
```

**The real design.** The design has no rule violations. The counts per track are equal to the plan's simulated counts
(`docs/coordination/app-shell-build.md`: C1 14 · D1 12 · D2 21 · D3a 32 · D4 26 · P1 23). No track has written its
tests yet, so every track is red:

```text
(C1) 0/14 named tests PASS · 14 failures
(P1) 0/23 named tests PASS · 23 failures
(D1) 0/12 named tests PASS · 12 failures
(D2) 0/21 named tests PASS · 21 failures
(D3a) 0/32 named tests PASS · 33 failures     # +1: the inventory is not committed yet (§12.5)
(D4) 0/26 named tests PASS · 26 failures
(G0) 0/0 named tests PASS · 1 failures        # G0 is gated by --self-test
```

## 2. Desktop spawns: green run

The default Desktop run now spawns `--shell-model`, `--controller-shell` and `--shell-window` as child processes. It
returns the first nonzero child exit code. The three suite classes are empty. `tools/run-tests.sh` exited 0:

```text
== CfdWorkbench.Core.Tests 29 s, 236 PASS
== CfdWorkbench.Desktop.Tests 27 s, 0 PASS
wall 31 s (budget 60 s)
all test harnesses passed
```

Each spawn prints one line in `.tmp-tests/Desktop.log`, so the log shows that each child ran:

```text
SUITE --shell-model exit 0
SUITE --controller-shell exit 0
SUITE --shell-window exit 0
```

**Planted-suite red run: NOT PERFORMED.** The plan requires a planted failing check in a spawned suite to make
`tools/run-tests.sh` exit nonzero. The edit that planted
`DesktopChecks.Check("G0_PlantedCheck_FailsSuite", () => throw …)` in `ShellModelTests.Run` was denied by the session's
permission classifier. It was not retried by another route. So the claim "a failing spawned check makes `run-tests.sh`
exit nonzero" is **Inferred**, not Verified. It is inferred from reading the code: `DesktopChecks.ExitCode` is 1 after
a failed check, `Spawn` returns the first nonzero child code, and `run-tests.sh` fails a nonzero suite. The Coordinator
decides how to close this.

## 3. `LayoutDocument.cs` against §3.4: probe

A scratch console probe (not committed) compared the records with the §3.4 example. The probe used a source-generated
context: camelCase names, `UnmappedMemberHandling.Disallow`, indented output, and a string-enum converter that refuses
integers. The probe deserialized the example, serialized it again, and compared the results:

```text
SEMANTIC-EQUAL True                      # output JSON deep-equals the §3.4 example
TOP-ORDER format,version,active,workspaces
BYTE-ROUNDTRIP True                      # Serialize(Parse(Serialize(x))) == Serialize(x)
REFUSED integer-enum True
REFUSED unknown-member True
REFUSED unknown-enum True
```

**Finding for P1 (the codec owner).** §3.4 sets `MaxDepth = 8`. When that value is set on the serializer options,
`Serialize` of the §3.4 example throws `JsonException` ("object depth is larger than the maximum allowed depth of 8") at
`$.workspaces.regions.groups.panes`. Reading at depth 8 succeeds. The cap fits the reader (`JsonDocumentOptions` or
`JsonReaderOptions`). It does not fit the writer options.
