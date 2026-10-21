---
id: proof-wsl-preflight
title: "WSL dependency preflight: Ruling 217's question answered by one bash script in the existing distro"
type: proof-pack
summary: "One bash script run inside the existing Ubuntu-24.04 distro answers whether the CUDA 13.2, OpenFOAM 2512 and build-tool set resolves for arm64 from the reviewed, signed repositories: it does, 213 packages, 3.24 GiB, nothing installed."
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [wsl, arm64, cuda, openfoam, preflight, bash]
links:
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: supersedes }
---

# WSL dependency preflight

**Question (Ruling 217).** Do the CUDA 13.2, OpenFOAM 2512 and build-tool packages resolve for this machine's
architecture from the reviewed repositories, signed by the reviewed keys, and how much is the download?

**Method.** `tools/wsl-preflight.sh`, run by hand inside the existing `Ubuntu-24.04` distro. apt runs with temporary
sources, lists, cache and state directories, so the distro's own apt configuration is never read or written; the host's
dpkg status is read so the simulation resolves against what is really installed. Nothing is installed. The three
keyrings are checked against the reviewed hashes before use; every repository `InRelease` is re-verified with `gpgv`
after `apt-get update`. The receipt is one JSON file written by `tools/wsl-preflight-receipt.py`.

```
wsl -d Ubuntu-24.04 --cd <repo> -- bash tools/wsl-preflight.sh --keyrings /mnt/d/reviewed --out docs/proof/wsl-preflight/<date>-<arch>.json [--download]
```

## Result, 2026-10-21, arm64 (the laptop)

| Field | Value |
|---|---|
| Result | PASS in 6 s (`2026-10-21-arm64.json`) |
| Architecture | arm64 / aarch64; GPU NVIDIA RTX Spark N1X, driver 617.42 |
| Indices verified | 5 of 5 (Ubuntu ports noble, noble-updates, noble-security; OpenFOAM noble; NVIDIA ubuntu2404/sbsa), each with a `VALIDSIG` from its reviewed keyring |
| Roots | 15, all resolved: `cuda-toolkit-13-2=13.2.2-1`, the five `openfoam2512*=2512.0-2`, gcc-13, g++-13, gfortran-13, openmpi-bin, libopenmpi-dev, libblas-dev, liblapack-dev, pkg-config, `make:arm64` |
| Resolution | 213 packages, no removals; 217 files, 3.24 GiB |
| Download | PASS in 103 s (`2026-10-21-arm64-download.json`): all 213 resolved archives downloaded into the temporary directory, 3.24 GiB, each hashed in the receipt, then deleted with the directory |

So the arm64 question has a plain answer: the whole set resolves from the signed repositories. The next step is not another
preflight; it is the build itself, on this distro or a throwaway one, with the same script as its precheck.

## What this replaces

The F1 packet under `docs/proof/win-gpu-f1-preflight/` (a throwaway distro per attempt, a contract pair, a leader epoch,
an exclusivity scan, cleanup with decision semantics; four attempts, no Linux stage ever run). Its reviewed inputs and
their hashes carry over unchanged; everything else was built for a second machine and a hostile peer that this project
does not have. The simplification decision is recorded in `docs/notes/rulings.md` and the retired packet stays in git
history.
