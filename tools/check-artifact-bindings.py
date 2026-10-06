#!/usr/bin/env python3
"""DERIVED-UNBOUND control: every derived/register pattern in .agents/artifacts.yml is bound in .gitattributes.

A pattern classified `derived` needs `<pattern> merge=coord-regen`; `register` needs
`<pattern> merge=coord-register`. `coord doctor` checks that the drivers are registered, not that each
pattern is bound, so an unbound pattern conflicts on every join while the doctor reads healthy.
Exit 1 with one line per unbound or wrongly bound pattern. `--self-test` plants a missing line.
"""

import re
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
DRIVER = {"derived": "coord-regen", "register": "coord-register"}


def classified(artifacts_text):
    """pattern -> class, for the non-comment lines `pattern: class [command]`."""
    found = {}
    for line in artifacts_text.splitlines():
        match = re.match(r"^(\S+):\s+(\w+)\b", line)
        if match and not line.lstrip().startswith("#"):
            found[match.group(1)] = match.group(2)
    return found


def bound(attributes_text):
    """pattern -> merge driver, for the non-comment lines carrying `merge=<driver>`."""
    found = {}
    for line in attributes_text.splitlines():
        match = re.match(r"^(\S+)\s+.*\bmerge=(\S+)", line)
        if match and not line.lstrip().startswith("#"):
            found[match.group(1)] = match.group(2)
    return found


def problems(artifacts_text, attributes_text):
    attached = bound(attributes_text)
    found = []
    for pattern, kind in classified(artifacts_text).items():
        want = DRIVER.get(kind)
        if want is None:
            continue
        if pattern not in attached:
            found.append(pattern + " is " + kind + " but .gitattributes has no '" + pattern + " merge=" + want + "' line")
        elif attached[pattern] != want:
            found.append(pattern + " is " + kind + " but is bound to merge=" + attached[pattern] + ", not " + want)
    return found


def self_test():
    artifacts = "# c\na/x.js: derived python gen\nb/*.jsonl: register\nc.md: authored\n"
    good = "* text=auto\na/x.js merge=coord-regen\nb/*.jsonl merge=coord-register\n"
    assert problems(artifacts, good) == [], "green case must have no problems"
    missing = problems(artifacts, "a/x.js merge=coord-regen\n")
    assert len(missing) == 1 and missing[0].startswith("b/*.jsonl"), missing
    wrong = problems(artifacts, "a/x.js merge=coord-register\nb/*.jsonl merge=coord-register\n")
    assert len(wrong) == 1 and "not coord-regen" in wrong[0], wrong
    assert classified(artifacts) == {"a/x.js": "derived", "b/*.jsonl": "register", "c.md": "authored"}
    print("check-artifact-bindings self-test OK", flush=True)


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    artifacts = (ROOT / ".agents" / "artifacts.yml").read_text(encoding="utf-8")
    attributes = (ROOT / ".gitattributes").read_text(encoding="utf-8")
    found = problems(artifacts, attributes)
    for line in found:
        print("DERIVED-UNBOUND: " + line)
    if found:
        print("Run `coord install` to bind every classified pattern.", file=sys.stderr)
        return 1
    print("artifact bindings ok: " + str(len([k for k in classified(artifacts).values() if k in DRIVER]))
          + " derived/register patterns all bound in .gitattributes", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
