---
id: kb-visible-presentation-open
title: Presentation timing gaps and falsifiers
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, risks, verification]
links:
  - {to: kb-visible-presentation, rel: documents}
review-by: 2026-12-24
summary: Defines unverified integration, hardware and workload seams plus the negative controls required before latency trials count as proof.
---

# Open questions and disconfirmation

**Flagged:** actual renderer selection, supported access to a presentation signal,
capture authorization, input timestamp mapping, observer overhead, target visibility,
reference machines and the full workload are unqualified. No running capture
helper, Metal adapter or hardware timing experiment was created here.

assume: a supported window/display capture route can correlate final required
regions without changing the app renderer. Confirm with a reviewed spike and
calibrated timestamps; if false, the capture option cannot qualify M1 timing.

assume: the chosen presentation event can be associated with the intended frame
generation. Confirm with old-generation, partial-region, dropped-frame and
superseded-operation negatives; if false, a timestamp remains an unrelated fact.

The spike must refuse: wrong window/process; stale accepted/source/draft identity;
partial inspector/status/canvas update; unchanged or skipped frame; zero Metal
presentation time; incomplete/blank/suspended capture; unknown clock conversion;
timeout; offscreen/occluded target without a proven visibility convention; and
late Preview replacing Cancel. An injected known delay must move the observed
bound, not merely increase a CPU or callback timer.

**Disconfirmation sought:** “use Metal's timestamp” looks direct, but the pinned
default tries OpenGL first and the Metal drawable is internal. The XML/comment
omits Metal from the fallback list; the actual initializer includes it between
OpenGL and Software. This source contradiction was found before commit and
corrected here; documentation is not runtime backend proof. “capture means displayed” has
an official WindowServer event definition, but does not alone prove clock alignment,
physical panel response or correct product pixels. “the batch already correlates
identity” is true in current code, but correlation does not move its endpoint to
the display. These limits remain even if a prototype returns a fast number.

An attempted source URL inferred from the option type name returned 404 and was
discarded. The repository tree inventory located the real extension source.
This is the existing NG-LOCAL class: a type name is not a verified filename.

**Next decision:** Owner chooses an exact bounded spike and failure oracle after
independent review. Any required host/permission questions should be concrete and
batched, while Windows W0 and other dependency-ready work continue. This packet
does not authorize a permission prompt, a framework fork or an acceptance waiver.
