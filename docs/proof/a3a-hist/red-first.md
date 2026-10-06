---
id: proof-a3a-hist-red-first
title: "A3a HIST selected-run feed receipt"
type: proof-pack
status: active
owner: "@trk-hist"
phase: implementation
tags: [a3a, hist, analysis, projection, feed, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
  - { to: proof-a3a-ctx-red-first, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  The projection feed follows the selected run's own revision. A geometry-Historical run keeps its verdicts, stations, root t/c and
  exact strip normals; an unheld revision reads Unavailable with a reason; a failed Evaluate keeps the previous Completed run's feed;
  a layer toggle raises LayersChanged. No schema or file-format change.
---

# A3a HIST receipt

Design: `docs/design/area3-analysis.md` §3 (stored facts only, derive on read), §3.4, §18.5 rows 13-16 and 22.
Session `trk-hist`, branch `fix/a3a-hist-feed`, 2026-10-05.

**Old revisions' bytes are held.** `AuthoringSession` keeps every accepted revision's source (the `sources` and `accepted` lists are
append-only; undo and redo move a cursor) and a reopened native file's envelope carries them all. The new read-only accessor
`AuthoringSession.AcceptedSourceOf(acceptedId)` returns those bytes, or null for an id the session does not hold. A revision that exists
only in a file this session did not load is not held, and reads Unavailable. No persistence or file-format change was needed.

**Feed.** `WorkbenchController.DeriveFeed(run, current, sourceOf)` derives verdicts, stations, root t/c and strip normals from the
source of `run.Inputs.AcceptedId`. A not-held revision returns `Labels.FeedRevisionNotHeld` and nothing else (never the current data).
Strip normals: `MethodRecord.DeriveNormals` over `VortexLattice.StripNormals`, which shares the lattice's own `SlopeNormal` with the solve.
After a Failed latest attempt the previous Completed run is fed (the run `AnalysisProjection.Build` shows as "Historical - previous result").
The feed cache key is the run id alone.

| Test | Red (before) | Green |
|---|---|---|
| `HistFeed_GeometryHistorical_KeepsOwnVerdictsStationsRootAndNormals` | no normals; feed empty after a span edit | own verdicts, stations, t/c, normals; the edited revision's normals differ |
| `HistFeed_RevisionNotHeld_UnavailableWithReasonNeverTheCurrentRevision` | no reason | reason on the feed; held control derives |
| `HistFeed_FailedEvaluate_PreviousCompletedKeepsItsVerdicts` | strips Unavailable | previous run's verdicts and normals kept |
| `HistFeed_SetLayerVisible_RaisesLayersChangedOncePerChange` | 0 events | one per change, none for a no-op |
| `AcceptedSourceOf_*` (Core, four checks) | accessor absent (compile) | earlier revision, after undo, unheld null, reopened image |

Ring 0 · about 4 s for the Desktop checks · Core checks 0.2 s.
