---
id: kb-visible-presentation-sources
title: Visible timing source ledger
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, sources]
links:
  - {to: kb-visible-presentation, rel: documents}
review-by: 2026-12-24
summary: Primary API and pinned implementation sources checked on 25 September 2026; source verification is kept distinct from runtime measurement.
---

# Source ledger

Access date: **2026-09-25 UTC**. Verified means the cited text/code was read;
runtime qualifications remain explicitly unverified.

| Source | Evidence and boundary |
|---|---|
| [Apple SCStreamFrameInfo.displayTime](https://developer.apple.com/documentation/screencapturekit/scstreamframeinfo/displaytime) | Official definition of the WindowServer frame display event. Does not identify this application's operation. |
| [Apple MTLDrawable.presentedTime](https://developer.apple.com/documentation/metal/mtldrawable/presentedtime) and [handler](https://developer.apple.com/documentation/metal/mtldrawable/addpresentedhandler(_:)) | Official host-time semantics, skipped/unpresented zero, and callback contract. No current app integration claimed. |
| Installed `MacOSX.sdk/.../Metal.framework/Headers/MTLDrawable.h`, lines 42–59 | API available macOS 10.15.4; SHA-256 `6ded45a7ca00e7ff0e8cfbe9799aedc0d84bcb65c35c679da2eadf0c5e777eaa`. |
| Installed `MacOSX.sdk/.../ScreenCaptureKit.framework/Headers/SCStream.h`, lines 40–57, 238–240, 418–421 | Frame status, requested cadence and mach absolute display-event units; SHA-256 `11633abf2df86bd6c92345a9c4804a0746e18d4db39994ae29243671f332825b`. SDK root observed through `xcrun --show-sdk-path`: `/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk`. |
| Pinned Avalonia 11.3.14 `Avalonia.Native.xml`, RenderingMode docs | Comment says OpenGL then Software; SHA-256 `5cddb5f19217d0e351b47aa19ca195bf9d5fb772e618c2093268e38a7f7b0649`. Read from the frozen R39 task-local NuGet directory. **Contradicted by initializer below.** |
| [Pinned platform options initializer](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/src/Avalonia.Native/AvaloniaNativePlatformExtensions.cs#L55) | Lines 67–72 order OpenGL, Metal, Software, while line 60's comment omits Metal. Actual selected renderer remains unobserved. |
| [Pinned native Metal implementation](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/native/Avalonia.Native/src/OSX/metal.mm#L166) and [interop](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/src/Avalonia.Native/avn.idl#L1144) | Internal drawable ownership/presentation; inspected interop lacks a presentation callback. GitHub tree API resolved tag 11.3.14 to `ebc60563a2f1037aba771d206af804be2901bea8`. |
| [Pinned IOSurface implementation](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/native/Avalonia.Native/src/OSX/rendertarget.mm#L168) | Layer submission and intermediate-surface queue behavior; not an observed app renderer. |
| Project R39 `0d4a59064bec6dac15338f5e6c9869ee9c0b338f`, `Program.cs`, `MainWindow.axaml.cs:446–549` | Managed start and fresh batch endpoint. Read in the isolated native-author checkout; C is not yet product-joined. |
| [Approved milestone decision](../../notes/m1-scope-decision.md), [product A8.1](../../specs/cfd-workbench-v1.md), [attachment investigation](../../investigations/review-window-attach.md) | Acceptance, fixture/host requirements and observed investigation host/display; no replacement of the 16 GB reference requirement. |

Search results from third-party bindings and forums were not used as load-bearing
sources. No public-source statement is promoted to measured behavior on this host.
