---
id: review-windows-runtime
title: Independent Windows qualification review
type: proof-pack
status: in-review
owner: "@cfd-windows-review-20260925"
phase: application-foundation
tags: [windows, review, security, persistence, testing]
links:
  - {to: coordination-windows-runtime-route, rel: documents}
  - {to: note-m1-scope-decision, rel: depends-on}
  - {to: coordination-contract-b-core, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-25
summary: W0 preparation passed conditionally, but the first executed Windows qualification failed. Source-bound native receipts expose four failed cases and a DACL consumer refusal; product runtime, containment, durability, accessibility and timing remain unqualified.
review-suggested:
  - { by: coordination-windows-runtime-route, on: 2026-09-24, reason: "R43-R44 hosted route executed with failed native cases and a DACL receipt refusal; review route dependencies." }
  - { by: defect-classes, on: 2026-09-24, reason: "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions." }
---

# Independent Windows qualification review

**Current disposition: Windows qualification failed; production veto remains.**
The source-bound Windows run and its limits are recorded in the final section.
The W0 preparation verdict below is historical and does not override that result.

**Disposition: PASS-WITH-CONDITIONS for prepared qualification infrastructure.**
This permits an Owner decision on a concrete bounded Windows run. It does not
authorize dispatch itself, accept a Windows store, clear a native product veto,
or satisfy M1. The user-approved Windows and visible-timing M1 obligations remain;
full section-editor design and implementation are excluded from this session.

Goal: independently test the Ruling 40 packet's evidence and boundaries. Done
when its source, receipt controls, failure limits and Data/Security/Test verdicts
are recorded against one frozen commit. Not in scope: author edits, production
code, permission changes, CI dispatch, new dependencies or section editing.
Tier T2; global active cap three. Root owns this review in a separate registered
tree; the W0 author does not clear these verdicts. This is a review node in the
existing execution graph, not another implementation plan.

## Frozen subject and surfaces

Candidate: `82a366a83c052d47f8b855560ff0ee1cb352d8ab`, branch
`feature/windows-runtime-qualification`, clean when independently inspected.
Author tree: `/Users/mallalieut/projects/CFD-Workbench-feature-windows-runtime-qualification`.
Reviewer tree: `/Users/mallalieut/projects/CFD-Workbench-feature-windows-runtime-review`,
base `b8561a5d1b4f3c129eabb13bb87821e6b2c8133f`.

Six authored paths were inspected: `docs/design/windows-runtime.md`,
`docs/proof/windows-runtime.md`, `tools/spikes/WindowsRuntime/WindowsRuntime.csproj`,
`tools/spikes/WindowsRuntime/Program.cs`, `tools/qualify-windows-runtime.py`, and
`.github/workflows/application-windows-qualification.yml`. The other three changed
paths are official audit/index projections/registers. No product source changed.
These candidate paths are not yet joined into the reviewer's base.

The review surface is fixture bytes → native operation/identity/publication →
scenario receipt → consumer validator → source/binary binding → workflow and
process containment → proof. The accepted `IProjectStore` contract was read
directly: immutable input, expected disk hash, create-only publication and owned
cleanup remain unchanged. `DurabilityConfirmed` still requires its existing
documented file/final-directory flush meaning; W0 does not reinterpret it.

## Independently observed evidence

Raw final author run: `/tmp/cfd-w0-final-proof-20260925`. Root recomputed it using
`/private/tmp/cfd-w0-independent-recount.py` rather than accepting the summary.

| Observation | Result and limit |
|---|---|
| Candidate identity and state | Exact commit above; `git status --porcelain` empty |
| Source binding | Five actual source/workflow files match both manifests; canonical manifest digest `b6115c703aa1cf69d86da5b43ce9c7ce9bbce240844d6f8883c5e6cfa1747200` recomputed |
| Executable binding | Apphost, managed DLL, deps and runtimeconfig hashes independently match; composite `0ee79b74b9f38e7535cfda6d63167bfcfcdf34076530c82d98467976d4c8a13f` recomputed |
| Raw summary | SHA-256 `73197a1deb2dd0735ec91d4367c1a77d2d64354bcb9e4dbbbe59b5fd14963ae1` recomputed |
| Fixture fidelity | Exact independently specified A/B binary bytes retained unchanged |
| Actual local native result | 26 distinct rows, all `Not assessed` / `W0-UNSUPPORTED-HOST`; exact source/binary identities, publication false, durability false; exit 3 |
| Local build log | SDK 10.0.203 reported; zero warnings/errors; build exit 0. This is macOS compilation, not Win32 ABI execution |
| Local process receipts | Build PID 73152, 1.4989450420252979 s; native PID 73174, 0.36368895904161036 s; both receipts report no timeout and quiescence. These are historical local group observations, not independently replayed Windows descendant proof |
| Frozen self-test rerun | 35 refusal controls plus concluding self-test pass observed; native qualification remains Not assessed |
| Original adversarial reproductions | Wrong conflict `after` hash now raises `W0-WRONG-BYTES`; wrong cleanup `foreignAfter` now raises `W0-WRONG-cleanup-refusal-foreignAfter` |

No redundant compile was needed after recomputing the immutable source/binary
receipt and rerunning the affected controls. Root did not execute a Windows API,
launch UI, dispatch CI or touch system permissions. The existing-output negative
is retained in the author proof, not independently rerun here.

## Findings and resolution

1. **Major / Verified, resolved — partial receipt rows could lie.** Before the
   correction, root's synthetic conflict and foreign-cleanup rows carried wrong
   hashes and were accepted individually; other Not assessed rows kept the overall
   result false. Envelope validation was insufficient. Root observed both failures
   before repair. The fixed consumer checks case-specific state and the author
   swept cancellation, partial write, cleanup, DACL, aliases, links, sharing and
   flush fields. Independent positive examples plus field mutations prevent an
   always-rejecting implementation from satisfying the controls. The coordinator
   records this class as EVID-WIN-STATE; this review does not co-author the fix.
2. **Major / Verified, resolved — source binding ended before execution.** The
   earlier driver lacked a post-run source comparison. The final driver compares
   the same five inputs after execution; its copied-source mutation is rejected
   by the same guard. Root reran that negative and recomputed actual retained
   before/after hashes. A later binary/source change must produce a new run.
3. **Blocker for production / Flagged, contained — absolute-name and in-place
   reparse races.** Denying delete sharing and checking an ancestor handle is not
   an observed proof against converting the same empty directory to a reparse
   point. The current oracle attempts a rename, not that mutation. Microsoft
   documents the empty-directory condition and the reparse operation; this is an
   unexecuted attack hypothesis, not a reproduced exploit. The design explicitly
   excludes the guarantee and admits no production path subset. W2 must qualify
   a handle-relative strategy or prove and accept an exact safe alternative.
   Sources: [reparse restrictions](https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-points),
   [reparse mutation](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_set_reparse_point),
   [CreateFile sharing semantics](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew).
4. **Blocker for production / Verified gap, contained — durability equivalence.**
   No final-directory-flush equivalent has been established. The candidate always
   leaves durability false; known publication reports uncertainty. The receipt
   validator refuses to promote the directory-durability case to Pass. Owner/Data
   must resolve the exact Windows contract before production, with no silent
   weakening of save acknowledgement or compatibility.
5. **Major / Verified gap, contained — Windows behavior is unexecuted.** Win32
   structure guards, creation-time DACL inspection, job assignment before resume,
   retained process identity, job-only termination and absence checks are prepared
   source. Their real ABI, sharing, denial, race, cleanup and UIA behavior require
   Windows execution. No native success is inferred from this review.

The source review checked the explicit UTF-16 API calls, 24-byte security
attributes, 52-byte file information, x64 rename offsets, one-byte disposition
field, handle ownership and descriptor cleanup. These checks support executing
the experiment; only a Windows run can qualify the ABI and semantics.

## Independent veto dispositions

**PERSONA: data-persistence-architect · MODE: Adversary · TIER: T2**

**VERDICT: PASS-WITH-CONDITIONS, preparation only.**
No persisted product schema or invariant is changed. The spike explicitly
separates publication from durability and rejects unsupported paths rather than
installing a fallback. **CLEARS-THE-VETO: yes for this isolated preparation;
no for a Windows product store.** Production containment, full project identity
and history/recovery round trips, overwrite ACL policy and durability remain open.

**PERSONA: security-identity-architect · MODE: Adversary · TIER: T2**

**VERDICT: PASS-WITH-CONDITIONS, preparation only.**
Trust boundaries are named; synthetic fixtures stay in unique disposable roots.
No privileges, credentials, global ACLs, registry settings or trust settings are
changed. The workflow is manual-only with an exact branch guard, contents-read
permission, disabled persisted checkout credentials, pinned action revisions,
task-local tooling and finite job/artifact retention. **CLEARS-THE-VETO: yes for
preparation; no production security claim.** Before dispatch, Owner must approve
the concrete ref/trigger route and verified access/cost. The current file alone
does not establish dispatch availability. Native rights, containment and process
observation must be measured; failed observation stays nonzero.

**PERSONA: test-architect · MODE: Adversary · TIER: T2**

**VERDICT: PASS-WITH-CONDITIONS, preparation only.**
The populated proof distinguishes Prepared/Compiled/Executed/Not assessed.
Root observed two false accepts and their rejection after repair; 35 refusal
controls and exact retained evidence bindings pass. **CLEARS-THE-VETO: yes for
the stated local receipt/preparation claims; no Windows or M1 acceptance.**
Native tests, process-tree cancellation, UIA/Narrator, reference-host visible
latency and integrated product workflows remain unexecuted or separately gated.

## Next and measurement limits

Owner may now decide the exact W1 branch/workflow run under existing authorized
testing scope. Independent review of actual returned Windows evidence precedes
any W2 production admission. A red/Not assessed directory-durability outcome is
expected evidence, never an excuse to mute the gate. W3 native accessibility and
visible timing remain separate obligations even if the UIA capability probe runs.

Review start marker: 2026-09-25T04:28:36Z. The closing audit measures the span,
including waiting for the frozen handoff; earlier read-only review preparation
is outside that marker. Token usage, effective billing model and monetary cost:
**not recorded**. No full section-editor work is authorized in this session.

## Portability correction, 2026-09-25

The integrated tooling gate subsequently found five failures in the otherwise
reviewed preparation: four text writes lacked an explicit LF newline policy and
the printing CLI lacked its legacy-console UTF-8 guard. Root independently ran
`verify-portable-text-io.py` against the joined candidate and observed exit 1 with
those exact five findings. The earlier local documentation check did not cover
this applicable tooling floor. The Windows run remained held.

The separately authorized one-file correction is clean commit
`529bcb9693c04b535f4421645eb98a7e117a4147`, in
`/Users/mallalieut/projects/CFD-Workbench-feature-windows-w0-portability`.
Root inspected the exact delta: four `newline="\n"` additions, guarded UTF-8
stdout/stderr configuration, and a real bounded subprocess `--help` control.
Native C# and the workflow are unchanged. The help control preserves argparse's
real process exit; the author's earlier intercepted-exit probe accidentally
continued into a local macOS qualification run. That unexpected run is disclosed,
not presented as Windows evidence or a required repeat build.

Root independently reran both `verify-portable-text-io.py` and
`verify-subprocess-utf8.py`: **PASS**. The 35 receipt refusal controls plus the
help-exit control also passed; raw independent output is
`/private/tmp/cfd-r42-independent-controls.txt`. Qualifier SHA-256 was recomputed
as `4a0ea88907fcd8ff58ba38b1846af7b6ee1a488fc4412b97f55b671b1d4a724d`.
Author correction receipts are retained at
`/tmp/cfd-w0-portability-proof-20260925`; its source check explicitly rejects
rebinding the older native receipt to the new qualifier.

**Delta disposition: PASS for prepared qualification.** The previous
Data/Security/Test conditions still apply. This clears the exact tooling repair
for one corrected integrated retry; only that run can establish the integrated
gate outcome. Root did not rebuild the native spike. The old full-run manifest
remains evidence for `82a366a`, not these changed Python bytes. The forthcoming
Windows run must bind fresh source, binaries, fixtures and results.

Class → sweep → prevent: script-producing packets must include the installed
portable-text and subprocess-encoding gates before handoff; documentation-only
checks cannot stand in for them. The existing executable gates caught this class
before any Windows dispatch. Do not repeat all integrated gates while the same
known-red source remains unchanged. The coordinator owns the shared register.

The addendum marker starts after the bounded source/control review; its measured
duration covers documentation finalization only. The preceding delta-review
elapsed time, tokens and monetary cost are **not recorded**.

## First Windows execution and workflow correction, 2026-09-25

Goal: independently review the exact dispatch correction and returned Windows
evidence before any production admission. Done when the observed failures,
receipt limitations and next decision are recorded. Scope remains the existing
review path; no source repair, new dispatch or section-editor work.

The first authorized push, commit `906714b1d9f76d0542385889f31b4f5809b3deb1`,
produced [run 36097138344](https://github.com/timianmalloo/CFD-Workbench/actions/runs/36097138344).
GitHub rejected three `runner.temp` expressions in job-level `env` before any
job started. Root observed the exact annotation, zero jobs and zero artifacts.
The [GitHub context table](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts)
allows `runner` in step-level `env`, not job-level `env`. Author, coordinator,
Owner and root all missed this semantic workflow check. A local documentation
gate or clean YAML-shaped diff had not established executable workflow validity.

Owner authorized a workflow-only correction and one retry. Root inspected clean
commit `7f34c13c3568ec31779866e061eb0a7d52dbfb36`: only the three environment
values moved to the SDK setup, self-test and native-driver steps. The exact
branch/ref, read-only permission, action revisions, SDK version, 12-minute job
timeout and three-day retention stayed unchanged. All four non-workflow source
hashes matched the previously reviewed inputs.

The official task-local `actionlint` 1.7.12 binary, SHA-256
`8db11704dc296f096216db4db65d86cd7f0ebfdf4c38453a1da276b137b88388`,
reported the original three context errors with exit 1 and accepted the corrected
workflow with exit 0. Root independently ran the same binary against the fixed
bytes. Its downloaded archive and checksum manifest matched the official release
digests. This semantic check is the recurrence control; no global dependency was
installed. Coordinator owns the shared defect-class and prelaunch records.

### Actual run and retained evidence

[Run 36097839626](https://github.com/timianmalloo/CFD-Workbench/actions/runs/36097839626),
attempt 1, executed the exact fixed SHA on `feature/windows-w1-qualification-20260925`.
Root independently read job `107953763808`: started `2026-09-25T05:16:06Z`,
completed failure `05:16:46Z`. The 40-second job interval is measured from those
timestamps; it is neither billed usage nor application latency. The host reports
Windows Server 2022 build 20348, image `20260920.314.1`, AMD64/X64 and NTFS.
SDK `10.0.203`, the receipt self-tests and native compilation ran. Compilation
reported zero warnings/errors. The native step failed; artifact retention passed.

Raw retained packet:
`/tmp/cfd-w1-run-36097839626/download/w0-7f34c13c3568ec31779866e061eb0a7d52dbfb36-1/`.
API/job metadata and full job log are one directory above it. Artifact ID
`10847997532` reports archive digest
`b303e6ff9f4f4e9942f2d696b7af1fc5462bd1ff10cec436446c96772ecf2729`;
this is the API's value, not an independent archive-byte recount.
Root's read-only recount is `/private/tmp/cfd-w1-independent-review.py`, output
`/private/tmp/cfd-w1-independent-review.txt`.

Root recomputed all five actual source hashes against the initial manifest and
canonical manifest digest
`78e5585a9dcc9db5344e28b0fdfbd5f00d4f70fe085a4a54999ccab5934b8e5d`.
Both fixtures match their exact prescribed bytes. Native stdout SHA-256 is
`436f50f0163bbd408a539fa7cf1e75e429b2d92d321eb9a682e2a66e0ff26b54`;
native process receipt SHA-256 is
`2bb0932d17b8d0d3190b37025e36cd31bc12f28962f29770d5cc7cadefa5d030`.

The native executable emitted 26 unique cases: **21 self-reported Pass, four
Fail, one Not assessed**. This is a recount of native assertions, not 21
independently accepted cases: the consumer rejected the packet.

| Case or boundary | Observed evidence | Disposition |
|---|---|---|
| Overwrite | `nativeError: 5`, publication false, `W0-WRONG-SAVE-STATE` | Failed; replacement semantics require investigation |
| Cancel after publication | Same native error and failed state; cancellation was not requested because publication did not complete | Failed; post-publication cancellation was not reached |
| Replacement DACL | `Access is denied.` | Failed; exact native cause remains unresolved |
| Ancestor substitution | `W0-ANCESTOR-MOVED` | Failed oracle; not proof that a directory moved, because the failure row omits the move result and Win32 error |
| Creation DACL consumer | Native output is `O:LAD:P(A;;FA;;;LA)`; Python requires a numeric `S-...` owner/ACE string | Root replay reproduces `W0-WRONG-DACL`; textual representation mismatch, not proof of a permissive ACL |
| Directory durability | `W0-UNSUPPORTED-DIRECTORY-DURABILITY`, durability false | Still Not assessed; no final-directory equivalent established |

The native DACL check inspects protected DACL, one non-inherited allow ACE,
current-user SID and the exact mask before payload. That source fact explains
what its Pass asserts; it does not make the separate Python regex correct.
No repair may simply accept arbitrary SDDL strings or mute the four native
failures. The ancestor oracle must preserve operation result and error before
throwing; its current message conflates distinct outcomes.

### Receipt limits and independent verdict

The validator aborts before the timeout-tree, observer-fault and UIA probes.
There is no final `summary.json`, post-run source manifest or binary-file
manifest. Native rows report composite binary identity
`cbb688e1ccf739eb47552d5d9c0b42a8113b1d3f0d67a1455602df6f673ad2b6`;
the historical process observer reports apphost hash
`6047540c859a93a9bbdbf4fa03872c49111b12a3ee6a669ee6e385f0e6bb7554`.
The workflow did not retain actual binary bytes. Root therefore cannot recount
the composite against those bytes or establish final source stability. A source
hash present in a row is not equivalent to those missing checks.

The four retained SDK/info/build/native process receipts report no timeout,
quiescence and no cleanup error, with PID/start/executable/hash observations.
Build includes compiler and console-host observations. Native PID 6116 ran for
0.171 seconds and exited 3; build ran for 4.954 seconds and exited 0. These are
historical runner measurements, not a locally repeated job-containment proof.
The required forced timeout and observer-failure cleanup controls did not run.
No hosted UIA, Narrator, Workbench window or displayed timing evidence exists
from this run.

**Data/Security/Test: FAIL for Windows qualification and production admission.**
The experiment successfully reached real Windows and falsified candidate
assumptions. Its failure is distinct from the earlier workflow rejection and
the expected durability gap. The source and evidence are retained; there is no
automatic third run. Owner must define an evidence-led repair/diagnostic scope
before another author or dispatch. M1 requirements remain intact.

Class → sweep → prevent: semantic workflow context checking is now a prelaunch
control. Separately, candidate native failures must retain discriminating raw
results, and downstream receipt/probe evidence must not disappear behind the
first validator exception. Those latter repairs are findings for the next Owner
packet, not silently implemented by this reviewer. Root also corrected a local
metadata-path lookup by file discovery after two nonexistent registry names;
that failed lookup produced no state change or evidence claim.

The review marker starts at `2026-09-25T05:16:19Z`; it includes waiting for the
returned packet and documentation, and excludes earlier prepush review.
Effective billing model, token usage and monetary cost: **not recorded**.
