---
id: proof-blend-certificate-heap
title: "Blend certificate heap spike verdict (Ruling 73, DR-XPA-1 c)"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: design
tags: [certificate, blend, heap, budget, all-query-bound, spans, ruling-73, dr-xpa-1, spike]
links:
  - { to: proof-blend-certificate-budget, rel: depends-on }
  - { to: design-cross-profile-abscissa, rel: relates-to }
  - { to: design-m12d-catalog, rel: relates-to }
  - { to: proof-m12b2-golden, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Measured on a branch build. A binary max-heap in Bernstein.Maximum, keyed on (coefficient maximum, insertion
  sequence), reproduces the rescan bit for bit: 52 of 52 fixtures, 1,196 queries, admission and query work equal.
  With its comparisons charged instead of 6 N (N + 1), the 1e6 all-query bound admits the 27-span (32-point) ceiling
  for two to four differing stations, 20 spans for five and 5 spans for six. GO for heap + max(256, 48 s) with the
  bound kept at 1e6.
review-suggested: []
---

# Blend certificate heap: spike verdict

**Verdict: GO for "heap + `N(s) = max(256, 48 s)`, bound kept at 1e6".** The heap changes no answer. Every query and
every admission bit matches the rescan build. Only the declared operation count changes, and that count is now derived
from the heap code. Under the unchanged 1e6 bound and 1e-12 gap, every fixture family in the previous spike certifies
up to the largest span count measured. The measured edges are **five differing stations at 20 spans** and **six at
5 spans**.

Environment: Apple silicon, .NET SDK 10.0.203, Release. Branch `spike/blend-certificate-heap` on `3f8ac16`. That Core
`src/` is identical to `fb0cd12`, the as-built Core the previous spike measured (`git diff --stat fb0cd12 3f8ac16 --
src tests` is empty). Other sessions shared the machine. The 1-minute load average was:

- **17.8 → 15.1** during the six parallel probe runs;
- **12.2 → 11.3** during the first ceiling pass, and **13.7 → 29.1** during the second (with six stations);
- **27.9 → 31.6** during the timing run in `output/timing.md`.

Work is deterministic. Times are not.

## 1. What changed (src)

- **`Bernstein.Maximum`** (`Geometry.cs`): pending nodes sit in a hand-written binary heap (`MaximumHeap`). Order:
  coefficient maximum descending, then insertion sequence ascending.
  - The rescan picked the first node in list order whose maximum equals `upper` (`FindIndex`). The list order is the
    insertion order: `RemoveAt` keeps the relative order, and `Add` appends left then right. So the heap top is the
    same node. Sequences are unique, so the order is total.
  - `upper` is the heap top's maximum. That equals the largest coefficient of any pending node, as before.
  - Unchanged: `Budget(watch)` per node, the depth guard (`Depth < 64`), both refusal texts, the `lower` updates, the
    tolerance test, and every `Rational` the loop constructs.
  - Every rational comparison goes through one site, `Compare`. A comparison constructs no `Rational`, so it costs no
    bit-work. This is as everywhere in Core, and it is why the work columns can match. **The comparisons are charged
    in the all-query operation model** (§2), and the probe counts them (§3.4).
  - `PriorityQueue` was not used. Its 4-ary layout is a library detail, and the operation bound must follow from code
    in this file.
- **Node budget** (`:394`): `int blendNodes = Math.Max(256, 48 * spanCount);`. The admission check
  `spanCount * 48 <= blendNodes` is kept. It now always holds (residual risk 4).
- **Operation model** (`QueryFeasibility.Prove`): the blend term `64 N + 6 N (N + 1)` becomes
  `64 N + 2 · MaximumComparisons(N, s, p)`. `Prove` receives `spanCount` and `degree` from admission. The 1e6 bound and
  the 1e-12 gap are unchanged.
- **Golden master** (`docs/proof/m12b2-golden`): two deliberate values. `RationalOperationsUpper` of `blended-dihedral`
  and `blended-peaks` goes from 859,600 to 487,922. The receipt records the arithmetic.

## 2. The heap's operation charge, derived from the code

One `Maximum` call, node budget N, s spans of degree p. Each node pops one entry and pushes two, so the heap holds at
most s + N entries. Let **L = ⌊log₂(s + N)⌋**, the largest number of levels a sift can cross.

| Site | Comparisons |
|---|---|
| Starting `lower` (both ends of each span) | 2 s |
| Initial push of each span: its maximum (p), then sift-up (≤ L) | s (p + L) |
| Per node: stopping test (`lower > 0`, gap ≤ tolerance) | 2 |
| Per node: `Pop` sift-down, two comparisons per level | 2 L |
| Per node: `lower` update from the split point | 1 |
| Per node: two pushes, each p + L | 2 (p + L) |
| Final stopping test, at return | 2 |

```
MaximumComparisons(N, s, p) = 2 s + s (p + L) + N (2 p + 4 L + 3) + 2
ops_blend = 64 N + 2 · MaximumComparisons(N, s, p)          (two Maximum calls per query)
```

The 64 N term (32 split and tolerance operations per node, two calls) is kept as built.

At N = 768, s = 16, p = 5 (L = 9): MaximumComparisons = 37,890, so the blend term is 49,152 + 75,780 = **124,932**.
The rescan charged 3,592,704. The previous spike's estimate was about 114 k.

**Checked exactly against the run:**
- The golden fixtures (s = 1, N = 256, L = 8): 859,600 − 394,752 + 23,074 = 487,922. That is the value the Core
  computed.
- Every hooked row: heap ops − 2C + 6 N (N + 1) equals the previous spike's measured rescan ops wherever both exist.
  For example, four sections at s = 6 gives 1,220,592, and two sections at s = 16 gives 4,041,648.

## 3. Results

### 3.1 Neutrality (`output/neutrality.md`, 0 differences)

| Comparison | Rows | Equal |
|---|---|---|
| A. Hooked heap vs hooked rescan, same rule, bound lifted: admission atoms and gap, status without the op bound, and the 23-query sweep (result hash, query work, nodes used, max T0 gap, refusals) | 104 (52 fixtures × rules 256 and 48 s) | **104** |
| A′. Admission work at 1e6, where both builds reached the same status | every such row | equal |
| B. Plain heap build vs plain as-built Core, wherever the as-built Core certified (status, admission work, query work, hash) | 11 | **11** |
| Fixture inputs: byte-identical to the previous spike's | 52 | 52 |

- **All 52 fixtures × 23 queries under 48 s (1,196 queries) are bit-identical to the rescan.** Query work is the summed
  operand bits of every `Rational` built, so an intermediate that differed would show there as well as in the hash.
- Admission work differs only on rows that the rescan build **refused at the op bound**. That refusal stops before the
  witnesses, so it spent less. There are 41 such rows (20 + 14 + 3 + 4), and all now certify.
- The as-built Core is not re-run. Its rows are the previous spike's committed outputs from a `src/` identical to this
  branch's base.
- The 52 shared fixtures were deleted from `fixtures/` after the check, because they are duplicates. Only the 16 new
  ones are kept, and the probe regenerates them all.

### 3.2 Capacity: max spans per section (`output/summary.md` A, `output/ceiling-heap.md`)

Each figure is the largest s such that every measured span count of the family up to s certifies (work 1e9, 1 s).
In the 4.2 M column, "d" means the rescan op count was derived exactly (§2) from this run, for fixtures the previous
spike did not run.

| Family (spans measured) | As built (256, bound 1e6) | Rescan + 48 s + bound 4.2 M | **Heap + 48 s, bound 1e6** |
|---|---|---|---|
| Two sections, 120 mm (5–16, + 27) | 5 | 16 (27: refused, 4.05 M at 16) | **16; 27 certifies (670,538 ops)** |
| Two sections, 2 m (5–16) | 5 | 16 | **16** |
| Three sections (5–16, + 27) | 5 | 16 | **16; 27 certifies (797,946)** |
| Four sections (1, 3, 5, 6, 8, 12, 16, + 27) | **none** | 12 d (16 = 4.30 M) | **16; 27 certifies (925,354)** |
| Five sections (5, 8, 12, 16, + 20, 21) | none | 12 d (16 = 4.42 M) | **20 (987,124); 21 refused (1,003,118)** |
| Six sections (1, 3, 5, 6) | none | 6 d (1.47 M at 6) | **5 (996,394); 6 refused (1,001,448)** |
| New foil 10 pt + 0 / 1 / 2 anchors, two sections (5 / 6 / 7) | 5 / none / none | 5 / 6 / 7 | **5 / 6 / 7** |
| New foil + anchors, three sections | 5 / none / none | 5 / 6 / 7 | **5 / 6 / 7** |
| Example 8 pt + 0 / 1 / 2 anchors, two and three sections (3 / 4 / 5) | all | all | **all** |

- With the heap, the op count barely depends on spans: **+7–8 k per span**.
- The count is driven by the number of differing profiles: **about +127 k per profile** (127,056 to 127,232 measured), the per-curve inverse
  bisection.
- The 1e6 bound is now a **limit on differing stations**:
  - up to four stations at any span count up to the 32-point ceiling;
  - five stations up to 20 spans;
  - six stations up to 5 spans;
  - seven stations never. This is Inferred by arithmetic, not run: six stations at s = 1 measured 995,890, plus the
    measured 127,056 per profile, gives 1,122,946.

### 3.3 Measured cost (all heap-build rows, 68 rows)

| Quantity | Measured | Of the limit |
|---|---|---|
| Worst admission work | 95,602,581 bit-work (five sections, 20 spans) | 9.6 % of 1e9 |
| Worst query work (one `SectionAt`) | 30,895,022 (two sections, 6 spans; knots k/6) | 3.1 % |
| Worst admission time (median of 3), five sections at 20 spans | 234 ms at load ≈ 12; 701 ms at load ≈ 29 | < 1 s |
| Worst query time | 393 ms (five sections, 20 spans, load ≈ 29); 163 ms in the stage runs (load ≈ 17) | < 1 s |
| Re-timed (`output/timing.md`, 9 admissions, 5 per η, load 27.9 → 31.6) | five sections at 20 spans: admission **687 min / 796 median ms**, query 219 / 261 ms. All others: admission ≤ 218 / 319 ms, query ≤ 202 / 254 ms | — |
| Query nodes used | 16–21 at every span count (as before) | N = 256–1,296 |
| Query refusals | 0 | — |
| max T0 relative gap | ≤ 1.94 × 10⁻¹², unchanged | each call ≤ 10⁻¹² |

### 3.4 Comparisons: measured vs charged

- The hooked copy counts every `Compare` call (`probe/patch-core.py` patch 4).
- Most measured in one query (two calls): **1,466** (27 spans), against a charge of 138,298.
- In 83 hooked rows with a query sweep, none exceeds its charge.
- The charge is a worst case for N nodes. A query uses 16–21 nodes, so the charge is about 1 % used. This is the same
  "charge the budget, not the use" property the previous spike found. It now costs O(N log N) instead of O(N²).

## 4. Tests

- `CFD_TEST_ONLY=Blend,Geometry,Assess,PlacementRule,ProofBudget,BlendRule` (Release): **59 ran, 0 failures**. This
  covers `PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged` (query bits),
  `…AssessWitnessesAndRefusalsUnchanged` (after the two-value golden edit) and `PlacementRule_OperationTree_TraceGolden`.
  - Before the golden edit, the only red was the expected one: `Expected 859600; actual 487922`.
- `tools/run-tests.sh`, once: **exit 3 (green, over budget)**. Line: `wall 89 s (budget 60 s) cpu 761 s load 8.19 -> 27.20`.
  - Core: 299 + 298 PASS, 0 failures. Desktop: 610 PASS. Analysis: 12 PASS. Cli: 3 PASS.
  - No `FAIL` line in any log. The known load-flaky Desktop check passed, so it was not re-run.
  - The overrun is contention: the 1-minute load rose to 27 from other sessions. Inferred: the Core parts are not
    slower, since the heap removes work and adds none.

## 5. Residual risk

1. **Twin-peak shapes** (carried over): best-first search can split more than one node per level on near-ties. The
   measured need (16–21 nodes) is far inside N. An exhausted budget refuses with `GEOMETRY-CERTIFICATE-DEFECT`. It
   never gives a wrong enclosure.
2. **Bit-work does not charge comparisons** anywhere in Core, so the `ProofBudget` does not see them. They are bounded
   by the operation model instead: at most 2 × 37,890 per query at 16 spans. Each costs about one multiply of
   coefficient width. Inferred: under 1 % of the measured query work.
3. **Timings** come from one Mac under load 10–32. The heaviest admission (five stations, 20 spans, 95.6 M bit-work)
   took 234 ms at load 12 and a median of 796 ms at load 30. That is close to the 1 s gauge under heavy contention.
   - The proof limit is work, not clock (DET-CLOCK), and work is at 9.6 %. So this is a responsiveness risk, not a
     refusal risk.
   - Quiet-machine figures are Inferred, from about 2.1 ns per unit: about 0.2 s.
4. **The admission check `spanCount * 48 <= blendNodes` is now a tautology** under the rule. It is kept as the stated
   argument. Remove it, or turn it into an assertion, when the rule lands on main.
5. **Not covered:**
   - five-station spans 6–7, 9–11 and 13–15 (5, 8, 12, 16 and 20 were run, and work is not monotone in s);
   - four-station spans between 16 and 27;
   - six-station spans 2 and 4;
   - seven stations (Inferred refused);
   - open trailing edges.

   No tests were added. The tie-order equivalence is guarded by the golden master (two blend fixtures) and this probe.
   A unit check that pins `Maximum` against a reference rescan over a tie-heavy input would make the guard local.

## 6. What it means for the user

| Change | Sections that may differ between stations |
|---|---|
| As built | Two or three stations: up to 10 points (5 spans). Four or more: never |
| **Heap + 48 s, 1e6 kept (this spike, measured)** | **Two to four stations: up to the 32-point ceiling (27 spans).** Five stations: up to 25 points (20 spans). Six stations: up to 10 points (5 spans, a New-foil section with no anchor). Seven or more: never (Inferred) |

The previous verdict's §4.2 finding (four differing stations refused) is resolved by this change. The 1e6 bound is now
a limit on the number of differing stations. Each adds about 127 k operations, and spans add only about 8 k each.
Seven or more differing stations would be refused at any span count. Catalog Replace and compatible fit inherit that
limit. Raising the station limit means re-modelling the per-curve inverse bisection (63,488 per curve), not the blend.

## 7. Reproduce

```sh
cd docs/proof/blend-certificate-heap
python3 probe/patch-core.py ../../.. <scratch>/core-hooked/CfdWorkbench.Core
dotnet build -c Release probe -p:CoreProject=<scratch>/core-hooked/CfdWorkbench.Core/CfdWorkbench.Core.csproj -o <scratch>/bin-hooked
dotnet build -c Release probe -o <scratch>/bin-heap
for m in stage1 stage2 four ceiling; do dotnet <scratch>/bin-hooked/CfdWorkbench.Core.Tests.dll $m .; dotnet <scratch>/bin-heap/CfdWorkbench.Core.Tests.dll $m .; done
dotnet <scratch>/bin-heap/CfdWorkbench.Core.Tests.dll timing .
python3 probe/neutrality.py . && python3 probe/summarize.py .
```

The six stage runs took 56 s in parallel (the previous spike took about 4–5 min per stage). The ceiling runs took 20 s
(first pass) and 39 s for the plain build in the second pass (with six stations, at load 29).

## 8. Durations

Measured from the scratch timestamps: the probe started at 16:31, and the verdict was finished at about 16:55 (local).
The session start was not recorded, so the total is not recorded. No repair cycle was needed.

Two corrections were made during the work:
- **Neutrality script** (§3.1): admission work is compared only where both builds reached the same 1e6 status.
- **Six stations:** a first draft of this verdict inferred "six stations never" from a rough per-profile model. A
  check against the measured per-profile increment contradicted it. The case was then measured: six stations
  certify up to 5 spans.
