# CFD WorkBench Proposal

A proposal to evolve the **cfd-bench** specification corpus into a real, user-friendly hydrofoil design workbench for **macOS and Windows**.

This directory is **not a git repository**. It is a reviewable design packet.

| File | Role |
|---|---|
| [sequence.html](sequence.html) | **Product sequence (follow-on).** Shell → CAD shape (anchors/levers + NL start) → 2D/3D estimate → owned CFD install → viz, with Claude Q&A on results. |
| [sequence.md](sequence.md) | Canonical text of the sequence proposal. |
| [proposal.html](proposal.html) | Post-benchmark proposal: what to keep from cfd-bench, C# vs Rust, recovery of the Windows trees. |
| [proposal.md](proposal.md) | Canonical text of the post-benchmark proposal. |
| [inventory.md](inventory.md) | Working list of **spikes** (weekend-scale, pass/fail) and **deeper research** (not a weekend). |

## What this is grounded in

- Knowledge, proposals, commitments and gaps in `~/projects/cfd-bench`
- The only surviving GHCP implementation record: `~/projects/cfd-bench/docs/benchmark/comparisons/grade-20260908T204118Z.*` (the C# tree itself was never pushed)
- Copilot arm on GitHub: seed only. Graded Windows implementation never pushed (`C:\Projects\CFD-Bench-GHCP`, SHA `db42694`).
- Claude Code arm on GitHub: seed plus a coordination runbook. Operator 2026-09-19: a **functional WPF app** existed on the Windows working copy and was not pushed.

## First action if you accept this

Recover **both** Windows trees (`C:\Projects\CFD-Bench-GHCP` and `C:\Projects\CFD-Bench-ClaudeCode`), including worktrees, before they rot. That is SPIKE-00 in the inventory — it now has a PowerShell bundle recipe. Do not `git push` those dirty `main`s; bundle first. Everything else can wait a day; that cannot.
