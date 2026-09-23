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

# Native application adapter proof · candidate checkpoint

The adapter uses the joined core API without changing core or persistence source. The macOS and Windows packages cross-build and the managed tests pass. **This is not M1 UI acceptance.** The independent macOS computer-use adapter has returned `cgWindowNotFound` for the running application, so no valid product screenshot, native AX tree, keyboard recording or layout measurement exists. The expected `application-macos-ui.png` and `application-macos-ax.json` are intentionally absent. Windows runtime is Not assessed on this macOS host.

Goal: one offline Example/open/edit/preview/apply/cancel/undo/redo/save/reopen/recovery route plus CLI identity and diagnostics. Done when root independently exercises the rendered native app, reviews AX and platform states, and Owner accepts the C proof. Out of scope: solver/export, full v7 workspace and Windows runtime inference from cross-publish. Tier T2, fan-out zero. The native medium is Avalonia 11.3.14 on .NET SDK 10.0.203; distribution candidates are a self-contained macOS `.app` and Windows portable folder. Accessibility targets are NSAccessibility and UI Automation. Official review references are [Apple macOS design](https://developer.apple.com/design/human-interface-guidelines/designing-for-macos/), [Apple keyboard guidance](https://developer.apple.com/design/human-interface-guidelines/keyboards), and [Microsoft Windows accessibility](https://learn.microsoft.com/en-us/windows/apps/develop/accessibility).

The latest source-bound gate command is `python3 tools/verify-application-adapters.py`. Its retained [receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-3mtmsc0r/receipts/verification.json) has SHA-256 `4ee26152341ceda0323b0588b00dbdb52bfae915e09c834bd7a6b50f589c443f`. It records base HEAD `21f2f5bf`, all 52 authored/source/config/fixture input hashes before and after, all seven restore maps under fresh scratch, 16 published binary hashes, 2206 artifact files, zero artifact symlinks, no observed collector, all exact-owned child groups quiescent, and all ten commands exiting zero. The source-tree generated-output inventory was unchanged during that gate. The token lint saw a nonempty XAML corpus; five critical token pairs have a measured minimum contrast ratio of 6.15:1. This only checks the declared tokens, not runtime high contrast.

The latest packages are [macOS `.app`](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-3mtmsc0r/packages/osx-arm64/CFD%20Workbench.app) and [Windows portable folder](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-3mtmsc0r/packages/win-x64/CFD%20Workbench%20Windows). Packaging proves build contents and self-contained runtimes, not launch usability, signing, notarization or SmartScreen reputation. Root independently verified an earlier package's file/hash parity and a packaged macOS CLI scientific fixture; the latter receipt is `/tmp/cfd-adapter-review.3sSb4H/cli-packaged-result/summary.json`, SHA-256 `2c64b23d...`.

| Claim | Failing condition and oracle | Evidence / red observed | Confidence and residual risk |
|---|---|---|---|
| CLI and source identity | `/1` or unsupported geometry must refuse with stable exit and unchanged bytes; admitted `/2` reports source/surface/evaluator identity and physical/normalized units | Root's 16 actual-process fixture cases and packaged scientific fixture passed; local CLI test project passed in the gate | Verified bounded cases; no whole-language conformance claim |
| Accepted/draft/recovery authority | Failed opens preserve active accepted source; definite conflict stays definite; uncertain publication remains dirty; invalid recovery is separate; incomplete numeric input cannot save | Named local controller tests; root independent three-case controller and recovery roundtrip probes passed | Verified named interleavings and fixture; rendered user comprehension still open |
| Derived view | Exactly 15 certified `PointAt` samples, selected interior η and one section query; independent upper/lower trailing edges; one physical XYZ scale; segment interpolation error labelled Not assessed | Local tests and root's eight editable LE/TE target displacement probe passed (2.829–3.333 mm for +5 mm CV edits) | Verified samples, not visual fidelity or app latency budgets |
| Semantic viewport | Native station/CV annotation controls expose full names, units, editable/locked state and stable IDs; section samples have separate labels | Managed control/peer test passes; controls are in a scrollable visual panel | Source-bound only. Native AX bridge, offscreen behavior and screen-reader traversal Not assessed |
| Keyboard and close | F6/Shift-F6 cycles enabled regions; platform Undo/Redo map; numeric Enter/Escape scoped; unsaved Save/Discard/Cancel | Named mapping tests and source readback | Actual OS key dispatch, dialogs and focus restoration Not assessed |

## Native UI-T4 proof rows

| Claim | Failing input or condition | Oracle | Evidence | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| Platform HIG | Window, file dialog or shortcut violates macOS/Windows convention | Independent platform checklist on native window | Native dialog and shortcut source; no rendered review | Open | Source only | Platform affordances may differ at runtime |
| Keyboard traversal | Core flow, recovery or unsaved dialog cannot be completed without pointer | Root CUA keyboard recording at minimum window | F6 and shortcut unit tests; dialog source | Open | Source only | Focus order/trap and default/cancel handling need live proof |
| Accessibility tree | Custom viewport lacks role/name/children or stable selected state | NSAccessibility/UIA capture with expected station/CV and section semantics | Managed peer/control checks only; no AX JSON | Open | Not assessed natively | Scrollable child bridge and offscreen behavior unverified |
| Theme/high contrast | OS Light/Dark/HighContrast makes text or focus unreadable | Runtime theme switch and contrast inspection | Token lint and measured fixed-pair contrast | Open | Source only | No platform high-contrast run |
| DPI/windowing | 1024×700, resize, mixed DPI, minimize/restore clips fields or changes focus | Runtime screenshots and keyboard at each size | XAML minimum dimensions only | Open | Not assessed | Center section/right actions may overflow |
| Large lists | More rows than viewport block input or disappear | Scroll and responsiveness measurement | Scrollable Navigator and annotation source | Open | Source only | No dense runtime benchmark |
| OS integration | Picker, dock/taskbar or file handling violates platform behavior | Native interaction and file-preservation probe | Controller disk probes; picker source | Open | Partial | Native picker was not exercised through CUA |
| Distribution trust | Unsigned/unnotarized/untrusted package is treated as release-ready | codesign/notarization/SmartScreen release receipt | Development packages only | Yes: release proof absent | Flagged | Release channel cannot ship yet |

Avalonia-specific rows: `AutomationProperties` and IDs exist on visible station/CV/source/actions, but NSAccessibility/UIA mapping is Not assessed. The XAML token gate and fixed contrast pairs pass; system theme/high contrast and focus visuals have no runtime verdict. Browser/CSS craft rules are inapplicable to this native XAML corpus, not a zero-file PASS. No generated exemplar art or third-party UI material is embedded; Avalonia packages are pinned by the project and their SBOM/license review remains an ADR-0003 release obligation.

Verdict: **BLOCK for native UI acceptance and join** pending actual macOS CUA/AX and independent review, review-harness hard states, minimum-window/overflow and timing measurements. Windows cross-build is Verified; Windows runtime, signing and distribution trust remain Not assessed.
