---
id: proof-win-cfmesh-r153
title: "W-5 Ruling 153 cartesianMesh availability probe"
type: doc
status: observed
owner: "@win-w5-cfmesh"
tags: [windows, openfoam, cfmesh, probe, ruling-153]
links:
  - { to: proof-win-cfmesh, rel: refines }
review-by: "2026-11-08"
summary: >-
  The activated OpenFOAM v2512 environment resolved cartesianMesh; its help exited 0 and confirmed the installed build.
---

# W-5 Ruling 153 availability probe

## Result

**Present.** The activated environment resolved `cartesianMesh`; `cartesianMesh -help` exited 0 and printed its usage
banner. The banner identifies **OpenFOAM-2512 (2512)** and **Build: `_bd2b6720-20260127`**. The installed package is
`openfoam2512 2512.0-2 install ok installed`. This completes only Ruling 102 step 1's availability check. Stop here
and request a separate release for a frozen S4/S6 coupon under the Ruling 98 W2c criteria, with a new preregistration
and L3 priority. This ruling does not authorize the coupon.

## Invocation and measured evidence

The committed LF script is [probe-r153.sh](../probe-r153.sh). The Windows host invoked it as:

```text
wsl.exe --distribution cfdw-openfoam2512 --exec /usr/bin/nice -n 10 /bin/bash /mnt/c/Projects/CFD-Workbench-win-w5-cfmesh-probe/docs/proof/win-cfmesh/probe-r153.sh
```

- Started UTC: `2026-10-08T23:11:52Z`
- Ended UTC: `2026-10-08T23:11:53Z`
- Script SHA256: `0e1bd050daf2f950457b653e63cd41601886d0e08d55f0719250b23b11aaa757`
- WSL command exit: `0`
- Outcome: `present`

The script sourced `/usr/lib/openfoam/openfoam2512/etc/bashrc` with errexit disabled, then enabled `set -u`. The
source exited `0`; `WM_PROJECT_VERSION=v2512`; `WM_PROJECT_DIR=/usr/lib/openfoam/openfoam2512`; and
`FOAM_APPBIN=/usr/lib/openfoam/openfoam2512/platforms/linux64GccDPInt32Opt/bin`. The exact package query used the
single-quoted format `dpkg-query -W -f='${Package} ${Version} ${Status}\n' openfoam2512` and exited `0`.

`command -v cartesianMesh` exited `0` and resolved to
`/usr/lib/openfoam/openfoam2512/platforms/linux64GccDPInt32Opt/bin/cartesianMesh`. `ls -l` and `sha256sum` both
exited `0`; the binary SHA256 is
`0c54ed3163357e78775f28ba1e38860dac44f0052adadd58f4e3f0795ca08861`. The help command exited `0`.

## Captures

- [probe-console.txt](probe-console.txt) — timestamps, script hash, stage exit codes, environment and load averages.
- [cartesianMesh-help.stdout.txt](cartesianMesh-help.stdout.txt) and
  [cartesianMesh-help.stderr.txt](cartesianMesh-help.stderr.txt) — separate raw help streams.
- [dpkg-query.txt](dpkg-query.txt) and [dpkg-query.stderr.txt](dpkg-query.stderr.txt) — package query result and stderr.
- [environment.txt](environment.txt) — OpenFOAM environment variables.
- [resolved-path.txt](resolved-path.txt), [resolved-ls.txt](resolved-ls.txt), and
  [resolved-sha256.txt](resolved-sha256.txt) — resolved executable evidence.
- [loadavg-before.txt](loadavg-before.txt) and [loadavg-after.txt](loadavg-after.txt) — `/proc/loadavg` captures.
- [script-sha256.txt](script-sha256.txt) — script hash as read by the WSL run.
- [command-v.stderr.txt](command-v.stderr.txt) and [probe-launch.stderr.txt](probe-launch.stderr.txt) — separate error streams.

No install, build, case YAML, mesh, solver, or coupon command ran. Windows home paths use `%USERPROFILE%` when a
redaction placeholder is needed; this committed evidence contains no account name or SID.
