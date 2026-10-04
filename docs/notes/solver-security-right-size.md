---
id: note-solver-security-right-size
title: "Solver security, right-sized: one trusted user on one laptop, cases the app writes"
type: decision-note
status: applied (Ruling 68)
owner: "@timianmalloo"
tags: [security, openfoam, launcher, threat-model, right-size, install, adr-0012, ruling-67]
links:
  - { to: adr-0012-openfoam-backend-macos, rel: refines }
  - { to: spec-cfd-workbench-v1, rel: relates-to }
  - { to: plan-fluids-round3, rel: relates-to }
  - { to: proof-spike-03-round2, rel: depends-on }
  - { to: rulings, rel: implements }
review-by: 2026-11-04
summary: >-
  Ruling 67 asked whether the solver security requirements are over-complicated. They are. The threat model is one
  trusted user on his own laptop, running cases the app writes from typed templates. Against that model, 4 of the 18
  current requirements stay as they are, 8 get simpler and 6 are dropped. The minimal set (M1-M8) is: the app writes
  its own controlDict with allowSystemOperations 0; it stops any launch whose master-rank banner is not Disallowing;
  argv only, built from typed values; it runs only cases whose every file it emitted (one CI emitter test, with the
  existing lint as oracle); each run gets an exclusive app-owned run directory; a clean child environment; resource
  caps with a kill fallback; the install, new or existing, is checked against the pinned hashes. Today's probe run
  (3 PASS / 5 FAIL) verifies the one control that matters. The 5 FAILs are 3 probe over-expectations, 1 probe defect
  (S-7 never reached the environment) and 1 lint gap. None is a failure of the refusal. Security review: PASS WITH
  CONDITIONS; simplifier: soft BLOCK cleared. All conditions are applied. Applied by Ruling 68 (2026-10-04): ADR-0012 D2,
  spec 1.7.1 A8.5 and A5.10, and the round-3 plan. DR-SEC-1 was ruled A (detect an existing install).
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-04, reason: "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized" }
  - { by: adr-0012-openfoam-backend-macos, on: 2026-10-04, reason: "ADR-0012 D2 right-sized and DR-SEC-1 A recorded (Ruling 68)" }
---

# Solver security, right-sized

**Result.** The solver security requirements are over-complicated for this product. One fact matters:
**an OpenFOAM dictionary can run code, and the app's own controlDict turns that off.** The probe run today verified
both halves (S-1 runs code under the app default; S-2, S-4 and S-5 show it refused under the product controlDict).
The rest of ADR-0012 D2 defends against an attacker who is not in this threat model. Most of it can go.

**Applied by Ruling 68 (2026-10-04):** the §7 edits are in ADR-0012 D2, spec 1.7.1 and the round-3 plan, and DR-SEC-1 is
ruled A. The sections below are the record of what was proposed.

## 1. The threat model (Ruling 67)

- **User.** One foil and wing designer. He is not a software engineer. He uses OpenFOAM, but not on this laptop
  yet (Ruling 67 (b)). He trusts himself and his own machine.
- **What the app does.** It writes the case dictionaries from typed templates and runs them on his laptop.
- **Realistic risks:**
  - **(a) A file from elsewhere** — a friend's design or a downloaded tutorial — opened in the app.
  - **(b) The app breaks its own run** — wrong paths, files deleted, runaway CPU, memory or disk.
  - **(c) Supply chain** — the wrong or tampered OpenFOAM or SU2 install.
- **Out of scope.** Another local user, malware already running as this user, and a hostile process that edits the
  app's files between two steps. Any of those can already do anything the user can do. A control against them only
  adds cost.

## 2. What the probes show (receipts `docs/proof/spike-03/security/20261004T155708Z/`)

I read every log. The summary line is in brackets.

| Probe | What it tested | Logged result | Reading | Confidence |
|---|---|---|---|---|
| S-1 [PASS] | `#codeStream` under the app's default controlDict | `Allowing` (log :21), `wmake libso` compiled the code (:31–36), exit 0, `dynamicCode/` present | Case dictionaries **do** run code by default. This control is what makes S-2 meaningful | Verified |
| S-2 [FAIL] | `#codeStream` serial, and a `codedFixedValue` BC on 2 ranks, under the product HOME controlDict | serial: `Disallowing`, `FOAM FATAL IO ERROR … case-supplied code may have been disabled`, exit 1, no `dynamicCode`. np2: rank 1 refused (`rank.1/stderr` :3–4), `FOAM parallel run exiting`, abort (log :81–82), exit 1, no `dynamicCode` | **Refused.** The FAIL is the probe's expectation that both ranks print the message. Rank 0's output ends at `Reading field U` with no message. The job aborted, and PMIx logged output-forwarding failures (`PMIX_ERR_IOF_FAILURE`, log :49–52, :78–79), so rank 0's error text may be lost. Which rank aborted first is not clear: prterun names "rank 0 with PID 0". The deciding fact is that no `dynamicCode/` exists, so no rank compiled the code | Verified (refusal); Inferred (why rank 0 printed nothing) |
| S-3 [FAIL] | `systemCall` function object, product controlDict | `Disallowing`; `FOAM Warning: Executing user-supplied system calls may have been disabled` (:43–44); `--> loading function object 'sc'`; run continued; exit 0 | **Refused.** The FAIL is the probe's expectation of a non-zero exit. In v2512 source, `systemCall::read` raises a FatalError when `allowSystemOperations` is 0 (`systemCall.C`:131–145). `functionObjectList` catches a non-IO error at construction, prints it as a warning, sets the object to null and does not insert it (`functionObjectList.C`:1149–1167, the `DEFAULT` → `WARN` fallthrough). So no call is dispatched. The payload (`/usr/bin/true`) leaves no marker, so the log alone cannot show this; the source does | Verified (source + log) |
| S-4 [PASS] | the case sets `InfoSwitches { allowSystemOperations 1; }`, plus a coded BC | `allowSystemOperations (unregistered)` (:28), refused (:42–43), exit 1, no `dynamicCode` | A case cannot turn the switch back on | Verified |
| S-5 [FAIL] | as S-2, but `FOAM_CONTROLDICT` only, with an empty HOME | checkMesh pre-flight `Disallowing`; serial refused, exit 1; np2 rank 1 refused, abort, exit 1; no `dynamicCode` | **Refused.** Same over-expectation and the same rank-0 output as S-2 | Verified |
| S-6 [PASS] | `libs ("libcfdwNoSuchLib.dylib")` | `dlopen` attempted (:30–31) | `libs` is not gated by the switch. This matters only for a case the app did not write | Verified |
| S-7 [FAIL] | the spike launcher from a hostile parent environment | `of-run: REFUSED: case tree differs from the launcher record`, exit 96; record log: `REFUSED launcher.out: new file` | **The hostile environment was never tested.** The probe wrote its own `launcher.out` inside the case after `init`, so the record check refused before OpenFOAM started. Markers = 0 because nothing ran. This is a probe defect, not a launcher result | Verified |
| S-8 [FAIL] | the lint on `#include`, a quoted regex keyword and `functionObjectLibs` | exit 3: `#include` and `functionObjectLibs` refused; the quoted keyword not reported | **Lint gap, low impact.** The lint marks keyword position by the previous token (`foam-dict-lint.py`:78). After `#include "x"` the previous token is `"x"`, so `"(U|p)"` is not in keyword position. Any file with a `#` directive is already refused, so the gap never lets a file through | Verified |

**Net.** The refusal works on the pinned build: serial, parallel, against a case override, and for `systemCall`.
No probe ran case code under the product controlDict. 0 of the 5 FAILs is a failure of the refusal.

Correction to the brief: S-7 did not "refuse the hostile environment". It refused the probe's own output file and
never reached the environment.

## 3. The two lenses argue

**Security & identity architect (Adversary).** My job is the trust boundary. Under this threat model there is
exactly one boundary that carries code: **dictionary text → the OpenFOAM process.** S-1 proves that text can compile
and run C++ under the default. That boundary needs one control, and S-2/S-4/S-5 show the control works. Three more
boundaries are real. **A foreign design file → the emitters:** a profile name with `; #codeStream {…}` in it, or a
number written as `1,5`, would reach a dictionary, so values must be typed and names must be the app's own ids.
**The run directory → the user's file system:** deleting or overwriting the user's files cannot be undone. **The
installer → the internet:** it needs a pinned hash, and so does an install the user already had. I will not let go of
the controlDict switch, the banner check, typed emission, the run-directory path policy and the install hash. I
accept dropping the rest, on one condition: the app never runs a case directory it did not write, and "wrote" means
every file came from an app emitter. If that changes, `libs` (S-6) and binaries in the case tree come back into
scope.

**Simplifier (Adversary).** D2 has 6 rules, 4 pre-launch gates, 2 hashed controlDict sources, a stop file, 8 probes
and a residual-risk list. Most of it defends against someone who edits the case between `verify` and launch. That
person is the user. Delete-list:

- `delete:` the case-tree record as a launch gate. The app writes a fresh run directory and launches at once. Keep
  the manifest only as the run's audit record.
- `delete:` the per-case checkMesh pre-flight. The banner is in every log. Check it on every run.
- `delete:` the persistent stop file. "Allowing" in an app-written case means an app bug. Stop that run and report
  it. The next run checks again anyway.
- `delete:` the option deny-list (`-libs`, `-case`, …). argv is built in code from an enum. No option comes from
  outside the code.
- `yagni:` two controlDict sources (product HOME file **and** the `FOAM_CONTROLDICT` text). One is enough; the
  banner proves it took effect.
- `yagni:` the per-launch bundle hash. The app writes the file from its own resources. The banner checks the effect.
- `delete:` the load-average and `join.lock` waits in the product, and a wall-clock cap nobody would set. The waits
  serve this repo's agents. Stop and the iteration cap cover a long run.
- `shrink:` the runtime lint becomes one CI test over the template emitters, with the existing lint as its oracle.
- `shrink:` S-1..S-8 become 2 probes (S-1 control, S-2 serial refusal), re-run only on a pin change.

**Where they agree.** The controlDict switch is the one code-execution control. The banner check is how it is
verified. Typed emission is the boundary rule for foreign input. The install hash covers the supply chain. The rest
is correctness or audit, kept only where it protects risk (b).

**Where they disagreed, and the resolution.**

- *Two controlDict sources.* Security wanted both ("belt and braces"). The simplifier asked what the second catches
  that the banner does not. Nothing: if the one source fails, the banner is not `Disallowing` and the run stops.
  **One source: the app-owned HOME file** (S-2, S-3 and S-4 used it alone). It is also the file OpenFOAM's own
  warning names (`~/.OpenFOAM/2512/controlDict`).
- *Symlinks in the run directory.* The simplifier called the guard out of scope (only a same-user actor could plant
  one). Security kept it: deletion is the one irreversible harm in risk (b), and the guard is one negative test.
  **Kept, as a test, not as runtime machinery.** Security holds the hard veto here.
- *The emitter test.* Security wanted every user-sourced field covered; the simplifier wanted one test, not a port
  of the lint's 26 fixtures. **One test, one hostile fixture covering every user-sourced field, with
  `foam-dict-lint.py` as the oracle.** Both conditions are met.
- *Runaway runs.* The simplifier dropped the wall-clock cap; security asked for a hard-kill fallback. **No
  wall-clock cap; Stop escalates to a kill, and orphan ranks are reaped.**

## 4. Per requirement: keep, simplify or drop

Source: ADR-0012 D1/D2 (`docs/adr/0012-openfoam-backend-macos.md`:58–143), spec A5.10 (:1057–1070) and A8.5
backend-substrate row (:1329; cited as :1306 in `fluids-round2.md`:44 and Ruling 67), round-3 plan
(`fluids-round3.md`:255–258, DR-F3-2, DR-F3-6), round-2 verdict Q3 (`verdict-round2.md`:51–91).

| # | Requirement | Verdict | One-line reason (threat) |
|---|---|---|---|
| 1 | `allowSystemOperations 0` in a controlDict the app supplies (D2 rule 3) | **Keep** | (a)(b): S-1 shows dictionaries run code by default; S-2/S-4/S-5 show this stops it |
| 2 | Banner check after launch; a missing banner fails the run (D2 rule 5) | **Simplify** | (b): one master banner per launch, read from the live output; anything but `Disallowing` stops the run; no stop file |
| 3 | Persistent stop file, per checkout or per user (D2 rule 5) | **Drop** | Allowing in an app-written case is an app bug; the next run checks again; a sticky lock only adds support calls |
| 4 | Two controlDict sources, product HOME file **and** `FOAM_CONTROLDICT` text (D2 rule 3) | **Simplify** | One source, the app-owned HOME file; the banner proves the effect; either alone is verified (S-2/S-4 vs S-5) |
| 5 | Pinned sha256 of the controlDict bundle, re-checked every launch and on the copy (D2 rule 3) | **Drop** | Only someone who can already edit the app could change it; the banner checks the effect directly |
| 6 | argv only; never `-c`, never a shell string; `zsh -f -e etc/openfoam -- app args` (D1, D2 rule 1, spec :1329) | **Keep** | (a)(b): a value from a foreign file can never become shell syntax; costs nothing |
| 7 | App-name allow-list; option deny-list (`-libs`, `-case`, `-fileHandler`, `-roots`, `-*-switch`) (D2 rule 1) | **Simplify** | Apps are an enum in code and options are built from typed values; no outside string reaches argv, so no deny-list |
| 8 | Refuse a case file named like argv[0] (D2 rule 1) | **Drop** | The run directory is fresh and app-written; the CI emitter test asserts no emitted file name equals an app name |
| 9 | `env -i` allow-list; launcher under `bash -p` (D2 rule 2) | **Simplify** | Keep a clean child environment: it stops the user's own `FOAM_CONTROLDICT`/`WM_*` from overriding the app's controlDict or version. Prove it with a unit test, not a live probe |
| 10 | Case-tree record: verify before every launch, refuse any new file (D2 rule 4) | **Simplify** | Keep the input manifest hash as the run's audit record; drop it as a launch gate |
| 11 | Token-level allow-list lint before every launch (D2 rule 4) | **Simplify** | Becomes one CI test: the emitters over a hostile fixture, checked by the existing lint |
| 12 | Per-case checkMesh "Disallowing" pre-flight, keyed by bundle hash (D2 rule 4) | **Drop** | The banner is in every run's log; a separate pre-flight adds a step and a state with no extra protection |
| 13 | Fixed order record → lint → pre-flight → launch (D2 rule 4) | **Drop** | Follows from 10–12: only "write fresh, then launch" remains |
| 14 | Resource limits: ≤ 6 ranks, `nice 10`, load-average wait, `join.lock` wait, peak RSS (D2 rule 6, plan §4) | **Simplify** | (b): keep a core-based rank default the user can lower, `nice 10`, one job at a time, the iteration cap, a free-disk check, Stop with kill fallback; drop the waits (dev-repo only); peak RSS becomes telemetry |
| 15 | Accepted residual-risk list (TOFU at `init`, same-user write of `runs/.launcher`, verify-to-start gap, pgrep targets) (D2) | **Drop** | Each is a residual of a gate that is dropped, or of an attacker outside the model |
| 16 | "No product build may enable the backend until S-1..S-8 PASS; any pin change voids the receipts" (D2 end) | **Simplify** | The refusal is verified today. Gate Run on the install smoke test showing `Disallowing` on the user's machine; re-run S-1 + S-2 serial on a pin change |
| 17 | Install pin: DMG sha256, build id; vendor channel only; terms shown; never disable Gatekeeper (D1, spec :1329) | **Keep** | (c): the supply-chain control; checked at install and on a pin change, not per launch. Extended in M8 to an install the user already had |
| 18 | `#codeStream`/`#calc` forbidden; typed templates; case directory names from the run key (spec :1072, :1329) | **Keep** | (a): a foreign design's names and values reach the dictionaries only as typed, app-generated tokens |

Count: 4 keep, 8 simplify, 6 drop.

Also kept, unchanged and not security: the A5.10 environment assistant rule (step ids only; every parameter bound by
the tool; never elevated; no free-form shell). That rule **is** the "on rails" installer (§8).

Round-3 plan: **DR-F3-6** is done (the probes ran today). **DR-F3-2** (no new launcher app, because an addition
"changes the launcher hash, the probe pins and the security review") no longer needs that reason: adding
`mapFields` or `foamToVTK` to the spike harness is a one-line change with no security review. The round-3 resource
rules (§4) stay as they are; they govern this repo's host, not the product.

## 5. The minimal requirement set, and what each item protects

| # | Requirement | Protects against | Evidence |
|---|---|---|---|
| M1 | For each run the app writes a controlDict with `allowSystemOperations 0` (InfoSwitches) and `stopAtWriteNowSignal 30` into an app-owned HOME (`.OpenFOAM/2512/controlDict`) | case-supplied code running (`#codeStream`, `coded*`, `systemCall`) | S-1 vs S-2, S-3, S-4 (Verified, with the full shipped file plus two edits) |
| M2 | For each OpenFOAM app launch (one `mpirun` = one launch), the app reads the single master-rank `allowSystemOperations` banner and the build id from the live output. A missing banner, anything but `Disallowing`, or a build id other than the pin stops the run and reports an app fault | M1 not taking effect (wrong HOME, a changed build) | banner only on rank 0 (`S-2/ranks/…/rank.0/stdout`:32; none in `rank.1/stderr`); banner (:21) before case reading (:26) in S-1 (Verified for v2512) |
| M3 | argv arrays only, built in code from an app enum and typed values; activation `zsh -f -e …/etc/openfoam -- <app> <args>` | shell injection from any name or value | ADR D1; `etc/openfoam`:355–361 (Verified in round 2) |
| M4 | **The app runs only case directories it wrote.** "Wrote" means every file in the case was emitted by an app emitter from the typed model: no user-supplied file is copied in, and imported geometry is re-exported from the geometry of record. Names in dictionaries are app ids. Numbers are invariant-culture and finite. No `#` directive, `coded*` or `systemCall` is emitted, and no `libs` entry (**Flagged:** confirm that no template needs one; if one does, a fixed shipped set). One CI test emits every template from a hostile design fixture that covers every user-sourced field (a name with `; #codeStream {…}`, `1,5`-style numbers, NaN, infinity). It asserts that the output passes `foam-dict-lint.py` and that no emitted file name equals an app name | risk (a): a friend's design or a tutorial carrying dictionary syntax or broken numbers | spec :1072, :1329; S-6 (`libs` not gated, so "no copied-in file" is load-bearing); the lint is the existing oracle |
| M5 | The run root is under the app's data directory. Each run gets its own `runs/<run key>/`, created exclusively (fail if it exists). The app writes and deletes only inside it, and the A8.5 export/recovery path policy applies. Negative tests: `..` or a separator in a run key; a pre-existing run directory → refused; a symlink inside the run directory → removed as a link, its target untouched | risk (b): wrong paths, overwriting a previous run, deleting user files | spec A8.5 export and recovery rows (:1322, :1325) |
| M6 | A clean child environment: PATH, HOME (app-owned), USER, LOGNAME, LANG=C. Proven by a unit test of the environment builder (a parent with `FOAM_CONTROLDICT`, `WM_PROJECT_SITE`, `BASH_ENV` → the child has only these keys) | keeps M1 deterministic: with one controlDict source, a `FOAM_CONTROLDICT` or `WM_*` in the user's own shell (likely for an OpenFOAM user) would override it; M2 is only the backstop | `of-run.sh`:95 (the same key set ran all 12 round-2 runs and today's probes) |
| M7 | Resource caps: a core-based rank default the user can lower; `nice 10`; one solver job at a time; the iteration cap (`endTime`); a free-disk check before launch. Stop sends `stopAtWriteNowSignal`; after a grace period, SIGTERM then SIGKILL to the run's process group (mpirun and ranks); at app start, reap ranks left by an earlier run. Peak RSS is recorded as telemetry | risk (b): runaway CPU, memory or disk; orphaned ranks after a crash | D2 rule 6 (measured: checkMesh 3.77 GB at 2.75 M cells); kill/reap is Inferred need, handed to the SRE lens |
| M8 | Install from the vendor channel only and verify the pinned hash and build id. An install the user already has is accepted when its executables match the D1 recorded sha256 values. Otherwise it is used only after the user accepts an "unverified install" disclosure. Each run's manifest records the install identity and whether it was verified. Show the licence and the "ad-hoc signed, not notarised" disclosure. Never disable Gatekeeper or SmartScreen; removing `com.apple.quarantine` or `spctl --master-disable` counts as disabling and is a refused step | risk (c): a wrong or tampered install, including one the app did not install | D1 DMG and executable sha256 (Verified on this host) |

Deferred, with its trigger: **if the app ever runs a case it did not write** (for example "run a friend's OpenFOAM
case"), add two checks. Refuse a `libs` entry outside the shipped set, because S-6 shows `libs` is not gated. Refuse
any binary, executable or symlink in the case tree.

Option for M1 (Flagged, not decided): the app could write a two-key controlDict instead of a copy of the shipped
GPL-3.0 `etc/controlDict`. OpenFOAM merges the user file over the system file (`debug.C`:159–163, security review),
so the shipped defaults would still apply. That removes a modified GPL file from the product. It must re-run S-2
serial against the two-key file before anything relies on it. If the full copy stays, list it in the third-party
notices with its GPL header.

## 6. What to do with S-2..S-8

| Probe | Action |
|---|---|
| S-1 | **Keep** as the positive control at a pin change. Without it a refusal proves nothing |
| S-2 (serial) | **Keep** at a pin change; this is the one refusal check M1 needs. Expectation: `Disallowing`, the case-supplied-code message, exit ≠ 0, no `dynamicCode/` |
| S-2 (np2) | **Drop.** Every rank reads the same controlDict. If kept, the expectation becomes: `Disallowing` once, on the master rank; exit ≠ 0; no `dynamicCode/`; the refusal message on **at least one** rank (an MPI abort stops the others, and PMIx may lose output) |
| S-3 | **Drop.** `systemCall` reads the same `dynamicCode::allowSystemOperations` switch (`systemCall.C`:131). If kept, the expectation becomes: the warning is present, exit is not checked, and the payload writes a marker so that "not run" is observable |
| S-4 | **Drop** for the product, because the app writes the controlDict, so a case cannot add `InfoSwitches`. Today's PASS stays on record |
| S-5 | **Drop.** It tested the second controlDict source (`FOAM_CONTROLDICT`), which M1 removes |
| S-6 | **Drop.** It documents the `libs` path, which only matters under the deferred trigger above |
| S-7 | **Drop** as a live probe; it never ran (probe defect). The M6 unit test replaces it |
| S-8 | **Drop** as a probe. The lint becomes the oracle of the M4 emitter test. The emitter emits no `#`, so the keyword-after-directive gap cannot matter and needs no fix |

## 7. Proposed edits (applied — Ruling 68)

### 7.1 ADR-0012 D2 — the product launcher

**Before** (`0012-openfoam-backend-macos.md`:72–143, summarised): six rules — argv with an app allow-list and an
option deny-list; `env -i` under `bash -p`; product HOME **and** `FOAM_CONTROLDICT` from one hashed bundle, re-hashed
at every launch; before every launch a case-tree record check, a token lint and a per-case checkMesh pre-flight in a
fixed order; after every launch a banner check with a persistent stop file; resource limits with load and `join.lock`
waits. Then a residual-risk list, and: *"no product build may enable the OpenFOAM backend, or run a case it did not
generate … until committed receipts … show S-1..S-8 PASS … Any pin change voids the receipts."*

**After:**

> ### D2 — The product launcher (right-sized, Ruling 67)
>
> Threat model: one trusted user on his own laptop, running cases the app writes. The risks are a foreign design
> file, the app breaking its own run, and a bad install (`docs/notes/solver-security-right-size.md`).
>
> 1. **Code in dictionaries is off.** For each run the app writes a controlDict with `allowSystemOperations 0` and
>    `stopAtWriteNowSignal 30` into an app-owned HOME (`.OpenFOAM/2512/controlDict`).
> 2. **The banner proves it.** For each OpenFOAM app launch (one `mpirun` is one launch) the app reads the master
>    rank's `allowSystemOperations` banner and the build id from the live output. A missing banner, anything but
>    `Disallowing`, or a build id other than the pin stops the run, which is reported as an app fault.
> 3. **argv only.** The app name comes from an enum in code; options come from typed values; activation is
>    `/bin/zsh -f -e …/etc/openfoam -- <app> <args…>`, never `-c`.
> 4. **Only cases the app wrote.** Every file in a case is emitted by an app emitter from the typed model; no
>    user-supplied file is copied in. Names are app ids; numbers are invariant and finite. A CI test emits every
>    template from a hostile design fixture and checks the output with `foam-dict-lint.py`.
> 5. **Own run directory, clean environment.** Each run is a new `runs/<run key>/` under the app data directory,
>    created exclusively. The app writes and deletes only there, under the A8.5 path policy. The child environment
>    is PATH, HOME, USER, LOGNAME and LANG=C only.
> 6. **Resource caps.** A core-based rank default the user can lower, `nice 10`, one solver job at a time, the
>    iteration cap, a free-disk check; Stop escalates to SIGTERM then SIGKILL of the run's process group; orphan
>    ranks are reaped at app start.
>
> **Evidence.** The 2026-10-04 probe run (`docs/proof/spike-03/security/20261004T155708Z/`): S-1 runs code under the
> default; S-2 and S-4 refuse it under the app-owned HOME controlDict, serial and on 2 ranks, with no
> `dynamicCode/`. **Rule 1 is Verified for v2512 on macOS arm64.** A pin change re-runs S-1 and S-2 serial. Run is
> enabled only after the install smoke test shows `Disallowing` on the user's machine.
>
> **Deferred.** If the app ever runs a case it did not write, add a `libs` allow-list (S-6: `libs` is not gated) and
> refuse binaries and symlinks in the case tree.

The spike harness (`cases/tools/of-run.sh`, lint, record tool) stays as this repo's tool for fluids rounds. It is no
longer the product contract.

### 7.2 Spec A8.5, backend-substrate row (:1329; cited as :1306)

**Before** (Validated where): *"digest/hash; typed templates; argv arrays never shell strings;
`#codeStream`/`#calc` forbidden (assume: OpenFOAM dictionaries can execute code — confirm at SPIKE-03)"*

**After:** *"digest/hash at install and on pin change (an install the app did not verify runs only after an
'unverified install' disclosure); typed templates; argv arrays never shell strings; the app writes its own
controlDict with `allowSystemOperations 0` and stops any launch whose master banner is not `Disallowing` (Verified
2026-10-04: dictionaries execute code under the default, S-1, and are refused under the app's controlDict, S-2/S-4);
the app runs only case directories whose every file it emitted"*

**Before** (Negative test): *"injected profile name; floating tag; a spoofed backend "Ready" must not enable Run"*

**After:** *"injected profile name (`; #codeStream {…}`) emits no directive; a `1,5` or NaN value is refused before
emission; floating tag; a spoofed backend "Ready" must not enable Run; a launch whose banner is not `Disallowing` is
stopped; removing `com.apple.quarantine` is a refused setup step"*

Threat and Disposition columns: unchanged.

### 7.3 Spec A5.10 readiness (:1057–1058)

**Before:** *"pass = the named output files exist with the recorded column layout and the fixture's Cl lies inside
its tolerance; … re-checked at every launch"*

**After:** *"pass = the named output files exist with the recorded column layout, the fixture's Cl lies inside its
tolerance, and each launch's master banner reads `Disallowing`; run at install and on a pin change; at every app
launch only the install path is re-checked, and the build id is compared on every run from the banner (D2 rule 2)"*

## 8. What this does to the guided install

Ruling 67 (b) also asked for an AI-assisted, "on rails" installer for OpenFOAM on macOS and OpenFOAM/SU2 on Windows,
so that the operator does not do tech support. The right-size removes every security step from the install path
except the hash. The user never sees a probe, a stop file, a record or a lint. The AI picks the next step id from a
fixed list and explains it; the tool runs it (spec :1330, kept).

**Install-time checks that remain (macOS, OpenFOAM.app v2512):**

| Step | Check | Confidence |
|---|---|---|
| 1 Find or download | detect an existing `/Applications/OpenFOAM-v2512.app`; otherwise download the pinned arm64 DMG from the vendor release page (this also settles the platform) | Verified (the pinned build is arm64 only) |
| 2 Hash | a download: DMG sha256 = `5eb2ab10…393439`; an existing install: the D1 executable sha256 values; no match → "unverified install" disclosure, or download the pinned build | Verified values (D1) |
| 3 Consent | licence verbatim; "ad-hoc signed, not notarised" disclosed | Verified (D1 disclosure) |
| 4 Install | copy the app to `/Applications`; first open only through the OS's own dialog; never `xattr -d com.apple.quarantine`, never `spctl --master-disable` | first-open behaviour **Flagged**: not recorded in any round |
| 5 Identify | build id `_87ed40d256-20251219` from the banner | Verified |
| 6 Smoke test | cavity-lid fixture through the app's launcher: outputs exist, Cl in tolerance, **master banner `Disallowing`** | Inferred (fixture defined in spec, not yet run by the app) |

The rank count is a run setting with a core-based default, not an install step.

**Windows: pending, not a contract.** There is no Windows host and no evidence (DR-F2-7). The spec offers WSL2 or
Docker Desktop for OpenFOAM and SU2 v8.5.0 release binaries. Pick one OpenFOAM route when the Windows round runs. The
same six steps apply: an elevation or reboot prompt only through the OS, with a pinned image digest or zip sha256
that is not yet recorded. SU2's `.cfg` is key = value with no known code-execution feature (**Inferred**, unchecked).

Gone from the install path: Docker on macOS, the S-1..S-8 probes, per-launch hash checks, the stop file, the case
record and the per-case pre-flight. The security part of a support call is now one question: "did step 6 say
`Disallowing`?"

**Decision request DR-SEC-1 (for the operator).** Ruling 67 says the user "would have ALREADY committed to OpenFOAM
ecosystem", and also that he uses OpenFOAM "but not locally". Which install path does the guided installer lead
with?

- **A.** Detect an existing install and accept it when its hashes match; otherwise download the pinned build.
- **B.** Always install the pinned build, in its own place.

Recommendation: **A**. It is one extra branch, and it avoids a second OpenFOAM on a machine that has one. Only one
path gets built. **Ruled A (Ruling 68).**

## 9. Residual risk (after right-sizing)

- A foreign design whose **numbers** are valid but physically wrong. This is a correctness risk, handled by the
  physics gates and labels, not by security.
- `libs` is not gated by the switch (S-6). Only M4 closes it ("every file emitted by the app"). The risk reopens
  under the deferred trigger.
- M2 detects an app fault; it does not prevent one. For an app-written case there is no case code to run first.
- An existing install that does not match the D1 hashes runs only after the user accepts the disclosure.
- Windows: no evidence, no SU2 hash pin, and the SU2 claim is unchecked (DR-F2-7).
- OpenFOAM v2512 and SU2 have no SBOM yet; no CVE scan was done.

## 10. Review (Adversary lenses, read only, 2026-10-04)

| Lens | Verdict | Findings and disposition |
|---|---|---|
| Security & identity architect | **PASS WITH CONDITIONS** (hard veto cleared; 0 Blockers) | 4 Majors, all applied: banner on the master rank only (M2, D2 rule 2, §7.3); an existing install is not hash-checked (M8, DR-SEC-1); "wrote" undefined (M4); M5 cited a repo agent rule, not a product contract (now the A8.5 path policy, exclusive create, negative tests). 4 Minors applied: the emitter test covers every user-sourced field with the lint as oracle; the GPL controlDict option; quarantine removal is a refused step; Stop kill fallback and orphan reap (handed to SRE). 1 Nit applied: "paths" → "sources". It confirmed the reading of S-2, S-3, S-7 and S-8 against the logs and the v2512 source |
| Simplifier | **BLOCK (soft)** on 1 Major → cleared by the fix | Major (same as security's first): banner on the master rank only, applied. Minors applied: build id compared in the banner read, not by a probe at app launch; one emitter test, not a port of 26 fixtures; no wall-clock cap; peak RSS is telemetry; core-based rank default; install 8 steps → 6; Windows marked pending; DR-SEC-1 raised; M6 reason restated; S-5 removed from the D2 evidence; the §9 timing bullet shortened. Not applied: delete the symlink clause. Security's hard veto covers the one irreversible harm, so it stays as one negative test (§3) |
