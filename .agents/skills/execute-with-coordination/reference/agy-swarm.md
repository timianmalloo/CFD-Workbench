---
skill: execute-with-coordination
part: agy-swarm
---

# Antigravity (AGY) native swarm execution (`--swarm`)

Use `--swarm` when running `/execute-with-coordination` inside Antigravity (agy) and the native `swarm` tool is available in the session. It executes the exact same `docs/coordination/<plan-id>.md` plan as `--agents`, `--brief`, and `--launch` without modifying non-Agy harnesses (Claude Code, Codex, Copilot, Grok, Muse). The coordinator still owns the division of responsibility, never authors track code, and always converges through `conductor-join.py`.

## Qualification (Stage 1)

Record each dimension from first-hand observation before dispatching — never assume enforcement from a mode name:

| Dimension | Native AGY Swarm mechanism | Qualification baseline |
|---|---|---|
| Workspace isolation | `swarm{action:"add", workspace:"share"}` (linked working copy) or `"branch"` (isolated clone) | `enforced` for author separation; confirm inherited hooks/drivers with `coord doctor` inside the share |
| Read-only verification | `swarm{action:"add", workspace:"inherit"}` | `enforced` only for non-editing verifiers/reviewers; never place two authors on `"inherit"` |
| Wakeup / doorbell | Shared message board (`@name`, `@#channel`, `swarm_wait`) | `enforced` inside the swarm session (zero-token sleep) |
| Stall detection | Dedicated `monitor` worker (`swarm_monitor` skill) | `enforced` when `swarm_monitor` is in the session's skill list; otherwise `unsupported` |
| Ownership hooks | `.agents/hooks.json` (`AGENT_SESSION`) | `observed-only` until verified by a live probe (`.agents/rules/agy-surface.md`) |

If the `swarm` tool is absent or a required boundary cannot be verified, fall back to `--brief`, `--launch`, or serial execution — never downgrade silently.

## Planning topology (`prepare-for-coordination`)

Keep the canonical `## Tracks` table unchanged so every harness reads the same plan. For `agy-swarm` plans, add an optional sub-table underneath `## Tracks`:

```markdown
### Swarm topology (AGY)
| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| auth-core | auth-api | share | backend | inherit |
| auth-verify | verify-auth | inherit | backend | inherit |
```

- **Roster names:** lowercase `[a-z0-9-_]`, max 24 chars, named for the job (`auth-api`, `verify-auth`), never reserved names (`all`, `coordinator`).
- **Workspace mapping (from Stage 3 artifact classes):**
  - Tracks owning `authored` paths -> `workspace: "share"` (one working copy per author, WT1–WT5).
  - Read-only verifier / Test Architect tracks -> `workspace: "inherit"` (zero-copy diff and test inspection).
  - Destructive refactors or high-risk spikes -> `workspace: "branch"` (isolated clone).
- **Channels:** Create area channels (`swarm{action:"channel", op:"create", channel:"<area>", topic:"<scope>"}`) only when the plan has more than four tracks or distinct bounded contexts. Keep `main` quiet for cross-area rulings and final completions.
- **Model choice:** Leave `model` unset on `add` (inherits the user's chosen model) unless the user or plan explicitly requests a specific model.

## Initialization and dispatch (Stages 2–3)

1. Complete Grounding (CO-S0, `coord doctor`, plan-to-repo check). Refuse any compiled prompt with `dispatchable: false` or an unanswered `DR-n`.
2. Name the swarm immediately:
   ```text
   swarm{action:"title", title:"<plan-id>: <scope>"}
   ```
3. Add auxiliary workers in the creation turn **only if** their built-in skills exist in the session's skill list:
   - `monitor` (if `swarm_monitor` is present): brief it to load `swarm_monitor` and post only to break a stall.
   - `narrator` (if `swarm_narrator` is present): brief it to load `swarm_narrator` and never call `post_message`.
   - Never load `swarm_monitor` or `swarm_narrator` in the coordinator itself.
4. Add initial unblocked tracks under the plan's fan-out cap and five-part contract (goal, done-when, bounds/budget, deadline, fallback, termination condition). Remember **the brief is public and cold**: every swarm member can read it, and a new worker has seen nothing of this conversation unless `fork` is passed. Include verbatim in every track brief:
   - `audit-log.py start --session <track-id> --skill <skill>` as the first command (DC-190).
   - Exact goal, done-when, and quoted ADR trigger lines (DC-189) from the plan row.
   - Owned `authored` paths; explicit statement that `derived`/`register` paths need no `coord claim`.
   - Context ceiling (400k tokens default) with hand-off rule, deadline, fallback, convergence condition, required exit evidence, and not-in-scope boundaries.
   - Shell and tool discipline: absolute paths only, no `EnterWorktree`/`ExitWorktree`, multi-line scripts as files, gate status never behind a pipe (CT27).
   - For `workspace: "share"` / `"branch"` authors: commit your work on your workspace branch before reporting done, and never push to a remote unless explicitly authorized.
   - Board routing contract: post `@coordinator [done] <diff summary + observed exit evidence>` on completion (or `@<verifier-name>` when a verifier is assigned first); post `@<peer> seam: <request>` for cross-track needs; post `@coordinator decision-request: <DR-n>` when an Owner ruling is required.
5. **First-turn rule:** `post_message`, `read_board`, and `swarm_wait` arrive on the turn *after* the first `swarm add`. Add the initial workers, display `open_board` from the tool result on its own line (if non-empty), and end the turn.

## Reactive coordination loop (Stages 4–5)

- **Zero-token sleep:** Call `swarm_wait{since: <highest_seen_seq>}` instead of polling `read_board`. Always pass the highest sequence number you actually read so concurrent posts are not missed.
- **Reference by sequence number:** Point workers and verifiers to earlier board posts by `#seq` (`@verify-auth check #4`), never by copy-pasting another worker's report.
- **Seams (Stage 4):** Peers negotiate scoped seams directly via `@<peer>` and record durable cross-track contracts with `coord request add` / `resolve` when the plan requires an audit trail. If two tracks need to edit the same `authored` file, halt both and issue a boundary correction — never let two authors share a file.
- **Owner rulings (D6, CO1):** A board decision request does not replace the durable register. Run `coord decide rule next --title "<title>" --text "<decision>" --request <req-id>` so `docs/notes/rulings.md` allocates the heading, then post `@<worker> <allocated ruling heading>: <decision>` on the board. A requester never rules on its own request.
- **Adversarial verification:** Aim for one `workspace: "inherit"` verifier (Test Architect / Simplifier lens) per two to four authors. Verifiers inspect the author's share diff and tests, posting defects back to `@<author>` — verifiers never edit code in an author's file.
- **Stalls and kick ladder (CO17):** When `monitor` posts `@coordinator [stall]`, read the cited `#seq` posts and act immediately (assign the next step, re-route a mention, swap a quota-blocked model, or climb the kick ladder). Never reply to `monitor` on the board.
- **Quota failover (never kill for quota):** If a worker fails on quota or receives a `[quota]` warning, wait until `swarm{action:"status"}` shows it `waiting`, inspect `swarm{action:"models"}`, and switch it with `swarm{action:"model", name:"<worker>", model:"<model>"}` (or omit `model` to inherit your active model). `model` keeps the worker's conversation and share workspace intact; notify the user of the switch.
- **Warm replacement:** Use `fork: "<member>"` on `swarm add` only when replacing a looping worker or splitting an investigation whose read context is worth keeping.

## Convergence and safe teardown (Stages 6–7)

1. Merge tracks in DAG dependency order — upstream first, downstream rebases.
2. Verify exit evidence first-hand (E14/E16): check `swarm{action:"status"}` for the worker's workspace path and branch, inspect the committed diff and test evidence directly, and confirm `verify-ruling-citations.py` is green:
   ```sh
   python3 docs/ai-forward-pack/scripts/verify-ruling-citations.py
   ```
3. Converge exclusively through `conductor-join.py` (see `reference/join.md`) from the primary checkout against the worker's committed branch:
   ```sh
   python3 docs/ai-forward-pack/scripts/conductor-join.py <branch> --title "<merge title>" --audit-shortname join-<track> --audit-summary "<what landed>" --audit-goal "<goal>" --audit-done-when "<done when>"
   ```
4. **Teardown order (`kill` deletes the share workspace):** Never call `swarm{action:"kill", name:"<worker>"}` before `conductor-join.py` succeeds and the commits are safely landed on the target branch. Never kill a worker to resolve a merge conflict (WT11). Killing a worker does not kill helpers that worker spawned — inspect `swarm{action:"status"}` and kill any remaining helpers once their work has landed.
5. **Close the swarm cleanly:** Leave `monitor` and `narrator` running until the whole plan converges. Before reporting completion to the user, post a single line `[done] <summary>` with no `@` mention so `monitor` goes quiet.
6. Append planned-vs-actual (including swarm roster, model failovers, and seam counts) to `docs/coordination/<plan-id>.md`, run `python3 docs/ai-forward-pack/scripts/docs-graph.py derive`, and record the closing `audit-log.py append` entry.
