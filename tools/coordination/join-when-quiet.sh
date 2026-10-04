#!/usr/bin/env bash
# Hold the join lock, wait for the running solver job (if any) to end and the 1-minute load to fall below 10, run the
# given conductor-join args, release. Usage: join-when-quiet.sh <out-file> <conductor-join args...>
# Env: CFDW_COORD_DIR   lock/trailer directory (default: <git common dir>/coord, created if missing; trailer.txt lives there)
#      CFDW_INTEGRATION the integration worktree the join runs in (required)
#      CFDW_JOIN_SESSION the session id passed to conductor-join (default: the AGENT_SESSION value, required)
# cases/tools/of-run.sh waits on <coord dir>/join.lock, so the two share one signal (defect class JOIN-RESOURCE-RACE).
# The lock comes FIRST so a solver track that starts jobs back to back cannot starve a join: it checks the lock before
# each new job. Solver detection matches process NAMES exactly (pgrep -x), never command-line text - a shell whose
# command line merely mentions "snappyHexMesh" once deadlocked a join against the solver track's own lock-wait loop.
set -u
S="${CFDW_COORD_DIR:-$(git rev-parse --path-format=absolute --git-common-dir)/coord}"
mkdir -p "$S"
here="$(cd "$(dirname "$0")" && pwd)"
integration="${CFDW_INTEGRATION:?set CFDW_INTEGRATION to the integration worktree path}"
session="${CFDW_JOIN_SESSION:-${AGENT_SESSION:?set CFDW_JOIN_SESSION or AGENT_SESSION}}"
out="$1"; shift
load1() { uptime | sed -E 's/.*load averages?: ([0-9]+).*/\1/'; }
foam() {
  local name
  for name in snappyHexMesh blockMesh checkMesh simpleFoam rhoSimpleFoam pimpleFoam potentialFoam decomposePar \
              reconstructPar surfaceFeatureExtract extrudeMesh gmsh mpirun orterun prterun; do
    pgrep -x "$name" >/dev/null && return 0
  done
  return 1
}
until mkdir "$S/join.lock.d" 2>/dev/null; do sleep 15; done   # one join at a time
touch "$S/join.lock"
trap 'rm -f "$S/join.lock"; rmdir "$S/join.lock.d" 2>/dev/null' EXIT
until ! foam && [ "$(load1)" -lt 10 ]; do sleep 20; done
echo "start load: $(uptime | sed 's/.*load averages*: //')"
cd "$integration" || exit 2
AGENT_SESSION="$session" python3 docs/ai-forward-pack/scripts/conductor-join.py "$@" --session "$session" \
  --trailer-file "$S/trailer.txt" > "$out" 2>&1
rc=$?
# The derived index and the append-only logs conflict at almost every join. When only they conflict, resolve and continue.
if [ $rc -ne 0 ] && grep -q 'CONFLICT' "$out"; then
  conflicted=$(git diff --name-only --diff-filter=U | tr '\n' ' ')
  others=$(printf '%s' "$conflicted" | tr ' ' '\n' | grep -vE '^(docs/docs-index.js|docs/audit/(audit|change)-log.jsonl)?$')
  if [ -n "$conflicted" ] && [ -z "$others" ]; then
    for log in docs/audit/audit-log.jsonl docs/audit/change-log.jsonl; do
      case " $conflicted " in *" $log "*)
        sed -i '' -E '/^(<<<<<<< .*|=======|>>>>>>> .*)$/d' "$log"
        python3 "$here/check-jsonl.py" "$log" || exit 1
        git add "$log" ;;
      esac
    done
    git checkout --theirs docs/docs-index.js 2>/dev/null; python3 docs/ai-forward-pack/scripts/docs-graph.py derive >/dev/null \
      && git add docs/docs-index.js && AGENT_SESSION="$session" git commit -q --no-edit \
      && echo "auto-resolved: $conflicted; continuing" >> "$out"
    set -- "$@" --continue
    AGENT_SESSION="$session" python3 docs/ai-forward-pack/scripts/conductor-join.py "$@" --session "$session" \
      --trailer-file "$S/trailer.txt" >> "$out" 2>&1
    rc=$?
  fi
fi
# A budget overrun is only a regression on a quiet machine. Other tracks can push the load up during the run (four joins
# on 2026-10-04 went 63-79 s at loads 24-30 and passed on re-run). When the last wall line is over budget AND the load
# rose above 15 during that run, wait for quiet and continue once; a second overrun stands.
wall_line=$(grep -E 'wall [0-9]+ s \(budget [0-9]+ s\)' "$out" | tail -1)
if [ $rc -ne 0 ] && [ -n "$wall_line" ]; then
  wall=$(printf '%s' "$wall_line" | sed -E 's/.*wall ([0-9]+) s \(budget ([0-9]+) s\).*/\1/')
  budget=$(printf '%s' "$wall_line" | sed -E 's/.*wall ([0-9]+) s \(budget ([0-9]+) s\).*/\2/')
  end_load=$(printf '%s' "$wall_line" | sed -nE 's/.*load [0-9.]+ -> ([0-9]+).*/\1/p')
  if [ "$wall" -gt "$budget" ] && [ -n "$end_load" ] && [ "$end_load" -gt 15 ]; then
    echo "budget overrun under load ($wall_line); waiting for quiet and continuing once" >> "$out"
    until ! foam && [ "$(load1)" -lt 10 ]; do sleep 20; done
    case " $* " in *" --continue "*) ;; *) set -- "$@" --continue ;; esac
    AGENT_SESSION="$session" python3 docs/ai-forward-pack/scripts/conductor-join.py "$@" --session "$session" \
      --trailer-file "$S/trailer.txt" >> "$out" 2>&1
    rc=$?
  fi
fi
echo "join=$rc $(grep -E 'wall|CONFLICT|exited' "$out" | tr '\n' ' ') end load: $(uptime | sed 's/.*load averages*: //')"
exit $rc
