---
id: coordination-two-machine
title: "Two machines - the Mac session and the Windows PC session, and how they meet in git"
type: plan
status: accepted
owner: "@timianmalloo"
tags: [coordination, windows, macos, git, rendezvous, pull-request]
links:
  - { to: rulings, rel: implements }
  - { to: coordination-windows-runtime-route, rel: relates-to }
  - { to: design-guided-solver-setup, rel: relates-to }
  - { to: adr-0012-openfoam-backend-macos, rel: relates-to }
  - { to: coordination-pc-kickoff, rel: relates-to }
review-by: "2026-11-06"
summary: >-
  Ruling 106. The Mac session is the only leader and owns Mac-only and shared work; the Windows PC session owns
  Windows-only work (setup, smoke test, Windows Save/Open, WSL OpenFOAM and native SU2, NACA 0012 runs, L3, cfMesh).
  The PC pushes win/* branches and opens pull requests; the Fable owner on the Mac reviews them; the Mac leader merges
  through conductor-join. Messages travel in docs/coordination/xmsg.jsonl through tools/xmsg.py.
---

# Two machines: the Mac session and the Windows PC session

Ruled by the operator on 2026-10-06 ([Ruling 106](../notes/rulings.md)). Both sessions read this file at grounding.

Labels: **Verified** = observed on 2026-10-06; **Inferred** = reasoned, not observed.

## Who does what

| | Mac session (leader) | PC session |
|---|---|---|
| Machine | macOS arm64, this repo's primary checkout | Windows x64 (Inferred until W-0's host survey) |
| Seats | Leader and Coordinator; the Fable owner runs here as a sub-agent | Coordinator of its own tracks only |
| Moves `main` | **yes, the only one** | **never** |
| Numbers rulings | **yes, the only one** (`coord-decide.py rule next` reads the local maximum; two machines would mint the same number) | never; it posts a `decision-request` message |
| Pushes | `main` and its own branches | only `win/*` branches |
| Owns | Mac-only and shared work: the DX build, group move, copy fixes, adaptive panels, the 1280x800 layout, the Run/Results backend core and macOS adapter, guided setup's shared flow and macOS route, export, macOS signing, test-ring cost thresholds | Windows-only work, W-0 to W-5 in [pc-kickoff.md](pc-kickoff.md): setup, smoke test, Windows Save/Open, the WSL OpenFOAM and native SU2 routes, NACA 0012 runs and the SPIKE-04 L3 run, the cfMesh tip spike |

**Paths.** The PC writes only: `src/CfdWorkbench.Persistence/**` for its Windows code path (the macOS path stays as it is),
Windows-only files it creates, `docs/proof/win-*/**`, `cases/win-*.yaml`, `docs/coordination/xmsg.jsonl`, and the
`docs/design/guided-solver-setup.md` rows it turns from Inferred to Verified. Anything else is a seam: post a `handoff`
message naming the file and why, and wait for the Mac to answer. The Mac does not edit the PC's paths while a `win/*`
branch that touches them is open.

## The rendezvous

```
PC:  git fetch && git merge origin/main      (start of every task)
PC:  work on win/<topic>, commit at checkpoints, git push -u origin win/<topic>
PC:  gh pr create --base main --head win/<topic>   (body: goal, done-when, evidence paths, receipt sha)
PC:  python tools/xmsg.py post --to mac --kind pr-ready --ref "PR #<n>" --text "<one line>"; commit, push
Mac: git fetch; python3 tools/xmsg.py unread --mark
Mac: Fable owner reviews the PR diff and evidence -> verdict as a PR comment + docs/reviews/pr-<n>.md
Mac: on APPROVE, conductor-join merges origin/win/<topic> into main, readiness green, push
     (GitHub marks the PR merged when its commits reach main)
Mac: xmsg post --to pc --kind review|done --ref "PR #<n>"
```

- **Why a comment, not a GitHub approval (Verified).** Both machines push as `timianmalloo`, and GitHub does not let an
  account approve its own pull request. The repository is **public** (`gh repo view`, 2026-10-06), so the review text
  is public too: no secrets, paths under a home directory are fine, no credentials or tokens in a receipt.
- **Readiness.** `main` moves only on green `python3 tools/run-readiness.py --check` on the Mac. The PC's tests are its
  evidence, not the gate: the ring's cost thresholds are tuned to the Mac's 16 CPUs, and Save/Open fails on Windows
  until W-2 lands (`ProjectStore.cs:266`, `DOC-UNSUPPORTED-PERSISTENCE`, Verified).
- **Windows evidence.** Each PR carries `docs/proof/win-<topic>/receipt.md` naming the commit it tested (`git rev-parse
  HEAD`), the Windows build, the SDK, and the commands with their exit codes. The Fable owner checks that the receipt's
  sha is the PR head or an ancestor whose later commits touch no tested path.
- **Decisions.** The operator answers whichever session asked. The PC posts `decision-request` (the question, options,
  recommendation, and the operator's answer if given there); the Mac files it through `coord-decide.py` and posts the
  `ruling` number back. Spec, copy and mockup decisions stay the operator's; the Fable owner rules the rest only under
  the operator's delegation.

## Messages: `tools/xmsg.py`

`.agents/mail/` and `.agents/requests.jsonl` are git-ignored (Verified, `.gitignore:14,25`), so the coord tools'
messages never reach the other machine. `docs/coordination/xmsg.jsonl` does: one JSON line per message, union-merged by
the `coord-register` driver (bound in `.gitattributes` and `.agents/artifacts.yml`, checked by
`check-artifact-bindings.py`). Kinds: `handoff`, `pr-ready`, `review`, `decision-request`, `ruling`, `blocked`, `done`,
`note`. The machine comes from `CFD_MACHINE` or the OS. `unread --mark` records what this machine has seen in the
git-ignored `.agents/xmsg-seen.json`. Its self-test runs in `check-docs.py`.

Read `unread` at the start of every task and after every `git pull`. A message is not delivered until it is committed
and pushed.

## Setup the PC needs once (each clone carries its own `.git/config`)

1. Git for Windows, with `git config --global core.autocrlf false`. The repo forces LF (`* text=auto eol=lf`), and
   content hashes and pinned sha256 values break on CRLF.
2. `python docs/ai-forward-pack/scripts/coord-core.py install` **in the PC's primary clone** (never in a worktree).
   Without it every register file (`rulings.md`, `audit-log.jsonl`, `xmsg.jsonl`, `docs-index.js`) conflicts on merge,
   because the merge drivers live in `.git/config`, which is not cloned. Then `coord-core.py doctor`.
3. .NET SDK **10.0.203** exactly: `global.json` pins it with `rollForward: disable`.

## What stays out

- No `refs/coord/leader` push: one leader needs no cross-machine lock.
- No second ruling register and no second audit store: the PC appends to the same `audit-log.jsonl` (union-merged;
  ULID ids do not collide).
- No GitHub Issues as a mailbox: the repo is public, and the message log already rides the commits it describes.
