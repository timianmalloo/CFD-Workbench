---
id: spec-amendments-1-7-1
title: "Spec 1.7.1 amendment batch — the solver-security right-size, as exact text, approved in Ruling 68"
type: spec
status: accepted
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, rulings, security, backend, openfoam]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7, rel: depends-on}
  - {to: rulings, rel: depends-on}
  - {to: note-solver-security-right-size, rel: depends-on}
  - {to: adr-0012-openfoam-backend-macos, rel: depends-on}
review-by: 2027-04-01
summary: >-
  One small batch for the spec owner, approved in Ruling 68. Four amendments to cfd-workbench-v1 (A8.5 two, A5.10
  two), each with the quoted before-text and the exact 1.7.1 text. They record the verified controlDict refusal, the
  right-sized launcher rules (ADR-0012 D2) and the install check, and make Run depend on the install smoke test
  showing Disallowing. Revision 1.7.1 carries them all; the change record is Appendix H, section H.1.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-04, reason: "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized" }
  - { by: adr-0012-openfoam-backend-macos, on: 2026-10-04, reason: "ADR-0012 D2 right-sized and DR-SEC-1 A recorded (Ruling 68)" }
---

# Spec 1.7.1 amendment batch

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.1.
**Status:** **approved** — Ruling 68 (operator as spec owner, 2026-10-04, `docs/notes/rulings.md`, last entry): apply
`docs/notes/solver-security-right-size.md` §7, with DR-SEC-1 A.

## How to read this

- **One row is one amendment**, in the form of [spec 1.7](spec-1.7.md). *Before* quotes the 1.7 text exactly; the
  script that applied the batch refused to write unless every quoted text matched exactly once. *After* is the exact
  new text in revision 1.7.1.
- **Superseded text is marked in place** (struck through, with the 1.7.1 text beside it). Appendix H, section H.1, of
  the spec is the change record.
- **Source for every row:** Ruling 68 and `docs/notes/solver-security-right-size.md` (M1 to M8, §7.2, §7.3).
  The launcher itself is decided in ADR-0012 D2; the spec cites it.

## Amendments (4)

| ID | Spec clause | Before (quoted) | After (exact new text) | Source | Affected tests or designs |
|---|---|---|---|---|---|
| AM-1.7.1-1 | A8.5 backend-substrate row, Validated where | `#codeStream`/`#calc` forbidden *(assume: OpenFOAM dictionaries can execute code — confirm at SPIKE-03)* | `#codeStream`/`#calc` forbidden ~~*(assume: OpenFOAM dictionaries can execute code — confirm at SPIKE-03)*~~ *(1.7.1: digest/hash at install and on a pin change, an install the app did not verify runs only after an "unverified install" disclosure; the app writes its own controlDict with `allowSystemOperations 0` and stops any launch whose master banner is not `Disallowing` — Verified 2026-10-04: dictionaries execute code under the default (S-1) and are refused under the app's controlDict (S-2, S-4); the app runs only case directories whose every file it emitted — Ruling 68, ADR-0012 D2)* | Ruling 68; note M1, M2, M4, M8; probes `docs/proof/spike-03/security/20261004T155708Z/` | the CI emitter test (M4); the banner-stop test (M2) |
| AM-1.7.1-2 | A8.5 backend-substrate row, Negative test | injected profile name; floating tag; a spoofed backend "Ready" must not enable Run | injected profile name; floating tag; a spoofed backend "Ready" must not enable Run *(1.7.1: the injected profile name `; #codeStream {…}` emits no directive; a `1,5` or NaN value is refused before emission; a launch whose banner is not `Disallowing` is stopped; removing `com.apple.quarantine` is a refused setup step — Ruling 68)* | Ruling 68; note §7.2, M4, M8 | the hostile-fixture emitter test; the refused-step test |
| AM-1.7.1-3 | A5.10 readiness, the pass clause | pass = the named output files exist with the recorded column layout and the fixture's Cl lies inside its tolerance; | pass = the named output files exist with the recorded column layout and the fixture's Cl lies inside its tolerance *(1.7.1: and the launch's master banner reads `Disallowing`; Run is enabled when this smoke test shows Disallowing on the user's machine — Ruling 68)*; | Ruling 68; note §7.3 | the install smoke test (cavity-lid fixture) |
| AM-1.7.1-4 | A5.10 readiness, the re-check clause | a Ready row without such a fact is Not ready — re-checked at every launch; | a Ready row without such a fact is Not ready — ~~re-checked at every launch~~ *(1.7.1: run at install and on a pin change; at every app launch only the install path is re-checked, and the build id is compared on every run from the banner — ADR-0012 D2 rule 2; an existing install is detected and hash-checked against the known builds, and one that does not match runs only after an "unverified install" disclosure is accepted — DR-SEC-1 A)*; | Ruling 68 (DR-SEC-1 A); note §7.3, M8 | the install-path re-check; the build-id comparison |

Line breaks inside the quoted spec text are wrapped in the source and shown unwrapped here.

## Outside the spec

- **ADR-0012 D2** is rewritten as the six short rules, with the evidence, the gate (Run is enabled when the install
  smoke test shows `Disallowing`; S-1 and S-2 serial stay as the pin-change check) and the DR-SEC-1 A install path.
- **Round 3** (`docs/plans/fluids-round3.md`) still runs through the spike harness on this dev machine; its S-1..S-8
  cites are updated.
