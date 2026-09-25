#!/usr/bin/env python3
"""Observe live leadership and exact checkout context before an application join."""
import argparse
import json
import math
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "docs/ai-forward-pack/scripts"
sys.path.insert(0, str(PACK))
from platform_process import (WindowsJob, release_windows_gate, spawn_windows_gate,
                              terminate_owned_process, wait_after_termination)

CAP = 4 * 1024 * 1024


class Refused(Exception):
    pass


def require(ok, code):
    if not ok:
        raise Refused(code)


def finite(value):
    return type(value) in (int, float) and math.isfinite(value)


def command(argv, cwd, env, timeout):
    """Reuse pack process ownership; keep bounded raw failure output as evidence."""
    started = time.time()
    monotonic = time.monotonic()
    row = {"argv": argv, "started_at": started, "monotonic_start": monotonic,
           "exit": None, "error": None}
    job = None
    child = None
    with tempfile.TemporaryFile() as out, tempfile.TemporaryFile() as err:
        try:
            if os.name == "nt":
                child = spawn_windows_gate(argv, cwd=cwd, env=env, stdout=out,
                                           stderr=err, closed_stdin=True)
                job = WindowsJob(child, process_limit=64)
                require(job.error is None and job.handle is not None, "OC-PROCESS-CONTAINMENT")
                require(release_windows_gate(child, close_after=True) is None,
                        "OC-PROCESS-GATE")
            else:
                child = subprocess.Popen(argv, cwd=cwd, env=env, stdout=out,
                                         stderr=err, start_new_session=True)
            row["pid"] = child.pid
            while child.poll() is None:
                require(time.monotonic() - monotonic < timeout, "OC-PROCESS-TIMEOUT")
                require(os.fstat(out.fileno()).st_size <= CAP and
                        os.fstat(err.fileno()).st_size <= CAP, "OC-PROCESS-OUTPUT-LIMIT")
                time.sleep(0.02)
            row["exit"] = child.returncode
            require(os.fstat(out.fileno()).st_size <= CAP and
                    os.fstat(err.fileno()).st_size <= CAP, "OC-PROCESS-OUTPUT-LIMIT")
        except (OSError, Refused, KeyboardInterrupt) as exc:
            row["error"] = str(exc) or "OC-INTERRUPTED"
        finally:
            if child is not None:
                # Kill only the group/job we created, including any surviving descendants.
                row["cleanup_error"] = terminate_owned_process(child, job)
                row["exit"], wait_error = wait_after_termination(child, job)
                if wait_error:
                    row["cleanup_error"] = wait_error
            if job is not None:
                job.close()
            out.seek(0)
            err.seek(0)
            row["stdout"] = out.read(CAP).decode("utf-8", errors="replace")
            row["stderr"] = err.read(CAP).decode("utf-8", errors="replace")
    row["ended_at"] = time.time()
    row["duration_seconds"] = time.monotonic() - monotonic
    return row


def forwarded(argv):
    values = {"--title", "--audit-shortname", "--audit-summary", "--audit-goal",
              "--audit-done-when", "--artifact", "--join", "--trailer-file"}
    flags = {"--docs-only", "--continue", "--no-push", "--no-build"}
    seen = set()
    branch = None
    index = 0
    while index < len(argv):
        token = argv[index]
        if token in values or token in flags:
            require(token not in seen or token == "--artifact", "OC-DUPLICATE-OPTION")
            seen.add(token)
            if token in values:
                index += 1
                require(index < len(argv) and bool(argv[index]) and
                        not argv[index].startswith("-"), "OC-OPTION-VALUE")
        else:
            require(not token.startswith("-") and branch is None, "OC-UNSUPPORTED-ARGUMENT")
            branch = token
        index += 1
    require({"--title", "--audit-shortname"} <= seen, "OC-REQUIRED-CONDUCTOR-ARGS")
    require((branch is None) == ("--continue" in seen), "OC-CONTINUATION-CONTEXT")


def context(expected, run):
    require(Path.cwd().resolve() == Path(expected.root), "OC-CWD")
    top = run(["git", "rev-parse", "--show-toplevel"])["stdout"].strip()
    common = run(["git", "rev-parse", "--path-format=absolute", "--git-common-dir"])["stdout"].strip()
    head = run(["git", "rev-parse", "HEAD"])["stdout"].strip()
    actual = {"root": str(Path(top).resolve()), "common_dir": str(Path(common).resolve()), "head": head}
    require(actual == {"root": expected.root, "common_dir": expected.common_dir,
                       "head": expected.head}, "OC-GIT-CONTEXT")
    require(actual["common_dir"] != str(Path(expected.root) / ".git"), "OC-NOT-WORKTREE")
    return actual


def check_leader(raw, expected, begin, end, now, elapsed):
    try:
        record = json.loads(raw, parse_constant=lambda _: None)
    except (ValueError, TypeError) as exc:
        raise Refused("OC-LEADER-JSON") from exc
    require(isinstance(record, dict), "OC-LEADER-SHAPE")
    require(record.get("state") == "live", "OC-LEADER-NOT-LIVE")
    require(record.get("leader") == expected.session, "OC-LEADER-HOLDER")
    require(type(record.get("epoch")) is int and record["epoch"] == expected.epoch,
            "OC-LEADER-EPOCH")
    require(record.get("tree") == "worktree", "OC-LEADER-TREE")
    require(isinstance(record.get("oid"), str) and bool(record["oid"]), "OC-LEADER-OID")
    for name in ("expires_at", "expires_in", "pinned_at", "ttl"):
        require(finite(record.get(name)), "OC-LEADER-TIME")
    expiry, remaining = record["expires_at"], record["expires_in"]
    require(begin <= end <= now and record["pinned_at"] <= end and
            0 < record["ttl"] and expiry <= end + record["ttl"] and
            record["pinned_at"] < expiry, "OC-LEADER-TIME")
    sample = expiry - remaining
    require(begin - 0.001 <= sample <= end + 0.001, "OC-LEADER-INCONSISTENT-TIME")
    require(finite(elapsed) and elapsed >= 0 and abs((now - begin) - elapsed) < 1,
            "OC-CLOCK-DISCONTINUITY")
    require(min(expiry - now, remaining - elapsed) >= expected.min_remaining,
            "OC-LEASE-HORIZON")
    return record


def attempt(expected, args, receipt, runner=command, coord=None, conductor=None):
    env = dict(os.environ)
    require(env.get("AGENT_SESSION", expected.session) == expected.session, "OC-ENV-SESSION")
    require(expected.session.strip() == expected.session and bool(expected.session), "OC-SESSION")
    require(expected.epoch > 0, "OC-EPOCH")
    for name in ("min_remaining", "conductor_timeout"):
        require(finite(getattr(expected, name)) and getattr(expected, name) > 0, "OC-POSITIVE-BUDGET")
    for name in ("root", "common_dir"):
        path = Path(getattr(expected, name))
        require(path.is_absolute() and str(path.resolve()) == str(path) and path.is_dir(), "OC-PATH")
    forwarded(args)
    env["AGENT_SESSION"] = expected.session

    def run(argv, timeout=15):
        row = runner(argv, expected.root, env, timeout)
        receipt["commands"].append(row)
        require(row.get("error") is None and row.get("cleanup_error") is None,
                "OC-CHILD-OBSERVATION")
        require(row["exit"] == 0, "OC-CHILD-FAILED")
        return row

    receipt["actual"] = context(expected, run)
    coord = coord or [sys.executable, str(Path(expected.root) / "docs/ai-forward-pack/scripts/coord-core.py")]
    conductor = conductor or [sys.executable, str(Path(expected.root) / "docs/ai-forward-pack/scripts/conductor-join.py")]
    run([*coord, "leader", "renew"])
    read = run([*coord, "leader", "who", "--json"])
    receipt["actual_after_readback"] = context(expected, run)
    require(finite(read.get("monotonic_start")), "OC-READ-TIMING-MISSING")
    receipt["leader"] = check_leader(read["stdout"], expected, read["started_at"], read["ended_at"],
                                    time.time(), time.monotonic() - read["monotonic_start"])
    argv = [*conductor, *args, "--session", expected.session, "--epoch", str(expected.epoch)]
    receipt["conductor"] = {"argv": argv, "started_at": time.time()}
    # Observed preflight only: the Coordinator remains responsible for lease renewal.
    child = runner(argv, expected.root, env, expected.conductor_timeout)
    receipt["commands"].append(child)
    receipt["conductor"] = child
    require(child.get("error") is None and child.get("cleanup_error") is None, "OC-CONDUCTOR-OBSERVATION")
    return child["exit"]


def parser():
    result = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    for name in ("session", "root", "common-dir", "head", "receipt"):
        result.add_argument("--" + name, required=True)
    result.add_argument("--epoch", type=int, required=True)
    result.add_argument("--min-remaining", type=float, required=True)
    result.add_argument("--conductor-timeout", type=float, required=True)
    return result


def encode_event(value):
    """Both intent and outcome obey strict JSON; nonfinite facts never serialize."""
    return json.dumps(value, sort_keys=True, allow_nan=False) + "\n"


def main(argv):
    if argv == ["--self-test"]:
        return self_test()
    try:
        split = argv.index("--")
    except ValueError:
        print("OC-SEPARATOR: conductor arguments must follow --", file=sys.stderr)
        return 2
    prefix = argv[:split]
    # Standard --option=value accepts -inf as a value; normalize before duplicate checks.
    keys = [word.split("=", 1)[0] for word in prefix if word.startswith("--")]
    if len(keys) != len(set(keys)):
        print("OC-DUPLICATE-OPTION", file=sys.stderr)
        return 2
    expected = parser().parse_args(prefix)
    requested = {key: str(value) if isinstance(value, float) and not math.isfinite(value) else value
                 for key, value in vars(expected).items()}
    receipt = {"schema": "owned-conductor/1", "expected": requested, "commands": [],
               "started_at": time.time(), "tokens": "not recorded", "cost": "not recorded"}
    start = time.monotonic()
    try:
        # Exclusive creation makes history append-only at the attempt boundary.
        with Path(expected.receipt).open("x", encoding="utf-8", newline="\n") as output:
            output.write(encode_event(receipt))
            output.flush()
            try:
                code = attempt(expected, argv[split + 1:], receipt)
            except (Refused, OSError, KeyboardInterrupt) as exc:
                receipt["refusal"] = str(exc) or "OC-INTERRUPTED"
                code = 12
            receipt["exit"] = code
            receipt["ended_at"] = time.time()
            receipt["duration_seconds"] = time.monotonic() - start
            # Two append-only events: intent and final observation. No overwrite.
            output.write(encode_event(receipt))
        print(json.dumps({"receipt": expected.receipt, "exit": code,
                          "refusal": receipt.get("refusal")}))
        return code
    except (OSError, ValueError) as exc:
        print("OC-RECEIPT: " + str(exc), file=sys.stderr)
        return 12


def self_test():
    """Real isolated Git worktree, stub authority/child, no shared leader mutation."""
    from copy import deepcopy
    from types import SimpleNamespace
    from unittest.mock import patch

    results = []
    original = Path.cwd()
    with tempfile.TemporaryDirectory(prefix="cfd-owned-join-") as scratch:
        base = Path(scratch).resolve()
        repo, tree = base / "repo", base / "tree"
        repo.mkdir()
        env = dict(os.environ, GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        for argv in (["git", "init", str(repo)],
                     ["git", "-c", "user.name=Fixture", "-c", "user.email=fixture@invalid",
                      "-C", str(repo), "commit", "--allow-empty", "-m", "fixture"],
                     ["git", "-C", str(repo), "worktree", "add", "-b", "fixture", str(tree)]):
            row = command(argv, base, env, 15)
            assert row["exit"] == 0, row
        head = command(["git", "rev-parse", "HEAD"], tree, env, 15)["stdout"].strip()
        expected = SimpleNamespace(root=str(tree), common_dir=str(repo / ".git"), head=head,
                                   session="fixture", epoch=19, min_remaining=30., conductor_timeout=5.)
        args = ["topic", "--title", "fixture", "--audit-shortname", "fixture"]
        os.chdir(tree)
        try:
            def case(name, mutation=None, options=None, change=None, environment=None,
                     fail_command=None, delay=0, expected_code=12, malformed=None):
                calls = []
                exp = deepcopy(expected)
                if change:
                    change(exp)
                def stub(argv, cwd, child_env, timeout):
                    if argv[0] == "git":
                        return command(argv, cwd, child_env, timeout)
                    now = time.time()
                    row = {"argv": argv, "started_at": now, "monotonic_start": time.monotonic(),
                           "ended_at": now, "exit": 0,
                           "stdout": "renewed", "stderr": "", "error": None, "cleanup_error": None}
                    if argv[0] == "conductor":
                        calls.append(argv)
                        row["exit"] = 7
                    elif argv[2] == "who":
                        record = {"leader": "fixture", "epoch": 19, "state": "live", "tree": "worktree",
                                  "oid": "a" * 40, "pinned_at": now - 100, "ttl": 300,
                                  "expires_at": now + 200, "expires_in": 200}
                        if mutation:
                            mutation(record)
                        row["stdout"] = malformed if malformed is not None else json.dumps(record)
                        row["started_at"] -= delay
                        row["ended_at"] -= delay
                    if fail_command and fail_command in argv:
                        row["exit"] = 3
                    return row
                receipt = {"commands": []}
                with patch.dict(os.environ, {"AGENT_SESSION": environment or "fixture"}):
                    try:
                        code = attempt(exp, options if options is not None else args, receipt,
                                       stub, ["coord"], ["conductor"])
                    except Refused:
                        code = 12
                assert code == expected_code, (name, code, receipt)
                assert len(calls) == (1 if expected_code == 7 else 0), (name, calls)
                if calls:
                    assert calls[0][-4:] == ["--session", "fixture", "--epoch", "19"]
                results.append({"case": name, "exit": code, "conductor_calls": len(calls)})

            # This deliberately models the missing live precondition (the initial RED).
            bypass = []
            expired = {"epoch": 19, "state": "expired"}
            if expired["epoch"] == expected.epoch:
                bypass.append("conductor")
            assert len(bypass) == 1
            case("expired-same-epoch", lambda r: r.update(state="expired"))
            for state in ("released", "absent", "not_checked"):
                case(state, lambda r, s=state: r.update(state=s))
            case("wrong-holder", lambda r: r.update(leader="other"))
            for epoch in (18, 20, True, "19"):
                case("wrong-epoch-" + str(epoch), lambda r, e=epoch: r.update(epoch=e))
            for key in ("leader", "epoch", "tree", "oid", "expires_at", "expires_in", "pinned_at", "ttl"):
                case("missing-" + key, lambda r, k=key: r.pop(k))
            for key in ("expires_at", "expires_in", "pinned_at", "ttl"):
                case("nonfinite-" + key, lambda r, k=key: r.update({k: float("nan")}))
            for bad in ("not json", "[]", "null"):
                case("bad-json-" + bad, malformed=bad)
            case("unknown-tree", lambda r: r.update(tree="primary"))
            case("inconsistent-expiry", lambda r: r.update(expires_in=500))
            case("insufficient-lease", lambda r: r.update(expires_at=r["expires_at"] - 190, expires_in=10))
            case("elapsed-readback", delay=250)
            case("renew-failure", fail_command="renew")
            case("who-failure", fail_command="who")
            case("wrong-head", change=lambda e: setattr(e, "head", "0" * 40))
            case("wrong-common", change=lambda e: setattr(e, "common_dir", str(tree)))
            case("wrong-cwd", change=lambda e: setattr(e, "root", str(repo)))
            case("wrong-session-env", environment="other")
            for extra in (["--session", "other"], ["--epoch", "20"], ["--epoch=19"],
                          ["--self-test"], ["--help"], ["--title", "duplicate"], ["--unknown"]):
                case("forwarded-" + extra[0], options=args + extra)
            for value in (0, -1, float("nan"), float("inf")):
                case("horizon-" + str(value), change=lambda e, v=value: setattr(e, "min_remaining", v))
            case("live-exit-preserved", expected_code=7)
            case("continue-current-context", options=["--continue", *args[1:]], expected_code=7)
            case("continue-stale-context", options=["--continue", *args[1:]],
                 change=lambda e: setattr(e, "head", "0" * 40))
            # Real CLI composition: an invalid context must leave a readable refusal receipt.
            path = base / "receipt.jsonl"
            cli = [sys.executable, str(Path(__file__).resolve()), "--session", "fixture", "--epoch", "19",
                   "--root", str(tree), "--common-dir", str(repo / ".git"), "--head", "0" * 40,
                   "--min-remaining", "30", "--conductor-timeout", "5", "--receipt", str(path), "--", *args]
            row = command(cli, tree, dict(env, AGENT_SESSION="fixture"), 15)
            facts = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]
            assert row["exit"] == 12 and facts[-1]["refusal"] == "OC-GIT-CONTEXT"
            assert "conductor" not in facts[-1]
            preserved = path.read_bytes()
            assert command(cli, tree, dict(env, AGENT_SESSION="fixture"), 15)["exit"] == 12
            assert path.read_bytes() == preserved
            results.append({"case": "real-cli-append-only-refusal", "exit": 12, "conductor_calls": 0})
            scripts = tree / "docs/ai-forward-pack/scripts"
            scripts.mkdir(parents=True)
            (scripts / "coord-core.py").write_text(
                "import json,time,sys\nnow=time.time()\n"
                "record=dict(leader='fixture',epoch=19,state='live',tree='worktree',oid='a'*40,"
                "pinned_at=now-100,ttl=300,expires_at=now+200,expires_in=200)\n"
                "print(json.dumps(record) if sys.argv[2]=='who' else 'renewed')\n", encoding="utf-8", newline="\n")
            (scripts / "conductor-join.py").write_text(
                "import sys,os\nfrom pathlib import Path\n"
                "assert sys.argv[-4:]==['--session','fixture','--epoch','19']\n"
                "assert os.environ['AGENT_SESSION']=='fixture'\n"
                "Path('invoked').write_text('once',encoding='utf-8')\nraise SystemExit(7)\n",
                encoding="utf-8", newline="\n")
            positive = list(cli)
            positive[positive.index("--head") + 1] = head
            positive[positive.index("--receipt") + 1] = str(base / "positive.jsonl")
            row = command(positive, tree, dict(env, AGENT_SESSION="fixture"), 15)
            assert row["exit"] == 7 and (tree / "invoked").read_text(encoding="utf-8") == "once", row
            facts = [json.loads(line) for line in (base / "positive.jsonl").read_text(encoding="utf-8").splitlines()]
            assert facts[-1]["conductor"]["exit"] == 7 and facts[-1]["leader"]["state"] == "live"
            results.append({"case": "real-cli-live-exit-preserved", "exit": 7, "conductor_calls": 1})
            (tree / "invoked").unlink()
            (scripts / "coord-core.py").write_text(
                "from pathlib import Path\nPath('leader-invoked').write_text('bad',encoding='utf-8')\nraise SystemExit(99)\n",
                encoding="utf-8", newline="\n")
            for budget in ("min-remaining", "conductor-timeout"):
                for invalid in ("nan", "inf", "-inf"):
                    invalid_path = base / (budget + invalid + ".jsonl")
                    invalid_cli = list(positive)
                    index = invalid_cli.index("--" + budget)
                    invalid_cli[index:index + 2] = ["--" + budget + "=" + invalid]
                    invalid_cli[invalid_cli.index("--receipt") + 1] = str(invalid_path)
                    row = command(invalid_cli, tree, dict(env, AGENT_SESSION="fixture"), 15)
                    assert row["exit"] == 12 and invalid_path.is_file(), (budget, invalid, row)
                    def reject_constant(value):
                        raise AssertionError("nonstandard JSON number: " + value)
                    events = [json.loads(line, parse_constant=reject_constant)
                              for line in invalid_path.read_text(encoding="utf-8").splitlines()]
                    assert len(events) == 2 and events[-1]["refusal"] == "OC-POSITIVE-BUDGET"
                    assert events[-1]["commands"] == [] and "conductor" not in events[-1]
                    assert not (tree / "leader-invoked").exists() and not (tree / "invoked").exists()
                    preserved = invalid_path.read_bytes()
                    assert command(invalid_cli, tree, dict(env, AGENT_SESSION="fixture"), 15)["exit"] == 12
                    assert invalid_path.read_bytes() == preserved
                    results.append({"case": "real-cli-" + budget + "-" + invalid,
                                    "exit": row["exit"], "conductor_calls": 0, "leader_calls": 0})
            for value in (float("nan"), float("inf"), float("-inf")):
                try:
                    encode_event({"injected_nonfinite_fact": value})
                except ValueError:
                    pass
                else:
                    raise AssertionError("strict serializer allowed a nonstandard number")
            duplicate = list(positive)
            duplicate[duplicate.index("--receipt") + 1] = str(base / "duplicate.jsonl")
            duplicate[duplicate.index("--"):duplicate.index("--")] = ["--epoch=20"]
            assert command(duplicate, tree, dict(env, AGENT_SESSION="fixture"), 15)["exit"] == 2
            assert not (base / "duplicate.jsonl").exists() and not (tree / "leader-invoked").exists()
            results.append({"case": "strict-serializer-and-normalized-duplicate", "exit": 2,
                            "conductor_calls": 0, "leader_calls": 0})
            timed = command([sys.executable, "-c", "import time; time.sleep(2)"], tree, env, .05)
            assert timed["error"] == "OC-PROCESS-TIMEOUT" and not timed["cleanup_error"]
            results.append({"case": "owned-timeout", "exit": timed["exit"], "conductor_calls": 0})
        finally:
            os.chdir(original)
    print(json.dumps({"result": "PASS", "cases": results, "count": len(results)}, sort_keys=True))
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    # Convert supported termination to owned cleanup and a final receipt.
    signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(KeyboardInterrupt()))
    raise SystemExit(main(sys.argv[1:]))
