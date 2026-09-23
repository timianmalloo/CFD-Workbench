#!/usr/bin/env python3
"""Compile the hydrofoil-workbench knowledge-base roll-ups from its area files.

The area files (``docs/knowledge/hydrofoil-workbench/NN-slug.md``) are the record. The roll-ups
this script writes (state-of-the-art, comparables, references, data-and-constants, glossary,
open-questions, sources) are derived caches of those files, rebuilt on every run — never edited
by hand (domain-and-data-modelling DM7). ``index.md`` is authored, not generated.

Usage: python3 tools/compile-knowledge.py [--check]
  --check   exit 1 if any roll-up on disk differs from what would be generated (CI gate).
"""
from __future__ import annotations

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
KB = ROOT / "docs" / "knowledge" / "hydrofoil-workbench"
AREA_RE = re.compile(r"^\d{2}-[a-z0-9-]+\.md$")
TODAY = "2026-09-20"

ROLLUPS = {
    "state-of-the-art.md": ("kb-hw-state-of-the-art", "State of the art", ["State of the art"],
        "Current best practice, leading techniques and the research frontier for every area of hydrofoil modeling, analysis, simulation, optimization and workbench design, compiled from the area files."),
    "comparables.md": ("kb-hw-comparables", "Comparable solutions and problem framings", ["Comparables"],
        "How existing products, libraries and the literature frame and solve each part of the problem, with what each does well and badly and its licence, compiled from the area files."),
    "references.md": ("kb-hw-references", "Reference information", ["Reference information"],
        "Standards, specifications, primary documentation and seminal works per area, with what each requires of CFD-Workbench, compiled from the area files."),
    "data-and-constants.md": ("kb-hw-data-and-constants", "Domain data, constants, formulae and invariants", ["Data, constants, formulae and invariants"],
        "Formulae with units and validity envelopes, datasets, constants and invariants per area, compiled from the area files."),
    "open-questions.md": ("kb-hw-open-questions", "Open questions and domain failure modes", ["Open questions and domain failure modes", "Disconfirming views sought"],
        "What the research could not settle, the cheapest next probe for each, the silent and expensive failure modes of the domain, and the disconfirming views that were sought, compiled from the area files."),
}


def read_area(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    m = re.match(r"^---\n(.*?)\n---\n(.*)$", text, re.S)
    if not m:
        raise SystemExit(f"{path.name}: missing frontmatter")
    fm, body = m.group(1), m.group(2)
    ident = re.search(r"^id:\s*(\S+)", fm, re.M).group(1).strip('"')
    title = re.search(r"^title:\s*(.+)$", fm, re.M).group(1).strip().strip('"')
    sections: dict[str, str] = {}
    current = None
    buf: list[str] = []
    for line in body.splitlines():
        if line.startswith("## "):
            if current is not None:
                sections[current] = "\n".join(buf).strip()
            current = line[3:].strip()
            buf = []
        else:
            buf.append(line)
    if current is not None:
        sections[current] = "\n".join(buf).strip()
    return {"file": path.name, "id": ident, "title": title, "sections": sections}


def frontmatter(ident: str, title: str, summary: str, extra_tags: list[str]) -> str:
    tags = ", ".join(["hydrofoil", "knowledge", "generated"] + extra_tags)
    return (
        "---\n"
        f"id: {ident}\n"
        f"title: \"{title}\"\n"
        "type: knowledge\n"
        "status: draft\n"
        "owner: \"@timianmalloo\"\n"
        "phase: knowledge\n"
        f"tags: [{tags}]\n"
        "links:\n"
        "  - { to: kb-hydrofoil-workbench, rel: refines }\n"
        "review-by: 2026-12-19\n"
        "summary: >-\n"
        f"  {summary}\n"
        "---\n\n"
    )


GENERATED_NOTE = (
    "> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this "
    "directory on {date}. Edit the area file, then re-run the script; this file is a derived cache "
    "(DM7) and is checked for drift by `--check`.\n\n"
)


def shift_headings(text: str, by: int = 1) -> str:
    out = []
    for line in text.splitlines():
        m = re.match(r"^(#{1,5}) ", line)
        if m:
            line = "#" * min(6, len(m.group(1)) + by) + line[len(m.group(1)):]
        out.append(line)
    return "\n".join(out)


def rollup(areas: list[dict], name: str) -> str:
    ident, title, wanted, summary = ROLLUPS[name]
    parts = [frontmatter(ident, title, summary, [name.replace(".md", "")]), f"# {title}\n\n", GENERATED_NOTE.format(date=TODAY)]
    for a in areas:
        parts.append(f"## {a['title']}\n\n*Source: [{a['file']}]({a['file']}) (`{a['id']}`).*\n\n")
        found = False
        for w in wanted:
            sec = a["sections"].get(w)
            if sec:
                found = True
                if len(wanted) > 1:
                    parts.append(f"### {w}\n\n")
                parts.append(shift_headings(sec, 2 if len(wanted) > 1 else 1) + "\n\n")
        if not found:
            parts.append("*Section absent in the area file.*\n\n")
    return "".join(parts)


TERM_RE = re.compile(r"^- \*\*(.+?)\*\*\s*[—–-]\s*(.+)$")


def glossary(areas: list[dict]) -> str:
    entries: dict[str, list[tuple[str, str]]] = {}
    for a in areas:
        sec = a["sections"].get("Glossary terms", "")
        for line in sec.splitlines():
            m = TERM_RE.match(line.strip())
            if not m:
                continue
            term, definition = m.group(1).strip(), m.group(2).strip()
            entries.setdefault(term.lower(), []).append((term, definition + f" *— from `{a['id']}`*"))
    parts = [
        "---\nid: kb-hw-glossary\ntitle: \"Hydrofoil workbench glossary\"\ntype: glossary\nstatus: draft\n"
        "owner: \"@timianmalloo\"\nphase: knowledge\ntags: [glossary, hydrofoil, generated]\nlinks:\n"
        "  - { to: kb-hydrofoil-workbench, rel: refines }\n  - { to: spec-cfd-workbench-v1, rel: relates-to }\n"
        "review-by: 2026-12-19\nsummary: >-\n  The ubiquitous language of hydrofoil design, analysis, simulation and optimization as used by "
        "CFD-Workbench, merged alphabetically from every area file. A term defined by more than one area lists every definition so a "
        "conflict is visible rather than silently resolved.\n---\n\n",
        "# Hydrofoil workbench glossary\n\n",
        GENERATED_NOTE.format(date=TODAY),
        f"{len(entries)} terms. A term with more than one definition shows each, labeled by its source area; reconcile in the area files, not here.\n\n",
    ]
    for key in sorted(entries):
        defs = entries[key]
        term = defs[0][0]
        parts.append(f"## {term}\n\n")
        for _, d in defs:
            parts.append(f"- {d}\n")
        parts.append("\n")
    return "".join(parts)


ROW_RE = re.compile(r"^\|\s*(S\d+)\s*\|(.+)$")


def sources(areas: list[dict]) -> str:
    parts = [
        frontmatter("kb-hw-sources", "Sources", "Every source cited by the area files, with type, URL, access date and the claim it supports; ids are area-prefixed so a citation such as 02·S7 resolves to one row.", ["sources"]),
        "# Sources\n\n",
        GENERATED_NOTE.format(date=TODAY),
        "| Id | Title / source | Type | URL | Accessed | Used for |\n|---|---|---|---|---|---|\n",
    ]
    count = 0
    for a in areas:
        prefix = a["file"][:2]
        sec = a["sections"].get("Sources", "")
        for line in sec.splitlines():
            m = ROW_RE.match(line.strip())
            if not m:
                continue
            count += 1
            parts.append(f"| {prefix}·{m.group(1)} |{m.group(2)}\n")
    parts.append(f"\n{count} sources across {len(areas)} area files.\n")
    return "".join(parts)


def main() -> int:
    check = "--check" in sys.argv
    areas = [read_area(p) for p in sorted(KB.glob("*.md")) if AREA_RE.match(p.name)]
    if not areas:
        raise SystemExit("no area files found")
    outputs = {name: rollup(areas, name) for name in ROLLUPS}
    outputs["glossary.md"] = glossary(areas)
    outputs["sources.md"] = sources(areas)
    drift = []
    for name, content in outputs.items():
        target = KB / name
        if check:
            if not target.exists() or target.read_text(encoding="utf-8") != content:
                drift.append(name)
        else:
            target.write_text(content, encoding="utf-8", newline="\n")
    if check:
        if drift:
            print("knowledge roll-ups drift from their area files: " + ", ".join(drift))
            return 1
        print(f"knowledge roll-ups current ({len(areas)} area files)")
        return 0
    print(f"compiled {len(outputs)} roll-ups from {len(areas)} area files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
