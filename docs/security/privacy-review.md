---
id: privacy-review
title: Offline application privacy review
type: privacy-review
status: proposed
owner: "@cfd-owner-20260923"
phase: architecture
tags: [privacy, application, local-files]
links:
  - {to: architecture-application, rel: documents}
  - {to: design-application-foundation, rel: documents}
  - {to: design-application-contracts, rel: documents}
review-by: 2027-03-23
summary: >-
  Captures identifying source comments, names, local paths and retained recovery/history for the offline slice.
  No personal-data transfer is introduced; metadata minimization and explicit local retention remain testable
  implementation obligations rather than assumed properties of the toolkit.
review-suggested:
  - { by: design-application-contracts, on: 2026-09-23, reason: "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams." }
---

# Offline application privacy review

## Personal-data inventory

| Category | Store and purpose | Retention / reader |
|---|---|---|
| Names/comments in authored source | Exact local project bytes, user design intent | Immutable history until user deletes project; parser/native literal display |
| Local file paths | File adapter/session preference, user-selected storage | Session/recent-file policy must be explicit; excluded from telemetry |
| Recovery draft | Local draft over accepted base, crash recovery | Until explicit resume/discard; never silently promoted |

M1 adds no rider data, account, payment, third-party service or telemetry exporter. User source may still
identify a person; “offline” does not mean no personal data.

B0 fixes the retention contract: full accepted history persists until user deletion, never silently pruned;
recovery persists until explicit disposition or complete admitted replacement. M1 has no durable recent-file
list. Metadata events are a local 256-event session ring discarded at close, with no source/path/vertex/name
or content-hash identifiers and no exporter. These are product obligations; executable session tests prove
recovery isolation/ownership, not runtime sink minimization. The [contract proof](../proof/application-contracts.md)
retains that distinction.

## Findings register (generated)

| source | Flow / categories | Finding | Disposition | Verification |
|---|---|---|---|---|
| [design-application-contracts](../design/application-contracts.md) | Source, IDs, path · linkability/identifiability/disclosure | Retained personal text can leak through events | Mitigate no raw text/hash/path telemetry, local-only 256-event ring; no MRU persistence | Product sink-marker test remains required |
| [design-application-contracts](../design/application-contracts.md) | History/operation receipts · non-repudiation/detectability | Local record may be mistaken for authenticated actor proof | Mitigate explicit operation-only identity; no author identity | Typed schema has no actor field |
| [design-application-contracts](../design/application-contracts.md) | Full snapshots/recovery · unawareness | Old comments and failed edits remain | Mitigate explicit retained-history/recovery disclosure and discard; no silent pruning | Recovery offer/cancel fixtures; native UI proof outstanding |
| [design-application-contracts](../design/application-contracts.md) | Local file retention · non-compliance | No server rights/deletion guarantee | Transfer OS-user ACL and user-controlled deletion; shared-device residual | No external data/account flows; native ACL proof remains |
| [design-application-foundation](../design/application-foundation.md) | Source names/comments and file paths · L/I/D/D | Identifying content could enter logs or leave device | Mitigate: metadata-only local logs; no network exporter | Marker in path/source absent from logs; offline walk |
| [design-application-foundation](../design/application-foundation.md) | Accepted history · N | Local facts might imply authenticated personal attribution | Mitigate: no authenticated-author claim; operation identity only | CLI/UI copy and schema omit fabricated actor |
| [design-application-foundation](../design/application-foundation.md) | Recovery/history · U | User unaware incomplete source is retained | Mitigate: explicit recovery offer, retained-history disclosure | Reopen invalid draft labels accepted versus recovery |
| [design-application-foundation](../design/application-foundation.md) | Local documents · N-compliance | Retention/access depends on local device policy | Transfer: OS-user filesystem ACL; explicit residual shared-device access | No egress or credential use; no application-encryption claim |

<!-- rolled up from 2 artifact(s) by docs-graph.py rollup on 2026-09-23 -->


## Rights, telemetry and transfers

The application writes user-controlled local files; users can inspect/export/delete them using normal
filesystem controls. History is not silently erased as a side effect of editing or Discard. Recovery UI
explains what is retained and offers explicit disposition. No server-side rights workflow or external
processor exists in M1. This is a product data-flow statement, not a legal compliance certification.

Normal-path telemetry contains operation IDs, bytes, statuses and timings, excluding source/path/name text.
Implementation must inject unique personal markers in those values and prove their absence from logs.
That production test is not yet run; a design table is not evidence of runtime minimization.

## Residual risks and review

No privacy-risk acceptance is self-certified. Shared-device access remains governed by the Owner-accepted
OS-user ACL posture, not application encryption. Product telemetry marker-scan and recovery UI disclosure
remain release gates. No network or credential permission is requested by this architecture. Independent
root/Owner review remains required.

`python3 docs/ai-forward-pack/scripts/docs-graph.py rollup --heading "Privacy analysis (LINDDUN-lite)" --type design --relative-to docs/security`
