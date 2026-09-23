#!/usr/bin/env python3
"""Independent contract cross-check. Requires blake3==1.0.8 and rfc8785==0.1.4.

Usage: python application-session-contract-vectors.py path/to/ApplicationContracts.dll
The C# session certificate authority is a fixture; this proves no geometry or native OS handle policy.
"""
import base64
from fractions import Fraction
import hashlib
import json
import os
from pathlib import Path
import random
import struct
import subprocess
import sys
import tempfile
import time

import blake3
import rfc8785

for stream in (sys.stdout, sys.stderr):
    if hasattr(stream, "reconfigure"):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def require(name, condition):
    if not condition:
        raise AssertionError(name)
    checks.append(name)


def bits(value):
    return struct.pack(">d", value).hex()


def exact(token, scale):
    # Fraction parses the decimal independently of the C# BigInteger converter.
    # Zero with a hostile exponent is special-cased before Python's own power allocation.
    significand = token.lower().split("e")[0]
    if not any(c in "123456789" for c in significand):
        return 0.0
    result = float(Fraction(token) * Fraction(10) ** scale)
    return 0.0 if result == 0 else result


class PersistenceModel:
    """State machine ONLY. These maps are not evidence of OS atomicity or handles."""
    def __init__(self):
        self.files = {}
        self.claimed = False

    def save(self, content, expected, *, capable=True, fault=None, competing=None):
        if not capable:
            return "DOC-UNSUPPORTED-PERSISTENCE"
        if self.claimed:
            return "DOC-WRITER-CLAIM"
        self.claimed = True
        try:
            if fault in ("before-write", "partial-write", "before-flush", "before-publish"):
                return "DOC-IO"
            if competing is not None:
                self.files["target"] = competing
            if self.files.get("target") != expected:
                return "DOC-CONFLICT"
            self.files["target"] = content
            if fault == "after-publish":
                return "DOC-SAVE-UNCERTAIN"
            return "OK"
        finally:
            self.claimed = False


def persistence_vectors():
    for platform in ("macOS-contract", "Windows-contract"):
        for fault in ("before-write", "partial-write", "before-flush", "before-publish", "after-publish"):
            model = PersistenceModel()
            model.files["target"] = b"old"
            code = model.save(b"new-complete", b"old", fault=fault)
            require(f"{platform}_{fault}_OldOrCompleteNew_Model", model.files["target"] == (b"new-complete" if fault == "after-publish" else b"old") and code != "OK" and not model.claimed)
        model = PersistenceModel()
        require(f"{platform}_CompetingCreator_NoOverwrite_Model", model.save(b"new", None, competing=b"theirs") == "DOC-CONFLICT" and model.files["target"] == b"theirs")
        require(f"{platform}_MissingPrimitive_NoFallback_Model", model.save(b"new", b"theirs", capable=False) == "DOC-UNSUPPORTED-PERSISTENCE" and model.files["target"] == b"theirs")
    # Actual local same-directory hard-link publication provides no-replace for this primitive.
    # It is NOT a production handle-relative path validator or a power-loss durability oracle.
    with tempfile.TemporaryDirectory(prefix="cfd-contract-") as name:
        directory = Path(name)
        temporary, target = directory / "owned-temp", directory / "target"
        temporary.write_bytes(b"new-complete")
        target.write_bytes(b"competing-creator")
        try:
            os.link(temporary, target)
        except FileExistsError:
            pass
        else:
            raise AssertionError("NoReplace_CompetingCreator_UnexpectedSuccess")
        require("LocalFilesystem_NoReplace_CreatorPreserved", target.read_bytes() == b"competing-creator" and temporary.read_bytes() == b"new-complete")
        target.unlink()
        os.link(temporary, target)
        temporary.unlink()
        require("LocalFilesystem_NoReplace_CompleteNewFile", target.read_bytes() == b"new-complete")


def native_replay(envelope):
    accepted = {a["id"]: a for a in envelope["accepted"]}
    current, redo, operations = None, [], set()
    for index, cursor in enumerate(envelope["cursors"]):
        assert cursor["sequence"] == index
        assert cursor["operationId"] not in operations
        operations.add(cursor["operationId"])
        target = accepted[cursor["target"]]
        reason = cursor["reason"]
        if reason in ("open", "apply"):
            assert target["parent"] == current and cursor["operationId"] == target["operationId"]
            redo.clear()
        elif reason == "undo":
            assert accepted[current]["parent"] == target["id"]
            redo.append(current)
        elif reason == "redo":
            assert redo.pop() == target["id"] and target["parent"] == current
        else:
            raise AssertionError("unknown cursor reason")
        current = target["id"]
    return current, redo


def main():
    dll = str(Path(sys.argv[1]).resolve())
    started = time.perf_counter()
    run = subprocess.run(["dotnet", dll], text=True, encoding="utf-8", capture_output=True, check=True, timeout=30)
    report = json.loads(run.stdout)
    require("CSharp_DeclaredChecks_AllExecuted", len(report["checks"]) >= 60 and len(set(report["checks"])) == len(report["checks"]))
    for row in report["numbers"]:
        require("Decimal_IndependentFraction_" + str(len(checks)), bits(exact(row["token"], row["scale"])) == row["bits"])
    canonical = report["canonical"].encode("utf-8")
    require("Jcs_IndependentLibrary_ExactBytes", rfc8785.dumps(json.loads(canonical)) == canonical)
    require("Blake3_IndependentBinding_ExactDigest", blake3.blake3(canonical).hexdigest() == report["canonicalBlake3"])
    example = report["exampleCanonical"].encode("utf-8")
    require("Example_Jcs_ExactBytes", rfc8785.dumps(json.loads(example)) == example)
    require("Example_Blake3_ExactDigest", blake3.blake3(example).hexdigest() == report["exampleSurfaceHash"])
    source = base64.b64decode(report["exampleSourceBase64"], validate=True)
    require("Example_Source_BomCrLfRetained", source.startswith(b"\xef\xbb\xbf") and b"\r\n" in source)
    envelope_bytes = base64.b64decode(report["nativeBase64"], validate=True)
    envelope = json.loads(envelope_bytes)
    for row in envelope["sources"]:
        original = b"".join(base64.b64decode(c, validate=True) for c in row["utf8Base64Chunks"])
        require("Native_SourceDigest_" + row["id"][:12], hashlib.sha256(original).hexdigest() == row["id"])
    current, redo = native_replay(envelope)
    require("Native_IndependentCursorReplay_BranchClearsRedo", current == envelope["accepted"][-1]["id"] and redo == [] and len(envelope["accepted"]) == 3)
    rng = random.Random(42019)
    requests, expected = [], []
    # Deterministic raw-bit corpus across normal/subnormal/exponent boundaries.
    bit_values = [0, 1, 0x8000000000000000, 0x7fefffffffffffff, 0x0010000000000000, 0x4340000000000000]
    bit_values += [rng.getrandbits(64) for _ in range(2000)]
    for b in bit_values:
        d = struct.unpack(">d", b.to_bytes(8, "big"))[0]
        if (b >> 52) & 2047 == 2047:
            continue
        requests.append(["bits", f"{b:016x}"])
        expected.append(rfc8785.dumps(d).decode())
    for _ in range(500):
        token = f"{rng.randrange(-10**18, 10**18)}e{rng.randrange(-330, 290)}"
        scale = rng.choice((0, -2, -3))
        requests.append(["decimal", token, str(scale)])
        expected.append(rfc8785.dumps(exact(token, scale)).decode())
    batch = subprocess.run(["dotnet", dll, "--batch"], input="\n".join(json.dumps(r) for r in requests) + "\n", text=True, encoding="utf-8", capture_output=True, check=True, timeout=30)
    outputs = [json.loads(line) for line in batch.stdout.splitlines()]
    require("Batch_CompleteResponseCount", len(outputs) == len(expected))
    for i, (actual, canonical_expected) in enumerate(zip(outputs, expected)):
        if actual.get("canonical") != canonical_expected:
            raise AssertionError(f"CrossRuntime_Case_{i}: {requests[i]!r} -> {actual!r}, expected {canonical_expected!r}")
    require("Jcs_DeterministicBitCorpus_IndependentAgreement", True)
    require("Decimal_DeterministicRationalCorpus_IndependentAgreement", True)
    for token in ("1e1000000000", "1e-1000000000"):
        probe = subprocess.run(["dotnet", dll, "--batch"], input=json.dumps(["decimal", token, "0"]) + "\n", text=True, encoding="utf-8", capture_output=True, check=True, timeout=5)
        require("Resource_HostileExponent_PromptRefusal_" + token, json.loads(probe.stdout)["error"] == "DSL-LIMIT")
    persistence_vectors()
    receipt = {"scope": "independent-contract-oracle", "runtime": report["runtime"], "csharpCheckCount": len(report["checks"]), "csharpChecks": report["checks"], "pythonCheckCount": len(checks), "pythonChecks": checks, "batchVectorCount": len(requests), "elapsedSeconds": time.perf_counter() - started, "exampleSurfaceHash": report["exampleSurfaceHash"], "nativeByteCount": len(envelope_bytes), "nativeLiveWindows": "Not assessed", "nativeHandlePolicy": "Not assessed", "geometryCertificate": "fixture authority only"}
    print(json.dumps(receipt, indent=2))


checks = []
if __name__ == "__main__":
    main()
