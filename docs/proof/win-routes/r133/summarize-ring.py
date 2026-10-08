"""Derive the receipt inventory from the preserved single-run application logs."""
import json,pathlib,re,subprocess
base=pathlib.Path(__file__).resolve().parent
raw=base/'raw-ring'
run=json.loads((raw/'run.json').read_text())
stdout=(raw/'ring.stdout.txt').read_text(errors='replace')
failures=[]
for log in sorted(raw.glob('*.log')):
    text=log.read_text(errors='replace')
    for match in re.finditer(r'^FAIL (\S+)(?: (.*))?$',text,re.M):
        error=match.group(2) or next((line for line in text.splitlines() if line.startswith('Unhandled exception.')),'Not recorded')
        failures.append(dict(source=log.name,test=match.group(1),first_error=error))
assert len(failures)==39,len(failures)
costs=[line for line in stdout.splitlines() if re.match(r'^FAILED: C-[25] ',line)]
assert len(costs)==10,costs
desktop=(raw/'Desktop.log').read_text(errors='replace').strip()
assert desktop.splitlines()==['SelfLaunchTests: all 6 cases passed.','APP-UNHANDLED APP-CRASH System.Exception']
summary=dict(tested_sha=run['tested_sha'],failures=failures,cost_failures=costs,desktop_output=desktop,desktop_frame_availability='Not recorded')
(raw/'failure-summary.json').write_bytes((json.dumps(summary,indent=2)+'\n').encode())
receipt=base.parent/'receipt.md'
text=subprocess.run(['git','show','HEAD:docs/proof/win-routes/receipt.md'],cwd=base.parents[3],check=True,stdout=subprocess.PIPE).stdout.decode('utf-8')
text=text.replace('Product integration, unobserved OS prompts/error paths and the current application ring remain open.','Product integration and unobserved OS prompts/error paths remain open; the single released application ring failed.')
text=text.replace('The heavy application ring is pending explicit coordinator release.','The single coordinator-released application ring failed; its worker-observed evidence is below.')
text=text.replace('One tested source SHA: `defbe0a931e703068a4c06278a413c0cf6d7b6bc` (`r133/tested-sha.txt`, final lightweight gate).','Solver preparation source SHA: `defbe0a931e703068a4c06278a413c0cf6d7b6bc` (`r133/tested-sha.txt`, historical lightweight gate).\nApplication ring tested SHA: `'+run['tested_sha']+'` (`r133/raw-ring/run.json`).')
text=text.replace('Commands/outputs are immutable in `r133/steps.jsonl`; logs normalize LF/trailing whitespace for review.','Solver commands/outputs are preserved in `r133/steps.jsonl`; solver logs normalize LF/trailing whitespace for review.\nApplication ring logs retain their raw bytes under `r133/raw-ring/`, protected by evidence-local Git attributes.')
text=text.replace('status and summary. Heavy `tools/run-tests.sh` explicitly held for W-1b capacity; not run.','status and summary. The numerical checkpoint initially held the application ring for W-1b capacity;\nthe coordinator subsequently released its single execution, recorded below.')
text=text.replace('Current coordinator ring must name every failing test/first error, crash frame,\ncost checks, tested SHA and DOTNET_ROOT per PR #4.','That earlier reported run is separate from the worker-observed ring below.')
text=text.replace('**Remaining:** coordinator/Owner review, official index/audit derivation, explicit ring release and single execution,\nthen delivery/review.','**Remaining:** coordinator/Owner review of the red application ring, official index/audit derivation,\nthen delivery/review.')
section='''
## Released application ring: worker-observed, failed

**Verified:** executed exactly once at `SHA` after coordinator capacity release. No solver rerun, product repair,
SDK installation, persistent host change or second ring. Exact entry command: `py -3 docs/proof/win-routes/r133/run-application-ring.py`;
the wrapper invoked `C:/Program Files/Git/bin/bash.exe --noprofile --norc tools/run-tests.sh` using the repository's
`tools/py-resolve.sh`. Process-only environment: `DOTNET_ROOT=%USERPROFILE%\\.dotnet`, that directory prepended to
PATH, `CFD_TEST_BUDGET_SECONDS=60`; observed SDK **10.0.203**. UTC start `START`.

Overall exit **1**. Wrapper wall **WALL s**; ring wall **60,992 ms** (printed 61 s), exceeding the 60 s budget by
992 ms; build clock **11,438 ms**, net **49,554 ms**. Bash reports CPU 2 s and load 5.61 → 7.49; native aggregate
CPU/RAM **Not recorded**. The failure path returned 1 before the final budget guard; no budget-pass claim.
Release build passed, **0 errors**, **2 AVLN3001 warnings** (CatalogDialog.axaml and SaveSectionDialog.axaml).

The first action after the process returned copied all **24** scratch log/clock files from `.tmp-tests` into
`r133/raw-ring/`. `run.json` records their byte counts and SHA-256. Raw stdout, SDK identity and clocks are retained;
`failure-summary.json` is derived from those files, without rewriting them.

| Suite / partition | Exit | Wall ms | Named failures | PASS lines |
|---|---:|---:|---:|---:|
| Core 1/3 | 1 | 40,048 | 13 | 215 |
| Core 2/3 | 1 | 45,294 | 13 | 215 |
| Core 3/3 | 1 | 39,692 | 11 | 216 |
| Desktop | 70 | 6,430 | Not recorded | 0 |
| Analysis 1/2 | 1 | 13,555 | 1 | 111 |
| Analysis 2/2 | 0 | 11,917 | 0 | 120 |
| CLI | 127 | 4,017 | 1 | 3 |

Desktop raw output is exactly:

```text
SelfLaunchTests: all 6 cases passed.
APP-UNHANDLED APP-CRASH System.Exception
```

Desktop stack frames and failing test name: **Not recorded**; neither Desktop.log nor ring stdout includes them.
CLI captures `Unhandled exception. System.InvalidOperationException: inspect --runs returned 3` followed by
`DOC-UNSUPPORTED-PERSISTENCE`, then a frame in `tests/CfdWorkbench.Cli.Tests/CliTests.cs:line 179` and
`Program.<Main>(String[] args)`. This does not establish the Desktop crash location.

All **39** named failures and first errors follow; log names identify their archived source.

| Raw log | Failing test | First error |
|---|---|---|
'''.replace('SHA',run['tested_sha']).replace('START',run['started_utc']).replace('WALL',f"{run['wrapper_wall_seconds']:.6f}")
for row in failures:
    cells=[row['source'],row['test'],row['first_error']]
    section+='| '+' | '.join('`'+cell.replace('|','\\|')+'`' for cell in cells)+' |\n'
section+='\nAll **10** cost failures, verbatim from ring stdout (0 COST-MISS):\n\n```text\n'+'\n'.join(costs)+'\n```\n'
section+='\nPost-ring lightweight checks: both Windows cases pass; full case validation exit 1 in 0.381 s only for the\nexisting shared `spike03-s6-w4.yaml` non-hash. LF frozen-input check exit 0 in 0.466 s. Docs check exit 1 in\n13.392 s; zero metadata errors/orphans, two coordinator-owned index drifts (proof-win-routes status/summary).\nExact commands, exits and times are in `r133/steps.jsonl`, with `ring-*.txt` outputs.\n'
text+=section
receipt.write_bytes(text.encode())
print('PASS: receipt derived from 39 named failures, 10 cost failures, exact Desktop output; raw files unchanged')
