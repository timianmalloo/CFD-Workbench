#!/usr/bin/env python3
"""Disposable W0 qualification. No product acceptance or automatic dispatch."""
import argparse
import base64
import copy
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import signal
import stat
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ["global.json", "tools/spikes/WindowsRuntime/WindowsRuntime.csproj",
           "tools/spikes/WindowsRuntime/Program.cs", "tools/qualify-windows-runtime.py",
           ".github/workflows/application-windows-qualification.yml"]
CASES = ("create", "create-collision", "read", "overwrite", "conflict", "immutable-input",
         "cancel-before", "cancel-after", "write-fault", "replace-fault", "sharing",
         "dacl-create", "dacl-inheritance", "dacl-replacement", "denial", "case-alias",
         "unicode-alias", "ads-device-unc", "hard-link", "leaf-reparse", "ancestor-reparse",
         "ancestor-substitution", "owned-cleanup", "cleanup-refusal", "file-flush",
         "directory-durability")
FIXTURE_A = b"W0 immutable fixture A\r\n\x00\xff"
FIXTURE_B = b"W0 independent fixture B\r\n\x01\xfe"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def verify_sources(base, manifest):
    after = {p: digest((base / p).read_bytes()) for p in manifest}
    if after != manifest:
        raise ValueError("W0-SOURCE-DRIFT")
    return after


def validate_dacl(value, token_sid):
    """Compare resolved descriptor semantics; raw SDDL is retained, not authority."""
    sid_pattern = r"S-1-[0-9]+(?:-[0-9]+){1,15}"
    if not isinstance(value, dict) or not isinstance(token_sid, str) or not re.fullmatch(sid_pattern, token_sid):
        raise ValueError("W0-DACL-SCHEMA")
    token = r"(?:S-1-[0-9]+(?:-[0-9]+){1,15}|[A-Z]{2})"
    raw = re.fullmatch(r"O:(" + token + r")D:P\(A;;FA;;;(" + token + r")\)", value.get("rawSddl", "")) if isinstance(value.get("rawSddl"), str) else None
    if raw is None:
        raise ValueError("W0-DACL-RAW-SCHEMA")
    resolution = value.get("rawResolution")
    if not isinstance(resolution, dict) or resolution.get("method") != "ConvertStringSecurityDescriptorToSecurityDescriptorW":
        raise ValueError("W0-DACL-RAW-RESOLUTION-MISSING")
    resolved = resolution.get("descriptor")
    if not isinstance(resolved, dict):
        raise ValueError("W0-DACL-RAW-DESCRIPTOR-MISSING")
    def resolve(token):
        if re.fullmatch(sid_pattern, token):
            return token
        if token == "SY":
            return "S-1-5-18"  # SDDL_LOCAL_SYSTEM / SECURITY_LOCAL_SYSTEM_RID.
        context = resolution.get("context")
        if token != "LA" or not isinstance(context, dict) or context.get("method") != "LsaQueryInformationPolicy:PolicyAccountDomainInformation:local":
            raise ValueError("W0-DACL-ALIAS-UNRESOLVED")
        domain = context.get("accountDomainSid")
        if not isinstance(domain, str) or not re.fullmatch(r"S-1-5-21-[0-9]+-[0-9]+-[0-9]+", domain):
            raise ValueError("W0-DACL-ALIAS-CONTEXT")
        return domain + "-500"  # Documented local administrator RID, not token suffix.
    if any(resolve(part) != token_sid for part in raw.groups()):
        raise ValueError("W0-DACL-RAW-SID-MISMATCH")
    for key in ("ownerSid", "daclPresent", "daclProtected", "aceCount", "aces"):
        if key not in resolved or json.dumps(resolved[key], sort_keys=True) != json.dumps(value.get(key), sort_keys=True):
            raise ValueError("W0-DACL-RAW-SEMANTICS-" + key)
    for key, expected in dict(ownerSid=token_sid, tokenUserSid=token_sid, daclPresent=True,
                              daclProtected=True, aceCount=1).items():
        if type(value.get(key)) is not type(expected) or value[key] != expected:
            raise ValueError("W0-DACL-" + key)
    aces = value.get("aces")
    if not isinstance(aces, list) or len(aces) != 1 or not isinstance(aces[0], dict):
        raise ValueError("W0-DACL-ACE-COUNT")
    for key, expected in dict(type="AccessAllowed", qualifier="AccessAllowed", flags=0,
                              inherited=False, mask=0x1f01ff, trusteeSid=token_sid).items():
        if type(aces[0].get(key)) is not type(expected) or aces[0][key] != expected:
            raise ValueError("W0-DACL-ACE-" + key)


def validate_case(name, proof):
    """Case-specific consumer contract, independent of native assertions."""
    def equals(key, expected):
        actual = proof.get(key)
        if actual != expected or type(actual) is not type(expected):
            raise ValueError("W0-WRONG-" + name + "-" + key)

    save_codes = {"create": "DOC-SAVE-UNCERTAIN", "create-collision": "DOC-CONFLICT",
                  "overwrite": "DOC-SAVE-UNCERTAIN", "conflict": "DOC-CONFLICT",
                  "immutable-input": "DOC-SAVE-UNCERTAIN", "cancel-before": "DOC-CANCELLED",
                  "cancel-after": "DOC-SAVE-UNCERTAIN", "write-fault": "DOC-IO",
                  "replace-fault": "DOC-SAVE-UNCERTAIN"}
    if name in save_codes:
        equals("saveCode", save_codes[name])
        equals("candidateDurability", False)
        equals("targetExists", name not in ("cancel-before", "write-fault"))
        equals("tempExists", False)
        equals("claimExists", False)
        equals("cancellationRequested", name in ("cancel-before", "cancel-after"))
        if name == "write-fault":
            equals("writtenBeforeFault", 1)
    if name == "cleanup-refusal":
        equals("foreignAfter", digest(FIXTURE_B))
        equals("cleanupRefused", True)
        equals("targetExists", True)
        equals("ownedOriginalExists", True)
    elif name == "owned-cleanup":
        equals("targetExists", False)
        equals("ownedDeleted", True)
    elif name == "sharing":
        equals("win32", 32)
    elif name == "denial":
        equals("win32", 5)
    elif name == "unicode-alias":
        equals("rejectedForms", 2)
    elif name == "ads-device-unc":
        equals("rejectedForms", 6)
    elif name == "dacl-inheritance":
        equals("protectedPrivate", True)
        equals("inheritedControlDetected", True)
        validate_dacl(proof.get("protectedDescriptor"), proof.get("tokenUserSid"))
    elif name in ("dacl-create", "dacl-replacement"):
        for field in (["creationDacl", "replacementDacl"] if name == "dacl-replacement" else ["creationDacl"]):
            validate_dacl(proof.get(field), proof.get("tokenUserSid"))
    elif name == "case-alias":
        identity = proof.get("identityBefore", "")
        if not re.fullmatch(r"[0-9a-f]{8}:[0-9a-f]{16}", identity):
            raise ValueError("W0-IDENTITY-MISSING")
        equals("aliasIdentity", identity)
    elif name == "hard-link":
        if type(proof.get("links")) is not int or proof["links"] < 2:
            raise ValueError("W0-HARDLINK-NOT-OBSERVED")
        equals("after", digest(FIXTURE_A))
    elif name in ("leaf-reparse", "ancestor-reparse"):
        equals("redirectRejected", True)
        equals("after", digest(FIXTURE_A))
    elif name == "ancestor-substitution":
        if proof.get("win32") not in (5, 32) or not proof.get("heldAncestors"):
            raise ValueError("W0-ANCESTOR-RENAME-NOT-REFUSED")
    elif name == "file-flush":
        equals("fileFlushed", True)


def validate(rows, source, binary):
    if not isinstance(rows, list) or any(not isinstance(row, dict) or not isinstance(row.get("case"), str) for row in rows):
        raise ValueError("W0-ROW-SCHEMA")
    if len(rows) != len(CASES) or {r.get("case") for r in rows} != set(CASES):
        raise ValueError("W0-CASE-SET")
    for row in rows:
        if row.get("source") != source or row.get("binary") != binary:
            raise ValueError("W0-BINDING")
        if row.get("status") not in ("Pass", "Expected rejection", "Not assessed", "Fail"):
            raise ValueError("W0-STATUS")
        if type(row.get("publication")) is not bool or row.get("durability") is not False:
            raise ValueError("W0-DURABILITY-CONTRACT")
        if not isinstance(row.get("evidence"), dict) or not isinstance(row.get("code"), str):
            raise ValueError("W0-EVIDENCE")
        descriptors = row["evidence"].get("creationDescriptors", [])
        if not isinstance(descriptors, list) or any(not isinstance(item, dict) for item in descriptors):
            raise ValueError("W0-DACL-CREATION-SCHEMA")
        for created in descriptors:
            validate_dacl(created.get("descriptor"), row["evidence"].get("tokenUserSid"))
        arms = row["evidence"].get("replacementDiagnostics", [])
        if not isinstance(arms, list) or any(not isinstance(arm, dict) for arm in arms):
            raise ValueError("W0-DIAGNOSTIC-ARM-SCHEMA")
        if row["case"] == "overwrite" and row["status"] == "Pass" and (
                len(arms) != 2 or [a.get("arm") for a in arms] != ["held-target", "released-target"] or
                row["evidence"].get("originalOverwriteInvoked") is not True):
            raise ValueError("W0-ORIGINAL-OR-DIAGNOSTICS-MISSING")
        for arm in arms:
            created_items = arm.get("creationDescriptors", [])
            if not isinstance(created_items, list) or any(not isinstance(item, dict) for item in created_items):
                raise ValueError("W0-DIAGNOSTIC-DACL-SCHEMA")
            for created in created_items:
                validate_dacl(created.get("descriptor"), row["evidence"].get("tokenUserSid"))
        if row["status"] in ("Pass", "Expected rejection"):
            if row["code"].startswith("W0-UNSUPPORTED") or not row["evidence"]:
                raise ValueError("W0-UNSUPPORTED-PASS")
            proof = row["evidence"]
            if proof.get("fixtureA") != digest(FIXTURE_A) or proof.get("fixtureB") != digest(FIXTURE_B):
                raise ValueError("W0-FIXTURE-BINDING")
            if proof.get("architecture") != "X64" or proof.get("filesystem") != "NTFS":
                raise ValueError("W0-HOST-CLAIM")
            name = row["case"]
            expected = {"create": FIXTURE_B, "read": FIXTURE_A, "immutable-input": FIXTURE_B,
                        "create-collision": FIXTURE_A, "conflict": FIXTURE_A, "replace-fault": FIXTURE_A,
                        "overwrite": FIXTURE_B, "cancel-after": FIXTURE_B}
            if name in expected and proof.get("after") != digest(expected[name]):
                raise ValueError("W0-WRONG-BYTES")
            if row["publication"] != (name in ("create", "overwrite", "immutable-input", "cancel-after", "dacl-replacement")):
                raise ValueError("W0-WRONG-PUBLICATION")
            if name == "directory-durability":
                raise ValueError("W0-UNRESOLVED-DURABILITY")
            validate_case(name, proof)
    return all(r["status"] in ("Pass", "Expected rejection") and
               all(arm.get("status") == "Observed" for arm in r["evidence"].get("replacementDiagnostics", [])) for r in rows)


def consume_rows(rows, source, binary, summary):
    try:
        return validate(rows, source, binary)
    except ValueError as error:
        summary["errors"].append(dict(stage="validation", code=str(error)))
        return False


def validate_diagnostic_controls(rows, source, binary):
    names = {"setup", "operation", "observation", "original", "unsafe", "cleanup"}
    if len(rows) != len(names) or {r.get("control") for r in rows} != names:
        raise ValueError("W0-DIAGNOSTIC-CONTROL-SET")
    for row in rows:
        fault = row["control"]
        if row.get("source") != source or row.get("binary") != binary or row.get("result") != "Pass":
            raise ValueError("W0-DIAGNOSTIC-CONTROL-BINDING")
        stop = fault in ("unsafe", "cleanup")
        if type(row.get("originalCalls")) is not int or row["originalCalls"] != (0 if stop else 1):
            raise ValueError("W0-ORIGINAL-SUPPRESSED")
        arms = row.get("evidence", {}).get("replacementDiagnostics", [])
        if len(arms) != 2 or [a.get("arm") for a in arms] != ["held-target", "released-target"]:
            raise ValueError("W0-DIAGNOSTIC-ARM-SET")
        if arms[1].get("status") != ("Not assessed" if stop else "Observed"):
            raise ValueError("W0-DIAGNOSTIC-SECOND-ARM-SUPPRESSED")
        if fault in ("setup", "operation", "observation") and (arms[0].get("status") != "Fail" or fault not in arms[0].get("steps", [])):
            raise ValueError("W0-DIAGNOSTIC-FAULT-NOT-OBSERVED")
        if fault == "original" and row["evidence"].get("originalException") != "injected original scenario":
            raise ValueError("W0-ORIGINAL-FAILURE-NOT-OBSERVED")
        if fault == "cleanup" and arms[0].get("cleanup") != "injected unclosed handle":
            raise ValueError("W0-CLEANUP-FAULT-NOT-OBSERVED")


def validate_final_arm_controls(rows, source, binary):
    names = {arm + "-" + fault for arm in ("held", "released") for fault in
             ("unsupported", "access", "observation", "identity", "cleanup")}
    if len(rows) != len(names) or {r.get("control") for r in rows} != names:
        raise ValueError("W0-FINAL-ARM-CONTROL-SET")
    for row in rows:
        if row.get("source") != source or row.get("binary") != binary or row.get("result") != "Pass":
            raise ValueError("W0-FINAL-ARM-CONTROL-BINDING")
        released = row["control"].startswith("released-")
        if type(row.get("originalCalls")) is not int or row["originalCalls"] != 0 or row.get("operations") != (2 if released else 1):
            raise ValueError("W0-FINAL-ARM-UNSAFE-EXECUTION")
        proof = row.get("evidence", {})
        if proof.get("originalOverwriteInvoked") is not False:
            raise ValueError("W0-FINAL-ARM-ORIGINAL-INVOKED")
        arms = proof.get("replacementDiagnostics", [])
        if len(arms) != 2 or [a.get("arm") for a in arms] != ["held-target", "released-target"]:
            raise ValueError("W0-FINAL-ARM-RECORDS")
        failed = arms[1 if released else 0]
        if failed.get("status") != "Unsafe" or (not released and arms[1].get("status") != "Not assessed"):
            raise ValueError("W0-FINAL-ARM-UNSAFE-STATUS")
        if row["control"].endswith("cleanup"):
            if failed.get("cleanup") != "injected unclosed handle":
                raise ValueError("W0-FINAL-ARM-CLEANUP-NOT-OBSERVED")
        else:
            error = failed.get("finalContainmentException", {})
            if failed.get("finalContainment") != "Unsafe" or not error.get("type") or not error.get("message"):
                raise ValueError("W0-FINAL-ARM-ERROR-LOST")
            if row["control"].endswith("access") and error.get("nativeError") != 5:
                raise ValueError("W0-FINAL-ARM-NATIVE-ERROR-LOST")
            if failed.get("cleanup") != "all acquired handles disposed":
                raise ValueError("W0-FINAL-ARM-CLEANUP-LOST")
        refused = [json.loads(line) for line in row.get("refusedRows", "").splitlines()]
        if len(refused) != len(CASES) or {r.get("case") for r in refused} != set(CASES):
            raise ValueError("W0-FINAL-ARM-REFUSED-CASE-SET")
        if any(r.get("status") != "Not assessed" or r.get("code") != "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS" or
               r.get("publication") is not False or r.get("durability") is not False or
               r.get("source") != source or r.get("binary") != binary for r in refused):
            raise ValueError("W0-FINAL-ARM-REFUSAL-CLAIM")


def validate_constructor_controls(rows, source, binary):
    if len(rows) != 2 or {r.get("control") for r in rows} != {"constructor-after-1", "constructor-after-3"}:
        raise ValueError("W0-CONSTRUCTOR-CONTROL-SET")
    for row in rows:
        count = 1 if row["control"].endswith("1") else 3
        if row.get("source") != source or row.get("binary") != binary or row.get("result") != "Pass":
            raise ValueError("W0-CONSTRUCTOR-BINDING")
        if type(row.get("acquired")) is not int or row["acquired"] != count or row.get("closed") != [True] * count or any(type(v) is not bool for v in row["closed"]):
            raise ValueError("W0-CONSTRUCTOR-ACQUIRED-CLEANUP")
        if row.get("foreignOpen") is not True or row.get("sameError") is not True or row.get("error") != "injected inspection after acquisition " + str(count):
            raise ValueError("W0-CONSTRUCTOR-FOREIGN-OR-ERROR")


def self_test():
    """Synthetic protocol data, never Windows runtime evidence."""
    r48_alias_test()
    r45_self_test()
    source, binary = "a" * 64, "b" * 64
    rows = [dict(case=c, status="Not assessed", code="W0-UNSUPPORTED", source=source,
                 binary=binary, publication=False, durability=False, evidence={}) for c in CASES]
    assert validate(rows, source, binary) is False
    mutations = []
    mutations.append(("missing", rows[:-1]))
    mutations.append(("duplicate", rows + [rows[0]]))
    for name, key, value in [("source", "source", "c" * 64), ("binary", "binary", "c" * 64),
                             ("unsupported-pass", "status", "Pass"),
                             ("durability", "durability", True)]:
        changed = copy.deepcopy(rows)
        changed[0][key] = value
        mutations.append((name, changed))
    for name, changed in mutations:
        try:
            validate(changed, source, binary)
        except ValueError:
            print(json.dumps({"control": name, "result": "rejected"}))
        else:
            raise AssertionError(name + " accepted wrong result")
    for wrong in ("after", "publication"):
        changed = copy.deepcopy(rows)
        changed[0].update(status="Pass", code="OK", publication=True,
                          evidence={"fixtureA": digest(FIXTURE_A), "fixtureB": digest(FIXTURE_B),
                                    "architecture": "X64", "filesystem": "NTFS", "after": digest(FIXTURE_B)})
        if wrong == "after":
            changed[0]["evidence"]["after"] = digest(FIXTURE_A)
        else:
            changed[0]["publication"] = False
        try:
            validate(changed, source, binary)
        except ValueError:
            print(json.dumps({"control": "wrong-" + wrong, "result": "rejected"}))
        else:
            raise AssertionError("wrong " + wrong + " accepted")
    for receipt in [{"quiescent": False, "observed": []},
                    {"quiescent": True, "observed": []}]:
        try:
            validate_tree(receipt)
        except ValueError:
            print(json.dumps({"control": "live-or-unobserved-tree", "result": "rejected"}))
        else:
            raise AssertionError("unproved child cleanup accepted")
    # Independent retained-schema examples for the root's concrete false accepts.
    fixtures = {
        "conflict": {"after": digest(FIXTURE_A), "saveCode": "DOC-CONFLICT", "candidateDurability": False,
                     "targetExists": True, "tempExists": False, "claimExists": False, "cancellationRequested": False},
        "cancel-before": {"saveCode": "DOC-CANCELLED", "candidateDurability": False,
                          "targetExists": False, "tempExists": False, "claimExists": False, "cancellationRequested": True},
        "write-fault": {"saveCode": "DOC-IO", "candidateDurability": False, "targetExists": False,
                        "tempExists": False, "claimExists": False, "writtenBeforeFault": 1, "cancellationRequested": False},
        "cleanup-refusal": {"foreignAfter": digest(FIXTURE_B), "cleanupRefused": True,
                            "targetExists": True, "ownedOriginalExists": True}}
    for case, proof in fixtures.items():
        example = copy.deepcopy(rows)
        row = next(r for r in example if r["case"] == case)
        row.update(status="Pass", code="OK", evidence=dict(proof, fixtureA=digest(FIXTURE_A),
                   fixtureB=digest(FIXTURE_B), architecture="X64", filesystem="NTFS"))
        assert validate(example, source, binary) is False  # Other cases remain NA.
        for field in proof:
            changed = copy.deepcopy(example)
            next(r for r in changed if r["case"] == case)["evidence"][field] = None
            try:
                validate(changed, source, binary)
            except ValueError:
                print(json.dumps({"control": case + "-wrong-" + field, "result": "rejected"}))
            else:
                raise AssertionError(case + " accepted wrong " + field)
    with tempfile.TemporaryDirectory(prefix="cfd-w0-source-control-") as scratch:
        copied = Path(scratch) / "copied-source.cs"
        copied.write_bytes((ROOT / "tools/spikes/WindowsRuntime/Program.cs").read_bytes())
        manifest = {copied.name: digest(copied.read_bytes())}
        assert verify_sources(Path(scratch), manifest) == manifest
        copied.write_bytes(copied.read_bytes() + b"\n// source changed during the simulated operation\n")
        try:
            verify_sources(Path(scratch), manifest)
        except ValueError as error:
            assert str(error) == "W0-SOURCE-DRIFT"
            print(json.dumps({"control": "source-drift-during-operation", "result": "rejected"}))
        else:
            raise AssertionError("source drift accepted")
    # Preserve argparse's real exit: intercepting sys.exit can accidentally run main.
    help_result = subprocess.run([sys.executable, str(Path(__file__).resolve()), "--help"],
                                 capture_output=True, text=True, encoding="utf-8", errors="replace",
                                 timeout=10, env=dict(os.environ, PYTHONIOENCODING="cp1252"))
    assert help_result.returncode == 0 and help_result.stderr == ""
    assert help_result.stdout.startswith("usage: qualify-windows-runtime.py")
    assert '"native_qualification"' not in help_result.stdout
    print(json.dumps({"control": "help-exits-without-qualification", "result": "Pass"}))
    print(json.dumps({"self_test": "Pass", "native_qualification": "Not assessed"}))


def r48_alias_test():
    sid = "S-1-5-21-1-2-3-500"
    descriptor = dict(rawSddl="O:LAD:P(A;;FA;;;LA)", ownerSid=sid, tokenUserSid=sid,
                      daclPresent=True, daclProtected=True, aceCount=1,
                      aces=[dict(type="AccessAllowed", qualifier="AccessAllowed", flags=0,
                                 inherited=False, mask=0x1f01ff, trusteeSid=sid)])
    attach_synthetic_resolution(descriptor)
    accepted = []
    for wrong in ("SY", "ZZ"):
        for counterpart in (sid, "LA"):
            for position in ("owner", "ace"):
                owner, trustee = (wrong, counterpart) if position == "owner" else (counterpart, wrong)
                item = dict(descriptor, rawSddl="O:" + owner + "D:P(A;;FA;;;" + trustee + ")")
                name = wrong + "-" + position + "-" + ("numeric" if counterpart == sid else "alias")
                try:
                    validate_dacl(item, sid)
                except ValueError:
                    print(json.dumps({"control": "r48-" + name, "result": "rejected"}))
                else:
                    accepted.append(name)
                    print(json.dumps({"control": "r48-" + name, "result": "WRONGLY ACCEPTED"}))
    assert not accepted, "contradictory aliases accepted: " + ",".join(accepted)
    for owner, trustee in ((sid, sid), ("LA", "LA"), (sid, "LA"), ("LA", sid)):
        validate_dacl(dict(descriptor, rawSddl="O:" + owner + "D:P(A;;FA;;;" + trustee + ")"), sid)
        print(json.dumps({"control": "r48-equivalent-" + owner + "-" + trustee, "result": "Pass", "scope": "synthetic"}))
    wrong = []
    for key in ("rawResolution",):
        item = copy.deepcopy(descriptor)
        del item[key]
        wrong.append((key, item))
    for key in ("context", "descriptor", "method"):
        item = copy.deepcopy(descriptor)
        del item["rawResolution"][key]
        wrong.append(("missing-" + key, item))
    item = copy.deepcopy(descriptor)
    item["rawResolution"]["context"]["accountDomainSid"] = "S-1-5-21-4-5-6"
    wrong.append(("wrong-domain-context", item))
    for key in ("ownerSid", "daclPresent", "daclProtected", "aceCount", "aces"):
        item = copy.deepcopy(descriptor)
        item["rawResolution"]["descriptor"][key] = None
        wrong.append(("raw-resolved-" + key, item))
    for key, changed in (("trusteeSid", "S-1-5-18"), ("type", "AccessDenied"), ("qualifier", "AccessDenied"),
                         ("flags", False), ("inherited", True), ("mask", 0)):
        item = copy.deepcopy(descriptor)
        item["rawResolution"]["descriptor"]["aces"][0][key] = changed
        wrong.append(("raw-resolved-ace-" + key, item))
    for name, item in wrong:
        try:
            validate_dacl(item, sid)
        except ValueError:
            print(json.dumps({"control": "r48-" + name, "result": "rejected"}))
        else:
            raise AssertionError("accepted " + name)
    system = copy.deepcopy(descriptor)
    system.update(rawSddl="O:SYD:P(A;;FA;;;SY)", ownerSid="S-1-5-18", tokenUserSid="S-1-5-18")
    system["aces"][0]["trusteeSid"] = "S-1-5-18"
    attach_synthetic_resolution(system)
    system["rawResolution"]["context"] = {}
    validate_dacl(system, "S-1-5-18")
    print(json.dumps({"control": "r48-SY-equivalent-positive", "result": "Pass", "scope": "synthetic"}))


def attach_synthetic_resolution(descriptor):
    """Synthetic receipt fixture only; does not execute LSA or resolve Windows SIDs."""
    descriptor["rawResolution"] = dict(method="ConvertStringSecurityDescriptorToSecurityDescriptorW",
        context=dict(method="LsaQueryInformationPolicy:PolicyAccountDomainInformation:local", accountDomainSid="S-1-5-21-1-2-3"),
        descriptor=copy.deepcopy({k: descriptor[k] for k in ("ownerSid", "daclPresent", "daclProtected", "aceCount", "aces")}))


def r45_self_test():
    """Frozen synthetic semantic/finalization contracts; no Windows SID resolution."""
    sid = "S-1-5-21-1-2-3-500"
    descriptor = dict(rawSddl="O:LAD:P(A;;FA;;;LA)", ownerSid=sid, tokenUserSid=sid,
                      daclPresent=True, daclProtected=True, aceCount=1,
                      aces=[dict(type="AccessAllowed", qualifier="AccessAllowed", flags=0,
                                 inherited=False, mask=0x1f01ff, trusteeSid=sid)])
    attach_synthetic_resolution(descriptor)
    for raw in (descriptor["rawSddl"], "O:" + sid + "D:P(A;;FA;;;" + sid + ")"):
        positive = dict(descriptor, rawSddl=raw)
        validate_dacl(positive, sid)
        print(json.dumps({"control": "semantic-dacl-equivalent-positive", "result": "Pass"}))
    mutations = [("owner", "ownerSid", "S-1-5-18"), ("current-user", "tokenUserSid", "S-1-5-18"),
                 ("protection", "daclProtected", False), ("present", "daclPresent", False),
                 ("count", "aceCount", 2), ("raw-malformed", "rawSddl", "anything"),
                 ("raw-numeric-mismatch", "rawSddl", "O:S-1-5-18D:P(A;;FA;;;S-1-5-18)")]
    bad = [(name, dict(descriptor, **{key: value})) for name, key, value in mutations]
    for key, value in [("trusteeSid", "S-1-5-18"), ("qualifier", "AccessDenied"),
                       ("type", "AccessDenied"), ("inherited", True), ("flags", 1), ("mask", 1)]:
        item = copy.deepcopy(descriptor)
        item["aces"][0][key] = value
        bad.append((key, item))
    bad.append(("extra-ace", dict(descriptor, aces=descriptor["aces"] * 2)))
    for key in descriptor["aces"][0]:
        bad.append(("missing-ace-" + key, dict(descriptor, aces=[{k: v for k, v in descriptor["aces"][0].items() if k != key}])))
    for key in descriptor:
        bad.append(("missing-" + key, {k: v for k, v in descriptor.items() if k != key}))
    for name, item in bad:
        try:
            validate_dacl(item, sid)
        except ValueError:
            print(json.dumps({"control": "dacl-" + name, "result": "rejected"}))
        else:
            raise AssertionError("DACL accepted " + name)
    safe_rows = [dict(case=c, source="source", binary="binary", evidence={"moved": False}) for c in CASES]
    safe = dict(quiescent=True, cleanup_error=None)
    assert continuation_allowed(safe, safe_rows, "source", "binary", {"status": "Pass"})
    for name, receipt, containment in [("live", dict(safe, quiescent=False), {"status": "Pass"}),
                                       ("cleanup", dict(safe, cleanup_error="failure"), {"status": "Pass"}),
                                       ("containment", safe, {"status": "Fail"})]:
        assert not continuation_allowed(receipt, safe_rows, "source", "binary", containment)
        print(json.dumps({"control": "unsafe-continuation-" + name, "result": "rejected"}))
    with tempfile.TemporaryDirectory(prefix="cfd-r45-finalizer-") as scratch:
        out = Path(scratch)
        (out / "source.cs").write_bytes(b"source")
        for name, value in (("fixture-a.bin", FIXTURE_A), ("fixture-b.bin", FIXTURE_B)):
            (out / name).write_bytes(value)
        state = dict(native_qualification="Not assessed", errors=[])
        assert consume_rows([], "source", "binary", state) is False
        assert finalize(out, state, {"source.cs": digest(b"source")}, base=out) == 1
        saved = json.loads((out / "summary.json").read_text(encoding="utf-8"))
        assert saved["errors"][0]["code"] == "W0-CASE-SET" and saved["sources_after"]["source.cs"] == digest(b"source")
        assert saved["fixtures"]["fixture-a.bin"]["sha256"] == digest(FIXTURE_A)
        assert saved["probes"]["uia-capability"]["status"] == "Not assessed"
        print(json.dumps({"control": "validator-failure-finalization", "result": "Pass"}))
        (out / "source.cs").write_bytes(b"changed")
        assert finalize(out, state, {"source.cs": digest(b"source")}, base=out) == 1
        assert any(e["code"] == "W0-SOURCE-DRIFT" for e in state["errors"])
        print(json.dumps({"control": "finalizer-source-drift", "result": "rejected"}))
        binaries = out / "artifacts/bin/WindowsRuntime/release"
        binaries.mkdir(parents=True)
        (binaries / "WindowsRuntime.dll").write_bytes(b"changed")
        state["binary_files"] = {"WindowsRuntime.dll": digest(b"original")}
        assert finalize(out, state, {"source.cs": digest(b"changed")}, base=out) == 1
        assert any(e["code"] == "W0-BINARY-DRIFT" for e in state["errors"])
        print(json.dumps({"control": "finalizer-binary-drift", "result": "rejected"}))
        fixture_root = out / "fixtures"
        fixture_root.mkdir()
        (fixture_root / "inside.bin").write_bytes(FIXTURE_A)
        inventory = fixture_inventory(fixture_root)
        assert inventory["inside.bin"]["sha256"] == digest(FIXTURE_A)
        os.link(out / "fixture-a.bin", fixture_root / "outside-hardlink.bin")
        try:
            fixture_inventory(fixture_root)
        except ValueError as error:
            assert str(error) == "W0-FIXTURE-EXTERNAL-HARDLINK"
            print(json.dumps({"control": "fixture-external-hardlink", "result": "rejected"}))
        else:
            raise AssertionError("external hardlink accepted")


def validate_tree(receipt, executable_hash=None):
    if receipt.get("quiescent") is not True or len(receipt.get("observed", [])) < 3:
        raise ValueError("W0-PROCESS-NOT-OBSERVED")
    for child in receipt["observed"]:
        if not all(child.get(k) for k in ("pid", "start", "executable", "sha256")):
            raise ValueError("W0-PROCESS-IDENTITY-MISSING")
        if executable_hash is not None and child["sha256"] != executable_hash:
            raise ValueError("W0-UNEXPECTED-CHILD")
    if len({(p["pid"], p["start"]) for p in receipt["observed"]}) != len(receipt["observed"]):
        raise ValueError("W0-DUPLICATE-CHILD")


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2), encoding="utf-8", newline="\n")


def fixture_inventory(root):
    """Bounded post-job observation, not hostile concurrent namespace containment."""
    result, links = {}, {}
    if not root.is_dir() or root.is_symlink() or getattr(root.lstat(), "st_file_attributes", 0) & 0x400:
        raise ValueError("W0-FIXTURE-ROOT-NOT-OBSERVED")
    for directory, folders, files in os.walk(root, followlinks=False):
        for name in folders + files:
            path = Path(directory) / name
            info = path.lstat()
            if len(result) >= 512:
                raise ValueError("W0-FIXTURE-INVENTORY-BOUND")
            if path.is_symlink():
                resolved = path.resolve(strict=True)
                if not resolved.is_relative_to(root.resolve()):
                    raise ValueError("W0-FIXTURE-ESCAPE")
                item = dict(kind="symlink", target=str(resolved.relative_to(root.resolve())))
            elif getattr(info, "st_file_attributes", 0) & 0x400:
                raise ValueError("W0-FIXTURE-UNSUPPORTED-REPARSE")
            elif stat.S_ISREG(info.st_mode):
                if info.st_size > 1024 * 1024:
                    raise ValueError("W0-FIXTURE-SIZE")
                key = (info.st_dev, info.st_ino)
                links.setdefault(key, []).append(info.st_nlink)
                payload = path.read_bytes()
                item = dict(kind="file", sha256=digest(payload), bytesBase64=base64.b64encode(payload).decode("ascii"), size=info.st_size,
                            identity=list(key), links=info.st_nlink)
            elif stat.S_ISDIR(info.st_mode):
                item = dict(kind="directory", identity=[info.st_dev, info.st_ino])
            else:
                raise ValueError("W0-FIXTURE-UNEXPECTED-KIND")
            result[str(path.relative_to(root))] = item
    if any(any(count != len(counts) for count in counts) for counts in links.values()):
        raise ValueError("W0-FIXTURE-EXTERNAL-HARDLINK")
    return result


def continuation_allowed(native, rows, source, binary, containment):
    if native.get("quiescent") is not True or native.get("cleanup_error") is not None:
        return False
    if containment.get("status") != "Pass" or len(rows) != len(CASES):
        return False
    if {r.get("case") for r in rows} != set(CASES):
        return False
    if any(r.get("source") != source or r.get("binary") != binary for r in rows):
        return False
    ancestor = next(r for r in rows if r["case"] == "ancestor-substitution")
    return ancestor.get("evidence", {}).get("moved") is False


def finalize(out, summary, manifest, base=ROOT):
    """Best available independent inventories survive semantic/parser exceptions."""
    errors = summary.setdefault("errors", [])
    after = {}
    for name in manifest:
        try:
            after[name] = digest((base / name).read_bytes())
        except OSError as error:
            after[name] = "Not recorded: " + str(error)
    summary["sources_after"] = after
    if after != manifest:
        errors.append(dict(stage="finalize", code="W0-SOURCE-DRIFT"))
    if "binary_files" in summary:
        binary_after = {}
        for name in summary["binary_files"]:
            try:
                binary_after[name] = digest((out / "artifacts/bin/WindowsRuntime/release" / name).read_bytes())
            except OSError as error:
                binary_after[name] = "Not recorded: " + str(error)
        summary["binary_files_after"] = binary_after
        if binary_after != summary["binary_files"]:
            errors.append(dict(stage="finalize", code="W0-BINARY-DRIFT"))
    for name, expected in (("fixture-a.bin", FIXTURE_A), ("fixture-b.bin", FIXTURE_B)):
        try:
            actual = (out / name).read_bytes()
            summary.setdefault("fixtures", {})[name] = dict(sha256=digest(actual), expected=digest(expected))
            if actual != expected:
                errors.append(dict(stage="finalize", code="W0-INPUT-MUTATED", path=name))
        except OSError as error:
            summary.setdefault("fixtures", {})[name] = dict(status="Not recorded", reason=str(error))
    summary["processes"] = {}
    summary["raw_rows_path"] = "native/stdout.txt"
    for path in sorted(out.glob("*/process.json")):
        try:
            summary["processes"][path.parent.name] = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError) as error:
            errors.append(dict(stage="finalize", code="W0-PROCESS-RECEIPT", reason=str(error)))
    for probe in ("tree-timeout", "observer-fault", "uia-capability"):
        summary.setdefault("probes", {}).setdefault(probe, dict(status="Not assessed", reason="prerequisite stage did not complete"))
    if errors:
        summary["native_qualification"] = "Fail"
    write_json(out / "summary.json", summary)
    return 1 if errors or summary["native_qualification"] == "Fail" else 0 if summary["native_qualification"] == "Pass" else 3


def run_local(argv, out, timeout, env):
    started = time.monotonic()
    with (out / "stdout.txt").open("wb") as stdout, (out / "stderr.txt").open("wb") as stderr:
        child = subprocess.Popen(argv, stdout=stdout, stderr=stderr, env=env, cwd=ROOT,
                                 start_new_session=True)
        timed_out = False
        try:
            code = child.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            timed_out = True
            os.killpg(child.pid, signal.SIGKILL)
            code = child.wait(timeout=10)
        try:
            os.killpg(child.pid, 0)
            quiescent = False
        except ProcessLookupError:
            quiescent = True
    return dict(argv=argv, pid=child.pid, exit=code, timeout=timed_out, quiescent=quiescent,
                elapsed_seconds=time.monotonic() - started, native_qualification="Not assessed")


def run_windows(argv, out, timeout, env, observer_fault=False):
    """Create suspended -> assign kill-on-close job -> resume; never kill by PID scan.

    ABI: Microsoft processthreadsapi.h, winnt.h and jobapi2.h, linked in design.
    Every process is owned before its first instruction. Query failure is fatal.
    """
    import ctypes as C
    import msvcrt
    U32, U64, PTR = C.c_uint32, C.c_uint64, C.c_void_p

    class SI(C.Structure):
        _fields_ = [("cb", U32), ("reserved", PTR), ("desktop", PTR), ("title", PTR),
                    ("x", U32), ("y", U32), ("xs", U32), ("ys", U32), ("xc", U32),
                    ("yc", U32), ("fill", U32), ("flags", U32), ("show", C.c_uint16),
                    ("reserved2size", C.c_uint16), ("reserved2", PTR),
                    ("stdin", PTR), ("stdout", PTR), ("stderr", PTR)]

    class PI(C.Structure):
        _fields_ = [("process", PTR), ("thread", PTR), ("pid", U32), ("tid", U32)]

    class Limits(C.Structure):
        _fields_ = [("user", U64), ("job", U64), ("flags", U32), ("min", U64),
                    ("max", U64), ("active", U32), ("affinity", U64),
                    ("priority", U32), ("scheduling", U32), ("io", U64 * 6),
                    ("processmemory", U64), ("jobmemory", U64), ("peakprocess", U64),
                    ("peakjob", U64)]

    assert C.sizeof(SI) == 104 and C.sizeof(PI) == 24 and C.sizeof(Limits) == 144
    api = C.WinDLL("kernel32", use_last_error=True)
    signatures = {
        "CreateJobObjectW": (PTR, [PTR, C.c_wchar_p]),
        "SetInformationJobObject": (U32, [PTR, U32, PTR, U32]),
        "QueryInformationJobObject": (U32, [PTR, U32, PTR, U32, PTR]),
        "CreateProcessW": (U32, [C.c_wchar_p, C.c_wchar_p, PTR, PTR, U32, U32, PTR,
                                  C.c_wchar_p, C.POINTER(SI), C.POINTER(PI)]),
        "AssignProcessToJobObject": (U32, [PTR, PTR]), "ResumeThread": (U32, [PTR]),
        "WaitForSingleObject": (U32, [PTR, U32]), "GetExitCodeProcess": (U32, [PTR, PTR]),
        "TerminateJobObject": (U32, [PTR, U32]), "TerminateProcess": (U32, [PTR, U32]),
        "CloseHandle": (U32, [PTR]), "OpenProcess": (PTR, [U32, U32, U32]),
        "GetProcessTimes": (U32, [PTR, PTR, PTR, PTR, PTR]),
        "QueryFullProcessImageNameW": (U32, [PTR, U32, C.c_wchar_p, PTR])}
    for name, (result, arguments) in signatures.items():
        method = getattr(api, name)
        method.restype, method.argtypes = result, arguments

    def check(ok):
        if not ok:
            raise OSError(C.get_last_error(), "W0-WIN32")

    def active_pids(job):
        # Fixed 128-process ceiling; ERROR_MORE_DATA is a refusal, never truncation.
        buffer = C.create_string_buffer(8 + 128 * 8)
        check(api.QueryInformationJobObject(job, 3, buffer, len(buffer), None))
        assigned, count = U32.from_buffer(buffer).value, U32.from_buffer(buffer, 4).value
        if assigned != count or count > 128:
            raise ValueError("W0-PROCESS-OBSERVER-INCOMPLETE")
        return list((U64 * count).from_buffer(buffer, 8))

    def identity(pid):
        handle = api.OpenProcess(0x1000, 0, pid)
        check(handle)
        try:
            times = [U64() for _ in range(4)]
            check(api.GetProcessTimes(handle, *(C.byref(t) for t in times)))
            name, size = C.create_unicode_buffer(32768), U32(32768)
            check(api.QueryFullProcessImageNameW(handle, 0, name, C.byref(size)))
            return dict(pid=pid, start=times[0].value, executable=name.value,
                        sha256=digest(Path(name.value).read_bytes()))
        finally:
            api.CloseHandle(handle)

    started, observed = time.monotonic(), {}
    job = api.CreateJobObjectW(None, None)
    check(job)
    info, process = Limits(), PI()
    info.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE; no breakaway.
    assigned, quiescent, code, timed_out = False, False, None, False
    try:
        check(api.SetInformationJobObject(job, 9, C.byref(info), C.sizeof(info)))
        with (out / "stdout.txt").open("wb") as stdout, (out / "stderr.txt").open("wb") as stderr, open(os.devnull, "rb") as stdin:
            handles = [msvcrt.get_osfhandle(f.fileno()) for f in (stdin, stdout, stderr)]
            for handle in handles:
                os.set_handle_inheritable(handle, True)
            startup = SI(cb=C.sizeof(SI), flags=0x100, stdin=handles[0], stdout=handles[1], stderr=handles[2])
            block = C.create_unicode_buffer("\0".join(k + "=" + v for k, v in sorted(env.items())) + "\0\0")
            command = C.create_unicode_buffer(subprocess.list2cmdline(argv))
            try:
                check(api.CreateProcessW(argv[0], command, None, None, 1, 0x404,
                                         block, str(ROOT), C.byref(startup), C.byref(process)))
            finally:
                for handle in handles:
                    os.set_handle_inheritable(handle, False)
            check(api.AssignProcessToJobObject(job, process.process))
            assigned = True
            observed[process.pid] = identity(process.pid)
            if api.ResumeThread(process.thread) == 0xFFFFFFFF:
                raise OSError(C.get_last_error(), "W0-RESUME")
            while True:
                if observer_fault:
                    raise ValueError("W0-INJECTED-OBSERVER-FAILURE")
                for pid in active_pids(job):
                    if pid not in observed:
                        observed[pid] = identity(pid)
                wait = api.WaitForSingleObject(process.process, 50)
                if wait == 0:
                    result = U32()
                    check(api.GetExitCodeProcess(process.process, C.byref(result)))
                    code = result.value
                    break
                if wait != 258:
                    raise OSError(C.get_last_error(), "W0-WAIT")
                if time.monotonic() - started >= timeout:
                    timed_out, code = True, 124
                    break
    finally:
        cleanup_error = None
        try:
            if assigned:
                check(api.TerminateJobObject(job, 125))
                deadline = time.monotonic() + 10
                while active_pids(job) and time.monotonic() < deadline:
                    time.sleep(0.02)
                quiescent = not active_pids(job)
            elif process.process:
                # Only the retained, never-resumed process handle is eligible here.
                check(api.TerminateProcess(process.process, 125))
                quiescent = api.WaitForSingleObject(process.process, 10000) == 0
        except (OSError, ValueError) as error:
            cleanup_error = str(error)
        finally:
            for handle in (process.thread, process.process, job):
                if handle:
                    api.CloseHandle(handle)
        receipt = dict(argv=argv, exit=code, timeout=timed_out, quiescent=quiescent,
                       cleanup_error=cleanup_error, observed=list(observed.values()), elapsed_seconds=time.monotonic() - started)
        (out / "process.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8", newline="\n")
    if not quiescent:
        raise ValueError("W0-CLEANUP-NOT-OBSERVED")
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    # Existing paths are never reused or recursively deleted.
    out = args.output.absolute() if args.output else Path(tempfile.gettempdir()) / ("cfd-w0-" + os.urandom(8).hex())
    out.mkdir(parents=False, exist_ok=False)
    manifest = {p: digest((ROOT / p).read_bytes()) for p in SOURCES}
    source = digest(json.dumps(manifest, sort_keys=True, separators=(",", ":")).encode())
    env = dict(os.environ, DOTNET_CLI_HOME=str(out / "dotnet-home"),
               NUGET_PACKAGES=str(out / "nuget"), DOTNET_CLI_TELEMETRY_OPTOUT="1",
               DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
               DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE="true", MSBUILDDISABLENODEREUSE="1")
    windows = sys.platform == "win32" and platform.machine().lower() in ("amd64", "x86_64")
    runner = run_windows if windows else run_local
    summary = dict(schema=1, source=source, sources=manifest, os=platform.platform(),
                   arch=platform.machine(), native_qualification="Not assessed", output=str(out),
                   runner_image={k: os.environ.get(k, "Not recorded") for k in ("ImageOS", "ImageVersion", "RUNNER_ARCH")})
    (out / "source.json").write_text(json.dumps(summary, indent=2), encoding="utf-8", newline="\n")

    summary.update(errors=[], probes={}, stage="start")
    try:
        execute(out, manifest, source, env, windows, runner, summary)
    except (OSError, ValueError, AssertionError, subprocess.SubprocessError) as error:
        summary["errors"].append(dict(stage=summary["stage"], code=str(error)))
    finally:
        result = finalize(out, summary, manifest)
    print(json.dumps(summary, sort_keys=True))
    return result


def execute(out, manifest, source, env, windows, runner, summary):
    def run(name, argv, timeout=90, **kwargs):
        summary["stage"] = name
        target = out / name
        target.mkdir()
        receipt = runner(argv, target, timeout, env, **kwargs)
        (target / "process.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8", newline="\n")
        if not receipt["quiescent"]:
            raise ValueError("W0-CLEANUP-NOT-OBSERVED")
        return receipt, (target / "stdout.txt").read_text(encoding="utf-8", errors="strict")

    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("W0-SDK-MISSING")
    sdk, version = run("sdk", [dotnet, "--version"])
    if sdk["exit"] != 0 or version.strip() != "10.0.203":
        raise ValueError("W0-SDK-MISMATCH")
    sdk_info, _ = run("sdk-info", [dotnet, "--info"])
    if sdk_info["exit"] != 0:
        raise ValueError("W0-SDK-INFO-MISSING")
    build, _ = run("build", [dotnet, "build", str(ROOT / SOURCES[1]), "--configuration", "Release",
                              "--artifacts-path", str(out / "artifacts"), "--disable-build-servers",
                              "-p:NuGetAudit=false", "-p:UseSharedCompilation=false"], timeout=180)
    if build["exit"] != 0:
        raise ValueError("W0-BUILD-FAILED")
    executable = out / "artifacts/bin/WindowsRuntime/release" / ("WindowsRuntime.exe" if windows else "WindowsRuntime")
    binary_files = {name: digest((executable.parent / name).read_bytes()) for name in
                    (executable.name, "WindowsRuntime.dll", "WindowsRuntime.deps.json", "WindowsRuntime.runtimeconfig.json")}
    binary = digest("".join(name + "\0" + binary_files[name] + "\n" for name in sorted(binary_files)).encode())
    summary.update(binary=binary, binary_files=binary_files, sdk=version.strip())
    write_json(out / "binary-manifest.json", dict(binary=binary, files=binary_files))
    env = dict(env, W0_SOURCE=source)
    controls, control_output = run("diagnostic-controls", [str(executable), "--diagnostic-controls"])
    control_rows = [json.loads(line) for line in control_output.splitlines()]
    summary["diagnostic_controls"] = control_rows
    if controls["exit"] != 0:
        raise ValueError("W0-DIAGNOSTIC-CONTROLS-FAILED")
    validate_diagnostic_controls(control_rows, source, binary)
    control_mutations = []
    for label, mutate in (
        ("suppressed-original", lambda rows: rows[0].update(originalCalls=0)),
        ("suppressed-second-arm", lambda rows: rows[0]["evidence"]["replacementDiagnostics"][1].update(status="Not assessed")),
        ("unsafe-continuation", lambda rows: rows[4].update(originalCalls=1)),
        ("cleanup-continuation", lambda rows: rows[5].update(originalCalls=1)),
        ("wrong-binary", lambda rows: rows[0].update(binary="wrong"))):
        changed = copy.deepcopy(control_rows)
        mutate(changed)
        try:
            validate_diagnostic_controls(changed, source, binary)
        except ValueError as error:
            control_mutations.append(dict(control=label, result="rejected", error=str(error)))
        else:
            raise ValueError("W0-DIAGNOSTIC-NEGATIVE-ACCEPTED-" + label)
    summary["diagnostic_control_mutations"] = control_mutations
    final_controls, final_output = run("final-arm-controls", [str(executable), "--final-arm-controls"])
    final_rows = [json.loads(line) for line in final_output.splitlines()]
    summary["final_arm_controls"] = final_rows
    if final_controls["exit"] != 0:
        raise ValueError("W0-FINAL-ARM-CONTROLS-FAILED")
    validate_final_arm_controls(final_rows, source, binary)
    constructor, constructor_output = run("constructor-controls", [str(executable), "--constructor-controls", str(out / "constructor-fixtures")])
    constructor_rows = [json.loads(line) for line in constructor_output.splitlines()]
    summary["constructor_controls"] = constructor_rows
    if constructor["exit"] != 0:
        raise ValueError("W0-CONSTRUCTOR-CONTROLS-FAILED")
    validate_constructor_controls(constructor_rows, source, binary)
    negatives = []
    for label, original, validator, mutate in (
        ("final-original-ran", final_rows, validate_final_arm_controls, lambda rows: rows[0].update(originalCalls=1)),
        ("final-later-arm-ran", final_rows, validate_final_arm_controls, lambda rows: rows[0].update(operations=2)),
        ("final-error-lost", final_rows, validate_final_arm_controls, lambda rows: rows[0]["evidence"]["replacementDiagnostics"][0].pop("finalContainmentException")),
        ("final-case-missing", final_rows, validate_final_arm_controls, lambda rows: rows[0].update(refusedRows="")),
        ("constructor-leak", constructor_rows, validate_constructor_controls, lambda rows: rows[0].update(closed=[False])),
        ("constructor-foreign-closed", constructor_rows, validate_constructor_controls, lambda rows: rows[0].update(foreignOpen=False)),
        ("constructor-error-replaced", constructor_rows, validate_constructor_controls, lambda rows: rows[0].update(sameError=False))):
        changed = copy.deepcopy(original)
        mutate(changed)
        try:
            validator(changed, source, binary)
        except ValueError as error:
            negatives.append(dict(control=label, result="rejected", error=str(error)))
        else:
            raise ValueError("W0-R51-NEGATIVE-ACCEPTED-" + label)
    summary["r51_mutations"] = negatives
    for name, fixture in (("fixture-a.bin", FIXTURE_A), ("fixture-b.bin", FIXTURE_B)):
        (out / name).write_bytes(fixture)
    runenv = dict(env, W0_SOURCE=source)
    env = runenv
    native, output = run("native", [str(executable), str(out / "native-fixtures"), str(out / "fixture-a.bin"), str(out / "fixture-b.bin")])
    summary["stage"] = "parse-native-rows"
    rows = [json.loads(line) for line in output.splitlines()]
    summary["rows"] = rows
    write_json(out / "rows.json", rows)
    qualified = consume_rows(rows, source, binary, summary)
    summary["native_exit"] = native["exit"]
    summary["native_qualification"] = "Fail" if summary["errors"] or any(r["status"] == "Fail" for r in rows) or native["exit"] not in (0, 3) else "Pass" if qualified and windows and native["exit"] == 0 else "Not assessed"
    containment = dict(status="Not assessed", reason="non-Windows host" if not windows else "native process not quiescent")
    if windows and native.get("quiescent") is True:
        try:
            inventory = fixture_inventory(out / "native-fixtures")
            containment = dict(status="Pass", scope="post-job disposable fixture inventory only", inventory=inventory)
        except (OSError, ValueError) as error:
            containment = dict(status="Fail", reason=str(error))
            summary["errors"].append(dict(stage="containment", code=str(error)))
    summary["containment"] = containment
    if (out / "fixture-a.bin").read_bytes() != FIXTURE_A or (out / "fixture-b.bin").read_bytes() != FIXTURE_B:
        raise ValueError("W0-INPUT-MUTATED")
    safe = windows and continuation_allowed(native, rows, source, binary, containment)
    if safe:
        verify_sources(ROOT, manifest)
    for probe in ("tree-timeout", "observer-fault", "uia-capability"):
        summary["probes"][probe] = dict(status="Not assessed", reason="non-Windows host" if not windows else "ownership/fixture containment prerequisite refused" if not safe else "prior probe did not complete")
    if safe:
        tree, _ = run("tree-timeout", [str(executable), "--tree-root"], timeout=5)
        validate_tree(tree, binary_files[executable.name])
        if not tree["timeout"] or tree["exit"] != 124:
            raise ValueError("W0-TIMEOUT-NOT-OBSERVED")
        summary["probes"]["tree-timeout"] = dict(status="Pass")
        try:
            run("observer-fault", [str(executable), "--tree-root"], timeout=5, observer_fault=True)
        except ValueError as error:
            if str(error) != "W0-INJECTED-OBSERVER-FAILURE":
                raise
            fault = json.loads((out / "observer-fault/process.json").read_text(encoding="utf-8"))
            if fault["quiescent"] is not True:
                raise ValueError("W0-FAULT-CLEANUP-NOT-OBSERVED")
        else:
            raise ValueError("W0-OBSERVER-FAULT-ACCEPTED")
        summary["probes"]["observer-fault"] = dict(status="Pass")
        powershell = shutil.which("powershell.exe")
        if not powershell:
            raise ValueError("W0-UIA-PROBE-UNAVAILABLE")
        uia, uia_output = run("uia-capability", [powershell, "-NoProfile", "-NonInteractive", "-Command",
             "[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); $ErrorActionPreference='Stop'; Add-Type -AssemblyName UIAutomationClient; "
             "$r=[System.Windows.Automation.AutomationElement]::RootElement; "
             "@{rootName=$r.Current.Name; childCount=$r.FindAll([System.Windows.Automation.TreeScope]::Children,"
             "[System.Windows.Automation.Condition]::TrueCondition).Count; "
             "userInteractive=[Environment]::UserInteractive; sessionId=(Get-Process -Id $PID).SessionId} | ConvertTo-Json -Compress"])
        if uia["exit"] != 0:
            raise ValueError("W0-UIA-PROBE-FAILED")
        summary["probes"]["uia-capability"] = dict(status="Observed", evidence=json.loads(uia_output))


if __name__ == "__main__":
    # PLAT-A: match pack-doctor's legacy-console guard; never depend on cp1252.
    for _stream in (sys.stdout, sys.stderr):
        if hasattr(_stream, "reconfigure"):
            try:
                _stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    try:
        sys.exit(main())
    except (OSError, ValueError, AssertionError, subprocess.SubprocessError) as error:
        print(json.dumps({"code": "W0-FAILED", "error": str(error)}), file=sys.stderr)
        sys.exit(1)
