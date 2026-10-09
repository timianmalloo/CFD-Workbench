#!/usr/bin/env python3
"""R184 blocked-proof packaging only; ring: docs, no execution or qualification.

Cost: file hashes plus Git reads. Sealing reuses the repository staged-byte sealer.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
PROOF = Path(__file__).resolve().parent


def git(*args):
    return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, timeout=30, check=True).stdout


def main():
    if sys.argv[1:] == ["snapshots"]:
        legacy = ROOT / "docs/proof/wri-r182-runner/closing-manifest.json"
        document = json.loads(legacy.read_text(encoding="utf-8"))
        original = [{**entry} for entry in document["files"]]
        approved = "e9714ce525681ade39aac19e0d86e80ab240907a"
        names = ("windows-runner.ps1", "windows-settings-preflight.ps1", "test-windows-runner.ps1", "check-windows-runner.py")
        bindings = []
        for name in names:
            path = "tools/" + name
            entries = [entry for entry in document["files"] if entry["path"] == path]
            if len(entries) != 1:
                raise RuntimeError("legacy entry missing or duplicated")
            blob = git("show", approved + ":" + path)
            if len(blob) != entries[0]["bytes"] or hashlib.sha256(blob).hexdigest() != entries[0]["sha256"]:
                raise RuntimeError("approved legacy source does not match its existing manifest")
            target = "docs/proof/wri-r182-runner/source-snapshots/e9714ce5/" + name
            destination = ROOT / target
            destination.parent.mkdir(parents=True, exist_ok=True)
            if destination.exists():
                raise RuntimeError("snapshot exists; append-only")
            destination.write_bytes(blob)
            entries[0]["path"] = target
            bindings.append({"original_path": path, "source_commit": approved,
                             "source_blob": git("rev-parse", approved + ":" + path).decode().strip(),
                             "snapshot_path": target, "bytes": len(blob), "sha256": hashlib.sha256(blob).hexdigest()})
        if len(document["files"]) != len(original) or any({k: v for k, v in before.items() if k != "path"} != {k: v for k, v in after.items() if k != "path"} for before, after in zip(original, document["files"])):
            raise RuntimeError("legacy entry count or non-path field changed")
        legacy.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
        current = "1477951f6cff53f7f9f6eee111425738dcc24b96"
        current_names = (*names, "windows-scale-run.ps1", "test-windows-scale-run.ps1")
        for commit, snapshot_names in ((current, current_names), ("36ccfa49bc8c57f48697b991ce6373a8f6806907", ("windows-scale-run.ps1", "test-windows-scale-run.ps1"))):
            for name in snapshot_names:
                path = "tools/" + name
                blob = git("show", commit + ":" + path)
                target = "docs/proof/wri-r184-result/source-snapshots/" + commit[:8] + "/" + name
                destination = ROOT / target
                destination.parent.mkdir(parents=True, exist_ok=True)
                if destination.exists():
                    raise RuntimeError("snapshot exists; append-only")
                destination.write_bytes(blob)
                bindings.append({"original_path": path, "source_commit": commit,
                                 "source_blob": git("rev-parse", commit + ":" + path).decode().strip(),
                                 "snapshot_path": target, "bytes": len(blob), "sha256": hashlib.sha256(blob).hexdigest()})
        (PROOF / "source-bindings.json").write_text(json.dumps({"legacy_entry_count": len(original), "bindings": bindings}, indent=2) + "\n", encoding="utf-8", newline="\n")
        print("SOURCE-SNAPSHOTS PASS legacy_paths_changed=4 entry_count=" + str(len(original)) + " source_blobs=" + str(len(bindings)))
    elif sys.argv[1:] == ["sources"]:
        paths = git("ls-files", "--", "src", "tests", "global.json", "CFDWorkbench.slnx").decode().splitlines()
        rows = [{"path": path, "sha256": hashlib.sha256((ROOT / path).read_bytes()).hexdigest()} for path in paths]
        git("diff", "--exit-code", "HEAD", "--", "src", "tests", "tools", "global.json", "CFDWorkbench.slnx")
        git("diff", "--exit-code", "36ccfa49bc8c57f48697b991ce6373a8f6806907", "1477951f6cff53f7f9f6eee111425738dcc24b96", "--", "src", "tests", "global.json", "CFDWorkbench.slnx")
        if git("ls-files", "--others", "--exclude-standard", "--", "src", "tests", "tools", "global.json", "CFDWorkbench.slnx"):
            raise RuntimeError("untracked source")
        data = {"head": git("rev-parse", "HEAD").decode().strip(), "source_count": len(rows),
                "source_sha256": hashlib.sha256(json.dumps(rows, sort_keys=True).encode()).hexdigest(),
                "postcycle_diff_exit": 0, "source_between_cycle_heads_diff_exit": 0,
                "execution": "not-run; packaging only"}
        (PROOF / "source-state.json").write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8", newline="\n")
        print("SOURCE-PROOF PASS count=" + str(len(rows)) + " source_sha256=" + data["source_sha256"])
    elif sys.argv[1:] == ["seal"]:
        spec = importlib.util.spec_from_file_location("sealer", ROOT / "docs/proof/wri-r182-runner/seal-proof.py")
        sealer = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(sealer)
        sealer.FOLDER = "docs/proof/wri-r184-result"
        sealer.TOOLS = git("ls-files", "--", "docs/proof/wri-r182-runner/source-snapshots").decode().splitlines()
        sealer.TOOLS += git("ls-files", "--", "docs/proof/wri-r184-result-cycle2", "docs/proof/wri-r184-driver/callback-repair-red", "docs/proof/wri-r184-driver/callback-repair-green").decode().splitlines()
        sealer.TOOLS += ["docs/proof/wri-r184-driver/r185/qualification" + suffix for suffix in (".json", ".stdout.txt", ".stderr.txt")]
        sealer.TOOLS += [".gitattributes"]
        sealer.main()
    else:
        raise RuntimeError("expected snapshots, sources or seal")


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    try:
        main()
    except Exception as error:
        print("BLOCKED-PACKAGING FAIL " + type(error).__name__, file=sys.stderr)
        raise SystemExit(1)
