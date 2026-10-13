---
id: proof-win-gpu-f1-r229-cleanup-red-first
title: "R229 cleanup candidate red-first observation"
type: proof-pack
status: complete
owner: "@pc-f1-r228-cleanup-author"
phase: preflight
tags: [windows, cleanup, red-first]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: relates-to }
review-by: "2026-10-13"
summary: >-
  The branch baseline had no exact cleanup entrypoint, while the prior recovery
  was bound to another GUID and a broader deletion scope.
---

# R229 cleanup candidate red-first observation

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
