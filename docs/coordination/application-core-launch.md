---
id: coordination-application-core-launch
title: First-core G3 launch and prewrite receipt
type: proof-pack
status: in-review
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, implementation]
links:
  - {to: coordination-contract-b-core, rel: documents}
  - {to: coordination-application-cancel-drill, rel: depends-on}
  - {to: design-application-contracts, rel: depends-on}
review-by: 2026-10-23
summary: Records the exact Ruling 13 serial worker lease, compiled brief identity and read-only prewrite gate; implementation evidence remains pending.
---

# First-core G3 launch receipt

Owner Ruling 13 freezes the 18 paths in
[the contract](contract-b-core.md) for one serial author. The built-in
collaborator was requested as `gpt-6-astra`; its effective model is **Not
recorded** by the interface. No filesystem enforcement is claimed. The
assigned session is `cfd-application-core-20260923`, branch
`feature/application-core`, worktree
`/Users/mallalieut/projects/CFD-Workbench-feature-application-core`, base
`598716f2931cab8ed94c0e618554c340134f25b0`. The worktree was created
through `coord worktree new`, and all 18 exact paths were granted to that
session under `WI-APPLICATION-CORE` with a bounded one-hour first-run lease.

The compiled worker brief is `/tmp/cfd-core-brief.txt`, SHA-256
`373238e5adad9877711e0f6b1611589f5b10fa507ec4a4d8119a3c89043b2147`.
It binds the snapshot of `docs/coordination/contract-b-core.md` at the worker's
base to SHA-256
`fa8a9f57c76d6edcdefa67de6a98202681477873520d655cc2814eaf2f2455a9`.
The B0 design, ADR 0004 and architecture snapshot hashes are respectively
`70cd7a2712cdf297de85b0598a7b45afb10001545991a6a1b4c531b72af00199`,
`95ce99a353758ac220e0d24e2a991b72a2d887a6f0f3d8c4bc441f31dd7b8d25`
and `461446794ab24ff77d41eef842cf463592d9656bd69f3fdaa2ae138eaf296fd9`.
Later coordination frontmatter flags do not mutate this worker's base.

The worker reported its first shell command as the required `audit-log.py
start --session cfd-application-core-20260923 --skill implement` at
`2026-09-23T15:21:59Z`. It reported the assigned cwd, branch, exact base,
empty tracked/untracked status and all four matching hashes. The Coordinator
independently read back the same cwd, branch, HEAD, clean status and hashes
before sending an explicit prewrite ACK. The worker's task-local cache root is
`/tmp/cfd-application-core-20260923` with dedicated .NET home, NuGet packages
and HTTP cache, temporary and build outputs, and receipts. No `HOME` reuse or
global trust change was authorized. Builds must track child PID/start identity;
interruption stops dispatch, terminates only verified owned children and reads
back absence, per the [observed drill](application-cancel-drill.md).

**State:** prewrite gate passed. The first red build was stopped for containment
review: its Python gate resolved a task-named scratch directory under macOS
`/var/folders` instead of the approved literal `/tmp` root, and .NET printed a
first-run certificate message. The build process PID `73926`, start identity
`Wed Sep 23 08:26:08 2026`, exited 1 after 4.381522 seconds; a subsequent
`ps` found no process. Seven untracked files were all in the 18-path lease;
the compiler failed on missing APIs, so no behavioral red test ran. The
worker preserved the task-named scratch and found only a pre-existing
certificate file from May 2026; no new global certificate effect or trust
action is established. Owner request `req-01M37E2ZFFXJ7XE28JNWCN4X75`
asks whether to repair the leased gate and resume. No build retry or product
continuation occurred before that ruling. Red/green behavior, platform
capability, independent review and product acceptance remain pending actual
evidence. The 90-call/55-minute checkpoint does not permit a partial join.
