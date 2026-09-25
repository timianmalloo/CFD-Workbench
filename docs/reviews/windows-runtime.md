---
id: review-windows-runtime
title: Independent Windows W0 qualification review
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
summary: Independent review admits the frozen W0 packet only as disposable qualification infrastructure. Local receipt and binding controls pass; Windows execution, production containment, durability, accessibility and timing remain unqualified.
---

# Independent W0 review

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
