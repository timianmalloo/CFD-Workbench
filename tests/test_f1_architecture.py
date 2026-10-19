"""F1 re-scope node C1 (plan docs/plans/f1-rescope-r237.md, Rulings 237, 238 (1), 240): the host architecture is detected
and recorded, never assumed; one table per architecture; host-identity.json is required evidence.

Ring: offline-fast (tests/test_f1_*.py under tools/run-tests.sh). No WSL, no network; two cases spawn the packet's
Python CLI once each. Cost: measured at the join (see the plan's ledger)."""
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path, PureWindowsPath
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
SPIKE = ROOT / 'docs/proof/win-gpu-f1-preflight/arm64-spike'
B2_LOCK = ROOT / 'docs/proof/win-gpu-b2/package-lock.json'
sys.path.insert(0, str(PACKET))
import architecture
import attempt
import contract
import manifest
import transaction

# The literals are carried here on purpose (plan C1): a table edit must disagree with this file, never with itself.
AMD64_ROOTFS_NAME = 'ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz'
AMD64_ROOTFS_SHA = '8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea'
ARM64_ROOTFS_NAME = 'ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz'
ARM64_ROOTFS_SHA = 'fecec1d9b7b750c12c109edb49c13c1006f4a2efabb9b8bf341f11c4c9f2ef11'
TODAYS_AMD64_SOURCES = [
    'deb [arch=amd64 signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://archive.ubuntu.com/ubuntu noble main universe',
    'deb [arch=amd64 signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://archive.ubuntu.com/ubuntu noble-updates main universe',
    'deb [arch=amd64 signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://security.ubuntu.com/ubuntu noble-security main universe',
    'deb [arch=amd64 signed-by=/etc/apt/keyrings/cfdw-openfoam.gpg] https://dl.openfoam.com/repos/deb noble main',
    'deb [arch=amd64 signed-by=/etc/apt/keyrings/cfdw-nvidia.gpg] https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/ /',
]


def x64_windows():
    # CIM string from the old x64 PC's receipt (docs/proof/win-setup/receipt.md); the rest are the native x64 names.
    return dict(os_architecture_cim='64-bit', os_architecture='X64', process_architecture='X64',
                processor_architecture_env='AMD64', processor_architew6432_env='')


def x64_linux():
    return dict(dpkg_architecture='amd64', uname_m='x86_64')


def receipt(name):
    return json.loads((SPIKE / name).read_text(encoding='utf-8'))


def windows_from(value):
    return dict(os_architecture_cim=value['windows']['os_architecture_cim'],
                os_architecture=value['windows']['os_architecture'],
                process_architecture=value['runtime']['process_architecture'],
                processor_architecture_env=value['windows']['processor_architecture_env'],
                processor_architew6432_env=value['windows']['processor_architew6432_env'])


def linux_from(value):
    inside = value['stock_distro_inside']
    return dict(dpkg_architecture=inside['dpkg_architecture'], uname_m=inside['uname_m'])


def observations(token):
    if token == 'arm64':
        value = receipt('host-identity-evidence.pwsh.json')
        windows, cpu, gpu = windows_from(value), value['windows']['cpu'], value['windows']['gpu']
    else:
        windows, cpu, gpu = x64_windows(), ['Fixture x64 CPU'], ['Fixture NVIDIA GPU']
    return dict(schema='cfdw-f1-host-observations/1', utc='2026-10-19T00:00:00.0000000+00:00',
                powershell_version='5.1.28000.1', powershell_edition='Desktop', os_version='10.0.28000.0',
                cpu=cpu, gpu=gpu, **windows)


def contract_fixture(token):
    value = json.loads((ROOT / 'docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json').read_bytes())
    row = architecture.TABLE[token]
    value.update(schema='cfdw-f1-r217-invocation/2', architecture=token,
                 host_start_floor_bytes=19684091961, host_reserve_bytes=10737418240)
    value['inputs']['rootfs'] = {'path': 'C:\\reviewed\\' + row['rootfs_name'], 'sha256': row['rootfs_sha256']}
    return value


def run_cli(*arguments):
    return subprocess.run([sys.executable, str(PACKET / 'architecture.py'), *arguments],
                          capture_output=True, text=True, encoding='utf-8', timeout=60)


class ArchitectureTests(unittest.TestCase):
    def test_Detect_X64Fixture_Amd64(self):
        self.assertEqual(architecture.detect(x64_windows(), x64_linux()), 'amd64')
        self.assertEqual(architecture.detect(x64_windows()), 'amd64')

    def test_Detect_Arm64Fixture_Arm64(self):
        for name in ('host-identity-evidence.pwsh.json', 'host-identity-evidence.ps51.json'):
            with self.subTest(receipt=name):
                value = receipt(name)
                self.assertEqual(architecture.detect(windows_from(value), linux_from(value)), 'arm64')

    def test_Detect_WindowsWslMismatch_Refuses(self):
        arm64 = windows_from(receipt('host-identity-evidence.pwsh.json'))
        cases = (
            ('x64 host, arm64 guest', x64_windows(), dict(dpkg_architecture='arm64', uname_m='aarch64')),
            ('arm64 host, amd64 guest', arm64, x64_linux()),
            ('guest dpkg and uname disagree', arm64, dict(dpkg_architecture='arm64', uname_m='x86_64')),
            # Measured 2026-10-19 on the arm64 laptop: Windows PowerShell 5.1 started from an x64 parent (ARM64X image)
            # reports an x64 process, an x64 OS and AMD64 with no PROCESSOR_ARCHITEW6432; only CIM says ARM 64-bit.
            ('x64-emulated Windows PowerShell 5.1 on arm64',
             dict(os_architecture_cim='ARM 64-bit Processor', os_architecture='X64', process_architecture='X64',
                  processor_architecture_env='AMD64', processor_architew6432_env=''), None),
            # Measured the same day: the SysWOW64 (x86) PowerShell on the same host.
            ('x86 WOW64 PowerShell on arm64',
             dict(os_architecture_cim='ARM 64-bit Processor', os_architecture='Arm64', process_architecture='X86',
                  processor_architecture_env='x86', processor_architew6432_env='ARM64'), None),
            ('environment disagrees', dict(x64_windows(), processor_architecture_env='ARM64'), None),
            ('unmapped CIM string', dict(x64_windows(), os_architecture_cim='32-bit'), None),
            ('empty', dict(os_architecture_cim='', os_architecture='', process_architecture='',
                           processor_architecture_env='', processor_architew6432_env=''), None),
            ('absent', {}, None),
        )
        for label, windows, linux in cases:
            with self.subTest(case=label), self.assertRaises(ValueError):
                architecture.detect(windows, linux)

    def test_Table_Amd64Row_EqualsLiterals(self):
        self.assertEqual(set(architecture.TABLE), {'amd64', 'arm64'})
        row = architecture.TABLE['amd64']
        self.assertEqual(row['rootfs_name'], AMD64_ROOTFS_NAME)
        self.assertEqual(row['rootfs_sha256'], AMD64_ROOTFS_SHA)
        self.assertEqual(row['ubuntu_archive'], 'http://archive.ubuntu.com/ubuntu')
        self.assertEqual(row['ubuntu_security'], 'http://security.ubuntu.com/ubuntu')
        self.assertEqual(row['nvidia_repository'], 'https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/')
        self.assertEqual(row['make_root'], 'make:amd64')
        self.assertEqual(architecture.native_compiler_packages('amd64'),
                         ('cpp-13-x86-64-linux-gnu', 'gcc-13-x86-64-linux-gnu', 'g++-13-x86-64-linux-gnu'))

    def test_Table_Arm64Row_EqualsLiterals(self):
        row = architecture.TABLE['arm64']
        self.assertEqual(row['rootfs_name'], ARM64_ROOTFS_NAME)
        self.assertEqual(row['rootfs_sha256'], ARM64_ROOTFS_SHA)
        self.assertEqual(row['ubuntu_archive'], 'http://ports.ubuntu.com/ubuntu-ports')
        self.assertEqual(row['ubuntu_security'], 'http://ports.ubuntu.com/ubuntu-ports')
        self.assertEqual(row['nvidia_repository'], 'https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/sbsa/')
        self.assertEqual(row['make_root'], 'make:arm64')
        self.assertEqual(architecture.native_compiler_packages('arm64'),
                         ('cpp-13-aarch64-linux-gnu', 'gcc-13-aarch64-linux-gnu', 'g++-13-aarch64-linux-gnu'))
        # The committed spike receipts agree with the literals: sources-evidence.json (rootfs index) and the stock
        # distro's apt URI line (ports host).
        entries = receipt('sources-evidence.json')['ubuntu_wsl_rootfs']['entries']
        self.assertEqual(entries[ARM64_ROOTFS_NAME], ARM64_ROOTFS_SHA)
        self.assertEqual(entries[AMD64_ROOTFS_NAME], AMD64_ROOTFS_SHA)
        uris = receipt('host-identity-evidence.pwsh.json')['stock_distro_inside']['apt_sources_uris']
        self.assertIn(row['ubuntu_archive'] + '/', uris)

    def test_Sources_Amd64_EqualsTodaysFiveLines(self):
        self.assertEqual(architecture.sources('amd64'), TODAYS_AMD64_SOURCES)

    def test_Sources_Arm64_PortsAndSbsa(self):
        lines = architecture.sources('arm64')
        self.assertEqual(len(lines), 5)
        for line in lines:
            self.assertTrue(line.startswith('deb [arch=arm64 signed-by='), line)
            for forbidden in ('x86_64', 'archive.ubuntu.com', 'security.ubuntu.com', 'amd64'):
                self.assertNotIn(forbidden, line)
        for index, suite in enumerate(('noble main universe', 'noble-updates main universe', 'noble-security main universe')):
            self.assertTrue(lines[index].endswith('http://ports.ubuntu.com/ubuntu-ports ' + suite), lines[index])
        self.assertIn('https://dl.openfoam.com/repos/deb noble main', lines[3])
        self.assertIn('https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/sbsa/ /', lines[4])
        with self.assertRaises(ValueError):
            architecture.sources('x64')

    def test_Manifest_FreezeWithoutHostIdentity_Fails(self):
        with tempfile.TemporaryDirectory() as temporary:
            live, frozen = Path(temporary) / 'live', Path(temporary) / 'frozen'
            live.mkdir()
            (live / 'a.txt').write_text('one\n', encoding='utf-8')
            with self.assertRaises(ValueError):
                manifest.freeze(live, frozen)
            self.assertFalse(frozen.exists())
            (live / 'host-identity.json').write_text('{"architecture": "arm64"}\n', encoding='utf-8')
            manifest.freeze(live, frozen)
            captured = {row['path'] for row in json.loads((frozen / 'capture-manifest.json').read_bytes())['files']}
            self.assertEqual(captured, {'a.txt', 'host-identity.json'})

    def test_HostIdentityCli_RecordsCpuGpu_RefusesOtherArchitecture(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, output = root / 'host-observations.json', root / 'host-identity.json'
            source.write_text(json.dumps(observations('amd64')), encoding='utf-8')
            result = run_cli('host-identity', '--observations', str(source), '--output', str(output),
                             '--expected-architecture', 'amd64')
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn('HOST-IDENTITY-PASS architecture=amd64', result.stdout)
            written = json.loads(output.read_text(encoding='utf-8'))
            self.assertEqual(written['schema'], 'cfdw-f1-host-identity/1')
            self.assertEqual(written['architecture'], 'amd64')
            self.assertEqual(written['cpu'], ['Fixture x64 CPU'])
            self.assertEqual(written['gpu'], ['Fixture NVIDIA GPU'])
            self.assertEqual(written['windows']['os_architecture'], 'X64')
            self.assertIsNone(written['refusal'])
            output.unlink()
            result = run_cli('host-identity', '--observations', str(source), '--output', str(output),
                             '--expected-architecture', 'arm64')
            self.assertEqual(result.returncode, 5)
            written = json.loads(output.read_text(encoding='utf-8'))
            self.assertEqual(written['architecture'], 'amd64')
            self.assertIn('arm64', written['refusal'])
            output.unlink()
            unmapped = dict(observations('amd64'), os_architecture_cim='32-bit', os_architecture='X86',
                            process_architecture='X86', processor_architecture_env='x86')
            source.write_text(json.dumps(unmapped), encoding='utf-8')
            result = run_cli('host-identity', '--observations', str(source), '--output', str(output),
                             '--expected-architecture', 'amd64')
            self.assertEqual(result.returncode, 5)
            written = json.loads(output.read_text(encoding='utf-8'))
            self.assertIsNone(written['architecture'])
            self.assertIn('32-bit', written['refusal'])
            self.assertEqual(written['windows']['os_architecture_cim'], '32-bit')
            self.assertEqual(written['windows']['os_architecture'], 'X86')
            output.unlink()
            # The emulated launcher measured on the laptop: refused, the host still recorded, no token.
            emulated = dict(observations('arm64'), os_architecture='X64', process_architecture='X64',
                            processor_architecture_env='AMD64', processor_architew6432_env='')
            source.write_text(json.dumps(emulated), encoding='utf-8')
            result = run_cli('host-identity', '--observations', str(source), '--output', str(output),
                             '--expected-architecture', 'arm64')
            self.assertEqual(result.returncode, 5)
            written = json.loads(output.read_text(encoding='utf-8'))
            self.assertIsNone(written['architecture'])
            self.assertIn('emulation', written['refusal'])
            self.assertEqual(written['windows']['os_architecture_cim'], 'ARM 64-bit Processor')
            self.assertEqual(written['cpu'], observations('arm64')['cpu'])

    def test_CheckCli_GuestMismatch_Refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            identity, guest = root / 'host-identity.json', root / 'guest-identity.txt'
            identity.write_text(json.dumps(architecture.host_identity(observations('amd64'), 'amd64')), encoding='utf-8')
            guest.write_text('dpkg_architecture=arm64\nuname_m=aarch64\nnvidia_smi_name=absent\n', encoding='utf-8')
            result = run_cli('check', '--host-identity', str(identity), '--guest-identity', str(guest))
            self.assertEqual(result.returncode, 5)
            guest.write_text('dpkg_architecture=amd64\nuname_m=x86_64\nnvidia_smi_name=Fixture NVIDIA GPU\n', encoding='utf-8')
            result = run_cli('check', '--host-identity', str(identity), '--guest-identity', str(guest))
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn('HOST-GUEST-ARCHITECTURE-PASS architecture=amd64', result.stdout)

    def test_Contract_SchemaOneOrArchitectureAbsent_Refuses(self):
        good = contract_fixture('amd64')
        contract.check_contract_fields(good)
        for label, value in (('schema /1', dict(good, schema='cfdw-f1-r217-invocation/1')),
                             ('architecture absent', {k: v for k, v in good.items() if k != 'architecture'}),
                             ('windows name', dict(good, architecture='X64')),
                             ('unknown token', dict(good, architecture='riscv64'))):
            with self.subTest(case=label), self.assertRaises(ValueError):
                contract.check_contract_fields(value)

    def test_Contract_RootfsOfOtherArchitecture_Refuses(self):
        for token, other in (('amd64', 'arm64'), ('arm64', 'amd64')):
            good = contract_fixture(token)
            contract.check_contract_fields(good)
            wrong_hash = json.loads(json.dumps(good))
            wrong_hash['inputs']['rootfs']['sha256'] = architecture.TABLE[other]['rootfs_sha256']
            wrong_name = json.loads(json.dumps(good))
            wrong_name['inputs']['rootfs']['path'] = 'C:\\reviewed\\' + architecture.TABLE[other]['rootfs_name']
            for label, value in (('hash', wrong_hash), ('name', wrong_name)):
                with self.subTest(token=token, case=label), self.assertRaises(ValueError):
                    contract.check_contract_fields(value)

    def test_AttemptBinding_RootfsHashFollowsArchitecture(self):
        for token in ('amd64', 'arm64'):
            with self.subTest(token=token):
                self.assertEqual(attempt.input_hashes(token)['rootfs'], architecture.TABLE[token]['rootfs_sha256'])
        with self.assertRaises(ValueError):
            attempt.input_hashes('X64')

    def test_Roots_MakeFollowsArchitecture(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'roots.args'
            transaction.roots(SimpleNamespace(b2_lock=B2_LOCK, output=output, architecture='arm64'))
            lines = output.read_text(encoding='utf-8').splitlines()
            self.assertIn('make:arm64', lines)
            self.assertNotIn('make:amd64', lines)
            with self.assertRaises(ValueError):
                transaction.roots(SimpleNamespace(b2_lock=B2_LOCK, output=output, architecture='x64'))

    def test_Contract_ArchitectureToken_AgreesAcrossSurfaces(self):
        # Plan section 7: the token read from every C1 surface of one prepared contract agrees.
        for token in ('amd64', 'arm64'):
            with self.subTest(token=token), tempfile.TemporaryDirectory() as temporary:
                prepared = contract_fixture(token)
                contract.check_contract_fields(prepared)
                identity = architecture.host_identity(observations(token), prepared['architecture'])
                self.assertEqual(identity['architecture'], token)
                self.assertIsNone(identity['refusal'])
                row = architecture.row(token)
                self.assertEqual(PureWindowsPath(prepared['inputs']['rootfs']['path']).name, row['rootfs_name'])
                self.assertEqual(prepared['inputs']['rootfs']['sha256'], row['rootfs_sha256'])
                for line in architecture.sources(token):
                    self.assertTrue(line.startswith(f'deb [arch={token} '), line)
                output = Path(temporary) / 'roots.args'
                transaction.roots(SimpleNamespace(b2_lock=B2_LOCK, output=output, architecture=token))
                self.assertIn(f'make:{token}', output.read_text(encoding='utf-8').splitlines())
                self.assertEqual(architecture.linux_token(row['dpkg'], row['uname_m']), token)
                self.assertEqual(attempt.input_hashes(token)['rootfs'], row['rootfs_sha256'])


if __name__ == '__main__':
    unittest.main()
