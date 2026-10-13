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
import shutil
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
    sys.modules[spec.name] = module
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
        host = os.environ.get('COMPUTERNAME')
        if host:
            value = re.sub(re.escape(host), lambda match: '<host>', value, flags=re.IGNORECASE)
        return re.sub(r'S-1-5-21-\d+-\d+-\d+-\d+', 'S-1-5-21-<machine>-<RID>', value)
    if isinstance(value, list):
        return [public_value(item) for item in value]
    if isinstance(value, dict):
        return {key: public_value(item) for key, item in value.items()}
    return value


def validate_identity(rows, names):
    keys = [row['key'].casefold() for row in rows]
    if len(keys) != len(set(keys)):
        raise ValueError('duplicate registry GUID')
    result = []
    for name, path in ((TARGET, TARGET_PATH), (PROTECTED, PROTECTED_PATH)):
        found = [row for row in rows if row['name'] == name]
        if names.count(name) != 1 or len(found) != 1 or found[0]['version'] != 2:
            raise ValueError('ambiguous/missing exact WSL identity: ' + name)
        if normalize(found[0]['path']) != normalize(path):
            raise ValueError('WSL registration path mismatch: ' + name)
        if sum(normalize(row.get('path', '')) == normalize(path) for row in rows) != 1:
            raise ValueError('ambiguous registration path: ' + name)
        result.append(found[0])
    if result[0]['key'] == result[1]['key']:
        raise ValueError('shared registry identity')
    return tuple(result)


def validate_absence(rows, names, path_present, protected, target_key=TARGET_KEY):
    if any(row['name'] == TARGET or row.get('key', '').casefold() == target_key.casefold() or
           normalize(row.get('path', '')) == normalize(TARGET_PATH) for row in rows) or TARGET in names or path_present:
        raise ValueError('target registration/list/path absence not proved; no manual path deletion')
    if [row for row in rows if row['name'] == PROTECTED] != [protected] or names.count(PROTECTED) != 1:
        raise ValueError('protected registry identity changed')


class RecoveryFailure(ValueError):
    """Ordered failures: initiating action first, then independent final checks."""
    def __init__(self, failures):
        self.failures = failures
        super().__init__('; '.join(phase + ': ' + str(error) for phase, error in failures))


def classified_failure(phase, error):
    return dict(phase=phase, exception_type=type(error).__name__,
                detail='Raw runtime text is not public; SHA256 identifies the failure',
                raw_error_sha256=hashlib.sha256(str(error).encode('utf-8')).hexdigest())


def flatten_failures(phase, error, nested=False):
    if not isinstance(error, RecoveryFailure):
        return [(phase, error)]
    result = []
    for name, failure in error.failures:
        context = phase + '/' + name if nested and name != phase else name
        result.extend(flatten_failures(context, failure, True))
    return result


def cleanup_sequence(action):
    action('verify-checkpoint')
    action('fresh-identity')
    action('old-owned-check')
    action('guard-ready')
    failures = []
    try:
        action('delete-debs')
    except Exception as error:
        failures.append(('delete-debs', error))
    finally:
        try:
            action('final-owned-check')
        except Exception as error:
            failures.append(('final-owned-check', error))
    if not failures:
        for name in ('fresh-identity', 'terminate', 'fresh-identity', 'unregister', 'absence', 'protected-invariance'):
            try:
                action(name)
            except Exception as error:
                failures.append((name, error))
                break
    if failures:
        if not any(phase == 'absence' for phase, _ in failures):
            try:
                action('absence')
            except Exception as error:
                failures.append(('absence', error))
        raise RecoveryFailure(failures)


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
        self.admitted_target_key = None
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
        self.remaining(cap)  # Before opening captures or launching any child.
        self.index += 1
        prefix = self.live / f'{self.index:03d}-{label}'
        private = self.live.parent / 'private'
        private.mkdir(exist_ok=True)
        raw_prefix = private / prefix.name
        started = time.monotonic()
        failures = []
        child = None
        with raw_prefix.with_suffix('.stdout.txt').open('wb') as out, raw_prefix.with_suffix('.stderr.txt').open('wb') as err:
            end = min(self.start + 600, started + cap)
            self.host_intent(argv, label, self.index)
            self.remaining(cap)  # Recheck after capture setup, immediately before Popen.
            try:
                child = subprocess.Popen(argv, cwd=self.repo, stdout=out, stderr=err,
                                         creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
                self.host_ack(child, label, self.index)
                while child.poll() is None:
                    if time.monotonic() >= end:
                        raise ValueError(label + ' timed out; Linux state remains unknown')
                    if guarded:
                        self.guard_ok()
                    time.sleep(0.1)
                self.remaining(cap)
                if child.returncode != 0:
                    raise ValueError(f'{label} exited {child.returncode}')
            except Exception as error:
                failures.append((label, error))
            finally:
                if child is not None and child.poll() is None:
                    try:
                        child.kill()
                        child.wait(timeout=self.remaining(5))
                    except Exception as error:
                        failures.append((label + '-launcher-reap', error))
                self.log.append(dict(label=label, argv=argv, exit=child.returncode if child else 'Not recorded',
                                     elapsed_seconds=time.monotonic() - started))
        for suffix in ('.stdout.txt', '.stderr.txt'):
            try:
                self.publish_capture(raw_prefix.with_suffix(suffix), prefix.with_suffix(suffix))
            except Exception as error:
                failures.append((label + '-capture' + suffix, error))
        if failures:
            raise RecoveryFailure(failures)
        return raw_prefix.with_suffix('.stdout.txt').read_bytes()

    def host_intent(self, argv, label, ordinal):
        records = self.live / 'launch-records'
        records.mkdir(exist_ok=True)
        output = records / f'{ordinal:03d}-{label}.intent.json'
        if output.exists():
            raise ValueError('host launch intent already exists')
        temporary = output.with_suffix('.json.tmp')
        temporary.write_text(json.dumps(dict(argv=public_value(argv), label=label,
                             host_ack='pending', linux_state='Not recorded'), sort_keys=True) + '\n',
                             encoding='utf-8', newline='\n')
        os.replace(temporary, output)

    def host_ack(self, child, label, ordinal):
        if type(child.pid) is not int or child.pid <= 0:
            raise ValueError('host launcher lacks a numeric ownership acknowledgement')
        self.write(f'launch-records/{ordinal:03d}-{label}.ack.json',
                   dict(host_pid=child.pid, linux_state='Unknown until guest acknowledgement or terminal verification'))

    def publish_capture(self, raw, output):
        data = raw.read_bytes()
        output.write_text(json.dumps(dict(schema='cfdw-classified-subprocess-capture/1',
                          bytes=len(data), sha256=hashlib.sha256(data).hexdigest(),
                          raw='Retained privately; raw runtime text is not public'), sort_keys=True) + '\n',
                          encoding='utf-8', newline='\n')

    def retain_failure(self, phase, error):
        flattened = flatten_failures(phase, error)
        self.errors.extend(classified_failure(name, failure) for name, failure in flattened)
        private = self.live.parent / 'private'
        try:
            private.mkdir(exist_ok=True)
            with (private / 'errors.jsonl').open('a', encoding='utf-8', newline='\n') as output:
                for name, failure in flattened:
                    output.write(json.dumps(dict(phase=name, exception_type=type(failure).__name__,
                                                raw_error=str(failure)), sort_keys=True) + '\n')
        except Exception as error:
            self.errors.append(classified_failure('private-error-retention', error))

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
        self.publish_intent(label, distro, command)
        result = self.command(self.linux_argv(distro, label, command), label, cap, guarded)
        self.assert_ack(label)
        return result

    def publish_intent(self, label, distro, command):
        output = self.live / (label + '.intent.json')
        temporary = output.with_suffix('.json.tmp')
        ack = self.live / ('guard.owner.txt' if label == 'guard' else label + '.entry.txt')
        temporary.write_text(json.dumps(dict(session=self.session, run_id=RUN_ID, target=distro,
                             stage=label, command=public_value(command), ownership='ack required'),
                             sort_keys=True) + '\n', encoding='utf-8', newline='\n')
        value = json.loads(temporary.read_bytes())
        value['previous_ack_sha256'] = digest(ack) if ack.exists() else None
        temporary.write_text(json.dumps(value, sort_keys=True) + '\n', encoding='utf-8', newline='\n')
        os.replace(temporary, output)
        records = self.live / 'intent-records'
        records.mkdir(exist_ok=True)
        ordinal = getattr(self, 'index', 0) + 1
        history = records / f'{ordinal:03d}-{label}.json'
        if history.exists():
            raise ValueError('prelaunch intent history already exists')
        shutil.copyfile(output, history)

    def assert_ack(self, label):
        path = self.live / ('guard.owner.txt' if label == 'guard' else label + '.entry.txt')
        if not path.is_file():
            raise ValueError('prelaunch intent lacks ownership acknowledgement: ' + label)
        intent = json.loads((self.live / (label + '.intent.json')).read_bytes())
        if intent.get('previous_ack_sha256') == digest(path):
            raise ValueError('prelaunch intent has stale ownership acknowledgement: ' + label)
        fields = dict(line.split('=', 1) for line in path.read_text(encoding='utf-8').splitlines() if '=' in line)
        identities = [fields.get(key, '') for key in ('pid', 'pgid', 'sid')]
        if not all(value.isdecimal() and int(value) > 0 for value in identities) or len(set(identities)) != 1:
            raise ValueError('ownership acknowledgement identity incomplete: ' + label)
        if fields.get('nice') != '10' or fields.get('Cpus_allowed_list') not in ('0', '0-1', '0,1'):
            raise ValueError('ownership acknowledgement affinity/nice invalid: ' + label)
        records = self.live / 'owner-records'
        records.mkdir(exist_ok=True)
        ordinal = getattr(self, 'index', 0)
        history = records / f'{ordinal:03d}-{label}.txt'
        if not history.exists():
            shutil.copyfile(path, history)

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
        if self.admitted_target_key is None:
            self.admitted_target_key = observed[0]['key']
        elif observed[0]['key'] != self.admitted_target_key:
            raise ValueError('admitted target GUID changed')
        for path in (Path(TARGET_PATH), Path(os.path.expandvars(PROTECTED_PATH))):
            if not path.is_dir() or any(parent.is_symlink() or parent.is_junction()
                                        for parent in (path, *path.parents)):
                raise ValueError('missing or linked distro path')
        self.write('registration-before.json', dict(registrations=public_registrations(rows), names=names))

    def stable_identity(self):
        """Two complete identical observations immediately before each destructive action."""
        self.remaining(15)
        self.fresh_identity()
        before = self.identity
        before_paths = self.path_identities()
        self.fresh_identity()
        if before != self.identity or before_paths != self.path_identities():
            raise ValueError('registration/path identity changed across stable admission')
        self.remaining(15)

    def path_identities(self):
        result = []
        for path in (Path(TARGET_PATH), Path(os.path.expandvars(PROTECTED_PATH))):
            for part in (path, *path.parents):
                value = part.lstat()
                if part.is_symlink() or part.is_junction():
                    raise ValueError('linked distro path during stable admission')
                result.append((str(part), value.st_dev, value.st_ino))
        return result

    def write(self, name, value):
        (self.live / name).write_text(json.dumps(public_value(value), indent=2, sort_keys=True) + '\n', encoding='utf-8', newline='\n')

    def owned(self, root, run_id, mode='check', kind='stage'):
        self.linux(PROTECTED if kind == 'guard' else TARGET, f'owned-{kind}-{mode}',
                   ['/bin/bash', self.linux_path(self.packet / 'owned-control.sh'), '--mode', mode,
                    '--run-root', self.linux_path(root), run_id] + (['--kind', 'guard'] if kind == 'guard' else []), 15)

    def guard_sample_time(self, value, *, state):
        if value['session_id'] != self.session or value['expected_sha256'] != self.before:
            raise ValueError('guard binding changed')
        stamp = value['last_sample_utc'] if state else value['utc']
        instant = dt.datetime.fromisoformat(stamp.replace('Z', '+00:00'))
        age = (dt.datetime.now(dt.timezone.utc) - instant).total_seconds()
        if not -5 <= age <= 12 or (value['state'] != 'running' if state else value['verdict'] != 'continue'):
            raise ValueError('guard stopped/stale')
        return instant

    def guard_ok(self, deadline=None):
        started = time.monotonic()
        end = min(started + self.remaining(0.5), deadline if deadline is not None else self.start + 600)
        attempts = 0
        previous = None
        try:
            for attempts in range(1, 11):
                self.remaining(0.5)
                if time.monotonic() >= end:
                    raise ValueError('guard consistency deadline exhausted')
                if self.guard is None or self.guard.poll() is not None:
                    raise ValueError('fresh completed guard exited/missing')
                state = json.loads((self.live / 'completed-guard.state.json').read_bytes())
                raw = (self.live / 'completed-guard-events.jsonl').read_bytes()
                complete = raw[:raw.rfind(b'\n') + 1]
                if not complete:
                    raise ValueError('fresh guard has no complete event')
                event = json.loads(complete.splitlines()[-1])
                state_time = self.guard_sample_time(state, state=True)
                event_time = self.guard_sample_time(event, state=False)
                samples = (state['completed_samples'], event['sample_number'])
                if previous is not None and any(current < old for current, old in zip(samples, previous)):
                    raise ValueError('guard samples regressed')
                previous = samples
                matching = event['sample_number'] == state['completed_samples'] and event['utc'] == state['last_sample_utc']
                in_flight = event['sample_number'] == state['completed_samples'] + 1 and event_time > state_time
                if not matching and not in_flight:
                    raise ValueError('guard event/state disagree')
                latest_state = json.loads((self.live / 'completed-guard.state.json').read_bytes())
                latest_time = self.guard_sample_time(latest_state, state=True)
                if latest_state != state:
                    if latest_state['completed_samples'] <= state['completed_samples'] or latest_time <= state_time:
                        raise ValueError('guard samples regressed or state changed without advancing')
                    if attempts == 10:
                        raise ValueError('guard state did not stabilize')
                    time.sleep(min(0.05, max(0.0, end - time.monotonic())))
                    continue
                if self.guard.poll() is not None:
                    raise ValueError('fresh completed guard exited/missing')
                self.guard_sample_time(state, state=True)
                self.guard_sample_time(event, state=False)
                self.remaining(0.5)
                if time.monotonic() >= end:
                    raise ValueError('guard consistency deadline exhausted')
                # R224: event N+1 is in flight; only stable state N is completed.
                if in_flight:
                    self.log.append(dict(label='guard-in-flight', completed_samples=state['completed_samples'],
                                         event_sample=event['sample_number'], elapsed_seconds=time.monotonic() - started))
                return state['completed_samples']
        finally:
            if attempts > 1:
                self.log.append(dict(label='guard-consistency', attempts=attempts,
                                     elapsed_seconds=time.monotonic() - started))

    def snapshot(self, name):
        self.linux(PROTECTED, name, ['/usr/bin/python3', self.linux_path(self.packet / 'completed_guard.py'),
                   'snapshot', '--output', self.linux_path(self.live / (name + '.json')), '--include-log-sha256'], 60)
        self.completed.validate_terminal(json.loads((self.live / (name + '.json')).read_bytes()), require_log_sha256=True)
        return digest(self.live / (name + '.json'))

    def start_guard(self):
        self.remaining(20)
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
        self.publish_intent('guard', PROTECTED, command)
        self.index += 1
        self.host_intent(argv, 'guard', self.index)
        private = self.live.parent / 'private'
        private.mkdir(exist_ok=True)
        self.guard_out = (private / 'guard.stdout.txt').open('wb')
        self.guard_err = (private / 'guard.stderr.txt').open('wb')
        end = min(self.start + 600, time.monotonic() + 20)
        try:
            self.remaining(20)
            self.guard = subprocess.Popen(argv, stdout=self.guard_out, stderr=self.guard_err,
                                          creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            self.host_ack(self.guard, 'guard', self.index)
        except Exception as error:
            failures = [('guard-launch', error)]
            for name in ('guard_out', 'guard_err'):
                try:
                    getattr(self, name).close()
                except Exception as error:
                    failures.append((name + '-close', error))
            raise RecoveryFailure(failures)
        while not (self.live / 'completed-guard.state.json').exists():
            if self.guard.poll() is not None or time.monotonic() >= end:
                raise ValueError('fresh guard startup failed')
            time.sleep(0.1)
        self.assert_ack('guard')
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
        self.stable_identity()
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
            self.stable_identity()
            self.command(['wsl.exe', '--' + name, TARGET], name, 30 if name == 'terminate' else 120, True)
        elif name == 'absence':
            rows, names = self.registrations(), self.names()
            validate_absence(rows, names, os.path.lexists(TARGET_PATH), self.identity[1], self.admitted_target_key)
            self.write('registration-after.json', dict(registrations=public_registrations(rows), names=names, target_absent=True))
        elif name == 'protected-invariance':
            count = self.guard_ok()
            end = time.monotonic() + self.remaining(12)
            while self.guard_ok(deadline=end) <= count:
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
            for name in ('guard_out', 'guard_err'):
                stream = getattr(self, name, None)
                if stream is not None:
                    stream.close()
            return
        failures = []
        try:
            (self.live / 'completed-guard-stop.request').write_text(self.session + '\n', encoding='ascii')
            self.guard.wait(timeout=self.remaining(20))
        except Exception as error:
            failures.append(('guard-stop-request/wait', error))
        finally:
            if self.guard.poll() is None:
                try:
                    self.guard.kill()
                    self.guard.wait(timeout=self.remaining(5))
                except Exception as error:
                    failures.append(('guard-launcher-reap', error))
            for mode in ('stop', 'check'):
                try:
                    self.owned(self.live, self.session, mode, 'guard')
                except Exception as error:
                    failures.append(('guard-owned-' + mode, error))
            try:
                self.assert_ack('guard')
            except Exception as error:
                failures.append(('guard-acknowledgement', error))
            for name in ('guard_out', 'guard_err'):
                try:
                    getattr(self, name).close()
                except Exception as error:
                    failures.append((name + '-close', error))
            private = self.live.parent / 'private'
            for suffix in ('.stdout.txt', '.stderr.txt'):
                try:
                    self.publish_capture(private / ('guard' + suffix), self.live / ('guard' + suffix))
                except Exception as error:
                    failures.append(('guard-capture' + suffix, error))
        if self.guard.returncode != 0:
            failures.append(('guard-exit', ValueError('guard nonzero or unreaped exit retained')))
        if failures:
            raise RecoveryFailure(failures)

    def publish_guard_prefix(self):
        raw = (self.live / 'completed-guard-events.jsonl').read_bytes()
        private = self.live.parent / 'private'
        private.mkdir(exist_ok=True)
        (private / 'completed-guard-events.raw.jsonl').write_bytes(raw)
        complete = raw[:raw.rfind(b'\n') + 1]
        rows = []
        for line in complete.splitlines():
            row = json.loads(line)
            if row.get('verdict') == 'stop':
                reason = str(row.get('reason', 'Not recorded'))
                row['reason'] = 'Runtime reason classified; SHA256=' + hashlib.sha256(reason.encode('utf-8')).hexdigest()
            rows.append(json.dumps(public_value(row), sort_keys=True))
        (self.live / 'completed-guard.public-events.jsonl').write_text('\n'.join(rows) + '\n', encoding='utf-8', newline='\n')

    def prepare_public(self):
        public = self.live.parent / 'public'
        public.mkdir(exist_ok=False)
        for source in sorted(self.live.rglob('*')):
            self.remaining(90)
            if source.is_file():
                if source.is_symlink() or source.is_junction():
                    raise ValueError('linked recovery evidence')
                target = public / source.relative_to(self.live)
                target.parent.mkdir(parents=True, exist_ok=True)
                if source.name == 'completed-guard-events.jsonl':
                    shutil.copyfile(self.live / 'completed-guard.public-events.jsonl', target)
                else:
                    shutil.copyfile(source, target)
        return public

    def receipt(self, cleanup_proved, binding_status):
        return dict(schema='cfdw-f1-cleanup-recovery/2', original_run_id=RUN_ID,
                    historical_status='FAILED: stage-simulate exited 5; checkpoint unproved; cleanup withheld',
                    original_manifest_sha256=FREEZE_SHA, cleanup_proved=cleanup_proved and not self.errors,
                    recovery_success=False, evidence_binding=binding_status,
                    initiating_error=self.errors[0] if self.errors else 'Not recorded',
                    errors=self.errors, elapsed_seconds=time.monotonic() - self.start,
                    source_commit=getattr(self, 'authorization_value', {}).get('source_commit', 'Not recorded'),
                    authorization_sha256=digest(self.authorization), checkpoint_commit=getattr(self, 'head', 'Not recorded'),
                    commands=self.log, affinity='Linux CPU0-1 nice10; protected guard CPU0 nice10; host lifecycle/Git N/A')

    def bind_evidence(self, success):
        self.write('recovery-receipt.json', self.receipt(success, 'pending; requires verified commit'))
        final = self.repo / REL / 'recovery-1/final'
        try:
            public = self.prepare_public()
            self.command([sys.executable, str(self.packet / 'manifest.py'), 'freeze',
                          '--live', str(public), '--frozen', str(final)], 'final-freeze', 90)
            relative = final.relative_to(self.repo).as_posix()
            self.command(['git', 'add', '-f', '--', relative], 'final-add')
            self.command(['git', 'commit', '-m', 'proof: separate F1 cleanup recovery outcome'], 'final-commit', 120)
            commit = self.command(['git', 'rev-parse', 'HEAD'], 'final-head').decode().strip()
            self.verify_commit(final, commit)
            self.write('recovery-receipt.json', self.receipt(success, 'verified commit=' + commit))
            self.remaining(1)
        except Exception as error:
            self.retain_failure('evidence-binding', error)
            self.write('recovery-receipt.json', self.receipt(False, 'incomplete; unverified; Mac decision required'))
            raise ValueError('recovery evidence binding incomplete; live/private evidence retained; no success') from error
        if self.errors or not success:
            raise ValueError('recovery failed; classified receipt committed; Mac decision required')
        print('RECOVERY-BINDING-PASS commit=' + commit)

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
            self.retain_failure('cleanup', error)
        finally:
            try:
                self.stop_guard()
            except Exception as error:
                self.retain_failure('guard-stop', error)
            if (self.live / 'completed-guard-events.jsonl').exists():
                try:
                    self.publish_guard_prefix()
                    self.completed.bind_events(argparse.Namespace(events=self.live / 'completed-guard.public-events.jsonl',
                        snapshot=self.live / 'completed-guard.snapshot.jsonl', binding=self.live / 'completed-guard-binding.json',
                        session_id=self.session))
                except Exception as error:
                    self.retain_failure('guard-binding', error)
        self.bind_evidence(success)


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
        print('RECOVERY-REFUSED: error classified; type=' + type(error).__name__ +
              ' SHA256=' + hashlib.sha256(str(error).encode('utf-8')).hexdigest(), file=sys.stderr)
        raise SystemExit(5)
