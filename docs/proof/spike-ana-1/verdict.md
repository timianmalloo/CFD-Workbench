---
id: proof-spike-ana-1
title: "SPIKE-ANA-1: in-process NeuralFoil port fidelity, XFOIL accuracy, CST fit and licence facts"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: design
tags: [analysis, neuralfoil, xfoil, cst, kulfan, polar, licence, spike, dr-ana-1]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: spec-cfd-workbench-v1, rel: relates-to }
  - { to: coordination-round-oct05, rel: relates-to }
review-by: 2026-11-30
summary: >-
  GO on C# port fidelity: 198 outputs across 90 shared CST cases differ from Python NeuralFoil by at most 3.91e-14 absolute.
  GO on the measured NACA 0012 accuracy grid: 78 of 78 local XFOIL 6.99 points meet both Cl and ln Cd limits.
  The package declares MIT and includes the weight files, but the weights have no separate notice in the installed wheel; distribution awaits security and operator licence review.
review-suggested: []
---

# SPIKE-ANA-1 verdict

**Result: GO for port fidelity; GO for model accuracy on the measured NACA 0012 range; GO WITH CONDITIONS for licence facts.** This is a spike result for DR-ANA-1(b), not a production or shipping approval. The comparison oracle is **local XFOIL 6.99**, built from the XFOIL 6.99 source archive hosted at [web.mit.edu](https://web.mit.edu/drela/Public/web/xfoil/) (XFOIL is GPL-licensed; only the host is MIT) in this proof directory. No published-polar fallback was used.

| Decision | Measured result | Verdict |
|---|---|---|
| (a) Port fidelity | C# versus Python NeuralFoil 0.3.2 `xxxlarge`: 90 identical CST cases, 198 outputs/case, maximum absolute difference **3.90798505e-14**; target about 1e-6 | **GO** |
| (b) Model accuracy | NACA 0012, Re 2e5 and 1e6, Ncrit 2/4/9, α −6°…+6° by 1°: **78/78 points (about 42 distinct pre-stall conditions; in-distribution; Cl and Cd only — Cm, x_tr and boundary-layer outputs not compared)** meet both `|ΔCl| ≤ 0.02` and `|Δln Cd| ≤ 0.03`. Worst `|ΔCl| = 0.00796145`; worst `|Δln Cd| = 0.01283264` | **GO for this bracket** |
| (c) Licence facts | The installed NeuralFoil wheel declares MIT and includes the weight `.npz` files in its `RECORD`. Its MIT `LICENSE.txt` is copied here. No separate notice for the weight files was found in that wheel. | **GO WITH CONDITIONS**: security review and operator ruling on the converted weights and ship route remain required. |

## Reproduce

From `/Users/mallalieut/projects/CFD-Workbench-spike-ana-1`, run:

```sh
sh docs/proof/spike-ana-1/rerun.sh
```

The script uses `/Users/mallalieut/dev/sim/.venv/bin/python` directly; it pins NeuralFoil 0.3.2, AeroSandbox 4.2.9, and both source-file hashes. It builds the official XFOIL 6.99 archive with the existing `gfortran`/X11 toolchain within `xfoil/`, without a system install. It generates the 90 Python cases, converts weights, builds and runs the isolated .NET 10 console probe, compares all outputs, reruns all six XFOIL polars, compares accuracy, measures timing, and runs `python3 tools/check-docs.py`. `xfoil/build.sh` records the source archive SHA-256 and fails if it changes. No product solution or test ring is used.

| Claim | Reproducing command in `rerun.sh` | Committed evidence |
|---|---|---|
| Weight conversion and Python predictions | `OPENBLAS_NUM_THREADS=1 …/bin/python docs/proof/spike-ana-1/reference.py` | `weights-manifest.json`, `probe/weights.bin`, `cases.tsv`, `python.tsv`, `cst-residual.tsv` |
| C# fixed oracle, all-case predictions and speed | `dotnet run -c Release --project …/probe.csproj -- --selftest`; `--cases …/cases.tsv …/csharp.tsv`; `--benchmark 300` | `red-first.md`, `csharp.tsv`, `timing.txt` |
| All 198 output differences | `…/bin/python docs/proof/spike-ana-1/compare_fidelity.py` | `fidelity.tsv` (one row per output with maximum absolute and relative difference and worst cases) |
| Local XFOIL polars | `sh docs/proof/spike-ana-1/xfoil/build.sh`; `…/bin/python docs/proof/spike-ana-1/run_xfoil.py` | `data/polar-re*-n*.txt` (six XFOIL outputs), `xfoil/run-*.in` and logs on rerun |
| All accuracy points and failures | `…/bin/python docs/proof/spike-ana-1/compare_accuracy.py` | `accuracy.tsv`, `accuracy-summary.tsv` |

`rerun.sh` is the executable command record; `reference.py`, `run_xfoil.py` and the two comparison scripts define the datasets and calculations. The external source tarball and binary are ignored by git and regenerated. The committed polar copies preserve XFOIL's values and header, with only Fortran-padded trailing spaces removed. The official archive SHA-256 is `5c0250643f52ce0e75d7338ae2504ce7907f2d49a30f921826717b8ac12ebe40`; this machine's built arm64 executable SHA-256 is `b97f92f761483d9c933d763ee8d84274ab46819b36ec4ccea39ebc08291f03a9`.

## Inference path and model identity

**Observed in the installed `neuralfoil/main.py` and weight arrays.** NeuralFoil's lower-level `get_aero_from_kulfan_parameters` accepts eight upper and eight lower Kulfan/CST weights, one leading-edge modification weight, trailing-edge thickness, α, Re, Ncrit, and upper/lower forced-transition positions. It builds 25 inputs: 16 shape weights; LE weight; 50 × TE thickness; sin(2α), cos(α), 1−cos²(α); `(ln Re−12.5)/3.5`; `(Ncrit−9)/4.5`; and two xtr positions. The probe uses natural transition, xtr=1 on each side, as does XFOIL.

The selected **`xxxlarge`** model is the largest of the eight installed variants (`xxsmall`, `xsmall`, `small`, `medium`, `large`, `xlarge`, `xxlarge`, `xxxlarge`). It is selected to test the strongest available accuracy case under DR-ANA-1's tolerance; smaller-model speed/accuracy is not inferred. The actual `.npz` has seven affine matrices, `25→512→512→512→512→512→512→198`, and float32 parameters. The installed `main.py` applies **swish** `x/(1+exp(−x))` between matrices, then evaluates a reflected input, flips the affected lift, moment, transition and boundary-layer outputs back, and averages. It subtracts squared Mahalanobis distance divided by 50 from each pass's raw confidence. It then applies sigmoid to confidence; scales Cl by 1/2 and Cm by 1/20; computes Cd as `exp(2(y−2))`; clips transition locations to [0,1]; and decodes 32 values per side each for momentum thickness, shape factor and edge-velocity ratio. `neuralfoil/main.py` is the executable source for this port. The installed package description says tanh in an architecture paragraph, which conflicts with this version's `np.swish` call; the C# port follows the observed call.

`reference.py` exports row-major float32 matrices and biases plus the float32 input mean and float64 inverse covariance into `probe/weights.bin` using the format in `weights-manifest.json`; the C# loader checks its header, dimensions and full length. Source `nn-xxxlarge.npz`: **5,356,191 bytes**, SHA-256 `94638c04bca3c303515cf0c2944e2b09f587818cbd2860183bd85f2fbeeef428`; source distribution `.npz`: **7,696 bytes**, SHA-256 `63a33149c902ad01ecf537dd2d127d9e7ffbf86527893f4dc76f25f7087a3573`. Converted on-disk resource: **5,717,832 bytes** (5.45 MiB), SHA-256 `e037bd2b92d5a964ffcda33ce0ea39af9d8807bcbb07fe182fb5ec1f1815c689`. A candidate method-version component is `NeuralFoil-0.3.2/xxxlarge/94638c04` **with `weights=5717832B` recorded in the version or manifest**; product naming is left to A3c.

## Port fidelity and CST fit

The shared CST grid is NACA 0012, 2412 and 4412 × α {−6, −3, 0, +3, +6} degrees × Re {2e5, 1e6} × Ncrit {2, 4, 9}. `cases.tsv` carries the same full-precision CST coefficients to Python and C#; Python calls `get_aero_from_kulfan_parameters`, so this test isolates inference from fitting. `fidelity.tsv` gives both maximum absolute and relative error **for each of the 198 outputs**. Relative error uses `max(|Python|, 1e-12)` as denominator and is unstable at exact physical zero.

| Output | Max absolute C#−Python | Max relative, floor 1e-12 |
|---|---:|---:|
| analysis confidence | 2.44e-15 | 2.55e-15 |
| Cl | 2.89e-15 | 2.22e-4 (Python Cl ≈ 0 at α=0; absolute error ≈ 2.22e-16) |
| Cd | 2.60e-17 | 2.70e-15 |
| Cm | 4.72e-16 | 6.66e-5 (Python Cm ≈ 0 at α=0) |
| Top / bottom xtr | 1.55e-15 / 1.89e-15 | 5.63e-15 / 1.71e-14 |
| 192 boundary-layer outputs | 3.91e-14 | 1.37e-14 |

The fit uses AeroSandbox's eight-weight-per-side CST with leading-edge modification on each normalized NACA section. Residuals compare reconstructed upper and lower ordinate with the original foil on 200 cosine-spaced x positions per side (400 ordinates). Values are chord fractions:

| Foil | RMS `y/c` | Max absolute `y/c` |
|---|---:|---:|
| NACA 0012 | 2.93e-5 | 1.09e-4 |
| NACA 2412 | 3.82e-5 | 2.36e-4 |
| NACA 4412 | 5.52e-5 | 3.58e-4 |

These are shape residuals, not aerodynamic errors. They quantify one source of difference between NeuralFoil on fitted CST and XFOIL on its built-in NACA geometry.

## Accuracy against local XFOIL

The XFOIL 6.99 archive hosted at [web.mit.edu](https://web.mit.edu/drela/Public/web/xfoil/) (GPL) was downloaded and built inside `xfoil/` with `gfortran` and the existing Homebrew X11 files. The linker produced a runnable Mach-O arm64 `XFOIL Version 6.99` binary. The upstream Makefile's `install -s` failed on macOS after linking; `build.sh` uses `cp` only within this proof directory. The first `PACC` incremental-save route hit a gfortran sequential-I/O error; the final run accumulates without a file and writes once using `PWRT`. All six final runs exited 0 and yielded 13 polar rows each. The oracle's per-point convergence relies on the 13-rows-per-polar assert, because `PACC` stores only converged points. The run uses XFOIL's NACA 0012 geometry, `PANE` (160 nodes), Mach 0, Re and Ncrit as below, natural transitions (`xtrf=1`), viscous solution, `ITER 200`, and `ASEQ -6 6 1`. This is the tested pre-stall α bracket: each polar has all 13 points and monotonic Cl; this does not establish a wider attached-flow envelope or experimental accuracy.

Errors are NeuralFoil minus XFOIL; the drag criterion uses `ln(Cd_NF/Cd_XFOIL)`. Positive/negative α share the symmetric NACA section. Full point-by-point results, including pass flags, are in `accuracy.tsv`.

| Re | Ncrit | Cl pass | ln Cd pass | Worst signed Cl error at α | Worst signed ln Cd error at α |
|---:|---:|---:|---:|---:|---:|
| 2e5 | 2 | 13/13 | 13/13 | +0.001445 at −4° | +0.009118 at +3° |
| 2e5 | 4 | 13/13 | 13/13 | −0.001834 at +2° | +0.011922 at 0° |
| 2e5 | 9 | 13/13 | 13/13 | +0.007961 at +3° | +0.012833 at +3° |
| 1e6 | 2 | 13/13 | 13/13 | −0.000760 at +3° | +0.006598 at +3° |
| 1e6 | 4 | 13/13 | 13/13 | −0.001913 at +5° | +0.011135 at +4° |
| 1e6 | 9 | 13/13 | 13/13 | −0.004678 at −6° | +0.007858 at +6° |

No point failed either tolerance. NACA 0012 is symmetric, so ±α are mirror points and the 78 points hold about 42 distinct conditions. Every worst signed ln Cd error is positive: NeuralFoil systematically over-predicts Cd by about 1 % against XFOIL on this bracket. This is accuracy **relative to a local XFOIL process**, not an experiment and not evidence for other foils, Reynolds numbers or stalled flow.

## Timing and licence record

Release .NET 10.0.203 on macOS arm64, one already-loaded model, 20 warm-up calls, then 300 serial calls: `timing.txt` records **570.346 ms total, 1.90115 ms/call** on this shared machine. Each call performs both symmetry passes and all 198 outputs. This is a measurement of the throwaway console probe; it is not a UI latency guarantee. Re-runs may vary with machine load.

The installed `neuralfoil-0.3.2.dist-info/METADATA` states `Classifier: License :: OSI Approved :: MIT License` and `License-File: LICENSE.txt`; its §License says NeuralFoil is licensed under MIT. Its `licenses/LICENSE.txt` begins `MIT License`, names **Copyright (c) 2023-2023 Peter Sharpe**, and requires the copyright and permission notice in copies or substantial portions. The copied file here has SHA-256 `f3a3857f0bfab1733bcea48be8b6f1ad2c43176f855362cdd6c334a360a93450`. The same wheel's `RECORD` lists this licence file, the eight `nn-*.npz` weight files and `scaled_input_distribution.npz`. No separate licence file or note was found inside `neuralfoil/nn_weights_and_biases/`, `main.py` or installed metadata that names different terms specifically for weights. The [upstream project README](https://github.com/peterdsharpe/NeuralFoil) likewise states MIT for NeuralFoil generally. These are package and file facts; interpreting rights to redistribute converted weights is for the security reviewer and operator.

The design's A8.5 warns that a Python NeuralFoil sidecar brings CasADi (LGPL-3.0) and IPOPT (EPL-2.0) into a separate review. This spike's C# probe has no Python runtime or subprocess dependency in its inference path. XFOIL is a local comparison process only, consistent with §5.5; the XFOIL site at [web.mit.edu](https://web.mit.edu/drela/Public/web/xfoil/) states it is GPL, and neither its binary nor its source is a product resource. The product path remains blocked on the licence ruling specified by the round-oct05 D2 contract.

## What this GO does not cover

- Post-stall and near-stall α (tested α is −6° to +6° only).
- Re below 2e5 or above 1e6; hydrofoil Re is usually above 1e6.
- Cambered or thick sections in accuracy; NACA 2412 and 4412 are in the fidelity grid only.
- Real edited sections: the CST-fit residual was measured on NACA 00xx/24xx/44xx only, max 3.6e-4 c.
- Water effects (free surface, cavitation, ventilation).
- Experiment: the result is relative to XFOIL, not to measured data.
- Windows: fidelity was measured on arm64 macOS only.

## Conditions for A3c

1. Store and gate on `analysis_confidence` (advisory, not an error bar).
2. An envelope guard on α, Re, Ncrit, section family and CST-fit residual marks out-of-range points non-computable, never silently dropped, and shows the CST residual for every edited section.
3. Tier label "surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only" (COMMIT-01), with re-evaluation at a higher tier before any candidate is promoted.
4. Method id and version in every run key: `NeuralFoil-0.3.2/xxxlarge/<source sha256 prefix>`.
5. A ring-0 fidelity fixture with per-platform tolerance and provenance.
6. Extend XFOIL accuracy to a cambered foil and a Re band above 1e6 before widening any claim.
7. Weights ship only after the operator's licence ruling, and then with:
   - THIRD-PARTY-NOTICES holding the full MIT text and copyright, NeuralFoil 0.3.2 xxxlarge, the upstream URL and a derived-from-XFOIL-data note, linked from About;
   - the wheel SHA-256 recorded and verified in a build step;
   - the weights hash checked at load;
   - a gate that fails the build if the notice or a hash is missing;
   - no Python, CasADi or IPOPT in the shipped path;
   - a gate that fails if any `xfoil/**` path other than `.gitignore` and `build.sh` is tracked.

## Evidence limits and next decision

**Verified:** all committed output tables were generated by the commands above; C# port matches Python on the shared CST grid; the local XFOIL polars and their metadata were read; the package metadata, licence and wheel record were inspected. **Inferred:** a version scheme containing package version, model size and weight hash will prevent silent model replacement; A3c must decide its exact durable version field. **Flagged:** weight redistribution terms need independent review; the method's accuracy outside this NACA 0012 pre-stall bracket, on water-specific dirty/tripped surfaces, and across platforms is unmeasured. Fan-out was zero, so no independent pre-merge reviewer cleared the probe; its external oracles and rerun gate are the proof here. The probe does not implement the product's profile drag, Find α, Cp, UI or persistence.

One probe-script defect was found while building the evidence: an initial call to `numpy.cosspace` failed because that symbol is in AeroSandbox's NumPy wrapper, not NumPy itself. The class is **assuming a wrapper API is in its base package**. `reference.py` now uses an explicit cosine grid; the rerun script invokes that path and fails before any verdict table if it regresses. The initial XFOIL incremental polar save also failed under this toolchain; the final `PWRT` path is checked for 13 rows in each of six files. These are local probe controls; the repository-wide defect-class register is outside this track's ownership.

Next: the security-identity-architect reviews the copied licence and weight provenance, then the operator rules on whether A3c may ship this weight resource. A3c should only start after that ruling and this spike's GO is accepted.

| Status | Result |
|---|---|
| Completed | Isolated C# port, red-first oracle, 90-case/198-output fidelity grid, three CST-fit residuals, six local XFOIL polars, 78-point accuracy comparison, weight/licence evidence, timing and rerun script. |
| Remaining | Security and operator licence ruling; production A3c and any wider polar envelope validation. |
| Best next action | Review the weight licence facts and rule on DR-ANA-1(b)'s ship route. |
