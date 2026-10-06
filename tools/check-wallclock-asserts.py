#!/usr/bin/env python3
"""WALLCLOCK-ASSERT-UNGATED control: a test that fails on a wall-clock budget must be load-gated.

A check that throws when elapsed time passes a literal budget fails on a loaded machine for a reason that says nothing
about the product (Rulings 81, 84: the frame budgets use RequireFrameBudget or print READINESS-MISS above load 24).
This lint finds, in tests/**/*.cs, a comparison of a timing value (Elapsed, TotalMilliseconds, TotalSeconds,
GetElapsedTime, or a variable assigned from one) with a number literal, where a throw or Fail follows within two lines,
and no load gate (RequireFrameBudget, ReadinessLoadGate, READINESS-MISS, LoadAverage1) sits within eight lines before.
A deadline that throws TimeoutException is a hang guard, not a budget, and is not flagged.
Each instance of 2026-10-06 is in the allowlist below as tracked debt (key: file + stripped line, never a line number).
A new ungated assertion exits 1; a stale allowlist entry also exits 1, so the debt list only shrinks. `--self-test`
plants one ungated and one gated shape.
"""

import re
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
TIMING = re.compile(r"\.Elapsed\b|TotalMilliseconds|TotalSeconds|GetElapsedTime")
ASSIGNED = re.compile(r"\b(\w+)\s*=\s*[^=].*(?:\.Elapsed\b|TotalMilliseconds|TotalSeconds|GetElapsedTime)")
LITERAL_COMPARE = re.compile(r"[<>]=?\s*-?\d[\d_.]*[fdm]?\b|\b\d[\d_.]*[fdm]?\s*[<>]=?")
THROWS = re.compile(r"\bthrow\b|\bFail\(|Exception\(")
GATE = re.compile(r"RequireFrameBudget|ReadinessLoadGate|READINESS-MISS|LoadAverage1|FrameBudgetVerdict")

# Tracked debt: (file relative to tests/, stripped source line). Fixing these is a later track; each needs the Ruling 81
# gate (RequireFrameBudget) or a READINESS-MISS path, then its entry is deleted here.
ALLOWLIST = {
    ('CfdWorkbench.Analysis.Tests/SectionSeamTests.cs', 'if (watch.Elapsed.TotalMilliseconds > 1000) throw new InvalidOperationException("warm 200-panel whole-wing section tier exceeded 1 s");'),
    ('CfdWorkbench.Desktop.Tests/AnalysisToggleTests.cs', 'if (p95 > 250) throw new Exception($"Toggle p95 {p95:F3} ms exceeds 250 ms.");'),
    ('CfdWorkbench.Desktop.Tests/PlanCanvasTests.cs', 'if (median > 8) throw new Exception(FormattableString.Invariant($"Plan render median {median:F2} ms is over its 8 ms budget"));'),
}


def findings(text):
    """[(line number, stripped line)] for each ungated wall-clock assertion in one file's text."""
    lines = text.splitlines()
    timed = {"median", "p95", "p99", "elapsedMs"}   # names that carry a duration even when a helper computed it
    found = []
    for number, line in enumerate(lines):
        code = line.split("//")[0]
        match = ASSIGNED.search(code)
        if match:
            timed.add(match.group(1))
        helper = re.search(r"\b(\w+)\s*=\s*[\w.]*\w(?<!From)(?<!FromSeconds)(?:Milliseconds|Ms|Seconds)\(", code)
        if helper:
            timed.add(helper.group(1))
        added = re.search(r"\b(\w+)\.Add\(.*(?:\.Elapsed\b|TotalMilliseconds|TotalSeconds|GetElapsedTime)", code)
        if added:
            timed.add(added.group(1))
        derived = re.match(r"\s*(?:var|double|long|int)?\s*(\w+)\s*=\s*[^=]", code)
        if derived and any(re.search(r"\b" + re.escape(name) + r"\b", code.split("=", 1)[1]) for name in timed):
            timed.add(derived.group(1))
        names = TIMING.search(code) or any(re.search(r"\b" + re.escape(name) + r"\b", code) for name in timed)
        if not names or not LITERAL_COMPARE.search(code) or "TimeoutException" in code:
            continue
        if not re.search(r"\bif\b|\bwhile\b|\bAssert|Require|>|<", code):
            continue
        window = "\n".join(lines[number:number + 3])
        if not THROWS.search(window) or "TimeoutException" in window or "WriteLine" in code:
            continue
        if GATE.search("\n".join(lines[max(0, number - 8):number + 3])):
            continue
        found.append((number + 1, line.strip()))
    return found


def scan(root):
    results = []
    for path in sorted((root / "tests").rglob("*.cs")):
        if any(part in ("bin", "obj") for part in path.parts):
            continue
        relative = path.relative_to(root / "tests").as_posix()
        for number, line in findings(path.read_text(encoding="utf-8", errors="replace")):
            results.append((relative, number, line))
    return results


def problems(results, allowlist):
    found = []
    keys = {(relative, line) for relative, _, line in results}
    for relative, number, line in results:
        if (relative, line) not in allowlist:
            found.append(f"WALLCLOCK-ASSERT-UNGATED: tests/{relative}:{number}: {line}")
    for relative, line in sorted(allowlist - keys):
        found.append(f"WALLCLOCK-ALLOWLIST-STALE: {relative}: {line} (fixed or moved; delete the entry)")
    return found


def self_test():
    ungated = "var w = Stopwatch.StartNew();\nRun();\nif (w.Elapsed.TotalMilliseconds > 1000) throw new InvalidOperationException(\"slow\");\n"
    assert len(findings(ungated)) == 1, findings(ungated)
    via_variable = "double ms = clock.Elapsed.TotalMilliseconds;\nif (ms > 50)\n    throw new Exception(\"x\");\n"
    assert len(findings(via_variable)) == 1, findings(via_variable)
    gated = "double ms = clock.Elapsed.TotalMilliseconds;\nDesktopChecks.RequireFrameBudget(\"X\", ms, 33, load);\nif (ms > 50) throw new Exception(\"x\");\n"
    assert findings(gated) == [], findings(gated)
    missed = "double ms = clock.Elapsed.TotalMilliseconds;\nif (ms >= 5) Console.WriteLine(\"READINESS-MISS X\");\n"
    assert findings(missed) == [], findings(missed)
    hang_guard = "if (deadline.Elapsed.TotalSeconds > 30) throw new TimeoutException(\"No mesh\");\n"
    assert findings(hang_guard) == [], findings(hang_guard)
    measure_only = "Console.WriteLine($\"MEASURE {w.Elapsed.TotalMilliseconds:F3} ms\");\n"
    assert findings(measure_only) == [], findings(measure_only)
    results = [("A.cs", 3, "if (w.Elapsed.TotalMilliseconds > 1000) throw x;")]
    assert len(problems(results, set())) == 1
    assert problems(results, {("A.cs", results[0][2])}) == []
    assert len(problems([], {("A.cs", "gone")})) == 1
    print("check-wallclock-asserts self-test OK", flush=True)


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    results = scan(ROOT)
    if "--list" in sys.argv[1:]:
        for relative, number, line in results:
            print(f"tests/{relative}:{number}: {line}")
        return 0
    found = problems(results, ALLOWLIST)
    for line in found:
        print(line)
    if found:
        print("Gate the assertion with DesktopChecks.RequireFrameBudget (Ruling 81) or print READINESS-MISS above load 24.",
              file=sys.stderr)
        return 1
    print(f"wall-clock assertions ok: {len(results)} ungated, all allowlisted as tracked debt", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
