#!/usr/bin/env python3
"""Ruling 182 runner guard. Ring: fast policy; Windows qualification is preparation-only.

--self-test proves planted policy failures and, on Windows, exercises the real
PowerShell runner with stub children (60 s ceiling, no scale or product checks).
Scope: tools/windows-runner.ps1 and windows-settings-preflight.ps1, no recursion.
UIA has a read-method allowlist; no policy token allowlist on the runner.
Portable policy uses lexical controls. Windows adds parsed UIA AST and native
runtime qualification. Policy and runtime costs print separately as measured ms.
"""
from __future__ import annotations

import argparse
from pathlib import Path
import os
import re
import subprocess
import sys
import hashlib
import json
import socket
import tempfile
from functools import lru_cache
import time
import importlib.util

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "tools/windows-runner.ps1"
PREFLIGHT = ROOT / "tools/windows-settings-preflight.ps1"
UIA_READ_METHODS = {"FindAll", "FindFirst", "GetCurrentPattern", "GetSelection"}
UIA_COMMANDS = {"param", "function", "if", "else", "foreach", "return", "throw", "break",
                "Add-Type", "Join-Path", "Find-Required", "Assert-WriSettingsState", "ForEach-Object"}
APPROVED_DOT_SOURCE = ". (Join-Path $PSScriptRoot 'windows-runner.ps1')"
APPROVED_CONSTRUCTORS = {
    "[System.Windows.Automation.PropertyCondition]::new($ae::AutomationIdProperty,$Id)",
    "[System.Windows.Automation.PropertyCondition]::new($ae::AutomationIdProperty,'EntityItemButton')",
}


@lru_cache(maxsize=1)
def phn_module():
    # Local hostname stays in the process environment; every published child outcome passes PHN.
    os.environ["CFD_PII_HOSTNAMES"] = socket.gethostname()
    spec = importlib.util.spec_from_file_location("phn", ROOT / "tools/check-proof-pii.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def scrub_text(value: str | bytes | None) -> str:
    if isinstance(value, bytes):
        value = value.decode("utf-8", errors="replace")
    value = value or ""
    home = str(Path.home())
    for original, replacement in ((home.replace('\\', '\\\\'), "%USERPROFILE%"),
                                  (home, "%USERPROFILE%"), (home.replace('\\', '/'), "%USERPROFILE%"),
                                  (socket.gethostname(), "<host>"), (str(ROOT), "$REPO")):
        value = re.sub(re.escape(original), lambda _: replacement, value, flags=re.I)
    value = re.sub(r"S-1-5-21-\d+-\d+-\d+(?:-\d+)?", "S-1-5-21-<machine>-<RID>", value)
    if phn_module().scan_text(value):
        return "PHN rejected derivative; text withheld"
    return value


def run_bounded_command(command: list[str], timeout: float, cwd: Path | None = None) -> dict:
    child = None
    try:
        child = subprocess.Popen(command, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = child.communicate(timeout=timeout)
        return {"exit": child.returncode, "child_exit": child.returncode, "stdout": scrub_text(stdout), "stderr": scrub_text(stderr),
                "timed_out": False, "cleanup": "root-exit-observed", "descendants": "not-assessed"}
    except subprocess.TimeoutExpired as exc:
        stdout, stderr = exc.output, exc.stderr
        cleanup = "root-exit-not-recorded"
        if child is not None:
            try:
                child.kill()
                stdout, stderr = child.communicate(timeout=1)
                cleanup = "root-termination-observed"
            except (OSError, subprocess.TimeoutExpired):
                cleanup = "root-termination-unresolved"
        return {"exit": 124, "child_exit": child.returncode if child is not None else None, "stdout": scrub_text(stdout), "stderr": scrub_text(stderr),
                "timed_out": True, "cleanup": cleanup, "descendants": "not-assessed"}
    except Exception as exc:
        cleanup = "not-started"
        if child is not None and child.poll() is None:
            try:
                child.kill(); child.communicate(timeout=1)
                cleanup = "root-termination-observed"
            except (OSError, subprocess.TimeoutExpired):
                cleanup = "root-termination-unresolved"
        return {"exit": 1, "child_exit": child.returncode if child is not None else None, "stdout": "", "stderr": scrub_text("child failure=" + type(exc).__name__),
                "timed_out": False, "cleanup": cleanup, "descendants": "not-assessed"}


@lru_cache(maxsize=32)
def uia_ast_surface(source: str) -> tuple[set[str], set[str], bool]:
    """Parse active commands/methods without executing the candidate preflight."""
    program = r'''param([string]$Source)
$tokens=$null; $errors=$null
$tree=[System.Management.Automation.Language.Parser]::ParseFile($Source,[ref]$tokens,[ref]$errors)
$commands=@($tree.FindAll({param($n) $n -is [System.Management.Automation.Language.CommandAst]},$true) | ForEach-Object { if ($_.InvocationOperator -eq 'Dot') {'<dot-source>'} else { $name=$_.GetCommandName(); if ($name) {$name} else {'<dynamic>'} } })
$methods=@($tree.FindAll({param($n) $n -is [System.Management.Automation.Language.InvokeMemberExpressionAst]},$true) | ForEach-Object {if ($_.Member -is [System.Management.Automation.Language.StringConstantExpressionAst]) {$_.Member.Value} else {'<dynamic>'}})
$dots=@($tree.FindAll({param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.InvocationOperator -eq 'Dot'},$true) | ForEach-Object {$_.Extent.Text})
$static=@($tree.FindAll({param($n) $n -is [System.Management.Automation.Language.InvokeMemberExpressionAst] -and $n.Static},$true) | ForEach-Object {@{target=$_.Expression.Extent.Text;member=$_.Member.Value;call=$_.Extent.Text}})
@{commands=$commands;methods=$methods;dots=$dots;static=$static;errors=@($errors).Count} | ConvertTo-Json -Compress -Depth 5
'''
    with tempfile.TemporaryDirectory(prefix="cfd-wri-ast-") as scratch:
        parser = Path(scratch) / "parse.ps1"
        candidate = Path(scratch) / "candidate.ps1"
        parser.write_text(program, encoding="utf-8", newline="\n")
        candidate.write_text(source, encoding="utf-8", newline="\n")
        result = run_bounded_command(["pwsh", "-NoProfile", "-File", str(parser), str(candidate)], timeout=15)
        if result["exit"]:
            return set(), set(), False
        try:
            parsed = json.loads(result["stdout"])
        except (ValueError, TypeError):
            return set(), set(), False
        commands = set(parsed["commands"])
        valid = parsed["dots"] == [APPROVED_DOT_SOURCE] and parsed["errors"] == 0
        constructors = []
        for call in parsed["static"]:
            if call["member"] == "new":
                constructors.append(call["call"])
            elif call["target"] != "[string]" or call["member"] != "Join":
                valid = False
        valid = valid and len(constructors) == 2 and set(constructors) == APPROVED_CONSTRUCTORS
        commands.discard("<dot-source>")
        return commands, set(parsed["methods"]), valid


def active_code(text: str) -> str:
    """Remove PowerShell comments and embedded C#; quoted # is data, not a comment."""
    result, i = [], 0
    while i < len(text):
        if text.startswith("<#", i):
            end = text.find("#>", i + 2)
            i = len(text) if end < 0 else end + 2
            result.append(" ")
        elif text.startswith("@'", i) or text.startswith('@"', i):
            terminator = "\n'@" if text[i + 1] == "'" else '\n"@'
            end = text.find(terminator, i + 2)
            i = len(text) if end < 0 else end + len(terminator)
            result.append("'<embedded>'")
        elif text[i] in "'\"":
            quote, start = text[i], i
            i += 1
            while i < len(text):
                if quote == '"' and text[i] == '`':
                    i += 2
                elif text[i] == quote:
                    if quote == "'" and text[i:i + 2] == "''":
                        i += 2
                    else:
                        i += 1
                        break
                else:
                    i += 1
            result.append(text[start:i])
        elif text[i] == "#":
            end = text.find("\n", i)
            i = len(text) if end < 0 else end
        else:
            result.append(text[i])
            i += 1
    return "".join(result)


def mask_strings(text: str) -> str:
    return re.sub(r"'(?:''|[^'])*'|\"(?:`.|[^\"])*\"", lambda m: ' ' * len(m[0]), text)


def function_body(active: str, name: str) -> str:
    masked = mask_strings(active)
    match = re.search(r"\bfunction\s+" + re.escape(name) + r"\b[^{}]*\{", masked)
    if not match:
        return ""
    start, depth = match.end(), 1
    for index in range(start, len(masked)):
        depth += (masked[index] == "{") - (masked[index] == "}")
        if depth == 0:
            return active[start:index]
    return ""


def problems(runner: str, preflight: str) -> list[str]:
    found = []
    preflight_source = preflight
    runner, preflight = active_code(runner), active_code(preflight)
    combined = runner + "\n" + preflight
    if re.search(r"AppliedDPI|Registry|HKCU:|HKLM:|Get-ItemProperty|Set-ItemProperty", combined, re.I):
        found.append("a registry access")
    methods = set(re.findall(r"\.([A-Za-z_]\w*)\s*\(", preflight))
    masked_preflight = mask_strings(preflight)
    commands = {m[1] for m in re.finditer(r"(?im)(?:^|[;|{}])\s*([A-Za-z][\w-]*)", masked_preflight)
                if not masked_preflight[m.end():].lstrip().startswith("=")}
    static_methods = set(re.findall(r"::([A-Za-z_]\w*)\s*\(", preflight))
    if static_methods - {"new", "Join"}:
        found.append("b UIA static mutation beyond preflight")
    constructor_targets = re.findall(r"\[([^]]+)\]::new\(", preflight)
    dots = re.findall(r"(?m)^\s*\.\s+.*$", preflight)
    if constructor_targets != ["System.Windows.Automation.PropertyCondition"] * 2 or [d.strip() for d in dots] != [APPROVED_DOT_SOURCE]:
        found.append("b exact UIA constructors/dot-source missing or added")
    if os.name == "nt":
        ast_commands, ast_methods, valid = uia_ast_surface(preflight_source)
        if not valid or ast_commands - UIA_COMMANDS or ast_methods - (UIA_READ_METHODS | {"new", "Join"}):
            found.append("b UIA parsed command/method outside read-only allowlist")
    if methods - UIA_READ_METHODS or commands - UIA_COMMANDS or re.search(r"&\s|\$\w+\.[\w.]+\s*=(?!=)", preflight):
        found.append("b UIA mutation beyond preflight")
    if "ValidateSet('Preflight')" not in preflight or "'-Action','Preflight'" not in runner:
        found.append("b exact Preflight action missing")
    if re.search(r"\[(?:System\.)?DateTime(?:Offset)?\].*?::\s*Parse|UtcNow\s*-g[et]", combined, re.I):
        found.append("c parsed/mixed-kind deadline")
    wait_sites = re.findall(r"\$\w+\.WaitForExit\([^)]*\)", runner)
    process_body = function_body(runner, "Invoke-WriProcess")
    if wait_sites != ["$Process.WaitForExit([int]$remainingMs)"] or "[Diagnostics.Stopwatch]::StartNew()" not in runner or "Wait-WriDeadline $child $rootDeadline $Clock.ElapsedMilliseconds" not in process_body:
        found.append("c Stopwatch remaining deadline missing")
    if "sdk/10.0.203" not in runner or "$sdkVersion -ne '10.0.203'" not in runner or "GetFullPath($DotnetPath) -ne $expectedDotnet" not in runner:
        found.append("d exact toolchain identity missing")
    if not re.search(r"\$handle\s*=\s*\$child.Handle", process_body) or not re.search(r"\$exitCode\s*=\s*\$child.ExitCode\s+Assert-WriNumericExit\s+\$exitCode", process_body):
        found.append("e retained handle or numeric exit missing")
    child_body = function_body(runner, "Invoke-WriChild")
    if not re.search(r"Assert-WriSourceClean \$Repo \$Clock \$deadline\s+\$before = Get-WriSourceFingerprint \$Repo \$Clock \$deadline\s+try \{\s+\$result = Invoke-WriProcess", child_body) or not re.search(r"finally \{\s+Assert-WriSourceUnchanged \$Repo \$before \$Clock \$deadline\s+Assert-WriEnvelope \$Clock \$deadline", child_body) or "Assert-WriSourceUnchanged $repo $baseline" not in runner:
        found.append("f before/after source protection missing")
    if "Assert-WriEnvelope $Clock $deadline" not in process_body or "Stop-WriJob $job $Clock $deadline" not in process_body:
        found.append("c total envelope/cleanup missing")
    if "@('build-server','shutdown')" not in runner or "$result.ExitCode -ne 0" not in runner:
        found.append("R181 numeric build-server shutdown missing")
    return found


def self_test() -> int:
    timeout_probe = run_bounded_command([sys.executable, "-c", "import os,socket,time; print(os.path.expanduser('~'),flush=True); print(socket.gethostname(),flush=True); print('S-1-5-21-'+'1-2-3-4',flush=True); time.sleep(60)"], timeout=0.75)
    if not timeout_probe["timed_out"] or timeout_probe["cleanup"] != "root-termination-observed" or not isinstance(timeout_probe["child_exit"], int) or phn_module().scan_text(timeout_probe["stdout"] + timeout_probe["stderr"]) or not all(placeholder in timeout_probe["stdout"] for placeholder in ("%USERPROFILE%", "<host>", "S-1-5-21-<machine>-<RID>")):
        print("FAIL checker timeout sanitization/cleanup fixture")
        return 1
    print(f"RED checker timeout rejected wrapper_exit=124 observed_child_exit={timeout_probe['child_exit']} cleanup=root-termination-observed descendants=not-assessed PHN=PASS traceback=false")
    policy_started = time.perf_counter()
    runner, preflight = RUNNER.read_text(encoding="utf-8"), PREFLIGHT.read_text(encoding="utf-8")
    fixtures = [
        ("a", runner + "\nGet-ItemProperty HKCU:\\ControlPanel -Name AppliedDPI", preflight),
        ("b", runner, preflight + "\n$expand.Expand()"),
        ("b", runner, preflight + "\n$scroll.ScrollIntoView()"),
        ("b", runner, preflight + "\n$virtual.Realize()"),
        ("b", runner, preflight + "\n$item.Select()"),
        ("b", runner.replace("'-Action','Preflight'", "'-Action','200'"), preflight),
        ("c", runner + "\nif([DateTime]::UtcNow -gt [DateTime]::Parse('2026-10-09T00:00:00Z')){}", preflight),
        ("c", runner.replace("$Process.WaitForExit([int]$remainingMs)", "$Process.WaitForExit(900000)"), preflight),
        ("d", runner.replace("$sdkVersion -ne '10.0.203'", "$sdkVersion -ne '10.0.999'"), preflight),
        ("e", runner.replace("$handle = $child.Handle", "$handle = $null"), preflight),
        ("e", runner.replace("Assert-WriNumericExit $exitCode", "# lost exit accepted"), preflight),
        ("f", runner.replace("Assert-WriSourceUnchanged $Repo $before", "# after-source omitted"), preflight),
        ("c", runner + "\n$child.WaitForExit()", preflight),
        ("c", runner.replace("Wait-WriDeadline $child $rootDeadline $Clock.ElapsedMilliseconds", "$child.WaitForExit()"), preflight),
        ("e", runner.replace("Assert-WriNumericExit $exitCode", "# Assert-WriNumericExit $exitCode"), preflight),
        ("f", runner.replace("    Assert-WriSourceClean $Repo $Clock $deadline\n    $before", "    $before"), preflight),
        ("b", runner, preflight + "\nSet-Content arbitrary.txt changed"),
        ("b", runner, preflight + "\n$writer=[IO.StreamWriter]::new('leak.txt')"),
        ("b", runner, preflight + "\n. (Join-Path $PSScriptRoot 'mutator.ps1')"),
    ]
    for number, (control, mutant_runner, mutant_preflight) in enumerate(fixtures, 1):
        if mutant_runner == runner and mutant_preflight == preflight:
            print(f"FAIL red fixture {number}: unchanged mutation anchor")
            return 1
        observed = problems(mutant_runner, mutant_preflight)
        if not any(p.startswith(control + " ") for p in observed):
            print(f"FAIL red fixture {number}: {control}")
            return 1
        print(f"RED fixture={number} control={control} rejected={next(p for p in observed if p.startswith(control + ' '))}")
    clean = problems(runner, preflight)
    if clean:
        print("FAIL unmutated policy: " + "; ".join(clean))
        return 1
    print("GREEN policy controls=a,b,c,d,e,f,R181")
    print(f"policy_self_test_ms={round((time.perf_counter() - policy_started) * 1000, 3)}")
    if os.name == "nt":
        runtime_started = time.perf_counter()
        result = run_bounded_command(["pwsh", "-NoProfile", "-File", str(ROOT / "tools/test-windows-runner.ps1"), "-PythonPath", sys.executable], cwd=ROOT, timeout=60)
        print(result["stdout"] + result["stderr"], end="")
        print(f"runtime_self_test_exit={result['exit']} cleanup={result['cleanup']} descendants={result['descendants']} runtime_self_test_ms={round((time.perf_counter() - runtime_started) * 1000, 3)}")
        return result["exit"]
    print("Windows runtime self-test NOT ASSESSED on this platform; policy PASS")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--scrub-capture", type=Path)
    args = parser.parse_args()
    if args.scrub_capture:
        raw = json.loads(args.scrub_capture.read_text(encoding="utf-8"))
        substitutions = []
        home = str(Path.home())
        host = socket.gethostname()
        # Literal hostname is confined to the process environment, never emitted or committed.
        os.environ["CFD_PII_HOSTNAMES"] = host
        sys.path.insert(0, str(ROOT / "tools"))
        phn = phn_module()
        safe = {}
        for stream in ("stdout", "stderr"):
            value = raw[stream]
            for kind, original, replacement in (("home", home, "%USERPROFILE%"), ("home-json", home.replace('\\', '\\\\'), "%USERPROFILE%"), ("home-posix", home.replace('\\', '/'), "%USERPROFILE%"), ("hostname", host, "<host>")):
                matches = len(re.findall(re.escape(original), value, re.I))
                if matches:
                    substitutions.append({"stream": stream, "kind": kind, "count": matches, "original_sha256": hashlib.sha256(original.encode()).hexdigest(), "replacement": replacement})
                    value = re.sub(re.escape(original), lambda _: replacement, value, flags=re.I)
            for sid in re.findall(r"S-1-5-21-\d+-\d+-\d+(?:-\d+)?", value):
                substitutions.append({"stream": stream, "kind": "sid", "original_sha256": hashlib.sha256(sid.encode()).hexdigest(), "replacement": "S-1-5-21-<machine>-<RID>"})
                value = value.replace(sid, "S-1-5-21-<machine>-<RID>")
            if phn.scan_text(value):
                print("PHN rejected capture derivative", file=sys.stderr)
                return 1
            safe[stream] = value
        safe["substitutions"] = substitutions
        print(json.dumps(safe))
        return 0
    if args.self_test:
        return self_test()
    policy_started = time.perf_counter()
    found = problems(RUNNER.read_text(encoding="utf-8"), PREFLIGHT.read_text(encoding="utf-8"))
    for problem in found:
        print("WRI-POLICY FAIL " + problem)
    print(f"WRI-POLICY {'FAIL' if found else 'PASS'} controls=a,b,c,d,e,f,R181")
    print(f"policy_check_ms={round((time.perf_counter() - policy_started) * 1000, 3)} platform={'Windows-AST' if os.name == 'nt' else 'portable-lexical'} runtime_qualification=not-run")
    return bool(found)


if __name__ == "__main__":
    # PLAT-A: match pack-doctor's legacy-console guard; never depend on cp1252.
    for _stream in (sys.stdout, sys.stderr):
        if hasattr(_stream, "reconfigure"):
            try:
                _stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    try:
        raise SystemExit(main())
    except Exception as exc:
        try:
            error = scrub_text("WRI-ERROR failure=" + type(exc).__name__ + " cleanup=not-recorded output=withheld traceback=false")
        except Exception:
            error = "WRI-ERROR publication-control-unavailable cleanup=not-recorded output=withheld traceback=false"
        print(error, file=sys.stderr)
        raise SystemExit(1)
