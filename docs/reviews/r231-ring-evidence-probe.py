"""Read-only retained-evidence checks; never runs the test ring or a live helper."""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
COMMIT = "73cf89c667386225fe646115a8191e6bedc067bf"
BASE = ROOT / "docs/proof/win-gpu-f1-preflight/attempt-1/cleanup"


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    mismatches = []
    for folder in ("r231-full-ring-capture", "r231-pre-ring-capture"):
        directory = BASE / folder
        manifest = directory / "capture-manifest.json"
        document = json.loads(manifest.read_bytes())
        assert document["source_commit"] == "2ae90b1717c208e8d42129d5e735b92fa1a04e52"
        entries = document["files"]
        paths = {(ROOT / entry["path"]).relative_to(directory).as_posix() for entry in entries}
        assert len(paths) == len(entries)
        actual = {path.relative_to(directory).as_posix() for path in directory.rglob("*") if path.is_file()}
        assert actual == paths | {"capture-manifest.json"}
        for entry in entries:
            path = ROOT / entry["path"]
            assert path.resolve().is_relative_to(directory.resolve())
            raw = path.read_bytes()
            committed = subprocess.run(["git", "cat-file", "blob", COMMIT + ":" + path.relative_to(ROOT).as_posix()],
                                       cwd=ROOT, capture_output=True, check=True).stdout
            assert raw == committed
            assert hashlib.sha1(b"blob " + str(len(committed)).encode() + b"\0" + committed).hexdigest() == entry["blob_oid"]
            prior = subprocess.run(["git", "cat-file", "blob", document["source_commit"] + ":" + entry["path"]],
                                   cwd=ROOT, capture_output=True, check=True).stdout
            assert prior == committed
            if len(committed) != entry["bytes"] or hashlib.sha256(committed).hexdigest() != entry["sha256"].lower():
                mismatches.append((folder, entry["path"]))
        bad = [name for capture, name in mismatches if capture == folder]
        print("MANIFEST-CHECK", folder, "files=" + str(len(entries)), "mismatches=" + str(len(bad)), "sha256=" + hashlib.sha256(manifest.read_bytes()).hexdigest())
        for name in bad:
            print("COMMITTED-HASH-MISMATCH", folder, name)
    directory = BASE / "r231-full-ring-capture"
    classifier = load("ring_classifier", ROOT / "tools/check-expected-failures.py")
    manifest = classifier.load_manifest(ROOT / "tests/expected-failures.windows.json")
    expected = {"Core.part1of3": (346, 9), "Core.part2of3": (210, 7), "Core.part3of3": (205, 11),
                "Analysis.part1of3": (44, 0), "Analysis.part2of3": (69, 0), "Analysis.part3of3": (146, 0),
                "Desktop": (776, 33), "Cli": (18, 2)}
    for name, counts in expected.items():
        text = (directory / (name + ".log")).read_text(encoding="utf-8")
        classified = classifier.classify(text, manifest, True)
        actual = (len(re.findall(r"^PASS ", text, re.M)), len(classified["expected"]))
        assert actual == counts, (name, actual)
        assert not classified["unexpected"] and not classified["unexpected_pass"]
        print("CLASSIFIER-PASS", name, "passes=" + str(actual[0]), "expected=" + str(actual[1]), "unexpected=0")
    desktop = (directory / "Desktop.log").read_text(encoding="utf-8")
    suites = re.findall(r"^SUITE (.+) exit ([01])$", desktop, re.M)
    timing = dict((name, float(seconds)) for name, seconds in re.findall(r"^SUITE-TIME (.+) ([\d.]+) s$", desktop, re.M))
    source = (ROOT / "tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs").read_text(encoding="utf-8")
    block = source.split("int spawnedExit =", 1)[1].split(");", 1)[0]
    declared = re.findall(r'"(--[^"]+)"', block)
    assert len(suites) == len(declared) == 18
    assert {name for name, status in suites} == set(declared) == set(timing)
    assert max(timing.values()) < 120
    families = {}
    partition_count = 0
    previous = 0
    for match in re.finditer(r"^SUITE (.+) exit [01]$", desktop, re.M):
        name = match[1]
        part = re.search(r"(--[a-z-]+) --part=(\d+)/(\d+)", name)
        if part:
            records = re.findall(r"^PARTITION (\d+)/(\d+) of (\d+) checks$", desktop[previous:match.start()], re.M)
            assert len(records) == 1 and records[0][:2] == (part[2], part[3])
            families.setdefault(part[1], []).append(tuple(map(int, records[0])))
            partition_count += 1
        previous = match.end()
    for family, parts in families.items():
        assert len({(total, checks) for part, total, checks in parts}) == 1
        assert sorted(part for part, total, checks in parts) == list(range(1, parts[0][1] + 1))
    assert partition_count == 15 and len(families) == 7
    for family, total in (("Core", 788), ("Analysis", 22)):
        for part in (1, 2, 3):
            text = (directory / f"{family}.part{part}of3.log").read_text(encoding="utf-8")
            assert re.search(rf"^PARTITION {part}/3 of {total} (checks|groups)$", text, re.M)
    print("COMPLETION-PASS children=18 desktop_partitions=15 families=7 core_parts=3 analysis_parts=3 max_child_s=" + str(max(timing.values())))
    python = (directory / "python-unittest.out").read_text(encoding="utf-8")
    assert "Ran 94 tests in 5.714s" in python and python.rstrip().endswith("OK")
    wall = int((directory / "wall.ms").read_text())
    build = int((directory / "build.ms").read_text())
    assert (wall, build, wall - build) == (207380, 14421, 192959)
    costs = load("ring_costs", ROOT / "tools/check-test-costs.py")
    # Replay the reported uncalibrated policy, not a measurement of original load/host.
    with tempfile.TemporaryDirectory() as temporary:
        errors, misses = costs.check(directory, costs.DEFAULT_JOBS, "11.70", "proc-gitbash", "synthetic", Path(temporary))
        status, line = costs.budget_verdict(207, 60, "11.70", "proc-gitbash", "synthetic", Path(temporary))
    assert not errors and len(misses) == 16 and status == 0 and "COST-MISS" in line
    print("COST-POLICY-REPLAY-PASS reported_load=11.70 uncalibrated=True errors=0 misses=16 budget_status=0")
    receipt_path = BASE / "r231-original-ring-telemetry-receipt.json"
    receipt = json.loads(receipt_path.read_bytes())
    assert hashlib.sha256(receipt_path.read_bytes()).hexdigest() == "f12be809b8318192dded940057f7461d134179e58b6e017367c10e85635c43d9"
    candidates = list((Path.home() / ".codex/sessions").rglob(receipt["source_file_basename"]))
    assert len(candidates) == 1, "exact telemetry source unavailable or ambiguous"
    lines = candidates[0].read_bytes().splitlines()
    wrapper_line = lines[receipt["source_line_number"] - 1]
    aggregate_line = lines[receipt["paired_aggregate"]["source_line_number"] - 1]
    assert hashlib.sha256(wrapper_line).hexdigest() == receipt["event"]["raw_jsonl_line_sha256"]
    assert hashlib.sha256(aggregate_line).hexdigest() == receipt["paired_aggregate"]["raw_jsonl_line_sha256"]
    wrapper_text = json.loads(wrapper_line)["payload"]["output"][1]["text"]
    item = json.loads(aggregate_line)["payload"]["item"]
    aggregate = item["aggregated_output"]
    assert hashlib.sha256(wrapper_text.encode()).hexdigest() == receipt["wrapper"]["text_utf8_sha256"]
    assert hashlib.sha256(aggregate.encode()).hexdigest() == receipt["paired_aggregate"]["output_utf8_sha256"]
    wrapper = json.loads(wrapper_text)
    assert wrapper["exit_code"] == item["exit_code"] == 0
    assert item["status"] == "completed"
    for line in receipt["allowlisted_verdict_lines"]:
        if "host [redacted]" in line:
            assert any(value.startswith(line.split("[redacted]")[0]) for value in aggregate.splitlines())
        else:
            assert line in aggregate.splitlines(), "receipt verdict line not in source"
    assert "all test harnesses passed" in aggregate
    print("TELEMETRY-RECEIPT-PASS raw_line_hashes=2 output_hashes=2 original_exit=0 status=completed allowlisted_lines=" + str(len(receipt["allowlisted_verdict_lines"])))
    print("PRIVACY: telemetry raw content and original host token not published")
    if mismatches:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
