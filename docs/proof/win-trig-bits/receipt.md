---
id: proof-win-trig-bits-20261008
title: Windows .NET 10 trigonometric bit dump
type: proof-pack
status: draft
owner: "@pc"
tags: [windows, determinism, trigonometry, evidence]
links:
  - {to: investigation-cat-determinism, rel: relates-to}
review-by: 2027-10-08
summary: One Windows x64 Release run records the requested .NET 10 trigonometric bit patterns and host/runtime details. It is a single-host measurement, not cross-platform proof.
---

# Windows trigonometric bit dump

## Goal and scope

This evidence measures the four requested trigonometric expressions for integer `i` from 0 through 80 and the four scalar expressions. It makes no source or test change outside this proof directory. `bits.stdout.txt` is the program's stdout, one line per loop value followed by four scalar lines.

## Inputs and host

- **Verified:** source: `Program.cs`; SHA-256 `2e8ce6834bbc4f040a5a140ffcfc7694308144c53fd7ed9e6f645580efb4dc61`.
- **Verified:** target framework `net10.0`; SDK `10.0.203`; runtime `10.0.7`; architecture `win-x64`. The captured `dotnet --info` is in `dotnet-info.txt`; `%USERPROFILE%` replaces the Windows account path.
- **Verified:** Microsoft Windows 11 Pro, OS version `10.0.26300`, build `26300`, 64-bit; CPU `12th Gen Intel(R) Core(TM) i9-12900H`, 14 cores / 20 logical processors.
- **Reported by coordinator:** during the build and run, `cfdw-l3-20261008-r1.service` was active with six `simpleFoam` ranks; latest iteration 14,069 and load about 6.07. This host-load context was supplied by the coordinator and was not independently sampled in this receipt.

## Commands and results

Commands ran from the repository root. `DOTNET_ROOT` was set to `%USERPROFILE%\.dotnet` and that directory was prepended to `PATH`, selecting the pinned SDK. Timings are wall time from a PowerShell `Stopwatch` around each command.

| Command | Exit | Elapsed |
|---|---:|---:|
| `dotnet build docs/proof/win-trig-bits/TrigBits.csproj -c Release -m:2` | 0 | 6,714 ms |
| `dotnet run --project docs/proof/win-trig-bits/TrigBits.csproj --configuration Release --no-build > docs/proof/win-trig-bits/bits.stdout.txt` | 0 | 1,174 ms |

The Release build capped MSBuild concurrency at two nodes. The run emitted 85 lines (81 indexed rows and four scalar rows) and 13,584 bytes. `bits.stdout.txt` contains the unmodified stdout.

## Raw-byte preservation and repair accounting

- **Verified:** `bits.stdout.txt` is 13,584 bytes with SHA-256 `7851d86309671f9c4cf17876c9b99e81aaf6b80ea751709c41c5c04c7209192d`.
- `.gitattributes` applies the exact-path rule `bits.stdout.txt -text` so Git preserves the captured CRLF bytes without text normalization.
- **Repair cycle 1:** corrected the direct `dotnet build Program.cs` command shape after its recorded `MSB4025` failure; the successful Release build and run are above.
- **Repair cycle 2:** corrected Git text normalization of the captured stdout by adding the exact-path attribute and restaging the retained working-tree bytes. No measurement was rerun.

## Repair validation

- **Verified:** the retained working file and staged stdout blob are each 13,584 bytes with SHA-256 `7851d86309671f9c4cf17876c9b99e81aaf6b80ea751709c41c5c04c7209192d`; the staged bytes have 85 CRLF terminators, no bare LF, and no trailing spaces or tabs. Their 85 line payloads match the prior committed output, so the numeric rows are unchanged.
- `py -3 tools/check-proof-pii.py`: pass, 0 user-path or machine-SID hits.
- `git diff --cached --check -- . ':(exclude)docs/proof/win-trig-bits/bits.stdout.txt'`: pass, exit 0.
- Unscoped `git diff --cached --check -- docs/proof/win-trig-bits/bits.stdout.txt`: exit 2 with 85 trailing-whitespace findings, one for each CRLF line. The byte-aware check above confirms these are line terminators retained for raw-output fidelity, not spaces or tabs.

The initial attempt to invoke `dotnet build` directly on `Program.cs` exited 1 in 410 ms with `MSB4025` because MSBuild parsed the source as a project file. The successful build used the small `TrigBits.csproj` wrapper and the one-source-file console program.

## Observed comparison

Comparing the paired bit columns in `bits.stdout.txt` on this Windows host:

- `Math.Cos(Math.PI*i/80.0)` and `double.CosPi(i/80.0)` differ at 42 indices: 11, 21, 22, 23, 25, 26, 27, 30, 31, 32, 34, 35, 36, 37, 38, 39, 40, 41, 42, 44, 45, 46, 47, 49, 50, 51, 52, 53, 54, 55, 57, 59, 62, 63, 64, 65, 67, 68, 71, 72, 73, 74.
- `double.SinPi(i/80.0)` and `Math.Sin(Math.PI*i/80.0)` differ at 36 indices: 11, 20, 22, 23, 26, 27, 28, 32, 44, 49, 51, 53, 54, 55, 57, 58, 59, 61, 62, 63, 64, 65, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80.
- Scalar bits: `Math.Atan(0.18)=3fc6cbbfd8acff50`; `Math.Atan2(0.06,0.94)=3fb0517b52aaa629`; `Math.Sin(0.1)=3fb98eaecb8bcb2c`; `Math.Cos(0.1)=3fefd712f9a817c1`.

## Limitations

This is one Windows x64 runtime measurement. It does not establish behavior on macOS, another Windows runtime, another CPU, or another SDK/runtime patch. It does not independently verify the coordinator-reported solver load. No implementation recommendation follows from these bits alone.
