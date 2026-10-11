---
id: proof-win-gpu-b1-precheck1
title: "B1 launcher precheck 1: bare setsid"
type: proof-pack
status: complete
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, wsl, ruling-201]
links:
  - { to: proof-win-gpu-b1-r199, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  Foreground teardown passed; bare setsid reproduced the early-launcher-exit blocker before attempt 3.
---

# Precheck 1: bare `setsid` result

Source commit: `7f83acd5`. This cap-free Ruling 201 precheck did not start B1 attempt 3.

The foreground case completed its identity line with PID, PGID, and SID all `44136`, confirmed its tagged `sleep 30`
by cmdline, stopped the Windows launcher, and found no tagged residual. The bare-`setsid` case produced no stdout or
stderr and no tagged sleep within the five-second bound, and its Windows launcher had already exited. The wrapper
failed closed before writing `launcher-precheck.json`.

This reproduces attempt 2's launcher behavior and rejects bare `setsid` as a candidate. Precheck 2 tests the two actual
Ruling 201 candidates: foreground and `setsid --wait`.
