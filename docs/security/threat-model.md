---
id: threat-model
title: Application security boundary review
type: threat-model
status: proposed
owner: "@cfd-owner-20260923"
phase: architecture
tags: [security, application, files]
links:
  - {to: architecture-application, rel: documents}
  - {to: design-application-foundation, rel: documents}
  - {to: design-application-contracts, rel: documents}
review-by: 2027-03-23
summary: >-
  Rolls up the offline application's file, command, rendering and telemetry threat analysis.
  Mitigations are proposed and tested only to the extent recorded in the architecture spike proof;
  filesystem race handling and distribution trust remain independent release gates.
review-suggested:
  - { by: design-application-contracts, on: 2026-09-23, reason: "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams." }
---

# Application security review

Owner Ruling 5 assigns this authored register to the architecture author; independent approval remains
with root/Owner. The first milestone has no network listener, account, remote model or credential flow.

The B0 contract author refreshes this register under its explicit follow-on lease. Rulings 9/10 approve
bounded numeric/history/receipt policy. The new [contract proof](../proof/application-contracts.md) separates
executed session/identity/refusal checks from fixture geometry and modeled platform persistence. No native
overwrite capability or telemetry-minimization implementation is accepted through these tables.

## Trust boundaries

| Boundary | Untrusted input | Trusted action |
|---|---|---|
| File ingestion | Source/native bytes and metadata | Bounded parser, reference/hash checks and certificate admission |
| UI/CLI command | Text, draft generation, selected target | Owned command and explicit Apply |
| Filesystem | Paths, links, existing files, concurrent writer | Scoped save with exclusive claim and expected hash |
| Display/logging | Names, comments, errors, path strings | Literal native text and minimized metadata events |

## Threat register (generated)

| source | Boundary | Threat | Disposition / negative test |
|---|---|---|---|
| [design-application-contracts](../design/application-contracts.md) | Source/native input | S/T: forged hashes/evaluator/receipt; R: unauthenticated author claims | Mitigate recomputation and exact binding/replay; operation IDs are not personal attribution; tampered receipt/hash cases |
| [design-application-contracts](../design/application-contracts.md) | Source/native input | D: exponent allocation, oversized history/escaped IDs | Mitigate preallocation scans and atomic size/line preflight; hostile-exponent and growth boundary cases |
| [design-application-contracts](../design/application-contracts.md) | Consumer → session | S/T/E: certificate/target substitution or mutated returned payload | Mitigate opaque authority, session-owned draft and defensive copies; wrong-binding/retarget/mutation cases |
| [design-application-contracts](../design/application-contracts.md) | Session → filesystem | T/E/D: ancestor/reparse race, colliding creator/temp/claim | Mitigate capability-gated native adapters and owned cleanup; model and local primitive checks only, native gate remains |
| [design-application-contracts](../design/application-contracts.md) | Display/telemetry | I/R: source/path/name disclosure or missing outcome | Mitigate literal display and bounded metadata events; production marker-scan and outcome tests required |
| [design-application-foundation](../design/application-foundation.md) | File → parser | S/T: forged source/hash or evaluator | Recompute exact hashes, strict version/grammar; tampered source never accepted |
| [design-application-foundation](../design/application-foundation.md) | File → parser | D: huge nested/number/string inputs | Enforce byte/token/count/time limits before expensive math; limits±1/fuzz |
| [design-application-foundation](../design/application-foundation.md) | Source text → UI | E/I: script or format injection | Native text values only, no eval/HTML/link execution; hostile strings remain literal |
| [design-application-foundation](../design/application-foundation.md) | Save → filesystem | T/E: symlink/path escape, external replacement | No-follow path/parent validation and scoped handle policy; link ancestor/race tests |
| [design-application-foundation](../design/application-foundation.md) | Command → Apply | S/T/R: stale/forged/duplicate write | Opaque certificate binding, operation IDs and append-only facts; mismatch fixtures |
| [design-application-foundation](../design/application-foundation.md) | Local metadata logs | I/R: source/name leakage or missing outcome | Local metadata-only events, no source/path/name; capture and scan event corpus |

<!-- rolled up from 2 artifact(s) by docs-graph.py rollup on 2026-09-23 -->


## Accepted-risk register

No author-approved risk acceptance is asserted. Proposed residuals awaiting Owner disposition:

Owner accepts the cooperative-writer and OS-user ACL posture (Ruling 8). Same-user hostile processes,
uncooperative overwrite and native ancestor/reparse races remain outside any proved guarantee. Missing safe
platform capability disables publication; new-file publication requires atomic no-replace (Ruling 9).

| Residual | Proposed handling | Release gate |
|---|---|---|
| Arbitrary writer after final hash check | Declare cooperative write policy; do not promise compare-and-swap | OS-specific conflict policy and race/fault suite |
| Ancestor swap or symlink race | Handle-relative no-follow implementation, chosen directory identity | macOS and Windows real filesystem negative tests |
| Native package authenticity/distribution | Pinned packages, transitive notices/SBOM, signed application | Signing/notarization and Windows installer proof |

## Cross-cutting controls and gaps

OS-user filesystem permissions are a named platform boundary. They do not establish app-level encryption
or protect from the same user's malicious processes. Source/hash mismatches, invalid schemas and exhausted
geometry proof fail closed. No content reaches a shell/eval/network importer. Metadata events omit user
source/path/name text. The [proof](../proof/application-spikes.md) records exercised primitives; native
production I/O and complete parser fuzzing remain Not assessed. Generated tables are refreshed with:

`python3 docs/ai-forward-pack/scripts/docs-graph.py rollup --heading "Adversarial analysis (STRIDE-lite)" --type design --relative-to docs/security`
