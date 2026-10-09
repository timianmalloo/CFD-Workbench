#!/usr/bin/env bash
# Ring lock: caps concurrent test rings on this machine at CFD_RING_MAX (default 2). Ruling 87 condition (3):
# a cap stated only in brief text is a memoir (CI6), so tools/run-tests.sh sources this file and takes a slot.
#
# A slot is a directory under one fixed shared path, so every git worktree of this repo contends for the same slots:
#   ${CFD_RING_SLOTS_DIR:-$HOME/.cache/cfd-workbench/ring-slots}/slot-<k>/pid   (the owning run-tests.sh PID)
# mkdir is atomic and portable. A slot whose PID is dead (a crashed ring) is reclaimed by renaming it away, which only one
# contender can win. A slot with no pid file older than a minute is a crash between mkdir and the pid write; it is
# reclaimed the same way. The wait is bounded (CFD_RING_LOCK_WAIT_SECONDS, default 900): on timeout the ring proceeds
# without a slot and prints RING-LOCK-TIMEOUT rather than deadlock. Every outcome prints a countable line to the ring log:
#   RING-LOCK waited <s> s            RING-LOCK-TIMEOUT waited <s> s (holders: <pids>)
# Exclusive mode (READINESS-UNLOCKED): tools/run-readiness.py measures frame budgets, so it takes EVERY slot, owned by its
# own PID, for the whole ring. `--acquire-all <pid>` first writes ${dir}/exclusive-wanted (its PID) so that new track rings
# stop taking slots (otherwise a freed slot is retaken by the next track and readiness starves), then takes the slots one
# by one as the current holders finish. The wait is bounded (CFD_READINESS_LOCK_WAIT_SECONDS, default 600: the holders are
# at most CFD_RING_MAX rings or suite runs, each normally within its 60 s ring budget or 120 s suite wait; 600 s is several
# of those, and a longer wait means a hung holder an operator should see). It prints who holds the slots (pid and command)
# at once and every 30 s, then one line: RING-LOCK-EXCLUSIVE waited <s> s | RING-LOCK-EXCLUSIVE-TIMEOUT waited <s> s
# (holders: ...), and exits 0 / 5. `--release-all <pid>` removes the marker and the slots owned by that PID. A dead owner's
# marker and slots are stale and reclaimed like any other. A track ring waiting behind the marker keeps its own bounded wait.
# Bash 3.2-safe: no associative arrays, no ${!prefix@}, no BASHPID. Exit 0 from --self-test: all cases pass.
#
#   source tools/ring-lock.sh; ring_lock_acquire "$$"; trap ring_lock_release EXIT
#   tools/ring-lock.sh --self-test        (no ring is run; fake holders are `sleep` processes; ring: every join; cost: ~25 s)

RING_LOCK_SLOT=""

ring_lock_dir() { echo "${CFD_RING_SLOTS_DIR:-$HOME/.cache/cfd-workbench/ring-slots}"; }

# Reclaim slot $1 if its owner is dead (or it never got a pid). Silent if another contender won the rename.
ring_lock_reclaim_if_stale() {
  local slot="$1" pid
  pid=$(cat "$slot/pid" 2>/dev/null || true)
  if [ -n "$pid" ]; then
    kill -0 "$pid" 2>/dev/null && return 1
  else
    # no pid yet: a live contender is between mkdir and its write; give it a minute
    [ -n "$(find "$slot" -maxdepth 0 -mmin -1 2>/dev/null)" ] && return 1
  fi
  mv "$slot" "$slot.stale.$$.$RANDOM" 2>/dev/null && rm -rf "$slot".stale.* 2>/dev/null
  return 0
}

# True while a live readiness run has announced it wants every slot: new rings wait instead of taking one.
ring_lock_exclusive_pending() {
  local pid
  pid=$(cat "$1/exclusive-wanted" 2>/dev/null || true)
  [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null
}

# ring_lock_holders <dir> <max> [skip pid]: one "slot-k pid=<pid> <command>" line per held slot.
ring_lock_holders() {
  local k=1 pid
  while [ "$k" -le "$2" ]; do
    pid=$(cat "$1/slot-$k/pid" 2>/dev/null || true)
    if [ -n "$pid" ] && [ "$pid" != "${3:-}" ]; then echo "  slot-$k pid=$pid $(ps -o command= -p "$pid" 2>/dev/null | cut -c1-100)"; fi
    k=$((k + 1))
  done
}

# ring_lock_peers <own pid>: the PIDs (one per line) of the other LIVE holders of a slot right now. CCL-A: tools/run-tests.sh samples
# this once a second into .tmp-tests/peers.txt, so tools/check-test-costs.py can tell a cost miss under a concurrent ring (advisory,
# the holder named) from one on a quiet host (a failure). An overlap that began or ended mid-ring is still caught; the end load is not.
ring_lock_peers() {
  local dir k=1 max="${CFD_RING_MAX:-2}" pid
  dir=$(ring_lock_dir)
  while [ "$k" -le "$max" ]; do
    pid=$(cat "$dir/slot-$k/pid" 2>/dev/null || true)
    if [ -n "$pid" ] && [ "$pid" != "$1" ] && kill -0 "$pid" 2>/dev/null; then echo "$pid"; fi
    k=$((k + 1))
  done
}

# ring_lock_acquire_all <owner pid>: take every slot for <owner>; returns 0, or 5 after the bounded wait (nothing kept).
ring_lock_acquire_all() {
  local owner="$1" max="${CFD_RING_MAX:-2}" limit="${CFD_READINESS_LOCK_WAIT_SECONDS:-600}" poll="${CFD_RING_POLL_SECONDS:-2}"
  local dir begin k slot held=0 next_report=0 held_slots=""
  dir=$(ring_lock_dir)
  mkdir -p "$dir"
  echo "$owner" > "$dir/exclusive-wanted"
  begin=$SECONDS
  while :; do
    k=1
    while [ "$k" -le "$max" ]; do
      slot="$dir/slot-$k"
      case " $held_slots " in *" $k "*) k=$((k + 1)); continue;; esac
      if mkdir "$slot" 2>/dev/null; then
        echo "$owner" > "$slot/pid"
        held_slots="$held_slots $k"
        held=$((held + 1))
        continue
      fi
      if ring_lock_reclaim_if_stale "$slot"; then continue; fi
      k=$((k + 1))
    done
    if [ "$held" -ge "$max" ]; then
      echo "RING-LOCK-EXCLUSIVE waited $((SECONDS - begin)) s"
      return 0
    fi
    if [ $((SECONDS - begin)) -ge "$limit" ]; then
      echo "RING-LOCK-EXCLUSIVE-TIMEOUT waited $((SECONDS - begin)) s (holders:)"
      ring_lock_holders "$dir" "$max" "$owner"
      ring_lock_release_all "$owner"
      return 5
    fi
    if [ $((SECONDS - begin)) -ge "$next_report" ]; then
      echo "RING-LOCK-EXCLUSIVE waiting $((SECONDS - begin)) s for these holders:"
      ring_lock_holders "$dir" "$max" "$owner"
      next_report=$((next_report + ${CFD_RING_REPORT_SECONDS:-30}))
    fi
    sleep "$poll"
  done
}

# ring_lock_release_all <owner pid>: remove the marker and every slot this owner holds; others' slots are left alone.
ring_lock_release_all() {
  local owner="$1" dir k=1 max="${CFD_RING_MAX:-2}"
  dir=$(ring_lock_dir)
  while [ "$k" -le "$max" ]; do
    if [ "$(cat "$dir/slot-$k/pid" 2>/dev/null || true)" = "$owner" ]; then rm -rf "$dir/slot-$k"; fi
    k=$((k + 1))
  done
  if [ "$(cat "$dir/exclusive-wanted" 2>/dev/null || true)" = "$owner" ]; then rm -f "$dir/exclusive-wanted"; fi
  return 0
}

# ring_lock_acquire <owner pid>: sets RING_LOCK_SLOT (empty after a timeout) and prints the RING-LOCK line.
ring_lock_acquire() {
  local owner="$1" max="${CFD_RING_MAX:-2}" limit="${CFD_RING_LOCK_WAIT_SECONDS:-900}" poll="${CFD_RING_POLL_SECONDS:-2}"
  local dir begin k slot holders
  dir=$(ring_lock_dir)
  mkdir -p "$dir"
  begin=$SECONDS
  while :; do
    k=1
    while [ "$k" -le "$max" ] && ! ring_lock_exclusive_pending "$dir"; do
      slot="$dir/slot-$k"
      if mkdir "$slot" 2>/dev/null; then
        echo "$owner" > "$slot/pid"
        RING_LOCK_SLOT="$slot"
        echo "RING-LOCK waited $((SECONDS - begin)) s"
        return 0
      fi
      if ring_lock_reclaim_if_stale "$slot"; then continue; fi   # retry the same slot after reclaiming it
      k=$((k + 1))
    done
    if [ $((SECONDS - begin)) -ge "$limit" ]; then
      holders=""
      k=1
      while [ "$k" -le "$max" ]; do holders="$holders $(cat "$dir/slot-$k/pid" 2>/dev/null || echo '?')"; k=$((k + 1)); done
      RING_LOCK_SLOT=""
      echo "RING-LOCK-TIMEOUT waited $((SECONDS - begin)) s (holders:$holders)"
      return 0
    fi
    sleep "$poll"
  done
}

ring_lock_release() {
  if [ -n "$RING_LOCK_SLOT" ]; then rm -rf "$RING_LOCK_SLOT"; RING_LOCK_SLOT=""; fi
}

ring_lock_self_test() {
  local root fails=0 total=0 out holder1 holder2 begin status
  root=$(mktemp -d "${TMPDIR:-/tmp}/ring-lock-test.XXXXXX")
  export CFD_RING_SLOTS_DIR="$root/slots" CFD_RING_MAX=2 CFD_RING_POLL_SECONDS=1
  note() { total=$((total + 1)); if [ "$1" = ok ]; then echo "SELFTEST PASS $2"; else echo "SELFTEST FAIL $2: $3"; fails=$((fails + 1)); fi; }
  mkdir -p "$CFD_RING_SLOTS_DIR"

  # 1. empty directory: first acquire is immediate and records the owner PID
  out=$(ring_lock_acquire $$; echo "slot=$RING_LOCK_SLOT")
  case "$out" in *"RING-LOCK waited 0 s"*) note ok "free slot is taken at once";; *) note bad "free slot is taken at once" "$out";; esac
  rm -rf "$CFD_RING_SLOTS_DIR"/slot-*

  # 2. two live fake holders block a third until the bounded wait ends; the third then proceeds with RING-LOCK-TIMEOUT
  sleep 60 & holder1=$!
  sleep 60 & holder2=$!
  mkdir "$CFD_RING_SLOTS_DIR/slot-1" "$CFD_RING_SLOTS_DIR/slot-2"
  echo "$holder1" > "$CFD_RING_SLOTS_DIR/slot-1/pid"; echo "$holder2" > "$CFD_RING_SLOTS_DIR/slot-2/pid"
  begin=$SECONDS
  out=$(CFD_RING_LOCK_WAIT_SECONDS=3 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in
    *"RING-LOCK-TIMEOUT waited 3 s"*"slot=[]"*) note ok "two live holders block a third, then RING-LOCK-TIMEOUT";;
    *) note bad "two live holders block a third, then RING-LOCK-TIMEOUT" "$out";;
  esac
  [ -d "$CFD_RING_SLOTS_DIR/slot-1" ] && [ -d "$CFD_RING_SLOTS_DIR/slot-2" ] && note ok "a timed-out ring leaves both holders' slots alone" \
    || note bad "a timed-out ring leaves both holders' slots alone" "slot removed"

  # 3. a holder that exits frees its slot: the waiting third acquires and prints a nonzero wait
  (sleep 2; kill "$holder1") &
  out=$(CFD_RING_LOCK_WAIT_SECONDS=20 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in
    *"RING-LOCK waited "[1-9]*" s"*"slot=[$CFD_RING_SLOTS_DIR/slot-1]"*) note ok "a dead holder's slot is reclaimed and the wait is printed";;
    *) note bad "a dead holder's slot is reclaimed and the wait is printed" "$out";;
  esac
  kill "$holder2" 2>/dev/null; wait 2>/dev/null
  rm -rf "$CFD_RING_SLOTS_DIR"/slot-*

  # 4. a crashed ring (dead PID, and a slot with no pid file at all) never deadlocks the next one
  mkdir "$CFD_RING_SLOTS_DIR/slot-1" "$CFD_RING_SLOTS_DIR/slot-2"
  echo 999999 > "$CFD_RING_SLOTS_DIR/slot-1/pid"
  touch -t 200001010000 "$CFD_RING_SLOTS_DIR/slot-2"
  out=$(CFD_RING_LOCK_WAIT_SECONDS=5 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in *"RING-LOCK waited 0 s"*"slot=[$CFD_RING_SLOTS_DIR/slot-1]"*) note ok "a dead-PID slot is reclaimed at once";; *) note bad "a dead-PID slot is reclaimed at once" "$out";; esac
  rm -rf "$CFD_RING_SLOTS_DIR"/slot-*
  mkdir "$CFD_RING_SLOTS_DIR/slot-1" "$CFD_RING_SLOTS_DIR/slot-2"
  touch -t 200001010000 "$CFD_RING_SLOTS_DIR/slot-1" "$CFD_RING_SLOTS_DIR/slot-2"
  out=$(CFD_RING_LOCK_WAIT_SECONDS=5 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in *"RING-LOCK waited 0 s"*"slot=["*"slot-"*) note ok "an old slot with no pid file is reclaimed";; *) note bad "an old slot with no pid file is reclaimed" "$out";; esac

  # 5. release frees only the caller's slot
  RING_LOCK_SLOT="$CFD_RING_SLOTS_DIR/slot-1"; mkdir -p "$RING_LOCK_SLOT"; ring_lock_release
  [ ! -d "$CFD_RING_SLOTS_DIR/slot-1" ] && note ok "release removes the owned slot" || note bad "release removes the owned slot" "still there"

  rm -rf "$CFD_RING_SLOTS_DIR"/slot-* "$CFD_RING_SLOTS_DIR"/exclusive-wanted

  # 6. exclusive mode: waits for a live holder, then holds every slot; a new ring waits behind it; release frees it
  sleep 60 & holder1=$!
  mkdir "$CFD_RING_SLOTS_DIR/slot-1"; echo "$holder1" > "$CFD_RING_SLOTS_DIR/slot-1/pid"
  (sleep 2; kill "$holder1") &
  out=$(CFD_READINESS_LOCK_WAIT_SECONDS=20 ring_lock_acquire_all $$)
  case "$out" in
    *"waiting 0 s for these holders"*"pid=$holder1"*"RING-LOCK-EXCLUSIVE waited "[1-9]*" s"*) note ok "exclusive acquire names the holder, waits for it, then takes every slot";;
    *) note bad "exclusive acquire names the holder, waits for it, then takes every slot" "$out";;
  esac
  if [ "$(cat "$CFD_RING_SLOTS_DIR/slot-1/pid" 2>/dev/null)" = "$$" ] && [ "$(cat "$CFD_RING_SLOTS_DIR/slot-2/pid" 2>/dev/null)" = "$$" ]; then
    note ok "every slot is owned by the exclusive owner"; else note bad "every slot is owned by the exclusive owner" "owners differ"; fi
  out=$(CFD_RING_LOCK_WAIT_SECONDS=2 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in *"RING-LOCK-TIMEOUT waited 2 s"*"slot=[]"*) note ok "a track ring waits while readiness holds the lock";; *) note bad "a track ring waits while readiness holds the lock" "$out";; esac
  ring_lock_release_all $$
  if [ -z "$(ls "$CFD_RING_SLOTS_DIR")" ]; then note ok "release-all frees every slot and the marker"; else note bad "release-all frees every slot and the marker" "$(ls "$CFD_RING_SLOTS_DIR")"; fi

  # 7. the marker stops new rings taking a slot a holder just freed (no starvation); a dead owner's marker is ignored
  sleep 60 & holder2=$!
  echo "$holder2" > "$CFD_RING_SLOTS_DIR/exclusive-wanted"
  out=$(CFD_RING_LOCK_WAIT_SECONDS=2 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in *"RING-LOCK-TIMEOUT"*"slot=[]"*) note ok "a free slot is not taken while an exclusive request is pending";; *) note bad "a free slot is not taken while an exclusive request is pending" "$out";; esac
  kill "$holder2" 2>/dev/null; wait "$holder2" 2>/dev/null
  out=$(CFD_RING_LOCK_WAIT_SECONDS=2 ring_lock_acquire $$; echo "slot=[$RING_LOCK_SLOT]")
  case "$out" in *"RING-LOCK waited 0 s"*) note ok "a dead owner's exclusive marker is ignored";; *) note bad "a dead owner's exclusive marker is ignored" "$out";; esac
  rm -rf "$CFD_RING_SLOTS_DIR"/slot-* "$CFD_RING_SLOTS_DIR"/exclusive-wanted

  # 8. exclusive timeout: exit 5, holder named, the live holder's slot untouched, nothing of ours kept
  sleep 60 & holder1=$!
  mkdir "$CFD_RING_SLOTS_DIR/slot-1"; echo "$holder1" > "$CFD_RING_SLOTS_DIR/slot-1/pid"
  status=0; out=$(CFD_READINESS_LOCK_WAIT_SECONDS=3 ring_lock_acquire_all $$) || status=$?
  case "$out" in
    *"RING-LOCK-EXCLUSIVE-TIMEOUT waited 3 s"*"pid=$holder1"*) if [ "$status" -eq 5 ]; then note ok "exclusive timeout exits 5 and names the holder"; else note bad "exclusive timeout exits 5" "status $status"; fi;;
    *) note bad "exclusive timeout exits 5 and names the holder" "$out";;
  esac
  if [ "$(cat "$CFD_RING_SLOTS_DIR/slot-1/pid" 2>/dev/null)" = "$holder1" ] && [ ! -e "$CFD_RING_SLOTS_DIR/slot-2" ] && [ ! -e "$CFD_RING_SLOTS_DIR/exclusive-wanted" ]; then
    note ok "a timed-out exclusive request keeps nothing and leaves the holder alone"; else note bad "a timed-out exclusive request keeps nothing and leaves the holder alone" "$(ls "$CFD_RING_SLOTS_DIR")"; fi
  kill "$holder1" 2>/dev/null; wait "$holder1" 2>/dev/null

  # 9. CCL-A peers: only a live holder other than the caller is named; a dead holder and the caller's own slot are not
  rm -rf "$CFD_RING_SLOTS_DIR"/slot-* "$CFD_RING_SLOTS_DIR"/exclusive-wanted
  sleep 60 & holder1=$!
  mkdir "$CFD_RING_SLOTS_DIR/slot-1" "$CFD_RING_SLOTS_DIR/slot-2"
  echo "$holder1" > "$CFD_RING_SLOTS_DIR/slot-1/pid"; echo $$ > "$CFD_RING_SLOTS_DIR/slot-2/pid"
  out=$(ring_lock_peers $$ | tr '\n' ' ')
  if [ "$out" = "$holder1 " ]; then note ok "peers names the other live holder and not the caller"; else note bad "peers names the other live holder and not the caller" "[$out]"; fi
  kill "$holder1" 2>/dev/null; wait "$holder1" 2>/dev/null
  out=$(ring_lock_peers $$ | tr '\n' ' ')
  if [ -z "$out" ]; then note ok "peers ignores a dead holder (a ring that ran alone has none)"; else note bad "peers ignores a dead holder" "[$out]"; fi

  rm -rf "$root"
  echo "SELFTEST $((total - fails))/$total cases"
  [ "$fails" -eq 0 ]
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  if [ "${1:-}" = "--self-test" ]; then ring_lock_self_test; exit $?; fi
  if [ "${1:-}" = "--acquire-all" ] && [ -n "${2:-}" ]; then ring_lock_acquire_all "$2"; exit $?; fi
  if [ "${1:-}" = "--release-all" ] && [ -n "${2:-}" ]; then ring_lock_release_all "$2"; exit $?; fi
  echo "usage: tools/ring-lock.sh --self-test | --acquire-all <pid> | --release-all <pid>   (or source it and call ring_lock_acquire <pid>)" >&2
  exit 2
fi
