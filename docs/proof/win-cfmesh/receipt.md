---
id: proof-win-cfmesh
title: "W-5 cfMesh availability probe"
type: proof-pack
status: blocked
owner: "@win-w5-cfmesh"
tags: [windows, openfoam, cfmesh, tip-mesh]
links:
  - { to: proof-win-routes, rel: relates-to }
  - { to: proof-spike-03-tip-bl-route, rel: relates-to }
  - { to: coordination-pc-kickoff, rel: implements }
review-by: "2026-11-07"
summary: >-
  W-5 stopped at its first availability gate. The noninteractive WSL probe could not resolve cartesianMesh;
  this does not establish that the installed OpenFOAM package lacks cfMesh.
---

# W-5 — cfMesh availability probe

**Goal:** decide whether the installed Windows OpenFOAM v2512 route exposes `cartesianMesh`, then run only the
authorized Ruling 98 coupon if the help probe succeeds. **Done when:** availability and its limit are recorded and
the coordinator has the exact Mac decision request. **Not in scope:** package installation, alternate mesh route,
solver, tuning, or any coupon run before a successful help probe. **Tier:** T1 evidence. **Fan-out:** 1.

## Probe result

Two capture/probe repair cycles were used; the cap fired. Cycle 1's observed console output is preserved in
[cycle-1-console.txt](cycle-1-console.txt). The measured command in cycle 2 was:

```text
wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc "cartesianMesh -help"
```

The intended Linux command and argv were `cartesianMesh -help`. It exited **127**. Captured stdout is empty;
stderr is preserved in [cartesianMesh-help.stderr.txt](cartesianMesh-help.stderr.txt), which reports
`cartesianMesh: command not found`. The measured UTC interval and exact WSL invocation are in [probe.md](probe.md).
No OpenFOAM environment activation retry ran after cycle 2.

The separate metadata invocation exited 0, but its captured output did not contain a resolved binary or an
OpenFOAM version. See [resolved-tool.txt](resolved-tool.txt). [host-runtime.txt](host-runtime.txt) records the
redacted Windows host identity and the previously verified WSL2 distro identity. The earlier W-3 receipt records
the installed OpenFOAM package version as **2512.0-2**; this W-5 probe did not independently verify the version
because the command did not resolve the OpenFOAM environment.

**Conclusion — unavailable in the probed runtime environment; installation presence is unresolved.** The exit 127
does not prove that the installed package lacks `cartesianMesh`, because the noninteractive WSL shell did not expose
the OpenFOAM executable on `PATH`. Per W-5's stop condition, no environment retry, install, alternate cfMesh build,
mesh generator, solver, or coupon run was attempted after this failure. No case YAML was created or changed.

## Coordinator decision request for Mac

> May W-5 make one availability-only retry after explicitly initializing the already-installed OpenFOAM v2512
> environment in the existing `cfdw-openfoam2512` WSL distro, to run and capture `cartesianMesh -help`? The current
> noninteractive probe exited 127 because `cartesianMesh` was not on that shell's `PATH`; the installed package is
> recorded as 2512.0-2 by W-3, so cfMesh absence is not established. No package install, alternate build, mesh
> generator, solver, or coupon run is requested. If help succeeds, W-5 will return for authorization to proceed
> under Ruling 102 and the existing W2c preregistration, with L3 priority and the stated compute cap.

Proposed fresh-track probe command, for the Mac/operator to authorize:

```text
wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc 'source /usr/lib/openfoam/openfoam2512/etc/bashrc && printf "WM_PROJECT_VERSION=%s\n" "$WM_PROJECT_VERSION" && dpkg-query -W openfoam2512 && command -v cartesianMesh && cartesianMesh -help'
```

## Redaction and residuals

User and machine identifiers are redacted. Raw stdout and stderr are kept in separate files. No mesh output exists.
The W-2c S4/S6 criteria remain unmeasured on Windows; no GO/NO-GO verdict can be made. The coordinator owns the
cross-repo docs-index and audit close.
