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
import tempfile
from pathlib import Path, PureWindowsPath

import architecture

OPENFOAM = "DC93C096174122E256DA24063386DD74948D208F"
NVIDIA = "EB693B3035CD5710E231E123A4B469963BF863CC"
PACKET = "docs/proof/win-gpu-f1-preflight/packet"
INPUT_KEYS = ("rootfs", "ubuntu_sums", "ubuntu_signature", "openfoam_keyring",
              "nvidia_keyring", "ubuntu_cloudimage_keyring", "b2_licenses")
# The B2 lock left the input set at node C3 (Ruling 237 (1)); the cloud-image keyring joined it at node C4 (Ruling 240 (2)).
ARGUMENT_INPUTS = tuple(name for name in INPUT_KEYS if name != "b2_licenses")   # the inputs verify reads from its arguments; the licence map is repo-derived
PROTECTED_STATES = ("cfdw-openfoam2512", "none")   # Ruling 240 (1)
FPR = re.compile(r"^[A-F0-9]{40}$")
SHA = re.compile(r"^[a-f0-9]{64}$")
TARGET_NAME = "cfdw-petsc-cuda132-f1"
TARGET_PARENT = "CFD-Workbench-wsl"


def derived_target_path(repo: PureWindowsPath) -> str:
    """Ruling 237: <repository parent>\\CFD-Workbench-wsl\\cfdw-petsc-cuda132-f1; never a constant, never a parameter.
    Control.ps1 Get-DerivedTargetPath is the same rule on the launcher side (tests/f1_derived_paths.ps1 proves them equal)."""
    repo = PureWindowsPath(repo)
    if (not re.fullmatch(r"[A-Za-z]:", repo.drive) or repo.root != "\\" or repo.parent == repo
            or re.search(r"\s", str(repo))):
        raise ValueError(f"repository root must be a no-space absolute drive path with a parent: {repo}")
    return str(repo.parent / TARGET_PARENT / TARGET_NAME)


def is_target_path(value: object) -> bool:
    if not isinstance(value, str) or re.search(r"\s", value) or any(ord(c) < 32 for c in value):
        return False
    path = PureWindowsPath(value)
    return (bool(re.fullmatch(r"[A-Za-z]:", path.drive)) and path.root == "\\" and ".." not in path.parts
            and path.parts[-2:] == (TARGET_PARENT, TARGET_NAME))


def check_target_path_derived(data: dict, repo: PureWindowsPath) -> None:
    recorded = str(PureWindowsPath(data["target_path"]))
    if recorded.casefold() != derived_target_path(repo).casefold():
        raise ValueError(f"contract target path {recorded} differs from the path derived from the repository")


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
    if (data.get("target") != TARGET_NAME or data.get("protected") not in PROTECTED_STATES or
            not is_target_path(data.get("target_path"))):
        raise ValueError("distro identity/path mismatch")
    floors = [data.get(name) for name in ("host_start_floor_bytes", "host_reserve_bytes")]
    if any(type(value) is not int or value <= 0 for value in floors) or floors[0] <= floors[1]:
        raise ValueError("host_start_floor_bytes and host_reserve_bytes must be positive integers, floor above reserve")
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


def fresh_attempt(document) -> bool:
    """Ruling 243 (2): a contract of the fresh-attempt scheme (attempt 2 or 3); the number is keyed once in attempt.py."""
    import attempt
    return document.get('attempt') in attempt.FRESH_ATTEMPTS


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
    with tempfile.TemporaryDirectory() as home:   # never the user's gpg.conf or keybox (node R, Security F6)
        if sys.platform == "win32":   # Git's gpg is an MSYS program: --homedir D:\x is read relative to its root; /d/x works
            home = "/" + home[0].lower() + "/" + home[3:].replace("\\", "/")
        result = subprocess.run([args.gpg, "--batch", "--no-options", "--homedir", home, "--show-keys",
                                 "--with-colons", "--fingerprint", str(args.keyring)], check=True,
                                capture_output=True, text=True, encoding="utf-8", timeout=15)
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
    raw = path.read_bytes()   # one read: the hash the launcher passed, the committed-blob comparison and the fields
    if getattr(args, 'expected_sha256', None):
        check_hash(args.expected_sha256, raw)
    document = json.loads(raw.decode("utf-8-sig"))
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
        if not fresh_attempt(document) or runtime.resolve(strict=True) != repo / document['run_root'] / 'live':
            raise ValueError('invalid supervisor runtime evidence path')
        status_args += ['--', '.', ':(exclude)' + document['run_root'] + '/live']
    if not proposed and git(repo, *status_args):
        raise ValueError("EXECUTE worktree is not clean before WSL")
    if git(repo, "branch", "--show-current") != args.branch:
        raise ValueError("execution branch mismatch")
    if not proposed:
        committed = subprocess.run(["git", "-C", str(repo), "show", f"HEAD:{relative}"],
                                   check=True, capture_output=True, timeout=15).stdout
        if committed != raw:
            raise ValueError("contract working bytes differ from committed blob")
    check_contract_fields(document)
    check_target_path_derived(document, PureWindowsPath(str(repo)))
    if fresh_attempt(document) and str(repo).casefold() != document['execution_worktree'].casefold():
        raise ValueError('fresh attempt execution worktree differs from invocation')
    if document["execution_branch"] != args.branch or document["session"] != args.session:
        raise ValueError("contract branch/session mismatch")
    if fresh:
        import attempt
        attempt.verify_authorization(repo, getattr(args, 'authorization', None), document,
            hashlib.sha256(raw).hexdigest(), getattr(args, 'run_id', None),
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
    paths = {name: getattr(args, name) for name in ARGUMENT_INPUTS}
    paths["b2_licenses"] = repo / "docs/proof/win-gpu-b2/license-map.json"
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
    if fresh_attempt(document):
        leader_args.append('--json')
    leader = subprocess.run(leader_args, cwd=repo, check=True, capture_output=True,
                            text=True, encoding="utf-8", timeout=15).stdout
    if fresh_attempt(document):
        check_live_leader_json(json.loads(leader), document['leader_epoch'])
    else:
        observed = dict(line.split(None, 1) for line in leader.splitlines() if len(line.split(None, 1)) == 2)
        check_live_leader(observed, document["leader_epoch"])
    print("INVOCATION-CONTRACT-PASS")


PREPARE_FIELDS = ('source_commit', 'execution_worktree', 'attempt', 'leader_epoch', 'requested_ruling', 'architecture',
                  'protected', 'host_start_floor_bytes', 'host_reserve_bytes')
REPO_INPUTS = {'b2_licenses': 'docs/proof/win-gpu-b2/license-map.json',
               'ubuntu_cloudimage_keyring': 'docs/proof/win-gpu-f1-preflight/inputs/ubuntu-cloudimage-keyring.gpg'}


CAPTURE_FOLDERS = ('checkpoint', 'final', 'live')


def capture_attribute_lines(run_root: str) -> list[str]:
    """The .gitattributes lines a run root needs so Git never normalizes a capture (RAW-CAPTURE-BLOB-MISMATCH 4)."""
    return [f"{run_root}/{folder}/** -text whitespace=-trailing-space,-blank-at-eof" for folder in CAPTURE_FOLDERS]


def ensure_capture_attributes(repo: Path, run_root: str) -> int:
    """Ruling 245 (3): write the run root's capture rules into .gitattributes once; returns how many lines were added.
    The launcher asserts the effect (`git check-attr text` unset) before its first WSL effect."""
    path = repo / ".gitattributes"
    existing = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
    missing = [line for line in capture_attribute_lines(run_root) if line not in existing]
    if missing:
        path.write_text("\n".join(existing + missing) + "\n", encoding="utf-8", newline="\n")
    return len(missing)


def prepare(args: argparse.Namespace) -> None:
    """Ruling 235 (3): produce the attempt-2 proposal pair (contract, proposed authorization) offline from a clean, exact
    author source commit, for an execution worktree that does not exist yet; no live check runs here. Without
    --source-commit the command is the validation-only preparation (verify in proposed mode)."""
    if getattr(args, 'source_commit', None) is None:
        return verify(args)
    import attempt
    repo = args.repo.resolve(strict=True)
    source = args.source_commit
    if not re.fullmatch(r'[a-f0-9]{40}', source) or git(repo, 'rev-parse', 'HEAD') != source:
        raise ValueError('preparation source must be the exact author HEAD')
    if git(repo, 'status', '--porcelain', '--untracked-files=all'):
        raise ValueError('preparation author worktree must be clean')
    for name in ('leader_epoch', 'requested_ruling', 'host_start_floor_bytes', 'host_reserve_bytes'):
        value = getattr(args, name, None)
        if type(value) is not int or value <= 0:
            raise ValueError(f'preparation {name} must be a positive integer')
    token = architecture.require_token(getattr(args, 'architecture', None))
    number = getattr(args, 'attempt', None)   # Ruling 243 (2): the attempt number is an explicit preparation input
    if type(number) is not int or number not in attempt.FRESH_ATTEMPTS:
        raise ValueError(f'preparation attempt must be one of {attempt.FRESH_ATTEMPTS}')
    if getattr(args, 'protected', None) not in PROTECTED_STATES:
        raise ValueError(f'preparation protected state must be one of {PROTECTED_STATES}')
    run_root = attempt.PREFIX + f'/attempt-{number}'
    paths = [repo / run_root / name for name in ('invocation-contract.json', 'attempt-authorization.json')]
    if (args.authorization is None or args.contract.resolve() != paths[0] or
            args.authorization.resolve() != paths[1] or any(path.exists() for path in paths)):
        raise ValueError('preparation requires the exact absent output pair')
    if not paths[0].parent.is_dir():
        raise ValueError('preparation output directory must already exist')
    worktree = args.execution_worktree
    if (not isinstance(worktree, str) or not worktree or '..' in PureWindowsPath(worktree).parts or
            not PureWindowsPath(worktree).is_absolute() or Path(worktree).exists()):
        raise ValueError('future execution worktree must be an absolute, canonical, uncreated path')

    def source_digest(name: str) -> str:
        blob = subprocess.run(['git', '-C', str(repo), 'show', f'{source}:{name}'],
                              check=True, capture_output=True, timeout=15).stdout
        if (repo / name).read_bytes() != blob:
            raise ValueError('preparation source/blob mismatch: ' + name)
        return hashlib.sha256(blob).hexdigest()

    names = sorted(path.relative_to(repo).as_posix() for path in (repo / PACKET).iterdir() if path.is_file())
    committed = sorted(git(repo, 'ls-tree', '-r', '--name-only', source, '--', PACKET).splitlines())
    if names != committed:
        raise ValueError('preparation packet file set differs from source')
    packet_hashes = {name: source_digest(name) for name in names}
    external_hashes = {name: source_digest(name) for name in (attempt.EXTERNAL, attempt.COORDINATOR)}
    reviewed = attempt.input_hashes(token)
    inputs = {}
    for name in INPUT_KEYS:
        if name in REPO_INPUTS:   # reviewed in the repository: the bytes are the source commit's, the path is the execution worktree's
            source_digest(REPO_INPUTS[name])
            actual = repo / REPO_INPUTS[name]
            projected = str(PureWindowsPath(worktree) / REPO_INPUTS[name])
        else:
            actual = getattr(args, name).resolve(strict=True)
            projected = str(actual)
        check_file_hash(reviewed[name], actual)
        inputs[name] = dict(path=projected, sha256=reviewed[name])
    document = dict(schema='cfdw-f1-r217-invocation/2', architecture=token, attempt=number, leader='mac',
                    leader_epoch=args.leader_epoch, target=TARGET_NAME, protected=args.protected,
                    target_path=derived_target_path(PureWindowsPath(worktree)),
                    host_start_floor_bytes=args.host_start_floor_bytes, host_reserve_bytes=args.host_reserve_bytes,
                    accepted_packet_commit=source, execution_branch=args.branch, session=args.session,
                    execution_worktree=worktree, run_id=args.run_id, run_root=run_root, packet_sha256=packet_hashes,
                    external_sha256=external_hashes, inputs=inputs, allowed_signers=attempt.SIGNERS)
    check_contract_fields(document)
    attempt.check_binding(document, args.run_id)
    added = ensure_capture_attributes(repo, run_root)   # Ruling 245 (3): the run root keeps its captured bytes
    contract_bytes = (json.dumps(document, indent=2, sort_keys=True) + '\n').encode('utf-8')
    contract_sha = hashlib.sha256(contract_bytes).hexdigest()
    authorization = attempt.authorization(document, contract_sha, ruling=args.requested_ruling)
    attempt.check_authorization(authorization, document, contract_sha, proposed=True)
    authorization_bytes = (json.dumps(authorization, indent=2, sort_keys=True) + '\n').encode('utf-8')
    # Both destinations are acquired exclusively before either is written: a collision never overwrites, and a
    # failure never leaves a half pair (these are exact repo-local paths created by this call).
    opened = []
    try:
        for path in paths:
            opened.append((path, path.open('xb')))
        for (_, stream), payload in zip(opened, (contract_bytes, authorization_bytes)):
            stream.write(payload)
        for _, stream in opened:
            stream.close()
    except OSError:
        for path, stream in opened:
            stream.close()
            path.unlink()
        raise
    print(f'PREPARATION-GENERATED outputs=2 inputs={len(inputs)} architecture={token} protected={args.protected} live_authority=False')
    print('contract_sha256=' + contract_sha)
    print('authorization_sha256=' + hashlib.sha256(authorization_bytes).hexdigest())
    print(f'capture_attributes_added={added}')


def main() -> None:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    for mode in ('verify', 'prepare'):
        check = sub.add_parser(mode)
        for key in ("contract", "repo", "rootfs", "ubuntu-sums", "ubuntu-signature",
                    "openfoam-keyring", "nvidia-keyring", "ubuntu-cloudimage-keyring"):
            check.add_argument("--" + key, type=Path, required=True)
        check.add_argument("--branch", required=True)
        check.add_argument("--session", required=True)
        check.add_argument('--run-id')
        check.add_argument('--authorization', type=Path)
        check.add_argument('--exclusive-run-id')
        check.add_argument('--expected-sha256', help='the launcher hashes the bytes it parsed; verify refuses other bytes')
        if mode == 'prepare':   # the producer (Ruling 235 (3)); without --source-commit, validation only
            check.add_argument('--source-commit')
            check.add_argument('--execution-worktree')
            check.add_argument('--attempt', type=int, help='the attempt number under the fresh-attempt scheme (Ruling 243 (2))')
            check.add_argument('--leader-epoch', type=int)
            check.add_argument('--requested-ruling', type=int)
            check.add_argument('--architecture', choices=sorted(architecture.TABLE))
            check.add_argument('--protected', choices=PROTECTED_STATES)
            check.add_argument('--host-start-floor-bytes', type=int)
            check.add_argument('--host-reserve-bytes', type=int)
    sign = sub.add_parser("signer")
    sign.add_argument("--contract", type=Path, required=True)
    sign.add_argument("--expected-sha256", required=True)
    sign.add_argument("--status", type=Path, required=True)
    sign.add_argument("--kind", choices=("ubuntu_rootfs", "ubuntu_archive", "openfoam", "nvidia"), required=True)
    target = sub.add_parser("target-path")
    target.add_argument("--repo", type=Path, required=True)
    ring = sub.add_parser("keyring")
    ring.add_argument("--contract", type=Path, required=True)
    ring.add_argument("--expected-sha256", required=True)
    ring.add_argument("--keyring", type=Path, required=True)
    ring.add_argument("--kind", choices=("ubuntu_rootfs", "ubuntu_archive", "openfoam", "nvidia"), required=True)
    ring.add_argument("--gpg", default="gpg", help="the gpg executable; on Windows the one beside Git's gpgv.exe")
    args = parser.parse_args()
    {"verify": verify, "prepare": prepare, "signer": verify_signer, "keyring": verify_keyring,
     "target-path": lambda a: print(derived_target_path(PureWindowsPath(str(a.repo))))}[args.command](args)


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        print(f"contract: {error}", file=sys.stderr)
        raise SystemExit(5)
