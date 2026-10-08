#!/usr/bin/env bash
# The join's test-ring step (docs/coordination/join.json checks). Ruling 89 (DR-JOIN-1): a join whose merge changes no path
# under src/, tests/, tools/, cases/ and no *.csproj, *.slnx, global.json or Directory.*.props skips tools/run-tests.sh and
# (which runs tools/check-test-costs.py itself) and prints `RING-SKIPPED docs-only: <changed paths, truncated>`. check-docs, the other join
# checks and the verify gates still run (they are their own join.json entries). The rule is derived from the merge diff
# (HEAD^1..HEAD, HEAD being the merge commit in conductor-join's checks step), never from a flag. Fail safe: a HEAD that
# is not a merge, or a diff that cannot be read, runs the ring. The readiness ring before main is unchanged.
#
#   tools/join-ring.sh              run, or skip, the ring for the current HEAD
#   tools/join-ring.sh --self-test  planted merges in a scratch repo (ring: every join; cost: about 1 s)
# Bash 3.2-safe. Exit: the ring's own exit status, else 0.

# stdin: changed paths, one per line. Prints the code paths (those that need the ring), none when docs-only.
join_ring_code_paths() {
  grep -E '^(src|tests|tools|cases)/|(^|/)[^/]*\.csproj$|(^|/)[^/]*\.slnx$|(^|/)global\.json$|(^|/)Directory\.[^/]*\.props$' || true
}

# Prints "run" or "skip <paths>" for the repo at $1.
join_ring_decide() {
  local repo="$1" paths code
  if ! git -C "$repo" rev-parse --verify -q 'HEAD^2' >/dev/null 2>&1; then echo "run not-a-merge"; return; fi
  if ! paths=$(git -C "$repo" diff --name-only 'HEAD^1' HEAD 2>/dev/null); then echo "run diff-unreadable"; return; fi
  code=$(printf '%s\n' "$paths" | join_ring_code_paths)
  if [ -n "$code" ]; then echo "run code-changed"; return; fi
  paths=$(printf '%s\n' "$paths" | grep . | head -8 | tr '\n' ' ' || true)
  echo "skip ${paths:-none}"
}

join_ring_main() {
  local root verdict
  root="$(cd "$(dirname "$0")/.." && pwd -P)"
  verdict=$(join_ring_decide "$root")
  case "$verdict" in
    skip*) echo "RING-SKIPPED docs-only: ${verdict#skip }"; return 0;;
  esac
  cd "$root"
  # tools/run-tests.sh runs tools/check-test-costs.py with the load it measured; a second call here had no load, so it
  # printed every rule as a COST-MISS and added nothing (docs/proof/rdh/readiness.md).
  "${JOIN_RING_TESTS:-tools/run-tests.sh}"
}

join_ring_self_test() {
  local dir fails=0 total=0 base
  dir=$(mktemp -d "${TMPDIR:-/tmp}/join-ring-test.XXXXXX")
  expect() {  # label, repo, expected verdict prefix
    local got; got=$(join_ring_decide "$2"); total=$((total + 1))
    case "$got" in "$3"*) echo "SELFTEST PASS $1";; *) echo "SELFTEST FAIL $1: expected $3, got $got"; fails=$((fails + 1));; esac
  }
  # make_merge <repo> <path touched on the side branch>: main has one commit, a side branch changes <path>, merge --no-ff
  make_merge() {
    git init -q "$1"; git -C "$1" config user.email t@t; git -C "$1" config user.name t
    git -C "$1" config commit.gpgsign false
    echo base > "$1/README.md"; git -C "$1" add -A; git -C "$1" commit -qm base
    git -C "$1" checkout -qb side
    mkdir -p "$(dirname "$1/$2")"; echo x > "$1/$2"; git -C "$1" add -A; git -C "$1" commit -qm side
    git -C "$1" checkout -q - ; echo main > "$1/main.txt"; git -C "$1" add -A; git -C "$1" commit -qm main
    git -C "$1" merge -q --no-ff -m merge side
  }
  base="$dir"
  make_merge "$base/docs" docs/notes/a.md;                 expect "docs-only merge skips" "$base/docs" skip
  make_merge "$base/src" src/App/Program.cs;               expect "merge touching src/ runs" "$base/src" run
  make_merge "$base/tools" tools/join-ring.sh;             expect "merge touching tools/ runs" "$base/tools" run
  make_merge "$base/tests" tests/X/Y.cs;                   expect "merge touching tests/ runs" "$base/tests" run
  make_merge "$base/cases" cases/a.yaml;                   expect "merge touching cases/ runs" "$base/cases" run
  make_merge "$base/proj" docs/Thing.csproj;               expect "a .csproj anywhere runs" "$base/proj" run
  make_merge "$base/gj" global.json;                       expect "global.json runs" "$base/gj" run
  make_merge "$base/props" build/Directory.Build.props;    expect "Directory.*.props runs" "$base/props" run
  make_merge "$base/slnx" X.slnx;                          expect "a .slnx runs" "$base/slnx" run
  make_merge "$base/lookalike" docs/tools/readme.md;       expect "docs/tools/ is not tools/ (skips)" "$base/lookalike" skip
  git -C "$base/docs" checkout -q HEAD^1;                  expect "a non-merge HEAD runs (fail safe)" "$base/docs" run
  rm -rf "$dir"
  echo "SELFTEST $((total - fails))/$total cases"
  [ "$fails" -eq 0 ]
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  if [ "${1:-}" = "--self-test" ]; then join_ring_self_test; exit $?; fi
  join_ring_main; exit $?
fi
