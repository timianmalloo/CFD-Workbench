#!/usr/bin/env bash
# PUSH-SUCCESS-BY-TEXT control (docs/lessons/defect-classes.md): a push is done only when the remote ref equals the local SHA.
# Never judge a push by its output text: "main -> main" also appears in "! [remote rejected] main -> main (Internal Server Error)".
#
#   tools/verify-push.sh [branch] [remote] [sha]    defaults: main, origin, HEAD
#   tools/verify-push.sh --self-test
# Exit 0 "PUSH-OK <sha>" only when `git ls-remote <remote> refs/heads/<branch>` equals the SHA.
# Exit 1 "PUSH-NOT-DONE ..." when the remote differs or lacks the branch; exit 2 when the remote cannot be read.
set -uo pipefail

verify() {  # verify <branch> <remote> <sha>   (run inside the repository)
  local branch="$1" remote="$2" want="$3" out got
  out=$(git ls-remote "$remote" "refs/heads/$branch" 2>&1) || { echo "PUSH-UNKNOWN cannot read $remote: $out" >&2; return 2; }
  got=$(printf '%s\n' "$out" | awk 'NR==1 {print $1}')
  if [ -n "$got" ] && [ "$got" = "$want" ]; then echo "PUSH-OK $want"; return 0; fi
  echo "PUSH-NOT-DONE remote ${remote}/${branch} is '${got:-absent}', local is $want" >&2
  return 1
}

self_test() {
  local dir rc=0 sha
  dir=$(mktemp -d) || return 1
  trap 'rm -rf "$dir"' RETURN
  git init -q --bare "$dir/remote.git" && git init -q -b main "$dir/work" || return 1
  (
    cd "$dir/work" || exit 1
    git config user.email t@example.invalid; git config user.name t
    git remote add origin "$dir/remote.git"
    git commit -q --allow-empty -m one
    # Planted rejected-push output: it contains "main -> main". A text check would call this success; the tool never reads it.
    printf '%s\n' 'To origin' ' ! [remote rejected] main -> main (Internal Server Error)' > "$dir/push-output.txt"
    grep -q 'main -> main' "$dir/push-output.txt" || exit 1
    sha=$(git rev-parse HEAD)
    verify main origin "$sha" 2>/dev/null && { echo "self-test FAIL: unpushed commit reported done"; exit 1; }
    git push -q origin main
    verify main origin "$sha" >/dev/null || { echo "self-test FAIL: pushed commit not reported done"; exit 1; }
    git commit -q --allow-empty -m two
    verify main origin "$(git rev-parse HEAD)" 2>/dev/null && { echo "self-test FAIL: stale remote reported done"; exit 1; }
    verify main "$dir/missing.git" "$sha" 2>/dev/null; [ $? -eq 2 ] || { echo "self-test FAIL: unreadable remote not exit 2"; exit 1; }
    echo "verify-push self-test OK: unpushed red, pushed green, stale red, unreadable remote exit 2, rejected-push text ignored"
  ) || rc=1
  return $rc
}

if [ "${1:-}" = "--self-test" ]; then self_test; exit $?; fi
branch="${1:-main}"; remote="${2:-origin}"
want="${3:-$(git rev-parse HEAD)}"
verify "$branch" "$remote" "$want"
