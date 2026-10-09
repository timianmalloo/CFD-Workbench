---
id: proof-rlk-red-first
title: "RLK red-first receipt"
type: proof-pack
status: active
owner: "@trk-rlk"
phase: implementation
tags: [rlk, readiness, ring-lock, red-first]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Readiness takes every ring slot for its whole run: red run with a fake ring (timed step started at once), green run (waits, track ring waits, BLOCKED exit 4 past the bound).
---
# RLK red first: readiness takes the ring lock (READINESS-UNLOCKED)

Behaviour: a readiness run holds every `tools/ring-lock.sh` slot while it runs, waits (bounded) for track rings to end,
and track rings started meanwhile wait. Stub ring step (one `python3 -c` that stamps the time and sleeps 3 s), real
processes (a `sleep 60` fake ring holding `slot-1`, the real `ring-lock.sh`). Cost: the whole `--self-test` is about 25 s;
the new cases `ring_lock_cases` about 10 s of it. Ring: on demand (the self-test is not wired into `check-docs`, as with the CBS cases; wiring needs `tools/check-docs.py`, not owned by RLK).

## Red (old `run-readiness.py`, new self-test cases) — `red-run.txt`

    self-test FAIL: readiness started its timed step 2.0 s before the fake ring released its slot (exit 0)
    self-test FAIL: the receipt does not record the measured ring wait: None
    self-test FAIL: a track ring started while readiness ran was not made to wait: 'RING-LOCK waited 0 s\nslot=[.../slots/slot-1]\n'
    self-test FAIL: readiness left the ring lock held after it finished
    self-test FAIL: a ring held past the bound gave exit 0 (want 4), step ran True, receipt True
    self-test FAILED                                   (exit 1)

The old code starts its timed step at once while a ring holds a slot, and a track ring takes a slot during readiness.

## Green (new code) — `green-run.txt`

    RING-LOCK-EXCLUSIVE waiting 0 s for these holders:
      slot-1 pid=47551 sleep 60
    RING-LOCK-EXCLUSIVE waited 2 s
    run-readiness: total 3.3 s ... waited 2.1 s for the ring lock
    ring lock: readiness waited 2.1 s for a fake ring held 2.0 s
    ...
    RING-LOCK-EXCLUSIVE-TIMEOUT waited 2 s (holders:)
      slot-1 pid=47697 sleep 60
    run-readiness: BLOCKED (ring busy): no timed step was run and no receipt was written ...
    self-test OK                                       (exit 0)

`tools/ring-lock.sh --self-test`: 15/15 (cases 6-8 are new: exclusive acquire waits and names the holder; a track ring
waits while readiness holds the lock; the pending marker stops a freed slot being retaken; a dead owner's marker is
ignored; timeout exits 5, keeps nothing). `tools/run-suite.sh --self-test`: 6/6. The CBS `finish` cases in the same
self-test (cleanup after expired deadline / timeout / failed kill, 0.2-0.3 s against a 2 s bound) are still green.

## Design

- Mechanism: exclusive mode in `ring-lock.sh` (`--acquire-all <pid>`, `--release-all <pid>`), owner = the readiness PID, so
  a dead readiness is reclaimed like any dead ring. `exclusive-wanted` (live PID) keeps new rings from taking a slot a
  holder just freed (starvation).
- Wait bound: 600 s (`CFD_READINESS_LOCK_WAIT_SECONDS`). The holders are at most `CFD_RING_MAX` (2) rings or suite runs, each
  within its 60 s ring budget or 120 s suite wait; 600 s covers several of those, and a longer wait means a hung holder
  that someone should look at.
- Blocked: exit 4, `run-readiness: BLOCKED (ring busy)`, no step run, no receipt (an older green receipt is not touched).
- The wait is outside the 240 s ring budget; the receipt records `ringWaitSeconds`.
- Held for the whole ring (build, gates, recounts, `--readiness` steps), not only the three timed steps: the build and the
  gates are heavy and move the load the frame gate reads.
- Residual: Windows (`os.name == "nt"`) runs unlocked, as the lock is a bash script. Track rings that already timed out
  and proceeded without a slot (`RING-LOCK-TIMEOUT`) are not counted; the printed holders do not list them.
