#!/usr/bin/env python3
"""Rulings 237 and 240: the host CPU architecture is detected and recorded, never assumed.

One module maps the Windows and Linux architecture names to the canonical dpkg token exactly once and holds the
per-architecture table (rootfs pin, Ubuntu archive host, NVIDIA repository, native compiler packages, make root)
that contract.py, transaction.py, preflight.sh and the launcher read. The launcher writes host-identity.json from
its observations before the first WSL effect (Ruling 238 (1): every receipt records the OS architecture, CPU and
GPU); the guest's probe stage records what the fresh distro reports, and `check` refuses a host/guest mismatch
before any network stage. A refused host identity is still written (architecture null or the detected token, plus
the refusal) so the cleanup evidence can freeze it: a measurement degrades to "not recorded", never to a guess.
"""
from __future__ import annotations

import argparse
import datetime as _datetime
import json
import sys
from pathlib import Path

SCHEMA_OBSERVATIONS = "cfdw-f1-host-observations/1"
SCHEMA_IDENTITY = "cfdw-f1-host-identity/1"
TABLE = {
    "amd64": {
        "windows": "X64", "processor_env": "AMD64", "dpkg": "amd64", "uname_m": "x86_64",
        # Win32_OperatingSystem.OSArchitecture on the old x64 PC: docs/proof/win-setup/receipt.md ("OSArchitecture: 64-bit").
        "cim": "64-bit",
        "rootfs_name": "ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz",
        "rootfs_sha256": "8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea",
        "ubuntu_archive": "http://archive.ubuntu.com/ubuntu",
        "ubuntu_security": "http://security.ubuntu.com/ubuntu",
        "nvidia_repository": "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/",
        "compiler_triplet": "x86-64-linux-gnu",
        "make_root": "make:amd64",
    },
    "arm64": {
        "windows": "Arm64", "processor_env": "ARM64", "dpkg": "arm64", "uname_m": "aarch64",
        # The arm64-spike host-identity receipts (os_architecture_cim), both runtimes.
        "cim": "ARM 64-bit Processor",
        "rootfs_name": "ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz",
        "rootfs_sha256": "fecec1d9b7b750c12c109edb49c13c1006f4a2efabb9b8bf341f11c4c9f2ef11",
        "ubuntu_archive": "http://ports.ubuntu.com/ubuntu-ports",
        "ubuntu_security": "http://ports.ubuntu.com/ubuntu-ports",
        "nvidia_repository": "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/sbsa/",
        "compiler_triplet": "aarch64-linux-gnu",
        "make_root": "make:arm64",
    },
}
UBUNTU_KEYRING = "/usr/share/keyrings/ubuntu-archive-keyring.gpg"


def require_token(value: object) -> str:
    if not isinstance(value, str) or value not in TABLE:
        raise ValueError(f"architecture must be one of {', '.join(sorted(TABLE))}: {value!r}")
    return value


def row(token: str) -> dict[str, str]:
    return TABLE[require_token(token)]


def native_compiler_packages(token: str) -> tuple[str, str, str]:
    triplet = row(token)["compiler_triplet"]
    return (f"cpp-13-{triplet}", f"gcc-13-{triplet}", f"g++-13-{triplet}")


def linux_token(dpkg_architecture: object, uname_m: object) -> str:
    for token, entry in TABLE.items():
        if dpkg_architecture == entry["dpkg"]:
            if uname_m != entry["uname_m"]:
                raise ValueError(f"dpkg {dpkg_architecture!r} and uname -m {uname_m!r} disagree")
            return token
    raise ValueError(f"unmapped dpkg architecture: {dpkg_architecture!r}")


def cim_token(os_architecture_cim: object) -> str:
    """The host's architecture from Win32_OperatingSystem.OSArchitecture, the one source an emulated process cannot
    repaint: measured 2026-10-19, Windows PowerShell 5.1 on the ARM64 laptop is an ARM64X image that runs as an x64
    process when an x64 parent starts it, and then RuntimeInformation and PROCESSOR_ARCHITECTURE both say x64 while
    CIM still says "ARM 64-bit Processor" (register entry EMULATED-PROCESS-REPORTS-AN-EMULATED-OS)."""
    for token, entry in TABLE.items():
        if os_architecture_cim == entry["cim"]:
            return token
    raise ValueError(f"unmapped CIM OS architecture: {os_architecture_cim!r}")


def detect(windows: dict, linux: dict | None = None) -> str:
    """The canonical token from the launcher's observations, and the guest's when known; any disagreement refuses."""
    token = cim_token(windows.get("os_architecture_cim"))
    entry = TABLE[token]
    seen = {key: windows.get(key) for key in ("os_architecture", "process_architecture", "processor_architecture_env")}
    expected = {"os_architecture": entry["windows"], "process_architecture": entry["windows"],
                "processor_architecture_env": entry["processor_env"]}
    if seen != expected:
        raise ValueError(f"the launcher runs under emulation on this {token} host (process view {seen}, native view "
                         f"{expected}): start the native PowerShell for {token}")
    if linux is not None:
        guest = linux_token(linux.get("dpkg_architecture"), linux.get("uname_m"))
        if guest != token:
            raise ValueError(f"guest architecture {guest} differs from the host {token}")
    return token


def _names(value: object, label: str) -> list[str]:
    if not isinstance(value, list) or not value or any(not isinstance(x, str) or not x.strip() for x in value):
        raise ValueError(f"host observations carry no {label} name")
    return list(value)


def host_identity(observations: dict, expected: str | None) -> dict:
    """The receipt. `architecture` is the detected token or None; `refusal` says why the run must not go on."""
    if observations.get("schema") != SCHEMA_OBSERVATIONS:
        raise ValueError("host observations schema mismatch")
    windows = {key: observations.get(key) for key in
               ("os_architecture_cim", "os_architecture", "process_architecture", "processor_architecture_env",
                "processor_architew6432_env", "os_version", "powershell_version", "powershell_edition")}
    receipt = {
        "schema": SCHEMA_IDENTITY,
        "recorded_utc": _datetime.datetime.now(_datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "observed_utc": observations.get("utc"),
        "architecture": None,
        "windows": windows,
        "cpu": _names(observations.get("cpu"), "CPU"),
        "gpu": _names(observations.get("gpu"), "GPU"),
        "gpgv": {"path": observations.get("gpgv_path") or "Not recorded",   # Ruling 240 (2): the Windows-side verifier
                 "version": observations.get("gpgv_version") or "Not recorded"},
        "refusal": None,
    }
    try:
        receipt["architecture"] = detect(windows)
        if expected is not None and receipt["architecture"] != require_token(expected):
            raise ValueError(f"contract prepared for {expected}, host is {receipt['architecture']}")
    except ValueError as error:
        receipt["refusal"] = str(error)
    return receipt


def sources(token: str) -> list[str]:
    entry = row(token)
    return [
        f"deb [arch={token} signed-by={UBUNTU_KEYRING}] {entry['ubuntu_archive']} noble main universe",
        f"deb [arch={token} signed-by={UBUNTU_KEYRING}] {entry['ubuntu_archive']} noble-updates main universe",
        f"deb [arch={token} signed-by={UBUNTU_KEYRING}] {entry['ubuntu_security']} noble-security main universe",
        f"deb [arch={token} signed-by=/etc/apt/keyrings/cfdw-openfoam.gpg] https://dl.openfoam.com/repos/deb noble main",
        f"deb [arch={token} signed-by=/etc/apt/keyrings/cfdw-nvidia.gpg] {entry['nvidia_repository']} /",
    ]


def read_key_values(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        key, separator, value = line.partition("=")
        if separator:
            values[key.strip()] = value.strip()
    return values


def write_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")


def command_host_identity(args: argparse.Namespace) -> None:
    observations = json.loads(args.observations.read_text(encoding="utf-8"))
    receipt = host_identity(observations, args.expected_architecture)
    write_json(args.output, receipt)
    if receipt["refusal"]:
        raise ValueError(f"HOST-IDENTITY-REFUSED {receipt['refusal']} (receipt written: {args.output})")
    print(f"HOST-IDENTITY-PASS architecture={receipt['architecture']} cpu={len(receipt['cpu'])} gpu={len(receipt['gpu'])}")


def command_check(args: argparse.Namespace) -> None:
    receipt = json.loads(args.host_identity.read_text(encoding="utf-8"))
    if receipt.get("schema") != SCHEMA_IDENTITY or receipt.get("refusal"):
        raise ValueError("host identity receipt is refused or not a receipt")
    token = require_token(receipt.get("architecture"))
    guest = read_key_values(args.guest_identity)
    if detect(receipt["windows"], guest) != token:
        raise ValueError("guest architecture differs from the recorded host")  # detect refuses first; belt and braces
    print(f"HOST-GUEST-ARCHITECTURE-PASS architecture={token} guest_gpu={guest.get('nvidia_smi_name', 'not recorded')!r}")


def command_sources(args: argparse.Namespace) -> None:
    sys.stdout.write("\n".join(sources(args.architecture)) + "\n")


def main() -> None:
    parser = argparse.ArgumentParser()
    commands = parser.add_subparsers(dest="command", required=True)
    identity = commands.add_parser("host-identity")
    identity.add_argument("--observations", type=Path, required=True)
    identity.add_argument("--output", type=Path, required=True)
    identity.add_argument("--expected-architecture", required=True)
    check = commands.add_parser("check")
    check.add_argument("--host-identity", type=Path, required=True)
    check.add_argument("--guest-identity", type=Path, required=True)
    commands.add_parser("sources").add_argument("--architecture", required=True)
    args = parser.parse_args()
    {"host-identity": command_host_identity, "check": command_check, "sources": command_sources}[args.command](args)


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError) as error:
        print(f"architecture: {error}", file=sys.stderr)
        raise SystemExit(5)
