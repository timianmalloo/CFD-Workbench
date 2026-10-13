#!/usr/bin/env python3
"""IMPORTLIB-UNREGISTERED-MODULE: require registration before dynamic module use.

Scan every tracked Python file, including managed runtime code. The only
exceptions are byte/line-pinned immutable historical proof loaders listed in
tools/importlib-historical-pins.json. New/changed/stale exceptions fail closed.
Unsupported creation/registration shapes refuse rather than claim proven flow.
Ring: fast/docs, self-test plus real scan; prints measured cost on every run.
"""
from __future__ import annotations

import ast
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess
import sys
import time

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
PINS = ROOT / "tools/importlib-historical-pins.json"
CONTROL = (ast.If, ast.For, ast.AsyncFor, ast.While, ast.Try, ast.TryStar,
           ast.With, ast.AsyncWith, ast.Match, ast.Return, ast.Raise, ast.Break, ast.Continue)
ACTIVE_PROOF = ("docs/proof/win-gpu-f1-preflight/recovery/",
                "docs/proof/win-gpu-f1-preflight/packet/",
                "docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/")


def qualified(node, bindings):
    if isinstance(node, ast.Name):
        return bindings.get(node.id, "")
    if isinstance(node, ast.Attribute):
        base = qualified(node.value, bindings)
        return base + "." + node.attr if base else ""
    return ""


def factory(node, bindings, factory_names):
    return isinstance(node, ast.Call) and (
        qualified(node.func, bindings) == "importlib.util.module_from_spec" or
        isinstance(node.func, ast.Attribute) and node.func.attr == "module_from_spec" or
        isinstance(node.func, ast.Name) and node.func.id in factory_names)


def same(left, right):
    return ast.dump(left, include_attributes=False) == ast.dump(right, include_attributes=False)


def registered_later(body, module, spec, bindings, origin):
    expected = [ast.Attribute(value=spec, attr="name", ctx=ast.Load()),
                ast.Attribute(value=ast.Name(id=module, ctx=ast.Load()), attr="__name__", ctx=ast.Load())]
    if origin is not None:
        expected.append(origin)
    dependent = ({node.id for node in ast.walk(spec) if isinstance(node, ast.Name)} |
                 {module} | {name for name, value in bindings.items() if value in ("sys", "sys.modules")})
    for node in body:
        if isinstance(node, ast.Assign) and isinstance(node.value, ast.Name) and node.value.id == module:
            if len(node.targets) == 1 and isinstance(node.targets[0], ast.Subscript):
                target = node.targets[0]
                if qualified(target.value, bindings) == "sys.modules" and any(same(target.slice, key) for key in expected):
                    return True
        if isinstance(node, CONTROL):
            return False
        if any(isinstance(item, ast.Name) and item.id in dependent and isinstance(item.ctx, ast.Load)
               for item in ast.walk(node)):
            return False
        if any(isinstance(item, ast.Name) and item.id in dependent and isinstance(item.ctx, ast.Store)
               for item in ast.walk(node)):
            return False
        if isinstance(node, (ast.Import, ast.ImportFrom)) and any(
                (alias.asname or alias.name.split(".")[0]) in dependent for alias in node.names):
            return False
    return False


def findings(text):
    tree = ast.parse(text)
    problems = []
    # Keep possible factory identities across conditional rebinding and scopes.
    # Ambiguous aliases refuse; a dropped binding must never hide a factory call.
    factory_names = {alias.asname or alias.name for node in ast.walk(tree)
                     if isinstance(node, ast.ImportFrom) and node.module == "importlib.util"
                     for alias in node.names if alias.name == "module_from_spec"}
    factory_names.add("module_from_spec")
    while True:
        previous = len(factory_names)
        for node in ast.walk(tree):
            if isinstance(node, ast.Assign) and (
                    isinstance(node.value, ast.Attribute) and node.value.attr == "module_from_spec" or
                    isinstance(node.value, ast.Name) and node.value.id in factory_names):
                factory_names.update(target.id for target in node.targets if isinstance(target, ast.Name))
        if len(factory_names) == previous:
            break

    def scan(body, inherited, inherited_specs):
        bindings, specs = dict(inherited), dict(inherited_specs)
        for position, node in enumerate(body):
            # Include definition headers, defaults, annotations, decorators and bases.
            # Child statement blocks receive their own registration flow below.
            expressions = []
            for field, value in ast.iter_fields(node):
                if field in ("body", "orelse", "finalbody", "handlers", "cases"):
                    continue
                values = value if isinstance(value, list) else [value]
                expressions.extend(item for item in values if isinstance(item, ast.AST))
            creations = [item for expression in expressions for item in ast.walk(expression)
                         if factory(item, bindings, factory_names)]
            for call in creations:
                assigned = isinstance(node, ast.Assign) and node.value is call and len(node.targets) == 1 and isinstance(node.targets[0], ast.Name)
                uncertain = isinstance(call.func, ast.Name) and qualified(call.func, bindings) != "importlib.util.module_from_spec"
                spec = call.args[0] if call.args else next((kw.value for kw in call.keywords if kw.arg == "spec"), None)
                origin = specs.get(spec.id) if isinstance(spec, ast.Name) else None
                if uncertain or not assigned or spec is None or not registered_later(
                        body[position + 1:], node.targets[0].id, spec, bindings, origin):
                    problems.append(call.lineno)
            if isinstance(node, ast.Import):
                for alias in node.names:
                    bindings[alias.asname or alias.name.split(".")[0]] = alias.name if alias.asname else alias.name.split(".")[0]
            elif isinstance(node, ast.ImportFrom):
                for alias in node.names:
                    bindings[alias.asname or alias.name] = (node.module or "") + "." + alias.name
            elif isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                local = dict(bindings)
                for argument in (*node.args.posonlyargs, *node.args.args, *node.args.kwonlyargs):
                    local.pop(argument.arg, None)
                for argument in (node.args.vararg, node.args.kwarg):
                    if argument:
                        local.pop(argument.arg, None)
                scan(node.body, local, specs)
                bindings.pop(node.name, None)
            elif isinstance(node, ast.ClassDef):
                scan(node.body, bindings, specs)
                bindings.pop(node.name, None)
            else:
                if isinstance(node, ast.Assign):
                    value_name = qualified(node.value, bindings)
                    for target in node.targets:
                        if isinstance(target, ast.Name):
                            if value_name:
                                bindings[target.id] = value_name
                            else:
                                bindings.pop(target.id, None)
                            if isinstance(node.value, ast.Call) and qualified(node.value.func, bindings).endswith(".spec_from_file_location") and node.value.args:
                                specs[target.id] = node.value.args[0]
                            else:
                                specs.pop(target.id, None)
                for field in ("body", "orelse", "finalbody"):
                    child = getattr(node, field, None)
                    if child:
                        scan(child, bindings, specs)
                for handler in getattr(node, "handlers", ()):
                    if handler.type:
                        problems.extend(item.lineno for item in ast.walk(handler.type)
                                        if factory(item, bindings, factory_names))
                    scan(handler.body, bindings, specs)
                for case in getattr(node, "cases", ()):
                    scan(case.body, bindings, specs)
                if isinstance(node, CONTROL):
                    # A conditional rebinding cannot establish an outer alias.
                    for child in ast.walk(node):
                        if isinstance(child, ast.Name) and isinstance(child.ctx, ast.Store):
                            bindings.pop(child.id, None)
                            specs.pop(child.id, None)
    scan(tree.body, {}, {})
    return sorted(set(problems))


def pin_problems(observed, pins):
    errors, admitted = [], set()
    for path, pin in sorted(pins.items()):
        if (not path.startswith("docs/proof/") or "\\" in path or
                ".." in PurePosixPath(path).parts or path.startswith(ACTIVE_PROOF)):
            errors.append("IMPORTLIB-PIN-FORBIDDEN: " + path)
        elif not isinstance(pin, dict) or set(pin) != {"sha256", "lines", "reason"} or not pin.get("reason"):
            errors.append("IMPORTLIB-PIN-INVALID: " + path)
        elif path not in observed:
            errors.append("IMPORTLIB-PIN-STALE: " + path)
        elif pin["sha256"] != observed[path]["sha256"] or pin["lines"] != observed[path]["lines"]:
            errors.append("IMPORTLIB-PIN-CHANGED: " + path)
        else:
            admitted.add(path)
    for path, row in sorted(observed.items()):
        if path not in admitted:
            errors.append(f"IMPORTLIB-UNREGISTERED-MODULE: {path}:{','.join(map(str, row['lines']))}")
    return errors, admitted


def self_test():
    prefix = "import importlib.util\nimport sys\nspec = importlib.util.spec_from_file_location('fixture', 'x.py')\n"
    create = "module = importlib.util.module_from_spec(spec)\n"
    register = "sys.modules[spec.name] = module\n"
    use = "spec.loader.exec_module(module)\n"
    cases = {
        "missing": (prefix + create + use, True),
        "ordered": (prefix + create + register + use, False),
        "late": (prefix + create + use + register, True),
        "wrong-module": (prefix + create + "sys.modules[spec.name] = other\n" + use, True),
        "wrong-key": (prefix + create + "sys.modules['other'] = module\n" + use, True),
        "conditional": (prefix + create + "if flag:\n    " + register + use, True),
        "nested-function-decoy": (prefix + create + "def decoy():\n    " + register + use, True),
        "reassigned-spec": (prefix + create + "spec = other\n" + register + use, True),
        "module-name": (prefix + create + "sys.modules[module.__name__] = module\n" + use, False),
        "literal-name": (prefix + create + "sys.modules['fixture'] = module\n" + use, False),
        "aliases": ("from importlib.util import module_from_spec as make\nfrom sys import modules as registry\n"
                    "module = make(spec)\nregistry[spec.name] = module\nspec.loader.exec_module(module)\n", False),
        "shadowed-sys": (prefix + "sys = other\n" + create + register + use, True),
        "inline-creation": (prefix + "spec.loader.exec_module(importlib.util.module_from_spec(spec))\n", True),
        "scoped": (prefix + "def load(spec):\n    " + create + "    " + register + "    return module\n", False),
        "comment-decoy": (prefix + create + "# " + register + use, True),
        "definition-default": (prefix + "def f(module=importlib.util.module_from_spec(spec)):\n    return module\n", True),
        "registry-rebound-after-creation": (prefix + create + "sys = other\n" + register + use, True),
        "conditional-factory-rebinding": ("from importlib.util import module_from_spec as make\nif flag:\n    make = other\nmodule = make(spec)\nspec.loader.exec_module(module)\n", True),
        "registry-alias-rebound": ("from importlib.util import module_from_spec as make\nfrom sys import modules as registry\nmodule = make(spec)\nregistry = other\nregistry[spec.name] = module\n", True),
        "definition-decorator": (prefix + "@decorate(importlib.util.module_from_spec(spec))\ndef f():\n    pass\n", True),
        "definition-base": (prefix + "class F(importlib.util.module_from_spec(spec)):\n    pass\n", True),
        "conditional-factory-introduced": (prefix + "if flag:\n    make = importlib.util.module_from_spec\nmodule = make(spec)\n" + register + use, True),
        "registry-attribute-rebound": (prefix + create + "sys.modules = other\n" + register + use, True),
        "exception-header": (prefix + "try:\n    pass\nexcept importlib.util.module_from_spec(spec):\n    pass\n", True),
    }
    for name, (source, expected) in cases.items():
        assert bool(findings(source)) == expected, name
    path = "docs/proof/historical/loader.py"
    observed = {path: {"sha256": "a" * 64, "lines": [1]}}
    pins = {path: {**observed[path], "reason": "immutable historical proof fixture"}}
    assert pin_problems(observed, pins) == ([], {path})
    assert any("STALE" in item for item in pin_problems({}, pins)[0])
    assert any("CHANGED" in item for item in pin_problems({path: {"sha256": "b" * 64, "lines": [1]}}, pins)[0])
    assert any("UNREGISTERED" in item for item in pin_problems({"docs/proof/new.py": observed[path]}, pins)[0])
    assert any("FORBIDDEN" in item for item in pin_problems({}, {"tools/loader.py": pins[path]})[0])
    assert any("FORBIDDEN" in item for item in pin_problems({}, {ACTIVE_PROOF[0] + "loader.py": pins[path]})[0])
    print(f"IMPORTLIB self-test PASS flow_cases={len(cases)} pin_cases=6")


def main():
    started = time.perf_counter()
    if sys.argv[1:] == ["--self-test"]:
        self_test()
        print(f"IMPORTLIB elapsed_seconds={time.perf_counter() - started:.3f} ring=fast/docs")
        return 0
    inventory = sys.argv[1:] == ["--inventory"]
    if sys.argv[1:] and not inventory:
        raise SystemExit("usage: check-importlib-registration.py [--self-test|--inventory]")
    paths = subprocess.run(["git", "ls-files", "-z", "--", "*.py"], cwd=ROOT,
                           check=True, capture_output=True).stdout.split(b"\0")
    pins = json.loads(PINS.read_text(encoding="utf-8")) if PINS.exists() else {}
    failures, observed = [], {}
    for raw in paths:
        if not raw:
            continue
        relative = raw.decode("utf-8")
        data = (ROOT / relative).read_bytes()
        try:
            lines = findings(data.decode("utf-8-sig"))
        except (SyntaxError, UnicodeError) as error:
            failures.append(f"IMPORTLIB-PARSE: {relative}: {type(error).__name__}")
            continue
        if lines:
            observed[relative] = {"sha256": hashlib.sha256(data).hexdigest(), "lines": lines}
    pin_errors, admitted = pin_problems(observed, pins)
    failures.extend(pin_errors)
    if inventory:
        print(json.dumps(observed, indent=2, sort_keys=True))
    else:
        for relative in sorted(admitted):
            print("IMPORTLIB historical-pin " + relative)
        print("\n".join(failures) if failures else "IMPORTLIB scan PASS")
        print(f"IMPORTLIB files={len(paths) - 1} failures={len(failures)} elapsed_seconds={time.perf_counter() - started:.3f} ring=fast/docs")
    return int(bool(failures))


if __name__ == "__main__":
    raise SystemExit(main())
