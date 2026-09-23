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
review-by: 2027-03-23
summary: >-
  Captures identifying source comments, names, local paths and retained recovery/history for the offline slice.
  No personal-data transfer is introduced; metadata minimization and explicit local retention remain testable
  implementation obligations rather than assumed properties of the toolkit.
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

## Findings register (generated)

| source | Flow / categories | Finding | Disposition | Verification |
|---|---|---|---|---|
| [design-application-foundation](design/application-foundation.md) | Source names/comments and file paths · L/I/D/D | Identifying content could enter logs or leave device | Mitigate: metadata-only local logs; no network exporter | Marker in path/source absent from logs; offline walk |
| [design-application-foundation](design/application-foundation.md) | Accepted history · N | Local facts might imply authenticated personal attribution | Mitigate: no authenticated-author claim; operation identity only | CLI/UI copy and schema omit fabricated actor |
| [design-application-foundation](design/application-foundation.md) | Recovery/history · U | User unaware incomplete source is retained | Mitigate: explicit recovery offer, retained-history disclosure | Reopen invalid draft labels accepted versus recovery |
| [design-application-foundation](design/application-foundation.md) | Local documents · N-compliance | Retention/access depends on local device policy | Transfer: OS-user filesystem ACL; explicit residual shared-device access | No egress or credential use; no application-encryption claim |

<!-- rolled up from 1 artifact(s) by docs-graph.py rollup on 2026-09-23 -->


## Rights, telemetry and transfers

The application writes user-controlled local files; users can inspect/export/delete them using normal
filesystem controls. History is not silently erased as a side effect of editing or Discard. Recovery UI
explains what is retained and offers explicit disposition. No server-side rights workflow or external
processor exists in M1. This is a product data-flow statement, not a legal compliance certification.

Normal-path telemetry contains operation IDs, bytes, statuses and timings, excluding source/path/name text.
Implementation must inject unique personal markers in those values and prove their absence from logs.
That production test is not yet run; a design table is not evidence of runtime minimization.

## Residual risks and review

No privacy-risk acceptance is self-certified. Shared-device access remains governed by OS-user ACLs, not
application encryption; Owner reviews that posture before release. Recent-file preference retention and
recovery cleanup need the serial contract gate's explicit policy. No network or credential permission is
requested by this architecture. Independent root/Owner review remains required.

`python3 docs/ai-forward-pack/scripts/docs-graph.py rollup --heading "Privacy analysis (LINDDUN-lite)" --type design`
