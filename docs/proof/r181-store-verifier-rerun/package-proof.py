"""Build capture-manifest.json from the committed blobs in this proof folder."""

from __future__ import annotations

import hashlib
import json
import subprocess
from pathlib import Path


PROOF = Path(__file__).resolve().parent
REPO = PROOF.parents[2]
PROOF_REL = PROOF.relative_to(REPO).as_posix()
MANIFEST = f"{PROOF_REL}/capture-manifest.json"


def git(*args: str, text: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(
        ["git", "-C", str(REPO), *args],
        check=True,
        capture_output=True,
        text=text,
    )


def main() -> None:
    changed = git("status", "--porcelain", "--", PROOF_REL).stdout.strip()
    if changed:
        raise SystemExit("R181-MANIFEST: proof folder must be committed and clean")
    names = [
        name
        for name in git("ls-tree", "-r", "--name-only", "HEAD", "--", PROOF_REL).stdout.splitlines()
        if name and name != MANIFEST
    ]
    if not names:
        raise SystemExit("R181-MANIFEST: no committed proof files")
    files = []
    for name in sorted(names):
        payload = git("show", f"HEAD:{name}", text=False).stdout
        files.append(
            {
                "path": name,
                "bytes": len(payload),
                "sha256": hashlib.sha256(payload).hexdigest(),
            }
        )
    (PROOF / "capture-manifest.json").write_text(
        json.dumps({"files": files}, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(f"R181 manifest prepared: {len(files)} committed files")


if __name__ == "__main__":
    main()
