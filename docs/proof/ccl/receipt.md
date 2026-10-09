---
id: proof-ccl-receipt
title: "CCL receipt: cost caps under a concurrent ring"
type: proof-pack
status: complete
owner: "@trk-ccl"
phase: implementation
summary: "End load lags; a ring that overlapped another reports C-2..C-5 as COST-ADVISORY naming the holder, a quiet ring still fails."
tags: [test-ring, cost-caps, ring-lock, proof]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
---

# CCL receipt: cost caps under a concurrent ring (trk-ccl)

Base main 9e969c5f, branch `fix/ccl-cost-concurrency`. Host: Mac, background load 13-17 from other tracks during every run below.

## Measurements (Verified: observed, 2026-10-09)

| Run | Core parts (ms) | Desktop (ms) | Analysis parts (ms) | net wall (ms) | load start -> end | Verdict |
|---|---|---|---|---|---|---|
| A. one ring alone | 30.0k / 30.5k / 30.6k | 40,427 | 4,845 / 4,845 | 43,876 | 16.48 -> 18.42 | green |
| B. one ring + synthetic peer from second 20 (8 busy loops, 25 s) | 40.9k / 40.9k / 41.3k | 46,193 | 4,814 / 4,912 | 49,641 | 17.02 -> 23.66 | green by 359 ms; end load under the gate of 24 |
| C. ring + peer holding a slot (after the fix) | 48.7k / 48.8k / 48.0k | 54,436 | 16,421 / 17,798 | 60,177 | 13.02 -> 25.86 | `COST-ADVISORY (concurrent ring 40101)` x 17, exit 0 |
| D. final ring alone (after the fix) | - | - | - | 43,082 | 17.66 -> 19.99 | green, no peers.txt |

Raw: `scratch/ccl/{solo,peer,fixed-peer,final}.log` and `.samples` (kept outside the repo); key lines in this receipt.

Findings:
1. (Verified) The end load is a lagging 1-minute average. In run B the peer ran only 25 s of a 49 s ring; the end load was 23.66, under the 24 gate, while net wall rose 5.8 s (43.9 -> 49.6 s, 359 ms from the 50 s cap) and Desktop rose 5.8 s (to 46.2 s, 3.2 s from its cap). A slightly heavier or earlier overlap fails C-3/C-4 while the load still reads <= 24. This is the failure the five tracks hit (C-2 at 5032/5083 ms at load 20.8).
2. (Verified) A ring alone has little margin even when quiet: Analysis parts 4,845 ms of 5,000.
3. (Inferred) A peer that ended before the final reading leaves the end load lower still. The sampler records any overlap during the ring, so it does not depend on this.

## Options

- (a) read the ring lock, advisory when another ring held a slot: **chosen**. The lock already knows who ran; no new threshold; the quiet-host cap is untouched; the exclusion is printed with the holder's PID.
- (b) `CFD_RING_MAX=1`: rejected. It would double wait time for every track (rings run ~50 s each, five-plus tracks queue), and it hides the very contention the cap is meant to separate from regressions.
- (c) sample load over the ring: rejected. Load remains a proxy; it cannot name the cause, and a threshold on the mean needs a new calibration.
- (d) none better found. Residual: load from outside the ring lock (a build, an editor) is still judged by the end load only.

## Change

- `tools/ring-lock.sh`: `ring_lock_peers <own pid>` lists live slot holders other than the caller. Self-test cases 9 (2 cases).
- `tools/run-tests.sh`: after taking its slot, a once-a-second sampler appends each peer PID to `.tmp-tests/peers.txt` (cleared at ring start); stopped before the cost check; prints `RING-CONCURRENT with ring PID(s) ...`.
- `tools/check-test-costs.py`: `read_peers`; with peers, an over-cap C-2..C-5 prints `COST-ADVISORY (concurrent ring <pids>) <rule> <value> load <load>` instead of failing. No file or an empty file keeps every cap a failure on a quiet host. C-6 (missing reading) and the functional checks are untouched. TEST-BUDGET is not changed.
- `docs/lessons/defect-classes.md`: class `COST-CAP-LAGGING-LOAD`.

## Red and green

- Red (`red-selftest.txt`): `check-test-costs.py --self-test` with the 6 new peers cases against the old checker: 81/83, exit 1; the two cases "a concurrent ring turns C-2..C-5 into COST-ADVISORY" and "two holders are both named" failed (the run failed on the over-cap readings).
- Green (`green-selftest.txt`): 83/83, exit 0. The quiet-host cases stay green and red: with no peers file, or an empty one, the same over-cap readings still fail C-2, C-3, C-4 and C-5; C-6 still fails with a peer present.
- `ring-lock.sh --self-test` (`ring-lock-selftest.txt`): 17/17, exit 0.
- End to end: run C above (real slot holder, real sampler, real checker).
