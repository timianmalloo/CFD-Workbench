"""Offline-fast R229 cleanup-only controls; no WSL process may launch."""
from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup.py"
spec = importlib.util.spec_from_file_location("r229_cleanup", SCRIPT)
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)


def row(name, key, path):
    return {"name": name, "key": key, "path": path, "version": 2}


class CleanupOnlyTests(unittest.TestCase):
    def setUp(self):
        self.target = row(cleanup.TARGET, cleanup.TARGET_KEY, cleanup.TARGET_PATH)
        self.protected = row(cleanup.PROTECTED, cleanup.PROTECTED_KEY, cleanup.PROTECTED_PATH)

    def test_identity_exact_passes(self):
        observed = cleanup.validate_identity([self.target, self.protected],
                                             [cleanup.TARGET, cleanup.PROTECTED], True, True)
        self.assertEqual(observed, (self.target, self.protected))

    def test_identity_mutations_refuse(self):
        cases = ([self.protected],
                 [self.target, self.target, self.protected],
                 [dict(self.target, key="{wrong}"), self.protected],
                 [dict(self.target, path="C:\\moved"), self.protected],
                 [self.target, dict(self.protected, version=1)])
        for rows in cases:
            with self.subTest(rows=rows), self.assertRaises(ValueError):
                cleanup.validate_identity(rows, [cleanup.TARGET, cleanup.PROTECTED], True, True)
        for names, target_path, protected_path in (([cleanup.PROTECTED], True, True),
                                                   ([cleanup.TARGET, cleanup.PROTECTED], False, True),
                                                   ([cleanup.TARGET, cleanup.PROTECTED], True, False)):
            with self.subTest(names=names), self.assertRaises(ValueError):
                cleanup.validate_identity([self.target, self.protected], names, target_path, protected_path)

    def test_absence_rejects_every_target_alias_and_protected_change(self):
        cleanup.validate_absence([self.protected], [cleanup.PROTECTED], False, True, self.protected)
        survivors = (dict(self.target, name="renamed", path="C:\\moved"),
                     dict(self.target, key="{other}", name="renamed"),
                     dict(self.target, key="{other}", path="C:\\moved"))
        for survivor in survivors:
            with self.subTest(survivor=survivor), self.assertRaises(ValueError):
                cleanup.validate_absence([self.protected, survivor], [cleanup.PROTECTED], False, True, self.protected)
        with self.assertRaises(ValueError):
            cleanup.validate_absence([dict(self.protected, path="C:\\moved")], [cleanup.PROTECTED], False, True, self.protected)

    def test_sequence_stops_at_first_failure(self):
        expected = ["precheck-identity", "owned-stage-refusal", "owned-guard-refusal",
                    "stable-before-terminate", "terminate", "stable-before-unregister",
                    "unregister", "stable-absence", "protected-invariance"]
        calls = []
        cleanup.cleanup_sequence(calls.append)
        self.assertEqual(calls, expected)
        for failed in expected:
            calls = []
            def action(name):
                calls.append(name)
                if name == failed:
                    raise ValueError("planted")
            with self.subTest(failed=failed), self.assertRaises(ValueError):
                cleanup.cleanup_sequence(action)
            self.assertEqual(calls[-1], failed)
            self.assertEqual(calls, expected[:expected.index(failed) + 1])

    def test_lifecycle_argv_is_exact_and_no_manual_delete_exists(self):
        instance = object.__new__(cleanup.Cleanup)
        instance.run_command = Mock(return_value=b"")
        instance.stable_identity = Mock(return_value=(self.target, self.protected))
        instance.stable_absence = Mock()
        instance.protected_before = self.protected
        instance.observe = Mock(return_value=([self.protected], [cleanup.PROTECTED], False, True))
        instance.write = Mock()
        instance.action("terminate")
        instance.action("unregister")
        self.assertEqual(instance.run_command.call_args_list[0].args[0], ["wsl.exe", "--terminate", cleanup.TARGET])
        self.assertEqual(instance.run_command.call_args_list[1].args[0], ["wsl.exe", "--unregister", cleanup.TARGET])
        source = SCRIPT.read_text(encoding="utf-8")
        for forbidden in ("rmtree(", "unlink(", "apt-get", "dpkg", "git clone", "cmake", "mpirun"):
            self.assertNotIn(forbidden, source)

    def test_authorization_requires_exact_accepted_contract(self):
        value = {"schema": "cfdw-f1-r229-cleanup-authorization/1", "status": "accepted", "ruling": 229,
                 "leader": "mac", "leader_epoch": 8, "source_commit": "a" * 40,
                 "payload_sha256": "b" * 64, "session": "pc-f1-r229-cleanup1",
                 "branch": "win/gpu-f1-r228-cleanup", "original_run_id": cleanup.RUN_ID,
                 "target": cleanup.TARGET, "target_key": cleanup.TARGET_KEY, "target_path": cleanup.TARGET_PATH,
                 "protected": cleanup.PROTECTED, "protected_key": cleanup.PROTECTED_KEY,
                 "protected_path": cleanup.PROTECTED_PATH, "protected_version": 2,
                 "allowed_actions": ["precheck", "owned-process-refusal", "terminate-target", "unregister-target", "prove-absence", "freeze-and-commit-evidence"],
                 "forbidden_actions": ["resume-preflight", "download", "install", "source-fetch", "build", "run-case", "benchmark", "manual-host-path-delete"],
                 "deadline_seconds": 600}
        cleanup.validate_authorization(value, "b" * 64)
        for field, replacement in (("status", "proposed"), ("ruling", None), ("target_key", "{wrong}"),
                                   ("payload_sha256", "c" * 64), ("allowed_actions", []),
                                   ("forbidden_actions", []), ("deadline_seconds", 601)):
            changed = dict(value)
            changed[field] = replacement
            with self.subTest(field=field), self.assertRaises(ValueError):
                cleanup.validate_authorization(changed, "b" * 64)

    def test_ruling_requires_all_exact_tokens(self):
        auth, payload, source = "a" * 64, "b" * 64, "c" * 40
        section = "\n".join(["### Ruling 229 — ACCEPT ONE CLEANUP-ONLY ACTION", auth, payload, source,
                              cleanup.TARGET_KEY, cleanup.TARGET_PATH, cleanup.PROTECTED_KEY,
                              cleanup.PROTECTED_PATH, "never resume preflight", "never download",
                              "never install", "never fetch source", "never build", "never run a case",
                              "never benchmark", "never delete host paths manually"])
        cleanup.verify_ruling(section, auth, payload, source)
        for token in (auth, cleanup.TARGET_KEY, "never benchmark"):
            with self.subTest(token=token), self.assertRaises(ValueError):
                cleanup.verify_ruling(section.replace(token, "missing", 1), auth, payload, source)
        with self.assertRaises(ValueError):
            cleanup.verify_ruling(section + "\nPROPOSED", auth, payload, source)

    def test_precheck_refusal_does_not_launch_wsl(self):
        with tempfile.TemporaryDirectory() as temporary:
            repo = Path(temporary)
            (repo / cleanup.REL).mkdir(parents=True)
            auth = repo / cleanup.AUTH_REL
            auth.write_text('{"status":"proposed"}\n', encoding="utf-8")
            (repo / cleanup.PROJECTION_REL).write_text('{}\n', encoding="utf-8")
            (repo / cleanup.PAYLOAD_REL).write_text('{}\n', encoding="utf-8")
            instance = cleanup.Cleanup(repo, auth.resolve(), "pc-f1-r229-cleanup1", 8)
            with patch.object(cleanup.subprocess, "run") as launch, self.assertRaises(ValueError):
                instance.precheck()
            launch.assert_not_called()

    def test_owned_checker_is_read_only(self):
        helper = SCRIPT.with_name("cleanup_owned_check.py").read_text(encoding="utf-8")
        self.assertNotIn("killpg", helper)
        self.assertNotIn("SIGTERM", helper)
        self.assertNotIn("SIGKILL", helper)
        self.assertIn("OWNED-ABSENT", helper)

    def test_historical_evidence_is_not_under_cleanup_source(self):
        for path in (ROOT / cleanup.FAILURE_REL, ROOT / "docs/proof/win-gpu-f1-preflight/recovery-1",
                     ROOT / "docs/proof/win-gpu-f1-preflight/run-1"):
            self.assertFalse(path.is_relative_to(ROOT / cleanup.REL))


if __name__ == "__main__":
    unittest.main()
