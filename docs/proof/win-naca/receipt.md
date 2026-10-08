---
id: proof-win-naca
title: "Windows W-4a/b NACA 0012 evidence and unlaunched L3 preparation"
type: proof-pack
status: in-progress
owner: "@win-w4-validation"
tags: [windows, wsl, openfoam, su2, tmr, naca0012, ruling-79]
links:
  - { to: coordination-pc-kickoff, rel: implements }
  - { to: coordination-windows-w0-w5-execution, rel: implements }
  - { to: proof-spike-04-round3, rel: relates-to }
  - { to: review-pr-7, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-08"
summary: >-
  Exact G0 L6 CPU comparison from Mac-equal LF dictionaries meets A4 on Windows;
  native SU2 completed but its declared iterative oracle failed; W-4b metadata and byte-preservation defects
  were corrected in a separately authorized evidence-only track. L3 remains prepared, not launched.
---

# W-4a/b Windows NACA 0012

**Goal:** execute W-4a exact round-3 L6 and W-4b native SU2 TMR, then prepare L3 for the coordinator start gate.
**Done when:** raw evidence, source/blob hashes, measured cross-OS and CFL3D comparisons, costs, exits and unlaunched
L3 commands are committed, or a bounded blocker is reported. **Not in scope:** L3/W-5 launches, application ring,
installs, product/shared-case changes, audit/index/xmsg authoring, publishing. **Tier:** T1; author/worker; fan-out 1.
Two repair cycles per track; W-4a 60 min, W-4b 120 min. The coordinator partition caps this worker at six cores while
B2 is active. Actual heavy work is serial: G0 two ranks, then SU2 one thread. OS/coordinator retain two physical cores.

One tested source SHA: `fd96651e6dd3ad9c01e4fbaa07c62a11fa553c09`. Worktree
`C:\Projects\CFD-Workbench-win-w4-validation`, branch `win/w4-validation`. Fetch/merge at grounding: already up to date.
`xmsg unread --mark` read the binding Mac review and LF-generator handoff before fixture generation.
Surface list: new Windows case YAMLs → frozen source inputs/configs → fresh solver directories → raw logs/history →
comparison/receipt → coordinator review/start note. Existing shared cases remain read-only.
The optimize-graph workflow is the serial DAG above; costs come from `commands.jsonl`, no modelled duration claims.

## Host and pinned solvers

Verified `host.txt`: Windows 11 Pro `10.0.26300`, i9-12900H **14 physical/20 logical cores**, RAM **34,009,374,720 bytes**,
Python 3.13.14. Installed Windows NumPy 2.4.6/PyYAML 6.0.3; WSL Python PyYAML 6.0.1, no NumPy. Generation uses the
existing Windows dependencies; Linux runtime/monitor use existing Python only. No dependency installed.

`build-inspection.txt`: private WSL2 `cfdw-openfoam2512`, Ubuntu 24.04.5 retained W-3 pin, runtime/common
`2512.0-2`, OpenFOAM v2512 `_bd2b6720-20260127`, label32/scalar64. simpleFoam SHA-256
`b7bb6321cac3586878a6c51c07f3139dba651ff1f3dbdb0dd406536b27846ba0`; other executable hashes recorded there.
Package pin `c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed` retained from W-3.
Native SU2 publisher zip pin `4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11`; exe hash
`3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248` checked directly before launch.
Versions/routes retain [W-3 receipt](../win-routes/receipt.md); numerical launches verify the resolved binaries anew.

## W-4a: exact G0 CPU comparison

Owner selected `cases/spike04r3-g0-l6.yaml` because it met A4; G1b's clipped limit cycle is inadmissible.
There is **no shared `cases/spike04r3-g2-l6.yaml`**. The Windows file is honestly named `win-spike04r3-g0-l6.yaml`.
The original Mac case/generator/manifest/report blob hashes and every frozen input hash are in `l6-frozen.json`,
written before launch. **All 11 dictionary hashes equal the exact Mac G0 `cfdw-manifest.json`**, not just numerics labels.
LF disk bytes equal staged Git bytes before launch. Source grid: NASA Family II L6 `225×65`, **14,336 cells**,
192,979 bytes, SHA-256 `5664336523cb02c8410c4f9dc67eccdfa79b34ea9549b712c0d9fd988aff3244`.
Acquired using the shared range-fetcher from the authoritative NASA archive, URL/member/exit in `tmr-grid-fetch.txt`.

Fresh run `/root/CFDWorkbench/runs/20261008T144237Z-win-spike04r3-g0-l6`. Source-bound `of-command.sh` is a manual Linux
adapter, not a product launcher. Clean environment, publisher activation, isolated HOME, identical M1 bundle
`f3debe8b5541fb400b0719976f591781a2faa21f96ea7ae0dccca97e4a6ef854`, one OMP/OpenBLAS thread, nice10, two MPI ranks,
tree verification, dictionary lint, checkMesh preflight, Disallowing banner and fail-closed Allowing stop remain active.
Every command has a 3,000 s external ceiling inside W-4a's total 60-minute box; no solver dependency/build change.

**Platform signal adaptation, declared before launch:** unchanged M1 configures signal **30**. Linux SIGUSR1 is **10**;
the upstream monitor uses the macOS SIGUSR1 alias (30). Evidence-local `a4-monitor-linux.py` changes only the effective
send to configured integer30 (Linux SIGPWR), retaining all A4 calculations. Historical monitor text still calls it
SIGUSR1; this receipt corrects the label. Shared monitor and M1 bytes are unchanged. This prevents a platform signal
number from silently replacing a graceful A4 stop with process termination; coordinator owns the defect-register seam.

Verified exits: checkMesh, decomposePar, simpleFoam, reconstructPar **0**; all Disallowing. **Exit0 is not a mesh pass**:
checkMesh reports `Failed 1 mesh checks`, exactly as the Mac G0 log, due to1,526 high-aspect-ratio cells (maximum
31,734,384.0499). Maximum non-orthogonality79.747411915°,206 faces above70°, also the same Mac diagnostic condition.
This is the prescribed G0 comparison, not a wing mesh admission. `l6-convergence.txt`: **A4 MET**, 7,577 iterations,
window 5,578…7,577, three residual orders passed, zero bounding nuTilda iterations in that window. Different monitor
poll timing produces a different final window; these are window comparisons with iterative half-bands retained.

| Quantity | Mac G0 window mean | Windows G0 window mean | Windows − Mac | Relative difference |
|---|---:|---:|---:|---:|
| Cl | 1.051600898 | 1.051599294 | −1.604e−6 | −1.525293486e−6 |
| Cd | 0.014812212 | 0.014812115 | −9.700e−8 | −6.548650532e−6 |
| Courant | Not recorded | Not recorded | Not recorded | Not recorded |

`l6-comparison.json` computes these directly from the raw reports. The observed differences replace an assumed 1e−3
for this measured comparison; they do not calibrate a universal future-build tolerance. Cl half-bands: Mac7.925e−6,
Windows1.942e−6; Cd: Mac3.314e−7, Windows8.962e−7. Differences lie below the respective sums. The exact steady
simpleFoam case and Mac reference do not emit Courant; no substitute is invented. This remains a G0 scheme diagnostic,
code-to-code, incompressible, fully turbulent, Re6e6, alpha10°, grid uncertainty unquantified, **no physical validation**.

Raw logs, coefficient/residual/y+ tables and GNU costs are retained in `l6-results/`. simpleFoam wrapper wall60.448s;
GNU wall60.00s, user115.01s/system5.30s, CPU200%, maximum RSS101,412KiB (GNU process-tree measure, not summed MPI RAM).
Pipeline wall66.726s. Preparation/mesh/decomposition/reconstruction costs and loads are in command ledger/raw time files.

## GPU inspection: no supported route qualified

NVIDIA visible in WSL: RTX3080Ti Laptop, driver596.47, 16,384MiB. Installed `libfusedFiniteVolume.so` exists, but
simpleFoam's loaded dependencies do not include it or CUDA/HIP/Umpire. Runtime has no source tree; supported GPU
dispatch/configuration contract **Not recorded**. Unactivated fused ldd shows libfiniteVolume unresolved because
publisher environment is absent; that is inspection context, not a broken installed solver. Presence/visibility cannot
establish acceleration. **Zero GPU trials**; no supported pinned path established, no install or build change. GPU feeds
neither this CPU equivalence comparison, L3 nor W-5. Any later route requires a separately authorised Mac decision.

## W-4b: native SU2 NASA TMR

The assumption is checked: [SU2 v8.5.0's own config](https://github.com/su2code/SU2/blob/v8.5.0/TestCases/rans/naca0012/turb_NACA0012_sa.cfg) **does contain a NASA TMR
NACA0012 SA config**, citing the historical NASA TMR URL. Actual source bytes/hashes and pinned v8.5.0 template are in
`sources/` and `publisher-config-acquisition.txt`. Separate TestCases tree SHA
`790c80ec5b543487b5f8ecf8bb0f0e4d2cc67f3f` is untruncated; its mesh was acquired at that immutable SHA too.

Publisher config is a restart regression setup with NEGATIVE/EXPERIMENTAL SA and MUSCL turbulence. Frozen adaptations
in `su2-frozen.json`: cold start, SA_OPTIONS NONE (WITHFT2 is optional in pinned template), MUSCL_TURB NO, freestream
nuTilda/nu3, CFL50 fixed, 20,000 iterations, reference area/length1, Mach0.15, Re6e6, alpha10°, temperature300K (540R).
No adaptation after observing a result. SU2's [SA initialization](https://github.com/su2code/SU2/blob/v8.5.0/SU2_CFD/src/solvers/CTurbSASolver.cpp)
and [configuration](https://github.com/su2code/SU2/blob/v8.5.0/Common/src/CConfig.cpp) establish the
freestream nu-factor contract; links and acquired bytes are source evidence, not measurements of runtime behavior.

The run uses **the exact authoritative NASA Family II L6** PLOT3D converted to SU2, preserving all coordinates/wake
merges, 14,336 positive-orientation quads, 14,576 unique points, 128 wall/352 farfield edges. Publisher mesh is retained
as qualification evidence, not silently equated to Family II. Mesh/config are LF and match staged blobs before launch.
[the cited TMR page](https://tmbwg.github.io/turbmodels/naca0012numerics_val_sa_withoutpv.html) documents standard farfield/no point vortex, CFL3D SA and first-order turbulence;
the checked-in reference is `docs/proof/spike-04/reference/cfl3d_results_sa_nopv_withN.dat`.
SU2's node-centred spatial implementation and SA S-hat handling differ from CFL3D's; matching inputs does not remove
that code-to-code scope. Unknown numerical/grid/experimental uncertainties remain Not recorded.

Fresh Windows run `runs/20261008T144728Z-win-su2-tmr-naca0012`; native `SU2_CFD.exe -t 1 tmr-sa.cfg`, one thread,
BELOW_NORMAL_PRIORITY_CLASS. Predeclared admission: at least three RMS residual orders, final 2,000-row Cl/Cd half-bands
≤1e−5/1e−6; no physical-validation claim or retuning on a failed result.

**Observed single-run result:** exit0, 20,000 history rows, final inner iteration19,999, no timeout. Native wrapper
wall1,767.372040s; CPU1,692.125s; sampled peak working set82,292,736bytes,17,596 samples; priority readback16384
(BELOW_NORMAL_PRIORITY_CLASS). Exact argv, binary hash, start UTC and fresh run path are in `su2-results/resources.json`.
The raw `log.SU2_CFD` and `history.csv` were copied immediately on exit. History SHA-256
`9d585b39fca3ce3be1f5f50a8d5f586f83cd8cd54b388fb55c8dd3bf82c71f97`; reference SHA-256
`9cef249a0365454d7292cbed4c909d13242c5dc094582727db23acdd2e6f03e7`.

`su2-comparison.json` evaluates the predeclared final2,000-row window18,000…19,999 directly:

| Quantity | Diagnostic window mean | Half-band | Last row | Declared cap |
|---|---:|---:|---:|---:|
| Cl | 1.0671148292090002 | 4.9999993e−10 | 1.06711483 | 1e−5 |
| Cd | 0.019056381390059995 | 5.0000004e−12 | 0.01905638139 | 1e−6 |

**Declared iterative oracle NOT MET.** First-row to conservative last-two-row log10 residual drops:
rho10.192652928, rhoU−0.38271234, rhoV−0.34902652, rhoE10.175428914, nu7.2915418 orders. Momentum residuals were
already around1e−14 at initialization and do not meet the predeclared three-order relative-drop rule. Stable Cl/Cd
does not override that rule; no retrospective convergence criterion, retuning or rerun is applied. The native log
also reports `Maximum number of iterations reached (ITER = 20000) before convergence`, while its density-only
table prints criterion<−12 as Yes; neither exit0 nor that one-field table establishes our broader frozen oracle.
The exact CFL3D Family II L6 reference is Cl1.0829061464/Cd0.014992422606, but the accepted comparison/differences
are **Not admitted**. Diagnostic means above are preserved without an invented equivalence tolerance. This is a
blocked iterative qualification, not physical validation or GCI. SU2 working-variable positivity/clipping history,
spatial/reference uncertainties and aggregate host utilization remain **Not recorded**.

**Repair 1, metadata only:** first full case gate rejected a missing `numerics.residual_criterion` and a string-valued
`decomposition.nice`. Preserved prelaunch YAML bytes/hash `31041c99f2f9645c459bcaa891f2df2107b8f6b388b2bf0f5b949dbec305c24d`
in `su2-prelaunch-case.yaml`; final schema-valid YAML hash
`e2feb2d7aaabcf0fb020ba0d89c3e5e68848de9a1b5ef35d8840f8bd4e04e179` (intermediate version). `su2-schema-repair.json` binds both and the exact
metadata fields. Residual criterion copies the already stated convergence text; nice10 is the schema's courtesy
setting, with a note that Windows uses below-normal priority instead. Neither YAML was read by SU2; its actual mesh,
config and run remain unchanged. No solver rerun. Class → sweep → control: schema validation after launch can leave
a frozen metadata artifact inadmissible; the full 43-case sweep is now green, and `freeze-su2.py` reuses the real
validator **before** writing a future launch freeze. That check catches these metadata shapes before launch;
the coordinator owns register/audit capture.

**Repair 2, metadata only, cap reached:** a semantic sweep found copied OpenFOAM purpose, incompressible flow,
unit-speed fluid, span/gate and boundary labels in the SU2 YAML. Owner authorized replacing only this metadata with
the unchanged publisher-config/runtime identity: compressible RANS, Mach0.15, Re6e6, SA-noft2, 300K, Family II L6,
SU2 farfield/adiabatic-wall semantics and the CFL3D comparison question. The intermediate bytes above are preserved
in `su2-after-schema-case.yaml`; final attempted YAML hash is
`fc2b2ca0b9c25933cbeddeeb14e04556a360d318a40e80aa919ad34800c0a036`.
Exact command `py -3 docs/proof/win-naca/repair-su2-semantics.py` exited1 in0.246875s, captured by
`su2-yaml-semantic-repair.txt` and `commands.jsonl`. Schema assertion reports two errors:
`physics.flow='compressible-steady-RANS'` is outside its enum (which includes `compressible-steady`), and
`physics.fluid.nu_m2_s` is required. No third correction is made. The two archived earlier YAML versions, original
`su2-frozen.json` and all actual mesh/config bytes remain unchanged. The repair's final JSON write and later
config-token assertions were not reached; this receipt and the closing hash manifest bind the observed attempt.

Additional uncorrected metadata residual: the attempted boundary annotation includes `MARKER_DESIGNING` although
the frozen config explicitly lists only `MARKER_MONITORING` and `MARKER_PLOTTING`. The repair helper's unreached
literal check spells Reynolds `6E6`, while the unchanged config spells `6.0E6`. Neither affects the already running
solver; both are reported at the cap. `freeze-su2.py` remains a historical launch authoring helper and must not be
reused until its copied semantic projection is reconciled. Class → sweep → control: copying a case across solver
families can pass schema while retaining false physics; compare metadata against both actual config and runtime
banner before launch, then apply the repository schema. The present schema gate fails this attempted metadata;
it does not validate the scientific contract. Register/control changes remain coordinator-owned.
W-4a repairs0; W-4b repairs2, solver reruns0. W-4b delivery is blocked by metadata at the cap; evidence collection
of the one unchanged run is permitted. Coordinator disposition is required before further correction or L3 start.

## L3: prepared, not launched

`cases/win-spike04r3-g2-l3.yaml` copies G2 D4 without the 36,000 s wall cap; original numerical settings,
120,000 iteration limit and clause-5 extension retained. Six ranks/nice10; no wall timeout. `l3-prepared.json`
holds YAML, authoritative 13,026,423-byte L3 grid, generator, M1 and monitor/adapter hashes. Grid hash
`23db6ddbf6de258a0921f5870482d56e70e604ed2dc795902c038b7b3fd606e1`. No L3 mesh generated and no L3 solver launched.
Exact generation/stage/launch/readback commands are in `l3-commands.txt`; detached supervisor writes a durable ledger,
raw solver logs and A4 log. Missing coordinator start-note reference refuses before any directory or solver mutation
(`l3-missing-note-refusal.txt`, expected exit1). No W-5 action. Coordinator posts the start note and releases capacity
before launch; posts done/blocked after final readback, then only an admitted L5/L4/L3 triplet enters `gci.py`.

## Validation and delivery

Case validation after repair1: **43 cases, 0 errors**, exit0, 0.435s. Initial docs check exit1 in28.884s: zero metadata
problems/orphans, only coordinator-owned index drift `file not in index: proof-win-naca`; existing131 freshness
suggestions are advisory. Index/audit/register handoffs are coordinator-owned.

At-cap validation of the final attempted YAML: **43 cases, 2 errors**, exit1,0.534553s; both Windows OpenFOAM YAMLs
pass. `docs-validation-at-cap.txt` exits1 in15.573890s at the same case gate, before the index sweep. The known
new-document index drift is therefore unresolved and was not re-evaluated by that final early-exit docs run.

**Correction:** the author initially mistook `SPIRAL: 12 commits…` for the branch verdict. The full raw gate context
shows that line is an intentional negative fixture followed by `self-test OK`, then the real branch's
`spiral check: not checked (HEAD == base)`. **Coordinator-reported:** an independent real gate returned exit0 and
`origin/main..HEAD` count0. No product change was introduced to defeat a gate, and no solver was rerun. Control:
read the complete emitting test context and its final result before treating a planted negative as a runtime finding.

**Completed:** exact G0 CPU comparison, read-only GPU capability inspection, one native SU2 run/raw archive and
declared-oracle evaluation, six-rank L3 command/source preparation and missing-note refusal. **Remaining:** W-4b
metadata disposition at repair cap and failed iterative-oracle disposition, coordinator index/audit/register work
and review. **Next:** coordinator records blocked/done disposition; only an explicitly reopened track may correct
metadata or change an oracle. No solver rerun is authorized by this receipt. L3 requires completed W-4b disposition
and the coordinator's durable start note before the prepared commands in `l3-commands.txt` can be used.

**Additional closing blocker, no further correction:** the first staged manifest matched111 files before SU2
completion. Its closing refresh failed at `docs/proof/win-naca/su2-results/log.SU2_CFD`: disk bytes differ from
the staged blob. The evidence attributes cover `*.log`, but the native filename is `log.*`; Git normalized CRLF
to LF while staging. No attribute or blob correction is made, per coordinator instruction after the repair cap.
The original raw disk file remains in both the fresh run and proof archive. Direct read-only measurement:

| Representation | Bytes | CRLF pairs | SHA-256 |
|---|---:|---:|---|
| Disk raw log | 3,045,192 | 27,120 | `aab8d165671b30d0ed1224fbd11bff4e6d4bb9a151a221dce52d74549c823543` |
| Staged log blob | 3,018,072 | 0 | `0a364550d294a8a9461ac5b343812a5cbb497d596915d8270c599d8c6cdcc6e2` |

`hash-manifest.json` is therefore a historical precompletion snapshot, **not** a closing manifest or claim of
all-file blob equality. `hash-manifest.py --head` has not been run and no successful closing binding is claimed.
The read-only measurement used Python hashlib over `Path.read_bytes()` and `git show :<path>`; both hash values
above are directly observed. Git staging does not change the measured disk file, but committed log bytes are
normalized, so this handback fails the raw-byte preservation requirement.

Whitespace check `git -c core.whitespace=-blank-at-eol,cr-at-eol diff --cached --check` also exited1 with
`new blank line at EOF` in `l6-results/log.checkMesh`, `log.decomposePar`, `log.reconstructPar`, `su2-pipeline.txt`
and `su2-results/log.SU2_CFD`. No raw-log whitespace exception or repair is applied. These are additional blockers,
not passed gates. Coordinator explicitly ordered the blocked handback committed as observed, no third correction,
no further gate and no L3 launch. The next permitted action is coordinator disposition; the original disk evidence
must be retained until a separately authorized serialization correction has bound it without normalization.
No application ring, publishing or source change. Total agent time, tokens, aggregate host utilisation **Not recorded**.
Command UTC/argv/exit/wall and solver resource emitting sources are recorded; missing measures stay Not recorded.

## W-4b evidence-only correction (2026-10-08)

The coordinator authorized a new evidence-only track after the prior two-cycle semantic-repair track stopped.
The earlier failed YAML versions remain byte-preserved as `su2-prelaunch-case.yaml`,
`su2-after-schema-case.yaml`, and `su2-after-semantic-failure-case.yaml`; the last is the exact failed
`fc2b2ca0...` version. The corrected `cases/win-su2-tmr-naca0012.yaml` uses the schema enum
`compressible-steady`, with RANS/Spalart-Allmaras details in their separate fields. It records
`nu_m2_s: 8.6806e-6`, derived as 52.0836 m/s × 1 m / 6,000,000, and labels this as reference
kinematic viscosity only; the full compressible solution uses Sutherland viscosity. The annotation now
matches the frozen config and SU2 runtime banner: farfield, adiabatic wall, monitoring and plotting markers;
it omits unsupported `MARKER_DESIGNING`.

The proof copy of `su2-results/log.SU2_CFD` was copied byte-for-byte from the retained run file. Its
3,045,192 bytes, 27,120 CRLF pairs and SHA-256 `aab8d165671b30d0ed1224fbd11bff4e6d4bb9a151a221dce52d74549c823543`
are unchanged. Added `-text` coverage for `l6-results/log.*` and `su2-results/log.*`; inherited evidence patterns remain unchanged. No whitespace-check exceptions were added. Raw-output whitespace findings remain disclosed.
Captured Windows user paths in committed W-4 proof text are represented as `%USERPROFILE%` to preserve
the executable location shape without committing the account name. The binary SHA-256 remains the identity
check; this redaction changes no solver input or raw output.

`closing-manifest.json` binds every W-4 proof file except itself, plus the three Windows case YAMLs, to
the staged and committed Git blobs. Its `source_base_sha` is `fd96651e6dd3ad9c01e4fbaa07c62a11fa553c09`,
the tested source base; the final evidence commit is a descendant and is reported separately at handoff.
This does not replace the historical launch freezes or `hash-manifest.json` precompletion snapshot.
The corrected metadata passed the case schema; the one SU2 run and the original iterative oracle remain
unchanged, with the oracle **NOT MET**. CFL3D comparison remains not admitted; no momentum residual is
reinterpreted, no solver rerun occurred, and L3 remains unlaunched. This correction used 2 of 2 cycles.
