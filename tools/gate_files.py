"""Shared file listing for gates that judge working-tree content (GATE-BEFORE-ADD).

`git ls-files` alone hides a new, not-yet-added file, so a gate run before `git add` passes and the
join then fails on that file. `worktree_files` returns tracked files plus untracked, non-ignored
files (`git ls-files --others --exclude-standard`). Gates that judge committed blobs by design
(HEAD) keep their own listing. Cost: one extra `git ls-files` call, about 0.02 s.
"""

import subprocess


def worktree_files(root, *pathspec):
    """Sorted, de-duplicated repo-relative paths: tracked plus untracked and not ignored."""
    found = set()
    for extra in ((), ("--others", "--exclude-standard")):
        out = subprocess.run(["git", "-C", str(root), "ls-files", "-z", *extra, *(("--",) + pathspec if pathspec else ())],
                             capture_output=True, check=True, timeout=60).stdout
        found.update(entry.decode("utf-8", "surrogateescape") for entry in out.split(b"\0") if entry)
    return sorted(found)
