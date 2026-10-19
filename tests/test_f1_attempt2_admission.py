"""Offline-fast attempt-2 authority/lease contracts; no WSL or lease mutation."""
import copy
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(PACKET))
import attempt
import contract


def fixture(epoch=19):
    data = json.loads((ROOT / attempt.PREFIX / 'attempt-1/invocation-contract.json').read_bytes())
    data.update(schema='cfdw-f1-r217-invocation/2', architecture='amd64',
                host_start_floor_bytes=19684091961, host_reserve_bytes=10737418240,
                attempt=2, leader_epoch=epoch, execution_branch='win/gpu-f1-attempt2',
                session='pc-f1-attempt2', run_id='f1-attempt2-20261013',
                run_root=attempt.PREFIX + '/attempt-2', execution_worktree=r'C:\Projects\f1-execution')
    del data['inputs']['b2_lock']   # the committed attempt-1 contract is /1; the B2 lock left the input set at C3
    data['inputs']['b2_licenses']['path'] = data['execution_worktree'] + '\\docs\\proof\\win-gpu-b2\\license-map.json'
    data['inputs']['ubuntu_cloudimage_keyring'] = {   # joined the input set at C4; reviewed in the execution worktree
        'path': data['execution_worktree'] + '\\docs\\proof\\win-gpu-f1-preflight\\inputs\\ubuntu-cloudimage-keyring.gpg',
        'sha256': attempt.INPUT_HASHES['ubuntu_cloudimage_keyring']}
    data['external_sha256']['docs/ai-forward-pack/scripts/coord-core.py'] = 'b'*64
    return data


class AttemptTwoTests(unittest.TestCase):
    def test_Binding_ExactAttemptTwoAndSelectedEpoch_Passes(self):
        for epoch in (8, 19):
            contract.check_contract_fields(fixture(epoch))

    def test_Binding_StaleOrMalformedIdentity_Refuses(self):
        for key, value in (('attempt', 3), ('attempt', True), ('leader_epoch', True),
                           ('leader_epoch', '19'), ('leader_epoch', 0), ('leader_epoch', 19.0),
                           ('execution_branch', 'win/gpu-f1-r225-attempt1'),
                           ('session', 'pc-f1-r225-attempt1'), ('run_id', 'r225-attempt1-epoch8-20261012'),
                           ('run_root', attempt.PREFIX + '/attempt-1')):
            with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                contract.check_contract_fields(dict(fixture(), **{key: value}))

    def test_Binding_B2OldWorktreeOrTraversal_Refuses(self):
        data = fixture()
        data['inputs']['b2_licenses']['path'] = r'C:\Projects\old-attempt1\file.json'
        with self.assertRaises(ValueError):
            contract.check_contract_fields(data)
        data = fixture()   # a contract that still lists the B2 lock as an input is refused: nothing reads it
        data['inputs']['b2_lock'] = dict(data['inputs']['b2_licenses'])
        with self.assertRaisesRegex(ValueError, 'exact seven reviewed inputs'):
            contract.check_contract_fields(data)
        for path in (r'C:\Projects\foo\..\old', 'relative', ''):
            with self.subTest(path=path), self.assertRaises(ValueError):
                contract.check_contract_fields(dict(fixture(), execution_worktree=path))

    def test_Authorization_RulingIsSuppliedAndStrict_ThenExactMainSectionRequired(self):
        data = fixture()
        for number in (901, 902):  # Synthetic test numbers; no live ruling is asserted.
            value = attempt.authorization(data, 'a' * 64, accepted=True, ruling=number)
            self.assertEqual(value['ruling'], number)
            self.assertEqual(value['leader_epoch'], 19)
            attempt.check_authorization(value, data, 'a' * 64)
            tokens = ['ACCEPT ONE PREFLIGHT ATTEMPT', value['contract_path'], value['contract_sha256'],
                      value['source_commit'], value['run_id'], value['session'], value['execution_branch'],
                      'epoch 19', 'c' * 64, value['run_root'] + '/attempt-authorization.json']
            exact = f'### Ruling {number} - synthetic fixture\n' + '\n'.join(tokens)
            attempt.check_ruling(exact, value, 'c' * 64)
            for bad in (exact.replace('epoch 19', 'epoch 8'), exact.replace('c' * 64, 'd' * 64),
                        exact.replace('epoch 19', 'epoch 190'),
                        exact.replace('ACCEPT ONE', 'PROPOSED ONE'), exact.replace(str(number), str(number+1))):
                with self.assertRaises(ValueError):
                    attempt.check_ruling(bad, value, 'c' * 64)
        for number in (None, True, 0, -1, '901', 901.0):
            with self.subTest(number=number), self.assertRaises(ValueError):
                attempt.authorization(data, 'a' * 64, accepted=True, ruling=number)

    def test_CoordinatorMutationPrimitive_RequiredOnlyForAttemptTwo(self):
        data = fixture()
        data['external_sha256'].pop('docs/ai-forward-pack/scripts/coord-core.py', None)
        with self.assertRaises(ValueError):
            attempt.check_binding(data)

    def test_Authorization_ExactScopeAndProposedRefusal(self):
        data = fixture()
        good = attempt.authorization(data, 'a' * 64, accepted=True, ruling=901)
        self.assertEqual(good['target_previous_keys'], ['{e958c4d5-ae78-4b50-a634-7102f334d3aa}', '{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}'])
        for key, value in (('ruling', True), ('requested_ruling', 902), ('leader_epoch', 8),
                           ('target_previous_keys', []), ('renewal_evidence', []), ('keepalive', {})):
            with self.subTest(key=key), self.assertRaises(ValueError):
                attempt.check_authorization(dict(good, **{key: value}), data, 'a' * 64)
        with self.assertRaises(ValueError):
            attempt.check_authorization(attempt.authorization(data, 'a' * 64, ruling=901), data, 'a' * 64)

    def test_LeaderJson_ExactAndFiniteFloor_PassesOnlyAtOrAbove200(self):
        good = dict(leader='mac', state='live', epoch=19, expires_in=200, oid='a'*40)
        contract.check_live_leader_json(good, 19)
        for key, value in (('expires_in', 199.999), ('expires_in', math.inf), ('expires_in', math.nan),
                           ('expires_in', -math.inf), ('expires_in', '300'), ('expires_in', True),
                           ('expires_in', None), ('epoch', '19'), ('epoch', 19.0), ('epoch', True),
                           ('epoch', 8), ('leader', 'Mac'), ('state', 'expired'), ('oid', '')):
            with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                contract.check_live_leader_json(dict(good, **{key:value}), 19)

    def test_CaptureAttributes_NewAttemptUnset(self):
        names = [attempt.PREFIX + '/attempt-2/' + folder + '/capture.txt' for folder in ('checkpoint', 'final')]
        result = subprocess.run(['git', 'check-attr', 'text', '--', *names], cwd=ROOT, capture_output=True, text=True, check=True)
        self.assertEqual([line.rsplit(': ', 1)[-1] for line in result.stdout.splitlines()], ['unset'] * 2)

    def test_HistoricalAttemptOne_AllCommittedBlobsConserved(self):
        prefix = attempt.PREFIX + '/attempt-1'
        names = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', 'dc3e0752', '--', prefix], cwd=ROOT, text=True).splitlines()
        self.assertTrue(names)
        for name in names:
            expected = subprocess.check_output(['git', 'show', 'dc3e0752:' + name], cwd=ROOT)
            self.assertEqual((ROOT / name).read_bytes(), expected, name)


if __name__ == '__main__':
    unittest.main()
