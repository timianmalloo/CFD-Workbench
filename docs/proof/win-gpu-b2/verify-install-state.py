#!/usr/bin/env python3
"""Fail closed unless the disposable distro matches the accepted package transition."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


FORBIDDEN_DRIVER = re.compile(
    r"^(?:cuda-drivers(?:$|-)|nvidia-(?:driver|dkms|kernel|headless|open)(?:$|-)|linux-(?:modules|objects)-nvidia(?:$|-))"
)
FORBIDDEN_SERIES = re.compile(r"-13-(?:3|4)(?:$|-)")


def read_snapshot(path: Path) -> dict[str, dict[str, str]]:
    result: dict[str, dict[str, str]] = {}
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        fields = line.split("\t")
        if len(fields) != 4:
            raise RuntimeError(f"invalid snapshot row {number}: {line!r}")
        package, version, architecture, status = fields
        if package in result:
            raise RuntimeError(f"duplicate package in snapshot: {package}")
        if status != "installed":
            continue
        result[package] = {"version": version, "architecture": architecture}
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--phase", choices=("before", "after"), required=True)
    parser.add_argument("--lock", type=Path, required=True)
    parser.add_argument("--snapshot", type=Path, required=True)
    parser.add_argument("--before", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    lock = json.loads(args.lock.read_text(encoding="utf-8"))
    if lock.get("installable_lock") is not True or lock.get("package_count") != 135:
        raise RuntimeError("accepted installable lock identity is invalid")
    expected = {item["package"]: item for item in lock["packages"]}
    if len(expected) != 135:
        raise RuntimeError("accepted lock has duplicate packages")
    current = read_snapshot(args.snapshot)

    mismatches: list[dict[str, str]] = []
    for package, item in expected.items():
        actual = current.get(package)
        wanted_version = item["old_version"] if args.phase == "before" else item["version"]
        if wanted_version == "Not installed":
            if actual is not None:
                mismatches.append({"package": package, "expected": wanted_version, "actual": actual["version"]})
        elif actual is None or actual["version"] != wanted_version or actual["architecture"] != item["architecture"]:
            mismatches.append(
                {
                    "package": package,
                    "expected": f"{wanted_version}/{item['architecture']}",
                    "actual": "Not installed" if actual is None else f"{actual['version']}/{actual['architecture']}",
                }
            )
    if mismatches:
        raise RuntimeError(f"{args.phase} package state mismatch: {mismatches[:5]}")

    changed: list[dict[str, str]] = []
    if args.phase == "after":
        if args.before is None:
            parser.error("--before is required for the after phase")
        before = read_snapshot(args.before)
        for package in sorted(set(before) | set(current)):
            left = before.get(package)
            right = current.get(package)
            if left != right:
                changed.append(
                    {
                        "package": package,
                        "before": "Not installed" if left is None else left["version"],
                        "after": "Not installed" if right is None else right["version"],
                    }
                )
        expected_changed = {name for name, item in expected.items() if item["old_version"] != item["version"]}
        actual_changed = {item["package"] for item in changed}
        if actual_changed != expected_changed:
            raise RuntimeError(
                f"package transition escaped lock: missing={sorted(expected_changed - actual_changed)[:5]} "
                f"extra={sorted(actual_changed - expected_changed)[:5]}"
            )

    forbidden = sorted(
        package for package in current if FORBIDDEN_DRIVER.search(package) or FORBIDDEN_SERIES.search(package)
    )
    if forbidden:
        raise RuntimeError(f"forbidden installed package: {forbidden[:5]}")

    document = {
        "schema": "cfdw-cuda-install-state/1",
        "phase": args.phase,
        "verified": True,
        "installed_package_count": len(current),
        "locked_package_count": len(expected),
        "changed_package_count": len(changed),
        "forbidden_packages": forbidden,
        "changed_packages": changed,
    }
    args.output.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(
        f"INSTALL-STATE-PASS phase={args.phase} installed={len(current)} "
        f"locked={len(expected)} changed={len(changed)}"
    )


if __name__ == "__main__":
    main()
