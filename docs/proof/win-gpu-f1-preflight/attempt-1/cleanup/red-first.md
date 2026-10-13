---
id: proof-win-gpu-f1-r230-cleanup-red-first
title: "R230 cleanup candidate red-first record"
type: proof-pack
status: complete
owner: "@pc-f1-r228-cleanup-author"
phase: preflight
tags: [windows, cleanup, red-first]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: relates-to }
review-by: "2026-10-13"
summary: >-
  The accepted cleanup candidate admitted target/protected BasePath aliases;
  three inert regression probes were red before repair and green after.
---

# R230 cleanup candidate red-first record

Before the isolated source was added, Git could not resolve the required entry
point at the branch baseline:

```text
$ git show HEAD:docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup.py
fatal: path 'docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup.py' exists on disk, but not in 'HEAD'
```

The accepted historical recovery source is bound to old target GUID
`{e958c4d5-ae78-4b50-a634-7102f334d3aa}` and includes `delete-debs`. Reusing it
would violate the exact R228 target identity and cleanup-only scope. The new
offline suite exercises the isolated source with inert boundaries.

## Repair cycle 1: registration-path aliases

At accepted candidate tip `115f3b05f5b4b4f8d13c22e9a7b40e1fbc62c519`,
three new production-boundary probes failed because no `ValueError` was raised:

```text
test_identity_target_path_alias_refuses ... FAIL
test_identity_protected_path_alias_refuses ... FAIL
test_absence_protected_path_alias_refuses ... FAIL
Ran 3 tests in 0.001s
FAILED (failures=3)
```

Each fixture retains the exact target and protected rows, then adds a third
registration with a distinct name/GUID and one reviewed BasePath. This proves
the failure is path multiplicity across all registrations, not a missing named
row. After the repair, all three probes pass and the complete offline recovery
union passes 64 tests. No WSL boundary is imported or called by these probes.

## R230 activation adaptation

The R230 follow-on changes the authority and identity seams. Three offline
probes require the canonical `cleanup-authorization.json`, prove lifecycle
arguments come from the validated authorization identity, and reject either
leader sample below 200 seconds or outside exact Mac epoch 8. The complete
offline recovery union now passes 67 tests. No probe imports `winreg` or starts
WSL. The accepted R229 alias regressions remain in the union unchanged.
