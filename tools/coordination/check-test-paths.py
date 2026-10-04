"""Fail when a Core test reads a repository file by a relative or binary-relative path (defect class TEST-PATH-RELATIVE).

Ring: fast (every join, via tools/check-docs.py). Cost: one read of tests/CfdWorkbench.Core.Tests/*.cs.
The readiness gate tools/verify-application-core.py runs the Core tests from a published copy outside the repository,
so a path like "docs/proof/..." (working-directory relative) or AppContext.BaseDirectory + "..", "src" (binary
relative) resolves to nothing there. Twice on 2026-10-03 such a read passed every join and failed only readiness.
Resolve repository files through PlacementTests.RepoRoot() (the source path, fixed at compile time).
"""
import pathlib
import re
import sys

for _stream in (sys.stdout, sys.stderr):  # cp1252 Windows consoles (verify-portable-text-io; pack-doctor.py:25)
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (OSError, ValueError):
            pass

ROOT = pathlib.Path(__file__).resolve().parents[2]
TESTS = ROOT / "tests" / "CfdWorkbench.Core.Tests"
# A File.* read whose first argument is a string literal naming a repository folder, or a path walked up from the binary.
# docs/examples/foildsl/ is the one repository folder the readiness gate copies into the published run directory
# (tools/verify-application-core.py: shutil.copytree(ROOT / "docs/examples/foildsl", published / ...)), so reads of it
# are allowed. Fixtures/ ships beside the test binary (the csproj copies it), so a walk that reaches Fixtures is allowed.
RELATIVE_READ = re.compile(r'\bFile\.\w+\(\s*"(?:\./)?(?!docs/examples/foildsl/)(?:docs|src|tests|tools|cases)/')
BINARY_WALK = re.compile(r'AppContext\.BaseDirectory\s*,(?:\s*"\.\."\s*,)+\s*"(?:src|docs|tools|cases)"')


def main():
    hits = []
    for path in sorted(TESTS.glob("*.cs")):
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if RELATIVE_READ.search(line) or BINARY_WALK.search(line):
                hits.append(f"{path.relative_to(ROOT)}:{number}: {line.strip()}")
    if hits:
        sys.exit("TEST-PATH-RELATIVE: read repository files through PlacementTests.RepoRoot(), not a relative or "
                 "binary-relative path (readiness runs the tests from a published copy):\n" + "\n".join(hits))
    print("test paths ok: no relative repository reads in the Core tests")


if __name__ == "__main__":
    main()
