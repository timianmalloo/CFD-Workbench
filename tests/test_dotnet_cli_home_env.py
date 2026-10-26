"""A disposable dotnet home must not persist its tools path in the user's environment."""
import ast
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


def environment_writes(tree):
    """Return environment variable writes by mapping name; never execute a tool script."""
    writes = {}

    def record(name, key, value):
        if isinstance(name, ast.Name) and isinstance(key, str):
            writes.setdefault(name.id, {}).setdefault(key, []).append(value)

    def pairs(node):
        if isinstance(node, ast.Dict):
            return [(k.value, v) for k, v in zip(node.keys, node.values)
                    if isinstance(k, ast.Constant) and isinstance(k.value, str)]
        return []

    for node in ast.walk(tree):
        if isinstance(node, ast.Assign):
            for target in node.targets:
                if isinstance(target, ast.Subscript) and isinstance(target.slice, ast.Constant):
                    record(target.value, target.slice.value, node.value)
                if isinstance(target, ast.Name) and isinstance(node.value, ast.Call):
                    call = node.value
                    if isinstance(call.func, ast.Name) and call.func.id == "dict":
                        for kw in call.keywords:
                            record(target, kw.arg, kw.value)
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute) and node.func.attr == "update":
            for kw in node.keywords:
                record(node.func.value, kw.arg, kw.value)
            for arg in node.args:
                for key, value in pairs(arg):
                    record(node.func.value, key, value)
        if isinstance(node, ast.For) and isinstance(node.iter, ast.Call):
            iterator = node.iter.func
            if isinstance(iterator, ast.Attribute) and iterator.attr == "items":
                for assignment in ast.walk(node):
                    if isinstance(assignment, ast.Assign):
                        for target in assignment.targets:
                            if isinstance(target, ast.Subscript) and isinstance(target.slice, ast.Name):
                                for key, value in pairs(iterator.value):
                                    record(target.value, key, value)
    return writes


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

    def test_fresh_home_disables_global_tools_path_mutation(self):
        # Scope: tools/*.py, nonrecursive; all environment mappings setting the home;
        # no allowlist. Comments, receipts, and an opt-out on another mapping do not pass.
        for script in sorted((ROOT / "tools").glob("*.py")):
            tree = ast.parse(script.read_text(encoding="utf-8-sig"), filename=str(script))
            for mapping, variables in environment_writes(tree).items():
                if "DOTNET_CLI_HOME" in variables:
                    with self.subTest(script=script.name, environment=mapping):
                        values = variables.get("DOTNET_ADD_GLOBAL_TOOLS_TO_PATH", [])
                        self.assertTrue(values and all(isinstance(v, ast.Constant) and v.value == "false"
                                                       for v in values),
                                        f"{script.name}: {mapping} sets DOTNET_CLI_HOME without "
                                        'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH="false"')


if __name__ == "__main__":
    unittest.main()
