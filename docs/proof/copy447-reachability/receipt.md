---
id: proof-copy447-reachability
title: COPY-447 public ProjectStore reachability measurement
type: proof-pack
status: blocked
owner: "@windows-worker"
phase: verification
tags: [windows, persistence, copy-447, proof]
links:
  - { to: mockup-w2-save-picker, rel: documents }
review-by: "2026-11-09"
summary: >-
  Partial public-API reachability capture for two Windows save requests. Both were
  refused with unchanged observed inventories; crash-left artifacts remain NOT ASSESSED.
---

# COPY-447 reachability measurement — blocked partial capture

Goal: observe what the current public `ProjectStore.SaveAsync` does for a new project and an overwrite on Windows, without bypassing production admission.
Done when: both isolated cases have source-bound before/after inventories, `SaveResult`, local telemetry and measured durations; the crash-left conclusion matches the observed public path.
Tier T1; fan-out cap 1; repair cap 2.

## Scope and outcome

The one attempted capture built the proof-local harness, passed its inventory self-test, and retained stdout/stderr derivatives for both cases. It did not produce the outer receipt. The harness was uncommitted when it ran, and the capture launcher did not retain its own exit/error. No case was rerun during packaging.

Both cases used the public `ProjectStore.SaveAsync` API on Windows 10.0.26300, .NET runtime 10.0.7, and NTFS. Each returned `DOC-UNSUPPORTED-PERSISTENCE` with `PublishedSha256=null`, `PublicationKnown=false`, and `DurabilityConfirmed=false`; the observed before/after inventory and target were unchanged.

| Case | Before / after | Target comparison | Save wall / local telemetry | Result |
|---|---|---|---|---|
| Create-only, absent target | Empty inventory before and after | `project.cfdw` remained absent | 14.4931 ms / 4.7376 ms | Returned `DOC-UNSUPPORTED-PERSISTENCE`; observed inventory and target unchanged |
| Overwrite, fixed 30-byte target | `project.cfdw`, 30 bytes, SHA-256 `9978676bb461eb3fceafcca436d98bfede01767d0059e1b7dfb10d74fddf9dcc`, unchanged before and after | Exact seeded target hash matched the expected hash; target remained unchanged | 8.2924 ms / 1.0904 ms | Returned `DOC-UNSUPPORTED-PERSISTENCE`; observed inventory and target unchanged |

The harness records the replacement image SHA-256 as `ed0a69cb2ad78b387cd28b123463ab793c76fd23bd91d150a143f65ab70d3312`. Each local telemetry event reports `document.save`, outcome `DOC-UNSUPPORTED-PERSISTENCE`, input 26 bytes, output 0, and publication/durability false.

**Crash-left artifacts are NOT ASSESSED.** These are normal public API calls whose observed before/after inventories and targets were unchanged. They do not show what process termination leaves at any save stage and do not support COPY-447 wording about leftover files.

## Capture limits and provenance

The eight original files under `capture/` are preserved byte-for-byte. They are PowerShell `Out-File` derivatives of the runner-returned strings, not raw child streams. Each of the four `.stderr.txt` files is exactly two bytes `0D0A`; that is the empty-pipeline newline written by `Out-File`, so the original child stderr bytes are **not recorded**. The original capture outer exit/error, per-child numeric exits, runner lifecycle fields, raw stdout/stderr hashes, and runtime source/binary bindings are **not recorded**. The original PII child output is also **not recorded**.

`package-pii.ps1` later ran a separate packaging-only, hostname-aware `tools/check-proof-pii.py` command through the committed Windows runner. It checks the retained proof tree's privacy only; it is not evidence for the ProjectStore run. Its separate stdout/stderr, numeric result, duration, PHN and cleanup metadata are in `capture/pii-*`. The metadata command field, `py -3 tools/check-proof-pii.py`, is an equivalent command description; the script invoked the supplied Python executable directly, and the literal executed argv was not retained. `capture-manifest.json` binds the packaged files for the eight original derivatives and the three packaging PII files; committed-blob validation is required after commit. The local `.gitattributes` marks these files byte-exact.

The source HEAD observed before the measurement was `b2af15967540dfda46b970892bb28be394dfae8d`. The probe files were uncommitted then; the measurement does not bind their executed bytes or the output binary. The captured HEAD is provenance only. COPY-447 remains unresolved pending a separately authorized reachable Windows store path or a ruling to measure the supported macOS store instead.

## Proof-local source

`Program.cs` invokes only public `ProjectStore.SaveAsync`, then records the public `SaveResult`, public local telemetry, inventory and target hashes. It does not access hooks, internals, `WindowsNative`, or product/test dispatch. `capture.ps1` documents the attempted one-shot build and run; its incomplete outer capture is retained as a partial result.
