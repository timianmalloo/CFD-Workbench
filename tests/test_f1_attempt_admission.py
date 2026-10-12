"""Offline-admission ring; no WSL effects; recursive conservation costs ~7 s."""
import copy
import hashlib
import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(PACKET))
import contract
import attempt


def fixture():
    value = json.loads((ROOT / 'docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json').read_bytes())
    value.update(attempt=1, leader_epoch=7, execution_branch='win/gpu-f1-r225-attempt1',
                 session='pc-f1-r225-attempt1', run_id='r225-attempt1-epoch7-20261012',
                 run_root='docs/proof/win-gpu-f1-preflight/attempt-1')
    value['external_sha256'] = {attempt.EXTERNAL: hashlib.sha256((ROOT / attempt.EXTERNAL).read_bytes()).hexdigest()}
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
                  'epoch 7', sha, value['run_root'] + '/attempt-authorization.json']
        exact = '### Ruling 227 — fixture\n' + '\n'.join(tokens) + '\n'
        attempt.check_ruling(exact, value, sha)
        for text in ('### Ruling 227 — fixture\nmissing\n### Ruling 228 — fixture\n' + '\n'.join(tokens),
                     exact.replace(sha, 'd' * 64), exact.replace('ACCEPT ONE', 'PROPOSED ONE')):
            with self.subTest(text=text[:40]), self.assertRaises(ValueError):
                attempt.check_ruling(text, value, sha)

    def test_ActualProjection_OnlyAuthorityFieldsDifferAndCannotUseLivePath(self):
        root = ROOT / attempt.PREFIX / 'attempt-1'
        data = json.loads((root / 'invocation-contract.json').read_bytes())
        sha = hashlib.sha256((root / 'invocation-contract.json').read_bytes()).hexdigest()
        proposed = json.loads((root / 'attempt-authorization.json').read_bytes())
        projection_path = root / 'authorization.accepted-projection.json'
        projection = json.loads(projection_path.read_bytes())
        self.assertEqual({key for key in proposed if proposed[key] != projection[key]}, {'status', 'ruling'})
        attempt.check_authorization(proposed, data, sha, proposed=True)
        attempt.check_authorization(projection, data, sha)
        with self.assertRaisesRegex(ValueError, 'outside exact live path'):
            attempt.verify_authorization(ROOT, projection_path, data, sha, data['run_id'], data['run_id'])

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
