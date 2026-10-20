"""Offline-fast real-Git producer tests for `contract.py prepare` (Ruling 235 (3), carried from the candidate 46c0df8c at
plan node C5 and re-based on the `/2` contract: architecture, derived target path, protected state, host floors, the
seven inputs). Synthetic input bytes, no live authority, no WSL."""
import argparse
import contextlib
import hashlib
import io
import json
from pathlib import Path, PureWindowsPath
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'docs/proof/win-gpu-f1-preflight/packet'))
import architecture  # noqa: E402
import attempt  # noqa: E402
import contract  # noqa: E402

KEYRING = 'docs/proof/win-gpu-f1-preflight/inputs/ubuntu-cloudimage-keyring.gpg'
LICENCES = 'docs/proof/win-gpu-b2/license-map.json'


class PreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.repo = Path(self.temp.name).resolve()
        shutil.copytree(ROOT / contract.PACKET, self.repo / contract.PACKET, ignore=shutil.ignore_patterns('__pycache__'))
        (self.repo / '.gitattributes').write_bytes(b'* -text\n')
        for name in (attempt.EXTERNAL, attempt.COORDINATOR, LICENCES, KEYRING):
            dest = self.repo / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, dest)
        output = self.repo / attempt.PREFIX / 'attempt-2'
        output.mkdir()
        (output / '.keep').write_bytes(b'fixture\n')
        inputs = self.repo / 'inputs'
        inputs.mkdir()
        self.token = 'amd64'
        values, hashes = {}, {}
        for name in ('rootfs', 'ubuntu_sums', 'ubuntu_signature', 'openfoam_keyring', 'nvidia_keyring'):
            path = inputs / (architecture.TABLE[self.token]['rootfs_name'] if name == 'rootfs' else name)
            path.write_bytes(name.encode())
            values[name] = path
            hashes[name] = hashlib.sha256(path.read_bytes()).hexdigest()
        values['ubuntu_cloudimage_keyring'] = self.repo / KEYRING
        self.addCleanup(patch.stopall)
        patch.dict(attempt.INPUT_HASHES, {k: v for k, v in hashes.items() if k != 'rootfs'}).start()
        patch.dict(architecture.TABLE[self.token], rootfs_sha256=hashes['rootfs']).start()
        self.git('init', '-q')
        self.git('add', '.')
        self.git('-c', 'user.name=Offline Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'source')
        self.worktree = r'C:\Projects\future-f1-fixture'
        self.args = argparse.Namespace(
            command='prepare', repo=self.repo, contract=output / 'invocation-contract.json',
            authorization=output / 'attempt-authorization.json', source_commit=self.git('rev-parse', 'HEAD'),
            execution_worktree=self.worktree, leader_epoch=8, requested_ruling=241, attempt=2,
            architecture=self.token, protected='none', host_start_floor_bytes=30000000000, host_reserve_bytes=10000000000,
            branch='win/gpu-f1-attempt2', session='pc-f1-attempt2', run_id='f1-attempt2-20261019', **values)

    def git(self, *args):
        return subprocess.check_output(['git', '-C', str(self.repo), *args], stderr=subprocess.PIPE, text=True).strip()

    def outputs(self):
        return {path.name: path.read_bytes() for path in (self.args.contract, self.args.authorization) if path.exists()}

    def prepare(self):
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            contract.prepare(self.args)
        return dict(line.split('=', 1) for line in stdout.getvalue().splitlines() if '=' in line and ' ' not in line)

    def test_Prepare_EmitsPair_HashesMatchCommitted(self):
        printed = self.prepare()
        contract_bytes = self.args.contract.read_bytes()
        authorization_bytes = self.args.authorization.read_bytes()
        self.assertEqual(printed['contract_sha256'], hashlib.sha256(contract_bytes).hexdigest())
        self.assertEqual(printed['authorization_sha256'], hashlib.sha256(authorization_bytes).hexdigest())
        document = json.loads(contract_bytes)
        self.assertEqual(document['schema'], 'cfdw-f1-r217-invocation/2')
        self.assertEqual(document['architecture'], self.token)
        self.assertEqual(document['protected'], 'none')
        self.assertEqual(document['accepted_packet_commit'], self.args.source_commit)
        self.assertEqual(document['target_path'], contract.derived_target_path(PureWindowsPath(self.worktree)))
        self.assertEqual((document['host_start_floor_bytes'], document['host_reserve_bytes']), (30000000000, 10000000000))
        for name, digest in document['packet_sha256'].items():
            blob = subprocess.check_output(['git', '-C', str(self.repo), 'show', f"{self.args.source_commit}:{name}"])
            self.assertEqual(digest, hashlib.sha256(blob).hexdigest(), name)
        self.assertEqual(set(document['inputs']), set(contract.INPUT_KEYS))
        self.assertEqual(document['inputs']['rootfs']['path'], str(self.args.rootfs))
        self.assertEqual(document['inputs']['b2_licenses']['path'], self.worktree + r'\docs\proof\win-gpu-b2\license-map.json')
        self.assertEqual(document['inputs']['ubuntu_cloudimage_keyring']['path'],
                         self.worktree + r'\docs\proof\win-gpu-f1-preflight\inputs\ubuntu-cloudimage-keyring.gpg')
        self.assertEqual(document['inputs']['ubuntu_cloudimage_keyring']['sha256'],
                         hashlib.sha256((ROOT / KEYRING).read_bytes()).hexdigest())
        contract.check_contract_fields(document)
        auth = json.loads(authorization_bytes)
        self.assertEqual(auth, attempt.authorization(document, printed['contract_sha256'], ruling=241))
        self.assertEqual(auth['protected_key'], 'not applicable')
        attempt.check_authorization(auth, document, printed['contract_sha256'], proposed=True)
        with self.assertRaises(ValueError):
            attempt.check_authorization(auth, document, printed['contract_sha256'])
        self.assertFalse(Path(self.worktree).exists())
        self.assertEqual(set(self.outputs()), {'invocation-contract.json', 'attempt-authorization.json'})

    def test_Prepare_AttemptThree_DerivesRunRootAndNames(self):
        # Ruling 243 (2): the pair for attempt 3 lands under attempt-3 with the names derived from the number.
        output = self.repo / attempt.PREFIX / 'attempt-3'
        output.mkdir()
        (output / '.keep').write_bytes(b'fixture\n')
        self.git('add', '.')
        self.git('-c', 'user.name=Offline Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'attempt-3 folder')
        self.args.source_commit = self.git('rev-parse', 'HEAD')
        self.args.attempt = 3
        self.args.contract = output / 'invocation-contract.json'
        self.args.authorization = output / 'attempt-authorization.json'
        self.args.branch, self.args.session, self.args.run_id = 'win/gpu-f1-attempt3', 'pc-f1-attempt3', 'f1-attempt3-20261020'
        printed = self.prepare()
        document = json.loads(self.args.contract.read_bytes())
        self.assertEqual((document['attempt'], document['run_root']), (3, attempt.PREFIX + '/attempt-3'))
        self.assertEqual((document['execution_branch'], document['session'], document['run_id']),
                         ('win/gpu-f1-attempt3', 'pc-f1-attempt3', 'f1-attempt3-20261020'))
        auth = json.loads(self.args.authorization.read_bytes())
        self.assertEqual(auth, attempt.authorization(document, printed['contract_sha256'], ruling=241))
        self.assertEqual(set(self.outputs()), {'invocation-contract.json', 'attempt-authorization.json'})

    def test_Prepare_AttemptMissingOrOutsideScheme_RefusesWithoutOutputs(self):
        for value in (1, 7, '3', True, 3.0, None):
            if value is None:
                del self.args.attempt
            else:
                self.args.attempt = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                contract.prepare(self.args)
            self.assertEqual(self.outputs(), {})

    def test_Prepare_WritesTheRunRootCaptureAttributes(self):
        # Ruling 245 (3): the pair's run root keeps its captured bytes; prepare writes the -text rules it needs, once.
        (self.repo / '.gitattributes').write_bytes(b'* text=auto eol=lf\n')
        self.git('add', '.')
        self.git('-c', 'user.name=Offline Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'attributes')
        self.args.source_commit = self.git('rev-parse', 'HEAD')
        printed = self.prepare()
        self.assertEqual(printed['capture_attributes_added'], '3')
        attributes = (self.repo / '.gitattributes').read_text(encoding='utf-8')
        for folder in ('checkpoint', 'final', 'live'):
            self.assertIn(f'{attempt.PREFIX}/attempt-2/{folder}/** -text', attributes)
        out = subprocess.check_output(['git', '-C', str(self.repo), 'check-attr', 'text', '--',
                                       f'{attempt.PREFIX}/attempt-2/checkpoint/x', f'{attempt.PREFIX}/attempt-2/live/x'],
                                      text=True, encoding='utf-8')
        self.assertEqual(out.count(': text: unset'), 2)
        for path in (self.args.contract, self.args.authorization):
            path.unlink()
        self.git('add', '.')
        self.git('-c', 'user.name=Offline Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'rules')
        self.args.source_commit = self.git('rev-parse', 'HEAD')
        self.assertEqual(self.prepare()['capture_attributes_added'], '0')

    def test_Prepare_DeclaredProtected_ProjectsTheBinding(self):
        self.args.protected = 'cfdw-openfoam2512'
        printed = self.prepare()
        auth = json.loads(self.args.authorization.read_bytes())
        self.assertEqual(auth['protected_key'], attempt.PROTECTED_KEY)
        self.assertEqual(auth['keepalive'], attempt.KEEPALIVE)
        self.assertEqual(printed['contract_sha256'], hashlib.sha256(self.args.contract.read_bytes()).hexdigest())

    def test_Prepare_WrongArchitectureProtectedOrFloors_RefusesWithoutOutputs(self):
        for name, value in (('architecture', 'arm64'), ('architecture', 'X64'), ('protected', 'other'), ('protected', ''),
                            ('host_start_floor_bytes', 5), ('host_reserve_bytes', 30000000001), ('host_start_floor_bytes', '30000000000')):
            with self.subTest(name=name, value=value):
                original = getattr(self.args, name)
                setattr(self.args, name, value)
                with self.assertRaises(ValueError):
                    contract.prepare(self.args)
                self.assertEqual(self.outputs(), {})
                setattr(self.args, name, original)

    def test_Prepare_WrongDirtyNonHeadSource_RefusesWithoutOutputs(self):
        original = self.args.source_commit
        self.args.source_commit = 'c' * 40
        with self.assertRaises(ValueError):
            contract.prepare(self.args)
        self.assertEqual(self.outputs(), {})
        self.args.source_commit = original
        dirty = self.repo / 'dirty'
        dirty.write_text('dirty', encoding='utf-8')
        with self.assertRaises(ValueError):
            contract.prepare(self.args)
        self.assertEqual(self.outputs(), {})
        dirty.unlink()
        self.git('-c', 'user.name=Offline Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '-qm', 'later')
        with self.assertRaises(ValueError):
            contract.prepare(self.args)
        self.assertEqual(self.outputs(), {})

    def test_Prepare_ExistingOutputOrTraversal_RefusesWithoutNewOutput(self):
        self.args.authorization.write_bytes(b'preserve')
        before = self.outputs()
        with self.assertRaises(ValueError):
            contract.prepare(self.args)
        self.assertEqual(self.outputs(), before)
        self.args.authorization.unlink()
        self.args.execution_worktree = r'C:\Projects\new\..\old'
        with self.assertRaises(ValueError):
            contract.prepare(self.args)
        self.assertEqual(self.outputs(), {})

    def test_Prepare_InvalidEpochOrRuling_RefusesWithoutOutputs(self):
        for name in ('leader_epoch', 'requested_ruling'):
            original = getattr(self.args, name)
            for value in (None, True, 0, -1, '8', 8.0):
                with self.subTest(name=name, value=value):
                    setattr(self.args, name, value)
                    with self.assertRaises(ValueError):
                        contract.prepare(self.args)
                    self.assertEqual(self.outputs(), {})
            setattr(self.args, name, original)

    def test_Prepare_SourceBlobMismatch_RefusesEvenIfStatusLooksClean(self):
        path = self.repo / contract.PACKET / 'Control.ps1'
        path.write_bytes(path.read_bytes() + b'changed')
        real_git = contract.git
        def hide_status(repo, *args):
            return '' if args[0] == 'status' else real_git(repo, *args)
        with patch.object(contract, 'git', side_effect=hide_status):
            with self.assertRaises(ValueError):
                contract.prepare(self.args)
        self.assertEqual(self.outputs(), {})


if __name__ == '__main__':
    unittest.main()
