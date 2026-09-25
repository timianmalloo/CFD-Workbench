---
id: kb-visible-presentation-methods
title: Presentation measurement methods
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, evidence]
links:
  - {to: kb-visible-presentation, rel: documents}
review-by: 2026-12-24
summary: Distinguishes renderer completion, OS presentation signals and calibrated frame observation for this pinned native application.
---

# Measurement methods

**Verified documentation:** Apple's [drawable presentation API](https://developer.apple.com/documentation/metal/mtldrawable/presentedtime)
reports host seconds for a displayed drawable; zero also covers skipped frames.
The [handler](https://developer.apple.com/documentation/metal/mtldrawable/addpresentedhandler(_:))
provides a place to observe it. This is a display-system contract, not a measured
physical panel response or a guarantee that every required application region
contains the intended generation.

**Verified documentation:** ScreenCaptureKit's [displayTime](https://developer.apple.com/documentation/screencapturekit/scstreamframeinfo/displaytime)
identifies the WindowServer frame event. The installed SDK defines its unit as
mach absolute time. **Inferred use:** correlate captured final regions with the
accepted/draft identity, then calibrate that clock against input/process start.
Callback arrival includes delivery work and must remain a separate diagnostic.

**Verified source:** pinned Avalonia's [IOSurface path](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/native/Avalonia.Native/src/OSX/rendertarget.mm)
flushes GL work and hands surfaces to a Core Animation layer; queued intermediate
surfaces can be discarded. Those transitions alone do not identify a final
displayed product frame. Its [Metal path](https://github.com/AvaloniaUI/Avalonia/blob/ebc60563a2f1037aba771d206af804be2901bea8/native/Avalonia.Native/src/OSX/metal.mm)
owns the drawable internally and schedules presentation when its rendering session
ends. No presented callback is wired in that inspected path.

**Flagged integration:** obtaining a supported app-specific presentation observer
without maintaining a framework fork remains unresolved. A display refresh timer,
GPU completion or a submitted transaction is insufficient by itself. The spike
must try to disprove operation-to-final-frame correlation, not merely return a
nonzero timestamp. No new framework version or renderer is selected here.
