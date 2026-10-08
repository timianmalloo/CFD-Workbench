"""Bind the closing W-4 evidence files to staged or committed Git blobs."""
import hashlib
import json
import pathlib
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[3]
proof = pathlib.Path(__file__).resolve().parent
manifest = proof / "closing-manifest.json"
cases = [root / "cases" / name for name in (
    "win-spike04r3-g0-l6.yaml",
    "win-su2-tmr-naca0012.yaml",
    "win-spike04r3-g2-l3.yaml",
)]
files = sorted(
    [p for p in proof.rglob("*") if p.is_file() and p != manifest and "__pycache__" not in p.parts]
    + cases
)
source_base = "fd96651e6dd3ad9c01e4fbaa07c62a11fa553c09"
head_mode = "--head" in sys.argv
ref = "HEAD:" if head_mode else ":"
records = []
for path in files:
    rel = path.relative_to(root).as_posix()
    data = path.read_bytes()
    blob = subprocess.run(
        ["git", "show", ref + rel], cwd=root, check=True, stdout=subprocess.PIPE
    ).stdout
    assert data == blob, rel
    records.append({"path": rel, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()})

state = {
    "source_base_sha": source_base,
    "scope": "Closing W-4 proof bytes and three Windows case YAMLs equal the staged or committed Git blobs; historical launch freezes are retained.",
    "files": records,
}
if head_mode:
    subprocess.run(
        ["git", "merge-base", "--is-ancestor", source_base, "HEAD"], cwd=root, check=True
    )
    assert json.loads(manifest.read_bytes()) == state
    head = subprocess.run(
        ["git", "rev-parse", "HEAD"], cwd=root, check=True, text=True, stdout=subprocess.PIPE
    ).stdout.strip()
    print(f"HEAD manifest verified: {len(records)} files; evidence commit {head} descends from source base {source_base}")
else:
    manifest.write_bytes((json.dumps(state, indent=2) + "\n").encode())
    print(f"Staged closing manifest written: {len(records)} files; source base {source_base}")
