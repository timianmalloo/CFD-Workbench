---
id: proof-win-cfmesh-probe
title: "W-5 cartesianMesh probe execution record"
type: doc
status: observed
owner: "@win-w5-cfmesh"
tags: [windows, openfoam, cfmesh, probe]
review-by: "2026-11-07"
summary: >-
  Two bounded probe attempts exited 127 before reaching cartesianMesh; the installed runtime environment remains unresolved.
---

# W-5 cartesianMesh availability probe

Redaction: user and machine identifiers are omitted; no solver or mesh generator was run. UTC is from the
Windows host clock. `cartesianMesh-help.stdout.txt` and `cartesianMesh-help.stderr.txt` are the raw streams from
the final recorded probe (cycle 2).

## Cycle 1 of 2

The first capture-script version failed PowerShell parsing before WSL launch; no probe was run. After that script
was corrected, the WSL attempt used a malformed `source` invocation and exited **127**. Its observed console output
was copied to [cycle-1-console.txt](cycle-1-console.txt). That invocation did not establish whether the installed
binary was available.

## Cycle 2 of 2

Started UTC: `2026-10-08T17:06:48.936Z`  
Ended UTC: `2026-10-08T17:06:50.166Z`  
Host/runtime identity: [host-runtime.txt](host-runtime.txt)

Metadata argv: `wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc 'printf "resolved_binary="; command -v cartesianMesh || true; printf "WM_PROJECT_VERSION="; printf "%s\n" "$WM_PROJECT_VERSION"; dpkg-query -W -f="${Version}\n" openfoam2512'`  
Metadata exit: `0`  
Metadata captured output: [resolved-tool.txt](resolved-tool.txt) — no resolved path or version was emitted.

Probe argv: `wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc 'cartesianMesh -help'`  
Linux command argv: `cartesianMesh -help`  
Exit: **127**  
Stdout: [cartesianMesh-help.stdout.txt](cartesianMesh-help.stdout.txt)  
Stderr: [cartesianMesh-help.stderr.txt](cartesianMesh-help.stderr.txt)

The command reported `cartesianMesh: command not found`. No OpenFOAM environment activation retry ran; the repair cap
was reached. The exact activation command for a fresh, authorized track is recorded in [receipt.md](receipt.md).
