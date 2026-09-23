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
