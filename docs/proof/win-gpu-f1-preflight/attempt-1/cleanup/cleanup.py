#!/usr/bin/env python3
"""Ruling-bound cleanup-only executor for the stopped R228 attempt.

This source performs no preflight stage and no host-path deletion. It refuses
unless origin/main contains the exact accepted Mac/Fable ruling.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import subprocess
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True

REL = Path("docs/proof/win-gpu-f1-preflight/attempt-1/cleanup")
AUTH_REL = REL / "cleanup-authorization.json"
ACTIVATION_SAMPLE_REL = REL / "leader-activation.json"
PAYLOAD_REL = REL / "payload.json"
FAILURE_REL = Path("docs/proof/win-gpu-f1-preflight/attempt-1/final")
PACKET_REL = Path("docs/proof/win-gpu-f1-preflight/packet")
TARGET = "cfdw-petsc-cuda132-f1"
TARGET_KEY = "{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}"
TARGET_PATH = r"C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1"
PROTECTED = "cfdw-openfoam2512"
PROTECTED_KEY = "{b7566a82-9cee-47ce-a246-f83632353cea}"
PROTECTED_PATH = r"%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512"
RUN_ID = "r225-attempt1-epoch8-20261012"
ORIGINAL_RUN_ROOT = r"C:\Projects\CFD-Workbench-win-gpu-f1-r225-attempt1\docs\proof\win-gpu-f1-preflight\attempt-1\live"
CHECKPOINT_COMMIT = "d6edbc63a39c7f59d9e4ad853c92e7821406e371"
CHECKPOINT_ANCESTORS = ["7c8a7984fcb101a4f29c212f221488aa1a44e637",
                        "8e10f78ea4c99fc60184c8b7372e751a149ff3b4"]
DEADLINE_SECONDS = 600


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def canonical(value: object) -> bytes:
    return (json.dumps(value, indent=2, sort_keys=True) + "\n").encode("utf-8")


def normalize(path: str) -> str:
    return os.path.expandvars(path).removeprefix("\\\\?\\").rstrip("\\").casefold()


def public_value(value: object) -> object:
    if isinstance(value, str):
        profile = os.environ.get("USERPROFILE")
        if profile:
            value = re.sub(re.escape(profile), "%USERPROFILE%", value, flags=re.IGNORECASE)
        host = os.environ.get("COMPUTERNAME")
        if host:
            value = re.sub(re.escape(host), "<host>", value, flags=re.IGNORECASE)
        return re.sub(r"S-1-5-21-\d+-\d+-\d+-\d+", "S-1-5-21-<machine>-<RID>", value)
    if isinstance(value, list):
        return [public_value(item) for item in value]
    if isinstance(value, dict):
        return {key: public_value(item) for key, item in value.items()}
    return value


def exact_row(rows: list[dict], name: str, key: str, path: str) -> dict:
    matches = [row for row in rows if row.get("name") == name]
    if len(matches) != 1:
        raise ValueError("missing or ambiguous registration: " + name)
    row = matches[0]
    if (str(row.get("key", "")).casefold() != key.casefold()
            or row.get("version") != 2
            or normalize(str(row.get("path", ""))) != normalize(path)):
        raise ValueError("registration identity mismatch: " + name)
    return row


def reviewed_identity(value: dict) -> dict:
    """Read the identity binding from authorization after exact-value validation."""
    return {key: value[key] for key in ("target", "target_key", "target_path", "protected",
                                         "protected_key", "protected_path", "protected_version")}


def validate_identity(rows: list[dict], names: list[str], target_path_present: bool,
                      protected_path_present: bool, identity: dict) -> tuple[dict, dict]:
    keys = [str(row.get("key", "")).casefold() for row in rows]
    if not keys or len(keys) != len(set(keys)):
        raise ValueError("missing or duplicate registration GUID")
    target = exact_row(rows, identity["target"], identity["target_key"], identity["target_path"])
    protected = exact_row(rows, identity["protected"], identity["protected_key"], identity["protected_path"])
    for name, path in ((identity["target"], identity["target_path"]),
                       (identity["protected"], identity["protected_path"])):
        if sum(normalize(str(row.get("path", ""))) == normalize(path) for row in rows) != 1:
            raise ValueError("ambiguous registration path: " + name)
    if names.count(identity["target"]) != 1 or names.count(identity["protected"]) != 1:
        raise ValueError("WSL list identity mismatch")
    if not target_path_present or not protected_path_present:
        raise ValueError("required distro path absent")
    if target["key"].casefold() == protected["key"].casefold():
        raise ValueError("target and protected GUID are shared")
    return target, protected


def validate_absence(rows: list[dict], names: list[str], target_path_present: bool,
                     protected_path_present: bool, protected_before: dict, identity: dict) -> None:
    if any(row.get("name") == identity["target"]
           or str(row.get("key", "")).casefold() == identity["target_key"].casefold()
           or normalize(str(row.get("path", ""))) == normalize(identity["target_path"])
           for row in rows):
        raise ValueError("target registration identity survived unregister")
    if identity["target"] in names or target_path_present:
        raise ValueError("target list/path absence not proved")
    protected = exact_row(rows, identity["protected"], identity["protected_key"], identity["protected_path"])
    if sum(str(row.get("key", "")).casefold() == identity["protected_key"].casefold() for row in rows) != 1:
        raise ValueError("ambiguous protected registration GUID")
    if sum(normalize(str(row.get("path", ""))) == normalize(identity["protected_path"]) for row in rows) != 1:
        raise ValueError("ambiguous protected registration path")
    if protected != protected_before or names.count(identity["protected"]) != 1 or not protected_path_present:
        raise ValueError("protected identity changed")


def validate_authorization(value: dict, payload_sha256: str) -> None:
    ruling = value.get("ruling")
    if type(ruling) is not int or ruling <= 0:
        raise ValueError("valid leader-assigned ruling number absent")
    expected = {
        "schema": f"cfdw-f1-r{ruling}-cleanup-authorization/1",
        "status": "accepted",
        "ruling": ruling,
        "leader": "mac",
        "leader_epoch": 8,
        "source_commit": value.get("source_commit"),
        "payload_sha256": payload_sha256,
        "session": f"pc-f1-r{ruling}-cleanup1",
        "branch": "win/gpu-f1-r228-cleanup",
        "checkpoint_commit": CHECKPOINT_COMMIT,
        "checkpoint_ancestors": CHECKPOINT_ANCESTORS,
        "original_run_id": RUN_ID,
        "original_run_root": ORIGINAL_RUN_ROOT,
        "target": TARGET,
        "target_key": TARGET_KEY,
        "target_path": TARGET_PATH,
        "protected": PROTECTED,
        "protected_key": PROTECTED_KEY,
        "protected_path": PROTECTED_PATH,
        "protected_version": 2,
        "allowed_actions": ["precheck", "owned-process-refusal", "terminate-target", "unregister-target", "prove-absence", "freeze-and-commit-evidence"],
        "forbidden_actions": ["resume-preflight", "download", "install", "source-fetch", "build", "run-case", "benchmark", "manual-host-path-delete"],
        "deadline_seconds": DEADLINE_SECONDS,
        "exclusive_run_id": f"pc-f1-r{ruling}-cleanup1",
        "pc_exclusive_required": True,
        "attempt": 1,
        "one_attempt_only": True,
        "failure_policy": "commit-receipt-stop-no-retry",
        "retry_requires_new_ruling": True,
    }
    if not re.fullmatch(r"[0-9a-f]{40}", str(value.get("source_commit", ""))):
        raise ValueError("exact reviewed source commit absent")
    if value != expected:
        raise ValueError("cleanup authorization differs from exact accepted contract")


def ruling_section(text: str, ruling: int) -> str:
    match = re.search(rf"(?ms)^### Ruling {ruling}\b.*?(?=^### Ruling |\Z)", text)
    if match is None:
        raise ValueError(f"Ruling {ruling} absent from origin/main")
    return match.group(0)


def verify_ruling(section: str, auth_sha: str, payload_sha: str, source_commit: str,
                  ruling: int) -> None:
    required = ["ACCEPT ONE CLEANUP-ONLY ACTION", auth_sha, payload_sha, source_commit,
                TARGET_KEY, TARGET_PATH, PROTECTED_KEY, PROTECTED_PATH,
                "never resume preflight", "never download", "never install",
                "never fetch source", "never build", "never run a case", "never benchmark",
                "never delete host paths manually", "R223(a)", "same commit", "R223(b)",
                "two leader samples", "at least 200 seconds", "R223(c)", "PC exclusive",
                "R223(d)", "one attempt", "no second run without a new ruling"]
    missing = [token for token in required if token not in section]
    if missing:
        raise ValueError(f"origin/main Ruling {ruling} lacks exact cleanup bindings: " + repr(missing))
    if "PROPOSED" in section:
        raise ValueError("origin/main ruling retains proposed marker")


def cleanup_sequence(action) -> None:
    for name in ("precheck-identity", "owned-stage-refusal", "owned-guard-refusal",
                 "stable-before-terminate", "terminate", "stable-before-unregister",
                 "unregister", "stable-absence", "protected-invariance"):
        action(name)


def validate_leader_sample(value: dict, epoch: int) -> None:
    if (value.get("leader") != "mac" or value.get("state") != "live"
            or value.get("epoch") != epoch or not isinstance(value.get("expires_in"), (int, float))
            or not math.isfinite(value["expires_in"]) or value["expires_in"] < 200):
        raise ValueError("live Mac leader epoch with at least 200 seconds required")


class Cleanup:
    def __init__(self, repo: Path, authorization: Path, session: str, epoch: int,
                 exclusive_run_id: str):
        self.repo = repo
        self.authorization = authorization
        self.session = session
        self.epoch = epoch
        self.exclusive_run_id = exclusive_run_id
        self.root = repo / REL
        self.live = self.root / "run-1/live"
        self.final = self.root / "run-1/final"
        self.binding = self.root / "run-1/binding.json"
        self.start = time.monotonic()
        self.index = 0
        self.log: list[dict] = []
        self.precheck_captures: list[tuple[int, str, str, dict]] = []
        self.protected_before: dict | None = None
        self.failure: dict | None = None

    def remaining(self, cap: float) -> float:
        left = min(cap, DEADLINE_SECONDS - (time.monotonic() - self.start))
        if left <= 0:
            raise ValueError("cleanup deadline exhausted")
        return left

    def run_command(self, argv: list[str], label: str, cap: float = 30) -> bytes:
        self.remaining(cap)
        started = time.monotonic()
        result = subprocess.run(argv, cwd=self.repo, capture_output=True, timeout=self.remaining(cap), check=False)
        self.index += 1
        for stream, data in (("stdout", result.stdout), ("stderr", result.stderr)):
            capture = {
                "schema": "cfdw-classified-subprocess-capture/1",
                "bytes": len(data),
                "sha256": hashlib.sha256(data).hexdigest(),
                "raw": "Not published",
            }
            if self.live.exists():
                (self.live / f"{self.index:03d}-{label}.{stream}.json").write_bytes(canonical(capture))
            else:
                self.precheck_captures.append((self.index, label, stream, capture))
        self.log.append({"label": label, "argv": public_value(argv), "exit": result.returncode,
                         "elapsed_seconds": time.monotonic() - started})
        if result.returncode != 0:
            raise ValueError(f"{label} exited {result.returncode}")
        return result.stdout

    def git(self, *args: str, label: str = "git") -> bytes:
        return self.run_command(["git", *args], label, 120)

    def precheck(self) -> None:
        if os.name != "nt":
            raise ValueError("Windows executor required")
        if self.live.exists() or self.final.exists() or self.binding.exists():
            raise ValueError("cleanup run-1 output already exists")
        payload_path = self.repo / PAYLOAD_REL
        payload_sha = digest(payload_path)
        value = json.loads(self.authorization.read_bytes())
        validate_authorization(value, payload_sha)
        if self.authorization != (self.repo / AUTH_REL).resolve():
            raise ValueError("only canonical cleanup authorization path accepted")
        if self.session != value["session"] or self.epoch != value["leader_epoch"]:
            raise ValueError("runtime session or leader epoch differs")
        if self.exclusive_run_id != value["exclusive_run_id"]:
            raise ValueError("R223(c) PC-exclusive run binding differs")
        if self.run_command(["git", "status", "--porcelain"], "clean").strip():
            raise ValueError("worktree dirty before cleanup")
        branch = self.run_command(["git", "branch", "--show-current"], "branch").decode().strip()
        if branch != value["branch"]:
            raise ValueError("execution branch differs")
        head = self.run_command(["git", "rev-parse", "HEAD"], "head").decode().strip()
        self.run_command(["git", "merge-base", "--is-ancestor", value["source_commit"], head], "source-ancestor")
        for relative in (AUTH_REL, ACTIVATION_SAMPLE_REL, PAYLOAD_REL):
            blob = self.run_command(["git", "cat-file", "blob", head + ":" + relative.as_posix()], "committed-blob")
            if blob != (self.repo / relative).read_bytes():
                raise ValueError("committed artifact differs: " + relative.as_posix())
        payload = json.loads(payload_path.read_bytes())
        if payload.get("source_commit") != value["source_commit"]:
            raise ValueError("payload source commit differs")
        for relative, expected in payload.get("files", {}).items():
            path = self.repo / relative
            if not path.is_file() or digest(path) != expected:
                raise ValueError("payload file hash differs: " + relative)
            blob = self.run_command(["git", "cat-file", "blob", value["source_commit"] + ":" + relative], "source-blob")
            if hashlib.sha256(blob).hexdigest() != expected:
                raise ValueError("reviewed source blob differs: " + relative)
        manifest = self.repo / PACKET_REL / "manifest.py"
        self.run_command([sys.executable, str(manifest), "verify", "--repo", str(self.repo),
                          "--frozen", str(self.repo / FAILURE_REL),
                          "--commit", value["source_commit"]], "verify-r228-failure", 120)
        self.run_command(["git", "merge-base", "--is-ancestor", value["checkpoint_commit"],
                          value["source_commit"]], "checkpoint-source-ancestor")
        for commit in value["checkpoint_ancestors"]:
            self.run_command(["git", "merge-base", "--is-ancestor", commit,
                              value["source_commit"]], "checkpoint-prerequisite-ancestor")
        self.run_command([sys.executable, str(manifest), "verify", "--repo", str(self.repo),
                          "--frozen", str(self.repo / FAILURE_REL),
                          "--commit", value["checkpoint_commit"]], "verify-r228-checkpoint", 120)
        auth_sha = digest(self.authorization)
        origin_ruling = self.run_command(["git", "show", "origin/main:docs/notes/rulings.md"], "origin-main-ruling").decode("utf-8")
        ruling = value["ruling"]
        verify_ruling(ruling_section(origin_ruling, ruling), auth_sha, payload_sha,
                      value["source_commit"], ruling)
        ruling_commits = self.run_command(["git", "log", "--format=%H", "--reverse",
                                           "-S", f"### Ruling {ruling}", "origin/main", "--",
                                           "docs/notes/rulings.md"], "ruling-commit").decode().splitlines()
        if len(ruling_commits) != 1:
            raise ValueError(f"R223(a) requires one defining Ruling {ruling} commit")
        ruling_commit = ruling_commits[0]
        changed = self.run_command(["git", "diff-tree", "--no-commit-id", "--name-only", "-r",
                                    ruling_commit], "ruling-paths").decode().splitlines()
        if "docs/notes/rulings.md" not in changed or AUTH_REL.as_posix() not in changed:
            raise ValueError("R223(a) ruling and authorization are not in one commit")
        ruling_auth = self.run_command(["git", "cat-file", "blob",
                                        ruling_commit + ":" + AUTH_REL.as_posix()], "ruling-auth-blob")
        if ruling_auth != self.authorization.read_bytes():
            raise ValueError("R223(a) ruling commit authorization differs")
        activation_path = self.repo / ACTIVATION_SAMPLE_REL
        ruling_sample = self.run_command(["git", "cat-file", "blob",
                                          ruling_commit + ":" + ACTIVATION_SAMPLE_REL.as_posix()],
                                         "ruling-leader-sample")
        if ruling_sample != activation_path.read_bytes():
            raise ValueError("R223(b) activation sample is not from ruling commit")
        validate_leader_sample(json.loads(ruling_sample), self.epoch)
        self.authorization_value = value
        self.identity = reviewed_identity(value)
        self.original_run_id = value["original_run_id"]
        self.original_run_root = value["original_run_root"]
        self.activation_sample = json.loads(ruling_sample)
        self.payload_sha = payload_sha

    def capture_pre_live_leader(self) -> None:
        raw = self.run_command([sys.executable, "docs/ai-forward-pack/scripts/coord-core.py",
                                "leader", "who", "--json"], "leader-immediately-pre-live")
        sample = json.loads(raw)
        validate_leader_sample(sample, self.epoch)
        self.write("leader-immediately-pre-live.json", sample)

    def registrations(self) -> list[dict]:
        import winreg
        rows = []
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Lxss") as key:
            for index in range(winreg.QueryInfoKey(key)[0]):
                identity = winreg.EnumKey(key, index)
                with winreg.OpenKey(key, identity) as subkey:
                    rows.append({"key": identity,
                                 "name": winreg.QueryValueEx(subkey, "DistributionName")[0],
                                 "path": winreg.QueryValueEx(subkey, "BasePath")[0],
                                 "version": winreg.QueryValueEx(subkey, "Version")[0]})
        return rows

    def names(self) -> list[str]:
        raw = self.run_command(["wsl.exe", "--list", "--quiet"], "distro-list", 15)
        return [line.strip().lstrip("\ufeff") for line in raw.decode("utf-16-le").splitlines() if line.strip()]

    def observe(self) -> tuple[list[dict], list[str], bool, bool]:
        return (self.registrations(), self.names(), os.path.lexists(self.identity["target_path"]),
                os.path.lexists(os.path.expandvars(self.identity["protected_path"])))

    def stable_identity(self, label: str) -> tuple[dict, dict]:
        first = self.observe()
        a = validate_identity(*first, self.identity)
        second = self.observe()
        b = validate_identity(*second, self.identity)
        if first != second or a != b:
            raise ValueError("identity changed across stable admission")
        self.write(label + ".json", {"registrations": first[0], "names": first[1],
                                     "target_path_present": first[2], "protected_path_present": first[3]})
        return a

    def stable_absence(self) -> None:
        first = self.observe()
        validate_absence(*first, self.protected_before, self.identity)
        second = self.observe()
        validate_absence(*second, self.protected_before, self.identity)
        if first != second:
            raise ValueError("absence changed across stable proof")
        self.write("absence.json", {"registrations": first[0], "names": first[1],
                                    "target_path_present": first[2], "protected_path_present": first[3]})

    def linux_path(self, path: Path | str) -> str:
        text = str(path)
        if not re.fullmatch(r"C:\\[^\s]+", text):
            raise ValueError("exact nonspace C: path required")
        return "/mnt/c/" + text[3:].replace("\\", "/")

    def owned_check(self, distro: str, kind: str) -> None:
        helper = self.repo / REL / "cleanup_owned_check.py"
        owned = self.repo / PACKET_REL / "owned_control.py"
        argv = ["wsl.exe", "--distribution", distro, "--user", "root", "--exec",
                "/usr/bin/taskset", "--cpu-list", "0" if distro == self.identity["protected"] else "0-1",
                "/usr/bin/nice", "-n", "10", "/usr/bin/python3", self.linux_path(helper),
                "--evidence-root", self.linux_path(self.repo / FAILURE_REL),
                "--expected-run-root", self.linux_path(self.original_run_root),
                "--run-id", self.original_run_id, "--owned-control", self.linux_path(owned), "--kind", kind]
        output = self.run_command(argv, "owned-" + kind + "-check", 30)
        if output.strip() != b"OWNED-ABSENT":
            raise ValueError("owned-process checker did not prove absence")

    def write(self, name: str, value: object) -> None:
        path = self.live / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(canonical(public_value(value)))

    def publish_precheck(self) -> None:
        for ordinal, label, stream, capture in self.precheck_captures:
            (self.live / f"{ordinal:03d}-{label}.{stream}.json").write_bytes(canonical(capture))
        self.write("precheck-commands.json", self.log)

    def action(self, name: str) -> None:
        if name == "precheck-identity":
            target, protected = self.stable_identity("identity-before")
            self.protected_before = protected
            if target["key"].casefold() != self.identity["target_key"].casefold():
                raise ValueError("target GUID changed")
        elif name == "owned-stage-refusal":
            self.owned_check(self.identity["target"], "stage")
        elif name == "owned-guard-refusal":
            self.owned_check(self.identity["protected"], "guard")
        elif name == "stable-before-terminate":
            self.stable_identity("identity-before-terminate")
        elif name == "terminate":
            self.run_command(["wsl.exe", "--terminate", self.identity["target"]], "terminate", 30)
        elif name == "stable-before-unregister":
            self.stable_identity("identity-before-unregister")
        elif name == "unregister":
            self.run_command(["wsl.exe", "--unregister", self.identity["target"]], "unregister", 120)
        elif name == "stable-absence":
            self.stable_absence()
        elif name == "protected-invariance":
            rows, names, target_path, protected_path = self.observe()
            validate_absence(rows, names, target_path, protected_path, self.protected_before, self.identity)
            self.write("protected-after.json", self.protected_before)
        else:
            raise ValueError("unknown cleanup action")

    def receipt(self, outcome: str) -> dict:
        ruling = self.authorization_value["ruling"]
        return {"schema": f"cfdw-f1-r{ruling}-cleanup-receipt/1", "outcome": outcome,
                "ruling": ruling, "source_commit": self.authorization_value["source_commit"],
                "authorization_sha256": digest(self.authorization), "payload_sha256": self.payload_sha,
                **self.identity, "original_run_id": self.original_run_id,
                "exclusive_run_id": self.exclusive_run_id,
                "activation_leader_sample": self.activation_sample,
                "commands": self.log, "failure": self.failure,
                "elapsed_seconds": time.monotonic() - self.start,
                "evidence_binding": "pending separate binding commit"}

    def bind(self, outcome: str) -> str:
        self.write("cleanup-receipt.json", self.receipt(outcome))
        manifest = self.repo / PACKET_REL / "manifest.py"
        self.run_command([sys.executable, str(manifest), "freeze", "--live", str(self.live),
                          "--frozen", str(self.final)], "freeze", 120)
        relative = self.final.relative_to(self.repo).as_posix()
        self.run_command(["git", "add", "-f", "--", relative], "git-add-final", 30)
        ruling = self.authorization_value["ruling"]
        self.run_command(["git", "commit", "-m",
                          f"proof: retain R{ruling} cleanup-only outcome"], "git-commit-final", 120)
        evidence_commit = self.run_command(["git", "rev-parse", "HEAD"], "evidence-head").decode().strip()
        self.run_command([sys.executable, str(manifest), "verify", "--repo", str(self.repo),
                          "--frozen", str(self.final), "--commit", evidence_commit], "verify-final", 120)
        binding = {"schema": f"cfdw-f1-r{ruling}-cleanup-binding/1", "outcome": outcome,
                   "evidence_commit": evidence_commit,
                   "manifest_sha256": digest(self.final / "capture-manifest.json")}
        self.binding.write_bytes(canonical(binding))
        self.run_command(["git", "add", "--", self.binding.relative_to(self.repo).as_posix()], "git-add-binding", 30)
        self.run_command(["git", "commit", "-m",
                          f"proof: bind R{ruling} cleanup-only evidence"], "git-commit-binding", 120)
        return self.run_command(["git", "rev-parse", "HEAD"], "binding-head").decode().strip()

    def execute(self) -> None:
        self.precheck()
        self.live.mkdir(parents=True)
        self.publish_precheck()
        outcome = "failed-stop-no-retry"
        try:
            self.capture_pre_live_leader()
            cleanup_sequence(self.action)
            outcome = "cleanup-proved"
        except Exception as error:
            self.failure = {"type": type(error).__name__,
                            "raw_error_sha256": hashlib.sha256(str(error).encode()).hexdigest()}
        binding_commit = self.bind(outcome)
        if outcome != "cleanup-proved":
            raise ValueError("cleanup failed; committed receipt; no retry")
        print(f"R{self.authorization_value['ruling']}-CLEANUP-PASS binding_commit=" + binding_commit)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--authorization", type=Path, required=True)
    parser.add_argument("--session", required=True)
    parser.add_argument("--epoch", type=int, required=True)
    parser.add_argument("--exclusive-run-id", required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[5]
    os.environ["AGENT_SESSION"] = args.session
    Cleanup(repo, args.authorization.resolve(strict=True), args.session, args.epoch,
            args.exclusive_run_id).execute()


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        print("CLEANUP-REFUSED type=" + type(error).__name__ + " sha256="
              + hashlib.sha256(str(error).encode()).hexdigest(), file=sys.stderr)
        raise SystemExit(5)
