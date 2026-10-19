"""Offline-fast renewal protocol with deterministic subprocess/clock boundaries."""
import importlib
import json
from pathlib import Path, PureWindowsPath
import sys
import tempfile
import unittest
from unittest.mock import patch
from unittest.mock import Mock
import argparse

PACKET = Path(__file__).resolve().parents[1] / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(PACKET))
sys.path.insert(0, str(Path(__file__).resolve().parent))


class RenewalTests(unittest.TestCase):
    def test_OwnerIdentity_ExactCreationTicksAndLiveHandleRequired(self):
        module = importlib.import_module('leader_supervisor')
        kernel=Mock()
        kernel.OpenProcess.return_value=42
        kernel.WaitForSingleObject.return_value=258
        def process_times(handle, created, *rest):
            stamp=module.ctypes.cast(created, module.ctypes.POINTER(module.wintypes.FILETIME)).contents
            stamp.dwHighDateTime=0;stamp.dwLowDateTime=123
            return 1
        kernel.GetProcessTimes.side_effect=process_times
        with patch.object(module.ctypes,'WinDLL',return_value=kernel,create=True):
            self.assertTrue(module.owner_alive({'pid':42,'created_ticks':123}))
            self.assertFalse(module.owner_alive({'pid':42,'created_ticks':124}))
            kernel.WaitForSingleObject.return_value=0
            self.assertFalse(module.owner_alive({'pid':42,'created_ticks':123}))
        self.assertEqual(kernel.CloseHandle.call_count,3)

    def test_OwnerLossAfterFirstRenew_RecordsOrphanRefusal(self):
        module=importlib.import_module('leader_supervisor')
        good=dict(leader='mac',state='live',epoch=19,expires_in=300,oid='a'*40)
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            with patch.object(module,'owner_alive',side_effect=[True,False]), patch.object(module,'read_leader',return_value=good), patch.object(module,'renew',return_value={}) as renew, patch.object(module.time,'sleep'):
                with self.assertRaisesRegex(ValueError,'OWNER'):
                    module.supervise(root,root,19,'run',owner={'pid':42,'created_ticks':123})
            renew.assert_called_once()
            result=json.loads((root/'leader-renew-exit.json').read_bytes())
            self.assertEqual(result['exit_code'],5)
            self.assertIn('OWNER',result['result'])

    def test_Cas_SuccessorInsertedAfterRead_IsNeverRenewed(self):
        module = importlib.import_module('leader_supervisor')
        core = Mock()
        core.leader_read.return_value = ({'leader':'mac','epoch':19,'expires_at':1300,'ttl':300}, 'a'*40, None)
        core.leader_state.return_value = 'live'
        core.leader_decide.return_value = ({'leader':'mac','epoch':19,'expires_at':1400}, None)
        core.leader_write.return_value = (None, {'code':'COORD-LEADER-STALE'})
        with patch.object(module, 'load_core', return_value=core), patch.object(module.time, 'time', return_value=1000), patch.object(module,'owner_alive',return_value=True):
            with self.assertRaisesRegex(ValueError, 'CAS'):
                module.renew(Path('.'), 19, {'pid':42,'created_ticks':123})
        self.assertEqual(core.leader_write.call_args.args[-1], 'a'*40)
        self.assertEqual(core.leader_write.call_args.args[1]['epoch'], 19)
        core.leader_read.assert_called_once()
        core.leader_write.reset_mock()
        with patch.object(module,'load_core',return_value=core), patch.object(module.time,'time',return_value=1000), patch.object(module,'owner_alive',return_value=False):
            with self.assertRaisesRegex(ValueError,'OWNER'):
                module.renew(Path('.'),19,{'pid':42,'created_ticks':123})
        core.leader_write.assert_not_called()

    def test_OwnerLossOrReuse_StopsBeforeRenew(self):
        module = importlib.import_module('leader_supervisor')
        for alive in (False,):
            with tempfile.TemporaryDirectory() as tmp:
                root=Path(tmp)
                with patch.object(module, 'owner_alive', return_value=alive), patch.object(module,'renew') as renew:
                    with self.assertRaisesRegex(ValueError,'OWNER'):
                        module.supervise(root, root, 19, 'run', owner={'pid':42,'created_ticks':123})
                renew.assert_not_called()

    def test_Supervisor_FullAdmissionCalledBeforeAnyMutation(self):
        module = importlib.import_module('leader_supervisor')
        args=Mock(repo=Path('.'), contract=Path('contract.json'))
        for reason in ('wrong worktree','wrong branch'):
            with patch.object(module.contract, 'verify', side_effect=ValueError(reason)) as verify:
                with self.assertRaisesRegex(ValueError,reason):
                    module.admit(args, {'inputs':{name:{'path':'fixture'} for name in ('rootfs','ubuntu_sums','ubuntu_signature','openfoam_keyring','nvidia_keyring')}})
                verify.assert_called_once()

    def test_Supervisor_ActualAdmissionRefusesWrongWorktreeAndBranch(self):
        module=importlib.import_module('leader_supervisor')
        from test_f1_attempt2_admission import fixture
        for problem in ('worktree','branch'):
            with self.subTest(problem=problem), tempfile.TemporaryDirectory() as tmp:
                repo=Path(tmp).resolve()
                document=fixture()
                document['target_path']=module.contract.derived_target_path(PureWindowsPath(str(repo)))  # Ruling 237
                path=repo/document['run_root']/'invocation-contract.json'
                path.parent.mkdir(parents=True)
                path.write_text(json.dumps(document),encoding='utf-8')
                output=path.parent/'live';output.mkdir()
                args=argparse.Namespace(repo=repo,contract=path,output=output,branch=document['execution_branch'],session=document['session'])
                def git(repo,*args):
                    return 'wrong' if args[0]=='branch' and problem=='branch' else document['execution_branch'] if args[0]=='branch' else ''
                with patch.object(module.contract,'git',side_effect=git), patch.object(module.contract.subprocess,'run',return_value=Mock(stdout=path.read_bytes())), patch.object(module,'renew') as renew:
                    with self.assertRaisesRegex(ValueError,problem):
                        module.admit(args,document)
                renew.assert_not_called()
    def test_Sample_TwoPhasesAreDistinctAndRedacted(self):
        module = importlib.import_module('leader_supervisor')
        observed = dict(leader='mac', state='live', epoch=19, expires_in=200, oid='a'*40,
                        host='private-host', tree='private-tree')
        with patch.object(module, 'read_leader', return_value=observed):
            first = module.sample(Path('.'), 19, 'run', 'activation')
            second = module.sample(Path('.'), 19, 'run', 'immediately-pre-live')
        self.assertNotEqual(first['event_id'], second['event_id'])
        self.assertEqual((first['phase'], second['phase']), ('activation', 'immediately-pre-live'))
        self.assertNotIn('private', json.dumps([first, second]))

    def test_Renewal_OneRenewThenOwnedStop_RecordsBoundedEvidence(self):
        module = importlib.import_module('leader_supervisor')
        good = dict(leader='mac', state='live', epoch=19, expires_in=300, oid='a'*40)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            def stop_after_wait(seconds):
                (root / 'leader-renew-stop.request').write_text('run\n', encoding='utf-8')
            with patch.object(module, 'owner_alive', return_value=True), patch.object(module, 'read_leader', return_value=good), patch.object(module, 'renew', return_value={'ref_old':'a'*40,'ref_new':'b'*40}) as renew, patch.object(module.time, 'sleep', side_effect=stop_after_wait):
                module.supervise(root, root, 19, 'run', limit=3600)
            renew.assert_called_once()
            rows = [json.loads(line) for line in (root / 'leader-renew-events.jsonl').read_text().splitlines()]
            self.assertEqual([row['event'] for row in rows], ['renew'])
            self.assertEqual(rows[0]['epoch'], 19)
            result = json.loads((root / 'leader-renew-exit.json').read_bytes())
            self.assertEqual(result['exit_code'], 0)
            self.assertEqual(result['run_id'], 'run')

    def test_Renewal_BadLeaseOrWrongStop_FailsWithoutRenew(self):
        module = importlib.import_module('leader_supervisor')
        for mode in ('ttl', 'stop'):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                if mode == 'stop':
                    (root / 'leader-renew-stop.request').write_text('another-run\n', encoding='utf-8')
                good = dict(leader='mac', state='live', epoch=19, expires_in=199, oid='a'*40)
                with patch.object(module, 'owner_alive', return_value=True), patch.object(module, 'read_leader', return_value=good), patch.object(module, 'renew') as renew:
                    with self.assertRaises(ValueError):
                        module.supervise(root, root, 19, 'run', limit=3600)
                renew.assert_not_called()
                if mode == 'ttl':
                    self.assertEqual(json.loads((root / 'leader-renew-exit.json').read_bytes())['exit_code'], 5)
                else:
                    self.assertFalse((root / 'leader-renew-exit.json').exists())

    def test_FinalFreeze_AllLeaseReceiptInputsAreCaptured(self):
        import attempt
        import manifest
        with tempfile.TemporaryDirectory() as tmp:
            live = Path(tmp) / 'live'
            live.mkdir()
            for name in attempt.RENEWAL_EVIDENCE:
                (live / name).write_bytes(b'{}\n' if name.endswith(('.json', '.jsonl')) else b'')
            (live / 'host-identity.json').write_bytes(b'{}\n')  # required by every freeze (Ruling 238 (1))
            frozen = Path(tmp) / 'final'
            manifest.freeze(live, frozen)
            captured = {row['path'] for row in json.loads((frozen / 'capture-manifest.json').read_bytes())['files']}
            self.assertEqual(captured, set(attempt.RENEWAL_EVIDENCE) | {'host-identity.json'})
            for name in captured:
                self.assertEqual((frozen / name).read_bytes(), (live / name).read_bytes())

    def test_Renewal_PreexistingControlOrEvidence_RefusesWithoutMutation(self):
        module = importlib.import_module('leader_supervisor')
        for name in ('leader-renew-stop.request', 'leader-renew-start.json', 'leader-renew-events.jsonl', 'leader-renew-exit.json'):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                (root / name).write_bytes(b'stale')
                before = {path.name: path.read_bytes() for path in root.iterdir()}
                with patch.object(module, 'read_leader', side_effect=AssertionError('no leader read')), patch.object(module, 'renew') as renew:
                    with self.assertRaisesRegex(ValueError, 'PREEXISTING'):
                        module.supervise(root, root, 19, 'run')
                renew.assert_not_called()
                self.assertEqual({path.name:path.read_bytes() for path in root.iterdir()}, before)


if __name__ == '__main__':
    unittest.main()
