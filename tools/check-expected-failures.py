#!/usr/bin/env python3
"""Windows ring: classify a harness log's FAIL lines against the known-expected-failure manifest.

Rulings 152 (2) and 154 (3): until W-2 B2 lands, the historical probe and the fail-closed Windows store tests fail on a
Windows host. They are listed in tests/expected-failures.windows.json, each keyed to its ruling, so the ring's exit
reflects only unexpected failures.

A FAIL is expected only when its test name AND the entry's fragment both match. The fragment is searched in the FAIL
line and the continuation lines under it (lines that start with no harness token), so a crash trace counts.
A listed test that PASSES is UNEXPECTED-PASS (a stale entry; delete it when W-2 lands) and fails the ring.
An abort (APP-UNHANDLED, or an "Unhandled exception" trace) is unexpected unless it sits under a FAIL whose entry
says aborts_harness (the later checks of that harness are then unassessed; the line says so).
On a host that is not Windows the manifest has no effect: every FAIL is unexpected.

Ring: every join (called by tools/run-tests.sh on Windows only; --self-test by tools/check-docs.py, fast ring).
Cost: one pass over the logs, under 0.1 s per log; --self-test 0.1 s (docs/proof/wrt/red-first.md).

Usage:
  check-expected-failures.py --self-test
  check-expected-failures.py --log FILE [--status N] [--host windows|other] [--manifest FILE]
Exit 0: no unexpected failure or pass, and (with --status N != 0) at least one expected failure explains the status.
Exit 1: otherwise. Output ends with `EXPECTED-FAIL <n> (manifest)` and `UNEXPECTED <m>`.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = ROOT / "tests" / "expected-failures.windows.json"
TOKENS = ("PASS ", "FAIL ", "COST ", "STACK ", "RESULT ", "PARTITION ", "STAGE ", "SUITE")
CLASSES = ("historical-probe", "fail-closed-store")
ABORTS = ("APP-UNHANDLED", "Unhandled exception")


def load_manifest(path: Path) -> dict[str, dict]:
    doc = json.loads(path.read_text(encoding="utf-8"))
    entries: dict[str, dict] = {}
    for entry in doc["entries"]:
        for key in ("name", "ruling", "class", "until", "fragment"):
            if not entry.get(key):
                raise ValueError(f"manifest entry without {key}: {entry}")
        if entry["class"] not in CLASSES:
            raise ValueError(f"manifest entry {entry['name']}: unknown class {entry['class']}")
        if entry["name"] in entries:
            raise ValueError(f"manifest lists {entry['name']} twice")
        entries[entry["name"]] = entry
    return entries


def parse(text: str) -> tuple[list[dict], set[str]]:
    """Return (events, passed names). An event is a FAIL (name, text, abort) or a standalone abort."""
    events: list[dict] = []
    passed: set[str] = set()
    open_fail: dict | None = None
    for line in text.splitlines():
        if line.startswith("FAIL "):
            rest = line[5:]
            name = rest.split(" ", 1)[0]
            open_fail = {"name": name, "text": rest, "abort": False}
            events.append(open_fail)
        elif line.startswith(ABORTS):
            if open_fail is not None:
                open_fail["abort"] = True
                open_fail["text"] += "\n" + line
            else:
                events.append({"name": "<abort>", "text": line, "abort": True})
        elif line.startswith(TOKENS):
            if line.startswith("PASS "):
                passed.add(line[5:].split(" ", 1)[0])
            if not line.startswith("STACK "):
                open_fail = None
        elif open_fail is not None:
            open_fail["text"] += "\n" + line
    return events, passed


def classify(text: str, manifest: dict[str, dict], windows: bool) -> dict[str, list[str]]:
    events, passed = parse(text)
    out: dict[str, list[str]] = {"expected": [], "unexpected": [], "unexpected_pass": []}
    for event in events:
        entry = manifest.get(event["name"]) if windows else None
        if entry is None:
            reason = "not in the manifest" if windows else "manifest has no effect on this host"
            out["unexpected"].append(f"{event['name']}: {reason}")
        elif entry["fragment"] not in event["text"]:
            out["unexpected"].append(f"{event['name']}: listed, but the failure text lacks the fragment "
                                     f"{entry['fragment']!r} (Ruling {entry['ruling']})")
        elif event["abort"] and not entry.get("aborts_harness"):
            out["unexpected"].append(f"{event['name']}: aborts its harness, and the entry does not say so")
        else:
            note = "; aborts its harness, later checks unassessed" if event["abort"] else ""
            out["expected"].append(f"{event['name']} (Ruling {entry['ruling']}, {entry['class']}, until {entry['until']}{note})")
    if windows:
        for name in sorted(passed & manifest.keys()):
            out["unexpected_pass"].append(
                f"{name} passed but Ruling {manifest[name]['ruling']} lists it: delete the entry from "
                "tests/expected-failures.windows.json")
    return out


def report(result: dict[str, list[str]], status: int) -> int:
    for line in result["expected"]:
        print("EXPECTED " + line)
    for line in result["unexpected"]:
        print("UNEXPECTED " + line)
    for line in result["unexpected_pass"]:
        print("UNEXPECTED-PASS " + line)
    bad = len(result["unexpected"]) + len(result["unexpected_pass"])
    unexplained = status != 0 and not result["expected"] and bad == 0
    if unexplained:
        print(f"UNEXPECTED harness exit {status} with no FAIL line the manifest explains")
    print(f"EXPECTED-FAIL {len(result['expected'])} (manifest)")
    print(f"UNEXPECTED {bad + (1 if unexplained else 0)}")
    return 1 if bad or unexplained else 0


def self_test() -> int:
    manifest = {
        "Probe_A": {"name": "Probe_A", "ruling": "145", "class": "historical-probe", "until": "W-2 B2 lands",
                    "fragment": "Win32=32"},
        "Store_B": {"name": "Store_B", "ruling": "154", "class": "fail-closed-store", "until": "W-2 B2 lands",
                    "fragment": "DOC-UNSUPPORTED-PERSISTENCE"},
        "Cli_C": {"name": "Cli_C", "ruling": "154", "class": "fail-closed-store", "until": "W-2 B2 lands",
                  "fragment": "DOC-UNSUPPORTED-PERSISTENCE", "aborts_harness": True},
    }
    probe = "FAIL Probe_A NativeFailure: NTSTATUS=0xc0000043; Win32=32: in use"
    store = "FAIL Store_B ContractError: DOC-UNSUPPORTED-PERSISTENCE"
    cases = [
        ("all expected", f"PASS X\n{probe}\nCOST Probe_A 1\n{store}\nRESULT failures=2\n", True, 0, 0, 2, 0, 0),
        ("one unexpected", f"{probe}\nFAIL Other_D Boom\nRESULT failures=2\n", True, 1, 1, 1, 1, 0),
        ("name match, wrong fragment", "FAIL Store_B InvalidOperationException: Expected 1; actual 2\n", True, 1, 1, 0, 1, 0),
        ("listed test passing", f"PASS Store_B\n{probe}\n", True, 1, 1, 1, 0, 1),
        ("fragment on a continuation line, harness aborts",
         "FAIL Cli_C\nUnhandled exception. System.InvalidOperationException: inspect returned 3: DOC-UNSUPPORTED-PERSISTENCE\n   at X\n",
         True, 0, 0, 1, 0, 0),
        ("abort under an entry that does not say it aborts",
         "FAIL Store_B\nUnhandled exception. DOC-UNSUPPORTED-PERSISTENCE\n", True, 1, 1, 0, 1, 0),
        ("standalone crash", "STAGE s\nAPP-UNHANDLED APP-CRASH System.Exception\nSystem.Exception: x\n", True, 1, 1, 0, 1, 0),
        ("exit status with no FAIL line", "PASS X\n", True, 1, 0, 0, 0, 0),
        ("not Windows: a listed failure stays a failure", f"{probe}\n{store}\n", False, 1, 2, 0, 2, 0),
        ("not Windows: a listed pass is fine", "PASS Store_B\n", False, 0, 0, 0, 0, 0),
    ]
    bad = 0
    for label, text, windows, want_exit, _count, want_ok, want_unexpected, want_pass in cases:
        status = 1 if label == "exit status with no FAIL line" else 0
        result = classify(text, manifest, windows)
        got = (len(result["expected"]), len(result["unexpected"]), len(result["unexpected_pass"]))
        print(f"-- {label}")
        code = report(result, 1 if "FAIL" in text else status)
        if code != want_exit or got != (want_ok, want_unexpected, want_pass):
            print(f"SELF-TEST FAIL: {label}: exit {code} (want {want_exit}), "
                  f"expected/unexpected/pass {got} (want {(want_ok, want_unexpected, want_pass)})")
            bad += 1
    try:
        real = load_manifest(MANIFEST)
    except (OSError, ValueError, KeyError) as error:
        print(f"SELF-TEST FAIL: manifest does not load: {error}")
        return 1
    print(f"EXPECTED-FAILURES self-test {'FAIL' if bad else 'ok'}: {len(cases)} cases, manifest {len(real)} entries")
    return 1 if bad else 0


def main(argv: list[str]) -> int:
    if "--self-test" in argv:
        return self_test()

    def option(flag: str, default: str | None = None) -> str | None:
        return argv[argv.index(flag) + 1] if flag in argv else default

    log = option("--log")
    if log is None:
        print(__doc__)
        return 2
    host = option("--host") or ("windows" if sys.platform == "win32" else "other")
    manifest = load_manifest(Path(option("--manifest") or MANIFEST))
    text = Path(log).read_text(encoding="utf-8", errors="replace")
    return report(classify(text, manifest, host == "windows"), int(option("--status", "0")))


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
