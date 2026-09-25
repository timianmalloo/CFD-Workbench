---
id: review-visible-presentation
title: Independent visible-presentation preparation review
type: proof-pack
status: in-review
owner: "@cfd-visible-review-20260925"
phase: application-foundation
tags: [performance, review, privacy, testing]
links:
  - {to: kb-visible-presentation, rel: depends-on}
  - {to: note-m1-scope-decision, rel: documents}
  - {to: coordination-application-build, rel: relates-to}
review-by: 2026-10-25
summary: Frozen preparation passes its independently replayed synthetic contract and clock controls. No native capture or visible-performance acceptance is granted; whole-package identity and the native observation contract still require preparation.
---

# Independent preparation review

**PASS-WITH-CONDITIONS for source-only preparation; not ready for a native
timing acceptance run.** No target window, alternate capture, permission query
or operating-system input was executed by this reviewer. All M1 visible budgets
remain unassessed. Full section editing remains outside this session.

Goal: independently inspect the frozen measurement contract and its actual
noncapture evidence. Done when Test/SRE/Data/privacy verdicts and the smallest
remaining native obligations are recorded. Not in scope: implementation edits,
capture authorization or execution, product changes, reference-host performance
trials or section editing. Tier T2 independent review; global active cap three.
The reviewer did not author the spike and does not clear its own implementation.

## Frozen subject and changed surfaces

Clean author commit `e688c182ece5eb04e04385bfd1c7ab245d5f886c`, branch
`feature/visible-presentation-spike`, tree
`/Users/mallalieut/projects/CFD-Workbench-feature-visible-presentation-spike`.
Root review tree is `feature/visible-presentation-review`, based on `062d14d`.
The six named artifacts are `docs/design/visible-presentation.md`,
`docs/proof/visible-presentation.md`, the three files under
`tools/spikes/VisiblePresentation/`, and `tools/qualify-visible-presentation.py`.
Three additional changed paths are official audit/index outputs. The Owner's
prose counted seven; the explicit list and delivered scope contain six.

The reviewed chain is synthetic state model → three rendered-region definitions
→ prepared observer → receipt model → calibration and interval evaluation →
proof. The native pixels-to-receipt bridge is absent and is explicitly unqualified.
Python fixtures and C# state checks do not prove that bridge. No product
representation, project schema, geometry or revision history changed.

## Independently observed evidence

Root inspected the entire Swift helper and target source, the actual Python
consumers and tests, design/proof, and retained raw evidence. Raw author receipts
remain under the author's ignored `spikes/visible-presentation/` directory; do
not discard that tree or scratch while the evidence is required. Root's recount
script is `/private/tmp/cfd-visible-independent-review.py`, with output
`/private/tmp/cfd-visible-independent-review.txt` and mutation outputs under
`/private/tmp/cfd-visible-independent-evidence/`.

| Check | Observation and limit |
|---|---|
| Five source hashes | Recomputed qualifier, C#, Swift, project and global SDK hashes; match the frozen proof and self-test manifest |
| Five binary/artifact hashes | Recomputed Swift helper, target apphost, DLL, deps and runtimeconfig; match proof. Helper is Mach-O arm64; it was not executed |
| Synthetic qualifier | Independently reran all 50 cases; output is byte-identical to author receipt SHA `4ee48ade0991bf0d11d86cc042949e96e5caad7564e77a1f1c13dc473ba62334` |
| Target model | Ran the actual apphost with `--contracts`; returned before Avalonia UI startup. Three regions have generation 3/value 120/Preview and late cancelled generation refusal; output byte-identical to receipt SHA `c5bede10f100e78c61c9c2b5614b058875a90ad1b25bea79d8fa80e8e825a1e6` |
| Four wrong-result mutants | Inspected each exact one-line change and independently reran it. Each exits 1 with named failing assertions; not merely an exception |
| Calibration | Recomputed all 32 retained pairs with rational arithmetic: quantization `128/3` ns, offset intersection `[-128/3,42]` ns. No trial-duration drift or cross-process equivalence inferred |
| Standalone clock refusal | Independently replayed the disjoint-bracket file: `VP-CLOCK`, exit 3 |
| Portability | Independently reran portable-text and subprocess-UTF8 gates: clean |
| Post-review source state | Source hashes unchanged and author worktree still clean |

The four mutation failures discriminate envelope identity, actual region content,
transient-final regression and uncertainty. The identity mutant fails all four
envelope-only cases; the regression mutant fails both regression cases. The
uncertainty mutant fails the independently specified interval and boundary cases.
They are four targeted controls, not a complete mutation score or native proof.

Root read the .NET build log: SDK 10.0.203, zero warnings/errors; its workload
verification warning is retained. The Swift log is empty. An empty log alone is
not a compiler-exit receipt; root observed the resulting arm64 helper and its
hash, but did not repeat Swift compilation. The proof's compile provenance is
therefore author/tool-reported, not a new root compile observation.

## Findings, fixes and limits

1. **Verified, corrected: negative-fixture aliasing hid a missing guard.** The
   first mutation result exposed shared envelope/frame identity objects. Each
   frame now owns a copied identity, with separate frame mismatch cases. Root
   inspected the correction and observed all four mutants fail. Coordinator
   records the class and executable recurrence control.
2. **Verified, corrected: different clock consumers disagreed on refusal.** Root
   supplied disjoint calibration brackets; the earlier standalone consumer
   returned success with a null intersection while the main assessor refused.
   The final consumer raises, the CLI returns `VP-CLOCK`/3, and case 50 prevents
   recurrence. Root observed the failing result before repair and replayed the fix.
3. **Verified, corrected: printing CLI omitted the portable-console guard.** The
   installed gate found it before handoff. The final UTF-8 guard and explicit
   reads pass both applicable tooling gates. Documentation checks alone were
   insufficient; the existing gates remain required.
4. **Verified gap, blocks a concrete native request: target identity is incomplete.**
   The helper verifies the apphost path/hash, bundle, PID and launch time before
   lookup. It does not bind the managed DLL/dependency closure or recheck process
   identity across observation. The four target hashes in the proof are useful
   preparation, not an implemented whole-package runtime identity contract.
5. **Verified gap, blocks visible timing acceptance: capture is not presentation
   qualification.** A desktop-independent window stream does not establish
   unoccluded physical visibility. Callback numbering cannot establish no lost
   display events. The helper does not prove state persistence between samples,
   cross-process calibration/drift, external cold/input boundaries or observer
   overhead. Native mode always refuses in the qualifier, which correctly
   prevents synthetic success from becoming a performance claim.
6. **Native-unexecuted obligations:** actual crop mapping, semantic decoding,
   blank/denied/revoked capture, startup/stop races, stream cleanup, real target
   replacement and timeout/cancel. The source has finite duration, exact target
   filtering, no image/audio file retention, no network/children, and explicit
   failure paths. These are reviewed source properties, not runtime guarantees.

The prepared helper's one-time shareable-content lookup touches window metadata
to locate the exact target, though it does not log other windows or capture them.
Any later user authorization must accurately describe that lookup and target-only
pixel observation. Whole-package binding and an exact measured window/crop request
must first be concrete. Do not treat existing TCC permission, CUA permission or a
successful compile as permission to run it. Root did not execute the helper's
permission preflight or even its no-argument refusal path.

## Independent dispositions and next boundary

**Test Architect — PASS-WITH-CONDITIONS for preparation.** The frozen synthetic
claims are observed and falsifiable; rendered and native failure claims remain
unassessed. No M1 or visible-latency veto is cleared.

**SRE — PASS-WITH-CONDITIONS for preparation.** Exact clock units, raw brackets,
callback/event separation and unknown drift/loss are explicit. Every future
attempt must stay in the denominator. Reference-host/workload budgets and
native cleanup remain open; no performance or resource-lifecycle acceptance.

**Data & Persistence — PASS for isolated measurement modelling.** Identity,
generation and content are separated from derived timing bounds; no second
product truth is created. This does not admit the missing native receipt bridge.

**Security/privacy — PASS-WITH-CONDITIONS for source preparation; native launch
withheld.** No alternate capture execution occurred. Full runtime identity and
an exact target/request plus specific user authorization remain prerequisites.

**UX & Accessibility — no product UI change; native implication unassessed.**
The synthetic bit fields are measurement fixtures, not a replacement workbench
or an accessibility demonstration. No current UI veto is cleared by them.

**Simplifier — preparation scope retained.** No renderer fork, forced backend,
extra package or product feature was introduced. Resolve the concrete remaining
identity/observer feasibility seam through an Owner-scoped increment; do not
expand this into generic screen tooling or silently change the M1 endpoint.

Root's measured review marker starts `2026-09-25T05:29:44Z`, includes waiting
for the frozen handoff and documentation, and excludes the earlier draft reads.
Requested/effective billing identity, tokens and monetary cost for this root
review: **not recorded**. The author's separately measured 1,085 seconds and
56/60 reported operations are not root's usage.
