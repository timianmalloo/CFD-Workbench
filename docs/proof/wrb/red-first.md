# Track WRB red-first record (Rulings 167, 168)

## Item 1 - CFD_RING_HOST override (Ruling 168 (3))

Self-test: `tools/check-test-costs.py --self-test` (ring: every join; cost: under 0.1 s), new group `self_test_ring_host` (9 cases).

- Red (cases added, `resolve_host` not yet written): `NameError: name 'resolve_host' is not defined`, exit 1.
- Green (after): `SELFTEST 50/50 cases`, exit 0. `CFD_RING_HOST=pc-win` resolves to `pc-win` (baseline `docs/proof/ring-pc-win/`); `Tim.PC`, `PC-Win`, `../x`, empty and 33 characters are refused naming CFD_RING_HOST.
- CLI: `CFD_RING_HOST=pc-win check-test-costs.py --resolve-host Tims-PC` prints `pc-win`, exit 0; `CFD_RING_HOST=Tim.PC` exits 2 with the refusal; unset prints the hostname passed in (behaviour unchanged).
- Sweep: `git grep -in "hostname|gethostname|platform.node|COMPUTERNAME" -- tools` finds one reader, `tools/run-tests.sh:73`. It now calls the one helper (`--resolve-host`); `check-test-costs.py` receives the key through `--host`.
- Report only (not built): PROOF-PII (`tools/check-proof-pii.py`) matches Windows home paths and SIDs only. It does not flag a `docs/proof/ring-*/` folder named after a raw hostname. A guard would need an allow-list of known keys (ring-b1, ring-b2, ring-b4, ring-oct05, ring-oct08, ring-split, plus the PC's chosen key), since a hostname is not pattern-detectable. Not a Mac-only fix: the Mac hostname already leaks the same way if a Mac baseline folder is named after it.
