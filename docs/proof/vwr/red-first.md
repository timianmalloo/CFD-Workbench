# Track VWR red-first record

Base: main bffe195f plus the PR #19 merge (8132e777). Verifier exit contract: 0 pass, 1 fail, 4 NOT ASSESSED off Windows.

## 1. Join gates (run-verify-gates)
- RED, before the change: `python3 docs/ai-forward-pack/scripts/run-verify-gates.py --skip verify-application-core.py verify-application-adapters.py`
  exited 1: `FAIL verify-windows-store.py ... TOTAL_WALL_SECONDS=0.036097`, "1 of 11 gate(s) FAILED". Capture: `red-gates.txt`.
- GREEN, after: the same command with `verify-windows-store.py` added to `--skip` exited 0, "10 gate(s) passed". Capture: `green-gates.txt`.
  The new join step `python3 tools/run-windows-store-gate.py` prints `TOTAL_WALL_SECONDS=...` and `NOT ASSESSED (verify-windows-store)` and exits 0.

## 2. Wrapper (tools/run-windows-store-gate.py)
- `--self-test` covers stub exits 0 (PASS), 4 (NOT ASSESSED, exit 0), 1, 2, 9 (FAIL, exit 1), the echoed TOTAL_WALL_SECONDS line, and a hung child
  killed at its limit (FAIL). Capture: `wrapper-selftest.txt` (`self-test OK`). The wrapper is new, so there is no earlier green to contrast.

## 3. Readiness (tools/run-readiness.py)
- RED, old code: a ring holding the verifier with exit 4, run by the pre-change `run_ring`, returned 1 (RED). Capture: `red-readiness.txt`
  (`OLD run_ring exit with verifier exit 4: 1`).
- GREEN, new code: `--self-test` now also proves exit 4 of the verifier is `not-assessed` and the ring stays green (and passes `--check`),
  exit 0 is recorded as `evidence`, exits 1 and 2 are red, exit 4 of an unruled script is red, and the entry's own timeout kills a hung
  verifier. Capture: `green-readiness-selftest.txt` (`self-test OK`).
