#!/usr/bin/env python3
"""Git merge driver for docs/lessons/defect-classes.md (defect class JOIN-LOG-CONFLICT).

Usage as a driver: merge-defect-register.py %O %A %B %P  (writes the result to %A, exit 0; exit 1 = conflict, %A holds a three-way merge with markers)
Self-test:         merge-defect-register.py --self-test

Resolves whole-entry additions and strict extensions of an existing entry. Every other case defers to `git merge-file -p`:
a clean merge (two dated lines added inside one entry) is written and exits 0; conflict hunks are written with markers
and exit 1; if git cannot run, the output is a whole-file ours/theirs conflict and exit 1. Git does NOT write markers itself: after a non-zero driver exit
it keeps whatever %A holds (ours) and marks the path conflicted, so an untouched %A looks resolved and drops theirs. An entry starts at a line beginning `**<CLASS-ID> · ` and runs to the next such line.
"""
import re
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

HEADER = re.compile(r"^\*\*(.+?) · ")


def parse(text):
    """Return (preamble, ordered list of (key, (body, gap))). key = (header prefix, occurrence): a prefix can repeat.
    gap = newlines that followed the body in its source, capped at 2 (one blank line); 2 for the last."""
    lines = text.split("\n")
    starts = [i for i, line in enumerate(lines) if HEADER.match(line)]
    if not starts:
        return (text.rstrip("\n"), 2), []

    def split(first, end, last):
        raw = "\n".join(lines[first:end])
        body = raw.rstrip("\n")
        return body, 2 if last else min(2, 1 + len(raw) - len(body))  # the register has no doubled blank line

    preamble = split(0, starts[0], False)
    seen, entries = {}, []
    for n, s in enumerate(starts):
        last = n + 1 == len(starts)
        prefix = HEADER.match(lines[s]).group(1)
        seen[prefix] = seen.get(prefix, 0) + 1
        entries.append(((prefix, seen[prefix]), split(s, len(lines) if last else starts[n + 1], last)))
    return preamble, entries


def merge(base, ours, theirs):
    """Return the merged text, or None when git must report a conflict."""
    pb, eb = parse(base)
    po, eo = parse(ours)
    pt, et = parse(theirs)
    if po[0] == pt[0] or pt[0] == pb[0]:
        preamble = po
    elif po[0] == pb[0]:
        preamble = pt
    else:
        return None
    mb, mo, mt = dict(eb), dict(eo), dict(et)
    base_keys = [k for k, _ in eb]
    # A deletion or reorder of a base entry on either side is not ours to resolve.
    for side in (eo, et):
        if [k for k, _ in side if k in mb] != base_keys:
            return None
    out = []
    for key in base_keys:
        b, o, t = mb[key], mo[key], mt[key]
        if o[0] == t[0] or t[0] == b[0]:
            out.append(o)
        elif o[0] == b[0]:
            out.append(t)
        elif t[0].startswith(o[0]):
            out.append(t)
        elif o[0].startswith(t[0]):
            out.append(o)
        else:
            return None
    out.extend(v for k, v in eo if k not in mb)
    for key, value in et:
        if key in mb:
            continue
        if key in mo:
            if mo[key][0] != value[0]:
                return None
            continue
        out.append(value)
    blocks = ([preamble] if preamble[0] else []) + out
    result = "".join(body + "\n" * gap for body, gap in blocks[:-1]) + blocks[-1][0] + "\n"
    # Conservation: every non-blank input line is an output line, or the head of one (a strict extension of its last line).
    have = [line for line in result.split("\n") if line.strip()]
    kept = set(have)
    for line in (ours + "\n" + theirs).split("\n"):
        if line.strip() and line not in kept and not any(h.startswith(line) for h in have):
            return None
    return result


def run_driver(base_path, ours_path, theirs_path):
    def read(path):
        return Path(path).read_text(encoding="utf-8")

    base, ours, theirs = read(base_path), read(ours_path), read(theirs_path)
    result = merge(base, ours, theirs)
    code = 0
    if result is None:
        result, code = three_way(base_path, ours_path, theirs_path, base, ours, theirs)
    Path(ours_path).write_text(result, encoding="utf-8", newline="\n")
    return code


def kept_every_line(result, base, ours, theirs):
    """Conservation: every non-blank line that ours or theirs added or changed (not in base) is in the output.
    A base line one side removed may go: that is git's three-way decision, and it is how a rewrap merges."""
    have, old = set(result.split("\n")), set(base.split("\n"))
    return all(line in have for line in (ours + "\n" + theirs).split("\n") if line.strip() and line not in old)


def three_way(base_path, ours_path, theirs_path, base, ours, theirs):
    """Return (text, exit code) from git merge-file -p: a clean merge that keeps every line -> (text, 0); real hunks ->
    (text with markers, 1); git cannot run, errors, or a clean merge that drops a line -> a whole-file conflict, 1."""
    try:
        proc = subprocess.run(
            ["git", "merge-file", "-p", "-L", "ours", "-L", "base", "-L", "theirs", ours_path, base_path, theirs_path],
            capture_output=True, check=False)
        text = proc.stdout.decode("utf-8")
        if proc.returncode == 0 and kept_every_line(text, base, ours, theirs):
            return text, 0
        if 0 < proc.returncode < 128 and "<<<<<<<" in text:
            return text, 1
    except (OSError, UnicodeDecodeError):
        pass
    return "<<<<<<< ours\n%s\n=======\n%s\n>>>>>>> theirs\n" % (ours.rstrip("\n"), theirs.rstrip("\n")), 1


def entry(name, text):
    return "**%s · Title.** %s" % (name, text)


def self_test():
    pre = "# Register\n\nIntro."
    a, x, y, z = entry("A", "a."), entry("X", "x."), entry("Y", "y."), entry("Z", "z.")

    def doc(*blocks, preamble=pre):
        return preamble + "\n\n" + "\n\n".join(blocks) + "\n"

    base = doc(a, x)
    x1, x2 = x, "second line of x."
    mid = doc(a, x1 + "\n" + x2)
    ext = x + "\n*2026-10-08 extended.*"
    cases = [
        ("both append different entries", base, doc(a, x, y), doc(a, x, z), doc(a, x, y, z)),
        ("one extends X, other appends Y", base, doc(a, ext), doc(a, x, y), doc(a, ext, y)),
        ("both edit X differently", base, doc(a, x + " one."), doc(a, x + " two."), None),
        ("both add a different dated line inside X (DPR shape)", mid, doc(a, x1 + "\n*2026-10-08 one.*\n" + x2), doc(a, x1 + "\n" + x2 + "\n*2026-10-09 two.*"), "git"),
        ("one deletes X", base, doc(a), doc(a, x, y), None),
        ("identical new entry on both", base, doc(a, x, y), doc(a, x, y), doc(a, x, y)),
        ("preamble edited on both", base, doc(a, x, preamble=pre + " One."), doc(a, x, preamble=pre + " Two."), None),
        ("reordered entries", base, doc(x, a), doc(a, x, y), None),
    ]
    fm_pre = "---\nid: r\nlinks:\n  - { to: base-a, rel: relates-to }\n---\n# Register"
    fm_base = doc(a, x, preamble=fm_pre)
    fm_ours = doc(a, x, preamble=fm_pre.replace("relates-to }\n", "relates-to }\n  - { to: ours-b, rel: relates-to }\n"))
    fm_theirs = doc(a, x, y, preamble=fm_pre.replace("relates-to }\n", "relates-to }\n  - { to: theirs-c, rel: relates-to }\n"))
    cases.append(("both add different frontmatter links (PR #17 shape)", fm_base, fm_ours, fm_theirs, None))
    failures = 0
    for name, b, o, t, want in cases:
        ok = merge(b, o, t) == (None if want == "git" else want)
        if want == "git":
            ok = ok and git_resolves(b, o, t)
        elif want is None:
            ok = ok and conflict_leaves_markers(b, o, t)
        failures += not ok
        print("%s: %s" % ("ok  " if ok else "FAIL", name))
    return 1 if failures else 0


def git_resolves(b, o, t):
    """The driver must exit 0 and keep every non-blank line of both sides."""
    with tempfile.TemporaryDirectory() as tmp:
        paths = [Path(tmp) / n for n in ("base", "ours", "theirs")]
        for path, text in zip(paths, (b, o, t)):
            path.write_text(text, encoding="utf-8", newline="\n")
        code = run_driver(*[str(p) for p in paths])
        left = paths[1].read_text(encoding="utf-8")
    return code == 0 and "<<<<<<<" not in left and kept_every_line(left, b, o, t)


def conflict_leaves_markers(b, o, t):
    """Run the driver on files: exit must be 1 and %A must hold markers plus every line theirs changed or added."""
    with tempfile.TemporaryDirectory() as tmp:
        paths = [Path(tmp) / n for n in ("base", "ours", "theirs")]
        for path, text in zip(paths, (b, o, t)):
            path.write_text(text, encoding="utf-8", newline="\n")
        code = run_driver(*[str(p) for p in paths])
        left = paths[1].read_text(encoding="utf-8")
    wanted = [line for line in t.split("\n") if line.strip() and line not in b.split("\n")]
    return code == 1 and "<<<<<<<" in left and ">>>>>>>" in left and all(line in left for line in wanted)


def main(argv):
    if argv[1:] == ["--self-test"]:
        return self_test()
    if len(argv) < 4:
        print(__doc__, file=sys.stderr)
        return 2
    return run_driver(argv[1], argv[2], argv[3])


if __name__ == "__main__":
    sys.exit(main(sys.argv))
