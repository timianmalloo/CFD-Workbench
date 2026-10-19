"""Plan docs/plans/f1-rescope-r237.md node C3 (Ruling 237 (1), Ruling 240): the roots are pins, the lock is produced per
architecture by the run that resolves them, and the frozen B2 amd64 lock leaves the packet's input set.

Ring: offline-fast (no apt, no network, no WSL); the fixtures are the committed B2 simulate output and the rows of the
plan's section 4.2. Red observed before C3: transaction.roots required --b2-lock, transaction.check_pins did not exist.
"""
import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
B2 = ROOT / 'docs/proof/win-gpu-b2'
B2_LOCK_SHA256 = '2306d58e2fb0d728195ee2691cb7faab7c651e04bb70746f242310834d61e76c'
sys.path.insert(0, str(PACKET))
import architecture  # noqa: E402
import attempt  # noqa: E402
import contract  # noqa: E402
import transaction  # noqa: E402

# The pins resolved on arm64 by lock-resolution-arm64.json (arm64_versions, highest), plus the four rows of plan
# section 4.2 whose arm64 identity differs from the amd64 lock (the native compiler internals carry the aarch64
# triplet; linux-libc-dev is at 147.147 because noble-updates dropped 146.146).
ARM64_RESOLVED = {
    'cuda-toolkit-13-2': '13.2.2-1', 'openfoam2512': '2512.0-2', 'openfoam2512-common': '2512.0-2',
    'openfoam2512-dev': '2512.0-2', 'openfoam2512-source': '2512.0-2', 'openfoam2512-tools': '2512.0-2',
    'gcc-13': '13.3.0-6ubuntu2~24.04.1', 'g++-13': '13.3.0-6ubuntu2~24.04.1', 'gfortran-13': '13.3.0-6ubuntu2~24.04.1',
    'make': '4.3-4.1build2', 'openmpi-bin': '4.1.6-7ubuntu2', 'libopenmpi-dev': '4.1.6-7ubuntu2',
    'libblas-dev': '3.12.0-3build1.1', 'liblapack-dev': '3.12.0-3build1.1', 'pkg-config': '1.8.1-2build1',
    'cpp-13-aarch64-linux-gnu': '13.3.0-6ubuntu2~24.04.1', 'gcc-13-aarch64-linux-gnu': '13.3.0-6ubuntu2~24.04.1',
    'g++-13-aarch64-linux-gnu': '13.3.0-6ubuntu2~24.04.1', 'linux-libc-dev': '6.8.0-147.147',
}
# Rows of the amd64 lock that the arm64 index does not carry (section 4.2).
AMD64_ONLY = ('cpp-13-x86-64-linux-gnu', 'gcc-13-x86-64-linux-gnu', 'g++-13-x86-64-linux-gnu',
              'libquadmath0', 'cuda-nsight-13-2', 'cuda-opencl-13-2')


def simulation_text(resolved, token):
    return ''.join(f'Inst {name} ({version} Ubuntu:24.04/noble [{token}])\n' for name, version in resolved.items())


def write(folder, name, text):
    path = Path(folder) / name
    path.write_text(text, encoding='utf-8', newline='\n')
    return path


def roots_for(token, folder):
    output = Path(folder) / 'roots.args'
    transaction.roots(SimpleNamespace(output=output, architecture=token))
    return output.read_text(encoding='utf-8').splitlines()


class TransactionRootsTests(unittest.TestCase):
    def test_Roots_Arm64IndexFixture_Resolves(self):
        with tempfile.TemporaryDirectory() as folder:
            pins = roots_for('arm64', folder)
            self.assertIn('make:arm64', pins)
            self.assertNotIn('make:amd64', pins)
            self.assertEqual(len(pins), len(transaction.PINS) + 1, pins)
            self.assertEqual(sorted(pins), sorted(set(pins)), 'duplicate pin')
            versioned = [pin for pin in pins if '=' in pin]
            self.assertTrue(all(pin.startswith(('cuda-toolkit-13-2=', 'openfoam2512')) for pin in versioned), versioned)
            rows = transaction.simulation_rows(write(folder, 'sim', simulation_text(ARM64_RESOLVED, 'arm64')))
            transaction.check_pins(rows, pins)
            names = {name for name, _old, _version in rows}
            self.assertTrue({'cpp-13-aarch64-linux-gnu', 'gcc-13-aarch64-linux-gnu', 'g++-13-aarch64-linux-gnu'} <= names)
            self.assertEqual(dict((n, v) for n, _o, v in rows)['linux-libc-dev'], '6.8.0-147.147')
            self.assertFalse(names & set(AMD64_ONLY), 'the amd64-only rows must not be asked for on arm64')

    def test_Roots_AbsentPin_Refuses(self):
        with tempfile.TemporaryDirectory() as folder:
            pins = roots_for('arm64', folder)
            absent = dict(ARM64_RESOLVED)
            del absent['openfoam2512-dev']
            rows = transaction.simulation_rows(write(folder, 'absent', simulation_text(absent, 'arm64')))
            with self.assertRaisesRegex(ValueError, 'pinned root absent.*openfoam2512-dev'):
                transaction.check_pins(rows, pins)
            other_version = dict(ARM64_RESOLVED, openfoam2512='2512.0-1')
            rows = transaction.simulation_rows(write(folder, 'other', simulation_text(other_version, 'arm64')))
            with self.assertRaisesRegex(ValueError, 'pinned root absent.*openfoam2512=2512.0-2'):
                transaction.check_pins(rows, pins)
            no_make = dict(ARM64_RESOLVED)
            del no_make['make']
            rows = transaction.simulation_rows(write(folder, 'nomake', simulation_text(no_make, 'arm64')))
            with self.assertRaisesRegex(ValueError, 'pinned root absent.*make:arm64'):
                transaction.check_pins(rows, pins)
            with self.assertRaises(ValueError):
                transaction.roots(SimpleNamespace(output=Path(folder) / 'x.args', architecture='x64'))

    def test_Roots_Amd64B2Fixture_ExpandsTo135Rows(self):
        # The regression oracle: B2's recorded simulate output is the amd64 expansion of the toolkit pin, and its 135
        # names and versions are B2's committed package-lock.json (Test Architect, plan section 10, cycle 2).
        lock_bytes = (B2 / 'package-lock.json').read_bytes()
        self.assertEqual(hashlib.sha256(lock_bytes).hexdigest(), B2_LOCK_SHA256)
        lock = json.loads(lock_bytes)
        self.assertEqual(lock['package_count'], 135)
        self.assertEqual(lock['root'], 'cuda-toolkit-13-2=13.2.2-1')
        rows = transaction.simulation_rows(B2 / 'apt-simulate.stdout.txt')
        self.assertEqual(len(rows), 135)
        self.assertEqual({(name, version) for name, _old, version in rows},
                         {(item['package'], item['version']) for item in lock['packages']})
        with tempfile.TemporaryDirectory() as folder:
            pins = roots_for('amd64', folder)
        self.assertIn(lock['root'], pins)
        self.assertIn('make:amd64', pins)
        transaction.check_pins(rows, [lock['root'], 'make:amd64', 'gcc-13', 'g++-13'])
        # The F1 transaction is not the B2 transaction: the OpenFOAM pins are absent from B2's expansion.
        with self.assertRaisesRegex(ValueError, 'pinned root absent.*openfoam2512'):
            transaction.check_pins(rows, pins)
        self.assertTrue({name for name, _o, _v in rows} >= set(AMD64_ONLY))

    def test_Lock_DocumentCarriesArchitecture_RowArchitectureMustAgree(self):
        def fake_metadata(package, version, arch='arm64'):
            return {'Package': package, 'Version': version, 'Architecture': arch,
                    'Filename': f'pool/main/{package}_{version}_{arch}.deb', 'Size': '7',
                    'SHA256': hashlib.sha256(package.encode()).hexdigest(), 'Installed-Size': '3'}

        with tempfile.TemporaryDirectory() as folder:
            simulation = write(folder, 'apt-simulate.stdout.txt', simulation_text(ARM64_RESOLVED, 'arm64'))
            uris = write(folder, 'apt-uris.stdout.txt', ''.join(
                f"'http://ports.ubuntu.com/ubuntu-ports/pool/main/{n}_{v}_arm64.deb' {n}_{v}_arm64.deb 7 SHA256:x\n"
                for n, v in ARM64_RESOLVED.items()))
            output = Path(folder) / 'transaction-lock.json'
            args = SimpleNamespace(simulation=simulation, uris=uris, output=output,
                                   args=Path(folder) / 'transaction.args', architecture='arm64')
            with patch.object(transaction, 'metadata', fake_metadata):
                transaction.lock(args)
            document = json.loads(output.read_text(encoding='utf-8'))
            self.assertEqual(document['schema'], 'cfdw-f1-r217-transaction-lock/2')
            self.assertEqual(document['architecture'], 'arm64')
            self.assertEqual(document['pins'], list(transaction.pinned_roots('arm64')))
            self.assertEqual(document['package_count'], len(ARM64_RESOLVED))
            self.assertTrue(all(row['architecture'] == 'arm64' for row in document['packages']))
            # A row the index serves for another architecture (a cross package) refuses the lock.
            with patch.object(transaction, 'metadata', lambda p, v: fake_metadata(p, v, 'all' if p == 'make' else 'amd64')):
                with self.assertRaisesRegex(ValueError, 'architecture'):
                    transaction.lock(args)
            # The pin check runs before any index read: an absent pin never reaches apt-cache.
            absent = dict(ARM64_RESOLVED)
            del absent['cuda-toolkit-13-2']
            args.simulation = write(folder, 'absent.txt', simulation_text(absent, 'arm64'))
            with patch.object(transaction, 'metadata', side_effect=AssertionError('index read before the pin check')):
                with self.assertRaisesRegex(ValueError, 'pinned root absent'):
                    transaction.lock(args)

    def test_Inputs_B2LockLeavesTheInputSet(self):
        self.assertNotIn('b2_lock', contract.INPUT_KEYS)
        self.assertIn('b2_licenses', contract.INPUT_KEYS)
        for token in ('amd64', 'arm64'):
            self.assertEqual(set(attempt.input_hashes(token)), set(contract.INPUT_KEYS))
        self.assertNotIn('b2_lock', attempt.INPUT_HASHES)
        self.assertEqual(len(contract.INPUT_KEYS), 7)   # six after C3, plus the cloud-image keyring at C4
        self.assertIn('ubuntu_cloudimage_keyring', contract.INPUT_KEYS)
        source = (PACKET / 'preflight.sh').read_text(encoding='utf-8')
        self.assertNotIn('--b2-lock', source)
        self.assertIn('--b2-licences', source)
        launcher = (PACKET / 'Invoke-Preflight.ps1').read_text(encoding='utf-8')
        self.assertNotIn('b2_lock', launcher)
        self.assertNotIn('package-lock.json', launcher)
        self.assertEqual(architecture.row('amd64')['make_root'], 'make:amd64')


if __name__ == '__main__':
    unittest.main()
