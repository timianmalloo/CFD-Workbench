#!/usr/bin/env python3
"""Attempt-2 measured lease samples and bounded, authorization-bound renewal."""
from __future__ import annotations
import argparse
from datetime import datetime, timezone
import ctypes
from ctypes import wintypes
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import uuid
from functools import lru_cache
import attempt
import contract

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, 'reconfigure'):
        try:
            _stream.reconfigure(encoding='utf-8', errors='replace')
        except (ValueError, OSError):
            pass


def read_leader(repo):
    result = subprocess.run([sys.executable, str(repo / 'docs/ai-forward-pack/scripts/coord-core.py'),
                             'leader', 'who', '--json'], cwd=repo, capture_output=True,
                            text=True, encoding='utf-8', check=True, timeout=15)
    return json.loads(result.stdout)


def sample(repo, epoch, run_id, phase):
    observed = read_leader(repo)
    contract.check_live_leader_json(observed, epoch)
    return dict(schema='cfdw-f1-leader-sample/1', event_id=uuid.uuid4().hex,
                utc=datetime.now(timezone.utc).isoformat(), run_id=run_id, phase=phase,
                **{key: observed[key] for key in ('leader', 'state', 'epoch', 'expires_in', 'oid')})


@lru_cache(maxsize=1)
def load_core(repo):
    spec = importlib.util.spec_from_file_location('f1_coord_core', repo / attempt.COORDINATOR)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def renew(repo, epoch, owner):
    core = load_core(repo)
    row, oid, error = core.leader_read(repo)
    now = time.time()
    if error or not row:
        raise ValueError('F1-RENEW-NOT-CHECKED')
    expires_in_before = row['expires_at'] - now   # Ruling 235 (2): the TTL read before the CAS write, from the admitted record
    contract.check_live_leader_json(dict(leader=row['leader'], epoch=row['epoch'],
        state=core.leader_state(row, now), expires_in=expires_in_before, oid=oid), epoch)
    updated, error = core.leader_decide('renew', row, now, 'mac', 'mac', row['ttl'],
                                      host=row.get('host'), tree=row.get('tree'))
    if error:
        raise ValueError('F1-RENEW-REFUSED')
    # Reads can block on Git; recheck ownership immediately before the CAS write.
    if not owner_alive(owner):
        raise ValueError('F1-RENEW-OWNER-LOST-OR-REPLACED')
    new_oid, error = core.leader_write(repo, updated, oid)
    if error:
        raise ValueError('F1-RENEW-CAS-REFUSED')
    return dict(ref_old=oid, ref_new=new_oid, expires_in_before=expires_in_before)


def owner_alive(owner):
    """Query an exact Windows process creation identity without launching a shell."""
    if not owner or type(owner.get('pid')) is not int or type(owner.get('created_ticks')) is not int:
        return False
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    kernel.OpenProcess.restype = wintypes.HANDLE
    kernel.GetProcessTimes.argtypes = [wintypes.HANDLE] + [ctypes.POINTER(wintypes.FILETIME)] * 4
    kernel.WaitForSingleObject.argtypes = [wintypes.HANDLE, wintypes.DWORD]
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    handle = kernel.OpenProcess(0x100000 | 0x1000, False, owner['pid'])
    if not handle:
        return False
    try:
        created, exited, kern, user = (wintypes.FILETIME() for _ in range(4))
        if not kernel.GetProcessTimes(handle, ctypes.byref(created), ctypes.byref(exited), ctypes.byref(kern), ctypes.byref(user)):
            return False
        ticks = (created.dwHighDateTime << 32) | created.dwLowDateTime
        return ticks == owner['created_ticks'] and kernel.WaitForSingleObject(handle, 0) == 258
    finally:
        kernel.CloseHandle(handle)


def admit(args, document):
    values = vars(args).copy()
    values.update(command='verify', runtime_output=args.output)
    # Every input verify reads from its arguments, from the one list contract.py owns (attempt 2 crashed on a hand copy).
    values.update({name: Path(document['inputs'][name]['path']) for name in contract.ARGUMENT_INPUTS})
    contract.verify(argparse.Namespace(**values))
    # Cache the hash-checked mutation implementation once; later filesystem edits
    # cannot replace the implementation on the next renewal tick.
    contract.check_file_hash(document['external_sha256'][attempt.COORDINATOR], args.repo / attempt.COORDINATOR)
    load_core(args.repo)


def write_json(path, value):
    path.write_text(json.dumps(value, sort_keys=True) + '\n', encoding='utf-8', newline='\n')


def supervise(repo, output, epoch, run_id, limit=3600, owner=None):
    controls = ['leader-renew-stop.request', 'leader-renew-start.json',
                'leader-renew-events.jsonl', 'leader-renew-exit.json']
    if any((output / name).exists() for name in controls):
        raise ValueError('F1-RENEW-PREEXISTING-ARTIFACT')
    started = time.monotonic()
    identity = dict(instance_id=uuid.uuid4().hex, pid=os.getpid(), run_id=run_id, epoch=epoch)
    marker = dict(identity, schema='cfdw-f1-renewal-start/1', owner=owner,
                  utc=datetime.now(timezone.utc).isoformat())
    with (output / 'leader-renew-start.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(marker, sort_keys=True) + '\n')
    code = 5
    reason = 'Not recorded'
    try:
        with (output / 'leader-renew-events.jsonl').open('x', encoding='utf-8', newline='\n') as target:
            while time.monotonic() - started < limit:
                if not owner_alive(owner):
                    raise ValueError('F1-RENEW-OWNER-LOST-OR-REPLACED')
                stop = output / 'leader-renew-stop.request'
                if stop.exists():
                    if stop.read_text(encoding='utf-8').strip() != run_id:
                        raise ValueError('F1-RENEW-STOP-IDENTITY')
                    code, reason = 0, 'owned-stop'
                    return
                contract.check_live_leader_json(read_leader(repo), epoch)
                mutation = renew(repo, epoch, owner)
                row = sample(repo, epoch, run_id, 'renewal')
                row.update(identity, event='renew', **mutation)
                target.write(json.dumps(row, sort_keys=True) + '\n')
                target.flush()
                os.fsync(target.fileno())
                # One-second owner/stop cadence; only renew every 60 seconds.
                for _ in range(60):
                    time.sleep(1)
                    if not owner_alive(owner):
                        raise ValueError('F1-RENEW-OWNER-LOST-OR-REPLACED')
                    if stop.exists() or time.monotonic() - started >= limit:
                        break
        raise ValueError('F1-RENEW-DEADLINE')
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        reason = str(error) if isinstance(error, ValueError) else 'F1-RENEW-SYSTEM-ERROR'
        raise
    finally:
        write_json(output / 'leader-renew-exit.json', dict(identity, schema='cfdw-f1-renewal-exit/1',
                   exit_code=code, result=reason,
                   elapsed_seconds=time.monotonic() - started, utc=datetime.now(timezone.utc).isoformat()))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=('sample', 'supervise'))
    parser.add_argument('--repo', required=True, type=Path)
    parser.add_argument('--epoch', required=True, type=int)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--phase', choices=('activation', 'immediately-pre-live'))
    parser.add_argument('--contract', type=Path)
    parser.add_argument('--authorization', type=Path)
    parser.add_argument('--exclusive-run-id')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--branch')
    parser.add_argument('--session')
    parser.add_argument('--owner-pid', type=int)
    parser.add_argument('--owner-created-ticks', type=int)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    if args.mode == 'sample':
        if not args.phase:
            parser.error('sample requires --phase')
        print(json.dumps(sample(repo, args.epoch, args.run_id, args.phase), sort_keys=True))
        return
    if args.contract is None or args.authorization is None or args.output is None:
        parser.error('supervise requires contract, authorization and output')
    raw = args.contract.read_bytes()
    document = json.loads(raw)
    if document.get('attempt') not in attempt.FRESH_ATTEMPTS or args.epoch != document.get('leader_epoch'):
        raise ValueError('F1-RENEW-CONTRACT')
    admit(args, document)
    supervise(repo, args.output, args.epoch, args.run_id,
              owner=dict(pid=args.owner_pid, created_ticks=args.owner_created_ticks))


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, KeyError, subprocess.SubprocessError):
        print('F1-LEADER-SUPERVISOR-FAILED', file=sys.stderr)
        raise SystemExit(5)
