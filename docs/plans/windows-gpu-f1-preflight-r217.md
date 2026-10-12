---
id: plan-windows-gpu-f1-preflight-r217
title: "Optimized execution graph - Windows GPU F1 dependency preflight"
type: doc
status: proposed
owner: "@pc-f1-preflight-coordinator"
tags: [windows, wsl, gpu, openfoam, petsc, coordination, ruling-217]
links:
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: proof-win-gpu-f1-dependency-worksheet, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  A serial, independently reviewed run-packet and one bounded disposable-WSL preflight close the Ruling 217
  dependency evidence while preserving the protected distro and prohibiting installation, builds and case runs.
---

# Windows GPU F1 dependency preflight — optimized graph

## Goal state

**Goal:** execute Ruling 217 once and produce a manifest-bound exact transaction lock, licence map, package sizes,
capacity bounds, completed-L3 guard evidence and verified cleanup.

**Done when:** the reviewed packet completes or records a bounded failure; the disposable distro and downloaded
packages are absent; `cfdw-openfoam2512` matches its pre-run snapshot; an independent Astra review gives ACCEPT or
VETO; and a GitHub PR reaches the Mac leader.

**Not in scope:** `dpkg -i`, `apt-get install`, PETSc or petsc4Foam source acquisition, compilation, a solver or case
run, any change to `cfdw-openfoam2512`, or a build/benchmark authorization claim.

**Tier:** T1. **Fan-out cap:** 1. **Live-operation cap:** one attempt and 60 minutes from the final host precheck
through unregister verification. A live failure triggers cleanup and a blocked receipt; it does not consume an
automatic retry.

## Grounded constraints

| observation | confidence | consequence |
|---|---|---|
| Ruling 217 authorizes the named disposable distro, apt simulation, package downloads for licence/size inspection, and unregister. | Verified — `docs/notes/rulings.md` Ruling 217 | The packet may perform only these effects. |
| C: free space was 1,844,653,740,032 bytes; the ruling floor is 19,684,091,961 bytes. | Verified on 2026-10-11 | Recheck immediately before import and throughout the run. |
| `wsl.exe --list --quiet` contained only `cfdw-openfoam2512`. | Verified on 2026-10-11 | Any pre-existing target distro is a stop. |
| WSL 2.7.14 exposes `wsl --install --from-file ... --vhd-size <size>` for a new local-file distribution. | Verified from local `wsl.exe --help`; Microsoft documents `--from-file` custom distribution installation and `defaultVhdSize` as a maximum | The packet uses target-only `--from-file ... --vhd-size 8GB`; it does not use `--manage --resize` or global `wsl --shutdown`. Verify guest capacity before network work and unregister immediately if the ceiling is not 8 GiB. |
| The B2 rootfs archive remains available at the committed B2 hash and size. | Verified by local inventory; bytes must be re-hashed by the packet | Reuse is allowed only after signed-rootfs verification. |
| Mac leadership was reclaimed at epoch 3 for this run. | Verified by `coord leader reclaim mac --host claude --ttl 900` | Carry epoch 3 at dispatch and renew it before every join; a changed epoch stops the plan. |

The governing source is `.claude/knowledge/execution-graph-optimization.md`; this repository does not expose that
pack knowledge file as a docs-graph node, so it is cited by path rather than recorded as a dangling typed edge. Prior
B2 receipts show that WSL lifecycle, guard and cleanup share one exclusive host resource, so splitting the live path
would add unsafe seams.

## Naive graph and mandatory floors

The naive sequence has 12 nodes and span 12: inspect → script → import → configure repos → simulate → download →
inspect → calculate → delete files → unregister → document → review. The mandatory floors are a frozen reviewed
packet, live leader fence, host/VHD capacity checks, signed-index/hash verification, zero-removal oracle,
completed-L3 pre/post equality, prohibited-command scan, fail-safe unregister, manifest binding, independent result
review, gates and audit.

## Optimized graph

| id | capability | goal | inputs | exit condition / oracle | tier | depends on |
|---|---|---|---|---|---|---|
| N1 | Reasoning | Author one self-testing packet and proof schema. | Ruling 217; B2 controls; F1 worksheet | Static allowlist contains no install/build/source/case command; planted forbidden command makes the self-test fail; every Linux helper inherits CPU 0-1 and nice 10 and captures effective values. | T1 | none |
| N2 | Independent review | Adversarially review frozen packet and bounds. | N1 commit | Astra ACCEPT; any unbounded effect, missing cleanup, unsigned source or weak oracle is VETO. | T1 | N1 |
| N3 | Deterministic mechanics | Join the accepted packet and fence live authority. | N2 verdict; renewed leader epoch | Conductor join green; exact reviewed script hashes unchanged; live epoch matches. | T0 | N2 |
| N4 | Deterministic mechanics | Run one contained preflight, commit its pre-cleanup evidence checkpoint, and always clean it up. | N3 packet; rootfs; host/WSL state | Pre-cleanup evidence commit exists before deletion; target absent at end; protected continuous-guard snapshot equal; live elapsed ≤3,600 s; every stage and exit recorded. | T0 | N3 |
| N5 | Reasoning | Finalize lock, licence, size, capacity and cleanup receipt from the committed checkpoint and cleanup captures. | N4 captures and checkpoint | Every resolved `.deb` has repository, version, architecture, URL, signed-index SHA-256, actual SHA-256, download bytes, Installed-Size and licence-notice status; totals reconcile; cleanup receipt binds the checkpoint. | T1 | N4 |
| N6 | Independent review | Review result and cleanup independently. | N4–N5 commit | Astra ACCEPT or VETO with exact missing row; protected distro and target absence checked from committed evidence. | T1 | N5 |
| N7 | Deterministic mechanics | Join, gate and push the evidence. | N6 verdict; renewed epoch | Conductor gates and readiness pass for final HEAD; branch pushed. | T0 | N6 |
| N8 | Reasoning | Hand the exact disposition to Mac. | N7 SHA | PR and xmsg name verdict, open F1-6/build authority, and prohibit execution beyond the ruling. | T0 | N7 |

```mermaid
flowchart LR
  N1[Packet author] --> N2[Astra packet review]
  N2 --> N3[Join and live fence]
  N3 --> N4[One preflight plus cleanup]
  N4 --> N5[Derive evidence]
  N5 --> N6[Astra result review]
  N6 --> N7[Join and gates]
  N7 --> N8[Mac handoff]
```

Every edge is a data or decision edge. The shared WSL registry, target path, protected distro and 60-minute clock fail
the independence test, so the live path stays serial. Packet and result reviews cannot overlap their inputs. The
optimized graph collapses target creation through unregister into one owned transaction and derives final evidence
from the committed pre-cleanup checkpoint without a second live run.

## Bounds, loops and containment

| loop / fan-out | variant | floor | exit | cap / fallback |
|---|---|---|---|---|
| Packet repair | count of Astra hard findings | zero | ACCEPT | Two review cycles. At cap, stop and report; no live run. |
| Live stage loop | count of unfinished named stages | zero | cleanup verified after final stage or failure | One live attempt; 3,600 s circuit breaker. A 10-minute cleanup reserve is protected; at 45 minutes unfinished work stops and a pre-cleanup checkpoint is committed by 50 minutes. Any failure records the initiating error, checkpoints available evidence, enters cleanup, and requests a ruling. |
| Package verification | unverified resolved package rows | zero | every row reconciles to signed index and downloaded bytes | No skipped row. Missing metadata blocks N5. |
| Agent fan-out | N/A | N/A | N/A | Width 1. Transient harness failure may be retried once before live execution; no live operation is retried. |

Cleanup is an idempotent `finally` obligation armed before the first target-creation command. Partial creation is
handled by checking both the target registration and dedicated path. Every child/control wait is capped by the smaller
of its stage limit and the remaining host deadline. Linux work starts under a foreground `setsid --wait` owner; its
PID/PGID/SID and command line are acknowledged before work proceeds, and a post-stage residual check must prove it
absent. Before deletion, the packet commits the raw captures, transaction lock, licence map, sizes and capacity record
as a manifest-bound checkpoint. It then deletes downloaded `.deb` files, terminates/unregisters the exact target,
proves the registration and install directory absent, captures a post-run protected snapshot, and records cleanup
failures without replacing the initiating error. The cleanup receipt is committed after absence verification.

## Verification width

| surface | fail clauses, jointly satisfiable |
|---|---|
| Authority | Exact target name/path and Ruling 217 stage allowlist; static scan rejects `dpkg -i`, `dpkg --install`, `apt-get install`, source URLs, compiler/build verbs and case commands. Root: packet sources; recursive: yes; tokens: prohibited commands; allowlist: quoted prohibition tests only. Every Linux helper enters through CPU 0-1/nice 10, then records `Cpus_allowed_list`, PID/PGID/SID and effective nice; an observed mismatch stops. Host-only WSL lifecycle and Git controls are labelled affinity-not-applicable. |
| Protected distro | Reuse B2 `completed_guard.py`: canonical terminal projection fields (unit Result/ExecMainCode/ExecMainStatus/ActiveState/SubState, iteration, marker counts, log size/mtime/tail/full hashes) bind the baseline; five-second samples run from before target creation through unregister; stale/missing state or changed projection stops; shutdown is bounded and sample/state/binding files are manifest-bound. |
| Capacity | Host free ≥19,684,091,961 bytes before target creation; ≥10 GiB throughout; target-only `--vhd-size 8GB`; guest block-device/filesystem maximum ≤8 GiB before network; physical VHD, guest free bytes and host free bytes sampled per stage. Any mismatch unregisters before network work. |
| Transaction | Apt simulation reports zero removals and the exact requested roots; package URI set and simulation set agree. |
| Trust | Ubuntu/OpenFOAM/NVIDIA Release signatures verify with pinned fingerprints/keyrings; each retained index hash is captured; downloaded `.deb` hash matches its signed index row. |
| Evidence | Manifest binds committed blobs; absent values are `Not recorded`, never inferred; totals are recomputed from row data. |
| Cleanup | Download directory absent, target distro/registration/path absent, protected distro present and unchanged. |

## Before versus after

| metric | naive | optimized | confidence |
|---|---:|---:|---|
| nodes | 12 | 8 | Inferred from this graph |
| span | 12 | 8 | Inferred; width remains 1 |
| parallel width | 1 | 1 | Declared |
| deterministic-mechanics nodes | 7/12 | 3/8 plus deterministic mechanics inside N4/N5 | Inferred classification |
| unbounded loops | 1 implicit retry risk | 0 | Declared controls |
| rigor floors | informal | 10 explicit | Counted above |

The 15× coordination multiplier does not buy machine-time speed here. Two independent Astra gates earn their context
cost because the packet author cannot clear its own authorization or evidence veto. All mechanics use repository
scripts and one executor.

## Budget and replanning

Packet author: 30 calls/30 minutes. Each Astra review: 10 calls/15 minutes. Live transaction: 60 minutes from final
precheck through final guard and absence verification, with 10 minutes reserved for cleanup. Evidence derivation and gates: 30 minutes. If author or reviewer budget expires, commit no
partial authorization and stop. If N4 changes shape because a signed index, resize, simulation or capacity check
fails, skip N5 success claims, preserve raw output, run cleanup, and re-plan only after Mac rules.

Re-plan checkpoints: N2 verdict, N3 live fence, the final pre-import checks, any simulation removal, any VHD/host
threshold breach, any hash mismatch, cleanup failure, and N6 verdict.

## Planned versus actual

Pending execution. Record calls, wall time, live elapsed, package count, downloaded bytes, maximum VHD bytes, host
minimum free bytes, repair passes, result verdict and whether every floor remained present.
