#!/usr/bin/env python3
"""conductor-join.py - the join, as a script: every step gated by its exit code, none by a
shell line.

The control for defect class DC-113's fourth recurrence (measured 2026-09-13, a consuming
repo's join): the conductor resolved a register conflict, piped the conflict-marker gate
through `| tail -1`, read the gate's remedy text as a pass, and committed a merge carrying
`<<<<<<<` (DC-136's shape) - caught only because the gate runner was re-run bare before the
push. Three earlier recurrences had the same cause: a hand-typed line at the join whose
status was the formatter's, not the gate's. This script IS the join line. It has no pipes;
each step is a subprocess whose return code decides whether the next runs. The
`execute-with-coordination` skill names it as the only join line.

Steps, in order, stop on the first red (the exit status is the failing step's number):
  0. leader fence                     `coord-core.py leader who --json` BEFORE the merge (and
                                      before --continue): refused with exit 11 (EXIT_FENCE - 0
                                      is success) when the epoch this join carries (--epoch,
                                      default: the ref's epoch at the join's start) is lower
                                      than the ref's, or when the ref cannot be read; an absent
                                      ref is "not applicable" (S2 has no leader)
 0b. dirty-primary fence              refuses dirt outside .agents/log/ before merging or
                                      resuming with --continue (exit 12); changes stay in place
  1. git merge --no-ff <branch>       a conflict stops here with the file list; resolve by hand,
                                      `git add`, `git commit --no-edit`, re-run with --continue
  2. audit marker                     `audit-log.py start --session <s> --skill execute-with-coordination`
                                      so the join's own duration is MEASURED (a resumed or
                                      hand-typed join set none: 14 of 14 join entries carried
                                      no duration, no tier, no fan-out)
  3. verify-no-conflict-markers       ALWAYS, before anything else reads the tree
  4. checks, then the recount         from join.json (`checks`, `recount`); the recount is timed
                                      as recount_seconds - the largest cost centre of a measured
                                      programme (44.8% of the conductor's active main line) and
                                      the one no entry recorded. --docs-only skips the recount.
  5. audit-log append                 tier T1, fan-out 0, the marker's duration, recount_seconds
  6. regenerate                       `regenerate` from join.json, default `coord-core.py regen`
  7. stage own outputs; commit         merge result, changed derived views, audit entry, session logs;
                                      refuse residual dirt before gates or push (exit 7)
  8. gates                            `gates` from join.json, default `run-verify-gates.py`
  9. git push <remote> <branch>       only if 8 passed; --no-push skips
 10. build                            `build` from join.json, optional; --no-build skips

join.json (default docs/coordination/join.json; --join <path> overrides; absent = defaults):
  { "checks":     [["python3", "tools/verify-register.py", "--fix-counts"]],
    "recount":    [["python3", "tools/verify-test-run.py", "--update"]],
    "regenerate": [["python3", "docs/ai-forward-pack/scripts/coord-core.py", "regen"]],
    "gates":      [["python3", "docs/ai-forward-pack/scripts/run-verify-gates.py"]],
    "build":      [["dotnet", "build", "src/App/App.csproj", "-c", "Release"]],
    "push_remote": "origin",
    "trailer_file": "docs/coordination/commit-trailer.txt" }
  A command whose first word is `python3` or `python` runs under THIS interpreter.

Usage (from the checkout the join lands on):
  python3 conductor-join.py <branch> --title "<merge title>" --audit-shortname <name>
      --audit-summary "<text>" --audit-goal "<text>" --audit-done-when "<text>"
      [--artifact <path> ...] [--docs-only] [--no-push] [--no-build] [--continue]
      [--join <join.json>] [--session <id>] [--self-test]

Exit 0 on a complete join; the failing step's number otherwise; 11 when the leader fence (step
0) refuses; 12 for dirty-primary refusal (also --continue); 14 when .agents/artifacts.yml is
absent or invalid (before the merge; the join stages only outputs it declares derived, plus the
audit log, the audit hub it creates and session logs); 13 for a merge refusal without unmerged
paths; 1 for a real conflict or an incomplete merge commit (MERGE_HEAD left); 7 for residual
dirt after committing, and for a refusal while staging (a foreign audit change, unrelated staged
paths, an unreadable git path list or audit hash); 2 on a usage error. Stdlib only.

Recovery after exit 7: gates and push have not run. Read the diagnostic and inspect
`git log -2`, `git status`, and the audit diff. A pre-commit refusal leaves no join chore
commit; the merge may already exist locally. Commit or stash foreign work under its own
session before retrying. A post-commit refusal leaves the join chore commit locally too:
keep it local, separate foreign changes under their author's session, and repair any bad
audit diff before proceeding. Once the tree and audit diff are verified, run the configured
step-8 gates, then the intended push/build steps. Do not blindly retry with --continue:
it performs another step-5 append and creates a second audit entry and chore commit.

Derived-output ownership remains a timing boundary: a foreign session writing an eligible
derived path during a join command can still be included. Joins require exclusive execution.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import runpy
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path
from collections.abc import Callable

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

HERE = Path(__file__).resolve().parent
PY = sys.executable
JOIN_JSON_DEFAULT = os.path.join("docs", "coordination", "join.json")
SKILL = "execute-with-coordination"
# spec-leader-designation US-7: the fence is a named step with its own exit code. Steps are
# 1..10 and 0 is success, so the fence's refusal cannot be "step 0" as a status.
EXIT_FENCE = 11
# Step 0b (investigation join-dirty-primary): an uncommitted change to a tracked path outside
# .agents/log/ in the checkout the join runs in. Git refuses the merge when the branch touches the
# same path ("Your local changes ... would be overwritten by merge"), and the former step 7's
# `git add -A` swept every other such change into the join commit under the join's name. Both are the
# same defect: a register write (a ruling, a logged prompt, an audit append) left uncommitted in
# the primary. The fence names it before step 1 and leaves the change in place.
EXIT_DIRTY = 12
# A nonzero merge without unmerged paths is a refusal, not a conflict (step 1).
EXIT_MERGE_REFUSED = 13
# No artifact registry, or an invalid one: refused before the merge (the join stages only declared outputs).
EXIT_REGISTRY = 14
AUDIT_HUB = "docs/audit/audit-log.md"


def _sibling(name: str) -> str:
    return str(HERE / name)


def repo_root(start: Path | None = None) -> Path | None:
    done = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=str(start or Path.cwd()),
                          capture_output=True, text=True, encoding="utf-8", errors="replace")
    if done.returncode != 0 or not done.stdout.strip():
        return None
    return Path(done.stdout.strip())


def _interp(command: list) -> list[str]:
    command = [str(c) for c in command]
    if command and command[0] in ("python3", "python"):
        command[0] = PY
    return command


def _windows_apps_alias(path: str) -> bool:
    """Recognize the inert WindowsApps command aliases returned by shutil.which."""
    return "windowsapps" in str(path).replace("\\", "/").casefold().split("/")


def _windows_bash(path: str | None = None, which=shutil.which,
                  program_files: list[str] | None = None) -> str:
    """Find a real Bash executable, ignoring Windows' placeholder command aliases."""
    search_path = os.environ.get("PATH", "") if path is None else path
    usable_path = os.pathsep.join(entry for entry in search_path.split(os.pathsep)
                                  if entry and not _windows_apps_alias(entry))
    candidate = which("bash", path=usable_path)
    if candidate and os.path.isfile(candidate) and not _windows_apps_alias(candidate):
        return str(candidate)
    roots = ((os.environ.get("ProgramFiles"), os.environ.get("ProgramFiles(x86)"))
             if program_files is None else program_files)
    if os.name == "nt":
        for root in roots:
            if root:
                candidate = str(Path(root) / "Git" / "bin" / "bash.exe")
                if os.path.isfile(candidate):
                    return candidate
    raise SystemExit(
        "conductor-join: cannot run a .sh join command on Windows: no valid bash.exe found; "
        "install Git for Windows or add its bin directory to PATH (WindowsApps aliases are invalid)"
    )


def _resolve_command(command: list, *, windows: bool | None = None, path: str | None = None,
                     which=shutil.which, program_files: list[str] | None = None) -> list[str]:
    """Resolve configured command tokens at the one subprocess boundary."""
    resolved = [str(token) for token in command]
    on_windows = os.name == "nt" if windows is None else windows
    if on_windows and resolved and resolved[0].casefold().endswith(".sh"):
        return [_windows_bash(path, which, program_files), *resolved]
    return resolved


def load_join(root: Path, path: str | None) -> dict:
    """The join contract for this repository, or the defaults. A malformed file is a usage
    error, never a silent fallback to defaults (a join that skipped the recount because its
    config had a typo would report as complete)."""
    candidate = Path(path) if path else root / JOIN_JSON_DEFAULT
    if not candidate.is_file():
        if path:
            raise SystemExit("conductor-join: --join {0} does not exist".format(candidate))
        return {}
    try:
        data = json.loads(candidate.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise SystemExit("conductor-join: {0} is not valid JSON ({1})".format(candidate, error))
    if not isinstance(data, dict):
        raise SystemExit("conductor-join: {0} must be a JSON object".format(candidate))
    return data


class Join:
    def __init__(self, root: Path, env: dict, log=print):
        self.root = root
        self.env = env
        self.log = log

    def run(self, step: int, what: str, command: list[str], allow=(0,)) -> subprocess.CompletedProcess:
        command = _resolve_command(command)
        self.log("\n== step {0}: {1}\n   $ {2}".format(step, what, " ".join(command)))
        completed = subprocess.run(command, cwd=str(self.root), env=self.env, text=True,
                                   encoding="utf-8", errors="replace", capture_output=True)
        tail = (completed.stdout + completed.stderr).strip().splitlines()
        for line in tail[-8:]:
            self.log("   | " + line[:200])
        if completed.returncode not in allow:
            self.log("\nconductor-join: step {0} ({1}) exited {2} - the join stops here.".format(
                step, what, completed.returncode))
            raise SystemExit(step)
        return completed

    def git(self, *args) -> subprocess.CompletedProcess:
        return subprocess.run(["git", *args], cwd=str(self.root), capture_output=True, text=True,
                              encoding="utf-8", errors="replace")

    def paths(self, *args) -> set[str]:
        result = self.git(*args, "-z")
        if result.returncode:
            self.log("conductor-join: cannot read git paths: " + result.stderr.strip())
            raise SystemExit(7)
        return {p for p in result.stdout.split("\0") if p}

    def dirty_paths(self) -> set[str]:
        return (self.paths("diff", "--no-renames", "--name-only")
                | self.paths("diff", "--cached", "--no-renames", "--name-only")
                | self.paths("ls-files", "--others", "--exclude-standard"))


def join_outputs(j: Join) -> Callable[[], dict[str, bytes | None]]:
    """Reuse the artifact registry's validated parser and longest-pattern classification.

    Only fingerprints changed by a join command become owned outputs. A file merely dirty
    at step 7 is not evidence the join wrote it. Session logs retain their existing exemption.
    """
    core = runpy.run_path(_sibling("coord-core.py"))
    registry = j.root / ".agents"
    # Fail closed before the merge: without a registry the join cannot tell its outputs from foreign
    # work, and every join would end at exit 7 after its merge and commit.
    try:
        loaded = core["load_registry"](registry)
    except core["CoordError"] as exc:
        j.log("\nconductor-join: .agents/artifacts.yml is invalid [COORD-JOIN-NO-REGISTRY]: {0}; "
              "nothing was changed (exit {1}).".format(exc, EXIT_REGISTRY))
        raise SystemExit(EXIT_REGISTRY)
    if loaded is None:
        j.log("\nconductor-join: no .agents/artifacts.yml [COORD-JOIN-NO-REGISTRY]: the join stages only "
              "outputs the registry classifies as derived; run `coord classify init` (or declare "
              "docs/audit/audit-data.js and docs/audit/index.html as derived) and commit it; nothing was "
              "changed (exit {0}).".format(EXIT_REGISTRY))
        raise SystemExit(EXIT_REGISTRY)

    def snapshot() -> dict[str, bytes | None]:
        result = {}
        for path in j.dirty_paths():
            # The audit hub is created by the join's own step-5 append when it is missing (ensure_hub).
            if path == AUDIT_HUB or (path != "docs/audit/audit-log.jsonl"
                                     and core["classify"](registry, path)[0] == "derived"):
                full = j.root / path
                result[path] = hashlib.sha256(full.read_bytes()).digest() if full.is_file() else None
        return result

    return snapshot


def leader_fence(j: Join, given: "int | None", log) -> None:
    """Step 0 - the join carries an epoch and the ref decides (spec-leader-designation US-7).

    Not a `Join.run` step: `run` knows exit codes only and the fence must read the JSON
    (absent is not-applicable; expired/released still carry the epoch). Refuses with
    EXIT_FENCE; never proceeds on an unread fence (R4: NOT CHECKED is not "no leader").
    """
    command = [PY, _sibling("coord-core.py"), "leader", "who", "--json"]
    log("\n== step 0: leader fence\n   $ {0}".format(" ".join(command)))
    done = subprocess.run(command, cwd=str(j.root), env=j.env, text=True, encoding="utf-8",
                          errors="replace", capture_output=True)
    payload = {}
    text = done.stdout.strip()
    if text:
        try:
            payload = json.loads(text.splitlines()[-1])
        except ValueError:
            payload = {}
    state = payload.get("state")
    if done.returncode == 4 or state in (None, "not_checked"):
        for line in (done.stdout + done.stderr).strip().splitlines()[-4:]:
            log("   | " + line[:200])
        log("\nconductor-join: step 0 (leader fence) refused - the leader ref could not be read "
            "[COORD-JOIN-LEADER-NOT-CHECKED]; a join never proceeds on an unread fence "
            "(exit {0}).".format(EXIT_FENCE))
        raise SystemExit(EXIT_FENCE)
    if state == "absent":
        log("   | no leader designated - fence not applicable (S2 has no leader)")
        return
    epoch = int(payload.get("epoch"))
    effective = epoch if given is None else int(given)
    log("   | ref epoch {0} ({1}, {2}); this join carries epoch {3}{4}".format(
        epoch, payload.get("leader") or "released", state, effective,
        "" if given is not None else " (default: read at the join's start)"))
    if effective < epoch:
        log("\nconductor-join: step 0 (leader fence) refused - epoch {0} < ref epoch {1} "
            "[COORD-JOIN-EPOCH-STALE]; the plan predates the current leader. Re-read "
            "`coord leader who`, re-plan under the current leader, then re-run (exit {2})."
            .format(effective, epoch, EXIT_FENCE))
        raise SystemExit(EXIT_FENCE)


def dirty_fence(j: Join, branch: str, log) -> None:
    """Step 0b - a tracked path modified or staged in this checkout, outside .agents/log/, stops
    the join before the merge with EXIT_DIRTY and the list of paths. Session logs are exempt
    because every session appends to its own file and the join's own chore commit carries them.
    The change is never stashed or committed here: whoever wrote it commits it under their name.
    """
    # Join.paths reads with -z (a spaced or non-ASCII path is one entry) and fails closed: a git read
    # that fails is exit 7, never an empty, clean-looking checkout (CTRL-G).
    paths = j.paths
    unstaged = paths("diff", "--name-only")
    staged = paths("diff", "--name-only", "--cached")
    untracked = paths("ls-files", "--others", "--exclude-standard")
    dirty = sorted({p for p in unstaged | staged | untracked if not p.startswith(".agents/log/")})
    on_branch = paths("diff", "--name-only", "HEAD..." + branch) if branch else set()
    log("\n== step 0b: dirty-primary fence")
    if not dirty:
        log("   | no uncommitted or untracked path outside .agents/log/")
        return
    for path in dirty:
        kind = "untracked" if path in untracked else "staged" if path in staged else "modified"
        log("   - {0}  ({1}; {2})".format(path, kind, "the branch also changes it: the merge would refuse"
                                           if path in on_branch else "unrelated work must be committed or stashed before joining"))
    log("\nconductor-join: step 0b (dirty-primary fence) refused - {0} uncommitted change(s) in this "
        "checkout [COORD-JOIN-DIRTY-PRIMARY]; commit them first under the session that wrote them "
        "(a ruling, a logged prompt or an audit append written into the primary is committed before "
        "any join starts), or stash them; nothing was changed (exit {1}).".format(len(dirty), EXIT_DIRTY))
    raise SystemExit(EXIT_DIRTY)


def join(args, root: Path, contract: dict, log=print, *, _after_fence=None) -> int:
    env = dict(os.environ)
    env.setdefault("PYTHONIOENCODING", "utf-8")
    session = args.session or env.get("AGENT_SESSION") or contract.get("session") or "conductor"
    env["AGENT_SESSION"] = session
    env.setdefault("AGENT_NAME", session)
    j = Join(root, env, log)

    branch_now = j.git("branch", "--show-current").stdout.strip()
    if not branch_now:
        log("conductor-join: run from a checkout on a branch (HEAD is detached)")
        return 2

    leader_fence(j, getattr(args, "epoch", None), log)
    dirty_fence(j, args.branch, log)
    snapshot = join_outputs(j)  # the registry is read before the merge
    audit_relative = "docs/audit/audit-log.jsonl"
    audit_path = root / audit_relative

    def audit_hash() -> str | None:
        if not audit_path.exists():
            return None
        hashed = j.git("hash-object", "--", audit_relative)
        if hashed.returncode:
            log("conductor-join: cannot hash audit log (exit 7): " + hashed.stderr)
            raise SystemExit(7)
        return hashed.stdout.strip()

    # Capture immediately after the fence, before any test seam or merge can write.
    audit_expected = audit_hash()
    head_at_fence = j.git("rev-parse", "HEAD").stdout.strip()
    # Private in-process seam: self-tests can reproduce a writer racing the fence.
    # No command-line option or inherited environment variable can activate it.
    if _after_fence is not None:
        _after_fence(root)

    trailer = ""
    trailer_file = args.trailer_file or contract.get("trailer_file")
    if trailer_file:
        trailer = "\n\n" + (root / trailer_file).read_text(encoding="utf-8").strip() + "\n"

    if not args.cont:
        if not args.branch:
            log("conductor-join: a branch is required unless --continue")
            return 2
        log("\n== step 1: merge --no-ff {0} into {1}".format(args.branch, branch_now))
        merge = j.git("merge", "--no-ff", args.branch, "-m", args.title + trailer)
        for line in (merge.stdout + merge.stderr).strip().splitlines()[-6:]:
            log("   | " + line[:200])
        if merge.returncode != 0:
            conflicts = sorted(j.paths("diff", "--name-only", "--diff-filter=U"))
            if not conflicts:
                if j.git("rev-parse", "--verify", "-q", "MERGE_HEAD").returncode == 0:
                    log("\nconductor-join: merge started but its commit did not complete "
                        "[COORD-JOIN-MERGE-INCOMPLETE] (exit 1); MERGE_HEAD exists. "
                        "Inspect git's diagnostic and the staged merge, finish the commit "
                        "by hand, then re-run with --continue.")
                    return 1
                log("\nconductor-join: merge refused [COORD-JOIN-MERGE-REFUSED] (exit {0}); "
                    "git did not create unmerged paths. Git's full refusal follows:".format(EXIT_MERGE_REFUSED))
                # Git names the paths in its own diagnostic (including overwrite refusals).
                # Keep it intact: the normal subprocess tail can omit the refusal heading.
                for line in (merge.stdout + merge.stderr).strip().splitlines():
                    log("   | " + line)
                    if line.startswith("\t"):
                        log("   - " + line.strip())
                return EXIT_MERGE_REFUSED
            log("\nconductor-join: the merge has conflicts - resolve by hand (a DERIVED file is "
                "regenerated, never resolved), `git add` them, `git commit --no-edit`, then "
                "re-run with --continue:")
            for c in conflicts:
                log("   - " + c)
            return 1
    else:
        log("\n== step 1: --continue (the merge was resolved by hand)")

    if j.git("rev-parse", "HEAD").stdout.strip() != head_at_fence:
        # Accept committed audit history brought by the merge, never a worktree append.
        tree = j.git("ls-tree", "HEAD", "--", audit_relative)
        if tree.returncode:
            log("conductor-join: cannot read merged audit baseline (exit 7): " + tree.stderr)
            return 7
        audit_expected = tree.stdout.split()[2] if tree.stdout.strip() else None

    merge_staged = j.paths("diff", "--cached", "--name-only")
    produced = set()

    def run_output(step, what, command):
        before = snapshot()
        result = j.run(step, what, command)
        after = snapshot()
        produced.update(p for p in before.keys() | after.keys()
                        if (p in before) != (p in after) or before.get(p) != after.get(p))
        return result

    # The join's own marker (F-22): one marker measures one join (AL4a), keyed to the skill
    # so a prompt logged meanwhile cannot consume it.
    j.run(2, "audit marker", [PY, _sibling("audit-log.py"), "start", "--session", session, "--skill", SKILL])

    j.run(3, "no conflict markers", [PY, _sibling("verify-no-conflict-markers.py")])

    for command in contract.get("checks") or []:
        run_output(4, "check", _interp(command))
    recount_started = time.monotonic()
    recount = contract.get("recount") or []
    if args.docs_only:
        log("\n== step 4: recount skipped (--docs-only)")
    elif not recount:
        log("\n== step 4: no recount configured (join.json `recount` is empty)")
    else:
        for command in recount:
            run_output(4, "recount", _interp(command))
    recount_seconds = int(time.monotonic() - recount_started)

    audit = [PY, _sibling("audit-log.py"), "append",
             "--shortname", args.audit_shortname, "--session", session,
             "--skill", SKILL, "--kind", "skill", "--tier", "T1", "--fan-out", "0",
             "--prompt", "the join of {0} into {1}".format(args.branch or "the resolved merge", branch_now),
             "--summary", "{0} recount_seconds={1} (docs_only={2}).".format(
                 args.audit_summary, recount_seconds, args.docs_only),
             "--goal", args.audit_goal, "--done-when", args.audit_done_when,
             "--signal-verification-path", "true", "--signal-verification-executed", "true",
             "--signal-acceptance-met", "true"]
    for a in args.artifact:
        audit += ["--artifact", a]
    if audit_hash() != audit_expected:
        log("conductor-join: audit log changed since the fence/committed merge "
            "[COORD-JOIN-AUDIT-CHANGED] (exit 7); own append and commit did not run; "
            "foreign work is kept in docs/audit/audit-log.jsonl.")
        return 7
    audit_before = audit_path.read_bytes() if audit_path.exists() else b""
    appended = run_output(5, "audit entry", audit)
    audit_after = audit_path.read_bytes()
    added = audit_after[len(audit_before):] if audit_after.startswith(audit_before) else b""
    try:
        own_entry = json.loads(added) if len(added.splitlines()) == 1 else {}
    except ValueError:
        own_entry = {}
    if (not added.endswith(b"\n") or not isinstance(own_entry, dict)
            or own_entry.get("id") != appended.stdout.strip()):
        log("conductor-join: audit append was not exactly the join's one entry "
            "[COORD-JOIN-AUDIT-CHANGED] (exit 7); commit did not run.")
        return 7
    audit_expected = audit_hash()
    produced.add(audit_relative)

    regenerate = contract.get("regenerate")
    if regenerate is None:
        regenerate = [[PY, _sibling("coord-core.py"), "regen"]]
    for command in regenerate:
        run_output(6, "regenerate derived views", _interp(command))

    if audit_hash() != audit_expected:
        log("conductor-join: audit log changed after the own append "
            "[COORD-JOIN-AUDIT-CHANGED] (exit 7); commit did not run; foreign work is kept.")
        return 7
    dirty_now = j.dirty_paths()
    produced.intersection_update(dirty_now)
    produced.update(p for p in dirty_now if p.startswith(".agents/log/"))
    unexpected_staged = j.paths("diff", "--cached", "--name-only") - merge_staged - produced
    if unexpected_staged:
        log("conductor-join: unrelated staged paths appeared after the fence; refusing commit (exit 7):")
        for path in sorted(unexpected_staged):
            log("   - " + path)
        return 7
    if produced:
        j.run(7, "stage join outputs", ["git", "--literal-pathspecs", "add", "-A", "--", *sorted(produced)])
    j.run(7, "commit", ["git", "commit", "-q", "-m",
                        "chore(join): {0} - the join's audit entry, derived views, floors{1}".format(
                            args.audit_shortname, trailer)])

    audit_diff = j.git("diff", "--numstat", "--no-renames", "HEAD^", "HEAD", "--", audit_relative)
    if audit_diff.returncode or audit_diff.stdout.split() != ["1", "0", audit_relative]:
        log("conductor-join: join commit audit diff is not exactly one added line "
            "[COORD-JOIN-AUDIT-DIFF] (exit 7); commit exists locally; gates and push did not run. "
            "Inspect and repair the local commit; --continue would append a second entry.")
        return 7

    status = j.git("status", "--porcelain=v1", "-z", "--untracked-files=all")
    if status.returncode:
        log("conductor-join: post-commit status could not be read (exit 7): " + status.stderr)
        return 7
    records = iter(status.stdout.split("\0"))
    remaining = set()
    for record in records:
        if not record:
            continue
        paths = [record[3:]]
        if "R" in record[:2] or "C" in record[:2]:
            paths.append(next(records))  # porcelain -z carries the rename/copy source too.
        remaining.update(p for p in paths if not p.startswith(".agents/log/"))
    if remaining:
        log("conductor-join: residual dirt after join commit [COORD-JOIN-DIRTY-AFTER-COMMIT] "
            "(exit 7); gates and push did not run; changes kept:")
        for path in sorted(remaining):
            log("   - " + path)
        return 7

    gates = contract.get("gates")
    if gates is None:
        gates = [[PY, _sibling("run-verify-gates.py")]]
    for command in gates:
        j.run(8, "every verify gate", _interp(command))

    if args.no_push:
        log("\n== step 9: push skipped (--no-push)")
    else:
        j.run(9, "push", ["git", "push", contract.get("push_remote") or "origin", branch_now])

    sha = j.git("rev-parse", "--short", "HEAD").stdout.strip()
    build = contract.get("build") or []
    if args.no_build or not build:
        log("\n== step 10: build skipped ({0}); {1} at {2}".format(
            "--no-build" if args.no_build else "none configured", branch_now, sha))
    else:
        for command in build:
            j.run(10, "build", _interp(command))
        log("\nconductor-join: complete - {0} at {1}; build ran".format(branch_now, sha))
    return 0


def self_test() -> int:
    """Two joins in a throwaway repository, both with --no-push --docs-only and no gates:
    (a) a clean branch completes, and the audit entry carries tier T1, fan_out 0 and a
    measured duration; (b) a branch that commits a file with a conflict marker - DC-136's
    shape, a hand-resolved file - stops at step 3 with NO join commit."""
    def git(cwd, *a):
        done = subprocess.run(["git", *a], cwd=str(cwd), capture_output=True, text=True,
                              encoding="utf-8", errors="replace")
        if done.returncode != 0:
            raise AssertionError("git {0}: {1}".format(" ".join(a), done.stderr))
        return done.stdout

    def audit_fixture(root):
        # Match the consuming tree: the marker is local, and audit render outputs are derived.
        (root / ".gitignore").write_text("docs/audit/.run-starts.json*\n", encoding="utf-8", newline="\n")
        (root / ".agents").mkdir(exist_ok=True)
        (root / ".agents" / "artifacts.yml").write_text(
            "docs/audit/audit-data.js: derived python audit-log.py render\n"
            "docs/audit/index.html: derived python audit-log.py render\n"
            "generated/*: derived python generate.py\n", encoding="utf-8", newline="\n")
        (root / "docs" / "audit").mkdir(parents=True, exist_ok=True)
        (root / "docs" / "audit" / "audit-log.md").write_text("audit reader\n", encoding="utf-8", newline="\n")

    problems = []
    # Regression: configured .sh commands must be translated for Windows, and the runner must
    # continue to stop on the child status after translation.
    with tempfile.TemporaryDirectory() as command_tmp:
        fixture = Path(command_tmp)
        bash = fixture / "Git" / "bin" / "bash.exe"
        bash.parent.mkdir(parents=True)
        bash.write_bytes(b"fixture")
        resolved = _resolve_command(["tools/join-ring.sh"], windows=True,
                                    path=str(bash.parent),
                                    which=lambda name, path=None: str(bash))
        if resolved != [str(bash), "tools/join-ring.sh"]:
            problems.append("a Windows .sh command was not prefixed with the resolved Bash executable")
        try:
            _resolve_command(["tools/join-ring.sh"], windows=True, path="",
                             which=lambda name, path=None: None, program_files=[])
        except SystemExit as exc:
            if "no valid bash.exe" not in str(exc):
                problems.append("missing Bash did not produce the specific fail-closed diagnostic")
        else:
            problems.append("a Windows .sh command ran without a valid Bash executable")
        alias = fixture / "WindowsApps" / "bash.exe"
        alias.parent.mkdir()
        alias.write_bytes(b"fixture")
        try:
            alias_result = _resolve_command(["tools/join-ring.sh"], windows=True, path=str(alias.parent),
                                            which=lambda name, path=None: str(alias), program_files=[])
            if alias_result[0] == str(alias):
                problems.append("a WindowsApps Bash alias was accepted as a valid shell")
        except SystemExit as exc:
            if "WindowsApps" not in str(exc):
                problems.append("a WindowsApps Bash alias was rejected without a specific diagnostic")
        if os.name == "nt":
            script = fixture / "probe.sh"
            script.write_text("exit 7\n", encoding="utf-8", newline="\n")
            try:
                Join(Path.cwd(), dict(os.environ), log=lambda _: None).run(4, "shell exit probe", [str(script)])
                problems.append("a Windows shell script's nonzero exit was allowed through the join runner")
            except SystemExit as exc:
                if exc.code != 4:
                    problems.append("a Windows shell script did not stop at its assigned join step")

    try:
        Join(Path.cwd(), dict(os.environ), log=lambda _: None).run(
            4, "intentional red command", [PY, "-c", "raise SystemExit(7)"])
        problems.append("a nonzero command was allowed through the join runner")
    except SystemExit as exc:
        if exc.code != 4:
            problems.append("a nonzero command did not stop at its assigned join step")

    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "repo"
        repo.mkdir()
        git(tmp, "init", "-q", "-b", "main", str(repo))
        git(repo, "config", "user.email", "join@example.invalid")
        git(repo, "config", "user.name", "join")
        (repo / "docs" / "audit").mkdir(parents=True)
        (repo / "README.md").write_text("base\n", encoding="utf-8", newline="\n")
        audit_fixture(repo)
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "init")
        contract = {"regenerate": [], "gates": [["python3", "-c", "print('gates ok')"]]}
        (repo / "join.json").write_text(json.dumps(contract), encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "join contract")

        # (a) the clean branch
        git(repo, "checkout", "-q", "-b", "feature/clean")
        (repo / "clean.txt").write_text("done\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "clean work")
        git(repo, "checkout", "-q", "main")
        lines = []
        args = _parser().parse_args(["feature/clean", "--title", "merge clean", "--audit-shortname",
                                     "join-clean", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != 0:
            problems.append("a clean join exited {0}: {1}".format(status, "\n".join(lines[-12:])))
        else:
            head = git(repo, "log", "-1", "--format=%s").strip()
            if not head.startswith("chore(join): join-clean"):
                problems.append("the join commit is not at HEAD ({0!r})".format(head))
            entries = [json.loads(l) for l in (repo / "docs" / "audit" / "audit-log.jsonl")
                       .read_text(encoding="utf-8").splitlines() if l.strip()]
            entry = entries[-1] if entries else {}
            if entry.get("tier") != "T1" or entry.get("fan_out") != 0:
                problems.append("the join entry lacks tier T1 / fan_out 0: {0}".format(entry))
            if "duration_seconds" not in entry:
                problems.append("the join entry carries no measured duration")
            if "recount_seconds=" not in entry.get("summary", ""):
                problems.append("the join entry does not record recount_seconds")

        def settle():
            # An unfenced join (the red state) sweeps the case's change into its commit or leaves a half-started
            # merge; clear both so the next case starts from a clean index and the run reports instead of raising.
            subprocess.run(["git", "merge", "--abort"], cwd=str(repo), capture_output=True)
            git(repo, "reset", "-q", "--hard", "HEAD")

        # (d) the dirty-primary fence (investigation join-dirty-primary): an uncommitted change to a
        # tracked path in the checkout - refused at step 0b with EXIT_DIRTY, NO merge, the change kept.
        git(repo, "checkout", "-q", "-b", "feature/dirty")
        (repo / "dirty.txt").write_text("branch work\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "work while the primary is dirty")
        git(repo, "checkout", "-q", "main")
        (repo / "README.md").write_text("base\nan uncommitted register write\n", encoding="utf-8", newline="\n")
        before = git(repo, "rev-parse", "HEAD").strip()
        lines = []
        args = _parser().parse_args(["feature/dirty", "--title", "merge dirty", "--audit-shortname",
                                     "join-dirty", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != EXIT_DIRTY:
            problems.append("an uncommitted change in the checkout did not stop at the dirty-primary "
                            "fence (exit {0})".format(status))
        if git(repo, "rev-parse", "HEAD").strip() != before:
            problems.append("the merge ran although the dirty-primary fence refused")
        if not any("COORD-JOIN-DIRTY-PRIMARY" in line for line in lines):
            problems.append("the dirty-primary refusal is not named")
        if not any(line.startswith("   - README.md  (modified; unrelated work must be committed") for line in lines):
            problems.append("the dirty-primary fence did not list README.md as a modified path the join would sweep")
        if "an uncommitted register write" not in (repo / "README.md").read_text(encoding="utf-8"):
            problems.append("the dirty-primary fence changed or dropped the uncommitted change")
        settle()
        # (f) an untracked file is refused too: step 7's `git add -A` would commit it under the join's name.
        (repo / "docs" / "new note.md").write_text("an uncommitted note\n", encoding="utf-8", newline="\n")
        lines = []
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != EXIT_DIRTY:
            problems.append("an untracked file in the checkout did not stop at the dirty-primary fence (exit {0})".format(status))
        if not any(line.startswith("   - docs/new note.md  (untracked;") for line in lines):
            problems.append("the dirty-primary fence did not list the untracked path as one entry")
        if (repo / "docs" / "new note.md").exists():
            (repo / "docs" / "new note.md").unlink()
        settle()
        # (g) the incident's own shape: a staged change to a path the branch also changes - refused at step 0b,
        # named as such, and the merge never ran (git would have refused it with a conflict-shaped message).
        git(repo, "checkout", "-q", "feature/dirty")
        (repo / "README.md").write_text("base\nchanged on the branch\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "the branch changes README")
        git(repo, "checkout", "-q", "main")
        (repo / "README.md").write_text("base\na staged register write\n", encoding="utf-8", newline="\n")
        git(repo, "add", "README.md")
        before = git(repo, "rev-parse", "HEAD").strip()
        lines = []
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != EXIT_DIRTY:
            problems.append("a staged change to a path the branch also changes did not stop at the dirty-primary "
                            "fence (exit {0})".format(status))
        if not any(line.startswith("   - README.md  (staged; the branch also changes it") for line in lines):
            problems.append("the fence did not name the staged path as one the branch also changes")
        if git(repo, "rev-parse", "HEAD").strip() != before:
            problems.append("the merge ran although the dirty-primary fence refused the staged change")
        settle()
        # (e) a session log is exempt: a modified .agents/log/ file alone passes the fence and the join lands.
        (repo / ".agents" / "log").mkdir(parents=True, exist_ok=True)
        (repo / ".agents" / "log" / "selftest.jsonl").write_text("{}\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "a session log")
        (repo / ".agents" / "log" / "selftest.jsonl").write_text("{}\n{}\n", encoding="utf-8", newline="\n")
        lines = []
        args = _parser().parse_args(["feature/dirty", "--title", "merge dirty", "--audit-shortname",
                                     "join-dirty", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != 0:
            problems.append("a modified session log alone stopped the join (exit {0}): {1}".format(
                status, "\n".join(lines[-6:])))
        settle()

        # (b) the hand-resolved file carrying a marker
        git(repo, "checkout", "-q", "-b", "feature/markers")
        (repo / "resolved-by-hand.md").write_text(
            "before\n" + "<" * 7 + " HEAD\nmine\n=======\ntheirs\n" + ">" * 7 + " theirs\n",
            encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "a file resolved by hand")
        git(repo, "checkout", "-q", "main")
        before = git(repo, "rev-parse", "HEAD").strip()
        lines = []
        args = _parser().parse_args(["feature/markers", "--title", "merge markers", "--audit-shortname",
                                     "join-markers", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != 3:
            problems.append("a merge carrying a conflict marker did not stop at step 3 (exit {0})".format(status))
        after = git(repo, "log", "-1", "--format=%s").strip()
        if after.startswith("chore(join)"):
            problems.append("a join commit was made over a conflict marker")
        if git(repo, "rev-parse", "HEAD").strip() == before:
            problems.append("precondition: the merge itself should have landed before the gate")

        # (c) the leader fence: a ref at epoch 2 and a plan carrying epoch 1 - refused at step 0
        # with EXIT_FENCE and NO merge (spec-leader-designation US-7).
        git(repo, "checkout", "-q", "-b", "feature/fenced")
        (repo / "fenced.txt").write_text("late\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "work under an old epoch")
        git(repo, "checkout", "-q", "main")
        now = time.time()
        blob = subprocess.run(["git", "hash-object", "-w", "--stdin"], cwd=str(repo),
                              input=json.dumps({"leader": "lead", "epoch": 2, "pinned_at": now,
                                                "expires_at": now + 300, "ttl": 300,
                                                "host": "claude", "tree": "primary"}),
                              capture_output=True, text=True, encoding="utf-8").stdout.strip()
        git(repo, "update-ref", "refs/coord/leader", blob, "0" * 40)
        before = git(repo, "rev-parse", "HEAD").strip()
        lines = []
        args = _parser().parse_args(["feature/fenced", "--title", "merge fenced", "--audit-shortname",
                                     "join-fenced", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest",
                                     "--epoch", "1"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != EXIT_FENCE:
            problems.append("a plan carrying epoch 1 against a ref at epoch 2 did not stop at the "
                            "fence (exit {0})".format(status))
        if git(repo, "rev-parse", "HEAD").strip() != before:
            problems.append("the merge ran although the leader fence refused")
        if not any("COORD-JOIN-EPOCH-STALE" in line for line in lines):
            problems.append("the fence's refusal is not named")
    # R2 cases use independent repositories so a failed assertion cannot poison another case.
    def r2_case(name, probe):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            git(root, "init", "-q", "-b", "main")
            git(root, "config", "user.email", "join@example.invalid")
            git(root, "config", "user.name", "join")
            (root / "README.md").write_text("base\n", encoding="utf-8", newline="\n")
            audit_fixture(root)
            git(root, "add", "-A")
            git(root, "commit", "-qm", "base")
            git(root, "checkout", "-qb", "feature")
            (root / "branch.txt").write_text("branch\n", encoding="utf-8", newline="\n")
            git(root, "add", "branch.txt")
            git(root, "commit", "-qm", "feature")
            git(root, "checkout", "-q", "main")
            args = _parser().parse_args(["feature", "--title", "merge", "--audit-shortname",
                                         "r2", "--no-push", "--docs-only", "--session", "selftest"])
            lines = []
            contract = {"regenerate": [], "gates": []}

            def run(hook=None, regenerate=None, checks=None):
                if regenerate is not None:
                    contract["regenerate"] = regenerate
                if checks is not None:
                    contract["checks"] = checks
                try:
                    return join(args, root, contract, log=lines.append, _after_fence=hook)
                except SystemExit as exc:
                    return exc.code

            if not probe(root, args, lines, run):
                problems.append(name)
                print("conductor-join --self-test: FAILED - " + name)
            else:
                print("conductor-join --self-test: OK - " + name)

    def late_write(root, args, lines, run):
        status = run(lambda r: (r / "README.md").write_text("late write\n", encoding="utf-8", newline="\n"))
        return (git(root, "show", "HEAD:README.md").strip() == "base"
                and (root / "README.md").read_text(encoding="utf-8") == "late write\n"
                and status == 7 and any("COORD-JOIN-DIRTY-AFTER-COMMIT" in l for l in lines)
                and any("README.md" in l for l in lines)
                and not any("step 8:" in l for l in lines))

    def refusal(root, args, lines, run):
        status = run(lambda r: (r / "branch.txt").write_text("untracked writer\n", encoding="utf-8", newline="\n"))
        return (status == 13 and any("COORD-JOIN-MERGE-REFUSED" in l for l in lines)
                and any(l == "   - branch.txt" for l in lines)
                and not any("the merge has conflicts" in l for l in lines)
                and (root / "branch.txt").read_text(encoding="utf-8") == "untracked writer\n")

    def tracked_refusal(root, args, lines, run):
        git(root, "checkout", "-q", "feature")
        (root / "README.md").write_text("feature\n", encoding="utf-8", newline="\n")
        git(root, "commit", "-qam", "change README")
        git(root, "checkout", "-q", "main")
        status = run(lambda r: (r / "README.md").write_text("local writer\n", encoding="utf-8", newline="\n"))
        return (status == 13 and any("COORD-JOIN-MERGE-REFUSED" in l for l in lines)
                and any(l == "   - README.md" for l in lines)
                and not any("the merge has conflicts" in l for l in lines))

    def conflict(root, args, lines, run):
        git(root, "checkout", "-q", "feature")
        (root / "README.md").write_text("feature\n", encoding="utf-8", newline="\n")
        git(root, "commit", "-qam", "feature README")
        git(root, "checkout", "-q", "main")
        (root / "README.md").write_text("main\n", encoding="utf-8", newline="\n")
        git(root, "commit", "-qam", "main README")
        status = run()
        return (status == 1 and any("the merge has conflicts" in l for l in lines)
                and any(l == "   - README.md" for l in lines))

    def continued(root, args, lines, run):
        git(root, "merge", "--no-ff", "feature", "-m", "hand committed merge")
        args.cont = True
        args.branch = None
        before = git(root, "rev-parse", "HEAD")
        (root / "README.md").write_text("unrelated\n", encoding="utf-8", newline="\n")
        status = run()
        return (status == EXIT_DIRTY and git(root, "rev-parse", "HEAD") == before
                and (root / "README.md").read_text(encoding="utf-8") == "unrelated\n"
                and any("COORD-JOIN-DIRTY-PRIMARY" in l for l in lines))

    def clean_continued(root, args, lines, run):
        git(root, "merge", "--no-ff", "feature", "-m", "hand committed merge")
        args.cont = True
        args.branch = None
        return run() == 0 and not git(root, "status", "--porcelain").strip()

    def derived_outputs(root, args, lines, run):
        (root / "generated").mkdir()
        (root / "generated" / "deleted.txt").write_text("old\n", encoding="utf-8", newline="\n")
        (root / "generate.py").write_text(
            "from pathlib import Path\n"
            "Path('generated/deleted.txt').unlink()\n"
            "Path('generated/with space.txt').write_text('derived', encoding='utf-8')\n",
            encoding="utf-8", newline="\n")
        git(root, "add", "-A")
        git(root, "commit", "-qm", "generator")
        status = run(regenerate=[[PY, "generate.py"]])
        return (status == 0 and git(root, "show", "HEAD:generated/with space.txt") == "derived"
                and not (root / "generated" / "deleted.txt").exists()
                and not git(root, "status", "--porcelain").strip())

    def audit_writer_fixture(root):
        (root / "docs/audit/audit-log.jsonl").write_text('{"id":"base"}\n', encoding="utf-8", newline="\n")
        (root / "foreign.py").write_text(
            "from pathlib import Path\n"
            "with Path('docs/audit/audit-log.jsonl').open('a', encoding='utf-8') as f:\n"
            "    f.write('{\"id\":\"foreign\"}\\n')\n", encoding="utf-8", newline="\n")
        git(root, "add", "docs/audit/audit-log.jsonl", "foreign.py")
        git(root, "commit", "-qm", "audit baseline and foreign writer")

    def foreign_audit(root, args, lines, run):
        audit_writer_fixture(root)
        status = run(checks=[[PY, "foreign.py"]])
        carried = '"foreign"' in git(root, "show", "HEAD:docs/audit/audit-log.jsonl")
        if carried:
            print("conductor-join --self-test: evidence - HEAD carries the foreign audit line")
        return (status == 7 and not carried
                and '"foreign"' in (root / "docs/audit/audit-log.jsonl").read_text(encoding="utf-8")
                and any("COORD-JOIN-AUDIT-CHANGED" in l for l in lines)
                and not any("step 5:" in l for l in lines))

    def foreign_audit_after_append(root, args, lines, run):
        audit_writer_fixture(root)
        status = run(regenerate=[[PY, "foreign.py"]])
        return (status == 7 and '"foreign"' not in git(root, "show", "HEAD:docs/audit/audit-log.jsonl")
                and any("COORD-JOIN-AUDIT-CHANGED" in l for l in lines)
                and not any("step 7: commit" in l for l in lines))

    def audit_commit_diff(root, args, lines, run):
        audit_writer_fixture(root)
        # Install only during step 6, so the hook affects the join's chore commit, not its merge.
        hook = "#!/bin/sh\nprintf '%s\\n' '{\"id\":\"foreign\"}' >> docs/audit/audit-log.jsonl\ngit add docs/audit/audit-log.jsonl\n"
        (root / "install_hook.py").write_text(
            "from pathlib import Path\n"
            "p = Path('.git/hooks/pre-commit')\n"
            "p.write_text(" + repr(hook) + ", encoding='utf-8')\n"
            "p.chmod(0o755)\n", encoding="utf-8", newline="\n")
        git(root, "add", "install_hook.py")
        git(root, "commit", "-qm", "hook installer")
        status = run(regenerate=[[PY, "install_hook.py"]])
        return (status == 7 and any("COORD-JOIN-AUDIT-DIFF" in l for l in lines)
                and not any("step 8:" in l for l in lines))

    def merged_audit_history(root, args, lines, run):
        git(root, "checkout", "-q", "feature")
        (root / "docs/audit/audit-log.jsonl").write_text('{"id":"branch-history"}\n', encoding="utf-8", newline="\n")
        git(root, "add", "docs/audit/audit-log.jsonl")
        git(root, "commit", "-qm", "committed branch audit history")
        git(root, "checkout", "-q", "main")
        status = run()
        entries = [json.loads(line) for line in git(root, "show", "HEAD:docs/audit/audit-log.jsonl").splitlines()]
        return status == 0 and len(entries) == 2 and entries[0]["id"] == "branch-history"

    def transient_output(root, args, lines, run):
        (root / "generate.py").write_text(
            "from pathlib import Path\n"
            "p = Path('generated/transient.txt')\n"
            "p.unlink() if p.exists() else (p.parent.mkdir(exist_ok=True), p.write_text('temporary'))\n",
            encoding="utf-8", newline="\n")
        git(root, "add", "generate.py")
        git(root, "commit", "-qm", "transient generator")
        status = run(regenerate=[[PY, "generate.py"], [PY, "generate.py"]])
        return status == 0 and not git(root, "status", "--porcelain").strip()

    def merge_hook_failure(root, args, lines, run):
        hook = root / ".git/hooks/pre-merge-commit"
        hook.write_text("#!/bin/sh\nexit 1\n", encoding="utf-8", newline="\n")
        hook.chmod(0o755)
        status = run()
        return (status == 1 and any("COORD-JOIN-MERGE-INCOMPLETE" in l for l in lines)
                and not any("COORD-JOIN-MERGE-REFUSED" in l for l in lines)
                and (root / ".git/MERGE_HEAD").is_file())

    def registry_refused(contents):
        def probe(root, args, lines, run):
            registry = root / ".agents" / "artifacts.yml"
            if contents is None:
                git(root, "rm", "-q", ".agents/artifacts.yml")
            else:
                registry.write_text(contents, encoding="utf-8", newline="\n")
                git(root, "add", ".agents/artifacts.yml")
            git(root, "commit", "-qm", "registry under test")
            head = git(root, "rev-parse", "HEAD").strip()
            status = run()
            return (status == EXIT_REGISTRY and git(root, "rev-parse", "HEAD").strip() == head
                    and any("COORD-JOIN-NO-REGISTRY" in l for l in lines)
                    and not git(root, "status", "--porcelain").strip())
        return probe

    def hub_missing(root, args, lines, run):
        # A fresh install: the step-5 append creates the audit hub, and the join commits it as its own.
        # docs/audit/ keeps its log, so audit-log.py still writes the product tree, not .agents/log.
        (root / "docs/audit/audit-log.jsonl").write_text('{"id":"base"}\n', encoding="utf-8", newline="\n")
        git(root, "rm", "-q", AUDIT_HUB)
        git(root, "add", "docs/audit/audit-log.jsonl")
        git(root, "commit", "-qm", "no hub yet")
        status = run()
        return (status == 0 and git(root, "cat-file", "-e", "HEAD:" + AUDIT_HUB) == ""
                and not git(root, "status", "--porcelain").strip())

    r2_case("R2 registry: no .agents/artifacts.yml refused before the merge, exit 14", registry_refused(None))
    r2_case("R2 registry: an invalid registry refused before the merge, exit 14", registry_refused("no colon here\n"))
    r2_case("R2 registry: a missing audit hub is created and committed by the join", hub_missing)
    r2_case("R2 case A: post-fence write stays dirty, outside commit; post-commit check exits 7", late_write)
    r2_case("R2 case B: untracked overwrite refusal names path and exits 13", refusal)
    r2_case("R2 case B: tracked overwrite refusal names path and exits 13", tracked_refusal)
    r2_case("R2 real conflict: names path and exits 1", conflict)
    r2_case("R2 case C: --continue fences unrelated dirt", continued)
    r2_case("R2 case C: clean --continue leaves empty porcelain", clean_continued)
    r2_case("R2 derived outputs: deletion and spaced path committed; porcelain empty", derived_outputs)
    r2_case("R2 repair: foreign audit append before step 5 refused, not carried", foreign_audit)
    r2_case("R2 repair: foreign audit append at step 6 refused, not carried", foreign_audit_after_append)
    r2_case("R2 repair: post-commit audit diff must be exactly one added line", audit_commit_diff)
    r2_case("R2 repair: committed branch audit history remains joinable", merged_audit_history)
    r2_case("R2 repair: transient generated path does not break staging", transient_output)
    r2_case("R2 repair: failed merge hook leaves MERGE_HEAD, not a refusal", merge_hook_failure)
    if problems:
        print("conductor-join --self-test: FAILED - " + "; ".join(problems))
        return 1
    print("conductor-join --self-test: OK - Windows .sh command resolution rejects WindowsApps aliases; "
          "nonzero child status stops its join step; a clean join completes with a measured T1 entry; "
          "conflict markers and stale epochs stop before the join commit; an uncommitted change in the "
          "checkout stops at the dirty-primary fence and a session log alone does not; "
          "R2 cases A/B/C, derived outputs, and repair-cycle audit/merge guards pass")
    return 0


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0],
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("branch", nargs="?", help="the branch to merge (omit with --continue)")
    parser.add_argument("--title", default="", help="the merge commit's title (the trailer is appended)")
    parser.add_argument("--audit-shortname", help="the join's audit shortname")
    parser.add_argument("--audit-summary", default="join")
    parser.add_argument("--audit-goal", default="")
    parser.add_argument("--audit-done-when", default="")
    parser.add_argument("--artifact", action="append", default=[], help="proof paths for the audit entry")
    parser.add_argument("--docs-only", action="store_true", help="skip the recount (no test or product change)")
    parser.add_argument("--continue", dest="cont", action="store_true",
                        help="the merge was resolved and committed by hand; fence the tree, then start at step 2")
    parser.add_argument("--no-push", action="store_true")
    parser.add_argument("--no-build", action="store_true")
    parser.add_argument("--join", help="the join contract (default docs/coordination/join.json)")
    parser.add_argument("--session", help="audit session id (default $AGENT_SESSION, then join.json, then 'conductor')")
    parser.add_argument("--trailer-file", dest="trailer_file", help="text appended to every commit message")
    parser.add_argument("--epoch", type=int, default=None,
                        help="the leader epoch this join's plan carries (`coord leader who`); "
                             "default: the ref's epoch at the join's start. Lower than the "
                             "ref's -> refused at step 0 with exit {0}".format(EXIT_FENCE))
    parser.add_argument("--self-test", action="store_true", help="prove a red step stops the join")
    return parser


def main(argv: list[str]) -> int:
    args = _parser().parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.audit_shortname or not args.title:
        print("conductor-join: --title and --audit-shortname are required", file=sys.stderr)
        return 2
    root = repo_root()
    if root is None:
        print("conductor-join: not inside a git repository", file=sys.stderr)
        return 2
    contract = load_join(root, args.join)
    try:
        return join(args, root, contract)
    except SystemExit as exc:
        return int(exc.code or 0)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
