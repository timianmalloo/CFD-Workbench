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
# Bash 3.2-safe: no associative arrays, no ${!prefix@}, no BASHPID. Exit 0 from --self-test: all cases pass.
#
#   source tools/ring-lock.sh; ring_lock_acquire "$$"; trap ring_lock_release EXIT
#   tools/ring-lock.sh --self-test        (no ring is run; fake holders are `sleep` processes; ring: every join; cost: ~10 s)

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

# ring_lock_acquire <owner pid>: sets RING_LOCK_SLOT (empty after a timeout) and prints the RING-LOCK line.
ring_lock_acquire() {
  local owner="$1" max="${CFD_RING_MAX:-2}" limit="${CFD_RING_LOCK_WAIT_SECONDS:-900}" poll="${CFD_RING_POLL_SECONDS:-2}"
  local dir begin k slot holders
  dir=$(ring_lock_dir)
  mkdir -p "$dir"
  begin=$SECONDS
  while :; do
    k=1
    while [ "$k" -le "$max" ]; do
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
  local root fails=0 total=0 out holder1 holder2 begin
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

  rm -rf "$root"
  echo "SELFTEST $((total - fails))/$total cases"
  [ "$fails" -eq 0 ]
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  if [ "${1:-}" = "--self-test" ]; then ring_lock_self_test; exit $?; fi
  echo "usage: tools/ring-lock.sh --self-test   (or source it and call ring_lock_acquire <pid>)" >&2
  exit 2
fi
