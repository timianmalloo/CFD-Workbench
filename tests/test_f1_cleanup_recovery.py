"""Offline-fast recovery controls; no WSL or recovery execution (~1 s target)."""
import importlib.util
import tempfile
import unittest
import contextlib
import io
import json
import sys
from unittest.mock import Mock, patch
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / 'docs/proof/win-gpu-f1-preflight/recovery/recover.py'
spec = importlib.util.spec_from_file_location('recover', SCRIPT)
recovery = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = recovery
spec.loader.exec_module(recovery)


class RecoveryTests(unittest.TestCase):
    def guard_fixture(self, root):
        instance = object.__new__(recovery.Recovery)
        instance.live, instance.session, instance.before = root, 'guard-fixture', 'a' * 64
        instance.start, instance.log = 0, []
        instance.guard = Mock()
        instance.guard.poll.return_value = None
        writer = recovery.load_helper(ROOT / recovery.REL / 'packet', 'completed_guard')
        stamp = recovery.dt.datetime.now(recovery.dt.timezone.utc).isoformat()
        state = dict(session_id=instance.session, expected_sha256=instance.before,
                     state='running', completed_samples=1, last_sample_utc=stamp)
        event = dict(session_id=instance.session, expected_sha256=instance.before,
                     verdict='continue', sample_number=1, utc=stamp)
        writer.append_event(root / 'completed-guard-events.jsonl', event)
        writer.write_atomic_json(root / 'completed-guard.state.json', state)
        return instance, writer, state, event

    def test_Guard_EventPublishedBeforeState_RechecksActualBoundary(self):
        # Offline-fast: real append/atomic replacement; no sleeps or processes (~0.01 s).
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            stamp = (recovery.dt.datetime.now(recovery.dt.timezone.utc) + recovery.dt.timedelta(milliseconds=1)).isoformat()
            event.update(sample_number=2, utc=stamp)
            state.update(completed_samples=2, last_sample_utc=stamp)
            writer.append_event(instance.live / 'completed-guard-events.jsonl', event)
            def finish_publication(_seconds):
                writer.write_atomic_json(instance.live / 'completed-guard.state.json', state)
            with patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep', side_effect=finish_publication) as pause:
                self.assertEqual(instance.guard_ok(), 1)
            pause.assert_not_called()
            self.assertEqual(instance.log[0]['completed_samples'], 1)
            self.assertEqual(instance.log[0]['event_sample'], 2)

    def test_Guard_AdjacentGapSameOrEarlierUtc_Refuses(self):
        for delta in (0, -1):
            with self.subTest(milliseconds=delta), tempfile.TemporaryDirectory() as temporary:
                instance, writer, state, event = self.guard_fixture(Path(temporary))
                event['sample_number'] = 2
                event['utc'] = (recovery.dt.datetime.fromisoformat(state['last_sample_utc']) + recovery.dt.timedelta(milliseconds=delta)).isoformat()
                writer.append_event(instance.live / 'completed-guard-events.jsonl', event)
                def complete_invalid_publication(_seconds):
                    writer.write_atomic_json(instance.live / 'completed-guard.state.json',
                        dict(state, completed_samples=2, last_sample_utc=event['utc']))
                with patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep', side_effect=complete_invalid_publication) as pause:
                    with self.assertRaisesRegex(ValueError, 'guard event/state disagree'):
                        instance.guard_ok()
                pause.assert_not_called()

    def moving_guard_state(self, instance, writer, state, event):
        read, confirm = Path.read_bytes, [False]
        def read_with_publication(path):
            if path.name == 'completed-guard.state.json':
                if confirm[0]:
                    state['completed_samples'] += 1
                    state['last_sample_utc'] = (recovery.dt.datetime.fromisoformat(state['last_sample_utc']) + recovery.dt.timedelta(milliseconds=1)).isoformat()
                    event.update(sample_number=state['completed_samples'], utc=state['last_sample_utc'])
                    writer.append_event(instance.live / 'completed-guard-events.jsonl', event)
                    writer.write_atomic_json(path, state)
                confirm[0] = not confirm[0]
            return read(path)
        return read_with_publication

    def test_Guard_ContinuouslyMovingState_RefusesAfterTenReads(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            publication = self.moving_guard_state(instance, writer, state, event)
            with patch.object(Path, 'read_bytes', publication), patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep') as pause:
                with self.assertRaisesRegex(ValueError, 'guard state did not stabilize'):
                    instance.guard_ok()
            self.assertEqual(pause.call_count, 9)
            self.assertEqual(instance.log[0]['attempts'], 10)

    def test_Guard_StateAdvancesDuringConfirmation_ThenStabilizes(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            publication = self.moving_guard_state(instance, writer, state, event)
            read, state_reads = Path.read_bytes, [0]
            def publish_once(path):
                if path.name == 'completed-guard.state.json':
                    state_reads[0] += 1
                    if state_reads[0] <= 2:
                        return publication(path)
                return read(path)
            with patch.object(Path, 'read_bytes', publish_once), patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep') as pause:
                self.assertEqual(instance.guard_ok(), 2)
            self.assertEqual(pause.call_count, 1)
            self.assertEqual(instance.log[0]['attempts'], 2)

    def test_Guard_PersistentInvalidGap_RefusesWithoutRetry(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            writer.append_event(instance.live / 'completed-guard-events.jsonl', dict(event, sample_number=2))
            with patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep') as pause:
                with self.assertRaisesRegex(ValueError, 'guard event/state disagree'):
                    instance.guard_ok()
            pause.assert_not_called()

    def test_Guard_InvalidPair_RefusesWithoutRetry(self):
        stale = (recovery.dt.datetime.now(recovery.dt.timezone.utc) - recovery.dt.timedelta(seconds=30)).isoformat()
        future = (recovery.dt.datetime.now(recovery.dt.timezone.utc) + recovery.dt.timedelta(seconds=30)).isoformat()
        for side, field, value in (
            ('event', 'sample_number', 3), ('event', 'sample_number', 0),
            ('event', 'utc', future), ('state', 'last_sample_utc', stale),
            ('event', 'utc', stale), ('state', 'state', 'stopped'),
            ('event', 'verdict', 'stop'), ('event', 'session_id', 'changed'),
            ('state', 'session_id', 'changed'), ('event', 'expected_sha256', 'changed'),
            ('state', 'expected_sha256', 'changed')):
            with self.subTest(side=side, field=field), tempfile.TemporaryDirectory() as temporary:
                instance, writer, state, event = self.guard_fixture(Path(temporary))
                (event if side == 'event' else state)[field] = value
                writer.write_atomic_json(instance.live / 'completed-guard.state.json', state)
                (instance.live / 'completed-guard-events.jsonl').write_bytes(writer.canonical_bytes(event))
                with patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep') as pause:
                    with self.assertRaises(ValueError):
                        instance.guard_ok()
                pause.assert_not_called()

    def test_Guard_SameSampleDifferentUtc_RefusesWithoutRetry(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            event['utc'] = (recovery.dt.datetime.now(recovery.dt.timezone.utc) + recovery.dt.timedelta(milliseconds=1)).isoformat()
            (instance.live / 'completed-guard-events.jsonl').write_bytes(writer.canonical_bytes(event))
            with patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep') as pause:
                with self.assertRaisesRegex(ValueError, 'guard event/state disagree'):
                    instance.guard_ok()
            pause.assert_not_called()

    def test_Guard_MalformedCompleteEvent_RefusesWithoutRetry(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            with (instance.live / 'completed-guard-events.jsonl').open('ab') as output:
                output.write(b'{malformed}\n')
            with patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep') as pause:
                with self.assertRaises(json.JSONDecodeError):
                    instance.guard_ok()
            pause.assert_not_called()

    def test_Guard_BindingStopStalenessOrRegressionDuringRetry_Refuses(self):
        for change in ('binding', 'stop', 'stale', 'regression', 'exit'):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as temporary:
                instance, writer, state, event = self.guard_fixture(Path(temporary))
                publication = self.moving_guard_state(instance, writer, state, event)
                def publish_invalid(_seconds):
                    state['completed_samples'] = 2
                    if change == 'binding':
                        state['expected_sha256'] = 'changed'
                    elif change == 'stop':
                        state['state'] = 'stopped'
                    elif change == 'stale':
                        state['last_sample_utc'] = (recovery.dt.datetime.now(recovery.dt.timezone.utc) - recovery.dt.timedelta(seconds=30)).isoformat()
                    elif change == 'regression':
                        state['completed_samples'] = event['sample_number'] = 0
                        (instance.live / 'completed-guard-events.jsonl').write_bytes(writer.canonical_bytes(event))
                    else:
                        instance.guard.poll.return_value = 0
                    writer.write_atomic_json(instance.live / 'completed-guard.state.json', state)
                with patch.object(Path, 'read_bytes', publication), patch.object(recovery.time, 'monotonic', return_value=1), patch.object(recovery.time, 'sleep', side_effect=publish_invalid) as pause:
                    with self.assertRaises(ValueError):
                        instance.guard_ok()
                self.assertEqual(pause.call_count, 1)

    def test_Guard_ConsistencyAbsoluteAndCallerDeadline_RefuseWithinBudget(self):
        for now, caller_end, expected_end in ((1, None, 1.5), (599.9, None, 600), (1, 1.075, 1.075)):
            with self.subTest(now=now, caller_end=caller_end), tempfile.TemporaryDirectory() as temporary:
                instance, writer, state, event = self.guard_fixture(Path(temporary))
                publication = self.moving_guard_state(instance, writer, state, event)
                clock = [now]
                def advance(seconds):
                    clock[0] += seconds
                with patch.object(Path, 'read_bytes', publication), patch.object(recovery.time, 'monotonic', side_effect=lambda: clock[0]), patch.object(recovery.time, 'sleep', side_effect=advance):
                    with self.assertRaises(ValueError):
                        instance.guard_ok(deadline=caller_end)
                self.assertLessEqual(clock[0], expected_end)
                self.assertLessEqual(instance.log[0]['attempts'], 10)

    def test_Ack_IntentVisibleBeforeAtomicOwnerPublication_RefusesThenAccepts(self):
        for label in ('delete-debs', 'guard'):
            with self.subTest(label=label), tempfile.TemporaryDirectory() as temporary:
                instance = object.__new__(recovery.Recovery)
                instance.live, instance.session = Path(temporary), 'ack-fixture'
                instance.publish_intent(label, recovery.TARGET, ['offline'])
                owner = instance.live / ('guard.owner.txt' if label == 'guard' else label + '.entry.txt')
                with self.assertRaisesRegex(ValueError, 'lacks ownership acknowledgement'):
                    instance.assert_ack(label)
                pending = owner.with_suffix('.pending')
                pending.write_text('pid=12\npgid=12\nsid=12\nnice=10\nCpus_allowed_list=0-1\n')
                recovery.os.replace(pending, owner)
                instance.assert_ack(label)
                self.assertEqual((instance.live / ('owner-records/000-' + label + '.txt')).read_bytes(), owner.read_bytes())

    def test_HostAck_NonnumericOrMissingPid_RefusesAndKeepsPendingIntent(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live = Path(temporary)
            instance.host_intent(['offline'], 'fixture', 1)
            for pid in (None, '12', 0, True):
                with self.subTest(pid=pid), self.assertRaisesRegex(ValueError, 'numeric ownership acknowledgement'):
                    instance.host_ack(SimpleNamespace(pid=pid), 'fixture', 1)
            intent = json.loads((instance.live / 'launch-records/001-fixture.intent.json').read_bytes())
            self.assertEqual(intent['host_ack'], 'pending')
            self.assertFalse((instance.live / 'launch-records/001-fixture.ack.json').exists())

    def test_Guard_DeadlineExpiresDuringMatchingRead_Refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            read = Path.read_bytes
            clock = [599.9]
            def delayed_read(path):
                data = read(path)
                if path.name == 'completed-guard-events.jsonl':
                    clock[0] = 600
                return data
            with patch.object(recovery.time, 'monotonic', side_effect=lambda: clock[0]), patch.object(Path, 'read_bytes', delayed_read):
                with self.assertRaisesRegex(ValueError, '600-second recovery deadline exhausted'):
                    instance.guard_ok()

    def test_Guard_ExitBeforeMatchingReturn_Refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance, writer, state, event = self.guard_fixture(Path(temporary))
            instance.guard.poll.side_effect = [None, 0]
            with patch.object(recovery.time, 'monotonic', return_value=1):
                with self.assertRaisesRegex(ValueError, 'guard exited/missing'):
                    instance.guard_ok()

    def test_ProtectedInvariance_PropagatesLocalDeadlineToGuardRead(self):
        instance = object.__new__(recovery.Recovery)
        instance.start, instance.before = 0, 'hash'
        instance.guard_ok = Mock(side_effect=[1, 2, 2])
        instance.snapshot = Mock(return_value='hash')
        with patch.object(recovery.time, 'monotonic', return_value=1):
            instance.action('protected-invariance')
        self.assertEqual(instance.guard_ok.call_args_list[1].kwargs, {'deadline': 13})

    def test_Command_ExpiredBudget_NeverLaunches(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.start = 0
            instance.live = Path(temporary)
            instance.repo = Path(temporary)
            instance.index, instance.log = 0, []
            child = Mock()
            child.poll.return_value = 0
            child.returncode = 0
            with patch.object(recovery.time, 'monotonic', return_value=601), patch.object(recovery.subprocess, 'Popen', return_value=child) as launch:
                with self.assertRaises(ValueError):
                    instance.command(['wsl.exe', '--unregister', recovery.TARGET], 'unregister')
                launch.assert_not_called()

    def test_Absence_RenamedMovedTargetGuid_Refuses(self):
        protected = {'name': recovery.PROTECTED, 'key': 'protected', 'path': recovery.PROTECTED_PATH}
        surviving = {'name': 'renamed', 'key': recovery.TARGET_KEY, 'path': 'moved'}
        with self.assertRaises(ValueError):
            recovery.validate_absence([protected, surviving], [recovery.PROTECTED, 'renamed'], False, protected)

    def test_Receipt_DeletionAndFinalReapFailure_RetainsBothInOrder(self):
        def action(name):
            if name == 'delete-debs':
                raise ValueError('initiating-delete-failure')
            if name == 'final-owned-check':
                raise ValueError('final-check-failure')
        with self.assertRaises(ValueError) as caught:
            recovery.cleanup_sequence(action)
        self.assertIn('initiating-delete-failure', str(caught.exception))
        self.assertIn('final-check-failure', str(caught.exception))
        self.assertLess(str(caught.exception).index('initiating-delete-failure'), str(caught.exception).index('final-check-failure'))

    def test_Guard_ExpiredBudget_NeverLaunches(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.start, instance.session = 0, 'recovery-fixture'
            instance.live = instance.freeze = instance.packet = Path(temporary)
            instance.snapshot = Mock(return_value='hash')
            instance.linux_path = lambda path: str(path)
            child = Mock()
            child.poll.return_value = 0
            with patch.object(recovery.time, 'monotonic', return_value=601), patch.object(recovery, 'digest', return_value='hash'), patch.object(recovery.subprocess, 'Popen', return_value=child) as launch:
                with self.assertRaises(ValueError):
                    instance.start_guard()
                launch.assert_not_called()

    def test_Linux_MissingEntryAck_RefusesAndRetainsIntent(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live = instance.packet = Path(temporary)
            instance.session = 'recovery-fixture'
            instance.linux_argv = Mock(return_value=['offline'])
            instance.command = Mock(return_value=b'ok')
            with self.assertRaises(ValueError):
                instance.linux(recovery.TARGET, 'delete-debs', ['offline'])
            self.assertTrue((instance.live / 'delete-debs.intent.json').is_file())

    def test_Lifecycle_UnstableIdentity_NeverLaunches(self):
        instance = object.__new__(recovery.Recovery)
        instance.command = Mock()
        instance.stable_identity = Mock(side_effect=ValueError('unstable'))
        with self.assertRaises(ValueError):
            instance.action('unregister')
        instance.command.assert_not_called()

    def test_Absence_TargetPathWithDifferentNameAndGuid_Refuses(self):
        protected = {'name': recovery.PROTECTED, 'key': 'protected', 'path': recovery.PROTECTED_PATH}
        surviving = {'name': 'renamed', 'key': 'different', 'path': recovery.TARGET_PATH}
        with self.assertRaises(ValueError):
            recovery.validate_absence([protected, surviving], [recovery.PROTECTED, 'renamed'], False, protected)

    def test_Packet_OnlyDeletionBranchChangesAndUsesPinnedHelper(self):
        import subprocess
        relative = 'docs/proof/win-gpu-f1-preflight/packet/preflight.sh'
        original = subprocess.run(['git', 'show', '02ec2cc3:' + relative], cwd=ROOT, check=True, capture_output=True).stdout
        current = (ROOT / relative).read_bytes()
        old_prefix, old_deletion = original.split(b'    delete-debs)\n', 1)
        new_prefix, new_deletion = current.split(b'    delete-debs)\n', 1)
        self.assertEqual(new_prefix, old_prefix)
        self.assertEqual(new_deletion.split(b'    *)', 1)[1], old_deletion.split(b'    *)', 1)[1])
        self.assertIn(b'check_cache.py', new_deletion)
        self.assertIn(b'--mode delete', new_deletion)

    def test_Packet_ScopeControl_RejectsOtherStageMutation(self):
        packet = ROOT / recovery.REL / 'packet'
        with patch.object(sys, 'path', [str(packet), *sys.path]):
            module_spec = importlib.util.spec_from_file_location('packet_selftest', packet / 'packet_selftest.py')
            module = importlib.util.module_from_spec(module_spec)
            sys.modules[module_spec.name] = module
            module_spec.loader.exec_module(module)
            data = (packet / 'preflight.sh').read_bytes()
            module.assert_cleanup_stage_scope(data)
            with self.assertRaises(AssertionError):
                module.assert_cleanup_stage_scope(data.replace(b'    simulate)', b'    altered)', 1))

    def test_Receipt_ThreeFailures_ClassifiesAndPreservesOrder(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live = Path(temporary) / 'live'
            instance.live.mkdir()
            instance.authorization = Path(temporary) / 'authorization.json'
            instance.authorization.write_text('{}')
            instance.errors, instance.log, instance.start = [], [], 0
            messages = ['initiating delete', 'final ownership', 'final absence']
            failure = recovery.RecoveryFailure(list(zip(('delete-debs', 'final-owned-check', 'absence'), map(ValueError, messages))))
            instance.retain_failure('cleanup', failure)
            with patch.object(recovery.time, 'monotonic', return_value=1):
                value = instance.receipt(False, 'pending')
            self.assertEqual([error['phase'] for error in value['errors']], ['delete-debs', 'final-owned-check', 'absence'])
            self.assertEqual(value['initiating_error'], value['errors'][0])
            self.assertNotIn('initiating delete', json.dumps(value))
            private = [json.loads(line) for line in (instance.live.parent / 'private/errors.jsonl').read_text().splitlines()]
            self.assertEqual([error['raw_error'] for error in private], messages)

    def test_Binding_ExpiredBudget_NoLaunchAndExplicitIncompleteReceipt(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.repo = Path(temporary)
            instance.live = instance.repo / recovery.REL / 'recovery-1/live'
            instance.live.mkdir(parents=True)
            instance.authorization = instance.repo / 'authorization.json'
            instance.authorization.write_text('{}')
            instance.errors, instance.log, instance.start, instance.index = [], [], 0, 0
            with patch.object(recovery.time, 'monotonic', return_value=601), patch.object(recovery.subprocess, 'Popen') as launch:
                with self.assertRaises(ValueError):
                    instance.bind_evidence(True)
                launch.assert_not_called()
            receipt = json.loads((instance.live / 'recovery-receipt.json').read_bytes())
            self.assertIn('incomplete', receipt['evidence_binding'])
            self.assertFalse(receipt['recovery_success'])
            self.assertEqual(receipt['errors'][0]['phase'], 'evidence-binding')

    def test_Command_ExpiryAfterCaptureSetup_NeverLaunches(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.start, instance.index, instance.log = 0, 0, []
            instance.live = instance.repo = Path(temporary)
            with patch.object(recovery.time, 'monotonic', side_effect=[590, 590, 600]), patch.object(recovery.subprocess, 'Popen') as launch:
                with self.assertRaises(ValueError):
                    instance.command(['offline'], 'fixture')
                launch.assert_not_called()

    def test_Ack_StaleOrPartialOwner_Refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live, instance.session = Path(temporary), 'fixture'
            owner = instance.live / 'delete-debs.entry.txt'
            owner.write_text('pid=11\npgid=11\nsid=11\nnice=10\nCpus_allowed_list=0-1\n')
            instance.publish_intent('delete-debs', recovery.TARGET, ['offline'])
            with self.assertRaises(ValueError):
                instance.assert_ack('delete-debs')
            owner.write_text('pid=12\npgid=12\n')
            with self.assertRaises(ValueError):
                instance.assert_ack('delete-debs')
            owner.write_text('pid=12\npgid=12\nsid=12\nnice=10\nCpus_allowed_list=0-1\n')
            instance.assert_ack('delete-debs')

    def test_StableAdmission_TwoDifferentObservations_Refuses(self):
        instance = object.__new__(recovery.Recovery)
        instance.remaining = Mock(return_value=10)
        observations = iter([('admitted',), ('changed',)])
        def observe():
            instance.identity = next(observations)
        instance.fresh_identity = Mock(side_effect=observe)
        instance.path_identities = Mock(return_value=('same-path',))
        with self.assertRaises(ValueError):
            instance.stable_identity()
        self.assertEqual(instance.fresh_identity.call_count, 2)

    def test_PinnedUnlink_ChangedFile_RefusesAndExactFdUsed(self):
        cache_spec = importlib.util.spec_from_file_location('cache', SCRIPT.parent / 'check_cache.py')
        cache = importlib.util.module_from_spec(cache_spec)
        sys.modules[cache_spec.name] = cache
        cache_spec.loader.exec_module(cache)
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'package.deb'
            path.write_bytes(b'archive')
            observed = path.stat()
            expected = (observed.st_dev, observed.st_ino, observed.st_mode, observed.st_size, observed.st_mtime_ns)
            changed = SimpleNamespace(st_dev=observed.st_dev, st_ino=observed.st_ino + 1, st_mode=observed.st_mode,
                                      st_size=observed.st_size, st_mtime_ns=observed.st_mtime_ns)
            with patch.object(cache.os, 'unlink') as unlink:
                with self.assertRaises(ValueError):
                    cache.unlink_verified('package.deb', 41, expected, changed)
                unlink.assert_not_called()
                cache.unlink_verified('package.deb', 41, expected, observed)
                unlink.assert_called_once_with('package.deb', dir_fd=41)

    def test_GuardStop_MultipleErrors_RetainsAllWithNoLaunch(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live, instance.session, instance.start = Path(temporary), 'fixture', 0
            instance.guard = Mock()
            instance.guard.wait.side_effect = ValueError('initial wait')
            instance.guard.poll.return_value = None
            instance.guard.returncode = None
            instance.guard_out, instance.guard_err = io.BytesIO(), io.BytesIO()
            instance.owned = Mock(side_effect=[ValueError('owned stop'), ValueError('owned check')])
            instance.assert_ack = Mock(side_effect=ValueError('ack unknown'))
            instance.publish_capture = Mock()
            with patch.object(recovery.time, 'monotonic', return_value=601), patch.object(recovery.subprocess, 'Popen') as launch:
                with self.assertRaises(recovery.RecoveryFailure) as caught:
                    instance.stop_guard()
                launch.assert_not_called()
            phases = [phase for phase, error in caught.exception.failures]
            self.assertEqual(phases, ['guard-stop-request/wait', 'guard-launcher-reap', 'guard-owned-stop',
                                      'guard-owned-check', 'guard-acknowledgement', 'guard-exit'])

    def test_GuardStop_CapturePublicationFailure_DoesNotOverwriteEarlierErrors(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live, instance.session, instance.start = Path(temporary), 'fixture', 0
            instance.guard = Mock()
            instance.guard.wait.side_effect = ValueError('first wait')
            instance.guard.poll.return_value = 0
            instance.guard.returncode = 5
            instance.guard_out, instance.guard_err = io.BytesIO(), io.BytesIO()
            instance.owned = Mock(side_effect=[ValueError('owned stop'), ValueError('owned check')])
            instance.assert_ack = Mock(side_effect=ValueError('ack unknown'))
            instance.publish_capture = Mock(side_effect=ValueError('capture publication'))
            with patch.object(recovery.time, 'monotonic', return_value=10):
                with self.assertRaises(recovery.RecoveryFailure) as caught:
                    instance.stop_guard()
            phases = [phase for phase, error in caught.exception.failures]
            self.assertEqual(phases[0], 'guard-stop-request/wait')
            self.assertIn('guard-owned-stop', phases)
            self.assertIn('guard-capture.stdout.txt', phases)
            self.assertIn('guard-capture.stderr.txt', phases)

    def test_NativeChild_IntentPrecedesLaunchAndRealPidAcknowledged(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.repo = Path(temporary)
            instance.live = instance.repo / 'live'
            instance.live.mkdir()
            instance.start, instance.index, instance.log = recovery.time.monotonic(), 0, []
            native_launch = recovery.subprocess.Popen
            def launch(argv, **kwargs):
                self.assertTrue((instance.live / 'launch-records/001-native.intent.json').is_file())
                return native_launch(argv, **kwargs)
            with patch.object(recovery.subprocess, 'Popen', side_effect=launch):
                output = instance.command([sys.executable, '-c', "print('OFFLINE-NATIVE')"], 'native', 60)
            self.assertEqual(output.strip(), b'OFFLINE-NATIVE')
            ack = json.loads((instance.live / 'launch-records/001-native.ack.json').read_bytes())
            self.assertGreater(ack['host_pid'], 0)
            public = json.loads((instance.live / '001-native.stdout.txt').read_bytes())
            self.assertEqual(public['schema'], 'cfdw-classified-subprocess-capture/1')
            self.assertNotIn('OFFLINE-NATIVE', (instance.live / '001-native.stdout.txt').read_text())

    def test_Guard_ExpiryAfterCaptureSetup_NeverLaunchesAndClosesStreams(self):
        with tempfile.TemporaryDirectory() as temporary:
            instance = object.__new__(recovery.Recovery)
            instance.live = Path(temporary) / 'live'
            instance.live.mkdir()
            instance.freeze = instance.packet = Path(temporary)
            instance.start, instance.session, instance.index = 0, 'fixture', 0
            instance.snapshot = Mock(return_value='hash')
            instance.linux_path = lambda path: str(path)
            with patch.object(recovery.time, 'monotonic', side_effect=[590, 590, 600]), patch.object(recovery, 'digest', return_value='hash'), patch.object(recovery.subprocess, 'Popen') as launch:
                with self.assertRaises(ValueError):
                    instance.start_guard()
                launch.assert_not_called()
            self.assertTrue(instance.guard_out.closed)
            self.assertTrue(instance.guard_err.closed)

    def test_Authorization_AbsentOrProposed_Refuses(self):
        for value in ({}, {'status': 'proposed'}):
            with self.subTest(value=value), self.assertRaises(ValueError):
                recovery.validate_authorization(value, 'abc', 's', 7)

    def test_Authorization_ExactAcceptedContract_Passes(self):
        value = recovery.authorization_example('abc', 's', 7)
        recovery.validate_authorization(value, 'abc', 's', 7)

    def test_Authorization_MutatedBinding_Refuses(self):
        for field in ('source_commit', 'session', 'leader_epoch', 'freeze_sha256',
                      'target', 'target_path', 'protected', 'original_run_id', 'ruling'):
            value = recovery.authorization_example('abc', 's', 7)
            value[field] = 'wrong'
            with self.subTest(field=field), self.assertRaises(ValueError):
                recovery.validate_authorization(value, 'abc', 's', 7)

    def test_Identity_MissingDuplicateWrongPath_Refuses(self):
        target = {'name': recovery.TARGET, 'key': '{target}', 'path': recovery.TARGET_PATH, 'version': 2}
        protected = {'name': recovery.PROTECTED, 'key': '{protected}', 'path': recovery.PROTECTED_PATH, 'version': 2}
        for rows in ([protected], [target, target, protected],
                     [dict(target, path=recovery.PROTECTED_PATH), protected],
                     [target, dict(protected, path=recovery.TARGET_PATH)]):
            with self.subTest(rows=rows), self.assertRaises(ValueError):
                recovery.validate_identity(rows, [recovery.TARGET, recovery.PROTECTED])

    def test_Identity_ExactRegistrations_Passes(self):
        rows = [{'name': recovery.TARGET, 'key': 'a', 'path': recovery.TARGET_PATH, 'version': 2},
                {'name': recovery.PROTECTED, 'key': 'b', 'path': recovery.PROTECTED_PATH, 'version': 2}]
        self.assertEqual(recovery.validate_identity(rows, [recovery.TARGET, recovery.PROTECTED])[0]['key'], 'a')

    def test_Freeze_ChangedManifestOrLinkedInput_Refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'capture-manifest.json').write_text('{}')
            with self.assertRaises(ValueError):
                recovery.validate_freeze(root)

    def test_Plan_CheckpointFailure_NoDestructiveCall(self):
        calls = []
        def action(name):
            calls.append(name)
            if name == 'verify-checkpoint':
                raise ValueError('unbound')
        with self.assertRaises(ValueError):
            recovery.cleanup_sequence(action)
        self.assertEqual(calls, ['verify-checkpoint'])

    def test_Plan_DeletionFailure_FinalReapNoUnregister(self):
        calls = []
        def action(name):
            calls.append(name)
            if name == 'delete-debs':
                raise ValueError('failed')
        with self.assertRaises(ValueError):
            recovery.cleanup_sequence(action)
        self.assertIn('final-owned-check', calls)
        self.assertEqual(calls[-1], 'absence')
        self.assertNotIn('unregister', calls)

    def test_Plan_FinalReapFailure_NoUnregister(self):
        calls = []
        def action(name):
            calls.append(name)
            if name == 'final-owned-check':
                raise ValueError('unknown')
        with self.assertRaises(ValueError):
            recovery.cleanup_sequence(action)
        self.assertNotIn('unregister', calls)

    def test_Plan_GuardOrIdentityFailure_NoDeletion(self):
        for failed in ('fresh-identity', 'old-owned-check', 'guard-ready'):
            calls = []
            def action(name):
                calls.append(name)
                if name == failed:
                    raise ValueError('blocked')
            with self.subTest(failed=failed), self.assertRaises(ValueError):
                recovery.cleanup_sequence(action)
            self.assertNotIn('delete-debs', calls)

    def test_Absence_AnyResidualOrProtectedChange_Refuses(self):
        protected = {'name': recovery.PROTECTED, 'key': 'protected'}
        for rows, names, exists in (([protected, {'name': recovery.TARGET}], [recovery.PROTECTED], False),
                                    ([protected], [recovery.PROTECTED, recovery.TARGET], False),
                                    ([protected], [recovery.PROTECTED], True),
                                    ([dict(protected, key='changed')], [recovery.PROTECTED], False)):
            with self.subTest(rows=rows, names=names, exists=exists), self.assertRaises(ValueError):
                recovery.validate_absence(rows, names, exists, protected)
        recovery.validate_absence([protected], [recovery.PROTECTED], False, protected)

    def test_Lifecycle_ActualAction_OnlyExactTargetArgv(self):
        instance = object.__new__(recovery.Recovery)
        instance.command = Mock()
        instance.stable_identity = Mock()
        for action in ('terminate', 'unregister'):
            instance.action(action)
            self.assertEqual(instance.command.call_args.args[0], ['wsl.exe', '--' + action, recovery.TARGET])
            self.assertTrue(instance.command.call_args.args[3])

    def test_Deletion_ActualAction_OnlyReviewedStageAndCache(self):
        instance = object.__new__(recovery.Recovery)
        instance.repo = ROOT
        instance.packet = ROOT / recovery.REL / 'packet'
        instance.live = ROOT / recovery.REL / 'recovery-1/live'
        instance.linux = Mock()
        instance.stable_identity = Mock()
        instance.linux_path = lambda path: '/offline/' + path.as_posix()
        instance.delete_debs()
        self.assertEqual(instance.linux.call_count, 2)
        admission, deletion = instance.linux.call_args_list
        self.assertEqual(admission.args[0], recovery.TARGET)
        self.assertTrue(admission.args[-1])
        argv = deletion.args[2]
        self.assertEqual(argv[argv.index('--stage') + 1], 'delete-debs')
        self.assertEqual(argv[argv.index('--run-id') + 1], recovery.RUN_ID)
        self.assertEqual(deletion.args[0], recovery.TARGET)
        self.assertTrue(deletion.args[-1])

    def test_Checkpoint_ActualCanonicalFreeze_Validates75Files(self):
        recovery.validate_freeze(ROOT / recovery.REL / 'run-1/checkpoint')

    def test_Cache_LinkMountUnexpectedEntry_Refuses(self):
        cache_spec = importlib.util.spec_from_file_location('cache', SCRIPT.parent / 'check_cache.py')
        cache = importlib.util.module_from_spec(cache_spec)
        sys.modules[cache_spec.name] = cache
        cache_spec.loader.exec_module(cache)
        with tempfile.TemporaryDirectory() as temporary:
            # The validator refuses a linked ancestor by design; on macOS /var is a symlink to /private/var, so the
            # fixture root must be the resolved path or the "ancestry is linked" refusal fires on the fixture itself.
            root = Path(temporary).resolve()
            store = root / 'cache'
            store.mkdir()
            mounts = root / 'mountinfo'
            mounts.write_text('')
            (store / 'unexpected.txt').write_text('x')
            with self.assertRaises(ValueError):
                cache.validate_cache(store, Path(store.anchor), mounts)
            (store / 'unexpected.txt').unlink()
            mounts.write_text('1 2 0:3 / ' + store.as_posix() + ' rw - ext4 root rw\n')
            with self.assertRaises(ValueError):
                cache.validate_cache(store, Path(store.anchor), mounts)
            mounts.write_text('')
            (store / 'package.deb').write_bytes(b'archive')
            with contextlib.redirect_stdout(io.StringIO()):
                cache.validate_cache(store, Path(store.anchor), mounts)

    def test_PublicProjection_ProtectedLocalPath_Redacts(self):
        rows = [{'name': recovery.PROTECTED, 'path': 'private-local-path', 'key': 'a', 'version': 2}]
        self.assertEqual(recovery.public_registrations(rows)[0]['path'], recovery.PROTECTED_PATH)
        self.assertTrue(recovery.PROTECTED_PATH.startswith('%USERPROFILE%'))
        with patch.dict(recovery.os.environ, {'USERPROFILE': 'synthetic-profile'}):
            self.assertEqual(recovery.public_value({'argv': ['synthetic-profile/python']}),
                             {'argv': ['%USERPROFILE%/python']})


if __name__ == '__main__':
    unittest.main()
