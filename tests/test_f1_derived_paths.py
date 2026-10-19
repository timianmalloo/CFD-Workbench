"""F1 re-scope node C2 (plan docs/plans/f1-rescope-r237.md; Rulings 237, 238 (3), 240): the contract records the target
path it was prepared for and the free-space floors; the packet derives the path from the repository location and
refuses a contract that differs. Ring: offline-fast (tests/test_f1_*.py under tools/run-tests.sh); no WSL."""
import json
import sys
import unittest
from pathlib import Path, PureWindowsPath

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(PACKET))
import architecture
import attempt
import contract

OLD_CONSTANT = r'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
LAPTOP = r'D:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'


def fixture(target_path=LAPTOP):
    value = json.loads((ROOT / 'docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json').read_bytes())
    row = architecture.TABLE['amd64']
    value.update(schema='cfdw-f1-r217-invocation/2', architecture='amd64', target_path=target_path,
                 host_start_floor_bytes=19684091961, host_reserve_bytes=10737418240)
    value['inputs']['rootfs'] = {'path': 'C:\\reviewed\\' + row['rootfs_name'], 'sha256': row['rootfs_sha256']}
    value['inputs']['ubuntu_cloudimage_keyring'] = {'path': 'C:\\reviewed\\ubuntu-cloudimage-keyring.gpg',
                                                    'sha256': attempt.INPUT_HASHES['ubuntu_cloudimage_keyring']}   # joined at C4
    return value


class DerivedPathTests(unittest.TestCase):
    def test_DerivedTargetPath_RepoParentOnDAndC(self):
        self.assertEqual(contract.derived_target_path(PureWindowsPath(r'D:\Projects\CFD-Workbench-f1-attempt2')), LAPTOP)
        self.assertEqual(contract.derived_target_path(PureWindowsPath(r'C:\Projects\CFD-Workbench-win-gpu-f1-r225-attempt1')), OLD_CONSTANT)
        for bad in (r'D:\\', r'relative\CFD-Workbench', r'\\server\share\CFD-Workbench', r'D:\Pro jects\CFD-Workbench'):
            with self.subTest(repo=bad), self.assertRaises(ValueError):
                contract.derived_target_path(PureWindowsPath(bad))

    def test_Contract_TargetPathShape_RefusesOutsideTheWslRoot(self):
        contract.check_contract_fields(fixture())
        contract.check_contract_fields(fixture(OLD_CONSTANT))
        for bad in (r'D:\Projects\cfdw-petsc-cuda132-f1', r'D:\Projects\CFD-Workbench-wsl\other',
                    r'D:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1\..\x', r'Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1',
                    r'D:\Pro jects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1', '', None, 7):
            with self.subTest(path=bad), self.assertRaises(ValueError):
                contract.check_contract_fields(fixture(bad))

    def test_Contract_TargetPathDiffersFromDerived_Refuses(self):
        document = fixture()
        contract.check_target_path_derived(document, PureWindowsPath(r'D:\Projects\CFD-Workbench-f1-attempt2'))
        contract.check_target_path_derived(fixture(LAPTOP.lower()), PureWindowsPath(r'D:\Projects\CFD-Workbench-f1-attempt2'))
        with self.assertRaises(ValueError):
            contract.check_target_path_derived(document, PureWindowsPath(r'C:\Projects\CFD-Workbench-win-gpu-f1-r225-attempt1'))
        with self.assertRaises(ValueError):
            contract.check_target_path_derived(fixture(OLD_CONSTANT), PureWindowsPath(r'D:\Projects\CFD-Workbench-f1-attempt2'))

    def test_Contract_HostFloors_RequiredPositiveAndOrdered(self):
        for label, change in (('floor absent', {'host_start_floor_bytes': None}), ('reserve absent', {'host_reserve_bytes': None}),
                              ('string', {'host_start_floor_bytes': '19684091961'}), ('bool', {'host_reserve_bytes': True}),
                              ('zero', {'host_reserve_bytes': 0}), ('float', {'host_start_floor_bytes': 19684091961.0}),
                              ('floor not above reserve', {'host_start_floor_bytes': 10737418240})):
            document = fixture()
            for key, value in change.items():
                if value is None:
                    del document[key]
                else:
                    document[key] = value
            with self.subTest(case=label), self.assertRaises(ValueError):
                contract.check_contract_fields(document)

    def test_AttemptLimits_FloorsComeFromTheContract(self):
        document = fixture()
        limits = attempt.host_limits(document)
        self.assertEqual(limits['host_initial_free_bytes'], 19684091961)
        self.assertEqual(limits['host_reserve_bytes'], 10737418240)
        other = dict(document, host_start_floor_bytes=30000000000, host_reserve_bytes=12000000000)
        self.assertEqual(attempt.host_limits(other)['host_initial_free_bytes'], 30000000000)
        self.assertEqual(attempt.host_limits(other)['host_reserve_bytes'], 12000000000)
        self.assertEqual(limits['vhd_max_bytes'], 8589934592)
        from test_f1_attempt_admission import fixture as attempt_fixture
        bound = dict(attempt_fixture(), host_start_floor_bytes=30000000000, host_reserve_bytes=12000000000)
        self.assertEqual(attempt.authorization(bound, 'a' * 64, accepted=True)['limits'], attempt.host_limits(bound))
        self.assertEqual(attempt.authorization(bound, 'a' * 64, accepted=True)['limits']['host_initial_free_bytes'], 30000000000)
        with self.assertRaises(ValueError):
            attempt.host_limits({k: v for k, v in document.items() if k != 'host_reserve_bytes'})


if __name__ == '__main__':
    unittest.main()
