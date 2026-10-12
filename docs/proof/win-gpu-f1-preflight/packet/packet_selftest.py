#!/usr/bin/env python3
"""Host-only planted failures; does not invoke WSL, apt, network, or a live wrapper."""
from pathlib import Path
from types import SimpleNamespace
import hashlib
import json
import tempfile

import contract
import owned_control
import transaction


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
        host.write_text('{"host_free_bytes":20000000000,"host_floor_bytes":19684091961,"target_vhd_physical_bytes":1000000000}\n', encoding="utf-8")
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
    root = "/mnt/c/review/run-1/live"
    run_id = "r217-one"
    cmd = f"/bin/bash /mnt/c/review/packet/preflight.sh --stage download --run-root {root} --packet-root /mnt/c/review/packet --run-id {run_id}"
    rows = [owned_control.ProcessRow(101, 101, 101, cmd),
            owned_control.ProcessRow(102, 101, 101, "apt-get download fixture")]
    assert owned_control.find_groups(rows, root, run_id, {}) == {101}
    assert owned_control.find_groups(rows[1:], root, run_id, {"download": 101}) == {101}
    expect_failure(lambda: owned_control.find_groups(rows, root, run_id, {"download": "partial"}), "partial owner")
    expect_failure(lambda: owned_control.read_processes(lambda: (_ for _ in ()).throw(OSError("ps failed"))), "ps failure")
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
    valid = {"schema": "cfdw-f1-r217-invocation/1", "leader": "mac", "leader_epoch": 5,
             "target": "cfdw-petsc-cuda132-f1", "protected": "cfdw-openfoam2512",
             "target_path": r"C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1",
             "accepted_packet_commit": "a" * 40,
             "inputs": {name: {"path": "C:\\reviewed", "sha256": "a" * 64} for name in contract.INPUT_KEYS},
             "allowed_signers": {"ubuntu_rootfs": ["A" * 40], "ubuntu_archive": ["B" * 40],
                                 "openfoam": [contract.OPENFOAM], "nvidia": [contract.NVIDIA]},
             "session": "r217", "execution_branch": "win/gpu-f1-r217-execute"}
    valid["inputs"]["rootfs"]["sha256"] = contract.ROOTFS
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
    print("packet planted-failure self-test: PASS")


if __name__ == "__main__":
    main()
