"""Plan docs/plans/f1-rescope-r237.md node C4 (Ruling 240 (1) and (2)): the protected distro is a contract field with a
`none` state, and the reviewed Ubuntu cloud-image keyring is a hash-bound contract input read by the Windows-side
signature check. Offline fast ring; no gpg, no WSL (the gpgv path is proved by tests/f1_protected_none.ps1).
"""
import hashlib
import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
KEYRING = ROOT / 'docs/proof/win-gpu-f1-preflight/inputs/ubuntu-cloudimage-keyring.gpg'
sys.path.insert(0, str(PACKET))
import architecture  # noqa: E402
import attempt  # noqa: E402
import contract  # noqa: E402


def attempt2_fixture():
    data = json.loads((ROOT / attempt.PREFIX / 'attempt-1/invocation-contract.json').read_bytes())
    worktree = r'C:\Projects\f1-execution'
    data.update(schema='cfdw-f1-r217-invocation/2', architecture='amd64',
                host_start_floor_bytes=19684091961, host_reserve_bytes=10737418240,
                attempt=2, leader_epoch=19, execution_branch='win/gpu-f1-attempt2',
                session='pc-f1-attempt2', run_id='f1-attempt2-20261013',
                run_root=attempt.PREFIX + '/attempt-2', execution_worktree=worktree)
    del data['inputs']['b2_lock']
    data['inputs']['b2_licenses']['path'] = worktree + '\\docs\\proof\\win-gpu-b2\\license-map.json'
    data['inputs']['ubuntu_cloudimage_keyring'] = {
        'path': worktree + '\\docs\\proof\\win-gpu-f1-preflight\\inputs\\ubuntu-cloudimage-keyring.gpg',
        'sha256': attempt.INPUT_HASHES['ubuntu_cloudimage_keyring']}
    data['external_sha256']['docs/ai-forward-pack/scripts/coord-core.py'] = 'b' * 64
    return data


class ProtectedNoneTests(unittest.TestCase):
    def test_Contract_CloudimageKeyringHashMismatch_Refuses(self):
        self.assertIn('ubuntu_cloudimage_keyring', contract.INPUT_KEYS)
        self.assertEqual(len(contract.INPUT_KEYS), 7)
        # The reviewed value is the committed copy's hash (ubuntu-keyring 2023.11.28.1, read from Ubuntu-24.04 on the laptop).
        self.assertEqual(hashlib.sha256(KEYRING.read_bytes()).hexdigest(), attempt.INPUT_HASHES['ubuntu_cloudimage_keyring'])
        for token in ('amd64', 'arm64'):
            self.assertEqual(set(attempt.input_hashes(token)), set(contract.INPUT_KEYS))
        contract.check_contract_fields(attempt2_fixture())
        data = attempt2_fixture()
        data['inputs']['ubuntu_cloudimage_keyring']['sha256'] = 'd' * 64
        with self.assertRaisesRegex(ValueError, 'accepted input hash changed: ubuntu_cloudimage_keyring'):
            contract.check_contract_fields(data)
        data = attempt2_fixture()
        del data['inputs']['ubuntu_cloudimage_keyring']
        with self.assertRaisesRegex(ValueError, 'unreviewed input: ubuntu_cloudimage_keyring'):
            contract.check_contract_fields(data)
        with self.assertRaisesRegex(ValueError, 'ubuntu_cloudimage_keyring path differs from execution worktree'):
            attempt.check_binding(data)   # the worktree binding refuses before the set count does
        data['inputs']['ubuntu_cloudimage_keyring'] = {'path': 'x', 'sha256': 'a' * 64}
        data['execution_worktree'] = r'C:\Projects\f1-execution'
        data['inputs']['ubuntu_cloudimage_keyring']['path'] = attempt2_fixture()['inputs']['ubuntu_cloudimage_keyring']['path']
        data['inputs']['extra'] = {'path': 'x', 'sha256': 'a' * 64}
        with self.assertRaisesRegex(ValueError, 'exact seven reviewed inputs'):
            attempt.check_binding(data)
        data = attempt2_fixture()   # the keyring is reviewed in the execution worktree, like the B2 licence map
        data['inputs']['ubuntu_cloudimage_keyring']['path'] = r'C:\reviewed\ubuntu-cloudimage-keyring.gpg'
        with self.assertRaisesRegex(ValueError, 'differs from execution worktree'):
            contract.check_contract_fields(data)

    def test_Contract_ProtectedNone_Admitted_OtherRefused(self):
        data = attempt2_fixture()
        self.assertEqual(data['protected'], 'cfdw-openfoam2512')
        contract.check_contract_fields(dict(data, protected='none'))
        for value in ('', 'None', 'Ubuntu-24.04', None, 0, ['none']):
            with self.subTest(value=value), self.assertRaisesRegex(ValueError, 'distro identity'):
                contract.check_contract_fields(dict(data, protected=value))

    def test_Authorization_ProtectedNone_RecordsNotApplicable(self):
        sha = 'e' * 64
        declared = attempt.authorization(attempt2_fixture(), sha, accepted=True, ruling=241)
        self.assertEqual(declared['protected'], 'cfdw-openfoam2512')
        self.assertEqual(declared['protected_key'], attempt.PROTECTED_KEY)
        self.assertEqual(declared['protected_path'], attempt.PROTECTED_PATH)
        self.assertEqual(declared['keepalive'], attempt.KEEPALIVE)
        none = attempt.authorization(dict(attempt2_fixture(), protected='none'), sha, accepted=True, ruling=241)
        self.assertEqual(none['protected'], 'none')
        for key in ('protected_key', 'protected_path', 'keepalive'):
            self.assertEqual(none[key], 'not applicable', key)
        self.assertEqual(set(none), set(declared), 'the projection keys must not depend on the state')
        attempt.check_authorization(none, dict(attempt2_fixture(), protected='none'), sha)
        with self.assertRaises(ValueError):
            attempt.check_authorization(none, attempt2_fixture(), sha)

    def test_HostIdentity_RecordsGpgv(self):
        observations = dict(schema='cfdw-f1-host-observations/1', utc='2026-10-19T00:00:00+00:00',
                            os_architecture_cim='ARM 64-bit Processor', os_architecture='Arm64',
                            process_architecture='Arm64', processor_architecture_env='ARM64',
                            processor_architew6432_env='', os_version='10.0.28000.0', powershell_version='7.6.7',
                            powershell_edition='Core', cpu=['Fixture CPU'], gpu=['Fixture GPU'],
                            gpgv_path=r'C:\Program Files\Git\usr\bin\gpgv.exe', gpgv_version='gpgv (GnuPG) 2.4.9')
        receipt = architecture.host_identity(observations, 'arm64')
        self.assertIsNone(receipt['refusal'])
        self.assertEqual(receipt['gpgv'], {'path': r'C:\Program Files\Git\usr\bin\gpgv.exe', 'version': 'gpgv (GnuPG) 2.4.9'})
        del observations['gpgv_path'], observations['gpgv_version']
        receipt = architecture.host_identity(observations, 'arm64')
        self.assertEqual(receipt['gpgv'], {'path': 'Not recorded', 'version': 'Not recorded'})
        self.assertIsNone(receipt['refusal'], 'the signature step, not the identity receipt, refuses an absent gpgv')


if __name__ == '__main__':
    unittest.main()
