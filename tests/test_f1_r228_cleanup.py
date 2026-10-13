"""Offline-fast R230 cleanup-only controls; no WSL process may launch."""
from __future__ import annotations

import importlib.util
import math
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup.py"
spec = importlib.util.spec_from_file_location("r230_cleanup", SCRIPT)
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)


def row(name, key, path):
    return {"name": name, "key": key, "path": path, "version": 2}


class CleanupOnlyTests(unittest.TestCase):
    def setUp(self):
        self.target = row(cleanup.TARGET, cleanup.TARGET_KEY, cleanup.TARGET_PATH)
        self.protected = row(cleanup.PROTECTED, cleanup.PROTECTED_KEY, cleanup.PROTECTED_PATH)
        self.identity = {"target": cleanup.TARGET, "target_key": cleanup.TARGET_KEY,
                         "target_path": cleanup.TARGET_PATH, "protected": cleanup.PROTECTED,
                         "protected_key": cleanup.PROTECTED_KEY,
                         "protected_path": cleanup.PROTECTED_PATH, "protected_version": 2}

    def test_identity_exact_passes(self):
        observed = cleanup.validate_identity([self.target, self.protected],
                                             [cleanup.TARGET, cleanup.PROTECTED], True, True,
                                             self.identity)
        self.assertEqual(observed, (self.target, self.protected))

    def test_identity_mutations_refuse(self):
        cases = ([self.protected],
                 [self.target, self.target, self.protected],
                 [dict(self.target, key="{wrong}"), self.protected],
                 [dict(self.target, path="C:\\moved"), self.protected],
                 [self.target, dict(self.protected, version=1)])
        for rows in cases:
            with self.subTest(rows=rows), self.assertRaises(ValueError):
                cleanup.validate_identity(rows, [cleanup.TARGET, cleanup.PROTECTED], True, True,
                                          self.identity)
        for names, target_path, protected_path in (([cleanup.PROTECTED], True, True),
                                                   ([cleanup.TARGET, cleanup.PROTECTED], False, True),
                                                   ([cleanup.TARGET, cleanup.PROTECTED], True, False)):
            with self.subTest(names=names), self.assertRaises(ValueError):
                cleanup.validate_identity([self.target, self.protected], names, target_path,
                                          protected_path, self.identity)

    def test_identity_target_path_alias_refuses(self):
        alias = row("renamed-target-alias", "{third}", cleanup.TARGET_PATH)
        with self.assertRaises(ValueError):
            cleanup.validate_identity([self.target, self.protected, alias],
                                      [cleanup.TARGET, cleanup.PROTECTED, "renamed-target-alias"],
                                      True, True, self.identity)

    def test_identity_protected_path_alias_refuses(self):
        alias = row("renamed-protected-alias", "{third}", cleanup.PROTECTED_PATH)
        with self.assertRaises(ValueError):
            cleanup.validate_identity([self.target, self.protected, alias],
                                      [cleanup.TARGET, cleanup.PROTECTED, "renamed-protected-alias"],
                                      True, True, self.identity)

    def test_absence_rejects_every_target_alias_and_protected_change(self):
        cleanup.validate_absence([self.protected], [cleanup.PROTECTED], False, True,
                                 self.protected, self.identity)
        survivors = (dict(self.target, name="renamed", path="C:\\moved"),
                     dict(self.target, key="{other}", name="renamed"),
                     dict(self.target, key="{other}", path="C:\\moved"))
        for survivor in survivors:
            with self.subTest(survivor=survivor), self.assertRaises(ValueError):
                cleanup.validate_absence([self.protected, survivor], [cleanup.PROTECTED], False,
                                         True, self.protected, self.identity)
        with self.assertRaises(ValueError):
            cleanup.validate_absence([dict(self.protected, path="C:\\moved")],
                                     [cleanup.PROTECTED], False, True, self.protected,
                                     self.identity)

    def test_absence_protected_path_alias_refuses(self):
        alias = row("renamed-protected-alias", "{third}", cleanup.PROTECTED_PATH)
        with self.assertRaises(ValueError):
            cleanup.validate_absence([self.protected, alias],
                                     [cleanup.PROTECTED, "renamed-protected-alias"], False, True,
                                     self.protected, self.identity)

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
        instance.identity = self.identity
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
        value = {"schema": "cfdw-f1-r230-cleanup-authorization/1", "status": "accepted", "ruling": 230,
                 "leader": "mac", "leader_epoch": 8, "source_commit": "a" * 40,
                 "payload_sha256": "b" * 64, "session": "pc-f1-r230-cleanup1",
                 "branch": "win/gpu-f1-r228-cleanup",
                 "checkpoint_commit": cleanup.CHECKPOINT_COMMIT,
                 "checkpoint_ancestors": cleanup.CHECKPOINT_ANCESTORS,
                 "original_run_id": cleanup.RUN_ID, "original_run_root": cleanup.ORIGINAL_RUN_ROOT,
                 "target": cleanup.TARGET, "target_key": cleanup.TARGET_KEY, "target_path": cleanup.TARGET_PATH,
                 "protected": cleanup.PROTECTED, "protected_key": cleanup.PROTECTED_KEY,
                 "protected_path": cleanup.PROTECTED_PATH, "protected_version": 2,
                 "allowed_actions": ["precheck", "owned-process-refusal", "terminate-target", "unregister-target", "prove-absence", "freeze-and-commit-evidence"],
                 "forbidden_actions": ["resume-preflight", "download", "install", "source-fetch", "build", "run-case", "benchmark", "manual-host-path-delete"],
                 "deadline_seconds": 600, "exclusive_run_id": "pc-f1-r230-cleanup1",
                 "pc_exclusive_required": True, "attempt": 1, "one_attempt_only": True,
                 "failure_policy": "commit-receipt-stop-no-retry",
                 "retry_requires_new_ruling": True}
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
        section = "\n".join(["### Ruling 230 — ACCEPT ONE CLEANUP-ONLY ACTION", auth, payload, source,
                              cleanup.TARGET_KEY, cleanup.TARGET_PATH, cleanup.PROTECTED_KEY,
                              cleanup.PROTECTED_PATH, "never resume preflight", "never download",
                              "never install", "never fetch source", "never build", "never run a case",
                              "never benchmark", "never delete host paths manually", "R223(a) same commit",
                              "R223(b) two leader samples at least 200 seconds", "R223(c) PC exclusive",
                              "R223(d) one attempt; no second run without a new ruling"])
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
            (repo / cleanup.PAYLOAD_REL).write_text('{}\n', encoding="utf-8")
            instance = cleanup.Cleanup(repo, auth.resolve(), "pc-f1-r230-cleanup1", 8,
                                       "pc-f1-r230-cleanup1")
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

    def test_precheck_verifies_complete_r228_failure_manifest(self):
        source = SCRIPT.read_text(encoding="utf-8")
        self.assertIn('"verify", "--repo", str(self.repo)', source)
        self.assertIn('"--frozen", str(self.repo / FAILURE_REL)', source)
        self.assertIn('"--commit", value["source_commit"]', source)

    def test_authorization_identity_drives_exact_lifecycle_argv(self):
        instance = object.__new__(cleanup.Cleanup)
        instance.identity = dict(self.identity, target="authorized-target")
        instance.run_command = Mock(return_value=b"")
        instance.action("terminate")
        instance.action("unregister")
        self.assertEqual(instance.run_command.call_args_list[0].args[0],
                         ["wsl.exe", "--terminate", "authorized-target"])
        self.assertEqual(instance.run_command.call_args_list[1].args[0],
                         ["wsl.exe", "--unregister", "authorized-target"])

    def test_two_leader_samples_require_exact_live_epoch_and_200_seconds(self):
        valid = {"leader": "mac", "state": "live", "epoch": 8, "expires_in": 200.0}
        cleanup.validate_leader_sample(valid, 8)
        for field, replacement in (("leader", "pc"), ("state", "expired"),
                                   ("epoch", 9), ("expires_in", 199.999)):
            sample = dict(valid)
            sample[field] = replacement
            with self.subTest(field=field), self.assertRaises(ValueError):
                cleanup.validate_leader_sample(sample, 8)
        source = SCRIPT.read_text(encoding="utf-8")
        self.assertIn("leader-activation.json", source)
        self.assertIn("leader-immediately-pre-live.json", source)

    def test_leader_sample_rejects_non_finite_ttl(self):
        valid = {"leader": "mac", "state": "live", "epoch": 8, "expires_in": 200.0}
        for ttl in (math.nan, math.inf, -math.inf):
            sample = dict(valid, expires_in=ttl)
            with self.subTest(ttl=ttl), self.assertRaises(ValueError):
                cleanup.validate_leader_sample(sample, 8)

    def test_pre_live_leader_refusal_binds_receipt_before_stopping(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(cleanup.Cleanup)
            instance.precheck = Mock()
            instance.live = Path(temporary) / "live"
            instance.publish_precheck = Mock()
            instance.capture_pre_live_leader = Mock(side_effect=ValueError("ttl below floor"))
            instance.action = Mock()
            instance.bind = Mock(return_value="d" * 40)
            instance.failure = None
            with self.assertRaisesRegex(ValueError, "committed receipt; no retry"):
                instance.execute()
            instance.action.assert_not_called()
            instance.bind.assert_called_once_with("failed-stop-no-retry")
            self.assertEqual(instance.failure["type"], "ValueError")

    def test_r223_conditions_are_fail_closed(self):
        source = SCRIPT.read_text(encoding="utf-8")
        for token in ("R223(a)", "R223(c)", "one_attempt_only",
                      "retry_requires_new_ruling", "exclusive_run_id"):
            self.assertIn(token, source)
        self.assertEqual(cleanup.AUTH_REL.name, "cleanup-authorization.json")


if __name__ == "__main__":
    unittest.main()
