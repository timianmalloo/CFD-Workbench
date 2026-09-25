---
id: proof-application-adapters
title: Native application adapter implementation proof
type: proof-pack
status: in-review
owner: "@cfd-native-adapters-20260923"
phase: implementation
tags: [application, native-ui, cli, accessibility, proof]
links:
  - {to: coordination-contract-c-native, rel: depends-on}
  - {to: design-application-foundation, rel: depends-on}
  - {to: workbench-direction, rel: depends-on}
review-by: 2026-10-23
summary: Source-bound build, CLI, controller and package evidence for the first native adapter; rendered macOS and Windows runtime acceptance remain open.
---

# Native application adapter proof · source review candidate

The latest source-bound result is the **R37 12-step gate and 72-row applied-theme matrix** recorded at the end of this document. Earlier receipts below remain as historical evidence and do not bind the R37 package.

The adapter uses the joined core API without changing core or persistence source. The macOS and Windows packages cross-build and the managed tests pass. **This is not M1 UI acceptance.** An initial macOS CUA attachment returned `cgWindowNotFound`; after the user confirmed visibility, root attached and observed the native interaction defects below. The expected standalone `application-macos-ui.png` and `application-macos-ax.json` remain absent because that CUA surface supplied no filesystem export. Windows runtime is Not assessed on this macOS host.

Goal: one offline Example/open/edit/preview/apply/cancel/undo/redo/save/reopen/recovery route plus CLI identity and diagnostics. Done when root independently exercises the rendered native app, reviews AX and platform states, and Owner accepts the C proof. Out of scope: solver/export, full v7 workspace and Windows runtime inference from cross-publish. Tier T2, fan-out zero. The native medium is Avalonia 11.3.14 on .NET SDK 10.0.203; distribution candidates are a self-contained macOS `.app` and Windows portable folder. Accessibility targets are NSAccessibility and UI Automation. Official review references are [Apple macOS design](https://developer.apple.com/design/human-interface-guidelines/designing-for-macos/), [Apple keyboard guidance](https://developer.apple.com/design/human-interface-guidelines/keyboards), and [Microsoft Windows accessibility](https://learn.microsoft.com/en-us/windows/apps/develop/accessibility).

The latest changed-source gate command is `python3 tools/verify-application-adapters.py`. Its retained [receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-_674s2ub/receipts/verification.json) has SHA-256 `22565c2c868b71da069b94e7b67b9e0a47e16092fb019fd39473d7691363017d`. It records base HEAD `de105f0`, all 52 authored/source/config/fixture input hashes before and after, seven restore maps under fresh scratch, 16 published binary hashes, 2206 artifact files, zero artifact symlinks, no observed collector, exact-owned child groups quiescent, and eleven commands exiting zero. The source-tree generated-output inventory was unchanged. The gate includes an executable macOS startup smoke with deterministic high-contrast, 1024×700, reduced-motion review selectors; it requires `Window.Opened`, zero exit and exact-owned process quiescence before publishing. Fifteen Light/Dark/HighContrast token contrast pairs have a measured minimum of 6.15:1. This checks declared tokens and XAML startup, not rendered high contrast or native AX.

An earlier real package launch found a `RowDefinition.Height` type mismatch that had passed compilation; raw [failure receipt](/private/tmp/cfd-c-final-ui-31t13mk0/launch-receipt.json) records `InvalidCastException` before window creation. The GridLength resource repair and executable startup smoke are the recurrence control. A later fresh gate attempt hit a Roslyn `csc` exit 139 (actual macOS crash report retained), with source/output hashes unchanged and no live owned children; [failure receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-7q1z1sza/receipts/verification.json) remains. One coordinator-authorized same-profile retry produced the passing final receipt above; this does not diagnose or erase the compiler crash.

The latest packages are [macOS `.app`](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-_674s2ub/packages/osx-arm64/CFD%20Workbench.app) and [Windows portable folder](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-_674s2ub/packages/win-x64/CFD%20Workbench%20Windows). Packaging proves build contents and self-contained runtimes, not launch usability, signing, notarization or SmartScreen reputation. Root independently verified an earlier package's file/hash parity and a packaged macOS CLI scientific fixture; the latter receipt is `/tmp/cfd-adapter-review.3sSb4H/cli-packaged-result/summary.json`, SHA-256 `2c64b23d...`.

The corrected pre-final package was copied to a unique review bundle by changing only `CFBundleIdentifier`; executable and DLL hashes matched that package. Its [launch receipt](/private/tmp/cfd-c-final-ui-aw8xt0s_/launch-receipt.json) records owned PID 48841, successful app initialization, assigned main window and `Window.Opened`. An initial CUA exact-path binding returned `cgWindowNotFound -10005`. After the user confirmed the window was visible, root attached to the same instance and inspected its rendered surface, AX names and interactions. That binary differs in SHA from later recompilations, so its native observations do not clear the new package.

After user visibility confirmation, root bound that review window with CUA and observed three native interaction defects: F6 skipped Navigator, keyboard selection of a locked CV replaced list AX children and lost focus, and Escape after a 5 mm preview left stale numeric text while another click on the selected CV did not reopen a draft. The bounded repair keeps station/CV item objects when accepted identity and source hash are unchanged, makes F6 focus actual station/control items, binds the selected accepted value after Cancel, and handles a repeated selected-item pointer release. The [targeted RED receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-c-targeted-qi3j7klw/receipts/targeted.json) captured missing control seams; the [targeted GREEN receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-c-targeted-yb3stpab/receipts/targeted.json), SHA-256 `8420d91876ab103c9f922c991e4daac53f84fb0af0a389f55d37c1620be4b222`, records build, managed controller/selection checks and apphost startup smoke, all zero with unchanged source/output hashes. The latest full gate above binds the changed source. **These are managed and startup controls, not native interaction proof.** Root must retry the exact F6, Down, Escape and repeated-click sequences against this newly built package before the UI veto can clear.

The review-only process selectors use `CFDW_REVIEW_MODE=1`. `CFDW_REVIEW_PERSONA` is `designer`, `keyboard`, `screen-reader`, or `dense`; `CFDW_REVIEW_SIZE` is `WIDTHxHEIGHT` within 1024×700–2560×1600; `CFDW_REVIEW_THEME` is `system`, `light`, `dark`, or `high-contrast`; and `CFDW_REVIEW_REDUCED_MOTION` is `0` or `1`. `CFDW_REVIEW_STATE` selects `empty`, `example`, `draft`, `invalid-input`, `refused-open`, `file`, `import`, `recovery`, `not-assessed`, or `invalid-geometry`. File-backed states require `CFDW_REVIEW_PATH`. The harness calls the real controller and core: a geometry status label is shown only after the actual `Geometry.Assess` status matches; `file` requires certified accepted geometry; `recovery` requires a separate saved offer; and `dense` requires a certified file with at least 16 actual station/control annotations. It does not synthesize accepted geometry. Persona changes initial focus to viewport, numeric/open, station, or control region. The reduced-motion selector removes live control transitions, including newly laid-out controls and the unsaved dialog; the source defines no keyframe animation. Native compositor/platform motion remains Not assessed.

Managed tests exercise the minimum 1024×700 layout arithmetic, scrollable full station/CV annotations, 15 named samples, theme token contrast, selector refusal, real invalid geometry, recovery and unprojectable draft binding. They do not prove rendered clipping, AX bridge behavior or native keyboard dispatch. At the minimum size the center plot calculation retains at least 250 pixels, the section panel uses a 300-pixel token, and right actions reflow into two rows. Actual font, DPI and platform chrome can still cause overflow; root must inspect the final package.

| Claim | Failing condition and oracle | Evidence / red observed | Confidence and residual risk |
|---|---|---|---|
| CLI and source identity | `/1` or unsupported geometry must refuse with stable exit and unchanged bytes; admitted `/2` reports source/surface/evaluator identity and physical/normalized units | Root's 16 actual-process fixture cases and packaged scientific fixture passed; local CLI test project passed in the gate | Verified bounded cases; no whole-language conformance claim |
| Accepted/draft/recovery authority | Failed opens preserve active accepted source; definite conflict stays definite; uncertain publication remains dirty; invalid recovery is separate; incomplete numeric input cannot save | Named local controller tests; root independent three-case controller and recovery roundtrip probes passed | Verified named interleavings and fixture; rendered user comprehension still open |
| Derived view | Exactly 15 certified `PointAt` samples, selected interior η and one section query; independent upper/lower trailing edges; one physical XYZ scale; segment interpolation error labelled Not assessed | Local tests and root's eight editable LE/TE target displacement probe passed (2.829–3.333 mm for +5 mm CV edits) | Verified samples, not visual fidelity or app latency budgets |
| Semantic viewport | Native station/CV annotation controls expose full names, units, editable/locked state and stable IDs; section samples have separate labels | Managed control/peer test passes; controls are in a scrollable visual panel | Source-bound only. Native AX bridge, offscreen behavior and screen-reader traversal Not assessed |
| Keyboard and close | F6/Shift-F6 cycles enabled regions; platform Undo/Redo map; numeric Enter/Escape scoped; unsaved Save/Discard/Cancel | Named mapping tests and source readback | Actual OS key dispatch, dialogs and focus restoration Not assessed |

## Latest bounded native interaction repair

Root's later CUA review of the previous package found three more interaction failures: F6 skipped DocumentTabs; Return/Space on the still-selected editable CV after Cancel did not restart editing; and a queued programmatic numeric-field change advanced an untouched TE draft or invalidated an unprojectable recovery. A retained pre-fix apphost trace shows TE text `120`, valid input and generation 0, followed by generation 1 without user input. The bounded repair adds a selected-tab focus target, keyboard re-edit on the selected CV item, and a binding guard that ignores delayed programmatic and duplicate current-text events while admitting changed user text.

The [RED receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-c-targeted-9f37gv7i/receipts/targeted.json) found the absent seams. The [targeted GREEN receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-c-targeted-sr0sfxpa/receipts/targeted.json), SHA-256 `8c1e750defc1db544cb5a03d8aa1b57385eb0a7dbacf54939b31ee83bb0b793c`, binds Desktop source SHA-256 `73a92abc84569ffc67f40162921f2039700a84154bce78999153c875424095e1` and test source SHA-256 `2eaf2705b5ec9abc99f2d879829223923189414578e36613d39692260eb31bde` to a Desktop DLL SHA-256 `d566e17017536539d149d1c874d7fa68987087e7bd1df5eb636c71ae4d34bb85`. Build and Desktop tests exited zero with unchanged source/output hashes, task-local caches and no remaining owned children. The earlier full gate and package do **not** bind these new source hashes. Corrected native F6, keyboard re-edit, TE selection generation 0, user numeric update, Cancel/reselect, repeated LE↔TE and invalid-recovery Preview remain open for root CUA after the combined B/C package gate.

## Native UI-T4 proof rows

| Claim | Failing input or condition | Oracle | Evidence | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| Platform HIG | Window, file dialog or shortcut violates macOS/Windows convention | Independent platform checklist on native window | Root inspected one live window; dialogs and platform matrix remain open | Open | Partial native | Platform affordances may differ at runtime |
| Keyboard traversal | Core flow, recovery or unsaved dialog cannot be completed without pointer | Root CUA keyboard recording at minimum window | Root observed F6 Navigator skip and selection focus loss; changed-source control passed, native rerun pending | Yes on prior binary | Native failure pending retest | Focus order/trap and dialog handling remain open |
| Accessibility tree | Custom viewport lacks role/name/children or stable selected state | NSAccessibility/UIA capture with expected station/CV and section semantics | Root saw station/CV and section AX names in one instance; list children were replaced on selection | Partial native | Native failure pending retest | Scrollable child bridge, offscreen behavior and full reader traversal unverified |
| Theme/high contrast | OS Light/Dark/HighContrast makes text or focus unreadable | Runtime theme switch and contrast inspection | Three token palettes and 15 contrast pairs measured by gate; review selector source | Open | Source only | No platform high-contrast run |
| DPI/windowing | 1024×700, resize, mixed DPI, minimize/restore clips fields or changes focus | Runtime screenshots and keyboard at each size | Minimum-width plot arithmetic and action reflow source/tests | Open | Not assessed | Native font, DPI and chrome may still overflow |
| Large lists | More rows than viewport block input or disappear | Scroll and responsiveness measurement | Scrollable full station/CV annotation source; dense real-file selector | Open | Source only | No dense runtime benchmark |
| OS integration | Picker, dock/taskbar or file handling violates platform behavior | Native interaction and file-preservation probe | Controller disk probes; picker source | Open | Partial | Native picker was not exercised through CUA |
| Distribution trust | Unsigned/unnotarized/untrusted package is treated as release-ready | codesign/notarization/SmartScreen release receipt | Development packages only | Yes: release proof absent | Flagged | Release channel cannot ship yet |

Avalonia-specific rows: `AutomationProperties` and IDs exist on visible station/CV/source/actions, but NSAccessibility/UIA mapping is Not assessed. The XAML token gate and fixed contrast pairs pass; system theme/high contrast and focus visuals have no runtime verdict. Browser/CSS craft rules are inapplicable to this native XAML corpus, not a zero-file PASS. No generated exemplar art or third-party UI material is embedded; Avalonia packages are pinned by the project and their SBOM/license review remains an ADR-0003 release obligation.

## R29 portability and native timing candidate · 2026-09-24

The joined B handoff is `13c883a`; this C continuation changes only the Desktop adapter, its managed tests, two package/verifier scripts, and this proof. `verify-portable-text-io.py` first found three missing explicit text-I/O controls (two stdout guards and one LF write); `verify-subprocess-utf8.py` found two implicit text encodings. Both executable gates now pass, respectively 0/0 findings, after explicit stdout guards, `newline="\n"`, and UTF-8 `check_output` calls. The sibling sweep was both whole-tool gates, not a claim that other platforms ran. The source/config input and package gate still need a final changed-source run after this candidate freezes.

The installed Avalonia 11.3.14 XML described renderer members that did not compile as public API. That RED is retained in `cfd-c-targeted-dlx1n84n/receipts/targeted.json`. The public target-bound path `ElementComposition.GetElementVisual(viewport)?.Compositor` compiled and one real window observed a render callback and `CompositionBatch.Rendered`; the raw [spike receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r29-render-spike-4dp_0lkz/receipts/render-spike.json) has SHA-256 `b753b4a35e6a588d417d332a8eea962512ed1042c7632865dd742cf7df1f5240`. Its reference-only frame check could match an earlier drawing, so that spike did **not** establish a fresh scene. The app's normal-path `NATIVE-METRIC` now records a monotonic viewport render serial and revision, the exact frame/provenance/section η, a serial baseline before Refresh, and a newer matching recording before target compositor batch completion. A timeout, supersession, stale state, missing compositor, or missing target recording emits `not_assessed`; no source hash/path is emitted. A batch callback is **not proof of successful target draw or display presentation**: Avalonia's server compositor can notify a rendered batch even when its render interface is not ready or a render attempt fails. The visible-event budget therefore remains **Not assessed**.

The first trial-driver attempt read the previous metric before a queued synthetic `TextChanged`; raw [driver RED](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r29-timing-control-g5ja4z7i/receipts/timing-control.json) preserves Preview supersession and an invalid known-delay conclusion. The repaired driver waits for a strictly newer edit Sequence and matching draft Generation. The [bounded control](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r29-timing-control-g2z4ycyj/receipts/timing-control.json), SHA-256 `fba071484a0669ad1efedbfa41b9b503c641295aabb82abaa9ce77f41289c54b`, observed a 150 ms injected Preview delay inside a 325.164 ms batch-cycle result. Before publication the metric remained pending. An immediately canceled in-flight Preview emitted `not_assessed/superseded`; this does not prove a completed-frame late-publication race. A separate real controller Preview with a 250 ms publication delay and 40 ms measurement timeout emitted `not_assessed/timeout` after 41.635 ms; late publication did not turn that result into success, and Cancel restored the accepted source hash. All control processes exited, with no remaining exact-owned descendants or observed collector. These are **synthetic TextChanged and direct-controller review controls**, not OS input.

The [raw trial receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r29-timing-trials-ym0g5jhk/receipts/timing-trials.json), SHA-256 `9d063d115f0750a1df933299495ec82066ef32020828e8b550070005b3112fe4`, retains every operation outcome and three separate process launch/PID/start/cleanup records. Its sibling `timing-summary.json` retains sorted raw successful timings. Thirty review-app cycles used a direct controller BeginEdit, synthetic numeric `TextChanged` (`5` or `6` mm), real controller Preview and Cancel, then checked the accepted source hash after each cycle. They did not assert full revision/history identity. Three fresh processes produced Example-ready readings from **managed `Main` start**, not external process launch. Each emitted record ends at a freshly correlated target compositor **batch cycle**, not native display presentation. Nearest-rank p95 is computed only over `batch_cycle_complete` outcomes; refusals/timeouts would remain in the raw denominator and are not silently dropped. The exact trial driver source snapshots are `/private/tmp/cfd-r29-trials-MainWindow.axaml.cs` SHA-256 `391d722178a2e379ea0e6b039719a559ca6f3fbf32a034a75de62d5156254dde` and `/private/tmp/cfd-r29-trials-WorkbenchController.cs` SHA-256 `c8b41fa10fad0cf4d82eb9a47eadfc3a9ba47c31679ccd277e9132c3b51996b6`. Review-only trial, delay and auto-close code was removed before final packaging; the normal-path metric logic remains.

| Operation and trigger | Outcomes | Successful subset p95 / max (ms) | Interpretation |
|---|---:|---:|---|
| Edit, synthetic TextChanged after direct BeginEdit | 30/30 batch cycles | 18.681 / 19.131 | Native app and UI dispatch, no OS input event |
| Preview, direct review helper using real controller | 30/30 batch cycles | 174.537 / 241.703 | Includes assessment and 15-point sampling |
| Cancel, direct review helper | 30/30 batch cycles | 16.265 / 16.371 | Accepted source restored each cycle |
| Example-ready, fresh managed process | 3/3 batch cycles | 1490.905 / 1490.905 | Managed start to batch, not `Popen` or first displayed pixel |

The following is the complete 93-record normal-path metric series in emission order. Every outcome is retained; `ms` is event/managed-start to target batch cycle. The full JSON receipt also preserves reason, section status, PID/start and raw stderr hashes.

| Launch | Seq | Operation | Outcome | ms |
|---:|---:|---|---|---:|
| 1 | 1 | example-ready | batch_cycle_complete | 1490.905 |
| 1 | 2 | edit | batch_cycle_complete | 18.681 |
| 1 | 3 | preview | batch_cycle_complete | 174.537 |
| 1 | 4 | cancel | batch_cycle_complete | 16.118 |
| 1 | 5 | edit | batch_cycle_complete | 16.399 |
| 1 | 6 | preview | batch_cycle_complete | 166.670 |
| 1 | 7 | cancel | batch_cycle_complete | 7.772 |
| 1 | 8 | edit | batch_cycle_complete | 19.131 |
| 1 | 9 | preview | batch_cycle_complete | 241.703 |
| 1 | 10 | cancel | batch_cycle_complete | 16.265 |
| 1 | 11 | edit | batch_cycle_complete | 12.831 |
| 1 | 12 | preview | batch_cycle_complete | 166.715 |
| 1 | 13 | cancel | batch_cycle_complete | 16.371 |
| 1 | 14 | edit | batch_cycle_complete | 13.806 |
| 1 | 15 | preview | batch_cycle_complete | 149.868 |
| 1 | 16 | cancel | batch_cycle_complete | 7.972 |
| 1 | 17 | edit | batch_cycle_complete | 9.281 |
| 1 | 18 | preview | batch_cycle_complete | 150.019 |
| 1 | 19 | cancel | batch_cycle_complete | 7.861 |
| 1 | 20 | edit | batch_cycle_complete | 10.129 |
| 1 | 21 | preview | batch_cycle_complete | 141.250 |
| 1 | 22 | cancel | batch_cycle_complete | 7.892 |
| 1 | 23 | edit | batch_cycle_complete | 8.576 |
| 1 | 24 | preview | batch_cycle_complete | 149.998 |
| 1 | 25 | cancel | batch_cycle_complete | 7.931 |
| 1 | 26 | edit | batch_cycle_complete | 4.369 |
| 1 | 27 | preview | batch_cycle_complete | 141.573 |
| 1 | 28 | cancel | batch_cycle_complete | 8.029 |
| 1 | 29 | edit | batch_cycle_complete | 13.467 |
| 1 | 30 | preview | batch_cycle_complete | 141.259 |
| 1 | 31 | cancel | batch_cycle_complete | 7.898 |
| 1 | 32 | edit | batch_cycle_complete | 8.789 |
| 1 | 33 | preview | batch_cycle_complete | 141.662 |
| 1 | 34 | cancel | batch_cycle_complete | 8.006 |
| 1 | 35 | edit | batch_cycle_complete | 8.011 |
| 1 | 36 | preview | batch_cycle_complete | 140.091 |
| 1 | 37 | cancel | batch_cycle_complete | 9.882 |
| 1 | 38 | edit | batch_cycle_complete | 10.244 |
| 1 | 39 | preview | batch_cycle_complete | 141.599 |
| 1 | 40 | cancel | batch_cycle_complete | 8.050 |
| 1 | 41 | edit | batch_cycle_complete | 9.441 |
| 1 | 42 | preview | batch_cycle_complete | 141.640 |
| 1 | 43 | cancel | batch_cycle_complete | 8.256 |
| 1 | 44 | edit | batch_cycle_complete | 9.065 |
| 1 | 45 | preview | batch_cycle_complete | 141.642 |
| 1 | 46 | cancel | batch_cycle_complete | 7.840 |
| 1 | 47 | edit | batch_cycle_complete | 9.182 |
| 1 | 48 | preview | batch_cycle_complete | 141.332 |
| 1 | 49 | cancel | batch_cycle_complete | 7.965 |
| 1 | 50 | edit | batch_cycle_complete | 10.242 |
| 1 | 51 | preview | batch_cycle_complete | 141.656 |
| 1 | 52 | cancel | batch_cycle_complete | 7.812 |
| 1 | 53 | edit | batch_cycle_complete | 9.128 |
| 1 | 54 | preview | batch_cycle_complete | 141.481 |
| 1 | 55 | cancel | batch_cycle_complete | 7.324 |
| 1 | 56 | edit | batch_cycle_complete | 11.719 |
| 1 | 57 | preview | batch_cycle_complete | 141.433 |
| 1 | 58 | cancel | batch_cycle_complete | 7.984 |
| 1 | 59 | edit | batch_cycle_complete | 10.383 |
| 1 | 60 | preview | batch_cycle_complete | 141.550 |
| 1 | 61 | cancel | batch_cycle_complete | 7.878 |
| 1 | 62 | edit | batch_cycle_complete | 10.507 |
| 1 | 63 | preview | batch_cycle_complete | 141.663 |
| 1 | 64 | cancel | batch_cycle_complete | 7.897 |
| 1 | 65 | edit | batch_cycle_complete | 6.600 |
| 1 | 66 | preview | batch_cycle_complete | 141.943 |
| 1 | 67 | cancel | batch_cycle_complete | 7.913 |
| 1 | 68 | edit | batch_cycle_complete | 11.026 |
| 1 | 69 | preview | batch_cycle_complete | 141.583 |
| 1 | 70 | cancel | batch_cycle_complete | 7.944 |
| 1 | 71 | edit | batch_cycle_complete | 11.049 |
| 1 | 72 | preview | batch_cycle_complete | 141.673 |
| 1 | 73 | cancel | batch_cycle_complete | 8.017 |
| 1 | 74 | edit | batch_cycle_complete | 10.952 |
| 1 | 75 | preview | batch_cycle_complete | 140.058 |
| 1 | 76 | cancel | batch_cycle_complete | 9.909 |
| 1 | 77 | edit | batch_cycle_complete | 10.354 |
| 1 | 78 | preview | batch_cycle_complete | 141.782 |
| 1 | 79 | cancel | batch_cycle_complete | 7.911 |
| 1 | 80 | edit | batch_cycle_complete | 10.814 |
| 1 | 81 | preview | batch_cycle_complete | 141.658 |
| 1 | 82 | cancel | batch_cycle_complete | 7.808 |
| 1 | 83 | edit | batch_cycle_complete | 11.016 |
| 1 | 84 | preview | batch_cycle_complete | 141.550 |
| 1 | 85 | cancel | batch_cycle_complete | 7.937 |
| 1 | 86 | edit | batch_cycle_complete | 10.322 |
| 1 | 87 | preview | batch_cycle_complete | 141.626 |
| 1 | 88 | cancel | batch_cycle_complete | 7.961 |
| 1 | 89 | edit | batch_cycle_complete | 8.268 |
| 1 | 90 | preview | batch_cycle_complete | 142.468 |
| 1 | 91 | cancel | batch_cycle_complete | 7.974 |
| 2 | 1 | example-ready | batch_cycle_complete | 634.770 |
| 3 | 1 | example-ready | batch_cycle_complete | 630.827 |

The finite root CUA review matrix uses three theme launches, grouping real states within each window. **Planned, not yet observed against the final package:**

| Native launch selectors | Real paths and checks | Open evidence |
|---|---|---|
| Light, designer, Example, 1024×700 | Example edit/Preview/Apply/Cancel/history, invalid numeric; resizable minimum layout and default `NATIVE-METRIC` emission | OS input timing, clipping, focus and displayed frame |
| Dark, keyboard, Empty, 1024×700 | Open `src/CfdWorkbench.Desktop/Assets/example.foil`; refuse `/tmp/cfd-adapter-review.3sSb4H/fixtures/geometry-invalid.foil` and `geometry-unsupported.foil`; inspect source; open `/tmp/cfd-adapter-review.3sSb4H/fixtures/invalid-recovery.cfdw.json`, Preview refusal and Discard | Picker/native recovery controls, Source-tab section exclusion, disk side effects |
| High contrast, dense, reduced motion, real `file` using `src/CfdWorkbench.Desktop/Assets/example.foil`, 1024×700 | Scroll full station/CV semantics, F6/Shift-F6, keyboard re-edit, close dialog and resize | AX bridge, contrast/focus and overflow |

The timing fixture has two section stations, 14 leading/trailing rail control vertices and exactly 15 physical viewport sample queries. The dense selector checks the real annotation count is at least 16. This fixture is **not** a 50,000-triangle workload; no large-mesh latency or native display budget can be inferred. Root's supported CUA, including Source-tab behavior and real keyboard input, remains the required final native oracle. No PNG or AX artifact is fabricated.

The final source-bound [11-step gate](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-c0toimuk/receipts/verification.json), SHA-256 `028de95d10c93ec85f2c5e8f7e18fdf9d1b9101d664cf785f77a41dc778f96fc`, exited zero in all steps after removing unused trial completion plumbing and making production emission use the tested serialization helper. It binds MainWindow source SHA-256 `5f6fcdf28b7c148004921d8656ed042e9faa04b3928c4989bde98aff55053338` and verifier source SHA-256 `7e2410d6b98733a414ef1d9b1a91ef3986b71744d628b6f16d72848b3c871dfa`. All seven restore asset roots were task-local; 2,210 artifact files had zero symlinks; source inputs and prior source-tree generated outputs were hash-unchanged; every exact-owned step process group was quiescent with no observed collector. The resulting [macOS `.app`](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-c0toimuk/packages/osx-arm64/CFD%20Workbench.app) has Desktop DLL SHA-256 `b376998c7912e666bdba765711cc86e6000dc45776d3e48f2df555429fa7539a`; the [Windows folder](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-c0toimuk/packages/win-x64/CFD%20Workbench%20Windows) has Desktop DLL SHA-256 `639c7648d0a4318352e4b7838feac99c4d85f99b9d0e801fa762bbc411ddccf5`. This package has not received the final root CUA matrix or Windows native runtime review.

Verdict: **BLOCK for native UI acceptance and join** pending root's CUA rerun of the changed package and remaining minimum-window, accessibility, dialog and timing checks. The final changed-source gate and Windows cross-build passed; Windows runtime, signing and distribution trust remain Not assessed.

## R37 current source-bound package and close-lifecycle control

An actual two-window RED showed a `WorkbenchController.Changed` callback queued for the first window before `Close`; its `Closed` handler disposed the controller, then that callback ran while the second window was open. The full stack in the retained [RED receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r34-focus-odr6emvg/receipts/focus-diagnostic.json) reaches `MainWindow.Refresh` → `WorkbenchController.HasRecovery` → core `AuthoringSession.Snapshot` and raises `DOC-CLOSED`. The identities in raw stdout distinguish both windows and controllers. The repair marks the first window closed before unsubscription and disposal, and guards queued/direct refresh and its asynchronous continuation. It does not alter core's `DOC-CLOSED` refusal.

The [targeted GREEN receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r34-focus-wumancrq/receipts/focus-diagnostic.json), SHA-256 `ebec856fda2fa4ccaf637392e497a66cde554fb18a04b3fe6fdccef58ed37921`, binds MainWindow source `30ddc24c213208e09c06fd94de3cb7e13fc5c812c5e8b7102b96e42ddc9749db` and test source `484ddccd2f0508ea21982630f1aa28e2ed876582aa507175c283b28a435341c8`. The queued callback produced no `DOC-CLOSED`; the second window published six updates and a new accepted identity. A real dirty-close modal was closed with its safe default Cancel result, leaving the second window visible with its accepted identity and owned draft; a later draft numeric update succeeded. This tests window-manager dialog close, not the physical Escape key.

The one R37 same-process [applied-theme matrix](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r30-theme-targeted-cdr_j4ds/receipts/theme-targeted.json), SHA-256 `2dc82887c210ca0b0b135e51c678d0208cfbcca56119fbf99c676db518b2259e`, measured **72/72 declared enabled text/focus rows** across Light, Dark, HighContrast and Default against four actual MainWindow instances in one process. Each focus-tab placement record has a current 189×48 target/adorner, 2-pixel inner inset, exact target binding, clipping and opaque semantic paints. Independent root arithmetic found minimum text 6.28577:1 and focus 4.01698:1, above unchanged 4.5:1 and 3:1 floors. The actual template/painted-backdrop extractor rejects missing, duplicate, alpha, NaN, weak ratio, wrong-target and missing/clipped focus evidence. Disabled numeric input is separately recorded and exempt. This managed window measurement does not prove OS compositing, screen-reader traversal or physical keyboard behavior.

The final changed-source [12-step gate receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-ddpasr4w/receipts/verification.json), SHA-256 `9808e586a57281d29d893e6007e6bdf8fea39dce6e2ef3f88ab67ef3aab59b66`, exited zero for XAML lint, solution build, CLI and Desktop tests, applied-theme matrix, native XAML startup smoke, four self-contained publishes and two packages. It binds 53 source/config/fixture input hashes unchanged before/after; all seven restore `project.assets.json`, package folders and outputs are under its unique task-local scratch. It records 2,223 artifact files, zero symlinks, unchanged old source-tree outputs, no observed collector and no remaining owned process group for any step. The package macOS Desktop/Core/Persistence DLL SHA-256 values are `97ef6a0844733c91a8e9d238dcd8ceb4731275312ead2a090c7e25387b9996d0`, `5d2729412961fb4eb818f34594aa932d8d1ec8b9a5e9adcebbc64607629588`, and `f0dec872409f59d9983cbb809df7e8e20e14886c8fa77dafe2b836bf55c6fb38`; the Windows Desktop DLL is `f6097a794889145bf326db0c2edad6fba6c768d6401c2d0e3a6bb96e4cba3c98`. The [macOS package](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-ddpasr4w/packages/osx-arm64/CFD%20Workbench.app) and [Windows portable folder](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-ddpasr4w/packages/win-x64/CFD%20Workbench%20Windows) are review candidates. Native Light/Dark/HighContrast CUA, actual unsaved Cancel/input and Windows runtime remain open; the native UI veto is **not** cleared by these managed measurements.

## R38 pointer-state contrast · targeted proof

Root twice observed the selected FoilDSL tab label disappear under a real High Contrast pointer hover in the prior native package. The [loaded-control RED](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r34-focus-jx_6palt/receipts/focus-diagnostic.json), SHA-256 `22162b18b8ac539792b3be76276cfe0cd074e5dbb9586f68a71a878f2f289bbf`, raised Avalonia's public `PointerEnteredEvent`, observed `IsPointerOver`, and measured `#CC000000` foreground over a transparent template root. Alpha 204/255 prevents a valid opaque contrast ratio, so the oracle refused the paint. A separate routed press/release/exit spike established those framework state transitions. These managed events do not prove OS pointer delivery. For unselected tabs and list items only, the pressed row uses an explicit `route=styled` pseudo-state because a real press changes selection; each row records its route.

The frozen extension keeps the preceding 18 rows and adds 60 per theme: selected/unselected Section and FoilDSL tabs at rest, hover, pressed, return and focus-hover; Example toolbar, Save/Discard/Cancel, and selected/unselected station/CV items at rest, hover, pressed and return; enabled owned-draft NumericInput and active read-only SourceText at rest, hover, return and focus-hover. Four actual MainWindow instances must emit **312 applied rows and 240 state records**. Source text is measured within its actual scroll clip. Disabled numeric is recorded separately and exempt. Text and focus floors remain 4.5:1 and 3:1.

The [first expanded matrix RED](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r30-theme-targeted-0jlsv9ov/receipts/theme-controls-fail.json) found undeclared focus-hover probes, a clipped Source backing error, a selected FoilDSL focus-state error, and NumericInput hover ratios 1.093:1 (Light/Default), 1.148:1 (Dark), and 1.000:1 (High Contrast). Build and baseline test passed; controls exited -6. A later build-only RED found `AVLN3000` because `TextPresenter.Foreground` is not an Avalonia property; the repair uses attached `TextElement.Foreground`. Neither failed run left an owned child or observed collector.

The last allowed complete [targeted matrix GREEN](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r30-theme-targeted-yv5sztuy/receipts/theme-targeted.json), SHA-256 `7a092abba8da2a4979e03474239889dc155d27ccad1518883d5a8183e95fa78c`, records **312/312** applied rows, **240/240** state records, zero row failures, build/test/controls exits zero, no observed collector and quiescent owned process groups. The verifier independently recomputed minimum text **6.28577:1**, focus **4.01698:1**, and selected High Contrast FoilDSL hover **19.556:1**. All 13 parser mutations were refused (missing, duplicate, alpha, ratio, NaN, focus placement/target/clip, state missing/duplicate/wrong/paint mismatch, Default-only). Source SHA-256: Styles `716bf5ff9cdafeb8a800daa5eec1ba08769d900ff1afb3ffcbff261b6971e70c`, tests `a5575acbcde6a90975e0a1ba78ac62947a7b095bf0b1062c387c086bc9235ce1`, verifier `17d49f57022ffb5ebeed701d688a7318096ea09e9fe072daf9115e0c95274d12`. Desktop DLL `530f1d51c3c50aeb80b856ce27153e31dc5efd5c815a295d8a3bcb41aa2505b3`, test DLL `1befdea309557dcae5d28bc293b4d20304c5b1a1d81c0908f863386c975e7a92`. A changed-source full gate/package and root's native pointer review remain open; this managed result does not clear the native UI veto.

The one final changed-source [12-step gate](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-jit8fbuw/receipts/verification.json), SHA-256 `5ff9fc6d44a91d42a8457fcb90f2d4be8ee446fffa966f5778d5eb49a60a23c4`, passed with exactly 312 applied-theme rows and 13 negative controls. All 53 source/config/fixture inputs and earlier source-tree outputs were hash-unchanged; seven restore roots stayed in unique task scratch, 2,223 artifact files had zero symlinks, and each of the 12 exact-owned process groups was quiescent with no observed collector. The 16 published binary hashes are in the receipt. The [macOS app](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-jit8fbuw/packages/osx-arm64/CFD%20Workbench.app) contains Desktop DLL SHA-256 `173fcb407b190d2ab21a71365cf82f915b446f1e0a36aea63d2934b01d7a876d`, Core `c1e300465f5f3319020db4a9d360f113b87ef95b125363d72458bb19076e7e4a` and Persistence `c60fa49bc12b68e5c857e300114758c5d971bd722dc97432eee496c71043449d`. The Windows portable output is in the same receipt; Windows native runtime remains Not assessed. Root's actual native High Contrast hover and focus review must use this changed package before C acceptance or join.

## R39 High Contrast numeric focus paint

Root's native review found a white focused NumericInput value on a white field after selecting editable leading `cv-2`, typing `5`, and moving focus. The retained [pre-fix applied-paint diagnostic](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r39-numeric-red-_zmkiimx/receipts/numeric-red.json), SHA-256 `951feced535f0dbbf31ce9518775f002a2097b2795a5e5787ae5b5218ed15a55`, identified the actual Fluent `PART_BorderElement` behind `TextPresenter`: both its focused background and the text foreground were white. The old ancestor-only backdrop oracle reported black and missed this sibling paint. The managed diagnostic dispatched framework text input; its draft generation stayed zero, so it established the paint transition, not a complete user edit. Root's CUA observation is separate native behavior evidence.

The fixed matrix preserves the earlier 312 row identities and adds 84 declared rows: eleven owned-draft NumericInput states and ten active read-only SourceText states in each of Light, Dark, High Contrast, and Default. It observes the actual template sibling, visible clipped text region, selected text, caret, focus border, and accepted/draft identity. The negative mutates only the sibling background and must be refused. A valid nonnegative generation decrease, negative generation, wrong draft, missing painter, and missing sibling negative are also refused. Framework routed pointer/text/focus events test styled control paths; OS input delivery and display presentation remain for native review.

The first [396-row candidate](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r39-theme-targeted-yy1c9g6n/receipts/r39-theme-targeted.json), SHA-256 `6e374cc009b33ec23c78e3986798de2c66c3b92daaabd23dbfc6ec83ecff9702`, passed the painted rows but exposed a stale-generation parser gap. The one allowed [corrective matrix](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-r39-theme-targeted-6dw0no60/receipts/r39-theme-targeted.json), SHA-256 `11111d15ec1d681a3a559652d8457b0e65865a281119777629b3f164c7a932be`, passed **396/396 applied rows, 240 state facts, 84 TextBox facts, four sibling-only negatives, four focus-placement records, and 21 parser mutations**. Build, test, and control exits were zero, with no observed collector or remaining owned process group. Source SHA-256: Styles `07426296ac49ccea9cd50f99f2705f5e03d9d15ca6575698c5dbea9779c94a31`, tests `3a8bd20b62cd3ee8e4da482affcf36798ff25a1b49fbaae79f744361b2eff18c`, verifier `7f440309ca2ad4fc5bbd451700c766b6d9d397abe22622cc1f5dc36ed6918910`.

The one changed-source [12-step gate/package](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-rf6vnmfl/receipts/verification.json), SHA-256 `56b2617ff2ef6005f5eafb1856a3e99aa7c0ec003fef875f00d4704d1b30f642`, passed with all 53 input hashes and prior source-tree output hashes unchanged, 2,223 artifact files/zero symlinks, 16 binary hashes, and all twelve exact-owned groups quiescent without an observed collector. The frozen [macOS package](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-rf6vnmfl/packages/osx-arm64/CFD%20Workbench.app) contains Desktop DLL SHA-256 `43d2c574cdaeeeed68096a6396df42a1f04130c6cc038e30ce0f04e2fa4909d4`, Core `ba78f1e164ae639c0c8a06a647bad89d5d6d78a823cc96d19e0c78eb0c6b9503`, and Persistence `dc0673ae0c6e5968d47d92c06e9e0526877bc5c3fdc60c9e50a26166878a0426`; Windows portable output is identified in the same receipt. Native High Contrast typing, selection, blur/refocus, read-only Source behavior, Cancel, and close remain open for root CUA. Windows runtime and display timing remain Not assessed. This gate does not clear the native veto or authorize a C join.
