"""Offline-fast independent production probes; no WSL/registry/lifecycle calls."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'docs/proof/win-gpu-f1-preflight/attempt-1/cleanup'
SOURCE = '6e788efbb7c2bc6f708927b183515c9ced60226d'
CANDIDATE = '5cecf7ae2b976d1b4b29ec0e8cdd99a16fe44e80'
HASHED_CANDIDATE = '2a7c269f08149af2bb6165e94249c7d25dcb46c2'
spec = importlib.util.spec_from_file_location('review_r230', FOLDER / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)

def git(*args):
    return subprocess.run(['git', '-C', str(ROOT), *args], capture_output=True, check=True).stdout

def refuses(call):
    try:
        call()
    except (ValueError, FileNotFoundError):
        return
    raise AssertionError('required refusal missing')

hashes = {
    cleanup.PAYLOAD_REL: '3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89',
    cleanup.AUTH_REL: '1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d',
    Path('docs/notes/windows-gpu-f1-r230-cleanup-request.md'): '123b364c419619c7c4619addc670f0095c56b5c87de8b6b1a6ccc504f56a9cb6',
}
for relative, expected in hashes.items():
    assert cleanup.digest(ROOT / relative) == expected
    assert git('show', CANDIDATE + ':' + relative.as_posix()) == (ROOT / relative).read_bytes()
    print(relative.name + '=' + expected)
payload = json.loads((ROOT / cleanup.PAYLOAD_REL).read_bytes())
auth = json.loads((ROOT / cleanup.AUTH_REL).read_bytes())
assert payload['source_commit'] == SOURCE
git('merge-base', '--is-ancestor', SOURCE, CANDIDATE)
git('merge-base', '--is-ancestor', SOURCE, HASHED_CANDIDATE)
git('merge-base', '--is-ancestor', HASHED_CANDIDATE, CANDIDATE)
for relative in (cleanup.PAYLOAD_REL, cleanup.AUTH_REL):
    assert git('show', HASHED_CANDIDATE + ':' + relative.as_posix()) == (ROOT / relative).read_bytes()
    assert git('show', HASHED_CANDIDATE + '^:' + relative.as_posix()) != (ROOT / relative).read_bytes()
attempt_root = ROOT / 'docs/proof/win-gpu-f1-preflight/attempt-1'
assert (attempt_root / 'attempt-authorization.json').read_bytes() == (attempt_root / 'authorization.accepted-projection.json').read_bytes()
for commit in [auth['checkpoint_commit'], *auth['checkpoint_ancestors']]:
    git('merge-base', '--is-ancestor', commit, SOURCE)
for relative, expected in payload['files'].items():
    assert cleanup.digest(ROOT / relative) == expected
    assert hashlib.sha256(git('show', SOURCE + ':' + relative)).hexdigest() == expected
cleanup.validate_authorization(auth, hashes[cleanup.PAYLOAD_REL])
for key in auth:
    changed = copy.deepcopy(auth)
    changed[key] = 'invalid' if key == 'source_commit' else None
    refuses(lambda: cleanup.validate_authorization(changed, hashes[cleanup.PAYLOAD_REL]))
identity = cleanup.reviewed_identity(auth)
rows = json.loads((ROOT / cleanup.FAILURE_REL / 'coordinator/host-registrations-after-failure.json').read_bytes())
target, protected = cleanup.validate_identity(rows, [identity['target'], identity['protected']], True, True, identity)
assert target['version'] == protected['version'] == 2
assert payload['target_identity'] == {key: target[key] for key in ('name', 'key', 'path')}
assert payload['protected_identity'] == protected
for key in ('target', 'protected'):
    alias = dict(name='unreviewed-alias', key='{11111111-2222-3333-4444-555555555555}',
                 path=identity[key + '_path'], version=2)
    refuses(lambda: cleanup.validate_identity(rows + [alias], [identity['target'], identity['protected'], alias['name']], True, True, identity))
    if key == 'protected':
        refuses(lambda: cleanup.validate_absence([protected, alias], [identity['protected'], alias['name']], False, True, protected, identity))
origin_rulings = git('show', 'origin/main:docs/notes/rulings.md').decode('utf-8')
refuses(lambda: cleanup.ruling_section(origin_rulings, auth['ruling']))
assert not (ROOT / cleanup.ACTIVATION_SAMPLE_REL).exists()
history = ['docs/proof/win-gpu-f1-preflight/' + name for name in ('attempt-1/checkpoint', 'attempt-1/final', 'run-1', 'recovery-1')]
assert git('diff', '--name-only', auth['checkpoint_commit'], CANDIDATE, '--', *history) == b''
print('BINDING-IDENTITY-AUTHORITY-HISTORY-PASS; source files=' + str(len(payload['files'])))

# Exercise the production execute boundary with the live/process methods mocked.
# The real second-sample reader and validator receive an expired-threshold sample.
with tempfile.TemporaryDirectory() as temporary:
    instance = cleanup.Cleanup(Path(temporary), ROOT / cleanup.AUTH_REL,
                               auth['session'], auth['leader_epoch'], auth['exclusive_run_id'])
    low_sample = {'leader': 'mac', 'state': 'live', 'epoch': 8, 'expires_in': 199.0}
    with patch.object(instance, 'precheck'), patch.object(instance, 'publish_precheck'), \
         patch.object(instance, 'run_command', return_value=json.dumps(low_sample).encode()) as command, \
         patch.object(instance, 'action') as action, patch.object(instance, 'bind') as bind:
        refuses(instance.execute)
        action.assert_not_called()
        assert command.call_count == 1
        assert command.call_args.args[0][-3:] == ['leader', 'who', '--json']
        receipt_missing = not bind.called
        bind.assert_called_once_with('failed-stop-no-retry')
        assert instance.failure['type'] == 'ValueError'
        instance.authorization_value = auth
        instance.payload_sha = hashes[cleanup.PAYLOAD_REL]
        instance.identity = identity
        instance.original_run_id = auth['original_run_id']
        instance.activation_sample = {'leader': 'mac', 'state': 'live', 'epoch': 8, 'expires_in': 200.0}
        receipt = instance.receipt('failed-stop-no-retry')
        assert receipt['outcome'] == 'failed-stop-no-retry'
        assert receipt['failure'] == instance.failure
        assert receipt['authorization_sha256'] == hashes[cleanup.AUTH_REL]
        print('LOW-TTL-PRE-LIVE: refused; lifecycle calls=0; bind calls=' + str(bind.call_count))

valid = {'leader': 'mac', 'state': 'live', 'epoch': 8, 'expires_in': 200.0}
cleanup.validate_leader_sample(valid, 8)
admitted_nonfinite = []
for label, ttl in [('NaN', float('nan')), ('Infinity', float('inf')), ('-Infinity', -float('inf'))]:
    try:
        cleanup.validate_leader_sample(dict(valid, expires_in=ttl), 8)
    except ValueError:
        pass
    else:
        admitted_nonfinite.append(label)
print('NONFINITE-TTL-ADMITTED=' + repr(admitted_nonfinite))
assert not receipt_missing and not admitted_nonfinite, 'REVIEW-VETO: pre-live lease failure escapes receipt; non-finite TTL is admitted'
print('PREVIOUS-VETO-CONTROLS-PASS: failed receipt binding and all three non-finite refusals')

# Synthetic future activation exercises the real precheck with only its
# subprocess boundary replaced. No activation file is created in the worktree.
source_blobs = {relative: git('show', SOURCE + ':' + relative) for relative in payload['files']}
section = '\n'.join([
    '### Ruling 230 — ACCEPT ONE CLEANUP-ONLY ACTION', hashes[cleanup.AUTH_REL],
    hashes[cleanup.PAYLOAD_REL], SOURCE, identity['target_key'], identity['target_path'],
    identity['protected_key'], identity['protected_path'], 'never resume preflight',
    'never download', 'never install', 'never fetch source', 'never build',
    'never run a case', 'never benchmark', 'never delete host paths manually',
    'R223(a) same commit', 'R223(b) two leader samples at least 200 seconds',
    'R223(c) PC exclusive', 'R223(d) one attempt; no second run without a new ruling',
])

def precheck_case(mode):
    with tempfile.TemporaryDirectory() as temporary:
        repo = Path(temporary)
        for relative, data in source_blobs.items():
            destination = repo / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(data)
        for relative in (cleanup.PAYLOAD_REL, cleanup.AUTH_REL):
            (repo / relative).write_bytes((ROOT / relative).read_bytes())
        sample = dict(valid, expires_in=199.0) if mode == 'low-activation' else valid
        (repo / cleanup.ACTIVATION_SAMPLE_REL).write_bytes(cleanup.canonical(sample))
        instance = cleanup.Cleanup(repo, (repo / cleanup.AUTH_REL).resolve(),
                                   'wrong' if mode == 'session' else auth['session'],
                                   9 if mode == 'epoch' else 8,
                                   'wrong' if mode == 'exclusive' else auth['exclusive_run_id'])
        if mode == 'used-attempt':
            instance.live.mkdir(parents=True)
        if mode == 'proposed':
            (repo / cleanup.AUTH_REL).write_bytes(cleanup.canonical(dict(auth, status='proposed')))
        if mode == 'altered-file':
            (repo / cleanup.REL / 'cleanup.py').write_bytes(b'altered')
        seen = []
        def command(argv, label, cap=30):
            assert argv[0] != 'wsl.exe'
            seen.append(label)
            if label == 'clean': return b'dirty' if mode == 'dirty' else b''
            if label == 'branch': return ('wrong' if mode == 'branch' else auth['branch']).encode()
            if label == 'head': return CANDIDATE.encode()
            if label.endswith('ancestor'):
                if mode == 'source-ancestor' and label == 'source-ancestor':
                    raise ValueError('synthetic source not ancestor')
                return b''
            if label == 'committed-blob':
                relative = argv[-1].split(':', 1)[1]
                return b'wrong' if mode == 'head-blob' else (repo / relative).read_bytes()
            if label == 'source-blob':
                return b'wrong' if mode == 'source-blob' else source_blobs[argv[-1].split(':', 1)[1]]
            if label in ('verify-r228-failure', 'verify-r228-checkpoint'):
                if mode == label: raise ValueError('synthetic manifest mismatch')
                return b'GIT-BINDING-PASS'
            if label == 'origin-main-ruling':
                return (origin_rulings if mode == 'missing-ruling' else section).encode()
            if label == 'ruling-commit':
                return b'' if mode == 'ambiguous-ruling' else (('f' * 40) + '\n').encode()
            if label == 'ruling-paths':
                return ('docs/notes/rulings.md\n' + ('' if mode == 'split-commit' else cleanup.AUTH_REL.as_posix() + '\n')).encode()
            if label == 'ruling-auth-blob':
                return b'wrong' if mode == 'ruling-auth' else (repo / cleanup.AUTH_REL).read_bytes()
            if label == 'ruling-leader-sample':
                return b'wrong' if mode == 'ruling-sample' else (repo / cleanup.ACTIVATION_SAMPLE_REL).read_bytes()
            raise AssertionError('unexpected production command: ' + label)
        with patch.object(instance, 'run_command', side_effect=command):
            if mode == 'valid':
                instance.precheck()
                assert instance.identity == identity
                assert instance.original_run_id == auth['original_run_id']
                assert instance.activation_sample == valid
            else:
                refuses(instance.precheck)
        return seen

cases = ('valid', 'session', 'epoch', 'exclusive', 'used-attempt', 'proposed',
         'dirty', 'branch', 'head-blob', 'source-ancestor', 'source-blob', 'altered-file',
         'verify-r228-failure', 'verify-r228-checkpoint', 'missing-ruling',
         'ambiguous-ruling', 'split-commit', 'ruling-auth', 'ruling-sample', 'low-activation')
for case in cases:
    precheck_case(case)
print('PRODUCTION-PRECHECK-PASS: ' + str(len(cases)) + ' synthetic activation/negative cases; zero WSL calls')

assert not hasattr(cleanup, 'RULING')
assert '230' not in (FOLDER / 'cleanup.py').read_text(encoding='utf-8')
alternate = dict(auth, ruling=731, schema='cfdw-f1-r731-cleanup-authorization/1',
                 session='pc-f1-r731-cleanup1', exclusive_run_id='pc-f1-r731-cleanup1')
cleanup.validate_authorization(alternate, hashes[cleanup.PAYLOAD_REL])
alternate_section = section.replace('### Ruling 230', '### Ruling 731')
assert cleanup.ruling_section(alternate_section, 731) == alternate_section
refuses(lambda: cleanup.ruling_section(alternate_section, 230))
cleanup.verify_ruling(alternate_section, hashes[cleanup.AUTH_REL], hashes[cleanup.PAYLOAD_REL], SOURCE, 731)
with tempfile.TemporaryDirectory() as temporary:
    repo = Path(temporary)
    path = repo / cleanup.AUTH_REL
    path.parent.mkdir(parents=True)
    path.write_bytes(cleanup.canonical(alternate))
    instance = cleanup.Cleanup(repo, path, alternate['session'], 8, alternate['exclusive_run_id'])
    instance.authorization_value = alternate
    instance.payload_sha = hashes[cleanup.PAYLOAD_REL]
    instance.identity = identity
    instance.original_run_id = alternate['original_run_id']
    instance.activation_sample = valid
    commands = []
    def binding_command(argv, label, cap=30):
        commands.append((label, argv))
        if label == 'freeze':
            instance.final.mkdir(parents=True)
            (instance.final / 'capture-manifest.json').write_bytes(b'{}\n')
        if label == 'evidence-head': return ('a' * 40).encode()
        if label == 'binding-head': return ('b' * 40).encode()
        return b''
    with patch.object(instance, 'run_command', side_effect=binding_command):
        assert instance.bind('failed-stop-no-retry') == 'b' * 40
    receipt = json.loads((instance.live / 'cleanup-receipt.json').read_bytes())
    binding = json.loads(instance.binding.read_bytes())
    assert receipt['ruling'] == 731
    assert receipt['schema'] == 'cfdw-f1-r731-cleanup-receipt/1'
    assert binding['schema'] == 'cfdw-f1-r731-cleanup-binding/1'
    commit_messages = [argv[-1] for label, argv in commands if label in ('git-commit-final', 'git-commit-binding')]
    assert len(commit_messages) == 2 and all('R731' in value for value in commit_messages)
print('HOLD-CONFORMANCE-PASS: distinct source/candidate, raw live=projection, dynamic section/receipt/binding')
