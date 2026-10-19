#!/usr/bin/env python3
"""R225 attempt admission. Proposal verification never grants live authority."""
from __future__ import annotations
import hashlib
import json
from pathlib import Path, PureWindowsPath
import re
import subprocess
import architecture
import contract

PREFIX = 'docs/proof/win-gpu-f1-preflight'
EXTERNAL = PREFIX + '/recovery/check_cache.py'
COORDINATOR = 'docs/ai-forward-pack/scripts/coord-core.py'
PROTECTED_KEY = '{b7566a82-9cee-47ce-a246-f83632353cea}'
OLD_TARGET_KEY = '{e958c4d5-ae78-4b50-a634-7102f334d3aa}'
RETIRED_TARGET_KEYS = [OLD_TARGET_KEY, '{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}']
KEEPALIVE_COMMAND = r'"C:\WINDOWS\System32\wsl.exe" -d cfdw-openfoam2512 -u root -- /usr/bin/sleep infinity'
KEEPALIVE = dict(pid=19296, parent_pid=15528, name='wsl.exe', child_pid=3524, child_parent_pid=19296,
    created_local='2026-10-08T10:02:23.1776050-07:00',
    child_name='wsl.exe', child_created_local='2026-10-08T10:02:23.2168250-07:00',
    command=KEEPALIVE_COMMAND, child_command=KEEPALIVE_COMMAND,
    protected_key=PROTECTED_KEY)
RENEWAL_EVIDENCE = ['leader-activation.json', 'leader-immediately-pre-live.json',
    'leader-renew-start.json', 'leader-renew-events.jsonl', 'leader-renew.stderr.txt', 'leader-renew-exit.json',
    'keepalive-activation.json', 'keepalive-immediately-pre-live.json', 'keepalive-finalization.json']
PROTECTED_PATH = r'%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512'
INPUT_HASHES = {
    'ubuntu_sums': '337176fdeda3dbbf7bd36146894c03670d5ed8cffc2bd804f39d4c4ace0d424f',
    'ubuntu_signature': 'c0deb277ccda24b8fcc2d36bf9d9530c0aed7138f30375dc45b6539a6aa6799b',
    'openfoam_keyring': '6d17d37b82fc840b8a5c767545703df7264cfa3e30f9cf74556ebb124bda1a93',
    'nvidia_keyring': '25100d6f2eccaee7d6719a70e3c0c6145064aa9edc2c32148f693ac4c524a376',
    'b2_licenses': '470de11258ff4e78a05a38cb3d1c75090387c79b2cc3b06fc3534e9eee5da4d6',
}


def input_hashes(token):
    """The six reviewed input hashes for one architecture: the rootfs pin follows the token (Ruling 237)."""
    return dict(INPUT_HASHES, rootfs=architecture.row(token)['rootfs_sha256'])
SIGNERS = {
    'ubuntu_rootfs': ['D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81', '4A3CE3CD565D7EB5C810E2B97FF3F408476CF100'],
    'ubuntu_archive': ['790BC7277767219C42C86F933B4FE6ACC0B21F32', '843938DF228D22F7B3742BC0D94AA3F0EFE21092', 'F6ECB3762474EDA9D21B7022871920D1991BC93C'],
    'openfoam': [contract.OPENFOAM], 'nvidia': [contract.NVIDIA],
}
LIMITS_FIXED = dict(deadline_seconds=3600, work_ceiling_seconds=2700, checkpoint_ceiling_seconds=3000,
                    vhd_max_bytes=8589934592, target_cpus=[0, 1], protected_cpus=[0], nice=10)


def host_limits(document):
    """Ruling 238 (3): the free-space floor and reserve come from the contract's worksheet fields, never constants."""
    floors = {}
    for key, name in (('host_initial_free_bytes', 'host_start_floor_bytes'), ('host_reserve_bytes', 'host_reserve_bytes')):
        value = document.get(name)
        if type(value) is not int or value <= 0:
            raise ValueError('contract host floors must be positive integers: ' + name)
        floors[key] = value
    return dict(LIMITS_FIXED, **floors)
STAGES = ['probe', 'repositories', 'simulate', 'download', 'inspect', 'delete-debs']
ACTIONS = ['fresh-target-create', 'rootfs-signature', 'completed-l3-before-watch-after',
           'owned-stop-check', 'terminate-target', 'unregister-target', 'absence-proof',
           'manifest-freeze-verify', 'evidence-commit']
FORBIDDEN = ['dpkg-install', 'apt-install', 'petsc-source-fetch', 'petsc4foam-source-fetch',
             'build', 'case-run', 'benchmark', 'protected-lifecycle', 'protected-file-change']


def check_binding(document, run_id=None):
    number = document.get('attempt')
    if type(number) is not int or number not in (1, 2):
        raise ValueError('only reviewed attempt 1 or 2 is admitted')
    expected = dict(leader='mac', leader_epoch=8, execution_branch=f'win/gpu-f1-r225-attempt{number}',
                    session=f'pc-f1-r225-attempt{number}', run_root=f'{PREFIX}/attempt-{number}')
    pattern = f'r225-attempt{number}-epoch8-[0-9]{{8}}'
    if number == 2:
        epoch = document.get('leader_epoch')
        if type(epoch) is not int or epoch <= 0:
            raise ValueError('attempt-2 epoch must be a positive integer')
        expected.update(leader_epoch=epoch, execution_branch='win/gpu-f1-attempt2', session='pc-f1-attempt2')
        pattern = r'f1-attempt2-[0-9]{8}'
        worktree = document.get('execution_worktree', '')
        if (not isinstance(worktree, str) or not PureWindowsPath(worktree).is_absolute() or
                str(PureWindowsPath(worktree)) != worktree or '..' in PureWindowsPath(worktree).parts or
                any(ord(char) < 32 for char in worktree)):
            raise ValueError('attempt-2 execution worktree must be explicit and canonical')
        if worktree.lower().endswith('cfd-workbench-win-gpu-f1-r225-attempt1'):
            raise ValueError('attempt-1 execution worktree is retired')
        path = str(PureWindowsPath(worktree) / 'docs/proof/win-gpu-b2/license-map.json')
        if document.get('inputs', {}).get('b2_licenses', {}).get('path') != path:
            raise ValueError('attempt-2 B2 input path differs from execution worktree')
    for key, value in expected.items():
        if document.get(key) != value:
            raise ValueError('attempt binding changed: ' + key)
    if not re.fullmatch(pattern, document.get('run_id', '')):
        raise ValueError('fresh attempt run identity required')
    if run_id is not None and run_id != document['run_id']:
        raise ValueError('invoked RunId differs from reviewed attempt')
    hashes = input_hashes(document.get('architecture'))
    if set(document.get('inputs', {})) != set(hashes):
        raise ValueError('exact six reviewed inputs required')
    for key, value in hashes.items():
        if document['inputs'][key].get('sha256') != value:
            raise ValueError('accepted input hash changed: ' + key)
    if document.get('allowed_signers') != SIGNERS:
        raise ValueError('exact accepted signer allowlists required')
    external = {EXTERNAL, COORDINATOR} if number == 2 else {EXTERNAL}
    if set(document.get('external_sha256', {})) != external:
        raise ValueError('external deletion helper binding required')
    if any(not contract.SHA.fullmatch(value) for value in document['external_sha256'].values()):
        raise ValueError('external helper digest invalid')


def authorization(document, contract_sha, accepted=False, ruling=None):
    number = document['attempt']
    value = dict(schema='cfdw-f1-preflight-authorization/1', status='accepted' if accepted else 'proposed',
                requested_ruling=228, ruling=228 if accepted else None,
                attempt=number, leader='mac', leader_epoch=8, source_commit=document['accepted_packet_commit'],
                run_id=document['run_id'], session=document['session'], execution_branch=document['execution_branch'],
                run_root=document['run_root'], contract_path=document['run_root'] + '/invocation-contract.json',
                contract_sha256=contract_sha, target=document['target'], target_path=document['target_path'],
                target_previous_key=OLD_TARGET_KEY, target_key='observed-after-fresh-creation',
                protected=document['protected'], protected_key=PROTECTED_KEY, protected_path=PROTECTED_PATH,
                packet_sha256=document['packet_sha256'], external_sha256=document['external_sha256'],
                inputs=document['inputs'], allowed_signers=document['allowed_signers'], limits=host_limits(document),
                allowed_stages=STAGES, allowed_actions=ACTIONS, forbidden_actions=FORBIDDEN,
                attempt_count=1, failure_policy='stop; immutable failure receipt; no next attempt without its own ruling',
                prerequisites=['live-mac-epoch8', 'exclusive-PC', 'continuous-PC-side-leader-renewal',
                               'target-name-registration-path-absent-before-creation', 'bind-new-GUID-for-rollback'])
    if number == 2:
        if type(ruling) is not int or ruling <= 0:
            raise ValueError('attempt-2 authorization ruling must be a positive integer')
        value.update(requested_ruling=ruling, ruling=ruling if accepted else None,
                     leader_epoch=document['leader_epoch'], execution_worktree=document['execution_worktree'],
                     target_previous_keys=RETIRED_TARGET_KEYS, keepalive=KEEPALIVE,
                     minimum_leader_ttl_seconds=200, renewal_evidence=RENEWAL_EVIDENCE)
        del value['target_previous_key']
        value['prerequisites'][0] = 'live-mac-contract-epoch-two-fresh-samples-at-least-200s'
    return value


def check_authorization(value, document, contract_sha, proposed=False):
    if value.get('status') not in ('proposed', 'accepted'):
        raise ValueError('unknown attempt authorization status')
    accepted = value.get('status') == 'accepted'
    if not accepted and not proposed:
        raise ValueError('proposed authorization cannot admit live work')
    ruling = value.get('ruling') if accepted else value.get('requested_ruling')
    if value != authorization(document, contract_sha, accepted=accepted, ruling=ruling):
        raise ValueError('attempt authorization source/run/hash/scope differs from exact contract')


def check_ruling(text, value, sha):
    number = value['ruling']
    if type(number) is not int or number <= 0:
        raise ValueError('accepted ruling must be a positive integer')
    match = re.search(rf'^### Ruling {number} [^\n]*\n(.*?)(?=^### Ruling |\Z)', text, re.M | re.S)
    if not match:
        raise ValueError('accepted attempt ruling missing on main')
    body = match.group(1)
    required = ['ACCEPT ONE PREFLIGHT ATTEMPT', value['contract_path'], value['contract_sha256'],
                value['source_commit'], value['run_id'], value['session'], value['execution_branch'],
                f"epoch {value['leader_epoch']}", sha, value['run_root'] + '/attempt-authorization.json']
    exact_epoch = re.search(rf"\bepoch {value['leader_epoch']}(?![0-9.])\b", body)
    if any(token not in body for token in required) or re.search(r'\bPROPOSED\b', body) or not exact_epoch:
        raise ValueError('main ruling does not accept exact attempt bytes/bindings')


def verify_authorization(repo, path, document, contract_sha, run_id, exclusive, proposed=False):
    if run_id is None:
        raise ValueError('explicit invoked RunId is required')
    check_binding(document, run_id)
    expected_path = document['run_root'] + '/attempt-authorization.json'
    if path is None or path.resolve(strict=True).relative_to(repo).as_posix() != expected_path:
        raise ValueError('attempt authorization is absent or outside exact live path')
    raw = path.read_bytes()
    value = json.loads(raw)
    check_authorization(value, document, contract_sha, proposed=proposed)
    if proposed:
        return
    if exclusive != run_id:
        raise ValueError('exclusive-PC attestation must bind exact RunId')
    committed = subprocess.run(['git', '-C', str(repo), 'show', 'HEAD:' + expected_path],
                               check=True, capture_output=True, timeout=15).stdout
    if committed != raw:
        raise ValueError('authorization bytes differ from committed live blob')
    main = contract.git(repo, 'show', 'origin/main:docs/notes/rulings.md')
    check_ruling(main, value, hashlib.sha256(raw).hexdigest())
