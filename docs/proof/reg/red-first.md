---
id: proof-reg-red-first
title: "REG red-first receipt"
type: proof-pack
status: active
owner: "@trk-reg"
phase: implementation
tags: [merge-driver, proof, join-log-conflict]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Receipt for the register-aware merge driver: red against an always-conflict stub, green self-test, WTH join replay byte-identical, scratch-clone fallback.
---
# REG red-first receipt (merge driver for the defect-classes register)

Red: the same file with `merge()` replaced by `return None` (a stub that always conflicts, `stub-red.txt`), exit 1:

    FAIL: both append different entries
    FAIL: one extends X, other appends Y
    FAIL: identical new entry on both
    ok  : both edit X differently / one deletes X / preamble edited on both / reordered entries   (conflict is the expected answer)

Green: `python3 tools/merge-defect-register.py --self-test`, exit 0, 7 of 7 ok (`self-test.txt`).

Replay of the WTH join (`replay-wth.sh`, `replay-wth.txt`): the first driver version exited 1 (the conservation check rejected
a mid-line extension of an entry's last line) and then differed by blank lines (entry gaps, a doubled blank in theirs). After
both fixes the driver output is byte-identical to the leader's committed file (`IDENTICAL`). Two repair cycles; none left.

Fallback (`scratch-clone-check.sh`, `scratch-clone-check.txt`): in a scratch repo without the registration, two branches that
each append an entry conflict (exit 1, 1 marker); after `tools/install-merge-drivers.sh` the same merge is clean and keeps both.

## RG2: the conflict path writes markers (track RG2)

Defect: on every unresolved case the driver exited 1 and left `%A` untouched. Git keeps `%A` (ours) after a non-zero driver exit
and marks the path conflicted, so `docs/lessons/defect-classes.md` was UU with no markers at the PR #17 join.

Red (`rg2-red.txt`, self-test with the new content assertions against the old driver), exit 1: all five conflict fixtures FAIL
(both edit X, one deletes X, preamble on both, reorder, and the new PR #17 frontmatter-links fixture); the three resolving fixtures stay ok.

Green (`rg2-green.txt`), exit 0, 8 of 8 ok. Each conflict fixture runs `run_driver` on files and asserts exit 1, `<<<<<<<` and
`>>>>>>>` in `%A`, and every line theirs changed or added present in `%A`.

Replay of the PR #17 join (`replay-pr17.sh`, `replay-pr17.txt`): driver exit 1, 3 marker lines, no line theirs added is missing
from `%A`, and theirs' links (`review-pr-9`, `proof-win-naca`) sit beside ours (`review-pr-13`, `proof-r163-windows-ring`)
between the markers.

Sweep: `coord-core.py` `merge-derived` and `merge-register` always exit 0 and write markers through `_write_conflict` (its docstring
names this hazard, S12b). No other driver exits non-zero without writing `%A`. The pack file was not edited.

## RG3: the driver defers to a clean three-way merge (track RG3)

Defect: main and DPR each added a different dated line inside the DPI-A entry. The entry rules gave up; `git merge-file -p` merged cleanly
(exit 0); the driver only used git output when it had conflict hunks, so it wrote a whole-file ours/theirs conflict (exit 1, two
2,260-line copies). The leader resolved it by hand.

Red (`replay-dpr-old.txt`, the pre-RG3 driver on the real DPR join, replay `replay-dpr.sh`): driver exit 1, "DIFFERS from committed", 1 marker
line, 4,522-line output. The same shape as a self-test fixture fails against the old driver for the same reason (the old driver has no
git-clean path); the first red run of that fixture used a same-position insert, which git itself conflicts on, so the fixture was rewritten
to insert at two different places.

Green (`rg3-green.txt`), exit 0, 9 of 9 ok: new fixture "both add a different dated line inside X (DPR shape)" asserts exit 0, no markers,
and every line either side added is present. "both edit X differently" still asserts exit 1 with hunks.

Replay (`replay-dpr.txt`): base 034f37fd, ours 9a1b3181, theirs bcd382bb. New driver exit 0, 0 markers, 2,261 lines, IDENTICAL to
`6d2cff44:docs/lessons/defect-classes.md` (the leader hand-resolution).

Brief deviation: the brief says every non-blank line of ours and theirs must be in the output. On the real join that is false and must be:
theirs still held two base lines (`rule). Status: pending a mechanical control...`) that main had rewritten, and git rightly dropped them (the
leader file lacks them too). Conservation is therefore: every line ours or theirs added or changed relative to base is in the output.
If one is missing, the driver falls back to the whole-file conflict and exits 1.
