---
id: review-windows-gpu-f1-r232-reader
title: "Independent Ruling 229 F1 reader-sharing review"
type: doc
status: accepted
owner: "@pc-f1-r232-reader-review"
phase: preflight
tags: [windows, reader-sharing, offline, independent-review]
links:
  - { to: proof-f1-ruling-229-reader-sharing-r232, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  ACCEPT exact offline reader-sharing candidate. Independent PowerShell 5.1/7
  probes verify sharing, decoding, event boundaries and exception disposal;
  distinct ring results are retained with their separate cost misses.
---

# Verdict and binding

**ACCEPT** candidate `70d022bf4369c8cb58888f88e6efba00070f35db`, parent
`64dbed61b68364af6f3bf12ac986809abac285ee`, for the bounded offline reader
repair. No hard veto remains in this review scope. The author does not clear
its own veto: this independent review executes production function bodies and
the static controls. No reviewer source repair, WSL, registry, preflight,
cleanup, terminate, unregister or other live lifecycle action occurred.
Existing proof receipts and authority are not promoted to cover new bytes.

| Candidate file | Git blob | SHA-256 |
|---|---|---|
| Invoke-Preflight.ps1, 33,057 bytes | `ab47d52e450099c2f6a82feb8a6cf7027b44525c` | `3bc3be1b64e8d5e2aec5c18b6817494ece30a82c7e12852b9f375414048534c4` |
| SelfTest.ps1, 14,043 bytes | `8605616014058b45a1fa4afb7c553e3122bd742c` | `ee263c1ff382293ace76be40f3ae89c8ee30be46e54fe5e3dba72b002b48f953` |

Both values were recomputed from committed blobs and compared with working
bytes. The structured independent receipt is `r232-reader-evidence.json`; its
producer is `r232-reader-evidence.py`. Only hashes, counts, timing and allowlisted
verdicts are published; private ring files and telemetry raw content stay local.

# Production behavior and adversarial controls

**VERIFIED:** both guard-state and event reads now pass through the actual
`Open-LiveEvidenceStream -> Read-LiveText` functions. The opener requests
read-only access with `FileShare.ReadWrite -bor FileShare.Delete`. No writer
behavior, guard identity, stale-state threshold, last-LF selection, event verdict
or parsing condition changes. The producer writes canonical bytes, appends
complete event rows and atomically replaces state; permitting write/delete
sharing addresses those filesystem operations.

The independent `r232-reader-probe.ps1` AST-extracts only the four production
reader/guard functions; wrapper top-level actions never execute. It passes on
Windows PowerShell 5.1 and PowerShell 7, including:

- The old `[IO.File]::ReadAllText` event expression fails while a writer is held
  open. Under the same fixture, the new production `Assert-Guard` succeeds.
  This independently reproduces the behavioral red-to-green boundary.
- UTF-8 with/without BOM and UTF-16 little/big endian BOM decode exact Unicode
  text. The StreamReader's UTF-8 default and BOM detection are explicit. The
  state JSON BOM and CRLF event row plus unfinished tail work through production
  guard functions. Existing invalid-byte decoder fallback is not newly claimed
  strict; this change preserves the standard decoder behavior.
- Empty/no-LF events, incomplete-only events, a malformed complete final row and
  a complete stop verdict refuse. Unfinished trailing text is ignored only
  after the last complete LF row. JSON failure occurs after the reader closes;
  immediate exclusive open succeeds, ruling out a retained file handle there.
- Rename, replacement-path read and deletion work while the original reader is
  held. This is Windows temporary-file sharing evidence, not a live drvfs test.
- A StreamReader-constructor failure on a non-readable stream disposes the
  owned FileStream. A synthetic throwing Stream proves a `ReadToEnd` exception
  disposes the reader-owned stream. Nested finally blocks also protect the
  stream if reader disposal throws; source establishes that control flow.

The normal opener returns only after successful construction; an open failure
does not leave an acquired handle owned by the function. Double disposal of
the FileStream is harmless in the tested runtimes. No catch turns a failed read
into empty or apparently valid state. Concurrent modification is still subject
to the existing event/state validation; this repair makes no atomic-pair or
snapshot claim and does not change the accepted guard protocol.

The author's reported initial regression red was a missing-opener assertion;
the subsequent token-regex correction is disclosed. Independently observed
behavioral red/green above supplements that structural red. The permanent test
invokes actual AST-extracted functions, not a parallel implementation. Its
temporary files and in-memory guard fixture do not confer live authorization.

# Class control, exemptions and history

**VERIFIED:** comparing literal `PS_GRANDFATHERED` sets from the parent and
candidate removes exactly the two F1 path/hash pairs, adds none, and preserves
the other seven B1/B2 pairs byte-for-byte. Static scan reports exactly those
seven immutable entries and zero unshared readers. Scanner logic is unchanged;
its self-test catches nine C# and three PowerShell offenders and accepts eight
C# and six PowerShell clean forms. The targeted behavioral fixture provides
the explicit-sharing floor that the generic FileStream naming heuristic cannot
fully prove by itself.

The READER-SHARE-DELETE class entry, scanner documentation and check-docs comment
consistently describe seven remaining exemptions. Its naming heuristic and
offline-versus-live limits remain explicit. Git comparison confirms B1/B2 proofs,
historical F1 run/recovery/attempt evidence, rulings, `src/` and `tests/` are
unchanged. Existing source-hash receipts are intentionally not regenerated.
Candidate audit entries preserve the initial red and append the later authorized
second-run result; the earlier entry is not rewritten into a pass.

# Independent checks and both ring attempts

| Review command | Observed result |
|---|---|
| `powershell -NoProfile -File docs/proof/win-gpu-f1-preflight/packet/SelfTest.ps1` | PASS, 0.471 s reported |
| Same packet test under `pwsh` | PASS, 0.369 s reported |
| `powershell -NoProfile -File docs/reviews/r232-reader-probe.ps1` | PASS, including behavioral old-reader red and disposal negatives |
| Same independent probe under `pwsh` | PASS |
| `py -3 tools/check-reader-sharing.py --self-test` | PASS, planted failures/clean controls |
| `py -3 tools/check-reader-sharing.py` | PASS, seven B1/B2 exemptions only |
| Safe verify bundle with application-core/adapters/windows-store explicitly skipped | 10/10 PASS; not blanket readiness |
| `py -3 docs/reviews/r232-reader-evidence.py` | Exact source/set bindings and both retained ring classifications PASS |

The ring was **not rerun by this reviewer**. Coordinator-supplied private
captures contain 80 files for attempt 1 and 116 for attempt 2. The independent
receipt hashes their complete inventories and the primary log/timing files.
Original tool telemetry at the recorded JSON paths independently establishes
the exit codes, rather than inferring them from counts. Source line and output
hashes bind those observations without publishing raw text or machine paths.

| Observation | Attempt 1 | Bounded attempt 2 |
|---|---:|---:|
| Original ring exit | **1** | **0**, all harnesses passed |
| Desktop PASS | 673 | 776 |
| Desktop expected classifications | 30 | 33 |
| Desktop unexpected classifications | **18** | **0** |
| Desktop partition records | 12 | 15, all parts complete |
| Wall milliseconds | 169,751 | 190,933 |
| Cost failures | 0 | 0 |
| Cost misses | 16 | 17 |
| End load | 17.19 | 13.45 |

Attempt 1 retains its timeout/drain/incomplete-partition failures. Attempt 2
has all 18 Desktop child completion records and no child at the 120-second
deadline. In both attempts the other logs classify with zero unexpected results:
Core 346/210/205 passes and 9/7/11 expected classifications; Analysis 44/69/146
passes; CLI 18 passes and two expected classifications; Python 50 tests, OK.
Expected classification counts include derived child-exit classifications and
do not mean all raw child processes exited zero.

**VERIFIED:** attempt 2 is functionally green, while exceeding the 60-second
budget. Its 190,933 ms wall / 188,819 ms net and separate budget-miss line remain
in the receipt. The original output records 17 cost-check misses and also emits
the budget-miss line; no budget success is claimed. The first red is not erased
or suppressed by the second result. **INFERRED only:** concurrency/load explains
the earlier failure; the two runs do not prove causation. Two ring attempts
reach the authorized cap; this review does not authorize a third.

# Remaining limits

Final documentation, graph, audit, privacy and whitespace checks are recorded in
the closing audit. Acceptance applies to exact candidate bytes and the measured
offline Windows sharing behavior. It does not prove a live Linux/Windows sharing
path, current target/protected state or cleanup success. Joining or activating
future work must preserve normal gates and obtain any required fresh owner
ruling and exact packet/authority rebindings. This review grants no live action.
