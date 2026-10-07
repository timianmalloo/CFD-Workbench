---
id: review-pr-2
title: "PR #2 (Windows PC) - W-0 setup evidence, Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-0]
links:
  - { to: coordination-pc-kickoff, rel: depends-on }
  - { to: coordination-two-machine, rel: depends-on }
  - { to: review-pr-1, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  APPROVE WITH CONDITIONS. The W-0 receipt meets the done-when line by line with observed commands; four conditions bind
  later receipts (Reported vs Verified, one tested SHA, a Done-when line, dotnet in a fresh shell).
---

# PR #2 — Fable owner review

PR #2 "Record Windows W-0 setup evidence" (origin/win/setup, head ddc6f44d, tested SHA 104cf851, base d0fc7fde) was
reviewed by the Fable owner on the Mac on 2026-10-07 under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**. The receipt
`docs/proof/win-setup/receipt.md` meets W-0's done-when line by line with observed commands and outputs: Git with
`core.autocrlf=false`, Python 3.13.14, .NET SDK 10.0.203 with roll-forward disabled, `gh auth status` ok,
`coord-core.py doctor` with both merge drivers effective, and a full host survey (Windows 11 Pro 26300.9457, x64,
i9-12900H 14C/20T, 34 GB, 1.86 TB free, hypervisor present, WSL 2.7.14 with no distribution). Firmware virtualization and
the WSL repair are honestly Not recorded. No secrets; home paths are allowed. The tested SHA is an ancestor of the head
whose one later commit appends xmsg lines only; `tools/check-docs.py` passed on head ddc6f44d in a scratch clone on the
Mac (exit 0). The PR is docs-only, so RING-SKIPPED under Ruling 89 applies.

Conditions bind later receipts and PRs:

1. Coordinator-reported results are labelled Reported or Inferred, never Verified.
2. One tested SHA per receipt, from `git rev-parse HEAD` at the final gate.
3. An explicit Done-when line in the PR body (PR #1 condition 6).
4. W-1 records `dotnet --version` in a fresh shell without the PATH refresh.

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/2#issuecomment-6042616221
