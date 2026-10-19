#!/usr/bin/env python3
"""F1-HOST-BINDING control (Ruling 237, plan docs/plans/f1-rescope-r237.md node C2): the F1 preflight packet's code carries
no host-bound literal. Fails a tracked or untracked .py, .ps1 or .sh file under docs/proof/win-gpu-f1-preflight/packet whose
non-comment line names the old PC's target root (C:\\Projects), the C: mount (/mnt/c/), an architecture-bound apt or
repository token (arch=amd64, x86_64) or the old PC's free-space worksheet numbers (19684091961, 10737418240).

Allowlisted hits (ALLOWLIST: file + stripped line + reason) are the self-test fixtures and the one table that must name
x86_64 (architecture.py's amd64 row); a stale entry also exits 1, so the list only shrinks. `--self-test` plants every
shape. Ring: fast (tools/check-docs.py), one pass over the packet files; cost ~0.05 s.
"""
import re
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
PACKET = "docs/proof/win-gpu-f1-preflight/packet"
SUFFIXES = {".py", ".ps1", ".sh"}
PATTERNS = (
    ("old-target-root", re.compile(r"C:\\+Projects", re.I)),
    ("c-mount", re.compile(r"/mnt/c/")),
    ("apt-arch", re.compile(r"arch=amd64")),
    ("x86_64", re.compile(r"x86_64")),
    ("old-floor", re.compile(r"\b(?:19684091961|10737418240)\b")),
)
# (file, stripped line) -> reason. Fixtures of the packet's own self-tests, and the amd64 table row.
ALLOWLIST = {
    ('architecture.py', '"windows": "X64", "processor_env": "AMD64", "dpkg": "amd64", "uname_m": "x86_64",'):
        'the amd64 table row: the one place the x86_64 name is defined',
    ('architecture.py', '"nvidia_repository": "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/",'):
        'the amd64 table row: the one place the x86_64 name is defined',
    ('transaction.py', 'uri = "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/./nsight_1-1_amd64.deb"'):
        'transaction self-test fixture (URI basename matching)',
    ('packet_selftest.py', '("stage", "/bin/bash /mnt/packet/preflight.sh --stage download --run-root /mnt/c/runAB --run-id runAB"),'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', '("guard", "/usr/bin/python3 /mnt/packet/completed_guard.py watch --session-id runAB --events /mnt/c/runAB/completed-guard-events.jsonl"),'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'assert owned_control.find_groups(foreign, "/mnt/c/runA", "runA", {}, kind) == set(), \\'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'assert owned_control.find_groups(exact_rows, "/mnt/c/runA", "runA", {}, kind) == {123}'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'entry = ("/bin/bash", "/mnt/packet/identity.sh", f"/mnt/c/runA/{owner_name}", "--", *tuple(exact.split()))'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', '"/mnt/c/runA", "runA", records, kind) == {123}'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'wrong_entry = (*entry[:2], f"/mnt/c/runAB/{owner_name}", *entry[3:])'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', '"/mnt/c/runA", "runA", records, kind),'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'exact.replace("/mnt/c/runA", "/mnt/c/runAB"),'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', '"/mnt/c/runA", "runA", {}, kind) == set()'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'expect_failure(lambda: owned_control.find_groups(foreign, "/mnt/c/runA", "runA",'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'foreign_command = command.replace("/mnt/c/runAB", str(here) + "B")'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'root = "/mnt/c/review/run-1/live"'):
        'owned-control self-test fixture (a Linux-side path literal)',
    ('packet_selftest.py', 'cmd = f"/bin/bash /mnt/c/review/packet/preflight.sh --stage download --run-root {root} --packet-root /mnt/c/review/packet --run-id {run_id}"'):
        'owned-control self-test fixture (a Linux-side path literal)',
}


def source_lines(text: str):
    for number, line in enumerate(text.splitlines(), 1):
        if line.lstrip().startswith("#"):
            continue
        yield number, line


def scan(files: dict[str, str]) -> tuple[list[str], list[str]]:
    """files: relative name -> text. Returns (hits not allowlisted, allowlist entries not seen)."""
    hits, seen = [], set()
    for name, text in sorted(files.items()):
        for number, line in source_lines(text):
            found = [label for label, pattern in PATTERNS if pattern.search(line)]
            if not found:
                continue
            key = (name, line.strip())
            if key in ALLOWLIST:
                seen.add(key)
                continue
            hits.append(f"{PACKET}/{name}:{number}: {', '.join(found)}: {line.strip()}")
    stale = [f"{name}: {line[:60]}" for name, line in ALLOWLIST if (name, line) not in seen]
    return hits, stale


def packet_files(root: Path) -> dict[str, str]:
    listed = subprocess.run(["git", "-C", str(root), "ls-files", "--cached", "--others", "--exclude-standard", "--", PACKET],
                            check=True, capture_output=True, text=True, encoding="utf-8").stdout.split("\n")
    files = {}
    for relative in listed:
        path = root / relative
        if relative and Path(relative).suffix in SUFFIXES and path.is_file():
            files[path.name] = path.read_text(encoding="utf-8")
    return files


def self_test() -> None:
    clean = {"a.py": 'target = derived\n# C:\\Projects in a comment is fine\n', "b.sh": '[[ "$run_root" == /mnt/[a-z]/* ]]\n'}
    hits, _ = scan(dict(clean))
    assert not hits, hits
    for label, planted in (("old-target-root", "$TargetPath = 'C:\\Projects\\CFD-Workbench-wsl\\x'\n"),
                           ("old-target-root", 'path = "C:\\\\Projects\\\\x"\n'),
                           ("c-mount", "return '/mnt/c/' + $x\n"),
                           ("apt-arch", "'deb [arch=amd64 signed-by=k] http://h noble main'\n"),
                           ("x86_64", "repos/ubuntu2404/x86_64/\n"),
                           ("old-floor", "$HostStartFloor = [int64]19684091961\n"),
                           ("old-floor", "host_reserve_bytes=10737418240,\n")):
        hits, _ = scan(dict(clean, **{"planted.ps1": planted}))
        assert len(hits) == 1 and label in hits[0], (label, hits)
    hits, stale = scan({"architecture.py": '    "windows": "X64", "processor_env": "AMD64", "dpkg": "amd64", "uname_m": "x86_64",\n'})
    assert not hits and len(stale) == len(ALLOWLIST) - 1, (hits, len(stale))
    with tempfile.TemporaryDirectory() as temporary:
        # An untracked packet file is scanned too (GATE-BEFORE-ADD).
        root = Path(temporary)
        subprocess.run(["git", "-C", str(root), "init", "-q"], check=True)
        packet = root / PACKET
        packet.mkdir(parents=True)
        (packet / "new.sh").write_text("x=/mnt/c/y\n", encoding="utf-8", newline="\n")
        hits, _ = scan(packet_files(root))
        assert len(hits) == 1 and "new.sh:1: c-mount" in hits[0], hits
    print("f1 host bindings self-test: PASS")


def main() -> int:
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    files = packet_files(ROOT)
    if not files:
        print("F1-HOST-BINDING: no packet files found", file=sys.stderr)
        return 1
    hits, stale = scan(files)
    for hit in hits:
        print("F1-HOST-BINDING FAIL: " + hit)
    for entry in stale:
        print("F1-HOST-BINDING stale allowlist entry (remove it): " + entry)
    if hits or stale:
        print("F1-HOST-BINDING FAIL: the packet derives machine paths and architecture names from the repository and the "
              "architecture table (Ruling 237); move the literal or, for a self-test fixture, allowlist the line with a reason")
        return 1
    print(f"f1 host bindings ok: {len(files)} packet files, {len(ALLOWLIST)} allowlisted fixture/table lines, no host-bound literal")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
