#!/usr/bin/env python3
"""Host-only planted failures; does not invoke WSL, apt, network, or a live wrapper."""
from pathlib import Path
from types import SimpleNamespace
import hashlib
import json
import tempfile
import time
from pathlib import PurePosixPath
from subprocess import CompletedProcess

import architecture
import contract
import owned_control
import transaction


def assert_cleanup_stage_scope(data):
    """Offline-fast: only delete-debs may differ from the accepted source (re-pinned at Ruling 240 nodes C1, C2 and C3; 02ec2cc3 before Ruling 237)."""
    prefix, tail = data.split(b'    delete-debs)\n', 1)
    suffix = tail.split(b'    *)', 1)[1]
    assert hashlib.sha256(prefix).hexdigest() == 'f0678550906d8616b1fcae888bc69c877b5fcaa50ae2eaa97b45521ef08a4271', 'non-cleanup packet stage changed'
    assert hashlib.sha256(suffix).hexdigest() == '5f944b439dcdb44a87e81a051cfd0c670348f7e6e49a1bdf0623d25295beaefa', 'packet dispatch suffix changed'
    assert b'"${packet_root}/../recovery/check_cache.py" --mode delete --cache "$cache"' in tail, 'pinned cache helper missing'


def expect_failure(fn, label):
    try:
        fn()
    except (ValueError, RuntimeError):
        return
    raise AssertionError(f"planted {label} passed")


def verify_size_case(index_value, control_value, should_pass):
    with tempfile.TemporaryDirectory() as folder:
        here = Path(folder)
        archives = here / "debs"
        archives.mkdir()
        archive = archives / "pkg_1_amd64.deb"
        archive.write_bytes(b"fixture")
        lock = here / "lock.json"
        lock.write_text(json.dumps({"packages": [{"package": "pkg", "version": "1", "architecture": "amd64",
            "sha256_signed_index": hashlib.sha256(b"fixture").hexdigest(), "download_bytes_signed_index": 7,
            "installed_kib_metadata": index_value}]}), encoding="utf-8")
        b2 = here / "b2.json"
        b2.write_text('{"packages": []}', encoding="utf-8")
        guest = here / "guest.txt"
        guest.write_text("block_bytes=8589934592\nfilesystem_bytes=8589934592\nfree_bytes=1000000000\nmaximum_bytes=8589934592\n", encoding="utf-8")
        host = here / "host.jsonl"
        host.write_text('{"host_free_bytes":20000000000,"host_floor_bytes":19000000000,"target_vhd_physical_bytes":1000000000}\n', encoding="utf-8")
        args = SimpleNamespace(lock=lock, b2_licences=b2, archives=archives,
            licences=here / "licenses.json", capacity=here / "capacity.json",
            guest_capacity=guest, host_capacity=host)
        original_control, original_payload = transaction.control_field, transaction.inspect_payload
        transaction.control_field = lambda _path, field: {
            "Package": "pkg", "Version": "1", "Architecture": "amd64", "Installed-Size": control_value}[field]
        transaction.inspect_payload = lambda _path: (70, [{"path": "usr/share/doc/pkg/copyright",
            "bytes": 1, "sha256": "x"}], [])
        try:
            if should_pass:
                transaction.verify(args)
            else:
                expect_failure(lambda: transaction.verify(args), "full archive size reconciliation")
            saved = json.loads(lock.read_text(encoding="utf-8"))
            assert saved["verified_against_signed_index"] == should_pass
            assert saved["packages"][0]["payload_bytes"] == 70
            cap = json.loads(args.capacity.read_text(encoding="utf-8"))
            assert cap["guest_block_bytes"] == 8589934592
            assert cap["target_vhd_max_physical_bytes"] == 1000000000
            assert cap["future_f1_proposed_headroom_bytes"] == 10 * 1024**3
        finally:
            transaction.control_field, transaction.inspect_payload = original_control, original_payload


def main():
    data = (Path(__file__).parent / 'preflight.sh').read_bytes()
    assert_cleanup_stage_scope(data)
    expect_failure_scope = data.replace(b'    simulate)', b'    altered)', 1)
    try:
        assert_cleanup_stage_scope(expect_failure_scope)
    except AssertionError:
        pass
    else:
        raise AssertionError('planted non-cleanup packet mutation passed')
    started = time.perf_counter()
    # Live URI failure: apt encodes Debian version punctuation. Offline fast ring;
    # no apt invocation. Cost is measured with the complete suite below.
    with tempfile.TemporaryDirectory() as folder:
        path = Path(folder) / "apt-uris.txt"
        filename = "cpp-13_13.3.0-6ubuntu2~24.04.1_amd64.deb"
        url = "http://archive.ubuntu.com/ubuntu/pool/main/g/gcc-13/" + filename.replace("~", "%7e")
        path.write_text(f"'{url}' {filename} 1042 MD5Sum:3197b91f8a37ff9bbae2ff279bab6e19\n", encoding="utf-8")
        assert transaction.match_uri(filename, 1042, transaction.uri_rows(path)) == (filename, url)
    for encoded, decoded in (("%7e", "~"), ("%7E", "~"), ("%2b", "+"), ("%2B", "+"), ("%3a", ":"), ("%3A", ":")):
        filename = f"pkg_1{decoded}2_amd64.deb"
        url = f"https://example.invalid/pool/pkg_1{encoded}2_amd64.deb"
        assert transaction.match_uri(filename, 7, {"apt-cache.deb": (url, 7)}) == ("apt-cache.deb", url)
        expect_failure(lambda: transaction.match_uri(filename, 8, {"apt-cache.deb": (url, 7)}), "URI size mismatch")
        expect_failure(lambda: transaction.match_uri(filename, 7, {"a.deb": (url, 7), "b.deb": (url, 7)}), "URI duplicate decoded basename")
    # Decode once, preserve literal plus, and never turn an encoded separator into a basename.
    for basename in ("pkg_1+2_amd64.deb", "pkg_1%2b2_amd64.deb"):
        url = "https://example.invalid/pool/" + basename.replace("%", "%25")
        assert transaction.match_uri(basename, 7, {"cache.deb": (url, 7)}) == ("cache.deb", url)
    for encoded, decoded in (("dir%2fpkg.deb", "pkg.deb"), ("dir%5cpkg.deb", "pkg.deb"),
                             ("pkg%00.deb", "pkg\x00.deb"), ("pkg%GG.deb", "pkg%GG.deb"),
                             ("pkg%.deb", "pkg%.deb"), ("pkg%ff.deb", "pkg\ufffd.deb")):
        url = "https://example.invalid/pool/" + encoded
        expect_failure(lambda: transaction.match_uri(decoded, 7, {"cache.deb": (url, 7)}), "unsafe or malformed URI basename")
    # Ruling 220 P3: exact attempt tokens; fast offline ring with a mocked signal sink.
    for kind, command in (
        ("stage", "/bin/bash /mnt/packet/preflight.sh --stage download --run-root /mnt/c/runAB --run-id runAB"),
        ("guard", "/usr/bin/python3 /mnt/packet/completed_guard.py watch --session-id runAB --events /mnt/c/runAB/completed-guard-events.jsonl"),
    ):
        foreign = [owned_control.ProcessRow(123, 123, 123, tuple(command.split()))]
        assert owned_control.find_groups(foreign, "/mnt/c/runA", "runA", {}, kind) == set(), \
            f"P3 {kind}: runA selected runAB"
        exact = command.replace("runAB", "runA")
        exact_rows = [owned_control.ProcessRow(123, 123, 123, tuple(exact.split()))]
        assert owned_control.find_groups(exact_rows, "/mnt/c/runA", "runA", {}, kind) == {123}
        owner_name = "download.entry.txt" if kind == "stage" else "guard.owner.txt"
        entry = ("/bin/bash", "/mnt/packet/identity.sh", f"/mnt/c/runA/{owner_name}", "--", *tuple(exact.split()))
        records = {"download" if kind == "stage" else "guard": 123}
        assert owned_control.find_groups([owned_control.ProcessRow(123, 123, 123, entry)],
                                         "/mnt/c/runA", "runA", records, kind) == {123}
        wrong_entry = (*entry[:2], f"/mnt/c/runAB/{owner_name}", *entry[3:])
        expect_failure(lambda: owned_control.find_groups([owned_control.ProcessRow(123, 123, 123, wrong_entry)],
                                                         "/mnt/c/runA", "runA", records, kind),
                       "pre-exec owner path collision")
        for changed in (exact.replace("run-id runA", "run-id runAB").replace("session-id runA", "session-id runAB"),
                        exact.replace("/mnt/c/runA", "/mnt/c/runAB"),
                        exact.replace("/mnt/packet/", "/mnt/packet/foreign-")):
            assert owned_control.find_groups([owned_control.ProcessRow(123, 123, 123, tuple(changed.split()))],
                                             "/mnt/c/runA", "runA", {}, kind) == set()
        expect_failure(lambda: owned_control.find_groups(foreign, "/mnt/c/runA", "runA",
                                                         {"download" if kind == "stage" else "guard": 123}, kind),
                       "recorded foreign argv")
        with tempfile.TemporaryDirectory() as folder:
            here = Path(folder) / "runA"
            here.mkdir()
            foreign_command = command.replace("/mnt/c/runAB", str(here) + "B")
            foreign_command = foreign_command.replace("/mnt/packet", str(PurePosixPath(str(here)).parent.parent / "packet"))
            original_read, original_signal = owned_control.read_processes, owned_control.send_group_signal
            signals = []
            owned_control.read_processes = lambda: [owned_control.ProcessRow(123, 123, 123, tuple(foreign_command.split()))]
            owned_control.send_group_signal = lambda group, sig: signals.append((group, sig))
            try:
                owned_control.control("stop", here, "runA", kind)
                assert not signals, f"P3 {kind}: foreign group received a signal"
            finally:
                owned_control.read_processes, owned_control.send_group_signal = original_read, original_signal
    root = "/mnt/c/review/run-1/live"
    run_id = "r217-one"
    cmd = f"/bin/bash /mnt/c/review/packet/preflight.sh --stage download --run-root {root} --packet-root /mnt/c/review/packet --run-id {run_id}"
    rows = [owned_control.ProcessRow(101, 101, 101, tuple(cmd.split())),
            owned_control.ProcessRow(102, 101, 101, ("apt-get", "download", "fixture"))]
    assert owned_control.find_groups(rows, root, run_id, {}) == {101}
    assert owned_control.find_groups(rows[1:], root, run_id, {"download": 101}) == {101}
    expect_failure(lambda: owned_control.find_groups(rows, root, run_id, {"download": "partial"}), "partial owner")
    expect_failure(lambda: owned_control.read_processes(lambda: (_ for _ in ()).throw(OSError("ps failed"))), "ps failure")
    inspected = owned_control.read_processes(lambda: CompletedProcess([], 0, "101 101 101\n"),
                                             lambda _pid: b"/bin/echo\0one token with spaces\0runAB\0")
    assert inspected[0].argv == ("/bin/echo", "one token with spaces", "runAB")
    # ps includes short-lived processes (including itself); recheck a missing PID.
    original_path = owned_control.Path
    owned_control.Path = lambda _path: SimpleNamespace(
        stat=lambda: (_ for _ in ()).throw(FileNotFoundError("PID independently absent")))
    try:
        assert owned_control.read_processes(lambda: CompletedProcess([], 0, "101 101 101\n"),
            lambda _pid: (_ for _ in ()).throw(FileNotFoundError("argv disappeared"))) == []
    finally:
        owned_control.Path = original_path
    for stat in (lambda: SimpleNamespace(),
                 lambda: (_ for _ in ()).throw(PermissionError("PID inspection denied"))):
        owned_control.Path = lambda _path: SimpleNamespace(stat=stat)
        try:
            expect_failure(lambda: owned_control.read_processes(lambda: CompletedProcess([], 0, "101 101 101\n"),
                lambda _pid: (_ for _ in ()).throw(FileNotFoundError("argv disappeared"))),
                "unproved PID disappearance")
        finally:
            owned_control.Path = original_path
    expect_failure(lambda: owned_control.read_processes(lambda: CompletedProcess([], 0, "101 101 101\n"),
                                                       lambda _pid: b"unterminated"), "partial proc argv")
    expect_failure(lambda: owned_control.read_processes(lambda: CompletedProcess([], 0, "101 101 101\n"),
                                                       lambda _pid: (_ for _ in ()).throw(OSError("proc denied"))),
                   "proc inspection failure")
    for wrong in (owned_control.ProcessRow(101, 999, 101, tuple(cmd.split())),
                  owned_control.ProcessRow(101, 101, 999, tuple(cmd.split())),
                  owned_control.ProcessRow(101, 101, 101, ("/bin/echo", cmd))):
        expect_failure(lambda wrong=wrong: owned_control.find_groups([wrong], root, run_id, {"download": 101}),
                       "recorded pid/pgid/argv mismatch")
    with tempfile.TemporaryDirectory() as folder:
        here = Path(folder)
        (here / "current-owner.pid").write_text("", encoding="ascii")
        expect_failure(lambda: owned_control.read_owner_records(here), "partial PID")
    with tempfile.TemporaryDirectory() as folder:
        here = Path(folder)
        (here / "download.intent.json").write_text('{"stage":"download"}', encoding="utf-8")
        original_read = owned_control.read_processes
        owned_control.read_processes = lambda: []
        try:
            expect_failure(lambda: owned_control.control("check", here, run_id),
                           "unacknowledged attempt intent")
        finally:
            owned_control.read_processes = original_read
    with tempfile.TemporaryDirectory() as folder:
        here = Path(folder)
        (here / "download.owner.txt").write_text("pid=101\npgid=101\nsid=101\n", encoding="ascii")
        original_read, original_kill, original_sleep = (owned_control.read_processes,
                                                       owned_control.send_group_signal, owned_control.time.sleep)
        active, killed = [True], []
        owned_control.read_processes = lambda: rows[1:] if active[0] else []
        def stop_group(group, signal):
            killed.append(group)
            active[0] = False
        owned_control.send_group_signal = stop_group
        owned_control.time.sleep = lambda _seconds: None
        try:
            owned_control.control("stop", here, run_id)
            assert killed == [101], "leader-gone child group was not killed"
        finally:
            owned_control.read_processes, owned_control.send_group_signal, owned_control.time.sleep = (
                original_read, original_kill, original_sleep)
    with tempfile.TemporaryDirectory() as folder:
        here = Path(folder)
        owner = here / "download.owner.txt"
        owner.write_text("pid=101\npgid=101\nsid=101\n", encoding="ascii")
        (here / "download.residual-absent.sha256").write_text(hashlib.sha256(owner.read_bytes()).hexdigest(), encoding="ascii")
        assert owned_control.read_owner_records(here) == {}
        owner.write_text("pid=102\npgid=102\nsid=102\n", encoding="ascii")
        expect_failure(lambda: owned_control.read_owner_records(here), "changed owner after absence proof")
    assert contract.signer_is_allowed(["[GNUPG:] VALIDSIG DC93C096174122E256DA24063386DD74948D208F 2026"],
                                      {"DC93C096174122E256DA24063386DD74948D208F"})
    expect_failure(lambda: contract.signer_is_allowed(["[GNUPG:] VALIDSIG FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF 2026"],
                                                     {"DC93C096174122E256DA24063386DD74948D208F"}), "signer mismatch")
    valid = {"schema": "cfdw-f1-r217-invocation/2", "architecture": "amd64", "leader": "mac", "leader_epoch": 5,
             "target": "cfdw-petsc-cuda132-f1", "protected": "cfdw-openfoam2512",
             "target_path": r"X:\fixture\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1",
             "host_start_floor_bytes": 19000000000, "host_reserve_bytes": 10000000000,
             "accepted_packet_commit": "a" * 40,
             "inputs": {name: {"path": "C:\\reviewed", "sha256": "a" * 64} for name in contract.INPUT_KEYS},
             "allowed_signers": {"ubuntu_rootfs": ["A" * 40], "ubuntu_archive": ["B" * 40],
                                 "openfoam": [contract.OPENFOAM], "nvidia": [contract.NVIDIA]},
             "session": "r217", "execution_branch": "win/gpu-f1-r217-execute"}
    valid["inputs"]["rootfs"] = {"path": "C:\\reviewed\\" + architecture.TABLE["amd64"]["rootfs_name"],
                                "sha256": architecture.TABLE["amd64"]["rootfs_sha256"]}
    contract.check_contract_fields(valid)
    for invalid in (None, 0, -1, "5", True):
        expect_failure(lambda invalid=invalid: contract.check_contract_fields(
            {**valid, "leader_epoch": invalid}), "invalid leader epoch")
    contract.check_live_leader({"leader": "mac", "epoch": "5", "state": "live"}, 5)
    expect_failure(lambda: contract.check_live_leader(
        {"leader": "mac", "epoch": "4", "state": "live"}, 5), "live epoch mismatch")
    expect_failure(lambda: contract.check_hash("00" * 32, b"packet"), "contract byte mismatch")
    assert transaction.reconcile_installed_size(3, "3") == (3, "signed-index-and-control")
    assert transaction.reconcile_installed_size("Not recorded", "3") == (3, "control-only-index-absent")
    expect_failure(lambda: transaction.reconcile_installed_size(1, "99"), "Installed-Size mismatch")
    expect_failure(lambda: transaction.reconcile_installed_size(1, ""), "Installed-Size missing")
    verify_size_case(99, "99", True)
    verify_size_case("Not recorded", "99", True)
    verify_size_case(1, "99", False)
    verify_size_case(1, "", False)
    print(f"packet planted-failure self-test: PASS ring=offline-fast elapsed_seconds={time.perf_counter() - started:.3f}")


if __name__ == "__main__":
    main()
