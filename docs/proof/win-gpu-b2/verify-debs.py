#!/usr/bin/env python3
"""Verify downloaded package bytes, measure payloads, and bind license notices."""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import tarfile
from pathlib import Path


GIB = 1024**3
UNPACK_RESERVE_BYTES = 2 * GIB
BUILD_RESERVE_BYTES = 1 * GIB
HEADROOM_BYTES = 10 * GIB


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def field(path: Path, name: str) -> str:
    return subprocess.run(
        ["dpkg-deb", "--field", str(path), name],
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
    ).stdout.strip()


def payload(path: Path) -> tuple[int, list[dict[str, object]], list[dict[str, str]]]:
    process = subprocess.Popen(["dpkg-deb", "--fsys-tarfile", str(path)], stdout=subprocess.PIPE)
    if process.stdout is None:
        raise RuntimeError(f"cannot read package payload: {path}")
    total = 0
    notices = []
    doc_links = []
    with tarfile.open(fileobj=process.stdout, mode="r|*") as archive:
        for member in archive:
            normalized = member.name.removeprefix("./")
            if member.issym() and normalized.startswith("usr/share/doc/"):
                doc_links.append({"path": member.name, "target": member.linkname})
            if not member.isfile():
                continue
            total += member.size
            base = Path(member.name).name.lower()
            if base == "copyright" or "license" in base or "notice" in base or "eula" in base:
                extracted = archive.extractfile(member)
                if extracted is None:
                    raise RuntimeError(f"cannot read notice {member.name} from {path.name}")
                data = extracted.read()
                notices.append(
                    {"path": member.name, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}
                )
    if process.wait() != 0:
        raise RuntimeError(f"dpkg-deb payload stream failed: {path}")
    return total, notices, doc_links


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lock", type=Path, required=True)
    parser.add_argument("--archives", type=Path, required=True)
    parser.add_argument("--eula", type=Path, required=True)
    parser.add_argument("--verification", type=Path, required=True)
    parser.add_argument("--licenses", type=Path, required=True)
    parser.add_argument("--capacity", type=Path, required=True)
    args = parser.parse_args()

    lock = json.loads(args.lock.read_text(encoding="utf-8"))
    expected = {(item["package"].split(":", 1)[0], item["version"]): item for item in lock["packages"]}
    found = {}
    license_rows = []
    total_payload = 0
    total_download = 0
    eula_hash = sha256(args.eula)
    for deb in sorted(args.archives.glob("*.deb")):
        package = field(deb, "Package")
        version = field(deb, "Version")
        key = (package, version)
        if key not in expected:
            raise RuntimeError(f"downloaded package is outside lock: {package}={version}")
        if key in found:
            raise RuntimeError(f"duplicate downloaded package: {package}={version}")
        item = expected[key]
        actual_hash = sha256(deb)
        actual_size = deb.stat().st_size
        if actual_hash != item["sha256"] or actual_size != item["download_bytes"]:
            raise RuntimeError(f"package bytes mismatch: {package}={version}")
        payload_bytes, notices, doc_links = payload(deb)
        basis = (
            {"kind": "cuda-13.2.2-eula", "sha256": eula_hash}
            if item["repository"] == "nvidia-cuda"
            else {"kind": "embedded-package-notice"}
        )
        found[key] = {
            "package": package,
            "version": version,
            "architecture": field(deb, "Architecture"),
            "deb": deb.name,
            "bytes": actual_size,
            "sha256": actual_hash,
            "payload_bytes": payload_bytes,
        }
        license_rows.append(
            {
                "package": package,
                "version": version,
                "repository": item["repository"],
                "basis": basis,
                "notices": notices,
                "doc_links": doc_links,
            }
        )
        total_download += actual_size
        total_payload += payload_bytes
    missing = sorted(set(expected) - set(found))
    if missing:
        raise RuntimeError(f"locked packages were not downloaded: {missing[:5]}")

    license_by_package = {row["package"]: row for row in license_rows}
    for row in license_rows:
        if row["repository"] != "ubuntu" or row["notices"]:
            continue
        candidates = []
        for link in row["doc_links"]:
            if Path(link["path"]).name != row["package"]:
                continue
            target = Path(link["target"]).name
            if target in license_by_package:
                candidates.append(target)
        if len(candidates) != 1:
            raise RuntimeError(
                f"Ubuntu package has no unique embedded/shared license notice: {row['package']}={row['version']}"
            )
        target = license_by_package[candidates[0]]
        if not target["notices"]:
            raise RuntimeError(f"shared license target has no notice: {candidates[0]}")
        row["basis"] = {
            "kind": "shared-package-notice",
            "target_package": target["package"],
            "target_version": target["version"],
        }

    usage = shutil.disk_usage("/")
    required = total_download + total_payload + UNPACK_RESERVE_BYTES + BUILD_RESERVE_BYTES + HEADROOM_BYTES
    if usage.free < required:
        raise RuntimeError(f"disposable filesystem capacity is insufficient: {usage.free} < {required}")
    verification = {
        "schema": "cfdw-cuda-download-verification/1",
        "verified": True,
        "package_count": len(found),
        "total_download_bytes": total_download,
        "total_payload_bytes": total_payload,
        "packages": [found[key] for key in sorted(found)],
    }
    licenses = {
        "schema": "cfdw-cuda-license-map/1",
        "complete": True,
        "cuda_eula": {"file": args.eula.name, "bytes": args.eula.stat().st_size, "sha256": eula_hash},
        "packages": sorted(license_rows, key=lambda item: (item["package"], item["version"])),
    }
    capacity = {
        "schema": "cfdw-cuda-capacity/1",
        "filesystem_total_bytes": usage.total,
        "filesystem_used_bytes": usage.used,
        "filesystem_free_bytes": usage.free,
        "download_bytes": total_download,
        "payload_bytes": total_payload,
        "unpack_reserve_bytes": UNPACK_RESERVE_BYTES,
        "build_output_reserve_bytes": BUILD_RESERVE_BYTES,
        "post_install_headroom_bytes": HEADROOM_BYTES,
        "required_free_bytes": required,
        "filesystem_gate_passed": True,
    }
    for path, document in (
        (args.verification, verification),
        (args.licenses, licenses),
        (args.capacity, capacity),
    ):
        path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
    lock["installable_lock"] = True
    lock["download_verification"] = args.verification.name
    lock["license_map"] = args.licenses.name
    lock["capacity"] = args.capacity.name
    args.lock.write_text(json.dumps(lock, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(
        f"DEB-LOCK-PASS packages={len(found)} download_bytes={total_download} "
        f"payload_bytes={total_payload} required_free_bytes={required} free_bytes={usage.free}"
    )


if __name__ == "__main__":
    main()
