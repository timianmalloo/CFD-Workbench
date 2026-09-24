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
