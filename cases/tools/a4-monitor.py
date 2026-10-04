#!/usr/bin/env python3
"""A4 iterative-convergence criterion (DR-F2-1, Ruling 65): watch a running solve and stop it, or report a finished one.

Run: python3 cases/tools/a4-monitor.py watch  <run-dir> <solver> <case.yaml>   (polls; sends SIGUSR1 when A4 holds)
     python3 cases/tools/a4-monitor.py report <run-dir> <solver> <case.yaml>   (final A4 verdict and window values)
A4 (plan docs/plans/fluids-round2.md §2.2, stated before any round-2 run; parameters read from numerics.a4):
  1. residual drop: for every equation, max(initial residual over the last 2 iterations) <= 10^-orders x its
     iteration-1 initial residual (the max over 2 iterations keeps a period-2 cycle from passing on its low phase);
  2. stationarity over the last W iterations: U_I = (max - min)/2 <= cap, for Cl and for Cd;
  3. clean window: zero "bounding nuTilda" lines inside W;
  4. watch mode evaluates at every multiple of poll_every_iterations; when 1-3 hold it sends SIGUSR1 to the solver
     ranks of this run (the bundled controlDict sets stopAtWriteNowSignal 30 = SIGUSR1 on macOS).
The reported value is the window mean. Reads postProcessing/solverInfo1/0/solverInfo.dat,
postProcessing/forceCoeffs1/0/coefficient.dat and log.<solver>.
"""
import os
import re
import signal
import subprocess
import sys
import time

import yaml

mode, run, solver, case_yaml = sys.argv[1:5]
a4 = yaml.safe_load(open(case_yaml))["numerics"]["a4"]
W = int(a4["window_iterations"])
orders = float(a4["residual_drop_orders"])
caps = {"Cl": float(a4["cl_half_band_cap"]), "Cd": float(a4["cd_half_band_cap"])}
poll = int(a4["poll_every_iterations"])


def table(rel):
    path = os.path.join(run, "postProcessing", rel)
    header, rows = None, []
    if not os.path.exists(path):
        return None, []
    with open(path) as f:
        for line in f:
            if line.startswith("#"):
                if "Time" in line:
                    header = line[1:].split()
                continue
            parts = line.split()
            if parts and header and len(parts) == len(header):
                rows.append(parts)
    return header, rows


def bounding_iterations(lo):
    """Iterations >= lo whose block in the solver log holds a 'bounding nuTilda' line."""
    hits, it = set(), None
    log = os.path.join(run, f"log.{solver}")
    with open(log, errors="replace") as f:
        for line in f:
            if line.startswith("Time = "):
                try:
                    it = int(float(line.split()[2]))
                except ValueError:
                    it = None
            elif "bounding nuTilda" in line and it is not None and it >= lo:
                hits.add(it)
    return hits


def evaluate():
    hs, rs = table("solverInfo1/0/solverInfo.dat")
    hc, rc = table("forceCoeffs1/0/coefficient.dat")
    n = min(len(rs), len(rc))
    if n < W or n < 2:
        return {"iterations": n, "ok": False, "why": f"fewer than W={W} iterations"}
    out = {"iterations": n}
    cols = [i for i, h in enumerate(hs) if h.endswith("_initial") and not h.startswith("Uz")]
    drops, ok1 = {}, True
    for i in cols:
        first = float(rs[0][i])
        last2 = max(float(rs[n - 1][i]), float(rs[n - 2][i]))
        ratio = last2 / first if first > 0 else float("inf")
        drops[hs[i].replace("_initial", "")] = ratio
        ok1 &= ratio <= 10 ** -orders
    out["residual_ratio_last2_over_first"] = drops
    win = rc[n - W:n]
    stats, ok2 = {}, True
    for q in ("Cl", "Cd"):
        j = hc.index(q)
        v = [float(r[j]) for r in win]
        ui = 0.5 * (max(v) - min(v))
        stats[q] = {"mean": sum(v) / len(v), "U_I": ui, "cap": caps[q], "last": v[-1]}
        ok2 &= ui <= caps[q]
    out["window"] = {"from": int(float(win[0][0])), "to": int(float(win[-1][0])), **stats}
    first_it = int(float(win[0][0]))
    b = bounding_iterations(first_it)
    out["bounding_iterations_in_window"] = len(b)
    ok3 = len(b) == 0
    out["clauses"] = {"1_residual_drop": ok1, "2_stationarity": ok2, "3_clean_window": ok3}
    out["ok"] = ok1 and ok2 and ok3
    return out


def fmt(r):
    lines = [f"iterations={r['iterations']} A4={'MET' if r['ok'] else 'NOT MET'}"]
    if "clauses" in r:
        lines.append("clauses " + " ".join(f"{k}={'pass' if v else 'fail'}" for k, v in r["clauses"].items()))
        lines.append("residual_ratio_last2_over_first " + " ".join(f"{k}={v:.3e}" for k, v in r["residual_ratio_last2_over_first"].items()))
        w = r["window"]
        lines.append(f"window iterations {w['from']}..{w['to']} (W={W})")
        for q in ("Cl", "Cd"):
            lines.append(f"{q}_window_mean={w[q]['mean']:.9f} {q}_U_I={w[q]['U_I']:.3e} cap={w[q]['cap']:.1e} {q}_last={w[q]['last']:.9f}")
        lines.append(f"bounding_nuTilda_iterations_in_window={r['bounding_iterations_in_window']}")
    else:
        lines.append(r.get("why", ""))
    return "\n".join(lines)


if mode == "report":
    r = evaluate()
    print(fmt(r))
    stop = os.path.join(run, "a4-stop.txt")
    print("stopped_by_A4=" + (open(stop).read().strip() if os.path.exists(stop) else "no (ran to max_iterations or failed)"))
    sys.exit(0)

# Round 3 (plan docs/plans/fluids-round3.md §3.3): optional numerics.a4.clause5_extension. Once A4 holds, the same
# run continues (no setting changed) until clause 5 holds against the coarser grid of its pair, U_I <= ratio x |mean -
# coarser mean| with the coarser U_I included, and any absolute target caps hold (the middle grid, whose finer
# neighbour is not run yet), or until extend_cap_iterations past the first A4 iteration.
c5 = a4.get("clause5_extension")
a4_first = None


def clause5(r):
    if not c5:
        return True, ""
    ok, parts = True, []
    w = r["window"]
    ref = c5.get("coarser")
    for q in ("Cl", "Cd"):
        if ref:
            eps = abs(w[q]["mean"] - float(ref[q.lower()]))
            bound = float(a4["gci_admission_ratio"]) * eps
            ui = max(w[q]["U_I"], float(ref[f"{q.lower()}_U_I"]))
            ok &= ui <= bound
            parts.append(f"{q} max U_I {ui:.2e} vs {bound:.2e} (|eps| {eps:.3e})")
        tgt = (c5.get("target_caps") or {}).get(q.lower())
        if tgt is not None:
            ok &= w[q]["U_I"] <= float(tgt)
            parts.append(f"{q} U_I {w[q]['U_I']:.2e} vs target {float(tgt):.1e}")
    return ok, "; ".join(parts)


# watch
abs_run = os.path.abspath(run)
pattern = rf"^([^ ]*/)?{solver} -parallel -case {re.escape(abs_run)}$"
next_check = poll
started = time.time()
while True:
    time.sleep(5)
    pids = subprocess.run(["pgrep", "-f", pattern], capture_output=True, text=True).stdout.split()
    hs, rs = table("solverInfo1/0/solverInfo.dat")
    n = len(rs)
    if not pids and time.time() - started > 120:
        print(f"a4-monitor: solver gone at {n} iterations; exiting", flush=True)
        break
    if n >= next_check:
        next_check = (n // poll + 1) * poll
        r = evaluate()
        print(f"a4-monitor: {time.strftime('%H:%M:%S')} " + fmt(r).replace("\n", " | "), flush=True)
        if r["ok"] and a4_first is None:
            a4_first = n
            print(f"a4-monitor: A4 first met at {n}", flush=True)
        why = None
        if a4_first is not None:
            c5_ok, c5_text = clause5(r)
            if c5_ok:
                why = f"A4 met at iteration {a4_first}; clause-5 extension: {c5_text} at {n}" if c5 else f"A4 met at iteration {n}"
            elif n >= a4_first + c5["extend_cap_iterations"]:
                why = f"A4 met at iteration {a4_first}; clause-5 extension cap +{c5['extend_cap_iterations']} reached at {n} without it ({c5_text})"
            else:
                print(f"a4-monitor: extending ({c5_text})", flush=True)
        if why and pids:
            with open(os.path.join(run, "a4-stop.txt"), "w") as f:
                f.write(f"{why}; SIGUSR1 sent to pids {' '.join(pids)} at {time.strftime('%Y-%m-%dT%H:%M:%S%z')}\n")
            for p in pids:
                os.kill(int(p), signal.SIGUSR1)
            print(f"a4-monitor: {why}; SIGUSR1 -> {pids}", flush=True)
            break
