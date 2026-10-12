---
id: proof-win-gpu-f1-recovery-integration
title: "F1 packet repair and cleanup recovery: offline integration proof"
type: proof-pack
status: proposed
owner: "@pc-f1-r217-recovery-integration-author"
phase: preflight
tags: [windows, recovery, offline, integration]
links:
  - { to: plan-windows-gpu-f1-recovery-integration, rel: depends-on }
  - { to: review-windows-gpu-f1-cleanup-recovery-cycle1, rel: depends-on }
  - { to: review-windows-gpu-f1-live-defect-repair, rel: depends-on }
  - { to: review-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: note-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  Offline candidate combines both accepted sources and retains the failed attempt.
  Combined suites and conservation controls pass. Fresh independent exact-byte
  review and a new Mac cleanup ruling remain required; no execution is authorized.
---

# Proposed integration, pending independent review

Parents, in commit order:

- Cleanup recovery: `02485495951fbd04ea07a2e8bc276b0df4ab93cd`.
- Packet repair: `65456e1cc858ff62b87c7a8d7030a6e83715b354`.

Their merge base is `6445bb6e1c6b9f27969724c711cc2ff322f01867`.
The two parent ACCEPTs retain their original scopes; neither accepts this join.
No WSL command, live wrapper, recovery entry point, retained-evidence mutation,
guest deletion, lifecycle action, download, install, acquisition, build or CFD
workload ran. Only host-only tests and offline Git/file inspection ran.

## Resolved seams and exact-byte conservation

**Verified; high confidence:** [source-hashes.json](source-hashes.json) records
bytes and SHA-256 for every one of the 15 packet files, all three recovery helpers,
the unchanged 35-test suite and historical bindings. The reproducible
[verify_integration.py](verify_integration.py) checks the map against exact Git
blobs and emits measured duration on its normal path. Run:

```powershell
py -3 -B docs/proof/win-gpu-f1-preflight/integration/verify_integration.py
```

Thirteen packet files exactly equal `65456e1c`. `preflight.sh` exactly equals
`02485495`, retaining only the reviewed `delete-debs` change. The shared Python
self-test is the exact Git three-way union: strict URI fixtures plus the unchanged
cleanup scope oracle and its planted non-deletion mutation. URI matching still
decodes only the final URL segment once and retains raw URL/cache keys separately
from signed-index filename/size/hash. Canonical checkpoint paths and distinct
`*Action` callback names remain exact packet-parent bytes; actual-body PowerShell
fixtures pass through the real finalizer. No reviewed safety control was removed.
The non-deletion shell prefix/suffix hashes remain the review's fixed values.

The audit merge helper preserves every complete parent record unchanged, once per
audit ID. The defect-register helper configuration pointed outside this checkout:
the primary helper's Git blob was `94fa12282700093b573ebfb75206e30cce91412b`,
while the accepted candidate helper was
`caa877ce432571df602f3a84a2ddcd6dd9983d1b`. The first conservation check rejected
that external helper's reordered register. The register was recomputed with the
candidate helper without changing either helper or shared Git configuration.
The control checks its helper blob and exact merged parent portion, all **212
unique parent entries once**, unchanged/extended bodies and helper-defined order.
Only the separately recorded integration lesson follows that parent portion.
Derived audit/index views were regenerated with repository tools.

The probe's initial corpus assertion incorrectly required every recovery capture
to exist in the older packet parent. The corrected check first inventories each
parent: the full cleanup-parent corpus must remain exact, and every common
packet-parent capture must also match. No source/evidence was changed to satisfy
that correction. This class is recorded in the always-loaded defect register and
the executable conservation control fails on dropped, duplicated or changed data.

**Verified; high confidence:** all 75 original checkpoint payloads plus manifest
and all 93 failed-live payloads plus manifest retain exact file sets and bytes.
The real packet binder reports **76** and **94** committed blobs against the
cleanup parent. Their Git tree IDs are `0e1054281495e3677b1948b1a9bc5ab15a0e9bb0`
and `b08d17f2eeced7e2291bf2fb262d8b44cddf441b`.
Historical VETO SHA-256 remains
`2be0ba9c514056cac232126c8cb4f13fa57f618623414f81ef8ec94efea0214b`;
invocation contract remains
`f109d4f253f436d8b178e40ea5af497316faf36b0c4398c8a598abc52748336c`.
The contract still binds its earlier accepted packet; it was not rewritten to
admit this candidate. The failed attempt remains `stage-simulate exited 5`,
checkpoint unproved and cleanup withheld.

## Observed offline checks

| Check | Result |
| --- | --- |
| Recovery tests | 35/35 PASS; 0.392 s |
| Merged Python packet suite | PASS; 0.544 s |
| Windows PowerShell / PowerShell 7 packet suites | PASS; 0.389 / 0.266 s |
| Git Bash syntax, four packet shell files | PASS; explicit Git Bash, no WindowsApps/WSL Bash |
| Authority planted negatives and packet scan | PASS |
| Transaction / manifest / completed-guard self-tests | PASS |
| Real checkpoint / failed-live manifest bindings | PASS; 76 / 94 blobs |
| Integration conservation and authority negatives | PASS; first full retained report 8.027 s |
| Merge-helper self-test and merge-binding gate | PASS |
| Inline-WSL gate | PASS |
| Offline-safe verify bundle | 10/10 PASS; application-core, application-adapters and Windows-store explicitly excluded |
| Proof privacy | PASS; zero raw user paths/SIDs, one existing allowlist, 365 binaries skipped |

Final docs, graph, privacy and gate observations are in the closing audit. These
checks do not establish WSL fidelity or live cleanup success. New conservation
control belongs to the **offline integration ring**, measured about eight seconds;
it is not added to the routine every-push gate. The final merge changes only docs
paths relative to its first parent, so the repository's docs-only join ring rule
applies; no product code, tests, tools, cases or build files changed.

## Authority limits and residual risk

**Verified; high confidence:** no cleanup-authorization JSON exists in the
inspected source corpus. Actual `validate_authorization()` refuses missing,
stale-ruling and broadened-action fixtures. Rulings bytes remain unchanged; no
new cleanup permission is created. Recovery still requires a separately committed
accepted exact source/identity/epoch contract and a new numbered hash-bound Mac
`F1-CLEANUP-ONLY` ruling. No retry authority follows from integration or source
acceptance. Fresh Astra review of the resulting immutable candidate is pending.

**Flagged:** repeated registration observations do not close the race before a
name-based WSL terminate/unregister. No-follow directory descriptors prevent
ancestor redirection but do not make file-stat/unlink atomic. Exclusive machine
ownership is not enforced by this source. Actual WSL startup/group reap, Linux
directory-descriptor behavior, registry/mount/cache mutation, current host state,
protected runtime invariance and deadline-incomplete cleanup remain unverified.
Future execution can fail with unknown live state and incomplete evidence binding;
success remains withheld. The Mac/operator decision must address these boundaries.

The seven-node plan delivered source conservation → combined offline proof →
proof/audit/derive → immutable candidate. One register seam was corrected; zero
packet/recovery repair cycles occurred. Duration is measured by the audit marker;
tokens and speedup are not recorded. The independent review node remains pending.
