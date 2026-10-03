#!/usr/bin/env python3
"""Launcher record for one case, kept OUTSIDE the case (security review cycle 1, findings 3, 4, 5, 7, 11).

Run: python3 cases/tools/launcher-record.py <command> <case-dir>
  init       after generation: pin every file the generator wrote (tree digest) and the manifest digest. Refuses if a
             record exists already (a case is generated once).
  verify     before a launch: the case tree must equal the recorded tree exactly (no new, changed or missing file
             outside the product's own root-level outputs); no shared library or executable image anywhere; the case
             root holds only recorded entries and product outputs. Exit 3 on any difference.
  update     after a launch (or a product pipeline step): re-pin the tree, now including OpenFOAM's outputs.
  preflight-set / preflight-check
             the checkMesh pre-flight record ("Disallowing" seen under the bundle), keyed by case realpath + manifest
             digest; check exits 3 when absent.
Records live in <repo>/runs/.launcher/<sha256(realpath)[:20]>/. In the product this is application state; in the
spike the same OS user can still write it (residual risk, stated in the verdict).
"""
import hashlib
import json
import os
import re
import sys

cmd, case = sys.argv[1], os.path.realpath(sys.argv[2])
repo = os.path.realpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
rec_dir = os.path.join(repo, "runs", ".launcher", hashlib.sha256(case.encode()).hexdigest()[:20])
ROOT_OUTPUTS = re.compile(r"^(run-ledger\.txt|log\.[\w.+-]+|time\.[\w.+-]+|a4-monitor\.log|a4-stop\.txt|convergence\.txt|"
                          r"layer-coverage\.txt|yplus-area\.txt|generator\.txt|VOID\.txt)$")
IMAGE_MAGIC = (b"\xfe\xed\xfa\xce", b"\xfe\xed\xfa\xcf", b"\xce\xfa\xed\xfe", b"\xcf\xfa\xed\xfe", b"\xca\xfe\xba\xbe",
               b"\x7fELF")


def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def tree():
    out, problems = {}, []
    for root, dirs, files in os.walk(case):
        dirs.sort()
        for name in sorted(files):
            p = os.path.join(root, name)
            rel = os.path.relpath(p, case)
            if os.path.islink(p):
                problems.append(f"{rel}: symbolic link")
                continue
            with open(p, "rb") as f:
                head = f.read(4)
            if name.endswith((".dylib", ".so", ".bundle", ".dll")) or head.startswith(IMAGE_MAGIC):
                problems.append(f"{rel}: shared library or executable image")
            if root == case and ROOT_OUTPUTS.match(name):
                continue
            out[rel] = sha(p)
        for d in dirs:
            if os.path.islink(os.path.join(root, d)):
                problems.append(f"{os.path.relpath(os.path.join(root, d), case)}: symbolic link directory")
    return out, problems


def manifest_digest():
    p = os.path.join(case, "cfdw-manifest.json")
    return sha(p) if os.path.isfile(p) else None


def fail(lines):
    for line in lines:
        print(f"launcher-record: REFUSED {line}")
    sys.exit(3)


if cmd == "init":
    if os.path.exists(os.path.join(rec_dir, "tree.json")):
        fail([f"record exists for {case}: a case is generated once"])
    md = manifest_digest()
    if md is None:
        fail(["cfdw-manifest.json missing"])
    t, problems = tree()
    if problems:
        fail(problems)
    os.makedirs(rec_dir, exist_ok=True)
    json.dump({"case": case, "manifest_sha256": md, "tree": t}, open(os.path.join(rec_dir, "tree.json"), "w"), indent=0)
    print(f"launcher-record: init files={len(t)} manifest={md[:12]} record={os.path.relpath(rec_dir, repo)}")
elif cmd in ("verify", "update"):
    p = os.path.join(rec_dir, "tree.json")
    if not os.path.isfile(p):
        fail([f"no launcher record for {case} (generate the case with a product generator first)"])
    rec = json.load(open(p))
    t, problems = tree()
    if manifest_digest() != rec["manifest_sha256"]:
        problems.append("cfdw-manifest.json differs from the recorded digest")
    if cmd == "verify":
        old = rec["tree"]
        problems += [f"{k}: new file" for k in sorted(set(t) - set(old))]
        problems += [f"{k}: missing" for k in sorted(set(old) - set(t))]
        problems += [f"{k}: changed" for k in sorted(set(t) & set(old)) if t[k] != old[k]]
        if problems:
            fail(problems[:50] + ([f"... {len(problems) - 50} more"] if len(problems) > 50 else []))
        print(f"launcher-record: verified files={len(t)}")
    else:
        if problems:
            fail(problems)
        rec["tree"] = t
        json.dump(rec, open(p, "w"), indent=0)
        print(f"launcher-record: updated files={len(t)}")
elif cmd == "preflight-set":
    md = manifest_digest()
    open(os.path.join(rec_dir, "preflight"), "w").write(f"manifest_sha256={md} banner=Disallowing\n")
elif cmd == "preflight-check":
    p = os.path.join(rec_dir, "preflight")
    md = manifest_digest()
    if not (os.path.isfile(p) and open(p).read().strip() == f"manifest_sha256={md} banner=Disallowing"):
        fail(["no checkMesh pre-flight with the Disallowing banner for this case and manifest"])
else:
    sys.exit(f"unknown command {cmd}")
