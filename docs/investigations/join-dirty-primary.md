---
id: investigation-join-dirty-primary
title: "Investigation: a join refuses after a register write into the primary checkout"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: investigate
tags: [coordination, conductor-join, registers, rulings, audit-log, worktrees]
links:
  - { to: defect-classes, rel: relates-to }
  - { to: coordination-next-best-six-wave2, rel: relates-to }
  - { to: investigation-desktop-native-crash, rel: relates-to }
review-by: "2026-11-22"
summary: >-
  Twice on 2026-10-22 a conductor-join refused to start because a ruling or a logged prompt had been written into the
  primary checkout and not committed. Git protects an uncommitted change on a path the branch also changes; when the
  branch does not change it, the join's own staging would sweep the change into the join commit instead. The join
  fenced the leader epoch but never the tree. The fix is a step-0b dirty-primary fence in conductor-join with a
  self-test seen red on the unfenced join, plus a stderr note from the two register writers.
---

# Investigation: a join refuses after a register write into the primary checkout

Labels: **Verified** = observed in this session with the command or file named; **Inferred** = reasoned, not observed.

## 1. Symptom (Verified)

`conductor-join.py <branch>` prints, at step 1:

```
error: Your local changes to the following files would be overwritten by merge:
    docs/audit/audit-log.jsonl
Please commit your changes or stash them before you merge.
Aborting
Merge with strategy ort failed.
conductor-join: the merge has conflicts - resolve by hand ...
```

and exits 1 with an empty conflict list. Nothing was merged. The message calls it a conflict; it is not one.

## 2. Reproduction (Verified, synthetic repository)

| Step | Command shape | Result |
|---|---|---|
| A branch appends a line to `docs/audit/audit-log.jsonl` and commits; main appends a different line and does not commit; `git merge --no-ff track` | the register write uncommitted on a path the branch changes | refused: "would be overwritten by merge", exit 2 |
| The same, after `git commit` of the local line | the write committed first | the merge proceeds to its content stage (a content conflict in the synthetic repo, which has no union driver; the real repo's driver resolves the register) |
| main modifies `README.md` uncommitted; the branch does not touch it; `git merge --no-ff track` | an unrelated dirty path | no refusal; the merge proceeds, and the dirty path stays uncommitted until the join's step 7 `git add -A` sweeps it into the join commit |

So the uncommitted write on a path the branch changes is **sufficient** for the refusal; clearing it from the working
tree first (a commit, or a stash) is what lets the merge start; and the sweep is the other face of the same state. Git
also refuses for a staged change on any path and for an untracked file at a path the branch adds (the review's probe),
which the fence covers as well.

## 3. Timeline (Verified from the session's logs)

1. 2026-10-22, the wave-2 close: Rulings 258 to 264 were each committed on main before the next join by the main
   session's sequencing, so the state never met a join.
2. The CR2 join: the Owner seat wrote Ruling 265 into the primary (`coord-decide.py rule` runs in `D:/Projects/CFD-Workbench`),
   and the main session logged an operator prompt with `prompt-log.py add`, both in the turn that started the join.
   Step 1 refused on `docs/audit/audit-log.jsonl` (`join-cr2.out`). The ruling was committed (91538934) and the join
   re-run; CR2 landed at 28e23de0.
3. The operator-decisions node: `prompt-log.py add` ran in the primary, then the join. Step 1 refused on both audit
   files (`join-opdec.out`). The prompt entry was committed and the join re-run; the node landed at d8e16c57.
4. Neither refusal lost anything: git's protection is what made the state visible. The cost was two aborted joins and
   two extra commits.

## 4. System map (Verified from source)

- `conductor-join.py` runs in the checkout it is started from (the primary by practice). Step 0 is the leader fence
  (`leader_fence`, reads the leader ref); step 1 is `git merge --no-ff <branch>`; step 7 is `git add -A`; the join
  commit follows. Before this change nothing between step 0 and step 1 read the tree.
- The register writers: `coord-decide.py rule` appends a heading to `docs/notes/rulings.md` and audit entries;
  `prompt-log.py add` and `audit-log.py append` write `docs/audit/audit-log.jsonl` and derive `docs/audit/audit-data.js`.
  None commits, by design: the Owner seat is told never to commit and the main session commits rulings after the
  citation and docs gates.
- The union merge driver for register files resolves committed content on both sides; it never sees the working tree.
- `.agents/log/<session>.jsonl` files are modified by every session event and are carried by the join's chore commit,
  so they are the one class of dirty path the join expects.

## 5. Hypotheses

| Hypothesis | Mechanism | Status |
|---|---|---|
| H1 git's protection of uncommitted changes on a path the merge updates | the proximate mechanism | **Verified** (section 2): sufficient for the refusal, and the refusal lifts once the working tree is clear of it |
| H2 the union merge driver should have resolved it | a driver acting on the working tree | **Refuted**: drivers act on committed trees; the refusal precedes any driver |
| H3 the Owner wrote into the wrong checkout | a seat placement error | **Refuted**: the Owner wrote where the practice puts rulings (the primary, committed on main after the gates); the missing step is the commit before the join, not the place |
| H4 the join has no fence for the tree (the structural cause) | a missing guard: a leader fence exists, a tree fence does not, and step 7 stages everything | **Verified**: the fence added at step 0b refuses the state, observed red on the unfenced join (section 7) |

## 6. Root cause (Verified)

The join assumes a clean primary and never checks it. A register write left uncommitted in the primary meets that
assumption in two ways: git refuses the merge when the branch changes the same path (today's two refusals), and the
join's `git add -A` would commit the write under the join's name when the branch does not (not seen today, inferred
from step 7's command). The writers cannot commit (the Owner must not, the audit scripts do not), so the commit step
lived only in the operator's sequencing, and a write made in the same turn as the join slipped past it twice.

## 7. The fix (on branch `inv-join-dirty-primary`)

- **`conductor-join.py` step 0b, `dirty_fence`:** before the merge, every modified, staged or untracked path outside
  `.agents/log/` refuses the join with `[COORD-JOIN-DIRTY-PRIMARY]`, the path list (each path's state and whether the
  branch also changes it; paths read with `-z`, so a space or a non-ASCII name is one entry), the remedy (commit under
  the session that wrote it, or stash), exit 12 (`EXIT_DIRTY`), and the change left in place. `--continue` skips it: a
  hand-resolved merge is already in progress.
- **Self-test cases (d), (f), (g) and (e):** a modified `README.md`, an untracked `docs/new note.md`, and a staged
  change to a path the branch also changes each refuse at step 0b with the merge not run and the change kept, named
  with their state; a modified session log alone still joins. Observed **red** with the fence call disabled (each
  "did not stop at the dirty-primary fence (exit 0)") and **green** with it restored. The red run also showed the
  sweep directly: the unfenced join committed case (d)'s modified file and case (f)'s untracked file under the join's
  name, and that swept content then made the next merge conflict, which is why the fixture settles the index after
  every case. The review's own probes also showed the exemption guarded (removing it turns (e) red) and the staged
  case covered.
- **The writers' note:** `coord-decide.py rule` and `prompt-log.py add` print one stderr line that the write is
  uncommitted and names the fence.
- **Blast radius:** a join now refuses in a state where it previously either refused with a misleading message or
  silently swept the change; no join that was clean before is affected. **Rollback:** revert the commit; the fence has
  no persistent state.

## 8. Generalisation

- **The class:** a tool that writes into a shared checkout without committing, followed by a tool that assumes the
  checkout is clean. Signature: a merge refused with "would be overwritten", or a commit that carries a file its branch
  never changed. Why it survives: the writer is told not to commit, the consumer never checks, and the protection that
  fires (git's) describes the symptom as a conflict.
- **Siblings swept:** the pack's other scripts that mention merging (`apply-learnings.py`, `coord-core.py`,
  `coord-runner.py`, `pack-apply.py`, `pack-doctor.py`, `session-profile.py`) return a classification string, read a
  merge attribute, or run `git merge-file` on text; none merges in a checkout. `conductor-join.py` is the only
  consumer, and its step 7 sweep is the only other face (Verified by grep).
- **The reusable rule:** a script that merges, rebases or commits in a checkout it did not create reads the tree first
  and refuses by name on what it would overwrite or sweep; a script that writes into a checkout without committing says
  so on stderr. The register entry is in `docs/lessons/defect-classes.md`.
- **Markers:** no `simplify:` or `assume:` marker in the four scripts (Verified by grep).

## 9. Phased repair plan

| Phase | Scope | Failure mode it removes | Validation | Status |
|---|---|---|---|---|
| R1 | The step-0b fence, its two self-test cases, the writers' notes, the register entry | a join refused with a conflict message, or a write swept into a join commit | the self-test red on the unfenced join and green with it; `check-docs` green; the join of this branch itself | this branch |
| R2 (follow-up) | `conductor-join` step 7 stages only the paths the join itself produced (the merge result, the derived views, the audit entry, the session logs) rather than `git add -A` | any future sweep the fence does not cover (a path dirtied between step 0b and step 7) | a self-test that dirties a path after the fence and asserts the join commit does not carry it | landed on `w3-jf-join-fence`, repair cycle 1: scoped staging; audit hash fenced before own append, checked again before staging, committed diff exactly one added line; vanished candidates excluded; refusal 13 distinct from conflict/incomplete merge 1; `--continue` fenced, post-commit porcelain checked; required cases red then green. Residual: a foreign write to an eligible derived path during a join command can still be staged; joins require exclusive execution |

## 10. Residual risk

- The fence reads the tree once, before the merge. A write landing between step 0b and step 7 (another session acting
  in the primary during the join) is still swept; R2 closes that. A `--continue` run skips the fence, so an unrelated
  change present at that point is swept too (Inferred, not run); R2 covers it the same way, and R2 should also check
  after the join commit that `git status --porcelain` is empty outside `.agents/log/`.
- Git's own refusal at step 1 is still reported by the join as "the merge has conflicts" with an empty list when it
  happens; the fence makes that path unreachable from a dirty tree, but a refusal for another reason would still read
  as a conflict. R2 tells the two apart at step 1.
- The fence exempts `.agents/log/` because the join's chore commit carries session logs by design; a register that
  moves under that folder would lose the fence.

## 11. Gate record

Diagnosis and fix by the main session; the Test Architect review is the Adversary pass on this branch before its join.
