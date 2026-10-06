#!/usr/bin/env python3
"""Messages between the Mac and the Windows PC sessions, carried by git (Ruling 106).

`.agents/mail/` and `.agents/requests.jsonl` are git-ignored, so nothing the coord tools write reaches
the other machine. This register does: `docs/coordination/xmsg.jsonl`, one JSON object per line,
union-merged by the coord-register driver. A session posts, commits, pushes; the other pulls and reads
`unread`. Which messages this machine has already seen is local state in `.agents/xmsg-seen.json`.

  post   --to mac|pc|both --kind KIND --text TEXT [--ref REF]
  unread [--mark]           messages to this machine (or both) not yet seen; --mark records them seen
  list   [--last N]
  --me mac|pc overrides the machine, which otherwise comes from CFD_MACHINE or the OS (win32 -> pc).
  --self-test              red if a message is lost, shown twice, shown to the wrong machine, or printed without its ref.
"""

import argparse
import contextlib
import io
import json
import os
import sys
import tempfile
import time
from datetime import datetime, timezone
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
MACHINES = ("mac", "pc")
KINDS = ("handoff", "pr-ready", "review", "decision-request", "ruling", "blocked", "done", "note")


def machine(override=None):
    me = override or os.environ.get("CFD_MACHINE") or ("pc" if sys.platform == "win32" else "mac")
    if me not in MACHINES:
        raise SystemExit("xmsg: machine must be one of " + ", ".join(MACHINES) + ", not " + repr(me))
    return me


def read(register):
    if not register.exists():
        return []
    rows = []
    with register.open(encoding="utf-8") as handle:
        for number, line in enumerate(handle, 1):
            if line.strip():
                try:
                    rows.append(json.loads(line))
                except json.JSONDecodeError as exc:
                    raise SystemExit("xmsg: {}:{} is not JSON ({})".format(register, number, exc))
    return sorted(rows, key=lambda row: row["id"])


def post(register, me, to, kind, text, ref=None):
    stamp = datetime.now(timezone.utc)
    row = {"id": "{}-{}-{:09d}".format(stamp.strftime("%Y%m%dT%H%M%S"), me, time.perf_counter_ns() % 10**9),
           "at": stamp.strftime("%Y-%m-%dT%H:%M:%SZ"), "from": me, "to": to, "kind": kind, "text": text}
    if ref:
        row["ref"] = ref
    register.parent.mkdir(parents=True, exist_ok=True)
    with register.open("a", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(row, ensure_ascii=False) + "\n")
    return row


def load_seen(seen_file):
    try:
        return set(json.loads(seen_file.read_text(encoding="utf-8")))
    except (OSError, ValueError):
        return set()


def unread(register, seen_file, me):
    seen = load_seen(seen_file)
    return [row for row in read(register) if row["to"] in (me, "both") and row["from"] != me and row["id"] not in seen]


def mark(seen_file, rows):
    seen = load_seen(seen_file) | {row["id"] for row in rows}
    seen_file.parent.mkdir(parents=True, exist_ok=True)
    seen_file.write_text(json.dumps(sorted(seen)), encoding="utf-8", newline="\n")


def show(rows):
    for row in rows:
        ref = " [" + row["ref"] + "]" if row.get("ref") else ""
        print("{} {}->{} {}{}: {}".format(row["at"], row["from"], row["to"], row["kind"], ref, row["text"]))


def self_test():
    with tempfile.TemporaryDirectory() as scratch:
        register, seen = Path(scratch, "xmsg.jsonl"), Path(scratch, "seen.json")
        post(register, "mac", "pc", "handoff", "one")
        post(register, "pc", "mac", "pr-ready", "two", "win/smoke")
        post(register, "mac", "both", "note", "three")
        pc_rows = unread(register, seen, "pc")
        failures = []
        if [row["text"] for row in pc_rows] != ["one", "three"]:
            failures.append("pc saw {}".format([row["text"] for row in pc_rows]))
        mark(seen, pc_rows)
        if unread(register, seen, "pc"):
            failures.append("a marked message was shown again")
        if [row["text"] for row in unread(register, Path(scratch, "mac-seen.json"), "mac")] != ["two"]:
            failures.append("mac did not see exactly the message addressed to it (its own post excluded)")
        shown = io.StringIO()
        with contextlib.redirect_stdout(shown):
            show(read(register))
        if "[win/smoke]" not in shown.getvalue():
            failures.append("show() did not print a message's ref")
        if failures:
            print("xmsg self-test FAILED: " + "; ".join(failures))
            return 1
    print("xmsg self-test: ok (routing, seen-marking, no loss)")
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--me")
    parser.add_argument("--register", default=str(ROOT / "docs" / "coordination" / "xmsg.jsonl"))
    parser.add_argument("--self-test", action="store_true")
    commands = parser.add_subparsers(dest="command")
    posting = commands.add_parser("post")
    posting.add_argument("--to", required=True, choices=MACHINES + ("both",))
    posting.add_argument("--kind", required=True, choices=KINDS)
    posting.add_argument("--text", required=True)
    posting.add_argument("--ref")
    reading = commands.add_parser("unread")
    reading.add_argument("--mark", action="store_true")
    listing = commands.add_parser("list")
    listing.add_argument("--last", type=int, default=20)
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    register, seen_file = Path(args.register), ROOT / ".agents" / "xmsg-seen.json"
    me = machine(args.me)
    if args.command == "post":
        show([post(register, me, args.to, args.kind, args.text, args.ref)])
        print("now commit {} and push".format(register.relative_to(ROOT).as_posix()))
    elif args.command == "unread":
        rows = unread(register, seen_file, me)
        show(rows)
        print("{} unread for {}".format(len(rows), me))
        if args.mark:
            mark(seen_file, rows)
    elif args.command == "list":
        show(read(register)[-args.last:])
    else:
        parser.print_help()
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
