"""Offline-fast helper import/ownership contract; process data are synthetic.

Discovered by tools/run-tests.sh. Cost is measured in the repair proof; no WSL,
host process inspection, signal, registry, cleanup, or live evidence access.
"""
from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

ROOT = Path(__file__).resolve().parents[1]
REL = Path("docs/proof/win-gpu-f1-preflight/attempt-1/cleanup")
OWNED = ROOT / "docs/proof/win-gpu-f1-preflight/packet/owned_control.py"
RUN_ROOT = "/synthetic/preflight/attempt-1/live"
RUN_ID = "synthetic-attempt"
MODULE_NAME = "r229_owned_control"


def load_source(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


helper = load_source("cleanup_owned_check_test", ROOT / REL / "cleanup_owned_check.py")
cleanup = load_source("cleanup_diagnostic_test", ROOT / REL / "cleanup.py")


def write_subprocess_owned_fixture(path, drop_registration=False):
    """Load real packet definitions; replace only live process/signal boundaries."""
    prefix = "import sys\nfrom pathlib import Path\n"
    if drop_registration:
        prefix += "sys.modules.pop(__name__, None)\n"
    source = prefix + (
        f"exec(compile(Path({str(OWNED)!r}).read_bytes(), {str(OWNED)!r}, 'exec'), globals())\n"
        "def forbidden_process(*args, **kwargs):\n"
        "    raise AssertionError('offline fixture forbids process or signal actions')\n"
        "subprocess.run = forbidden_process\n"
        "send_group_signal = forbidden_process\n"
        "def read_processes():\n"
        "    return []\n")
    path.write_text(source, encoding="utf-8", newline="\n")


class OwnedCheckTests(unittest.TestCase):
    def setUp(self):
        # Each test starts without a registry entry that could hide the defect.
        registry = patch.dict(sys.modules)
        registry.start()
        self.addCleanup(registry.stop)
        sys.modules.pop(MODULE_NAME, None)

    def test_LoadOwnedControl_RealDataclass_LoadsWithoutProcessCalls(self):
        with patch.object(subprocess, "run", side_effect=AssertionError("process forbidden")) as launch:
            module = helper.load_owned_control(OWNED)
        self.assertEqual(module.ProcessRow(17, 17, 17, ("synthetic",)).pid, 17)
        self.assertIs(sys.modules[module.ProcessRow.__module__], module)
        launch.assert_not_called()

    def invoke_check(self, root, kind="stage", argv=None, unavailable=False):
        """Inject only ps/proc boundaries; real loader/parser/matcher still run.

        The ps field and NUL-delimited argv contracts are paired with the real
        read_processes injection seam in test_ReadProcesses_Fixture_ParsesRows.
        """
        process_argv = argv or ()
        original_read_bytes = Path.read_bytes

        def read_bytes(path):
            if path.as_posix() == "/proc/17/cmdline":
                return b"\0".join(item.encode() for item in process_argv) + b"\0"
            return original_read_bytes(path)

        def run(command, **kwargs):
            self.assertEqual(command, ["ps", "-e", "-o", "pid=,pgid=,sid="])
            if unavailable:
                raise OSError("synthetic private process diagnostic")
            return subprocess.CompletedProcess(command, 0, "17 17 17\n" if argv else "", "")

        output = io.StringIO()
        with patch.object(subprocess, "run", side_effect=run), \
                patch.object(subprocess, "Popen", side_effect=AssertionError("launch forbidden")), \
                patch("os.kill", side_effect=AssertionError("signal forbidden")), \
                patch("os.killpg", side_effect=AssertionError("group signal forbidden"), create=True), \
                patch.object(Path, "read_bytes", read_bytes), contextlib.redirect_stdout(output):
            helper.check(root, RUN_ROOT, RUN_ID, OWNED, kind)
        return output.getvalue()

    def test_ReadProcesses_Fixture_ParsesRows(self):
        module = helper.load_owned_control(OWNED)
        rows = module.read_processes(
            run=lambda: subprocess.CompletedProcess(["ps"], 0, "17 17 17\n", ""),
            read_argv=lambda pid: b"/foreign/process\0")
        self.assertEqual(rows, [module.ProcessRow(17, 17, 17, ("/foreign/process",))])

    def test_Check_EmptyStageAndGuard_ProvesAbsence(self):
        with tempfile.TemporaryDirectory() as temporary:
            for kind in ("stage", "guard"):
                with self.subTest(kind=kind):
                    self.assertEqual(self.invoke_check(Path(temporary), kind), "OWNED-ABSENT\n")

    def test_Check_ForeignProcess_ProvesAttemptAbsence(self):
        with tempfile.TemporaryDirectory() as temporary:
            self.assertEqual(self.invoke_check(Path(temporary), argv=("/foreign/process",)), "OWNED-ABSENT\n")

    def assert_unknown_root(self, root):
        for kind in ("stage", "guard"):
            output = io.StringIO()
            empty_ps = subprocess.CompletedProcess(["ps"], 0, "", "")
            with self.subTest(kind=kind):
                with contextlib.redirect_stdout(output), \
                        patch.object(subprocess, "run", return_value=empty_ps) as launch, \
                        self.assertRaisesRegex(RuntimeError, "^F1-OWNED-STATE-UNKNOWN$"):
                    helper.check(root, RUN_ROOT, RUN_ID, OWNED, kind)
                self.assertEqual(output.getvalue(), "")
                launch.assert_not_called()

    def test_Check_MissingEvidenceRoot_RefusesBeforeProcessInspection(self):
        with tempfile.TemporaryDirectory() as temporary:
            self.assert_unknown_root(Path(temporary) / "missing-evidence")

    def test_Check_FileInsteadOfEvidenceDirectory_RefusesBeforeProcessInspection(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary) / "evidence"
            root.write_text("synthetic substituted file", encoding="utf-8")
            self.assert_unknown_root(root)

    def test_Check_UnreadableEvidenceDirectory_RefusesBeforeProcessInspection(self):
        # Windows ACLs and privileged POSIX callers make chmod nondeterministic.
        # Real missing/file roots pair this injected filesystem failure boundary.
        with tempfile.TemporaryDirectory() as temporary, \
                patch.object(Path, "iterdir", side_effect=PermissionError("synthetic private evidence detail")):
            self.assert_unknown_root(Path(temporary))

    def test_Check_OwnedStageAndGuard_RefusesWithResidualCode(self):
        stage = ("/bin/bash", "/synthetic/preflight/packet/preflight.sh", "--stage", "probe",
                 "--run-root", RUN_ROOT, "--run-id", RUN_ID)
        guard = ("/usr/bin/python3", "/synthetic/preflight/packet/completed_guard.py", "watch",
                 "--session-id", RUN_ID, "--events", RUN_ROOT + "/completed-guard-events.jsonl")
        with tempfile.TemporaryDirectory() as temporary:
            for kind, argv in (("stage", stage), ("guard", guard)):
                with self.subTest(kind=kind), self.assertRaisesRegex(RuntimeError, "^F1-OWNED-RESIDUAL$"):
                    self.invoke_check(Path(temporary), kind, argv)

    def test_Check_ReusedOwner_RefusesUnknownState(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "probe.owner.txt").write_text("pid=17\npgid=17\nsid=17\n", encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "^F1-OWNED-STATE-UNKNOWN$"):
                self.invoke_check(root, argv=("/foreign/process",))

    def test_Check_UnavailableProcesses_RefusesUnknownState(self):
        with tempfile.TemporaryDirectory() as temporary, \
                self.assertRaisesRegex(RuntimeError, "^F1-OWNED-STATE-UNKNOWN$"):
            self.invoke_check(Path(temporary), unavailable=True)

    def test_Check_UnresolvedIntent_RefusesUnknownState(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "probe.intent.json").write_text("{}", encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "^F1-OWNED-STATE-UNKNOWN$"):
                self.invoke_check(root)

    def test_Main_ImportFailure_EmitsOnlyStableCode(self):
        args = ["helper", "--evidence-root", "/synthetic", "--expected-run-root", RUN_ROOT,
                "--run-id", RUN_ID, "--owned-control", "/missing/private/source.py", "--kind", "stage"]
        output, error_output = io.StringIO(), io.StringIO()
        with patch.object(sys, "argv", args), contextlib.redirect_stdout(output), \
                contextlib.redirect_stderr(error_output), \
                patch.object(subprocess, "run", side_effect=AssertionError("process forbidden")) as launch, \
                self.assertRaises(SystemExit) as refusal:
            helper.main()
        self.assertEqual(refusal.exception.code, 5)
        self.assertEqual(output.getvalue(), "")
        self.assertEqual(error_output.getvalue(), "F1-OWNED-IMPORT-FAILED\n")
        launch.assert_not_called()

    def test_Cli_UnregisteredRealDataclass_RefusesImportWithoutProcessCalls(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            owned = root / "owned_fixture.py"
            write_subprocess_owned_fixture(owned, drop_registration=True)
            command = [sys.executable, str(ROOT / REL / "cleanup_owned_check.py"),
                       "--evidence-root", str(root), "--expected-run-root", RUN_ROOT,
                       "--run-id", RUN_ID, "--owned-control", str(owned), "--kind", "stage"]
            result = subprocess.run(command, capture_output=True, timeout=10, check=False)
        self.assertEqual(result.returncode, 5)
        self.assertEqual(result.stdout, b"")
        self.assertEqual(result.stderr.splitlines(), [b"F1-OWNED-IMPORT-FAILED"])

    # cleanup.py's linux_path accepts only a Windows (C:\) repo root, so this case can run only on the PC (Fable join
    # condition 1, 2026-10-13). Skipped, and so reported, elsewhere; the PC ring still runs it.
    @unittest.skipUnless(sys.platform == "win32", "needs a Windows repo root (cleanup.py linux_path); runs on the PC ring")
    def test_Execute_HelperDiagnostic_BindsSanitizedCodeAndStops(self):
        for diagnostic in (b"F1-OWNED-IMPORT-FAILED", b"F1-OWNED-RESIDUAL", b"F1-OWNED-STATE-UNKNOWN",
                           b"private argv: /synthetic/private", b"F1-OWNED-RESIDUAL\nprivate detail"):
            with self.subTest(diagnostic=diagnostic), tempfile.TemporaryDirectory() as temporary:
                repo = Path(temporary)
                instance = cleanup.Cleanup(repo, repo / "synthetic-auth.json", "synthetic-session", 8, "synthetic-run")
                instance.precheck = Mock()
                instance.publish_precheck = Mock()
                instance.capture_pre_live_leader = Mock()
                instance.stable_identity = Mock(return_value=({"key": cleanup.TARGET_KEY}, {}))
                instance.action = Mock(wraps=instance.action)
                instance.bind = Mock(return_value="a" * 40)
                # Synthetic receipt inputs bypass admission only in this fixture.
                instance.authorization.write_bytes(b"synthetic fixture, no authority")
                instance.authorization_value = {"ruling": 230, "source_commit": "a" * 40}
                instance.identity = {"target": "synthetic-target", "protected": "synthetic-protected",
                                     "target_key": cleanup.TARGET_KEY}
                instance.payload_sha = "b" * 64
                instance.original_run_id = RUN_ID
                instance.original_run_root = r"C:\synthetic\preflight\attempt-1\live"
                instance.activation_sample = {}
                result = subprocess.CompletedProcess(["synthetic-child"], 5, b"", diagnostic + b"\n")
                with patch.object(subprocess, "run", return_value=result) as launch, \
                        self.assertRaisesRegex(ValueError, "committed receipt; no retry"):
                    instance.execute()
                expected = diagnostic.decode() if diagnostic in (
                    b"F1-OWNED-IMPORT-FAILED", b"F1-OWNED-RESIDUAL", b"F1-OWNED-STATE-UNKNOWN") else "F1-OWNED-CHECK-FAILED"
                self.assertEqual(instance.failure["diagnostic_code"], expected)
                self.assertEqual(instance.log[-1]["diagnostic_code"], expected)
                self.assertEqual([call.args[0] for call in instance.action.call_args_list],
                                 ["precheck-identity", "owned-stage-refusal"])
                instance.bind.assert_called_once_with("failed-stop-no-retry")
                self.assertEqual(instance.receipt("failed-stop-no-retry")["failure"]["diagnostic_code"], expected)
                launch.assert_called_once()
                self.assertEqual(launch.call_args.args[0][:3],
                                 ["wsl.exe", "--distribution", "synthetic-target"])
                captures = [json.loads(path.read_bytes()) for path in instance.live.glob("*.json")]
                self.assertEqual(len(captures), 2)
                self.assertTrue(all(capture["raw"] == "Not published" for capture in captures))
                self.assertNotIn("private", json.dumps(instance.failure))


if __name__ == "__main__":
    unittest.main()
