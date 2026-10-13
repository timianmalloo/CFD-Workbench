"""Offline candidate/blob and retained-ring review; no ring or live command execution."""
import ast
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
CANDIDATE = "70d022bf4369c8cb58888f88e6efba00070f35db"


def blob(commit, path):
    return subprocess.run(["git", "cat-file", "blob", commit + ":" + path], cwd=ROOT,
                          capture_output=True, check=True).stdout


def literal(source, name):
    tree = ast.parse(source)
    return ast.literal_eval(next(node.value for node in tree.body if isinstance(node, ast.Assign)
                                and any(isinstance(target, ast.Name) and target.id == name for target in node.targets)))


def main():
    path = "tools/check-reader-sharing.py"
    old = literal(blob(CANDIDATE + "^", path), "PS_GRANDFATHERED")
    new = literal(blob(CANDIDATE, path), "PS_GRANDFATHERED")
    removed = old - new
    assert len(old) == 9 and len(new) == 7 and not new - old and len(removed) == 2
    assert all(name == "docs/proof/win-gpu-f1-preflight/packet/Invoke-Preflight.ps1" for name, digest in removed)
    assert all("win-gpu-b1/" in name or "win-gpu-b2/" in name for name, digest in new)
    bindings = {}
    for name in ("Invoke-Preflight.ps1", "SelfTest.ps1"):
        relative = "docs/proof/win-gpu-f1-preflight/packet/" + name
        raw = blob(CANDIDATE, relative)
        assert raw == (ROOT / relative).read_bytes()
        bindings[name] = {"bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest(),
                          "blob_oid": hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()}
    spec = importlib.util.spec_from_file_location("r232_classifier", ROOT / "tools/check-expected-failures.py")
    classifier = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(classifier)
    expected = classifier.load_manifest(ROOT / "tests/expected-failures.windows.json")
    reports = []
    telemetry_basename = "rollout-2026-10-12T21-07-51-01a13371-fd53-76a3-bfb5-4e0e7082e37c.jsonl"
    telemetry_files = list((Path.home() / ".codex/sessions").rglob(telemetry_basename))
    assert len(telemetry_files) == 1
    telemetry_lines = telemetry_files[0].read_bytes().splitlines()
    for attempt in (1, 2):
        capture = Path(os.environ["LOCALAPPDATA"]) / "Temp" / f"r232-reader-repair-ring{attempt}-capture"
        files = [path for path in capture.rglob("*") if path.is_file()]
        assert len(files) == (80 if attempt == 1 else 116)
        manifest = [{"path": path.relative_to(capture).as_posix(), "bytes": path.stat().st_size,
                     "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in sorted(files)]
        report = {"attempt": attempt, "capture_basename": capture.name, "file_count": len(files),
                  "all_file_inventory_sha256": hashlib.sha256(json.dumps(manifest, sort_keys=True, separators=(",", ":")).encode()).hexdigest(),
                  "bindings": {}, "suites": {}}
        for name in ("Core.part1of3", "Core.part2of3", "Core.part3of3", "Desktop", "Analysis.part1of3", "Analysis.part2of3", "Analysis.part3of3", "Cli"):
            raw = (capture / (name + ".log")).read_bytes()
            text = raw.decode("utf-8")
            result = classifier.classify(text, expected, True)
            report["bindings"][name + ".log"] = {"bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()}
            report["suites"][name] = {"pass": len(re.findall(r"^PASS ", text, re.M)), "expected": len(result["expected"]),
                                     "unexpected": len(result["unexpected"]) + len(result["unexpected_pass"]),
                                     "milliseconds": int((capture / (name + ".ms")).read_text())}
        for name in ("wall.ms", "build.ms", "python-unittest.out"):
            raw = (capture / name).read_bytes()
            report["bindings"][name] = {"bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()}
        report["wall_ms"] = int((capture / "wall.ms").read_text())
        report["build_ms"] = int((capture / "build.ms").read_text())
        report["net_ms"] = report["wall_ms"] - report["build_ms"]
        python = (capture / "python-unittest.out").read_text(encoding="utf-8")
        assert "Ran 50 tests" in python and python.rstrip().endswith("OK")
        report["python_tests"] = 50
        desktop = (capture / "Desktop.log").read_text(encoding="utf-8")
        report["desktop_child_records"] = len(re.findall(r"^SUITE .+ exit \d+$", desktop, re.M))
        report["desktop_partition_records"] = len(re.findall(r"^PARTITION ", desktop, re.M))
        report["desktop_max_child_seconds"] = max(map(float, re.findall(r"^SUITE-TIME .+ ([\d.]+) s$", desktop, re.M)))
        assert report["suites"]["Desktop"]["pass"] == (673 if attempt == 1 else 776)
        assert report["suites"]["Desktop"]["unexpected"] == (18 if attempt == 1 else 0)
        if attempt == 2:
            assert report["suites"]["Desktop"]["expected"] == 33
            assert report["desktop_child_records"] == 18 and report["desktop_partition_records"] == 15
            assert report["desktop_max_child_seconds"] < 120
            harness = (ROOT / "tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs").read_text(encoding="utf-8")
            block = harness.split("int spawnedExit =", 1)[1].split(");", 1)[0]
            declared = re.findall(r'"(--[^"]+)"', block)
            observed = re.findall(r"^SUITE (.+) exit [01]$", desktop, re.M)
            assert len(set(observed)) == 18 and set(observed) == set(declared)
            families = {}
            previous = 0
            for match in re.finditer(r"^SUITE (.+) exit [01]$", desktop, re.M):
                part = re.search(r"(--[a-z-]+) --part=(\d+)/(\d+)", match[1])
                if part:
                    records = re.findall(r"^PARTITION (\d+)/(\d+) of (\d+) checks$", desktop[previous:match.start()], re.M)
                    assert len(records) == 1 and records[0][:2] == (part[2], part[3])
                    families.setdefault(part[1], []).append(tuple(map(int, records[0])))
                previous = match.end()
            assert len(families) == 7
            for parts in families.values():
                assert len({(total, checks) for part, total, checks in parts}) == 1
                assert sorted(part for part, total, checks in parts) == list(range(1, parts[0][1] + 1))
        for name, result in report["suites"].items():
            if name != "Desktop":
                assert result["unexpected"] == 0
        for name, counts in {"Core.part1of3": (346,9), "Core.part2of3": (210,7), "Core.part3of3": (205,11),
                             "Analysis.part1of3": (44,0), "Analysis.part2of3": (69,0), "Analysis.part3of3": (146,0), "Cli": (18,2)}.items():
            assert (report["suites"][name]["pass"], report["suites"][name]["expected"]) == counts
        reports.append(report)
        ordinal = 2161 if attempt == 1 else 2378
        raw_line = telemetry_lines[ordinal - 1]
        item = json.loads(raw_line)["payload"]["item"]
        original_output = item["aggregated_output"]
        assert item["exit_code"] == (1 if attempt == 1 else 0)
        assert str(report["wall_ms"]) + " ms" in original_output
        assert ("all test harnesses passed" in original_output) == (attempt == 2)
        cost_match = re.search(r"^test costs: (\d+) failures, (\d+) COST-MISS \(load ([\d.]+)\)$", original_output.replace("\r\n", "\n"), re.M)
        assert cost_match is not None
        assert cost_match.groups() == (("0", "16", "17.19") if attempt == 1 else ("0", "17", "13.45"))
        allowed = []
        for line in original_output.splitlines():
            if line.startswith(("test costs:", "wall ", "all test harnesses passed")):
                allowed.append(line)
            elif line.startswith("COST-MISS TEST-BUDGET "):
                allowed.append(line.split(" host ")[0] + " host [redacted]")
        report["original_telemetry"] = {"source_basename": telemetry_basename, "line_number": ordinal,
            "json_path": "$.payload.item.aggregated_output", "raw_line_sha256": hashlib.sha256(raw_line).hexdigest(),
            "output_utf8_sha256": hashlib.sha256(original_output.encode()).hexdigest(), "exit_code": item["exit_code"],
            "status": item["status"], "allowlisted_lines": allowed}
        print("ORIGINAL-EXIT-PASS", attempt, "exit=" + str(item["exit_code"]), "cost_failures=" + cost_match[1],
              "cost_misses=" + cost_match[2], "end_load=" + cost_match[3])
        print("CAPTURE-PASS", attempt, "files=" + str(len(files)), "Desktop=" + json.dumps(report["suites"]["Desktop"]),
              "partitions=" + str(report["desktop_partition_records"]), "wall_ms=" + str(report["wall_ms"]))
    output = {"candidate": CANDIDATE, "source_bindings": bindings, "removed_grandfather_entries": sorted(removed),
              "remaining_grandfather_count": len(new), "ring_attempts": reports,
              "privacy": "No raw capture text or absolute private path is published; inventory digest covers all retained files."}
    destination = ROOT / "docs/reviews/r232-reader-evidence.json"
    destination.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8", newline="\n")
    print("BINDINGS-PASS grandfather_removed=2 remaining=7 source_files=2")


if __name__ == "__main__":
    main()
