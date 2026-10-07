#!/usr/bin/env bash
# One Python resolver for the bash tools (WFX item 1). On Windows `python3` is the Microsoft Store alias: it prints
# "Python was not found" and exits 9009, so a command name on PATH is not proof. A candidate is WORKING only when it runs
# `-c 'import sys'` and exits 0. Order: python3, `py -3` (the Windows launcher), python.
#
#   . tools/py-resolve.sh; py -c 'print(1)'      py runs the first working candidate; exit 127 + message if none
#   tools/py-resolve.sh --self-test              fake candidates on PATH (cost: under 1 s)
# Bash 3.2-safe. The resolved choice is cached in PY_RESOLVED (a string, split on purpose: "py -3" is two words).

py_resolve() {
  [ -n "${PY_RESOLVED:-}" ] && return 0
  local c
  for c in "python3" "py -3" "python"; do
    # shellcheck disable=SC2086 # word splitting of "py -3" is the point
    if command $c -c 'import sys' >/dev/null 2>&1; then PY_RESOLVED="$c"; return 0; fi
  done
  return 1
}

py() {
  if ! py_resolve; then echo "py-resolve: no working python3, 'py -3' or python on PATH" >&2; return 127; fi
  # shellcheck disable=SC2086
  command $PY_RESOLVED "$@"
}

py_resolve_self_test() {
  local dir fails=0 total=0 got
  dir=$(mktemp -d "${TMPDIR:-/tmp}/py-resolve-test.XXXXXX")
  # fake <name> <exit>: a script that behaves like the Store alias (message, exit) or like a working interpreter (exit 0)
  fake() {
    printf '#!/bin/sh\n%s\n' "$2" > "$dir/$1"; chmod +x "$dir/$1"
  }
  expect() {  # label, expected choice
    total=$((total + 1))
    if [ "$got" = "$2" ]; then echo "SELFTEST PASS $1"; else echo "SELFTEST FAIL $1: expected '$2', got '$got'"; fails=$((fails + 1)); fi
  }
  alias_body='echo "Python was not found; run without arguments to install from the Microsoft Store" >&2; exit 9009'
  ok_body='exit 0'
  choose() { got=$(PY_RESOLVED="" PATH="$dir:$PATH" bash -c ". '$0'; py_resolve && echo \"\$PY_RESOLVED\"" 2>/dev/null || echo none); }

  fake python3 "$alias_body"; fake py "$ok_body"; fake python "$ok_body"
  choose; expect "Store-alias python3 is skipped, py -3 chosen" "py -3"
  fake py "$alias_body"
  choose; expect "alias py is skipped too, python chosen" "python"
  fake python3 "$ok_body"
  choose; expect "working python3 wins first" "python3"
  # the callers must use py, never a bare python3 command (comment lines excluded)
  local here; here="$(cd "$(dirname "$0")" && pwd -P)"
  got=$(grep -hE '^[^#]*python3 ' "$here/run-tests.sh" "$here/join-ring.sh" | grep -vc 'py-resolve' || true)
  expect "run-tests.sh and join-ring.sh call no bare python3" "0"
  rm -rf "$dir"
  echo "SELFTEST $((total - fails))/$total cases"
  [ "$fails" -eq 0 ]
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  if [ "${1:-}" = "--self-test" ]; then py_resolve_self_test; exit $?; fi
  echo "usage: . tools/py-resolve.sh   or   tools/py-resolve.sh --self-test" >&2; exit 2
fi
