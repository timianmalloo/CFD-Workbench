#!/usr/bin/env python3
"""Fail a branch that shows the coordination-spiral signature.

Signature (docs/lessons/defect-classes.md, COORD-SPIRAL): a verification or
coordination loop that produces many commits, none of which touch src/ or
tests/, and whose subjects are mostly bookkeeping (join, record, ruling,
review, hold, schedule, authorize, handback, handoff, preflight).
"""

import os
import re
import subprocess
import sys
import tempfile

ROOT_MARKERS = ("src", "tests")

BOOKKEEPING_RE = re.compile(
    r"\b(join|record|ruling|rule|review|hold|schedule|authori[sz]e|handback|handoff|preflight)\b",
    re.IGNORECASE,
)
CHORE_JOIN_RE = re.compile(r"^chore\(join\)", re.IGNORECASE)

MIN_COMMITS = 12
BOOKKEEPING_SHARE = 0.6


def is_bookkeeping(subject):
    return bool(BOOKKEEPING_RE.search(subject) or CHORE_JOIN_RE.match(subject))


def touches_product(sha, cwd):
    result = subprocess.run(
        ["git", "diff-tree", "--no-commit-id", "--name-only", "-r", sha],
        cwd=cwd,
        check=True,
        capture_output=True,
        text=True,
    )
    for path in result.stdout.splitlines():
        path = path.strip()
        if not path:
            continue
        first = path.split("/", 1)[0]
        if first in ROOT_MARKERS:
            return True
    return False


def get_merge_base(cwd):
    result = subprocess.run(
        ["git", "merge-base", "HEAD", "main"],
        cwd=cwd,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        return None
    return result.stdout.strip()


def get_commits(base, cwd):
    result = subprocess.run(
        ["git", "log", "--format=%H\t%s", f"{base}..HEAD"],
        cwd=cwd,
        check=True,
        capture_output=True,
        text=True,
    )
    commits = []
    for line in result.stdout.splitlines():
        if not line.strip():
            continue
        sha, _, subject = line.partition("\t")
        commits.append((sha, subject))
    return commits


def evaluate(cwd):
    base = get_merge_base(cwd)
    if base is None:
        print("spiral check: not checked (main not found)", flush=True)
        return 0

    head = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=cwd,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()

    if head == base:
        print("spiral check: not checked (HEAD == base)", flush=True)
        return 0

    commits = get_commits(base, cwd)
    n = len(commits)
    if n == 0:
        print("spiral check: not checked (no commits since base)", flush=True)
        return 0

    product_count = 0
    bookkeeping_count = 0
    for sha, subject in commits:
        if touches_product(sha, cwd):
            product_count += 1
        if is_bookkeeping(subject):
            bookkeeping_count += 1

    pct = round(100 * bookkeeping_count / n)
    base_short = base[:7]

    signature = (
        n >= MIN_COMMITS
        and product_count == 0
        and (bookkeeping_count / n) >= BOOKKEEPING_SHARE
    )

    if signature:
        print(
            f"SPIRAL: {n} commits, 0 product, {pct}% bookkeeping since "
            f"{base_short} — stop the track and report to the operator "
            f"(docs/lessons/defect-classes.md COORD-SPIRAL)",
            flush=True,
        )
        return 1

    print(f"spiral check: ok ({n} commits, {product_count} product)", flush=True)
    return 0


def _init_repo(directory):
    subprocess.run(["git", "init", "-q"], cwd=directory, check=True)
    subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=directory, check=True)
    subprocess.run(["git", "config", "user.name", "Test"], cwd=directory, check=True)


def _commit(directory, filename, content, subject):
    path = os.path.join(directory, filename)
    os.makedirs(os.path.dirname(path), exist_ok=True) if os.path.dirname(filename) else None
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(content)
    subprocess.run(["git", "add", "-A"], cwd=directory, check=True)
    subprocess.run(["git", "commit", "-q", "-m", subject], cwd=directory, check=True)


def _build_case(base_realdir, case_name, commit_specs):
    """Build a throw-away repo: one base commit on main, then commits on a feature branch."""
    directory = os.path.join(base_realdir, case_name)
    os.makedirs(directory, exist_ok=True)
    _init_repo(directory)
    subprocess.run(["git", "checkout", "-q", "-b", "main"], cwd=directory, check=True)
    _commit(directory, "README.md", "base\n", "chore: base commit")
    subprocess.run(["git", "checkout", "-q", "-b", "feature/test"], cwd=directory, check=True)
    for filename, content, subject in commit_specs:
        _commit(directory, filename, content, subject)
    return directory


def self_test():
    bookkeeping_subjects = [
        "record: schedule join",
        "join: record decision",
        "ruling: authorize hold",
        "review: hold for schedule",
        "chore(join): sync",
        "handback: preflight review",
        "authorize: hold ruling",
        "handoff: record join",
        "preflight: schedule review",
        "rule: hold authorize",
        "record: join ruling",
        "review: handback preflight",
    ]
    assert len(bookkeeping_subjects) == 12

    with tempfile.TemporaryDirectory(prefix="check-spiral-selftest-") as raw_dir:
        real_dir = os.path.realpath(raw_dir)

        # Case (a): 12 bookkeeping-only docs commits -> exit 1
        case_a_specs = [
            (f"docs/note-{i}.md", f"note {i}\n", subj)
            for i, subj in enumerate(bookkeeping_subjects)
        ]
        dir_a = _build_case(real_dir, "case_a", case_a_specs)
        rc_a = evaluate(dir_a)
        assert rc_a == 1, f"case (a) expected exit 1, got {rc_a}"

        # Case (b): same but one commit touches src/x.cs -> exit 0
        case_b_specs = list(case_a_specs)
        case_b_specs[0] = ("src/x.cs", "// code\n", case_b_specs[0][2])
        dir_b = _build_case(real_dir, "case_b", case_b_specs)
        rc_b = evaluate(dir_b)
        assert rc_b == 0, f"case (b) expected exit 0, got {rc_b}"

        # Case (c): 12 docs commits with ordinary subjects -> exit 0
        ordinary_subjects = [f"docs: update note {i}" for i in range(12)]
        case_c_specs = [
            (f"docs/note-{i}.md", f"note {i}\n", subj)
            for i, subj in enumerate(ordinary_subjects)
        ]
        dir_c = _build_case(real_dir, "case_c", case_c_specs)
        rc_c = evaluate(dir_c)
        assert rc_c == 0, f"case (c) expected exit 0, got {rc_c}"

        # Case (d): 11 bookkeeping commits -> exit 0
        case_d_specs = [
            (f"docs/note-{i}.md", f"note {i}\n", subj)
            for i, subj in enumerate(bookkeeping_subjects[:11])
        ]
        dir_d = _build_case(real_dir, "case_d", case_d_specs)
        rc_d = evaluate(dir_d)
        assert rc_d == 0, f"case (d) expected exit 0, got {rc_d}"

    print("self-test OK", flush=True)
    return 0


def main():
    if "--self-test" in sys.argv[1:]:
        return self_test()
    return evaluate(os.getcwd())


if __name__ == "__main__":
    sys.exit(main())
