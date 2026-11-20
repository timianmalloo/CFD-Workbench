#!/usr/bin/env bash
set -euo pipefail

# Reproduces test 24 (docs/design/cross-profile-abscissa.md §3.6, §10):
# A pre-CF (cfdw-cv/2-only) build at commit df566d56 refuses a cfdw-cv/3 project
# with DOC-VERSION (exit 3) and leaves the file bytes untouched.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"
PRE_CF_COMMIT="${1:-df566d56}"
PROJECT_SRC="$SCRIPT_DIR/mixed-spacing.cfdw.json"
TMP_DIR="$(mktemp -d)"
OLD_TREE="$TMP_DIR/pre-cf"
WORK_FILE="$TMP_DIR/mixed-spacing.cfdw.json"

cleanup() {
  git -C "$REPO_ROOT" worktree remove --force "$OLD_TREE" >/dev/null 2>&1 || true
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

cp "$PROJECT_SRC" "$WORK_FILE"
WORK_ARG="$WORK_FILE"
if command -v cygpath >/dev/null 2>&1; then
  WORK_ARG="$(cygpath -w "$WORK_FILE")"
fi
SHA_BEFORE="$(sha256sum "$WORK_FILE" | awk '{print $1}')"
echo "SHA256_BEFORE=$SHA_BEFORE"

# 1. Verify the current CF build inspects the project with evaluator cfdw-cv/3.
dotnet build "$REPO_ROOT/src/CfdWorkbench.Cli/CfdWorkbench.Cli.csproj" -c Release -v quiet --nologo
CLI_NEW="$REPO_ROOT/src/CfdWorkbench.Cli/bin/Release/net10.0/CfdWorkbench.Cli.dll"
echo "=== Current (CF) inspect --json ==="
NEW_OUT="$(dotnet "$CLI_NEW" inspect "$WORK_ARG" --json)"
printf '%s\n' "$NEW_OUT" | grep -E '"evaluator"|"status"|"code"' | head -n 10
if ! grep -q '"evaluator": "cfdw-cv/3"' <<< "$NEW_OUT"; then
  echo "FAIL: current CF build did not report evaluator cfdw-cv/3" >&2
  exit 1
fi

# 2. Checkout the pre-CF commit into a detached scratch worktree and build its CLI.
git -C "$REPO_ROOT" worktree add --detach "$OLD_TREE" "$PRE_CF_COMMIT" >/dev/null 2>&1
dotnet build "$OLD_TREE/src/CfdWorkbench.Cli/CfdWorkbench.Cli.csproj" -c Release -v quiet --nologo
CLI_OLD="$OLD_TREE/src/CfdWorkbench.Cli/bin/Release/net10.0/CfdWorkbench.Cli.dll"

# 3. Inspect the cfdw-cv/3 project with the pre-CF CLI; expect exit 3 and DOC-VERSION.
echo "=== Pre-CF ($PRE_CF_COMMIT) inspect --json ==="
set +e
OLD_OUT="$(dotnet "$CLI_OLD" inspect "$WORK_ARG" --json 2>&1)"
OLD_EXIT=$?
set -e
printf '%s\n' "$OLD_OUT"
echo "OLD_EXIT=$OLD_EXIT"

SHA_AFTER="$(sha256sum "$WORK_FILE" | awk '{print $1}')"
echo "SHA256_AFTER=$SHA_AFTER"

if [[ "$OLD_EXIT" -ne 3 ]]; then
  echo "FAIL: expected exit 3 from pre-CF build, got $OLD_EXIT" >&2
  exit 1
fi
if ! grep -q "DOC-VERSION" <<< "$OLD_OUT"; then
  echo "FAIL: pre-CF output did not report DOC-VERSION" >&2
  exit 1
fi
if [[ "$SHA_BEFORE" != "$SHA_AFTER" ]]; then
  echo "FAIL: pre-CF open modified file bytes" >&2
  exit 1
fi
echo "PASS: pre-CF build ($PRE_CF_COMMIT) refused cfdw-cv/3 project with DOC-VERSION (exit $OLD_EXIT) and left bytes untouched."
