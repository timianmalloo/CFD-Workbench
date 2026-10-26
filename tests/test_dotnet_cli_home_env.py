"""A disposable dotnet home must not persist its tools path in the user's environment."""
import ast
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
HOME = "DOTNET_CLI_HOME"
FLAG = "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"


def environment_writes(tree):
    """Return environment variable writes by mapping name; never execute a tool script."""
    writes = {}
    parents = {child: parent for parent in ast.walk(tree) for child in ast.iter_child_nodes(parent)}
    handled = set()

    def record(name, key, value, token):
        if isinstance(name, (ast.Name, ast.Attribute, ast.Dict, ast.Call)) and isinstance(key, str):
            scope = name
            while scope in parents and not isinstance(scope, (ast.FunctionDef, ast.AsyncFunctionDef)):
                scope = parents[scope]
            owner = f"{getattr(scope, 'name', 'module')}:{getattr(scope, 'lineno', 0)}"
            identity = ast.unparse(name) if isinstance(name, (ast.Name, ast.Attribute)) else f"inline@{name.lineno}:{name.col_offset}"
            writes.setdefault(f"{owner}/{identity}", {}).setdefault(key, []).append(value)
            if key == HOME:
                handled.add(token)

    def pairs(node):
        if isinstance(node, ast.Dict):
            return [(k.value, v, k) for k, v in zip(node.keys, node.values)
                    if isinstance(k, ast.Constant) and isinstance(k.value, str)]
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and node.func.id == "dict":
            return [(kw.arg, kw.value, kw) for kw in node.keywords if kw.arg]
        return []

    for node in ast.walk(tree):
        if isinstance(node, ast.Assign):
            for target in node.targets:
                if isinstance(target, ast.Subscript) and isinstance(target.slice, ast.Constant):
                    record(target.value, target.slice.value, node.value, target.slice)
                if isinstance(target, (ast.Name, ast.Attribute)):
                    for key, value, token in pairs(node.value):
                        record(target, key, value, token)
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute) and node.func.attr == "update":
            for kw in node.keywords:
                record(node.func.value, kw.arg, kw.value, kw)
            for arg in node.args:
                for key, value, token in pairs(arg):
                    record(node.func.value, key, value, token)
        if isinstance(node, ast.Call):
            if (isinstance(node.func, ast.Attribute) and node.func.attr == "setdefault"
                    and node.args and isinstance(node.args[0], ast.Constant)):
                key = node.args[0].value
                # setdefault cannot prove the flag: an inherited true value wins.
                value = node.args[1] if key != FLAG and len(node.args) > 1 else node
                record(node.func.value, key, value, node.args[0])
            for kw in node.keywords:
                if kw.arg == "env":
                    for key, value, token in pairs(kw.value):
                        record(kw.value, key, value, token)
        if isinstance(node, ast.For) and isinstance(node.iter, ast.Call):
            iterator = node.iter.func
            if isinstance(iterator, ast.Attribute) and iterator.attr == "items":
                for assignment in ast.walk(node):
                    if isinstance(assignment, ast.Assign):
                        for target in assignment.targets:
                            if isinstance(target, ast.Subscript) and isinstance(target.slice, ast.Name):
                                for key, value, token in pairs(iterator.value):
                                    record(target.value, key, value, token)
    # Unknown home occurrences are not permission to skip the guard. Known reads in
    # receipts and key lists are harmless only when this file also proves a home mapping.
    for node in ast.walk(tree):
        if ((isinstance(node, ast.Constant) and node.value == HOME)
                or (isinstance(node, ast.keyword) and node.arg == HOME)) and node not in handled:
            parent = parents.get(node)
            is_read = (isinstance(parent, (ast.Tuple, ast.List))
                       or isinstance(parent, ast.Subscript) and isinstance(parent.ctx, ast.Load)
                       or isinstance(parent, ast.Call) and isinstance(parent.func, ast.Attribute)
                       and parent.func.attr == "get")
            if not is_read:
                writes[f"unproven@{node.lineno}:{node.col_offset}"] = {HOME: [node]}
    return writes


def guard_errors(source):
    writes = environment_writes(ast.parse(source))
    homes = {mapping: variables for mapping, variables in writes.items() if HOME in variables}
    if HOME in source and not homes:
        return ["text contains DOTNET_CLI_HOME without a proven environment mapping"]
    return [f"{mapping} sets DOTNET_CLI_HOME without a proven {FLAG}=false"
            for mapping, variables in homes.items()
            if not variables.get(FLAG) or not all(isinstance(v, ast.Constant) and v.value == "false"
                                                 for v in variables[FLAG])]


class DotnetCliHomeEnvironmentTests(unittest.TestCase):
    def test_review_forms_cannot_silently_escape_the_guard(self):
        fixtures = {
            "os.environ subscript": 'os.environ["DOTNET_CLI_HOME"] = "scratch"',
            "dict unpack": 'env = {**os.environ, "DOTNET_CLI_HOME": "scratch"}',
            "env keyword": 'run(env={"DOTNET_CLI_HOME": "scratch"})',
            "setdefault": 'env.setdefault("DOTNET_CLI_HOME", "scratch")',
        }
        for name, source in fixtures.items():
            with self.subTest(form=name):
                writes = environment_writes(ast.parse(source))
                self.assertTrue(any("DOTNET_CLI_HOME" in variables for variables in writes.values()),
                                f"{name}: fresh home silently escaped the AST guard")
                self.assertTrue(guard_errors(source), f"{name}: unsafe mapping passed")

    def test_unknown_and_mispaired_settings_fail_closed(self):
        for source in (
            '# DOTNET_CLI_HOME configured dynamically',
            'setattr(env, "DOTNET_CLI_HOME", "scratch")',
            'env["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "false"\nsetattr(env, "DOTNET_CLI_HOME", "scratch")',
            'env["DOTNET_CLI_HOME"] = "scratch"\nother["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "false"',
            'env["DOTNET_CLI_HOME"] = "scratch"\nenv.setdefault("DOTNET_ADD_GLOBAL_TOOLS_TO_PATH", "false")',
        ):
            with self.subTest(source=source):
                self.assertTrue(guard_errors(source))

    def test_review_forms_with_explicit_paired_flag_pass(self):
        for source in (
            'os.environ["DOTNET_CLI_HOME"] = "scratch"\nos.environ["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "false"',
            'env = {**os.environ, "DOTNET_CLI_HOME": "scratch", "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH": "false"}',
            'run(env={"DOTNET_CLI_HOME": "scratch", "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH": "false"})',
            'env.setdefault("DOTNET_CLI_HOME", "scratch")\nenv["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "false"',
        ):
            with self.subTest(source=source):
                self.assertEqual([], guard_errors(source))

    def test_fresh_home_disables_global_tools_path_mutation(self):
        # Scope: tools/*.py, nonrecursive; all environment mappings setting the home;
        # no allowlist. Comments, receipts, and an opt-out on another mapping do not pass.
        for script in sorted((ROOT / "tools").glob("*.py")):
            with self.subTest(script=script.name):
                self.assertEqual([], guard_errors(script.read_text(encoding="utf-8-sig")))


if __name__ == "__main__":
    unittest.main()
