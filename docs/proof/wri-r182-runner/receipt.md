---
id: proof-wri-r182-runner-ready
title: "Ruling 182 Windows runner-ready receipt"
type: proof-pack
status: complete
owner: "@win-wri-r182-runner"
phase: implementation
tags: [windows, runner, ruling-182, red-first, proof]
links:
  - { to: review-pr-23, rel: relates-to }
  - { to: review-pr-22, rel: relates-to }
  - { to: proof-msp-receipt, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Reusable runner preparation passes all six Ruling 182 controls and the Ruling 181
  lifecycle fixtures on an MSP/RG5 head. No scale change, product contract check, or
  store verifier execution occurred. This receipt does not claim an execution budget.
---

# Ruling 182 Windows runner-ready receipt

**Verified:** runner preparation passes on merged head
`be3900d5304e663418c94291123e5885f038c896`. Ancestry checks for MSP join `1d52616a`
and RG5 join `83bd96b4` both returned 0. Integration was a fast-forward, with no
conflict or implementation edit. Three runner files retain the hashes approved
after repair cycle **2/2**. The checker has the narrowly authorized R183 portability
correction below; the coordinator reported Astra **PASS** for those corrected bytes.

**No display scale change, no selection of 200%, no execution of any of the 14
product contract checks, and no unchanged store-verifier run occurred.** There is
no Windows product PASS or readiness claim here. The fresh Ruling 179 execution
budget is not claimed by this preparation receipt.

## Goal and surfaces

Goal: commit a reusable, self-tested Windows runner and retained runner-ready proof.
Done when the six controls and R181 lifecycle are red-first/green, MSP is in the
base, the required non-product gates pass, and the result is committed locally.
Not in scope: display mutation, product checks, verifier execution, publishing.
Tier T1; fan-out 0; repair cap 2, consumed 2/2. This worker used its assigned tree.

Surfaces: process launch/job ownership → monotonic deadline → numeric exit → SDK
shutdown → job accounting/cleanup → PHN-checked output → proof metadata/manifest.
The read-only UIA preflight and before/after source guard are separate boundaries.
There is no product store/model/service/UI/compute change from this track.

The reusable library is `tools/windows-runner.ps1`. Its SDK checks adapt
`docs/proof/ring-windows/capture.ps1:14-29`. Its deadline helper adapts the remaining
Stopwatch-budget mechanism in `capture-calibration.ps1`; it does not parse an
instant. The existing `verify-capture-deadline.ps1` was inspected as the mutation
contract. The new runner has its own executable boundary and mutation tests.

## Retained observations

The retained invocation was:

```powershell
py -3 docs/proof/wri-r182-runner/capture-proof.py qualification
```

It executed `py -3 tools/check-windows-runner.py --self-test`, with the launcher
resolving an absolute Python executable. Complete producer stdout and stderr are
`qualification.stdout.txt` and `qualification.stderr.txt`; numeric process exit
**0**, outer wall **40,976.185 ms**, no timeout. The stderr capture is empty.
`qualification.json` binds output hashes, approved tool hashes, source state, and
the tested base. The producer explicitly sanitizes nested child text before
publication. These files preserve its exact emitted bytes; they are not a
reconstruction of an earlier run.

The pre-merge repair-2 run was observed only in tool session **57369**, completion
chunk **8c2f89**, exit **0**. **No filesystem stdout artifact existed for that run.**
Its reported policy suite cost was 11,772.8 ms and runtime cost 28,236.011 ms.
Those earlier observations are provenance only, not raw file evidence. The retained
post-merge run supplies the runtime evidence for this receipt.

| Control | Red observed in retained qualification | Green evidence |
|---|---|---|
| (a) No AppliedDPI/registry read | Planted access fails fixture 1 | Unmutated policy passes; no registry access in the runner/preflight surface |
| (b) Read-only UIA preflight | Expand, scroll, realize, select, Set-Content, StreamWriter constructor, second dot-source, and non-Preflight action all fail | Windows AST allows exact PropertyCondition constructors, read methods, and one approved dot-source; actual preflight script rejects a controlled absent-frame provider with exit 1; valid state fixture passes |
| (c) Stopwatch deadline | Parsed/mixed-kind deadline, hard-coded wait, added unbounded wait, and replaced wait fail | 899,750/900,000 ms boundary waits exactly 250 ms; 900,000/900,000 expires without waiting; startup/timeout/descendant fixtures return within their envelopes |
| (d) Toolchain identity | Wrong dotnet path, wrong Python path, and changed SDK predicate fail | Exact user-local `%USERPROFILE%\.dotnet\dotnet.exe`, SDK 10.0.203; absolute Python executable identifies itself as Python 3.13.14 |
| (e) Numeric child exit | Null/lost/nonnumeric exit, omitted/commented validation, and missing retained handle fail | Stub children return observed Int32 exits 0 and 3 with retained stable handles |
| (f) Zero source edits | One-byte tracked edit, untracked source, removed pre-child guard, and omitted after-child guard fail | Fixture baseline/final are clean and byte-identical; actual 313-file src/tests snapshots match before and after qualification |

Live Settings availability is **not assessed** by this run. The actual script's
absent-frame boundary uses a controlled provider; it does not prepare or mutate
Settings. Future live preflight fails closed unless the operator has already made
the required controls visible. The constructor and command mutants are parsed,
never executed.

## Lifecycle, cleanup, and publication

**Verified:** BuildVerifier mode emits this exact event sequence in its stub test:

```text
target-root-exit:0 > shutdown-start > shutdown-exit:0 > target-job-query > target-job-terminate > target-job-close
```

Shutdown is a numeric observed exit before any target-job query/termination. An
exit-3 shutdown fixture rejects, withholds the target residual query and completion
claim, and requests job-close fallback; the fixture independently observes the
descendant dead. Actual `dotnet build-server shutdown` also returned **0**. The
unchanged store verifier was not run.

Residual evidence is **job active-process accounting only**. No system-wide residual
sampler was invented or run. The Python qualification launcher reports its own
root exit and explicitly marks its descendant status **not assessed**.

| Runtime boundary | Observed post-merge result | Fixture ceiling |
|---|---|---|
| Assignment failure | Retained handle wait; root observed dead; 512 ms | 5,000 ms |
| Injected unassigned termination failure | Initial cleanup remains unresolved; bounded fallback observed complete; 556 ms | 5,000 ms |
| Root exits, descendant retains output | Root exit 0; one descendant; termination complete; 2,968 ms | 5,000 ms |
| Timeout cleanup | Rejected, raw publication false; 1,386 ms | 2,000 ms |
| Delayed startup | Rejected before child launch; 889 ms | 1,000 ms |
| Checker timeout | Wrapper exit 124, distinct observed child exit 1; root termination observed; PHN PASS | 750 ms plus 1 s cleanup allowance |

The assignment-failure path observes termination after `TerminateProcess`; an API
request alone is not completion. Its contract is the [Microsoft Win32 termination
documentation](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-terminateprocess).
Unresolved cleanup remains explicit. Raw child capture files are temporary;
published derivatives replace home, hostname, and SID values, bind substitutions
and raw hashes, and pass PHN before publication. The inherited-output fixture
proves all three substitutions. The checker timeout path publishes no raw traceback.

## Costs and source baseline

The **60,000 ms per-child end-to-end allowance is Inferred**, not measured capacity.
PR #21 measured its largest child (build) at 24.9227852 s and views at 11.188750 /
12.815539 s. Twice the largest timing, rounded upward to a 10 s boundary, is 60 s.
The runner reserves half the remaining envelope for cleanup, PHN, and final source
proof. Startup, root execution, descendants, and final checks share one Stopwatch.
These stub measurements do not prove a future eight-check views group will fit.

| Measurement | Ring | Measured post-merge cost |
|---|---|---|
| Standalone Windows AST policy | Fast policy; invoked by Ready preflight | 1,457.452 ms (`policy.stdout.txt`) |
| 19-mutant policy suite | Runner preparation | 11,667.007 ms |
| Native Windows runtime suite | Runner preparation; no product checks | 28,161.613 ms |
| Capture wrapper plus full qualification | Runner preparation | 40,976.185 ms |

Portable policy is lexical and does not claim Windows AST/runtime qualification.
Windows qualification requires PowerShell 7 on Windows x64. Product compatibility
on Windows/macOS is unaffected by this tooling-only addition.

`git diff --exit-code HEAD -- src tests` returned **0** before and after. The
313-file snapshot hash is
`5cf2087e5498c0a903c53146498a4f666d8307b6e7a68b60b0f62718db8f40cc`
at both points. MSP's already-merged test changes belong to the base, not this
track. Approved implementation SHA-256 values are bound by the closing manifest
and qualification metadata, including the runner hash
`db8a8872e503575b8c084d0f30d4c15e635defc0c7043e6a126c472d391c3961`.

## Gate contract

The retained wrapper accepts only qualification, policy, docs, PII, manifests, and
the exact configured verify-gates invocation. Commands and numeric exits are in
each named `.json` capture. The repository's invocation, with the Windows Python
name substitution, is:

```powershell
py -3 docs/ai-forward-pack/scripts/run-verify-gates.py --skip verify-application-core.py verify-application-adapters.py verify-windows-store.py
```

The coordinator relayed the Mac ruling that `tools/run-tests.sh` is not required
for runner-ready; the Mac join owns that ring. It was not run here. The PII capture
sets `CFD_PII_HOSTNAMES` from local `COMPUTERNAME` only in the process environment,
without printing or committing its value. Capture and closing manifests bind
retained bytes and the four approved implementation files. Raw text attributes
preserve producer line endings.

The initial `docs.json` and `verify.json` record exit **1**: the checker wrote its
two temporary AST files without an explicit LF setting and printed from a main
entry without the pack console guard. `pii.json` records exit **0**. These red
captures remain intact; they are not replaced by the later green captures.

## Ruling 183 portability track and owner review

The coordinator authorized a fresh, bounded portability track after the original
repair cap fired. It allowed **one local correction in the checker only**, within
15 minutes. It was not a third runner repair. The authorization is retained as a
quoted delegation record in `r183-delegation.json`. At packaging, the coordinator
authorized importing only `docs/notes/rulings.md` from
`3a139e3edba8788ab6f6cc2ca3802300e432a9c6`, the Mac-authored Ruling 183 register.
Its worktree Git blob hash and `origin/main:docs/notes/rulings.md` both equal
`514cd1995dd4ff10856571ed52dac8aef5773a05`. No other path from that commit was
imported and no later main was merged; the qualified base remains `be3900d5`.

The exact delta in `r183-checker.patch` adds `newline="\n"` to the only two
`write_text` calls, at lines 111–112, and copies the `__main__` stream guard from
`tools/qualify-windows-runtime.py:503-509`. No allowlist was added. The one-file
sweep and the repository portability gate found no remaining omission. The new
checker SHA-256 is
`8f044473b919558dbedd5a6e2a3ccd520025f81c6c48f31afb83d1501a48aa49`.
The runner, Settings preflight, and runtime self-test files are unchanged.

Execution preserved the required order: retain red files → one correction →
observe all three prerequisite gate exits as 0 → expanded qualification → freeze
delta and hashes → independent owner review. The coordinator reported Astra
**PASS** after matching the frozen hashes. Work started at 17:49:42 UTC and was
frozen at 17:53:58 UTC on 2026-10-09, within the 15-minute boundary.

| Corrected-byte command | Numeric exit | Measured wall | Retained evidence prefix |
|---|---|---|---|
| `py -3 docs/ai-forward-pack/scripts/verify-portable-text-io.py` | 0 | 558.009 ms | `r183-portable` |
| Exact configured verify-gates command above | 0 | 8,683.075 ms | `r183-verify` |
| `py -3 tools/check-docs.py` | 0 | 108,796.020 ms | `r183-docs` |
| `py -3 tools/check-windows-runner.py --self-test` after all three pass | 0 | 41,242.062 ms | `r183-qualification` |

Each prefix has complete producer `.stdout.txt`, `.stderr.txt`, and `.json`
metadata. The corrected qualification's policy mutant suite measured
**12,082.483 ms** and Windows runtime suite **27,979.805 ms**. It again proves all
six controls and R181, SDK 10.0.203, numeric stub exits 0/3, strict read-only UIA,
bounded descendant/assignment cleanup, and the shutdown-before-job-query order.
Its 313-file source baseline/final hash is the same hash recorded above, with both
src/tests diff exits **0**. Its stdout SHA-256 is
`092a698b3e12a3667c596b5579f71f181c0bd95441b8964b9585863867943e7d`;
stderr is empty. PHN passes before every capture publication.

`seal-proof.py` hashes staged Git blobs, including producer line endings, into
capture and closing manifests. The capture form binds this folder; the closing
form also binds the four implementation files. The repository manifest checker
checks committed `HEAD` blobs, so final committed-byte validation and push status
are reported in the coordinator handoff after the local commit. Final packaging
changes require documentation, graph, PII, manifest, and diff checks; they do not
require another product or scale run.

**Readiness, the fresh execution budget, and Windows product acceptance remain
closed.** A committed runner-ready receipt does not attest a future scale run.

## Repairs and residual limits

Cycle 1 corrected active guard bypasses, inherited-output cleanup, total envelopes,
and capture PII. Cycle 2 corrected exact UIA constructors/dot-source, asynchronous
assignment cleanup, R181 ordering, and checker timeout/error publication. Both were
reviewed separately; the coordinator reported Astra PASS for the final hashes.
The initial post-merge qualification required no integration deviation. Its later
repository gates exposed portability omissions, which stopped the original track
at its cap. The separately authorized R183 correction and requalification above
closed those omissions without editing the other three implementation files.

The before/after source guards detect a child mutation; they do not silently restore
it. Live UIA success, the 14-check scale contract, Windows product acceptance,
system-wide residual sampling, and readiness remain outside this evidence. A new
hard implementation defect stops the track at the consumed repair cap.
