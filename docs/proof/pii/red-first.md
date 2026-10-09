---
id: proof-pii-red-first
title: "PROOF-PII red-first receipt"
type: proof-pack
status: active
owner: "@track-pii"
phase: implementation
tags: [pii, proof, ruling-145]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Red-first, self-test, cost and hash-safety receipt for the PROOF-PII guard and the Windows user-name scrub (Ruling 145 (5)).
---

# trk-pii: red first

- Red: `python3 tools/check-proof-pii.py` on the pre-scrub tree exited 1 and named 29 files (`red-run.txt`; the name is redacted to `<name>` there).
- Green: after the scrub the same command exits 0 (`PROOF-PII ok: 0 Windows user paths or machine SIDs (1 allowlisted)`).
- Self-test: `python3 tools/check-proof-pii.py --self-test` catches 7 offenders (backslash, JSON-escaped, forward slash, WSL, lowercase, SID with and without RID) and passes 6 clean texts, including a macOS home path.
- Cost: about 1.0 s (3 runs, 1.03 / 1.02 / 1.05 s over 2807 tracked files); ring: fast, run by `tools/check-docs.py`.
- Registers (`docs/audit/*.jsonl`, `xmsg.jsonl`, `rulings.md`): 0 Windows-form hits, so no register allowlist entry. The macOS name appears there and is out of scope.
- Hash safety: `docs/proof/win-routes/manifest.json` recorded 15 of the changed files; those rows were refreshed in the same commit and `hash-manifest.py --check` passes (160 rows). `docs/docs-index.js` hashes are derived and regenerated. `fixture-observed-sha256.txt` records the sha256 of the fixture `.deb`, not of itself, so only its path column changed.
- Allowlisted, unchanged: `docs/ai-forward-pack/scripts/verify-no-machine-paths.py` (pack self-test fixture with the dummy name `x`; pack-owned).

# trk-phn: hostname rules (Ruling 181 (5))

- Rules added to `tools/check-proof-pii.py`: Windows default names (`DESKTOP-`/`LAPTOP-` plus 7, `WIN-` plus 11, upper case only); `COMPUTERNAME=`/`:` with a value; `"MachineName": "<name>"` (also JSON-escaped); a `systeminfo` `Host Name:` line; a UNC host prefix, raw and JSON-escaped; and (added after the first sweep showed the PC's real name in solver logs under no listed shape) the OpenFOAM log banner line `Host` plus two or more spaces, a colon and the name. Placeholders that never fire: `<...>`, `%...%`, `$...`, `{...}`, `localhost`, `.`, `?`, `wsl.localhost`. Hits print the name masked (two characters and the length); the guard never echoes a hostname.
- Optional run-time list: added. `CFD_PII_HOSTNAMES` (comma-separated, whole-token, case-insensitive) is read from the environment and never committed, so a PC can guard its real name locally. Unset, it does nothing.
- Red: a scratch script loads the pre-change guard (`git show HEAD:tools/check-proof-pii.py`) and runs the 11 new offender fixtures through it: 11 of 11 uncaught. The same fixtures through the new guard: 11 of 11 caught. The old guard flags 0 of the 13 new clean fixtures.
- Self-test: `python3 tools/check-proof-pii.py --self-test` prints `18 offenders caught, 19 clean passed` (7 old plus 11 host offenders; 6 old plus 13 host clean, which include a repo path, a VBCSCompiler pipe name, a hyphenated class name and a C# string escape). All fixture names are invented and assembled from parts.
- False positives found by the first sweep and removed by tightening the rule, not by allowlist: a hyphenated defect-class name that starts with the default-name shape (the pattern now refuses a following `-<letter>`); a UNC-looking C# or JSON string escape (names of one character or starting with a dot no longer count).
- Cost: before 1.46 s (3 runs on the same tree: 1.43 / 1.47 / 1.48), after 1.80 s (4 runs: 1.81 / 1.78 / 1.79 / 1.79); +0.35 s in the fast ring. A first version with a leading lookbehind took 6 to 10 s; each rule now starts with a literal and a cheap substring pre-filter. Ring: fast, run by `tools/check-docs.py`.
- Sweep of main (names masked), guard exit 1, no allowlist entry added, nothing scrubbed: 64 files fire. 7 files carry the PC's real hostname in the OpenFOAM banner (`docs/proof/win-naca/l6-results/log.{checkMesh,decomposePar,reconstructPar,simpleFoam}`, `docs/proof/win-routes/r133/cavity-{blockMesh-2,checkMesh,icoFoam}.txt`); 56 files under `docs/proof/spike-03` and `spike-04` carry the 3-character label `Mac` in the banner (an earlier scrub value, not a machine name); 1 file, `tools/spikes/WindowsRuntime/Program.cs:146`, uses a generic 6-character word as a UNC server name in a test string. The leader rules on adding `mac` and that word to the placeholder set, and on the 7 manifest-pinned files (PC scrub). The real name is also quoted in prose in `docs/notes/rulings.md` (Ruling 174), `docs/reviews/pr-18.md` and `docs/coordination/xmsg.jsonl`, and in `log.simpleFoam` line 20 (an MPI rank line); the shape rules cannot see those, so the PC or the leader sets `CFD_PII_HOSTNAMES` locally to guard them.
