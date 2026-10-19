#!/usr/bin/env python3
"""Ruling 217 metadata-only apt lock and downloaded-archive inspection.

Lock model (Ruling 237 (1), plan f1-rescope-r237 node C3): the roots are pins the publisher keeps (release roots and
unversioned tool names); the run on each architecture resolves them and writes its own transaction-lock.json,
hashed against that run's signed indices. No prior lock is an input.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
import tarfile
from pathlib import Path
from urllib.parse import unquote, urlsplit

import architecture

INST = re.compile(r"^Inst\s+(\S+)(?:\s+\[([^]]+)\])?\s+\((\S+)\s+")
URI = re.compile(r"^'([^']+)'\s+(\S+)\s+(\d+)\s+(\S+)$")
FORBIDDEN = re.compile(r"^(?:cuda-drivers(?:$|-)|nvidia-(?:driver|dkms|kernel|headless|open)(?:$|-)|linux-(?:modules|objects)-nvidia(?:$|-))")
PINS = (
    "cuda-toolkit-13-2=13.2.2-1",
    "openfoam2512=2512.0-2", "openfoam2512-common=2512.0-2",
    "openfoam2512-dev=2512.0-2", "openfoam2512-source=2512.0-2",
    "openfoam2512-tools=2512.0-2", "gcc-13", "g++-13", "gfortran-13", "openmpi-bin",
    "libopenmpi-dev", "libblas-dev", "liblapack-dev", "pkg-config",
)


def pinned_roots(token: str) -> tuple[str, ...]:
    """The pins the run resolves: the toolkit and OpenFOAM release roots, the tool names, and `make` per architecture."""
    return PINS + (architecture.row(token)["make_root"],)


def check_pins(rows: list[tuple[str, str, str]], pins: tuple[str, ...] | list[str]) -> None:
    """Every pin is an install row of the simulated transaction, at the pinned version when one is given."""
    versions = {name: version for name, _old, version in rows}
    for pin in pins:
        name, _sep, version = pin.partition("=")
        name = name.split(":", 1)[0]   # make:<arch> is the row `make` on its native architecture
        if name not in versions or (version and versions[name] != version):
            raise ValueError(f"pinned root absent from the transaction: {pin}")


def write_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True, allow_nan=False) + "\n", encoding="utf-8", newline="\n")


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def paragraphs(text: str) -> list[dict[str, str]]:
    records: list[dict[str, str]] = []
    for part in re.split(r"\n\s*\n", text.strip()):
        row: dict[str, str] = {}
        current = ""
        for line in part.splitlines():
            if line[:1].isspace() and current:
                row[current] += " " + line.strip()
            else:
                key, separator, value = line.partition(":")
                if separator:
                    current = key
                    row[key] = value.strip()
        if row:
            records.append(row)
    return records


def roots(args: argparse.Namespace) -> None:
    values = pinned_roots(args.architecture)
    args.output.write_text("\n".join(values) + "\n", encoding="utf-8", newline="\n")
    print(f"ROOTS-PASS pins={len(values)} architecture={args.architecture}")


def metadata(package: str, version: str) -> dict[str, str]:
    base = package.split(":", 1)[0]
    result = subprocess.run(["apt-cache", "show", "-a", base], check=True,
                            capture_output=True, text=True, encoding="utf-8", timeout=30)
    matches = [row for row in paragraphs(result.stdout)
               if row.get("Package") == base and row.get("Version") == version]
    identities = {(row.get("Architecture"), row.get("Filename"), row.get("SHA256")) for row in matches}
    if len(identities) != 1 or not matches:
        raise ValueError(f"ambiguous apt metadata: {package}={version}")
    row = matches[0]
    if any(key not in row for key in ("Architecture", "Filename", "Size", "SHA256")):
        raise ValueError(f"incomplete apt metadata: {package}={version}")
    return row


def simulation_rows(path: Path) -> list[tuple[str, str, str]]:
    lines = path.read_text(encoding="utf-8").splitlines()
    if any(line.startswith("Remv ") for line in lines):
        raise ValueError("apt simulation contains a removal")
    rows = []
    names = set()
    for line in lines:
        match = INST.match(line)
        if match:
            name, old, version = match.groups()
            if name in names or FORBIDDEN.search(name):
                raise ValueError(f"duplicate or forbidden transaction row: {name}")
            names.add(name)
            rows.append((name, old or "Not installed", version))
    if not rows:
        raise ValueError("apt simulation has no install rows")
    return sorted(rows)


def compare(args: argparse.Namespace) -> None:
    if simulation_rows(args.first) != simulation_rows(args.second):
        raise ValueError("exact pinned apt simulation changed the transaction")
    print("EXACT-SIMULATION-PASS")


def uri_rows(path: Path) -> dict[str, tuple[str, int]]:
    result: dict[str, tuple[str, int]] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        match = URI.match(line)
        if not match:
            continue
        url, filename, size, _ = match.groups()
        if not url.startswith(("https://", "http://")):
            raise ValueError(f"unexpected package URI scheme: {url}")
        if filename in result:
            raise ValueError(f"duplicate package URI filename: {filename}")
        result[filename] = (url, int(size))
    if not result:
        raise ValueError("apt URI set is empty")
    return result


def uri_basename(url: str) -> str:
    # Decode the URL's last segment once; never reinterpret decoded separators.
    encoded = urlsplit(url).path.rsplit("/", 1)[-1]
    if re.search(r"%(?![0-9A-Fa-f]{2})", encoded):
        raise ValueError(f"malformed package URI escape: {url}")
    basename = unquote(encoded, errors="strict")
    if not basename or basename in (".", "..") or re.search(r"[/\\\x00-\x1f\x7f]", basename):
        raise ValueError(f"unsafe package URI basename: {url}")
    return basename


def match_uri(filename: str, size: int, uris: dict[str, tuple[str, int]]) -> tuple[str, str]:
    matches = [(cache_name, url) for cache_name, (url, uri_size) in uris.items()
               if uri_basename(url) == filename and uri_size == size]
    if len(matches) != 1:
        raise ValueError(f"expected one signed-index URI for {filename}/{size}, found {len(matches)}")
    return matches[0]


def lock(args: argparse.Namespace) -> None:
    token = args.architecture
    pins = pinned_roots(token)
    selected = simulation_rows(args.simulation)
    check_pins(selected, pins)   # before any index read: an absent pin never reaches apt-cache
    uris = uri_rows(args.uris)
    packages = []
    consumed = set()
    for name, old, version in selected:
        row = metadata(name, version)
        if row["Architecture"] not in (token, "all"):
            raise ValueError(f"row architecture differs from the host: {name} is {row['Architecture']}, host {token}")
        filename = Path(row["Filename"]).name
        cache_name, url = match_uri(filename, int(row["Size"]), uris)
        consumed.add(cache_name)
        packages.append({
            "package": name, "old_version": old, "version": version,
            "architecture": row["Architecture"], "repository_filename": row["Filename"],
            "url": url, "apt_cache_filename": cache_name,
            "sha256_signed_index": row["SHA256"],
            "download_bytes_signed_index": int(row["Size"]),
            "installed_kib_metadata": int(row["Installed-Size"]) if "Installed-Size" in row else "Not recorded",
            "depends": row.get("Depends", "Not recorded"),
        })
    if consumed != set(uris):
        raise ValueError(f"URI/transaction set differs: unused={sorted(set(uris)-consumed)[:5]}")
    document = {
        "schema": "cfdw-f1-r217-transaction-lock/2", "installable": False, "architecture": token,
        "pins": list(pins),
        "removals": 0, "recommendations": "excluded", "packages": packages,
        "package_count": len(packages),
        "total_download_bytes_signed_index": sum(x["download_bytes_signed_index"] for x in packages),
        "installed_kib_known": sum(x["installed_kib_metadata"] for x in packages
                                   if isinstance(x["installed_kib_metadata"], int)),
        "installed_kib_missing": [x["package"] for x in packages
                                  if not isinstance(x["installed_kib_metadata"], int)],
    }
    write_json(args.output, document)
    args.args.write_text("".join(f"{x['package']}={x['version']}\n" for x in packages),
                         encoding="utf-8", newline="\n")
    print(f"LOCK-PASS architecture={token} pins={len(pins)} packages={len(packages)} "
          f"download={document['total_download_bytes_signed_index']}")


def control_field(path: Path, name: str) -> str:
    return subprocess.run(["dpkg-deb", "--field", str(path), name], check=True,
                          capture_output=True, text=True, encoding="utf-8", timeout=30).stdout.strip()


def reconcile_installed_size(index_value: object, control_value: str) -> tuple[int, str]:
    if not control_value.isdecimal() or int(control_value) <= 0:
        raise ValueError("archive control Installed-Size missing/invalid")
    actual = int(control_value)
    if index_value == "Not recorded":
        return actual, "control-only-index-absent"
    if not isinstance(index_value, int) or index_value <= 0:
        raise ValueError("signed-index Installed-Size invalid")
    if index_value != actual:
        raise ValueError(f"Installed-Size disagreement: signed-index={index_value} control={actual}")
    return actual, "signed-index-and-control"


def capacity_observations(guest_path: Path, host_path: Path) -> dict[str, object]:
    guest = dict(line.split("=", 1) for line in guest_path.read_text(encoding="utf-8").splitlines()
                 if "=" in line)
    needed = ("block_bytes", "filesystem_bytes", "free_bytes", "maximum_bytes")
    if any(not guest.get(name, "").isdecimal() for name in needed):
        raise ValueError("guest capacity measurement missing")
    host = [json.loads(line) for line in host_path.read_text(encoding="utf-8").splitlines() if line]
    if not host or any(not isinstance(row.get("host_free_bytes"), int) or
                       not isinstance(row.get("host_floor_bytes"), int) for row in host):
        raise ValueError("host capacity measurement missing")
    physical = [row["target_vhd_physical_bytes"] for row in host
                if isinstance(row.get("target_vhd_physical_bytes"), int)]
    if not physical:
        raise ValueError("target physical VHD measurement missing")
    return {"guest_block_bytes": int(guest["block_bytes"]),
            "guest_filesystem_bytes": int(guest["filesystem_bytes"]),
            "guest_free_bytes_at_probe": int(guest["free_bytes"]),
            "guest_maximum_bytes": int(guest["maximum_bytes"]),
            "host_min_free_bytes": min(row["host_free_bytes"] for row in host),
            "host_max_required_floor_bytes": max(row["host_floor_bytes"] for row in host),
            "target_vhd_max_physical_bytes": max(physical),
            "host_samples": len(host)}


def inspect_payload(path: Path) -> tuple[int, list[dict[str, object]], list[dict[str, str]]]:
    process = subprocess.Popen(["dpkg-deb", "--fsys-tarfile", str(path)], stdout=subprocess.PIPE)
    if process.stdout is None:
        raise ValueError(f"no payload stream: {path.name}")
    total = 0
    notices: list[dict[str, object]] = []
    links: list[dict[str, str]] = []
    with tarfile.open(fileobj=process.stdout, mode="r|*") as archive:
        for member in archive:
            normalized = member.name.removeprefix("./")
            if member.issym() and normalized.startswith("usr/share/doc/"):
                links.append({"path": normalized, "target": member.linkname})
            if not member.isfile():
                continue
            total += member.size
            basename = Path(normalized).name.lower()
            if normalized.startswith("usr/share/doc/") and (
                    basename == "copyright" or "license" in basename or "notice" in basename or "eula" in basename):
                stream = archive.extractfile(member)
                if stream is None:
                    raise ValueError(f"unreadable notice: {path.name}:{normalized}")
                data = stream.read()
                notices.append({"path": normalized, "bytes": len(data),
                                "sha256": hashlib.sha256(data).hexdigest()})
    if process.wait(timeout=60) != 0:
        raise ValueError(f"payload reader failed: {path.name}")
    return total, notices, links


def verify(args: argparse.Namespace) -> None:
    lock_data = json.loads(args.lock.read_text(encoding="utf-8"))
    b2_licences = json.loads(args.b2_licences.read_text(encoding="utf-8"))
    b2_by_name = {row["package"]: row for row in b2_licences["packages"]}
    packages = lock_data["packages"]
    expected = {(x["package"].split(":", 1)[0], x["version"]): x for x in packages}
    found = {}
    licences = []
    size_disagreements = []
    for path in sorted(args.archives.glob("*.deb")):
        name, version, arch = (control_field(path, field) for field in ("Package", "Version", "Architecture"))
        key = (name, version)
        if key not in expected or key in found:
            raise ValueError(f"unexpected/duplicate archive: {name}={version}")
        item = expected[key]
        actual_hash, actual_size = digest(path), path.stat().st_size
        if (actual_hash != item["sha256_signed_index"] or
                actual_size != item["download_bytes_signed_index"] or arch != item["architecture"]):
            raise ValueError(f"archive differs from signed-index row: {name}={version}")
        installed_field = control_field(path, "Installed-Size")
        try:
            installed_kib, size_basis = reconcile_installed_size(item["installed_kib_metadata"], installed_field)
        except ValueError as error:
            installed_kib = int(installed_field) if installed_field.isdecimal() else "Not recorded"
            size_basis = "disagrees-or-missing"
            size_disagreements.append({"package": name, "signed_index": item["installed_kib_metadata"],
                                       "archive_control": installed_field or "Not recorded", "error": str(error)})
        payload_bytes, notices, links = inspect_payload(path)
        item.update({"sha256_downloaded": actual_hash, "download_bytes_actual": actual_size,
                     "installed_kib_control": installed_kib, "installed_size_basis": size_basis,
                     "payload_bytes": payload_bytes,
                     "archive_filename": path.name})
        licences.append({"package": name, "version": version, "notices": notices,
                         "doc_links": links, "basis": "embedded-or-shared-package-notice" if notices or links else "Not recorded"})
        found[key] = path.name
    if set(found) != set(expected):
        raise ValueError(f"archive set incomplete: {len(found)}/{len(expected)}")
    by_name = {x["package"]: x for x in licences}
    for row in licences:
        if row["notices"]:
            continue
        targets = [Path(x["target"]).name for x in row["doc_links"]]
        if len(targets) == 1 and targets[0] in by_name and by_name[targets[0]]["notices"]:
            row["basis"] = {"shared_notice_package": targets[0]}
        elif len(targets) == 1 and (Path("/usr/share/doc") / targets[0] / "copyright").is_file():
            notice = Path("/usr/share/doc") / targets[0] / "copyright"
            row["basis"] = {"rootfs_shared_notice": targets[0], "sha256": digest(notice)}
        elif row["package"] in b2_by_name and b2_by_name[row["package"]]["basis"]["kind"] == "cuda-13.2.2-eula":
            row["basis"] = {"prior_b2_eula": b2_licences["cuda_eula"]}
        else:
            row["basis"] = "Not recorded"
    totals = {
        "download_bytes": sum(x["download_bytes_actual"] for x in packages),
        "archive_payload_bytes": sum(x["payload_bytes"] for x in packages),
        "installed_kib_known": sum(x["installed_kib_control"] for x in packages
                                   if isinstance(x["installed_kib_control"], int)),
        "installed_kib_missing": [x["package"] for x in packages
                                  if not isinstance(x["installed_kib_control"], int)],
        "license_notices_missing": [x["package"] for x in licences if x["basis"] == "Not recorded"],
        "installed_size_disagreements": size_disagreements,
    }
    lock_data["verified_against_signed_index"] = not size_disagreements
    lock_data["totals"] = totals
    write_json(args.lock, lock_data)
    write_json(args.licences, {"schema": "cfdw-f1-r217-license-map/1", "rows": licences,
                               "complete": not totals["license_notices_missing"]})
    measured = capacity_observations(args.guest_capacity, args.host_capacity)
    write_json(args.capacity, {"schema": "cfdw-f1-r217-capacity/1", **totals, **measured,
                               "source_archive_bytes": "Not recorded", "unpack_reserve_bytes": "Not recorded",
                               "build_output_reserve_bytes": "Not recorded", "case_bytes": "Not recorded",
                               "current_preflight_vhd_cap_bytes": 8 * 1024**3,
                               "future_f1_proposed_headroom_bytes": 10 * 1024**3,
                               "full_f1_threshold_bytes": "Flagged: future source/build/case capacity not measured"})
    if totals["installed_kib_missing"] or totals["license_notices_missing"] or size_disagreements:
        raise ValueError("installed-size/index reconciliation or licence closure incomplete; evidence retained")
    print(f"ARCHIVES-PASS packages={len(packages)} bytes={totals['download_bytes']}")


def self_test() -> None:
    from tempfile import TemporaryDirectory
    with TemporaryDirectory() as temporary:
        path = Path(temporary) / "simulation"
        path.write_text("Inst good (1 Ubuntu:24.04 [amd64])\n", encoding="utf-8")
        assert simulation_rows(path) == [("good", "Not installed", "1")]
        path.write_text("Remv bad [1]\nInst good (1 Ubuntu:24.04 [amd64])\n", encoding="utf-8")
        try:
            simulation_rows(path)
        except ValueError:
            pass
        else:
            raise AssertionError("planted removal passed")
        path.write_text("Inst nvidia-driver-590 (1 Ubuntu:24.04 [amd64])\n", encoding="utf-8")
        try:
            simulation_rows(path)
        except ValueError:
            pass
        else:
            raise AssertionError("planted Linux driver passed")
    assert len(paragraphs("Package: a\nVersion: 1\n\nPackage: b\nVersion: 2\n")) == 2
    # The fixture names are not build tools: authority_scan.py refuses a quoted compiler or make literal in the packet
    # (found at plan node V); the real make:<arch> case runs in tests/test_f1_transaction_roots.py.
    rows = [("pkg-config", "Not installed", "1.8"), ("libblas-dev", "Not installed", "3.12")]
    check_pins(rows, ("pkg-config:arm64", "libblas-dev"))
    for pins in (("pkg-config:arm64", "libblas-dev", "liblapack-dev"), ("pkg-config=1.9",)):
        try:
            check_pins(rows, pins)
        except ValueError:
            pass
        else:
            raise AssertionError("planted absent pin passed")
    uri = "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/./nsight_1-1_amd64.deb"
    assert match_uri("nsight_1-1_amd64.deb", 5, {"nsight_1-virtual_amd64.deb": (uri, 5)}) == (
        "nsight_1-virtual_amd64.deb", uri)
    print("transaction self-test: PASS")


def main() -> None:
    parser = argparse.ArgumentParser()
    commands = parser.add_subparsers(dest="command", required=True)
    roots_parser = commands.add_parser("roots")
    roots_parser.add_argument("--architecture", required=True)
    roots_parser.add_argument("--output", type=Path, required=True)
    lock_parser = commands.add_parser("lock")
    lock_parser.add_argument("--simulation", type=Path, required=True)
    lock_parser.add_argument("--uris", type=Path, required=True)
    lock_parser.add_argument("--output", type=Path, required=True)
    lock_parser.add_argument("--args", type=Path, required=True)
    lock_parser.add_argument("--architecture", required=True)
    verify_parser = commands.add_parser("verify")
    verify_parser.add_argument("--lock", type=Path, required=True)
    verify_parser.add_argument("--archives", type=Path, required=True)
    verify_parser.add_argument("--licences", type=Path, required=True)
    verify_parser.add_argument("--capacity", type=Path, required=True)
    verify_parser.add_argument("--b2-licences", type=Path, required=True)
    verify_parser.add_argument("--guest-capacity", type=Path, required=True)
    verify_parser.add_argument("--host-capacity", type=Path, required=True)
    compare_parser = commands.add_parser("compare")
    compare_parser.add_argument("--first", type=Path, required=True)
    compare_parser.add_argument("--second", type=Path, required=True)
    commands.add_parser("self-test")
    args = parser.parse_args()
    {"roots": roots, "lock": lock, "verify": verify, "compare": compare,
     "self-test": lambda _: self_test()}[args.command](args)


if __name__ == "__main__":
    try:
        main()
    except (OSError, subprocess.SubprocessError, ValueError, KeyError) as error:
        print(f"transaction: {error}", file=sys.stderr)
        raise SystemExit(5)
