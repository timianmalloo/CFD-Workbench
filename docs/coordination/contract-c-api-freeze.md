---
id: coordination-contract-c-api-freeze
title: Native adapter public API freeze at joined core
type: plan
status: proposed
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, desktop, cli, contract]
links:
  - {to: coordination-contract-c-native, rel: relates-to}
  - {to: coordination-contract-b-core, rel: depends-on}
  - {to: design-application-contracts, rel: depends-on}
review-by: 2026-10-23
summary: Compiled final-core consumer and measured sparse native projection seam before adapter dispatch.
---

# C public API freeze

The source boundary is joined core `18278c4030de998d3b799ab60c0140aba4a00a3a`.
Owner Ruling 22 accepted that bounded B for integration; this freeze is an
adapter contract, not a native UI or M1 pass. Root independently reviewed the
final compiled source/output and passed the API/sample seam, while reserving
actual UI, accessibility, section-curve and performance vetoes. A task-local
compiled consumer at
`/tmp/cfd-c-api-freeze.BRrhq0/Program.cs` (SHA-256
`de6f0992ad1730aec1ff0be4494751bacbe647f5ec9a42f7d97c9a55f7cbecbf`)
and `Consumer.csproj` (SHA-256
`58df1b8c3ea9d8f47e4299b9d5b2f4889fe7fa7a8581f5476d47f8e41d472f42`)
built and exited 0 against the actual `Core` and `Persistence` project references.
The final 15-point run output SHA-256 is
`57da7c7fc9ef82b91329e73d1c8d8e50c7469e21ff62c8e86511fedf9adc8aca`;
stderr contains the expected save result only. The process finished; the
ordinary shell receipt did not record a child-PID lineage, so this run is not
the Ruling 21 lifecycle preflight.

| Need | Compiled final API and required consumer behavior |
|---|---|
| Import and IDs | `FoilSource.Parse(byte[])`, `SourceParse.Authored()`, `FoilSource.MaterializeIds(parsed)`, then `AuthoringSession.Open(source, operationId, acceptIdInsertion)`. A missing-ID open with `false` returns exact candidate bytes and retains zero accepted facts. Explicit acceptance changes that state. |
| Bound authored facts | `InspectAccepted()` returns `AcceptedInspection(AuthoredProjection, GeometryAssessment)`. Projection exposes source/revision-bound named rails, control IDs/eta/SI ordinates and locks, assignments, diagnostics and evaluator. The adapter selects IDs from this value and does not parse or guess them. |
| One assessment per projection | `InspectAccepted().Geometry.Certificate` or a current `Validate(draftId,generation,cancellation).Certificate` feeds many `Geometry.PointAt(certificate,eta,x,upper,port,timeBudget,cancellationToken)` and `SectionAt(certificate,eta,x,timeBudget,cancellationToken)` calls. `Session.Preview` revalidates for every point and is not the viewport sampling loop. A draft job retains assessment, generation and target; discard late results after cancel, update or inspected-revision change. |
| Refusal and bounds | A canceled query returns `GEOMETRY-CANCELLED`; a zero deadline returns `GEOMETRY-BUDGET`. The certificate's placement enclosure width is numeric error, not sampled mesh/chordal error. Unsupported or unassessed geometry remains read-only. |
| Save acknowledgement | Capture `session.SaveImage()` once, pass those exact bytes to `new SaveRequest(image, expectedDiskSha256, operationId)` and `ProjectStore.SaveAsync`. Call `session.AcknowledgeSaved(image)` only for `Code == OK`, `PublicationKnown`, `DurabilityConfirmed` and matching `PublishedSha256 == Identity.Sha256(image)`. Otherwise keep dirty and surface conflict or uncertain publication; reopen/compare before retry if uncertain. `ReadAsync` supplies image plus on-disk SHA for the next expected token; `Reopen(image)` owns adoption. |

The consumer used the normative `/2` basic foil fixture, observed `IdCandidate`,
explicit acceptance, certified accepted inspection, two authored rails and two
profile assignments. It validated a leading-rail draft and observed the
interior eta=0.5 leading-edge point move outside its prior enclosure, then canceled
without adoption, saved and acknowledged exact image bytes, and reopened the
same accepted source. On macOS the store refused a `/tmp` symlink-alias parent
with `DOC-UNSUPPORTED-PERSISTENCE`; the canonical `/private/tmp` task directory
returned `OK`, `PublicationKnown=true`, `DurabilityConfirmed=true`. That first
failure is retained in the scratch transcript and must not be described as a
store defect or an authorized path fallback in product code.

## Measured sparse projection and C limit

On this macOS ARM64/.NET 10.0.203 host, three passes of **15** queries at
eta = 0, 0.5, 1, with one shared leading-edge point and separate upper/lower
mid-chord and trailing-edge points per station, took 134.4902, 120.9904 and
124.2261 ms from one accepted certificate. One center `SectionAt` per pass
took 17.8969, 18.051 and 15.881 ms. One draft `Validate` took 7.8437 ms.
An earlier 18-point probe took 153.5795–186.9292 ms for points alone, plus
15.162–17.9365 ms for one section and 7.2972 ms validation. These are bounded
host observations, not a dense-grid or 250 ms UI guarantee. An earlier
12-point root/tip-only sampler was rejected in independent review because an
interior rail edit could leave every displayed point unchanged. The initial C
projection uses no more than 15 certified point queries, including an interior
station, plus one section query
per accepted/preview frame, on a cancelable background job with one assessment;
the actual native app measures edit feedback, full preview/cancel latency and
visual adequacy. It must disclose if the coarse rendered sampling cannot meet
the intended inspection or timing need. One `SectionAt` returns one section
query, not a complete 2D section curve; the native app must use a measured
bounded set of section queries or provide an honest inspectable numeric sample.
The drawn lines represent sampled
display geometry; point enclosure widths cannot justify unsampled segments.

The C author may request a typed core seam if this frozen API cannot meet the
actual UI obligation. Such a request stops only the affected adapter work;
there is no implicit B path lease or second evaluator in C.
