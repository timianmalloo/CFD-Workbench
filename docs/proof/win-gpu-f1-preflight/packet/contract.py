#!/usr/bin/env python3
"""Verify a separately committed R217 invocation contract before any WSL effect."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import subprocess
import sys
from pathlib import Path, PureWindowsPath

import architecture

OPENFOAM = "DC93C096174122E256DA24063386DD74948D208F"
NVIDIA = "EB693B3035CD5710E231E123A4B469963BF863CC"
PACKET = "docs/proof/win-gpu-f1-preflight/packet"
INPUT_KEYS = ("rootfs", "ubuntu_sums", "ubuntu_signature", "openfoam_keyring",
              "nvidia_keyring", "b2_lock", "b2_licenses")
FPR = re.compile(r"^[A-F0-9]{40}$")
SHA = re.compile(r"^[a-f0-9]{64}$")


def git(repo: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True,
                          check=True, text=True, encoding="utf-8", timeout=15).stdout.strip()


def check_hash(expected: str, data: bytes) -> None:
    if not SHA.fullmatch(expected) or hashlib.sha256(data).hexdigest() != expected:
        raise ValueError("invocation contract byte/hash mismatch")


def check_file_hash(expected: str, path: Path) -> None:
    if not SHA.fullmatch(expected):
        raise ValueError(f"invalid reviewed hash: {path}")
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    if digest.hexdigest() != expected:
        raise ValueError(f"reviewed input bytes changed: {path}")


def check_contract_fields(data: dict) -> None:
    if data.get("schema") != "cfdw-f1-r217-invocation/2":
        raise ValueError("invocation contract schema mismatch")
    token = architecture.require_token(data.get("architecture"))
    epoch = data.get("leader_epoch")
    if data.get("leader") != "mac" or type(epoch) is not int or epoch <= 0:
        raise ValueError("reviewed Mac leader epoch must be a positive integer")
    if (data.get("target") != "cfdw-petsc-cuda132-f1" or
            data.get("protected") != "cfdw-openfoam2512" or
            data.get("target_path") != r"C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1"):
        raise ValueError("distro identity/path mismatch")
    if not re.fullmatch(r"[0-9a-f]{40}", data.get("accepted_packet_commit", "")):
        raise ValueError("accepted packet commit absent")
    for key in INPUT_KEYS:
        row = data.get("inputs", {}).get(key, {})
        if not isinstance(row.get("path"), str) or not SHA.fullmatch(row.get("sha256", "")):
            raise ValueError(f"unreviewed input: {key}")
    rootfs = architecture.row(token)
    if data["inputs"]["rootfs"]["sha256"] != rootfs["rootfs_sha256"]:
        raise ValueError(f"rootfs hash is not the {token} pin")
    if PureWindowsPath(data["inputs"]["rootfs"]["path"]).name != rootfs["rootfs_name"]:
        raise ValueError(f"rootfs name is not the {token} pin")
    signers = data.get("allowed_signers", {})
    for name in ("ubuntu_rootfs", "ubuntu_archive", "openfoam", "nvidia"):
        values = signers.get(name)
        if not isinstance(values, list) or not values or any(not FPR.fullmatch(x) for x in values):
            raise ValueError(f"reviewed signer set missing/invalid: {name}")
        if len(values) != len(set(values)):
            raise ValueError(f"duplicate signer: {name}")
    if OPENFOAM not in signers["openfoam"] or NVIDIA not in signers["nvidia"]:
        raise ValueError("OpenFOAM/NVIDIA signer anchor missing")
    if not re.fullmatch(r"[A-Za-z0-9._-]+", data.get("session", "")):
        raise ValueError("execution session absent")
    if 'attempt' in data:
        import attempt
        attempt.check_binding(data)
    elif not re.fullmatch(r"win/gpu-f1-r217-[A-Za-z0-9._/-]+", data.get("execution_branch", "")):
        raise ValueError("execution branch absent")


def signer_is_allowed(lines: list[str], allowed: set[str]) -> bool:
    signatures = []
    for line in lines:
        if line.startswith("[GNUPG:] VALIDSIG "):
            fields = line.split()
            if len(fields) < 3 or not FPR.fullmatch(fields[2]):
                raise ValueError("malformed VALIDSIG")
            signatures.append(fields[2])
    if not signatures or any(value not in allowed for value in signatures):
        raise ValueError(f"unapproved or missing signing identity: {signatures}")
    return True


def verify_signer(args: argparse.Namespace) -> None:
    check_file_hash(args.expected_sha256, args.contract)
    document = json.loads(args.contract.read_text(encoding="utf-8"))
    check_contract_fields(document)
    signer_is_allowed(args.status.read_text(encoding="utf-8").splitlines(),
                      set(document["allowed_signers"][args.kind]))
    print(f"SIGNER-PASS kind={args.kind}")


def verify_keyring(args: argparse.Namespace) -> None:
    check_file_hash(args.expected_sha256, args.contract)
    document = json.loads(args.contract.read_text(encoding="utf-8"))
    check_contract_fields(document)
    result = subprocess.run(["gpg", "--batch", "--show-keys", "--with-colons", "--fingerprint",
                             str(args.keyring)], check=True, capture_output=True, text=True,
                            encoding="utf-8", timeout=15)
    found = {line.split(":")[9] for line in result.stdout.splitlines() if line.startswith("fpr:")}
    allowed = set(document["allowed_signers"][args.kind])
    if not found or not found.issubset(allowed):
        raise ValueError(f"keyring contains unapproved identities: {sorted(found - allowed)}")
    print(f"KEYRING-PASS kind={args.kind} keys={len(found)}")


def check_live_leader(observed: dict[str, str], reviewed_epoch: int) -> None:
    if (observed.get("leader") != "mac" or observed.get("state") != "live" or
            observed.get("epoch") != str(reviewed_epoch)):
        raise ValueError("live Mac epoch differs from separately reviewed contract")


def check_live_leader_json(observed: dict, reviewed_epoch: int) -> None:
    ttl = observed.get('expires_in')
    if (observed.get('leader') != 'mac' or observed.get('state') != 'live' or
            type(observed.get('epoch')) is not int or observed['epoch'] != reviewed_epoch or
            type(ttl) not in (int, float) or not math.isfinite(ttl) or ttl < 200 or
            not re.fullmatch(r'[a-f0-9]{40}', observed.get('oid', ''))):
        raise ValueError('F1-LEADER-REFUSED: exact live epoch and finite TTL >=200 required')


def verify(args: argparse.Namespace) -> None:
    repo = args.repo.resolve(strict=True)
    path = args.contract.resolve(strict=True)
    relative = path.relative_to(repo).as_posix()
    document = json.loads(path.read_text(encoding="utf-8"))
    fresh = 'attempt' in document
    expected_relative = (document.get('run_root', '') if fresh else 'docs/proof/win-gpu-f1-preflight/run-1') + '/invocation-contract.json'
    if relative != expected_relative:
        raise ValueError("contract is not at the fixed reviewed path")
    proposed = getattr(args, 'command', 'verify') == 'prepare'
    status_args = ['status', '--porcelain', '--untracked-files=all']
    runtime = getattr(args, 'runtime_output', None)
    if runtime is not None:
        # Only the supervisor's already-created evidence is exempt. All source,
        # authority and input checks below remain the same mutation admission.
        if document.get('attempt') != 2 or runtime.resolve(strict=True) != repo / document['run_root'] / 'live':
            raise ValueError('invalid supervisor runtime evidence path')
        status_args += ['--', '.', ':(exclude)' + document['run_root'] + '/live']
    if not proposed and git(repo, *status_args):
        raise ValueError("EXECUTE worktree is not clean before WSL")
    if git(repo, "branch", "--show-current") != args.branch:
        raise ValueError("execution branch mismatch")
    if not proposed:
        committed = subprocess.run(["git", "-C", str(repo), "show", f"HEAD:{relative}"],
                                   check=True, capture_output=True, timeout=15).stdout
        if committed != path.read_bytes():
            raise ValueError("contract working bytes differ from committed blob")
    check_contract_fields(document)
    if document.get('attempt') == 2 and str(repo).casefold() != document['execution_worktree'].casefold():
        raise ValueError('attempt-2 execution worktree differs from invocation')
    if document["execution_branch"] != args.branch or document["session"] != args.session:
        raise ValueError("contract branch/session mismatch")
    if fresh:
        import attempt
        attempt.verify_authorization(repo, getattr(args, 'authorization', None), document,
            hashlib.sha256(path.read_bytes()).hexdigest(), getattr(args, 'run_id', None),
            getattr(args, 'exclusive_run_id', None), proposed=proposed)
    source = document["accepted_packet_commit"]
    subprocess.run(["git", "-C", str(repo), "merge-base", "--is-ancestor", source, "HEAD"],
                   check=True, capture_output=True, timeout=15)
    actual_files = sorted(p.relative_to(repo).as_posix() for p in (repo / PACKET).iterdir() if p.is_file())
    expected_files = sorted(document.get("packet_sha256", {}))
    source_files = sorted(git(repo, "ls-tree", "-r", "--name-only", source, "--", PACKET).splitlines())
    if actual_files != expected_files or actual_files != source_files:
        raise ValueError("accepted packet file set changed")
    for name in actual_files:
        expected = document["packet_sha256"][name]
        check_hash(expected, (repo / name).read_bytes())
        if hashlib.sha256(subprocess.run(["git", "-C", str(repo), "show", f"{source}:{name}"],
                                         check=True, capture_output=True, timeout=15).stdout).hexdigest() != expected:
            raise ValueError(f"accepted source blob differs: {name}")
    if fresh:
        for name, expected in document['external_sha256'].items():
            check_hash(expected, (repo / name).read_bytes())
            blob = subprocess.run(['git', '-C', str(repo), 'show', f'{source}:{name}'],
                                  check=True, capture_output=True, timeout=15).stdout
            check_hash(expected, blob)
    paths = {"rootfs": args.rootfs, "ubuntu_sums": args.ubuntu_sums,
             "ubuntu_signature": args.ubuntu_signature, "openfoam_keyring": args.openfoam_keyring,
             "nvidia_keyring": args.nvidia_keyring,
             "b2_lock": repo / "docs/proof/win-gpu-b2/package-lock.json",
             "b2_licenses": repo / "docs/proof/win-gpu-b2/license-map.json"}
    for name, actual in paths.items():
        full = actual.resolve(strict=True)
        row = document["inputs"][name]
        if str(full).casefold() != row["path"].casefold():
            raise ValueError(f"input path changed: {name}")
        check_file_hash(row["sha256"], full)
    if proposed:
        print('PREPARATION-CONTRACT-PASS inputs=7 live_authority=False')
        return
    leader_args = [sys.executable, str(repo / "docs/ai-forward-pack/scripts/coord-core.py"), "leader", "who"]
    if document.get('attempt') == 2:
        leader_args.append('--json')
    leader = subprocess.run(leader_args, cwd=repo, check=True, capture_output=True,
                            text=True, encoding="utf-8", timeout=15).stdout
    if document.get('attempt') == 2:
        check_live_leader_json(json.loads(leader), document['leader_epoch'])
    else:
        observed = dict(line.split(None, 1) for line in leader.splitlines() if len(line.split(None, 1)) == 2)
        check_live_leader(observed, document["leader_epoch"])
    print("INVOCATION-CONTRACT-PASS")


def main() -> None:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    for mode in ('verify', 'prepare'):
        check = sub.add_parser(mode)
        for key in ("contract", "repo", "rootfs", "ubuntu-sums", "ubuntu-signature",
                    "openfoam-keyring", "nvidia-keyring"):
            check.add_argument("--" + key, type=Path, required=True)
        check.add_argument("--branch", required=True)
        check.add_argument("--session", required=True)
        check.add_argument('--run-id')
        check.add_argument('--authorization', type=Path)
        check.add_argument('--exclusive-run-id')
    sign = sub.add_parser("signer")
    sign.add_argument("--contract", type=Path, required=True)
    sign.add_argument("--expected-sha256", required=True)
    sign.add_argument("--status", type=Path, required=True)
    sign.add_argument("--kind", choices=("ubuntu_rootfs", "ubuntu_archive", "openfoam", "nvidia"), required=True)
    ring = sub.add_parser("keyring")
    ring.add_argument("--contract", type=Path, required=True)
    ring.add_argument("--expected-sha256", required=True)
    ring.add_argument("--keyring", type=Path, required=True)
    ring.add_argument("--kind", choices=("ubuntu_rootfs", "ubuntu_archive", "openfoam", "nvidia"), required=True)
    args = parser.parse_args()
    {"verify": verify, "prepare": verify, "signer": verify_signer, "keyring": verify_keyring}[args.command](args)


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        print(f"contract: {error}", file=sys.stderr)
        raise SystemExit(5)
