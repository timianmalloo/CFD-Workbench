---
id: investigation-review-window-attach
title: Review window attachment and stalled human waits
type: investigation
status: in-review
owner: "@cfd-focus-20260924"
tags: [native-ui, coordination, investigation]
links:
  - {to: coordination-contract-c-native, rel: depends-on}
  - {to: review-ui-application-native, rel: relates-to}
  - {to: plan-application-build, rel: relates-to}
review-by: 2026-10-25
summary: Reproduced global CUA attachment failure and recovered the unchanged process by resetting the supported CUA session. Two consecutive fresh builds passed the reset/readiness protocol without human foregrounding; internal CUA state remains inferred.
---

# Review window attachment

Goal: remove repeated operator foregrounding from native review. Done when two
consecutive fresh builds attach without human action and failure is detected before
human escalation. Not in scope: M1 scope changes, privacy bypasses, alternate UI
automation. T2; active cap three (root investigator, Owner decision author, C theme
author). The user explicitly authorizes fixing after investigation; the skill's
default report-only stop does not apply.

## Surface list and execution graph

Launch process and identity receipt → app startup/window → supported CUA AX and
screenshot attachment → readiness receipt and gate → coordinator scheduling and
human escalation → independent proof, defect register and audit. No domain model,
geometry, persistence or product scope change is authorized by this repair.

Root investigates and repairs launch readiness. Owner independently prepares the
M1 decision. C author independently completes R38 theme repair; native windows are
serialized with root to avoid focus contention. This extends the existing overall
execution graph, not a second implementation plan. Human waits block only the
dependent node. Batch all necessary human decisions into one request, continue
dependency-ready nodes, and record blocked nodes explicitly.

## Symptom, timeline and map

[Verified] The independent native review records repeated exact-path and exact
bundle-id `Computer Use server error -10005: cgWindowNotFound` despite a live PID
and `NATIVE-STARTUP window-opened`. User foregrounding preceded successful attach.
The Dark R37 process started 24 September at 09:13:34 local; successful review
resumed that evening. The user reports about ten hours of graph stall. That
interval is elapsed wait, not measured compute usage.

[Verified] The later R37 High Contrast copy attached without user action despite
the same unique-bundle-id and `/private/tmp` approach. A temporary identity alone
therefore does not explain every result. Multiple earlier review windows remained
live until cleanup. All six identified obsolete owned windows and the completed
Dark/High Contrast windows were closed through CUA. One uncertain window was
closed by the user. The existing launch guard checks process accumulation, not
native attachment.

Launch → process liveness → managed Opened → CUA discovery → AX/screenshot → review.
The old flow treated earlier nodes as evidence for later ones, then substituted
an unbounded human wait for an absent machine readiness contract.

## Environment and hypotheses

Method: change analysis plus event timeline, testing identity, authorization,
activation, display/Space and accumulated windows as competing causes.

[Verified, 2026-09-25 UTC] macOS 27.0 build 26A428, Apple M4 Max. Display inventory
from `system_profiler SPDisplaysDataType -json`: one online built-in Color LCD,
display ID 1, main, non-mirrored, 3024×1964 pixels / 1512×982 points at 120 Hz.
Supported CUA app inventory exposes no macOS window list or Space identifiers.

[Verified] Through CUA, System Settings → Privacy & Security → Screen & System
Audio Recording shows **Codex Computer Use on**. Device Control and Data Access
also shows **Codex Computer Use on**. Its visible description includes controlling
apps. AX reads, clicks and screenshot capture of System Settings succeed. No
permission was changed. No review bundle is listed as the controlling/capturing
app. This rules out a blanket current denial to the CUA host; it does not prove
the historical permission state at each failure.

- assume: historical TCC state matched the current observed state. Confirm with
  contemporaneous permission evidence if available; if false, historical failures
  may have another cause. Current working host permissions need no speculative reset.
- assume: an unavailable Space identifier cannot distinguish off-Space placement
  from other attachment failure. Confirm only through supported CUA observation;
  if unavailable, leave the candidate unresolved rather than invoke private APIs.

## Reproduction and cause ledger

[Verified] Diagnostic copy (not a fresh build) launched at 2026-09-24 20:12:25
local: `/private/tmp/cfd-r37-dark-ui-ili1v_m4/CFD Workbench.app`, identifier
`com.cfdworkbench.desktop.focusrepro20260924`, PID 47472. Receipt SHA-256
`863dcf7a6ae52b561d5a1e3f2072e397702fca45e33c39a8b4609dae30cd75d2`.
Direct executable launch had no explicit native activation call. Startup stderr
reported app initialization, classic desktop lifetime, main window assignment and
`window-opened`; its metric explicitly says batch cycle, not presentation.

The first exact-path supported attachment succeeded in 129 ms without user action.
Observed window: `CFD Workbench — Offline Foil · REVIEW designer / example / dark /
reduced motion`, standard window with Raise, FoilViewport and DocumentTabs. Screenshot
capture succeeded. Clicking its minimize control and later pressing Command-H did
not produce the historical attachment error: subsequent AX and screenshot operations
succeeded. The tool may restore visibility; these operations do not establish
actual off-Space placement. The app was closed through its close control and
Discard for the owned unsaved embedded Example; CUA inventory then showed none.

[Verified] Power logs show the display on during the original 09:14 CUA attempts,
then clamshell sleep at 19:17:44 and display wake at 19:38:56. That later sleep does
not explain the earlier failure. Space membership at the original failure was not
recorded. No unsupported window enumeration or private TCC database was used.

[Verified] A second genuinely fresh build reproduced the exact failure at
2026-09-25T03:29:28.799Z. PID 51522, start `Thu Sep 24 20:29:08 2026`, remained live
with `window-opened`. Path `/private/tmp/cfd-focus-build2-light-ui-4fwm4pux/CFD
Workbench.app`, bundle `com.cfdworkbench.desktop.focusbuild220260924`. All three
bounded path/id/path calls returned `Error: Computer Use server error -10005:
cgWindowNotFound` in 102 ms. The previously usable System Settings handle returned
the same error. The supported inventory listed both apps running, but exposed no
window list/Space identifier.

[Verified] `mcp__cua_repl.js_reset`, followed by the documented exact-path getApp
entry point, immediately recovered AX and screenshot capture for the **same PID,
start time, binary, bundle and window** without a user action or permission change.
The subsequent gate observed the original PID and accepted revision
`23115787-4bcf-4b56-8482-b31d93453fb7`. This is a controlled recovery at the CUA-session
boundary, not a relaunch accidentally mistaken for success.

### Cause ledger and disconfirmation

| Candidate | Observed test | Disposition |
|---|---|---|
| Temporary path / unique bundle | Both failure and recovery used identical bytes, PID, path and id; other unique copies also attached. | Not sufficient to cause failure; no identity/path change justified. |
| New target needs capture/control TCC | CUA host switches were on; System Settings also failed; session reset alone restored the unchanged target. | No evidence of missing target permission. No permission request or reset needed. Historical TCC state remains the explicit assumption above. |
| Launch never activates | Launcher invokes executable directly; failure reproduced, but other direct launches succeeded. Reset restored the background-looking Light window before an explicit Raise. | Launch method alone insufficient; supported Raise remains a readiness step. |
| Different display / Space | One main built-in display observed. CUA has no macOS Space/window enumeration. | Another display unsupported by current inventory; Space membership unverified. The `assume:` limit remains. |
| Unusable persistent CUA session | Multiple apps failed in the existing session; supported session reset restored the unchanged app; next fresh build with pre-attach reset passed. | Recovery verified. Stale internal session/binding state is **Inferred**, not an inspected internal implementation. |
| Graph-wide wait | Prior repeated foreground requests awaited the user while launch-only proof remained green. New native gate returns a blocked node with exact error; Owner/author/coordinator work continued. | Systemic readiness/wait defect verified and controlled. |

The evidence establishes a sufficient supported recovery and excludes a required
bundle or permission mutation in this reproduction. It does **not** establish
necessary-and-sufficient causation inside the external CUA service. Exact internal
state and whether reset would cure every future failure remain **Flagged**. This
limit must survive summaries; no claim that macOS or CUA internals were fixed.

## Generalization and repair proof

[Verified] `node tools/check-review-attach.mjs` on the old launch-only receipt
returns exit 1, `NATIVE_REVIEW_BLOCKED: exact launch-bound AX/screenshot readiness
is absent`. `node tools/test-review-attach.mjs` passes missing-window, retry,
supported Raise, wrong-window, incomplete-surface, missing-capture, human-assisted,
timeout and identity-binding negatives. The historical exact error is injected at
the adapter boundary; that is not a fresh reproduction of the platform trigger.

Fix scope: add a supported CUA adapter, mandatory receipt gate and node-local human
wait policy. No target bundle, OS permission or application identity is changed.
Rollback removes the helper and gate but reopens the orchestration defect; retain
the blocked state rather than resume routine foreground requests. Separate live
PID/start/executable checks must bracket attach because CUA can relaunch apps.

## Two consecutive fresh-build proof

The final protocol resets the supported CUA REPL once per review launch, then
uses the bounded helper and live-process receipt gate. No computer automation
outside CUA is used. The reset is an explicit tool action, not a fictitious API
inside the helper. Its receipt flag is an attestation backed by the transcript.

| Build | Freshness and original identity | Actual readiness |
|---|---|---|
| 2, Light | Separate `dotnet publish` PID 50908 and package PID 50990; 53 source hashes equal frozen R38. Launch PID 51522, start 20:29:08. [Build assessment](review-attach-evidence/build2-assessment.json), [launch](review-attach-evidence/build2-launch.json). | [Before reset](review-attach-evidence/build2-before-reset.json): exact failure, 102 ms. [After reset](review-attach-evidence/build2-ready.json): one attempt, **1359 ms**, 185002 screenshot bytes; unchanged live identity verified before/after. |
| 3, Dark | Another isolated publish/package, new output/cache roots, 53 unchanged inputs; launch PID 52748, start 20:33:39. [Build receipt](review-attach-evidence/build3-build.json), [launch](review-attach-evidence/build3-launch.json). | Supported reset before attach; [ready receipt](review-attach-evidence/build3-ready.json): one attempt, **1385 ms**, 186102 screenshot bytes; unchanged original identity verified before/after. |

Both attached without a user action, OS permission change or app activation request
to the user. Both owned Example windows were closed through CUA Close→Discard;
final CUA inventory contained no running Workbench app. These are attachment
durations, **not** product cold-start or visible-performance proof. Build 2/3 are
fresh compiler executions, not copies of one compiled binary. Source identity is
proved; DLL byte parity with build 1 is not claimed (embedded Git HEAD differs,
and additional metadata differences were not attributed speculatively). The
overstrict build-2 parity failure is preserved by its assessment's referenced hash.

## Phased repair and verification

| Phase | Change and failure prevented | Proof | Disposition |
|---|---|---|---|
| 1 | Separate process liveness from native readiness; refuse exited/reused PID. | Old launch-only receipt exits 1; exited launch and changed-process negatives. | Implemented. |
| 2 | Fresh supported CUA session, exact window, advertised Raise, AX and capture, three attempts/30 seconds. | Reproduced global failure and unchanged-PID recovery; two consecutive fresh builds above. | Implemented; external internals remain inferred. |
| 3 | Node-local blocking, batched human requests, independent scheduling. | Owner decision, theme repair and scope coordination proceeded while native trial failed. | Mandatory C contract and always-loaded defect register. |
| 4 | Independent boundary review. | SRE/Test found post-Raise title validation and unexercised active timeout branch; exact-row recheck and fake-timer pending-operation negative now pass. | Independently cleared for readiness scope. |

Implementation mistakes were retained as class evidence: an unverified `reviewer`
persona made an initial process exit -6 before CUA; the launcher now rejects that
argument and the live gate refuses exited processes. Screenshot `instanceof`
rejected a cross-realm Buffer despite valid JPEG bytes; the cross-realm regression
was observed failing, then passed with `ArrayBuffer.isView` plus the Uint8Array tag.
The CUA runtime rejected string code generation; subsequent function installation
uses literal code only. No blocked operation was bypassed.

## Independent review and remaining obligations

The coordinator independently read the implementation, ran the regression suite,
and validated both live-run receipts against their exact launch hashes, including
byte equality with retained raw receipts. Its two findings were addressed above.
Final independent disposition: **PASS for the bounded attachment-readiness boundary**,
with internal CUA cause explicitly Inferred/Flagged. Root did not clear its
own Test Architect veto. Privacy/trust scope is unchanged, so no new Security
permission veto is triggered. Host-internal diagnostic access and exact Space IDs
remain unavailable through the supported surface.

Part 2's single decision request was prepared independently by Owner. The user
replied **approve**: Windows and visible timing remain M1; full section authoring
is M1.1. Coordinator owns the plan/ledger/track updates. This investigation does
not clear the separate native High Contrast numeric-field veto or accept M1.

`GATE investigate · 2026-09-25 · independent coordinator acting as SRE/Test Architect
· readiness recovery/control PASS · internal platform root cause NOT VERIFIED
· no permission or product acceptance waiver.`

## Measurements

Skill start is recorded by `audit-log.py`. Monetary cost, token usage and effective
model billing identity: **not recorded**. UI tool transcript carries observed AX
and screenshots; the supported API does not expose filesystem export.
