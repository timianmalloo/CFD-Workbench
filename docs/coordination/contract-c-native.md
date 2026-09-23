---
id: coordination-contract-c-native
title: Provisional native desktop and CLI adapter assignment
type: plan
status: proposed
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, desktop, cli]
links:
  - {to: coordination-application-build, rel: depends-on}
  - {to: coordination-contract-b-core, rel: depends-on}
  - {to: architecture-application, rel: depends-on}
  - {to: design-application-foundation, rel: depends-on}
  - {to: design-language, rel: depends-on}
  - {to: mockup-workbench-v7, rel: relates-to}
review-by: 2026-10-23
summary: A held, exact-path candidate for the first native desktop and CLI adapter after the full core gate.
review-suggested:
  - { by: architecture-application, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
  - { by: design-application-foundation, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
  - { by: mockup-workbench-v7, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
---

# C · native desktop and CLI adapters (preparation only)

**Status:** not dispatchable. This packet prepares the serial successor to B; it
does not freeze an API, assign a worker, or claim a product pass. Owner Ruling 13
admits only the 18-path B core track. B's two isolated commits `5f40af0` and
`5b5b489`, plus the later `a8a6351` placement/session checkpoint, are not an
integrated consumer contract. Owner Ruling 16 requires a compiled immutable,
source/revision-bound authored-control projection and public consumer fixture
inside the existing B lease before C can select imported CVs or stations. Before C can
launch, B must return a clean complete commit, root and Owner must clear its
Data/Test/Geometry/Security vetoes, `conductor-join.py` must integrate it, and
the Coordinator must compile this packet against the **actual** public core and
store signatures with an independent review. Any changed path/API is a new
Owner routing/contract decision, not a silent edit here.

Rulings 17–19 also hold C until the reviewed evaluator `/2` identity, all-query
certificate feasibility and native store policy are implemented and proved
against the reconciled companions. A `/1` source or saved project is preserved
and refused by the unavailable evaluator path, never silently interpreted as
`/2`; the adapter cannot hide this version decision.

## Goal and boundary

Deliver the first runnable offline macOS/Windows workbench and CLI against one
accepted-source core. Start with an embedded, explicitly ID-authored Example.
Open native `.foil` and `.cfdw.json`; show the exact accepted source, distinct
source/Surface identity, diagnostics and certified derived foil in a viewport
and section view. For a missing-ID `.foil`, show the candidate source diff and
require explicit acceptance. Permit one numeric independent leading or trailing
rail CV edit through the core-owned draft, Preview, Apply and Cancel; then
Undo/Redo, atomic Save/Reopen and an explicit recovery offer. The CLI reads the
same core/store and reports identity and diagnostics. Analysis says
**Unavailable — no method implemented**. A renderer sample never constructs a
certificate or makes an unsupported source editable.

This is a narrow M1 authoring path, not the full v7 section-profile/bank/decision
workspace or a solver/export implementation. The UI takes its hierarchy,
colors, typography, focus and state language from `DESIGN.md` and the [v7
mockup](../mockups/workbench-v7.md), while the product specification and
FoilDSL own behavior. Accepted versus draft versus recovery remains visibly
labelled in every view. No second editable curve table or copied identity
algorithm belongs in an adapter.

The C author runs `$implement` for red → green → refactor, its triggered
Testing Strategy union, Proof Pack and independent pre-merge review. Because
this track builds a user-facing native surface, the author also runs the
triggered `$ui-design` **review** contract with the UX & Accessibility,
UX Researcher/IA, Native Desktop, Test Architect and Simplifier lenses. Reuse
the existing v7 direction, flows and `DESIGN.md` tokens; a new speculative
browser mockup is not an admission artifact. Review structure before polish:
Example, open/import, draft/preview/apply/cancel, error, Not assessed,
recovery and unavailable-analysis states must be reachable and legible in the
actual desktop shell.

## Provisional exact path inventory (24)

These paths are **reserved candidates**, not live leases. The solution file is
sequentially shared only after B is joined; C cannot edit B's core/store/tests.
No directory wildcard grants ownership. Official index/audit outputs are
derived/register exceptions and remain subject to the join gate.

| Area | Candidate authored paths |
|---|---|
| Shared solution | `CFDWorkbench.slnx` |
| Native desktop (11) | `src/CfdWorkbench.Desktop/CfdWorkbench.Desktop.csproj`, `src/CfdWorkbench.Desktop/Program.cs`, `src/CfdWorkbench.Desktop/App.axaml`, `src/CfdWorkbench.Desktop/App.axaml.cs`, `src/CfdWorkbench.Desktop/MainWindow.axaml`, `src/CfdWorkbench.Desktop/MainWindow.axaml.cs`, `src/CfdWorkbench.Desktop/Viewport.cs`, `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `src/CfdWorkbench.Desktop/Styles.axaml`, `src/CfdWorkbench.Desktop/Info.plist`, `src/CfdWorkbench.Desktop/Assets/example.foil` |
| CLI (2) | `src/CfdWorkbench.Cli/CfdWorkbench.Cli.csproj`, `src/CfdWorkbench.Cli/Program.cs` |
| Adapter proof (4) | `tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj`, `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs`, `tests/CfdWorkbench.Cli.Tests/CfdWorkbench.Cli.Tests.csproj`, `tests/CfdWorkbench.Cli.Tests/CliTests.cs` |
| Packaging/gate (2) | `tools/package-application.py`, `tools/verify-application-adapters.py` |
| Durable evidence (3) | `docs/proof/application-adapters.md`, `docs/proof/application-macos-ui.png`, `docs/proof/application-macos-ax.json` |
| Independent UI review (1) | `docs/reviews/ui-application-native.md` |

At freeze, check the actual core/store API and whether Avalonia's headless test
package or packaging APIs need a focused spike. The A spike established .NET
10.0.203, Avalonia/Fluent 11.3.14, a macOS file picker, a live numeric input
and a rendered illustrative viewport; it did **not** prove product UI, Windows
runtime, AX viewport semantics or M1 packaging. Package/license/SBOM findings
in ADR 0003 remain obligations. No `tools/spikes/` path is implicitly leased.

## Observable acceptance and proof

1. **One authority.** An installed Example starts from the embedded, explicit-ID
   FoilDSL resource and yields the same accepted source SHA-256, Surface hash
   and evaluator in GUI and CLI. Independent opens allocate fresh
   project/Design/accepted UUIDs; both adapters report those UUIDs identically
   only when opening the same saved native project envelope.
   The resource is checked against the normative Example's meaning;
   it is not silently generated from a missing-ID fixture at launch. A `.foil`
   import with missing IDs retains original bytes until the user accepts its
   deterministic candidate. Unknown language geometry stays read-only with
   actionable diagnostics. The adapter discovers custom rail CV IDs, normalized
   authored positions, units, locks and authored profile assignments from the
   Ruling 16 immutable core projection; it never parses accepted source again
   or guesses `cv-N` IDs. Derived inspection slices remain distinct from authored
   station facts and do not acquire a new source revision.
2. **A complete edit.** Numeric leading/trailing CV input owns one core draft;
   target, base and generation remain fixed while inspecting elsewhere. Preview
   shows provenance and bounded error, Apply uses only current core assessment,
   Cancel leaves source/history unchanged, and stale/retargeted/invalid attempts
   visibly refuse. Undo/Redo restore source and geometry together. Save/Reopen
   round-trips accepted bytes, identity, history and a separate invalid recovery
   offer. CLI reports the same admitted revision and stable diagnostic codes.
3. **Real derived views.** The viewport and section use core `PointAt`/`SectionAt`
   or a reviewed bounded projection of their certified enclosures. View state
   labels axes/units, section position, accepted versus preview provenance,
   error/Not assessed and the unavailable analysis pane. A static v7 SVG or
   spike shape cannot satisfy this gate. Measure the spec's 5-second cold start,
   100 ms edit feedback and 250 ms preview/cancel budgets on an actual app;
   disclose failures, do not infer speed from tests. At the public API freeze,
   profile whether a `Preview` call reassesses geometry. The viewport must use
   one current core assessment for a bounded projection instead of issuing an
   unmeasured fresh validation for each plotted vertex; if the frozen API lacks
   that seam, obtain a reviewed core contract change before adapter code.
4. **Native UX and access.** Test native open/save dialogs, tab order, labeled
   numeric input with unit, Return/Escape behavior, disabled/error/recovery
   states, focus restoration, keyboard Undo/Redo and reduced-motion/contrast
   modes. The viewport needs an accessible name and an inspectable data/section
   equivalent; the A spike's AX `role=unknown` is an open gap. Root independently
   inspects the live macOS app through native AX, keyboard and screenshot, then
   reviews the committed proof image/AX record. The native review harness must
   select persona, window size, state, theme and reduced-motion setting and
   expose the hard states for repeatable structure, focus and accessibility
   critique. Record rubric findings with location, severity, evidence and fix
   in `docs/reviews/ui-application-native.md`; the author cannot clear its own
   UX/accessibility veto. A headless render
   alone is not native interaction evidence.
5. **UI tokens and platform.** `Styles.axaml` maps the active `DESIGN.md` colors,
   type and spacing; the argument-free gate scans a nonempty C#/XAML corpus for
   off-token literals, measures critical contrast and runs interaction tests.
   The existing 59-rule browser/CSS gate is not a native Avalonia verdict; record
   browser-specific checks as inapplicable to the native corpus rather than
   skipped or a zero-file PASS, and apply the equivalent token/contrast/state
   checks to actual XAML and rendered controls. Build and self-contained
   publish for osx-arm64 and win-x64, and create a reviewable `.app`/Windows
   portable bundle. macOS live proof and Windows cross-build/publish proof are
   separate; Windows runtime tests remain Not assessed unless actually run on
   a Windows host. An unrun Windows desktop workflow cannot be called M1 pass
   on both OSes.
6. **Instrumentation and privacy.** Normal-path core events remain visible as
   bounded local operation/status/duration/volume facts with stable codes; the
   GUI and CLI can show failure detail without writing source, path, vertex,
   hash or personal data to telemetry. No network exporter, MRU or background
   upload. The adapter calls save acknowledgement only after the exact captured
   bytes were published and OS save token returned.

Red-first adapter tests cover the normal Example/edit/save/CLI route plus
cancel, stale validation, unsupported geometry, file conflict, recovery,
wrong extension/type, keyboard and accessible labels. An argument-free
`tools/verify-application-adapters.py` must build once into unique task-local
scratch, run named tests, check both publish targets where available, inspect
nonempty native token corpus and return actual subprocess statuses with owned
process quiescence. The proof names exact commands, package versions, source
fingerprints, durations, screenshots and unrun platform obligations. Root/Owner
review before join; one integrated recount and actual rendered app exercise
follow in D. A green C test run is not M1 acceptance.

## Routing and launch hold

The user's routine-coding preference is Grok or Agy to conserve Codex/Claude
capacity. Ruling 6 rejects the observed Grok ACP profile for production and
forbids another mode-probe loop; Agy's approved shell profile failed an
authorized operation. Ruling 11's requested-Astra/effective-Not-recorded
exception applies to **first core only**. At a free active-agent seat, the
Coordinator will present existing receipts and this frozen adapter contract to
the Owner for **one** routine-route decision with a fallback. Serial
observed-only containment, exact model/cwd/HEAD/path/cache readback,
same-harness lifecycle, independent diff/root review and honest unsupported
fields remain floors. No fourth active reviewer/worker is activated to decide
this while root, Coordinator and B author occupy the cap of three.

Before any worker write: assign a fresh session/branch/worktree and base after
B join; compile an exact brief with no open decision line, path inventory,
goals/non-goals, initial ≤90-call/≤55-minute/≤100k-context checkpoint budget,
one worker/no subagents, cancellation/owned-child protocol and output hash;
run `coord doctor`, leader/cap/claim checks and actual harness preflight. A cap
causes measured checkpoint/replan, never a partial join or weakened gate.
