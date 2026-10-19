"""Offline-admission ring; no WSL effects; recursive conservation costs ~7 s."""
import copy
import hashlib
import json
from pathlib import Path, PureWindowsPath
import sys
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(PACKET))
import contract
import attempt


def fixture():
    value = json.loads((ROOT / 'docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json').read_bytes())
    value.update(schema='cfdw-f1-r217-invocation/2', architecture='amd64',
                 host_start_floor_bytes=19684091961, host_reserve_bytes=10737418240,
                 attempt=1, leader_epoch=8, execution_branch='win/gpu-f1-r225-attempt1',
                 session='pc-f1-r225-attempt1', run_id='r225-attempt1-epoch8-20261012',
                 run_root='docs/proof/win-gpu-f1-preflight/attempt-1')
    value['external_sha256'] = {attempt.EXTERNAL: hashlib.sha256((ROOT / attempt.EXTERNAL).read_bytes()).hexdigest()}
    del value['inputs']['b2_lock']   # the committed run-1 contract is /1; the B2 lock left the input set at C3
    value['inputs']['ubuntu_cloudimage_keyring'] = {'path': 'C:\\reviewed\\ubuntu-cloudimage-keyring.gpg',
                                                    'sha256': attempt.INPUT_HASHES['ubuntu_cloudimage_keyring']}   # joined at C4
    return value


class AttemptAdmissionTests(unittest.TestCase):
    def test_FreshAttempt_BranchSessionRunRoot_ProductionFieldsAccept(self):
        contract.check_contract_fields(fixture())

    def test_ExactFieldMutations_Refuse(self):
        for key, value in (('attempt', 2), ('attempt', True), ('leader_epoch', 6),
                           ('run_id', 'old-run'), ('run_root', attempt.PREFIX + '/run-1'),
                           ('execution_branch', 'win/gpu-f1-r217-execute'), ('session', 'old-session'),
                           ('accepted_packet_commit', 'stale')):
            with self.subTest(key=key, value=value):
                data = fixture()
                data[key] = value
                with self.assertRaises(ValueError):
                    contract.check_contract_fields(data)

    def test_AuthorizationExactScopeMutations_Refuse(self):
        data = fixture()
        original = attempt.authorization(data, 'a' * 64)
        for key, value in (('source_commit', 'b' * 40), ('run_id', 'old'), ('leader_epoch', 6),
                           ('contract_sha256', 'b' * 64), ('allowed_stages', ['install']),
                           ('attempt_count', 2), ('status', 'unknown')):
            with self.subTest(key=key):
                changed = copy.deepcopy(original)
                changed[key] = value
                with self.assertRaises(ValueError):
                    attempt.check_authorization(changed, data, 'a' * 64, proposed=True)

    def test_ProposedAuthority_NormalPathRefuses(self):
        data = fixture()
        with self.assertRaisesRegex(ValueError, 'proposed authorization'):
            attempt.check_authorization(attempt.authorization(data, 'a' * 64), data, 'a' * 64)

    def test_ExactSignerAndSevenInputHashes_RefuseMutations(self):
        for key in contract.INPUT_KEYS:
            with self.subTest(key=key):
                data = fixture()
                data['inputs'][key]['sha256'] = 'b' * 64
                with self.assertRaises(ValueError):
                    contract.check_contract_fields(data)
        data = fixture()
        data['allowed_signers']['ubuntu_archive'].append('A' * 40)
        with self.assertRaises(ValueError):
            contract.check_contract_fields(data)

    def test_ExternalHelperChangedBytes_Refuses(self):
        with self.assertRaises(ValueError):
            contract.check_hash('b' * 64, (ROOT / attempt.EXTERNAL).read_bytes())

    def test_RulingBoundaries_OutsideTokensAndInsideWrongDigest_Refuse(self):
        value = attempt.authorization(fixture(), 'a' * 64, accepted=True)
        sha = 'c' * 64
        tokens = ['ACCEPT ONE PREFLIGHT ATTEMPT', value['contract_path'], value['contract_sha256'],
                  value['source_commit'], value['run_id'], value['session'], value['execution_branch'],
                  'epoch 8', sha, value['run_root'] + '/attempt-authorization.json']
        exact = '### Ruling 228 — fixture\n' + '\n'.join(tokens) + '\n'
        attempt.check_ruling(exact, value, sha)
        for text in ('### Ruling 228 — fixture\nmissing\n### Ruling 229 — fixture\n' + '\n'.join(tokens),
                     exact.replace(sha, 'd' * 64), exact.replace('ACCEPT ONE', 'PROPOSED ONE')):
            with self.subTest(text=text[:40]), self.assertRaises(ValueError):
                attempt.check_ruling(text, value, sha)

    def test_ActivatedLiveAuthorization_EqualsProjectionButProjectionPathRemainsNonCanonical(self):
        root = ROOT / attempt.PREFIX / 'attempt-1'
        data = json.loads((root / 'invocation-contract.json').read_bytes())
        sha = hashlib.sha256((root / 'invocation-contract.json').read_bytes()).hexdigest()
        live = json.loads((root / 'attempt-authorization.json').read_bytes())
        projection_path = root / 'authorization.accepted-projection.json'
        projection = json.loads(projection_path.read_bytes())
        self.assertEqual(live, projection)
        # The committed attempt-1 contract is schema /1; /2 code reads the floors from the contract and refuses /1 before
        # the path check (Rulings 237, 238 (3)), so the claims are made on the same document lifted to /2 with the
        # floors the committed authorization recorded.
        lifted = dict(data, schema='cfdw-f1-r217-invocation/2', architecture='amd64',
                      host_start_floor_bytes=19684091961, host_reserve_bytes=10737418240,
                      inputs=dict({key: value for key, value in data['inputs'].items() if key != 'b2_lock'},
                                  ubuntu_cloudimage_keyring=fixture()['inputs']['ubuntu_cloudimage_keyring']))
        # The committed attempt-1 authorization projected the /1 seven-input map (the B2 lock left the input set at
        # C3), so the projection equality is claimed over that map; the binding and path claims use the /2 input set.
        committed_inputs = dict(lifted, inputs=data['inputs'])
        attempt.check_authorization(live, committed_inputs, sha)
        attempt.check_authorization(projection, committed_inputs, sha)
        with self.assertRaisesRegex(ValueError, 'outside exact live path'):
            attempt.verify_authorization(ROOT, projection_path, lifted, sha, data['run_id'], data['run_id'])

    def verify_boundary_fixture(self, root):
        data = fixture()
        # Ruling 237: verify compares the contract's target_path with the path derived from the repository it runs in.
        data['target_path'] = contract.derived_target_path(PureWindowsPath(str(root.resolve())))
        packet_name = contract.PACKET + '/fixture.txt'
        packet_bytes, helper_bytes = b'packet-fixture', b'helper-fixture'
        for name, raw in ((packet_name, packet_bytes), (attempt.EXTERNAL, helper_bytes)):
            path = root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(raw)
        data['packet_sha256'] = {packet_name: hashlib.sha256(packet_bytes).hexdigest()}
        data['external_sha256'] = {attempt.EXTERNAL: hashlib.sha256(helper_bytes).hexdigest()}
        for key, name in (('b2_licenses', 'docs/proof/win-gpu-b2/license-map.json'),
                          ('ubuntu_cloudimage_keyring', 'docs/proof/win-gpu-f1-preflight/inputs/ubuntu-cloudimage-keyring.gpg')):
            path = root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes((ROOT / name).read_bytes())
            data['inputs'][key]['path'] = str(path.resolve())
        contract_path = root / data['run_root'] / 'invocation-contract.json'
        contract_path.parent.mkdir(parents=True)
        contract_raw = (json.dumps(data) + '\n').encode()
        contract_path.write_bytes(contract_raw)
        auth_path = contract_path.parent / 'attempt-authorization.json'
        accepted = attempt.authorization(data, hashlib.sha256(contract_raw).hexdigest(), accepted=True)
        accepted_raw = (json.dumps(accepted) + '\n').encode()
        auth_path.write_bytes(accepted_raw)
        tokens = ['ACCEPT ONE PREFLIGHT ATTEMPT', accepted['contract_path'], accepted['contract_sha256'],
                  accepted['source_commit'], accepted['run_id'], accepted['session'], accepted['execution_branch'],
                  'epoch 8', hashlib.sha256(accepted_raw).hexdigest(), data['run_root'] + '/attempt-authorization.json']
        ruling = '### Ruling 228 — synthetic fixture\n' + '\n'.join(tokens)
        calls = []
        def git_boundary(repo, *argv):
            if argv[0] == 'status': return ''
            if argv[0] == 'branch': return data['execution_branch']
            if argv[0] == 'ls-tree': return packet_name
            if argv == ('show', 'origin/main:docs/notes/rulings.md'): return ruling
            raise AssertionError('unexpected Git boundary: ' + repr(argv))
        def process_boundary(argv, **kwargs):
            calls.append(argv)
            if argv[0] == 'wsl.exe': raise AssertionError('WSL-SENTINEL: never launch real WSL')
            if argv[0] == sys.executable:
                return SimpleNamespace(stdout='leader mac\nepoch 8\nstate live\n', returncode=0)
            if 'merge-base' in argv: return SimpleNamespace(stdout=b'', returncode=0)
            if 'show' in argv:
                name = argv[-1]
                if name == 'HEAD:' + data['run_root'] + '/invocation-contract.json': raw = contract_path.read_bytes()
                elif name == 'HEAD:' + data['run_root'] + '/attempt-authorization.json': raw = auth_path.read_bytes()
                elif name.endswith(':' + packet_name): raw = packet_bytes
                elif name.endswith(':' + attempt.EXTERNAL): raw = helper_bytes
                else: raise AssertionError('unexpected blob boundary: ' + name)
                return SimpleNamespace(stdout=raw, returncode=0)
            raise AssertionError('unexpected process boundary')
        args = SimpleNamespace(command='verify', repo=root, contract=contract_path, authorization=auth_path,
            branch=data['execution_branch'], session=data['session'], run_id=data['run_id'], exclusive_run_id=data['run_id'],
            rootfs=Path(data['inputs']['rootfs']['path']), ubuntu_sums=Path(data['inputs']['ubuntu_sums']['path']),
            ubuntu_signature=Path(data['inputs']['ubuntu_signature']['path']),
            openfoam_keyring=Path(data['inputs']['openfoam_keyring']['path']), nvidia_keyring=Path(data['inputs']['nvidia_keyring']['path']),
            ubuntu_cloudimage_keyring=Path(data['inputs']['ubuntu_cloudimage_keyring']['path']))
        return data, args, accepted, git_boundary, process_boundary, calls

    def test_RealVerify_MissingProposedAlteredAuthority_ZeroWsl(self):
        for mode in ('missing', 'proposed', 'altered'):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as temporary:
                data, args, accepted, git_boundary, process_boundary, calls = self.verify_boundary_fixture(Path(temporary))
                if mode == 'missing': args.authorization = None
                else:
                    value = attempt.authorization(data, accepted['contract_sha256']) if mode == 'proposed' else dict(accepted, run_id='stale-run')
                    args.authorization.write_text(json.dumps(value), encoding='utf-8')
                with patch.object(contract, 'git', side_effect=git_boundary), patch.object(contract.subprocess, 'run', side_effect=process_boundary), patch.object(attempt, 'verify_authorization', wraps=attempt.verify_authorization) as authorization_boundary:
                    with self.assertRaisesRegex(ValueError, 'authorization.*(absent|proposed|source/run/hash/scope)|proposed authorization'):
                        contract.verify(args)
                    authorization_boundary.assert_called_once()
                    forwarded = authorization_boundary.call_args.args
                    self.assertEqual(forwarded[1], args.authorization)
                    self.assertEqual(forwarded[3], hashlib.sha256(args.contract.read_bytes()).hexdigest())
                    self.assertEqual(forwarded[4], args.run_id)
                    self.assertEqual(forwarded[5], args.exclusive_run_id)
                self.assertEqual([argv for argv in calls if argv[0] == 'wsl.exe'], [])

    def test_RealVerify_ExternalHelperMapWiring_RejectsChangedWorktreeAndSource(self):
        for location in ('worktree', 'source'):
            with self.subTest(location=location), tempfile.TemporaryDirectory() as temporary:
                data, args, accepted, git_boundary, process_boundary, calls = self.verify_boundary_fixture(Path(temporary))
                if location == 'worktree':
                    (Path(temporary) / attempt.EXTERNAL).write_bytes(b'changed-helper')
                def changed_source(argv, **kwargs):
                    result = process_boundary(argv, **kwargs)
                    if location == 'source' and argv[-1].endswith(':' + attempt.EXTERNAL):
                        result.stdout = b'changed-source-helper'
                    return result
                with patch.object(contract, 'git', side_effect=git_boundary), patch.object(contract.subprocess, 'run', side_effect=changed_source):
                    with self.assertRaisesRegex(ValueError, 'byte/hash mismatch'):
                        contract.verify(args)
                self.assertEqual([argv for argv in calls if argv[0] == 'wsl.exe'], [])

    def test_HistoricalRun1_RecursiveBytesEqualBase(self):
        import subprocess
        prefix = attempt.PREFIX + '/run-1'
        names = subprocess.run(['git', 'ls-tree', '-r', '--name-only', '17d7427b', '--', prefix],
                               cwd=ROOT, check=True, capture_output=True, text=True, encoding='utf-8').stdout.splitlines()
        self.assertTrue(names)
        for name in names:
            expected = subprocess.run(['git', 'show', '17d7427b:' + name], cwd=ROOT, check=True, capture_output=True).stdout
            self.assertEqual((ROOT / name).read_bytes(), expected, name)


if __name__ == '__main__':
    unittest.main()
