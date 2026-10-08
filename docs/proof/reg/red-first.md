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
