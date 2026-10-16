"""Offline-fast real-Git producer tests; synthetic input bytes, no live authority."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'docs/proof/win-gpu-f1-preflight/packet'))
import attempt
import contract


class PreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.repo=Path(self.temp.name).resolve()
        shutil.copytree(ROOT/contract.PACKET,self.repo/contract.PACKET,ignore=shutil.ignore_patterns('__pycache__'))
        (self.repo/'.gitattributes').write_bytes(b'* -text\n')
        for name in (attempt.EXTERNAL,attempt.COORDINATOR,'docs/proof/win-gpu-b2/package-lock.json','docs/proof/win-gpu-b2/license-map.json'):
            dest=self.repo/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/name,dest)
        output=self.repo/attempt.PREFIX/'attempt-2'
        output.mkdir();(output/'.keep').write_bytes(b'fixture\n')
        inputs=self.repo/'inputs';inputs.mkdir()
        values={}
        hashes={}
        for name in ('rootfs','ubuntu_sums','ubuntu_signature','openfoam_keyring','nvidia_keyring'):
            path=inputs/name;path.write_bytes(name.encode())
            values[name]=path;hashes[name]=hashlib.sha256(path.read_bytes()).hexdigest()
        self.addCleanup(patch.stopall)
        patch.dict(attempt.INPUT_HASHES,hashes).start()
        patch.object(contract,'ROOTFS',hashes['rootfs']).start()
        self.git('init','-q')
        self.git('add','.')
        self.git('-c','user.name=Offline Fixture','-c','user.email=fixture@example.invalid','commit','-qm','source')
        self.args=argparse.Namespace(command='prepare',repo=self.repo,contract=output/'invocation-contract.json',
            authorization=output/'attempt-authorization.json',source_commit=self.git('rev-parse','HEAD'),
            execution_worktree=r'C:\Projects\future-f1-fixture',leader_epoch=8,requested_ruling=235,
            branch='win/gpu-f1-attempt2',session='pc-f1-attempt2',run_id='f1-attempt2-20261013',**values)

    def git(self,*args):
        return subprocess.check_output(['git','-C',str(self.repo),*args],stderr=subprocess.PIPE,text=True).strip()

    def outputs(self):
        return {path.name:path.read_bytes() for path in (self.args.contract,self.args.authorization) if path.exists()}

    def test_Prepare_ExactSelfVerifyingPair_ProjectsOnlyB2AndNeverCreatesFutureTree(self):
        contract.prepare(self.args)
        document=json.loads(self.args.contract.read_bytes())
        digest=hashlib.sha256(self.args.contract.read_bytes()).hexdigest()
        auth=json.loads(self.args.authorization.read_bytes())
        self.assertEqual(auth,attempt.authorization(document,digest,ruling=235))
        self.assertEqual(document['accepted_packet_commit'],self.args.source_commit)
        contract.check_contract_fields(document)
        attempt.check_authorization(auth,document,digest,proposed=True)
        with self.assertRaises(ValueError):attempt.check_authorization(auth,document,digest)
        self.assertEqual(document['inputs']['rootfs']['path'],str(self.args.rootfs))
        self.assertEqual(document['inputs']['b2_lock']['path'],r'C:\Projects\future-f1-fixture\docs\proof\win-gpu-b2\package-lock.json')
        self.assertFalse(Path(self.args.execution_worktree).exists())
        self.assertEqual(set(self.outputs()),{'invocation-contract.json','attempt-authorization.json'})

    def test_Prepare_WrongDirtyNonHeadSource_RefusesWithoutOutputs(self):
        original=self.args.source_commit
        self.args.source_commit='c'*40
        with self.assertRaises(ValueError):contract.prepare(self.args)
        self.assertEqual(self.outputs(),{})
        self.args.source_commit=original
        dirty=self.repo/'dirty';dirty.write_text('dirty',encoding='utf-8')
        with self.assertRaises(ValueError):contract.prepare(self.args)
        self.assertEqual(self.outputs(),{})
        dirty.unlink()
        self.git('-c','user.name=Offline Fixture','-c','user.email=fixture@example.invalid','commit','--allow-empty','-qm','later')
        with self.assertRaises(ValueError):contract.prepare(self.args)
        self.assertEqual(self.outputs(),{})

    def test_Prepare_ExistingOutputOrTraversal_RefusesWithoutNewOutput(self):
        self.args.authorization.write_bytes(b'preserve')
        before=self.outputs()
        with self.assertRaises(ValueError):contract.prepare(self.args)
        self.assertEqual(self.outputs(),before)
        self.args.authorization.unlink()
        self.args.execution_worktree=r'C:\Projects\new\..\old'
        with self.assertRaises(ValueError):contract.prepare(self.args)
        self.assertEqual(self.outputs(),{})

    def test_Prepare_InvalidEpochOrRuling_RefusesWithoutOutputs(self):
        for name in ('leader_epoch','requested_ruling'):
            original=getattr(self.args,name)
            for value in (None,True,0,-1,'8',8.0):
                with self.subTest(name=name,value=value):
                    setattr(self.args,name,value)
                    with self.assertRaises(ValueError):contract.prepare(self.args)
                    self.assertEqual(self.outputs(),{})
            setattr(self.args,name,original)

    def test_Prepare_SourceBlobMismatch_RefusesEvenIfStatusLooksClean(self):
        path=self.repo/contract.PACKET/'Control.ps1';path.write_bytes(path.read_bytes()+b'changed')
        real_git=contract.git
        def hide_status(repo,*args):return '' if args[0]=='status' else real_git(repo,*args)
        with patch.object(contract,'git',side_effect=hide_status):
            with self.assertRaises(ValueError):contract.prepare(self.args)
        self.assertEqual(self.outputs(),{})


if __name__=='__main__':unittest.main()
