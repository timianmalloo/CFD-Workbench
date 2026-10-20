---
id: review-f1-rescope-r237-node-a
title: "F1 re-scope node A: Adversary review of the Ruling 243 bash.exe exclusivity rule"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, f1, review, adversary, security, exclusivity]
links:
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: review-f1-rescope-r237-node-r, rel: relates-to }
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-attempt3-plan, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  The Security & Identity Architect's Adversary-mode review of the DR-5 (a) change (Ruling 243 (1)): bash.exe counted
  as a WSL actor only at the System32 path. ACCEPT WITH CONDITIONS; both conditions applied in the same node (the
  canonical path compare with planted dotted and prefixed paths; the fixture's path restored) and the boundary wording
  adopted. Verbatim report, then the dispositions.
---

# F1 re-scope node A: Adversary review of the Ruling 243 bash.exe exclusivity rule

Reviewer: the Security & Identity Architect persona (`.claude/agents/security-identity-architect.md`), Adversary
mode, read-only in the node worktree at `0d30af2b` (HEAD moved to `dcca9974` during the review). Question put: can a
process that can touch WSL evade the scan under the new rule; does the fail-closed branch hold on every row shape;
is the trust boundary stated truthfully. The report follows verbatim.

## 1. Report (verbatim)

PERSONA: security-identity-architect   MODE: Adversary   TIER: T1
VERDICT: PASS-WITH-CONDITIONS (parent vocabulary: ACCEPT WITH CONDITIONS)

Note: HEAD moved during review (dcca9974 landed on top of 0d30af2b); findings cover both.

FINDINGS:

1. [Major / condition] (Verified) The System32 path compare is not canonical, so the path clause can be evaded by a non-normalized spelling. Live `Win32_Process` on this laptop reports `ExecutablePath = C:\Program Files\Git\bin\..\usr\bin\bash.exe` — WMI returns the path as launched, `..` included. A legacy launcher started as `C:\Windows\System32\..\System32\bash.exe` or `System32\.\bash.exe` fails `-ieq` at `Control.ps1:218` and is classed *not* an actor. Probe: raw `-ieq` False for both; `[IO.Path]::GetFullPath()` True for both; `\\?\`-prefixed still False. Not a blocker: `C:\Windows\System32\bash.exe` (and SysWOW64/SysArm32) does not exist on the attempt-3 host (Verified), so the clause is inert here. Fix: strip `\\?\` (as `Get-BoundTargetKey` already does at line 127) and compare `[IO.Path]::GetFullPath($bashPath)`; a throw from GetFullPath stays fail-closed; plant a `..` fixture that refuses.

2. [Major / condition] (Verified) Commit dcca9974 corrupted the planted fixture: `tests/f1_attempt2_controls.ps1:75` now reads `'"C:Program FilesGitSrbinbash.exe" ./probe.sh wsl -d ...'` (sed consumed the backslashes; `\u` upper-cased). The test still refuses via the command-line clause, so it is green on a string that is not a Git bash command line. Fix: restore `"C:\Program Files\Git\usr\bin\bash.exe" ./probe.sh wsl -d cfdw-petsc-cuda132-f1` with an editor, not sed. Also flag to parent: `docs/proof/win-gpu-f1-preflight/rescope/verification-v.json` shows deleted (` D`) in the worktree.

3. [Minor / advisory] (Verified) The trust boundary is understated in the comment (`Control.ps1:207-208`) and the register *Derive* ("keys on the executable's path, never on the image name alone"). The scan's identity is WMI-reported text under the launcher's own principal; every evasion in the question (copied/renamed launcher, symlink under Developer Mode, spoofed `$env:SystemRoot`, `wsl.exe` renamed) sits inside that principal, and the command-line clause matches text, not invocation. State plainly: this is a hygiene control over the operator's own process population, not a boundary against a hostile peer; and that on the laptop the System32 launcher is absent, so the rule's live effect is (a) Git bash excluded, (b) null/unknown path refused. The ruling's "only under System32" is a provenance claim, not a capability claim — the register should not imply otherwise.

4. [Nit] (Verified) Fail-closed holds on every row shape. `PSObject.Properties['ExecutablePath']` returns `$null` without throwing under StrictMode (the `$noPath` fixture passes with `Set-StrictMode -Version Latest` at test line 3); CIM rows carry the property (live); null/whitespace → actor (lines 215-216). Replay rows (`Invoke-Preflight.ps1:400-404`) lack the property and would be actors, but the sample only ever holds keepalive `wsl.exe` rows (`:258-260`, count-gated `:397`), so the branch is unreachable there. Protected/elevated → null → refused (Inferred, correct direction). `$env:SystemRoot` unset → `Join-Path` throws → fail closed (Inferred).

5. [Nit / advisory] (Verified) Pre-existing residual: `wslservice.exe` is live with a null `ExecutablePath`; `wslhost/wslrelay/wslg.exe` are outside the name regex by design (line 204 comment). Out of this question's scope.

6. Attempt-number generalization (`-ge 2` at lines 130/146, `-in @(2,3)` at line 100): noted, not reviewed in depth; the cap at 3 holds via `Get-InvocationRunRoot`.

Suite: `pwsh -NoProfile -File tests\f1_attempt2_controls.ps1` → `attempt2 controls PASS`, exit 0.

CLEARS-THE-VETO: yes, on conditions 1 and 2 — boundaries named? after finding 3's wording; input validated trusted-side? yes once canonicalized; no secrets in source/logs? yes; no exploitable CVE on a shipped path? n/a (no dependency change); least privilege? yes (read-only CIM under the launcher's principal).

RESIDUAL RISK: a same-principal process that renames or copies a WSL launcher, or invokes WSL via a script file whose command line never names `wsl`, is caught only while its `wsl.exe` child is alive at a sample instant. Accept with rationale in the register.

ACCEPT WITH CONDITIONS

## 2. Dispositions (the author, same node)

| Finding | Disposition | Where |
|---|---|---|
| 1 (condition) | Applied: the `\\?\` prefix is stripped and the path compared through `[IO.Path]::GetFullPath`; a throw stays fail closed. Planted: `System32\..\System32\.\bash.exe` and the prefixed form refuse; a Git bash reported through `bin\..\usr\bin` passes. Red observed against the pre-condition `Control.ps1` (the dotted System32 path passed), green after on both runtimes. | `packet/Control.ps1` `Assert-AttemptExclusivity`; `tests/f1_attempt2_controls.ps1` |
| 2 (condition) | Applied before the report arrived: the fixture's path restored verbatim by a scripted edit (commit `43102086`); the `sed` pass that ate the backslashes is the class AGENT-HEREDOC's sibling (a shell quoting pass over a path) and the edit scripts now carry raw strings. The receipt shown as deleted was the runner's regeneration in progress; it is committed on the node's final commit. | commit `43102086`; `rescope/verification-v.json` |
| 3 (advisory) | Adopted: the code comment and the register's *Derive* say that the scan reads WMI text under the launcher's own principal and is a hygiene control over the operator's own process population, not a boundary against a hostile peer; the live effect on the laptop (Git's bash excluded, a null or unknown path refused, the System32 launcher absent) is stated. | `packet/Control.ps1`; `docs/lessons/defect-classes.md` NAME-MATCH-COUNTS-A-FOREIGN-TOOL-AS-AN-ACTOR |
| 4, 5 (nits) | Recorded; no change. The replay-row branch is unreachable with a `bash.exe` row by the sample's count gate; `wslservice.exe` with a null path stays outside the name regex by design. | this document |
| 6 | The attempt-number generalization was not in the review's question; its proof is the red-first suites on both runtimes (plan ledger, node A, Ruling 243). | `docs/plans/f1-rescope-r237.md` §9 |
| Residual risk | Accepted with the reviewer's rationale, recorded in the register entry. | `docs/lessons/defect-classes.md` |
