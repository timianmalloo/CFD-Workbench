#!/bin/bash
# Ruling 151 C2: one read-only, bounded observation. Emits allowlisted fields only.
set -u
export LC_ALL=C
run=/root/CFDWorkbench/runs/20261008T145043Z-win-spike04r3-g2-l3
printf 'capture_start_utc=%s\n' "$(date -u +%FT%TZ)"
printf 'readback_version=1\n'
units=$(systemctl list-units --all --plain --no-legend 'cfdw*' 2>/dev/null | awk '$1 ~ /^cfdw[-A-Za-z0-9_.]*\.service$/ {print $1}')
if test -z "$units"; then printf 'unit=Not recorded (no matching cfdw service)\n'; else
  while IFS= read -r unit; do
    printf 'unit=%s\n' "$unit"
    systemctl show "$unit" --property=ActiveState,SubState,ActiveEnterTimestamp,MainPID,ControlGroup,Nice --no-pager 2>/dev/null | grep -E '^(ActiveState|SubState|ActiveEnterTimestamp|MainPID|ControlGroup|Nice)='
    cg=$(systemctl show "$unit" --value --property=ControlGroup 2>/dev/null)
    if [[ "$cg" =~ ^/[-A-Za-z0-9_./]+$ ]] && test -r "/sys/fs/cgroup$cg/cgroup.procs"; then
      while IFS= read -r pid; do
        [[ "$pid" =~ ^[0-9]+$ ]] || continue
        test -r "/proc/$pid/stat" || continue
        statline=$(cat "/proc/$pid/stat" 2>/dev/null) || continue
        rest=${statline#*) }
        read -ra fields <<< "$rest"
        exe=$(readlink "/proc/$pid/exe" 2>/dev/null || true)
        exe=${exe##*/}
        case "$exe" in bash|python3|python3.*|mpirun|simpleFoam|nice|time) ;; *) continue;; esac
        ppid=${fields[1]:-missing}; ni=${fields[16]:-missing}; start=${fields[19]:-missing}
        role=other
        case "$exe" in simpleFoam) role=rank;; mpirun) role=mpi;; esac
        cmd=$(tr '\0' '\n' < "/proc/$pid/cmdline" 2>/dev/null || true)
        case "$cmd" in *l3-supervisor.sh*) role=supervisor;; *a4-monitor-linux.py*) role=monitor;; esac
        printf 'member pid=%s ppid=%s exe=%s start_ticks=%s ni=%s role=%s\n' "$pid" "$ppid" "$exe" "$start" "$ni" "$role"
        if test "$role" = supervisor || test "$role" = monitor; then
          source_name=l3-supervisor.sh
          test "$role" = monitor && source_name=a4-monitor-linux.py
          source_path=/mnt/c/Projects/CFD-Workbench-win-w4-validation/docs/proof/win-naca/$source_name
          if test -r "$source_path"; then printf 'source role=%s name=%s sha256=%s\n' "$role" "$source_name" "$(sha256sum "$source_path" | cut -d' ' -f1)"; else printf 'source role=%s name=%s sha256=Not recorded\n' "$role" "$source_name"; fi
        fi
      done < "/sys/fs/cgroup$cg/cgroup.procs"
    else printf 'cgroup_members=Not recorded\n'; fi
  done <<< "$units"
fi
if test -d "$run"; then
  printf 'run_present=yes\n'
  for name in run-ledger.txt log.pipeline log.simpleFoam a4-monitor.log convergence.txt time.simpleFoam; do
    file="$run/$name"
    if test -f "$file"; then
      before=$(stat -c '%s %y' "$file" 2>/dev/null || echo 'Not recorded')
      printf 'file=%s before=%s\n' "$name" "$before"
      case "$name" in
        log.simpleFoam)
          # A completed iteration has its own ExecutionTime marker after Time =.
          tail -c 262144 "$file" 2>/dev/null | awk '/^Time = [0-9]+([.]0+)?[[:space:]]*$/ {n=$3} /^ExecutionTime =/ && n!="" {last=n} END {if(last!="") print "latest_complete_iteration=" last; else print "latest_complete_iteration=Not recorded (bounded tail)"}'
          ;;
        a4-monitor.log)
          tail -c 65536 "$file" 2>/dev/null | grep -E '(^iterations=|A4=|^clauses |^window iterations |^stopped_by_A4=)' | tail -n 8 | cut -c 1-240 | sed 's/^/monitor_observation=/' || true
          ;;
        run-ledger.txt)
          tail -n 20 "$file" 2>/dev/null | grep -E '(^start=|^end=|^solver_exit=|^start-note=)' | sed -E 's/start-note=[^ ]+/start-note=<redacted>/' | cut -c 1-240 | sed 's/^/ledger_observation=/' || true
          ;;
      esac
      after=$(stat -c '%s %y' "$file" 2>/dev/null || echo 'Not recorded')
      printf 'file=%s after=%s\n' "$name" "$after"
    else printf 'file=%s state=absent\n' "$name"; fi
  done
  count=$(find "$run" -maxdepth 1 -type d -name 'processor[0-9]*' 2>/dev/null | wc -l)
  printf 'processor_directory_count=%s\n' "$count"
else printf 'run_present=no\n'; fi
printf 'capture_end_utc=%s\n' "$(date -u +%FT%TZ)"
