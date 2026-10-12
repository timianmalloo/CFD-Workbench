#!/usr/bin/env python3
"""Proposed cleanup-only recovery. No execution without a new committed Mac ruling."""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import importlib.util
import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True

TARGET = 'cfdw-petsc-cuda132-f1'
PROTECTED = 'cfdw-openfoam2512'
TARGET_PATH = r'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
PROTECTED_PATH = r'%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512'
TARGET_KEY = '{e958c4d5-ae78-4b50-a634-7102f334d3aa}'
PROTECTED_KEY = '{b7566a82-9cee-47ce-a246-f83632353cea}'
RUN_ID = 'r217-run1-epoch6-20261012'
FREEZE_SHA = '76ab6e220a21c10c694a7eecd9d00679d1a0a42b3444598dae2682ac627f0bea'
ORIGINAL = Path(r'C:\Projects\CFD-Workbench-win-gpu-f1-r217-execute-r220')
REL = Path('docs/proof/win-gpu-f1-preflight')


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_helper(packet, name):
    spec = importlib.util.spec_from_file_location('recovery_' + name, packet / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def authorization_example(commit, session, epoch):
    return dict(schema='cfdw-f1-cleanup-authorization/1', status='accepted', leader='mac',
                ruling=221, source_commit=commit, session=session, leader_epoch=epoch,
                freeze_sha256=FREEZE_SHA, target=TARGET, target_path=TARGET_PATH,
                target_key=TARGET_KEY, protected=PROTECTED, protected_path=PROTECTED_PATH,
                protected_key=PROTECTED_KEY, original_run_id=RUN_ID,
                allowed_actions=['delete-debs', 'terminate-target', 'unregister-target'],
                deadline_seconds=600)


def validate_authorization(value, commit, session, epoch):
    expected = authorization_example(commit, session, epoch)
    if type(value.get('ruling')) is not int or value['ruling'] <= 220:
        raise ValueError('new numbered Mac cleanup ruling required')
    expected['ruling'] = value['ruling']
    if value != expected:
        raise ValueError('cleanup authorization differs from exact reviewed contract')


def validate_freeze(root):
    if digest(root / 'capture-manifest.json') != FREEZE_SHA:
        raise ValueError('original freeze manifest changed')
    document = json.loads((root / 'capture-manifest.json').read_bytes())
    entries = document['files']
    if document['schema'] != 'cfdw-f1-r217-capture-manifest/1' or len(entries) != 75:
        raise ValueError('original 75-file freeze required')
    actual = set()
    for path in root.rglob('*'):
        if path.is_symlink() or path.is_junction():
            raise ValueError('linked freeze evidence')
        if path.is_file():
            actual.add(path.relative_to(root).as_posix())
    if actual != {e['path'] for e in entries} | {'capture-manifest.json'}:
        raise ValueError('freeze file set changed')
    for entry in entries:
        name = Path(entry['path'])
        if name.is_absolute() or '..' in name.parts:
            raise ValueError('unsafe manifest path')
        path = root / name
        if path.stat().st_size != entry['bytes'] or digest(path) != entry['sha256']:
            raise ValueError('freeze payload changed: ' + entry['path'])


def normalize(path):
    return os.path.expandvars(path).removeprefix('\\\\?\\').rstrip('\\').casefold()


def public_registrations(rows):
    return [dict(row, path=PROTECTED_PATH if row['name'] == PROTECTED else TARGET_PATH)
            for row in rows if row['name'] in (TARGET, PROTECTED)]


def public_value(value):
    if isinstance(value, str):
        profile = os.environ.get('USERPROFILE')
        if profile:
            value = re.sub(re.escape(profile), lambda match: '%USERPROFILE%', value, flags=re.IGNORECASE)
        return re.sub(r'S-1-5-21-\d+-\d+-\d+-\d+', 'S-1-5-21-<machine>-<RID>', value)
    if isinstance(value, list):
        return [public_value(item) for item in value]
    if isinstance(value, dict):
        return {key: public_value(item) for key, item in value.items()}
    return value


def validate_identity(rows, names):
    result = []
    for name, path in ((TARGET, TARGET_PATH), (PROTECTED, PROTECTED_PATH)):
        found = [row for row in rows if row['name'] == name]
        if names.count(name) != 1 or len(found) != 1 or found[0]['version'] != 2:
            raise ValueError('ambiguous/missing exact WSL identity: ' + name)
        if normalize(found[0]['path']) != normalize(path):
            raise ValueError('WSL registration path mismatch: ' + name)
        result.append(found[0])
    if result[0]['key'] == result[1]['key']:
        raise ValueError('shared registry identity')
    return tuple(result)


def validate_absence(rows, names, path_present, protected):
    if any(row['name'] == TARGET for row in rows) or TARGET in names or path_present:
        raise ValueError('target registration/list/path absence not proved; no manual path deletion')
    if [row for row in rows if row['name'] == PROTECTED] != [protected] or names.count(PROTECTED) != 1:
        raise ValueError('protected registry identity changed')


def cleanup_sequence(action):
    action('verify-checkpoint')
    action('fresh-identity')
    action('old-owned-check')
    action('guard-ready')
    try:
        action('delete-debs')
    finally:
        action('final-owned-check')
    action('fresh-identity')
    action('terminate')
    action('fresh-identity')
    action('unregister')
    action('absence')
    action('protected-invariance')


class Recovery:
    def __init__(self, repo, authorization, session, epoch):
        self.repo, self.authorization = repo, authorization
        self.session, self.epoch = session, epoch
        self.packet = repo / REL / 'packet'
        self.live = repo / REL / 'recovery-1/live'
        self.freeze = repo / REL / 'run-1/checkpoint'
        self.old_live = ORIGINAL / REL / 'run-1/live'
        self.guard = None
        self.start = time.monotonic()
        self.log = []
        self.index = 0
        self.before = None
        self.errors = []
        self.identity = None
        self.manifest = load_helper(self.packet, 'manifest')
        self.completed = load_helper(self.packet, 'completed_guard')
        self.contract = load_helper(self.packet, 'contract')

    def remaining(self, cap):
        remaining = min(cap, 600 - (time.monotonic() - self.start))
        if remaining <= 0:
            raise ValueError('600-second recovery deadline exhausted; stop for Mac')
        return remaining

    def command(self, argv, label, cap=30, guarded=False):
        # Every WSL command runs under taskset/nice/setsid or is an exact lifecycle/list command.
        if guarded:
            self.guard_ok()
        self.index += 1
        prefix = self.live / f'{self.index:03d}-{label}'
        started = time.monotonic()
        with prefix.with_suffix('.stdout.txt').open('wb') as out, prefix.with_suffix('.stderr.txt').open('wb') as err:
            child = subprocess.Popen(argv, cwd=self.repo, stdout=out, stderr=err,
                                     creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            try:
                end = started + self.remaining(cap)
                while child.poll() is None:
                    if time.monotonic() >= end:
                        raise ValueError(label + ' timed out; Linux state remains unknown')
                    if guarded:
                        self.guard_ok()
                    time.sleep(0.1)
                if child.returncode != 0:
                    raise ValueError(f'{label} exited {child.returncode}')
            finally:
                if child.poll() is None:
                    child.kill()
                    child.wait(timeout=5)
                self.log.append(dict(label=label, argv=argv, exit=child.returncode,
                                     elapsed_seconds=time.monotonic() - started))
        return prefix.with_suffix('.stdout.txt').read_bytes()

    def linux_path(self, path):
        text = str(path.resolve())
        if not re.fullmatch(r'C:\\[^\s]+', text) or path.is_symlink() or path.is_junction():
            raise ValueError('only exact nonlinked C: paths without whitespace accepted')
        return '/mnt/c/' + text[3:].replace('\\', '/')

    def linux_argv(self, distro, label, command):
        return ['wsl.exe', '--distribution', distro, '--user', 'root', '--exec',
                '/usr/bin/taskset', '--cpu-list', '0' if distro == PROTECTED else '0-1',
                '/usr/bin/nice', '-n', '10', '/usr/bin/setsid', '--wait', '/bin/bash',
                self.linux_path(self.packet / 'identity.sh'),
                self.linux_path(self.live / (label + '.entry.txt')), '--'] + command

    def linux(self, distro, label, command, cap=30, guarded=False):
        return self.command(self.linux_argv(distro, label, command), label, cap, guarded)

    def registrations(self):
        import winreg
        rows = []
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r'Software\Microsoft\Windows\CurrentVersion\Lxss') as key:
            for index in range(winreg.QueryInfoKey(key)[0]):
                name = winreg.EnumKey(key, index)
                with winreg.OpenKey(key, name) as subkey:
                    rows.append(dict(key=name, name=winreg.QueryValueEx(subkey, 'DistributionName')[0],
                                     path=winreg.QueryValueEx(subkey, 'BasePath')[0],
                                     version=winreg.QueryValueEx(subkey, 'Version')[0]))
        return rows

    def names(self):
        raw = self.command(['wsl.exe', '--list', '--quiet'], 'distro-list', 15)
        return [line.strip().lstrip('\ufeff') for line in raw.decode('utf-16-le').splitlines() if line.strip()]

    def fresh_identity(self):
        rows, names = self.registrations(), self.names()
        observed = validate_identity(rows, names)
        if observed[0]['key'] != TARGET_KEY or observed[1]['key'] != PROTECTED_KEY:
            raise ValueError('registry GUID differs from retained offline identity')
        if self.identity is not None and observed != self.identity:
            raise ValueError('registration changed during recovery')
        self.identity = observed
        for path in (Path(TARGET_PATH), Path(os.path.expandvars(PROTECTED_PATH))):
            if not path.is_dir() or any(parent.is_symlink() or parent.is_junction()
                                        for parent in (path, *path.parents)):
                raise ValueError('missing or linked distro path')
        self.write('registration-before.json', dict(registrations=public_registrations(rows), names=names))

    def write(self, name, value):
        (self.live / name).write_text(json.dumps(public_value(value), indent=2, sort_keys=True) + '\n', encoding='utf-8', newline='\n')

    def owned(self, root, run_id, mode='check', kind='stage'):
        self.linux(PROTECTED if kind == 'guard' else TARGET, f'owned-{kind}-{mode}',
                   ['/bin/bash', self.linux_path(self.packet / 'owned-control.sh'), '--mode', mode,
                    '--run-root', self.linux_path(root), run_id] + (['--kind', 'guard'] if kind == 'guard' else []), 15)

    def guard_ok(self):
        if self.guard is None or self.guard.poll() is not None:
            raise ValueError('fresh completed guard exited/missing')
        state = json.loads((self.live / 'completed-guard.state.json').read_bytes())
        raw = (self.live / 'completed-guard-events.jsonl').read_bytes()
        complete = raw[:raw.rfind(b'\n') + 1]
        if not complete:
            raise ValueError('fresh guard has no complete event')
        event = json.loads(complete.splitlines()[-1])
        for value in (state, event):
            if value['session_id'] != self.session or value['expected_sha256'] != self.before:
                raise ValueError('guard binding changed')
        if event['sample_number'] != state['completed_samples'] or event['utc'] != state['last_sample_utc']:
            raise ValueError('guard event/state disagree')
        age = (dt.datetime.now(dt.timezone.utc) - dt.datetime.fromisoformat(state['last_sample_utc'].replace('Z', '+00:00'))).total_seconds()
        if state['state'] != 'running' or event['verdict'] != 'continue' or not -5 <= age <= 12:
            raise ValueError('guard stopped/stale')
        return state['completed_samples']

    def snapshot(self, name):
        self.linux(PROTECTED, name, ['/usr/bin/python3', self.linux_path(self.packet / 'completed_guard.py'),
                   'snapshot', '--output', self.linux_path(self.live / (name + '.json')), '--include-log-sha256'], 60)
        self.completed.validate_terminal(json.loads((self.live / (name + '.json')).read_bytes()), require_log_sha256=True)
        return digest(self.live / (name + '.json'))

    def start_guard(self):
        self.before = self.snapshot('l3-before')
        if self.before != digest(self.freeze / 'l3-terminal-before.json'):
            raise ValueError('historical protected terminal identity changed')
        command = ['/usr/bin/python3', self.linux_path(self.packet / 'completed_guard.py'), 'watch',
                   '--expected', self.linux_path(self.live / 'l3-before.json'), '--expected-sha256', self.before,
                   '--events', self.linux_path(self.live / 'completed-guard-events.jsonl'),
                   '--state', self.linux_path(self.live / 'completed-guard.state.json'),
                   '--stop-request', self.linux_path(self.live / 'completed-guard-stop.request'),
                   '--session-id', self.session, '--interval-seconds', '5', '--max-samples', '132']
        argv = self.linux_argv(PROTECTED, 'guard', command)
        # Packet guard ownership uses this exact canonical filename.
        argv[argv.index('--') - 1] = self.linux_path(self.live / 'guard.owner.txt')
        self.guard_out = (self.live / 'guard.stdout.txt').open('wb')
        self.guard_err = (self.live / 'guard.stderr.txt').open('wb')
        self.guard = subprocess.Popen(argv, stdout=self.guard_out, stderr=self.guard_err,
                                      creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        end = time.monotonic() + self.remaining(20)
        while not (self.live / 'completed-guard.state.json').exists():
            if self.guard.poll() is not None or time.monotonic() >= end:
                raise ValueError('fresh guard startup failed')
            time.sleep(0.1)
        self.guard_ok()

    def delete_debs(self):
        self.linux(TARGET, 'cache-admission', ['/usr/bin/python3',
                   self.linux_path(self.repo / REL / 'recovery/check_cache.py')], 15, True)
        contract = json.loads((self.repo / REL / 'run-1/invocation-contract.json').read_bytes())
        args = ['/bin/bash', self.linux_path(self.packet / 'preflight.sh'), '--stage', 'delete-debs',
                '--run-root', self.linux_path(self.live), '--packet-root', self.linux_path(self.packet),
                '--b2-lock', contract['inputs']['b2_lock']['path'],
                '--openfoam-key', contract['inputs']['openfoam_keyring']['path'],
                '--nvidia-key', contract['inputs']['nvidia_keyring']['path'],
                '--openfoam-fingerprint', 'DC93C096174122E256DA24063386DD74948D208F', '--run-id', RUN_ID,
                '--contract', self.linux_path(self.repo / REL / 'run-1/invocation-contract.json'),
                '--contract-sha256', digest(self.repo / REL / 'run-1/invocation-contract.json')]
        self.linux(TARGET, 'delete-debs', args, 120, True)

    def action(self, name):
        if name == 'verify-checkpoint':
            validate_freeze(self.freeze)
            validate_freeze(ORIGINAL / 'Save-Checkpoint')
            self.verify_commit(self.freeze, self.head)
            retained = self.repo / REL / 'run-1/failed-live'
            self.verify_commit(retained, self.head)
            entries = json.loads((retained / 'capture-manifest.json').read_bytes())['files']
            actual = {path.relative_to(self.old_live).as_posix() for path in self.old_live.rglob('*') if path.is_file()}
            if actual != {entry['path'] for entry in entries}:
                raise ValueError('original live evidence file set changed')
            for entry in entries:
                path = self.old_live / entry['path']
                if path.is_symlink() or path.is_junction() or self.manifest.hash_file(path) != (entry['bytes'], entry['sha256']):
                    raise ValueError('original live evidence changed')
        elif name == 'fresh-identity':
            self.fresh_identity()
            if self.guard is not None:
                self.guard_ok()
        elif name == 'old-owned-check':
            self.owned(self.old_live, RUN_ID)
            self.owned(self.old_live, RUN_ID, kind='guard')
        elif name == 'guard-ready':
            self.start_guard()
        elif name == 'delete-debs':
            self.delete_debs()
        elif name == 'final-owned-check':
            self.fresh_identity()
            self.owned(self.live, RUN_ID, 'stop')
            self.owned(self.live, RUN_ID)
            self.owned(self.old_live, RUN_ID)
        elif name in ('terminate', 'unregister'):
            self.command(['wsl.exe', '--' + name, TARGET], name, 30 if name == 'terminate' else 120, True)
        elif name == 'absence':
            rows, names = self.registrations(), self.names()
            validate_absence(rows, names, os.path.lexists(TARGET_PATH), self.identity[1])
            self.write('registration-after.json', dict(registrations=public_registrations(rows), names=names, target_absent=True))
        elif name == 'protected-invariance':
            count = self.guard_ok()
            end = time.monotonic() + self.remaining(12)
            while self.guard_ok() <= count:
                if time.monotonic() >= end:
                    raise ValueError('no fresh post-unregister guard sample')
                time.sleep(0.1)
            if self.snapshot('l3-after') != self.before:
                raise ValueError('protected terminal changed')
            self.guard_ok()
        else:
            raise ValueError('unknown recovery action')

    def verify_commit(self, frozen, commit):
        self.command([sys.executable, str(self.packet / 'manifest.py'), 'verify',
                      '--repo', str(self.repo), '--frozen', str(frozen), '--commit', commit], 'manifest-verify', 120)

    def stop_guard(self):
        if self.guard is None:
            return
        try:
            (self.live / 'completed-guard-stop.request').write_text(self.session + '\n', encoding='ascii')
            self.guard.wait(timeout=self.remaining(20))
        finally:
            if self.guard.poll() is None:
                self.guard.kill()
                self.guard.wait(timeout=5)
            try:
                self.owned(self.live, self.session, 'stop', 'guard')
                self.owned(self.live, self.session, kind='guard')
            finally:
                self.guard_out.close()
                self.guard_err.close()
        if self.guard.returncode != 0:
            raise ValueError('guard nonzero exit retained')

    def precheck(self):
        self.head = self.command(['git', 'rev-parse', 'HEAD'], 'head').decode().strip()
        if self.command(['git', 'status', '--porcelain'], 'clean').strip():
            raise ValueError('worktree dirty; refusal before WSL')
        value = json.loads(self.authorization.read_bytes())
        source = value.get('source_commit', '')
        if not re.fullmatch('[0-9a-f]{40}', source):
            raise ValueError('reviewed source commit absent')
        validate_authorization(value, source, self.session, self.epoch)
        self.command(['git', 'merge-base', '--is-ancestor', source, self.head], 'source-ancestor')
        auth_blob = self.command(['git', 'cat-file', 'blob', self.head + ':' + self.authorization.relative_to(self.repo).as_posix()], 'authorization-blob')
        if auth_blob != self.authorization.read_bytes():
            raise ValueError('authorization not committed byte-for-byte')
        ruling = (self.repo / 'docs/notes/rulings.md').read_text(encoding='utf-8')
        match = re.search(r'(?ms)^### Ruling ' + str(value['ruling']) + r'\b.*?(?=^### Ruling |\Z)', ruling)
        if match is None or 'F1-CLEANUP-ONLY' not in match[0] or digest(self.authorization) not in match[0]:
            raise ValueError('committed numbered Mac ruling must bind authorization hash')
        if self.command(['git', 'cat-file', 'blob', self.head + ':docs/notes/rulings.md'], 'ruling-blob') != (self.repo / 'docs/notes/rulings.md').read_bytes():
            raise ValueError('ruling uncommitted')
        for root in (self.packet, self.repo / REL / 'recovery'):
            for path in root.rglob('*'):
                if path.is_file() and '__pycache__' not in path.parts:
                    relative = path.relative_to(self.repo).as_posix()
                    if self.command(['git', 'cat-file', 'blob', source + ':' + relative], 'source-blob') != path.read_bytes():
                        raise ValueError('reviewed executable source changed: ' + relative)
        leader = self.command([sys.executable, 'docs/ai-forward-pack/scripts/coord-core.py', 'leader', 'who'], 'leader').decode()
        observed = dict(line.split(None, 1) for line in leader.splitlines() if len(line.split(None, 1)) == 2)
        self.contract.check_live_leader(observed, self.epoch)
        self.authorization_value = value

    def run(self):
        if os.name != 'nt':
            raise ValueError('Windows executor required')
        self.live.mkdir(parents=True, exist_ok=False)
        success = False
        try:
            self.precheck()
            cleanup_sequence(self.action)
            success = True
        except Exception as error:
            self.errors.append(str(error))
        finally:
            try:
                self.stop_guard()
            except Exception as error:
                self.errors.append('guard-stop: ' + str(error))
            if (self.live / 'completed-guard-events.jsonl').exists():
                try:
                    self.completed.bind_events(argparse.Namespace(events=self.live / 'completed-guard-events.jsonl',
                        snapshot=self.live / 'completed-guard.snapshot.jsonl', binding=self.live / 'completed-guard-binding.json',
                        session_id=self.session))
                except Exception as error:
                    self.errors.append('guard-binding: ' + str(error))
        receipt = dict(schema='cfdw-f1-cleanup-recovery/1', original_run_id=RUN_ID,
                       historical_status='FAILED: stage-simulate exited 5; checkpoint unproved; cleanup withheld',
                       original_manifest_sha256=FREEZE_SHA, recovery_success=success and not self.errors,
                       errors=self.errors, elapsed_seconds=time.monotonic() - self.start,
                       source_commit=getattr(self, 'authorization_value', {}).get('source_commit', 'Not recorded'),
                       authorization_sha256=digest(self.authorization), checkpoint_commit=getattr(self, 'head', 'Not recorded'),
                       commands=self.log, affinity='Linux CPU0-1 nice10; protected guard CPU0 nice10; host lifecycle/Git N/A')
        self.write('recovery-receipt.json', receipt)
        final = self.repo / REL / 'recovery-1/final'
        self.command([sys.executable, str(self.packet / 'manifest.py'), 'freeze',
                      '--live', str(self.live), '--frozen', str(final)], 'final-freeze', 90)
        relative = final.relative_to(self.repo).as_posix()
        self.command(['git', 'add', '-f', '--', relative], 'final-add')
        self.command(['git', 'commit', '-m', 'proof: separate F1 cleanup recovery outcome'], 'final-commit', 120)
        commit = self.command(['git', 'rev-parse', 'HEAD'], 'final-head').decode().strip()
        self.verify_commit(final, commit)
        if self.errors or not success:
            raise ValueError('recovery failed; receipt committed; Mac decision required: ' + '; '.join(self.errors))
        print('RECOVERY-BINDING-PASS commit=' + commit)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--authorization', type=Path, required=True)
    parser.add_argument('--session', required=True)
    parser.add_argument('--epoch', type=int, required=True)
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9._-]+', args.session) or args.session == RUN_ID:
        raise ValueError('distinct exact recovery session required')
    repo = Path(__file__).resolve().parents[4]
    os.environ['AGENT_SESSION'] = args.session
    Recovery(repo, args.authorization.resolve(strict=True), args.session, args.epoch).run()


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        print('RECOVERY-REFUSED: ' + str(error), file=sys.stderr)
        raise SystemExit(5)
