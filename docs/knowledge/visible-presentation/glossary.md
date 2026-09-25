---
id: kb-visible-presentation-glossary
title: Visible timing glossary
type: glossary
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, terminology]
links:
  - {to: kb-visible-presentation, rel: documents}
review-by: 2026-12-24
summary: Separates batch completion, OS presentation, observation and physical display response so timing evidence cannot silently change meaning.
---

# Terms

- **Batch completion:** the current toolkit diagnostic endpoint; not a displayed
  pixel observation. Verified in [native review](../../reviews/ui-application-native.md).
- **Presentation timestamp:** an API-defined display-system event time. Its clock
  and object must be named; it is not the handler's arrival time. Verified API
  distinction; [sources](sources.md).
- **Final visible state:** the required regions showing the correct operation and
  source/draft generation. Product-derived definition; [requirements](references.md).
- **Observation interval:** bounds on when that state first appeared, including
  missed frames and clock uncertainty. Proposed [measurement model](data-and-constants.md).
- **Physical panel response:** light emitted by a display after its internal
  processing. No measurement in this packet establishes it; do not equate an OS
  timestamp with photometric observation. Flagged scope boundary.
- **Not assessed:** evidence cannot establish the promised endpoint or bound;
  neither a zero duration nor a pass. Consistent with the existing C proof contract.
