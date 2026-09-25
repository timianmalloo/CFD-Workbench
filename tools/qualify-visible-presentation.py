#!/usr/bin/env python3
"""Synthetic receipt contract only. Never launches UI or capture."""
import argparse
import copy
import json
import hashlib
import sys
import tempfile
import subprocess
from fractions import Fraction
from pathlib import Path


def package_manifest(directory):
    """Seal every private published file; resolve concrete RID deps without guessing."""
    root = Path(directory).absolute()
    if root != root.resolve():
        raise ValueError("VP-PACKAGE-ESCAPE")
    files = {}
    for path in sorted(root.rglob("*")):
        if path.is_symlink() or not path.resolve().is_relative_to(root.resolve()):
            raise ValueError("VP-PACKAGE-ESCAPE")
        if path.is_file():
            relative = path.relative_to(root).as_posix()
            if relative.casefold() in {p.casefold() for p in files}:
                raise ValueError("VP-PACKAGE-AMBIGUOUS")
            files[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
    minimum = {"VisiblePresentation", "VisiblePresentation.dll", "VisiblePresentation.deps.json",
               "VisiblePresentation.runtimeconfig.json"}
    if not minimum <= files.keys():
        raise ValueError("VP-PACKAGE-MISSING")
    deps = json.loads((root / "VisiblePresentation.deps.json").read_text(encoding="utf-8"))
    target = deps["runtimeTarget"]["name"]
    if (not target.endswith("/osx-arm64") or target not in deps["targets"]
            or any(value for key, value in deps["targets"].items() if key != target)):
        raise ValueError("VP-RESOLUTION-AMBIGUOUS")
    resolution = {}
    for library, assets in deps["targets"][target].items():
        if assets.get("runtimeTargets"):
            raise ValueError("VP-RESOLUTION-AMBIGUOUS")
        for category in ("runtime", "native", "resources"):
            for asset, metadata in assets.get(category, {}).items():
                if asset.endswith("/_._"):
                    continue
                name = Path(asset).name
                relative = (metadata["locale"] + "/" + name) if category == "resources" else name
                if relative not in files:
                    raise ValueError("VP-RESOLUTION-MISSING:" + relative)
                key = library + ":" + category + ":" + asset
                if relative in resolution.values():
                    raise ValueError("VP-RESOLUTION-AMBIGUOUS")
                resolution[key] = relative
    return dict(schema=1, rid="osx-arm64", files=files, resolution=resolution,
                runtimeTrust="framework-dependent-system-runtime-not-attested")


def verify_package(directory, expected):
    try:
        return "VP-PACKAGE-BOUND" if package_manifest(directory) == expected else "VP-PACKAGE-CHANGED"
    except (OSError, ValueError, KeyError, TypeError):
        return "VP-PACKAGE-REFUSED"


def package_tests(directory):
    """Independent byte/path perturbations against the real package reader."""
    import shutil
    original = package_manifest(directory)
    rows = []
    with tempfile.TemporaryDirectory(prefix="vp-contract-") as temporary:
        root = Path(temporary).resolve() / "package"
        shutil.copytree(directory, root)
        def record(name, expected):
            actual = verify_package(root, original)
            rows.append(dict(case=name, expected=expected, actual=actual, passed=actual == expected))
        record("whole_publish_positive", "VP-PACKAGE-BOUND")
        dll = root / "VisiblePresentation.dll"
        data = dll.read_bytes()
        dll.write_bytes(data + b"changed")
        record("private_managed_changed", "VP-PACKAGE-CHANGED")
        dll.write_bytes(data)
        native = next(root.glob("*.dylib"))
        native_data = native.read_bytes()
        native.unlink()
        record("private_native_missing", "VP-PACKAGE-REFUSED")
        native.write_bytes(native_data)
        extra = root / "undeclared.txt"
        extra.write_text("extra", encoding="utf-8", newline="\n")
        record("undeclared_file", "VP-PACKAGE-CHANGED")
        extra.unlink()
        extra.symlink_to(dll)
        record("symlink_escape_or_alias", "VP-PACKAGE-REFUSED")
        extra.unlink()
        dll.unlink()
        record("minimum_asset_missing", "VP-PACKAGE-REFUSED")
    print(json.dumps(dict(cases=rows, manifest=original, qualification="noncapture-contract-executed",
                          temporary_cleanup="TemporaryDirectory context completed"), sort_keys=True))
    return 0 if all(row["passed"] for row in rows) else 1


def request_tests(helper):
    """Only the helper's pure parser entry; never --capture-reviewed."""
    minimum = ["VisiblePresentation", "VisiblePresentation.dll", "VisiblePresentation.deps.json",
               "VisiblePresentation.runtimeconfig.json"]
    positive = dict(pid=7, window=9, executable="/synthetic/VisiblePresentation", executableSHA256="a" * 64,
                    launchReferenceBits=1, startSeconds=10, startMicroseconds=123,
                    bundle="synthetic", title="synthetic", durationSeconds=1,
                    width=192, height=192, packageRoot="/synthetic",
                    packageFiles={name: "a" * 64 for name in minimum}, helperSHA256="b" * 64,
                    contextRoot="/synthetic/source",
                    contextFiles={name: "c" * 64 for name in ["tools/spikes/VisiblePresentation/Capture.swift",
                        "tools/spikes/VisiblePresentation/Program.cs", "tools/spikes/VisiblePresentation/VisiblePresentation.csproj",
                        "tools/qualify-visible-presentation.py", "global.json"]}, frameCap=3,
                    geometry=dict(x=0, y=0, width=192, height=192, scale=1),
                    crops=[dict(x=x, y=0, width=64, height=64, expected="d" * 64) for x in (0, 64, 128)])
    cases = [("structural_positive_not_observed_target", positive, 0)]
    def bad(name, edit):
        item = copy.deepcopy(positive)
        edit(item)
        cases.append((name, item, 3))
    bad("unknown_field", lambda r: r.update(fullScreen=True))
    bad("wrong_executable", lambda r: r.update(executable="/other/VisiblePresentation"))
    bad("missing_private_minimum", lambda r: r["packageFiles"].pop("VisiblePresentation.dll"))
    bad("path_escape", lambda r: r["packageFiles"].update({"../escape": "a" * 64}))
    bad("invalid_digest", lambda r: r.update(helperSHA256="x" * 64))
    bad("invalid_pid", lambda r: r.update(pid=0))
    bad("invalid_window", lambda r: r.update(window=0))
    bad("ambiguous_scale", lambda r: r["geometry"].update(scale=2))
    bad("invalid_dimensions", lambda r: r.update(width=0))
    bad("overlap", lambda r: r["crops"][1].update(x=0))
    bad("crop_escape", lambda r: r["crops"][2].update(x=129))
    bad("unbounded_frames", lambda r: r.update(frameCap=601))
    bad("unbounded_duration", lambda r: r.update(durationSeconds=9))
    rows = []
    with tempfile.TemporaryDirectory(prefix="vp-request-") as temporary:
        path = Path(temporary) / "synthetic.json"
        for name, item, expected in cases:
            encoded = json.dumps(item, sort_keys=True, separators=(",", ":")).encode("utf-8")
            path.write_bytes(encoded)
            result = subprocess.run([str(helper.resolve()), "--contract-request", str(path)],
                                    capture_output=True, encoding="utf-8", timeout=10, check=False)
            output = json.loads(result.stdout)
            rows.append(dict(case=name, input=item, expected_exit=expected, exit=result.returncode,
                             output=output, stderr=result.stderr, passed=result.returncode == expected))
    print(json.dumps(dict(cases=rows, native_entry="not called", temporary_cleanup="completed"), sort_keys=True))
    return 0 if all(row["passed"] for row in rows) else 1


def clock_report(raw):
    """Report adjacent noncapture sample uncertainty, never a cross-run drift guarantee."""
    frequency = raw["stopwatch_frequency"]
    numerator, denominator = raw["mach_numerator"], raw["mach_denominator"]
    if any(type(x) is not int or x <= 0 for x in (frequency, numerator, denominator)):
        raise ValueError("clock rates")
    quantum = Fraction(1_000_000_000, frequency) + Fraction(numerator, denominator)
    pairs = raw["pairs"]
    if not 2 <= len(pairs) <= 1000:
        raise ValueError("clock sample count")
    intervals = []
    previous = None
    for pair in pairs:
        before, mach, after = (pair[k] for k in ("before", "mach", "after"))
        if any(type(x) is not int or x < 0 for x in (before, mach, after)) or before > after:
            raise ValueError("clock sample")
        if previous is not None and (before < previous[0] or mach < previous[1]):
            raise ValueError("clock regression")
        previous = (after, mach)
        mn = Fraction(mach * numerator, denominator)
        intervals.append([Fraction(before * 1_000_000_000, frequency) - mn - quantum,
                          Fraction(after * 1_000_000_000, frequency) - mn + quantum])
    lower, upper = max(x[0] for x in intervals), min(x[1] for x in intervals)
    if lower > upper:
        raise ValueError("empty clock offset intersection")
    return dict(samples=len(pairs), quantization_bound_ns=str(quantum),
                offset_intersection_ns=[str(lower), str(upper)],
                drift="Not assessed: short adjacent sampling has no trial-duration bound",
                visible_latency="Not assessed", raw=raw)


def assess(receipt, expected):
    """Fail closed; synthetic state-machine premises never qualify native pixels."""
    def refuse(code):
        return dict(code=code, visible_latency="Not assessed")
    def integer(value):
        return type(value) is int and abs(value) < 2 ** 63
    try:
        r = receipt
        if r["schema"] != 1 or not integer(r["budget_ns"]) or r["budget_ns"] < 0:
            return refuse("VP-SCHEMA")
        if not isinstance(r["input"], list) or len(r["input"]) != 2 or not all(map(integer, r["input"])):
            return refuse("VP-SCHEMA")
        ilo, ihi = r["input"]
        if ilo > ihi:
            return refuse("VP-SCHEMA")
        if r["mode"] != "synthetic":
            return refuse("VP-NATIVE-UNQUALIFIED")
        if r["identity"] != expected["identity"]:
            return refuse("VP-IDENTITY")
        if r["outcome"] != "completed":
            return refuse("VP-OUTCOME")
        if r["visibility"] != "synthetic-only":
            return refuse("VP-VISIBILITY")
        if r["persistence"] != "synthetic-monotone-model":
            return refuse("VP-PERSISTENCE")
        if r["sample_loss"] != 0:
            return refuse("VP-LOSS")
        tb, pairs = r["timebase"], r["clock_pairs"]
        if (not isinstance(tb, list) or len(tb) != 2 or not all(integer(x) and x > 0 for x in tb)
                or not integer(r["drift_ns"]) or r["drift_ns"] < 0
                or not integer(r["resolution_ns"]) or r["resolution_ns"] < 1):
            return refuse("VP-CLOCK")
        if not isinstance(pairs, list) or len(pairs) < 2 or len(pairs) > 1000:
            return refuse("VP-CLOCK")
        offsets = []
        previous = None
        for before, mach, after in pairs:
            if not all(map(integer, (before, mach, after))) or before > after or mach < 0:
                return refuse("VP-CLOCK")
            if previous is not None and (mach <= previous[1] or before <= previous[2]):
                return refuse("VP-CLOCK")
            converted = Fraction(mach * tb[0], tb[1])
            offsets.append((before - converted, after - converted))
            previous = (before, mach, after)
        olo, ohi = max(x[0] for x in offsets), min(x[1] for x in offsets)
        if olo > ohi:
            return refuse("VP-CLOCK")
        uncertainty = r["drift_ns"] + r["resolution_ns"]
        frames = r["frames"]
        if not isinstance(frames, list) or not 2 <= len(frames) <= 1000:
            return refuse("VP-PREDECESSOR")
        if set(expected["regions"]) != {"inspector", "status", "canvas"}:
            return refuse("VP-SCHEMA")
        first_final = None
        previous_event = -1
        previous_sequence = None
        converted_frames = []
        for index, f in enumerate(frames):
            if f["identity"] != expected["identity"]:
                return refuse("VP-IDENTITY")
            event, seq = f["event"], f["sequence"]
            if (not all(map(integer, (event, seq, f["received"]))) or event <= 0
                    or event <= previous_event or f["received"] < event
                    or (previous_sequence is not None and seq != previous_sequence + 1)):
                return refuse("VP-ORDER")
            if not pairs[0][1] <= event <= pairs[-1][1]:
                return refuse("VP-CLOCK")
            if f["status"] != "complete":
                return refuse("VP-FRAME")
            previous_event, previous_sequence = event, seq
            t = Fraction(event * tb[0], tb[1])
            converted_frames.append((t + olo - uncertainty, t + ohi + uncertainty))
            final = f["regions"] == expected["regions"]
            if final and first_final is None:
                first_final = index
            elif first_final is not None and not final:
                return refuse("VP-REGRESSION")
        if first_final is None:
            return refuse("VP-NO-FINAL")
        if first_final == 0:
            return refuse("VP-PREDECESSOR")
        old_lo = converted_frames[first_final - 1][0]
        new_hi = converted_frames[first_final][1]
        if new_hi < ihi or old_lo < ilo:
            return refuse("VP-DELTA")
        lower, upper = max(Fraction(0), old_lo - ihi), new_hi - ilo
        # Outward integer rounding retains conversion uncertainty.
        lo = lower.numerator // lower.denominator
        hi = -(-upper.numerator // upper.denominator)
        code = ("synthetic_upper_bound" if hi <= r["budget_ns"] else
                "synthetic_lower_bound" if lo > r["budget_ns"] else "synthetic_overlap")
        return dict(code=code, interval_ns=[lo, hi], visible_latency="Not assessed",
                    endpoint="synthetic-state-machine", uncertainty_ns=uncertainty,
                    offset_ns=[str(olo), str(ohi)])
    except (KeyError, TypeError, ValueError, ZeroDivisionError, OverflowError):
        return refuse("VP-SCHEMA")


def fixture():
    identity = dict(pid=7, window=9, package="a" * 64, launch="owned-1")
    regions = {name: dict(operation="edit-1", source="b" * 64, draft="d1",
                         generation=2, state="draft", content=content)
               for name, content in (("inspector", "chord=120mm"),
                                     ("status", "Draft ready"),
                                     ("canvas", [0, 120, 40, 80]))}
    expected = dict(identity=identity, regions=regions)
    old = copy.deepcopy(regions)
    for region in old.values():
        region["generation"] = 1
    frames = [dict(sequence=i, event=t, received=t + 3, status="complete",
                   identity=copy.deepcopy(identity), regions=copy.deepcopy(r))
              for i, t, r in ((1, 20, old), (2, 40, regions), (3, 50, regions))]
    receipt = dict(schema=1, mode="synthetic", identity=identity,
                   input=[10, 12], input_kind="direct-method-not-OS-input",
                   timebase=[1, 1], clock_pairs=[[0, 0, 2], [100, 100, 102]],
                   drift_ns=1, resolution_ns=1, frames=frames,
                   outcome="completed", persistence="synthetic-monotone-model",
                   visibility="synthetic-only", sample_loss=0, budget_ns=40)
    return receipt, expected


def run_tests():
    raw, expected = fixture()
    cases = [("valid", raw, "synthetic_upper_bound", [6, 34])]
    def bad(name, edit, code):
        changed = copy.deepcopy(raw)
        edit(changed)
        cases.append((name, changed, code, None))
    for key in ("pid", "window", "package", "launch"):
        bad("wrong_" + key, lambda r, k=key: r["identity"].update({k: "wrong"}), "VP-IDENTITY")
        bad("frame_wrong_" + key, lambda r, k=key: r["frames"][1]["identity"].update({k: "wrong"}), "VP-IDENTITY")
    for key in ("operation", "source", "draft", "generation", "state", "content"):
        bad("stale_" + key, lambda r, k=key: [f["regions"]["canvas"].update({k: "wrong"}) for f in r["frames"]], "VP-NO-FINAL")
    bad("one_region_old", lambda r: [f["regions"]["inspector"].update(generation=1) for f in r["frames"]], "VP-NO-FINAL")
    bad("marker_wrong_content", lambda r: [(f.update(completion=True), f["regions"]["canvas"].update(content=[])) for f in r["frames"]], "VP-NO-FINAL")
    for status in ("idle", "skipped", "blank", "suspended", "incomplete", "started"):
        bad(status, lambda r, s=status: r["frames"][1].update(status=s), "VP-FRAME")
    bad("zero_presentation", lambda r: r["frames"][1].update(event=0), "VP-ORDER")
    bad("duplicate", lambda r: r["frames"].insert(1, copy.deepcopy(r["frames"][0])), "VP-ORDER")
    bad("reordered", lambda r: r["frames"].reverse(), "VP-ORDER")
    bad("missing_frame", lambda r: r.update(sample_loss=1), "VP-LOSS")
    bad("missing_predecessor", lambda r: r["frames"].pop(0), "VP-PREDECESSOR")
    bad("sequence_gap", lambda r: r["frames"][1].update(sequence=8), "VP-ORDER")
    bad("unknown_clock", lambda r: r.update(timebase=None), "VP-CLOCK")
    bad("unknown_drift", lambda r: r.update(drift_ns=None), "VP-CLOCK")
    bad("inconsistent_clock", lambda r: r.update(clock_pairs=[[0, 0, 0], [100, 200, 100]]), "VP-CLOCK")
    bad("negative_delta", lambda r: r.update(input=[90, 100]), "VP-DELTA")
    bad("callback_before_event", lambda r: r["frames"][1].update(received=39), "VP-ORDER")
    bad("timeout", lambda r: r.update(outcome="timeout"), "VP-OUTCOME")
    bad("cancelled", lambda r: r.update(outcome="cancelled"), "VP-OUTCOME")
    bad("hidden", lambda r: r.update(visibility="hidden"), "VP-VISIBILITY")
    bad("occluded_unproved", lambda r: r.update(visibility="window-content-only"), "VP-VISIBILITY")
    bad("native_not_qualified", lambda r: r.update(mode="native"), "VP-NATIVE-UNQUALIFIED")
    bad("unknown_persistence", lambda r: r.update(persistence="assumed"), "VP-PERSISTENCE")
    bad("transient_final_regression", lambda r: r["frames"].append(dict(copy.deepcopy(r["frames"][0]), sequence=4, event=60, received=63)), "VP-REGRESSION")
    bad("late_preview_after_cancel", lambda r: r["frames"][2]["regions"]["status"].update(state="preview"), "VP-REGRESSION")
    bad("malformed", lambda r: r.update(input="10"), "VP-SCHEMA")
    bad("nan", lambda r: r.update(budget_ns=float("nan")), "VP-SCHEMA")
    records = []
    for name, receipt, code, interval in cases:
        result = assess(receipt, expected)
        ok = result["code"] == code and (interval is None or result.get("interval_ns") == interval)
        records.append(dict(case=name, expected_code=code, actual=result, passed=ok, raw=receipt))
    # Independent metamorphic law: translation changes no latency; a later final moves both bounds.
    shifted = copy.deepcopy(raw)
    shifted["input"] = [x + 1000 for x in shifted["input"]]
    for f in shifted["frames"]:
        f["event"] += 1000
        f["received"] += 1000
    shifted["clock_pairs"] = [[0, 0, 2], [2000, 2000, 2002]]
    result = assess(shifted, expected)
    records.append(dict(case="clock_translation", actual=result, passed=result.get("interval_ns") == [6, 34]))
    for budget, code in ((6, "synthetic_overlap"), (5, "synthetic_lower_bound"), (34, "synthetic_upper_bound")):
        changed = dict(raw, budget_ns=budget)
        result = assess(changed, expected)
        records.append(dict(case="budget_" + str(budget), actual=result, passed=result["code"] == code))
    root = Path(__file__).resolve().parents[1]
    sources = ["tools/qualify-visible-presentation.py", "tools/spikes/VisiblePresentation/Program.cs",
               "tools/spikes/VisiblePresentation/Capture.swift", "tools/spikes/VisiblePresentation/VisiblePresentation.csproj", "global.json"]
    hashes = {p: hashlib.sha256((root / p).read_bytes()).hexdigest() for p in sources if (root / p).is_file()}
    report = clock_report(dict(stopwatch_frequency=1000000000, mach_numerator=1, mach_denominator=1,
                              pairs=[dict(before=10, mach=10, after=12), dict(before=100, mach=100, after=102)]))
    records.append(dict(case="paired_clock_conversion", actual=report,
                        passed=report["offset_intersection_ns"] == ["-2", "4"]))
    disjoint = dict(stopwatch_frequency=1000000000, mach_numerator=1, mach_denominator=1,
                    pairs=[dict(before=10, mach=100, after=12), dict(before=100, mach=100, after=102)])
    try:
        clock_report(disjoint)
        rejected = False
    except ValueError:
        rejected = True
    records.append(dict(case="paired_clock_disjoint_refused", raw=disjoint, passed=rejected))
    print(json.dumps(dict(schema=1, qualification="synthetic-executed", visible_latency="Not assessed", source_sha256=hashes,
                          expected=expected, cases=records, passed=sum(r["passed"] for r in records),
                          total=len(records)), sort_keys=True, allow_nan=True))
    return 0 if all(r["passed"] for r in records) else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--receipt", type=Path)
    parser.add_argument("--expected", type=Path)
    parser.add_argument("--clock-receipt", type=Path)
    parser.add_argument("--package", type=Path)
    parser.add_argument("--package-tests", type=Path)
    parser.add_argument("--request-tests", type=Path)
    args = parser.parse_args()
    if args.request_tests:
        return request_tests(args.request_tests)
    if args.package_tests:
        return package_tests(args.package_tests)
    if args.package:
        print(json.dumps(package_manifest(args.package), sort_keys=True))
        return 0
    if args.self_test:
        return run_tests()
    if args.clock_receipt:
        try:
            if args.clock_receipt.stat().st_size > 100_000:
                raise ValueError("clock receipt size")
            print(json.dumps(clock_report(json.loads(args.clock_receipt.read_text(encoding="utf-8"))), sort_keys=True))
        except (OSError, ValueError, KeyError, TypeError, ZeroDivisionError):
            print(json.dumps(dict(code="VP-CLOCK", visible_latency="Not assessed")))
            return 3
        return 0
    if not args.receipt or not args.expected:
        parser.error("use --self-test or both --receipt and --expected")
    try:
        if args.receipt.stat().st_size > 1_000_000 or args.expected.stat().st_size > 100_000:
            raise ValueError("receipt size")
        result = assess(json.loads(args.receipt.read_text(encoding="utf-8")), json.loads(args.expected.read_text(encoding="utf-8")))
    except (OSError, ValueError):
        result = dict(code="VP-SCHEMA", visible_latency="Not assessed")
    print(json.dumps(result, sort_keys=True))
    return 3


if __name__ == "__main__":
    for _stream in (sys.stdout, sys.stderr):
        if hasattr(_stream, "reconfigure"):
            try:
                _stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    raise SystemExit(main())
