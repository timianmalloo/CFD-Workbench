#!/usr/bin/env python3
"""Turn one apt simulation into a complete exact package/version/hash lock."""
from __future__ import annotations

import argparse
import json
import re
import subprocess
from pathlib import Path


INST = re.compile(r"^Inst\s+(\S+)(?:\s+\[([^]]+)\])?\s+\((\S+)\s+")
FORBIDDEN_DRIVER = re.compile(
    r"^(cuda-drivers?|nvidia-(?:driver|dkms|kernel|headless|open)|linux-(?:modules|objects)-nvidia)"
)
FORBIDDEN_SERIES = re.compile(r"(?:^|-)13-[34](?:$|-)")


def paragraphs(text: str) -> list[dict[str, str]]:
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
        if record:
            records.append(record)
    return records


def metadata(package: str, version: str) -> dict[str, str]:
    result = subprocess.run(
        ["apt-cache", "show", f"{package}={version}"],
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    base = package.split(":", 1)[0]
    matches = [
        record
        for record in paragraphs(result.stdout)
        if record.get("Package") == base and record.get("Version") == version
    ]
    unique = {
        tuple(record.get(field, "") for field in ("Package", "Version", "Architecture", "Filename", "SHA256")): record
        for record in matches
    }
    matches = list(unique.values())
    if len(matches) != 1:
        raise RuntimeError(f"expected one unique artifact for {package}={version}, found {len(matches)}")
    required = ("Architecture", "Filename", "Size", "SHA256")
    missing = [field for field in required if field not in matches[0]]
    if missing:
        raise RuntimeError(f"{package}={version} lacks {', '.join(missing)}")
    return matches[0]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--simulation", type=Path, required=True)
    parser.add_argument("--json", type=Path, required=True)
    parser.add_argument("--args", type=Path, required=True)
    args = parser.parse_args()

    lines = args.simulation.read_text(encoding="utf-8").splitlines()
    if any(line.startswith("Remv ") for line in lines):
        raise RuntimeError("apt simulation contains a removal")
    selected: dict[str, tuple[str | None, str]] = {}
    for line in lines:
        match = INST.match(line)
        if not match:
            continue
        package, old_version, version = match.groups()
        if package in selected:
            raise RuntimeError(f"duplicate apt transaction row for {package}")
        selected[package] = (old_version, version)
    if not selected:
        raise RuntimeError("apt simulation contains no install rows")

    packages = []
    for package in sorted(selected):
        old_version, version = selected[package]
        base = package.split(":", 1)[0]
        if FORBIDDEN_DRIVER.search(base):
            raise RuntimeError(f"Linux driver package is forbidden: {package}")
        if FORBIDDEN_SERIES.search(base):
            raise RuntimeError(f"CUDA 13.3/13.4 package is forbidden: {package}")
        if base in {"cuda", "cuda-toolkit"}:
            raise RuntimeError(f"rolling CUDA root is forbidden: {package}")
        record = metadata(package, version)
        packages.append(
            {
                "package": package,
                "version": version,
                "architecture": record["Architecture"],
                "old_version": old_version or "Not installed",
                "repository": "nvidia-cuda" if record["Filename"].startswith("./") else "ubuntu",
                "filename": record["Filename"],
                "download_bytes": int(record["Size"]),
                "installed_kib_metadata": int(record["Installed-Size"])
                if "Installed-Size" in record
                else "Not recorded",
                "sha256": record["SHA256"],
            }
        )

    exact = {item["package"]: item["version"] for item in packages}
    required_exact = {
        "cuda-toolkit-13-2": "13.2.2-1",
        "cuda-toolkit-config-common": "13.2.86-1",
        "cuda-toolkit-13-config-common": "13.2.86-1",
    }
    for package, version in required_exact.items():
        if exact.get(package) != version:
            raise RuntimeError(f"required exact package missing: {package}={version}")

    document = {
        "schema": "cfdw-cuda-package-lock/1",
        "installable_lock": False,
        "root": "cuda-toolkit-13-2=13.2.2-1",
        "recommendations": "excluded",
        "removals": 0,
        "package_count": len(packages),
        "new_count": sum(item["old_version"] == "Not installed" for item in packages),
        "upgrade_count": sum(item["old_version"] != "Not installed" for item in packages),
        "total_download_bytes_metadata": sum(item["download_bytes"] for item in packages),
        "packages": packages,
    }
    args.json.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
    args.args.write_text(
        "".join(f"{item['package']}={item['version']}\n" for item in packages),
        encoding="utf-8",
        newline="\n",
    )
    print(
        f"PACKAGE-LOCK-PASS packages={len(packages)} new={document['new_count']} "
        f"upgrades={document['upgrade_count']} bytes={document['total_download_bytes_metadata']}"
    )


if __name__ == "__main__":
    main()
