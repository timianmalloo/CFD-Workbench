"""Offline-fast recovery controls; no WSL or recovery execution (~1 s target)."""
import importlib.util
import tempfile
import unittest
import contextlib
import io
from unittest.mock import Mock, patch
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / 'docs/proof/win-gpu-f1-preflight/recovery/recover.py'
spec = importlib.util.spec_from_file_location('recover', SCRIPT)
recovery = importlib.util.module_from_spec(spec)
spec.loader.exec_module(recovery)


class RecoveryTests(unittest.TestCase):
    def test_Authorization_AbsentOrProposed_Refuses(self):
        for value in ({}, {'status': 'proposed'}):
            with self.subTest(value=value), self.assertRaises(ValueError):
                recovery.validate_authorization(value, 'abc', 's', 7)

    def test_Authorization_ExactAcceptedContract_Passes(self):
        value = recovery.authorization_example('abc', 's', 7)
        recovery.validate_authorization(value, 'abc', 's', 7)

    def test_Authorization_MutatedBinding_Refuses(self):
        for field in ('source_commit', 'session', 'leader_epoch', 'freeze_sha256',
                      'target', 'target_path', 'protected', 'original_run_id', 'ruling'):
            value = recovery.authorization_example('abc', 's', 7)
            value[field] = 'wrong'
            with self.subTest(field=field), self.assertRaises(ValueError):
                recovery.validate_authorization(value, 'abc', 's', 7)

    def test_Identity_MissingDuplicateWrongPath_Refuses(self):
        target = {'name': recovery.TARGET, 'key': '{target}', 'path': recovery.TARGET_PATH, 'version': 2}
        protected = {'name': recovery.PROTECTED, 'key': '{protected}', 'path': recovery.PROTECTED_PATH, 'version': 2}
        for rows in ([protected], [target, target, protected],
                     [dict(target, path=recovery.PROTECTED_PATH), protected],
                     [target, dict(protected, path=recovery.TARGET_PATH)]):
            with self.subTest(rows=rows), self.assertRaises(ValueError):
                recovery.validate_identity(rows, [recovery.TARGET, recovery.PROTECTED])

    def test_Identity_ExactRegistrations_Passes(self):
        rows = [{'name': recovery.TARGET, 'key': 'a', 'path': recovery.TARGET_PATH, 'version': 2},
                {'name': recovery.PROTECTED, 'key': 'b', 'path': recovery.PROTECTED_PATH, 'version': 2}]
        self.assertEqual(recovery.validate_identity(rows, [recovery.TARGET, recovery.PROTECTED])[0]['key'], 'a')

    def test_Freeze_ChangedManifestOrLinkedInput_Refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'capture-manifest.json').write_text('{}')
            with self.assertRaises(ValueError):
                recovery.validate_freeze(root)

    def test_Plan_CheckpointFailure_NoDestructiveCall(self):
        calls = []
        def action(name):
            calls.append(name)
            if name == 'verify-checkpoint':
                raise ValueError('unbound')
        with self.assertRaises(ValueError):
            recovery.cleanup_sequence(action)
        self.assertEqual(calls, ['verify-checkpoint'])

    def test_Plan_DeletionFailure_FinalReapNoUnregister(self):
        calls = []
        def action(name):
            calls.append(name)
            if name == 'delete-debs':
                raise ValueError('failed')
        with self.assertRaises(ValueError):
            recovery.cleanup_sequence(action)
        self.assertEqual(calls[-1], 'final-owned-check')
        self.assertNotIn('unregister', calls)

    def test_Plan_FinalReapFailure_NoUnregister(self):
        calls = []
        def action(name):
            calls.append(name)
            if name == 'final-owned-check':
                raise ValueError('unknown')
        with self.assertRaises(ValueError):
            recovery.cleanup_sequence(action)
        self.assertNotIn('unregister', calls)

    def test_Plan_GuardOrIdentityFailure_NoDeletion(self):
        for failed in ('fresh-identity', 'old-owned-check', 'guard-ready'):
            calls = []
            def action(name):
                calls.append(name)
                if name == failed:
                    raise ValueError('blocked')
            with self.subTest(failed=failed), self.assertRaises(ValueError):
                recovery.cleanup_sequence(action)
            self.assertNotIn('delete-debs', calls)

    def test_Absence_AnyResidualOrProtectedChange_Refuses(self):
        protected = {'name': recovery.PROTECTED, 'key': 'protected'}
        for rows, names, exists in (([protected, {'name': recovery.TARGET}], [recovery.PROTECTED], False),
                                    ([protected], [recovery.PROTECTED, recovery.TARGET], False),
                                    ([protected], [recovery.PROTECTED], True),
                                    ([dict(protected, key='changed')], [recovery.PROTECTED], False)):
            with self.subTest(rows=rows, names=names, exists=exists), self.assertRaises(ValueError):
                recovery.validate_absence(rows, names, exists, protected)
        recovery.validate_absence([protected], [recovery.PROTECTED], False, protected)

    def test_Lifecycle_ActualAction_OnlyExactTargetArgv(self):
        instance = object.__new__(recovery.Recovery)
        instance.command = Mock()
        for action in ('terminate', 'unregister'):
            instance.action(action)
            self.assertEqual(instance.command.call_args.args[0], ['wsl.exe', '--' + action, recovery.TARGET])
            self.assertTrue(instance.command.call_args.args[3])

    def test_Deletion_ActualAction_OnlyReviewedStageAndCache(self):
        instance = object.__new__(recovery.Recovery)
        instance.repo = ROOT
        instance.packet = ROOT / recovery.REL / 'packet'
        instance.live = ROOT / recovery.REL / 'recovery-1/live'
        instance.linux = Mock()
        instance.linux_path = lambda path: '/offline/' + path.as_posix()
        instance.delete_debs()
        self.assertEqual(instance.linux.call_count, 2)
        admission, deletion = instance.linux.call_args_list
        self.assertEqual(admission.args[0], recovery.TARGET)
        self.assertTrue(admission.args[-1])
        argv = deletion.args[2]
        self.assertEqual(argv[argv.index('--stage') + 1], 'delete-debs')
        self.assertEqual(argv[argv.index('--run-id') + 1], recovery.RUN_ID)
        self.assertEqual(deletion.args[0], recovery.TARGET)
        self.assertTrue(deletion.args[-1])

    def test_Checkpoint_ActualCanonicalFreeze_Validates75Files(self):
        recovery.validate_freeze(ROOT / recovery.REL / 'run-1/checkpoint')

    def test_Cache_LinkMountUnexpectedEntry_Refuses(self):
        cache_spec = importlib.util.spec_from_file_location('cache', SCRIPT.parent / 'check_cache.py')
        cache = importlib.util.module_from_spec(cache_spec)
        cache_spec.loader.exec_module(cache)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            store = root / 'cache'
            store.mkdir()
            mounts = root / 'mountinfo'
            mounts.write_text('')
            (store / 'unexpected.txt').write_text('x')
            with self.assertRaises(ValueError):
                cache.validate_cache(store, Path(store.anchor), mounts)
            (store / 'unexpected.txt').unlink()
            mounts.write_text('1 2 0:3 / ' + store.as_posix() + ' rw - ext4 root rw\n')
            with self.assertRaises(ValueError):
                cache.validate_cache(store, Path(store.anchor), mounts)
            mounts.write_text('')
            (store / 'package.deb').write_bytes(b'archive')
            with contextlib.redirect_stdout(io.StringIO()):
                cache.validate_cache(store, Path(store.anchor), mounts)

    def test_PublicProjection_ProtectedLocalPath_Redacts(self):
        rows = [{'name': recovery.PROTECTED, 'path': 'private-local-path', 'key': 'a', 'version': 2}]
        self.assertEqual(recovery.public_registrations(rows)[0]['path'], recovery.PROTECTED_PATH)
        self.assertTrue(recovery.PROTECTED_PATH.startswith('%USERPROFILE%'))
        with patch.dict(recovery.os.environ, {'USERPROFILE': 'synthetic-profile'}):
            self.assertEqual(recovery.public_value({'argv': ['synthetic-profile/python']}),
                             {'argv': ['%USERPROFILE%/python']})


if __name__ == '__main__':
    unittest.main()
