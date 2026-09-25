---
id: review-visible-presentation
title: Independent visible-presentation preparation review
type: proof-pack
status: in-review
owner: "@cfd-visible-identity-review-20260925"
phase: application-foundation
tags: [performance, review, privacy, testing]
links:
  - {to: kb-visible-presentation, rel: depends-on}
  - {to: note-m1-scope-decision, rel: documents}
  - {to: coordination-application-build, rel: relates-to}
  - {to: design-visible-presentation, rel: documents}
  - {to: proof-visible-presentation, rel: depends-on}
review-by: 2026-10-25
summary: R47 preparation passes independent package, pure request and identity replay. The concrete private package closure is established; native identity, geometry, lifecycle and on-screen timing remain unmeasured and no capture execution is authorized.
review-suggested:
  - { by: coordination-application-build, on: 2026-09-24, reason: "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims" }
  - { by: design-visible-presentation, on: 2026-09-24, reason: "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims" }
  - { by: proof-visible-presentation, on: 2026-09-24, reason: "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims" }
---

# Independent preparation review

**Current disposition: see the R47 review below.** The following R41 findings are
historical; R47 closes its concrete private-package inventory gap, while retaining
the native execution and timing gates.

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

## R47 independent review, 2026-09-25

**PASS-WITH-CONDITIONS for the frozen preparation packet; native timing and a
concrete capture request remain unready.** Subject: clean author
`657cd8c87a422ed69685311b1206406536c5734d`, exactly five authored paths under
[Ruling 47](../notes/rulings.md#ruling-47--prepare-whole-package-timing-identity-and-durable-noncapture-evidence).
Root reviewed in isolated `feature/visible-identity-review` from `112a614`.
Only this review and official audit/index outputs are reviewer-owned.

Goal: independently verify the actual private package, request/identity contract
and durable evidence. Done when Data/Security/Test/SRE dispositions and exact
remaining native obligations are recorded. Not in scope: capture, permission
preflight, target UI, product timing, Windows or section implementation. Tier T2;
global active cap three. The surface chain reviewed is published assets/deps →
manifest → request → Swift identity/geometry reader → receipt → assessor/proof.
No product representation or source was changed by the reviewer.

### Observed replay

The two durable capsules decode to exactly 253,143 and 49,728 bytes and match
their committed SHA-256 values:
`711b114d58ca1c23ee67292402d5a0517aa82dcb1030d388ff0744e6f312d30d` and
`dee76d7754ddfa32d4e5f87dd07188b9fab489b86c08bb586a2a883bc62f9497`.
Root checked every nested retained-log digest, source/helper/DLL binding, all
31 published file hashes and all 27 unique deps mappings against actual files.
The selected RID is `osx-arm64`; its empty portable target is not a competing
asset resolution. No loaded-memory attestation is inferred from disk hashes.

| Independent check | Actual outcome | Boundary |
|---|---|---|
| Synthetic assessor | 50 cases, exit 0, byte-identical replay | Native mode remains refused |
| Swift pure request parser | 14 cases, exit 0, byte-identical replay | Synthetic PID/window/crops only |
| Actual Python package reader | Six cases, exit 0, byte-identical replay | Positive, changed managed bytes, missing native/required files, extra file, symlink |
| Swift pure identity comparator | Eight cases, exit 0, byte-identical replay | Equality/nil inputs; no AppKit/process/window lookup |
| Actual target model | Exit 0, byte-identical three-region state/late-Cancel result | Returns before Avalonia startup |
| Actual self-process identity | Exit 0; equal PID/start/executable values, stable true | Self-process only; cross-API precision unmeasured |
| Four source mutants | All exit 1 with named failed assertions | Identity, content, regression and uncertainty; each exact source substitution checked |
| Arm64 load commands | Reran `otool -arch arm64 -l`; exact retained stdout match | 31 actual imports all `/System/Library` or `/usr/lib` |
| Clock operands | Independently recomputed 32 rational intervals | Quantization `128/3` ns, intersection `[-128/3,42]` ns; no trial drift claim |
| Tooling/source stability | Portable-text and subprocess-UTF8 clean; source/helper/DLL hashes unchanged; author tree clean | No new native compiler or rendered-state claim |

The `/usr/local/lib/libAvalonia.Native.OSX.dylib` entry is `LC_ID_DYLIB`, not an
external import. Root inspected load-command kinds separately from names; the
other install names are `@rpath/libSkiaSharp.dylib` and
`@rpath/libHarfBuzzSharp.dylib`. The private static dependency inventory is
established for this package. OS libraries and the installed framework-dependent
.NET runtime are the explicit system trust boundary. A later launch must bind
its loader/environment context; this does not require attesting all system memory.

Recount scripts: `/private/tmp/cfd-r47-independent-review.py` and
`/private/tmp/cfd-r47-native-clock-review.py`; raw outputs are under
`/private/tmp/cfd-r47-independent-evidence/`. Its main report SHA-256 is
`0e8736bd577ce9304ac744cb9fd3d17f313bf4d498d0ec085b13c446e272238b`.
Independent load-command stdout SHA-256:
`3797fe9d7eaaba974fd2f28e01bb74c175655a120854ebaa87f667707cd27bb8`.
The source proof's capsules preserve the replay operands if scratch disappears;
the table above is a derived review, not a claim to embed every raw process byte.

Reviewer correction: the first local exact-clock calculation omitted the
documented quantization allowance and failed its own intersection assertion.
Root re-read the consumer's unit/uncertainty contract, included both clock quanta,
and compared the resulting rational values directly to the retained analysis.
This was a reviewer-oracle error, not a reported product defect. The correction
is explicit so no false exact-clock conclusion survives. Coordinator owns the
shared-register recurrence note and V16 propagation at integration.

### Dispositions and next permitted boundary

**Data/Security: preparation PASS.** The concrete package closure and immutable
request shape are verified to the stated boundary. Helper source rechecks
process/start/executable/bundle, package/helper/context bytes and window geometry
at the named points. Those native reader calls remain unexecuted; pure equality
tests do not qualify them. In particular, Swift's real file guard and process
observations need an appropriate later runtime check, not a Python-reader proxy.

**Test/SRE: PASS-WITH-CONDITIONS for preparation.** Real pure entry points and
negative controls were replayed, with no target, permission query, capture or
OS input. The startup/callback/stop races, crop decoder and presented-state bridge
remain unverified. The observer hashes the package on its main callback queue;
observer overhead and a bounded outer process/cleanup contract must be established
before using native results. The queued timer alone is not proof of a hard
deadline while synchronous work runs. No new native defect is inferred without
execution, and no performance veto is cleared.

**Privacy/UX/Simplifier: retain the narrow feasibility scope.** There is no new
product UI or section editor. The helper's metadata lookup and transient
target-only full-frame buffers must be disclosed in any later specific user
request; no other-window/audio retention or egress is proposed. Existing OS
permission and broad implementation authorization are not capture authorization.
The author disclosed incremental compile/fixture corrections within its bounded
window; root did not mislabel that history as one atomic correction.

Next Owner packet should settle the smallest runtime evidence sequence: bind the
reviewed loader context and owned process identity, obtain actual target geometry
through the supported review route, and prepare one exact finite request plus
outer timeout/cleanup evidence. Batch the necessary specific capture authorization
and any genuinely unavailable Windows/reference-host information once reviewable.
Keep Windows implementation dependency-ready work running while waiting. Do not
ask for window focus repeatedly, collect product budget trials against an
unqualified observer, expand to generic capture tooling, or start the full section
editor. All on-screen M1 budgets, visibility/loss/drift and reference-host proof
remain **Not assessed**.

Root's marker `2026-09-25T06:20:24Z` measures the frozen review run and excludes
earlier read-only grounding. The author's 867-second run is separate. Effective
model, tokens and monetary cost: **not recorded**.
