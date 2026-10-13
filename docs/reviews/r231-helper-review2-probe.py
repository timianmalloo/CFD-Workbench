"""Offline final-cycle CLI proof; synthetic filesystem failures, zero launches."""
import contextlib
import hashlib
import io
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests"))
import test_f1_cleanup_owned_check as fixture


def main():
    with tempfile.TemporaryDirectory() as temporary:
        base = Path(temporary)
        file_root = base / "file-root"
        file_root.write_text("synthetic", encoding="utf-8")
        for state, evidence in (("missing", base / "missing"), ("file", file_root), ("unreadable", base)):
            for kind in ("stage", "guard"):
                sys.modules.pop(fixture.MODULE_NAME, None)
                args = ["helper", "--evidence-root", str(evidence), "--expected-run-root", fixture.RUN_ROOT,
                        "--run-id", fixture.RUN_ID, "--owned-control", str(fixture.OWNED), "--kind", kind]
                stdout, stderr = io.StringIO(), io.StringIO()
                with contextlib.ExitStack() as stack:
                    stack.enter_context(patch.object(sys, "argv", args))
                    stack.enter_context(contextlib.redirect_stdout(stdout))
                    stack.enter_context(contextlib.redirect_stderr(stderr))
                    launch = stack.enter_context(patch.object(subprocess, "run", side_effect=AssertionError("process forbidden")))
                    child = stack.enter_context(patch.object(subprocess, "Popen", side_effect=AssertionError("launch forbidden")))
                    signal = stack.enter_context(patch.object(os, "kill", side_effect=AssertionError("signal forbidden")))
                    if state == "unreadable":
                        stack.enter_context(patch.object(Path, "iterdir", side_effect=PermissionError("PRIVATE-SYNTHETIC-DETAIL")))
                    try:
                        fixture.helper.main()
                    except SystemExit as error:
                        assert error.code == 5
                    else:
                        raise AssertionError("invalid evidence accepted")
                    assert stdout.getvalue() == ""
                    assert stderr.getvalue() == "F1-OWNED-STATE-UNKNOWN\n"
                    launch.assert_not_called()
                    child.assert_not_called()
                    signal.assert_not_called()
                print(f"ROOT-CLI-REFUSAL-PASS state={state} kind={kind} process_calls=0 stdout_bytes=0")
    for relative in (fixture.REL / "cleanup.py", fixture.REL / "cleanup_owned_check.py",
                     Path("tests/test_f1_cleanup_owned_check.py"), fixture.REL / "payload.json",
                     fixture.REL / "cleanup-authorization.json"):
        print("SHA256", relative.as_posix(), hashlib.sha256((ROOT / relative).read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
