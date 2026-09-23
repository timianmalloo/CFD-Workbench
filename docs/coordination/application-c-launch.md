---
id: coordination-application-c-launch
title: Native adapter author launch and monitoring receipt
type: proof-pack
status: in-progress
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, native, launch]
links:
  - {to: coordination-contract-c-native, rel: implements}
  - {to: coordination-contract-c-api-freeze, rel: depends-on}
  - {to: coordination-application-cancel-drill, rel: relates-to}
review-by: 2026-10-23
summary: Actual conditional Sol collaborator launch after joined B and independently reviewed API freeze.
---

# C launch receipt · 2026-09-23

Owner Ruling 21 conditionally routed one serial built-in Codex C author
requested as `gpt-6-sol`. B joined at `18278c4030de998d3b799ab60c0140aba4a00a3a`;
the final compiled one-assessment API consumer and 15-point interior-station
sampling were independently reviewed by root before launch. The freeze is
committed at `21f2f5bf6e9e2e09fde346a042fdcb65f0f04939`, with docs check
104 artifacts, zero defects and 77 existing nonblocking V16 suggestions.

The Coordinator invoked built-in `spawn_agent` once with `model=gpt-6-sol`,
`fork_turns=none`, task name `/root/coordinator/native_adapter_author`, and
the absolute brief `/tmp/cfd-c-native-brief.txt` (SHA-256
`67640cd2df08a77b78352a0416567ed217336f770dc830fca6a10de0ed93c61c`).
The returned worker name confirms a collaborator started; the host does not
expose its effective inference model. **Requested model: gpt-6-sol; effective
model: Not recorded.** No cost or stronger confinement claim follows.

The exact 24 authored paths are listed in
[the C packet](contract-c-native.md#provisional-exact-path-inventory-24).
Fresh isolated worktree:
`/Users/mallalieut/projects/CFD-Workbench-feature-application-native-adapters`,
branch `feature/application-native-adapters`, session
`cfd-adapters-20260923`, base `21f2f5bf6e9e2e09fde346a042fdcb65f0f04939`.
The worker directly read back that cwd, branch, HEAD, empty porcelain status,
doctor exit 0, leader epoch 2 live, 22 terminal/0 open decisions, and zero
lease overlaps before code. SDK 10.0.203 on macOS arm64 was observed. The
worker planned canonical `/private/tmp/cfd-adapters-cfd-adapters-20260923`
task-local NuGet/.NET/temp/output roots with
`DOTNET_GENERATE_ASPNET_CERTIFICATE=false`; actual process and file receipts
remain due at the first executable checkpoint. Globally cached Avalonia
11.3.14 packages were absent at preflight, so package/API binding is a bounded
first spike, not a presumed available dependency.

Initial worker boundary is at most 90 tool calls, 55 minutes or 100k context,
then a measured handback and replan; no partial join. Exact claims are acquired
for minutes of editing and released, not held for the whole track. The reused
[cancellation drill](application-cancel-drill.md) proves interruption plus
explicit owned-child termination, **not** automatic subprocess cancellation.
Current execution has root, Coordinator and one C worker active, meeting the
three-seat cap. Root owns independent native UI/AX and technical review and
will inspect the first runnable window as soon as the author supplies its
bundle path. Windows runtime, signing and full M1 acceptance remain open.

## Ruling 23 build-output containment correction

The first three `dotnet run` invocations set task-local NuGet, CLI-home and
TMPDIR but did not override MSBuild output/intermediate paths. The author
stopped under Ruling 21 after untracked `bin/obj` appeared in five projects of
its fresh isolated tree. First CLI RED, CLI GREEN and Desktop RED are
transcribed in `/tmp/cfd-c-first-build-transcribed.json` (SHA-256
`722b23187df5beb1a6dc8d436280692faa83352dba492086daf1994f916a3ac6`);
the original tool transcript, not a separately retained raw stdout file, is
the source. The exact no-symlink inventory
`/tmp/cfd-c-output-drift-manifest.json` (SHA-256
`4270e13ccea2e2fec27126b6e4ca7f59367cc47b623042b013ffe4c0edee4a80`)
records ten directories, 150 files and 11,232,864 bytes. No original files
were deleted. Read-only global directory metadata predated the first observed
build, but this limited check cannot establish zero certificate/cache effect.

Owner [Ruling 23](../notes/rulings.md) resolved typed request
`req-01M37R2NXSX4C8Z6V9TP3BEBPD` and authorized one same-worker, ten-minute
corrected build. The worker used the accepted core gate's
`dotnet build --artifacts-path` convention, a fresh canonical task-local
scratch with all six cache/temp roots, disabled certificate generation, build
servers and shared compilation, then ran the built test DLL without a rebuild.
Raw retained receipt
`/private/tmp/cfd-adapters-r23-cfd-adapters-20260923/receipts/receipt.json`
(SHA-256 `c1f300ac24d7aeb98ea697b1ddb2e2374cbca4fe275289d9044d693b0b887e2a`)
binds exact argv/env, stdout/stderr hashes, PID/start identities and exits.
Build and test exited 0; build had zero compiler warnings/errors. Four projects
placed their assets in distinct `artifacts/obj/<project>` and binaries in
`artifacts/bin/<project>/debug`; 136 artifact files contained no symlinks, and
the four asset files name the task-local NuGet cache. Both observed child
processes were absent at direct readback. All original 150 source-tree files
remained hash-identical, with no extras or missing paths. Coordinator accepted
this **output containment only** and resumed normal same-scope Ruling 21 work.
It is not a CLI, native UI, global zero-effect or M1 pass.

## Ruling 24 Avalonia child-process correction

The first Desktop.Tests build used the corrected Ruling 23 output layout, but
Avalonia.BuildServices 11.3.2 started collector PID `22494` after its parent
exited with a missing-`partial` compiler error. The author identified and
terminated only that owned child, then read back its absence. The failed
build's 114 output files and raw receipt remain at
`/private/tmp/cfd-adapters-desktop-build.mwdf8T`; that compiler failure is not
a behavioral test result. A bounded read-only check found a pre-existing
BuildServices ID file (mtime 13:44:43 UTC, before C launch) and a directory
mtime during the build. It did not read the ID or license contents. Any first
transmission or transient global effect is **Not recorded**; no global cleanup
was attempted.

Owner [Ruling 24](../notes/rulings.md) authorized one same-worker correction:
make the window class `partial`, set the pinned package's process-local
`AVALONIA_TELEMETRY_OPTOUT=1`, and measure one build with exact child and output
receipts. The retained raw receipt at
`/private/tmp/cfd-adapters-r24-avalonia.ljA0p3/receipts/receipt.json`
has SHA-256 `3b9294108330cf702dd4f411af1f22649dd8e8672a7023e37d7d19f48d08b05d`.
It records the opt-out in the actual child environment, six local cache/temp
roots, disabled ASP.NET certificate generation and the unique `--artifacts-path`
root. Build PID `24214` exited 1 with four ordinary C# errors (two CLI class
qualifications and two `Func<Task>` method-group mismatches). Live group
sampling saw MSBuild and compiler children, **no collector**; all observed
PIDs and the build group were absent at Coordinator readback. The 114 actual
artifact files match the receipt exactly, with no symlinks. The original 150
source-tree output files still match their pre-retry hashes with no added or
missing files. This clears **Ruling 24 containment only**; the author resumed
ordinary C repair. It does not prove zero historical telemetry, a successful
Desktop build, native interaction or M1 acceptance.

## First C implementation checkpoint

The author reached its first measured checkpoint at 19:11:15 UTC, about 54
minutes after the 18:17 launch marker. Total tool calls across resumed turns
are **Not recorded**; the last slice estimated about 60. Eighteen of 23
author-owned candidate paths exist. The independent native UI review is the
24th path and belongs to root. The solution, argument-free verifier and proof
document remain to be authored; PNG and AX JSON require real capture and are
open. The original ten generated `bin/obj` directories remain retained for
exact-manifest quarantine before any clean author commit. No partial join was
requested. The author appended partial audit
`al-01M37TWEA3C7BNJB90HYJFM6HA` and resumed the same C scope with a fresh
19:11:57 UTC audit marker, bounded by the next 50 minutes, 80 tool calls and
100k context; hitting a cap requires another measured handback and replan.

Root independently ran 15 CLI subprocess cases against the frozen corrected
CLI, covering three `/2` source/Surface identity oracles, accepted source
preservation, a native reopen, normalized `z/c` versus physical metres, the
15-point certified projection, size boundaries, and truthful refusal classes.
The retained summary at
`/tmp/cfd-adapter-review.3sSb4H/cli-bounded-results/summary.json` has SHA-256
`33cb7fd27f69930ccc367eaeca824ed59ff9746e0cce76b97d3fd2c5a13a8a7c`.
A separate invalid-recovery native file CLI case passed without changing its
original bytes or accepted identities; summary SHA-256
`0e584c2c4f0f0668e9cb5b927e645190338d3b4478c88a62c56b848dd2b31045`.
These are bounded CLI results, not native authoring acceptance.

The first desktop build and direct test run passed under task-local output and
the process-local Avalonia opt-out (raw receipt SHA-256
`7ade683705dd62be3f525501b4e21102ed24656a3123e2db91e9c681bb028831`).
An exact owned launch logged `Window.Opened`, but root `cua_repl` returned
`cgWindowNotFound` for the original and a single copied review-only unique-ID
bundle. A pre-correction direct CoreGraphics window query and denied System
Events count are retained **only as diagnostics**; neither is accepted AX,
rendered or keyboard proof. The author stopped the launch-isolation loop,
terminated its exact-owned PID `27098` after identity readback, and left one
exact-owned review PID `27716` live for the pending visibility check. The
separate PID `25730` has unknown ownership and was not touched. Root's
supported CUA inspection remains open.

Root independently reproduced three controller failures against an earlier
frozen DLL, then reran the same cases green against the corrected DLL:
rejected FoilDSL and missing-native opens preserve the active accepted source;
a definite Save As conflict preserves disk bytes and does not enter uncertain
save. The green log at
`/tmp/cfd-controller-review.B2SeOB/receipts/controller-green.log` has SHA-256
`ba24e3564219bfcea6fdd01d43ded3deb53cc615bb6a8221f98cbbc64cf6f770`.
The author separately observed red/green stale-frame, uncertain-durability
and delayed cross-session save cases in its isolated scratch. Its latest
targeted build/test passed, but the later one-line viewport orientation edit
had not been rebuilt at this checkpoint. Controller completion, native
rendering, Windows runtime and full M1 acceptance remain open.
