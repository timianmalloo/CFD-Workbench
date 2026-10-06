#!/usr/bin/env python3
"""check-event-subscribers: every event declared in src/ has a subscriber in src/ (class TEST-UNWIRED-EVENT).

A test that proves behaviour by counting an event's invocations proves nothing when no production code listens to the
event (PG-26: the Properties pane's `Announced` was asserted by three tests and reached no assistive technology). This
check fails when a C# `event` declared under src/ has no `<name> +=` anywhere under src/; subscriptions in tests do not
count. An event that is subscribed outside src/ by design, or knowingly unwired until a named slice, is listed in
ALLOWED with its reason; an allowed event that gains a subscriber, or no longer exists, fails the check so the list
cannot go stale. Exit 0 clean · 1 a finding · 2 usage.
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

# "()" admits tuple types: without it `event Action<PointRef?, (double X0, double X1)?>? SectionShowRequested` was never
# seen, so an unwired event passed (found by M1.2c PNL).
DECLARATION = re.compile(r"\bevent\s+[\w<>,.?\[\]() ]+?\s+(\w+)\s*[;{=]")

ALLOWED = {
    "CanExecuteChanged": "ICommand member; the framework subscribes through command bindings, not src/",
    "VertexSelected": "SectionCanvas: the section editor's canvas is not wired to a commit path until M1.2c",
    "VertexMoved": "SectionCanvas: the section editor's canvas is not wired to a commit path until M1.2c",
    "LayersChanged": "WorkbenchController: raised by SetLayerVisible (HIST); the Layers pane (PNA) and the layer views (LAY) subscribe when those tracks merge; remove this entry then",
    "FocusedTargetChanged": "SectionCanvas and Viewport: focus-into-view is not wired in the shell until app-shell D4 (M1.2e)",
}


def main(argv):
    root = Path(argv[1]) if len(argv) > 1 else Path(__file__).resolve().parent.parent
    src = root / "src"
    if not src.is_dir():
        print("check-event-subscribers: no src/ under {0}".format(root), file=sys.stderr)
        return 2
    text = {path: path.read_text(encoding="utf-8") for path in sorted(src.rglob("*.cs"))}
    declared = {}
    for path, body in text.items():
        for number, line in enumerate(body.splitlines(), 1):
            for match in DECLARATION.finditer(line):
                declared.setdefault(match.group(1), "{0}:{1}".format(path.relative_to(root).as_posix(), number))
    corpus = "\n".join(text.values())
    subscribed = {name for name in declared if re.search(r"\b{0}\s*\+=".format(re.escape(name)), corpus)}
    findings = ["TEST-UNWIRED-EVENT  {0} ({1}) has no subscriber under src/".format(name, where)
                for name, where in sorted(declared.items()) if name not in subscribed and name not in ALLOWED]
    findings += ["TEST-UNWIRED-EVENT  allow-list entry {0} is stale: {1}".format(
                     name, "it now has a subscriber" if name in subscribed else "no such event under src/")
                 for name in sorted(ALLOWED) if name not in declared or name in subscribed]
    for line in findings:
        print(line)
    print("check-event-subscribers: {0} event(s), {1} allowed, {2} finding(s)".format(
        len(declared), len(ALLOWED), len(findings)))
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
