---
id: review-application-architecture
title: Independent review of the application foundation architecture
type: proof-pack
status: in-review
owner: "@cfd-application-20260923"
tags: [architecture, application, independent-review, native-ui, provenance]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: spec-foildsl, rel: depends-on}
  - {to: domain-experts, rel: depends-on}
review-by: 2026-10-23
summary: >-
  Independent lead review of the first application architecture and contract spikes.
  Records observed native interaction evidence, contract findings and outstanding gates;
  it does not certify an application implementation or Windows runtime behavior.
---

# Independent application architecture review

Review session: `cfd-application-20260923`, 23 September 2026. The root Codex lead is
independent of architecture author `cfd-arch-codex-20260923` and technical Owner
`cfd-owner-20260923`. The lenses below are reviewer roles, not separate claimed model
executions. The author cannot clear these findings or the Owner's independent veto.

**Disposition: conditional pass for architecture direction and serial contract completion.**
Reviewed architecture commit `a92c4e74a939ed9afd4ffddbe52e5eeead6206c2`. The native toolkit
spike demonstrates a narrow interaction path. This review does not authorize production
fan-out, claim a complete G3 contract freeze or qualify the viewport as accessible.

## Authority and scope

The build basis is [product specification revision 1.5](../specs/cfd-workbench-v1.md),
identified by its `build-basis` metadata, supersession statement and link to mockup v7.
[FoilDSL 4.0](../specs/foildsl.md) supplies the normative language, identity and draft
contract. Revision 0.2 is historical context, not the implementation authority.

The first delivery must cross source opening, validation, evaluated geometry, a scoped
independent rail draft, Apply/Cancel, revision history, persistence/reopen and the shared
CLI boundary. Solver, scientific results, export and AI capabilities remain unavailable
until their own implementation and evidence exist. A successful spike is not that delivery.

## Findings and required dispositions

The findings below distinguish inspected source, observed interaction, executed checks
and retained receipts. Resolution of a spike finding does not discharge the corresponding
production implementation obligation.

| ID / lens | Evidence and consequence | Required resolution | Status |
|---|---|---|---|
| AR-01 · Data / ordering | Initial `Validation` had no candidate source/base/generation binding. `Apply` could consume stale or unrelated validation. Later source adds `ValidationKey` checks. | Bind exact source bytes, accepted base, evaluator and draft generation to an opaque validated result; refuse mismatches. | Resolved at primitive level: final source inspected; root executed nine mismatch refusals. |
| AR-02 · Data / identity | Revised certificate bound a validation key while public `Validation.DefinitionHash` remained independently replaceable. A copied validation could carry an arbitrary accepted definition hash. | Bind definition identity to the certificate and reject forged or missing identities. | Resolved at primitive level: equality guard inspected and forged/empty hash cases executed. |
| AR-03 · Geometry / Test | Python near-bound fixture initially only tested that a positive rational was greater than zero. Normalization fixture initially only compared interval endpoints. Neither proves the named admission/error behavior. | Exercise the actual certificate with near-bound rails; demonstrate a known maximum enclosure and propagation into a physical error budget. | Resolved for stated primitive claims: final functions and 30-case receipt inspected. Full placement error remains a product gate. |
| AR-04 · Persistence | Initial atomic spike re-read a nonexistent target unconditionally after writing its temporary file, preventing first save with expected hash `None`. | Prove both first save and replacement, with fault and external-change fixtures preserving the prior or complete new file. | Resolved in Python primitive: absent-target guard and final new-file/fault receipts inspected. |
| AR-05 · Security / Persistence | Initial symlink guard covered the target and immediate parent only. An ancestor symlink remained possible. The spike explicitly disclaims CAS against an uncooperative writer after the final hash check. | State the exact cooperative-writer and path policy; prove rejected link traversal, collision and fault cases. Do not advertise arbitrary external-writer CAS. | Ancestor guard/negative receipt inspected. OS handle-relative races and production store remain explicit gates. |
| AR-06 · UX / Accessibility | Native numeric field and Open button expose names and keyboard behavior. Custom viewport exposes role `unknown`. | Use a meaningful native automation peer and keyboard/table equivalent in the application. | Spike limitation observed; product gate remains open. |
| AR-07 · Platform / Test | macOS native flow observed; no Windows native interaction observed by root. | Separate macOS runtime, Windows build/headless evidence and Windows native obligations. | No cross-platform runtime pass claimed. |
| AR-08 · Discoverability | `.gitignore` rule `spikes/` hides the three new spike sources from ordinary status/add. | Track exactly the source/project/oracle files; exclude generated binaries, packages and scratch. | Sources committed; final source fingerprints independently matched. Coordinator verifies complete path inventory at join. |
| AR-09 · Data / contract readiness | First slice design describes the envelope fields and a deliberately partial session interface, but not a complete frozen schema/session contract. | Complete exact record/reference/extension/version rules and executable session contract before dependent fan-out; alternatively make serial contract completion an explicit gate before implementation. | Conditional: exact proposed envelope and deterministic history rules inspected; serial executable contract gate remains mandatory. |
| AR-10 · Data / history | ADR 0002 distinguishes source-only, Design and Surface revisions. The first draft describes one accepted transaction row without spelling out all three identities. | State how a comment-only source transaction leaves Design/Surface identity unchanged while retaining source history and undo. | Resolved in design: accepted source facts may reuse Design identity and Surface hash. Implementation tests outstanding. |
| AR-11 · Governance / delivery | ADR's rollup ownership statement predates Ruling 5. Design says every stage preserves a CLI, while the proposed CLI writer comes after the core track. | Align the nine-file author lease; distinguish internal dependency tracks from the delivered vertical M1, or give the early track an actual CLI path. | Nine-path scope updated; B is an internal serial contract/core track, not a delivered M1. |
| AR-12 · Persistence / cleanup | Owner review found that an exclusive temporary-file collision could reach unconditional cleanup and delete the pre-existing file. Root reads the final control before disposition. | Delete only a temporary file created by this invocation; prove pre-existing temporary and lock sentinels survive refused writes. | Resolved in primitive: `temp_created` inspected; two collision cases passed in final receipt. Author's guard-removal RED deleted the sentinel; production port must retain this control. |

**Disconfirmed reviewer concern:** root initially suggested that the normalization error bound
assumed a symmetric profile. Owner challenged that premise. Direct re-reading of FoilDSL §6
confirmed `q=(x,C ± thickness*T/2)`: camber is fixed, so it cancels from the difference between
normalization endpoints. The concern is withdrawn; no symmetry restriction is introduced.
The remaining bound still needs to distinguish thickness-normalization uncertainty from
abscissa inversion and trigonometric evaluation error. This is a review correction, not a
product requirement change.

## Observed native interaction

Root used the native computer-use interface against bundle
`org.cfdworkbench.architecturespike`, titled **CFD-Workbench architecture spike**:

1. The screenshot displayed the numeric input, Open foil button and a cyan section
   outline on a black viewport. These are explicitly illustrative spike graphics.
2. The accessibility tree named **Leading edge x in metres**, initially `0.014049`,
   and **Open foil file**. Root changed the numeric text to `0.020`.
3. Tab focused the Open button. Return opened the native **Open FoilDSL** panel.
4. Escape dismissed that panel. The numeric text remained `0.020`, focus returned
   to the Open button and the status read **Open cancelled. Accepted source unchanged.**
5. The viewport's accessibility role was `unknown`, despite its descriptive name.

Confidence: **Verified for these observed controls and transitions only**. No actual
FoilDSL file was loaded by the spike. No parser, geometric edit, revision or save flow
was exercised through this UI. Earlier root attempts returned `cgWindowNotFound` while
the process remained alive; subsequent binding and the flow above succeeded after the
window was visible. A running process alone was not counted as rendered-surface proof.

## Gate disposition and residual obligations

Root read all four architecture/design/proof documents and both security/privacy registers.
The final Python receipt contains 30 passing checks and its explicit limits; root inspected
that receipt and the focal algorithms. Root independently executed `--contracts`: matching
binding accepted, nine mismatches refused. All three source hashes match the proof document.
macOS native input/picker proof was independently observed as above. Build/publish results
are the author's retained evidence; Windows execution is not inferred from a cross-build.

| Adversary lens | Disposition |
|---|---|
| Data & Persistence / Distributed Systems | Conditional pass for immutable-source direction and serial contract completion; no parallel interface freeze yet. |
| Computational Geometry / Test Architect | Conditional pass for the explicit conservative proof strategy and exercised primitives; parser, C# JCS, complete certificate assembly and error budget remain implementation gates. |
| Security & Identity / Privacy | Conditional pass for offline boundaries and explicit residuals; native path races, exact schema validation and metadata minimization need production negative tests. |
| UX & Accessibility / Marine CAD UX | Toolkit interaction spike passes its bounded flow. Product accessibility, complete states and full authoring workflow are not accepted. |
| Simplifier / Enterprise Architect | Native modular monolith, one core and local source facts avoid premature IPC/database/framework layers. No load-bearing abstraction identified for deletion in this proposal. `net: -0 lines possible`. |

The generated rollups expose a separate mechanical defect: links were emitted relative to
the docs root although embedded in `docs/security/`. Root's destination-relative generator
fix and failing-then-passing regression must join before those tables are regenerated.

Remaining gates:

- Complete executable session/schema/identity/persistence contracts before implementation
  or dependent fan-out; preserve the distinct source, Design and Surface revision semantics.
- Implement and independently prove parser, canonicalizer, continuous geometry certificate,
  normalization/placement error bounds, cancellation and conservative Not assessed outcomes.
- Exercise actual native save/reopen, path and fault behavior on target platforms; Python
  primitives do not establish .NET filesystem guarantees.
- Build the complete authoring flow to the tokens and native accessibility contract;
  prove macOS and Windows runtime behavior separately.
- Reconcile the selected direction with project instructions/specification and regenerate
  the security links through the repaired tool before integration.

The architecture author has not self-cleared any veto. This independent review clears
the direction only under the conditions above. Owner Ruling 8 records matching conditional
approval. Serial contract completion, production implementation and runnable M1 proof remain
outstanding.
