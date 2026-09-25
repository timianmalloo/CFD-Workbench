#!/usr/bin/env python3
"""Disposable W0 qualification. No product acceptance or automatic dispatch."""
import argparse
import base64
import copy
import hashlib
import json
import ntpath
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
    r53_self_test()
    r54_self_test()
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


def native_metadata(path):
    """Same Win32 identity representation as the retained native creation handle.

    Deliberately do not equate Python st_ino/st_dev with Win32 volume/file ids.
    """
    import ctypes
    from ctypes import wintypes as w
    class Info(ctypes.Structure):
        _fields_ = [(name, w.DWORD) for name in ("attributes", "c0", "c1", "a0", "a1", "w0", "w1",
                                                 "volume", "sizeHigh", "sizeLow", "links", "indexHigh", "indexLow")]
    api = ctypes.WinDLL("kernel32", use_last_error=True)
    api.CreateFileW.argtypes = [w.LPCWSTR, w.DWORD, w.DWORD, ctypes.c_void_p, w.DWORD, w.DWORD, w.HANDLE]
    api.CreateFileW.restype = w.HANDLE
    api.GetFileInformationByHandle.argtypes = [w.HANDLE, ctypes.POINTER(Info)]
    api.GetFileInformationByHandle.restype = w.BOOL
    api.CloseHandle.argtypes = [w.HANDLE]
    api.CloseHandle.restype = w.BOOL
    if ctypes.sizeof(Info) != 52:
        raise ValueError("R53-INVENTORY-ABI")
    handle = api.CreateFileW(str(path), 0, 7, None, 3, 0x00200000, None)
    if handle == ctypes.c_void_p(-1).value:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        info = Info()
        if not api.GetFileInformationByHandle(handle, ctypes.byref(info)):
            raise ctypes.WinError(ctypes.get_last_error())
        return dict(identity=f"{info.volume:08x}:{info.indexHigh:08x}{info.indexLow:08x}",
                    attributes=info.attributes, size=(info.sizeHigh << 32) | info.sizeLow, links=info.links)
    finally:
        if not api.CloseHandle(handle):
            raise ctypes.WinError(ctypes.get_last_error())


def receipt_object(value):
    if type(value) is not dict:
        raise ValueError("R54-OBJECT")
    return value


def receipt_require(value):
    if not value:
        raise ValueError("R54-RELATION")


def receipt_text(value):
    if type(value) is not str or not value or "\0" in value:
        raise ValueError("R54-TEXT")
    return value


def receipt_integer(value, bits=32):
    if type(value) is not int or not 0 <= value < 2 ** bits:
        raise ValueError("R54-INTEGER")
    return value


def receipt_identity(value):
    if type(value) is not str or re.fullmatch(r"[0-9a-f]{8}:[0-9a-f]{16}", value) is None:
        raise ValueError("R54-IDENTITY")
    return value


def receipt_boolean(value):
    if type(value) is not bool:
        raise ValueError("R54-BOOLEAN")
    return value


def receipt_snapshot(value, directory=False, exists=True):
    value = receipt_object(value)
    fields = {"exists", "identity", "attributes" if directory else "sha256", "error"}
    receipt_require(fields <= value.keys() and receipt_boolean(value.get("exists")) is exists)
    if exists:
        receipt_identity(value.get("identity"))
        receipt_require(value["error"] is None)
        if directory:
            receipt_require(receipt_integer(value.get("attributes")) & 0x410 == 0x10)
        else:
            receipt_require(type(value.get("sha256")) is str and re.fullmatch(r"[0-9a-f]{64}", value["sha256"]) is not None)
    else:
        # DirectorySnapshot catches the actual Win32 missing-path observation.
        receipt_require(directory and value["identity"] is None and value["attributes"] is None and
                        type(value["error"]) is str and value["error"] in ("2", "3"))
    return value


def receipt_path(value):
    value = receipt_text(value)
    receipt_require(re.match(r"^[A-Za-z]:\\", value) is not None and "/" not in value and
                    all(part not in ("", ".", "..") for part in value[3:].split("\\")))
    return value


def expected_denial(rows, source, binary):
    receipt_require(type(rows) is list)
    for row in rows:
        receipt_object(row)
    matches = [r for r in rows if r.get("case") == "denial"]
    receipt_require(len(matches) == 1)
    row = matches[0]
    item = receipt_object(receipt_object(row.get("evidence")).get("expectedDenial"))
    meta = receipt_object(item.get("metadata"))
    receipt_identity(meta.get("identity"))
    receipt_text(item.get("path"))
    receipt_require(row.get("source") == source and row.get("binary") == binary and row.get("status") == "Pass" and
                    receipt_boolean(item.get("denied")) and receipt_boolean(item.get("creationHandleRetained")) and
                    receipt_integer(item.get("error")) == 5 and receipt_integer(item.get("access")) == 0xc0000000 and
                    "sha256" in item and item["sha256"] is None and item.get("hashStatus") == "Not assessed: read access denied" and
                    set(meta) == {"identity", "attributes", "size", "links"} and
                    receipt_integer(meta.get("attributes")) & 0x410 == 0 and
                    receipt_integer(meta.get("links")) == 1 and receipt_integer(meta.get("size"), 64) == 0)
    return item


def fixture_inventory(root, denial=None, metadata=native_metadata, read=lambda path: path.read_bytes()):
    """Bounded post-job observation, not hostile concurrent namespace containment."""
    result, links, denial_seen = {}, {}, False
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
                observed_before = metadata(path) if denial is not None and str(path) == denial["path"] else None
                try:
                    payload = read(path)
                    if denial is not None and str(path) == denial["path"]:
                        raise ValueError("R53-EXPECTED-DENIAL-WAS-READABLE")
                    item = dict(kind="file", sha256=digest(payload), bytesBase64=base64.b64encode(payload).decode("ascii"), size=info.st_size,
                                identity=list(key), links=info.st_nlink)
                except PermissionError as error:
                    if (denial is None or str(path) != denial["path"] or getattr(error, "winerror", None) != 5 or
                        error.errno != 13 or info.st_nlink != 1 or info.st_size != denial["metadata"]["size"]):
                        raise ValueError("R53-UNEXPECTED-READ-DENIAL") from error
                    before = metadata(path)
                    if before != denial["metadata"] or observed_before != before or metadata(path) != before:
                        raise ValueError("R53-DENIAL-IDENTITY-OR-METADATA-DRIFT")
                    denial_seen = True
                    item = dict(kind="expected-unreadable-file", sha256=None, bytesBase64=None,
                                hashStatus=denial["hashStatus"], metadata=before, errno=error.errno, winerror=error.winerror)
            elif stat.S_ISDIR(info.st_mode):
                item = dict(kind="directory", identity=[info.st_dev, info.st_ino])
            else:
                raise ValueError("W0-FIXTURE-UNEXPECTED-KIND")
            result[str(path.relative_to(root))] = item
    if any(any(count != len(counts) for count in counts) for counts in links.values()):
        raise ValueError("W0-FIXTURE-EXTERNAL-HARDLINK")
    if denial is not None and not denial_seen:
        raise ValueError("R53-EXPECTED-DENIAL-MISSING")
    return result


def ordered_phases(preconditions, probes, mutation, trace=None):
    """The real execution seam: any prerequisite/probe exception prevents mutation."""
    for name, action in (("preconditions", preconditions), ("independent-probes", probes), ("mutation", mutation)):
        if trace is not None:
            trace.append(dict(phase=name, state="started"))
        action()
        if trace is not None:
            trace.append(dict(phase=name, state="completed"))


DISCRIMINATION = ("ex-replace", "ex-no-replace", "ex-conflict", "ex-cancel-before", "ex-cancel-after", "ex-foreign-cleanup",
                  "ancestor-zero", "ancestor-list-held", "ancestor-list-released")


def validate_layout(row, source, binary):
    name = "C:\\owned\\caf\u00e9.bin".encode("utf-16-le")
    expected = (3).to_bytes(4, "little") + bytes(12) + len(name).to_bytes(4, "little") + name + bytes(4)
    if (row.get("source") != source or row.get("binary") != binary or row.get("pass") is not True or
        row.get("infoClass") != 22 or row.get("flags") != 3 or row.get("widths") != [4, 8, 4, 2] or
        row.get("offsets") != [0, 8, 16, 20] or row.get("structSize") != 24 or row.get("nameBytes") != len(name) or
        row.get("bufferLength") != len(expected) or row.get("bufferHex") != expected.hex()):
        raise ValueError("R53-LAYOUT-RECEIPT")


def validate_discrimination(rows, source, binary):
    receipt_require(type(rows) is list and len(rows) == 9)
    arms = []
    for row in rows:
        receipt_object(row)
        arms.append(receipt_text(row.get("arm")))
    receipt_require(set(arms) == set(DISCRIMINATION))
    for row in rows:
        e, arm = receipt_object(row.get("evidence")), row["arm"]
        receipt_require(row.get("source") == source and row.get("binary") == binary and row.get("status") == "Observed")
        directory = receipt_path(e.get("directory"))
        receipt_require(ntpath.basename(directory) == arm and e.get("arm") == arm and
                        e.get("cleanup") == "all acquired handles disposed" and e.get("containment") == "Verified disposable root only")
        arm_id = receipt_identity(e.get("armDirectoryIdentity"))
        chain = e.get("parentIdentities")
        receipt_require(type(chain) is list and len(chain) > 0)
        for identity in chain:
            receipt_identity(identity)
        receipt_require(e.get("fixtureA") == digest(FIXTURE_A) and e.get("fixtureB") == digest(FIXTURE_B))
        receipt_boolean(e.get("publication"))
        receipt_require(receipt_boolean(e.get("durability")) is False)
        if arm.startswith("ex-"):
            publish = arm in ("ex-replace", "ex-cancel-after", "ex-foreign-cleanup")
            invoke = arm not in ("ex-conflict", "ex-cancel-before")
            old, staged = receipt_identity(e.get("oldIdentity")), receipt_identity(e.get("stagedIdentity"))
            after = receipt_identity(e.get("oldIdentityAfter"))
            new = receipt_snapshot(e.get("newPath"))
            receipt_require(chain[-1] == arm_id and old != staged and old == after and
                            e["publication"] is publish and receipt_boolean(e.get("callInvoked")) is invoke and
                            receipt_boolean(e.get("oldHandleOpen")) is True and
                            receipt_integer(e.get("targetAccess")) == 0x80000000 and receipt_integer(e.get("targetShare")) == 7 and
                            e.get("targetLifetime") == "retained through call and all observations" and
                            all(e.get(k) == digest(FIXTURE_A) for k in ("oldHash", "oldHashAfter")) and
                            all(e.get(k) == digest(FIXTURE_B) for k in ("stagedHash", "stagedHashAfter")) and
                            new["identity"] == (staged if publish else old) and new["sha256"] == digest(FIXTURE_B if publish else FIXTURE_A) and
                            receipt_boolean(e.get("cancellationRequested")) is (arm in ("ex-cancel-before", "ex-cancel-after")) and
                            e.get("saveCode") == ("DOC-SAVE-UNCERTAIN" if publish else "DOC-CANCELLED" if arm == "ex-cancel-before" else "DOC-CONFLICT") and
                            e.get("expectedHash") == digest(FIXTURE_B if arm == "ex-conflict" else FIXTURE_A))
            if invoke:
                call = receipt_object(e.get("exCall"))
                destination = receipt_path(call.get("destinationPath"))
                source_path = receipt_path(call.get("sourcePath"))
                try:
                    byte_count = len(destination.encode("utf-16-le"))
                except UnicodeEncodeError as error:
                    raise ValueError("R54-UTF16") from error
                before = receipt_snapshot(call.get("destinationBefore"))
                receipt_require(source_path == ntpath.join(directory, "staged.bin") and destination == ntpath.join(directory, "target.bin") and
                                receipt_identity(call.get("sourceIdentity")) == staged and call.get("sourceHash") == e["stagedHash"] and
                                before["identity"] == old and before["sha256"] == e["oldHash"] and
                                receipt_integer(call.get("class"), 31) == 22 and receipt_integer(call.get("flags")) == (2 if arm == "ex-no-replace" else 3) and
                                receipt_integer(call.get("nameBytes"), 31) == byte_count and receipt_integer(call.get("bufferLength"), 31) == 24 + byte_count and
                                receipt_boolean(call.get("returned")) is publish and "nativeError" in call)
                if publish:
                    receipt_require(call["nativeError"] is None)
                else:
                    receipt_require(receipt_integer(call["nativeError"], 31) in (80, 183))
            else:
                receipt_require("exCall" not in e)
            if arm == "ex-foreign-cleanup":
                receipt_require(receipt_boolean(e.get("cleanupRefused")) is True and e.get("foreignHash") == digest(FIXTURE_A))
            try:
                validate_dacl(e.get("stagedDacl"), e.get("tokenUserSid"))
            except (AttributeError, TypeError, KeyError) as error:
                raise ValueError("R54-DACL-SCHEMA") from error
        else:
            held = arm == "ancestor-list-held"
            receipt_require(e["publication"] is False and receipt_integer(e.get("access")) == (0 if arm == "ancestor-zero" else 129) and
                            receipt_integer(e.get("share")) == 3 and receipt_integer(e.get("flags")) == 0 and
                            receipt_boolean(e.get("handleOpenAtCall")) is (arm != "ancestor-list-released") and
                            receipt_boolean(e.get("moved")) is (not held) and receipt_identity(e.get("heldIdentity")) == arm_id and
                            receipt_path(e.get("sourcePath")) == directory and receipt_path(e.get("destinationPath")) == directory + "-moved")
            before = receipt_snapshot(e.get("sourceBefore"), directory=True)
            receipt_snapshot(e.get("destinationBefore"), directory=True, exists=False)
            source_after = receipt_snapshot(e.get("sourceAfter"), directory=True, exists=held)
            destination_after = receipt_snapshot(e.get("destinationAfter"), directory=True, exists=not held)
            observed = source_after if held else destination_after
            receipt_require(before["identity"] == arm_id and observed["identity"] == arm_id and before["attributes"] == observed["attributes"] and "nativeError" in e)
            if held:
                receipt_require(receipt_integer(e["nativeError"], 31) in (5, 32))
            else:
                receipt_require(e["nativeError"] is None)


def r54_fixture_rows(prefix="C:\\owned"):
    """Synthetic producer-shaped receipts, never native Windows evidence."""
    sid = "S-1-5-21-1-2-3-500"
    descriptor = dict(rawSddl="O:" + sid + "D:P(A;;FA;;;" + sid + ")", ownerSid=sid, tokenUserSid=sid,
                      daclPresent=True, daclProtected=True, aceCount=1,
                      aces=[dict(type="AccessAllowed", qualifier="AccessAllowed", flags=0, inherited=False, mask=0x1f01ff, trusteeSid=sid)])
    attach_synthetic_resolution(descriptor)
    old, staged, directory_id, root_id = ["12345678:" + f"{i:016x}" for i in range(1, 5)]
    file = lambda identity, payload: dict(exists=True, identity=identity, sha256=digest(payload), error=None)
    folder = lambda: dict(exists=True, identity=directory_id, attributes=16, error=None)
    absent = lambda: dict(exists=False, identity=None, attributes=None, error="2")
    rows = []
    for arm in DISCRIMINATION:
        directory = ntpath.join(prefix, arm)
        e = dict(arm=arm, directory=directory, armDirectoryIdentity=directory_id, fixtureA=digest(FIXTURE_A), fixtureB=digest(FIXTURE_B),
                 tokenUserSid=sid, publication=False, durability=False, cleanup="all acquired handles disposed", containment="Verified disposable root only")
        if arm.startswith("ex-"):
            publish = arm in ("ex-replace", "ex-cancel-after", "ex-foreign-cleanup")
            invoke = arm not in ("ex-conflict", "ex-cancel-before")
            e.update(parentIdentities=[root_id, directory_id], oldIdentity=old, oldIdentityAfter=old, stagedIdentity=staged,
                     oldHash=digest(FIXTURE_A), oldHashAfter=digest(FIXTURE_A), stagedHash=digest(FIXTURE_B), stagedHashAfter=digest(FIXTURE_B),
                     targetAccess=0x80000000, targetShare=7, targetLifetime="retained through call and all observations",
                     publication=publish, callInvoked=invoke, oldHandleOpen=True, newPath=file(staged if publish else old, FIXTURE_B if publish else FIXTURE_A),
                     cancellationRequested=arm in ("ex-cancel-before", "ex-cancel-after"), expectedHash=digest(FIXTURE_B if arm == "ex-conflict" else FIXTURE_A),
                     saveCode="DOC-SAVE-UNCERTAIN" if publish else "DOC-CANCELLED" if arm == "ex-cancel-before" else "DOC-CONFLICT", stagedDacl=descriptor)
            if invoke:
                destination = ntpath.join(directory, "target.bin")
                length = len(destination.encode("utf-16-le"))
                e["exCall"] = dict(sourcePath=ntpath.join(directory, "staged.bin"), destinationPath=destination, sourceIdentity=staged,
                                   sourceHash=digest(FIXTURE_B), destinationBefore=file(old, FIXTURE_A), nameBytes=length, bufferLength=24+length,
                                   flags=2 if arm == "ex-no-replace" else 3, returned=publish, nativeError=None if publish else 183)
                e["exCall"]["class"] = 22
            if arm == "ex-foreign-cleanup":
                e.update(cleanupRefused=True, foreignHash=digest(FIXTURE_A))
        else:
            held = arm == "ancestor-list-held"
            e.update(parentIdentities=[root_id], sourcePath=directory, destinationPath=directory+"-moved", access=0 if arm == "ancestor-zero" else 129,
                     share=3, flags=0, sourceBefore=folder(), destinationBefore=absent(), heldIdentity=directory_id,
                     handleOpenAtCall=arm != "ancestor-list-released", moved=not held, sourceAfter=folder() if held else absent(),
                     destinationAfter=absent() if held else folder(), nativeError=32 if held else None)
        rows.append(dict(arm=arm, status="Observed", source="s", binary="b", evidence=e))
    return rows


def r54_self_test():
    rows = r54_fixture_rows()
    cases = []
    def negative(label, value):
        try:
            validate_discrimination(value, "s", "b")
        except ValueError as error:
            cases.append(dict(control=label, result="rejected", code=str(error)))
        else:
            raise AssertionError("R54-ACCEPTED-" + label)
    for value in (None, {}, "rows", [None] * 9):
        negative("outer-" + repr(value), value)
    # The six original mutations remain exact, now applied to realistic positives.
    for label, mutate in (
        ("missing-old-identities", lambda e: [e.pop(k) for k in ("oldIdentity", "oldIdentityAfter")]),
        ("old-identity-malformed", lambda e: e.update(oldIdentity="invented", oldIdentityAfter="invented")),
        ("ex-call-wrong-source", lambda e: e["exCall"].update(sourceIdentity="foreign")),
        ("ex-call-wrong-buffer", lambda e: e["exCall"].update(bufferLength=1))):
        changed = copy.deepcopy(rows)
        mutate(changed[0]["evidence"])
        negative(label, changed)
    changed = copy.deepcopy(rows)
    changed[7]["evidence"].pop("parentIdentities")
    negative("missing-parent-chain", changed)
    # Declared required fields, bounded to the newly owned schema; prior DACL internals retain their own controls.
    def paths(value, prefix=()):
        for key, item in value.items():
            if key == "tokenUserSid" or key == "stagedDacl":
                continue
            yield prefix + (key,), item
            if type(item) is dict:
                yield from paths(item, prefix + (key,))
    for index, row in enumerate(rows):
        for path, value in paths(row):
            wrong = [None, [], {}, True, 1.0, "malformed"]
            if type(value) is int:
                wrong += [-1, 2 ** 64, float(value), str(value)]
            if type(value) is bool:
                wrong += [not value, int(value)]
            for label, mutant in [("missing", None)] + [("wrong-" + str(i), v) for i, v in enumerate(wrong) if type(v) is not type(value) or v != value]:
                changed = copy.deepcopy(rows)
                parent = changed[index]
                for part in path[:-1]:
                    parent = parent[part]
                if label == "missing":
                    parent.pop(path[-1])
                else:
                    parent[path[-1]] = mutant
                negative(str(index) + ":" + ".".join(path) + ":" + label, changed)
    for index, field, value in (
        (0, "parentIdentities", []), (0, "parentIdentities", [None]), (0, "parentIdentities", ["12345678:0000000000000099"]),
        (0, "oldIdentityAfter", "12345678:0000000000000099"), (0, "oldHashAfter", digest(FIXTURE_B)),
        (2, "exCall", rows[0]["evidence"]["exCall"]), (3, "exCall", None),
        (0, "directory", "C:\\wrong\\ex-replace")):
        changed = copy.deepcopy(rows)
        changed[index]["evidence"][field] = value
        negative(f"relationship-{index}-{field}", changed)
    for field, value in (("sourceIdentity", "12345678:0000000000000099"), ("sourceHash", digest(FIXTURE_A)),
                         ("sourcePath", "C:\\owned\\ex-replace\\foreign.bin"),
                         ("destinationPath", "C:\\other\\ex-replace\\target.bin"),
                         ("nameBytes", 1), ("bufferLength", 1), ("nativeError", 5)):
        changed = copy.deepcopy(rows)
        changed[0]["evidence"]["exCall"][field] = value
        negative("call-relationship-" + field, changed)
    for collision in (80, 183):
        for denial in (5, 32):
            changed = copy.deepcopy(rows)
            changed[1]["evidence"]["exCall"]["nativeError"] = collision
            changed[7]["evidence"]["nativeError"] = denial
            validate_discrimination(changed, "s", "b")
            cases.append(dict(control=f"valid-errors-{collision}-{denial}", result="Pass"))
    unicode_rows = r54_fixture_rows("C:\\owned-é-🚀")
    validate_discrimination(unicode_rows, "s", "b")
    cases.append(dict(control="unicode-UTF16-declared-buffer", result="Pass", scope="synthetic ABI receipt; no native path admission"))
    wrong = copy.deepcopy(unicode_rows)
    call = wrong[0]["evidence"]["exCall"]
    call["nameBytes"] = len(call["destinationPath"]) * 2
    call["bufferLength"] = 24 + call["nameBytes"]
    negative("unicode-codepoints-not-UTF16", wrong)
    item = dict(path="C:\\owned\\denial\\target.bin", metadata=dict(identity="12345678:0000000000000001", attributes=32, links=1, size=0),
                denied=True, error=5, access=0xc0000000, creationHandleRetained=True, sha256=None, hashStatus="Not assessed: read access denied")
    denial_row = dict(case="denial", source="s", binary="b", status="Pass", evidence=dict(expectedDenial=item))
    expected_denial([denial_row], "s", "b")
    cases.append(dict(control="denial-valid", result="Pass"))
    def deny_negative(label, changed):
        try:
            expected_denial(changed, "s", "b")
        except ValueError as error:
            cases.append(dict(control="denial-" + label, result="rejected", code=str(error)))
        else:
            raise AssertionError("R54-DENIAL-ACCEPTED-" + label)
    changed = copy.deepcopy(denial_row)
    changed["evidence"]["expectedDenial"]["metadata"].update(links=True, size=False)
    deny_negative("bool-integer-metadata", [changed])
    for path, value in paths(denial_row):
        for label, mutant in [("missing", None), ("null", None), ("list", []), ("bool", True), ("float", float(value) if type(value) is int else 1.0)]:
            if label != "missing" and type(mutant) is type(value) and mutant == value:
                continue
            changed = copy.deepcopy(denial_row)
            parent = changed
            for part in path[:-1]:
                parent = parent[part]
            if label == "missing":
                parent.pop(path[-1])
            else:
                parent[path[-1]] = mutant
            deny_negative(".".join(path) + ":" + label, [changed])
    for malformed in (None, {}, [None], [{"case": "denial", "evidence": []}]):
        deny_negative("outer-" + repr(malformed), malformed)
    for case in cases:
        print(json.dumps(dict(case, suite="r54-schema", scope="synthetic receipt consumer")))


def r53_self_test():
    rows = r54_fixture_rows()
    validate_discrimination(rows, "s", "b")
    print(json.dumps(dict(control="r53-consumer-positive", result="Pass", scope="synthetic only")))
    mutations = [(0, k, v) for k, v in (("oldHandleOpen", False), ("oldHashAfter", digest(FIXTURE_B)),
        ("oldIdentityAfter", "new"), ("publication", False), ("durability", True), ("stagedHashAfter", digest(FIXTURE_A)),
        ("cleanup", "unknown"), ("containment", "unknown"), ("stagedDacl", {}), ("saveCode", "DOC-SAVED"))]
    mutations += [(1, "exCall", {"class": 22, "flags": 2, "returned": False, "nativeError": 5}),
                  (2, "callInvoked", True), (3, "callInvoked", True), (4, "cancellationRequested", False),
                  (5, "cleanupRefused", False), (5, "foreignHash", digest(FIXTURE_B)),
                  (6, "access", 129), (7, "access", 0), (7, "moved", True), (8, "handleOpenAtCall", True)]
    for index, key, value in mutations:
        changed = copy.deepcopy(rows)
        changed[index]["evidence"][key] = value
        try:
            validate_discrimination(changed, "s", "b")
        except ValueError:
            print(json.dumps(dict(control=f"r53-consumer-{index}-{key}", result="rejected")))
        else:
            raise AssertionError(key)
    # These invoke the same sequencing seam as execute, including real exceptions.
    for fault in (None, "preconditions", "probes", "mutation"):
        trace = []
        def phase(name):
            trace.append(name)
            if name == fault:
                raise ValueError("injected-" + name)
        try:
            ordered_phases(lambda: phase("preconditions"), lambda: phase("probes"), lambda: phase("mutation"))
        except ValueError as error:
            assert str(error) == "injected-" + fault
        expected = ["preconditions", "probes", "mutation"]
        assert trace == (expected if fault is None else expected[:expected.index(fault) + 1])
        print(json.dumps(dict(control="r53-order-" + str(fault), trace=trace, result="Pass")))
    rows = [dict(case=c, source="s", binary="b", evidence={"moved": True}) for c in CASES]
    assert not continuation_allowed(dict(quiescent=True), rows, "s", "b", dict(status="Pass"))
    print(json.dumps(dict(control="r53-moved-forged-inventory-pass", result="rejected")))
    with tempfile.TemporaryDirectory(prefix="cfd-r53-denial-") as scratch:
        root = Path(scratch)
        target = root / "denied.bin"
        target.write_bytes(b"")
        meta = dict(identity="00000001:0000000000000002", attributes=32, size=0, links=1)
        item = dict(path=str(target), metadata=meta, denied=True, error=5, access=0xc0000000,
                    creationHandleRetained=True, sha256=None, hashStatus="Not assessed: read access denied")
        row = dict(case="denial", source="s", binary="b", status="Pass", evidence=dict(expectedDenial=item))
        assert expected_denial([row], "s", "b") == item
        def denied(path):
            error = PermissionError(13, "injected exact Windows denial")
            error.winerror = 5
            raise error
        result = fixture_inventory(root, item, metadata=lambda p: meta, read=denied)
        assert result["denied.bin"]["sha256"] is None
        print(json.dumps(dict(control="r53-bound-denial", result="Pass", receipt=result)))
        for label, changed, observe, reader in (
            ("wrong-path", dict(item, path=str(root / "other")), lambda p: meta, denied),
            ("wrong-identity", item, lambda p: dict(meta, identity="00000001:0000000000000003"), denied),
            ("wrong-size", item, lambda p: dict(meta, size=1), denied),
            ("unexpected-denial", None, lambda p: meta, denied),
            ("readable", item, lambda p: meta, lambda p: b""),
            ("metadata-failure", item, lambda p: (_ for _ in ()).throw(OSError("injected metadata")), denied)):
            try:
                fixture_inventory(root, changed, metadata=observe, read=reader)
            except (ValueError, OSError):
                print(json.dumps(dict(control="r53-denial-" + label, result="rejected")))
            else:
                raise AssertionError(label)
        for key, value in (("creationHandleRetained", False), ("denied", False), ("error", 13), ("sha256", "invented")):
            changed = copy.deepcopy(row)
            changed["evidence"]["expectedDenial"][key] = value
            try:
                expected_denial([changed], "s", "b")
            except ValueError:
                print(json.dumps(dict(control="r53-denial-" + key, result="rejected")))
            else:
                raise AssertionError(key)
        target.unlink()
        try:
            fixture_inventory(root, item, metadata=lambda p: meta, read=denied)
        except ValueError:
            print(json.dumps(dict(control="r53-denial-missing", result="rejected")))
        else:
            raise AssertionError("missing denial")


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
    layout, layout_output = run("layout-controls", [str(executable), "--layout-controls"])
    layout_row = json.loads(layout_output)
    summary["layout_controls"] = layout_row
    if layout["exit"] != 0:
        raise ValueError("R53-LAYOUT-FAILED")
    validate_layout(layout_row, source, binary)
    summary["layout_mutations"] = []
    for key, value in (("flags", 1), ("infoClass", 3), ("offsets", [0, 4, 12, 16]), ("structSize", 20), ("bufferHex", "00"), ("binary", "wrong")):
        changed = dict(layout_row, **{key: value})
        try:
            validate_layout(changed, source, binary)
        except ValueError:
            summary["layout_mutations"].append(dict(control=key, result="rejected"))
        else:
            raise ValueError("R53-LAYOUT-MUTANT-ACCEPTED")
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
    def preconditions():
        verify_sources(ROOT, manifest)
        if any(digest((executable.parent / name).read_bytes()) != value for name, value in binary_files.items()):
            raise ValueError("R53-PRE-PROBE-BINARY-DRIFT")
        if (out / "fixture-a.bin").read_bytes() != FIXTURE_A or (out / "fixture-b.bin").read_bytes() != FIXTURE_B:
            raise ValueError("R53-PRE-PROBE-INPUT-DRIFT")

    def independent_probes():
        for probe in ("tree-timeout", "observer-fault", "uia-capability"):
            summary["probes"][probe] = dict(status="Not assessed", reason="non-Windows host" if not windows else "prior probe did not complete")
        if windows:
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
                summary["probes"]["uia-capability"] = dict(status="Not assessed", reason="PowerShell unavailable")
                return
            uia, uia_output = run("uia-capability", [powershell, "-NoProfile", "-NonInteractive", "-Command",
                 "[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); $ErrorActionPreference='Stop'; Add-Type -AssemblyName UIAutomationClient; "
                 "$r=[System.Windows.Automation.AutomationElement]::RootElement; "
                 "@{rootName=$r.Current.Name; childCount=$r.FindAll([System.Windows.Automation.TreeScope]::Children,"
                 "[System.Windows.Automation.Condition]::TrueCondition).Count; "
                 "userInteractive=[Environment]::UserInteractive; sessionId=(Get-Process -Id $PID).SessionId} | ConvertTo-Json -Compress"])
            if uia["exit"] != 0:
                summary["probes"]["uia-capability"] = dict(status="Not assessed", reason="UIA capability probe failed", exit=uia["exit"])
                return
            summary["probes"]["uia-capability"] = dict(status="Observed", evidence=json.loads(uia_output))

    def mutations():
        if windows:
            candidate, candidate_output = run("discrimination", [str(executable), "--discrimination", str(out / "discrimination-fixtures"), str(out / "fixture-a.bin"), str(out / "fixture-b.bin")])
            candidate_rows = [json.loads(line) for line in candidate_output.splitlines()]
            summary["discrimination"] = candidate_rows
            write_json(out / "discrimination.json", candidate_rows)
            if candidate["exit"] != 0 or len(candidate_rows) != 9 or any(r.get("status") in ("Unsafe", "Not assessed") for r in candidate_rows):
                raise ValueError("R53-DISCRIMINATION-SAFETY-REFUSED")
            try:
                validate_discrimination(candidate_rows, source, binary)
            except ValueError as error:
                summary["errors"].append(dict(stage="discrimination", code=str(error)))
            summary["discrimination_inventory"] = fixture_inventory(out / "discrimination-fixtures")
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
                inventory = fixture_inventory(out / "native-fixtures", expected_denial(rows, source, binary))
                containment = dict(status="Pass", scope="post-job disposable fixture inventory only", inventory=inventory)
            except (OSError, ValueError) as error:
                containment = dict(status="Fail", reason=str(error))
                summary["errors"].append(dict(stage="containment", code=str(error)))
        summary["containment"] = containment
        if (out / "fixture-a.bin").read_bytes() != FIXTURE_A or (out / "fixture-b.bin").read_bytes() != FIXTURE_B:
            raise ValueError("W0-INPUT-MUTATED")
        summary["post_mutation_continuation_allowed"] = windows and continuation_allowed(native, rows, source, binary, containment)

    summary["phase_trace"] = []
    ordered_phases(preconditions, independent_probes, mutations, summary["phase_trace"])


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
