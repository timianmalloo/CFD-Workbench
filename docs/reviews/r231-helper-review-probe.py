"""Independent offline R231 boundary probe; no process launch or live inspection."""
from pathlib import Path
import contextlib
import hashlib
import io
import subprocess
import sys
import tempfile
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests"))
import test_f1_cleanup_owned_check as fixture


def main():
    case = fixture.OwnedCheckTests()
    case.setUp()
    try:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            missing = root / "missing-evidence"
            assert not missing.exists()
            for kind in ("stage", "guard"):
                result = case.invoke_check(missing, kind)
                assert result == "OWNED-ABSENT\n", repr(result)
                print(f"UNKNOWN-EVIDENCE-FAIL-OPEN kind={kind} missing_root=True result=OWNED-ABSENT")
            file_root = root / "not-a-directory"
            file_root.write_text("synthetic", encoding="utf-8")
            result = case.invoke_check(file_root)
            assert result == "OWNED-ABSENT\n", repr(result)
            print("UNKNOWN-EVIDENCE-FAIL-OPEN file_root=True result=OWNED-ABSENT")
            (root / "probe.owner.txt").write_text("pid=invalid\n", encoding="utf-8")
            try:
                case.invoke_check(root)
            except RuntimeError as error:
                assert str(error) == "F1-OWNED-STATE-UNKNOWN"
            else:
                raise AssertionError("malformed owner was accepted")
            print("MALFORMED-OWNER-REFUSAL-PASS")
        for kind in ("stage", "guard"):
            with tempfile.TemporaryDirectory() as temporary:
                args = ["helper", "--evidence-root", temporary, "--expected-run-root", fixture.RUN_ROOT,
                        "--run-id", fixture.RUN_ID, "--owned-control", str(fixture.OWNED), "--kind", kind]
                stderr = io.StringIO()
                with patch.object(sys, "argv", args), contextlib.redirect_stderr(stderr), \
                        patch.object(subprocess, "run", side_effect=OSError("PRIVATE-SYNTHETIC-DETAIL")), \
                        patch.object(subprocess, "Popen", side_effect=AssertionError("launch forbidden")):
                    try:
                        fixture.helper.main()
                    except SystemExit as error:
                        assert error.code == 5
                    else:
                        raise AssertionError("unknown processes accepted")
                assert stderr.getvalue() == "F1-OWNED-STATE-UNKNOWN\n"
                print(f"SANITIZED-CLI-UNKNOWN-PASS kind={kind}")
        for relative in (fixture.REL / "cleanup.py", fixture.REL / "cleanup_owned_check.py",
                         Path("tests/test_f1_cleanup_owned_check.py"), fixture.REL / "payload.json",
                         fixture.REL / "cleanup-authorization.json"):
            print("SHA256", relative.as_posix(), hashlib.sha256((ROOT / relative).read_bytes()).hexdigest())
    finally:
        case.doCleanups()


if __name__ == "__main__":
    main()
