---
id: proof-win-r151-l3
title: "Ruling 151 C2 Windows L3 active progress readback"
type: proof-pack
status: in-progress
owner: "@win-w4-validation"
phase: implementation
tags: [proof, windows, naca0012, openfoam, progress]
links:
  - { to: proof-win-naca, rel: relates-to }
  - { to: review-pr-9, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-09"
summary: >-
  One partial, read-only Ruling 151 C2 snapshot observes the active Windows L3 unit
  and six ranks; A4 and final acceptance remain pending.
---

# Windows L3 progress readback — Ruling 151 C2

**Status: partial/incomplete active-progress readback.** The wrapper recorded 2026-10-09 22:28:22.1104918Z to 22:28:22.8467457Z, but these are wrapper-stage timestamps: the second keepalive check and metadata write followed the recorded end. The Linux output begins at 22:28:21Z, one second earlier than the Windows start; clock alignment was **Not measured**. The wrapper contains the command template `wsl.exe -d cfdw-openfoam2512 -u root --exec /bin/bash <script>`, where `<script>` was `readback.sh`; literal launched argv were **Not recorded** separately. The script was uncommitted at execution; its recorded execution SHA-256 was `77f7b2c911ffdf4994eb2f90a5aa4f735176b6a26fbd453cde3e19f0b1717694`. WSL exit was **0**, and the retained stderr text is empty. `capture.stdout.txt` and `capture.stderr.txt` are decoded process streams re-encoded as UTF-8, not raw byte streams. `capture.json` retains the original, unsupported `capture_state="complete readback; mutable sources non-atomic"`. This receipt supersedes that assessment: partial observation; query completeness not established; no enforced wall-time limit. The script reads systemd, `/proc`, and selected run files. No solver or monitor action was requested.

**Verified in the capture:** `cfdw-l3-20261008-r1.service` was `active/running`; `ActiveEnterTimestamp` was 2026-10-08 10:03:18 PDT; MainPID 483; cgroup `/system.slice/cfdw-l3-20261008-r1.service`; unit `Nice=10`. MainPID 483 was the Bash supervisor, with PPID 1, start tick 6694 and NI 10. PID 913 was the Python monitor, PPID 483, start tick 8220 and NI 10. Its process command contained `a4-monitor-linux.py`; the supervisor command contained `l3-supervisor.sh`. The matching source files in the earlier Windows validation checkout hashed `e65f58997fa16b1c83fa35a5bd5ad1ba3c843e7ea65abb0c65815d244b5c0f22` and `6718a96a18be0e8a473d4f794dbd345f1e11c805a1b5b110c1e697b38970777d`, respectively. The current source-file hashes are a readback, not original launch-time hashes. Full process commands and environment are deliberately absent from the public capture.

Six cgroup members were `simpleFoam` ranks: PIDs 1116–1121, each PPID 1113, start tick 8284 and effective NI **19**. The current output did not enumerate PID 1113's executable, so its live `mpirun` identity and NI are **Not recorded** by this snapshot. The prepared `of-command-l3.sh` source runs `mpirun -np 6` through `nice -n 10`, and the unit itself reports Nice 10. Nested nice is a plausible explanation for the rank NI 19, but binding that prepared adapter to the actual running command is **Not recorded**. Ruling 151's prior review also reports six ranks at nice 19. The historical YAML `cases/win-spike04r3-g2-l3.yaml` remains pinned at `decomposition.nice: 10` and is unchanged; it describes the prepared contract, while this receipt records the observed rank priority. The Mac must dispose of that discrepancy for final reconciliation.

Windows PID **19296** was observed as a live `wsl.exe` both before and after the WSL call with the same executable identity and creation time, 2026-10-08 17:02:23.1776050Z. That establishes only PID identity and liveness at the two checks. Its keepalive function and historical relationship to this distro or unit are **Not recorded** by the available process metadata. The systemd unit and six ranks were live during the Linux readback.

The selected run-directory observation found six `processor*` directories. The retained filtered tail of `run-ledger.txt` recorded checkMesh exit 0, decomposePar exit 0, and a `simpleFoam` start at 2026-10-08 17:03:35Z; it contained no solver exit line. `log.pipeline`, `log.simpleFoam`, `a4-monitor.log`, and `time.simpleFoam` existed; `convergence.txt` was absent. The selected file sizes and before/after modified times appear in `capture.stdout.txt`. In a bounded 262,144-byte tail of `log.simpleFoam`, the latest iteration with an `ExecutionTime` marker was **54,492**. This does not assert a final iteration or run completion. An earlier partial-run directory and its preservation inventory or timing are **Not recorded** by this capture.

The latest captured A4 monitor observation was `iterations=54003 A4=NOT MET`, at monitor text time 15:17:01 and source `a4-monitor.log` (file modified 2026-10-09 15:17:01 PDT). It reported residual-drop pass, stationarity fail, and clean-window pass. This is an **interim** A4 result, not a final failure. No Cl/Cd result is admitted, no GCI was run, and no L3 triplet enters `gci.py` on this evidence. The run files can change during a read; this is a non-atomic snapshot with per-file before/after size and modification time. The monitor log was captured only through a bounded, truncated allowlist and no final A4 report was executed.

Original launch-time hashes, preservation timing/binding, and final solver/monitor/reconstruction exits are **Not recorded**. No old `docs/proof/win-naca/` manifest or evidence bytes were rehashed or changed. Final L3 acceptance and case-priority reconciliation require a Mac disposition after a separate final readback. The executed scripts and capture outputs are historical artifacts and must not be rerun or replaced. Repair cycles used for this docs-only correction: **2 of 2**.
