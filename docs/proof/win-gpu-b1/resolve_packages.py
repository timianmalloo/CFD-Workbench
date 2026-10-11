#!/usr/bin/env python3
"""Inventory one CUDA meta-package closure from retained NVIDIA Packages.gz."""
from __future__ import annotations

import argparse
import gzip
import json
import re
import subprocess
from collections import defaultdict, deque
from pathlib import Path


DEPENDENCY_SPLIT = re.compile(r",\s*(?![^()]*\))")
DEPENDENCY = re.compile(r"^([A-Za-z0-9+.-]+)(?::[A-Za-z0-9-]+)?(?:\s*\((<<|<=|=|>=|>>)\s*([^)]+)\))?")


def parse_packages(path: Path) -> list[dict[str, str]]:
    text = gzip.decompress(path.read_bytes()).decode("utf-8")
    records = []
    for paragraph in text.strip().split("\n\n"):
        record: dict[str, str] = {}
        current = None
        for line in paragraph.splitlines():
            if line.startswith((" ", "\t")) and current:
                record[current] += " " + line.strip()
                continue
            key, separator, value = line.partition(":")
            if separator:
                current = key
                record[key] = value.strip()
        if "Package" in record and "Version" in record:
            records.append(record)
    return records


def version_satisfies(version: str, operator: str | None, required: str | None) -> bool:
    if not operator:
        return True
    result = subprocess.run(
        ["dpkg", "--compare-versions", version, operator, required],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    )
    return result.returncode == 0


def newer(left: str, right: str) -> bool:
    return subprocess.run(
        ["dpkg", "--compare-versions", left, "gt", right],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    ).returncode == 0


def select(records: list[dict[str, str]], operator: str | None, required: str | None) -> dict[str, str] | None:
    candidates = [record for record in records if version_satisfies(record["Version"], operator, required)]
    if not candidates:
        return None
    winner = candidates[0]
    for candidate in candidates[1:]:
        if newer(candidate["Version"], winner["Version"]):
            winner = candidate
    return winner


def dependency_choices(text: str) -> list[list[tuple[str, str | None, str | None]]]:
    groups = []
    for group in DEPENDENCY_SPLIT.split(text):
        choices = []
        for alternative in group.split("|"):
            match = DEPENDENCY.match(alternative.strip())
            if match:
                choices.append((match.group(1), match.group(2), match.group(3)))
        if choices:
            groups.append(choices)
    return groups


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--packages", type=Path, required=True)
    parser.add_argument("--root", required=True)
    parser.add_argument("--version")
    parser.add_argument("--architecture", default="amd64")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    all_records = parse_packages(args.packages)
    by_name: dict[str, list[dict[str, str]]] = defaultdict(list)
    for record in all_records:
        if record.get("Architecture") in (args.architecture, "all"):
            by_name[record["Package"]].append(record)

    queue = deque([(args.root, "=" if args.version else None, args.version, "root")])
    selected: dict[str, dict[str, str]] = {}
    external: list[dict[str, object]] = []
    while queue:
        name, operator, required, parent = queue.popleft()
        if name in selected:
            if not version_satisfies(selected[name]["Version"], operator, required):
                raise RuntimeError(f"selected {name}={selected[name]['Version']} violates {operator} {required}")
            continue
        record = select(by_name.get(name, []), operator, required)
        if record is None:
            external.append({"package": name, "operator": operator, "version": required, "required_by": parent})
            continue
        required_fields = ("Architecture", "Filename", "Size", "SHA256")
        missing = [field for field in required_fields if field not in record]
        if missing:
            raise RuntimeError(f"{name}={record['Version']} lacks {', '.join(missing)}")
        selected[name] = record
        for field in ("Pre-Depends", "Depends"):
            for choices in dependency_choices(record.get(field, "")):
                chosen = next((choice for choice in choices if choice[0] in by_name), choices[0])
                queue.append((*chosen, f"{name}={record['Version']}"))

    packages = []
    for name in sorted(selected):
        record = selected[name]
        packages.append(
            {
                "package": name,
                "version": record["Version"],
                "architecture": record["Architecture"],
                "filename": record["Filename"],
                "download_bytes": int(record["Size"]),
                "installed_kib": int(record["Installed-Size"]) if "Installed-Size" in record else "Not recorded",
                "sha256": record["SHA256"],
                "depends": record.get("Depends", ""),
                "license": record.get("License", "Not recorded"),
            }
        )
    blocking_reasons = [
        "repository InRelease signature is retained but not verified",
        "NVIDIA EULA is retained separately rather than declared per package",
    ]
    if external:
        blocking_reasons.append("dependencies outside the NVIDIA repository are unresolved")
    if any(item["installed_kib"] == "Not recorded" for item in packages):
        blocking_reasons.append("at least one installed size is not recorded")
    document = {
        "schema": "cfdw-cuda-package-inventory/1",
        "installable_lock": False,
        "blocking_reasons": blocking_reasons,
        "root": args.root,
        "architecture": args.architecture,
        "requested_version": args.version or "latest in retained metadata",
        "package_count": len(packages),
        "total_download_bytes": sum(item["download_bytes"] for item in packages),
        "total_installed_kib": "Not recorded" if any(
            item["installed_kib"] == "Not recorded" for item in packages
        ) else sum(item["installed_kib"] for item in packages),
        "packages": packages,
        "external_dependencies": external,
    }
    args.output.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
