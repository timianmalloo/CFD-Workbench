---
id: design-visible-presentation
title: Visible presentation endpoint feasibility contract
type: design
status: in-review
owner: "@cfd-visible-endpoint-20260925"
phase: application-foundation
tags: [performance, presentation, spike]
links:
  - {to: kb-visible-presentation, rel: depends-on}
  - {to: note-m1-scope-decision, rel: refines}
  - {to: proof-visible-presentation, rel: tested-by}
review-by: 2026-12-24
summary: A source-bound synthetic target and fail-closed receipt model test endpoint feasibility. A target-only native observer is compiled but unexecuted; no visible latency or physical visibility is qualified.
---

# Visible endpoint feasibility

## Goal and authorization

Goal: investigate an honest endpoint for the approved M1 visible timing budgets.
Done when the bounded source packet, executable noncapture negatives, clocks and
remaining native seams are reviewable. Not in scope: product source, renderer
selection, capture execution, enumeration, TCC changes, native UI control,
performance trials, full section authoring or M1 acceptance. Tier T1; active cap
three (root, Coordinator, author). Requested author model gpt-6-astra; effective
model identity **Not recorded**.

[Ruling 41](../notes/rulings.md#ruling-41--prepare-a-truthful-visible-endpoint-spike-without-alternate-capture-execution)
is the contract. Its list contains six paths despite saying seven. Coordinator
confirmed the explicit six-path list governs; no invented seventh artifact.
Official audit/index outputs are derived separately. Base `f1a4e2c8a17c`;
branch `feature/visible-presentation-spike`.

The endpoint constrains which future measurements could establish cold launch
<=5 s, numeric feedback p95 <=100 ms, Preview p95 <=250 ms and Cancel <=250 ms.
The fixed product workload and 16 GB reference-host pair remain unchanged.

## Execution graph and rigor floors

The received delegation is the compiled scope; no extra feature is inferred.
The local optimize-graph pass collapses source/contract discovery into one node.
No new agent is spawned. The unresolved contracts and fixed negative cases form
the declining work set; 60 tool calls / 40 minutes is a circuit breaker, not an
acceptance criterion. One implementation plus one evidence-directed correction.

| Node | Capability | Input / exit oracle | Dependency |
|---|---|---|---|
| A ground API and intent | Reasoning | R41 + pinned sources; explicit supported seams and refusals | none |
| B contract and red cases | Reasoning | A; initial missing validator fails | data A |
| C target/helper/parser | Reasoning | B; exact source compiles, noncapture contracts hold | data B |
| D qualify and mutate | Deterministic mechanics | C; negative matrix and wrong-result mutants produce expected results | data C |
| E proof/index/docs | Deterministic mechanics | D; hashes, limitations, documentation gate | data D |
| F independent root review | Independent review | E; Data/Security/Test disposition, author cannot clear veto | decision E |

Before/after: six nodes, serial span six dependency stages, local width one,
one bounded corrective pass. D/E are deterministic; F is independent review,
not a tool gate. Work/time estimates are **Not recorded**; no unsupported speedup claim.
Red-first, failure matrix, surface tracing, source binding and independent
review are immovable. Outcome and measured duration belong in the proof/audit.

## Endpoint comparison

**Verified source:** Avalonia 11.3.14 is pinned in the existing spike and this
project. Its [initializer](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/src/Avalonia.Native/AvaloniaNativePlatformExtensions.cs)
tries OpenGL, Metal, Software. The selected runtime backend is **unobserved**.
No renderer is forced, no private reflection or framework fork is used.

The inspected public `Compositor.RequestCommitAsync` documents completion when
changes are applied on the render thread. The inspected native `avn.idl` is
internal and provides no drawable-presented callback in its Metal target
interop. This is evidence about these inspected surfaces, not a proof that all
possible Avalonia extension APIs lack such a route. No supported consumer
presentation endpoint was established. The target therefore reports backend
`unobserved`, never inferred from the option order.

**Verified SDK contract:** ScreenCaptureKit `displayTime` is a WindowServer
frame event in mach absolute units; frame callback receipt is separate. Metal
`presentedTime` uses host seconds and zero for unpresented/skipped drawables.
Neither is panel-photon timing. See the [primary ledger](../knowledge/visible-presentation/sources.md).
The prepared helper uses `SCContentFilter(desktopIndependentWindow:)`, so its
pixels do not establish physical unoccluded visibility. `isOnScreen` alone is
also insufficient. This route can investigate target-window content only.

## Domain, grain and change surfaces

This is an isolated measurement bounded context. `TrialIdentity` is a value
object comprising exact process, launch identity, window and package. A trial
aggregate contains immutable expected region states plus ordered append-only
observations. Its invariant is that no observation from another process,
operation, source or draft generation can qualify the trial.

One raw row is exactly one frame callback or one explicit terminal outcome.
Region values are snapshot facts; a new generation appends, never overwrites
the meaning of a previous fact. Frame counts are additive; raw timestamps,
latency bounds and p95 are non-additive. No persistence migration or production
model changes occur. Bounds are derived once from retained raw observations.

Surface list: state writer `TargetState` -> three `Region` values ->
`TargetSurface.Render` -> pixels -> prepared Swift region decoder -> raw JSON
-> future native bridge -> qualifier -> proof. The native bridge and real
pixel/semantic correlation are **Not assessed**. Python fixtures directly feed
the qualifier and are explicitly synthetic, not substitutes for that bridge.
The C# state and Python receipt fixtures are different independent fixtures;
their successful tests do not assert a cross-language wire integration.

The visual target consists of three full 16x16 black/white bit fields. Each
entire field is synthetic content derived from SHA-256 of a region's role,
operation, source, draft, generation, state and numeric value. There is no
separate completion marker capable of qualifying unchanged region content.
Inspector/status/canvas are logical synthetic regions, not a product UI or
proof of its accessibility or content. A future product oracle must compare
actual rendered values and geometry in every required region independently.
The synthetic target is compiled, not launched or visually reviewed here.

## State and temporal contract

The standalone state machine starts accepted, enters Assessing for Preview,
publishes the matching generation, acknowledges Cancel with accepted content,
and rejects a later completion from the cancelled generation. The target's
two-second scheduler is demonstration pacing only. Direct model calls are not
OS input. State logs are not presentation evidence.

The Python receipt requires separately supplied expected identity and exact
three-region content. It rejects stale source/operation/draft/generation, wrong
content, incomplete regions and a misleading completion marker. A native
receipt always returns `VP-NATIVE-UNQUALIFIED` in this version. There is no
native PASS branch, including when a caller supplies plausible visibility.

For a **synthetic monotone model only**, given input interval `[Ilo,Ihi]`, a
known nonfinal predecessor and first final observation, bounds are
`[max(0, old_lower - Ihi), new_upper - Ilo]`, rounded outward. Required premises:
strict frame-event order; contiguous observed sequence; complete frames; zero
declared sample loss; calibrated bounds spanning every event; known predecessor;
the synthetic state-machine persistence premise; no later nonfinal observation.
A transient final followed by regression invalidates the result rather than
moving the lower bound to a later predecessor. An unsampled regression cannot
be ruled out in native capture: this is an explicit qualification blocker.

Upper <= budget means `synthetic_upper_bound`; lower > budget means
`synthetic_lower_bound`; overlap means `synthetic_overlap`. These names are
algorithm test results and retain `visible_latency: Not assessed`.
Timeouts, cancellation, malformed data, blank/suspended/incomplete/skipped frames,
missing predecessors, gaps, zero/reordered events and unknown clock/persistence
return explicit refusal codes. Native sequence numbers count callbacks, not
WindowServer frames; contiguous callback indices do not prove zero capture loss.

## Clock and input conventions

| Clock | Unit / epoch / semantics | Qualification |
|---|---|---|
| .NET Stopwatch | ticks, declared frequency, arbitrary monotone epoch | 32 paired noncapture samples executed |
| mach absolute | ticks, `mach_timebase_info` numerator/denominator -> ns, host monotone absolute clock | sampled between Stopwatch reads |
| SCK display event | raw mach absolute value supplied by frame attachment | helper compiled; event unobserved |
| SCK callback | separate `mach_absolute_time()` at receipt | helper compiled; event unobserved |
| Metal presentation | host seconds; zero is not presentation | source-only; inaccessible route not invented |
| external cold-start | supervisor timestamps immediately before process creation and after spawn call | Prepared, not Main; no cold-launch trial |

No common epoch is assumed. For each paired read `(Sbefore,M,Safter)`, the
possible offset is the converted Stopwatch bracket minus converted mach time,
expanded for declared quantization. Intersect paired intervals and retain all
raw values. Empty intersection is refusal. Native adjacent samples allow equal
ticks (nondecreasing); synthetic frame events require strict progress.
The observed adjacent clock sample span does not bound trial-duration drift.
The synthetic algorithm has explicit finite drift/resolution inputs; missing
drift is refusal. Cross-process calibration over trial duration remains open.

Future OS-input trials need the root-operated CUA input boundary calibrated to
the target handler and observer without substituting CUA call duration for
latency. Future cold launch starts before creating the process/runtime; this
target's Main cannot supply that start. Display ID, pixel scale, bounds,
refresh convention and unoccluded visibility must be measured per trial.

## Prepared helper privacy, lifecycle and exact launch boundary

The compiled helper has no default capture. Its only observation entry is:

```sh
spikes/visible-presentation/capture --capture-reviewed spikes/visible-presentation/owned-request.json
```

**Do not run this command under R41.** The request does not exist because root
has not launched an owned target or measured its window/crop identity. A later
specific user authorization must name this alternate observer and its one-time
target lookup; CUA permission or existing OS permission is not authorization.
Root must review the exact final source/binary hashes first.

R47 supersedes the R41 request shape. The exact schema and remaining readiness
conditions are specified below. No observed PID/window/crop request exists.

The prepared `CGPreflightScreenCaptureAccess` call does not request permission;
false refuses. This query was **not executed**. Only after review/authorization
would the helper request shareable metadata, filter one exact process/window
and use the desktop-independent window filter. No metadata of other windows is
logged or retained; no broader capture fallback exists. It never requests TCC,
opens settings or uses UI automation. Missing permission, target mismatch and
unavailable content return refusal. Existing permission can race/revoke; errors
remain refusal, never automatic permission retries.

R47 seals the concrete published file inventory and resolves its selected deps
assets. That does not attest loaded memory or dynamic loader resolution. The
prepared helper is **not yet an executable capture-authorization packet**.

Only target region bit evidence, timestamps, status and scale are emitted.
Each cell's interior must be uniformly near black or white; antialiased edges
are excluded by one pixel. No screenshot/video/audio file is written. Full frame
buffers exist transiently in memory; no egress, children or persistent image
storage. Local stdout receipts should remain in task scratch until review, then
retain only source-bound minimal proof at Owner disposition. Other data must not
be placed in the synthetic target. The finite timer starts before metadata
lookup; timeout/SIGINT/SIGTERM request stream stop; acknowledgement is recorded,
and a two-second missing acknowledgement exits with cleanup unobserved.
The helper launches no children. Native startup/stop races, permission revocation,
empty capture, cancellation and resource cleanup are **native-unexecuted**, not
proven from source or process disappearance.

## Future discriminating experiments and remaining decisions

After exact helper/privacy review and specific authorization, first prove three
region decoding, full target identity, permission refusal and stop lifecycle.
Do not begin budget trials while the native bridge, capture-loss or physical
visibility/persistence premises are unresolved. A target-only route may remain
unsuitable for the approved physical-visible endpoint despite correct pixels.

A later known-delay experiment uses independently timestamped input and two
otherwise identical transitions, with a fixed delay inserted before final
content publication. Its presented bound must move by the delay within stated
uncertainty. A CPU-duration or callback-delay movement alone fails the oracle.
Inject partial-region, stale generation, final-then-regression and late Preview
after Cancel faults; each must refuse. Alternate observer-on/off blocks using
the same workload and host to estimate observer overhead with uncertainty;
retaining requested 60 Hz alone does not establish cadence or overhead.

Before product sampling, preregister trial count, nearest-rank p95 estimator,
cold-cache protocol and failures. Every attempted trial stays in the denominator.
Unobserved/failed/time-out trials contribute an unbounded upper interval, never
disappear; report counts and reasons. A population-tail claim requires its own
sample/confidence design. Historical 30 synthetic trials and three Main starts
are not that protocol. No timing PASS, Windows visible proof, reference-host
qualification or section-editor admission follows from this packet.

## R47 identity and exact request contract

**Status: preparation only; native readiness blocked.** R47 author scope is
exactly Capture.swift, Program.cs, the qualifier, this design and its proof.
Tier T2 applies to identity and request contracts. The original T1 opening was
corrected at grounding. No product surface is changed. Root owns independent
Data/Security/Test review and native-request disposition.

The execution graph is serial: grounding/claims -> concrete RID publish and
contract design -> one coherent implementation -> targeted noncapture checks
and bounded corrections -> durable proof -> independent root review. No fan-out.
The author budget is 60 top-level calls/40 minutes. Work/span were not measured
per node; no estimated speedup is claimed. Stop when the preparation packet is
frozen, including named blockers; never run capture to remove a blocker.

The data grain is one reviewed request for one PID/start identity, one window,
one immutable published package, one geometry and three expected region bit
digests. Identities and digests are non-additive value objects. A geometry,
source, package, process or operation change requires a new reviewed request;
there is no in-place retargeting. Evidence is append-only per finite attempt.
Surface trace: publish files/deps -> Python manifest -> reviewed JSON -> Swift
Request -> initial/post-lookup/per-frame/completion checks -> refusal receipts.
The target's existing Region/Witness -> bit field path remains unchanged.

### Concrete package evidence and trust boundary

The concrete build uses .NET SDK 10.0.203, `dotnet publish -c Release -r osx-arm64
--self-contained false`, the existing project and dependency versions. The
qualifier inventories **all** regular files in that directory, including PDB,
managed assemblies and native dylibs. It rejects symlinks, escaping roots,
case-fold aliases, missing required assets and nonempty competing dependency
targets. The selected `runtimeTarget` must be osx-arm64. Every runtime/native/
resource asset maps to a unique actual published relative path; missing or
ambiguous resolution refuses. An empty portable target is observed in the
concrete deps document and is not a second resolution candidate.

The observed inventory has 31 files and 27 resolved deps assets. The native
link inspection is retained in proof. `/System/Library` and `/usr/lib` frameworks,
the installed .NET runtime/host, loader policy and OS process APIs form an
explicit **system-runtime trust boundary**, not package hash entries. Static
publish/deps/link evidence does not attest already-loaded memory. The arm64
load commands show only system library/framework imports for these private
dylibs and the apphost; Avalonia's `/usr/local/lib/libAvalonia.Native.OSX.dylib`
is **LC_ID_DYLIB**, not a missing imported file. No concrete unresolved static
private asset remains in this inspected package. Root must independently check
the retained resolution rows and load commands before accepting that closure.
Dynamic loader overrides for a future launch must be constrained in its reviewed
launch context; this is distinct from demanding universal memory attestation.
The 31-file inventory is not an arbitrary approved-library allowlist.

### Exact R47 JSON schema

The request is UTF-8 JSON in Foundation sorted-key canonical form, without
escaped slashes or surrounding whitespace, less than 1 MB. Exact keys only;
canonical equality rejects duplicate keys and alternate encodings. No default,
wildcard, full-screen option or unknown key is accepted.

| Field | Exact meaning and constraint |
|---|---|
| `pid`, `window` | Positive Int32 PID and UInt32 window ID; measured later, not fixture values |
| `startSeconds`, `startMicroseconds` | UInt64 kernel `proc_bsdinfo.pbi_start_tvsec/pbi_start_tvusec`; seconds >0, microseconds <1,000,000 |
| `launchReferenceBits` | Exact UInt64 bit pattern of AppKit Date seconds since reference date; no tolerance |
| `executable`, `executableSHA256` | Exact `<packageRoot>/VisiblePresentation` and its lowercase 64-hex digest |
| `bundle`, `title` | Exact owned target bundle identifier (empty if observed absent) and title |
| `packageRoot`, `packageFiles` | Canonical absolute package directory and complete relative-path -> lowercase SHA-256 map; reviewed against qualifier deps resolution |
| `helperSHA256` | SHA-256 of the exact reviewed helper binary executing the request |
| `contextRoot`, `contextFiles` | Absolute source root and exactly five relative keys: Capture.swift, Program.cs, VisiblePresentation.csproj under tools/spikes/VisiblePresentation; tools/qualify-visible-presentation.py; global.json; values are SHA-256 |
| `durationSeconds`, `frameCap` | Integers 1..8 and 1..600; timer begins before metadata lookup |
| `width`, `height` | Integers 192..2048, exact target buffer pixels |
| `geometry` | Exactly finite numeric x/y/width/height/scale; positive size/scale; point size times scale equals pixel dimensions |
| `crops` | Exactly three disjoint objects, each exactly integer x/y/width/height plus lowercase 64-hex expected; bounds inside buffer, sizes >=64 and multiples of 16 |

`geometry.x/y` is the target WindowServer frame origin in desktop points.
`geometry.width/height` is the reviewed target frame/content size in points.
Crops use target-buffer pixel coordinates with top-left origin. Window frame,
filter content size/scale and per-frame contentRect/scale/contentScale must all
match the request. ContentRect is required to be origin zero and the exact
point size; native decoration/content semantics have **not** been measured.
Any mismatch refuses, even if that makes the current target infeasible. No
inferred crop transform or silent resampling adjustment is permitted.

The helper checks kernel PID/start, executable, bundle, exact AppKit date,
package files, helper and source hashes initially, after asynchronous lookup,
for each delivered frame and at completion. Target-specific CGWindow metadata
checks owner/window/bounds after lookup, per frame and at completion. The kernel
header exposes integer microsecond units; actual precision, collision freedom,
and cross-API consistency are **unmeasured**. Date bit equality adds no proof
of kernel uniqueness. Exited/short-read/unverifiable process lookup refuses.
Disk checks cannot close all TOCTOU or loaded-memory races; root retains veto.

### Finite feasibility and privacy plan (not executed)

1. Independently review the closure, helper/source hashes and exact schema.
   Review the concrete private closure and resolve process-start precision before native entry.
2. In a later specifically authorized session, root measures the one owned
   target PID/start/window, bounds, scale and content mapping. No observed value
   may be copied from the synthetic parser fixture. Three expected digests must
   be independently derived from the intended operation/generation/content.
3. Review one concrete request. Any move/resize/scale/package/process change
   invalidates it and requires a new review. First trials discriminate wrong
   owner/window, moved/resized crop, stale/late state, target exit, denial, blank
   frames and stop acknowledgement; cap every attempt at request duration and
   frame count. A failed premise remains a refusal in the denominator.

The later authorization must disclose transient shareable-window metadata
lookup, selection of this one target, target-specific window metadata checks,
transient full-target frame buffers, finite duration/frame cap, three retained
region bit digests with timestamps/status and source identities, and the exact
local receipt path. Retain compact reviewed evidence in this proof; delete
unneeded raw native receipts after root review and disposition. No audio, other
window retention, screenshots/video files, user document content or egress.
The helper requests no permission and has no full-screen fallback.

Pure `--contract-request` and `--identity-contracts` return before AppKit calls,
permission APIs, enumeration or stream creation. Target `--contracts`, `--clock`
and `--identity-contracts` return before Avalonia startup. Only these entries
were executed. Native permission denial, actual owner/window races and stream
cleanup remain unexecuted. Stop request and bounded two-second acknowledgement
remain prepared; process exit alone is not stream-cleanup evidence.

`VP-NATIVE-UNQUALIFIED` remains unconditional in the native receipt assessor.
Desktop-independent pixels do not prove on-screen visibility; callback sequence
does not prove absence of sample loss; adjacent local clocks do not bound
cross-process drift. Unknown premises never become plausible metrics.
