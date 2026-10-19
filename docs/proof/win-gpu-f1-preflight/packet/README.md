---
id: proof-win-gpu-f1-r217-packet
title: "Ruling 217 F1 dependency preflight execution packet"
type: proof-pack
status: proposed
owner: "@pc-f1-r217-packet"
phase: preflight
tags: [windows, wsl, gpu, openfoam, petsc, ruling-217]
links:
  - { to: plan-windows-gpu-f1-preflight-r217, rel: implements }
  - { to: coordination-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  Reviewed-only, self-testing host and Linux packet for one bounded disposable Ruling 217 metadata/download preflight, with a committed checkpoint before cleanup.
---

# Ruling 217 preflight packet — frozen source, no live run here

## Live-defect repair proof (offline only, 2026-10-12)

**Goal:** repair exactly the encoded apt URI mismatch and dynamically shadowed checkpoint
path found in the separate `win/gpu-f1-r217-execute-r220` execution tree. **Done when:**
both regressions are observed red, the complete offline packet suite and relevant gates
pass, and the candidate repair is committed and pushed for immutable independent review. **Not in scope:** WSL, recovery,
live cleanup, authority, resource limits, signer/input contracts, or any other runtime
change. **Tier:** T1. **Fan-out cap:** one author; independent acceptance belongs to the
reviewer. This section supplements the prior Ruling 220 repair proof below.

The change surfaces are apt URI capture → signed-index basename/size matching → transaction
lock, and wrapper checkpoint path → finalizer callback scope → manifest `--frozen` → Git
checkpoint directory. The lock schema, signed filename/hash/size authority, original URI
and apt cache filename remain unchanged. Product UI, storage migrations, and domain data
modelling are not applicable to this representation/scoping repair.

Execution graph: grounding → regression fixtures → observed reds → source repair → complete
offline suite and gates → audit/derived views → commit/push → independent review. Edges are
data/evidence dependencies. Independent read-only checks share no write surface and may run
together; code edits and Git mutations remain serial. Every verification node has the
oracle below. Planned and actual author graph: eight nodes, one repair cycle, all proof
floors retained. There are no retry nodes; if validation requires repair, the variant is
the count of unresolved findings, floor zero, exit all controls passing, cap two cycles.
The cap firing stops this track and reports to the coordinator. Modeled work and span are
**Inferred**, equal for the serial author chain; no elapsed estimate or token cost is
invented. Actual suite latency is emitted below and total author latency comes from the
closing audit marker. Scope or authority drift stops the repair without dropping a gate.

**Verified source observation, read-only:** the execution tree's
`run-1/live/stage-simulate.stderr.txt` reports
`cpp-13_13.3.0-6ubuntu2~24.04.1_amd64.deb/1042, found 0`; `apt-uris.stdout.txt` contains
the otherwise identical URL basename with `%7e`. Its `cleanup-receipt.json` records
checkpoint `Substring` failure, no checkpoint SHA, and cleanup withheld. These observations
are failure provenance, not a successful live proof or permission to recover the target.

**Verified red before production edits:** `py -3 -B packet_selftest.py` exited 1 with
`ValueError: expected one signed-index URI for cpp-13_13.3.0-6ubuntu2~24.04.1_amd64.deb/1042, found 0`.
Windows PowerShell 5.1 `SelfTest.ps1` exited 1 with
`canonical checkpoint callback failed: checkpoint: checkpoint freeze received a shadowed path`.
The PowerShell regression parses the actual wrapper, evaluates its checkpoint path assignment,
loads its actual `Save-Checkpoint` and `Invoke-GitCommit` functions, and invokes them through
`Invoke-Finalization`. Only the child launcher and file writers are inert mocks. A temporary
fake SHA output exercises the actual commit-path conversion and SHA read without Git effects.
Manifest freezing and binding must observe the canonical `run-1/checkpoint` directory;
Git's scoped add must receive exactly `docs/proof/win-gpu-f1-preflight/run-1/checkpoint`.

Class → sweep → derive → prevent:

- **URI representation mismatch:** sweep all tracked Python URL-basename readers under
  `docs/proof`, `tools`, and `tests`. The B2 binder already decodes; the F1 matcher is the
  undecoded reader. Use stdlib `unquote` once on the final URL segment, preserving literal
  plus, original URL/cache identity, exact size, and exactly one match. Reject malformed
  escapes, invalid UTF-8, decoded separators, and control bytes instead of repairing them
  into a match. Fixtures cover `%7e/%7E`, `%2b/%2B`, `%3a/%3A`, literal plus, one-pass
  decoding, size mismatch, duplicate decoded identity, and unsafe/malformed basenames.
- **PowerShell dynamic callback shadowing:** sweep tracked PowerShell callback parameters
  and path/state variables under the same roots. The F1 `$Checkpoint` callback/directory
  collision is the observed instance; the other five finalizer callbacks have no matching
  wrapper path variables. Rename the directory `$CheckpointPath`, name all finalizer
  scriptblocks `*Action`, and retain named-argument aliases. The actual-body regression
  binds both directory consumers, rather than merely recording that a callback ran.

Both classes and their executable controls are also recorded in
`docs/lessons/defect-classes.md`. New fixtures run in the **offline-fast ring**, with no WSL,
apt, network, process cleanup, or live wrapper invocation. **Verified initial green costs:**
Python packet suite **0.527 s**, Windows PowerShell 5.1 **0.353 s**, PowerShell 7 **0.254 s**,
emitted on each suite's normal path. The authority scanner and its planted negatives,
transaction, manifest and completed-guard self-tests, all three PowerShell AST parses,
Git Bash syntax checks on all four shell scripts, and `check-wsl-inline.py` also passed.
`tools/check-docs.py` passed. Graph validation found zero defects, orphans, and index drift;
131 preexisting advisory review suggestions remained. All ten offline-safe verify gates passed;
application-core, application-adapters, and Windows-store gates were explicitly skipped because
their build/store checks are outside this packet repair. The real Git-body strengthening stayed
green and did not require a second production repair cycle. The coordinator directed publication
of the candidate for immutable independent review; no author self-acceptance is claimed.
The canonical graph tool flags the prior packet/environment reviews and contract-preparation
note for review, without changing historical verdicts or the invocation contract's bytes.

Residual risk: the repaired packet has no live lifecycle or cleanup proof. The accepted
invocation contract remains bound to the earlier packet bytes; any future runtime use
requires the existing independent acceptance and contract process. Recovery remains a
separate decision. Neither successful offline mocks nor unchanged authority scans authorize it.

## Ruling 220 repair evidence (offline; acceptance remains open)

Goal: repair only P1 final cleanup reap and P3 exact attempt discovery. The surface list is
`/proc` argv inspection → recorded pid/pgid/sid and group selection → TERM/KILL controls →
cleanup-stage finally → finalization receipt → offline regression tests and this README.
UX/UI and domain persistence changes are not applicable. No live packet execution is authorized here.

Execution graph (one author, one repair attempt): grounding → red fixtures → repair → complete
offline suite and repo gates → audit/derived views → commit/push. Each edge consumes the previous
node's evidence; independent read-only checks may overlap. There is no repair retry loop. The
independent reviewer, not this author, clears hard vetoes after the packet handoff. Duration is
measured by the closing audit run; token cost is not recorded. The preexisting implementation and
stdlib suffice; no dependency or new execution stage is added.

**Verified red before the fix, 2026-10-12:** Windows PowerShell `SelfTest.ps1` exited 1 with
`P1 startup cleanup launch escaped final reap`; Python `packet_selftest.py` exited 1 with
`AssertionError: P3 stage: runA selected runAB`. These are host-only planted callbacks/process rows,
not observations of a running Linux workload. No WSL, apt, network, install, build or case ran.

Class → sweep → derive → prevent: P1 is stale termination evidence after a cleanup launch;
P3 is prefix membership mistaken for attempt identity. The sweep covers every packet-owned stage
and guard discovery, saved-owner selection, cleanup stage launch, and finalization callback. Controls
are final bounded stop/reap after launch and after all callbacks, plus exact NUL-delimited argv and
recorded leader identity checks. The offline fast ring must reject startup/ack/timeout cleanup failures,
runA/runAB stage and guard collisions, and recorded identity mismatch while preserving leader-gone
child cleanup. These specialize the existing R217 cleanup/ownership defect classes.

**Verified green after the fix:** `packet_selftest.py`; `SelfTest.ps1` on Windows PowerShell
5.1 and PowerShell 7.6; authority scanner including planted negatives; transaction, manifest
and completed-guard self-tests; AST parsing of all three `.ps1` files; Git Bash `-n` on all
four `.sh` files; `tools/check-wsl-inline.py`; `tools/check-docs.py`; graph validation (zero
defects); and all ten offline-safe verify gates. The application-core, application-adapters,
and Windows-store gates were explicitly skipped because their build/store checks are outside
this packet-author task. No product-code path changed; the join's docs-only ring decision
belongs to the coordinator. Existing graph review suggestions remain open.

Measured offline-fast fixture costs: PowerShell 5.1 **0.293 s**, PowerShell 7.6 **0.214 s**,
and Python **0.464 s** before the precommit inspection-race control below. Both fixture suites emit
elapsed time on their normal path. One authorized repair cycle includes the precommit correction below. The exact recorded
`identity.sh` launch wrapper is recognized before exec, with the exact owner-file token and
intended stage/guard argv; after exec, the exact interpreter/script/run tokens are required.
Unavailable, malformed, or undecodable `/proc` argv remains unknown and fails closed. A missing
cmdline is omitted only after independent `/proc/<pid>` stat proves that PID absent. Runtime
process teardown, target absence, protected-state equality, and the separately reviewed
invocation contract remain **unobserved**. This author claims source repair and offline proof,
not independent acceptance or authority to run the packet.

**Precommit correction, Verified red first:** source inspection found that the new scanner would
reject a process that exited between `ps` and `/proc`, including `ps` itself. The added offline
fixture failed with `RuntimeError: argv inspection failed for pid 101: argv disappeared` before
the correction. Class → sweep → derive → prevent: asynchronous inspection can confuse a proved
exit with an unreadable live process; the sweep covers every new argv read. A second independent
PID stat now permits omission only on `FileNotFoundError`. Present-PID and denied-stat fixtures
must fail closed, while independently absent-PID fixtures must pass. This is a precommit defect
created and caught in this repair, not live-runtime evidence; it is recorded here because the
author's authorized paths exclude the defect register. The coordinator can consolidate the class
into the register at the join.

This packet is for **one** reviewed live attempt in an isolated EXECUTE worktree. The PACKET author did not run it.
It permits a disposable dependency simulation and resolved `.deb` acquisition for licence/size inspection only.
`cfdw-openfoam2512` is read only for the completed-L3 guard and signed Ubuntu rootfs verification. The target is
exactly `cfdw-petsc-cuda132-f1` at `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`.
[Ruling 217](../../../notes/rulings.md) and the [execution plan](../../../plans/windows-gpu-f1-preflight-r217.md)
govern. Mac is the decision owner. The coordinator lease had reached epoch **5** when this repair was authored;
the packet remains epoch-agnostic. Independent Astra acceptance of these exact packet bytes and coordinator
reconciliation of the earlier plan text precede any EXECUTE invocation.

## Inputs and invocation

Run from the native PowerShell (`pwsh` on an ARM64 host) in the **EXECUTE** worktree after packet acceptance, using
reviewed absolute local paths on any drive letter, without whitespace. Required inputs are the already existing B2 Ubuntu Noble WSL rootfs tarball, its matching Ubuntu
`SHA256SUMS` and `.gpg`, reviewed binary OpenFOAM and NVIDIA keyrings, a unique run id, the executor's coordination
session, and a **separately reviewed and committed** `run-1/invocation-contract.json`. No input is silently fetched.
The wrapper verifies that contract, its Git blob, Mac's live epoch against the separately reviewed contract value,
the exact execution branch/session, all packet
source blobs, and all local input hashes **before the first WSL effect**. The contract is created only after Astra
accepts the packet, so its packet hash map has no circular dependency.

```powershell
& .\docs\proof\win-gpu-f1-preflight\packet\Invoke-Preflight.ps1 `
  -Rootfs C:\reviewed\ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz `
  -UbuntuSums C:\reviewed\SHA256SUMS `
  -UbuntuSignature C:\reviewed\SHA256SUMS.gpg `
  -OpenFoamKeyring C:\reviewed\openfoam.gpg `
  -NvidiaKeyring C:\reviewed\cuda-archive-keyring.gpg `
  -Contract C:\Projects\CFD-Workbench-win-gpu-f1-r217-execute\docs\proof\win-gpu-f1-preflight\run-1\invocation-contract.json `
  -RunId <REVIEWED_UNIQUE_ID> -AgentSession <EXECUTOR_SESSION>
```

The path examples are placeholders, not local files. The rootfs is the Ubuntu noble WSL rootfs for the host's
architecture (Ruling 237), pinned by name and SHA-256 in `architecture.py`'s table: amd64
`ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz` at `8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea`
(the B2 356,739,129-byte archive), arm64 `ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz` at
`fecec1d9b7b750c12c109edb49c13c1006f4a2efabb9b8bf341f11c4c9f2ef11`; `verify-rootfs.sh` checks its
Ubuntu detached signature with the protected distro's cloud-image keyring before target creation. NVIDIA's expected
key fingerprint is `EB693B3035CD5710E231E123A4B469963BF863CC`, established in the
[B2 source-verification receipt](../../win-gpu-b2/source-verify.stdout.txt). The OpenFOAM publisher fingerprint is
fixed at `DC93C096174122E256DA24063386DD74948D208F`, also captured in the
[earlier repository key proof](../../win-routes/repo-key-fingerprint.txt). Actual `gpgv VALIDSIG` signer fingerprints
must be in the reviewed contract's allowlists; all keyring fingerprints must also be allowlisted. Ubuntu cloud-image
and archive signer sets remain **Flagged until independently reviewed** in the committed invocation contract.

The contract has schema `cfdw-f1-r217-invocation/2`, `architecture` (`amd64` or `arm64`, the dpkg name; the
launcher refuses a contract prepared for another architecture than the one it detects), `leader: "mac"`, a
positive integer `leader_epoch` set to the
current coordinator-reviewed value at invocation, exact `target`, `protected`,
`target_path`, `session`, and `execution_branch`. It names `accepted_packet_commit` (the separately accepted source
commit) and `packet_sha256` mapping **every** `packet/**` relative path to its SHA-256. Its `inputs` map has exact
absolute `path` and SHA-256 for `rootfs`, `ubuntu_sums`, `ubuntu_signature`, `openfoam_keyring`, `nvidia_keyring`
and `b2_licenses` (six; the B2 package lock left the input set at Ruling 240 node C3, see the lock model below).
Its `allowed_signers` map has reviewed 40-hex fingerprint arrays for `ubuntu_rootfs`,
`ubuntu_archive`, `openfoam`, and `nvidia`; the fixed OpenFOAM/NVIDIA anchors must be present. The executor and
coordinator review those values against primary publisher metadata and local signed evidence, commit the contract on
the clean execution branch, and only then invoke the wrapper. A placeholder, uncommitted contract, stale epoch,
unapproved signer, byte mismatch or dirty worktree blocks before WSL. The runtime freezes the committed contract
SHA-256 and passes it into guest signer checks.

## Effects and evidence

The wrapper starts one host monotonic deadline **before the final precheck**. Right after the contract precheck it
records the host's OS architecture, CPU and GPU names (`host-observations.json`, from `RuntimeInformation` and CIM)
and `architecture.py` maps them once into `host-identity.json` (Ruling 238 (1)); an unmapped or emulated host, or
a contract for the other architecture, is refused there with the receipt still written, and target creation
requires that receipt. The token comes from CIM's `Win32_OperatingSystem.OSArchitecture`; the process view
(`RuntimeInformation`, `PROCESSOR_ARCHITECTURE`) must agree with it, so on an ARM64 host the launcher is the
native `pwsh`: Windows PowerShell 5.1 there is an ARM64X image that runs as an x64 process when an x64 parent
starts it and is refused as emulated. The guest's `probe` stage records `guest-identity.txt` (`dpkg --print-architecture`,
`uname -m`, the `nvidia-smi` GPU name when WSL exposes it) and the host compares it with the receipt before any
network stage. The wrapper checks exact distro absence,
protected presence, rootfs hash and signature, then starts the copied/adapted B2 `completed_guard.py` before the
first target-creation effect. The guard samples every 5 seconds through unregister. Its terminal projection includes
unit result/status, final iteration, marker counts and log byte/mtime/tail/full hashes. Missing, stale, partial or
changed observations stop new work. Guard events and state are frozen into both evidence commits; the final binding
has a contiguous complete-row prefix and a full before/after protected snapshot comparison.

Only `wsl.exe --install --from-file <verified local rootfs> --name cfdw-petsc-cuda132-f1 --location
<repository parent>\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1 --vhd-size 8GB --no-launch` creates the target; the
location derives from the EXECUTE worktree's parent directory (Ruling 237: never a constant, never a parameter), the
contract records the absolute path it was prepared for in `target_path`, and the launcher refuses a contract whose
path differs from the derived one. WSL paths follow the drive letter (`/mnt/<letter>/`). The next
Linux stage measures both root block-device and filesystem maxima and requires each ≤8,589,934,592 bytes. The host
also checks target physical VHD ≤8 GiB and the target drive's free space: at or above the contract's
`host_start_floor_bytes` before creation and `host_reserve_bytes` throughout (Ruling 238 (3): both come from the
host's worksheet recorded with the attempt, never from the old PC's numbers; a contract without them refuses). **No network stage starts before both capacity checks pass.** The host records five-second capacity rows.
Target Linux stages are `probe`, `repositories`, `simulate`, `download`, `inspect`, and cleanup `delete-debs`.
Every Linux entry uses `taskset --cpu-list 0-1` (protected guard uses CPU 0), `nice -n 10`, and foreground
`setsid --wait`; it records effective `Cpus_allowed_list`, nice, PID, PGID, SID and cmdline. Stage leaders must have
PID=PGID=SID. Host WSL lifecycle and Git commands have affinity **N/A**. Stage owner files publish atomically before
work; exact NUL-delimited `/proc` argv tokens also cover missing owner files. The fixed packet script,
run ID/root (or guard session/event path), and recorded leader PID/PGID/SID must agree; substring
membership is never an ownership test. `ps` or `/proc` inspection failure is unknown, never absence.
Every completed stage proves residual absence, and a leader-gone owned group is signalled and rechecked. Uncertain
identity blocks unregister. Timed-out creation is bounded by repeated registration/list/path observations and remains
an uncertain failed attempt even if a stable absence is later seen.

The repositories stage uses exact Ubuntu Noble main/universe, updates and security indices, OpenFOAM Noble and
NVIDIA CUDA 13.2, rendered per architecture from `architecture.py`'s table (amd64: `archive.ubuntu.com`,
`security.ubuntu.com`, NVIDIA `ubuntu2404/x86_64`; arm64: `ports.ubuntu.com/ubuntu-ports`, NVIDIA
`ubuntu2404/sbsa`); `make` is asked for as `make:<architecture>`. `apt-get update` must authenticate them with explicit keyrings. It copies all five `InRelease`
files from an isolated target-only apt lists directory, verifies each via `gpgv`, records key fingerprints and hashes package indices, and freezes index bytes as a
compressed tar. **Lock model (Ruling 237 (1)).** The roots are pins the publisher keeps: `cuda-toolkit-13-2=13.2.2-1`,
the five OpenFOAM `2512.0-2` packages, and the unversioned tool names GCC/G++/gfortran 13, OpenMPI, BLAS/LAPACK,
`pkg-config` and `make:<architecture>` (`transaction.py PINS`, `roots`). The run on each architecture resolves them
(`apt-get --simulate`), `transaction.py lock` refuses before any index read when a pin is absent from the transaction
or at another version, refuses a row the index serves for another architecture, and writes the run's own
`transaction-lock.json` (schema `cfdw-f1-r217-transaction-lock/2`, `architecture`, `pins`) hashed against that run's
signed indices. No prior lock is an input: B2's 135-row amd64 lock is a regression fixture
(`tests/test_f1_transaction_roots.py`), not a root list, because its exact pins stop resolving once `noble-updates`
drops a superseded version (register entry LOCK-PINS-A-MOVING-SUITE). The simulation excludes recommendations, requires zero removals, maps every
`apt-get --print-uris` row to one signed-index SHA-256/size row, and confirms an exact pinned re-simulation. Only
then does `apt-get download` fetch that resolved set into the disposable guest. `transaction.py` verifies every
downloaded archive against the authenticated index, streams package payloads without extracting them to disk,
reconciles signed-index and archive-control `Installed-Size`, records licence-notice hashes, and writes
`transaction-lock.json`, `license-map.json` and `capacity.json`. A mismatch or missing control value blocks a
complete result while retaining per-row evidence; an absent signed-index value is labelled control-only. Archive
payload bytes remain a separate quantity. Capacity evidence joins measured guest block/filesystem/free bytes and
host minimum free/required floors with the current 8-GiB VHD. The proposed future 10-GiB F1 headroom and full
source/build/case threshold remain **Flagged** outside this preflight.

All captures go to `docs/proof/win-gpu-f1-preflight/run-1/live/` in the EXECUTE worktree. Before deleting archives
or unregistering, `manifest.py` freezes a complete event prefix and raw/derived captures under `run-1/checkpoint/`;
the wrapper makes a real Git commit and verifies every manifest entry against that commit's blob. It first stops
the exact owned workload, regardless of guard health. **If checkpoint binding fails, it still stops the owned
workload and observer, performs no archive deletion or unregister, and emits an explicit Mac decision handoff.**
With a proved checkpoint and stop, it removes only resolved `.deb` files inside the target. Every deletion-stage
exit runs a fresh bounded stop/reap in `finally`, including startup/acknowledgement/timeout errors. It checks residual absence,
terminates and unregisters the exact target, independently checks registration/path absence, takes a post-unregister
guard sample and protected snapshot, stops/reaps the guard through an independent finally path even if snapshot
fails, binds the available complete event prefix, and performs a final unconditional bounded owned-work stop/reap
after the last finalization callback. No owned workload launches after that final check; its result replaces the
earlier `owned_stopped` state. It then commits a separate `run-1/final/` manifest and cleanup
receipt. Partial creation treats registration and path independently; recursive path removal is allowed only
after checking the fixed absolute location and rejecting a reparse point. The initiating error is retained alongside
cleanup errors.

The whole host step, including final guard/absence verification, is capped at **3,600 seconds**. New stages stop at
45 minutes. A pre-cleanup Git checkpoint must finish by 50 minutes; the last 10 minutes are reserved for cleanup.
Every child/control wait uses `min(stage limit including startup, remaining absolute work/checkpoint/host deadline)`;
the WSL distro-list control is also bounded. Stage ceilings are creation 300 s,
guest probe 60 s, repository/index work 300 s, simulation 300 s, package download 1,200 s, inspection 600 s,
deb deletion 120 s, unregister 120 s and guard stop 20 s. A cap firing is a failed attempt, not a retry.

Package installation, PETSc/petsc4Foam source acquisition, compilation, an OpenFOAM case or solver run, global WSL
shutdown, and target resizing are prohibited. `authority_scan.py` plants each forbidden command class and proves
the two authorized apt simulation/URI forms and `apt-get download` remain allowed; it scans all executable packet
sources except its own fixture file. The `check-wsl-inline` gate forbids an inline multiline WSL program.

## Packet-only checks

These do **not** invoke the live wrapper:

```powershell
py -3 docs/proof/win-gpu-f1-preflight/packet/authority_scan.py
py -3 -B docs/proof/win-gpu-f1-preflight/packet/packet_selftest.py
powershell -NoProfile -File docs/proof/win-gpu-f1-preflight/packet/SelfTest.ps1
py -3 docs/proof/win-gpu-f1-preflight/packet/transaction.py self-test
py -3 docs/proof/win-gpu-f1-preflight/packet/manifest.py self-test
py -3 docs/proof/win-gpu-f1-preflight/packet/completed_guard.py self-test
py -3 -m unittest discover -s tests -p 'test_f1_*.py'
pwsh -NoProfile -File tools/run-f1-powershell-suites.ps1
py -3 tools/check-f1-host-bindings.py
py -3 tools/check-wsl-inline.py
py -3 tools/check-docs.py
```

The wrapper and helpers were authored from B2's
[`completed_guard.py`](../../win-gpu-b2/completed_guard.py),
[`capture-completed-install.ps1`](../../win-gpu-b2/capture-completed-install.ps1),
[`stop-owned.sh`](../../win-gpu-b2/stop-owned.sh),
[`residual-owned.sh`](../../win-gpu-b2/residual-owned.sh),
[`build-package-lock.py`](../../win-gpu-b2/build-package-lock.py) and
[`verify-debs.py`](../../win-gpu-b2/verify-debs.py), with a single 3,600-second host boundary and no install/test
stage. The prior proof is a source of mechanics, not permission to reuse its disposable identity or mutate its
evidence.
