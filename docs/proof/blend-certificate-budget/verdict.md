---
id: proof-blend-certificate-budget
title: "Blend certificate budget spike verdict (Ruling 73, DR-XPA-1 c, DR-M12D-1 spike E)"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: design
tags: [certificate, blend, budget, all-query-bound, spans, ruling-73, dr-xpa-1, dr-m12d-1, spike]
links:
  - { to: design-cross-profile-abscissa, rel: relates-to }
  - { to: design-m12d-catalog, rel: relates-to }
  - { to: proof-cross-profile-abscissa, rel: depends-on }
  - { to: proof-m12c-certificate-spike, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Measured against the as-built Core. The node budget can scale with spans without weakening soundness: it never enters
  the enclosure (52 fixtures x 23 queries are bit-identical under five rules), and the real need is 17-21 nodes at any
  span count. Scaling it alone gains one span (two sections: 6; three: 5). The limit is the all-query operation bound
  (1e6), which charges 6 N (N + 1) for the budget N rather than for the nodes a query uses. With that bound lifted,
  5-16 spans certify for two and three sections at 120 mm and 2 m within 3.9 % of the 1e9 work limit and under 1 s.
  New finding: four differing station sections are refused as built at any span count.
review-suggested: []
---

# Blend certificate budget: spike verdict

**Verdict: GO for a span-scaled node budget, but the budget alone gains one span.** The binding limit is the all-query
operation bound. Stage 1 is not a no-go: 8 spans, and every count up to 16, certify within 1e9 work and 1 s once the
all-query bound is lifted. The **recommended rule** is `N(s) = max(256, 48 s)`, paired with one of two changes to the
all-query bound (§4). That bound is a decision for the operator.

Environment: Apple silicon, .NET SDK 10.0.203, Release, Core at `fb0cd12`. Load average **27–42** during every run,
because other sessions were running on the machine. Work is deterministic. Wall times are upper bounds (§3.3). No
src/ or tests/ file changed.

## 1. Method

- **Probe** (`probe/`). It reuses the Core test assembly's `InternalsVisibleTo` grant (the GSPK, SPK and XPA pattern).
  Every proof runs in Core:
  - `Geometry.Assess` (admission) and `Geometry.SectionAt` (a query) each run under their own `ProofBudget` (limit 1e9).
  - Fixtures are built with `DatImport.FitToBasis`, `FoilSource.SqrtProfileBasis`, `SectionEdits.Apply` and
    `FoilSource.InsertOnce`.
- **Instrumented copy** (`probe/patch-core.py`). It copies `src/CfdWorkbench.Core` to the scratchpad and makes three
  hook edits in `Geometry.cs`, plus a `ProbeHooks` class:
  - the node budget at `:394` becomes a rule of `spanCount`;
  - the all-query bound at `:736` becomes a parameter, and the operation count is recorded;
  - `EncloseMaximumT0` (`:218`) records the nodes used and the enclosure.

  At its defaults the copy **is** the as-built Core. The constant-rule rows run against both builds and match
  **52 of 52**: status, reason, admission work, query work, atoms and the result hash
  (`output/neutrality-{stage1,stage2,four}.md`).
- **Fixtures** (`fixtures/`, 52 files):
  - **Synthetic.** NACA 0012 at Root, Example `section-a` at Tip, and a cambered NACA variant at Mid or Outer. All are
    fitted onto one sqrt spacing with s + 5 points (s spans, simple knots), so they share abscissae and differ in
    shape. Chord 120 mm and 2 m (GSPK planform).
  - **Authored.** New foil (10 points) and Example (8 points), with Root made distinct by a y move, then 0, 1 or 2
    anchors propagated to the differing neighbours (XPA case J).
- **Rules** for the node budget N, with s = spans:
  - `256` (as built);
  - `max(256, ⌈51.2 s⌉)` (today's ratio per span);
  - `max(256, 48 s)`;
  - `max(256, 96 s)`;
  - `1,000,000` ("unbounded"; it measures what a query actually uses).
- **Per row:**
  - admission three times with the bound at 1e6 (status, work, median time);
  - admission once with the bound lifted (status, operation count);
  - 23 `SectionAt` queries at η = k/24, x = 0.3 (nodes, work, time, max T0 gap, result hash).
- **Raw rows:** `output/stage1-hooked.md`, `output/stage2-hooked.md`, `output/four-hooked.md` and the `.json` files
  beside them. **Derived:** `output/summary.md` (`probe/summarize.py`) and `output/timing.md`.

## 2. Results: maximum spans per section

Each figure is the largest s such that every span count from the family's smallest up to s certifies.

### 2.1 Under the as-built limits (node budget, all-query bound 1e6, work 1e9, 1 s)

| Rule | Two sections, 120 mm | Two sections, 2 m | Three sections (Root–Mid–Tip) | Four sections | New foil 10 pt + anchors (2 / 3 sections) | Example 8 pt + anchors (2 / 3 sections) |
|---|---|---|---|---|---|---|
| 256 (as built) | **5** | **5** | **5** | **none** (1, 3 and 5 spans refused) | 0 anchors / 0 anchors | 2 anchors / 2 anchors |
| max(256, ⌈51.2 s⌉) | 5 | 5 | 5 | none | 0 / 0 | 2 / 2 |
| **max(256, 48 s)** | **6** | **6** | **5** | none | **1** / 0 | 2 / 2 |
| max(256, 96 s) | none of 5–16 | none | none | none | none / none | 0 / none (3 spans) |
| 1,000,000 | none | none | none | none | none | none |

A bigger budget certifies **less**, because the all-query bound charges for the budget, not for the use.

### 2.2 Within 1e9 work and 1 s only (all-query bound lifted; admission and every query)

| Rule | Two sections, 120 mm | Two sections, 2 m | Three sections | Four sections | New foil + 2 anchors (7 spans), 2 / 3 sections |
|---|---|---|---|---|---|
| 256 (as built) | 5 (node budget) | 5 | 5 | 5 | refused (node budget) |
| every rule ≥ 48 s | **16 of 16 measured** | **16** | **16** | **6 of 6 measured** | **certified / certified** |

The "5" for four sections under the unbounded rule in `output/summary.md` B comes from one 1.68 s query at load
average 41. A re-timed run gives a minimum of 250 ms for the same fixture (`output/timing.md`).

### 2.3 Measured cost (every hooked row: 260 rows, 52 fixtures)

| Quantity | Measured | Of the limit |
|---|---|---|
| Admission work | ≤ 38.4 M bit-work (three sections, 14 spans) | 3.8 % of 1e9 |
| Query work (one `SectionAt`) | ≤ 30.9 M bit-work | 3.1 % of 1e9 |
| Query nodes actually used (both `Maximum` calls, max) | **17–21, at every span count 1–16** | 256 as built: 12× margin |
| max T0 relative gap (query) | ≤ 1.94 × 10⁻¹² (low and high scale; each call meets 10⁻¹²) | — |
| Query refusals | 0 of 5,106 queries | — |
| Admission atoms (`SubdivisionNodes`, the profile maxima) | recorded per row | — |
| Wall time, re-timed minimum under load | admission ≤ 197 ms, query ≤ 384 ms | < 1 s |

Work does not grow with spans. It follows the knot denominators. The sqrt spacing's knots are k/s: dyadic at
s = 8 and s = 16, and costly at s = 5, 6 and 13. For example, s = 16 costs 4.0 M per query and s = 6 costs 30.5 M.
This is what GSPK §4 inferred about non-dyadic knots, now observed on the as-built path. Chord (120 mm vs 2 m) changes
admission work by < 0.5 % and nothing else.

## 3. Stage 1: can the node budget be a function of input size without weakening soundness? Yes.

### 3.1 The budget never enters the enclosure (Verified: code and measurement)

- `Bernstein.Maximum` (`Geometry.cs:1055-1078`) returns `(lower, upper)` only when
  `upper - lower <= 1e-12 * lower`.
  - `upper` is the largest Bernstein coefficient of any pending node. By the convex-hull property it bounds the
    polynomial on that node.
  - `lower` is an attained value: a span end or a split point.
- `nodeBudget` only decides whether the loop answers or throws `ProofRefusal`. A larger budget does not give a looser
  proof. It is the same proof, allowed to run longer before it gives up.
- **Measured:** the 23 `SectionAt` results are **bit-identical under all five rules** for all 52 fixtures queried
  under two or more rules. Budgets ranged from 256 to 1,000,000 (`output/summary.md` D).

### 3.2 What the budget does carry, and what changes when it scales

- The admission check `spanCount * 48 <= blendNodes` (`:405`) is the as-built argument that a query will not run out
  of nodes. Under `N = 48 s` it holds with equality. That is the same argument applied to any span count, not a
  weaker one.
- The second requirement (`:408`, depth 48 is enough for 10⁻¹²/4) does not depend on spans, and every fixture
  passed it.
- **(Inferred, analysis, not run)** The argument is a one-path-per-span heuristic, not a proof of sufficiency.
  - The `:408` bound and `lower ≥ 1/4` mean that no node at depth 48 is ever split, which bounds depth.
  - Best-first search can still split more than one node per level when two coefficient maxima nearly tie.
  - The measured need, 17–21 nodes, is well inside the budget, because the search follows one thickness maximum
    whatever the span count.
  - If a query did run out, it would refuse with `GEOMETRY-CERTIFICATE-DEFECT`. It would never return a wrong
    enclosure.
  - Scaling with spans leaves this exactly as it is today.
- **The cost of a bigger budget is the declared query cost, not accuracy.** N enters `QueryFeasibility.Prove` as
  `64 N + 6 N (N + 1)` (`:735`). That is why 96 s and the unbounded rule refuse even 5 spans (§2.1).

### 3.3 Timing caveat

Every run shared the machine (load average 27–42). The table in §2.3 shows the re-timed minimum. The `ProofBudget`
calibration (`Geometry.cs:810-817`: about 2.1 ns per unit on a quiet machine) puts the worst query, 30.9 M units, at
**about 65 ms** on a quiet machine. That figure is Inferred: it was not observed on a quiet machine.

## 4. Stage 2: the all-query bound

### 4.1 What it bounds (Verified: code and exact decomposition)

`QueryFeasibility.Prove` (`:678-793`) is an admission-time model. It counts the rational operations of any
`SectionAt` or `PointAt` at any finite binary64 (η, x), and the certificate records the count as
`QueryFeasibility.RationalOperationsUpper`.

```
ops = 10,000 + Σ_curves [ 8·spans_c + 128·(8p(p+1) + 32(p+1) + 64) ] + 64·N + 6·N·(N+1)
```

- The per-curve term is the 128-deep inverse-abscissa bisection: 63,488 per degree-5 curve, independent of spans.
  Each distinct profile adds two curves, so **+127,056 per profile**.
- The blend term models two `Maximum` calls of N nodes each, including rescanning every pending node per node. That
  rescan is the quadratic term named by the `simplify:` note at `:1060-1063`.
- Measured: `ops − 64 N − 6 N (N + 1)` is constant per family plus 32 (two sections) or 48 (three) per span:
  - two sections: 448,432 + 32 s;
  - three sections: 575,488 + 48 s;
  - four sections: 702,416 + 64 s.

  The model is followed exactly (`output/summary.md` C).

With the bound at 1e6, the largest admissible N is about **297** for two profiles (6 spans at 48 s) and about **260**
for three profiles (5 spans). For four profiles there is **none**: N = 256 already gives **1,113,584** at one span.

### 4.2 Finding: four differing station sections are refused as built (Verified, measured)

A wing with four stations whose sections all differ is refused ("All-query operation bound exceeds one million") at
1, 3 and 5 spans per section, before the span count matters. Neither the XPA design nor DR-XPA-1 has named this
limit. Per-section editing at more than three stations (catalog Replace, compatible fit) depends on it.

### 4.3 Can the bound scale with spans the same way? Not linearly. It can be re-modelled or raised.

- **Raising it is sound.** The bound sits outside the enclosure: it declares the worst case a query can cost. The
  runtime guard is the per-query `ProofBudget` (1e9). The two do not compose today. 1e6 operations on operands of up
  to 32,768 bits each can exceed 1e9 bit-work, so the operation bound alone does not prove that a query fits the work
  limit (Inferred, arithmetic).
  - Origin: commit `cce9ee5`. No derivation of 1e6 was found in `docs/design` or `docs/adr`, so it reads as a policy
    constant (Inferred).
- **"The same way" does not work.** Under N = 48 s the blend term grows as 13,824 s². A bound that covers 16 spans
  must be **4.05 M** (two sections, measured 4,041,648) or **4.17 M** (three sections, measured 4,168,880). That is a
  4× weaker cost promise, spent on nodes that the measured queries never use (17–21).

## 5. Recommendation

1. **Node budget: `N(s) = max(256, 48 s)`** (`Geometry.cs:394`).
   - Sound by §3.1: no answer changes, which was measured.
   - It keeps the as-built argument (§3.2) for every span count.
   - Every document that certifies today keeps a bit-identical certificate, operation count included, because
     N = 256 for s ≤ 5.
   - Margin is not needed (the measured need is 17–21), and a margin costs 6 N² in the bound (§2.1, 96 s).
2. **All-query bound: re-derive the blend term and keep the fixed 1e6.** This is the primary option.
   - Replace the rescan in `Bernstein.Maximum` with a max-heap. That is the code's own upgrade trigger: *"the node
     budget rises"* (`:1063`).
   - Charge about `2·N·(32 + 2(p+1) + 3⌈log₂(N+s)⌉)`. That is **~114 k at N = 768**, against 3.59 M today.
   - **Inferred capacity:** 16 spans with up to 5 differing stations, and up to the 32-point ceiling (27 spans) with
     up to 4.
   - **What confirms it:** implement it on a branch and re-run this probe against it. The probe reads the operation
     count through the hooks. Expect possible last-bit differences from heap tie order. Any such enclosure is still
     valid, but it is not bit-equal, so the hash comparison becomes a containment check.
   - **What breaks if it is false:** the capacity figures in this item. Nothing in §2 depends on it.
3. **Measured alternative, if a Core algorithm change is unwanted:** keep the rescan and raise the fixed bound to
   **4.2 M**.
   - Measured: 16 spans for two and three sections.
   - By the exact model (§4.1): four sections up to 15 spans (3.86 M).
   - The cost: the declared per-query worst case grows 4.2×. The measured query cost does not change.

## 6. What it means for the user

Points per section: with simple knots, **points = spans + 5**. Each anchor adds 5 points and 1 span. The New foil is
10 points and 5 spans. The Example is 8 points and 3 spans.

| Change | Sections that may differ between stations |
|---|---|
| **As built** | Two or three stations: up to **10 points (5 spans)** each. So a New-foil section with **no** anchor, or an Example section with up to 2 anchors. **Four or more differing stations: never**, at any point count (§4.2) |
| N = 48 s only | Two stations: up to **11 points / 6 spans** (New foil + 1 anchor). Three stations: unchanged (5 spans) |
| N = 48 s + bound 4.2 M (measured) | Two or three stations: up to **21 points (16 spans)**, e.g. New foil + up to 4 anchors (30 points, 9 spans) within the 32-point ceiling. Four stations: up to 15 spans |
| N = 48 s + heap model, 1e6 kept (Inferred) | Up to the 32-point ceiling with up to 4 differing stations; 16 spans with up to 5 |

For the compatible-fit design (`design-cross-profile-abscissa`), the shared spacing S\* gathers every station's
anchors. So "spans per section" means spans of the **shared** spacing, which is the binding count. Catalog Replace
(`design-m12d-catalog`) inherits the four-station wall in §4.2 under every rule.

## 7. Durations (the new prior)

| Stage | Box | Measured |
|---|---|---|
| Stage 1: grounding, patch script, probe from scratch, 120 hooked and 24 as-built rows, neutrality | 60 min | **11 min** |
| Stage 2: three-section chains, New foil / Example anchors, the four-station case, op decomposition, re-timing | 60 min | **13 min** |
| Verdict, gates and commit | — | recorded in the report |

No repair cycle was needed. One neutrality comparison was corrected: it now compares query columns only when the
as-built row certified, because the hooked copy's query columns for refused rows come from a lifted-bound run that the
as-built Core cannot make.

## 8. Reproduce

```sh
cd docs/proof/blend-certificate-budget
python3 probe/patch-core.py ../../.. <scratch>/core-hooked/CfdWorkbench.Core
dotnet build -c Release probe -p:CoreProject=<scratch>/core-hooked/CfdWorkbench.Core/CfdWorkbench.Core.csproj -o <scratch>/bin-hooked
dotnet build -c Release probe -o <scratch>/bin-asbuilt
for m in stage1 stage2 four; do dotnet <scratch>/bin-hooked/CfdWorkbench.Core.Tests.dll $m .; dotnet <scratch>/bin-asbuilt/CfdWorkbench.Core.Tests.dll $m .; python3 probe/neutrality.py . $m; done
dotnet <scratch>/bin-hooked/CfdWorkbench.Core.Tests.dll timing .
python3 probe/summarize.py .
```

The hooked stage 1 and stage 2 runs take about 4–5 min each under load. Everything except the millisecond columns is
deterministic.

## 9. Residual risk

- **Inferred:** the heap capacity in §5.2 is a model, not a run. The quiet-machine query time (about 65 ms) comes from
  the `ProofBudget` calibration, not from an observation.
- **Not covered:**
  - η sampling is 23 points at one x. The node count depends only on the blend weight, not on x, but a near-tie weight
    between the samples is not excluded.
  - Shapes are NACA 0012, `section-a` and two scaled NACA variants. A twin-peak thickness distribution, the
    adversarial case for §3.2, was not built.
  - Spans above 16 were not run, and neither were open trailing edges.
  - All timings come from one Mac under load.
- **Fixture residuals** (fit onto the sqrt spacing, ≤ 3.0 × 10⁻⁴ chord) are irrelevant here. The fixtures are valid
  differing shapes, not accuracy claims.
