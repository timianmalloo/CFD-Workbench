---
id: note-m1-scope-decision
title: "User decision — Windows and visible performance in M1; section editing in M1.1"
type: decision-note
status: accepted
owner: "@owner"
phase: "application-foundation"
tags: [decision-note, milestone, windows, performance, sections]
links:
  - { to: spec-cfd-workbench-v1, rel: relates-to }
  - { to: spec-foildsl, rel: relates-to }
  - { to: architecture-application, rel: relates-to }
  - { to: coordination-contract-c-native, rel: relates-to }
  - { to: coordination-application-build, rel: relates-to }
review-by: "2026-10-25"
review-suggested:
  - { by: architecture-application, on: 2026-09-24, reason: "User-approved M1 two-platform visible-timing gates and M1.1 section authoring changed the delivery architecture; review dependent milestone claims." }
  - { by: coordination-application-build, on: 2026-09-24, reason: "Approved Windows/timing M1 and section M1.1 placement plus R39 native veto changed the coordination gates; review dependent status and handoffs." }
summary: >-
  The user approved Windows x64 runtime and measured on-screen budgets in M1 and
  placed the full section editor in M1.1. Native and platform proof remain open.
---

# M1 scope decision

**Status: user-approved, 2026-09-25 UTC.** In response to the single combined
question below and its explicit Owner recommendation, the user's exact answer
was `approve` (audit prompt `al-01M3BA3M123XF92DD2D8PSHNA1`). The adopted placement is **Windows x64 runtime: M1; measured
on-screen timing: M1; full section editor: named M1.1 Section authoring**.
This approves the scope placement, not completion of any gate. The independent
native UI veto, including the later High Contrast focused numeric-field failure,
remains separate from this decision and the CUA attachment investigation.

**Question:** Should we keep **real Windows x64 operation and measured on-screen
budgets in M1**, and put the **full section editor in a named M1.1 Section authoring
increment** (Owner recommendation), or choose a different placement for any row
below: **IN M1 / named M1.1 / deferred**? A single response such as
`Windows: M1; timing: M1; sections: M1.1` resolves all three placements.

The approved recommendation preserves the current two-platform, measured M1
promise and makes section authoring an explicit next increment. It does not
promise a date.
Keeping section editing deferred to the existing M2 is the smallest unchanged-plan
alternative. Choosing a macOS-only or performance-unqualified M1 instead is a real
change to acceptance, not recognition that the original gates passed.

## What the current authorities say

| Obligation | Product and language authority | Current milestone authority and implication |
|---|---|---|
| Real Windows runtime | [Product A8.1](../specs/cfd-workbench-v1.md#a81-iso-25010-table-each-row-a-fixture-or-a-labelled-boundary), source lines 1081–1090: Windows **x64**, both-OS testing, native Narrator evidence; DOC-02 at line 968 requires save/reopen on either platform; CLI-02 at line 1020 requires macOS ARM64 and Windows x64 ring-0 runs. [FoilDSL §6 and §10](../specs/foildsl.md), especially DSL-10 at line 487, require reproducible evaluation and exact persisted source/dependencies/recovery across OSes. | [C contract, Goal and boundary](../coordination/contract-c-native.md#goal-and-boundary), lines 45–60, promises a runnable macOS/Windows native workbench and CLI. Its platform gate at lines 186–190 explicitly separates cross-publication from Windows-host runtime proof. [Architecture §8](../architecture/application.md#8-vertical-delivery-and-release-gates) separates native Windows UIA/Narrator, replacement and numerical parity from cross-build. Windows is already in M1. |
| Visible performance | [Product CAD-03](../specs/cfd-workbench-v1.md), line 1027: numeric echo in inspector, status and canvas within **100 ms p95**. A8.1 at lines 1081–1082 names **cold launch ≤5 s**, **preview regeneration p95 ≤250 ms**, and **Cancel acknowledgement ≤250 ms**. Its reference is 21 authored stations, 201 slices, 50k triangles and 10k plot points on fixed 16 GB Apple-silicon and Windows x64 machines. The performance row calls the larger product targets “proposed”; that wording does not erase the explicit C gate. FoilDSL defines transaction correctness, not a separate latency waiver. | [C contract §3](../coordination/contract-c-native.md), lines 140–154, expressly requires the three budgets measured in the actual app and failures disclosed; it starts with a bounded 15-point/one-section projection. [Ruling 29](rulings.md#ruling-29--bounded-c-portability-repair-and-truthful-native-timing-proof) requires an honest endpoint. The small Example and sparse projection cannot silently replace the specified workload. Remaining broader analysis/results performance targets are not added to this offline M1 by this request. |
| Full section editor | [Product A4.14](../specs/cfd-workbench-v1.md#a414-section-scope-dimensions-and-design-decisions-revision-15), lines 669–686; SRC-03/10, CAD-10, GEO-07/08, UX-01 and UI-25 require editable sections with declared scope and coherent source/geometry. [FoilDSL §5/§6/§9](../specs/foildsl.md) define independent upper/lower curves, normalized profile blending, shared versus independent edits and t/c policy. DSL-02/03/07/15/16 at lines 479–493 cover standalone sections, drafts, undo, scope and safe inspection. | [C contract](../coordination/contract-c-native.md#goal-and-boundary), lines 50–60, permits **one numeric independent LE/TE rail edit** and expressly excludes the full v7 section-profile workspace. [Architecture §8](../architecture/application.md#8-vertical-delivery-and-release-gates) places more profiles/assets/section edits and certified blending in **M2 geometry breadth**. Adding the full editor to M1 expands the current milestone; absence of that editor is not itself a breach of the narrow C contract. |

For this choice, **full section editor** means the product section-authoring
workflow, not merely a section plot or a second numeric field: persistent station
thumbnail/action; standalone 2D section opening; upper/lower spline control frames,
pointer, keyboard and numeric edits; applicable lock/constraint and construction
operations under A4.5/A4.6 and GEO-05/12–15; conversion residuals and original-profile
provenance under GEO-08; shared/independent scope; explicit Keep current/Use source
thickness intent; disclosed neighboring blend intervals; continuously validated
Preview/Apply/Cancel; exact source/history/Save/Reopen/Undo/Redo; accessible inspection
while drafting. It needs the corresponding multi-profile core admission, not a UI
that accepts sampled geometry. Catalog ranking, scientific analysis and export are
separate product work; this phrase does not silently add them. Any narrower editor
must be named as a subset rather than labelled “full.”

The [v7 walkthrough and limits](../mockups/workbench-v7.md#review-walkthrough) show
shared/independent editing, upper/lower controls and thickness intent. Its sampled
multi-profile validation and browser-session history explicitly do **not** prove
native persistence, continuous geometry certification or a delivered production
section editor. The mockup is a behavioral reference, not evidence of M1 completion.

## Current evidence and concrete Windows routes

Read-only inventory was made on 2026-09-25 UTC. Repository clauses were read at
`854aa6e88a41` in the decision worktree. The independent native review was read at
root commit `c4810e927fa343b8b905dfe4b575d29426dc9542`; it retains source/package-bound
R37 evidence while R38 repair is active. Findings below are bounded to those
observations, not a claim about later author output.

| Route or evidence | Observed state | Consequence |
|---|---|---|
| Local machine/virtualization | `uname -m`: arm64; `hw.memsize`: 68719476736 bytes; `kern.hv_support`: 1. No `prlctl`, `VBoxManage`, `vmrun`, either QEMU system binary, `utmctl`, `multipass`, `xfreerdp` or Azure CLI on PATH. `gh` and macOS `pwsh` exist. No matching VM/Windows/remote-desktop application in `/Applications` or `~/Applications`, or matching running process name. | Hardware virtualization capability is not a configured Windows host. macOS PowerShell is not Windows runtime proof. The current host also differs from the specified 16 GB reference machine. |
| VM images / remote Windows | Standard `~/Parallels`, `~/Virtual Machines.localized`, `~/Documents/Virtual Machines.localized` and UTM Documents directories are absent. No remote Windows route was identified in the coordination/workflow records examined. | No accessible Windows desktop or image is verified. This was a bounded inventory, not a disk-wide search or proof that none could exist. No credentials, licenses, SSH configurations or remote sessions were inspected. Windows ARM remains an open support-matrix choice; it cannot silently replace the stated x64 target. |
| Existing GitHub access | Read-only `gh api` calls verified public repo `timianmalloo/CFD-Workbench`, current account push/admin permissions, Actions enabled, and zero registered repository runners. The sole active workflow is `docs-health`; its source uses `ubuntu-latest`. The last three observed runs completed successfully on 2026-09-23/21 and concern docs only. | A Windows workflow can be proposed through the existing repository route without procuring a machine. No Windows job was configured or dispatched; API access does not prove scheduling, runtime success or an interactive desktop. Repository-level runner inventory is not an inventory of every possible external machine. |
| Documented hosted Windows candidate | GitHub documents x64 `windows-2022`/`windows-2025` standard runners and free standard-runner use for public repositories. [Official runner reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners), checked 2026-09-25. | A pinned Windows x64 CI lane is a concrete candidate for executable, store/fault and identity tests. Its unattended environment does not establish an interactive Windows client, UIA/Narrator, mixed-DPI behavior or the reference laptop's presentation timing. Those require a separately qualified native review route. No actual CI usage or charge was measured. |
| Current product Windows support | `src/CfdWorkbench.Persistence/ProjectStore.cs:173–177` restricts the measured store to macOS arm64; the comment explicitly leaves Windows handle/reparse/sharing guarantees unmeasured. `tools/verify-application-core.py:150–152` refuses Windows execution pending an ownership/cleanup adapter. Native review lines 1158–1179 retain Windows DLL cross-publication, not live tests. | Windows-in-M1 needs implementation and negative security/data tests as well as a host. Simply running the existing binary or adding a green build lane is insufficient. |
| Native timing | [Independent review](../reviews/ui-application-native.md), lines 753–768 and 1158–1179: 30 edit, 30 Preview, 30 Cancel diagnostics plus three managed starts use synthetic events and fresh compositor-batch completion on the small Example. Managed startup excludes pre-Main time. Display presentation is not observed. | The diagnostic endpoint is useful but cannot prove visible 100/250 ms or cold process-launch ≤5 s. Neither CUA call duration nor a faster compute-only sample closes this gap. |
| Section production | Joined M1 contracts admit a conservative one-profile path and one independent rail transaction. The C review explicitly leaves full v7 profile editing outside this increment. | No delivered full section editor or certified multi-profile authoring is established. Existing native section inspection is not section editing. |

No new job, VM, remote connection, purchase, deployment, credential access or UI
session was started for this inventory. The Coordinator would own a future route
and its bounded author assignment **after** a user scope decision; root would retain
independent native/Test/Security/Data review. No Windows worker or host is currently
assigned by this note.

## Options, cost and effect on M1

Costs below describe required work and relative uncertainty, not measured agent
spend or calendar estimates. Aggregate tokens, money and delivery dates are **Not
recorded**. No option authorizes failing safety, identity, persistence or accessibility
checks in capabilities still claimed as supported.

| Obligation | IN M1 | Named M1.1 | Deferred | Owner recommendation |
|---|---|---|---|---|
| Windows runtime | Preserve the existing two-OS definition. Implement and prove native Windows store/process behavior; add real Windows x64 runtime CI and qualify interactive UIA/Narrator/file-dialog/DPI evidence. Highest environment uncertainty; M1 remains blocked until that evidence exists. | **M1.1 Windows parity**. Explicitly redefine M1 as macOS-only; keep Windows portable output labelled unverified and native saving unsupported until parity passes. Moves substantial implementation and host cost later, with platform-divergence risk. | Leave Windows a product target without a committed increment. M1 becomes macOS-only with no promised Windows delivery boundary; greatest risk of indefinite platform drift. | **IN M1.** Two platforms are an existing promise; the verified CI access makes part of the proof route concrete, though an interactive Windows host remains unresolved. |
| Visible timing | Keep ≤5 s cold, ≤100 ms p95 echo, ≤250 ms p95 Preview and ≤250 ms Cancel acknowledgement as acceptance. Establish an actual presentation endpoint and the specified workload/machines; then optimize only measured failures. Work and possible renderer/core changes are unquantified; instrumentation feasibility is the first risk. | **M1.1 Visible-performance qualification**. M1 is explicitly functionally reviewed but performance-unqualified, with measured failures and unmeasured endpoints disclosed. This changes the existing C gate; a batch statistic is never relabelled a PASS. Delays responsiveness assurance and may expose users to latency. | Remove a dated performance milestone while preserving the product targets as unresolved. Lowest immediate proof work, highest risk that responsiveness regressions become normal. | **IN M1.** Do not substitute a weaker endpoint or smaller workload. If the user chooses faster functional delivery instead, the M1.1 exception must explicitly say what timing evidence is absent. |
| Full section editor | Expand M1 from the single-rail walking skeleton to the defined section workflow. Requires multi-profile certificate/core contracts, transaction/history/provenance changes, conversion/constraint proof and native editor/accessibility work. Largest functional expansion and scientific-validation risk. | **M1.1 Section authoring**. Pull this defined portion of M2 forward into a named follow-on increment; retain the current narrow M1. Requires design/ADR and exact acceptance decomposition before implementation, with remaining M2 geometry breadth still separate. | Keep it in existing **M2 geometry breadth** without accelerating it. No change to current M1; broader CAD remains unavailable and must be labelled so. | **M1.1 Section authoring.** Makes the requested capability explicit without calling the current section plot a full editor or enlarging M1 implicitly. Keeping M2 is valid if no earlier priority is intended. |

## What honest screen timing would require

The timing contract must bind the frozen package, OS/device/display configuration,
fixture, operation ID and source/draft generation. Measure input-to-final-correct
visible state, including all required inspector/status/canvas echoes; an
“Assessing” frame is not Preview completion. Cancel must acknowledge visibly and
prevent a late stale result from replacing that state. Cold launch begins at actual
process launch and ends when the initial document is visibly usable, including
runtime startup before Main.

Use a validated platform presentation signal or independently calibrated capture
method that identifies the displayed final frame. Record timestamp alignment and
uncertainty, raw trials, warm-up policy, failures/timeouts and the declared percentile
calculation. Preserve the existing known-delay and stale/cancel negative controls.
Separate the small Example diagnostics from the A8.1 workload; record each platform
and reference-machine result separately. A renderer submit, layout, fresh compositor
batch, screenshot acquisition round trip or general UI-tool latency is not by
itself that presentation signal. If the signal cannot be established, say **Not
assessed** and return to the selected milestone gate; do not manufacture a result.

## Delivery edges after approval

The [coordination plan](../coordination/application-build.md) now stages three
separate proof tracks. Windows x64 requires a qualified runtime route, native
store/process adaptation, real executable and UI evidence. Visible timing requires
an observed final on-screen endpoint on the stated workload and platforms. M1.1
section authoring requires a detailed design and independent scientific, Data,
UX and Test review before an implementation author is assigned. Those tracks
may be prepared independently, but M1 acceptance still requires every M1 gate.

No numerical budget, security rule, identity rule or accessibility floor is
waived. Current batch-cycle diagnostics are not presentation results. This
decision does not clear the current C native veto, join C, approve M1 or mark
Windows supported. The existing M2 geometry breadth retains work outside the
named M1.1 section-authoring subset.
