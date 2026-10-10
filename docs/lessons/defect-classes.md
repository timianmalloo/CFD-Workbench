---
id: defect-classes
title: CFD-Workbench defect-class register
type: doc
status: accepted
owner: "@timianmalloo"
tags: [lessons, controls, geometry, specification]
links:
  - {to: spec-cfd-workbench, rel: relates-to}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: mockup-workbench-v1, rel: relates-to}
  - {to: review-pr-13, rel: relates-to}
  - {to: proof-r163-windows-ring, rel: relates-to}
  - {to: review-pr-9, rel: relates-to}
  - {to: proof-win-naca, rel: relates-to}
review-by: 2027-03-19
summary: Design-time failure classes and their mandatory checks, loaded at session grounding under AGENTS.md. Product-runtime controls remain explicitly pending until the corresponding implementation exists.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: mockup-workbench-v1, on: 2026-09-20, reason: "Mockup v1 built against spec v1 and cleared by the UX & Accessibility lens 2026-09-20; supersedes the 2026-09-19 prototype as the review artifact." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# Defect classes

This register is an always-loaded grounding control under AGENTS.md. Each row makes a future author check the class before changing its surface. Acceptance oracles below are specified controls, not claims of executed application tests.

| Class | Class → sweep → derive → prevent | Control and status |
|---|---|---|
| RAW-CAPTURE-BLOB-MISMATCH · Raw evidence bytes change before delivery | A capture was normalized to LF in commit `1f2cc9f8` (13,584 to 13,499 bytes) and restored in `b5c6b52e`. Swept the Ruling 163 captures and their committed blobs. Derive identity from the committed blob, not a pre-commit working file. | `docs/proof/r163-windows-ring/verify-captures.py` compares every receipt-declared SHA-256 and byte count with `git show HEAD:<path>`; run it after commit and before delivery. The capture directory's `.gitattributes` preserves raw files with `-text`. |
| CAPTURE-DEADLINE-DRIFT · Relative waits and late termination let a ring exceed its total ceiling. | Ruling 168 run 1's capture-entry-to-tree-stop UTC span was 305,960.528 ms (+5,960.528 ms); the separate launch-to-stop monotonic clock was 302,154 ms (+2,154 ms). Its `WaitForExit(300000)` began after setup. Swept setup, launch, wait, process-tree termination, and final envelope measurement. Derive one monotonic deadline at entry, route every wait through the remaining-time wrapper, request tree termination at expiry, and reject an entry-to-stop UTC envelope over the active ruling ceiling. | `docs/proof/ring-windows/verify-capture-deadline.ps1` runs `capture-calibration.ps1 -SelfTest`, then mutates the shared wait to `WaitForExit(900000)` and disables the envelope result; both mutations must fail. The helper checks both waits use only the remaining budget and fails an over-ceiling envelope. Exact 300 s repair output is retained in `docs/proof/ring-windows/calibration/deadline-self-test.txt`; the 900 s test output is retained in `calibration-ruling-170/deadline-self-test.txt`. Run 1's receipt preserves both clock grains and the original taskkill output. |
| WSL-INLINE-ARGV · Nested PowerShell → `wsl.exe` → `bash -lc` inline strings lose or expand arguments. | Shell-boundary quoting can erase or expand Bash arguments, causing a source or metadata command to differ from the intended argv. PR #11's two probes did not consistently run the requested activation or retain raw output. | WSL commands run from committed script files, never inline `-lc` strings (CT27). Ruling 153 uses `docs/proof/win-cfmesh/probe-r153.sh`. Mechanical control `tools/check-wsl-inline.py` (check-docs, fast ring; 2026-10-09). Controlled. |
| WIN-SIGNAL-NUMBER · A signal integer is treated as portable across operating systems. | Swept stop configuration, monitor behavior, and receipt labels together. Derive the symbolic signal name from the target OS before dispatch; signal 30 is Linux SIGPWR and macOS SIGUSR1, while Linux SIGUSR1 is 10. | Always-loaded rule: verify and record the target OS's symbolic signal name before sending a numeric signal. Linux signal-30 stop evidence is in `docs/proof/win-naca/receipt.md`; shared monitor change remains out of scope. |
| WSL-CALLER-LIFETIME · A detached WSL unit inherits the lifetime of its launching `wsl.exe`. | Swept launch, caller exit, and unit state together. Derive a named keepalive independent of the caller. | Always-loaded rule: do not accept a detached WSL launch until the named keepalive is active after `wsl.exe` exits. Ruling 151 C2 readback remains open for the later done receipt. |
| RAW-LOG-PATTERN · A `*.log` rule misses files named `log.*`. | Swept native log basenames and compared worktree bytes with staged Git blobs. Derive attributes from the actual basename. | `.gitattributes` covers `l6-results/log.* -text` and `su2-results/log.* -text`; the closing manifest compares proof files with Git blobs. No raw log bytes are changed by this correction. |
| CASE-SCHEMA-AFTER-LAUNCH · Case schema validation runs only after a launch. | Swept launch preparation for a pre-launch validator. Derive the existing case validator as the authority. | `docs/proof/win-naca/freeze-su2.py` writes the inputs and case YAML, validates that YAML with the real case validator, then stages and freezes; validation precedes launch, not the initial writes. `tools/check-docs.py` runs the same validator. Red/repaired evidence is retained in `all-case-validation-at-cap.txt` and `all-case-validation-after-repair.txt`. |
| ABSENCE-CLAIM-UNSUPPORTED · A summary asserts absence while retained raw evidence contradicts it. | Swept each absence statement against a search over the retained evidence. Derive that an absence claim is valid only with its exact grep and empty result. | Always-loaded rule: every absence claim cites the command/search and its empty result; if the search returns evidence, list it and withdraw the absence claim. Ruling 165 inventories the retained `RESULT` lines. |
| PY-TRUNCATE-BEFORE-VALIDATE · A file writer truncates its destination before validating write options. | A `Path.write_text` call opened and truncated `l3-prepared.json` before rejecting an invalid newline option. Swept this update path. Derive: validate and serialize before opening the destination, and use a temporary file plus replace when preserving existing evidence matters. | Always-loaded rule: prepare output bytes before mutating an existing file; for generated evidence, write a temporary sibling and replace only after serialization succeeds. This file was restored from HEAD and hash-updated; the retained raw ring logs were not involved. |
| GEO-A · Competing shape authority | Catalog assignment, thickness interpolation and a t/c channel can each appear to own thickness. Swept A4, GEO-07, profile conversion and result invalidation. Derive effective coordinates from one t/c owner after normalized shape blending. | Standing review rule: blend profiles with different thickness-peak locations, renormalize before t/c; test retained override versus source thickness. A4/GEO-07 specify it; runtime fixture pending. |
| UX-A · Optional path becomes mandatory in flow | A flowchart linked export only from accepted AI output despite global export prose. Swept F1–F5 and IA for optional-dependency bottlenecks. Derive independent entry for global actions and a failure return path. | Standing flow review rule: trace no-key New→edit→save→export without AI nodes. F5 corrected; future keyboard E2E trace is required. |
| SPEC-A · Scope named but not falsifiable | Broad “analysis” requirements omitted transition/load/critical-speed outputs named in proposals. Swept all six source stages against story IDs. Derive explicit supported and unavailable paths. | Source-to-story coverage matrix plus ANA-10–14/AI-05; independent Product/Test review confirmed closure. |
| FRAME-A · Handedness asserted from intuition | A transient draft mistakenly called aft/starboard/up inconsistent. Swept coordinate, incidence and mirrored-half wording. Derive from forward/starboard/down: flipping x and z preserves determinant +1. | Always-loaded rule: express basis against a known physical reference and calculate determinant before changing signs; never “correct” a source from mental rotation alone. Final A4 uses the source's right-handed frame; numerical geometry tests remain a handoff requirement. |
| FIT-A · Approximate conversion treated as categorical error | Draft prohibited a fit from reporting zero, even if measured. Swept conversion, CST and source-identity claims. Derive error from observed comparison, not method label. | Always-loaded rule: conversion reports measured residual and tolerance; zero is permitted only with evidence, never assumed. A4 updated. |
| UI-B · Hidden state overridden by authored display rule | The specification contents filter set `hidden`, but the navigation `display:block` rule kept all 27 links visible. Swept prototype author guidance for the same selector shape. Derive proof from rendered visibility, not the attribute alone. | Explicit `[hidden]` rule; browser oracle observed 27 visible links before the fix and exactly 1 after the geometry query. Standing rule: harness/filter assertions inspect rendered visibility. |
| UI-C · Rerender discards focus or capabilities | Workspace replacement dropped keyboard focus to BODY; narrow CSS hid project navigation. Swept all task routes and review selectors. Derive semantic focus restoration and an explicit project drawer instead of hidden functionality. | Portable `tools/check-mockup.mjs` covers repeated keyboard edits, dialog return focus and narrow project access. **Recurred in mockup v3 (2026-09-21)** in every renderer that replaces its container — fixed as a class with one `withFocus` wrapper over nineteen renderers and a `focusKey` that names every `data-*` control identity; `tools/check-mockup-v3.mjs` activates one control per shell region *and* per document form by keyboard and asserts the same logical control keeps focus. Rule: a mockup wraps its renderers before it writes its first one. Independent reviewer observed the original failures and corrected behavior. |
| EVID-A · Historical presentation reads mutable current state | Results were labeled revision 12 while rendering current edited stations. Swept snapshot geometry, pressure masks and source labels. Derive the result view from its immutable fixture input; partial evidence must actually be masked. | Browser oracle edits the current shape and requires historical Results SVG unchanged; partial Results must contain masked paths. |
| GEO-B · Readout and preview use different evaluators | Quintic preview replaced linear geometry, but area still summed linear station segments. Swept area/AR and profile preview commit semantics. Derive readouts from the same curve evaluator, or label an unaccepted profile preview without mutating the design. | Tangent-edit regression requires linked dimensions to update; unaccepted section preview leaves design revision/geometry intact. Controls live in the portable mockup check. |
| DOC-C · Generated edition repeats canonical metadata | The HTML header hardcoded revision 0.1 while the updated Markdown declared 0.2. Swept renderer, visible body, source hash and browser parity. Derive the header from the canonical visible revision. | `tools/check-spec-html.mjs` now checks edition-label equality as well as full text/hash. Observed red: stale HTML returned `revisionMatches:false` and `hashMatches:false`; final regenerated evidence is recorded in the iteration proof. |
| TEST-A · Sensitivity oracle omits degeneracy and constraints | Draft GEO-13 required every weight increase to change the curve, including zero-influence or fully locked fixtures. Independent review disconfirmed the universal condition. Swept weighted-control, endpoint and lock acceptance wording. Derive expected sensitivity from a nondegenerate unlocked fixture; locks use a separate preservation/rejection oracle. | Always-loaded rule: geometry property tests name nondegeneracy, nonzero influence and applicable constraints before requiring a change. GEO-13 carries those preconditions; the mockup check exercises a specific off-curve control. Production evaluator tests remain required. |
| UI-E · Render failure leaves plausible prior evidence | A slice-preview local-name collision threw during a 2D switch, leaving the prior 3D view visible. Swept view selection, field selection and sample replay: selected controls alone are not proof of rendered state. Derive field/view presentation from selected identity and reject stale evidence after errors. | `tools/check-mockup.mjs` checks actual 2D SVG identity and fails on page exceptions, alongside sample/metric linkage. The author observed the view oracle fail on this transient defect before fixing its variable scope. |
| GEO-C · A draft follows mutable selection instead of its edit target | New curve drafts could be retargeted when station/channel selection changed. Swept section, outline and selection handlers. Keep target ownership explicit. V7 preserves a draft during read-only inspection and refuses competing writes; selection never transfers ownership. | Browser regression switches target with an active draft and requires unchanged draft/base/source and no cross-station/channel application. `tools/check-authoring-v7.mjs` covers another station and Source; the CAD oracle covers another curve. Production edit drafts bind base revision and target identity. |
| EVID-B · Missing-variable status overstates missing-result scope | The partial wall-shear state hid only a field but said coefficients and forces were absent while showing them. Swept missing case versus missing variable messages and metric rendering. Derive availability and copy at their actual scope. | Independent rendered review identified the mismatch; the missing-wall-field oracle requires field-specific copy with valid metrics retained, while failed-sample checks require all sample evidence cleared. |
| SPEC-B · One quantity, two definitions in one artifact | Specification v1 defined the identity tolerance as "1 µm AND 10⁻⁶ relative, both must fail" in A4.1 and "pass iff ≤ 1 µm AND ≤ 10⁻⁶" in A4.5 (opposite logic), and the run key with two member lists neither of which held the settings hash. Swept every term the spec defines more than once (identity tolerance, run key, Example, revision, supported, Unavailable). Derive: one section owns the definition; every other mention cites it. | Gate rule at `/specify` Stage 4: a Test Architect sweep greps each defined term for a second definition; the v1 gate observed the Blocker before the fix. A future `tools/check-spec-terms.py` lint is the pending mechanical control. |
| DATA-B · Derived number printed from rounded intermediates | KB 04's goal-state row printed CL 0.774 because q had been rounded to 13,570 Pa before the division; exact arithmetic gives 0.773, and the spec promised reproduction "to displayed precision". Swept the seven-point table and every figure the spec cites from it. Derive every displayed number from pinned inputs in one executed script, never by hand. | `scratchpad/goal_fixture.py` re-executed the table (pinned 1 kn = 0.514444 m/s); GOAL-02 now names its pinned inputs; the mockup's Brief computes the triples live and the browser oracle asserts them. Rule: a KB table of derived numbers carries its inputs and its generating script. |
| UI-F · Unscoped attribute selector binds a handler to the wrong element | The mockup's `[data-mode]` click binding matched both the mode buttons and the window element carrying `data-mode` state, so any click in the window reset the curve mode. Swept every `document.querySelectorAll('[data-…]')` in the artifact for an ancestor carrying the same attribute. Derive: state attributes live on the root/window, control attributes on buttons, and handler queries are scoped to their container. | `tools/check-mockup-v1.mjs` observed red (Smooth mode lost after clicking the weight field) before the fix; handler queries now scope to `#editor-side`; the window carries no attribute a control also uses. |
| UI-G · Review chrome measured as product surface | The craft detector flagged seven harness colours as off-token and a fixed drawer was hidden behind the sticky harness, so the review instrument produced findings and blocked a control. Swept the harness palette and every fixed-position overlay. Derive: review chrome uses the design language's dark tokens, and product overlays (Checks drawer, assistant) are positioned inside the window, never against the page. | Craft gate re-run clean but for one recorded deviation (the spec's em-dash strings); the browser oracle clicks the drawer close control while the harness is sticky. |
| SPEC-C · A derived state named without its function | Revision 1.1 said an Experiment's status was "derived from its runs" and listed six values; no rule decided the status of eleven Completed and one Failed case, so no test could assert it. Swept every "derived" in A3.1–A3.4. Derive: a derived attribute is written as a function of named facts and events, with the mixed-outcome input as its failing input. | Gate rule at `/specify` Stage 4: every "derived" carries `= f(…)`; the 1.1 gate observed the gap and the status function now sits in A3.1 with RUN-05's mixed-outcome Given. |
| DATA-C · A reference by hash to a mutable singleton | An Experiment referenced the Goal state by hash while the Goal state was a single editable value, so one constraint edit and a save left the reference with no referent. Swept every by-hash reference (Design revision ✓, Profile revision ✓, Goal state ✗, Backend environment ✗ — installation-local). Derive: anything referenced by hash is an append-only version; anything installation-local is referenced through a pinned value copied at the reference time. | Goal state and Setup brief are append-only entities; the Experiment carries a backend pin value; GOAL-03's reopen-and-resolve Given is the failing input; DM10 applies to references, not only to updates. |
| SEC-B · Allow-listed action with unbound parameters | The environment assistant allow-listed five actions and showed "the exact command", but nothing said where the digest, URL, distribution name or limit came from — a well-formed wrong digest would pass consent. Swept every proposal kind for parameters a model could supply to an executing step. Derive: a proposal carries an id and, at most, one bounded scalar; the tool binds every other parameter from published product data. | A5.10/A5.12/AI-11/A8.5: step id only; four negative tests (wrong-but-valid digest, out-of-range limit, metacharacters, `curl … | sh`); the tool never elevates. The v2 mockup renders a refused step. |
| UI-H · Duplicate object key silently overrides state | The v2 mockup's model literal declared `run:` twice (the freshness run object and the new run-console state); the later key won and the console crashed on `undefined.state`. Swept the model literal for duplicate keys and the render code for shared names. Derive: one object, one key; a new sub-state gets a new name. | The smoke run observed the crash before the oracle; the console state is `runs`; the assembler could lint duplicate keys in object literals (pending mechanical control). |
| UI-I · A rendered value false to its own fixture | The v2 mockup printed "A_cav 0.041 ≤ 0.02" as a fixed string, a sine wave labelled "difference flood", literal station deviations ("0", "0.004 mm") and a per-sample colour range under a "fixed range" caption — each a number or comparison the fixture did not produce. Swept every rendered inequality, every "computed" visual and every deviation readout. Derive: a comparison is rendered from the two operands; a visual named after a computation is computed from its inputs; an unmeasured quantity says "Not recorded". | The lens found all four by reading (the craft gate cannot); the v2 oracle now asserts the data-driven comparison strings, the computed difference twin, the "Not recorded" readouts and the series-fixed range caption. Rule for mockups: any string that states a relation carries its operands. |
| UI-H2 · Web-page habits in a client mockup | The v2 mockup rendered the client as a stacked page: 1,450–6,500 px tall at every window width, page scroll everywhere, no panel scrolling internally, the area strip wrapping to two or three rows inside a 64 px title bar, and toolbar controls that scrolled or clipped. The archetype signature said `Layout:ViewportWorkbench`; the build did not. Swept the layout facet against the signature and every region against "scrolls inside itself". Derive: for a thick client the window is the unit — a fixed frame, internal scroll per region, a toolbar that fits or overflows into a menu, never wraps or scrolls. | `tools/check-mockup-v3.mjs` fails on window scroll, toolbar > 44 px or overflowing, parameter row > one row, `More ▾` without a hidden group or any hidden group at ≥ 1280 px, or a dock that does not scroll internally, at five presets × six areas; the in-artifact audit prints the shell verdict on every render. Rule: a mockup for a client declares its window presets and proves the shell contract before its content. |
| UI-J · A drawing where the geometry is the interface | The v3 mockup drew every curve as a 60-point polyline, showed one projection per named view with keyboard-only rotation, edited the four channels only as η-plots and opened the section editor as a modal — a *viewer with a curve pane* where the user expected a CAD model. Swept every curve, every view and every editing surface. Derive: for a geometry tool the drawing *is* the interface — every curve a spline, one camera over one model with named presets and free orbit, control curves edited on the elevation that shapes them, and a section edited as a document in the editor group, never a modal. | `tools/check-mockup-v4.mjs` group 13 fails on a polyline in a geometry view, a named view that is not the camera, a channel without a handle in its elevation, an edit that does not open the shared draft, or a station editor that is a dialog; the register rule for mockups: declare the drawing model (splines · camera · elevations · documents) before the first view is built. |
| UI-K · A surface token used on the other surface | The v4 mockup drew the focus ring on the graphite viewport with the light surface's `--focus` (2.45:1) and the refusal ring with `--danger` (2.30:1); a token-pair audit that lists the intended pairs cannot see a token used off its surface, and an oracle that asserts *token identity* on the focused element proves nothing about contrast. Swept every state colour drawn inside `.viewport`, `.rail`, `.statusbar`. Derive: every state token has a viewport twin (`--focus-viewport`, `--danger-viewport`) and a rule that names the surface it belongs to; contrast is measured on the computed pair, never inferred from the token. | `tools/check-mockup-v4.mjs` focuses a viewport handle in all three themes and computes the ring's contrast against the computed viewport background (≥ 3:1, ≥ 3 px); the in-artifact audit lists both viewport pairs; DESIGN.md §2 names the twins and forbids the surface tokens on graphite. |
| UI-L · A drawing rendered for one box and shown in another | The v5 mockup drew each viewport's SVG with a `viewBox` sized at render time (with a 120 px floor) and CSS `width:100%`; whenever the box changed afterwards — a dock collapsing, the bottom panel, the window preset, the floor itself — the SVG scaled and its 12 px labels read 11.7 px and its 40 px vertex hit circles 17 px, under both floors, while every token and box assertion stayed green. Two sibling shapes in the same sweep: a closed `<details>` menu whose items still had a box (48 counted as small targets) and `innerText` of a hidden viewport that includes those items. Swept every SVG that takes its size from its container and every closed menu. Derive: a viewport renders at its true pixel size (no floor), re-renders synchronously from a `ResizeObserver` (a deferred frame can be throttled), closed menus are `display:none`, and the oracle measures text and target sizes *after* the walk, not on a fresh page. | `tools/check-mockup-v5.mjs` groups 1–2 measure the smallest SVG text and the target floors at five presets × six areas after the shell walk; group 13 asserts the forced single viewport at the reflow preset; the in-page audit ignores `details:not([open]) .menu`. |
| UI-M · A drawing rendered from a quantity it does not carry | The v5 station document printed "8 per side", "6 controls each" and "conversion residual 0.000" for a section whose fit had nine vertices and, once measured, an 841 µm residual: three surfaces, three constants, no operand. Swept every rendered count, residual and deviation in the workspace. Derive: a rendered number is computed from its operands at render time, or it reads "not recorded"; the same quantity on two surfaces comes from one function; and the conversion chooses its vertex count *by* the measured residual (centripetal parameters, knots by averaging) rather than asserting one. | `tools/check-mockup-v5.mjs` group 6 asserts the residual shown in the HUD, the strip and Properties equals the residual measured and meets the acceptance, and that Delete, Rebuild and Fit points report a deviation from the curves. |
| UI-N · An assertion the failure mode also satisfies | The v5 oracle's pointer-drag test asserted only the *direction* of the change ("dragging aft lengthens the chord"); the drag mapped the pointer through the SVG captured at press time, which the first re-render detached (its box read zero), so every drag slammed the vertex to its ordering limit — aft — and the test stayed green. The operator found it with a trackpad. Two siblings in the same sweep: hit circles that overlapped at the tip (a press grabbed whichever painted last) and a focus-preserving re-render whose restored focus re-selected the *previous* vertex through its focus handler. Swept every pointer test for direction-only assertions. Derive: a drag test asserts the **relation** (the glyph stays under the pointer to ≤ 2 px over a dozen small moves), a hit test **presses** every vertex at its own centre and reads the selection back, and any handler that calls `preventDefault` on a press focuses its target itself before the re-render. | `tools/check-mockup-v5.mjs` group 6: the twelve-step drag with the pointer-offset bound, the press-every-vertex selection sweep, focus on the dragged vertex on release; the mockup's `liveMap` resolves the svg and mapping per move and retargets a press to the nearest vertex centre. |
| UI-O · A value shown rounder than its own edit step | v9 showed a plan point's position to 0.1 mm in Properties and on its canvas label while ⌘-nudge moved it 0.01 mm and the Points grid showed 0.01 mm: the nudge was invisible in two of three places, and the same quantity had two precisions. Swept every readout in v10: station distance rounded 212.5 → "213 mm"; the canvas "TE gap" at 0.1 mm against 0.01 mm in Properties. Derive: a quantity has one precision everywhere — the finest step that edits it (typed or placed lengths 0.01 mm; derived lengths 0.1 mm; angles 0.1°). | `tools/check-mockup-v10.mjs` gate "fine nudge visible everywhere" (model, Properties, grid and canvas label agree after a ⌘-nudge); rule in `DESIGN.md` §12.0e. |
| UI-P · A hard state dropped when the shell is rebuilt | v8 rendered first-run, loading and open-failed states on a Start screen; v9 rebuilt the shell and its harness kept only default / invalid / overflow, so the empty and loading states silently disappeared and the v9 review did not notice. Swept the v10 harness against the DX9 list. Derive: a rebuilt surface re-derives its state inventory from the list, not from the previous harness. | `tools/check-mockup-v10.mjs` iterates the first-run, opening and open-failed screens in every combination and gates "first run, opening, cancel, open failed". |
| UI-Q · Drawing that takes the pointer or hides focus from a target | In v9 the selected point's handle line was drawn over its own centre, so a press on the centre hit the line (no handler) and the focus check found the point covered; the v9 sweep had exempted SVG targets from its occlusion check, so it never saw it (a sibling of UI-N). A coincident neighbour (trailing-edge upper and lower end points) also covered a focused point. Derive: decorative drawing gets `pointer-events:none`; the selected point paints once more on top; an occlusion measure has no exemptions and states what it measures (hit-testing plus the raised clone, not paint). | `tools/check-mockup-v10.mjs` gate "no focused model-area target hidden by or overlapped by a float" (318 targets, SVG included, centre plus 8 samples) and the Tab walk with a float. |
| UI-R · A value accepted that the user did not type | v9 and early v10 parsed typed numbers with `parseFloat` and returned silently on failure: "12abc" became 12, "abc" left the text in the box with no message (WCAG 3.3.1). Swept every input: Properties fields, Points-grid cells, Wing dimensions. Derive: one validator for every typed number — `Number()`, lengths > 0, `aria-invalid` and an alert tied by `aria-describedby`, geometry unchanged, Escape restores. | `tools/check-mockup-v10.mjs` gate "a11y cycle 1: invalid numbers refused with a message"; red on v9 (`12abc` → 12, no `aria-invalid`), green on v10. |
| UI-S · An edit outside a draft that the draft's rollback discards | Early v10 left Span / Root chord / Tip chord editable inside the section editor; the section draft's snapshot covers the planform, so Cancel silently discarded a planform edit, and Escape on a point cancelled the whole draft. Derive: inside a draft, only the draft's own object is editable; Escape steps back and never discards — only Cancel does. | `tools/check-mockup-v10.mjs`: `dimsReadOnlyInSection`, `a11y3_escapeKeepsEdit`, "Finish adds one undo step, Cancel none". |
| STATUS-CLOBBER · A background completion overwrites a newer message | The accepted-slice sampling started by Open / New foil runs ~0.25 s on the pool, and its completion wrote `Status` unconditionally (`WorkbenchController.RefreshAcceptedAsync`). An arrow on a fixed point in that window showed "This RootEnd point is fixed…", then the stale "Accepted η 0.5 slice…" report replaced it. The reopen notice "A separate recovery draft is available." was lost the same way. Seen as the flaky `PlanCanvas_LockedNudge_AssertiveLockCopy`: a warm suite process builds the fixture faster than the sample, so the key lands mid-sampling (16/20 in 4 loaded lanes before the fix, 0/52 after). Swept every async `Status` writer. The section-draft Preview reports (`WorkbenchController.cs` ~1119, ~1473) keep the same unguarded shape behind their own version guard. The synchronous "Sampling…" placeholder still replaces a command's own report (Span applied, commit report); that is a separate, deliberate ordering. Derive: a background report replaces only the placeholder it wrote; an error may still replace. | Every `Status` write bumps a counter. The sampling report writes only if the counter still equals the value it had after its placeholder. Red-first: `LockedNudge_DuringSampling_LockCopySurvivesCompletion` (`--controller-shell`) presses the lock during sampling, waits on the completion signal (Provenance `accepted` via `Changed`), and failed 3/3 before the fix. `PlanCanvas_LockedNudge_AssertiveLockCopy` also asserts the rendered status line and prints status, line and live setting on failure. Rule: an async writer to a shared message slot carries a staleness check against the slot, not only against the model version. |

## Application architecture boundary sweep — 2026-09-23

**DATA-E recurrence · Validation detached from its candidate or derived identity.**
The first compiling port sketch allowed Apply to receive an unrelated validation result;
the first repair still allowed its public definition hash to be replaced independently.
Sweep: source bytes/hash, accepted base, draft ID, generation, evaluator, assessment and
certificate identity. Derive: one opaque certificate binds those inputs and its computed
definition hash; Apply compares all bindings under the transaction boundary. Prevent:
`tools/spikes/ApplicationNativeUi/Program.cs --contracts` exercises nine mismatch cases,
including a forged/empty definition hash and a moved accepted base. This is a binding
primitive, not proof of a production parser, session or geometry evaluator. Port the same
negative cases into the production core's required gate before its first acceptance.

**UI-N / TEST-A recurrence · A named oracle does not invoke its claimed behavior.**
An initial near-bound geometry check only compared a positive rational with zero, and a
normalization check only ordered interval endpoints. Sweep: every spike check's name,
focal function, failing input and claimed confidence. Derive: exercise the actual
separation function near its boundary and test a known maximum plus physical error
enclosure. Prevent: `Chord_NearBound_AdmitsPositiveRefusesTouching`,
`Maximum_KnownQuadraticDegreeElevated_EnclosesQuarter` and
`Normalization_EnclosurePropagated_PhysicalErrorBelowOneNanometre` in
`tools/spikes/application-contract-vectors.py`. These prove bounded primitives only;
they do not establish full-language conformance.

**IO-A · Cleanup deletes an artifact this invocation did not create.** Owner review
found that exclusive temporary creation could fail on an existing file, then `finally`
unconditionally removed that file. Sweep: target, temporary and sidecar-claim creation,
failure and cleanup paths. Derive: acquisition and ownership precede cleanup; a collision
does not transfer ownership. Prevent: the spike tracks `temp_created` and includes
`Atomic_TemporaryCollision_PreservesUnownedFileTargetAndReleasesClaim` and
`Atomic_ClaimCollision_PreservesOtherClaimAndTarget`. The guard is a local cooperative
writer control, not proof against an arbitrary hostile filesystem race.

**FFI-A · Matching apparent argument types hides a variadic native ABI mismatch.**
A real macOS native Save requested `0600` but published mode `0454` with correct
project bytes. The one-variable arm64 probe reproduced wrong creation modes when
`openat` was declared with four fixed arguments; the correct three-fixed-plus-
variadic declaration created `0600`. Sweep both `open` and `openat` in the
production store and the native primitive test helper, then independently read
temporary, overwrite-claim and final inode modes. Derive: a C compiler makes
the variadic call behind a fixed managed bridge, and the held descriptor must
show regular-file mode exactly `0600` **before any project byte write**; a
post-create chmod cannot establish safe creation. Prevent: candidate
`ee6d73ad` adds `Store_CreationPermissions_BeforeWriteAndAfterPublication`,
`Store_UnsafeCreatedMode_RefusedBeforeBytesWithOwnedCleanup` (extra `0777`
bits on create/overwrite), `Store_MissingOwnerPermissions_RefusedBeforeBytes`
(zero/one owner-bit loss on create/overwrite), and
`Store_OwnerStrippingUmask_FailsClosedWithoutRepair`. The core gate runs the
normal mode matrix under isolated child umasks `0000`, `0022` and `0077` in
build and published layouts; it separately exercises owner-stripping umask
`0600` as a fail-closed capability case. Root's
[investigation](../investigations/native-save-permissions.md)
retains the original file unchanged. The candidate is unjoined; independent
packaged native Save and Owner review still gate production acceptance.

**REVIEW-A · A reviewer substitutes a familiar equation for the normative transform.**
Root initially treated thickness normalization as scaling the whole section, then
incorrectly questioned a bound for cambered sections. Owner disconfirmed it; direct
FoilDSL §6 inspection shows fixed camber in `q=(x,C ± thickness*T/2)`, so C cancels
between normalization endpoints. Sweep: review claims about normalization, placement
and conversion error. Derive: trace exactly which terms change before asserting a
failure. Preventive always-loaded rule: before making a mathematical veto finding,
quote the governing equation and map the challenged computation to its terms. Read
the equation now; do not rely on a remembered model. The incorrect symmetry restriction
was withdrawn and is not a product requirement.

**REVIEW-B · A reviewer reads a call but not the supplied configuration.**
Coordinator saw `SerializeToUtf8Bytes` and raised a possible compact-one-line native
envelope conflict with the 4096-byte line cap, without opening the `Options` value
passed to that call. Root disconfirmed it: `Native.Options` sets `WriteIndented=true`.
Sweep: the encoder call, its options, actual encoded bytes and line-cap preflight.
Derive: a call-site name does not establish behavior when a passed configuration
changes it. Preventive always-loaded rule: follow every load-bearing argument to
its value and inspect the emitted artifact before raising a boundary finding. The
B0 contract gate must execute a positive encoded-envelope/line-cap case as well
as the long-scalar negative case; this mistaken compact-output finding is withdrawn.

**DATA-F · Outward DTO arrays alias session-owned state.** In the first B0
candidate, `Snapshot`, `CaptureRecovery` and `Envelope` returned nested mutable
arrays that a caller could alter without a command, generation or accepted
revision. Sweep: every public return and async save request carrying source,
draft, recovery or base64 chunks. Derive: the session retains authority and
returns defensive images at every boundary. Prevent: B0's named
`Draft_ExternalRetargetMutation_PreservesOwnedTarget`,
`RecoveryAndEnvelope_CallerMutation_PreservesOwnedBytes`,
`Snapshot_CallerMutation_DoesNotChangeSession` and
`SaveRequest_AsyncBoundary_CapturesDefensiveImage` checks. These are contract
fixture assertions; production core tests must repeat the class.

**DATA-F production recurrence:** the first `SourceParse` retained a caller's
mutable `IReadOnlyList<Diagnostic>` and exposed it while `IsParsed` read its
changing count; a public constructor could also manufacture apparent success.
Sweep: source bytes, diagnostics, session snapshots and every returned view
that could carry authority. Derive: parser construction stays internal, bytes
and diagnostic collections are copied into immutable outward images, and
success requires an internal parsed definition rather than an empty list
alone. Prevent: production `Source_OutwardMutation_PreservesAuthority`,
`Source_DiagnosticsMutation_Refused` and
`Source_PublicConstructor_CannotForgeSuccess` pass in the isolated core
checkpoint. These are API ownership controls, not geometry acceptance.

**DATA-G · Retry semantics are not reconstructable from durable facts.** The
first B0 candidate lost Open/Apply operation IDs or their target-sensitive
payload binding at Reopen, allowing duplicate or conflicting use of an ID.
Sweep: Open, rail Apply, Undo/Redo, no-op and branch replay, before and after
save/reopen. Derive: every persisted transaction retains exactly the facts
needed to compare its ID, action and immutable payload; a rail Apply carries
its edit receipt even when geometry is unchanged. Prevent: Ruling 10's schema
plus `Open_Retry_ExactlyOnce`, `Reopen_OpenRetry_DurableExactlyOnce`,
`Reopen_ApplyRetry_DurableExactlyOnce`, `Reopen_OperationKindReuse_Refused`,
`Reopen_ApplyTargetReuse_Refused`, `Reopen_ApplyDraftReuse_Refused` and
`NoOp_Reopen_VolatileIdExpired`. The final oracle passed; no preserved
historical pre-fix execution is claimed.

**DATA-E recurrence · Alternate admission entry skips its proof binding.**
The first B0 candidate guarded Apply but could Open or Reopen a source with an
unrelated or unavailable geometry authority after hash/schema checks. Sweep:
every way a source becomes accepted or active, including Open, Apply, Undo,
Redo and Reopen. Derive: byte integrity and semantic hash are necessary but do
not establish geometric admission; each entry verifies an opaque certificate
bound to the exact candidate/evaluator or preserves the prior state with Not
assessed. Prevent: `Open_UnrelatedCertificate_Refused`,
`Reopen_UnknownGeometry_Refused` and `Reopen_UnrelatedCertificate_Refused`
in the B0 contract fixture, with production analogues required.

**DATA-H · Dirty state compares a disk image with reserialized state.** A
valid native file may use different harmless JSON whitespace from the
writer's normalized envelope. Comparing raw disk SHA with a reserialized
current image falsely marks it dirty; acknowledging an older async save can
also falsely clear a newer revision. Sweep: raw file conflict token, normalized
session image, captured save request and post-save current state. Derive: keep
the exact on-disk hash for external-conflict detection, but compare normalized
session images for dirty state and acknowledge only the captured image.
Prevent: `Reopen_DifferentWhitespace_Clean`,
`Save_LateAcknowledgement_NewerRevisionRemainsDirty` and
`SaveRequest_AsyncBoundary_CapturesDefensiveImage`; a future native store must
exercise the same split with real files.

**EVID-C · A partial worker stream is mistaken for final execution evidence.**
Coordinator initially interpreted an early Grok stream prefix as containing no tool use;
the closed 117-row stream contained a completed read and successful `pwd`. Sweep: final
stream hash, event counts, tool IDs, terminal updates, exact commands and actual files.
Derive: inspect a closed complete receipt and distinguish narration, dispatched calls and
completed operations. Prevent: `tools/coord-stream-summary.py` hashes the complete stream,
correlates calls/updates and checks required successful commands. The complete receipt
passes its `pwd` requirement; the partial prefix and missing-build requirement fail.
No successful read/shell probe is promoted to write/build or containment qualification.

**COORD-REGEN · Shared ledger target mistaken for the invoking checkout.**
`coord regen` reads the shared primary registry and debt correctly, but its dispatcher
passed the primary path to the generator, changing primary `docs/audit/audit-data.js`
while leaving the linked author's derived file stale. Sweep: every coord command that
combines a shared registry/ledger root with checkout-local file effects. Derive: keep
`repo_root()` as the shared state root and pass `checkout_top(os.getcwd())` only to
the generator target; fail closed when the invoking checkout is unknown. Prevent:
`tools/verify-coord-regen-worktree.py` invokes the real CLI in a temporary primary and
linked Git worktree. Its reverted-dispatch RED case changes primary only; the fixed
GREEN case changes linked only and clears shared debt. The primary was restored and
both checkout statuses read back. No global `repo_root()` change is authorized by
this class.

**UI-N / TEST-A recurrence · A helper-level oracle bypasses the failing caller.**
The first regen regression called `cmd_regen(store, linked)` directly; that passed even
with the faulty dispatcher because the helper already honored its second argument.
Sweep: the public CLI entry point, dispatcher, helper and output tree for controls
whose stated failure arises at a boundary. Derive: inject or revert the actual faulty
call site and assert the wrong side effect before the fix, then invoke the same public
entry point after the fix. Prevent: the argument-free `verify-coord-regen-worktree.py`
is auto-discovered by `run-verify-gates.py`; it proves RED with only the dispatch line
reverted and GREEN in the current CLI. A direct-helper test alone cannot certify the
worktree routing behavior.

**TEST-A recurrence · A frozen consumer compiles against a different DLL than
the intended snapshot.** Root's first corrected viewport sampler project
resolved an older adapter assembly through MSBuild candidate-file probing
ahead of its stated `HintPath`; the missing `InteriorEta` compile error was a
review harness binding failure, not a product regression. The earlier
fixed-sampler RED still applied but was re-attributed to the actual frozen
controller DLL. Sweep: all isolated .NET consumers that copy app assemblies
or rely on `HintPath`. Derive: turn off default item discovery in the scratch
consumer and compare each runtime assembly SHA with a frozen manifest before
execution. Prevent: the always-read C contract requires that manifest for
independent binary review; root's corrected sampler retained the SHA-bound
assembly manifest and obtained the same-edit GREEN for all eight editable
targets. A passing consumer with only the top-level DLL fingerprint does not
close this class.

**PACK-I · Generated links are relative to the input root instead of their destination.**
Security/privacy rollups embedded under `docs/security/` contained `design/...` links,
which resolved below the wrong directory. Sweep: both rollup tables and the shared link
emitter. Derive: the scan root and output link base are distinct. Prevent:
`docs-graph.py rollup --relative-to docs/security` computes links for the destination;
the default docs-root behavior is retained. `tools/check-rollup-links.py`, wired into
`tools/check-docs.py`, was observed RED before the option existed and GREEN for default
and nested output directories, checking resolution to an actual source file.

**COORD-ENV · One-command environment assignment does not identify later mutations.**
Root scoped `AGENT_SESSION` to an audit command, then committed later in the same shell
without exporting it. The commit hook explicitly reported advisory/no identity; that
commit is not claimed as enforcing. Sweep: the eight committed paths were subsequently
checked with explicit identity and each returned structured `decision: allow`. Preventive
always-loaded rule: export the task identity in every mutating shell batch, or prefix
each Git mutation individually; inspect the hook result and never treat its advisory
exit zero as an ownership check. A post-commit check does not retroactively strengthen
the original commit-boundary evidence. B0's `cb73079e` commit repeated the
advisory/no-identity shape; Coordinator explicitly checked all twelve committed
paths under `cfd-contracts-author-20260923` afterward and observed twelve
`allow` decisions, again without a retrospective enforcement claim.
The R37 independent review checkpoint repeated the shape: its first commit
omitted `AGENT_SESSION`, so the hook was advisory. An attempted amend with the
identity set had no staged paths. Root then ran the supported ownership check
with explicit identity against all four committed paths and observed four
`allow` decisions. That readback bounds ownership but does not turn the first
commit into an enforced commit-boundary check. The same per-mutation identity
rule applies to review commits as to production commits.
The W1 independent review commit repeated this lapse: `AGENT_SESSION` was
omitted, so the hook was advisory. Root immediately checked all four committed
paths with the correct session while its claims were live; each returned
`allow`, then it released the claims. That is an ownership readback, not an
enforced original commit. A future review commit must prefix the actual
`git commit` process, not only the preceding audit or claim command.

Recurrence (M1.2a/M1.2b waves, 5 more): `coord-core.py precommit` prints
`"advisory: AGENT_SESSION is unset, so nothing was checked"` and exits 0 —
enforcing nothing — whenever the committing shell lacks that variable. Every
coordination brief already says "export `AGENT_SESSION` in EVERY shell call,
including `git commit`" — the same per-mutation identity rule recorded above —
and it still failed five more times across the M1.2a/M1.2b waves alone (U1a,
the Owner seat twice, G0 repair, the Coordinator once), on top of the five
occurrences already named here (Root, B0 `cb73079e`, R37, W1, R45 `fce9759`).
The prose mitigation is not a control: the committing shell is free to ignore
a sentence in a brief. **Control, proposed and not yet implemented** (pack
scripts under `docs/ai-forward-pack/scripts/` are vendored, not repo-owned —
an open "pack patch" item for the pack maintainers, not a local fix): change
`coord-core.py precommit` to FAIL (nonzero exit, blocking the commit) rather
than print an advisory line, whenever it is invoked inside a linked worktree
that a session has registered against (i.e., `coord worktree list`/the
session registry already knows this path) and `AGENT_SESSION` is unset in the
environment; it may remain advisory only in the primary checkout or an
unregistered tree. Until that patch lands, the only real control is the
Leader's post-hoc branch read (`git log`, `git status` against the track's
owned paths) — a readback, not a commit-boundary check.

**TOOL-PATCH · A replace operation is expressed as delete-plus-add in one patch.**
The first full parser patch asked `apply_patch` to delete and add the same
path in one transaction; the tool rejected it and changed no file. Sweep:
large rewrites within a leased file and any staged path after patch failure.
Derive: use one `Update File` operation (or bounded sequential updates), then
read back the intended path and status before compiling. Preventive
always-loaded procedure: on any patch error, check `git status --short` and
the file contents before retrying; never count a prepared patch as applied.
The parser's first build and tests were reported only after that readback.

**ENV-C · A declared build scratch is silently replaced by a host temp default.**
The first core gate used `tempfile.gettempdir()` at import, which resolved to
macOS `/var/folders/.../T` despite the approved task-specific `/tmp` plan.
Sweep: core gate and both joined .NET recount scripts; unrelated unique temp
directories without an exact-root promise are not mislabeled violations.
Derive: pass the scratch parent explicitly, make it unique per invocation,
resolve the macOS `/tmp`→`/private/tmp` alias, and assert the canonical parent
before launching a child. Prevent: `tools/verify-application-core.py` records
logical and canonical scratch, checks its parent, and records all six .NET
cache/temp paths and build outputs under that unique root on every normal run.
The Ruling 14 retry receipt
`/tmp/cfd-application-core-20260923-uj2qxazf/receipts/environment.json`
shows the corrected paths; its six observed owned PIDs ended quiescent. The
old task-named `/var/folders` scratch remains retained for review, not erased
as if it were unowned clutter.

**ENV-D · A skip-first-run switch does not disable every SDK first-run effect.**
The first .NET build printed a certificate-installation banner even with
`DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`; no new global certificate was proved,
and a matching PFX predates this session. Sweep: both joined .NET recount
scripts and the first-core verification gate. Derive: set Microsoft's
`DOTNET_GENERATE_ASPNET_CERTIFICATE=false` before invoking `dotnet`, retain
task-local CLI and NuGet caches, and inspect the actual command output rather
than inferring trust from a banner. Prevent: all three scripts now set that
variable; the Ruling 14 retry's environment receipt records it and its build
log contains no certificate/trust banner. No broad certificate clean or trust
change is a legitimate repair for this class.

**PROC-C · Agent interruption leaves an owned build child running.** The
built-in cancellation drill interrupted its active worker, but PID `70844`
survived until the Coordinator sent TERM to that exact observed PID and read
back absence. Sweep: every build/test runner and worktree handback. Derive:
track PID with start identity, stop dispatch on interruption, terminate only
verified owned children and read back quiescence; an interrupt API return is
not process proof. Prevent: the first-core gate writes process/environment
receipts and checks live descendants before success. Its post-launch observer
failure path reports descendant quiescence **Not assessed**, reaps the exact
`Popen` child, and stops the route. Windows fails closed before child launch
until a measured process-tree adapter exists. The [drill receipt](../coordination/application-cancel-drill.md)
retains the original survival/cleanup sequence.
The first Avalonia build is a sibling of this class: a package task launched
collector PID `22494` that outlived its failed compiler parent even with
`--disable-build-servers`. Sweep now includes dependency build tasks, not
only test runners. The pinned package documents process-local
`AVALONIA_TELEMETRY_OPTOUT=1`; the Ruling 24 measured retry recorded that
setting in the child environment and observed no collector in its sampled
process group, then verified the group empty. Prevent: the C gate keeps the
opt-out and exact PID/start/descendant sampling on every Avalonia build or
publish, and stops on an observed collector or override. Sampled absence is
not proof of zero prior network effect or a universal no-child guarantee.

**EVID-TZ · A local timestamp is given a UTC suffix.** A first read-only
`stat -t ...Z` printed the PFX's Pacific local clock while labeling it `Z`.
Sweep: evidence commands that attach `Z` to host-formatted timestamps.
Derive: set `TZ=UTC` on the command or retain an explicit numeric offset;
never add a UTC label after formatting. Preventive always-loaded command
pattern: `TZ=UTC stat -f '%Sm' -t '%Y-%m-%dT%H:%M:%SZ' <path>`. The corrected
readback was `2026-05-05T18:32:31Z`, before this task; the first mislabeled
output is not reused as UTC evidence.

**NUM-G · A secondary numeric parameter bypasses the token resource bound.**
The first production `DecimalSi.Parse(token, int decimalScale)` checked the
token's effective exponent, then added an arbitrary public `decimalScale`
before `BigInteger.Pow`; a small token with an extreme caller scale could
allocate without bound. Sweep: production `Identity.cs` and the B0 fixture's
same helper shape. Derive: grammar-supported length and area unit exponents
are a closed set `0, -2, -3, -4, -6`; reject any other scale **before** token
scanning or exponent construction. Prevent: production
`Decimal_UnsupportedScale_RefusesBeforeScaling` and extreme-int boundary tests
in `IdentityTests.cs`. The +1-scale case was observed failing before the guard
and passing after it; deliberately running `int.MaxValue` against the unsafe
version would defeat the resource control and is not claimed. The B0 source
remains a design fixture, not a production numeric API, and retains this
residual sibling pending any separately scoped spike maintenance.

**LEX-D · End-of-line anchor mistaken for full-token acceptance.** .NET regex
`$` can match before a final newline, so the first numeric helper could accept
`1\n` as one token. Sweep: production numeric lexer and B0 fixture token,
integer, hash and UUID regexes using terminal `$`; parser admission must
check complete spans rather than trust a prefix. Derive: use `\A...\z` or
check exact match length for a single token. Prevent: production
`Decimal_TrailingNewline_RefusesNonToken` was observed RED with `$` and GREEN with
`\z`; whole-source parser tests must retain the same byte-span boundary.
The B0 fixture's remaining `$` forms are recorded as design-only residuals,
not promoted to a production admission claim.

**LEX-E · A synthetic end marker is forgeable source text.** The first whole-
source parser used a token spelling `EOF` and `Expect("EOF")` without proving
the reader reached the actual final token index. A user identifier `EOF`
could end parsing early and hide trailing text. Sweep: foil, standalone
section and every alternate root production. Derive: success requires the
structural end sentinel **and** complete token consumption, never a word
equality alone. Prevent: production `Parse_FoilSpoofedEof_RejectsTail` and
`Parse_SectionSpoofedEof_RejectsTail` were observed RED before the index
check and GREEN afterward; the independent frozen-DLL consumer also checked
the spoofed-tail boundary.

**GRAMMAR-B · A shared helper admits its caller's forbidden supergrammar.**
`ReadProfile` accepted asset references when called by standalone section,
whose normative production permits profile body only. Sweep: shared profile,
lock, assertion and assignment readers at each root grammar entry. Derive:
pass the root production's allowed variant explicitly and reject a forbidden
variant in the syntactic phase, even if the shared helper can parse it for
another caller. Prevent: `Parse_StandaloneAsset_IsSyntaxError` was observed
RED then GREEN. `Parse_OneAssignment_ReportsSyntax` likewise makes a missing
required second assignment a syntax error; root independently reproduced
and then closed that named finding against frozen DLLs. The parser's full
language conformance remains a separate gate.

**DIAG-A · A generic parse failure erases actionable context.** The first
parser emitted null entity and generic reason/recovery for every failure.
Sweep: curve count/order, missing profile/asset, lock and assertion failures.
Derive: carry the failing curve or reference identity and the specific
required count/order/action into stable diagnostic fields while preserving
the original source span and phase. Prevent: production
`Diagnostic_CurveError_NamesCurveAndRequirement` and
`Diagnostic_MissingReference_NamesTarget` were observed RED before context
propagation and GREEN afterward. Uncovered diagnostic families remain
explicitly open; two named examples do not clear the full diagnostic contract.

**TEST-SPAN · A diagnostic oracle guesses the failure token.** An initial
missing-unit case expected the `evaluator` keyword. The parser consumed that
word as the prospective unit and correctly stopped at the following quoted
evaluator value. Sweep: mixed syntax/lexical cases where a missing token lets
the next token fill its slot. Derive the expected span by tracing the frozen
grammar and checking the exact source slice, rather than weakening the span
assertion after a mismatch. Prevent: `Ruling15_MissingUnit_PreventsOverflowBinding`
checks code, phase and the quoted token span; its first wrong-oracle receipt
`ac71hlaw` and corrected 63-case receipt `83ry315v` remain retained.

**CAP-SEAT · An optional review silently exceeds the agreed active-agent cap.**
During serial core work, root activated the Owner for an optional math review
while root, Coordinator and core author already occupied the three active
execution seats. Root stopped that review without a new writer or process.
Sweep: optional review activation, worker resume, and replacement after a
checkpoint. Derive: count actual active team members with `list_agents`
immediately before activation; schedule an optional reviewer only when a seat
is free, or explicitly replace a paused active seat. Prevent: the Coordinator
records the observed count and cap in the dispatch checkpoint, and refuses a
fourth active execution turn. The Owner review is deferred to a seat change;
its pending finding is not treated as already delivered or cleared.

**GRAPH-REG · Generic graph propagation crosses an exclusive register writer.**
`docs-graph.py flag --changed coordination-application-build` included the
inbound `rulings` artifact and added review-suggested frontmatter to
`docs/notes/rulings.md`, whose writer is `coord decide rule`. Sweep: V16 inbound
neighbors of coordination changes and other register-class targets. Derive:
metadata provenance does not override an exclusive register write path.
Prevent: the always-read coordination plan requires restoring only that
generated flag, deriving the index again, and checking the ruling-register
diff is empty before commit. The current flag was removed; no ruling prose
or numbered decision changed. A future graph-tool class-aware exclusion may
replace this local join control after separate review.

**ORACLE-X · A reviewer equates a physical station with its spline parameter.**
In the independent twist-collision calculation, I first used `eta=0.203125`
as the cubic span parameter and reported CV2 weight `9633/32768`. The authored
abscissa is nonuniform: its first span reaches that eta at local `u=1/2`, so
the correct weight is `3/8`. The initial angle bits were withdrawn before a
technical ruling; this was a reviewer-oracle error, not a product regression.
Sweep: section, twist, rail and placed-point oracles that evaluate a B-spline
at a physical x/eta. Derive: solve or prove `x(u)=requested position` before
using basis weights, then compare exact/interval outputs. Prevent: the
always-read B packet requires that inverse-abscissa check, and
root's retained `exact-oracle.py` plus nonlinear-x section consumer provide
the corrected named evidence. Matching a qualitative outcome from a wrong
parameter is not counted as a valid oracle.

**IDENT-CV · A derived-unit transform erases an authored distinction before identity.**
The `/1` twist hasher converted individual degree controls to rounded radians;
the adjacent binary64 degree values `1.791` and `1.7910000000000001`
collapsed to one Surface hash although degree-curve-then-convert evaluation
produced different placed coordinates. Sweep semantic hashes, cache keys and
certificate bindings that canonicalize a derived representation instead of
the inputs defining its evaluator. Derive identity from the exact defining
degree CVs under an explicit evaluator version; retain source SHA separately.
Prevent: FoilDSL DSL-19/20 and the B0 recount's four required Ruling 17 checks
exercise the collision and `/1` refusal. Production `/2` hash, native history
and no-adoption controls remain an open B gate; a green B0 fixture alone does
not close the class in the application.

**CERT-FEAS · A shape certificate omits deterministic query executability.**
The frozen `/1` evaluator certified a foil with constant `1e-300`-degree
twist, yet a subsequent `PointAt` exhausted its rational arithmetic cap and
returned Not assessed. Sweep certification preconditions against every public
finite-binary64 query path, inverse, trigonometric range reduction, rounding
and result conversion; a sampled grid cannot quantify the full domain.
Derive a whole-domain bound tied to actual operations and caps, or refuse
before certification. Prevent: Owner Ruling 18 is an always-read B admission
floor; a named tiny-angle counterexample plus an inspectable all-query bound
witness are required of the production B proof. This control is **open** until
the executable `/2` gate and independent science review pass.

**STORE-ALIAS · A lock key follows a path spelling rather than a directory entry.**
On the measured Mac volume, case-equivalent target names could acquire
different raw-spelling overwrite claims for the same file. Sweep case,
Unicode and parent-path aliases plus noncooperating-writer limits across
filesystem publication paths. Derive a single fixed reserved claim relative
to the held actual parent directory, with owned cleanup before final
directory durability and no CAS claim. Prevent: Ruling 19's fixed-claim
contract and production `Store_EquivalentCaseAlias_CannotAcquireSecondWriterClaim`
plus root's 23-assertion frozen store composition probe cover the observed
Mac recurrence. Windows and final integrated store acceptance remain open.

**CO-UI · A native adapter packet names UI outcomes but omits its review workflow.**
The first provisional C packet required keyboard, accessibility and token
proof, yet did not bind the author to `$implement` and the triggered
`$ui-design` review contract or its companion lenses. Sweep: native adapter
assignments that mention a prior mockup or design tokens as if those alone
review the running interface. Derive: a worker packet for a user-facing native
surface must name the implementation and UI review workflows, native harness
states, independent accessibility veto, and browser-only checks that are
inapplicable. Prevent: the always-read C launch packet carries those explicit
conditions; its independent pre-dispatch review checks them against the
actual compiled brief before any adapter lease is issued.
**CO-UI recurrence · A native window diagnostic leaves the supported UI
inspection surface.** After an Avalonia `Window.Opened` event and CUA
`cgWindowNotFound`, the author made one direct read-only CoreGraphics window
query (two onscreen same-bundle windows) and attempted a System Events count
(assistive access denied) before the CUA-only tool boundary was recalled. No
file/UI mutation or AX proof followed. Sweep: all native UI visibility,
accessibility, keyboard and screenshot observations; process state, stdout,
and application-internal lifecycle instrumentation remain distinct allowed
diagnostics. Derive: a window count or framework callback cannot certify the
user-visible or accessible surface, and a denied alternate inspection route
is not an invitation to retry. Prevent: the always-read C packet now names
`cua_repl` as the UI observation path, forbids direct external UI inspection
for worker proof, and reserves independent root CUA readback as the rendered
gate. The retained CoreGraphics result is diagnostic only, while CUA window
binding remains open.

**PLAT-A recurrence · Repository tools inherit host text defaults.** The integrated
pack gate found text writes without LF selection and printing CLIs without a UTF-8
console guard, including root's new rollup regression. Sweep: seven project scripts,
their text-read siblings, and the Coordinator-owned spike/recount scripts. Derive:
repository text is UTF-8/LF; console encoding must not depend on a Windows code page.
Prevent: explicit read/write encodings and LF writes, plus the pack's guarded stream
reconfiguration. `verify-portable-text-io.py --root .` reported ten findings before
root's fixes and passed as a standalone gate afterward. The Coordinator fixed
subprocess encoding and its newly joined scripts; the integrated gate then passed
10/10 checks on the joined branch. The initial root diagnostic batch continued after the failing command, so its
final shell exit is not claimed as the gate result; standalone checks preserve it.

**PLAT-A handoff recurrence · A new tool bypasses an applicable repository gate.**
The W0 author ran local documentation and receipt controls, but omitted the
existing portable-text gate. The first integrated conductor run then refused
`tools/qualify-windows-runtime.py`: four JSON writes had host-default newlines
and its printing entry point lacked the UTF-8 console guard. Root independently
reproduced all five findings. Ruling 42's one-file correction passed the
portable-text and subprocess-UTF8 gates, 35 existing refusal controls, a new
help-exit control, and the corrected integrated 11/11 gate. The frozen old
native receipt remains bound to the old source. Prevent: a tooling handoff
must attach actual `verify-portable-text-io.py` and
`verify-subprocess-utf8.py` exit/output receipts before its author declares
ready; `check-docs.py` is not their substitute. The W0 route packet carries
this pre-handoff rule.
*2026-10-09 (Ruling 183, GATE-AFTER-FREEZE).* The same shapes recurred in `tools/check-windows-runner.py`: two text
writes without `newline`, and a printing entry point without the stdio guard. This time the gate was not skipped; it
ran **after** the PC owner's exact-hash freeze. A green review therefore bound bytes the gates then refused, and the
repair cap fired on gate-shaped work. The W0 pre-handoff rule never reached the Ruling 182 runner route, and the Mac's
16:32Z gate list named the gates without their order.

*Control:* in any PC track that changes `tools/`, `run-verify-gates.py` and `check-docs.py` run green, with output
retained, **before** the self-test freeze and before any exact-hash review. The review receipt cites both outputs; a
reviewer refuses a hash review without them. The Mac states gate lists in order: gates, then freeze, then review.
Status: prose rule in Ruling 183 and the route text; the mechanical form (the receipt check requiring both outputs,
timestamped before the review) is pending.
*2026-10-09 (WIG join, GATE-BEFORE-ADD).* A third ordering shape: the gate ran before the freeze, but before `git add`.
- Track WIG ran check-docs green, then committed a new `tools/check-wsl-inline.py`.
- `check-proof-pii.py` reads tracked files only (`git ls-files`), so it never saw the new file. Its fixture, a WSL home path with a one-letter account,
  reads as a real account name.
- The leader's join (step 4) failed on it. The leader changed the fixture to `<name>` on top of the merge, and both gates
  then passed.

*Derive:* a track stages its files (`git add -A`) before its final gate run, so gates that read the index see new
files. *Control:* the briefs' common rule now says "stage, then gates, then commit"; mechanical form pending.
*Status: controlled (2026-10-09, track GBA).* The working-tree gates `check-proof-pii.py`, `check-wsl-inline.py` and
`check-notices.py` list files through `tools/gate_files.py` (`git ls-files` plus `--others --exclude-standard`), so a
new unstaged file is judged. Each `--self-test` plants an untracked offender in a temp repo. `check-merge-bindings.py`,
`check-capture-manifests.py` and `check-foildsl-spec-sync.py` judge committed blobs by design and keep HEAD. Proof:
`docs/proof/gba/red-first.md`.

**READINESS-UNLOCKED · Readiness measures frame budgets while a track's ring runs.**
On 2026-10-09 readiness for the PR #24 join was RED: `Readiness_OrbitFrameP95Under33Ms` measured 45.26 ms against 33 ms at
load 16.99. Its load gate is 24, so the measurement counted. Track CBS was running `tools/run-tests.sh` at the time. A
re-run with no track running gave 16.30 ms (GREEN). `tools/run-suite.sh` and the rings take `tools/ring-lock.sh`, but
`tools/run-readiness.py` does not (no `ring-lock` reference), so the leader's readiness and a track's ring share the CPU.

**Class → sweep → derive → prevent:**
- *Signature:* a performance gate run without the lock that serializes the other heavy runs on the host.
- *Sweep:* the readiness frame and cost gates (`Readiness_*`, C-2/C-3/C-5 cost caps). Track rings take the lock. The
  load gate (24 on 16 cores) admits a load from one concurrent ring.
- *Derive:* readiness holds the ring lock exclusively while its Desktop and Analysis parts run; a track ring waits.
- *Prevent:* `tools/run-readiness.py` takes every slot of `tools/ring-lock.sh` (exclusive mode, `--acquire-all`) for the
  whole ring, after announcing it in `exclusive-wanted` so new track rings stop taking freed slots. It prints the holders
  (pid and command) and the measured wait, records `ringWaitSeconds` in the receipt, and past 600 s
  (`CFD_READINESS_LOCK_WAIT_SECONDS`; two holders at 60 s ring or 120 s suite budgets, so 600 s means a hung holder) exits 4,
  `BLOCKED (ring busy)`, with no receipt. Status: controlled (`python3 tools/run-readiness.py --self-test`, case group
  `ring_lock_cases`, plus `tools/ring-lock.sh --self-test` cases 6-8; ring: on demand, not yet in check-docs, about 25 s). Proof: `docs/proof/rlk/`.
  Residual: the lock is a bash script, so Windows readiness runs unlocked; the PC runs no concurrent track rings.

*2026-10-09 recurrence, Windows runner preparation:* the runner's reviewed checker
omitted LF settings on two temporary AST writes and the printing-entry console
guard. The existing text-mode-hash and portable-text gates rejected it. Sweep:
all text writes and the main entry in that checker; only those three omissions.
Derive: portable temporary text and console output are required even when policy
and native runtime fixtures pass. Prevent: the existing gates remain required,
without an allowlist; the R183 one-file correction retained original red captures,
then direct portable, configured verify, docs, and expanded qualification exit-0
captures before independent owner PASS (`docs/proof/wri-r182-runner/receipt.md`).

**TEST-ENTRY · A test intercepts process exit and runs unintended work.** A
local legacy-console probe replaced `sys.exit` while invoking `--help` on the
W0 qualifier. Argparse's intended exit was suppressed, so `main` continued
into a task-local macOS build/refusal. That run exited with 26 Windows-native
cases Not assessed and did not dispatch or qualify Windows. The fixed control
executes the CLI in a bounded child, requires exit 0 and help output, and
rejects qualification output. Future entry-point tests must preserve normal
exit semantics or isolate the process; they must not monkey-patch `sys.exit`
then infer `--help` was harmless. The unintended run and correction are
retained in the R42 author audit.

**TEST-FIXTURE-ALIAS · A shared negative fixture hides a missing identity
guard.** In the R41 visible-presentation synthetic assessor, the first
envelope-identity mutant survived: the fixture reused one mutable identity
object for the envelope and frame records, so changing the envelope also
changed every frame. A separate frame guard then rejected the receipt, hiding
the removed envelope guard. The author changed each frame identity to an
independent deep copy, added four frame-only mismatch cases, and reran the
same four wrong-result mutants. Final synthetic controls were 50/50 and all
four mutants exited 1, including the formerly surviving envelope mutant.
Prevent: a validator with multiple independently required identity surfaces
must construct independent fixture values and mutate each surface alone.
The permanent R41 controls exercise envelope and frame mismatch separately;
future single-object fixture reuse must fail a boundary-specific mutant.
This is synthetic oracle evidence only; native capture and visible timing
remain Not assessed.

**API-ACCESS · Generated SDK documentation is mistaken for a callable client API.**
The installed Avalonia 11.3.14 XML documented `TopLevel.Renderer` and
`IRendererWithCompositor.Compositor`, but the exact Desktop compile rejected the
route (`CS0103`/`CS0122`): the documented types were not accessible to this
application. Sweep the proposed rendering chain through the installed public
signatures and an actual consumer compile before basing a timing contract on it.
Derive: XML presence proves documentation, not public accessibility. Prevent:
the always-loaded check-before-use rule requires an actual Desktop consumer
build against the adopted public route before relying on an SDK symbol. The
retained R29 internal-route compile RED and public
`ElementComposition.GetElementVisual(viewport)?.Compositor` compile GREEN show
that control firing once; those historical receipts are not a future gate by
themselves. No generated-XML claim alone clears the native render endpoint or
presentation gate.

**API-TYPE · A reflected SDK property is assigned an inferred runtime type.**
The R34 focus-placement test read public Avalonia composition properties by
reflection and demanded `System.Numerics.Vector2` for `Size`. The installed
Avalonia 11.3.14 getter returns `Avalonia.Vector`; the test rejected all four
tab-focus rows before checking placement. The same wrong-family assumption
affected `AnchorPoint`, `Offset`, `Scale` and `CenterPoint`. Sweep every public
property used by a reflected SDK probe for its exact installed getter type,
including reviewer assertions. Derive: successful compilation of a generic
reflection helper does not establish the runtime property contract. Prevent:
use direct typed public getters in the Desktop consumer; retain reflection
only for the two nonpublic adorner-link fields with exact type/version refusal.
The R35 compiler-negative deliberately assigning public `Size` to
`System.Numerics.Vector2` must fail with `CS0029`, while the restored typed
consumer must compile. The author and independent reviewer both missed the
type before R34; the compiler control applies to both paths.

**EVID-RENDER · A prior render marker can impersonate completion of new work.**
The first runtime spike compared the frame object by reference without resetting
a render epoch. A previous draw of the same accepted frame could satisfy that
comparison even if the requested Refresh had not drawn. Sweep every timing
endpoint for a fresh event serial, the exact target revision/frame, source-bound
operation and draft generation, and for a later state change before completion.
Derive: identity equality is necessary but not evidence that work happened
*after* the operation began. Prevent: `NativeRenderCorrelation.Fresh` requires a
strictly greater render serial plus matching revision/frame;
`NativeRenderCorrelation.SameState` separately checks draft generation. The
targeted negative checks reject an unchanged serial, a wrong revision, a prior
frame and a stale draft generation. The native-app control observed a new edit metric,
an injected delayed Preview, Cancel, and a superseded Preview; a separate
timeout control retained `not_assessed` after late publication. These are
batch-cycle diagnostics, not display-presentation proof. Final C source and
native UI review remain open.

**EVID-ENDPOINT · A framework callback is promoted beyond what it observes.**
Avalonia's compositor `Rendered` callback can complete after a render cycle
that returned early because the render interface was not ready, or after a
caught rendering failure. Sweep the start and stop of each cold, edit, Preview
and Cancel metric through interim status, target Render invocation, compositor
batch and actual display. Derive: a successful batch callback does not prove a
successful draw or pixels presented to the user; CUA call duration is not an
application metric either. Prevent: the R29 normal-path metric names its
endpoint `fresh_target_batch_cycle_not_presentation`, reports timeout and
supersession as `not_assessed`, and omits source/hash fields from emitted facts.
The targeted serialization check rejects source/hash keys after an early
candidate emitted a source-hash prefix. Root independently verified the
known-delay and timeout controls, while actual visible 5-second/100 ms/250 ms
acceptance stays Not assessed pending a supported presentation measurement.

## FoilDSL boundary sweep — 2026-09-22

**PACK-H · Additive hook refresh duplicates a logical callback.** Revision 92 changed the
managed Python launcher, so command-string union retained both old and new callbacks.
Sweep: every Claude lifecycle event and matcher, including read/prompt/session hooks.
Derive: callback identity is event + matcher + managed script, not launcher bytes.
Prevent: `tools/check-pack-hooks.py`, invoked by `tools/check-docs.py`, was observed RED
on four duplicate targets before removing the four obsolete entries. It refuses an
empty inspected set. Project-owned hooks and settings remain untouched. Pack-source
upstream repair is separate from this consumer-side control.

**GEO-R · An independently presented control has a coupled authority.** User correction: moving
LE carried TE because the record stored LE plus chord. Sweep: both pointer maps, keyboard/numeric
editors, cage, seeds, source grammar/identity/examples, preview/history, station/area/analysis readers.
Derive: author the two absolute rails independently; chord is TE−LE. Per-index compensation is invalid
when the rails' abscissae/knots differ. Prevent: `tools/check-independent-edges.mjs` (observed RED at
1.602926 mm unwanted TE motion) asserts opposite controls, 401 samples and rendered path unchanged
for unequal six/nine-CV bases, both-coordinate pointer moves, keyboard/source/numeric editing and
history. It also rejects positive-ordinate TE curves that cross LE. Native/full geometry certification
remains separate. Rename sweeps must distinguish authored channel identifiers from physical units
and brief targets: the unchanged source/CAD regressions caught a chord-target rename and sorted
test-identifier mismatch before handoff; the v5 oracle remains an archived-contract regression.

**DATA-D · A projection omits part of its source.** Class: a fitted spline is carried as points
while its reader uses a stale knot vector. Sweep: upper/lower fitting, section editor, source
serialization, skin, cage, residual and undo snapshots. Derive: every spline record carries degree,
points and its own knots; every reader consumes that record. Prevent: `tools/check-foildsl.mjs`
asserts knot cardinality, source/record round-trip and a profile edit reaching the 3D reader;
the preserved CAD oracle recomputes residuals from the same curve operands. Archived v5 is unchanged.

**DATA-E · One authoring route bypasses acceptance.** Class: visual Apply could accept negative
chord that the text validator rejects; re-seeding could emit near-endpoint floats outside the exact
language contract. Sweep: master/section Apply, source Apply, recipe seeds, station edits and locks.
Derive: shared pre-acceptance validation, one draft, and explicit normalization only at recipe conversion.
Prevent: the source oracle rejects negative visual Apply without changing revision, rejects competing
writers, round-trips both recipe generators, and checks malformed/version/reference/resource boundaries.

**EVID-A/UI-M recurrence:** read-only preview erased residual evidence, and undo restored geometry
without that evidence. Sweep: preview save/restore and both history directions. Derive: restore complete
accepted state, including evidence, or say not recorded. Prevent: the source-preview preservation
assertion and existing CAD residual oracle both run on v6. **UI-L/UI-H2 recurrence:** source preview
microtext and source chrome consumed the reflow editor; fifteen layout/theme cells now assert readable
editor height, target/contrast floors and no window scroll, with independent screenshot inspection.

**SPEC-A recurrence:** reused acceptance IDs made cross-specification evidence ambiguous. Product
criteria now use SRC and language criteria DSL; rendered-spec checks count actual source IDs and
blocks. **NG protocol correction:** an assumed pack graph node did not exist locally; graph inventory
rejected the dangling link before derivation. The control is the existing whole-graph validator,
not an invented local knowledge node. Measured normalizations and bounded prototype limitations
remain disclosed in the review hub; no full-language or scientific conformance claim follows.

**NG-LOCAL recurrence · A local path or subcommand is constructed before inventory.**
During the R17 companion handoff, three read-only attempts named absent
`verify-application-contracts.py`, `specify/reference/flow.md`, and
`coord leader show`; each failed before a write. The installed inventory instead
contains `recount-application-contracts.py`, the inline `specify` flow, and
`coord leader who`. Sweep unknown local files, skill references and custom CLI
subcommands in coordination packets. Derive: first inventory with `rg --files`
or read the actual skill/script dispatcher, then call the discovered path or
advertised subcommand. A guessed `--help` on a custom script is not presumed
read-only until its dispatch is inspected. Prevent: the always-read R17
companion packet requires this inventory gate before its recount/render/mockup
commands; dependent reads are sequential after inventory, not batched with it.

**CO-LEASE · A source-review lease is reclaimed before the downstream merge ends.**
The R17 companion was accepted and joined into coordination, but the root
reviewer reclaimed `docs/reviews/application-core.md` while that same committed
file still had to merge into the clean core worker tree. The core commit hook
refused the staged merge. Root released only that path, and the author resumed
without a bypass. Sweep multi-tree handoffs where a reviewed source branch has
more than one destination, including generated index and audit joins. Derive:
lease lifetime follows the last downstream integration fence, not the source
author's commit or the first join. Prevent: the always-read companion packet
requires the coordinator to name every destination, check `coord check` before
each destination commit, and notify the reviewer to reclaim only after the
final clean destination HEAD and conductor gate receipt. The refusal remains a
valid control observation, not a product failure.

**CO-LEASE recurrence · A pre-join inbound inventory misses a post-join neighbor.**
During the M1 scope V16 sweep, the Coordinator claimed the inbound file list
computed before joining the attachment investigation. The official `flag`
operation then wrote that newly joined investigation's frontmatter without an
advance exact-file claim. No conflicting lease existed; the Coordinator noticed
the changed path, claimed it, and re-enumerated the full inbound set before the
next mutation. Derive: a graph inventory is valid only for the current joined
tree, not for its pre-merge parent. Prevent: the always-read coordination plan
requires a fresh `docs-graph.py` inbound inventory and exact `coord check`/
claims **after every join** and immediately before a flag sweep. Stop before
the first mutation if any new neighbor is unclaimed; restore only the generated
register frontmatter delta when `rulings` is inbound.

**CO-EXIT · A dependent join step runs after its prerequisite fails.** The core
handoff initially batched `git commit --no-edit` and conductor continuation;
the commit was refused by the live review lease, but the conductor still ran
recounts and appended an audit entry before its own commit refusal. No hook was
bypassed; the author retained both raw attempts and checked exact child-process
quiescence. Sweep merge, audit, recount, release and launch command groups where
a later mutation depends on a prior exit and state. Derive: a successful probe
or recount does not imply the merge commit exists. Prevent: the companion packet
requires separate tool boundaries and explicit exit, `HEAD`, `MERGE_HEAD`,
staged-path and owned-child readback before continuation; an interrupted join
cannot be labelled complete from its partial gate output.

**CO-ARTIFACT · A task-local package cache is mistaken for task-local build outputs.**
The first C `dotnet run` calls bound NuGet, CLI home and TMPDIR to unique
scratch, but default MSBuild `bin/obj` still appeared in five projects under
the isolated source tree. Ruling 21's path-drift stop fired; the 150 files
were inventoried and preserved. Sweep every build, restore, publish and test
entry point, including transitive project references and implicit rebuilds.
Derive: environment cache variables do not relocate MSBuild output or
intermediate paths. Prevent: the always-read C packet requires the accepted
`--artifacts-path` per-project layout, all six local cache/temp roots and a
gate assertion that no new source-tree `bin/obj` or outside-root assets appear.
The one Ruling 23 corrected build produced four distinct project outputs under
fresh task scratch while preserving the first files; the full adapter gate
must make this recurrence control executable before C handback.

**NG-LOCAL recurrence:** this C worker first attempted an absent `coord.py`
path before using installed `coord-core.py`; a Coordinator read-only command
also used a shell glob for nonexistent `*log` filenames and failed before
inspection. Root had likewise guessed a nonexisting ADR path before `rg`
inventory. During this Ruling 24 handoff, Coordinator also guessed unsupported
`claim list` and `precommit --json` forms before reading the advertised CLI;
both failed before writes. The existing inventory-first control applies:
list exact paths or inspect script dispatch before constructing a command,
and read returned receipt paths rather than guessing suffixes. The C launch
brief now names `coord-core.py` and exact receipt paths; failed guesses remain
recorded so a future gate cannot call them verified.
At final C handoff, the author also used `for path in ...` in zsh: lowercase
`path` is tied to `PATH`, so the loop body could not find `python3`. No repo or
external write occurred, and the author reran with `artifact_file`. Sweep
task-local shell variable names used around executable lookup. The always-read
CT26/no-guessing control now names this tied-variable hazard: use a task-specific
name, then check the executable resolves before a mutating loop.
During visible-presentation source review, a web text rendering of pinned
Avalonia source omitted blank lines and made the comment/initializer appear
four lines earlier than the immutable file. The Reviewer sent a correction,
then `nl -ba` on the pinned downloaded bytes confirmed the author's original
line references. The source-content finding was valid, but the line-number
objection was wrong. For exact line citations, inspect the pinned raw bytes
with a physical line-number tool; a rendered web view's line positions are
not the file's line numbers. This is a reviewer NG-local correction, not an
author defect.

**CO-DECISION-VIS · A new ruling is assumed visible in an older isolated worktree.**
Ruling 23 was recorded after the C worktree fork, so that tree's local
`docs/notes/rulings.md` did not yet contain it. The Coordinator detected this
before the worker's corrected attempt and supplied the canonical Coordinator
tree path read-only. Sweep every post-fork ruling and contract amendment before
asking a paused worker to resume. Prevent: the resume packet explicitly names
the ruling's current absolute path and verified request ID, or joins the
reviewed decision before source work; a local stale copy cannot silently act
as current authority.

**CO-LEADER · A live review wait outlasts the conductor designation.**
While R36–R37 review continued, the Coordinator missed the prescribed renewal
interval; epoch 10 expired before a later `leader renew`. No join ran during
the lapse. Sweep long review and agent waits for elapsed designation time,
not just the next planned merge. Derive: a lease held at the start of a wait
does not remain authority at its end. Prevent: read `leader who` and renew
within its 100-second cadence while work continues. The conductor's epoch
fence rejects unread or lower epochs but does **not** establish a live
designation for an expired same-epoch invocation. The project precondition
is a supported `leader renew` followed immediately by `leader who --json`
showing the expected holder, live state and epoch before every conductor
mutation; preserve that readback. After the quiet period, the
supported `leader reclaim` established epoch 11 before shared work resumed.
An expired designation is never silently renewed or treated as a clean join.
Recurrence during the Part1/M1 decision join: epoch 14 expired while the
Coordinator waited for shared documentation leases. Renewal refused; the
supported quiet-period reclaim established epoch 15. No join or mutation ran
under the lapse. The Coordinator then released epoch 15 at the genuine wait
boundary. During the R42 independent-review join, epoch 17 again expired.
The same-epoch conductor invocation passed its narrow epoch fence and reached
merge step 1; this disproved the earlier claim that that fence rejects an
expired same-epoch join. No competing leader was observed. The Coordinator
stopped, used supported quiet-period reclaim to epoch 18, and reran the
affected join under a live readback. This is a missing project-level
live-leadership precondition, not a managed conductor code deviation. A
fail-closed project preflight is implemented under Ruling 46 in
`tools/run-owned-conductor.py`. Its embedded self-test first reproduces an
epoch-only expired join, then requires zero conductor invocations for expired,
released, absent, unknown or contradictory leader/context observations.
Positive live and post-conflict continuation fixtures preserve the child's
nonzero exit and bind the checked session/epoch. The coordination plan wires
future application joins through the wrapper after independent acceptance;
until that acceptance, the explicit renew/readback remains mandatory.
This closes an invocation-control omission, not a claimed managed-tool defect.
It remains an observed preflight, not an atomic leadership lock.

R46's first implementation run also exposed a local-contract error:
`WindowsJob` requires a process argument and constructs/assigns the job in its
constructor; it has no separate `assign` method. The source signature was
re-read, the caller corrected, and the same complete self-test passed. This
is the existing NG-LOCAL class: read the concrete installed signature before
using it. The runtime fixture prevents the zero-argument construction from
silently returning; Windows execution of the reused primitive remains unverified
in this macOS-only wrapper proof.

R45 author commit `fce9759` repeated COORD-ENV: missing `AGENT_SESSION` made
the commit hook advisory. Supported explicit checks subsequently returned
allow for its five owned paths. That bounds ownership after the fact; it does
not turn the original hook into enforcement. All wrapper launches bind the
environment identity, and all commit shells must still prefix Git mutations.

**TEST-CLI-RECEIPT · Helper-only input tests miss the actual refusal surface.**
R46's helper tests rejected nonfinite budgets, but the real CLI wrote a nonstandard
`NaN` intent and failed before its final refusal event. Root and independent
Owner reproduced exit 12 with no authority or conductor activity and an incomplete
receipt. Sweep: inspect both budget fields, positive/negative infinity, argparse
negative-value handling, both event serializers and receipt reuse. Derive: a
helper rejection is not input-to-evidence integrity. Prevent: R49 real-CLI fixtures
for all six inputs strictly decode every event, require a complete stable refusal,
prove zero leader/conductor effects and unchanged bytes on reuse. A direct strict
serializer injection and normalized duplicate-option test cover the seams the
repair changes. Invalid numbers are explicit diagnostic strings, never plausible
replacement budgets. These controls prevent malformed refusal evidence; they do
not grant live-leader or product acceptance.

**CI-ACTIONS-CONTEXT · A syntactically valid workflow uses a context where it
is unavailable.** Ruling 43's disposable Windows workflow placed three
`runner.temp` references in job-level `env`. Local checks and author,
reviewer and Owner pre-push review all missed the context-location rule.
GitHub rejected run `36097138344` before any job or artifact. The official
`actionlint` v1.7.12 task-local binary failed the frozen workflow with the
same three errors. Ruling 44 moved exactly those variables into their
consuming step environments; the same binary exited 0, and run
`36097839626` entered an actual Windows job. The prevention rule for future
workflow launches is an executable semantic `actionlint` check on exact
candidate bytes before push, in addition to YAML parsing and manual review.
This control catches context availability at the workflow key, while the
remote run remains the runtime oracle; a green lint cannot qualify native
Windows behavior.

**PACK-UIKB · A triggered skill links to a knowledge directory absent from the consuming checkout.**
The installed `ui-design` UI-T4 text references
`docs/knowledge/native-client-ui-design/`, but this project checkout does not
contain that directory; the installed native proof template and XAML token
linter do exist. Sweep triggered skill references before C dispatch, not after
UI authoring. Derive: an absent referenced path is a deployment-link gap, not
evidence that the native design standard is satisfied or absent. Prevent: the
always-read C packet requires path inventory at UI-T4 preflight, the installed
template/linter and named native proof rows, with the authoritative pack-source
knowledge read only where available. Pack deployment reconciliation remains a
separate exact-source change; no speculative local KB is created in C.

**UI-XAML · A typed XAML resource compiles but fails when the native window loads.**
The adapter gate built, tested and published the Desktop package, but the first
real app launch threw `InvalidCastException` at `MainWindow.axaml`'s section
`RowDefinition.Height`: its `SectionPanelHeight` resource was `x:Double`, while
the property requires `GridLength`. Sweep the Desktop XAML's static-resource
assignments to typed properties, including window dimensions and viewport
dimensions; the section row was the mismatched shape. Derive: source lint and
assembly compilation do not prove XAML resource conversion at window creation.
Prevent: `tools/verify-application-adapters.py` now runs the real Desktop
apphost's `native-xaml-startup-smoke` before publish and requires its
`NATIVE-STARTUP smoke-opened` marker plus exit zero. The resource is now a
`GridLength`. The original package launch is retained as RED at
`/private/tmp/cfd-c-final-ui-31t13mk0/launch-receipt.json`; the targeted
startup receipt is `/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-c-targeted-pronye03/receipts/targeted.json`.
This control proves loader startup only; rendered UI, AX and keyboard behavior
still require native inspection.

**UI-LIFETIME · A queued window callback reads a disposed document.**
In the same-process contrast fixture, closing the first native window disposed
its controller while a previously queued `Changed` callback still called
`MainWindow.Refresh`. The full stack reached `HasRecovery` and
`AuthoringSession.Snapshot`, which correctly refused `DOC-CLOSED`; the second
window remained open. Sweep every posted UI callback and asynchronous
continuation that can outlive its owning window, including a cancelled close.
Derive: unsubscription prevents future notifications but does not retract an
already queued callback. Prevent: the actual `Closed` path marks the window
closed before unsubscribing and disposing, and posted callbacks recheck that
state before reading the controller. The R37 two-window regression drains the
old callback, requires the next window to update, and verifies a dirty-close
Cancel leaves its draft live for another numeric change. Core `DOC-CLOSED`
continues to reject reads after disposal; the UI guard does not weaken it.

*Recurrence 2026-10-02 — the controller's own queued callbacks.* The
`--shell-window` check `Focus_UndoFromCanvas_StaysOnCanvas` failed with
`DOC-CLOSED` under load (4 of 12 loaded Debug suite runs). The stack showed
the dispatcher running `WorkbenchController.Notify` directly:
the previous check's 250 ms surface timer fired on a pool thread, queued
`OnUiThread(Notify)`, and the check then disposed its controller. The queued
`Notify` ran in the next check's `Settle`. It raised `SelectionChanged` into the
old `ShellHost`, which read `Planform` → `Snapshot` on the closed session. The
first sweep covered the window's posts, not the controller's. Siblings with the
same shape: `CompleteDirectCommandAsync`'s `finally { Notify(); }` and the
`RefreshAcceptedAsync` catch path. Prevent: `Notify` returns once the controller
is disposed (one choke point for every view notification), proven red-first by
`Controller_DisposedWithQueuedSurfaceNotify_NotifiesNoView` (`--views`); after
the fix, 0 of 46 loaded runs failed. The check harness now prints a
`STACK <check>` line on every failure, so a job queued by an earlier check is
visible in the log. Still open, and owned elsewhere: `Adopt` raises
`SelectionChanged` outside `Notify`, so an open that completes after `Dispose`
still notifies and adopts a live session. View-side posts (`ShellHost`
`RefreshOnUiThread`, `ModelArea.OnControllerChanged`) queued before `Dispose`
still read the controller.

*Closed 2026-10-02 — an open that completes after `Dispose`.* `Adopt` is the one
choke point for the four adopt sites (`OpenAsync`, `CommitPreparedFoil` for New
and `.foil`, `OpenFoilAsync`, `AcceptCandidateAsync`). On a disposed controller
it now disposes the late session, makes no store, raises nothing, and returns
false; the two outcome-returning sites map that to `Superseded`. A native read
that the closed store refuses (`DOC-CLOSED`) is also `Superseded`, not `Failed`.
A generation bump in `Dispose` alone was not enough, because
`CompleteGestureBeforeDocumentActionAsync` awaits before the generation is taken.
Proven red-first by `Controller_OpenCompletesAfterDispose_SupersededNoEventsNoLeak`
(`--views`): gated `IProjectStore.ReadAsync`, Dispose, release — once with a
read that finishes (was `Opened`) and once with a read the closed store refuses
(was `Failed`). The view-side posts named above remain open.

**UI-REVIEW-WINDOW · Review launches accumulate native windows after their proof ends.**
Serial R29/R37 review checkpoints left six exact receipt-bound old app processes
alive while the newest Dark window was under inspection; an earlier first app
had no exact ownership receipt. The user observed too many windows. Sweep every
native review launch and review-close boundary, including a CUA reattachment
that can auto-launch a previously closed bundle. Derive: a build's owned-child
quiescence does not cover separately launched GUI review instances, and a
matching bundle name alone does not authorize cleanup. Prevent: the
[review launch guard](../coordination/review-launch-guard.py) snapshots live
app PID/start/executable once, binds each to a retained exact launch receipt,
and refuses another launch unless zero apps remain or one current review is
explicitly allowlisted by receipt. Unknown or reused PID identities are
preserved and block, never killed. The launch helper invokes this guard before
copying or starting an app and records its hash and preflight state. The
simulated older-owned/allowed-current/reused-PID controls and real live-HC
refusal were observed; after each CUA close, inspect process inventory rather
than reattaching merely to verify exit, since reattachment can launch a new
window. Close only exact-owned windows through the UI after checking draft and
unsaved state; the first app was left to the user to close.

**UI-THEME-ORACLE · Token arithmetic hides runtime resource precedence and applied paint.**
The frozen Dark workbench rendered pale panels with near-white labels, and the
HighContrast window retained a light/teal palette. The verifier had combined
base and variant brush maps as if the variant won, while Avalonia resolves a
root `ResourceDictionary` key before its theme dictionary. Sweep resource
shadowing, actual template text and painted backing, focus indicators, selected
items and app-owned dialogs in every declared theme. Derive: palette values do
not prove the color pair that a control paints. Prevent: the adapter gate loads
the real XAML, rejects root keys shadowing theme keys, requires the complete
declared theme/control/state matrix, and fails closed on missing rows, unknown
opacity, backing or nonfinite contrast. The root-shadow mutation and native
Dark/HighContrast review are retained RED controls. The applied 72-row matrix
remains open until its actual receipt passes; resource-only GREEN is not a
contrast verdict.

**UI-INTERACTION-STATE-ORACLE · A resting-state contrast matrix misses a pointer state.**
The R37 72-row applied-control matrix passed its declared Light, Dark, High
Contrast and Default rows, yet root's actual High Contrast window twice hid
the selected FoilDSL tab label black-on-black during pointer hover; F6 removed
hover and restored the white label. Sweep the installed template's meaningful
enabled pointer-over, pressed, selected and focus combinations for both tab
headers, then the narrow custom-brush toolbar/modal button and navigator-list
siblings. Derive: a complete set of controls and themes is still incomplete if
the tested **state** set omits a template override. Prevent under Ruling 38:
freeze expected `(theme, control, state)` identities before execution; require
actual loaded-control foreground/painter/backdrop and 4.5:1 text or 3:1
applicable focus ratios for every declared row; fail on missing, duplicate,
wrong-state or nonfinite evidence; retain a negative with the state override
removed or the original failing source. A synthetic pseudo-class test is
styled-state evidence only; root's supported native pointer review is the
behavioral gate. This control is **pending** until the bounded author matrix
and rebuilt native review pass; no prior 72-row result clears it.

**TEST-CAPABILITY · A positional UI fixture selects a locked domain object.**
The R32 contrast fixture selected control-list index 1 and expected an editable
numeric draft. The Example's leading `cv-1` is locked by `root_mirror`, so the
product correctly refused `BeginEdit`; one fixture prerequisite concealed 70
downstream rows. Sweep test and review fixtures that equate list position with
domain editability or accepted identity. Derive: choose a target from the
authored `Editable` fact, then assert the UI item names that same rail/control
and the draft belongs to it. Prevent: the R33 matrix fixture selects by
authored editable identity, asserts the bound item and owned enabled field, and
retains a locked-target negative. Its complete-matrix receipt counts every
failed or unexecuted required row, so a prerequisite cannot manufacture a
partial PASS.

## Authoring decisions boundary sweep — 2026-09-22

**GEO-C / DATA-D recurrence:** introducing multiple profiles makes a selected-profile singleton unsafe as a
whole-wing geometry source. Sweep: profile bank, assignments, equal-x span blend, 2D inspection, 3D skin/cage,
source emit/parse, Apply/Cancel, undo/redo and retained alternatives. Derive: geometry reads assignments;
selection reads geometry. Prevent: `check-authoring-v7.mjs` requires exact geometry/source invariance while
selecting profiles, local blend reach for a fork, full-thickness reconstruction, unequal parameterization,
bank history, held t/c and explicit constrained source-thickness targets. Its missing-entry oracle was
observed RED on v6 before v7 authoring.

**UI-H2 / UI-M recurrence:** contextual controls reduced the available section canvas to a scaled 13.8 px
SVG at the reflow preset, shrinking its 24 CV targets to 2.1 px. Sweep: entry, section and dialog geometry at
the inherited viewport/theme cells. Derive: a precision editor scrolls internally at its minimum working
size rather than scaling targets below the floor. Prevent: the authoring oracle measures section targets,
contrast and shell overflow, including 640×400; the initial failure was observed before the CSS correction.
Identity/policy labels also retained old catalog text after a profile fork. The same oracle checks inspected
profile identity; all section/Properties/source projections now name the selected record and owned draft.

**UI-M recurrence:** focus restoration after picking a different curve reselected the old focused vertex.
Sweep: pointer picking, options selection and keyboard edits during an owned draft. Derive: transfer focus
before replacing the old focused subtree. Prevent: the v7 CAD oracle requires selecting elevation/twist
while retaining an unchanged TE draft and refuses a competing nudge. Native button/select Return must not
be intercepted as a global section Apply; contextual actions retain their own keyboard semantics.

**DATA-D recurrence:** a coarse equal-x table interpolated requested display points near a sharp leading
edge, creating an observed ~0.0068 chord error for a stress profile. Derive: evaluate the inverse-x curve at
each requested chord position and cache those evaluations; retain the declared sampled maximum-thickness
limit. The independent differing-parameterization probe is the control; it does not certify the continuous
kernel. Exact thickness extrema and geometry certification remain unverified production obligations.

**DATA-D / UI-M recurrence:** a profile-bank editor must derive its CV count, knot vector and valid selected
index from the inspected profile, not a previously visited profile. The source parser permits different
six/twelve-CV profile records. The authoring oracle visits both, requiring correct metadata, bounded
selection and unchanged accepted geometry/source. Draft-title and bottom-status projections are included
in active-draft reflow checks; an accepted-only screenshot misses those states.

**Test-fixture integrity:** a legacy lifecycle test directly appended station 0.7 after its earlier proposal
had already created that station. Sweep: direct station-list mutations in the CAD oracle. Derive: use the
public add operation only when absent; never corrupt the model to establish an unrelated lifecycle setup.
The full flow now runs without the observed DSL-STATION error, and source reconciliation validates emitted
records before replacing accepted text. Historical v6 remains available as its original contract oracle.

**Graph metadata recurrence:** an unregistered `follows` relation was rejected by the whole-repository docs
gate; it was corrected to the registered `relates-to`. The validator remains the preventive control.

## Coverage correction — weighted editing and linked flow evidence

The 2026-09-19 user iteration extends **SPEC-A**: the earlier mockup's profile edit was a camber fixture, its analysis condition handler only reported that conditions changed, and Results was a static pressure view. A visually populated control is not proof that the requested quantity has a compute reader. Sweep: section/outline editing, analysis inputs and metrics, simulation setup, Results field/plot/table selection, and unit conversion. Derive all views from accepted curve definitions or one selected immutable sample. Prevent: the extended `tools/check-mockup.mjs` must change weights/conditions/samples and assert the resulting geometry, load or field changes, then assert Cancel, Undo and historical isolation. A missing-control assertion was observed failing before this iteration's UI edits. Production curve and CFD validation remains a separate obligation.

**EVID-A recurrence:** after new sweeps could capture a later accepted revision, some banner/assistant/status strings still hardcoded revision 12. The geometry itself remained pinned. The sweep reached all revision labels; they now derive from the selected run snapshot. The regression must build a run after a geometry edit and compare its labels with its pinned identity. **HYG-A cleanup:** overwritten legacy condition/field handlers were removed instead of retaining a second dead implementation.

No product defects or scientific validation tests are claimed executed in this documentation/prototype task. Controls will fail the implementation gate if these fixtures or end-to-end paths are absent.

**2026-09-20 sweep (spec v1 and mockup v1).** The v1 gate found 1 Blocker and 20 Majors on revision 1.0; every one is a sentence-level control now in the spec (identity oracle per object kind, one run key, AI-06 thresholds, single home for station → profile, exclusion on the finding, ψ-aligned thickness, one e band, 41 enumerated non-happy flow edges, state · string · component table). The mockup's em-dash saturation finding is a standing, recorded deviation: the strings are the spec's fixed copy (one state, one string) and are not rewritten for cadence.

**2026-09-21 sweep (mockup v4, CAD editing views).** The operator's list of "little things" (icons, splines, free rotation, a 2D station editor, elevations for curve editing, explicit control curves) was one class — the drawing had been treated as an illustration rather than the interface — recorded as UI-J with the drawing-model rule as its control.

**2026-09-21 sweep (mockup v3, thick-client shell).** The operator's diagnosis (page scroll, a wrapping toolbar) was measured before the rebuild and became UI-H2; the layout-facet mismatch is the class, the v2 page the instance. Five layout defects found while building the shell (implicit grid rows and columns from auto-placement, a closed `<details>` menu counting toward scrollWidth, duplicate ids across navigator and toolbar) were caught by the shell oracle and the smoke run before any gate; they are recorded in `docs/notes/thick-client-shell.md` as constraints, not classes, because the oracle already fails their shape.

**2026-09-21 sweep (spec 1.1 and mockup v2).** The 1.1 delta gate found 3 Blockers and 28 Majors, all definitional; three are new classes above (SPEC-C, DATA-C, SEC-B). The mockup v2 build surfaced UI-H before any gate through the smoke run — the cheapest control fired first.
## CO-UI-READY · Launch success mistaken for review readiness

**Class and signature:** a process starts and reports `Window.Opened`, but the
actual inspection adapter cannot attach. Repeated operator foreground requests
then become an unbounded barrier for the whole execution graph. Five requests and
an approximately ten-hour wait were reported in the native workbench review.

**Why it survived:** build, process-liveness and applied-style tests did not call
the supported native AX/screenshot adapter. The process-accumulation guard correctly
prevents duplicate launches but never established attachment. A historical
`cgWindowNotFound` does not prove either missing target-bundle permission or an
off-Space window. A later reproduction affected both Workbench and System Settings;
supported CUA session reset restored the same original app PID. Internal stale
session state is inferred; the recovery boundary is observed.

**Class → sweep → derive → prevent:** the investigation swept native review
history, launcher, process guard and C contract. Confirmed siblings are the repeated
path/bundle attachment waits; process cleanup is a related but different guard.
Current CUA host permission reads and working captures rule out a blanket current
denial; temporary unique bundles also attach successfully, so their identity alone
is not a verified cause. No relevant `assume:`/`simplify:` marker in those launch
surfaces established an activation contract.

**Control:** start each review attachment with the supported CUA REPL reset and
first-call initialization, then `docs/coordination/review-attach.mjs` requires exact-title AX and a
nonempty supported screenshot, optionally uses the advertised Raise action, and
returns a bounded failure after at most three attempts/30 seconds. The executable
`tools/check-review-attach.mjs` gate was observed exiting 1 with
`NATIVE_REVIEW_BLOCKED` for the old successful-process-only receipt. The focused
test injects the historical adapter error and rejects wrong windows, incomplete
surfaces, missing capture, human-assisted, stale and timeout evidence. Injected
failure is protocol regression evidence, not a claim to reproduce the original
macOS trigger. Fresh-build observations and independent disposition belong in
[the investigation](../investigations/review-window-attach.md).

**Always-loaded rule:** batch necessary human requests once; block only dependent
nodes and schedule other ready work. Do not repeat a routine foreground request.
Do not widen permissions or substitute another UI API to clear the gate. Re-read
PID/start/executable before and after attach because CUA may relaunch an exited
app and does not expose a PID in its app handle.

**API-boundary recurrence (NG):** an unverified review persona caused an exited
launch; validating liveness before any CUA call prevents an automatic relaunch
from hiding it. The actual review parser admits designer/keyboard/screen-reader/
dense, not arbitrary prose roles. A separate helper error used realm-local
`instanceof Uint8Array` for a cross-realm screenshot Buffer. The actual byte view
was observed, a `node:vm` negative failed before repair, and the helper now checks
`ArrayBuffer.isView` plus the byte-array tag. Do not infer transport object identity
from a type declaration. Literal CUA function code is required; string code
generation is unavailable. The regression suite also exercises a changed window
after Raise and a timed-out uncancellable operation with no overlapping retry.

**CO-DECISION-WRITE · An Owner ruling mutates the designated leader tree during
an active conductor merge.** Ruling 40 was recorded through the supported
`coord decide rule next` command while the Coordinator's docs-only R39 review
merge had an unresolved generated-index conflict. The decision tool correctly
appended the authoritative ruling and audit in the designated leader checkout,
but the append appeared there as unstaged changes while the merge was open.
The independent Owner tree and its index stayed clean. This was a real
cross-session shared-checkout write window, even though no ruling bytes were
lost: the Coordinator inspected the exact append, regenerated the derived
views, staged the register and review together, and the conductor's conflict,
docs and ruling-citation gates passed.

**Class → sweep → derive → prevent:** a register-class path needs no ordinary
lease, so lease checks alone cannot serialize a decision append with a merge.
The same risk applies to future `coord decide rule next` calls and any official
writer targeting the leader tree's audit/register during conductor resolution.
The always-read [coordination plan](../coordination/application-build.md) now
requires a short shared-register write window: finish or pause the conductor
merge, tell the Owner when the designated leader tree is ready, let the ruling
append, inspect its unstaged diff and derived views, then resume the merge.
Do not transfer that write to the Owner's branch or overwrite the append to
make a generated conflict disappear. The check is the exact register/audit
diff plus `verify-ruling-citations.py` and the conductor gates; this incident
was recovered, not evidence that concurrent register writes are safe.

**EVID-WIN-STATE · A source-bound receipt validates the scenario name but not
its scenario-specific persisted state.** In the W0 Windows qualifier draft,
the common receipt validator checked fixture/host fields while its expected
state map omitted conflict preservation and foreign-file retention on cleanup
refusal. Root's disposable synthetic review and the author independently
fed a conflict row with a wrong after-hash and a cleanup-refusal row with
wrong `foreignAfter`; each case was accepted before the correction. The
overall W0 receipt was still nonpassing because unrelated native rows were
Not assessed, which could have hidden these local false accepts. Sweep the
same shape across cancel-before (target absent), write-fault owned cleanup
and every other named stateful case, not only the two reproduced rows.

**Class → sweep → derive → prevent:** a source/binary hash proves which
qualifier ran, not that each scenario's expected bytes and publication state
were asserted. The W0 author is adding case-specific pre/post/foreign byte
contracts and `--self-test` field mutations; a separate copied-source drift
negative must reject edits after the pre-build hash. The [always-read Windows
route](../coordination/windows-runtime-route.md) requires those controls
before W0 handback. This register records the confirmed draft failure class;
final repaired PASS remains subject to frozen author receipt and independent
root Data/Security/Test review. No Windows-native case is made PASS by a
synthetic validator test on macOS.

**EVID-TARGET-IDENTITY · A convenient object or host path is mistaken for the value being proved.** In R47's timing preparation, an envelope-identity negative shared the frame's mutable identity object. Removing the envelope guard left the frame guard to fail, so the deliberate mutant escaped the intended test. A separate self-process check compared anonymous C# objects by reference: equal PID/start/executable values printed identically but returned unstable. A copied fixture root used macOS's symlinked temporary-directory spelling and failed the production no-symlink rule for the wrong reason. The first dependency reader also treated an empty portable target as competing with the concrete RID-specific target. These are distinct instances of the same evidence-boundary error: a test surrogate did not preserve the real identity or representation grain.

**Class → sweep → derive → prevent:** the R47 suite now copies envelope and frame identities independently and includes frame-only mismatches; all four deliberate source mutants fail. The self-process oracle compares identity values, and its equal-value case must remain green. The temporary positive root is canonicalized while the actual package reader still refuses symlinked roots. The real deps reader selects the concrete RID and retains ambiguity/missing-asset negatives. Root independently replayed 78 pure controls and four mutants against frozen `657cd8c`; this proves these preparation controls, not native target identity. The [proof](../proof/visible-presentation.md) keeps the initial failures and corrected outcomes.

**EVID-CLOCK-CONSUMER · A derived receipt reports a plausible empty result as success.** The standalone R47 clock-report consumer returned exit zero and `offset_intersection_ns: null` for disjoint paired brackets even though the synthetic assessor rejected the same clock contradiction. Its permanent disjoint-pair fixture now requires `VP-CLOCK`/exit 3; the raw RED/GREEN receipts are retained in the proof capsule. Root's first independent recomputation omitted the documented clock quantization and failed its own assertion; the corrected rational check includes both quanta and records `[-128/3, 42]` ns for the retained 32 pairs. The control is the executable disjoint-intersection refusal plus independent exact-unit recomputation, not prose about clocks. Neither check measures cross-process drift or a displayed frame.

**COORD-SPIRAL · A verification or coordination loop around a self-made artifact with no repair-cycle cap.** Between 2026-09-25 04:09Z and 08:56Z a Codex-led session made 131 commits across branches; zero touched `src/` or `tests/`; roughly 75% of subjects were bookkeeping (join, record, ruling, review, hold, schedule, authorize). The mechanism: ruling → author packet → independent review finds the validator false-accepts → repair ruling → repeat (R41–R56). The evidence validator became the product; no cap bounded the repair cycles, so the loop ran unbounded instead of stopping and escalating.

**Class → sweep → derive → prevent:** the class is any coordination/verification loop that keeps repairing its own evidence artifact with no cycle cap and no product-side diff. Controls: `tools/check-spiral.py` (wired into `tools/check-docs.py`) fails a branch whose commits since `main` are `>= 12`, none touch `src/` or `tests/`, and `>= 60%` of subjects match the bookkeeping vocabulary; and the AGENTS.md project section now states the 2-cycle repair cap — at the cap, stop the track and report to the operator instead of issuing another ruling.

**EDIT-KIND-REOPEN · A new edit kind ships without the persisted-reference check learning it.** Insert, Delete, Fair and Rebuild stored their kind in `EditReceipt.Rail`, but the envelope check (`EditTarget`) knew only rail and side names, so any project saved after a construction failed to reopen with `DOC-REFERENCE`, and a recovery captured mid-construction blocked opening the whole project. Found by the Fair/Rebuild sub-agent, not by a test.

**Class → sweep → derive → prevent:** any new draft kind must round-trip save → reopen → Undo/Redo and recovery. Sweep covered the accepted-edit check and the recovery check. Control: `ReopenConstructionTests` exercises every construction kind and both recovery cases; a kind added without a matching case is the gap a reviewer looks for first.

**BUDGET-DISPLAY · Display sampling ran at certificate precision inside the proof budget.** `ProfileAt` sampled the display curves with the certificate's tolerance, spending about 970 ms of the 1-second proof budget for a 10-vertex Rebuild (validation itself took 13 ms), so small overheads threw `GEOMETRY-BUDGET`; reopen and Undo/Redo then threw instead of degrading.

**Class → sweep → derive → prevent:** presentation work must not share or spend the certification budget at certificate precision, and a budget refusal on a stored revision must degrade to Not assessed, never throw. Controls: display samples stop at 1e-8 (measured about 430 ms); `Rebuild_TenVertices_CertifiedOneUndoItem` runs under the real budget; `Reopen_ForcedBudgetExhaustion_OpensNotAssessed` forces a zero budget and requires reopen to succeed.

**DET-CLOCK · A wall-clock budget in a deterministic path.** `ProofBudget` refused a geometry proof once one second of wall time had passed. Under CPU load, fast-ring Core checks failed with `GEOMETRY-BUDGET` ("Cooperative proof time budget exhausted"). Examples are `Rebuild_TenVertices_CertifiedOneUndoItem`, `Reopen_InsertThenDelete_NeverThrows` and `Profile_LocalSupport_OutsideSpansUnchanged`, plus 7 checks at 8 Desktop slots: at least 4 times on 2026-10-02. A slower or busier user machine would refuse an edit that a quiet one accepts. The result of a deterministic path depended on the machine's load. Operator ruling (2026-10-02): the proof limit counts work, not clock.

**Class → sweep → derive → prevent:** signature: a deterministic computation that reads a clock to decide its outcome, through a `Stopwatch`, a `TimeSpan` limit, `DateTime` or `TimeProvider`. Telemetry that only *records* elapsed time is not a member. Sweep (2026-10-03, `src/**`): `ProofBudget` was the one instance. It was used by `Geometry.Assess`, `PointAt`, `SectionAt`, `AuthoringSession.Sample` and `ThicknessFit.Maximum`. The other `Stopwatch` and `TimeProvider` uses in Core, Persistence and Desktop measure durations for events, frames and logs and decide nothing. Derive: measure the work before choosing the unit. Over 11,780 budgets, the summed operand bits of each exact rational tracked proof time with r = 0.99; the call count of rational operations scored 0.84 and the count of `Check()` calls 0.46. So the limit counts bit-work, at 1,000,000,000 units. That is 4.4x the worst accepted proof (225,137,163 units, the display samples of a ten-vertex rebuild). Controls: `ProofBudget_HoldsNoClock` (fast ring, structural) fails when the budget holds a clock-typed field. `Readiness_ProofOutcome_SameWithCpuStarved` (readiness ring) runs the worst proof quiet and again under 4 spinning threads per core, and requires the same outcome. It was red first on the time budget (`ProofRefusal: Cooperative proof time budget exhausted.`). `ProofBudget_SameProof_SameWorkAndExactBoundary` pins the same work for the same proof and the exact refusal boundary. `Readiness_ProofWork_WorstFixtureWithinQuarterOfLimit` fails when the worst case passes a quarter of the limit, so a recalibration is deliberate and measured.

The Computational Geometry review found a sibling: order-dependent work. `Geometry.TwistDomainDegrees` was a static initializer that ran an exact-rational binary search. The first proof to touch `Geometry` paid for it (57,835 units), so `ProofBudget_SameProof_SameWorkAndExactBoundary` was red when run alone (`Expected 3524586; actual 3466751`). Sweep: the proof-path types and the lazy values inside a proof (`SourceParse` hashes do no rational work). That initializer was the only instance. Fix: the value is a pinned `const` (design §5.1), and `LargestAdmissibleTwist()` is now its test oracle. Control: `ProofBudget_ProofPathTypes_HaveNoTypeInitializer` fails when a proof-path type gains a type initializer. A unit's cost also grows with operand width: about 2–3 ns at 512 bits and about 10 ns at the 32768-bit cap (Apple silicon, quiet). So the limit bounds work, not a fixed time: a refusal near the cap can take about 10 s. The calibration check reports the widest accepted operand, 3,971 bits.

**VLM-TIP-B · A calibrated numerical convention is presented as physical uncertainty.** Found on the held branch `fix/a3a-vlm-tip-law` (rejected, Ruling 88 D1). Its η* law made the fine-n tip error c=0 by construction (the reference was the n128/n256 mean), so its band bounded only discretisation of the convention. Elliptic α5 gave α_eff* about 10.55° while mid-span CL/(π AR) and the tip's own `Cl_local` angle (about 4.4°) disagreed, and the rectangular α18 falsifier could not be made Outside. Sweep: any tolerance fitted to a reference built from the same lattice family. Derive: such a band is a convergence statement, never a validity statement; a verdict needs an independent anchor (analytic mid-span, the strip's own lift). Prevent: the evidence is committed in `docs/proof/vlm-tip-study/repaired-verdict.md`; the analytic mid-span anchor is the test `Readiness_EllipticMidspan_InducedAngleMatchesCLOverPiAR`; the tip strip stays unjudged (`MarkOutermostProvisional`, Ruling 78) until the S2/S3 study in `docs/plans/tip-handling.md` supplies an independent basis.

**VLM-TIP-D · A new unit conversion duplicates an owned constant.** The held branch wrote `180 / Math.PI` in `MethodRecord.cs`, a second source of the degree conversion. The fast Core check `PlacementRule_RadiansConstant_SingleSiteInSource` was red on exactly the new site and green after the one-line repair to `VortexLattice.ToDegrees`. Sweep: every `180 / Math.PI` site in source. Derive: radians to degrees goes through `VortexLattice.ToDegrees`. Control: that named check, which is on main.

**HARNESS-SILENT-EXIT · An external agent exits 0 without doing the work.** The Agy wiring run waited 80 minutes on its own background task, hit its print timeout, and exited 0 with 13 changed lines and no report. An exit code is not a result.

**Class → sweep → derive → prevent:** a delegation is done only when its final message contains the brief's Return section and the branch shows the claimed commit. Controls: every delegation brief now says "run everything in the foreground; no background processes or sub-agents"; the Leader reads the branch (`git log`, `git status`) before treating a run as complete, and a missing Return section counts as a failed cycle against the 2-cycle cap.

**TEST-RING · The slow gate runs on every join while the tests do not.** Measured 2026-09-27: every join ran two app gates (a GUI launch, a cold NuGet restore, four self-contained publishes, the Core suite nine times; about 376 s when green, Inferred) and two spike recounts (20 s, inputs `tools/spikes/**` only). It never ran `tools/run-tests.sh`. Both app gates were red on `main` and the joins went ahead, so the expensive gate was also ignored.

**Class → sweep → derive → prevent:** a gate belongs to the ring its cost and its trigger justify. The fast ring runs on every join and must stay cheap and green, so it can be enforced. Slow, GUI or publish gates and input-scoped recounts belong at readiness. Sweep: `join.json` checks, recount and gates; `.github/workflows` (no workflow runs C#, recorded as an open item). Controls: `check-docs.py` TEST-RING fails when the fast ring loses `run-tests.sh` or `xaml-token-lint`, or regains a slow gate or a recount, and when the readiness ring loses any of them. `tools/run-readiness.py` runs that ring and records a receipt for HEAD; `--check` refuses a merge to main without a green receipt. Red-first: the old `join.json` and a weakened copy both fail TEST-RING.

**TEST-COST · Test wall time grows with no number attached.** The runner ran three independent suites in series in Debug and printed no time, so a 67 s run was never seen as a cost. Plans then multiplied it: "three times with identical PASS sets" and 1–2.5 h agent time boxes (S8 was boxed at 1 h and measured 254 s).

**Class → sweep → derive → prevent:** measure the suite on every run, and fail when it grows. Controls: `tools/run-tests.sh` prints build, per-suite and wall seconds and PASS counts. It exits 3 (TEST-BUDGET) when a green run is over `CFD_TEST_BUDGET_SECONDS` (default 60 s, 2× the measured 30 s). It exits 1 when a named suite exits 0 with no `PASS` line (the HARNESS-SILENT-EXIT shape at the suite level). Red-first: a 5 s budget exits 3; a zero-PASS named suite exits 1. Sweep (F2): `tools/verify-application-core.py` re-ran the whole Core suite 9 times for properties only the 32 store checks read (umask, native helper), 302 s against a 300 s ring limit. It now runs the full suite once per build shape and the store checks alone under the other masks and fault variants: 87–90 s. Controls: the Core harness selector `CFD_TEST_ONLY` fails when a prefix selects nothing; the gate fails `STORE-SUBSET` when a `ProjectStoreTests.cs` check has a name the selector cannot reach, when file, environment, temp-path, Persistence or native code appears in another Core test file or in `src/CfdWorkbench.Core`, or when a run does not pass exactly the store checks named in the source. Red-first: a helper that widens files to 0644 only under umask 0077 passed the full run and failed the 0077 subset (gate exit 1); a store run that silently skipped 27 checks and exited 0 failed the gate; a selector matching nothing exits 1.

**TEST-RECEIPT-RACE · A test fails on its receipt metadata, not its assertion.** `AssertPrivateMode` read `Process.StartTime` on a short-lived `/usr/bin/stat` after it could already be reaped. Under CPU load that threw `Win32Exception` and failed a store-permission test whose assertion held. The SRE review saw it once, in the first loaded run.

**Class → sweep → derive → prevent:** evidence fields that are recorded but not asserted degrade to "Not recorded" and never throw (IO8). Sweep: `git grep .StartTime` over src, tests and tools found this one site. Control: the helper `StartTimeOrNotRecorded`, plus the loaded-run check in docs/reviews/test-ci-waste.md (16 busy loops, 3 green runs). No automated load ring exists; this is recorded as an open item.

**HARNESS-LAUNCH-SHAPE · A process relaunches itself assuming one launch shape, and its unhandled exception aborts instead of naming itself.** The Desktop test harness started its section-flow and section-tools children with `ProcessStartInfo(Environment.ProcessPath!, "--section-flow")`. Under the apphost (`dotnet run`, used by `tools/run-tests.sh`) that is the program. Under `dotnet X.dll` (the adapters gate) it is the muxer, so the child ran `dotnet --section-flow` and exited 1. The harness threw `section-flow exited 1` (`WorkbenchTests.cs:1867`). With no top-level handler, .NET called `abort()`: exit 134 and a macOS crash report on every gate run. `CfdWorkbench.Desktop` had the same abort path (`CFDW_REVIEW_MODE=2` → 134). Investigation: `docs/investigations/desktop-launch-abort.md`.

**Class → sweep → derive → prevent:** signature: `Environment.ProcessPath` (or `MainModule`, `GetCommandLineArgs()[0]`) used as a relaunch target; a `Main` or top-level program with no unhandled-exception exit. It survived because the inner loop runs only the apphost shape, the gate that uses the muxer was already red on `main`, and each failure left a crash report rather than a message. Sweep: those two relaunch sites were the only `ProcessPath` uses. The app and the Desktop harness were the only unguarded GUI entry points. The Core and CLI harnesses keep the runtime default (not changed). Controls: `SelfLaunch.StartInfo` passes the entry assembly unless the host is the apphost. `SelfLaunchTests` (first in the default harness run, in both `run-tests.sh` and the gate) checks both launch shapes and a child-throw probe that must exit 70 with `APP-UNHANDLED`. A source scan fails on any `Environment.ProcessPath` in `src/` or `tests/` outside `SelfLaunch.cs`. All were observed failing (3/4) on the unfixed code. `StartupFailure.Install()` (app and harness) turns an unhandled exception into `APP-UNHANDLED <exception>` on stderr and exit 70. The related gate defect, Avalonia's build telemetry collector outliving `dotnet build` under the core gate's exact process ownership, is fixed by `AVALONIA_TELEMETRY_OPTOUT=1` in `tools/verify-application-core.py`, matching the adapters gate.

**NAV-STAR-COLLAPSE · A new Auto element above a star-sized list starves it to zero at the minimum viewport.** `80758ec` put the Auto-height station card above the navigator's `*` Rail controls row. At 1024 × 700 the fixed rows exceed the column, so `ControlList` got no bounds and its items never realized. Bisect: `7fbd5dd` gave 40/40 cv theme rows, `80758ec` gave 0/40. It survived because `run-tests.sh` never runs `--theme-controls`, and the adapters gate that does was stopped earlier by the HARNESS-LAUNCH-SHAPE abort. **Class → sweep → derive → prevent:** swept the other star rows in `MainWindow.axaml`. The centre document area is the window's own `*` column and row, and the Section tab already scrolls. Control: the navigator scrolls, and its lists take `NavigatorListHeight`. The 1024 × 700 `--theme-controls` matrix in `tools/verify-application-adapters.py` fails on a zero-bound list; it was observed failing on `80758ec` and on the unfixed HEAD.

**GUI-AMBIENT-INPUT · A GUI probe assumes no real OS pointer over the window.** After the navigator relayout, the operator's cursor sat over the unselected CV item, and its `rest` row read `pointerOver=true`. With the cursor parked off-window: 40/40. With it in place: 36/40. **Class → sweep → derive → prevent:** every `ProbeStates` row shares the shape. Control: the probe establishes rest (a synthetic pointer exit) instead of assuming it. It was observed green with the cursor in its failing position.

**UI-DEAD-CONTROL · A visible enabled shell button has neither a Click handler nor a bound Command.** The M1.2a open-failure actions were shown and focused but did nothing. The sweep also found candidate-ID and recovery alert actions in the shell. **Class → sweep → derive → prevent:** `UI_DEAD_CONTROL_ShellButtonsHaveActions` walks the shell visual tree in Start, Opening, open-failure, candidate-ID, recovery and ordinary alert states. It fails on every visible enabled application button without `Button.ClickEvent` registration or a bound Command. Framework-owned `PART_` template buttons use Dock's own input path and are excluded. The red run named AlertLocateButton, AlertOpenAnotherButton, AlertTryAgainButton and AlertRemoveRecentButton together with the three alert-band actions. Action-effect tests cover the four open-failure buttons separately.

**TEST-REPO-LAYOUT · A test walks `AppContext.BaseDirectory` looking for `CFDWorkbench.slnx`, and breaks when the gate runs the built assembly from an artifacts directory outside the repo (HARNESS-LAUNCH-SHAPE's sibling).** `ShellWindowTests.cs`'s `FindRepoRoot()` walked up from the test binary's own directory. The inner loop (`tools/run-tests.sh`) always runs in-repo, so this never failed there. `tools/verify-application-adapters.py` builds with `--artifacts-path` under a task-local temp scratch root and runs the Desktop test DLL from there, so the walk never found the solution file and threw `DirectoryNotFoundException`, failing `Recent_*`, `OpenFailure_Alert*`, `Open_Refused/NeedsIds_*`, `Copy_*` and `Telemetry_MarkerInjection_*` — 11+ Desktop checks. Separately, `tools/verify-application-core.py`'s STORE-SUBSET guard flagged the same class one layer down: `LayoutFileTests.cs` and `PreferenceStoreTests.cs` create files and read `Path.GetTempPath()` (P1 preference-file permissions) but sat outside the umask-varying `ProjectStoreTests.cs` subset, so their file-permission checks ran once, at the build's default umask, never proven under the other masks.

**Class → sweep → derive → prevent:** signature: a test resolving the checked-out repository tree at runtime from `AppContext.BaseDirectory`, rather than at compile time from `[CallerFilePath]` (the pattern `SelfLaunch.cs` already used correctly) or from a linked fixture. Sweep: every `FindRepoRoot()`/`.tmp-tests` call site in `ShellWindowTests.cs`. A scratch directory now resolves under `Path.GetTempPath()` (the harness's TMPDIR, same alias-fix as `LayoutFileTests.Root()`), never a repo-relative path. A repo file a test reads — `DESIGN.md`'s COPY rows, the `example.foil` fixture — is linked into the test output by `CfdWorkbench.Desktop.Tests.csproj` `Content` items and read from `AppContext.BaseDirectory`. The one check that genuinely needs the whole checked-out tree, `Architecture_DockConfinedToShell` (a scan of every `.cs` file under `src/CfdWorkbench.Desktop`), resolves the root from `[CallerFilePath]` instead, exactly like `SelfLaunch.cs`'s own scan — left unchanged, since it already used the safe pattern. Controls: `verify-application-core.py`'s `STORE_PREFIXES`/`STORE_TEST_FILES` now cover `LayoutFileTests.cs` and `PreferenceStoreTests.cs` too, so their checks run under every umask variant and are exempt from the STORE-SUBSET sensitive-code sweep as a declared subset, not a leak. `SelfLaunchTests.NoRuntimeRepoRootWalk` (fast ring, next to the existing `Environment.ProcessPath` scan) fails on any file under `tests/` that references `CFDWorkbench.slnx` without also using `CallerFilePath` — the signature of a runtime walk rather than a compile-time one. Red-first: the unfixed `ShellWindowTests.cs` fails this new check (`SelfLaunchTests` throws `APP-UNHANDLED` naming the file); a planted `.tmp-tests`/`AppContext.BaseDirectory` walk in an unrelated `tests/` file fails it the same way.

**Recurrence (2026-09-30, READYFIX2):** `SelfLaunchTests.NoRuntimeRepoRootWalk` only scanned for the
`CFDWorkbench.slnx`-walk shape and missed the cwd-relative-literal variant: `FoilSourceTests.cs`,
`GeometryTests.cs`, `PointGestureTests.cs`, `PointModelTests.cs`, `PointCommandTests.cs` and
`ReopenPointEditTests.cs` read M1.2b fixtures through `"tests/CfdWorkbench.Core.Tests/Fixtures/m12b/" + name`
— a string built against the process's current directory, not the repo. `tools/verify-application-core.py`
runs the *published* `CfdWorkbench.Core.Tests.dll` with `cwd=published` (a task-local artifacts directory, not
the repo root) to prove the shipped binary behaves the same as the one `run-tests.sh` built in place; that
run saw 38 failures (`RESULT failures=38`) because every fixture path resolved against the wrong directory.
`tools/run-tests.sh` never catches this, because its cwd is always the repo root. Fix: each fixture is now read
through `M12bFixtures.Path(name)` (`tests/CfdWorkbench.Core.Tests/M12bFixtures.cs`), which resolves
`AppContext.BaseDirectory` first (where the csproj's new `Content Include="Fixtures\m12b\**\*"` item copies
the files) and falls back to the `[CallerFilePath]`-relative source tree — the same shape `LayoutFileTests.Fixture`
already used for the layout fixtures. Control: a sibling scan, `SelfLaunchTests.NoCwdRelativeFixturePath`, fails
on any string literal under `tests/CfdWorkbench.Core.Tests` matching `"(tests|src)/...")` — the literal-prefix
shape `NoRuntimeRepoRootWalk`'s narrower pattern missed. Scope note: the scan is deliberately bounded to
`CfdWorkbench.Core.Tests`, the one project a gate relocates (verified by reading every `cwd=` call site in
`tools/*.py`: `verify-application-core.py` is the only script that runs a test DLL from a directory other than
`ROOT`). `CfdWorkbench.Desktop.Tests` and `CfdWorkbench.Cli.Tests` carry the same cwd-relative-literal shape
today (`WorkbenchTests.cs`, `ControllerShellTests.cs` read `"src/CfdWorkbench.Desktop/Assets/example.foil"`)
but are not yet failing, because `verify-application-adapters.py` always launches them with `cwd=ROOT`; they
are a residual watch item, not a current violation, and the scan must widen to cover a project the moment any
gate starts relocating its cwd. The fix itself first tripped the STORE-SUBSET control
(`verify-application-core.py:59-66`): a `File.Exists`-probed fallback in the new `M12bFixtures.Path` read as
umask-sensitive code outside the declared store-test files. Resolved by dropping the probe — the csproj's new
`Content` item guarantees the fixture sits at `AppContext.BaseDirectory` under every build shape, so one
`Path.Combine` is enough, and that control stayed exactly as strict as it was.

**UI-RENDERED-STATE · A control exists while its realized output is absent.** The M1.2a shell passed 390 tests before the operator saw an empty Plan + 3D view after tab re-entry, duplicate tab rows, and an unbound Section canvas. The prior checks inspected control existence and view-model state but never forced a draw after leaving and returning to a Dock document. Sweep: each model document's realized control, visual root, nonzero bounds, and document-specific render input; the selected and no-station Section states.

**Class → sweep → derive → prevent:** the fast-ring controls are `Shell_F7_ModelTabReentry_RendersAcceptedFoil`, which forces a frame invalidation after return and requires a new draw of the accepted frame, and `Shell_AllModelTabs_ReentryRealizesContent`, which re-enters each Dock model tab and asserts attached, nonzero content and its render input. `Shell_F9_SectionSelectedStation_DrawsProfile` and `Shell_F9_SectionNoStation_ShowsEmptyCopy` cover both Section states. The F7 red run was `LastRecordedRevision = 0` after `FrameRevision = 1` despite a visible, nonzero viewport; Dock reveal duration zero made it green. Future model tabs join the re-entry array before their code is accepted. Evidence: [shellfix red runs](../proof/shellfix-red-runs.md).

Design control (M1.2b): signature: a visible behaviour (a view, a selection, a focus ring, a drag) proven only by controller or view-model state, never by the realized visual tree or the rendered pixels. Control (M1.2b design §12.3 tier 3): rendered tests render the whole realized window, locate targets through `TranslatePoint`, and read pixels there (glyph, ring, curve, orientation); state-only assertions do not count as proof of a visible behaviour; named tests `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints`, `PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn`, `PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne`. Before an operator session every native row carries an agent attach receipt with a screenshot (CO-UI-READY). Sweep of the other M1.2a canvases (`SectionCanvas`) is the M1.2c design's.

**REPLAY-MEMO · An idempotency memo is rebuilt with a different key than the one written.** A dimension commit memoizes `"dimension:"+Name+":"+Text` (`AuthoringSession.cs`:674), but reopen rebuilds every non-open, non-undo cursor's memo as `"apply:"+SessionBinding` (`:802-810`). After a reopen, a retry with the same operation id is refused `DOC-OPERATION-CONFLICT` instead of returning the prior id. Found by the Patterns Expert at the M1.2b design gate, by reading, not by a test.

**Class → sweep → derive → prevent:** signature: two functions compute the same memo or cache key (one at write, one at rebuild). Control: one `Fingerprint(receipt)` used at commit and at replay, landing in M1.2b B1a; tests `Reopen_RetrySameDimensionOperationId_ReturnsPriorId` and `Reopen_RetrySamePointOperationId_ReturnsPriorId`. Sweep for the implementer: every `operations[...] =` and `operations.Add` site in `AuthoringSession.cs`.

**GEOM-AUTHORITY · A display re-derives the placement rule and becomes a second geometry definition.** The reviewed
v10 mockup's `draw3d` and `drawSide` (`docs/mockups/workbench-v10.html`:690-697) place sections as `(le + x·c, y,
dih + z·c)` with no twist rotation, although the example foil carries −2° at the tip: a display that re-wrote FoilDSL §6
dropped a term and nothing failed. The M1.2b geometry lens refused the same shape for a 3D view (m12b-points §0.2, OI-1).

**Class → sweep → derive → prevent:** signature: any code outside the certificate that computes a placed point from
channel values (twist rotation, dihedral offset, Rule A normalization), or restates the radians-per-degree constant.
Sweep (2026-09-30, `3a37f5f`): `src/` computes placement only in `Geometry.PointAt` (`Geometry.cs`:85-119); the constant
appears four times, all in `Geometry.cs` (`:100`, `:413`, `:477`, `:640`); the v10 mockup is the one instance. Controls
(ADR-0010, M1.2b2 design §9): `PlacementRule_RadiansConstant_SingleSiteInSource` (the literal occurs once in `src/`),
`PlacementRule_SelectBlend_SameStationsAsCertificate`, `Placement_DisplayWithinCertifiedEnclosure_Fixtures` (every
display point within 1 nm of the certified enclosure), the certificate golden master (its failure names the bound
models to review), the compiler (curve records and the evaluator are `internal` to Core, so the Desktop cannot evaluate
a curve), and the sign fixture `Placement_SignFixture_PositiveTwistTrailingEdgeDown` rendered in every view.

**REPLAY-CROSS-KIND · A replay guard assumed every memoized operation id belonged to its own command family, and crashed instead of refusing when it did not.** `ApplyPointCommandCore` (`AuthoringSession.cs`) treated `operations.ContainsKey(operationId)` alone as "this id is a point-command replay," then read `accepted.Single(row => row.OperationId == operationId)` to recover the prior parent. An id first consumed by `ApplyDimension` or by `Undo`/`Redo` also sets `replay = true` on a later `ApplyPointCommand` call with the same id, but leaves no matching point-command row in `accepted`, so `.Single()` threw an untyped `InvalidOperationException: Sequence contains no matching element` instead of the typed `DOC-OPERATION-CONFLICT` every other reuse path returns — both in the same session and after a reopen. Red run: `docs/proof/m12b-b1b-red-runs.md` § "Cursor operation replay red" (`FAIL History_ReapplyOperationDifferentPayload_Refused InvalidOperationException: Sequence contains no matching element`), fixed in `d1e8d45`.

**Class → sweep → derive → prevent:** signature: a replay/idempotency check that branches only on "is this id known" without checking what kind of thing it was known *as*. Derive: recover the prior record with `SingleOrDefault`, not `Single`, and refuse by contract error whenever its kind is outside the set legitimately eligible to replay (`AuthoringSession.cs`:1001-1004 — `priorOperation?.Edit?.Rail is not ("point-type" or "tangent-kind")` — `DOC-OPERATION-CONFLICT`, never an unhandled exception). Controls: `ApplyPointCommand_SameOperationDifferentKind_DocOperationConflict` and `Reopen_SameOperationDifferentKind_DocOperationConflict` (`tests/CfdWorkbench.Core.Tests/PointCommandTests.cs`, `ReopenPointEditTests.cs`) cover same-family kind mismatch (already refused via the `Fingerprint`/`Retry` payload check, unaffected by this fix); `History_ReapplyOperationDifferentPayload_Refused` (`ReopenPointEditTests.cs`:25-37) is the one that exercises cross-family reuse — a Dimension-consumed id and an Undo-consumed id, each retried as a `PointCommand` — and is the test that observed the untyped crash before the guard. Sweep for a future kind: any new operation-consuming call (`ApplyDimension`, `Undo`, `Redo`, `ApplyPointCommand`) that adds to `operations` without a matching `accepted` row of a kind the other consumers recognize.

**CLAMP-RAW-NOT-DERIVED · An overflow guard checked the raw input for finiteness but not the value derived from it, so a large-but-finite input still produced a non-finite delta.** `UpdatePointGesture` (`AuthoringSession.cs`) rejected non-finite `spanMeters`/`aftMeters` up front, then computed `rawEta = (spanMeters - selected.SpanMeters) / halfSpan` and `rawAft = (aftMeters - selected.AftMeters) * 1e6` and rounded them without checking whether the arithmetic itself had overflowed. A finite `double.MaxValue` target survived the input check, overflowed in the subtract/scale, and reached `PatchGesture`/the DSL patch encoder, which is where the guard actually fired (`DSL-PATCH`) — not the gesture clamp that was supposed to catch it. Red run: `docs/proof/m12b-b1b-red-runs.md` § "Gesture observability and overflow reds" (`FAIL UpdatePointGesture_BypassedClamp_DslPatch ContractError: DSL-PATCH`), fixed in `d1e8d45`.

**Class → sweep → derive → prevent:** signature: a finiteness/bounds check runs on the operands but not on the value the next step actually consumes. Derive: check `IsFinite` again on every derived quantity between the input check and the point it is used, and fall back to the last known-good state rather than continuing. Control: `AuthoringSession.cs`:373-375 adds `if (!double.IsFinite(rawEta) || !double.IsFinite(rawAft)) return LastFrame();` before rounding, where `LastFrame()` re-reads the selected point's current, already-committed position instead of propagating the overflowed delta. `UpdatePointGesture_BypassedClamp_DslPatch` (`tests/CfdWorkbench.Core.Tests/PointGestureTests.cs`:143-151) now drives both a `double.MaxValue` span and a `double.MaxValue` aft target through the public gesture API and requires each to report `Clamped == true`, then calls the now-`internal` `AuthoringSession.PatchGesture` directly with an out-of-order coordinate to prove the DSL-level backstop (`DSL-PATCH`) still refuses a bad patch even if a caller bypassed the gesture clamp entirely — defense in depth, not a replacement for the clamp fix. Sweep for a future track: any other derived-then-rounded quantity computed from two independently-checked inputs (scale, divide, subtract) before a downstream contract boundary.

**CONTROL-GAMED-BY-RENAME · A name-based retirement control was satisfied by renaming the thing it was meant to retire.** M1.2b design §8 retired the pre-shell rail editing: `RailEditorPane`, `BeginEdit(rail,id)`, `UpdateDraft(si)` and `PatchRail`, "once no caller remains". U2's control, `RailEditorPane_Removed_NoReferencesRemain`, grepped `src/` and `tests/` for those four tokens. U2 deleted the pane and renamed the API (`OpenControlDraft`, `ReviseOrdinate`, `RewriteControlOrdinate`), so the grep passed while the capability, the callers and the whole pre-shell window (`new MainWindow()`, reached only by tests; production always built `MainWindow(shellMode: true)`) remained. Signature: a retirement or absence check that matches identifiers instead of exercising the behaviour.

**Class → sweep → derive → prevent:** derive: a retirement control asks "can a caller still do it?", by construction and by signature, never "is the word still there?". Swept `tests/` for source-scanning controls: this was the only retirement grep. Three others are usage lints (`SelfLaunch.cs`: `Environment.ProcessPath` and the runtime repo-root walk; `Architecture_DockConfinedToShell`). They have the same weakness, since an alias evades a token match; they are recorded here as residual risk, not converted. Control: `RailEditorPane_Removed_NoReferencesRemain` (`tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs`) now (1) constructs `MainWindow` through every public constructor and every bool argument and requires the content to be `ShellHost`; (2) finds, by parameter signature, every public `(string, string, string)` opener and `(string, long, double)` reviser on `AuthoringSession` and every `(string, string)` opener and `(double)` reviser on `WorkbenchController`, invokes them on the Example, and fails if any pair revises one control ordinate; (3) resumes the golden M1.2a rail recovery and fails if any reviser changes it. Red on `2e1c569`: `MainWindow(False, null) builds Grid, not the shell; MainWindow() builds Grid, not the shell; AuthoringSession.BeginRailEdit then ReviseOrdinate revises one control ordinate; AuthoringSession.BeginPointGesture then ReviseOrdinate revises one control ordinate; WorkbenchController.OpenControlDraft then ReviseOrdinate revises one control ordinate; WorkbenchController.ReviseOrdinate revises a resumed rail recovery draft`. A rename cannot pass it. The pre-shell window's own controls retired with it, including NAV-STAR-COLLAPSE's `--theme-controls` matrix, whose subject (the pre-shell navigator) no longer exists; the gate's theme evidence is now the shell matrix `ThemeMatrix_ShellControls_AppliedContrast`.

**MAIN-PUSH-UNGATED · A push to main went out before the readiness receipt named its commit.** On 2026-10-01 the
Coordinator fast-forwarded `main` to `fa40f88` and pushed it, then read the AGENTS.md rule that a merge to main needs
`tools/run-readiness.py --check` green for that HEAD. The rule had arrived in the very merge being pushed; the session
had loaded the older AGENTS.md at grounding. The only receipt on the machine was red and named `2e1c569`. Readiness was
run after the push and came back GREEN for `fa40f88`, so no defect shipped, but the gate ran after the action it guards.

**Class → sweep → derive → prevent:** a gate that guards an outward action must run before it, from the tree being
pushed, not from the rules remembered at session start. Sweep: the only outward main action in the pack's flow is the
push; joins already run their fast ring through `run-verify-gates.py`. Control: `tools/hooks/pre-push` refuses a push
of `refs/heads/main` unless some worktree holds a green `.tmp-tests/readiness.json` naming the pushed commit (red-first:
refused `3b396e5`, accepted `fa40f88`, ignored a non-main ref). Installed in the shared hooks directory, so every
worktree is covered.

**UI-TRANSLATION-LOSS · An approved mockup's layout contract is not carried into the native build.** The approved v10
mockup drew Properties as one grid — label 56 px | input | unit 24 px, collapsible groups, right-aligned mono values
(`docs/mockups/workbench-v10.html` `.field`, `group()`). The M1.2b build stacks the Wing labels above their values and
lays point rows out as StackPanels whose input starts wherever its label ends (`PropertiesPane.axaml`:37-72, :75-120;
four different value x positions in `anchor-handle.png`). The operator called it "a bunch of text" (F-1). Nothing
failed: the native tests assert values and names, never the layout the operator approved.

**Class → sweep → derive → prevent:** signature: a rule the operator approved that exists only as CSS in a mockup (a
shared column, a unit column, a precision, a token width) with no native assertion and no Styles.axaml token. Sweep
(2026-10-01, `3b396e5`, Properties only): lost in translation — the shared label column, the unit column (Root and Tip
chord, Mean chord, MAC), the groups, derived-length precision 0.1 mm (UI-40; the build shows 0.01), Max t/c in % (the
build shows the ratio 0.12), "°" (the build shows "deg"). Not swept: Browser, Rail controls, the Plan view chrome —
Flagged. Derive: every layout rule the operator approves lands as a token or a named native test in the same change
that builds it. Controls: `tools/check-mockup-property-grid.mjs` "one label column, one value edge" and "every quantity
carries its unit" (controlled, mockup); `PropertiesPane_Rows_ShareOneLabelColumn` and
`PropertiesPane_EveryQuantityHasUnitOrIsDimensionless` proposed in `docs/reviews/ui-property-grid.md` §10 for the fix
track (uncontrolled until they land red-first).

**BLANK-ESTIMATE · A derived value that cannot be computed renders as a dash, with no reason, and takes its neighbours
with it.** `BindPointAndWing` (`PropertiesPane.axaml.cs`:217-224) writes "≈ —" into all five estimates when
`estimates is null || !Converged || !IsFinite(MacMeters)`: one non-finite MAC blanks Mean chord, Area and Aspect ratio
too, and the reason line shows only when `estimates` is not null — and then the raw `Compute.Outcome` string. D-4's
only visible symptom was "≈ —", which reads the same as "not computed yet".

**Class → sweep → derive → prevent:** signature: a placeholder glyph ("—", "≈ —", "Area: —") standing for a failed
computation. Sweep (2026-10-01): `PropertiesPane.axaml`:95, 99, 102-110, 118-119; `PropertiesPane.axaml.cs`:218-226;
`PropertiesView.cs`:134 — eleven sites, one owner. Derive: availability is per quantity; an unavailable value reads
"Unavailable" and its group states the reason in words (COPY-70, COPY-155), never an outcome code. Controls: the
mockup oracle's "no bare ≈ —" audit and its unavailable-state interaction (controlled, mockup);
`PropertiesPane_EstimatesUnavailable_ShowReasonNeverDash` and `PropertiesPane_OneNonFiniteEstimate_OthersStillShown`
proposed for the fix track with D-4 (uncontrolled until they land red-first).

**TEST-UNWIRED-EVENT · A test proves behaviour by counting an event that no production code listens to.** PGRID's
first build (`b052fe9`) raised the Properties pane's `Announced` event for COPY-160, the nudge value and errors, and
three tests (`PropertiesPane_Error_AnnouncedOncePerFailedCommit`, `FieldNudge_KeyUp_AnnouncesValueOnceInStatus`,
`Unavailable_AnnouncedInStatus`) asserted it was raised once. Nothing in `src/` subscribed, so no announcement reached
VoiceOver. The UX & Accessibility build review caught it (PG-26); the tests were green.

**Class → sweep → derive → prevent:** signature: an event declared in `src/` whose only `+=` subscribers are in
`tests/`. Sweep (2026-10-01, `7048fa1`): 17 events; besides `Announced`, `SectionCanvas.VertexSelected`, `VertexMoved`
and `FocusedTargetChanged` (also on `Viewport`) have test-only subscribers — the Section tab's editable canvas raises
edits nobody applies, which matches the operator's 30 Sep "Section tab doesn't seem functional"; wiring belongs to
M1.2c. `CanExecuteChanged` is an `ICommand` member the framework subscribes to. Derive: a test of an announcement
asserts the text change of an already-attached live-region element, never an event count (PGRID cycle 2 ported the
three tests this way). Control: `tools/check-event-subscribers.py` fails on any event declared in `src/` with no
subscriber in `src/`, except a named allow-list with reasons, and fails when an allow-list entry goes stale. Red-first:
it reports `Announced` at `b052fe9`; clean at `7048fa1`. Ring: every join (`docs/coordination/join.json` checks);
cost 0.5 s.

**READINESS-LATE · A track's change trips a readiness-ring gate that only runs after the join.** Twice on
2026-10-01 a track joined green and readiness then went red on the integration head: PL0's read-only source scans
(`Directory.EnumerateFiles`) tripped `verify-application-core.py` STORE-SUBSET (`b21d91c`), and PGRID's five new grid
brushes tripped `verify-application-adapters.py`'s fixed 23-key count (`9f23c68`). Both gates are skipped in the join's
fast ring by design (TEST-RING), so no track saw them; `main` held correctly, but each cost a fix branch and another
3.5-minute readiness run after the fact.

**Class → sweep → derive → prevent:** signature: a readiness gate that pins a count, a list or a pattern over files a
track owns. Sweep: `run-verify-gates.py`'s readiness-only gates are `verify-application-core.py` (STORE-SUBSET partition
and pattern) and `verify-application-adapters.py` (brush keys, contrast pairs, publish); both fired today. Derive: a
track whose diff touches `src/` or `tests/` runs `python3 tools/run-readiness.py` in its own worktree before its Return
and reports the receipt; the Coordinator's briefs carry that line. Control (partial): the adapters gate now derives its
key count from the named key sets, so a new brush is one set entry, not a magic number. Uncontrolled until a join
refuses a src/ branch without a green branch-tip receipt — recorded as the upgrade trigger: a third occurrence.

**RENDER-NOT-STYLE · A computed style asserted as if it were the rendering.** The structure-B mockup gave editable
values `text-decoration: underline dotted` (the non-colour editability cue, SC 1.4.1). The page audit read
`getComputedStyle(...).textDecorationLine === 'underline'` and passed. But Chrome does not draw an `<input>`'s text
underline when the input has an explicit height, so the capture showed no underline at all. Natively the same trap
exists one level down: Avalonia 11.3.14's `TextBox` has no `TextDecorations` property.

**Class → sweep → derive → prevent:** signature: an accessibility or look cue checked only through computed style or a
property's presence, never through what is painted. Sweep (2026-10-01): the property-grid oracle's cue checks — the
underline (found), ▾ via `::after` content (it renders), and the focus box via `box-shadow` (it renders). Derive: a cue
that carries a floor gets one render-level control. Controls: the page audit's "underlined values have no fixed
height" (a content-box proxy that goes red on a planted fixed height); the native test
`PropertiesPane_B_EditableValueHasDottedUnderline` asserts painted pixels, not a property
(`docs/reviews/ui-property-grid-cells.md` §5.3; uncontrolled natively until it lands red first).

**FOCUS-START · A keyboard check that starts focus where native focus never is.** NS-1 (operator, 2026-10-02):
natively, Tab from a clicked point walked 21 Plan targets and the three dock tabs, then reached the Wing before the
selection. `PropertiesPane_B_EveryEditableValueIsTabStop` passed because its walk focused the pane's first stop
directly, moved with `KeyboardNavigationHandler.Move`, and asserted reachability only. The walk never started where
the user's focus is, and it never saw the order.

**Class → sweep → derive → prevent:** signature: a keyboard-navigation test that puts focus inside the region under
test (`Focus(NavigationMethod.Tab)` on its first stop) and asserts a set, not a sequence. Sweep (2026-10-02,
`3b0545e`): both `TabWalk` callers (`EveryEditableValueIsTabStop`, `KeyboardWalk_NoTrap_KindBoxOneStop`) had the
signature. The other `Focus(NavigationMethod.Tab)` uses in the tests check a focused control's own behaviour, such as
its ring, its menu or its commit, not a walk. The sweep found four instances in the product, fixed in
`fix/native-ns1-ns3` with red lines in `docs/reviews/property-grid-native.md`: the Plan's Tab index ignored a pointer
focus, the Plan re-entry index was stale, the Wing came before the selection in tree order, and each tab was a Tab
stop. It found one sibling left open: `SectionCanvas` Tab wraps with no exit, and its test asserts the wrap. Derive:
a walk starts from the focus a real user would have at that moment (after the pointer action that leads there),
drives the Tab *key*, and asserts order as well as reach. Control: `TabWalk` now starts on the Plan at the selected
point and presses Tab as key events. `PropertiesPane_B_TabFromClickedPoint_ReachesValuesInOrder` asserts the resume
point, the stop count between regions, the top-to-bottom order and the re-entry, on the real `MainWindow`. It was red
first at `3b0545e`.

## 2026-10-03 — coordination, design-trace and proof-order classes

**JOIN-RESOURCE-RACE · A resource rule written as prose, with no shared signal.** The rule "no solver during a join" lived
in briefs. A join's 60 s test run (`tools/run-tests.sh`) collided with a 6-rank `snappyHexMesh` (wall 83 s, 1-minute load
76), so both the join's timing and the mesh were distorted.

**Class → sweep → derive → prevent:** signature: two actors compete for the machine and the only agreement between them is
a sentence. Sweep: the two launchers of heavy work are the join (`conductor-join.py`) and the solver wrapper
(`cases/tools/of-run.sh`); `of-run.sh` already waited on a lock file (Ruling 60) but nothing created it. Derive: one lock
file, one owner each side. Control (added): `tools/coordination/join-when-quiet.sh` takes the join lock first, waits for
the running job and for load < 10, runs the join, releases on exit; `of-run.sh` waits on the same
`<git common dir>/coord/join.lock` (override `CFDW_JOIN_LOCK` or `CFDW_COORD_DIR`). The lock comes first so a solver track
that starts jobs back to back cannot starve a join. Gap: a join started without the script still ignores the lock;
upgrade trigger is a second collision.

**PROC-MATCH-BY-CMDLINE · A process check by command-line text matches an unrelated shell.** A `pgrep -f
'snappyHexMesh|...'` guard matched another agent's wait loop whose command line merely named the solver, so the join waited
for the solver and the solver track waited for the join: a deadlock.

**Class → sweep → derive → prevent:** signature: `pgrep -f` or `pkill -f` used to ask "is program X running". Sweep
(2026-10-03) of `tools/` and `cases/tools/`: one hit, a comment in `tools/native-windows.swift` advising `pgrep -f`; it now
says `-x`. Derive: match the process name exactly. Control (added): `join-when-quiet.sh` uses `pgrep -x` over a name list,
and `tools/coordination/check-process-match.py` (fast ring, run by `tools/check-docs.py`) fails on any `pgrep|pkill -f`
under those two trees; observed red on a planted `pgrep -f x`.

**DELETE-WITHOUT-CALLERS · A design deletes an API and lists its replacements, not its callers.** Design seam S-3 named the
M1.1 writers to delete and two Desktop suites, but not the seven Core test files that call them. The CTL track stopped.
Design commit `747766c` amended S-3.

**Class → sweep → derive → prevent:** signature: a deletion or signature change whose surface list was written from the
definitions, not from a reference search. Derive: the surface list is the reference search result (E7). Sweep done: the
amended S-3 now lists the seven files. Control (prose-only gap): the design gate asks for the search command and its count
next to every delete; nothing fails mechanically if it is missing. Upgrade trigger: a second stop of this shape, then a
`design-slice` check that each "delete X" row cites a grep count.

**OWNERSHIP-MISSES-DATA-SOURCE · A promised visible behaviour is traced to its UI, not to the file that produces its data.**
Screen 2c promised a marker at the largest change; the data came from the refusal in `SectionEdits.cs`, which did not carry
the position, and no track owned that file. The EDT track stopped. Same family as DELETE-WITHOUT-CALLERS: the dispatch
list was incomplete for a reason the design could have shown.

**Class → sweep → derive → prevent:** signature: a behaviour in a mockup or design whose data crosses a file that no track
owns. Derive: before dispatch, trace each promised visible behaviour back to the file that produces its data, and put that
file in some track's ownership. Control (prose-only gap, recorded honestly): no coordination brief template exists in
`docs/ai-forward-pack/templates/` or `docs/coordination/` (the briefs are per-build plans), so the check has no row to live
in. It is a checklist item for the Coordinator's pre-dispatch step. Upgrade trigger: a third stop, then a `join.json`-style
ownership manifest with a check that every named data file has an owner.

**2026-10-05 recurrences (3 more; the upgrade trigger fired).** TGL stopped on the point-press path in `PlanCanvas` and
`ElevationView`, the `MainWindow` palette route, and the navbar keyboard-path and command-parity tests (the §18.5 rows
named `ShellMode` and `Selection`, whose readers are those files). LAY stopped because `LayerData` carried only a
`Legend` string and the batlow brushes were not in `Styles.axaml`. A read-only pre-dispatch trace agent run before LAY
and PNA still missed the `LayerData` payload. Each stop cost about 5 to 10 minutes. A trace agent is the same prose
control with a model in it: it reads the rows and finds the files the rows name, not the files the rows' data crosses.

Control (added, an aid and not a gate): `tools/trace-brief.py --design <design.md> --track <CODE> --owned <file>` reads
the design's Trace table and lists (1) each file in the track's rows' "Producing file" column that the brief's owned
list does not cover, and (2) each `src/` and `tests/` file that mentions a backticked type named in those rows'
"Data" column and is not owned. Exit 1 when anything is listed. `--self-test` rebuilds the TGL case from its rows and
is red when the brief omits the `Selection` reader, green when it lists it; `tools/check-docs.py` runs the self-test.
Measured on the real TGL rows with only the producing files owned: 48 candidates (8 producers, 40 readers), so the
output is noisy by design (`Selection`, `Geometry` are common words in the code); it names `PlanCanvas.cs`,
`ElevationView.cs`, `MainWindow.axaml.cs`, `ControllerShellTests.cs` and `ShellWindowTests.cs`, all five TGL stops. It
would not have named the LAY stop: row 13 says "layer data PRJ" with no backticked type, and a style resource is
not a type reference. Residual: the class stays open. Nothing can prove ownership from a table that names data
loosely. Derive: a trace row names its data as backticked type names, one per payload field group, so the script has
something to search (the design-slice convention to adopt for the next design; not yet applied to §18.5). Upgrade
trigger: a fourth stop, then the ownership manifest that `join.json` already models for test rings.

**SCANNER-TOO-BROAD · A retired-name scanner matches a bare word that a later design legitimately reuses.** The retirement
scan in `ShellWindowTests.cs` matched the bare word `SectionMode`, which blocked the M1.2c design's new type of that name.
Commit `8b07ca5` narrowed it to the old usage (attribute, bool property, viewport read), checked against both shapes.

**Class → sweep → derive → prevent:** signature: a "must not reappear" pattern made of a common identifier rather than the
retired usage. Sweep: the other names in that regex (`SamplesDocument`, `Plan3DContent`, `FoilViewport`, `ViewportProvenance`,
`FromInspection`) are specific to the retired surface; `3D samples` is a phrase, left as is. Other retirement tests in
`tests/` were not swept (not done today). Control (existing, per instance): the scan itself, now red on the old shape and
green on the new one. Related to CONTROL-GAMED-BY-RENAME from the other side: a retirement control asks about usage, not
spelling.

**INVERTED-SCALING-RULE · A design states which input binds a limit in the wrong direction.** Design line 446 said the
smallest chord sets the 10 um limit. The deviation in mm is normalised deviation times chord, so the largest chord binds.
The paired-mockup track found it. Fixed in `b200ebb` (design, mockup and review text).

**Class → sweep → derive → prevent:** signature: a "the smallest/largest X sets the limit" sentence with no worked number.
Derive: write the formula and one numeric example beside the sentence. Sweep: the fix commit touched the design, the mockup
and `docs/reviews/ui-m12c-paired.md`. Control (existing): SPT's test was renamed so the old wording would fail
(a name that states the rule); no repo-wide lint is possible for the sentence shape.

**HARNESS-STDIN-STALL · A background agent CLI waits on stdin for its whole limit.** A background `codex exec` blocked on
stdin for the full 2 h background limit and did no work, with no signal until the limit fired.

**Class → sweep → derive → prevent:** signature: a headless CLI started in the background without its stdin closed and
without an early progress check. Derive: close stdin and check output growth within minutes. Control (prose-only gap, to be
placed in the delegation recipe): start with `< /dev/null`, then confirm the output file grew within 3 minutes or kill and
report. No script in this repo launches those CLIs (swept `*.md` and `*.sh` for `codex exec`: none); the recipe lives in
the operator's memory notes, outside the repo.

**PAIRED-CLAIM-ONE-SIDE-TESTED · A claim about two surfaces is proven on one.** "Kind · both surfaces" passed SPTF because
the tests checked knots on both surfaces but the tangent row on one. PNL found it from a capture. Fix branch
`fix/m12c-paired-kind` (`29e9111` red tests, `6f0adf4` the writes) added both-surface tests and swept every per-side write in
`SectionEdits.cs` (set-tangent, to-anchor, to-control).

**Class → sweep → derive → prevent:** signature: a spec sentence with "both", "each side" or "paired" and a test that asserts
one side. Derive: the test enumerates the sides. Control (existing for the instance): the tests added in `29e9111`, red
first. Standing rule for new paired operations: assert the partner row for every write kind, not only the one the
operation names.

**CHECKER-PATTERN-GAP · A static checker's declaration pattern misses a legal syntax.** `tools/check-event-subscribers.py`
matched event declarations with a regex that had no parentheses, so a tuple-typed event was never checked. Fixed in
`41ea562`: 25 events are seen, it was 24.

**Class → sweep → derive → prevent:** signature: a checker whose recall is never reported. Derive: the checker prints how
many items it saw. Sweep: the checker now reports that count and the count rose by one. Control (existing): that count
is the visible check; the pin is not yet asserted (upgrade trigger: a miss of this kind again, then a floor on the count in
`join.json`).

**RED-FIRST-SKIPPED-UNDER-BOX · Code written before tests under a time box.** PNL wrote code before tests for 10 named
tests. Afterwards product mutants proved the tests (`docs/proof/m12c-pnl/red-first-receipt.md`), so the proof exists but the
order did not.

**Class → sweep → derive → prevent:** signature: a branch whose test names appear without a recorded red run. Derive: red
first is a property of history, which `tools/check-named-tests.py` cannot see (it checks names, not order). Control
(prose-only gap, after-the-fact substitute): a red-first receipt that names each test and the product mutant that turns it
red, as recorded for PNL. Proposed mechanical control: the track's Return lists the commit where each new test was red, and
a join script checks that a test-only commit precedes the first `src/` commit touching its subject. Not built today.

**JOIN-LOG-CONFLICT · Append-only logs and the derived index conflict at nearly every join.** Today's join tooling resolves
the case where only `docs/docs-index.js` and the two audit logs conflict. Control (added):
`tools/coordination/check-jsonl.py` fails a log that has a non-JSON line or a duplicate id after the conflict markers are
removed; `join-when-quiet.sh` runs it before it continues. Self-test fixture run: a good file passes (exit 0), a file with a
duplicate id fails (exit 1), a file with a bad line fails (exit 1).
*2026-10-08 (Mac leader 3122f106).* The markdown sibling: this register itself conflicted at 4 of 9 code joins (HRN,
WRT, NUM, WTH), because every track appends its entry at the end and `coord-register` only unions JSONL. Three were pure
tail appends. WTH also extended an existing entry in place, so a plain `merge=union` would silently keep both the old and
the new text of an edited claim; it is rejected. Control (built, track REG): `tools/merge-defect-register.py`, a register-aware
merge driver for this file (`.gitattributes` line `merge=defect-register`; registered per clone by
`tools/install-merge-drivers.sh`, used only by the Mac's joins since the PC never merges main). It auto-resolves only when
each side's changes are new whole entries or a strict extension of an existing entry, keeps every input line, and exits
non-zero (normal conflict markers) for any other case. Self-test (`--self-test`, in `tools/check-docs.py`, fast ring,
~0.05 s): both append, extend plus append, identical new entry resolve; two different edits, a deletion, a reorder and a
preamble edit on both sides conflict. Replay of the WTH join reproduces the leader's committed file byte for byte
(`docs/proof/reg/replay-wth.txt`). A clone without the registration falls back to git's normal merge.
*2026-10-08 (Ruling 169 join, track RG2).* The REG driver itself had this defect class: its conflict path exited 1 and left `%A` untouched, assuming git then writes markers. It does not; git keeps ours and marks the path conflicted, so `defect-classes.md` was UU with no markers and a routine `git add` would have dropped theirs. Signature: a tool's failure path assumed a caller behaviour without testing it. Control: the driver now writes `git merge-file -p` output (markers) into `%A` before exiting 1, and `--self-test` asserts on every conflict fixture both exit 1 and markers plus theirs' changed lines in `%A`, with a fixture for the PR #17 shape (both sides add different frontmatter links). Sweep: the pack drivers (`coord-core.py` `merge-derived`, `merge-register`) always exit 0 and write markers via `_write_conflict`, whose docstring records this hazard as S12b; no sibling found. Proof: `docs/proof/reg/` (RG2 section).
*2026-10-09 (DPR join, track RG3).* An entry-level "conflict" that a three-way line merge resolves: main and DPR each added a different dated line inside the DPI-A entry. The entry rules called that a conflict, `git merge-file -p` merged it cleanly, and the driver ignored the clean result and wrote a whole-file ours/theirs conflict (two 2,260-line copies); the leader resolved it by hand. Signature: a fallback that assumed "no conflict hunk" means "git cannot run". Control: the driver now defers to git when the entry rules give up. A clean merge is written and exits 0 when every line either side added or changed is kept; hunks are written with markers and exit 1; only a git failure writes the whole-file conflict. A base line one side removed may go, as git decides. Self-test fixture: both sides add a different dated line at different places in one entry. Replay of the DPR join exits 0 and reproduces the committed file byte for byte. Proof: `docs/proof/reg/` (RG3 section).
*2026-10-09 (track RG4).* The driver still stopped at the commonest parallel shape: two tracks each append one dated line at the end of one entry (FVT, ECR, V3D under DPI-A). Git reports one hunk with an empty base side, and the leader kept both lines by hand, ours first, at two joins. `tools/merge-defect-register.py` now reads each hunk (`git merge-file --diff3`): base side empty, both sides pure insertions with nothing in common, each a dated paragraph (first line `*YYYY-MM-DD (`, no blank line, no entry header) -> ours then theirs, no markers. Any other hunk keeps its markers and the driver exits 1; the conservation check stays. Replay of the ECR and V3D joins is byte-identical to the committed files (`docs/proof/reg/replay-rg4.txt`); the old driver exited 1 on both (`replay-rg4-old.txt`). Fixtures: `python3 tools/merge-defect-register.py --self-test` (15 of 15).
*2026-10-09 (track RG5).* The driver kept the content of an entry one side added but not its place: `merge()` appended every new entry after all base entries, so the MSP join moved REGISTER-CLASS-MISMATCH from before CFD-CLAIM-SCOPE to the end, and the CRD join then conflicted with CRD's in-place edit of it (a hand edit). A new entry is now inserted after the entry that preceded it on its own side (ours first at a shared anchor). Replay of the MSP join keeps the order; replay of the CRD join with the order kept merges clean and equals the hand-resolved file (`docs/proof/reg/red-first.md` RG5). Sweep: the other driver paths (`union_appends`, `three_way`) delegate placement to git and do not reorder.
*2026-10-09 (PR #28 join, Ruling 192 (1)).* The pack's JSONL register driver (`coord-core.py merge-register`) writes every merged row with `json.dumps(sort_keys=True)`, while `audit-log.py` appends rows with unsorted keys. So a merge where both sides changed `docs/audit/audit-log.jsonl` re-serializes main's existing lines, and the PR #28 branch also carried 4 re-serialized lines with its new entry out of order. The leader rebuilt the file as main's 913 lines byte for byte plus the one new entry appended, and checked the prefix with `cmp`. Append-only means the bytes hold, not just the ids. Pack finding for `/updatepack`: the driver should keep each side's original line bytes. Until then the leader rebuilds at each join that touches the log.

**REGISTER-CLASS-MISMATCH · A markdown file is bound to a JSONL register merge driver.**
On 2026-10-09 the PHN join merged `docs/notes/rulings.md`: PHN scrubbed Ruling 174, and main had added Ruling 182.
- `.gitattributes:7` binds the file to `merge=coord-register` (from `coord install`, 90cf9f94, 2026-10-05).
- That pack driver unions JSONL. It could not parse markdown, so it wrote a whole-file ours/theirs conflict, by design,
  and exited 0, also by design (S12b: "make the failure visible in the file").
- Git therefore created the merge commit with markers inside. A plain three-way merge would have been clean (different
  regions).
- `conductor-join` step 3 (`verify-no-conflict-markers.py`) stopped the join before any push. The leader rebuilt the
  file as main's copy with the scrub applied, verified it held 182 rulings, and continued.

Earlier joins never hit this, because PC branches do not add rulings, so only one side ever changed the file.

**Class → sweep → derive → prevent:**
- *Signature:* a file bound to a merge driver whose input format it does not have.
- *Sweep:* read each `merge=` line in `.gitattributes` and compare the bound file's format with its driver.
  `xmsg.jsonl` is JSONL (correct). Every other binding is to be checked by the follow-up.
- *Derive:* a driver binding is chosen by the file's format; markdown registers merge as `authored`, or through a
  driver that parses markdown.
- *Prevent:* the marker gate (step 3) caught it, which is the pack's intended net. The binding fix is track CRD: rebind
  `rulings.md`, plus a check that every `merge=coord-register` path parses as JSONL, red first on today's binding.
- *Source of the binding (track CRD):* `.agents/artifacts.yml` (repo-owned, below the managed-block end marker) classed
  the file `register`; `coord install` copies each register pattern into `.gitattributes`. Both lines are removed, so
  a re-run of `coord install` does not bring the binding back and the pack needs no change.
- *Sweep result:* the other `coord-register` paths all parse as JSONL; the `defect-register` path is markdown.
- *Control:* `tools/check-merge-bindings.py`, run by `tools/check-docs.py` (fast ring, about 0.1 s). Red on the old
  binding (`docs/notes/rulings.md: bound to merge=coord-register but line 1 is not JSON`), green after. Replay of the PHN
  merge with plain `git merge-file` exits 0 and equals the committed file by hash. Proof: `docs/proof/crd/`.
  Status: controlled (`tools/check-merge-bindings.py`).

**CFD-CLAIM-SCOPE · A label names a stronger quantity or cause than its data supports.** PRJ displayed `CL/CD` using
`CDi`, and attributed every e below 0.85 to a lattice effect even though physical washout can lower e at low CL.
Sweep: the Analysis projection's ratio, e, envelope and layer rows plus proposed COPY-217/232/233. Derive: a ratio's
denominator and a diagnostic's causal range travel in the same row as the value; missing total drag gives Unavailable.
Control: `Projection_TotalDragMissing_ClCdUnavailable`, `Projection_TrefftzLiftUsedForE`,
`Envelope_EOutOfBand_AdvisoryNotBlocking`, `Labels_EAboveOne_LatticeAttributionOnlyMeasuredBand` and
`Labels_VerifiedLattice_NamesFixtureScope` in ring A. Their red or planted-mutant failures are recorded in
`docs/proof/a3a-prj2/proof-pack.md`.

**CFD-RENUMBERED-GEOMETRY · A compacted strip id is mistaken for its original span edge.** The lattice numbers kept
strips by `kept.Count`; projection reconstructed widths from J. Sweep: CDi, Trefftz e, root bending, the loading curve,
strip detail and 3D lift arrows. Derive: carry original `ya/yb` with each stored strip and use them for every width
reader. Control: `Projection_ExcludedClosingTip_UsesKeptStripEdges` compares all width-dependent outputs with direct
integration after excluding a closing-tip strip; `Projection_LegacyStripEdges_OmittedAndHashIntact` guards expansion.
Older incomplete rows without edges report width-dependent outputs as Unavailable.

**CFD-ADVISORY-SILENT-OUTSIDE-CLAIM · An advisory that names a band stays silent outside that band.** The e note stated a lattice-bias cause only for 1 < e ≤ 1.02 at 64 × 4, and said nothing for e > 1.02 or for e > 1 on any other lattice. Sweep: `EAdvisory` and COPY-232/233/240. Derive: the measured-band sentence stays inside its band; outside it the note names the check and no cause. Control: `Labels_EAboveOne_LatticeAttributionOnlyMeasuredBand` (e = 1.03 at 64 × 4, and e = 1.01 at 32 × 4) expects COPY-240 and refuses "lattice bias" there. COPY-217's blanket "at the 64 × 4" is the same class as CFD-CLAIM-SCOPE: `Labels_VerifiedLattice_NamesFixtureScope` now requires each fixture's lattice (F-8 32 × 4, F-16 AR 5 4 × 1, F-18/F-19 32/64/128 × 4, F-6 32/64/128 × 4 cosine span uniform chord, F-21 16 × 4 per half).

**CFD-SPAN-EDGE-NAMED-INBOARD · A span edge called inboard is the lower-y edge.** Lattice stations increase in y, so the first edge of a strip is the lower-y edge; on a port strip that edge is outboard. Sweep: `StripLoad` edges and `AnalysisProjection.Width`. Derive: store `YLow`/`YHigh` (wire `yLow`/`yHigh`; no stored document had used `ya`/`yb`). Control: the `StripLoad` declaration states `YLow` < `YHigh` and the port fact; `Projection_LegacyStripEdges_OmittedAndHashIntact` requires those wire names when present and neither name when absent; `TipStrip_ExampleFoil_OutermostProvisional` requires `YHigh` > `YLow` on every produced strip.
**DLG-ROW-INDEX · A rendered row test treats position as identity.** The Section Source row made the existing
Own t/c test fail because it read `Rows[0]`. Sweep: the station and section row checks in `PointsPaneTests` and
`CatalogDialogTests`; the other checks use stable keys or labels. Derive: a test of one quantity selects its
stable row key and then asserts the value and surrounding state. Control: `Properties_SectionGroup_OwnTcAndPerStationTcConsequence`
now reads `sec:own` by key and passes with the Source row present.

**DLG-MODAL-AWAIT · A command sweep awaits a modal choice it never supplies.** Adding the catalog and save commands
made the status-strip command sweep stop after its earlier checks. Sweep: the section command table's three modal
entries (Import, Replace, Save); all remain in the availability/reason pass. Derive: the generic command sweep
does not execute modal rows; rendered dialog checks supply each choice and assert the result. Control:
`SectionCommands_EveryRow_RunsOrNamesReason` excludes those three from its execution loop and passes in the
`--status-strip --part=1/3` fast partition; DLG's dialog checks cover Replace and Save.

**HOOK-CWD-RELATIVE · A hook command names its script by a path relative to the shell's working directory.** On
2026-10-05 one Bash call ran `cd .claude/skills/execute-with-coordination`; the session cwd stayed there; every
PreToolUse hook then failed (`can't open file '.../skills/execute-with-coordination/docs/ai-forward-pack/hooks/mail-doorbell.py'`)
and blocked every tool call until the operator ran `cd <repo>`. Sweep: all four configs. `.claude/settings.json` (10)
and `.grok/hooks/ai-forward.json` (8) and `.github/hooks/ai-forward.json` (12 bash + 12 powershell) used the bare
form; `.agents/hooks.json` already used `$(git rev-parse --show-toplevel)/...`. The hook scripts themselves locate
the repo from cwd by walking parents (`coord-core.repo_root`), so they are subdirectory-safe; `session-start.py`
alone gates on `<cwd>/docs` and silently no-ops from a subdirectory (fail-open, not a crash; not changed here).
Derive: keep the relative path when it exists (so the hook still acts on the tree it runs in), else fall back to
`git rev-parse --show-toplevel` from the same cwd (same tree in a primary checkout and a linked worktree);
PowerShell gets the equivalent `Test-Path` / `Join-Path` form. Control: `tools/check-pack-hooks.py` now reads all four
configs and fails on any hook command that names `docs/ai-forward-pack/hooks/*.py` without the toplevel fallback;
planted bare command red, restored green. OPEN UPSTREAM: these configs are installed by the AI-Forward pack
(`docs/ai-forward-pack/hooks/copilot.ai-forward-hooks.json` and the pack's Claude/Grok templates), so the fix must
also land in the pack source or `/updatepack` reinstalls the bare form and the new control goes red.

**ANALYSIS-HARNESS-GROWTH · One harness's wall grows with every track, so a fixed per-harness limit fails on growth, not on a regression.**
C-2 (Analysis <= 5 s) failed at quiet load three times in a day: 5,386 ms (load 13), then 5,314 ms (load 14) after the
Analysis track had been trimmed once (B2) and the next tracks (CTX, S1, S4) each added checks. No check got slower; the
sum grew. A trim buys one track of headroom and the next track spends it, and re-basing the limit would only move the
failure. Sweep: the other single-process harnesses in the ring: Core and Desktop already run as parts (`--part=k/n`);
Cli is 1-2 s and has no growth path in the plan. Derive: a harness that every track appends to is partitioned, and its
limit applies per part, as DR-ANA-10 limits Desktop. Control (B4): `tests/CfdWorkbench.Analysis.Tests/AnalysisChecks.cs`
runs as `--part=k/n` (whole test classes, placed longest first onto the lighter part, so checks that share a fixture stay
together); `tools/run-tests.sh` runs `Analysis 1/2` and `Analysis 2/2` and its partition check fails when a part is missing or
the parts disagree on the group count; `tools/check-test-costs.py` applies C-2 to each Analysis part, and its self-test plants
a part over 5 s (red) and two parts of 4.9 s that sum over 5 s (green). Adding a test class means one line in the `groups`
array with its measured cost hint; a stale hint costs balance, never coverage. When a part nears 4 s, raise n (one number in
`jobs=`), do not re-base C-2.
2026-10-08 (ABL): the hints drifted within two days. Part 1 ran 4900 and 4936 ms against part 2 at 4065 and 3947 ms, and three
tracks failed C-2 on part 1; Projection was hinted 1180 ms and costs 29, SectionForce 780 and costs 297, SectionSeam 1040 and costs
1528. Re-hinted from three whole-harness runs (`docs/proof/abl/measure.md`): skew fell to 93-255 ms, parts 4550-4971 ms in the ring.
A group costs more cold than after the groups that warm it, so hints taken from a partition's own GROUP lines moved the layout and
went red twice; use the whole-harness median. Proposed control, not built (the skew needs both parts' times, and each part is a
separate process): `tools/check-test-costs.py` prints PARTITION-SKEW when |part 1 - part 2| > 15 % of the C-2 limit (750 ms),
from the `.ms` files `tools/run-tests.sh` already writes; it costs no run time. Still open: at ring load the two parts hold about
9.3 s, so 4.5 s per part needs n=3, which needs a ruling.
2026-10-09 (OBS): the skew control is built. `tools/check-test-costs.py` prints `PARTITION-SKEW <harness> parts=<ms list> skew_ms=<n> (hints stale?)` for
Core and Analysis when the slowest part minus the fastest exceeds 15 % of the per-part limit; it is advisory and never fails the ring. Its
self-test plants a balanced and a skewed set. Cost: five `.ms` reads, under 1 ms. The Core limit is a 30,000 ms reference (Core parts read 31.6 / 31.7 / 24.2 s: a real 7.5 s skew, not tuned here), not a ruled limit.
2026-10-09 (CBL): Core parts are now cost-placed, not round-robin: `tests/CfdWorkbench.Core.Tests/Fixtures/core-costs.tsv` (longest first onto the lightest part; an unlisted check keeps `i % n`). Parts read 29.3 / 29.7 / 30.3 s with no PARTITION-SKEW (was 31.6 / 31.6 / 21.9 s). The table goes stale as checks are added; the PARTITION-SKEW line is the detector, and regenerate is `python3 tests/CfdWorkbench.Core.Tests/Fixtures/core-costs.py <logs of CFD_CORE_COST=1 runs>` (`docs/proof/cbl/measure.md`).

**DESKTOP-HARNESS-GROWTH · A CPU-bound harness whose parts run concurrently cannot be partitioned into margin.** C-4 (Desktop <= 43 s)
read 42.5 / 43.5 / 42.8 s at quiet load (one red in three), then 45,636 ms at load 21.8 on the PNA join, after every UI track had added
checks. Sibling of ANALYSIS-HARNESS-GROWTH, whose control (partition, limit per part) worked for Analysis because its single process
was start-up bound. For Desktop it failed on measurement (docs/plans/test-cost.md 9.8, the sweep): two concurrent Desktop jobs read
43.8-46.5 s each against 42.5-43.5 s for one, because the ring is CPU-bound (500+ CPU-s on 16 cores) and the parts compete. Derive: where
the ring is CPU-bound, only less CPU lowers a clock; a partition moves the same work between concurrent jobs. Control (Ruling 99):
(1) Track B5 cuts Desktop CPU, starting with the heaviest children (plan-canvas, properties-view, shell-window), without dropping a
check or loosening an assertion, and returns C-4 to 43,000 ms; (2) growth is made countable: each UI track's join note reports its Desktop
child-seconds delta from `SUITE-TIME` (the sum over `.tmp-tests/Desktop.log`); (3) C-4 stays a load-gated tripwire (Ruling 87) with the limit
set only from a recorded 3-run quiet baseline (`docs/proof/ring-b4/baseline-desktop.csv`), never by an unrecorded move.

**ANA-UNUSED-DERIVATIVE-GATE · A panel estimate refuses valid cambered geometry because an unused endpoint derivative is singular.**
The imported NACA 2412 wing had a finite contour but a negative-infinite leading-edge `CamberSlope`; the section estimator
used panel coordinates, yet its old validation rejected the derivative. Sweep: `SectionEstimator` was the only section
consumer requiring finite camber slopes; `PanelMethod` uses coordinates. Derive: validate the inputs the selected method
actually reads. Control: `Section_CamberedWing129_WarmTime` runs all 129 stations of a 2 % cambered foil through the
section tier and requires finite panel results and a nonzero zero-lift angle.

**ANA-TIER-NUMBER-UNLABELLED · A rendered numerical row loses the method tier that qualifies it.**
Sweep: Section, polar, V_crit, profile Cd and drag-band projection rows. Derive: the data row owns a tier note that
the desktop table renders verbatim. Control: `Section_ProvisionalAndProjectionRows` scans numeric section/polar rows
and the panel-derived V_crit; `Polar_LowConfidence_AdvisoryReachesDragSums` checks the projected drag note.

**ANA-ADVISORY-LOST-IN-SUM · A numeric polar confidence warning disappears when Cd is summed into drag.**
Sweep: `StripCoupler.ProfileCd`, `Loads.ProfileDrag`, `Loads.WingDrag`, projection and tip consistency. Derive:
`StripValue.FlagCode` follows the value without refusing it; tip judgement and confidence are separate.
Control: `Polar_LowConfidence_AdvisoryReachesDragSums` and `Tip_ConfidenceNeverClearsNotJudged`.

**ANA-GOVERNING-STATION-TRANSFER · A pressure-resolution flag is assigned to a nearby strip and replaces its envelope verdict.**
Sweep: service strip mutation, section rows, V_crit and legacy strip display. Derive: the two-grid delta belongs to
the selected station's Cp_min/Cavitation result. Control: `Section_ProvisionalAndProjectionRows` checks the station
rows and preserves an outside legacy strip verdict with a separate note.

**ANA-EMPIRICAL-AXIS-OMITTED · An empirical factor is returned outside an untested source axis.**
Sweep: the A5.2 source [S6] axes h/c, Re, Fr_h and α, plus the Ncrit drag input. Derive: each exceeded axis
adds a reason code and suppresses the corrected number. Control: `FreeSurface_A52_EachEnvelopeAxis` tests each
boundary; `FreeSurface_A52_FactorsBesideDeepWater` checks the ordered drag band and omission codes.

**ANA-BAND-ASSUMED-ORDER · A two-method drag band uses input order rather than value order.**
Sweep: profile drag, wing drag and wing CL/CD projection. Derive: sort each band by numerical value after unit
conversion. Control: `DragBand_NcritValueOrderAndWingRatio` reverses the Ncrit drag ordering and checks both bands.

**ANA-TESTED-WORST-AS-BOUND · A tested-foil maximum is described as a universal pressure-error bound.**
Sweep: design §5.1 and its proof restatement. Derive: name the fixture set, keep the per-run two-grid measurement,
and state the reviewer's 6.9–7.7 % 6 %-thick finding separately. Control: the Ruling 90 proof now records both
measurements and `Section_ProvisionalAndProjectionRows` makes a measured >10 % run provisional.

## 2026-10-05 round — merge bindings, brief fixtures, shell shapes, load flakes

**DERIVED-UNBOUND · A pattern is classified as generated or append-only but no merge driver is bound to it.**
`.agents/artifacts.yml` classified 8 patterns; `.gitattributes` bound 3. `docs/docs-index.js`, `change-log.jsonl`,
`rulings.md` and the ledgers therefore took the default text merge and conflicted on nearly every join on 2026-10-05.
`coord doctor` read "merge driver effective" because it checks that the drivers are registered in git config, not that
each classified pattern has a `merge=` line. `coord install` bound the rest (commit `90cf9f94`).

**Class → sweep → derive → prevent:** signature: a registry of what each artifact is, and a second file that must
mirror it, with only the driver checked. Sweep: the other registries that need a binding are `.agents/artifacts.yml`
(8 of 8 now bound) and `docs/coordination/join.json` (covered by TEST-RING). Derive: the binding is a function of the
classification, so the check recomputes it. Control (added): `tools/check-artifact-bindings.py`, run by
`tools/check-docs.py`, fails when a `derived` pattern lacks `<pattern> merge=coord-regen`, a `register` pattern lacks
`<pattern> merge=coord-register`, or a pattern is bound to the other driver. Self-test red on a missing line and on a
wrong driver; red on the real `.gitattributes` with the `docs/docs-index.js` line removed, green restored. OPEN
UPSTREAM: `coord doctor` should verify per-pattern binding; the pack is not edited here.

**BRIEF-FIXTURE-AGAINST-SPEC · A repair brief asks for a test the spec forbids, and the coder widens the code to pass it.**
The E2 repair brief asked for a FoilDSL test with a `tangents` block before `ids`. `docs/specs/foildsl.md:194` says a
`tangents` block is legal only in 4.1, after `ids`. The coder widened the parser grammar so the test passed.

**Class → sweep → derive → prevent:** signature: a brief or fixture written from memory of the grammar, and a
green test used as the proof that the grammar is right. Sweep: the grammar lives in one place, the `Grammar` class of
`src/CfdWorkbench.Core/FoilSource.cs`; no other parser reads FoilDSL. Derive: a grammar change and its spec
belong in one branch, or a ruling must say why they differ. Control (added): `tools/check-foildsl-spec-sync.py`, run by
`tools/check-docs.py`, fails a branch (merge-base with `main` to HEAD) that changes a line in the `Grammar` class
without changing `docs/specs/foildsl.md` and without `Ruling <n>` in a commit message. Self-test builds five
throw-away repos: red on a bare grammar change; green for a spec change, a cited ruling, a change outside `Grammar`, and
an unrelated branch. Residual: it cannot see a wrong spec edit, only a missing one; and a widened parser inside another
file would escape it (the sweep found none).

**AGENT-HEREDOC · An agent uses a shell heredoc to write a program, against a rule in its brief.**
Sub-agents used heredocs at least 7 times on 2026-10-05 although every brief named CT27 ("a multi-line program is a
file, then a run, never a heredoc"; the shape measured at 70 s). The coordinator used heredocs in the same session.
Prose in the brief did not change the behaviour, so the rule is a memoir (CI6).

**Class → sweep → derive → prevent:** signature: a behaviour rule that agents must follow on every call, enforced only
by being written down. Sweep: other CT27 shapes (a gate behind a pipe, a sub-agent calling `EnterWorktree`) have the
same property; the profiler counts all three, nothing refuses them. Derive: the refusal belongs at the tool seam.
Control (installed, Ruling 104, 2026-10-06): a PreToolUse hook on `Bash`, `tools/hooks/no-heredoc.py`, wired in
`.claude/settings.json`; its `--self-test` runs in `check-docs.py` and `check-pack-hooks.py` requires the toplevel
fallback for `tools/hooks/`. Proposal: `docs/proof/round-oct05-lessons/heredoc-hook-proposal.md`. Open: whether
repo-level hooks fire for sub-agent Bash calls is measured after the join, in a new session (hooks load at session
start); until then the class stays open for sub-agents. Not covered: Grok, Copilot and agy payloads.

**SECTION-EDITOR-LOAD-FLAKE · One failed check in the shared-fixture Section Editor suite leaves a gesture pressed and cascades.**
`SectionEditor_DragMove_DrawsWithinOneFrame` failed at least 4 times on 2026-10-05 at load 40 to 200, and passed alone
each time; the earlier note in `docs/proof/ring-b2/profile.md` saw 6 checks fail with one cascading `DSL-DRAFT-OWNED`.
The same load-gate family as the Ruling 81 frame budgets. The instance's own cause is Inferred (CPU starvation lets
something notify or draw between moves); the failure text of the four runs was not kept.

**Class → sweep → derive → prevent:** signature: a check that presses a pointer on a shared fixture and asserts before
it releases. A failing assertion skips the release, the controller keeps the draft, and every later `fixture.Reset()` check
fails with `DSL-DRAFT-OWNED`, so one flake reads as six failures. Sweep: `SectionEditorTests.cs` has the one
press-assert-release shape on a shared fixture in this check; the Fixture-per-check ones (`SlowAssessment`) discard the
fixture on failure. Derive: release in `finally`. Control (added, test-side): `SectionEditor_DragMove_DrawsWithinOneFrame`
releases the pointer when the drag assertions throw. Red first with a planted throw at move 3, run as
`--section-editor --part=2/2` with a non-symlinked `TMPDIR`: the planted failure plus 5 `DSL-DRAFT-OWNED` cascades
before the fix; the planted failure alone after. The fix does not stop the first failure at load. Proposed, not done:
gate that check's functional assertions the Ruling 81 way (READINESS-MISS above load 24) only if its failure text,
captured at load, shows a timing cause; capture the message first, because a gate over an unknown cause hides a real
regression. Class stays open on that point.

*Recurrence (MHY, 2026-10-07).* The same check, `SectionEditor_DragMove_DrawsWithinOneFrame`, failed again in the two
ring-headroom passes (2026-10-06 and 2026-10-07, per the track brief): `docs/proof/ring-split/moved-2.md:57` records one
failure in the ring run of ring headroom 2 (load 8 to 17.6, passes alone) and "it failed once in the earlier pass too".
That is 2 more instances, 6 or more in all. The instance's cause is still Inferred, and the failure text was still not
kept in the records above (the `STACK` line from `c9a06fad` prints one for a failed Desktop check; `moved-2.md` does not quote it). The class
stays open. Next step unchanged and now cheaper to justify: on the next failure save the check's failure line and the load,
and decide on the READINESS-MISS gate from that text, not from the count.

*Investigation (trk-flk, 2026-10-08), `docs/proof/flk/investigation.md`.* No failure in about 90 runs up to load 157 (alone at 64 hogs, part 2/2 at 100 hogs, 8 concurrent suites); CPU starvation is not supported by the data (the field failures were at load 8 to 17.6). The stale-step hypothesis was refuted with a forced late step; the surface-projection one is untested because the fixture probably never asks for a surface. The cause is unknown. Control (added, test-side): both counting checks (`SectionEditor_DragMove_DrawsWithinOneFrame`, `SectionEditor_NudgeRun_NoShellRefreshPerKey`) use `NotifyProbe`, which puts the event, draft generation, `CheckAccess()` and the notifier's stack into the FAIL line; "notified" and "applied a step" are separate messages. Red first (`docs/proof/flk/red-first.md`): a planted `Select` post between moves fails the check and the stack names `PlantedSelectPost`. The next real failure in any ring names its notifier; decide the fix (product guard, fixture drain, or oracle) from that line. Class stays open.

*Root cause (trk-fss, 2026-10-08), `docs/proof/fss/red-first.md`.* Two field failures (run 1a at load 14; the RGM join ring at start load 2.3) named the notifier: `WorkbenchController.CompleteSurface` (`:888`) called `Notify` from `RunSurfaceAsync`'s continuation while `Fixture.Move` pumped the dispatcher. The mesh job was queued by `Fixture.Reset` (section entry), before `Press`: a test-isolation defect, not a product one. **"Load" was only the amplifier**; the cause is an in-flight background completion inside a measured window. Control (added, test-side): `Fixture.SettleSurface()` before the window in both `NotifyProbe` checks, plus `SectionEditor_DragMove_HeldSurfaceDrainedBeforeThePress`, which holds the mesh seam so the completion lands between two moves. Red first: without the drain it fails with the field stack (`CompleteSurface` <- `RunSurfaceAsync` <- `Fixture.Move`, Move 2); with it, it passes, no load needed. Frequency: 0 of 12 before, 0 of 12 after at load 2 to 4, so the rate is below 1 in 12 and the deterministic check is the proof. Class generalised: **a check that counts notifications (or applied steps) over a window drains background work first.** Sweep: only the two `NotifyProbe` sites assert a zero count on a shared fixture; the `ControllerViewTests` counters have no wanted surface or gate their own. Proposed, not done: a guard in `tools/check-docs.py` that fails a test file with a `NotifyProbe` or a `Changed +=` counter and no `Settle`/drain call in the same check; cost is a regex pass, false positives are likely (handlers that wait on an event), so propose it to the operator before building. Class closed on the cause; the `READINESS-MISS` gate question is withdrawn (no timing oracle was involved).

**JOIN-CHECK-BEFORE-REGEN · The join checks the tree before it pays the regeneration the merge driver deferred.**
After `coord install` bound `docs/docs-index.js` to `merge=coord-regen` (commit `90cf9f94`), the POL and CI joins
both stopped at step 4 with `validate: 1 index-drift item(s)` (CI: `file not in index:
proof-round-oct05-heredoc-hook-proposal`; POL's item was not captured). `coord-core.py merge-derived` resolves a
derived file to ours and records the regeneration as owed; `conductor-join.py` pays it at step 6, after the step-4
checks, so any branch that adds a docs node while main also moved fails its first check. Correction: the coordinator
first attributed this to the driver regenerating against a half-written tree; reading `cmd_merge_derived` showed the
driver never regenerates.

**Class → sweep → derive → prevent:** signature: a deferred side effect (owed work) and a reader that runs before the
step that pays it. Sweep: the only owed work in the join is `coord regen`; the derived patterns are the 8 in
`.agents/artifacts.yml`, and `check-docs.py` (docs-graph validate) is the step-4 reader that sees them. Derive: the
first check must pay the debt. Control (added): `docs/coordination/join.json` `checks` now opens with
`coord-core.py regen`, and the TEST-RING guard in `tools/check-docs.py` fails when `regenerate` is set and the first
check is not `coord-core.py regen` (red before the `join.json` edit, green after). OPEN UPSTREAM: `conductor-join.py`
should run the regeneration before its checks; the pack is not edited here.

## 2026-10-06 round — copy ids, wall-clock asserts, raw codes, parallel load, brief premises, ring capacity

**COPY-ID-COLLISION · Two parallel tracks append the "next" id to one numbered table.**
DX and GRP each added a `DESIGN.md` §7 row numbered COPY-394. The merge conflict caught it; a union-merged table
(`merge=union`, or a regenerated view) would have kept both rows and one id would have meant two sentences.

**Class → sweep → derive → prevent:** signature: a sequence number taken by "last id + 1" in a file two branches edit.
Sweep: `DESIGN.md` has 384 `| COPY-<n> |` rows with no repeated id today (observed). Other `docs/` notes repeat ids such
as COPY-08 or COPY-103, but each note numbers its own local rows, so they are out of scope. Rulings and audit ids are
timestamps or one-writer registers. Derive: an id must be unique per file, and a parallel track must not choose it.
Control (added): `tools/check-copy-ids.py`, run by `tools/check-docs.py` (fast ring, no test code), fails on a repeated
row id in `DESIGN.md`. Self-test red on a planted duplicate; red on the real file with COPY-394 appended a second time;
green restored. Rule: a parallel track takes copy ids only at the join, or from a range the Coordinator hands out in its
brief. Residual: the lint sees a duplicate only after the merge, so it backs up the rule and does not replace it.

**WALLCLOCK-ASSERT-UNGATED · A test throws on a wall-clock budget with no load gate.**
`Section_WingRun_PanelValuesAtEveryStation` throws when a warm run takes over 1 s. GRP's ring failed on it at load
47-71, while the frame checks fail only at a quiet load (Rulings 81, 84: `DesktopChecks.RequireFrameBudget`, or print
`READINESS-MISS` above load 24).

**Class → sweep → derive → prevent:** signature: a duration compared with a literal, inside a check that fails, with no
read of the machine load. A loaded run is not evidence about the code. Sweep (grep of `tests/**/*.cs` for `Stopwatch`,
`Elapsed`, `TotalMilliseconds`, `TotalSeconds`, `GetElapsedTime`, then the comparisons that throw): three ungated
instances, listed as tracked debt, not fixed here: `tests/CfdWorkbench.Analysis.Tests/SectionSeamTests.cs:298` (1 s,
the GRP failure), `tests/CfdWorkbench.Desktop.Tests/AnalysisToggleTests.cs:34` (p95 over 250 ms),
`tests/CfdWorkbench.Desktop.Tests/PlanCanvasTests.cs:1312` (median over 8 ms). The other timing sites either gate
(`RequireFrameBudget`, `READINESS-MISS`: `View3dTests.cs:746`, `PlanCanvasTests.cs:1330`, `SectionEditorTests.cs:797`,
`:875`, `SectionDraftTests.cs:386`), only print `MEASURE`/`COST`/`OBSERVED`, or are hang guards that throw
`TimeoutException`. Derive: the gate is the existing Ruling 81 helper. Control (added): `tools/check-wallclock-asserts.py`,
run by `tools/check-docs.py`, fails on a new ungated assertion, and on an allowlist entry whose line is gone, so the debt
list only shrinks (keyed by file and line text, not line number). Self-test red on a planted ungated assertion, on a
planted one through a variable, green on a gated one, a `READINESS-MISS` print, a `TimeoutException` guard and a bare
`MEASURE`; red on the real tree with the allowlist emptied (3 findings), green with it. Residual: a text heuristic, not a
parser; a budget computed in a helper whose name does not end in `Milliseconds`, `Ms` or `Seconds` can escape it. The fix
for the three instances is a later track (`RequireFrameBudget` or `READINESS-MISS`; delete each allowlist entry).

**RAW-CODE-ON-SCREEN · An internal code reaches a display cell.**
Internal `ANA-*` codes reached display cells from CPY's eight reason rows and from the strip coupling; the operator sees a
code, not a sentence.

**Class → sweep → derive → prevent:** signature: a display string that falls back to, or is built from, a machine code.
Control (exists, DX, readiness ring): `Projection_NoRawAnaCodeInAnyCell` (`DxSectionTests.cs:58`) builds one fixture per
registered `Labels.ReasonTexts` code and scans every cell, note, legend and table for the raw code. Sweep of the other
code families (src has `ANA` 263, `DOC` 269, `DSL` 448 string literals; no `SRC-`, `GEO-` family in `src/`): the scan
covers `ANA-` only. `DSL-` and `DOC-` codes reach the screen at these sites: `PointsView.cs:219` (`error.Reason ?? error.Code`),
`WorkbenchController.cs:123` (`item.Reason` empty, then `item.Code`), `:1197` (`?? result.Code`), `:2381`
(`error.Reason ?? "{Code}: ..."`), `SaveSectionDialog.axaml.cs:81` (`({error.Code})` in a sentence), and the
"`<code>: <sentence>`" status prefix at `WorkbenchController.cs:438, 2125, 2141, 2648` and `MainWindow.axaml.cs:124`. The
prefix is a product convention with a sentence beside it, so only a code standing alone is the defect; the fallbacks at
`:123`, `:1197` and `PointsView.cs:219` can produce one. Not fixed (product code). Proposal for the Coordinator: extend
`NoRawCode` over the `DSL-` and `DOC-` families the same way (one fixture per registered code); that is a test-code
change outside this track.

**PARALLEL-BUILD-LOAD · The Coordinator runs more build tracks than the machine holds.**
Three build tracks ran at once. Their builds and ad-hoc test runs sit outside the ring lock (`tools/ring-lock.sh` caps
only full rings) and drove the 1-minute load to 47-77: load-only failures in GRP and PNL, and one C-3 join stop.

**Class → sweep → derive → prevent:** signature: a capacity rule held only by the Coordinator's judgement. Sweep: the
ring lock covers rings; builds and single-check runs have no signal (the same property as JOIN-RESOURCE-RACE and the
solver wrapper). Derive: the signal is the 1-minute load and the count of running build tracks. Control (added): rule in
`docs/plans/test-cost.md` §9.10 (at most 2 build tracks at once; no dispatch while load is over 24) and
`tools/dispatch-gate.py [--running <n>]`, which prints `GO load=<n>` or `WAIT load=<n> <reason>` (exit 1) and prints
`WAIT load=not-recorded` when the load cannot be read. Its self-test runs in `tools/check-docs.py` (red on over-cap, over
24, and unreadable load; green at 24). Residual: it is a tool the Coordinator runs, not a refusal at the dispatch seam, so
it is a memoir until a skill or hook calls it (cf. AGENT-HEREDOC). OPEN: wire it into the `execute-with-coordination`
dispatch step (pack file, not edited here).

**BRIEF-PREMISE-UNCHECKED · A brief states a fact about the code or the product that nobody opened.** Extends
BRIEF-FIXTURE-AGAINST-SPEC (a brief asserting a grammar fact it had not read). Two instances this round: the DX brief
said "the run's section stations" were a few (the mockup shows four); the product samples every lattice strip (126 on the
example), so the Stations table was 3,030 px tall (`docs/proof/dx/red-first.md`, repair cycle 2, red `expected 3; actual
42`). The GRP brief assumed Core exposed the binding point; it did not, which is why GRP Core came first (the round plan
orders it so; no proof file records the premise itself, so the second instance is stated from the plan, Inferred).

**Class → sweep → derive → prevent:** signature: a sentence in a brief with a number or an "exposed/owned by" claim and no
file:line. Sweep: both instances are one shape, a premise about a data source. Would `tools/trace-brief.py` have caught
them? Partly. It reads a design's Trace table (visible behaviour, data, producing file, owner) and flags producing files and
type mentions the brief does not own, so the GRP premise (the producer of the binding point is in Core) is the shape it
catches, provided the design's Trace table names that producer. It does not read counts, so the DX premise (4 versus 126)
escapes it; that needs the proof to read the run, which the DX repair did. Derive: a brief premise is a claim; mark it
`assume:` (NG rule) or cite the file. No new control: a second lint over free prose would flag every number in a brief
(CI9 cost). Rule added: a brief states a count or an ownership claim only with a file:line or an `assume:` marker, and
the Coordinator runs `trace-brief.py` on any brief whose track reads data it does not own.

**RING-AT-BUDGET (capacity, not a defect).** The fast ring's net time (C-3, limit 50.0 s) rose from about 47 s to about
49.5 s in one round across 5 joins, and 11 checks moved to readiness to fit. Measured series, options and costs:
`docs/proof/round-oct06-lessons/ring-at-budget.md`. The operator decides.

Third instance (2026-10-08, Ruling 143, `docs/proof/ring-oct08/moves.md`): after the TCV join the net was 47.5 s on a quiet
machine and 50.6 s under load (C-3 red), Analysis parts 4.9 s against C-2's 5 s. 24 checks moved to readiness (19 Desktop, 5
Analysis); net 43.2-43.9 s, Analysis 3.8-4.2 s, PASS union unchanged (1811), readiness 137.7 s to 141.6 s. Two findings. First,
moving 22 Desktop checks (about 35 s of COST) cut the Desktop wall by only 2-3 s; re-ordering the spawn list (`--analysis`, 17 s,
was last and set the wall) cut about 4 s more. A check's COST is not its share of the wall, so the first move is the spawn order,
and only then the check. Second, a check can be a gate's evidence (ThemeMatrix feeds `verify-application-adapters.py`): moving
it needed its own mode, not a silent removal.
What would stop a fourth (proposal only, no control built): (1) a join-time headroom gate that fails when the net is within
2 s of C-3 or an Analysis part within 0.4 s of C-2 at end load <= 24, so the move happens one join early and not at the cap;
(2) `check-test-costs.py` flags a spawn list whose last-started suite is longer than the median suite (the order rule, now prose
in `WorkbenchTests.cs`, becomes a check); (3) AGENTS.md already says a new check states its ring and cost, but nothing reads that
statement, so a lint that a new `Check(` in a fast-ring `Run` carries a ring comment would only add ceremony; do not build it.

**MOCKUP-STATE-UNBUILT · An operator-approved mockup state was never built, and the build passed its named checks and the capture review.**
Instance: DX state 5 (`docs/design/dx-screen-states.md` row 5, "Section view with Cp on the profile, vik pinned at 0, Cp_min marker")
was approved at Ruling 108 and shipped as a plain z/c line chart with a Cp comb; the Section document later moved to the main
area (SMA) with the same chart. The DX proof (`docs/proof/dx/red-first.md`, item 6) listed seven required states and captured
each, but the profile was not one of the seven. SMA's `captures.md` did say "the profile has no colour ramp", as a difference
and not as a missing state, and the review read it as acceptable. Built as track CPV (Ruling 126). Cause: the proof's state
table is written from the brief, not from the approved mockup, so a state the brief did not list cannot fail it.

**Class → sweep → derive → prevent:** signature: a proof table whose rows come from the brief's list and not from the mockup's
states. Sweep (done, bounded): `dx-screen-states.md` has 54 rows. Rows 1 to 25 (the Section tab, Cp, estimator and cavitation
screen) were checked against `src/`: every approved string in COPY-293 to COPY-312 is in code (COPY-298 and COPY-299 by their
text in `Labels.cs`, not by id), and row 5 is the only state found drawn wrongly, now built. The seven states in the DX proof
table all have captures (`01` to `11`). Rows 26 to 54 (polar, transition, bucket, drag, Find alpha) were not swept state by
state; captures `03`, `04`, `05`, `09`, `10` cover the polar, transition, bucket and Find alpha screens, the drag rows only by
`06` and `11`. So "any other state claimed but not built" is: none found in rows 1 to 25; rows 26 to 54 not verified
(Inferred as built; OPEN). Derive: the check that would have failed is "every approved mockup state maps to an app capture or
to `not built` with a ruling". Control: before a UI join the track's `captures.md` carries a state-by-state table, one row per
approved mockup state (taken from the mockup, not the brief), each row an app capture or "not built, Ruling n". Could
`tools/` check that the table exists? Only half: a `check-docs.py` lint could require that any `docs/proof/<track>/captures.md`
which cites a mockup also contains a table with a `not built` or `.png` cell per row, but it cannot know the mockup's state
list, because the mockups hold their states as code (`STATES` objects in the HTML), not as a manifest. A real check needs a
`docs/mockups/<name>.states.json` manifest per approved mockup plus the lint that every id appears in the proof table; that is
more than a check-docs lint, so it is proposed, not built. Until then the Coordinator asks for the table at join review.

## 2026-10-07 round — Mac hygiene after the PC reviews (MHY)

**ROUTE-PARENT-MISSING · An external command is invoked before the destination parent the route owns exists.**
The PC's W-3 stopped at the two-repair cap: `wsl.exe --import` was given a destination under `<app-data>\wsl\`, and the
run directory and fixture cache sat under paths nothing had created. Ruling 133 classed it: the design (§3.2 step 4) named an
import destination with no step that creates its parent, and §6 named no creator for the run directory. The extraction slip
was the same class in execution.

**Class → sweep → derive → prevent:** signature: a route step whose argv names a path, and no earlier step or sentence that
creates and checks that path's parent. Sweep (design only; the PC runs the route): `docs/design/guided-solver-setup.md`
§3.2 steps 4, 5 (files written through `\\wsl.localhost\...\etc\apt\`) and 8, and the §6 run directory. The macOS routes
(§3.1) install an app bundle and have no owned destination that a command must find. Derive: before every external
command the route creates and verifies each destination parent it owns, names who creates the run directory (the route),
and tests the negative path. Control (added, design): the owned-parent rule in §3.2 and the §6 run-directory paragraph,
amended in this track and marked Ruling 133. Residual: the control is design text until the route is built; the build track
must turn it into a negative-path test (missing parent: created, or a clean refusal with a next step), and the PC's W-3
re-entry is the first observation. Class stays open until that test exists.

**PUSH-SUCCESS-BY-TEXT · A push is declared done because its output contains a ref line.**
The Coordinator's retry loop grepped `main -> main`. That text also appears in `! [remote rejected] main -> main (Internal
Server Error)`, so a rejected push read as success and the loop stopped.

**Class → sweep → derive → prevent:** signature: success judged by matching text in a command's output. Derive: the remote
is the authority; a push is done only when `git ls-remote origin refs/heads/main` equals the local SHA. Sweep: the
`pre-push` hook (`tools/hooks/pre-push`) runs before the push and checks only the readiness receipt for the pushed SHA,
so it cannot see the outcome and is not changed. A grep of `tools/` and `docs/coordination/` for `main -> main` found no
other consumer. Control (added): `tools/verify-push.sh [branch] [remote] [sha]` prints `PUSH-OK <sha>` (exit 0) only on
equality, `PUSH-NOT-DONE` (exit 1) otherwise, `PUSH-UNKNOWN` (exit 2) when the remote cannot be read. Its `--self-test` plants
a rejected-push output that contains `main -> main`, and requires red on an unpushed commit, green once pushed, red on a
stale remote, and exit 2 on an unreadable remote (observed OK). Rule: the Coordinator runs it after every push to main
and treats nothing else as proof. Residual: it is a tool the Coordinator runs, not a refusal at a seam, and its self-test
is not in `tools/check-docs.py` because that gate also runs on Windows where bash may be absent; run
`tools/verify-push.sh --self-test` when the script changes. Not covered: a push that succeeds and is then force-overwritten.

**STDIN-HANG · A bare `python3 -` in a chained command waits on stdin until the timeout.**
A Coordinator command chain held a stray `python3 -` (no heredoc, no pipe, no redirect). The interpreter read standard
input, nothing wrote to it, and the call ran to its 600 s timeout, so no later step ran.

**Class → sweep → derive → prevent:** signature: an interpreter or filter (`python3 -`, `python -`, `node -`, `cat -`,
`bash -s`, `sh -s`) whose stdin is the terminal. A heredoc-adjacent shape: the same author intent as `python3 - <<EOF`
(a program in the command) with the program left out. Could the existing hook catch it? Checked
(`tools/hooks/no-heredoc.py`): no. Its `START` pattern needs `<<` plus a delimiter line, so a bare `python3 -` passes
(fail-open by design). Derive: refuse the command when one of those words ends a pipeline stage that has no `|` before it and
no `<`/`<<`/`<<<` after it. A pipe into it is legitimate (`curl ... | python3 -`), so the rule must read the stage, not
the substring. Proposal (not installed; the hook belongs to the operator's decision, as Ruling 104 did for heredocs): add
`has_bare_stdin_reader(command)` to `no-heredoc.py` with the same fail-open contract, a self-test row for each of
`ls; python3 -`, `cat -`, `x && python3 - arg` (all refused) and `echo 1 | python3 -`, `python3 - < f.py`, `python3 -c "print(1)"`
(allowed), and the same reason text, "a program is a file, then a run". Until installed, the rule is the existing CT27 no-heredoc
rule read for this shape: never start an interpreter with `-` unless its stdin is named on that stage. Class stays open.

**MUTANT-RESTORE-CHECKOUT · Restoring a planted mutant with `git checkout -- <file>` discards uncommitted work in that file.**
In the SFV build a mutant was planted in a source file that also held uncommitted work, and `git checkout -- <file>` restored
the mutant's file to HEAD, dropping that work.

**Class → sweep → derive → prevent:** signature: a mutation step whose undo is a checkout of the whole file, run on a tree that is
not committed. Sweep: the proof files that record mutants (`docs/proof/*/red-first.md`) use planted-then-restored edits; the
safe ones undo with an inverse edit or run on a committed file. No other instance is known (not swept file by file).
Derive: the undo for a mutant is a pure function of the committed state only if the file was committed first. Rule (added):
commit before planting a mutant; restore with `git checkout -- <file>` only after that commit, and never on a file with
other uncommitted edits. This track followed it (the gate mutant in `docs/proof/mhy/red-first.md` was planted after commit
`a7d77839`). No tool control: a check would have to know which edits are mutants, and the rule costs one commit. It is a
rule stated here, for the Coordinator to put in the next round's common brief (the round-oct06 `common.md` does not carry it); repeat on a second instance and add a `tools/` helper that
refuses to plant on a dirty file.

**WINDOWS-TEXT-MODE-HASH · A file is written in text mode and hashed from its on-disk bytes.**
The PC hashed CRLF bytes from its checkout, while the committed blobs are LF (PR #4, `docs/reviews/pr-4.md`): the recorded hash
matched no committed input. On Windows, `write_text` and `open(..., "w")` translate `\n` to `\r\n` unless `newline="\n"` is
given.

**Class → sweep → derive → prevent:** signature: text-mode write, then `read_bytes()` or `rb` hashing of the same file, with the hash
recorded or compared against a committed one. Derive: a hash of a committed input is of the committed bytes, so write with
`newline="\n"` (or write bytes). Sweep (`tools/`, `cases/tools/`; grep of every file that imports `hashlib` or calls `sha256sum`
for writes without `newline=`): clean: `tools/verify-application-adapters.py`, `tools/qualify-windows-runtime.py` (every
`write_text` has `newline="\n"`; the others write bytes). Affected, Mac-run generators that write OpenFOAM dictionaries with
`write_text` and then record `sha256((run / r).read_bytes())` in `cfdw-manifest.json`: `cases/tools/make-tip-bl-gmsh.py`
(329 then 427), `make-tip-coupon-gmsh.py` (286, 384), `make-wing-gmsh.py` (209, 307), `make-wing-r2.py` (37, 371),
`make-tip-bl-snappy.py` (37, 364), `make-tmr-case.py` (133-151, 312), and `cases/tools/test-foam-dict-lint.py` (54-55, `open(p, "w")`
then `rb` hash). `make-wing-case.py:33` writes dictionaries with `write_text` too but records only the STL hash (bytes). All are
run on the Mac today, where `\n` is unchanged, so the recorded hashes are the committed ones; the defect appears only when one
runs on Windows. **Fixed (track trk-tmh, round-oct06):** `newline="\n"` is on every text-mode write in the seven generators,
`make-wing-case.py` and `test-foam-dict-lint.py`. `tools/check-neuralfoil-weights.py`, `check-notices.py`,
`coord-stream-summary.py` and `recount-application-contracts.py` write no file (stdout, or bytes), so they need no fix.
**Control built:** `tools/check-text-mode-hash.py` (AST scan of `tools/**/*.py` and `cases/tools/**/*.py`: a file that uses
`hashlib` or `sha256sum` and calls `write_text(` or a text-mode `open` without `newline=` fails; the allowlist carries a reason
per entry and a stale entry also fails, so it only shrinks). It runs in `tools/check-docs.py` (`run_lesson_controls`, self-test
then scan), fast ring, measured 0.25 s wall for the scan. Red first: on the pre-fix tree it named all eight files
(`docs/proof/tmh/red-first.md`). One entry stays: `cases/tools/launcher-record.py`, which writes only its own record files and
reads them back as text (reason in the allowlist). No hash drift on the Mac: `make-tmr-case.py` before and after produced
byte-identical trees and manifests.

**MOCKUP-PHYSICS-UNCHECKED · An approved mockup carries a physics error, and the review of the mockup does not find it.**
The force-vector mockup (approved at Ruling 130) drew V∞ mirrored (falling to the right at a positive angle, lift leaning aft)
and converted the Imperial moment per span with the force-per-length factor (0.737562 instead of 0.224809). The hydrodynamicist's
review of the mockup did not catch either; the build did (`docs/proof/sfv/captures.md:63-64`, `docs/proof/sfv/red-first.md`
mutants for both).

**Class → sweep → derive → prevent:** signature: a mockup whose drawing or number encodes a sign or a unit factor that nobody
re-derived, because the review reads the picture. Derive: such a mockup states its conventions as checkable numbers (V∞ angle and
the screen direction it implies, each unit factor with its source quantity), and the build re-derives each from the model, not
from the mockup's script. Control (rule, with a proof trail): a physics-bearing mockup carries a "conventions" table: quantity,
sign or direction, unit factor, expected value for one worked example; the build's red-first file contains one row per entry,
each a test that fails on the mirrored or mis-scaled variant (the SFV mutants are the pattern). Sweep (bounded): the other
physics-bearing mockups are the ones with drawn flow or force quantities; none other has a hydrodynamicist review on record
that this track could check, so the sweep is not done (OPEN). No lint: a table's presence is checkable, its correctness is not,
and the correctness check is the build's re-derivation. Persona: the hydrodynamicist reviews the table's numbers, not only the
picture.

**JSON-NEWLINE-PLATFORM · An indented JSON writer takes `Environment.NewLine`, so its bytes differ on Windows.**
System.Text.Json with `WriteIndented = true` (or `Utf8JsonWriter` with `Indented = true`) separates lines with `Environment.NewLine`, which is `\r\n` on Windows. The
layout, recent-list, display-preference and native-project images are hashed, compared and saved, so a Windows build wrote different bytes from the committed fixtures
(PR #6 review, `docs/reviews/pr-6.md`; the Core layout and recent-list failures in `docs/proof/win-smoke-reverify/receipt.md`). Sibling of WINDOWS-TEXT-MODE-HASH.

**Class → sweep → derive → prevent:** signature: a JSON writer that indents and whose bytes leave the process. Derive: pin `NewLine = "\n"` on every such
options instance. Sweep (`grep -rn "Indented = true" src`): `LayoutCodec.cs`, `RecentList.cs` (source-generation attributes), `PreferenceStore.cs` (`Utf8JsonWriter`),
`AuthoringSession.cs` (`NativeProject.Options`), `Cli/Program.cs`: all five pinned; the one test copy that rebuilds a writer (`RunStoreTests.NoRunByteIdentical`) pinned too.
Control: `LayoutCodec_IndentedJson_PinsLfNewLine` (Core tests, `LayoutFileTests.cs`, ring cost under 0.1 s) fails any `src/**/*.cs` file that indents JSON without a
`NewLine = "\n"` in it, and asserts the pinned context options and the serialized images hold no `\r`. `Environment.NewLine` cannot be forced on macOS, so the
scan is the part that fails on the old code. Residual: a file with two options instances, one pinned, passes the scan.

**PROOF-PII · A proof file records the Windows account name or a machine SID.**
Receipts, logs and JSON outputs copied from a Windows run carried `C:\Users\<name>` home paths (and, on PR #8, a machine SID in
`red-native.stdout.txt`) into the committed tree: 29 tracked files before the scrub (`docs/proof/pii/red-run.txt`). A proof is written to be
read by others, so a tool's raw output is not fit to commit as it came. Operator decision 2026-10-08 (Ruling 145 (5)): scrub the current tree
and fail new offenders. Git history is **not** rewritten and nothing is force-pushed, so old commits still hold the name.

**Class → sweep → derive → prevent:** signature: a tracked text file with a Windows home path (backslash, JSON-escaped, `C:/Users/<name>`,
`/mnt/c/Users/<name>`) or a SID `S-1-5-21-<n>-<n>-<n>`. Sweep: `python3 tools/check-proof-pii.py` over `git ls-files` (29 files, 0 in the
append-only registers); `docs/proof/win-routes/manifest.json` was refreshed for the 15 rows whose bytes changed. Derive: write `%USERPROFILE%`
(Windows), `$HOME` (WSL), `<name>`, and `S-1-5-21-<machine>-<RID>`; a script derives its path at run time from `USERPROFILE`. Control:
`tools/check-proof-pii.py` (`--self-test` plants every form, a SID, and a macOS home path that must not fire), run by `tools/check-docs.py`
(fast ring, about 1 s); a shrink-only allowlist carries a reason per entry (one: the pack's own fixture with a dummy name). Residual: the
guard sees a name only in a path or SID shape, and history is unchanged.

Ruling 174/181 instance (2026-10-09): no entry for hostnames existed, so this line extends PROOF-PII. Ruling 174 scrubbed a raw PC hostname from a PR, but the guard matched only user paths and SIDs, so the class had no control (Ruling 181 (5)). `tools/check-proof-pii.py` now also fails a Windows default machine name, `COMPUTERNAME=<name>`, a `MachineName` JSON value, a `systeminfo` `Host Name:` line and a UNC host prefix, prints hits masked, and reads an optional uncommitted `CFD_PII_HOSTNAMES` list; self-test fixtures use invented names (`docs/proof/pii/red-first.md`, trk-phn). Residual: a hostname that is not a default name and appears outside those shapes is seen only through the local list.

Ruling 165 instance (2026-10-08): an unpushed local audit entry included a concrete Windows account path. The correction reset that entry before any push and re-appended it with `%USERPROFILE%` at write time. The new audit line contains no account path; prior shared history was not rewritten.

*2026-10-09 (PHN join, UNION-REVIVES-SCRUB).* A scrub of a union-merged register comes undone at the merge.
- PHN replaced the hostname in `docs/coordination/xmsg.jsonl` line 64.
- The union driver kept main's unscrubbed copy beside the scrubbed one, so the literal was back on the merge result.
- The leader caught it by sweeping with the literal in an environment variable, removed the duplicate before
  `--continue`, and swept again (0 hits).

*Signature:* an edit to an existing line of a file whose merge driver unions lines; both versions survive. *Derive:* a
scrub of a union register is re-swept on the merge result, and every branch that holds the register scrubs it too (the
PC was told, xmsg 2026-10-09T16:12:55Z). *Control:* `check-proof-pii.py` with `CFD_PII_HOSTNAMES` set, run at every join
that touches a register. The leader keeps the literal outside the repo. Status: controlled for the leader's joins; PC
branches pending its scrub.

*2026-10-10 (Ruling 197, PII-GATE-SKIPS-UTF16).* The guard was blind to a file encoding.
- `check-proof-pii.py:137-138` skips any file with a NUL byte in its first 8 KiB, treating it as binary.
- Windows tools such as `wsl.exe --version` and `--list` write UTF-16LE with no BOM, which is NUL-patterned. So PR #29's
  `wsl-version.stdout.txt` and `wsl-list.stdout.txt` were never scanned.
- The reviewer decoded both and found 0 hits: clean, but by hand, not by the gate.

*Signature:* a PII or text gate whose binary heuristic also excludes a text encoding the platform emits. *Derive:* a
NUL-patterned file with the UTF-16LE shape (NUL in every odd byte) is decoded as UTF-16 and scanned, not skipped. A
genuinely binary file stays skipped, and the gate names it. *Control:* pending a Mac tools track, red first on a synthetic
UTF-16LE offender built at run time.

**READER-SHARE-DELETE · A reader opened without delete sharing blocks a POSIX replace on Windows.**
A product reader that opens a user file with `FileShare.Read` (or through `File.ReadAllBytes*`, `File.OpenRead`, which share Read only) holds a handle
the Windows save cannot replace under: the handle-relative rename fails with NativeFailure Win32 32, NTSTATUS 0xC0000043. A `FILE_SHARE_READ |
FILE_SHARE_DELETE` reader keeps its old bytes while the replace succeeds (Ruling 145 (1)). macOS ignores share modes, so no Mac test sees it.

**Class → sweep → derive → prevent:** signature: a `src/**/*.cs` line with `FileShare.Read` not followed by `| FileShare.Delete`, or a `File.ReadAll*`/`OpenRead`/`OpenText`
call. Sweep: three sites (`ShellHost.ImportDatAsync`, `Cli.ReadFoilBoundedAsync`, `SectionLibrary.ScanCore`), now all on `UserFile.OpenRead`; `PaneDiagnostics` is an append
writer and out of scope. Derive: one opener, `CfdWorkbench.Persistence.UserFile`, owns the flags. Control: `tools/check-reader-sharing.py` (`--self-test` plants nine
offenders and eight clean forms incl. writers), run by `tools/check-docs.py` (fast ring, 0.3 s); behaviour test `Library_UserFileHeldReader_SurvivesReplaceByRename` (red on Windows
with the old flags only). Residual: a reader built from `StreamReader(path)` or a new API outside the pattern list passes the scan. **2026-10-09 (WRB, Ruling 167 (a)):** on the PC the behaviour test threw `UnauthorizedAccessException` because it replaced the file with `File.Move(..., overwrite: true)`, which is `MoveFileEx` on Windows, not the product's replace. The test now replaces through the product primitive on Windows (`WindowsNative.Rename(source, parent, name, replace: true)`, handle-relative POSIX-semantics rename; `File.Move` stays on macOS). Not provable red on a Mac; the next PC ring must show `Library_UserFileHeldReader_SurvivesReplaceByRename` PASS (`docs/proof/wrb/red-first.md`).

**TEST-TMP-ALIAS · A Desktop test builds its scratch path from the raw system temp directory, and the store refuses it.** The production store refuses a path with a symlinked component (`DOC-UNSUPPORTED-PERSISTENCE`). macOS's default TMPDIR is `/var/folders/...`, under the `/var` link. `tools/run-tests.sh` exports a non-symlinked TMPDIR, so the ring was green while the same harness run alone through `tools/run-suite.sh` failed `SaveDialog_EmptyOrDuplicate_ErrorFocusStays` (DOC-UNSUPPORTED-PERSISTENCE) and `SaveDialog_Save_LiveRegionFocusToSectionMenu`; `--section-editor --part=2/2` alone showed both, part 1/2 passed. Four helpers had each patched the alias by hand with a different prefix list (`/tmp/`, `/var/`), and 20 sites took `Path.GetTempPath()` raw. **The handoff's second defect, "SaveDialog checks fail when one part runs alone", was not a second defect:** with a non-symlinked TMPDIR every SaveDialog check passes in part 2/2 alone, in part 1/2 alone and one by one (`CFD_TEST_ONLY`); the checks set up their own library root and read nothing from a sibling part. They sit in part 2 only because of the registration-index partition. **Class → sweep → derive → prevent:** signature: a Desktop test that takes `Path.GetTempPath()` or `Path.GetTempFileName()` directly. Sweep: 20 sites in 8 files fixed (`WorkbenchTests`, `ControllerSection`, `ControllerShell`, `ShellWindow`, `StatusStrip`, `PropertiesCells`, `CatalogDialog`, `AnalysisPanel`); the same shape in Core, Analysis and Cli tests is reported, not fixed (their harnesses were not run alone here). Derive: `TestTemp` (`tests/CfdWorkbench.Desktop.Tests/TestTemp.cs`) resolves every link in the root, so one definition replaces the six hand-patched ones; production still refuses links. Control: `SelfLaunchTests.NoRawTempPath` (fast ring, ~10 ms) fails on any Desktop test file but `TestTemp.cs` that names the raw call; it was red on 8 files before the change (`docs/proof/hrn/red-first.md`). Residual: the scan sees the call by name, not a path built another way; for the part-alone claim there is no structural rule, only the alone-run evidence. **2026-10-08 (WTH):** the sweep HRN reported is done. Run alone with the default macOS TMPDIR, the Core harness gave 43 FAIL (DOC-UNSUPPORTED-PERSISTENCE) and the Cli harness aborted; 10 raw sites (7 Core, 3 Cli) now go through a per-project `TestTemp` class, and both harnesses pass alone (Core 722 PASS, Cli 6 PASS). `NoRawTempPath` scans the Desktop, Core and Cli test projects; it was red on 5 Core/Cli files (`docs/proof/wth/red-first.md`).

**RAW-ERROR-CODE-AS-CAUSE · A DOC-/DSL- error code is the whole cause on a product surface.**
`Labels.SaveFailed(code)` printed "Save failed: DOC-IO — ...", the uncertain save led with `{result.Code}: Save was not acknowledged`, and the edit and source refusals led with
`{code}: ...` (Ruling 148 found 67 distinct codes in 32 files; 133 save and refusal texts failed the control on main). A person cannot act on a code. The sibling class
RAW-CODE-ON-SCREEN covered ANA- codes in analysis cells only; this one is the DOC-/DSL- codes on the save strip, the open alert and the refusal fallbacks.

**Class → sweep → derive → prevent:** signature: a `$"{code}: ..."` or `SaveFailed(code)` shape, or a status text that is the code plus a generic tail. Sweep: every `"(DOC|DSL)-..."` literal
in `src/` (cause-rows inventory, Ruling 155); 20 rows COPY-412 to COPY-431. Derive: one lookup, `Labels.SaveRefusal` / `Labels.Refusal` / `Labels.OpenCodeLine`, takes a code and returns a plain
cause with the code only inside "(code)" (open: on a second "Code:" line); an unknown code gets the generic row. Control: `Labels_NoRawDocDslCodeAloneOnAProductSurface` (Analysis fast ring)
and `Copy_OpenFailure_EveryCodeInSrc_PlainSentenceThenCodeLine_Ruling155` (Desktop) read the literals from `src/` at test time, so a new code is covered the day it lands
(red-first: `docs/proof/cwr/red-first.md`). Residual: a surface that formats a code without calling the lookup is not seen (the status lines at WorkbenchController `{error.Code}: Uncertain save remains dirty` and the section-draft `{assessment.Code}: ...` still lead with a code and a sentence); the CLI keeps raw codes by ruling. **2026-10-08 (STL):** the residual is closed for five sites. Ruling 158 gave the save-retry and section-draft status lines plain copy (COPY-432 to 438, `Labels.ReadBackFailed` / `RetryNotConfirmed` / `DiskChangedAfterSave` / `DraftUnavailable`); the control `Labels_NoStatusLineLeadsWithACode_Ruling158` (no exceptions after Ruling 159) scans `src/` for `$"{x.Code}: ..."` (also after a `...Prefix()`), red on 4 lines on main (`docs/proof/stl/red-first.md`). The fifth, `{error.Code}: Geometry display unavailable` (COPY-439, Ruling 159), was listed for one turn and is now wired and unlisted.

**WRT-HARNESS-ABORT · A test that throws at the top level of a harness hides every later check.** The Desktop default run is top-level statements. `Save_As` over an existing file threw `Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE` on Windows, and the throw ended the process at stage `native-review-options`: the PC's ring (Ruling 154 finding 5) reported one crash and left the theme matrix and all 18 spawned suites unassessed, and the receipt did not say so. A listed-expected failure has the same blind spot the other way: a manifest entry that is never checked for staleness keeps excusing a test after it starts passing. **Class → sweep → derive → prevent:** signature: a bare `throw` in the top-level flow of `WorkbenchTests.cs` (or a CLI harness `Main`) that a host-dependent store answer can reach. Sweep: the Desktop flow has more such throws; on Windows the next one is `Durable recovery save did not settle the unchanged visible draft`, and `Cli_AnalyseRunKey_EqualsServiceOnCustomOp` aborts the CLI harness; both are reported, not fixed here (the CLI file is not this track's). Derive: a host-dependent assertion runs as `DesktopChecks.Check`, which prints one `FAIL` line and goes on, and the default run folds `DesktopChecks.FailureCount` into its exit code. Control: red-first through the `storeFactory` seam (a store answering the code aborts the old harness and gives one FAIL plus 697 later PASS lines, `docs/proof/wrt/red-first.md`); `tools/check-expected-failures.py` reports an abort not listed with `aborts_harness` as unexpected, and `UNEXPECTED-PASS` when a listed test passes, so the manifest shrinks when W-2 B2 lands (`--self-test` in `tools/check-docs.py`, 0.22 s). Residual: other top-level throws stay until each is converted; the manifest sees only what a harness log prints. **2026-10-08 (WTH):** the two reported aborts are converted. The recovery block (`Recovery_SaveReopenResume_KeepsDraftSeparateFromAccepted`) is a `DesktopChecks.Check`; a planted real-store refusal now gives one FAIL and the prefix runs on (678 PASS after it). The five CLI catch blocks print `FAIL <name> <type>: <message>` and count instead of rethrowing, and the CLI exit is 1 when any failed. `Cli_InspectRuns_RevisionIsSessionLabel` was hidden behind the same abort and is now listed (Inferred until the next PC ring).

**NUM-RESIDUE · Round-off residue is displayed as a significant number.**
A quantity that is zero by construction comes out of floating-point arithmetic as 1e-15, and a three-significant-figure formatter prints it as a measurement: the nc = 1 strip table read "M′ c/4 (lattice) 0.00000000000000355 N·m/m" (the lattice vortex sits at c/4, so on a straight c/4 line with linear elevation and camber endpoints on the chord, as on the Example foil, the c/4 couple is zero; a curved or swept c/4 line leaves a geometric residue the floor does not hide; x_cp is already undefined below nc = 2). Track NUM measured the residue at 1.2e-17 of q c² on the Example foil. The same track fixed a sibling in the same string, "1 chordwise panels".

**Class → sweep → derive → prevent:** signature: a `Sig3` (or any significant-figure) call on a signed physical quantity that can be identically zero by construction. Sweep: of the `Sig3` callers, the couple was the proven case; `LiftLabel`/`LiftRow` (zero lift on a symmetric section at alpha 0) and `InducedDragLabel`/`InducedDragRow` (Gamma near 0) can show the same residue and are reported, not changed (docs/proof/num/red-first.md). Derive: one helper, `Labels.CoupleValue`, owns the floor (1e-9 of q c², marked `assume:`) for the label and the table row. Control: `SectionForce_OneChordwisePanel_CoupleIsRoundOff_ShownAsZero_TableAndProfile_TrackNum` (fast ring, about 0.4 s) runs the Example foil at nc = 1 and fails on the old code; `SectionForce_FourChordwisePanels_CoupleUnchanged_TrackNum` pins the default. Residual: a quantity other than the couple is not covered until it gets the same helper.

2026-10-08 follow-up done (track NCR, Ruling 161): the CFD review asked that a structurally unresolvable quantity not print as a number at all. At nc < 2 x_cp/c and M′ c/4 read "Not resolved · 1 chordwise panel" (COPY-SF18 to SF21), the profile draws no couple glyph, and one predicate, `SectionForces.IsMomentResolved`, owns the rule for every surface. Control: `NotResolved_OneChordwisePanel_TableValuesReadRuling161_LabelsDropBias` (Analysis, fast ring) and `SectionProfile_OneChordwisePanel_NoCoupleGlyph_LabelReadsNotResolved_FourPanelsDrawsIt_Ruling161` (Desktop, `--analysis`); both failed on the old code (docs/proof/ncr/red-first.md).

**TEST-FOREIGN-OS-BRANCH · A test runs another OS's branch on the host.** `PrefStore_DirectoryLink_WindowsBranchUsesJunction` called `TryDirectoryLink(made, real, windows: false, ...)` on any host. On Windows that is `Directory.CreateSymbolicLink`, which needs a privilege, and the check failed with "A required privilege is not held" (Ruling 154 condition, `docs/reviews/pr-12.md`). The test selected the branch with a literal, so the host's own platform never decided what ran. **Class → sweep → derive → prevent:** signature: a test passes a literal `windows: true` or `windows: false` to a platform branch with no `OperatingSystem.Is*` line in the same method. Sweep: one test (two call sites) in `tests/`. Derive: each half runs only on its own host and the other prints `NOT ASSESSED`, the way the directory-link helper already did. Control: `SelfLaunchTests.NoForeignOsBranchWithoutHostGuard` (fast ring, ~15 ms) fails on any test file that has such a literal without a guard; it was red on `PreferenceStoreTests.cs:71` and `:77` before the change (`docs/proof/wth/red-first.md`). Residual: the scan sees the literal argument name `windows:` and a guard anywhere earlier in the method; a differently named flag, or a guard for an unrelated branch, passes it. The macOS run no longer checks the shape of the Windows `mklink` command (that half is Windows-only by the ruling). 2026-10-08 sibling shape (Ruling 176, track TMI): a test path literal valid on one OS only. `Telemetry_MarkerInjection_AbsentEverywhere` copied to `"win\\" + marker`, one file name on macOS but a missing `win` folder on Windows (`DirectoryNotFoundException`, PR #18). Fixed by `CopyMarkerFiles(..., backslashIsSeparator)`: the separator shape uses `Path.Combine(root, "win", name)` and creates the folder, and `Telemetry_MarkerPaths_SeparatorShapeCreatesFolder` runs that shape on the Mac (`docs/proof/tmi/red-first.md`). Derive: build paths from `Path.Combine` segments, or name a backslash case as one OS's.

**SWALLOWED-CAUSE · A catch returns null and the event that depends on the value records nothing about why.** `SectionReplace.CatalogFamilies` caught `ContractError` from `Catalog.Load()` and returned null, so the `catalog.preview` event carried an empty family with no reason; on the Windows ring `Replace_Preview_EmitsCatalogPreviewOutcome` read "expected Naca, got empty" and the CAT-UNAVAILABLE cause stayed hidden (`docs/proof/cat/investigation.md` section 5; Ruling 156 (3)). **Class → sweep → derive → prevent:** signature: `catch (ContractError) { return null; }` (or a bare default) on a value an event or log later reads. Sweep: the catch sites in `src/CfdWorkbench.Core` that feed a recorded field are listed in `docs/proof/srs/red-first.md` as not swept (the record-write-path sweep is a separate item). Derive: the event gets an optional `FamilyRefusal` (code, check, detail) beside `Family`; the failure stays cached because the embedded catalog cannot change in a process. Control: `Replace_Preview_CatalogUnavailable_EventRecordsRefusal` plants a failing loader and asserts the field (red on main: `docs/proof/srs/red-first.md`). Residual: other swallow sites are not yet scanned. (2026-10-08)

**RAW-CAPTURE-BLOB-MISMATCH · A raw evidence capture is normalized or otherwise changed between capture and delivery.** PR #13 records commit `1f2cc9f8` normalizing `bits.stdout.txt` from 13,584 bytes to 13,499 before `b5c6b52e` restored the CRLF capture; payload lines stayed identical, but the committed file no longer represented the captured bytes (`docs/reviews/pr-13.md`, Ruling 163). **Class → sweep → derive → prevent:** signature: the receipt's file hash or byte count differs from the committed Git blob. Sweep: every captured stream and suite log in `docs/proof/r163-windows-ring/` is listed in `capture-manifest.json`. Derive: file identity is SHA-256 and byte count of `git show HEAD:<path>`, not the working-tree copy. Control: `verify-captures.py` fails when either committed-blob value differs from the receipt-declared manifest; the proof directory's `.gitattributes` uses `-text` for raw captures. Delivery requires this check after commit. **2026-10-09 (WRB, Ruling 167):** the class control is now general: `tools/check-capture-manifests.py` checks every `docs/proof/*/capture-manifest.json` in HEAD (SHA-256 and byte count of `git show HEAD:<path>`; a path outside the manifest's folder or an uncommitted capture also fails), run by `tools/check-docs.py` (fast ring; `--self-test` about 1.3 s, real run 1.1 s, 14 planted cases incl. a byte count off by one and a working-tree edit after commit; it also reads `docs/proof/*/closing-manifest.json`, e.g. `win-naca` with 119 entries, leader note on the PR #17 review). Residual: a manifest not yet in HEAD is checked only after it is committed.

**CRT-GOLDEN · A bit golden on a C-runtime transcendental is a platform-keyed authority.**
.NET forwards `Math.Sin/Cos/Atan/Atan2/Pow/Exp/Log` to the platform C runtime (Apple libm, UCRT, glibc), which need not agree in the last bit, so a SHA-256, a committed `.dat`, or a bit golden taken from such a value is true on the machine that recorded it and false on another. The Windows ring found it as `entry naca-0009: first differing byte 727`: one ulp of `Math.Cos(Math.PI*i/80.0)` at i = 63 (docs/proof/cat/investigation.md, Ruling 154 class d, Ruling 156).

**Class → sweep → derive → prevent:** signature: a recorded byte, hash or `!=` compare whose value passes through a banned call. Sweep: `Catalog.cs` (9 call sites, 4 functions) and `Placement.cs` (3 sites) are the instances that feed a committed golden; the record-write sites (`SectionReplace.cs:327`, `DatImport.cs`, `ConstrainedFit.cs`, `FoilSource.cs:654`) are a separate track. Derive: the generator path uses only `+ - * / sqrt` and the managed `double.CosPi`/`SinCosPi`, whose 81-value bit golden (`Fixtures/catalog/spacing-cospi-80.tsv`) pins the runtime. Control: `tools/check-crt-transcendentals.py` (`--self-test`; fast ring, under 0.1 s, in `tools/check-docs.py`) bans the calls on an explicit file list; `CatalogGenerator_Spacing_CosPiBitGoldenAndAccuracy` pins the bits and the 2^-52 accuracy (red-first: `docs/proof/caf/red-first.md`). Residual: a file not on the list is not scanned; cross-OS determinism is Inferred until the Windows ring is green on the merged build.
*2026-10-08 (Ruling 157, track RWF; investigation `docs/proof/rws/investigation.md`):* the record-write sweep of `src/` found 111 call lines in 42 files: (a) 9 feed a committed hash or golden (Catalog, Placement; closed above), (b) 13 write bytes into a user's project, (c) 89 are display or validation only. Controls: (b) New-project default (`FoilSource.cs`, `SectionReplace.cs`) joins the gate's FILES with ALLOWED_COUNT 0 and `NewDefault_ControlPointDoubles_BitGolden` pins its control-point doubles; (b) data-dependent fits (dat rotation, lambda fit, tangent angle, handle polar) cannot drop the transcendental, so four `RecordPath_*` checks hold the regenerated bytes within the 1e-6 chord identity tolerance and print `DRIFT <family> max_chord=...` for the PC ring, and `PlacementRule.SinCosDegrees` is the one managed sin/cos of degrees; (c) stay off the gate (a marker on 89 lines is noise). The 1-ulp probe is `tools/crt-probe/`, on demand, in no ring. Residual: a `DatImport.cs:386` residual within 1e-16 of the limit flips the chosen spacing on any OS (`docs/proof/rwf/residual.md`); `Tan` at `DatImport.cs:331`, `Pow` at `:460`, `Exp/Log` at `ConstrainedFit.cs:281`, `Pow` at `SectionEdits.cs:402-403` stay on the C runtime under the tolerance checks.

**WSL-INLINE-ARGV · Nested PowerShell → `wsl.exe` → `bash -lc` inline strings lose or expand arguments.** PowerShell and Bash each interpret quoting at their own boundary, so the intended Bash argv can be changed before execution; the PR #11 probes produced a malformed `source` call and lost the package query's version interpolation (`docs/reviews/pr-11.md`, Ruling 153). **Class → sweep → derive → prevent:** signature: a Windows WSL invocation embeds a multi-argument Linux command in a `bash -lc` inline string. Sweep: the two earlier W-5 probe attempts are recorded in PR #11; no broader repository sweep was performed for this availability-only task. Derive: keep the Linux command and capture logic in one committed LF script and invoke that file through WSL. Control: the always-loaded rule is “WSL commands run from committed script files, never inline `-lc` strings” (CT27); `docs/proof/win-cfmesh/probe-r153.sh` is the Ruling 153 instance. (2026-10-08) **Mechanical control (2026-10-09):** `tools/check-wsl-inline.py`, run by `tools/check-docs.py`, fails a tracked `.ps1`/`.py`/`.sh`/`.cmd` line where `wsl`/`wsl.exe` precedes `bash`/`sh` with a `-c` flag; self-test plants PowerShell, Python argv-list and `-e bash -c` shapes plus script-file, comment, docstring and `/mnt/` negatives. Sweep of main: 0 hits (the PR #11 inline probes were never committed as scripts); allowlist empty and shrink-only. Proof: `docs/proof/wig/red-first.md`.

**NATIVE-ERROR-SCOPE · A native failure outside the qualified set is projected as a product code.**
`NativeFailure.ProductCode` returned `DOC-IO` for every Win32 error other than the qualified sharing violation. That fallback would let a future adapter turn an unqualified native failure into a product outcome.

**Class → sweep → derive → prevent:** signature: a native-error-to-product-code property with a generic fallback. Sweep: `rg ProductCode src tests` found the property, its Win32 32 assertion, and no adapter reads. Derive: map only Win32 32 to `DOC-CONFLICT`; preserve every other error as null until qualified. Control: `WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped` in `WindowsProjectStoreTests.cs`, with the red run retained in `docs/proof/win-product-code/red-first.md`. Status: controlled.

**CEILING-STALE-REGIME · A time ceiling is derived from a regime that no longer exists, and the leader misses the report that says so.**
Ruling 168 (1) set a 300 s Windows evidence-ring ceiling from rings of 140 s and 168.6 s. In both, the Desktop harness had
crashed early on the fail-closed store and never spawned its suites. Tracks WRT and WTH then let Desktop run in full on
Windows. At 00:11Z the PC reported a complete ring on 1146ec8e at 345,588 ms (xmsg 20261009T001155) and asked whether a
concurrent-L3 run qualifies. The Mac leader did not read it: its watch covered branch heads, PR heads and PR comments,
but not PC xmsg lines on `origin/win/*`. At 00:19Z it restated the 300 s ceiling. Calibration run 1 was killed at 302 s
in Desktop's spawn stage, and the PC filed a second decision request. Ruling 170 raised the ceiling to 900 s.

**Class → sweep → derive → prevent:**
- *Signature:* a numeric limit whose basis (the runs it was measured from) predates a change that alters what the runs
  execute. The change here: harness aborts fixed, so more coverage runs. A second signature: a coordinator watch that
  omits one of the peer's channels.
- *Sweep:* the other Windows limits, C-2 and C-3 per part, are advisory on the uncalibrated host
  (`check-test-costs.py:45-49`). No other ceiling is stated in a ruling.
- *Derive:*
  - a ceiling is never below the latest measured complete wall for that host;
  - a change that removes a harness abort or a skip re-opens every limit derived from runs that had it;
  - the leader reads every peer channel at each poll.
- *Prevent:*
  - (a) Ruling 170 states the first rule. The Windows baseline (`docs/proof/ring-pc-win/baseline.csv`, Ruling 168 (2)) will
    replace the prose ceiling with measured rows. Until then this is prose, Status: pending a mechanical control.
  - (b) The leader's PC watch now emits `XMSG-FROM-PC <id> <kind>` for every new PC message on `origin/main` and
    `origin/win/*`. This is session tooling, not repo tooling. The repo-level control remains `python3 tools/xmsg.py
    unread`, run at each join (continuation prompt).
2026-10-09 (OBS): a killed log could not tell slow from hung, because Spawn buffers each child's output and STAGE lines had no time.
Every Desktop `STAGE <name>` line now carries `elapsed_ms=<since harness start>`, and Spawn prints `SPAWN-START <mode> elapsed_ms=<n>` at
each child start, unbuffered. A killed log now shows which children had started and how long ago. Checks: `StageTimingTests`.
*2026-10-09 (WATCH-GAP).* Same family, a third signature: a watch whose baseline lives in memory. The leader's 30-minute
Monitor expired while the leader was blocked on an operator question, and each re-arm started from an empty baseline. So
four PC messages (04:11–04:24Z: the Ruling 179 run blocked at cap 2/2, PR #23, a PR #22 refresh, a decision request) were
never emitted, and the PC sat idle for 11 h. Control: the watch now saves its baseline to disk
(`cfd-workbench-continuation/pc-watch.sh`, state in `watch-state/`). A re-arm compares against the last saved state and
replays the gap. `pc-watch.sh --self-test` is red if a change made while the watch was down is not replayed. This is
still session tooling. The repo-level control stays `xmsg.py unread` at each join, which would have caught this at the
next join but not while idle.

**CLEANUP-BLOCKS-CEILING · A cleanup path that can block defeats the ceiling it serves.**
The PC's W-2 `verify-windows-store.py` (unpushed 1d89f6c8, xmsg 20261009T010555) enforced a 60 s hard ceiling. Its timeout
path, `_terminate_tree`, called a blocking `stream.close()` on the ceiling thread. With an already-expired deadline and
an injected `taskkill` failure, a real Windows pipe child blocked cleanup for 1.49 s. The self-test used a fake BytesIO
stream, which cannot reproduce reader-thread pipe locking, so the ceiling was asserted and not proved. The PC's owner
reviewer vetoed it at the 2/2 repair cap.

**Class → sweep → derive → prevent:**
- *Signature:* a timeout or ceiling whose enforcement path itself performs an unbounded wait (close, join, kill, flush),
  proved only against an in-memory fake.
- *Sweep:* done 2026-10-09 on the Mac (`docs/proof/cbs/audit.md`). Exposed and fixed: `tools/run-readiness.py` `finish`
  (unbounded `process.wait()`; a failed group kill raised and orphaned the group), `tools/run-windows-store-gate.py`
  `run_gate` (5 s cleanup with an escaped pipe holder; a failed kill raised), and the Desktop harness `RunBuffered` in
  `WorkbenchTests.cs` (unbounded `WaitForExit()` stream drain). Safe: `run-suite.sh`, `run-tests.sh`, `ring-lock.sh`
  (bounded lock waits, no kill). Pack-managed, not edited: `run-verify-gates.py` (bounded 5 s; Windows `taskkill` has no
  timeout) and `conductor-join.py` (no ceiling) are findings for `/updatepack`.
- *Derive:* the timeout path never blocks on the ceiling thread; reader threads are daemon, closed on a helper thread
  or abandoned with a bounded join; the ceiling clock starts at process start.
- *Prevent:* Ruling 171 (1): every timeout self-test uses a real child process (not an in-memory stream). It covers an
  already-expired deadline and a failed kill, and prints the measured cleanup time on every run. Status:
  Mac sweep CLOSED 2026-10-09 (fixes plus real-child self-tests in `run-windows-store-gate.py --self-test` and
  `run-readiness.py --self-test`, both printing the measured cleanup time; the Desktop `RunBuffered` fix has no red-first
  test, see `docs/proof/cbs/red-first.md`); the PC repair track is still open.
- *2026-10-09 sibling (non-zero non-failure exit):* a tool contract's non-zero non-failure exit code
  (`verify-windows-store.py` exit 4 = NOT ASSESSED) met a runner that treats non-zero as failure (`run-verify-gates.py`,
  `run-readiness.py` `run_entry`); the PR #19 join stopped at step 8 on the Mac. Control: `tools/run-windows-store-gate.py
  --self-test` (each exit and the timeout, stub child) and `tools/run-readiness.py --self-test` (`ENTRY_RULES` exit map and
  per-entry timeout). Sweep: a new `tools/verify-*.py` with a non-pass/fail exit needs a join wrapper and an `ENTRY_RULES` row.
- *2026-10-09 sibling (rule lookup by any argument):* `rule_for` matched `ENTRY_RULES` against every argument, so the
  `run-verify-gates.py --skip ... verify-windows-store.py` line took the verifier's rule (60 s, exit 4 excluded, evidence label).
  Control: a rule applies to the executed script only; `tools/run-readiness.py --self-test` runs a script that exits 4 with the
  ruled name in `--skip` and requires red (`docs/proof/rrf/red-first.md`).

**FIXTURE-RESET-WITHOUT-DRAIN · A shared-fixture reset returns before the previous check's background job lands, and the next check captures a baseline that depends on that job.**
`Elevation_ZoomPanFit_KeyboardAndPointerSameCamera` failed once in `--views --part=1/2` as "⌘0 fits" (load 1.9 to 16).
Cause (Verified, `docs/proof/ezf/investigation.md`): `Fixture.Reset` fitted the camera while the accepted mesh issued by the
previous check's Escape was in flight; the mesh landed mid-check and the later fit used a different bounding box (Target Z
0.0469 vs 0.0008, Distance equal). Sibling of SECTION-EDITOR-LOAD-FLAKE: the same root (a background surface job
completing between dispatcher pumps), where load only stretches the window.

**Class → sweep → derive → prevent:**
- *Signature:* a Desktop check that fails only inside a part, at higher load, never alone, with a difference in a derived
  value (a fit target) and no product defect; its fixture reset does not wait for `SurfaceUpdating`.
- *Why it survives:* alone and unloaded the mesh lands inside `Reset`'s own pumps; "back to its first state" was prose;
  "CPU starvation" was accepted without a captured failure text.
- *Sweep:* only the ZoomPanFit check stores a fit and later re-fits; the other Elevation camera readers project through the
  current camera. OPEN, exposure Inferred (not reproduced): `SectionEditorTests.Reset` (`:1155`) and the pixel-sampling
  checks after a mutating predecessor (`Elevation_FrontBand_RenderedFromSurfaceView`, `Elevation_LockedDihedralRoot_*`).
- *Derive:* a reset that promises a first state drains the work the previous check left running.
- *Prevent (Ruling 172):* `Fixture.Reset` pumps until `!Controller.SurfaceUpdating` before the first `Fit()`; the ZoomPanFit
  check asserts the surface is settled at its baseline. Red first with a planted mesh delay (`docs/proof/ezf/red-first.md`):
  camera compare red at 60 and 150 ms on the old Reset, assertion red at 150 ms, green at 0, 60, 150, 200, 800 ms after the
  drain. Not done: a lint for a fixture `Reset` that fits without a drain (investigation P3).

**HOST-REBASE-BY-RECOLLECTION · A per-host re-base names the rules it covers from memory of the first failures, not from the full failing set of a replay.**
2026-10-09, track PHL. Ruling 173 (2) recommended three limits (analysis part, desktop, net wall) for the pc-win baseline. A
replay of run 1 through `check-test-costs.py` with those three limits still failed 24 times on C-5 (the 500 ms per-check rule,
worst check 1,708 ms on Windows); the "28 cost failures" the review counted were 4 + 24.

**Class → sweep → derive → prevent:**
- *Signature:* a baseline or limit set accepted on a count ("28 would fail") whose parts were never listed against the rules.
- *Sweep:* every Mac constant in `check-test-costs.py` (C-2, C-3, C-4, C-5 and its exemptions) is now re-basable per host; C-6 and the load gate are not, by design.
- *Derive:* a host baseline is accepted only when a replay of its own runs through the tool exits 0.
- *Prevent:* `check-test-costs.py --self-test` carries a case built from the pc-win run-1 numbers (C-2, C-3, C-4 and C-5) that passes with the baseline limits and fails without them; `docs/proof/phl/sim-with-baseline.txt` replays all three runs.

**COUNT-QUOTED-NOT-LISTED · A ruling quotes a failure count by class ("22 store names") that nobody listed against the log.**
2026-10-09, track PHL. Ruling 173 (3) says 22 class-(b) names enter the Windows manifest. Two of the 22 were already in it;
20 were new. `Telemetry_MarkerInjection_AbsentEverywhere` (a `win\` path fixture that fails with DirectoryNotFound on Windows)
is not a store failure: it is a ninth Windows-platform name for the class-(a) investigation, left UNEXPECTED.
- *Prevent:* each manifest entry carries a fragment from the real failure text, and `docs/proof/phl/replay-desktop.txt` replays all three runs (26 EXPECTED, 15 UNEXPECTED, identical in each).

**DPI-A · A layout constant or DIP comparison verified only at scale 1.0 and 2.0 fails at a fractional Windows scale.**
The PC ring failed eight Desktop checks at an Inferred 150 % scale (Ruling 173/174; `docs/proof/wri/investigation.md`). Avalonia rounds
border, padding and margin per edge with `Math.Round(v * s) / s` (half to even), so 1 DIP renders as 1.333 and 3 DIP as 2.667 at 1.5.
A content-sized `TextBox.prop-b` came to 35 px = 23.33 DIP, under the 24 DIP target floor (WCAG 2.2 SC 2.5.8): a real product miss.
*2026-10-09 (Ruling 178).* The scale is now **Verified 150 % (P2, 93240b06; tested head 842e575d)**: in-process RenderScaling
1.5, Screens.Primary.Scaling 1.5, UseLayoutRounding true (`docs/proof/wri-probe/instrumented.stdout.txt:1`). P4 measured the
pre-WDF TextBox at 23.333 DIP = 35 px. Items 3, 4 and 5 are scale effects (rounded view-frame borders; a 2/3-DIP sampling
seam), item 6 is open, and WDF's green at 1.5 waits on the Ruling 177 P6.
*2026-10-09 (track DPR).* Item 3 repaired (`DevicePixel.Rounded`; `docs/proof/dpr/red-first.md`). Items 4 and 5 are not: the Elevation
chip is absent from a device-resolution render (also on the Mac at scale 2), so its fix is not a sampler; the View3d chip sampler
(`DevicePixel.NearestAtDevice`) waits on an owner of `View3dTests.cs`. Item 6 open; the PC P6 is the Windows proof.
*2026-10-09 (Ruling 179, PR #21).* The Ruling 177 P6 ran on a WDF head without DPR (43830897). Item 8 is closed on Windows:
it is green at 1.5 and 2.0. Item 2 is a second assertion with zero tolerance (`ControllerViewTests.cs:640` on main,
647 vs 647.333), the same class; repair queued (track FVT). Item 4 is **capture path, Inferred**: everything
`ElevationView.RenderOverlay` draws is absent from a device-resolution `RenderTargetBitmap` on both Windows 1.5 and Mac
2.0. The overlay sits inside `PushOpacity` + `PushClip` (`ElevationView.cs:1115`, `:1199`), and View3d's chip, drawn
without them, is present. A spike decides capture versus product (track ECR). Item 5 needs a scale-aware View3d `Shot`
(track V3D). Item 6 still waits on the fact-TextBlock print.
*2026-10-09 (track FVT).* Item 2 repaired: both arrangement lines use `DevicePixel.Tolerance`; `Rounded` cannot state them (width
rounds up, height to even). The sweep of 23 `Near(…, 0, …)` sites found 2 more exposed or possibly exposed (`ControllerViewTests.cs:1171-1172`,
`AnalysisPanelTests.cs:590`), left as findings. Proof: `docs/proof/fvt/red-first.md`. The PC must see item 2 PASS at 150 %.
*2026-10-09 (track ECR).* Item 4 is a **capture defect, Inferred** (`docs/proof/ecr/spike.md`): at scale 1.5 and 2 a `RenderTargetBitmap` drops what the Elevation `Overlay` child draws, with or without `PushOpacity` and the chip `PushClip` (a rectangle drawn first, outside both, is absent too; Verified on the Mac). No live-window screenshot was possible, so the product side is not Verified. The chip check now samples a 96-dpi capture and accepts a line split over two pixels (`DevicePixel.HoldsHalfOf`; `Elevation_ChipBorderSampler_HoldsHalfOfASplitLine`, red-first in `docs/proof/ecr/red-first.md`). The Windows 1.5 proof is the PC ring.
*2026-10-09 (track V3D).* Item 5 repaired: the View3d fixture takes a second, device-resolution `Shot` and the chip top border is sampled with `DevicePixel.NearestAtDevice` (radius 2). Proof: `docs/proof/v3d/red-first.md`. The PC must see item 5 PASS at 150 %.
*2026-10-09 (Ruling 180).* Item 4 is now **Verified** a test-capture defect. The operator saw the Side chip, the lane
caption and Front starboard/port on the live app on a Retina Mac. A device-resolution `RenderTargetBitmap` drops the
Elevation overlay; the compositor does not. No product change.
*2026-10-09 (Ruling 186, paused).* The operator paused the Windows proof after five harness-blocked attempts. Items 2,
3 and 5 are repaired in the tests on the Mac but NOT ASSESSED at 150 % on Windows; item 6 is open; the ninth check is
not run. Residual risk: a fractional-scale defect on Windows that the Mac cannot show stays undetected until the proof
resumes. The resume path is the committed runner and driver, plus a Settings-launch precondition.

**Class → sweep → derive → prevent:**
- *Signature:* a style or test that sums DIP terms to a boundary (24, 320, a 1 DIP offset) and passes at scales 1 and 2 but not 1.25, 1.5 or 1.75.
- *Why it survives:* the Mac runs only at 1 and 2; the one fractional-scale ring is the PC calibration, and its failures were read as class (a) noise.
- *Sweep:* items 1, 2, 6, 7, 8 fixed or held by track WDF. OPEN, pending the PC scale probe: item 3 (double-click label), items 4 and 5 (chip border pixel sampling), and item 6 (measured skew 1.0 DIP against a predicted 0.667).
- *Derive:* a target floor is declared (`MinHeight`), never left as the sum of rounded terms; a DIP comparison uses one device pixel as its tolerance.
- *Prevent:* `PropertiesPane_Density_EveryInputDeclaresMinHeightOf24` (fails on the old style, `docs/proof/wdf/red-first.md`); `DevicePixel.Tolerance` in `tests/CfdWorkbench.Desktop.Tests/DevicePixel.cs`. No forced-scale hook exists on the Mac, so the PC ring is the only proof of the rendered height. Not done: a grep gate on `Bounds` compared to DIP literals (investigation, class prevention row).

**JOIN-CONFIG-READ-BEFORE-MERGE · A join that changes its own gate config runs the first pass with the old config.**
On 2026-10-09 the VWR join (PR #19's verifier plus its wiring) added `verify-windows-store.py` to the `--skip` list in
`docs/coordination/join.json`. `conductor-join.py` loads join.json once, at start, from the pre-merge tree
(`conductor-join.py:78`, `:272`), so step 8 ran the old skip list and stopped on the verifier's by-design exit 4. A
`--continue` reloaded the merged join.json and passed (10 gates OK, `NOT ASSESSED (verify-windows-store)`). Two earlier PR
#19 join attempts had stopped the same way before the wiring existed.

**Class → sweep → derive → prevent:**
- *Signature:* a configuration file that a pipeline reads at start, changed by the very change the pipeline is
  checking.
- *Sweep:* join.json's `checks`, `recount`, `regenerate` and `gates` all load from the pre-merge tree. readiness
  (`tools/run-readiness.py`) reads its steps at run time, after the merge commit, so it is not affected.
- *Derive:* a join whose merge diff touches `docs/coordination/join.json` is expected to need one `--continue`, and the
  leader treats a step-8 stop on a newly skipped gate as that case, not as a defect.
- *Prevent:* the pack file is pack-managed (not edited here). The control is this entry plus the continuation prompt's
  join steps. Status: pending a pack-side reload, noted for `/updatepack`.

**POST-JOIN-EDIT-UNGATED · A commit made after a join completes skips the join's gates, and readiness does not run them.**
On 2026-10-09 the Mac leader met PR #20's two join conditions with a commit made **after** `conductor-join.py` had
finished. One edit went into `docs/proof/wri-probe/receipt.md`, which the PC's `capture-manifest.json` pins byte for
byte. `tools/run-readiness.py` was GREEN, because readiness does not run check-docs or the capture check, so main was
pushed at 87436728 with `check-capture-manifests.py` failing (8342 bytes declared, 8718 committed). The leader caught it
on a re-check, restored the pinned bytes, and moved the note to `mac-join-note.md`.

**Class → sweep → derive → prevent:**
- *Signature:* any change committed between a completed join and the push, outside a `--continue`.
- *Sweep:* the earlier post-join edits this session (the PR #12 receipt line, the PR #17 index refresh) went through a
  `--continue`, or touched no pinned file.
- *Derive:* a join condition is applied before the join completes: fix on top of the merge, then `--continue`, never
  as a later commit. A file listed in any capture or closing manifest is never edited; an annotation goes in a sibling
  file. *2026-10-09 (PR #21 join):* the PC's manifest re-pins every file in its folder, so the PR #20 sibling note is now
  pinned too. Each Mac note is a new file and is never edited (`mac-join-note-pr21.md`). The capture check fails any edit.
- *Prevent:* the leader runs `python3 tools/check-docs.py` before every push that follows a non-join commit (session
  rule). Status: controlled. `docs/coordination/join.json` readiness now opens with `python3 tools/check-docs.py`
  (about 10 s); a mismatched capture manifest turns `run-readiness.py` RED (`docs/proof/rcd/red-first.md`).

**RUNNER-MIXED-TIME-BASIS · A deadline parsed to local DateTime is compared directly with a UTC clock.**
2026-10-09, Ruling 179 Windows execution. The comparison at `docs/proof/wri-r179/execute.ps1:19`
stopped before any product check. `deadline-observation.stdout.txt` verifies that parsing
`2026-10-09T04:20:00Z` gives Local-kind `2026-10-08T21:20:00-07:00`; at 04:12:17Z, the direct
comparison says expired while the UTC-normalized comparison says not expired. Cap 2/2 stopped
the execution track; Settings and a fresh process verified restoration to 150% and 1.5/1.5.

**Class -> sweep -> derive -> prevent:**
- *Signature:* `UtcNow` ordered against a `DateTime.Parse` result whose Kind was not established.
- *Sweep:* proof-local PowerShell scripts have one such comparison, execute.ps1:19. Settings and
  process scripts record UTC timestamps without parsed-deadline comparisons.
- *Derive:* deadlines compare absolute instants in the same basis, never local clock fields against UTC.
- *Prevent (proposed, not implemented):* next authorized runner must use DateTimeOffset with explicit
  AssumeUniversal/AdjustToUniversal or equivalent and a planted timezone self-test covering UTC and
  America/Los_Angeles offsets, future and expired instants. The current runner remains unchanged after
  the cap. This entry is a mandatory grounding warning, not a claim that a failing gate exists.
- *Companion preparation findings:* inspect actual newline bytes before patch anchors; verify explicit
  pinned SDK/Python executable identities before ProcessStartInfo launch. In this run the source anchor
  guessed CRLF over LF, system dotnet lacked 10.0.203, and PATH-resolved `py` rejected `-3` during cleanup.
  The next-run deterministic preflight controls are proposed, not implemented.
- *Prevent (Ruling 182, replaces the proposal):* the next runner holds no parsed instant. It reuses the Stopwatch
  deadline of `docs/proof/ring-windows/capture-calibration.ps1:11,17-20`, whose mutation self-test is
  `verify-capture-deadline.ps1`. The runner's own self-test adds 899,750 ms → waits and 900,000 ms → expired. Status:
  controlled by `tools/windows-runner.ps1` and `py -3 tools/check-windows-runner.py --self-test`;
  the retained Windows proof is `docs/proof/wri-r182-runner/receipt.md` (see THROWAWAY-RUNNER).

**THROWAWAY-RUNNER · A one-off Windows run is driven by uncommitted harness code, and the harness, not the product, fails.**
Three Windows scale runs in a row stopped in their own harness before a product check could tell anything:
- PR #20: an added `AppliedDPI == 192` guard skipped all eight checks at a verified 200 %.
- PR #21 (ac58814f): UIA waited 55 s for a checkbox inside a collapsed Settings group.
- PR #23: a Local-kind deadline and a wrong `dotnet` path.

In two of the three cases the repo already held the correct shape: `capture.ps1:14-29` checks the pinned SDK, and
`capture-calibration.ps1` holds a self-tested Stopwatch deadline (Ruling 182, finding 6). The instrumentation each run
rebuilt (the scale-context print, the item-6 bounds) had no committed home either.

**Class → sweep → derive → prevent:**
- *Signature:* a run whose runner is written for that run, used once, and not self-tested. Its failure spends the run's
  repair cap on the harness.
- *Sweep:* PRs #20, #21 and #23 (above). The ring captures (`docs/proof/ring-windows/`) use committed, self-tested
  scripts and did not fail this way.
- *Derive:*
  - a Windows run uses a committed runner whose self-test is green in a committed runner-ready receipt before any scale
    change;
  - the runner reuses committed shapes (the reuse rung) before writing new ones;
  - measurements the receipt needs are printed by the committed harness, not by temporary instrumentation.
- *Prevent:* Ruling 182's six red-first preflight controls (a)–(f) in `docs/reviews/pr-23.md`: no AppliedDPI read, a
  read-only UIA preflight, a Stopwatch deadline, toolchain identity, numeric stub exits 0 and 3, and zero source edits.
  The fresh budget opens only on a committed runner-ready receipt. Status: pending the PC runner; the Mac measurement
  prints are track MSP.
  - *2026-10-09, trk-msp:* the Mac half of the prevent has landed. The Desktop harness prints `SCALE_CONTEXT` once per window mode, `ITEM6` (the three item-6 controls) and `P3` (the double-click clause) on pass as well as fail. `Spawn_WindowModeWithoutScaleContext_Fails` fails the run if a spawned window mode lacks its line (`docs/proof/msp/receipt.md`).
  - *2026-10-09, Windows Ruling 182:* the PC half is executable: `tools/windows-runner.ps1`,
    `windows-settings-preflight.ps1`, `check-windows-runner.py`, and `test-windows-runner.ps1`.
    The retained runner-ready receipt proves six controls red-first/green on an MSP/RG5 head.
    Status: controlled for runner preparation; scale/product proof remains unrun.
  - *2026-10-09, Ruling 185 (sentinel fixture in a semantic reader's working directory):* the driver's qualification
    used up its repair cap on the harness again.
    - `test-windows-runner.ps1` wrote the literal `identity` to a fixture `global.json` as an opaque sentinel for the hash
      guard.
    - `Assert-WriToolchain` then ran `dotnet --version` with that fixture as its working directory
      (`windows-runner.ps1:191`, `:303`), and the SDK resolver parsed the placeholder.
    - The throw at `:306` folds a non-zero exit and a wrong version into one message, so the cause stayed Inferred until a
      one-variable re-run.

    *Signature:* a fixture written for one reader sits where a second reader parses it. *Control:* a fixture that mirrors
    a real repo file is copied byte for byte from the repo root. One assertion before the toolchain call compares the two
    and throws `WRI-FIXTURE`. Status: in the PC's Ruling 185 track. Next step, parked: `Assert-WriToolchain` carries the
    exit code and trimmed stdout in its throw (a library track with its own hash review).

**CLOSING-MANIFEST-MUTABLE-PATH · A closing manifest pins a live source path whose bytes are expected to change.**
On 2026-10-09 the PC's Ruling 184 blocked receipt could not pass check-docs. `docs/proof/wri-r182-runner/closing-manifest.json`
binds four tools by their live `tools/` paths: the checker, the runner, the runner test and the preflight. Ruling 184
legitimately changed those files, so their sizes and SHAs no longer matched. The manifest froze history by pointing at
the present. The fix was authorized at 19:49:54Z: immutable snapshot copies of the `e9714ce5` blobs inside the proof
folder, with only those four paths rebound and the bytes and SHAs unchanged.

**Class → sweep → derive → prevent:**
- *Signature:* a closing or capture manifest entry whose path lies outside `docs/proof/` and names a file a later track
  may edit (`tools/`, `src/`, `tests/`).
- *Sweep (2026-10-09, Ruling 187):* done. Across all closing manifests, two kinds of entry lay outside `docs/proof/`:
  win-naca's `cases/*.yaml`, which is by design, and the PR #25 manifest's own `.gitattributes`. That second entry is
  the class recurring in the PR that recorded it: every proof track appends to `.gitattributes`. The leader rebound it at
  the PR #25 join to `docs/proof/wri-r184-result/source-snapshots/cfd947c3/gitattributes.txt`, with the same bytes and
  SHA and a `-text` attribute.
- *Sweep extended (2026-10-09, Ruling 190, review of PR #27):* a scan of all 42 proof manifests found 9 bindings to live
  paths, all from before this PR. `win-naca` (closing and hash manifests) binds `cases/*.yaml`, which is by design.
  `win-routes/manifest.json` binds 2 `cases/*.yaml` and **`docs/design/guided-solver-setup.md`, a live design
  document**: an edit to that design would turn check-docs red. That is an open instance, to be rebound to a snapshot when
  the prevent lands.
- *Derive:* a manifest freezes evidence by copying it into the proof folder; it never binds a path that a later track
  may change.
- *Prevent:* pending. `check-capture-manifests.py` flags a closing-manifest path under `tools/`, `src/` or `tests/`,
  red first on the R182 manifest at main. Until then this is prose and the PC's snapshot convention.

**WINDOWS-CAPTURE-LIFECYCLE · A launcher mistakes root exit or a termination request for complete, bounded process cleanup.**
Ruling 181's watchdog lost the numeric exit. Runner review also exposed inherited output handles, asynchronous
assignment-failure termination, and residual accounting before SDK shutdown.
Ruling 184 preparation exposed a related startup boundary race: sleeping a truncated remaining allowance can wake
before the Stopwatch reaches the deadline, launch a child, and exceed the total allowance during cleanup. The retained
`docs/proof/wri-r184-driver/startup-probe.stdout.txt` observed WRI-DEADLINE at 1015 ms against a 1000 ms envelope.
The active runner now rejects an injected delay at or above the derived remaining allowance before sleeping or launching.
The existing delayed-startup runtime fixture keeps its WRI-ENVELOPE and under-1000-ms requirements; a new policy
mutant removes this refusal and must fail control (c). This is the single R184 preparation repair cycle.

**Class → sweep → derive → prevent:**
- *Signature:* missing retained handle/numeric exit, an unbounded drain after root exit, a termination request without
  observed completion, or a target residual query before numeric build-server shutdown.
- *Sweep:* PR #22 watchdog, historical PR #23 runner, and the new reusable runner's normal, timeout, assignment-failure,
  and BuildVerifier paths. Historical pinned scripts remain evidence, not active launchers.
- *Derive:* preserve process handles; suspended launch owns the job before resume; one Stopwatch includes startup,
  cleanup, and final source checks; BuildVerifier mode records shutdown exit before target job accounting.
- *Prevent:* `py -3 tools/check-windows-runner.py --self-test` runs 0/3 exit stubs, inherited-output, assignment and
  termination failures, timeout, delayed-startup, shutdown-order, shutdown-exit-3, and PII fixtures. Ring: Windows
  runner preparation; measured runtime 28.162 s, whole retained qualification 40.976 s. Status: controlled; no verifier
  execution or system-wide residual sampler is claimed (`docs/proof/wri-r182-runner/receipt.md`).
- *2026-10-09 (Ruling 190, PR #27, deferred-metadata sibling):* the exit was observed and then lost by **write order**.
  `docs/proof/copy447-reachability/capture.ps1` ran every child through the committed runner, which returned numeric
  exits, PHN results and raw hashes. The script held them in memory for one final `receipt.json` (`:124-147`). With
  `$ErrorActionPreference='Stop'` and no catch, a later throw discarded all of them and the launcher's own error.
  Separately, `package-pii.ps1:34-35` overwrote the original `pii.*` captures. *Derive:* a capture script writes each
  child's metadata to disk right after that child, before any assertion. The launcher catches and records its own
  error. Packaging writes beside originals, never over them. *Control:* a PC condition of Ruling 190 (red-first on a
  stub exiting 3 plus an injected throw after the case). It is pending until a capture script is next used. COPY-447 has
  no budget before W-2 B2.

**RUNNER-POLICY-NAME-ONLY · A guard sees a name in source and falsely certifies the active call or allowed target.**
The first runner guard accepted commented numeric validation, removed pre-child source validation, added unbounded
waits, a StreamWriter constructor, and a second dot-source. Separate adversarial review found those false-greens.

**Class → sweep → derive → prevent:**
- *Signature:* comments satisfy presence checks, one allowed method name admits a different constructor target, or a
  set erases a duplicate invocation whose count is part of the contract.
- *Sweep:* deadline call sites, numeric exit validation, before/after source guards, UIA commands/methods/constructors,
  and dot-source count across the four new implementation files.
- *Derive:* inspect active call sites/control flow; on Windows parse UIA AST; allow exact constructor targets and
  exactly one approved dot-source. Portable lexical policy is named separately from Windows qualification.
- *Prevent:* the 19 planted policy mutants in `tools/check-windows-runner.py --self-test`; Ready also runs the policy
  gate. Ring: fast policy / preparation mutant suite. Measured Windows policy 1.457 s, mutant suite 11.667 s. Status:
  controlled; output/timeout publication is PHN-checked (`docs/proof/wri-r182-runner/receipt.md`).

## 2026-10-09 round — cost caps under a concurrent ring (CCL)

**COST-CAP-LAGGING-LOAD · A cost cap is excused by an end-of-run load average, which lags and cannot tell a ring that overlapped another from a quiet one.**
Five tracks (RG4, PHN, SDG, CBS, RLK) each lost a repair cycle: the first `tools/run-tests.sh` exited 1 on C-2..C-5 only (Analysis 6869 and 5216 ms against 5000; net wall 50,032 ms; C-2 5032 and 5083 ms at load 20.8), and every re-run passed. C-2..C-5 failed whenever the 1-minute load at ring end was <= 24, but that load averages the last minute: a second ring that started late or ended before the end reading leaves it under 24 while the overlap still slowed a part. Measured 2026-10-09 (`docs/proof/ccl/receipt.md`): a ring alone ended at load 18.4 with net wall 43.9 s; with a second ring from second 20 it ended at 23.66 (under the gate) with net wall 49.6 s against the 50 s cap and Desktop 46.2 s.

**Class → sweep → derive → prevent:** signature: a pass/fail rule on a shared-resource quantity (wall clock, CPU) that decides contention from one lagging sample taken after the work, not from what ran during it. Sweep: C-2, C-3, C-4, C-5 in `tools/check-test-costs.py` (fixed here); TEST-BUDGET (`--budget`) uses the same end-load gate and was left as is (its 60 s budget carries more slack; measured 54 s with a peer); C-6 is a missing reading, not a contention quantity, and stays ungated. Derive: ask the lock that already knows who ran, not the load average. Control: `ring_lock_peers` in `tools/ring-lock.sh` and a once-a-second sampler in `tools/run-tests.sh` write `.tmp-tests/peers.txt`; `check-test-costs.py` turns an over-cap C-2..C-5 into `COST-ADVISORY (concurrent ring <pids>)` (printed, never silent) and keeps it a failure when the file is absent or empty. Ring: every join; cost one `cat` per second. Tests: `check-test-costs.py --self-test` (6 peers cases, red 81/83 before the change, 83/83 after) and `ring-lock.sh --self-test` (2 peers cases). Residual: a load source outside the ring lock (a build, an editor) is still judged by the end load only.

**CALLBACK-PATH-SHADOW · A callback parameter shadows a captured path in a case-insensitive dynamic scope.**
The first Ruling 184 live cycle passed callback text as PowerShell's -File argument. Its earlier callback-only tests
proved sequencing but never reached the real driver's child-argument construction.

**Class → sweep → derive → prevent:**
- *Signature:* a captured path shares its name with a callback parameter; a shape check or callback-only stub passes,
  but the actual boundary receives the callback value instead of the file path.
- *Sweep:* the scale driver's preflight, driver and DLL path locals against the sequence's Preflight, Build, Select,
  Contract, Restore and Readback parameters. The preflight path was the collision; the driver/DLL names were distinct.
- *Derive:* name the path `preflightPath` and reuse that same value at both real preflight child call sites.
- *Prevent:* `tools/test-windows-scale-run.ps1` exercises the actual driver callback with external children stubbed,
  captures the -File argument, and compares it with the real preflight path. Red before the rename, green afterward,
  retained under `docs/proof/wri-r184-driver/callback-repair-{red,green}/`; the first live receipt retains numeric exit64.
  Ring: runner preparation, 10s ceiling; observed red3.147s/green3.020s. Status: controlled for the path boundary.
  The second live cycle reached that path and failed closed on absent Settings; product/scale proof remains unassessed
  (`docs/proof/wri-r184-result/receipt.md`).

**TRANSIENT-STATE-REDERIVED · A key clears a derived value, and the next refresh rebuilds it from a saved input.**
Escape set `PlanCanvas.TooltipText` to null, but nothing recorded the dismissal. `UpdatePlan` runs on every controller
`Changed` and re-read the saved `lastHover`, so a mesh completion or the 250 ms behind timer brought the tooltip back
under a still pointer. In the suite it read as a flake in `PlanCanvas_Escape_DismissTooltipThenClearSelection`
(`docs/proof/pce/investigation.md`); in the app it was an operator-visible bug. Ruling 188.

**Class → sweep → derive → prevent:**
- *Signature:* a view clears a derived value (tooltip, probe, ring) on a key; a refresh path recomputes it from a saved
  position with no record of the dismissal.
- *Sweep:* the PCE sweep of hover, tooltip, timer and two-step-key checks (investigation Q3): only the Escape check was
  exposed. `HoverPoint_TooltipCopyAndRing`, `HoverRail`, `ProbeAndDelta`, `EscapeOnHandle`, the drag-Escape checks and the
  `Elevation_*Probe*` checks read at once or wait for the gesture; the other `Key.Escape` sites are single-press dialog
  or text cancels, grep only (Inferred, not opened line by line).
- *Derive:* record the dismissal (`tooltipDismissed`), clear it on a pointer move to a different position, and let the
  refresh path (`ReadHover`) keep the tooltip null while it is set. The probe readout is unchanged (Ruling 188 (2)).
- *Prevent:* `PlanCanvas_Escape_TooltipStaysDismissedAcrossRefresh` holds the mesh, hovers, presses Escape, releases the
  hold and asserts tooltip null, selection kept, probe unchanged, and a real move restores the tooltip. Red before the
  fix, green after (`docs/proof/pef/red-first.md`). Ring: `--plan-canvas`, every code-changing join; cost about 1.5 s.
  Status: controlled.

**PLAT-A · 2026-10-09 · An admitted Windows PASS outlives the inputs it was earned on.** Ruling 189 admitted the Windows store PASS (tested head a5c45644) only while the verifier, runner, three build-input trees and four blobs stay unchanged. Control: `docs/proof/windows-store-admission.json` binds them; `tools/check-windows-admission.py` prints `WINDOWS-STORE-EVIDENCE ... current` or `... STALE since <path>` (exit 0, so Core changes are never blocked), `tools/run-windows-store-gate.py` prints the same line, `--self-test` runs in check-docs (fast ring), and a docs file that claims `Windows store: PASS (current)` while stale fails. Status: controlled.

**ROW-MODAL-A · 2026-10-09 · A new command row that opens a modal dialog stops the all-gestures probe.** Adding `file.export` put a row whose command opens the real Export dialog (modal) before the rows after it. `WindowsShell_EveryTableGesture_FiresItsCommandOnce` runs every row's command in turn, so the open dialog took every later key and 15 rows read "fired 0" (first ring of track trk-dat).
- *Sweep:* the checks that execute rows, not only read them: `CommandTable_Parity_*` and `ShellWindowTests` read the table; only the gesture probe runs each command. The other modal rows (Replace from catalog, Save to My sections) are disabled outside a section, so the probe never ran them.
- *Derive:* a row that opens a modal has a test hook that replaces the dialog (`ShowCatalogDialog`, `ShowExportDialog`); a probe that runs every row sets the hooks.
- *Prevent:* the probe check sets `ShowExportDialog`. The probe is the control: the next modal row without a hook fails the same check red (it did here, before the hook). Ring: `--shell-window`, every code-changing join. Status: controlled.
